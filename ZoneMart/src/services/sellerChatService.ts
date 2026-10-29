/**
 * ================================================================
 * SELLER CHAT REALTIME SERVICE (ZoneMart)
 * ================================================================
 * Quản lý hội thoại & chat trực tiếp giữa Người Mua (Buyer) và Gian Hàng (Seller).
 * Hỗ trợ đồng bộ thời gian thực 0ms qua BroadcastChannel (đa tab cùng trình duyệt),
 * Server-Sent Events (SSE) xuyên thiết bị / mạng LAN, và nhịp tim hiện diện
 * trực tuyến (Online / Offline Presence Heartbeat) chuẩn xác theo thời gian thực.
 */

import { reactive } from 'vue';
import { apiFetch, getApiUrl } from '../utils/apiConfig';
import { getCleanAvatar } from '../utils/avatarHelper';

export interface ChatProductContext {
  id?: string;
  name?: string;
  price: number;
  oldPrice?: number;
  image?: string;
  unit?: string;
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  storeId: string;
  storeName: string;
  buyerId: string;
  buyerName: string;
  buyerAvatar?: string;
  senderRole: 'buyer' | 'seller';
  senderId: string;
  senderName: string;
  senderAvatar?: string;
  receiverRole: 'buyer' | 'seller';
  receiverId: string;
  receiverName: string;
  content: string;
  productContext?: ChatProductContext;
  timestamp: number;
  isRead: boolean;
}

export interface PresenceInfo {
  id: string;
  role: 'buyer' | 'seller';
  name: string;
  isOnline: boolean;
  lastSeen: number;
}

export interface ConversationItem {
  conversationId: string;
  storeId: string;
  storeName: string;
  buyerId: string;
  buyerName: string;
  buyerAvatar?: string;
  storeAvatar?: string;
  lastMessage?: ChatMessage;
  unreadCount: number;
  isPeerOnline: boolean;
  updatedAt: number;
}

export type ChatMessageCallback = (msg: ChatMessage) => void;
export type PresenceCallback = (presence: PresenceInfo) => void;
export type TypingCallback = (payload: { conversationId: string; role: string; name: string; isTyping: boolean }) => void;

class SellerChatService {
  private channel: BroadcastChannel | null = null;
  private readonly CHANNEL_NAME = 'zonemart_seller_chat_channel';
  private readonly STORAGE_MESSAGES_KEY = 'zonemart_seller_chat_messages_cache';
  private readonly STORAGE_CONV_KEY = 'zonemart_seller_chat_conversations_cache';

  // State phản ứng toàn cục
  public presenceMap = reactive<Record<string, PresenceInfo>>({});
  public messagesByConv = reactive<Record<string, ChatMessage[]>>({});
  public conversations = reactive<ConversationItem[]>([]);
  public activeTyping = reactive<Record<string, { name: string; isTyping: boolean; timer?: any }>>({});

  private messageListeners: ChatMessageCallback[] = [];
  private presenceListeners: PresenceCallback[] = [];
  private typingListeners: TypingCallback[] = [];

  private heartbeatTimer: any = null;
  private currentRegisteredUser: { id: string; role: 'buyer' | 'seller'; name: string } | null = null;

  constructor() {
    this.initChannel();
    this.initSseListener();
    this.initStorageFallback();
    this.loadFromLocalStorage();
    this.startPresenceCheckInterval();
    this.syncInitialPresences();
    this.startPeriodicPresenceSync();
  }

  /**
   * Khởi tạo kênh BroadcastChannel phục vụ trao đổi 0ms giữa các tab cùng máy
   */
  private initChannel() {
    if (typeof window === 'undefined' || !('BroadcastChannel' in window)) return;
    try {
      this.channel = new BroadcastChannel(this.CHANNEL_NAME);
      this.channel.onmessage = (event) => {
        this.handleIncomingPayload(event.data);
      };
    } catch (e) {
      console.warn('[SellerChatService] BroadcastChannel init error:', e);
    }
  }

