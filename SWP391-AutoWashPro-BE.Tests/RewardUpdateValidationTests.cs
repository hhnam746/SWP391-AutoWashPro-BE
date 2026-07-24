using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using RewardService = SWP391_AutoWashPro_BE.Service.Reward.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class RewardUpdateValidationTests
{
    // [Fact]
    // public async Task UpdateReward_WhenDiscountValueIsMissingForPut_ThrowsFullReplaceMessage()
    // {
    //     await using var dbContext = CreateDbContext();
    //     var tier = await SeedTierAsync(dbContext);
    //     var reward = await SeedRewardAsync(dbContext, tier.Id);
    //     var service = CreateRewardService(dbContext);
    //
    //     var request = CreateRequest(tier.Id);
    //     request.DiscountValue = 0;
    //
    //     var exception = await Assert.ThrowsAsync<Exception>(() => service.UpdateReward(reward.Id, request));
    //
    //     Assert.Equal(
    //         "Discount value is required for full-replace PUT updates and must be greater than 0.",
    //         exception.Message);
    // }

    // [Theory]
    // [InlineData(true)]
    // [InlineData(false)]
    // // public async Task CreateAndUpdateReward_WhenPercentageDiscountIs100_ThrowsConsistentMessage(bool isUpdate)
    // // {
    // //     await using var dbContext = CreateDbContext();
    // //     var tier = await SeedTierAsync(dbContext);
    // //     var reward = await SeedRewardAsync(dbContext, tier.Id);
    // //     var service = CreateRewardService(dbContext);
    // //
    // //     var request = CreateRequest(tier.Id);
    // //     request.Name = isUpdate ? reward.Name : $"Reward {Guid.NewGuid():N}";
    // //     request.DiscountType = DiscountType.Percentage;
    // //     request.DiscountValue = 100;
    // //
    // //     var exception = isUpdate
    // //         ? await Assert.ThrowsAsync<Exception>(() => service.UpdateReward(reward.Id, request))
    // //         : await Assert.ThrowsAsync<Exception>(() => service.CreateReward(request));
    // //
    // //     Assert.Equal("Percentage discount must be less than 100.", exception.Message);
    // // }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static RewardService CreateRewardService(AppDbContext dbContext)
    {
        return new RewardService(
            dbContext,
            new FakeNotificationService(),
            new HttpContextAccessor());
    }

    private static async Task<Tier> SeedTierAsync(AppDbContext dbContext)
    {
        var tier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Gold",
            Level = 1,
            RequiredWashes = 0,
            PriorityBookingDays = 5,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Tiers.Add(tier);
        await dbContext.SaveChangesAsync();

        return tier;
    }

    private static async Task<Reward> SeedRewardAsync(AppDbContext dbContext, Guid tierId)
    {
        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = $"Reward {Guid.NewGuid():N}",
            RewardType = RewardType.Voucher,
            PointsRequired = 100,
            QuantityAvailable = 5,
            ValidDays = 30,
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 50000,
            Description = "Seed reward",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Rewards.Add(reward);
        dbContext.RewardTiers.Add(new RewardTier
        {
            Id = Guid.NewGuid(),
            RewardId = reward.Id,
            TierId = tierId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return reward;
    }

    private static SWP391_AutoWashPro_BE.Service.Reward.Request.RewardRequest CreateRequest(Guid tierId)
    {
        return new SWP391_AutoWashPro_BE.Service.Reward.Request.RewardRequest
        {
            Name = $"Updated Reward {Guid.NewGuid():N}",
            RewardType = RewardType.Voucher,
            PointsRequired = 200,
            QuantityAvailable = 10,
            ValidDays = 45,
            Description = "Updated reward",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 25000,
            IsActive = true,
            TierIds = [tierId]
        };
    }

    private sealed class FakeNotificationService : SWP391_AutoWashPro_BE.Service.Notification.IService
    {
        public Task<SWP391_AutoWashPro_BE.Service.Notification.Response.GetNotificationResponse> GetNotification(
            NotificationType? type,
            bool? isRead,
            int page,
            int pageSize)
        {
            throw new NotSupportedException();
        }

        public Task<SWP391_AutoWashPro_BE.Service.Notification.Response.UpdateNotificationStatusResponse> UpdateNotificationStatus(
            SWP391_AutoWashPro_BE.Service.Notification.Request.UpdateNotificationStatusRequest request)
        {
            throw new NotSupportedException();
        }

        public Task SendNotification(SWP391_AutoWashPro_BE.Service.Notification.Request.SendNotificationRequest request)
        {
            return Task.CompletedTask;
        }

        public Task SendNotificationToUser(
            Guid userId,
            Guid notificationId,
            NotificationType type,
            string title,
            string content,
            string? metadata,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
