using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class CustomerDateOfBirthCorrection : BaseEntity, IAuditableEntity
{
    public Guid CustomerId { get; set; }
    public CustomerProfile Customer { get; set; } = null!;
    public Guid AdminUserId { get; set; }
    public User AdminUser { get; set; } = null!;
    public DateOnly? PreviousDateOfBirth { get; set; }
    public DateOnly NewDateOfBirth { get; set; }
    public string Reason { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
