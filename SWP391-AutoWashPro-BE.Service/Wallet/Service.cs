using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;
using SePayOptions = SWP391_AutoWashPro_BE.Service.SePay.Options;

namespace SWP391_AutoWashPro_BE.Service.Wallet;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly SePayOptions _sePayOptions;

    public Service(
        AppDbContext dbContext,
        IHttpContextAccessor httpContext,
        IOptions<SePayOptions> sePayOptions)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _sePayOptions = sePayOptions.Value;
    }

    public async Task<Response.GetWalleResponse> GetUserWallet()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new KeyNotFoundException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new KeyNotFoundException("Customer profile not found");

        var query = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        
        if (query == null)
        {
            throw new KeyNotFoundException("Wallet not found");
        }
        
        var result = new Response.GetWalleResponse
        {
            Id = query.Id,
            Balance = query.Balance,
            Currency = "VND"
        };
        return result;
    }

    public async Task<Response.WalletTopupResponse> TopupUserWallet(Request.WalletTopupRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
           .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
            throw new KeyNotFoundException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new KeyNotFoundException("Customer profile not found");

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        if (wallet == null)
        {
            throw new KeyNotFoundException("Wallet not found");
        }
        wallet.Balance += request.Balance;

        var topUpTransaction = new Repository.Entities.Transaction
        {
            Amount = request.Balance,
            Type = Repository.Enums.TransactionType.WalletTopup,
            Description = "Wallet top-up",
            TransactionDate = DateTime.UtcNow,
            CustomerId = customerProfile.Id,
            CustomerProfile = customerProfile,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        await _dbContext.Transactions.AddAsync(topUpTransaction);
        await _dbContext.SaveChangesAsync();

        var result = new Response.WalletTopupResponse
        {
            Id = wallet.Id,
            Balance = wallet.Balance,
            Message = "Wallet topped up successfully"
        };
        return result;
    }

    public async Task<Response.GetWalleResponse> GetUserWalletV2()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var userQuery = _dbContext.Users
            .Where(user => user.Id == userIdGuid);

        var user = await userQuery
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var customerProfileQuery = _dbContext.CustomerProfiles
            .Where(customerProfile => customerProfile.UserId == userIdGuid);

        var customerProfile = await customerProfileQuery
            .FirstOrDefaultAsync();

        if (customerProfile == null)
        {
            throw new InvalidOperationException("Customer profile not found");
        }

        var walletQuery = _dbContext.Wallets
            .Where(wallet => wallet.CustomerId == customerProfile.Id);

        var wallet = await walletQuery
            .FirstOrDefaultAsync();

        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found");
        }

        var result = new Response.GetWalleResponse
        {
            Id = wallet.Id,
            Balance = wallet.Balance,
            Currency = "VND"
        };

        return result;
    }

    public async Task<Response.WalletTopupV2Response> TopupUserWalletV2(Request.WalletTopupRequest request)
    {
        if (request.Balance <= 0)
        {
            throw new ArgumentException("Balance must be greater than 0.");
        }

        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var userQuery = _dbContext.Users
            .Where(user => user.Id == userIdGuid);

        var user = await userQuery
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var customerProfileQuery = _dbContext.CustomerProfiles
            .Where(customerProfile => customerProfile.UserId == userIdGuid);

        var customerProfile = await customerProfileQuery
            .FirstOrDefaultAsync();

        if (customerProfile == null)
        {
            throw new InvalidOperationException("Customer profile not found");
        }

        var walletQuery = _dbContext.Wallets
            .Where(wallet => wallet.CustomerId == customerProfile.Id);

        var wallet = await walletQuery
            .FirstOrDefaultAsync();

        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found");
        }

        if (string.IsNullOrWhiteSpace(_sePayOptions.BankName))
        {
            throw new InvalidOperationException("SePay BankName is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_sePayOptions.BankAccount))
        {
            throw new InvalidOperationException("SePay BankAccount is not configured.");
        }

        var transferContentPrefix = string.IsNullOrWhiteSpace(_sePayOptions.TransferContentPrefix)
            ? "TOPUP"
            : _sePayOptions.TransferContentPrefix.Trim().ToUpperInvariant();
        var qrBaseUrl = string.IsNullOrWhiteSpace(_sePayOptions.QrBaseUrl)
            ? "https://vietqr.app/img"
            : _sePayOptions.QrBaseUrl.Trim();
        var qrTemplate = string.IsNullOrWhiteSpace(_sePayOptions.QrTemplate)
            ? "compact"
            : _sePayOptions.QrTemplate.Trim();
        var referenceCode = $"{transferContentPrefix}-{Guid.NewGuid():N}";

        var now = DateTimeOffset.UtcNow;
        var topUpTransaction = new Repository.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Balance,
            Type = TransactionType.WalletTopup,
            Description = "Wallet top-up",
            TransactionDate = DateTime.UtcNow,
            CustomerId = customerProfile.Id,
            CustomerProfile = customerProfile,
            Status = TransactionStatus.Pending,
            ReferenceCode = referenceCode,
            Provider = ProviderType.SePay,
            TransferType = Repository.Enums.TransferType.In,
            Gateway = "SePay",
            AccountNumber = _sePayOptions.BankAccount,
            RawContent = referenceCode,
            ProviderDescription = referenceCode,
            WalletBalanceBefore = wallet.Balance,
            ExpiredAt = now.AddMinutes(15),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _dbContext.Transactions.AddAsync(topUpTransaction);
        await _dbContext.SaveChangesAsync();

        var qrCode = $"{qrBaseUrl}?" +
                     $"bank={Uri.EscapeDataString(_sePayOptions.BankName)}&" +
                     $"acc={Uri.EscapeDataString(_sePayOptions.BankAccount)}&" +
                     $"amount={(int)request.Balance}&" +
                     $"des={Uri.EscapeDataString(referenceCode)}&" +
                     $"template={Uri.EscapeDataString(qrTemplate)}&" +
                     $"showinfo={_sePayOptions.QrShowInfo.ToString().ToLowerInvariant()}";

        if (_sePayOptions.IncludeAccountHolderInQr &&
            !string.IsNullOrWhiteSpace(_sePayOptions.AccountHolder))
        {
            qrCode += $"&holder={Uri.EscapeDataString(_sePayOptions.AccountHolder.Trim())}";
        }

        var result = new Response.WalletTopupV2Response
        {
            TransactionId = topUpTransaction.Id,
            Amount = request.Balance,
            Currency = Response.WalletCurrency,
            BankName = _sePayOptions.BankName,
            BankAccount = _sePayOptions.BankAccount,
            ReferenceCode = referenceCode,
            Description = referenceCode,
            QRCode = qrCode,
            Status = TransactionStatus.Pending.ToString(),
            ExpiredAt = topUpTransaction.ExpiredAt ?? now.AddMinutes(15),
            Message = "Create wallet top-up request successfully"
        };

        return result;
    }

    public async Task<Response.WalletTopupStatusResponse> GetWalletTopupStatus(Guid transactionId)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var userQuery = _dbContext.Users
            .Where(user => user.Id == userIdGuid);

        var user = await userQuery
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var customerProfileQuery = _dbContext.CustomerProfiles
            .Where(customerProfile => customerProfile.UserId == userIdGuid);

        var customerProfile = await customerProfileQuery
            .FirstOrDefaultAsync();

        if (customerProfile == null)
        {
            throw new InvalidOperationException("Customer profile not found");
        }

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.Id == transactionId &&
                transaction.CustomerId == customerProfile.Id &&
                transaction.Type == TransactionType.WalletTopup);

        var selectedQuery = query
            .Select(transaction => new Response.WalletTopupStatusResponse
            {
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                Currency = Response.WalletCurrency,
                ReferenceCode = transaction.ReferenceCode ?? string.Empty,
                Status = transaction.Status != null
                    ? transaction.Status.ToString()!
                    : string.Empty,
                CreatedAt = transaction.CreatedAt,
                ExpiredAt = transaction.ExpiredAt,
                PaidAt = transaction.PaidAt,
                ExternalTransactionId = transaction.ExternalTransactionId,
                BankReferenceCode = transaction.BankReferenceCode
            });

        var result = await selectedQuery
            .FirstOrDefaultAsync();

        if (result == null)
        {
            throw new KeyNotFoundException("Wallet top-up transaction not found");
        }

        return result;
    }
}
