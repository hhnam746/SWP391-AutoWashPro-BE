namespace SWP391_AutoWashPro_BE.Service.RedisOtpService;

public interface IService
{
    Task SaveOtpAsync(string email, string otpHash, TimeSpan ttl);
    Task<string?> GetOtpAsync(string email);
    Task DeleteOtpAsync(string email);
    Task<long> IncrementSendCountAsync(string email);
    Task<long> GetSendCountAsync(string email);
    Task ResetSendCountAsync(string email);
    Task<long> IncrementVerifyAttemptAsync(string email);
    Task<long> GetVerifyAttemptCountAsync(string email);
    Task ResetVerifyAttemptCountAsync(string email);
}
