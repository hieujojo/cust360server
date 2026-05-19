using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

/// <summary>
/// Interface cho AuditLogRepository. Hợp đồng truy cập data AuditLog (append-only).
/// Mục đích: Loose coupling, dễ test Service (mock repo).
/// </summary>
public interface IAuditLogRepository
{
    Task InsertAsync(AuditLog log, CancellationToken ct = default);

    Task<(List<AuditLog> Items, long Total)> FindPagedAsync(
        string? action, string? actorId, string? targetUserId,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken ct = default);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
