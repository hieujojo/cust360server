using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CRM.Api.Modules.Repositories;

public sealed class ActivityRepository : BaseRepository<Activity>, IActivityRepository
{
    private const string CollectionName = "activities";

    public ActivityRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser) { }

    public new async Task InsertAsync(Activity activity, CancellationToken ct = default)
        => await base.InsertAsync(activity, ct);

    public new async Task<Activity?> FindByIdAsync(string id, CancellationToken ct = default)
        => await base.FindByIdAsync(id, ct);

    public async Task<Activity?> FindByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        var filter = DepartmentScopedFilter
            & Builders<Activity>.Filter.Eq(x => x.externalId, externalId);
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<Activity?> FindByExternalIdInOrgAsync(
        string organizationId, string externalId, CancellationToken ct = default)
    {
        var filter = Builders<Activity>.Filter.Eq(x => x.organizationId, organizationId)
            & Builders<Activity>.Filter.Eq(x => x.externalId, externalId)
            & Builders<Activity>.Filter.Eq(x => x.isDeleted, false);
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task InsertForOrgAsync(Activity activity, CancellationToken ct = default)
    {
        activity.isDeleted = false;
        await Collection.InsertOneAsync(activity, cancellationToken: ct);
    }

    public async Task<List<Activity>> FindCursorAsync(
        FilterDefinition<Activity> additionalFilter,
        DateTime? cursorOccurredAt,
        string? cursorId,
        int limit,
        CancellationToken ct = default)
    {
        var filter = DepartmentScopedFilter & additionalFilter;

        if (cursorOccurredAt.HasValue && !string.IsNullOrEmpty(cursorId))
        {
            var cursorFilter = Builders<Activity>.Filter.Or(
                Builders<Activity>.Filter.Lt(x => x.occurredAt, cursorOccurredAt.Value),
                Builders<Activity>.Filter.And(
                    Builders<Activity>.Filter.Eq(x => x.occurredAt, cursorOccurredAt.Value),
                    Builders<Activity>.Filter.Lt(x => x.id, cursorId)));

            filter &= cursorFilter;
        }

        var sort = Builders<Activity>.Sort
            .Descending(x => x.occurredAt)
            .Descending(x => x.id);

        return await Collection.Find(filter).Sort(sort).Limit(limit).ToListAsync(ct);
    }

    public new async Task UpdateAsync(string id, UpdateDefinition<Activity> update, CancellationToken ct = default)
        => await base.UpdateAsync(id, update, ct);

    public new async Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default)
        => await base.SoftDeleteAsync(id, ct);

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        // Replace legacy sparse unique index (allowed only one externalId:null per org).
        const string legacyExternalIdIndex = "org_externalId_unique";
        const string partialExternalIdIndex = "org_externalId_partial_unique";

        using var cursor = await Collection.Indexes.ListAsync(cancellationToken: ct);
        var existing = await cursor.ToListAsync(ct);
        var indexNames = existing.Select(i => i["name"].AsString).ToHashSet();
        if (indexNames.Contains(legacyExternalIdIndex))
            await Collection.Indexes.DropOneAsync(legacyExternalIdIndex, ct);

        var indexes = new List<CreateIndexModel<Activity>>
        {
            new(
                Builders<Activity>.IndexKeys
                    .Ascending(x => x.organizationId)
                    .Ascending(x => x.customerId)
                    .Descending(x => x.occurredAt),
                new CreateIndexOptions { Name = "org_customer_occurredAt" }),
            new(
                Builders<Activity>.IndexKeys
                    .Ascending(x => x.organizationId)
                    .Ascending(x => x.dealId)
                    .Descending(x => x.occurredAt),
                new CreateIndexOptions { Name = "org_deal_occurredAt" }),
            new(
                Builders<Activity>.IndexKeys
                    .Ascending(x => x.organizationId)
                    .Ascending(x => x.departmentId),
                new CreateIndexOptions { Name = "org_department" }),
        };

        if (!indexNames.Contains(partialExternalIdIndex))
        {
            indexes.Add(new CreateIndexModel<Activity>(
                Builders<Activity>.IndexKeys
                    .Ascending(x => x.organizationId)
                    .Ascending(x => x.externalId),
                new CreateIndexOptions<Activity>
                {
                    Name = partialExternalIdIndex,
                    Unique = true,
                        PartialFilterExpression = Builders<Activity>.Filter.And(
                            Builders<Activity>.Filter.Exists(x => x.externalId),
                            Builders<Activity>.Filter.Type(x => x.externalId, BsonType.String),
                            Builders<Activity>.Filter.Ne(x => x.externalId, ""))
                }));
        }

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
