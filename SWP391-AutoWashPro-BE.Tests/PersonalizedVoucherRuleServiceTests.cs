using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Constants;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class PersonalizedVoucherRuleServiceTests
{
    [Fact]
    public async Task CreateRule_RejectsSecondActiveRuleForSameTrigger()
    {
        await using var dbContext = await CreateDbContextAsync();
        var service = CreateRuleService(dbContext);
        await service.CreateRuleAsync(CreateBirthdayRuleRequest("Birthday Voucher"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRuleAsync(CreateBirthdayRuleRequest("Another Birthday Voucher")));

        Assert.Contains("active personalized voucher rule", exception.Message);
        Assert.Equal(1, await dbContext.PersonalizedVoucherRules.CountAsync());
    }

    [Fact]
    public async Task UpdateStatus_RejectsActivationWhenTriggerAlreadyHasActiveRule()
    {
        await using var dbContext = await CreateDbContextAsync();
        var service = CreateRuleService(dbContext);
        await service.CreateRuleAsync(CreateBirthdayRuleRequest("Active Birthday Voucher"));
        var inactiveRequest = CreateBirthdayRuleRequest("Inactive Birthday Voucher");
        inactiveRequest.IsActive = false;
        var inactiveRule = await service.CreateRuleAsync(inactiveRequest);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRuleStatusAsync(
                inactiveRule.Id,
                new Request.UpdateRuleStatusRequest { IsActive = true }));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("\"invalid\"")]
    public async Task TriggerConfig_FailsClosedForDisabledOrInvalidValue(string configValue)
    {
        await using var dbContext = await CreateDbContextAsync();
        var config = await dbContext.SystemConfigs.SingleAsync(x =>
            x.ConfigKey == PersonalizedVoucherConfigKeys.Birthday);
        config.ConfigValue = configValue;
        await dbContext.SaveChangesAsync();
        var service = new TriggerConfigService(
            dbContext,
            NullLogger<TriggerConfigService>.Instance);

        Assert.False(await service.IsEnabledAsync(PersonalizedVoucherTriggerType.Birthday));
    }

    [Fact]
    public async Task TriggerConfig_FailsClosedWhenKeyIsMissing()
    {
        await using var dbContext = await CreateDbContextAsync();
        var config = await dbContext.SystemConfigs.SingleAsync(x =>
            x.ConfigKey == PersonalizedVoucherConfigKeys.Birthday);
        dbContext.SystemConfigs.Remove(config);
        await dbContext.SaveChangesAsync();
        var service = new TriggerConfigService(
            dbContext,
            NullLogger<TriggerConfigService>.Instance);

        Assert.False(await service.IsEnabledAsync(PersonalizedVoucherTriggerType.Birthday));
    }

    [Fact]
    public async Task SeededTriggerConfigs_EnableAllSupportedTriggers()
    {
        await using var dbContext = await CreateDbContextAsync();
        var service = new TriggerConfigService(
            dbContext,
            NullLogger<TriggerConfigService>.Instance);

        foreach (var triggerType in Enum.GetValues<PersonalizedVoucherTriggerType>())
        {
            Assert.True(await service.IsEnabledAsync(triggerType));
        }
    }

    private static RuleService CreateRuleService(AppDbContext dbContext)
    {
        return new RuleService(
            dbContext,
            Microsoft.Extensions.Options.Options.Create(new Options
            {
                TimeZoneId = "Asia/Ho_Chi_Minh"
            }));
    }

    private static Request.RuleRequest CreateBirthdayRuleRequest(string voucherName)
    {
        return new Request.RuleRequest
        {
            VoucherName = voucherName,
            TriggerType = PersonalizedVoucherTriggerType.Birthday,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15,
            VoucherValidityDays = 14,
            IsActive = true
        };
    }

    private static async Task<AppDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }
}
