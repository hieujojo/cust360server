using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Constants;

namespace CRM.Api.Modules.Mappers;

/// <summary>User entity (camelCase) → DTO (PascalCase). Che giấu dữ liệu nhạy cảm.</summary>
public static class UserMapper
{
    /// <summary>Map cơ bản — không có tên dept/team. Dùng khi không cần lookup.</summary>
    public static UserResponse ToResponse(this User user)
        => new()
        {
            Id             = user.id,
            OrganizationId = user.organizationId,
            EmployeeCode   = user.employeeCode,
            Email          = user.email,
            DisplayName    = user.displayName,
            Role           = user.role,
            RoleName       = Roles.GetName(user.role),
            DepartmentId   = user.departmentId,
            DepartmentName = null,
            TeamId         = user.teamId,
            TeamName       = null,
            IsTeamLead     = false,
            Phone          = user.phone,
            AvatarUrl      = user.avatarUrl,
            Status         = user.isActive ? "Active" : "Inactive",
            CreatedAt      = user.createdAt,
            UpdatedAt      = user.updatedAt,
            CreatedBy      = user.createdBy
            // password bị bỏ hoàn toàn — không bao giờ trả về client
        };

    /// <summary>Map đầy đủ — kèm tên dept/team và trạng thái team lead.</summary>
    public static UserResponse ToResponse(
        this User user,
        string? departmentName,
        string? teamName,
        bool    isTeamLead)
        => new()
        {
            Id             = user.id,
            OrganizationId = user.organizationId,
            EmployeeCode   = user.employeeCode,
            Email          = user.email,
            DisplayName    = user.displayName,
            Role           = user.role,
            RoleName       = Roles.GetName(user.role),
            DepartmentId   = user.departmentId,
            DepartmentName = departmentName,
            TeamId         = user.teamId,
            TeamName       = teamName,
            IsTeamLead     = isTeamLead,
            Phone          = user.phone,
            AvatarUrl      = user.avatarUrl,
            Status         = user.isActive ? "Active" : "Inactive",
            CreatedAt      = user.createdAt,
            UpdatedAt      = user.updatedAt,
            CreatedBy      = user.createdBy
        };

    public static List<UserResponse> ToResponseList(this IEnumerable<User> users)
        => users.Select(u => u.ToResponse()).ToList();
}
