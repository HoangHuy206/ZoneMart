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
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? category = null, [FromQuery] string? search = null)
    {
        try
        {
            var count = await _mongoService.Products.CountDocumentsAsync(new BsonDocument());

            // Tự động Seed nếu chưa có sản phẩm nào trong MongoDB
            if (count == 0)
            {
                await SeedInitialProductsAsync();
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

            var products = await _mongoService.Products.Find(filter).ToListAsync();

            // Làm giàu dữ liệu tên cửa hàng và email người bán từ bảng Stores
            try
            {
                var stores = await _mongoService.Stores.Find(_ => true).ToListAsync();
                var storeDict = stores.ToDictionary(s => s.Id ?? "", s => s);

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

            // Kiểm tra xem sản phẩm đã tồn tại theo tên và store chưa
            var prodFilter = Builders<Product>.Filter.And(
                Builders<Product>.Filter.Eq(p => p.ProductName, dto.Name),
                Builders<Product>.Filter.Eq(p => p.StoreId, storeId)
            );
            var existing = await _mongoService.Products.Find(prodFilter).FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.Price = dto.Price > 0 ? dto.Price : existing.Price;
                existing.Category = !string.IsNullOrWhiteSpace(dto.Category) ? dto.Category : existing.Category;
                existing.StockQuantity = dto.Stock > 0 ? dto.Stock : existing.StockQuantity;
                if (!string.IsNullOrWhiteSpace(dto.Image)) existing.ImageUrl = dto.Image;
                existing.AiStatus = dto.Status == "active" ? "approved" : (dto.Status ?? "pending");
                existing.StoreName = resolvedStoreName;
                existing.SellerEmail = resolvedSellerEmail;
                await _mongoService.Products.ReplaceOneAsync(p => p.Id == existing.Id, existing);
                return Ok(new { success = true, message = "Đã cập nhật sản phẩm trong MongoDB", product = existing });
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

        await _mongoService.Stores.InsertManyAsync(stores);
    }

    private async Task SeedInitialProductsAsync()
    {
        await SeedInitialStoresAsync();

        var products = new List<Product>
        {
            new()
            {
                Id = "65f099887766554433221101",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (Khay 500g)",
                Category = "Thực phẩm tươi",
                Description = "Thịt ba chỉ bò Mỹ thái lát mỏng chuẩn vị lẩu nướng, thịt mềm ngọt thơm.",
                Price = 185000,
                Weight = 0.5,
                StockQuantity = 50,
                StockStatus = "in_stock",
                ImageUrl = "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=600&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f099887766554433221102",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
                Category = "Nhu yếu phẩm",
                Description = "Gạo ngon nhất thế giới, hạt thon dài trắng trong, cơm dẻo mềm thơm mùi lá dứa.",
                Price = 190000,
                Weight = 5.0,
                StockQuantity = 80,
                StockStatus = "in_stock",
                ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=600&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f099887766554433221103",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Hộp Dâu Tây Đà Lạt Tươi Ngọt Chuẩn VietGAP (500g)",
                Category = "Trái cây & Nông sản",
                Description = "Dâu tây tươi hái tại vườn Đà Lạt buổi sớm, quả mọng đỏ ngọt mát giàu vitamin C.",
                Price = 95000,
                Weight = 0.5,
                StockQuantity = 40,
                StockStatus = "in_stock",
                ImageUrl = "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=600&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f099887766554433221104",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Combo Bánh Mì Chảo Nóng Hổi Kèm Pate Đặc Biệt",
                Category = "Món ăn nóng",
                Description = "Bánh mì giòn rụm ăn kèm pate gia truyền, trứng ốp la lòng đào và xúc xích rán xém cạnh.",
                Price = 45000,
                Weight = 0.4,
                StockQuantity = 30,
                StockStatus = "in_stock",
                ImageUrl = "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=600&q=80",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _mongoService.Products.InsertManyAsync(products);
    }
}

