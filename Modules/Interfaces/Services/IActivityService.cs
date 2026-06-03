using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IActivityService
{
    Task<ActivityListResponse> GetListAsync(ActivityListFilterRequest request, CancellationToken ct = default);
    Task<ServiceResult<ActivityResponse>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<ActivityResponse>> CreateAsync(CreateActivityRequest request, CancellationToken ct = default);
    Task<ServiceResult<ActivityResponse>> UpdateAsync(string id, UpdateActivityRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default);
}
