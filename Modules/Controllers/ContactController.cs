using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/customers/{customerId}/contacts")]
[Authorize(Policy = Policies.AnyRole)]
[Produces("application/json")]
public sealed class ContactController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public ContactController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Thêm người liên hệ mới.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddContact(string customerId, [FromBody] CreateContactRequest request, CancellationToken ct)
    {
        var result = await _customerService.AddContactAsync(customerId, request, ct);
        return result.IsSuccess
            ? CreatedAtAction("GetById", "Customer", new { id = customerId }, result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Cập nhật người liên hệ.</summary>
    [HttpPut("{contactId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContact(string customerId, string contactId, [FromBody] CreateContactRequest request, CancellationToken ct)
    {
        var result = await _customerService.UpdateContactAsync(customerId, contactId, request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Xóa người liên hệ.</summary>
    [HttpDelete("{contactId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveContact(string customerId, string contactId, CancellationToken ct)
    {
        var result = await _customerService.RemoveContactAsync(customerId, contactId, ct);
        return result.IsSuccess
            ? NoContent()
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>Đặt người liên hệ chính.</summary>
    [HttpPut("{contactId}/primary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryContact(string customerId, string contactId, CancellationToken ct)
    {
        var result = await _customerService.SetPrimaryContactAsync(customerId, contactId, ct);
        return result.IsSuccess
            ? Ok(new { message = "Đã đặt người liên hệ chính." })
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }
}
