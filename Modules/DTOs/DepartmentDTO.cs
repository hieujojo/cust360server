using System.ComponentModel.DataAnnotations;

namespace CRM.Api.Modules.DTOs;

// ============================================================================
// REQUESTS
// ============================================================================

/// <summary>POST /api/settings/departments</summary>
public sealed class CreateDepartmentRequest
{
    [Required(ErrorMessage = "Tên phòng ban là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Tên phòng ban tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>PUT /api/settings/departments/{id}</summary>
public sealed class UpdateDepartmentRequest
{
    [MaxLength(100, ErrorMessage = "Tên phòng ban tối đa 100 ký tự.")]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? ManagerId { get; set; }
}

// ============================================================================
// RESPONSES
// ============================================================================

public sealed class DepartmentResponse
{
    public string  Id          { get; init; } = string.Empty;
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ManagerId   { get; init; }
    public string? ManagerName { get; init; }

    /// <summary>Số lượng teams thuộc phòng ban này.</summary>
    public int     TeamCount   { get; init; }

    /// <summary>Số lượng users được gán vào phòng ban.</summary>
    public int     UserCount   { get; init; }

    public DateTime CreatedAt  { get; init; }
    public DateTime UpdatedAt  { get; init; }
}

/// <summary>Response gọn — dùng trong dropdown / lookup.</summary>
public sealed class DepartmentSummary
{
    public string Id   { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
