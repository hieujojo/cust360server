using MongoDB.Bson.Serialization.Attributes;
using CRM.Api.Infrastructure.MongoDB;

namespace CRM.Api.Modules.Models;

/// <summary>Team (nhóm) thuộc một phòng ban. Collection: teams.</summary>
[BsonIgnoreExtraElements]
public sealed class Team : IOrganizationDocument
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string id { get; set; } = string.Empty;

    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string organizationId { get; set; } = string.Empty;

    /// <summary>Phòng ban chứa team này.</summary>
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string departmentId { get; set; } = string.Empty;

    public string name { get; set; } = string.Empty;

    public string? description { get; set; }

    /// <summary>UserId của team lead. Nullable — team có thể chưa có lead.</summary>
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string? leadId { get; set; }

    public bool isDeleted { get; set; } = false;

    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;

    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string? createdBy { get; set; }

    // Bridge camelCase → IOrganizationDocument (PascalCase)
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
