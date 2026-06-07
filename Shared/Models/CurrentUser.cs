namespace CRM.Api.Shared.Models;

/// <summary>Thông tin user hiện tại từ JWT. Scoped per request.</summary>
public sealed class CurrentUser
{
    public string UserId { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public int Role { get; set; }
    public string? DepartmentId { get; set; }
    public string? TeamId { get; set; }
    public string Email { get; set; } = string.Empty;

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    public bool IsAdminOrAbove => Role <= 2;
    public bool IsOwner => Role == 1;
}
