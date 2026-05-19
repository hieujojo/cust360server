namespace CRM.Api.Infrastructure.Settings;

/// <summary>Bind từ appsettings.json section "MongoDB".</summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDB";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName     { get; set; } = string.Empty;
}
