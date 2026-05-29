using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;

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
        
        var query = _dbContext.Tiers.Where(x => true);
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
        var exist = await _dbContext.Tiers
            .AnyAsync(x => x.Name == request.Name);

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
        var tier = await _dbContext.Tiers
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tier == null)
        {
            throw new Exception("Tier not found");
        }

        tier.Name = request.Name;
        tier.Level = request.Level;
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
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tier == null)
        {
            throw new Exception("Tier not found");
        }

        tier.IsDeleted = true;
        tier.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Tier disabled successfully";
    }
}