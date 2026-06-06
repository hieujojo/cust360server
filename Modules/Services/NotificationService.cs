using CRM.Api.Modules.Models;
using CRM.Api.Modules.Repositories;
using CRM.Api.Services;

namespace CRM.Api.Modules.Services;

public sealed class NotificationService
{
    private readonly NotificationRepository _repository;
    private readonly FirebaseService _firebase;

    public NotificationService(NotificationRepository repository, FirebaseService firebase)
    {
        _repository = repository;
        _firebase = firebase;
    }

    public async Task CreateAsync(Notification notif, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        if (notif.createdAt == default)
            notif.createdAt = now;
        notif.updatedAt = now;

        await _repository.InsertAsync(notif, ct);

        await _firebase.WriteFirestoreSubDocAsync(
            $"notifications/{notif.organizationId}/items/{notif.id}",
            ToFirestorePayload(notif)
        );
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        var updated = await _repository.MarkAllReadAsync(userId, ct);

        foreach (var notif in updated)
        {
            await _firebase.WriteFirestoreSubDocAsync(
                $"notifications/{notif.organizationId}/items/{notif.id}",
                ToFirestorePayload(notif)
            );
        }
    }

    private static object ToFirestorePayload(Notification notif) =>
        new
        {
            id = notif.id,
            organizationId = notif.organizationId,
            userId = notif.userId,
            type = notif.type.ToString(),
            title = notif.title,
            body = notif.body,
            contextUrl = notif.contextUrl,
            isRead = notif.isRead,
            isDeleted = notif.isDeleted,
            createdAt = notif.createdAt,
            updatedAt = notif.updatedAt,
        };
}
