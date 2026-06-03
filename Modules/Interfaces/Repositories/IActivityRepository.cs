using CRM.Api.Modules.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IActivityRepository
{
    Task InsertAsync(Activity activity, CancellationToken ct = default);
    Task<Activity?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<Activity?> FindByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<Activity?> FindByExternalIdInOrgAsync(string organizationId, string externalId, CancellationToken ct = default);
    Task InsertForOrgAsync(Activity activity, CancellationToken ct = default);
    Task<List<Activity>> FindCursorAsync(
        FilterDefinition<Activity> additionalFilter,
        DateTime? cursorOccurredAt,
        string? cursorId,
        int limit,
        CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<Activity> update, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default);
    Task EnsureIndexesAsync(CancellationToken ct = default);
}
