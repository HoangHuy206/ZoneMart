using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public AdminController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    /// <summary>
    /// <summary>
    /// LẤY CHI TIẾT ĐỐI TÁC & TOÀN BỘ ẢNH GIẤY TỜ (THEO ID VÀ LOẠI ĐỐI TÁC)
    /// </summary>
    [HttpGet("partner-detail/{id}")]
    public async Task<IActionResult> GetPartnerDetail(string id, [FromQuery] string type = "seller")
    {
        if (type.ToLower() == "seller")
        {
            Store? store = null;
            try
            {
                store = await _mongoService.Stores.Find(s => s.Id == id).FirstOrDefaultAsync();
            }
            catch { }

            if (store == null && AuthController.InMemoryStores.TryGetValue(id, out var memStore))
            {
                store = memStore;
            }

            if (store == null) return NotFound(new { success = false, message = "Không tìm thấy hồ sơ gian hàng" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = store.Id,
                    type = "seller",
                    name = store.StoreName,
                    applicant = store.OwnerFullName,
                    cccd = store.CccdNumber,
                    address = store.Address,
                    category = store.Category,
                    date = store.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    status = store.Status,
                    rejectReason = store.RejectReason,
                    bankName = store.BankName,
                    bankAccountNumber = store.BankAccountNumber,
                    cccdFrontImage = store.CccdFrontImage,
                    cccdBackImage = store.CccdBackImage,
                    foodSafetyCertImage = store.FoodSafetyCertImage
                }
            });
        }
        else
        {
            Shipper? shipper = null;
            try
            {
                shipper = await _mongoService.Shippers.Find(s => s.Id == id).FirstOrDefaultAsync();
            }
            catch { }

            if (shipper == null && AuthController.InMemoryShippers.TryGetValue(id, out var memShip))
            {
                shipper = memShip;
            }

            if (shipper == null) return NotFound(new { success = false, message = "Không tìm thấy hồ sơ tài xế" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = shipper.Id,
                    type = "shipper",
                    name = $"Tài Xế: {shipper.FullName}",
                    applicant = shipper.FullName,
                    cccd = shipper.CccdNumber,
                    address = $"{shipper.VehicleModel} - {shipper.LicensePlate}",
                    category = shipper.VehicleType,
                    date = shipper.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    status = shipper.Status,
                    rejectReason = shipper.RejectReason,
                    bankName = shipper.BankName,
                    bankAccountNumber = shipper.BankAccountNumber,
                    cccdFrontImage = shipper.CccdFrontImage,
                    cccdBackImage = shipper.CccdBackImage,
                    drivingLicenseImage = shipper.DrivingLicenseImage,
                    avatarUrl = shipper.AvatarUrl
                }
            });
        }
    }

    /// <summary>
    /// LẤY DANH SÁCH ĐỐI TÁC (SELLER & SHIPPER) ĐANG CHỜ DUYỆT
    /// </summary>
    [HttpGet("pending-partners")]
    public async Task<IActionResult> GetPendingPartners()
    {
        var list = new List<PendingPartnerDto>();

        // 1. Lấy danh sách Store có Status == "Pending" từ MongoDB Atlas (Tối ưu hóa dung lượng truyền tải)
        try
        {
            var storeFilter = Builders<Store>.Filter.Eq(s => s.Status, "Pending");
            var pendingStores = await _mongoService.Stores.Find(storeFilter).ToListAsync();
            foreach (var store in pendingStores)
            {
                list.Add(new PendingPartnerDto
                {
                    Id = store.Id ?? "",
                    Type = "seller",
                    Name = store.StoreName,
                    Applicant = store.OwnerFullName,
                    Cccd = store.CccdNumber,
                    Address = store.Address,
                    Category = store.Category,
                    Date = store.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    Status = store.Status,
                    BankName = store.BankName,
                    BankAccountNumber = store.BankAccountNumber,
                    // Chỉ gửi URL ngắn nếu có, chuỗi base64 lớn sẽ nạp theo yêu cầu qua partner-detail
                    CccdFrontImage = (store.CccdFrontImage?.Length > 300) ? "" : store.CccdFrontImage ?? "",
                    CccdBackImage = (store.CccdBackImage?.Length > 300) ? "" : store.CccdBackImage ?? "",
                    FoodSafetyCertImage = (store.FoodSafetyCertImage?.Length > 300) ? "" : store.FoodSafetyCertImage ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Atlas Query Stores Warning] {ex.Message}");
        }

        // Bổ sung từ InMemoryStores nếu chưa có trong list
        foreach (var kvp in AuthController.InMemoryStores)
        {
            var store = kvp.Value;
            if (store.Status == "Pending" && !list.Any(p => p.Id == store.Id))
            {
                list.Add(new PendingPartnerDto
                {
                    Id = store.Id ?? kvp.Key,
                    Type = "seller",
                    Name = store.StoreName,
                    Applicant = store.OwnerFullName,
                    Cccd = store.CccdNumber,
                    Address = store.Address,
                    Category = store.Category,
                    Date = store.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    Status = store.Status,
                    BankName = store.BankName,
                    BankAccountNumber = store.BankAccountNumber,
                    CccdFrontImage = (store.CccdFrontImage?.Length > 300) ? "" : store.CccdFrontImage ?? "",
                    CccdBackImage = (store.CccdBackImage?.Length > 300) ? "" : store.CccdBackImage ?? "",
                    FoodSafetyCertImage = (store.FoodSafetyCertImage?.Length > 300) ? "" : store.FoodSafetyCertImage ?? ""
                });
            }
        }

        // 2. Lấy danh sách Shipper có Status == "Pending" từ MongoDB Atlas
        try
        {
            var shipperFilter = Builders<Shipper>.Filter.Eq(s => s.Status, "Pending");
            var pendingShippers = await _mongoService.Shippers.Find(shipperFilter).ToListAsync();
            foreach (var ship in pendingShippers)
            {
                list.Add(new PendingPartnerDto
                {
                    Id = ship.Id ?? "",
                    Type = "shipper",
                    Name = $"Tài Xế: {ship.FullName}",
                    Applicant = ship.FullName,
                    Cccd = ship.CccdNumber,
                    Address = $"{ship.VehicleModel} - {ship.LicensePlate} ({ship.OperatingArea})",
                    Category = ship.VehicleType,
                    Date = ship.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    Status = ship.Status,
                    BankName = ship.BankName,
                    BankAccountNumber = ship.BankAccountNumber,
                    CccdFrontImage = (ship.CccdFrontImage?.Length > 300) ? "" : ship.CccdFrontImage ?? "",
                    CccdBackImage = (ship.CccdBackImage?.Length > 300) ? "" : ship.CccdBackImage ?? "",
                    DrivingLicenseImage = (ship.DrivingLicenseImage?.Length > 300) ? "" : ship.DrivingLicenseImage ?? "",
                    AvatarUrl = ship.AvatarUrl
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Atlas Query Shippers Warning] {ex.Message}");
        }

        // Bổ sung từ InMemoryShippers
        foreach (var kvp in AuthController.InMemoryShippers)
        {
            var ship = kvp.Value;
            if (ship.Status == "Pending" && !list.Any(p => p.Id == ship.Id))
            {
                list.Add(new PendingPartnerDto
                {
                    Id = ship.Id ?? kvp.Key,
                    Type = "shipper",
                    Name = $"Tài Xế: {ship.FullName}",
                    Applicant = ship.FullName,
                    Cccd = ship.CccdNumber,
                    Address = $"{ship.VehicleModel} - {ship.LicensePlate} ({ship.OperatingArea})",
                    Category = ship.VehicleType,
                    Date = ship.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    Status = ship.Status,
                    BankName = ship.BankName,
                    BankAccountNumber = ship.BankAccountNumber,
                    CccdFrontImage = ship.CccdFrontImage,
                    CccdBackImage = ship.CccdBackImage,
                    DrivingLicenseImage = ship.DrivingLicenseImage,
                    AvatarUrl = ship.AvatarUrl
                });
            }
        }

        return Ok(new { success = true, data = list });
    }

    /// <summary>
    /// PHÊ DUYỆT ĐỐI TÁC (SELLER HOẶC SHIPPER)
    /// </summary>
    [HttpPost("approve-partner")]
    public async Task<IActionResult> ApprovePartner([FromBody] PartnerActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return BadRequest(new { success = false, message = "Thiếu ID đối tác cần duyệt!" });
        }

        string type = request.Type?.ToLower() ?? "seller";

        if (type == "seller")
        {
            // 1. Cập nhật Store -> Status = "Active"
            Store? store = null;
            try
            {
                var filter = Builders<Store>.Filter.Eq(s => s.Id, request.Id);
                var update = Builders<Store>.Update
                    .Set(s => s.Status, "Active")
                    .Set(s => s.RejectReason, (string?)null);
                store = await _mongoService.Stores.FindOneAndUpdateAsync(filter, update);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB Atlas Approve Store Warning] {ex.Message}");
            }

            if (AuthController.InMemoryStores.TryGetValue(request.Id, out var memStore))
            {
                memStore.Status = "Active";
                memStore.RejectReason = null;
                store ??= memStore;
            }

            if (store != null && !string.IsNullOrWhiteSpace(store.UserId))
            {
                // 2. Kích hoạt quyền IsSeller = true cho User liên kết
                try
                {
                    var userUpdate = Builders<User>.Update.Set(u => u.IsSeller, true);
                    await _mongoService.Users.UpdateOneAsync(Builders<User>.Filter.Eq(u => u.Id, store.UserId), userUpdate);
                }
                catch { }

                var user = AuthController.InMemoryUsers.Values.FirstOrDefault(u => u.Id == store.UserId);
                if (user != null)
                {
                    user.IsSeller = true;
                }
            }

            Console.WriteLine($"✅ [Admin] Đã PHÊ DUYỆT gian hàng: ID {request.Id} (Store: {store?.StoreName})");
            return Ok(new
            {
                success = true,
                message = $"Đã DUYỆT gian hàng '{store?.StoreName ?? request.Id}' thành công! Tài khoản người bán đã được kích hoạt."
            });
        }
        else // shipper
        {
            Shipper? shipper = null;
            try
            {
                var filter = Builders<Shipper>.Filter.Eq(s => s.Id, request.Id);
                var update = Builders<Shipper>.Update
                    .Set(s => s.Status, "Active")
                    .Set(s => s.RejectReason, (string?)null);
                shipper = await _mongoService.Shippers.FindOneAndUpdateAsync(filter, update);
            }
            catch { }

            if (AuthController.InMemoryShippers.TryGetValue(request.Id, out var memShipper))
            {
                memShipper.Status = "Active";
                memShipper.RejectReason = null;
                shipper ??= memShipper;
            }

            if (shipper != null && !string.IsNullOrWhiteSpace(shipper.UserId))
            {
                try
                {
                    var userUpdate = Builders<User>.Update.Set(u => u.IsShipper, true);
                    await _mongoService.Users.UpdateOneAsync(Builders<User>.Filter.Eq(u => u.Id, shipper.UserId), userUpdate);
                }
                catch { }

                var user = AuthController.InMemoryUsers.Values.FirstOrDefault(u => u.Id == shipper.UserId);
                if (user != null)
                {
                    user.IsShipper = true;
                }
            }

            Console.WriteLine($"✅ [Admin] Đã PHÊ DUYỆT tài xế: ID {request.Id} (Tên: {shipper?.FullName})");
            return Ok(new
            {
                success = true,
                message = $"Đã DUYỆT tài xế '{shipper?.FullName ?? request.Id}' thành công! Tài xế đã có thể nhận đơn."
            });
        }
    }

    /// <summary>
    /// TỪ CHỐI HỒ SƠ ĐỐI TÁC
    /// </summary>
    [HttpPost("reject-partner")]
    public async Task<IActionResult> RejectPartner([FromBody] PartnerActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return BadRequest(new { success = false, message = "Thiếu ID đối tác cần từ chối!" });
        }

        string reason = string.IsNullOrWhiteSpace(request.Reason) ? "Hồ sơ chưa đạt tiêu chuẩn kiểm duyệt của sàn ZoneMart." : request.Reason.Trim();
        string type = request.Type?.ToLower() ?? "seller";

        if (type == "seller")
        {
            try
            {
                var filter = Builders<Store>.Filter.Eq(s => s.Id, request.Id);
                var update = Builders<Store>.Update
                    .Set(s => s.Status, "Rejected")
                    .Set(s => s.RejectReason, reason);
                await _mongoService.Stores.UpdateOneAsync(filter, update);
            }
            catch { }

            if (AuthController.InMemoryStores.TryGetValue(request.Id, out var memStore))
            {
                memStore.Status = "Rejected";
                memStore.RejectReason = reason;
            }

            Console.WriteLine($"❌ [Admin] Đã TỪ CHỐI gian hàng: ID {request.Id}. Lý do: {reason}");
            return Ok(new { success = true, message = $"Đã TỪ CHỐI hồ sơ gian hàng. Lý do: {reason}" });
        }
        else
        {
            try
            {
                var filter = Builders<Shipper>.Filter.Eq(s => s.Id, request.Id);
                var update = Builders<Shipper>.Update
                    .Set(s => s.Status, "Rejected")
                    .Set(s => s.RejectReason, reason);
                await _mongoService.Shippers.UpdateOneAsync(filter, update);
            }
            catch { }

            if (AuthController.InMemoryShippers.TryGetValue(request.Id, out var memShipper))
            {
                memShipper.Status = "Rejected";
                memShipper.RejectReason = reason;
            }

            Console.WriteLine($"❌ [Admin] Đã TỪ CHỐI tài xế: ID {request.Id}. Lý do: {reason}");
            return Ok(new { success = true, message = $"Đã TỪ CHỐI hồ sơ tài xế. Lý do: {reason}" });
        }
    }
}

public class PartnerActionRequest
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "seller"; // seller hoặc shipper
    public string? Reason { get; set; }
}

public class PendingPartnerDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "seller";
    public string Name { get; set; } = string.Empty;
    public string Applicant { get; set; } = string.Empty;
    public string Cccd { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string CccdFrontImage { get; set; } = string.Empty;
    public string CccdBackImage { get; set; } = string.Empty;
    public string FoodSafetyCertImage { get; set; } = string.Empty;
    public string DrivingLicenseImage { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

