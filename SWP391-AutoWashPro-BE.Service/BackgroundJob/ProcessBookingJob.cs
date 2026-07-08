using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessBookingJob : IJob
{
    // Booking quá 1 phút sau StartTime mà vẫn chưa check-in thì sẽ tự hủy.
    private static readonly TimeSpan AutoCancelGracePeriod = TimeSpan.FromMinutes(1);

    // Dùng để format thời gian hiển thị trong notification theo múi giờ VN.
    private static readonly TimeSpan DisplayTimeOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProcessBookingJob> _logger;

    public ProcessBookingJob(
        AppDbContext dbContext,
        ILogger<ProcessBookingJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var nowUtc = DateTimeOffset.UtcNow;
        var autoCancelBefore = nowUtc.Subtract(AutoCancelGracePeriod);

        // Mở transaction để giảm rủi ro 2 instance cùng xử lý một batch booking.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Lấy trực tiếp các booking overdue cần auto-cancel trong một query duy nhất.
        var overdueBookings = await _dbContext.Bookings
            .Where(x => x.Status == BookingStatus.Confirmed && x.StartTime <= autoCancelBefore)
            .Select(x => new
            {
                Booking = x,
                x.Id,
                x.StartTime,
                CustomerUserId = x.Customer != null ? x.Customer.UserId : (Guid?)null,
                BranchName = x.Branch != null ? x.Branch.Name : null
            })
            .ToListAsync(cancellationToken);

        if (overdueBookings.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var notifications = new List<Repository.Entities.Notification>();

        foreach (var item in overdueBookings)
        {
            // Double-check trạng thái ngay trước khi update để tránh xử lý booking đã bị đổi trạng thái.
            if (item.Booking.Status != BookingStatus.Confirmed)
            {
                continue;
            }

            // Chuyển booking sang Cancelled vì đã quá thời gian check-in.
            item.Booking.Status = BookingStatus.Cancelled;
            item.Booking.CancelledAt = nowUtc;
            item.Booking.UpdatedAt = nowUtc;

            // Chỉ tạo notification khi vẫn còn đủ dữ liệu customer để gửi.
            if (item.CustomerUserId.HasValue)
            {
                notifications.Add(new Repository.Entities.Notification()
                {
                    Id = Guid.NewGuid(),
                    UserId = item.CustomerUserId.Value,
                    Type = NotificationType.BookingCancelled,
                    Title = "Booking Auto-Cancelled",
                    Content =
                        $"Your booking at {item.BranchName ?? "our branch"} on " +
                        $"{item.StartTime.ToOffset(DisplayTimeOffset):HH:mm dd/MM/yyyy} " +
                        $"has been automatically cancelled due to no check-in.",
                    IsRead = false,
                    CreatedAt = nowUtc
                });
            }
        }

        if (notifications.Count > 0)
        {
            _dbContext.Notifications.AddRange(notifications);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "ProcessBookingJob auto-cancelled {BookingCount} booking(s) at {RunTimeUtc}.",
            overdueBookings.Count,
            nowUtc);
    }
}