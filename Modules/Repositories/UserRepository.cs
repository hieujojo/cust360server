using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Repositories;

/// <summary>
/// Repository quản lý User trong MongoDB.
/// Kế thừa BaseRepository → tự động filter theo organizationId (multi-tenant).
/// </summary>
public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    private const string CollectionName = "users";

    public UserRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, CollectionName, currentUser) { }

    // ─── READ ────────────────────────────────────────────────────────────────

    /// <summary>Tìm user theo ID. Filter: organizationId + isDeleted=false + id</summary>
    public new async Task<User?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            return null;

        var filter =
            Builders<User>.Filter.Eq("_id", objectId)
            & Builders<User>.Filter.Eq(x => x.isDeleted, false);

        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    /// <summary>Tìm user theo email (lowercase, unique trong org). Use case: Login, validate email.</summary>
    /// <summary>
    /// Tìm user theo email (lowercase).
    /// KHÔNG filter theo org vì dùng cho login (chưa có org context).
    /// Nếu có nhiều user cùng email ở các org khác nhau, trả về user đầu tiên.
    /// TODO: Phase 2 - Thêm subdomain hoặc organizationId vào login request.
    /// </summary>
    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.email, email.ToLowerInvariant());
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Danh sách users có filter + pagination. Trả về (Items, Total).
    /// Filter: role, departmentId, isActive, search (tìm trong displayName hoặc email).
    /// Sort: createdAt DESC (mới nhất trước).
    ///
    /// DEPARTMENT SCOPING:
    /// - Owner/Admin: thấy tất cả users trong org
    /// - User (role=3): chỉ thấy users trong department của mình
    /// </summary>
    public async Task<(List<User> Items, long Total)> FindPagedAsync(
        int? role,
        string? departmentId,
        bool? isActive,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        // Base filter với department scoping
        var filter = Builders<User>.Filter.Empty;

        if (role.HasValue)
            filter &= Builders<User>.Filter.Eq(x => x.role, role.Value);

        if (!string.IsNullOrWhiteSpace(departmentId))
            filter &= Builders<User>.Filter.Eq(x => x.departmentId, departmentId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            filter &= status.ToLowerInvariant() switch
            {
                "active" => Builders<User>.Filter.Eq(x => x.isActive, true)
                    & Builders<User>.Filter.Ne(x => x.lastLoginAt, null),
                "inactive" => Builders<User>.Filter.Eq(x => x.isActive, false),
                "pending" => Builders<User>.Filter.Eq(x => x.isActive, true)
                    & Builders<User>.Filter.Eq(x => x.lastLoginAt, null),
                _ => Builders<User>.Filter.Empty,
            };
        }
        else if (isActive.HasValue)
        {
            filter &= Builders<User>.Filter.Eq(x => x.isActive, isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var regex = new MongoDB.Bson.BsonRegularExpression(search, "i");
            filter &= Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(x => x.displayName, regex),
                Builders<User>.Filter.Regex(x => x.email, regex)
            );
        }

        // Apply department scoping
        var scopedFilter = DepartmentScopedFilter & filter;

        var total = await Collection.CountDocumentsAsync(scopedFilter, cancellationToken: ct);
        var items = await Collection
            .Find(scopedFilter)
            .Sort(Builders<User>.Sort.Descending(x => x.createdAt))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<List<User>> FindAllAsync(CancellationToken ct = default)
    {
        return await Collection
            .Find(ActiveOrgFilter)
            .Sort(Builders<User>.Sort.Descending(x => x.createdAt))
            .ToListAsync(ct);
    }

    /// <summary>Kiểm tra email đã tồn tại trong org chưa. Use case: Validate khi tạo user.</summary>
    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var filter = OrgFilter & Builders<User>.Filter.Eq(x => x.email, email.ToLowerInvariant());
        return await Collection.Find(filter).AnyAsync(ct);
    }

    /// <summary>Kiểm tra employee code đã tồn tại trong org chưa.</summary>
    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        var filter = OrgFilter & Builders<User>.Filter.Eq(x => x.employeeCode, code);
        return await Collection.Find(filter).AnyAsync(ct);
    }

    /// <summary>Đếm tổng số users trong org (bao gồm cả inactive). Use case: Dashboard, statistics.</summary>
    public async Task<long> CountAllAsync(CancellationToken ct = default) =>
        await Collection.CountDocumentsAsync(OrgFilter, cancellationToken: ct);

    public async Task<long> CountByDepartmentAsync(
        string departmentId,
        CancellationToken ct = default
    )
    {
        var filter = OrgFilter & Builders<User>.Filter.Eq(x => x.departmentId, departmentId);
        return await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task SetLastLoginAsync(string id, DateTime loginAt, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.id, id);
        var update = Builders<User>
            .Update.Set(x => x.lastLoginAt, loginAt)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    // ─── WRITE ───────────────────────────────────────────────────────────────

    /// <summary>Thêm user mới. Email tự động lowercase. OrganizationId tự động gán từ JWT.</summary>
    public new async Task InsertAsync(User user, CancellationToken ct = default)
    {
        user.email = user.email.ToLowerInvariant();
        await base.InsertAsync(user, ct);
    }

    /// <summary>Cập nhật user theo ID. Filter: organizationId + id.</summary>
    public new async Task UpdateAsync(
        string id,
        UpdateDefinition<User> update,
        CancellationToken ct = default
    )
    {
        var filter = OrgFilter & Builders<User>.Filter.Eq(x => x.id, id);
        await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>Activate/Deactivate user. Use case: Admin vô hiệu hóa tài khoản nhân viên nghỉ việc.</summary>
    public async Task SetActiveStatusAsync(string id, bool isActive, CancellationToken ct = default)
    {
        var filter = OrgFilter & Builders<User>.Filter.Eq(x => x.id, id);
        var update = Builders<User>
            .Update.Set(x => x.isActive, isActive)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    // ─── INDEXES ─────────────────────────────────────────────────────────────

    /// <summary>Tìm user theo reset token. KHÔNG filter theo org — token đã đủ unique.</summary>
    public async Task<User?> FindByResetTokenAsync(string token, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.passwordResetToken, token);
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    /// <summary>Lưu reset token + expiry vào user.</summary>
    public async Task SetResetTokenAsync(
        string id,
        string token,
        DateTime expiry,
        CancellationToken ct = default
    )
    {
        var filter = Builders<User>.Filter.Eq(x => x.id, id);
        var update = Builders<User>
            .Update.Set(x => x.passwordResetToken, token)
            .Set(x => x.passwordResetExpiry, expiry)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>Xóa reset token sau khi dùng xong.</summary>
    public async Task ClearResetTokenAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.id, id);
        var update = Builders<User>
            .Update.Unset(x => x.passwordResetToken)
            .Unset(x => x.passwordResetExpiry)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Tạo indexes để tối ưu performance. Gọi khi app khởi động.
    /// - org_employee_code_unique: Unique (organizationId + employeeCode)
    /// - org_email_unique: Unique (organizationId + email) → Login, validate
    /// - org_role: Filter theo role
    /// - org_department: Filter theo phòng ban
    /// - org_active: Filter theo trạng thái active/inactive
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<User>(
                Builders<User>
                    .IndexKeys.Ascending(x => x.organizationId)
                    .Ascending(x => x.employeeCode),
                new CreateIndexOptions { Unique = true, Name = "org_employee_code_unique" }
            ),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.email),
                new CreateIndexOptions { Unique = true, Name = "org_email_unique" }
            ),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.role),
                new CreateIndexOptions { Name = "org_role" }
            ),
            new CreateIndexModel<User>(
                Builders<User>
                    .IndexKeys.Ascending(x => x.organizationId)
                    .Ascending(x => x.departmentId),
                new CreateIndexOptions { Name = "org_department" }
            ),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.organizationId).Ascending(x => x.teamId),
                new CreateIndexOptions { Name = "org_team" }
            ),
            new CreateIndexModel<User>(
                Builders<User>
                    .IndexKeys.Ascending(x => x.organizationId)
                    .Ascending(x => x.isActive),
                new CreateIndexOptions { Name = "org_active" }
            ),
        };

        await Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
