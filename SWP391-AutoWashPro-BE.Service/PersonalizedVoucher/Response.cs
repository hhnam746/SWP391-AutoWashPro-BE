using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class Response
{
    public enum IssueStatus
    {
        Issued,
        AlreadyIssued,
        Skipped
    }

    public class IssueResult
    {
        public IssueStatus Status { get; set; }
        public Guid? IssuanceId { get; set; }
        public Guid? VoucherId { get; set; }
        public string? SkippedReason { get; set; }
    }

    public class RuleResponse
    {
        public Guid Id { get; set; }
        public string VoucherName { get; set; } = null!;
        public PersonalizedVoucherTriggerType TriggerType { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public int? ThresholdDays { get; set; }
        public int VoucherValidityDays { get; set; }
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
    }

    public class ReportItem
    {
        public Guid VoucherRuleId { get; set; }
        public string VoucherName { get; set; } = null!;
        public PersonalizedVoucherTriggerType TriggerType { get; set; }
        public int IssuedCount { get; set; }
        public int ActiveCount { get; set; }
        public int ReservedCount { get; set; }
        public int UsedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int NotificationPendingCount { get; set; }
        public int NotificationSentCount { get; set; }
        public int NotificationFailedCount { get; set; }
        public int EmailPendingCount { get; set; }
        public int EmailSentCount { get; set; }
        public int EmailFailedCount { get; set; }
        public decimal ConversionRate { get; set; }
    }
}
