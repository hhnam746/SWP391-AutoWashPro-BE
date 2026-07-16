using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class PersonalizedPromotionRule : BaseEntity, IAuditableEntity
{
    public Guid PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;
    public PersonalizedVoucherTriggerType TriggerType { get; set; }
    public int? ThresholdDays { get; set; }
    public int VoucherValidityDays { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool SendInAppNotification { get; set; }
    public bool SendEmail { get; set; }
    public string? NotificationTitleTemplate { get; set; }
    public string? NotificationContentTemplate { get; set; }
    public string? EmailSubjectTemplate { get; set; }
    public string? EmailBodyTemplate { get; set; }
    public string? CallToActionUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<PersonalizedVoucherIssuance> Issuances { get; set; } =
        new List<PersonalizedVoucherIssuance>();
}
