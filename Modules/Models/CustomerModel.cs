using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CRM.Api.Infrastructure.MongoDB;

namespace CRM.Api.Modules.Models;

/// <summary>Khách hàng. Collection: customers. Hỗ trợ multi-tenancy qua organizationId.</summary>
[BsonIgnoreExtraElements]
public sealed class Customer : IOrganizationDocument , ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    
    public string organizationId { get; set; } = string.Empty;

    /// <summary>Auto-generated. Format: CUST-YYYY-NNNN.</summary>
    public string customerCode { get; set; } = string.Empty;

    public string name { get; set; } = string.Empty;

    /// <summary>Lead | Active | Inactive | Churned.</summary>
    public string status { get; set; } = "Lead";

    /// <summary>Website | Referral | Cold Call | Event | Partner | Other.</summary>
    public string source { get; set; } = "Website";

    public string? email { get; set; }
    public string? phone { get; set; }

    /// <summary>User ID chủ sở hữu customer.</summary>
    
    public string ownerId { get; set; } = string.Empty;

    /// <summary>Phòng ban sở hữu customer.</summary>
    
    public string departmentId { get; set; } = string.Empty;

    /// <summary>Danh sách người liên hệ (embedded documents).</summary>
    public List<Contact> contacts { get; set; } = [];

    /// <summary>Trường tùy chỉnh động. Lưu dưới dạng BsonDocument, serialize thành Dictionary.</summary>
    public BsonDocument? customFields { get; set; }

    public bool isDeleted { get; set; } = false;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;

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
