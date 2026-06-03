using Google.Apis.Auth.OAuth2;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IGoogleSyncService
{
    Task SyncCalendarForUserAsync(
        string userId,
        string organizationId,
        CancellationToken ct = default
    );
    Task ProcessGmailNotificationAsync(
        string emailAddress,
        string historyId,
        CancellationToken ct = default
    );
    Task SyncAllConnectedUsersAsync(CancellationToken ct = default);
    Task RegisterCalendarWatchAsync(
        string userId,
        string organizationId,
        UserCredential credential,
        CancellationToken ct = default
    );
}
