using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("actor_email")]
    public string ActorEmail { get; set; } = string.Empty; // Email của Admin hoặc Quản lý thực hiện thao tác

    [BsonElement("actor_role")]
    public string ActorRole { get; set; } = "Manager"; // "Admin" hoặc "Manager"

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // "Duyệt KYC", "Từ chối KYC", "Cảnh báo", "Tạm khóa 10 ngày", "Cấm vĩnh viễn", "Mở khóa", "Xóa tài khoản", "Tạo Quản lý"

    [BsonElement("target_user_email")]
    public string TargetUserEmail { get; set; } = string.Empty; // Email tài khoản bị tác động

    [BsonElement("details")]
    public string Details { get; set; } = string.Empty; // Chi tiết lý do hoặc ghi chú

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
