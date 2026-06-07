using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/settings/organization")]
[Produces("application/json")]
public sealed class OrganizationSettingsController : ControllerBase
{
    private readonly IOrganizationSettingsService _settingsService;

    public OrganizationSettingsController(IOrganizationSettingsService settingsService) =>
        _settingsService = settingsService;

    [HttpGet]
    [Authorize(Policy = Policies.AnyRole)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct) =>
        Ok(await _settingsService.GetProfileAsync(ct));

    [HttpPut]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateOrganizationProfileRequest request,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _settingsService.UpdateProfileAsync(request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPost("logo")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(
                new { ErrorCode = "FILE_REQUIRED", ErrorMessage = "Vui lòng chọn file logo." }
            );

        await using var stream = file.OpenReadStream();
        var result = await _settingsService.UploadLogoAsync(
            stream,
            file.FileName,
            file.ContentType,
            ct
        );

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }
}
