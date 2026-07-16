using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class RuleService : IRuleService
{
    private readonly AppDbContext _dbContext;
    private readonly TimeZoneInfo _timeZone;

    public RuleService(AppDbContext dbContext, IOptions<Options> options)
    {
        _dbContext = dbContext;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
    }

    public async Task<Base.Response.PageResult<Response.RuleResponse>> GetRulesAsync(
        Request.GetRulesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PageIndex < 1 || request.PageSize < 1)
        {
            throw new ArgumentException("PageIndex and PageSize must be greater than 0.");
        }

        var query = _dbContext.PersonalizedPromotionRules.AsNoTracking();
        if (request.TriggerType.HasValue)
        {
            query = query.Where(x => x.TriggerType == request.TriggerType.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.TriggerType)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new Response.RuleResponse
            {
                Id = x.Id,
                PromotionId = x.PromotionId,
                PromotionName = x.Promotion.Name,
                TriggerType = x.TriggerType,
                ThresholdDays = x.ThresholdDays,
                VoucherValidityDays = x.VoucherValidityDays,
                Priority = x.Priority,
                IsActive = x.IsActive,
                SendInAppNotification = x.SendInAppNotification,
                SendEmail = x.SendEmail,
                NotificationTitleTemplate = x.NotificationTitleTemplate,
                NotificationContentTemplate = x.NotificationContentTemplate,
                EmailSubjectTemplate = x.EmailSubjectTemplate,
                EmailBodyTemplate = x.EmailBodyTemplate,
                CallToActionUrl = x.CallToActionUrl,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new Base.Response.PageResult<Response.RuleResponse>
        {
            Items = items,
            TotalItems = totalItems,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }

    public async Task<Response.RuleResponse> CreateRuleAsync(
        Request.RuleRequest request,
        CancellationToken cancellationToken = default)
    {
        RuleValidator.Validate(request);
        await EnsurePromotionExistsAsync(request.PromotionId, cancellationToken);
        await EnsureRuleDoesNotExistAsync(null, request, cancellationToken);

        var nowUtc = DateTimeOffset.UtcNow;
        var rule = new PersonalizedPromotionRule
        {
            Id = Guid.NewGuid(),
            CreatedAt = nowUtc
        };

        ApplyRequest(rule, request, nowUtc);
        rule.UpdatedAt = null;
        _dbContext.PersonalizedPromotionRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRuleResponseAsync(rule.Id, cancellationToken);
    }

    public async Task<Response.RuleResponse> UpdateRuleAsync(
        Guid id,
        Request.RuleRequest request,
        CancellationToken cancellationToken = default)
    {
        RuleValidator.Validate(request);
        await EnsurePromotionExistsAsync(request.PromotionId, cancellationToken);
        await EnsureRuleDoesNotExistAsync(id, request, cancellationToken);

        var rule = await _dbContext.PersonalizedPromotionRules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rule == null)
        {
            throw new KeyNotFoundException("Personalized promotion rule not found.");
        }

        ApplyRequest(rule, request, DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRuleResponseAsync(rule.Id, cancellationToken);
    }

    public async Task<Response.RuleResponse> UpdateRuleStatusAsync(
        Guid id,
        Request.UpdateRuleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var rule = await _dbContext.PersonalizedPromotionRules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rule == null)
        {
            throw new KeyNotFoundException("Personalized promotion rule not found.");
        }

        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRuleResponseAsync(rule.Id, cancellationToken);
    }

    public async Task<List<Response.ReportItem>> GetReportAsync(
        Request.ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromDate == default || request.ToDate == default)
        {
            throw new ArgumentException("FromDate and ToDate are required.");
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate cannot be later than ToDate.");
        }

        var fromUtc = ToUtc(request.FromDate, TimeOnly.MinValue);
        var toExclusiveUtc = ToUtc(request.ToDate.AddDays(1), TimeOnly.MinValue);
        var nowUtc = DateTimeOffset.UtcNow;

        var query = _dbContext.PersonalizedVoucherIssuances
            .AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc);

        if (request.TriggerType.HasValue)
        {
            query = query.Where(x => x.TriggerType == request.TriggerType.Value);
        }

        var rows = await query
            .GroupBy(x => new
            {
                x.PromotionId,
                x.PromotionRuleId,
                CampaignName = x.Promotion.Name,
                x.TriggerType
            })
            .Select(group => new
            {
                group.Key.PromotionId,
                group.Key.PromotionRuleId,
                group.Key.CampaignName,
                group.Key.TriggerType,
                IssuedCount = group.Count(),
                ActiveCount = group.Count(x => x.Voucher.Status == VoucherStatus.Active && x.Voucher.ExpiresAt > nowUtc),
                UsedCount = group.Count(x => x.Voucher.Status == VoucherStatus.Used),
                ExpiredCount = group.Count(x => x.Voucher.Status == VoucherStatus.Expired || x.Voucher.ExpiresAt <= nowUtc),
                NotificationPendingCount = group.Count(x =>
                    x.NotificationStatus == PersonalizedVoucherDeliveryStatus.Pending),
                NotificationSentCount = group.Count(x =>
                    x.NotificationStatus == PersonalizedVoucherDeliveryStatus.Sent),
                NotificationFailedCount = group.Count(x =>
                    x.NotificationStatus == PersonalizedVoucherDeliveryStatus.Failed),
                EmailPendingCount = group.Count(x =>
                    x.EmailStatus == PersonalizedVoucherDeliveryStatus.Pending),
                EmailSentCount = group.Count(x => x.EmailStatus == PersonalizedVoucherDeliveryStatus.Sent),
                EmailFailedCount = group.Count(x =>
                    x.EmailStatus == PersonalizedVoucherDeliveryStatus.Failed)
            })
            .OrderBy(x => x.TriggerType)
            .ThenBy(x => x.CampaignName)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new Response.ReportItem
        {
            PromotionId = x.PromotionId,
            PromotionRuleId = x.PromotionRuleId,
            CampaignName = x.CampaignName,
            TriggerType = x.TriggerType,
            IssuedCount = x.IssuedCount,
            ActiveCount = x.ActiveCount,
            UsedCount = x.UsedCount,
            ExpiredCount = x.ExpiredCount,
            NotificationPendingCount = x.NotificationPendingCount,
            NotificationSentCount = x.NotificationSentCount,
            NotificationFailedCount = x.NotificationFailedCount,
            EmailPendingCount = x.EmailPendingCount,
            EmailSentCount = x.EmailSentCount,
            EmailFailedCount = x.EmailFailedCount,
            ConversionRate = x.IssuedCount == 0
                ? 0
                : Math.Round((decimal)x.UsedCount * 100 / x.IssuedCount, 2)
        }).ToList();
    }

    private async Task<Response.RuleResponse> GetRuleResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _dbContext.PersonalizedPromotionRules
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new Response.RuleResponse
            {
                Id = x.Id,
                PromotionId = x.PromotionId,
                PromotionName = x.Promotion.Name,
                TriggerType = x.TriggerType,
                ThresholdDays = x.ThresholdDays,
                VoucherValidityDays = x.VoucherValidityDays,
                Priority = x.Priority,
                IsActive = x.IsActive,
                SendInAppNotification = x.SendInAppNotification,
                SendEmail = x.SendEmail,
                NotificationTitleTemplate = x.NotificationTitleTemplate,
                NotificationContentTemplate = x.NotificationContentTemplate,
                EmailSubjectTemplate = x.EmailSubjectTemplate,
                EmailBodyTemplate = x.EmailBodyTemplate,
                CallToActionUrl = x.CallToActionUrl,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return response ?? throw new KeyNotFoundException("Personalized promotion rule not found.");
    }

    private async Task EnsurePromotionExistsAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Promotions
            .AsNoTracking()
            .AnyAsync(x => x.Id == promotionId && !x.IsDeleted, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Promotion not found.");
        }
    }

    private async Task EnsureRuleDoesNotExistAsync(
        Guid? currentRuleId,
        Request.RuleRequest request,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.PersonalizedPromotionRules.AsNoTracking().AnyAsync(
            x => (!currentRuleId.HasValue || x.Id != currentRuleId.Value) &&
                 x.PromotionId == request.PromotionId &&
                 x.TriggerType == request.TriggerType &&
                 x.ThresholdDays == request.ThresholdDays,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(
                "A rule with the same promotion, trigger, and threshold already exists.");
        }
    }

    private static void ApplyRequest(
        PersonalizedPromotionRule rule,
        Request.RuleRequest request,
        DateTimeOffset nowUtc)
    {
        rule.PromotionId = request.PromotionId;
        rule.TriggerType = request.TriggerType;
        rule.ThresholdDays = request.ThresholdDays;
        rule.VoucherValidityDays = request.VoucherValidityDays;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;
        rule.SendInAppNotification = request.SendInAppNotification;
        rule.SendEmail = request.SendEmail;
        rule.NotificationTitleTemplate = request.NotificationTitleTemplate?.Trim();
        rule.NotificationContentTemplate = request.NotificationContentTemplate?.Trim();
        rule.EmailSubjectTemplate = request.EmailSubjectTemplate?.Trim();
        rule.EmailBodyTemplate = request.EmailBodyTemplate?.Trim();
        rule.CallToActionUrl = request.CallToActionUrl?.Trim();
        rule.UpdatedAt = rule.CreatedAt == default ? null : nowUtc;
    }

    private DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var localDateTime = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDateTime, _timeZone), TimeSpan.Zero);
    }
}
