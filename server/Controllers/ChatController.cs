using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly SellerChatManager _chatManager;

        public ChatController(SellerChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public class PresenceRequest
        {
            public string Id { get; set; } = string.Empty;
            public string Role { get; set; } = "buyer";
            public string Name { get; set; } = string.Empty;
            public bool IsOnline { get; set; } = true;
        }

        public class ReadRequest
        {
            public string ConversationId { get; set; } = string.Empty;
            public string ReaderRole { get; set; } = "seller";
        }

        public class TypingRequest
        {
            public string ConversationId { get; set; } = string.Empty;
            public string Role { get; set; } = "buyer";
            public string Name { get; set; } = string.Empty;
            public bool IsTyping { get; set; } = false;
        }

        [HttpGet("messages")]
        public IActionResult GetMessages([FromQuery] string? conversationId, [FromQuery] string? storeId, [FromQuery] string? buyerId)
        {
            var targetConvId = conversationId;
            if (string.IsNullOrWhiteSpace(targetConvId) && !string.IsNullOrWhiteSpace(storeId) && !string.IsNullOrWhiteSpace(buyerId))
            {
                targetConvId = SellerChatManager.BuildConversationId(storeId, buyerId);
            }

            if (string.IsNullOrWhiteSpace(targetConvId))
            {
                return BadRequest(new { success = false, message = "Cần truyền conversationId hoặc storeId + buyerId." });
            }

            var messages = _chatManager.GetMessages(targetConvId);
            return Ok(new { success = true, conversationId = targetConvId, messages });
        }

        [HttpPost("messages")]
        public IActionResult SendMessage([FromBody] ChatMessageItem msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Content) && msg.ProductContext == null)
            {
                return BadRequest(new { success = false, message = "Nội dung tin nhắn không được để trống." });
            }

            var saved = _chatManager.SendMessage(msg);
            return Ok(new { success = true, message = saved });
        }

        [HttpGet("conversations")]
        public IActionResult GetConversations([FromQuery] string? storeId, [FromQuery] string? buyerId)
        {
            var list = _chatManager.GetConversations(storeId, buyerId);
            return Ok(new { success = true, conversations = list });
        }

        [HttpPost("presence")]
        public IActionResult UpdatePresence([FromBody] PresenceRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Id))
            {
                return BadRequest(new { success = false, message = "Id người dùng / gian hàng không hợp lệ." });
            }

            _chatManager.UpdatePresence(req.Id, req.Role, req.Name, req.IsOnline);
            return Ok(new { success = true, id = req.Id, isOnline = req.IsOnline });
        }

        [HttpGet("presence")]
        public IActionResult GetAllPresences()
        {
            var presences = _chatManager.GetAllPresences();
            return Ok(new { success = true, presences });
        }

        [HttpPost("read")]
        public IActionResult MarkAsRead([FromBody] ReadRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConversationId))
            {
                return BadRequest(new { success = false, message = "ConversationId không hợp lệ." });
            }

            _chatManager.MarkConversationAsRead(req.ConversationId, req.ReaderRole);
            return Ok(new { success = true });
        }

        [HttpPost("typing")]
        public IActionResult BroadcastTyping([FromBody] TypingRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConversationId))
            {
                return BadRequest(new { success = false, message = "ConversationId không hợp lệ." });
            }

            _chatManager.BroadcastTyping(req.ConversationId, req.Role, req.Name, req.IsTyping);
            return Ok(new { success = true });
        }
    }
}
