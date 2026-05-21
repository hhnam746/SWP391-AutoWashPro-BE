using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Branch : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
