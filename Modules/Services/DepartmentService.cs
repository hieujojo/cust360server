using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Bson;

namespace CRM.Api.Modules.Services;

/// <summary>Quản lý phòng ban embedded trong organizations.departments.</summary>
public sealed class DepartmentService : IDepartmentService
{
    private readonly IOrganizationRepository _organizationRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly CurrentUser _currentUser;

    public DepartmentService(
        IOrganizationRepository organizationRepo,
        IUserRepository userRepo,
        ITeamRepository teamRepo,
        CurrentUser currentUser
    )
    {
        _organizationRepo = organizationRepo;
        _userRepo = userRepo;
        _teamRepo = teamRepo;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<DepartmentResponse>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken ct = default
    )
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);

        if (
            org.departments.Any(x =>
                x.name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)
            )
        )
            return ServiceResult<DepartmentResponse>.Fail(
                "NAME_EXISTS",
                $"Phòng ban '{request.Name}' đã tồn tại."
            );

        var dept = new OrgDepartment
        {
            id = ObjectId.GenerateNewId().ToString(),
            name = request.Name.Trim(),
            description = request.Description?.Trim(),
            managerId = null,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
        };

        org.departments.Add(dept);
        await _organizationRepo.UpdateDepartmentsAsync(org.departments, ct);

        return ServiceResult<DepartmentResponse>.Ok(await BuildResponseAsync(dept, ct));
    }

    public async Task<ServiceResult<DepartmentResponse>> UpdateAsync(
        string id,
        UpdateDepartmentRequest request,
        CancellationToken ct = default
    )
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        var dept = org.departments.FirstOrDefault(x => x.id == id);
        if (dept is null)
            return ServiceResult<DepartmentResponse>.Fail("NOT_FOUND", "Không tìm thấy phòng ban.");

        if (
            request.Name != null
            && org.departments.Any(x =>
                x.id != id && x.name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)
            )
        )
            return ServiceResult<DepartmentResponse>.Fail(
                "NAME_EXISTS",
                $"Phòng ban '{request.Name}' đã tồn tại."
            );

        if (request.ManagerId != null && !string.IsNullOrWhiteSpace(request.ManagerId))
        {
            var managerValidation = await ValidateManagerAsync(request.ManagerId, id, ct);
            if (!managerValidation.IsSuccess)
                return managerValidation.ToTyped<DepartmentResponse>();
        }

        if (request.Name != null)
            dept.name = request.Name.Trim();
        if (request.Description != null)
            dept.description = request.Description.Trim();
        if (request.ManagerId != null)
            dept.managerId = string.IsNullOrWhiteSpace(request.ManagerId)
                ? null
                : request.ManagerId;

        dept.updatedAt = DateTime.UtcNow;
        await _organizationRepo.UpdateDepartmentsAsync(org.departments, ct);

        return ServiceResult<DepartmentResponse>.Ok(await BuildResponseAsync(dept, ct));
    }

    public async Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        var dept = org.departments.FirstOrDefault(x => x.id == id);
        if (dept is null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy phòng ban.");

        var userCount = await _userRepo.CountByDepartmentAsync(id, ct);
        if (userCount > 0)
            return ServiceResult.Fail(
                "DEPT_HAS_USERS",
                $"Không thể xóa phòng ban vì còn {userCount} nhân viên đang được gán."
            );

        org.departments.RemoveAll(x => x.id == id);
        await _organizationRepo.UpdateDepartmentsAsync(org.departments, ct);

        await _teamRepo.SoftDeleteByDepartmentAsync(id, ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<DepartmentResponse>> GetByIdAsync(
        string id,
        CancellationToken ct = default
    )
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        var dept = org.departments.FirstOrDefault(x => x.id == id);
        if (dept is null)
            return ServiceResult<DepartmentResponse>.Fail("NOT_FOUND", "Không tìm thấy phòng ban.");

        return ServiceResult<DepartmentResponse>.Ok(await BuildResponseAsync(dept, ct));
    }

    public async Task<List<DepartmentResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        var teams = await _teamRepo.FindAllAsync(ct);
        var teamCountByDept = teams
            .GroupBy(t => t.departmentId)
            .ToDictionary(g => g.Key, g => g.Count());

        var results = new List<DepartmentResponse>();
        foreach (var dept in org.departments.OrderBy(x => x.name))
        {
            results.Add(
                await BuildResponseAsync(dept, ct, teamCountByDept.GetValueOrDefault(dept.id, 0))
            );
        }

        return results;
    }

    private async Task<DepartmentResponse> BuildResponseAsync(
        OrgDepartment dept,
        CancellationToken ct,
        int? teamCountOverride = null
    )
    {
        var teamCount =
            teamCountOverride ?? (await _teamRepo.FindByDepartmentAsync(dept.id, ct)).Count;
        var userCount = (int)await _userRepo.CountByDepartmentAsync(dept.id, ct);
        string? managerName = null;

        if (!string.IsNullOrWhiteSpace(dept.managerId))
        {
            var manager = await _userRepo.FindByIdAsync(dept.managerId, ct);
            managerName = manager?.displayName;
        }

        return dept.ToResponse(teamCount, userCount, managerName);
    }

    private async Task<ServiceResult> ValidateManagerAsync(string managerId, string departmentId, CancellationToken ct)
    {
        if (!ObjectId.TryParse(managerId, out _))
            return ServiceResult.Fail("MANAGER_INVALID", "ID quản lý không hợp lệ.");

        var manager = await _userRepo.FindByIdAsync(managerId, ct);
        if (manager is null)
            return ServiceResult.Fail("MANAGER_NOT_FOUND", "Không tìm thấy quản lý.");

        if (manager.departmentId != departmentId)
            return ServiceResult.Fail("MANAGER_DEPT_MISMATCH", "Người được chọn làm quản lý bắt buộc phải là nhân sự của phòng ban này.");

        return ServiceResult.Ok();
    }
}
