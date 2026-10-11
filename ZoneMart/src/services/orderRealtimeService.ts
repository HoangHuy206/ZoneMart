/**
 * ================================================================
 * ORDER REALTIME & ROUTING SERVICE (ZoneMart)
 * ================================================================
 * Quản lý luồng đặt hàng, phát tán sự kiện thời gian thực (Real-time),
 * kiểm tra bán kính nổ đơn 5km (Haversine) và định vị tuyến đường
 * ngắn nhất (OSRM Driving API) cho Leaflet Map.
 */

export interface OrderLocation {
  lat: number;
  lng: number;
  address?: string;
  name?: string;
  phone?: string;
}

export interface OrderItemDetail {
  id?: string;
  productId?: string;
  name: string;
  price: number;
  quantity: number;
  image?: string;
  shop?: string;
}

export interface RealtimeOrder {
  id: string;
  orderCode?: string;
  total: number;
  shippingFee: number;
  paymentMethod: string;
  createdAt: string;
  status: "pending" | "confirmed" | "picking" | "delivering" | "completed" | "cancelled";
  statusText?: string;
  customer: OrderLocation;
  store: OrderLocation;
  items: OrderItemDetail[];
  shipperInfo?: {
    name: string;
    phone: string;
    licensePlate: string;
    shipperCode?: string;
    avatarUrl?: string;
  };
}

export type OrderEventCallback = (order: RealtimeOrder) => void;
export type StatusEventCallback = (payload: { orderId: string; status: string; statusText: string; order: RealtimeOrder }) => void;

class OrderRealtimeService {
  private channel: BroadcastChannel | null = null;
  private sseSource: EventSource | null = null;
  private orderCreatedListeners: OrderEventCallback[] = [];
  private orderStatusListeners: StatusEventCallback[] = [];
  private orderDispatchListeners: OrderEventCallback[] = [];
  private ordersClearedListeners: (() => void)[] = [];
  private cachedOrdersMap = new Map<string, RealtimeOrder>();
  private audioCtx: AudioContext | null = null;
  private readonly CHANNEL_NAME = "zonemart_order_realtime_channel";
  private readonly STORAGE_KEY = "zonemart_active_delivery_order";

  constructor() {
    this.initChannel();
    this.initStorageListener();
    this.initServerSync();
  }

  /**
   * Kết nối Server-Sent Events (SSE) và tự động đồng bộ xuyên trình duyệt (Chrome, Cốc Cốc, Mobile)
   */
  private initServerSync() {
    if (typeof window === "undefined") return;

    // 1. Kết nối SSE để nhận sự kiện tức thì
    if ("EventSource" in window) {
      try {
        this.sseSource = new EventSource("/api/realtime/events");
        this.sseSource.onmessage = (event) => {
          try {
            const data = JSON.parse(event.data);
            this.handleIncomingMessage(data, true);
          } catch (e) {}
        };
        this.sseSource.onerror = () => {
          // Tự động thử lại khi server restart
        };
      } catch (e) {}
    }

    // 2. Định kỳ kéo danh sách đơn từ Server (Fall-safe Polling mỗi 1.5s)
    setInterval(() => {
      this.syncOrdersFromServer();
    }, 1500);

    // Kéo ngay lần đầu
    setTimeout(() => {
      this.syncOrdersFromServer();
    }, 200);
  }

  /**
   * Đồng bộ dữ liệu đơn hàng từ server về bộ nhớ và localStorage
   */
  public async syncOrdersFromServer() {
    try {
      const res = await fetch("/api/realtime/orders");
      if (res.ok) {
        const data = await res.json();
        if (data && Array.isArray(data.orders)) {
          data.orders.forEach((ord: RealtimeOrder) => {
            const existing = this.cachedOrdersMap.get(ord.id);
            const isNew = !existing;
            const isStatusChanged = existing && existing.status !== ord.status;

            this.cachedOrdersMap.set(ord.id, ord);

            if (isNew) {
              this.orderCreatedListeners.forEach((cb) => cb(ord));
            } else if (isStatusChanged) {
              this.orderStatusListeners.forEach((cb) =>
                cb({
                  orderId: ord.id,
                  status: ord.status,
                  statusText: ord.statusText || "",
                  order: ord
                })
              );
              if (ord.status === "confirmed") {
                this.orderDispatchListeners.forEach((cb) => cb(ord));
              }
            }
          });
        }
      }
    } catch (e) {}
  }

