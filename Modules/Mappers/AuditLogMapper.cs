using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

/// <summary>AuditLog entity (camelCase) → DTO (PascalCase).</summary>
public static class AuditLogMapper
{
    public static AuditLogResponse ToResponse(this AuditLog log)
        => new()
        {
            Id              = log.id,
            ActorId         = log.actorId,
            ActorEmail      = log.actorEmail,
            Action          = log.action,
            TargetUserId    = log.targetUserId,
            TargetUserEmail = log.targetUserEmail,
            IpAddress       = log.ipAddress,
            UserAgent       = log.userAgent,
            Metadata        = log.metadata,
            CreatedAt       = log.createdAt
        };

    public static List<AuditLogResponse> ToResponseList(this IEnumerable<AuditLog> logs)
        => logs.Select(ToResponse).ToList();
}
