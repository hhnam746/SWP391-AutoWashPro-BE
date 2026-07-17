using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;
using Xunit;
using MailService = SWP391_AutoWashPro_BE.Service.MailService;
using NotificationService = SWP391_AutoWashPro_BE.Service.Notification;
using PersonalizedVoucherService = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Service;

namespace SWP391_AutoWashPro_BE.Tests;

public class PersonalizedVoucherPostgresTests
{
    [Fact]
    public async Task Issue_CreatesPromotionVoucherWithoutChangingPointsOrRewardQuantity()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var reward = await AddRewardAsync(dbContext, seed.TierId, quantity: 5);
        var service = CreateIssuanceService(dbContext);

        var result = await service.TryIssuePersonalizedVoucherAsync(
            seed.CustomerId,
            seed.RuleId,
            PersonalizedVoucherTriggerType.Birthday,
            "BIRTHDAY:2026",
            null);

        Assert.Equal(Response.IssueStatus.Issued, result.Status);
        var voucher = await dbContext.Vouchers.SingleAsync(x => x.Id == result.VoucherId);
        Assert.Null(voucher.RewardId);
        Assert.Equal(seed.PromotionId, voucher.PromotionId);
        Assert.True(voucher.ExpiresAt <= seed.PromotionEndDate);
        Assert.Equal(1000, await dbContext.CustomerProfiles
            .Where(x => x.Id == seed.CustomerId)
            .Select(x => x.TotalPoints)
            .SingleAsync());
        Assert.Equal(5, await dbContext.Rewards
            .Where(x => x.Id == reward.Id)
            .Select(x => x.QuantityAvailable)
            .SingleAsync());
    }

    [Fact]
    public async Task RerunAndConcurrentContexts_CreateOnlyOneVoucherAndIssuance()
    {
        var connectionString = RequireConnectionString();
        await using (var setupContext = CreateDbContext(connectionString))
        {
            await ResetDatabaseAsync(setupContext);
            var seed = await SeedAsync(setupContext, PersonalizedVoucherTriggerType.Birthday);

            await using var firstContext = CreateDbContext(connectionString);
            await using var secondContext = CreateDbContext(connectionString);
            var firstService = CreateIssuanceService(firstContext);
            var secondService = CreateIssuanceService(secondContext);

            var results = await Task.WhenAll(
                firstService.TryIssuePersonalizedVoucherAsync(
                    seed.CustomerId,
                    seed.RuleId,
                    PersonalizedVoucherTriggerType.Birthday,
                    "BIRTHDAY:2026",
                    null),
                secondService.TryIssuePersonalizedVoucherAsync(
                    seed.CustomerId,
                    seed.RuleId,
                    PersonalizedVoucherTriggerType.Birthday,
                    "BIRTHDAY:2026",
                    null));

            Assert.Contains(results, x => x.Status == Response.IssueStatus.Issued);
            Assert.Contains(results, x => x.Status == Response.IssueStatus.AlreadyIssued);
        }

        await using var assertionContext = CreateDbContext(connectionString);
        Assert.Equal(1, await assertionContext.PersonalizedVoucherIssuances.CountAsync());
        Assert.Equal(1, await assertionContext.Vouchers.CountAsync());
    }

    [Theory]
    [InlineData(AccountStatus.Locked, true)]
    [InlineData(AccountStatus.Inactive, true)]
    [InlineData(AccountStatus.Active, false)]
    public async Task Issue_SkipsLockedInactiveOrUnverifiedCustomer(
        AccountStatus status,
        bool isVerified)
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(
            dbContext,
            PersonalizedVoucherTriggerType.Birthday,
            status,
            isVerified);
        var service = CreateIssuanceService(dbContext);

        var result = await service.TryIssuePersonalizedVoucherAsync(
            seed.CustomerId,
            seed.RuleId,
            PersonalizedVoucherTriggerType.Birthday,
            Guid.NewGuid().ToString(),
            null);

        Assert.Equal(Response.IssueStatus.Skipped, result.Status);
        Assert.Empty(dbContext.Vouchers);
    }

    [Fact]
    public async Task Issue_SkipsInactiveExpiredAndTierMismatchedPromotion()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var service = CreateIssuanceService(dbContext);
        var promotion = await dbContext.Promotions.SingleAsync(x => x.Id == seed.PromotionId);

        promotion.IsActive = false;
        await dbContext.SaveChangesAsync();
        Assert.Equal(Response.IssueStatus.Skipped, (await service.TryIssuePersonalizedVoucherAsync(
            seed.CustomerId, seed.RuleId, PersonalizedVoucherTriggerType.Birthday, "inactive", null)).Status);

        promotion.IsActive = true;
        promotion.EndDate = DateTimeOffset.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();
        Assert.Equal(Response.IssueStatus.Skipped, (await service.TryIssuePersonalizedVoucherAsync(
            seed.CustomerId, seed.RuleId, PersonalizedVoucherTriggerType.Birthday, "expired", null)).Status);

        promotion.EndDate = DateTimeOffset.UtcNow.AddDays(30);
        promotion.IsGlobal = false;
        await dbContext.SaveChangesAsync();
        Assert.Equal(Response.IssueStatus.Skipped, (await service.TryIssuePersonalizedVoucherAsync(
            seed.CustomerId, seed.RuleId, PersonalizedVoucherTriggerType.Birthday, "tier", null)).Status);

        dbContext.PromotionTiers.Add(new PromotionTier
        {
            Id = Guid.NewGuid(),
            PromotionId = promotion.Id,
            TierId = seed.TierId,
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        Assert.Equal(Response.IssueStatus.Skipped, (await service.TryIssuePersonalizedVoucherAsync(
            seed.CustomerId,
            seed.RuleId,
            PersonalizedVoucherTriggerType.Birthday,
            "tier-soft-deleted",
            null)).Status);

        Assert.Empty(dbContext.Vouchers);
    }

    [Fact]
    public async Task BirthdayBatch_IssuesOnlyOnceForTheCurrentYear()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTimeOffset.UtcNow,
            "Asia/Ho_Chi_Minh").DateTime);
        await SeedAsync(
            dbContext,
            PersonalizedVoucherTriggerType.Birthday,
            dateOfBirth: new DateOnly(1999, localToday.Month, localToday.Day));
        var audienceService = CreateAudienceService(dbContext, new RecordingDeliveryService());

        Assert.Equal(1, await audienceService.ProcessBirthdayAsync());
        Assert.Equal(0, await audienceService.ProcessBirthdayAsync());
        Assert.Equal(1, await dbContext.PersonalizedVoucherIssuances.CountAsync());
    }

    [Fact]
    public async Task InactiveBatch_SkipsNullLoginSelectsLargestThresholdAndDoesNotRepeat()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(
            dbContext,
            PersonalizedVoucherTriggerType.InactiveCustomer,
            lastLoginAt: DateTimeOffset.UtcNow.AddDays(-40),
            thresholdDays: 30);
        await AddCustomerAsync(dbContext, seed.TierId, lastLoginAt: null);
        dbContext.PersonalizedPromotionRules.Add(CreateRule(
            seed.PromotionId,
            PersonalizedVoucherTriggerType.InactiveCustomer,
            thresholdDays: 7,
            priority: 100));
        await dbContext.SaveChangesAsync();
        var audienceService = CreateAudienceService(dbContext, new RecordingDeliveryService());

        Assert.Equal(1, await audienceService.ProcessInactiveCustomersAsync());
        Assert.Equal(0, await audienceService.ProcessInactiveCustomersAsync());

        var issuance = await dbContext.PersonalizedVoucherIssuances
            .Include(x => x.PromotionRule)
            .SingleAsync();
        Assert.Equal(seed.CustomerId, issuance.CustomerId);
        Assert.Equal(30, issuance.PromotionRule.ThresholdDays);
    }

    [Fact]
    public async Task AcquisitionBatch_PrefersWelcomeThenAllowsNoFirstBookingAfterWelcomeExpires()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(
            dbContext,
            PersonalizedVoucherTriggerType.Welcome,
            verifiedAt: DateTimeOffset.UtcNow.AddDays(-10),
            userCreatedAt: DateTimeOffset.UtcNow.AddDays(-20));
        dbContext.PersonalizedPromotionRules.Add(CreateRule(
            seed.PromotionId,
            PersonalizedVoucherTriggerType.NoFirstBooking,
            thresholdDays: 7,
            priority: 100));
        await dbContext.SaveChangesAsync();
        var audienceService = CreateAudienceService(dbContext, new RecordingDeliveryService());

        Assert.Equal(1, await audienceService.ProcessAcquisitionAsync());

        var issuance = await dbContext.PersonalizedVoucherIssuances.SingleAsync();
        Assert.Equal(PersonalizedVoucherTriggerType.Welcome, issuance.TriggerType);
        Assert.Equal(1, await dbContext.Vouchers.CountAsync(x =>
            x.CustomerId == seed.CustomerId && x.Status == VoucherStatus.Active));

        var welcomeVoucher = await dbContext.Vouchers.SingleAsync();
        welcomeVoucher.Status = VoucherStatus.Expired;
        welcomeVoucher.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        Assert.Equal(1, await audienceService.ProcessAcquisitionAsync());
        Assert.Equal(2, await dbContext.PersonalizedVoucherIssuances.CountAsync());
        Assert.Equal(1, await dbContext.PersonalizedVoucherIssuances.CountAsync(x =>
            x.TriggerType == PersonalizedVoucherTriggerType.NoFirstBooking));
        Assert.Equal(1, await dbContext.Vouchers.CountAsync(x =>
            x.CustomerId == seed.CustomerId &&
            x.Status == VoucherStatus.Active &&
            x.ExpiresAt > DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task TierUpgradeRerun_DoesNotCreateASecondVoucher()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.TierUpgrade);
        var audienceService = CreateAudienceService(dbContext, new RecordingDeliveryService());
        var bookingId = Guid.NewGuid();

        var first = await audienceService.ProcessTierUpgradeAsync(
            seed.CustomerId,
            seed.TierId,
            bookingId);
        var second = await audienceService.ProcessTierUpgradeAsync(
            seed.CustomerId,
            seed.TierId,
            bookingId);

        Assert.Equal(Response.IssueStatus.Issued, first.Status);
        Assert.Equal(Response.IssueStatus.AlreadyIssued, second.Status);
        Assert.Equal(1, await dbContext.Vouchers.CountAsync());
    }

    [Fact]
    public async Task DeliveryFailure_DoesNotRollbackVoucherAndStopsAtConfiguredAttemptLimit()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(
            dbContext,
            PersonalizedVoucherTriggerType.InactiveCustomer,
            thresholdDays: 30,
            sendNotification: true,
            sendEmail: true);
        var issueResult = await CreateIssuanceService(dbContext).TryIssuePersonalizedVoucherAsync(
            seed.CustomerId,
            seed.RuleId,
            PersonalizedVoucherTriggerType.InactiveCustomer,
            "INACTIVE:30:123",
            null);
        var deliveryService = new DeliveryService(
            dbContext,
            new FailingNotificationService(),
            new FailingMailService(),
            Microsoft.Extensions.Options.Options.Create(new SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Options
            {
                BatchSize = 10,
                DeliveryMaxAttempts = 2,
                DeliveryRetryDelayMinutes = 0,
                TimeZoneId = "Asia/Ho_Chi_Minh"
            }),
            NullLogger<DeliveryService>.Instance);

        await deliveryService.DispatchAsync(issueResult.IssuanceId!.Value);
        await deliveryService.DispatchAsync(issueResult.IssuanceId.Value);
        await deliveryService.DispatchAsync(issueResult.IssuanceId.Value);

        var issuance = await dbContext.PersonalizedVoucherIssuances.SingleAsync();
        Assert.Equal(1, await dbContext.Vouchers.CountAsync());
        Assert.Equal(PersonalizedVoucherDeliveryStatus.Failed, issuance.NotificationStatus);
        Assert.Equal(PersonalizedVoucherDeliveryStatus.Failed, issuance.EmailStatus);
        Assert.Equal(2, issuance.NotificationAttemptCount);
        Assert.Equal(2, issuance.EmailAttemptCount);
    }

    [Fact]
    public async Task ExistingRewardRedemption_StillCreatesRewardVoucherAndUpdatesInventory()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var reward = await AddRewardAsync(dbContext, seed.TierId, quantity: 3);
        var httpContextAccessor = CreateHttpContextAccessor(seed.UserId);
        var rewardService = new SWP391_AutoWashPro_BE.Service.Reward.Service(
            dbContext,
            new RecordingNotificationService(),
            httpContextAccessor);

        await rewardService.RedeemReward(reward.Id);

        var voucher = await dbContext.Vouchers.SingleAsync();
        Assert.Equal(reward.Id, voucher.RewardId);
        Assert.Null(voucher.PromotionId);
        Assert.Equal(2, reward.QuantityAvailable);
        Assert.Equal(900, (await dbContext.CustomerProfiles.SingleAsync()).TotalPoints);
    }

    [Fact]
    public async Task Booking_AppliesActiveTierPromotion()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        await SetPromotionActiveAsync(dbContext, seed.PromotionId, false);
        var nowUtc = DateTimeOffset.UtcNow;
        await AddPromotionAsync(
            dbContext,
            DiscountType.FixedAmount,
            25000,
            isGlobal: false,
            isActive: true,
            isDeleted: false,
            nowUtc.AddDays(-1),
            nowUtc.AddDays(1),
            seed.TierId);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);

        var response = await CreateBookingService(dbContext, seed.UserId).CreateBooking(
            CreateBookingRequest(bookingData, voucherId: null, minute: 0));

        await AssertBookingPricingAsync(dbContext, response, expectedDiscount: 25000);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    [InlineData("relation-deleted")]
    [InlineData("future")]
    [InlineData("expired")]
    public async Task Booking_DoesNotApplyIneligibleTierPromotion(string eligibilityState)
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        await SetPromotionActiveAsync(dbContext, seed.PromotionId, false);
        var nowUtc = DateTimeOffset.UtcNow;
        var isActive = eligibilityState != "inactive";
        var isDeleted = eligibilityState == "deleted";
        var isTierLinkDeleted = eligibilityState == "relation-deleted";
        var startDate = eligibilityState == "future"
            ? nowUtc.AddDays(1)
            : nowUtc.AddDays(-2);
        var endDate = eligibilityState == "expired"
            ? nowUtc.AddDays(-1)
            : nowUtc.AddDays(2);
        await AddPromotionAsync(
            dbContext,
            DiscountType.FixedAmount,
            25000,
            isGlobal: false,
            isActive,
            isDeleted,
            startDate,
            endDate,
            seed.TierId,
            isTierLinkDeleted);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);

        var response = await CreateBookingService(dbContext, seed.UserId).CreateBooking(
            CreateBookingRequest(bookingData, voucherId: null, minute: 0));

        await AssertBookingPricingAsync(dbContext, response, expectedDiscount: 0);
    }

    [Fact]
    public async Task Booking_ReflectsTierPromotionDeactivateAndReactivateImmediately()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        await SetPromotionActiveAsync(dbContext, seed.PromotionId, false);
        var nowUtc = DateTimeOffset.UtcNow;
        var tierPromotion = await AddPromotionAsync(
            dbContext,
            DiscountType.FixedAmount,
            25000,
            isGlobal: false,
            isActive: true,
            isDeleted: false,
            nowUtc.AddDays(-1),
            nowUtc.AddDays(1),
            seed.TierId);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);
        var bookingService = CreateBookingService(dbContext, seed.UserId);

        tierPromotion.IsActive = false;
        await dbContext.SaveChangesAsync();
        var inactiveResponse = await bookingService.CreateBooking(
            CreateBookingRequest(bookingData, voucherId: null, minute: 0));

        tierPromotion.IsActive = true;
        await dbContext.SaveChangesAsync();
        var activeResponse = await bookingService.CreateBooking(
            CreateBookingRequest(bookingData, voucherId: null, minute: 15));

        await AssertBookingPricingAsync(dbContext, inactiveResponse, expectedDiscount: 0);
        await AssertBookingPricingAsync(dbContext, activeResponse, expectedDiscount: 25000);
    }

    [Fact]
    public async Task Booking_Regression_InactiveTierPromotionDoesNotAddTwentyFiveThousand()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        await SetPromotionActiveAsync(dbContext, seed.PromotionId, false);
        var nowUtc = DateTimeOffset.UtcNow;
        await AddPromotionAsync(
            dbContext,
            DiscountType.FixedAmount,
            10000,
            isGlobal: true,
            isActive: true,
            isDeleted: false,
            nowUtc.AddDays(-1),
            nowUtc.AddDays(1));
        await AddPromotionAsync(
            dbContext,
            DiscountType.FixedAmount,
            25000,
            isGlobal: false,
            isActive: false,
            isDeleted: false,
            nowUtc.AddDays(-1),
            nowUtc.AddDays(1),
            seed.TierId);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);

        var response = await CreateBookingService(dbContext, seed.UserId).CreateBooking(
            CreateBookingRequest(bookingData, voucherId: null, minute: 0));

        await AssertBookingPricingAsync(dbContext, response, expectedDiscount: 10000);
    }

    [Fact]
    public async Task Booking_WithoutVoucher_StillAutoAppliesBirthdayPromotion()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);

        var response = await CreateBookingService(dbContext, seed.UserId).CreateBooking(
            CreateBookingRequest(bookingData, voucherId: null, minute: 0));

        await AssertBookingPricingAsync(dbContext, response, expectedDiscount: 15000);
    }

    [Fact]
    public async Task Booking_WithBirthdayVoucher_ExcludesItsSourcePromotion()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var issueResult = await CreateIssuanceService(dbContext).TryIssuePersonalizedVoucherAsync(
            seed.CustomerId,
            seed.RuleId,
            PersonalizedVoucherTriggerType.Birthday,
            "BIRTHDAY:PRICING",
            null);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);

        var response = await CreateBookingService(dbContext, seed.UserId).CreateBooking(
            CreateBookingRequest(bookingData, issueResult.VoucherId, minute: 0));

        await AssertBookingPricingAsync(dbContext, response, expectedDiscount: 15000);
    }

    [Fact]
    public async Task Booking_WithPromotionVoucher_StillStacksUnrelatedPromotion()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var nowUtc = DateTimeOffset.UtcNow;
        await AddPromotionAsync(
            dbContext,
            DiscountType.FixedAmount,
            10000,
            isGlobal: true,
            isActive: true,
            isDeleted: false,
            nowUtc.AddDays(-1),
            nowUtc.AddDays(1));
        var issueResult = await CreateIssuanceService(dbContext).TryIssuePersonalizedVoucherAsync(
            seed.CustomerId,
            seed.RuleId,
            PersonalizedVoucherTriggerType.Birthday,
            "BIRTHDAY:STACKING",
            null);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);

        var response = await CreateBookingService(dbContext, seed.UserId).CreateBooking(
            CreateBookingRequest(bookingData, issueResult.VoucherId, minute: 0));

        await AssertBookingPricingAsync(dbContext, response, expectedDiscount: 25000);
    }

    [Fact]
    public async Task Booking_WithRewardVoucher_DoesNotExcludeAutoPromotions()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var reward = await AddRewardAsync(dbContext, seed.TierId, quantity: 1);
        var rewardService = new SWP391_AutoWashPro_BE.Service.Reward.Service(
            dbContext,
            new RecordingNotificationService(),
            CreateHttpContextAccessor(seed.UserId));
        await rewardService.RedeemReward(reward.Id);
        var rewardVoucher = await dbContext.Vouchers.SingleAsync();
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);

        var response = await CreateBookingService(dbContext, seed.UserId).CreateBooking(
            CreateBookingRequest(bookingData, rewardVoucher.Id, minute: 0));

        Assert.Null(rewardVoucher.PromotionId);
        await AssertBookingPricingAsync(dbContext, response, expectedDiscount: 40000);
    }

    [Theory]
    [InlineData("invalid-status", "Voucher is inactive")]
    [InlineData("expired", "Voucher expired")]
    [InlineData("used", "Voucher already used")]
    [InlineData("invalid-discount", "Voucher has no discount value")]
    public async Task Booking_InvalidVoucher_PreservesValidationAndDoesNotCreateBooking(
        string invalidState,
        string expectedMessage)
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Birthday);
        var nowUtc = DateTimeOffset.UtcNow;
        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            CustomerId = seed.CustomerId,
            PromotionId = seed.PromotionId,
            Code = $"INVALID-{Guid.NewGuid():N}",
            Status = invalidState == "invalid-status" ? VoucherStatus.Used : VoucherStatus.Active,
            DiscountType = DiscountType.Percentage,
            DiscountValue = invalidState == "invalid-discount" ? 0 : 15,
            ExpiresAt = invalidState == "expired" ? nowUtc.AddMinutes(-1) : nowUtc.AddDays(1),
            UsedAt = invalidState == "used" ? nowUtc.AddMinutes(-1) : null,
            CreatedAt = nowUtc
        };
        dbContext.Vouchers.Add(voucher);
        await dbContext.SaveChangesAsync();
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);
        var walletBalanceBefore = await dbContext.Wallets
            .Where(x => x.CustomerId == seed.CustomerId)
            .Select(x => x.Balance)
            .SingleAsync();

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            CreateBookingService(dbContext, seed.UserId).CreateBooking(
                CreateBookingRequest(bookingData, voucher.Id, minute: 0)));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Empty(dbContext.Bookings);
        Assert.Equal(walletBalanceBefore, await dbContext.Wallets
            .Where(x => x.CustomerId == seed.CustomerId)
            .Select(x => x.Balance)
            .SingleAsync());
    }

    [Fact]
    public async Task Booking_RejectsVoucherOwnedByAnotherCustomer()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var owner = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Welcome);
        var issueResult = await CreateIssuanceService(dbContext).TryIssuePersonalizedVoucherAsync(
            owner.CustomerId,
            owner.RuleId,
            PersonalizedVoucherTriggerType.Welcome,
            "WELCOME:OWNER",
            null);
        var otherCustomer = await AddCustomerAsync(dbContext, owner.TierId, lastLoginAt: null);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, otherCustomer.Id);
        var service = CreateBookingService(dbContext, otherCustomer.UserId);

        var exception = await Assert.ThrowsAsync<Exception>(() => service.CreateBooking(
            CreateBookingRequest(bookingData, issueResult.VoucherId, minute: 0)));

        Assert.Equal("Voucher not found", exception.Message);
        Assert.Empty(dbContext.Bookings);
    }

    [Fact]
    public async Task AcquisitionVoucher_IsRejectedWhenCustomerAlreadyHasABooking()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var seed = await SeedAsync(dbContext, PersonalizedVoucherTriggerType.Welcome);
        var issueResult = await CreateIssuanceService(dbContext).TryIssuePersonalizedVoucherAsync(
            seed.CustomerId,
            seed.RuleId,
            PersonalizedVoucherTriggerType.Welcome,
            "WELCOME:FIRST",
            null);
        var bookingData = await AddBookingPrerequisitesAsync(dbContext, seed.CustomerId);
        var existingStart = CreateBookingStartTime(minute: 0);
        dbContext.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = seed.CustomerId,
            BranchId = bookingData.BranchId,
            VehicleId = bookingData.VehicleId,
            BookingDate = DateOnly.FromDateTime(existingStart.DateTime),
            StartTime = existingStart.ToUniversalTime(),
            EndTime = existingStart.AddMinutes(15).ToUniversalTime(),
            Status = BookingStatus.Cancelled,
            BasePrice = 100000,
            DiscountAmount = 0,
            FinalPrice = 100000,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var service = CreateBookingService(dbContext, seed.UserId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBooking(
            CreateBookingRequest(bookingData, issueResult.VoucherId, minute: 15)));

        Assert.Contains("first booking", exception.Message);
    }

    private static PersonalizedVoucherService CreateIssuanceService(AppDbContext dbContext)
    {
        return new PersonalizedVoucherService(
            dbContext,
            NullLogger<PersonalizedVoucherService>.Instance);
    }

    private static AudienceService CreateAudienceService(
        AppDbContext dbContext,
        IDeliveryService deliveryService)
    {
        return new AudienceService(
            dbContext,
            CreateIssuanceService(dbContext),
            deliveryService,
            Microsoft.Extensions.Options.Options.Create(new SWP391_AutoWashPro_BE.Service.PersonalizedVoucher.Options
            {
                BatchSize = 100,
                TimeZoneId = "Asia/Ho_Chi_Minh"
            }),
            NullLogger<AudienceService>.Instance);
    }

    private static SWP391_AutoWashPro_BE.Service.Booking.Service CreateBookingService(
        AppDbContext dbContext,
        Guid userId)
    {
        return new SWP391_AutoWashPro_BE.Service.Booking.Service(
            dbContext,
            CreateHttpContextAccessor(userId),
            new UnusedServiceScopeFactory(),
            new RecordingNotificationService(),
            new RecordingAudienceService(),
            NullLogger<SWP391_AutoWashPro_BE.Service.Booking.Service>.Instance);
    }

    private static async Task<AppDbContext> CreateCleanDbContextAsync()
    {
        var dbContext = CreateDbContext(RequireConnectionString());
        await ResetDatabaseAsync(dbContext);
        return dbContext;
    }

    private static AppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task ResetDatabaseAsync(AppDbContext dbContext)
    {
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    private static string RequireConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "PERSONALIZED_VOUCHER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip(
                "Set PERSONALIZED_VOUCHER_TEST_CONNECTION to run PostgreSQL integration tests.");
        }

        return connectionString!;
    }

    private static async Task<SeedData> SeedAsync(
        AppDbContext dbContext,
        PersonalizedVoucherTriggerType triggerType,
        AccountStatus accountStatus = AccountStatus.Active,
        bool isVerified = true,
        DateOnly? dateOfBirth = null,
        DateTimeOffset? lastLoginAt = null,
        DateTimeOffset? verifiedAt = null,
        DateTimeOffset? userCreatedAt = null,
        int? thresholdDays = null,
        bool sendNotification = false,
        bool sendEmail = false)
    {
        var tier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = $"Test Tier {Guid.NewGuid():N}",
            Level = 99,
            RequiredWashes = 0,
            PriorityBookingDays = 30,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Tiers.Add(tier);
        await dbContext.SaveChangesAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.test",
            Phone = Guid.NewGuid().ToString("N")[..10],
            PasswordHash = "test-hash",
            Role = UserRole.Customer,
            Status = accountStatus,
            isVerify = isVerified,
            LastLoginAt = lastLoginAt,
            VerifiedAt = verifiedAt,
            CreatedAt = userCreatedAt ?? DateTimeOffset.UtcNow
        };
        var customer = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TierId = tier.Id,
            FirstName = "Voucher",
            LastName = "Tester",
            TotalPoints = 1000,
            DateOfBirth = dateOfBirth,
            DateOfBirthSetAt = dateOfBirth.HasValue ? DateTimeOffset.UtcNow : null,
            CreatedAt = user.CreatedAt
        };
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Name = $"Test Promotion {Guid.NewGuid():N}",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            EndDate = DateTimeOffset.UtcNow.AddDays(30),
            IsGlobal = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var rule = CreateRule(
            promotion.Id,
            triggerType,
            thresholdDays,
            priority: 10,
            sendNotification,
            sendEmail);

        dbContext.Users.Add(user);
        dbContext.CustomerProfiles.Add(customer);
        dbContext.Promotions.Add(promotion);
        dbContext.PersonalizedPromotionRules.Add(rule);
        await dbContext.SaveChangesAsync();

        return new SeedData(
            user.Id,
            customer.Id,
            tier.Id,
            promotion.Id,
            rule.Id,
            promotion.EndDate);
    }

    private static PersonalizedPromotionRule CreateRule(
        Guid promotionId,
        PersonalizedVoucherTriggerType triggerType,
        int? thresholdDays,
        int priority,
        bool sendNotification = false,
        bool sendEmail = false)
    {
        return new PersonalizedPromotionRule
        {
            Id = Guid.NewGuid(),
            PromotionId = promotionId,
            TriggerType = triggerType,
            ThresholdDays = thresholdDays,
            VoucherValidityDays = 14,
            Priority = priority,
            IsActive = true,
            SendInAppNotification = sendNotification,
            SendEmail = sendEmail,
            NotificationTitleTemplate = sendNotification ? "Voucher {VoucherCode}" : null,
            NotificationContentTemplate = sendNotification
                ? "Hello {CustomerName}, use {Discount} before {ExpiresAt}."
                : null,
            EmailSubjectTemplate = sendEmail ? "Voucher {VoucherCode}" : null,
            EmailBodyTemplate = sendEmail
                ? "Hello {CustomerName}, {PromotionName} gives {Discount} until {ExpiresAt}. " +
                  "Book at {BookingUrl}. Code {VoucherCode}."
                : null,
            CallToActionUrl = sendEmail ? "https://example.test/booking" : null,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static async Task<CustomerProfile> AddCustomerAsync(
        AppDbContext dbContext,
        Guid tierId,
        DateTimeOffset? lastLoginAt)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.test",
            Phone = Guid.NewGuid().ToString("N")[..10],
            PasswordHash = "test-hash",
            Role = UserRole.Customer,
            Status = AccountStatus.Active,
            isVerify = true,
            LastLoginAt = lastLoginAt,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
        };
        var customer = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TierId = tierId,
            FirstName = "Second",
            LastName = "Customer",
            TotalPoints = 1000,
            CreatedAt = user.CreatedAt
        };
        dbContext.Users.Add(user);
        dbContext.CustomerProfiles.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    private static async Task<Reward> AddRewardAsync(
        AppDbContext dbContext,
        Guid tierId,
        int quantity)
    {
        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = $"Test Reward {Guid.NewGuid():N}",
            RewardType = RewardType.Voucher,
            PointsRequired = 100,
            QuantityAvailable = quantity,
            ValidDays = 30,
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 25000,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Rewards.Add(reward);
        dbContext.RewardTiers.Add(new RewardTier
        {
            Id = Guid.NewGuid(),
            RewardId = reward.Id,
            TierId = tierId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return reward;
    }

    private static async Task SetPromotionActiveAsync(
        AppDbContext dbContext,
        Guid promotionId,
        bool isActive)
    {
        var promotion = await dbContext.Promotions.SingleAsync(x => x.Id == promotionId);
        promotion.IsActive = isActive;
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Promotion> AddPromotionAsync(
        AppDbContext dbContext,
        DiscountType discountType,
        decimal discountValue,
        bool isGlobal,
        bool isActive,
        bool isDeleted,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid? tierId = null,
        bool isTierLinkDeleted = false)
    {
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Name = $"Pricing Promotion {Guid.NewGuid():N}",
            DiscountType = discountType,
            DiscountValue = discountValue,
            StartDate = startDate,
            EndDate = endDate,
            IsGlobal = isGlobal,
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Promotions.Add(promotion);

        if (tierId.HasValue)
        {
            dbContext.PromotionTiers.Add(new PromotionTier
            {
                Id = Guid.NewGuid(),
                PromotionId = promotion.Id,
                TierId = tierId.Value,
                IsDeleted = isTierLinkDeleted,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
        return promotion;
    }

    private static async Task AssertBookingPricingAsync(
        AppDbContext dbContext,
        SWP391_AutoWashPro_BE.Service.Booking.Response.CreateBookingResponse response,
        decimal expectedDiscount)
    {
        Assert.Equal(100000, response.BasePrice);
        Assert.Equal(expectedDiscount, response.DiscountAmount);
        Assert.Equal(100000 - expectedDiscount, response.FinalPrice);

        var booking = await dbContext.Bookings.SingleAsync(x => x.Id == response.Id);
        Assert.Equal(response.BasePrice, booking.BasePrice);
        Assert.Equal(expectedDiscount, booking.DiscountAmount);
        Assert.Equal(response.FinalPrice, booking.FinalPrice);
    }

    private static async Task<BookingPrerequisites> AddBookingPrerequisitesAsync(
        AppDbContext dbContext,
        Guid customerId)
    {
        var vehicleTypeId = await dbContext.VehicleTypes
            .Where(x => x.TypeName == VehicleTypes.Sedan)
            .Select(x => x.Id)
            .SingleAsync();
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = $"Test Branch {Guid.NewGuid():N}",
            Address = "Test address",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            VehicleTypeId = vehicleTypeId,
            LicensePlate = Guid.NewGuid().ToString("N")[..10],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Balance = 10000000,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Branches.Add(branch);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Wallets.Add(wallet);
        await dbContext.SaveChangesAsync();
        return new BookingPrerequisites(branch.Id, vehicle.Id);
    }

    private static SWP391_AutoWashPro_BE.Service.Booking.Request.CreateBookingRequest CreateBookingRequest(
        BookingPrerequisites bookingData,
        Guid? voucherId,
        int minute)
    {
        var startTime = CreateBookingStartTime(minute);
        return new SWP391_AutoWashPro_BE.Service.Booking.Request.CreateBookingRequest
        {
            BranchId = bookingData.BranchId,
            VehicleId = bookingData.VehicleId,
            VoucherId = voucherId,
            BookingDate = DateOnly.FromDateTime(startTime.DateTime),
            StartTime = startTime,
            redemPoint = false
        };
    }

    private static DateTimeOffset CreateBookingStartTime(int minute)
    {
        var localDate = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date.AddDays(2);
        return new DateTimeOffset(
            localDate.Year,
            localDate.Month,
            localDate.Day,
            8,
            minute,
            0,
            TimeSpan.FromHours(7));
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(Guid userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "Test");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private sealed record SeedData(
        Guid UserId,
        Guid CustomerId,
        Guid TierId,
        Guid PromotionId,
        Guid RuleId,
        DateTimeOffset PromotionEndDate);

    private sealed record BookingPrerequisites(Guid BranchId, Guid VehicleId);

    private sealed class RecordingDeliveryService : IDeliveryService
    {
        public List<Guid> IssuanceIds { get; } = new();

        public Task DispatchAsync(Guid issuanceId, CancellationToken cancellationToken = default)
        {
            IssuanceIds.Add(issuanceId);
            return Task.CompletedTask;
        }

        public Task<int> RetryPendingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FailingNotificationService : NotificationService.IService
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
            throw new InvalidOperationException("Expected notification failure.");
    }

    private sealed class RecordingNotificationService : NotificationService.IService
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
            Task.CompletedTask;

        public Task SendNotificationToUser(
            Guid userId,
            Guid notificationId,
            NotificationType type,
            string title,
            string content,
            string? metadata,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FailingMailService : MailService.IService
    {
        public Task SendMail(
            MailService.MailContent mailContent,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Expected mail failure.");
    }

    private sealed class RecordingAudienceService : IAudienceService
    {
        public Task<int> ProcessBirthdayAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> ProcessInactiveCustomersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> ProcessAcquisitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<Response.IssueResult> ProcessTierUpgradeAsync(
            Guid customerId,
            Guid newTierId,
            Guid bookingId,
            CancellationToken cancellationToken = default) => Task.FromResult(new Response.IssueResult
        {
            Status = Response.IssueStatus.Skipped
        });
    }

    private sealed class UnusedServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() =>
            throw new NotSupportedException("The booking path under test does not create background scopes.");
    }
}
