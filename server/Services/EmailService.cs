using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ZoneMart.Server.Models;
using ZoneMart.Server.Settings;

namespace ZoneMart.Server.Services;

public interface IEmailService
{
    Task<bool> SendTicketNotificationAsync(SupportTicketRequest request, string ticketCode);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendTicketNotificationAsync(SupportTicketRequest request, string ticketCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.AppPassword))
            {
                _logger.LogWarning("EmailService: Chưa cấu hình SenderEmail hoặc AppPassword trong appsettings.json.");
                return false;
            }

            var receiver = string.IsNullOrWhiteSpace(_settings.ReceiverEmail) ? _settings.SenderEmail : _settings.ReceiverEmail;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("ZoneMart Admin", receiver));
            message.Subject = $"[ZoneMart Hỗ Trợ] Yêu Cầu Mới #{ticketCode} - {request.FullName}";

            var vnTime = DateTime.UtcNow.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss");

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <div style=""font-family: Arial, sans-serif; max-width: 620px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; background: #ffffff;"">
                    <div style=""background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 24px; color: #ffffff; text-align: center;"">
                        <h1 style=""margin: 0; font-size: 22px; font-weight: 800; letter-spacing: 0.5px;"">🛒 ZONEMART CSKH 24/7</h1>
                        <p style=""margin: 6px 0 0 0; font-size: 14px; opacity: 0.9;"">Thông Báo Yêu Cầu Hỗ Trợ Khách Hàng Mới</p>
                    </div>

                    <div style=""padding: 24px;"">
                        <div style=""background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 8px; padding: 14px; margin-bottom: 20px; display: flex; justify-content: space-between; align-items: center;"">
                            <div>
                                <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 700;"">Mã Ticket:</span><br/>
                                <strong style=""font-size: 18px; color: #2563eb;"">#{ticketCode}</strong>
                            </div>
                            <div style=""text-align: right;"">
                                <span style=""font-size: 12px; color: #64748b;"">Thời gian gửi:</span><br/>
                                <strong style=""font-size: 13px; color: #334155;"">{vnTime}</strong>
                            </div>
                        </div>

                        <h3 style=""font-size: 16px; color: #0f172a; margin-top: 0; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;"">👤 Thông Tin Khách Hàng</h3>
                        <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 14px;"">
                            <tr>
                                <td style=""padding: 8px 0; color: #64748b; width: 140px;"">Họ và tên:</td>
                                <td style=""padding: 8px 0; font-weight: bold; color: #0f172a;"">{request.FullName}</td>
                            </tr>
                            <tr>
                                <td style=""padding: 8px 0; color: #64748b;"">Email liên hệ:</td>
                                <td style=""padding: 8px 0; font-weight: bold; color: #2563eb;""><a href=""mailto:{request.Email}"" style=""color: #2563eb; text-decoration: none;"">{request.Email}</a></td>
                            </tr>
                            <tr>
                                <td style=""padding: 8px 0; color: #64748b;"">Số điện thoại:</td>
                                <td style=""padding: 8px 0; font-weight: bold; color: #0f172a;""><a href=""tel:{request.Phone}"" style=""color: #0f172a; text-decoration: none;"">{request.Phone}</a></td>
                            </tr>
                            {(string.IsNullOrWhiteSpace(request.OrderCode) ? "" : $@"
                            <tr>
                                <td style=""padding: 8px 0; color: #64748b;"">Mã đơn hàng:</td>
                                <td style=""padding: 8px 0; font-weight: bold; color: #d97706;"">#{request.OrderCode}</td>
                            </tr>")}
                            <tr>
                                <td style=""padding: 8px 0; color: #64748b;"">Chủ đề phân loại:</td>
                                <td style=""padding: 8px 0; font-weight: bold; color: #16a34a;"">{request.Topic}</td>
                            </tr>
                            {(string.IsNullOrWhiteSpace(request.FileName) ? "" : $@"
                            <tr>
                                <td style=""padding: 8px 0; color: #64748b;"">Tệp đính kèm:</td>
                                <td style=""padding: 8px 0; color: #475569;"">📎 {request.FileName}</td>
                            </tr>")}
                        </table>

                        <h3 style=""font-size: 16px; color: #0f172a; margin-top: 0; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;"">💬 Nội Dung Chi Tiết</h3>
                        <div style=""background: #f1f5f9; border-left: 4px solid #2563eb; padding: 14px; border-radius: 0 8px 8px 0; font-size: 14px; line-height: 1.6; color: #1e293b; white-space: pre-wrap;"">{request.Message}</div>

                        <div style=""margin-top: 24px; padding-top: 16px; border-top: 1px dashed #cbd5e1; text-align: center; font-size: 12px; color: #94a3b8;"">
                            Đây là email tự động gửi từ Cổng Hỗ Trợ Khách Hàng ZoneMart. Vui lòng không trả lời trực tiếp email này.
                        </div>
                    </div>
                </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // Chấp nhận SSL/TLS
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);

            // Bỏ khoảng trắng trong App Password nếu có
            var cleanPassword = _settings.AppPassword.Replace(" ", "").Trim();
            await client.AuthenticateAsync(_settings.SenderEmail.Trim(), cleanPassword);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("EmailService: Đã gửi email thông báo thành công cho Ticket #{TicketCode} đến {Receiver}", ticketCode, receiver);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmailService: Gặp lỗi khi gửi email cho Ticket #{TicketCode}", ticketCode);
            return false;
        }
    }
}
