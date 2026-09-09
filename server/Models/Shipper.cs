using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class Shipper
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("user_id")]
    public string UserId { get; set; } = string.Empty;       // Liên kết ID tài khoản Buyer ban đầu

    [BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;     // Họ tên tài xế

    [BsonElement("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;  // SĐT liên hệ

    [BsonElement("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;    // Link / data ảnh chân dung

    // Giấy tờ tùy thân & Bằng lái
    [BsonElement("cccd_number")]
    public string CccdNumber { get; set; } = string.Empty;   // Số CCCD

    [BsonElement("cccd_front_image")]
    public string CccdFrontImage { get; set; } = string.Empty;

    [BsonElement("cccd_back_image")]
    public string CccdBackImage { get; set; } = string.Empty;

    [BsonElement("driving_license_image")]
    public string DrivingLicenseImage { get; set; } = string.Empty; // Ảnh bằng lái xe (GPLX)

    // Thông tin phương tiện
    [BsonElement("license_plate")]
    public string LicensePlate { get; set; } = string.Empty; // Biển số xe (để khách nhận diện)

    [BsonElement("vehicle_type")]
    public string VehicleType { get; set; } = "Xe máy xăng";   // Xe máy xăng / Xe máy điện

    [BsonElement("vehicle_model")]
    public string VehicleModel { get; set; } = string.Empty;   // Mẫu xe / Dòng xe cụ thể (Honda Wave, VinFast Feliz S, ...)

    [BsonElement("operating_area")]
    public string OperatingArea { get; set; } = "Quận Cầu Giấy"; // Khu vực chạy chính

    // Trạng thái vận hành & Vị trí GPS thời gian thực
    [BsonElement("current_latitude")]
    public double CurrentLatitude { get; set; } = 21.0285;             // Tọa độ Vĩ độ hiện tại

    [BsonElement("current_longitude")]
    public double CurrentLongitude { get; set; } = 105.8542;            // Tọa độ Kinh độ hiện tại

    [BsonElement("is_online")]
    public bool IsOnline { get; set; } = false;             // Bật/Tắt công tắc sẵn sàng nhận đơn

    [BsonElement("is_busy")]
    public bool IsBusy { get; set; } = false;               // Đang bận giao đơn khác

    // Tài khoản nhận tiền cước
    [BsonElement("bank_name")]
    public string BankName { get; set; } = string.Empty;

    [BsonElement("bank_account_number")]
    public string BankAccountNumber { get; set; } = string.Empty;

    // Trạng thái duyệt của Ban Quản Lý
    [BsonElement("status")]
    public string Status { get; set; } = "Pending";         // "Pending" -> "Approved" -> "Rejected"

    [BsonElement("reject_reason")]
    public string? RejectReason { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
