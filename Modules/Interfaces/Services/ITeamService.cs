using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

public interface ITeamService
{
    Task<ServiceResult<TeamResponse>> CreateAsync(string departmentId, CreateTeamRequest request, CancellationToken ct = default);
    Task<ServiceResult<TeamResponse>> UpdateAsync(string departmentId, string id, UpdateTeamRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(string departmentId, string id, CancellationToken ct = default);
    Task<ServiceResult<TeamResponse>> GetByIdAsync(string departmentId, string id, CancellationToken ct = default);
    Task<List<TeamResponse>> GetByDepartmentAsync(string departmentId, CancellationToken ct = default);
}
