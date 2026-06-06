using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Repositories;

public sealed class NotificationRepository : BaseRepository<Notification>
{
    private const string CollectionName = "notifications";

    public NotificationRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser) { }

    public async Task<List<Notification>> GetByUserAsync(
        string userId,
        int limit = 50,
        CancellationToken ct = default
    )
    {
        var filter = ActiveOrgFilter & Builders<Notification>.Filter.Eq(x => x.userId, userId);
        var sort = Builders<Notification>.Sort.Descending(x => x.createdAt);

        return await Collection.Find(filter).Sort(sort).Limit(limit).ToListAsync(ct);
    }

    /// <summary>Đánh dấu tất cả unread của user là đã đọc. Trả về các bản ghi đã cập nhật.</summary>
    public async Task<List<Notification>> MarkAllReadAsync(
        string userId,
        CancellationToken ct = default
    )
    {
        var filter =
            ActiveOrgFilter
            & Builders<Notification>.Filter.Eq(x => x.userId, userId)
            & Builders<Notification>.Filter.Eq(x => x.isRead, false);

        var items = await Collection.Find(filter).ToListAsync(ct);
        if (items.Count == 0)
            return items;

        var now = DateTime.UtcNow;
        var update = Builders<Notification>
            .Update.Set(x => x.isRead, true)
            .Set(x => x.updatedAt, now);

        await Collection.UpdateManyAsync(filter, update, cancellationToken: ct);

        foreach (var item in items)
        {
            item.isRead = true;
            item.updatedAt = now;
        }

        return items;
    }

    public new async Task InsertAsync(Notification notification, CancellationToken ct = default) =>
        await base.InsertAsync(notification, ct);
}
