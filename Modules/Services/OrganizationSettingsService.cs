using CRM.Api.Infrastructure.Storage;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Shared.Exceptions;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

public sealed class OrganizationSettingsService : IOrganizationSettingsService
{
    private static readonly HashSet<string> AllowedLogoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/svg+xml",
    };

    private readonly IOrganizationRepository _organizationRepo;
    private readonly ICloudinaryStorageService _storageService;

    public OrganizationSettingsService(
        IOrganizationRepository organizationRepo,
        ICloudinaryStorageService storageService
    )
    {
        _organizationRepo = organizationRepo;
        _storageService = storageService;
    }

    public async Task<OrganizationProfileResponse> GetProfileAsync(CancellationToken ct = default)
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        return org.ToProfileResponse();
    }

    public async Task<ServiceResult<OrganizationProfileResponse>> UpdateProfileAsync(
        UpdateOrganizationProfileRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("name", "Tên công ty là bắt buộc.");

        await _organizationRepo.UpdateProfileAsync(
            request.Name,
            request.Timezone,
            request.Currency,
            request.Language,
            ct
        );

        return ServiceResult<OrganizationProfileResponse>.Ok(await GetProfileAsync(ct));
    }

    public async Task<ServiceResult<OrganizationProfileResponse>> UploadLogoAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default
    )
    {
        if (!AllowedLogoTypes.Contains(contentType))
            return ServiceResult<OrganizationProfileResponse>.Fail(
                "INVALID_FILE_TYPE",
                "Chỉ chấp nhận file ảnh PNG, JPG, WEBP hoặc SVG."
            );

        if (content.Length > 2 * 1024 * 1024)
            return ServiceResult<OrganizationProfileResponse>.Fail(
                "FILE_TOO_LARGE",
                "Logo tối đa 2MB."
            );

        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        var logoUrl = await _storageService.UploadAsync(
            content,
            fileName,
            contentType,
            "crm360/logos",
            ct
        );

        await _storageService.DeleteByUrlAsync(org.logoUrl, ct);
        await _organizationRepo.UpdateLogoUrlAsync(logoUrl, ct);

        return ServiceResult<OrganizationProfileResponse>.Ok(await GetProfileAsync(ct));
    }
}
