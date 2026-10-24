using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers
{
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly OrderRealtimeManager _realtimeManager;

        public PaymentController(OrderRealtimeManager realtimeManager)
        {
            _realtimeManager = realtimeManager;
        }

        // ========================================================
        // 0. API ĐĂNG KÝ THÔNG TIN ĐƠN HÀNG (SẢN PHẨM, HÌNH ẢNH, GIÁ)
        // POST /api/payment/register-order
        // ========================================================
        [HttpPost("/api/payment/register-order")]
        public IActionResult RegisterOrder([FromBody] JsonElement body)
        {
            try
            {
                string code = "";
                long total = 0;
                string buyerName = "";
                JsonElement items = default;

                if (body.TryGetProperty("code", out var codeProp)) code = codeProp.GetString() ?? "";
                if (body.TryGetProperty("total", out var totProp))
                {
                    if (totProp.ValueKind == JsonValueKind.Number) total = totProp.GetInt64();
                    else if (long.TryParse(totProp.GetString(), out var parsedTot)) total = parsedTot;
                }
                if (body.TryGetProperty("buyerName", out var bnProp)) buyerName = bnProp.GetString() ?? "";
                JsonElement? itemsElem = null;
                if (body.TryGetProperty("items", out var itProp) && itProp.ValueKind != JsonValueKind.Undefined) itemsElem = itProp;

                _realtimeManager.RegisterOrder(code, total, buyerName, itemsElem);
                return Ok(new { ok = true, code });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }

        // ========================================================
        // 1. API SERVER-SENT EVENTS (SSE): GET /api/payment/events?code=ZMxxxxxx
        // ========================================================
        [HttpGet("/api/payment/events")]
        public async Task StreamPaymentEvents([FromQuery] string? code, CancellationToken cancellationToken)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache, no-transform");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            try
            {
                await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch
            {
                return;
            }

            var clientId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<bool>();

            _realtimeManager.RegisterPaymentSseClient(clientId, code ?? "", async (payload) =>
            {
                try
                {
                    await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                catch
                {
                    tcs.TrySetResult(true);
                }
            });

            try
            {
                using (cancellationToken.Register(() => tcs.TrySetResult(true)))
                {
                    while (!cancellationToken.IsCancellationRequested && !tcs.Task.IsCompleted)
                    {
                        await Task.WhenAny(tcs.Task, Task.Delay(15000, cancellationToken));
                        if (cancellationToken.IsCancellationRequested || tcs.Task.IsCompleted) break;

                        try
                        {
                            await Response.WriteAsync(": ping\n\n", cancellationToken);
                            await Response.Body.FlushAsync(cancellationToken);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                _realtimeManager.UnregisterPaymentSseClient(clientId);
            }
        }

        // ========================================================
        // 2. API CHECK TRẠNG THÁI: GET /api/payment/check?code=ZMxxxxxx&amount=...&bankAccount=...
        // ========================================================
        [HttpGet("/api/payment/check")]
        public IActionResult CheckPaymentStatus([FromQuery] string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Ok(new { paid = false, code = "" });
            }

            var result = _realtimeManager.CheckPayment(code);
            return Ok(new
            {
                paid = result.Paid,
                code = result.Code,
                amount = result.Amount,
                time = result.Time,
                bankAccount = result.BankAccount,
                matchedBy = result.Paid ? "ORDER_CODE" : ""
            });
        }

        // ========================================================
        // 3. API THÔNG TIN TUNNEL & WEBHOOK
        // GET /api/payment/tunnel-info
        // ========================================================
        [HttpGet("/api/payment/tunnel-info")]
        public IActionResult GetTunnelInfo()
        {
            return Ok(new
            {
                tunnelUrl = "https://monogram-dilute-tightly.ngrok-free.dev",
                tokenUrl = "https://monogram-dilute-tightly.ngrok-free.dev/api/token_generate",
                callbackUrl = "https://monogram-dilute-tightly.ngrok-free.dev/bank/api/transaction-sync",
                totalPaidRecorded = _realtimeManager.GetPaidOrdersCount(),
                recentTransactions = _realtimeManager.GetRecentTransactions(5)
            });
        }

        // ========================================================
        // 4. XỬ LÝ TOÀN BỘ POST REQUESTS TỪ WEBHOOK TIỀN VÀO CỦA NGÂN HÀNG
        // POST /api/payment/webhook, POST /bank/api/transaction-sync
        // ========================================================
        [HttpPost("/api/payment/webhook")]
        [HttpPost("/bank/api/transaction-sync")]
        [HttpPost("/api/payment/transaction-callback")]
        [HttpPost("/tingo-callback")]
        public IActionResult HandlePaymentWebhook([FromBody] JsonElement body)
        {
            try
            {
                long amount = 0;
                if (body.TryGetProperty("amount", out var a1)) amount = a1.GetInt64();
                else if (body.TryGetProperty("transferAmount", out var a2)) amount = a2.GetInt64();
                else if (body.TryGetProperty("data", out var d1) && d1.TryGetProperty("amount", out var a3)) amount = a3.GetInt64();

                string transType = "C";
                if (body.TryGetProperty("transType", out var t1)) transType = t1.GetString() ?? "C";
                else if (body.TryGetProperty("type", out var t2)) transType = t2.GetString() ?? "C";
                if (transType.Equals("D", StringComparison.OrdinalIgnoreCase) || transType.Equals("DEBIT", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { error = false, message = "Bỏ qua giao dịch tiền ra (Debit)" });
                }

                // Trích xuất mã đơn hàng ZMxxxxxx
                string fullRaw = body.GetRawText();
                string? matchedCode = OrderRealtimeManager.ExtractOrderCode(fullRaw);

                string bankAccount = "";
                if (body.TryGetProperty("bankaccount", out var b1)) bankAccount = b1.GetString() ?? "";
                else if (body.TryGetProperty("accountNo", out var b2)) bankAccount = b2.GetString() ?? "";

                if (!string.IsNullOrWhiteSpace(matchedCode))
                {
                    _realtimeManager.RecordPaidOrder(matchedCode, amount, bankAccount, body);
                    return Ok(new
                    {
                        error = false,
                        toastMessage = "Thanh toán thành công",
                        orderCode = matchedCode,
                        amount
                    });
                }
                else if (amount > 0)
                {
                    var genericCode = "TX_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _realtimeManager.RecordPaidOrder(genericCode, amount, bankAccount, body);
                    return Ok(new
                    {
                        error = false,
                        toastMessage = "Đã nhận thanh toán",
                        orderCode = genericCode,
                        amount
                    });
                }

                return Ok(new { error = false, message = "Webhook received" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = true, errorReason = ex.Message });
            }
        }
    }
}
