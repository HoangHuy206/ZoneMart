using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Settings;

namespace ZoneMart.Server.Services;

public class MongoDbService
{
    private IMongoDatabase _database;

    public MongoDbService(IOptions<MongoDBSettings> settings)
    {
        string connStr = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(connStr))
        {
            connStr = settings.Value?.ConnectionString?.Trim() ?? "";
        }

        if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("<username>") || connStr.Contains("your_mongodb") || connStr.Contains("YOUR_MONGODB"))
        {
            connStr = ResolveMongoConnFromEnvFile();
        }

        if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("<username>") || connStr.Contains("YOUR_MONGODB"))
        {
            Console.WriteLine("⚠️ [MongoDbService] Chưa cấu hình MongoDB Connection String. Vui lòng thiết lập biến môi trường MONGODB_CONNECTION_STRING hoặc trong .env!");
            // Khởi tạo một client in-memory mock hoặc fallback localhost để không gây crash ứng dụng khi khởi động
            connStr = "mongodb://localhost:27017";
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
            Console.WriteLine($"⚠️ [MongoDbService] Lỗi phân giải cú pháp ConnectionString ({ex.Message}). Sử dụng kết nối mặc định...");
            clientSettings = new MongoClientSettings
            {
                Server = new MongoServerAddress("localhost", 27017),
                ServerSelectionTimeout = TimeSpan.FromSeconds(3),
                ConnectTimeout = TimeSpan.FromSeconds(3)
            };
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
            Console.WriteLine($"⚠️ [MongoDbService] Ping MongoDB không thành công ({pingEx.Message}). Hệ thống vẫn tiếp tục với bộ đệm sẵn sàng.");
        }
    }

    private static string ResolveMongoConnFromEnvFile()
    {
        try
        {
            var searchDirs = new[]
            {
                Directory.GetCurrentDirectory(),
                AppDomain.CurrentDomain.BaseDirectory,
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../..")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../..")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../.."))
            };

            foreach (var dir in searchDirs)
            {
                var envPath = Path.Combine(dir, ".env");
                if (File.Exists(envPath))
                {
                    var lines = File.ReadAllLines(envPath);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;
                        var parts = trimmed.Split('=', 2);
                        var key = parts[0].Trim();
                        var val = parts[1].Trim().Trim('"', '\'');
                        if (key == "MONGODB_CONNECTION_STRING" && !string.IsNullOrWhiteSpace(val))
                        {
                            return val;
                        }
                    }
                }
            }
        }
        catch { }
        return "";
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
