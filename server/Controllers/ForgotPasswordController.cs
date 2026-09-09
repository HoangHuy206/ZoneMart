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
public class ForgotPasswordController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    // Lưu trữ tạm OTP in-memory với thời gian hết hạn 120 giây (2 phút)
    private static readonly ConcurrentDictionary<string, OtpInfo> OtpStore = new();

    public ForgotPasswordController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    /// <summary>
    /// BƯỚC 1: Gửi mã OTP 6 chữ số về Email của khách hàng thông qua Gmail SMTP
    /// Sender: dobinh225599@gmail.com
    /// App Password: vvir cypy baev wsdk
    /// Thời gian tồn tại: 120 giây (2 phút)
    /// </summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập địa chỉ Email!" });
        }

        string email = request.Email.Trim().ToLower();

        try
        {
            // 1. Kiểm tra tài khoản trong MongoDB Atlas
            var filter = Builders<User>.Filter.Eq(u => u.PhoneEmail, email);
            var existingUser = await _mongoService.Users.Find(filter).FirstOrDefaultAsync();

            if (existingUser == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy tài khoản", errorType = "ACCOUNT_NOT_FOUND" });
            }

            // 2. Tạo mã xác thực ngẫu nhiên 6 chữ số
            string otpCode = new Random().Next(100000, 999999).ToString();
            DateTime expireTime = DateTime.UtcNow.AddSeconds(120); // Hết hạn sau 120 giây

            OtpStore[email] = new OtpInfo
            {
                OtpCode = otpCode,
                ExpireTime = expireTime
            };

            // 3. Gửi Email Mã Xác Thực thực tế thông qua MailKit (Chủ động gửi + Tự động chọn tài khoản dự phòng nếu có sự cố)
            _ = Task.Run(async () =>
            {
                string subject = $"[{otpCode}] Mã Xác Thực Khôi Phục Mật Khẩu ZoneMart";
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
                                    <p style='color: #64748B !important; font-size: 12.5px; font-weight: 600; margin: 8px 0 0 0;'>Trung Tâm Khôi Phục Mật Khẩu Tài Khoản</p>
                                </td>
                            </tr>

                            <!-- DIVIDER -->
                            <tr>
                                <td style='padding: 0 24px;'>
                                    <div style='height: 1px; background-color: #F1F5F9;'></div>
                                </td>
                            </tr>

                            <!-- TIÊU ĐỀ KHÔI PHỤC MẬT KHẨU -->
                            <tr>
                                <td align='center' style='padding: 20px 24px 12px 24px;'>
                                    <h2 style='color: #D94E15 !important; font-size: 19px; font-weight: 900; margin: 0 0 6px 0;'>🔑 MÃ XÁC THỰC KHÔI PHỤC MẬT KHẨU</h2>
                                    <p style='color: #475569 !important; font-size: 13.5px; margin: 0;'>Chào bạn, dưới đây là mã xác thực để đặt lại mật khẩu mới cho tài khoản <b style='color: #2563EB !important;'>{email}</b></p>
                                </td>
                            </tr>

                            <!-- BOX MÃ XÁC THỰC -->
                            <tr>
                                <td align='center' style='padding: 12px 24px 16px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #FFF7ED; border: 1.5px dashed #D94E15; border-radius: 16px; padding: 20px;'>
                                        <tr>
                                            <td align='center'>
                                                <span style='color: #D94E15 !important; font-size: 38px; font-weight: 900; letter-spacing: 8px; display: block;'>{otpCode}</span>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <tr>
                                <td style='padding: 0 24px 20px 24px;'>
                                    <p style='color: #EF4444 !important; font-size: 13px; text-align: center; margin: 0;'>
                                        <b>⚡ Lưu ý:</b> Mã này có hiệu lực trong <b>2 phút (120 giây)</b>. Vui lòng tuyệt đối không cung cấp mã này cho bất kỳ ai.
                                    </p>
                                </td>
                            </tr>

                            <!-- FOOTER -->
                            <tr>
                                <td style='padding: 0 24px 24px 24px;'>
                                    <div style='height: 1px; background-color: #F1F5F9; margin-bottom: 16px;'></div>
                                    <p style='color: #94A3B8 !important; font-size: 12px; text-align: center; margin: 0;'>
                                        © 2026 ZoneMart E-Commerce Platform. Thư tự động không cần phản hồi.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </div>";

                await SendEmailViaMailKitAsync(email, subject, htmlBody, "ZoneMart Hỗ Trợ Khách Hàng");
            });

            return Ok(new
            {
                success = true,
                message = $"Mã xác thực đã được gửi tới email {email} (hiệu lực 120s)!",
                // Trả về OTP trong response dev để tiện xem thử nếu mạng chậm
                devOtp = otpCode
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }

    /// <summary>
    /// BƯỚC 2: Xác nhận mã OTP 6 chữ số
    /// </summary>
    [HttpPost("verify-otp")]
    public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập mã xác thực!" });
        }

        string email = request.Email.Trim().ToLower();
        string inputOtp = request.Otp.Trim();

        if (!OtpStore.TryGetValue(email, out var otpInfo))
        {
            return BadRequest(new { success = false, message = "Chưa gửi mã xác thực hoặc mã đã hết hạn. Vui lòng yêu cầu lại!" });
        }

        if (DateTime.UtcNow > otpInfo.ExpireTime)
        {
            OtpStore.TryRemove(email, out _);
            return BadRequest(new { success = false, message = "Mã xác thực đã hết hạn (quá 2 phút). Vui lòng gửi lại mã mới!" });
        }

        if (otpInfo.OtpCode != inputOtp)
        {
            return BadRequest(new { success = false, message = "Mã xác thực không chính xác! Vui lòng kiểm tra lại Email." });
        }

        // Xác thực thành công -> Đánh dấu đã xác thực
        otpInfo.IsVerified = true;

        return Ok(new
        {
            success = true,
            message = "Xác nhận mã thành công! Vui lòng nhập mật khẩu mới."
        });
    }

    /// <summary>
    /// BƯỚC 3: Đặt lại Mật Khẩu Mới và lưu trực tiếp vào CSDL MongoDB Atlas
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ mật khẩu mới!" });
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new { success = false, message = "Mật khẩu mới phải có tối thiểu 6 ký tự!" });
        }

        string email = request.Email.Trim().ToLower();

        if (!OtpStore.TryGetValue(email, out var otpInfo) || !otpInfo.IsVerified)
        {
            return BadRequest(new { success = false, message = "Bạn chưa xác thực mã thành công. Vui lòng thực hiện lại từ đầu!" });
        }

        try
        {
            // Cập nhật Mật khẩu mới trong CSDL MongoDB Atlas
            var filter = Builders<User>.Filter.Eq(u => u.PhoneEmail, email);
            var update = Builders<User>.Update.Set(u => u.PasswordHash, request.NewPassword);

            var result = await _mongoService.Users.UpdateOneAsync(filter, update);

            // Xóa mã đã dùng
            OtpStore.TryRemove(email, out _);

            // Lấy địa chỉ IP người dùng thực hiện đổi mật khẩu
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
            {
                clientIp = forwardedFor.ToString().Split(',')[0].Trim();
            }

            // Gửi Thư Cảnh Báo An Toàn Thay Đổi Mật Khẩu (kèm IP & Vị trí ước tính) về Gmail
            _ = Task.Run(async () =>
            {
                await SendPasswordChangedNotificationEmailAsync(email, clientIp);
            });

            return Ok(new
            {
                success = true,
                message = "Đổi mật khẩu thành công! Bạn có thể đăng nhập ngay bây giờ bằng mật khẩu mới."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi cập nhật CSDL MongoDB: {ex.Message}" });
        }
    }

    /// <summary>
    /// GỬI THƯ CẢNH BÁO THAY ĐỔI MẬT KHẨU THÀNH CÔNG VỀ GMAIL NGUỜI DÙNG (KÈM IP VÀ VỊ TRÍ TƯƠNG ĐỐI)
    /// </summary>
    private static async Task SendPasswordChangedNotificationEmailAsync(string toEmail, string rawIp)
    {
        try
        {
            string currentTimeStr = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

            // Xử lý IP & Vị trí ước tính (Tương đối)
            string displayIp = (rawIp == "::1" || rawIp == "127.0.0.1" || string.IsNullOrWhiteSpace(rawIp)) ? "113.190.234.88" : rawIp;
            string locationStr = "Hà Nội, Việt Nam (Định vị ước tính tương đối)";

            if (rawIp != "::1" && rawIp != "127.0.0.1" && !string.IsNullOrWhiteSpace(rawIp))
            {
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                    var json = await httpClient.GetStringAsync($"http://ip-api.com/json/{rawIp}?lang=vi");
                    if (json.Contains("\"city\""))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        string city = root.TryGetProperty("city", out var c) ? c.GetString() ?? "" : "";
                        string country = root.TryGetProperty("country", out var co) ? co.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(country))
                        {
                            locationStr = $"{city}, {country}".Trim(',', ' ');
                        }
                    }
                }
                catch
                {
                    // Giữ vị trí mặc định nếu dịch vụ API bận
                }
            }

            string subject = "[ZoneMart] Cảnh Báo An Toàn: Mật Khẩu Tài Khoản Của Bạn Đã Được Thay Đổi";
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
                                    <p style='color: #64748B !important; font-size: 12.5px; font-weight: 600; margin: 8px 0 0 0;'>Trung Tâm Bảo Mật & An Toàn Tài Khoản</p>
                                </td>
                            </tr>

                            <!-- DIVIDER -->
                            <tr>
                                <td style='padding: 0 24px;'>
                                    <div style='height: 1px; background-color: #F1F5F9;'></div>
                                </td>
                            </tr>
                            
                            <!-- TIÊU ĐỀ CẢNH BÁO -->
                            <tr>
                                <td align='center' style='padding: 20px 24px 12px 24px;'>
                                    <h2 style='color: #D94E15 !important; font-size: 19px; font-weight: 900; margin: 0 0 6px 0;'>🔐 CẢNH BÁO THAY ĐỔI MẬT KHẨU</h2>
                                    <p style='color: #475569 !important; font-size: 13.5px; margin: 0;'>Mật khẩu tài khoản của bạn vừa được cập nhật thành công</p>
                                </td>
                            </tr>
                            
                            <tr>
                                <td style='padding: 0 24px 16px 24px;'>
                                    <p style='color: #334155 !important; font-size: 14px; line-height: 1.6; margin: 0;'>
                                        Xin chào, Hệ thống ghi nhận mật khẩu cho tài khoản <b style='color: #2563EB !important;'>{toEmail}</b> vừa được thay đổi thành công vào lúc <b style='color: #0F172A !important;'>{currentTimeStr}</b>.
                                    </p>
                                </td>
                            </tr>

                            <!-- CHI TIẾT ĐỊA CHỈ & IP (DÙNG BẢNG HTML CHUẨN MỌI THIẾT BỊ) -->
                            <tr>
                                <td style='padding: 8px 24px 16px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #FFF7ED; border: 1.5px solid #FED7AA; border-radius: 16px; padding: 18px 20px;'>
                                        <tr>
                                            <td style='padding-bottom: 10px;'>
                                                <span style='color: #9A3412 !important; font-size: 12.5px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.5px; display: block;'>
                                                    🛡️ THÔNG TIN ĐỊA CHỈ IP & VỊ TRÍ
                                                </span>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 6px 0; color: #431407 !important; font-size: 13.5px; line-height: 1.5;'>
                                                🌐 <b style='color: #431407 !important;'>Địa chỉ IP:</b> <span style='color: #D94E15 !important; font-weight: 700;'>{displayIp}</span>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 6px 0; color: #431407 !important; font-size: 13.5px; line-height: 1.5;'>
                                                📍 <b style='color: #431407 !important;'>Vị trí ước tính:</b> <span style='color: #D94E15 !important; font-weight: 700;'>{locationStr}</span>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <!-- THÔNG BÁO HƯỚNG DẪN AN TOÀN -->
                            <tr>
                                <td style='padding: 0 24px 16px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #FFF7ED; border: 1px solid #FED7AA; border-radius: 16px; padding: 16px 18px;'>
                                        <tr>
                                            <td style='padding-bottom: 8px;'>
                                                <b style='color: #9A3412 !important; font-size: 13.5px;'>⚠️ Có phải bạn vừa thực hiện thay đổi này?</b>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='color: #431407 !important; font-size: 13.5px; line-height: 1.6;'>
                                                Nếu <b>KHÔNG PHẢI BẠN</b> thực hiện đổi mật khẩu, hãy đăng nhập lại tài khoản ngay lập tức xem lại thông tin và tiến hành bảo mật tài khoản nhé!
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <!-- NÚT BẤM VÀO ĐĂNG NHẬP -->
                            <tr>
                                <td align='center' style='padding: 8px 24px 24px 24px;'>
                                    <table border='0' cellpadding='0' cellspacing='0'>
                                        <tr>
                                            <td align='center' style='background-color: #D94E15; border-radius: 30px; padding: 13px 32px;'>
                                                <a href='http://localhost:5173/login' style='color: #ffffff !important; text-decoration: none; font-weight: 800; font-size: 14px; display: inline-block; letter-spacing: 0.5px;'>
                                                    ĐĂNG NHẬP ZONEMART NGAY ➔
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <!-- FOOTER -->
                            <tr>
                                <td style='padding: 0 24px 24px 24px;'>
                                    <div style='height: 1px; background-color: #F1F5F9; margin-bottom: 16px;'></div>
                                    <p style='color: #94A3B8 !important; font-size: 12px; text-align: center; margin: 0;'>
                                        © 2026 ZoneMart E-Commerce Platform. Thư bảo mật tự động từ hệ thống ZoneMart.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </div>";

            await SendEmailViaMailKitAsync(toEmail, subject, htmlBody, "ZoneMart Security");
            Console.WriteLine($"📧 [Gmail SMTP] Đã gửi thành công THƯ CẢNH BÁO THAY ĐỔI MẬT KHẨU (IP: {displayIp}, Vị trí: {locationStr}) tới {toEmail}");
        }
        catch (Exception mailEx)
        {
            Console.WriteLine($"❌ [Gmail SMTP Error] Lỗi gửi thư cảnh báo mật khẩu tới {toEmail}: {mailEx.Message}");
        }
    }

    /// <summary>
    /// Hàm dùng chung gửi Email thông qua MailKit (hỗ trợ tự động thử lại với tài khoản dự phòng nếu tài khoản chính gặp sự cố)
    /// </summary>
    private static async Task<bool> SendEmailViaMailKitAsync(string toEmail, string subject, string htmlBody, string senderName = "ZoneMart Security")
    {
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
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(account.Email, account.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"📧 [MailKit SMTP Success] Đã gửi thành công email tới {toEmail} qua tài khoản {account.Email}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MailKit SMTP Warning] Không thể gửi qua {account.Email}: {ex.Message}. Đang thử tài khoản dự phòng...");
            }
        }

        Console.WriteLine($"❌ [MailKit SMTP Error] Tất cả tài khoản gửi email tới {toEmail} đều thất bại.");
        return false;
    }
}

public class OtpInfo
{
    public string OtpCode { get; set; } = string.Empty;
    public DateTime ExpireTime { get; set; }
    public bool IsVerified { get; set; } = false;
}

public class SendOtpRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
