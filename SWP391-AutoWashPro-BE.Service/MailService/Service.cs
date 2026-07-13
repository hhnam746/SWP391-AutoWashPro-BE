using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace SWP391_AutoWashPro_BE.Service.MailService;

public class Service : IService
{
    private readonly MailOptions _mailOptions = new();
    private readonly ILogger<Service> _logger;

    public Service(IConfiguration configuration, ILogger<Service> logger)
    {
        configuration.GetSection("MailOptions").Bind(_mailOptions);
        _logger = logger;
    }

    public async Task SendMail(MailContent mailContent)
    {
        ValidateMailOptions();

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
        smtp.Timeout = 10000;

        _logger.LogInformation(
            "Sending email to {Recipient} via SMTP host {Host}:{Port} using {SecurityOption}",
            mailContent.To,
            _mailOptions.Host,
            _mailOptions.Port,
            SecureSocketOptions.StartTls);

        try
        {
            await smtp.ConnectAsync(_mailOptions.Host, _mailOptions.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_mailOptions.Mail, _mailOptions.Password);
            await smtp.SendAsync(email);
            _logger.LogInformation("Email sent successfully to {Recipient}", mailContent.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SMTP send failed for {Recipient} via {Host}:{Port}",
                mailContent.To,
                _mailOptions.Host,
                _mailOptions.Port);
            throw new InvalidOperationException("Failed to send OTP email.", ex);
        }

        await smtp.DisconnectAsync(true);
    }

    private void ValidateMailOptions()
    {
        if (string.IsNullOrWhiteSpace(_mailOptions.Mail))
        {
            throw new InvalidOperationException("MailOptions:Mail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_mailOptions.DisplayName))
        {
            throw new InvalidOperationException("MailOptions:DisplayName is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_mailOptions.Password))
        {
            throw new InvalidOperationException("MailOptions:Password is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_mailOptions.Host))
        {
            throw new InvalidOperationException("MailOptions:Host is not configured.");
        }

        if (_mailOptions.Port <= 0)
        {
            throw new InvalidOperationException("MailOptions:Port is not configured.");
        }
    }
}
