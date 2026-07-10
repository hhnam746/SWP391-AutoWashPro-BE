namespace SWP391_AutoWashPro_BE.Service.AiService;

public class GoogleAiStudioOptions
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? Model { get; set; }
    public string? FallbackModel { get; set; }
    public decimal? Temperature { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int? MaxRetries { get; set; }
    public int? RetryDelayMs { get; set; }
}
