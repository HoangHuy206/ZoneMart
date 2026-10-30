<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import { sellerChatService, type ConversationItem } from '../../services/sellerChatService';
import { getCleanAvatar } from '../../utils/avatarHelper';

const props = defineProps<{
  storeId?: string;
  storeName?: string;
  sellerAvatar?: string;
}>();

const emit = defineEmits<{
  (e: 'unread-count-change', count: number): void;
}>();

const currentStoreId = computed(() => props.storeId || 's_mydinh');
const currentStoreName = computed(() => props.storeName || 'Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình');

const selectedConvId = ref<string>('');
const searchQuery = ref('');
const filterTab = ref<'all' | 'unread' | 'online'>('all');
const inputText = ref('');
const messagesContainerRef = ref<HTMLDivElement | null>(null);
let typingTimer: any = null;

const quickReplies = [
  'Dạ sản phẩm vẫn còn sẵn số lượng lớn ạ!',
  'ZoneMart cam kết giao hỏa tốc 20-30 phút tận tay bạn ạ!',
  'Dạ bạn được đồng kiểm tra hàng trước khi thanh toán nhé!',
  'Cảm ơn bạn đã tin tưởng ủng hộ gian hàng, shop xin chuẩn bị đơn ngay ạ!'
];

// Nạp danh sách hội thoại từ service
const allConversations = computed(() => {
  return sellerChatService.conversations;
});

// Lọc hội thoại theo tìm kiếm & tab
const filteredConversations = computed(() => {
  let list = allConversations.value;

  // Lọc theo search
  const q = searchQuery.value.trim().toLowerCase();
  if (q) {
    list = list.filter(
      (c) =>
        c.buyerName.toLowerCase().includes(q) ||
        (c.lastMessage?.content && c.lastMessage.content.toLowerCase().includes(q)) ||
        (c.lastMessage?.productContext?.name && c.lastMessage.productContext.name.toLowerCase().includes(q))
    );
  }

  // Lọc theo tab
  if (filterTab.value === 'unread') {
    list = list.filter((c) => (c.unreadCount || 0) > 0);
  } else if (filterTab.value === 'online') {
    list = list.filter((c) => sellerChatService.isPeerOnline(c.buyerId, c.buyerName));
  }

  return list;
});

// Cuộc trò chuyện đang chọn
const activeConv = computed(() => {
  return allConversations.value.find((c) => c.conversationId === selectedConvId.value) || null;
});

// Danh sách tin nhắn của cuộc trò chuyện đang chọn
const activeMessages = computed(() => {
  if (!selectedConvId.value) return [];
  return sellerChatService.messagesByConv[selectedConvId.value] || [];
});

// Kiểm tra khách hàng trong cuộc trò chuyện hiện tại có đang online không
const isBuyerOnline = computed(() => {
  if (!activeConv.value) return false;
  return sellerChatService.isPeerOnline(activeConv.value.buyerId, activeConv.value.buyerName);
});

const buyerPresenceLabel = computed(() => {
  if (!activeConv.value) return '⚪ Ngoại tuyến';
  return sellerChatService.getPeerPresenceLabel(activeConv.value.buyerId, activeConv.value.buyerName);
});

// Kiểm tra khách hàng có đang gõ bàn phím không
const isBuyerTyping = computed(() => {
  if (!selectedConvId.value) return false;
  const typing = sellerChatService.activeTyping[selectedConvId.value];
  return typing?.isTyping === true && typing.name !== currentStoreName.value;
});

// Tổng số tin nhắn chưa đọc của người bán
const totalUnreadCount = computed(() => {
  return allConversations.value.reduce((acc, c) => acc + (c.unreadCount || 0), 0);
});

watch(
  totalUnreadCount,
  (val) => {
    emit('unread-count-change', val);
  },
  { immediate: true }
);

const scrollToBottom = () => {
  nextTick(() => {
    if (messagesContainerRef.value) {
      messagesContainerRef.value.scrollTop = messagesContainerRef.value.scrollHeight;
    }
  });
};

