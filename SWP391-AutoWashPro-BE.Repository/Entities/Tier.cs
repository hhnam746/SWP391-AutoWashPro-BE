using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Tier : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public int RequiredWashes { get; set; }
    public int PriorityBookingDays { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<CustomerProfile> CustomerProfiles { get; set; } = new List<CustomerProfile>();
    public ICollection<PromotionTier> PromotionTiers { get; set; } = new List<PromotionTier>();
    public ICollection<RewardTier> RewardTiers { get; set; } = new List<RewardTier>();
}