  /**
   * Khởi tạo BroadcastChannel hỗ trợ đồng bộ đa tab tức thì (0ms latency)
   */
  private initChannel() {
    try {
      if (typeof window !== "undefined" && "BroadcastChannel" in window) {
        this.channel = new BroadcastChannel(this.CHANNEL_NAME);
        this.channel.onmessage = (event) => {
          this.handleIncomingMessage(event.data);
        };
      }
    } catch (e) {
      console.warn("[OrderRealtimeService] BroadcastChannel init error:", e);
    }
  }

  /**
   * Lắng nghe StorageEvent dự phòng cho các trường hợp trình duyệt không hỗ trợ BroadcastChannel
   */
  private initStorageListener() {
    if (typeof window === "undefined") return;
    window.addEventListener("storage", (e) => {
      if (e.key === "zonemart_order_bus_event" && e.newValue) {
        try {
          const payload = JSON.parse(e.newValue);
          this.handleIncomingMessage(payload);
        } catch (err) {}
      }
    });
  }

  /**
   * Xử lý gói tin nhận được từ các tab/cửa sổ khác hoặc Server SSE
   */
  private handleIncomingMessage(data: any, _fromServer = false) {
    if (!data || !data.type) return;

    if (data.type === "ORDERS_CLEARED") {
      this.cachedOrdersMap.clear();
      this.clearActiveOrder();
      this.ordersClearedListeners.forEach((cb) => cb());
    } else if (data.type === "ORDER_CREATED" && data.order) {
      this.cachedOrdersMap.set(data.order.id, data.order);
      this.orderCreatedListeners.forEach((cb) => cb(data.order));
    } else if (data.type === "ORDER_STATUS_CHANGED" && data.payload) {
      const p = data.payload;
      if (p.order) this.cachedOrdersMap.set(p.order.id, p.order);
      this.orderStatusListeners.forEach((cb) => cb(p));
      this.updateUserOrdersHistory(p.order || { id: p.orderId, status: p.status, statusText: p.statusText });
    } else if (data.type === "ORDER_DISPATCH_READY" && data.order) {
      this.cachedOrdersMap.set(data.order.id, data.order);
      this.orderDispatchListeners.forEach((cb) => cb(data.order));
    }
  }

  /**
   * Lắng nghe sự kiện toàn bộ đơn hàng bị xóa sạch
   */
  public onOrdersCleared(callback: () => void) {
    this.ordersClearedListeners.push(callback);
    return () => {
      this.ordersClearedListeners = this.ordersClearedListeners.filter((c) => c !== callback);
    };
  }

  /**
   * Gọi API xóa sạch toàn bộ đơn trên server & broadcast cho mọi client
   */
  public async clearAllOrders(): Promise<boolean> {
    try {
      this.cachedOrdersMap.clear();
      this.clearActiveOrder();
      const res = await fetch("/api/realtime/orders/clear", { method: "POST" });
      this.broadcast({ type: "ORDERS_CLEARED" });
      return res.ok;
    } catch (e) {
      this.broadcast({ type: "ORDERS_CLEARED" });
      return false;
    }
  }

  /**
   * Phát tán gói tin đi tới các tab khác và nội bộ tab hiện tại
   */
  private broadcast(data: any) {
    // 1. BroadcastChannel (0ms đa tab)
    if (this.channel) {
      try {
        this.channel.postMessage(data);
      } catch (e) {}
    }

    // 2. Storage event fallback
    try {
      localStorage.setItem("zonemart_order_bus_event", JSON.stringify({ ...data, _ts: Date.now() }));
    } catch (e) {}

    // 3. Xử lý trực tiếp cho tab hiện tại
    this.handleIncomingMessage(data);
  }