watch(
  () => activeMessages.value.length,
  () => {
    scrollToBottom();
  }
);

watch(
  () => selectedConvId.value,
  (newId) => {
    if (newId) {
      sellerChatService.fetchMessages(newId);
      sellerChatService.markAsRead(newId, 'seller');
      scrollToBottom();
    }
  }
);

onMounted(() => {
  // Đăng ký nhịp tim hiện diện trực tuyến cho Người Bán (Store)
  sellerChatService.registerPresenceHeartbeat(currentStoreId.value, 'seller', currentStoreName.value);
  // Đồng thời đăng ký cho cả ID s_mydinh và s1 để đảm bảo buyer thấy online tức thì
  sellerChatService.updatePresence('s_mydinh', 'seller', 'Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình', true);
  sellerChatService.updatePresence('s1', 'seller', 'Vườn Rau Hữu Cơ Bác Ba', true);

  // Kéo danh sách hội thoại ban đầu
  sellerChatService.fetchConversations({ storeId: currentStoreId.value }).then((list) => {
    if (list.length > 0 && !selectedConvId.value) {
      selectedConvId.value = list[0].conversationId;
    }
  });
});

const selectConversation = (conv: ConversationItem) => {
  selectedConvId.value = conv.conversationId;
};

const handleInput = () => {
  if (!selectedConvId.value) return;
  sellerChatService.sendTyping(selectedConvId.value, 'seller', currentStoreName.value, true);
  if (typingTimer) clearTimeout(typingTimer);
  typingTimer = setTimeout(() => {
    if (selectedConvId.value) {
      sellerChatService.sendTyping(selectedConvId.value, 'seller', currentStoreName.value, false);
    }
  }, 2000);
};

const handleSendMessage = async () => {
  const text = inputText.value.trim();
  if (!text || !activeConv.value) return;

  inputText.value = '';
  if (selectedConvId.value) {
    sellerChatService.sendTyping(selectedConvId.value, 'seller', currentStoreName.value, false);
  }

  await sellerChatService.sendMessage({
    storeId: activeConv.value.storeId || currentStoreId.value,
    storeName: currentStoreName.value,
    buyerId: activeConv.value.buyerId,
    buyerName: activeConv.value.buyerName,
    buyerAvatar: activeConv.value.buyerAvatar,
    senderRole: 'seller',
    senderId: currentStoreId.value,
    senderName: currentStoreName.value,
    senderAvatar: getCleanAvatar(props.sellerAvatar, currentStoreName.value, 'seller'),
    receiverRole: 'buyer',
    receiverId: activeConv.value.buyerId,
    receiverName: activeConv.value.buyerName,
    content: text
  });

  scrollToBottom();
};

