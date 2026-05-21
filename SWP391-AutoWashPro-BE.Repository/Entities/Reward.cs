using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Reward : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = null!;
    public RewardType RewardType { get; set; }
    public int PointsRequired { get; set; }
    public int QuantityAvailable { get; set; }
    public int ValidDays { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<RewardTier> RewardTiers { get; set; } = new List<RewardTier>();
    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
