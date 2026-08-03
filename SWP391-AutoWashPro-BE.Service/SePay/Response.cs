namespace SWP391_AutoWashPro_BE.Service.SePay;

public class Response
{
    public class SePayWebhookResponse
    {
        public bool Success { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid? TransactionId { get; set; }
    }
}
