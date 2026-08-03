namespace SWP391_AutoWashPro_BE.Service.SePay;

public class Options
{
    public const string SectionName = "SePayOptions";

    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string QrTemplate { get; set; } = "qronly";
    public string TransferContentPrefix { get; set; } = "TOPUP";
    public bool UseHmacSignature { get; set; }
    public string SecretKey { get; set; } = string.Empty;
}
