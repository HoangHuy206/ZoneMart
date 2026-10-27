using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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

    private static (object Stats, DateTime Expiry)? _statsCache = null;
    private static (object Data, DateTime Expiry)? _usersCache = null;
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> PartnerDetailsCache = new(StringComparer.OrdinalIgnoreCase);

    public static void CacheStorePartner(Store store)
    {
        if (store == null) return;
        var partnerData = new
        {
            id = store.Id,
            type = "seller",
            name = store.StoreName,
            applicant = store.OwnerFullName,
            cccd = store.CccdNumber ?? "",
            cccdNumber = store.CccdNumber ?? "",
            nguyenQuan = store.NguyenQuan ?? "",
            address = store.Address,
            category = store.Category,
            date = store.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            status = store.Status,
            rejectReason = store.RejectReason,
            bankName = store.BankName,
            bankAccountNumber = store.BankAccountNumber,
            cccdFrontImage = store.CccdFrontImage ?? "",
            cccdBackImage = store.CccdBackImage ?? "",
            foodSafetyCertImage = store.FoodSafetyCertImage ?? ""
        };

        if (!string.IsNullOrEmpty(store.Id)) PartnerDetailsCache[store.Id] = partnerData;
        if (!string.IsNullOrEmpty(store.StoreCode)) PartnerDetailsCache[store.StoreCode] = partnerData;
        if (!string.IsNullOrEmpty(store.UserId)) PartnerDetailsCache[store.UserId] = partnerData;
        if (!string.IsNullOrEmpty(store.PhoneEmail)) PartnerDetailsCache[store.PhoneEmail] = partnerData;
    }

    public static void CacheShipperPartner(Shipper shipper)
    {
        if (shipper == null) return;
        var shipperData = new
        {
            id = shipper.Id,
            type = "shipper",
            name = $"Tài Xế: {shipper.FullName}",
            applicant = shipper.FullName,
            cccd = shipper.CccdNumber ?? "",
            cccdNumber = shipper.CccdNumber ?? "",
            nguyenQuan = shipper.NguyenQuan ?? "",
            address = $"{shipper.VehicleModel} - {shipper.LicensePlate}",
            category = shipper.VehicleType,
            date = shipper.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            status = shipper.Status,
            rejectReason = shipper.RejectReason,
            bankName = shipper.BankName,
            bankAccountNumber = shipper.BankAccountNumber,
            cccdFrontImage = shipper.CccdFrontImage ?? "",
            cccdBackImage = shipper.CccdBackImage ?? "",
            drivingLicenseImage = shipper.DrivingLicenseImage ?? "",
            avatarUrl = shipper.AvatarUrl ?? ""
        };

        if (!string.IsNullOrEmpty(shipper.Id)) PartnerDetailsCache[shipper.Id] = shipperData;
        if (!string.IsNullOrEmpty(shipper.ShipperCode)) PartnerDetailsCache[shipper.ShipperCode] = shipperData;
        if (!string.IsNullOrEmpty(shipper.UserId)) PartnerDetailsCache[shipper.UserId] = shipperData;
        if (!string.IsNullOrEmpty(shipper.PhoneNumber)) PartnerDetailsCache[shipper.PhoneNumber] = shipperData;
    }

    public static void InvalidateAdminCache()
    {
        _statsCache = null;
        _usersCache = null;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            if (_statsCache.HasValue && DateTime.UtcNow < _statsCache.Value.Expiry)
            {
                return Ok(_statsCache.Value.Stats);
            }

            var totalUsersTask = _mongoService.Users.CountDocumentsAsync(u => !u.IsDeleted);
            var pendingShopsTask = _mongoService.Stores.CountDocumentsAsync(s => s.Status == "Pending");
            var pendingShippersTask = _mongoService.Shippers.CountDocumentsAsync(s => s.Status == "Pending");
            var lockedUsersTask = _mongoService.Users.CountDocumentsAsync(u => !u.IsDeleted && (u.AccountStatus == "locked_10_days" || u.AccountStatus == "banned" || u.AccountStatus == "suspended"));
            var totalOrdersTask = _mongoService.ParentOrders.CountDocumentsAsync(_ => true);
            var totalStoresTask = _mongoService.Stores.CountDocumentsAsync(s => s.Status == "Approved" || s.Status == "Active");
            var totalShippersTask = _mongoService.Shippers.CountDocumentsAsync(s => s.Status == "Approved" || s.Status == "Active");
            var totalProductsTask = _mongoService.Products.CountDocumentsAsync(_ => true);

            var shipperProj = Builders<Shipper>.Projection
                .Include(s => s.FullName)
                .Include(s => s.LicensePlate)
                .Include(s => s.VehicleModel)
                .Include(s => s.IsOnline)
                .Include(s => s.Status);
            var shippersTask = _mongoService.Shippers.Find(_ => true).Project<Shipper>(shipperProj).ToListAsync();

            var orderProj = Builders<ParentOrder>.Projection
                .Include(o => o.TotalAmount)
                .Include(o => o.CreatedAt);
            var ordersTask = _mongoService.ParentOrders.Find(_ => true).Project<ParentOrder>(orderProj).ToListAsync();

            await Task.WhenAll(totalUsersTask, pendingShopsTask, pendingShippersTask, lockedUsersTask, totalOrdersTask, totalStoresTask, totalShippersTask, totalProductsTask, shippersTask, ordersTask);

            var totalUsers = await totalUsersTask;
            var pendingShops = await pendingShopsTask;
            var pendingShippers = await pendingShippersTask;
            var lockedUsers = await lockedUsersTask;
            var totalOrders = await totalOrdersTask;
            var totalStores = await totalStoresTask;
            var totalShippers = await totalShippersTask;
            var totalProducts = await totalProductsTask;
            var shippers = await shippersTask;
            var orders = await ordersTask;

            var onlineShippers = shippers.Count(s => s.IsOnline || s.Status == "Approved");
            var deliveringShippers = Math.Max(1, shippers.Count(s => s.IsOnline));

            decimal totalRevenue = orders.Sum(o => o.TotalAmount);
            decimal shopCommission = totalRevenue * 0.05m;
            decimal shipperFees = orders.Count > 0 ? orders.Count * 25000m : 0;
            long totalViews = (totalOrders * 12) + (totalUsers * 8) + (totalProducts * 5) + (shippers.Count * 6);

            // Dữ liệu danh sách Shipper thực tế từ CSDL MongoDB
            var activeShipperFeed = shippers.Select(s => new
            {
                fullName = s.FullName,
                licensePlate = string.IsNullOrEmpty(s.LicensePlate) ? "29N1-67890" : s.LicensePlate,
                vehicleModel = string.IsNullOrEmpty(s.VehicleModel) ? "Xe máy" : s.VehicleModel,
                isOnline = s.IsOnline,
                locationNote = s.IsOnline ? "Quận Cầu Giấy • GPS Active" : "Đang giao đơn hàng"
            }).ToList();

            // Tính toán dữ liệu thật theo các ngày trong tuần cho Biểu đồ (Visual Bar Chart)
            var chartWeekly = new List<object>();
            var today = DateTime.UtcNow.Date;
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                var dayOrders = orders.Where(o => o.CreatedAt.Date == targetDate).ToList();
                decimal dayRevenue = dayOrders.Sum(o => o.TotalAmount);
                int dayOrderCount = dayOrders.Count;

                string label = i == 0 ? "Hôm nay" : targetDate.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Thứ 2",
                    DayOfWeek.Tuesday => "Thứ 3",
                    DayOfWeek.Wednesday => "Thứ 4",
                    DayOfWeek.Thursday => "Thứ 5",
                    DayOfWeek.Friday => "Thứ 6",
                    DayOfWeek.Saturday => "Thứ 7",
                    DayOfWeek.Sunday => "CN",
                    _ => targetDate.ToString("dd/MM")
                };

                int height = (totalRevenue > 0 && dayRevenue > 0)
                    ? Math.Min((int)((dayRevenue / totalRevenue) * 100), 100) 
                    : (dayOrderCount > 0 ? 20 : 5);

                chartWeekly.Add(new
                {
                    dayLabel = label,
                    date = targetDate.ToString("yyyy-MM-dd"),
                    revenue = dayRevenue,
                    orderCount = dayOrderCount,
                    heightPercent = height
                });
            }

            var resultObj = new
            {
                success = true,
                stats = new
                {
                    totalUsers,
                    totalStores,
                    totalShippers,
                    totalProducts,
                    pendingShops,
                    pendingShippers,
                    lockedUsers,
                    totalOrders,
                    onlineShippers,
                    deliveringShippers,
                    totalRevenue,
                    shopCommission,
                    shipperFees,
                    totalViews,
                    chartWeekly,
                    activeShipperFeed
                }
            };
            _statsCache = (resultObj, DateTime.UtcNow.AddSeconds(5));
            return Ok(resultObj);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết hồ sơ đối tác (Shop hoặc Shipper) theo ID
    /// </summary>
    [HttpGet("partner/{id}")]
    public async Task<IActionResult> GetPartnerDetail(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { success = false, message = "ID đối tác không hợp lệ" });
            string cleanId = id.Trim();

            // 1. Kiểm tra cache đối tác đã nạp trước đó (0ms siêu tốc)
            if (PartnerDetailsCache.TryGetValue(cleanId, out var cachedData))
            {
                return Ok(new
                {
                    success = true,
                    data = cachedData,
                    partner = cachedData
                });
            }

            // 2. Tìm trong AuthController InMemory
            Store? store = null;
            if (AuthController.InMemoryStores.TryGetValue(cleanId, out var memStore))
            {
                store = memStore;
            }
            if (store == null)
            {
                store = AuthController.InMemoryStores.Values.FirstOrDefault(s =>
                    (!string.IsNullOrEmpty(s.Id) && s.Id.Equals(cleanId, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.StoreCode) && s.StoreCode.Equals(cleanId, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.UserId) && s.UserId.Equals(cleanId, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.PhoneEmail) && s.PhoneEmail.Equals(cleanId, StringComparison.OrdinalIgnoreCase))
                );
            }

            // 3. Nếu chưa thấy, tìm trong MongoDB Atlas theo Id, StoreCode, UserId, PhoneEmail
            if (store == null)
            {
                try
                {
                    var storeFilters = new List<FilterDefinition<Store>>();
                    if (ObjectId.TryParse(cleanId, out _))
                    {
                        storeFilters.Add(Builders<Store>.Filter.Eq(s => s.Id, cleanId));
                    }
                    storeFilters.Add(Builders<Store>.Filter.Eq(s => s.StoreCode, cleanId));
                    storeFilters.Add(Builders<Store>.Filter.Eq(s => s.UserId, cleanId));
                    storeFilters.Add(Builders<Store>.Filter.Eq(s => s.PhoneEmail, cleanId));

                    store = await _mongoService.Stores.Find(Builders<Store>.Filter.Or(storeFilters)).FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ [GetPartnerDetail Store Mongo Error] {ex.Message}");
                }
            }

            if (store != null)
            {
                CacheStorePartner(store);
                if (PartnerDetailsCache.TryGetValue(cleanId, out var partnerData))
                {
                    return Ok(new
                    {
                        success = true,
                        data = partnerData,
                        partner = partnerData
                    });
                }
            }

            // 4. Kiểm tra hồ sơ Shipper tương tự
            Shipper? shipper = null;
            if (AuthController.InMemoryShippers.TryGetValue(cleanId, out var memShip))
            {
                shipper = memShip;
            }
            if (shipper == null)
            {
                shipper = AuthController.InMemoryShippers.Values.FirstOrDefault(sh =>
                    (!string.IsNullOrEmpty(sh.Id) && sh.Id.Equals(cleanId, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(sh.ShipperCode) && sh.ShipperCode.Equals(cleanId, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(sh.UserId) && sh.UserId.Equals(cleanId, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(sh.PhoneNumber) && sh.PhoneNumber.Equals(cleanId, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (shipper == null)
            {
                try
                {
                    var shipperFilters = new List<FilterDefinition<Shipper>>();
                    if (ObjectId.TryParse(cleanId, out _))
                    {
                        shipperFilters.Add(Builders<Shipper>.Filter.Eq(sh => sh.Id, cleanId));
                    }
                    shipperFilters.Add(Builders<Shipper>.Filter.Eq(sh => sh.ShipperCode, cleanId));
                    shipperFilters.Add(Builders<Shipper>.Filter.Eq(sh => sh.UserId, cleanId));
                    shipperFilters.Add(Builders<Shipper>.Filter.Eq(sh => sh.PhoneNumber, cleanId));

                    shipper = await _mongoService.Shippers.Find(Builders<Shipper>.Filter.Or(shipperFilters)).FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ [GetPartnerDetail Shipper Mongo Error] {ex.Message}");
                }
            }

            if (shipper != null)
            {
                CacheShipperPartner(shipper);
                if (PartnerDetailsCache.TryGetValue(cleanId, out var shipperData))
                {
                    return Ok(new
                    {
                        success = true,
                        data = shipperData,
                        partner = shipperData
                    });
                }
            }

            return NotFound(new { success = false, message = "Không tìm thấy hồ sơ đối tác" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Tra cứu & lọc danh sách tài khoản từ MongoDB Atlas theo Smart Search (Tên người mua, Tên Cửa Hàng Shop, Shipper biển số xe, Quản lý, Gmail), Role, Status
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? query, [FromQuery] string? role, [FromQuery] string? status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(role) && string.IsNullOrWhiteSpace(status))
            {
                if (_usersCache.HasValue && DateTime.UtcNow < _usersCache.Value.Expiry)
                {
                    return Ok(_usersCache.Value.Data);
                }
            }

            var userProj = Builders<User>.Projection.Exclude(u => u.FaceEmbedding);
            var storeProj = Builders<Store>.Projection
                .Exclude(s => s.CccdFrontImage)
                .Exclude(s => s.CccdBackImage)
                .Exclude(s => s.FoodSafetyCertImage);
            var shipperProj = Builders<Shipper>.Projection
                .Exclude(s => s.AvatarUrl)
                .Exclude(s => s.CccdFrontImage)
                .Exclude(s => s.CccdBackImage)
                .Exclude(s => s.DrivingLicenseImage);

            var usersTask = _mongoService.Users.Find(u => !u.IsDeleted).Project<User>(userProj).SortByDescending(u => u.CreatedAt).ToListAsync();
            var storeTask = _mongoService.Stores.Find(_ => true).Project<Store>(storeProj).SortByDescending(s => s.CreatedAt).ToListAsync();
            var shipperTask = _mongoService.Shippers.Find(_ => true).Project<Shipper>(shipperProj).SortByDescending(s => s.CreatedAt).ToListAsync();

            await Task.WhenAll(usersTask, storeTask, shipperTask);

            var usersList = await usersTask;
            var storeList = await storeTask;
            var shipperList = await shipperTask;

            // Tự động nạp trước (pre-warm cache) các hồ sơ Đang Chờ Duyệt (Pending) chạy ngầm để Admin thẩm định tức thì 0ms
            _ = Task.Run(async () =>
            {
                try
                {
                    var pendingStores = storeList.Where(s => s.Status == "Pending" || s.Status == "pending").ToList();
                    foreach (var ps in pendingStores)
                    {
                        if (string.IsNullOrEmpty(ps.Id) || PartnerDetailsCache.ContainsKey(ps.Id)) continue;
                        try
                        {
                            var fullStore = await _mongoService.Stores.Find(s => s.Id == ps.Id).FirstOrDefaultAsync();
                            if (fullStore != null) CacheStorePartner(fullStore);
                        }
                        catch { }
                    }

                    var pendingShippers = shipperList.Where(sh => sh.Status == "Pending" || sh.Status == "pending").ToList();
                    foreach (var psh in pendingShippers)
                    {
                        if (string.IsNullOrEmpty(psh.Id) || PartnerDetailsCache.ContainsKey(psh.Id)) continue;
                        try
                        {
                            var fullShipper = await _mongoService.Shippers.Find(sh => sh.Id == psh.Id).FirstOrDefaultAsync();
                            if (fullShipper != null) CacheShipperPartner(fullShipper);
                        }
                        catch { }
                    }
                }
                catch { }
            });

            var allCombined = new List<AdminUserViewDto>();

            // 1. Map existing users and attach Store & Shipper details (Ưu tiên theo PhoneEmail, UserId, StoreCode, Name)
            foreach (var u in usersList)
            {
                var store = storeList.FirstOrDefault(s =>
                    (!string.IsNullOrEmpty(s.PhoneEmail) && !string.IsNullOrEmpty(u.PhoneEmail) && s.PhoneEmail.Trim().ToLower() == u.PhoneEmail.Trim().ToLower()) ||
                    (!string.IsNullOrEmpty(s.UserId) && (s.UserId == u.Id || (!string.IsNullOrEmpty(u.UserCode) && s.UserId == u.UserCode))) ||
                    (!string.IsNullOrEmpty(s.StoreCode) && !string.IsNullOrEmpty(u.UserCode) && s.StoreCode == u.UserCode)
                );

                var shipper = shipperList.FirstOrDefault(sh =>
                    (!string.IsNullOrEmpty(sh.PhoneNumber) && !string.IsNullOrEmpty(u.PhoneEmail) && sh.PhoneNumber.Trim().ToLower() == u.PhoneEmail.Trim().ToLower()) ||
                    (!string.IsNullOrEmpty(sh.UserId) && (sh.UserId == u.Id || (!string.IsNullOrEmpty(u.UserCode) && sh.UserId == u.UserCode))) ||
                    (!string.IsNullOrEmpty(sh.ShipperCode) && !string.IsNullOrEmpty(u.UserCode) && sh.ShipperCode == u.UserCode)
                );

                string primaryRole = u.IsAdmin ? "Admin" 
                    : u.IsManager ? "Manager" 
                    : (u.IsSeller && store != null && (store.Status == "Pending" || store.Status == "pending")) ? "Seller" 
                    : (u.IsShipper && shipper != null && (shipper.Status == "Pending" || shipper.Status == "pending")) ? "Shipper" 
                    : (u.IsSeller && store != null && (store.Status == "Rejected" || store.Status == "rejected")) ? "RejectedSeller"
                    : (u.IsShipper && shipper != null && (shipper.Status == "Rejected" || shipper.Status == "rejected")) ? "RejectedShipper"
                    : u.IsSeller ? "Seller" 
                    : u.IsShipper ? "Shipper" 
                    : "Buyer";

                string accountStatus = u.AccountStatus ?? "active";
                if (accountStatus != "banned" && accountStatus != "locked_10_days" && accountStatus != "suspended")
                {
                    if ((store != null && (store.Status == "Pending" || store.Status == "pending")) || (shipper != null && (shipper.Status == "Pending" || shipper.Status == "pending")) || u.AccountStatus == "pending")
                    {
                        accountStatus = "pending";
                    }
                    else if ((store != null && (store.Status == "Rejected" || store.Status == "rejected")) || (shipper != null && (shipper.Status == "Rejected" || shipper.Status == "rejected")) || u.AccountStatus == "rejected")
                    {
                        accountStatus = "rejected";
                        if (u.AccountStatus != "rejected")
                        {
                            u.AccountStatus = "rejected";
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var up = Builders<User>.Update.Set(x => x.AccountStatus, "rejected");
                                    await _mongoService.Users.UpdateOneAsync(x => x.Id == u.Id, up);
                                }
                                catch { }
                            });
                        }
                    }
                }

                allCombined.Add(new AdminUserViewDto
                {
                    id = u.Id ?? "",
                    email = u.PhoneEmail ?? "",
                    fullName = u.FullName ?? "Người Dùng",
                    avatarUrl = !string.IsNullOrEmpty(u.AvatarUrl) ? u.AvatarUrl : (shipper != null && !string.IsNullOrEmpty(shipper.AvatarUrl) ? shipper.AvatarUrl : (store != null ? "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200" : "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=150")),
                    primaryRole = primaryRole,
                    isBuyer = u.IsBuyer,
                    isSeller = u.IsSeller,
                    isShipper = u.IsShipper,
                    isAdmin = u.IsAdmin,
                    isManager = u.IsManager,
                    permissions = u.Permissions ?? new List<string>(),
                    accountStatus = accountStatus,
                    lockUntil = u.LockUntil,
                    violationCount = u.ViolationCount,
                    createdAt = u.CreatedAt.ToString("dd/MM/yyyy"),
                    cccdNumber = (store != null && !string.IsNullOrWhiteSpace(store.CccdNumber)) ? store.CccdNumber.Trim()
                        : (shipper != null && !string.IsNullOrWhiteSpace(shipper.CccdNumber)) ? shipper.CccdNumber.Trim()
                        : (!string.IsNullOrWhiteSpace(u.CccdNumber) ? u.CccdNumber.Trim() : string.Empty),
                    storeDetails = store != null ? new
                    {
                        storeId = store.Id,
                        storeCode = store.StoreCode,
                        storeName = store.StoreName,
                        ownerFullName = store.OwnerFullName,
                        phoneEmail = store.PhoneEmail,
                        address = store.Address,
                        sellerType = store.SellerType,
                        ward = store.Ward,
                        district = store.District,
                        marketName = store.MarketName,
                        category = store.Category,
                        openHours = store.OpenHours,
                        bankName = store.BankName,
                        bankAccountNumber = store.BankAccountNumber,
                        status = store.Status,
                        rejectReason = store.RejectReason,
                        cccdNumber = store.CccdNumber,
                        nguyenQuan = store.NguyenQuan,
                        cccdFrontImage = "",
                        cccdBackImage = "",
                        foodSafetyCertImage = "",
                        createdAt = store.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    } : null,
                    shipperDetails = shipper != null ? new
                    {
                        shipperId = shipper.Id,
                        shipperCode = shipper.ShipperCode,
                        licensePlate = shipper.LicensePlate,
                        vehicleType = shipper.VehicleType,
                        vehicleModel = shipper.VehicleModel,
                        operatingArea = shipper.OperatingArea,
                        bankName = shipper.BankName,
                        bankAccountNumber = shipper.BankAccountNumber,
                        avatarUrl = (!string.IsNullOrEmpty(shipper.AvatarUrl) && !shipper.AvatarUrl.StartsWith("data:")) ? shipper.AvatarUrl : "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200",
                        status = shipper.Status,
                        rejectReason = shipper.RejectReason,
                        cccdNumber = shipper.CccdNumber,
                        nguyenQuan = shipper.NguyenQuan,
                        cccdFrontImage = "",
                        cccdBackImage = "",
                        drivingLicenseImage = "",
                        createdAt = shipper.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    } : null
                });
            }

            // 2. Add unmapped Stores as Seller accounts
            foreach (var store in storeList)
            {
                bool alreadyMapped = allCombined.Any(u =>
                    (u.storeDetails != null && (string)((dynamic)u.storeDetails).storeId == store.Id) ||
                    (!string.IsNullOrEmpty(store.PhoneEmail) && !string.IsNullOrEmpty(u.email) && u.email.Trim().ToLower() == store.PhoneEmail.Trim().ToLower()) ||
                    (!string.IsNullOrEmpty(store.OwnerFullName) && !string.IsNullOrEmpty(u.fullName) && u.fullName.Trim().ToLower() == store.OwnerFullName.Trim().ToLower())
                );

                if (!alreadyMapped)
                {
                    allCombined.Add(new AdminUserViewDto
                    {
                        id = store.Id ?? Guid.NewGuid().ToString(),
                        email = !string.IsNullOrEmpty(store.PhoneEmail) ? store.PhoneEmail : ("seller." + (store.StoreName?.Replace(" ", "").ToLower() ?? "shop") + "@zonemart.vn"),
                        fullName = store.OwnerFullName ?? "Chủ Tiệm " + store.StoreName,
                        avatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200",
                        primaryRole = "Seller",
                        isBuyer = true,
                        isSeller = true,
                        isShipper = false,
                        isAdmin = false,
                        isManager = false,
                        accountStatus = store.Status == "Pending" ? "pending" : "active",
                        lockUntil = null,
                        violationCount = 0,
                        createdAt = store.CreatedAt.ToString("dd/MM/yyyy"),
                        cccdNumber = store.CccdNumber?.Trim() ?? string.Empty,
                        storeDetails = new
                        {
                            storeId = store.Id,
                            storeCode = store.StoreCode,
                            storeName = store.StoreName,
                            ownerFullName = store.OwnerFullName,
                            phoneEmail = store.PhoneEmail,
                            address = store.Address,
                            category = store.Category,
                            openHours = store.OpenHours,
                            bankName = store.BankName,
                            bankAccountNumber = store.BankAccountNumber,
                            status = store.Status,
                            rejectReason = store.RejectReason,
                            cccdNumber = store.CccdNumber,
                            nguyenQuan = store.NguyenQuan,
                            cccdFrontImage = "",
                            cccdBackImage = "",
                            foodSafetyCertImage = "",
                            createdAt = store.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                        },
                        shipperDetails = null
                    });
                }
            }

            // 3. Add unmapped Shippers as Shipper accounts
            foreach (var shipper in shipperList)
            {
                bool alreadyMapped = allCombined.Any(u =>
                    (u.shipperDetails != null && (string)((dynamic)u.shipperDetails).shipperId == shipper.Id) ||
                    (!string.IsNullOrEmpty(shipper.PhoneNumber) && !string.IsNullOrEmpty(u.email) && u.email.Trim().ToLower() == shipper.PhoneNumber.Trim().ToLower()) ||
                    (!string.IsNullOrEmpty(shipper.FullName) && !string.IsNullOrEmpty(u.fullName) && u.fullName.Trim().ToLower() == shipper.FullName.Trim().ToLower())
                );

                if (!alreadyMapped)
                {
                    allCombined.Add(new AdminUserViewDto
                    {
                        id = shipper.Id ?? Guid.NewGuid().ToString(),
                        email = shipper.PhoneNumber ?? "shipper@zonemart.vn",
                        fullName = shipper.FullName ?? "Tài Xế " + shipper.LicensePlate,
                        avatarUrl = (!string.IsNullOrEmpty(shipper.AvatarUrl) && !shipper.AvatarUrl.StartsWith("data:")) ? shipper.AvatarUrl : "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200",
                        primaryRole = "Shipper",
                        isBuyer = true,
                        isSeller = false,
                        isShipper = true,
                        isAdmin = false,
                        isManager = false,
                        accountStatus = shipper.Status == "Pending" ? "pending" : "active",
                        lockUntil = null,
                        violationCount = 0,
                        createdAt = shipper.CreatedAt.ToString("dd/MM/yyyy"),
                        cccdNumber = shipper.CccdNumber?.Trim() ?? string.Empty,
                        storeDetails = null,
                        shipperDetails = new
                        {
                            shipperId = shipper.Id,
                            shipperCode = shipper.ShipperCode,
                            licensePlate = shipper.LicensePlate,
                            vehicleType = shipper.VehicleType,
                            vehicleModel = shipper.VehicleModel,
                            operatingArea = shipper.OperatingArea,
                            bankName = shipper.BankName,
                            bankAccountNumber = shipper.BankAccountNumber,
                            avatarUrl = (!string.IsNullOrEmpty(shipper.AvatarUrl) && !shipper.AvatarUrl.StartsWith("data:")) ? shipper.AvatarUrl : "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200",
                            status = shipper.Status,
                            rejectReason = shipper.RejectReason,
                            cccdNumber = shipper.CccdNumber,
                            nguyenQuan = shipper.NguyenQuan,
                            cccdFrontImage = "",
                            cccdBackImage = "",
                            drivingLicenseImage = "",
                            createdAt = shipper.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                        }
                    });
                }
            }

            // 4. Smart Query Filtering
            IEnumerable<AdminUserViewDto> filtered = allCombined;

            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.Trim().ToLower();
                filtered = filtered.Where(u =>
                {
                    string name = u.fullName ?? "";
                    string email = u.email ?? "";
                    string storeName = u.storeDetails != null ? ((dynamic)u.storeDetails).storeName?.ToString() ?? "" : "";
                    string licensePlate = u.shipperDetails != null ? ((dynamic)u.shipperDetails).licensePlate?.ToString() ?? "" : "";
                    string shipperCode = u.shipperDetails != null ? ((dynamic)u.shipperDetails).shipperCode?.ToString() ?? "" : "";

                    return name.ToLower().Contains(q) ||
                           email.ToLower().Contains(q) ||
                           storeName.ToLower().Contains(q) ||
                           licensePlate.ToLower().Contains(q) ||
                           shipperCode.ToLower().Contains(q);
                });
            }

            if (!string.IsNullOrWhiteSpace(role) && role.ToLower() != "all" && role.ToLower() != "tất cả")
            {
                string r = role.ToLower();
                if (r == "buyer") filtered = filtered.Where(u => u.primaryRole == "Buyer");
                else if (r == "seller") filtered = filtered.Where(u => u.isSeller || u.primaryRole == "Seller");
                else if (r == "shipper") filtered = filtered.Where(u => u.isShipper || u.primaryRole == "Shipper");
                else if (r == "manager") filtered = filtered.Where(u => u.isManager || u.primaryRole == "Manager");
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all" && status.ToLower() != "tất cả")
            {
                string s = status.ToLower();
                if (s == "active") filtered = filtered.Where(u => u.accountStatus == "active");
                else if (s == "pending") filtered = filtered.Where(u => u.accountStatus == "pending" || (u.storeDetails != null && ((dynamic)u.storeDetails).status == "Pending") || (u.shipperDetails != null && ((dynamic)u.shipperDetails).status == "Pending"));
                else if (s == "suspended") filtered = filtered.Where(u => u.accountStatus == "suspended" || u.accountStatus == "locked_10_days");
                else if (s == "banned") filtered = filtered.Where(u => u.accountStatus == "banned");
            }

            var resultList = filtered.ToList();
            var resultObj = new { success = true, count = resultList.Count, users = resultList };
            if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(role) && string.IsNullOrWhiteSpace(status))
            {
                _usersCache = (resultObj, DateTime.UtcNow.AddSeconds(5));
            }
            return Ok(resultObj);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Phê duyệt hồ sơ nâng cấp Seller / Shipper (Cả Admin và Quản lý đều có quyền)
    /// </summary>
    [HttpPost("approve-kyc")]
    public async Task<IActionResult> ApproveKyc([FromBody] ApproveKycRequest req)
    {
        InvalidateAdminCache();
        try
        {
            string emailToSend = "";

            if (req.Type.ToLower() == "seller")
            {
                var storeFilter = Builders<Store>.Filter.Or(
                    Builders<Store>.Filter.Eq(s => s.Id, req.TargetId),
                    Builders<Store>.Filter.Eq(s => s.StoreCode, req.TargetId),
                    Builders<Store>.Filter.Eq(s => s.UserId, req.TargetId),
                    Builders<Store>.Filter.Eq(s => s.PhoneEmail, req.TargetId)
                );
                var store = await _mongoService.Stores.Find(storeFilter).FirstOrDefaultAsync();
                if (store == null) return NotFound(new { success = false, message = "Không tìm thấy hồ sơ Cửa hàng!" });

                var storeUpdate = Builders<Store>.Update
                    .Set(s => s.Status, "Approved")
                    .Set(s => s.RejectReason, null);
                await _mongoService.Stores.UpdateOneAsync(s => s.Id == store.Id, storeUpdate);
                store.Status = "Approved";
                if (store.Id != null) AuthController.InMemoryStores[store.Id] = store;
                if (!string.IsNullOrEmpty(store.StoreCode)) AuthController.InMemoryStores[store.StoreCode] = store;

                // Cập nhật User chính xác: kích hoạt AccountStatus = "active" và IsSeller = true
                var filterList = new List<FilterDefinition<User>>();
                if (!string.IsNullOrEmpty(store.UserId) && MongoDB.Bson.ObjectId.TryParse(store.UserId, out _))
                {
                    filterList.Add(Builders<User>.Filter.Eq(u => u.Id, store.UserId));
                }
                if (!string.IsNullOrEmpty(store.UserId))
                {
                    filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, store.UserId));
                }
                if (!string.IsNullOrEmpty(store.StoreCode))
                {
                    filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, store.StoreCode));
                }
                if (!string.IsNullOrEmpty(store.PhoneEmail))
                {
                    string cleanMail = store.PhoneEmail.Trim().ToLower();
                    filterList.Add(Builders<User>.Filter.Eq(u => u.PhoneEmail, cleanMail));
                    filterList.Add(Builders<User>.Filter.Regex(u => u.PhoneEmail, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleanMail)}$", "i")));
                    filterList.Add(Builders<User>.Filter.Eq(u => u.Phone, store.PhoneEmail.Trim()));
                }
                var user = filterList.Count > 0 ? await _mongoService.Users.Find(Builders<User>.Filter.Or(filterList)).FirstOrDefaultAsync() : null;
                if (user != null)
                {
                    string targetUserCode = !string.IsNullOrEmpty(store.StoreCode)
                        ? store.StoreCode
                        : ((!string.IsNullOrEmpty(user.UserCode) && user.UserCode.StartsWith("CUST-")) ? $"SEL-{Random.Shared.Next(1000, 9999)}" : user.UserCode);

                    var userUpdate = Builders<User>.Update
                        .Set(u => u.IsSeller, true)
                        .Set(u => u.AccountStatus, "active")
                        .Set(u => u.UserCode, targetUserCode);
                    await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, userUpdate);

                    user.IsSeller = true;
                    user.AccountStatus = "active";
                    user.UserCode = targetUserCode;
                    if (!string.IsNullOrEmpty(user.PhoneEmail)) AuthController.InMemoryUsers[user.PhoneEmail] = user;

                    // Liên kết store.UserId trực tiếp với user.Id
                    await _mongoService.Stores.UpdateOneAsync(s => s.Id == store.Id, Builders<Store>.Update.Set(s => s.UserId, user.Id));
                    store.UserId = user.Id ?? "";
                    if (store.Id != null) AuthController.InMemoryStores[store.Id] = store;
                }

                // Trích xuất chính xác Email và Tên để gửi thông báo Gmail
                string cleanEmail = "";
                if (user != null && !string.IsNullOrWhiteSpace(user.PhoneEmail) && user.PhoneEmail.Contains("@"))
                {
                    cleanEmail = user.PhoneEmail.Trim().ToLower();
                }
                if (string.IsNullOrWhiteSpace(cleanEmail) && !string.IsNullOrWhiteSpace(store.PhoneEmail) && store.PhoneEmail.Contains("@"))
                {
                    cleanEmail = store.PhoneEmail.Trim().ToLower();
                }
                if (string.IsNullOrWhiteSpace(cleanEmail) && !string.IsNullOrWhiteSpace(store.UserId) && store.UserId.Contains("@"))
                {
                    cleanEmail = store.UserId.Trim().ToLower();
                }

                string cleanName = !string.IsNullOrWhiteSpace(user?.FullName) 
                    ? user.FullName.Trim() 
                    : (!string.IsNullOrWhiteSpace(store.OwnerFullName) 
                        ? store.OwnerFullName.Trim() 
                        : store.StoreName);

                string storeCode = !string.IsNullOrWhiteSpace(store.StoreCode) ? store.StoreCode : ("SEL-" + (store.Id?.Substring(Math.Max(0, store.Id.Length - 4)) ?? "8839").ToUpper());
                emailToSend = cleanEmail;

                // Gửi Gmail chúc mừng phê duyệt gian hàng thành công
                if (!string.IsNullOrWhiteSpace(cleanEmail))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string subject = $"[ZoneMart] 🎉 CHÚC MỪNG: Gian hàng \"{store.StoreName}\" của bạn đã được phê duyệt thành công!";
                            string htmlBody = $@"
                                <div style='background-color: #FFF7ED; padding: 32px 12px; font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif;'>
                                    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 580px; background: #FFFFFF; border-radius: 24px; border: 1.5px solid #FED7AA; box-shadow: 0 15px 35px rgba(217, 78, 21, 0.12); overflow: hidden; margin: 0 auto; padding: 32px 28px;'>
                                        <tr>
                                            <td align='center' style='padding-bottom: 20px; border-bottom: 1px solid #FFEDD5;'>
                                                <div style='font-size: 26px; font-weight: 900; color: #D94E15; letter-spacing: -0.5px;'>
                                                    Zone<span style='color: #10B981;'>Mart</span> <span style='font-size: 15px; color: #D94E15; background: #FFEDD5; padding: 3px 12px; border-radius: 20px;'>SELLER PORTAL</span>
                                                </div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td align='center' style='padding: 24px 0 16px 0;'>
                                                <h2 style='color: #C2410C; font-size: 22px; font-weight: 900; margin: 0 0 8px 0;'>🎉 HỒ SƠ GIAN HÀNG ĐÃ ĐƯỢC PHÊ DUYỆT!</h2>
                                                <p style='color: #475569; font-size: 15px; margin: 0;'>Xin chúc mừng chủ tiệm <b>{cleanName}</b>,</p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='color: #334155; font-size: 14.5px; line-height: 1.65;'>
                                                <p style='margin-bottom: 16px;'>
                                                    Ban quản lý <b>ZoneMart Seller</b> trân trọng thông báo: Hồ sơ đăng ký mở gian hàng đối tác của bạn đã được thẩm định đạt chuẩn và <b>CHÍNH THỨC ĐƯỢC PHÊ DUYỆT</b> trên toàn sàn!
                                                </p>

                                                <div style='background: linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 100%); border: 1.5px solid #FED7AA; border-radius: 18px; padding: 20px; margin: 20px 0;'>
                                                    <h4 style='color: #C2410C; margin: 0 0 12px 0; font-size: 15px;'>🏪 THÔNG TIN GIAN HÀNG KÍCH HOẠT:</h4>
                                                    <ul style='margin: 0; padding-left: 18px; color: #334155; font-size: 14px; line-height: 1.7;'>
                                                        <li><b>Tên Gian Hàng:</b> <span style='color: #D94E15; font-weight: 800;'>{store.StoreName}</span></li>
                                                        <li><b>Mã Định Danh (Seller ID):</b> <span style='color: #0284C7; font-weight: 700;'>{storeCode}</span></li>
                                                        <li><b>Tài Khoản / Gmail Đăng Nhập:</b> <b>{cleanEmail}</b></li>
                                                        <li><b>Ngành Hàng Kinh Doanh:</b> {store.Category}</li>
                                                        <li><b>Trạng Thái:</b> <span style='background: #DCFCE7; color: #15803D; padding: 2px 10px; border-radius: 12px; font-weight: 700; font-size: 13px;'>ĐÃ KÍCH HOẠT SẴN SÀNG MỞ BÁN</span></li>
                                                    </ul>
                                                </div>

                                                <div style='background: #F0FDF4; border-left: 4px solid #16A34A; padding: 14px 16px; margin: 18px 0; border-radius: 8px;'>
                                                    <p style='margin: 0; color: #166534; font-weight: bold; font-size: 13.5px;'>💡 HƯỚNG DẪN BẮT ĐẦU KINH DOANH:</p>
                                                    <ol style='margin: 6px 0 0 0; padding-left: 20px; color: #14532D; font-size: 13px; line-height: 1.6;'>
                                                        <li>Đăng nhập vào Kênh Người Bán (Seller Dashboard) với tài khoản Gmail của bạn.</li>
                                                        <li>Đăng tải các món ăn, thực phẩm tươi ngon và cập nhật bảng giá.</li>
                                                        <li>Bật công tắc nhận đơn để khách hàng xung quanh có thể đặt món ngay lập tức.</li>
                                                    </ol>
                                                </div>

                                                <div style='text-align: center; margin: 28px 0 16px 0;'>
                                                    <a href='http://localhost:5173/login?role=seller' style='background: #D94E15; color: #FFFFFF; font-size: 15px; font-weight: 800; text-decoration: none; padding: 14px 36px; border-radius: 30px; display: inline-block; box-shadow: 0 8px 20px rgba(217, 78, 21, 0.3);'>
                                                        BẮT ĐẦU MỞ BÁN NGAY BÂY GIỜ ➔
                                                    </a>
                                                </div>

                                                <p style='color: #64748B; font-size: 12.5px; text-align: center; margin-top: 14px;'>
                                                    <i>Lưu ý: Quý đối tác vui lòng luôn nghiêm túc tuân thủ quy chuẩn An toàn vệ sinh thực phẩm và quy chế hoạt động của ZoneMart.</i>
                                                </p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td align='center' style='padding-top: 24px; border-top: 1px solid #FFEDD5; color: #94A3B8; font-size: 12px;'>
                                                Trân trọng,<br />
                                                <b style='color: #D94E15; font-size: 13.5px;'>BAN QUẢN LÝ ĐỐI TÁC GIAN HÀNG ZONEMART</b><br />
                                                Hotline Hỗ Trợ: 1900 6868 • Email: dobinh225599@gmail.com
                                            </td>
                                        </tr>
                                    </table>
                                </div>";

                            Console.WriteLine($"📧 [Approve KYC] Đang gửi Gmail chúc mừng tới {cleanEmail} ({cleanName})...");
                            bool sent = await SendEmailViaMailKitAsync(cleanEmail, subject, htmlBody, "ZoneMart Support");
                            Console.WriteLine(sent ? $"✅ [Approve KYC] Gửi Gmail thành công tới {cleanEmail}!" : $"⚠️ [Approve KYC] Không thể gửi Gmail tới {cleanEmail}!");
                        }
                        catch (Exception mailEx)
                        {
                            Console.WriteLine($"⚠️ [Approve KYC Mail Error]: {mailEx.Message}");
                        }
                    });
                }
            }
            else if (req.Type.ToLower() == "shipper")
            {
                var shipperFilter = Builders<Shipper>.Filter.Or(
                    Builders<Shipper>.Filter.Eq(s => s.Id, req.TargetId),
                    Builders<Shipper>.Filter.Eq(s => s.ShipperCode, req.TargetId),
                    Builders<Shipper>.Filter.Eq(s => s.UserId, req.TargetId),
                    Builders<Shipper>.Filter.Eq(s => s.PhoneNumber, req.TargetId)
                );
                var shipper = await _mongoService.Shippers.Find(shipperFilter).FirstOrDefaultAsync();
                if (shipper == null) return NotFound(new { success = false, message = "Không tìm thấy hồ sơ Tài xế!" });

                var shipperUpdate = Builders<Shipper>.Update
                    .Set(s => s.Status, "Approved")
                    .Set(s => s.RejectReason, null);
                await _mongoService.Shippers.UpdateOneAsync(s => s.Id == shipper.Id, shipperUpdate);
                shipper.Status = "Approved";
                if (shipper.Id != null) AuthController.InMemoryShippers[shipper.Id] = shipper;
                if (!string.IsNullOrEmpty(shipper.ShipperCode)) AuthController.InMemoryShippers[shipper.ShipperCode] = shipper;

                var filterList = new List<FilterDefinition<User>>();
                if (!string.IsNullOrEmpty(shipper.UserId) && MongoDB.Bson.ObjectId.TryParse(shipper.UserId, out _))
                {
                    filterList.Add(Builders<User>.Filter.Eq(u => u.Id, shipper.UserId));
                }
                if (!string.IsNullOrEmpty(shipper.UserId))
                {
                    filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, shipper.UserId));
                }
                if (!string.IsNullOrEmpty(shipper.ShipperCode))
                {
                    filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, shipper.ShipperCode));
                }
                if (!string.IsNullOrEmpty(shipper.PhoneNumber))
                {
                    string cleanPhone = shipper.PhoneNumber.Trim().ToLower();
                    filterList.Add(Builders<User>.Filter.Eq(u => u.PhoneEmail, cleanPhone));
                    filterList.Add(Builders<User>.Filter.Regex(u => u.PhoneEmail, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleanPhone)}$", "i")));
                    filterList.Add(Builders<User>.Filter.Eq(u => u.Phone, shipper.PhoneNumber.Trim()));
                }
                var user = filterList.Count > 0 ? await _mongoService.Users.Find(Builders<User>.Filter.Or(filterList)).FirstOrDefaultAsync() : null;
                if (user != null)
                {
                    string targetUserCode = !string.IsNullOrEmpty(shipper.ShipperCode)
                        ? shipper.ShipperCode
                        : ((!string.IsNullOrEmpty(user.UserCode) && user.UserCode.StartsWith("CUST-")) ? $"SHP-{Random.Shared.Next(1000, 9999)}" : user.UserCode);

                    var userUpdate = Builders<User>.Update
                        .Set(u => u.IsShipper, true)
                        .Set(u => u.AccountStatus, "active")
                        .Set(u => u.UserCode, targetUserCode);
                    await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, userUpdate);

                    user.IsShipper = true;
                    user.AccountStatus = "active";
                    user.UserCode = targetUserCode;
                    if (!string.IsNullOrEmpty(user.PhoneEmail)) AuthController.InMemoryUsers[user.PhoneEmail] = user;

                    await _mongoService.Shippers.UpdateOneAsync(s => s.Id == shipper.Id, Builders<Shipper>.Update.Set(s => s.UserId, user.Id));
                    shipper.UserId = user.Id ?? "";
                    if (shipper.Id != null) AuthController.InMemoryShippers[shipper.Id] = shipper;
                }

                string cleanEmail = "";
                if (user != null && !string.IsNullOrWhiteSpace(user.PhoneEmail) && user.PhoneEmail.Contains("@"))
                {
                    cleanEmail = user.PhoneEmail.Trim().ToLower();
                }
                if (string.IsNullOrWhiteSpace(cleanEmail) && !string.IsNullOrWhiteSpace(shipper.PhoneNumber) && shipper.PhoneNumber.Contains("@"))
                {
                    cleanEmail = shipper.PhoneNumber.Trim().ToLower();
                }
                if (string.IsNullOrWhiteSpace(cleanEmail) && !string.IsNullOrWhiteSpace(shipper.UserId) && shipper.UserId.Contains("@"))
                {
                    cleanEmail = shipper.UserId.Trim().ToLower();
                }

                string cleanName = !string.IsNullOrWhiteSpace(user?.FullName) 
                    ? user.FullName.Trim() 
                    : (!string.IsNullOrWhiteSpace(shipper.FullName) 
                        ? shipper.FullName.Trim() 
                        : "Quý Tài Xế");

                string shipperCode = string.IsNullOrEmpty(shipper.ShipperCode) ? $"ZM-DRV-8839" : shipper.ShipperCode;
                emailToSend = cleanEmail;

                if (!string.IsNullOrWhiteSpace(cleanEmail))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string subject = $"[ZoneMart Driver] 🎉 THÔNG BÁO DUYỆT HỒ SƠ TÀI XẾ #{shipperCode} THÀNH CÔNG!";
                            string htmlBody = $@"
                                <div style='background-color: #F0F9FF; padding: 32px 12px; font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif;'>
                                    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 580px; background: #FFFFFF; border-radius: 24px; border: 1.5px solid #BAE6FD; box-shadow: 0 15px 35px rgba(2, 132, 199, 0.12); overflow: hidden; margin: 0 auto; padding: 32px 28px;'>
                                        <tr>
                                            <td align='center' style='padding-bottom: 20px; border-bottom: 1px solid #E0F2FE;'>
                                                <div style='font-size: 26px; font-weight: 900; color: #0284C7; letter-spacing: -0.5px;'>
                                                    Zone<span style='color: #D94E15;'>Mart</span> <span style='font-size: 16px; color: #0284C7; background: #E0F2FE; padding: 3px 10px; border-radius: 20px;'>DRIVER PORTAL</span>
                                                </div>
                                            </td>
                                        </tr>

                                        <tr>
                                            <td align='center' style='padding: 24px 0 16px 0;'>
                                                <h2 style='color: #0369A1; font-size: 22px; font-weight: 900; margin: 0 0 8px 0;'>🎉 HỒ SƠ ĐÃ ĐƯỢC PHÊ DUYỆT CHÍNH THỨC!</h2>
                                                <p style='color: #475569; font-size: 15px; margin: 0;'>Xin chúc mừng tài xế <b>{cleanName}</b>,</p>
                                            </td>
                                        </tr>

                                        <tr>
                                            <td style='color: #334155; font-size: 14.5px; line-height: 1.65;'>
                                                <p style='margin-bottom: 16px;'>
                                                    Ban quản lý <b>ZoneMart Driver</b> xin vui mừng thông báo: Hồ sơ đăng ký tài xế giao hàng đối tác của bạn đã được thẩm định và <b>DUYỆT THÀNH CÔNG</b>!
                                                </p>

                                                <div style='background: linear-gradient(135deg, #F0F9FF 0%, #E0F2FE 100%); border: 1.5px solid #BAE6FD; border-radius: 18px; padding: 20px; margin: 20px 0;'>
                                                    <h4 style='color: #0369A1; margin: 0 0 12px 0; font-size: 15px;'>🪪 THÔNG TIN TÀI KHOẢN TÀI XẾ CỦA BẠN:</h4>
                                                    <ul style='margin: 0; padding-left: 18px; color: #334155; font-size: 14px; line-height: 1.7;'>
                                                        <li><b>Mã Định Danh Tài Xế (ID):</b> <span style='color: #0284C7; font-weight: 800;'>{shipperCode}</span></li>
                                                        <li><b>Biển Số Xe Đăng Ký:</b> <span style='color: #D94E15; font-weight: 700;'>{shipper.LicensePlate}</span></li>
                                                        <li><b>Khu Vực Chạy Chính:</b> {shipper.OperatingArea}</li>
                                                        <li><b>Trạng Thái:</b> <span style='background: #DCFCE7; color: #15803D; padding: 2px 10px; border-radius: 12px; font-weight: 700; font-size: 13px;'>ĐÃ KÍCH HOẠT SẴN SÀNG NHẬN ĐƠN</span></li>
                                                    </ul>
                                                </div>

                                                <p style='margin-bottom: 20px; text-align: center;'>
                                                    Bạn có thể dùng Số điện thoại/Gmail đăng ký và Mật khẩu vừa tạo để đăng nhập ngay vào ứng dụng ZoneMart Driver.
                                                </p>

                                                <div style='text-align: center; margin: 24px 0;'>
                                                    <a href='http://localhost:5173/login?role=shipper' style='background: #0284C7; color: #FFFFFF; font-size: 15px; font-weight: 800; text-decoration: none; padding: 14px 36px; border-radius: 30px; display: inline-block; box-shadow: 0 8px 20px rgba(2, 132, 199, 0.3);'>
                                                        ĐĂNG NHẬP ZONEMART DRIVER NGAY ➔
                                                    </a>
                                                </div>
                                            </td>
                                        </tr>

                                        <tr>
                                            <td align='center' style='padding-top: 24px; border-top: 1px solid #E0F2FE; color: #94A3B8; font-size: 12px;'>
                                                Trân trọng,<br />
                                                <b style='color: #0284C7; font-size: 13.5px;'>BAN ĐIỀU HÀNH ĐỘI TÀI XẾ ZONEMART</b><br />
                                                Hotline Hỗ Trợ: 1900 6868 • Email: dobinh225599@gmail.com
                                            </td>
                                        </tr>
                                    </table>
                                </div>";

                            Console.WriteLine($"📧 [Approve KYC] Đang gửi Gmail chúc mừng tới {cleanEmail} ({cleanName})...");
                            bool sent = await SendEmailViaMailKitAsync(cleanEmail, subject, htmlBody, "ZoneMart Fleet");
                            Console.WriteLine(sent ? $"✅ [Approve KYC] Gửi Gmail thành công tới {cleanEmail}!" : $"⚠️ [Approve KYC] Không thể gửi Gmail tới {cleanEmail}!");
                        }
                        catch (Exception mailEx)
                        {
                            Console.WriteLine($"⚠️ [Approve KYC Mail Error]: {mailEx.Message}");
                        }
                    });
                }
            }

            // Lưu Nhật ký Audit Log
            await LogAuditAsync(req.ActorEmail, req.ActorRole, "Phê duyệt KYC", emailToSend, $"Phê duyệt thành công hồ sơ {req.Type} (#{req.TargetId})");

            return Ok(new { success = true, message = "Phê duyệt hồ sơ thành công! Đã cập nhật vai trò và gửi Gmail thông báo." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Từ chối hồ sơ kèm lý do bắt buộc (Cả Admin và Quản lý)
    /// </summary>
    [HttpPost("reject-kyc")]
    public async Task<IActionResult> RejectKyc([FromBody] RejectKycRequest req)
    {
        InvalidateAdminCache();
        if (string.IsNullOrWhiteSpace(req.Reason))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập lý do từ chối hồ sơ!" });
        }

        try
        {
            string emailToSend = "";
            string nameToEmail = "";

            if (req.Type.ToLower() == "seller")
            {
                var storeFilter = Builders<Store>.Filter.Or(
                    Builders<Store>.Filter.Eq(s => s.Id, req.TargetId),
                    Builders<Store>.Filter.Eq(s => s.StoreCode, req.TargetId),
                    Builders<Store>.Filter.Eq(s => s.UserId, req.TargetId),
                    Builders<Store>.Filter.Eq(s => s.PhoneEmail, req.TargetId)
                );
                var store = await _mongoService.Stores.Find(storeFilter).FirstOrDefaultAsync();
                if (store != null)
                {
                    var storeUpdate = Builders<Store>.Update
                        .Set(s => s.Status, "Rejected")
                        .Set(s => s.RejectReason, req.Reason);
                    await _mongoService.Stores.UpdateOneAsync(s => s.Id == store.Id, storeUpdate);
                    store.Status = "Rejected";
                    store.RejectReason = req.Reason;
                    if (store.Id != null) AuthController.InMemoryStores[store.Id] = store;
                    if (!string.IsNullOrEmpty(store.StoreCode)) AuthController.InMemoryStores[store.StoreCode] = store;

                    var filterList = new List<FilterDefinition<User>>();
                    if (!string.IsNullOrEmpty(store.UserId) && MongoDB.Bson.ObjectId.TryParse(store.UserId, out _))
                    {
                        filterList.Add(Builders<User>.Filter.Eq(u => u.Id, store.UserId));
                    }
                    if (!string.IsNullOrEmpty(store.UserId))
                    {
                        filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, store.UserId));
                    }
                    if (!string.IsNullOrEmpty(store.StoreCode))
                    {
                        filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, store.StoreCode));
                    }
                    if (!string.IsNullOrEmpty(store.PhoneEmail))
                    {
                        string cleanMail = store.PhoneEmail.Trim().ToLower();
                        filterList.Add(Builders<User>.Filter.Eq(u => u.PhoneEmail, cleanMail));
                        filterList.Add(Builders<User>.Filter.Regex(u => u.PhoneEmail, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleanMail)}$", "i")));
                        filterList.Add(Builders<User>.Filter.Eq(u => u.Phone, store.PhoneEmail.Trim()));
                    }
                    var user = filterList.Count > 0 ? await _mongoService.Users.Find(Builders<User>.Filter.Or(filterList)).FirstOrDefaultAsync() : null;
                    if (user != null)
                    {
                        var userUpdate = Builders<User>.Update.Set(u => u.AccountStatus, "rejected");
                        await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, userUpdate);
                        user.AccountStatus = "rejected";
                    }
                    emailToSend = (!string.IsNullOrEmpty(user?.PhoneEmail) && user.PhoneEmail.Contains("@")) 
                        ? user.PhoneEmail 
                        : ((!string.IsNullOrEmpty(store.PhoneEmail) && store.PhoneEmail.Contains("@")) ? store.PhoneEmail : "");
                    nameToEmail = user?.FullName ?? store.OwnerFullName;

                    // Gửi Gmail thông báo từ chối hồ sơ Seller
                    _ = Task.Run(async () =>
                    {
                        string subject = "[ZoneMart Seller] THONG BAO KET QUA THAM DINH HO SO GIAN HANG";
                        string htmlBody = $@"
                            <div style='background-color: #FEF2F2; padding: 32px 12px; font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif;'>
                                <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 580px; background: #FFFFFF; border-radius: 24px; border: 1.5px solid #FECACA; box-shadow: 0 15px 35px rgba(220, 38, 38, 0.1); overflow: hidden; margin: 0 auto; padding: 32px 28px;'>
                                    <tr>
                                        <td align='center' style='padding-bottom: 20px; border-bottom: 1px solid #FEE2E2;'>
                                            <div style='font-size: 26px; font-weight: 900; color: #DC2626; letter-spacing: -0.5px;'>
                                                Zone<span style='color: #D94E15;'>Mart</span> <span style='font-size: 16px; color: #DC2626; background: #FEE2E2; padding: 3px 10px; border-radius: 20px;'>SELLER SUPPORT</span>
                                            </div>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td align='center' style='padding: 24px 0 16px 0;'>
                                            <h2 style='color: #B91C1C; font-size: 21px; font-weight: 900; margin: 0 0 8px 0;'>HỒ SƠ GIAN HÀNG CHƯA ĐẠT YÊU CẦU THẨM ĐỊNH</h2>
                                            <p style='color: #475569; font-size: 15px; margin: 0;'>Kính gửi chủ tiệm <b>{nameToEmail}</b> (Gian hàng: <b style='color: #DC2626;'>{store.StoreName}</b>),</p>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='color: #334155; font-size: 14.5px; line-height: 1.65;'>
                                            <p style='margin-bottom: 16px;'>
                                                Ban quản lý <b>ZoneMart Seller</b> đã tiến hành thẩm định hồ sơ đăng ký mở gian hàng của bạn. Rất tiếc, hồ sơ hiện tại <b>CHƯA ĐƯỢC PHÊ DUYỆT</b> do phát hiện một số sai sót hoặc thiếu giấy tờ cần thiết sau đây:
                                            </p>

                                            <div style='background: #FEF2F2; border-left: 5px solid #DC2626; border-radius: 12px; padding: 18px; margin: 20px 0;'>
                                                <h4 style='color: #991B1B; margin: 0 0 8px 0; font-size: 15px;'>📌 LÝ DO CHI TIẾT TỪ BAN THẨM ĐỊNH:</h4>
                                                <p style='margin: 0; color: #7F1D1D; font-size: 14px; font-weight: bold; line-height: 1.6;'>
                                                    &quot;{req.Reason}&quot;
                                                </p>
                                            </div>

                                            <p style='margin-bottom: 16px;'>
                                                <b>Hướng dẫn khắc phục:</b> Bạn vui lòng kiểm tra lại ảnh chụp CCCD, Giấy chứng nhận An toàn thực phẩm hoặc điều chỉnh thông tin chính xác theo lý do trên và đăng ký nộp lại hồ sơ.
                                            </p>

                                            <div style='text-align: center; margin: 24px 0;'>
                                                <a href='http://localhost:5173/register-seller' style='background: #DC2626; color: #FFFFFF; font-size: 15px; font-weight: 800; text-decoration: none; padding: 14px 36px; border-radius: 30px; display: inline-block; box-shadow: 0 8px 20px rgba(220, 38, 38, 0.25);'>
                                                    CẬP NHẬT & NỘP LẠI HỒ SƠ ➔
                                                </a>
                                            </div>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td align='center' style='padding-top: 24px; border-top: 1px solid #FEE2E2; color: #94A3B8; font-size: 12px;'>
                                            Trân trọng,<br />
                                            <b style='color: #B91C1C; font-size: 13.5px;'>BAN QUẢN LÝ GIAN HÀNG ZONEMART SELLER</b><br />
                                            Hotline Hỗ Trợ: 1900 6868 • Email: hh9393100@gmail.com
                                        </td>
                                    </tr>
                                </table>
                            </div>";

                        if (!string.IsNullOrWhiteSpace(emailToSend) && emailToSend.Contains("@"))
                        {
                            await SendEmailViaMailKitAsync(emailToSend, subject, htmlBody, "ZoneMart Support");
                        }
                    });
                }
            }
            else if (req.Type.ToLower() == "shipper")
            {
                var shipperFilter = Builders<Shipper>.Filter.Or(
                    Builders<Shipper>.Filter.Eq(s => s.Id, req.TargetId),
                    Builders<Shipper>.Filter.Eq(s => s.ShipperCode, req.TargetId),
                    Builders<Shipper>.Filter.Eq(s => s.UserId, req.TargetId),
                    Builders<Shipper>.Filter.Eq(s => s.PhoneNumber, req.TargetId)
                );
                var shipper = await _mongoService.Shippers.Find(shipperFilter).FirstOrDefaultAsync();
                if (shipper != null)
                {
                    var shipperUpdate = Builders<Shipper>.Update
                        .Set(s => s.Status, "Rejected")
                        .Set(s => s.RejectReason, req.Reason);
                    await _mongoService.Shippers.UpdateOneAsync(s => s.Id == shipper.Id, shipperUpdate);
                    shipper.Status = "Rejected";
                    shipper.RejectReason = req.Reason;
                    if (shipper.Id != null) AuthController.InMemoryShippers[shipper.Id] = shipper;
                    if (!string.IsNullOrEmpty(shipper.ShipperCode)) AuthController.InMemoryShippers[shipper.ShipperCode] = shipper;

                    var filterList = new List<FilterDefinition<User>>();
                    if (!string.IsNullOrEmpty(shipper.UserId) && MongoDB.Bson.ObjectId.TryParse(shipper.UserId, out _))
                    {
                        filterList.Add(Builders<User>.Filter.Eq(u => u.Id, shipper.UserId));
                    }
                    if (!string.IsNullOrEmpty(shipper.UserId))
                    {
                        filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, shipper.UserId));
                    }
                    if (!string.IsNullOrEmpty(shipper.ShipperCode))
                    {
                        filterList.Add(Builders<User>.Filter.Eq(u => u.UserCode, shipper.ShipperCode));
                    }
                    if (!string.IsNullOrEmpty(shipper.PhoneNumber))
                    {
                        string cleanPhone = shipper.PhoneNumber.Trim().ToLower();
                        filterList.Add(Builders<User>.Filter.Eq(u => u.PhoneEmail, cleanPhone));
                        filterList.Add(Builders<User>.Filter.Regex(u => u.PhoneEmail, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleanPhone)}$", "i")));
                        filterList.Add(Builders<User>.Filter.Eq(u => u.Phone, shipper.PhoneNumber.Trim()));
                    }
                    var user = filterList.Count > 0 ? await _mongoService.Users.Find(Builders<User>.Filter.Or(filterList)).FirstOrDefaultAsync() : null;
                    if (user != null)
                    {
                        var userUpdate = Builders<User>.Update.Set(u => u.AccountStatus, "rejected");
                        await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, userUpdate);
                        user.AccountStatus = "rejected";
                    }
                    emailToSend = (!string.IsNullOrEmpty(user?.PhoneEmail) && user.PhoneEmail.Contains("@")) 
                        ? user.PhoneEmail 
                        : (shipper.PhoneNumber.Contains("@") ? shipper.PhoneNumber : "");
                    nameToEmail = user?.FullName ?? shipper.FullName;

                    // Gửi Gmail thông báo lý do từ chối Shipper
                    _ = Task.Run(async () =>
                    {
                        string subject = "[ZoneMart Driver] THONG BAO THAM DINH HO SO (CAN BO SUNG GIAY TO)";
                        string htmlBody = $@"
                            <div style='background-color: #FEF2F2; padding: 32px 12px; font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif;'>
                                <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 580px; background: #FFFFFF; border-radius: 24px; border: 1.5px solid #FECACA; box-shadow: 0 15px 35px rgba(220, 38, 38, 0.1); overflow: hidden; margin: 0 auto; padding: 32px 28px;'>
                                    <tr>
                                        <td align='center' style='padding-bottom: 20px; border-bottom: 1px solid #FEE2E2;'>
                                            <div style='font-size: 26px; font-weight: 900; color: #DC2626; letter-spacing: -0.5px;'>
                                                Zone<span style='color: #D94E15;'>Mart</span> <span style='font-size: 16px; color: #DC2626; background: #FEE2E2; padding: 3px 10px; border-radius: 20px;'>DRIVER SUPPORT</span>
                                            </div>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td align='center' style='padding: 24px 0 16px 0;'>
                                            <h2 style='color: #B91C1C; font-size: 21px; font-weight: 900; margin: 0 0 8px 0;'>HỒ SƠ ĐĂNG KÝ CHƯA ĐẠT YÊU CẦU THẨM ĐỊNH</h2>
                                            <p style='color: #475569; font-size: 15px; margin: 0;'>Kính gửi <b>{nameToEmail}</b>,</p>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='color: #334155; font-size: 14.5px; line-height: 1.65;'>
                                            <p style='margin-bottom: 16px;'>
                                                Ban quản lý <b>ZoneMart Driver</b> đã tiến hành thẩm định hồ sơ đăng ký tài xế của bạn. Rất tiếc, hồ sơ hiện tại <b>CHƯA ĐƯỢC PHÊ DUYỆT</b> do phát hiện một số sai sót hoặc thiếu thông tin sau đây:
                                            </p>

                                            <div style='background: #FEF2F2; border-left: 5px solid #DC2626; border-radius: 12px; padding: 18px; margin: 20px 0;'>
                                                <h4 style='color: #991B1B; margin: 0 0 8px 0; font-size: 15px;'>📌 LÝ DO CHI TIẾT TỪ BAN THẨM ĐỊNH:</h4>
                                                <p style='margin: 0; color: #7F1D1D; font-size: 14px; font-weight: bold; line-height: 1.6;'>
                                                    &quot;{req.Reason}&quot;
                                                </p>
                                            </div>

                                            <p style='margin-bottom: 16px;'>
                                                <b>Hướng dẫn khắc phục:</b> Bạn vui lòng truy cập lại Cổng đăng ký tài xế ZoneMart Driver, chụp lại ảnh CCCD / GPLX hoặc điều chỉnh thông tin chính xác theo lý do trên và bấm nộp lại hồ sơ.
                                            </p>

                                            <div style='text-align: center; margin: 24px 0;'>
                                                <a href='http://localhost:5173/register-shipper#shipper-register-card' style='background: #DC2626; color: #FFFFFF; font-size: 15px; font-weight: 800; text-decoration: none; padding: 14px 36px; border-radius: 30px; display: inline-block; box-shadow: 0 8px 20px rgba(220, 38, 38, 0.25);'>
                                                    CẬP NHẬT & NỘP LẠI HỒ SƠ ➔
                                                </a>
                                            </div>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td align='center' style='padding-top: 24px; border-top: 1px solid #FEE2E2; color: #94A3B8; font-size: 12px;'>
                                            Trân trọng,<br />
                                            <b style='color: #B91C1C; font-size: 13.5px;'>BAN QUẢN LÝ TÀI XẾ ZONEMART DRIVER</b><br />
                                            Hotline Hỗ Trợ: 1900 6868 • Email: dobinh225599@gmail.com
                                        </td>
                                    </tr>
                                </table>
                            </div>";

                        if (!string.IsNullOrWhiteSpace(emailToSend) && emailToSend.Contains("@"))
                        {
                            await SendEmailViaMailKitAsync(emailToSend, subject, htmlBody, "ZoneMart Fleet");
                        }
                    });
                }
            }

            // Lưu Nhật ký Audit Log
            await LogAuditAsync(req.ActorEmail, req.ActorRole, "Từ chối KYC", emailToSend, $"Từ chối hồ sơ {req.Type} (#{req.TargetId}), lý do: {req.Reason}");

            return Ok(new { success = true, message = "Đã từ chối hồ sơ và gửi email lý do chi tiết cho người dùng." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Chế tài xử lý tài khoản vi phạm (3 mức độ: Khóa cảnh cáo 10 ngày, Tạm đình chỉ 30 ngày, Cấm vĩnh viễn)
    /// </summary>
    [HttpPost("discipline-user")]
    [HttpPost("punish-user")]
    public async Task<IActionResult> PunishUser([FromBody] PunishUserRequest req)
    {
        try
        {
            string userEmail = !string.IsNullOrWhiteSpace(req.UserEmail) ? req.UserEmail.Trim().ToLower() : "";
            string targetUserId = req.UserId?.Trim() ?? "";

            // 1. Tìm trong Users
            User? user = null;
            if (MongoDB.Bson.ObjectId.TryParse(targetUserId, out _))
            {
                user = await _mongoService.Users.Find(u => u.Id == targetUserId).FirstOrDefaultAsync();
            }
            if (user == null)
            {
                user = await _mongoService.Users.Find(u => u.UserCode == targetUserId || u.PhoneEmail == targetUserId).FirstOrDefaultAsync();
            }
            if (user == null && !string.IsNullOrEmpty(userEmail))
            {
                user = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == userEmail).FirstOrDefaultAsync();
            }

            // 2. Fallback tìm theo Store
            if (user == null)
            {
                Store? st = null;
                if (MongoDB.Bson.ObjectId.TryParse(targetUserId, out _)) st = await _mongoService.Stores.Find(s => s.Id == targetUserId).FirstOrDefaultAsync();
                if (st == null) st = await _mongoService.Stores.Find(s => s.StoreCode == targetUserId || s.PhoneEmail == targetUserId).FirstOrDefaultAsync();
                if (st == null && !string.IsNullOrEmpty(userEmail)) st = await _mongoService.Stores.Find(s => s.PhoneEmail == userEmail).FirstOrDefaultAsync();

                if (st != null)
                {
                    userEmail = st.PhoneEmail ?? userEmail;
                    user = await _mongoService.Users.Find(u => u.PhoneEmail == st.PhoneEmail || u.Id == st.UserId).FirstOrDefaultAsync();
                }
            }

            // 3. Fallback tìm theo Shipper
            if (user == null)
            {
                Shipper? sh = null;
                if (MongoDB.Bson.ObjectId.TryParse(targetUserId, out _)) sh = await _mongoService.Shippers.Find(s => s.Id == targetUserId).FirstOrDefaultAsync();
                if (sh == null) sh = await _mongoService.Shippers.Find(s => s.ShipperCode == targetUserId || s.PhoneNumber == targetUserId).FirstOrDefaultAsync();
                if (sh == null && !string.IsNullOrEmpty(userEmail)) sh = await _mongoService.Shippers.Find(s => s.PhoneNumber == userEmail).FirstOrDefaultAsync();

                if (sh != null)
                {
                    userEmail = sh.PhoneNumber ?? userEmail;
                    user = await _mongoService.Users.Find(u => u.PhoneEmail == sh.PhoneNumber || u.Id == sh.UserId).FirstOrDefaultAsync();
                }
            }

            if (user == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản để xử lý kỷ luật!" });

            string statusMsg = "";
            string newStatus = "locked_10_days";
            DateTime? lockTime = DateTime.UtcNow.AddDays(10);

            if (req.Level == 1)
            {
                // Mức 1: Khóa cảnh cáo 10 ngày (240 giờ)
                newStatus = "locked_10_days";
                lockTime = DateTime.UtcNow.AddDays(10);
                statusMsg = $"Đã áp dụng chế tài: Khóa cảnh cáo tài khoản 10 ngày (240 giờ) đối với {user.FullName}.";
            }
            else if (req.Level == 2)
            {
                // Mức 2: Tạm đình chỉ vận hành sàn
                newStatus = "suspended";
                lockTime = DateTime.UtcNow.AddDays(30);
                statusMsg = $"Đã áp dụng chế tài: Tạm đình chỉ vận hành sàn 30 ngày đối với {user.FullName}.";
            }
            else if (req.Level == 3)
            {
                // Mức 3: Cấm vĩnh viễn (Permanent Ban)
                newStatus = "banned";
                lockTime = null;
                statusMsg = $"Đã áp dụng chế tài: Cấm tài khoản vĩnh viễn đối với {user.FullName}.";
            }

            var update = Builders<User>.Update
                .Set(u => u.AccountStatus, newStatus)
                .Set(u => u.LockUntil, lockTime)
                .Inc(u => u.ViolationCount, 1);
            await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, update);

            // Đồng bộ bộ nhớ In-Memory
            if (!string.IsNullOrEmpty(user.PhoneEmail) && AuthController.InMemoryUsers.TryGetValue(user.PhoneEmail, out var inMemUser))
            {
                inMemUser.AccountStatus = newStatus;
                inMemUser.LockUntil = lockTime;
                inMemUser.ViolationCount += 1;
            }

            // Đồng bộ Store/Shipper nếu bị khóa hoặc đình chỉ
            if (newStatus == "banned" || newStatus == "suspended" || newStatus == "locked_10_days")
            {
                string entityStatus = newStatus == "banned" ? "Banned" : "Suspended";
                await _mongoService.Stores.UpdateManyAsync(
                    s => s.UserId == user.Id || s.PhoneEmail == user.PhoneEmail,
                    Builders<Store>.Update.Set(s => s.Status, entityStatus)
                );
                await _mongoService.Shippers.UpdateManyAsync(
                    sh => sh.UserId == user.Id || sh.PhoneNumber == user.PhoneEmail,
                    Builders<Shipper>.Update.Set(sh => sh.Status, entityStatus)
                );
            }

            // Gửi Gmail thông báo chế tài
            _ = Task.Run(async () =>
            {
                try
                {
                    string subject = $"[ZoneMart] Thông Báo Chế Tài Vi Phạm Quy Định (Mức {req.Level})";
                    string htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; background: #fafafa; padding: 24px;'>
                            <div style='max-width: 500px; margin: 0 auto; background: #ffffff; border-radius: 16px; padding: 24px; border: 1px solid #e5e7eb;'>
                                <h2 style='color: #dc2626;'>🚫 THÔNG BÁO XỬ LÝ VI PHẠM SÀN ZONEMART</h2>
                                <p>Xin chào <b>{user.FullName}</b> ({user.PhoneEmail}),</p>
                                <p>Hệ thống ghi nhận tài khoản của bạn đã vi phạm quy định vận hành của ZoneMart:</p>
                                <div style='background: #fef2f2; border: 1px solid #fca5a5; padding: 12px; border-radius: 8px; font-weight: bold; color: #991b1b; margin: 12px 0;'>
                                    Mức độ xử lý: Mức {req.Level}<br/>
                                    Lý do: {req.Reason}
                                </div>
                                <p>{(req.Level == 1 ? "Tài khoản của bạn tạm ngưng phục vụ trong 10 ngày (240 giờ)." : req.Level == 2 ? "Tài khoản của bạn tạm đình chỉ hoạt động trong 30 ngày." : "Tài khoản của bạn đã bị cấm vĩnh viễn khỏi nền tảng ZoneMart.")}</p>
                            </div>
                        </div>";
                    await SendEmailViaMailKitAsync(user.PhoneEmail, subject, htmlBody, "ZoneMart Security");
                }
                catch { }
            });

            await LogAuditAsync(req.ActorEmail, req.ActorRole, $"Chế tài Mức {req.Level}", user.PhoneEmail, $"Lý do: {req.Reason}. Status mới: {newStatus}");

            return Ok(new { success = true, message = statusMsg, accountStatus = newStatus, lockUntil = lockTime });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Mở khóa tài khoản (Unlock) -> Phục hồi về Active
    /// </summary>
    [HttpPost("unlock-user")]
    public async Task<IActionResult> UnlockUser([FromBody] UnlockUserRequest req)
    {
        InvalidateAdminCache();
        try
        {
            string userEmail = !string.IsNullOrWhiteSpace(req.UserEmail) ? req.UserEmail.Trim().ToLower() : "";
            string targetUserId = req.UserId?.Trim() ?? "";

            User? user = null;
            if (MongoDB.Bson.ObjectId.TryParse(targetUserId, out _))
            {
                user = await _mongoService.Users.Find(u => u.Id == targetUserId).FirstOrDefaultAsync();
            }
            if (user == null)
            {
                user = await _mongoService.Users.Find(u => u.UserCode == targetUserId || u.PhoneEmail == targetUserId).FirstOrDefaultAsync();
            }
            if (user == null && !string.IsNullOrEmpty(userEmail))
            {
                user = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == userEmail).FirstOrDefaultAsync();
            }

            if (user == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản!" });

            var update = Builders<User>.Update.Set(u => u.AccountStatus, "active").Unset(u => u.LockUntil);
            await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, update);

            // Đồng bộ In-Memory
            if (!string.IsNullOrEmpty(user.PhoneEmail) && AuthController.InMemoryUsers.TryGetValue(user.PhoneEmail, out var inMemUser))
            {
                inMemUser.AccountStatus = "active";
                inMemUser.LockUntil = null;
            }

            // Phục hồi Store/Shipper về Active
            await _mongoService.Stores.UpdateManyAsync(
                s => s.UserId == user.Id || s.PhoneEmail == user.PhoneEmail,
                Builders<Store>.Update.Set(s => s.Status, "Active")
            );
            await _mongoService.Shippers.UpdateManyAsync(
                sh => sh.UserId == user.Id || sh.PhoneNumber == user.PhoneEmail,
                Builders<Shipper>.Update.Set(sh => sh.Status, "Active")
            );

            _ = Task.Run(async () =>
            {
                try
                {
                    string subject = "[ZoneMart] Thông Báo Phục Hồi Tài Khoản Hoạt Động Trở Lại";
                    string htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; background: #f0fdf4; padding: 24px;'>
                            <div style='max-width: 500px; margin: 0 auto; background: #ffffff; border-radius: 16px; padding: 24px; border: 1px solid #bbf7d0;'>
                                <h2 style='color: #16a34a;'>🔓 TÀI KHOẢN CỦA BẠN ĐÃ ĐƯỢC MỞ KHÓA!</h2>
                                <p>Xin chào <b>{user.FullName}</b>,</p>
                                <p>Ban Quản Lý ZoneMart đã phê duyệt mở khóa tài khoản <b>{user.PhoneEmail}</b>. Trạng thái tài khoản của bạn hiện là <b>Active (Đang hoạt động)</b>.</p>
                                <p>Bạn có thể đăng nhập và sử dụng đầy đủ tính năng bình thường.</p>
                            </div>
                        </div>";
                    await SendEmailViaMailKitAsync(user.PhoneEmail, subject, htmlBody, "ZoneMart Security");
                }
                catch { }
            });

            await LogAuditAsync(req.ActorEmail, req.ActorRole, "Mở khóa tài khoản", user.PhoneEmail, "Khôi phục trạng thái Active");

            return Ok(new { success = true, message = "Đã mở khóa tài khoản thành công! Gửi email thông báo về Gmail." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// API XÓA TÀI KHOẢN DÀNH CHO ADMIN DASHBOARD (POST /api/admin/delete-user)
    /// Xóa tài khoản, tự động dọn dẹp các hồ sơ Store hoặc Shipper liên kết nếu có
    /// </summary>
    [HttpPost("delete-user")]
    public async Task<IActionResult> DeleteUserPost([FromBody] DeleteUserPostRequest req)
    {
        InvalidateAdminCache();
        if (string.IsNullOrWhiteSpace(req.UserId))
        {
            return BadRequest(new { success = false, message = "Thiếu mã tài khoản cần xóa!" });
        }

        try
        {
            string userEmail = !string.IsNullOrWhiteSpace(req.Email) ? req.Email.Trim().ToLower() : "";
            string userName = "";
            string targetUserId = req.UserId?.Trim() ?? "";

            // 1. Tìm trong collection Users
            User? user = null;
            if (MongoDB.Bson.ObjectId.TryParse(targetUserId, out _))
            {
                user = await _mongoService.Users.Find(u => u.Id == targetUserId).FirstOrDefaultAsync();
            }
            if (user == null)
            {
                user = await _mongoService.Users.Find(u => u.UserCode == targetUserId || u.PhoneEmail == targetUserId).FirstOrDefaultAsync();
            }
            if (user == null && !string.IsNullOrEmpty(userEmail))
            {
                user = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == userEmail).FirstOrDefaultAsync();
            }

            if (user != null)
            {
                if (string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(user.PhoneEmail))
                    userEmail = user.PhoneEmail.Trim().ToLower();
                userName = user.FullName ?? "";
            }

            // 2. Tìm trong collection Stores (nếu là gian hàng)
            Store? store = null;
            if (MongoDB.Bson.ObjectId.TryParse(targetUserId, out _))
            {
                store = await _mongoService.Stores.Find(s => s.Id == targetUserId).FirstOrDefaultAsync();
            }
            if (store == null)
            {
                store = await _mongoService.Stores.Find(s => s.StoreCode == targetUserId || s.PhoneEmail == targetUserId).FirstOrDefaultAsync();
            }
            if (store == null && !string.IsNullOrEmpty(userEmail))
            {
                store = await _mongoService.Stores.Find(s => s.PhoneEmail != null && s.PhoneEmail.ToLower() == userEmail).FirstOrDefaultAsync();
            }

            if (store != null)
            {
                if (string.IsNullOrEmpty(userName)) userName = store.StoreName ?? store.OwnerFullName ?? "";
                if (string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(store.PhoneEmail)) userEmail = store.PhoneEmail.Trim().ToLower();
            }

            // 3. Tìm trong collection Shippers (nếu là tài xế)
            Shipper? shipper = null;
            if (MongoDB.Bson.ObjectId.TryParse(targetUserId, out _))
            {
                shipper = await _mongoService.Shippers.Find(s => s.Id == targetUserId).FirstOrDefaultAsync();
            }
            if (shipper == null)
            {
                shipper = await _mongoService.Shippers.Find(s => s.ShipperCode == targetUserId || s.PhoneNumber == targetUserId).FirstOrDefaultAsync();
            }
            if (shipper == null && !string.IsNullOrEmpty(userEmail))
            {
                shipper = await _mongoService.Shippers.Find(s => s.PhoneNumber != null && s.PhoneNumber.ToLower() == userEmail).FirstOrDefaultAsync();
            }

            if (shipper != null)
            {
                if (string.IsNullOrEmpty(userName)) userName = shipper.FullName ?? "";
                if (string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(shipper.PhoneNumber)) userEmail = shipper.PhoneNumber.Trim().ToLower();
            }

            // 4. XÓA TRIỆT ĐỂ KHỎI TẤT CẢ CÁC COLLECTION MONGODB (HARD DELETE)
            await _mongoService.Users.DeleteManyAsync(u => 
                u.Id == targetUserId || 
                (user != null && u.Id == user.Id) || 
                (!string.IsNullOrEmpty(userEmail) && u.PhoneEmail != null && u.PhoneEmail.ToLower() == userEmail) ||
                (user != null && !string.IsNullOrEmpty(user.UserCode) && u.UserCode == user.UserCode)
            );

            await _mongoService.Stores.DeleteManyAsync(s => 
                s.Id == targetUserId || 
                s.UserId == targetUserId || 
                (store != null && s.Id == store.Id) ||
                (user != null && (s.UserId == user.Id || (!string.IsNullOrEmpty(user.UserCode) && s.UserId == user.UserCode))) || 
                (!string.IsNullOrEmpty(userEmail) && s.PhoneEmail != null && s.PhoneEmail.ToLower() == userEmail)
            );

            await _mongoService.Shippers.DeleteManyAsync(sh => 
                sh.Id == targetUserId || 
                sh.UserId == targetUserId || 
                (shipper != null && sh.Id == shipper.Id) ||
                (user != null && (sh.UserId == user.Id || (!string.IsNullOrEmpty(user.UserCode) && sh.UserId == user.UserCode))) || 
                (!string.IsNullOrEmpty(userEmail) && sh.PhoneNumber != null && sh.PhoneNumber.ToLower() == userEmail)
            );

            // 5. DỌN DẸP SẠCH SẼ BỘ NHỚ ĐỆM IN-MEMORY (TRÁNH FALLBACK ĐĂNG NHẬP LẠI)
            if (!string.IsNullOrEmpty(userEmail))
            {
                AuthController.InMemoryUsers.TryRemove(userEmail, out _);
            }
            if (!string.IsNullOrEmpty(targetUserId))
            {
                AuthController.InMemoryUsers.TryRemove(targetUserId, out _);
            }
            var inMemUserKeys = AuthController.InMemoryUsers.Where(kv => 
                kv.Value.Id == targetUserId || 
                (!string.IsNullOrEmpty(userEmail) && kv.Value.PhoneEmail != null && kv.Value.PhoneEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            ).Select(kv => kv.Key).ToList();
            foreach (var k in inMemUserKeys) AuthController.InMemoryUsers.TryRemove(k, out _);

            var inMemStoreKeys = AuthController.InMemoryStores.Where(kv => 
                kv.Key == targetUserId || 
                kv.Value.Id == targetUserId || 
                kv.Value.UserId == targetUserId || 
                (!string.IsNullOrEmpty(userEmail) && kv.Value.PhoneEmail != null && kv.Value.PhoneEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            ).Select(kv => kv.Key).ToList();
            foreach (var k in inMemStoreKeys) AuthController.InMemoryStores.TryRemove(k, out _);

            var inMemShipperKeys = AuthController.InMemoryShippers.Where(kv => 
                kv.Key == targetUserId || 
                kv.Value.Id == targetUserId || 
                kv.Value.UserId == targetUserId || 
                (!string.IsNullOrEmpty(userEmail) && kv.Value.PhoneNumber != null && kv.Value.PhoneNumber.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            ).Select(kv => kv.Key).ToList();
            foreach (var k in inMemShipperKeys) AuthController.InMemoryShippers.TryRemove(k, out _);

            await LogAuditAsync(req.ActorEmail, req.ActorRole, "Xóa tài khoản vĩnh viễn", userEmail, $"ADMIN đã xóa hoàn toàn tài khoản #{targetUserId} ({userName}) khỏi hệ thống!");

            return Ok(new { success = true, message = $"Đã xóa hoàn toàn tài khoản [{userName}] khỏi hệ thống!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// XÓA TÀI KHOẢN (Account Deletion) - ĐẶC QUYỀN DUY NHẤT CỦA ADMIN
    /// Quản lý (Manager) gọi vào API này sẽ bị chặn bằng HTTP 403 Forbidden!
    /// Yêu cầu mã xác nhận an toàn confirmationCode == "XACNHAN"
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId, [FromQuery] string confirmationCode, [FromQuery] string actorEmail, [FromQuery] string actorRole)
    {
        InvalidateAdminCache();
        // 1. Phân quyền nghiêm ngặt: Kiểm tra nếu không phải Admin -> Báo lỗi 403 Forbidden
        if (string.IsNullOrWhiteSpace(actorRole) || (actorRole.ToLower() != "admin" && actorRole.ToLower() != "administrator"))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "⛔ LỖI 403 FORBIDDEN: Thao tác xóa vĩnh viễn tài khoản là ĐẶC QUYỀN DUY NHẤT CỦA ADMIN! Tài khoản Quản lý không có quyền hạn này."
            });
        }

        // 2. Xác minh cơ chế an toàn 2 lớp (Safety Confirmation)
        if (confirmationCode != "XACNHAN")
        {
            return BadRequest(new
            {
                success = false,
                message = "Chưa nhập đúng mã xác nhận nguy hiểm 'XACNHAN'. Thao tác xóa đã bị hủy!"
            });
        }

        try
        {
            User? user = null;
            if (MongoDB.Bson.ObjectId.TryParse(userId, out _))
            {
                user = await _mongoService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            }
            if (user == null)
            {
                user = await _mongoService.Users.Find(u => u.UserCode == userId || u.PhoneEmail == userId).FirstOrDefaultAsync();
            }
            if (user == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản để xóa!" });

            // 3. Thực hiện xóa triệt để chuẩn CSDL
            string userEmail = user.PhoneEmail?.Trim().ToLower() ?? "";
            string userCode = user.UserCode ?? "";

            await _mongoService.Users.DeleteManyAsync(u => u.Id == userId || (!string.IsNullOrEmpty(userEmail) && u.PhoneEmail != null && u.PhoneEmail.ToLower() == userEmail));
            if (MongoDB.Bson.ObjectId.TryParse(userId, out _))
            {
                await _mongoService.Users.DeleteManyAsync(u => u.Id == userId || (!string.IsNullOrEmpty(userEmail) && u.PhoneEmail != null && u.PhoneEmail.ToLower() == userEmail));
            }
            else
            {
                await _mongoService.Users.DeleteManyAsync(u => u.UserCode == userId || (!string.IsNullOrEmpty(userEmail) && u.PhoneEmail != null && u.PhoneEmail.ToLower() == userEmail));
            }
            await _mongoService.Stores.DeleteManyAsync(s => s.UserId == userId || (!string.IsNullOrEmpty(userCode) && s.UserId == userCode) || (!string.IsNullOrEmpty(userEmail) && s.PhoneEmail != null && s.PhoneEmail.ToLower() == userEmail));
            await _mongoService.Shippers.DeleteManyAsync(sh => sh.UserId == userId || (!string.IsNullOrEmpty(userCode) && sh.UserId == userCode) || (!string.IsNullOrEmpty(userEmail) && sh.PhoneNumber != null && sh.PhoneNumber.ToLower() == userEmail));

            if (!string.IsNullOrEmpty(userEmail)) AuthController.InMemoryUsers.TryRemove(userEmail, out _);
            AuthController.InMemoryUsers.TryRemove(userId, out _);

            await LogAuditAsync(actorEmail, actorRole, "Xóa tài khoản vĩnh viễn", user.PhoneEmail ?? "", $"ADMIN đã xóa vĩnh viễn tài khoản #{userId}");

            return Ok(new
            {
                success = true,
                message = $"Đã xóa an toàn tài khoản [{user.FullName} - {user.PhoneEmail}] khỏi ứng dụng!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private async Task EnsureShiftManagersSeededAsync()
    {
        try
        {
            var count = await _mongoService.ShiftManagers.CountDocumentsAsync(_ => true);
            if (count == 0)
            {
                var existingUsers = await _mongoService.Users.Find(u => u.IsManager && !u.IsDeleted).ToListAsync();
                int idx = 1;
                foreach (var u in existingUsers)
                {
                    string code = $"QL-{idx:D3}";
                    var sm = new ShiftManager
                    {
                        Id = code,
                        ManagerCode = code,
                        FullName = u.FullName,
                        Email = u.PhoneEmail,
                        Phone = u.PhoneEmail,
                        PasswordHash = u.PasswordHash,
                        Role = "Manager",
                        Status = "Active",
                        CreatedAt = u.CreatedAt
                    };
                    await _mongoService.ShiftManagers.InsertOneAsync(sm);
                    idx++;
                }

                if (existingUsers.Count == 0)
                {
                    var defaultSm = new ShiftManager
                    {
                        Id = "QL-001",
                        ManagerCode = "QL-001",
                        FullName = "ql khu vực 3",
                        Email = "kv3@gmail.com",
                        Phone = "kv3@gmail.com",
                        PasswordHash = "123",
                        Role = "Manager",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _mongoService.ShiftManagers.InsertOneAsync(defaultSm);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnsureShiftManagersSeededAsync] Warning: {ex.Message}");
        }
    }

    private async Task<string> GenerateNextManagerIdAsync()
    {
        var list = await _mongoService.ShiftManagers.Find(_ => true).ToListAsync();
        int maxIdx = 0;
        foreach (var sm in list)
        {
            string code = sm.Id ?? sm.ManagerCode ?? "";
            if (code.StartsWith("QL-", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(code.Substring(3), out int parsed) && parsed > maxIdx)
                {
                    maxIdx = parsed;
                }
            }
        }
        return $"QL-{(maxIdx + 1):D3}";
    }

    /// <summary>
    /// Thêm tài khoản Quản lý mới vào collection riêng "shift_managers" với ID QL-001, QL-002,...
    /// </summary>
    [HttpPost("create-manager")]
    public async Task<IActionResult> CreateManager([FromBody] CreateManagerRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ActorRole) || (req.ActorRole.ToLower() != "admin" && req.ActorRole.ToLower() != "administrator"))
        {
            return StatusCode(403, new { success = false, message = "Chỉ Administrator mới có quyền tạo thêm tài khoản Quản lý ca trực!" });
        }

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.FullName))
        {
            return BadRequest(new { success = false, message = "Vui lòng điền đầy đủ Họ tên, Gmail và Mật khẩu!" });
        }

        try
        {
            await EnsureShiftManagersSeededAsync();
            string cleanEmail = req.Email.Trim().ToLower();

            var existingSm = await _mongoService.ShiftManagers.Find(m => m.Email == cleanEmail && !m.IsDeleted).FirstOrDefaultAsync();
            if (existingSm != null)
            {
                return BadRequest(new { success = false, message = $"Email này đã được dùng cho Quản lý [{existingSm.Id} - {existingSm.FullName}]!" });
            }

            string newManagerId = await GenerateNextManagerIdAsync();

            var newShiftManager = new ShiftManager
            {
                Id = newManagerId,
                ManagerCode = newManagerId,
                FullName = req.FullName.Trim(),
                Email = cleanEmail,
                Phone = cleanEmail,
                PasswordHash = req.Password,
                Role = "Manager",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            await _mongoService.ShiftManagers.InsertOneAsync(newShiftManager);

            // Đồng bộ sang collection users để hỗ trợ đăng nhập hệ thống song song
            var existingUser = await _mongoService.Users.Find(u => u.PhoneEmail == cleanEmail && !u.IsDeleted).FirstOrDefaultAsync();
            if (existingUser == null)
            {
                var newManagerUser = new User
                {
                    UserCode = newManagerId,
                    PhoneEmail = cleanEmail,
                    PasswordHash = req.Password,
                    FullName = req.FullName.Trim(),
                    IsBuyer = true,
                    IsManager = true,
                    IsAdmin = false,
                    AccountStatus = "active",
                    CreatedAt = DateTime.UtcNow
                };
                await _mongoService.Users.InsertOneAsync(newManagerUser);
            }
            else
            {
                var update = Builders<User>.Update
                    .Set(u => u.IsManager, true)
                    .Set(u => u.UserCode, newManagerId);
                await _mongoService.Users.UpdateOneAsync(u => u.Id == existingUser.Id, update);
            }

            await LogAuditAsync(req.ActorEmail, req.ActorRole, "Tạo Quản lý Mới", cleanEmail, $"Khởi tạo tài khoản Quản lý #{newManagerId} cho {req.FullName} vào thư mục shift_managers");

            return Ok(new
            {
                success = true,
                managerId = newManagerId,
                message = $"Đã khởi tạo tài khoản Quản lý #{newManagerId} cho {req.FullName} ({cleanEmail}) vào thư mục shift_managers thành công!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Danh sách các Quản lý ca trực từ collection riêng "shift_managers"
    /// </summary>
    [HttpGet("managers")]
    public async Task<IActionResult> GetManagers()
    {
        try
        {
            await EnsureShiftManagersSeededAsync();
            var shiftManagers = await _mongoService.ShiftManagers.Find(m => !m.IsDeleted).SortBy(m => m.Id).ToListAsync();
            var managers = shiftManagers.Select(m => new
            {
                id = m.Id,
                userCode = m.ManagerCode,
                fullName = m.FullName,
                email = m.Email,
                phoneEmail = m.Email,
                role = m.Role,
                status = m.Status,
                accountStatus = m.Status.ToLower(),
                isManager = true,
                createdAt = m.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            return Ok(new { success = true, managers });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Vô hiệu hóa / Thu hồi quyền Quản lý trong shift_managers
    /// </summary>
    [HttpPost("toggle-manager-status")]
    public async Task<IActionResult> ToggleManagerStatus([FromBody] ToggleManagerRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ActorRole) || (req.ActorRole.ToLower() != "admin" && req.ActorRole.ToLower() != "administrator"))
        {
            return StatusCode(403, new { success = false, message = "Chỉ Administrator mới có quyền thu hồi hoặc tạm khóa tài khoản Quản lý!" });
        }

        try
        {
            var sm = await _mongoService.ShiftManagers.Find(m => m.Id == req.ManagerId || m.ManagerCode == req.ManagerId).FirstOrDefaultAsync();
            if (sm != null)
            {
                string newStatus = sm.Status == "Active" ? "Suspended" : "Active";
                await _mongoService.ShiftManagers.UpdateOneAsync(
                    m => m.Id == sm.Id,
                    Builders<ShiftManager>.Update.Set(m => m.Status, newStatus)
                );

                // Cập nhật cả bảng users tương ứng
                await _mongoService.Users.UpdateManyAsync(
                    u => u.PhoneEmail == sm.Email,
                    Builders<User>.Update.Set(u => u.AccountStatus, newStatus.ToLower())
                );

                await LogAuditAsync(req.ActorEmail, req.ActorRole, "Thay đổi trạng thái Quản lý", sm.Email, $"Trạng thái mới: {newStatus}");

                return Ok(new { success = true, message = $"Đã chuyển trạng thái Quản lý [{sm.Id} - {sm.FullName}] thành '{newStatus}'", newStatus });
            }

            var manager = await _mongoService.Users.Find(u => u.Id == req.ManagerId).FirstOrDefaultAsync();
            if (manager == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản Quản lý!" });

            string userStatus = manager.AccountStatus == "active" ? "suspended" : "active";
            var userUpdate = Builders<User>.Update.Set(u => u.AccountStatus, userStatus);
            await _mongoService.Users.UpdateOneAsync(u => u.Id == manager.Id, userUpdate);

            await LogAuditAsync(req.ActorEmail, req.ActorRole, "Thay đổi trạng thái Quản lý", manager.PhoneEmail, $"Trạng thái mới: {userStatus}");

            return Ok(new { success = true, message = $"Đã chuyển trạng thái Quản lý [{manager.FullName}] thành '{userStatus}'", newStatus = userStatus });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Nhật ký thao tác vận hành (Audit Logs)
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs()
    {
        try
        {
            var logs = await _mongoService.AuditLogs.Find(_ => true).SortByDescending(l => l.Timestamp).Limit(50).ToListAsync();
            return Ok(new { success = true, logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Phân quyền vai trò và quyền hạn chi tiết cho tài khoản người dùng
    /// </summary>
    [HttpPost("assign-role")]
    [HttpPost("update-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ActorRole) || (req.ActorRole.ToLower() != "admin" && req.ActorRole.ToLower() != "administrator"))
        {
            return StatusCode(403, new { success = false, message = "Chỉ Quản trị viên (Admin) mới có quyền phân quyền cho tài khoản!" });
        }

        if (string.IsNullOrWhiteSpace(req.UserId))
        {
            return BadRequest(new { success = false, message = "Vui lòng cung cấp ID tài khoản cần phân quyền!" });
        }

        try
        {
            string cleanId = req.UserId?.Trim() ?? "";
            string cleanEmail = req.Email?.Trim() ?? "";
            string cleanCode = req.UserCode?.Trim() ?? "";

            var user = await _mongoService.Users.Find(u => 
                (!string.IsNullOrEmpty(u.Id) && u.Id == cleanId) ||
                (!string.IsNullOrEmpty(u.UserCode) && (u.UserCode == cleanId || (!string.IsNullOrEmpty(cleanCode) && u.UserCode == cleanCode))) ||
                (!string.IsNullOrEmpty(u.PhoneEmail) && (u.PhoneEmail.ToLower() == cleanId.ToLower() || (!string.IsNullOrEmpty(cleanEmail) && u.PhoneEmail.ToLower() == cleanEmail.ToLower())))
            ).FirstOrDefaultAsync();

            ShiftManager? shiftManager = null;
            // 1. Kiểm tra nếu là Quản Lý Ca Trực trong shift_managers
            shiftManager = await _mongoService.ShiftManagers.Find(m => 
                (!string.IsNullOrEmpty(m.Id) && m.Id == cleanId) ||
                (!string.IsNullOrEmpty(m.ManagerCode) && (m.ManagerCode == cleanId || (!string.IsNullOrEmpty(cleanCode) && m.ManagerCode == cleanCode))) ||
                (!string.IsNullOrEmpty(m.Email) && (m.Email.ToLower() == cleanId.ToLower() || (!string.IsNullOrEmpty(cleanEmail) && m.Email.ToLower() == cleanEmail.ToLower())))
            ).FirstOrDefaultAsync();

            if (user == null && shiftManager != null)
            {
                user = await _mongoService.Users.Find(u => 
                    (!string.IsNullOrEmpty(u.PhoneEmail) && u.PhoneEmail.ToLower() == shiftManager.Email.ToLower()) ||
                    (!string.IsNullOrEmpty(u.UserCode) && u.UserCode == shiftManager.Id)
                ).FirstOrDefaultAsync();

                if (user == null)
                {
                    user = new User
                    {
                        UserCode = shiftManager.ManagerCode ?? shiftManager.Id,
                        FullName = shiftManager.FullName,
                        PhoneEmail = shiftManager.Email,
                        PasswordHash = "123456",
                        IsBuyer = true,
                        IsManager = true,
                        AccountStatus = "active",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _mongoService.Users.InsertOneAsync(user);
                }
            }

            // 2. Nếu chưa có, kiểm tra trong Stores collection (Khu vực & Chợ số)
            if (user == null)
            {
                var store = await _mongoService.Stores.Find(s => 
                    s.Id == cleanId || 
                    s.StoreCode == cleanId || 
                    (!string.IsNullOrEmpty(s.PhoneEmail) && (s.PhoneEmail.ToLower() == cleanId.ToLower() || (!string.IsNullOrEmpty(cleanEmail) && s.PhoneEmail.ToLower() == cleanEmail.ToLower())))
                ).FirstOrDefaultAsync();

                if (store != null)
                {
                    if (!string.IsNullOrEmpty(store.UserId))
                    {
                        user = await _mongoService.Users.Find(u => u.Id == store.UserId || u.UserCode == store.UserId).FirstOrDefaultAsync();
                    }
                    if (user == null && !string.IsNullOrEmpty(store.PhoneEmail))
                    {
                        user = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == store.PhoneEmail.ToLower()).FirstOrDefaultAsync();
                    }
                    if (user == null)
                    {
                        user = new User
                        {
                            FullName = store.OwnerFullName ?? store.StoreName,
                            PhoneEmail = store.PhoneEmail ?? $"seller.{cleanId}@zonemart.vn",
                            PasswordHash = "123456",
                            IsBuyer = true,
                            IsSeller = true,
                            AccountStatus = store.Status == "Rejected" ? "rejected" : "active",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _mongoService.Users.InsertOneAsync(user);
                        await _mongoService.Stores.UpdateOneAsync(s => s.Id == store.Id, Builders<Store>.Update.Set(s => s.UserId, user.Id));
                    }
                }
            }

            // 3. Nếu chưa có, kiểm tra trong Shippers collection
            if (user == null)
            {
                var shipper = await _mongoService.Shippers.Find(sh => 
                    sh.Id == cleanId || 
                    sh.ShipperCode == cleanId || 
                    (!string.IsNullOrEmpty(sh.PhoneNumber) && (sh.PhoneNumber.ToLower() == cleanId.ToLower() || (!string.IsNullOrEmpty(cleanEmail) && sh.PhoneNumber.ToLower() == cleanEmail.ToLower())))
                ).FirstOrDefaultAsync();

                if (shipper != null)
                {
                    if (!string.IsNullOrEmpty(shipper.UserId))
                    {
                        user = await _mongoService.Users.Find(u => u.Id == shipper.UserId || u.UserCode == shipper.UserId).FirstOrDefaultAsync();
                    }
                    if (user == null && !string.IsNullOrEmpty(shipper.PhoneNumber))
                    {
                        user = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == shipper.PhoneNumber.ToLower()).FirstOrDefaultAsync();
                    }
                    if (user == null)
                    {
                        user = new User
                        {
                            FullName = shipper.FullName,
                            PhoneEmail = shipper.PhoneNumber ?? $"shipper.{cleanId}@zonemart.vn",
                            PasswordHash = "123456",
                            IsBuyer = true,
                            IsShipper = true,
                            AccountStatus = shipper.Status == "Rejected" ? "rejected" : "active",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _mongoService.Users.InsertOneAsync(user);
                        await _mongoService.Shippers.UpdateOneAsync(sh => sh.Id == shipper.Id, Builders<Shipper>.Update.Set(sh => sh.UserId, user.Id));
                    }
                }
            }

            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản người dùng trên hệ thống!" });
            }

            // An toàn hệ thống: Không thể hạ quyền tài khoản Admin gốc (admin@zonemart.vn)
            if (!string.IsNullOrEmpty(user.PhoneEmail) && user.PhoneEmail.ToLower() == "admin@zonemart.vn" && req.IsAdmin == false)
            {
                return BadRequest(new { success = false, message = "Không thể tước quyền Admin của tài khoản quản trị hệ thống gốc (admin@zonemart.vn)!" });
            }

            bool newBuyer = req.IsBuyer ?? user.IsBuyer;
            bool newSeller = req.IsSeller ?? user.IsSeller;
            bool newShipper = req.IsShipper ?? user.IsShipper;
            bool newManager = req.IsManager ?? user.IsManager;
            bool newAdmin = req.IsAdmin ?? user.IsAdmin;
            var newPermissions = req.Permissions ?? user.Permissions ?? new List<string>();

            // Nếu người dùng không chọn quyền nào, mặc định là Khách Hàng (Buyer)
            if (!newBuyer && !newSeller && !newShipper && !newManager && !newAdmin)
            {
                newBuyer = true;
            }

            var updateDef = Builders<User>.Update
                .Set(u => u.IsBuyer, newBuyer)
                .Set(u => u.IsSeller, newSeller)
                .Set(u => u.IsShipper, newShipper)
                .Set(u => u.IsManager, newManager)
                .Set(u => u.IsAdmin, newAdmin)
                .Set(u => u.Permissions, newPermissions);

            // Đồng bộ trạng thái gian hàng nếu cấp/hủy quyền Seller
            var existingStore = await _mongoService.Stores.Find(s => s.UserId == user.Id || (!string.IsNullOrEmpty(s.PhoneEmail) && s.PhoneEmail.ToLower() == user.PhoneEmail.ToLower())).FirstOrDefaultAsync();
            if (newSeller)
            {
                if (existingStore != null)
                {
                    if (existingStore.Status == "Pending" || existingStore.Status == "pending" || existingStore.Status == "Rejected" || existingStore.Status == "Suspended")
                    {
                        await _mongoService.Stores.UpdateOneAsync(s => s.Id == existingStore.Id, Builders<Store>.Update.Set(s => s.Status, "Approved"));
                    }
                }
                else
                {
                    // Tự động tạo hồ sơ gian hàng đã duyệt để tài khoản bắt đầu bán hàng ngay
                    var newStore = new Store
                    {
                        UserId = user.Id,
                        StoreCode = "SEL-" + (new Random().Next(1000, 9999)),
                        StoreName = "Gian Hàng " + user.FullName,
                        OwnerFullName = user.FullName,
                        PhoneEmail = user.PhoneEmail,
                        Status = "Approved",
                        Category = "Bách Hóa Tổng Hợp",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _mongoService.Stores.InsertOneAsync(newStore);
                }
            }
            else
            {
                if (existingStore != null && (existingStore.Status == "Approved" || existingStore.Status == "Active" || existingStore.Status == "Pending" || existingStore.Status == "pending"))
                {
                    await _mongoService.Stores.UpdateOneAsync(s => s.Id == existingStore.Id, Builders<Store>.Update.Set(s => s.Status, "Suspended"));
                }
            }

            // Đồng bộ trạng thái tài xế nếu cấp/hủy quyền Shipper
            var existingShipper = await _mongoService.Shippers.Find(sh => sh.UserId == user.Id || (!string.IsNullOrEmpty(sh.PhoneNumber) && sh.PhoneNumber.ToLower() == user.PhoneEmail.ToLower())).FirstOrDefaultAsync();
            if (newShipper)
            {
                if (existingShipper != null)
                {
                    if (existingShipper.Status == "Pending" || existingShipper.Status == "pending" || existingShipper.Status == "Rejected" || existingShipper.Status == "Suspended")
                    {
                        await _mongoService.Shippers.UpdateOneAsync(sh => sh.Id == existingShipper.Id, Builders<Shipper>.Update.Set(sh => sh.Status, "Approved"));
                    }
                }
                else
                {
                    // Tự động tạo hồ sơ tài xế đã duyệt để tài khoản nhận đơn giao ngay
                    var newShipperRecord = new Shipper
                    {
                        UserId = user.Id,
                        ShipperCode = "SHP-" + (new Random().Next(1000, 9999)),
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneEmail,
                        Status = "Approved",
                        LicensePlate = "29-X1 " + (new Random().Next(10000, 99999)),
                        VehicleType = "Xe máy",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _mongoService.Shippers.InsertOneAsync(newShipperRecord);
                }
            }
            else
            {
                if (existingShipper != null && (existingShipper.Status == "Approved" || existingShipper.Status == "Pending" || existingShipper.Status == "pending"))
                {
                    await _mongoService.Shippers.UpdateOneAsync(sh => sh.Id == existingShipper.Id, Builders<Shipper>.Update.Set(sh => sh.Status, "Suspended"));
                }
            }

            // Đồng bộ bộ sưu tập shift_managers nếu là Quản Lý Ca Trực
            if (shiftManager == null)
            {
                shiftManager = await _mongoService.ShiftManagers.Find(m => 
                    m.Email.ToLower() == user.PhoneEmail.ToLower() || 
                    (!string.IsNullOrEmpty(user.UserCode) && (m.Id == user.UserCode || m.ManagerCode == user.UserCode))
                ).FirstOrDefaultAsync();
            }

            if (newManager)
            {
                if (shiftManager != null)
                {
                    await _mongoService.ShiftManagers.UpdateOneAsync(
                        m => m.Id == shiftManager.Id,
                        Builders<ShiftManager>.Update.Set(m => m.Status, "Active").Set(m => m.Role, "Quản lý ca trực")
                    );
                }
                else
                {
                    // Tự động cấp mã QL-xxx tiếp theo
                    var count = await _mongoService.ShiftManagers.CountDocumentsAsync(Builders<ShiftManager>.Filter.Empty);
                    string nextId = $"QL-{(count + 1):D3}";
                    var newSm = new ShiftManager
                    {
                        Id = nextId,
                        ManagerCode = nextId,
                        FullName = user.FullName,
                        Email = user.PhoneEmail,
                        Phone = user.PhoneEmail,
                        Role = "Quản lý ca trực",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _mongoService.ShiftManagers.InsertOneAsync(newSm);
                }
            }
            else if (!newAdmin && shiftManager != null)
            {
                await _mongoService.ShiftManagers.UpdateOneAsync(
                    m => m.Id == shiftManager.Id,
                    Builders<ShiftManager>.Update.Set(m => m.Status, "Suspended")
                );
            }

            await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, updateDef);
            _usersCache = null;
            _statsCache = null;

            string primaryRole = newAdmin ? "Admin" 
                : newManager ? "Manager" 
                : newSeller ? "Seller" 
                : newShipper ? "Shipper" 
                : "Buyer";

            var rolesList = new List<string>();
            if (newAdmin) rolesList.Add("Quản Trị Viên");
            if (newManager) rolesList.Add("Quản Lý");
            if (newSeller) rolesList.Add("Chủ Gian Hàng");
            if (newShipper) rolesList.Add("Tài Xế Shipper");
            if (newBuyer) rolesList.Add("Khách Hàng");

            string rolesSummary = string.Join(", ", rolesList);
            string note = string.IsNullOrWhiteSpace(req.Reason) ? "Quản trị viên cập nhật phân quyền tài khoản" : req.Reason.Trim();

            await LogAuditAsync(
                req.ActorEmail ?? "admin@zonemart.vn",
                req.ActorRole ?? "Admin",
                "Phân quyền tài khoản",
                user.PhoneEmail,
                $"Cập nhật vai trò [{primaryRole}] (Các vai trò: {rolesSummary}). Quyền hạn: [{string.Join(", ", newPermissions)}]. Lý do: {note}"
            );

            return Ok(new
            {
                success = true,
                message = $"Đã phân quyền thành công cho tài khoản [{user.FullName}] thành [{rolesSummary}]!",
                user = new
                {
                    id = user.Id,
                    email = user.PhoneEmail,
                    fullName = user.FullName,
                    primaryRole,
                    isBuyer = newBuyer,
                    isSeller = newSeller,
                    isShipper = newShipper,
                    isManager = newManager,
                    isAdmin = newAdmin,
                    permissions = newPermissions
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi phân quyền: " + ex.Message });
        }
    }

    private async Task LogAuditAsync(string actorEmail, string actorRole, string action, string targetEmail, string details)
    {
        try
        {
            var log = new AuditLog
            {
                ActorEmail = string.IsNullOrWhiteSpace(actorEmail) ? "admin@zonemart.vn" : actorEmail,
                ActorRole = string.IsNullOrWhiteSpace(actorRole) ? "Admin" : actorRole,
                Action = action,
                TargetUserEmail = targetEmail,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
            await _mongoService.AuditLogs.InsertOneAsync(log);
        }
        catch
        {
            // Ignore log errors
        }
    }

    private static async Task<bool> SendEmailViaMailKitAsync(string toEmail, string subject, string htmlBody, string senderName = "ZoneMart Admin")
    {
        string cleanToEmail = toEmail?.Trim().ToLower() ?? "";
        if (string.IsNullOrWhiteSpace(cleanToEmail) || !cleanToEmail.Contains("@"))
        {
            Console.WriteLine("⚠️ [Admin SMTP Warning] Bỏ qua gửi email do địa chỉ toEmail không hợp lệ!");
            return false;
        }

        var accounts = new (string Email, string Password)[]
        {
            ("dobinh225599@gmail.com", "pihzfkraulkrrccz"),
            ("hh9393100@gmail.com", "bjgiqgdpcozzfqip")
        };

        foreach (var account in accounts)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, account.Email));
                message.To.Add(new MailboxAddress("", cleanToEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 8000;
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(account.Email, account.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                return true;
            }
            catch
            {
                // Thử tài khoản tiếp theo
            }
        }
        return false;
    }

    /// <summary>
    /// Nạp dữ liệu mẫu thực tế trực tiếp vào CSDL MongoDB Atlas (nếu CSDL đang trống)
    /// </summary>
    [HttpPost("seed-sample-data")]
    public async Task<IActionResult> SeedSampleData()
    {
        await EnsureDataSeededAsync(force: true);
        return Ok(new { success = true, message = "Đã nạp thành công dữ liệu mẫu thực tế vào CSDL MongoDB Atlas!" });
    }

    private static bool _isDataSeeded = false;

    private async Task EnsureDataSeededAsync(bool force = false)
    {
        if (_isDataSeeded && !force) return;
        _isDataSeeded = true;
        try
        {
            // Luôn đảm bảo tài khoản Admin hệ thống tồn tại và có quyền IsAdmin = true
            var adminUser = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == "admin@zonemart.vn").FirstOrDefaultAsync();
            if (adminUser == null)
            {
                adminUser = new User
                {
                    PhoneEmail = "admin@zonemart.vn",
                    PasswordHash = "admin123",
                    Password = "admin123",
                    FullName = "Super Administrator",
                    IsAdmin = true,
                    IsManager = true,
                    AccountStatus = "active",
                    CreatedAt = DateTime.UtcNow
                };
                await _mongoService.Users.InsertOneAsync(adminUser);
            }
            else if (!adminUser.IsAdmin)
            {
                await _mongoService.Users.UpdateOneAsync(
                    u => u.Id == adminUser.Id,
                    Builders<User>.Update.Set(u => u.IsAdmin, true).Set(u => u.IsManager, true).Set(u => u.AccountStatus, "active")
                );
            }

            // Đảm bảo hhuy85895@gmail.com là tài khoản Khách hàng thông thường (Buyer), không phải Admin
            var hhuyUser = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == "hhuy85895@gmail.com").FirstOrDefaultAsync();
            if (hhuyUser != null && (hhuyUser.IsAdmin || hhuyUser.IsManager))
            {
                await _mongoService.Users.UpdateOneAsync(
                    u => u.Id == hhuyUser.Id,
                    Builders<User>.Update.Set(u => u.IsAdmin, false).Set(u => u.IsManager, false).Set(u => u.IsBuyer, true)
                );
            }

            var userCount = await _mongoService.Users.CountDocumentsAsync(_ => true);
            if (userCount == 0 || force)
            {
                if (force && userCount > 0)
                {
                    // Tránh ghi đè nếu đã có người dùng
                    return;
                }

                var sampleUsers = new List<User>
                {
                    new User { PhoneEmail = "admin@zonemart.vn", PasswordHash = "admin123", FullName = "Super Administrator", IsAdmin = true, IsManager = true, AccountStatus = "active" },
                    new User { PhoneEmail = "manager01@zonemart.vn", PasswordHash = "manager123", FullName = "Nguyễn Văn Quản Lý", IsBuyer = true, IsManager = true, AccountStatus = "active" },
                    new User { PhoneEmail = "tiemcoba@gmail.com", PasswordHash = "123456", FullName = "Trần Thị Mai", IsBuyer = true, IsSeller = true, AccountStatus = "active" },
                    new User { PhoneEmail = "nam.tran88@gmail.com", PasswordHash = "123456", FullName = "Trần Văn Nam", IsBuyer = true, IsShipper = true, AccountStatus = "pending" },
                    new User { PhoneEmail = "hoangyen.le@gmail.com", PasswordHash = "123456", FullName = "Lê Hoàng Yến", IsBuyer = true, AccountStatus = "suspended", ViolationCount = 2, LockUntil = DateTime.UtcNow.AddDays(10) },
                    new User { PhoneEmail = "shopvipham@gmail.com", PasswordHash = "123456", FullName = "Phạm Quốc Hùng", IsBuyer = true, IsSeller = true, AccountStatus = "banned", ViolationCount = 4 }
                };
                await _mongoService.Users.InsertManyAsync(sampleUsers);

                var storeUser = sampleUsers.First(u => u.PhoneEmail == "tiemcoba@gmail.com");
                var sampleStore = new Store
                {
                    UserId = storeUser.Id ?? "",
                    StoreName = "Tiệm Trà & Nước Ép Tươi Cô Ba",
                    Category = "Đồ Uống",
                    Address = "56 Nguyễn Phong Sắc, Cầu Giấy, Hà Nội",
                    OwnerFullName = "Trần Thị Mai",
                    CccdNumber = "001201012345",
                    CccdFrontImage = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=400",
                    CccdBackImage = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=400",
                    FoodSafetyCertImage = "https://images.unsplash.com/photo-1557804506-669a67965ba0?auto=format&fit=crop&w=400",
                    Status = "Approved"
                };
                await _mongoService.Stores.InsertOneAsync(sampleStore);

                var pendingStore = new Store
                {
                    UserId = sampleUsers[4].Id ?? "",
                    StoreName = "Nông Sản Sạch Ba Vì",
                    Category = "Nông Sản",
                    Address = "120 Xuân Thủy, Cầu Giấy, Hà Nội",
                    OwnerFullName = "Lê Hoàng Yến",
                    CccdNumber = "034200112233",
                    CccdFrontImage = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=400",
                    Status = "Pending"
                };
                await _mongoService.Stores.InsertOneAsync(pendingStore);

                var shipperUser = sampleUsers.First(u => u.PhoneEmail == "nam.tran88@gmail.com");
                var sampleShipper = new Shipper
                {
                    UserId = shipperUser.Id ?? "",
                    FullName = "Trần Văn Nam",
                    PhoneNumber = "nam.tran88@gmail.com",
                    LicensePlate = "29N1-67890",
                    VehicleType = "Xe máy xăng",
                    VehicleModel = "Honda Wave Alpha 110cc",
                    CccdNumber = "034200008899",
                    CccdFrontImage = "https://images.unsplash.com/photo-1557804506-669a67965ba0?auto=format&fit=crop&w=400",
                    DrivingLicenseImage = "https://images.unsplash.com/photo-1580273916550-e323be2ae537?auto=format&fit=crop&w=400",
                    IsOnline = true,
                    Status = "Pending"
                };
                await _mongoService.Shippers.InsertOneAsync(sampleShipper);

                var sampleOrder = new ParentOrder
                {
                    BuyerId = sampleUsers[2].Id ?? ObjectId.GenerateNewId().ToString(),
                    TotalAmount = 195000,
                    PaymentMethod = "COD",
                    PaymentStatus = "paid",
                    ShippingAddress = "56 Nguyễn Phong Sắc, Cầu Giấy, Hà Nội",
                    CreatedAt = DateTime.UtcNow
                };
                await _mongoService.ParentOrders.InsertOneAsync(sampleOrder);
            }

            // Nạp thêm đơn hàng mẫu cho các ngày trong tuần nếu chưa có đủ đơn
            var orderCount = await _mongoService.ParentOrders.CountDocumentsAsync(_ => true);
            if (orderCount < 5)
            {
                var today = DateTime.UtcNow.Date;
                var weeklySampleOrders = new List<ParentOrder>
                {
                    new ParentOrder { TotalAmount = 18500000, PaymentMethod = "VNPAY", PaymentStatus = "paid", ShippingAddress = "Cầu Giấy, Hà Nội", CreatedAt = today.AddDays(-6) },
                    new ParentOrder { TotalAmount = 24200000, PaymentMethod = "COD", PaymentStatus = "paid", ShippingAddress = "Thanh Xuân, Hà Nội", CreatedAt = today.AddDays(-5) },
                    new ParentOrder { TotalAmount = 19800000, PaymentMethod = "COD", PaymentStatus = "paid", ShippingAddress = "Đống Đa, Hà Nội", CreatedAt = today.AddDays(-4) },
                    new ParentOrder { TotalAmount = 31500000, PaymentMethod = "VNPAY", PaymentStatus = "paid", ShippingAddress = "Ba Đình, Hà Nội", CreatedAt = today.AddDays(-3) },
                    new ParentOrder { TotalAmount = 22100000, PaymentMethod = "COD", PaymentStatus = "paid", ShippingAddress = "Hai Bà Trưng, Hà Nội", CreatedAt = today.AddDays(-2) },
                    new ParentOrder { TotalAmount = 27400000, PaymentMethod = "VNPAY", PaymentStatus = "paid", ShippingAddress = "Hoàn Kiếm, Hà Nội", CreatedAt = today.AddDays(-1) },
                    new ParentOrder { TotalAmount = 35600000, PaymentMethod = "COD", PaymentStatus = "paid", ShippingAddress = "Nam Từ Liêm, Hà Nội", CreatedAt = today }
                };
                await _mongoService.ParentOrders.InsertManyAsync(weeklySampleOrders);
            }
        }
        catch
        {
            // Ignore seed errors
        }
    }

    /// <summary>
    /// API TỰ ĐỘNG CHUẨN HÓA & CẬP NHẬT TOÀN BỘ MÃ ID TRÊN MONGODB ATLAS
    /// - Shipper: SHP-0001, SHP-0002...
    /// - Seller: SEL-0001, SEL-0002...
    /// - Khách Hàng: CUST-0001, CUST-0002...
    /// - Đơn Hàng: ZM-0001, ZM-0002...
    /// </summary>
    [HttpPost("migrate-all-ids")]
    [HttpGet("migrate-all-ids")]
    public async Task<IActionResult> MigrateAllIds()
    {
        try
        {
            await MigrateAllIdsInternalAsync();
            var shipperCount = await _mongoService.Shippers.CountDocumentsAsync(_ => true);
            var storeCount = await _mongoService.Stores.CountDocumentsAsync(_ => true);
            var custCount = await _mongoService.Users.CountDocumentsAsync(u => u.IsBuyer && !u.IsShipper && !u.IsSeller);
            var orderCount = await _mongoService.ParentOrders.CountDocumentsAsync(_ => true);

            return Ok(new
            {
                success = true,
                message = "Đã cập nhật chuẩn hóa toàn bộ Mã ID trên CSDL MongoDB Atlas thành công!",
                stats = new
                {
                    shippers = $"{shipperCount} tài khoản SHP-XXXX",
                    stores = $"{storeCount} gian hàng SEL-XXXX",
                    customers = $"{custCount} khách hàng CUST-XXXX",
                    orders = $"{orderCount} đơn hàng ZM-XXXX"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi cập nhật CSDL: {ex.Message}" });
        }
    }

    private async Task MigrateAllIdsInternalAsync()
    {
        try
        {
            // 1. SHIPPER IDs: SHP-0001, SHP-0002...
            var shippers = await _mongoService.Shippers.Find(_ => true).SortBy(s => s.CreatedAt).ToListAsync();
            for (int i = 0; i < shippers.Count; i++)
            {
                string shpCode = $"SHP-{(i + 1):D4}";
                var shp = shippers[i];
                var updateShp = Builders<Shipper>.Update
                    .Set(s => s.ShipperCode, shpCode)
                    .Set(s => s.UserId, shpCode);
                await _mongoService.Shippers.UpdateOneAsync(s => s.Id == shp.Id, updateShp);

                if (!string.IsNullOrEmpty(shp.PhoneNumber))
                {
                    var userUpdate = Builders<User>.Update
                        .Set(u => u.UserCode, shpCode)
                        .Set(u => u.IsShipper, true);
                    await _mongoService.Users.UpdateManyAsync(u => u.PhoneEmail.ToLower() == shp.PhoneNumber.ToLower() || u.UserCode == shp.UserId || u.UserCode == shp.ShipperCode, userUpdate);
                }
            }

            // 2. SELLER IDs: SEL-0001, SEL-0002...
            var stores = await _mongoService.Stores.Find(_ => true).SortBy(s => s.CreatedAt).ToListAsync();
            for (int i = 0; i < stores.Count; i++)
            {
                string selCode = $"SEL-{(i + 1):D4}";
                var st = stores[i];
                var updateSt = Builders<Store>.Update
                    .Set(s => s.StoreCode, selCode)
                    .Set(s => s.UserId, selCode);
                await _mongoService.Stores.UpdateOneAsync(s => s.Id == st.Id, updateSt);

                if (!string.IsNullOrEmpty(st.OwnerFullName))
                {
                    var userUpdate = Builders<User>.Update
                        .Set(u => u.UserCode, selCode)
                        .Set(u => u.IsSeller, true);
                    await _mongoService.Users.UpdateManyAsync(u => u.FullName.ToLower() == st.OwnerFullName.ToLower() || u.UserCode == st.UserId || u.UserCode == st.StoreCode, userUpdate);
                }
            }

            // 3. CUSTOMER IDs: CUST-0001, CUST-0002...
            var customers = await _mongoService.Users.Find(u => u.IsBuyer && !u.IsShipper && !u.IsSeller).SortBy(u => u.CreatedAt).ToListAsync();
            for (int i = 0; i < customers.Count; i++)
            {
                string custCode = $"CUST-{(i + 1):D4}";
                var cust = customers[i];
                var updateCust = Builders<User>.Update.Set(u => u.UserCode, custCode);
                await _mongoService.Users.UpdateOneAsync(u => u.Id == cust.Id, updateCust);
            }

            // 4. ORDER IDs: ZM-0001, ZM-0002...
            var orders = await _mongoService.ParentOrders.Find(_ => true).SortBy(o => o.CreatedAt).ToListAsync();
            for (int i = 0; i < orders.Count; i++)
            {
                string zmCode = $"ZM-{(i + 1):D4}";
                var ord = orders[i];
                var updateOrd = Builders<ParentOrder>.Update.Set("order_code", zmCode);
                await _mongoService.ParentOrders.UpdateOneAsync(o => o.Id == ord.Id, updateOrd);
            }

            Console.WriteLine($"✅ [Migration MongoDB Atlas] Đã chuẩn hóa toàn bộ Mã ID trên Database thành công! (Shippers: SHP-0001+, Stores: SEL-0001+, Customers: CUST-0001+, Orders: ZM-0001+)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Migration Warning] {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy danh sách thông báo thời gian thực của Admin (Người đăng ký Store, Shipper, KYC mới...)
    /// Tự động đồng bộ từ Pending Stores/Shippers nếu bảng AdminNotifications chưa có.
    /// </summary>
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var notifs = await _mongoService.AdminNotifications
                .Find(_ => true)
                .SortByDescending(n => n.CreatedAt)
                .Limit(50)
                .ToListAsync();

            // Nếu danh sách rỗng, tự động tạo thông báo ban đầu từ các đơn đăng ký Pending thực tế
            if (notifs.Count == 0)
            {
                var pendingStores = await _mongoService.Stores.Find(s => s.Status == "Pending").ToListAsync();
                foreach (var s in pendingStores)
                {
                    var notif = new AdminNotification
                    {
                        Type = "seller",
                        Title = "Đăng ký mở gian hàng mới",
                        Message = $"Chủ tiệm {s.OwnerFullName} vừa đăng ký mở gian hàng \"{s.StoreName}\". Chờ duyệt KYC.",
                        TargetUserId = s.UserId ?? "",
                        TargetName = s.StoreName ?? "",
                        IsRead = false,
                        CreatedAt = s.CreatedAt
                    };
                    await _mongoService.AdminNotifications.InsertOneAsync(notif);
                    notifs.Add(notif);
                }

                var pendingShippers = await _mongoService.Shippers.Find(sh => sh.Status == "Pending").ToListAsync();
                foreach (var sh in pendingShippers)
                {
                    var notif = new AdminNotification
                    {
                        Type = "shipper",
                        Title = "Đăng ký tài xế shipper mới",
                        Message = $"Tài xế {sh.FullName} ({sh.LicensePlate}) vừa nộp hồ sơ đăng ký đối tác shipper. Chờ xét duyệt.",
                        TargetUserId = sh.UserId ?? "",
                        TargetName = sh.FullName ?? "",
                        IsRead = false,
                        CreatedAt = sh.CreatedAt
                    };
                    await _mongoService.AdminNotifications.InsertOneAsync(notif);
                    notifs.Add(notif);
                }

                notifs = notifs.OrderByDescending(n => n.CreatedAt).ToList();
            }

            int unreadCount = notifs.Count(n => !n.IsRead);

            return Ok(new
            {
                success = true,
                unreadCount,
                notifications = notifs
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo hoặc một thông báo cụ thể là đã đọc
    /// </summary>
    [HttpPost("notifications/mark-read")]
    public async Task<IActionResult> MarkNotificationAsRead([FromBody] MarkNotificationReadRequest? req)
    {
        try
        {
            if (req != null && !string.IsNullOrWhiteSpace(req.Id))
            {
                var filter = Builders<AdminNotification>.Filter.Eq(n => n.Id, req.Id);
                var update = Builders<AdminNotification>.Update.Set(n => n.IsRead, true);
                await _mongoService.AdminNotifications.UpdateOneAsync(filter, update);
            }
            else
            {
                // Đánh dấu tất cả là đã đọc
                var filter = Builders<AdminNotification>.Filter.Eq(n => n.IsRead, false);
                var update = Builders<AdminNotification>.Update.Set(n => n.IsRead, true);
                await _mongoService.AdminNotifications.UpdateManyAsync(filter, update);
            }

            return Ok(new { success = true, message = "Đã cập nhật trạng thái thông báo" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

public class MarkNotificationReadRequest
{
    public string? Id { get; set; }
}

public class ApproveKycRequest
{
    public string TargetId { get; set; } = string.Empty;
    public string Type { get; set; } = "seller"; // seller hoặc shipper
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
}

public class RejectKycRequest
{
    public string TargetId { get; set; } = string.Empty;
    public string Type { get; set; } = "seller";
    public string Reason { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
}

public class PunishUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int Level { get; set; } = 1; // 1, 2, 3
    public string Reason { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
}

public class UnlockUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
}

public class CreateManagerRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
}

public class ToggleManagerRequest
{
    public string ManagerId { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
}

public class AdminUserViewDto
{
    public string id { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string fullName { get; set; } = string.Empty;
    public string avatarUrl { get; set; } = string.Empty;
    public string primaryRole { get; set; } = string.Empty;
    public bool isBuyer { get; set; }
    public bool isSeller { get; set; }
    public bool isShipper { get; set; }
    public bool isAdmin { get; set; }
    public bool isManager { get; set; }
    public string accountStatus { get; set; } = "active";
    public DateTime? lockUntil { get; set; }
    public int violationCount { get; set; }
    public string createdAt { get; set; } = string.Empty;
    public string cccdNumber { get; set; } = string.Empty;
    public string nguyenQuan { get; set; } = string.Empty;
    public object? storeDetails { get; set; }
    public object? shipperDetails { get; set; }
    public List<string> permissions { get; set; } = new();
}

public class AssignRoleRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserCode { get; set; }
    public string? ActorEmail { get; set; }
    public string? ActorRole { get; set; }
    public bool? IsBuyer { get; set; }
    public bool? IsSeller { get; set; }
    public bool? IsShipper { get; set; }
    public bool? IsManager { get; set; }
    public bool? IsAdmin { get; set; }
    public List<string>? Permissions { get; set; }
    public string? Reason { get; set; }
}

public class DeleteUserPostRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
}
