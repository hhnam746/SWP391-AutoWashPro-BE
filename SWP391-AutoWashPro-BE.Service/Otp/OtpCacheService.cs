using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace SWP391_AutoWashPro_BE.Service.Otp;

public class OtpCacheService : IOtpCacheService
{
    private readonly IDatabase _redis;
    private readonly OtpOptions _otpOption = new();

    public OtpCacheService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        configuration.GetSection("OtpOptions").Bind(_otpOption);
        configuration.GetSection(nameof(OtpOptions)).Bind(_otpOption);
        _redis = redis.GetDatabase();
    }

    public async Task SaveOtpAsync(string email, string otp)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedOtp = NormalizeOtp(otp);
        var cooldownKey = GetCooldownKey(normalizedEmail);

        if (await _redis.KeyExistsAsync(cooldownKey))
        {
            throw new InvalidOperationException("Please wait before requesting another OTP.");
        }

        await _redis.StringSetAsync(
            GetOtpKey(normalizedEmail),
            HashOtp(normalizedOtp),
            TimeSpan.FromMinutes(_otpOption.ExpireMinutes));

        await _redis.KeyDeleteAsync(GetAttemptsKey(normalizedEmail));

        await _redis.StringSetAsync(
            cooldownKey,
            "1",
            TimeSpan.FromSeconds(_otpOption.CooldownSeconds));
    }

    public async Task VerifyOtpAsync(string email, string otp)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedOtp = NormalizeOtp(otp);
        var attemptsKey = GetAttemptsKey(normalizedEmail);

        var attemptValue = await _redis.StringGetAsync(attemptsKey);
        if (attemptValue.HasValue && long.TryParse(attemptValue!, out var attempts) && attempts >= _otpOption.MaxAttempts)
        {
            throw new InvalidOperationException("OTP attempts exceeded. Please request a new OTP.");
        }

        var savedOtpHash = await _redis.StringGetAsync(GetOtpKey(normalizedEmail));
        if (!savedOtpHash.HasValue)
        {
            throw new InvalidOperationException("OTP is expired or invalid.");
        }

        var inputOtpHash = HashOtp(normalizedOtp);
        if (!FixedTimeEquals(savedOtpHash!, inputOtpHash))
        {
            var currentAttempts = await IncrementAttemptAsync(normalizedEmail);
            if (currentAttempts >= _otpOption.MaxAttempts)
            {
                await DeleteOtpAsync(normalizedEmail);
                throw new InvalidOperationException("OTP attempts exceeded. Please request a new OTP.");
            }

            throw new UnauthorizedAccessException("Invalid OTP.");
        }

        await _redis.KeyDeleteAsync(GetOtpKey(normalizedEmail));
        await _redis.KeyDeleteAsync(attemptsKey);
    }

    public async Task DeleteOtpAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        await _redis.KeyDeleteAsync(GetOtpKey(normalizedEmail));
    }

    public async Task<long> IncrementAttemptAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var attemptsKey = GetAttemptsKey(normalizedEmail);
        var attempts = await _redis.StringIncrementAsync(attemptsKey);

        await _redis.KeyExpireAsync(attemptsKey, TimeSpan.FromMinutes(_otpOption.ExpireMinutes));

        return attempts;
    }

    public async Task<string> CreateResetTokenAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        await _redis.StringSetAsync(
            GetResetTokenKey(token),
            normalizedEmail,
            TimeSpan.FromMinutes(_otpOption.ResetTokenExpireMinutes));

        return token;
    }

    public async Task<string> ValidateResetTokenAsync(string token)
    {
        var normalizedToken = NormalizeToken(token);
        var email = await _redis.StringGetAsync(GetResetTokenKey(normalizedToken));

        if (!email.HasValue)
        {
            throw new InvalidOperationException("Reset token is expired or invalid.");
        }

        return email!;
    }

    public async Task DeleteResetTokenAsync(string token)
    {
        var normalizedToken = NormalizeToken(token);
        await _redis.KeyDeleteAsync(GetResetTokenKey(normalizedToken));
    }

    private static string GetOtpKey(string email) => $"forgot-password:otp:{email}";

    private static string GetAttemptsKey(string email) => $"forgot-password:attempts:{email}";

    private static string GetCooldownKey(string email) => $"forgot-password:cooldown:{email}";

    private static string GetResetTokenKey(string token) => $"forgot-password:reset-token:{token}";

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeOtp(string otp)
    {
        if (string.IsNullOrWhiteSpace(otp))
        {
            throw new ArgumentException("OTP is required.", nameof(otp));
        }

        return otp.Trim();
    }

    private static string NormalizeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Reset token is required.", nameof(token));
        }

        return token.Trim().ToLowerInvariant();
    }

    private static string HashOtp(string otp)
    {
        var otpBytes = Encoding.UTF8.GetBytes(otp);
        var hashBytes = SHA256.HashData(otpBytes);

        return Convert.ToHexString(hashBytes);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
