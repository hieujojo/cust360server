using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IQuotationRepository
{
    Task InsertAsync(Quotation quote, CancellationToken ct = default);
    Task<Quotation?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<Quotation>> FindAsync(FilterDefinition<Quotation> additionalFilter, SortDefinition<Quotation>? sort = null, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<Quotation> update, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default);
    Task<string?> GetLatestQuotationCodeAsync(int year, CancellationToken ct = default);
}
