using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Promotion;
using Xunit;
using BranchService = SWP391_AutoWashPro_BE.Service.Branch.Service;
using PromotionEntity = SWP391_AutoWashPro_BE.Repository.Entities.Promotion;

namespace SWP391_AutoWashPro_BE.Tests;

public class ApplicablePromotionSelectorTests
{
    [Fact]
    public async Task Query_ReturnsAllAndOnlyApplicablePromotionsOnceInStableOrder()
    {
        await using var dbContext = CreateDbContext();
        var effectiveAt = new DateTimeOffset(2026, 7, 24, 8, 0, 0, TimeSpan.Zero);
        var customerTier = CreateTier("Member", 1);
        var otherTier = CreateTier("Silver", 2);
        var deletedTier = CreateTier("Deleted", 3, isDeleted: true);

        var validGlobal = CreatePromotion(
            "Valid global",
            effectiveAt,
            createdMinutes: 1,
            isGlobal: true,
            startDate: effectiveAt);
        var validTierFirst = CreatePromotion("Valid tier first", effectiveAt, createdMinutes: 2);
        validTierFirst.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var validTierSecond = CreatePromotion("Valid tier second", effectiveAt, createdMinutes: 2);
        validTierSecond.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var globalWithTierLink = CreatePromotion("Global with tier link", effectiveAt, createdMinutes: 4, isGlobal: true);
        var inactive = CreatePromotion("Inactive", effectiveAt, createdMinutes: 5, isGlobal: true, isActive: false);
        var deleted = CreatePromotion("Deleted", effectiveAt, createdMinutes: 6, isGlobal: true, isDeleted: true);
        var future = CreatePromotion(
            "Future",
            effectiveAt,
            createdMinutes: 7,
            isGlobal: true,
            startDate: effectiveAt.AddMinutes(1));
        var expired = CreatePromotion(
            "Expired",
            effectiveAt,
            createdMinutes: 8,
            isGlobal: true,
            endDate: effectiveAt);
        var wrongTier = CreatePromotion("Wrong tier", effectiveAt, createdMinutes: 9);
        var deletedTierLink = CreatePromotion("Deleted tier link", effectiveAt, createdMinutes: 10);
        var promotionForDeletedTier = CreatePromotion("Deleted tier", effectiveAt, createdMinutes: 11);

        dbContext.AddRange(
            customerTier,
            otherTier,
            deletedTier,
            validGlobal,
            validTierFirst,
            validTierSecond,
            globalWithTierLink,
            inactive,
            deleted,
            future,
            expired,
            wrongTier,
            deletedTierLink,
            promotionForDeletedTier);
        dbContext.PromotionTiers.AddRange(
            CreatePromotionTier(validTierFirst, customerTier),
            CreatePromotionTier(validTierSecond, customerTier),
            CreatePromotionTier(globalWithTierLink, customerTier),
            CreatePromotionTier(wrongTier, otherTier),
            CreatePromotionTier(deletedTierLink, customerTier, isDeleted: true),
            CreatePromotionTier(promotionForDeletedTier, deletedTier));
        await dbContext.SaveChangesAsync();

        var promotions = await ApplicablePromotionSelector
            .Query(dbContext, customerTier.Id, effectiveAt)
            .ToListAsync();

        Assert.Equal(
            ["Valid global", "Valid tier first", "Valid tier second", "Global with tier link"],
            promotions.Select(promotion => promotion.Name));
        Assert.Equal(promotions.Count, promotions.Select(promotion => promotion.Id).Distinct().Count());
    }

    [Fact]
    public async Task AvailablePromotions_ReturnsTheSharedSelectorResult()
    {
        await using var dbContext = CreateDbContext();
        var nowUtc = DateTimeOffset.UtcNow;
        var customerTier = CreateTier("Member", 1);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Phone = "0900000000",
            PasswordHash = "test",
            Status = AccountStatus.Active,
            isVerify = true,
            CreatedAt = nowUtc
        };
        var customerProfile = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TierId = customerTier.Id,
            Tier = customerTier,
            FirstName = "Promotion",
            LastName = "Tester",
            CreatedAt = nowUtc
        };
        var validGlobal = CreatePromotion("Valid global", nowUtc, createdMinutes: 1, isGlobal: true);
        var validTier = CreatePromotion("Valid tier", nowUtc, createdMinutes: 2);
        var inactive = CreatePromotion(
            "Inactive",
            nowUtc,
            createdMinutes: 3,
            isGlobal: true,
            isActive: false);

        dbContext.AddRange(customerTier, user, customerProfile, validGlobal, validTier, inactive);
        dbContext.PromotionTiers.Add(CreatePromotionTier(validTier, customerTier));
        await dbContext.SaveChangesAsync();

        var expectedIds = await ApplicablePromotionSelector
            .Query(dbContext, customerTier.Id, nowUtc)
            .Select(promotion => promotion.Id)
            .ToListAsync();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
                "Test"));
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var response = await new BranchService(dbContext, httpContextAccessor).GetPromotions();

        Assert.Equal(expectedIds, response.data.Select(promotion => promotion.Id));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Tier CreateTier(string name, int level, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Level = level,
        RequiredWashes = 0,
        PriorityBookingDays = 0,
        IsDeleted = isDeleted,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static PromotionEntity CreatePromotion(
        string name,
        DateTimeOffset effectiveAt,
        int createdMinutes,
        bool isGlobal = false,
        bool isActive = true,
        bool isDeleted = false,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = name,
        DiscountType = DiscountType.FixedAmount,
        DiscountValue = 10000,
        StartDate = startDate ?? effectiveAt.AddDays(-1),
        EndDate = endDate ?? effectiveAt.AddDays(1),
        IsGlobal = isGlobal,
        IsActive = isActive,
        IsDeleted = isDeleted,
        CreatedAt = effectiveAt.AddMinutes(createdMinutes)
    };

    private static PromotionTier CreatePromotionTier(
        PromotionEntity promotion,
        Tier tier,
        bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        PromotionId = promotion.Id,
        TierId = tier.Id,
        IsDeleted = isDeleted,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
