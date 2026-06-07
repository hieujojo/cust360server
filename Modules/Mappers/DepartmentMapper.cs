using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class DepartmentMapper
{
    public static DepartmentResponse ToResponse(
        this OrgDepartment dept,
        int teamCount = 0,
        int userCount = 0,
        string? managerName = null)
        => new()
        {
            Id = dept.id,
            Name = dept.name,
            Description = dept.description,
            ManagerId = dept.managerId,
            ManagerName = managerName,
            TeamCount = teamCount,
            UserCount = userCount,
            CreatedAt = dept.createdAt,
            UpdatedAt = dept.updatedAt,
        };

    public static DepartmentResponse ToResponse(this Department dept, int teamCount = 0)
        => new()
        {
            Id = dept.id,
            Name = dept.name,
            Description = dept.description,
            TeamCount = teamCount,
            CreatedAt = dept.createdAt,
            UpdatedAt = dept.updatedAt,
        };

    public static DepartmentSummary ToSummary(this OrgDepartment dept)
        => new() { Id = dept.id, Name = dept.name };

    public static List<DepartmentResponse> ToResponseList(this IEnumerable<OrgDepartment> depts, int teamCount = 0)
        => depts.Select(d => d.ToResponse(teamCount)).ToList();
}
