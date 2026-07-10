namespace SWP391_AutoWashPro_BE.Service.OtpService;

public interface IService
{
    Task GenerateAndSendOtpAsync(string email);
    Task<bool> VerifyOtpAsync(string email, string otpCode);
    Task InvalidateOldOtpsAsync(string email);
}
