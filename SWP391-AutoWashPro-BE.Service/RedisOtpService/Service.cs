using StackExchange.Redis;

namespace SWP391_AutoWashPro_BE.Service.RedisOtpService;

public class Service : IService
{
    private static readonly TimeSpan SendCountTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan VerifyAttemptTtl = TimeSpan.FromMinutes(5);

    private readonly IDatabase _database;

    public Service(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public Task SaveOtpAsync(string email, string otpHash, TimeSpan ttl)
    {
        return _database.StringSetAsync(GetOtpKey(email), otpHash, ttl);
    }

    public async Task<string?> GetOtpAsync(string email)
    {
        var value = await _database.StringGetAsync(GetOtpKey(email));
        return value.IsNull ? null : value.ToString();
    }

    public Task DeleteOtpAsync(string email)
    {
        return _database.KeyDeleteAsync(GetOtpKey(email));
    }

    public async Task<long> IncrementSendCountAsync(string email)
    {
        var key = GetSendCountKey(email);
        var count = await _database.StringIncrementAsync(key);

        if (count == 1)
        {
            await _database.KeyExpireAsync(key, SendCountTtl);
        }

        return count;
    }

    public async Task<long> GetSendCountAsync(string email)
    {
        var value = await _database.StringGetAsync(GetSendCountKey(email));

        if (value.IsNullOrEmpty || !long.TryParse(value.ToString(), out var count))
        {
            return 0;
        }

        return count;
    }

    public Task ResetSendCountAsync(string email)
    {
        return _database.KeyDeleteAsync(GetSendCountKey(email));
    }

    public async Task<long> IncrementVerifyAttemptAsync(string email)
    {
        var key = GetVerifyAttemptKey(email);
        var count = await _database.StringIncrementAsync(key);

        if (count == 1)
        {
            await _database.KeyExpireAsync(key, VerifyAttemptTtl);
        }

        return count;
    }

    public async Task<long> GetVerifyAttemptCountAsync(string email)
    {
        var value = await _database.StringGetAsync(GetVerifyAttemptKey(email));

        if (value.IsNullOrEmpty || !long.TryParse(value.ToString(), out var count))
        {
            return 0;
        }

        return count;
    }

    public Task ResetVerifyAttemptCountAsync(string email)
    {
        return _database.KeyDeleteAsync(GetVerifyAttemptKey(email));
    }

    private static string GetOtpKey(string email) => $"otp:{email.Trim().ToLowerInvariant()}";

    private static string GetSendCountKey(string email) => $"otp_limit:{email.Trim().ToLowerInvariant()}";

    private static string GetVerifyAttemptKey(string email) => $"otp_attempt:{email.Trim().ToLowerInvariant()}";
}
