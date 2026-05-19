using System.ComponentModel.DataAnnotations;

namespace CRM.Api.Modules.DTOs;

// ============================================================================
// REQUESTS - AUTH
// ============================================================================

/// <summary>POST /api/auth/login</summary>
public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// ============================================================================
// REQUESTS - USER ADMIN
// ============================================================================

/// <summary>POST /api/admin/users</summary>
public sealed class CreateUserRequest
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chức vụ là bắt buộc.")]
    [MaxLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>1 = Owner | 2 = Admin | 3 = User</summary>
    [Required(ErrorMessage = "Role là bắt buộc.")]
    [Range(1, 3, ErrorMessage = "Role phải là 1, 2 hoặc 3.")]
    public int Role { get; set; }

    /// <summary>Bắt buộc với Role = 3.</summary>
    public string? DepartmentId { get; set; }

    /// <summary>Team thuộc phòng ban. Tùy chọn với mọi role.</summary>
    public string? TeamId { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }
}

/// <summary>PUT /api/admin/users/{id}</summary>
public sealed class UpdateUserRequest
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [Range(1, 3, ErrorMessage = "Role phải là 1, 2 hoặc 3.")]
    public int? Role { get; set; }

    public string? DepartmentId { get; set; }

    /// <summary>Team thuộc phòng ban. Tùy chọn với mọi role.</summary>
    public string? TeamId { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }
}

/// <summary>PUT /api/admin/users/{id}/status</summary>
public sealed class ToggleUserStatusRequest
{
    [Required]
    public bool IsActive { get; set; }

    /// <summary>Lý do — lưu vào AuditLog.</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>PUT /api/admin/users/{id}/reset-password</summary>
public sealed class ResetPasswordRequest
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>GET /api/admin/users</summary>
public sealed class GetUsersRequest
{
    public int?    Role         { get; set; }
    public string? DepartmentId { get; set; }
    public bool?   IsActive     { get; set; }

    /// <summary>Lọc theo team.</summary>
    public string? TeamId       { get; set; }

    /// <summary>Tìm theo tên hoặc email.</summary>
    public string? Search   { get; set; }
    public int     Page     { get; set; } = 1;
    public int     PageSize { get; set; } = 20;
}

// ============================================================================
// REQUESTS - AUTH (FORGOT / RESET PASSWORD)
// ============================================================================

/// <summary>POST /api/auth/forgot-password</summary>
public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>POST /api/auth/reset-password</summary>
public sealed class ResetPasswordByTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// ============================================================================
// REQUESTS - USER PROFILE
// ============================================================================

/// <summary>PUT /api/users/me/password</summary>
public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// ============================================================================
// REQUESTS - AUDIT LOG
// ============================================================================

/// <summary>GET /api/admin/audit-logs</summary>
public sealed class GetAuditLogsRequest
{
    public string?   Action       { get; set; }
    public string?   ActorId      { get; set; }
    public string?   TargetUserId { get; set; }
    public DateTime? FromDate     { get; set; }
    public DateTime? ToDate       { get; set; }
    public int       Page         { get; set; } = 1;
    public int       PageSize     { get; set; } = 50;
}

// ============================================================================
// RESPONSES - USER
// ============================================================================

/// <summary>Thông tin user trả về client. Không expose Password.</summary>
public sealed class UserResponse
{
    public string  Id             { get; init; } = string.Empty;
    public string  OrganizationId { get; init; } = string.Empty;
    public string  EmployeeCode   { get; init; } = string.Empty;
    public string  Email          { get; init; } = string.Empty;
    public string  DisplayName    { get; init; } = string.Empty;
    public string  JobTitle       { get; init; } = string.Empty;

    /// <summary>Số role: 1 / 2 / 3</summary>
    public int     Role           { get; init; }

    /// <summary>Tên role: Owner / Admin / User</summary>
    public string  RoleName       { get; init; } = string.Empty;

    public string? DepartmentId   { get; init; }

    /// <summary>Tên phòng ban — lookup từ departments collection.</summary>
    public string? DepartmentName { get; init; }

    public string? TeamId         { get; init; }

    /// <summary>Tên team — lookup từ teams collection.</summary>
    public string? TeamName       { get; init; }

    /// <summary>True nếu user là lead của team mình.</summary>
    public bool    IsTeamLead     { get; init; }

    public string? Phone          { get; init; }
    public string? AvatarUrl      { get; init; }

    /// <summary>"Active" hoặc "Inactive" — thân thiện hơn boolean.</summary>
    public string  Status         { get; init; } = string.Empty;

    public DateTime CreatedAt     { get; init; }
    public DateTime UpdatedAt     { get; init; }
    public string?  CreatedBy     { get; init; }
}

// ============================================================================
// RESPONSES - AUTH
// ============================================================================

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User  { get; set; } = null!;
}

// ============================================================================
// RESPONSES - AUDIT LOG
// ============================================================================

/// <summary>Thông tin audit log trả về client.</summary>
public sealed class AuditLogResponse
{
    public string  Id              { get; init; } = string.Empty;
    public string? ActorId         { get; init; }
    public string  ActorEmail      { get; init; } = string.Empty;
    public string  Action          { get; init; } = string.Empty;
    public string? TargetUserId    { get; init; }
    public string? TargetUserEmail { get; init; }
    public string? IpAddress       { get; init; }
    public string? UserAgent       { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public DateTime CreatedAt      { get; init; }
}
