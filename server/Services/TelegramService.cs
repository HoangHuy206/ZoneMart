using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ZoneMart.Server.Models;
using ZoneMart.Server.Settings;

namespace ZoneMart.Server.Services;

public interface ITelegramService
{
    Task<bool> SendTicketNotificationAsync(SupportTicketRequest request, string ticketCode);
}

public class TelegramService : ITelegramService
{
    private readonly TelegramSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramService> _logger;
    private static string? _cachedChatId;

    public TelegramService(IOptions<TelegramSettings> settings, HttpClient httpClient, ILogger<TelegramService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendTicketNotificationAsync(SupportTicketRequest request, string ticketCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.BotToken))
            {
                _logger.LogWarning("TelegramService: Chưa cấu hình BotToken.");
                return false;
            }

            var chatId = _settings.ChatId;
            if (string.IsNullOrWhiteSpace(chatId))
            {
                chatId = _cachedChatId;
            }

            // Nếu chưa có ChatId, tự động truy vấn getUpdates để lấy ChatId gần nhất
            if (string.IsNullOrWhiteSpace(chatId))
            {
                chatId = await TryGetLatestChatIdAsync();
                if (!string.IsNullOrWhiteSpace(chatId))
                {
                    _cachedChatId = chatId;
                }
            }

            if (string.IsNullOrWhiteSpace(chatId))
            {
                _logger.LogWarning("TelegramService: Chưa tìm thấy ChatId. Hãy gửi /start đến bot trước.");
                return false;
            }

            var vnTime = DateTime.UtcNow.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss");

            var text = $@"🔔 <b>[ZONEMART] CÓ YÊU CẦU HỖ TRỢ MỚI!</b>
━━━━━━━━━━━━━━━━━━━━
🏷 <b>Mã Ticket:</b> <code>#{ticketCode}</code>
⏰ <b>Thời gian:</b> {vnTime}

👤 <b>Khách hàng:</b> {EscapeHtml(request.FullName)}
📧 <b>Email:</b> <code>{EscapeHtml(request.Email)}</code>
📞 <b>Số điện thoại:</b> <code>{EscapeHtml(request.Phone)}</code>
{(string.IsNullOrWhiteSpace(request.OrderCode) ? "" : $"📦 <b>Mã đơn hàng:</b> <code>#{EscapeHtml(request.OrderCode)}</code>\n")}📂 <b>Chủ đề:</b> {EscapeHtml(request.Topic)}
{(string.IsNullOrWhiteSpace(request.FileName) ? "" : $"📎 <b>Tệp đính kèm:</b> {EscapeHtml(request.FileName)}\n")}
💬 <b>Nội dung yêu cầu:</b>
<blockquote>{EscapeHtml(request.Message)}</blockquote>
━━━━━━━━━━━━━━━━━━━━
<i>⚡ Hệ thống tự động đẩy thông báo từ ZoneMart Portal</i>";

            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = "HTML"
            };

            var url = $"https://api.telegram.org/bot{_settings.BotToken.Trim()}/sendMessage";
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("TelegramService: Đã gửi thông báo Telegram thành công cho Ticket #{TicketCode} đến chat_id {ChatId}", ticketCode, chatId);
                return true;
            }
            else
            {
                _logger.LogError("TelegramService: Lỗi từ Telegram API: {Response}", responseString);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TelegramService: Gặp ngoại lệ khi gửi thông báo Telegram");
            return false;
        }
    }

    private async Task<string?> TryGetLatestChatIdAsync()
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_settings.BotToken.Trim()}/getUpdates";
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonNode.Parse(response);
            var results = json?["result"]?.AsArray();

            if (results != null && results.Count > 0)
            {
                // Lấy bản ghi tin nhắn mới nhất
                for (int i = results.Count - 1; i >= 0; i--)
                {
                    var msg = results[i]?["message"] ?? results[i]?["channel_post"];
                    var id = msg?["chat"]?["id"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        return id;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("TelegramService: Không thể tự động lấy ChatId qua getUpdates: {Message}", ex.Message);
        }
        return null;
    }

    private static string EscapeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
