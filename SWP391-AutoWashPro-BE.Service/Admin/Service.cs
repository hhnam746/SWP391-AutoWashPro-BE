using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.User;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> UpdateUserVerificationStatus(Guid userId)
    {
        var targetUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (targetUser == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (targetUser.Role != UserRole.Customer)
        {
            throw new InvalidOperationException("Only customer accounts can be verified.");
        }

        if (targetUser.Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Only active users can be verified.");
        }

        if (targetUser.isVerify)
        {
            return "User is already verified.";
        }

        targetUser.isVerify = true;
        targetUser.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        return "Verify user successfully";
    }

    public async Task<Base.Response.PageResult<Response.AllProfileResponse>> GetAllUserProfile(
        string? searchTerm, 
        int pageSize, 
        int pageIndex)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(x => x.CustomerProfile)
            .ThenInclude(x => x!.Tier)
            .Where(x => x.isVerify &&
                        x.Role == UserRole.Customer &&
                        x.CustomerProfile != null);
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(x => 
                x.CustomerProfile!.FirstName.Contains(searchTerm) || 
                x.CustomerProfile!.LastName.Contains(searchTerm) || 
                x.Email!.Contains(searchTerm));
        }
        
        var totalItems = await query.CountAsync();
        
        query = query.OrderBy(x => x.CreatedAt);
    
        query = query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);

        var selectedQuery = query.Select(x => new Response.AllProfileResponse()
        {
            Id = x.Id,
            Email = x.Email!,
            Phone = x.Phone,
            Role = x.Role,
            Status = x.Status,
            IsVerified = x.isVerify,
            LastLoginAt  = x.LastLoginAt,
            ProfileData = new Response.ProfileData()
            {
                Id = x.Id,
                FirstName = x.CustomerProfile!.FirstName,
                LastName = x.CustomerProfile.LastName,
                Cccd = x.CustomerProfile.Cccd,
                TotalPoints = x.CustomerProfile.TotalPoints,
                TotalWashes = x.CustomerProfile.TotalWashes,
                TierData = x.CustomerProfile.Tier! == null
                    ? null
                    : new Response.TierData()
                    {
                        Id = x.CustomerProfile.Tier.Id,
                        Name = x.CustomerProfile.Tier.Name,
                        Level = x.CustomerProfile.Tier.Level
                    }
            },
            VehicleCount = x.CustomerProfile.Vehicles.Count(v =>
                v.IsActive &&
                v.DeletedAt == null),
            ActiveBookingCount = x.CustomerProfile.Bookings.Count(b =>
                b.Status == BookingStatus.Pending ||
                b.Status == BookingStatus.Confirmed ||
                b.Status == BookingStatus.CheckIn ||
                b.Status == BookingStatus.InProgress),
            
        });
        
        var listResult = await selectedQuery.ToListAsync();
        
        var result = new Base.Response.PageResult<Response.AllProfileResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };

        return result;
    }

    public async Task<Base.Response.PageResult<Response.AllProfileResponse>> GetUsersNeedVerification(
        string? searchTerm,
        int pageSize,
        int pageIndex)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(x => x.CustomerProfile)
            .ThenInclude(x => x!.Tier)
            .Where(x => !x.isVerify &&
                        x.Role == UserRole.Customer &&
                        x.Status == AccountStatus.Active &&
                        x.CustomerProfile != null);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(x =>
                x.CustomerProfile!.FirstName.Contains(searchTerm) ||
                x.CustomerProfile!.LastName.Contains(searchTerm) ||
                x.Email!.Contains(searchTerm));
        }

        var totalItems = await query.CountAsync();

        query = query.OrderBy(x => x.CreatedAt);

        query = query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);

        var selectedQuery = query.Select(x => new Response.AllProfileResponse()
        {
            Id = x.Id,
            Email = x.Email!,
            Phone = x.Phone,
            Role = x.Role,
            IsVerified = x.isVerify,
            Status = x.Status,
            LastLoginAt = x.LastLoginAt,
            ProfileData = new Response.ProfileData()
            {
                Id = x.Id,
                FirstName = x.CustomerProfile!.FirstName,
                LastName = x.CustomerProfile.LastName,
                Cccd = x.CustomerProfile.Cccd,
                TotalPoints = x.CustomerProfile.TotalPoints,
                TotalWashes = x.CustomerProfile.TotalWashes,
                TierData = x.CustomerProfile.Tier! == null
                    ? null
                    : new Response.TierData()
                    {
                        Id = x.CustomerProfile.Tier.Id,
                        Name = x.CustomerProfile.Tier.Name,
                        Level = x.CustomerProfile.Tier.Level
                    }
            },
            VehicleCount = x.CustomerProfile.Vehicles.Count(v =>
                v.IsActive &&
                v.DeletedAt == null),
            ActiveBookingCount = x.CustomerProfile.Bookings.Count(b =>
                b.Status == BookingStatus.Pending ||
                b.Status == BookingStatus.Confirmed ||
                b.Status == BookingStatus.CheckIn ||
                b.Status == BookingStatus.InProgress),
        });

        var listResult = await selectedQuery.ToListAsync();

        var result = new Base.Response.PageResult<Response.AllProfileResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };

        return result;
    }
}
