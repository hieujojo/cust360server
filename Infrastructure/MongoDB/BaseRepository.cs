using CRM.Api.Shared.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CRM.Api.Infrastructure.MongoDB;

/// <summary>
/// Base repository. Tự động inject organizationId vào mọi query.
/// Không expose IMongoCollection ra ngoài.
/// </summary>
public abstract class BaseRepository<T>
    where T : IOrganizationDocument
{
    private readonly IMongoCollection<T> _collection;
    private readonly CurrentUser _currentUser;

    protected BaseRepository(MongoDbContext context, string collectionName, CurrentUser currentUser)
    {
        _collection = context.GetCollection<T>(collectionName);
        _currentUser = currentUser;
    }

    // ─── Filters ─────────────────────────────────────────────────────────────

    /// <summary>Filter theo organizationId — bắt buộc trong mọi query.</summary>
    protected FilterDefinition<T> OrgFilter =>
        Builders<T>.Filter.Eq("organizationId", _currentUser.OrganizationId);

    /// <summary>OrgFilter + isDeleted = false.</summary>
    protected FilterDefinition<T> ActiveOrgFilter
    {
        get
        {
            var filter = OrgFilter;
            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
                filter &= Builders<T>.Filter.Eq("isDeleted", false);
            return filter;
        }
    }

    /// <summary>
    /// Department-based scoping: User (role=3) chỉ thấy data của department mình.
    /// Owner/Admin thấy toàn bộ.
    /// </summary>
    protected FilterDefinition<T> DepartmentScopedFilter
    {
        get
        {
            var filter = ActiveOrgFilter;

            // Owner (role=1) và Admin (role=2): thấy toàn bộ
            if (_currentUser.Role <= 2)
                return filter;

            // User (role=3): chỉ thấy department của mình
            if (!string.IsNullOrEmpty(_currentUser.DepartmentId))
                filter &= Builders<T>.Filter.Eq("departmentId", _currentUser.DepartmentId);

            return filter;
        }
    }

    /// <summary>Truy cập collection trong subclass.</summary>
    protected IMongoCollection<T> Collection => _collection;

    // ─── Read ─────────────────────────────────────────────────────────────────

    public async Task<T?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<T>.Filter.Eq("id", id);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<List<T>> FindManyAsync(
        FilterDefinition<T> additionalFilter,
        SortDefinition<T>? sort = null,
        int? skip = null,
        int? limit = null,
        CancellationToken ct = default
    )
    {
        var filter = ActiveOrgFilter & additionalFilter;

        var query = _collection.Find(filter);

        if (sort != null)
            query = query.Sort(sort);
        if (skip.HasValue)
            query = query.Skip(skip.Value);
        if (limit.HasValue)
            query = query.Limit(limit.Value);

        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Tìm nhiều documents với department scoping.
    /// User (role=3) chỉ thấy data của department mình.
    /// </summary>
    public async Task<List<T>> FindManyWithDepartmentScopeAsync(
        FilterDefinition<T> additionalFilter,
        SortDefinition<T>? sort = null,
        int? skip = null,
        int? limit = null,
        CancellationToken ct = default
    )
    {
        var filter = DepartmentScopedFilter & additionalFilter;
        var query = _collection.Find(filter);

        if (sort != null)
            query = query.Sort(sort);
        if (skip.HasValue)
            query = query.Skip(skip.Value);
        if (limit.HasValue)
            query = query.Limit(limit.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<long> CountAsync(
        FilterDefinition<T> additionalFilter,
        CancellationToken ct = default
    )
    {
        var filter = ActiveOrgFilter & additionalFilter;
        return await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task<bool> ExistsAsync(
        FilterDefinition<T> additionalFilter,
        CancellationToken ct = default
    )
    {
        var filter = ActiveOrgFilter & additionalFilter;
        return await _collection.Find(filter).AnyAsync(ct);
    }

    // ─── Write ────────────────────────────────────────────────────────────────

    public async Task InsertAsync(T document, CancellationToken ct = default)
    {
        document.OrganizationId = _currentUser.OrganizationId;
        if (document is ISoftDeletable softDeletable)
            softDeletable.IsDeleted = false;
        await _collection.InsertOneAsync(document, cancellationToken: ct);
    }

    public async Task UpdateAsync(
        string id,
        UpdateDefinition<T> update,
        CancellationToken ct = default
    )
    {
        var filter = ActiveOrgFilter & Builders<T>.Filter.Eq("id", id);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default)
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            throw new InvalidOperationException($"{typeof(T).Name} không hỗ trợ soft delete.");

        var filter = ActiveOrgFilter & Builders<T>.Filter.Eq("id", id);
        var update = Builders<T>.Update.Set("isDeleted", true).Set("updatedAt", DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }
}
