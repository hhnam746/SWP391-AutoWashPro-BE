using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;

namespace SWP391_AutoWashPro_BE.Service.Promotion;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    public Service(AppDbContext context)
    {
        _dbContext = context;
    }


    public async Task<Base.Response.PageResult<Response.PromotionResponse>> GetPromotion(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.Promotions.Where(x => true);

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
            IsActive = x.IsActive
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

        var newPromotion = new Repository.Entities.Promotion()
        {
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
        await _dbContext.SaveChangesAsync();

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

        _dbContext.Promotions.Remove(promotion);

        await _dbContext.SaveChangesAsync();

        return "Deleted promotion successfully";
    }
}