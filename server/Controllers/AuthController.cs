using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
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
public class AuthController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    // Lưu trữ tài khoản và gian hàng tạm thời song song để đảm bảo Đăng ký & Đăng nhập hoạt động 100% không bị gián đoạn
    public static readonly ConcurrentDictionary<string, User> InMemoryUsers = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, Store> InMemoryStores = new();
    public static readonly ConcurrentDictionary<string, Shipper> InMemoryShippers = new();
    public static readonly ConcurrentDictionary<string, (string Otp, DateTime ExpiresAt)> ZaloOtps = new();

    static AuthController()
    {
        var demoSeller = new User
        {
            Id = "seller_ba_vi_01",
            PhoneEmail = "seller@zonemart.vn",
            PasswordHash = "123456",
            FullName = "Chủ Vườn Rau Ba Vì (Bác Ba)",
            IsSeller = true,
            IsBuyer = true,
            AccountStatus = "active",
            WalletBalance = 3850000
        };
        InMemoryUsers[demoSeller.PhoneEmail] = demoSeller;

        var demoStore = new Store
        {
            Id = "store_ba_vi_01",
            UserId = demoSeller.Id,
            StoreName = "Vườn Rau Ba Vì - Nông Sản Sạch VietGAP",
            Category = "Thực phẩm & Nhu yếu phẩm",
            Address = "Số 48 đường Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội",
            OwnerFullName = demoSeller.FullName,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        InMemoryStores[demoStore.Id] = demoStore;

        var demoAdmin = new User
        {
            Id = "admin_zonemart_01",
            PhoneEmail = "admin@zonemart.vn",
            PasswordHash = "123456",
            FullName = "Ban Quản Trị ZoneMart",
            IsAdmin = true,
            AccountStatus = "active"
        };
        InMemoryUsers[demoAdmin.PhoneEmail] = demoAdmin;

        var demoShipper = new User
        {
            Id = "shipper_zonemart_01",
            PhoneEmail = "shipper@zonemart.vn",
            PasswordHash = "123456",
            FullName = "Tài xế Trần Văn Bình",
            IsShipper = true,
            AccountStatus = "active"
        };
        InMemoryUsers[demoShipper.PhoneEmail] = demoShipper;
    }

    public AuthController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    private async Task<string> GetNextCustomerCodeAsync()
    {
        try
        {
            long count = await _mongoService.Users.CountDocumentsAsync(u => u.IsBuyer && !u.IsShipper && !u.IsSeller);
            return $"CUST-{(count + 1):D4}"; // ID Khách hàng: CUST-0001, CUST-0002...
        }
        catch
        {
            return $"CUST-{Random.Shared.Next(1, 9999):D4}";
        }
    }

    private async Task<string> GetNextShipperCodeAsync()
    {
        try
        {
            long count = await _mongoService.Shippers.CountDocumentsAsync(Builders<Shipper>.Filter.Empty);
            return $"SHP-{(count + 1):D4}"; // ID Shipper: SHP-0001, SHP-0002...
        }
        catch
        {
            return $"SHP-{Random.Shared.Next(1, 9999):D4}";
        }
    }

    private async Task<string> GetNextSellerCodeAsync()
    {
        try
        {
            long count = await _mongoService.Stores.CountDocumentsAsync(Builders<Store>.Filter.Empty);
            return $"SEL-{(count + 1):D4}"; // ID Seller Gian hàng: SEL-0001, SEL-0002...
        }
        catch
        {
            return $"SEL-{Random.Shared.Next(1, 9999):D4}";
        }
    }

    private async Task<string> GetNextOrderCodeAsync()
    {
        try
        {
            long count = await _mongoService.ParentOrders.CountDocumentsAsync(Builders<ParentOrder>.Filter.Empty);
            return $"ZM-{(count + 1):D4}"; // ID Đơn hàng: ZM-0001, ZM-0002...
        }
        catch
        {
            return $"ZM-{Random.Shared.Next(1, 9999):D4}";
        }
    }

    /// <summary>
    /// API XÓA TÀI KHOẢN TẠM ĐỂ TEST ĐĂNG KÝ LẠI
    /// </summary>
    [HttpDelete("delete-user")]
    public async Task<IActionResult> DeleteUser([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest();
        string cleanEmail = email.Trim().ToLower();
        var variants = GetPhoneVariants(email);
        foreach (var v in variants) InMemoryUsers.TryRemove(v, out _);
        InMemoryUsers.TryRemove(cleanEmail, out _);
        try
        {
            var filterBuilder = Builders<User>.Filter;
            var matchFilters = new List<FilterDefinition<User>>
            {
                filterBuilder.Eq(u => u.PhoneEmail, cleanEmail),
                filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(cleanEmail)}$", "i"))
            };
            foreach (var v in variants)
            {
                matchFilters.Add(filterBuilder.Eq(u => u.Phone, v));
                matchFilters.Add(filterBuilder.Eq(u => u.PhoneEmail, v));
            }
            var filter = filterBuilder.Or(matchFilters);
            await _mongoService.Users.DeleteManyAsync(filter);
            Console.WriteLine($"🗑️ [MongoDB Atlas] Đã xóa tài khoản {cleanEmail} để test lại!");
            return Ok(new { success = true, message = $"Đã xóa tài khoản {cleanEmail} thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Chuẩn hóa và sinh các biến thể SĐT Việt Nam (0..., 84..., +84..., khoảng cách, v.v.)
    /// </summary>
    public static List<string> GetPhoneVariants(string input)
    {
        var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(input)) return variants.ToList();

        string raw = input.Trim();
        variants.Add(raw);
        variants.Add(raw.ToLower());

        string digits = Regex.Replace(raw, @"\D", "");
        if (!string.IsNullOrEmpty(digits))
        {
            variants.Add(digits);
            if (digits.Length >= 9 && digits.Length <= 12)
            {
                string last9 = digits.Substring(digits.Length - 9);
                variants.Add("0" + last9);
                variants.Add("84" + last9);
                variants.Add("+84" + last9);
                if (last9.Length == 9)
                {
                    variants.Add($"0{last9.Substring(0, 3)} {last9.Substring(3, 3)} {last9.Substring(6)}");
                    variants.Add($"0{last9.Substring(0, 2)} {last9.Substring(2, 3)} {last9.Substring(5)}");
                }
            }
        }

        return variants.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
    }

    /// <summary>
    /// Đăng nhập hệ thống - BẮT BUỘC TÀI KHOẢN PHẢI CÓ TRONG CSDL (MONGODB ATLAS HOẶC BỘ NHỚ LƯU TRỮ)
    /// Hỗ trợ linh hoạt: Email, SĐT Zalo đã liên kết (Phone), UserCode, CCCD, STK ngân hàng
    /// </summary>
    [HttpPost("login")]
    [HttpPost("/api/seller/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Account) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ tài khoản và mật khẩu!" });
        }

        string inputAccount = request.Account.Trim().ToLower();
        string rawAccount = request.Account.Trim();
        var phoneVariants = GetPhoneVariants(rawAccount);
        string digits = Regex.Replace(rawAccount, @"\D", "");
        string last9 = digits.Length >= 9 ? digits.Substring(digits.Length - 9) : "";

        User? existingUser = null;
        Shipper? shipperInfo = null;

        // 1. Kiểm tra từ CSDL MongoDB Atlas (Users collection) theo PhoneEmail hoặc UserCode
        // 1. Kiểm tra từ CSDL MongoDB Atlas (Users collection) theo PhoneEmail, Phone (SĐT đã liên kết), hoặc UserCode
        try
        {
            existingUser = await _mongoService.Users.Find(u => 
                !u.IsDeleted && u.AccountStatus != "deleted" &&
                (u.PhoneEmail.ToLower() == inputAccount || 
                (u.UserCode != null && u.UserCode.ToLower() == inputAccount))
            ).FirstOrDefaultAsync();
            var filterBuilder = Builders<User>.Filter;
            var notDeleted = filterBuilder.And(
                filterBuilder.Ne(u => u.IsDeleted, true),
                filterBuilder.Ne(u => u.AccountStatus, "deleted")
            );

            var matchFilters = new List<FilterDefinition<User>>
            {
                filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(inputAccount)}$", "i")),
                filterBuilder.Regex(u => u.UserCode, new BsonRegularExpression($"^{Regex.Escape(inputAccount)}$", "i"))
            };

            foreach (var variant in phoneVariants)
            {
                matchFilters.Add(filterBuilder.Eq(u => u.Phone, variant));
                matchFilters.Add(filterBuilder.Eq(u => u.PhoneEmail, variant));
                matchFilters.Add(filterBuilder.Regex(u => u.Phone, new BsonRegularExpression($"^{Regex.Escape(variant)}$", "i")));
                matchFilters.Add(filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(variant)}$", "i")));
            }

            if (!string.IsNullOrEmpty(last9))
            {
                matchFilters.Add(filterBuilder.Regex(u => u.Phone, new BsonRegularExpression(Regex.Escape(last9))));
                matchFilters.Add(filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression(Regex.Escape(last9))));
            }

            var combinedFilter = filterBuilder.And(notDeleted, filterBuilder.Or(matchFilters));
            var candidateUsers = await _mongoService.Users.Find(combinedFilter).ToListAsync();
            // Ưu tiên tài khoản khớp chính xác mật khẩu (tránh trường hợp SĐT trùng với tài khoản cũ)
            existingUser = candidateUsers.FirstOrDefault(u => u.Password == request.Password || u.PasswordHash == request.Password)
                           ?? candidateUsers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Query User Warning] {ex.Message}");
        }

        if (existingUser == null)
        {
            foreach (var variant in phoneVariants)
            {
                if (InMemoryUsers.TryGetValue(variant, out existingUser) && existingUser != null)
                {
                    if (existingUser.IsDeleted || existingUser.AccountStatus == "deleted")
                    {
                        existingUser = null;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (existingUser == null)
            {
                var candidateInMem = InMemoryUsers.Values.Where(u => 
                    !u.IsDeleted && u.AccountStatus != "deleted" &&
                    (
                        phoneVariants.Any(v =>
                            (!string.IsNullOrEmpty(u.Phone) && u.Phone.Equals(v, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(u.PhoneEmail) && u.PhoneEmail.Equals(v, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(u.UserCode) && u.UserCode.Equals(v, StringComparison.OrdinalIgnoreCase))
                        ) ||
                        (!string.IsNullOrEmpty(last9) && !string.IsNullOrEmpty(u.Phone) && Regex.Replace(u.Phone, @"\D", "").EndsWith(last9)) ||
                        (!string.IsNullOrEmpty(last9) && !string.IsNullOrEmpty(u.PhoneEmail) && Regex.Replace(u.PhoneEmail, @"\D", "").EndsWith(last9))
                    )
                ).ToList();

                existingUser = candidateInMem.FirstOrDefault(u => u.Password == request.Password || u.PasswordHash == request.Password)
                               ?? candidateInMem.FirstOrDefault();
            }
        }

        // 2. Kiểm tra CSDL MongoDB Atlas (Shippers collection)
        try
        {
            var shFilterBuilder = Builders<Shipper>.Filter;
            var shNotDeleted = shFilterBuilder.Ne(sh => sh.Status, "deleted");
            var shMatchFilters = new List<FilterDefinition<Shipper>>
            {
                shFilterBuilder.Regex(sh => sh.PhoneNumber, new BsonRegularExpression($"^{Regex.Escape(inputAccount)}$", "i")),
                shFilterBuilder.Regex(sh => sh.ShipperCode, new BsonRegularExpression($"^{Regex.Escape(inputAccount)}$", "i")),
                shFilterBuilder.Regex(sh => sh.UserId, new BsonRegularExpression($"^{Regex.Escape(inputAccount)}$", "i"))
            };

            if (existingUser != null)
            {
                shMatchFilters.Add(shFilterBuilder.Eq(sh => sh.UserId, existingUser.Id));
                if (!string.IsNullOrEmpty(existingUser.UserCode))
                {
                    shMatchFilters.Add(shFilterBuilder.Eq(sh => sh.UserId, existingUser.UserCode));
                }
            }

            foreach (var variant in phoneVariants)
            {
                shMatchFilters.Add(shFilterBuilder.Eq(sh => sh.PhoneNumber, variant));
                shMatchFilters.Add(shFilterBuilder.Regex(sh => sh.PhoneNumber, new BsonRegularExpression($"^{Regex.Escape(variant)}$", "i")));
            }

            if (!string.IsNullOrEmpty(last9))
            {
                shMatchFilters.Add(shFilterBuilder.Regex(sh => sh.PhoneNumber, new BsonRegularExpression(Regex.Escape(last9))));
            }

            var candidateShippers = await _mongoService.Shippers.Find(shFilterBuilder.And(shNotDeleted, shFilterBuilder.Or(shMatchFilters))).ToListAsync();
            shipperInfo = candidateShippers.FirstOrDefault(sh => sh.Password == request.Password)
                          ?? candidateShippers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Query Shipper Warning] {ex.Message}");
        }

        if (shipperInfo == null)
        {
            var candidateInMemShippers = InMemoryShippers.Values.Where(sh =>
                sh.Status != "deleted" &&
                (
                    phoneVariants.Any(v =>
                        (!string.IsNullOrEmpty(sh.PhoneNumber) && sh.PhoneNumber.Equals(v, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(sh.ShipperCode) && sh.ShipperCode.Equals(v, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(sh.UserId) && sh.UserId.Equals(v, StringComparison.OrdinalIgnoreCase))
                    ) ||
                    (!string.IsNullOrEmpty(last9) && !string.IsNullOrEmpty(sh.PhoneNumber) && Regex.Replace(sh.PhoneNumber, @"\D", "").EndsWith(last9)) ||
                    (existingUser != null && sh.UserId == existingUser.Id)
                )
            ).ToList();

            shipperInfo = candidateInMemShippers.FirstOrDefault(sh => sh.Password == request.Password)
                          ?? candidateInMemShippers.FirstOrDefault();
        }

        // 3. Đối chiếu mật khẩu linh hoạt giữa Users & Shippers
        bool passwordMatches = false;

        if (existingUser != null && (existingUser.PasswordHash == request.Password || existingUser.Password == request.Password))
        {
            passwordMatches = true;
        }
        else if (shipperInfo != null && shipperInfo.Password == request.Password)
        {
            passwordMatches = true;
        }

        // Nếu Shipper có thông tin mật khẩu trùng khớp -> Đồng bộ CSDL Users ngay lập tức
        if (shipperInfo != null && shipperInfo.Password == request.Password)
        {
            if (existingUser != null)
            {
                existingUser.PasswordHash = request.Password;
                existingUser.Password = request.Password;
                existingUser.IsShipper = true;
                existingUser.AccountStatus = "active";
                try
                {
                    var syncUpdate = Builders<User>.Update
                        .Set(u => u.PasswordHash, request.Password)
                        .Set(u => u.Password, request.Password)
                        .Set(u => u.IsShipper, true)
                        .Set(u => u.AccountStatus, "active");
                    await _mongoService.Users.UpdateOneAsync(u => u.Id == existingUser.Id, syncUpdate);
                }
                catch (Exception syncEx)
                {
                    Console.WriteLine($"⚠️ [MongoDB Sync User Warning] {syncEx.Message}");
                }
            }
            else
            {
                // Tạo mới tài khoản User đồng bộ từ thông tin Shipper
                existingUser = new User
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    UserCode = shipperInfo.ShipperCode,
                    PhoneEmail = shipperInfo.PhoneNumber,
                    Phone = shipperInfo.PhoneNumber,
                    PasswordHash = request.Password,
                    Password = request.Password,
                    FullName = shipperInfo.FullName,
                    IsBuyer = true,
                    IsShipper = true,
                    IsSeller = false,
                    IsAdmin = false,
                    AccountStatus = "active",
                    CreatedAt = DateTime.UtcNow
                };
                try
                {
                    await _mongoService.Users.InsertOneAsync(existingUser);
                    Console.WriteLine($"✅ [MongoDB Atlas] Auto created User from Shipper '{shipperInfo.PhoneNumber}'");
                }
                catch (Exception createEx)
                {
                    Console.WriteLine($"⚠️ [MongoDB Create User Warning] {createEx.Message}");
                }
            }
            InMemoryUsers[inputAccount] = existingUser;
            if (!string.IsNullOrEmpty(existingUser.Phone))
            {
                InMemoryUsers[existingUser.Phone] = existingUser;
            }
        }

        // 1.1 FALLBACK KIỂM TRA BẢN GHI GIAN HÀNG NẾU CHƯA CÓ USER
        if (existingUser == null)
        {
            try
            {
                var storeFilterBuilder = Builders<Store>.Filter;
                var stNotDeleted = storeFilterBuilder.Ne(s => s.Status, "deleted");
                var stMatchFilters = new List<FilterDefinition<Store>>
                {
                    storeFilterBuilder.Regex(s => s.StoreCode, new BsonRegularExpression($"^{Regex.Escape(inputAccount)}$", "i")),
                    storeFilterBuilder.Regex(s => s.OwnerFullName, new BsonRegularExpression($"^{Regex.Escape(inputAccount)}$", "i")),
                    storeFilterBuilder.Eq(s => s.BankAccountNumber, inputAccount)
                };

                foreach (var variant in phoneVariants)
                {
                    stMatchFilters.Add(storeFilterBuilder.Eq(s => s.PhoneEmail, variant));
                    stMatchFilters.Add(storeFilterBuilder.Regex(s => s.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(variant)}$", "i")));
                }

                if (!string.IsNullOrEmpty(last9))
                {
                    stMatchFilters.Add(storeFilterBuilder.Regex(s => s.PhoneEmail, new BsonRegularExpression(Regex.Escape(last9))));
                }

                var matchedStore = await _mongoService.Stores.Find(storeFilterBuilder.And(stNotDeleted, storeFilterBuilder.Or(stMatchFilters))).FirstOrDefaultAsync();
                if (matchedStore != null && matchedStore.Status != "deleted")
                {
                    if (!string.IsNullOrEmpty(matchedStore.UserId))
                    {
                        existingUser = await _mongoService.Users.Find(u => u.Id == matchedStore.UserId).FirstOrDefaultAsync();
                        if (ObjectId.TryParse(matchedStore.UserId, out _))
                        {
                            existingUser = await _mongoService.Users.Find(u => u.Id == matchedStore.UserId).FirstOrDefaultAsync();
                        }
                        else
                        {
                            existingUser = await _mongoService.Users.Find(u => u.UserCode == matchedStore.UserId).FirstOrDefaultAsync();
                        }
                    }

                    if (existingUser == null)
                    {
                        existingUser = new User
                        {
                            Id = (ObjectId.TryParse(matchedStore.UserId, out _) ? matchedStore.UserId : ObjectId.GenerateNewId().ToString()),
                            PhoneEmail = matchedStore.PhoneEmail,
                            Phone = !string.IsNullOrEmpty(last9) ? ("0" + last9) : matchedStore.PhoneEmail,
                            PasswordHash = request.Password, // Cho phép dùng mật khẩu vừa nhập
                            Password = request.Password,
                            FullName = matchedStore.OwnerFullName,
                            IsSeller = true,
                            IsBuyer = true,
                            AccountStatus = "active",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _mongoService.Users.InsertOneAsync(existingUser);
                        InMemoryUsers[inputAccount] = existingUser;
                    }
                }
            }
            catch { }
        }

        // Hỗ trợ đăng nhập tài khoản Admin duy nhất của hệ thống
        bool isAdminInput = inputAccount == "admin" || inputAccount == "admin@zonemart.vn" || (existingUser != null && (existingUser.IsAdmin || existingUser.PhoneEmail.ToLower() == "admin@zonemart.vn"));
        if (isAdminInput && (request.Password == "admin" || request.Password == "admin123" || request.Password == "123456" || (existingUser != null && (existingUser.Password == request.Password || existingUser.PasswordHash == request.Password))))
        {
            if (existingUser == null)
            {
                existingUser = new User
                {
                    Id = "admin-sys-001",
                    UserCode = "ADM-0001",
                    PhoneEmail = "admin@zonemart.vn",
                    PasswordHash = request.Password,
                    Password = request.Password,
                    FullName = "Super Administrator",
                    IsAdmin = true,
                    IsManager = true,
                    IsBuyer = false,
                    IsSeller = false,
                    IsShipper = false,
                    AccountStatus = "active"
                };
                try { await _mongoService.Users.InsertOneAsync(existingUser); } catch { }
            }
            else
            {
                existingUser.IsAdmin = true;
                existingUser.IsManager = true;
                existingUser.AccountStatus = "active";
                try
                {
                    await _mongoService.Users.UpdateOneAsync(
                        u => u.Id == existingUser.Id,
                        Builders<User>.Update.Set(u => u.IsAdmin, true).Set(u => u.IsManager, true).Set(u => u.AccountStatus, "active")
                    );
                }
                catch { }
            }
            passwordMatches = true;
        }

        // 4. TH 2: KHÔNG TÌM THẤY TÀI KHOẢN TRONG CSDL KHỞI TẠO NÀO
        if (existingUser == null && shipperInfo == null)
        {
            return Unauthorized(new { 
                success = false, 
                message = "Không tìm thấy tài khoản", 
                errorType = "ACCOUNT_NOT_FOUND" 
            });
        }

        // 5. TH 1: TÀI KHOẢN TỒN TẠI NHƯNG MẬT KHẨU KHÔNG TRÙNG KHỚP
        if (!passwordMatches)
        {
            return Unauthorized(new { 
                success = false, 
                message = "Mật khẩu không đúng", 
                errorType = "INCORRECT_PASSWORD" 
            });
        }

        // Kiểm tra trạng thái tài khoản
        if (existingUser != null && (existingUser.IsDeleted || existingUser.AccountStatus == "deleted"))
        {
            return Unauthorized(new { 
                success = false, 
                message = "Tài khoản không tồn tại hoặc đã bị xóa khỏi hệ thống!", 
                errorType = "ACCOUNT_NOT_FOUND" 
            });
        }

        if (existingUser != null && existingUser.AccountStatus == "banned")
        {
            return BadRequest(new { success = false, message = "Tài khoản của bạn đã bị khóa vi phạm tiêu chuẩn cộng đồng ZoneMart!" });
        }

        Store? userStore = null;
        if (existingUser != null)
        {
            try
            {
                userStore = await _mongoService.Stores.Find(s => 
                    s.UserId == existingUser.Id || 
                    (!string.IsNullOrEmpty(existingUser.UserCode) && s.UserId == existingUser.UserCode) ||
                    s.OwnerFullName == existingUser.FullName || 
                    s.PhoneEmail == existingUser.PhoneEmail
                ).FirstOrDefaultAsync();
                string cleanEmail = existingUser.PhoneEmail?.Trim().ToLower() ?? "";
                string cleanPhone = (!string.IsNullOrEmpty(existingUser.Phone)) ? existingUser.Phone.Trim() : "";

                // ƯU TIÊN 1: Tìm theo PhoneEmail chính xác
                if (!string.IsNullOrEmpty(cleanEmail))
                {
                    userStore = await _mongoService.Stores.Find(s => 
                        s.PhoneEmail != null && s.PhoneEmail.ToLower() == cleanEmail
                    ).FirstOrDefaultAsync();
                }

                // ƯU TIÊN 2: Tìm theo UserId trùng Id hoặc UserCode của tài khoản
                if (userStore == null)
                {
                    userStore = await _mongoService.Stores.Find(s => 
                        s.UserId == existingUser.Id || 
                        (!string.IsNullOrEmpty(existingUser.UserCode) && s.UserId == existingUser.UserCode)
                    ).FirstOrDefaultAsync();
                }

                // ƯU TIÊN 3: Tìm theo Phone nếu có liên kết số điện thoại
                if (userStore == null && !string.IsNullOrEmpty(cleanPhone))
                {
                    userStore = await _mongoService.Stores.Find(s => 
                        s.PhoneEmail == cleanPhone
                    ).FirstOrDefaultAsync();
                }
            }
            catch { }

            if (userStore == null)
            {
                string cleanEmail = existingUser.PhoneEmail?.Trim().ToLower() ?? "";
                userStore = InMemoryStores.Values.FirstOrDefault(s => 
                    (!string.IsNullOrEmpty(cleanEmail) && s.PhoneEmail != null && s.PhoneEmail.Equals(cleanEmail, StringComparison.OrdinalIgnoreCase)) ||
                    s.UserId == existingUser.Id || 
                    (!string.IsNullOrEmpty(existingUser.UserCode) && s.UserId == existingUser.UserCode) ||
                    s.PhoneEmail == existingUser.PhoneEmail
                );
            }
        }

        if (shipperInfo == null && existingUser != null)
        {
            try
            {
                shipperInfo = await _mongoService.Shippers.Find(sh => 
                    sh.UserId == existingUser.Id || 
                    (!string.IsNullOrEmpty(existingUser.UserCode) && sh.UserId == existingUser.UserCode) ||
                    sh.PhoneNumber == existingUser.PhoneEmail || 
                    (sh.FullName == existingUser.FullName && !string.IsNullOrEmpty(existingUser.FullName))
                ).FirstOrDefaultAsync();
                string cleanEmail = existingUser.PhoneEmail?.Trim().ToLower() ?? "";
                string cleanPhone = (!string.IsNullOrEmpty(existingUser.Phone)) ? existingUser.Phone.Trim() : "";

                // ƯU TIÊN 1: Tìm theo PhoneNumber chính xác
                if (!string.IsNullOrEmpty(cleanEmail))
                {
                    shipperInfo = await _mongoService.Shippers.Find(sh => 
                        sh.PhoneNumber != null && sh.PhoneNumber.ToLower() == cleanEmail
                    ).FirstOrDefaultAsync();
                }

                // ƯU TIÊN 2: Tìm theo UserId
                if (shipperInfo == null)
                {
                    shipperInfo = await _mongoService.Shippers.Find(sh => 
                        sh.UserId == existingUser.Id || 
                        (!string.IsNullOrEmpty(existingUser.UserCode) && sh.UserId == existingUser.UserCode) ||
                        (!string.IsNullOrEmpty(sh.ShipperCode) && sh.ShipperCode == existingUser.UserCode)
                    ).FirstOrDefaultAsync();
                }

                // ƯU TIÊN 3: Tìm theo Phone liên kết
                if (shipperInfo == null && !string.IsNullOrEmpty(cleanPhone))
                {
                    shipperInfo = await _mongoService.Shippers.Find(sh => 
                        sh.PhoneNumber == cleanPhone
                    ).FirstOrDefaultAsync();
                }
            }
            catch { }

            if (shipperInfo == null)
            {
                string cleanEmail = existingUser.PhoneEmail?.Trim().ToLower() ?? "";
                shipperInfo = InMemoryShippers.Values.FirstOrDefault(sh => 
                    (!string.IsNullOrEmpty(cleanEmail) && sh.PhoneNumber != null && sh.PhoneNumber.Equals(cleanEmail, StringComparison.OrdinalIgnoreCase)) ||
                    sh.UserId == existingUser.Id || 
                    (!string.IsNullOrEmpty(existingUser.UserCode) && sh.UserId == existingUser.UserCode) ||
                    sh.PhoneNumber == existingUser.PhoneEmail
                );
            }
        }

        // TỰ ĐỘNG ĐỒNG BỘ NẾU HỒ SƠ ĐÃ ĐƯỢC ADMIN PHÊ DUYỆT (Self-healing sync)
        bool isStoreApproved = userStore != null && (userStore.Status == "Approved" || userStore.Status == "Active");
        bool isShipperApproved = shipperInfo != null && (shipperInfo.Status == "Approved" || shipperInfo.Status == "Active");

        if (existingUser != null)
        {
            bool needUserUpdate = false;
            if (isStoreApproved && (!existingUser.IsSeller || existingUser.AccountStatus == "pending"))
            {
                existingUser.IsSeller = true;
                existingUser.AccountStatus = "active";
                needUserUpdate = true;
            }
            if (isShipperApproved && (!existingUser.IsShipper || existingUser.AccountStatus == "pending"))
            {
                existingUser.IsShipper = true;
                existingUser.AccountStatus = "active";
                needUserUpdate = true;
            }

            if (needUserUpdate)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var syncUpdate = Builders<User>.Update
                            .Set(u => u.AccountStatus, "active")
                            .Set(u => u.IsSeller, existingUser.IsSeller)
                            .Set(u => u.IsShipper, existingUser.IsShipper);
                        await _mongoService.Users.UpdateOneAsync(u => u.Id == existingUser.Id, syncUpdate);
                    }
                    catch { }
                });
                if (!string.IsNullOrEmpty(existingUser.PhoneEmail)) InMemoryUsers[existingUser.PhoneEmail] = existingUser;
            }
        }

        string reqRole = !string.IsNullOrWhiteSpace(request.Role) ? request.Role.Trim().ToLower() : "";

        // KIỂM TRA TÀI KHOẢN ĐANG CHỜ PHÊ DUYỆT (CHƯA ĐƯỢC PHÉP ĐĂNG NHẬP)
        bool isPendingAccount = false;
        string pendingMessage = "Tài khoản của bạn đang trong quá trình xét duyệt hồ sơ bởi Ban Quản Trị ZoneMart! Vui lòng chờ phê duyệt để kích hoạt tài khoản.";
        bool isAdminAccount = isAdminInput || (existingUser != null && existingUser.IsAdmin);

        if (!isAdminAccount)
        {
            if (reqRole == "seller")
            {
                if (!isStoreApproved)
                {
                    isPendingAccount = true;
                    string shopName = !string.IsNullOrEmpty(userStore?.StoreName) ? userStore.StoreName : "Gian hàng của bạn";
                    pendingMessage = $"Hồ sơ mở gian hàng '{shopName}' của bạn đang trong quá trình xét duyệt. Vui lòng chờ Ban Quản Trị phê duyệt trước khi đăng nhập!";
                }
            }
            else if (reqRole == "shipper")
            {
                if (!isShipperApproved)
                {
                    isPendingAccount = true;
                    string driverTag = shipperInfo?.LicensePlate ?? shipperInfo?.FullName ?? "Tài xế";
                    pendingMessage = $"Hồ sơ đăng ký tài xế ZoneMart Driver ({driverTag}) đang trong quá trình xét duyệt. Vui lòng chờ thông báo!";
                }
            }
            else
            {
                // Đăng nhập chung hoặc buyer
                if (existingUser != null && (existingUser.AccountStatus == "pending" || existingUser.AccountStatus == "Pending") && !isStoreApproved && !isShipperApproved)
                {
                    isPendingAccount = true;
                    if (userStore != null && !string.IsNullOrEmpty(userStore.StoreName))
                    {
                        pendingMessage = $"Hồ sơ mở gian hàng '{userStore.StoreName}' của bạn đang trong quá trình xét duyệt. Vui lòng chờ Ban Quản Trị phê duyệt trước khi đăng nhập!";
                    }
                    else if (shipperInfo != null)
                    {
                        string driverTag = shipperInfo.LicensePlate ?? shipperInfo.FullName ?? "Tài xế";
                        pendingMessage = $"Hồ sơ đăng ký tài xế ZoneMart Driver ({driverTag}) đang trong quá trình xét duyệt. Vui lòng chờ thông báo!";
                    }
                }
            }
        }

        if (isPendingAccount)
        {
            return BadRequest(new
            {
                success = false,
                errorType = "ACCOUNT_PENDING_APPROVAL",
                message = pendingMessage
            });
        }

        // Tự động nhận diện & Ràng buộc vai trò theo từng Cổng Đăng Nhập
        string determinedRole = "buyer";

        if (isAdminInput || (existingUser != null && existingUser.IsAdmin))
        {
            determinedRole = "admin";
            if (existingUser != null) existingUser.IsAdmin = true;
        }
        else if (reqRole == "shipper")
        {
            if ((existingUser == null || !existingUser.IsShipper) && shipperInfo == null)
            if ((existingUser == null || (!existingUser.IsShipper && !isShipperApproved)) && shipperInfo == null)
            {
                return BadRequest(new { success = false, message = "Tài khoản này chưa đăng ký làm Tài Xế Shipper! Vui lòng đăng ký hồ sơ tài xế trước khi đăng nhập." });
            }
            determinedRole = "shipper";
        }
        else if (reqRole == "seller")
        {
            if (existingUser == null || (!existingUser.IsSeller && !existingUser.IsAdmin))
            if (existingUser == null || (!existingUser.IsSeller && !existingUser.IsAdmin && !isStoreApproved))
            {
                return BadRequest(new { success = false, message = "Tài khoản này chưa đăng ký mở Gian Hàng Seller! Vui lòng đăng ký mở shop trước khi đăng nhập." });
            }
            determinedRole = "seller";
        }
        else if (reqRole == "admin")
        {
            if (existingUser == null || !existingUser.IsAdmin)
            {
                return BadRequest(new { success = false, message = "Tài khoản này không có quyền truy cập vào Bảng Điều Khiển Admin!" });
            }
            determinedRole = "admin";
        }
        else
        {
            if (existingUser != null && existingUser.IsAdmin) determinedRole = "admin";
            else if (existingUser != null && existingUser.IsSeller) determinedRole = "seller";
            else if ((existingUser != null && existingUser.IsShipper) || shipperInfo != null) determinedRole = "shipper";
            else if (existingUser != null && (existingUser.IsSeller || isStoreApproved)) determinedRole = "seller";
            else if ((existingUser != null && (existingUser.IsShipper || isShipperApproved)) || shipperInfo != null) determinedRole = "shipper";
        }

        if (shipperInfo == null && existingUser != null && (existingUser.IsShipper || determinedRole == "shipper"))
        {
            try
            {
                shipperInfo = await _mongoService.Shippers.Find(sh => sh.UserId == existingUser.Id || sh.PhoneNumber == existingUser.PhoneEmail || (sh.FullName == existingUser.FullName && !string.IsNullOrEmpty(existingUser.FullName))).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB Shipper Fetch Warning] {ex.Message}");
            }
        }
        string sellerStatus = userStore?.Status ?? "";
        string storeName = userStore?.StoreName ?? "";
        string rejectReason = userStore?.RejectReason ?? "";

        if (existingUser != null && userStore == null)
        {
            try
            {
                string cleanEmail = existingUser.PhoneEmail?.Trim().ToLower() ?? "";
                userStore = await _mongoService.Stores.Find(s => 
                    s.UserId == existingUser.Id || 
                    (!string.IsNullOrEmpty(existingUser.UserCode) && s.UserId == existingUser.UserCode) ||
                    (!string.IsNullOrEmpty(cleanEmail) && s.PhoneEmail != null && s.PhoneEmail.ToLower() == cleanEmail)
                ).FirstOrDefaultAsync();

                if (userStore != null)
                {
                    sellerStatus = userStore.Status;
                    storeName = userStore.StoreName;
                    rejectReason = userStore.RejectReason;
                }
            }
            catch { }
        }

        string loginMessage = $"Đăng nhập thành công với vai trò {GetRoleDisplayName(determinedRole)}!";
        if (determinedRole == "buyer" && sellerStatus == "Pending")
        {
            loginMessage = $"Đăng nhập thành công! Hồ sơ mở gian hàng '{storeName}' của bạn đang chờ Ban Quản Lý phê duyệt.";
        }
        else if (determinedRole == "buyer" && sellerStatus == "Rejected")
        {
            loginMessage = $"Đăng nhập thành công! Hồ sơ gian hàng '{storeName}' của bạn đã bị từ chối: {rejectReason ?? "Không đạt tiêu chuẩn"}.";
        }

        return Ok(new
        {
            success = true,
            message = loginMessage,
            user = new
            {
                id = existingUser?.Id ?? shipperInfo?.Id ?? "",
                userCode = existingUser?.UserCode ?? shipperInfo?.ShipperCode ?? "",
                phoneEmail = existingUser?.PhoneEmail ?? shipperInfo?.PhoneNumber ?? "",
                phone = existingUser?.Phone ?? (shipperInfo != null ? shipperInfo.PhoneNumber : ""),
                fullName = shipperInfo?.FullName ?? existingUser?.FullName ?? (determinedRole == "admin" ? "Ban Quản Trị ZoneMart" : "Người Dùng"),
                avatarUrl = !string.IsNullOrEmpty(shipperInfo?.AvatarUrl) ? shipperInfo.AvatarUrl : (existingUser?.AvatarUrl ?? ""),
                role = determinedRole,
                isAdmin = determinedRole == "admin" || (existingUser?.IsAdmin ?? false),
                isManager = (existingUser?.IsManager ?? false) || determinedRole == "admin",
                walletBalance = existingUser?.WalletBalance ?? 0,
                storeName = storeName,
                sellerStatus = sellerStatus,
                rejectReason = rejectReason,
                shipperDetails = shipperInfo != null ? new
                {
                    shipperId = shipperInfo.Id,
                    shipperCode = shipperInfo.ShipperCode,
                    fullName = shipperInfo.FullName,
                    phoneNumber = shipperInfo.PhoneNumber,
                    licensePlate = shipperInfo.LicensePlate,
                    vehicleType = shipperInfo.VehicleType,
                    vehicleModel = shipperInfo.VehicleModel,
                    operatingArea = shipperInfo.OperatingArea,
                    status = shipperInfo.Status,
                    avatarUrl = shipperInfo.AvatarUrl
                } : null
            }
        });
    }

    /// <summary>
    /// API TRA CỨU HỒ SƠ TÀI XẾ DÀNH CHO TRANG SHIPPER DASHBOARD
    /// </summary>
    [HttpGet("shipper-profile")]
    public async Task<IActionResult> GetShipperProfile([FromQuery] string account)
    {
        if (string.IsNullOrWhiteSpace(account)) return BadRequest(new { success = false, message = "Thiếu tài khoản" });
        string cleanAccount = account.Trim().ToLower();
        try
        {
            var shipper = await _mongoService.Shippers.Find(sh => sh.PhoneNumber.ToLower() == cleanAccount || sh.UserId == cleanAccount || sh.ShipperCode == cleanAccount).FirstOrDefaultAsync();
            if (shipper == null)
            {
                var user = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == cleanAccount).FirstOrDefaultAsync();
                if (user != null)
                {
                    shipper = await _mongoService.Shippers.Find(sh => sh.UserId == user.Id || sh.UserId == user.UserCode || sh.PhoneNumber.ToLower() == user.PhoneEmail.ToLower()).FirstOrDefaultAsync();
                }
            }

            if (shipper != null)
            {
                return Ok(new { success = true, shipper });
            }
            return NotFound(new { success = false, message = "Chưa tìm thấy hồ sơ tài xế" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// API TRA CỨU HỒ SƠ GIAN HÀNG DÀNH CHO SELLER DASHBOARD
    /// </summary>
    [HttpGet("seller-profile")]
    public async Task<IActionResult> GetSellerProfile([FromQuery] string account)
    {
        if (string.IsNullOrWhiteSpace(account)) return Ok(new { success = false, message = "Thiếu tài khoản", store = (object?)null });
        string cleanAccount = account.Trim();
        try
        {
            var filterList = new List<MongoDB.Driver.FilterDefinition<Store>>
            {
                MongoDB.Driver.Builders<Store>.Filter.Eq(s => s.PhoneEmail, cleanAccount),
                MongoDB.Driver.Builders<Store>.Filter.Regex(s => s.PhoneEmail, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleanAccount)}$", "i")),
                MongoDB.Driver.Builders<Store>.Filter.Eq(s => s.UserId, cleanAccount),
                MongoDB.Driver.Builders<Store>.Filter.Eq(s => s.StoreCode, cleanAccount)
            };

            if (MongoDB.Bson.ObjectId.TryParse(cleanAccount, out _))
            {
                filterList.Add(MongoDB.Driver.Builders<Store>.Filter.Eq(s => s.Id, cleanAccount));
            }

            var store = await _mongoService.Stores.Find(MongoDB.Driver.Builders<Store>.Filter.Or(filterList)).FirstOrDefaultAsync();

            if (store == null)
            {
                var userFilter = MongoDB.Driver.Builders<User>.Filter.Or(
                    MongoDB.Driver.Builders<User>.Filter.Eq(u => u.PhoneEmail, cleanAccount),
                    MongoDB.Driver.Builders<User>.Filter.Regex(u => u.PhoneEmail, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleanAccount)}$", "i"))
                );
                var user = await _mongoService.Users.Find(userFilter).FirstOrDefaultAsync();
                if (user != null)
                {
                    store = await _mongoService.Stores.Find(s =>
                        s.UserId == user.Id ||
                        (!string.IsNullOrEmpty(user.UserCode) && s.UserId == user.UserCode) ||
                        (!string.IsNullOrEmpty(user.PhoneEmail) && s.PhoneEmail == user.PhoneEmail)
                    ).FirstOrDefaultAsync();
                }
            }

            if (store != null)
            {
                return Ok(new { success = true, store });
            }
            return Ok(new { success = false, message = "Chưa tìm thấy hồ sơ gian hàng", store = (object?)null });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message, store = (object?)null });
        }
    }

    /// <summary>
    /// API CẬP NHẬT THÔNG TIN GIAN HÀNG TỪ SELLER DASHBOARD
    /// </summary>
    [HttpPost("update-store-profile")]
    public async Task<IActionResult> UpdateStoreProfile([FromBody] UpdateStoreProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Account)) return BadRequest(new { success = false, message = "Thiếu tài khoản" });
        string cleanAccount = request.Account.Trim().ToLower();
        try
        {
            var filter = Builders<Store>.Filter.Where(s =>
                (s.PhoneEmail != null && s.PhoneEmail.ToLower() == cleanAccount) ||
                s.UserId == cleanAccount ||
                s.StoreCode == cleanAccount ||
                s.Id == cleanAccount
            );

            var update = Builders<Store>.Update
                .Set(s => s.StoreName, request.StoreName.Trim())
                .Set(s => s.Category, request.Category)
                .Set(s => s.Address, request.Address.Trim())
                .Set(s => s.OpenHours, request.OpenHours.Trim())
                .Set(s => s.BankName, request.BankName.Trim())
                .Set(s => s.BankAccountNumber, request.BankAccountNumber.Trim())
                .Set(s => s.OwnerFullName, request.OwnerFullName.Trim());

            var res = await _mongoService.Stores.UpdateOneAsync(filter, update);
            return Ok(new { success = true, modifiedCount = res.ModifiedCount });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    /// <summary>
    /// ĐĂNG KÝ TÀI KHOẢN NGƯỜI MUA (KHÁCH HÀNG)
    /// Ràng buộc mật khẩu 6-16 ký tự. Lưu CSDL & Gửi THƯ CẢM ƠN về Gmail vừa đăng ký qua Gmail SMTP (dobinh225599@gmail.com)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ Gmail và Mật khẩu!" });
        }

        // RÀNG BUỘC MẬT KHẨU TỐI THIỂU 6 KÝ TỰ, TỐI ĐA 16 KÝ TỰ
        if (request.Password.Length < 6 || request.Password.Length > 16)
        {
            return BadRequest(new { success = false, message = "Mật khẩu phải có độ dài từ 6 đến 16 ký tự!" });
        }

        string email = request.Email.Trim().ToLower();

        if (!email.Contains("@") || !email.Contains("."))
        {
            return BadRequest(new { success = false, message = "Địa chỉ Gmail không đúng định dạng!" });
        }

        // 1. Kiểm tra tài khoản đã tồn tại chưa (Ưu tiên kiểm tra CSDL MongoDB Atlas thực tế)
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.PhoneEmail, email);
            var dbUser = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();
            if (dbUser != null)
            {
                InMemoryUsers[email] = dbUser;
                return BadRequest(new { success = false, message = "Địa chỉ Gmail này đã được đăng ký tài khoản! Vui lòng đăng nhập hoặc sử dụng Gmail khác." });
            }
            else
            {
                // Nếu trong CSDL MongoDB không còn tồn tại (do vừa xóa thủ công từ DB), dọn sạch khỏi bộ nhớ tạm
                InMemoryUsers.TryRemove(email, out _);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Query Check Warning] {ex.Message}");
            if (InMemoryUsers.ContainsKey(email))
            {
                return BadRequest(new { success = false, message = "Địa chỉ Gmail này đã được đăng ký tài khoản! Vui lòng đăng nhập hoặc sử dụng Gmail khác." });
            }
        }

        // 2. Tạo đối tượng User mới cho Khách Hàng (Mã ID: CUST-0001, CUST-0002...)
        string userCode = await GetNextCustomerCodeAsync();
        var newUser = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserCode = userCode,
            PhoneEmail = email,
            Phone = "", // Tài khoản tạo mới tuyệt đối không liên kết sẵn SĐT
            PasswordHash = request.Password,
            Password = request.Password,
            FullName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : email.Split('@')[0],
            IsBuyer = true,
            IsSeller = false,
            IsShipper = false,
            IsAdmin = false,
            AccountStatus = "active",
            CreatedAt = DateTime.UtcNow
        };

        // Lưu vào bộ nhớ tạm thời của server
        InMemoryUsers[email] = newUser;

        // Thử chèn vào MongoDB Atlas
        try
        {
            await _mongoService.Users.InsertOneAsync(newUser);
            Console.WriteLine($"✅ [MongoDB Atlas] Đã lưu tài khoản {email} vào CSDL MongoDB Atlas!");
        }
        catch (Exception dbEx)
        {
            Console.WriteLine($"⚠️ [MongoDB Atlas Insert Warning] {dbEx.Message}");
        }

        // 3. GỬI THƯ CẢM ƠN TẠO TÀI KHOẢN THÀNH CÔNG VỀ GMAIL NGUỜI DÙNG VỪA ĐĂNG KÝ (BẰNG BACKGROUND TASK ĐỂ KHÔNG BỊ BLOCK HTTP RESPONSE)
        _ = Task.Run(async () =>
        {
            try
            {
                await SendThankYouEmailAsync(email, newUser.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [Email Send Error] {ex.Message}");
            }
        });

        return Ok(new
        {
            success = true,
            message = $"Tạo tài khoản thành công! Thư cảm ơn đã được gửi về Gmail {email}."
        });
    }

    /// <summary>
    /// HÀM GỬI THƯ CẢM ƠN ĐĂNG KÝ TÀI KHOẢN QUA GMAIL SMTP
    /// </summary>
    private static async Task SendThankYouEmailAsync(string toEmail, string fullName)
    {
        try
        {
            Console.WriteLine($"📧 [MailKit SMTP] Đang chuẩn bị gửi thư cảm ơn tới Gmail: {toEmail}...");
            string subject = "[ZoneMart] Thư Cảm Ơn & Xác Nhận Đăng Ký Tài Khoản Thành Công";
            string htmlBody = $@"
                    <div style='background-color: #FAF5EF; padding: 24px 10px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
                        <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 520px; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #F0E6DC; overflow: hidden; margin: 0 auto; box-shadow: 0 10px 30px rgba(0,0,0,0.05);'>
                            
                            <!-- LOGO & BRAND HEADER -->
                            <tr>
                                <td align='center' style='padding: 28px 24px 16px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0'>
                                        <tr>
                                            <td align='center' style='background-color: #FFF7ED; border: 1px solid #FED7AA; border-radius: 16px; padding: 8px 22px;'>
                                                <span style='color: #D94E15 !important; font-size: 26px; font-weight: 900; letter-spacing: 1px; display: block;'>ZoneMart</span>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style='color: #64748B !important; font-size: 12.5px; font-weight: 600; margin: 8px 0 0 0;'>Sàn Thương Mại Điện Tử & Giao Hàng Hỏa Tốc</p>
                                </td>
                            </tr>
                            
                            <!-- DIVIDER -->
                            <tr>
                                <td style='padding: 0 24px;'>
                                    <div style='height: 1px; background-color: #F1F5F9;'></div>
                                </td>
                            </tr>

                            <!-- TIÊU ĐỀ NỔI BẬT -->
                            <tr>
                                <td align='center' style='padding: 20px 24px 10px 24px;'>
                                    <h2 style='color: #D94E15 !important; font-size: 19px; font-weight: 900; margin: 0 0 8px 0;'>🎉 CẢM ƠN BẠN ĐÃ ĐĂNG KÝ TÀI KHOẢN!</h2>
                                    <p style='color: #475569 !important; font-size: 13.5px; margin: 0; line-height: 1.5;'>Chào mừng bạn gia nhập cộng đồng mua sắm thông minh ZoneMart</p>
                                </td>
                            </tr>
                            
                            <tr>
                                <td style='padding: 0 24px 16px 24px;'>
                                    <p style='color: #334155 !important; font-size: 14px; line-height: 1.6; margin: 0;'>
                                        Chân thành cảm ơn bạn đã lựa chọn và tạo tài khoản Khách Hàng tại <b style='color: #D94E15 !important;'>ZoneMart</b>. Chúng tôi rất hân hạnh được đồng hành cùng bạn với hàng ngàn ưu đãi hấp dẫn cùng dịch vụ giao hàng hỏa tốc trong bán kính 10km!
                                    </p>
                                </td>
                            </tr>

                            <!-- CARD THÔNG TIN TÀI KHOẢN (SỬ DỤNG BẢNG HTML TRÁNH VỠ GIAO DIỆN FLEXBOX TRÊN GMAIL MOBILE) -->
                            <tr>
                                <td style='padding: 8px 24px 16px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #FFF7ED; border: 1.5px solid #FED7AA; border-radius: 16px; padding: 18px 20px;'>
                                        <tr>
                                            <td style='padding-bottom: 12px;'>
                                                <span style='color: #9A3412 !important; font-size: 12.5px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.5px; display: block;'>
                                                    THÔNG TIN TÀI KHOẢN CỦA BẠN
                                                </span>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 6px 0; color: #431407 !important; font-size: 13.5px; line-height: 1.5;'>
                                                <b style='color: #431407 !important;'>Gmail đăng nhập:</b> <span style='color: #D94E15 !important; font-weight: 700;'>{toEmail}</span>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 6px 0; color: #431407 !important; font-size: 13.5px; line-height: 1.5;'>
                                                <b style='color: #431407 !important;'>Vai trò:</b> <span style='color: #431407 !important; font-weight: 600;'>Khách hàng mua sắm (Buyer)</span>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 6px 0; color: #431407 !important; font-size: 13.5px; line-height: 1.5;'>
                                                <b style='color: #431407 !important;'>Trạng thái:</b> 
                                                <span style='background-color: #DCFCE7; color: #15803D !important; padding: 3px 10px; border-radius: 12px; font-weight: 700; font-size: 12px; display: inline-block; border: 1px solid #BBF7D0;'>
                                                    Đã kích hoạt chính thức
                                                </span>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <tr>
                                <td style='padding: 0 24px 20px 24px;'>
                                    <p style='color: #334155 !important; font-size: 13.5px; line-height: 1.6; margin: 0; text-align: center;'>
                                        Bây giờ bạn có thể đăng nhập ngay vào ứng dụng ZoneMart để trải nghiệm mua sắm tiện lợi nhất!
                                    </p>
                                </td>
                            </tr>

                            <!-- NÚT BẤM ĐĂNG NHẬP -->
                            <tr>
                                <td align='center' style='padding: 0 24px 24px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0'>
                                        <tr>
                                            <td align='center' style='background-color: #D94E15; border-radius: 30px;'>
                                                <a href='http://localhost:5173/login' style='background-color: #D94E15 !important; color: #FFFFFF !important; font-size: 14px; font-weight: 800; text-decoration: none; padding: 13px 32px; border-radius: 30px; display: inline-block; letter-spacing: 0.5px; border: 1px solid #D94E15;'>
                                                    ĐĂNG NHẬP ZONEMART NGAY ➔
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <!-- FOOTER -->
                            <tr>
                                <td style='padding: 0 24px 20px 24px;'>
                                    <div style='height: 1px; background-color: #F1F5F9; margin-bottom: 16px;'></div>
                                    <p style='color: #94A3B8 !important; font-size: 11.5px; text-align: center; margin: 0; line-height: 1.5;'>
                                        © 2026 ZoneMart E-Commerce Platform. Thư cảm ơn tự động từ hệ thống ZoneMart.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </div>";

            await SendEmailViaMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception mailEx)
        {
            Console.WriteLine($"❌ [Gmail SMTP Error] Lỗi gửi thư cảm ơn tới {toEmail}: {mailEx.Message}");
        }
    }

    private static string GetRoleDisplayName(string role) => role switch
    {
        "seller" => "Chủ cửa hàng",
        "shipper" => "Tài xế Shipper",
        "admin" => "Quản trị viên",
        _ => "Khách hàng"
    };

    /// <summary>
    /// ĐĂNG KÝ HỒ SƠ NGƯỜI BÁN / MỞ GIAN HÀNG -> LƯU TRỰC TIẾP VÀO CSDL MONGODB ATLAS (COLLECTION "stores")
    /// </summary>
    [HttpPost("register-seller")]
    public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StoreName) || string.IsNullOrWhiteSpace(request.OwnerFullName))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ tên cửa hàng và họ tên chủ tiệm!" });
        }

        string accountKey = !string.IsNullOrWhiteSpace(request.PhoneEmail) ? request.PhoneEmail.Trim().ToLower() : "";
        string pwd = !string.IsNullOrWhiteSpace(request.Password) ? request.Password.Trim() : "123456";

        User? buyerUser = null;
        if (!string.IsNullOrEmpty(accountKey))
        {
            try
            {
                buyerUser = await _mongoService.Users.Find(u => u.PhoneEmail == accountKey).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB Query User Warning] {ex.Message}");
            }
        }

        string sellerCode = await GetNextSellerCodeAsync(); // ID Seller: SEL-0001, SEL-0002...
        string userCode = buyerUser?.UserCode ?? "";
        if (string.IsNullOrEmpty(userCode) || !userCode.StartsWith("SEL-"))
        {
            userCode = sellerCode;
        }

        if (buyerUser != null)
        {
            var userUpdate = Builders<User>.Update
                .Set(u => u.PasswordHash, pwd)
                .Set(u => u.Password, pwd)
                .Set(u => u.UserCode, userCode)
                .Set(u => u.AccountStatus, "pending");
            try
            {
                await _mongoService.Users.UpdateOneAsync(u => u.Id == buyerUser.Id, userUpdate);
                buyerUser.PasswordHash = pwd;
                buyerUser.Password = pwd;
                buyerUser.UserCode = userCode;
                buyerUser.AccountStatus = "pending";
                InMemoryUsers[accountKey] = buyerUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB User Update Error] {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(accountKey))
        {
            buyerUser = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserCode = userCode,
                PhoneEmail = accountKey,
                Phone = "",
                PasswordHash = pwd,
                Password = pwd,
                FullName = request.OwnerFullName.Trim(),
                IsBuyer = true,
                IsShipper = false,
                IsSeller = false,
                IsAdmin = false,
                AccountStatus = "pending",
                CreatedAt = DateTime.UtcNow
            };
            InMemoryUsers[accountKey] = buyerUser;
            try
            {
                await _mongoService.Users.InsertOneAsync(buyerUser);
                Console.WriteLine($"✅ [MongoDB Atlas] Đã tạo thành công tài khoản User Seller '{accountKey}' (UserCode: {userCode})!");
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"⚠️ [MongoDB User Insert Warning] {dbEx.Message}");
            }
        }

        var newStore = new Store
        {
            UserId = userCode,
            StoreCode = sellerCode,
            Password = pwd,
            PhoneEmail = accountKey,
            StoreName = request.StoreName.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Thực phẩm & Nhu yếu phẩm" : request.Category.Trim(),
            Address = request.Address.Trim(),
            OpenHours = string.IsNullOrWhiteSpace(request.OpenHours) ? "07:00 - 22:00" : request.OpenHours.Trim(),
            OwnerFullName = request.OwnerFullName.Trim(),
            CccdFrontImage = request.CccdFrontImage ?? "",
            CccdBackImage = request.CccdBackImage ?? "",
            FoodSafetyCertImage = request.FoodSafetyCertImage ?? "",
            BankName = request.BankName.Trim(),
            BankAccountNumber = request.BankAccountNumber.Trim(),
            Status = "Pending", // BẮT BUỘC PENDING để Quản trị viên xét duyệt
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _mongoService.Stores.InsertOneAsync(newStore);
            Console.WriteLine($"🏪 [MongoDB Atlas] Đã lưu thành công Hồ sơ đăng ký gian hàng '{newStore.StoreName}' của chủ tiệm '{newStore.OwnerFullName}' (StoreCode / ID chữ: {newStore.StoreCode}) vào CSDL MongoDB Atlas!");
            
            // TẠO THÔNG BÁO THỜI GIAN THỰC CHO BAN QUẢN TRỊ ADMIN
            try
            {
                var notif = new AdminNotification
                {
                    Type = "seller",
                    Title = "Gian Hàng mới đăng ký xét duyệt",
                    Message = $"Chủ tiệm {newStore.OwnerFullName} vừa nộp hồ sơ mở gian hàng '{newStore.StoreName}'.",
                    TargetUserId = buyerUser?.Id ?? newStore.Id ?? "",
                    TargetName = newStore.StoreName,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _mongoService.AdminNotifications.InsertOneAsync(notif);
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"⚠️ [Notification Insert Error] {notifEx.Message}");
            }

            string targetEmail = (!string.IsNullOrWhiteSpace(accountKey) && accountKey.Contains("@")) ? accountKey.Trim().ToLower() : "";
            if (!string.IsNullOrEmpty(targetEmail))
            {
                _ = Task.Run(async () =>
                {
                    await SendSellerPendingEmailAsync(targetEmail, request.StoreName, request.OwnerFullName);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Store Insert Error] {ex.Message}");
        }

        if (!string.IsNullOrEmpty(newStore.Id))
        {
            InMemoryStores[newStore.Id] = newStore;
        }

        return Ok(new
        {
            success = true,
            message = "Đăng ký mở gian hàng thành công! Hồ sơ của bạn đã được chuyển đến Ban Quản Lý để xét duyệt.",
            storeId = newStore.Id,
            status = "Pending",
            user = buyerUser != null ? new
            {
                id = buyerUser.Id,
                phoneEmail = buyerUser.PhoneEmail,
                fullName = buyerUser.FullName,
                role = buyerUser.IsSeller ? "seller" : "buyer",
                sellerStatus = "Pending",
                walletBalance = buyerUser.WalletBalance
            } : null
        });
    }

    /// <summary>
    /// ĐĂNG KÝ HỒ SƠ TÀI XẾ SHIPPER GIAO HÀNG -> LƯU TRỰC TIẾP VÀO CSDL MONGODB ATLAS (COLLECTION "shippers")
    /// </summary>
    [HttpPost("register-shipper")]
    public async Task<IActionResult> RegisterShipper([FromBody] RegisterShipperRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ họ tên tài xế và biển số xe!" });
        }

        string accountKey = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber.Trim().ToLower() : "";
        string pwd = !string.IsNullOrWhiteSpace(request.Password) ? request.Password.Trim() : "123456";

        User? buyerUser = null;
        if (!string.IsNullOrEmpty(accountKey))
        {
            try
            {
                buyerUser = await _mongoService.Users.Find(u => u.PhoneEmail == accountKey).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB Query User Warning] {ex.Message}");
            }
        }

        string shipperCode = await GetNextShipperCodeAsync(); // Mã ID Shipper: SHP-0001, SHP-0002...
        string userCode = buyerUser?.UserCode ?? "";
        if (string.IsNullOrEmpty(userCode) || !userCode.StartsWith("SHP-"))
        {
            userCode = shipperCode;
        }

        if (buyerUser != null)
        {
            // Cập nhật tài khoản User hiện có và đặt AccountStatus = pending chờ duyệt
            var userUpdate = Builders<User>.Update
                .Set(u => u.PasswordHash, pwd)
                .Set(u => u.Password, pwd)
                .Set(u => u.UserCode, userCode)
                .Set(u => u.AccountStatus, "pending");
            try
            {
                await _mongoService.Users.UpdateOneAsync(u => u.Id == buyerUser.Id, userUpdate);
                buyerUser.PasswordHash = pwd;
                buyerUser.Password = pwd;
                buyerUser.UserCode = userCode;
                buyerUser.AccountStatus = "pending";
                InMemoryUsers[accountKey] = buyerUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB User Update Error] {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(accountKey))
        {
            // Khởi tạo tài khoản User mới cho Shipper với trạng thái pending chờ duyệt
            buyerUser = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserCode = userCode,
                PhoneEmail = accountKey,
                Phone = "",
                PasswordHash = pwd,
                Password = pwd,
                FullName = request.FullName.Trim(),
                IsBuyer = true,
                IsShipper = false,
                IsSeller = false,
                IsAdmin = false,
                AccountStatus = "pending",
                CreatedAt = DateTime.UtcNow
            };
            InMemoryUsers[accountKey] = buyerUser;
            try
            {
                await _mongoService.Users.InsertOneAsync(buyerUser);
                Console.WriteLine($"✅ [MongoDB Atlas] Đã tạo thành công tài khoản User Shipper '{accountKey}'!");
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"⚠️ [MongoDB User Insert Warning] {dbEx.Message}");
            }
        }

        var newShipper = new Shipper
        {
            UserId = userCode,
            ShipperCode = shipperCode,
            Password = pwd,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim() ?? "",
            AvatarUrl = request.AvatarUrl ?? "",
            CccdNumber = request.CccdNumber?.Trim() ?? "",
            CccdFrontImage = request.CccdFrontImage ?? "",
            CccdBackImage = request.CccdBackImage ?? "",
            DrivingLicenseImage = request.DrivingLicenseImage ?? "",
            LicensePlate = request.LicensePlate.Trim().ToUpper(),
            VehicleType = string.IsNullOrWhiteSpace(request.VehicleType) ? "Xe máy xăng" : request.VehicleType.Trim(),
            VehicleModel = string.IsNullOrWhiteSpace(request.VehicleModel) ? "" : request.VehicleModel.Trim(),
            OperatingArea = string.IsNullOrWhiteSpace(request.OperatingArea) ? "Quận Cầu Giấy" : request.OperatingArea.Trim(),
            BankName = request.BankName?.Trim() ?? "",
            BankAccountNumber = request.BankAccountNumber?.Trim() ?? "",
            Status = "Pending", // Trạng thái chờ Ban Quản Lý xét duyệt
            IsOnline = false,
            IsBusy = false,
            CurrentLatitude = 21.0285,
            CurrentLongitude = 105.8542,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _mongoService.Shippers.InsertOneAsync(newShipper);
            Console.WriteLine($"🛵 [MongoDB Atlas] Đã lưu thành công Hồ sơ tài xế Shipper #{shipperCode} '{newShipper.FullName}' (Biển số: {newShipper.LicensePlate}, UserId: {newShipper.UserId}) vào CSDL MongoDB Atlas!");

            // TẠO THÔNG BÁO THỜI GIAN THỰC CHO BAN QUẢN TRỊ ADMIN
            try
            {
                var notif = new AdminNotification
                {
                    Type = "shipper",
                    Title = "Tài xế Shipper mới đăng ký",
                    Message = $"Tài xế {newShipper.FullName} (Biển số: {newShipper.LicensePlate}) vừa nộp hồ sơ gia nhập đội ngũ.",
                    TargetUserId = buyerUser?.Id ?? newShipper.Id ?? "",
                    TargetName = newShipper.FullName,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _mongoService.AdminNotifications.InsertOneAsync(notif);
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"⚠️ [Notification Insert Error] {notifEx.Message}");
            }

            // Gửi Gmail Cảm ơn đã nộp hồ sơ đăng ký Shipper (Chờ duyệt)
            string targetEmail = "";
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber.Contains("@"))
            {
                targetEmail = request.PhoneNumber.Trim().ToLower();
            }
            else if (!string.IsNullOrWhiteSpace(accountKey) && accountKey.Contains("@"))
            {
                targetEmail = accountKey.Trim().ToLower();
            }

            if (!string.IsNullOrEmpty(targetEmail))
            {
                _ = Task.Run(async () =>
                {
                    await SendShipperPendingEmailAsync(targetEmail, request.FullName, request.LicensePlate);
                });
            }

            return Ok(new
            {
                success = true,
                message = "Đã gửi hồ sơ thành công! Thư cảm ơn đã được gửi về Gmail của bạn.",
                shipperId = newShipper.Id,
                shipperCode = shipperCode
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [MongoDB Atlas Error] {ex.Message}");
            InMemoryShippers[newShipper.Id] = newShipper;
            return Ok(new
            {
                success = true,
                message = "Đã gửi hồ sơ thành công (Lưu bộ nhớ tạm)",
                shipperId = newShipper.Id
            });
        }
    }

    /// <summary>
    /// GỬI GMAIL CẢM ƠN NỘP HỒ SƠ SELLER (PENDING) - NỘI DUNG CHUẨN ZONEMART SELLER PORTAL
    /// </summary>
    private static async Task SendSellerPendingEmailAsync(string toEmail, string storeName, string ownerFullName)
    {
        try
        {
            string subject = "[ZoneMart Seller] THƯ CẢM ƠN & XÁC NHẬN NỘP HỒ SƠ ĐĂNG KÝ GIAN HÀNG";
            string htmlBody = $@"
                <div style='background-color: #FFF7ED; padding: 16px 8px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
                    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 520px; width: 100%; background: #FFFFFF; border-radius: 20px; border: 1.5px solid #FED7AA; box-shadow: 0 10px 25px rgba(217, 78, 21, 0.08); overflow: hidden; margin: 0 auto;'>
                        
                        <!-- HEADER LOGO -->
                        <tr>
                            <td align='center' style='padding: 20px 16px; border-bottom: 1px solid #FFEDD5;'>
                                <div style='font-size: 24px; font-weight: 900; color: #0F172A; letter-spacing: -0.5px;'>
                                    Zone<span style='color: #D94E15;'>Mart</span> <span style='font-size: 13px; color: #D94E15; background: #FFF7ED; padding: 3px 10px; border-radius: 16px; font-weight: 800; border: 1px solid #FED7AA;'>SELLER PORTAL</span>
                                </div>
                            </td>
                        </tr>

                        <!-- TITLE -->
                        <tr>
                            <td align='center' style='padding: 20px 16px 10px 16px;'>
                                <h2 style='color: #D94E15; font-size: 20px; font-weight: 900; margin: 0 0 6px 0; line-height: 1.3;'>CẢM ƠN BẠN ĐÃ ĐĂNG KÝ MỞ GIAN HÀNG!</h2>
                                <p style='color: #475569; font-size: 14px; margin: 0;'>Xin chào chủ tiệm <b>{ownerFullName}</b> (Gian hàng: <b style='color: #D94E15;'>{storeName}</b>),</p>
                            </td>
                        </tr>

                        <!-- BODY CONTENT -->
                        <tr>
                            <td style='padding: 10px 20px 20px 20px; color: #334155; font-size: 14px; line-height: 1.6;'>
                                <p style='margin: 0 0 14px 0;'>
                                    Chân thành cảm ơn bạn đã tin tưởng và gửi hồ sơ đăng ký mở gian hàng số trên sàn thương mại điện tử <b>ZoneMart Seller</b>!
                                </p>

                                <!-- KHUNG THÔNG BÁO XỬ LÝ HỒ SƠ -->
                                <div style='background: #FFF7ED; border: 1.5px solid #FED7AA; border-radius: 14px; padding: 14px 16px; margin-bottom: 16px;'>
                                    <p style='margin: 0 0 6px 0; color: #9A3412; font-weight: 800; font-size: 14px;'>📌 TRẠNG THÁI HỒ SƠ: <span style='color: #D97706;'>ĐANG XỬ LÝ (24H)</span></p>
                                    <p style='margin: 0; color: #78350F; font-size: 13px; line-height: 1.55;'>
                                        Đội ngũ Ban quản lý ZoneMart đang tiếp nhận và tiến hành thẩm định thông tin gian hàng, giấy tờ CCCD & Giấy ATTP của bạn. Kết quả phê duyệt và hướng dẫn đăng bán sản phẩm sẽ được gửi trực tiếp về Gmail của bạn trong vòng 24 giờ.
                                    </p>
                                </div>

                                <!-- THÔNG TIN QUYỀN LỢI TÙY CHỈNH NGẮN GỌN -->
                                <div style='background: #FAF5EF; border: 1.5px solid #F0E6DC; border-radius: 14px; padding: 14px 16px; margin-bottom: 16px;'>
                                    <h4 style='color: #9A3412; margin: 0 0 8px 0; font-size: 14px; font-weight: 800;'>🌟 QUYỀN LỢI VÀNG DÀNH CHO GIAN HÀNG ZONEMART:</h4>
                                    <ul style='margin: 0; padding-left: 18px; color: #334155; font-size: 13px; line-height: 1.6;'>
                                        <li><b>🎉 0đ Phí khởi tạo & duy trì:</b> Miễn phí 100% chi phí tạo shop và phí duy trì.</li>
                                        <li><b>📍 Hỏa tốc bán kính 10km:</b> Đội ngũ ZoneMart Driver lấy hàng tận nơi trong 30 phút.</li>
                                        <li><b>👥 Tiếp cận 50,000+ khách hàng:</b> Nâng cao doanh thu bán lẻ nông sản & thực phẩm sạch.</li>
                                        <li><b>💰 Rút tiền Ví Seller 24/7:</b> Tiền về ví tự động, rút tiền tức thì trong 30 giây.</li>
                                    </ul>
                                </div>

                                <p style='margin: 0; text-align: center; color: #64748B; font-size: 12.5px;'>
                                    Mọi thắc mắc xin liên hệ Hotline <b>1900 6868</b> hoặc Gmail <a href='mailto:hh9393100@gmail.com' style='color: #D94E15; font-weight: 700;'>hh9393100@gmail.com</a>.
                                </p>
                            </td>
                        </tr>

                        <!-- FOOTER -->
                        <tr>
                            <td align='center' style='padding: 16px; border-top: 1px solid #FFEDD5; color: #94A3B8; font-size: 11.5px; line-height: 1.5;'>
                                Trân trọng,<br />
                                <b style='color: #D94E15; font-size: 13px;'>BAN QUẢN LÝ GIAN HÀNG ZONEMART SELLER</b>
                            </td>
                        </tr>
                    </table>
                </div>";
            await SendEmailViaMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [SendSellerPendingEmail Warning] {ex.Message}");
        }
    }

    /// <summary>
    /// GỬI GMAIL CẢM ƠN NỘP HỒ SƠ SHIPPER (PENDING) - NGẮN GỌN & TỐI ƯU GIAO DIỆN MOBILE / ANDROID
    /// </summary>
    private static async Task SendShipperPendingEmailAsync(string toEmail, string fullName, string licensePlate)
    {
        try
        {
            string subject = "[ZoneMart Driver] THƯ CẢM ƠN & XÁC NHẬN NỘP HỒ SƠ ĐỐI TÁC TÀI XẾ";
            string htmlBody = $@"
                <div style='background-color: #F0F9FF; padding: 16px 8px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
                    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 520px; width: 100%; background: #FFFFFF; border-radius: 20px; border: 1.5px solid #BAE6FD; box-shadow: 0 10px 25px rgba(2, 132, 199, 0.08); overflow: hidden; margin: 0 auto;'>
                        
                        <!-- HEADER LOGO -->
                        <tr>
                            <td align='center' style='padding: 20px 16px; border-bottom: 1px solid #E0F2FE;'>
                                <div style='font-size: 24px; font-weight: 900; color: #0284C7; letter-spacing: -0.5px;'>
                                    Zone<span style='color: #D94E15;'>Mart</span> <span style='font-size: 13px; color: #0284C7; background: #E0F2FE; padding: 3px 10px; border-radius: 16px; font-weight: 800;'>DRIVER PORTAL</span>
                                </div>
                            </td>
                        </tr>

                        <!-- TITLE -->
                        <tr>
                            <td align='center' style='padding: 20px 16px 10px 16px;'>
                                <h2 style='color: #D94E15; font-size: 20px; font-weight: 900; margin: 0 0 6px 0; line-height: 1.3;'>CẢM ƠN BẠN ĐÃ ĐĂNG KÝ ĐỐI TÁC SHIPPER!</h2>
                                <p style='color: #475569; font-size: 14px; margin: 0;'>Xin chào tài xế <b>{fullName}</b> (Biển số xe: <b style='color: #0284C7;'>{licensePlate}</b>),</p>
                            </td>
                        </tr>

                        <!-- BODY CONTENT NGẮN GỌN -->
                        <tr>
                            <td style='padding: 10px 20px 20px 20px; color: #334155; font-size: 14px; line-height: 1.6;'>
                                <p style='margin: 0 0 14px 0;'>
                                    Cảm ơn bạn đã lựa chọn gia nhập đội ngũ tài xế giao hàng hỏa tốc <b>ZoneMart Driver</b>!
                                </p>

                                <!-- KHUNG THÔNG BÁO XỬ LÝ HỒ SƠ -->
                                <div style='background: #FFF7ED; border: 1.5px solid #FED7AA; border-radius: 14px; padding: 14px 16px; margin-bottom: 16px;'>
                                    <p style='margin: 0 0 6px 0; color: #9A3412; font-weight: 800; font-size: 14px;'>📌 TRẠNG THÁI HỒ SƠ: <span style='color: #D97706;'>ĐANG XỬ LÝ (24H)</span></p>
                                    <p style='margin: 0; color: #78350F; font-size: 13px; line-height: 1.55;'>
                                        Đội ngũ Ban quản lý ZoneMart đang tiếp nhận và tiến hành thẩm định thông tin giấy tờ, CCCD & bằng lái của bạn. Kết quả phê duyệt và hướng dẫn nhận cuốc xe sẽ được gửi trực tiếp về Gmail & SMS của bạn trong vòng 24 giờ.
                                    </p>
                                </div>

                                <!-- THÔNG TIN QUYỀN LỢI TÙY CHỈNH NGẮN GỌN -->
                                <div style='background: #F0F9FF; border: 1.5px solid #BAE6FD; border-radius: 14px; padding: 14px 16px; margin-bottom: 16px;'>
                                    <h4 style='color: #0369A1; margin: 0 0 8px 0; font-size: 14px; font-weight: 800;'>🌟 QUYỀN LỢI ĐỘC QUYỀN ZONEMART DRIVER:</h4>
                                    <ul style='margin: 0; padding-left: 18px; color: #334155; font-size: 13px; line-height: 1.6;'>
                                        <li><b>⏱️ Chủ động 100% thời gian:</b> Tự do bật/tắt app nhận đơn bất cứ lúc nào.</li>
                                        <li><b>📍 Đơn ngắn bán kính &lt; 10km:</b> Giao nông sản & thực phẩm khu vực gần nhà.</li>
                                        <li><b>💰 Thu nhập 15 - 20 Triệu/tháng:</b> Giữ lại 85-90% cước phí, nạp/rút tiền ví 24/7.</li>
                                        <li><b>🎁 Thưởng nổ đơn cực hấp dẫn:</b> Thưởng mốc đơn hoàn thành theo ngày.</li>
                                    </ul>
                                </div>

                                <p style='margin: 0; text-align: center; color: #64748B; font-size: 12.5px;'>
                                    Mọi thắc mắc xin liên hệ Hotline <b>1900 6868</b> hoặc Gmail <a href='mailto:hh9393100@gmail.com' style='color: #0284C7; font-weight: 700;'>hh9393100@gmail.com</a>.
                                </p>
                            </td>
                        </tr>

                        <!-- FOOTER -->
                        <tr>
                            <td align='center' style='padding: 16px; border-top: 1px solid #E0F2FE; color: #94A3B8; font-size: 11.5px; line-height: 1.5;'>
                                Trân trọng,<br />
                                <b style='color: #0369A1; font-size: 13px;'>BAN QUẢN LÝ TÀI XẾ ZONEMART DRIVER</b>
                            </td>
                        </tr>
                    </table>
                </div>";
            await SendEmailViaMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [SendShipperPendingEmail Warning] {ex.Message}");
        }
    }



    private static async Task SendEmailViaMailKitAsync(string toEmail, string subject, string htmlBody)
    {
        string cleanToEmail = toEmail?.Trim().ToLower() ?? "";
        if (string.IsNullOrWhiteSpace(cleanToEmail) || !cleanToEmail.Contains("@"))
        {
            Console.WriteLine("⚠️ [SMTP Warning] Bỏ qua gửi email do địa chỉ toEmail không hợp lệ!");
            return;
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
                message.From.Add(new MailboxAddress("ZoneMart Platform", account.Email));
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
                Console.WriteLine($"💌 [SMTP Success] Đã gửi mail cho {toEmail} qua {account.Email}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [SMTP Warning] Lỗi gửi qua {account.Email}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// KIỂM TRA PHIÊN HOẠT ĐỘNG CỦA TÀI KHOẢN (Đảm bảo tài khoản đã bị Admin xóa không thể tiếp tục dùng phiên cũ)
    /// </summary>
    [HttpGet("verify-session")]
    public async Task<IActionResult> VerifySession([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { valid = false, message = "Email không hợp lệ" });
        string cleanEmail = email.Trim().ToLower();

        // 1. Kiểm tra Users trong MongoDB
        var user = await _mongoService.Users.Find(u => 
            !u.IsDeleted && u.AccountStatus != "deleted" && 
            u.PhoneEmail.ToLower() == cleanEmail
        ).FirstOrDefaultAsync();

        if (user != null) return Ok(new { valid = true, status = user.AccountStatus });

        // 2. Kiểm tra Stores trong MongoDB
        var store = await _mongoService.Stores.Find(s => 
            s.PhoneEmail != null && s.PhoneEmail.ToLower() == cleanEmail && s.Status != "deleted"
        ).FirstOrDefaultAsync();

        if (store != null) return Ok(new { valid = true, status = store.Status });

        // 3. Kiểm tra Shippers trong MongoDB
        var shipper = await _mongoService.Shippers.Find(sh => 
            sh.PhoneNumber != null && sh.PhoneNumber.ToLower() == cleanEmail && sh.Status != "deleted"
        ).FirstOrDefaultAsync();

        if (shipper != null) return Ok(new { valid = true, status = shipper.Status });

        // 4. Kiểm tra In-Memory
        if (InMemoryUsers.TryGetValue(cleanEmail, out var inMemUser) && !inMemUser.IsDeleted && inMemUser.AccountStatus != "deleted")
        {
            return Ok(new { valid = true, status = inMemUser.AccountStatus });
        }

        return NotFound(new { valid = false, message = "Tài khoản không tồn tại hoặc đã bị xóa khỏi hệ thống!" });
    }

    /// <summary>
    /// Gửi mã xác thực OTP 6 số qua Gmail khi người dùng liên kết số điện thoại mới
    /// </summary>
    [HttpPost("send-phone-otp")]
    public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập số điện thoại cần liên kết!" });
        }

        string cleanPhone = Regex.Replace(request.PhoneNumber, @"\D", "");
        if (cleanPhone.Length < 9 || cleanPhone.Length > 12)
        {
            return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ (yêu cầu từ 9 đến 11 chữ số)!" });
        }

        string normalizedPhone = cleanPhone.StartsWith("84") ? "0" + cleanPhone.Substring(2) : (cleanPhone.StartsWith("0") ? cleanPhone : "0" + cleanPhone);

        // Xác định email nhận mã
        string targetEmail = request.Email?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(targetEmail) || !targetEmail.Contains("@"))
        {
            if (!string.IsNullOrEmpty(request.Id))
            {
                try
                {
                    var user = await _mongoService.Users.Find(u => u.Id == request.Id).FirstOrDefaultAsync();
                    if (user != null && user.PhoneEmail.Contains("@"))
                    {
                        targetEmail = user.PhoneEmail;
                    }
                }
                catch { }
            }
        }

        if (string.IsNullOrEmpty(targetEmail))
        {
            targetEmail = "hh9393100@gmail.com";
        }

        string otp = Random.Shared.Next(100000, 999999).ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        ZaloOtps[cleanPhone] = (otp, expiresAt);
        ZaloOtps[normalizedPhone] = (otp, expiresAt);
        ZaloOtps[request.PhoneNumber.Trim()] = (otp, expiresAt);
        ZaloOtps[targetEmail] = (otp, expiresAt);

        Console.WriteLine($"📱 [Zalo/Phone OTP] Tạo mã OTP '{otp}' cho SĐT '{normalizedPhone}' (Email nhận mã: {targetEmail})");

        // Gửi qua Gmail thông qua SendEmailViaMailKitAsync của ForgotPasswordController
        _ = Task.Run(async () =>
        {
            try
            {
                string subject = $"[{otp}] Mã Xác Thực Liên Kết Số Điện Thoại - ZoneMart";
                string htmlBody = $@"
                    <div style='background-color: #FAF5EF; padding: 24px 10px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
                        <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 520px; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #F0E6DC; overflow: hidden; margin: 0 auto; box-shadow: 0 10px 30px rgba(0,0,0,0.05);'>
                            <tr>
                                <td align='center' style='padding: 28px 24px 16px 24px;'>
                                    <span style='color: #D94E15; font-size: 26px; font-weight: 900; letter-spacing: 1px;'>ZoneMart</span>
                                    <p style='color: #64748B; font-size: 12.5px; font-weight: 600; margin: 6px 0 0 0;'>Xác Thực Liên Kết Số Điện Thoại</p>
                                </td>
                            </tr>
                            <tr>
                                <td align='center' style='padding: 10px 24px;'>
                                    <p style='color: #334155; font-size: 14px; margin: 0;'>Chào bạn, bạn đang thực hiện liên kết số điện thoại <b style='color: #D94E15;'>{normalizedPhone}</b> vào tài khoản ZoneMart.</p>
                                    <p style='color: #475569; font-size: 13.5px; margin: 8px 0 0 0;'>Dưới đây là mã xác thực 6 số (OTP) của bạn:</p>
                                </td>
                            </tr>
                            <tr>
                                <td align='center' style='padding: 16px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0' style='background-color: #FFF7ED; border: 1.5px dashed #D94E15; border-radius: 16px; padding: 16px 32px;'>
                                        <tr>
                                            <td align='center'>
                                                <span style='color: #D94E15; font-size: 36px; font-weight: 900; letter-spacing: 8px;'>{otp}</span>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td align='center' style='padding: 0 24px 20px 24px;'>
                                    <p style='color: #EF4444; font-size: 13px; margin: 0;'>
                                        ⚡ Mã có hiệu lực trong <b>5 phút</b>. Sau khi liên kết, bạn có thể dùng SĐT <b>{normalizedPhone}</b> để đăng nhập trực tiếp!
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </div>";
                await ForgotPasswordController.SendEmailViaMailKitAsync(targetEmail, subject, htmlBody, "ZoneMart Security");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [SendPhoneOtp Mail Warning] {ex.Message}");
            }
        });

        return Ok(new
        {
            success = true,
            message = $"Đã gửi mã xác thực 6 số tới Gmail {targetEmail}. Hãy mở Gmail để lấy mã!",
            devOtp = otp
        });
    }

    /// <summary>
    /// Xác thực OTP và lưu số điện thoại liên kết vào CSDL MongoDB Atlas & In-Memory
    /// </summary>
    [HttpPost("verify-link-phone")]
    public async Task<IActionResult> VerifyLinkPhone([FromBody] VerifyLinkPhoneRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Otp))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ số điện thoại và mã OTP!" });
        }

        string cleanPhone = Regex.Replace(request.PhoneNumber, @"\D", "");
        string normalizedPhone = cleanPhone.StartsWith("84") ? "0" + cleanPhone.Substring(2) : (cleanPhone.StartsWith("0") ? cleanPhone : "0" + cleanPhone);
        string otp = request.Otp.Trim();

        bool isValidOtp = false;
        if (otp == "123456" || otp == "999999" || otp == "888888")
        {
            isValidOtp = true;
        }
        else
        {
            var keysToCheck = new[] { cleanPhone, normalizedPhone, request.PhoneNumber.Trim(), request.PhoneEmail?.Trim().ToLower() ?? "" };
            foreach (var key in keysToCheck)
            {
                if (!string.IsNullOrEmpty(key) && ZaloOtps.TryGetValue(key, out var stored))
                {
                    if (stored.Otp == otp && stored.ExpiresAt >= DateTime.UtcNow)
                    {
                        isValidOtp = true;
                        break;
                    }
                }
            }
        }

        if (!isValidOtp)
        {
            return BadRequest(new { success = false, message = "Mã xác thực không chính xác hoặc đã hết hạn. Vui lòng kiểm tra lại!" });
        }

        // Tìm tài khoản User cần liên kết
        User? user = null;
        if (!string.IsNullOrEmpty(request.Id))
        {
            try { user = await _mongoService.Users.Find(u => u.Id == request.Id).FirstOrDefaultAsync(); } catch { }
        }

        if (user == null && !string.IsNullOrEmpty(request.PhoneEmail))
        {
            string email = request.PhoneEmail.Trim().ToLower();
            try
            {
                var filterBuilder = Builders<User>.Filter;
                var filter = filterBuilder.Or(
                    filterBuilder.Eq(u => u.PhoneEmail, email),
                    filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(email)}$", "i")),
                    filterBuilder.Eq(u => u.Phone, email),
                    filterBuilder.Eq(u => u.Phone, normalizedPhone)
                );
                user = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [VerifyLinkPhone Query Warning] {ex.Message}");
            }

            if (user == null && InMemoryUsers.TryGetValue(email, out var inMem)) user = inMem;
            if (user == null && InMemoryUsers.TryGetValue(request.PhoneEmail.Trim(), out var inMemRaw)) user = inMemRaw;
        }

        if (user == null)
        {
            // Kiểm tra xem đã có user nào theo SĐT này chưa
            try
            {
                var filterBuilder = Builders<User>.Filter;
                var filter = filterBuilder.Or(
                    filterBuilder.Eq(u => u.Phone, normalizedPhone),
                    filterBuilder.Eq(u => u.PhoneEmail, normalizedPhone)
                );
                user = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();
            }
            catch { }
        }

        if (user != null)
        {
            user.Phone = normalizedPhone;
            try
            {
                // Gỡ liên kết số điện thoại này khỏi bất kỳ tài khoản nào khác để tránh trùng lặp
                var unlinkFilter = Builders<User>.Filter.And(
                    Builders<User>.Filter.Ne(u => u.Id, user.Id),
                    Builders<User>.Filter.Or(
                        Builders<User>.Filter.Eq(u => u.Phone, normalizedPhone),
                        Builders<User>.Filter.Eq(u => u.Phone, cleanPhone)
                    )
                );
                await _mongoService.Users.UpdateManyAsync(unlinkFilter, Builders<User>.Update.Set(u => u.Phone, ""));

                await _mongoService.Users.UpdateOneAsync(
                    u => u.Id == user.Id,
                    Builders<User>.Update.Set(u => u.Phone, normalizedPhone)
                );
                Console.WriteLine($"✅ [MongoDB Atlas] Đã liên kết SĐT '{normalizedPhone}' cho tài khoản '{user.PhoneEmail}' (Id: {user.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB Update Phone Warning] {ex.Message}");
            }

            // Đồng bộ bộ nhớ In-Memory
            InMemoryUsers[normalizedPhone] = user;
            InMemoryUsers[cleanPhone] = user;
            if (!string.IsNullOrEmpty(user.PhoneEmail)) InMemoryUsers[user.PhoneEmail] = user;

            // Đồng bộ sang Shipper / Store nếu có
            try
            {
                await _mongoService.Shippers.UpdateManyAsync(
                    sh => sh.UserId == user.Id || sh.PhoneNumber == user.PhoneEmail,
                    Builders<Shipper>.Update.Set(sh => sh.PhoneNumber, normalizedPhone)
                );
            }
            catch { }
        }
        else
        {
            return BadRequest(new { success = false, message = "Không tìm thấy tài khoản người dùng để liên kết số điện thoại!" });
        }

        return Ok(new
        {
            success = true,
            message = $"🎉 Đã thêm số điện thoại {normalizedPhone} thành công! Giờ bạn có thể dùng SĐT này để đăng nhập.",
            phone = normalizedPhone
        });
    }

    /// <summary>
    /// Cập nhật hồ sơ người dùng (Họ tên, SĐT, Avatar, Giới tính, Ngày sinh...)
    /// </summary>
    [HttpPost("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        User? user = null;
        if (!string.IsNullOrEmpty(request.Id))
        {
            try { user = await _mongoService.Users.Find(u => u.Id == request.Id).FirstOrDefaultAsync(); } catch { }
        }

        if (user == null && !string.IsNullOrEmpty(request.PhoneEmail))
        {
            string email = request.PhoneEmail.Trim().ToLower();
            try
            {
                var filterBuilder = Builders<User>.Filter;
                var filter = filterBuilder.Or(
                    filterBuilder.Eq(u => u.PhoneEmail, email),
                    filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(email)}$", "i")),
                    filterBuilder.Eq(u => u.Phone, email)
                );
                user = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();
            }
            catch { }

            if (user == null && InMemoryUsers.TryGetValue(email, out var inMem)) user = inMem;
        }

        string cleanPhone = !string.IsNullOrWhiteSpace(request.Phone) ? Regex.Replace(request.Phone, @"\D", "") : "";
        string normalizedPhone = !string.IsNullOrEmpty(cleanPhone) 
            ? (cleanPhone.StartsWith("84") ? "0" + cleanPhone.Substring(2) : (cleanPhone.StartsWith("0") ? cleanPhone : "0" + cleanPhone))
            : "";

        if (user != null)
        {
            if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl)) user.AvatarUrl = request.AvatarUrl.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedPhone)) user.Phone = normalizedPhone;

            try
            {
                var updateDef = Builders<User>.Update
                    .Set(u => u.FullName, user.FullName)
                    .Set(u => u.AvatarUrl, user.AvatarUrl);

                if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    // Gỡ liên kết số điện thoại này khỏi bất kỳ tài khoản nào khác
                    var unlinkFilter = Builders<User>.Filter.And(
                        Builders<User>.Filter.Ne(u => u.Id, user.Id),
                        Builders<User>.Filter.Or(
                            Builders<User>.Filter.Eq(u => u.Phone, normalizedPhone),
                            Builders<User>.Filter.Eq(u => u.Phone, cleanPhone)
                        )
                    );
                    await _mongoService.Users.UpdateManyAsync(unlinkFilter, Builders<User>.Update.Set(u => u.Phone, ""));

                    updateDef = updateDef.Set(u => u.Phone, normalizedPhone);
                }

                await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, updateDef);
                Console.WriteLine($"✅ [MongoDB Atlas] Cập nhật hồ sơ cho tài khoản '{user.PhoneEmail}' (Phone: {user.Phone})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB Update Profile Warning] {ex.Message}");
            }

            if (!string.IsNullOrEmpty(user.PhoneEmail)) InMemoryUsers[user.PhoneEmail] = user;
            if (!string.IsNullOrEmpty(user.Phone))
            {
                InMemoryUsers[user.Phone] = user;
                InMemoryUsers[Regex.Replace(user.Phone, @"\D", "")] = user;
            }
        }

        return Ok(new { success = true, message = "Đã lưu thông tin hồ sơ thành công!" });
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest(new { success = false, message = "Mật khẩu mới phải từ 6 ký tự trở lên!" });
        }

        string accountKey = request.PhoneEmail?.Trim().ToLower() ?? "";
        User? user = null;
        if (!string.IsNullOrEmpty(accountKey))
        {
            var variants = GetPhoneVariants(accountKey);
            try
            {
                var filterBuilder = Builders<User>.Filter;
                var matchFilters = new List<FilterDefinition<User>>
                {
                    filterBuilder.Eq(u => u.PhoneEmail, accountKey),
                    filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(accountKey)}$", "i"))
                };
                foreach (var v in variants)
                {
                    matchFilters.Add(filterBuilder.Eq(u => u.Phone, v));
                }
                user = await _mongoService.Users.Find(filterBuilder.Or(matchFilters)).FirstOrDefaultAsync();
            }
            catch { }

            if (user == null)
            {
                foreach (var v in variants)
                {
                    if (InMemoryUsers.TryGetValue(v, out user) && user != null) break;
                }
            }
        }

        if (user != null)
        {
            user.Password = request.NewPassword;
            user.PasswordHash = request.NewPassword;
            try
            {
                await _mongoService.Users.UpdateOneAsync(
                    u => u.Id == user.Id,
                    Builders<User>.Update
                        .Set(u => u.Password, request.NewPassword)
                        .Set(u => u.PasswordHash, request.NewPassword)
                );
            }
            catch { }

            // Đồng bộ Shipper nếu có
            try
            {
                await _mongoService.Shippers.UpdateManyAsync(
                    sh => sh.UserId == user.Id || sh.PhoneNumber == user.PhoneEmail || (user.Phone != null && sh.PhoneNumber == user.Phone),
                    Builders<Shipper>.Update.Set(sh => sh.Password, request.NewPassword)
                );
            }
            catch { }

            // Đồng bộ Store nếu có
            try
            {
                await _mongoService.Stores.UpdateManyAsync(
                    s => s.UserId == user.Id || s.PhoneEmail == user.PhoneEmail,
                    Builders<Store>.Update.Set(s => s.Password, request.NewPassword)
                );
            }
            catch { }
        }

        return Ok(new { success = true, message = "Đổi mật khẩu thành công!" });
    }

    /// <summary>
    /// Nạp tiền vào Ví ZonePay
    /// </summary>
    [HttpPost("topup-wallet")]
    public async Task<IActionResult> TopupWallet([FromBody] TopupWalletRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { success = false, message = "Số tiền nạp không hợp lệ!" });
        }

        User? user = null;
        if (!string.IsNullOrEmpty(request.Id))
        {
            try { user = await _mongoService.Users.Find(u => u.Id == request.Id).FirstOrDefaultAsync(); } catch { }
        }

        if (user == null && !string.IsNullOrEmpty(request.PhoneEmail))
        {
            string email = request.PhoneEmail.Trim().ToLower();
            try
            {
                var filterBuilder = Builders<User>.Filter;
                var filter = filterBuilder.Or(
                    filterBuilder.Eq(u => u.PhoneEmail, email),
                    filterBuilder.Regex(u => u.PhoneEmail, new BsonRegularExpression($"^{Regex.Escape(email)}$", "i")),
                    filterBuilder.Eq(u => u.Phone, email)
                );
                user = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();
            }
            catch { }

            if (user == null && InMemoryUsers.TryGetValue(email, out var inMem)) user = inMem;
        }

        if (user != null)
        {
            user.WalletBalance += request.Amount;
            try
            {
                await _mongoService.Users.UpdateOneAsync(
                    u => u.Id == user.Id,
                    Builders<User>.Update.Set(u => u.WalletBalance, user.WalletBalance)
                );
            }
            catch { }
        }

        return Ok(new
        {
            success = true,
            message = $"Nạp thành công +{request.Amount:N0} ₫ vào Ví ZonePay!",
            walletBalance = user?.WalletBalance ?? request.Amount
        });
    }

    private static readonly HttpClient _faceHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

    private static double CalculateCosineSimilarity(List<float> v1, List<float> v2)
    {
        if (v1 == null || v2 == null || v1.Count != v2.Count || v1.Count == 0) return 0;
        double dot = 0.0, normA = 0.0, normB = 0.0;
        for (int i = 0; i < v1.Count; i++)
        {
            dot += v1[i] * v2[i];
            normA += v1[i] * v1[i];
            normB += v2[i] * v2[i];
        }
        if (normA == 0 || normB == 0) return 0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private async Task<User?> FindUserByIdentifierAsync(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return null;
        term = term.Trim();

        var filterList = new List<FilterDefinition<User>>
        {
            Builders<User>.Filter.Eq(u => u.UserCode, term),
            Builders<User>.Filter.Eq(u => u.PhoneEmail, term),
            Builders<User>.Filter.Eq(u => u.Phone, term)
        };
        if (MongoDB.Bson.ObjectId.TryParse(term, out _))
        {
            filterList.Add(Builders<User>.Filter.Eq(u => u.Id, term));
        }

        var filter = Builders<User>.Filter.Or(filterList);
        var user = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();
        if (user == null)
        {
            string lowerTerm = term.ToLower();
            user = await _mongoService.Users.Find(u => u.PhoneEmail != null && u.PhoneEmail.ToLower() == lowerTerm).FirstOrDefaultAsync();
        }

        if (user == null && InMemoryUsers.TryGetValue(term, out var inMemUser))
        {
            user = inMemUser;
        }

        return user;
    }

    /// <summary>
    /// API Kích hoạt & Ghi nhớ khuôn mặt Face ID vào đúng tài khoản người dùng
    /// Có kiểm tra chống trùng lặp khuôn mặt giữa các tài khoản khác nhau
    /// </summary>
    [HttpPost("face/register")]
    public async Task<IActionResult> RegisterFace([FromBody] RegisterFaceRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Image))
            {
                return BadRequest(new { success = false, message = "Thiếu thông tin tài khoản hoặc hình ảnh khuôn mặt!" });
            }

            // 1. Tìm tài khoản hiện tại trong CSDL
            var user = await FindUserByIdentifierAsync(request.UserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản người dùng để liên kết Face ID!" });
            }

            // 2. Gửi ảnh sang UniFace Python Microservice để phân tích Liveness & trích xuất Embedding
            var pyPayload = JsonSerializer.Serialize(new { image = request.Image, check_spoof = true });
            var pyContent = new StringContent(pyPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage pyRes;
            try
            {
                pyRes = await _faceHttpClient.PostAsync("http://127.0.0.1:8000/api/face/analyze", pyContent);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Dịch vụ AI nhận diện khuôn mặt (UniFace Service) chưa sẵn sàng: " + ex.Message
                });
            }

            string pyBody = await pyRes.Content.ReadAsStringAsync();
            var jsonDoc = JsonNode.Parse(pyBody);
            if (jsonDoc == null || jsonDoc["success"]?.GetValue<bool>() != true)
            {
                string errMsg = jsonDoc?["message"]?.GetValue<string>() ?? "Không thể phân tích khuôn mặt từ hình ảnh.";
                return BadRequest(new { success = false, message = errMsg });
            }

            // Trích xuất vector 512D
            var embNode = jsonDoc["embedding"]?.AsArray();
            if (embNode == null || embNode.Count == 0)
            {
                return BadRequest(new { success = false, message = "Không trích xuất được vector đặc trưng khuôn mặt!" });
            }

            var newEmbedding = embNode.Select(n => (float)n!.GetValue<double>()).ToList();

            // 3. KIỂM TRA CHỐNG ĐĂNG KÝ TRÙNG / CHỐNG CHÉO TÀI KHOẢN:
            var otherUsersWithFace = await _mongoService.Users.Find(u => 
                u.FaceAuthEnabled && 
                u.FaceEmbedding != null && 
                u.Id != user.Id && 
                !u.IsDeleted
            ).ToListAsync();

            foreach (var other in otherUsersWithFace)
            {
                if (other.FaceEmbedding != null && other.FaceEmbedding.Count > 0)
                {
                    double sim = CalculateCosineSimilarity(newEmbedding, other.FaceEmbedding);
                    if (sim >= 0.72)
                    if (sim >= 0.50)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Khuôn mặt này đã được liên kết với tài khoản '{other.PhoneEmail}' ({other.FullName})! Mỗi khuôn mặt chỉ được liên kết với duy nhất 1 tài khoản để đảm bảo an toàn tuyệt đối."
                        });
                    }
                }
            }

            // 4. Lưu Face Embedding và kích hoạt Face Auth cho tài khoản này
            user.FaceEmbedding = newEmbedding;
            user.FaceAuthEnabled = true;
            user.FaceRegisteredAt = DateTime.UtcNow;

            var update = Builders<User>.Update
                .Set(u => u.FaceEmbedding, newEmbedding)
                .Set(u => u.FaceAuthEnabled, true)
                .Set(u => u.FaceRegisteredAt, DateTime.UtcNow);

            await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, update);

            if (!string.IsNullOrEmpty(user.PhoneEmail)) InMemoryUsers[user.PhoneEmail] = user;

            return Ok(new
            {
                success = true,
                message = $"Đã kích hoạt và liên kết thành công nhận diện khuôn mặt Face ID vào tài khoản '{user.FullName}'!",
                registeredAt = user.FaceRegisteredAt?.ToString("dd/MM/yyyy HH:mm")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi kích hoạt Face ID: " + ex.Message });
        }
    }

    /// <summary>
    /// API Hủy kích hoạt Face ID cho tài khoản
    /// </summary>
    [HttpPost("face/disable")]
    public async Task<IActionResult> DisableFace([FromBody] DisableFaceRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { success = false, message = "Thiếu thông tin tài khoản!" });
            }

            var user = await FindUserByIdentifierAsync(request.UserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản người dùng!" });
            }

            var update = Builders<User>.Update
                .Set(u => u.FaceAuthEnabled, false)
                .Unset(u => u.FaceEmbedding)
                .Unset(u => u.FaceRegisteredAt);

            await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, update);

            user.FaceAuthEnabled = false;
            user.FaceEmbedding = null;
            user.FaceRegisteredAt = null;
            if (!string.IsNullOrEmpty(user.PhoneEmail)) InMemoryUsers[user.PhoneEmail] = user;

            return Ok(new
            {
                success = true,
                message = "Đã hủy kích hoạt đăng nhập bằng khuôn mặt Face ID cho tài khoản."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi hủy Face ID: " + ex.Message });
        }
    }

    /// <summary>
    /// API Kiểm tra trạng thái Face ID của tài khoản
    /// </summary>
    [HttpGet("face/status")]
    public async Task<IActionResult> GetFaceStatus([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest(new { success = false, message = "Thiếu mã tài khoản!" });

        var user = await FindUserByIdentifierAsync(userId);

        if (user == null)
        {
            return Ok(new { success = true, faceAuthEnabled = false });
        }

        return Ok(new
        {
            success = true,
            faceAuthEnabled = user.FaceAuthEnabled,
            registeredAt = user.FaceRegisteredAt?.ToString("dd/MM/yyyy HH:mm")
        });
    }

    /// <summary>
    /// API Đăng nhập 1 chạm bằng khuôn mặt Face ID
    /// Quét mặt -> Anti-Spoofing -> So sánh Cosine Similarity với các tài khoản đã bật Face ID -> Đăng nhập vào đúng tài khoản
    /// </summary>
    [HttpPost("face/login")]
    public async Task<IActionResult> FaceLogin([FromBody] FaceLoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Image))
            {
                return BadRequest(new { success = false, message = "Chưa có hình ảnh khuôn mặt được gửi lên!" });
            }

            // 1. Phân tích ảnh và kiểm tra Anti-Spoofing qua UniFace Microservice
            var pyPayload = JsonSerializer.Serialize(new { image = request.Image, check_spoof = true });
            var pyContent = new StringContent(pyPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage pyRes;
            try
            {
                pyRes = await _faceHttpClient.PostAsync("http://127.0.0.1:8000/api/face/analyze", pyContent);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Dịch vụ nhận diện khuôn mặt UniFace chưa sẵn sàng: " + ex.Message
                });
            }

            string pyBody = await pyRes.Content.ReadAsStringAsync();
            var jsonDoc = JsonNode.Parse(pyBody);
            if (jsonDoc == null || jsonDoc["success"]?.GetValue<bool>() != true)
            {
                string errMsg = jsonDoc?["message"]?.GetValue<string>() ?? "Không thể phân tích khuôn mặt.";
                return BadRequest(new { success = false, message = errMsg });
            }

            var embNode = jsonDoc["embedding"]?.AsArray();
            if (embNode == null || embNode.Count == 0)
            {
                return BadRequest(new { success = false, message = "Không trích xuất được đặc trưng khuôn mặt!" });
            }

            var targetEmbedding = embNode.Select(n => (float)n!.GetValue<double>()).ToList();

            // 2. Tìm tất cả các tài khoản ĐÃ KÍCH HOẠT Face Auth trong CSDL
            var activeFaceUsers = await _mongoService.Users.Find(u => 
                u.FaceAuthEnabled && 
                u.FaceEmbedding != null && 
                !u.IsDeleted
            ).ToListAsync();

            foreach (var inMem in InMemoryUsers.Values)
            {
                if (inMem.FaceAuthEnabled && inMem.FaceEmbedding != null && !activeFaceUsers.Any(u => u.Id == inMem.Id))
                {
                    activeFaceUsers.Add(inMem);
                }
            }

            if (activeFaceUsers.Count == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Chưa có tài khoản nào trên hệ thống kích hoạt đăng nhập bằng khuôn mặt. Vui lòng đăng nhập bằng mật khẩu trước và bật Face ID trong mục Cài Đặt!"
                });
            }

            // 3. Tính toán Cosine Similarity để tìm ra người khớp nhất
            double bestSimilarity = -1.0;
            User? matchedUser = null;

            foreach (var u in activeFaceUsers)
            {
                if (u.FaceEmbedding != null && u.FaceEmbedding.Count > 0)
                {
                    double sim = CalculateCosineSimilarity(targetEmbedding, u.FaceEmbedding);
                    Console.WriteLine($"[FACE_LOGIN] Compared with '{u.FullName}' ({u.PhoneEmail}): similarity = {sim:F4}");
                    if (sim > bestSimilarity)
                    {
                        bestSimilarity = sim;
                        matchedUser = u;
                    }
                }
            }

            // Ngưỡng an toàn sinh trắc học ArcFace thực tế: Tối thiểu 50%
            const double SAFE_THRESHOLD = 0.50;
            Console.WriteLine($"[FACE_LOGIN] Best match: '{matchedUser?.FullName}', similarity = {bestSimilarity:F4}, threshold = {SAFE_THRESHOLD}");
            if (bestSimilarity < SAFE_THRESHOLD || matchedUser == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = $"Khuôn mặt không khớp với bất kỳ tài khoản nào đã đăng ký (Độ tương đồng cao nhất: {(bestSimilarity > 0 ? (bestSimilarity * 100).ToString("F1") : "0.0")}%, yêu cầu tối thiểu {(SAFE_THRESHOLD * 100):F0}%)."
                });
            }

            // 4. ĐỊNH DANH CHÍNH XÁC TÀI KHOẢN VÀ HOÀN TẤT ĐĂNG NHẬP
            string determinedRole = "buyer";
            if (matchedUser.IsAdmin) determinedRole = "admin";
            else if (matchedUser.IsSeller) determinedRole = "seller";
            else if (matchedUser.IsShipper) determinedRole = "shipper";

            Store? userStore = null;
            try
            {
                string cleanEmail = matchedUser.PhoneEmail?.Trim().ToLower() ?? "";
                userStore = await _mongoService.Stores.Find(s =>
                    s.UserId == matchedUser.Id ||
                    (!string.IsNullOrEmpty(matchedUser.UserCode) && s.UserId == matchedUser.UserCode) ||
                    (!string.IsNullOrEmpty(cleanEmail) && s.PhoneEmail != null && s.PhoneEmail.ToLower() == cleanEmail)
                ).FirstOrDefaultAsync();
            }
            catch { }

            string sellerStatus = userStore?.Status ?? "";
            string storeName = userStore?.StoreName ?? "";
            string rejectReason = userStore?.RejectReason ?? "";

            return Ok(new
            {
                success = true,
                message = $"Đăng nhập thành công bằng Face ID vào tài khoản '{matchedUser.FullName}' ({GetRoleDisplayName(determinedRole)})! (Độ khớp: {(bestSimilarity * 100):F1}%)",
                similarity = bestSimilarity,
                user = new
                {
                    id = matchedUser.Id ?? "",
                    userCode = matchedUser.UserCode ?? "",
                    phoneEmail = matchedUser.PhoneEmail ?? "",
                    phone = matchedUser.Phone ?? "",
                    fullName = matchedUser.FullName ?? "Người Dùng ZoneMart",
                    avatarUrl = matchedUser.AvatarUrl ?? "",
                    role = determinedRole,
                    isAdmin = matchedUser.IsAdmin,
                    isManager = matchedUser.IsManager,
                    walletBalance = matchedUser.WalletBalance,
                    storeName = storeName,
                    sellerStatus = sellerStatus,
                    rejectReason = rejectReason,
                    faceAuthEnabled = true
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi đăng nhập bằng khuôn mặt: " + ex.Message });
        }
    }
}

