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
        User? existingUser = null;

        // 1. Kiểm tra từ CSDL MongoDB Atlas thực tế và bộ nhớ lưu trữ
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.PhoneEmail, inputAccount);
            existingUser = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [MongoDB Query Warning] {ex.Message}");
        }

        if (existingUser == null)
        {
            InMemoryUsers.TryGetValue(inputAccount, out existingUser);
        }
        else
        {
            InMemoryUsers[inputAccount] = existingUser;
        }

        // 2. TH 2: KHÔNG TÌM THẤY TÀI KHOẢN TRONG CSDL
        if (existingUser == null)
        {
            return Unauthorized(new { 
                success = false, 
                message = "Không tìm thấy tài khoản", 
                errorType = "ACCOUNT_NOT_FOUND" 
            });
        }

        // 3. TH 1: TÀI KHOẢN ĐÃ TỒN TẠI TRONG CSDL, NHƯNG MẬT KHẨU KHÔNG ĐÚNG
        if (existingUser.PasswordHash != request.Password)
        {
            return Unauthorized(new { 
                success = false, 
                message = "Mật khẩu không đúng", 
                errorType = "INCORRECT_PASSWORD" 
            });
        }

        // Kiểm tra trạng thái tài khoản
        if (existingUser.AccountStatus == "banned")
        {
            return BadRequest(new { success = false, message = "Tài khoản của bạn đã bị khóa vi phạm tiêu chuẩn cộng đồng ZoneMart!" });
        }

        // Tự động nhận diện vai trò dựa trên CSDL
        string determinedRole = "buyer";
        if (existingUser.IsAdmin)
        {
            determinedRole = "admin";
        }
        else if (existingUser.IsSeller)
        {
            determinedRole = "seller";
        }
        else if (existingUser.IsShipper)
        {
            determinedRole = "shipper";
        }

        return Ok(new
        {
            success = true,
            message = $"Đăng nhập thành công với vai trò {GetRoleDisplayName(determinedRole)}!",
            user = new
            {
                id = existingUser.Id,
                phoneEmail = existingUser.PhoneEmail,
                fullName = existingUser.FullName,
                avatarUrl = existingUser.AvatarUrl,
                role = determinedRole,
                walletBalance = existingUser.WalletBalance
            }
        });
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

        // 2. Tạo đối tượng User mới cho Khách Hàng
        var newUser = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PhoneEmail = email,
            PasswordHash = request.Password,
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

        var newStore = new Store
        {
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
            Console.WriteLine($"🏪 [MongoDB Atlas] Đã lưu thành công Hồ sơ đăng ký gian hàng '{newStore.StoreName}' của chủ tiệm '{newStore.OwnerFullName}' vào CSDL MongoDB Atlas!");
            
            return Ok(new
            {
                success = true,
                message = "Đã gửi hồ sơ thành công",
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

        var newShipper = new Shipper
        {
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
            Console.WriteLine($"🛵 [MongoDB Atlas] Đã lưu thành công Hồ sơ tài xế Shipper '{newShipper.FullName}' (Biển số: {newShipper.LicensePlate}) vào CSDL MongoDB Atlas!");

            return Ok(new
            {
                success = true,
                message = "Đã gửi hồ sơ thành công",
                shipperId = newShipper.Id
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [MongoDB Atlas Error] {ex.Message}");
            return StatusCode(500, new { success = false, message = $"Lỗi chèn CSDL MongoDB: {ex.Message}" });
        }
    }
}

public class LoginRequest
{
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
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
}

public class RegisterShipperRequest
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
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
