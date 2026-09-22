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
        "mướp", "khoai", "sắn", "đậu", "lạc", "mè", "vừng", "tiêu", "ớt", "gừng", "tỏi", "hành", "sả"
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
        var visionTask = InspectImageWithGeminiAsync(imageBytes, mimeType, cancellationToken);
        var policyTask = JudgeContentWithKiraAsync(request.ProductName, request.CategoryName ?? string.Empty, cancellationToken);

        await Task.WhenAll(visionTask, policyTask);

        var visionResult = await visionTask;
        var policyResult = await policyTask;

        // 3. Đối soát chéo: So sánh DetectedItem (Gemini) với ProductName
        var (isCrossMatch, matchPercentage, crossMatchWarning) = CrossCheckItemConsistency(
            visionResult.DetectedItem, 
            request.ProductName, 
            request.CategoryName
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

        // Chỉ duyệt khi tất cả các tiêu chí đều đạt chuẩn
        bool isApproved = visionResult.IsImageSafe 
                       && policyResult.IsTextSafe 
                       && policyResult.IsMeaningful 
                       && isCrossMatch;

        return new ProductModerationResponse(
            IsApproved: isApproved,
            HasWarnings: warnings.Count > 0,
            Warnings: warnings,
            PolicyResult: policyResult,
            VisionResult: visionResult,
            IsCrossMatch: isCrossMatch,
            MatchPercentage: matchPercentage,
            CrossMatchWarning: crossMatchWarning
        );
    }

    #region Model 1: Gemini Vision Inspector

    private async Task<VisionInspectorResult> InspectImageWithGeminiAsync(
        byte[]? imageBytes, 
        string mimeType, 
        CancellationToken cancellationToken)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return new VisionInspectorResult(
                IsImageSafe: false,
                DetectedItem: "Chưa có ảnh",
                ImageWarning: "Sản phẩm bắt buộc phải có hình ảnh thực tế để Vision Inspector kiểm tra."
            );
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("GeminiClient");
            string apiKey = _geminiSettings.ApiKey?.Trim() ?? string.Empty;
            string model = string.IsNullOrWhiteSpace(_geminiSettings.Model) ? "gemini-3-flash-preview" : _geminiSettings.Model;
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            string base64Image = Convert.ToBase64String(imageBytes);

            var prompt = """
            Bạn là "Vision Inspector" của sàn thương mại điện tử chợ dân sinh ZoneMart.
            Nhiệm vụ của bạn là CHỈ KIỂM TRA THỊ GIÁC HÌNH ẢNH:

            1. Kiểm tra An toàn Thị giác (isImageSafe):
               - Ảnh có chứa:
                 * Vũ khí sát thương, súng ngắn, súng trường, đạn dược, bom, mìn, chất nổ, dao găm quân dụng.
                 * Ma túy, chất gây nghiện, cần sa, thuốc lá điện tử, bóng cười.
                 * Nội dung 18+, ảnh khỏa thân, khiêu dâm, đồi trụy, gợi dục, hở hang phản cảm, đồ chơi người lớn.
                 * Bạo lực, kinh dị, máu me, hành vi phạm pháp.
               - Nếu CÓ vi phạm bất kỳ nội dung nào trên:
                 * "isImageSafe": false
                 * "imageWarning": "Hình ảnh chứa nội dung cấm hoặc gợi cảm, nhạy cảm phản cảm."
               - Nếu KHÔNG vi phạm:
                 * "isImageSafe": true
                 * "imageWarning": ""

            2. Nhận diện vật thể chính trong ảnh (detectedItem):
               - Xác định chính xác vật thể, chủ thể hoặc món hàng xuất hiện rõ nhất trong ảnh.
               - Trả về tên tiếng Việt ngắn gọn, súc tích (ví dụ: "khẩu súng", "trái xoài", "bắp cải", "thịt lợn", "cá basa", "điện thoại", "khuôn mặt người", "người", "gói muối").

            ĐỊNH DẠNG JSON TRẢ VỀ BẮT BUỘC (DUY NHẤT 1 ĐỐI TƯỢNG JSON):
            {
              "isImageSafe": true,
              "detectedItem": "tên tiếng Việt vật thể",
              "imageWarning": ""
            }
            """;

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
                _logger.LogWarning("Gemini Vision Inspector lỗi HTTP {StatusCode}: {Body}", response.StatusCode, responseString);
                return new VisionInspectorResult(
                    IsImageSafe: true, 
                    DetectedItem: "Chưa xác định", 
                    ImageWarning: "Hệ thống Vision Inspector đang bận. Bài đăng được đưa vào hàng đợi kiểm duyệt."
                );
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
                                ImageWarning = "Hình ảnh chứa vũ khí hoặc súng đạn bị nghiêm cấm trên sàn." 
                            };
                        }
                        return parsed;
                    }
                }
            }

            return new VisionInspectorResult(false, "Không xác định", "Không thể phân tích nội dung hình ảnh.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Gemini Vision Inspector timeout.");
            return new VisionInspectorResult(true, "Timeout", "Quá thời gian kiểm tra hình ảnh. Chuyển sang chờ duyệt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Gemini Vision Inspector: {Message}", ex.Message);
            return new VisionInspectorResult(true, "Lỗi kiểm tra", "Dịch vụ Vision Inspector tạm thời gián đoạn.");
        }
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

    private PolicyJudgeResult FallbackPolicyEvaluation(string productName, string categoryName)
    {
        var trimmedName = productName.Trim();
        var normName = NormalizeVietnamese(trimmedName);

        // 1. Kiểm tra từ cấm và từ ngữ nhạy cảm / gợi cảm / thô tục 18+
        var match = ProhibitedWordsRegex.Match(trimmedName);
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

        // Kiểm tra đối chiếu không dấu
        foreach (var kw in ProhibitedKeywords)
        {
            var normKw = NormalizeVietnamese(kw);
            if (normName.Equals(normKw, StringComparison.OrdinalIgnoreCase) || 
                normName.StartsWith(normKw + " ") || 
                normName.EndsWith(" " + normKw) || 
                normName.Contains(" " + normKw + " "))
            {
                return new PolicyJudgeResult(
                    IsTextSafe: false,
                    IsMeaningful: true,
                    TextWarning: $"Tên sản phẩm chứa từ ngữ cấm hoặc nhạy cảm trên sàn: '{kw}'"
                );
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
        string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(detectedItem) || detectedItem == "Chưa xác định" || detectedItem == "Chưa có ảnh")
        {
            return (false, 30, "Chưa xác định được vật thể trong ảnh để đối chiếu.");
        }

        string normDetected = NormalizeVietnamese(detectedItem);
        string normProduct = NormalizeVietnamese(productName);

        // 1. Nếu ảnh phát hiện vũ khí/súng đạn -> Lập tức mâu thuẫn/cấm
        if (normDetected.Contains("sung") || normDetected.Contains("vu khi") || normDetected.Contains("dan") || normDetected.Contains("bom"))
        {
            return (false, 0, $"Hình ảnh nhận diện là vũ khí/súng đạn ('{detectedItem}'), bị cấm tuyệt đối trên sàn ZoneMart.");
        }

        // 2. Nếu phát hiện hình người/chân dung/khuôn mặt mà tên không phải dịch vụ liên quan
        if (normDetected.Contains("nguoi") || normDetected.Contains("chan dung") || normDetected.Contains("khuon mat") || normDetected.Contains("mat nguoi"))
        {
            return (false, 0, $"Hình ảnh nhận diện là hình ảnh người/chân dung ('{detectedItem}'), không phải hình ảnh sản phẩm hợp lệ.");
        }

        // 3. Nếu phát hiện vật dụng công nghệ/đồ không phải thực phẩm nhưng tên là thực phẩm
        string[] nonFoodItems = new[] { "dien thoai", "may tinh", "laptop", "xe may", "o to", "dong ho", "quan ao", "giay dep", "meme" };
        bool isDetectedNonFood = nonFoodItems.Any(item => normDetected.Contains(item));

        string[] foodCategories = new[] { "rau cu qua", "thit ca tuoi", "trai cay tuoi", "thuc pham bo duong", "mon an nong", "thuc pham", "rau", "thit", "ca" };
        bool isProductFood = foodCategories.Any(fc => normProduct.Contains(fc) || NormalizeVietnamese(categoryName ?? "").Contains(fc));

        if (isDetectedNonFood && isProductFood)
        {
            return (false, 10, $"Hình ảnh nhận diện là '{detectedItem}', mâu thuẫn hoàn toàn với sản phẩm thực phẩm '{productName}'.");
        }

        // 4. Kiểm tra chéo phân loại trái cây vs rau củ vs thịt cá
        bool isFruit = normDetected.Contains("xoai") || normDetected.Contains("tao") || normDetected.Contains("chuoi") || normDetected.Contains("cam") || normDetected.Contains("dua hau");
        bool isVeggie = normProduct.Contains("rau") || normProduct.Contains("cai") || normProduct.Contains("muong");
        if (isFruit && isVeggie)
        {
            return (false, 25, $"Hình ảnh nhận diện là trái cây ('{detectedItem}') nhưng tên sản phẩm lại là rau củ ('{productName}').");
        }

        // 5. Kiểm tra trùng khớp từ khóa trực tiếp (Overlap từ vựng)
        var detectedTokens = normDetected.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var productTokens = normProduct.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);

        bool hasKeywordMatch = detectedTokens.Any(dt => dt.Length > 1 && productTokens.Contains(dt));

        if (hasKeywordMatch || normProduct.Contains(normDetected) || normDetected.Contains(normProduct))
        {
            return (true, 95, null);
        }

        // NẾU KHÔNG CÓ BẤT KỲ TỪ KHÓA NÀO TRÙNG NHAU: BẮT BUỘC TRẢ VỀ FALSE (CẢNH BÁO LỆCH ẢNH VÀ TÊN)
        return (false, 20, $"Hình ảnh nhận diện là '{detectedItem}' không trùng khớp với tên sản phẩm được khai báo '{productName}'.");
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