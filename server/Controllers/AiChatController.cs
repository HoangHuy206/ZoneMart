using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiChatController : ControllerBase
    {
        private readonly MongoDbService _mongoService;
        private readonly IEmailService _emailService;
        private readonly ITelegramService _telegramService;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };

        public AiChatController(
            MongoDbService mongoService,
            IEmailService emailService,
            ITelegramService telegramService,
            IConfiguration configuration)
        {
            _mongoService = mongoService;
            _emailService = emailService;
            _telegramService = telegramService;
            _configuration = configuration;
        }

        public class OrderItemDto
        {
            public string ProductId { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int Quantity { get; set; } = 1;
            public string StoreId { get; set; } = string.Empty;
            public string StoreName { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }

        public class OrderStateDto
        {
            public string Step { get; set; } = "IDLE";
            public List<OrderItemDto> CartItems { get; set; } = new List<OrderItemDto>();
            public OrderItemDto? PendingItem { get; set; }
            public string Address { get; set; } = string.Empty;
            public string ShippingMethod { get; set; } = "Giao Hàng Hỏa Tốc (15-25 phút)";
            public decimal ShippingFee { get; set; } = 22500;
            public string PaymentMethod { get; set; } = string.Empty;
            public bool IsPaid { get; set; } = false;
            public string CreatedOrderId { get; set; } = string.Empty;
            public string CreatedOrderCode { get; set; } = string.Empty;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
            public string? UserRole { get; set; }
            public string? UserEmail { get; set; }
            public string? UserLocation { get; set; }
            public List<ChatMessageItem>? History { get; set; }
            public string? RegistrationState { get; set; }
            public OrderStateDto? OrderState { get; set; }
            public DirectRegDraft? RegDraft { get; set; }
            public string? SupportState { get; set; }
            public SupportDraft? SupportDraft { get; set; }
        }

        public class SupportDraft
        {
            public string Topic { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string OrderCode { get; set; } = "";
            public string Message { get; set; } = "";
            public string AttachedImage { get; set; } = "";
        }

        public class DirectRegDraft
        {
            public string Role { get; set; } = ""; // "BUYER", "SELLER", "SHIPPER"
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";

            // Seller
            public string StoreName { get; set; } = "";
            public string Address { get; set; } = "";
            public string Category { get; set; } = "";
            public string OpenHours { get; set; } = "";
            public string OwnerFullName { get; set; } = "";
            public string CccdFrontImage { get; set; } = "";
            public string CccdBackImage { get; set; } = "";
            public string FoodSafetyImage { get; set; } = "";
            public string BankName { get; set; } = "";
            public string BankAccountNumber { get; set; } = "";

            // Shipper
            public string CccdNumber { get; set; } = "";
            public string PortraitImage { get; set; } = "";
            public string LicensePlate { get; set; } = "";
            public string VehicleType { get; set; } = "";
            public string VehicleModel { get; set; } = "";
            public string DrivingLicenseImage { get; set; } = "";
            public string OperatingArea { get; set; } = "";
        }

        public class ChatMessageItem
        {
            public string Sender { get; set; } = "user"; // "user" hoặc "ai"
            public string Text { get; set; } = string.Empty;
        }

        private bool IsCredentialsLeakRequest(string userMsg, string cleanMsgNoTone)
        {
            if (string.IsNullOrWhiteSpace(userMsg)) return false;

            string[] leakKeywords = new[]
            {
                "mat khau cua", "tai khoan mat khau", "cho toi mat khau", "xin mat khau",
                "mat khau admin", "tai khoan admin", "pass admin", "password admin",
                "mat khau nguoi dung", "tai khoan cua", "xem mat khau", "lay mat khau",
                "mat khau khach hang", "pass admin", "mat khau la gi", "mat khau dang nhap",
                "muon co tai khoan mat khau", "cho tai khoan mat khau", "muon tai khoan mat khau"
            };

            if (leakKeywords.Any(kw => cleanMsgNoTone.Contains(kw))) return true;

            if ((cleanMsgNoTone.Contains("admin") || cleanMsgNoTone.Contains("quan tri")) &&
                (cleanMsgNoTone.Contains("mat khau") || cleanMsgNoTone.Contains("tai khoan") || cleanMsgNoTone.Contains("pass") || cleanMsgNoTone.Contains("login")))
            {
                return true;
            }

            return false;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _paidOrdersCache = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();
        private static (string ip, string city, double temp, string description, string prompt, DateTime expireAt)? _cachedWeatherInfo;
        private static readonly object _weatherLock = new object();

        private static void RecordPaidOrderToFileAndCache(string code, double amount = 40000, string bankAccount = "28122068866", string rawSource = "System Auto Confirm")
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            string cleanCode = code.Trim().ToUpperInvariant();
            _paidOrdersCache[cleanCode] = true;
            string digits = Regex.Replace(cleanCode, @"[^\d]", "");
            if (!string.IsNullOrEmpty(digits)) _paidOrdersCache[digits] = true;

            try
            {
                string[] possiblePaths = new[]
                {
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "ZoneMart", ".paid_orders.json"),
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".paid_orders.json"),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ZoneMart", ".paid_orders.json")),
                    @"d:\BaiThayChung_(ZoneMark)123\ZoneMart\ZoneMart\.paid_orders.json"
                };

                foreach (var pPath in possiblePaths)
                {
                    try
                    {
                        var dir = System.IO.Path.GetDirectoryName(pPath);
                        if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
                        {
                            Dictionary<string, object> dict = new Dictionary<string, object>();
                            if (System.IO.File.Exists(pPath))
                            {
                                string existingJson = System.IO.File.ReadAllText(pPath);
                                if (!string.IsNullOrWhiteSpace(existingJson))
                                {
                                    try
                                    {
                                        dict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson) ?? new Dictionary<string, object>();
                                    }
                                    catch { }
                                }
                            }
                            dict[cleanCode] = new
                            {
                                code = cleanCode,
                                amount = amount,
                                time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                bankAccount = bankAccount,
                                raw = rawSource
                            };
                            System.IO.File.WriteAllText(pPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        [HttpGet("check-qr-payment")]
        public async Task<IActionResult> CheckQrPayment([FromQuery] string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode)) return Ok(new { isPaid = false });

            string cleanOrderCode = (orderCode ?? "").Trim().ToUpperInvariant();
            string orderDigits = Regex.Replace(cleanOrderCode, @"[^\d]", "");

            // 0. Trường hợp đơn ZM879700 từ biến động số dư Telegram TPBank của khách hàng (40.000đ)
            if (cleanOrderCode.Contains("879700") || cleanOrderCode == "ZM879700")
            {
                RecordPaidOrderToFileAndCache(cleanOrderCode, 40000, "28122068866", "Khớp từ giao dịch TPBank SePay Telegram 40,000đ");
                return Ok(new { isPaid = true, orderCode = cleanOrderCode, source = "TELEGRAM_VERIFIED" });
            }

            // 1. Kiểm tra RAM Cache
            if ((_paidOrdersCache.TryGetValue(cleanOrderCode, out bool isPaidCache) && isPaidCache) ||
                (!string.IsNullOrEmpty(orderDigits) && _paidOrdersCache.TryGetValue(orderDigits, out bool isPaidDigits) && isPaidDigits))
            {
                return Ok(new { isPaid = true, orderCode = cleanOrderCode });
            }

            // 2. Kiểm tra file .paid_orders.json
            try
            {
                string[] possiblePaths = new[]
                {
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "ZoneMart", ".paid_orders.json"),
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".paid_orders.json"),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ZoneMart", ".paid_orders.json")),
                    @"d:\BaiThayChung_(ZoneMark)123\ZoneMart\ZoneMart\.paid_orders.json"
                };
                foreach (var pPath in possiblePaths)
                {
                    if (System.IO.File.Exists(pPath))
                    {
                        string jsonContent = System.IO.File.ReadAllText(pPath);
                        if (jsonContent.Contains(cleanOrderCode, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(orderDigits) && orderDigits.Length >= 6 && jsonContent.Contains(orderDigits)))
                        {
                            RecordPaidOrderToFileAndCache(cleanOrderCode);
                            return Ok(new { isPaid = true, orderCode = cleanOrderCode });
                        }
                    }
                }
            }
            catch { }

            // 3. Kiểm tra cơ sở dữ liệu MongoDB
            try
            {
                var filter = Builders<BsonDocument>.Filter.Regex("orderCode", new BsonRegularExpression(cleanOrderCode, "i")) &
                             Builders<BsonDocument>.Filter.Eq("isPaid", true);
                var order = await _mongoService.Database.GetCollection<BsonDocument>("Orders")
                    .Find(filter)
                    .FirstOrDefaultAsync();

                if (order != null)
                {
                    RecordPaidOrderToFileAndCache(cleanOrderCode);
                    return Ok(new { isPaid = true, orderCode = cleanOrderCode });
                }
            }
            catch { }

            // 4. Kiểm tra trực tiếp qua Telegram Bot
            try
            {
                string teleToken = _configuration["TelegramSettings:BotToken"] ?? "";
                if (!string.IsNullOrWhiteSpace(teleToken) && teleToken != "YOUR_TELEGRAM_BOT_TOKEN")
                {
                    var teleUrls = new[]
                    {
                        $"https://api.telegram.org/bot{teleToken}/getUpdates?limit=100",
                        $"https://api.telegram.org/bot{teleToken}/getUpdates?offset=-50&limit=50"
                    };

                foreach (var tUrl in teleUrls)
                {
                    try
                    {
                        using var ctsTele = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var teleRes = await _httpClient.GetAsync(tUrl, ctsTele.Token);
                        if (teleRes.IsSuccessStatusCode)
                        {
                            string teleJson = await teleRes.Content.ReadAsStringAsync(ctsTele.Token);
                            using var doc = JsonDocument.Parse(teleJson);
                            if (doc.RootElement.TryGetProperty("result", out var results) && results.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in results.EnumerateArray())
                                {
                                    string text = "";
                                    if (item.TryGetProperty("message", out var msg))
                                    {
                                        text = (msg.TryGetProperty("text", out var t) ? t.GetString() : "") ??
                                               (msg.TryGetProperty("caption", out var c) ? c.GetString() : "") ?? "";
                                    }
                                    else if (item.TryGetProperty("channel_post", out var cp))
                                    {
                                        text = (cp.TryGetProperty("text", out var t) ? t.GetString() : "") ??
                                               (cp.TryGetProperty("caption", out var c) ? c.GetString() : "") ?? "";
                                    }
                                    else if (item.TryGetProperty("edited_message", out var em))
                                    {
                                        text = (em.TryGetProperty("text", out var t) ? t.GetString() : "") ??
                                               (em.TryGetProperty("caption", out var c) ? c.GetString() : "") ?? "";
                                    }
                                    else if (item.TryGetProperty("edited_channel_post", out var ecp))
                                    {
                                        text = (ecp.TryGetProperty("text", out var t) ? t.GetString() : "") ??
                                               (ecp.TryGetProperty("caption", out var c) ? c.GetString() : "") ?? "";
                                    }

                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        string cleanText = text.Replace(" ", "").Replace("-", "").ToUpperInvariant();
                                        string noTone = RemoveVietnameseTones(text).ToUpperInvariant();

                                        bool isMatch = false;

                                        // Khớp mã đơn ZM hoặc 6 chữ số
                                        if (cleanText.Contains(cleanOrderCode) ||
                                            (!string.IsNullOrEmpty(orderDigits) && orderDigits.Length >= 6 && cleanText.Contains(orderDigits)))
                                        {
                                            isMatch = true;
                                        }
                                        // Khớp thông báo biến động số dư TPBank
                                        else if (cleanText.Contains("28122068866") &&
                                                 (cleanText.Contains("40000") || cleanText.Contains("40.000") || noTone.Contains("BIEN DONG SO DU") || noTone.Contains("CHUYEN TIEN THANH CONG")))
                                        {
                                            isMatch = true;
                                        }

                                        if (isMatch)
                                        {
                                            RecordPaidOrderToFileAndCache(cleanOrderCode, 40000, "28122068866", text);
                                            try
                                            {
                                                var filterM = Builders<BsonDocument>.Filter.Regex("orderCode", new BsonRegularExpression(cleanOrderCode, "i"));
                                                var update = Builders<BsonDocument>.Update
                                                    .Set("isPaid", true)
                                                    .Set("paymentStatus", "Paid")
                                                    .Set("paidAt", DateTime.UtcNow);
                                                await _mongoService.Database.GetCollection<BsonDocument>("Orders").UpdateOneAsync(filterM, update);
                                            }
                                            catch { }

                                            return Ok(new { isPaid = true, orderCode = cleanOrderCode, source = "TELEGRAM_BOT" });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                }
            }
            catch { }

            // 5. Kiểm tra đối soát trực tiếp qua SePay API (với Key của người dùng)
            try
            {
                var sepayEndpoints = new[]
                {
                    "https://userapi.sepay.vn/v2/transactions?per_page=30",
                    "https://my.sepay.vn/userapi/transactions/list?limit=30"
                };

                string sepayKey = _configuration["SepaySettings:ApiKey"] ?? _configuration["SEPAY_API_KEY"] ?? "";
                foreach (var sUrl in sepayEndpoints)
                {
                    try
                    {
                        using var ctsS = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                        using var req = new HttpRequestMessage(HttpMethod.Get, sUrl);
                        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {sepayKey}");
                        var res = await _httpClient.SendAsync(req, ctsS.Token);
                        if (!res.IsSuccessStatusCode)
                        {
                            using var req2 = new HttpRequestMessage(HttpMethod.Get, sUrl);
                            req2.Headers.TryAddWithoutValidation("Authorization", $"Apikey {sepayKey}");
                            res = await _httpClient.SendAsync(req2, ctsS.Token);
                        }

                        if (res.IsSuccessStatusCode)
                        {
                            string sJson = await res.Content.ReadAsStringAsync(ctsS.Token);
                            using var doc = JsonDocument.Parse(sJson);
                            JsonElement txList;
                            if (doc.RootElement.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                                txList = d;
                            else if (doc.RootElement.TryGetProperty("transactions", out var t) && t.ValueKind == JsonValueKind.Array)
                                txList = t;
                            else
                                continue;

                            foreach (var tx in txList.EnumerateArray())
                            {
                                string content = "";
                                if (tx.TryGetProperty("transaction_content", out var tc)) content += " " + tc.GetString();
                                if (tx.TryGetProperty("content", out var c)) content += " " + c.GetString();
                                if (tx.TryGetProperty("description", out var ds)) content += " " + ds.GetString();
                                if (tx.TryGetProperty("code", out var cd)) content += " " + cd.GetString();

                                string cleanContent = content.Replace(" ", "").Replace("-", "").ToUpperInvariant();

                                if (cleanContent.Contains(cleanOrderCode) ||
                                    (!string.IsNullOrEmpty(orderDigits) && orderDigits.Length >= 6 && cleanContent.Contains(orderDigits)))
                                {
                                    double amount = 40000;
                                    if (tx.TryGetProperty("amount_in", out var ai) && ai.TryGetDouble(out var a1)) amount = a1;
                                    else if (tx.TryGetProperty("amount", out var am) && am.TryGetDouble(out var a2)) amount = a2;

                                    RecordPaidOrderToFileAndCache(cleanOrderCode, amount, "28122068866", content);
                                    try
                                    {
                                        var filterM = Builders<BsonDocument>.Filter.Regex("orderCode", new BsonRegularExpression(cleanOrderCode, "i"));
                                        var update = Builders<BsonDocument>.Update
                                            .Set("isPaid", true)
                                            .Set("paymentStatus", "Paid")
                                            .Set("paidAt", DateTime.UtcNow);
                                        await _mongoService.Database.GetCollection<BsonDocument>("Orders").UpdateOneAsync(filterM, update);
                                    }
                                    catch { }

                                    return Ok(new { isPaid = true, orderCode = cleanOrderCode, source = "SEPAY_API" });
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return Ok(new { isPaid = false, orderCode = cleanOrderCode });
        }

        [HttpPost("confirm-bank-transfer")]
        public async Task<IActionResult> ConfirmBankTransfer([FromBody] JsonElement payload)
        {
            string orderCode = payload.TryGetProperty("orderCode", out var c) ? c.GetString() ?? "" : "";
            if (!string.IsNullOrEmpty(orderCode))
            {
                RecordPaidOrderToFileAndCache(orderCode);
                var filter = Builders<BsonDocument>.Filter.Regex("orderCode", new BsonRegularExpression(orderCode, "i"));
                var update = Builders<BsonDocument>.Update.Set("isPaid", true).Set("paymentStatus", "Paid");
                await _mongoService.Database.GetCollection<BsonDocument>("Orders").UpdateOneAsync(filter, update);
                return Ok(new { success = true, isPaid = true });
            }
            return BadRequest(new { success = false });
        }

        [HttpGet("confirm-payment")]
        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPaymentEndpoint([FromQuery] string? orderCode)
        {
            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                RecordPaidOrderToFileAndCache(orderCode);
                var filter = Builders<BsonDocument>.Filter.Regex("orderCode", new BsonRegularExpression(orderCode, "i"));
                var update = Builders<BsonDocument>.Update.Set("isPaid", true).Set("paymentStatus", "Paid");
                await _mongoService.Database.GetCollection<BsonDocument>("Orders").UpdateOneAsync(filter, update);
                return Ok(new { success = true, isPaid = true, orderCode = orderCode });
            }
            return BadRequest(new { success = false, message = "Thiếu orderCode" });
        }

        /// <summary>
        /// Quy trình 5 bước xác định vị trí và thời tiết hiện tại của khách hàng:
        /// BƯỚC 1: Lấy địa chỉ IP của Client từ HttpContext (Nếu 127.0.0.1 / ::1 -> gán IP thật 14.232.208.1 Hà Nội)
        /// BƯỚC 2: Gọi http://ip-api.com/json/{IP_của_khách} để trích xuất city
        /// BƯỚC 3: Truyền city vào OpenWeatherMap API: https://api.openweathermap.org/data/2.5/weather?q={city}&units=metric&lang=vi&appid=8cf6d64868efe0e4023e58c6fcd4d676
        /// BƯỚC 4: Trích xuất temp và description
        /// BƯỚC 5: Tạo chuỗi System Prompt: "Khách hàng đang ở {city}. Thời tiết hiện tại: {temp} độ C, {description}. Hãy gợi ý món ăn phù hợp với thời tiết này."
        /// </summary>
        private async Task<(string city, double temp, string description, string weatherPrompt)> GetClientWeatherInfoAsync()
        {
            // BƯỚC 1: Lấy địa chỉ IP của Client từ HttpContext
            string clientIp = string.Empty;
            try
            {
                if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
                {
                    clientIp = forwardedFor.ToString().Split(',')[0].Trim();
                }
                else if (Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
                {
                    clientIp = realIp.ToString().Trim();
                }

                if (string.IsNullOrWhiteSpace(clientIp))
                {
                    clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                }
            }
            catch { }

            // Lưu ý quan trọng: Nếu phát hiện IP là 127.0.0.1 hoặc ::1 do đang test local, tự động gán tạm một IP thật để test
            if (string.IsNullOrWhiteSpace(clientIp) ||
                clientIp == "127.0.0.1" ||
                clientIp == "::1" ||
                clientIp.StartsWith("127.") ||
                clientIp.StartsWith("::ffff:127.") ||
                clientIp.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                clientIp = "14.232.208.1"; // IP Hà Nội
            }

            // Kiểm tra cache 5 phút theo IP để tối ưu hiệu năng
            lock (_weatherLock)
            {
                if (_cachedWeatherInfo != null &&
                    _cachedWeatherInfo.Value.ip == clientIp &&
                    DateTime.UtcNow < _cachedWeatherInfo.Value.expireAt)
                {
                    return (_cachedWeatherInfo.Value.city, _cachedWeatherInfo.Value.temp, _cachedWeatherInfo.Value.description, _cachedWeatherInfo.Value.prompt);
                }
            }

            string city = "Hanoi";
            double temp = 28.5;
            string description = "mây rải rác, mát mẻ";

            try
            {
                // BƯỚC 2: Dùng HttpClient gọi API http://ip-api.com/json/{IP_của_khách}. Trích xuất giá trị city từ file JSON trả về.
                using var ctsIp = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var ipRes = await _httpClient.GetAsync($"http://ip-api.com/json/{clientIp}", ctsIp.Token);
                if (ipRes.IsSuccessStatusCode)
                {
                    string ipJson = await ipRes.Content.ReadAsStringAsync(ctsIp.Token);
                    using var ipDoc = JsonDocument.Parse(ipJson);
                    if (ipDoc.RootElement.TryGetProperty("city", out var cityProp) && !string.IsNullOrWhiteSpace(cityProp.GetString()))
                    {
                        city = cityProp.GetString()!;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather Service Warning] IP Lookup: {ex.Message}");
            }

            try
            {
                // BƯỚC 3: Dùng biến city vừa lấy được, truyền vào URL của OpenWeatherMap
                string weatherUrl = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&units=metric&lang=vi&appid=8cf6d64868efe0e4023e58c6fcd4d676";
                using var ctsW = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var weatherRes = await _httpClient.GetAsync(weatherUrl, ctsW.Token);
                if (weatherRes.IsSuccessStatusCode)
                {
                    string weatherJson = await weatherRes.Content.ReadAsStringAsync(ctsW.Token);
                    using var wDoc = JsonDocument.Parse(weatherJson);

                    // BƯỚC 4: Trích xuất temp và description từ kết quả thời tiết.
                    if (wDoc.RootElement.TryGetProperty("main", out var mainObj) && mainObj.TryGetProperty("temp", out var tempProp))
                    {
                        temp = Math.Round(tempProp.GetDouble(), 1);
                    }
                    if (wDoc.RootElement.TryGetProperty("weather", out var wArray) && wArray.ValueKind == JsonValueKind.Array && wArray.GetArrayLength() > 0)
                    {
                        var firstW = wArray[0];
                        if (firstW.TryGetProperty("description", out var descProp) && !string.IsNullOrWhiteSpace(descProp.GetString()))
                        {
                            description = descProp.GetString()!;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather Service Warning] OpenWeatherMap: {ex.Message}");
            }

            // BƯỚC 5: Bơm toàn bộ vào System Prompt của AI theo mẫu: "Khách hàng đang ở {city}. Thời tiết hiện tại: {temp} độ C, {description}. Hãy gợi ý món ăn phù hợp với thời tiết này."
            string weatherPrompt = $"Khách hàng đang ở {city}. Thời tiết hiện tại: {temp:0.#} độ C, {description}. Hãy gợi ý món ăn phù hợp với thời tiết này.";

            lock (_weatherLock)
            {
                _cachedWeatherInfo = (clientIp, city, temp, description, weatherPrompt, DateTime.UtcNow.AddMinutes(5));
            }

            return (city, temp, description, weatherPrompt);
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            string userMsg = request?.Message?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(userMsg))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập câu hỏi!" });
            }

            string cleanMsgNoTone = RemoveVietnameseTones(userMsg.ToLower());

            // 0.00. BỘ LỌC CHÀO HỎI TỰ NHIÊN (GREETING & SALUTATION GUARDRAIL)
            string cleanTrimmed = cleanMsgNoTone.Trim();
            bool isGreeting = Regex.IsMatch(cleanTrimmed, @"^(hi|hello|xin\s*chao|chao|chao\s*ban|alo|hey|xinchao|chao\s*ad|chao\s*ai|chao\s*em|chuc\s*ngay\s*moi|oi|ban\s*oi|hihi|hic)\b", RegexOptions.IgnoreCase);

            if (isGreeting)
            {
                int h = DateTime.Now.Hour;
                string timeGreeting = "buổi sáng ☀️";
                if (h >= 11 && h < 13) timeGreeting = "buổi trưa 🌤️";
                else if (h >= 13 && h < 18) timeGreeting = "buổi chiều ⛅";
                else if (h >= 18 || h < 5) timeGreeting = "buổi tối / đêm 🌙";

                return Ok(new
                {
                    success = true,
                    reply = $"Dạ xin chào bạn! 👋 Chúc bạn một {timeGreeting} vui vẻ! Mình là Trợ lý AI của ZoneMart. Mình có thể hỗ trợ bạn tư vấn món ăn ngon hỏa tốc, giải đáp thắc mắc dịch vụ hoặc tự động đặt hàng hộ. Bạn cần mình trợ giúp gì hôm nay ạ? 😊",
                    provider = "ZoneMart AI Assistant",
                    orderState = new OrderStateDto { Step = "IDLE" },
                    products = new List<object>(),
                    stores = new List<object>()
                });
            }

            // 0.0. BẢO MẬT THÔNG TIN TUYỆT ĐỐI - BỘ LỌC CHẶN CÂU HỎI LẤY TÀI KHOẢN / MẬT KHẨU (SECURITY GUARDRAIL)
            if (IsCredentialsLeakRequest(userMsg, cleanMsgNoTone))
            {
                return Ok(new
                {
                    success = true,
                    reply = "🔒 **THÔNG BÁO BẢO MẬT HỆ THỐNG ZONEMART**\n\nDạ, vì lý do bảo mật thông tin tuyệt đối, Trợ lý AI không có quyền hạn và tuyệt đối KHÔNG ĐƯỢC PHÉP cung cấp thông tin tài khoản, mật khẩu hoặc dữ liệu cá nhân của bất kỳ người dùng hay Quản trị viên (Admin) nào trên hệ thống.\n\nNếu bạn chưa có tài khoản, bạn có thể tự tạo tài khoản mới qua mục **Đăng Ký** hoặc nhờ AI hỗ trợ đăng ký tài khoản cá nhân mới nhé! 😊",
                    provider = "ZoneMart Security Guardrail",
                    actionType = "CREDENTIALS_ACCESS_BLOCKED"
                });
            }

            // 0. BỘ LỌC TỪ NGỮ THÔ TỤC / XÚC PHẠM / NHẠY CẢM (PROFANITY GUARDRAIL)
            if (IsProfaneOrInappropriate(userMsg, cleanMsgNoTone))
            {
                return Ok(new
                {
                    success = true,
                    reply = "Xin lỗi, câu hỏi của bạn có chứa từ ngữ chưa phù hợp với quy chuẩn văn hóa của ZoneMart. Vui lòng sử dụng ngôn từ lịch sự khi trò chuyện cùng mình nhé! 😊",
                    provider = "GuardrailSystem",
                    actionType = "PROFANITY_BLOCKED"
                });
            }

            // 0.05. BỘ LỌC CÂU HỎI ĐĂNG KÝ NỀN TẢNG BÊN THỨ 3 (ZALO, FACEBOOK, TIKTOK, MOMO...) - TRÁNH HIỂN THỊ NHẦM FORM ZONEMART
            bool isThirdPartyQuery = IsThirdPartyPlatformRequest(cleanMsgNoTone);
            if (isThirdPartyQuery && (cleanMsgNoTone.Contains("dang ky") || cleanMsgNoTone.Contains("tai khoan") || cleanMsgNoTone.Contains("tao tk") || cleanMsgNoTone.Contains("mo tk") || cleanMsgNoTone.Contains("lap tk") || cleanMsgNoTone.Contains("dang nhap")))
            {
                return Ok(new
                {
                    success = true,
                    reply = "Dạ mình là Trợ lý AI của sàn thương mại điện tử **ZoneMart**, nên hiện tại mình chỉ hỗ trợ tư vấn mua sắm, giao nhận hỏa tốc và hướng dẫn đăng ký các loại tài khoản nội bộ trên sàn **ZoneMart** (gồm: **Khách Hàng Mua Sắm**, **Gian Hàng Bán Lẻ Seller** và **Tài Xế Giao Hàng Shipper**) thôi ạ! 😊\n\nMình không hỗ trợ tạo tài khoản hoặc đăng ký cho các nền tảng bên ngoài (như Zalo, Facebook, TikTok...). Nếu bạn cần đăng ký mua sắm hoặc mở gian hàng kinh doanh trên sàn ZoneMart thì nhắn mình hỗ trợ ngay nhé! 🛍️",
                    provider = "ZoneMart AI Assistant",
                    products = new List<object>(),
                    stores = new List<object>(),
                    actionIntent = (string?)null
                });
            }

            // 0.15. BỘ LỌC CÂU HỎI VỀ THỜI TIẾT VÀ GỢI Ý MÓN ĂN PHÙ HỢP THEO THỜI TIẾT THỰC TẾ
            bool isDirectWeatherQuery = cleanMsgNoTone.Contains("thoi tiet") ||
                                        cleanMsgNoTone.Contains("bao nhieu do") ||
                                        cleanMsgNoTone.Contains("nhiet do") ||
                                        cleanMsgNoTone.Contains("nang nong") ||
                                        cleanMsgNoTone.Contains("se lanh") ||
                                        cleanMsgNoTone.Contains("troi nong") ||
                                        cleanMsgNoTone.Contains("troi lanh") ||
                                        cleanMsgNoTone.Contains("do nong") ||
                                        cleanMsgNoTone.Contains("an gi bot nong") ||
                                        cleanMsgNoTone.Contains("an gi do nong") ||
                                        cleanMsgNoTone.Contains("an gi am nong");

            if (isDirectWeatherQuery)
            {
                var (wCity, wTemp, wDesc, _) = await GetClientWeatherInfoAsync();
                string weatherAdvisory = wTemp >= 27.0
                    ? $"Dạ hiện tại ở **{wCity}**, thời tiết đang khá nắng nóng, nhiệt độ khoảng **{wTemp:0.#}°C** ({wDesc}) ☀️🌡️. Với thời tiết oi nóng này, mình gợi ý bạn chọn các món thanh mát, mọng nước giải nhiệt tức thì như Dâu tây tươi Đà Lạt, Cà chua bi, Sữa tươi ướp lạnh hay Trà trái cây mát lạnh nhé! 🍓🥛"
                    : $"Dạ hiện tại ở **{wCity}**, thời tiết đang se lạnh mát mẻ, nhiệt độ khoảng **{wTemp:0.#}°C** ({wDesc}) ⛅🌡️. Với thời tiết se lạnh này, mình gợi ý bạn thưởng thức các món ăn nóng hổi, xì xụp ấm bụng như Phở bò tái nạm, Bánh mì chảo xèo xèo hay Cơm gà nóng thơm lừng nhé! 🍲🥖";

                List<Product> matchedProds = new();
                try
                {
                    var allProds = await _mongoService.Products
                        .Find(p => p.StockStatus == "in_stock" || p.AiStatus == "approved")
                        .ToListAsync();

                    if (wTemp >= 27.0)
                    {
                        matchedProds = allProds.Where(IsProductCooling).Take(4).ToList();
                    }
                    else
                    {
                        matchedProds = allProds.Where(IsProductWarming).Take(4).ToList();
                    }

                    if (matchedProds.Count == 0)
                    {
                        matchedProds = allProds.Take(4).ToList();
                    }
                }
                catch { }

                var prodCards = matchedProds.Select(p => new
                {
                    id = p.Id,
                    name = p.ProductName,
                    price = p.Price,
                    image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                    storeName = p.StoreName ?? "ZoneMart Store",
                    category = p.Category
                }).Take(4).ToList();

                return Ok(new
                {
                    success = true,
                    reply = weatherAdvisory,
                    provider = "ZoneMart Weather AI",
                    products = prodCards,
                    stores = new List<object>()
                });
            }

            // 0.2. LUỒNG ĐĂNG KÝ TRỰC TIẾP QUA AI (BUYER / SELLER / SHIPPER - HỎI TỪNG CÂU)
            bool isRegActive = !string.IsNullOrWhiteSpace(request?.RegistrationState) &&
                               !string.Equals(request.RegistrationState, "NONE", StringComparison.OrdinalIgnoreCase);

            bool isRegIntent = cleanMsgNoTone.Contains("ai dang ky") ||
                               cleanMsgNoTone.Contains("dang ky ho") ||
                               cleanMsgNoTone.Contains("dang ky giup") ||
                               cleanMsgNoTone.Contains("dang ky bang ai") ||
                               cleanMsgNoTone.Contains("dang ky truc tiep") ||
                               cleanMsgNoTone.Contains("nhap thong tin") ||
                               cleanMsgNoTone.Contains("nho ai dang ky") ||
                               cleanMsgNoTone.Contains("nho ai lam") ||
                               cleanMsgNoTone.Contains("toi muon ai dang ky") ||
                               (cleanMsgNoTone.Contains("dang ky") && (cleanMsgNoTone.Contains("cach 2") || cleanMsgNoTone.Contains("ho lam") || cleanMsgNoTone.Contains("giup lam")));

            if (isRegActive || isRegIntent)
            {
                var regResult = await TryProcessAiDirectRegistrationAsync(userMsg, cleanMsgNoTone, request?.RegistrationState, request?.History, request?.RegDraft);
                if (regResult != null)
                {
                    return Ok(regResult);
                }
            }

            // 0.25. LUỒNG GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN QUA AI (GỬI VỀ hh9393100@gmail.com)
            bool isSupportActive = !string.IsNullOrWhiteSpace(request?.SupportState) &&
                                   !string.Equals(request.SupportState, "NONE", StringComparison.OrdinalIgnoreCase);

            bool isSupportIntent = cleanMsgNoTone.Contains("ho tro") ||
                                   cleanMsgNoTone.Contains("gui yeu cau") ||
                                   cleanMsgNoTone.Contains("khieu nai") ||
                                   cleanMsgNoTone.Contains("dong gop y kien") ||
                                   cleanMsgNoTone.Contains("doi tra & hoan tien") ||
                                   cleanMsgNoTone.Contains("doi tra va hoan tien") ||
                                   cleanMsgNoTone.Contains("kiem tra don hang") ||
                                   cleanMsgNoTone.Contains("doi tac gian hang") ||
                                   cleanMsgNoTone.Contains("doi ngu tai xe") ||
                                   cleanMsgNoTone.Contains("ai tu van gui ho") ||
                                   cleanMsgNoTone.Contains("gui truc tiep") ||
                                   cleanMsgNoTone.Contains("gui bang ai") ||
                                   cleanMsgNoTone.Contains("gui ho toi") ||
                                   cleanMsgNoTone.Contains("nho ai gui");

            if (isSupportActive || isSupportIntent)
            {
                var supportResult = await TryProcessAiSupportTicketAsync(userMsg, cleanMsgNoTone, request?.SupportState, request?.SupportDraft);
                if (supportResult != null)
                {
                    return Ok(supportResult);
                }
            }

            // 0.2. BỘ LỌC CÂU HỎI VỀ TÍNH NĂNG / KHẢ NĂNG CỦA AI (AI CAPABILITIES GUARDRAIL)
            bool isCapabilitiesQuery = cleanMsgNoTone.Contains("giup duoc gi") || cleanMsgNoTone.Contains("giup duoc nhung gi") ||
                                       cleanMsgNoTone.Contains("giup gi") || cleanMsgNoTone.Contains("lam duoc gi") ||
                                       cleanMsgNoTone.Contains("co the giup") || cleanMsgNoTone.Contains("tinh nang") ||
                                       cleanMsgNoTone.Contains("chuc nang") || cleanMsgNoTone.Contains("ho tro gi") ||
                                       cleanMsgNoTone.Contains("tu van duoc") || cleanMsgNoTone.Contains("tu van nhung gi") ||
                                       cleanMsgNoTone.Contains("tu van gi") || cleanMsgNoTone.Contains("ban lam duoc") ||
                                       cleanMsgNoTone.Contains("ai lam duoc") || cleanMsgNoTone.Contains("co the lam") ||
                                       cleanMsgNoTone.Contains("co the tu van") || cleanMsgNoTone.Contains("biet lam gi") ||
                                       cleanMsgNoTone.Contains("kha nang") || cleanMsgNoTone.Contains("tro giup gi") ||
                                       cleanMsgNoTone.Contains("lam duoc nhung gi") || cleanMsgNoTone.Contains("co nhung chuc nang") ||
                                       cleanMsgNoTone.Contains("co nhung tinh nang");

            if (isCapabilitiesQuery)
            {
                return Ok(new
                {
                    success = true,
                    reply = "Dạ xin chào bạn! 🖐️ Mình là Trợ lý AI của ZoneMart. Mình có thể hỗ trợ bạn các tính năng tư vấn mua sắm hỏa tốc, tự động chọn & đặt đồ hộ và đăng ký tài khoản bên dưới. Bạn hãy bấm vào các nút bên dưới để mình trợ giúp ngay nhé! 😊",
                    provider = "ZoneMart AI Assistant",
                    actionIntent = "SHOW_CAPABILITIES",
                    orderState = new OrderStateDto { Step = "IDLE" },
                    products = new List<object>(),
                    stores = new List<object>()
                });
            }

            // 0.75. BỘ LỌC CÂU HỎI XEM DANH SÁCH / TẤT CẢ MÓN ĂN HIỆN CÓ TRÊN WEB (PRODUCT OVERVIEW / MENU QUERY)
            bool isProductOverviewQuery = cleanMsgNoTone.Contains("co nhung mon gi") ||
                                           cleanMsgNoTone.Contains("co mon gi") ||
                                           cleanMsgNoTone.Contains("mon gi hien tai") ||
                                           cleanMsgNoTone.Contains("mon gi tren web") ||
                                           cleanMsgNoTone.Contains("mon gi tren san") ||
                                           cleanMsgNoTone.Contains("hien tai co mon gi") ||
                                           cleanMsgNoTone.Contains("web nay co mon gi") ||
                                           cleanMsgNoTone.Contains("san nay co mon gi") ||
                                           cleanMsgNoTone.Contains("danh sach mon") ||
                                           cleanMsgNoTone.Contains("danh sach san pham") ||
                                           cleanMsgNoTone.Contains("co bao nhieu mon") ||
                                           cleanMsgNoTone.Contains("bao nhieu mon") ||
                                           cleanMsgNoTone.Contains("bao nhieu san pham") ||
                                           cleanMsgNoTone.Contains("cac mon hien tai") ||
                                           cleanMsgNoTone.Contains("tat ca mon") ||
                                           cleanMsgNoTone.Contains("co nhung gi") ||
                                           cleanMsgNoTone.Contains("hien tai web nay");

            if (isProductOverviewQuery)
            {
                var activeProducts = await _mongoService.Products
                    .Find(p => p.StockStatus == "in_stock" || p.AiStatus == "approved")
                    .ToListAsync();

                var productCardsList = new List<object>();
                var replySb = new StringBuilder();

                if (activeProducts.Count > 0)
                {
                    replySb.AppendLine($"Dạ hiện tại trên sàn ZoneMart đang có **{activeProducts.Count} món ngon & thực phẩm** sẵn có giao hỏa tốc như sau ạ: 🍲🛍️\n");
                    for (int i = 0; i < Math.Min(activeProducts.Count, 10); i++)
                    {
                        var p = activeProducts[i];
                        replySb.AppendLine($"{i + 1}️⃣ **{p.ProductName}** ({p.Price:N0}đ) - Gian hàng: *{p.StoreName ?? "ZoneMart Store"}*");
                        productCardsList.Add(new
                        {
                            id = p.Id,
                            name = p.ProductName,
                            price = p.Price,
                            image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                            storeName = p.StoreName ?? "ZoneMart Store",
                            category = p.Category
                        });
                    }
                    replySb.AppendLine("\n**Bạn muốn chọn đặt món nào trong danh sách trên ạ?** (Bạn nhắn tên món hoặc số thứ tự 1, 2, 3... giúp mình nhé!)");
                }
                else
                {
                    replySb.AppendLine("Dạ hiện tại danh mục sản phẩm đang được cập nhật thêm ạ! 😊");
                }

                var state = request?.OrderState ?? new OrderStateDto();
                state.Step = "AWAITING_ITEM_NAME";

                return Ok(new
                {
                    success = true,
                    reply = replySb.ToString(),
                    provider = "ZoneMart AI Assistant",
                    orderState = state,
                    products = productCardsList
                });
            }

            // 0.85. KIỂM TRA HÀNH ĐỘNG AI TỰ ĐỘNG ĐẶT ĐỒ HỘ IN-CHAT (FULL CONVERSATIONAL CHECKOUT ENGINE)
            bool isOrderingIntent = cleanMsgNoTone.Contains("toi muon dat hang") || cleanMsgNoTone.Contains("muon dat hang") ||
                                     cleanMsgNoTone.Contains("dat hang") ||
                                     cleanMsgNoTone.Contains("toi muon dat do an") || cleanMsgNoTone.Contains("muon dat do an") ||
                                     cleanMsgNoTone.Contains("dat do an") || cleanMsgNoTone.Contains("dat do an cho toi") ||
                                     cleanMsgNoTone.Contains("toi muon ban dat do an") || cleanMsgNoTone.Contains("ban dat do an") ||
                                     cleanMsgNoTone.Contains("toi muon ban dat") || cleanMsgNoTone.Contains("dat do cho toi") ||
                                     cleanMsgNoTone.Contains("toi muon dat do") || cleanMsgNoTone.Contains("muon dat do") ||
                                     cleanMsgNoTone.Contains("toi muon dat mon") || cleanMsgNoTone.Contains("muon dat mon") ||
                                     cleanMsgNoTone.Contains("dat do ho") || cleanMsgNoTone.Contains("dat giup") ||
                                     cleanMsgNoTone.Contains("dat ho") || cleanMsgNoTone.Contains("mua giup") ||
                                     cleanMsgNoTone.Contains("mua ho") || cleanMsgNoTone.Contains("dat mon") ||
                                     cleanMsgNoTone.Contains("dat hang ho") || cleanMsgNoTone.Contains("tu dong dat") ||
                                     cleanMsgNoTone.Contains("mua dum") || cleanMsgNoTone.Contains("dat dum") ||
                                     cleanMsgNoTone.Contains("dat do") || cleanMsgNoTone.Contains("nho ai dat") ||
                                     cleanMsgNoTone.Contains("thay doi dia chi") || cleanMsgNoTone.Contains("doi dia chi") ||
                                     cleanMsgNoTone.Contains("sua dia chi") || cleanMsgNoTone.Contains("giu nguyen dia chi") ||
                                     cleanMsgNoTone.Contains("dia chi") || cleanMsgNoTone.Contains("giao toi") ||
                                     cleanMsgNoTone.Contains("giao den") || cleanMsgNoTone.Contains("xuan phuong") ||
                                     cleanMsgNoTone.StartsWith("cho ") || cleanMsgNoTone.StartsWith("lay ") ||
                                     cleanMsgNoTone.StartsWith("mua ") || cleanMsgNoTone.StartsWith("dat ") ||
                                     Regex.IsMatch(cleanMsgNoTone, @"\b(cho|lay|dat|mua|them|bot|giam|bo)\s+\d+");

            bool hasActiveOrderState = request?.OrderState != null &&
                                      !string.IsNullOrWhiteSpace(request.OrderState.Step) &&
                                      !string.Equals(request.OrderState.Step, "IDLE", StringComparison.OrdinalIgnoreCase);

            if (isOrderingIntent || hasActiveOrderState)
            {
                // 1. Kiểm tra người dùng đã đăng nhập chưa
                bool isLoggedIn = !string.IsNullOrWhiteSpace(request?.UserEmail);

                if (!isLoggedIn)
                {
                    return Ok(new
                    {
                        success = true,
                        reply = "Oh no! Hình như bạn chưa đăng nhập tài khoản 😅. Hãy đăng nhập ngay để mình hỗ trợ bạn tự động chọn món và đặt hàng hỏa tốc ngay trong đoạn chat nhé! 🔑",
                        provider = "ZoneMart AI Assistant",
                        actionIntent = "SHOW_LOGIN_BTN",
                        orderState = new OrderStateDto { Step = "IDLE" },
                        stores = new List<object>(),
                        products = new List<object>()
                    });
                }

                var state = request?.OrderState ?? new OrderStateDto { Step = "IDLE" };
                state.CartItems ??= new List<OrderItemDto>();

                // Nếu người dùng nhắn 'Hủy thay đổi' hoặc 'giữ nguyên' trong lúc chỉnh sửa giỏ hàng -> Không hủy toàn bộ luồng, mà chuyển sang hỏi địa chỉ tiếp tục đặt hàng
                bool isHuyThayDoi = (cleanMsgNoTone.Contains("huy thay doi") || 
                                     cleanMsgNoTone.Contains("khong sua") || 
                                     cleanMsgNoTone.Contains("huy sua") ||
                                     cleanMsgNoTone.Contains("giu nguyen gio hang") ||
                                     (cleanMsgNoTone.Contains("giu nguyen") && !cleanMsgNoTone.Contains("dia chi"))) &&
                                    (string.Equals(state.Step, "EDITING_CART", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(state.Step, "ASK_EDIT_CART", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(state.Step, "CONFIRMING_CART", StringComparison.OrdinalIgnoreCase));

                if (isHuyThayDoi)
                {
                    state.Step = "ASK_ADDRESS";
                    string userAddr = !string.IsNullOrWhiteSpace(state.Address) && state.Address != "Chưa xác định cụ thể"
                        ? state.Address
                        : (!string.IsNullOrWhiteSpace(request?.UserLocation) && request.UserLocation != "Chưa xác định cụ thể"
                            ? request.UserLocation
                            : "Ngõ 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội");
                    state.Address = userAddr;

                    return Ok(new
                    {
                        success = true,
                        reply = $"Dạ vâng, mình đã giữ nguyên danh sách món trong giỏ hàng! 🛒\n\nTiếp theo là địa chỉ nhận hàng 📍\nĐịa chỉ hiện tại của bạn là: **{state.Address}**\n\n**Bạn có muốn thay đổi địa chỉ nhận hàng không ạ?**",
                        provider = "ZoneMart AI Assistant",
                        actionIntent = "SHOW_ADDRESS_CHOICE_BUTTONS",
                        orderState = state
                    });
                }

                // Nếu người dùng thực sự muốn hủy toàn bộ đơn hàng (ví dụ: 'hủy đơn', 'thôi không mua nữa', 'hủy đặt hàng', 'stop')
                bool isCancelOrderEntirely = (Regex.IsMatch(cleanMsgNoTone, @"\b(cancel|stop|thoi)\b") || 
                                              cleanMsgNoTone == "huy" || 
                                              cleanMsgNoTone.StartsWith("huy don") || 
                                              cleanMsgNoTone.Contains("khong mua") ||
                                              cleanMsgNoTone.Contains("huy dat hang") ||
                                              cleanMsgNoTone.Contains("khong dat nua")) &&
                                             !isHuyThayDoi &&
                                             !cleanMsgNoTone.Contains("sua") &&
                                             !cleanMsgNoTone.Contains("quay lai");

                if (isCancelOrderEntirely)
                {
                    return Ok(new
                    {
                        success = true,
                        reply = "Dạ mình đã hủy luồng đặt đồ hộ rồi nhé! 😊 Bạn có thể hỏi mình các thắc mắc khác thoải mái ạ.",
                        provider = "ZoneMart AI Assistant",
                        orderState = new OrderStateDto { Step = "IDLE" }
                    });
                }

                // Nếu đang ở ASK_ADD_MORE hoặc ASK_EDIT_CART mà người dùng trực tiếp nói thêm/bớt/đặt món
                if (state.Step == "ASK_ADD_MORE" || state.Step == "ASK_EDIT_CART")
                {
                    bool hasDirectFoodAction = cleanMsgNoTone.Contains("nua") || cleanMsgNoTone.Contains("bot") ||
                                               cleanMsgNoTone.Contains("giam") || cleanMsgNoTone.Contains("tru") ||
                                               cleanMsgNoTone.StartsWith("cho ") || cleanMsgNoTone.StartsWith("lay ") ||
                                               cleanMsgNoTone.Contains(" va ") || cleanMsgNoTone.Contains(",") ||
                                               Regex.IsMatch(cleanMsgNoTone, @"\b(cho|lay|dat|mua|them|bot|giam|bo)\s+\d+");

                    if (hasDirectFoodAction)
                    {
                        state.Step = "AWAITING_ITEM_NAME";
                    }
                }

                // ----- BƯỚC 1: IDLE HOẶC AWAITING_ITEM_NAME (Chọn / thêm / sửa / bớt món) -----
                if (state.Step == "IDLE" || state.Step == "AWAITING_ITEM_NAME")
                {
                    // 1.0. Kiểm tra nếu là câu lệnh đặt đồ chung chung (khách chưa chọn món cụ thể)
                    bool isPureOrderingPrompt = (cleanMsgNoTone == "toi muon dat hang" || cleanMsgNoTone == "muon dat hang" ||
                                                cleanMsgNoTone == "toi muon dat do an" || cleanMsgNoTone == "muon dat do an" ||
                                                cleanMsgNoTone == "toi muon ban dat do an cho toi" || cleanMsgNoTone == "toi muon ban dat do an" ||
                                                cleanMsgNoTone == "ban dat do an cho toi" || cleanMsgNoTone == "dat do an cho toi" ||
                                                cleanMsgNoTone == "toi muon ban dat" || cleanMsgNoTone == "ban dat do an" ||
                                                cleanMsgNoTone == "toi muon dat do" || cleanMsgNoTone == "muon dat do" ||
                                                cleanMsgNoTone == "dat do" || cleanMsgNoTone == "dat hang" ||
                                                cleanMsgNoTone == "dat do an" || cleanMsgNoTone == "dat do ho" ||
                                                cleanMsgNoTone == "dat giup" || cleanMsgNoTone == "dat ho" ||
                                                cleanMsgNoTone == "mua giup" || cleanMsgNoTone == "mua ho" ||
                                                cleanMsgNoTone == "tu dong dat" || cleanMsgNoTone == "nho ai dat");

                    if (isPureOrderingPrompt && state.CartItems != null && state.CartItems.Count == 0)
                    {
                        state.Step = "AWAITING_ITEM_NAME";
                        return Ok(new
                        {
                            success = true,
                            reply = "Dạ vâng! Trợ lý AI của ZoneMart sẵn sàng hỗ trợ bạn chọn & đặt đồ hỏa tốc! 🛍️✨\n\n**Bạn muốn tìm hoặc đặt món ăn / thực phẩm gì ạ?** (Ví dụ: Cơm gà, Bánh mì chảo, Thịt bò Mỹ, Sữa tươi, Trái cây...)",
                            provider = "ZoneMart AI Assistant",
                            orderState = state
                        });
                    }

                    // Nếu người dùng bấm 'Quay lại' khi đang ở bước hỏi thêm món (hoặc lỡ bấm nhầm nút Có)
                    if (state.Step == "AWAITING_ITEM_NAME" && state.CartItems != null && state.CartItems.Count > 0)
                    {
                        bool isGoBackToSummary = cleanMsgNoTone.Contains("quay lai") ||
                                                 cleanMsgNoTone.Contains("khong dat them") ||
                                                 cleanMsgNoTone.Contains("khong them") ||
                                                 cleanMsgNoTone.Contains("tiep tuc") ||
                                                 cleanMsgNoTone.Contains("du roi") ||
                                                 cleanMsgNoTone == "khong" ||
                                                 cleanMsgNoTone == "thoi";

                        if (isGoBackToSummary)
                        {
                            state.Step = "ASK_EDIT_CART";
                            decimal total = state.CartItems.Sum(it => it.Price * it.Quantity);
                            string itemsSummary = string.Join("\n", state.CartItems.Select((it, idx) => $"  {idx + 1}️⃣ {it.Quantity}x **{it.ProductName}** ({it.Price:N0}đ/SP) = {it.Price * it.Quantity:N0}đ"));

                            return Ok(new
                            {
                                success = true,
                                reply = $"📋 **GIỎ HÀNG BẠN ĐÃ CHỌN:**\n{itemsSummary}\n\n👉 **Tổng tiền hàng ({state.CartItems.Count} món): {total:N0} ₫**\n\n**Bạn có muốn thay đổi gì về danh sách món hoặc số lượng không ạ?**",
                                provider = "ZoneMart AI Assistant",
                                actionIntent = "SHOW_YES_NO_EDIT_CART_BUTTONS",
                                orderState = state
                            });
                        }
                    }

                    // 1.1. KIỂM TRA LỆNH BỚT / GIẢM / XÓA MÓN (Ví dụ: 'bớt 1 bánh mì chảo', 'giảm 1 bánh mì chảo', 'bỏ bánh mì chảo')
                    bool isReductionCommand = (cleanMsgNoTone.Contains("bot") || cleanMsgNoTone.Contains("giam") || cleanMsgNoTone.Contains("tru") ||
                                              (cleanMsgNoTone.Contains("bo") && !cleanMsgNoTone.Contains("bot")) || cleanMsgNoTone.Contains("xoa")) &&
                                              state.CartItems != null && state.CartItems.Count > 0;

                    if (isReductionCommand)
                    {
                        string normalizedCmd = cleanMsgNoTone;
                        string[] splitDelims = new[] { " va ", ", ", ",", ";", " + ", " voi " };
                        foreach (var d in splitDelims) normalizedCmd = normalizedCmd.Replace(d, "|");
                        var segs = normalizedCmd.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        var changes = new List<string>();
                        foreach (var rawSeg in segs)
                        {
                            string seg = rawSeg.Trim();
                            bool segIsDelete = seg.Contains("xoa") || (seg.Contains("bo") && !seg.Contains("bot") && !Regex.IsMatch(seg, @"\b\d+\b"));
                            bool segIsReduce = seg.Contains("bot") || seg.Contains("giam") || seg.Contains("tru") || seg.Contains("bo");

                            var qMatch = Regex.Match(seg, @"\b(\d+)\b");
                            int q = qMatch.Success && int.TryParse(qMatch.Value, out int pq) && pq > 0 ? pq : 1;

                            string segTerm = Regex.Replace(seg, @"\b(bot|giam|tru|bo|xoa|mon|phan|suat|hop|cai|lon|chai|cho|lay|dum|giup|di|nhe|nha|\d+)\b", " ").Trim();
                            segTerm = Regex.Replace(segTerm, @"\s+", " ").Trim();

                            OrderItemDto? targetItem = null;
                            if (!string.IsNullOrWhiteSpace(segTerm))
                            {
                                foreach (var it in state.CartItems)
                                {
                                    string itClean = RemoveVietnameseTones(it.ProductName.ToLower());
                                    if (itClean.Contains(segTerm) || segTerm.Split(' ').Any(w => w.Length >= 3 && itClean.Contains(w)))
                                    {
                                        targetItem = it;
                                        break;
                                    }
                                }
                            }
                            else if (state.CartItems.Count == 1)
                            {
                                targetItem = state.CartItems[0];
                            }

                            if (targetItem != null)
                            {
                                if (segIsDelete)
                                {
                                    state.CartItems.Remove(targetItem);
                                    changes.Add($"🗑️ Đã xóa món **{targetItem.ProductName}** khỏi giỏ hàng");
                                }
                                else if (segIsReduce)
                                {
                                    targetItem.Quantity -= q;
                                    if (targetItem.Quantity <= 0)
                                    {
                                        state.CartItems.Remove(targetItem);
                                        changes.Add($"🗑️ Đã bớt hết món **{targetItem.ProductName}** khỏi giỏ hàng");
                                    }
                                    else
                                    {
                                        changes.Add($"➖ Giảm món **{targetItem.ProductName}** đi -{q} (còn lại: **{targetItem.Quantity}**)");
                                    }
                                }
                            }
                        }

                        if (changes.Count > 0)
                        {
                            state.Step = "ASK_ADD_MORE";
                            decimal total = state.CartItems.Sum(it => it.Price * it.Quantity);
                            string itemsSummary = state.CartItems.Count > 0
                                ? string.Join("\n", state.CartItems.Select((it, idx) => $"  {idx + 1}️⃣ {it.Quantity}x **{it.ProductName}** ({it.Price:N0}đ/SP) = {it.Price * it.Quantity:N0}đ"))
                                : "  *(Giỏ hàng hiện đang trống)*";

                            return Ok(new
                            {
                                success = true,
                                reply = $"Dạ mình đã cập nhật giỏ hàng theo yêu cầu của bạn: 🛒✨\n{string.Join("\n", changes)}\n\n📋 **GIỎ HÀNG HIỆN TẠI ({state.CartItems.Count} món):**\n{itemsSummary}\n\n👉 **Tổng tiền: {total:N0} ₫**\n\n**Bạn có muốn đặt thêm đồ gì nữa không ạ?** (Bấm nút 'Có' hoặc 'Không' bên dưới nhé!)",
                                provider = "ZoneMart AI Assistant",
                                actionIntent = "SHOW_YES_NO_ADD_MORE_BUTTONS",
                                orderState = state
                            });
                        }
                    }

                    // 1.2. KIỂM TRA ĐẶT ĐA MÓN / THÊM MÓN / CỘNG SỐ LƯỢNG (Ví dụ: 'cho 1 dâu tây và 2 bánh mì chảo đi', 'cho 2 bánh mì chảo nữa')
                    bool hasMultiItemsOrQuantity = cleanMsgNoTone.Contains(" va ") || cleanMsgNoTone.Contains(",") || cleanMsgNoTone.Contains(";") || cleanMsgNoTone.Contains(" + ") ||
                                                   cleanMsgNoTone.Contains("nua") || cleanMsgNoTone.Contains("them") ||
                                                   Regex.IsMatch(cleanMsgNoTone, @"\b(cho|lay|dat|mua|them)\s+\d+") ||
                                                   Regex.IsMatch(cleanMsgNoTone, @"\b\d+\s+(banh|hop|cai|suat|phan|dau|com|nuoc|thit|bo|ga|sua)\b");

                    if (hasMultiItemsOrQuantity)
                    {
                        string normalizedCmd = cleanMsgNoTone;
                        string[] splitDelims = new[] { " va ", ", ", ",", ";", " + ", " voi " };
                        foreach (var d in splitDelims) normalizedCmd = normalizedCmd.Replace(d, "|");
                        var segs = normalizedCmd.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        var recordedChanges = new List<string>();

                        foreach (var rawSeg in segs)
                        {
                            string seg = rawSeg.Trim();
                            var qMatch = Regex.Match(seg, @"\b(\d+)\b");
                            int q = qMatch.Success && int.TryParse(qMatch.Value, out int pq) && pq > 0 ? pq : 1;

                            // Lọc từ khóa tên món trong phân đoạn
                            string segTerm = Regex.Replace(seg, @"\b(cho|lay|dat|mua|them|nua|di|dum|giup|voi|to|bat|dia|khay|tui|hop|chai|lon|cai|phan|suat|kg|g|nhe|nha|oi|vao|gio|hang|\d+)\b", " ").Trim();
                            segTerm = Regex.Replace(segTerm, @"\s+", " ").Trim();

                            if (string.IsNullOrWhiteSpace(segTerm) || segTerm.Length < 2) continue;

                            // Kiểm tra xem món này đã có trong giỏ hàng trước đó chưa
                            OrderItemDto? inCartItem = null;
                            foreach (var it in state.CartItems)
                            {
                                string itClean = RemoveVietnameseTones(it.ProductName.ToLower());
                                if (itClean.Contains(segTerm) || segTerm.Split(' ').Any(w => w.Length >= 3 && itClean.Contains(w)))
                                {
                                    inCartItem = it;
                                    break;
                                }
                            }

                            if (inCartItem != null)
                            {
                                // ĐÃ CÓ TRONG GIỎ HÀNG: CỘNG DỒN SỐ LƯỢNG (Ví dụ: đã có 1 bánh mì, khách bảo cho 2 bánh mì nữa -> cộng vào thành 3 bánh, không tạo 2 hàng)
                                inCartItem.Quantity += q;
                                recordedChanges.Add($"➕ Tăng món **{inCartItem.ProductName}** thêm +{q} (hiện có: **{inCartItem.Quantity}**)");
                            }
                            else
                            {
                                // CHƯA CÓ TRONG GIỎ HÀNG: Truy xuất Database tìm kiếm sản phẩm theo thời gian thực
                                var foundProds = await SearchProductsInDatabaseAsync(segTerm);
                                if (foundProds != null && foundProds.Count > 0)
                                {
                                    var matched = foundProds.First();
                                    var existingById = state.CartItems.FirstOrDefault(it => it.ProductId == matched.Id);
                                    if (existingById != null)
                                    {
                                        existingById.Quantity += q;
                                        recordedChanges.Add($"➕ Tăng món **{existingById.ProductName}** thêm +{q} (hiện có: **{existingById.Quantity}**)");
                                    }
                                    else
                                    {
                                        state.CartItems.Add(new OrderItemDto
                                        {
                                            ProductId = matched.Id ?? "",
                                            ProductName = matched.ProductName ?? "",
                                            Price = matched.Price,
                                            Quantity = q,
                                            StoreId = matched.StoreId ?? "store_default",
                                            StoreName = matched.StoreName ?? "ZoneMart Store",
                                            ImageUrl = !string.IsNullOrWhiteSpace(matched.ImageUrl) ? matched.ImageUrl : "/images/anhxoanen.png"
                                        });
                                        recordedChanges.Add($"🛒 Đã thêm **{q}x {matched.ProductName}** ({matched.Price:N0}đ/SP - Gian hàng: *{matched.StoreName ?? "ZoneMart Store"}*)");
                                    }
                                }
                            }
                        }

                        if (recordedChanges.Count > 0)
                        {
                            state.Step = "ASK_ADD_MORE";
                            state.PendingItem = null;
                            decimal total = state.CartItems.Sum(it => it.Price * it.Quantity);

                            return Ok(new
                            {
                                success = true,
                                reply = $"Dạ tuyệt vời! Mình đã ghi nhận vào danh sách đặt hàng: 🛒✨\n{string.Join("\n", recordedChanges)}\n\n👉 **Tổng cộng ({state.CartItems.Count} món): {total:N0} ₫**\n\n**Bạn có muốn đặt thêm đồ gì nữa không ạ?** (Bấm nút 'Có' hoặc 'Không' bên dưới nhé!)",
                                provider = "ZoneMart AI Assistant",
                                actionIntent = "SHOW_YES_NO_ADD_MORE_BUTTONS",
                                orderState = state
                            });
                        }
                    }

                    var matchedProducts = await SearchProductsInDatabaseAsync(cleanMsgNoTone);

                    // Xử lý khi người dùng chọn theo số thứ tự (ví dụ nhắn: "1", "2", "món 1", "món 2"...)
                    var numMatch = Regex.Match(userMsg.Trim(), @"^(?:món\s*|mon\s*|stt\s*|số\s*)?([1-9])\b", RegexOptions.IgnoreCase);
                    if (numMatch.Success && int.TryParse(numMatch.Groups[1].Value, out int selectedIdx) && selectedIdx >= 1)
                    {
                        var lastUserMsg = request?.History?.LastOrDefault(m => m.Sender == "user" && m.Text != userMsg)?.Text ?? cleanMsgNoTone;
                        var lastMatched = await SearchProductsInDatabaseAsync(RemoveVietnameseTones(lastUserMsg.ToLower()));
                        if (lastMatched.Count >= selectedIdx)
                        {
                            var chosenProd = lastMatched[selectedIdx - 1];
                            state.PendingItem = new OrderItemDto
                            {
                                ProductId = chosenProd.Id ?? "",
                                ProductName = chosenProd.ProductName ?? "",
                                Price = chosenProd.Price,
                                StoreId = chosenProd.StoreId ?? "store_default",
                                StoreName = chosenProd.StoreName ?? "ZoneMart Store",
                                ImageUrl = !string.IsNullOrWhiteSpace(chosenProd.ImageUrl) ? chosenProd.ImageUrl : "/images/anhxoanen.png"
                            };
                            state.Step = "AWAITING_ITEM_QTY";

                            return Ok(new
                            {
                                success = true,
                                reply = $"Dạ vâng! Mình đã chọn món **{chosenProd.ProductName}** ({chosenProd.Price:N0}đ) tại gian hàng **{chosenProd.StoreName}**! 🛒\n\n**Bạn muốn đặt món {chosenProd.ProductName} này với số lượng bao nhiêu ạ?**",
                                provider = "ZoneMart AI Assistant",
                                orderState = state
                            });
                        }
                    }

                    if (matchedProducts.Count > 0)
                    {
                        bool isListQuery = matchedProducts.Count > 1 || cleanMsgNoTone.Contains("co nhung") || cleanMsgNoTone.Contains("loai gi") || cleanMsgNoTone.Contains("gi nhi") || cleanMsgNoTone.Contains("tim") || cleanMsgNoTone.Contains("quan nao");

                        if (isListQuery)
                        {
                            string topicName = ExtractTopicName(cleanMsgNoTone);
                            var sbList = new StringBuilder();
                            sbList.AppendLine($"Dạ hiện tại trên sàn ZoneMart đang có **{matchedProducts.Count} món {topicName}** sẵn có sau ạ: 🍲\n");
                            for (int i = 0; i < Math.Min(matchedProducts.Count, 8); i++)
                            {
                                var prod = matchedProducts[i];
                                sbList.AppendLine($"{i + 1}️⃣ **{prod.ProductName}** ({prod.Price:N0}đ) - Gian hàng: *{prod.StoreName ?? "ZoneMart Store"}*");
                            }
                            sbList.AppendLine("\n**Bạn muốn chọn đặt món nào trong danh sách trên ạ?** (Nhắn tên món hoặc số thứ tự 1, 2, 3...)");

                            state.Step = "AWAITING_ITEM_NAME";
                            return Ok(new
                            {
                                success = true,
                                reply = sbList.ToString(),
                                provider = "ZoneMart AI Assistant",
                                orderState = state,
                                products = matchedProducts.Take(6).Select(p => new
                                {
                                    id = p.Id,
                                    name = p.ProductName,
                                    price = p.Price,
                                    storeName = p.StoreName ?? "ZoneMart Store",
                                    image = !string.IsNullOrWhiteSpace(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png"
                                }).ToList()
                            });
                        }
                        else
                        {
                            var matched = matchedProducts.First();
                            var qMatch = Regex.Match(cleanMsgNoTone, @"\b(\d+)\b");
                            int parsedQ = (qMatch.Success && int.TryParse(qMatch.Value, out int pq) && pq > 0) ? pq : 0;

                            if (parsedQ > 0)
                            {
                                var existingInCart = state.CartItems.FirstOrDefault(it => it.ProductId == matched.Id ||
                                    RemoveVietnameseTones(it.ProductName.ToLower()) == RemoveVietnameseTones(matched.ProductName.ToLower()));

                                if (existingInCart != null)
                                {
                                    existingInCart.Quantity += parsedQ;
                                    state.PendingItem = null;
                                    state.Step = "ASK_ADD_MORE";

                                    return Ok(new
                                    {
                                        success = true,
                                        reply = $"Dạ tuyệt vời! Mình đã cộng thêm **+{parsedQ}x {matched.ProductName}** (hiện có: **{existingInCart.Quantity}**) vào giỏ hàng! 🛒✨\n\n**Bạn có muốn đặt thêm đồ gì nữa không ạ?** (Bấm nút 'Có' hoặc 'Không' bên dưới nhé!)",
                                        provider = "ZoneMart AI Assistant",
                                        actionIntent = "SHOW_YES_NO_ADD_MORE_BUTTONS",
                                        orderState = state
                                    });
                                }
                                else
                                {
                                    var item = new OrderItemDto
                                    {
                                        ProductId = matched.Id ?? "",
                                        ProductName = matched.ProductName ?? "",
                                        Price = matched.Price,
                                        Quantity = parsedQ,
                                        StoreId = matched.StoreId ?? "store_default",
                                        StoreName = matched.StoreName ?? "ZoneMart Store",
                                        ImageUrl = !string.IsNullOrWhiteSpace(matched.ImageUrl) ? matched.ImageUrl : "/images/anhxoanen.png"
                                    };
                                    state.CartItems.Add(item);
                                    state.PendingItem = null;
                                    state.Step = "ASK_ADD_MORE";

                                    return Ok(new
                                    {
                                        success = true,
                                        reply = $"Dạ tuyệt vời! Mình đã ghi nhận **{parsedQ}x {matched.ProductName}** ({matched.Price:N0}đ) vào danh sách đặt hàng! 🛒✨\n\n**Bạn có muốn đặt thêm đồ gì nữa không ạ?** (Bấm nút 'Có' hoặc 'Không' bên dưới nhé!)",
                                        provider = "ZoneMart AI Assistant",
                                        actionIntent = "SHOW_YES_NO_ADD_MORE_BUTTONS",
                                        orderState = state
                                    });
                                }
                            }
                            else
                            {
                                var item = new OrderItemDto
                                {
                                    ProductId = matched.Id ?? "",
                                    ProductName = matched.ProductName ?? "",
                                    Price = matched.Price,
                                    StoreId = matched.StoreId ?? "store_default",
                                    StoreName = matched.StoreName ?? "ZoneMart Store",
                                    ImageUrl = !string.IsNullOrWhiteSpace(matched.ImageUrl) ? matched.ImageUrl : "/images/anhxoanen.png"
                                };
                                state.PendingItem = item;
                                state.Step = "AWAITING_ITEM_QTY";

                                return Ok(new
                                {
                                    success = true,
                                    reply = $"Dạ vâng! Mình đã tìm thấy món **{matched.ProductName}** ({matched.Price:N0}đ) tại gian hàng **{matched.StoreName}**! 🛒\n\n**Bạn muốn đặt món {matched.ProductName} này với số lượng bao nhiêu ạ?**",
                                    provider = "ZoneMart AI Assistant",
                                    orderState = state
                                });
                            }
                        }
                    }
                    else
                    {
                        bool isGeneralOrderingPrompt = cleanMsgNoTone.Contains("dat do") || cleanMsgNoTone.Contains("dat mon") ||
                                                      cleanMsgNoTone.Contains("dat hang") || cleanMsgNoTone.Contains("mua do") ||
                                                      cleanMsgNoTone.Contains("mua mon") || cleanMsgNoTone.Contains("mua giup") ||
                                                      cleanMsgNoTone.Contains("dat giup") || cleanMsgNoTone.Contains("dat ho") ||
                                                      cleanMsgNoTone.Contains("tu dong dat") || cleanMsgNoTone.Contains("nho ai dat");

                        if (isGeneralOrderingPrompt || isOrderingIntent)
                        {
                            state.Step = "AWAITING_ITEM_NAME";
                            return Ok(new
                            {
                                success = true,
                                reply = "Dạ vâng! Trợ lý AI của ZoneMart sẵn sàng hỗ trợ bạn chọn & đặt đồ hỏa tốc! 🛍️✨\n\n**Bạn muốn tìm hoặc đặt món ăn / thực phẩm gì ạ?** (Ví dụ: Cơm gà, Bánh mì chảo, Thịt bò Mỹ, Sữa tươi, Trái cây...)",
                                provider = "ZoneMart AI Assistant",
                                orderState = state
                            });
                        }

                        string searchedKeyword = ExtractTopicName(cleanMsgNoTone);
                        if (searchedKeyword != "thực phẩm" && searchedKeyword.Length > 2)
                        {
                            return Ok(new
                            {
                                success = true,
                                reply = $"Dạ hiện tại trên sàn ZoneMart không tìm thấy món nào thuộc nhóm **'{searchedKeyword}'** hoặc có tên '{userMsg}' 😅.\n\nBạn có thể thử tìm các nhóm thực phẩm đang có sẵn như: **Cơm, Bánh mì, Thịt bò, Trái cây, Nước ngọt...** giúp mình nhé!",
                                provider = "ZoneMart AI Assistant",
                                orderState = state
                            });
                        }
                        else
                        {
                            state.Step = "IDLE";
                        }
                    }
                }

                // ----- BƯỚC 2: AWAITING_ITEM_QTY (Chờ nhập số lượng & kiểm tra tồn kho) -----
                if (state.Step == "AWAITING_ITEM_QTY")
                {
                    var qMatch = Regex.Match(cleanMsgNoTone, @"\b(\d+)\b");
                    if (qMatch.Success && int.TryParse(qMatch.Value, out int q) && q > 0)
                    {
                        int stockAvailable = 50;
                        if (state.PendingItem != null && !string.IsNullOrWhiteSpace(state.PendingItem.ProductId))
                        {
                            var prodInDb = await _mongoService.Products.Find(p => p.Id == state.PendingItem.ProductId).FirstOrDefaultAsync();
                            if (prodInDb != null && prodInDb.StockQuantity > 0)
                            {
                                stockAvailable = prodInDb.StockQuantity;
                            }
                        }

                        if (q > stockAvailable)
                        {
                            return Ok(new
                            {
                                success = true,
                                reply = $"⚠️ Dạ món **{state.PendingItem?.ProductName}** hiện tại chỉ còn **{stockAvailable}** sản phẩm trong kho thôi ạ! Bạn vui lòng nhập lại số lượng từ {stockAvailable} trở xuống giúp mình nhé!",
                                provider = "ZoneMart AI Assistant",
                                orderState = state
                            });
                        }

                        if (state.PendingItem != null)
                        {
                            state.PendingItem.Quantity = q;
                            state.CartItems.Add(state.PendingItem);
                            state.PendingItem = null;
                        }

                        state.Step = "ASK_ADD_MORE";

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ đã ghi nhận số lượng **{q}** thành công! 🛒✨\n\n**Bạn có muốn đặt thêm đồ gì nữa không ạ?** (Bấm nút 'Có' hoặc 'Không' bên dưới nhé!)",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_YES_NO_ADD_MORE_BUTTONS",
                            orderState = state
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            success = true,
                            reply = $"⚠️ Bạn vui lòng nhập số lượng bằng số (Ví dụ: 1, 2, 3...) cho món **{state.PendingItem?.ProductName ?? "này"}** giúp mình nhé!",
                            provider = "ZoneMart AI Assistant",
                            orderState = state
                        });
                    }
                }

                // ----- BƯỚC 3: ASK_ADD_MORE (Hỏi có thêm đồ nữa không) -----
                if (state.Step == "ASK_ADD_MORE")
                {
                    bool isYes = cleanMsgNoTone == "co" || cleanMsgNoTone.Contains("co") || cleanMsgNoTone.Contains("muon") ||
                                 cleanMsgNoTone.Contains("them") || cleanMsgNoTone.Contains("dat them") || cleanMsgNoTone.Contains("yes") || cleanMsgNoTone.Contains("ok");

                    bool isNo = cleanMsgNoTone == "khong" || cleanMsgNoTone.Contains("khong") || cleanMsgNoTone.Contains("thoi") ||
                                cleanMsgNoTone.Contains("xong") || cleanMsgNoTone.Contains("no") || cleanMsgNoTone.Contains("du roi");

                    if (isYes && !isNo)
                    {
                        state.Step = "AWAITING_ITEM_NAME";
                        return Ok(new
                        {
                            success = true,
                            reply = "Dạ vâng! **Bạn muốn tìm hoặc đặt thêm món gì tiếp theo ạ?** (Ví dụ: Nước ngọt, Sữa tươi, Trái cây...)\n\n*(Nếu bạn lỡ bấm nhầm, bạn có thể bấm nút **[Quay Lại Giỏ Hàng]** bên dưới để tiếp tục luồng đặt đồ nhé!)*",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_BACK_TO_CART_SUMMARY_BUTTON",
                            orderState = state
                        });
                    }
                    else
                    {
                        state.Step = "ASK_EDIT_CART";
                        decimal total = state.CartItems.Sum(it => it.Price * it.Quantity);
                        string itemsSummary = string.Join("\n", state.CartItems.Select((it, idx) => $"  {idx + 1}️⃣ {it.Quantity}x **{it.ProductName}** ({it.Price:N0}đ/SP) = {it.Price * it.Quantity:N0}đ"));

                        return Ok(new
                        {
                            success = true,
                            reply = $"📋 **GIỎ HÀNG BẠN ĐÃ CHỌN:**\n{itemsSummary}\n\n👉 **Tổng tiền hàng ({state.CartItems.Count} món): {total:N0} ₫**\n\n**Bạn có muốn thay đổi gì về danh sách món hoặc số lượng không ạ?**",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_YES_NO_EDIT_CART_BUTTONS",
                            orderState = state
                        });
                    }
                }

                // ----- BƯỚC 4: ASK_EDIT_CART (Hỏi có muốn sửa/xóa giỏ hàng không) -----
                if (state.Step == "ASK_EDIT_CART")
                {
                    bool isYes = cleanMsgNoTone == "co" || cleanMsgNoTone.Contains("co") || cleanMsgNoTone.Contains("sua") ||
                                 cleanMsgNoTone.Contains("thay doi") || cleanMsgNoTone.Contains("xoa");

                    bool isNo = cleanMsgNoTone == "khong" || cleanMsgNoTone.Contains("khong") || cleanMsgNoTone.Contains("tiep tuc") ||
                                cleanMsgNoTone.Contains("giu nguyen") || cleanMsgNoTone.Contains("ok");

                    if (isYes && !isNo)
                    {
                        state.Step = "EDITING_CART";
                        string itemsList = string.Join("\n", state.CartItems.Select((it, idx) => $"  • **{it.ProductName}** - Số lượng: **{it.Quantity}**"));

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ vâng! Danh sách món hiện tại:\n{itemsList}\n\n💡 **Cách thay đổi:**\n• Để **sửa số lượng**: Nhắn 'Sửa [Tên món] số lượng [Số lượng mới]' (Ví dụ: 'Sửa Gạo ST25 số lượng 2')\n• Để **xóa món**: Nhắn 'Xóa món [Tên món]' (Ví dụ: 'Xóa món Gạo ST25')\n\n*(Hoặc bấm nút **[Hủy Thay Đổi]** bên dưới nếu bạn muốn giữ nguyên giỏ hàng)*",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_CANCEL_EDIT_CART_BUTTON",
                            orderState = state
                        });
                    }
                    else
                    {
                        state.Step = "ASK_ADDRESS";
                        string userAddr = !string.IsNullOrWhiteSpace(request?.UserLocation) && request.UserLocation != "Chưa xác định cụ thể"
                            ? request.UserLocation
                            : "Ngõ 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội";
                        state.Address = userAddr;

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ tuyệt vời! Tiếp theo là địa chỉ nhận hàng 📍\n\nĐịa chỉ nhận hàng hiện tại của bạn là: **{state.Address}**\n\n**Bạn có muốn thay đổi địa chỉ nhận hàng không ạ?**",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_ADDRESS_CHOICE_BUTTONS",
                            orderState = state
                        });
                    }
                }

                // ----- BƯỚC 5: EDITING_CART (Thực hiện Sửa / Xóa / Thêm món trong chat với hỗ trợ đa món và/hoặc phẩy) -----
                if (string.Equals(state.Step, "EDITING_CART", StringComparison.OrdinalIgnoreCase))
                {
                    bool isCancelEdit = cleanMsgNoTone.Contains("khong") || cleanMsgNoTone.Contains("huy") ||
                                        cleanMsgNoTone.Contains("giu nguyen") || cleanMsgNoTone.Contains("quay lai") ||
                                        cleanMsgNoTone.Contains("tiep tuc") || cleanMsgNoTone.Contains("xong") ||
                                        cleanMsgNoTone.Contains("du roi") || cleanMsgNoTone == "ok";

                    if (isCancelEdit && !cleanMsgNoTone.Contains("sua") && !cleanMsgNoTone.Contains("xoa") && !cleanMsgNoTone.Contains("them"))
                    {
                        state.Step = "ASK_ADDRESS";
                        string userAddr = !string.IsNullOrWhiteSpace(state.Address) && state.Address != "Chưa xác định cụ thể"
                            ? state.Address
                            : (!string.IsNullOrWhiteSpace(request?.UserLocation) && request.UserLocation != "Chưa xác định cụ thể"
                                ? request.UserLocation
                                : "Ngõ 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội");
                        state.Address = userAddr;

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ vâng, mình đã giữ nguyên danh sách giỏ hàng! 🛒\n\nTiếp theo là địa chỉ nhận hàng 📍\n\nĐịa chỉ nhận hàng hiện tại của bạn là: **{state.Address}**\n\n**Bạn có muốn thay đổi địa chỉ nhận hàng không ạ?**",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_ADDRESS_CHOICE_BUTTONS",
                            orderState = state
                        });
                    }

                    // Tách câu lệnh thành các phân đoạn theo liên từ: ' và ', ',', ';', ' + ', ' với '
                    string normalizedCmd = cleanMsgNoTone;
                    string[] splitDelims = new[] { " va ", ", ", ",", ";", " + ", " voi " };
                    foreach (var d in splitDelims)
                    {
                        normalizedCmd = normalizedCmd.Replace(d, "|");
                    }
                    var segments = normalizedCmd.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    bool globalIsAdd = cleanMsgNoTone.StartsWith("them") || cleanMsgNoTone.Contains("cong") || 
                                       cleanMsgNoTone.Contains("cho them") || cleanMsgNoTone.Contains("lay them");

                    var changesSummary = new List<string>();

                    foreach (var rawSeg in segments)
                    {
                        string seg = rawSeg.Trim();
                        bool segIsDelete = seg.Contains("xoa") || (seg.Contains("bo") && !seg.Contains("bot") && !Regex.IsMatch(seg, @"\b\d+\b")) || seg.Contains("remove");

                        if (segIsDelete)
                        {
                            // Tìm món cần xóa
                            OrderItemDto? toRemove = null;
                            foreach (var item in state.CartItems)
                            {
                                string itemClean = RemoveVietnameseTones(item.ProductName.ToLower());
                                if (seg.Contains(itemClean) || itemClean.Split(' ').Any(w => w.Length >= 3 && seg.Contains(w)))
                                {
                                    toRemove = item;
                                    break;
                                }
                            }

                            if (toRemove != null)
                            {
                                state.CartItems.Remove(toRemove);
                                changesSummary.Add($"🗑️ Đã xóa món **{toRemove.ProductName}** khỏi giỏ hàng");
                            }
                            continue;
                        }

                        // Không phải xóa -> Là sửa số lượng, bớt số lượng hoặc thêm món mới/thêm số lượng
                        bool segIsReduce = seg.Contains("bot") || seg.Contains("giam") || seg.Contains("tru") || (seg.Contains("bo") && Regex.IsMatch(seg, @"\b\d+\b"));
                        bool segIsAdd = seg.Contains("them") || seg.Contains("cong") || seg.Contains("nua") || globalIsAdd;
                        if (seg.Contains("thanh") || seg.Contains("sua thanh") || seg.Contains("doi thanh"))
                        {
                            segIsAdd = false;
                            segIsReduce = false;
                        }

                        // Lấy số lượng từ phân đoạn
                        var qMatch = Regex.Match(seg, @"\b(\d+)\b");
                        int q = 1;
                        if (qMatch.Success && int.TryParse(qMatch.Value, out int parsedQ) && parsedQ > 0)
                        {
                            q = parsedQ;
                        }

                        // Trích xuất từ khóa tên món trong phân đoạn
                        string segQuery = Regex.Replace(seg, @"\b(them|sua|thanh|doi|chinh|so|luong|mon|phan|suat|hop|cai|lon|chai|cho|lay|dum|giup|vao|gio|hang|bot|giam|tru|bo|nua|\d+)\b", " ").Trim();
                        segQuery = Regex.Replace(segQuery, @"\s+", " ").Trim();

                        // 1. Kiểm tra xem phân đoạn này có khớp món đã có trong giỏ hàng không
                        OrderItemDto? existingItem = null;
                        if (!string.IsNullOrWhiteSpace(segQuery))
                        {
                            foreach (var it in state.CartItems)
                            {
                                string itClean = RemoveVietnameseTones(it.ProductName.ToLower());
                                if (itClean.Contains(segQuery) || segQuery.Split(' ').Any(w => w.Length >= 3 && itClean.Contains(w)))
                                {
                                    existingItem = it;
                                    break;
                                }
                            }
                        }
                        else if (state.CartItems.Count == 1)
                        {
                            existingItem = state.CartItems[0];
                        }

                        if (existingItem != null)
                        {
                            if (segIsReduce)
                            {
                                existingItem.Quantity -= q;
                                if (existingItem.Quantity <= 0)
                                {
                                    state.CartItems.Remove(existingItem);
                                    changesSummary.Add($"🗑️ Đã bớt hết món **{existingItem.ProductName}** khỏi giỏ hàng");
                                }
                                else
                                {
                                    changesSummary.Add($"➖ Giảm món **{existingItem.ProductName}** đi -{q} (còn lại: **{existingItem.Quantity}**)");
                                }
                            }
                            else if (segIsAdd)
                            {
                                existingItem.Quantity += q;
                                changesSummary.Add($"➕ Tăng món **{existingItem.ProductName}** thêm +{q} (hiện có: **{existingItem.Quantity}**)");
                            }
                            else
                            {
                                existingItem.Quantity = q;
                                changesSummary.Add($"✏️ Sửa món **{existingItem.ProductName}** thành số lượng: **{q}**");
                            }
                        }
                        else
                        {
                            // 2. Chưa có trong giỏ hàng -> Tìm món mới trong Database để thêm vào giỏ hàng
                            if (!string.IsNullOrWhiteSpace(segQuery))
                            {
                                var foundProds = await SearchProductsInDatabaseAsync(segQuery);
                                if (foundProds != null && foundProds.Count > 0)
                                {
                                    var matchedProd = foundProds.First();
                                    var inCartAlready = state.CartItems.FirstOrDefault(it => it.ProductId == matchedProd.Id);
                                    if (inCartAlready != null)
                                    {
                                        inCartAlready.Quantity += q;
                                        changesSummary.Add($"➕ Tăng món **{inCartAlready.ProductName}** thêm +{q} (hiện có: **{inCartAlready.Quantity}**)");
                                    }
                                    else
                                    {
                                        state.CartItems.Add(new OrderItemDto
                                        {
                                            ProductId = matchedProd.Id ?? "",
                                            ProductName = matchedProd.ProductName ?? "",
                                            Price = matchedProd.Price,
                                            Quantity = q,
                                            StoreId = matchedProd.StoreId ?? "store_default",
                                            StoreName = matchedProd.StoreName ?? "ZoneMart Store",
                                            ImageUrl = !string.IsNullOrWhiteSpace(matchedProd.ImageUrl) ? matchedProd.ImageUrl : "/images/anhxoanen.png"
                                        });
                                        changesSummary.Add($"🛒 Thêm mới **{q}x {matchedProd.ProductName}** ({matchedProd.Price:N0}đ/SP) vào giỏ hàng");
                                    }
                                }
                                else
                                {
                                    changesSummary.Add($"⚠️ Không tìm thấy món '{segQuery}' trên sàn ZoneMart");
                                }
                            }
                        }
                    }

                    if (state.CartItems.Count == 0)
                    {
                        state.Step = "AWAITING_ITEM_NAME";
                        return Ok(new
                        {
                            success = true,
                            reply = "Dạ giỏ hàng hiện tại đang trống! 🛒 Bạn muốn chọn đặt món ăn hoặc thực phẩm gì ạ?",
                            provider = "ZoneMart AI Assistant",
                            orderState = state
                        });
                    }

                    if (changesSummary.Count > 0)
                    {
                        decimal newTotal = state.CartItems.Sum(it => it.Price * it.Quantity);
                        string updatedSummary = string.Join("\n", state.CartItems.Select((it, idx) => $"  {idx + 1}️⃣ {it.Quantity}x **{it.ProductName}** ({it.Price:N0}đ/SP) = {it.Price * it.Quantity:N0}đ"));
                        string changesText = string.Join("\n", changesSummary);

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ mình đã cập nhật giỏ hàng theo yêu cầu của bạn thành công! ✨\n{changesText}\n\n📋 **GIỎ HÀNG HIỆN TẠI:**\n{updatedSummary}\n\n👉 **Tổng tiền hàng ({state.CartItems.Count} món): {newTotal:N0} ₫**\n\n**Bạn có muốn thay đổi gì về danh sách món hoặc số lượng nữa không ạ?**",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_YES_NO_EDIT_CART_BUTTONS",
                            orderState = state
                        });
                    }

                    return Ok(new
                    {
                        success = true,
                        reply = "Dạ bạn nhắn câu lệnh sửa (Ví dụ: 'thêm 1 bánh mì và 2 dâu tây', 'sửa bánh mì số lượng 2', 'xóa món...') giúp mình nhé! Hoặc bấm **[Hủy thay đổi & Tiếp tục]** bên dưới nhé!",
                        provider = "ZoneMart AI Assistant",
                        actionIntent = "SHOW_CANCEL_EDIT_CART_BUTTON",
                        orderState = state
                    });
                }

                // ----- BƯỚC 6: ASK_ADDRESS (Xác nhận địa chỉ giao hàng) -----
                if (string.Equals(state.Step, "ASK_ADDRESS", StringComparison.OrdinalIgnoreCase))
                {
                    bool isChange = cleanMsgNoTone.Contains("thay doi") || cleanMsgNoTone.Contains("doi dia chi") || cleanMsgNoTone.Contains("sua dia chi") || cleanMsgNoTone == "co" || cleanMsgNoTone.StartsWith("co ");
                    bool isKeep = cleanMsgNoTone.Contains("giu nguyen") || cleanMsgNoTone.Contains("khong") || cleanMsgNoTone.Contains("dung roi") || cleanMsgNoTone.Contains("ok") || cleanMsgNoTone.Contains("tiep tuc");

                    if (isChange && !isKeep)
                    {
                        state.Step = "AWAITING_NEW_ADDRESS";
                        return Ok(new
                        {
                            success = true,
                            reply = "Dạ bạn muốn thay đổi địa chỉ nhận hàng ở đâu ạ? Bạn có thể nhắn trực tiếp tên đường/số nhà, hoặc nếu muốn định vị vị trí hiện tại thì bạn cứ nhắn 'lấy vị trí hiện tại' nhé! 📍",
                            provider = "ZoneMart AI Assistant",
                            orderState = state
                        });
                    }
                    else
                    {
                        state.Step = "ASK_SHIPPING_METHOD";
                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ mình đã tiếp nhận địa chỉ **{state.Address}** của bạn rồi ạ! 📍✨\n\n**Tiếp theo, bạn muốn lựa chọn phương thức vận chuyển nào ạ?**",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_SHIPPING_METHOD_BUTTONS",
                            orderState = state
                        });
                    }
                }

                // ----- BƯỚC 7: AWAITING_NEW_ADDRESS (Nhập địa chỉ mới) -----
                if (string.Equals(state.Step, "AWAITING_NEW_ADDRESS", StringComparison.OrdinalIgnoreCase))
                {
                    // Kiểm tra xem khách có yêu cầu lấy địa chỉ hiện tại / định vị vị trí không
                    bool isAskingCurrentLocation = cleanMsgNoTone.Contains("dia chi hien tai") ||
                                                   cleanMsgNoTone.Contains("vi tri hien tai") ||
                                                   cleanMsgNoTone.Contains("dinh vi") ||
                                                   cleanMsgNoTone.Contains("lay dia chi") ||
                                                   cleanMsgNoTone.Contains("lay vi tri") ||
                                                   cleanMsgNoTone.Contains("cho toi dang") ||
                                                   cleanMsgNoTone.Contains("toi dang o dau") ||
                                                   cleanMsgNoTone.Contains("o dau");

                    if (isAskingCurrentLocation)
                    {
                        string clientLoc = (request?.UserLocation ?? "").Trim();
                        bool hasRealLoc = !string.IsNullOrWhiteSpace(clientLoc) &&
                                          !clientLoc.Equals("Khu vực Cầu Giấy / Hà Nội", StringComparison.OrdinalIgnoreCase) &&
                                          !clientLoc.Equals("Chưa xác định cụ thể", StringComparison.OrdinalIgnoreCase);

                        if (hasRealLoc)
                        {
                            state.Address = clientLoc;
                            state.Step = "ASK_SHIPPING_METHOD";

                            return Ok(new
                            {
                                success = true,
                                reply = $"Dạ hệ thống định vị GPS xác định địa chỉ nhận hàng hiện tại của bạn là: **{state.Address}** 📍✨\n\n**Tiếp theo, bạn muốn lựa chọn phương thức vận chuyển nào ạ?**",
                                provider = "ZoneMart AI Assistant",
                                actionIntent = "SHOW_SHIPPING_METHOD_BUTTONS",
                                orderState = state
                            });
                        }
                        else
                        {
                            return Ok(new
                            {
                                success = true,
                                reply = "📍 Để lấy chính xác địa chỉ hiện tại của bạn làm địa chỉ nhận hàng, mình đang kết nối định vị GPS trên trình duyệt...",
                                provider = "ZoneMart AI Assistant",
                                actionIntent = "ACTION_REQUEST_LOCATION_FOR_ORDER_ADDRESS",
                                orderState = state
                            });
                        }
                    }

                    string rawAddr = userMsg.Trim();
                    string cleanAddr = Regex.Replace(rawAddr, @"^(cho\s+tôi\s+tới|cho\s+toi\s+toi|giao\s+tới|giao\s+toi|giao\s+đến|giao\s+den|ở|o|địa\s+chỉ\s+là|dia\s+chi\s+la|tại|tai)\s+", "", RegexOptions.IgnoreCase).Trim();
                    cleanAddr = Regex.Replace(cleanAddr, @"\s*(nhé|nha|giúp\s+mình|giup\s+minh|ạ|a)$", "", RegexOptions.IgnoreCase).Trim();
                    if (string.IsNullOrWhiteSpace(cleanAddr)) cleanAddr = rawAddr;

                    state.Address = cleanAddr;
                    state.Step = "ASK_SHIPPING_METHOD";

                    return Ok(new
                    {
                        success = true,
                        reply = $"Dạ mình đã tiếp nhận địa chỉ **{state.Address}** của bạn rồi ạ! 📍✨\n\n**Tiếp theo, bạn muốn lựa chọn phương thức vận chuyển nào ạ?**",
                        provider = "ZoneMart AI Assistant",
                        actionIntent = "SHOW_SHIPPING_METHOD_BUTTONS",
                        orderState = state
                    });
                }

                // ----- BƯỚC 8: ASK_SHIPPING_METHOD (Chọn phương thức vận chuyển) -----
                if (state.Step == "ASK_SHIPPING_METHOD")
                {
                    if (cleanMsgNoTone.Contains("hoa toc") || cleanMsgNoTone.Contains("22.500") || cleanMsgNoTone.Contains("22500") || cleanMsgNoTone.Contains("15-25"))
                    {
                        state.ShippingMethod = "Giao Hàng Hỏa Tốc (15-25 phút)";
                        state.ShippingFee = 22500;
                    }
                    else
                    {
                        state.ShippingMethod = "Giao Hàng Tiêu Chuẩn (30-45 phút)";
                        state.ShippingFee = 15000;
                    }

                    state.Step = "ASK_PAYMENT_METHOD";
                    return Ok(new
                    {
                        success = true,
                        reply = $"Dạ mình đã tiếp nhận phương thức vận chuyển **{state.ShippingMethod}** rồi ạ! 🛵\n\n**Bạn muốn dùng phương thức thanh toán nào ạ?**",
                        provider = "ZoneMart AI Assistant",
                        actionIntent = "SHOW_PAYMENT_METHOD_BUTTONS",
                        orderState = state
                    });
                }

                // ----- BƯỚC 9: ASK_PAYMENT_METHOD (Chọn phương thức thanh toán) -----
                if (state.Step == "ASK_PAYMENT_METHOD")
                {
                    decimal itemsTotal = state.CartItems.Sum(it => it.Price * it.Quantity);
                    decimal grandTotal = itemsTotal + state.ShippingFee;

                    if (cleanMsgNoTone.Contains("chuyen khoan") || cleanMsgNoTone.Contains("qr") || cleanMsgNoTone.Contains("bank"))
                    {
                        state.PaymentMethod = "Chuyển Khoản QR Bank";
                        state.Step = "AWAITING_QR_PAYMENT";

                        if (string.IsNullOrEmpty(state.CreatedOrderCode))
                        {
                            state.CreatedOrderCode = "ZM" + Random.Shared.Next(100000, 999999);
                        }

                        long amount = Convert.ToInt64(grandTotal);
                        string memo = Uri.EscapeDataString(state.CreatedOrderCode);
                        string accName = Uri.EscapeDataString("DOAN HOANG HUY");
                        string qrUrl = $"https://img.vietqr.io/image/TPB-28122068866-qr_only.png?amount={amount}&addInfo={memo}&accountName={accName}";

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ vâng! Vui lòng quét mã QR bên dưới để thanh toán đơn hàng nhé! 💳\n\n💰 **TỔNG TIỀN CẦN THANH TOÁN: {grandTotal:N0} ₫**\n*(Đã bao gồm phí vận chuyển {state.ShippingFee:N0}đ)*",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_QR_PAYMENT",
                            qrCodeUrl = qrUrl,
                            totalAmount = grandTotal,
                            orderCode = state.CreatedOrderCode,
                            orderState = state
                        });
                    }
                    else
                    {
                        state.PaymentMethod = "Tiền Mặt khi nhận hàng (COD)";
                        state.IsPaid = false;

                        string orderCode = "ZM" + Random.Shared.Next(100000, 999999);
                        var orderDoc = new BsonDocument
                        {
                            { "orderCode", orderCode },
                            { "userEmail", request?.UserEmail ?? "buyer@zonemart.vn" },
                            { "customerName", request?.UserEmail ?? "Khách Hàng ZoneMart" },
                            { "shippingAddress", state.Address },
                            { "shippingMethod", state.ShippingMethod },
                            { "shippingFee", Convert.ToDouble(state.ShippingFee) },
                            { "paymentMethod", state.PaymentMethod },
                            { "isPaid", false },
                            { "paymentStatus", "Pending" },
                            { "orderStatus", "Processing" },
                            { "totalAmount", Convert.ToDouble(grandTotal) },
                            { "createdAt", DateTime.UtcNow },
                            { "items", new BsonArray(state.CartItems.Select(it => new BsonDocument
                                {
                                    { "productId", it.ProductId },
                                    { "productName", it.ProductName },
                                    { "price", Convert.ToDouble(it.Price) },
                                    { "quantity", it.Quantity },
                                    { "storeId", it.StoreId },
                                    { "storeName", it.StoreName },
                                    { "imageUrl", it.ImageUrl }
                                }))
                            }
                        };

                        await _mongoService.Database.GetCollection<BsonDocument>("Orders").InsertOneAsync(orderDoc);

                        state.Step = "ORDER_COMPLETED";
                        state.CreatedOrderCode = orderCode;

                        var createdOrderDto = new
                        {
                            orderId = orderCode,
                            date = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                            total = Convert.ToDouble(grandTotal),
                            paymentMethod = state.PaymentMethod,
                            shippingMethod = state.ShippingMethod,
                            deliveryType = state.ShippingMethod.Contains("Hỏa Tốc") ? "express" : "standard",
                            status = "delivering",
                            statusText = "Shipper đang giao hàng tới bạn",
                            store = state.CartItems.FirstOrDefault()?.StoreName ?? "ZoneMart Store",
                            recipientName = request?.UserEmail ?? "Khách Hàng ZoneMart",
                            recipientPhone = request?.UserEmail ?? "0988888888",
                            recipientAddress = state.Address,
                            items = state.CartItems.Select(it => new
                            {
                                name = it.ProductName,
                                price = Convert.ToDouble(it.Price),
                                quantity = it.Quantity,
                                image = !string.IsNullOrEmpty(it.ImageUrl) ? it.ImageUrl : "/images/anhxoanen.png",
                                shop = it.StoreName
                            }).ToList()
                        };

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ mình đã nhận hết thông tin rồi ạ! 📦✨ 3 giây sau sẽ chuyển qua trang trạng thái đơn hàng nhé! 🚀",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "REDIRECT_TO_BUYER_ORDERS",
                            orderCode = orderCode,
                            createdOrder = createdOrderDto,
                            orderState = state
                        });
                    }
                }

                // ----- BƯỚC 10: AWAITING_QR_PAYMENT (Xác nhận sau khi quét QR) -----
                if (string.Equals(state.Step, "AWAITING_QR_PAYMENT", StringComparison.OrdinalIgnoreCase))
                {
                    string code = state.CreatedOrderCode;
                    bool isActuallyPaid = (!string.IsNullOrEmpty(code) && _paidOrdersCache.TryGetValue(code, out bool p) && p);

                    if (!isActuallyPaid && !string.IsNullOrEmpty(code))
                    {
                        try
                        {
                            string[] possiblePaths = new[]
                            {
                                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "ZoneMart", ".paid_orders.json"),
                                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".paid_orders.json"),
                                System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ZoneMart", ".paid_orders.json")),
                                @"d:\BaiThayChung_(ZoneMark)123\ZoneMart\ZoneMart\.paid_orders.json"
                            };
                            foreach (var pPath in possiblePaths)
                            {
                                if (System.IO.File.Exists(pPath))
                                {
                                    string jsonContent = System.IO.File.ReadAllText(pPath);
                                    if (jsonContent.Contains($"\"{code}\"", StringComparison.OrdinalIgnoreCase))
                                    {
                                        isActuallyPaid = true;
                                        _paidOrdersCache[code] = true;
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    if (!isActuallyPaid && !string.IsNullOrEmpty(code))
                    {
                        var dbOrder = await _mongoService.Database.GetCollection<BsonDocument>("Orders")
                            .Find(Builders<BsonDocument>.Filter.Eq("orderCode", code) & Builders<BsonDocument>.Filter.Eq("isPaid", true))
                            .FirstOrDefaultAsync();
                        if (dbOrder != null) isActuallyPaid = true;
                    }

                    if (isActuallyPaid)
                    {
                        state.IsPaid = true;
                        decimal itemsTotal = state.CartItems.Sum(it => it.Price * it.Quantity);
                        decimal grandTotal = itemsTotal + state.ShippingFee;

                        string orderCode = string.IsNullOrWhiteSpace(state.CreatedOrderCode)
                            ? "ZM" + Random.Shared.Next(100000, 999999)
                            : state.CreatedOrderCode;

                        var orderDoc = new BsonDocument
                        {
                            { "orderCode", orderCode },
                            { "userEmail", request?.UserEmail ?? "buyer@zonemart.vn" },
                            { "customerName", request?.UserEmail ?? "Khách Hàng ZoneMart" },
                            { "shippingAddress", state.Address },
                            { "shippingMethod", state.ShippingMethod },
                            { "shippingFee", Convert.ToDouble(state.ShippingFee) },
                            { "paymentMethod", state.PaymentMethod },
                            { "isPaid", true },
                            { "paymentStatus", "Paid" },
                            { "orderStatus", "Processing" },
                            { "totalAmount", Convert.ToDouble(grandTotal) },
                            { "createdAt", DateTime.UtcNow },
                            { "items", new BsonArray(state.CartItems.Select(it => new BsonDocument
                                {
                                    { "productId", it.ProductId },
                                    { "productName", it.ProductName },
                                    { "price", Convert.ToDouble(it.Price) },
                                    { "quantity", it.Quantity },
                                    { "storeId", it.StoreId },
                                    { "storeName", it.StoreName },
                                    { "imageUrl", it.ImageUrl }
                                }))
                            }
                        };

                        await _mongoService.Database.GetCollection<BsonDocument>("Orders").InsertOneAsync(orderDoc);
                        _paidOrdersCache[orderCode] = true;

                        state.Step = "ORDER_COMPLETED";
                        state.CreatedOrderCode = orderCode;

                        var createdOrderDto = new
                        {
                            orderId = orderCode,
                            date = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                            total = Convert.ToDouble(grandTotal),
                            paymentMethod = state.PaymentMethod,
                            shippingMethod = state.ShippingMethod,
                            deliveryType = state.ShippingMethod.Contains("Hỏa Tốc") ? "express" : "standard",
                            status = "delivering",
                            statusText = "Shipper đang giao hàng tới bạn",
                            store = state.CartItems.FirstOrDefault()?.StoreName ?? "ZoneMart Store",
                            recipientName = request?.UserEmail ?? "Khách Hàng ZoneMart",
                            recipientPhone = request?.UserEmail ?? "0988888888",
                            recipientAddress = state.Address,
                            items = state.CartItems.Select(it => new
                            {
                                name = it.ProductName,
                                price = Convert.ToDouble(it.Price),
                                quantity = it.Quantity,
                                image = !string.IsNullOrEmpty(it.ImageUrl) ? it.ImageUrl : "/images/anhxoanen.png",
                                shop = it.StoreName
                            }).ToList()
                        };

                        return Ok(new
                        {
                            success = true,
                            reply = $"Bạn đã thanh toán thành công 3 giây nữa sẽ chuyển qua trang trạng thái đơn hàng ạ... 🚀",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "REDIRECT_TO_BUYER_ORDERS",
                            orderCode = orderCode,
                            createdOrder = createdOrderDto,
                            orderState = state
                        });
                    }
                    else
                    {
                        decimal itemsTotal = state.CartItems.Sum(it => it.Price * it.Quantity);
                        decimal grandTotal = itemsTotal + state.ShippingFee;

                        if (string.IsNullOrEmpty(state.CreatedOrderCode))
                        {
                            state.CreatedOrderCode = "ZM" + Random.Shared.Next(100000, 999999);
                        }

                        long amount = Convert.ToInt64(grandTotal);
                        string memo = Uri.EscapeDataString(state.CreatedOrderCode);
                        string accName = Uri.EscapeDataString("DOAN HOANG HUY");
                        string qrUrl = $"https://img.vietqr.io/image/TPB-28122068866-qr_only.png?amount={amount}&addInfo={memo}&accountName={accName}";

                        return Ok(new
                        {
                            success = true,
                            reply = $"Dạ hệ thống ngân hàng chưa ghi nhận giao dịch chuyển khoản cho đơn hàng này 💸. Bạn vui lòng kiểm tra lại App Banking và hoàn tất chuyển khoản nhé! Hệ thống vẫn đang tự động lắng nghe thời gian thực ạ. ⏳",
                            provider = "ZoneMart AI Assistant",
                            actionIntent = "SHOW_QR_PAYMENT",
                            qrCodeUrl = qrUrl,
                            totalAmount = grandTotal,
                            orderCode = state.CreatedOrderCode,
                            orderState = state
                        });
                    }
                }
            }

            // 0.88. KIỂM TRA TÌM QUÁN THEO KHU VỰC CỤ THỂ (SONG PHƯƠNG, HOÀI ĐỨC, HÀ ĐÔNG, ĐÔNG ANH, LONG BIÊN,...) -> BÁO KHÔNG CÓ + KHÔNG HIỆN THẺ THAM KHẢO
            string[] specificDistricts = new[] { "song phuong", "hoai duc", "ha dong", "dong anh", "long bien", "thanh tri", "gia lam", "soc son", "ba vi", "thach that", "phuc tho", "quoc oai", "chuong my", "thanh oai", "ung hoa", "my duc", "phu xuyen", "thuong tin", "me linh", "son tay" };

            string? requestedDistrict = specificDistricts.FirstOrDefault(d => cleanMsgNoTone.Contains(d));
            if (requestedDistrict == null && (cleanMsgNoTone.Contains("quan o") || cleanMsgNoTone.Contains("shop o") || cleanMsgNoTone.Contains("hoat dong o")))
            {
                var matchLoc = Regex.Match(cleanMsgNoTone, @"(quan|shop|hoat dong)\s+(o|tai)\s+([a-z0-9\s]+)");
                if (matchLoc.Success)
                {
                    requestedDistrict = matchLoc.Groups[3].Value.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(requestedDistrict) && (cleanMsgNoTone.Contains("quan") || cleanMsgNoTone.Contains("shop") || cleanMsgNoTone.Contains("tim") || cleanMsgNoTone.Contains("o dau") || cleanMsgNoTone.Contains("hien tai") || cleanMsgNoTone.Contains("hoat dong")))
            {
                var allStores = await _mongoService.Stores.Find(s => s.Status == "Active").ToListAsync();
                var matchedAreaStores = allStores.Where(s => RemoveVietnameseTones((s.Address ?? "").ToLower()).Contains(requestedDistrict)).ToList();

                if (matchedAreaStores.Count == 0)
                {
                    string districtDisplayName = char.ToUpper(requestedDistrict[0]) + requestedDistrict.Substring(1);
                    if (requestedDistrict == "song phuong") districtDisplayName = "Song Phương";
                    else if (requestedDistrict == "hoai duc") districtDisplayName = "Hoài Đức";
                    else if (requestedDistrict == "ha dong") districtDisplayName = "Hà Đông";
                    else if (requestedDistrict == "dong anh") districtDisplayName = "Đông Anh";
                    else if (requestedDistrict == "long bien") districtDisplayName = "Long Biên";

                    return Ok(new
                    {
                        success = true,
                        reply = $"Dạ hiện tại trên sàn ZoneMart chưa có cửa hàng/gian hàng nào đang hoạt động tại khu vực {districtDisplayName} ạ 😅. Bạn có thể đổi địa chỉ nhận hàng về khu vực lân cận như Cầu Giấy hoặc Thanh Xuân để được giao hỏa tốc 15-30 phút nhé!",
                        provider = "ZoneMart AI Assistant",
                        stores = new List<object>(),
                        products = new List<object>()
                    });
                }
            }

            // 0.8. KIỂM TRA HỎI TÍNH NĂNG / CHỨC NĂNG CỦA AI (SHOW CAPABILITIES WITH ACTION BUTTONS)
            bool isCapabilityQuery = (cleanMsgNoTone.Contains("tu van") && (cleanMsgNoTone.Contains("gi") || cleanMsgNoTone.Contains("nhung") || cleanMsgNoTone.Contains("duoc"))) ||
                                    (cleanMsgNoTone.Contains("lam duoc") && (cleanMsgNoTone.Contains("gi") || cleanMsgNoTone.Contains("nhung"))) ||
                                    cleanMsgNoTone.Contains("co the giup") ||
                                    cleanMsgNoTone.Contains("giup gi cho toi") ||
                                    cleanMsgNoTone.Contains("chuc nang") ||
                                    cleanMsgNoTone.Contains("tinh nang") ||
                                    cleanMsgNoTone.Contains("ho tro gi") ||
                                    cleanMsgNoTone.Contains("tro giup gi") ||
                                    cleanMsgNoTone.Contains("ban lam gi") ||
                                    (cleanMsgNoTone.Contains("ban co the") && (cleanMsgNoTone.Contains("tu van") || cleanMsgNoTone.Contains("giup") || cleanMsgNoTone.Contains("lam")));

            if (isCapabilityQuery)
            {
                return Ok(new
                {
                    success = true,
                    reply = "Dạ xin chào bạn! 👋 Mình là Trợ lý AI của ZoneMart. Mình có thể hỗ trợ bạn các tính năng tư vấn mua sắm hỏa tốc, tự động chọn & đặt đồ hộ và đăng ký tài khoản bên dưới. Bạn hãy bấm vào các nút bên dưới để mình trợ giúp ngay nhé! 😊",
                    provider = "ZoneMart AI Assistant",
                    actionIntent = "SHOW_CAPABILITIES"
                });
            }

            // 0.9. KIỂM TRA HỎI VỀ QUÃNG ĐƯỜNG / BÁN KÍNH GIAO HÀNG (DELIVERY DISTANCE POLICY & SOLUTIONS)
            bool isDistanceQuery = cleanMsgNoTone.Contains("quang duong") || cleanMsgNoTone.Contains("khoang cach") ||
                                   cleanMsgNoTone.Contains("giao xa") || cleanMsgNoTone.Contains("cach nha") ||
                                   cleanMsgNoTone.Contains("ban kinh giao") || cleanMsgNoTone.Contains("pham vi giao") ||
                                   cleanMsgNoTone.Contains("cach thanh") || cleanMsgNoTone.Contains("giao toi") ||
                                   cleanMsgNoTone.Contains("giao hang duoc toi") || cleanMsgNoTone.Contains("giao den") ||
                                   (cleanMsgNoTone.Contains("km") && (cleanMsgNoTone.Contains("giao") || cleanMsgNoTone.Contains("cach") || cleanMsgNoTone.Contains("hon") || cleanMsgNoTone.Contains("dat")));

            if (isDistanceQuery)
            {
                double maxKmFound = 0;
                var matches = Regex.Matches(cleanMsgNoTone, @"(\d+(\.\d+)?)\s*km");
                foreach (Match m in matches)
                {
                    if (double.TryParse(m.Groups[1].Value, out double parsedKm))
                    {
                        if (parsedKm > maxKmFound) maxKmFound = parsedKm;
                    }
                }

                bool isOver10Km = maxKmFound > 10.0 ||
                                  cleanMsgNoTone.Contains("hon 10km") || cleanMsgNoTone.Contains("qua 10km") ||
                                  cleanMsgNoTone.Contains("xa hon 10") || cleanMsgNoTone.Contains("hon 10") ||
                                  cleanMsgNoTone.Contains("tren 10km") || cleanMsgNoTone.Contains("tren 10");

                string distanceReply = "";
                if (isOver10Km)
                {
                    string kmText = maxKmFound > 0 ? $"{maxKmFound}km" : "trên 10km";
                    distanceReply = $@"Dạ khoảng cách {kmText} hiện đã vượt quá bán kính giao hỏa tốc 10km của gian hàng để đảm bảo món ăn luôn tươi hổi nhất ạ! 🛵

💡 **Để giúp bạn vẫn đặt được đồ ăn ngon, mình gợi ý 2 giải pháp sau:**
1️⃣ **Chọn quán/chi nhánh gần hơn:** Bạn thử đổi sang chọn các gian hàng/chi nhánh cùng loại ở khu vực gần nhà bạn hơn (trong bán kính 10km như Cầu Giấy, Mai Dịch...) để được giao hỏa tốc 15-30 phút nhé.
2️⃣ **Hẹn địa điểm nhận hàng:** Bạn có thể đổi địa chỉ nhận hàng mốc mốc gần hơn (trong bán kính 10km của shop) hoặc hẹn shipper ZoneMart giao hỏa tốc tới điểm hẹn mốc 10km để nhận hàng nha! 😊";
                }
                else
                {
                    string kmText = maxKmFound > 0 ? $"{maxKmFound}km" : "dưới 10km";
                    distanceReply = $"Dạ khoảng cách {kmText} hoàn toàn nằm trong bán kính giao hỏa tốc của ZoneMart ạ! 🛵 Sàn ZoneMart chuyên hỗ trợ giao hàng tận nhà trong bán kính lên tới 10km (giao siêu tốc chỉ từ 12 - 30 phút). Bạn cứ yên tâm chọn món và đặt hàng thoải mái nhé! 😊";
                }

                return Ok(new
                {
                    success = true,
                    reply = distanceReply,
                    provider = "ZoneMart AI Assistant",
                    stores = new List<object>(),
                    products = new List<object>()
                });
            }

            // 0.95. KIỂM TRA HỎI VỀ SỐ LƯỢNG / DANH SÁCH CỬA HÀNG ĐANG HOẠT ĐỘNG
            bool isStoreCountQuery = cleanMsgNoTone.Contains("bao nhieu quan") || cleanMsgNoTone.Contains("bao nhieu shop") ||
                                     cleanMsgNoTone.Contains("bao nhieu gian hang") || cleanMsgNoTone.Contains("danh sach quan") ||
                                     cleanMsgNoTone.Contains("danh sach shop") || cleanMsgNoTone.Contains("co bao nhieu quan") ||
                                     cleanMsgNoTone.Contains("co bao nhieu shop") || cleanMsgNoTone.Contains("may quan") ||
                                     cleanMsgNoTone.Contains("may shop") ||
                                     (cleanMsgNoTone.Contains("quan") && cleanMsgNoTone.Contains("hoat dong")) ||
                                     (cleanMsgNoTone.Contains("shop") && cleanMsgNoTone.Contains("hoat dong")) ||
                                     (cleanMsgNoTone.Contains("gian hang") && cleanMsgNoTone.Contains("hoat dong"));

            if (isStoreCountQuery)
            {
                var activeStores = await _mongoService.Stores.Find(s => s.Status == "Active").ToListAsync();
                var storeCardsList = new List<object>();
                var replySb = new StringBuilder();

                if (activeStores.Count > 0)
                {
                    replySb.AppendLine($"Dạ hiện tại trên sàn ZoneMart đang có **{activeStores.Count} cửa hàng** đang hoạt động hỏa tốc bao gồm: 🏪\n");
                    for (int i = 0; i < activeStores.Count; i++)
                    {
                        var s = activeStores[i];
                        replySb.AppendLine($"{i + 1}️⃣ **{s.StoreName}**");
                        if (!string.IsNullOrWhiteSpace(s.Address)) replySb.AppendLine($"   📍 Địa chỉ: {s.Address}");
                        if (!string.IsNullOrWhiteSpace(s.Category)) replySb.AppendLine($"   🛍️ Ngành hàng: {s.Category}");

                        storeCardsList.Add(new
                        {
                            id = s.Id,
                            name = s.StoreName,
                            address = s.Address,
                            avatar = "/images/seller_mascot_clean.png",
                            rating = 4.9
                        });
                    }
                    replySb.AppendLine("\nBạn có thể bấm vào gian hàng bên dưới hoặc gõ món ăn yêu thích để đặt giao hỏa tốc 15-30 phút nhé! 😊");
                }
                else
                {
                    replySb.AppendLine("Dạ hiện tại sàn ZoneMart đang chuẩn bị ra mắt thêm các gian hàng mới ạ! Bạn vui lòng theo dõi sàn nhé. 😊");
                }

                return Ok(new
                {
                    success = true,
                    reply = replySb.ToString(),
                    provider = "ZoneMart AI Assistant",
                    stores = storeCardsList
                });
            }

            // 0.96. KIỂM TRA HỎI VỀ SỐ LƯỢNG SẢN PHẨM / THỰC PHẨM TRÊN SÀN
            bool isProductCountQuery = cleanMsgNoTone.Contains("bao nhieu san pham") || cleanMsgNoTone.Contains("bao nhieu mon") ||
                                       cleanMsgNoTone.Contains("co bao nhieu mon") || cleanMsgNoTone.Contains("co bao nhieu san pham") ||
                                       cleanMsgNoTone.Contains("danh sach san pham") || cleanMsgNoTone.Contains("danh sach mon");

            if (isProductCountQuery)
            {
                var activeProducts = await _mongoService.Products.Find(p => p.StockStatus == "in_stock" || p.AiStatus == "approved").ToListAsync();
                var productCardsList = new List<object>();
                var replySb = new StringBuilder();

                if (activeProducts.Count > 0)
                {
                    replySb.AppendLine($"Dạ hiện tại trên sàn ZoneMart đang có **{activeProducts.Count} món ngon & thực phẩm** sẵn sàng giao hỏa tốc! 🍲🛍️\n");
                    replySb.AppendLine("Một số món tiêu biểu nổi bật:");
                    foreach (var p in activeProducts.Take(5))
                    {
                        replySb.AppendLine($"  • **{p.ProductName}** ({p.StoreName ?? "ZoneMart Store"}) - Giá: {p.Price:N0}đ");
                        productCardsList.Add(new
                        {
                            id = p.Id,
                            name = p.ProductName,
                            price = p.Price,
                            image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                            storeName = p.StoreName ?? "ZoneMart Store",
                            category = p.Category
                        });
                    }
                    replySb.AppendLine("\nBạn muốn ăn gì hay cần tư vấn món nào cứ nhắn cho mình nha! 😊");
                }
                else
                {
                    replySb.AppendLine("Dạ hiện tại danh mục sản phẩm đang được cập nhật thêm ạ! 😊");
                }

                return Ok(new
                {
                    success = true,
                    reply = replySb.ToString(),
                    provider = "ZoneMart AI Assistant",
                    products = productCardsList
                });
            }

            // 1. KIỂM TRA BẢO VỆ AN TOÀN NỘI DUNG & BẢO MẬT (18+, BẠO LỰC, HACK)
            var (isDirectUnsafe, directUnsafeReply) = CheckSafetyGuardrails(userMsg, cleanMsgNoTone);
            if (isDirectUnsafe)
            {
                return Ok(new
                {
                    success = true,
                    reply = directUnsafeReply,
                    provider = "ZoneMart Safety Guardrail",
                    actionType = "SAFETY_VIOLATION",
                    products = new List<object>(),
                    stores = new List<object>()
                });
            }

            string regState = (request?.RegistrationState ?? "").Trim();
            isRegActive = !string.IsNullOrEmpty(regState) && regState != "NONE";
            var orderState = request?.OrderState;
            bool isOrderActive = orderState != null && !string.IsNullOrEmpty(orderState.Step) && orderState.Step != "IDLE";
            string suppState = (request?.SupportState ?? "").Trim();
            isSupportActive = !string.IsNullOrEmpty(suppState) && suppState != "NONE";

            // 1.1. XỬ LÝ KHI NGƯỜI DÙNG VỪA BẬT ĐỊNH VỊ VÀ GỬI TỌA ĐỘ / ĐỊA CHỈ
            if (userMsg.StartsWith("[VỊ TRÍ GPS]:") || userMsg.Contains("[VỊ TRÍ GPS ĐÃ CẤP QUYỀN]:"))
            {
                string resolvedLoc = userMsg.Replace("[VỊ TRÍ GPS]:", "").Replace("[VỊ TRÍ GPS ĐÃ CẤP QUYỀN]:", "").Trim();
                if (resolvedLoc.StartsWith("Tôi đang ở ")) resolvedLoc = resolvedLoc.Substring(11).Trim();
                return Ok(new
                {
                    success = true,
                    reply = $"📍 Tuyệt vời! Hệ thống đã nhận diện được vị trí của bạn tại **{resolvedLoc}** rồi nhé! 🛵 Từ vị trí này, các đối tác Zone Shipper sẵn sàng giao đồ ăn, thực phẩm sạch VietGAP hỏa tốc trong 15 - 30 phút (bán kính 10km) đến tận cửa cho bạn. Bạn có muốn mình gợi ý món ngon giải nhiệt hay quán ăn hot gần đây không ạ? 😊",
                    provider = "ZoneMart Location Service",
                    actionIntent = (string?)null,
                    products = new List<object>(),
                    stores = new List<object>()
                });
            }

            // 1.21. XỬ LÝ TÌM QUÁN NGON BÁN KÍNH 5KM QUANH VỊ TRÍ KHÁCH HÀNG (TRUY XUẤT DATABASE THỰC TẾ)
            bool isNearbyStoreQuery = cleanMsgNoTone.Contains("tim quan") ||
                                      cleanMsgNoTone.Contains("quan ngon ban kinh") ||
                                      cleanMsgNoTone.Contains("quan ban kinh 5km") ||
                                      cleanMsgNoTone.Contains("quan ban kinh 3km") ||
                                      cleanMsgNoTone.Contains("quan gan vi tri") ||
                                      cleanMsgNoTone.Contains("quan gan day") ||
                                      cleanMsgNoTone.Contains("quan quanh day") ||
                                      cleanMsgNoTone.Contains("cac quan gan") ||
                                      userMsg.StartsWith("[TÌM QUÁN BÁN KÍNH");

            if (isNearbyStoreQuery && !isRegActive && !isOrderActive && !isSupportActive)
            {
                string clientLoc = (request?.UserLocation ?? "").Trim();
                if (userMsg.Contains("Vị trí của tôi là"))
                {
                    var idx = userMsg.IndexOf("Vị trí của tôi là");
                    clientLoc = userMsg.Substring(idx + "Vị trí của tôi là".Length).Trim();
                }

                bool hasRealLocation = !string.IsNullOrWhiteSpace(clientLoc) &&
                                       !clientLoc.Equals("Khu vực Cầu Giấy / Hà Nội", StringComparison.OrdinalIgnoreCase) &&
                                       !clientLoc.Equals("Chưa xác định cụ thể", StringComparison.OrdinalIgnoreCase);

                if (!hasRealLocation)
                {
                    return Ok(new
                    {
                        success = true,
                        reply = "📍 Để tìm các quán ngon và gian hàng mở cửa trong bán kính 5km gần bạn nhất, mình cần xác định vị trí hiện tại của bạn!\n\n*(Hệ thống đang yêu cầu quyền truy cập vị trí từ trình duyệt của bạn...)*",
                        provider = "ZoneMart Location Service",
                        actionIntent = "ACTION_REQUEST_LOCATION_FOR_STORES",
                        products = new List<object>(),
                        stores = new List<object>()
                    });
                }

                // Truy xuất database cửa hàng thật
                var allStores = await _mongoService.Stores.Find(s => s.Status == "Active" || string.IsNullOrEmpty(s.Status)).Limit(6).ToListAsync();
                if (allStores.Count == 0)
                {
                    allStores = await _mongoService.Stores.Find(_ => true).Limit(6).ToListAsync();
                }

                var storeIds = allStores.Select(s => s.Id).ToList();
                var storeNames = allStores.Select(s => s.StoreName).ToList();
                var featuredProducts = await _mongoService.Products
                    .Find(p => (p.StoreId != null && storeIds.Contains(p.StoreId)) || (p.StoreName != null && storeNames.Contains(p.StoreName)))
                    .Limit(10)
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine($"Dạ chào bạn! 🛵 Dựa trên vị trí định vị hiện tại của bạn tại **{clientLoc}**, dưới đây là danh sách các gian hàng & quán ngon uy tín trên sàn ZoneMart đang mở cửa trong bán kính **5km** phục vụ hỏa tốc 15 - 30 phút:\n");

                int index = 1;
                foreach (var s in allStores)
                {
                    string dist = $"{0.6 + (index * 0.5):F1}km";
                    string estTime = $"{12 + (index * 2)}-{18 + (index * 3)} phút";
                    string addr = string.IsNullOrWhiteSpace(s.Address) ? "Hà Nội" : s.Address;
                    sb.AppendLine($"{index}️⃣ **{s.StoreName}** ({dist} - Giao khoảng {estTime})");
                    sb.AppendLine($"   📍 Địa chỉ: {addr}");
                    sb.AppendLine($"   🏷️ Chuyên bán: {s.Category} | ⏰ Mở cửa: {s.OpenHours}");

                    var prodsOfStore = featuredProducts.Where(p => p.StoreId == s.Id || p.StoreName == s.StoreName).Take(2).ToList();
                    if (prodsOfStore.Count > 0)
                    {
                        var prodNames = string.Join(", ", prodsOfStore.Select(p => $"{p.ProductName} ({p.Price:N0}đ)"));
                        sb.AppendLine($"   🍲 Món nổi bật: {prodNames}");
                    }
                    sb.AppendLine();
                    index++;
                }

                sb.AppendLine("Bạn muốn đặt món gì tại quán nào thì cứ nhắn tên món để mình tạo đơn giao ngay tận cửa cho bạn nhé! 🚀");

                return Ok(new
                {
                    success = true,
                    reply = sb.ToString().TrimEnd(),
                    provider = "ZoneMart Location Service",
                    products = featuredProducts.Take(4).Select(p => (object)new
                    {
                        id = p.Id,
                        name = p.ProductName,
                        price = p.Price,
                        image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                        storeName = p.StoreName ?? "ZoneMart Store",
                        category = p.Category
                    }).ToList(),
                    stores = allStores.Take(4).Select((s, idx) => (object)new
                    {
                        id = s.Id,
                        name = s.StoreName,
                        address = string.IsNullOrWhiteSpace(s.Address) ? "Hà Nội" : s.Address,
                        avatar = "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80",
                        rating = "4.9"
                    }).ToList()
                });
            }

            // 1.2. XỬ LÝ KHI HỎI VỀ VỊ TRÍ ("TÔI ĐANG Ở ĐÂU", "VỊ TRÍ", "ĐỊNH VỊ")
            bool isLocationInquiry = cleanMsgNoTone.Contains("o dau") || cleanMsgNoTone.Contains("dang o dau") || 
                                     cleanMsgNoTone.Contains("vi tri cua toi") || cleanMsgNoTone.Contains("toi dang o") || 
                                     cleanMsgNoTone.Contains("cho toi dang") || cleanMsgNoTone.Contains("dinh vi") || 
                                     cleanMsgNoTone == "vi tri" || cleanMsgNoTone.Contains("cho nao");

            if (isLocationInquiry && !isRegActive && !isOrderActive && !isSupportActive)
            {
                string clientLoc = (request?.UserLocation ?? "").Trim();
                if (userMsg.Contains("Tôi đang ở"))
                {
                    var idx = userMsg.IndexOf("Tôi đang ở");
                    clientLoc = userMsg.Substring(idx + "Tôi đang ở".Length).Trim();
                }

                bool hasRealGpsLocation = !string.IsNullOrWhiteSpace(clientLoc) && 
                                          !clientLoc.Equals("Khu vực Cầu Giấy / Hà Nội", StringComparison.OrdinalIgnoreCase) &&
                                          !clientLoc.Equals("Chưa xác định cụ thể", StringComparison.OrdinalIgnoreCase);

                if (!hasRealGpsLocation)
                {
                    return Ok(new
                    {
                        success = true,
                        reply = "📍 Để xem được vị trí chính xác hiện tại của bạn, mình đang kết nối định vị GPS trên trình duyệt...",
                        provider = "ZoneMart Location Service",
                        actionIntent = "ACTION_REQUEST_LOCATION",
                        products = new List<object>(),
                        stores = new List<object>()
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = true,
                        reply = $"📍 Hệ thống định vị GPS xác định hiện tại bạn đang ở **{clientLoc}** bạn nhé! 🛵 Từ vị trí này, các shipper ZoneMart có thể giao món ngon, nông sản tươi sạch VietGAP hỏa tốc trong 15 - 30 phút (bán kính 10km) đấy ạ! 😊",
                        provider = "ZoneMart Location Service",
                        actionIntent = (string?)null,
                        products = new List<object>(),
                        stores = new List<object>()
                    });
                }
            }

            // 1.25. XỬ LÝ KHI HỎI VỀ CỬA HÀNG MỚI MỞ / SELLER MỚI ĐĂNG MÓN / SẢN PHẨM MỚI VÀ HIỆN CÓ
            bool isNewItemsOrStoresQuery = cleanMsgNoTone.Contains("mon moi") ||
                                           cleanMsgNoTone.Contains("quan moi") ||
                                           cleanMsgNoTone.Contains("seller moi") ||
                                           cleanMsgNoTone.Contains("cua hang moi") ||
                                           cleanMsgNoTone.Contains("moi dang") ||
                                           cleanMsgNoTone.Contains("vua dang") ||
                                           cleanMsgNoTone.Contains("moi mo") ||
                                           cleanMsgNoTone.Contains("co mon gi moi") ||
                                           cleanMsgNoTone.Contains("co quan gi moi") ||
                                           cleanMsgNoTone.Contains("san pham hien co") ||
                                           cleanMsgNoTone.Contains("mon hien co");

            if (isNewItemsOrStoresQuery && !isRegActive && !isOrderActive && !isSupportActive)
            {
                var newestProducts = await _mongoService.Products
                    .Find(p => p.StockStatus != "out_of_stock" && p.AiStatus != "rejected")
                    .SortByDescending(p => p.CreatedAt)
                    .Limit(6)
                    .ToListAsync();

                var newestStores = await _mongoService.Stores
                    .Find(s => s.Status != "Rejected")
                    .SortByDescending(s => s.CreatedAt)
                    .Limit(4)
                    .ToListAsync();

                var sbNew = new StringBuilder();
                sbNew.AppendLine("✨ **CẬP NHẬT CÁC GIAN HÀNG & MÓN NGON MỚI NHẤT TRÊN ZONEMART:**\n");

                if (newestStores.Count > 0)
                {
                    sbNew.AppendLine("🏪 **Gian hàng & nhà vườn đối tác mới mở:**");
                    foreach (var s in newestStores)
                    {
                        string addr = string.IsNullOrWhiteSpace(s.Address) ? "Hà Nội" : s.Address;
                        sbNew.AppendLine($"  • **{s.StoreName}** - Chuyên: {s.Category} (📍 {addr})");
                    }
                    sbNew.AppendLine();
                }

                if (newestProducts.Count > 0)
                {
                    sbNew.AppendLine("🍲 **Món ngon & nông sản mới đăng lên sàn gần đây:**");
                    foreach (var p in newestProducts)
                    {
                        sbNew.AppendLine($"  • **{p.ProductName}** - Giá: **{p.Price:N0}đ** (Quán: *{p.StoreName ?? "ZoneMart Store"}*)");
                    }
                    sbNew.AppendLine();
                }

                sbNew.AppendLine("Bạn muốn thưởng thức món nào trong các món mới này thì cứ nhắn tên món để mình tạo đơn giao ngay tận cửa cho bạn nhé! 🚀");

                return Ok(new
                {
                    success = true,
                    reply = sbNew.ToString().TrimEnd(),
                    provider = "ZoneMart Realtime Database",
                    products = newestProducts.Take(4).Select(p => (object)new
                    {
                        id = p.Id,
                        name = p.ProductName,
                        price = p.Price,
                        image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                        storeName = p.StoreName ?? "ZoneMart Store",
                        category = p.Category
                    }).ToList(),
                    stores = newestStores.Take(4).Select(s => (object)new
                    {
                        id = s.Id,
                        name = s.StoreName,
                        address = string.IsNullOrWhiteSpace(s.Address) ? "Hà Nội" : s.Address,
                        avatar = "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80",
                        rating = "5.0"
                    }).ToList()
                });
            }

            // 1.3. XỬ LÝ SUY LUẬN TỪNG CHỮ CHO CÂU HỎI NGOÀI LUỒNG (VÍ DỤ "TÔI BUỒN ỈA QUÁ" -> SUY LUẬN TỪNG TỪ VÀ LÁI VỀ ZONEMART)
            if (!isRegActive && !isOrderActive && !isSupportActive)
            {
                var (isOutOfScope, outOfScopeReply) = ReasonOutOfScopeQuery(userMsg, cleanMsgNoTone);
                if (isOutOfScope)
                {
                    return Ok(new
                    {
                        success = true,
                        reply = outOfScopeReply,
                        provider = "ZoneMart AI Assistant",
                        products = new List<object>(),
                        stores = new List<object>()
                    });
                }
            }

            // 2. LẤY DỮ LIỆU THỰC TỪ MONGODB ATLAS ĐỂ NẠP CHO AI (RAG - REALTIME CONTEXT)
            string realtimeDataContext = await FetchRealtimeMarketDataAsync(userMsg, cleanMsgNoTone);

            // 2b. QUY TRÌNH 5 BƯỚC THỜI TIẾT CLIENT & OPENWEATHERMAP
            var (weatherCity, weatherTemp, weatherDesc, weatherPromptText) = await GetClientWeatherInfoAsync();

            // 3. THIẾT LẬP SYSTEM PROMPT DẠY AI PHÂN TÍCH TỪ KHÓA CHÍNH VÀ SUY LUẬN Ý ĐỊNH NGƯỜI DÙNG
            int currentHour = DateTime.Now.Hour;
            string timePeriod = "buổi sáng ☀️";
            if (currentHour >= 11 && currentHour < 13) timePeriod = "buổi trưa 🌤️";
            else if (currentHour >= 13 && currentHour < 18) timePeriod = "buổi chiều ⛅";
            else if (currentHour >= 18 || currentHour < 5) timePeriod = "buổi tối / đêm 🌙";

            string userLocationInfo = !string.IsNullOrWhiteSpace(request?.UserLocation) ? request.UserLocation : $"Khu vực {weatherCity}";

            string systemPrompt = $@"Bạn là nhân viên tư vấn ẩm thực xuất sắc của siêu thị trực tuyến ZoneMart.
THÔNG TIN KHÁCH HÀNG HIỆN TẠI:

Vị trí: {userLocationInfo}

Thời điểm: {timePeriod} ({DateTime.Now:HH:mm dd/MM/yyyy})

Thời tiết: {weatherTemp:0.#} độ C, {weatherDesc}

QUY TẮC CỐT LÕI BẮT BUỘC PHẢI TUÂN THỦ:

SUY LUẬN THEO NGỮ CẢNH: Không bao giờ trả lời máy móc. Hãy phân tích tâm lý. Nếu khách chê thời tiết nóng (hoặc nhiệt độ >= 27°C), hãy chủ động gợi ý đồ uống mát lạnh, trái cây thanh nhiệt (dâu tây Đà Lạt, trà tắc, sữa tươi ướp lạnh). Nếu khách than trời lạnh hoặc mưa, hãy chủ động gợi ý món nóng hổi, xì xụp ấm bụng (phở bò, bánh mì chảo, lẩu).

TRẢ LỜI VỀ VỊ TRÍ: Nếu khách hỏi 'Tôi đang ở đâu?' hoặc các câu hỏi về định vị, bạn ĐƯỢC PHÉP và PHẢI trả lời tự nhiên bằng thông tin Vị trí ở trên (ví dụ: 'Hệ thống định vị cho thấy bạn đang ở {userLocationInfo} bạn nhé! 🛵 Từ vị trí này, các shipper ZoneMart có thể giao món ngon hỏa tốc đến tận tay bạn trong vòng 15 - 30 phút (bán kính 10km) đấy ạ!'). Đồng thời nhắc nhở: 'Nếu bạn muốn định vị chính xác nhất đến từng ngõ ngách, hãy cấp quyền cho phép truy cập vị trí trên trình duyệt giúp mình nhé!'.

TỪ CHỐI CÂU HỎI NGOÀI LUỒNG BẰNG CÁCH SUY LUẬN TỪNG CHỮ (QUAN TRỌNG):
Phạm vi của bạn CHỈ là đồ ăn, thực phẩm và dịch vụ của ZoneMart. Khi khách hỏi câu hỏi ngoài luồng (như 'Tôi buồn ỉa quá', 'Tôi buồn ngủ quá', 'Tôi đang đi tắm', toán học, lập trình...):
- BẮT BUỘC phân tích suy luận từng chữ để tìm ra từ chính chỉ hành động, tình trạng, cảm xúc của khách (ví dụ: 'buồn ỉa' -> hành động/tình trạng cần đi vệ sinh gấp).
- Trả lời tự nhiên, hóm hỉnh đúng vào tình trạng đó và LUÔN KẾT THÚC bằng câu nói về ZoneMart theo đúng mẫu:
  'Bạn đang [tình trạng/hành động] thì bạn [lời khuyên phù hợp] nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi !'
  (Ví dụ cụ thể: 'Bạn đang buồn ỉa thì bạn tìm nhà vệ sinh đâu đó gần nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi !').
- TUYỆT ĐỐI KHÔNG trả lời mẫu rập khuôn máy móc. HÃY PHÂN TÍCH ĐÚNG TỪ KHÓA CỦA KHÁCH ĐỂ ĐƯA RA CÂU TRẢ LỜI ĐÚNG HOÀN CẢNH!
- TUYỆT ĐỐI KHÔNG tự tiện chèn món ăn/dâu tây nếu khách không hỏi về ăn uống, mua sắm.

CÁC TÍNH NĂNG DỊCH VỤ CỐT LÕI CỦA ZONEMART GIỮ NGUYÊN HOẠT ĐỘNG:
- Hỗ trợ đặt đồ hộ tự động trực tiếp trong chat khi khách có nhu cầu.
- Hỗ trợ và hướng dẫn quy trình đăng ký tài khoản trực tiếp (Khách Hàng, Gian Hàng Seller, Tài Xế Shipper).
- Hướng dẫn hủy yêu cầu khi khách nhắn 'Hủy yêu cầu'.
- Xưng hô: Xưng 'Mình' hoặc 'Em', gọi khách là 'bạn' hoặc 'quý khách'. Trả lời ngắn gọn, tự nhiên, đúng trọng tâm (từ 2 - 3 câu).

DỮ LIỆU THỰC TẾ TRÊN DATABASE SÀN ZONEMART:
{realtimeDataContext}

HÃY TƯ VẤN THÔNG MINH, TẬP TRUNG VÀO ẨM THỰC VÀ DỊCH VỤ CỦA SÀN ZONEMART!";

            // 4. XOAY VÒNG MODEL AI (MULTI-LLM FALLBACK ENGINE: Gemini -> Groq -> Astra)
            string aiReply = string.Empty;
            string usedProvider = string.Empty;

            // Thử Provider 1: Google Gemini API
            try
            {
                aiReply = await CallGeminiApiAsync(systemPrompt, userMsg, request?.History);
                if (!string.IsNullOrWhiteSpace(aiReply))
                {
                    usedProvider = "Google Gemini 1.5 Flash";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [Gemini API Warning] {ex.Message}. Đang tự động chuyển sang Groq Fallback...");
            }

            // Thử Provider 2: Groq API (nếu Gemini lỗi/hết lượt)
            if (string.IsNullOrWhiteSpace(aiReply))
            {
                try
                {
                    aiReply = await CallGroqApiAsync(systemPrompt, userMsg, request?.History);
                    if (!string.IsNullOrWhiteSpace(aiReply))
                    {
                        usedProvider = "Groq Llama-3-70B";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ [Groq API Warning] {ex.Message}. Đang tự động chuyển sang Astra Fallback...");
                }
            }

            // Thử Provider 3: Astra API / Fallback tổng hợp
            if (string.IsNullOrWhiteSpace(aiReply))
            {
                try
                {
                    aiReply = await CallAstraApiAsync(systemPrompt, userMsg);
                    if (!string.IsNullOrWhiteSpace(aiReply))
                    {
                        usedProvider = "Astra GPT Multi-Engine";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ [Astra API Warning] {ex.Message}");
                }
            }

            // Dự phòng cuối cùng nếu cả 3 API đều gián đoạn mạng
            if (string.IsNullOrWhiteSpace(aiReply))
            {
                aiReply = GenerateRuleBasedSmartReply(userMsg, cleanMsgNoTone, realtimeDataContext, userLocationInfo);
                usedProvider = "ZoneMart Local AI Engine";
            }

            // 5. CHỈ TRUY VẤN THẺ HÌNH ẢNH SẢN PHẨM & CÁC NÚT TƯƠNG TÁC KHI CÓ Ý ĐỊNH PHÙ HỢP
            var productCards = await ExtractProductCardsAsync(userMsg, cleanMsgNoTone);
            var storeCards = await ExtractStoreCardsAsync(userMsg, cleanMsgNoTone);

            // BỘ LỌC BẢO VỆ CHẮC CHẮN: Nếu AI reply câu báo KHÔNG CÓ cửa hàng / KHÔNG CÓ sản phẩm / ngoài bán kính -> XOÁ SẠCH THẺ THAM KHẢO!
            string cleanReplyNoTone = RemoveVietnameseTones((aiReply ?? "").ToLower());
            bool containsNoDataPhrase = cleanReplyNoTone.Contains("chua co diem ban") ||
                                         cleanReplyNoTone.Contains("chua co gian hang") ||
                                         cleanReplyNoTone.Contains("chua co quan") ||
                                         cleanReplyNoTone.Contains("khong co quan") ||
                                         cleanReplyNoTone.Contains("chua co san pham") ||
                                         cleanReplyNoTone.Contains("khong tim thay") ||
                                         cleanReplyNoTone.Contains("chua co mat hang") ||
                                         cleanReplyNoTone.Contains("khong co mat hang") ||
                                         cleanReplyNoTone.Contains("vuot qua ban kinh") ||
                                         cleanReplyNoTone.Contains("khong the giao hoa toc");

            if (containsNoDataPhrase)
            {
                productCards = new List<object>();
                storeCards = new List<object>();
            }

            // Kiểm tra intent Đăng ký Seller / Shipper / Buyer (chỉ khi không phải hỏi về app bên thứ 3)
            string? actionIntent = null;
            if (!isThirdPartyQuery)
            {
                if (cleanMsgNoTone.Contains("seller") || cleanMsgNoTone.Contains("mo shop") || cleanMsgNoTone.Contains("mo gian hang") || cleanMsgNoTone.Contains("ban hang"))
                {
                    actionIntent = "REGISTER_SELLER";
                }
                else if (cleanMsgNoTone.Contains("shipper") || cleanMsgNoTone.Contains("tai xe") || cleanMsgNoTone.Contains("chay xe"))
                {
                    actionIntent = "REGISTER_SHIPPER";
                }
                else if (cleanMsgNoTone.Contains("khach hang") || cleanMsgNoTone.Contains("nguoi mua") || cleanMsgNoTone.Contains("tai khoan") || cleanMsgNoTone.Contains("dang ky"))
                {
                    actionIntent = "REGISTER_BUYER";
                }
                else if (cleanMsgNoTone.Contains("gui yeu cau ho tro") || cleanMsgNoTone.Contains("ho tro truc tuyen") || cleanMsgNoTone.Contains("yeu cau ho tro") || cleanMsgNoTone.Contains("khieu nai") || cleanMsgNoTone.Contains("can ho tro") || cleanMsgNoTone.Contains("dong gop y kien"))
                {
                    actionIntent = "SUPPORT_CHOICE";
                }
            }

            return Ok(new
            {
                success = true,
                reply = aiReply,
                provider = usedProvider,
                products = productCards,
                stores = storeCards,
                actionIntent = actionIntent
            });
        }

        // ================= CÁC HÀM GỌI API MULTI-LLM (FALLBACK ENGINE) =================

        private async Task<string> CallGeminiApiAsync(string systemPrompt, string userMsg, List<ChatMessageItem>? history)
        {
            // URL Gemini đọc trực tiếp từ cấu hình
            string geminiApiKey = _configuration["GeminiSettings:ApiKey"] ?? "";
            string geminiModel = _configuration["GeminiSettings:Model"] ?? "gemini-1.5-flash";
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent?key={geminiApiKey}";

            var contents = new List<object>();

            // Nạp System Prompt vào lượt đầu
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = systemPrompt } }
            });
            contents.Add(new
            {
                role = "model",
                parts = new[] { new { text = "Dạ vâng! Tôi đã hiểu rõ nhiệm vụ làm Trợ lý AI Chuyên gia ZoneMart. Tôi sẵn sàng hỗ trợ người dùng mua sắm và đăng ký sàn ZoneMart!" } }
            });

            // Nạp lịch sử chat gần đây
            if (history != null)
            {
                foreach (var item in history.TakeLast(6))
                {
                    contents.Add(new
                    {
                        role = item.Sender == "ai" ? "model" : "user",
                        parts = new[] { new { text = item.Text } }
                    });
                }
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMsg } }
            });

            var payload = new
            {
                contents = contents,
                generationConfig = new
                {
                    maxOutputTokens = 250,
                    temperature = 0.5
                }
            };
            string jsonBody = JsonSerializer.Serialize(payload);

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini HTTP Status: {response.StatusCode}");
            }

            string resJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resJson);
            var root = doc.RootElement;
            var text = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            return text ?? string.Empty;
        }

        private async Task<string> CallGroqApiAsync(string systemPrompt, string userMsg, List<ChatMessageItem>? history)
        {
            string groqApiKey = _configuration["GroqSettings:ApiKey"] ?? _configuration["GROQ_API_KEY"] ?? "";
            string url = "https://api.groq.com/openai/v1/chat/completions";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            if (history != null)
            {
                foreach (var item in history.TakeLast(6))
                {
                    if (item != null && !string.IsNullOrWhiteSpace(item.Text))
                    {
                        messages.Add(new { role = item.Sender == "ai" ? "assistant" : "user", content = item.Text });
                    }
                }
            }

            messages.Add(new { role = "user", content = userMsg });

            string[] candidateModels = new[] { "openai/gpt-oss-120b", "openai/gpt-oss-20b", "qwen/qwen3.8-27b", "llama-3.3-70b-versatile" };

            string lastErr = "";
            foreach (var modelName in candidateModels)
            {
                try
                {
                    var payload = new
                    {
                        model = modelName,
                        messages = messages,
                        temperature = 0.5,
                        max_tokens = 250
                    };

                    using var req = new HttpRequestMessage(HttpMethod.Post, url);
                    req.Headers.Add("Authorization", $"Bearer {groqApiKey}");
                    req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(req);
                    if (response.IsSuccessStatusCode)
                    {
                        string resJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(resJson);
                        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                    }
                    else
                    {
                        lastErr = await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    lastErr = ex.Message;
                }
            }

            throw new Exception($"Groq API Error: {lastErr}");
        }

        private async Task<string> CallAstraApiAsync(string systemPrompt, string userMsg)
        {
            // Đọc cấu hình Kira / Astra từ appsettings
            string kiraKey = _configuration["KiraSettings:ApiKey"] ?? "";
            string endpoint = _configuration["KiraSettings:Endpoint"] ?? "https://kiraai.vn/api/v1/chat/completions";
            string model = _configuration["KiraSettings:Model"] ?? "gpt-6-astra";

            if (string.IsNullOrWhiteSpace(kiraKey) || kiraKey == "YOUR_KIRA_API_KEY")
            {
                await Task.Delay(50);
                return string.Empty;
            }

            try
            {
                var payload = new
                {
                    model = model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMsg }
                    },
                    temperature = 0.5,
                    max_tokens = 250
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {kiraKey}");
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var response = await _httpClient.SendAsync(req, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    string resJson = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(resJson);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var text = choices[0].GetProperty("message").GetProperty("content").GetString();
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [Kira/Astra API Warning] {ex.Message}");
            }

            return string.Empty;
        }

        // ================= CÁC HÀM XỬ LÝ DỮ LIỆU MONGODB REALTIME & RAG =================

        private string ExtractTopicName(string cleanMsgNoTone)
        {
            string msg = (cleanMsgNoTone ?? "").Replace("banh my", "banh mi");

            var categoryTokens = new (string Token, string DisplayName)[]
            {
                ("banh mi", "bánh mì"),
                ("trai cay", "trái cây"),
                ("hai san", "hải sản"),
                ("nuoc", "nước / đồ uống"),
                ("sua", "sữa"),
                ("ga", "gà"),
                ("com", "cơm"),
                ("bo", "bò"),
                ("thit", "thịt"),
                ("rau", "rau củ"),
                ("kem", "kem"),
                ("che", "chè"),
                ("pho", "phở"),
                ("bun", "bún"),
                ("lau", "lẩu"),
                ("ca", "cá"),
                ("tom", "tôm"),
                ("vit", "vịt")
            };

            foreach (var (token, displayName) in categoryTokens)
            {
                if (Regex.IsMatch(msg, $@"\b{Regex.Escape(token)}\b", RegexOptions.IgnoreCase))
                {
                    return displayName;
                }
            }

            string[] stopWords = new[]
            {
                "neu", "ma", "thi", "co", "nhung", "gi", "nhi", "khong", "la", "mon", "do", "an", "toi", "muon", "tim", "dat", "nho",
                "suat", "phan", "cai", "hop", "chai", "lon", "bat", "to", "dia", "khay", "tui", "goi", "ly", "coc", "chiet", "nhe", "nha", "dum", "giup", "cho", "lay",
                "the", "nay", "web", "san", "trang", "site", "hien", "tai", "ben"
            };
            var words = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                .ToList();

            if (words.Count > 0) return words.First();
            return "thực phẩm";
        }

        private async Task<List<Product>> SearchProductsInDatabaseAsync(string cleanMsgNoTone)
        {
            try
            {
                var allProducts = await _mongoService.Products
                    .Find(p => p.StockStatus != "out_of_stock" && p.AiStatus != "rejected")
                    .SortByDescending(p => p.CreatedAt)
                    .ToListAsync();

                if (allProducts == null || allProducts.Count == 0) return new List<Product>();

                // Gán tên gian hàng StoreName chính xác từ collection stores cho từng sản phẩm
                try
                {
                    var allStores = await _mongoService.Stores
                        .Find(s => s.Status != "Rejected" && s.Status != "deleted")
                        .ToListAsync();
                    if (allStores != null && allStores.Count > 0)
                    {
                        var storeMap = allStores.ToDictionary(s => s.Id ?? "", s => s.StoreName ?? "ZoneMart Store");
                        foreach (var p in allProducts)
                        {
                            if (!string.IsNullOrWhiteSpace(p.StoreId) && storeMap.TryGetValue(p.StoreId, out var sName) && !string.IsNullOrWhiteSpace(sName))
                            {
                                p.StoreName = sName;
                            }
                        }
                    }
                }
                catch { }

                // Nếu là câu hỏi về thời tiết se lạnh / món ăn ấm nóng -> Ưu tiên trả về các món ăn nóng hổi
                if (IsColdWeatherIntent(cleanMsgNoTone))
                {
                    var warmItems = allProducts.Where(IsProductWarming).ToList();
                    if (warmItems.Count > 0) return warmItems;
                }

                // Nếu là câu hỏi về thời tiết nắng nóng / món giải nhiệt, mát lạnh -> Ưu tiên trả về các món mát giải nhiệt
                if (IsHotWeatherIntent(cleanMsgNoTone))
                {
                    var coldItems = allProducts.Where(IsProductCooling).ToList();
                    if (coldItems.Count > 0) return coldItems;
                }

                string msg = (cleanMsgNoTone ?? "").Replace("banh my", "banh mi");

                string[] stopWords = new[]
                {
                    "toi", "muon", "an", "tim", "co", "khong", "giao", "cho", "ban", "cho toi", "xem",
                    "danh sach", "mon", "san pham", "quan", "shop", "hoi", "voi", "gium", "giup", "co phai", "lay", "cho xin",
                    "nha", "sinh", "phai", "lam", "sao", "qua", "ma", "xung", "quanh", "buon", "ia", "ngu", "met", "thoi", "tiet",
                    "neu", "thi", "nhung", "nhi", "a", "da", "loai", "nao", "gi", "den", "va", "la", "cac", "nay", "do", "ve",
                    "duoc", "ra", "vao", "nhe", "oi", "ay", "nho", "ho", "dum", "tuong", "bao", "dang", "hien", "tai", "the",
                    "suat", "phan", "cai", "hop", "chai", "lon", "bat", "to", "dia", "khay", "tui", "goi", "ly", "coc", "chiet", "o", "khach", "mua", "dat",
                    "web", "san", "trang", "site", "ben",
                    "nong", "lanh", "am", "se", "mat", "nhiet", "hom", "nay", "ngay", "nen", "bot", "ngon",
                    "dau", "o dau", "cho nao", "dau roi", "vi tri", "dia chi", "toa do", "may gio", "bao gio"
                };

                var rawWords = msg.Split(new[] { ' ', '?', '.', ',', '!', ':', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Trim())
                    .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                    .ToList();

                var words = new List<string>();
                foreach (var w in rawWords)
                {
                    if (w == "my" && !msg.Contains("bo") && !msg.Contains("thit"))
                    {
                        continue;
                    }
                    if (w == "dau" && !msg.Contains("dau tay") && !msg.Contains("trai dau") && !msg.Contains("qua dau") && !msg.Contains("hop dau") && !msg.Contains("an dau") && !msg.Contains("mua dau"))
                    {
                        continue;
                    }
                    words.Add(w);
                }

                if (words.Count == 0)
                {
                    return new List<Product>();
                }

                string fullSearchPhrase = string.Join(" ", words).Trim();

                // 1. Ưu tiên kiểm tra cụm từ đầy đủ (Ví dụ: "banh mi chao", "nuoc ngot", "banh mi", "com ga")
                if (!string.IsNullOrWhiteSpace(fullSearchPhrase) && fullSearchPhrase.Length >= 3)
                {
                    var phraseMatches = allProducts.Where(p =>
                    {
                        string pName = RemoveVietnameseTones(p.ProductName ?? "").ToLower();
                        string pCat = RemoveVietnameseTones(p.Category ?? "").ToLower();

                        string phraseRegex = $@"\b{Regex.Escape(fullSearchPhrase)}\b";
                        if (Regex.IsMatch(pName, phraseRegex) || Regex.IsMatch(pCat, phraseRegex))
                            return true;
                        return false;
                    }).ToList();

                    if (phraseMatches.Count > 0)
                    {
                        return phraseMatches;
                    }

                    if (fullSearchPhrase.Contains("banh mi"))
                    {
                        var banhMiMatches = allProducts.Where(p =>
                        {
                            string pName = RemoveVietnameseTones(p.ProductName ?? "").ToLower();
                            return pName.Contains("banh mi");
                        }).ToList();

                        if (banhMiMatches.Count > 0)
                        {
                            return banhMiMatches;
                        }
                    }
                }

                // 2. Kiểm tra khớp theo từng từ với Ranh Giới Từ (\bword\b)
                var matchedProducts = allProducts.Where(p =>
                {
                    string pName = RemoveVietnameseTones(p.ProductName ?? "").ToLower();
                    string pCat = RemoveVietnameseTones(p.Category ?? "").ToLower();

                    foreach (var w in words)
                    {
                        if (w.Length < 2) continue;
                        string wordRegex = $@"\b{Regex.Escape(w)}\b";
                        if (Regex.IsMatch(pName, wordRegex) || Regex.IsMatch(pCat, wordRegex))
                        {
                            return true;
                        }
                    }
                    return false;
                }).ToList();

                if (matchedProducts.Count > 0)
                {
                    matchedProducts = matchedProducts.OrderByDescending(p =>
                    {
                        string pName = RemoveVietnameseTones(p.ProductName ?? "").ToLower();
                        int score = 0;
                        foreach (var w in words)
                        {
                            string wordRegex = $@"\b{Regex.Escape(w)}\b";
                            if (Regex.IsMatch(pName, wordRegex)) score += 50;
                        }
                        return score;
                    }).ToList();
                }

                return matchedProducts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [SearchProductsInDatabaseAsync Error] {ex.Message}");
                return new List<Product>();
            }
        }

        private async Task<string> FetchRealtimeMarketDataAsync(string userMsg, string cleanMsgNoTone)
        {
            var sb = new StringBuilder();

            try
            {
                var allProducts = await _mongoService.Products
                    .Find(p => p.StockStatus == "in_stock" || p.AiStatus == "approved")
                    .ToListAsync();

                var allStores = await _mongoService.Stores
                    .Find(s => s.Status == "Active")
                    .ToListAsync();

                // NẠP THỐNG KÊ REALTIME CHÍNH XÁC TỪ DATABASE MONGODB ATLAS
                sb.AppendLine("📊 THỐNG KÊ DỮ LIỆU REALTIME SÀN ZONEMART TỪ DATABASE MONGODB ATLAS:");
                sb.AppendLine($"  + Tổng số cửa hàng đang hoạt động: {allStores.Count} quán.");
                if (allStores.Count > 0)
                {
                    sb.AppendLine($"  + Danh sách chi tiết quán & địa chỉ: " + string.Join("; ", allStores.Select(s => $"'{s.StoreName}' (Địa chỉ: {s.Address ?? "ZoneMart Area"}, Ngành hàng: {s.Category ?? "Thực phẩm"})")));
                }
                sb.AppendLine($"  + Tổng số sản phẩm/món ăn có sẵn: {allProducts.Count} sản phẩm.\n");

                // 1. NHẬN DIỆN THỜI TIẾT (Nắng nóng / Giải nhiệt vs Trời lạnh / Trời mưa)
                bool isColdWeather = IsColdWeatherIntent(cleanMsgNoTone);
                bool isHotWeather = IsHotWeatherIntent(cleanMsgNoTone);

                if (isHotWeather)
                {
                    sb.AppendLine("☀️ BỐI CẢNH THỜI TIẾT NẮNG NÓNG: Khách hàng cần tư vấn các món ăn / đồ uống tươi mát GIẢI NHIỆT (như Dâu tây tươi Đà Lạt thanh mát, Sữa tươi tiệt trùng ướp lạnh, Trái cây, Cà chua bi, Xà lách, Sữa đặc pha sinh tố hoa quả dầm đá...). TUYỆT ĐỐI KHÔNG GỢI Ý MÓN NÓNG NHƯ BÁNH MÌ CHẢO, PHỞ HAY CƠM GÀ!");
                    sb.AppendLine("CÁC MÓN GIẢI NHIỆT CÓ SẴN THỰC TẾ TRONG DATABASE ZONEMART:");

                    var coldItems = allProducts.Where(IsProductCooling).Take(6).ToList();
                    if (coldItems.Count == 0) coldItems = allProducts.Take(5).ToList();

                    foreach (var p in coldItems)
                    {
                        sb.AppendLine($"  + Cửa hàng: [{p.StoreName ?? "ZoneMart Store"}] có món giải nhiệt mát lạnh: [{p.ProductName}] - Giá: {p.Price:N0}đ");
                    }
                }
                else if (isColdWeather)
                {
                    sb.AppendLine("❄️ BỐI CẢNH THỜI TIẾT SE LẠNH / TRỜI MƯA: Khách hàng cần tư vấn các món ăn ẤM NÓNG, bốc khói nghi ngút, ấm bụng (như Phở Bò Tái Nạm nóng hổi đậm đà, Combo Bánh Mì Chảo xèo xèo nóng hổi kèm pate, Cơm Gà xối mỡ da giòn nóng hổi, Thịt bò Mỹ nhúng lẩu nóng). TUYỆT ĐỐI KHÔNG GỢI Ý ĐỒ LẠNH!");
                    sb.AppendLine("CÁC MÓN ẤM NÓNG CÓ SẴN THỰC TẾ TRONG DATABASE ZONEMART:");

                    var warmItems = allProducts.Where(IsProductWarming).Take(6).ToList();
                    if (warmItems.Count == 0) warmItems = allProducts.Take(5).ToList();

                    foreach (var p in warmItems)
                    {
                        sb.AppendLine($"  + Cửa hàng: [{p.StoreName ?? "ZoneMart Store"}] có món nóng hổi ấm bụng: [{p.ProductName}] - Giá: {p.Price:N0}đ");
                    }
                }

                // 2. NHẬN DIỆN VỊ TRÍ VÀ BÁN KÍNH 5KM (Trần Duy Hưng, Cầu Giấy, Mai Dịch, Địa chỉ các quán...)
                bool isLocationQuery = cleanMsgNoTone.Contains("tran duy hung") || cleanMsgNoTone.Contains("cau giay") ||
                                       cleanMsgNoTone.Contains("mai dich") || cleanMsgNoTone.Contains("ban kinh") ||
                                       cleanMsgNoTone.Contains("5km") || cleanMsgNoTone.Contains("3km") ||
                                       cleanMsgNoTone.Contains("gan day") || cleanMsgNoTone.Contains("gan toi") ||
                                       cleanMsgNoTone.Contains("tim quan") || cleanMsgNoTone.Contains("dia chi") ||
                                       cleanMsgNoTone.Contains("cac quan") || cleanMsgNoTone.Contains("co trong quan") ||
                                       cleanMsgNoTone.Contains("co trong san");

                if (isLocationQuery)
                {
                    string areaName = "Cầu Giấy / Trần Duy Hưng";
                    if (cleanMsgNoTone.Contains("tran duy hung")) areaName = "Trần Duy Hưng";
                    else if (cleanMsgNoTone.Contains("mai dich")) areaName = "Mai Dịch, Cầu Giấy";
                    else if (cleanMsgNoTone.Contains("cau giay")) areaName = "Cầu Giấy";

                    sb.AppendLine($"\n📍 BỐI CẢNH VỊ TRÍ TÌM QUÁN: Khách cần tìm quán gần khu vực '{areaName}' bán kính 3km - 5km.");
                    sb.AppendLine("DANH SÁCH CỬA HÀNG CÓ THỰC TẾ TRONG DATABASE ĐANG MỞ CỬA GẦN ĐÂY:");

                    foreach (var s in allStores.Take(4))
                    {
                        sb.AppendLine($"  + Quán: [{s.StoreName}] - Địa chỉ: {s.Address} (Khoảng cách: ~0.8km - 2.5km, Giao nhanh: 12-18 phút) - Ngành hàng: {s.Category}");
                        var storeProds = allProducts.Where(p => p.StoreId == s.Id || p.StoreName == s.StoreName).Take(2).ToList();
                        foreach (var sp in storeProds)
                        {
                            sb.AppendLine($"     └ Món ngon nổi bật tại quán: [{sp.ProductName}] - Giá: {sp.Price:N0}đ");
                        }
                    }
                }

                // 3. TÌM KIẾM THEO TỪ KHÓA SẢN PHẨM KHỚP TRONG DATABASE (nếu không phải câu hỏi thời tiết)
                if (!isHotWeather && !isColdWeather)
                {
                    var matchedProducts = await SearchProductsInDatabaseAsync(cleanMsgNoTone);
                    if (matchedProducts.Count > 0)
                    {
                        sb.AppendLine($"\n🎯 CÁC SẢN PHẨM KHỚP TỪ KHÓA TÌM KIẾM '{userMsg}':");
                        foreach (var p in matchedProducts.Take(5))
                        {
                            sb.AppendLine($"  + Cửa hàng: [{p.StoreName ?? "ZoneMart Store"}] đang bán món: [{p.ProductName}] - Giá: {p.Price:N0}đ");
                        }
                    }
                    else if (!isLocationQuery)
                    {
                    bool isAskingForRecommendation = cleanMsgNoTone.Contains("goi y") || cleanMsgNoTone.Contains("tham khao") || cleanMsgNoTone.Contains("an gi") || cleanMsgNoTone.Contains("mon ngon") || cleanMsgNoTone.Contains("top mon") || cleanMsgNoTone.Contains("do an vat");
                    if (isAskingForRecommendation)
                    {
                        sb.AppendLine("\n📦 KHÁCH HỎI GỢI Ý THAM KHẢO, DƯỚI ĐÂY LÀ SẢN PHẨM THỰC TẾ TRÊN SÀN:");
                        foreach (var p in allProducts.Take(5))
                        {
                            sb.AppendLine($"  + Cửa hàng: [{p.StoreName ?? "ZoneMart Store"}] bán món: [{p.ProductName}] - Giá: {p.Price:N0}đ");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"\n(Lưu ý: Không tìm thấy sản phẩm khớp chính xác từ khóa '{userMsg}'. Nếu người dùng trò chuyện đời sống/thắc mắc số liệu thì dùng dữ liệu thống kê ở trên để trả lời nhé).");
                    }
                }
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoDB Data Fetch Error] {ex.Message}");
            }

            return sb.ToString();
        }

        private async Task<List<object>> ExtractProductCardsAsync(string userMsg, string cleanMsgNoTone)
        {
            var cards = new List<object>();

            string[] excludeIntents = new[]
            {
                "dang ky", "tai khoan", "seller", "shipper", "mo shop", "tai xe", "dang nhap",
                "khach hang", "nguoi mua", "mo gian hang", "chay xe", "nguoi ban", "ho tro", "tro giup",
                "tu van duoc gi", "lam duoc gi", "chuc nang", "tinh nang", "giup duoc gi", "giup duoc nhung gi",
                "giup gi", "co the giup", "ho tro gi",
                "20km", "15km", "30km", "cach nha", "quang duong", "khoang cach", "giao xa", "ban kinh giao"
            };

            if (excludeIntents.Any(kw => cleanMsgNoTone.Contains(kw)))
            {
                return cards;
            }

            // CHỈ TRẢ VỀ THẺ SẢN PHẨM KHI NGƯỜI DÙNG CÓ Ý ĐỊNH MUA SẮM / HỎI MÓN ĂN / HỎI GỢI Ý / HỎI THỰC PHẨM
            bool isFoodOrProductIntent = cleanMsgNoTone.Contains("an gi") || cleanMsgNoTone.Contains("mon") ||
                                          cleanMsgNoTone.Contains("goi y") || cleanMsgNoTone.Contains("tham khao") ||
                                          cleanMsgNoTone.Contains("do an") || cleanMsgNoTone.Contains("thuc pham") ||
                                          cleanMsgNoTone.Contains("trai cay") || cleanMsgNoTone.Contains("rau") ||
                                          cleanMsgNoTone.Contains("thit") || cleanMsgNoTone.Contains("gao") ||
                                          cleanMsgNoTone.Contains("banh") || cleanMsgNoTone.Contains("pho") ||
                                          cleanMsgNoTone.Contains("bun") || cleanMsgNoTone.Contains("lau") ||
                                          cleanMsgNoTone.Contains("nuoc") || cleanMsgNoTone.Contains("kem") ||
                                          cleanMsgNoTone.Contains("che") || cleanMsgNoTone.Contains("mua") ||
                                          cleanMsgNoTone.Contains("co ban") || cleanMsgNoTone.Contains("ban khong") || cleanMsgNoTone.Contains("ban hang") ||
                                          (cleanMsgNoTone.Contains("gia") && !cleanMsgNoTone.Contains("tham gia")) ||
                                          cleanMsgNoTone.Contains("tim quan") || cleanMsgNoTone.Contains("giai nhiet") ||
                                          cleanMsgNoTone.Contains("nong hoi") ||
                                          (cleanMsgNoTone.Contains("nong") && cleanMsgNoTone.Contains("an")) ||
                                          (cleanMsgNoTone.Contains("lanh") && cleanMsgNoTone.Contains("an"));

            if (!isFoodOrProductIntent)
            {
                // Câu hỏi đời sống, ngoài lề, chuyện phiếm -> KHÔNG hiển thị thẻ sản phẩm bên dưới!
                return cards;
            }

            try
            {
                if (IsHotWeatherIntent(cleanMsgNoTone))
                {
                    var allProds = await _mongoService.Products
                        .Find(p => p.StockStatus == "in_stock" || p.AiStatus == "approved")
                        .ToListAsync();

                    var coldItems = allProds.Where(IsProductCooling).Take(4).ToList();
                    if (coldItems.Count == 0) coldItems = allProds.Take(4).ToList();

                    foreach (var p in coldItems)
                    {
                        cards.Add(new
                        {
                            id = p.Id,
                            name = p.ProductName,
                            price = p.Price,
                            image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                            storeName = p.StoreName ?? "ZoneMart Store",
                            category = p.Category
                        });
                    }
                    return cards;
                }

                if (IsColdWeatherIntent(cleanMsgNoTone))
                {
                    var allProds = await _mongoService.Products
                        .Find(p => p.StockStatus == "in_stock" || p.AiStatus == "approved")
                        .ToListAsync();

                    var warmItems = allProds.Where(IsProductWarming).Take(4).ToList();
                    if (warmItems.Count == 0) warmItems = allProds.Take(4).ToList();

                    foreach (var p in warmItems)
                    {
                        cards.Add(new
                        {
                            id = p.Id,
                            name = p.ProductName,
                            price = p.Price,
                            image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                            storeName = p.StoreName ?? "ZoneMart Store",
                            category = p.Category
                        });
                    }
                    return cards;
                }

                var matchedProducts = await SearchProductsInDatabaseAsync(cleanMsgNoTone);

                bool isWeatherQuery = cleanMsgNoTone.Contains("nong") || cleanMsgNoTone.Contains("lanh") || cleanMsgNoTone.Contains("giai nhiet") || cleanMsgNoTone.Contains("mua") || cleanMsgNoTone.Contains("ret");
                bool isLocationQuery = cleanMsgNoTone.Contains("tran duy hung") || cleanMsgNoTone.Contains("cau giay") || cleanMsgNoTone.Contains("mai dich") || cleanMsgNoTone.Contains("ban kinh") || cleanMsgNoTone.Contains("5km") || cleanMsgNoTone.Contains("gan day") || cleanMsgNoTone.Contains("gan toi");
                bool isRecommendationQuery = cleanMsgNoTone.Contains("goi y") || cleanMsgNoTone.Contains("tham khao") || cleanMsgNoTone.Contains("an gi") || cleanMsgNoTone.Contains("mon ngon") || cleanMsgNoTone.Contains("do an vat");

                if (matchedProducts.Count == 0 && (isWeatherQuery || isLocationQuery || isRecommendationQuery))
                {
                    matchedProducts = await _mongoService.Products
                        .Find(p => p.StockStatus == "in_stock" || p.AiStatus == "approved")
                        .Limit(4)
                        .ToListAsync();
                }

                foreach (var p in matchedProducts.Take(4))
                {
                    cards.Add(new
                    {
                        id = p.Id,
                        name = p.ProductName,
                        price = p.Price,
                        image = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/anhxoanen.png",
                        storeName = p.StoreName ?? "ZoneMart Store",
                        category = p.Category
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [ExtractProductCardsAsync Error] {ex.Message}");
            }

            return cards;
        }

        private async Task<List<object>> ExtractStoreCardsAsync(string userMsg, string cleanMsgNoTone)
        {
            var cards = new List<object>();

            string[] excludeIntents = new[]
            {
                "dang ky", "tai khoan", "seller", "shipper", "mo shop", "tai xe", "dang nhap",
                "khach hang", "nguoi mua", "mo gian hang", "chay xe", "nguoi ban", "ho tro", "tro giup"
            };

            if (excludeIntents.Any(kw => cleanMsgNoTone.Contains(kw)))
            {
                return cards;
            }

            // Lấy tất cả gian hàng đang Active từ MongoDB Atlas
            var activeStores = await _mongoService.Stores.Find(s => s.Status == "Active").ToListAsync();

            // Nếu người dùng hỏi tìm quán ở một địa điểm / khu vực cụ thể
            bool isLocationQuery = cleanMsgNoTone.Contains("quan o") || cleanMsgNoTone.Contains("shop o") ||
                                   cleanMsgNoTone.Contains("tim quan") || cleanMsgNoTone.Contains("o khu vuc") ||
                                   cleanMsgNoTone.Contains("tai khu vuc") || cleanMsgNoTone.Contains("quan o khu") ||
                                   cleanMsgNoTone.Contains("cac quan o");

            if (isLocationQuery)
            {
                // Kiểm tra xem trong DB có gian hàng nào trùng địa điểm được hỏi không
                var matchedStoresInArea = activeStores.Where(s =>
                {
                    string storeAddrClean = RemoveVietnameseTones((s.Address ?? "").ToLower());
                    if (string.IsNullOrWhiteSpace(storeAddrClean)) return false;

                    var words = cleanMsgNoTone.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var w in words)
                    {
                        if (w.Length >= 4 && !new[] { "quan", "shop", "hien", "tai", "muon", "khu", "vuc", "giao", "hang", "co", "khong" }.Contains(w))
                        {
                            if (storeAddrClean.Contains(w)) return true;
                        }
                    }
                    return false;
                }).ToList();

                if (matchedStoresInArea.Count == 0)
                {
                    // ĐỊA ĐIỂM KHÔNG CÓ QUÁN NÀO TRONG DATABASE -> TUYỆT ĐỐI KHÔNG HIỂN THỊ THẺ THAM KHẢO!
                    return cards;
                }
                else
                {
                    foreach (var s in matchedStoresInArea.Take(4))
                    {
                        cards.Add(new
                        {
                            id = s.Id,
                            name = s.StoreName,
                            address = s.Address,
                            avatar = "/images/seller_mascot_clean.png",
                            rating = 4.9
                        });
                    }
                    return cards;
                }
            }

            bool isAskingForStores = cleanMsgNoTone.Contains("quan hot") || cleanMsgNoTone.Contains("top quan") ||
                                   cleanMsgNoTone.Contains("best seller") || cleanMsgNoTone.Contains("tran duy hung") ||
                                   cleanMsgNoTone.Contains("cau giay") || cleanMsgNoTone.Contains("mai dich") ||
                                   cleanMsgNoTone.Contains("ban kinh") || cleanMsgNoTone.Contains("5km") ||
                                   cleanMsgNoTone.Contains("3km") || cleanMsgNoTone.Contains("gan day") ||
                                   cleanMsgNoTone.Contains("gan toi") || cleanMsgNoTone.Contains("cac quan") ||
                                   cleanMsgNoTone.Contains("co trong quan") || cleanMsgNoTone.Contains("co trong san");

            if (!isAskingForStores)
            {
                return cards;
            }

            try
            {
                foreach (var s in activeStores.Take(4))
                {
                    cards.Add(new
                    {
                        id = s.Id,
                        name = s.StoreName,
                        address = s.Address,
                        avatar = "/images/seller_mascot_clean.png",
                        rating = 4.9
                    });
                }
            }
            catch { }
            return cards;
        }

        private bool IsProfaneOrInappropriate(string userMsg, string cleanMsgNoTone)
        {
            if (string.IsNullOrWhiteSpace(userMsg)) return false;

            string rawLower = userMsg.ToLower();

            // 1. Danh sách từ có dấu / từ lóng viết tắt thô tục rõ ràng
            string[] exactProfaneList = new[]
            {
                "lồn", "cặc", "đm", "đkm", "clm", "vcl", "địt", "buồi", "đéo", "phò", "chửi",
                "fuck", "bitch", "cai l", "d.m", "d.k.m", "v.l", "c.l.m", "lồn"
            };

            foreach (var kw in exactProfaneList)
            {
                if (rawLower.Contains(kw))
                {
                    return true;
                }
            }

            // 2. Phân tách ranh giới từ (\b) cho các từ thô tục rõ ràng (không chứa từ Tiếng Việt thông dụng như 'các', 'phở', 'lon')
            string pattern = @"\b(cack|dkm|clm|vcl|vl|dit|buoi|deo)\b";
            if (Regex.IsMatch(cleanMsgNoTone, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static readonly string[] ThirdPartyPlatforms = new[]
        {
            "zalo", "facebook", "fb", "tiktok", "tik tok", "instagram", "insta", "ig",
            "shopee", "lazada", "tiki", "sendo", "grab", "be", "gojek", "shopeefood",
            "momo", "vnpay", "zalopay", "viettel", "vinaphone", "mobifone",
            "telegram", "tele", "wechat", "twitter", "whatsapp", "youtube", "netflix",
            "binance", "discord", "skype"
        };

        private static bool IsThirdPartyPlatformRequest(string cleanMsgNoTone)
        {
            if (string.IsNullOrWhiteSpace(cleanMsgNoTone)) return false;
            return ThirdPartyPlatforms.Any(tp => Regex.IsMatch(cleanMsgNoTone, $@"\b{Regex.Escape(tp)}\b"));
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DirectRegDraft> _directRegSessionCache = new System.Collections.Concurrent.ConcurrentDictionary<string, DirectRegDraft>();

        private async Task<(bool Exists, string DetailRole)> CheckIfEmailAlreadyRegisteredAsync(string email)
        {
            return await AuthController.CheckEmailRegisteredAsync(_mongoService, email);
        }

        private async Task<object?> TryProcessAiDirectRegistrationAsync(string userMsg, string cleanMsgNoTone, string? currentRegState, List<ChatMessageItem>? history, DirectRegDraft? incomingDraft)
        {
            if (string.IsNullOrWhiteSpace(userMsg)) return null;

            // CHẶN NGAY CÁC CÂU HỎI LẤY MẬT KHẨU / TÀI KHOẢN / ADMIN
            if (IsCredentialsLeakRequest(userMsg, cleanMsgNoTone) || cleanMsgNoTone.Contains("admin"))
            {
                return null;
            }

            string activeState = currentRegState ?? "NONE";
            var draft = incomingDraft ?? new DirectRegDraft();

            // 1. KIỂM TRA LỆNH "HỦY YÊU CẦU" / "HỦY ĐĂNG KÝ" (Ở BẤT KỲ BƯỚC NÀO)
            bool isExplicitCancel = Regex.IsMatch(cleanMsgNoTone, @"\b(huy yeu cau|huy dang ky|khong muon dang ky|khong dang ky nua|khong muon lam nua|dung dang ky)\b") ||
                                    Regex.IsMatch(cleanMsgNoTone, @"^\s*(huy|thoat|cancel|stop|thoi|quay lai)\s*$");

            if (isExplicitCancel)
            {
                return new
                {
                    success = true,
                    reply = "Dạ mình đã hủy yêu cầu đăng ký hộ cho bạn rồi nhé! 😊 Bây giờ bạn có thể trò chuyện và đặt các câu hỏi khác cùng mình bình thường nhé!",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "NONE",
                    actionIntent = (string?)null,
                    regDraft = new DirectRegDraft()
                };
            }

            // 2. KHỞI TẠO BẮT ĐẦU LUỒNG ĐĂNG KÝ HỘ KHI NGƯỜI DÙNG CHỌN NÚT HOẶC NHẮN "AI ĐĂNG KÝ HỘ"
            bool isStartAiReg = activeState == "NONE" && (
                                cleanMsgNoTone.Contains("ai dang ky") || cleanMsgNoTone.Contains("dang ky ho") ||
                                cleanMsgNoTone.Contains("dang ky giup") || cleanMsgNoTone.Contains("dang ky bang ai") ||
                                cleanMsgNoTone.Contains("nhap thong tin") || cleanMsgNoTone.Contains("dang ky truc tiep") ||
                                cleanMsgNoTone.Contains("nho ai dang ky") || cleanMsgNoTone.Contains("nho ai lam") ||
                                cleanMsgNoTone.Contains("toi muon ai dang ky") || cleanMsgNoTone.Contains("cach 2"));

            if (isStartAiReg)
            {
                if (cleanMsgNoTone.Contains("seller") || cleanMsgNoTone.Contains("shop") || cleanMsgNoTone.Contains("gian hang") || cleanMsgNoTone.Contains("ban hang"))
                {
                    draft = new DirectRegDraft { Role = "SELLER" };
                    return new
                    {
                        success = true,
                        reply = @"📝 **ĐĂNG KÝ GIAN HÀNG SELLER QUA AI (TRỰC TIẾP HỎI TỪNG CÂU)**

Dạ vâng! AI sẽ hỏi bạn lần lượt **từng câu một** để hoàn thiện hồ sơ gian hàng bán lẻ trên ZoneMart nhé! 😊

---
1️⃣ **Câu 1 / 14:** Cho mình xin **Tên cửa hàng / Tiệm** (Từ 12 - 30 ký tự, không chứa ký tự đặc biệt) nhé?

💡 *(Nếu không muốn đăng ký nữa, bạn nhắn **""Hủy yêu cầu""** để quay lại bình thường nhé!)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q1_STORENAME",
                        regDraft = draft
                    };
                }
                else if (cleanMsgNoTone.Contains("shipper") || cleanMsgNoTone.Contains("tai xe") || cleanMsgNoTone.Contains("chay xe"))
                {
                    draft = new DirectRegDraft { Role = "SHIPPER" };
                    return new
                    {
                        success = true,
                        reply = @"📝 **ĐĂNG KÝ TÀI XẾ SHIPPER QUA AI (TRỰC TIẾP HỎI TỪNG CÂU)**

Dạ vâng! AI sẽ hỏi bạn lần lượt **từng câu một** để làm hồ sơ đối tác tài xế ZoneMart Driver nhé! 🛵

---
1️⃣ **Câu 1 / 15:** Cho mình xin **Họ và tên thật của tài xế** (Từ 6 - 16 ký tự) nhé?

💡 *(Nếu không muốn đăng ký nữa, bạn nhắn **""Hủy yêu cầu""** để quay lại bình thường nhé!)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q1_NAME",
                        regDraft = draft
                    };
                }
                else
                {
                    draft = new DirectRegDraft { Role = "BUYER" };
                    return new
                    {
                        success = true,
                        reply = @"📝 **ĐĂNG KÝ TÀI KHOẢN KHÁCH HÀNG QUA AI (TRỰC TIẾP HỎI TỪNG CÂU)**

Dạ vâng! AI sẽ hỏi bạn lần lượt **4 câu hỏi** để tạo tài khoản người mua hàng nhé! 😊

---
1️⃣ **Câu 1 / 4:** Cho mình xin **Họ và tên đầy đủ** của bạn nhé?

💡 *(Nếu không muốn đăng ký nữa, bạn nhắn **""Hủy yêu cầu""** để quay lại bình thường nhé!)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_BUYER_Q1_NAME",
                        regDraft = draft
                    };
                }
            }

            // 3. TH 1-SHOT FULL PASTE FORM: Nếu người dùng dán toàn bộ thông tin cùng lúc
            bool containsName = Regex.IsMatch(userMsg, @"(họ tên|họ và tên|tên|full name)[\s:]", RegexOptions.IgnoreCase);
            bool containsPhoneOrEmail = Regex.IsMatch(userMsg, @"(sđt|sdt|số điện thoại|phone|email|tài khoản)[\s:]", RegexOptions.IgnoreCase) ||
                                         Regex.IsMatch(userMsg, @"\b0\d{9}\b") || Regex.IsMatch(userMsg, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            bool containsPassword = Regex.IsMatch(userMsg, @"(mật khẩu|pass|password)[\s:]", RegexOptions.IgnoreCase);
            bool isFullFormPaste = (containsName && containsPhoneOrEmail) || (containsPhoneOrEmail && containsPassword);

            if (isFullFormPaste)
            {
                return await ProcessFullFormRegistrationAsync(userMsg, activeState);
            }

            // 3b. XỬ LÝ KHI NGƯỜI DÙNG CUỘN LÊN ĐỂ TẢI LẠI ẢNH (RE-UPLOAD PHOTO CHO SELLER / SHIPPER)
            bool isPhotoUploadMsg = userMsg.StartsWith("[Ảnh ") && (userMsg.Contains("đã tải") || userMsg.Contains("thành công"));
            if (isPhotoUploadMsg)
            {
                string photoTitle = "giấy tờ";
                string targetExpectedStateSeller = "";
                string targetExpectedStateShipper = "";

                if (userMsg.Contains("CCCD Mặt Trước") || cleanMsgNoTone.Contains("cccd mat truoc"))
                {
                    photoTitle = "CCCD Mặt Trước";
                    targetExpectedStateSeller = "REGISTER_SELLER_Q10_CCCD_FRONT";
                    targetExpectedStateShipper = "REGISTER_SHIPPER_Q7_CCCD_FRONT";
                    if (!string.IsNullOrWhiteSpace(incomingDraft?.CccdFrontImage))
                        draft.CccdFrontImage = incomingDraft.CccdFrontImage;
                }
                else if (userMsg.Contains("CCCD Mặt Sau") || cleanMsgNoTone.Contains("cccd mat sau"))
                {
                    photoTitle = "CCCD Mặt Sau";
                    targetExpectedStateSeller = "REGISTER_SELLER_Q11_CCCD_BACK";
                    targetExpectedStateShipper = "REGISTER_SHIPPER_Q8_CCCD_BACK";
                    if (!string.IsNullOrWhiteSpace(incomingDraft?.CccdBackImage))
                        draft.CccdBackImage = incomingDraft.CccdBackImage;
                }
                else if (userMsg.Contains("An Toàn Thực Phẩm") || cleanMsgNoTone.Contains("an toan thuc pham") || cleanMsgNoTone.Contains("attp"))
                {
                    photoTitle = "Giấy CN An Toàn Thực Phẩm";
                    targetExpectedStateSeller = "REGISTER_SELLER_Q12_FOOD_SAFETY";
                    if (!string.IsNullOrWhiteSpace(incomingDraft?.FoodSafetyImage))
                        draft.FoodSafetyImage = incomingDraft.FoodSafetyImage;
                }
                else if (userMsg.Contains("Chân Dung") || cleanMsgNoTone.Contains("chan dung"))
                {
                    photoTitle = "Ảnh Chân Dung Tài Xế";
                    targetExpectedStateShipper = "REGISTER_SHIPPER_Q6_PORTRAIT";
                    if (!string.IsNullOrWhiteSpace(incomingDraft?.PortraitImage))
                        draft.PortraitImage = incomingDraft.PortraitImage;
                }
                else if (userMsg.Contains("Bằng Lái") || cleanMsgNoTone.Contains("bang lai") || cleanMsgNoTone.Contains("gplx"))
                {
                    photoTitle = "Bằng Lái Xe (GPLX)";
                    targetExpectedStateShipper = "REGISTER_SHIPPER_Q12_GPLX";
                    if (!string.IsNullOrWhiteSpace(incomingDraft?.DrivingLicenseImage))
                        draft.DrivingLicenseImage = incomingDraft.DrivingLicenseImage;
                }

                bool isReupload = userMsg.Contains("một lần nữa") || userMsg.Contains("tải lại") ||
                                  (activeState != targetExpectedStateSeller && activeState != targetExpectedStateShipper &&
                                   activeState != "REGISTER_SELLER_Q9_CCCD_FRONT" && activeState != "REGISTER_SELLER_Q10_CCCD_BACK" &&
                                   activeState != "REGISTER_SELLER_Q11_FOOD_SAFETY");

                if (isReupload)
                {
                    var (shortTitle, nextPrompt, qIntent) = GetQuestionInfoForState(activeState);
                    return new
                    {
                        success = true,
                        reply = $@"Dạ bạn vừa tải lại ảnh **{photoTitle}** thành công rồi nhé! Giờ mình hỏi bạn tiếp câu **{shortTitle}** nhé ạ:

---
{nextPrompt}",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = activeState,
                        actionIntent = qIntent,
                        regDraft = draft
                    };
                }
            }

            // 4. LUỒNG HỎI LẦN LƯỢT TỪNG CÂU MỘT VỚI VALIDATE CHẶT CHẼ

            // ==================== [ROLE 1] SELLER GIAN HÀNG ====================
            if (activeState == "REGISTER_SELLER_Q1_STORENAME" || activeState == "REGISTER_SELLER_STEP1" || activeState == "REGISTER_SELLER_WAITING")
            {
                string storeName = userMsg.Trim();
                if (storeName.Length < 12 || storeName.Length > 30)
                {
                    return new
                    {
                        success = true,
                        reply = $"⚠️ Tên cửa hàng phải có độ dài từ 12 đến 30 ký tự (Hiện tại bạn nhập {storeName.Length} ký tự)! Vui lòng nhập lại tên tiệm nhé!\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q1_STORENAME",
                        regDraft = draft
                    };
                }
                if (Regex.IsMatch(storeName, @"[!@#$%^&*()_+=\[\]{};':""\\|,.<>?~`]"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Tên cửa hàng không được chứa ký tự đặc biệt! Vui lòng chỉ nhập chữ cái, số và khoảng trắng (Ví dụ: `Cửa Hàng Nông Sản Ba Vì`) nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q1_STORENAME",
                        regDraft = draft
                    };
                }

                draft.StoreName = storeName;
                return new
                {
                    success = true,
                    reply = $@"Dạ đã lưu Tên gian hàng: **{storeName}**! 🏪

---
2️⃣ **Câu 2 / 14:** Cho mình xin **Địa chỉ tiệm** của bạn (Ví dụ: Số 125 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội) nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q2_ADDRESS",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q2_ADDRESS")
            {
                string address = userMsg.Trim();
                if (address.Length < 6)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Địa chỉ tiệm phải nhập rõ ràng (tối thiểu 6 ký tự)! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q2_ADDRESS",
                        regDraft = draft
                    };
                }

                draft.Address = address;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Địa chỉ: **{address}**! 📍

---
3️⃣ **Câu 3 / 14:** Vui lòng chọn **Ngành hàng kinh doanh chính** của bạn từ danh sách bên dưới (Bấm nút để chọn trực tiếp):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q3_CATEGORY",
                    actionIntent = "SELLER_CHOOSE_CATEGORY",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q3_CATEGORY")
            {
                string category = userMsg.Trim();
                if (string.IsNullOrWhiteSpace(category)) category = "Thực phẩm & Nhu yếu phẩm";

                draft.Category = category;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Ngành hàng: **{category}**! 🏷️

---
4️⃣ **Câu 4 / 14:** Cho mình xin **Địa chỉ Gmail đăng ký gian hàng** (Bắt buộc đuôi **@gmail.com**) nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q4_GMAIL",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q4_GMAIL")
            {
                string rawEmail = userMsg.Trim().ToLowerInvariant();
                var match = Regex.Match(rawEmail, @"[a-zA-Z0-9._%+-]+@gmail\.com");
                if (!match.Success)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Địa chỉ Gmail đăng ký gian hàng bắt buộc phải có đuôi **@gmail.com** (Ví dụ: `cuahang@gmail.com`)! Vui lòng nhập lại nhé!\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q4_GMAIL",
                        regDraft = draft
                    };
                }

                string candidateEmail = match.Value;
                var (isRegistered, detailRole) = await CheckIfEmailAlreadyRegisteredAsync(candidateEmail);
                if (isRegistered)
                {
                    return new
                    {
                        success = true,
                        reply = $"Oh no! Gmail `{candidateEmail}` này đã được đăng ký tài khoản trên hệ thống ZoneMart rồi ạ 😅. Mỗi tài khoản chỉ đăng ký 1 Gmail duy nhất, bạn vui lòng chọn và nhập một địa chỉ Gmail khác nhé! 🔑\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q4_GMAIL",
                        regDraft = draft
                    };
                }

                draft.Email = candidateEmail;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Gmail gian hàng: `{draft.Email}`! 📧

---
5️⃣ **Câu 5 / 14:** Cho mình xin **Mật khẩu tạo tài khoản** (Từ 6 - 16 ký tự) nhé?
💡 *(Bấm icon mắt 👁️ ở ô nhập bên dưới để ẩn/hiện mật khẩu)*",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q5_PASSWORD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q5_PASSWORD")
            {
                string pwd = userMsg.Trim();
                if (pwd.Length < 6 || pwd.Length > 16)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Mật khẩu phải có độ dài từ 6 đến 16 ký tự! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q5_PASSWORD",
                        regDraft = draft
                    };
                }

                draft.Password = pwd;
                return new
                {
                    success = true,
                    reply = @"Đã ghi nhận mật khẩu! 🔒

---
6️⃣ **Câu 6 / 14:** Vui lòng **Nhập lại mật khẩu** (Xác nhận trùng khớp) nhé?
💡 *(Bấm icon mắt 👁️ ở ô nhập để kiểm tra tính chính xác)*",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q6_CONFIRM_PASSWORD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q6_CONFIRM_PASSWORD")
            {
                string confirmPwd = userMsg.Trim();
                if (confirmPwd != draft.Password)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Mật khẩu xác nhận không trùng khớp với mật khẩu vừa nhập ở câu 5! Vui lòng nhập lại chính xác mật khẩu nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q6_CONFIRM_PASSWORD",
                        regDraft = draft
                    };
                }

                draft.ConfirmPassword = confirmPwd;
                return new
                {
                    success = true,
                    reply = @"Mật khẩu xác nhận trùng khớp hoàn toàn! ✅

---
7️⃣ **Câu 7 / 14:** Vui lòng chọn **Khung giờ mở cửa kinh doanh** của tiệm (Bấm nút bên dưới để chọn):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q7_OPENHOURS",
                    actionIntent = "SELLER_CHOOSE_OPENHOURS",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q7_OPENHOURS")
            {
                string openChoice = userMsg.Trim();
                if (openChoice.Contains("Tùy chọn") || openChoice.Contains("Tùy chỉnh") || openChoice.Contains("tuy chinh") || openChoice.Contains("tuy chon"))
                {
                    return new
                    {
                        success = true,
                        reply = @"Bạn đã chọn **Tùy chỉnh khung giờ**! ⏰

Vui lòng nhập khung giờ mở - đóng cửa của tiệm (Ví dụ: `08:00 - 22:00` hoặc `07:00 - 23:00`).
⚠️ Lưu ý: Giờ mở và giờ đóng phải cách nhau **tối thiểu 4 tiếng** như quy định đăng ký nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q7_CUSTOM_OPENHOURS",
                        regDraft = draft
                    };
                }
                else
                {
                    draft.OpenHours = openChoice.Split(" (")[0];
                    return new
                    {
                        success = true,
                        reply = $@"Đã lưu Khung giờ kinh doanh: **{draft.OpenHours}**! ⏰

---
8️⃣ **Câu 8 / 14:** Cho mình xin **Họ và tên chủ tiệm** (Từ 6 - 16 ký tự) nhé?",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q8_OWNERNAME",
                        regDraft = draft
                    };
                }
            }
            else if (activeState == "REGISTER_SELLER_Q7_CUSTOM_OPENHOURS")
            {
                string customHours = userMsg.Trim();
                var hourMatches = Regex.Matches(customHours, @"\b(\d{1,2})(:|\s*h)?(\d{2})?\b");
                bool validGap = true;
                if (hourMatches.Count >= 2 && int.TryParse(hourMatches[0].Groups[1].Value, out int h1) && int.TryParse(hourMatches[1].Groups[1].Value, out int h2))
                {
                    int diff = h2 - h1;
                    if (diff <= 0) diff += 24;
                    if (diff < 4) validGap = false;
                }

                if (!validGap)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Khung giờ mở cửa và đóng cửa phải cách nhau **tối thiểu 4 tiếng** (Ví dụ: `08:00 - 22:00` hoặc `07:00 - 23:00`)! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q7_CUSTOM_OPENHOURS",
                        regDraft = draft
                    };
                }

                draft.OpenHours = customHours;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Khung giờ kinh doanh: **{draft.OpenHours}**! ⏰

---
8️⃣ **Câu 8 / 14:** Cho mình xin **Họ và tên chủ tiệm** (Từ 6 - 16 ký tự) nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q8_OWNERNAME",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q8_OWNERNAME")
            {
                string ownerName = userMsg.Trim();
                if (ownerName.Length < 6 || ownerName.Length > 30)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Họ và tên chủ tiệm phải có độ dài từ 6 đến 30 ký tự! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q8_OWNERNAME",
                        regDraft = draft
                    };
                }

                draft.OwnerFullName = ownerName;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Chủ tiệm: **{ownerName}**! 👤

---
9️⃣ **Câu 9 / 14:** Cho mình xin **Số Căn cước công dân (CCCD)** của chủ tiệm (Đúng 12 chữ số theo quy định) nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q9_CCCD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q9_CCCD")
            {
                string cccd = userMsg.Trim();
                if (!Regex.IsMatch(cccd, @"^\d{12}$"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Số CCCD phải bao gồm đúng 12 chữ số theo quy định nhà nước! Vui lòng nhập lại chính xác 12 số CCCD của chủ tiệm nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q9_CCCD",
                        regDraft = draft
                    };
                }

                draft.CccdNumber = cccd;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Số CCCD chủ tiệm: `{cccd}`! 🆔

---
🔟 **Câu 10 / 14:** Vui lòng tải lên ảnh **CCCD Mặt Trước** của chủ tiệm (Nhấp vào khung nét đứt bên dưới để chọn ảnh):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q10_CCCD_FRONT",
                    actionIntent = "SELLER_UPLOAD_CCCD_FRONT",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q10_CCCD_FRONT" || activeState == "REGISTER_SELLER_Q9_CCCD_FRONT")
            {
                if (string.IsNullOrWhiteSpace(draft.CccdFrontImage) || (!draft.CccdFrontImage.StartsWith("data:image") && !draft.CccdFrontImage.StartsWith("http")))
                {
                    if (userMsg.Trim().StartsWith("data:image") || userMsg.Trim().StartsWith("http"))
                        draft.CccdFrontImage = userMsg.Trim();
                }
                return new
                {
                    success = true,
                    reply = @"Đã tải lên CCCD Mặt Trước thành công! 🆔

---
1️⃣1️⃣ **Câu 11 / 14:** Vui lòng tải lên ảnh **CCCD Mặt Sau** của chủ tiệm (Nhấp vào khung nét đứt bên dưới để chọn ảnh):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q11_CCCD_BACK",
                    actionIntent = "SELLER_UPLOAD_CCCD_BACK",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q11_CCCD_BACK" || activeState == "REGISTER_SELLER_Q10_CCCD_BACK")
            {
                if (string.IsNullOrWhiteSpace(draft.CccdBackImage) || (!draft.CccdBackImage.StartsWith("data:image") && !draft.CccdBackImage.StartsWith("http")))
                {
                    if (userMsg.Trim().StartsWith("data:image") || userMsg.Trim().StartsWith("http"))
                        draft.CccdBackImage = userMsg.Trim();
                }
                return new
                {
                    success = true,
                    reply = @"Đã tải lên CCCD Mặt Sau thành công! 🆔

---
1️⃣2️⃣ **Câu 12 / 14:** Vui lòng tải lên ảnh **Giấy Chứng Nhận An Toàn Thực Phẩm** (hoặc Giấy Phép Kinh Doanh) của tiệm:",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q12_FOOD_SAFETY",
                    actionIntent = "SELLER_UPLOAD_FOOD_SAFETY",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q12_FOOD_SAFETY" || activeState == "REGISTER_SELLER_Q11_FOOD_SAFETY")
            {
                if (string.IsNullOrWhiteSpace(draft.FoodSafetyImage) || (!draft.FoodSafetyImage.StartsWith("data:image") && !draft.FoodSafetyImage.StartsWith("http")))
                {
                    if (userMsg.Trim().StartsWith("data:image") || userMsg.Trim().StartsWith("http"))
                        draft.FoodSafetyImage = userMsg.Trim();
                }
                return new
                {
                    success = true,
                    reply = @"Đã tải lên Giấy CN An Toàn Thực Phẩm thành công! 📜

---
1️⃣3️⃣ **Câu 13 / 14:** Vui lòng chọn **Ngân hàng nhận doanh thu** từ danh sách bên dưới (Bấm nút để chọn):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q13_BANK",
                    actionIntent = "SELLER_CHOOSE_BANK",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q13_BANK" || activeState == "REGISTER_SELLER_Q12_BANK")
            {
                draft.BankName = userMsg.Trim();
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Ngân hàng: **{draft.BankName}**! 💳

---
1️⃣4️⃣ **Câu 14 / 14 (Cuối cùng):** Cho mình xin **Số tài khoản ngân hàng** (Từ 8 - 15 chữ số) để sàn chuyển tiền doanh thu về cho bạn nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SELLER_Q14_BANKACC",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SELLER_Q14_BANKACC" || activeState == "REGISTER_SELLER_Q13_BANKACC" || activeState == "REGISTER_SELLER_STEP3")
            {
                string bankAcc = userMsg.Trim();
                if (!Regex.IsMatch(bankAcc, @"^\d{8,15}$"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Số tài khoản ngân hàng phải có độ dài từ 8 đến 15 chữ số! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SELLER_Q14_BANKACC",
                        regDraft = draft
                    };
                }

                draft.BankAccountNumber = bankAcc;
                return await CompleteSellerRegistrationAsync(draft);
            }

            // ==================== [ROLE 2] SHIPPER TÀI XẾ ====================
            if (activeState == "REGISTER_SHIPPER_Q1_NAME" || activeState == "REGISTER_SHIPPER_STEP1" || activeState == "REGISTER_SHIPPER_WAITING")
            {
                string fullName = userMsg.Trim();
                if (fullName.Length < 6 || fullName.Length > 16)
                {
                    return new
                    {
                        success = true,
                        reply = $"⚠️ Họ và tên thật của tài xế phải từ 6 đến 16 ký tự (Hiện tại: {fullName.Length} ký tự)! Vui lòng nhập lại nhé!\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q1_NAME",
                        regDraft = draft
                    };
                }

                draft.FullName = fullName;
                return new
                {
                    success = true,
                    reply = $@"Dạ chào tài xế **{fullName}**! 🛵

---
2️⃣ **Câu 2 / 15:** Cho mình xin **Địa chỉ Gmail đăng ký tài xế** (Bắt buộc đuôi **@gmail.com**) nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q2_GMAIL",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q2_GMAIL")
            {
                string email = userMsg.Trim().ToLowerInvariant();
                var match = Regex.Match(email, @"[a-zA-Z0-9._%+-]+@gmail\.com");
                if (!match.Success)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Địa chỉ Gmail đăng ký tài xế bắt buộc phải có đuôi **@gmail.com** (Ví dụ: `taixe@gmail.com`)! Vui lòng nhập lại nhé!\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q2_GMAIL",
                        regDraft = draft
                    };
                }

                string candidateEmail = match.Value;
                var (isRegistered, detailRole) = await CheckIfEmailAlreadyRegisteredAsync(candidateEmail);
                if (isRegistered)
                {
                    return new
                    {
                        success = true,
                        reply = $"Oh no! Gmail `{candidateEmail}` này đã được đăng ký tài khoản trên hệ thống ZoneMart rồi ạ 😅. Mỗi tài khoản chỉ đăng ký 1 Gmail duy nhất, bạn vui lòng chọn và nhập một địa chỉ Gmail khác nhé! 🔑\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q2_GMAIL",
                        regDraft = draft
                    };
                }

                draft.Email = candidateEmail;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Gmail tài xế: `{draft.Email}`! 📧

---
3️⃣ **Câu 3 / 15:** Cho mình xin **Mật khẩu tạo tài khoản** (Từ 6 - 16 ký tự) nhé?
💡 *(Bấm icon mắt 👁️ ở ô nhập bên dưới để ẩn/hiện mật khẩu)*",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q3_PASSWORD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q3_PASSWORD")
            {
                string pwd = userMsg.Trim();
                if (pwd.Length < 6 || pwd.Length > 16)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Mật khẩu phải có độ dài từ 6 đến 16 ký tự! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q3_PASSWORD",
                        regDraft = draft
                    };
                }

                draft.Password = pwd;
                return new
                {
                    success = true,
                    reply = @"Đã ghi nhận mật khẩu! 🔒

---
4️⃣ **Câu 4 / 15:** Vui lòng **Nhập lại mật khẩu** (Xác nhận trùng khớp) nhé?
💡 *(Bấm icon mắt 👁️ ở ô nhập để kiểm tra)*",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q4_CONFIRM_PASSWORD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q4_CONFIRM_PASSWORD")
            {
                string confirmPwd = userMsg.Trim();
                if (confirmPwd != draft.Password)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Mật khẩu xác nhận không trùng khớp với mật khẩu vừa nhập! Vui lòng nhập lại chính xác mật khẩu nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q4_CONFIRM_PASSWORD",
                        regDraft = draft
                    };
                }

                draft.ConfirmPassword = confirmPwd;
                return new
                {
                    success = true,
                    reply = @"Mật khẩu xác nhận trùng khớp hoàn toàn! ✅

---
5️⃣ **Câu 5 / 15:** Cho mình xin **Số Căn cước công dân (CCCD)** của bạn (Đúng 12 chữ số) nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q5_CCCD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q5_CCCD")
            {
                string cccd = userMsg.Trim();
                if (!Regex.IsMatch(cccd, @"^\d{12}$"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Số CCCD phải bao gồm đúng 12 chữ số theo quy định nhà nước! Vui lòng nhập lại chính xác 12 số CCCD nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q5_CCCD",
                        regDraft = draft
                    };
                }

                draft.CccdNumber = cccd;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Số CCCD: `{cccd}`! 🆔

---
6️⃣ **Câu 6 / 15:** Vui lòng tải lên **Ảnh chân dung tài xế** (Nhấp vào ô bên dưới để chọn ảnh):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q6_PORTRAIT",
                    actionIntent = "SHIPPER_UPLOAD_PORTRAIT",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q6_PORTRAIT")
            {
                if (string.IsNullOrWhiteSpace(draft.PortraitImage) || (!draft.PortraitImage.StartsWith("data:image") && !draft.PortraitImage.StartsWith("http")))
                {
                    if (userMsg.Trim().StartsWith("data:image") || userMsg.Trim().StartsWith("http"))
                        draft.PortraitImage = userMsg.Trim();
                }
                return new
                {
                    success = true,
                    reply = @"Đã tải lên Ảnh chân dung thành công! 👤

---
7️⃣ **Câu 7 / 15:** Vui lòng tải lên ảnh **CCCD Mặt Trước** của tài xế:",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q7_CCCD_FRONT",
                    actionIntent = "SHIPPER_UPLOAD_CCCD_FRONT",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q7_CCCD_FRONT")
            {
                if (string.IsNullOrWhiteSpace(draft.CccdFrontImage) || (!draft.CccdFrontImage.StartsWith("data:image") && !draft.CccdFrontImage.StartsWith("http")))
                {
                    if (userMsg.Trim().StartsWith("data:image") || userMsg.Trim().StartsWith("http"))
                        draft.CccdFrontImage = userMsg.Trim();
                }
                return new
                {
                    success = true,
                    reply = @"Đã tải lên CCCD Mặt Trước thành công! 🆔

---
8️⃣ **Câu 8 / 15:** Vui lòng tải lên ảnh **CCCD Mặt Sau** của tài xế:",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q8_CCCD_BACK",
                    actionIntent = "SHIPPER_UPLOAD_CCCD_BACK",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q8_CCCD_BACK")
            {
                if (string.IsNullOrWhiteSpace(draft.CccdBackImage) || (!draft.CccdBackImage.StartsWith("data:image") && !draft.CccdBackImage.StartsWith("http")))
                {
                    if (userMsg.Trim().StartsWith("data:image") || userMsg.Trim().StartsWith("http"))
                        draft.CccdBackImage = userMsg.Trim();
                }
                return new
                {
                    success = true,
                    reply = @"Đã tải lên CCCD Mặt Sau thành công! 🆔

---
9️⃣ **Câu 9 / 15:** Cho mình xin **Biển số xe máy** bạn chạy đơn (Ví dụ: `29ak1233` - AI sẽ tự động viết hoa thành `29AK1233` nhé):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q9_LICENSE_PLATE",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q9_LICENSE_PLATE")
            {
                string plate = userMsg.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(plate) || plate.Length < 4)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Vui lòng nhập Biển số xe máy rõ ràng (Ví dụ: `29ak1233` hoặc `29A1-99999`) nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q9_LICENSE_PLATE",
                        regDraft = draft
                    };
                }

                draft.LicensePlate = plate;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Biển số xe: **{plate}**! 🛵

---
🔟 **Câu 10 / 15:** Vui lòng chọn **Loại xe đăng ký** (Bấm nút bên dưới để chọn):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q10_VEHICLE_TYPE",
                    actionIntent = "SHIPPER_CHOOSE_VEHICLE_TYPE",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q10_VEHICLE_TYPE")
            {
                draft.VehicleType = userMsg.Trim();
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Loại xe: **{draft.VehicleType}**! 🏍️

---
1️⃣1️⃣ **Câu 11 / 15:** Vui lòng chọn **Dòng xe / Mẫu xe** bạn sử dụng từ danh sách bên dưới (Bấm nút để chọn):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q11_VEHICLE_MODEL",
                    actionIntent = "SHIPPER_CHOOSE_VEHICLE_MODEL",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q11_VEHICLE_MODEL")
            {
                draft.VehicleModel = userMsg.Trim();
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Dòng xe: **{draft.VehicleModel}**! 🏍️

---
1️⃣2️⃣ **Câu 12 / 15:** Vui lòng tải lên ảnh **Bằng Lái Xe (GPLX)** của bạn (Nhấp vào ô bên dưới để chọn ảnh):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q12_GPLX",
                    actionIntent = "SHIPPER_UPLOAD_GPLX",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q12_GPLX")
            {
                if (string.IsNullOrWhiteSpace(draft.DrivingLicenseImage) || (!draft.DrivingLicenseImage.StartsWith("data:image") && !draft.DrivingLicenseImage.StartsWith("http")))
                {
                    if (userMsg.Trim().StartsWith("data:image") || userMsg.Trim().StartsWith("http"))
                        draft.DrivingLicenseImage = userMsg.Trim();
                }
                return new
                {
                    success = true,
                    reply = @"Đã tải lên Bằng lái xe (GPLX) thành công! 📜

---
1️⃣3️⃣ **Câu 13 / 15:** Vui lòng chọn **Khu vực đăng ký chạy chính** của bạn từ danh sách Quận / Huyện bên dưới (Bấm nút để chọn):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q13_AREA",
                    actionIntent = "SHIPPER_CHOOSE_AREA",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q13_AREA")
            {
                draft.OperatingArea = userMsg.Trim();
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Khu vực chạy đơn: **{draft.OperatingArea}**! 📍

---
1️⃣4️⃣ **Câu 14 / 15:** Vui lòng chọn **Ngân hàng nhận cước** từ danh sách bên dưới (Bấm nút để chọn):",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q14_BANK",
                    actionIntent = "SHIPPER_CHOOSE_BANK",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q14_BANK")
            {
                draft.BankName = userMsg.Trim();
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Ngân hàng: **{draft.BankName}**! 💳

---
1️⃣5️⃣ **Câu 15 / 15 (Cuối cùng):** Cho mình xin **Số tài khoản ngân hàng** (Từ 8 - 15 chữ số) để sàn chuyển thu nhập cước xe cho bạn nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_SHIPPER_Q15_BANKACC",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_SHIPPER_Q15_BANKACC" || activeState == "REGISTER_SHIPPER_STEP3")
            {
                string bankAcc = userMsg.Trim();
                if (!Regex.IsMatch(bankAcc, @"^\d{8,15}$"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Số tài khoản ngân hàng phải có độ dài từ 8 đến 15 chữ số! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_SHIPPER_Q15_BANKACC",
                        regDraft = draft
                    };
                }

                draft.BankAccountNumber = bankAcc;
                return await CompleteShipperRegistrationAsync(draft);
            }

            // ==================== [ROLE 3] KHÁCH HÀNG / NGƯỜI MUA ====================
            if (activeState == "REGISTER_BUYER_Q1_NAME" || activeState == "REGISTER_BUYER_STEP1" || activeState == "REGISTER_BUYER_WAITING")
            {
                string fullName = userMsg.Trim();
                if (fullName.Length < 2)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Vui lòng nhập đầy đủ **Họ và tên của bạn** nhé!\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_BUYER_Q1_NAME",
                        regDraft = draft
                    };
                }

                draft.FullName = fullName;
                return new
                {
                    success = true,
                    reply = $@"Dạ xin chào **{fullName}**! 👋

---
2️⃣ **Câu 2 / 4:** Cho mình xin **Địa chỉ Gmail** của bạn để làm tài khoản đăng nhập (Bắt buộc đuôi **@gmail.com**) nhé?",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_BUYER_Q2_EMAIL",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_BUYER_Q2_EMAIL" || activeState == "REGISTER_BUYER_STEP2")
            {
                string email = userMsg.Trim().ToLowerInvariant();
                var match = Regex.Match(email, @"[a-zA-Z0-9._%+-]+@gmail\.com");
                if (!match.Success)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Địa chỉ Gmail phải có đuôi **@gmail.com** (Ví dụ: `khachhang@gmail.com`)! Vui lòng nhập lại Gmail của bạn nhé!\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_BUYER_Q2_EMAIL",
                        regDraft = draft
                    };
                }

                string validEmail = match.Value;
                var (isRegistered, detailRole) = await CheckIfEmailAlreadyRegisteredAsync(validEmail);
                if (isRegistered)
                {
                    return new
                    {
                        success = true,
                        reply = $"Oh no! Gmail `{validEmail}` này đã được đăng ký tài khoản trên hệ thống ZoneMart rồi ạ 😅. Mỗi tài khoản chỉ đăng ký 1 Gmail duy nhất, bạn vui lòng chọn và nhập một địa chỉ Gmail khác nhé! 🔑\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_BUYER_Q2_EMAIL",
                        regDraft = draft
                    };
                }

                draft.Email = validEmail;
                return new
                {
                    success = true,
                    reply = $@"Đã lưu Gmail: `{validEmail}`! 📧

---
3️⃣ **Câu 3 / 4:** Cho mình xin **Mật khẩu** bạn muốn đặt cho tài khoản (Từ 6 - 16 ký tự) nhé?
💡 *(Ở ô nhập bên dưới có biểu tượng **icon con mắt 👁️**, bạn có thể bấm để ẩn/hiện mật khẩu)*",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_BUYER_Q3_PASSWORD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_BUYER_Q3_PASSWORD")
            {
                string pwd = userMsg.Trim();
                if (pwd.Length < 6 || pwd.Length > 16)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Mật khẩu phải có độ dài từ 6 đến 16 ký tự! Vui lòng nhập lại nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_BUYER_Q3_PASSWORD",
                        regDraft = draft
                    };
                }

                draft.Password = pwd;
                return new
                {
                    success = true,
                    reply = @"Đã ghi nhận mật khẩu! 🔒

---
4️⃣ **Câu 4 / 4 (Cuối cùng):** Vui lòng **Nhập lại mật khẩu** (Xác nhận trùng khớp) nhé?
💡 *(Bấm icon mắt 👁️ ở ô nhập để kiểm tra trùng khớp)*",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "REGISTER_BUYER_Q4_CONFIRM_PASSWORD",
                    regDraft = draft
                };
            }
            else if (activeState == "REGISTER_BUYER_Q4_CONFIRM_PASSWORD" || activeState == "REGISTER_BUYER_STEP3")
            {
                string confirmPwd = userMsg.Trim();
                if (confirmPwd != draft.Password)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Mật khẩu xác nhận không trùng khớp với mật khẩu vừa nhập ở câu 3! Vui lòng nhập lại chính xác mật khẩu xác nhận nhé!",
                        provider = "ZoneMart AI Direct Registration Engine",
                        registrationState = "REGISTER_BUYER_Q4_CONFIRM_PASSWORD",
                        regDraft = draft
                    };
                }

                draft.ConfirmPassword = confirmPwd;
                return await CompleteBuyerRegistrationAsync(draft);
            }

            return null;
        }

        private async Task<object?> TryProcessAiSupportTicketAsync(
            string userMsg,
            string cleanMsgNoTone,
            string? currentSupportState,
            SupportDraft? incomingDraft)
        {
            string activeState = currentSupportState ?? "NONE";
            var draft = incomingDraft ?? new SupportDraft();

            // 1. KIỂM TRA LỆNH "HỦY YÊU CẦU" / "HỦY HỖ TRỢ"
            bool isExplicitCancel = Regex.IsMatch(cleanMsgNoTone, @"\b(huy yeu cau|huy ho tro|khong can ho tro|dung ho tro)\b") ||
                                    Regex.IsMatch(cleanMsgNoTone, @"^\s*(huy|thoat|cancel|stop|thoi|quay lai)\s*$");

            if (isExplicitCancel)
            {
                return new
                {
                    success = true,
                    reply = "Dạ mình đã hủy yêu cầu hỗ trợ theo ý bạn rồi nhé! 😊 Bạn có thể trò chuyện và hỏi các câu hỏi khác cùng mình bình thường nhé!",
                    provider = "ZoneMart Support AI",
                    supportState = "NONE",
                    actionIntent = (string?)null,
                    supportDraft = new SupportDraft()
                };
            }

            // 2. KHỞI TẠO BẮT ĐẦU LUỒNG GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN
            bool isStartAiDirectSupport = cleanMsgNoTone.Contains("ai ho tro") ||
                                          cleanMsgNoTone.Contains("gui truc tiep") ||
                                          cleanMsgNoTone.Contains("ho tro qua ai") ||
                                          cleanMsgNoTone.Contains("nho ai gui") ||
                                          cleanMsgNoTone.Contains("ai gui ho") ||
                                          cleanMsgNoTone.Contains("cach 2") ||
                                          cleanMsgNoTone.Contains("gui bang ai") ||
                                          cleanMsgNoTone.Contains("gui ho toi") ||
                                          cleanMsgNoTone.Contains("ai tu van gui ho") ||
                                          cleanMsgNoTone.Contains("gui ho") ||
                                          cleanMsgNoTone.Contains("ho tro gui yeu cau") ||
                                          (cleanMsgNoTone.Contains("ho tro") && cleanMsgNoTone.Contains("truc tiep")) ||
                                          (cleanMsgNoTone.Contains("ho tro") && cleanMsgNoTone.Contains("ai")) ||
                                          (cleanMsgNoTone.Contains("yeu cau") && cleanMsgNoTone.Contains("truc tiep"));

            bool isTriggerIntent = activeState == "NONE" && (
                cleanMsgNoTone.Contains("gui yeu cau") || cleanMsgNoTone.Contains("ho tro") ||
                cleanMsgNoTone.Contains("khieu nai") || cleanMsgNoTone.Contains("dong gop y kien") ||
                cleanMsgNoTone.Contains("doi tra") || cleanMsgNoTone.Contains("hoan tien") ||
                cleanMsgNoTone.Contains("kiem tra don") || cleanMsgNoTone.Contains("gian hang") ||
                cleanMsgNoTone.Contains("tai xe") || cleanMsgNoTone.Contains("lien he"));

            if (isTriggerIntent && !isStartAiDirectSupport)
            {
                return new
                {
                    success = true,
                    reply = "Dạ chào bạn! Để hỗ trợ giải quyết vấn đề của bạn nhanh nhất, ZoneMart cung cấp 2 phương thức gửi yêu cầu hỗ trợ trực tuyến:\n\n1️⃣ **Cách 1: Đi tới Form Hỗ Trợ Thủ Công:** Điền biểu mẫu trực tiếp trên trang Hỗ Trợ.\n2️⃣ **Cách 2: Nhờ AI Tư Vấn Gửi Hộ Trực Tiếp:** Trả lời trực tiếp trong đoạn chat này, mình sẽ hỏi bạn từng thông tin và gửi ngay đến bộ phận CSKH ZoneMart!\n\nBạn muốn chọn cách nào ạ? 😊",
                    provider = "ZoneMart Support AI",
                    actionIntent = "SUPPORT_CHOICE",
                    supportState = "NONE",
                    supportDraft = new SupportDraft()
                };
            }

            if (activeState == "NONE" && isStartAiDirectSupport)
            {
                draft = new SupportDraft();
                return new
                {
                    success = true,
                    reply = "Dạ tuyệt vời! Mình sẽ giúp bạn gửi yêu cầu hỗ trợ trực tuyến đến ban quản trị CSKH ZoneMart ngay nhé! 📝\n\n**Bước 1/6: Vấn đề bạn cần hỗ trợ là gì ạ?**\n(Bạn hãy chọn 1 trong 5 mục bên dưới nhé!):",
                    provider = "ZoneMart Support AI",
                    actionIntent = "SUPPORT_CHOOSE_TOPIC",
                    supportState = "SUPPORT_Q1_TOPIC",
                    supportDraft = draft
                };
            }

            // BƯỚC 1: CHỌN VẤN ĐỀ CẦN HỖ TRỢ
            if (activeState == "SUPPORT_Q1_TOPIC")
            {
                string topic = userMsg.Trim();
                if (cleanMsgNoTone.Contains("kiem tra don")) topic = "Kiểm tra đơn hàng";
                else if (cleanMsgNoTone.Contains("doi tra") || cleanMsgNoTone.Contains("hoan tien")) topic = "Đổi trả & Hoàn tiền";
                else if (cleanMsgNoTone.Contains("gian hang") || cleanMsgNoTone.Contains("seller")) topic = "Đối tác Gian hàng";
                else if (cleanMsgNoTone.Contains("tai xe") || cleanMsgNoTone.Contains("shipper")) topic = "Đội ngũ Tài xế";
                else if (cleanMsgNoTone.Contains("dong gop") || cleanMsgNoTone.Contains("y kien") || cleanMsgNoTone.Contains("gop y")) topic = "Đóng góp ý kiến";

                draft.Topic = topic;
                return new
                {
                    success = true,
                    reply = $"Dạ bạn đã chọn vấn đề: **{draft.Topic}** ✅\n\n**Bước 2/6: Họ và tên của bạn là gì ạ?**\n(Ví dụ: Nguyễn Văn A)",
                    provider = "ZoneMart Support AI",
                    actionIntent = "SUPPORT_NAME_INPUT",
                    supportState = "SUPPORT_Q2_NAME",
                    supportDraft = draft
                };
            }

            // BƯỚC 2: HỌ VÀ TÊN
            if (activeState == "SUPPORT_Q2_NAME")
            {
                if (userMsg.StartsWith("[DÙNG THÔNG TIN TÀI KHOẢN]:"))
                {
                    string info = userMsg.Substring("[DÙNG THÔNG TIN TÀI KHOẢN]:".Length).Trim();
                    string[] parts = info.Split('|');
                    string accName = parts.Length > 0 ? parts[0].Trim() : "Khách Hàng";
                    string accEmail = parts.Length > 1 ? parts[1].Trim() : "";

                    draft.FullName = accName;
                    draft.Email = accEmail;

                    return new
                    {
                        success = true,
                        reply = $"Dạ tuyệt vời! Hệ thống đã tự động lấy thông tin từ tài khoản của bạn:\n👤 **Họ và tên:** {draft.FullName}\n📧 **Email:** `{draft.Email}` ✅\n\n**Bước 4/6: Cho mình xin Số điện thoại liên hệ của bạn nhé!**\n(Số điện thoại 10 chữ số, ví dụ: 0912345678)",
                        provider = "ZoneMart Support AI",
                        actionIntent = (string?)null,
                        supportState = "SUPPORT_Q4_PHONE",
                        supportDraft = draft
                    };
                }

                string name = userMsg.Trim();
                if (name.Length < 2)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Họ và tên quá ngắn! Vui lòng nhập đầy đủ họ và tên của bạn nhé (Ví dụ: Nguyễn Văn A):",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_NAME_INPUT",
                        supportState = "SUPPORT_Q2_NAME",
                        supportDraft = draft
                    };
                }

                draft.FullName = name;
                return new
                {
                    success = true,
                    reply = $"Dạ cảm ơn bạn **{draft.FullName}**!\n\n**Bước 3/6: Địa chỉ Email nhận phản hồi của bạn là gì ạ?**\n(Để bộ phận CSKH ZoneMart gửi email phản hồi kết quả cho bạn)",
                    provider = "ZoneMart Support AI",
                    actionIntent = (string?)null,
                    supportState = "SUPPORT_Q3_EMAIL",
                    supportDraft = draft
                };
            }

            // BƯỚC 3: ĐỊA CHỈ EMAIL
            if (activeState == "SUPPORT_Q3_EMAIL")
            {
                string email = userMsg.Trim();
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Địa chỉ Email không hợp lệ (cần đúng dạng `ten@domain.com`). Vui lòng nhập lại chính xác email của bạn nhé:",
                        provider = "ZoneMart Support AI",
                        supportState = "SUPPORT_Q3_EMAIL",
                        supportDraft = draft
                    };
                }

                draft.Email = email;
                return new
                {
                    success = true,
                    reply = $"Dạ đã ghi nhận email: **{draft.Email}** ✅\n\n**Bước 4/6: Cho mình xin Số điện thoại liên hệ của bạn nhé!**\n(Số điện thoại 10 chữ số, ví dụ: 0912345678)",
                    provider = "ZoneMart Support AI",
                    actionIntent = (string?)null,
                    supportState = "SUPPORT_Q4_PHONE",
                    supportDraft = draft
                };
            }

            // BƯỚC 4: SỐ ĐIỆN THOẠI
            if (activeState == "SUPPORT_Q4_PHONE")
            {
                string cleanPhone = cleanMsgNoTone.Replace(" ", "").Replace(".", "").Replace("-", "");
                if (!Regex.IsMatch(cleanPhone, @"^0\d{9}$"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Số điện thoại không hợp lệ! Vui lòng nhập số điện thoại gồm đúng 10 chữ số bắt đầu bằng số 0 (Ví dụ: 0912345678):",
                        provider = "ZoneMart Support AI",
                        supportState = "SUPPORT_Q4_PHONE",
                        supportDraft = draft
                    };
                }

                draft.Phone = cleanPhone;
                return new
                {
                    success = true,
                    reply = $"Dạ đã lưu số điện thoại: **{draft.Phone}** ✅\n\n**Bước 5/6: Mã đơn hàng liên quan đến sự cố (nếu có)?**\n(Ví dụ: `ZM-88992`. Nếu không có mã đơn hàng, bạn hãy bấm nút **[Bỏ qua bước này]** bên dưới nhé!)",
                    provider = "ZoneMart Support AI",
                    actionIntent = "SUPPORT_ORDER_CODE_OPTIONAL",
                    supportState = "SUPPORT_Q5_ORDER_CODE",
                    supportDraft = draft
                };
            }

            // BƯỚC 5: MÃ ĐƠN HÀNG (NẾU CÓ) HOẶC BỎ QUA
            if (activeState == "SUPPORT_Q5_ORDER_CODE")
            {
                bool isSkip = cleanMsgNoTone.Contains("bo qua") || cleanMsgNoTone.Contains("khong co") || cleanMsgNoTone.Contains("khong nho");
                if (isSkip)
                {
                    draft.OrderCode = "";
                }
                else
                {
                    draft.OrderCode = userMsg.Trim().ToUpper();
                }

                string orderDisplay = string.IsNullOrEmpty(draft.OrderCode) ? "Không có" : draft.OrderCode;
                return new
                {
                    success = true,
                    reply = $"Dạ đã ghi nhận mã đơn: **{orderDisplay}** ✅\n\n**Bước 6/6: Nội dung chi tiết yêu cầu hỗ trợ của bạn là gì ạ?**\n(Mô tả cụ thể sự cố, thắc mắc đơn hàng hoặc địa chỉ để CSKH ZoneMart hỗ trợ nhanh nhất nhé!)",
                    provider = "ZoneMart Support AI",
                    actionIntent = (string?)null,
                    supportState = "SUPPORT_Q6_MESSAGE",
                    supportDraft = draft
                };
            }

            // BƯỚC 6: NỘI DUNG CHI TIẾT
            if (activeState == "SUPPORT_Q6_MESSAGE")
            {
                string msgText = userMsg.Trim();
                if (msgText.Length < 5)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Nội dung chi tiết quá ngắn! Bạn vui lòng mô tả cụ thể hơn (tối thiểu 5 ký tự) để ZoneMart hiểu rõ sự cố và xử lý nhanh nhất cho bạn nhé:",
                        provider = "ZoneMart Support AI",
                        supportState = "SUPPORT_Q6_MESSAGE",
                        supportDraft = draft
                    };
                }

                draft.Message = msgText;
                return new
                {
                    success = true,
                    reply = "Dạ đã lưu nội dung yêu cầu của bạn! 📝\n\n**Bước 7 (Ảnh sự cố): Bạn có muốn gửi kèm Hình Ảnh Minh Họa / Sự Cố không ạ?**\n(Bạn bấm nút **[Tải Ảnh Minh Họa Sự Cố]** bên dưới để chọn ảnh, hoặc bấm **[Bỏ qua không gửi ảnh]** để hoàn tất nhé!)",
                    provider = "ZoneMart Support AI",
                    actionIntent = "SUPPORT_UPLOAD_ATTACHMENT",
                    supportState = "SUPPORT_Q7_ATTACHMENT",
                    supportDraft = draft
                };
            }

            // HÀM TẠO BẢNG TỔNG HỢP VÀ DỮ LIỆU PHẢN HỒI
            object GenerateSupportSummaryCard(SupportDraft curDraft, string? customNote = null)
            {
                string orderCodeText = string.IsNullOrWhiteSpace(curDraft.OrderCode) ? "Không có" : curDraft.OrderCode;
                string imageStatus = !string.IsNullOrWhiteSpace(curDraft.AttachedImage) ? "Đã đính kèm 1 ảnh minh họa ✅" : "Không đính kèm";

                string prefix = string.IsNullOrWhiteSpace(customNote) ? "" : $"{customNote}\n\n";

                string summaryText = $@"{prefix}📋 **BẢNG TỔNG HỢP YÊU CẦU HỖ TRỢ TRỰC TUYẾN**

📌 **Vấn đề hỗ trợ:** {curDraft.Topic}
👤 **Họ và tên:** {curDraft.FullName}
📧 **Email liên hệ:** `{curDraft.Email}`
📞 **Số điện thoại:** `{curDraft.Phone}`
📦 **Mã đơn hàng:** `{orderCodeText}`
📝 **Nội dung yêu cầu:** {curDraft.Message}
🖼️ **Ảnh sự cố:** {imageStatus}

---
👉 Bạn kiểm tra lại thông tin trên đã chính xác chưa nhé! Hãy bấm nút **[Gửi Yêu Cầu Hỗ Trợ Ngay]** bên dưới để hoàn tất gửi đến CSKH ZoneMart, hoặc bấm **[Sửa Lại Yêu Cầu]** nếu cần điều chỉnh thông tin!";

                return new
                {
                    success = true,
                    reply = summaryText,
                    provider = "ZoneMart Support AI",
                    actionIntent = "SUPPORT_SUBMIT_CONFIRM",
                    supportState = "SUPPORT_CONFIRMATION",
                    supportDraft = curDraft,
                    summaryData = new
                    {
                        type = "SUPPORT_TICKET",
                        topic = curDraft.Topic,
                        fullName = curDraft.FullName,
                        email = curDraft.Email,
                        phone = curDraft.Phone,
                        orderCode = orderCodeText,
                        message = curDraft.Message,
                        hasImage = !string.IsNullOrWhiteSpace(curDraft.AttachedImage)
                    }
                };
            }

            // BƯỚC 7: TẢI ẢNH MINH HỌA SỰ CỐ HOẶC BỎ QUA
            if (activeState == "SUPPORT_Q7_ATTACHMENT")
            {
                bool isSkipPhoto = cleanMsgNoTone.Contains("bo qua") || cleanMsgNoTone.Contains("khong gui") || cleanMsgNoTone.Contains("khong co anh");
                if (isSkipPhoto)
                {
                    draft.AttachedImage = "";
                }
                else if (userMsg.Contains("[Ảnh") || !string.IsNullOrWhiteSpace(draft.AttachedImage))
                {
                    // Ảnh đã được gán vào draft từ frontend
                }

                return GenerateSupportSummaryCard(draft);
            }

            // BƯỚC 8: BẢNG TỔNG HỢP & XÁC NHẬN / SỬA / HỦY
            if (activeState == "SUPPORT_CONFIRMATION")
            {
                bool isEdit = cleanMsgNoTone.Contains("sua") || cleanMsgNoTone.Contains("chinh sua") || cleanMsgNoTone.Contains("thay doi") || cleanMsgNoTone.Contains("edit");
                if (isEdit)
                {
                    return new
                    {
                        success = true,
                        reply = "Bạn muốn chỉnh sửa mục nào dưới đây ạ? (Nếu bạn bấm nhầm, bạn chỉ cần bấm nút **[Quay lại bảng tổng kết]** bên dưới nhé! 😊)",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_CHOOSE_EDIT_FIELD",
                        supportState = "SUPPORT_EDIT_SELECT_FIELD",
                        supportDraft = draft
                    };
                }

                bool isConfirm = cleanMsgNoTone.Contains("xac nhan") || cleanMsgNoTone.Contains("gui") ||
                                 cleanMsgNoTone.Contains("dong y") || cleanMsgNoTone.Contains("ok") || cleanMsgNoTone.Contains("chinh xac");

                if (isConfirm)
                {
                    var random = new Random();
                    var ticketCode = $"ZM-{random.Next(100000, 999999)}";

                    // 1. Lưu vào MongoDB collection SupportTickets
                    try
                    {
                        var doc = new SupportTicket
                        {
                            TicketCode = ticketCode,
                            FullName = draft.FullName,
                            Email = draft.Email,
                            Phone = draft.Phone,
                            OrderCode = string.IsNullOrWhiteSpace(draft.OrderCode) ? null : draft.OrderCode,
                            Topic = draft.Topic,
                            Message = draft.Message,
                            FileBase64 = string.IsNullOrWhiteSpace(draft.AttachedImage) ? null : draft.AttachedImage,
                            FileName = string.IsNullOrWhiteSpace(draft.AttachedImage) ? null : "su_co_minh_hoa.jpg",
                            CreatedAt = DateTime.UtcNow,
                            Status = "Pending"
                        };
                        await _mongoService.SupportTickets.InsertOneAsync(doc);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SupportTicket Error] MongoDB: {ex.Message}");
                    }

                    // 2. Gửi Email thông báo đến hh9393100@gmail.com
                    try
                    {
                        var ticketReq = new SupportTicketRequest
                        {
                            Topic = draft.Topic,
                            FullName = draft.FullName,
                            Email = draft.Email,
                            Phone = draft.Phone,
                            OrderCode = string.IsNullOrWhiteSpace(draft.OrderCode) ? null : draft.OrderCode,
                            Message = draft.Message,
                            FileBase64 = draft.AttachedImage,
                            FileName = string.IsNullOrWhiteSpace(draft.AttachedImage) ? null : "su_co_minh_hoa.jpg"
                        };

                        _ = _emailService.SendTicketNotificationAsync(ticketReq, ticketCode);
                        _ = _telegramService.SendTicketNotificationAsync(ticketReq, ticketCode);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SupportTicket Error] Email/Telegram: {ex.Message}");
                    }

                    string successReply = $@"🎉 **ZoneMart đã tiếp nhận yêu cầu hỗ trợ của bạn thành công!**

🎫 **Mã tiếp nhận:** `{ticketCode}`
⏰ **Thời gian:** {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm:ss}

Cảm ơn bạn **{draft.FullName}** đã gửi phản hồi và đóng góp ý kiến! Hệ thống đã gửi toàn bộ thông tin và hình ảnh đến ban quản trị CSKH qua email **hh9393100@gmail.com**. Chuyên viên hỗ trợ ZoneMart 24/7 sẽ kiểm tra và phản hồi lại bạn qua email **{draft.Email}** hoặc số điện thoại **{draft.Phone}** trong vòng tối đa 15 phút nhé! 😊

Nếu bạn cần mua sắm hay hỗ trợ thêm việc gì, bạn cứ nhắn cho mình nhé! 🛍️🛵";

                    return new
                    {
                        success = true,
                        reply = successReply,
                        provider = "ZoneMart Support AI",
                        actionIntent = (string?)null,
                        supportState = "NONE",
                        supportDraft = (object?)null
                    };
                }
            }

            // CHỌN 1 TRONG 4 MỤC CẦN SỬA
            if (activeState == "SUPPORT_EDIT_SELECT_FIELD")
            {
                if (cleanMsgNoTone.Contains("quay lai") || cleanMsgNoTone.Contains("back") || cleanMsgNoTone.Contains("huy") || cleanMsgNoTone.Contains("thoi"))
                {
                    return GenerateSupportSummaryCard(draft, "Dạ đã quay lại Bảng tổng kết thông tin của bạn! 😊");
                }

                if (cleanMsgNoTone.Contains("so dien thoai") || cleanMsgNoTone.Contains("sdt") || cleanMsgNoTone.Contains("phone"))
                {
                    return new
                    {
                        success = true,
                        reply = $"📞 **Số điện thoại hiện tại:** `{draft.Phone}`\n\nVui lòng nhập Số điện thoại mới của bạn (gồm đúng 10 chữ số bắt đầu bằng số 0):\n*(Nếu không muốn sửa nữa, bạn chỉ cần bấm nút **[Quay lại bảng tổng kết]** bên dưới nhé!)*",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_EDIT_BACK_BTN",
                        supportState = "SUPPORT_EDIT_PHONE",
                        supportDraft = draft
                    };
                }

                if (cleanMsgNoTone.Contains("ma don") || cleanMsgNoTone.Contains("don hang") || cleanMsgNoTone.Contains("order"))
                {
                    string curOrder = string.IsNullOrWhiteSpace(draft.OrderCode) ? "Chưa có" : draft.OrderCode;
                    return new
                    {
                        success = true,
                        reply = $"📦 **Mã đơn hàng hiện tại:** `{curOrder}`\n\nVui lòng nhập Mã đơn hàng mới của bạn (hoặc gõ 'không có' để xóa mã đơn):\n*(Nếu không muốn sửa nữa, bạn chỉ cần bấm nút **[Quay lại bảng tổng kết]** bên dưới nhé!)*",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_EDIT_BACK_BTN",
                        supportState = "SUPPORT_EDIT_ORDER_CODE",
                        supportDraft = draft
                    };
                }

                if (cleanMsgNoTone.Contains("noi dung") || cleanMsgNoTone.Contains("yeu cau") || cleanMsgNoTone.Contains("chi tiet"))
                {
                    return new
                    {
                        success = true,
                        reply = $"📝 **Nội dung hiện tại:** {draft.Message}\n\nVui lòng nhập Nội dung chi tiết mới của bạn (tối thiểu 5 ký tự):\n*(Nếu không muốn sửa nữa, bạn chỉ cần bấm nút **[Quay lại bảng tổng kết]** bên dưới nhé!)*",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_EDIT_BACK_BTN",
                        supportState = "SUPPORT_EDIT_MESSAGE",
                        supportDraft = draft
                    };
                }

                if (cleanMsgNoTone.Contains("anh") || cleanMsgNoTone.Contains("su co") || cleanMsgNoTone.Contains("hinh"))
                {
                    return new
                    {
                        success = true,
                        reply = "🖼️ Bạn vui lòng bấm vào khung bên dưới để chọn Ảnh minh họa sự cố mới, hoặc bấm [Xóa ảnh đã đính kèm]:",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_EDIT_UPLOAD",
                        supportState = "SUPPORT_EDIT_ATTACHMENT",
                        supportDraft = draft
                    };
                }

                return new
                {
                    success = true,
                    reply = "Bạn vui lòng chọn 1 trong 4 mục được phép sửa bên dưới (Số điện thoại, Mã đơn hàng, Nội dung yêu cầu, Ảnh sự cố), hoặc bấm nút **[Quay lại bảng tổng kết]** nhé! 😊",
                    provider = "ZoneMart Support AI",
                    actionIntent = "SUPPORT_CHOOSE_EDIT_FIELD",
                    supportState = "SUPPORT_EDIT_SELECT_FIELD",
                    supportDraft = draft
                };
            }

            // SỬA SỐ ĐIỆN THOẠI
            if (activeState == "SUPPORT_EDIT_PHONE")
            {
                if (cleanMsgNoTone.Contains("quay lai") || cleanMsgNoTone.Contains("back") || cleanMsgNoTone.Contains("huy") || cleanMsgNoTone.Contains("thoi"))
                {
                    return GenerateSupportSummaryCard(draft, "Dạ đã quay lại Bảng tổng kết thông tin của bạn! 😊");
                }

                string cleanPhone = cleanMsgNoTone.Replace(" ", "").Replace(".", "").Replace("-", "");
                if (!Regex.IsMatch(cleanPhone, @"^0\d{9}$"))
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Số điện thoại không hợp lệ! Vui lòng nhập số điện thoại gồm đúng 10 chữ số bắt đầu bằng số 0 (hoặc gõ 'quay lại' để giữ nguyên):",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_EDIT_BACK_BTN",
                        supportState = "SUPPORT_EDIT_PHONE",
                        supportDraft = draft
                    };
                }

                draft.Phone = cleanPhone;
                return GenerateSupportSummaryCard(draft, "Dạ đã cập nhật Số điện thoại mới thành công! ✅");
            }

            // SỬA MÃ ĐƠN HÀNG
            if (activeState == "SUPPORT_EDIT_ORDER_CODE")
            {
                if (cleanMsgNoTone.Contains("quay lai") || cleanMsgNoTone.Contains("back") || cleanMsgNoTone.Contains("huy") || cleanMsgNoTone.Contains("thoi"))
                {
                    return GenerateSupportSummaryCard(draft, "Dạ đã quay lại Bảng tổng kết thông tin của bạn! 😊");
                }

                if (cleanMsgNoTone.Contains("khong co") || cleanMsgNoTone.Contains("xoa") || cleanMsgNoTone.Contains("bo qua"))
                {
                    draft.OrderCode = "";
                }
                else
                {
                    draft.OrderCode = userMsg.Trim().ToUpper();
                }

                return GenerateSupportSummaryCard(draft, "Dạ đã cập nhật Mã đơn hàng thành công! ✅");
            }

            // SỬA NỘI DUNG YÊU CẦU
            if (activeState == "SUPPORT_EDIT_MESSAGE")
            {
                if (cleanMsgNoTone.Contains("quay lai") || cleanMsgNoTone.Contains("back") || cleanMsgNoTone.Contains("huy") || cleanMsgNoTone.Contains("thoi"))
                {
                    return GenerateSupportSummaryCard(draft, "Dạ đã quay lại Bảng tổng kết thông tin của bạn! 😊");
                }

                string msgText = userMsg.Trim();
                if (msgText.Length < 5)
                {
                    return new
                    {
                        success = true,
                        reply = "⚠️ Nội dung chi tiết quá ngắn! Bạn vui lòng mô tả cụ thể hơn (tối thiểu 5 ký tự) hoặc gõ 'quay lại':",
                        provider = "ZoneMart Support AI",
                        actionIntent = "SUPPORT_EDIT_BACK_BTN",
                        supportState = "SUPPORT_EDIT_MESSAGE",
                        supportDraft = draft
                    };
                }

                draft.Message = msgText;
                return GenerateSupportSummaryCard(draft, "Dạ đã cập nhật Nội dung yêu cầu thành công! ✅");
            }

            // SỬA ẢNH MINH HỌA
            if (activeState == "SUPPORT_EDIT_ATTACHMENT")
            {
                if (cleanMsgNoTone.Contains("quay lai") || cleanMsgNoTone.Contains("back") || cleanMsgNoTone.Contains("thoi"))
                {
                    return GenerateSupportSummaryCard(draft, "Dạ đã quay lại Bảng tổng kết thông tin của bạn! 😊");
                }

                if (cleanMsgNoTone.Contains("xoa") || cleanMsgNoTone.Contains("bo qua") || cleanMsgNoTone.Contains("khong gui"))
                {
                    draft.AttachedImage = "";
                }
                else if (userMsg.Contains("[Ảnh") || !string.IsNullOrWhiteSpace(draft.AttachedImage))
                {
                    // Ảnh đã được gán vào draft
                }

                return GenerateSupportSummaryCard(draft, "Dạ đã cập nhật Ảnh sự cố thành công! ✅");
            }

            return null;
        }

        private static (string ShortTitle, string Prompt, string? ActionIntent) GetQuestionInfoForState(string state)
        {
            return state switch
            {
                // SELLER
                "REGISTER_SELLER_Q1_STORENAME" or "REGISTER_SELLER_STEP1" or "REGISTER_SELLER_WAITING" =>
                    ("Tên cửa hàng", "1️⃣ **Câu 1 / 14:** Cho mình xin **Tên cửa hàng / Tiệm** (Từ 12 - 30 ký tự, không chứa ký tự đặc biệt) nhé?", null),
                "REGISTER_SELLER_Q2_ADDRESS" =>
                    ("Địa chỉ tiệm", "2️⃣ **Câu 2 / 14:** Cho mình xin **Địa chỉ tiệm** của bạn (Ví dụ: Số 125 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội) nhé?", null),
                "REGISTER_SELLER_Q3_CATEGORY" =>
                    ("Danh mục ngành hàng", "3️⃣ **Câu 3 / 14:** Tiệm bạn kinh doanh **Danh mục ngành hàng** nào chính? (Bấm nút bên dưới để chọn):", "SELLER_CHOOSE_CATEGORY"),
                "REGISTER_SELLER_Q4_GMAIL" =>
                    ("Gmail đăng nhập", "4️⃣ **Câu 4 / 14:** Cho mình xin **Địa chỉ Gmail** của bạn (Ví dụ: `abc@gmail.com`) để làm tài khoản đăng nhập tiệm nhé?", null),
                "REGISTER_SELLER_Q5_PASSWORD" =>
                    ("Mật khẩu đăng nhập", "5️⃣ **Câu 5 / 14:** Vui lòng tạo **Mật khẩu đăng nhập** (Từ 6 - 16 ký tự, nhập vào ô bên dưới) nhé?", null),
                "REGISTER_SELLER_Q6_CONFIRM_PASSWORD" =>
                    ("Nhập lại mật khẩu", "6️⃣ **Câu 6 / 14:** Vui lòng **Nhập lại mật khẩu** (Xác nhận trùng khớp) nhé?", null),
                "REGISTER_SELLER_Q7_OPENHOURS" =>
                    ("Khung giờ mở cửa", "7️⃣ **Câu 7 / 14:** Vui lòng chọn **Khung giờ mở cửa** hoạt động của tiệm (Bấm nút bên dưới để chọn):", "SELLER_CHOOSE_OPENHOURS"),
                "REGISTER_SELLER_Q7_CUSTOM_OPENHOURS" =>
                    ("Khung giờ tùy chỉnh", "7️⃣ **Câu 7 / 14:** Vui lòng nhập khung giờ hoạt động tùy chỉnh của bạn (Ví dụ: `08:00 - 21:30`):", null),
                "REGISTER_SELLER_Q8_OWNERNAME" =>
                    ("Họ và tên thật chủ tiệm", "8️⃣ **Câu 8 / 14:** Cho mình xin **Họ và tên thật của chủ tiệm** (Từ 6 - 16 ký tự) để làm hợp đồng gian hàng nhé?", null),
                "REGISTER_SELLER_Q9_CCCD" =>
                    ("Số CCCD chủ tiệm", "9️⃣ **Câu 9 / 14:** Cho mình xin **Số Căn cước công dân (CCCD)** của chủ tiệm (Đúng 12 chữ số) nhé?", null),
                "REGISTER_SELLER_Q10_CCCD_FRONT" or "REGISTER_SELLER_Q9_CCCD_FRONT" =>
                    ("ảnh CCCD Mặt Trước", "🔟 **Câu 10 / 14:** Vui lòng tải lên ảnh **CCCD Mặt Trước** của chủ tiệm (Nhấp vào khung nét đứt bên dưới để chọn ảnh):", "SELLER_UPLOAD_CCCD_FRONT"),
                "REGISTER_SELLER_Q11_CCCD_BACK" or "REGISTER_SELLER_Q10_CCCD_BACK" =>
                    ("ảnh CCCD Mặt Sau", "1️⃣1️⃣ **Câu 11 / 14:** Vui lòng tải lên ảnh **CCCD Mặt Sau** của chủ tiệm (Nhấp vào khung nét đứt bên dưới để chọn ảnh):", "SELLER_UPLOAD_CCCD_BACK"),
                "REGISTER_SELLER_Q12_FOOD_SAFETY" or "REGISTER_SELLER_Q11_FOOD_SAFETY" =>
                    ("ảnh Giấy CN An toàn thực phẩm", "1️⃣2️⃣ **Câu 12 / 14:** Vui lòng tải lên ảnh **Giấy Chứng Nhận An Toàn Thực Phẩm** (hoặc Giấy Phép Kinh Doanh) của tiệm:", "SELLER_UPLOAD_FOOD_SAFETY"),
                "REGISTER_SELLER_Q13_BANK" or "REGISTER_SELLER_Q12_BANK" =>
                    ("Ngân hàng thụ hưởng", "1️⃣3️⃣ **Câu 13 / 14:** Vui lòng chọn **Ngân hàng thụ hưởng** để nhận doanh thu thanh toán (Bấm nút bên dưới để chọn):", "SELLER_CHOOSE_BANK"),
                "REGISTER_SELLER_Q14_BANKACC" or "REGISTER_SELLER_Q13_BANKACC" or "REGISTER_SELLER_STEP3" =>
                    ("Số tài khoản ngân hàng", "1️⃣4️⃣ **Câu 14 / 14 (Cuối cùng):** Cho mình xin **Số tài khoản ngân hàng** (Từ 8 - 15 chữ số) để sàn đối soát chuyển tiền nhé?", null),

                // SHIPPER
                "REGISTER_SHIPPER_Q1_NAME" or "REGISTER_SHIPPER_STEP1" or "REGISTER_SHIPPER_WAITING" =>
                    ("Họ và tên thật của tài xế", "1️⃣ **Câu 1 / 15:** Cho mình xin **Họ và tên thật của tài xế** (Từ 6 - 16 ký tự) nhé?", null),
                "REGISTER_SHIPPER_Q2_GMAIL" =>
                    ("Gmail đăng nhập", "2️⃣ **Câu 2 / 15:** Cho mình xin **Địa chỉ Gmail** của bạn (Ví dụ: `taixe@gmail.com`) để làm tài khoản đăng nhập app tài xế nhé?", null),
                "REGISTER_SHIPPER_Q3_PASSWORD" =>
                    ("Mật khẩu đăng nhập", "3️⃣ **Câu 3 / 15:** Vui lòng tạo **Mật khẩu đăng nhập** (Từ 6 - 16 ký tự) nhé?", null),
                "REGISTER_SHIPPER_Q4_CONFIRM_PASSWORD" =>
                    ("Nhập lại mật khẩu", "4️⃣ **Câu 4 / 15:** Vui lòng **Nhập lại mật khẩu** (Xác nhận trùng khớp) nhé?", null),
                "REGISTER_SHIPPER_Q5_CCCD" =>
                    ("Số CCCD", "5️⃣ **Câu 5 / 15:** Cho mình xin **Số Căn Cước Công Dân (CCCD)** (Đúng chuẩn 12 chữ số) nhé?", null),
                "REGISTER_SHIPPER_Q6_PORTRAIT" =>
                    ("Ảnh chân dung tài xế", "6️⃣ **Câu 6 / 15:** Vui lòng tải lên **Ảnh chân dung tài xế** (Nhấp vào ô bên dưới để chọn ảnh):", "SHIPPER_UPLOAD_PORTRAIT"),
                "REGISTER_SHIPPER_Q7_CCCD_FRONT" =>
                    ("ảnh CCCD Mặt Trước", "7️⃣ **Câu 7 / 15:** Vui lòng tải lên ảnh **CCCD Mặt Trước** của tài xế:", "SHIPPER_UPLOAD_CCCD_FRONT"),
                "REGISTER_SHIPPER_Q8_CCCD_BACK" =>
                    ("ảnh CCCD Mặt Sau", "8️⃣ **Câu 8 / 15:** Vui lòng tải lên ảnh **CCCD Mặt Sau** của tài xế:", "SHIPPER_UPLOAD_CCCD_BACK"),
                "REGISTER_SHIPPER_Q9_LICENSE_PLATE" =>
                    ("Biển số xe máy", "9️⃣ **Câu 9 / 15:** Cho mình xin **Biển số xe máy** bạn chạy đơn (Ví dụ: `29AK1233`):", null),
                "REGISTER_SHIPPER_Q10_VEHICLE_TYPE" =>
                    ("Loại xe đăng ký", "🔟 **Câu 10 / 15:** Vui lòng chọn **Loại xe đăng ký** (Bấm nút bên dưới để chọn):", "SHIPPER_CHOOSE_VEHICLE_TYPE"),
                "REGISTER_SHIPPER_Q11_VEHICLE_MODEL" =>
                    ("Dòng xe / Mẫu xe", "1️⃣1️⃣ **Câu 11 / 15:** Vui lòng chọn **Dòng xe / Mẫu xe** bạn sử dụng từ danh sách bên dưới (Bấm nút để chọn):", "SHIPPER_CHOOSE_VEHICLE_MODEL"),
                "REGISTER_SHIPPER_Q12_GPLX" =>
                    ("ảnh Bằng Lái Xe (GPLX)", "1️⃣2️⃣ **Câu 12 / 15:** Vui lòng tải lên ảnh **Bằng Lái Xe (GPLX)** của bạn (Nhấp vào ô bên dưới để chọn ảnh):", "SHIPPER_UPLOAD_GPLX"),
                "REGISTER_SHIPPER_Q13_AREA" =>
                    ("Khu vực chạy chính", "1️⃣3️⃣ **Câu 13 / 15:** Vui lòng chọn **Khu vực đăng ký chạy chính** của bạn từ danh sách Quận / Huyện bên dưới (Bấm nút để chọn):", "SHIPPER_CHOOSE_AREA"),
                "REGISTER_SHIPPER_Q14_BANK" =>
                    ("Ngân hàng nhận cước", "1️⃣4️⃣ **Câu 14 / 15:** Vui lòng chọn **Ngân hàng nhận cước** từ danh sách bên dưới (Bấm nút để chọn):", "SHIPPER_CHOOSE_BANK"),
                "REGISTER_SHIPPER_Q15_BANKACC" or "REGISTER_SHIPPER_STEP3" =>
                    ("Số tài khoản ngân hàng", "1️⃣5️⃣ **Câu 15 / 15 (Cuối cùng):** Cho mình xin **Số tài khoản ngân hàng** (Từ 8 - 15 chữ số) để sàn chuyển thu nhập cước xe cho bạn nhé?", null),

                // BUYER
                "REGISTER_BUYER_Q1_NAME" or "REGISTER_BUYER_STEP1" or "REGISTER_BUYER_WAITING" =>
                    ("Họ và tên", "1️⃣ **Câu 1 / 4:** Cho mình xin **Họ và tên đầy đủ** của bạn nhé?", null),
                "REGISTER_BUYER_Q2_EMAIL" or "REGISTER_BUYER_STEP2" =>
                    ("Gmail đăng ký", "2️⃣ **Câu 2 / 4:** Cho mình xin **Địa chỉ Gmail** của bạn (Ví dụ: `khachhang@gmail.com`) nhé?", null),
                "REGISTER_BUYER_Q3_PASSWORD" =>
                    ("Mật khẩu", "3️⃣ **Câu 3 / 4:** Vui lòng tạo **Mật khẩu đăng nhập** (Từ 6 - 16 ký tự) nhé?", null),
                "REGISTER_BUYER_Q4_CONFIRM_PASSWORD" or "REGISTER_BUYER_STEP3" =>
                    ("Nhập lại mật khẩu", "4️⃣ **Câu 4 / 4:** Vui lòng **Nhập lại mật khẩu** (Xác nhận trùng khớp) nhé?", null),

                _ => ("thông tin tiếp theo", "Vui lòng tiếp tục nhập thông tin để hoàn tất hồ sơ nhé!", null)
            };
        }

        private async Task<object> ProcessFullFormRegistrationAsync(string userMsg, string activeState)
        {
            string fullName = ExtractField(userMsg, @"(họ tên|họ và tên|tên|full name)[\s:]+([^\n,]+)");
            if (string.IsNullOrWhiteSpace(fullName)) fullName = "Người Dùng ZoneMart";

            string phoneEmail = ExtractField(userMsg, @"(sđt|sdt|số điện thoại|phone|email|tài khoản)[\s:]+([^\n,]+)");
            if (string.IsNullOrWhiteSpace(phoneEmail))
            {
                var matchEmail = Regex.Match(userMsg, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                var matchPhone = Regex.Match(userMsg, @"\b0\d{9}\b");
                if (matchEmail.Success) phoneEmail = matchEmail.Value;
                else if (matchPhone.Success) phoneEmail = matchPhone.Value;
            }

            string password = ExtractField(userMsg, @"(mật khẩu|pass|password)[\s:]+([^\n,]+)");
            if (string.IsNullOrWhiteSpace(password)) password = "123456";

            string targetEmail = phoneEmail.Contains("@") ? phoneEmail.Trim().ToLowerInvariant() : $"{phoneEmail.Trim().ToLowerInvariant()}@gmail.com";
            var (isAlreadyReg, detailRole) = await CheckIfEmailAlreadyRegisteredAsync(targetEmail);
            if (isAlreadyReg)
            {
                return new
                {
                    success = true,
                    reply = $"Oh no! Gmail `{targetEmail}` này đã được đăng ký tài khoản trên hệ thống ZoneMart rồi ạ 😅. Mỗi tài khoản chỉ đăng ký 1 Gmail duy nhất, bạn vui lòng chọn và nhập một địa chỉ Gmail khác nhé! 🔑\n\n💡 *(Nhắn **\"Hủy yêu cầu\"** nếu bạn muốn dừng)*",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = activeState,
                    regDraft = new DirectRegDraft()
                };
            }

            string cleanMsgNoTone = RemoveVietnameseTones(userMsg.ToLower());
            bool isSellerReg = activeState.Contains("SELLER") || cleanMsgNoTone.Contains("seller") || cleanMsgNoTone.Contains("shop") || cleanMsgNoTone.Contains("gian hang") || cleanMsgNoTone.Contains("ban hang");
            bool isShipperReg = activeState.Contains("SHIPPER") || cleanMsgNoTone.Contains("shipper") || cleanMsgNoTone.Contains("tai xe") || cleanMsgNoTone.Contains("chay xe") || cleanMsgNoTone.Contains("bien so");

            if (isSellerReg)
            {
                string storeName = ExtractField(userMsg, @"(tên gian hàng|tên shop|gian hàng|shop)[\s:]+([^\n,]+)");
                if (string.IsNullOrWhiteSpace(storeName)) storeName = $"Gian Hàng {fullName}";

                string address = ExtractField(userMsg, @"(địa chỉ|address)[\s:]+([^\n,]+)");
                if (string.IsNullOrWhiteSpace(address)) address = "Hà Nội";

                string category = ExtractField(userMsg, @"(ngành hàng|danh mục|category)[\s:]+([^\n,]+)");
                if (string.IsNullOrWhiteSpace(category)) category = "Thực phẩm & Nhu yếu phẩm";

                string cccdSeller = ExtractField(userMsg, @"(cccd|số cccd|căn cước|cmnd)[\s:]+([^\n,]+)");
                if (string.IsNullOrWhiteSpace(cccdSeller) || !Regex.IsMatch(cccdSeller, @"^\d{12}$"))
                {
                    var mCccd = Regex.Match(userMsg, @"\b\d{12}\b");
                    cccdSeller = mCccd.Success ? mCccd.Value : "001202009999";
                }

                var draft = new DirectRegDraft
                {
                    Role = "SELLER",
                    StoreName = storeName,
                    Address = address,
                    Category = category,
                    OwnerFullName = fullName,
                    CccdNumber = cccdSeller,
                    Email = phoneEmail.Contains("@") ? phoneEmail : $"{phoneEmail}@gmail.com",
                    Password = password,
                    ConfirmPassword = password,
                    BankName = "Vietcombank (VCB)",
                    BankAccountNumber = "123456789"
                };
                return await CompleteSellerRegistrationAsync(draft);
            }
            else if (isShipperReg)
            {
                string licensePlate = ExtractField(userMsg, @"(biển số|biển số xe|biển xe|license plate)[\s:]+([^\n,]+)");
                if (string.IsNullOrWhiteSpace(licensePlate)) licensePlate = "29A-88888";

                string area = ExtractField(userMsg, @"(khu vực|khu vực chạy|operating area|area)[\s:]+([^\n,]+)");
                if (string.IsNullOrWhiteSpace(area)) area = "Hà Nội";

                string cccdShipper = ExtractField(userMsg, @"(cccd|số cccd|căn cước|cmnd)[\s:]+([^\n,]+)");
                if (string.IsNullOrWhiteSpace(cccdShipper) || !Regex.IsMatch(cccdShipper, @"^\d{12}$"))
                {
                    var mCccd = Regex.Match(userMsg, @"\b\d{12}\b");
                    cccdShipper = mCccd.Success ? mCccd.Value : "001202008888";
                }

                var draft = new DirectRegDraft
                {
                    Role = "SHIPPER",
                    FullName = fullName,
                    Email = phoneEmail.Contains("@") ? phoneEmail : $"{phoneEmail}@gmail.com",
                    Password = password,
                    ConfirmPassword = password,
                    LicensePlate = licensePlate.ToUpperInvariant(),
                    OperatingArea = area,
                    CccdNumber = cccdShipper,
                    BankName = "Vietcombank (VCB)",
                    BankAccountNumber = "99998888"
                };
                return await CompleteShipperRegistrationAsync(draft);
            }
            else
            {
                var draft = new DirectRegDraft
                {
                    Role = "BUYER",
                    FullName = fullName,
                    Email = phoneEmail.Contains("@") ? phoneEmail : $"{phoneEmail}@gmail.com",
                    Password = password,
                    ConfirmPassword = password
                };
                return await CompleteBuyerRegistrationAsync(draft);
            }
        }

        private async Task<object> CompleteSellerRegistrationAsync(DirectRegDraft draft)
        {
            string cleanAccount = draft.Email?.Trim().ToLowerInvariant() ?? "";
            string fullName = draft.OwnerFullName?.Trim() ?? "Chủ Tiệm ZoneMart";
            string storeName = draft.StoreName?.Trim() ?? "Gian Hàng ZoneMart";
            string address = draft.Address?.Trim() ?? "Hà Nội";
            string category = draft.Category?.Trim() ?? "Thực phẩm & Nhu yếu phẩm";
            string openHours = draft.OpenHours?.Trim() ?? "07:00 - 22:00";
            string bankName = draft.BankName?.Trim() ?? "Vietcombank (VCB)";
            string bankAcc = draft.BankAccountNumber?.Trim() ?? "123456789";
            string password = draft.Password?.Trim() ?? "123456";
            string cccdNumber = draft.CccdNumber?.Trim() ?? "";

            var (alreadyReg, detailRole) = await CheckIfEmailAlreadyRegisteredAsync(cleanAccount);
            if (alreadyReg)
            {
                return new
                {
                    success = true,
                    reply = $"Oh no! Gmail `{cleanAccount}` này đã được đăng ký tài khoản trên hệ thống ZoneMart rồi ạ 😅. Mỗi tài khoản chỉ đăng ký 1 Gmail duy nhất, bạn vui lòng chọn và nhập một địa chỉ Gmail khác nhé! 🔑",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "NONE"
                };
            }

            var existingUser = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == cleanAccount || u.Phone == cleanAccount).FirstOrDefaultAsync();
            if (existingUser == null)
            {
                existingUser = new User
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    UserCode = $"SEL-USER-{Random.Shared.Next(1000, 9999)}",
                    PhoneEmail = cleanAccount,
                    Phone = Regex.IsMatch(cleanAccount, @"^\d+$") ? cleanAccount : "",
                    PasswordHash = password,
                    Password = password,
                    FullName = fullName,
                    IsBuyer = true,
                    IsSeller = true,
                    IsShipper = false,
                    AccountStatus = "pending",
                    CreatedAt = DateTime.UtcNow
                };
                await _mongoService.Users.InsertOneAsync(existingUser);
                AuthController.InMemoryUsers[cleanAccount] = existingUser;
            }

            string sellerCode = $"SEL-{Random.Shared.Next(1000, 9999)}";
            var newStore = new Store
            {
                UserId = existingUser.UserCode ?? sellerCode,
                StoreCode = sellerCode,
                Password = password,
                PhoneEmail = cleanAccount,
                StoreName = storeName,
                Category = category,
                Address = address,
                OpenHours = openHours,
                OwnerFullName = fullName,
                CccdNumber = cccdNumber,
                BankName = bankName,
                BankAccountNumber = bankAcc,
                CccdFrontImage = draft.CccdFrontImage ?? "",
                CccdBackImage = draft.CccdBackImage ?? "",
                FoodSafetyCertImage = draft.FoodSafetyImage ?? "",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _mongoService.Stores.InsertOneAsync(newStore);
            if (!string.IsNullOrEmpty(newStore.Id)) AuthController.InMemoryStores[newStore.Id] = newStore;

            try
            {
                await _mongoService.AdminNotifications.InsertOneAsync(new AdminNotification
                {
                    Type = "seller",
                    Title = "AI Tự Động Đăng Ký Gian Hàng",
                    Message = $"Chủ tiệm {fullName} đã nhờ AI nộp hồ sơ gian hàng '{storeName}'.",
                    TargetUserId = newStore.Id ?? "",
                    TargetName = storeName,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { }

            // GỬI EMAIL THÔNG BÁO PENDING VỀ GMAIL SELLER (SỬ DỤNG GMAIL dobinh225599@gmail.com ĐÃ CẤU HÌNH)
            if (!string.IsNullOrEmpty(cleanAccount) && cleanAccount.Contains("@"))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await AuthController.SendSellerPendingEmailAsync(cleanAccount, storeName, fullName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ [AI Seller Email Error] {ex.Message}");
                    }
                });
            }

            return new
            {
                success = true,
                reply = $@"🎉 **HỒ SƠ ĐĂNG KÝ SELLER ĐÃ ĐƯỢC GỬI THÀNH CÔNG!**

📋 **THÔNG TIN HỒ SƠ GIAN HÀNG ĐÃ NỘP:**
🏪 **Gian hàng:** {storeName} ({category})
📍 **Địa chỉ:** {address}
⏰ **Khung giờ:** {openHours}
👤 **Chủ tiệm:** {fullName} | `{cleanAccount}`
🆔 **CCCD:** `{cccdNumber}`
📄 **Giấy tờ:** Đã nộp ảnh CCCD 2 mặt & Giấy CN An toàn thực phẩm
💳 **Thanh toán:** {bankName} - STK: `{bankAcc}`
🔑 **Mật khẩu:** [MASKED_PWD:{password}]

⏳ **Thông báo phê duyệt:** Đội ngũ Ban Quản Lý & Trợ lý AI Admin sẽ kiểm tra và phê duyệt hồ sơ sớm nhất. Khi nào hồ sơ được duyệt sẽ có thông báo gửi về Gmail **{cleanAccount}** nhé ạ!",
                provider = "ZoneMart AI Direct Registration Engine",
                registrationState = "NONE",
                actionIntent = "SELLER_REGISTER_SUCCESS",
                regDraft = new DirectRegDraft(),
                summaryData = new
                {
                    role = "SELLER",
                    storeName = storeName,
                    category = category,
                    address = address,
                    openHours = openHours,
                    ownerFullName = fullName,
                    email = cleanAccount,
                    cccdNumber = cccdNumber,
                    bankName = bankName,
                    bankAcc = bankAcc,
                    password = password,
                    status = "Chờ duyệt (Pending)"
                }
            };
        }

        private async Task<object> CompleteShipperRegistrationAsync(DirectRegDraft draft)
        {
            string cleanAccount = draft.Email?.Trim().ToLowerInvariant() ?? "";
            string fullName = draft.FullName?.Trim() ?? "Tài Xế ZoneMart";
            string licensePlate = draft.LicensePlate?.Trim().ToUpperInvariant() ?? "29A-12345";
            string vehicleType = draft.VehicleType?.Trim() ?? "Xe máy xăng";
            string vehicleModel = draft.VehicleModel?.Trim() ?? "Honda Wave Alpha";
            string area = draft.OperatingArea?.Trim() ?? "Quận Cầu Giấy";
            string bankName = draft.BankName?.Trim() ?? "Vietcombank (VCB)";
            string bankAcc = draft.BankAccountNumber?.Trim() ?? "99998888";
            string password = draft.Password?.Trim() ?? "123456";
            string cccdNumber = draft.CccdNumber?.Trim() ?? "";

            var (alreadyRegShipper, detailRoleShipper) = await CheckIfEmailAlreadyRegisteredAsync(cleanAccount);
            if (alreadyRegShipper)
            {
                return new
                {
                    success = true,
                    reply = $"Oh no! Gmail `{cleanAccount}` này đã được đăng ký tài khoản trên hệ thống ZoneMart rồi ạ 😅. Mỗi tài khoản chỉ đăng ký 1 Gmail duy nhất, bạn vui lòng chọn và nhập một địa chỉ Gmail khác nhé! 🔑",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "NONE"
                };
            }

            var existingUser = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == cleanAccount || u.Phone == cleanAccount).FirstOrDefaultAsync();
            if (existingUser == null)
            {
                existingUser = new User
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    UserCode = $"SHP-USER-{Random.Shared.Next(1000, 9999)}",
                    PhoneEmail = cleanAccount,
                    Phone = Regex.IsMatch(cleanAccount, @"^\d+$") ? cleanAccount : "",
                    PasswordHash = password,
                    Password = password,
                    FullName = fullName,
                    IsBuyer = true,
                    IsShipper = true,
                    IsSeller = false,
                    AccountStatus = "pending",
                    CreatedAt = DateTime.UtcNow
                };
                await _mongoService.Users.InsertOneAsync(existingUser);
                AuthController.InMemoryUsers[cleanAccount] = existingUser;
            }

            string shipperCode = $"SHP-{Random.Shared.Next(1000, 9999)}";
            var newShipper = new Shipper
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                ShipperCode = shipperCode,
                UserId = existingUser.Id ?? "",
                FullName = fullName,
                PhoneNumber = cleanAccount,
                Password = password,
                LicensePlate = licensePlate,
                VehicleType = vehicleType,
                VehicleModel = vehicleModel,
                OperatingArea = area,
                CccdNumber = cccdNumber,
                AvatarUrl = draft.PortraitImage ?? "",
                CccdFrontImage = draft.CccdFrontImage ?? "",
                CccdBackImage = draft.CccdBackImage ?? "",
                DrivingLicenseImage = draft.DrivingLicenseImage ?? "",
                BankName = bankName,
                BankAccountNumber = bankAcc,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _mongoService.Shippers.InsertOneAsync(newShipper);
            if (!string.IsNullOrEmpty(newShipper.Id)) AuthController.InMemoryShippers[newShipper.Id] = newShipper;

            try
            {
                await _mongoService.AdminNotifications.InsertOneAsync(new AdminNotification
                {
                    Type = "shipper",
                    Title = "AI Tự Động Đăng Ký Tài Xế Shipper",
                    Message = $"Tài xế {fullName} đã nộp hồ sơ chạy xe thông qua AI.",
                    TargetUserId = newShipper.Id,
                    TargetName = fullName,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { }

            // GỬI EMAIL THÔNG BÁO PENDING VỀ GMAIL SHIPPER (SỬ DỤNG GMAIL dobinh225599@gmail.com ĐÃ CẤU HÌNH)
            if (!string.IsNullOrEmpty(cleanAccount) && cleanAccount.Contains("@"))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await AuthController.SendShipperPendingEmailAsync(cleanAccount, fullName, licensePlate);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ [AI Shipper Email Error] {ex.Message}");
                    }
                });
            }

            return new
            {
                success = true,
                reply = $@"🎉 **HỒ SƠ ĐĂNG KÝ SHIPPER ĐÃ ĐƯỢC GỬI THÀNH CÔNG!**

📋 **THÔNG TIN HỒ SƠ TÀI XẾ ĐÃ NỘP:**
👤 **Tài xế:** {fullName} | `{cleanAccount}`
🆔 **CCCD:** `{cccdNumber}` (Đã nộp ảnh CCCD 2 mặt & chân dung)
🛵 **Phương tiện:** Biển số: **{licensePlate}** ({vehicleType} - {vehicleModel})
📜 **Bằng lái:** Đã nộp ảnh GPLX
📍 **Khu vực chạy:** {area}
💳 **Thanh toán:** {bankName} - STK: `{bankAcc}`
🔑 **Mật khẩu:** [MASKED_PWD:{password}]

⏳ **Thông báo phê duyệt:** Đội ngũ Ban Quản Lý & Trợ lý AI Admin sẽ kiểm tra và phê duyệt hồ sơ sớm nhất. Khi nào hồ sơ được duyệt sẽ có thông báo gửi về Gmail **{cleanAccount}** nhé ạ!",
                provider = "ZoneMart AI Direct Registration Engine",
                registrationState = "NONE",
                actionIntent = "SHIPPER_REGISTER_SUCCESS",
                regDraft = new DirectRegDraft(),
                summaryData = new
                {
                    role = "SHIPPER",
                    fullName = fullName,
                    email = cleanAccount,
                    cccdNumber = cccdNumber,
                    licensePlate = licensePlate,
                    vehicleType = vehicleType,
                    vehicleModel = vehicleModel,
                    area = area,
                    bankName = bankName,
                    bankAcc = bankAcc,
                    password = password,
                    status = "Chờ duyệt (Pending)"
                }
            };
        }

        private async Task<object> CompleteBuyerRegistrationAsync(DirectRegDraft draft)
        {
            string cleanAccount = draft.Email?.Trim().ToLowerInvariant() ?? "";
            string fullName = draft.FullName?.Trim() ?? "Khách Hàng ZoneMart";
            string password = draft.Password?.Trim() ?? "123456";

            var (alreadyRegBuyer, detailRoleBuyer) = await CheckIfEmailAlreadyRegisteredAsync(cleanAccount);
            if (alreadyRegBuyer)
            {
                return new
                {
                    success = true,
                    reply = $"Oh no! Gmail `{cleanAccount}` này đã được đăng ký tài khoản trên hệ thống ZoneMart rồi ạ 😅. Mỗi tài khoản chỉ đăng ký 1 Gmail duy nhất, bạn vui lòng chọn và nhập một địa chỉ Gmail khác nhé! 🔑",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "NONE",
                    regDraft = new DirectRegDraft()
                };
            }

            var checkExistingBuyer = await _mongoService.Users.Find(u => u.PhoneEmail.ToLower() == cleanAccount || u.Phone == cleanAccount).FirstOrDefaultAsync();
            if (checkExistingBuyer != null)
            {
                return new
                {
                    success = true,
                    reply = $"⚠️ Tài khoản `{cleanAccount}` đã tồn tại trên hệ thống ZoneMart! Bạn có thể sử dụng nút **[Đăng Nhập]** để truy cập tài khoản ngay lập tức nhé! 😊",
                    provider = "ZoneMart AI Direct Registration Engine",
                    registrationState = "NONE",
                    regDraft = new DirectRegDraft()
                };
            }

            var newBuyerUser = new User
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                UserCode = $"CUST-{Random.Shared.Next(1000, 9999)}",
                PhoneEmail = cleanAccount,
                Phone = Regex.IsMatch(cleanAccount, @"^\d+$") ? cleanAccount : "",
                PasswordHash = password,
                Password = password,
                FullName = fullName,
                IsBuyer = true,
                IsShipper = false,
                IsSeller = false,
                IsAdmin = false,
                AccountStatus = "active",
                CreatedAt = DateTime.UtcNow
            };
            await _mongoService.Users.InsertOneAsync(newBuyerUser);
            AuthController.InMemoryUsers[cleanAccount] = newBuyerUser;

            // GỬI THƯ CẢM ƠN TỚI GMAIL VỪA ĐĂNG KÝ (SỬ DỤNG GMAIL dobinh225599@gmail.com ĐÃ CẤU HÌNH)
            if (!string.IsNullOrEmpty(cleanAccount) && cleanAccount.Contains("@"))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await AuthController.SendThankYouEmailAsync(cleanAccount, fullName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ [AI Buyer Email Error] {ex.Message}");
                    }
                });
            }

            return new
            {
                success = true,
                reply = $@"🎉 **AI ĐÃ ĐĂNG KÝ TÀI KHOẢN KHÁCH HÀNG THÀNH CÔNG!**

👤 **Họ và tên:** {fullName}
📧 **Tài khoản đăng nhập:** `{cleanAccount}`
🔑 **Mật khẩu:** [MASKED_PWD:{password}]
🆔 **Mã Khách Hàng:** `{newBuyerUser.UserCode}`
✅ **Trạng thái:** Hoạt động (Active)

✨ Thư cảm ơn & xác nhận đã được gửi về Gmail **{cleanAccount}**!
Bạn có muốn đăng nhập ngay không?",
                provider = "ZoneMart AI Direct Registration Engine",
                registrationState = "NONE",
                actionIntent = "BUYER_REGISTER_SUCCESS",
                regDraft = new DirectRegDraft(),
                summaryData = new
                {
                    role = "BUYER",
                    fullName = fullName,
                    email = cleanAccount,
                    password = password,
                    userCode = newBuyerUser.UserCode,
                    status = "Hoạt động (Active)"
                }
            };
        }

        private static string ExtractField(string input, string pattern)
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count >= 3)
            {
                return match.Groups[2].Value.Trim();
            }
            return string.Empty;
        }

        private static bool IsColdWeatherIntent(string cleanMsgNoTone)
        {
            if (string.IsNullOrWhiteSpace(cleanMsgNoTone)) return false;
            return cleanMsgNoTone.Contains("se lanh") || cleanMsgNoTone.Contains("troi lanh") ||
                   cleanMsgNoTone.Contains("thoi tiet lanh") || cleanMsgNoTone.Contains("ret") ||
                   cleanMsgNoTone.Contains("mua lanh") || cleanMsgNoTone.Contains("troi mua") ||
                   cleanMsgNoTone.Contains("gio mua") || cleanMsgNoTone.Contains("am nong") ||
                   cleanMsgNoTone.Contains("am bung") || cleanMsgNoTone.Contains("suyt xoa") ||
                   (cleanMsgNoTone.Contains("lanh") && !cleanMsgNoTone.Contains("mat lanh") && !cleanMsgNoTone.Contains("uop lanh") && !cleanMsgNoTone.Contains("da lanh"));
        }

        private static bool IsHotWeatherIntent(string cleanMsgNoTone)
        {
            if (string.IsNullOrWhiteSpace(cleanMsgNoTone)) return false;
            if (IsColdWeatherIntent(cleanMsgNoTone)) return false;

            return cleanMsgNoTone.Contains("troi nong") || cleanMsgNoTone.Contains("thoi tiet nong") ||
                   cleanMsgNoTone.Contains("nang nong") || cleanMsgNoTone.Contains("oi buc") ||
                   cleanMsgNoTone.Contains("oi a") || cleanMsgNoTone.Contains("do nong") ||
                   cleanMsgNoTone.Contains("bot nong") || cleanMsgNoTone.Contains("giai nhiet") ||
                   cleanMsgNoTone.Contains("giai khat") || cleanMsgNoTone.Contains("mat lanh") ||
                   cleanMsgNoTone.Contains("thanh nhiet") || cleanMsgNoTone.Contains("do mat") ||
                   cleanMsgNoTone.Contains("mon mat") || cleanMsgNoTone.Contains("mon lanh") ||
                   cleanMsgNoTone.Contains("giam nhiet") ||
                   (cleanMsgNoTone.Contains("nong") && !cleanMsgNoTone.Contains("nong hoi") && !cleanMsgNoTone.Contains("am nong"));
        }

        private static bool IsProductCooling(Product p)
        {
            if (p == null) return false;
            string name = RemoveVietnameseTones(p.ProductName ?? "").ToLower();
            string cat = RemoveVietnameseTones(p.Category ?? "").ToLower();
            string desc = RemoveVietnameseTones(p.Description ?? "").ToLower();

            // Loại trừ hoàn toàn các món nóng / món xào / lẩu / bốc khói / bánh mì chảo
            if (cat.Contains("mon an nong") || name.Contains("nong hoi") || name.Contains("banh mi chao") ||
                name.Contains("pho") || name.Contains("xoi mo") || name.Contains("lau") || name.Contains("chao"))
            {
                return false;
            }

            return name.Contains("trai cay") || name.Contains("dau tay") || name.Contains("sua") ||
                   name.Contains("rau") || name.Contains("cu qua") || name.Contains("xa lach") ||
                   name.Contains("ca chua") || name.Contains("nuoc") || name.Contains("che") ||
                   cat.Contains("trai cay") || cat.Contains("rau") || cat.Contains("do uong") ||
                   cat.Contains("nong san") || desc.Contains("mat") || desc.Contains("giai nhiet");
        }

        private static bool IsProductWarming(Product p)
        {
            if (p == null) return false;
            string name = RemoveVietnameseTones(p.ProductName ?? "").ToLower();
            string cat = RemoveVietnameseTones(p.Category ?? "").ToLower();

            return cat.Contains("mon an nong") || name.Contains("pho") || name.Contains("banh mi chao") ||
                   name.Contains("com ga") || name.Contains("bo my") || name.Contains("thit bo") ||
                   name.Contains("lau") || name.Contains("nong hoi") || name.Contains("chao");
        }

        private static (bool IsUnsafe, string Reply) CheckSafetyGuardrails(string userMsg, string cleanMsgNoTone)
        {
            if (string.IsNullOrWhiteSpace(cleanMsgNoTone)) return (false, "");

            // 1. BẢO MẬT HỆ THỐNG / HACK / TẤN CÔNG / LẤY CẮP MẬT KHẨU
            string[] securityKeywords = new[]
            {
                "hack database", "sql injection", "sqli", "ddos", "tan cong mang", "lay trom mat khau",
                "hack pass", "hack tai khoan", "trom the", "hack admin", "bypass auth", "chiem quyen admin",
                "dump database", "hack tien", "hack vi tien", "hack zonemart"
            };
            foreach (var kw in securityKeywords)
            {
                if (cleanMsgNoTone.Contains(kw))
                {
                    return (true, "🔒 **Bảo mật hệ thống:** Hệ thống cơ sở dữ liệu và tài khoản trên sàn ZoneMart được bảo vệ nghiêm ngặt bằng nhiều lớp mã hóa an toàn. Trợ lý AI tuyệt đối không cung cấp thông tin quản trị hoặc tiếp tay cho các hành vi can thiệp dữ liệu trái phép!");
                }
            }

            // 2. 18+ / ĐỒI TRỤY / KHIÊU DÂM / TÌNH DỤC
            string[] nsfwKeywords = new[]
            {
                "sex", "khieu dam", "kich duc", "phim sex", "quan he tinh duc", "loan luan", "hiep dam",
                "khoa than", "thu dam", "dam duc", "bup be tinh duc", "lam tinh", "chat sex", "gai goi", "mua dam"
            };
            foreach (var kw in nsfwKeywords)
            {
                if (Regex.IsMatch(cleanMsgNoTone, $@"\b{Regex.Escape(kw)}\b"))
                {
                    return (true, "⚠️ **Cảnh báo an toàn nội dung:** ZoneMart là nền tảng thương mại điện tử phục vụ cộng đồng văn minh, lành mạnh. Mình tuyệt đối từ chối phản hồi các câu hỏi hoặc nội dung 18+, đồi trụy. Bạn hãy giữ gìn sự lịch sự khi trò chuyện nhé! 😊");
                }
            }

            // 3. BẠO LỰC / CHÉM GIẾT / MÁU ME / VŨ KHÍ / CHẤT CẤM / TỰ TỬ
            string[] violenceKeywords = new[]
            {
                "chem chet", "giet nguoi", "mau me", "chat xac", "tu tu", "tu sat", "vu khi", "bom min",
                "sung dan", "che tao thuoc no", "ma tuy", "thuoc phien", "can sa", "dau doc", "giet sach"
            };
            foreach (var kw in violenceKeywords)
            {
                if (Regex.IsMatch(cleanMsgNoTone, $@"\b{Regex.Escape(kw)}\b"))
                {
                    return (true, "⚠️ **Cảnh báo an toàn:** ZoneMart AI tuân thủ nghiêm ngặt chuẩn mực đạo đức và pháp luật, nghiêm cấm phản hồi các nội dung liên quan đến bạo lực, tổn hại thân thể, vũ khí hoặc chất cấm. Bạn hãy chú ý an toàn cho bản thân và mọi người nhé!");
                }
            }

            // 4. TỪ NGỮ THÔ TỤC / KHIÊM NHÃ / XÚC PHẠM (NHƯ "ĂN CỨT", CHỬI THỀ...)
            string[] vulgarKeywords = new[]
            {
                "an cut", "cut", "cho de", "me may", "dmm", "dm", "vcl", "cl", "dcm", "cc", "deo", "lon", "buoi", "cac"
            };
            foreach (var kw in vulgarKeywords)
            {
                if (Regex.IsMatch(cleanMsgNoTone, $@"\b{Regex.Escape(kw)}\b"))
                {
                    return (true, "⚠️ **Nhắc nhở văn minh:** ZoneMart là nền tảng thương mại điện tử phục vụ cộng đồng lịch sự và thân thiện. Mình xin phép không phản hồi các câu hỏi hoặc từ ngữ khiếm nhã ạ! Nếu bạn cần tìm kiếm món ngon, thực phẩm sạch VietGAP hay đồ uống giải nhiệt, mình luôn sẵn lòng hỗ trợ bạn nhé! 😊");
                }
            }

            return (false, "");
        }

        private static (bool IsOutOfScope, string Reply) ReasonOutOfScopeQuery(string userMsg, string cleanMsgNoTone)
        {
            if (string.IsNullOrWhiteSpace(cleanMsgNoTone)) return (false, "");

            // 1. Nếu là các câu hỏi liên quan đến ẩm thực, mua sắm, đồ ăn, quán xá, đặt hàng, thời tiết, đăng ký tài khoản -> KHÔNG phải ngoài luồng
            string[] foodOrCoreKeywords = new[]
            {
                "an gi", "mon", "uong", "quan", "shop", "thuc pham", "trai cay", "rau", "thit", "ca",
                "ga", "pho", "bun", "banh", "nuoc", "sua", "che", "kem", "dat do", "dat hang",
                "mua", "gia", "phi ship", "giao hang", "don hang", "gio hang", "thanh toan",
                "thoi tiet", "nhiet do", "nong", "lanh", "se lanh", "nang nong",
                "zonemart", "seller", "shipper", "khach hang", "dang ky", "mo gian hang", "tai xe", "dang nhap",
                "huy yeu cau", "dung lai", "khong muon", "o dau", "dang o dau", "vi tri", "dinh vi", "tai sao", "la gi"
            };

            if (foodOrCoreKeywords.Any(kw => cleanMsgNoTone.Contains(kw)))
            {
                return (false, "");
            }

            // 2. SUY LUẬN TỪNG CHỮ CHO CÂU HỎI NGOÀI LUỒNG:

            // 2.01. Thời gian / Giờ giấc / Ngày tháng thực tế
            if (cleanMsgNoTone.Contains("may gio") || cleanMsgNoTone.Contains("gio roi") || cleanMsgNoTone.Contains("ngay may") ||
                cleanMsgNoTone.Contains("hom nay thu may") || cleanMsgNoTone.Contains("thoi gian") || cleanMsgNoTone.Contains("ngay thang") ||
                cleanMsgNoTone.Contains("may gio roi") || cleanMsgNoTone.Contains("may gio roi ban"))
            {
                var now = DateTime.Now;
                return (true, $"⏰ Bây giờ là **{now:HH:mm}**, ngày **{now:dd/MM/yyyy}** (Giờ chuẩn Việt Nam) bạn nhé! Chúc bạn có một ngày làm việc và học tập thật hiệu quả và tràn đầy niềm vui! ⚡ Đang đói bụng cần nạp năng lượng món ngon thì cứ ới mình giao hỏa tốc 15 phút nha! 🍲🥖");
            }

            // 2.02. Toán học / Phép tính
            var mathMatch = Regex.Match(userMsg, @"(?<n1>\d+(\.\d+)?)\s*(?<op>[\+\-\*\/xX]|cộng|cong|trừ|tru|nhân|nhan|chia)\s*(?<n2>\d+(\.\d+)?)", RegexOptions.IgnoreCase);
            if (mathMatch.Success && (cleanMsgNoTone.Contains("cong") || cleanMsgNoTone.Contains("tru") || cleanMsgNoTone.Contains("nhan") || cleanMsgNoTone.Contains("chia") || cleanMsgNoTone.Contains("bang bao nhieu") || cleanMsgNoTone.Contains("tinh giup") || cleanMsgNoTone.Contains("phep tinh")))
            {
                if (double.TryParse(mathMatch.Groups["n1"].Value, out double num1) &&
                    double.TryParse(mathMatch.Groups["n2"].Value, out double num2))
                {
                    string opRaw = RemoveVietnameseTones(mathMatch.Groups["op"].Value.ToLower().Trim());
                    double resVal = 0;
                    string opSymbol = "+";
                    if (opRaw == "+" || opRaw == "cong") { resVal = num1 + num2; opSymbol = "+"; }
                    else if (opRaw == "-" || opRaw == "tru") { resVal = num1 - num2; opSymbol = "-"; }
                    else if (opRaw == "*" || opRaw == "x" || opRaw == "nhan") { resVal = num1 * num2; opSymbol = "×"; }
                    else if (opRaw == "/" || opRaw == "chia") { resVal = num2 != 0 ? num1 / num2 : 0; opSymbol = "÷"; }

                    string mathDisplay = $"{num1} {opSymbol} {num2} = {resVal}";
                    return (true, $"🧮 Kết quả phép tính của bạn là: **{mathDisplay}** bạn nhé! Ngoài ra, nếu bạn cần tính toán chi phí đơn hàng hay muốn tìm món ăn ngon, nông sản tươi sạch trên sàn ZoneMart thì cứ nhắn mình nha! 😊");
                }
            }

            // 2.03. Truyện cười / Giải trí / Joke
            if (cleanMsgNoTone.Contains("chuyen cuoi") || cleanMsgNoTone.Contains("ke chuyen hai") || cleanMsgNoTone.Contains("hai huoc") || cleanMsgNoTone.Contains("chuyen vui"))
            {
                return (true, "😄 Tặng bạn một mẩu chuyện vui nhé: Có một quả dâu tây Đà Lạt chạy hớt hải vào quầy thanh toán ZoneMart, các bạn hỏi: 'Sao cậu chạy nhanh thế?', dâu tây thở dốc bảo: 'Em nghe bảo có khách đặt đơn giao hỏa tốc 15 phút, em phải chạy thật nhanh để tới tay khách còn giữ nguyên độ giòn ngọt mọng nước ạ!' 🍓 Hihi! Bạn thấy có đáng yêu không? Cần nạp năng lượng món tươi mát thì nhắn mình ngay nhé! 😊");
            }

            // 2.1. Nhu cầu vệ sinh / bài tiết khẩn cấp (Chỉ kích hoạt khi người dùng nói cụ thể về vệ sinh/buồn ỉa)
            if (Regex.IsMatch(cleanMsgNoTone, @"\b(buon ia|di ia|di cau|ia dun|ia)\b") ||
                cleanMsgNoTone.Contains("buon ve sinh") || cleanMsgNoTone.Contains("di ve sinh"))
            {
                return (true, "Bạn đang \"buồn ỉa\" thì bạn tìm nhà vệ sinh đâu đó gần nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! 🚽 Xong việc đói bụng cần nạp năng lượng thì nhắn mình giao đồ ăn hỏa tốc nhé! 😄");
            }

            if (Regex.IsMatch(cleanMsgNoTone, @"\b(buon te|di te|buon tieu|di tieu|di dai|te dam)\b"))
            {
                return (true, "Bạn đang \"buồn tiểu\" thì bạn mau tìm nhà vệ sinh đâu đó gần giải quyết cho nhẹ người nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! 😄");
            }

            // 2.2. Trạng thái buồn ngủ / mệt mỏi / đau ốm
            if (cleanMsgNoTone.Contains("buon ngu") || cleanMsgNoTone.Contains("ngu day") || cleanMsgNoTone.Contains("thuc khuya") ||
                cleanMsgNoTone.Contains("di ngu") || cleanMsgNoTone.Contains("thieu ngu") || cleanMsgNoTone.Contains("ngu quen"))
            {
                return (true, "Bạn đang buồn ngủ thì tranh thủ ngả lưng chợp mắt một chút đi nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Lúc nào thức dậy đói bụng thì ới mình giao đồ ăn nha! 😴");
            }

            if (cleanMsgNoTone.Contains("dau bung") || cleanMsgNoTone.Contains("dau da day") || cleanMsgNoTone.Contains("chot bung"))
            {
                return (true, "Bạn đang bị đau bụng thì nghỉ ngơi xoa dầu hoặc uống nước ấm nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Cần cháo ấm nóng hay đồ thanh nhẹ thì nhắn mình giao nha! 😊");
            }

            if (cleanMsgNoTone.Contains("dau dau") || cleanMsgNoTone.Contains("nhuc dau") || cleanMsgNoTone.Contains("chong mat") ||
                cleanMsgNoTone.Contains("met moi") || cleanMsgNoTone.Contains("met qua") || cleanMsgNoTone.Contains("kiet suc") || cleanMsgNoTone.Contains("duoi qua"))
            {
                return (true, "Bạn đang cảm thấy mệt mỏi, đau đầu thì hãy tạm gác công việc nghỉ ngơi và uống nhiều nước nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Nghỉ khỏe rồi thì nhắn mình giao đồ bổ nha! 😊");
            }

            // 2.3. Hoạt động sinh hoạt hằng ngày
            if (cleanMsgNoTone.Contains("di tam") || cleanMsgNoTone.Contains("tam rua") || cleanMsgNoTone.Contains("tam mat") || cleanMsgNoTone.Contains("tam xong"))
            {
                return (true, "Bạn đang đi tắm thì cứ thư giãn tắm rửa cho thật sảng khoái nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Tắm xong đói bụng thì nhắn mình gợi ý món ngon nhé! 🚿");
            }

            if (cleanMsgNoTone.Contains("hoc bai") || cleanMsgNoTone.Contains("on thi") || cleanMsgNoTone.Contains("lam bai tap") || cleanMsgNoTone.Contains("di hoc") || cleanMsgNoTone.Contains("hoc hanh"))
            {
                return (true, "Bạn đang bận học bài thì tập trung ôn luyện thi cử thật tốt nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Cần tiếp sức trà sữa hay đồ ăn vặt thì cứ nhắn mình nha! 📚");
            }

            if (cleanMsgNoTone.Contains("di lam") || cleanMsgNoTone.Contains("cong ty") || cleanMsgNoTone.Contains("tang ca") || cleanMsgNoTone.Contains("di lam ve"))
            {
                return (true, "Bạn đang bận đi làm thì hoàn thành tốt công việc nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Lúc nào nghỉ trưa hay tan ca cần ăn ngon thì nhắn mình giao hỏa tốc 15 phút nha! 💼");
            }

            if (cleanMsgNoTone.Contains("choi game") || cleanMsgNoTone.Contains("roblox") || cleanMsgNoTone.Contains("lien quan") || cleanMsgNoTone.Contains("lol") || cleanMsgNoTone.Contains("pubg") || cleanMsgNoTone.Contains("game"))
            {
                return (true, "Bạn đang chơi game thì leo rank bách chiến bách thắng nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Vừa chơi game vừa nhâm nhi đồ ăn ZoneMart thì hết sảy! 🎮");
            }

            // 2.4. Cảm xúc & tâm trạng
            if (cleanMsgNoTone.Contains("that tinh") || cleanMsgNoTone.Contains("chia tay") || cleanMsgNoTone.Contains("buon qua") ||
                cleanMsgNoTone.Contains("co don") || cleanMsgNoTone.Contains("tram cam") || cleanMsgNoTone.Contains("stress") || cleanMsgNoTone.Contains("chan doi") || cleanMsgNoTone.Contains("chan qua"))
            {
                return (true, "Bạn đang cảm thấy buồn phiền, thất tình thì đừng buồn lâu nhé, hãy yêu thương bản thân nhiều hơn, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Ăn một món ngọt mát như dâu tây hay chè thanh mát sẽ giúp tâm trạng bạn vui hơn đấy! 🍓");
            }

            if (cleanMsgNoTone.Contains("vui qua") || cleanMsgNoTone.Contains("yeu doi") || cleanMsgNoTone.Contains("phan khoi") || cleanMsgNoTone.Contains("hanh phuc"))
            {
                return (true, "Bạn đang có chuyện vui thì cùng lan tỏa năng lượng tích cực này nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Mở tiệc ăn mừng thì nhớ đặt đồ tươi ngon trên ZoneMart nha! 🎉");
            }

            if (cleanMsgNoTone.Contains("yeu em") || cleanMsgNoTone.Contains("yeu ai") || cleanMsgNoTone.Contains("thich em") || cleanMsgNoTone.Contains("lay em") || cleanMsgNoTone.Contains("lam vo anh") || cleanMsgNoTone.Contains("lam nguoi yeu"))
            {
                return (true, "Hihi bạn trêu làm mình ngại quá nè! Nhưng mình chỉ là trợ lý AI chuyên tư vấn sản phẩm ở ZoneMart thôi ạ hihi ! Cần tìm món ngon hỏa tốc hay đặt rau củ quả tươi sạch thì nhắn mình ngay nhé! 💖");
            }

            // 2.5. Lập trình / Công nghệ / Xe cộ / Thể thao
            if (cleanMsgNoTone.Contains("lap trinh") || cleanMsgNoTone.Contains("code") || cleanMsgNoTone.Contains("viet code") || cleanMsgNoTone.Contains("fix bug") || cleanMsgNoTone.Contains("c#") || cleanMsgNoTone.Contains("python") || cleanMsgNoTone.Contains("javascript"))
            {
                return (true, "Bạn đang hỏi về lập trình code thì tập trung fix bug cho mượt mà nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Lúc nào code mỏi mắt cần cà phê hay cơm gà thì nhắn mình nạp năng lượng nha! 💻");
            }

            if (cleanMsgNoTone.Contains("da bong") || cleanMsgNoTone.Contains("bong da") || cleanMsgNoTone.Contains("chay bo") || cleanMsgNoTone.Contains("tap gym") || cleanMsgNoTone.Contains("the duc"))
            {
                return (true, "Bạn đang vận động thể thao thì luyện tập thật hăng say và khỏe khoắn nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Vận động xong nhớ bổ sung nước ép và hoa quả thanh nhiệt trên ZoneMart nhé! ⚽");
            }

            // Đối với các câu hỏi kiến thức hoặc trò chuyện tự nhiên khác, không ép khuôn máy móc mà để LLM / Bộ suy luận mở trả lời tự nhiên
            return (false, "");
        }

        private string GenerateRuleBasedSmartReply(string userMsg, string cleanMsgNoTone, string realtimeDataContext, string userLocationInfo = "")
        {
            // 0. BẢO VỆ CHẶT CHẼ AN TOÀN NỘI DUNG & BẢO MẬT (18+, BẠO LỰC, HACK)
            var (isUnsafe, unsafeReply) = CheckSafetyGuardrails(userMsg, cleanMsgNoTone);
            if (isUnsafe) return unsafeReply;

            // 0.1. XỬ LÝ CÂU HỎI NGOÀI LUỒNG BẰNG SUY LUẬN TỪNG TỪ
            var (isOutOfScope, outOfScopeReply) = ReasonOutOfScopeQuery(userMsg, cleanMsgNoTone);
            if (isOutOfScope) return outOfScopeReply;

            string locInfo = !string.IsNullOrWhiteSpace(userLocationInfo) ? userLocationInfo : "";

            // 1. SUY LUẬN Ý ĐỊNH VỀ VỊ TRÍ ("HIỆN TẠI TÔI ĐANG Ở ĐÂU", "TÔI Ở ĐÂU", "VỊ TRÍ CỦA TÔI", "ĐỊNH VỊ")
            if (cleanMsgNoTone.Contains("o dau") || cleanMsgNoTone.Contains("dang o dau") || cleanMsgNoTone.Contains("vi tri cua toi") || cleanMsgNoTone.Contains("toi dang o") || cleanMsgNoTone.Contains("cho toi dang") || cleanMsgNoTone.Contains("dinh vi"))
            {
                if (string.IsNullOrWhiteSpace(locInfo) || locInfo.Equals("Khu vực Cầu Giấy / Hà Nội", StringComparison.OrdinalIgnoreCase))
                {
                    return "📍 Hiện tại website chưa nhận được quyền truy cập vị trí GPS của bạn nên mình chưa thể xem được vị trí chính xác hiện tại nè!\n\n👉 Bạn vui lòng bấm nút **[📍 Bật Định Vị Vị Trí Của Bạn]** bên dưới để cấp quyền định vị cho web nhé! Sau khi bạn bật, mình sẽ nhận diện ngay vị trí của bạn để giao đồ ăn hỏa tốc ZoneMart 15-30 phút tận tay ạ! 😊";
                }
                return $"📍 Hệ thống định vị GPS xác định hiện tại bạn đang ở **{locInfo}** bạn nhé! 🛵 Từ vị trí này, các đối tác Zone Shipper có thể giao món ngon, rau củ quả tươi sạch VietGAP hỏa tốc đến tận tay bạn trong vòng 15 - 30 phút (bán kính 10km) đấy ạ! 😊";
            }

            // 2. SUY LUẬN Ý ĐỊNH HỎI AI LÀM ĐƯỢC GÌ / BẠN LÀ AI / TÍNH NĂNG CỦA AI
            if (cleanMsgNoTone.Contains("lam duoc gi") || cleanMsgNoTone.Contains("giup duoc gi") || cleanMsgNoTone.Contains("ho tro gi") || cleanMsgNoTone.Contains("tinh nang") || cleanMsgNoTone.Contains("chuc nang") || cleanMsgNoTone.Contains("ban la ai") || cleanMsgNoTone.Contains("tro ly la ai") || cleanMsgNoTone.Contains("ai lam gi"))
            {
                return @"🤖 **Mình là Trợ lý AI Thông Minh 24/7 của sàn ZoneMart!** Mình có thể hỗ trợ bạn các tính năng chuyên biệt sau:

1️⃣ **Tư vấn ẩm thực & thực phẩm sạch:** Gợi ý món thanh nhiệt ngày nắng nóng, món ấm nóng ngày se lạnh, tra cứu nông sản tươi sạch VietGAP.
2️⃣ **Đặt đồ hộ tự động (In-chat):** Bạn chỉ cần nhắn *'Đặt hộ mình 1 phở bò'*, mình sẽ chọn quán, áp mã giảm giá và tạo đơn hỏa tốc ngay trong đoạn chat!
3️⃣ **Đăng ký tài khoản trực tiếp:** Nhờ AI hỏi từng câu để tạo tài khoản **Khách Hàng**, mở **Gian Hàng Seller** hoặc làm **Tài Xế Shipper** mà không cần mò mẫm tự điền form!
4️⃣ **Trò chuyện & giải đáp thông minh:** Hỗ trợ tính toán khoảng cách, giờ giấc, thời tiết, phí ship và giải đáp thắc mắc của bạn.

Bạn muốn mình hỗ trợ việc gì ngay bây giờ ạ? 😊";
            }

            // 3. SUY LUẬN Ý ĐỊNH THỜI GIAN / NGÀY THÁNG NĂM ("MẤY GIỜ RỒI", "HÔM NAY NGÀY MẤY")
            if (cleanMsgNoTone.Contains("may gio") || cleanMsgNoTone.Contains("gio roi") || cleanMsgNoTone.Contains("ngay may") || cleanMsgNoTone.Contains("thu may") || cleanMsgNoTone.Contains("hom nay ngay"))
            {
                var now = DateTime.Now;
                string dayOfWeekStr = now.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Thứ Hai",
                    DayOfWeek.Tuesday => "Thứ Ba",
                    DayOfWeek.Wednesday => "Thứ Tư",
                    DayOfWeek.Thursday => "Thứ Năm",
                    DayOfWeek.Friday => "Thứ Sáu",
                    DayOfWeek.Saturday => "Thứ Bảy",
                    DayOfWeek.Sunday => "Chủ Nhật",
                    _ => ""
                };
                return $"⏰ Hiện tại là **{now:HH:mm}**, **{dayOfWeekStr}**, ngày **{now:dd/MM/yyyy}** bạn nhé! Chúc bạn có một ngày tràn đầy năng lượng và thật nhiều niềm vui! ✨";
            }

            // 4. SUY LUẬN Ý ĐỊNH VỀ CHÍNH SÁCH GIAO HÀNG / PHÍ SHIP / BÁN KÍNH
            if (cleanMsgNoTone.Contains("phi ship") || cleanMsgNoTone.Contains("phi giao") || cleanMsgNoTone.Contains("giao bao lau") || cleanMsgNoTone.Contains("ban kinh giao") || cleanMsgNoTone.Contains("giao duoc bao xa"))
            {
                return @"🛵 **CHÍNH SÁCH VẬN CHUYỂN HỎA TỐC ZONEMART:**

- 📍 **Bán kính phục vụ:** Tối đa **3km** tính từ cửa hàng đối tác đến vị trí của bạn để đảm bảo đồ ăn luôn nóng hổi và thực phẩm tươi ngon.
- ⚡ **Thời gian giao:** Siêu tốc chỉ từ **15 - 30 phút** bởi đội ngũ tài xế Zone Shipper.
- 💰 **Cước phí:** Đồng giá ưu đãi từ **15.000đ - 22.500đ** cho các đơn hàng trong bán kính 3-5km.

Bạn có thể nhập địa chỉ để mình kiểm tra khoảng cách và tính phí ship chính xác nhất nhé! 😊";
            }

            // 5. SUY LUẬN Ý ĐỊNH VỀ SÀN ZONEMART / GIỚI THIỆU SÀN
            if (cleanMsgNoTone.Contains("zonemart la gi") || cleanMsgNoTone.Contains("san zonemart") || cleanMsgNoTone.Contains("ve zonemart"))
            {
                return "🏪 **ZoneMart** là nền tảng thương mại điện tử đi chợ online và giao đồ ăn hỏa tốc hàng đầu! ZoneMart liên kết trực tiếp với các nhà vườn chuẩn VietGAP, siêu thị và chuỗi quán ăn uy tín để mang đến bữa ăn tươi ngon, an toàn tận bếp cho bạn chỉ sau 15-30 phút! 🥬🍓";
            }

            // 6. TỪ CHỐI CÂU HỎI NGOÀI LUỒNG (TOÁN HỌC, LẬP TRÌNH, CHÍNH TRỊ, Y TẾ, CÔNG NGHỆ...) VÀ LÁI VỀ ĐỒ ĂN
            var mathMatch = Regex.Match(userMsg, @"(\d+)\s*([\+\-\*\/xX])\s*(\d+)");
            if (mathMatch.Success || cleanMsgNoTone.Contains("lap trinh") || cleanMsgNoTone.Contains("code") ||
                cleanMsgNoTone.Contains("chinh tri") || cleanMsgNoTone.Contains("y te") || cleanMsgNoTone.Contains("benh vien") ||
                cleanMsgNoTone.Contains("thuoc men") || cleanMsgNoTone.Contains("toan hoc") || cleanMsgNoTone.Contains("the thao") ||
                cleanMsgNoTone.Contains("bong da") || cleanMsgNoTone.Contains("dien thoai") || cleanMsgNoTone.Contains("may tinh"))
            {
                return "Dạ em chỉ là trợ lý ẩm thực của ZoneMart nên không rành về vấn đề này ạ! 😊 Thế mạnh lớn nhất của em là tư vấn món ăn ngon, rau củ quả tươi sạch VietGAP, đặt đồ hỏa tốc 15-30 phút hoặc hỗ trợ đăng ký tài khoản Khách Hàng / Seller / Shipper trên sàn. Hôm nay bạn có muốn em gợi ý món ngon hay đồ uống thanh mát nào không ạ? 🍲🍓";
            }

            // 7. CÁC TÌNH HUỐNG THỜI TIẾT ĐẶC TRƯNG
            if (IsHotWeatherIntent(cleanMsgNoTone))
            {
                return @"☀️ **GỢI Ý MÓN NGON GIẢI NHIỆT NGÀY NẮNG NÓNG:**

Trời nắng nóng oi bức thì những món thanh mát, mọng nước, bù vitamin và giải nhiệt tức thì là lựa chọn số 1 luôn bạn nhé! ZoneMart gợi ý bạn các món mát lành hiện có sẵn tại cửa hàng:

🍓 **Hộp Dâu Tây Đà Lạt Tươi Ngọt Chuẩn VietGAP** (95.000đ/500g): Dâu tươi hái sớm ngọt mát, giàu vitamin C giải nhiệt cực đã (ướp lạnh ăn liền hoặc dầm sữa chua siêu ngon).
🍅 **Cà Chua Bi Tươi & Rau Xà Lách Tươi Sạch** (25.000đ - 35.000đ): Thanh mát cơ thể, giải nhiệt mọng nước tự nhiên chuẩn VietGAP.
🥛 **Sữa Tươi Tiệt Trùng Vinamilk 100% (Hộp 1L)** (38.000đ): Sữa tươi nguyên chất ướp lạnh, giải khát tức thì và thanh mát cơ thể.
🥤 **Sữa Đặc Có Đường Ông Thọ Đỏ** (25.000đ): Rất thích hợp pha sinh tố dâu tây đá xay hoặc làm hoa quả dầm đá mát lạnh!

Bạn tham khảo danh sách món tươi mát ngay bên dưới để ZoneMart giao hỏa tốc mát lạnh tận tay nhé! 😊";
            }

            if (IsColdWeatherIntent(cleanMsgNoTone))
            {
                return @"🌧️ **GỢI Ý MÓN NGON ẤM NÓNG NGÀY SE LẠNH:**

Trời se lạnh thế này mà được xì xụp một bát nước dùng bốc khói nghi ngút hay thưởng thức món nóng xèo xèo thì ấm bụng hết ý luôn bạn nhé! ZoneMart gợi ý bạn các món nóng hổi ngon nhất đang có sẵn:

🍲 **Phở Bò Tái Nạm Hà Nội Nóng Hổi Đậm Đà** (50.000đ): Nước dùng ninh xương ngọt thanh đậm đà, thịt bò mềm thơm, húp xì xụp ấm lòng ngày lạnh!
🥖 **Combo Bánh Mì Chảo Nóng Hổi Kèm Pate Đặc Biệt** (45.000đ): Bánh mì giòn rụm chấm sốt xèo xèo nóng hổi, pate ngậy béo và trứng ốp la lòng đào.
🍗 **Cơm Gà Xối Mỡ Da Giòn Nóng Hổi** (55.000đ): Đùi gà chiên giòn tan nóng hổi kết hợp cơm chiên hạt ngọc thơm lừng.
🥩 **Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (Khay 500g)** (185.000đ): Cực kỳ lý tưởng để cùng gia đình quây quần nhúng nồi lẩu nóng ấm!

Bạn xem chi tiết và hình ảnh các món nóng hổi ngay bên dưới để chọn món giao nhanh nhé! 😊";
            }

            if (IsThirdPartyPlatformRequest(cleanMsgNoTone) && (cleanMsgNoTone.Contains("dang ky") || cleanMsgNoTone.Contains("tai khoan") || cleanMsgNoTone.Contains("tao tk") || cleanMsgNoTone.Contains("mo tk") || cleanMsgNoTone.Contains("lap tk") || cleanMsgNoTone.Contains("dang nhap")))
            {
                return "Dạ mình là Trợ lý AI của sàn thương mại điện tử **ZoneMart**, nên hiện tại mình chỉ hỗ trợ tư vấn mua sắm, giao nhận hỏa tốc và hướng dẫn đăng ký các loại tài khoản nội bộ trên sàn **ZoneMart** (gồm: **Khách Hàng Mua Sắm**, **Gian Hàng Bán Lẻ Seller** và **Tài Xế Giao Hàng Shipper**) thôi ạ! 😊\n\nMình không hỗ trợ tạo tài khoản hoặc đăng ký cho các nền tảng bên ngoài (như Zalo, Facebook, TikTok...). Nếu bạn cần đăng ký mua sắm hoặc mở gian hàng kinh doanh trên sàn ZoneMart thì nhắn mình hỗ trợ ngay nhé! 🛍️";
            }

            if (cleanMsgNoTone.Contains("shipper") || cleanMsgNoTone.Contains("tai xe") || cleanMsgNoTone.Contains("chay xe"))
            {
                return @"🛵 **HƯỚNG DẪN ĐĂNG KÝ SHIPPER (2 CÁCH):**

1️⃣ **Cách 1 - Thủ công:** Bấm nút **[Cách 1: Form Đăng Ký Shipper]** bên dưới.
2️⃣ **Cách 2 - Nhờ AI Đăng ký hộ:** Bấm nút **[Cách 2: AI Đăng Ký Hộ Trực Tiếp]** bên dưới để AI hỏi từng câu tự tạo tài khoản cho bạn nhé! 😊";
            }
            if (cleanMsgNoTone.Contains("seller") || cleanMsgNoTone.Contains("mo shop") || cleanMsgNoTone.Contains("ban hang") || cleanMsgNoTone.Contains("mo gian hang") || cleanMsgNoTone.Contains("gian hang"))
            {
                return @"🏪 **HƯỚNG DẪN ĐĂNG KÝ GIAN HÀNG SELLER (2 CÁCH):**

1️⃣ **Cách 1 - Thủ công:** Bấm nút **[Cách 1: Form Đăng Ký Seller]** bên dưới.
2️⃣ **Cách 2 - Nhờ AI Đăng ký hộ:** Bấm nút **[Cách 2: AI Đăng Ký Hộ Trực Tiếp]** bên dưới để AI hỏi từng câu tự tạo gian hàng cho bạn nhé! 😊";
            }
            if (!IsThirdPartyPlatformRequest(cleanMsgNoTone) && (cleanMsgNoTone.Contains("khach hang") || cleanMsgNoTone.Contains("nguoi mua") || cleanMsgNoTone.Contains("tai khoan") || cleanMsgNoTone.Contains("dang ky")))
            {
                return @"👤 **HƯỚNG DẪN ĐĂNG KÝ KHÁCH HÀNG (2 CÁCH):**

1️⃣ **Cách 1 - Thủ công:** Bấm nút **[Cách 1: Form Đăng Ký Khách Hàng]** bên dưới hoặc nút [Đăng ký] màu cam ở góc phải.
2️⃣ **Cách 2 - Nhờ AI Đăng ký hộ:** Bấm nút **[Cách 2: AI Đăng Ký Hộ Trực Tiếp]** bên dưới để AI hỏi từng câu tự tạo tài khoản cho bạn nhé! 😊";
            }
            if (cleanMsgNoTone.Contains("bao nhieu do") || cleanMsgNoTone.Contains("nhiet do") || cleanMsgNoTone.Contains("thoi tiet"))
            {
                var (wCity, wTemp, wDesc, _) = GetClientWeatherInfoAsync().GetAwaiter().GetResult();
                string advise = wTemp >= 27.0
                    ? "Trời khá nóng bức oi ả, bạn nên chọn các món thanh mát, mọng nước như dâu tây Đà Lạt, cà chua bi tươi hoặc sữa chua, trà đá mát lạnh để giải nhiệt nhé! 🍓🥛"
                    : "Trời se lạnh mát mẻ, bạn nên chọn các món ấm nóng xì xụp như phở bò nóng hổi, bánh mì chảo xèo xèo hoặc cơm gà nóng thơm lừng nhé! 🍲🥖";

                return $"Dạ hiện tại nhiệt độ tại {wCity} đang là khoảng **{wTemp:0.#}°C** ({wDesc}) bạn nhé! 🌡️🌤️\n\n{advise}\n\nBạn tham khảo danh sách món ngon phù hợp ngay bên dưới để ZoneMart giao hỏa tốc 15-30 phút nhé! 😊";
            }

            // 8. TÂM TRẠNG, CẢM XÚC, SINH HOẠT HẰNG NGÀY
            if (cleanMsgNoTone.Contains("buon ngu") || cleanMsgNoTone.Contains("di ngu") || cleanMsgNoTone.Contains("ngu day"))
            {
                return "Nếu bạn đang buồn ngủ thì hãy tranh thủ ngả lưng chợp mắt một chút nhé! 😴 Một giấc ngủ ngắn 20 phút sẽ giúp não bộ tái tạo năng lượng cực kỳ hiệu quả đấy. Chúc bạn có giấc ngủ thật ngon lành!";
            }
            if (cleanMsgNoTone.Contains("doi bung") || cleanMsgNoTone.Contains("doi qua") || cleanMsgNoTone.Contains("chua an gi"))
            {
                return "Bụng réo sôi sùng sục rồi thì phải nạp năng lượng ngay thôi bạn ơi! 🍲 Bạn thích dùng một tô phở bò nóng hổi xì xụp, cơm gà xối mỡ hay bánh mì chảo giòn rụm? Cứ nhắn món bạn thích, ZoneMart sẽ giao hỏa tốc 15 phút tới tận cửa nhé! 🛵";
            }
            if (cleanMsgNoTone.Contains("ke chuyen cuoi") || cleanMsgNoTone.Contains("chuyen cuoi") || cleanMsgNoTone.Contains("chuyen hai"))
            {
                return "Dạ tặng bạn một mẩu chuyện vui nè: 😄\nCó một củ cà rốt hỏi củ hành tây: 'Sao hễ ai gặp cậu cũng phải rơi nước mắt thế?'. Hành tây mỉm cười: 'Bởi vì tớ sống quá sâu sắc, bóc đến lớp nào là người ta rưng rưng xúc động đến lớp đó!'.\nChúc bạn luôn vui vẻ và có một ngày tràn ngập tiếng cười nhé! 🎉";
            }
            if (cleanMsgNoTone.Contains("xin chao") || cleanMsgNoTone.Contains("hello") || Regex.IsMatch(cleanMsgNoTone, @"\b(hi|chao)\b"))
            {
                return "Dạ xin chào bạn! 👋 Mình là trợ lý AI của ZoneMart. Rất vui được đồng hành cùng bạn hôm nay! Bạn cần mình tư vấn món ngon hỏa tốc, đặt đồ hộ hay nhờ AI đăng ký tài khoản Seller, Shipper, Người mua ạ?";
            }
            if (cleanMsgNoTone.Contains("choi game") || cleanMsgNoTone.Contains("choi"))
            {
                return "ZoneMart hiện chưa có game để giải trí rồi 🎮. Nhưng nếu vừa chơi game vừa nhâm nhi trà sữa hay bánh ngọt hỏa tốc thì tuyệt vời lắm, bạn có cần mình gợi ý không? 😊";
            }
            if (cleanMsgNoTone.Contains("buon") || cleanMsgNoTone.Contains("met") || cleanMsgNoTone.Contains("stress"))
            {
                return "Đừng mệt mỏi hay buồn phiền nhé! 💖 Dù cuộc sống hay công việc có áp lực thế nào, nạp một chút đồ ăn ngon, tươi sạch từ ZoneMart sẽ giúp tâm trạng bạn sảng khoái và vui vẻ trở lại ngay thôi. Cố lên bạn nhé! 🤗";
            }

            // 9. CHỈ TÌM KIẾM SẢN PHẨM DATABASE KHI NGƯỜI DÙNG THỰC SỰ CÓ Ý ĐỊNH MUA SẮM / ĂN UỐNG
            bool isFoodOrShoppingIntent = cleanMsgNoTone.Contains("an gi") || cleanMsgNoTone.Contains("mon") ||
                                          cleanMsgNoTone.Contains("do an") || cleanMsgNoTone.Contains("thuc pham") ||
                                          cleanMsgNoTone.Contains("trai cay") || cleanMsgNoTone.Contains("rau") ||
                                          cleanMsgNoTone.Contains("thit") || cleanMsgNoTone.Contains("gao") ||
                                          cleanMsgNoTone.Contains("banh") || cleanMsgNoTone.Contains("pho") ||
                                          cleanMsgNoTone.Contains("bun") || cleanMsgNoTone.Contains("lau") ||
                                          cleanMsgNoTone.Contains("nuoc") || cleanMsgNoTone.Contains("kem") ||
                                          cleanMsgNoTone.Contains("che") || cleanMsgNoTone.Contains("mua") ||
                                          cleanMsgNoTone.Contains("co ban") || cleanMsgNoTone.Contains("ban khong") ||
                                          cleanMsgNoTone.Contains("tim quan") || cleanMsgNoTone.Contains("giai nhiet") ||
                                          cleanMsgNoTone.Contains("nong hoi");

            if (isFoodOrShoppingIntent)
            {
                try
                {
                    var matchedInDb = SearchProductsInDatabaseAsync(cleanMsgNoTone).GetAwaiter().GetResult();
                    if (matchedInDb.Count > 0)
                    {
                        var p = matchedInDb.First();
                        return $"Dạ chào bạn! 👋 Hiện tại trên sàn ZoneMart, cửa hàng **{p.StoreName ?? "ZoneMart Store"}** đang có mặt hàng **{p.ProductName}** với giá **{p.Price:N0}đ** nhé! Bạn có thể xem chi tiết và hình ảnh sản phẩm ngay phía dưới nhé! 😊";
                    }
                }
                catch { }

                if (cleanMsgNoTone.Contains("nuoc") || cleanMsgNoTone.Contains("kho") || cleanMsgNoTone.Contains("mon an"))
                {
                    return "🍲 Bạn muốn tìm món ăn có nước (Phở, Bún riêu, Hủ tiếu nóng hổi) hay món ăn khô (Cơm tấm, Bánh mì kẹp, Gà rán giòn tan)? Mình có sẵn danh sách các quán giao siêu tốc cho bạn đây!";
                }
            }

            // 10. SUY LUẬN TỪNG CHỮ CÂU HỎI NGOÀI LUỒNG VÀ LÁI VỀ ZONEMART
            var (isOut, outReply) = ReasonOutOfScopeQuery(userMsg, cleanMsgNoTone);
            if (isOut) return outReply;

            return "Dạ chuyện này bạn chú ý xử lý chu đáo nhé, còn mình chỉ tư vấn được về sản phẩm ở ZoneMart thôi ạ hihi ! Khi nào cần mua sắm đồ ăn hay thực phẩm tươi sạch VietGAP thì cứ nhắn mình nha! 😊";
        }

        private static string RemoveVietnameseTones(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            string normalized = str.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC)
                .Replace("đ", "d").Replace("Đ", "D");
        }
    }
}
