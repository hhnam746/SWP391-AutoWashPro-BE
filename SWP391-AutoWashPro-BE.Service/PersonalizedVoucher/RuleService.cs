using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class RuleService : IRuleService
{
    private static readonly Expression<Func<PersonalizedVoucherRule, Response.RuleResponse>> RuleProjection =
        rule => new Response.RuleResponse
        {
            Id = rule.Id,
            VoucherName = rule.VoucherName,
            TriggerType = rule.TriggerType,
            DiscountType = rule.DiscountType,
            DiscountValue = rule.DiscountValue,
            ThresholdDays = rule.ThresholdDays,
            VoucherValidityDays = rule.VoucherValidityDays,
            IsActive = rule.IsActive,
            SendInAppNotification = rule.SendInAppNotification,
            SendEmail = rule.SendEmail,
            NotificationTitleTemplate = rule.NotificationTitleTemplate,
            NotificationContentTemplate = rule.NotificationContentTemplate,
            EmailSubjectTemplate = rule.EmailSubjectTemplate,
            EmailBodyTemplate = rule.EmailBodyTemplate,
            CallToActionUrl = rule.CallToActionUrl,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };

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

        var query = _dbContext.PersonalizedVoucherRules.AsNoTracking();
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
            .ThenByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(RuleProjection)
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
        await EnsureSingleActiveRuleAsync(null, request.TriggerType, request.IsActive, cancellationToken);

        var nowUtc = DateTimeOffset.UtcNow;
        var rule = new PersonalizedVoucherRule
        {
            Id = Guid.NewGuid(),
            CreatedAt = nowUtc
        };

        ApplyRequest(rule, request, nowUtc);
        rule.UpdatedAt = null;
        _dbContext.PersonalizedVoucherRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRuleResponseAsync(rule.Id, cancellationToken);
    }

    public async Task<Response.RuleResponse> UpdateRuleAsync(
        Guid id,
        Request.RuleRequest request,
        CancellationToken cancellationToken = default)
    {
        RuleValidator.Validate(request);

        var rule = await _dbContext.PersonalizedVoucherRules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rule == null)
        {
            throw new KeyNotFoundException("Personalized voucher rule not found.");
        }

        await EnsureSingleActiveRuleAsync(id, request.TriggerType, request.IsActive, cancellationToken);
        ApplyRequest(rule, request, DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRuleResponseAsync(rule.Id, cancellationToken);
    }

    public async Task<Response.RuleResponse> UpdateRuleStatusAsync(
        Guid id,
        Request.UpdateRuleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var rule = await _dbContext.PersonalizedVoucherRules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rule == null)
        {
            throw new KeyNotFoundException("Personalized voucher rule not found.");
        }

        await EnsureSingleActiveRuleAsync(id, rule.TriggerType, request.IsActive, cancellationToken);
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
                x.VoucherRuleId,
                VoucherName = x.Voucher.Name,
                x.TriggerType
            })
            .Select(group => new
            {
                group.Key.VoucherRuleId,
                group.Key.VoucherName,
                group.Key.TriggerType,
                IssuedCount = group.Count(),
                ActiveCount = group.Count(x =>
                    x.Voucher.Status == VoucherStatus.Active && x.Voucher.ExpiresAt > nowUtc),
                ReservedCount = group.Count(x => x.Voucher.Status == VoucherStatus.Reserved),
                UsedCount = group.Count(x => x.Voucher.Status == VoucherStatus.Used),
                ExpiredCount = group.Count(x =>
                    x.Voucher.Status == VoucherStatus.Expired ||
                    (
                        x.Voucher.Status == VoucherStatus.Active &&
                        x.Voucher.ExpiresAt <= nowUtc
                    )),
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
            .ThenBy(x => x.VoucherName)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new Response.ReportItem
        {
            VoucherRuleId = x.VoucherRuleId,
            VoucherName = x.VoucherName,
            TriggerType = x.TriggerType,
            IssuedCount = x.IssuedCount,
            ActiveCount = x.ActiveCount,
            ReservedCount = x.ReservedCount,
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

    private async Task<Response.RuleResponse> GetRuleResponseAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _dbContext.PersonalizedVoucherRules
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(RuleProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return response ?? throw new KeyNotFoundException("Personalized voucher rule not found.");
    }

    private async Task EnsureSingleActiveRuleAsync(
        Guid? currentRuleId,
        PersonalizedVoucherTriggerType triggerType,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!isActive)
        {
            return;
        }

        var exists = await _dbContext.PersonalizedVoucherRules
            .AsNoTracking()
            .AnyAsync(
                x => x.IsActive &&
                     x.TriggerType == triggerType &&
                     (!currentRuleId.HasValue || x.Id != currentRuleId.Value),
                cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(
                $"An active personalized voucher rule already exists for trigger {triggerType}.");
        }
    }

    private static void ApplyRequest(
        PersonalizedVoucherRule rule,
        Request.RuleRequest request,
        DateTimeOffset nowUtc)
    {
        rule.VoucherName = request.VoucherName.Trim();
        rule.TriggerType = request.TriggerType;
        rule.DiscountType = request.DiscountType;
        rule.DiscountValue = request.DiscountValue;
        rule.ThresholdDays = request.ThresholdDays;
        rule.VoucherValidityDays = request.VoucherValidityDays;
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
