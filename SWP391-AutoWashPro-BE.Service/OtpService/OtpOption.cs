using System.ComponentModel.DataAnnotations;

namespace SWP391_AutoWashPro_BE.Service.OtpService;

public class OtpOptions
{
    [Required] public int ExpiryMinutes { get; set; } = 5;
    [Required] public int MaxFailedAttempts { get; set; } = 5;
    [Required] public int MaxSendPerHour { get; set; } = 5;
}
