using MongoDB.Driver;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

/// <summary>
/// Interface cho CustomerRepository. Hợp đồng truy cập data Customer trong MongoDB.
/// </summary>
public interface ICustomerRepository
{
    // ─── CRUD ─────────────────────────────────────────────────────────────────
    Task<Customer?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<Customer?> FindByCustomerCodeAsync(string code, CancellationToken ct = default);
    Task InsertAsync(Customer customer, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<Customer> update, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default);
    Task<bool> RestoreAsync(string id, CancellationToken ct = default);

    // ─── List & Pagination ────────────────────────────────────────────────────
    Task<(List<Customer> Items, long Total)> FindPagedAsync(
        string? status, string? ownerId, string? phone,
        string sortBy, string sortDir,
        int page, int pageSize,
        CancellationToken ct = default);

    // ─── Contact Operations (embedded array) ──────────────────────────────────
    Task<bool> AddContactAsync(string customerId, Contact contact, CancellationToken ct = default);
    Task<bool> UpdateContactAsync(string customerId, string contactId, UpdateDefinition<Customer> update, CancellationToken ct = default);
    Task<bool> RemoveContactAsync(string customerId, string contactId, CancellationToken ct = default);
    Task<bool> ResetAllContactsPrimaryAsync(string customerId, CancellationToken ct = default);
    Task<bool> SetContactPrimaryAsync(string customerId, string contactId, CancellationToken ct = default);

    // ─── Validation helpers ───────────────────────────────────────────────────
    Task<bool> IsCustomerCodeUniqueAsync(string code, CancellationToken ct = default);

    // ─── Counter (cho code generation) ────────────────────────────────────────
    Task<long> GetNextSequenceAsync(string key, CancellationToken ct = default);

    // ─── Indexes ──────────────────────────────────────────────────────────────
    Task EnsureIndexesAsync(CancellationToken ct = default);
}
