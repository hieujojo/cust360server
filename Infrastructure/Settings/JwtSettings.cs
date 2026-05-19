namespace CRM.Api.Infrastructure.Settings;

/// <summary>Bind từ appsettings.json section "Jwt".</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret   { get; set; } = string.Empty;
    public string Issuer   { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Phút. Default: 60.</summary>
    public int AccessTokenExpiryMinutes { get; set; } = 60;
}
