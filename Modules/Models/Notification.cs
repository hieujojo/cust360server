using CRM.Api.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

/// <summary>In-app notification. Collection: notifications.</summary>
[BsonIgnoreExtraElements]
public sealed class Notification : IOrganizationDocument, ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;
    public string userId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public NotificationType type { get; set; }

    public string title { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public string contextUrl { get; set; } = string.Empty;
    public bool isRead { get; set; }
    public bool isDeleted { get; set; }
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

    bool ISoftDeletable.IsDeleted
    {
        get => isDeleted;
        set => isDeleted = value;
    }
}

public enum NotificationType
{
    DealAssigned,
    DealMoved,
    TicketCreated,
}
