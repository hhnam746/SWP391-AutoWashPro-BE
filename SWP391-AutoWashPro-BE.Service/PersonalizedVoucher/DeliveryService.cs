using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using MailService = SWP391_AutoWashPro_BE.Service.MailService;
using NotificationService = SWP391_AutoWashPro_BE.Service.Notification;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class DeliveryService : IDeliveryService
{
    private const string NotificationErrorCode = "NOTIFICATION_DELIVERY_FAILED";
    private const string EmailErrorCode = "EMAIL_DELIVERY_FAILED";

    private readonly AppDbContext _dbContext;
    private readonly NotificationService.IService _notificationService;
    private readonly MailService.IService _mailService;
    private readonly Options _options;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<DeliveryService> _logger;

    public DeliveryService(
        AppDbContext dbContext,
        NotificationService.IService notificationService,
        MailService.IService mailService,
        IOptions<Options> options,
        ILogger<DeliveryService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _mailService = mailService;
        _options = options.Value;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        _logger = logger;
    }

    public async Task DispatchAsync(Guid issuanceId, CancellationToken cancellationToken = default)
    {
        var issuance = await _dbContext.PersonalizedVoucherIssuances
            .Include(x => x.Customer)
            .ThenInclude(x => x.User)
            .Include(x => x.VoucherRule)
            .Include(x => x.Voucher)
            .FirstOrDefaultAsync(x => x.Id == issuanceId, cancellationToken);
        if (issuance == null)
        {
            return;
        }

        if (CanAttempt(
                issuance.NotificationStatus,
                issuance.NotificationAttemptCount,
                issuance.NotificationLastAttemptAt))
        {
            await SendNotificationAsync(issuance, cancellationToken);
        }

        if (CanAttempt(issuance.EmailStatus, issuance.EmailAttemptCount, issuance.EmailLastAttemptAt))
        {
            await SendEmailAsync(issuance, cancellationToken);
        }
    }

    public async Task<int> RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        var retryBefore = DateTimeOffset.UtcNow.AddMinutes(-_options.DeliveryRetryDelayMinutes);
        var maxAttempts = Math.Max(1, _options.DeliveryMaxAttempts);
        var batchSize = Math.Max(1, _options.BatchSize);

        var issuanceIds = await _dbContext.PersonalizedVoucherIssuances
            .AsNoTracking()
            .Where(x =>
                ((x.NotificationStatus == PersonalizedVoucherDeliveryStatus.Pending ||
                  x.NotificationStatus == PersonalizedVoucherDeliveryStatus.Failed) &&
                 x.NotificationAttemptCount < maxAttempts &&
                 (!x.NotificationLastAttemptAt.HasValue || x.NotificationLastAttemptAt <= retryBefore)) ||
                ((x.EmailStatus == PersonalizedVoucherDeliveryStatus.Pending ||
                  x.EmailStatus == PersonalizedVoucherDeliveryStatus.Failed) &&
                 x.EmailAttemptCount < maxAttempts &&
                 (!x.EmailLastAttemptAt.HasValue || x.EmailLastAttemptAt <= retryBefore)))
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var issuanceId in issuanceIds)
        {
            await DispatchAsync(issuanceId, cancellationToken);
        }

        return issuanceIds.Count;
    }

    private bool CanAttempt(
        PersonalizedVoucherDeliveryStatus status,
        int attemptCount,
        DateTimeOffset? lastAttemptAt)
    {
        if (status is not (PersonalizedVoucherDeliveryStatus.Pending or
            PersonalizedVoucherDeliveryStatus.Failed))
        {
            return false;
        }

        if (attemptCount >= Math.Max(1, _options.DeliveryMaxAttempts))
        {
            return false;
        }

        return !lastAttemptAt.HasValue ||
               lastAttemptAt <= DateTimeOffset.UtcNow.AddMinutes(-_options.DeliveryRetryDelayMinutes);
    }

    private async Task SendNotificationAsync(
        PersonalizedVoucherIssuance issuance,
        CancellationToken cancellationToken)
    {
        var rule = issuance.VoucherRule;
        if (!rule.SendInAppNotification || !issuance.NotificationId.HasValue)
        {
            issuance.NotificationStatus = PersonalizedVoucherDeliveryStatus.NotRequired;
            issuance.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var attemptAt = DateTimeOffset.UtcNow;
        issuance.NotificationAttemptCount++;
        issuance.NotificationLastAttemptAt = attemptAt;
        issuance.UpdatedAt = attemptAt;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var title = Render(rule.NotificationTitleTemplate!, issuance, false);
            var content = Render(rule.NotificationContentTemplate!, issuance, false);
            var metadata = JsonSerializer.Serialize(new
            {
                issuance.VoucherId,
                issuance.VoucherRuleId,
                issuance.TriggerType,
                issuance.CycleKey
            });

            await _notificationService.SendNotificationToUser(
                issuance.Customer.UserId,
                issuance.NotificationId.Value,
                NotificationType.PersonalizedVoucher,
                title,
                content,
                metadata,
                cancellationToken);

            issuance.NotificationStatus = PersonalizedVoucherDeliveryStatus.Sent;
            issuance.NotificationSentAt = DateTimeOffset.UtcNow;
            issuance.NotificationLastError = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            issuance.NotificationStatus = PersonalizedVoucherDeliveryStatus.Failed;
            issuance.NotificationLastError = NotificationErrorCode;
            LogDeliveryFailure(issuance, NotificationErrorCode);
        }

        issuance.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SendEmailAsync(
        PersonalizedVoucherIssuance issuance,
        CancellationToken cancellationToken)
    {
        var rule = issuance.VoucherRule;
        if (!rule.SendEmail)
        {
            issuance.EmailStatus = PersonalizedVoucherDeliveryStatus.NotRequired;
            issuance.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var attemptAt = DateTimeOffset.UtcNow;
        issuance.EmailAttemptCount++;
        issuance.EmailLastAttemptAt = attemptAt;
        issuance.UpdatedAt = attemptAt;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(issuance.Customer.User.Email))
            {
                throw new InvalidOperationException("Customer email is not configured.");
            }

            await _mailService.SendMail(new MailService.MailContent
            {
                To = issuance.Customer.User.Email,
                Subject = Render(rule.EmailSubjectTemplate!, issuance, false),
                Body = Render(rule.EmailBodyTemplate!, issuance, true)
            }, cancellationToken);

            issuance.EmailStatus = PersonalizedVoucherDeliveryStatus.Sent;
            issuance.EmailSentAt = DateTimeOffset.UtcNow;
            issuance.EmailLastError = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            issuance.EmailStatus = PersonalizedVoucherDeliveryStatus.Failed;
            issuance.EmailLastError = EmailErrorCode;
            LogDeliveryFailure(issuance, EmailErrorCode);
        }

        issuance.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string Render(
        string template,
        PersonalizedVoucherIssuance issuance,
        bool htmlEncodeValues)
    {
        var customerName = $"{issuance.Customer.FirstName} {issuance.Customer.LastName}".Trim();
        var expiresAt = TimeZoneInfo.ConvertTime(issuance.Voucher.ExpiresAt, _timeZone)
            .ToString("dd/MM/yyyy HH:mm");

        return TemplateRenderer.Render(
            template,
            customerName,
            issuance.Voucher.Name,
            issuance.Voucher.DiscountType,
            issuance.Voucher.DiscountValue,
            issuance.Voucher.Code,
            expiresAt,
            issuance.VoucherRule.CallToActionUrl ?? string.Empty,
            htmlEncodeValues);
    }

    private void LogDeliveryFailure(PersonalizedVoucherIssuance issuance, string errorCode)
    {
        _logger.LogWarning(
            "Personalized voucher delivery failed. CustomerId={CustomerId}, VoucherRuleId={VoucherRuleId}, TriggerType={TriggerType}, CycleKey={CycleKey}, VoucherId={VoucherId}, ErrorCode={ErrorCode}.",
            issuance.CustomerId,
            issuance.VoucherRuleId,
            issuance.TriggerType,
            issuance.CycleKey,
            issuance.VoucherId,
            errorCode);
    }
}
