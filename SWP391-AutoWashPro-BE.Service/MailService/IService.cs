namespace SWP391_AutoWashPro_BE.Service.MailService;

public interface IService
{
    public Task SendMail(MailContent mailContent, CancellationToken cancellationToken = default);
}

public class MailContent
{
    public required string To { get; set; }          // Địa chỉ gửi đến
    public required string Subject { get; set; }     // Chủ đề (tiêu đề email)
    public required string Body { get; set; }        // Nội dung (hỗ trợ HTML) của email
}
