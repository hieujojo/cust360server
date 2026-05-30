using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IDealService
{
    Task<List<DealResponse>> GetListAsync(DealListFilterRequest request, CancellationToken ct = default);
    Task<DealStatsResponse> GetStatsAsync(CancellationToken ct = default);
    Task<ServiceResult<DealResponse>> CreateAsync(CreateDealRequest request, CancellationToken ct = default);
    Task<ServiceResult<DealResponse>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<DealResponse>> UpdateAsync(string id, UpdateDealRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<DealResponse>> ChangeStageAsync(string id, ChangeDealStageRequest request, CancellationToken ct = default);
}

