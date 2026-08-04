namespace SWP391_AutoWashPro_BE.Service.SePay;

public class Options
{
    public const string SectionName = "SePayOptions";

    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string QrBaseUrl { get; set; } = "https://vietqr.app/img";
    public string QrTemplate { get; set; } = "compact";
    public bool QrShowInfo { get; set; }
    public bool IncludeAccountHolderInQr { get; set; }
    public string TransferContentPrefix { get; set; } = "TOPUP";
    public bool UseHmacSignature { get; set; }
    public string SecretKey { get; set; } = string.Empty;
}
