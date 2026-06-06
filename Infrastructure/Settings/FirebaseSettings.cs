namespace CRM.Api.Infrastructure.Settings;

/// <summary>Bind từ appsettings.json section "Firebase".</summary>
public sealed class FirebaseSettings
{
    public const string SectionName = "Firebase";

    public string CredentialsPath { get; set; } = "firebase-credentials.json";
}
