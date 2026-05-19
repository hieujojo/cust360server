using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Authorization;

namespace CRM.Api.Modules.Controllers;

/// <summary>Xem audit logs. Chỉ Owner.</summary>
[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = Policies.OwnerOnly)]
[Produces("application/json")]
public sealed class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
        => _auditLogService = auditLogService;

    /// <summary>Danh sách audit logs có filter và phân trang.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] GetAuditLogsRequest request, CancellationToken ct)
        => Ok(await _auditLogService.GetPagedAsync(request, ct));
}
