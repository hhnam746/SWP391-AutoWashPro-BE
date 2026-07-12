using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessBookingReminderJob : IJob
{
    private static readonly TimeSpan DisplayTimeOffset = TimeSpan.FromHours(7);
    private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromDays(1);

    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProcessBookingReminderJob> _logger;

    public ProcessBookingReminderJob(
        AppDbContext dbContext,
        ILogger<ProcessBookingReminderJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var nowUtc = DateTimeOffset.UtcNow;
        var nowLocal = nowUtc.ToOffset(DisplayTimeOffset);
        var reminderBefore = nowUtc.Add(ReminderLeadTime);

        _logger.LogInformation(
            "ProcessBookingReminderJob started at {RunTimeUtc} ({RunTimeLocal}). ReminderBefore={ReminderBefore}.",
            nowUtc,
            nowLocal,
            reminderBefore);

        var dueReminderBookings = await _dbContext.Bookings
            .Where(x => x.Status == BookingStatus.Confirmed && x.StartTime <= reminderBefore)
            .Select(x => new
            {
                Booking = x,
                x.Id,
                x.StartTime,
                CustomerUserId = x.Customer != null ? x.Customer.UserId : (Guid?)null,
                BranchName = x.Branch != null ? x.Branch.Name : null
            })
            .ToListAsync(cancellationToken);

        if (dueReminderBookings.Count == 0)
        {
            _logger.LogInformation(
                "ProcessBookingReminderJob found no due reminders at {RunTimeUtc} ({RunTimeLocal}). ReminderBefore={ReminderBefore}.",
                nowUtc,
                nowLocal,
                reminderBefore);
            return;
        }

        var bookingIds = dueReminderBookings.Select(x => x.Id.ToString()).ToList();
        var existingReminderMetadata = await _dbContext.Notifications
            .Where(x => x.Type == NotificationType.BookingReminder &&
                        x.Metadata != null &&
                        bookingIds.Contains(x.Metadata))
            .Select(x => x.Metadata!)
            .ToListAsync(cancellationToken);

        var existingReminderSet = existingReminderMetadata.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var notifications = new List<Repository.Entities.Notification>();
        var remindedBookingIds = new List<Guid>();

        foreach (var item in dueReminderBookings)
        {
            if (item.Booking.Status != BookingStatus.Confirmed)
            {
                continue;
            }

            var metadata = item.Id.ToString();
            if (existingReminderSet.Contains(metadata) || !item.CustomerUserId.HasValue)
            {
                continue;
            }

            notifications.Add(new Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = item.CustomerUserId.Value,
                Type = NotificationType.BookingReminder,
                Title = "Booking Reminder",
                Content =
                    $"Reminder: Your booking at {item.BranchName ?? "our branch"} starts at {item.StartTime.ToOffset(DisplayTimeOffset):HH:mm dd/MM/yyyy}.",
                Metadata = metadata,
                IsRead = false,
                CreatedAt = nowUtc
            });

            remindedBookingIds.Add(item.Id);
            existingReminderSet.Add(metadata);

            _logger.LogInformation(
                "ProcessBookingReminderJob queued reminder for booking {BookingId}. StartTimeUtc={StartTimeUtc}, StartTimeLocal={StartTimeLocal}.",
                item.Id,
                item.StartTime,
                item.StartTime.ToOffset(DisplayTimeOffset));
        }

        if (notifications.Count == 0)
        {
            _logger.LogInformation(
                "ProcessBookingReminderJob found due bookings but no new reminders to create at {RunTimeUtc}.",
                nowUtc);
            return;
        }

        _dbContext.Notifications.AddRange(notifications);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ProcessBookingReminderJob created {ReminderCount} reminder notification(s) at {RunTimeUtc}. BookingIds={BookingIds}.",
            notifications.Count,
            nowUtc,
            string.Join(", ", remindedBookingIds));
    }
}
