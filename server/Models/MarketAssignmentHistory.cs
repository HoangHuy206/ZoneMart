using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

/// <summary>
/// Thực thể Lịch Sử Phân Công Quản Lý Chợ (Audit Trail / Assignment History)
/// Đáp ứng quy tắc 3: "Phân công quản lý phải được lưu lịch sử"
/// </summary>
[BsonIgnoreExtraElements]
public class MarketAssignmentHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("decision_number")]
    public string DecisionNumber { get; set; } = string.Empty; // Số quyết định phân công của Xã/Phường

    [BsonElement("market_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MarketId { get; set; } = string.Empty;

    [BsonElement("market_name")]
    public string MarketName { get; set; } = string.Empty; // VD: Chợ A, Chợ B, Chợ C

    [BsonElement("management_board_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ManagementBoardId { get; set; } = string.Empty;

    [BsonElement("board_name")]
    public string BoardName { get; set; } = string.Empty; // VD: Ban quản lý chợ số 01

    [BsonElement("assigned_by")]
    public string AssignedBy { get; set; } = "UBND Xã / Phường"; // Cơ quan phân công / Admin

    [BsonElement("assigned_date")]
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("end_date")]
    public DateTime? EndDate { get; set; } // Ngày bàn giao/chuyển giao ban quản lý khác (nếu có)

    [BsonElement("status")]
    public string Status { get; set; } = "Active"; // Active (Đang phụ trách), Transferred (Đã bàn giao), Revoked (Thu hồi)

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty; // Ghi chú nhiệm vụ, kế hoạch chuyển đổi số

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

