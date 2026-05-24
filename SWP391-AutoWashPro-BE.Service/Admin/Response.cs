using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Response
{
    public class BranchResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class GetUserStatusResponse
    {
        public Guid UserId { get; set; }
        public AccountStatus Status { get; set; }
    }

    public class AllProfileResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public UserRole Role { get; set; }
        public AccountStatus Status { get; set; }
        public bool IsVerified { get; set; }
        public ProfileData? ProfileData { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public int VehicleCount { get; set; }
        public int ActiveBookingCount { get; set; }
    }

    public class ProfileData
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Cccd { get; set; }
        public int TotalPoints { get; set; }
        public int TotalWashes { get; set; }
        public TierData? TierData { get; set; }
    }

    public class TierData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
    }

    public class GetUserByIdResponse : AllProfileResponse
    {
        public WalletResponse? Wallet { get; set; }
        public List<VehicleResponse> Vehicles { get; set; } = new();
    }

    public class WalletResponse
    {
        public decimal Balance { get; set; }
    }

    public class VehicleResponse
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; }
        public bool IsActive { get; set; }
    }

    public class BookingListResponse
    {
        public List<BookingResponse> Data { get; set; } = new();
    }
    

    public class BookingResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly BookingDate { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public BookingCustomerResponse Customer { get; set; } = new();
        public BookingVehicleResponse Vehicle { get; set; } = new();
        public BookingBranchResponse Branch { get; set; } = new();
        public decimal FinalPrice { get; set; }
    }

    public class BookingCustomerResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string TierName { get; set; } = string.Empty;
    }

    public class BookingVehicleResponse
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
    }

    public class BookingBranchResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class BookingSlotResponse
    {
        public Guid BranchId { get; set; }
        public DateOnly Date { get; set; }
        public int SlotDurationMinutes { get; set; }
        public List<SlotDataResponse> Data { get; set; } = new();
    }

    public class SlotDataResponse
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public SlotBookingResponse? Booking { get; set; }
    }

    public class SlotBookingResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
