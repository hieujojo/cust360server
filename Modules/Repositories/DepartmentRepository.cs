using MongoDB.Driver;
using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Repositories;

/// <summary>Repository quản lý Department trong MongoDB. Collection: departments.</summary>
public sealed class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
{
    private const string CollectionName = "departments";

    public DepartmentRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser) { }

    public new async Task<Department?> FindByIdAsync(string id, CancellationToken ct = default)
        => await base.FindByIdAsync(id, ct);

    public async Task<List<Department>> FindAllAsync(CancellationToken ct = default)
        => await FindManyAsync(
            Builders<Department>.Filter.Empty,
            Builders<Department>.Sort.Ascending(x => x.name),
            ct: ct);

    public async Task<bool> NameExistsAsync(string name, string? excludeId = null, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter
            & Builders<Department>.Filter.Regex(x => x.name, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i"));

        if (!string.IsNullOrEmpty(excludeId))
            filter &= Builders<Department>.Filter.Ne(x => x.id, excludeId);

        return await Collection.Find(filter).AnyAsync(ct);
    }

    public new async Task InsertAsync(Department department, CancellationToken ct = default)
        => await base.InsertAsync(department, ct);

    public new async Task UpdateAsync(string id, UpdateDefinition<Department> update, CancellationToken ct = default)
        => await base.UpdateAsync(id, update, ct);

    public new async Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default)
        => await base.SoftDeleteAsync(id, ct);

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Department>(
                Builders<Department>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.name),
                new CreateIndexOptions { Name = "org_name" }),

            new CreateIndexModel<Department>(
                Builders<Department>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.isDeleted),
                new CreateIndexOptions { Name = "org_isDeleted" }),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
