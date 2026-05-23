using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.User;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<Response.ProfileResponse> GetProfile()
    {
        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.CustomerProfile)
            .ThenInclude(x => x!.Tier)
            .FirstOrDefaultAsync(x => x.Id == userIdGuid &&
                                      x.isVerify &&
                                      x.Role == UserRole.Customer);

        if (existingUser == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (existingUser.CustomerProfile == null)
        {
            throw new KeyNotFoundException("Customer profile not found.");
        }

        var customerProfile = existingUser.CustomerProfile;

        var response = new Response.ProfileResponse()
        {
            Id = existingUser.Id,
            Email = existingUser.Email ?? string.Empty,
            Phone = existingUser.Phone,
            Role = existingUser.Role,
            Status = existingUser.Status,
            ProfileData = new Response.ProfileData
            {
                Id = customerProfile.Id,
                FirstName = customerProfile.FirstName,
                LastName = customerProfile.LastName,
                Cccd = customerProfile.Cccd,
                TierData = customerProfile!.Tier == null
                    ? null
                    : new Response.TierData
                    {
                        Id = customerProfile.Tier.Id,
                        Name = customerProfile.Tier.Name,
                        Level = customerProfile.Tier.Level
                    }
            },
            LastPointActivityAt = customerProfile.LastPointActivityAt,
            TotalPoints = customerProfile.TotalPoints,
            TotalWashes = customerProfile.TotalWashes
        };

        return response;
    }
}
