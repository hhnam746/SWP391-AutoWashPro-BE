using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
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
}