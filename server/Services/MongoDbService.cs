using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Settings;

namespace ZoneMart.Server.Services;

public class MongoDbService
{
    private IMongoDatabase _database;

    private const string FallbackReplicaSetConn = "mongodb://huyhoangzz:hH28122006...@ac-txqeg1o-shard-00-00.trwjvms.mongodb.net:27017,ac-txqeg1o-shard-00-01.trwjvms.mongodb.net:27017,ac-txqeg1o-shard-00-02.trwjvms.mongodb.net:27017/zonemart?tls=true&replicaSet=atlas-p9e9jm-shard-0&authSource=admin&readPreference=primaryPreferred&retryWrites=true&w=majority";

    public MongoDbService(IOptions<MongoDBSettings> settings)
    {
        string connStr = settings.Value?.ConnectionString?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("<username>") || connStr.Contains("your_mongodb"))
        {
            connStr = "mongodb+srv://huyhoangzz:hH28122006...@cluster0.trwjvms.mongodb.net/zonemart?retryWrites=true&w=majority&appName=Cluster0";
        }

        MongoClientSettings? clientSettings = null;
        try
        {
            clientSettings = MongoClientSettings.FromConnectionString(connStr);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(7);
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(5);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDbService] Lỗi phân giải cú pháp ConnectionString ({ex.Message}). Tự động kích hoạt Fallback Direct ReplicaSet...");
            clientSettings = MongoClientSettings.FromConnectionString(FallbackReplicaSetConn);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(7);
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(5);
        }

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(settings.Value?.DatabaseName ?? "zonemart");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cts.Token);
            Console.WriteLine("🍃 [MongoDbService] Đã kết nối thành công đến MongoDB Atlas (zonemart)!");
        }
        catch (Exception pingEx)
        {
            Console.WriteLine($"⚠️ [MongoDbService] Kết nối chính thất bại ({pingEx.Message}). Tự động kích hoạt kết nối Fallback Direct ReplicaSet...");
            try
            {
                var fbSettings = MongoClientSettings.FromConnectionString(FallbackReplicaSetConn);
                fbSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(7);
                fbSettings.ConnectTimeout = TimeSpan.FromSeconds(5);
                var fbClient = new MongoClient(fbSettings);
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var fbDb = fbClient.GetDatabase(settings.Value?.DatabaseName ?? "zonemart");
                fbDb.RunCommand<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cts2.Token);
                _database = fbDb;
                Console.WriteLine("✅ [MongoDbService] Kết nối Fallback Direct ReplicaSet đã hoạt động hoàn hảo!");
            }
            catch (Exception fbEx)
            {
                Console.WriteLine($"❌ [MongoDbService] Fallback cũng gặp sự cố: {fbEx.Message}");
            }
        }
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
    public IMongoCollection<Review> Reviews => _database.GetCollection<Review>("reviews");
    public IMongoCollection<CommuneWard> CommuneWards => _database.GetCollection<CommuneWard>("commune_wards");
    public IMongoCollection<Market> Markets => _database.GetCollection<Market>("markets");
    public IMongoCollection<ManagementBoard> ManagementBoards => _database.GetCollection<ManagementBoard>("management_boards");
    public IMongoCollection<MarketAssignmentHistory> MarketAssignmentHistories => _database.GetCollection<MarketAssignmentHistory>("market_assignment_histories");
    public IMongoCollection<ShiftManager> ShiftManagers => _database.GetCollection<ShiftManager>("shift_managers");
}
