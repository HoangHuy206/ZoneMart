using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Settings;

namespace ZoneMart.Server.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IOptions<MongoDBSettings> settings)
    {
        MongoClientSettings? clientSettings = null;
        string connStr = settings.Value?.ConnectionString ?? "";

        try
        {
            clientSettings = MongoClientSettings.FromConnectionString(connStr);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDbService] Lỗi phân giải kết nối MongoDB: {ex.Message}");
            string fallbackConn = "mongodb://huyhoangzz:hH28122006...@ac-txqeg1o-shard-00-00.trwjvms.mongodb.net:27017,ac-txqeg1o-shard-00-01.trwjvms.mongodb.net:27017,ac-txqeg1o-shard-00-02.trwjvms.mongodb.net:27017/zonemart?tls=true&replicaSet=atlas-p9e9jm-shard-0&authSource=admin&readPreference=primaryPreferred&retryWrites=true&w=majority";
            try
            {
                clientSettings = MongoClientSettings.FromConnectionString(fallbackConn);
                Console.WriteLine("✅ [MongoDbService] Đã kích hoạt kết nối Fallback Direct ReplicaSet thành công.");
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"❌ [MongoDbService] Không thể phân giải fallback: {ex2.Message}");
                clientSettings = new MongoClientSettings();
            }
        }

        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(settings.Value?.DatabaseName ?? "zonemart");
    }

    public IMongoDatabase Database => _database;

    // Collections
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Store> Stores => _database.GetCollection<Store>("stores");
    public IMongoCollection<Shipper> Shippers => _database.GetCollection<Shipper>("shippers");
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("products");
    public IMongoCollection<ParentOrder> ParentOrders => _database.GetCollection<ParentOrder>("parentorders");
    public IMongoCollection<SubOrder> SubOrders => _database.GetCollection<SubOrder>("suborders");
    public IMongoCollection<OrderItem> OrderItems => _database.GetCollection<OrderItem>("orderitems");
    public IMongoCollection<WalletTransaction> WalletTransactions => _database.GetCollection<WalletTransaction>("wallettransactions");
    public IMongoCollection<SupportTicket> SupportTickets => _database.GetCollection<SupportTicket>("support_tickets");
    public IMongoCollection<UserCart> UserCarts => _database.GetCollection<UserCart>("user_carts");
    public IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>("audit_logs");
    public IMongoCollection<AdminNotification> AdminNotifications => _database.GetCollection<AdminNotification>("admin_notifications");
}
