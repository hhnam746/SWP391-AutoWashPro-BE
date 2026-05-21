using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Voucher : BaseEntity, IAuditableEntity
{
    public Guid CustomerId { get; set; }
    public CustomerProfile Customer { get; set; } = null!;

    public Guid? RewardId { get; set; }
    public Reward? Reward { get; set; }

    public Guid? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    public string Code { get; set; } = null!;
    public VoucherStatus Status { get; set; } = VoucherStatus.Active;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
