using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

[BsonIgnoreExtraElements]
public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("store_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("store_name")]
    public string? StoreName { get; set; }

    [BsonElement("seller_email")]
    public string? SellerEmail { get; set; }

    [BsonElement("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = "Thực phẩm & Tiêu dùng";

    [BsonElement("item_type")]
    public string ItemType { get; set; } = "product"; // product (Sản phẩm), service (Dịch vụ)

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

    [BsonElement("batch_number")]
    public string BatchNumber { get; set; } = "LOT-HN-" + DateTime.UtcNow.ToString("yyyyMM") + "-01"; // Mã số Lô hàng

    [BsonElement("supplier_name")]
    public string SupplierName { get; set; } = "Hợp tác xã Nông nghiệp Sạch địa phương"; // Tên Nguồn cung cấp / Cơ sở cung ứng

    [BsonElement("production_date")]
    public DateTime? ProductionDate { get; set; } = DateTime.UtcNow.AddDays(-2); // Ngày sản xuất / thu hoạch

    [BsonElement("expiry_date")]
    public DateTime? ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(10); // Hạn sử dụng

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
