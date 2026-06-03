using CRM.Api.Modules.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/webhooks/calendar")]
[AllowAnonymous]
public sealed class CalendarWebhookController : ControllerBase
{
    private readonly IGoogleSyncService _syncService;

    public CalendarWebhookController(
        IGoogleSyncService syncService,
        ILogger<CalendarWebhookController> logger
    )
    {
        _syncService = syncService;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        var resourceState = Request.Headers["X-Goog-Resource-State"].ToString();
        ;

        // Lần đầu đăng ký Google gửi "sync", bỏ qua
        if (resourceState == "sync")
            return Ok();

        // Có thay đổi → sync tất cả user
        try
        {
            await _syncService.SyncAllConnectedUsersAsync(ct);
        }
        catch (Exception)
        {
            //
        }

        return Ok();
    }
}
