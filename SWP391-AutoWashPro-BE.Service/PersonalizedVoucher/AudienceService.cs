using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class AudienceService : IAudienceService
{
    private readonly AppDbContext _dbContext;
    private readonly IService _personalizedVoucherService;
    private readonly IDeliveryService _deliveryService;
    private readonly Options _options;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<AudienceService> _logger;

    public AudienceService(
        AppDbContext dbContext,
        IService personalizedVoucherService,
        IDeliveryService deliveryService,
        IOptions<Options> options,
        ILogger<AudienceService> logger)
    {
        _dbContext = dbContext;
        _personalizedVoucherService = personalizedVoucherService;
        _deliveryService = deliveryService;
        _options = options.Value;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        _logger = logger;
    }

    public async Task<int> ProcessBirthdayAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(nowUtc, _timeZone).DateTime);
        var cycleKey = PersonalizationPolicy.CreateBirthdayCycleKey(localDate.Year);
        var rules = await GetActiveRulesAsync(
            PersonalizedVoucherTriggerType.Birthday,
            nowUtc,
            cancellationToken);
        if (rules.Count == 0)
        {
            return 0;
        }

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
                TierId = x.TierId,
                DateOfBirth = x.DateOfBirth
            })
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var customer in customers.Where(x =>
                     PersonalizationPolicy.IsBirthday(x.DateOfBirth!.Value, localDate)))
        {
            var rule = SelectEligibleRule(rules, customer.TierId);
            if (rule == null)
            {
                continue;
            }

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
        var nowUtc = DateTimeOffset.UtcNow;
        var rules = await GetActiveRulesAsync(
            PersonalizedVoucherTriggerType.InactiveCustomer,
            nowUtc,
            cancellationToken);
        if (rules.Count == 0)
        {
            return 0;
        }

        var minimumThreshold = rules.Min(x => x.ThresholdDays!.Value);
        var latestEligibleLogin = nowUtc.AddDays(-minimumThreshold);
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
                TierId = x.TierId,
                LastLoginAt = x.User.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var customer in customers.OrderBy(x => x.LastLoginAt))
        {
            var inactiveDays = (int)Math.Floor((nowUtc - customer.LastLoginAt!.Value).TotalDays);
            var eligibleRules = rules.Where(x => IsTierEligible(x.Promotion, customer.TierId));
            var rule = PersonalizationPolicy.SelectInactiveRule(
                eligibleRules,
                inactiveDays,
                x => x.ThresholdDays,
                x => x.Priority);
            if (rule == null)
            {
                continue;
            }

            var cycleKey = PersonalizationPolicy.CreateInactiveCycleKey(
                rule.ThresholdDays!.Value,
                customer.LastLoginAt.Value);
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
        var nowUtc = DateTimeOffset.UtcNow;
        var welcomeRules = await GetActiveRulesAsync(
            PersonalizedVoucherTriggerType.Welcome,
            nowUtc,
            cancellationToken);
        var noFirstBookingRules = await GetActiveRulesAsync(
            PersonalizedVoucherTriggerType.NoFirstBooking,
            nowUtc,
            cancellationToken);
        if (welcomeRules.Count == 0 && noFirstBookingRules.Count == 0)
        {
            return 0;
        }

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
                TierId = x.TierId,
                UserCreatedAt = x.User.CreatedAt,
                VerifiedAt = x.User.VerifiedAt,
                HasWelcomeIssuance = x.PersonalizedVoucherIssuances.Any(i =>
                    i.TriggerType == PersonalizedVoucherTriggerType.Welcome)
            })
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var customer in customers.OrderBy(x => x.UserCreatedAt))
        {
            PersonalizedPromotionRule? welcomeRule = null;
            PersonalizedPromotionRule? noFirstBookingRule = null;
            PersonalizedVoucherTriggerType triggerType;
            string cycleKey;

            if (customer.VerifiedAt.HasValue && !customer.HasWelcomeIssuance)
            {
                welcomeRule = SelectEligibleRule(welcomeRules, customer.TierId);
            }

            var accountAgeDays = (int)Math.Floor((nowUtc - customer.UserCreatedAt).TotalDays);
            noFirstBookingRule = noFirstBookingRules
                .Where(x => IsTierEligible(x.Promotion, customer.TierId) &&
                            x.ThresholdDays <= accountAgeDays)
                .OrderByDescending(x => x.ThresholdDays)
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            var selectedTrigger = PersonalizationPolicy.ChooseAcquisitionTrigger(
                welcomeRule != null,
                noFirstBookingRule != null);
            if (!selectedTrigger.HasValue)
            {
                continue;
            }

            PersonalizedPromotionRule rule;
            if (selectedTrigger == PersonalizedVoucherTriggerType.Welcome)
            {
                rule = welcomeRule!;
                triggerType = PersonalizedVoucherTriggerType.Welcome;
                cycleKey = PersonalizationPolicy.CreateWelcomeCycleKey(customer.VerifiedAt!.Value);
            }
            else
            {
                rule = noFirstBookingRule!;
                triggerType = PersonalizedVoucherTriggerType.NoFirstBooking;
                cycleKey = PersonalizationPolicy.CreateNoFirstBookingCycleKey(
                    rule.ThresholdDays!.Value,
                    customer.UserCreatedAt);
            }

            await IssueAndDispatchAsync(
                customer.CustomerId,
                rule.Id,
                triggerType,
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
        var rules = await GetActiveRulesAsync(
            PersonalizedVoucherTriggerType.TierUpgrade,
            DateTimeOffset.UtcNow,
            cancellationToken);
        var rule = rules
            .Where(x => IsTierEligible(x.Promotion, newTierId))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .FirstOrDefault();
        if (rule == null)
        {
            return new Response.IssueResult
            {
                Status = Response.IssueStatus.Skipped,
                SkippedReason = "No active tier upgrade rule is available for the new tier."
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

    private async Task<List<PersonalizedPromotionRule>> GetActiveRulesAsync(
        PersonalizedVoucherTriggerType triggerType,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PersonalizedPromotionRules
            .AsNoTracking()
            .Include(x => x.Promotion)
            .ThenInclude(x => x.PromotionTiers)
            .ThenInclude(x => x.Tier)
            .Where(x =>
                x.TriggerType == triggerType &&
                x.IsActive &&
                x.Promotion.IsActive &&
                !x.Promotion.IsDeleted &&
                x.Promotion.StartDate <= nowUtc &&
                x.Promotion.EndDate > nowUtc)
            .ToListAsync(cancellationToken);
    }

    private static PersonalizedPromotionRule? SelectEligibleRule(
        IEnumerable<PersonalizedPromotionRule> rules,
        Guid tierId)
    {
        return rules
            .Where(x => IsTierEligible(x.Promotion, tierId))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .FirstOrDefault();
    }

    private static bool IsTierEligible(Repository.Entities.Promotion promotion, Guid tierId)
    {
        return promotion.IsGlobal == true || promotion.PromotionTiers.Any(x =>
            x.TierId == tierId &&
            !x.IsDeleted &&
            !x.Tier.IsDeleted);
    }

    private int BatchSize => Math.Max(1, _options.BatchSize);

    private sealed class CustomerCandidate
    {
        public Guid CustomerId { get; set; }
        public Guid TierId { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset UserCreatedAt { get; set; }
        public DateTimeOffset? VerifiedAt { get; set; }
        public bool HasWelcomeIssuance { get; set; }
    }
}