const handleSendQuickReply = async (reply: string) => {
  if (!activeConv.value) return;
  inputText.value = '';
  await sellerChatService.sendMessage({
    storeId: activeConv.value.storeId || currentStoreId.value,
    storeName: currentStoreName.value,
    buyerId: activeConv.value.buyerId,
    buyerName: activeConv.value.buyerName,
    buyerAvatar: activeConv.value.buyerAvatar,
    senderRole: 'seller',
    senderId: currentStoreId.value,
    senderName: currentStoreName.value,
    senderAvatar: getCleanAvatar(props.sellerAvatar, currentStoreName.value, 'seller'),
    receiverRole: 'buyer',
    receiverId: activeConv.value.buyerId,
    receiverName: activeConv.value.buyerName,
    content: reply
  });
  scrollToBottom();
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
  <div class="seller-chat-dashboard-container">
    <!-- TOP HEADER BAR -->
    <div class="chat-dashboard-header">
      <div class="header-titles">
        <h4 class="m-0 fw-bold d-flex align-items-center gap-2">
          <i class="bi bi-chat-square-dots-fill text-warning"></i>
          Trung Tâm Tư Vấn Khách Hàng Thời Gian Thực
        </h4>
        <p class="text-muted fs-sm m-0 mt-1">
          Hội thoại trực tuyến 0ms giữa Shop và Khách hàng, cập nhật trạng thái Online theo thời gian thực.
        </p>
      </div>

      <div class="header-presence-pill">
        <span class="pulse-dot-green"></span>
        <span class="presence-text">Gian hàng đang trực tuyến</span>
        <span class="badge bg-success-subtle text-success border border-success-subtle ms-2">Thời gian thực</span>
      </div>
    </div>

    <!-- MAIN TWO-COLUMN SPLIT -->
    <div class="chat-dashboard-body">
      <!-- CỘT TRÁI: DANH SÁCH KHÁCH HÀNG / HỘI THOẠI -->
      <aside class="conversations-sidebar">
        <!-- Ô TÌM KIẾM -->
        <div class="search-box-wrap">
          <i class="bi bi-search search-icon"></i>
          <input
            v-model="searchQuery"
            type="text"
            class="search-input"
            placeholder="Tìm theo tên khách hàng, nội dung..."
          />
        </div>

        <!-- TABS LỌC -->
        <div class="filter-tabs-row">
          <button
            type="button"
            class="filter-tab-btn"
            :class="{ active: filterTab === 'all' }"
            @click="filterTab = 'all'"
          >
            Tất cả ({{ allConversations.length }})
          </button>
          <button
            type="button"
            class="filter-tab-btn"
            :class="{ active: filterTab === 'unread' }"
            @click="filterTab = 'unread'"
          >
            Chưa đọc
            <span v-if="totalUnreadCount > 0" class="mini-badge">{{ totalUnreadCount }}</span>
          </button>
          <button
            type="button"
            class="filter-tab-btn"
            :class="{ active: filterTab === 'online' }"
            @click="filterTab = 'online'"
          >
            Đang Online
          </button>
        </div>

        <!-- DANH SÁCH THẺ HỘI THOẠI -->
        <div class="conversations-scroll-list">
          <div v-if="filteredConversations.length === 0" class="empty-list-state">
            <i class="bi bi-inbox fs-2 text-muted"></i>
            <p class="mt-2 text-muted fs-sm">Không có cuộc trò chuyện nào phù hợp.</p>
          </div>

          <div
            v-for="conv in filteredConversations"
            :key="conv.conversationId"
            class="conv-item-card"
            :class="{
              active: conv.conversationId === selectedConvId,
              'has-unread': (conv.unreadCount || 0) > 0
            }"
            @click="selectConversation(conv)"
          >
            <div class="conv-avatar-wrap">
              <img
                :src="getCleanAvatar(conv.buyerAvatar, conv.buyerName, 'buyer')"
                alt="Buyer Avatar"
                class="conv-avatar-img"
              />
              <span
                class="conv-online-dot"
                :class="{ online: sellerChatService.isPeerOnline(conv.buyerId, conv.buyerName) }"
                :title="sellerChatService.isPeerOnline(conv.buyerId, conv.buyerName) ? 'Khách hàng đang online' : 'Ngoại tuyến'"
              ></span>
            </div>

            <div class="conv-info-wrap">
              <div class="conv-top-line">
                <strong class="conv-buyer-name">{{ conv.buyerName }}</strong>
                <span class="conv-time" v-if="conv.lastMessage">{{ formatTime(conv.lastMessage.timestamp) }}</span>
              </div>

              <div class="conv-bottom-line">
                <span class="conv-snippet">
                  <span v-if="conv.lastMessage?.senderRole === 'seller'" class="me-1 text-muted">Bạn:</span>
                  {{ conv.lastMessage?.content || 'Hình ảnh / Sản phẩm' }}
                </span>
                <span v-if="(conv.unreadCount || 0) > 0" class="conv-unread-pill">
                  {{ conv.unreadCount }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </aside>

      <!-- CỘT PHẢI: KHUNG CHAT CHI TIẾT VỚI KHÁCH HÀNG -->
      <main class="chat-thread-main">
        <template v-if="activeConv">
          <!-- THREAD HEADER -->
          <div class="thread-header">
            <div class="thread-user-info">
              <div class="thread-avatar-wrap">
                <img
                  :src="getCleanAvatar(activeConv.buyerAvatar, activeConv.buyerName, 'buyer')"
                  alt="Buyer"
                  class="thread-avatar-img"
                />
                <span
                  class="thread-status-dot"
                  :class="{ online: isBuyerOnline, offline: !isBuyerOnline }"
                ></span>
              </div>
              <div class="thread-text-block">
                <div class="thread-name-line">
                  <span class="thread-name">{{ activeConv.buyerName }}</span>
                  <span class="badge-role">Khách hàng</span>
                </div>
                <div class="thread-status-line">
                  <span class="status-text" :class="{ 'is-live': isBuyerOnline }">
                    <span v-if="isBuyerOnline" class="pulse-beacon-sm"></span>
                    {{ isBuyerOnline ? '🟢 Đang trực tuyến trên ứng dụng' : buyerPresenceLabel }}
                  </span>
                </div>
              </div>
            </div>

            <div class="thread-actions">
              <button
                type="button"
                class="btn-thread-action"
                title="Đánh dấu đã đọc"
                @click="sellerChatService.markAsRead(activeConv.conversationId, 'seller')"
              >
                <i class="bi bi-check2-all"></i> Đã đọc
              </button>
            </div>
          </div>

          <!-- BANNER NẾU CÓ SẢN PHẨM KHÁCH HÀNG ĐANG HỎI -->
          <div v-if="activeConv.lastMessage?.productContext" class="product-inquiry-bar">
            <img :src="activeConv.lastMessage.productContext.image" class="inquiry-thumb" alt="Product" />
            <div class="inquiry-meta">
              <div class="inquiry-label">Khách hàng đang quan tâm sản phẩm:</div>
              <strong class="inquiry-title">{{ activeConv.lastMessage.productContext.name }}</strong>
            </div>
            <div class="inquiry-price">
              {{ formatPrice(activeConv.lastMessage.productContext.price) }}
            </div>
          </div>

          <!-- DANH SÁCH TIN NHẮN -->
          <div ref="messagesContainerRef" class="thread-messages-scroll">
            <div class="thread-security-pill">
              <i class="bi bi-lock-fill me-1"></i> Tin nhắn được đồng bộ thời gian thực 2 chiều giữa Người Bán và Khách Hàng.
            </div>

            <div
              v-for="msg in activeMessages"
              :key="msg.id"
              class="thread-msg-row"
              :class="msg.senderRole === 'seller' ? 'row-seller' : 'row-buyer'"
            >
              <img
                v-if="msg.senderRole === 'buyer'"
                :src="getCleanAvatar(activeConv.buyerAvatar, activeConv.buyerName, 'buyer')"
                class="msg-avatar"
                alt="Buyer"
              />

              <div class="msg-bubble-wrap">
                <!-- Card sản phẩm đính kèm -->
                <div v-if="msg.productContext" class="embedded-product-card">
                  <img :src="msg.productContext.image" class="embedded-img" alt="Product" />
                  <div class="embedded-info">
                    <div class="embedded-name">{{ msg.productContext.name }}</div>
                    <div class="embedded-price">{{ formatPrice(msg.productContext.price) }}</div>
                  </div>
                </div>

                <div class="msg-text">{{ msg.content }}</div>

                <div class="msg-meta">
                  <span class="msg-time">{{ formatTime(msg.timestamp) }}</span>
                  <span v-if="msg.senderRole === 'seller'" class="msg-status">
                    <i class="bi" :class="msg.isRead ? 'bi-check2-all text-primary' : 'bi-check2'"></i>
                  </span>
                </div>
              </div>
            </div>

            <!-- HIỆU ỨNG KHÁCH HÀNG ĐANG GÕ BÀN PHÍM -->
            <div v-if="isBuyerTyping" class="thread-msg-row row-buyer typing-indicator-row">
              <img
                :src="getCleanAvatar(activeConv.buyerAvatar, activeConv.buyerName, 'buyer')"
                class="msg-avatar"
                alt="Buyer"
              />
              <div class="typing-bubble-seller">
                <span class="dot"></span>
                <span class="dot"></span>
                <span class="dot"></span>
                <span class="txt">Khách hàng đang soạn tin...</span>
              </div>
            </div>
          </div>

          <!-- MẪU PHẢN HỒI NHANH CHO SELLER -->
          <div class="seller-quick-replies-strip">
            <span class="quick-reply-label"><i class="bi bi-lightning-fill text-warning"></i> Trả lời nhanh:</span>
            <div class="replies-chips">
              <button
                v-for="(r, idx) in quickReplies"
                :key="idx"
                type="button"
                class="btn-reply-chip"
                @click="handleSendQuickReply(r)"
              >
                {{ r }}
              </button>
            </div>
          </div>

          <!-- KHUNG SOẠN TIN NHẮN -->
          <div class="thread-input-box">
            <form class="thread-input-form" @submit.prevent="handleSendMessage">
              <input
                v-model="inputText"
                type="text"
                class="thread-text-input"
                placeholder="Nhập câu trả lời tư vấn cho khách hàng..."
                @input="handleInput"
              />
              <button
                type="submit"
                class="btn-thread-send"
                :disabled="!inputText.trim()"
                title="Gửi tin nhắn"
              >
                <i class="bi bi-send-fill me-1"></i> Gửi
              </button>
            </form>
          </div>
        </template>

        <!-- KHI CHƯA CHỌN HỘI THOẠI NÀO -->
        <div v-else class="no-conv-selected">
          <div class="empty-illustration">
            <i class="bi bi-chat-heart-fill"></i>
          </div>
          <h5>Chọn một khách hàng để bắt đầu tư vấn</h5>
          <p class="text-muted fs-sm">
            Tất cả tin nhắn đến từ người mua sẽ hiển thị và cập nhật thời gian thực tại đây.
          </p>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
.seller-chat-dashboard-container {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 120px);
  min-height: 600px;
  background: #ffffff;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 20px -4px rgba(15, 23, 42, 0.06);
  overflow: hidden;
}

