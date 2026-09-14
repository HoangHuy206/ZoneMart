using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public CartController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    /// <summary>
    /// API Lấy giỏ hàng của người dùng từ Database MongoDB Atlas
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserCart(string userId)
    {
        try
        {
            var cart = await _mongoService.UserCarts
                .Find(c => c.UserId == userId)
                .FirstOrDefaultAsync();

            // Nếu người dùng chưa có giỏ hàng trong Database, khởi tạo giỏ hàng từ sản phẩm thật của MongoDB
            if (cart == null || cart.Stores.Count == 0)
            {
                cart = await GenerateDefaultCartFromRealProductsAsync(userId);
            }

            return Ok(new
            {
                success = true,
                data = cart
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy giỏ hàng từ Database: " + ex.Message });
        }
    }

    /// <summary>
    /// API Lưu & Đồng bộ giỏ hàng của người dùng vào Database MongoDB Atlas
    /// </summary>
    [HttpPost("{userId}")]
    public async Task<IActionResult> SaveUserCart(string userId, [FromBody] SaveCartDto dto)
    {
        try
        {
            var filter = Builders<UserCart>.Filter.Eq(c => c.UserId, userId);
            var update = Builders<UserCart>.Update
                .Set(c => c.Stores, dto.Stores)
                .Set(c => c.VoucherCode, dto.VoucherCode)
                .Set(c => c.UpdatedAt, DateTime.UtcNow)
                .SetOnInsert(c => c.Id, ObjectId.GenerateNewId().ToString())
                .SetOnInsert(c => c.UserId, userId);

            await _mongoService.UserCarts.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true }
            );

            return Ok(new
            {
                success = true,
                message = "Đã đồng bộ giỏ hàng vào Database MongoDB Atlas thành công!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi lưu giỏ hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// API Lấy danh sách sản phẩm gợi ý mua kèm (Cross-sell) từ Database thật
    /// </summary>
    [HttpGet("suggested")]
    public async Task<IActionResult> GetSuggestedProducts()
    {
        try
        {
            var products = await _mongoService.Products
                .Find(_ => true)
                .Limit(8)
                .ToListAsync();

            var stores = await _mongoService.Stores
                .Find(_ => true)
                .ToListAsync();

            var storeMap = stores.ToDictionary(s => s.Id ?? string.Empty, s => s);

            var result = products.Select(p =>
            {
                string sName = "ZoneMart Bách Hóa Cầu Giấy";
                double distance = 1.2;
                if (!string.IsNullOrEmpty(p.StoreId) && storeMap.TryGetValue(p.StoreId, out var st))
                {
                    sName = st.StoreName;
                }

                return new
                {
                    id = p.Id,
                    name = p.ProductName,
                    price = p.Price,
                    originalPrice = p.Price * 1.2m,
                    image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
                    unit = p.Category.Contains("Thịt") ? "Khay 500g" : (p.Category.Contains("Nông sản") ? "Túi 1kg" : "Phần"),
                    storeId = p.StoreId,
                    storeName = sName,
                    distanceKm = distance,
                    deliveryTime = "15 - 20 phút"
                };
            }).ToList();

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa sạch giỏ hàng trong Database
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> ClearUserCart(string userId)
    {
        try
        {
            await _mongoService.UserCarts.DeleteOneAsync(c => c.UserId == userId);
            return Ok(new { success = true, message = "Đã xóa sạch giỏ hàng trong Database!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // Helper tạo giỏ hàng ban đầu từ các sản phẩm thật trong collection 'products' và 'stores'
    private async Task<UserCart> GenerateDefaultCartFromRealProductsAsync(string userId)
    {
        var products = await _mongoService.Products.Find(_ => true).ToListAsync();
        var stores = await _mongoService.Stores.Find(_ => true).ToListAsync();

        var cart = new UserCart
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            VoucherCode = "FREESHIP10K",
            UpdatedAt = DateTime.UtcNow
        };

        if (products.Count >= 2 && stores.Count >= 2)
        {
            // Quán 1
            var store1 = stores[0];
            var prod1 = products[0]; // Thịt bò
            var prod2 = products.Count > 1 ? products[1] : products[0]; // Gạo ST25

            var group1 = new CartStoreDto
            {
                StoreId = store1.Id ?? "st_1",
                StoreName = store1.StoreName,
                DistanceKm = 1.2,
                DeliveryTime = "15 - 20 phút",
                Note = "Chọn khay thịt tươi mới về sáng nay giúp em nhé!",
                Items = [
                    new()
                    {
                        Id = prod1.Id ?? "p1",
                        Name = prod1.ProductName,
                        Price = prod1.Price,
                        OriginalPrice = prod1.Price * 1.2m,
                        Quantity = 2,
                        Unit = "Khay 500g",
                        Image = prod1.ImageUrl,
                        Selected = true
                    },
                    new()
                    {
                        Id = prod2.Id ?? "p6",
                        Name = prod2.ProductName,
                        Price = prod2.Price,
                        OriginalPrice = prod2.Price * 1.2m,
                        Quantity = 1,
                        Unit = "Túi 5kg",
                        Image = prod2.ImageUrl,
                        Selected = true
                    }
                ]
            };

            // Quán 2
            var store2 = stores[1];
            var prod3 = products.Count > 2 ? products[2] : products[0]; // Dâu tây VietGAP

            var group2 = new CartStoreDto
            {
                StoreId = store2.Id ?? "st_2",
                StoreName = store2.StoreName,
                DistanceKm = 2.5,
                DeliveryTime = "20 - 25 phút",
                Note = "Lấy dâu tây quả to mọng nhé tiệm.",
                Items = [
                    new()
                    {
                        Id = prod3.Id ?? "p2",
                        Name = prod3.ProductName,
                        Price = prod3.Price,
                        OriginalPrice = prod3.Price * 1.25m,
                        Quantity = 1,
                        Unit = "Hộp 500g",
                        Image = prod3.ImageUrl,
                        Selected = true
                    }
                ]
            };

            cart.Stores = [group1, group2];
        }

        // Lưu vào Database MongoDB Atlas
        try
        {
            await _mongoService.UserCarts.InsertOneAsync(cart);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [UserCart Insert Warning] {ex.Message}");
        }

        return cart;
    }

    public class SaveCartDto
    {
        public List<CartStoreDto> Stores { get; set; } = [];
        public string? VoucherCode { get; set; }
    }
}
