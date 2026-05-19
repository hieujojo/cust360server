using MongoDB.Driver;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface ITeamRepository
{
    Task<Team?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<Team>> FindByDepartmentAsync(string departmentId, CancellationToken ct = default);
    Task<List<Team>> FindAllAsync(CancellationToken ct = default);
    Task<bool> NameExistsInDepartmentAsync(string name, string departmentId, string? excludeId = null, CancellationToken ct = default);

    /// <summary>Đếm số thành viên (users) trong team — dùng để hiển thị MemberCount.</summary>
    Task<int> CountMembersAsync(string teamId, CancellationToken ct = default);

    Task InsertAsync(Team team, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<Team> update, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default);

    /// <summary>Xóa mềm tất cả teams thuộc một department — dùng khi xóa department.</summary>
    Task SoftDeleteByDepartmentAsync(string departmentId, CancellationToken ct = default);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
