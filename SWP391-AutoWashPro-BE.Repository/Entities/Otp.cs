using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Otp : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string OtpHash { get; set; } = null!;
    public int FailedAttemptCount { get; set; } //Số lần nhập OTP sai
    public bool IsUsed { get; set; }

    public DateTimeOffset ExpiresAt { get; set; } //thời gian hết hạn của OTP
    
    public DateTimeOffset? UsedAt { get; set; } //Người dùng sử dụng khi nào

    public DateTimeOffset CreatedAt { get; set; } //để đây thôi
    public DateTimeOffset? UpdatedAt { get; set; }
}