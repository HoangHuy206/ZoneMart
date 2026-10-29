<script setup lang="ts">
import { ref, nextTick, watch, onMounted } from 'vue';
import { useSellerChat } from '../../composables/useSellerChat';
import { getCleanAvatar } from '../../utils/avatarHelper';

const {
  isChatModalOpen,
  isMinimized,
  activeStore,
  activeProduct,
  currentMessages,
  isStoreOnline,
  storePresenceLabel,
  isSellerTyping,
  closeChat,
  toggleMinimize,
  sendMessage,
  sendProductCard,
  sendTyping
} = useSellerChat();

const inputText = ref('');
const messagesContainerRef = ref<HTMLDivElement | null>(null);
let typingTimer: any = null;

const quickQuestions = [
  'Sản phẩm này còn hàng không shop?',
  'Giao hỏa tốc trong bao lâu?',
  'Có được đồng kiểm khi nhận không?',
  'Hạn sử dụng còn xa không ạ?'
];

const quickEmojis = ['👋', '👍', '❤️', '📦', '⚡', '✨'];

const scrollToBottom = () => {
  nextTick(() => {
    if (messagesContainerRef.value) {
      messagesContainerRef.value.scrollTop = messagesContainerRef.value.scrollHeight;
    }
  });
};

watch(
  () => currentMessages.value.length,
  () => {
    scrollToBottom();
  }
);

watch(
  () => isChatModalOpen.value,
  (open) => {
    if (open) {
      scrollToBottom();
    }
  }
);

onMounted(() => {
  scrollToBottom();
});

const handleInput = () => {
  sendTyping(true);
  if (typingTimer) clearTimeout(typingTimer);
  typingTimer = setTimeout(() => {
    sendTyping(false);
  }, 2000);
};

const handleSend = async () => {
  const text = inputText.value.trim();
  if (!text) return;
  inputText.value = '';
  sendTyping(false);
  await sendMessage(text);
  scrollToBottom();
};

const handleSendQuick = async (q: string) => {
  await sendMessage(q);
  scrollToBottom();
};

const handleAddEmoji = (emoji: string) => {
  inputText.value += emoji;
};

const formatPrice = (val?: number) => {
  if (val === undefined || val === null) return '0 đ';
  return `${val.toLocaleString('vi-VN')} đ`;
};

const formatTime = (ts: number) => {
  const d = new Date(ts);
  return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
};
</script>

