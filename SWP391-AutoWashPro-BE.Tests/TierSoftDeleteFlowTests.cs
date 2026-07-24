using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using AdminRequest = SWP391_AutoWashPro_BE.Service.Admin.Request;
using AdminService = SWP391_AutoWashPro_BE.Service.Admin.Service;
using AuthRequest = SWP391_AutoWashPro_BE.Service.Auth.Request;
using AuthService = SWP391_AutoWashPro_BE.Service.Auth.Service;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking.Service;
using LoyaltyService = SWP391_AutoWashPro_BE.Service.Loyalty.Service;
using PersonalizedVoucherResponse = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Response;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class TierSoftDeleteFlowTests
{
    [Fact]
    public async Task CheckIn_WithSevenWashes_IgnoresDeletedSilverAndRemainsMember()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedBookingScenarioAsync(dbContext, totalWashes: 7, BookingStatus.Confirmed);
        var audienceService = new RecordingAudienceService();
        var bookingService = CreateBookingService(dbContext, audienceService);

        await bookingService.CheckInBookingByAdmin(scenario.BookingId);

        var customer = await dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .SingleAsync(x => x.Id == scenario.CustomerId);
        Assert.Equal(8, customer.TotalWashes);
        Assert.Equal(70, customer.TotalPoints);
        Assert.Equal(scenario.MemberTierId, customer.TierId);
        Assert.Equal("Member", customer.Tier.Name);
        Assert.DoesNotContain(
            dbContext.Notifications,
            notification => notification.Type == NotificationType.TierUpgraded);
        Assert.Empty(audienceService.TierUpgradeCalls);

        var loyalty = await CreateLoyaltyService(dbContext, scenario.UserId).GetMyLoyaltyOverview();
        Assert.Equal("Silver", loyalty.NextTier?.Name);
        Assert.Equal(13, loyalty.NextTier?.RequiredWashes);
        Assert.Equal(5, loyalty.NextTier?.RemainingWashes);
    }

    [Fact]
    public async Task CheckIn_ReachingThirteenWashes_UpgradesToActiveSilverOnce()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedBookingScenarioAsync(dbContext, totalWashes: 12, BookingStatus.Confirmed);
        var audienceService = new RecordingAudienceService();
        var bookingService = CreateBookingService(dbContext, audienceService);

        await bookingService.CheckInBookingByAdmin(scenario.BookingId);

        var customer = await dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .SingleAsync(x => x.Id == scenario.CustomerId);
        Assert.Equal(13, customer.TotalWashes);
        Assert.Equal(scenario.SilverTierId, customer.TierId);
        Assert.Equal("Silver", customer.Tier.Name);

        var notification = Assert.Single(
            dbContext.Notifications.Where(item => item.Type == NotificationType.TierUpgraded));
        Assert.Contains("Member to Silver", notification.Content);

        var audienceCall = Assert.Single(audienceService.TierUpgradeCalls);
        Assert.Equal(scenario.CustomerId, audienceCall.CustomerId);
        Assert.Equal(scenario.SilverTierId, audienceCall.TierId);
        Assert.Equal(scenario.BookingId, audienceCall.BookingId);
    }

    [Fact]
    public async Task CompleteBookingByAdmin_IgnoresEligibleDeletedHigherTiers()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedBookingScenarioAsync(dbContext, totalWashes: 7, BookingStatus.InProgress);
        var adminService = new AdminService(
            dbContext,
            new HttpContextAccessor(),
            null!,
            null!,
            NullLogger<AdminService>.Instance);

        await adminService.CompleteBookingByAdmin(
            scenario.BookingId,
            new AdminRequest.CompleteBookingByAdminRequest());

        var customer = await dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .SingleAsync(x => x.Id == scenario.CustomerId);
        Assert.Equal(8, customer.TotalWashes);
        Assert.Equal(scenario.MemberTierId, customer.TierId);
        Assert.DoesNotContain(
            dbContext.Notifications,
            notification => notification.Type == NotificationType.TierUpgraded);
    }

    [Fact]
    public async Task Register_AssignsLowestActiveTierInsteadOfDeletedTier()
    {
        await using var dbContext = CreateDbContext();
        var deletedLegacyTier = CreateTier("Legacy", level: 0, requiredWashes: 0, isDeleted: true);
        var memberTier = CreateTier("Member", level: 1, requiredWashes: 0);
        dbContext.Tiers.AddRange(deletedLegacyTier, memberTier);
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(
            dbContext,
            new FakeMediaService(),
            new FakeMailService(),
            NullLogger<AuthService>.Instance,
            new FakeJwtService(),
            new ConfigurationBuilder().Build(),
            new FakeSecurityService(),
            new FakeOtpService());

        await authService.Register(new AuthRequest.RegisterRequest
        {
            Email = "new.customer@example.com",
            Phone = "0912345678",
            Password = "Strong@123",
            FirstName = "New",
            LastName = "Customer",
            FaceImages =
            [
                CreateFormFile("face-1.jpg"),
                CreateFormFile("face-2.jpg"),
                CreateFormFile("face-3.jpg")
            ]
        });

        var customer = await dbContext.CustomerProfiles.SingleAsync();
        Assert.Equal(memberTier.Id, customer.TierId);
        Assert.NotEqual(deletedLegacyTier.Id, customer.TierId);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<BookingScenario> SeedBookingScenarioAsync(
        AppDbContext dbContext,
        int totalWashes,
        BookingStatus bookingStatus)
    {
        var member = CreateTier("Member", level: 1, requiredWashes: 0);
        var silver = CreateTier("Silver", level: 2, requiredWashes: 13);
        var deletedSilver = CreateTier("Bạc", level: 2, requiredWashes: 5, isDeleted: true);
        var gold = CreateTier("Gold", level: 3, requiredWashes: 24);
        var deletedGold = CreateTier("Vàng", level: 3, requiredWashes: 8, isDeleted: true);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"tier-test-{Guid.NewGuid():N}@example.com",
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            PasswordHash = "hashed-password",
            Role = UserRole.Customer,
            Status = AccountStatus.Active,
            isVerify = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var customer = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TierId = member.Id,
            Tier = member,
            FirstName = "Tier",
            LastName = "Customer",
            TotalPoints = totalWashes * 10 - 10,
            TotalWashes = totalWashes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Tier Test Branch",
            Address = "1 Test Street",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            LicensePlate = $"TEST-{Random.Shared.Next(10000, 99999)}",
            Brand = "Test",
            Model = "Vehicle",
            IsActive = true,
            VehicleTypeId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            Balance = 1_000_000,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            BranchId = branch.Id,
            Branch = branch,
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            EndTime = DateTimeOffset.UtcNow.AddMinutes(14),
            Status = bookingStatus,
            BasePrice = 100_000,
            DiscountAmount = 0,
            FinalPrice = 100_000,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AddRange(
            member,
            silver,
            deletedSilver,
            gold,
            deletedGold,
            user,
            customer,
            branch,
            vehicle,
            wallet,
            booking);
        dbContext.SystemConfigs.AddRange(
            CreateConfig("CancelTimeMinutes", "30"),
            CreateConfig("PaymentDeposite", "0"),
            CreateConfig("BonusPoint", "10"));
        await dbContext.SaveChangesAsync();

        return new BookingScenario(
            user.Id,
            customer.Id,
            booking.Id,
            member.Id,
            silver.Id);
    }

    private static BookingService CreateBookingService(
        AppDbContext dbContext,
        RecordingAudienceService audienceService) =>
        new(
            dbContext,
            new HttpContextAccessor(),
            null!,
            null!,
            audienceService,
            NullLogger<BookingService>.Instance);

    private static LoyaltyService CreateLoyaltyService(
        AppDbContext dbContext,
        Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "TestAuth");
        return new LoyaltyService(
            dbContext,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            });
    }

    private static Tier CreateTier(
        string name,
        int level,
        int requiredWashes,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = level,
            RequiredWashes = requiredWashes,
            PriorityBookingDays = 0,
            IsDeleted = isDeleted,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static SystemConfig CreateConfig(string key, string value) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConfigKey = key,
            ConfigValue = value,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static IFormFile CreateFormFile(string fileName) =>
        new FakeFormFile(fileName);

    private sealed class RecordingAudienceService :
        SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.IAudienceService
    {
        public List<TierUpgradeCall> TierUpgradeCalls { get; } = [];

        public Task<int> ProcessBirthdayAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> ProcessInactiveCustomersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> ProcessAcquisitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<PersonalizedVoucherResponse.IssueResult> ProcessTierUpgradeAsync(
            Guid customerId,
            Guid newTierId,
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            TierUpgradeCalls.Add(new TierUpgradeCall(customerId, newTierId, bookingId));
            return Task.FromResult(new PersonalizedVoucherResponse.IssueResult
            {
                Status = PersonalizedVoucherResponse.IssueStatus.Skipped
            });
        }
    }

    private sealed class FakeMediaService : SWP391_AutoWashPro_BE.Service.MediaService.IService
    {
        public Task<string> UploadImageAsync(IFormFile file) =>
            Task.FromResult($"https://example.test/{file.FileName}");
    }

    private sealed class FakeFormFile(string fileName) : IFormFile
    {
        private static readonly byte[] Content = [1, 2, 3];

        public string ContentType => "image/jpeg";
        public string ContentDisposition => string.Empty;
        public IHeaderDictionary Headers { get; } = null!;
        public long Length => Content.Length;
        public string Name => "faceImages";
        public string FileName => fileName;

        public void CopyTo(Stream target) => target.Write(Content);

        public Task CopyToAsync(
            Stream target,
            CancellationToken cancellationToken = default) =>
            target.WriteAsync(Content, cancellationToken).AsTask();

        public Stream OpenReadStream() => new MemoryStream(Content);
    }

    private sealed class FakeMailService : SWP391_AutoWashPro_BE.Service.MailService.IService
    {
        public Task SendMail(
            SWP391_AutoWashPro_BE.Service.MailService.MailContent mailContent,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeJwtService : SWP391_AutoWashPro_BE.Service.JwtService.IService
    {
        public string GenerateAccessToken(IEnumerable<Claim> claims) => "test-token";

        public ClaimsPrincipal ValidateToken(string token) => new();
    }

    private sealed class FakeSecurityService : SWP391_AutoWashPro_BE.Service.Security.IService
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string storedHash) =>
            storedHash == $"hashed:{password}";
    }

    private sealed class FakeOtpService : SWP391_AutoWashPro_BE.Service.OtpService.IService
    {
        public Task GenerateAndSendOtpAsync(string email) => Task.CompletedTask;

        public Task<bool> VerifyOtpAsync(string email, string otpCode) =>
            Task.FromResult(true);

        public Task InvalidateOldOtpsAsync(string email) => Task.CompletedTask;
    }

    private sealed record BookingScenario(
        Guid UserId,
        Guid CustomerId,
        Guid BookingId,
        Guid MemberTierId,
        Guid SilverTierId);

    private sealed record TierUpgradeCall(
        Guid CustomerId,
        Guid TierId,
        Guid BookingId);
}
