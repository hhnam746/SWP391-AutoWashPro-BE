using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using AuthService = SWP391_AutoWashPro_BE.Service.Auth.Service;
using UserService = SWP391_AutoWashPro_BE.Service.User.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class BirthdayProfileRulesTests
{
    [Fact]
    public async Task Register_WithoutBirthday_LeavesBirthdayNull()
    {
        await using var dbContext = CreateDbContext();
        await SeedTierAsync(dbContext);

        var service = CreateAuthService(dbContext);
        var request = CreateRegisterRequest(dateOfBirth: null);

        var result = await service.Register(request);

        var customerProfile = await dbContext.CustomerProfiles.SingleAsync();

        Assert.Equal("User registered successfully!", result);
        Assert.Null(customerProfile.DateOfBirth);
        Assert.Null(customerProfile.DateOfBirthSetAt);
    }

    [Fact]
    public async Task Register_WithBirthday_PersistsBirthdayAndSetTimestamp()
    {
        await using var dbContext = CreateDbContext();
        await SeedTierAsync(dbContext);

        var service = CreateAuthService(dbContext);
        var request = CreateRegisterRequest(new DateOnly(2000, 1, 15));

        await service.Register(request);

        var customerProfile = await dbContext.CustomerProfiles.SingleAsync();

        Assert.Equal(new DateOnly(2000, 1, 15), customerProfile.DateOfBirth);
        Assert.NotNull(customerProfile.DateOfBirthSetAt);
    }

    [Fact]
    public async Task Register_WithFutureBirthday_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();
        await SeedTierAsync(dbContext);

        var service = CreateAuthService(dbContext);
        var request = CreateRegisterRequest(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.Register(request));

        Assert.Contains("future", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateProfile_WhenBirthdayIsNull_SetsBirthdayOnce()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedCustomerAsync(dbContext, dateOfBirth: null);

        var service = CreateUserService(dbContext, seeded.User.Id);
        var request = new SWP391_AutoWashPro_BE.Service.User.Request.UpdateProfileRequest
        {
            DateOfBirth = new DateOnly(1999, 12, 31)
        };

        var result = await service.UpdateProfile(request);

        var profile = await dbContext.CustomerProfiles.SingleAsync(x => x.UserId == seeded.User.Id);

        Assert.Equal("Update customer profile successfully.", result);
        Assert.Equal(new DateOnly(1999, 12, 31), profile.DateOfBirth);
        Assert.NotNull(profile.DateOfBirthSetAt);
    }

    [Fact]
    public async Task UpdateProfile_WhenBirthdayAlreadySetToDifferentValue_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedCustomerAsync(dbContext, new DateOnly(2001, 6, 1));

        var service = CreateUserService(dbContext, seeded.User.Id);
        var request = new SWP391_AutoWashPro_BE.Service.User.Request.UpdateProfileRequest
        {
            DateOfBirth = new DateOnly(2002, 7, 2)
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateProfile(request));
        var profile = await dbContext.CustomerProfiles.SingleAsync(x => x.UserId == seeded.User.Id);

        Assert.Contains("administrator", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new DateOnly(2001, 6, 1), profile.DateOfBirth);
    }

    [Fact]
    public async Task UpdateProfile_WhenBirthdayMatchesExistingValue_ReturnsNoChanges()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedCustomerAsync(dbContext, new DateOnly(2001, 6, 1));
        var originalSetAt = seeded.CustomerProfile.DateOfBirthSetAt;

        var service = CreateUserService(dbContext, seeded.User.Id);
        var request = new SWP391_AutoWashPro_BE.Service.User.Request.UpdateProfileRequest
        {
            DateOfBirth = new DateOnly(2001, 6, 1)
        };

        var result = await service.UpdateProfile(request);
        var profile = await dbContext.CustomerProfiles.SingleAsync(x => x.UserId == seeded.User.Id);

        Assert.Equal("No profile changes detected.", result);
        Assert.Equal(new DateOnly(2001, 6, 1), profile.DateOfBirth);
        Assert.Equal(originalSetAt, profile.DateOfBirthSetAt);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedTierAsync(AppDbContext dbContext)
    {
        dbContext.Tiers.Add(new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Member",
            Level = 1,
            RequiredWashes = 0,
            PriorityBookingDays = 5,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<(User User, CustomerProfile CustomerProfile)> SeedCustomerAsync(
        AppDbContext dbContext,
        DateOnly? dateOfBirth)
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
            FirstName = "Anh",
            LastName = "Nguyen",
            DateOfBirth = dateOfBirth,
            DateOfBirthSetAt = dateOfBirth.HasValue ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.CustomerProfile = customerProfile;

        dbContext.Tiers.Add(tier);
        dbContext.Users.Add(user);
        dbContext.CustomerProfiles.Add(customerProfile);

        await dbContext.SaveChangesAsync();

        return (user, customerProfile);
    }

    private static AuthService CreateAuthService(AppDbContext dbContext)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        return new AuthService(
            dbContext,
            new FakeMediaService(),
            new FakeMailService(),
            NullLogger<AuthService>.Instance,
            new FakeJwtService(),
            configuration,
            new FakeSecurityService(),
            new FakeOtpService());
    }

    private static UserService CreateUserService(AppDbContext dbContext, Guid userId)
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

        return new UserService(
            dbContext,
            httpContextAccessor,
            new FakeSecurityService(),
            new FakeMediaService());
    }

    private static SWP391_AutoWashPro_BE.Service.Auth.Request.RegisterRequest CreateRegisterRequest(DateOnly? dateOfBirth)
    {
        return new SWP391_AutoWashPro_BE.Service.Auth.Request.RegisterRequest
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            Phone = $"09{Random.Shared.NextInt64(10000000, 99999999)}",
            Password = "Strong@123",
            FirstName = "Anh",
            LastName = "Nguyen",
            DateOfBirth = dateOfBirth,
            FaceImages =
            [
                CreateFormFile("face-1.jpg"),
                CreateFormFile("face-2.jpg"),
                CreateFormFile("face-3.jpg")
            ]
        };
    }

    private static IFormFile CreateFormFile(string fileName)
    {
        return new FakeFormFile(fileName, new byte[] { 1, 2, 3 });
    }

    private sealed class FakeMediaService : SWP391_AutoWashPro_BE.Service.MediaService.IService
    {
        public Task<string> UploadImageAsync(IFormFile file)
        {
            return Task.FromResult($"https://example.com/{file.FileName}");
        }
    }

    private sealed class FakeMailService : SWP391_AutoWashPro_BE.Service.MailService.IService
    {
        public Task SendMail(SWP391_AutoWashPro_BE.Service.MailService.MailContent mailContent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityService : SWP391_AutoWashPro_BE.Service.Security.IService
    {
        public string Hash(string password)
        {
            return $"hashed::{password}";
        }

        public bool Verify(string password, string storedHash)
        {
            return storedHash == Hash(password);
        }
    }

    private sealed class FakeJwtService : SWP391_AutoWashPro_BE.Service.JwtService.IService
    {
        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            return "test-token";
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    private sealed class FakeOtpService : SWP391_AutoWashPro_BE.Service.OtpService.IService
    {
        public Task GenerateAndSendOtpAsync(string email)
        {
            return Task.CompletedTask;
        }

        public Task<bool> VerifyOtpAsync(string email, string otpCode)
        {
            return Task.FromResult(true);
        }

        public Task InvalidateOldOtpsAsync(string email)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFormFile : IFormFile
    {
        private readonly byte[] _content;

        public FakeFormFile(string fileName, byte[] content)
        {
            _content = content;
            FileName = fileName;
            Name = "faceImages";
            ContentDisposition = string.Empty;
            ContentType = "image/jpeg";
            Headers = new HeaderDictionary();
            Length = content.Length;
        }

        public string ContentType { get; }
        public string ContentDisposition { get; }
        public IHeaderDictionary Headers { get; }
        public long Length { get; }
        public string Name { get; }
        public string FileName { get; }

        public void CopyTo(Stream target)
        {
            target.Write(_content, 0, _content.Length);
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            return target.WriteAsync(_content, cancellationToken).AsTask();
        }

        public Stream OpenReadStream()
        {
            return new MemoryStream(_content, writable: false);
        }
    }
}