/* HEADER BAR */
.chat-dashboard-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 24px;
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
}

.header-presence-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #166534;
  font-size: 13px;
  font-weight: 600;
  padding: 6px 14px;
  border-radius: 20px;
}

.pulse-dot-green {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #22c55e;
  animation: pulse-ring 1.6s infinite;
}

@keyframes pulse-ring {
  0% { transform: scale(0.9); opacity: 0.8; }
  50% { transform: scale(1.4); opacity: 1; box-shadow: 0 0 6px #22c55e; }
  100% { transform: scale(0.9); opacity: 0.8; }
}

/* MAIN BODY */
.chat-dashboard-body {
  display: flex;
  flex: 1;
  min-height: 0;
}

/* SIDEBAR HỘI THOẠI */
.conversations-sidebar {
  width: 340px;
  border-right: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  background: #ffffff;
  flex-shrink: 0;
}

.search-box-wrap {
  position: relative;
  padding: 12px 16px;
  border-bottom: 1px solid #f1f5f9;
}

.search-icon {
  position: absolute;
  left: 28px;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
  font-size: 13px;
}

.search-input {
  width: 100%;
  padding: 8px 12px 8px 34px;
  font-size: 13px;
  border-radius: 10px;
  border: 1px solid #e2e8f0;
  background: #f8fafc;
  outline: none;
  transition: all 0.2s;
}

.search-input:focus {
  border-color: #ea580c;
  background: #ffffff;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}

.filter-tabs-row {
  display: flex;
  padding: 6px 16px;
  gap: 6px;
  border-bottom: 1px solid #f1f5f9;
  background: #fafafa;
}

.filter-tab-btn {
  background: transparent;
  border: none;
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  padding: 6px 10px;
  border-radius: 8px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  transition: all 0.2s;
}

.filter-tab-btn.active {
  background: #ea580c;
  color: #ffffff;
}

.mini-badge {
  background: #ef4444;
  color: #ffffff;
  font-size: 10px;
  padding: 1px 5px;
  border-radius: 10px;
}

/* DANH SÁCH CUỘC TRÒ CHUYỆN */
.conversations-scroll-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.conv-item-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.15s ease;
  margin-bottom: 4px;
  border: 1px solid transparent;
}

.conv-item-card:hover {
  background: #f8fafc;
}

.conv-item-card.active {
  background: #fff7ed;
  border-color: #fed7aa;
}

.conv-avatar-wrap {
  position: relative;
  width: 44px;
  height: 44px;
  flex-shrink: 0;
}

.conv-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
  border: 1px solid #e2e8f0;
}

