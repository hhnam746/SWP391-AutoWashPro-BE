using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class PromotionTier : BaseEntity, IAuditableEntity
{
    public Guid TierId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;
    public Tier Tier { get; set; } = null!;
}
