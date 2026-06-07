using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization> GetOrCreateCurrentAsync(CancellationToken ct = default);
    Task<Organization?> GetCurrentAsync(CancellationToken ct = default);
    Task UpdatePipelineStagesAsync(List<PipelineStage> stages, CancellationToken ct = default);
    Task UpdateProfileAsync(
        string name,
        string timezone,
        string currency,
        string language,
        CancellationToken ct = default);
    Task UpdateLogoUrlAsync(string? logoUrl, CancellationToken ct = default);
    Task UpdateDepartmentsAsync(List<OrgDepartment> departments, CancellationToken ct = default);
    Task<OrgDepartment?> GetDepartmentByIdAsync(string id, CancellationToken ct = default);
}
