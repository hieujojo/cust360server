using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IOrganizationSettingsService
{
    Task<OrganizationProfileResponse> GetProfileAsync(CancellationToken ct = default);
    Task<ServiceResult<OrganizationProfileResponse>> UpdateProfileAsync(
        UpdateOrganizationProfileRequest request,
        CancellationToken ct = default);
    Task<ServiceResult<OrganizationProfileResponse>> UploadLogoAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);
}
