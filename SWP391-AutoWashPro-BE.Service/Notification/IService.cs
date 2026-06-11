using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Notification;

public interface IService
{
    public Task<Response.GetNotificationResponse> GetNotification(NotificationType? type, bool? isRead, int page, int pageSize);
    public Task<Response.UpdateNotificationStatusResponse> UpdateNotificationStatus(Request.UpdateNotificationStatusRequest request);
    public Task SendNotification(Request.SendNotificationRequest request);
}