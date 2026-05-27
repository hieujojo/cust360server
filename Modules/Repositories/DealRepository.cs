using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Repositories;

public sealed class DealRepository : BaseRepository<Deal>, IDealRepository
{
    private const string CollectionName = "deals";

    public DealRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser) { }

    public new async Task InsertAsync(Deal deal, CancellationToken ct = default) => await base.InsertAsync(deal, ct);
    public new async Task<Deal?> FindByIdAsync(string id, CancellationToken ct = default) => await base.FindByIdAsync(id, ct);
    public new async Task UpdateAsync(string id, UpdateDefinition<Deal> update, CancellationToken ct = default) => await base.UpdateAsync(id, update, ct);
    public new async Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default) => await base.SoftDeleteAsync(id, ct);

    public async Task<List<Deal>> FindAsync(FilterDefinition<Deal> additionalFilter, SortDefinition<Deal>? sort = null, CancellationToken ct = default)
        => await FindManyWithDepartmentScopeAsync(additionalFilter, sort: sort, ct: ct);

    public async Task<long> CountByStageAsync(string stage, CancellationToken ct = default)
    {
        var filter = Builders<Deal>.Filter.Eq(x => x.stage, stage);
        var list = await FindManyAsync(filter, limit: 1, ct: ct);
        if (list.Count == 0)
            return 0;

        return await CountAsync(filter, ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Deal>(
                Builders<Deal>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.stage),
                new CreateIndexOptions { Name = "org_stage" }),
            new CreateIndexModel<Deal>(
                Builders<Deal>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.ownerId),
                new CreateIndexOptions { Name = "org_owner" }),
            new CreateIndexModel<Deal>(
                Builders<Deal>.IndexKeys.Ascending(x => x.organizationId).Descending(x => x.updatedAt),
                new CreateIndexOptions { Name = "org_updatedAt" }),
            new CreateIndexModel<Deal>(
                Builders<Deal>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.customerId),
                new CreateIndexOptions { Name = "org_customer" }),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}

