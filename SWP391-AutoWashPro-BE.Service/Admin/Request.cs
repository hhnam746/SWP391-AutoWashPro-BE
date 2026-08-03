using System.ComponentModel.DataAnnotations;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Request
{
    public class UpdateUserByStatusRequest
    {
        [Required]
        [EnumDataType(typeof(AccountStatus))]
        public AccountStatus? Status { get; set; }
    }
    
    public class GetBookingRequest
    {
        public Guid? BranchId { get; set; }
        public DateOnly? Date { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public BookingStatus? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    
    public class GetBookingSlotRequest
    {
        public Guid BranchId { get; set; }
        public DateOnly Date { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetDashboardRequest
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public Guid? BranchId { get; set; }
    }

    public class CreateBranch
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
    }

    public class UpdateBranch
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public bool? IsActive { get; set; }
    }
    
    public class ReportRequest
    {
        public DateTimeOffset FromDate { get; set; }
        public DateTimeOffset ToDate { get; set; }
        public Guid? BranchId { get; set; }
    }

    public class GetRevenueReportRequest
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        // public Guid BranchId { get; set; }
    }

    public class GetBranchReportRequest
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetLoyaltyReportRequest
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
    }

    public class GetWalletTopupTransactionsRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
        public TransactionStatus? Status { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
    }

    public class CompleteBookingByAdminRequest
    {
        public string? Note { get; set; }
    }

    public class CancelBookingByAdminRequest
    {
        public string? Reason { get; set; }
    }

    public class RejectIdentityDocument
    {
        public required string RejectReason { get; set; }
    }

    public class CorrectCustomerDateOfBirthRequest
    {
        public DateOnly DateOfBirth { get; set; }
        public required string Reason { get; set; }
    }
}
