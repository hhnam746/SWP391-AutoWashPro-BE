using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class RewardTier : BaseEntity, IAuditableEntity
{
    public Guid RewardId { get; set; }
    public Guid TierId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Reward Reward { get; set; } = null!;
    public Tier Tier { get; set; } = null!;
}
