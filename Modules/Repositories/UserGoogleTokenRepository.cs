using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Repositories;

public sealed class UserGoogleTokenRepository : BaseRepository<UserGoogleToken>, IUserGoogleTokenRepository
{
    private const string CollectionName = "user_google_tokens";

    public UserGoogleTokenRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser) { }

    public async Task<UserGoogleToken?> FindByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<UserGoogleToken>.Filter.Eq(x => x.userId, userId);
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<List<UserGoogleToken>> FindAllWithSyncEnabledAsync(CancellationToken ct = default)
    {
        var filter = Builders<UserGoogleToken>.Filter.Or(
            Builders<UserGoogleToken>.Filter.Eq(x => x.calendarSyncEnabled, true),
            Builders<UserGoogleToken>.Filter.Eq(x => x.gmailSyncEnabled, true));

        return await Collection.Find(filter).ToListAsync(ct);
    }

    public async Task UpsertAsync(UserGoogleToken token, CancellationToken ct = default)
    {
        token.updatedAt = DateTime.UtcNow;

        var filter = Builders<UserGoogleToken>.Filter.Eq(x => x.organizationId, token.organizationId)
            & Builders<UserGoogleToken>.Filter.Eq(x => x.userId, token.userId);
        var existing = await Collection.Find(filter).FirstOrDefaultAsync(ct);

        if (existing == null)
        {
            token.createdAt = DateTime.UtcNow;
            await Collection.InsertOneAsync(token, cancellationToken: ct);
            return;
        }

        token.id = existing.id;
        token.organizationId = existing.organizationId;
        await Collection.ReplaceOneAsync(filter, token, cancellationToken: ct);
    }

    public async Task DeleteByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var filter = ActiveOrgFilter & Builders<UserGoogleToken>.Filter.Eq(x => x.userId, userId);
        await Collection.DeleteOneAsync(filter, ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<UserGoogleToken>(
                Builders<UserGoogleToken>.IndexKeys
                    .Ascending(x => x.organizationId)
                    .Ascending(x => x.userId),
                new CreateIndexOptions { Name = "org_user_unique", Unique = true }),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
