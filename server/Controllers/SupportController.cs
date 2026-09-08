using Microsoft.AspNetCore.Mvc;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ITelegramService _telegramService;
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        IEmailService emailService,
        ITelegramService telegramService,
        MongoDbService mongoDbService,
        ILogger<SupportController> logger)
    {
        _emailService = emailService;
        _telegramService = telegramService;
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    [HttpPost("ticket")]
    public async Task<IActionResult> CreateTicket([FromBody] SupportTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new SupportTicketResponse
            {
                Success = false,
                Message = "Vui lòng điền đầy đủ họ tên, email và nội dung yêu cầu."
            });
        }

        var random = new Random();
        var ticketCode = $"ZM-{random.Next(100000, 999999)}";

        _logger.LogInformation("Nhận yêu cầu hỗ trợ mới: {TicketCode} từ {FullName} ({Email})", ticketCode, request.FullName, request.Email);

        // Chạy đồng thời cả gửi Email và Telegram
        var emailTask = _emailService.SendTicketNotificationAsync(request, ticketCode);
        var telegramTask = _telegramService.SendTicketNotificationAsync(request, ticketCode);

        // Lưu vào MongoDB nếu khả dụng
        try
        {
            var doc = new SupportTicket
            {
                TicketCode = ticketCode,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                OrderCode = request.OrderCode,
                Topic = request.Topic,
                Message = request.Message,
                FileName = request.FileName,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            };
            await _mongoDbService.SupportTickets.InsertOneAsync(doc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Không thể ghi vào MongoDB (tiếp tục xử lý gửi thông báo): {Message}", ex.Message);
        }

        await Task.WhenAll(emailTask, telegramTask);

        var emailSent = await emailTask;
        var telegramSent = await telegramTask;

        return Ok(new SupportTicketResponse
        {
            Success = true,
            TicketCode = ticketCode,
            EmailSent = emailSent,
            TelegramSent = telegramSent,
            Message = "Yêu cầu hỗ trợ đã được tiếp nhận và gửi thông báo thành công!"
        });
    }
}
