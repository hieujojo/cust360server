using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class TeamMapper
{
    public static TeamResponse ToResponse(
        this Team team,
        string departmentName,
        string? leadName    = null,
        int     memberCount = 0)
        => new()
        {
            Id             = team.id,
            DepartmentId   = team.departmentId,
            DepartmentName = departmentName,
            Name           = team.name,
            Description    = team.description,
            LeadId         = team.leadId,
            LeadName       = leadName,
            MemberCount    = memberCount,
            CreatedAt      = team.createdAt,
            UpdatedAt      = team.updatedAt,
        };

    public static TeamSummary ToSummary(this Team team)
        => new() { Id = team.id, Name = team.name, DepartmentId = team.departmentId };
}
