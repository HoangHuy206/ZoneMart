using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

/// <summary>
/// Thực thể Chợ địa phương (Một Xã/Phường có thể có nhiều chợ)
/// </summary>
[BsonIgnoreExtraElements]
public class Market
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("market_code")]
    public string MarketCode { get; set; } = string.Empty; // VD: MKT_A, MKT_B, MKT_C

    [BsonElement("market_name")]
    public string MarketName { get; set; } = string.Empty; // VD: Chợ A, Chợ B, Chợ C

    [BsonElement("ward_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string WardId { get; set; } = string.Empty; // Khóa ngoại liên kết Xã / Phường

    [BsonElement("ward_name")]
    public string WardName { get; set; } = string.Empty;

    [BsonElement("district_name")]
    public string DistrictName { get; set; } = string.Empty;

    [BsonElement("management_board_id")]
    public string? ManagementBoardId { get; set; } // Ban quản lý hiện tại phụ trách

    [BsonElement("management_board_name")]
    public string? ManagementBoardName { get; set; }

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("market_type")]
    public string MarketType { get; set; } = "Chợ dân sinh truyền thống";

    [BsonElement("total_stalls")]
    public int TotalStalls { get; set; } = 50; // Tổng số gian hàng / sạp

    [BsonElement("status")]
    public string Status { get; set; } = "Active"; // Active, Upgrading, Inactive

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

