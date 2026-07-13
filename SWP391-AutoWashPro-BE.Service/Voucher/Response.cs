using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Voucher;

public class Response
{
    public class VoucherResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string? RewardName { get; set; }
        public VoucherStatus Status { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? UsedAt { get; set; }
    }
    
    public class ValidateVoucherResponse
    {
        public Guid VoucherId { get; set; }
        public string Code { get; set; } = null!;
        public string? RewardName { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }
}
