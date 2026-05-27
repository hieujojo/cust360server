using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/settings/pipeline-stages")]
[Authorize(Policy = Policies.AdminOrAbove)]
[Produces("application/json")]
public sealed class PipelineSettingsController : ControllerBase
{
    private readonly IPipelineStageService _pipelineStageService;

    public PipelineSettingsController(IPipelineStageService pipelineStageService)
        => _pipelineStageService = pipelineStageService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _pipelineStageService.GetAsync(ct));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] UpsertPipelineStageRequest request, CancellationToken ct)
    {
        var result = await _pipelineStageService.CreateAsync(request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string id, [FromBody] UpsertPipelineStageRequest request, CancellationToken ct)
    {
        var result = await _pipelineStageService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _pipelineStageService.DeleteAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reorder([FromBody] ReorderPipelineStagesRequest request, CancellationToken ct)
    {
        var result = await _pipelineStageService.ReorderAsync(request, ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }
}

