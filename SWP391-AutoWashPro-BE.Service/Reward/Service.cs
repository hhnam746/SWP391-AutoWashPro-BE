using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Reward;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly Notification.IService _notificationService;
    private readonly IHttpContextAccessor _httpContext;

    public Service(AppDbContext dbContext, Notification.IService notificationService, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _httpContext = httpContext;
    }

    public async Task<Base.Response.PageResult<Response.RewardResponse>> GetAllReward(string? searchTerm, int pageSize,
        int pageIndex)
    {
        var query = _dbContext.Rewards.Where(x =>  x.IsActive == true && x.RewardType == RewardType.Voucher);
        if (searchTerm != null)
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }
        query = query.OrderBy(x => x.Name);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selected = query.Select(x => new Response.RewardResponse()
        {
            Id = x.Id,
            Name = x.Name,
            RewardType = x.RewardType,
            PointsRequired = x.PointsRequired,
            QuantityAvailable = x.QuantityAvailable,
            ValidDays = x.ValidDays,
            Description = x.Description,
            DiscountType = x.DiscountType,
            DiscountValue = x.DiscountValue,
            IsActive = x.IsActive,
            TierIds = x.RewardTiers
                .Where(rt => !rt.Tier.IsDeleted)
                .Select(rt => rt.TierId)
                .ToList()
        });

        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.RewardResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return result;
    }

    public async Task<string> CreateReward(Request.RewardRequest request)
    {
        var exist = await _dbContext.Rewards.AnyAsync(x => x.Name == request.Name);
        if (exist)
        {
            throw new InvalidOperationException("Reward already exists");
        }
        
        
        if (request.TierIds == null || !request.TierIds.Any())
        {
            throw new ArgumentException("Please select at least one tier");
        }
      
        var tierIds = request.TierIds.Distinct().ToList();

        var validTierCount = await _dbContext.Tiers
            .CountAsync(t => tierIds.Contains(t.Id) && !t.IsDeleted);

        if (validTierCount != tierIds.Count)
        {
            throw new ArgumentException("One or more selected tiers are invalid or have been deleted.");
        }

        ValidateRewardValues(
            request.PointsRequired,
            request.QuantityAvailable,
            request.ValidDays,
            request.DiscountType,
            request.DiscountValue);

        var newReward = new Repository.Entities.Reward()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            RewardType = request.RewardType,
            PointsRequired = request.PointsRequired,
            QuantityAvailable = request.QuantityAvailable,
            ValidDays = request.ValidDays,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            Description = request.Description,
            IsActive = true,
        };
        _dbContext.Rewards.Add(newReward);
        foreach (var tierId in tierIds)
        {
            var rewardTier = new RewardTier()
            {
                RewardId = newReward.Id,
                TierId = tierId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.RewardTiers.Add(rewardTier);
        }

        await _dbContext.SaveChangesAsync();

        return "Reward created successfully";
    }

    public async Task<string> UpdateReward(Guid id, Request.UpdateRewardRequest request)
    {
        var reward = await _dbContext.Rewards
            .FirstOrDefaultAsync(x => x.Id == id);

        if (reward == null)
        {
            throw new KeyNotFoundException("Reward not found");
        }

        if (request.Name != null)
        {
            var exist = await _dbContext.Rewards.AnyAsync(x =>
                x.Id != id &&
                x.Name == request.Name);

            if (exist)
            {
                throw new InvalidOperationException("Reward already exists");
            }

            reward.Name = request.Name;
        }

        if (request.RewardType.HasValue)
            reward.RewardType = request.RewardType.Value;
        if (request.PointsRequired.HasValue)
            reward.PointsRequired = request.PointsRequired.Value;
        if (request.QuantityAvailable.HasValue)
            reward.QuantityAvailable = request.QuantityAvailable.Value;
        if (request.ValidDays.HasValue)
            reward.ValidDays = request.ValidDays.Value;
        if (request.Description != null)
            reward.Description = request.Description;
        if (request.DiscountType.HasValue)
            reward.DiscountType = request.DiscountType.Value;
        if (request.DiscountValue.HasValue)
            reward.DiscountValue = request.DiscountValue.Value;
        if (request.IsActive.HasValue)
            reward.IsActive = request.IsActive.Value;

        ValidateRewardValues(
            reward.PointsRequired,
            reward.QuantityAvailable,
            reward.ValidDays,
            reward.DiscountType,
            reward.DiscountValue);

        if (request.TierIds != null)
        {
            if (!request.TierIds.Any())
                throw new ArgumentException("Please select at least one tier");

            var tierIds = request.TierIds.Distinct().ToList();
            var validTierCount = await _dbContext.Tiers
                .CountAsync(t => tierIds.Contains(t.Id) && !t.IsDeleted);

            if (validTierCount != tierIds.Count)
                throw new ArgumentException("One or more selected tiers are invalid or have been deleted.");

            var oldRewardTiers = await _dbContext.RewardTiers
                .Where(x => x.RewardId == reward.Id)
                .ToListAsync();

            _dbContext.RewardTiers.RemoveRange(oldRewardTiers);

            foreach (var tierId in tierIds)
            {
                _dbContext.RewardTiers.Add(new RewardTier
                {
                    RewardId = reward.Id,
                    TierId = tierId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        reward.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return "Reward updated successfully";
    }

    private static void ValidateRewardValues(
        int pointsRequired,
        int quantityAvailable,
        int validDays,
        DiscountType discountType,
        decimal discountValue)
    {
        if (pointsRequired <= 0)
            throw new ArgumentException("Points required must be greater than 0.");
        if (quantityAvailable < 0)
            throw new ArgumentException("Quantity available cannot be negative.");
        if (validDays <= 0)
            throw new ArgumentException("Valid days must be greater than 0.");
        if (discountValue <= 0)
            throw new ArgumentException("Discount value must be greater than 0.");
        if (discountType == DiscountType.Percentage && discountValue > 100)
            throw new ArgumentException("Percentage discount cannot exceed 100.");
    }

    public async Task<string> DeleteReward(Guid id)
    {
        var reward = await _dbContext.Rewards
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive == true);

        if (reward == null)
        {
            throw new Exception("Reward not found");
        }

        reward.IsActive = false;
        reward.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Reward deleted successfully";
    }

    public async Task<string> RedeemReward(Guid rewardId)
    {
        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContext);
        
        var customer = await _dbContext.CustomerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
            throw new Exception("Customer not found");

        var reward = await _dbContext.Rewards
            .Include(x => x.RewardTiers)
            .FirstOrDefaultAsync(x => x.Id == rewardId);

        if (reward == null)
            throw new Exception("Reward not found");

        if (!reward.IsActive)
            throw new Exception("Reward is inactive");

        if (reward.QuantityAvailable <= 0)
            throw new Exception("Reward out of stock");

        if (customer.TotalPoints < reward.PointsRequired)
            throw new Exception("Not enough points");

        var canRedeem = reward.RewardTiers
            .Any(x => x.TierId == customer.TierId);

        if (!canRedeem)
            throw new Exception("Your tier cannot redeem this reward");

        customer.TotalPoints -= reward.PointsRequired;

        reward.QuantityAvailable -= 1;

        var pointTransaction = new Repository.Entities.PointTransaction()
        {
            CustomerId = customer.Id,
            RewardId = reward.Id,
            Points = -reward.PointsRequired,
            TransactionType = PointTransactionType.Redeem,
            Description = $"Redeem reward: {reward.Name}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var voucher = new Repository.Entities.Voucher
        {
            CustomerId = customer.Id,
            RewardId = reward.Id,

            Code = Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpper(),

            Status = VoucherStatus.Active,

            DiscountType = reward.DiscountType,
            DiscountValue = reward.DiscountValue,

            ExpiresAt = DateTimeOffset.UtcNow
                .AddDays(reward.ValidDays),

            CreatedAt = DateTimeOffset.UtcNow
        };

        // var notification = new Repository.Entities.Notification
        // {
        //     UserId = id,
        //     Type = NotificationType.RewardRedeemed,
        //     Title = "Reward Redeemed",
        //     CreatedAt = DateTimeOffset.UtcNow
        // };

        _dbContext.PointTransactions.Add(pointTransaction);

        _dbContext.Vouchers.Add(voucher);

        // _dbContext.Notifications.Add(notification);

        await _dbContext.SaveChangesAsync();
        
        
        // await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
        // {
        //     UserId = id,
        //     Type = NotificationType.RewardRedeemed,
        //     Data = $"You have successfully redeemed {reward.Name}. Your voucher code is {voucher.Code}."
        // });
        
        return "Reward redeemed successfully";
    }
}
