using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class Store
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("user_id")]
    public string UserId { get; set; } = string.Empty; // Khóa ngoại liên kết tài khoản

    [BsonElement("store_name")]
    public string StoreName { get; set; } = string.Empty; // Tên tiệm

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // Danh mục chính

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty; // Địa chỉ cửa hàng

    [BsonElement("open_hours")]
    public string OpenHours { get; set; } = "07:00 - 22:00"; // Khung giờ mở cửa (vd: 7h đến 22h)

    [BsonElement("location")]
    public GeoPoint Location { get; set; } = new();

    [BsonElement("owner_full_name")]
    public string OwnerFullName { get; set; } = string.Empty; // Họ tên chủ tiệm

    [BsonElement("cccd_number")]
    public string CccdNumber { get; set; } = string.Empty; // Số CCCD

    [BsonElement("cccd_front_image")]
    public string CccdFrontImage { get; set; } = string.Empty; // Link / data ảnh CCCD mặt trước

    [BsonElement("cccd_back_image")]
    public string CccdBackImage { get; set; } = string.Empty; // Link / data ảnh CCCD mặt sau

    [BsonElement("food_safety_cert_image")]
    public string FoodSafetyCertImage { get; set; } = string.Empty; // Ảnh giấy chứng nhận an toàn thực phẩm

    [BsonElement("bank_name")]
    public string BankName { get; set; } = string.Empty; // Tên ngân hàng

    [BsonElement("bank_account_number")]
    public string BankAccountNumber { get; set; } = string.Empty; // Số tài khoản nhận tiền

    [BsonElement("status")]
    public string Status { get; set; } = "Pending"; // Pending, Active, Rejected

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
