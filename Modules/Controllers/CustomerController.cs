using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Policy = Policies.AnyRole)] // Có role là vào được (1, 2, 3). Chi tiết Role 3 giới hạn bằng data scoping.
[Produces("application/json")]
public sealed class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Tạo khách hàng mới.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
{
    var result = await _customerService.CreateAsync(request, ct);
    
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>Lấy thông tin khách hàng.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _customerService.GetByIdAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Cập nhật thông tin cơ bản khách hàng.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Xóa mềm khách hàng (Chỉ Admin/Owner).</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOrAbove)] // Ghi đè policy của class
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _customerService.DeleteAsync(id, ct);
        return result.IsSuccess
            ? NoContent()
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Khôi phục khách hàng bị xóa mềm (Chỉ Owner).</summary>
    [HttpPut("{id}/restore")]
    [Authorize(Policy = Policies.OwnerOnly)] // Chỉ cho phép Owner
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(string id, CancellationToken ct)
    {
        var result = await _customerService.RestoreAsync(id, ct);
        return result.IsSuccess
            ? Ok(new { message = "Khôi phục khách hàng thành công." })
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Danh sách khách hàng (có phân trang & lọc).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] CustomerListFilterRequest request, CancellationToken ct)
    {
        var result = await _customerService.GetListAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Thống kê số lượng khách hàng theo trạng thái (dùng cho stat cards trên trang Customers).</summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _customerService.GetStatsAsync(ct);
        return Ok(result);
    }

    /// <summary>Tìm kiếm toàn văn (Atlas Search).</summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { ErrorCode = "INVALID_QUERY", ErrorMessage = "Từ khóa tìm kiếm không được rỗng." });

        var result = await _customerService.SearchAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Giao diện 360 độ của khách hàng.</summary>
    [HttpGet("{id}/360")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get360View(string id, CancellationToken ct)
    {
        var result = await _customerService.Get360ViewAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Đổi trạng thái khách hàng.</summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateCustomerStatusRequest request, CancellationToken ct)
    {
        var result = await _customerService.UpdateStatusAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(new { message = "Cập nhật trạng thái thành công." })
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Đổi người phụ trách khách hàng (Chỉ Admin/Owner).</summary>
    [HttpPut("{id}/owner")]
    [Authorize(Policy = Policies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOwner(string id, [FromBody] UpdateCustomerOwnerRequest request, CancellationToken ct)
    {
        var result = await _customerService.UpdateOwnerAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(new { message = "Cập nhật người phụ trách thành công." })
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }
}
