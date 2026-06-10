using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Notification;

public class Request
{
    public class UpdateNotificationStatusRequest
    {
        public List<Guid> Ids { get; set; }
        public bool IsRead { get; set; }
        public bool MarkAll { get; set; }
    }
    
    public class SendNotificationRequest
    {
        public Guid? UserId { get; set; }
        public string? Data { get; set; }
        public NotificationType Type { get; set; }
        public string? RedirectUrl { get; set; }
        public string? Metadata { get; set; }
    }
}