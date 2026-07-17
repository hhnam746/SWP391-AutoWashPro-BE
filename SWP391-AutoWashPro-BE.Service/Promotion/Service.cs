using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Promotion;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    private readonly Notification.IService _notificationService;

    
    public Service(AppDbContext context, Notification.IService notificationService)
    {
        _dbContext = context;
        _notificationService = notificationService;
    }


    public async Task<Base.Response.PageResult<Response.PromotionResponse>> GetPromotion(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.Promotions.Where(x => x.IsActive == true && x.IsDeleted == false);

        if (searchTerm != null)
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        query = query.OrderBy(x => x.Name);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selected = query.Select(x => new Response.PromotionResponse()
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            DiscountType = x.DiscountType,
            DiscountValue = x.DiscountValue,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            IsGlobal = x.IsGlobal,
            IsActive = x.IsActive,
            TierIds = x.PromotionTiers
                .Where(pt => !pt.IsDeleted && !pt.Tier.IsDeleted)
                .Select(pt => pt.TierId)
                .ToList()
        });
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.PromotionResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        
        return result;
    }

    public async Task<string> CreatePromotion(Request.PromotionRequest request)
    {
        var exists = await _dbContext.Promotions.AnyAsync(x => x.Name == request.Name);
        
        if (exists)
        {
            throw new Exception("Promotion already exists");
        }
        
        if (request.IsGlobal == false &&
            (request.TierIds == null || !request.TierIds.Any()))
        {
            throw new Exception("Please select at least one tier");
        }
        List<Guid> tierIds = new();
        if (request.IsGlobal == false)
        {
            tierIds = request.TierIds.Distinct().ToList();
            var validTierCount = await _dbContext.Tiers
                .CountAsync(t => tierIds.Contains(t.Id) && !t.IsDeleted);

            if (validTierCount != tierIds.Count)
            {
                throw new Exception("One or more selected tiers are invalid or have been deleted.");
            }
        }
        if (request.DiscountValue <= 0)
            throw new Exception("Discount value must be greater than 0");

        if (request.DiscountType == DiscountType.Percentage && request.DiscountValue >= 100)
            throw new Exception("Percentage discount must be less than 100");

        var newPromotion = new Repository.Entities.Promotion()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsGlobal = request.IsGlobal,
            IsActive = true,
        };
        await _dbContext.Promotions.AddAsync(newPromotion);
        

        if (request.IsGlobal == false)
        {
            foreach (var tierId in tierIds)
            {
                await _dbContext.PromotionTiers.AddAsync(new PromotionTier
                {
                    PromotionId = newPromotion.Id,
                    TierId = tierId,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }
        // if (request.IsGlobal == false)
        // {
        //     foreach (var tierId in request.TierIds)
        //     {
        //        await  _dbContext.PromotionTiers.AddAsync(new PromotionTier()
        //         {
        //             PromotionId = newPromotion.Id,
        //             TierId = tierId,
        //             CreatedAt = DateTimeOffset.UtcNow
        //         });
        //     }
        // }
        
        await _dbContext.SaveChangesAsync();
        
        //Add thông báo realtime signalR
        var query = _dbContext.Users.Where(x =>
                                                                x.isVerify == true && 
                                                                x.Status == AccountStatus.Active && 
                                                                x.Role == UserRole.Customer);
        
        var customerIds = await query.Select(x => x.Id).ToListAsync();
        
        
        foreach (var customerId in customerIds)
        {
            await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
            {
                UserId = customerId,
                Type = NotificationType.SystemAlert,
                Data = $"New promotion available: {newPromotion.Name}. Valid until {newPromotion.EndDate:dd/MM/yyyy}."
            });
        }
        
        return "Promotion created successfully";
    }

    public async Task<string> UpdatePromotion(Guid id, Request.UpdatePromotionRequest request)
    {
        var promotion = await _dbContext.Promotions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (promotion == null)
        {
            throw new Exception("Promotion not found");
        }
        
        var exists = await _dbContext.Promotions.AnyAsync(x =>
            x.Id != id &&
            x.Name == request.Name);

        if (exists)
        {
            throw new Exception("Promotion already exists");
        }
        
        if (request.Name != null)
            promotion.Name = request.Name;

        if (request.Description != null)
            promotion.Description = request.Description;

        if (request.DiscountType.HasValue)
            promotion.DiscountType = request.DiscountType.Value;

        if (request.DiscountValue.HasValue)
            promotion.DiscountValue = request.DiscountValue.Value;

        if (request.StartDate.HasValue)
            promotion.StartDate = request.StartDate.Value;

        if (request.EndDate.HasValue)
            promotion.EndDate = request.EndDate.Value;

        if (request.IsGlobal.HasValue)
            promotion.IsGlobal = request.IsGlobal.Value;

        if (request.IsActive.HasValue)
            promotion.IsActive = request.IsActive.Value;

        promotion.UpdatedAt = DateTimeOffset.UtcNow;
        
        if (promotion.DiscountValue <= 0)
            throw new Exception("Discount value must be greater than 0");

        if (promotion.DiscountType == DiscountType.Percentage && promotion.DiscountValue >= 100)
            throw new Exception("Percentage discount must be less than 100");
        if (request.IsGlobal == false && request.TierIds == null)
            throw new ArgumentException("Please select at least one tier when changing to a tier-specific promotion");
        if (promotion.IsGlobal == false && request.TierIds != null && !request.TierIds.Any())
            throw new ArgumentException("Please select at least one tier");

        var shouldSynchronizeTierLinks = request.TierIds != null || request.IsGlobal == true;
        if (shouldSynchronizeTierLinks)
        {
            if (promotion.IsGlobal == false)
            {
                if (!request.TierIds!.Any())
                {
                    throw new ArgumentException("Please select at least one tier");
                }

                var tierIds = request.TierIds!.Distinct().ToHashSet();

                var validTierCount = await _dbContext.Tiers
                    .CountAsync(t => tierIds.Contains(t.Id) && !t.IsDeleted);

                if (validTierCount != tierIds.Count)
                {
                    throw new ArgumentException("One or more selected tiers are invalid or have been deleted.");
                }
            }

            var desiredTierIds = promotion.IsGlobal == false
                ? request.TierIds!.Distinct().ToHashSet()
                : [];
            var existingPromotionTiers = await _dbContext.PromotionTiers
                .Where(x => x.PromotionId == promotion.Id)
                .ToListAsync();

            var nowUtc = DateTimeOffset.UtcNow;
            foreach (var promotionTier in existingPromotionTiers)
            {
                var shouldBeActive = desiredTierIds.Remove(promotionTier.TierId);
                if (promotionTier.IsDeleted == !shouldBeActive)
                    continue;

                promotionTier.IsDeleted = !shouldBeActive;
                promotionTier.UpdatedAt = nowUtc;
            }

            foreach (var tierId in desiredTierIds)
            {
                _dbContext.PromotionTiers.Add(new PromotionTier
                {
                    PromotionId = promotion.Id,
                    TierId = tierId,
                    IsDeleted = false,
                    CreatedAt = nowUtc
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        return "Promotion updated successfully";
    }

    public async Task<string> UpdatePromotionStatus(Guid id, Request.UpdatePromotionStatusRequest request)
    {
        var promotion = await _dbContext.Promotions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (promotion == null)
        {
            throw new Exception("Promotion not found");
        }

        promotion.IsActive = request.IsActive;
        promotion.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Promotion status updated successfully";
    }

    public async Task<string> DeletePromotion(Guid id)
    {
        var promotion = await _dbContext.Promotions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (promotion == null)
        {
            throw new Exception("Promotion not found");
        }

        if (promotion.IsActive)
        {
            throw new Exception("Cannot delete active promotion");
        }

        promotion.IsDeleted = true;

        await _dbContext.SaveChangesAsync();

        return "Deleted promotion successfully";
    }
}
