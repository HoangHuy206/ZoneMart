using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

/// <summary>
/// Thực thể Ban Quản Lý Chợ Số (Một Ban Quản Lý có thể quản lý 1 hoặc nhiều chợ)
/// </summary>
[BsonIgnoreExtraElements]
public class ManagementBoard
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("board_code")]
    public string BoardCode { get; set; } = string.Empty; // VD: BQL_01, BQL_02

    [BsonElement("board_name")]
    public string BoardName { get; set; } = string.Empty; // VD: Ban quản lý chợ số 01, Ban quản lý chợ số 02

    [BsonElement("ward_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string WardId { get; set; } = string.Empty; // Khóa ngoại trực thuộc Xã / Phường

    [BsonElement("ward_name")]
    public string WardName { get; set; } = string.Empty;

    [BsonElement("leader_name")]
    public string LeaderName { get; set; } = string.Empty; // Trưởng ban quản lý

    [BsonElement("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("members_count")]
    public int MembersCount { get; set; } = 5; // Số lượng cán bộ trong ban

    [BsonElement("managed_market_ids")]
    public List<string> ManagedMarketIds { get; set; } = []; // Danh sách các chợ đang phụ trách (Chợ A, Chợ B...)

    [BsonElement("status")]
    public string Status { get; set; } = "Active";

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

