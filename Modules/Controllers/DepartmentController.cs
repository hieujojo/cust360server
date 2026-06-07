using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

/// <summary>Quản lý phòng ban embedded trong organization. Chỉ Admin/Owner.</summary>
[ApiController]
[Route("api/settings/departments")]
[Produces("application/json")]
public sealed class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _deptService;

    public DepartmentController(IDepartmentService deptService) => _deptService = deptService;

    /// <summary>Danh sách tất cả phòng ban trong org (đọc: mọi role).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AnyRole)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _deptService.GetAllAsync(ct));

    /// <summary>Chi tiết 1 phòng ban (đọc: mọi role).</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = Policies.AnyRole)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _deptService.GetByIdAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Tạo phòng ban mới.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _deptService.CreateAsync(request, ct);

        if (!result.IsSuccess)
            return result.ErrorCode == "NAME_EXISTS"
                ? Conflict(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        if (result.Data is null)
            return StatusCode(
                500,
                new { ErrorCode = "INTERNAL_ERROR", ErrorMessage = "Unexpected null result." }
            );

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);
    }

    /// <summary>Cập nhật phòng ban.</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _deptService.UpdateAsync(id, request, ct);

        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Xóa phòng ban. Block nếu còn user gán vào.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _deptService.DeleteAsync(id, ct);

        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
                "DEPT_HAS_USERS" => BadRequest(new { result.ErrorCode, result.ErrorMessage }),
                _ => BadRequest(new { result.ErrorCode, result.ErrorMessage }),
            };

        return NoContent();
    }
}
