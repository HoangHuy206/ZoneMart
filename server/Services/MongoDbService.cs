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
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
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
}
