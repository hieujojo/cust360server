using MongoDB.Bson;
using MongoDB.Driver;
using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Repositories;

/// <summary>Repository quản lý Customer trong MongoDB. Collection: customers.</summary>
public sealed class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
{
    private const string CollectionName = "customers";
    private readonly MongoDbContext _context;

    public CustomerRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser)
    {
        _context = context;
    }

    // ─── CRUD ─────────────────────────────────────────────────────────────────

    public new async Task<Customer?> FindByIdAsync(string id, CancellationToken ct = default)
        => await base.FindByIdAsync(id, ct);

    public async Task<Customer?> FindByCustomerCodeAsync(string code, CancellationToken ct = default)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.customerCode, code);
        var results = await FindManyAsync(filter, limit: 1, ct: ct);
        return results.FirstOrDefault();
    }

    public new async Task InsertAsync(Customer customer, CancellationToken ct = default)
        => await base.InsertAsync(customer, ct);

    public new async Task UpdateAsync(string id, UpdateDefinition<Customer> update, CancellationToken ct = default)
        => await base.UpdateAsync(id, update, ct);

    public new async Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default)
        => await base.SoftDeleteAsync(id, ct);

    public async Task<bool> RestoreAsync(string id, CancellationToken ct = default)
    {
        // Bypass ActiveOrgFilter to find deleted document
        var filter = OrgFilter 
            & Builders<Customer>.Filter.Eq(c => c.id, id)
            & Builders<Customer>.Filter.Eq(c => c.isDeleted, true);
            
        var update = Builders<Customer>.Update
            .Set(c => c.isDeleted, false)
            .Set(c => c.updatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    // ─── List & Pagination ────────────────────────────────────────────────────

    public async Task<(List<Customer> Items, long Total)> FindPagedAsync(
        string? status, string? ownerId, string? phone,
        string sortBy, string sortDir,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        // Build dynamic filter
        var additionalFilter = Builders<Customer>.Filter.Empty;

        if (!string.IsNullOrEmpty(status))
            additionalFilter &= Builders<Customer>.Filter.Eq(c => c.status, status);

        if (!string.IsNullOrEmpty(ownerId))
            additionalFilter &= Builders<Customer>.Filter.Eq(c => c.ownerId, ownerId);

        if (!string.IsNullOrEmpty(phone))
            additionalFilter &= Builders<Customer>.Filter.Eq(c => c.phone, phone);

        // Build sort
        var sort = BuildSort(sortBy, sortDir);

        // Count + query with department scoping (Role 3 chỉ thấy dept mình)
        var total = await CountWithDepartmentScopeAsync(additionalFilter, ct);
        var items = await FindManyWithDepartmentScopeAsync(
            additionalFilter, sort,
            skip: (page - 1) * pageSize,
            limit: pageSize,
            ct: ct);

        return (items, total);
    }

    // ─── Contact Operations ───────────────────────────────────────────────────

    public async Task<bool> AddContactAsync(string customerId, Contact contact, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<Customer>.Filter.Eq(c => c.id, customerId);
        var update = Builders<Customer>.Update
            .Push(c => c.contacts, contact)
            .Set(c => c.updatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateContactAsync(
        string customerId, string contactId,
        UpdateDefinition<Customer> update, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter
            & Builders<Customer>.Filter.Eq(c => c.id, customerId)
            & Builders<Customer>.Filter.ElemMatch(c => c.contacts, ct2 => ct2.id == contactId);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> RemoveContactAsync(string customerId, string contactId, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<Customer>.Filter.Eq(c => c.id, customerId);
        var update = Builders<Customer>.Update
            .PullFilter(c => c.contacts, c => c.id == contactId)
            .Set(c => c.updatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> ResetAllContactsPrimaryAsync(string customerId, CancellationToken ct = default)
    {
        // Set all contacts.isPrimary = false
        var filter = ActiveOrgFilter & Builders<Customer>.Filter.Eq(c => c.id, customerId);
        var update = Builders<Customer>.Update.Set("contacts.$[].isPrimary", false);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public async Task<bool> SetContactPrimaryAsync(string customerId, string contactId, CancellationToken ct = default)
    {
        // Set specific contact isPrimary = true using positional operator
        var filter = ActiveOrgFilter
            & Builders<Customer>.Filter.Eq(c => c.id, customerId)
            & Builders<Customer>.Filter.ElemMatch(c => c.contacts, c => c.id == contactId);

        var update = Builders<Customer>.Update
            .Set("contacts.$.isPrimary", true)
            .Set(c => c.updatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    // ─── Validation helpers ───────────────────────────────────────────────────

    public async Task<bool> IsCustomerCodeUniqueAsync(string code, CancellationToken ct = default)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.customerCode, code);
        return !await ExistsAsync(filter, ct);
    }

    // ─── Counter (dùng cho customer code generation — thread-safe) ────────────

    public async Task<long> GetNextSequenceAsync(string key, CancellationToken ct = default)
    {
        var collection = _context.GetCollection<BsonDocument>("counters");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", key);
        var update = Builders<BsonDocument>.Update.Inc("seq", 1L);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await collection.FindOneAndUpdateAsync(filter, update, options, ct);
        return result["seq"].AsInt64;
    }

    // ─── Indexes ──────────────────────────────────────────────────────────────

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            // List query: filter org + active + sort by createdAt
            new CreateIndexModel<Customer>(
                Builders<Customer>.IndexKeys
                    .Ascending(c => c.organizationId)
                    .Ascending(c => c.isDeleted)
                    .Descending(c => c.createdAt),
                new CreateIndexOptions { Name = "org_active_created" }),

            // Department scoping cho Role 3
            new CreateIndexModel<Customer>(
                Builders<Customer>.IndexKeys
                    .Ascending(c => c.organizationId)
                    .Ascending(c => c.departmentId),
                new CreateIndexOptions { Name = "org_department" }),

            // Customer code uniqueness per org
            new CreateIndexModel<Customer>(
                Builders<Customer>.IndexKeys
                    .Ascending(c => c.organizationId)
                    .Ascending(c => c.customerCode),
                new CreateIndexOptions { Name = "org_customerCode", Unique = true }),

            // Owner lookup
            new CreateIndexModel<Customer>(
                Builders<Customer>.IndexKeys
                    .Ascending(c => c.organizationId)
                    .Ascending(c => c.ownerId),
                new CreateIndexOptions { Name = "org_owner" }),

            // Status filter
            new CreateIndexModel<Customer>(
                Builders<Customer>.IndexKeys
                    .Ascending(c => c.organizationId)
                    .Ascending(c => c.status),
                new CreateIndexOptions { Name = "org_status" }),

            // Name sort/search
            new CreateIndexModel<Customer>(
                Builders<Customer>.IndexKeys
                    .Ascending(c => c.organizationId)
                    .Ascending(c => c.name),
                new CreateIndexOptions { Name = "org_name" }),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static SortDefinition<Customer> BuildSort(string sortBy, string sortDir)
    {
        var isDesc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "name"      => isDesc ? Builders<Customer>.Sort.Descending(c => c.name)      : Builders<Customer>.Sort.Ascending(c => c.name),
            "status"    => isDesc ? Builders<Customer>.Sort.Descending(c => c.status)    : Builders<Customer>.Sort.Ascending(c => c.status),
            "owner"     => isDesc ? Builders<Customer>.Sort.Descending(c => c.ownerId)   : Builders<Customer>.Sort.Ascending(c => c.ownerId),
            _           => isDesc ? Builders<Customer>.Sort.Descending(c => c.createdAt) : Builders<Customer>.Sort.Ascending(c => c.createdAt),
        };
    }

    /// <summary>Đếm documents với department scoping.</summary>
    private async Task<long> CountWithDepartmentScopeAsync(
        FilterDefinition<Customer> additionalFilter, CancellationToken ct)
    {
        var filter = DepartmentScopedFilter & additionalFilter;
        return await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }
}
