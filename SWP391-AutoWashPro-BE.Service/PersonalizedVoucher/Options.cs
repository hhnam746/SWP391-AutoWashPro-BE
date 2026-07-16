namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class Options
{
    public const string SectionName = "PersonalizedVoucher";
    public int BatchSize { get; set; } = 100;
    public int DeliveryMaxAttempts { get; set; } = 3;
    public int DeliveryRetryDelayMinutes { get; set; } = 30;
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
}
