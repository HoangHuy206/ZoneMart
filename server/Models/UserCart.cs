using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

[BsonIgnoreExtraElements]
public class UserCart
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("user_id")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("stores")]
    public List<CartStoreDto> Stores { get; set; } = [];

    [BsonElement("voucher_code")]
    public string? VoucherCode { get; set; } = "FREESHIP10K";

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CartStoreDto
{
    [BsonElement("store_id")]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("store_name")]
    public string StoreName { get; set; } = string.Empty;

    [BsonElement("distance_km")]
    public double DistanceKm { get; set; } = 1.2;

    [BsonElement("delivery_time")]
    public string DeliveryTime { get; set; } = "15 - 20 phút";

    [BsonElement("note")]
    public string? Note { get; set; }

    [BsonElement("items")]
    public List<CartStoreItemDto> Items { get; set; } = [];
}

public class CartStoreItemDto
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; } = 0;

    [BsonElement("original_price")]
    public decimal? OriginalPrice { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("image")]
    public string Image { get; set; } = string.Empty;

    [BsonElement("unit")]
    public string? Unit { get; set; }

    [BsonElement("selected")]
    public bool Selected { get; set; } = true;
}
