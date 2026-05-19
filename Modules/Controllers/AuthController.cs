using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;

namespace CRM.Api.Modules.Controllers;

/// <summary>Xác thực: login, logout, lấy JWT token.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
        => _authService = authService;

    /// <summary>Đăng nhập bằng email + password. Trả về JWT access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct);

        if (!result.IsSuccess)
            return result.ErrorCode == "ACCOUNT_INACTIVE"
                ? Unauthorized(new { result.ErrorCode, result.ErrorMessage })
                : Unauthorized(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Đăng xuất. Phase 1: stateless JWT, client tự xóa token.</summary>
    [HttpPost("logout")]
    [Authorize(Policy = Policies.AnyRole)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // Phase 1: stateless JWT — không cần làm gì ở server
        // Client tự xóa token khỏi localStorage/sessionStorage
        // Phase 2: blacklist token bằng Redis nếu cần revoke ngay
        return Ok(new { message = "Đăng xuất thành công." });
    }

    /// <summary>Gửi email link đặt lại mật khẩu. Luôn trả 200 để tránh email enumeration.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var clientBaseUrl = $"{Request.Scheme}://{Request.Host}";
        await _authService.ForgotPasswordAsync(request, clientBaseUrl, ct);

        // Luôn trả 200 — không tiết lộ email có tồn tại hay không
        return Ok(new { message = "Nếu email tồn tại, bạn sẽ nhận được link đặt lại mật khẩu trong vài phút." });
    }

    /// <summary>Đặt lại mật khẩu bằng token từ email.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordByTokenRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.ResetPasswordByTokenAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }
}
