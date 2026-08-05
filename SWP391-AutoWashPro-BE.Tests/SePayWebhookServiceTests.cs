using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SePayRequest = SWP391_AutoWashPro_BE.Service.SePay.Request;
using SePayService = SWP391_AutoWashPro_BE.Service.SePay.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class SePayWebhookServiceTests
{
    [Fact]
    public async Task SePayWebhook_ShouldProcess_WhenContentContainsReferenceWithExtraSuffix()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser();
        var tier = CreateTier();
        var customerProfile = CreateCustomerProfile(user, tier);
        var wallet = CreateWallet(customerProfile, 10_000m);
        var transaction = CreatePendingTopupTransaction(customerProfile, wallet.Balance, 5_000m, "TOPUP-cbcf7e37311d4fb7a771fccfef3376c2");

        dbContext.AddRange(user, tier, customerProfile, wallet, transaction);
        await dbContext.SaveChangesAsync();

        var service = new SePayService(dbContext, NullLogger<SePayService>.Instance);
        var request = new SePayRequest.SePayWebhookRequest
        {
            Id = 71479992,
            Gateway = "TPBank",
            TransactionDate = "2026-08-04 08:51:05",
            AccountNumber = "00005668350",
            Content = "TOPUPcbcf7e37311d4fb7a771fccfef3376c2-040826-08:51:04 6216ASCB02TCMQCX",
            TransferType = "in",
            Description = "BankAPINotify TOPUPcbcf7e37311d4fb7a771fccfef3376c2-040826-08:51:04 6216ASCB02TCMQCX",
            TransferAmount = 5_000,
            ReferenceCode = "267V602262160310",
            Accumulated = 184515
        };

        var result = await service.SePayWebhook(request);

        Assert.True(result.Success);
        Assert.Equal("processed", result.Code);
        Assert.Equal(transaction.Id, result.TransactionId);
        Assert.False(result.AlreadyProcessed);
        Assert.Equal(TransactionStatus.Succeeded.ToString(), result.TransactionStatus);

        var savedWallet = await dbContext.Wallets
            .FirstAsync(walletItem => walletItem.Id == wallet.Id);
        var savedTransaction = await dbContext.Transactions
            .FirstAsync(transactionItem => transactionItem.Id == transaction.Id);

        Assert.Equal(15_000m, savedWallet.Balance);
        Assert.Equal(TransactionStatus.Succeeded, savedTransaction.Status);
        Assert.Equal("71479992", savedTransaction.ExternalTransactionId);
        Assert.Equal("267V602262160310", savedTransaction.BankReferenceCode);
    }

    [Fact]
    public async Task SePayWebhook_ShouldReturnDuplicate_WhenExternalTransactionIdAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser();
        var tier = CreateTier();
        var customerProfile = CreateCustomerProfile(user, tier);
        var wallet = CreateWallet(customerProfile, 15_000m);
        var transaction = CreateSucceededTopupTransaction(customerProfile, 10_000m, 5_000m, "TOPUP-duplicate-case", "71774458");

        dbContext.AddRange(user, tier, customerProfile, wallet, transaction);
        await dbContext.SaveChangesAsync();

        var service = new SePayService(dbContext, NullLogger<SePayService>.Instance);
        var request = new SePayRequest.SePayWebhookRequest
        {
            Id = 71774458,
            Gateway = "TPBank",
            TransactionDate = "2026-08-05 19:11:21",
            AccountNumber = "00005668350",
            Content = "TOPUPduplicatecase-050826-19:11:20 6217ASCB02TZMTUU",
            TransferType = "in",
            Description = "BankAPINotify TOPUPduplicatecase-050826-19:11:20 6217ASCB02TZMTUU",
            TransferAmount = 5_000,
            ReferenceCode = "267V602262171436",
            Accumulated = 59083
        };

        var result = await service.SePayWebhook(request);

        Assert.True(result.Success);
        Assert.Equal("duplicate", result.Code);
        Assert.Equal(transaction.Id, result.TransactionId);
        Assert.True(result.AlreadyProcessed);
        Assert.Equal(TransactionStatus.Succeeded.ToString(), result.TransactionStatus);

        var savedWallet = await dbContext.Wallets
            .FirstAsync(walletItem => walletItem.Id == wallet.Id);
        var savedTransaction = await dbContext.Transactions
            .FirstAsync(transactionItem => transactionItem.Id == transaction.Id);

        Assert.Equal(15_000m, savedWallet.Balance);
        Assert.Equal(TransactionStatus.Succeeded, savedTransaction.Status);
        Assert.Equal("71774458", savedTransaction.ExternalTransactionId);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "customer@example.com",
        Phone = "0900000001",
        PasswordHash = "hashed-password",
        Role = UserRole.Customer,
        Status = AccountStatus.Active,
        isVerify = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Tier CreateTier() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Member",
        Level = 1,
        RequiredWashes = 0,
        PriorityBookingDays = 0,
        IsDeleted = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static CustomerProfile CreateCustomerProfile(User user, Tier tier) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        User = user,
        TierId = tier.Id,
        Tier = tier,
        FirstName = "Auto",
        LastName = "Wash",
        TotalPoints = 0,
        TotalWashes = 0,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Wallet CreateWallet(CustomerProfile customerProfile, decimal balance) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerProfile.Id,
        Customer = customerProfile,
        Balance = balance,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Transaction CreatePendingTopupTransaction(
        CustomerProfile customerProfile,
        decimal walletBalanceBefore,
        decimal amount,
        string referenceCode) => new()
    {
        Id = Guid.NewGuid(),
        Amount = amount,
        Type = TransactionType.WalletTopup,
        Description = "Wallet top-up",
        TransactionDate = DateTime.UtcNow,
        CustomerId = customerProfile.Id,
        CustomerProfile = customerProfile,
        Status = TransactionStatus.Pending,
        ReferenceCode = referenceCode,
        Provider = ProviderType.SePay,
        TransferType = TransferType.In,
        Gateway = "SePay",
        AccountNumber = "00005668350",
        RawContent = referenceCode,
        ProviderDescription = referenceCode,
        WalletBalanceBefore = walletBalanceBefore,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static Transaction CreateSucceededTopupTransaction(
        CustomerProfile customerProfile,
        decimal walletBalanceBefore,
        decimal amount,
        string referenceCode,
        string externalTransactionId) => new()
    {
        Id = Guid.NewGuid(),
        Amount = amount,
        Type = TransactionType.WalletTopup,
        Description = "Wallet top-up",
        TransactionDate = DateTime.UtcNow,
        CustomerId = customerProfile.Id,
        CustomerProfile = customerProfile,
        Status = TransactionStatus.Succeeded,
        ReferenceCode = referenceCode,
        Provider = ProviderType.SePay,
        ExternalTransactionId = externalTransactionId,
        TransferType = TransferType.In,
        Gateway = "TPBank",
        AccountNumber = "00005668350",
        RawContent = referenceCode,
        ProviderDescription = referenceCode,
        WalletBalanceBefore = walletBalanceBefore,
        WalletBalanceAfter = walletBalanceBefore + amount,
        PaidAt = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