.conv-online-dot {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 11px;
  height: 11px;
  border-radius: 50%;
  border: 2px solid #ffffff;
  background: #cbd5e1;
}

.conv-online-dot.online {
  background: #22c55e;
}

.conv-info-wrap {
  flex: 1;
  min-width: 0;
}

.conv-top-line {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  margin-bottom: 4px;
}

.conv-buyer-name {
  font-size: 13.5px;
  color: #1e293b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.conv-time {
  font-size: 11px;
  color: #94a3b8;
  flex-shrink: 0;
}

.conv-bottom-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.conv-snippet {
  font-size: 12px;
  color: #64748b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.conv-item-card.has-unread .conv-snippet {
  font-weight: 600;
  color: #0f172a;
}

.conv-unread-pill {
  background: #ea580c;
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  padding: 1px 7px;
  border-radius: 10px;
  flex-shrink: 0;
}

/* KHUNG CHAT CHÍNH (RIGHT MAIN) */
.chat-thread-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: #f8fafc;
  min-width: 0;
}

/* THREAD HEADER */
.thread-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 20px;
  background: #ffffff;
  border-bottom: 1px solid #e2e8f0;
}

.thread-user-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.thread-avatar-wrap {
  position: relative;
  width: 40px;
  height: 40px;
}

.thread-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
}

