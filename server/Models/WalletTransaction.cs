using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class WalletTransaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("sub_order_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SubOrderId { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; } = 0;

    [BsonElement("type")]
    public string Type { get; set; } = "deposit"; // deposit, payment, refund, withdraw, earning

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
