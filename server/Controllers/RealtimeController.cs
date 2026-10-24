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
    public class RealtimeController : ControllerBase
    {
        private readonly OrderRealtimeManager _realtimeManager;

        public RealtimeController(OrderRealtimeManager realtimeManager)
        {
            _realtimeManager = realtimeManager;
        }

        // ========================================================
        // API REALTIME SSE: GET /api/realtime/events
        // ========================================================
        [HttpGet("/api/realtime/events")]
        public async Task StreamRealtimeEvents(CancellationToken cancellationToken)
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

            _realtimeManager.RegisterRealtimeSseClient(clientId, async (payload) =>
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
                        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(15000, cancellationToken));
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
                _realtimeManager.UnregisterRealtimeSseClient(clientId);
            }
        }

        // ========================================================
        // API REALTIME ORDERS: GET /api/realtime/orders
        // ========================================================
        [HttpGet("/api/realtime/orders")]
        public IActionResult GetRealtimeOrders()
        {
            var orders = _realtimeManager.GetAllRealtimeOrders();
            return Ok(new { success = true, orders });
        }

        // ========================================================
        // API REALTIME ORDERS: POST /api/realtime/orders
        // ========================================================
        [HttpPost("/api/realtime/orders")]
        public IActionResult CreateRealtimeOrder([FromBody] JsonElement order)
        {
            string orderId = "";
            if (order.TryGetProperty("id", out var idProp))
            {
                orderId = idProp.GetString() ?? "";
            }
            if (string.IsNullOrWhiteSpace(orderId) && order.TryGetProperty("orderCode", out var codeProp))
            {
                orderId = codeProp.GetString() ?? "";
            }
            if (string.IsNullOrWhiteSpace(orderId))
            {
                orderId = "ZM" + new Random().Next(100000, 999999);
            }

            _realtimeManager.AddOrUpdateOrder(orderId, order, isNew: true);
            return Ok(new { success = true, order });
        }

        // ========================================================
        // API REALTIME ORDERS: PUT /api/realtime/orders
        // ========================================================
        [HttpPut("/api/realtime/orders")]
        public IActionResult UpdateRealtimeOrder([FromBody] JsonElement body)
        {
            string orderId = "";
            string status = "";
            string statusText = "";
            JsonElement? shipperInfo = null;

            if (body.TryGetProperty("orderId", out var idProp)) orderId = idProp.GetString() ?? "";
            if (body.TryGetProperty("status", out var stProp)) status = stProp.GetString() ?? "";
            if (body.TryGetProperty("statusText", out var sttProp)) statusText = sttProp.GetString() ?? "";
            if (body.TryGetProperty("shipperInfo", out var shProp)) shipperInfo = shProp;

            if (!string.IsNullOrWhiteSpace(orderId))
            {
                _realtimeManager.UpdateOrderStatus(orderId, status, statusText, shipperInfo);
            }

            return Ok(new { success = true });
        }

        // ========================================================
        // API REALTIME ORDERS: POST /api/realtime/orders/clear
        // ========================================================
        [HttpPost("/api/realtime/orders/clear")]
        public IActionResult ClearRealtimeOrders()
        {
            _realtimeManager.ClearOrders();
            return Ok(new { success = true });
        }
    }
}
