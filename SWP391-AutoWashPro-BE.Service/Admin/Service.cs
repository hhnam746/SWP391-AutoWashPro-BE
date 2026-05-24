using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Service : IService
{
    private const int DefaultSlotDurationMinutes = 15;
    private const int WorkingStartHour = 8;
    private const int WorkingEndHour = 17;
    private static readonly TimeSpan DefaultUtcOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<List<Response.BranchResponse>> GetBranches(bool? isActive, string? keyword)
    {
        var query = _dbContext.Branches.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, $"%{keyword}%") ||
                EF.Functions.ILike(x.Address, $"%{keyword}%"));
        }

        return await query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Select(x => new Response.BranchResponse
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<string> CreateBranch(Request.CreateBranch request)
    {
        var branchName = request.Name.Trim();
        var branchAddress = request.Address.Trim();
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new ArgumentException("Branch name is required.");
        }

        if (string.IsNullOrWhiteSpace(branchAddress))
        {
            throw new ArgumentException("Branch address is required.");
        }

        var branchNameExists = await _dbContext.Branches
            .AnyAsync(x => x.Name.ToLower() == branchName.ToLower());

        if (branchNameExists)
        {
            throw new InvalidOperationException("Branch name already exists.");
        }

        var newBranch = new Branch()
        {
            Name = branchName,
            Address = branchAddress,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        
        _dbContext.Add(newBranch);
        await _dbContext.SaveChangesAsync();
        return "Create branch successfully";
    }

    public async Task<string> UpdateBranch(Guid id, Request.UpdateBranch request)
    {
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id && x.IsActive == true);
        if (branch == null)
        {
            throw new KeyNotFoundException("Branch not found.");
        }

        var hasName = request.Name != null;
        var hasAddress = request.Address != null;
        var hasIsActive = request.IsActive.HasValue;

        if (!hasName && !hasAddress && !hasIsActive)
        {
            throw new ArgumentException("At least one field is required for update.");
        }

        if (hasName && string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Branch name cannot be empty.");
        }

        if (hasAddress && string.IsNullOrWhiteSpace(request.Address))
        {
            throw new ArgumentException("Branch address cannot be empty.");
        }

        if (hasName)
        {
            var branchName = request.Name!.Trim();
            var branchNameExists = await _dbContext.Branches
                .AnyAsync(x => x.Id != id && x.Name.ToLower() == branchName.ToLower());

            if (branchNameExists)
            {
                throw new InvalidOperationException("Branch name already exists.");
            }

            branch.Name = branchName;
        }

        if (hasAddress)
        {
            branch.Address = request.Address!.Trim();
        }

        if (hasIsActive)
        {
            branch.IsActive = request.IsActive!.Value;
        }

        branch.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return "Update branch successfully";
    }

    public async Task<string> DeleteBranch(Guid id)
    {
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id && x.IsActive == true);
        if (branch == null)
        {
            throw new KeyNotFoundException("Branch not found.");
        }
        
        branch.IsActive = false;
        branch.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return "Delete branch successfully";
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

    public async Task<Response.GetUserByIdResponse> GetUserById(Guid userId)
    {
        var result = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId &&
                        x.Role == UserRole.Customer &&
                        x.CustomerProfile != null)
            .Select(x => new Response.GetUserByIdResponse()
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
                    Id = x.CustomerProfile!.Id,
                    FirstName = x.CustomerProfile.FirstName,
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
                Wallet = x.CustomerProfile.Wallet == null
                    ? null
                    : new Response.WalletResponse()
                    {
                        Balance = x.CustomerProfile.Wallet.Balance
                    },
                Vehicles = x.CustomerProfile.Vehicles
                    .Where(v => v.DeletedAt == null)
                    .OrderByDescending(v => v.IsActive)
                    .ThenBy(v => v.CreatedAt)
                    .Select(v => new Response.VehicleResponse()
                    {
                        Id = v.Id,
                        LicensePlate = v.LicensePlate,
                        IsActive = v.IsActive
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (result == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return result;
    }

    public async Task<Response.GetUserStatusResponse> GetUserStatusById(Guid userId)
    {
        var result = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new Response.GetUserStatusResponse
            {
                UserId = x.Id,
                Status = x.Status
            })
            .FirstOrDefaultAsync();

        if (result == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return result;
    }

    public async Task<string> UpdateUserStatusById(Guid userId, Request.UpdateUserByStatusRequest request)
    {
        var currentAdminId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(currentAdminId) || !Guid.TryParse(currentAdminId, out var currentAdminGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        if (request == null)
        {
            throw new ArgumentException("Request body is required.");
        }

        if (request.Status is null)
        {
            throw new ArgumentException("Status is required.");
        }

        var newStatus = request.Status.Value;
        if (!Enum.IsDefined(typeof(AccountStatus), newStatus))
        {
            throw new ArgumentException("Status must be one of: Active, Locked, Inactive.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (user.Role == UserRole.Customer && !user.isVerify)
        {
            throw new InvalidOperationException("User is not verified.");
        }

        var isSelfLocking = user.Id == currentAdminGuid && user.Role == UserRole.Admin &&
                            newStatus != AccountStatus.Active;
        if (isSelfLocking)
        {
            var activeAdminCount = await _dbContext.Users.CountAsync(u =>
                u.Role == UserRole.Admin && u.Status == AccountStatus.Active);

            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException(
                    "Cannot lock or deactivate yourself because the system must keep at least one active admin.");
            }
        }

        user.Status = newStatus;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return "Update user status successfully";
    }

    public async Task<List<Response.BookingResponse>> GetBookings(
        Request.GetBookingRequest request)
    {
        if (request.BranchId == Guid.Empty)
        {
            throw new ArgumentException("BranchId is required.");
        }

        if (request.Date == default)
        {
            throw new ArgumentException("Date is required.");
        }

        var branchExists = await _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.BranchId);

        if (!branchExists)
        {
            throw new KeyNotFoundException("Branch not found.");
        }

        var query = _dbContext.Bookings
            .AsNoTracking()
            .Where(x =>
                x.Customer.User.Role == UserRole.Customer &&
                x.Customer.User.isVerify &&
                x.Customer.User.Status == AccountStatus.Active &&
                x.BranchId == request.BranchId &&
                x.BookingDate == request.Date);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        var items = await query
            .OrderBy(x => x.StartTime)
            .Select(x => new Response.BookingResponse
            {
                Id = x.Id,
                Status = ToBookingStatusValue(x.Status),
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                FinalPrice = x.FinalPrice,

                Customer = new Response.BookingCustomerResponse
                {
                    Id = x.Customer.UserId,
                    FullName = $"{x.Customer.FirstName} {x.Customer.LastName}".Trim(),
                    Phone = x.Customer.User.Phone,
                    TierName = x.Customer.Tier.Name
                },

                Vehicle = new Response.BookingVehicleResponse
                {
                    Id = x.Vehicle.Id,
                    LicensePlate = x.Vehicle.LicensePlate
                },

                Branch = new Response.BookingBranchResponse
                {
                    Id = x.Branch.Id,
                    Name = x.Branch.Name
                }
            })
            .ToListAsync();

        return items;
    }

    public async Task<Response.BookingSlotResponse> GetBookingSlots(Request.GetBookingSlotRequest request)
    {
        if (request.BranchId == Guid.Empty)
        {
            throw new ArgumentException("BranchId is required.");
        }

        if (request.Date == default)
        {
            throw new ArgumentException("Date is required.");
        }

        var branchExists = await _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.BranchId);

        if (!branchExists)
        {
            throw new KeyNotFoundException("Branch not found.");
        }

        var bookedSlots = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.BranchId == request.BranchId &&
                        x.BookingDate == request.Date &&
                        x.Status != BookingStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.StartTime,
                x.EndTime,
                x.Vehicle.LicensePlate,
                x.Customer.FirstName,
                x.Customer.LastName
            })
            .ToListAsync();

        var bookingsBySlotStart = bookedSlots
            .GroupBy(x => x.StartTime.ToOffset(DefaultUtcOffset))
            .ToDictionary(x => x.Key, x => x.First());

        var slotData = new List<Response.SlotDataResponse>();
        var slotStart = BuildSlotTime(request.Date, WorkingStartHour, 0);
        var workingEnd = BuildSlotTime(request.Date, WorkingEndHour, 0);

        while (slotStart < workingEnd)
        {
            var slotEnd = slotStart.AddMinutes(DefaultSlotDurationMinutes);
            if (bookingsBySlotStart.TryGetValue(slotStart, out var booking))
            {
                slotData.Add(new Response.SlotDataResponse
                {
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    Status = "booked",
                    Booking = new Response.SlotBookingResponse
                    {
                        Id = booking.Id,
                        Status = ToBookingStatusValue(booking.Status),
                        LicensePlate = booking.LicensePlate,
                        CustomerName = $"{booking.FirstName} {booking.LastName}".Trim()
                    }
                });
            }
            else
            {
                slotData.Add(new Response.SlotDataResponse
                {
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    Status = "available",
                    Booking = null
                });
            }

            slotStart = slotEnd;
        }

        return new Response.BookingSlotResponse
        {
            BranchId = request.BranchId,
            Date = request.Date,
            SlotDurationMinutes = DefaultSlotDurationMinutes,
            Data = slotData
        };
    }

    private static DateTimeOffset BuildSlotTime(DateOnly date, int hour, int minute)
    {
        var localDateTime = date.ToDateTime(new TimeOnly(hour, minute));
        return new DateTimeOffset(localDateTime, DefaultUtcOffset);
    }

    private static string ToBookingStatusValue(BookingStatus status)
    {
        return status switch
        {
            BookingStatus.Pending => "pending",
            BookingStatus.Confirmed => "confirmed",
            BookingStatus.CheckIn => "check_in",
            BookingStatus.InProgress => "in_progress",
            BookingStatus.Completed => "completed",
            BookingStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    private static BookingStatus ParseBookingStatus(string status)
    {
        var normalizedStatus = status.Trim().ToLowerInvariant();
        return normalizedStatus switch
        {
            "pending" => BookingStatus.Pending,
            "confirmed" => BookingStatus.Confirmed,
            "check_in" => BookingStatus.CheckIn,
            "in_progress" => BookingStatus.InProgress,
            "completed" => BookingStatus.Completed,
            "cancelled" => BookingStatus.Cancelled,
            _ => throw new ArgumentException(
                "Status must be one of: pending, confirmed, check_in, in_progress, completed, cancelled.")
        };
    }
}
