using MongoDB.Driver;
using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Repositories;

/// <summary>Repository quản lý Feedback trong MongoDB. Collection: feedbacks.</summary>
public sealed class FeedbackRepository : BaseRepository<Feedback>, IFeedbackRepository
{
    private const string CollectionName = "feedbacks";

    public FeedbackRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser)
    {
    }

    // ─── CRUD ─────────────────────────────────────────────────────────────────

    public new async Task<Feedback?> FindByIdAsync(string id, CancellationToken ct = default)
        => await base.FindByIdAsync(id, ct);

    public new async Task InsertAsync(Feedback feedback, CancellationToken ct = default)
        => await base.InsertAsync(feedback, ct);

    public new async Task UpdateAsync(string id, UpdateDefinition<Feedback> update, CancellationToken ct = default)
        => await base.UpdateAsync(id, update, ct);

    public new async Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default)
        => await base.SoftDeleteAsync(id, ct);

    // ─── List & Pagination ────────────────────────────────────────────────────

    public async Task<(List<Feedback> Items, long Total)> FindPagedAsync(
        string? type,
        string? category,
        string? status,
        string sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // Build dynamic filter
        var additionalFilter = Builders<Feedback>.Filter.Empty;

        if (!string.IsNullOrEmpty(type))
            additionalFilter &= Builders<Feedback>.Filter.Eq(f => f.type, type);

        if (!string.IsNullOrEmpty(category))
            additionalFilter &= Builders<Feedback>.Filter.Eq(f => f.category, category);

        if (!string.IsNullOrEmpty(status))
            additionalFilter &= Builders<Feedback>.Filter.Eq(f => f.status, status);

        // Build sort
        var sort = BuildSort(sortBy, sortDir);

        // Count + query
        var total = await CountAsync(additionalFilter, ct);
        var items = await FindManyAsync(
            additionalFilter,
            sort,
            skip: (page - 1) * pageSize,
            limit: pageSize,
            ct: ct);

        return (items, total);
    }

    // ─── Reply Operations ─────────────────────────────────────────────────────

    public async Task<bool> AddReplyAsync(string feedbackId, FeedbackReply reply, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<Feedback>.Filter.Eq(f => f.id, feedbackId);
        var update = Builders<Feedback>.Update
            .Push(f => f.replies, reply)
            .Set(f => f.updatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    // ─── Status Operations ────────────────────────────────────────────────────

    public async Task<bool> UpdateStatusAsync(string id, string status, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<Feedback>.Filter.Eq(f => f.id, id);
        var update = Builders<Feedback>.Update
            .Set(f => f.status, status)
            .Set(f => f.updatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    // ─── Indexes ──────────────────────────────────────────────────────────────

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            // List query: filter org + active + sort by createdAt
            new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys
                    .Ascending(f => f.organizationId)
                    .Ascending(f => f.isDeleted)
                    .Descending(f => f.createdAt),
                new CreateIndexOptions { Name = "org_active_created" }),

            // Type filter
            new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys
                    .Ascending(f => f.organizationId)
                    .Ascending(f => f.type),
                new CreateIndexOptions { Name = "org_type" }),

            // Category filter
            new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys
                    .Ascending(f => f.organizationId)
                    .Ascending(f => f.category),
                new CreateIndexOptions { Name = "org_category" }),

            // Status filter
            new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys
                    .Ascending(f => f.organizationId)
                    .Ascending(f => f.status),
                new CreateIndexOptions { Name = "org_status" }),

            // Author lookup
            new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys
                    .Ascending(f => f.organizationId)
                    .Ascending(f => f.authorId),
                new CreateIndexOptions { Name = "org_author" }),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static SortDefinition<Feedback> BuildSort(string sortBy, string sortDir)
    {
        var isDesc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "createdat" => isDesc ? Builders<Feedback>.Sort.Descending(f => f.createdAt) : Builders<Feedback>.Sort.Ascending(f => f.createdAt),
            "updatedat" => isDesc ? Builders<Feedback>.Sort.Descending(f => f.updatedAt) : Builders<Feedback>.Sort.Ascending(f => f.updatedAt),
            "status"    => isDesc ? Builders<Feedback>.Sort.Descending(f => f.status)    : Builders<Feedback>.Sort.Ascending(f => f.status),
            _           => Builders<Feedback>.Sort.Descending(f => f.createdAt), // Default: newest first
        };
    }
}
