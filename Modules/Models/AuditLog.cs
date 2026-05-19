using MongoDB.Bson.Serialization.Attributes;
using CRM.Api.Infrastructure.MongoDB;

namespace CRM.Api.Modules.Models;

/// <summary>Lịch sử thao tác. Collection: audit_logs. Append-only. TTL: 1 năm.</summary>
public sealed class AuditLog : IOrganizationDocument
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string id { get; set; } = string.Empty;

    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string organizationId { get; set; } = string.Empty;

    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string? actorId { get; set; }

    public string actorEmail { get; set; } = string.Empty;
    public string action { get; set; } = string.Empty;

    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string? targetUserId { get; set; }

    public string? targetUserEmail { get; set; }
    public string? ipAddress { get; set; }
    public string? userAgent { get; set; }
    public Dictionary<string, string>? metadata { get; set; }

    /// <summary>TTL field — MongoDB tự xóa sau 1 năm.</summary>
    public DateTime createdAt { get; set; } = DateTime.UtcNow;

    // Bridge camelCase fields → IOrganizationDocument interface
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

/// <summary>Tên các action ghi vào AuditLog.</summary>
public static class AuditActions
{
    public const string UserCreated         = "UserCreated";
    public const string UserUpdated         = "UserUpdated";
    public const string UserActivated       = "UserActivated";
    public const string UserDeactivated     = "UserDeactivated";
    public const string UserLoggedIn        = "UserLoggedIn";
    public const string UserPasswordChanged = "UserPasswordChanged";
    public const string UserPasswordReset   = "UserPasswordReset";
    public const string UserForgotPassword  = "UserForgotPassword";
}
