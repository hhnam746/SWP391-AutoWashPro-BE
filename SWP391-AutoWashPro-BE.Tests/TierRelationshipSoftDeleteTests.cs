using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using PromotionRequest = SWP391_AutoWashPro_BE.Service.Promotion.Request;
using PromotionService = SWP391_AutoWashPro_BE.Service.Promotion.Service;
using TierService = SWP391_AutoWashPro_BE.Service.Tier.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class TierRelationshipSoftDeleteTests
{
    [Fact]
    public async Task UpdatePromotion_SoftDeletesAndReactivatesExistingTierLinks()
    {
        await using var dbContext = CreateDbContext();
        var firstTier = CreateTier("Member", 1);
        var secondTier = CreateTier("Silver", 2);
        var promotion = CreatePromotion();

        dbContext.AddRange(firstTier, secondTier, promotion);
        dbContext.PromotionTiers.Add(CreatePromotionTier(promotion, firstTier));
        await dbContext.SaveChangesAsync();

        var service = new PromotionService(dbContext, null!);
        await service.UpdatePromotion(promotion.Id, new PromotionRequest.UpdatePromotionRequest
        {
            TierIds = [secondTier.Id]
        });
        await service.UpdatePromotion(promotion.Id, new PromotionRequest.UpdatePromotionRequest
        {
            TierIds = [firstTier.Id]
        });

        var promotionTiers = await dbContext.PromotionTiers
            .Where(item => item.PromotionId == promotion.Id)
            .ToListAsync();
        var response = Assert.Single((await service.GetPromotion(null, 50, 1)).Items);

        Assert.Equal(2, promotionTiers.Count);
        Assert.False(Assert.Single(promotionTiers, item => item.TierId == firstTier.Id).IsDeleted);
        Assert.True(Assert.Single(promotionTiers, item => item.TierId == secondTier.Id).IsDeleted);
        Assert.Equal([firstTier.Id], response.TierIds);
    }

    [Fact]
    public async Task UpdatePromotion_ChangingToGlobalWithoutTierIds_SoftDeletesExistingTierLinks()
    {
        await using var dbContext = CreateDbContext();
        var tier = CreateTier("Member", 1);
        var promotion = CreatePromotion();
        var promotionTier = CreatePromotionTier(promotion, tier);

        dbContext.AddRange(tier, promotion, promotionTier);
        await dbContext.SaveChangesAsync();

        await new PromotionService(dbContext, null!).UpdatePromotion(
            promotion.Id,
            new PromotionRequest.UpdatePromotionRequest { IsGlobal = true });

        Assert.True(promotion.IsGlobal);
        Assert.True(promotionTier.IsDeleted);
    }

    [Fact]
    public async Task DeleteTier_WhenActiveRewardUsesTier_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var tier = CreateTier("Member", 1);
        var reward = CreateReward(isActive: true);
        dbContext.AddRange(tier, reward);
        dbContext.RewardTiers.Add(CreateRewardTier(reward, tier));
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TierService(dbContext).DeleteTier(tier.Id));

        Assert.Contains("active rewards or promotions", exception.Message);
        Assert.False(tier.IsDeleted);
    }

    [Fact]
    public async Task DeleteTier_WhenActivePromotionUsesTier_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var tier = CreateTier("Member", 1);
        var promotion = CreatePromotion(isActive: true);
        dbContext.AddRange(tier, promotion);
        dbContext.PromotionTiers.Add(CreatePromotionTier(promotion, tier));
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TierService(dbContext).DeleteTier(tier.Id));

        Assert.Contains("active rewards or promotions", exception.Message);
        Assert.False(tier.IsDeleted);
    }

    [Fact]
    public async Task DeleteTier_WhenOnlyInactiveOrHistoricalLinksRemain_SoftDeletesTier()
    {
        await using var dbContext = CreateDbContext();
        var tier = CreateTier("Member", 1);
        var reward = CreateReward(isActive: false);
        var promotion = CreatePromotion(isActive: true);
        promotion.IsGlobal = true;
        var rewardTier = CreateRewardTier(reward, tier);
        var promotionTier = CreatePromotionTier(promotion, tier);

        dbContext.AddRange(tier, reward, promotion, rewardTier, promotionTier);
        await dbContext.SaveChangesAsync();

        await new TierService(dbContext).DeleteTier(tier.Id);

        Assert.True(tier.IsDeleted);
        Assert.False(rewardTier.IsDeleted);
        Assert.False(promotionTier.IsDeleted);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Tier CreateTier(string name, int level) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Level = level,
        RequiredWashes = 0,
        PriorityBookingDays = 0,
        IsDeleted = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Reward CreateReward(bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Reward {Guid.NewGuid():N}",
        RewardType = RewardType.Voucher,
        PointsRequired = 60,
        QuantityAvailable = 3,
        ValidDays = 40,
        DiscountType = DiscountType.Percentage,
        DiscountValue = 10,
        IsActive = isActive,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Promotion CreatePromotion(bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Promotion {Guid.NewGuid():N}",
        DiscountType = DiscountType.Percentage,
        DiscountValue = 10,
        StartDate = DateTimeOffset.UtcNow.AddDays(-1),
        EndDate = DateTimeOffset.UtcNow.AddDays(1),
        IsGlobal = false,
        IsActive = isActive,
        IsDeleted = false,
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

    private static PromotionTier CreatePromotionTier(
        Promotion promotion,
        Tier tier,
        bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        PromotionId = promotion.Id,
        Promotion = promotion,
        TierId = tier.Id,
        Tier = tier,
        IsDeleted = isDeleted,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
