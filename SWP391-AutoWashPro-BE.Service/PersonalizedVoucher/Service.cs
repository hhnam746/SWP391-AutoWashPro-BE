using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class Service : IService
{
    private const string IdempotencyConstraintName =
        "UX_personalized_voucher_issuance_customer_trigger_cycle";

    private readonly AppDbContext _dbContext;
    private readonly ITriggerConfigService _triggerConfigService;
    private readonly ILogger<Service> _logger;

    public Service(
        AppDbContext dbContext,
        ITriggerConfigService triggerConfigService,
        ILogger<Service> logger)
    {
        _dbContext = dbContext;
        _triggerConfigService = triggerConfigService;
        _logger = logger;
    }

    public async Task<Response.IssueResult> TryIssuePersonalizedVoucherAsync(
        Guid customerId,
        Guid voucherRuleId,
        PersonalizedVoucherTriggerType triggerType,
        string cycleKey,
        string? triggerReference,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty || voucherRuleId == Guid.Empty)
        {
            throw new ArgumentException("CustomerId and voucherRuleId are required.");
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

        if (!await _triggerConfigService.IsEnabledAsync(triggerType, cancellationToken))
        {
            return Skipped("Personalized voucher trigger is disabled or misconfigured.");
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

        var rule = await _dbContext.PersonalizedVoucherRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == voucherRuleId, cancellationToken);

        if (rule == null)
        {
            return Skipped("Personalized voucher rule not found.");
        }

        if (!rule.IsActive || rule.TriggerType != triggerType)
        {
            return Skipped("Personalized voucher rule is inactive or does not match the trigger.");
        }

        if (rule.VoucherValidityDays <= 0 || rule.DiscountValue <= 0)
        {
            return Skipped("Voucher rule configuration is invalid.");
        }

        if (rule.DiscountType == DiscountType.Percentage && rule.DiscountValue > 100)
        {
            return Skipped("Voucher percentage discount configuration is invalid.");
        }

        var voucher = new Repository.Entities.Voucher
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            RewardId = null,
            Name = rule.VoucherName,
            Code = $"PV-{Guid.NewGuid():N}".ToUpperInvariant(),
            Status = VoucherStatus.Active,
            DiscountType = rule.DiscountType,
            DiscountValue = rule.DiscountValue,
            ExpiresAt = nowUtc.AddDays(rule.VoucherValidityDays),
            CreatedAt = nowUtc
        };

        var issuance = new PersonalizedVoucherIssuance
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            VoucherRuleId = rule.Id,
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
                    "Personalized voucher issuance already exists. CustomerId={CustomerId}, VoucherRuleId={VoucherRuleId}, TriggerType={TriggerType}, CycleKey={CycleKey}, VoucherId={VoucherId}.",
                    customerId,
                    existing.VoucherRuleId,
                    triggerType,
                    normalizedCycleKey,
                    existing.VoucherId);
                return AlreadyIssued(existing);
            }

            throw;
        }

        _logger.LogInformation(
            "Personalized voucher issued. CustomerId={CustomerId}, VoucherRuleId={VoucherRuleId}, TriggerType={TriggerType}, CycleKey={CycleKey}, VoucherId={VoucherId}.",
            customerId,
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
