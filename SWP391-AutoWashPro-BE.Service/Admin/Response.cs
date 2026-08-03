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

    public class DashboardResponse
    {
        public DashboardSummaryResponse Summary { get; set; } = new();
        public List<DashboardTodayBookingResponse> TodayBookings { get; set; } = new();
        public List<DashboardTopBranchResponse> TopBranches { get; set; } = new();
    }

    public class DashboardSummaryResponse
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int LockedCustomers { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
    }

    public class DashboardTodayBookingResponse
    {
        public Guid Id { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
    }

    public class DashboardTopBranchResponse
    {
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int CompletedBookings { get; set; }
        public decimal Revenue { get; set; }
    }

    public class GetUserStatusResponse
    {
        public Guid UserId { get; set; }
        public AccountStatus Status { get; set; }
        public bool IsVerify { get; set; }
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
        public DateOnly? DateOfBirth { get; set; }
        public int TotalPoints { get; set; }
        public int TotalWashes { get; set; }
        public List<string> FaceImageUrls { get; set; } = new();
        public TierData? TierData { get; set; }
    }

    public class TierData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int PriorityBookingDays { get; set; }
        public int RequiredWashes { get; set; }
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
        public int TotalItems { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
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

    public class RevenueReportResponse
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<RevenueReportItemResponse> Data { get; set; } = new();
    }

    public class RevenueReportItemResponse
    {
        public DateOnly Date { get; set; }
        public int BookingCount { get; set; }
        public int CompletedBookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class BranchReportItemResponse
    {
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal Revenue { get; set; }
    }

    public class LoyaltyReportResponse
    {
        public LoyaltySummaryResponse Summary { get; set; } = new();
        public List<TierDistributionItemResponse> TierDistribution { get; set; } = new();
    }

    public class LoyaltySummaryResponse
    {
        public int TotalPointsEarned { get; set; }
        public int TotalPointsRedeemed { get; set; }
        public int TotalRewardsRedeemed { get; set; }
        public int TierUpgradeCount { get; set; }
    }

    public class WalletTopupTransactionItemResponse
    {
        public Guid TransactionId { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionStatus? Status { get; set; }
        public ProviderType? Provider { get; set; }
        public string? ReferenceCode { get; set; }
        public string? ExternalTransactionId { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class TierDistributionItemResponse
    {
        public string TierName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
    }

    public class CompleteBookingByAdminResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CompletedAt { get; set; }
        public int PointsEarned { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CancelBookingByAdminResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CancelledAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
