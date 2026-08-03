using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.SePay;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<Service> _logger;

    public Service(AppDbContext dbContext, ILogger<Service> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Response.SePayWebhookResponse> SePayWebhook(Request.SePayWebhookRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body is required.");
        }

        if (request.Id <= 0)
        {
            throw new ArgumentException("Webhook id must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Content is required.");
        }

        if (string.IsNullOrWhiteSpace(request.TransferType))
        {
            throw new ArgumentException("TransferType is required.");
        }

        if (request.TransferAmount <= 0)
        {
            throw new ArgumentException("TransferAmount must be greater than 0.");
        }

        var normalizedContent = request.Content.Trim();
        var externalTransactionId = request.Id.ToString();
        var transferType = request.TransferType.Trim().ToLowerInvariant();
        var rawPayload = JsonSerializer.Serialize(request);

        if (!string.Equals(transferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Ignored SePay webhook because TransferType={TransferType} is not supported. WebhookId={WebhookId}.",
                request.TransferType,
                request.Id);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "ignored",
                Message = "Only transferType 'in' is supported."
            };
        }

        var duplicateQuery = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.ExternalTransactionId == externalTransactionId);

        var duplicateTransaction = await duplicateQuery
            .Select(transaction => new
            {
                transaction.Id
            })
            .FirstOrDefaultAsync();

        if (duplicateTransaction != null)
        {
            _logger.LogInformation(
                "Ignored duplicate SePay webhook. WebhookId={WebhookId}, TransactionId={TransactionId}.",
                request.Id,
                duplicateTransaction.Id);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "duplicate",
                Message = "Webhook was already processed.",
                TransactionId = duplicateTransaction.Id
            };
        }

        var pendingTransactionQuery = _dbContext.Transactions
            .Where(transaction =>
                transaction.Type == TransactionType.WalletTopup &&
                transaction.Status == TransactionStatus.Pending &&
                transaction.ReferenceCode == normalizedContent);

        var pendingTransaction = await pendingTransactionQuery
            .FirstOrDefaultAsync();

        if (pendingTransaction == null)
        {
            _logger.LogWarning(
                "Ignored SePay webhook because no pending wallet top-up was found for ReferenceCode={ReferenceCode}. WebhookId={WebhookId}.",
                normalizedContent,
                request.Id);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "ignored",
                Message = "No pending wallet top-up transaction matched the webhook reference."
            };
        }

        if (pendingTransaction.Amount != request.TransferAmount)
        {
            _logger.LogWarning(
                "Ignored SePay webhook because amount mismatch. TransactionId={TransactionId}, ExpectedAmount={ExpectedAmount}, ActualAmount={ActualAmount}.",
                pendingTransaction.Id,
                pendingTransaction.Amount,
                request.TransferAmount);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "amount_mismatch",
                Message = "Webhook amount did not match the pending transaction.",
                TransactionId = pendingTransaction.Id
            };
        }

        var walletQuery = _dbContext.Wallets
            .Where(wallet => wallet.CustomerId == pendingTransaction.CustomerId);

        var wallet = await walletQuery
            .FirstOrDefaultAsync();

        if (wallet == null)
        {
            throw new KeyNotFoundException("Wallet not found.");
        }

        DateTimeOffset? providerTransactionDate = null;
        if (!string.IsNullOrWhiteSpace(request.TransactionDate) &&
            DateTimeOffset.TryParse(request.TransactionDate, out var parsedTransactionDate))
        {
            providerTransactionDate = parsedTransactionDate;
        }

        var now = DateTimeOffset.UtcNow;
        var paidAt = providerTransactionDate ?? now;
        var walletBalanceBefore = wallet.Balance;
        var walletBalanceAfter = walletBalanceBefore + pendingTransaction.Amount;

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

        wallet.Balance = walletBalanceAfter;
        wallet.UpdatedAt = now;

        pendingTransaction.Status = TransactionStatus.Succeeded;
        pendingTransaction.Provider = ProviderType.SePay;
        pendingTransaction.ExternalTransactionId = externalTransactionId;
        pendingTransaction.TransferType = TransferType.In;
        pendingTransaction.Gateway = request.Gateway;
        pendingTransaction.AccountNumber = request.AccountNumber;
        pendingTransaction.ProviderCode = request.Code;
        pendingTransaction.BankReferenceCode = request.ReferenceCode;
        pendingTransaction.ProviderTransactionDate = providerTransactionDate;
        pendingTransaction.PaidAt = paidAt;
        pendingTransaction.RawContent = normalizedContent;
        pendingTransaction.ProviderDescription = request.Description;
        pendingTransaction.RawPayload = rawPayload;
        pendingTransaction.WalletBalanceBefore = walletBalanceBefore;
        pendingTransaction.WalletBalanceAfter = walletBalanceAfter;
        pendingTransaction.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        _logger.LogInformation(
            "Processed SePay webhook successfully. WebhookId={WebhookId}, TransactionId={TransactionId}, CustomerId={CustomerId}, Amount={Amount}.",
            request.Id,
            pendingTransaction.Id,
            pendingTransaction.CustomerId,
            pendingTransaction.Amount);

        return new Response.SePayWebhookResponse
        {
            Success = true,
            Code = "processed",
            Message = "Webhook processed successfully.",
            TransactionId = pendingTransaction.Id
        };
    }
}
