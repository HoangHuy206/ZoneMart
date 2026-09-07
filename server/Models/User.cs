using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("phone_email")]
    public string PhoneEmail { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("avatar_url")]
    public string AvatarUrl { get; set; } = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=150";

    [BsonElement("is_buyer")]
    public bool IsBuyer { get; set; } = true;

    [BsonElement("is_seller")]
    public bool IsSeller { get; set; } = false;

    [BsonElement("is_shipper")]
    public bool IsShipper { get; set; } = false;

    [BsonElement("is_admin")]
    public bool IsAdmin { get; set; } = false;

    [BsonElement("wallet_balance")]
    public decimal WalletBalance { get; set; } = 0;

    [BsonElement("violation_count")]
    public int ViolationCount { get; set; } = 0;

    [BsonElement("account_status")]
    public string AccountStatus { get; set; } = "active"; // active, locked_10_days, banned

    [BsonElement("lock_until")]
    public DateTime? LockUntil { get; set; }

    [BsonElement("location")]
    public GeoPoint Location { get; set; } = new();

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