.thread-status-dot {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 11px;
  height: 11px;
  border-radius: 50%;
  border: 2px solid #ffffff;
}

.thread-status-dot.online {
  background: #22c55e;
}

.thread-status-dot.offline {
  background: #94a3b8;
}

.thread-name-line {
  display: flex;
  align-items: center;
  gap: 8px;
}

.thread-name {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
}

.badge-role {
  font-size: 11px;
  background: #f1f5f9;
  color: #475569;
  padding: 1px 7px;
  border-radius: 6px;
}

.thread-status-line {
  font-size: 12px;
  color: #64748b;
  margin-top: 2px;
}

.status-text.is-live {
  color: #16a34a;
  font-weight: 600;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.pulse-beacon-sm {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #22c55e;
  animation: pulse-ring 1.5s infinite;
}

.btn-thread-action {
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
  color: #334155;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-thread-action:hover {
  background: #e2e8f0;
}

/* BANNER SẢN PHẨM KHÁCH HỎI */
.product-inquiry-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 18px;
  background: #fff7ed;
  border-bottom: 1px solid #fed7aa;
}

.inquiry-thumb {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid #fed7aa;
}

.inquiry-meta {
  flex: 1;
}

.inquiry-label {
  font-size: 11px;
  color: #c2410c;
  font-weight: 500;
}

.inquiry-title {
  font-size: 13px;
  color: #9a3412;
}

.inquiry-price {
  font-size: 14px;
  font-weight: 700;
  color: #ea580c;
}

/* MESSAGES SCROLL */
.thread-messages-scroll {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.thread-security-pill {
  align-self: center;
  background: #f1f5f9;
  color: #64748b;
  font-size: 11px;
  padding: 4px 12px;
  border-radius: 12px;
  margin-bottom: 8px;
}

.thread-msg-row {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  max-width: 75%;
}

.thread-msg-row.row-buyer {
  align-self: flex-start;
}

.thread-msg-row.row-seller {
  align-self: flex-end;
  flex-direction: row-reverse;
}

.msg-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.msg-bubble-wrap {
  padding: 12px 16px;
  border-radius: 16px;
  position: relative;
}

.row-buyer .msg-bubble-wrap {
  background: #ffffff;
  color: #1e293b;
  border-bottom-left-radius: 4px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.05);
}

