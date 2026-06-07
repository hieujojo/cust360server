using System.ComponentModel.DataAnnotations;

namespace CRM.Api.Modules.DTOs;

// ============================================================================
// REQUESTS
// ============================================================================

/// <summary>POST /api/settings/departments/{departmentId}/teams</summary>
public sealed class CreateTeamRequest
{
    [Required(ErrorMessage = "Tên team là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Tên team tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>PUT /api/settings/departments/{departmentId}/teams/{id}</summary>
public sealed class UpdateTeamRequest
{
    [MaxLength(100, ErrorMessage = "Tên team tối đa 100 ký tự.")]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Truyền null để xóa lead, truyền userId để gán lead mới.</summary>
    public string? LeadId { get; set; }

    /// <summary>Dùng để phân biệt "không truyền LeadId" vs "muốn xóa lead".</summary>
    public bool ClearLead { get; set; } = false;
}

// ============================================================================
// RESPONSES
// ============================================================================

public sealed class TeamResponse
{
    public string  Id             { get; init; } = string.Empty;
    public string  DepartmentId   { get; init; } = string.Empty;
    public string  DepartmentName { get; init; } = string.Empty;
    public string  Name           { get; init; } = string.Empty;
    public string? Description    { get; init; }

    /// <summary>UserId của team lead.</summary>
    public string? LeadId          { get; init; }

    /// <summary>Tên hiển thị của team lead.</summary>
    public string? LeadName        { get; init; }

    /// <summary>Số lượng thành viên trong team.</summary>
    public int     MemberCount     { get; init; }

    public DateTime CreatedAt      { get; init; }
    public DateTime UpdatedAt      { get; init; }
}

/// <summary>Response gọn — dùng trong dropdown / lookup.</summary>
public sealed class TeamSummary
{
    public string Id           { get; init; } = string.Empty;
    public string Name         { get; init; } = string.Empty;
    public string DepartmentId { get; init; } = string.Empty;
}
