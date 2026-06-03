using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/auth/google")]
[Produces("application/json")]
public sealed class GoogleAuthController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuth;

    public GoogleAuthController(IGoogleAuthService googleAuth) => _googleAuth = googleAuth;

    [HttpGet("connect-url")]
    [Authorize(Policy = Policies.AnyRole)]
    public IActionResult GetConnectUrl() => Ok(new { url = _googleAuth.GetAuthorizationUrl() });

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct
    )
    {
        await _googleAuth.HandleCallbackAsync(code, state, ct);
        var frontend = Request.Headers["Origin"].FirstOrDefault() ?? "http://localhost:5192";
        return Redirect($"{frontend}/settings/google?connected=1");
    }

    [HttpGet("status")]
    [Authorize(Policy = Policies.AnyRole)]
    public async Task<IActionResult> Status(CancellationToken ct) =>
        Ok(await _googleAuth.GetStatusAsync(ct));

    [HttpPost("disconnect")]
    [Authorize(Policy = Policies.AnyRole)]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await _googleAuth.DisconnectAsync(ct);
        return NoContent();
    }
}
