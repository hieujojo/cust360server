using CRM.Api.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

[BsonIgnoreExtraElements]
public sealed class UserGoogleToken : IOrganizationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;
    public string userId { get; set; } = string.Empty;
    public string googleEmail { get; set; } = string.Empty;
    public string encryptedRefreshToken { get; set; } = string.Empty;
    public string? accessToken { get; set; }
    public DateTime? accessTokenExpiresAt { get; set; }
    public string? calendarSyncToken { get; set; }
    public DateTime? gmailWatchExpiration { get; set; }
    public string? gmailHistoryId { get; set; }
    public bool calendarSyncEnabled { get; set; } = true;
    public bool gmailSyncEnabled { get; set; } = true;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;

    string IOrganizationDocument.Id
    {
        get => id;
        set => id = value;
    }

    string IOrganizationDocument.OrganizationId
    {
        get => organizationId;
        set => organizationId = value;
    }
}
