using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class AdminNotification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "seller"; // "seller" | "shipper" | "kyc" | "system"

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("target_user_id")]
    public string TargetUserId { get; set; } = string.Empty;

    [BsonElement("target_name")]
    public string TargetName { get; set; } = string.Empty;

    [BsonElement("is_read")]
    public bool IsRead { get; set; } = false;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

