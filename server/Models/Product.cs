using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("store_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = "Thực phẩm & Tiêu dùng";

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; } = 0;

    [BsonElement("weight")]
    public double Weight { get; set; } = 0.5; // kg

    [BsonElement("stock_quantity")]
    public int StockQuantity { get; set; } = 100;

    [BsonElement("ai_status")]
    public string AiStatus { get; set; } = "pending"; // pending, approved, rejected, suspicious

    [BsonElement("ai_check_note")]
    public string AiCheckNote { get; set; } = string.Empty;

    [BsonElement("stock_status")]
    public string StockStatus { get; set; } = "in_stock"; // in_stock, out_of_stock

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