<template>
  <div v-if="isChatModalOpen && activeStore" class="seller-chat-modal-root" :class="{ minimized: isMinimized }">
    <!-- KHUNG CHAT HOÀN CHỈNH -->
    <div class="seller-chat-card">
      <!-- HEADER -->
      <div class="chat-header" @click="isMinimized && toggleMinimize()">
        <div class="header-store-info">
          <div class="store-avatar-wrap">
            <img :src="getCleanAvatar(activeStore.avatar, activeStore.name, 'seller')" alt="Store" class="store-avatar-img" />
            <span class="status-indicator-dot" :class="{ online: isStoreOnline, offline: !isStoreOnline }"></span>
          </div>
          <div class="store-text-group">
            <div class="store-title-line">
              <span class="store-name-text">{{ activeStore.name }}</span>
              <span class="badge-verified"><i class="bi bi-patch-check-fill"></i></span>
            </div>
            <div class="store-status-line">
              <span class="online-tag" :class="{ 'is-live': isStoreOnline }">
                <span class="pulse-beacon" v-if="isStoreOnline"></span>
                {{ isStoreOnline ? '🟢 Đang hoạt động' : storePresenceLabel }}
              </span>
              <span class="sep-dot">•</span>
              <span class="response-rate">⚡ Phản hồi 5 phút</span>
            </div>
          </div>
        </div>

        <div class="header-actions">
          <button type="button" class="btn-action-icon" :title="isMinimized ? 'Mở rộng' : 'Thu nhỏ'" @click.stop="toggleMinimize">
            <i class="bi" :class="isMinimized ? 'bi-chevron-up' : 'bi-dash-lg'"></i>
          </button>
          <button type="button" class="btn-action-icon" title="Đóng chat" @click.stop="closeChat">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>
      </div>

      <!-- BODY & INPUT (CHỈ HIỂN THỊ KHI KHÔNG THU NHỎ) -->
      <div v-show="!isMinimized" class="chat-content-body">
        <!-- BANNER GHIM SẢN PHẨM ĐANG XEM -->
        <div v-if="activeProduct" class="pinned-product-banner">
          <img :src="activeProduct.image" :alt="activeProduct.name" class="pinned-product-thumb" />
          <div class="pinned-product-meta">
            <div class="pinned-product-title">{{ activeProduct.name }}</div>
            <div class="pinned-product-price">
              {{ formatPrice(activeProduct.price) }}
              <span v-if="activeProduct.unit" class="text-muted fw-normal fs-xs">/ {{ activeProduct.unit }}</span>
            </div>
          </div>
          <div class="pinned-actions">
            <button type="button" class="btn-send-product" @click="sendProductCard">
              <i class="bi bi-send-fill me-1"></i> Hỏi về món này
            </button>
            <button type="button" class="btn-dismiss-product" title="Bỏ qua" @click="activeProduct = null">
              <i class="bi bi-x"></i>
            </button>
          </div>
        </div>

        <!-- DANH SÁCH TIN NHẮN -->
        <div ref="messagesContainerRef" class="chat-messages-container">
          <div class="chat-security-notice">
            <i class="bi bi-shield-check text-success me-1"></i>
            Hội thoại được mã hóa & đồng bộ thời gian thực bảo vệ quyền lợi người mua.
          </div>

          <div v-if="currentMessages.length === 0" class="empty-chat-state">
            <div class="empty-icon-box">💬</div>
            <div class="empty-title">Bắt đầu trò chuyện với Shop</div>
            <p class="empty-desc">
              Gửi tin nhắn hoặc chọn gợi ý bên dưới để được nhân viên tư vấn giải đáp ngay!
            </p>
          </div>

          <div v-for="msg in currentMessages" :key="msg.id" class="message-row" :class="msg.senderRole === 'buyer' ? 'row-buyer' : 'row-seller'">
            <!-- Avatar nếu là seller -->
            <img v-if="msg.senderRole === 'seller'" :src="getCleanAvatar(activeStore.avatar, activeStore.name, 'seller')" class="bubble-avatar" alt="Seller" />

            <div class="bubble-payload">
              <!-- Card sản phẩm đính kèm (nếu có) -->
              <div v-if="msg.productContext" class="msg-product-card">
                <img :src="msg.productContext.image" class="msg-card-img" alt="Product" />
                <div class="msg-card-info">
                  <div class="msg-card-name">{{ msg.productContext.name }}</div>
                  <div class="msg-card-price">{{ formatPrice(msg.productContext.price) }}</div>
                </div>
              </div>

              <!-- Nội dung văn bản -->
              <div class="bubble-text">{{ msg.content }}</div>

              <!-- Thời gian & Trạng thái -->
              <div class="bubble-meta">
                <span class="bubble-time">{{ formatTime(msg.timestamp) }}</span>
                <span v-if="msg.senderRole === 'buyer'" class="bubble-status">
                  <i class="bi" :class="msg.isRead ? 'bi-check2-all text-primary' : 'bi-check2'"></i>
                </span>
              </div>
            </div>
          </div>

            <!-- HIỆU ỨNG ĐANG GÕ BÀN PHÍM -->
          <div v-if="isSellerTyping" class="message-row row-seller typing-row">
            <img :src="getCleanAvatar(activeStore.avatar, activeStore.name, 'seller')" class="bubble-avatar" alt="Seller" />
            <div class="typing-bubble">
              <span class="dot"></span>
              <span class="dot"></span>
              <span class="dot"></span>
              <span class="typing-text">Shop đang soạn tin nhắn...</span>
            </div>
          </div>
        </div>

        <!-- GỢI Ý CÂU HỎI NHANH -->
        <div class="quick-chips-bar">
          <button v-for="(q, idx) in quickQuestions" :key="idx" type="button" class="chip-btn" @click="handleSendQuick(q)">
            {{ q }}
          </button>
        </div>

        <!-- KHU VỰC NHẬP LIỆU -->
        <div class="chat-input-bar">
          <div class="emoji-strip">
            <span v-for="em in quickEmojis" :key="em" class="emoji-item" @click="handleAddEmoji(em)">
              {{ em }}
            </span>
          </div>
          <form class="input-form" @submit.prevent="handleSend">
            <input
              v-model="inputText"
              type="text"
              class="chat-text-input"
              placeholder="Nhắn tin cho Shop..."
              @input="handleInput"
            />
            <button type="submit" class="btn-send-message" :disabled="!inputText.trim()" title="Gửi tin nhắn">
              <i class="bi bi-send-fill"></i>
            </button>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.seller-chat-modal-root {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 1050;
  width: 380px;
  max-width: calc(100vw - 32px);
  box-shadow: 0 16px 40px -12px rgba(15, 23, 42, 0.25), 0 0 0 1px rgba(15, 23, 42, 0.08);
  border-radius: 20px;
  background: #ffffff;
  overflow: hidden;
  font-family: inherit;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.seller-chat-modal-root.minimized {
  width: 320px;
  box-shadow: 0 8px 24px -6px rgba(15, 23, 42, 0.18);
}

.seller-chat-card {
  display: flex;
  flex-direction: column;
  height: 540px;
}

.minimized .seller-chat-card {
  height: auto;
}

/* HEADER */
.chat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  color: #ffffff;
  cursor: pointer;
  user-select: none;
}

