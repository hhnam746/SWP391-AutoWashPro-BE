namespace SWP391_AutoWashPro_BE.Service.OtpService;

public interface IService
{
    // 1. Tạo và gửi OTP (trả về id của OTP hoặc gửi trực tiếp)
    // - Sinh mã OTP ngẫu nhiên (VD: 6 số)
    // - Hash mã OTP này lại (để lưu vào DB an toàn)
    // - Lưu record vào bảng Otp
    // - Gửi OTP thật (plaintext) qua email của người dùng
    Task<string> GenerateAndSendOtpAsync(Guid userId, string email);

    // 2. Xác nhận OTP người dùng nhập vào
    // - Kiểm tra mã OTP gửi lên (hash và so sánh với DB)
    // - Kiểm tra hết hạn (ExpiresAt)
    // - Kiểm tra số lần nhập sai (FailedAttemptCount) < MaxFailedAttempts
    // - Nếu đúng thì mark IsUsed = true và UsedAt = DateTimeOffset.UtcNow
    Task<bool> VerifyOtpAsync(Guid userId, string otpCode);

    // 3. (Tùy chọn) Xóa mềm hoặc vô hiệu hóa các OTP cũ của User này
    // Dùng để khi user yêu cầu gửi lại (Resend), các OTP cũ chưa hết hạn sẽ không xài được nữa.
    Task InvalidateOldOtpsAsync(Guid userId);
}