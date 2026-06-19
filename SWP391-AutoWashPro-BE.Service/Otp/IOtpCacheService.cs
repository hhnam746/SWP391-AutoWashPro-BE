namespace SWP391_AutoWashPro_BE.Service.Otp;

public interface IOtpCacheService
{
    Task SaveOtpAsync(string email, string otp);
    Task VerifyOtpAsync(string email, string otp);
    Task DeleteOtpAsync(string email);
    Task<long> IncrementAttemptAsync(string email); // tránh spam OTP
    Task<string> CreateResetTokenAsync(string email);
    Task<string> ValidateResetTokenAsync(string token);
    Task DeleteResetTokenAsync(string token);
}
