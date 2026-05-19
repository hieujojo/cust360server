using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;

namespace CRM.Api.Modules.Controllers;

/// <summary>Quản lý team trong phòng ban. Đọc: mọi role. Ghi: chỉ Admin/Owner.</summary>
[ApiController]
[Route("api/departments/{departmentId}/teams")]
[Authorize(Policy = Policies.AnyRole)]
[Produces("application/json")]
public sealed class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
        => _teamService = teamService;

    /// <summary>Danh sách teams trong một phòng ban.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDepartment(string departmentId, CancellationToken ct)
        => Ok(await _teamService.GetByDepartmentAsync(departmentId, ct));

    /// <summary>Chi tiết 1 team.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string departmentId, string id, CancellationToken ct)
    {
        var result = await _teamService.GetByIdAsync(departmentId, id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Tạo team mới trong phòng ban. Chỉ Admin/Owner.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        string departmentId, [FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _teamService.CreateAsync(departmentId, request, ct);

        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "NAME_EXISTS"    => Conflict(new { result.ErrorCode, result.ErrorMessage }),
                "DEPT_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
                _                => BadRequest(new { result.ErrorCode, result.ErrorMessage })
            };

        return CreatedAtAction(nameof(GetById), new { departmentId, id = result.Data!.Id }, result.Data);
    }

    /// <summary>Cập nhật team. Chỉ Admin/Owner.</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string departmentId, string id, [FromBody] UpdateTeamRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _teamService.UpdateAsync(departmentId, id, request, ct);

        if (!result.IsSuccess)
            return result.ErrorCode is "NOT_FOUND" or "DEPT_NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Xóa mềm team. Chỉ Admin/Owner.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string departmentId, string id, CancellationToken ct)
    {
        var result = await _teamService.DeleteAsync(departmentId, id, ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }
}
