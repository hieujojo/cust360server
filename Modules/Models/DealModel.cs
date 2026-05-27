using CRM.Api.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

[BsonIgnoreExtraElements]
public sealed class Deal : IOrganizationDocument, ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string customerId { get; set; } = string.Empty;
    public decimal value { get; set; }
    public string currency { get; set; } = "VND";
    public DateTime? expectedCloseDate { get; set; }
    public string ownerId { get; set; } = string.Empty;
    public string stage { get; set; } = string.Empty;
    public int probability { get; set; }
    public string? notes { get; set; }
    public List<DealStageHistoryItem> stageHistory { get; set; } = [];
    public List<string> contacts { get; set; } = [];
    public List<string> quotations { get; set; } = [];
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;
    public bool isDeleted { get; set; } = false;

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

public sealed class DealStageHistoryItem
{
    public string stage { get; set; } = string.Empty;
    public DateTime changedAt { get; set; } = DateTime.UtcNow;
    public string changedBy { get; set; } = string.Empty;
}

