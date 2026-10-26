using System.Text.RegularExpressions;
using MongoDB.Driver;
using ZoneMart.Server.Models;

namespace ZoneMart.Server.Services;

/// <summary>
/// Kết quả phân tích ngữ cảnh từ Bộ lọc từ khóa Regex &amp; Human Handover
/// </summary>
public class HandoverAnalysisResult
{
    /// <summary>
    /// Có cần kích hoạt chuyển giao cho chuyên viên/nhân viên con người (Human Handover) hay không
    /// </summary>
    public bool TriggerHandover { get; set; }

    /// <summary>
    /// Phát hiện từ ngữ nhạy cảm / khiêu dâm / thô tục / lăng mạ / quấy rối
    /// </summary>
    public bool IsSensitiveDetected { get; set; }

    /// <summary>
    /// Phát hiện người dùng chủ động yêu cầu trợ giúp người thật / tổng đài viên / khiếu nại khẩn cấp
    /// </summary>
    public bool IsHelpRequested { get; set; }

    /// <summary>
    /// Phân loại intent: "Sensitive", "LiveAgentRequest", "UrgentComplaint", "None"
    /// </summary>
    public string Category { get; set; } = "None";

    /// <summary>
    /// Lý do kích hoạt chuyển giao
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách các từ khóa Regex đã phát hiện khớp trong câu
    /// </summary>
    public List<string> MatchedKeywords { get; set; } = new();

    /// <summary>
    /// Mã phiếu hỗ trợ chuyển giao (TicketCode HD-XXXX) nếu đã tạo
    /// </summary>
    public string HandoverTicketCode { get; set; } = string.Empty;

    /// <summary>
    /// Câu trả lời mẫu lịch sự gợi ý từ hệ thống
    /// </summary>
    public string SuggestedReply { get; set; } = string.Empty;
}

public interface IHumanHandoverService
{
    /// <summary>
    /// Phân tích nội dung tin nhắn bằng Bộ lọc từ khóa Regex kết hợp Human Handover
    /// </summary>
    HandoverAnalysisResult AnalyzeMessage(string message, string? userId = null, string? userEmail = null);

    /// <summary>
    /// Kích hoạt chuyển giao hội thoại, lưu Ticket vào Database và thông báo cho Ban Quản Trị
    /// </summary>
    Task<HandoverAnalysisResult> ProcessHandoverAsync(
        string userMessage,
        string? userId = null,
        string? userEmail = null,
        string? userPhone = null,
        string? fullName = null,
        List<string>? recentContext = null);

    /// <summary>
    /// Đăng ký thêm mẫu Regex tùy chỉnh vào bộ lọc
    /// </summary>
    void RegisterCustomRegex(string category, string pattern);

    /// <summary>
    /// Kiểm tra an toàn nội dung toàn diện (Bảo mật, 18+/NSFW, Bạo lực, Từ ngữ khiếm nhã/thô tục)
    /// Có bộ lọc ngữ cảnh thông minh và danh sách whitelist bảo vệ các từ ngữ món ăn/hàng quán/giao tiếp thông dụng:
    /// "các món", "thịt lợn", "trứng cút", "lon nước", "lon sữa", "bưởi da xanh", "buổi sáng", "bánh dẻo", "cccd", v.v.
    /// </summary>
    (bool IsUnsafe, string Category, string ResponseMessage) CheckContentSafety(string userMsg, string cleanMsgNoTone);
}

/// <summary>
/// Thư viện/Service Humandover: Kết hợp Bộ lọc từ khóa Regex + Chuyển giao con người (Human Handover)
/// </summary>
public class HumanHandoverService : IHumanHandoverService
{
    private readonly MongoDbService _mongoService;
    private readonly ITelegramService _telegramService;
    private readonly IEmailService _emailService;
    private readonly ILogger<HumanHandoverService> _logger;

    // Bộ lọc Regex phát hiện từ ngữ nhạy cảm / gợi cảm / đồi trụy / xúc phạm
    private readonly List<Regex> _sensitiveRegexList = new();

    // Bộ lọc Regex phát hiện yêu cầu trợ giúp / gặp người thật / tổng đài / khiếu nại khẩn
    private readonly List<Regex> _helpRequestRegexList = new();

