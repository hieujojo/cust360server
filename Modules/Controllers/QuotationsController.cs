using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/deals/{dealId}/quotations")]
[Authorize(Policy = Policies.AnyRole)]
public sealed class QuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;

    public QuotationsController(IQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(string dealId, CancellationToken ct)
    {
        var result = await _quotationService.GetListByDealIdAsync(dealId, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string dealId, string id, CancellationToken ct)
    {
        var result = await _quotationService.GetByIdAsync(id, ct);
        return result.IsSuccess 
            ? Ok(result.Data) 
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(string dealId, [FromBody] CreateQuotationRequest request, CancellationToken ct)
    {
        var result = await _quotationService.CreateAsync(dealId, request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { dealId, id = result.Data!.Id }, result.Data)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string dealId, string id, [FromBody] UpdateQuotationRequest request, CancellationToken ct)
    {
        var result = await _quotationService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string dealId, string id, CancellationToken ct)
    {
        var result = await _quotationService.DeleteAsync(id, ct);
        return result.IsSuccess
            ? NoContent()
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }
}
