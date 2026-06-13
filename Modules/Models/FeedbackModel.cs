using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CRM.Api.Infrastructure.MongoDB;

namespace CRM.Api.Modules.Models;

/// <summary>Góp ý & Phản hồi. Collection: feedbacks. Hỗ trợ multi-tenancy qua organizationId.</summary>
[BsonIgnoreExtraElements]
public sealed class Feedback : IOrganizationDocument, ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;

    /// <summary>customer | internal.</summary>
    public string type { get; set; } = "internal";

    /// <summary>feature_request | improvement | complaint | praise | other.</summary>
    public string category { get; set; } = "other";

    public string title { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;

    /// <summary>open | in_progress | resolved | closed.</summary>
    public string status { get; set; } = "open";

    /// <summary>Có gửi ẩn danh hay không.</summary>
    public bool isAnonymous { get; set; } = false;

    /// <summary>User ID người tạo feedback.</summary>
    public string authorId { get; set; } = string.Empty;
    public string authorName { get; set; } = string.Empty;
    public string? authorEmail { get; set; }

    /// <summary>Customer ID nếu type = customer.</summary>
    public string? customerId { get; set; }
    public string? customerName { get; set; }

    /// <summary>Danh sách replies (embedded documents).</summary>
    public List<FeedbackReply> replies { get; set; } = [];

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

/// <summary>Reply cho feedback (embedded document).</summary>
[BsonIgnoreExtraElements]
public sealed class FeedbackReply
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string feedbackId { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;

    public string authorId { get; set; } = string.Empty;
    public string authorName { get; set; } = string.Empty;
    public bool isAnonymous { get; set; } = false;

    public DateTime createdAt { get; set; } = DateTime.UtcNow;
}
