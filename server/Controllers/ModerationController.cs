using Microsoft.AspNetCore.Mvc;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModerationController : ControllerBase
{
    private readonly IProductModerationService _moderationService;
    private readonly ILogger<ModerationController> _logger;

    public ModerationController(
        IProductModerationService moderationService,
        ILogger<ModerationController> logger)
    {
        _moderationService = moderationService;
        _logger = logger;
    }

    /// <summary>
    /// API Kiểm duyệt thời gian thực Tên và Hình ảnh sản phẩm trước khi đăng bài
    /// </summary>
    [HttpPost("check-product")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckProduct(
        [FromForm] ProductModerationRequest request, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Kiểm duyệt sản phẩm: {ProductName}", request.ProductName);

        var result = await _moderationService.ModerateProductAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// API Gửi email cảnh báo vi phạm bài đăng tự động tới người bán qua Gmail SMTP
    /// </summary>
    [HttpPost("send-violation-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendViolationEmail([FromBody] ViolationEmailRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ" });

        var sellerEmail = string.IsNullOrWhiteSpace(request.SellerEmail) 
            ? "dovanbinh487@gmail.com" 
            : request.SellerEmail.Trim();

        var storeName = string.IsNullOrWhiteSpace(request.StoreName) ? "Gian hàng ZoneMart" : request.StoreName.Trim();
        var prodName = string.IsNullOrWhiteSpace(request.ProductName) ? "Sản phẩm vi phạm" : request.ProductName.Trim();
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Vi phạm từ ngữ cấm hoặc nhạy cảm trên sàn" : request.Reason.Trim();
        var count = Math.Max(1, request.ViolationCount);
        var actionType = (request.ActionType ?? "warn").ToLower();

        string subject;
        string badgeColor;
        string badgeText;
        string sanctionWarning;

        if (actionType == "ban" || count > 10)
        {
            subject = $"[ZONEMART] 🚨 QUYẾT ĐỊNH XÓA VĨNH VIỄN TÀI KHOẢN NGƯỜI BÁN (LẦN VI PHẠM {count})";
            badgeColor = "#991B1B";
            badgeText = $"XÓA VĨNH VIỄN TÀI KHOẢN (LẦN VI PHẠM {count})";
            sanctionWarning = "Do đã tái phạm vượt quá 10 lần, tài khoản gian hàng của quý đối tác đã bị XÓA VĨNH VIỄN khỏi toàn bộ hệ sinh thái ZoneMart theo quy chế kiểm duyệt Luồng 2.";
        }
        else if (actionType == "lock" || count > 5)
        {
            subject = $"[ZONEMART] ⚠️ THÔNG BÁO TẠM ĐÌNH CHỈ TÀI KHOẢN 10 NGÀY (LẦN VI PHẠM {count}/10)";
            badgeColor = "#DC2626";
            badgeText = $"TẠM KHÓA TÀI KHOẢN 10 NGÀY (LẦN VI PHẠM {count}/10)";
            sanctionWarning = "Tài khoản của quý đối tác đã bị tạm khóa chức năng bán hàng trong 10 ngày. Nếu tiếp tục vi phạm vượt ngưỡng 10 lần, tài khoản sẽ bị XÓA VĨNH VIỄN khỏi nền tảng.";
        }
        else
        {
            subject = $"[ZONEMART] ⚠️ CẢNH BÁO VI PHẠM CHÍNH SÁCH ĐĂNG BÀI (LẦN {count}/5)";
            badgeColor = "#D97706";
            badgeText = $"CẢNH BÁO VI PHẠM LẦN {count}/5";
            sanctionWarning = "Bài đăng chứa nội dung cấm đã bị hệ thống AI xóa bỏ ngay lập tức. Quý đối tác vui lòng kiểm tra lại quy chế đăng bài. Nếu tiếp tục tái phạm từ lần thứ 6 trở đi, tài khoản sẽ tự động bị KHÓA 10 NGÀY!";
        }

        var htmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>{subject}</title>
</head>
<body style=""margin:0;padding:0;background-color:#F8FAFC;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#1E293B;"">
  <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F8FAFC;padding:30px 15px;"">
    <tr>
      <td align=""center"">
        <table width=""600"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""background:#FFFFFF;border-radius:16px;overflow:hidden;box-shadow:0 10px 25px rgba(0,0,0,0.05);border:1px solid #E2E8F0;"">
          <!-- Header -->
          <tr>
            <td style=""background:linear-gradient(135deg, #1E293B 0%, #0F172A 100%);padding:28px 32px;text-align:center;"">
              <h1 style=""margin:0;font-size:24px;color:#FFFFFF;letter-spacing:1px;font-weight:800;"">
                Zone<span style=""color:#F97316;"">Mart</span>
              </h1>
              <p style=""margin:6px 0 0;font-size:13px;color:#94A3B8;text-transform:uppercase;letter-spacing:1.5px;"">
                Hệ Thống Kiểm Duyệt AI An Toàn Thực Phẩm &amp; Nội Dung
              </p>
            </td>
          </tr>

          <!-- Banner Type -->
          <tr>
            <td style=""padding:24px 32px 10px;"">
              <div style=""background:{badgeColor};color:#FFFFFF;border-radius:8px;padding:12px 16px;text-align:center;font-weight:700;font-size:15px;letter-spacing:0.5px;"">
                {badgeText}
              </div>
            </td>
          </tr>

          <!-- Content -->
          <tr>
            <td style=""padding:15px 32px 30px;"">
              <p style=""font-size:15px;line-height:1.6;color:#334155;"">
                Kính gửi <strong>{storeName}</strong> ({sellerEmail}),
              </p>
              <p style=""font-size:14px;line-height:1.6;color:#475569;"">
                Hệ thống <strong>ZoneMart AI Vision &amp; Policy Guard</strong> phát hiện bài đăng của gian hàng có dấu hiệu vi phạm chính sách nội dung trên sàn thương mại điện tử ZoneMart:
              </p>

              <!-- Chi tiết vi phạm -->
              <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""background:#F1F5F9;border-radius:12px;padding:16px 20px;margin:18px 0;border:1px solid #E2E8F0;"">
                <tr>
                  <td style=""padding:6px 0;font-size:13px;color:#64748B;width:150px;"">Tên sản phẩm:</td>
                  <td style=""padding:6px 0;font-size:14px;font-weight:700;color:#0F172A;"">{prodName}</td>
                </tr>
                <tr>
                  <td style=""padding:6px 0;font-size:13px;color:#64748B;"">Lý do phát hiện:</td>
                  <td style=""padding:6px 0;font-size:14px;font-weight:700;color:#DC2626;"">{reason}</td>
                </tr>
                <tr>
                  <td style=""padding:6px 0;font-size:13px;color:#64748B;"">Biện pháp xử lý:</td>
                  <td style=""padding:6px 0;font-size:13px;font-weight:600;color:#B91C1C;"">Đã xóa bài ngay lập tức &amp; ghi nhận vi phạm</td>
                </tr>
                <tr>
                  <td style=""padding:6px 0;font-size:13px;color:#64748B;"">Thời gian ghi nhận:</td>
                  <td style=""padding:6px 0;font-size:13px;color:#334155;"">{DateTime.Now:HH:mm:ss dd/MM/yyyy}</td>
                </tr>
              </table>

              <!-- Cảnh báo chế tài -->
              <div style=""background:#FEF2F2;border-left:4px solid #EF4444;border-radius:6px;padding:14px 16px;margin-bottom:22px;"">
                <p style=""margin:0;font-size:13px;line-height:1.6;color:#991B1B;font-weight:600;"">
                  ⚠️ {sanctionWarning}
                </p>
              </div>

              <p style=""font-size:13px;line-height:1.6;color:#64748B;margin:0;"">
                Nếu bạn cho rằng đây là sự nhầm lẫn của hệ thống AI, vui lòng liên hệ trực tiếp Bộ phận Kiểm Duyệt ZoneMart qua hotline hoặc gửi email khiếu nại để được hỗ trợ kiểm tra lại thủ công.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#F8FAFC;padding:20px 32px;border-top:1px solid #E2E8F0;text-align:center;"">
              <p style=""margin:0 0 6px;font-size:12px;color:#64748B;"">
                Tổng đài hỗ trợ đối tác ZoneMart: <strong>1900 6868</strong> • Email: <a href=""mailto:hh9393100@gmail.com"" style=""color:#F97316;text-decoration:none;font-weight:600;"">hh9393100@gmail.com</a>
              </p>
              <p style=""margin:0;font-size:11px;color:#94A3B8;"">
                &copy; {DateTime.Now.Year} ZoneMart Platform. Thông báo tự động từ hệ thống kiểm duyệt AI. Vui lòng không trả lời thư này.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        try
        {
            _logger.LogInformation("Đang gửi email cảnh báo vi phạm tới {SellerEmail} cho sản phẩm '{ProductName}'", sellerEmail, prodName);
            var sendResult = await ForgotPasswordController.SendEmailViaMailKitAsync(sellerEmail, subject, htmlBody, "ZoneMart AI Safety Guard");
            
            return Ok(new
            {
                success = sendResult,
                message = sendResult ? $"Đã gửi email cảnh báo vi phạm tới {sellerEmail}" : "Gửi email thất bại",
                sellerEmail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gửi email cảnh báo vi phạm tới {SellerEmail}", sellerEmail);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