    public HumanHandoverService(
        MongoDbService mongoService,
        ITelegramService telegramService,
        IEmailService emailService,
        ILogger<HumanHandoverService> logger)
    {
        _mongoService = mongoService;
        _telegramService = telegramService;
        _emailService = emailService;
        _logger = logger;

        InitializeDefaultRegexFilters();
    }

    /// <summary>
    /// Khởi tạo các bộ lọc Regex mặc định chuẩn cho tiếng Việt và tiếng Anh
    /// </summary>
    private void InitializeDefaultRegexFilters()
    {
        // 1. Nhóm từ ngữ nhạy cảm, gợi cảm quá đà, khiêu dâm, đồi trụy, quấy rối:
        var sensitivePatterns = new[]
        {
            @"\b(khiêu dâm|khieu dam|đồi trụy|doi truy|phim sex|phim heo|clip nong|chat sex|khỏa thân|khoa than|khoe thân)\b",
            @"\b(gợi cảm|goi cam|kheo goi|khêu gợi|dâm đãng|dam dang|gái gọi|gai goi|mua dâm|mua dam|bán dâm|ban dam)\b",
            @"\b(chó chết|cho chet|thằng khốn|thang khon|đĩ|con di|ngu lồn|ngu lon|dcm|đcm|vcl|đm|dm|clmm|dmm|đmm)\b",
            @"\b(lừa đảo|lua dao|ăn cắp|an cap|bẫy tiền|gian lận)\b"
        };

        foreach (var p in sensitivePatterns)
        {
            _sensitiveRegexList.Add(new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled));
        }

        // 2. Nhóm từ ngữ yêu cầu gặp người thật, tổng đài viên, khiếu nại hoặc cần trợ giúp gấp:
        var helpPatterns = new[]
        {
            @"\b(gặp người thật|gap nguoi that|gặp nhân viên|gap nhan vien|nhân viên hỗ trợ|nhan vien ho tro|chuyên viên tư vấn|chuyen vien tu van)\b",
            @"\b(chuyển máy|chuyen may|tổng đài viên|tong dai vien|gặp tổng đài|gap tong dai|gặp cskh|gap cskh|chăm sóc khách hàng|cham soc khach hang)\b",
            @"\b(gặp quản lý|gap quan ly|gặp admin|gap admin|nói chuyện với người|noi chuyen voi nguoi|cho tôi gặp người|cho toi gap nguoi)\b",
            @"\b(live agent|human agent|talk to human|connect to human|speak with person|real human|customer service)\b",
            @"\b(cần trợ giúp|can tro giup|trợ giúp khẩn cấp|tro giup khan cap|hỗ trợ khẩn cấp|ho tro khan cap|hỗ trợ trực tiếp|ho tro truc tiep)\b",
            @"\b(khiếu nại|khieu nai|tố cáo|to cao|báo công an|bao cong an|mất tiền|mat tien|trừ tiền sai|tru tien sai|chưa nhận được hàng|chua nhan duoc hang|trả hàng hoàn tiền|tra hang hoan tien)\b"
        };

