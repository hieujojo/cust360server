using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization> GetOrCreateCurrentAsync(CancellationToken ct = default);
    Task<Organization?> GetCurrentAsync(CancellationToken ct = default);
    Task UpdatePipelineStagesAsync(List<PipelineStage> stages, CancellationToken ct = default);
}

