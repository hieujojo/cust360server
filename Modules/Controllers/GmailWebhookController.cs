using System.Text.Json;
using CRM.Api.Modules.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Modules.Controllers;

[ApiController]
[Route("api/webhooks/gmail")]
[AllowAnonymous]
public sealed class GmailWebhookController : ControllerBase
{
    private readonly IGoogleSyncService _syncService;
    private readonly ILogger<GmailWebhookController> _logger; // thêm field

    public GmailWebhookController(
        IGoogleSyncService syncService,
        ILogger<GmailWebhookController> logger
    )
    {
        _syncService = syncService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        try
        {
            using var doc = JsonDocument.Parse(body);
            var message = doc.RootElement.GetProperty("message");
            var data = message.GetProperty("data").GetString();
            if (string.IsNullOrEmpty(data))
                return Ok();

            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data));
            using var inner = JsonDocument.Parse(decoded);
            var email = inner.RootElement.GetProperty("emailAddress").GetString() ?? string.Empty;
            var historyId = inner.RootElement.GetProperty("historyId").GetRawText();

            await _syncService.ProcessGmailNotificationAsync(email, historyId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gmail webhook processing failed");
        }

        return Ok();
    }
}
