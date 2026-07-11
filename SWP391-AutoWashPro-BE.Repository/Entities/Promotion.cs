using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Promotion : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool? IsGlobal { get; set; }
    public bool IsActive { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<PromotionTier> PromotionTiers { get; set; } = new List<PromotionTier>();
    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
