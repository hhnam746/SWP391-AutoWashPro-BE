using System.ComponentModel.DataAnnotations;

namespace SWP391_AutoWashPro_BE.Service.OtpService;

public class OtpOptions
{
    [Required] public int ExpiryMinutes { get; set; }
    [Required] public int MaxFailedAttempts { get; set; }
}