  /**
   * Lắng nghe gói tin SSE từ Server phát tán qua window event 'zonemart_chat_event'
   */
  private initSseListener() {
    if (typeof window === 'undefined') return;
    window.addEventListener('zonemart_chat_event', (e: any) => {
      if (e?.detail) {
        this.handleIncomingPayload(e.detail);
      }
    });
  }

  /**
   * Dự phòng bằng StorageEvent nếu trình duyệt không có BroadcastChannel
   */
  private initStorageFallback() {
    if (typeof window === 'undefined') return;
    window.addEventListener('storage', (e) => {
      if (e.key === 'zonemart_seller_chat_bus' && e.newValue) {
        try {
          const payload = JSON.parse(e.newValue);
          this.handleIncomingPayload(payload);
        } catch {}
      }
    });

    // Bắt sự kiện người dùng đóng tab để phát tín hiệu offline lập tức
    window.addEventListener('beforeunload', () => {
      if (this.currentRegisteredUser) {
        this.updatePresence(this.currentRegisteredUser.id, this.currentRegisteredUser.role, this.currentRegisteredUser.name, false);
      }
    });
  }

  /**
   * Nạp lịch sử tin nhắn và danh sách hội thoại từ bộ nhớ đệm
   */
  private loadFromLocalStorage() {
    if (typeof window === 'undefined') return;
    try {
      const savedMessages = localStorage.getItem(this.STORAGE_MESSAGES_KEY);
      if (savedMessages) {
        const parsed = JSON.parse(savedMessages);
        Object.assign(this.messagesByConv, parsed);
      }
      const savedConvs = localStorage.getItem(this.STORAGE_CONV_KEY);
      if (savedConvs) {
        const parsedConvs = JSON.parse(savedConvs);
        this.conversations.splice(0, this.conversations.length, ...parsedConvs);
      }
    } catch {}
  }

  private saveToLocalStorage() {
    if (typeof window === 'undefined') return;
    try {
      localStorage.setItem(this.STORAGE_MESSAGES_KEY, JSON.stringify(this.messagesByConv));
      localStorage.setItem(this.STORAGE_CONV_KEY, JSON.stringify(this.conversations));
    } catch {}
  }

  /**
   * Định kỳ kiểm tra timeout hiện diện để chuyển Online -> Offline nếu mất nhịp tim sau 20 giây
   */
  private startPresenceCheckInterval() {
    if (typeof window === 'undefined') return;
    setInterval(() => {
      const now = Date.now();
      for (const id in this.presenceMap) {
        const p = this.presenceMap[id];
        if (p.isOnline && now - p.lastSeen > 20000) {
          p.isOnline = false;
        }
      }
    }, 3000);
  }

  /**
   * Kéo liên tục trạng thái Online từ Server để đồng bộ thời gian thực xuyên trình duyệt
   */
  private startPeriodicPresenceSync() {
    if (typeof window === 'undefined') return;
    setInterval(() => {
      this.syncInitialPresences();
    }, 2500);
  }

  /**
   * Kéo danh sách trạng thái Online từ Server
   */
  private async syncInitialPresences() {
    try {
      const res = await apiFetch('/api/chat/presence');
      if (res.ok) {
        const data = await res.json();
        if (data?.presences) {
          for (const key in data.presences) {
            this.presenceMap[key] = data.presences[key];
          }
        }
      }
    } catch {}
  }

