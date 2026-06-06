using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Services;

/// <summary>Xử lý nghiệp vụ quản lý phòng ban.</summary>
public sealed class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _deptRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly CurrentUser _currentUser;

    public DepartmentService(
        IDepartmentRepository deptRepo,
        ITeamRepository teamRepo,
        CurrentUser currentUser
    )
    {
        _deptRepo = deptRepo;
        _teamRepo = teamRepo;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<DepartmentResponse>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken ct = default
    )
    {
        if (await _deptRepo.NameExistsAsync(request.Name, ct: ct))
            return ServiceResult<DepartmentResponse>.Fail(
                "NAME_EXISTS",
                $"Phòng ban '{request.Name}' đã tồn tại."
            );

        var dept = new Department
        {
            organizationId = _currentUser.OrganizationId,
            name = request.Name.Trim(),
            description = request.Description?.Trim(),
            createdBy = _currentUser.UserId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
        };

        await _deptRepo.InsertAsync(dept, ct);
        return ServiceResult<DepartmentResponse>.Ok(dept.ToResponse());
    }

    public async Task<ServiceResult<DepartmentResponse>> UpdateAsync(
        string id,
        UpdateDepartmentRequest request,
        CancellationToken ct = default
    )
    {
        var dept = await _deptRepo.FindByIdAsync(id, ct);
        if (dept is null)
            return ServiceResult<DepartmentResponse>.Fail("NOT_FOUND", "Không tìm thấy phòng ban.");

        if (
            request.Name != null
            && await _deptRepo.NameExistsAsync(request.Name, excludeId: id, ct: ct)
        )
            return ServiceResult<DepartmentResponse>.Fail(
                "NAME_EXISTS",
                $"Phòng ban '{request.Name}' đã tồn tại."
            );

        var update = Builders<Department>.Update.Set(x => x.updatedAt, DateTime.UtcNow);

        if (request.Name != null)
            update = update.Set(x => x.name, request.Name.Trim());
        if (request.Description != null)
            update = update.Set(x => x.description, request.Description.Trim());

        await _deptRepo.UpdateAsync(id, update, ct);

        var updated = await _deptRepo.FindByIdAsync(id, ct);
        var teamCount = (await _teamRepo.FindByDepartmentAsync(id, ct)).Count;
        return ServiceResult<DepartmentResponse>.Ok(updated!.ToResponse(teamCount));
    }

    public async Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        var dept = await _deptRepo.FindByIdAsync(id, ct);
        if (dept is null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy phòng ban.");

        // Xóa mềm tất cả teams thuộc phòng ban này
        await _teamRepo.SoftDeleteByDepartmentAsync(id, ct);
        await _deptRepo.SoftDeleteAsync(id, ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<DepartmentResponse>> GetByIdAsync(
        string id,
        CancellationToken ct = default
    )
    {
        var dept = await _deptRepo.FindByIdAsync(id, ct);
        if (dept is null)
            return ServiceResult<DepartmentResponse>.Fail("NOT_FOUND", "Không tìm thấy phòng ban.");

        var teamCount = (await _teamRepo.FindByDepartmentAsync(id, ct)).Count;
        return ServiceResult<DepartmentResponse>.Ok(dept.ToResponse(teamCount));
    }

    public async Task<List<DepartmentResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var depts = await _deptRepo.FindAllAsync(ct);

        var teams = await _teamRepo.FindAllAsync(ct);

        // Group teams theo departmentId để đếm
        var teamCountByDept = teams
            .GroupBy(t => t.departmentId)
            .ToDictionary(g => g.Key, g => g.Count());

        return depts.Select(d => d.ToResponse(teamCountByDept.GetValueOrDefault(d.id, 0))).ToList();
    }
}
