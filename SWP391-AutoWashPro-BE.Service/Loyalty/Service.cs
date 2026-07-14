using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Service.Loyalty;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<Response.LoyaltyMeResponse> GetMyLoyaltyOverview()
    {
        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId &&
                                      x.Role == UserRole.Customer &&
                                      x.Status == AccountStatus.Active);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var customer = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
        {
            throw new KeyNotFoundException("Customer profile not found.");
        }

        var nextTier = await _dbContext.Tiers
            .AsNoTracking()
            .Where(x => x.Level > customer.Tier.Level)
            .OrderBy(x => x.Level)
            .FirstOrDefaultAsync();

        var benefits = await _dbContext.RewardTiers
            .AsNoTracking()
            .Where(x => x.TierId == customer.TierId)
            .Select(x => x.Reward.Name)
            .Distinct()
            .ToListAsync();

        var response = new Response.LoyaltyMeResponse()
        {
            CustomerId = customer.Id,
            TotalPoints = customer.TotalPoints,
            TotalWashes = customer.TotalWashes,
            LastPointActivityAt = customer.LastPointActivityAt,
            CurrentTier = customer!.Tier == null
                ? null
                : new Response.TierInfo()
                {
                    Id = customer.Tier.Id,
                    Name = customer.Tier.Name,
                    Level = customer.Tier.Level,
                    Description = customer.Tier.Description,
                    PriorityBookingDays = customer.Tier.PriorityBookingDays,
                    RequiredWashes = customer.Tier.RequiredWashes,
                },
            NextTier = nextTier == null
                ? null
                : new Response.NextTierInfo()
                {
                    Id = nextTier.Id,
                    Name = nextTier.Name,
                    Level = nextTier.Level,
                    RequiredWashes = nextTier.RequiredWashes,
                    RemainingWashes = Math.Max(nextTier.RequiredWashes - customer.TotalWashes, 0)
                },
            Benefits = benefits
        };

        return response;
    }

    public async Task<Response.GetPointTransactionsResponse> GetPointTransactions(Request.GetPointTransactionsRequest request)
    {

        if (request.Page <= 0)
        {
            throw new ArgumentException("page must be greater than 0.");
        }

        if (request.PageSize <= 0)
        {
            throw new ArgumentException("pageSize must be greater than 0.");
        }

        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Role
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (user.Role != UserRole.Customer)
        {
            throw new ForbiddenAccessException("Only customer accounts can access this resource.");
        }

        var customer = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id
            })
            .FirstOrDefaultAsync();

        if (customer == null)
        {
            throw new KeyNotFoundException("Customer profile not found.");
        }

        var query = _dbContext.PointTransactions
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Id);

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.TransactionType == request.Type.Value);
        }

        var totalCount = await query.CountAsync();

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new Response.PointTransactionItem
            {
                Id = x.Id,
                Type = x.TransactionType.ToString().ToLowerInvariant(),
                Points = x.Points,
                Description = x.Description,
                BookingId = x.BookingId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new Response.GetPointTransactionsResponse
        {
            Data = data,
            Pagination = new Response.Pagination
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            }
        };
    }
    
    public async Task<List<Response.ConfigResponse>> GetAllConfigs()
    {
        var query = _dbContext.SystemConfigs.Where(x => true);

        var selected = query.Select(x => new Response.ConfigResponse()
        {
            ConfigKey = x.ConfigKey,
            ConfigValue = x.ConfigValue,
            Description = x.Description
        });

        var result = await selected.ToListAsync();
        
        return result;

    }

    public async Task<string> UpdateConfig(Request.ConfigRequest request)
    {
        var config = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == request.ConfigKey);

        if (config == null)
        {
            throw new Exception("Config not found");
        }

        var configValue = request.ConfigValue.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => request.ConfigValue.GetString(),
            System.Text.Json.JsonValueKind.Number => request.ConfigValue.GetRawText(),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(configValue))
        {
            throw new Exception("Config value must be a string or number");
        }

        config.ConfigValue = configValue;
        config.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Updated Config successfully";
    }
}
