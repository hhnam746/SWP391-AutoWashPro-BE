using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Notification;

public static class NotificationTemplateProvider
{
    public static (string Title, string Content) GetTemplate(NotificationType type, string? data)
    {
        return type switch
        {
            NotificationType.BookingCreated =>
                ("Đặt lịch thành công", $"Lịch rửa xe của bạn đã được xác nhận. {data ?? string.Empty}".Trim()),

            NotificationType.BookingReminder =>
                ("Nhắc lịch rửa xe", $"Bạn có lịch rửa xe sắp đến. {data ?? string.Empty}".Trim()),

            NotificationType.BookingCancelled =>
                ("Lịch rửa xe đã hủy", $"Lịch rửa xe của bạn đã bị hủy. {data ?? string.Empty}".Trim()),

            NotificationType.BookingCompleted =>
                ("Hoàn tất rửa xe", $"Dịch vụ rửa xe của bạn đã hoàn tất. {data ?? "Cảm ơn bạn đã sử dụng dịch vụ."}".Trim()),

            NotificationType.TierUpgraded =>
                ("Nâng hạng thành viên", $"Chúc mừng! Hạng thành viên của bạn đã được nâng cấp. {data ?? string.Empty}".Trim()),

            NotificationType.RewardRedeemed =>
                ("Đổi thưởng thành công", $"Bạn đã đổi thưởng thành công. {data ?? string.Empty}".Trim()),

            NotificationType.IdentityApproved =>
                ("Xác minh tài khoản thành công", data ?? "Hồ sơ xác minh của bạn đã được duyệt thành công."),

            NotificationType.IdentityRejected =>
                ("Xác minh tài khoản bị từ chối", $"Hồ sơ xác minh của bạn đã bị từ chối. {data ?? string.Empty}".Trim()),

            NotificationType.SystemAlert =>
                ("Thông báo hệ thống", data ?? "Bạn có thông báo mới."),

            _ =>
                ("Thông báo", "Bạn có thông báo mới.")
        };
    }
}