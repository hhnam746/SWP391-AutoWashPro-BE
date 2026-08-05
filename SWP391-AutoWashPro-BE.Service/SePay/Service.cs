using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.SePay;

public class Service : IService
{
    private const string SecretHeaderName = "X-SePay-Secret";
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string SignatureHeaderName = "X-SePay-Signature";

    private readonly AppDbContext _dbContext;
    private readonly ILogger<Service> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Options _options;

    public Service(
        AppDbContext dbContext,
        ILogger<Service> logger,
        IHttpContextAccessor httpContextAccessor,
        IOptions<Options> options)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
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
        var utcNow = DateTimeOffset.UtcNow;

        ValidateWebhookAuthentication(rawPayload);

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
                Message = "Only transferType 'in' is supported.",
                AlreadyProcessed = false
            };
        }

        if (!string.IsNullOrWhiteSpace(_options.BankAccount) &&
            !string.Equals(
                request.AccountNumber?.Trim(),
                _options.BankAccount.Trim(),
                StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Ignored SePay webhook because AccountNumber={AccountNumber} did not match configured account. WebhookId={WebhookId}.",
                request.AccountNumber,
                request.Id);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "ignored",
                Message = "Webhook account number is not allowed.",
                AlreadyProcessed = false
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
                TransactionId = duplicateTransaction.Id,
                AlreadyProcessed = true,
                TransactionStatus = TransactionStatus.Succeeded.ToString()
            };
        }

        var paymentCode = ExtractPaymentCode(
            normalizedContent,
            request.Description);

        if (paymentCode == null)
        {
            _logger.LogWarning(
                "Ignored SePay webhook because no payment code could be extracted. WebhookId={WebhookId}, Content={Content}.",
                request.Id,
                normalizedContent);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "ignored",
                Message = "No payment reference code was found in the webhook content.",
                AlreadyProcessed = false
            };
        }

        var pendingTransactionQuery = _dbContext.Transactions
            .Where(transaction =>
                transaction.Type == TransactionType.WalletTopup &&
                transaction.ReferenceCode == paymentCode);

        var pendingTransaction = await pendingTransactionQuery
            .FirstOrDefaultAsync();

        if (pendingTransaction == null)
        {
            _logger.LogWarning(
                "Ignored SePay webhook because no wallet top-up was found for ReferenceCode={ReferenceCode}. WebhookId={WebhookId}. Description={Description}.",
                paymentCode,
                request.Id,
                request.Description);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "ignored",
                Message = "No pending wallet top-up transaction matched the webhook reference.",
                AlreadyProcessed = false
            };
        }

        if (pendingTransaction.Status != TransactionStatus.Pending)
        {
            _logger.LogInformation(
                "Ignored SePay webhook because transaction was no longer pending. TransactionId={TransactionId}, Status={Status}, WebhookId={WebhookId}.",
                pendingTransaction.Id,
                pendingTransaction.Status,
                request.Id);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "ignored",
                Message = "Wallet top-up transaction is no longer pending.",
                TransactionId = pendingTransaction.Id,
                AlreadyProcessed = false,
                TransactionStatus = pendingTransaction.Status?.ToString()
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
                TransactionId = pendingTransaction.Id,
                AlreadyProcessed = false,
                TransactionStatus = pendingTransaction.Status?.ToString()
            };
        }

        if (pendingTransaction.ExpiredAt.HasValue &&
            pendingTransaction.ExpiredAt.Value <= utcNow)
        {
            pendingTransaction.Status = TransactionStatus.Expired;
            pendingTransaction.UpdatedAt = utcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogWarning(
                "Ignored SePay webhook because transaction expired. TransactionId={TransactionId}, ExpiredAt={ExpiredAt}, WebhookId={WebhookId}.",
                pendingTransaction.Id,
                pendingTransaction.ExpiredAt,
                request.Id);

            return new Response.SePayWebhookResponse
            {
                Success = true,
                Code = "expired",
                Message = "Wallet top-up transaction has expired.",
                TransactionId = pendingTransaction.Id,
                AlreadyProcessed = false,
                TransactionStatus = pendingTransaction.Status?.ToString()
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

        var paidAt = providerTransactionDate ?? utcNow;
        var walletBalanceBefore = wallet.Balance;
        var walletBalanceAfter = walletBalanceBefore + pendingTransaction.Amount;

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            wallet.Balance = walletBalanceAfter;
            wallet.UpdatedAt = utcNow;

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
            pendingTransaction.UpdatedAt = utcNow;

            await _dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();

            var duplicatedWebhookQuery = _dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.ExternalTransactionId == externalTransactionId);

            var duplicatedWebhook = await duplicatedWebhookQuery
                .Select(transaction => new
                {
                    transaction.Id
                })
                .FirstOrDefaultAsync();

            if (duplicatedWebhook != null)
            {
                return new Response.SePayWebhookResponse
                {
                    Success = true,
                    Code = "duplicate",
                    Message = "Webhook was already processed.",
                    TransactionId = duplicatedWebhook.Id,
                    AlreadyProcessed = true,
                    TransactionStatus = TransactionStatus.Succeeded.ToString()
                };
            }

            throw;
        }

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
            TransactionId = pendingTransaction.Id,
            AlreadyProcessed = false,
            TransactionStatus = pendingTransaction.Status.ToString()
        };
    }

    private void ValidateWebhookAuthentication(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new UnauthorizedAccessException("Webhook context was not found.");

        if (_options.UseHmacSignature)
        {
            var providedSignature = GetHeaderValue(httpContext, SignatureHeaderName);

            if (string.IsNullOrWhiteSpace(providedSignature))
            {
                throw new UnauthorizedAccessException("Webhook signature header is required.");
            }

            var expectedSignature = ComputeSignature(rawPayload, _options.SecretKey);

            if (!FixedTimeEquals(providedSignature, expectedSignature))
            {
                throw new UnauthorizedAccessException("Webhook signature is invalid.");
            }

            return;
        }

        return;
    }

    private static string? GetHeaderValue(HttpContext httpContext, string headerName)
    {
        if (!httpContext.Request.Headers.TryGetValue(headerName, out var values))
        {
            return null;
        }

        return values.FirstOrDefault()?.Trim();
    }

    private static string? GetBearerToken(HttpContext httpContext)
    {
        var authorizationHeader = GetHeaderValue(httpContext, "Authorization");

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorizationHeader["Bearer ".Length..].Trim();
    }

    private string? ExtractPaymentCode(string content, string? description)
    {
        var paymentCode = ExtractPaymentCode(content);

        if (paymentCode != null)
        {
            return paymentCode;
        }

        return string.IsNullOrWhiteSpace(description)
            ? null
            : ExtractPaymentCode(description);
    }

    private string? ExtractPaymentCode(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var transferContentPrefix = string.IsNullOrWhiteSpace(_options.TransferContentPrefix)
            ? "TOPUP"
            : NormalizePaymentCode(_options.TransferContentPrefix);
        var normalizedSource = NormalizePaymentCode(source);
        var paymentCodeRegex = new Regex(
            $@"{Regex.Escape(transferContentPrefix)}[A-F0-9]{{32}}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var match = paymentCodeRegex.Match(normalizedSource);

        if (!match.Success)
        {
            return null;
        }

        var rawCode = match.Value;
        return $"{transferContentPrefix}-{rawCode[transferContentPrefix.Length..].ToLowerInvariant()}";
    }

    private static string NormalizePaymentCode(string value)
    {
        var buffer = new char[value.Length];
        var length = 0;

        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character))
            {
                continue;
            }

            buffer[length] = char.ToUpperInvariant(character);
            length++;
        }

        return new string(buffer, 0, length);
    }

    private static string ComputeSignature(string payload, string secretKey)
    {
        var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretKeyBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left.Trim());
        var rightBytes = Encoding.UTF8.GetBytes(right.Trim());

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
