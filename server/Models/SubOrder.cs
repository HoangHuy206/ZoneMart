using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class SubOrder
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("parent_order_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ParentOrderId { get; set; } = string.Empty;

    [BsonElement("store_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("shipper_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ShipperId { get; set; }

    [BsonElement("delivery_type")]
    public string DeliveryType { get; set; } = "standard"; // standard, express

    [BsonElement("shipping_fee")]
    public decimal ShippingFee { get; set; } = 15000;

    [BsonElement("distance_km")]
    public double DistanceKm { get; set; } = 0;

    [BsonElement("status")]
    public string Status { get; set; } = "pending"; 
    // pending, seller_accepted, finding_shipper, picking, delivering, completed, cancelled, boom_dispute, returned

    [BsonElement("cancel_reason")]
    public string? CancelReason { get; set; }

    [BsonElement("violation_level")]
    public string ViolationLevel { get; set; } = "none"; // none, light, severe

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
