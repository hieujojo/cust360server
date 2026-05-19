using MongoDB.Driver;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<Department>> FindAllAsync(CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, string? excludeId = null, CancellationToken ct = default);
    Task InsertAsync(Department department, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<Department> update, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default);
    Task EnsureIndexesAsync(CancellationToken ct = default);
}
