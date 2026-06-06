using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Repositories;
using CRM.Api.Modules.Services;
using CRM.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = Policies.AnyRole)]
[Produces("application/json")]
public sealed class NotificationController : ControllerBase
{
    private readonly NotificationRepository _repository;
    private readonly NotificationService _notificationService;

    public NotificationController(
        NotificationRepository repository,
        NotificationService notificationService
    )
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "userId is required." });

        return Ok(await _repository.GetByUserAsync(userId, ct: ct));
    }

    [HttpPut("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkAllRead(
        [FromBody] MarkAllNotificationsReadRequest request,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { error = "userId is required." });

        await _notificationService.MarkAllReadAsync(request.UserId, ct);
        return NoContent();
    }
}