.header-store-info {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.store-avatar-wrap {
  position: relative;
  width: 42px;
  height: 42px;
  flex-shrink: 0;
}

.store-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #ffffff;
}

.status-indicator-dot {
  position: absolute;
  bottom: -1px;
  right: -1px;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid #0f172a;
}

.status-indicator-dot.online {
  background: #22c55e;
}

.status-indicator-dot.offline {
  background: #94a3b8;
}

.store-text-group {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.store-title-line {
  display: flex;
  align-items: center;
  gap: 6px;
}

.store-name-text {
  font-size: 14px;
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  color: #ffffff;
}

.badge-verified {
  color: #38bdf8;
  font-size: 13px;
}

.store-status-line {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11.5px;
  color: #94a3b8;
}

.online-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-weight: 500;
}

.online-tag.is-live {
  color: #4ade80;
  font-weight: 600;
}

.pulse-beacon {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #4ade80;
  animation: pulse-glow 1.5s infinite;
}

@keyframes pulse-glow {
  0% { transform: scale(0.9); opacity: 0.8; }
  50% { transform: scale(1.4); opacity: 1; box-shadow: 0 0 6px #4ade80; }
  100% { transform: scale(0.9); opacity: 0.8; }
}

.sep-dot {
  opacity: 0.5;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.btn-action-icon {
  background: transparent;
  border: none;
  color: #cbd5e1;
  width: 28px;
  height: 28px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-action-icon:hover {
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
}

/* CHAT CONTENT BODY */
.chat-content-body {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  background: #f8fafc;
}

/* PINNED PRODUCT BANNER */
.pinned-product-banner {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  background: #fff7ed;
  border-bottom: 1px solid #ffedd5;
}

.pinned-product-thumb {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid #fed7aa;
}

.pinned-product-meta {
  flex: 1;
  min-width: 0;
}

.pinned-product-title {
  font-size: 12px;
  font-weight: 600;
  color: #9a3412;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.pinned-product-price {
  font-size: 13px;
  font-weight: 700;
  color: #ea580c;
}

.pinned-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

.btn-send-product {
  background: #ea580c;
  color: #ffffff;
  border: none;
  padding: 5px 10px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
  white-space: nowrap;
}

.btn-send-product:hover {
  background: #c2410c;
}

.btn-dismiss-product {
  background: transparent;
  border: none;
  color: #9a3412;
  font-size: 16px;
  cursor: pointer;
  padding: 2px 4px;
}

/* MESSAGES CONTAINER */
.chat-messages-container {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  scroll-behavior: smooth;
}

.chat-security-notice {
  text-align: center;
  font-size: 11px;
  color: #64748b;
  background: rgba(241, 245, 249, 0.85);
  border: 1px dashed #cbd5e1;
  padding: 6px 10px;
  border-radius: 8px;
  margin-bottom: 4px;
}

.empty-chat-state {
  margin: auto;
  text-align: center;
  padding: 24px 16px;
}

.empty-icon-box {
  font-size: 36px;
  margin-bottom: 8px;
}

.empty-title {
  font-size: 14px;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 4px;
}

.empty-desc {
  font-size: 12px;
  color: #64748b;
  margin: 0;
  line-height: 1.4;
}

/* MESSAGE ROWS */
.message-row {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  max-width: 85%;
}

.row-seller {
  align-self: flex-start;
}

.row-buyer {
  align-self: flex-end;
  flex-direction: row-reverse;
}

.bubble-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.bubble-payload {
  padding: 10px 14px;
  border-radius: 16px;
  position: relative;
}

.row-seller .bubble-payload {
  background: #ffffff;
  color: #1e293b;
  border-bottom-left-radius: 4px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
  border: 1px solid #e2e8f0;
}

.row-buyer .bubble-payload {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  color: #ffffff;
  border-bottom-right-radius: 4px;
  box-shadow: 0 2px 6px rgba(234, 88, 12, 0.25);
}

.bubble-text {
  font-size: 13.5px;
  line-height: 1.45;
  word-break: break-word;
}

.bubble-meta {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 4px;
  margin-top: 4px;
  font-size: 10px;
  opacity: 0.75;
}

/* MSG PRODUCT CARD */
.msg-product-card {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(0, 0, 0, 0.04);
  padding: 6px 8px;
  border-radius: 8px;
  margin-bottom: 8px;
}

.row-buyer .msg-product-card {
  background: rgba(255, 255, 255, 0.15);
}

.msg-card-img {
  width: 36px;
  height: 36px;
  border-radius: 6px;
  object-fit: cover;
}

.msg-card-name {
  font-size: 11.5px;
  font-weight: 600;
  line-height: 1.25;
}

.msg-card-price {
  font-size: 11.5px;
  font-weight: 700;
  margin-top: 2px;
}

/* TYPING INDICATOR */
.typing-bubble {
  display: flex;
  align-items: center;
  gap: 4px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  padding: 8px 12px;
  border-radius: 14px;
  border-bottom-left-radius: 4px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.06);
}

.typing-bubble .dot {
  width: 6px;
  height: 6px;
  background: #94a3b8;
  border-radius: 50%;
  animation: typing-wave 1.4s infinite ease-in-out;
}

.typing-bubble .dot:nth-child(2) {
  animation-delay: 0.2s;
}

.typing-bubble .dot:nth-child(3) {
  animation-delay: 0.4s;
}

.typing-text {
  font-size: 11px;
  color: #64748b;
  margin-left: 4px;
}

@keyframes typing-wave {
  0%, 60%, 100% { transform: translateY(0); }
  30% { transform: translateY(-4px); }
}

/* QUICK CHIPS */
.quick-chips-bar {
  display: flex;
  gap: 6px;
  overflow-x: auto;
  padding: 6px 12px;
  background: #ffffff;
  border-top: 1px solid #f1f5f9;
  scrollbar-width: none;
}

.quick-chips-bar::-webkit-scrollbar {
  display: none;
}

.chip-btn {
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
  color: #334155;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 11.5px;
  white-space: nowrap;
  cursor: pointer;
  transition: all 0.2s;
}

.chip-btn:hover {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
}

/* INPUT BAR */
.chat-input-bar {
  padding: 8px 12px 12px;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
}

.emoji-strip {
  display: flex;
  gap: 8px;
  margin-bottom: 6px;
}

.emoji-item {
  font-size: 15px;
  cursor: pointer;
  transition: transform 0.15s;
  user-select: none;
}

.emoji-item:hover {
  transform: scale(1.25);
}

.input-form {
  display: flex;
  align-items: center;
  gap: 8px;
}

.chat-text-input {
  flex: 1;
  border: 1px solid #cbd5e1;
  background: #f8fafc;
  border-radius: 20px;
  padding: 8px 14px;
  font-size: 13px;
  outline: none;
  transition: all 0.2s;
}

.chat-text-input:focus {
  border-color: #ea580c;
  background: #ffffff;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.15);
}

.btn-send-message {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #ea580c;
  border: none;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-send-message:hover:not(:disabled) {
  background: #c2410c;
  transform: scale(1.05);
}

.btn-send-message:disabled {
  background: #cbd5e1;
  cursor: not-allowed;
}
</style>
