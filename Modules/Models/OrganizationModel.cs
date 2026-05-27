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

public sealed class PipelineStage
{
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string name { get; set; } = string.Empty;
    public int order { get; set; }
    public string color { get; set; } = "#2563eb";
    public int stuckThreshold { get; set; } = 7;
}

