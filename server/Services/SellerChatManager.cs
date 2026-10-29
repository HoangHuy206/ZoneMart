using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZoneMart.Server.Services
{
    public class ChatProductContext
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public string? Image { get; set; }
        public string? Unit { get; set; }
    }

    public class ChatMessageItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ConversationId { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerAvatar { get; set; }
        public string SenderRole { get; set; } = "buyer"; // "buyer" | "seller"
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public string ReceiverRole { get; set; } = "seller";
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public ChatProductContext? ProductContext { get; set; }
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public bool IsRead { get; set; } = false;
    }

    public class PresenceInfo
    {
        public string Id { get; set; } = string.Empty; // storeId or buyerId
        public string Role { get; set; } = string.Empty; // "seller" or "buyer"
        public string Name { get; set; } = string.Empty;
        public bool IsOnline { get; set; } = true;
        public long LastSeen { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public class ConversationSummary
    {
        public string ConversationId { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerAvatar { get; set; }
        public string? StoreAvatar { get; set; }
        public ChatMessageItem? LastMessage { get; set; }
        public int UnreadCount { get; set; } = 0;
        public bool IsPeerOnline { get; set; } = false;
        public long UpdatedAt { get; set; } = 0;
    }

    public class SellerChatManager
    {
        private readonly OrderRealtimeManager _orderRealtimeManager;
        private readonly ConcurrentDictionary<string, List<ChatMessageItem>> _conversations = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, PresenceInfo> _presences = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _chatStorageFilePath;
        private readonly object _fileLock = new();

        public SellerChatManager(OrderRealtimeManager orderRealtimeManager)
        {
            _orderRealtimeManager = orderRealtimeManager;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            _chatStorageFilePath = Path.Combine(projectRoot, ".seller_chat_data.json");

            LoadFromDisk();
            SeedWelcomingMessagesIfEmpty();
        }

        private void LoadFromDisk()
        {
            try
            {
                if (File.Exists(_chatStorageFilePath))
                {
                    var json = File.ReadAllText(_chatStorageFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, List<ChatMessageItem>>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            _conversations[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SellerChatManager] Load error: {ex.Message}");
            }
        }

        private void SaveToDisk()
        {
            Task.Run(() =>
            {
                lock (_fileLock)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(_conversations, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_chatStorageFilePath, json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SellerChatManager] Save error: {ex.Message}");
                    }
                }
            });
        }

        public static string BuildConversationId(string storeId, string buyerId)
        {
            var s = string.IsNullOrWhiteSpace(storeId) ? "store_default" : storeId.Trim();
            var b = string.IsNullOrWhiteSpace(buyerId) ? "guest" : buyerId.Trim();
            return $"{s}__{b}";
        }

        private void SeedWelcomingMessagesIfEmpty()
        {
            if (!_conversations.IsEmpty) return;

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Conversation 1: Tổng Kho Đồ Gia Dụng Mỹ Đình
            var convId1 = BuildConversationId("s_mydinh", "buyer_demo");
            var list1 = new List<ChatMessageItem>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = convId1,
                    StoreId = "s_mydinh",
                    StoreName = "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
                    BuyerId = "buyer_demo",
                    BuyerName = "Khách Hàng ZoneMart",
                    SenderRole = "seller",
                    SenderId = "s_mydinh",
                    SenderName = "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
                    ReceiverRole = "buyer",
                    ReceiverId = "buyer_demo",
                    ReceiverName = "Khách Hàng ZoneMart",
                    Content = "Chào bạn! Tổng Kho Đồ Gia Dụng Mỹ Đình hân hạnh phục vụ. Shop sẵn sàng tư vấn về Nước Rửa Bát Hữu Cơ hoặc giao hỏa tốc 20-30 phút tận tay bạn nhé!",
                    Timestamp = now - 3600000,
                    IsRead = true
                }
            };
            _conversations[convId1] = list1;

            // Also for default store ID "s1"
            var convId2 = BuildConversationId("s1", "buyer_demo");
            var list2 = new List<ChatMessageItem>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = convId2,
                    StoreId = "s1",
                    StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                    BuyerId = "buyer_demo",
                    BuyerName = "Khách Hàng ZoneMart",
                    SenderRole = "seller",
                    SenderId = "s1",
                    SenderName = "Vườn Rau Hữu Cơ Bác Ba",
                    ReceiverRole = "buyer",
                    ReceiverId = "buyer_demo",
                    ReceiverName = "Khách Hàng ZoneMart",
                    Content = "Dạ chào quý khách! Vườn rau Bác Ba vừa thu hoạch rau tươi sáng nay, chuẩn VietGAP an toàn tuyệt đối. Cần ship hỏa tốc cứ nhắn shop nhé!",
                    Timestamp = now - 7200000,
                    IsRead = true
                }
            };
            _conversations[convId2] = list2;

            SaveToDisk();
        }

        #region Presence Real-time Tracking

        public void UpdatePresence(string id, string role, string name, bool isOnline)
        {
            if (string.IsNullOrWhiteSpace(id)) return;

            var cleanId = id.Trim();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var info = new PresenceInfo
            {
                Id = cleanId,
                Role = role ?? "buyer",
                Name = name ?? "",
                IsOnline = isOnline,
                LastSeen = now
            };

            _presences[cleanId] = info;
            if (!string.IsNullOrWhiteSpace(name))
            {
                _presences[name.Trim()] = info;
            }

            // Phát tán realtime sự kiện thay đổi trạng thái online tới mọi client SSE
            var payload = JsonSerializer.Serialize(new
            {
                type = "CHAT_PRESENCE",
                payload = info
            });
            _orderRealtimeManager.BroadcastRealtimeMessage(payload);
        }

        public bool IsUserOnline(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            var clean = id.Trim();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_presences.TryGetValue(clean, out var p))
            {
                return p.IsOnline && (now - p.LastSeen) < 15000;
            }
            foreach (var item in _presences.Values)
            {
                if (string.Equals(item.Name?.Trim(), clean, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Id?.Trim(), clean, StringComparison.OrdinalIgnoreCase))
                {
                    return item.IsOnline && (now - item.LastSeen) < 15000;
                }
            }
            return false;
        }

        public Dictionary<string, PresenceInfo> GetAllPresences()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var result = new Dictionary<string, PresenceInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _presences)
            {
                var diff = now - kvp.Value.LastSeen;
                var active = kvp.Value.IsOnline && diff < 15000;
                result[kvp.Key] = new PresenceInfo
                {
                    Id = kvp.Value.Id,
                    Role = kvp.Value.Role,
                    Name = kvp.Value.Name,
                    IsOnline = active,
                    LastSeen = kvp.Value.LastSeen
                };
            }
            return result;
        }

        #endregion

        #region Chat Messaging

        public ChatMessageItem SendMessage(ChatMessageItem msg)
        {
            if (string.IsNullOrWhiteSpace(msg.Id)) msg.Id = Guid.NewGuid().ToString();
            if (msg.Timestamp <= 0) msg.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (string.IsNullOrWhiteSpace(msg.ConversationId))
            {
                msg.ConversationId = BuildConversationId(msg.StoreId, msg.BuyerId);
            }

            var list = _conversations.GetOrAdd(msg.ConversationId, _ => new List<ChatMessageItem>());
            lock (list)
            {
                list.Add(msg);
            }

            // Tự động đánh dấu người gửi là đang Online
            UpdatePresence(msg.SenderId, msg.SenderRole, msg.SenderName, true);

            SaveToDisk();

            // Phát tán realtime tin nhắn mới tới toàn hệ thống qua SSE
            var payload = JsonSerializer.Serialize(new
            {
                type = "CHAT_MESSAGE",
                payload = msg
            });
            _orderRealtimeManager.BroadcastRealtimeMessage(payload);

            return msg;
        }

        public List<ChatMessageItem> GetMessages(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) return new List<ChatMessageItem>();

            if (_conversations.TryGetValue(conversationId, out var list))
            {
                lock (list)
                {
                    return list.OrderBy(m => m.Timestamp).ToList();
                }
            }

            // Fallback: Tìm hội thoại theo storeId hoặc storeName và buyerId nếu id có dạng "store__buyer"
            var parts = conversationId.Split(new[] { "__" }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                var sId = parts[0];
                var bId = parts[1];
                foreach (var kvp in _conversations)
                {
                    var msgs = kvp.Value;
                    if (msgs != null && msgs.Count > 0)
                    {
                        var f = msgs.First();
                        if ((string.Equals(f.StoreId, sId, StringComparison.OrdinalIgnoreCase) || string.Equals(f.StoreName, sId, StringComparison.OrdinalIgnoreCase))
                            && string.Equals(f.BuyerId, bId, StringComparison.OrdinalIgnoreCase))
                        {
                            lock (msgs)
                            {
                                return msgs.OrderBy(m => m.Timestamp).ToList();
                            }
                        }
                    }
                }
            }

            return new List<ChatMessageItem>();
        }

        public List<ConversationSummary> GetConversations(string? storeId = null, string? buyerId = null)
        {
            var results = new List<ConversationSummary>();

            foreach (var kvp in _conversations)
            {
                var convId = kvp.Key;
                var messages = kvp.Value;
                if (messages == null || messages.Count == 0) continue;

                var firstMsg = messages.First();
                var lastMsg = messages.Last();

                var currentStoreId = firstMsg.StoreId;
                var currentBuyerId = firstMsg.BuyerId;

                // Bộ lọc: kiểm tra theo storeId HOẶC storeName
                if (!string.IsNullOrWhiteSpace(storeId))
                {
                    bool matchStore = string.Equals(currentStoreId, storeId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(firstMsg.StoreName?.Trim(), storeId.Trim(), StringComparison.OrdinalIgnoreCase);
                    if (!matchStore) continue;
                }
                if (!string.IsNullOrWhiteSpace(buyerId) && !string.Equals(currentBuyerId, buyerId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var unread = messages.Count(m => !m.IsRead && (string.IsNullOrWhiteSpace(storeId) ? m.ReceiverId == buyerId : (string.Equals(m.ReceiverId, storeId, StringComparison.OrdinalIgnoreCase) || string.Equals(m.ReceiverName, storeId, StringComparison.OrdinalIgnoreCase) || m.ReceiverRole == "seller")));
                var peerId = !string.IsNullOrWhiteSpace(storeId) ? currentBuyerId : currentStoreId;

                results.Add(new ConversationSummary
                {
                    ConversationId = convId,
                    StoreId = currentStoreId,
                    StoreName = firstMsg.StoreName,
                    BuyerId = currentBuyerId,
                    BuyerName = firstMsg.BuyerName,
                    BuyerAvatar = firstMsg.BuyerAvatar,
                    LastMessage = lastMsg,
                    UnreadCount = unread,
                    IsPeerOnline = IsUserOnline(peerId) || IsUserOnline(firstMsg.BuyerName),
                    UpdatedAt = lastMsg.Timestamp
                });
            }

            return results.OrderByDescending(c => c.UpdatedAt).ToList();
        }

        public void MarkConversationAsRead(string conversationId, string readerRole)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) return;

            if (_conversations.TryGetValue(conversationId, out var list))
            {
                lock (list)
                {
                    foreach (var msg in list)
                    {
                        if (!msg.IsRead && msg.ReceiverRole.Equals(readerRole, StringComparison.OrdinalIgnoreCase))
                        {
                            msg.IsRead = true;
                        }
                    }
                }

                SaveToDisk();

                var payload = JsonSerializer.Serialize(new
                {
                    type = "CHAT_READ",
                    payload = new { conversationId, readerRole }
                });
                _orderRealtimeManager.BroadcastRealtimeMessage(payload);
            }
        }

        public void BroadcastTyping(string conversationId, string role, string name, bool isTyping)
        {
            var payload = JsonSerializer.Serialize(new
            {
                type = "CHAT_TYPING",
                payload = new { conversationId, role, name, isTyping }
            });
            _orderRealtimeManager.BroadcastRealtimeMessage(payload);
        }

        #endregion
    }
}
