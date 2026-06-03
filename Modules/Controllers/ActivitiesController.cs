using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/activities")]
[Authorize(Policy = Policies.AnyRole)]
[Produces("application/json")]
public sealed class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivitiesController(IActivityService activityService) => _activityService = activityService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] ActivityListFilterRequest request, CancellationToken ct)
        => Ok(await _activityService.GetListAsync(request, ct));

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
    {
        var result = await _activityService.GetByIdAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateActivityRequest request, CancellationToken ct)
    {
        var result = await _activityService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return CreatedAtAction(nameof(Detail), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateActivityRequest request, CancellationToken ct)
    {
        var result = await _activityService.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _activityService.DeleteAsync(id, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return NoContent();
    }
}
