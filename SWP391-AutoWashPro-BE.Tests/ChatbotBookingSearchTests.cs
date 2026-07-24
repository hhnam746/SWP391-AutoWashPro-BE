using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.AiService;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class ChatbotBookingSearchTests
{
    [Fact]
    public void Detect_WithBookingDetailPhraseWithoutGuid_ReturnsBookingIntent()
    {
        var detector = new IntentDetector();

        var result = detector.Detect("Cho toi xem chi tiet booking cua toi o chi nhanh Thu Duc");

        Assert.Equal(ChatIntent.BookingDetail, result.Intent);
        Assert.True(result.HasBranchHint);
        Assert.Null(result.BookingId);
    }

    [Fact]
    public void Detect_WithBookingDetailDate_ExtractsBookingDate()
    {
        var detector = new IntentDetector();

        var result = detector.Detect("chi tiet booking ngay 12/07/2026");

        Assert.Equal(ChatIntent.BookingDetail, result.Intent);
        Assert.Equal(new DateOnly(2026, 7, 12), result.BookingDate);
    }

    [Fact]
    public void Detect_WithBookingCountReference_ExtractsRequestedBookingCount()
    {
        var detector = new IntentDetector();

        var result = detector.Detect("Chi tiet 10 booking do");

        Assert.Equal(ChatIntent.BookingDetail, result.Intent);
        Assert.Equal(10, result.RequestedBookingCount);
    }

    [Fact]
    public async Task SearchMyBookingsForChatbot_WithBranchAndLicensePlate_FiltersAndOrdersResults()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedCustomerWithBookingsAsync(dbContext);
        var service = CreateBookingService(dbContext, seeded.User.Id);

        var result = await service.SearchMyBookingsForChatbot(
            normalizedMessage: "booking cua toi o chi nhanh thu duc bien so 51a12345",
            bookingDate: null,
            licensePlate: "51A12345",
            status: null,
            hasBranchHint: true,
            hasLicensePlateHint: true,
            hasStatusHint: false,
            limit: 5);

        Assert.True(result.HasResolvedFilters);
        Assert.Equal("Thu Duc", result.MatchedBranch);
        Assert.Equal("51A12345", result.MatchedLicensePlate);
        Assert.Equal(2, result.TotalMatched);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("51A-12345", item.LicensePlate));
        Assert.All(result.Items, item => Assert.Equal("Thu Duc", item.BranchName));
        Assert.True(result.Items[0].StartTime >= result.Items[1].StartTime);
    }

    [Fact]
    public async Task SearchMyBookingsForChatbot_WithUnknownBranchHint_ReturnsHelpfulMessage()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedCustomerWithBookingsAsync(dbContext);
        var service = CreateBookingService(dbContext, seeded.User.Id);

        var result = await service.SearchMyBookingsForChatbot(
            normalizedMessage: "booking cua toi o chi nhanh quan 9",
            bookingDate: null,
            licensePlate: null,
            status: null,
            hasBranchHint: true,
            hasLicensePlateHint: false,
            hasStatusHint: false,
            limit: 5);

        Assert.True(result.HasRequestedFilters);
        Assert.False(result.HasResolvedFilters);
        Assert.NotNull(result.Message);
        Assert.Empty(result.Items);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<(User User, CustomerProfile CustomerProfile)> SeedCustomerWithBookingsAsync(AppDbContext dbContext)
    {
        var tier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Member",
            Level = 1,
            RequiredWashes = 0,
            PriorityBookingDays = 5,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            Phone = "0912345678",
            PasswordHash = "hashed-password",
            Role = UserRole.Customer,
            Status = AccountStatus.Active,
            isVerify = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var customerProfile = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TierId = tier.Id,
            Tier = tier,
            FirstName = "An",
            LastName = "Nguyen",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var thuDuc = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Thu Duc",
            Address = "1 Vo Van Ngan",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var goVap = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Go Vap",
            Address = "99 Quang Trung",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var vehicle1 = new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            Customer = customerProfile,
            LicensePlate = "51A-12345",
            Brand = "Toyota",
            Model = "Vios",
            IsActive = true,
            VehicleTypeId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var vehicle2 = new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            Customer = customerProfile,
            LicensePlate = "59B-67890",
            Brand = "Honda",
            Model = "City",
            IsActive = true,
            VehicleTypeId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Tiers.Add(tier);
        dbContext.Users.Add(user);
        dbContext.CustomerProfiles.Add(customerProfile);
        dbContext.Branches.AddRange(thuDuc, goVap);
        dbContext.Vehicles.AddRange(vehicle1, vehicle2);
        dbContext.Bookings.AddRange(
            new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customerProfile.Id,
                Customer = customerProfile,
                VehicleId = vehicle1.Id,
                Vehicle = vehicle1,
                BranchId = thuDuc.Id,
                Branch = thuDuc,
                BookingDate = new DateOnly(2026, 7, 10),
                StartTime = new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero),
                Status = BookingStatus.Confirmed,
                BasePrice = 120000,
                DiscountAmount = 0,
                FinalPrice = 120000,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customerProfile.Id,
                Customer = customerProfile,
                VehicleId = vehicle1.Id,
                Vehicle = vehicle1,
                BranchId = thuDuc.Id,
                Branch = thuDuc,
                BookingDate = new DateOnly(2026, 7, 15),
                StartTime = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero),
                Status = BookingStatus.Completed,
                BasePrice = 150000,
                DiscountAmount = 10000,
                FinalPrice = 140000,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customerProfile.Id,
                Customer = customerProfile,
                VehicleId = vehicle2.Id,
                Vehicle = vehicle2,
                BranchId = goVap.Id,
                Branch = goVap,
                BookingDate = new DateOnly(2026, 7, 16),
                StartTime = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 7, 16, 13, 0, 0, TimeSpan.Zero),
                Status = BookingStatus.Cancelled,
                BasePrice = 110000,
                DiscountAmount = 0,
                FinalPrice = 110000,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return (user, customerProfile);
    }

    private static BookingService CreateBookingService(AppDbContext dbContext, Guid userId)
    {
        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, UserRole.Customer.ToString())
                },
                "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        return new BookingService(
            dbContext,
            httpContextAccessor,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            new FakeNotificationService(),
            new FakeAudienceService(),
            NullLogger<BookingService>.Instance);
    }

    private sealed class FakeNotificationService : SWP391_AutoWashPro_BE.Service.Notification.IService
    {
        public Task<SWP391_AutoWashPro_BE.Service.Notification.Response.GetNotificationResponse> GetNotification(NotificationType? type, bool? isRead, int page, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<SWP391_AutoWashPro_BE.Service.Notification.Response.UpdateNotificationStatusResponse> UpdateNotificationStatus(SWP391_AutoWashPro_BE.Service.Notification.Request.UpdateNotificationStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public Task SendNotification(SWP391_AutoWashPro_BE.Service.Notification.Request.SendNotificationRequest request)
        {
            return Task.CompletedTask;
        }

        public Task SendNotificationToUser(Guid userId, Guid notificationId, NotificationType type, string title, string content, string? metadata, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAudienceService : SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.IAudienceService
    {
        public Task<int> ProcessBirthdayAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> ProcessInactiveCustomersAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> ProcessAcquisitionAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Response.IssueResult> ProcessTierUpgradeAsync(Guid customerId, Guid newTierId, Guid bookingId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Response.IssueResult
            {
                Status = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Response.IssueStatus.Skipped
            });
        }
    }
}
