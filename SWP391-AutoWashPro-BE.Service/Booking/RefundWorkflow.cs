using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Booking;

internal static class RefundWorkflow
{
    internal const string CustomerCancelBeforeDeadlineReasonCode = "customer_cancel_before_deadline";
    internal const string CustomerCancelAfterDeadlineReasonCode = "customer_cancel_after_deadline";
    internal const string AdminCancelReasonCode = "admin_cancel";
    internal const string AutoCancelNoCheckinReasonCode = "auto_cancel_no_checkin";

    internal static async Task<decimal> GetTotalPaidAmountAsync(
        AppDbContext dbContext,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.BookingId == bookingId &&
                (transaction.Type == TransactionType.Deposit ||
                 transaction.Type == TransactionType.FullPayment));

        var totalPaidAmount = await query
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken);

        return totalPaidAmount ?? 0m;
    }

    internal static RefundDecision CalculateCustomerCancellationRefund(
        DateTimeOffset now,
        DateTimeOffset bookingStartTime,
        int cancellationDeadlineHours,
        decimal totalPaidAmount)
    {
        var cancellationDeadline = bookingStartTime.AddHours(-cancellationDeadlineHours);

        if (now < cancellationDeadline)
        {
            return CreateDecision(CustomerCancelBeforeDeadlineReasonCode, totalPaidAmount, 100m);
        }

        return CreateDecision(CustomerCancelAfterDeadlineReasonCode, totalPaidAmount, 0m);
    }

    internal static RefundDecision CalculateAdminCancellationRefund(decimal totalPaidAmount)
    {
        return CreateDecision(AdminCancelReasonCode, totalPaidAmount, 100m);
    }

    internal static RefundDecision CreateAutoCancelDecision()
    {
        return CreateDecision(AutoCancelNoCheckinReasonCode, 0m, 0m);
    }

    internal static SWP391_AutoWashPro_BE.Repository.Entities.Transaction CreateRefundTransaction(
        SWP391_AutoWashPro_BE.Repository.Entities.Booking booking,
        Guid customerId,
        decimal refundAmount,
        string reasonCode,
        string description,
        decimal walletBalanceBefore,
        decimal walletBalanceAfter,
        DateTimeOffset now)
    {
        return new SWP391_AutoWashPro_BE.Repository.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            Amount = refundAmount,
            Type = TransactionType.Refund,
            Description = description,
            TransactionDate = now.UtcDateTime,
            CustomerId = customerId,
            BookingId = booking.Id,
            Booking = booking,
            Status = TransactionStatus.Succeeded,
            Provider = ProviderType.Internal,
            TransferType = TransferType.In,
            WalletBalanceBefore = walletBalanceBefore,
            WalletBalanceAfter = walletBalanceAfter,
            RawContent = reasonCode,
            ProviderDescription = reasonCode,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static RefundDecision CreateDecision(
        string reasonCode,
        decimal totalPaidAmount,
        decimal refundPercent)
    {
        var refundAmount = decimal.Round(
            totalPaidAmount * refundPercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return new RefundDecision(
            refundAmount > 0m,
            refundAmount,
            reasonCode);
    }
}

internal sealed record RefundDecision(
    bool RefundApplied,
    decimal RefundAmount,
    string ReasonCode);
