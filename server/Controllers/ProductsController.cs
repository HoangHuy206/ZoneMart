using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public ProductsController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    /// <summary>
    /// API Lấy danh sách sản phẩm thực từ MongoDB (Tự động Seed dữ liệu chuẩn ban đầu nếu database trống)
    /// </summary>
    private static bool _initialSeedChecked = false;

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? category = null, [FromQuery] string? search = null)
    {
        try
        {
            if (!_initialSeedChecked)
            {
                _initialSeedChecked = true;
                var count = await _mongoService.Products.CountDocumentsAsync(new BsonDocument());
                if (count < 25)
                {
                    await SeedInitialProductsAsync();
                }
            }

            var builder = Builders<Product>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                filter &= builder.Regex(p => p.Category, new BsonRegularExpression(category, "i"));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter &= builder.Or(
                    builder.Regex(p => p.ProductName, new BsonRegularExpression(search, "i")),
                    builder.Regex(p => p.Description, new BsonRegularExpression(search, "i"))
                );
            }

            Console.WriteLine($">>> [ProductsController.GetProducts] Querying Products.Find filter...");
            var products = await _mongoService.Products.Find(filter).ToListAsync();
            Console.WriteLine($">>> [ProductsController.GetProducts] Loaded {products.Count} products from MongoDB");

            // Làm giàu dữ liệu tên cửa hàng và email người bán từ bảng Stores (chỉ khi có sản phẩm thiếu)
            try
            {
                var missingStoreInfo = products.Any(p => (string.IsNullOrWhiteSpace(p.StoreName) || string.IsNullOrWhiteSpace(p.SellerEmail)) && !string.IsNullOrEmpty(p.StoreId));
                if (missingStoreInfo)
                {
                    var storeProjection = Builders<Store>.Projection
                        .Include(s => s.Id)
                        .Include(s => s.StoreName)
                        .Include(s => s.PhoneEmail);

                    var stores = await _mongoService.Stores
                        .Find(_ => true)
                        .Project<Store>(storeProjection)
                        .ToListAsync();

                    var storeDict = stores
                        .Where(s => !string.IsNullOrEmpty(s.Id))
                        .GroupBy(s => s.Id!)
                        .ToDictionary(g => g.Key, g => g.First());

                    foreach (var p in products)
                    {
                        if (!string.IsNullOrEmpty(p.StoreId) && storeDict.TryGetValue(p.StoreId, out var st))
                        {
                            if (string.IsNullOrWhiteSpace(p.StoreName))
                            {
                                p.StoreName = st.StoreName;
                            }
                            if (string.IsNullOrWhiteSpace(p.SellerEmail))
                            {
                                p.SellerEmail = st.PhoneEmail;
                            }
                        }
                    }
                }
            }
            catch {}

            return Ok(new
            {
                success = true,
                count = products.Count,
                data = products
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class SyncProductDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public string? Unit { get; set; }
        public int Stock { get; set; }
        public string? Image { get; set; }
        public string? SellerEmail { get; set; }
        public string? StoreName { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// API Đồng bộ sản phẩm từ người bán vào MongoDB
    /// </summary>
    [HttpPost("sync-product")]
    public async Task<IActionResult> SyncProduct([FromBody] SyncProductDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { success = false, message = "Tên sản phẩm không được để trống" });
            }

            // Tìm Store theo SellerEmail hoặc StoreName
            string storeId = "65f01234567890abcdef0001";
            Store? foundStore = null;
            if (!string.IsNullOrWhiteSpace(dto.SellerEmail) || !string.IsNullOrWhiteSpace(dto.StoreName))
            {
                var storeFilter = Builders<Store>.Filter.Or(
                    Builders<Store>.Filter.Eq(s => s.PhoneEmail, dto.SellerEmail),
                    Builders<Store>.Filter.Eq(s => s.StoreName, dto.StoreName)
                );
                foundStore = await _mongoService.Stores.Find(storeFilter).FirstOrDefaultAsync();
                if (foundStore != null && !string.IsNullOrEmpty(foundStore.Id))
                {
                    storeId = foundStore.Id;
                }
            }

            string resolvedStoreName = !string.IsNullOrWhiteSpace(dto.StoreName)
                ? dto.StoreName
                : (foundStore?.StoreName ?? "Gian hàng ZoneMart");
            string resolvedSellerEmail = !string.IsNullOrWhiteSpace(dto.SellerEmail)
                ? dto.SellerEmail
                : (foundStore?.PhoneEmail ?? "");

            // Kiểm tra xem sản phẩm đã tồn tại theo Id hoặc theo (tên + store) chưa
            Product? existing = null;
            if (!string.IsNullOrWhiteSpace(dto.Id) && ObjectId.TryParse(dto.Id, out _))
            {
                existing = await _mongoService.Products.Find(p => p.Id == dto.Id).FirstOrDefaultAsync();
            }

            if (existing == null)
            {
                var prodFilter = Builders<Product>.Filter.And(
                    Builders<Product>.Filter.Eq(p => p.ProductName, dto.Name),
                    Builders<Product>.Filter.Eq(p => p.StoreId, storeId)
                );
                existing = await _mongoService.Products.Find(prodFilter).FirstOrDefaultAsync();
            }

            if (existing != null)
            {
                existing.Price = dto.Price > 0 ? dto.Price : existing.Price;
                existing.Category = !string.IsNullOrWhiteSpace(dto.Category) ? dto.Category : existing.Category;
                existing.StockQuantity = dto.Stock > 0 ? dto.Stock : existing.StockQuantity;
                if (!string.IsNullOrWhiteSpace(dto.Image)) existing.ImageUrl = dto.Image;
                existing.AiStatus = dto.Status == "active" ? "approved" : (dto.Status == "rejected_need_edit" ? "rejected" : (dto.Status ?? "pending"));
                existing.StoreName = resolvedStoreName;
                existing.SellerEmail = resolvedSellerEmail;
                await _mongoService.Products.ReplaceOneAsync(p => p.Id == existing.Id, existing);
                return Ok(new { success = true, message = "Đã cập nhật sản phẩm trong MongoDB", product = existing });
            }

            // Nếu sản phẩm đã bị từ chối hoặc yêu cầu sửa/gỡ bỏ, không tự ý chèn mới vào MongoDB
            if (dto.Status == "rejected_need_edit" || dto.Status == "rejected")
            {
                return Ok(new { success = true, message = "Sản phẩm đã bị gỡ bỏ, không thêm mới vào MongoDB" });
            }

            var newProd = new Product
            {
                StoreId = storeId,
                StoreName = resolvedStoreName,
                SellerEmail = resolvedSellerEmail,
                ProductName = dto.Name,
                Category = !string.IsNullOrWhiteSpace(dto.Category) ? dto.Category : "Rau củ quả",
                Description = $"Sản phẩm {dto.Name} tươi sạch thu hoạch tự nhiên từ {resolvedStoreName}. Đã kiểm duyệt AI Vision Guard.",
                Price = dto.Price > 0 ? dto.Price : 25000,
                Weight = 0.5,
                StockQuantity = dto.Stock > 0 ? dto.Stock : 30,
                StockStatus = dto.Stock > 0 ? "in_stock" : "out_of_stock",
                ImageUrl = !string.IsNullOrWhiteSpace(dto.Image) ? dto.Image : "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
                AiStatus = dto.Status == "active" ? "approved" : "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _mongoService.Products.InsertOneAsync(newProd);
            return Ok(new { success = true, message = "Đã đồng bộ sản phẩm vào MongoDB thành công", product = newProd });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// API Xóa sản phẩm khỏi MongoDB (Xóa triệt để mọi bản ghi trùng lặp)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(string id, [FromQuery] string? name = null)
    {
        try
        {
            var decodedId = System.Uri.UnescapeDataString(id).Trim();
            var targetName = !string.IsNullOrWhiteSpace(name) ? System.Uri.UnescapeDataString(name).Trim() : decodedId;

            var filter = Builders<Product>.Filter.Or(
                Builders<Product>.Filter.Eq(p => p.Id, decodedId),
                Builders<Product>.Filter.Eq(p => p.ProductName, decodedId),
                Builders<Product>.Filter.Eq(p => p.ProductName, targetName),
                Builders<Product>.Filter.Regex(p => p.ProductName, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(targetName)}$", "i")),
                Builders<Product>.Filter.Regex(p => p.ProductName, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(decodedId)}$", "i"))
            );

            var result = await _mongoService.Products.DeleteManyAsync(filter);
            return Ok(new { success = true, deletedCount = result.DeletedCount, message = "Đã xóa sản phẩm thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    /// <summary>
    /// API Lấy danh sách các cửa hàng trong bán kính 10km
    /// </summary>
    [HttpGet("stores")]
    public async Task<IActionResult> GetStores()
    {
        try
        {
            var storeCount = await _mongoService.Stores.CountDocumentsAsync(new BsonDocument());
            if (storeCount == 0)
            {
                await SeedInitialStoresAsync();
            }

            var stores = await _mongoService.Stores.Find(_ => true).ToListAsync();
            return Ok(new
            {
                success = true,
                count = stores.Count,
                data = stores
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private async Task SeedInitialStoresAsync()
    {
        var stores = new List<Store>
        {
            new()
            {
                Id = "65f01234567890abcdef0001",
                StoreName = "ZoneMart Bách Hóa Cầu Giấy",
                Category = "Thực phẩm & Tiêu dùng",
                Address = "Số 165 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội",
                OpenHours = "06:30 - 22:30",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f01234567890abcdef0002",
                StoreName = "Siêu Thị Trái Cây Xanh VietGAP",
                Category = "Trái cây & Nông sản",
                Address = "Số 28 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
                OpenHours = "07:00 - 22:00",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f01234567890abcdef0003",
                StoreName = "Tiệm Bánh Mì Zone & Nước Ép",
                Category = "Món ăn nóng",
                Address = "Số 89 Duy Tân, Cầu Giấy, Hà Nội",
                OpenHours = "06:00 - 21:00",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var s in stores)
        {
            var existing = await _mongoService.Stores.Find(x => x.Id == s.Id).FirstOrDefaultAsync();
            if (existing == null)
            {
                await _mongoService.Stores.InsertOneAsync(s);
            }
        }
    }

    [HttpPost("sync-full-catalog")]
    public async Task<IActionResult> SyncFullCatalog()
    {
        await SeedInitialProductsAsync();
        var all = await _mongoService.Products.Find(_ => true).ToListAsync();
        return Ok(new { success = true, totalProducts = all.Count, message = "Đã đồng bộ đầy đủ toàn bộ sản phẩm sàn ZoneMart vào MongoDB Atlas!" });
    }

    private async Task SeedInitialProductsAsync()
    {
        await SeedInitialStoresAsync();
        var products = CatalogSeedData.GetCatalogProducts();

        foreach (var p in products)
        {
            var existing = await _mongoService.Products
                .Find(x => x.ProductName == p.ProductName || x.Id == p.Id)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                await _mongoService.Products.InsertOneAsync(p);
            }
        }
    }

    public class ReduceStockRequest
    {
        public List<ReduceStockItem> Items { get; set; } = new();
    }

    public class ReduceStockItem
    {
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// API Giảm tồn kho sản phẩm khi khách mua hàng thành công
    /// </summary>
    [HttpPost("reduce-stock")]
    public async Task<IActionResult> ReduceStock([FromBody] ReduceStockRequest req)
    {
        try
        {
            if (req?.Items == null || req.Items.Count == 0) return Ok(new { success = true });

            foreach (var it in req.Items)
            {
                var filterBuilder = Builders<Product>.Filter;
                var filters = new List<FilterDefinition<Product>>();

                // Chỉ thêm filter Id nếu ProductId là ObjectId 24 ký tự hợp lệ để tránh FormatException
                if (!string.IsNullOrWhiteSpace(it.ProductId) && MongoDB.Bson.ObjectId.TryParse(it.ProductId, out _))
                {
                    filters.Add(filterBuilder.Eq(p => p.Id, it.ProductId));
                }

                if (!string.IsNullOrWhiteSpace(it.ProductName))
                {
                    string cleanName = it.ProductName.Trim();
                    filters.Add(filterBuilder.Regex(p => p.ProductName, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleanName)}$", "i")));
                    filters.Add(filterBuilder.Regex(p => p.ProductName, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(cleanName), "i")));
                }

                int qty = it.Quantity > 0 ? it.Quantity : 1;
                List<Product> matchedProducts = new();

                if (filters.Count > 0)
                {
                    var filter = filterBuilder.Or(filters);
                    matchedProducts = await _mongoService.Products.Find(filter).ToListAsync();
                }

                // Fallback nếu chưa tìm thấy: tìm kiếm mờ theo tên sản phẩm
                if (matchedProducts.Count == 0 && !string.IsNullOrWhiteSpace(it.ProductName))
                {
                    string normName = it.ProductName.Trim().ToLowerInvariant();
                    var allDbProducts = await _mongoService.Products.Find(_ => true).ToListAsync();
                    matchedProducts = allDbProducts.Where(p =>
                        !string.IsNullOrWhiteSpace(p.ProductName) &&
                        (p.ProductName.Trim().ToLowerInvariant().Contains(normName) ||
                         normName.Contains(p.ProductName.Trim().ToLowerInvariant()))
                    ).ToList();
                }

                foreach (var prod in matchedProducts)
                {
                    int currentStock = prod.StockQuantity;
                    int newStock = Math.Max(0, currentStock - qty);
                    var update = Builders<Product>.Update.Set(p => p.StockQuantity, newStock);
                    await _mongoService.Products.UpdateOneAsync(filterBuilder.Eq(p => p.Id, prod.Id), update);
                    Console.WriteLine($"[ProductsController.ReduceStock] Đã trừ kho sản phẩm '{prod.ProductName}': {currentStock} -> {newStock} (SL mua: {qty})");
                }
            }

            return Ok(new { success = true, message = "Đã cập nhật giảm tồn kho sản phẩm thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

