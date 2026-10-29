import { getCleanAvatar } from '../utils/avatarHelper';
import { ref, computed, watch, onMounted } from 'vue';
import { useAuth } from './useAuth';
import { sellerChatService, type ChatProductContext } from '../services/sellerChatService';

export interface ActiveStore {
  id: string;
  name: string;
  avatar?: string;
  address?: string;
  rating?: number;
  deliveryTime?: string;
  distanceKm?: number;
}

const isChatModalOpen = ref(false);
const isMinimized = ref(false);
const activeStore = ref<ActiveStore | null>(null);
const activeProduct = ref<ChatProductContext | null>(null);

function getOrCreateGuestBuyerId(): string {
  if (typeof window === 'undefined') return 'guest_default';
  let id = localStorage.getItem('zonemart_buyer_chat_guest_id');
  if (!id) {
    id = `guest_${Math.random().toString(36).substring(2, 9)}`;
    localStorage.setItem('zonemart_buyer_chat_guest_id', id);
  }
  return id;
}

export function useSellerChat() {
  const auth = useAuth();

  // Nhận diện định danh Người Mua (Buyer)
  const currentBuyerId = computed(() => {
    const user = auth.currentUser.value;
    if (user?.id) return String(user.id);
    if (user?.phoneEmail) return String(user.phoneEmail).toLowerCase().trim();
    return getOrCreateGuestBuyerId();
  });

  const currentBuyerName = computed(() => {
    const user = auth.currentUser.value;
    if (user?.fullName) return user.fullName;
    if ((user as any)?.name) return (user as any).name;
    return 'Khách Hàng ZoneMart';
  });

  const currentBuyerAvatar = computed(() => {
    const user = auth.currentUser.value;
    const raw = user?.avatarUrl || (user as any)?.avatar;
    return getCleanAvatar(raw, currentBuyerName.value, 'buyer');
  });

  // Conversation ID hiện tại giữa Buyer và Active Store
  const currentConversationId = computed(() => {
    if (!activeStore.value?.id) return '';
    return `${activeStore.value.id}__${currentBuyerId.value}`;
  });

  // Danh sách tin nhắn của cuộc trò chuyện hiện tại
  const currentMessages = computed(() => {
    if (!currentConversationId.value) return [];
    return sellerChatService.messagesByConv[currentConversationId.value] || [];
  });

  // Trạng thái online thời gian thực của Gian Hàng đang chat
  const isStoreOnline = computed(() => {
    if (!activeStore.value) return false;
    return sellerChatService.isPeerOnline(activeStore.value.id, activeStore.value.name);
  });

  const storePresenceLabel = computed(() => {
    if (!activeStore.value) return '⚪ Ngoại tuyến';
    return sellerChatService.getPeerPresenceLabel(activeStore.value.id, activeStore.value.name);
  });

  // Trạng thái Seller đang gõ bàn phím
  const isSellerTyping = computed(() => {
    if (!currentConversationId.value) return false;
    const typing = sellerChatService.activeTyping[currentConversationId.value];
    return typing?.isTyping === true;
  });

  // Đăng ký nhịp tim hiện diện của Buyer ngay khi mở trang
  onMounted(() => {
    sellerChatService.registerPresenceHeartbeat(currentBuyerId.value, 'buyer', currentBuyerName.value);
  });

  // Khi mở cuộc trò chuyện mới, tự động tải tin nhắn từ server và đánh dấu đã đọc
  watch(
    () => currentConversationId.value,
    (newConvId) => {
      if (newConvId && isChatModalOpen.value) {
        sellerChatService.fetchMessages(newConvId);
        sellerChatService.markAsRead(newConvId, 'buyer');
      }
    }
  );

  /**
   * Mở hộp thoại chat với một gian hàng cụ thể
   */
  const openChatWithStore = (store: ActiveStore, product?: any) => {
    activeStore.value = {
      id: store.id || 's1',
      name: store.name || 'Gian Hàng ZoneMart',
      avatar: getCleanAvatar(store.avatar, store.name || 'Gian Hàng', 'seller'),
      address: store.address || 'Hà Nội',
      rating: store.rating || 4.9,
      deliveryTime: store.deliveryTime || '20-30 phút',
      distanceKm: store.distanceKm || 1.2
    };

    if (product) {
      activeProduct.value = {
        id: product.id,
        name: product.name,
        price: product.price,
        oldPrice: product.oldPrice,
        image: product.image,
        unit: product.unit
      };
    } else {
      activeProduct.value = null;
    }

    isChatModalOpen.value = true;
    isMinimized.value = false;

    // Kéo tin nhắn và đánh dấu đã đọc
    if (currentConversationId.value) {
      sellerChatService.fetchMessages(currentConversationId.value);
      sellerChatService.markAsRead(currentConversationId.value, 'buyer');
    }
  };

  const closeChat = () => {
    isChatModalOpen.value = false;
  };

  const toggleMinimize = () => {
    isMinimized.value = !isMinimized.value;
  };

  /**
   * Gửi tin nhắn từ Buyer tới Seller
   */
  const sendMessage = async (content: string, productContext?: ChatProductContext) => {
    if (!activeStore.value) return;
    if (!content.trim() && !productContext) return;

    await sellerChatService.sendMessage({
      storeId: activeStore.value.id,
      storeName: activeStore.value.name,
      buyerId: currentBuyerId.value,
      buyerName: currentBuyerName.value,
      buyerAvatar: currentBuyerAvatar.value,
      senderRole: 'buyer',
      senderId: currentBuyerId.value,
      senderName: currentBuyerName.value,
      senderAvatar: currentBuyerAvatar.value,
      receiverRole: 'seller',
      receiverId: activeStore.value.id,
      receiverName: activeStore.value.name,
      content: content.trim(),
      productContext: productContext || undefined
    });
  };

  /**
   * Gửi card sản phẩm đang xem vào khung chat để hỏi Shop
   */
  const sendProductCard = async () => {
    if (!activeProduct.value || !activeStore.value) return;
    await sendMessage(`Tôi muốn hỏi thông tin về sản phẩm "${activeProduct.value.name}"`, activeProduct.value);
    activeProduct.value = null; // Sau khi gửi thì ẩn banner ghim
  };

  const sendTyping = (isTyping: boolean) => {
    if (!currentConversationId.value) return;
    sellerChatService.sendTyping(currentConversationId.value, 'buyer', currentBuyerName.value, isTyping);
  };

  return {
    isChatModalOpen,
    isMinimized,
    activeStore,
    activeProduct,
    currentBuyerId,
    currentBuyerName,
    currentBuyerAvatar,
    currentConversationId,
    currentMessages,
    isStoreOnline,
    storePresenceLabel,
    isSellerTyping,
    openChatWithStore,
    closeChat,
    toggleMinimize,
    sendMessage,
    sendProductCard,
    sendTyping
  };
}
