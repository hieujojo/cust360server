using CRM.Api.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

/// <summary>Activity log entry. Collection: activities.</summary>
[BsonIgnoreExtraElements]
public sealed class Activity : IOrganizationDocument, ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;
    public string customerId { get; set; } = string.Empty;
    [BsonIgnoreIfNull]
    public string? dealId { get; set; }
    public string departmentId { get; set; } = string.Empty;

    /// <summary>call | email | meeting | note | system</summary>
    public string type { get; set; } = string.Empty;

    /// <summary>manual | system | gmail | calendar</summary>
    public string source { get; set; } = "manual";

    public bool isAutoSync { get; set; }
    /// <summary>Chỉ set khi sync Google — không ghi null (tránh unique index).</summary>
    [BsonIgnoreIfNull]
    public string? externalId { get; set; }

    public string createdBy { get; set; } = string.Empty;
    public DateTime occurredAt { get; set; } = DateTime.UtcNow;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;
    public bool isDeleted { get; set; }

    // call
    public string? outcome { get; set; }
    public int? durationMinutes { get; set; }
    public string? note { get; set; }

    // email
    public string? subject { get; set; }
    public string? summary { get; set; }
    /// <summary>inbound | outbound</summary>
    public string? direction { get; set; }

    // meeting
    public string? location { get; set; }
    public List<string> attendees { get; set; } = [];
    public string? nextSteps { get; set; }

    // note
    public string? body { get; set; }

    // system
    public string? systemEvent { get; set; }
    public Dictionary<string, string>? metadata { get; set; }

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

public static class ActivityTypes
{
    public const string Call = "call";
    public const string Email = "email";
    public const string Meeting = "meeting";
    public const string Note = "note";
    public const string System = "system";
}

public static class ActivitySources
{
    public const string Manual = "manual";
    public const string System = "system";
    public const string Gmail = "gmail";
    public const string Calendar = "calendar";
}
