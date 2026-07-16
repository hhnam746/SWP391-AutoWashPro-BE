using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace SWP391_AutoWashPro_BE.Service.MailService;

public class Service : IService
{
    private readonly MailOptions _mailOptions = new();

    public Service(IConfiguration configuration)
    {
        configuration.GetSection("MailOptions").Bind(_mailOptions);
    }

    public async Task SendMail(MailContent mailContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_mailOptions.Mail))
        {
            throw new InvalidOperationException("MailOption:Mail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_mailOptions.Host))
        {
            throw new InvalidOperationException("MailOption:Host is not configured.");
        }

        MimeMessage email = new();
        email.Sender = new MailboxAddress(_mailOptions.DisplayName, _mailOptions.Mail);
        email.From.Add(new MailboxAddress(_mailOptions.DisplayName, _mailOptions.Mail));
        email.To.Add(MailboxAddress.Parse(mailContent.To));
        email.Subject = mailContent.Subject;


        BodyBuilder builder = new();
        builder.HtmlBody = mailContent.Body;
        email.Body = builder.ToMessageBody();

        // dùng SmtpClient của MailKit
        using SmtpClient smtp = new();

        await smtp.ConnectAsync(
            _mailOptions.Host,
            _mailOptions.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);
        await smtp.AuthenticateAsync(_mailOptions.Mail, _mailOptions.Password, cancellationToken);
        await smtp.SendAsync(email, cancellationToken);

        await smtp.DisconnectAsync(true, cancellationToken);
    }
}