.row-seller .msg-bubble-wrap {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  color: #ffffff;
  border-bottom-right-radius: 4px;
  box-shadow: 0 2px 6px rgba(234, 88, 12, 0.2);
}

.msg-text {
  font-size: 14px;
  line-height: 1.45;
  word-break: break-word;
}

.msg-meta {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 4px;
  font-size: 10.5px;
  margin-top: 4px;
  opacity: 0.8;
}

/* EMBEDDED PRODUCT CARD */
.embedded-product-card {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(0, 0, 0, 0.04);
  padding: 6px 10px;
  border-radius: 8px;
  margin-bottom: 8px;
}

.row-seller .embedded-product-card {
  background: rgba(255, 255, 255, 0.15);
}

.embedded-img {
  width: 38px;
  height: 38px;
  border-radius: 6px;
  object-fit: cover;
}

.embedded-name {
  font-size: 12px;
  font-weight: 600;
}

.embedded-price {
  font-size: 12px;
  font-weight: 700;
}

/* TYPING INDICATOR */
.typing-bubble-seller {
  display: flex;
  align-items: center;
  gap: 4px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  padding: 8px 12px;
  border-radius: 14px;
  border-bottom-left-radius: 4px;
}

.typing-bubble-seller .dot {
  width: 6px;
  height: 6px;
  background: #94a3b8;
  border-radius: 50%;
  animation: typing-anim 1.4s infinite ease-in-out;
}

.typing-bubble-seller .dot:nth-child(2) {
  animation-delay: 0.2s;
}

.typing-bubble-seller .dot:nth-child(3) {
  animation-delay: 0.4s;
}

.typing-bubble-seller .txt {
  font-size: 11.5px;
  color: #64748b;
  margin-left: 6px;
}

@keyframes typing-anim {
  0%, 60%, 100% { transform: translateY(0); }
  30% { transform: translateY(-4px); }
}

/* QUICK REPLIES STRIP */
.seller-quick-replies-strip {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 18px;
  background: #ffffff;
  border-top: 1px solid #f1f5f9;
  overflow-x: auto;
}

.quick-reply-label {
  font-size: 12px;
  font-weight: 700;
  color: #475569;
  white-space: nowrap;
}

.replies-chips {
  display: flex;
  gap: 6px;
  overflow-x: auto;
  scrollbar-width: none;
}

.replies-chips::-webkit-scrollbar {
  display: none;
}

.btn-reply-chip {
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
  color: #334155;
  padding: 5px 12px;
  border-radius: 14px;
  font-size: 12px;
  white-space: nowrap;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-reply-chip:hover {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
}

/* INPUT BOX */
.thread-input-box {
  padding: 12px 18px;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
}

.thread-input-form {
  display: flex;
  gap: 10px;
}

.thread-text-input {
  flex: 1;
  border: 1px solid #cbd5e1;
  border-radius: 12px;
  padding: 10px 16px;
  font-size: 13.5px;
  outline: none;
  background: #f8fafc;
  transition: all 0.2s;
}

.thread-text-input:focus {
  border-color: #ea580c;
  background: #ffffff;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}

.btn-thread-send {
  background: #ea580c;
  border: none;
  color: #ffffff;
  padding: 0 20px;
  border-radius: 12px;
  font-size: 13.5px;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s;
}

.btn-thread-send:hover:not(:disabled) {
  background: #c2410c;
  transform: translateY(-1px);
}

.btn-thread-send:disabled {
  background: #cbd5e1;
  cursor: not-allowed;
}

/* EMPTY STATE KHI CHƯA CHỌN CONV */
.no-conv-selected {
  margin: auto;
  text-align: center;
  padding: 40px;
}

.empty-illustration {
  font-size: 48px;
  color: #ea580c;
  opacity: 0.6;
  margin-bottom: 12px;
}
</style>
