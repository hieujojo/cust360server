using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IPipelineStageService
{
    Task<List<PipelineStageResponse>> GetAsync(CancellationToken ct = default);
    Task<ServiceResult<List<PipelineStageResponse>>> CreateAsync(UpsertPipelineStageRequest request, CancellationToken ct = default);
    Task<ServiceResult<List<PipelineStageResponse>>> UpdateAsync(string id, UpsertPipelineStageRequest request, CancellationToken ct = default);
    Task<ServiceResult<List<PipelineStageResponse>>> DeleteAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<List<PipelineStageResponse>>> ReorderAsync(ReorderPipelineStagesRequest request, CancellationToken ct = default);
}

