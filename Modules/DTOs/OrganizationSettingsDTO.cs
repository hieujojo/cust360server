using System.ComponentModel.DataAnnotations;

namespace CRM.Api.Modules.DTOs;

public sealed class OrganizationProfileResponse
{
    public string Id { get; init; } = string.Empty;
    public string OrganizationId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
    public string Currency { get; init; } = "VND";
    public string Language { get; init; } = "vi";
}

/// <summary>PUT /api/settings/organization</summary>
public sealed class UpdateOrganizationProfileRequest
{
    [Required(ErrorMessage = "Tên công ty là bắt buộc.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [MaxLength(10)]
    public string Language { get; set; } = "vi";
}
