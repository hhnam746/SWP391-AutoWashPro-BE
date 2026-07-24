using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using RewardRequest = SWP391_AutoWashPro_BE.Service.Reward.Request;
using RewardService = SWP391_AutoWashPro_BE.Service.Reward.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class RewardServiceTests
{
    [Fact]
    public async Task GetAllReward_ExcludesDeletedTierIdsAndReturnsDiscountFields()
    {
        await using var dbContext = CreateDbContext();
        var activeTier = CreateTier("Member", 1, isDeleted: false);
        var deletedTier = CreateTier("Silver", 2, isDeleted: true);
        var reward = CreateReward();

        dbContext.AddRange(activeTier, deletedTier, reward);
        dbContext.RewardTiers.AddRange(
            CreateRewardTier(reward, activeTier),
            CreateRewardTier(reward, deletedTier),
            CreateRewardTier(reward, CreateTier("Gold", 3, isDeleted: false), isDeleted: true));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetAllReward(null, 50, 1);

        var response = Assert.Single(result.Items);
        Assert.Equal(DiscountType.Percentage, response.DiscountType);
        Assert.Equal(10m, response.DiscountValue);
        Assert.Equal([activeTier.Id], response.TierIds);
    }

    [Fact]
    public async Task UpdateReward_SoftDeletesOldTierLinkAndPreservesOmittedFields()
    {
        await using var dbContext = CreateDbContext();
        var activeTier = CreateTier("Member", 1, isDeleted: false);
        var deletedTier = CreateTier("Silver", 2, isDeleted: true);
        var reward = CreateReward();

        dbContext.AddRange(activeTier, deletedTier, reward);
        dbContext.RewardTiers.Add(CreateRewardTier(reward, deletedTier));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.UpdateReward(reward.Id, new RewardRequest.UpdateRewardRequest
        {
            Name = "Updated reward",
            TierIds = [activeTier.Id]
        });

        await dbContext.Entry(reward).ReloadAsync();
        var rewardTiers = await dbContext.RewardTiers
            .Where(rewardTier => rewardTier.RewardId == reward.Id)
            .ToListAsync();

        Assert.Equal("Updated reward", reward.Name);
        Assert.Equal(RewardType.Voucher, reward.RewardType);
        Assert.Equal(DiscountType.Percentage, reward.DiscountType);
        Assert.Equal(10m, reward.DiscountValue);
        Assert.Equal(2, rewardTiers.Count);
        Assert.False(Assert.Single(rewardTiers, item => item.TierId == activeTier.Id).IsDeleted);
        Assert.True(Assert.Single(rewardTiers, item => item.TierId == deletedTier.Id).IsDeleted);
    }

    [Fact]
    public async Task UpdateReward_ReselectingTier_ReactivatesExistingLink()
    {
        await using var dbContext = CreateDbContext();
        var firstTier = CreateTier("Member", 1, isDeleted: false);
        var secondTier = CreateTier("Silver", 2, isDeleted: false);
        var reward = CreateReward();

        dbContext.AddRange(firstTier, secondTier, reward);
        dbContext.RewardTiers.Add(CreateRewardTier(reward, firstTier));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.UpdateReward(reward.Id, new RewardRequest.UpdateRewardRequest
        {
            TierIds = [secondTier.Id]
        });
        await service.UpdateReward(reward.Id, new RewardRequest.UpdateRewardRequest
        {
            TierIds = [firstTier.Id]
        });

        var rewardTiers = await dbContext.RewardTiers
            .Where(rewardTier => rewardTier.RewardId == reward.Id)
            .ToListAsync();

        Assert.Equal(2, rewardTiers.Count);
        Assert.False(Assert.Single(rewardTiers, item => item.TierId == firstTier.Id).IsDeleted);
        Assert.True(Assert.Single(rewardTiers, item => item.TierId == secondTier.Id).IsDeleted);
    }

    [Fact]
    public async Task UpdateReward_WithDeletedTier_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var deletedTier = CreateTier("Silver", 2, isDeleted: true);
        var reward = CreateReward();

        dbContext.AddRange(deletedTier, reward);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateReward(reward.Id, new RewardRequest.UpdateRewardRequest
            {
                TierIds = [deletedTier.Id]
            }));

        Assert.Equal("One or more selected tiers are invalid or have been deleted.", exception.Message);
        Assert.Empty(await dbContext.RewardTiers.ToListAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static RewardService CreateService(AppDbContext dbContext) =>
        new(dbContext, null!, new HttpContextAccessor());

    private static Tier CreateTier(string name, int level, bool isDeleted) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Level = level,
        RequiredWashes = 0,
        PriorityBookingDays = 0,
        IsDeleted = isDeleted,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Reward CreateReward() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Reward",
        RewardType = RewardType.Voucher,
        PointsRequired = 60,
        QuantityAvailable = 3,
        ValidDays = 40,
        Description = "Description",
        DiscountType = DiscountType.Percentage,
        DiscountValue = 10m,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static RewardTier CreateRewardTier(Reward reward, Tier tier, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        RewardId = reward.Id,
        Reward = reward,
        TierId = tier.Id,
        Tier = tier,
        IsDeleted = isDeleted,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
