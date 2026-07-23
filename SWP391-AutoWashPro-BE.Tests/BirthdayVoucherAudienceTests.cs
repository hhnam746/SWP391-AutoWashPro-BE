using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;
using Xunit;
using PersonalizedVoucherService = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Service;
using PersonalizedVoucherOptions = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Options;

namespace SWP391_AutoWashPro_BE.Tests;

public class BirthdayVoucherAudienceTests
{
    [Fact]
    public async Task ProcessBirthdayAsync_MatchingCustomer_IssuesOnceAndDispatchesVoucher()
    {
        await using var dbContext = CreateDbContext();
        var localToday = GetLocalToday();
        var seed = await SeedAsync(dbContext, new DateOnly(1995, localToday.Month, localToday.Day));
        var deliveryService = new RecordingDeliveryService();
        var audienceService = CreateAudienceService(dbContext, deliveryService);

        var firstProcessed = await audienceService.ProcessBirthdayAsync();
        var secondProcessed = await audienceService.ProcessBirthdayAsync();

        Assert.Equal(1, firstProcessed);
        Assert.Equal(0, secondProcessed);

        var issuance = await dbContext.PersonalizedVoucherIssuances.SingleAsync();
        var voucher = await dbContext.Vouchers.SingleAsync();
        Assert.Equal(seed.CustomerId, issuance.CustomerId);
        Assert.Equal(PersonalizedVoucherTriggerType.Birthday, issuance.TriggerType);
        Assert.Equal($"BIRTHDAY:{localToday.Year}", issuance.CycleKey);
        Assert.Equal(localToday.ToString("yyyy-MM-dd"), issuance.TriggerReference);
        Assert.Equal(issuance.VoucherId, voucher.Id);
        Assert.Equal(seed.RuleId, issuance.VoucherRuleId);
        Assert.Equal("Birthday Test Voucher", voucher.Name);
        Assert.Equal(new[] { issuance.Id }, deliveryService.DispatchedIssuanceIds);
    }

    [Fact]
    public async Task ProcessBirthdayAsync_NonMatchingBirthday_DoesNotIssueVoucher()
    {
        await using var dbContext = CreateDbContext();
        var localToday = GetLocalToday();
        await SeedAsync(dbContext, new DateOnly(1995, localToday.Month, localToday.AddDays(1).Day));
        var deliveryService = new RecordingDeliveryService();

        var processed = await CreateAudienceService(dbContext, deliveryService).ProcessBirthdayAsync();

        Assert.Equal(0, processed);
        Assert.Empty(dbContext.PersonalizedVoucherIssuances);
        Assert.Empty(deliveryService.DispatchedIssuanceIds);
    }

