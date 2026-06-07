using MongoDB.Driver;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

/// <summary>
/// Interface cho UserRepository. Hợp đồng truy cập data User trong MongoDB.
/// Mục đích: Loose coupling, dễ test Service (mock repo), có thể đổi DB/cache.
/// </summary>
public interface IUserRepository
{
    Task<User?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task<(List<User> Items, long Total)> FindPagedAsync(
        int? role, string? departmentId, bool? isActive, string? status, string? search,
        int page, int pageSize, CancellationToken ct = default);

    Task<long> CountByDepartmentAsync(string departmentId, CancellationToken ct = default);

    Task SetLastLoginAsync(string id, DateTime loginAt, CancellationToken ct = default);

    Task<List<User>> FindAllAsync(CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Check if employee code exists dalam organization.</summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);

    /// <summary>Đếm tổng users trong org — dùng để generate EmployeeCode.</summary>
    Task<long> CountAllAsync(CancellationToken ct = default);

    Task InsertAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<User> update, CancellationToken ct = default);
    Task SetActiveStatusAsync(string id, bool isActive, CancellationToken ct = default);
    Task<User?> FindByResetTokenAsync(string token, CancellationToken ct = default);
    Task SetResetTokenAsync(string id, string token, DateTime expiry, CancellationToken ct = default);
    Task ClearResetTokenAsync(string id, CancellationToken ct = default);
    Task EnsureIndexesAsync(CancellationToken ct = default);
}
