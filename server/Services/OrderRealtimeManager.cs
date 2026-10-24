using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ZoneMart.Server.Services
{
    public class OrderRealtimeManager
    {
        private readonly ConcurrentDictionary<string, JsonElement> _realtimeOrders = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, JsonElement> _registeredOrders = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, JsonElement> _paidOrders = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Func<string, Task>> _realtimeSseClients = new();
        private readonly ConcurrentDictionary<string, (string OrderCode, Func<string, Task> Callback)> _paymentSseClients = new();

        private readonly string _realtimeOrdersFilePath;
        private readonly string _registeredOrdersFilePath;
        private readonly string _paidOrdersFilePath;
        private readonly object _fileLock = new();

        public OrderRealtimeManager()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            
            _realtimeOrdersFilePath = Path.Combine(projectRoot, ".pending_realtime_orders.json");
            _registeredOrdersFilePath = Path.Combine(projectRoot, ".pending_orders.json");
            _paidOrdersFilePath = Path.Combine(projectRoot, ".paid_orders.json");

            LoadFromDisk();
        }

        private void LoadFromDisk()
        {
            try
            {
                if (File.Exists(_realtimeOrdersFilePath))
                {
                    var json = File.ReadAllText(_realtimeOrdersFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict) _realtimeOrders[kvp.Key] = kvp.Value.Clone();
                    }
                }
            }
            catch {}

            try
            {
                if (File.Exists(_registeredOrdersFilePath))
                {
                    var json = File.ReadAllText(_registeredOrdersFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict) _registeredOrders[kvp.Key] = kvp.Value.Clone();
                    }
                }
            }
            catch {}

            try
            {
                if (File.Exists(_paidOrdersFilePath))
                {
                    var json = File.ReadAllText(_paidOrdersFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict) _paidOrders[kvp.Key] = kvp.Value.Clone();
                    }
                }
            }
            catch {}
        }

        private void SaveRealtimeOrdersToDisk()
        {
            Task.Run(() =>
            {
                lock (_fileLock)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(_realtimeOrders, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_realtimeOrdersFilePath, json);
                    }
                    catch {}
                }
            });
        }

        private void SaveRegisteredOrdersToDisk()
        {
            Task.Run(() =>
            {
                lock (_fileLock)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(_registeredOrders, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_registeredOrdersFilePath, json);
                    }
                    catch {}
                }
            });
        }

        private void SavePaidOrdersToDisk()
        {
            Task.Run(() =>
            {
                lock (_fileLock)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(_paidOrders, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_paidOrdersFilePath, json);
                    }
                    catch {}
                }
            });
        }

        #region Realtime Orders API Methods

        public List<JsonElement> GetAllRealtimeOrders()
        {
            return _realtimeOrders.Values.Reverse().ToList();
        }

        public void AddOrUpdateOrder(string orderId, JsonElement orderObj, bool isNew)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return;
            
            _realtimeOrders[orderId] = orderObj.Clone();
            SaveRealtimeOrdersToDisk();

            var eventType = isNew ? "ORDER_CREATED" : "ORDER_STATUS_CHANGED";
            var payload = isNew 
                ? JsonSerializer.Serialize(new { type = eventType, payload = orderObj })
                : JsonSerializer.Serialize(new { type = eventType, payload = new { orderId, order = orderObj } });

            BroadcastRealtimeMessage(payload);
        }

        public void UpdateOrderStatus(string orderId, string status, string statusText, JsonElement? shipperInfo = null)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return;

            if (_realtimeOrders.TryGetValue(orderId, out var existing))
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(existing.GetRawText()) 
                               ?? new Dictionary<string, object>();
                    dict["status"] = status;
                    dict["statusText"] = statusText;
                    if (shipperInfo.HasValue)
                    {
                        dict["shipperInfo"] = shipperInfo.Value;
                    }
                    var updatedJson = JsonSerializer.Serialize(dict);
                    using var doc = JsonDocument.Parse(updatedJson);
                    _realtimeOrders[orderId] = doc.RootElement.Clone();
                    SaveRealtimeOrdersToDisk();

                    var payload = JsonSerializer.Serialize(new
                    {
                        type = "ORDER_STATUS_CHANGED",
                        payload = new
                        {
                            orderId,
                            status,
                            statusText,
                            order = doc.RootElement
                        }
                    });
                    BroadcastRealtimeMessage(payload);
                    return;
                }
                catch {}
            }

            var fallbackPayload = JsonSerializer.Serialize(new
            {
                type = "ORDER_STATUS_CHANGED",
                payload = new
                {
                    orderId,
                    status,
                    statusText
                }
            });
            BroadcastRealtimeMessage(fallbackPayload);
        }

        public void ClearOrders()
        {
            _realtimeOrders.Clear();
            SaveRealtimeOrdersToDisk();
            BroadcastRealtimeMessage(JsonSerializer.Serialize(new { type = "ORDERS_CLEARED" }));
        }

        #endregion

        #region Payment API Methods

        public void RegisterOrder(string code, long total, string buyerName, JsonElement? items)
        {
            var cleanCode = ExtractOrderCode(code) ?? code?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(cleanCode)) return;

            object orderObj = items.HasValue && items.Value.ValueKind != JsonValueKind.Undefined
                ? new { code = cleanCode, total, buyerName, items = items.Value, time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                : new { code = cleanCode, total, buyerName, items = Array.Empty<object>(), time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

            var orderData = JsonSerializer.SerializeToElement(orderObj);
            _registeredOrders[cleanCode] = orderData;
            SaveRegisteredOrdersToDisk();
        }

        public (bool Paid, string Code, long Amount, long Time, string BankAccount) CheckPayment(string rawCode)
        {
            var cleanCode = ExtractOrderCode(rawCode) ?? rawCode?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(cleanCode)) return (false, "", 0, 0, "");

            if (_paidOrders.TryGetValue(cleanCode, out var elem))
            {
                long amount = 0;
                long time = 0;
                string bankAccount = "";
                if (elem.TryGetProperty("amount", out var aProp)) amount = aProp.GetInt64();
                if (elem.TryGetProperty("time", out var tProp)) time = tProp.GetInt64();
                if (elem.TryGetProperty("bankAccount", out var bProp)) bankAccount = bProp.GetString() ?? "";
                return (true, cleanCode, amount, time, bankAccount);
            }

            foreach (var kvp in _paidOrders)
            {
                var k = kvp.Key.ToUpper();
                if (cleanCode.Length >= 6 && (k == cleanCode || k.Contains(cleanCode) || cleanCode.Contains(k)))
                {
                    long amount = 0;
                    long time = 0;
                    string bankAccount = "";
                    if (kvp.Value.TryGetProperty("amount", out var aProp)) amount = aProp.GetInt64();
                    if (kvp.Value.TryGetProperty("time", out var tProp)) time = tProp.GetInt64();
                    if (kvp.Value.TryGetProperty("bankAccount", out var bProp)) bankAccount = bProp.GetString() ?? "";
                    return (true, cleanCode, amount, time, bankAccount);
                }
            }

            return (false, cleanCode, 0, 0, "");
        }

        public void RecordPaidOrder(string code, long amount, string bankAccount, JsonElement raw)
        {
            var cleanCode = ExtractOrderCode(code) ?? code?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(cleanCode)) return;

            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var data = JsonSerializer.SerializeToElement(new
            {
                code = cleanCode,
                amount,
                time,
                bankAccount,
                raw
            });

            _paidOrders[cleanCode] = data;
            SavePaidOrdersToDisk();

            // Broadcast to Payment SSE
            var paymentPayload = JsonSerializer.Serialize(new
            {
                paid = true,
                code = cleanCode,
                amount,
                time,
                bankAccount
            });
            BroadcastPaymentMessage(cleanCode, paymentPayload);

            // Also check if there's a realtime order with this code to update its payment status
            if (_realtimeOrders.TryGetValue(cleanCode, out _))
            {
                UpdateOrderStatus(cleanCode, "confirmed", "Đã thanh toán - Chờ chuẩn bị hàng");
            }
        }

        public int GetPaidOrdersCount() => _paidOrders.Count;
        public IEnumerable<KeyValuePair<string, JsonElement>> GetRecentTransactions(int count = 5) 
            => _paidOrders.TakeLast(count);

        #endregion

        #region SSE Subscriptions

        public void RegisterRealtimeSseClient(string clientId, Func<string, Task> callback)
        {
            _realtimeSseClients[clientId] = callback;
        }

        public void UnregisterRealtimeSseClient(string clientId)
        {
            _realtimeSseClients.TryRemove(clientId, out _);
        }

        public void BroadcastRealtimeMessage(string message)
        {
            foreach (var kvp in _realtimeSseClients)
            {
                _ = kvp.Value(message);
            }
        }

        public void RegisterPaymentSseClient(string clientId, string orderCode, Func<string, Task> callback)
        {
            var clean = ExtractOrderCode(orderCode) ?? orderCode?.Trim().ToUpper() ?? "";
            _paymentSseClients[clientId] = (clean, callback);
        }

        public void UnregisterPaymentSseClient(string clientId)
        {
            _paymentSseClients.TryRemove(clientId, out _);
        }

        public void BroadcastPaymentMessage(string orderCode, string message)
        {
            var clean = ExtractOrderCode(orderCode) ?? orderCode?.Trim().ToUpper() ?? "";
            foreach (var kvp in _paymentSseClients)
            {
                var target = kvp.Value.OrderCode;
                if (string.IsNullOrEmpty(target) || 
                    string.Equals(target, clean, StringComparison.OrdinalIgnoreCase) ||
                    (clean.Length >= 6 && target.Contains(clean, StringComparison.OrdinalIgnoreCase)))
                {
                    _ = kvp.Value.Callback(message);
                }
            }
        }

        #endregion

        public static string? ExtractOrderCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // 1. ZM kèm 6 chữ số
            var match6 = Regex.Match(text, @"\bZM(\d{6})\b", RegexOptions.IgnoreCase);
            if (match6.Success) return $"ZM{match6.Groups[1].Value}".ToUpper();

            // 2. ZM có khoảng trắng hoặc dấu gạch: ZM 948999
            var matchSpace = Regex.Match(text, @"ZM[\s\-_]+(\d{6})", RegexOptions.IgnoreCase);
            if (matchSpace.Success) return $"ZM{matchSpace.Groups[1].Value}".ToUpper();

            // 3. Fallback ZM từ 4 đến 8 chữ số
            var matchLoose = Regex.Match(text, @"ZM(\d{4,8})", RegexOptions.IgnoreCase);
            if (matchLoose.Success) return $"ZM{matchLoose.Groups[1].Value}".ToUpper();

            return null;
        }
    }
}
