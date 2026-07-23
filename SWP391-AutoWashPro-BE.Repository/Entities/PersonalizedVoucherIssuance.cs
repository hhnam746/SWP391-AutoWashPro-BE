using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class PersonalizedVoucherIssuance : BaseEntity, IAuditableEntity
{
    public Guid CustomerId { get; set; }
    public CustomerProfile Customer { get; set; } = null!;
    public Guid VoucherRuleId { get; set; }
    public PersonalizedVoucherRule VoucherRule { get; set; } = null!;
    public Guid VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;
    public PersonalizedVoucherTriggerType TriggerType { get; set; }
    public string CycleKey { get; set; } = null!;
    public string? TriggerReference { get; set; }

    public Guid? NotificationId { get; set; }
    public PersonalizedVoucherDeliveryStatus NotificationStatus { get; set; }
    public int NotificationAttemptCount { get; set; }
    public DateTimeOffset? NotificationLastAttemptAt { get; set; }
    public DateTimeOffset? NotificationSentAt { get; set; }
    public string? NotificationLastError { get; set; }

    public PersonalizedVoucherDeliveryStatus EmailStatus { get; set; }
    public int EmailAttemptCount { get; set; }
    public DateTimeOffset? EmailLastAttemptAt { get; set; }
    public DateTimeOffset? EmailSentAt { get; set; }
    public string? EmailLastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
