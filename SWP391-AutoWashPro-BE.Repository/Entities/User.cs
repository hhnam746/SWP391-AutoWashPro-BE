using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class User : BaseEntity, IAuditableEntity
{
    public string? Email { get; set; }
    public string Phone { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Customer;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public bool isVerify { get; set; } = false;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public CustomerProfile? CustomerProfile { get; set; }
    public ICollection<UserFaceImage> UserFaceImages { get; set; } = new List<UserFaceImage>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<SystemConfig> UpdatedSystemConfigs { get; set; } = new List<SystemConfig>();
}
