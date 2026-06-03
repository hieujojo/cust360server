namespace CRM.Api.Infrastructure.Settings;

public sealed class GoogleSettings
{
    public const string SectionName = "Google";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string PubSubTopic { get; set; } = string.Empty;
    public string TokenEncryptionKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int CalendarSyncIntervalMinutes { get; set; } = 15;
}
