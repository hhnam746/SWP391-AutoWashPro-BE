using System.ComponentModel.DataAnnotations;

namespace SWP391_AutoWashPro_BE.Service.Otp;

public class OtpOptions
{
    [Required]
    public int ExpireMinutes { get; set; }
    [Required]
    public int MaxAttempts { get; set; }
    [Required]
    public int CooldownSeconds { get; set; }
    [Required]
    public int ResetTokenExpireMinutes { get; set; }
}
