namespace CRM.Api.Infrastructure.Email;

public interface IEmailService
{
    Task SendAccountCreatedAsync(
        string toEmail, string displayName, string password,
        CancellationToken ct = default);

    Task SendAccountDeactivatedAsync(
        string toEmail, string displayName,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string toEmail, string displayName, string newPassword,
        CancellationToken ct = default);

    Task SendPasswordResetLinkAsync(
        string toEmail, string displayName, string resetLink,
        CancellationToken ct = default);
}
