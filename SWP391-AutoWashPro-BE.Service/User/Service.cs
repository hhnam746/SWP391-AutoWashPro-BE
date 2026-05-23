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
    private readonly Security.IService _securityService;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext, Security.IService securityService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _securityService = securityService;
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

    public async Task<string> UpdateProfile(Request.UpdateProfileRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body is required.");
        }

        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var existingUser = await _dbContext.Users
            .Include(x => x.CustomerProfile)
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

        var hasFirstName = !string.IsNullOrWhiteSpace(request.FirstName);
        var hasLastName = !string.IsNullOrWhiteSpace(request.LastName);
        var hasCccd = request.Cccd != null;
        var isUpdated = false;

        if (!hasFirstName && !hasLastName && !hasCccd)
        {
            throw new ArgumentException("At least one field must be provided for update.");
        }

        if (hasFirstName)
        {
            var firstName = request.FirstName!.Trim();
            if (!string.Equals(customerProfile.FirstName, firstName, StringComparison.Ordinal))
            {
                customerProfile.FirstName = firstName;
                isUpdated = true;
            }
        }

        if (hasLastName)
        {
            var lastName = request.LastName!.Trim();
            if (!string.Equals(customerProfile.LastName, lastName, StringComparison.Ordinal))
            {
                customerProfile.LastName = lastName;
                isUpdated = true;
            }
        }

        if (hasCccd)
        {
            var normalizedCccd = string.IsNullOrWhiteSpace(request.Cccd)
                ? null
                : request.Cccd.Trim();
            var currentCccd = string.IsNullOrWhiteSpace(customerProfile.Cccd)
                ? null
                : customerProfile.Cccd.Trim();

            if (!string.Equals(currentCccd, normalizedCccd, StringComparison.Ordinal))
            {
                if (normalizedCccd != null)
                {
                    var isDuplicateCccd = await _dbContext.CustomerProfiles
                        .AnyAsync(x => x.UserId != userIdGuid && x.Cccd == normalizedCccd);

                    if (isDuplicateCccd)
                    {
                        throw new ArgumentException("CCCD already exists.");
                    }
                }

                customerProfile.Cccd = normalizedCccd;
                isUpdated = true;
            }
        }

        if (!isUpdated)
        {
            return "No profile changes detected";
        }

        customerProfile.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (hasCccd &&
                  ex.InnerException?.Message.Contains("IX_customer_profile_cccd", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ArgumentException("CCCD already exists.");
        }

        return "Update customer profile successfully";
    }

    public async Task<string> UpdateProfileByPassword(Request.UpdateProfileByPassword request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("New password is required.");
        }

        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid &&
                                      x.isVerify &&
                                      x.Role == UserRole.Customer);

        if (existingUser == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        existingUser.PasswordHash = _securityService.Hash(request.NewPassword.Trim());
        existingUser.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        return "Update new password successfully";
    }
}
