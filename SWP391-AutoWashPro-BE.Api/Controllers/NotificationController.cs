using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Notification;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("/api/v1/notifications")]

public class NotificationController : ControllerBase
{
    private readonly IService _service;
    public NotificationController(IService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] NotificationType? type, bool? isRead,
        int page, int pageSize)
    {
        var result = await _service.GetNotification(type, isRead, page, pageSize);
        return Ok(result);
    }

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Request.UpdateNotificationStatusRequest request)
    {
        var result = await _service.UpdateNotificationStatus(request);
        return Ok(result);
    }
}