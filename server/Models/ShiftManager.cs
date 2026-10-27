using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

/// <summary>
/// Thực thể Quản Lý Ca Trực lưu tại collection riêng "shift_managers" trong CSDL MongoDB
/// Định dạng mã ID: QL-001, QL-002, QL-003,...
/// </summary>
public class ShiftManager
{
    [BsonId]
    public string Id { get; set; } = string.Empty; // "QL-001", "QL-002",...

    [BsonElement("manager_code")]
    public string ManagerCode { get; set; } = string.Empty; // "QL-001"

    [BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = "Manager";

    [BsonElement("status")]
    public string Status { get; set; } = "Active";

    [BsonElement("assigned_markets_count")]
    public int AssignedMarketsCount { get; set; } = 0;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("is_deleted")]
    public bool IsDeleted { get; set; } = false;
}
