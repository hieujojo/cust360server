using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/deals")]
[Authorize(Policy = Policies.AnyRole)]
[Produces("application/json")]
public sealed class DealsController : ControllerBase
{
    private readonly IDealService _dealService;

    public DealsController(IDealService dealService) => _dealService = dealService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] DealListFilterRequest request, CancellationToken ct)
        => Ok(await _dealService.GetListAsync(request, ct));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDealRequest request, CancellationToken ct)
    {
        var result = await _dealService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return CreatedAtAction(nameof(Detail), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
    {
        var result = await _dealService.GetByIdAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDealRequest request, CancellationToken ct)
    {
        var result = await _dealService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _dealService.DeleteAsync(id, ct);
        return result.IsSuccess
            ? NoContent()
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPatch("{id}/stage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStage(string id, [FromBody] ChangeDealStageRequest request, CancellationToken ct)
    {
        var result = await _dealService.ChangeStageAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }
}

