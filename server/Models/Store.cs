using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class Store
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("store_name")]
    public string StoreName { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("location")]
    public GeoPoint Location { get; set; } = new();

    [BsonElement("cccd_number")]
    public string CccdNumber { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "pending"; // pending, active, rejected

    [BsonElement("store_type")]
    public string StoreType { get; set; } = "all_day"; // all_day, time_slot

    [BsonElement("open_hours")]
    public string OpenHours { get; set; } = "07:00-22:00";

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
