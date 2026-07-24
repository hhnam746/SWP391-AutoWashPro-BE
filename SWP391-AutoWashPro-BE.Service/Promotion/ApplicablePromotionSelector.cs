using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using PromotionEntity = SWP391_AutoWashPro_BE.Repository.Entities.Promotion;

namespace SWP391_AutoWashPro_BE.Service.Promotion;

public static class ApplicablePromotionSelector
{
    public static IQueryable<PromotionEntity> Query(
        AppDbContext dbContext,
        Guid tierId,
        DateTimeOffset effectiveAt)
    {
        return dbContext.Promotions
            .AsNoTracking()
            .Where(promotion =>
                promotion.IsActive &&
                !promotion.IsDeleted &&
                promotion.StartDate <= effectiveAt &&
                promotion.EndDate > effectiveAt &&
                (
                    promotion.IsGlobal == true ||
                    promotion.PromotionTiers.Any(promotionTier =>
                        promotionTier.TierId == tierId &&
                        !promotionTier.IsDeleted &&
                        !promotionTier.Tier.IsDeleted)
                ))
            .OrderBy(promotion => promotion.CreatedAt)
            .ThenBy(promotion => promotion.Id);
    }
}