  /**
   * Bộ xử lý trung tâm mọi gói tin thời gian thực
   */
  private handleIncomingPayload(data: any) {
    if (!data || !data.type) return;

    if (data.type === 'CHAT_MESSAGE' && data.payload) {
      const msg: ChatMessage = data.payload;
      this.storeMessageLocally(msg);
      this.messageListeners.forEach((cb) => cb(msg));
    } else if (data.type === 'CHAT_PRESENCE' && data.payload) {
      const p: PresenceInfo = data.payload;
      this.presenceMap[p.id] = {
        ...p,
        lastSeen: p.lastSeen || Date.now()
      };
      this.presenceListeners.forEach((cb) => cb(this.presenceMap[p.id]));
    } else if (data.type === 'CHAT_TYPING' && data.payload) {
      const { conversationId, role, name, isTyping } = data.payload;
      if (!this.activeTyping[conversationId]) {
        this.activeTyping[conversationId] = { name: '', isTyping: false };
      }
      this.activeTyping[conversationId].name = name;
      this.activeTyping[conversationId].isTyping = isTyping;

      if (this.activeTyping[conversationId].timer) {
        clearTimeout(this.activeTyping[conversationId].timer);
      }
      if (isTyping) {
        this.activeTyping[conversationId].timer = setTimeout(() => {
          if (this.activeTyping[conversationId]) {
            this.activeTyping[conversationId].isTyping = false;
          }
        }, 3500);
      }
      this.typingListeners.forEach((cb) => cb(data.payload));
    } else if (data.type === 'CHAT_READ' && data.payload) {
      const { conversationId, readerRole } = data.payload;
      if (this.messagesByConv[conversationId]) {
        this.messagesByConv[conversationId].forEach((m) => {
          if (m.receiverRole === readerRole) m.isRead = true;
        });
      }
    }
  }

  /**
   * Lưu tin nhắn vào bộ nhớ đệm và cập nhật tóm tắt hội thoại
   */
  private storeMessageLocally(msg: ChatMessage) {
    if (!this.messagesByConv[msg.conversationId]) {
      this.messagesByConv[msg.conversationId] = [];
    }
    msg.buyerAvatar = getCleanAvatar(msg.buyerAvatar, msg.buyerName, 'buyer');
    if (msg.senderRole === 'seller') {
      msg.senderAvatar = getCleanAvatar(msg.senderAvatar, msg.senderName, 'seller');
    } else {
      msg.senderAvatar = getCleanAvatar(msg.senderAvatar, msg.senderName, 'buyer');
    }

    const list = this.messagesByConv[msg.conversationId];
    const exists = list.some((m) => m.id === msg.id);
    if (!exists) {
      list.push(msg);
      list.sort((a, b) => a.timestamp - b.timestamp);
    }

    // Cập nhật hoặc thêm vào danh sách hội thoại
    let conv = this.conversations.find((c) => c.conversationId === msg.conversationId);
    if (!conv) {
      conv = {
        conversationId: msg.conversationId,
        storeId: msg.storeId,
        storeName: msg.storeName,
        buyerId: msg.buyerId,
        buyerName: msg.buyerName,
        buyerAvatar: msg.buyerAvatar,
        lastMessage: msg,
        unreadCount: msg.isRead ? 0 : 1,
        isPeerOnline: this.isPeerOnline(msg.senderRole === 'buyer' ? msg.storeId : msg.buyerId),
        updatedAt: msg.timestamp
      };
      this.conversations.unshift(conv);
    } else {
      conv.lastMessage = msg;
      conv.updatedAt = msg.timestamp;
      if (!msg.isRead) {
        conv.unreadCount = (conv.unreadCount || 0) + 1;
      }
      // Đưa hội thoại lên đầu danh sách
      const idx = this.conversations.indexOf(conv);
      if (idx > 0) {
        this.conversations.splice(idx, 1);
        this.conversations.unshift(conv);
      }
    }

    this.saveToLocalStorage();
  }

  /**
   * Phát tán gói tin ra các tab khác qua BroadcastChannel và localStorage
   */
  private broadcastLocal(payload: any) {
    if (this.channel) {
      try {
        this.channel.postMessage(payload);
      } catch {}
    }
    if (typeof localStorage !== 'undefined') {
      try {
        localStorage.setItem('zonemart_seller_chat_bus', JSON.stringify({ ...payload, _t: Date.now() }));
      } catch {}
    }
  }

  // ================================================================
  // PUBLIC APIS: PRESENCE & ONLINE STATUS
  // ================================================================

