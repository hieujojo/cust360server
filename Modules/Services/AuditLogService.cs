using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

/// <summary>Ghi và truy vấn audit logs. Append-only.</summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepo;
    private readonly CurrentUser _currentUser;

    public AuditLogService(IAuditLogRepository auditLogRepo, CurrentUser currentUser)
    {
        _auditLogRepo = auditLogRepo;
        _currentUser = currentUser;
    }

    /// <summary>Ghi log. Không throw exception. organizationId optional - nếu null thì lấy từ CurrentUser.</summary>
    public async Task LogAsync(
        string action,
        string? organizationId = null,
        string? targetUserId = null,
        string? targetUserEmail = null,
        Dictionary<string, string>? metadata = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default
    )
    {
        try
        {
            var log = new AuditLog
            {
                organizationId = organizationId ?? _currentUser.OrganizationId,
                actorId = _currentUser.IsAuthenticated ? _currentUser.UserId : null,
                actorEmail = _currentUser.Email,
                action = action,
                targetUserId = targetUserId,
                targetUserEmail = targetUserEmail,
                ipAddress = ipAddress,
                userAgent = userAgent,
                metadata = metadata,
                createdAt = DateTime.UtcNow,
            };

            await _auditLogRepo.InsertAsync(log, ct);
        }
        catch (Exception)
        {
            // Ignore exceptions during audit log writing
        }
    }

    public async Task<PagedResult<AuditLogResponse>> GetPagedAsync(
        GetAuditLogsRequest request,
        CancellationToken ct = default
    )
    {
        var (items, total) = await _auditLogRepo.FindPagedAsync(
            request.Action,
            request.ActorId,
            request.TargetUserId,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize,
            ct
        );

        return PagedResult<AuditLogResponse>.Create(
            items.ToResponseList(),
            total,
            request.Page,
            request.PageSize
        );
    }
}
