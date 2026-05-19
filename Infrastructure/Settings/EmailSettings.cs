namespace CRM.Api.Infrastructure.Settings;

/// <summary>Bind từ appsettings.json section "Email".</summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost     { get; set; } = string.Empty;
    public int    SmtpPort     { get; set; } = 587;
    public string SmtpUser     { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool   EnableSsl    { get; set; } = true;
    public string FromAddress  { get; set; } = string.Empty;
    public string FromName     { get; set; } = "CRM Customer 360";
}