  /**
   * Tính khoảng cách chuẩn mặt cầu Trái Đất (Haversine Formula) tính bằng km
   */
  public calculateDistanceKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371; // Bán kính Trái Đất (km)
    const dLat = ((lat2 - lat1) * Math.PI) / 180;
    const dLon = ((lon2 - lon1) * Math.PI) / 180;
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos((lat1 * Math.PI) / 180) *
        Math.cos((lat2 * Math.PI) / 180) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return Number((R * c).toFixed(2));
  }

  /**
   * Tìm con đường ngắn nhất thực tế qua OSRM Driving Engine
   * Nếu có lỗi mạng hoặc offline, tự động fallback trả về đường thẳng nối 2 điểm
   */
  public async fetchShortestRoute(
    fromLat: number,
    fromLng: number,
    toLat: number,
    toLng: number
  ): Promise<[number, number][]> {
    try {
      const url = `https://router.project-osrm.org/route/v1/driving/${fromLng},${fromLat};${toLng},${toLat}?overview=full&geometries=geojson`;
      const res = await fetch(url, { signal: AbortSignal.timeout(3500) });
      if (res.ok) {
        const data = await res.json();
        if (data.code === "Ok" && data.routes && data.routes.length > 0) {
          // OSRM trả về mảng [lng, lat] -> Đổi lại thành [lat, lng] cho Leaflet
          const coordinates = data.routes[0].geometry.coordinates;
          return coordinates.map((coord: [number, number]) => [coord[1], coord[0]]);
        }
      }
    } catch (e) {
      console.warn("[OrderRealtimeService] OSRM router fallback to straight line:", e);
    }

    // Fallback: Tạo đường nối trực tiếp với 1 điểm uốn ở giữa để bản đồ vẫn mượt mà
    return [
      [fromLat, fromLng],
      [(fromLat + toLat) / 2, (fromLng + toLng) / 2],
      [toLat, toLng]
    ];
  }

  /**
   * Phát âm thanh nổ đơn hàng hỏa tốc (Web Audio API không phụ thuộc file mp3 ngoài)
   * Tạo âm thanh chuông xe máy / còi báo nổ đơn điện tử dồn dập
   */
  public playOrderAlertSound() {
    try {
      const AudioCtxClass = window.AudioContext || (window as any).webkitAudioContext;
      if (!AudioCtxClass) return;
      if (!this.audioCtx) {
        this.audioCtx = new AudioCtxClass();
      }
      if (this.audioCtx.state === "suspended") {
        this.audioCtx.resume();
      }

      const now = this.audioCtx.currentTime;
      // Chuỗi nốt báo động nổ đơn dồn dập 3 tiếng
      const notes = [
        { freq: 880, start: 0.0, dur: 0.15 },
        { freq: 1174, start: 0.16, dur: 0.15 },
        { freq: 1760, start: 0.32, dur: 0.3 },
        { freq: 880, start: 0.7, dur: 0.15 },
        { freq: 1174, start: 0.86, dur: 0.15 },
        { freq: 1760, start: 1.02, dur: 0.35 }
      ];

      notes.forEach((n) => {
        const osc = this.audioCtx!.createOscillator();
        const gain = this.audioCtx!.createGain();
        osc.type = "sine";
        osc.frequency.setValueAtTime(n.freq, now + n.start);

        gain.gain.setValueAtTime(0.3, now + n.start);
        gain.gain.exponentialRampToValueAtTime(0.001, now + n.start + n.dur);

        osc.connect(gain);
        gain.connect(this.audioCtx!.destination);
        osc.start(now + n.start);
        osc.stop(now + n.start + n.dur);
      });
    } catch (e) {
      console.warn("Lỗi phát âm thanh chuông nổ đơn:", e);
    }
  }

  /**
   * Phát chuông báo giao hàng thành công
   */
  public playSuccessChime() {
    try {
      const AudioCtxClass = window.AudioContext || (window as any).webkitAudioContext;
      if (!AudioCtxClass) return;
      if (!this.audioCtx) this.audioCtx = new AudioCtxClass();
      if (this.audioCtx.state === "suspended") this.audioCtx.resume();

      const now = this.audioCtx.currentTime;
      const notes = [
        { freq: 523.25, time: 0, dur: 0.12 },
        { freq: 659.25, time: 0.13, dur: 0.12 },
        { freq: 783.99, time: 0.26, dur: 0.15 },
        { freq: 1046.5, time: 0.42, dur: 0.35 }
      ];

      notes.forEach((n) => {
        const osc = this.audioCtx!.createOscillator();
        const gain = this.audioCtx!.createGain();
        osc.type = "triangle";
        osc.frequency.setValueAtTime(n.freq, now + n.time);
        gain.gain.setValueAtTime(0.25, now + n.time);
        gain.gain.exponentialRampToValueAtTime(0.001, now + n.time + n.dur);
        osc.connect(gain);
        gain.connect(this.audioCtx!.destination);
        osc.start(now + n.time);
        osc.stop(now + n.time + n.dur);
      });
    } catch (e) {}
  }

  /**
   * 1. NGƯỜI MUA: Phát đơn hàng mới sau khi đặt hàng / thanh toán
   */
  public dispatchNewOrder(order: RealtimeOrder) {
    if (!order.status) order.status = "pending";
    if (!order.statusText) order.statusText = "Chờ quán xác nhận";
    this.cachedOrdersMap.set(order.id, order);
    this.saveActiveOrder(order);
    this.updateUserOrdersHistory(order);

    // Đồng bộ lên Server (0ms xuyên trình duyệt)
    try {
      fetch("/api/realtime/orders", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(order)
      }).catch(() => {});
    } catch (e) {}

    this.broadcast({
      type: "ORDER_CREATED",
      order
    });
  }

  /**
   * 2. NGƯỜI BÁN: Xác nhận đơn hàng -> Chuyển sang tìm tài xế trong bán kính 5km
   */
  public sellerConfirmOrder(orderId: string): RealtimeOrder | null {
    let order = this.cachedOrdersMap.get(orderId) || this.getActiveOrder();
    if (!order) {
      order = {
        id: orderId,
        orderCode: orderId,
        total: 40000,
        shippingFee: 5000,
        paymentMethod: "Tiền mặt COD",
        createdAt: "Vừa xong",
        status: "confirmed",
        statusText: "Đã xác nhận • Đang tìm tài xế",
        customer: { lat: 21.0333, lng: 105.7983, name: "Khách hàng ZoneMart", address: "Hà Nội" },
        store: { lat: 21.0345, lng: 105.7960, name: "test Gian Hàng", address: "Cầu Giấy, Hà Nội" },
        items: []
      };
    }

    order.status = "confirmed";
    order.statusText = "Đã xác nhận • Đang tìm tài xế";

    // Tự động đồng bộ vị trí & tọa độ thực tế của Gian Hàng nếu có
    try {
      if (typeof localStorage !== "undefined") {
        const currentUserStr = localStorage.getItem("currentUser");
        if (currentUserStr) {
          const u = JSON.parse(currentUserStr);
          const emailKey = (u.phoneEmail || u.email || "").toLowerCase().trim();
          if (emailKey) {
            const storeStr = localStorage.getItem(`zonemart_seller_store_${emailKey}`);
            if (storeStr) {
              const s = JSON.parse(storeStr);
              if (s.lat && s.lng) {
                order.store.lat = s.lat;
                order.store.lng = s.lng;
                if (s.name) order.store.name = s.name;
                if (s.address) order.store.address = s.address;
                if (s.phone) order.store.phone = s.phone;
              }
            }
          }
        }
      }
    } catch (e) {}

    this.cachedOrdersMap.set(orderId, order);
    this.saveActiveOrder(order);
    this.updateUserOrdersHistory(order);

    // Đồng bộ lên Server
    try {
      fetch("/api/realtime/orders", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          orderId,
          status: "confirmed",
          statusText: "Đã xác nhận • Đang tìm tài xế"
        })
      }).catch(() => {});
    } catch (e) {}

    this.broadcast({
      type: "ORDER_STATUS_CHANGED",
      payload: {
        orderId,
        status: "confirmed",
        statusText: "Đã xác nhận • Đang tìm tài xế",
        order
      }
    });

    // Phát tín hiệu nổ đơn cho các tài xế trong bán kính 5km
    this.broadcast({
      type: "ORDER_DISPATCH_READY",
      order
    });

    return order;
  }

  /**
   * 2b. NGƯỜI BÁN: Hủy bỏ / từ chối đơn hàng
   */
  public sellerCancelOrder(orderId: string, reason = "Quán bận, từ chối nhận đơn"): RealtimeOrder | null {
    let order = this.cachedOrdersMap.get(orderId) || this.getActiveOrder();
    if (order) {
      order.status = "cancelled";
      order.statusText = reason;
      this.cachedOrdersMap.set(orderId, order);
      this.saveActiveOrder(order);
      this.updateUserOrdersHistory(order);

      try {
        fetch("/api/realtime/orders", {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            orderId,
            status: "cancelled",
            statusText: reason
          })
        }).catch(() => {});
      } catch (e) {}

      this.broadcast({
        type: "ORDER_STATUS_CHANGED",
        payload: {
          orderId,
          status: "cancelled",
          statusText: reason,
          order
        }
      });
    }
    return order;
  }

  /**
   * 3. TÀI XẾ: Đồng ý nhận đơn -> Định vị đi đến Quán lấy hàng
   */
  public acceptOrder(orderId: string, shipperInfo: any): RealtimeOrder | null {
    let active = this.cachedOrdersMap.get(orderId) || this.getActiveOrder();
    if (!active) {
      active = {
        id: orderId,
        orderCode: orderId,
        total: 40000,
        shippingFee: 5000,
        paymentMethod: "Tiền mặt COD",
        createdAt: "Vừa xong",
        status: "picking",
        statusText: "Tài xế đang đến quán lấy hàng",
        customer: { lat: 21.0333, lng: 105.7983, name: "Khách hàng ZoneMart", address: "Hà Nội" },
        store: { lat: 21.0345, lng: 105.7960, name: "test Gian Hàng", address: "Cầu Giấy, Hà Nội" },
        items: []
      };
    }

    active.status = "picking";
    active.statusText = "Tài xế đang đến quán lấy hàng";
    active.shipperInfo = {
      name: shipperInfo.fullName || shipperInfo.name || "Tài xế ZoneMart",
      phone: shipperInfo.phoneEmail || shipperInfo.phone || "0912 888 999",
      licensePlate: shipperInfo.licensePlate || "29N1-67890",
      shipperCode: shipperInfo.shipperCode || "ZM-DRV-01",
      avatarUrl: shipperInfo.avatarUrl || ""
    };

    this.cachedOrdersMap.set(orderId, active);
    this.saveActiveOrder(active);
    this.updateUserOrdersHistory(active);

    // Đồng bộ lên Server
    try {
      fetch("/api/realtime/orders", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          orderId,
          status: "picking",
          statusText: "Tài xế đang đến quán lấy hàng",
          shipperInfo: active.shipperInfo
        })
      }).catch(() => {});
    } catch (e) {}

    this.broadcast({
      type: "ORDER_STATUS_CHANGED",
      payload: {
        orderId,
        status: "picking",
        statusText: "Tài xế đang đến quán lấy hàng",
        order: active
      }
    });

    return active;
  }

  /**
   * 4. TÀI XẾ: Đã lấy đơn hàng thành công tại quán -> Định vị chuyển qua Người Dùng
   */
  public confirmOrderPicked(orderId: string): RealtimeOrder | null {
    let active = this.cachedOrdersMap.get(orderId) || this.getActiveOrder();
    if (!active) return null;

    active.status = "delivering";
    active.statusText = "Tài xế đang tới vị trí của bạn";

    this.cachedOrdersMap.set(orderId, active);
    this.saveActiveOrder(active);
    this.updateUserOrdersHistory(active);

    // Đồng bộ lên Server
    try {
      fetch("/api/realtime/orders", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          orderId,
          status: "delivering",
          statusText: "Tài xế đang tới vị trí của bạn"
        })
      }).catch(() => {});
    } catch (e) {}

    this.broadcast({
      type: "ORDER_STATUS_CHANGED",
      payload: {
        orderId,
        status: "delivering",
        statusText: "Tài xế đang tới vị trí của bạn",
        order: active
      }
    });

    return active;
  }

  /**
   * 5. TÀI XẾ: Đã giao thành công tới khách hàng -> Cập nhật Giao thành công cả 2 bên
   */
  public confirmOrderDelivered(orderId: string): RealtimeOrder | null {
    let active = this.cachedOrdersMap.get(orderId) || this.getActiveOrder();
    if (!active) return null;

    active.status = "completed";
    active.statusText = "Giao thành công";

    this.cachedOrdersMap.set(orderId, active);
    this.saveActiveOrder(active);
    this.updateUserOrdersHistory(active);
    this.playSuccessChime();

    // Đồng bộ lên Server
    try {
      fetch("/api/realtime/orders", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          orderId,
          status: "completed",
          statusText: "Giao thành công"
        })
      }).catch(() => {});
    } catch (e) {}

    this.broadcast({
      type: "ORDER_STATUS_CHANGED",
      payload: {
        orderId,
        status: "completed",
        statusText: "Giao thành công",
        order: active
      }
    });

    return active;
  }

  /**
   * Lắng nghe sự kiện nổ đơn mới (cho Seller & Shipper)
   */
  public onOrderCreated(callback: OrderEventCallback) {
    this.orderCreatedListeners.push(callback);
    return () => {
      this.orderCreatedListeners = this.orderCreatedListeners.filter((c) => c !== callback);
    };
  }

  /**
   * Lắng nghe sự kiện quán đã xác nhận, sẵn sàng điều phối tài xế (cho Shipper)
   */
  public onOrderDispatchReady(callback: OrderEventCallback) {
    this.orderDispatchListeners.push(callback);
    return () => {
      this.orderDispatchListeners = this.orderDispatchListeners.filter((c) => c !== callback);
    };
  }

  /**
   * Lắng nghe sự kiện cập nhật trạng thái đơn (cho Buyer và Seller)
   */
  public onOrderStatusChanged(callback: StatusEventCallback) {
    this.orderStatusListeners.push(callback);
    return () => {
      this.orderStatusListeners = this.orderStatusListeners.filter((c) => c !== callback);
    };
  }

  /**
   * Lấy toàn bộ danh sách đơn hàng đã đồng bộ
   */
  public getAllOrders(): RealtimeOrder[] {
    return Array.from(this.cachedOrdersMap.values());
  }

  /**
   * Lưu trữ đơn hàng đang giao hiện hành
   */
  public saveActiveOrder(order: RealtimeOrder) {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(order));
    } catch (e) {}
  }

  public getActiveOrder(): RealtimeOrder | null {
    try {
      const raw = localStorage.getItem(this.STORAGE_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch (e) {
      return null;
    }
  }

  public clearActiveOrder() {
    try {
      localStorage.removeItem(this.STORAGE_KEY);
    } catch (e) {}
  }

  /**
   * Cập nhật đồng bộ vào các key danh sách đơn hàng của người mua (Buyer) trong localStorage
   */
  private updateUserOrdersHistory(order: RealtimeOrder) {
    try {
      for (let i = 0; i < localStorage.length; i++) {
        const k = localStorage.key(i);
        if (k && k.startsWith("zonemart_profile_orders_")) {
          const raw = localStorage.getItem(k);
          if (raw) {
            const list = JSON.parse(raw);
            if (Array.isArray(list)) {
              let updated = false;
              const newList = list.map((item: any) => {
                if (item.id === order.id || item.orderId === order.id) {
                  updated = true;
                  return {
                    ...item,
                    status: order.status,
                    statusText: order.statusText,
                    shipperInfo: order.shipperInfo
                  };
                }
                return item;
              });
              if (updated) {
                localStorage.setItem(k, JSON.stringify(newList));
              }
            }
          }
        }
      }

      // Kích hoạt sự kiện để BuyerOrdersView tự động cập nhật
      window.dispatchEvent(new CustomEvent("zonemart:refresh_buyer_orders"));
    } catch (e) {}
  }
}

export const orderRealtimeService = new OrderRealtimeService();

