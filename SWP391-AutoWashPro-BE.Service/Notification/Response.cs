using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Notification;

public class Response
{
    public class GetNotificationResponse
    {
        public required List<NotificationItem> Data { get; set; }
        public required int UnreadCount { get; set; }
        public required PaginationResponse Pagination { get; set; }
    }

    public class NotificationItem
    {
        public required Guid Id { get; set; }
        public required NotificationType Type { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required bool IsRead { get; set; }
        public required string MetaData { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
    }

    

    public class PaginationResponse
    {
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalCount { get; set; }
        public required int TotalPages { get; set; }
    }

    public class UpdateNotificationStatusResponse
    {
        public required int UpdatedCount { get; set; }
        public required int UnreadCount { get; set; }
    }
}