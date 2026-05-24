using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Booking;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("/api/v1/bookings/")]
[Authorize]
public class BookingController:ControllerBase
{
    private readonly IService _service;
    public BookingController(IService service)
    {
        _service = service;
    }

    [HttpGet("slot")]
    public async Task<IActionResult> GetBookingSlots([FromQuery]Guid BranchId, [FromQuery] DateOnly Date)
    {
        var result = await _service.GetBookingSlots(BranchId, Date);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] Request.CreateBookingRequest bookingRequest)
    {
        var result = await _service.CreateBooking(bookingRequest);
        return Ok(result);
    }

    [HttpGet("")]
    public async Task<IActionResult> GetBooking(BookingStatus? status, DateOnly fromDate, DateOnly toDate, int page,
        int pageSize)
    {
        var result = await _service.GetBookings(status, fromDate, toDate, page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id}/check-in")]
    public async Task<IActionResult> CheckInBooking(Guid Id)
    {
        var result = await _service.CheckInBooking(Id);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookingById([FromQuery]Guid id)
    {
        var result = await _service.GetBookingById(id);
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid Id, Request.CancelBookingRequest bookingRequest)
    {
        var result = await _service.CancelBooking(Id, bookingRequest);
        return Ok(result);
    }
}