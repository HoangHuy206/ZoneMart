using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ZoneMart.Server.Settings;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OcrController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly FptAiSettings _fptAiSettings;
    private readonly ILogger<OcrController> _logger;

    public OcrController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<FptAiSettings> fptAiOptions,
        ILogger<OcrController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _fptAiSettings = fptAiOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Proxy OCR Endpoint nhận file ảnh tải lên từ client (multipart/form-data),
    /// gọi dịch vụ FPT.AI ID Recognition v2 để trích xuất thông tin CMND / CCCD.
    /// </summary>
    [HttpPost("scan-id-card")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ScanIdCard(IFormFile? file)
    {
        try
        {
            // 1. Kiểm tra file tải lên
            var targetFile = file ?? Request.Form.Files["image"] ?? Request.Form.Files.FirstOrDefault();
            if (targetFile == null || targetFile.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng tải lên file ảnh CMND/CCCD hợp lệ (multipart/form-data)!"
                });
            }

            // Kiểm tra định dạng ảnh cơ bản
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };
            var extension = Path.GetExtension(targetFile.FileName)?.ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(extension) && !validExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Định dạng file không được hỗ trợ. Vui lòng tải file ảnh JPG, JPEG, PNG hoặc WEBP."
                });
            }

            // Kiểm tra dung lượng (giới hạn 10MB)
            if (targetFile.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dung lượng ảnh vượt quá giới hạn cho phép (tối đa 10MB)!"
                });
            }

            // 2. Lấy API Key từ biến môi trường hoặc cấu hình (tuyệt đối không hardcode)
            string apiKey = ResolveFptAiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("YOUR_FPT_AI_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("FPT.AI API Key chưa được thiết lập trong biến môi trường hoặc appsettings.json.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Chưa cấu hình FPT.AI API Key trong biến môi trường (FPT_AI_API_KEY) hoặc file appsettings.json. Vui lòng kiểm tra lại cấu hình hệ thống!"
                });
            }

            // 3. Chuẩn bị request gọi sang FPT.AI ID Recognition (Endpoint chính xác: https://api.fpt.ai/vision/idr/vnm)
            string endpoint = _fptAiSettings.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Contains("id-recognition/v2"))
            {
                endpoint = "https://api.fpt.ai/vision/idr/vnm";
            }

            var httpClient = _httpClientFactory.CreateClient("FptAiClient");
            httpClient.Timeout = TimeSpan.FromSeconds(_fptAiSettings.TimeoutSeconds > 0 ? _fptAiSettings.TimeoutSeconds : 30);

            using var multipartContent = new MultipartFormDataContent();
            await using var fileStream = targetFile.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);

            string contentType = string.IsNullOrWhiteSpace(targetFile.ContentType) ? "image/jpeg" : targetFile.ContentType;
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            // FPT.AI yêu cầu field name là "image"
            multipartContent.Add(streamContent, "image", targetFile.FileName ?? "id_card.jpg");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.TryAddWithoutValidation("api-key", apiKey);
            httpRequest.Headers.TryAddWithoutValidation("api_key", apiKey);
            httpRequest.Content = multipartContent;

            _logger.LogInformation("Đang gửi yêu cầu nhận diện CCCD tới FPT.AI Endpoint: {Endpoint}", endpoint);

            HttpResponseMessage? fptResponse = null;
            try
            {
                fptResponse = await httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FPT.AI không phản hồi hoặc timeout. Tự động chuyển tiếp sang Secondary AI Engine để đảm bảo luồng thông suốt.");
                var secondaryFallback = await ParseWithSecondaryOcrAsync(targetFile);
                if (secondaryFallback != null)
                {
                    return Ok(new
                    {
                        success = true,
                        source = "Secondary AI Engine (FPT.AI Unreachable Failover)",
                        data = secondaryFallback
                    });
                }

                return StatusCode(502, new
                {
                    success = false,
                    message = "Hệ thống AI nhận diện đang bận hoặc gián đoạn kết nối. Vui lòng thử lại sau ít giây!"
                });
            }

            // Xử lý mã lỗi HTTP từ bên thứ ba
            string responseBody = await fptResponse.Content.ReadAsStringAsync();

            if (!fptResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("FPT.AI trả về HTTP status code {StatusCode}: {Body}. Kích hoạt Secondary AI Engine an toàn.", (int)fptResponse.StatusCode, responseBody);

                // Khi FPT.AI gặp bất kỳ lỗi HTTP nào (429 Rate Limit, 401 Unauthorized, 500, v.v.),
                // tự động chuyển sang Secondary OCR Engine nhận diện thời gian thực để người dùng không bao giờ bị nghẽn
                var secondaryResult = await ParseWithSecondaryOcrAsync(targetFile);
                if (secondaryResult != null)
                {
                    return Ok(new
                    {
                        success = true,
                        source = "Secondary AI Engine (Auto Failover)",
                        data = secondaryResult
                    });
                }

                return Ok(new
                {
                    success = false,
                    message = "Không thể nhận diện ký tự trên ảnh thẻ đã tải lên. Vui lòng kiểm tra lại ảnh chụp rõ nét, không bị lóa hoặc bóng tối."
                });
            }

            // 4. Phân tích kết quả JSON từ FPT.AI
            try
            {
                using var jsonDoc = JsonDocument.Parse(responseBody);
                var root = jsonDoc.RootElement;

                // Kiểm tra errorCode trong response
                int errorCode = 0;
                if (root.TryGetProperty("errorCode", out var codeEl) && codeEl.TryGetInt32(out var parsedCode))
                {
                    errorCode = parsedCode;
                }

                string errorMessage = "";
                if (root.TryGetProperty("errorMessage", out var msgEl))
                {
                    errorMessage = msgEl.GetString() ?? "";
                }

                if (errorCode != 0)
                {
                    string userFriendlyMsg = ResolveErrorCodeMessage(errorCode, errorMessage);
                    _logger.LogWarning("FPT.AI trả về errorCode {ErrorCode}: {Message}", errorCode, userFriendlyMsg);

                    return Ok(new
                    {
                        success = false,
                        errorCode = errorCode,
                        message = userFriendlyMsg
                    });
                }

                // Kiểm tra mảng data
                if (!root.TryGetProperty("data", out var dataEl) ||
                    dataEl.ValueKind != JsonValueKind.Array ||
                    dataEl.GetArrayLength() == 0)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Không tìm thấy dữ liệu thẻ CCCD/CMND trong bức ảnh đã tải lên. Vui lòng chụp rõ nét hơn!"
                    });
                }

                var cardData = dataEl[0];

                // 5. Trích xuất và chuẩn hóa các trường thông tin theo yêu cầu
                string rawName = GetJsonString(cardData, "name");
                string rawId = GetJsonString(cardData, "id");
                string rawDob = GetJsonString(cardData, "dob");
                string rawHome = GetJsonString(cardData, "home");
                string rawAddress = GetJsonString(cardData, "address");

                string normalizedName = NormalizeName(rawName);
                string normalizedId = NormalizeIdNumber(rawId);
                string normalizedDob = NormalizeDate(rawDob);
                string normalizedHome = NormalizeAddress(rawHome);
                string normalizedAddress = NormalizeAddress(rawAddress);

                // Kiểm tra nếu tất cả các trường cốt lõi đều trống
                if (string.IsNullOrEmpty(normalizedName) && string.IsNullOrEmpty(normalizedId) && string.IsNullOrEmpty(normalizedHome))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Hình ảnh không rõ hoặc không phải mặt trước của thẻ CCCD/CMND hợp lệ."
                    });
                }

                // 6. Trả về JSON chuẩn cho client
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        ho_va_ten = normalizedName,
                        name = normalizedName,
                        so_cccd = normalizedId,
                        id = normalizedId,
                        ngay_sinh = normalizedDob,
                        dob = normalizedDob,
                        nguyen_quan = normalizedHome,
                        home = normalizedHome,
                        dia_chi_thuong_tru = normalizedAddress,
                        address = normalizedAddress
                    }
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lỗi phân tích JSON từ response của FPT.AI: {Body}", responseBody);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi định dạng phản hồi từ hệ thống nhận diện FPT.AI."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi thực hiện OCR CCCD");
            return StatusCode(500, new
            {
                success = false,
                message = "Đã xảy ra lỗi trong quá trình quét CCCD: " + ex.Message
            });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Lấy API Key từ biến môi trường OS, appsettings.json, hoặc file .env
    /// </summary>
    private string ResolveFptAiApiKey()
    {
        // 1. Biến môi trường hệ thống
        string? envKey = Environment.GetEnvironmentVariable("FPT_AI_API_KEY")
                      ?? Environment.GetEnvironmentVariable("FPTAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey)) return envKey.Trim();

        // 2. Cấu hình appsettings.json
        if (!string.IsNullOrWhiteSpace(_fptAiSettings.ApiKey)) return _fptAiSettings.ApiKey.Trim();
        string? configKey = _configuration["FptAiSettings:ApiKey"] ?? _configuration["FPT_AI_API_KEY"];
        if (!string.IsNullOrWhiteSpace(configKey)) return configKey.Trim();

        // 3. Đọc từ các file .env trong project nếu có
        try
        {
            var searchPaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "../ZoneMart/.env"),
                Path.Combine(Directory.GetCurrentDirectory(), "../../ZoneMart/.env"),
                Path.Combine(Directory.GetCurrentDirectory(), "../.env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env")
            };

            foreach (var path in searchPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    var lines = System.IO.File.ReadAllLines(path);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;

                        var parts = trimmed.Split('=', 2);
                        var keyName = parts[0].Trim();
                        var val = parts[1].Trim().Trim('"', '\'');

                        if ((keyName == "FPT_AI_API_KEY" || keyName == "FPTAI_API_KEY") && !string.IsNullOrWhiteSpace(val))
                        {
                            return val;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Không thể đọc file .env để tìm FPT_AI_API_KEY");
        }

        return string.Empty;
    }

    private static string GetJsonString(JsonElement el, string propertyName)
    {
        if (el.TryGetProperty(propertyName, out var prop))
        {
            return prop.GetString() ?? "";
        }
        return "";
    }

    private static string CleanText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string val = input.Trim();
        if (val.Equals("N/A", StringComparison.OrdinalIgnoreCase)) return "";
        // Loại bỏ các khoảng trắng thừa liên tiếp
        return Regex.Replace(val, @"\s+", " ");
    }

    private static string NormalizeName(string? rawName)
    {
        var cleaned = CleanText(rawName);
        if (string.IsNullOrEmpty(cleaned)) return "";
        return cleaned.ToUpperInvariant();
    }

    private static string NormalizeIdNumber(string? rawId)
    {
        var cleaned = CleanText(rawId);
        if (string.IsNullOrEmpty(cleaned)) return "";
        // Giữ lại các chữ số liên tiếp
        return Regex.Replace(cleaned, @"[^\d]", "");
    }

    private static string NormalizeDate(string? rawDob)
    {
        return CleanText(rawDob);
    }

    private static string NormalizeAddress(string? rawAddr)
    {
        return CleanText(rawAddr);
    }

    private async Task<object?> ParseWithSecondaryOcrAsync(IFormFile targetFile)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(25);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("K88349887788957"), "apikey");
            form.Add(new StringContent("eng"), "language");
            form.Add(new StringContent("2"), "OCREngine");

            await using var stream = targetFile.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            string contentType = string.IsNullOrWhiteSpace(targetFile.ContentType) ? "image/jpeg" : targetFile.ContentType;
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", targetFile.FileName ?? "cccd.jpg");

            var response = await httpClient.PostAsync("https://api.ocr.space/parse/image", form);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Secondary OCR trả về status {StatusCode}", response.StatusCode);
                return null;
            }

            var respJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(respJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("ParsedResults", out var parsedArray) && parsedArray.GetArrayLength() > 0)
            {
                var first = parsedArray[0];
                string parsedText = first.TryGetProperty("ParsedText", out var pt) ? (pt.GetString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(parsedText)) return null;

                _logger.LogInformation("Secondary OCR text trích xuất: {Text}", parsedText);

                // Trích xuất số CCCD (12 chữ số hoặc 9 chữ số CMND)
                string extractedId = "";
                var idMatch = Regex.Match(parsedText, @"(?:\b|^)(?:S[oốó0]|No\.?)?[^\d\n\r]*(\d{9,12})(?:\b|$)", RegexOptions.IgnoreCase);
                if (idMatch.Success)
                {
                    extractedId = idMatch.Groups[1].Value.Trim();
                }

                // Trích xuất Họ và tên (thường nằm sau Họ và tên / Full name, viết hoa)
                string extractedName = "";
                var nameMatch = Regex.Match(parsedText, @"(?:Họ\s*(?:và|va)?\s*tên|Full\s*name)[^\n\r:]*[:\n\r\s]+([^\n\r]+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    extractedName = nameMatch.Groups[1].Value.Trim();
                }
                else
                {
                    // Tìm dòng toàn chữ hoa dài hơn 5 ký tự (đặc trưng tên trên CCCD)
                    var lines = parsedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var t = line.Trim();
                        if (t.Length >= 5 && t.Length <= 40 && Regex.IsMatch(t, @"^[A-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝĐẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼẾỀỂỄỆỈĨỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪỬỮỰỲỴỶỸ\s]+$"))
                        {
                            if (!t.Contains("CỘNG HÒA") && !t.Contains("VIỆT NAM") && !t.Contains("CĂN CƯỚC") && !t.Contains("CITIZEN") && !t.Contains("IDENTITY"))
                            {
                                extractedName = t;
                                break;
                            }
                        }
                    }
                }

                // Trích xuất ngày sinh (dd/MM/yyyy)
                string extractedDob = "";
                var dobMatch = Regex.Match(parsedText, @"(?:\b|^)(\d{2}/\d{2}/\d{4})(?:\b|$)", RegexOptions.IgnoreCase);
                if (dobMatch.Success)
                {
                    extractedDob = dobMatch.Groups[1].Value.Trim();
                }

                // Trích xuất Quê quán / Place of origin
                string extractedOrigin = "";
                var originMatch = Regex.Match(parsedText, @"(?:Quê\s*quán|Qué\s*quan|Place\s*of\s*origin?)[^\n\r:]*[:\n\r\s]+([^\n\r]+)", RegexOptions.IgnoreCase);
                if (originMatch.Success)
                {
                    extractedOrigin = originMatch.Groups[1].Value.Trim();
                }

                // Trích xuất Nơi thường trú / Place of residence
                string extractedAddress = "";
                var addrMatch = Regex.Match(parsedText, @"(?:Nơi\s*thường\s*trú|Noi\s*thu[oờ]ng\s*tr[uú]|Place\s*of\s*residence)[^\n\r:]*[:\n\r\s]+([^\n\r]+)", RegexOptions.IgnoreCase);
                if (addrMatch.Success)
                {
                    extractedAddress = addrMatch.Groups[1].Value.Trim();
                    // Loại bỏ các mẩu ngày hết hạn nếu dính vào (ví dụ 'Ch, rh dàn21/03/2029 Thái Hòa...')
                    extractedAddress = Regex.Replace(extractedAddress, @"^.*?(\d{2}/\d{2}/\d{4})", "").Trim();
                    extractedAddress = extractedAddress.Trim('*', '-', ' ', ',');
                }

                if (string.IsNullOrWhiteSpace(extractedOrigin) && !string.IsNullOrWhiteSpace(extractedAddress))
                {
                    extractedOrigin = extractedAddress;
                }

                if (!string.IsNullOrWhiteSpace(extractedId) || !string.IsNullOrWhiteSpace(extractedName))
                {
                    return new
                    {
                        ho_va_ten = NormalizeName(extractedName),
                        name = NormalizeName(extractedName),
                        so_cccd = NormalizeIdNumber(extractedId),
                        id = NormalizeIdNumber(extractedId),
                        ngay_sinh = NormalizeDate(extractedDob),
                        dob = NormalizeDate(extractedDob),
                        nguyen_quan = NormalizeAddress(extractedOrigin),
                        home = NormalizeAddress(extractedOrigin),
                        dia_chi_thuong_tru = NormalizeAddress(extractedAddress),
                        address = NormalizeAddress(extractedAddress)
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý Secondary OCR");
        }

        return null;
    }

    private static string ResolveErrorCodeMessage(int errorCode, string defaultMessage)
    {
        return errorCode switch
        {
            1 => "Thông tin tham số hoặc định dạng ảnh không hợp lệ (Invalid parameters/values).",
            2 => "Không thể cắt góc thẻ CCCD/CMND. Ảnh có thể bị mất góc hoặc biến dạng quá mức.",
            3 => "Không tìm thấy thẻ CMND/CCCD trong ảnh hoặc ảnh quá mờ, lóa sáng, thiếu sáng.",
            5 => "Yêu cầu nhận diện hết thời gian xử lý hoặc không đúng định dạng chuẩn.",
            _ => !string.IsNullOrWhiteSpace(defaultMessage)
                ? defaultMessage
                : $"Dịch vụ nhận diện FPT.AI thông báo lỗi mã {errorCode}."
        };
    }

    #endregion
}

