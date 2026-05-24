using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Booking;

public class Response
{
   public class GetBookingSlotsResponse()
    {
        public required Guid BranchId { get; set; }
        public required DateOnly Date { get; set; }
        public required int SlotDurationMinutes { get; set; }
        public required List<SlotStatus> Data { get; set; }
    }

    public class SlotStatus
    {
        public required DateTimeOffset StartTime { get; set; }
        public required DateTimeOffset EndTime { get; set; }
        public required BookingStatus Status { get; set; }
    }

    public class CreateBookingResponse
    {
        public required Guid Id { get; set; }
        public required BookingStatus Status { get; set; }
        public required BookingBranch Branch { get; set; }
        public required BookingVehicle Vehicle { get; set; }
        public required DateOnly BookingDate { get; set; }
        public required DateTimeOffset StartTime { get; set; }
        public required DateTimeOffset EndTime { get; set; }
        public required decimal BasePrice { get; set; }
        public required decimal DiscountAmount { get; set; }
        public required decimal FinalPrice { get; set; }
    }

    public class GetBookingsResponse
    {
        public required List<BookingItem> Data { get; set; }
        public required Pagination Pagination { get; set; }
    }

    public class BookingItem
    {
        public required Guid Id { get; set; }
        public required BookingStatus Status { get; set; }
        public required DateOnly BookingDate { get; set; }
        public required DateTimeOffset StartTime { get; set; }
        public required DateTimeOffset EndTime { get; set; }
        public required BookingBranchDetail Branch { get; set; }
        public required BookingVehicle Vehicle { get; set; }
        public required decimal FinalPrice { get; set; }
    }

    public class GetBookingDetailResponse
    {
        public required Guid Id { get; set; }
        public required BookingStatus Status { get; set; }
        public required DateOnly BookingDate { get; set; }
        public required DateTimeOffset StartTime { get; set; }
        public required DateTimeOffset EndTime { get; set; }
        public required BookingBranchDetail Branch { get; set; }
        public required BookingVehicleDetail Vehicle { get; set; }
        public VoucherInfo? Voucher { get; set; }
        public required decimal BasePrice { get; set; }
        public required decimal DiscountAmount { get; set; }
        public required decimal FinalPrice { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CheckInBookingResponse
    {
        public required Guid Id { get; set; }
        public required BookingStatus Status { get; set; }
        public required DateTimeOffset CheckedInAt { get; set; }
        public required DateTimeOffset EstimatedCompletedAt { get; set; }
        public required string Message { get; set; }
    }

    public class CancelBookingResponse
    {
        public required Guid Id { get; set; }
        public required BookingStatus Status { get; set; }
        public required DateTime CancelledAt { get; set; }
        public required string Message { get; set; }
    }

    public class BookingBranch
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
    }

    public class BookingBranchDetail
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
    }

    public class BookingVehicle
    {
        public required Guid Id { get; set; }
        public required string LicensePlate { get; set; }
    }

    public class BookingVehicleDetail
    {
        public required Guid Id { get; set; }
        public required string LicensePlate { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
    }

    public class VoucherInfo
    {
        public required Guid? Id { get; set; }
        public required string? Code { get; set; }
        public required decimal? DiscountAmount { get; set; }
    }

    public class Pagination
    {
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalCount { get; set; }
        public required int TotalPages { get; set; }
    }
}