using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Models;
using Xunit;
using BookingRequest = SWP391_AutoWashPro_BE.Service.Booking.Request;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking.Service;
using NotificationService = SWP391_AutoWashPro_BE.Service.Notification;
using PersonalizedVoucherService = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

namespace SWP391_AutoWashPro_BE.Tests;

public class BookingScheduleWarningTests
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateBooking_CloseBookingAtSameOrDifferentBranch_ReturnsWarning(bool sameBranch)
    {
        await using var fixture = await CreateFixtureAsync(sameBranch);

        var exception = await Assert.ThrowsAsync<BookingScheduleWarningException>(
            () => fixture.Service.CreateBooking(fixture.Request));

        Assert.Equal("BOOKING_TIME_TOO_CLOSE", exception.Warning.Code);
        Assert.Equal("warning", exception.Warning.Severity);
        Assert.Equal(30, exception.Warning.ThresholdMinutes);
        var conflict = Assert.Single(exception.Warning.Conflicts);
        Assert.Equal(fixture.ExistingBookingId, conflict.BookingId);
        Assert.Equal(sameBranch, conflict.IsSameBranch);
        Assert.Equal(30, conflict.GapMinutes);
        Assert.Equal(1, await fixture.DbContext.Bookings.CountAsync());
        Assert.Equal(10_000_000m, (await fixture.DbContext.Wallets.SingleAsync()).Balance);
    }

    [Fact]
    public async Task CreateBooking_WhenConflictIsAcknowledged_CreatesBooking()
    {
        await using var fixture = await CreateFixtureAsync(sameBranch: false);
        fixture.Request.AcknowledgedScheduleConflictIds = [fixture.ExistingBookingId];

        var response = await fixture.Service.CreateBooking(fixture.Request);

        Assert.Equal(BookingStatus.Confirmed, response.Status);
        Assert.Equal(2, await fixture.DbContext.Bookings.CountAsync());
    }

    [Fact]
    public async Task CreateBooking_WhenGapExceedsConfiguredThreshold_CreatesBooking()
    {
        await using var fixture = await CreateFixtureAsync(sameBranch: true, existingStartMinute: 0);

        var response = await fixture.Service.CreateBooking(fixture.Request);

        Assert.Equal(BookingStatus.Confirmed, response.Status);
        Assert.Equal(2, await fixture.DbContext.Bookings.CountAsync());
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task CreateBooking_IgnoresTerminalBookings(BookingStatus status)
    {
        await using var fixture = await CreateFixtureAsync(sameBranch: true, existingStatus: status);

        var response = await fixture.Service.CreateBooking(fixture.Request);

        Assert.Equal(BookingStatus.Confirmed, response.Status);
    }

    [Theory]
    [InlineData("100", 1500)]
    [InlineData("250", 3750)]
    public async Task CreateBooking_WhenRedeemingPoints_UsesConfiguredPointValue(
        string configuredValue,
        int expectedDiscount)
    {
        await using var fixture = await CreateFixtureAsync(
            sameBranch: true,
            existingStartMinute: 0,
            redeemPoint: true,
            totalPoints: 15,
            includeRedeemPointValueConfig: true,
            redeemPointValue: configuredValue);

        var response = await fixture.Service.CreateBooking(fixture.Request);
        var booking = await fixture.DbContext.Bookings
            .SingleAsync(x => x.Id == response.Id);

        Assert.Equal(expectedDiscount, response.DiscountAmount);
        Assert.Equal(100000 - expectedDiscount, response.FinalPrice);
        Assert.Equal(15, booking.RedemAmount);
    }

    [Fact]
    public async Task CreateBooking_WhenRedeemPointValueConfigIsMissing_Throws()
    {
        await using var fixture = await CreateFixtureAsync(
            sameBranch: true,
            existingStartMinute: 0,
            redeemPoint: true,
            totalPoints: 15);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateBooking(fixture.Request));

        Assert.Equal("RedeemPointValue config not found", exception.Message);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task CreateBooking_WhenRedeemPointValueConfigIsInvalid_Throws(string configuredValue)
    {
        await using var fixture = await CreateFixtureAsync(
            sameBranch: true,
            existingStartMinute: 0,
            redeemPoint: true,
            totalPoints: 15,
            includeRedeemPointValueConfig: true,
            redeemPointValue: configuredValue);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateBooking(fixture.Request));

        Assert.Equal("Invalid RedeemPointValue config value", exception.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenNotRedeemingPoints_DoesNotRequirePointValueConfig()
    {
        await using var fixture = await CreateFixtureAsync(
            sameBranch: true,
            existingStartMinute: 0,
            redeemPoint: false);

        var response = await fixture.Service.CreateBooking(fixture.Request);

        Assert.Equal(BookingStatus.Confirmed, response.Status);
        Assert.Equal(0, response.DiscountAmount);
    }

    private static async Task<Fixture> CreateFixtureAsync(
        bool sameBranch,
        int existingStartMinute = 15,
        BookingStatus existingStatus = BookingStatus.Confirmed,
        bool redeemPoint = false,
        int totalPoints = 0,
        bool includeRedeemPointValueConfig = false,
        string redeemPointValue = "100")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var dbContext = new AppDbContext(options);

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var tierId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var existingBranchId = Guid.NewGuid();
        var requestedBranchId = sameBranch ? existingBranchId : Guid.NewGuid();

        var tier = new Tier
        {
            Id = tierId,
            Name = "Test Tier",
            Level = 1,
            RequiredWashes = 0,
            PriorityBookingDays = 30,
            CreatedAt = now
        };
        var user = new User
        {
            Id = userId,
            Phone = "0900000000",
            PasswordHash = "test",
            Role = UserRole.Customer,
            Status = AccountStatus.Active,
            isVerify = true,
            CreatedAt = now
        };
        var customer = new CustomerProfile
        {
            Id = customerId,
            UserId = userId,
            User = user,
            TierId = tierId,
            Tier = tier,
            FirstName = "Test",
            LastName = "Customer",
            TotalPoints = totalPoints,
            CreatedAt = now
        };
        var vehicleType = new VehicleType
        {
            Id = vehicleTypeId,
            TypeName = VehicleTypes.Sedan,
            VehicleSlot = 1,
            SizeLevel = 1,
            CreatedAt = now
        };
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            Customer = customer,
            VehicleTypeId = vehicleTypeId,
            VehicleType = vehicleType,
            LicensePlate = "51A-12345",
            IsActive = true,
            CreatedAt = now
        };
        var existingBranch = CreateBranch(existingBranchId, "Existing Branch", now);
        var requestedBranch = sameBranch
            ? existingBranch
            : CreateBranch(requestedBranchId, "Requested Branch", now);
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Customer = customer,
            Balance = 10_000_000m,
            CreatedAt = now
        };

        dbContext.AddRange(tier, user, customer, vehicleType, vehicle, existingBranch, wallet);
        if (!sameBranch)
        {
            dbContext.Branches.Add(requestedBranch);
        }

        AddConfig(dbContext, "WorkingStartHour", "8");
        AddConfig(dbContext, "WorkingEndHour", "17");
        AddConfig(dbContext, "SlotDurationMinutes", "15");
        AddConfig(dbContext, "SlotBreakMinutes", "0");
        AddConfig(dbContext, "BasePrice", "100000");
        AddConfig(dbContext, "SedanBasePrice", "0");
        AddConfig(dbContext, "SuvBasePrice", "30000");
        AddConfig(dbContext, "PaymentDeposite", "30");
        AddConfig(dbContext, "BookingProximityWarningMinutes", "30");
        if (includeRedeemPointValueConfig)
        {
            AddConfig(dbContext, "RedeemPointValue", redeemPointValue);
        }

        var bookingDate = DateTimeOffset.UtcNow.ToOffset(VietnamOffset).Date.AddDays(2);
        var existingStart = new DateTimeOffset(
            bookingDate.Year,
            bookingDate.Month,
            bookingDate.Day,
            8,
            existingStartMinute,
            0,
            VietnamOffset);
        var existingBookingId = Guid.NewGuid();
        dbContext.Bookings.Add(new Booking
        {
            Id = existingBookingId,
            CustomerId = customerId,
            Customer = customer,
            VehicleId = vehicleId,
            Vehicle = vehicle,
            BranchId = existingBranchId,
            Branch = existingBranch,
            BookingDate = DateOnly.FromDateTime(bookingDate),
            StartTime = existingStart.ToUniversalTime(),
            EndTime = existingStart.AddMinutes(15).ToUniversalTime(),
            Status = existingStatus,
            BasePrice = 100000,
            FinalPrice = 100000,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync();

        var requestedStart = new DateTimeOffset(
            bookingDate.Year,
            bookingDate.Month,
            bookingDate.Day,
            9,
            0,
            0,
            VietnamOffset);
        var request = new BookingRequest.CreateBookingRequest
        {
            BranchId = requestedBranchId,
            VehicleId = vehicleId,
            VoucherId = null,
            BookingDate = DateOnly.FromDateTime(bookingDate),
            StartTime = requestedStart,
            redemPoint = redeemPoint
        };

        var service = new BookingService(
            dbContext,
            CreateHttpContextAccessor(userId),
            new UnusedServiceScopeFactory(),
            new UnusedNotificationService(),
            new UnusedAudienceService(),
            NullLogger<BookingService>.Instance);

        return new Fixture(dbContext, service, request, existingBookingId);
    }

    private static Branch CreateBranch(Guid id, string name, DateTimeOffset now)
    {
        return new Branch
        {
            Id = id,
            Name = name,
            Address = $"{name} address",
            IsActive = true,
            CreatedAt = now
        };
    }

    private static void AddConfig(AppDbContext dbContext, string key, string value)
    {
        dbContext.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = key,
            ConfigValue = value,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test");
        return new FixedHttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private sealed class FixedHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed record Fixture(
        AppDbContext DbContext,
        BookingService Service,
        BookingRequest.CreateBookingRequest Request,
        Guid ExistingBookingId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }
    }

    private sealed class UnusedServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new NotSupportedException();
    }

    private sealed class UnusedNotificationService : NotificationService.IService
    {
        public Task<NotificationService.Response.GetNotificationResponse> GetNotification(
            NotificationType? type,
            bool? isRead,
            int page,
            int pageSize) => throw new NotSupportedException();

        public Task<NotificationService.Response.UpdateNotificationStatusResponse> UpdateNotificationStatus(
            NotificationService.Request.UpdateNotificationStatusRequest request) =>
            throw new NotSupportedException();

        public Task SendNotification(NotificationService.Request.SendNotificationRequest request) =>
            throw new NotSupportedException();

        public Task SendNotificationToUser(
            Guid userId,
            Guid notificationId,
            NotificationType type,
            string title,
            string content,
            string? metadata,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedAudienceService : PersonalizedVoucherService.IAudienceService
    {
        public Task<int> ProcessBirthdayAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ProcessInactiveCustomersAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ProcessAcquisitionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PersonalizedVoucherService.Response.IssueResult> ProcessTierUpgradeAsync(
            Guid customerId,
            Guid newTierId,
            Guid bookingId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
