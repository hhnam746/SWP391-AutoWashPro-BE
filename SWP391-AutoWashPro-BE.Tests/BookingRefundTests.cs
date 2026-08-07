using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using AdminRequest = SWP391_AutoWashPro_BE.Service.Admin.Request;
using AdminService = SWP391_AutoWashPro_BE.Service.Admin.Service;
using BookingRequest = SWP391_AutoWashPro_BE.Service.Booking.Request;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class BookingRefundTests
{
    [Fact]
    public async Task CancelBooking_ShouldNotCreateRefundTransaction_WhenCustomerCancelsBeforeDeadline()
    {
        await using var dbContext = CreateDbContext();
        var seed = SeedCustomerBookingData(dbContext, DateTimeOffset.UtcNow.AddHours(2), 30m);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(
            dbContext,
            CreateHttpContextAccessor(seed.User.Id),
            null!,
            null!,
            null!,
            NullLogger<BookingService>.Instance);

        var result = await service.CancelBooking(seed.Booking.Id, new BookingRequest.CancelBookingRequest
        {
            Reason = "Khong the den dung gio"
        });

        Assert.False(result.RefundApplied);
        Assert.Equal(0m, result.RefundAmount);
        Assert.Null(result.RefundTransactionId);
        Assert.Equal("customer_cancel_before_deadline", result.RefundReasonCode);

        var wallet = await dbContext.Wallets.FirstAsync(x => x.Id == seed.Wallet.Id);
        var refundCount = await dbContext.Transactions.CountAsync(x =>
            x.BookingId == seed.Booking.Id &&
            x.Type == TransactionType.Refund);

        Assert.Equal(70m, wallet.Balance);
        Assert.Equal(0, refundCount);
    }

    [Fact]
    public async Task CancelBooking_ShouldCancelWithoutRefund_WhenCustomerCancelsAfterDeadline()
    {
        await using var dbContext = CreateDbContext();
        var seed = SeedCustomerBookingData(dbContext, DateTimeOffset.UtcNow.AddMinutes(30), 30m);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(
            dbContext,
            CreateHttpContextAccessor(seed.User.Id),
            null!,
            null!,
            null!,
            NullLogger<BookingService>.Instance);

        var result = await service.CancelBooking(seed.Booking.Id, new BookingRequest.CancelBookingRequest
        {
            Reason = "Huy sat gio"
        });

        Assert.False(result.RefundApplied);
        Assert.Equal(0m, result.RefundAmount);
        Assert.Null(result.RefundTransactionId);
        Assert.Equal("customer_cancel_after_deadline", result.RefundReasonCode);

        var wallet = await dbContext.Wallets.FirstAsync(x => x.Id == seed.Wallet.Id);
        var refundCount = await dbContext.Transactions.CountAsync(x =>
            x.BookingId == seed.Booking.Id &&
            x.Type == TransactionType.Refund);

        Assert.Equal(70m, wallet.Balance);
        Assert.Equal(0, refundCount);
    }

    [Fact]
    public async Task CancelBookingByAdmin_ShouldRefundDepositToWallet_WhenBookingIsPending()
    {
        await using var dbContext = CreateDbContext();
        var seed = SeedCustomerBookingData(
            dbContext,
            DateTimeOffset.UtcNow.AddMinutes(30),
            30m,
            BookingStatus.Pending);
        await dbContext.SaveChangesAsync();

        var service = new AdminService(
            dbContext,
            new HttpContextAccessor(),
            null!,
            null!,
            NullLogger<AdminService>.Instance);

        var result = await service.CancelBookingByAdmin(seed.Booking.Id, new AdminRequest.CancelBookingByAdminRequest
        {
            Reason = "Branch gap su co"
        });

        Assert.True(result.RefundApplied);
        Assert.Equal(30m, result.RefundAmount);
        Assert.NotNull(result.RefundTransactionId);
        Assert.Equal("admin_cancel", result.RefundReasonCode);

        var wallet = await dbContext.Wallets.FirstAsync(x => x.Id == seed.Wallet.Id);
        var refundTransaction = await dbContext.Transactions.FirstAsync(x =>
            x.Id == result.RefundTransactionId &&
            x.Type == TransactionType.Refund);

        Assert.Equal(100m, wallet.Balance);
        Assert.Equal(TransactionStatus.Succeeded, refundTransaction.Status);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test");

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static SeedData SeedCustomerBookingData(
        AppDbContext dbContext,
        DateTimeOffset bookingStartTime,
        decimal depositAmount,
        BookingStatus bookingStatus = BookingStatus.Confirmed)
    {
        const decimal startingBalance = 100m;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            Phone = "0900000001",
            PasswordHash = "hash",
            Role = UserRole.Customer,
            Status = AccountStatus.Active,
            isVerify = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var tier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Member",
            Level = 1,
            RequiredWashes = 0,
            PriorityBookingDays = 30,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var customerProfile = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TierId = tier.Id,
            Tier = tier,
            FirstName = "Auto",
            LastName = "Wash",
            TotalPoints = 0,
            TotalWashes = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            Customer = customerProfile,
            Balance = startingBalance - depositAmount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Branch A",
            Address = "Address",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            Customer = customerProfile,
            VehicleId = Guid.NewGuid(),
            BranchId = branch.Id,
            Branch = branch,
            BookingDate = DateOnly.FromDateTime(bookingStartTime.UtcDateTime),
            StartTime = bookingStartTime,
            EndTime = bookingStartTime.AddMinutes(60),
            Status = bookingStatus,
            BasePrice = 100m,
            DiscountAmount = 0m,
            FinalPrice = 100m,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var depositTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = depositAmount,
            Type = TransactionType.Deposit,
            Description = "Deposite Booking",
            TransactionDate = DateTime.UtcNow,
            CustomerId = customerProfile.Id,
            CustomerProfile = customerProfile,
            BookingId = booking.Id,
            Booking = booking,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var cancellationDeadlineConfig = new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = "CancellationDeadlineHours",
            ConfigValue = "1",
            Description = "Cancellation deadline",
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AddRange(
            user,
            tier,
            customerProfile,
            wallet,
            branch,
            booking,
            depositTransaction,
            cancellationDeadlineConfig);

        return new SeedData(user, wallet, booking);
    }

    private sealed record SeedData(User User, Wallet Wallet, Booking Booking);
}
