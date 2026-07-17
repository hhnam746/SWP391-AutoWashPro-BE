using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class Service : IService
{
    private const string IdempotencyConstraintName =
        "UX_personalized_voucher_issuance_customer_trigger_cycle";

    private readonly AppDbContext _dbContext;
    private readonly ILogger<Service> _logger;

    public Service(AppDbContext dbContext, ILogger<Service> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Response.IssueResult> TryIssuePersonalizedVoucherAsync(
        Guid customerId,
        Guid promotionRuleId,
        PersonalizedVoucherTriggerType triggerType,
        string cycleKey,
        string? triggerReference,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty || promotionRuleId == Guid.Empty)
        {
            throw new ArgumentException("CustomerId and promotionRuleId are required.");
        }

        if (string.IsNullOrWhiteSpace(cycleKey) || cycleKey.Length > 200)
        {
            throw new ArgumentException("CycleKey is required and cannot exceed 200 characters.");
        }

        if (triggerReference?.Length > 200)
        {
            throw new ArgumentException("TriggerReference cannot exceed 200 characters.");
        }

        var normalizedCycleKey = cycleKey.Trim();
        var existing = await FindExistingAsync(customerId, triggerType, normalizedCycleKey, cancellationToken);
        if (existing != null)
        {
            return AlreadyIssued(existing);
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var customer = await _dbContext.CustomerProfiles
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId, cancellationToken);

        if (customer == null)
        {
            return Skipped("Customer not found.");
        }

        if (customer.User.Role != UserRole.Customer ||
            customer.User.Status != AccountStatus.Active ||
            !customer.User.isVerify)
        {
            return Skipped("Customer account is not active and verified.");
        }

        var rule = await _dbContext.PersonalizedPromotionRules
            .Include(x => x.Promotion)
            .ThenInclude(x => x.PromotionTiers)
            .ThenInclude(x => x.Tier)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == promotionRuleId, cancellationToken);

        if (rule == null)
        {
            return Skipped("Personalized promotion rule not found.");
        }

        if (!rule.IsActive || rule.TriggerType != triggerType)
        {
            return Skipped("Personalized promotion rule is inactive or does not match the trigger.");
        }

        var promotion = rule.Promotion;
        if (!promotion.IsActive || promotion.IsDeleted ||
            promotion.StartDate > nowUtc || promotion.EndDate <= nowUtc)
        {
            return Skipped("Promotion is inactive or outside its validity period.");
        }

        var canUsePromotion = promotion.IsGlobal == true ||
                              promotion.PromotionTiers.Any(x =>
                                  x.TierId == customer.TierId &&
                                  !x.IsDeleted &&
                                  !x.Tier.IsDeleted);
        if (!canUsePromotion)
        {
            return Skipped("Customer tier is not eligible for the promotion.");
        }

        if (rule.VoucherValidityDays <= 0)
        {
            return Skipped("Voucher validity configuration is invalid.");
        }

        var expiresAt = nowUtc.AddDays(rule.VoucherValidityDays);
        if (expiresAt > promotion.EndDate)
        {
            expiresAt = promotion.EndDate;
        }

        if (expiresAt <= nowUtc)
        {
            return Skipped("Voucher would expire immediately.");
        }

        var voucher = new Repository.Entities.Voucher
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PromotionId = promotion.Id,
            RewardId = null,
            Code = $"PV-{Guid.NewGuid():N}".ToUpperInvariant(),
            Status = VoucherStatus.Active,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            ExpiresAt = expiresAt,
            CreatedAt = nowUtc
        };

        var issuance = new PersonalizedVoucherIssuance
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PromotionId = promotion.Id,
            PromotionRuleId = rule.Id,
            VoucherId = voucher.Id,
            TriggerType = triggerType,
            CycleKey = normalizedCycleKey,
            TriggerReference = string.IsNullOrWhiteSpace(triggerReference) ? null : triggerReference.Trim(),
            NotificationId = rule.SendInAppNotification ? Guid.NewGuid() : null,
            NotificationStatus = rule.SendInAppNotification
                ? PersonalizedVoucherDeliveryStatus.Pending
                : PersonalizedVoucherDeliveryStatus.NotRequired,
            EmailStatus = rule.SendEmail
                ? PersonalizedVoucherDeliveryStatus.Pending
                : PersonalizedVoucherDeliveryStatus.NotRequired,
            CreatedAt = nowUtc
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _dbContext.Vouchers.Add(voucher);
            _dbContext.PersonalizedVoucherIssuances.Add(issuance);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsIdempotencyConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();

            existing = await FindExistingAsync(customerId, triggerType, normalizedCycleKey, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation(
                    "Personalized voucher issuance already exists. CustomerId={CustomerId}, PromotionId={PromotionId}, TriggerType={TriggerType}, CycleKey={CycleKey}, VoucherId={VoucherId}.",
                    customerId,
                    existing.PromotionId,
                    triggerType,
                    normalizedCycleKey,
                    existing.VoucherId);
                return AlreadyIssued(existing);
            }

            throw;
        }

        _logger.LogInformation(
            "Personalized voucher issued. CustomerId={CustomerId}, PromotionId={PromotionId}, PromotionRuleId={PromotionRuleId}, TriggerType={TriggerType}, CycleKey={CycleKey}, VoucherId={VoucherId}.",
            customerId,
            promotion.Id,
            rule.Id,
            triggerType,
            normalizedCycleKey,
            voucher.Id);

        return new Response.IssueResult
        {
            Status = Response.IssueStatus.Issued,
            IssuanceId = issuance.Id,
            VoucherId = voucher.Id
        };
    }

    private async Task<PersonalizedVoucherIssuance?> FindExistingAsync(
        Guid customerId,
        PersonalizedVoucherTriggerType triggerType,
        string cycleKey,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PersonalizedVoucherIssuances
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId &&
                     x.TriggerType == triggerType &&
                     x.CycleKey == cycleKey,
                cancellationToken);
    }

    private static bool IsIdempotencyConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: IdempotencyConstraintName
        };
    }

    private static Response.IssueResult AlreadyIssued(PersonalizedVoucherIssuance issuance)
    {
        return new Response.IssueResult
        {
            Status = Response.IssueStatus.AlreadyIssued,
            IssuanceId = issuance.Id,
            VoucherId = issuance.VoucherId
        };
    }

    private static Response.IssueResult Skipped(string reason)
    {
        return new Response.IssueResult
        {
            Status = Response.IssueStatus.Skipped,
            SkippedReason = reason
        };
    }
}
