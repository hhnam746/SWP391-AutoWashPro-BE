using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessBookingAutoCompleteJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProcessBookingAutoCompleteJob> _logger;

    public ProcessBookingAutoCompleteJob(
        AppDbContext dbContext,
        ILogger<ProcessBookingAutoCompleteJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var nowUtc = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "ProcessBookingAutoCompleteJob started at {RunTimeUtc}.",
            nowUtc);

        var queryDueBookings = _dbContext.Bookings
            .Where(x => x.Status == BookingStatus.InProgress && x.EndTime <= nowUtc);
        
        var selectedQueryDueBookings = queryDueBookings
            .Select(x => new
            {
                Booking = x,
                x.Id,
                CustomerUserId = x.Customer != null ? x.Customer.UserId : (Guid?)null,
                BranchName = x.Branch != null ? x.Branch.Name : null,
            });
                
        var dueBookings = await selectedQueryDueBookings.ToListAsync(cancellationToken);

        if (dueBookings.Count == 0)
        {
            _logger.LogInformation(
                "ProcessBookingAutoCompleteJob found no due bookings at {RunTimeUtc}.",
                nowUtc);
            return;
        }

        var notifications = new List<Repository.Entities.Notification>();
        var completedBookingIds = new List<Guid>();

        foreach (var item in dueBookings)
        {
            if (item.Booking.Status != BookingStatus.InProgress)
            {
                continue;
            }

            item.Booking.Status = BookingStatus.Completed;
            item.Booking.CompletedAt = nowUtc;
            item.Booking.UpdatedAt = nowUtc;
            completedBookingIds.Add(item.Id);

            if (item.CustomerUserId.HasValue)
            {
                notifications.Add(new Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = item.CustomerUserId.Value,
                    Type = NotificationType.BookingCompleted,
                    Title = "Booking Completed",
                    Content =
                        $"Your booking at {item.BranchName ?? "our branch"} has been completed successfully. Thank you for using our service.",
                    IsRead = false,
                    CreatedAt = nowUtc
                });
            }
        }

        if (completedBookingIds.Count == 0)
        {
            _logger.LogInformation(
                "ProcessBookingAutoCompleteJob found due bookings but none remained eligible at {RunTimeUtc}.",
                nowUtc);
            return;
        }

        if (notifications.Count > 0)
        {
            _dbContext.Notifications.AddRange(notifications);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ProcessBookingAutoCompleteJob completed {BookingCount} booking(s) at {RunTimeUtc}. BookingIds={BookingIds}.",
            completedBookingIds.Count,
            nowUtc,
            string.Join(", ", completedBookingIds));
    }
}
