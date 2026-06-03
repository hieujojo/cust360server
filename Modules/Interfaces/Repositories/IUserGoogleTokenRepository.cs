using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IUserGoogleTokenRepository
{
    Task<UserGoogleToken?> FindByUserIdAsync(string userId, CancellationToken ct = default);
    Task<List<UserGoogleToken>> FindAllWithSyncEnabledAsync(CancellationToken ct = default);
    Task UpsertAsync(UserGoogleToken token, CancellationToken ct = default);
    Task DeleteByUserIdAsync(string userId, CancellationToken ct = default);
    Task EnsureIndexesAsync(CancellationToken ct = default);
}
