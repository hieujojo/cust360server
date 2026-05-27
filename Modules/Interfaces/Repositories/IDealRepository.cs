using MongoDB.Driver;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IDealRepository
{
    Task InsertAsync(Deal deal, CancellationToken ct = default);
    Task<Deal?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<Deal>> FindAsync(FilterDefinition<Deal> additionalFilter, SortDefinition<Deal>? sort = null, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<Deal> update, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default);
    Task<long> CountByStageAsync(string stage, CancellationToken ct = default);
    Task EnsureIndexesAsync(CancellationToken ct = default);
}

