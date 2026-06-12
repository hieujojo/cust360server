using System.Net;
using System.Net.Mail;
using CRM.Api.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRM.Api.Infrastructure.Email;

/// <summary>SMTP implementation. Swap provider bằng cách implement lại IEmailService.</summary>
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger   = logger;
    }

    public async Task SendAccountCreatedAsync(
        string toEmail,
        string displayName,
        string password,
        CancellationToken ct = default
    )
    {
        var subject = "Tài khoản CRM của bạn đã được tạo";
        var body = $"""
            <h2>Xin chào {displayName},</h2>
            <p>Tài khoản CRM Customer 360 của bạn đã được tạo bởi quản trị viên.</p>
            <p><strong>Email:</strong> {toEmail}</p>
            <p><strong>Mật khẩu:</strong> {password}</p>
            <br/><p>Trân trọng,<br/>CRM Customer 360</p>
            """;

        await SendAsync(toEmail, subject, body, ct);
    }

    public async Task SendAccountDeactivatedAsync(
        string toEmail,
        string displayName,
        CancellationToken ct = default
    )
    {
        var subject = "Tài khoản CRM của bạn đã bị vô hiệu hóa";
        var body = $"""
            <h2>Xin chào {displayName},</h2>
            <p>Tài khoản CRM Customer 360 của bạn đã bị vô hiệu hóa bởi quản trị viên.</p>
            <p>Vui lòng liên hệ quản trị viên nếu có thắc mắc.</p>
            <br/><p>Trân trọng,<br/>CRM Customer 360</p>
            """;

        await SendAsync(toEmail, subject, body, ct);
    }

    public async Task SendPasswordResetAsync(
        string toEmail,
        string displayName,
        string newPassword,
        CancellationToken ct = default
    )
    {
        var subject = "Mật khẩu CRM của bạn đã được đặt lại";
        var body = $"""
            <h2>Xin chào {displayName},</h2>
            <p>Mật khẩu tài khoản CRM Customer 360 của bạn đã được đặt lại bởi quản trị viên.</p>
            <p><strong>Email:</strong> {toEmail}</p>
            <p><strong>Mật khẩu mới:</strong> {newPassword}</p>
            <p>Vui lòng đổi mật khẩu ngay sau khi đăng nhập.</p>
            <br/><p>Trân trọng,<br/>CRM Customer 360</p>
            """;

        await SendAsync(toEmail, subject, body, ct);
    }

    public async Task SendPasswordResetLinkAsync(
        string toEmail,
        string displayName,
        string resetLink,
        CancellationToken ct = default
    )
    {
        var subject = "Yêu cầu đặt lại mật khẩu CRM";
        var body = $"""
            <h2>Xin chào {displayName},</h2>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            <p>Nhấn vào link bên dưới để đặt lại mật khẩu. Link có hiệu lực trong <strong>15 phút</strong>.</p>
            <p><a href="{resetLink}" style="padding:10px 20px;background:#4F46E5;color:white;border-radius:6px;text-decoration:none;">Đặt lại mật khẩu</a></p>
            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
            <br/><p>Trân trọng,<br/>CRM Customer 360</p>
            """;

        await SendAsync(toEmail, subject, body, ct);
    }

    private async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct
    )
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword),
                EnableSsl = _settings.EnableSsl,
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Email sent to {To} | subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} | subject: {Subject}", toEmail, subject);
        }
    }
}
