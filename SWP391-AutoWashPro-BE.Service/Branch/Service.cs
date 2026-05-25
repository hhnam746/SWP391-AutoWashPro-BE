using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Branch;

public class Service : IService
{
    
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    
    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }
    
    public async Task<Response.GetBranchesResponse> GetBranches(string? keyword, bool? IsActive)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");

        var query = _dbContext.Branches.Where(x => true);
        if (keyword != null)
        {
            query = query.Where(x => x.Name.ToLower().Contains(keyword.ToLower()));
        }

        if (IsActive != null)
        {
            query = query.Where(x => x.IsActive == IsActive);
        }

        var selectedQuery = query.Select(x => new Response.BranchItem
        {
            Id = x.Id,
            Name = x.Name,
            Address = x.Address,
            IsActive = x.IsActive
        });
        
        var result = new Response.GetBranchesResponse()
        {
            Data = await selectedQuery.ToListAsync(),
        };
        return result;
    }

    public async Task<Response.GetTiersResponse> GetTiers()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        var query = _dbContext.Tiers.Where(x => true);
        var selectedQuery = query.Select(x => new Response.TierItem
        {
            Id = x.Id,
            Name = x.Name,
            Level = x.Level,
            RequiredWashes = x.RequiredWashes,
            PriorityBookingDays = x.PriorityBookingDays,
            Description = x.Description
        });
        var result = new Response.GetTiersResponse()
        {
            Data = await selectedQuery.ToListAsync(),
        };
        return result;
    }

    public async Task<Response.GetUserAvailablePromotion> GetPromotions()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");

        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);

        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        // Lấy promotion theo tier
        var promotionIds = await _dbContext.PromotionTiers
            .Where(x => x.TierId == customerProfile.TierId)
            .Select(x => x.PromotionId)
            .ToListAsync();

        var tierPromotions = await _dbContext.Promotions
            .Where(x => promotionIds.Contains(x.Id))
            .ToListAsync();

        // Lấy promotion global
        var globalPromotions = await _dbContext.Promotions
            .Where(x => x.IsGlobal == true)
            .ToListAsync();

        // Gộp lại
        globalPromotions.AddRange(tierPromotions);

        // Remove duplicate
        var promotions = globalPromotions.DistinctBy(x => x.Id).ToList();

        return new Response.GetUserAvailablePromotion
        {
            data = promotions.Select(x => new Response.PromotionInfor
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                DiscountType = x.DiscountType,
                discountValue = x.DiscountValue,
                endTime = x.EndDate
            }).ToList()
        };
    }

    public async Task<Response.GetRewardsResponse> GetRewards()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);

        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        var rewards = await _dbContext.Rewards
            .Include(x => x.RewardTiers)
            .ThenInclude(x => x.Tier)
            .ToListAsync();

        var response = new Response.GetRewardsResponse
        {
            Data = rewards.Select(x => new Response.RewardItem
            {
                Id = x.Id,
                Name = x.Name,
                RewardType = x.RewardType,
                PointsRequired = x.PointsRequired,
                QuantityAvailable = x.QuantityAvailable,
                ValidDays = x.ValidDays,
                Description = x.Description,

                IsRedeemable = x.QuantityAvailable > 0 &&
                               customerProfile.TotalPoints >= x.PointsRequired &&
                               x.RewardTiers.Any(rt => rt.TierId == customerProfile.TierId),

                AllowedTiers = x.RewardTiers
                    .Select(rt => new Response.AllowedTierItem
                    {
                        Id = rt.Tier.Id,
                        Name = rt.Tier.Name
                    })
                    .ToList()

            }).ToList()
        };

        return response;
    }
}