using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

/// <summary>
/// Interface cho AuditLogService. Hợp đồng ghi log hành động user.
/// Mục đích: Loose coupling, dễ test Controller.
/// </summary>
public interface IAuditLogService
{
    /// <summary>Ghi log. Không throw exception. organizationId optional - nếu null thì lấy từ CurrentUser.</summary>
    Task LogAsync(
        string action,
        string? organizationId = null,
        string? targetUserId = null, string? targetUserEmail = null,
        Dictionary<string, string>? metadata = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default);

    Task<PagedResult<AuditLogResponse>> GetPagedAsync(
        GetAuditLogsRequest request, CancellationToken ct = default);
}
