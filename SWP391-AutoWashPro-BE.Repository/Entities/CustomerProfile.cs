using SWP391_AutoWashPro_BE.Repository.Abstraction;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class CustomerProfile : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TierId { get; set; }
    public Tier Tier { get; set; } = null!;

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Cccd { get; set; }
    public int TotalPoints { get; set; }
    public int TotalWashes { get; set; }
    public DateTimeOffset? LastPointActivityAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? WalletId { get; set; }
    public Wallet? Wallet { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
