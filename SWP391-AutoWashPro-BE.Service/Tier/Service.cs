using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;

namespace SWP391_AutoWashPro_BE.Service.Tier;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Base.Response.PageResult<Response.TierResponse>> GetAllTier(string? searchTerm, int pageSize, int pageIndex)
    {
        
        var query = _dbContext.Tiers.Where(x => x.IsDeleted == false);
        if (searchTerm != null)
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        query = query.OrderBy(x => x.Name);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selected = query.Select(x => new Response.TierResponse()
        {
            Id = x.Id,
            Name = x.Name,
            Level =  x.Level,
            RequiredWashes =  x.RequiredWashes,
            PriorityBookingDays =   x.PriorityBookingDays,
            Description =   x.Description,
        });
          
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.TierResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        
        return result;
    }

    public async Task<string> CreateTier(Request.TierRequest request)
    {
        // var exist = await _dbContext.Tiers
        //     .AnyAsync(x => x.Name == request.Name);
        var exist = await _dbContext.Tiers
            .AnyAsync(x =>
                !x.IsDeleted &&
                (x.Name == request.Name || x.Level == request.Level));

        if (exist)
        {
            throw new Exception("Tier already exists");
        }

        var newTier = new Repository.Entities.Tier
        {
            Name = request.Name,
            Level = request.Level,
            RequiredWashes = request.RequiredWashes,
            PriorityBookingDays = request.PriorityBookingDays,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Tiers.Add(newTier);
        await _dbContext.SaveChangesAsync();

        return "Tier created successfully";
    }

    public async Task<string> UpdateTier(Guid id, Request.TierRequest request)
    {
        var exist = await _dbContext.Tiers.AnyAsync(x =>
            x.Id != id &&
            !x.IsDeleted &&
            x.Name == request.Name);

        if (exist)
        {
            throw new Exception("Tier already exists");
        }
        
        var tier = await _dbContext.Tiers
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        // if (tier == null)
        // {
        //     throw new KeyNotFoundException("Tier not found");
        // }

        if (tier == null)
        {
            throw new KeyNotFoundException("Tier not found");
        }

        // Chỉ kiểm tra khi thay đổi điều kiện lên hạng
        if (tier.RequiredWashes != request.RequiredWashes)
        {
            // Không cho sửa nếu đã có khách hàng thuộc tier này
            var hasCustomer = await _dbContext.CustomerProfiles
                .AnyAsync(x => x.TierId == id);

            if (hasCustomer)
            {
                throw new InvalidOperationException(
                    "Cannot change the minimum required washes because customers are already assigned to this tier.");
            }

            // Lấy hạng thấp hơn gần nhất
            var lowerTier = await _dbContext.Tiers
                .Where(x => !x.IsDeleted && x.Level < tier.Level)
                .OrderByDescending(x => x.Level)
                .FirstOrDefaultAsync();

            if (lowerTier != null &&
                request.RequiredWashes <= lowerTier.RequiredWashes)
            {
                throw new InvalidOperationException(
                    $"Required washes must be greater than {lowerTier.Name}.");
            }

            // Lấy hạng cao hơn gần nhất
            var upperTier = await _dbContext.Tiers
                .Where(x => !x.IsDeleted && x.Level > tier.Level)
                .OrderBy(x => x.Level)
                .FirstOrDefaultAsync();

            if (upperTier != null &&
                request.RequiredWashes >= upperTier.RequiredWashes)
            {
                throw new InvalidOperationException(
                    $"Required washes must be less than {upperTier.Name}.");
            }
        }

        tier.Name = request.Name;
        // tier.Level = request.Level;
        tier.RequiredWashes = request.RequiredWashes;
        tier.PriorityBookingDays = request.PriorityBookingDays;
        tier.Description = request.Description;
        tier.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Tier updated successfully";
    }

    public async Task<string> DeleteTier(Guid id)
    {
        var tier = await _dbContext.Tiers
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (tier == null)
        {
            throw new KeyNotFoundException("Tier not found");
        }
        var assignedCustomers = await _dbContext.CustomerProfiles
            .CountAsync(cp => cp.TierId == id);

        if (assignedCustomers > 0)
        {
            throw new InvalidOperationException(
                "Cannot delete this tier because customers are currently assigned to it. Please assign them to another tier before deleting."
            );
        }

        var isUsedByActiveReward = await _dbContext.RewardTiers.AnyAsync(rewardTier =>
            rewardTier.TierId == id &&
            !rewardTier.IsDeleted &&
            rewardTier.Reward.IsActive);
        var isUsedByActivePromotion = await _dbContext.PromotionTiers.AnyAsync(promotionTier =>
            promotionTier.TierId == id &&
            !promotionTier.IsDeleted &&
            promotionTier.Promotion.IsActive &&
            promotionTier.Promotion.IsGlobal == false &&
            !promotionTier.Promotion.IsDeleted);

        if (isUsedByActiveReward || isUsedByActivePromotion)
        {
            throw new InvalidOperationException(
                "Cannot delete this tier because active rewards or promotions are using it. Please reassign them to another tier before deleting."
            );
        }

        tier.IsDeleted = true;
        tier.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Tier disabled successfully";
    }
}
