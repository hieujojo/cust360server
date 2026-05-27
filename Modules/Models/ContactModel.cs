using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CRM.Api.Modules.Models;

/// <summary>Người liên hệ — embedded document trong Customer.contacts[].</summary>
[BsonIgnoreExtraElements]
public sealed class Contact
{
    /// <summary>ID dạng ObjectId string, sinh tự động khi tạo.</summary>
    
    public string id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string name { get; set; } = string.Empty;

    /// <summary>Chức vụ / vai trò của người liên hệ.</summary>
    public string? role { get; set; }

    public string? email { get; set; }
    public string? phone { get; set; }

    /// <summary>Chỉ có tối đa 1 contact isPrimary = true per customer.</summary>
    public bool isPrimary { get; set; } = false;

    public DateTime createdAt { get; set; } = DateTime.UtcNow;
}
