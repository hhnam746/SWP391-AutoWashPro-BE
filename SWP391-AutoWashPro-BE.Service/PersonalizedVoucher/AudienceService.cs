using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class AudienceService : IAudienceService
{
    private readonly AppDbContext _dbContext;
    private readonly IService _personalizedVoucherService;
    private readonly IDeliveryService _deliveryService;
    private readonly ITriggerConfigService _triggerConfigService;
    private readonly Options _options;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<AudienceService> _logger;

    public AudienceService(
        AppDbContext dbContext,
        IService personalizedVoucherService,
        IDeliveryService deliveryService,
        ITriggerConfigService triggerConfigService,
        IOptions<Options> options,
        ILogger<AudienceService> logger)
    {
        _dbContext = dbContext;
        _personalizedVoucherService = personalizedVoucherService;
        _deliveryService = deliveryService;
        _triggerConfigService = triggerConfigService;
        _options = options.Value;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        _logger = logger;
    }

    public async Task<int> ProcessBirthdayAsync(CancellationToken cancellationToken = default)
    {
        var rule = await GetActiveRuleAsync(
            PersonalizedVoucherTriggerType.Birthday,
            cancellationToken);
        if (rule == null)
        {
            return 0;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(nowUtc, _timeZone).DateTime);
        var cycleKey = PersonalizationPolicy.CreateBirthdayCycleKey(localDate.Year);

        var customers = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(x =>
                x.DateOfBirth.HasValue &&
                x.User.Role == UserRole.Customer &&
                x.User.Status == AccountStatus.Active &&
                x.User.isVerify &&
                !x.PersonalizedVoucherIssuances.Any(i =>
                    i.TriggerType == PersonalizedVoucherTriggerType.Birthday &&
                    i.CycleKey == cycleKey))
            .Select(x => new CustomerCandidate
            {
                CustomerId = x.Id,
                DateOfBirth = x.DateOfBirth
            })
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var customer in customers.Where(x =>
                     PersonalizationPolicy.IsBirthday(x.DateOfBirth!.Value, localDate)))
        {
            await IssueAndDispatchAsync(
                customer.CustomerId,
                rule.Id,
                PersonalizedVoucherTriggerType.Birthday,
                cycleKey,
                localDate.ToString("yyyy-MM-dd"),
                cancellationToken);
            processed++;
            if (processed >= BatchSize)
            {
                break;
            }
        }

        return processed;
    }

    public async Task<int> ProcessInactiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        var rule = await GetActiveRuleAsync(
            PersonalizedVoucherTriggerType.InactiveCustomer,
            cancellationToken);
        if (rule?.ThresholdDays is not > 0)
        {
            return 0;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var thresholdDays = rule.ThresholdDays.Value;
        var latestEligibleLogin = nowUtc.AddDays(-thresholdDays);
        var customers = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(x =>
                x.User.Role == UserRole.Customer &&
                x.User.Status == AccountStatus.Active &&
                x.User.isVerify &&
                x.User.LastLoginAt.HasValue &&
                x.User.LastLoginAt <= latestEligibleLogin)
            .Select(x => new CustomerCandidate
            {
                CustomerId = x.Id,
                LastLoginAt = x.User.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var customer in customers.OrderBy(x => x.LastLoginAt))
        {
            var cycleKey = PersonalizationPolicy.CreateInactiveCycleKey(
                thresholdDays,
                customer.LastLoginAt!.Value);
            var existing = await _dbContext.PersonalizedVoucherIssuances
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CustomerId == customer.CustomerId &&
                    x.TriggerType == PersonalizedVoucherTriggerType.InactiveCustomer &&
                    x.CycleKey == cycleKey,
                    cancellationToken);
            if (existing)
            {
                continue;
            }

            await IssueAndDispatchAsync(
                customer.CustomerId,
                rule.Id,
                PersonalizedVoucherTriggerType.InactiveCustomer,
                cycleKey,
                customer.LastLoginAt.Value.ToString("O"),
                cancellationToken);
            processed++;
            if (processed >= BatchSize)
            {
                break;
            }
        }

        return processed;
    }

    public async Task<int> ProcessAcquisitionAsync(CancellationToken cancellationToken = default)
    {
        var welcomeRule = await GetActiveRuleAsync(
            PersonalizedVoucherTriggerType.Welcome,
            cancellationToken);
        var noFirstBookingRule = await GetActiveRuleAsync(
            PersonalizedVoucherTriggerType.NoFirstBooking,
            cancellationToken);
        if (welcomeRule == null && noFirstBookingRule == null)
        {
            return 0;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var customers = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(x =>
                x.User.Role == UserRole.Customer &&
                x.User.Status == AccountStatus.Active &&
                x.User.isVerify &&
                !x.Bookings.Any() &&
                !x.Vouchers.Any(v =>
                    v.Status == VoucherStatus.Active &&
                    v.ExpiresAt > nowUtc &&
                    v.PersonalizedVoucherIssuance != null &&
                    (v.PersonalizedVoucherIssuance.TriggerType == PersonalizedVoucherTriggerType.Welcome ||
                     v.PersonalizedVoucherIssuance.TriggerType == PersonalizedVoucherTriggerType.NoFirstBooking)))
            .Select(x => new CustomerCandidate
            {
                CustomerId = x.Id,
                UserCreatedAt = x.User.CreatedAt,
                VerifiedAt = x.User.VerifiedAt,
                HasWelcomeIssuance = x.PersonalizedVoucherIssuances.Any(i =>
                    i.TriggerType == PersonalizedVoucherTriggerType.Welcome)
            })
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var customer in customers.OrderBy(x => x.UserCreatedAt))
        {
            var canIssueWelcome =
                welcomeRule != null &&
                customer.VerifiedAt.HasValue &&
                !customer.HasWelcomeIssuance;

            var accountAgeDays = (int)Math.Floor((nowUtc - customer.UserCreatedAt).TotalDays);
            var canIssueNoFirstBooking =
                noFirstBookingRule?.ThresholdDays is > 0 &&
                noFirstBookingRule.ThresholdDays.Value <= accountAgeDays;

            var selectedTrigger = PersonalizationPolicy.ChooseAcquisitionTrigger(
                canIssueWelcome,
                canIssueNoFirstBooking);
            if (!selectedTrigger.HasValue)
            {
                continue;
            }

            PersonalizedVoucherRule rule;
            string cycleKey;
            if (selectedTrigger == PersonalizedVoucherTriggerType.Welcome)
            {
                rule = welcomeRule!;
                cycleKey = PersonalizationPolicy.CreateWelcomeCycleKey(customer.VerifiedAt!.Value);
            }
            else
            {
                rule = noFirstBookingRule!;
                cycleKey = PersonalizationPolicy.CreateNoFirstBookingCycleKey(
                    rule.ThresholdDays!.Value,
                    customer.UserCreatedAt);
            }

            await IssueAndDispatchAsync(
                customer.CustomerId,
                rule.Id,
                selectedTrigger.Value,
                cycleKey,
                null,
                cancellationToken);
            processed++;
            if (processed >= BatchSize)
            {
                break;
            }
        }

        return processed;
    }

    public async Task<Response.IssueResult> ProcessTierUpgradeAsync(
        Guid customerId,
        Guid newTierId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetActiveRuleAsync(
            PersonalizedVoucherTriggerType.TierUpgrade,
            cancellationToken);
        if (rule == null)
        {
            return new Response.IssueResult
            {
                Status = Response.IssueStatus.Skipped,
                SkippedReason = "No active tier upgrade voucher rule is available."
            };
        }

        return await IssueAndDispatchAsync(
            customerId,
            rule.Id,
            PersonalizedVoucherTriggerType.TierUpgrade,
            PersonalizationPolicy.CreateTierUpgradeCycleKey(newTierId),
            bookingId.ToString(),
            cancellationToken);
    }

    private async Task<Response.IssueResult> IssueAndDispatchAsync(
        Guid customerId,
        Guid ruleId,
        PersonalizedVoucherTriggerType triggerType,
        string cycleKey,
        string? triggerReference,
        CancellationToken cancellationToken)
    {
        var result = await _personalizedVoucherService.TryIssuePersonalizedVoucherAsync(
            customerId,
            ruleId,
            triggerType,
            cycleKey,
            triggerReference,
            cancellationToken);

        if (result.IssuanceId.HasValue && result.Status is
            Response.IssueStatus.Issued or Response.IssueStatus.AlreadyIssued)
        {
            await _deliveryService.DispatchAsync(result.IssuanceId.Value, cancellationToken);
        }
        else if (result.Status == Response.IssueStatus.Skipped)
        {
            _logger.LogInformation(
                "Personalized voucher candidate skipped. CustomerId={CustomerId}, RuleId={RuleId}, TriggerType={TriggerType}, CycleKey={CycleKey}, Reason={Reason}.",
                customerId,
                ruleId,
                triggerType,
                cycleKey,
                result.SkippedReason);
        }

        return result;
    }

    private async Task<PersonalizedVoucherRule?> GetActiveRuleAsync(
        PersonalizedVoucherTriggerType triggerType,
        CancellationToken cancellationToken)
    {
        if (!await _triggerConfigService.IsEnabledAsync(triggerType, cancellationToken))
        {
            return null;
        }

        return await _dbContext.PersonalizedVoucherRules
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TriggerType == triggerType && x.IsActive,
                cancellationToken);
    }

    private int BatchSize => Math.Max(1, _options.BatchSize);

    private sealed class CustomerCandidate
    {
        public Guid CustomerId { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset UserCreatedAt { get; set; }
        public DateTimeOffset? VerifiedAt { get; set; }
        public bool HasWelcomeIssuance { get; set; }
    }
}
