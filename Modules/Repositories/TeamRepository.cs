using MongoDB.Driver;
using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Repositories;

/// <summary>Repository quản lý Team trong MongoDB. Collection: teams.</summary>
public sealed class TeamRepository : BaseRepository<Team>, ITeamRepository
{
    private const string CollectionName = "teams";

    // Cần truy cập collection users để đếm members
    private readonly IMongoCollection<MongoDB.Bson.BsonDocument> _usersCollection;

    public TeamRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser)
    {
        _usersCollection = context.GetCollection<MongoDB.Bson.BsonDocument>("users");
    }

    public new async Task<Team?> FindByIdAsync(string id, CancellationToken ct = default)
        => await base.FindByIdAsync(id, ct);

    public async Task<List<Team>> FindByDepartmentAsync(string departmentId, CancellationToken ct = default)
        => await FindManyAsync(
            Builders<Team>.Filter.Eq(x => x.departmentId, departmentId),
            Builders<Team>.Sort.Ascending(x => x.name),
            ct: ct);

    public async Task<List<Team>> FindAllAsync(CancellationToken ct = default)
        => await FindManyAsync(
            Builders<Team>.Filter.Empty,
            Builders<Team>.Sort.Ascending(x => x.name),
            ct: ct);

    public async Task<bool> NameExistsInDepartmentAsync(
        string name, string departmentId, string? excludeId = null, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter
            & Builders<Team>.Filter.Eq(x => x.departmentId, departmentId)
            & Builders<Team>.Filter.Regex(x => x.name, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i"));

        if (!string.IsNullOrEmpty(excludeId))
            filter &= Builders<Team>.Filter.Ne(x => x.id, excludeId);

        return await Collection.Find(filter).AnyAsync(ct);
    }

    public async Task<int> CountMembersAsync(string teamId, CancellationToken ct = default)
    {
        var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.And(
            MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("teamId", teamId),
            MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("isDeleted", false));

        return (int)await _usersCollection.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public new async Task InsertAsync(Team team, CancellationToken ct = default)
        => await base.InsertAsync(team, ct);

    public new async Task UpdateAsync(string id, UpdateDefinition<Team> update, CancellationToken ct = default)
        => await base.UpdateAsync(id, update, ct);

    public new async Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default)
        => await base.SoftDeleteAsync(id, ct);

    public async Task SoftDeleteByDepartmentAsync(string departmentId, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<Team>.Filter.Eq(x => x.departmentId, departmentId);
        var update = Builders<Team>.Update
            .Set(x => x.isDeleted, true)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await Collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.departmentId),
                new CreateIndexOptions { Name = "org_department" }),

            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.departmentId).Ascending(x => x.name),
                new CreateIndexOptions { Name = "org_department_name" }),

            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.isDeleted),
                new CreateIndexOptions { Name = "org_isDeleted" }),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
