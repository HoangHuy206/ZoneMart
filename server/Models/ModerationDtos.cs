using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace ZoneMart.Server.Models;

/// <summary>
/// 1. Kết quả từ Model Gemini ("Vision Inspector")
/// Kiểm tra thị giác: nội dung cấm và nhận diện vật thể
/// </summary>
public record VisionInspectorResult(
    [property: JsonPropertyName("isImageSafe")] bool IsImageSafe,
    [property: JsonPropertyName("detectedItem")] string DetectedItem,
    [property: JsonPropertyName("imageWarning")] string ImageWarning
)
{
    [JsonPropertyName("isSafe")]
    public bool IsSafe => IsImageSafe;

    [JsonPropertyName("warningMessage")]
    public string WarningMessage => ImageWarning;

    [JsonPropertyName("violationReason")]
    public string ViolationReason => ImageWarning;
}

/// <summary>
/// 2. Kết quả từ Model GPT-6 Astra qua Kira AI endpoint ("Policy & Content Judge")
/// Phân tích ngữ nghĩa văn bản: kiểm tra từ cấm, nhạy cảm 18+, và phát hiện rác/vô nghĩa/spam
/// </summary>
public record PolicyJudgeResult(
    [property: JsonPropertyName("isTextSafe")] bool IsTextSafe,
    [property: JsonPropertyName("isMeaningful")] bool IsMeaningful,
    [property: JsonPropertyName("textWarning")] string TextWarning
)
{
    [JsonPropertyName("isClean")]
    public bool IsClean => IsTextSafe && IsMeaningful;
}

/// <summary>
/// DTO Request nhận từ Frontend
/// </summary>
public class ProductModerationRequest
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    public string ProductName { get; set; } = string.Empty;

    public string? CategoryName { get; set; }

    public IFormFile? ProductImage { get; set; }

    public string? ImageBase64 { get; set; }
}

/// <summary>
/// Tổng hợp kết quả phản hồi cuối cùng sau khi chạy song song 2 Model qua Task.WhenAll
/// </summary>
public record ProductModerationResponse(
    bool IsApproved,
    bool HasWarnings,
    List<string> Warnings,
    PolicyJudgeResult PolicyResult,
    VisionInspectorResult VisionResult,
    bool IsCrossMatch,
    int MatchPercentage,
    string? CrossMatchWarning
)
{
    [JsonPropertyName("isMatch")]
    public bool IsMatch => IsCrossMatch && IsApproved;

    [JsonPropertyName("textResult")]
    public LegacyTextResult TextResult => new(
        IsClean: PolicyResult.IsTextSafe && PolicyResult.IsMeaningful,
        FlaggedWords: string.IsNullOrWhiteSpace(PolicyResult.TextWarning) 
            ? new List<string>() 
            : new List<string> { PolicyResult.TextWarning },
        WarningMessage: PolicyResult.TextWarning
    );
}

public record LegacyTextResult(
    [property: JsonPropertyName("isClean")] bool IsClean,
    [property: JsonPropertyName("flaggedWords")] List<string> FlaggedWords,
    [property: JsonPropertyName("warningMessage")] string? WarningMessage
);

public class ViolationEmailRequest
{
    public string SellerEmail { get; set; } = string.Empty;
    public string? StoreName { get; set; }
    public string? ProductName { get; set; }
    public string? Reason { get; set; }
    public int ViolationCount { get; set; } = 1;
    public string ActionType { get; set; } = "warn";
}

