using SWP391_AutoWashPro_BE.Service.Booking;

namespace SWP391_AutoWashPro_BE.Service.Models;

public sealed class BookingScheduleWarningException : Exception
{
    public BookingScheduleWarningException(Response.ScheduleWarning warning)
        : base("Booking time is too close to another booking.")
    {
        Warning = warning;
    }

    public Response.ScheduleWarning Warning { get; }
}
