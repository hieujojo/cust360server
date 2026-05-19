using MongoDB.Driver;
using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Repositories;

/// <summary>Append-only — không có Update/Delete.</summary>
public sealed class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    private const string CollectionName = "audit_logs";

    public AuditLogRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser) { }

    /// <summary>Insert audit log. organizationId đã được set sẵn trong AuditLogService.</summary>
    public new async Task InsertAsync(AuditLog log, CancellationToken ct = default)
    {
        // Không gọi base.InsertAsync vì nó sẽ override organizationId từ CurrentUser
        // AuditLog.organizationId đã được set đúng trong AuditLogService
        await Collection.InsertOneAsync(log, cancellationToken: ct);
    }

    public async Task<(List<AuditLog> Items, long Total)> FindPagedAsync(
        string? action, string? actorId, string? targetUserId,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken ct = default)
    {
        var filter = OrgFilter;

        if (!string.IsNullOrWhiteSpace(action))
            filter &= Builders<AuditLog>.Filter.Eq(x => x.action, action);

        if (!string.IsNullOrWhiteSpace(actorId))
            filter &= Builders<AuditLog>.Filter.Eq(x => x.actorId, actorId);

        if (!string.IsNullOrWhiteSpace(targetUserId))
            filter &= Builders<AuditLog>.Filter.Eq(x => x.targetUserId, targetUserId);

        if (fromDate.HasValue)
            filter &= Builders<AuditLog>.Filter.Gte(x => x.createdAt, fromDate.Value);

        if (toDate.HasValue)
            filter &= Builders<AuditLog>.Filter.Lte(x => x.createdAt, toDate.Value);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await Collection
            .Find(filter)
            .Sort(Builders<AuditLog>.Sort.Descending(x => x.createdAt))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<AuditLog>(
                Builders<AuditLog>.IndexKeys.Ascending(x => x.createdAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(365), Name = "ttl_1year" }),

            new CreateIndexModel<AuditLog>(
                Builders<AuditLog>.IndexKeys
                    .Ascending(x => x.organizationId)
                    .Ascending(x => x.action)
                    .Descending(x => x.createdAt),
                new CreateIndexOptions { Name = "org_action_date" }),

            new CreateIndexModel<AuditLog>(
                Builders<AuditLog>.IndexKeys
                    .Ascending(x => x.organizationId)
                    .Ascending(x => x.actorId),
                new CreateIndexOptions { Name = "org_actor" }),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
