using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Wallet;
using WalletService = SWP391_AutoWashPro_BE.Service.Wallet.Service;
using SePayOptions = SWP391_AutoWashPro_BE.Service.SePay.Options;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class WalletTopupFlowTests
{
    [Fact]
    public async Task TopupUserWalletV2_ShouldReturnExpiredAt_AndCreatePendingTransaction()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser();
        var tier = CreateTier();
        var customerProfile = CreateCustomerProfile(user, tier);
        var wallet = CreateWallet(customerProfile, 10_000m);

        dbContext.AddRange(user, tier, customerProfile, wallet);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, user.Id);
        var request = new Request.WalletTopupRequest
        {
            Balance = 5_000m
        };

        var startedAt = DateTimeOffset.UtcNow;
        var result = await service.TopupUserWalletV2(request);
        var finishedAt = DateTimeOffset.UtcNow;

        Assert.Equal(5_000m, result.Amount);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(result.ReferenceCode, result.Description);
        Assert.True(result.ExpiredAt > finishedAt.AddMinutes(14));
        Assert.True(result.ExpiredAt <= startedAt.AddMinutes(16));

        var transactionQuery = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Id == result.TransactionId);

        var transaction = await transactionQuery
            .FirstOrDefaultAsync();

        Assert.NotNull(transaction);
        Assert.Equal(TransactionStatus.Pending, transaction!.Status);
        Assert.Equal(result.ReferenceCode, transaction.ReferenceCode);
        Assert.Equal(result.ExpiredAt, transaction.ExpiredAt);
    }

    [Fact]
    public async Task GetWalletTopupStatus_ShouldReturnStatus_ForCurrentCustomerTransaction()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser();
        var tier = CreateTier();
        var customerProfile = CreateCustomerProfile(user, tier);
        var wallet = CreateWallet(customerProfile, 15_000m);
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 5_000m,
            Type = TransactionType.WalletTopup,
            Description = "Wallet top-up",
            TransactionDate = DateTime.UtcNow,
            CustomerId = customerProfile.Id,
            CustomerProfile = customerProfile,
            Status = TransactionStatus.Succeeded,
            ReferenceCode = "TOPUP-87ab7c599cdf4dc097902250e118c49c",
            Provider = ProviderType.SePay,
            ExternalTransactionId = "71795086",
            TransferType = TransferType.In,
            Gateway = "TPBank",
            AccountNumber = "00005668350",
            BankReferenceCode = "267V602262171625",
            PaidAt = DateTimeOffset.UtcNow,
            ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AddRange(user, tier, customerProfile, wallet, transaction);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, user.Id);

        var result = await service.GetWalletTopupStatus(transaction.Id);

        Assert.Equal(transaction.Id, result.TransactionId);
        Assert.Equal(5_000m, result.Amount);
        Assert.Equal("Succeeded", result.Status);
        Assert.Equal(transaction.ReferenceCode, result.ReferenceCode);
        Assert.Equal("71795086", result.ExternalTransactionId);
        Assert.Equal("267V602262171625", result.BankReferenceCode);
        Assert.Equal(transaction.ExpiredAt, result.ExpiredAt);
        Assert.Equal(transaction.PaidAt, result.PaidAt);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static WalletService CreateService(AppDbContext dbContext, Guid userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "TestAuth");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        var sePayOptions = Options.Create(new SePayOptions
        {
            BankName = "TPBank",
            BankAccount = "00005668350",
            TransferContentPrefix = "TOPUP"
        });

        return new WalletService(dbContext, httpContextAccessor, sePayOptions);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "wallet@example.com",
        Phone = "0900000011",
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
        FirstName = "Wallet",
        LastName = "Tester",
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
}