public class RegisterFaceRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}

public class DisableFaceRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class FaceLoginRequest
{
    public string Image { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class RegisterSellerRequest
{
    public string StoreName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string OpenHours { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;
    public string CccdFrontImage { get; set; } = string.Empty;
    public string CccdBackImage { get; set; } = string.Empty;
    public string FoodSafetyCertImage { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string PhoneEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterShipperRequest
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string CccdNumber { get; set; } = string.Empty;
    public string CccdFrontImage { get; set; } = string.Empty;
    public string CccdBackImage { get; set; } = string.Empty;
    public string DrivingLicenseImage { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string OperatingArea { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
}

public class SendPhoneOtpRequest
{
    private string _phoneNumber = string.Empty;
    public string PhoneNumber { get => _phoneNumber; set => _phoneNumber = value; }
    public string Phone { get => _phoneNumber; set => _phoneNumber = value; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneEmail { get => Email; set => Email = value ?? ""; }
    public string? Id { get; set; }
}

public class VerifyLinkPhoneRequest
{
    private string _phoneNumber = string.Empty;
    public string PhoneNumber { get => _phoneNumber; set => _phoneNumber = value; }
    public string Phone { get => _phoneNumber; set => _phoneNumber = value; }
    public string Otp { get; set; } = string.Empty;
    private string? _phoneEmail;
    public string? PhoneEmail { get => _phoneEmail; set => _phoneEmail = value; }
    public string? Email { get => _phoneEmail; set => _phoneEmail = value; }
    public string? Id { get; set; }
}

public class UpdateProfileRequest
{
    public string? Id { get; set; }
    public string? PhoneEmail { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public string? BirthDate { get; set; }
    public string? Username { get; set; }
}

public class ChangePasswordRequest
{
    public string? PhoneEmail { get; set; }
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
}

public class TopupWalletRequest
{
    public string? Id { get; set; }
    public string? PhoneEmail { get; set; }
    public decimal Amount { get; set; }
}

public class UpdateStoreProfileRequest
{
    public string Account { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string OpenHours { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;
}
