namespace SWP391_AutoWashPro_BE.Service.Voucher;

public class Request
{
    public class ValidateVoucherRequest
    {
        public string Code { get; set; } = null!;
        public decimal TotalAmount { get; set; }
    }
}