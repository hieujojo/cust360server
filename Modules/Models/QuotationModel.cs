using CRM.Api.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

[BsonIgnoreExtraElements]
public sealed class Quotation : IOrganizationDocument, ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;
    public string dealId { get; set; } = string.Empty;
    public string customerName { get; set; } = string.Empty;
    public string code { get; set; } = string.Empty; // QUO-YYYY-NNNN
    public decimal totalValue { get; set; }
    public string currency { get; set; } = "VND";
    public string status { get; set; } = "Draft"; // Draft, Sent, Accepted, Rejected
    public string? notes { get; set; }
    public List<QuotationItem> items { get; set; } = new();
    public int version { get; set; } = 1;
    public DateTime? validUntil { get; set; }
    
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

public sealed class QuotationItem
{
    public string description { get; set; } = string.Empty;
    public string? category { get; set; }
    public decimal quantity { get; set; }
    public decimal unitPrice { get; set; }
    public decimal total { get; set; }
}
