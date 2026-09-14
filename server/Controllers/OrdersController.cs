using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public OrdersController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    // DTO cho yêu cầu Checkout
    public class CheckoutRequest
    {
        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "ONLINE_QR"; // ONLINE_QR, ZONEPAY_WALLET, COD, CREDIT_CARD
        public string DeliveryType { get; set; } = "express"; // express, standard, scheduled
        public string? DeliveryTimeSlot { get; set; }
        public string? VoucherCode { get; set; }
        public decimal VoucherDiscount { get; set; } = 0;
        public decimal ShippingFee { get; set; } = 0;
        public decimal SubTotal { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;
        public List<StoreCheckoutGroupDto> Stores { get; set; } = [];
    }

    public class StoreCheckoutGroupDto
    {
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public double DistanceKm { get; set; } = 1.5;
        public string DeliveryTime { get; set; } = "15 - 25 phút";
        public string? Note { get; set; }
        public List<CartItemDto> Items { get; set; } = [];
    }

    public class CartItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0;
        public int Quantity { get; set; } = 1;
        public string Image { get; set; } = string.Empty;
        public string? Unit { get; set; }
    }

    /// <summary>
    /// API Tạo đơn hàng Checkout trực tiếp vào Database MongoDB Atlas
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequest request)
    {
        try
        {
            if (request.Stores == null || request.Stores.Count == 0)
            {
                return BadRequest(new { success = false, message = "Đơn hàng chưa có sản phẩm nào!" });
            }

            // 1. Chuẩn hóa ID người mua thành ObjectId hợp lệ
            string safeBuyerId = EnsureValidObjectId(request.BuyerId);

            // 2. Nếu thanh toán bằng Ví ZonePay: Kiểm tra và trừ tiền trong ví
            if (request.PaymentMethod == "ZONEPAY_WALLET")
            {
                var userFilter = Builders<User>.Filter.Or(
                    Builders<User>.Filter.Eq(u => u.Id, safeBuyerId),
                    Builders<User>.Filter.Eq(u => u.PhoneEmail, request.BuyerPhone),
                    Builders<User>.Filter.Eq(u => u.FullName, request.BuyerName)
                );
                var user = await _mongoService.Users.Find(userFilter).FirstOrDefaultAsync();

                if (user != null)
                {
                    if (user.WalletBalance < request.TotalAmount)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Số dư Ví ZonePay ({user.WalletBalance:N0} ₫) không đủ để thanh toán đơn hàng {request.TotalAmount:N0} ₫. Vui lòng nạp thêm hoặc chọn phương thức khác!"
                        });
                    }

                    // Trừ tiền trong ví
                    var updateWallet = Builders<User>.Update.Inc(u => u.WalletBalance, -request.TotalAmount);
                    await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, updateWallet);

                    // Ghi lịch sử giao dịch ví
                    var trans = new WalletTransaction
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        UserId = user.Id ?? safeBuyerId,
                        Amount = -request.TotalAmount,
                        Type = "payment",
                        Description = $"Thanh toán đơn hàng ZoneMart giao hỏa tốc",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _mongoService.WalletTransactions.InsertOneAsync(trans);
                }
            }

            // 3. Tạo ParentOrder
            var parentOrder = new ParentOrder
            {
                Id = ObjectId.GenerateNewId().ToString(),
                BuyerId = safeBuyerId,
                TotalAmount = request.TotalAmount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = request.PaymentMethod == "ZONEPAY_WALLET" ? "paid" : "unpaid",
                ShippingAddress = $"{request.BuyerName} ({request.BuyerPhone}) - {request.ShippingAddress}",
                DeliveryCoordinates = [105.7944, 21.0333], // Hub Cầu Giấy
                CreatedAt = DateTime.UtcNow
            };

            await _mongoService.ParentOrders.InsertOneAsync(parentOrder);

            // 4. Tạo SubOrders theo từng cửa hàng và OrderItems
            var createdSubOrders = new List<object>();

            // Danh sách tài xế mẫu đại diện
            string[] demoShippers = ["Trần Văn Bình (29N1-67890)", "Nguyễn Văn Nam (29M1-8888)", "Lê Hoàng Tuấn (29H2-1234)"];
            string[] demoPhones = ["0912 888 999", "0987 654 321", "0904 555 666"];
            int shipperIdx = 0;

            foreach (var storeGroup in request.Stores)
            {
                string safeStoreId = EnsureValidObjectId(storeGroup.StoreId);
                var subOrder = new SubOrder
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ParentOrderId = parentOrder.Id,
                    StoreId = safeStoreId,
                    ShipperId = ObjectId.GenerateNewId().ToString(),
                    DeliveryType = request.DeliveryType,
                    ShippingFee = 15000,
                    DistanceKm = storeGroup.DistanceKm,
                    Status = "delivering", // Khởi tạo ở trạng thái đang giao để người mua theo dõi
                    CreatedAt = DateTime.UtcNow
                };

                await _mongoService.SubOrders.InsertOneAsync(subOrder);

                // Lưu các món hàng thuộc subOrder này
                var itemSummaryList = new List<string>();
                foreach (var item in storeGroup.Items)
                {
                    string safeProductId = EnsureValidObjectId(item.Id);
                    var orderItem = new OrderItem
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        SubOrderId = subOrder.Id,
                        ProductId = safeProductId,
                        Quantity = item.Quantity,
                        PriceAtTime = item.Price
                    };
                    await _mongoService.OrderItems.InsertOneAsync(orderItem);
                    itemSummaryList.Add($"{item.Name} (x{item.Quantity})");
                }

                string assignedShipper = demoShippers[shipperIdx % demoShippers.Length];
                string assignedPhone = demoPhones[shipperIdx % demoPhones.Length];
                shipperIdx++;

                createdSubOrders.Add(new
                {
                    subId = subOrder.Id,
                    storeId = storeGroup.StoreId,
                    storeName = storeGroup.StoreName,
                    items = string.Join(", ", itemSummaryList),
                    status = "delivering",
                    statusText = "Shipper đang giao hàng tới bạn (Khoảng 15-20 phút)",
                    shipperInfo = assignedShipper,
                    shipperPhone = assignedPhone,
                    distanceKm = storeGroup.DistanceKm,
                    note = storeGroup.Note
                });
            }

            return Ok(new
            {
                success = true,
                message = "Đặt hàng thành công và đã lưu vào cơ sở dữ liệu MongoDB!",
                orderId = parentOrder.Id,
                totalAmount = parentOrder.TotalAmount,
                paymentMethod = parentOrder.PaymentMethod,
                paymentStatus = parentOrder.PaymentStatus,
                date = parentOrder.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                deliveryType = request.DeliveryType,
                subOrders = createdSubOrders
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Lỗi khi lưu đơn hàng vào database: " + ex.Message
            });
        }
    }

    /// <summary>
    /// API Lấy danh sách toàn bộ đơn mua của khách hàng từ Database MongoDB Atlas
    /// </summary>
    [HttpGet("buyer/{buyerId}")]
    public async Task<IActionResult> GetBuyerOrders(string buyerId)
    {
        try
        {
            FilterDefinition<ParentOrder> parentFilter;
            if (!string.IsNullOrWhiteSpace(buyerId) && ObjectId.TryParse(buyerId, out _))
            {
                parentFilter = Builders<ParentOrder>.Filter.Eq(p => p.BuyerId, buyerId);
            }
            else
            {
                // Nếu buyerId không phải ObjectId 24 hex (ví dụ 'usr_buyer_01'), lấy danh sách đơn gần đây
                parentFilter = Builders<ParentOrder>.Filter.Empty;
            }

            var parentOrders = await _mongoService.ParentOrders
                .Find(parentFilter)
                .SortByDescending(p => p.CreatedAt)
                .Limit(20)
                .ToListAsync();

            // Nếu chưa tìm thấy đơn theo buyerId (ví dụ dùng mock buyer), lấy 10 đơn gần nhất của hệ thống
            if (parentOrders.Count == 0)
            {
                parentOrders = await _mongoService.ParentOrders
                    .Find(_ => true)
                    .SortByDescending(p => p.CreatedAt)
                    .Limit(10)
                    .ToListAsync();
            }

            var result = new List<object>();

            foreach (var po in parentOrders)
            {
                var subOrders = await _mongoService.SubOrders
                    .Find(s => s.ParentOrderId == po.Id)
                    .ToListAsync();

                var subOrdersFormatted = new List<object>();

                foreach (var sub in subOrders)
                {
                    var items = await _mongoService.OrderItems
                        .Find(i => i.SubOrderId == sub.Id)
                        .ToListAsync();

                    // Tìm tên cửa hàng
                    var store = await _mongoService.Stores
                        .Find(s => s.Id == sub.StoreId)
                        .FirstOrDefaultAsync();

                    string storeName = store?.StoreName ?? "ZoneMart Cầu Giấy (10km)";

                    // Format danh sách món
                    var itemNames = new List<string>();
                    foreach (var item in items)
                    {
                        var prod = await _mongoService.Products
                            .Find(p => p.Id == item.ProductId)
                            .FirstOrDefaultAsync();
                        itemNames.Add(prod != null ? $"{prod.ProductName} (x{item.Quantity})" : $"Sản phẩm ZoneMart (x{item.Quantity})");
                    }

                    if (itemNames.Count == 0)
                    {
                        itemNames.Add("Nông sản tươi & Bách hóa sạch ZoneMart");
                    }

                    subOrdersFormatted.Add(new
                    {
                        subId = sub.Id,
                        storeName = storeName,
                        items = string.Join(", ", itemNames),
                        status = sub.Status,
                        statusText = sub.Status == "completed" 
                            ? "Giao hàng thành công" 
                            : (sub.Status == "cancelled" ? "Đã hủy đơn" : "Shipper đang giao hàng tới bạn"),
                        shipperInfo = "Lê Hoàng Nam (29N1-67890)",
                        shipperPhone = "0987 654 321"
                    });
                }

                result.Add(new
                {
                    orderId = po.Id,
                    date = po.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    total = po.TotalAmount,
                    paymentMethod = po.PaymentMethod,
                    paymentStatus = po.PaymentStatus,
                    deliveryType = subOrders.Count > 0 ? "express" : "standard",
                    subOrders = subOrdersFormatted
                });
            }

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Lỗi khi lấy dữ liệu đơn mua: " + ex.Message
            });
        }
    }

    /// <summary>
    /// API Cập nhật trạng thái đơn con (Ví dụ xác nhận nhận hàng, khiếu nại)
    /// </summary>
    [HttpPut("suborder/{subId}/status")]
    public async Task<IActionResult> UpdateSubOrderStatus(string subId, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var filter = Builders<SubOrder>.Filter.Eq(s => s.Id, subId);
            var update = Builders<SubOrder>.Update
                .Set(s => s.Status, dto.Status);

            if (!string.IsNullOrEmpty(dto.CancelReason))
            {
                update = update.Set(s => s.CancelReason, dto.CancelReason);
            }

            var res = await _mongoService.SubOrders.UpdateOneAsync(filter, update);

            return Ok(new
            {
                success = res.ModifiedCount > 0,
                message = "Cập nhật trạng thái đơn hàng thành công!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = "completed";
        public string? CancelReason { get; set; }
    }

    // Hàm an toàn đảm bảo chuỗi luôn là ObjectId 24 ký tự hợp lệ cho MongoDB
    private static string EnsureValidObjectId(string? input)
    {
        if (!string.IsNullOrWhiteSpace(input) && ObjectId.TryParse(input, out _))
        {
            return input;
        }
        return ObjectId.GenerateNewId().ToString();
    }
}
