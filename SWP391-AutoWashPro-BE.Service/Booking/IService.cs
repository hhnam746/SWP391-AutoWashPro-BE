using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Booking;

public interface IService
{
    public Task<Response.GetBookingSlotsResponse> GetBookingSlots (Guid BranchId, DateOnly Date);
    public Task<Response.CreateBookingResponse> CreateBooking (Request.CreateBookingRequest bookingRequest);
    public Task<Response.GetBookingsResponse> GetBookings(BookingStatus? status, DateOnly fromDate, DateOnly toDate, int page, int pageSize);
    public Task<Response.GetBookingDetailResponse> GetBookingById (Guid bookingId);
    public Task<Response.CheckInBookingResponse> CheckInBooking (Guid Id);
    public Task<Response.CancelBookingResponse> CancelBooking (Guid Id, Request.CancelBookingRequest bookingRequest);
}