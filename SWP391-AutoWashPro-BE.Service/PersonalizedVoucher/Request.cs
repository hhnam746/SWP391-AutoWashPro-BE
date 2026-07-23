using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class Request
{
    public class RuleRequest
    {
        public string VoucherName { get; set; } = null!;
        public PersonalizedVoucherTriggerType TriggerType { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public int? ThresholdDays { get; set; }
        public int VoucherValidityDays { get; set; }
        public bool IsActive { get; set; } = true;
        public bool SendInAppNotification { get; set; }
        public bool SendEmail { get; set; }
        public string? NotificationTitleTemplate { get; set; }
        public string? NotificationContentTemplate { get; set; }
        public string? EmailSubjectTemplate { get; set; }
        public string? EmailBodyTemplate { get; set; }
        public string? CallToActionUrl { get; set; }
    }

    public class UpdateRuleStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class GetRulesRequest
    {
        public PersonalizedVoucherTriggerType? TriggerType { get; set; }
        public bool? IsActive { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ReportRequest
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public PersonalizedVoucherTriggerType? TriggerType { get; set; }
    }
}
