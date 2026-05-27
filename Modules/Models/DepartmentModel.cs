using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CRM.Api.Infrastructure.MongoDB;

namespace CRM.Api.Modules.Models;

/// <summary>Phòng ban của tổ chức. Collection: departments.</summary>
[BsonIgnoreExtraElements]
public sealed class Department : IOrganizationDocument , ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    
    public string organizationId { get; set; } = string.Empty;

    public string name { get; set; } = string.Empty;

    public string? description { get; set; }

    public bool isDeleted { get; set; } = false;

    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;

    
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

    bool ISoftDeletable.IsDeleted
    {
        get => isDeleted;
        set => isDeleted = value;
    }
}
