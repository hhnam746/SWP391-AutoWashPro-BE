using StackExchange.Redis;
namespace SWP391_AutoWashPro_BE.Service.OTPDemoService;

public class OtpService
{
    private readonly IDatabase _db;

    public OtpService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task SaveOtp(string email, string otp)
    {
        await _db.StringSetAsync(
            $"otp:{email}",
            otp,
            TimeSpan.FromMinutes(5));
    }

    public async Task<string?> GetOtp(string email)
    {
        return await _db.StringGetAsync($"otp:{email}");
    }
    
    public async Task<bool> VerifyOtp(string email, string otp)
    {
        var savedOtp = await GetOtp(email);

        if (savedOtp == null)
            return false;

        if (savedOtp != otp)
            return false;

        await _db.KeyDeleteAsync($"otp:{email}");

        return true;
    }
    
}