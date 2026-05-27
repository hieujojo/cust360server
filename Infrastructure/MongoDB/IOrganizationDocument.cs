namespace CRM.Api.Infrastructure.MongoDB;

/// <summary>Bắt buộc cho mọi MongoDB document. Đảm bảo có organizationId.</summary>
public interface IOrganizationDocument
{
    string Id             { get; set; }
    string OrganizationId { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
