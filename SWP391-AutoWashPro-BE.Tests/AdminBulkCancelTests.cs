using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using AdminRequest = SWP391_AutoWashPro_BE.Service.Admin.Request;
using AdminService = SWP391_AutoWashPro_BE.Service.Admin.Service;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class AdminBulkCancelTests
{
    [Fact]
    public async Task CancelBookingFromTo_ShouldCancelOnlyConfirmedBookings_AndRefundDeposit()
    {
        await using var dbContext = CreateDbContext();
        var seed = SeedBulkCancellationData(dbContext);
        await dbContext.SaveChangesAsync();

        var service = new AdminService(
            dbContext,
            new HttpContextAccessor(),
            null!,
            null!,
            NullLogger<AdminService>.Instance);

        var result = await service.CancelBookingFromTo(new AdminRequest.CancelBookingFromTo
        {
            BranchId = seed.Branch.Id,
            FromDate = seed.RangeFromDate,
            ToDate = seed.RangeToDate
        });

        Assert.Equal(seed.Branch.Id, result.BranchId);
        Assert.Equal(3, result.TotalBookingCount);
        Assert.Equal(2, result.CancelledBookingCount);
        Assert.Equal(2, result.RefundedBookingCount);
        Assert.Equal(1, result.SkippedBookingCount);
        Assert.Equal(60m, result.TotalRefundAmount);

        var confirmedBooking1 = await dbContext.Bookings.FirstAsync(x => x.Id == seed.ConfirmedBooking1.Id);
        var confirmedBooking2 = await dbContext.Bookings.FirstAsync(x => x.Id == seed.ConfirmedBooking2.Id);
        var checkInBooking = await dbContext.Bookings.FirstAsync(x => x.Id == seed.CheckInBooking.Id);

        Assert.Equal(BookingStatus.Cancelled, confirmedBooking1.Status);
        Assert.Equal(BookingStatus.Cancelled, confirmedBooking2.Status);
        Assert.Equal(BookingStatus.CheckIn, checkInBooking.Status);

        var wallet1 = await dbContext.Wallets.FirstAsync(x => x.Id == seed.ConfirmedWallet1.Id);
        var wallet2 = await dbContext.Wallets.FirstAsync(x => x.Id == seed.ConfirmedWallet2.Id);
        var wallet3 = await dbContext.Wallets.FirstAsync(x => x.Id == seed.CheckInWallet.Id);

        Assert.Equal(100m, wallet1.Balance);
        Assert.Equal(100m, wallet2.Balance);
        Assert.Equal(70m, wallet3.Balance);

        var confirmedBookingIds = new[]
        {
            seed.ConfirmedBooking1.Id,
            seed.ConfirmedBooking2.Id
        };

        var refundCount = await dbContext.Transactions.CountAsync(x =>
            x.BookingId.HasValue &&
            confirmedBookingIds.Contains(x.BookingId.Value) &&
            x.Type == TransactionType.Refund);

        Assert.Equal(2, refundCount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static BulkSeedData SeedBulkCancellationData(AppDbContext dbContext)
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Bulk Cancel Branch",
            Address = "Address",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var fromDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(1).UtcDateTime);
        var confirmedStart1 = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9))), TimeSpan.Zero);
        var confirmedStart2 = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(11))), TimeSpan.Zero);
        var checkInStart = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(13))), TimeSpan.Zero);

        var confirmed1 = CreateBookingScenario(dbContext, branch, confirmedStart1, BookingStatus.Confirmed, 30m);
        var confirmed2 = CreateBookingScenario(dbContext, branch, confirmedStart2, BookingStatus.Confirmed, 30m);
        var checkIn = CreateBookingScenario(dbContext, branch, checkInStart, BookingStatus.CheckIn, 30m);

        dbContext.Add(branch);
        dbContext.AddRange(
            confirmed1.User,
            confirmed1.Tier,
            confirmed1.CustomerProfile,
            confirmed1.Wallet,
            confirmed1.Booking,
            confirmed1.DepositTransaction,
            confirmed2.User,
            confirmed2.Tier,
            confirmed2.CustomerProfile,
            confirmed2.Wallet,
            confirmed2.Booking,
            confirmed2.DepositTransaction,
            checkIn.User,
            checkIn.Tier,
            checkIn.CustomerProfile,
            checkIn.Wallet,
            checkIn.Booking,
            checkIn.DepositTransaction);

        return new BulkSeedData(
            branch,
            fromDate,
            fromDate,
            confirmed1.Wallet,
            confirmed2.Wallet,
            checkIn.Wallet,
            confirmed1.Booking,
            confirmed2.Booking,
            checkIn.Booking);
    }

    private static BookingScenario CreateBookingScenario(
        AppDbContext dbContext,
        Branch branch,
        DateTimeOffset bookingStartTime,
        BookingStatus bookingStatus,
        decimal depositAmount)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.com",
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
            Balance = 100m - depositAmount,
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
            Description = "Deposit Booking",
            TransactionDate = DateTime.UtcNow,
            CustomerId = customerProfile.Id,
            CustomerProfile = customerProfile,
            BookingId = booking.Id,
            Booking = booking,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return new BookingScenario(user, tier, customerProfile, wallet, booking, depositTransaction);
    }

    private sealed record BookingScenario(
        User User,
        Tier Tier,
        CustomerProfile CustomerProfile,
        Wallet Wallet,
        Booking Booking,
        Transaction DepositTransaction);

    private sealed record BulkSeedData(
        Branch Branch,
        DateOnly RangeFromDate,
        DateOnly RangeToDate,
        Wallet ConfirmedWallet1,
        Wallet ConfirmedWallet2,
        Wallet CheckInWallet,
        Booking ConfirmedBooking1,
        Booking ConfirmedBooking2,
        Booking CheckInBooking);
}
