using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Service : IService
{
    private static readonly TimeSpan DefaultUtcOffset = TimeSpan.FromHours(7);
    private  int WorkingStartHour = 0;
    private  int WorkingEndHour = 0;
    private  int SlotDurationMinutes = 0;
    private  int SlotBreakMinutes = 0;
    private int PointsPerCompletedWash = 0;

    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly Notification.IService _notificationService;
    private readonly BookingService.IService _bookingService;
    private readonly ILogger<Service> _logger;

    public Service(
        AppDbContext dbContext,
        IHttpContextAccessor httpContext,
        Notification.IService notificationService,
        BookingService.IService bookingService,
        ILogger<Service> logger)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _notificationService = notificationService;
        _bookingService = bookingService;
        _logger = logger;
    }

    public async Task<List<Response.BranchResponse>> GetBranches(bool? isActive, string? keyword)
    {
        var query = _dbContext.Branches
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

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
            .AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == branchName.ToLower());

        if (branchNameExists)
        {
            throw new InvalidOperationException("Branch name already exists.");
        }

        var newBranch = new Repository.Entities.Branch()
        {
            Name = branchName,
            Address = branchAddress,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        
        _dbContext.Add(newBranch);
        await _dbContext.SaveChangesAsync();
        return "Create branch successfully";
    }

    public async Task<string> UpdateBranch(Guid id, Request.UpdateBranch request)
    {
        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
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
                .AnyAsync(x => x.Id != id && !x.IsDeleted && x.Name.ToLower() == branchName.ToLower());

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
        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (branch == null)
        {
            throw new KeyNotFoundException("Branch not found.");
        }
        
        branch.IsDeleted = true;
        branch.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return "Delete branch successfully";
    }

    public async Task<Response.DashboardResponse> GetDashboard(Request.GetDashboardRequest request)
    {
        if (request.FromDate == default || request.ToDate == default)
        {
            throw new ArgumentException("FromDate and ToDate are required.");
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.");
        }

        var totalBranches = 0;
        var activeBranches = 0;
        if (request.BranchId.HasValue)
        {
            var requestedBranch = await _dbContext.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.BranchId.Value && !x.IsDeleted);

            if (requestedBranch == null)
            {
                throw new KeyNotFoundException("Branch not found.");
            }

            totalBranches = 1;
            activeBranches = requestedBranch.IsActive ? 1 : 0;
        }
        else
        {
            totalBranches = await _dbContext.Branches.AsNoTracking().CountAsync(x => !x.IsDeleted);
            activeBranches = await _dbContext.Branches.AsNoTracking().CountAsync(x => !x.IsDeleted && x.IsActive);
        }

        var bookingsInRangeQuery = _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.BookingDate >= request.FromDate && x.BookingDate <= request.ToDate);

        if (request.BranchId.HasValue)
        {
            bookingsInRangeQuery = bookingsInRangeQuery.Where(x => x.BranchId == request.BranchId.Value);
        }

        var totalCustomers = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(x => x.Role == UserRole.Customer);

        var activeCustomers = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(x => x.Role == UserRole.Customer && x.Status == AccountStatus.Active);

        var lockedCustomers = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(x => x.Role == UserRole.Customer && x.Status == AccountStatus.Locked);

        var totalBookings = await bookingsInRangeQuery.CountAsync();
        var completedBookings = await bookingsInRangeQuery.CountAsync(x => x.Status == BookingStatus.Completed);
        var cancelledBookings = await bookingsInRangeQuery.CountAsync(x => x.Status == BookingStatus.Cancelled);
        var totalRevenue = await bookingsInRangeQuery
            .Where(x => x.Status == BookingStatus.Completed)
            .SumAsync(x => (decimal?)x.FinalPrice) ?? 0m;

        var todayDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(DefaultUtcOffset).DateTime);
        var todayBookingsQuery = _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.BookingDate == todayDate);

        if (request.BranchId.HasValue)
        {
            todayBookingsQuery = todayBookingsQuery.Where(x => x.BranchId == request.BranchId.Value);
        }

        var todayBookings = await todayBookingsQuery
            .OrderBy(x => x.StartTime)
            .Take(50)
            .Select(x => new
            {
                x.Id,
                x.StartTime,
                x.Status,
                BranchName = x.Branch.Name,
                x.Vehicle.LicensePlate
            })
            .ToListAsync();

        var topBranches = await bookingsInRangeQuery
            .Where(x => x.Status == BookingStatus.Completed)
            .GroupBy(x => new { x.BranchId, x.Branch.Name })
            .Select(x => new Response.DashboardTopBranchResponse
            {
                BranchId = x.Key.BranchId,
                BranchName = x.Key.Name,
                CompletedBookings = x.Count(),
                Revenue = x.Sum(y => y.FinalPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.CompletedBookings)
            .Take(5)
            .ToListAsync();

        return new Response.DashboardResponse
        {
            Summary = new Response.DashboardSummaryResponse
            {
                TotalCustomers = totalCustomers,
                ActiveCustomers = activeCustomers,
                LockedCustomers = lockedCustomers,
                TotalBookings = totalBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalRevenue = totalRevenue,
                TotalBranches = totalBranches,
                ActiveBranches = activeBranches
            },
            TodayBookings = todayBookings
                .Select(x => new Response.DashboardTodayBookingResponse
                {
                    Id = x.Id,
                    StartTime = x.StartTime.ToOffset(DefaultUtcOffset),
                    Status = x.Status.ToString(),
                    BranchName = x.BranchName,
                    LicensePlate = x.LicensePlate
                })
                .ToList(),
            TopBranches = topBranches
        };
    }

    public async Task<Response.RevenueReportResponse> GetRevenueReport(Request.GetRevenueReportRequest request)
    {
        if (request.FromDate == default || request.ToDate == default)
        {
            throw new ArgumentException("FromDate and ToDate are required.");
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.");
        }

        // if (request.BranchId == Guid.Empty)
        // {
        //     throw new ArgumentException("BranchId is required.");
        // }

        var branchExists = await _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted);

        if (!branchExists)
        {
            throw new KeyNotFoundException("Branch not found.");
        }

        var dailyRaw = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x =>
                        x.BookingDate >= request.FromDate &&
                        x.BookingDate <= request.ToDate)
            .GroupBy(x => x.BookingDate)
            .Select(x => new
            {
                Date = x.Key,
                BookingCount = x.Count(),
                CompletedBookingCount = x.Count(y => y.Status == BookingStatus.Completed),
                Revenue = x
                    .Where(y => y.Status == BookingStatus.Completed)
                    .Sum(y => (decimal?)y.FinalPrice) ?? 0m
            })
            .ToListAsync();

        var dailyLookup = dailyRaw.ToDictionary(x => x.Date, x => x);
        var data = new List<Response.RevenueReportItemResponse>();
        var totalRevenue = 0m;

        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            if (dailyLookup.TryGetValue(date, out var day))
            {
                data.Add(new Response.RevenueReportItemResponse
                {
                    Date = day.Date,
                    BookingCount = day.BookingCount,
                    CompletedBookingCount = day.CompletedBookingCount,
                    Revenue = day.Revenue
                });
                totalRevenue += day.Revenue;
                continue;
            }

            data.Add(new Response.RevenueReportItemResponse
            {
                Date = date,
                BookingCount = 0,
                CompletedBookingCount = 0,
                Revenue = 0m
            });
        }

        return new Response.RevenueReportResponse
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalRevenue = totalRevenue,
            Data = data
        };
    }

    public async Task<Base.Response.PageResult<Response.BranchReportItemResponse>> GetBranchReport(
        Request.GetBranchReportRequest request)
    {
        if (request.FromDate == default || request.ToDate == default)
        {
            throw new ArgumentException("FromDate and ToDate are required.");
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.");
        }

        if (request.PageIndex <= 0)
        {
            throw new ArgumentException("PageIndex must be greater than 0.");
        }

        if (request.PageSize <= 0)
        {
            throw new ArgumentException("PageSize must be greater than 0.");
        }

        var groupedQuery = _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.BookingDate >= request.FromDate &&
                        x.BookingDate <= request.ToDate)
            .GroupBy(x => new { x.BranchId, x.Branch.Name })
            .Select(x => new Response.BranchReportItemResponse
            {
                BranchId = x.Key.BranchId,
                BranchName = x.Key.Name,
                CompletedBookings = x.Count(y => y.Status == BookingStatus.Completed),
                CancelledBookings = x.Count(y => y.Status == BookingStatus.Cancelled),
                Revenue = x
                    .Where(y => y.Status == BookingStatus.Completed)
                    .Sum(y => (decimal?)y.FinalPrice) ?? 0m
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.CompletedBookings)
            .ThenBy(x => x.BranchName);

        var totalItems = await groupedQuery.CountAsync();
        var items = await groupedQuery
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new Base.Response.PageResult<Response.BranchReportItemResponse>
        {
            Items = items,
            TotalItems = totalItems,
            PageSize = request.PageSize,
            PageIndex = request.PageIndex
        };
    }

    public async Task<Response.LoyaltyReportResponse> GetLoyaltyReport(Request.GetLoyaltyReportRequest request)
    {
        if (request.FromDate == default || request.ToDate == default)
        {
            throw new ArgumentException("FromDate and ToDate are required.");
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.");
        }

        var fromDateTime = request.FromDate.ToDateTime(TimeOnly.MinValue);
        var toExclusiveDateTime = request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var fromLocal = new DateTimeOffset(fromDateTime, DefaultUtcOffset);
        var toExclusiveLocal = new DateTimeOffset(toExclusiveDateTime, DefaultUtcOffset);
        var fromUtc = fromLocal.ToUniversalTime();
        var toExclusiveUtc = toExclusiveLocal.ToUniversalTime();

        var pointTransactionsInRange = _dbContext.PointTransactions
            .AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc);

        var totalPointsEarned = await pointTransactionsInRange
            .Where(x => x.TransactionType == PointTransactionType.Earn)
            .SumAsync(x => (int?)x.Points) ?? 0;

        var totalPointsRedeemed = await pointTransactionsInRange
            .Where(x => x.TransactionType == PointTransactionType.Redeem)
            .SumAsync(x => (int?)x.Points) ?? 0;

        var totalRewardsRedeemed = await pointTransactionsInRange
            .CountAsync(x => x.TransactionType == PointTransactionType.Redeem && x.RewardId != null);

        var tierUpgradeCount = await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(x => x.Type == NotificationType.TierUpgraded &&
                             x.CreatedAt >= fromUtc &&
                             x.CreatedAt < toExclusiveUtc);

        var tierDistribution = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(x => x.User.Role == UserRole.Customer)
            .GroupBy(x => new { x.TierId, x.Tier.Name, x.Tier.Level })
            .Select(x => new
            {
                x.Key.Level,
                Item = new Response.TierDistributionItemResponse
                {
                    TierName = x.Key.Name,
                    CustomerCount = x.Count()
                }
            })
            .OrderBy(x => x.Level)
            .Select(x => x.Item)
            .ToListAsync();

        return new Response.LoyaltyReportResponse
        {
            Summary = new Response.LoyaltySummaryResponse
            {
                TotalPointsEarned = totalPointsEarned,
                TotalPointsRedeemed = totalPointsRedeemed,
                TotalRewardsRedeemed = totalRewardsRedeemed,
                TierUpgradeCount = tierUpgradeCount
            },
            TierDistribution = tierDistribution
        };
    }

    public Task<BookingService.Response.CheckInBookingResponse> CheckInBookingByAdmin(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return _bookingService.CheckInBookingByAdmin(bookingId, cancellationToken);
    }

    public async Task<Response.CompleteBookingByAdminResponse> CompleteBookingByAdmin(
        Guid bookingId,
        Request.CompleteBookingByAdminRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body is required.");
        }

        var booking = await _dbContext.Bookings
            .Include(x => x.Customer)
                .ThenInclude(x => x.User)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        if (booking.Status != BookingStatus.InProgress &&
            booking.Status != BookingStatus.CheckIn)
        {
            throw new InvalidOperationException("Only check_in or in_progress bookings can be completed manually.");
        }

        var now = DateTimeOffset.UtcNow;
        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = now;
        booking.UpdatedAt = now;

        var customerProfile = booking.Customer;
        customerProfile.TotalPoints += PointsPerCompletedWash;
        customerProfile.TotalWashes += 1;
        customerProfile.LastPointActivityAt = now;

        _dbContext.PointTransactions.Add(new Repository.Entities.PointTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            BookingId = booking.Id,
            Points = PointsPerCompletedWash,
            TransactionType = PointTransactionType.Earn,
            Description = "Points earned from completed booking.",
            CreatedAt = now
        });

        var currentTier = await _dbContext.Tiers
            .FirstOrDefaultAsync(x => x.Id == customerProfile.TierId);

        if (currentTier != null)
        {
            var upgradedTier = await _dbContext.Tiers
                .Where(x => x.Level > currentTier.Level &&
                            x.RequiredWashes <= customerProfile.TotalWashes)
                .OrderByDescending(x => x.Level)
                .FirstOrDefaultAsync();

            if (upgradedTier != null)
            {
                customerProfile.TierId = upgradedTier.Id;
                _dbContext.Notifications.Add(new Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = customerProfile.UserId,
                    Type = NotificationType.TierUpgraded,
                    Title = "Tier Upgraded",
                    Content = $"Congratulations! Your tier has been upgraded from {currentTier.Name} to {upgradedTier.Name}.",
                    IsRead = false,
                    CreatedAt = now
                });
            }
        }

        var manualNote = string.IsNullOrWhiteSpace(request.Note)
            ? string.Empty
            : $" Note: {request.Note.Trim()}";

        _dbContext.Notifications.Add(new Repository.Entities.Notification
        {
            Id = Guid.NewGuid(),
            UserId = customerProfile.UserId,
            Type = NotificationType.BookingCompleted,
            Title = "Booking Completed",
            Content = $"Your booking at {booking.Branch.Name} has been completed successfully.{manualNote}",
            IsRead = false,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync();

        return new Response.CompleteBookingByAdminResponse
        {
            Id = booking.Id,
            Status = booking.Status.ToString(),
            CompletedAt = booking.CompletedAt.Value,
            PointsEarned = PointsPerCompletedWash,
            Message = "Booking completed and loyalty points applied"
        };
    }

    public async Task<Response.CancelBookingByAdminResponse> CancelBookingByAdmin(
        Guid bookingId,
        Request.CancelBookingByAdminRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required.");
        }

        var booking = await _dbContext.Bookings
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Booking is already cancelled.");
        }

        if (booking.Status == BookingStatus.Completed)
        {
            throw new InvalidOperationException("Completed booking cannot be cancelled.");
        }

        var now = DateTimeOffset.UtcNow;
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.UpdatedAt = now;

        _dbContext.Notifications.Add(new Repository.Entities.Notification
        {
            Id = Guid.NewGuid(),
            UserId = booking.Customer.UserId,
            Type = NotificationType.BookingCancelled,
            Title = "Booking Cancelled",
            Content = $"Your booking at {booking.Branch.Name} has been cancelled. Reason: {request.Reason.Trim()}",
            IsRead = false,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync();

        return new Response.CancelBookingByAdminResponse
        {
            Id = booking.Id,
            Status = booking.Status.ToString(),
            CancelledAt = booking.CancelledAt.Value,
            Message = "Booking cancelled"
        };
    }

    //Verify Account

    public async Task<string> ApprovalIdentity(Guid userId)
    {
        var targetUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && 
                                                                         !u.isVerify && 
                                                                         u.Status == AccountStatus.Pending || 
                                                                         u.Status == AccountStatus.Rejected);

        Console.WriteLine($"{targetUser.Id} | {targetUser.Email} | {targetUser.isVerify} |  {targetUser.Status}");
        if (targetUser == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (targetUser.Role != UserRole.Customer)
        {
            throw new InvalidOperationException("Only customer accounts can be verified.");
        }

        if (targetUser.Status != AccountStatus.Pending &&
            targetUser.Status != AccountStatus.Rejected)
        {
            throw new InvalidOperationException("Only pending or rejected users can be verified.");
        }

        Console.WriteLine("Target: " + targetUser.isVerify);

        targetUser.isVerify = true;
        targetUser.Status =  AccountStatus.Active;
        targetUser.VerifiedAt = DateTimeOffset.UtcNow;
        targetUser.UpdatedAt = targetUser.VerifiedAt;
        
        Console.WriteLine("Target: " + targetUser.isVerify);


        await _dbContext.SaveChangesAsync();
        
        //gọi lại SignalR
        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
        {
            UserId = targetUser.Id,
            Type = NotificationType.IdentityApproved,
            Data = null,
        });
        
        
        return "Verify user successfully";
    }

    public async Task<string> RejectIdentity(Guid userId, Request.RejectIdentityDocument request)
    {
        var targetUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && 
                                                                         !u.isVerify && 
                                                                         u.Status == AccountStatus.Pending);
        if (targetUser == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (request.RejectReason is null)
        {
            throw new ArgumentException("Reason is required.");
        }
        
        if (targetUser.Role != UserRole.Customer)
        {
            throw new InvalidOperationException("Only customer accounts can be verified.");
        }

        if (targetUser.Status != AccountStatus.Pending)
        {
            throw new InvalidOperationException("Only pending users can be verified.");
        }

        if (targetUser.isVerify)
        {
            return "User is already verified.";
        }

        // targetUser.isVerify = true;
        targetUser.Status =  AccountStatus.Rejected;
        targetUser.Reason = request.RejectReason;
        targetUser.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        
        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
        {
            UserId = targetUser.Id,
            Type = NotificationType.IdentityRejected,
            Data = null,
        });
        
        
        return "Reject user successfully";
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
            .Where(x => x.isVerify && x.Status == AccountStatus.Active || x.Status == AccountStatus.Locked &&
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

        query = query.OrderByDescending(x => x.CreatedAt);

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
                DateOfBirth = x.CustomerProfile.DateOfBirth,
                TotalPoints = x.CustomerProfile.TotalPoints,
                TotalWashes = x.CustomerProfile.TotalWashes,
                FaceImageUrls = x.UserFaceImages
                    .Where(x => x.IsActive)
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .Take(3)
                    .ToList(),
                TierData = x.CustomerProfile.Tier! == null
                    ? null
                    : new Response.TierData()
                    {
                        Id = x.CustomerProfile.Tier.Id,
                        Name = x.CustomerProfile.Tier.Name,
                        Level = x.CustomerProfile.Tier.Level,
                        PriorityBookingDays = x.CustomerProfile.Tier.PriorityBookingDays,
                        RequiredWashes = x.CustomerProfile.Tier.RequiredWashes,
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
                        x.CustomerProfile != null &&
                        (x.Status == AccountStatus.Pending ||
                         x.Status == AccountStatus.Rejected));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(x =>
                x.CustomerProfile!.FirstName.Contains(searchTerm) ||
                x.CustomerProfile!.LastName.Contains(searchTerm) ||
                x.Email!.Contains(searchTerm));
        }

        var totalItems = await query.CountAsync();

        query = query.OrderByDescending(x => x.CreatedAt);

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
                DateOfBirth = x.CustomerProfile.DateOfBirth,
                TotalPoints = x.CustomerProfile.TotalPoints,
                TotalWashes = x.CustomerProfile.TotalWashes,
                FaceImageUrls = x.UserFaceImages
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .Take(3)
                    .ToList(),
                TierData = x.CustomerProfile.Tier! == null
                    ? null
                    : new Response.TierData()
                    {
                        Id = x.CustomerProfile.Tier.Id,
                        Name = x.CustomerProfile.Tier.Name,
                        Level = x.CustomerProfile.Tier.Level,
                        PriorityBookingDays = x.CustomerProfile.Tier.PriorityBookingDays,
                        RequiredWashes = x.CustomerProfile.Tier.RequiredWashes,
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
                    DateOfBirth = x.CustomerProfile.DateOfBirth,
                    TotalPoints = x.CustomerProfile.TotalPoints,
                    TotalWashes = x.CustomerProfile.TotalWashes,
                    FaceImageUrls = x.UserFaceImages
                        .Where(i => i.IsActive)
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .Take(3)
                        .ToList(),
                    TierData = x.CustomerProfile.Tier! == null
                        ? null
                        : new Response.TierData()
                        {
                            Id = x.CustomerProfile.Tier.Id,
                            Name = x.CustomerProfile.Tier.Name,
                            Level = x.CustomerProfile.Tier.Level,
                            PriorityBookingDays = x.CustomerProfile.Tier.PriorityBookingDays,
                            RequiredWashes = x.CustomerProfile.Tier.RequiredWashes,
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
                Status = x.Status,
                IsVerify = x.isVerify,
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

    public async Task<string> CorrectCustomerDateOfBirth(
        Guid userId,
        Request.CorrectCustomerDateOfBirthRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Correction reason is required.");
        }

        SWP391_AutoWashPro_BE.Service.User.DateOfBirthValidator.EnsureValid(request.DateOfBirth);
        var adminUserId = ServiceClaimHelper.GetRequiredAdminId(_httpContext);

        var customer = await _dbContext.CustomerProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.User.Role == UserRole.Customer,
                cancellationToken);

        if (customer == null)
        {
            throw new KeyNotFoundException("Customer not found.");
        }

        if (customer.DateOfBirth == request.DateOfBirth)
        {
            return "No date of birth changes detected.";
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var correction = new CustomerDateOfBirthCorrection
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            AdminUserId = adminUserId,
            PreviousDateOfBirth = customer.DateOfBirth,
            NewDateOfBirth = request.DateOfBirth,
            Reason = request.Reason.Trim(),
            CreatedAt = nowUtc
        };

        customer.DateOfBirth = request.DateOfBirth;
        customer.DateOfBirthSetAt ??= nowUtc;
        customer.UpdatedAt = nowUtc;
        _dbContext.CustomerDateOfBirthCorrections.Add(correction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminUserId} corrected date of birth for CustomerId={CustomerId}. CorrectionId={CorrectionId}.",
            adminUserId,
            customer.Id,
            correction.Id);

        return "Customer date of birth corrected successfully.";
    }

    public async Task<Base.Response.PageResult<Response.BookingResponse>> GetBookings(
        Request.GetBookingRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        if (request == null)
        {
            throw new ArgumentException("Request is required.");
        }

        if (request.BranchId.HasValue && request.BranchId.Value == Guid.Empty)
        {
            throw new ArgumentException("BranchId is invalid.");
        }

        if (request.PageIndex <= 0)
        {
            throw new ArgumentException("PageIndex must be greater than 0.");
        }

        if (request.PageSize <= 0)
        {
            throw new ArgumentException("PageSize must be greater than 0.");
        }

        if (request.FromDate.HasValue &&
            request.ToDate.HasValue &&
            request.FromDate.Value > request.ToDate.Value)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.");
        }

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.BranchId.Value && !x.IsDeleted);

            if (!branchExists)
            {
                throw new KeyNotFoundException("Branch not found.");
            }
        }

        var query = _dbContext.Bookings
            .AsNoTracking()
            .AsQueryable();

        if (request.BranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == request.BranchId.Value);
        }

        if (request.Date.HasValue)
        {
            query = query.Where(x => x.BookingDate == request.Date.Value);
        }
        else
        {
            if (request.FromDate.HasValue)
            {
                query = query.Where(x => x.BookingDate >= request.FromDate.Value);
            }
        
            if (request.ToDate.HasValue)
            {
                query = query.Where(x => x.BookingDate <= request.ToDate.Value);
            }
        }

        if (request.Status.HasValue && request.Status.Value != BookingStatus.Available)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        var totalItems = await query.CountAsync();

        var bookingDetail = query
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .Select(x => new Response.BookingResponse
            {
                Id = x.Id,
                Status = x.Status.ToString(),
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                FinalPrice = x.FinalPrice,

                Customer = new Response.BookingCustomerResponse
                {
                    Id = x.Customer.UserId,
                    FullName = $"{x.Customer.FirstName} {x.Customer.LastName}".Trim(),
                    Phone = x.Customer.User.Phone ?? string.Empty,
                    TierName = x.Customer.Tier != null ? x.Customer.Tier.Name : string.Empty
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
            });

        var items = await bookingDetail
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        foreach (var booking in items)
        {
            booking.StartTime = booking.StartTime.ToOffset(DefaultUtcOffset);
            booking.EndTime = booking.EndTime.ToOffset(DefaultUtcOffset);
        }

        return new Base.Response.PageResult<Response.BookingResponse>
        {
            Items = items,
            TotalItems = totalItems,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }

    public async Task<Response.BookingSlotResponse> GetBookingSlots(Request.GetBookingSlotRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        
        if (request.BranchId == Guid.Empty)
        {
            throw new ArgumentException("BranchId is required.");
        }

        if (request.Date == default)
        {
            throw new ArgumentException("Date is required.");
        }

        if (request.PageIndex <= 0)
        {
            throw new ArgumentException("PageIndex must be greater than 0.");
        }

        if (request.PageSize <= 0)
        {
            throw new ArgumentException("PageSize must be greater than 0.");
        }

        var branchExists = await _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.BranchId && !x.IsDeleted);

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
        
        //lấy ra config từ system config
        
        var workingStartHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingStartHour");

        if (workingStartHourConfig == null)
        {
            throw new Exception("WorkingStartHour config not found");
        }

        if (!int.TryParse(workingStartHourConfig.ConfigValue, out var workingStartHour))
        {
            throw new Exception("Invalid WorkingStartHour config value");
        }
        ///////////////// ///////////////// ///////////////// /////////////////
        WorkingStartHour = workingStartHour;
        ///////////////// ///////////////// ///////////////// /////////////////
        
        var workingEndHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingEndHour");

        if (workingEndHourConfig == null)
        {
            throw new Exception("WorkingEndHour config not found");
        }

        if (!int.TryParse(workingEndHourConfig.ConfigValue, out var workingEndHour))
        {
            throw new Exception("Invalid WorkingEndHour config value");
        }

        ///////////////// ///////////////// ///////////////// /////////////////
        WorkingEndHour = workingEndHour;
        ///////////////// ///////////////// ///////////////// /////////////////
        
        var slotDurationConfig = await _dbContext.SystemConfigs
                                     .FirstOrDefaultAsync(x => x.ConfigKey == "SlotDurationMinutes")
                                 ?? throw new Exception("SlotDurationMinutes config not found");

        if (!int.TryParse(slotDurationConfig.ConfigValue, out SlotDurationMinutes))
        {
            throw new Exception("Invalid SlotDurationMinutes config value");
        }

        var slotBreakConfig = await _dbContext.SystemConfigs
                                  .FirstOrDefaultAsync(x => x.ConfigKey == "SlotBreakMinutes")
                              ?? throw new Exception("SlotBreakMinutes config not found");

        if (!int.TryParse(slotBreakConfig.ConfigValue, out SlotBreakMinutes))
        {
            throw new Exception("Invalid SlotBreakMinutes config value");
        }

        var slotData = new List<Response.SlotDataResponse>();

        var currentTime = new DateTimeOffset(
            request.Date.Year,
            request.Date.Month,
            request.Date.Day,
            WorkingStartHour,
            0,
            0,
            DefaultUtcOffset);

        var endWorkTime = new DateTimeOffset(
            request.Date.Year,
            request.Date.Month,
            request.Date.Day,
            WorkingEndHour,
            0,
            0,
            DefaultUtcOffset);

        while (currentTime.AddMinutes(SlotDurationMinutes) <= endWorkTime)
        {
            var slotStartTime = currentTime;
            var slotEndTime = currentTime.AddMinutes(SlotDurationMinutes);

            var booking = bookedSlots.FirstOrDefault(x =>
                x.StartTime.UtcDateTime == slotStartTime.UtcDateTime &&
                x.EndTime.UtcDateTime == slotEndTime.UtcDateTime);

            if (booking != null)
            {
                slotData.Add(new Response.SlotDataResponse
                {
                    StartTime = slotStartTime,
                    EndTime = slotEndTime,
                    Status = "booked",
                    Booking = new Response.SlotBookingResponse
                    {
                        Id = booking.Id,
                        Status = booking.Status.ToString(),
                        LicensePlate = booking.LicensePlate,
                        CustomerName = $"{booking.FirstName} {booking.LastName}".Trim()
                    }
                });
            }
            else
            {
                slotData.Add(new Response.SlotDataResponse
                {
                    StartTime = slotStartTime,
                    EndTime = slotEndTime,
                    Status = "available",
                    Booking = null
                });
            }

            currentTime = slotEndTime.AddMinutes(SlotBreakMinutes);
        }

        var totalItems = slotData.Count;
        var pagedSlotData = slotData
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new Response.BookingSlotResponse
        {
            BranchId = request.BranchId,
            Date = request.Date,
            SlotDurationMinutes = SlotDurationMinutes,
            TotalItems = totalItems,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = pagedSlotData
        };
    }
}
