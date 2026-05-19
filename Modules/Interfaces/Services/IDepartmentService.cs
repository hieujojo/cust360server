using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IDepartmentService
{
    Task<ServiceResult<DepartmentResponse>> CreateAsync(CreateDepartmentRequest request, CancellationToken ct = default);
    Task<ServiceResult<DepartmentResponse>> UpdateAsync(string id, UpdateDepartmentRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<DepartmentResponse>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<DepartmentResponse>> GetAllAsync(CancellationToken ct = default);
}
