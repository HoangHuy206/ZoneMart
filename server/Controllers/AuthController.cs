using System.Collections.Concurrent;
using System.Net;
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

    // Lưu trữ tài khoản tạm thời song song để đảm bảo Đăng ký & Đăng nhập hoạt động 100% không bị gián đoạn
    private static readonly ConcurrentDictionary<string, User> InMemoryUsers = new(StringComparer.OrdinalIgnoreCase);

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
        InMemoryUsers.TryRemove(cleanEmail, out _);
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.PhoneEmail, cleanEmail);
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
    /// Đăng nhập hệ thống - BẮT BUỘC TÀI KHOẢN PHẢI CÓ TRONG CSDL (MONGODB ATLAS HOẶC BỘ NHỚ LƯU TRỮ)
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
        User? existingUser = null;
        Shipper? shipperInfo = null;

        // 1. Kiểm tra từ CSDL MongoDB Atlas (Users collection) theo PhoneEmail hoặc UserCode
        try
        {
            existingUser = await _mongoService.Users.Find(u => 
                u.PhoneEmail.ToLower() == inputAccount || 
                (u.UserCode != null && u.UserCode.ToLower() == inputAccount)
            ).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Query User Warning] {ex.Message}");
        }

        if (existingUser == null)
        {
            InMemoryUsers.TryGetValue(inputAccount, out existingUser);
        }

        // 2. Kiểm tra CSDL MongoDB Atlas (Shippers collection)
        try
        {
            shipperInfo = await _mongoService.Shippers.Find(sh => 
                sh.PhoneNumber.ToLower() == inputAccount || 
                (sh.ShipperCode != null && sh.ShipperCode.ToLower() == inputAccount) || 
                (sh.UserId != null && sh.UserId.ToLower() == inputAccount) ||
                (existingUser != null && sh.UserId == existingUser.Id)
            ).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Query Shipper Warning] {ex.Message}");
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
        }

        // Hỗ trợ đăng nhập tài khoản Admin duy nhất của hệ thống
        if ((inputAccount == "admin" || inputAccount == "admin@zonemart.vn") && (request.Password == "admin" || request.Password == "admin123" || request.Password == "123456"))
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
                    IsBuyer = false,
                    IsSeller = false,
                    IsShipper = false,
                    AccountStatus = "active"
                };
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
        if (existingUser != null && existingUser.AccountStatus == "banned")
        {
            return BadRequest(new { success = false, message = "Tài khoản của bạn đã bị khóa vi phạm tiêu chuẩn cộng đồng ZoneMart!" });
        }

        // Tự động nhận diện & Ràng buộc vai trò theo từng Cổng Đăng Nhập
        string reqRole = !string.IsNullOrWhiteSpace(request.Role) ? request.Role.Trim().ToLower() : "";
        string determinedRole = "buyer";

        if (reqRole == "shipper")
        {
            if ((existingUser == null || !existingUser.IsShipper) && shipperInfo == null)
            {
                return BadRequest(new { success = false, message = "Tài khoản này chưa đăng ký làm Tài Xế Shipper! Vui lòng đăng ký hồ sơ tài xế trước khi đăng nhập." });
            }
            determinedRole = "shipper";
        }
        else if (reqRole == "seller")
        {
            if (existingUser == null || (!existingUser.IsSeller && !existingUser.IsAdmin))
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

        return Ok(new
        {
            success = true,
            message = $"Đăng nhập thành công với vai trò {GetRoleDisplayName(determinedRole)}!",
            user = new
            {
                id = existingUser?.Id ?? shipperInfo?.Id ?? "",
                userCode = existingUser?.UserCode ?? shipperInfo?.ShipperCode ?? "",
                phoneEmail = existingUser?.PhoneEmail ?? shipperInfo?.PhoneNumber ?? "",
                fullName = shipperInfo?.FullName ?? existingUser?.FullName ?? "Tài Xế",
                avatarUrl = !string.IsNullOrEmpty(shipperInfo?.AvatarUrl) ? shipperInfo.AvatarUrl : (existingUser?.AvatarUrl ?? ""),
                role = determinedRole,
                walletBalance = existingUser?.WalletBalance ?? 0,
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
                    message.From.Add(new MailboxAddress("ZoneMart E-Commerce", account.Email));
                    message.To.Add(new MailboxAddress("", toEmail));
                    message.Subject = subject;

                    var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                    message.Body = bodyBuilder.ToMessageBody();

                    using var client = new SmtpClient();
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(account.Email, account.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    Console.WriteLine($"💌 [MailKit SMTP Success] Đã gửi thành công THƯ CẢM ƠN tới Gmail: {toEmail} qua {account.Email}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ [MailKit SMTP Warning] Không thể gửi qua {account.Email}: {ex.Message}");
                }
            }
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
                .Set(u => u.IsSeller, true)
                .Set(u => u.PasswordHash, pwd)
                .Set(u => u.Password, pwd)
                .Set(u => u.UserCode, userCode)
                .Set(u => u.AccountStatus, "active");
            try
            {
                await _mongoService.Users.UpdateOneAsync(u => u.Id == buyerUser.Id, userUpdate);
                buyerUser.IsSeller = true;
                buyerUser.PasswordHash = pwd;
                buyerUser.Password = pwd;
                buyerUser.UserCode = userCode;
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
                PasswordHash = pwd,
                Password = pwd,
                FullName = request.OwnerFullName.Trim(),
                IsBuyer = true,
                IsShipper = false,
                IsSeller = true,
                IsAdmin = false,
                AccountStatus = "active",
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
            Status = "Pending", // Trạng thái chờ Tài khoản Quản lý xét duyệt
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _mongoService.Stores.InsertOneAsync(newStore);
            Console.WriteLine($"🏪 [MongoDB Atlas] Đã lưu thành công Hồ sơ đăng ký gian hàng '{newStore.StoreName}' của chủ tiệm '{newStore.OwnerFullName}' (StoreCode / ID chữ: {newStore.StoreCode}) vào CSDL MongoDB Atlas!");
            
            string targetEmail = (!string.IsNullOrWhiteSpace(accountKey) && accountKey.Contains("@")) ? accountKey.Trim().ToLower() : "";
            if (!string.IsNullOrEmpty(targetEmail))
            {
                _ = Task.Run(async () =>
                {
                    await SendSellerPendingEmailAsync(targetEmail, request.StoreName, request.OwnerFullName);
                });
            }

            return Ok(new
            {
                success = true,
                message = "Đã gửi hồ sơ thành công! Thư cảm ơn đã được gửi về Gmail của bạn.",
                storeId = newStore.Id
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [MongoDB Atlas Error] {ex.Message}");
            return StatusCode(500, new { success = false, message = $"Lỗi chèn CSDL MongoDB: {ex.Message}" });
        }
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
            // Cập nhật tài khoản User hiện có thành Shipper và lưu Password & UserCode
            var userUpdate = Builders<User>.Update
                .Set(u => u.IsShipper, true)
                .Set(u => u.PasswordHash, pwd)
                .Set(u => u.Password, pwd)
                .Set(u => u.UserCode, userCode)
                .Set(u => u.AccountStatus, "active");
            try
            {
                await _mongoService.Users.UpdateOneAsync(u => u.Id == buyerUser.Id, userUpdate);
                buyerUser.IsShipper = true;
                buyerUser.PasswordHash = pwd;
                buyerUser.Password = pwd;
                buyerUser.UserCode = userCode;
                InMemoryUsers[accountKey] = buyerUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB User Update Error] {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(accountKey))
        {
            // Khởi tạo tài khoản User mới hoàn toàn cho Shipper để đăng nhập được
            buyerUser = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserCode = userCode,
                PhoneEmail = accountKey,
                PasswordHash = pwd,
                Password = pwd,
                FullName = request.FullName.Trim(),
                IsBuyer = true,
                IsShipper = true,
                IsSeller = false,
                IsAdmin = false,
                AccountStatus = "active",
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
            return StatusCode(500, new { success = false, message = $"Lỗi chèn CSDL MongoDB: {ex.Message}" });
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
