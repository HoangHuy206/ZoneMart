using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class OrderItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("sub_order_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubOrderId { get; set; } = string.Empty;

    [BsonElement("product_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("price_at_time")]
    public decimal PriceAtTime { get; set; } = 0;
}