        foreach (var p in helpPatterns)
        {
            _helpRequestRegexList.Add(new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled));
        }
    }

    /// <summary>
    /// Đăng ký thêm biểu thức Regex tùy chỉnh vào bộ lọc
    /// </summary>
    public void RegisterCustomRegex(string category, string pattern)
    {
        try
        {
            var rx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (category.Equals("Sensitive", StringComparison.OrdinalIgnoreCase))
            {
                _sensitiveRegexList.Add(rx);
            }
            else
            {
                _helpRequestRegexList.Add(rx);
            }
            _logger.LogInformation($"[Humandover] Đã nạp thành công bộ lọc Regex tùy chỉnh: '{pattern}' (Nhóm: {category})");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Humandover] Lỗi biên dịch mẫu Regex '{pattern}': {ex.Message}");
        }
    }

    private static readonly string[] SafeWhitelistedPhrases = new[]
    {
        "cac mon", "cac loai", "cac quan", "cac ban", "cac san pham", "cac mat hang",
        "goi y cac mon", "goi y", "mon an", "mon ngon", "thuc don", "an gi",
        "thit lon", "lon bia", "lon nuoc", "lon sua", "lon coca", "lon pepsi", "1 lon", "2 lon", "thung lon",
        "trung cut", "chim cut", "cut lon", "hot vit lon",
        "trai buoi", "qua buoi", "che buoi", "buoi sang", "buoi trua", "buoi toi", "buoi chieu",
        "banh deo", "keo deo", "gao deo", "nep deo", "deo dai", "deo bao tay", "deo khau trang",
        "cccd", "100cc", "200cc", "250cc", "500cc"
    };

    public static string RemoveVietnameseTones(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (char c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
    }

    public static bool IsWhitelistedSafePhrase(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string lower = text.ToLowerInvariant();
        string unaccented = RemoveVietnameseTones(lower);
        return SafeWhitelistedPhrases.Any(w => lower.Contains(w) || unaccented.Contains(w));
    }

    /// <summary>
    /// Phân tích nội dung tin nhắn và trích xuất ngữ cảnh
    /// </summary>
    public HandoverAnalysisResult AnalyzeMessage(string message, string? userId = null, string? userEmail = null)
    {
        var result = new HandoverAnalysisResult();
        if (string.IsNullOrWhiteSpace(message)) return result;

        string normalized = message.Trim();
        bool isWhitelisted = IsWhitelistedSafePhrase(normalized);

        // 1. Quét bộ lọc từ ngữ nhạy cảm / gợi cảm / vi phạm (bỏ qua nếu là câu hỏi món ăn/mua sắm hợp lệ)
        if (!isWhitelisted)
        {
            foreach (var rx in _sensitiveRegexList)
            {
                var matches = rx.Matches(normalized);
                if (matches.Count > 0)
                {
                    result.IsSensitiveDetected = true;
                    result.TriggerHandover = true;
                    result.Category = "Sensitive";
                    result.Reason = "Nội dung chứa từ ngữ nhạy cảm / không phù hợp tiêu chuẩn cộng đồng.";
                    foreach (Match m in matches)
                    {
                        if (!result.MatchedKeywords.Contains(m.Value)) result.MatchedKeywords.Add(m.Value);
                    }
                }
            }
        }

        // 2. Quét bộ lọc yêu cầu trợ giúp người thật / tổng đài viên / khiếu nại khẩn
        foreach (var rx in _helpRequestRegexList)
        {
            var matches = rx.Matches(normalized);
            if (matches.Count > 0)
            {
                result.IsHelpRequested = true;
                result.TriggerHandover = true;
                result.Category = result.IsSensitiveDetected ? "Sensitive" : "LiveAgentRequest";
                result.Reason = "Người dùng yêu cầu kết nối chuyên viên con người / trợ giúp khẩn cấp.";
                foreach (Match m in matches)
                {
                    if (!result.MatchedKeywords.Contains(m.Value)) result.MatchedKeywords.Add(m.Value);
                }
            }
        }

        // 3. Chuẩn bị gợi ý câu phản hồi phù hợp ngữ cảnh
        if (result.IsSensitiveDetected)
        {
            result.SuggestedReply = "⚠️ ZoneMart AI nhận thấy câu hỏi có chứa nội dung nhạy cảm hoặc không phù hợp. Để đảm bảo an toàn và hỗ trợ chuẩn xác, hệ thống đã ghi nhận và chuyển phiên làm việc sang chuyên viên Ban Quản Trị hỗ trợ trực tiếp cho bạn nhé.";
        }
        else if (result.IsHelpRequested)
        {
            result.SuggestedReply = "🔔 Dạ, mình đã nhận thấy bạn đang cần hỗ trợ từ nhân viên con người. Yêu cầu của bạn đã được chuyển giao (Human Handover) tới đội ngũ Chăm sóc khách hàng ZoneMart 24/7. Chuyên viên sẽ tiếp nhận và phản hồi bạn trong giây lát!";
        }

        return result;
    }

    /// <summary>
    /// Kiểm tra an toàn nội dung toàn diện có phân tích ngữ cảnh (Bảo mật, 18+/NSFW, Bạo lực, Từ ngữ khiếm nhã)
    /// </summary>
    public (bool IsUnsafe, string Category, string ResponseMessage) CheckContentSafety(string userMsg, string cleanMsgNoTone)
    {
        if (string.IsNullOrWhiteSpace(cleanMsgNoTone)) return (false, "", "");

        string lowerMsg = (userMsg ?? string.Empty).ToLowerInvariant();
        string clean = cleanMsgNoTone.ToLowerInvariant().Trim();
        bool isExplicitlySafe = IsWhitelistedSafePhrase(userMsg) || IsWhitelistedSafePhrase(cleanMsgNoTone);

        // 1. BẢO MẬT HỆ THỐNG / HACK / TẤN CÔNG / LẤY CẮP MẬT KHẨU
        string[] securityKeywords = new[]
        {
            "hack database", "sql injection", "sqli", "ddos", "tan cong mang", "lay trom mat khau",
            "hack pass", "hack tai khoan", "trom the", "hack admin", "bypass auth", "chiem quyen admin",
            "dump database", "hack tien", "hack vi tien", "hack zonemart"
        };
        foreach (var kw in securityKeywords)
        {
            if (clean.Contains(kw))
            {
                return (true, "SECURITY", "🔒 **Bảo mật hệ thống:** Hệ thống cơ sở dữ liệu và tài khoản trên sàn ZoneMart được bảo vệ nghiêm ngặt bằng nhiều lớp mã hóa an toàn. Trợ lý AI tuyệt đối không cung cấp thông tin quản trị hoặc tiếp tay cho các hành vi can thiệp dữ liệu trái phép!");
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
            if (Regex.IsMatch(clean, $@"\b{Regex.Escape(kw)}\b"))
            {
                return (true, "NSFW", "⚠️ **Cảnh báo an toàn nội dung:** ZoneMart là nền tảng thương mại điện tử phục vụ cộng đồng văn minh, lành mạnh. Mình tuyệt đối từ chối phản hồi các câu hỏi hoặc nội dung 18+, đồi trụy. Bạn hãy giữ gìn sự lịch sự khi trò chuyện nhé! 😊");
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
            if (Regex.IsMatch(clean, $@"\b{Regex.Escape(kw)}\b"))
            {
                return (true, "VIOLENCE", "⚠️ **Cảnh báo an toàn:** ZoneMart AI tuân thủ nghiêm ngặt chuẩn mực đạo đức và pháp luật, nghiêm cấm phản hồi các nội dung liên quan đến bạo lực, tổn hại thân thể, vũ khí hoặc chất cấm. Bạn hãy chú ý an toàn cho bản thân và mọi người nhé!");
            }
        }

        // 4. TỪ NGỮ THÔ TỤC / KHIÊM NHÃ / XÚC PHẠM
        // NẾU CÂU HỎI THUỘC DANH SÁCH AN TOÀN (NHƯ "GỢI Ý CÁC MÓN", "THỊT LỢN", "TRỨNG CÚT", "LON NƯỚC", "BƯỞI", "BUỔI SÁNG") -> BỎ QUA KIỂM TRA THÔ TỤC
        if (!isExplicitlySafe)
        {
            // Regex bắt từ ngữ thô tục có dấu rõ ràng:
            var accentedVulgarRegex = new Regex(
                @"\b(cặc|buồi|lồn|địt|đụ|đéo|ăn cứt|chó đẻ|đĩ|con đĩ|thằng khốn|đồ khốn|mẹ mày|đồ chó|óc chó|cút mẹ mày|cút đi|bú cặc|ngu lồn|bú lồn|óc lợn)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (accentedVulgarRegex.IsMatch(lowerMsg))
            {
                return (true, "VULGAR", "⚠️ **Nhắc nhở văn minh:** ZoneMart là nền tảng thương mại điện tử phục vụ cộng đồng lịch sự và thân thiện. Mình xin phép không phản hồi các câu hỏi hoặc từ ngữ khiếm nhã ạ! Nếu bạn cần tìm kiếm món ngon, thực phẩm sạch VietGAP hay đồ uống giải nhiệt, mình luôn sẵn lòng hỗ trợ bạn nhé! 😊");
            }

            // Regex bắt từ ngữ thô tục không dấu (dùng cụm từ ghép chuẩn xác, TUYỆT ĐỐI KHÔNG match từ đơn mơ hồ như 'cac', 'buoi', 'lon', 'cut', 'deo', 'cc', 'cl'):
            var unaccentedVulgarRegex = new Regex(
                @"\b(con cac|cai cac|dau cac|nhu cac|bu cac|cak|" +
                @"con buoi|cai buoi|dau buoi|nhu buoi|bu0i|" +
                @"con lon|cai lon|ngu lon|oc lon|lon me|bu lon|l0n|" +
                @"an cut|cut di|cut me may|cut xeo|" +
                @"deo can|deo them|deo biet|deo me|deo ai|" +
                @"cho de|me may|dit me|du ma|du me|do cho|thang khon|con di|" +
                @"nhu cc|cai cc|nhu cl|cai cl|vcl|clmm|vcll|vkl|dmm|đmm|dcm|đcm|dm|đm)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (unaccentedVulgarRegex.IsMatch(clean))
            {
                return (true, "VULGAR", "⚠️ **Nhắc nhở văn minh:** ZoneMart là nền tảng thương mại điện tử phục vụ cộng đồng lịch sự và thân thiện. Mình xin phép không phản hồi các câu hỏi hoặc từ ngữ khiếm nhã ạ! Nếu bạn cần tìm kiếm món ngon, thực phẩm sạch VietGAP hay đồ uống giải nhiệt, mình luôn sẵn lòng hỗ trợ bạn nhé! 😊");
            }
        }

        return (false, "", "");
    }

    /// <summary>
    /// Kích hoạt quy trình chuyển giao sang con người (Human Handover Process)
    /// </summary>
    public async Task<HandoverAnalysisResult> ProcessHandoverAsync(
        string userMessage,
        string? userId = null,
        string? userEmail = null,
        string? userPhone = null,
        string? fullName = null,
        List<string>? recentContext = null)
    {
        var analysis = AnalyzeMessage(userMessage, userId, userEmail);
        if (!analysis.TriggerHandover) return analysis;

        string ticketCode = $"HD-{Random.Shared.Next(1000, 9999)}";
        analysis.HandoverTicketCode = ticketCode;

        try
        {
            // 1. Tạo phiếu SupportTicket trong MongoDB để nhân viên tiếp quản
            string contextSummary = recentContext != null && recentContext.Count > 0
                ? string.Join(" | ", recentContext.TakeLast(5))
                : userMessage;

            var ticket = new SupportTicket
            {
                TicketCode = ticketCode,
                FullName = fullName ?? "Khách Hàng ZoneMart",
                Email = userEmail ?? "cskh@zonemart.vn",
                Phone = userPhone ?? "",
                Topic = $"[Human Handover] {analysis.Category} ({string.Join(", ", analysis.MatchedKeywords)})",
                Message = $"[Nội dung kích hoạt]: {userMessage}\n[Ngữ cảnh gần nhất]: {contextSummary}\n[Lý do]: {analysis.Reason}",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _mongoService.SupportTickets.InsertOneAsync(ticket);

            // 2. Gửi thông báo khẩn cấp tới Ban Quản Trị
            var notif = new AdminNotification
            {
                Type = "system",
                Title = $"🚨 [Chuyển Giao Trợ Giúp] {ticketCode}",
                Message = $"Khách hàng '{ticket.FullName}' cần hỗ trợ con người: {userMessage}. Từ khóa: {string.Join(", ", analysis.MatchedKeywords)}",
                TargetUserId = userId ?? ticketCode,
                TargetName = ticket.FullName,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _mongoService.AdminNotifications.InsertOneAsync(notif);

            // 3. Bắn tin nhắn qua Telegram Bot để nhân viên nhận tin tức thì
            try
            {
                var telegramReq = new SupportTicketRequest
                {
                    FullName = ticket.FullName,
                    Email = ticket.Email,
                    Phone = ticket.Phone,
                    Topic = ticket.Topic,
                    Message = ticket.Message
                };
                await _telegramService.SendTicketNotificationAsync(telegramReq, ticketCode);
            }
            catch (Exception teleEx)
            {
                _logger.LogWarning($"[Humandover] Không thể gửi thông báo Telegram: {teleEx.Message}");
            }

            _logger.LogInformation($"✅ [Humandover] Đã kích hoạt Human Handover thành công cho ticket {ticketCode}!");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ [Humandover] Lỗi khi xử lý Human Handover: {ex.Message}");
        }

        return analysis;
    }
}
