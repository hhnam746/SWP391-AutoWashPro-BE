using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessBookingAutoCancelJob : IJob
{
    private const string CancelTimeMinutesConfigKey = "CancelTimeMinutes";

    // Dùng để format thời gian hiển thị trong notification theo múi giờ VN.
    private static readonly TimeSpan DisplayTimeOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProcessBookingAutoCancelJob> _logger;

    public ProcessBookingAutoCancelJob(
        AppDbContext dbContext,
        ILogger<ProcessBookingAutoCancelJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var nowUtc = DateTimeOffset.UtcNow;
        var nowLocal = nowUtc.ToOffset(DisplayTimeOffset);
        
        //tìm xem có config CancelTimeMinutes trong System config
        var cancelTimeConfig = await _dbContext.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ConfigKey == CancelTimeMinutesConfigKey, cancellationToken);

        if (cancelTimeConfig == null)
        {
            throw new InvalidOperationException($"{CancelTimeMinutesConfigKey} config not found");
        }

        if (!int.TryParse(cancelTimeConfig.ConfigValue, out var cancelTimeMinutes) || cancelTimeMinutes < 0)
        {
            throw new InvalidOperationException($"Invalid {CancelTimeMinutesConfigKey} config value");
        }

        var autoCancelBefore = nowUtc.AddMinutes(-cancelTimeMinutes);

        _logger.LogInformation(
            "ProcessBookingJob started at {RunTimeUtc} ({RunTimeLocal}). CancelTimeMinutes={CancelTimeMinutes}, AutoCancelBefore={AutoCancelBefore}.",
            nowUtc,
            nowLocal,
            cancelTimeMinutes,
            autoCancelBefore);

        // Mở transaction để giảm rủi ro 2 instance cùng xử lý một batch booking.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Lấy trực tiếp các booking overdue cần auto-cancel trong một query duy nhất.
        var queryOverdueBookings = _dbContext.Bookings
            .Where(x => x.Status == BookingStatus.Confirmed && x.StartTime <= autoCancelBefore);

        var selectedOverdueBookings = queryOverdueBookings.Select(x => new
        {
            Booking = x,
            x.Id,
            x.StartTime,
            CustomerUserId = x.Customer != null ? x.Customer.UserId : (Guid?)null,
            BranchName = x.Branch != null ? x.Branch.Name : null
        });
                
        var overdueBookings = await selectedOverdueBookings.ToListAsync(cancellationToken);

        //Không tìm thấy booking/đơn đặt lịch nào bị quá hạn.
        if (overdueBookings.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "ProcessBookingJob found no overdue bookings at {RunTimeUtc} ({RunTimeLocal}). AutoCancelBefore={AutoCancelBefore}, CancelTimeMinutes={CancelTimeMinutes}.",
                nowUtc,
                nowLocal,
                autoCancelBefore,
                cancelTimeMinutes);
            return;
        }

        var notifications = new List<Repository.Entities.Notification>();
        var cancelledBookingIds = new List<Guid>();

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
            cancelledBookingIds.Add(item.Id);

            _logger.LogInformation(
                "ProcessBookingJob cancelling booking {BookingId}. StartTimeUtc={StartTimeUtc}, StartTimeLocal={StartTimeLocal}, CancelledAtUtc={CancelledAtUtc}.",
                item.Id,
                item.StartTime,
                item.StartTime.ToOffset(DisplayTimeOffset),
                nowUtc);

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

        //background job print after change status
        _logger.LogInformation(
            "ProcessBookingJob auto-cancelled {BookingCount} booking(s) at {RunTimeUtc}. BookingIds={BookingIds}.",
            overdueBookings.Count,
            nowUtc,
            string.Join(", ", cancelledBookingIds));
    }
}
