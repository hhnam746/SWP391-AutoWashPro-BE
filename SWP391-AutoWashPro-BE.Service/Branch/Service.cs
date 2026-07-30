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

        var query = _dbContext.Branches.Where(x => !x.IsDeleted);
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
        var query = _dbContext.Tiers.Where(x => x.IsDeleted == false);
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

        // Kiểm tra user
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Lấy customer profile
        var customerProfile = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);

        if (customerProfile == null)
            throw new KeyNotFoundException("Customer profile not found");

        var nowUtc = DateTimeOffset.UtcNow;

        // Lấy ID promotion theo tier của customer
        var promotionIds = await _dbContext.PromotionTiers
            .AsNoTracking()
            .Where(x =>
                x.TierId == customerProfile.TierId &&
                !x.IsDeleted &&
                !x.Tier.IsDeleted &&
                !x.Promotion.IsDeleted)
            .Select(x => x.PromotionId)
            .ToListAsync();

        // Lấy promotion theo tier
        var tierPromotions = await _dbContext.Promotions
            .AsNoTracking()
            .Where(x =>
                promotionIds.Contains(x.Id) &&
                !x.IsDeleted &&
                x.IsActive &&
                x.StartDate <= nowUtc &&
                x.EndDate > nowUtc)
            .ToListAsync();

        // Lấy global promotion
        var globalPromotions = await _dbContext.Promotions
            .AsNoTracking()
            .Where(x =>
                x.IsGlobal == true &&
                !x.IsDeleted &&
                x.IsActive &&
                x.StartDate <= nowUtc &&
                x.EndDate > nowUtc)
            .ToListAsync();

        // Gộp hai danh sách
        globalPromotions.AddRange(tierPromotions);

        // Loại bỏ promotion bị trùng
        var promotions = globalPromotions
            .DistinctBy(x => x.Id)
            .ToList();

        return new Response.GetUserAvailablePromotion
        {
            data = promotions.Select(x => new Response.PromotionInfor
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description ?? string.Empty,
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
            .Where(x => x.IsActive)
            .Include(x => x.RewardTiers
                .Where(rt => !rt.IsDeleted && !rt.Tier.IsDeleted))
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
                               x.RewardTiers.Any(rt =>
                                   rt.TierId == customerProfile.TierId &&
                                   !rt.IsDeleted &&
                                   !rt.Tier.IsDeleted),

                AllowedTiers = x.RewardTiers
                    .Where(rt => !rt.IsDeleted && !rt.Tier.IsDeleted)
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
    
    public async Task<Base.Response.PageResult<Response.BranchItem>> GetAllBranches(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.Branches.Where(x => true);
        if (searchTerm != null)
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        query = query.OrderBy(x => x.Name);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selected = query.Select(x => new Response.BranchItem()
        {
            Id = x.Id,
            Name = x.Name,
            Address = x.Address,
            IsActive = x.IsActive,
        });
          
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.BranchItem>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        
        return result;
        

    }

    public async Task<string> CreateBranch(Request.BranchRequest request)
    {
        var existing = await _dbContext.Branches
            .AnyAsync(x => x.Name == request.Name);

        if (existing)
        {
            throw new Exception("Branch already exists");
        }

        var newBranch = new Repository.Entities.Branch()
        {
            Name = request.Name,
            Address = request.Address,
            IsActive = true
        };
        _dbContext.Branches.Add(newBranch);
        await _dbContext.SaveChangesAsync();

        return "Branch created successfully";
        
    }

    public async Task<string> UpdateBranch(Guid id,Request.BranchRequest request)
    {
        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == id);

        if (branch == null)
        {
            throw new Exception("Branch not found");
        }

        branch.Name = request.Name;
        branch.Address = request.Address;
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        
        return "Branch updated successfully";
    }

    public async Task<string> DeleteBranch(Guid id)
    {
        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == id);

        if (branch == null)
        {
            throw new Exception("Branch not found");
        }

        branch.IsActive = false;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
         
        return "Branch deleted successfully";
    }
}
