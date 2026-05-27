using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;

namespace CRM.Api.Modules.Controllers;

/// <summary>Quản lý phòng ban. Đọc: mọi role. Ghi: chỉ Admin/Owner.</summary>
[ApiController]
[Route("api/departments")]
[Authorize(Policy = Policies.AnyRole)]
[Produces("application/json")]
public sealed class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _deptService;

    public DepartmentController(IDepartmentService deptService)
        => _deptService = deptService;

    /// <summary>Danh sách tất cả phòng ban trong org.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _deptService.GetAllAsync(ct));

    /// <summary>Chi tiết 1 phòng ban.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _deptService.GetByIdAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Tạo phòng ban mới. Chỉ Admin/Owner.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _deptService.CreateAsync(request, ct);

        if (!result.IsSuccess)
            return result.ErrorCode == "NAME_EXISTS"
                ? Conflict(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        if (result.Data is null)
            return StatusCode(500, new { ErrorCode = "INTERNAL_ERROR", ErrorMessage = "Unexpected null result." });

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);

    }

    /// <summary>Cập nhật phòng ban. Chỉ Admin/Owner.</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _deptService.UpdateAsync(id, request, ct);

        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Xóa mềm phòng ban và toàn bộ teams bên trong. Chỉ Admin/Owner.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _deptService.DeleteAsync(id, ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }
}
