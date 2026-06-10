using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class VehicleType : BaseEntity, IAuditableEntity
{
    
    public VehicleTypes TypeName { get; set; }
    public int VehicleSlot { get; set; }
    public int SizeLevel { get; set; }
    
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}