    [Fact]
    public async Task ProcessBirthdayAsync_CustomersAcrossTiersReceiveTheSameTriggerVoucher()
    {
        await using var dbContext = CreateDbContext();
        var localToday = GetLocalToday();
        await SeedAsync(dbContext, new DateOnly(1995, localToday.Month, localToday.Day));
        var nowUtc = DateTimeOffset.UtcNow;
        var secondTier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Different Birthday Tier",
            Level = 2,
            RequiredWashes = 20,
            PriorityBookingDays = 2,
            CreatedAt = nowUtc
        };
        var secondUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"birthday-tier-{Guid.NewGuid():N}@example.test",
            Phone = $"08{Random.Shared.NextInt64(10000000, 99999999)}",
            PasswordHash = "test-password-hash",
            Role = UserRole.Customer,
            Status = AccountStatus.Active,
            isVerify = true,
            CreatedAt = nowUtc
        };
        var secondCustomer = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = secondUser.Id,
            User = secondUser,
            TierId = secondTier.Id,
            Tier = secondTier,
            FirstName = "Other",
            LastName = "Tier",
            DateOfBirth = new DateOnly(1990, localToday.Month, localToday.Day),
            DateOfBirthSetAt = nowUtc,
            CreatedAt = nowUtc
        };
        dbContext.AddRange(secondTier, secondUser, secondCustomer);
        await dbContext.SaveChangesAsync();

        var processed = await CreateAudienceService(
            dbContext,
            new RecordingDeliveryService()).ProcessBirthdayAsync();

        Assert.Equal(2, processed);
        Assert.Equal(2, await dbContext.Vouchers.CountAsync());
        Assert.Equal(2, await dbContext.PersonalizedVoucherIssuances.CountAsync());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ProcessBirthdayAsync_IneligibleRuleOrCustomer_DoesNotIssueVoucher(
        bool ruleIsActive,
        bool customerIsVerified)
    {
        await using var dbContext = CreateDbContext();
        var localToday = GetLocalToday();
        await SeedAsync(
            dbContext,
            new DateOnly(1995, localToday.Month, localToday.Day),
            ruleIsActive,
            customerIsVerified);
        var deliveryService = new RecordingDeliveryService();

        var processed = await CreateAudienceService(dbContext, deliveryService).ProcessBirthdayAsync();

        Assert.Equal(0, processed);
        Assert.Empty(dbContext.PersonalizedVoucherIssuances);
        Assert.Empty(deliveryService.DispatchedIssuanceIds);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static AudienceService CreateAudienceService(
        AppDbContext dbContext,
        IDeliveryService deliveryService)
    {
        var triggerConfigService = new TriggerConfigService(
            dbContext,
            NullLogger<TriggerConfigService>.Instance);
        var issuanceService = new PersonalizedVoucherService(
            dbContext,
            triggerConfigService,
            NullLogger<PersonalizedVoucherService>.Instance);
        var options = Microsoft.Extensions.Options.Options.Create(new PersonalizedVoucherOptions
        {
            TimeZoneId = "Asia/Ho_Chi_Minh",
            BatchSize = 10
        });

        return new AudienceService(
            dbContext,
            issuanceService,
            deliveryService,
            triggerConfigService,
            options,
            NullLogger<AudienceService>.Instance);
    }

    private static async Task<(Guid CustomerId, Guid RuleId)> SeedAsync(
        AppDbContext dbContext,
        DateOnly dateOfBirth,
        bool ruleIsActive = true,
        bool customerIsVerified = true)
    {
        await dbContext.Database.EnsureCreatedAsync();
        var nowUtc = DateTimeOffset.UtcNow;
        var tier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Birthday Test Tier",
            Level = 1,
            RequiredWashes = 0,
            PriorityBookingDays = 1,
            CreatedAt = nowUtc
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"birthday-{Guid.NewGuid():N}@example.test",
            Phone = $"09{Random.Shared.NextInt64(10000000, 99999999)}",
            PasswordHash = "test-password-hash",
            Role = UserRole.Customer,
            Status = AccountStatus.Active,
            isVerify = customerIsVerified,
            CreatedAt = nowUtc
        };
        var customer = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TierId = tier.Id,
            Tier = tier,
            FirstName = "Birthday",
            LastName = "Customer",
            DateOfBirth = dateOfBirth,
            DateOfBirthSetAt = nowUtc,
            CreatedAt = nowUtc
        };
        var rule = new PersonalizedVoucherRule
        {
            Id = Guid.NewGuid(),
            VoucherName = "Birthday Test Voucher",
            TriggerType = PersonalizedVoucherTriggerType.Birthday,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15,
            VoucherValidityDays = 14,
            IsActive = ruleIsActive,
            SendInAppNotification = false,
            SendEmail = false,
            CreatedAt = nowUtc
        };

        dbContext.AddRange(tier, user, customer, rule);
        await dbContext.SaveChangesAsync();
        return (customer.Id, rule.Id);
    }

    private static DateOnly GetLocalToday() => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Ho_Chi_Minh").DateTime);

    private sealed class RecordingDeliveryService : IDeliveryService
    {
        public List<Guid> DispatchedIssuanceIds { get; } = [];

        public Task DispatchAsync(Guid issuanceId, CancellationToken cancellationToken = default)
        {
            DispatchedIssuanceIds.Add(issuanceId);
            return Task.CompletedTask;
        }

        public Task<int> RetryPendingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
