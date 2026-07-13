using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWP391_AutoWashPro_BE.Service.MailService;
using SWP391_AutoWashPro_BE.Service.Models;
using RedisOtpService = SWP391_AutoWashPro_BE.Service.RedisOtpService;

namespace SWP391_AutoWashPro_BE.Service.OtpService;

public class Service : IService
{
    private readonly OtpOptions _otpOption = new();
    private readonly Security.IService _securityService;
    private readonly MailService.IService _mailService;
    private readonly RedisOtpService.IService _redisOtpService;
    private readonly ILogger<Service> _logger;

    public Service(
        IConfiguration configuration,
        Security.IService securityService,
        MailService.IService mailService,
        RedisOtpService.IService redisOtpService,
        ILogger<Service> logger)
    {
        configuration.GetSection(nameof(OtpOptions)).Bind(_otpOption);
        _securityService = securityService;
        _mailService = mailService;
        _redisOtpService = redisOtpService;
        _logger = logger;
    }

    public async Task GenerateAndSendOtpAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        _logger.LogInformation("Generating OTP for {Email}", normalizedEmail);
        var sendCount = await _redisOtpService.GetSendCountAsync(normalizedEmail);

        if (sendCount >= _otpOption.MaxSendPerHour)
        {
            throw new TooManyRequestsException("OTP request limit exceeded. Please try again later.");
        }

        await InvalidateOldOtpsAsync(normalizedEmail);

        var otpCode = Random.Shared.Next(100000, 999999).ToString();
        var otpHash = _securityService.Hash(otpCode);

        await _redisOtpService.SaveOtpAsync(
            normalizedEmail,
            otpHash,
            TimeSpan.FromMinutes(_otpOption.ExpiryMinutes));
        _logger.LogInformation("OTP stored in Redis for {Email} with expiry {ExpiryMinutes} minutes", normalizedEmail, _otpOption.ExpiryMinutes);

        await _redisOtpService.IncrementSendCountAsync(normalizedEmail);
        _logger.LogInformation("OTP send count incremented for {Email}", normalizedEmail);

