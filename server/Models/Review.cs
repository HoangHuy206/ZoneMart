using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

[BsonIgnoreExtraElements]
public class Review
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("sub_order_id")]
    public string SubOrderId { get; set; } = string.Empty;

    [BsonElement("store_id")]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("store_name")]
    public string StoreName { get; set; } = string.Empty;

    [BsonElement("buyer_id")]
    public string BuyerId { get; set; } = string.Empty;

    [BsonElement("buyer_name")]
    public string BuyerName { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; } = 5; // 1 -> 5 sao

    [BsonElement("comment")]
    public string Comment { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

