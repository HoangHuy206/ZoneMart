using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ZoneMart.Server.Models;
using ZoneMart.Server.Settings;

namespace ZoneMart.Server.Services;

public interface IProductModerationService
{
    Task<ProductModerationResponse> ModerateProductAsync(
        ProductModerationRequest request, 
        CancellationToken cancellationToken = default);
}

public class ProductModerationService : IProductModerationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiSettings _geminiSettings;
    private readonly KiraSettings _kiraSettings;
    private readonly ILogger<ProductModerationService> _logger;

    // Danh sách từ cấm nghiêm ngặt: Vũ khí, súng đạn, chất nổ, ma túy, 18+, gợi cảm, gợi dục, từ ngữ thô tục...
    private static readonly string[] ProhibitedKeywords = new[]
    {
        // Vũ khí, súng đạn, chất nổ
        "vũ khí", "vu khi", "súng", "sung", "súng lục", "súng trường", "súng hơi", "khẩu súng", "khau sung",
        "đạn", "dan", "bullet", "bom", "mìn", "lựu đạn", "chất nổ", "thuốc nổ", "pháo nổ", "dao găm", "mã tấu", "kiếm nhật",
        
        // Ma túy, chất gây nghiện
        "ma túy", "ma tuy", "cần sa", "can sa", "heroin", "thuốc lắc", "bóng cười", "cỏ mỹ", "hồng phiến",
        "hàng giả", "hàng nhái", "lừa đảo",
        
        // 18+, gợi cảm, gợi dục, từ ngữ thô tục, nhạy cảm
        "địt", "dit", "đụ", "du", "dume", "đụ má", "du ma", "lồn", "lon", "cặc", "cac", "buồi", "buoi",
        "chịch", "chich", "xoạc", "xoac", "dâm", "dam", "khiêu dâm", "khieu dam", "gợi cảm", "goi cam",
        "gợi dục", "goi duc", "khoả thân", "khoa than", "nude", "búp bê tình dục", "sextoy", "thuốc kích dục",
        "khe ngực", "khe nguc", "khe mông", "khe mong", "hở hang", "ho hang", "phản cảm", "phan cam",
        "sex", "porn", "vú", "bím", "gái gọi", "bán dâm"
    };

    private static readonly Regex ProhibitedWordsRegex = new(
        @"(^|\s|[,.\-_/!@#$%^&*])(" + string.Join("|", ProhibitedKeywords.Select(Regex.Escape)) + @")(?=\s|[,.\-_/!@#$%^&*]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(500)
    );

    // Danh sách từ ngữ spam, vô nghĩa thường gặp
    private static readonly HashSet<string> KnownSpamWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "he", "ha", "ho", "hi", "khe", "khee", "tets", "test", "demo", "spam", "asdf", "qwerty", 
        "xyz", "abc", "aaa", "bbb", "ccc", "ddd", "eee", "fff", "ggg", "zzz", "123", "$$$", "###"
    };

    // Danh mục từ thực phẩm ngắn hợp lệ tiếng Việt
    private static readonly HashSet<string> ValidShortFoodWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "cam", "táo", "nho", "mít", "ổi", "lê", "bơ", "gạo", "muối", "thịt", "cá", "tôm", "cua", 
        "rau", "ngô", "bắp", "nấm", "trứng", "sữa", "yến", "mực", "ốc", "hàu", "sò", "bí", "bầu", 
        "mướp", "khoai", "sắn", "đậu", "đỗ", "lạc", "mè", "vừng", "tiêu", "ớt", "gừng", "tỏi", "hành", "sả",
        "heo", "lợn", "lon", "bò", "gà", "vịt", "bưởi", "xoài", "chuối", "mận", "đào", "dưa", "cải", "ngót", 
        "dền", "sung", "nem", "chả", "giò"
    };

    // Regex phát hiện chuỗi ký tự rác/vô nghĩa/spam
    private static readonly Regex SpamNameRegex = new(
        @"^([a-zA-Z0-9])\1{2,}$|^[^a-zA-Z0-9\u00C0-\u1EF9]+$|^[a-zA-Z0-9]{1,2}$|^(test|asdf|qwerty|xyz|abc|demo|spam|he|ha|ho|hi|khe|khee|tets|aaa|bbb|ccc|ddd|\$\$\$|###|123)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public ProductModerationService(
        IHttpClientFactory httpClientFactory,
        IOptions<GeminiSettings> geminiSettings,
        IOptions<KiraSettings> kiraSettings,
        ILogger<ProductModerationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _geminiSettings = geminiSettings.Value;
        _kiraSettings = kiraSettings.Value;
        _logger = logger;
    }

    public async Task<ProductModerationResponse> ModerateProductAsync(
        ProductModerationRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Trích xuất dữ liệu hình ảnh
        var (imageBytes, mimeType) = await ExtractImageDataAsync(request, cancellationToken);

        // 2. Chạy SONG SONG (Parallel Pipeline) 2 model qua Task.WhenAll
        // Tối ưu Multimodal: Truyền trực tiếp ProductName và CategoryName vào Vision Inspector để đối soát ngữ cảnh chuẩn xác
        var visionTask = InspectImageWithGeminiAsync(imageBytes, mimeType, request.ProductName, request.CategoryName ?? string.Empty, request.ProductImage?.FileName ?? string.Empty, cancellationToken);
        var policyTask = JudgeContentWithKiraAsync(request.ProductName, request.CategoryName ?? string.Empty, cancellationToken);

        await Task.WhenAll(visionTask, policyTask);

        var visionResult = await visionTask;
        var policyResult = await policyTask;

        // 3. Đối soát chéo: So sánh DetectedItem (Gemini) với ProductName có kết hợp kết quả từ Vision
        var (isCrossMatch, matchPercentage, crossMatchWarning) = CrossCheckItemConsistency(
            visionResult.DetectedItem, 
            request.ProductName, 
            request.CategoryName,
            visionResult.IsCrossMatch,
            visionResult.MatchPercentage
        );

        // 4. Tổng hợp cảnh báo
        var warnings = new List<string>();

        if (!policyResult.IsTextSafe && !string.IsNullOrWhiteSpace(policyResult.TextWarning))
            warnings.Add(policyResult.TextWarning);
        else if (!policyResult.IsMeaningful && !string.IsNullOrWhiteSpace(policyResult.TextWarning))
            warnings.Add(policyResult.TextWarning);

        if (!visionResult.IsImageSafe && !string.IsNullOrWhiteSpace(visionResult.ImageWarning))
            warnings.Add(visionResult.ImageWarning);

        if (!isCrossMatch && !string.IsNullOrWhiteSpace(crossMatchWarning))
            warnings.Add(crossMatchWarning);

        // Phân loại 3 tầng chuẩn Hybrid Moderation:
        // 1. Vi phạm nghiêm trọng (Hàng cấm, vũ khí, ma túy, 18+ porn) -> Auto-Reject & xử phạt
        // 2. Nghi vấn / Lệch Tên - Ảnh -> Human Handover (Duyệt tay cho Admin/Manager)
        // 3. Chuẩn chỉ 100% -> Auto-Approved mở bán
        bool isSevereViolation = (!policyResult.IsTextSafe && (policyResult.TextWarning.Contains("vũ khí") || policyResult.TextWarning.Contains("súng") || policyResult.TextWarning.Contains("bom") || policyResult.TextWarning.Contains("ma túy") || policyResult.TextWarning.Contains("chất cấm") || policyResult.TextWarning.Contains("18+")))
                              || (!visionResult.IsImageSafe && (visionResult.ImageWarning.Contains("vũ khí") || visionResult.ImageWarning.Contains("súng") || visionResult.ImageWarning.Contains("bom") || visionResult.ImageWarning.Contains("18+")));

        bool isApproved = false;
        bool requiresManualReview = false;
        string moderationAction = "approved";

        if (isSevereViolation)
        {
            isApproved = false;
            requiresManualReview = false;
            moderationAction = "violation_rejected";
        }
        else if (!isCrossMatch || matchPercentage < 70 || !policyResult.IsMeaningful || !visionResult.IsImageSafe || !policyResult.IsTextSafe || warnings.Count > 0)
        {
            isApproved = false;
            requiresManualReview = true;
            moderationAction = "human_handover";
            if (!warnings.Any(w => w.Contains("duyệt tay")))
            {
                warnings.Add("⚠️ Phát hiện nghi vấn độ lệch Tên - Ảnh hoặc danh mục: Đã kích hoạt Human Handover chuyển sang Ban Quản Trị (Admin/Manager) duyệt tay.");
            }
        }
        else
        {
            isApproved = true;
            requiresManualReview = false;
            moderationAction = "approved";
        }

        return new ProductModerationResponse(
            IsApproved: isApproved,
            HasWarnings: warnings.Count > 0,
            Warnings: warnings,
            PolicyResult: policyResult,
            VisionResult: visionResult,
            IsCrossMatch: isCrossMatch,
            MatchPercentage: matchPercentage,
            CrossMatchWarning: crossMatchWarning,
            RequiresManualReview: requiresManualReview,
            ModerationAction: moderationAction
        );
    }

    #region Model 1: Gemini Vision Inspector (Multimodal Joint Verification)

    private async Task<VisionInspectorResult> InspectImageWithGeminiAsync(
        byte[]? imageBytes, 
        string mimeType, 
        string productName = "",
        string categoryName = "",
        string imageFileName = "",
        CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return new VisionInspectorResult(
                IsImageSafe: false,
                DetectedItem: "Chưa có ảnh",
                ImageWarning: "Sản phẩm bắt buộc phải có hình ảnh thực tế để Vision Inspector kiểm tra."
            );
        }

        string apiKey = GetEffectiveGeminiApiKey();
        string model = GetEffectiveGeminiModel();

        // Nếu chưa cấu hình API Key thật hoặc đang ở môi trường dev offline, kích hoạt bộ quy tắc thẩm định nội bộ
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("Gemini API key chưa được cấu hình. Kích hoạt bộ quy tắc thẩm định hình ảnh nội bộ (Smart Rule-based Fallback).");
            return EvaluateSmartLocalVision(imageBytes, mimeType, productName, categoryName, imageFileName);
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("GeminiClient");
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            string base64Image = Convert.ToBase64String(imageBytes);

            var prompt = $"""
            Bạn là chuyên gia thẩm định và kiểm duyệt sản phẩm tự động cho sàn thương mại điện tử chợ dân sinh ZoneMart (chuyên thực phẩm tươi sống, rau củ quả, nhu yếu phẩm).
            Thông tin sản phẩm đăng bán:
            - Tên sản phẩm khai báo: "{productName}"
            - Danh mục khai báo: "{categoryName}"

            Nhiệm vụ của bạn là kiểm tra ĐỒNG THỜI thị giác hình ảnh và độ tương thích với thông tin sản phẩm:

            1. An toàn Thị giác (isImageSafe):
               - Kiểm tra ảnh có chứa:
                 * Vũ khí sát thương, súng ngắn, súng trường, đạn dược, bom, mìn, chất nổ, dao găm quân dụng.
                 * Ma túy, chất gây nghiện, cần sa, thuốc lá điện tử, bóng cười.
                 * Nội dung 18+, ảnh khỏa thân, khiêu dâm, đồi trụy, gợi dục, hở hang phản cảm, đồ chơi người lớn.
                 * Bạo lực, kinh dị, máu me, hành vi phạm pháp.
               - Nếu CÓ vi phạm bất kỳ nội dung nào trên:
                 * "isImageSafe": false
                 * "imageWarning": "Hình ảnh chứa nội dung cấm hoặc nhạy cảm phản cảm."
               - Nếu AN TOÀN:
                 * "isImageSafe": true
                 * "imageWarning": ""

            2. Nhận diện vật thể chính trong ảnh (detectedItem):
               - Xác định chính xác vật thể, món hàng hoặc chủ thể xuất hiện rõ nhất trong ảnh.
               - Trả về tên tiếng Việt ngắn gọn, súc tích (ví dụ: "rau xà lách", "trái xoài", "thịt heo", "cá basa", "nước rửa bát", "khẩu súng",...).

            3. Đối soát ngữ nghĩa và độ tương thích (isCrossMatch):
               - Thẩm định xem: Hình ảnh này có hợp lý, phù hợp để làm ảnh minh họa cho sản phẩm "{productName}" (thuộc danh mục "{categoryName}") không?
               - NGUYÊN TẮC QUAN TRỌNG:
                 * Chấp nhận từ đồng nghĩa, món ăn, chế biến, hình thức đóng gói (ví dụ: ảnh rau xà lách, rau diếp, đĩa rau củ, luống xà lách tươi đều KHỚP với "rau xà lách" -> isCrossMatch: true).
                 * Chấp nhận các mặt hàng thực phẩm nông sản tương đương trong cùng danh mục.
                 * Chỉ đánh giá KHÔNG KHỚP (isCrossMatch: false) nếu hình ảnh hoàn toàn không liên quan, cọc cạch hoặc cố tình lừa đảo (ví dụ: tên "rau xà lách" nhưng ảnh chụp điện thoại iPhone, xe máy, giày dép, súng đạn, ảnh chân dung người, hoặc thịt cá tươi).

            4. Điểm tương thích matchPercentage (0 - 100):
               - 85 - 100: Ảnh khớp đúng hoặc minh họa rất tốt cho sản phẩm.
               - 60 - 84: Ảnh thuộc cùng danh mục nhưng là sản phẩm tương tự.
               - Dưới 50: Ảnh hoàn toàn không liên quan đến sản phẩm được khai báo.

            ĐỊNH DẠNG JSON TRẢ VỀ BẮT BUỘC (DUY NHẤT 1 ĐỐI TƯỢNG JSON):
            """ + "\r\n{\r\n  \"isImageSafe\": true,\r\n  \"detectedItem\": \"tên tiếng Việt vật thể\",\r\n  \"isCrossMatch\": true,\r\n  \"matchPercentage\": 95,\r\n  \"imageWarning\": \"\"\r\n}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody), 
                Encoding.UTF8, 
                "application/json"
            );

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
            requestMessage.Headers.Add("x-goog-api-key", apiKey);
            requestMessage.Content = jsonContent;

            var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini Vision Inspector lỗi HTTP {StatusCode}: {Body}. Chuyển sang Fallback nội bộ.", response.StatusCode, responseString);
                return EvaluateSmartLocalVision(imageBytes, mimeType, productName, categoryName, imageFileName);
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && 
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0)
            {
                var rawJson = parts[0].GetProperty("text").GetString();
                if (!string.IsNullOrWhiteSpace(rawJson))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var parsed = JsonSerializer.Deserialize<VisionInspectorResult>(rawJson.Trim(), options);
                    if (parsed != null)
                    {
                        var normItem = (parsed.DetectedItem ?? string.Empty).ToLowerInvariant();
                        if (normItem.Contains("súng") || normItem.Contains("gun") || normItem.Contains("vũ khí") || normItem.Contains("đạn"))
                        {
                            return parsed with 
                            { 
                                IsImageSafe = false, 
                                IsCrossMatch = false,
                                MatchPercentage = 0,
                                ImageWarning = "Hình ảnh chứa vũ khí hoặc súng đạn bị nghiêm cấm trên sàn." 
                            };
                        }
                        return parsed;
                    }
                }
            }

            return EvaluateSmartLocalVision(imageBytes, mimeType, productName, categoryName, imageFileName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Gemini Vision Inspector timeout. Sử dụng Fallback nội bộ an toàn.");
            return EvaluateSmartLocalVision(imageBytes, mimeType, productName, categoryName, imageFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Gemini Vision Inspector: {Message}. Sử dụng Fallback nội bộ.", ex.Message);
            return EvaluateSmartLocalVision(imageBytes, mimeType, productName, categoryName, imageFileName);
        }
    }

    private string GetEffectiveGeminiApiKey()
    {
        var cfgKey = _geminiSettings.ApiKey?.Trim() ?? string.Empty;
        if (HasValidApiKey(cfgKey)) return cfgKey;

        var envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")?.Trim();
        if (HasValidApiKey(envKey)) return envKey!;

        return string.Empty;
    }

    private string GetEffectiveGeminiModel()
    {
        var model = _geminiSettings.Model?.Trim();
        if (!string.IsNullOrWhiteSpace(model) && !model.Contains("preview", StringComparison.OrdinalIgnoreCase))
        {
            return model;
        }
        return "gemini-1.5-flash";
    }

    private static bool HasValidApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return false;
        var trimmed = apiKey.Trim();
        return !trimmed.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) && trimmed.Length >= 10;
    }

    private VisionInspectorResult EvaluateSmartLocalVision(
        byte[]? imageBytes, 
        string mimeType, 
        string productName, 
        string categoryName,
        string imageFileName = "")
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return new VisionInspectorResult(
                IsImageSafe: false,
                DetectedItem: "Chưa có ảnh",
                ImageWarning: "Sản phẩm bắt buộc phải có hình ảnh thực tế để kiểm duyệt."
            );
        }

        var preCheck = FallbackPolicyEvaluation(productName, categoryName);
        if (!preCheck.IsTextSafe)
        {
            return new VisionInspectorResult(
                IsImageSafe: false,
                DetectedItem: "Nội dung vi phạm",
                ImageWarning: preCheck.TextWarning,
                IsCrossMatch: false,
                MatchPercentage: 0
            );
        }

        if (!preCheck.IsMeaningful)
        {
            return new VisionInspectorResult(
                IsImageSafe: true,
                DetectedItem: "Nội dung không rõ ràng",
                ImageWarning: preCheck.TextWarning,
                IsCrossMatch: false,
                MatchPercentage: 25
            );
        }

        string normFile = NormalizeVietnamese(imageFileName);
        string normProduct = NormalizeVietnamese(productName);
        string normCat = NormalizeVietnamese(categoryName);
        string detectedItem = string.IsNullOrWhiteSpace(productName) ? "Sản phẩm thực tế" : productName;
        bool isCrossMatch = true;
        int matchPercentage = 92;
        string imageWarning = string.Empty;

        // Nhận diện đặc trưng danh mục hình ảnh từ tệp tin ảnh
        if (!string.IsNullOrWhiteSpace(normFile))
        {
            if (normFile.Contains("thit") || normFile.Contains("pork") || normFile.Contains("beef") || normFile.Contains("meat") || normFile.Contains("heo") || normFile.Contains("lon") || normFile.Contains("ca ") || normFile.Contains("fish"))
            {
                detectedItem = "Thịt cá tươi sống";
                if (IsVeggieRelated(normProduct) || normCat.Contains("rau cu"))
                {
                    isCrossMatch = false;
                    matchPercentage = 15;
                    imageWarning = $"Hình ảnh nhận diện là thịt/cá ('{detectedItem}') mâu thuẫn với tên sản phẩm rau củ ('{productName}'). Đã kích hoạt Human Handover chuyển Ban Quản Trị đối soát duyệt tay.";
                }
            }
            else if (normFile.Contains("rau") || normFile.Contains("lettuce") || normFile.Contains("cai") || normFile.Contains("veggie") || normFile.Contains("salad") || normFile.Contains("xa lach"))
            {
                detectedItem = "Rau củ quả tươi";
                if (IsMeatFishRelated(normProduct) || normCat.Contains("thit") || normCat.Contains("ca"))
                {
                    isCrossMatch = false;
                    matchPercentage = 15;
                    imageWarning = $"Hình ảnh nhận diện là rau củ ('{detectedItem}') mâu thuẫn với tên sản phẩm thịt/cá ('{productName}'). Đã kích hoạt Human Handover chuyển Ban Quản Trị đối soát duyệt tay.";
                }
            }
            else if (normFile.Contains("muoi") || normFile.Contains("salt") || normFile.Contains("gia vi") || normFile.Contains("duong") || normFile.Contains("sugar"))
            {
                detectedItem = "Gia vị / Muối ăn";
                if (IsVeggieRelated(normProduct) || IsMeatFishRelated(normProduct))
                {
                    isCrossMatch = false;
                    matchPercentage = 20;
                    imageWarning = $"Hình ảnh nhận diện là gia vị bao bì ('{detectedItem}') mâu thuẫn với tên sản phẩm tươi sống ('{productName}'). Đã chuyển Ban Quản Trị duyệt tay.";
                }
            }
            else if (normFile.Contains("sung") || normFile.Contains("dan") || normFile.Contains("gun") || normFile.Contains("weapon") || normFile.Contains("bom"))
            {
                if (!IsFoodException(productName, categoryName))
                {
                    return new VisionInspectorResult(
                        IsImageSafe: false,
                        DetectedItem: "Vũ khí / Súng đạn",
                        ImageWarning: "Hình ảnh nhận diện chứa vũ khí hoặc vật phẩm cấm trên sàn ZoneMart.",
                        IsCrossMatch: false,
                        MatchPercentage: 0
                    );
                }
            }
        }

        return new VisionInspectorResult(
            IsImageSafe: true,
            DetectedItem: detectedItem,
            ImageWarning: imageWarning,
            IsCrossMatch: isCrossMatch,
            MatchPercentage: matchPercentage
        );
    }

    #endregion

    #region Model 2: GPT-6 Astra qua Kira AI endpoint

    private async Task<PolicyJudgeResult> JudgeContentWithKiraAsync(
        string productName, 
        string categoryName, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return new PolicyJudgeResult(
                IsTextSafe: false,
                IsMeaningful: false,
                TextWarning: "Tên sản phẩm không được để trống."
            );
        }

        // Luôn chạy bộ lọc nhanh trước để phát hiện ngay từ cấm nghiêm trọng
        var preCheck = FallbackPolicyEvaluation(productName, categoryName);
        if (!preCheck.IsTextSafe || !preCheck.IsMeaningful)
        {
            return preCheck;
        }

        if (string.IsNullOrWhiteSpace(_kiraSettings.Endpoint) || string.IsNullOrWhiteSpace(_kiraSettings.ApiKey))
        {
            return preCheck;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("KiraClient");
            string endpoint = _kiraSettings.Endpoint;
            string model = string.IsNullOrWhiteSpace(_kiraSettings.Model) ? "gpt-6-astra" : _kiraSettings.Model;

            var systemPrompt = """
            Bạn là "Policy & Content Judge" cho sàn thương mại điện tử chợ dân sinh ZoneMart.
            Nhiệm vụ: Phân tích ngữ nghĩa văn bản của Tên sản phẩm và Danh mục:

            1. isTextSafe (bool):
               - Kiểm tra xem Tên sản phẩm có chứa từ cấm, vũ khí, súng đạn, chất gây nghiện, ma túy, từ lóng khiêu dâm, 18+, gợi cảm, gợi dục, lách luật không?
               - Nếu CÓ vi phạm: "isTextSafe": false, "textWarning": "Tên sản phẩm vi phạm chính sách cấm hoặc nhạy cảm: [nêu từ ngữ vi phạm]"
               - Nếu AN TOÀN: "isTextSafe": true

            2. isMeaningful (bool):
               - Kiểm tra xem Tên sản phẩm có phải là chuỗi ký tự rác, vô nghĩa, hoặc spam gõ linh tinh không?
                 (Ví dụ spam/vô nghĩa: "he", "khe", "khee", "tets", "aaa", "bbb", "$$$", "###", "test", "asdf", "qwerty", "123").
               - Tên hợp lệ phải là tên một món hàng hoặc sản phẩm cụ thể.
               - Nếu là rác/spam: "isMeaningful": false, "textWarning": "Tên sản phẩm vô nghĩa hoặc là chuỗi ký tự spam. Vui lòng nhập tên cụ thể."
               - Nếu hợp lệ: "isMeaningful": true

            ĐỊNH DẠNG JSON TRẢ VỀ BẮT BUỘC (DUY NHẤT 1 ĐỐI TƯỢNG JSON):
            {
              "isTextSafe": true,
              "isMeaningful": true,
              "textWarning": ""
            }
            """;

            var userContent = $"Tên sản phẩm: \"{productName}\"\nDanh mục: \"{categoryName}\"";

            var requestPayload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userContent }
                },
                temperature = 0.1,
                response_format = new { type = "json_object" }
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (!string.IsNullOrWhiteSpace(_kiraSettings.ApiKey))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _kiraSettings.ApiKey);
            }
            requestMessage.Content = new StringContent(
                JsonSerializer.Serialize(requestPayload), 
                Encoding.UTF8, 
                "application/json"
            );

            var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return preCheck;
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && 
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var contentJson))
            {
                var contentStr = contentJson.GetString();
                if (!string.IsNullOrWhiteSpace(contentStr))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<PolicyJudgeResult>(contentStr.Trim(), options);
                    if (result != null) return result;
                }
            }

            return preCheck;
        }
        catch
        {
            return preCheck;
        }
    }

    /// <summary>
    /// Nhận diện các từ ngữ nông sản / thực phẩm hợp lệ tiếng Việt nhưng dễ bị nhầm lẫn âm với từ thô tục khi bỏ dấu
    /// (Ví dụ: "lợn" -> "lon", "bưởi" -> "buoi", "cóc dầm" -> "dam", "quả sung / bông súng" -> "sung")
    /// </summary>
    private static bool IsFoodException(string productName, string? categoryName = null)
    {
        if (string.IsNullOrWhiteSpace(productName)) return false;
        var low = productName.ToLowerInvariant().Trim();
        var norm = NormalizeVietnamese(low);
        var catNorm = NormalizeVietnamese(categoryName ?? string.Empty);

        // 1. Thịt lợn / Thịt heo và các sản phẩm nông sản từ lợn
        if (low.Contains("lợn") || low.Contains("heo") ||
            norm.Contains("thit lon") || norm.Contains("suon lon") || norm.Contains("mo lon") || 
            norm.Contains("tai lon") || norm.Contains("gio lon") || norm.Contains("lon man") || 
            norm.Contains("lon rung") || norm.Contains("lon cap nach") || norm.Contains("tiet lon") ||
            norm.Contains("long lon") || norm.Contains("da lon") || norm.Contains("nac lon") ||
            (norm.Contains("lon") && (catNorm.Contains("thit") || catNorm.Contains("thuc pham"))))
        {
            return true;
        }

        // 2. Bưởi (Trái cây tươi)
        if (low.Contains("bưởi") || 
            (norm.Contains("buoi") && (norm.Contains("trai") || norm.Contains("qua") || norm.Contains("xanh") || 
             norm.Contains("nam roi") || norm.Contains("dien") || norm.Contains("tep") || catNorm.Contains("trai cay") || catNorm.Contains("hoa qua"))))
        {
            return true;
        }

        // 3. Quả sung, sung muối, bông súng, hoa súng
        if (norm.Contains("sung") && (norm.Contains("qua") || norm.Contains("trai") || norm.Contains("muoi") || 
            norm.Contains("nep") || norm.Contains("bong") || norm.Contains("hoa") || norm.Contains("rau") || 
            norm.Contains("canh") || norm.Contains("xao") || catNorm.Contains("rau cu") || catNorm.Contains("nong san")))
        {
            return true;
        }

        // 4. Món dầm chua ngọt (cóc dầm, xoài dầm, mận dầm, me dầm)
        if (low.Contains("dầm") || 
            (norm.Contains("dam") && (norm.Contains("coc") || norm.Contains("xoai") || norm.Contains("me") || 
             norm.Contains("man") || norm.Contains("chua") || norm.Contains("ngot") || norm.Contains("ot"))))
        {
            return true;
        }

        // 5. Đơn vị quy cách lon / hộp thực phẩm (lon sữa, lon nước, lon bia, lon ngô...)
        if (norm.Contains("lon") && (norm.Contains("sua") || norm.Contains("nuoc") || norm.Contains("bia") || 
            norm.Contains("pate") || norm.Contains("ca ") || norm.Contains("hop") || norm.Contains("ngo") || norm.Contains("dau")))
        {
            return true;
        }

        return false;
    }

    private PolicyJudgeResult FallbackPolicyEvaluation(string productName, string categoryName)
    {
        var trimmedName = productName.Trim();
        var normName = NormalizeVietnamese(trimmedName);
        bool isFood = IsFoodException(trimmedName, categoryName);

        // Khử ký tự lách luật (Obfuscation / Leetspeak như s.ú.n.g, b-o-m, c_ầ_n_s_a, s u n g)
        string normCleaned = Regex.Replace(normName, @"(?<=[a-z0-9])[\p{P}\p{S}]+(?=[a-z0-9])", "");
        string normCollapsed = Regex.Replace(normCleaned, @"\b([a-z0-9])\s+(?=[a-z0-9]\b)", "$1");

        // 1. Kiểm tra từ cấm và từ ngữ nhạy cảm / gợi cảm / thô tục 18+
        if (!isFood)
        {
            var match = ProhibitedWordsRegex.Match(trimmedName);
            if (!match.Success && !string.IsNullOrWhiteSpace(normCleaned))
            {
                match = ProhibitedWordsRegex.Match(normCleaned);
            }
            if (match.Success)
            {
                var detectedWord = match.Groups[2].Value;
                var warning = $"Tên sản phẩm chứa từ ngữ cấm hoặc nhạy cảm trên sàn: '{detectedWord}'";
                return new PolicyJudgeResult(
                    IsTextSafe: false,
                    IsMeaningful: true,
                    TextWarning: warning
                );
            }

            // Kiểm tra đối chiếu không dấu (bỏ qua các từ đơn âm nhạy cảm nếu là thực phẩm thông dụng)
            foreach (var kw in ProhibitedKeywords)
            {
                var normKw = NormalizeVietnamese(kw);
                if (normKw is "lon" or "buoi" or "sung" or "dam" or "du" or "cac") continue;

                if (normName.Equals(normKw, StringComparison.OrdinalIgnoreCase) || 
                    normName.StartsWith(normKw + " ") || 
                    normName.EndsWith(" " + normKw) || 
                    normName.Contains(" " + normKw + " ") ||
                    normCleaned.Equals(normKw, StringComparison.OrdinalIgnoreCase) ||
                    normCleaned.StartsWith(normKw + " ") ||
                    normCleaned.EndsWith(" " + normKw) ||
                    normCleaned.Contains(" " + normKw + " ") ||
                    normCollapsed.Equals(normKw, StringComparison.OrdinalIgnoreCase) ||
                    normCollapsed.Contains(normKw))
                {
                    return new PolicyJudgeResult(
                        IsTextSafe: false,
                        IsMeaningful: true,
                        TextWarning: $"Tên sản phẩm chứa từ ngữ cấm hoặc nhạy cảm trên sàn: '{kw}'"
                    );
                }
            }
        }
        else
        {
            // Đối với thực phẩm hợp lệ đã nhận diện, chỉ chặn nếu cố tình chứa vũ khí thực tế, ma túy hoặc từ thô tục rõ ràng
            string[] explicitProhibited = new[] { "súng", "sung luc", "đạn", "bom", "mìn", "ma túy", "cần sa", "heroin", "thuốc lắc", "bóng cười", "đồ chơi tình dục", "sextoy" };
            foreach (var ep in explicitProhibited)
            {
                var normEp = NormalizeVietnamese(ep);
                if (normName.Contains(normEp))
                {
                    return new PolicyJudgeResult(
                        IsTextSafe: false,
                        IsMeaningful: true,
                        TextWarning: $"Tên sản phẩm chứa chất cấm hoặc vũ khí nguy hiểm: '{ep}'"
                    );
                }
            }
        }

        // 2. Kiểm tra chuỗi rác/spam
        if (KnownSpamWords.Contains(trimmedName) || KnownSpamWords.Contains(normName) || SpamNameRegex.IsMatch(trimmedName))
        {
            var warning = $"Tên sản phẩm '{trimmedName}' quá ngắn hoặc là từ ngữ spam/vô nghĩa. Vui lòng đặt tên món hàng rõ ràng.";
            return new PolicyJudgeResult(
                IsTextSafe: true,
                IsMeaningful: false,
                TextWarning: warning
            );
        }

        // Nếu chỉ là 1 từ duy nhất có độ dài <= 4 ký tự mà không nằm trong danh sách thực phẩm hợp lệ
        if (!trimmedName.Contains(' ') && trimmedName.Length <= 4 && !ValidShortFoodWords.Contains(normName))
        {
            var warning = $"Tên sản phẩm '{trimmedName}' không phải tên hàng hóa cụ thể. Vui lòng ghi rõ tên sản phẩm.";
            return new PolicyJudgeResult(
                IsTextSafe: true,
                IsMeaningful: false,
                TextWarning: warning
            );
        }

        return new PolicyJudgeResult(
            IsTextSafe: true,
            IsMeaningful: true,
            TextWarning: string.Empty
        );
    }

    #endregion

    #region Cross-Modal Consistency Check

    private (bool IsCrossMatch, int MatchPercentage, string? CrossMatchWarning) CrossCheckItemConsistency(
        string detectedItem, 
        string productName, 
        string? categoryName,
        bool? modelCrossMatch = null,
        int? modelMatchPercentage = null)
    {
        if (string.IsNullOrWhiteSpace(detectedItem) || detectedItem == "Chưa có ảnh")
        {
            return (false, 0, "Chưa có hình ảnh để kiểm tra đối chiếu.");
        }

        string normDetected = NormalizeVietnamese(detectedItem);
        string normProduct = NormalizeVietnamese(productName);
        string normCat = NormalizeVietnamese(categoryName ?? string.Empty);

        // 1. Nếu ảnh phát hiện vũ khí/súng đạn -> Lập tức mâu thuẫn/cấm
        if (normDetected.Contains("sung") || normDetected.Contains("vu khi") || normDetected.Contains("dan") || normDetected.Contains("bom"))
        {
            return (false, 0, $"Hình ảnh nhận diện là vũ khí/súng đạn ('{detectedItem}'), bị cấm tuyệt đối trên sàn ZoneMart.");
        }

        // 2. Nếu phát hiện hình người/chân dung/khuôn mặt mà tên không phải dịch vụ liên quan
        if (normDetected.Contains("chan dung") || normDetected.Contains("khuon mat") || normDetected.Contains("mat nguoi") ||
            (normDetected.Contains("nguoi") && !normProduct.Contains("nguoi") && !normCat.Contains("dich vu")))
        {
            return (false, 0, $"Hình ảnh nhận diện là hình ảnh người/chân dung ('{detectedItem}'), không phải hình ảnh sản phẩm hợp lệ.");
        }

        // 3. Nếu phát hiện vật dụng công nghệ/đồ không phải thực phẩm nhưng tên là thực phẩm
        string[] nonFoodItems = new[] { "dien thoai", "may tinh", "laptop", "xe may", "o to", "dong ho", "quan ao", "giay dep", "meme" };
        bool isDetectedNonFood = nonFoodItems.Any(item => normDetected.Contains(item));

        string[] foodCategories = new[] { "rau cu qua", "thit ca tuoi", "trai cay tuoi", "thuc pham bo duong", "mon an nong", "thuc pham", "rau", "thit", "ca" };
        bool isProductFood = foodCategories.Any(fc => normProduct.Contains(fc) || normCat.Contains(fc));

        if (isDetectedNonFood && isProductFood)
        {
            return (false, 10, $"Hình ảnh nhận diện là '{detectedItem}', mâu thuẫn hoàn toàn với sản phẩm thực phẩm '{productName}'.");
        }

        // 4. Nếu AI Model (Gemini Multimodal) đã trực tiếp xác thực độ khớp theo ngữ cảnh
        if (modelCrossMatch.HasValue)
        {
            int pct = modelMatchPercentage ?? (modelCrossMatch.Value ? 95 : 20);
            if (modelCrossMatch.Value)
            {
                return (true, Math.Max(pct, 85), null);
            }
            else if (pct < 45)
            {
                return (false, pct, $"Hình ảnh nhận diện là '{detectedItem}' không trùng khớp với tên sản phẩm '{productName}'.");
            }
        }

        // 5. Đối soát ngữ nghĩa theo Taxonomy nông sản/thực phẩm Việt Nam
        bool detectedIsVeg = IsVeggieRelated(normDetected);
        bool productIsVeg = IsVeggieRelated(normProduct) || normCat.Contains("rau cu");
        if (detectedIsVeg && productIsVeg)
        {
            return (true, 92, null);
        }

        bool detectedIsMeat = IsMeatFishRelated(normDetected);
        bool productIsMeat = IsMeatFishRelated(normProduct) || normCat.Contains("thit") || normCat.Contains("ca") || normCat.Contains("hai san");
        if (detectedIsMeat && productIsMeat)
        {
            return (true, 92, null);
        }

        bool detectedIsFruit = IsFruitRelated(normDetected);
        bool productIsFruit = IsFruitRelated(normProduct) || normCat.Contains("trai cay") || normCat.Contains("hoa qua");
        if (detectedIsFruit && productIsFruit)
        {
            return (true, 92, null);
        }

        // 5.1. Bắt mâu thuẫn chéo giữa các phân loại thực phẩm đối lập (Ví dụ: tên là Rau nhưng ảnh là Thịt) -> Chuyển Admin duyệt tay
        if (detectedIsMeat && productIsVeg)
        {
            return (false, 15, $"Hình ảnh nhận diện là thịt/cá ('{detectedItem}') mâu thuẫn với tên sản phẩm rau củ ('{productName}'). Đã chuyển Ban Quản Trị đối soát duyệt tay.");
        }

        if (detectedIsVeg && productIsMeat)
        {
            return (false, 15, $"Hình ảnh nhận diện là rau củ ('{detectedItem}') mâu thuẫn với tên sản phẩm thịt/cá ('{productName}'). Đã chuyển Ban Quản Trị đối soát duyệt tay.");
        }

        if (detectedIsMeat && productIsFruit)
        {
            return (false, 15, $"Hình ảnh nhận diện là thịt/cá ('{detectedItem}') mâu thuẫn với tên sản phẩm trái cây ('{productName}'). Đã chuyển Ban Quản Trị đối soát duyệt tay.");
        }

        // 6. Kiểm tra trùng khớp từ khóa trực tiếp (Overlap từ vựng)
        var detectedTokens = normDetected.Split(new[] { ' ', ',', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var productTokens = normProduct.Split(new[] { ' ', ',', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

        bool hasKeywordMatch = detectedTokens.Any(dt => dt.Length > 1 && productTokens.Contains(dt));

        if (hasKeywordMatch || normProduct.Contains(normDetected) || normDetected.Contains(normProduct))
        {
            return (true, 95, null);
        }

        // 7. Nếu là sản phẩm thuộc danh mục hàng hóa chung hợp lệ và ảnh không vi phạm
        if (detectedItem == productName || normDetected == normProduct)
        {
            return (true, 95, null);
        }

        // NẾU KHÔNG CÓ BẤT KỲ TỪ KHÓA NÀO TRÙNG NHAU
        return (false, 20, $"Hình ảnh nhận diện là '{detectedItem}' không trùng khớp với tên sản phẩm được khai báo '{productName}'.");
    }

    private static bool IsVeggieRelated(string normText)
    {
        string[] keywords = new[]
        {
            "rau", "xa lach", "salad", "lettuce", "diep", "cai", "muong", "ngot", "mong toi",
            "bap cai", "sup lo", "ca chua", "dua leo", "dua chuot", "cu qua", "nam", "gia do",
            "hanh", "toi", "ot", "khoai", "bi", "bau", "muop", "ngo", "bap", "dau co", "nong san"
        };
        return keywords.Any(kw => normText.Contains(kw));
    }

    private static bool IsMeatFishRelated(string normText)
    {
        string[] keywords = new[]
        {
            "thit", "heo", "lon", "bo", "ga", "vit", "pork", "beef", "chicken", "ca",
            "basa", "hoi", "chep", "thu", "tom", "cua", "muc", "bach tuoc", "hai san",
            "seafood", "cha", "gio", "xuc xich", "trung", "suon"
        };
        return keywords.Any(kw => normText.Contains(kw));
    }

    private static bool IsFruitRelated(string normText)
    {
        string[] keywords = new[]
        {
            "trai cay", "hoa qua", "fruit", "xoai", "tao", "cam", "chuoi", "oi", "man",
            "nho", "dua hau", "buoi", "sau rieng", "bo", "thanh long", "chom chom", "nhan",
            "vai", "le", "quyt", "mit", "coc", "me"
        };
        return keywords.Any(kw => normText.Contains(kw));
    }

    private static string NormalizeVietnamese(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string normalized = text.ToLowerInvariant().Trim();
        string[] vietnameseSigns = new string[]
        {
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        };

        for (int i = 1; i < vietnameseSigns.Length; i++)
        {
            for (int j = 0; j < vietnameseSigns[i].Length; j++)
            {
                normalized = normalized.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
        }

        return normalized;
    }

    #endregion

    #region Helper trích xuất ảnh

    private static async Task<(byte[]? Data, string MimeType)> ExtractImageDataAsync(
        ProductModerationRequest request, 
        CancellationToken cancellationToken)
    {
        if (request.ProductImage != null && request.ProductImage.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            await request.ProductImage.CopyToAsync(memoryStream, cancellationToken);
            var mimeType = string.IsNullOrWhiteSpace(request.ProductImage.ContentType) 
                ? "image/jpeg" 
                : request.ProductImage.ContentType;
            return (memoryStream.ToArray(), mimeType);
        }

        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            var base64 = request.ImageBase64;
            var mimeType = "image/jpeg";

            if (base64.Contains(','))
            {
                var parts = base64.Split(',');
                var match = Regex.Match(parts[0], @"data:(?<mime>[\w/\-\.]+);base64");
                if (match.Success)
                {
                    mimeType = match.Groups["mime"].Value;
                }
                base64 = parts[1];
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                return (bytes, mimeType);
            }
            catch
            {
                return (null, mimeType);
            }
        }

        return (null, "image/jpeg");
    }

    #endregion
}
