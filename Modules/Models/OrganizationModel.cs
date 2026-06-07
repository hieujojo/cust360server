using CRM.Api.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

[BsonIgnoreExtraElements]
public sealed class Organization : IOrganizationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;
    public string? name { get; set; }
    public string? logoUrl { get; set; }
    public string timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public string currency { get; set; } = "VND";
    public string language { get; set; } = "vi";
    public List<OrgDepartment> departments { get; set; } = [];
    public List<PipelineStage> pipelineStages { get; set; } = [];

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

public sealed class OrgDepartment
{
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string name { get; set; } = string.Empty;
    public string? description { get; set; }
    public string? managerId { get; set; }
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PipelineStage
{
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string name { get; set; } = string.Empty;
    public int order { get; set; }
    public string color { get; set; } = "#2563eb";
    public int defaultProbability { get; set; } = 0;
    public int stuckThreshold { get; set; } = 7;
}