        try
        {
            var mailContent = new MailContent()
            {
                To = normalizedEmail,
                Subject = "AutoWash Pro – Your One-Time Password (OTP)",
                Body = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>AutoWash Pro Verification Code</title>
</head>

<body style=""
    margin:0;
    padding:40px 16px;
    background:#EEEAE2;
    font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
<tr>
<td align=""center"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
       style=""
            max-width:640px;
            background:#FFFDF9;
            border-radius:24px;
            overflow:hidden;
            box-shadow:0 20px 80px rgba(15,15,15,0.12);
       "">

    <tr>
        <td style=""
            background:#0A0A0B;
            padding:40px 48px;
            text-align:center;
            border-top:4px solid #C6A56A;
        "">

        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
            <tr>
                <td align=""center"" style=""padding:40px 0 24px;"">

                    <table role=""presentation""
                           cellpadding=""0""
                           cellspacing=""0""
                           border=""0""
                           width=""90""
                           height=""90""
                           style=""
                                width:90px;
                                height:90px;
                                background:#0B0B0B;
                                border:1px solid #D4AF37;
                                border-radius:24px;
                                box-shadow:0 12px 30px rgba(0,0,0,.25);
                           "">
                        <tr>
                            <td align=""center""
                                valign=""middle""
                                style=""
                                    width:90px;
                                    height:90px;
                                    color:#D4AF37;
                                    font-size:38px;
                                    font-weight:bold;
                                    font-family:Georgia,'Times New Roman',serif;
                                    letter-spacing:2px;
                                    line-height:38px;
                                "">
                                AW
                            </td>
                        </tr>
                    </table>

                </td>
            </tr>
        </table>

            <p style=""
                margin:24px 0 8px;
                color:#C6A56A;
                font-size:11px;
                letter-spacing:3px;
                font-weight:600;
                text-transform:uppercase;
            "">
                Security Verification
            </p>

            <h1 style=""
                margin:0;
                color:#FFFDF9;
                font-size:32px;
                font-weight:600;
                line-height:1.3;
            "">
                Verify Your Identity
            </h1>

        </td>
    </tr>

    <tr>
        <td style=""padding:48px;"">

            <h2 style=""
                margin:0 0 16px;
                color:#111111;
                font-size:24px;
                font-weight:600;
            "">
                One-Time Password
            </h2>

            <p style=""
                margin:0;
                color:#5A564F;
                font-size:15px;
                line-height:1.8;
            "">
                We received a request to verify your AutoWash Pro account.
                Please use the verification code below to continue.
            </p>

            <table
                width=""100%""
                cellpadding=""0""
                cellspacing=""0""
                border=""0""
                style=""
                    margin:36px 0;
                    background:#111112;
                    border:1px solid #292722;
                    border-radius:20px;
                "">
                <tr>
                    <td
                        align=""center""
                        style=""padding:36px;"">

                        <div style=""
                            color:#8C877D;
                            font-size:11px;
                            letter-spacing:3px;
                            text-transform:uppercase;
                            margin-bottom:18px;
                        "">
                            Verification Code
                        </div>

                        <div style=""
                            color:#E3C581;
                            font-size:52px;
                            font-weight:700;
                            letter-spacing:12px;
                            font-family:'Courier New', monospace;
                            line-height:1;
                        "">
                            {otpCode}
                        </div>

                    </td>
                </tr>
            </table>

            <table
                width=""100%""
                cellpadding=""0""
                cellspacing=""0""
                border=""0""
                style=""
                    background:#F5F1E9;
                    border:1px solid #E2DACB;
                    border-radius:16px;
                "">
                <tr>
                    <td style=""padding:24px;"">

                        <p style=""
                            margin:0 0 12px;
                            color:#111111;
                            font-size:14px;
                            font-weight:600;
                        "">
                            Security Information
                        </p>

                        <p style=""
                            margin:0;
                            color:#5A564F;
                            font-size:14px;
                            line-height:1.8;
                        "">
                            This verification code will expire in
                            <strong>{_otpOption.ExpiryMinutes} minutes</strong>.
                            For your security, never share this code with anyone,
                            including AutoWash Pro employees.
                        </p>

                    </td>
                </tr>
            </table>

            <p style=""
                margin:32px 0 0;
                color:#69645B;
                font-size:14px;
                line-height:1.8;
            "">
                If you did not request this verification, you can safely ignore this email.
            </p>

            <div style=""
                margin-top:40px;
                border-top:1px solid #E6E0D5;
                height:1px;
            ""></div>

            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                    <td style=""padding-top:24px;"">

                        <p style=""
                            margin:0;
                            color:#111111;
                            font-size:15px;
                            font-weight:600;
                        "">
                            AutoWash Pro Team
                        </p>

                        <p style=""
                            margin:6px 0 0;
                            color:#77726B;
                            font-size:13px;
                        "">
                            Smart Booking • Loyalty Rewards • Premium Care
                        </p>

                    </td>
                </tr>
            </table>

        </td>
    </tr>

    <tr>
        <td style=""
            background:#0A0A0B;
            text-align:center;
            padding:30px;
            border-top:1px solid #292722;
        "">

            <p style=""
                margin:0 0 10px;
                color:#C6A56A;
                font-size:10px;
                letter-spacing:3px;
                font-weight:700;
                text-transform:uppercase;
            "">
                AutoWash Pro
            </p>

            <p style=""
                margin:0;
                color:#77726B;
                font-size:11px;
                letter-spacing:1px;
            "">
                SMART BOOKING • LOYALTY REWARDS • PREMIUM CARE
            </p>

        </td>
    </tr>

</table>

</td>
</tr>
</table>

</body>
</html>
"
            };

            _logger.LogInformation("Sending OTP email to {Email}", normalizedEmail);
            await _mailService.SendMail(mailContent);
        }
        catch (Exception ex)
        {
            await InvalidateOldOtpsAsync(normalizedEmail);
            _logger.LogError(ex, "Failed to send OTP email to {Email}. OTP has been invalidated.", normalizedEmail);
            throw;
        }
    }

    public async Task<bool> VerifyOtpAsync(string email, string otpCode)
    {
        var normalizedEmail = NormalizeEmail(email);
        var otpHash = await _redisOtpService.GetOtpAsync(normalizedEmail);

        if (string.IsNullOrWhiteSpace(otpHash))
        {
            return false;
        }

        var failedAttemptCount = await _redisOtpService.GetVerifyAttemptCountAsync(normalizedEmail);
        if (failedAttemptCount >= _otpOption.MaxFailedAttempts)
        {
            await InvalidateOldOtpsAsync(normalizedEmail);
            return false;
        }

        var isOtpValid = _securityService.Verify(otpCode, otpHash);
        if (!isOtpValid)
        {
            failedAttemptCount = await _redisOtpService.IncrementVerifyAttemptAsync(normalizedEmail);

            if (failedAttemptCount >= _otpOption.MaxFailedAttempts)
            {
                await InvalidateOldOtpsAsync(normalizedEmail);
            }

            return false;
        }

        await InvalidateOldOtpsAsync(normalizedEmail);
        return true;
    }

    public async Task InvalidateOldOtpsAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        await _redisOtpService.DeleteOtpAsync(normalizedEmail);
        await _redisOtpService.ResetVerifyAttemptCountAsync(normalizedEmail);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
