using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class Shipper
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("license_plate")]
    public string LicensePlate { get; set; } = string.Empty;

    [BsonElement("vehicle_info")]
    public string VehicleInfo { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "pending"; // pending, active, rejected

    [BsonElement("is_online")]
    public bool IsOnline { get; set; } = false;

    [BsonElement("current_location")]
    public GeoPoint CurrentLocation { get; set; } = new();

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
