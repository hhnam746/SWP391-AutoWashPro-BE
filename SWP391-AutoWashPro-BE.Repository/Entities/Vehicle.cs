using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Vehicle : BaseEntity, IAuditableEntity
{
    public Guid CustomerId { get; set; }
    public CustomerProfile Customer { get; set; } = null!;

    public string LicensePlate { get; set; } = null!;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Guid VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public ICollection<VehicleImage> VehicleImages { get; set; } = new List<VehicleImage>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
