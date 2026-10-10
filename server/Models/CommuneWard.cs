using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

/// <summary>
/// Thực thể Xã / Phường (Đơn vị hành chính cấp cơ sở quản lý chợ địa phương)
/// </summary>
[BsonIgnoreExtraElements]
public class CommuneWard
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("ward_code")]
    public string WardCode { get; set; } = string.Empty; // VD: WARD_DVH

    [BsonElement("ward_name")]
    public string WardName { get; set; } = string.Empty; // VD: Phường Dịch Vọng Hậu

    [BsonElement("district_name")]
    public string DistrictName { get; set; } = string.Empty; // VD: Quận Cầu Giấy

    [BsonElement("city_name")]
    public string CityName { get; set; } = "Hà Nội";

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