  /**
   * Đăng ký nhịp tim hiện diện trực tuyến (Heartbeat) cho vai trò hiện tại (Buyer / Seller)
   */
  public registerPresenceHeartbeat(id: string, role: 'buyer' | 'seller', name: string) {
    this.currentRegisteredUser = { id, role, name };
    this.updatePresence(id, role, name, true);

    if (this.heartbeatTimer) clearInterval(this.heartbeatTimer);
    this.heartbeatTimer = setInterval(() => {
      this.updatePresence(id, role, name, true);
    }, 3000);
  }

  /**
   * Cập nhật trạng thái trực tuyến
   */
  public updatePresence(id: string, role: 'buyer' | 'seller', name: string, isOnline: boolean) {
    const payload = {
      id,
      role,
      name,
      isOnline,
      lastSeen: Date.now()
    };

    this.presenceMap[id] = payload;
    this.broadcastLocal({ type: 'CHAT_PRESENCE', payload });

    // Gửi về Server Backend C#
    apiFetch('/api/chat/presence', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }).catch(() => {});
  }

  /**
   * Kiểm tra xem một đối tượng (Store ID hoặc Buyer ID) có đang trực tuyến không
   */
  /**
   * Tìm kiếm thông tin hiện diện theo ID hoặc Tên
   */
  public findPresence(idOrName: string): PresenceInfo | null {
    if (!idOrName) return null;
    const clean = idOrName.trim();
    if (this.presenceMap[clean]) return this.presenceMap[clean];

    const low = clean.toLowerCase();
    for (const key in this.presenceMap) {
      const p = this.presenceMap[key];
      if (
        key.toLowerCase() === low ||
        (p.name && p.name.toLowerCase().trim() === low) ||
        (p.id && p.id.toLowerCase().trim() === low)
      ) {
        return p;
      }
    }
    return null;
  }

  public isPeerOnline(idOrName?: string, secondaryIdOrName?: string): boolean {
    if (!idOrName && !secondaryIdOrName) return false;
    const p1 = idOrName ? this.findPresence(idOrName) : null;
    if (p1 && p1.isOnline === true && Date.now() - p1.lastSeen < 30000) return true;
    const p2 = secondaryIdOrName ? this.findPresence(secondaryIdOrName) : null;
    if (p2 && p2.isOnline === true && Date.now() - p2.lastSeen < 30000) return true;
    return false;
  }

  /**
   * Lấy nhãn chữ hiển thị trạng thái online thân thiện chuẩn xác theo thời gian thực
   */
  public getPeerPresenceLabel(idOrName?: string, secondaryIdOrName?: string): string {
    const online = this.isPeerOnline(idOrName, secondaryIdOrName);
    if (online) return '🟢 Đang hoạt động';
    const p = (idOrName ? this.findPresence(idOrName) : null) || (secondaryIdOrName ? this.findPresence(secondaryIdOrName) : null);
    if (p && p.lastSeen) {
      const diffSec = Math.floor((Date.now() - p.lastSeen) / 1000);
      if (diffSec < 30) return '🟢 Đang hoạt động';
      if (diffSec < 60) return '⚪ Vừa mới truy cập';
      const mins = Math.floor(diffSec / 60);
      if (mins < 60) return `⚪ Hoạt động ${mins} phút trước`;
      const hours = Math.floor(mins / 60);
      return `⚪ Hoạt động ${hours} giờ trước`;
    }
    return '⚪ Ngoại tuyến';
  }

  // ================================================================
  // PUBLIC APIS: MESSAGING
  // ================================================================

  /**
   * Gửi tin nhắn mới từ Buyer hoặc Seller
   */
  public async sendMessage(params: {
    storeId: string;
    storeName: string;
    buyerId: string;
    buyerName: string;
    buyerAvatar?: string;
    senderRole: 'buyer' | 'seller';
    senderId: string;
    senderName: string;
    senderAvatar?: string;
    receiverRole: 'buyer' | 'seller';
    receiverId: string;
    receiverName: string;
    content: string;
    productContext?: ChatProductContext;
  }): Promise<ChatMessage> {
    const conversationId = `${params.storeId}__${params.buyerId}`;
    const newMsg: ChatMessage = {
      id: `msg_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`,
      conversationId,
      ...params,
      timestamp: Date.now(),
      isRead: false
    };

    // 1. Lưu ngay vào local state & hiển thị tức thì trên giao diện
    this.storeMessageLocally(newMsg);

    // 2. Phát tán ngay sang các tab khác qua BroadcastChannel (0ms latency)
    this.broadcastLocal({ type: 'CHAT_MESSAGE', payload: newMsg });

    // 3. Đồng bộ lên Server Backend C# và SSE
    try {
      const res = await apiFetch('/api/chat/messages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newMsg)
      });
      if (res.ok) {
        const data = await res.json();
        if (data?.message?.id) {
          newMsg.id = data.message.id;
        }
      }
    } catch (e) {
      console.warn('[SellerChatService] Send to server fallback to local:', e);
    }

    return newMsg;
  }

  /**
   * Kéo danh sách tin nhắn của một cuộc hội thoại từ Server
   */
  public async fetchMessages(conversationId: string): Promise<ChatMessage[]> {
    if (!conversationId) return [];

    // Ưu tiên trả về dữ liệu cache trước
    const cached = this.messagesByConv[conversationId] || [];

    try {
      const res = await apiFetch(`/api/chat/messages?conversationId=${encodeURIComponent(conversationId)}`);
      if (res.ok) {
        const data = await res.json();
        if (data?.messages && Array.isArray(data.messages)) {
          this.messagesByConv[conversationId] = data.messages;
          this.saveToLocalStorage();
          return data.messages;
        }
      }
    } catch {}

    return cached;
  }

  /**
   * Kéo danh sách hội thoại của người bán hoặc người mua
   */
  public async fetchConversations(query: { storeId?: string; buyerId?: string }): Promise<ConversationItem[]> {
    try {
      const params = new URLSearchParams();
      if (query.storeId) params.append('storeId', query.storeId);
      if (query.buyerId) params.append('buyerId', query.buyerId);

      const res = await apiFetch(`/api/chat/conversations?${params.toString()}`);
      if (res.ok) {
        const data = await res.json();
        if (data?.conversations && Array.isArray(data.conversations)) {
          this.conversations.splice(0, this.conversations.length, ...data.conversations);
          this.saveToLocalStorage();
          return this.conversations;
        }
      }
    } catch {}

    return this.conversations;
  }

  /**
   * Đánh dấu cuộc hội thoại đã đọc
   */
  public markAsRead(conversationId: string, readerRole: 'buyer' | 'seller') {
    if (this.messagesByConv[conversationId]) {
      this.messagesByConv[conversationId].forEach((m) => {
        if (m.receiverRole === readerRole) m.isRead = true;
      });
    }

    const conv = this.conversations.find((c) => c.conversationId === conversationId);
    if (conv) conv.unreadCount = 0;

    this.saveToLocalStorage();
    this.broadcastLocal({ type: 'CHAT_READ', payload: { conversationId, readerRole } });

    apiFetch('/api/chat/read', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ conversationId, readerRole })
    }).catch(() => {});
  }

  /**
   * Gửi trạng thái đang gõ phím
   */
  public sendTyping(conversationId: string, role: 'buyer' | 'seller', name: string, isTyping: boolean) {
    const payload = { conversationId, role, name, isTyping };
    this.broadcastLocal({ type: 'CHAT_TYPING', payload });

    apiFetch('/api/chat/typing', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }).catch(() => {});
  }

  // ================================================================
  // SUBSCRIPTIONS
  // ================================================================

  public onMessage(cb: ChatMessageCallback) {
    this.messageListeners.push(cb);
    return () => {
      this.messageListeners = this.messageListeners.filter((c) => c !== cb);
    };
  }

  public onPresence(cb: PresenceCallback) {
    this.presenceListeners.push(cb);
    return () => {
      this.presenceListeners = this.presenceListeners.filter((c) => c !== cb);
    };
  }

  public onTyping(cb: TypingCallback) {
    this.typingListeners.push(cb);
    return () => {
      this.typingListeners = this.typingListeners.filter((c) => c !== cb);
    };
  }
}

export const sellerChatService = new SellerChatService();
