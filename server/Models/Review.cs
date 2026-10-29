using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

[BsonIgnoreExtraElements]
public class ProductReviewItem
{
    [BsonElement("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("product_image")]
    public string ProductImage { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; } = 5;

    [BsonElement("comment")]
    public string Comment { get; set; } = string.Empty;

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();
}

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

    [BsonElement("buyer_avatar")]
    public string BuyerAvatar { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; } = 5; // 1 -> 5 sao chung

    [BsonElement("comment")]
    public string Comment { get; set; } = string.Empty;

    // 1. Phân hệ Tài xế (Shipper)
    [BsonElement("shipper_id")]
    public string ShipperId { get; set; } = string.Empty;

    [BsonElement("shipper_name")]
    public string ShipperName { get; set; } = string.Empty;

    [BsonElement("shipper_rating")]
    public int ShipperRating { get; set; } = 5;

    [BsonElement("shipper_comment")]
    public string ShipperComment { get; set; } = string.Empty;

    [BsonElement("shipper_tags")]
    public List<string> ShipperTags { get; set; } = new();

    // 2. Phân hệ Gian hàng (Store)
    [BsonElement("store_rating")]
    public int StoreRating { get; set; } = 5;

    [BsonElement("store_comment")]
    public string StoreComment { get; set; } = string.Empty;

    [BsonElement("store_tags")]
    public List<string> StoreTags { get; set; } = new();

    // 3. Phân hệ Từng Sản Phẩm (Products)
    [BsonElement("product_reviews")]
    public List<ProductReviewItem> ProductReviews { get; set; } = new();

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
