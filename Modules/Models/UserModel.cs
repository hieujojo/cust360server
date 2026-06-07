using CRM.Api.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

/// <summary>Nhân viên nội bộ của tổ chức. Collection: users.</summary>
[BsonIgnoreExtraElements]
public sealed class User : IOrganizationDocument, ISoftDeletable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string organizationId { get; set; } = string.Empty;

    /// <summary>Unique trong org.</summary>
    public string email { get; set; } = string.Empty;

    /// <summary>BCrypt hash — không trả về client.</summary>
    public string password { get; set; } = string.Empty;

    /// <summary>Auto-generated. Format: NV-YYYY-NNNN.</summary>
    public string employeeCode { get; set; } = string.Empty;

    /// <summary>1 = Owner | 2 = Admin | 3 = User</summary>
    public int role { get; set; }

    /// <summary>Bắt buộc với role = 3.</summary>
    public string? departmentId { get; set; }

    /// <summary>Team thuộc phòng ban. Tùy chọn với mọi role.</summary>
    public string? teamId { get; set; }

    public string displayName { get; set; } = string.Empty;
    public string? phone { get; set; }
    public string? avatarUrl { get; set; }

    /// <summary>false → token bị reject tại middleware.</summary>
    public bool isActive { get; set; } = true;

    /// <summary>Null khi user chưa đăng nhập lần nào — dùng cho trạng thái Pending.</summary>
    public DateTime? lastLoginAt { get; set; }

    /// <summary>Token reset mật khẩu (JWT ngắn hạn). Null khi không có yêu cầu reset.</summary>
    public string? passwordResetToken { get; set; }

    /// <summary>Thời điểm hết hạn của reset token. Null khi không có yêu cầu reset.</summary>
    public DateTime? passwordResetExpiry { get; set; }

    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;
    public bool isDeleted { get; set; } = false;

    public string? createdBy { get; set; }

    // Bridge camelCase fields → IOrganizationDocument interface (PascalCase)
    string IOrganizationDocument.Id
    {
        get => id;
        set => id = value;
    }

    string IOrganizationDocument.OrganizationId
    {
        get => organizationId;
        set => organizationId = value;
    }

    bool ISoftDeletable.IsDeleted
    {
        get => isDeleted;
        set => isDeleted = value;
    }
}
