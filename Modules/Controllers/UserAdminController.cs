using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

/// <summary>Quản lý user. Chỉ Admin và Owner.</summary>
[ApiController]
[Route("api/settings/users")]
[Authorize(Policy = Policies.AdminOrAbove)]
[Produces("application/json")]
public sealed class UserAdminController : ControllerBase
{
    private readonly IUserService _userService;

    public UserAdminController(IUserService userService) => _userService = userService;

    /// <summary>Danh sách users có filter và phân trang.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken ct
    ) => Ok(await _userService.GetPagedAsync(request, ct));

    /// <summary>Danh sách tất cả users (không phân trang).</summary>
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var result = await _userService.GetAllAsync(ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Chi tiết 1 user.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Tạo tài khoản nhân viên mới. Gửi email thông báo.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct
        );

        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "EMAIL_EXISTS" or "EMPLOYEE_CODE_CONFLICT" => Conflict(
                    new { result.ErrorCode, result.ErrorMessage }
                ),
                _ => BadRequest(new { result.ErrorCode, result.ErrorMessage }),
            };

        if (result.Data is null)
            return StatusCode(
                500,
                new { ErrorCode = "INTERNAL_ERROR", ErrorMessage = "Unexpected null result." }
            );

        return CreatedAtAction(nameof(GetUser), new { id = result.Data.Id }, result.Data);
    }

    /// <summary>Cập nhật thông tin user.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.UpdateUserAsync(
            id,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct
        );

        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Activate hoặc Deactivate tài khoản.</summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(
        string id,
        [FromBody] ToggleUserStatusRequest request,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.ToggleUserStatusAsync(
            id,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct
        );

        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(
            new
            {
                message = $"Tài khoản đã được {(request.IsActive ? "kích hoạt" : "vô hiệu hóa")}.",
            }
        );
    }

    /// <summary>Reset password cho nhân viên. Gửi email mật khẩu mới.</summary>
    [HttpPut("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        string id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.ResetPasswordAsync(
            id,
            request.NewPassword,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct
        );

        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(new { message = "Mật khẩu đã được reset. Email thông báo đã gửi." });
    }
}
