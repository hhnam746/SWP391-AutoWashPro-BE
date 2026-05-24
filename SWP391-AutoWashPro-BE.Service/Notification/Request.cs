namespace SWP391_AutoWashPro_BE.Service.Notification;

public class Request
{
    public class UpdateNotificationStatusRequest
    {
        public List<Guid> Ids { get; set; }
        public bool IsRead { get; set; }
        public bool MarkAll { get; set; }
    }
}