using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class ParentOrder
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("buyer_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string BuyerId { get; set; } = string.Empty;

    [BsonElement("total_amount")]
    public decimal TotalAmount { get; set; } = 0;

    [BsonElement("payment_method")]
    public string PaymentMethod { get; set; } = "COD"; // COD, ONLINE_QR

    [BsonElement("payment_status")]
    public string PaymentStatus { get; set; } = "unpaid"; // unpaid, paid, refunded

    [BsonElement("shipping_address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [BsonElement("delivery_coordinates")]
    public double[] DeliveryCoordinates { get; set; } = [105.8342, 21.0278];

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
