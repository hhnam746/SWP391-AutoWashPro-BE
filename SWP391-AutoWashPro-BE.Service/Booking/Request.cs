using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Booking;

public class Request
{
    public class GetBookingSlotsRequest
    {
        public required Guid BranchId { get; set; }
        public required DateTime Date { get; set; }
        public Guid? VehicleId { get; set; }
    }

    public class CreateBookingRequest
    {
        public required Guid BranchId { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid? VoucherId { get; set; }
        public required DateOnly BookingDate { get; set; }
        public required DateTimeOffset StartTime { get; set; }
        public required bool? redemPoint { get; set; }
        public IReadOnlyCollection<Guid> AcknowledgedScheduleConflictIds { get; set; } = Array.Empty<Guid>();
       
    }

    public class GetBookingsRequest
    {
        public BookingStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CheckInBookingRequest
    {
        public required BookingStatus isCheckin { get; set; }
    }

    public class CancelBookingRequest
    {
        public required string Reason { get; set; }
    }
}
