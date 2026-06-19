using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Service.MailService;

namespace SWP391_AutoWashPro_BE.Service.OtpService;

public class Service : IService
{
    private readonly OtpOptions _otpOption = new();
    private readonly AppDbContext _dbContext;
    private readonly Security.IService _securityService;
    private readonly MailService.IService _mailService;
    private readonly ILogger<Service> _logger;

    public Service(
        IConfiguration configuration,
        AppDbContext dbContext,
        Security.IService securityService,
        MailService.IService mailService,
        ILogger<Service> logger)
    {
        configuration.GetSection(nameof(OtpOptions)).Bind(_otpOption);
        _dbContext = dbContext;
        _securityService = securityService;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendOtpAsync(Guid userId, string email)
    {
        // 1. Vô hiệu hóa các OTP cũ đang có hiệu lực của user
        await InvalidateOldOtpsAsync(userId);

        // 2. Tạo mã OTP ngẫu nhiên (6 chữ số)
        var otpCode = Random.Shared.Next(100000, 999999).ToString();

        // 3. Hash mã OTP và lưu vào db
        var otp = new Otp
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OtpHash = _securityService.Hash(otpCode),
            FailedAttemptCount = 0,
            IsUsed = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_otpOption.ExpiryMinutes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Otps.Add(otp);
        await _dbContext.SaveChangesAsync();

        // 4. Gửi OTP qua email (fire & forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var mailContent = new MailContent()
                {
                    To = email,
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

    <!-- Header -->
    <tr>
        <td style=""
            background:#0A0A0B;
            padding:40px 48px;
            text-align:center;
            border-top:4px solid #C6A56A;
        "">

        <!-- Premium Logo -->
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

    <!-- Content -->
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

            <!-- OTP Card -->
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

            <!-- Security Box -->
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

            <!-- Divider -->
            <div style=""
                margin-top:40px;
                border-top:1px solid #E6E0D5;
                height:1px;
            ""></div>

            <!-- Signature -->
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

    <!-- Footer -->
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
                await _mailService.SendMail(mailContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi OTP qua email đến {Email}", email);
            }
        });

        return otp.Id.ToString();
    }

    public async Task<bool> VerifyOtpAsync(Guid userId, string otpCode)
    {
        // Lấy OTP mới nhất, chưa sử dụng và chưa hết hạn của user
        var otp = await _dbContext.Otps
            .Where(x => x.UserId == userId && !x.IsUsed && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            return false;
        }

        // Kiểm tra xem đã vượt quá số lần thử tối đa chưa
        if (otp.FailedAttemptCount >= _otpOption.MaxFailedAttempts)
        {
            return false;
        }

        // Kiểm tra mã OTP
        var isOtpValid = _securityService.Verify(otpCode, otp.OtpHash);

        if (!isOtpValid)
        {
            // Nếu sai thì tăng số lần thử
            otp.FailedAttemptCount++;
            otp.UpdatedAt = DateTimeOffset.UtcNow;

            _dbContext.Otps.Update(otp);
            await _dbContext.SaveChangesAsync();
            return false;
        }

        // Nếu đúng thì cập nhật trạng thái đã sử dụng
        otp.IsUsed = true;
        otp.UsedAt = DateTimeOffset.UtcNow;
        otp.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Otps.Update(otp);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task InvalidateOldOtpsAsync(Guid userId)
    {
        var activeOtps = await _dbContext.Otps
            .Where(x => x.UserId == userId && !x.IsUsed && x.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync();

        if (activeOtps.Any())
        {
            foreach (var otp in activeOtps)
            {
                // Thay vì xóa mềm (IsDeleted) mà bảng Otp không có, ta ép IsUsed = true và đổi ngày Expire về quá khứ
                otp.IsUsed = true;
                otp.ExpiresAt = DateTimeOffset.UtcNow;
                otp.UpdatedAt = DateTimeOffset.UtcNow;
            }

            _dbContext.Otps.UpdateRange(activeOtps);
            await _dbContext.SaveChangesAsync();
        }
    }
}