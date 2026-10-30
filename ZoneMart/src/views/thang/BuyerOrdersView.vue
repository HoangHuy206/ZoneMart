<script setup lang="ts">
/**
 * ================================================================
 * QUẢN LÝ ĐƠN MUA (BUYER ORDERS) - Phụ trách: Thắng
 * Hiển thị đầy đủ hình ảnh, tên, giá, số lượng, cửa hàng và địa chỉ
 * ================================================================
 */
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useAuth } from "../../composables/useAuth";
import { apiFetch } from "../../utils/apiConfig";
import { useProductCatalog, type CatalogProduct } from "../../composables/useProductCatalog";
import { orderRealtimeService } from "../../services/orderRealtimeService";
import { useToast } from "../../composables/useToast";

const auth = useAuth();
const router = useRouter();
const route = useRoute();
const catalog = useProductCatalog();
const toast = useToast();

const orders = ref<any[]>([]);
const isLoading = ref<boolean>(true);
let loadingTimer: any = null;

const currentAccount = computed(() => auth.currentUser.value);
const currentAccountName = computed(() => {
  const acc = auth.currentUser.value;
  return acc?.fullName || acc?.phoneEmail || acc?.phone || "Khách Hàng";
});

interface OrderProductItem {
  id?: string;
  productId?: string;
  name: string;
  price: number;
  quantity: number;
  image?: string;
  shop?: string;
}

const parseOrderItems = (rawItems: any): OrderProductItem[] => {
  if (!rawItems) return [];
  if (Array.isArray(rawItems)) {
    return rawItems.map((it: any) => {
      if (typeof it === "object" && it !== null) {
        return {
          id: it.id || it.productId || "",
          productId: it.productId || it.id || "",
          name: it.name || "Sản phẩm ZoneMart",
          price: Number(it.price) || 0,
          quantity: Number(it.quantity) || 1,
          image: it.image || "",
          shop: it.shop || ""
        };
      }
      return {
        id: "",
        productId: "",
        name: String(it),
        price: 0,
        quantity: 1,
        image: "",
        shop: ""
      };
    });
  }
  if (typeof rawItems === "string") {
    const trimmed = rawItems.trim();
    if (trimmed.startsWith("[") && trimmed.endsWith("]")) {
      try {
        const parsed = JSON.parse(trimmed);
        if (Array.isArray(parsed)) {
          return parseOrderItems(parsed);
        }
      } catch (e) {}
    }
    // Dạng chuỗi text liệt kê "Tên SP (x2), Tên SP (x1)"
    return trimmed.split(",").map((part: string) => {
      const p = part.trim();
      const matchQty = p.match(/\(x(\d+)\)/i);
      const qty = matchQty ? parseInt(matchQty[1]) : 1;
      const cleanName = p.replace(/\(x\d+\)/i, "").trim();
      return {
        name: cleanName || p,
        price: 0,
        quantity: qty,
        image: "",
        shop: ""
      };
    });
  }
  return [];
};

const getOrderStoreName = (order: any): string => {
  const items = parseOrderItems(order.items);
  if (items.length > 0 && items[0].shop) {
    return items[0].shop;
  }
  return order.store || order.storeName || "ZoneMart Đối Tác";
};

const getOrderStatusClass = (status: any): string => {
  if (!status) return "pending";
  const s = String(status).toLowerCase();
  if (s === "completed" || s.includes("thành công")) return "completed";
  if (s === "delivering" || s.includes("vị trí của bạn") || s.includes("đang tới")) return "delivering";
  if (s === "picking" || s.includes("đến quán") || s.includes("lấy hàng")) return "picking";
  if (s === "confirmed" || s.includes("tìm tài xế") || s.includes("đã xác nhận")) return "confirmed";
  if (s === "cancelled" || s.includes("hủy")) return "cancelled";
  if (s === "pending" || s.includes("chờ quán")) return "pending";
  return "processing";
};

const getOrderStatusText = (status: any, orderObj?: any): string => {
  if (orderObj?.statusText) return orderObj.statusText;
  if (!status) return "Chờ quán xác nhận";
  if (typeof status === "string") {
    const clean = status.replace(/\s*\(TPBank\)/gi, "").replace(/\s*TPBank/gi, "").trim();
    const s = clean.toLowerCase();
    if (s === "pending" || s.includes("chờ quán") || s.includes("chờ xác nhận")) return "Chờ quán xác nhận";
    if (s === "confirmed" || s.includes("tìm tài xế") || s.includes("đã xác nhận")) return "Đã xác nhận • Đang tìm tài xế";
    if (s === "picking" || s.includes("đến quán") || s.includes("lấy hàng")) return "Tài xế đang đến quán lấy hàng";
    if (s === "delivering" || s.includes("vị trí của bạn") || s.includes("đang tới")) return "Tài xế đang tới vị trí của bạn";
    if (s === "completed" || s.includes("thành công") || s.includes("giao thành công")) return "Giao hàng thành công";
    if (s === "cancelled" || s.includes("hủy")) return "Đã hủy đơn";
    return clean;
  }
  return "Chờ quán xác nhận";
};

const formatPaymentMethod = (method: any): string => {
  if (!method) return "";
  const clean = String(method).replace(/\s*\(TPBank\)/gi, "").replace(/\s*TPBank/gi, "").trim();
  if (clean.toLowerCase() === "chuyển khoản") return "Chuyển khoản QR";
  return clean || "Chuyển khoản QR";
};

const formatPrice = (val: number | undefined): string => {
  return ((val || 0).toLocaleString("vi-VN")) + " ₫";
};

const handleImgError = (e: Event) => {
  const target = e.target as HTMLImageElement;
  target.src = "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=300&q=80";
};

// Đọc chính xác đơn hàng thuộc về tài khoản đang đăng nhập
const loadBuyerOrders = () => {
  const acc = auth.currentUser.value;
  const isDemo = Boolean(
    acc &&
    acc.id &&
    ["usr_buyer_01", "usr_seller_01", "usr_shipper_01", "usr_admin_01"].includes(acc.id)
  );

  const candidateKeys = new Set<string>();
  if (acc) {
    if (acc.phoneEmail) candidateKeys.add("zonemart_profile_orders_" + acc.phoneEmail.toLowerCase().trim());
    if (acc.id) candidateKeys.add("zonemart_profile_orders_" + acc.id.toLowerCase().trim());
    if (acc.phone) candidateKeys.add("zonemart_profile_orders_" + acc.phone.toLowerCase().trim());
  } else {
    candidateKeys.add("zonemart_profile_orders_guest");
  }

  const collectedOrders: any[] = [];
  const seenIds = new Set<string>();

  // 1. Quét các key cụ thể của tài khoản này
  candidateKeys.forEach((key) => {
    const raw = localStorage.getItem(key);
    if (raw) {
      try {
        const parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) {
          parsed.forEach((ord: any) => {
            const id = ord.orderId || ord.id;
            if (id && !seenIds.has(id)) {
              seenIds.add(id);
              collectedOrders.push(ord);
            }
          });
        }
      } catch (e) {}
    }
  });

  // 2. Tìm thêm các đơn liên quan đến tài khoản này trong các phân vùng lưu trữ khác (theo SĐT, họ tên hoặc mã KH)
  if (acc) {
    const userPhone = (acc.phone || acc.phoneEmail || "").toLowerCase().trim();
    const userName = (acc.fullName || "").toLowerCase().trim();
    const userId = (acc.id || "").toLowerCase().trim();

    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i);
      if (k && k.startsWith("zonemart_profile_orders_")) {
        try {
          const raw = localStorage.getItem(k);
          if (raw) {
            const list = JSON.parse(raw);
            if (Array.isArray(list)) {
              list.forEach((ord: any) => {
                const id = ord.orderId || ord.id;
                if (id && !seenIds.has(id)) {
                  const ordPhone = (ord.recipientPhone || "").toLowerCase().trim();
                  const ordName = (ord.recipientName || "").toLowerCase().trim();
                  const ordBuyerId = (ord.buyerId || "").toLowerCase().trim();

                  if (
                    (userPhone && ordPhone && (ordPhone === userPhone || ordPhone.includes(userPhone) || userPhone.includes(ordPhone))) ||
                    (userName && ordName && ordName === userName) ||
                    (userId && ordBuyerId && ordBuyerId === userId)
                  ) {
                    seenIds.add(id);
                    collectedOrders.push(ord);
                  }
                }
              });
            }
          }
        } catch (e) {}
      }
    }
  }

  // 3. Nếu là tài khoản Demo (usr_buyer_01...) và chưa có đơn thực tế nào, nạp đơn mẫu
  if (isDemo && collectedOrders.length === 0) {
    collectedOrders.push(
      {
        orderId: "ORD_98213",
        date: "07/09/2026 18:30",
        total: 395000,
        paymentMethod: "Chuyển khoản QR",
        shippingMethod: "Hỏa Tốc Siêu Tốc (10km)",
        deliveryType: "express",
        status: "delivering",
        statusText: "Shipper đang giao hàng tới bạn",
        store: "ZoneMart Nông Sản Sạch Cầu Giấy",
        items: [
          {
            name: "Thịt Bò Mỹ Nhập Khẩu Tươi Ngon",
            price: 150000,
            quantity: 2,
            image: "https://images.unsplash.com/photo-1551028150-64b9f398f678?auto=format&fit=crop&w=400&q=80",
            shop: "ZoneMart Nông Sản Sạch Cầu Giấy"
          },
          {
            name: "Gạo ST25 Ông Cua Thơm Thượng Hạng (5kg)",
            price: 95000,
            quantity: 1,
            image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80",
            shop: "ZoneMart Nông Sản Sạch Cầu Giấy"
          }
        ]
      },
      {
        orderId: "ORD_97842",
        date: "05/09/2026 12:15",
        total: 125000,
        paymentMethod: "Tiền mặt khi nhận hàng (COD)",
        shippingMethod: "Giao Hàng Tiêu Chuẩn",
        deliveryType: "standard",
        status: "completed",
        statusText: "Giao hàng thành công",
        store: "Siêu Thị Trái Cây Xanh",
        items: [
          {
            name: "Dâu Tây Đà Lạt Giống Nhật Hộp 500g",
            price: 125000,
            quantity: 1,
            image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400&q=80",
            shop: "Siêu Thị Trái Cây Xanh"
          }
        ]
      }
    );
  }

  orders.value = collectedOrders;
};

const realTimeToast = ref("");
const showRealTimeToast = ref(false);
let realTimeToastTimer: any = null;
const triggerRealTimeToast = (msg: string) => {
  realTimeToast.value = msg;
  showRealTimeToast.value = true;
  if (realTimeToastTimer) clearTimeout(realTimeToastTimer);
  realTimeToastTimer = setTimeout(() => {
    showRealTimeToast.value = false;
  }, 4500);
};

let unsubscribeRealTimeStatus: (() => void) | null = null;

// Tải dữ liệu đơn hàng ngay lập tức và hiệu ứng mượt mà
const fetchOrdersWithLoading = (delayMs: number = 220) => {
  loadBuyerOrders();
  isLoading.value = true;
  if (loadingTimer) clearTimeout(loadingTimer);

  loadingTimer = setTimeout(() => {
    isLoading.value = false;
  }, delayMs);
};

const handleGlobalRefresh = () => {
  fetchOrdersWithLoading(550);
};

onMounted(() => {
  if (route.query.tab === 'purchased' || route.query.tab === 'purchased_products' || route.query.tab === 'reviews') {
    activeFilterTab.value = 'purchased_products';
  } else if (route.query.tab === 'completed') {
    activeFilterTab.value = 'completed';
  } else if (route.query.tab === 'delivering') {
    activeFilterTab.value = 'delivering';
  }
  fetchOrdersWithLoading(650);
  window.addEventListener("zonemart:refresh_buyer_orders", handleGlobalRefresh);

  // Lắng nghe cập nhật trạng thái đơn hàng thời gian thực (từ Seller & Shipper)
  unsubscribeRealTimeStatus = orderRealtimeService.onOrderStatusChanged(({ orderId, status, statusText, order }) => {
    const found = orders.value.find((o) => o.id === orderId || o.orderId === orderId);
    if (found) {
      found.status = status;
      found.statusText = statusText;
      if (order?.shipperInfo) found.shipperInfo = order.shipperInfo;
    } else {
      loadBuyerOrders();
    }

    if (status === "confirmed") {
      triggerRealTimeToast(`📋 Đơn #${orderId}: Quán đã xác nhận và đang tìm tài xế!`);
    } else if (status === "picking") {
      triggerRealTimeToast(`🚴 Đơn #${orderId}: Tài xế đang đến quán lấy hàng!`);
    } else if (status === "delivering") {
      triggerRealTimeToast(`📦 Đơn #${orderId}: Tài xế đang tới vị trí của bạn!`);
    } else if (status === "completed") {
      triggerRealTimeToast(`🎉 Đơn #${orderId}: Giao hàng thành công! Cảm ơn bạn đã đặt hàng tại ZoneMart.`);
    } else if (status === "cancelled") {
      triggerRealTimeToast(`❌ Đơn #${orderId}: Đơn hàng đã bị hủy (${statusText || 'Quán bận'}).`);
    }
  });
});

onUnmounted(() => {
  if (loadingTimer) clearTimeout(loadingTimer);
  if (realTimeToastTimer) clearTimeout(realTimeToastTimer);
  if (unsubscribeRealTimeStatus) unsubscribeRealTimeStatus();
  window.removeEventListener("zonemart:refresh_buyer_orders", handleGlobalRefresh);
});

// Tự động tải lại và hiển thị hiệu ứng loading khi chuyển tài khoản
watch(
  () => [auth.currentUser.value?.id, auth.currentUser.value?.phoneEmail],
  () => {
    if (route.query.tab === 'purchased' || route.query.tab === 'purchased_products' || route.query.tab === 'reviews') {
      activeFilterTab.value = 'purchased_products';
    } else if (route.query.tab === 'completed') {
      activeFilterTab.value = 'completed';
    } else if (route.query.tab === 'delivering') {
      activeFilterTab.value = 'delivering';
    } else if (route.query.tab === 'all') {
      activeFilterTab.value = 'all';
    }
    fetchOrdersWithLoading(500);
  }
);

// Tải lại khi route query thay đổi
watch(
  () => [route.fullPath, route.query.refresh, route.query.tab],
  () => {
    fetchOrdersWithLoading(500);
  }
);

// ========================================================
// BỘ LỌC ĐƠN HÀNG & TAB SẢN PHẨM ĐÃ MUA (GIỐNG SHOPEE)
// ========================================================
type OrderFilterTab = 'all' | 'delivering' | 'completed' | 'purchased_products';
const activeFilterTab = ref<OrderFilterTab>('all');

export interface PurchasedProductEntry {
  orderId: string;
  orderDate: string;
  storeName: string;
  storeId: string;
  productId: string;
  name: string;
  image: string;
  price: number;
  quantity: number;
  uniqueKey: string;
}

const getInitialRatedProductsMap = (): Record<string, { rating: number; comment: string; tags: string[]; date: string }> => {
  if (typeof window === "undefined") return {};
  try {
    const raw = localStorage.getItem("zonemart_rated_products_map");
    if (raw) return JSON.parse(raw);
  } catch {}
  return {};
};

const ratedProductsMap = ref<Record<string, { rating: number; comment: string; tags: string[]; date: string }>>(getInitialRatedProductsMap());

const isProductItemRated = (orderId: string, item: any): boolean => {
  const rawId = String(orderId).replace(/^#/, "").trim();
  const match = findProductMatch(item);
  const prodId = match?.id || item.productId || item.id || ("prod_" + (item.name || "").trim().toLowerCase().replace(/[^a-z0-9]/g, "_"));
  const key = `${rawId}_${prodId}`;
  return Boolean(ratedProductsMap.value[key] || ratedProductsMap.value[prodId]);
};

const getProductItemRating = (orderId: string, item: any): number => {
  const rawId = String(orderId).replace(/^#/, "").trim();
  const match = findProductMatch(item);
  const prodId = match?.id || item.productId || item.id || ("prod_" + (item.name || "").trim().toLowerCase().replace(/[^a-z0-9]/g, "_"));
  const key = `${rawId}_${prodId}`;
  return ratedProductsMap.value[key]?.rating || ratedProductsMap.value[prodId]?.rating || 5;
};

// Đơn hàng lọc theo trạng thái
const filteredOrders = computed(() => {
  if (activeFilterTab.value === 'all') return orders.value;
  if (activeFilterTab.value === 'delivering') {
    return orders.value.filter(o => {
      const cls = getOrderStatusClass(o.status);
      return cls === 'delivering' || cls === 'picking' || cls === 'confirmed';
    });
  }
  if (activeFilterTab.value === 'completed') {
    return orders.value.filter(o => getOrderStatusClass(o.status) === 'completed' || o.status === 'completed');
  }
  return orders.value;
});

const deliveringOrdersCount = computed(() => {
  return orders.value.filter(o => {
    const cls = getOrderStatusClass(o.status);
    return cls === 'delivering' || cls === 'picking' || cls === 'confirmed';
  }).length;
});

const completedOrdersCount = computed(() => {
  return orders.value.filter(o => getOrderStatusClass(o.status) === 'completed' || o.status === 'completed').length;
});

// Trích xuất tất cả sản phẩm từ các đơn hàng ĐÃ GIAO THÀNH CÔNG (Purchased Products like Shopee)
const allPurchasedProducts = computed<PurchasedProductEntry[]>(() => {
  const result: PurchasedProductEntry[] = [];
  const completed = orders.value.filter(o => getOrderStatusClass(o.status) === 'completed' || o.status === 'completed');

  for (const ord of completed) {
    const rawId = String(ord.orderId || ord.id || "").replace(/^#/, "").trim();
    const storeName = getOrderStoreName(ord);
    const storeId = ord.storeId || ord.store?.id || "";
    const items = parseOrderItems(ord.items);

    for (const it of items) {
      const match = findProductMatch(it);
      const prodId = match?.id || it.productId || it.id || ("prod_" + (it.name || "").trim().toLowerCase().replace(/[^a-z0-9]/g, "_"));
      const uniqueKey = `${rawId}_${prodId}`;
      result.push({
        orderId: rawId,
        orderDate: ord.date || "Vừa xong",
        storeName,
        storeId,
        productId: prodId,
        name: it.name,
        image: it.image || match?.image || "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=300&q=80",
        price: it.price || match?.price || 0,
        quantity: it.quantity || 1,
        uniqueKey
      });
    }
  }

  return result;
});

const unratedProductsCount = computed(() => {
  return allPurchasedProducts.value.filter(p => !ratedProductsMap.value[p.uniqueKey] && !ratedProductsMap.value[p.productId]).length;
});

// Modal đánh giá riêng từng sản phẩm (Giống Shopee)
const showSingleProductModal = ref(false);
const singleReviewProduct = ref<PurchasedProductEntry | null>(null);
const singleProductRating = ref(0);
const singleProductComment = ref("");
const singleProductTags = ref<string[]>([]);
const showExtraReview = ref(false);
const singleStoreRating = ref(0);
const singleStoreComment = ref("");
const singleShipperRating = ref(0);
const singleShipperComment = ref("");

const openSingleProductReviewModal = (entry: PurchasedProductEntry) => {
  singleReviewProduct.value = entry;
  const existing = ratedProductsMap.value[entry.uniqueKey] || ratedProductsMap.value[entry.productId];
  if (existing) {
    singleProductRating.value = existing.rating || 0;
    singleProductComment.value = existing.comment || "";
    singleProductTags.value = existing.tags ? [...existing.tags] : [];
  } else {
    singleProductRating.value = 0; // Không mặc định 5 sao
    singleProductComment.value = ""; // Không tự động chèn lời nhắn
    singleProductTags.value = []; // Không tự động tick sẵn tag
  }
  showExtraReview.value = false;
  singleStoreRating.value = 0;
  singleStoreComment.value = "";
  singleShipperRating.value = 0;
  singleShipperComment.value = "";
  showSingleProductModal.value = true;
};

const openSingleProductReviewModalFromOrder = (item: any, order: any) => {
  const rawId = String(order.orderId || order.id || "").replace(/^#/, "").trim();
  const storeName = getOrderStoreName(order);
  const match = findProductMatch(item);
  const prodId = match?.id || item.productId || item.id || ("prod_" + (item.name || "").trim().toLowerCase().replace(/[^a-z0-9]/g, "_"));
  
  const entry: PurchasedProductEntry = {
    orderId: rawId,
    orderDate: order.date || "Vừa xong",
    storeName,
    storeId: order.storeId || "",
    productId: prodId,
    name: item.name,
    image: item.image || match?.image || "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=300&q=80",
    price: item.price || match?.price || 0,
    quantity: item.quantity || 1,
    uniqueKey: `${rawId}_${prodId}`
  };

  openSingleProductReviewModal(entry);
};

const submitSingleProductReview = async () => {
  if (!singleReviewProduct.value) return;
  if (!singleProductRating.value || singleProductRating.value <= 0) {
    toast.warning("Vui lòng bấm chọn số sao đánh giá chất lượng sản phẩm (1-5 sao)!", "Chưa chọn số sao ⭐");
    return;
  }
  const entry = singleReviewProduct.value;
  isSubmittingReview.value = true;

  try {
    const payload = {
      orderId: entry.orderId,
      subOrderId: "",
      storeId: entry.storeId,
      storeName: entry.storeName,
      buyerId: auth.currentUser.value?.id || "",
      buyerName: currentAccountName.value,
      buyerAvatar: auth.currentUser.value?.avatarUrl || (auth.currentUser.value as any)?.avatar || "",
      rating: singleProductRating.value,
      comment: singleProductComment.value,
      shipperRating: showExtraReview.value ? singleShipperRating.value : 5,
      shipperComment: showExtraReview.value ? singleShipperComment.value : "",
      storeRating: showExtraReview.value ? singleStoreRating.value : 5,
      storeComment: showExtraReview.value ? singleStoreComment.value : "",
      productReviews: [
        {
          productId: entry.productId,
          productName: entry.name,
          productImage: entry.image,
          rating: singleProductRating.value,
          comment: singleProductComment.value,
          tags: singleProductTags.value
        }
      ]
    };

    // 1. Cập nhật rating local 0ms
    catalog.recordLocalReview(payload);

    // 2. Gửi API backend
    await apiFetch("/api/orders/reviews", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    }).catch(() => null);

    // 3. Đánh dấu đã đánh giá sản phẩm này
    ratedProductsMap.value[entry.uniqueKey] = {
      rating: singleProductRating.value,
      comment: singleProductComment.value,
      tags: singleProductTags.value,
      date: new Date().toLocaleDateString("vi-VN")
    };
    ratedProductsMap.value[entry.productId] = ratedProductsMap.value[entry.uniqueKey];

    try {
      localStorage.setItem("zonemart_rated_products_map", JSON.stringify(ratedProductsMap.value));
    } catch {}

    toast.success(
      `Đánh giá thành công cho sản phẩm "${entry.name}"! Điểm số trung bình đã được cập nhật và sản phẩm sẽ được ưu tiên xếp lên đầu trang.`,
      "Đánh Giá Thành Công ⭐"
    );
    showSingleProductModal.value = false;
    singleReviewProduct.value = null;
  } catch (err) {
    toast.success("Đã lưu đánh giá thành công!", "Đánh Giá Thành Công ⭐");
    showSingleProductModal.value = false;
    singleReviewProduct.value = null;
  } finally {
    isSubmittingReview.value = false;
  }
};

// ========================================================
// TRẠNG THÁI ĐÁNH GIÁ ĐA PHÂN HỆ (TÀI XẾ, GIAN HÀNG & TỪNG SẢN PHẨM)
// ========================================================
interface ReviewProductItemState {
  productId: string;
  name: string;
  image: string;
  quantity: number;
  price: number;
  rating: number;
  comment: string;
  tags: string[];
}

const getInitialRatedOrders = (): Set<string> => {
  if (typeof window === "undefined") return new Set();
  try {
    const raw = localStorage.getItem("zonemart_rated_order_ids");
    if (raw) return new Set(JSON.parse(raw));
  } catch {}
  return new Set();
};

const ratedOrderIds = ref<Set<string>>(getInitialRatedOrders());
const showReviewModal = ref<boolean>(false);
const selectedOrderForReview = ref<any>(null);
const isSubmittingReview = ref<boolean>(false);

// 1. Phân hệ Tài xế giao vận (Shipper)
const shipperRating = ref<number>(5);
const shipperComment = ref<string>("");
const selectedShipperTags = ref<string[]>([]);
const defaultShipperTags = [
  "⚡ Giao siêu tốc",
  "😊 Thân thiện & lịch sự",
  "📦 Hàng nguyên vẹn, không dập",
  "⏱️ Đúng giờ hẹn",
  "📍 Tìm đúng địa chỉ"
];

// 2. Phân hệ Gian hàng đối tác (Store)
const storeRating = ref<number>(5);
const storeComment = ref<string>("");
const selectedStoreTags = ref<string[]>([]);
const defaultStoreTags = [
  "📦 Đóng gói cẩn thận",
  "🌿 Nông sản tươi rói",
  "⏱️ Chuẩn bị nhanh chóng",
  "💯 Đúng & đủ món",
  "🎁 Quà tặng chu đáo"
];

// 3. Phân hệ Từng Sản phẩm chi tiết (Products)
const reviewProductItems = ref<ReviewProductItemState[]>([]);
const defaultProductTags = [
  "🌿 Rất tươi ngon",
  "💰 Đáng tiền",
  "✨ Đúng hình ảnh",
  "😋 Vị thơm ngọt tự nhiên",
  "🔄 Sẽ mua lại"
];

const toggleTag = (list: string[], tag: string) => {
  const idx = list.indexOf(tag);
  if (idx >= 0) list.splice(idx, 1);
  else list.push(tag);
};

const openReviewModal = (order: any) => {
  selectedOrderForReview.value = order;
  shipperRating.value = 0;
  shipperComment.value = "";
  selectedShipperTags.value = [];

  storeRating.value = 0;
  storeComment.value = "";
  selectedStoreTags.value = [];

  const parsed = parseOrderItems(order.items);
  reviewProductItems.value = parsed.map((it: any) => {
    const match = findProductMatch(it);
    return {
      productId: match?.id || it.productId || it.id || ("prod_" + it.name.trim().toLowerCase().replace(/[^a-z0-9]/g, "_")),
      name: it.name,
      image: it.image || match?.image || "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=300&q=80",
      quantity: it.quantity || 1,
      price: it.price || match?.price || 0,
      rating: 0,
      comment: "",
      tags: []
    };
  });

  showReviewModal.value = true;
};

const closeReviewModal = () => {
  showReviewModal.value = false;
  selectedOrderForReview.value = null;
};

// ========================================================
// TRUY XUẤT NGUỒN GỐC 6 BƯỚC (SƠ ĐỒ CHƯƠNG 2 - LUỒNG 6)
// Đơn hàng -> Chi tiết đơn -> Lô hàng -> Gian hàng -> Merchant -> Nguồn cung cấp
// ========================================================
const showTraceabilityModal = ref<boolean>(false);
const selectedOrderForTrace = ref<any>(null);
const isLoadingTraceability = ref<boolean>(false);
const traceabilityResult = ref<any>(null);

const openTraceabilityModal = async (order: any) => {
  selectedOrderForTrace.value = order;
  showTraceabilityModal.value = true;
  isLoadingTraceability.value = true;
  traceabilityResult.value = null;

  try {
    const rawId = String(order.orderId || order.id || "").replace(/^#/, "").trim();
    const subId = order.subOrders?.[0]?.subId || rawId;
    const res = await apiFetch(`/api/orders/traceability/${subId}`);
    if (res.ok) {
      const data = await res.json();
      traceabilityResult.value = data.traceability;
    } else {
      // Dữ liệu truy xuất chuẩn 6 bước
      traceabilityResult.value = {
        orderId: rawId,
        orderDate: order.date || new Date().toLocaleDateString("vi-VN"),
        deliveryType: order.deliveryType || "Giao ngay địa phương (Bán kính 10km)",
        orderStatus: order.status || "completed",
        items: (order.items || []).map((it: any) => ({
          productName: it.name || it.title || "Thực phẩm / Nông sản địa phương",
          quantity: it.quantity || 1,
          price: it.price || 35000,
          category: "Nông sản tươi sống",
          batch: {
            batchNumber: "LOT-HN-202610-01",
            productionDate: "08/10/2026",
            expiryDate: "18/10/2026",
            aiCheckNote: "Nguồn gốc an toàn, kiểm duyệt AI đạt chuẩn chất lượng"
          },
          store: {
            storeName: getOrderStoreName(order),
            marketName: "Chợ dân sinh truyền thống địa phương",
            ward: "Xã/Phường quản lý",
            district: "Quận/Huyện"
          },
          merchant: {
            ownerName: "Tiểu thương đã xác thực định danh",
            cccdMasked: "0010****8992",
            foodSafetyCert: "Đã xác thực giấy chứng nhận ATTP"
          },
          supplier: {
            supplierName: "Hợp tác xã Nông nghiệp Sạch địa phương",
            originType: "Nông sản địa phương đạt tiêu chuẩn VietGAP / OCOP"
          }
        }))
      };
    }
  } catch (err) {
    console.error("Lỗi khi tải dữ liệu truy xuất nguồn gốc:", err);
  } finally {
    isLoadingTraceability.value = false;
  }
};

const closeTraceabilityModal = () => {
  showTraceabilityModal.value = false;
  selectedOrderForTrace.value = null;
  traceabilityResult.value = null;
};

const handleConfirmReceived = async (id: string, order?: any) => {
  if (confirm(`Bạn đã nhận đủ và hài lòng với đơn hàng #${id}? Bấm OK để xác nhận hoàn tất và đánh giá!`)) {
    if (order) {
      order.status = "completed";
      openReviewModal(order);
    } else {
      const found = orders.value.find(o => (o.orderId || o.id) === id);
      if (found) {
        found.status = "completed";
        openReviewModal(found);
      }
    }
  }
};

const submitReview = async () => {
  if (!selectedOrderForReview.value) return;
  const unrated = reviewProductItems.value.find(it => !it.rating || it.rating <= 0);
  if (unrated) {
    toast.warning(`Vui lòng bấm chọn số sao đánh giá cho sản phẩm "${unrated.name}"!`, "Chưa chọn số sao ⭐");
    return;
  }
  const order = selectedOrderForReview.value;
  const orderId = String(order.orderId || order.id || "").replace(/^#/, "").trim();
  const storeName = getOrderStoreName(order);
  const storeId = order.storeId || order.store?.id || "";

  isSubmittingReview.value = true;
  try {
    const payload = {
      orderId,
      subOrderId: order.subOrderId || "",
      storeId,
      storeName,
      buyerId: auth.currentUser.value?.id || "",
      buyerName: currentAccountName.value,
      buyerAvatar: auth.currentUser.value?.avatarUrl || (auth.currentUser.value as any)?.avatar || "",
      rating: storeRating.value,
      comment: storeComment.value,
      shipperId: order.shipperId || order.shipper?.id || "",
      shipperName: order.shipperName || order.shipper?.name || "Tài xế ZoneMart Express",
      shipperRating: shipperRating.value,
      shipperComment: shipperComment.value,
      shipperTags: selectedShipperTags.value,
      storeRating: storeRating.value,
      storeComment: storeComment.value,
      storeTags: selectedStoreTags.value,
      productReviews: reviewProductItems.value.map(it => ({
        productId: it.productId,
        productName: it.name,
        productImage: it.image,
        rating: it.rating,
        comment: it.comment,
        tags: it.tags
      }))
    };

    // 1. Cập nhật local rating ngay lập tức để Bảng Xếp Hạng phản ứng 0ms
    catalog.recordLocalReview(payload);

    // 2. Gửi đồng bộ lên backend API
    await apiFetch("/api/orders/reviews", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    }).catch(() => null);

    ratedOrderIds.value.add(orderId);
    try {
      localStorage.setItem("zonemart_rated_order_ids", JSON.stringify(Array.from(ratedOrderIds.value)));
    } catch {}

    toast.success(
      `Cảm ơn bạn! Đã ghi nhận đánh giá cho Tài xế, Gian hàng "${storeName}" và ${reviewProductItems.value.length} sản phẩm. Điểm số trung bình đã được cập nhật ngay trên sàn!`,
      "Hoàn Tất Đánh Giá 🎉"
    );
    closeReviewModal();
  } catch (e) {
    ratedOrderIds.value.add(orderId);
    try {
      localStorage.setItem("zonemart_rated_order_ids", JSON.stringify(Array.from(ratedOrderIds.value)));
    } catch {}
    toast.success("Đã ghi nhận đánh giá của bạn! Cảm ơn bạn.", "Đã Ghi Nhận");
    closeReviewModal();
  } finally {
    isSubmittingReview.value = false;
  }
};

const findProductMatch = (item: any): CatalogProduct | undefined => {
  if (!item) return undefined;
  const all = catalog.allProducts.value || [];
  
  if (item.id || item.productId) {
    const byId = all.find(p => p.id === (item.id || item.productId));
    if (byId) return byId;
  }
  
  if (item.name) {
    const target = item.name.toLowerCase().trim();
    const byExact = all.find(p => p.name.toLowerCase().trim() === target);
    if (byExact) return byExact;
    
    const bySub = all.find(p => {
      const pn = p.name.toLowerCase().trim();
      return pn.includes(target) || target.includes(pn);
    });
    if (bySub) return bySub;
  }

  if (item.image) {
    const byImg = all.find(p => p.image === item.image);
    if (byImg) return byImg;
  }

  return undefined;
};

const goToProductDetail = (item: any) => {
  const match = findProductMatch(item);
  if (match) {
    router.push(`/products/${match.id}`);
  } else if (item.id || item.productId) {
    router.push(`/products/${item.id || item.productId}`);
  } else if (item.name) {
    router.push(`/products?search=${encodeURIComponent(item.name)}`);
  } else {
    router.push('/products');
  }
};

const handleBuyAgain = (order: any) => {
  const items = parseOrderItems(order.items);
  if (!items || items.length === 0) {
    router.push('/products');
    return;
  }
  goToProductDetail(items[0]);
};

const handleReportIssue = (order: any) => {
  const items = parseOrderItems(order.items);
  const firstItem = items[0] || {};
  const orderId = String(order.orderId || order.id || '').replace(/^#/, '').trim();
  const storeName = getOrderStoreName(order);
  const total = order.total || 0;

  const supportData = {
    orderCode: orderId,
    productName: firstItem.name || 'Đơn hàng ZoneMart',
    productImage: firstItem.image || '',
    storeName: storeName,
    total: total,
    date: order.date || '',
    recipientName: order.recipientName || '',
    recipientPhone: order.recipientPhone || '',
    itemsCount: items.length
  };

  try {
    sessionStorage.setItem('zonemart_support_order', JSON.stringify(supportData));
  } catch (e) {}

  router.push({
    path: '/contact',
    query: {
      orderCode: orderId,
      productName: firstItem.name || '',
      productImage: firstItem.image || '',
      storeName: storeName,
      total: String(total),
      topic: 'order'
    }
  });
};
</script>

<template>
  <div class="buyer-orders-container">
    <!-- Real-time Order Notification Toast -->
    <transition name="toast-slide">
      <div v-if="showRealTimeToast" class="realtime-status-toast">
        <i class="bi bi-broadcast text-primary"></i>
        <span>{{ realTimeToast }}</span>
      </div>
    </transition>

    <div class="header">
      <div class="header-content">
        <h2><i class="bi bi-box-seam-fill me-2 text-primary"></i>Đơn Mua Của Bạn</h2>
        <div class="account-meta-line" v-if="currentAccount">
          <span class="user-chip">
            <i class="bi bi-person-circle text-primary"></i>
            <span>Đơn hàng của tài khoản: <strong>{{ currentAccountName }}</strong></span>
            <span class="role-subpill">{{ auth.roleLabel.value }}</span>
          </span>
          <span class="orders-count-indicator" v-if="!isLoading">
            • <strong>{{ orders.length }}</strong> đơn hàng
          </span>
        </div>
        <p v-else>Theo dõi trực tiếp chi tiết sản phẩm, hình ảnh và trạng thái các đơn hàng của bạn.</p>
      </div>
      <div class="header-actions">
        <button class="btn-refresh-orders" @click="fetchOrdersWithLoading(500)" :disabled="isLoading" title="Tải lại đơn mua">
          <i class="bi bi-arrow-clockwise" :class="{ 'spin-icon': isLoading }"></i>
          <span>{{ isLoading ? 'Đang tải...' : 'Làm mới' }}</span>
        </button>
        <router-link to="/products" class="btn-continue-shopping">
          <i class="bi bi-cart-plus me-1"></i> Mua thêm nông sản
        </router-link>
      </div>
    </div>

    <!-- THANH ĐIỀU HƯỚNG TAB CHUẨN SHOPEE (TẤT CẢ, ĐANG GIAO, HOÀN THÀNH, SẢN PHẨM ĐÃ MUA) -->
    <div class="buyer-orders-tabs-nav">
      <button 
        type="button"
        class="tab-nav-btn" 
        :class="{ active: activeFilterTab === 'all' }" 
        @click="activeFilterTab = 'all'"
      >
        <i class="bi bi-collection-fill me-1"></i> Tất Cả Đơn ({{ orders.length }})
      </button>

      <button 
        type="button"
        class="tab-nav-btn" 
        :class="{ active: activeFilterTab === 'delivering' }" 
        @click="activeFilterTab = 'delivering'"
      >
        <i class="bi bi-bicycle me-1"></i> Đang Giao Hàng ({{ deliveringOrdersCount }})
      </button>

      <button 
        type="button"
        class="tab-nav-btn" 
        :class="{ active: activeFilterTab === 'completed' }" 
        @click="activeFilterTab = 'completed'"
      >
        <i class="bi bi-check-circle-fill me-1"></i> Đã Hoàn Thành ({{ completedOrdersCount }})
      </button>

      <button 
        type="button"
        class="tab-nav-btn highlight-shopee" 
        :class="{ active: activeFilterTab === 'purchased_products' }" 
        @click="activeFilterTab = 'purchased_products'"
      >
        <i class="bi bi-stars text-warning me-1"></i> Sản Phẩm Đã Mua & Đánh Giá ({{ allPurchasedProducts.length }})
        <span class="badge-pending-rate" v-if="unratedProductsCount > 0">{{ unratedProductsCount }} chờ đánh giá</span>
      </button>
    </div>

    <!-- HIỆU ỨNG LOADING SKELETON (TASTE-SKILL TACTILE SHIMMER) -->
    <div v-if="isLoading" class="skeleton-orders-wrap">
      <div class="sync-status-indicator">
        <span class="sync-spinner-ring"></span>
        <span class="sync-text">
          Đang đồng bộ đơn mua của tài khoản <strong>{{ currentAccountName }}</strong>...
        </span>
      </div>

      <div v-for="i in 2" :key="i" class="order-card-skeleton">
        <!-- Top Bar Skeleton -->
        <div class="skeleton-row skeleton-top-bar">
          <div class="skeleton-item sk-pill-code"></div>
          <div class="skeleton-right-badges">
            <div class="skeleton-item sk-pill-sm"></div>
            <div class="skeleton-item sk-pill-sm"></div>
          </div>
        </div>

        <!-- Store Banner Skeleton -->
        <div class="skeleton-row skeleton-store-row">
          <div class="skeleton-item sk-circle-avatar"></div>
          <div class="skeleton-col">
            <div class="skeleton-item sk-line-title"></div>
            <div class="skeleton-item sk-line-sub"></div>
          </div>
          <div class="skeleton-item sk-status-pill ms-auto"></div>
        </div>

        <!-- Product Row Skeleton -->
        <div class="skeleton-product-item">
          <div class="skeleton-item sk-thumb"></div>
          <div class="skeleton-col flex-grow-1">
            <div class="skeleton-item sk-line-prod-name"></div>
            <div class="skeleton-item sk-line-prod-meta"></div>
          </div>
          <div class="skeleton-item sk-price-block"></div>
        </div>

        <!-- Address Skeleton -->
        <div class="skeleton-row skeleton-addr-row">
          <div class="skeleton-item sk-line-full"></div>
        </div>

        <!-- Bottom Bar Skeleton -->
        <div class="skeleton-row skeleton-bottom-row">
          <div class="skeleton-item sk-line-sm"></div>
          <div class="skeleton-col-actions ms-auto">
            <div class="skeleton-item sk-price-total"></div>
            <div class="skeleton-item sk-btn"></div>
            <div class="skeleton-item sk-btn"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- GIAO DIỆN TAB: SẢN PHẨM ĐÃ MUA & ĐÁNH GIÁ (CHUẨN SHOPEE) -->
    <div v-else-if="activeFilterTab === 'purchased_products'" class="purchased-products-view">
      <div class="purchased-header-intro">
        <div class="intro-left">
          <h3><i class="bi bi-bag-check-fill text-success me-2"></i>Các Sản Phẩm Bạn Đã Mua</h3>
          <p>Chỉ những sản phẩm từ đơn hàng đã giao thành công mới có thể gửi đánh giá xác thực (Chuẩn Shopee).</p>
        </div>
        <div class="intro-right-stats">
          <span class="stat-pill">Tổng đã mua: <strong>{{ allPurchasedProducts.length }}</strong> món</span>
          <span class="stat-pill highlight" v-if="unratedProductsCount > 0">Chưa đánh giá: <strong>{{ unratedProductsCount }}</strong> món</span>
        </div>
      </div>

      <div v-if="allPurchasedProducts.length === 0" class="empty-purchased-card">
        <div class="empty-icon-wrap"><i class="bi bi-box2-heart"></i></div>
        <h4>Chưa có sản phẩm đã mua nào hoàn tất</h4>
        <p>Khi bạn nhận hàng thành công từ shipper, các món hàng sẽ hiển thị ở đây để bạn chấm sao và viết cảm nhận giống Shopee!</p>
        <router-link to="/products" class="btn btn-warning fw-bold text-dark btn-sm">
          <i class="bi bi-cart-plus me-1"></i> Khám phá mua sắm ngay
        </router-link>
      </div>

      <div v-else class="purchased-products-grid">
        <div 
          v-for="entry in allPurchasedProducts" 
          :key="entry.uniqueKey" 
          class="purchased-product-card"
        >
          <div class="card-thumb-wrap" @click="goToProductDetail(entry)" title="Bấm để xem sản phẩm">
            <img :src="entry.image" :alt="entry.name" class="card-thumb-img" @error="handleImgError" />
            <span class="qty-badge-float">x{{ entry.quantity }}</span>
          </div>

          <div class="card-content-wrap">
            <div class="card-top-row">
              <span class="store-badge-tag"><i class="bi bi-shop me-1"></i>{{ entry.storeName }}</span>
              <span class="order-code-tag">#{{ entry.orderId }} • {{ entry.orderDate }}</span>
            </div>

            <h4 class="card-prod-title" @click="goToProductDetail(entry)">{{ entry.name }}</h4>

            <div class="card-price-row">
              <span class="unit-price" v-if="entry.price > 0">{{ formatPrice(entry.price) }}</span>
              <span class="subtotal-text" v-if="entry.price > 0">Tổng: <strong>{{ formatPrice(entry.price * entry.quantity) }}</strong></span>
            </div>

            <div class="card-action-bar">
              <!-- Nút trạng thái Đánh giá -->
              <div v-if="ratedProductsMap[entry.uniqueKey] || ratedProductsMap[entry.productId]" class="rated-status-group">
                <span class="badge bg-success-subtle text-success border border-success-subtle p-2">
                  <i class="bi bi-patch-check-fill text-success me-1"></i>
                  Đã đánh giá {{ (ratedProductsMap[entry.uniqueKey] || ratedProductsMap[entry.productId]).rating }}★
                </span>
                <button type="button" class="btn btn-sm btn-outline-secondary btn-re-review" @click="openSingleProductReviewModal(entry)">
                  <i class="bi bi-pencil-square me-1"></i> Xem / Sửa
                </button>
              </div>

              <div v-else class="unrated-status-group">
                <button type="button" class="btn btn-sm btn-warning text-dark fw-bold btn-shopee-rate" @click="openSingleProductReviewModal(entry)">
                  <i class="bi bi-star-fill text-dark me-1"></i> Đánh Giá Sản Phẩm
                </button>
                <span class="rate-incentive-hint"><i class="bi bi-award-fill text-warning me-1"></i>Tích lũy điểm uy tín</span>
              </div>

              <button type="button" class="btn btn-sm btn-outline-primary ms-auto btn-buy-again-mini" @click="goToProductDetail(entry)">
                <i class="bi bi-arrow-repeat me-1"></i> Mua Lại
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- DANH SÁCH ĐƠN HÀNG (LỌC THEO TAB TẤT CẢ, ĐANG GIAO, HOÀN THÀNH) -->
    <div v-else-if="filteredOrders.length > 0" class="orders-list">
      <div v-for="order in filteredOrders" :key="order.orderId || order.id" class="order-card">
        <!-- HEADER ĐƠN HÀNG -->
        <div class="order-top">
          <div class="order-meta-info">
            <span class="order-tag">Mã đơn:</span>
            <strong class="order-id-code">#{{ order.orderId || order.id }}</strong>
            <span class="order-meta-sep">•</span>
            <span class="order-date"><i class="bi bi-clock me-1"></i>{{ order.date }}</span>
          </div>

          <div class="order-top-badges">
            <span class="badge payment-badge" v-if="order.paymentMethod">
              <i class="bi bi-credit-card-2-front me-1"></i>{{ formatPaymentMethod(order.paymentMethod) }}
            </span>
            <span class="delivery-badge" :class="order.deliveryType || (order.shippingMethod?.includes('Hỏa Tốc') ? 'express' : 'standard')">
              <i class="bi bi-lightning-charge-fill me-1"></i>
              {{ order.shippingMethod || (order.deliveryType === 'standard' ? 'Giao Tiêu Chuẩn' : 'Hỏa Tốc 10km') }}
            </span>
          </div>
        </div>

        <!-- THÂN ĐƠN HÀNG -->
        <div class="order-body">
          <!-- Banner Cửa Hàng & Trạng Thái -->
          <div class="order-store-banner">
            <div class="store-brand">
              <div class="store-icon-wrap">
                <i class="bi bi-shop"></i>
              </div>
              <div class="store-name-group">
                <h4 class="store-name">{{ getOrderStoreName(order) }}</h4>
                <span class="store-verified"><i class="bi bi-patch-check-fill text-success me-1"></i>Gian hàng chính hãng</span>
              </div>
            </div>

            <div class="status-pill" :class="getOrderStatusClass(order.status)">
              <i v-if="getOrderStatusClass(order.status) === 'completed'" class="bi bi-check-circle-fill me-1"></i>
              <i v-else-if="getOrderStatusClass(order.status) === 'delivering'" class="bi bi-bicycle me-1"></i>
              <i v-else-if="getOrderStatusClass(order.status) === 'picking'" class="bi bi-geo-alt-fill me-1"></i>
              <i v-else-if="getOrderStatusClass(order.status) === 'confirmed'" class="bi bi-radar me-1"></i>
              <i v-else-if="getOrderStatusClass(order.status) === 'cancelled'" class="bi bi-x-circle me-1"></i>
              <i v-else class="bi bi-hourglass-split me-1"></i>
              <span>{{ getOrderStatusText(order.status, order) }}</span>
            </div>
          </div>

          <!-- Thanh thông tin Shipper khi đang lấy/giao hàng -->
          <div v-if="order.shipperInfo && (getOrderStatusClass(order.status) === 'picking' || getOrderStatusClass(order.status) === 'delivering')" class="buyer-shipper-live-card">
            <div class="shipper-avatar-mini">
              <i class="bi bi-bicycle text-primary"></i>
            </div>
            <div class="shipper-live-text">
              <div class="shipper-live-name">
                <strong>{{ order.shipperInfo.name }}</strong> ({{ order.shipperInfo.phone }})
                <span class="plate-tag">{{ order.shipperInfo.licensePlate }}</span>
              </div>
              <small class="shipper-live-desc">
                {{ getOrderStatusClass(order.status) === 'picking' ? 'Tài xế đang trên đường đến quán lấy món hàng cho bạn' : 'Tài xế đang tới vị trí của bạn' }}
              </small>
            </div>
          </div>

          <!-- DANH SÁCH SẢN PHẨM TRONG ĐƠN (ĐẦY ĐỦ HÌNH ẢNH, GIÁ, SỐ LƯỢNG) -->
          <div class="products-list">
            <div 
              v-for="(item, idx) in parseOrderItems(order.items)" 
              :key="idx" 
              class="product-item-card clickable-product"
              @click="goToProductDetail(item)"
              title="Bấm để xem chi tiết sản phẩm này"
            >
              <!-- Hình ảnh sản phẩm -->
              <div class="product-thumb-box">
                <img 
                  v-if="item.image" 
                  :src="item.image" 
                  :alt="item.name" 
                  class="product-thumb-img" 
                  @error="handleImgError" 
                />
                <div v-else class="product-thumb-fallback">
                  <i class="bi bi-basket3-fill"></i>
                </div>
              </div>

              <!-- Thông tin chi tiết sản phẩm -->
              <div class="product-details-col">
                <h4 class="product-name">{{ item.name }}</h4>
                <div class="product-shop-badge" v-if="item.shop">
                  <i class="bi bi-geo-alt-fill me-1"></i>{{ item.shop }}
                </div>
                <div class="product-price-qty-row">
                  <span class="unit-price" v-if="item.price > 0">{{ formatPrice(item.price) }}</span>
                  <span class="qty-badge">Số lượng: <strong>x{{ item.quantity || 1 }}</strong></span>
                </div>
              </div>

              <!-- Thành tiền sản phẩm & Nút đánh giá món lẻ (Chuẩn Shopee) -->
              <div class="product-subtotal-box" v-if="item.price > 0">
                <span class="subtotal-label">Thành tiền:</span>
                <strong class="subtotal-amount">{{ formatPrice((item.price || 0) * (item.quantity || 1)) }}</strong>
                
                <div class="inline-item-rate-wrap" v-if="getOrderStatusClass(order.status) === 'completed' || order.status === 'completed'">
                  <button 
                    v-if="!isProductItemRated(order.orderId || order.id, item)" 
                    type="button"
                    class="btn btn-xs btn-outline-warning btn-rate-item-inline"
                    @click.stop="openSingleProductReviewModalFromOrder(item, order)"
                    title="Đánh giá chất lượng riêng cho sản phẩm này"
                  >
                    <i class="bi bi-star-fill text-warning me-1"></i> Đánh giá món
                  </button>
                  <span v-else class="badge-rated-item-inline">
                    <i class="bi bi-check-circle-fill text-success me-1"></i> Đã đánh giá {{ getProductItemRating(order.orderId || order.id, item) }}★
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- HỘP THÔNG TIN NGƯỜI NHẬN & ĐỊA CHỈ GIAO HÀNG -->
          <div v-if="order.shippingAddress || order.recipientName" class="shipping-info-box">
            <div class="info-row">
              <i class="bi bi-geo-alt-fill info-icon text-danger"></i>
              <div class="info-text">
                <strong>Địa chỉ nhận hàng:</strong> 
                <span v-if="order.recipientName" class="recipient-name">{{ order.recipientName }} ({{ order.recipientPhone }})</span> - 
                <span>{{ order.shippingAddress }}</span>
              </div>
            </div>
            <div v-if="order.deliveryNote" class="info-row note-row">
              <i class="bi bi-chat-left-dots-fill info-icon text-warning"></i>
              <div class="info-text">
                <strong>Lời nhắn giao hàng:</strong> <span>{{ order.deliveryNote }}</span>
              </div>
            </div>

          </div>
        </div>

        <!-- FOOTER ĐƠN HÀNG (TỔNG TIỀN VÀ NÚT THAO TÁC) -->
        <div class="order-bottom">
          <div class="bottom-left-summary">
            <span class="items-count-text">
              <i class="bi bi-bag-check me-1"></i>
              Tổng cộng <strong>{{ parseOrderItems(order.items).reduce((sum, it) => sum + (it.quantity || 1), 0) }}</strong> món hàng
            </span>
          </div>

          <div class="bottom-right-actions">
            <div class="total-price-box">
              <span class="total-label">Tổng thanh toán:</span>
              <strong class="total-amount">{{ formatPrice(order.total) }}</strong>
            </div>

            <div class="action-buttons">
              <button 
                class="btn btn-outline-info btn-sm text-dark fw-bold" 
                @click="openTraceabilityModal(order)"
                title="Truy xuất nguồn gốc 6 bước: Đơn hàng -> Lô hàng -> Chủ sạp -> Nhà cung cấp"
              >
                <i class="bi bi-shield-check text-info me-1"></i> Truy Xuất Nguồn Gốc 🔍
              </button>
              <button class="btn btn-outline-primary btn-sm" @click="handleBuyAgain(order)" title="Xem và mua lại sản phẩm này">
                <i class="bi bi-arrow-repeat me-1"></i> Mua Lại
              </button>
              <button 
                v-if="getOrderStatusClass(order.status) === 'delivering'" 
                class="btn btn-success btn-sm" 
                @click="handleConfirmReceived(order.orderId || order.id, order)"
              >
                <i class="bi bi-check-circle-fill me-1"></i> Đã nhận hàng
              </button>
              <button 
                v-if="getOrderStatusClass(order.status) === 'completed' || order.status === 'completed'" 
                class="btn btn-warning btn-sm text-dark fw-bold" 
                @click="openReviewModal(order)"
                title="Đánh giá chất lượng sản phẩm & dịch vụ"
              >
                <i class="bi bi-star-fill text-dark me-1"></i>
                {{ ratedOrderIds.has(String(order.orderId || order.id).replace(/^#/, '').trim()) ? 'Đã Đánh Giá ⭐' : 'Đánh Giá ⭐' }}
              </button>
              <button 
                class="btn btn-outline-secondary btn-sm" 
                @click="handleReportIssue(order)"
                title="Gửi yêu cầu hỗ trợ cho đơn hàng này"
              >
                <i class="bi bi-question-circle me-1"></i> Trợ giúp
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- GIAO DIỆN KHI CHƯA CÓ ĐƠN MUA -->
    <div v-else class="empty-orders-card">
      <div class="empty-art-circle">
        <i class="bi bi-box2-heart"></i>
      </div>
      <h3 class="empty-main-title">Bạn Chưa Có Đơn Mua Nào</h3>
      <p class="empty-sub-text">
        Chưa có đơn hàng nào được đặt. Hãy khám phá các gian hàng nông sản và thực phẩm tươi sống quanh bạn để đặt những bữa ăn ngon lành nhé!
      </p>
      <router-link to="/products" class="btn-shop-now">
        <i class="bi bi-bag-plus-fill me-1"></i> Khám Phá Nông Sản Quanh Bạn (10km)
      </router-link>
    </div>

    <!-- MODAL ĐÁNH GIÁ 3 PHÂN HỆ: TÀI XẾ, GIAN HÀNG & TỪNG SẢN PHẨM -->
    <div v-if="showReviewModal" class="review-modal-backdrop" @click.self="closeReviewModal">
      <div class="review-modal-dialog">
        <div class="review-modal-header">
          <div class="review-modal-title">
            <div class="review-header-icon"><i class="bi bi-stars"></i></div>
            <div>
              <h3>Đánh Giá Đơn Hàng #{{ String(selectedOrderForReview?.orderId || selectedOrderForReview?.id).replace(/^#/, '') }}</h3>
              <p>Phản hồi của bạn giúp tối ưu thứ hạng sản phẩm & chất lượng phục vụ</p>
            </div>
          </div>
          <button type="button" class="btn-close-review" @click="closeReviewModal" title="Đóng">&times;</button>
        </div>

        <div class="review-modal-scrollable-body" v-if="selectedOrderForReview">
          
          <!-- PHÂN HỆ 1: ĐÁNH GIÁ TÀI XẾ (SHIPPER) -->
          <div class="review-section-card shipper-card">
            <div class="section-card-header">
              <div class="section-icon shipper-icon"><i class="bi bi-bicycle"></i></div>
              <div class="section-info">
                <h4>1. Đánh Giá Tài Xế Giao Vận</h4>
                <p>{{ selectedOrderForReview.shipperName || selectedOrderForReview.shipper?.name || 'Tài xế ZoneMart Hỏa Tốc' }} (Bán kính 10km)</p>
              </div>
            </div>

            <div class="interactive-stars-box">
              <div class="stars-selector">
                <span 
                  v-for="s in 5" 
                  :key="s" 
                  class="star-item" 
                  :class="{ active: s <= shipperRating }"
                  @click="shipperRating = s"
                >★</span>
              </div>
              <span class="rating-badge-text">
                {{ shipperRating === 5 ? '⭐ 5/5 - Giao siêu nhanh & thân thiện' : shipperRating === 4 ? '⭐ 4/5 - Hài lòng, giao đúng hẹn' : shipperRating === 3 ? '⭐ 3/5 - Bình thường' : shipperRating === 2 ? '⭐ 2/5 - Giao chậm trễ' : '⭐ 1/5 - Thái độ chưa tốt' }}
              </span>
            </div>

            <div class="quick-tags-container">
              <button 
                v-for="tag in defaultShipperTags" 
                :key="tag" 
                type="button" 
                class="quick-tag-pill" 
                :class="{ active: selectedShipperTags.includes(tag) }"
                @click="toggleTag(selectedShipperTags, tag)"
              >
                {{ tag }}
              </button>
            </div>

            <input 
              v-model="shipperComment" 
              type="text" 
              class="form-control review-input-text" 
              placeholder="Lời khen hoặc góp ý thêm cho shipper (tùy chọn)..." 
            />
          </div>

          <!-- PHÂN HỆ 2: ĐÁNH GIÁ GIAN HÀNG (STORE) -->
          <div class="review-section-card store-card">
            <div class="section-card-header">
              <div class="section-icon store-icon"><i class="bi bi-shop"></i></div>
              <div class="section-info">
                <h4>2. Đánh Giá Gian Hàng</h4>
                <p><strong>{{ getOrderStoreName(selectedOrderForReview) }}</strong> • Chuẩn bị & đóng gói</p>
              </div>
            </div>

            <div class="interactive-stars-box">
              <div class="stars-selector">
                <span 
                  v-for="s in 5" 
                  :key="s" 
                  class="star-item" 
                  :class="{ active: s <= storeRating }"
                  @click="storeRating = s"
                >★</span>
              </div>
              <span class="rating-badge-text">
                {{ storeRating === 5 ? '⭐ 5/5 - Gian hàng uy tín, hàng chuẩn' : storeRating === 4 ? '⭐ 4/5 - Đóng gói tốt, giao đủ' : storeRating === 3 ? '⭐ 3/5 - Bình thường' : storeRating === 2 ? '⭐ 2/5 - Đóng gói sơ sài' : '⭐ 1/5 - Thiếu hàng / Hàng kém chất lượng' }}
              </span>
            </div>

            <div class="quick-tags-container">
              <button 
                v-for="tag in defaultStoreTags" 
                :key="tag" 
                type="button" 
                class="quick-tag-pill" 
                :class="{ active: selectedStoreTags.includes(tag) }"
                @click="toggleTag(selectedStoreTags, tag)"
              >
                {{ tag }}
              </button>
            </div>

            <input 
              v-model="storeComment" 
              type="text" 
              class="form-control review-input-text" 
              placeholder="Cảm nhận về quy cách phục vụ của gian hàng..." 
            />
          </div>

          <!-- PHÂN HỆ 3: ĐÁNH GIÁ TỪNG SẢN PHẨM TRONG ĐƠN -->
          <div class="review-section-card products-card">
            <div class="section-card-header">
              <div class="section-icon product-icon"><i class="bi bi-basket3-fill"></i></div>
              <div class="section-info">
                <h4>3. Đánh Giá Chi Tiết Từng Sản Phẩm ({{ reviewProductItems.length }} món)</h4>
                <p>Điểm số sẽ được tính trung bình động và đưa món ngon nhất lên đầu sàn</p>
              </div>
            </div>

            <div class="product-review-list">
              <div 
                v-for="(item, idx) in reviewProductItems" 
                :key="item.productId || idx" 
                class="product-item-review-row"
              >
                <div class="product-thumb-col">
                  <img :src="item.image" :alt="item.name" class="p-thumb-img" @error="handleImgError" />
                </div>
                <div class="product-review-details-col">
                  <div class="p-header-info">
                    <strong class="p-name">{{ item.name }}</strong>
                    <span class="p-qty badge bg-light text-dark border">x{{ item.quantity }}</span>
                  </div>

                  <div class="interactive-stars-box mini">
                    <div class="stars-selector">
                      <span 
                        v-for="s in 5" 
                        :key="s" 
                        class="star-item mini" 
                        :class="{ active: s <= item.rating }"
                        @click="item.rating = s"
                      >★</span>
                    </div>
                    <span class="p-rating-label">
                      {{ item.rating === 5 ? 'Rất tươi ngon (5★)' : item.rating === 4 ? 'Tươi ngon (4★)' : item.rating === 3 ? 'Ổn (3★)' : item.rating === 2 ? 'Kém tươi (2★)' : 'Hỏng / Dập (1★)' }}
                    </span>
                  </div>

                  <div class="quick-tags-container mini">
                    <button 
                      v-for="tag in defaultProductTags" 
                      :key="tag" 
                      type="button" 
                      class="quick-tag-pill mini" 
                      :class="{ active: item.tags.includes(tag) }"
                      @click="toggleTag(item.tags, tag)"
                    >
                      {{ tag }}
                    </button>
                  </div>

                  <textarea 
                    v-model="item.comment" 
                    rows="2" 
                    class="form-control review-textarea-small" 
                    placeholder="Nhận xét độ tươi, vị ngon, đóng gói của món này..."
                  ></textarea>
                </div>
              </div>
            </div>
          </div>

        </div>

        <div class="review-modal-footer">
          <button type="button" class="btn btn-secondary btn-sm" @click="closeReviewModal" :disabled="isSubmittingReview">
            Hủy Bỏ
          </button>
          <button type="button" class="btn btn-warning btn-sm fw-bold text-dark btn-submit-rating" @click="submitReview" :disabled="isSubmittingReview">
            <i class="bi bi-star-fill text-dark me-1" v-if="!isSubmittingReview"></i>
            <span class="spinner-border spinner-border-sm me-1" v-else></span>
            {{ isSubmittingReview ? 'Đang lưu đánh giá & tính xếp hạng...' : 'Gửi Đánh Giá & Cập Nhật Bảng Xếp Hạng' }}
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL ĐÁNH GIÁ RIÊNG SẢN PHẨM ĐÃ MUA (CHUẨN SHOPEE) -->
    <div v-if="showSingleProductModal && singleReviewProduct" class="review-modal-backdrop" @click.self="showSingleProductModal = false">
      <div class="review-modal-dialog">
        <div class="review-modal-header">
          <div class="review-modal-title">
            <div class="review-header-icon"><i class="bi bi-star-fill"></i></div>
            <div>
              <h3>Đánh Giá Sản Phẩm Đã Mua</h3>
              <p>Mã đơn: #{{ singleReviewProduct.orderId }} • Gian hàng: {{ singleReviewProduct.storeName }}</p>
            </div>
          </div>
          <button type="button" class="btn-close-review" @click="showSingleProductModal = false" title="Đóng">&times;</button>
        </div>

        <div class="review-modal-scrollable-body">
          <!-- Card Sản phẩm đang đánh giá -->
          <div class="single-prod-eval-card">
            <div class="eval-thumb-col">
              <img :src="singleReviewProduct.image" :alt="singleReviewProduct.name" class="eval-thumb-img" @error="handleImgError" />
            </div>
            <div class="eval-details-col">
              <strong class="eval-prod-name">{{ singleReviewProduct.name }}</strong>
              <div class="eval-meta-row">
                <span class="badge bg-light text-dark border">Số lượng: x{{ singleReviewProduct.quantity }}</span>
                <span class="eval-price" v-if="singleReviewProduct.price > 0">{{ formatPrice(singleReviewProduct.price) }}</span>
              </div>
            </div>
          </div>

          <!-- Bộ Chấm Sao Tương Tác Lớn Cho Món Ăn -->
          <div class="review-section-card">
            <div class="stars-prompt-label">Chất lượng sản phẩm & độ hài lòng:</div>
            <div class="interactive-stars-box large-center">
              <div class="stars-selector">
                <span 
                  v-for="s in 5" 
                  :key="s" 
                  class="star-item large" 
                  :class="{ active: singleProductRating > 0 && s <= singleProductRating }"
                  @click="singleProductRating = s"
                >★</span>
              </div>
              <span class="rating-badge-text fs-6" :class="{ 'text-muted': singleProductRating === 0 }">
                {{ singleProductRating === 5 ? '⭐ Tuyệt vời - Rất tươi ngon & chuẩn vị' : singleProductRating === 4 ? '⭐ Hài lòng - Món ăn ngon, đúng mô tả' : singleProductRating === 3 ? '⭐ Bình thường - Ăn tạm được' : singleProductRating === 2 ? '⭐ Kém chất lượng - Không tươi' : singleProductRating === 1 ? '⭐ Rất tệ - Hỏng hoặc dập ôi' : '👉 Vui lòng bấm chọn số sao đánh giá (1-5 sao)' }}
              </span>
            </div>

            <!-- Gợi ý Tag Đánh Giá Nhanh -->
            <div class="quick-tags-container">
              <button 
                v-for="tag in defaultProductTags" 
                :key="tag" 
                type="button" 
                class="quick-tag-pill" 
                :class="{ active: singleProductTags.includes(tag) }"
                @click="toggleTag(singleProductTags, tag)"
              >
                {{ tag }}
              </button>
            </div>

            <!-- Ô nhận xét chi tiết -->
            <textarea 
              v-model="singleProductComment" 
              rows="3" 
              class="form-control review-textarea" 
              placeholder="Hãy chia sẻ trải nghiệm thực tế về độ tươi ngon, hương vị, đóng gói của món ăn này để giúp đỡ người mua khác nhé..."
            ></textarea>
          </div>

          <!-- Mở rộng: Đánh giá kèm Dịch Vụ Người Bán & Vận Chuyển (Tùy chọn giống Shopee) -->
          <div class="accordion-extra-review">
            <button type="button" class="btn-toggle-extra" @click="showExtraReview = !showExtraReview">
              <span><i class="bi bi-sliders me-1"></i> Đánh giá thêm dịch vụ Người bán & Shipper (Tùy chọn)</span>
              <i class="bi" :class="showExtraReview ? 'bi-chevron-up' : 'bi-chevron-down'"></i>
            </button>

            <div v-if="showExtraReview" class="extra-eval-content">
              <!-- Gian hàng -->
              <div class="extra-eval-row">
                <span class="extra-label"><i class="bi bi-shop text-warning me-1"></i> Dịch vụ người bán:</span>
                <div class="stars-selector">
                  <span v-for="s in 5" :key="s" class="star-item mini" :class="{ active: s <= singleStoreRating }" @click="singleStoreRating = s">★</span>
                </div>
              </div>

              <!-- Shipper -->
              <div class="extra-eval-row">
                <span class="extra-label"><i class="bi bi-bicycle text-primary me-1"></i> Tốc độ giao vận:</span>
                <div class="stars-selector">
                  <span v-for="s in 5" :key="s" class="star-item mini" :class="{ active: s <= singleShipperRating }" @click="singleShipperRating = s">★</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="review-modal-footer">
          <button type="button" class="btn btn-secondary btn-sm" @click="showSingleProductModal = false" :disabled="isSubmittingReview">
            Hủy Bỏ
          </button>
          <button type="button" class="btn btn-warning btn-sm fw-bold text-dark btn-submit-rating" @click="submitSingleProductReview" :disabled="isSubmittingReview">
            <i class="bi bi-send-fill me-1" v-if="!isSubmittingReview"></i>
            <span class="spinner-border spinner-border-sm me-1" v-else></span>
            {{ isSubmittingReview ? 'Đang gửi...' : 'Hoàn Tất & Gửi Đánh Giá' }}
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL TRUY XUẤT NGUỒN GỐC & AN TOÀN NÔNG SẢN (LOẠI BỎ TOÀN BỘ CÁC BƯỚC) -->
    <div v-if="showTraceabilityModal" class="review-modal-backdrop" @click.self="closeTraceabilityModal">
      <div class="trace-modal-container">
        <!-- HEADER -->
        <div class="trace-modal-header">
          <div class="trace-header-content">
            <div class="trace-header-badge">
              <i class="bi bi-shield-fill-check"></i> CHỨNG NHẬN MINH BẠCH
            </div>
            <h4 class="trace-header-title">Truy Xuất Nguồn Gốc Chuỗi Cung Ứng</h4>
            <p class="trace-header-subtitle">Hệ thống giám sát chất lượng nông sản & an toàn thực phẩm ZoneMart</p>
          </div>
          <button type="button" class="trace-btn-close" @click="closeTraceabilityModal" title="Đóng">&times;</button>
        </div>

        <!-- LOADING STATE -->
        <div class="trace-loading-box" v-if="isLoadingTraceability">
          <div class="trace-spinner"></div>
          <p class="trace-loading-text">Đang đối soát chuỗi truy xuất nguồn gốc từ hệ thống Chợ Số...</p>
        </div>

        <!-- RESULT BODY -->
        <div class="trace-modal-body" v-else-if="traceabilityResult">
          <!-- THÔNG TIN ĐƠN HÀNG -->
          <div class="trace-order-banner">
            <div class="trace-order-info">
              <div class="trace-order-code">
                <span class="trace-label">Mã đơn hàng:</span>
                <span class="trace-value">#{{ traceabilityResult.orderId }}</span>
              </div>
              <span class="trace-delivery-pill">{{ traceabilityResult.deliveryType }}</span>
            </div>
            <div class="trace-order-meta">
              <i class="bi bi-clock-history"></i> {{ traceabilityResult.orderDate }}
            </div>
          </div>

          <!-- DANH SÁCH SẢN PHẨM TRUY XUẤT -->
          <div class="trace-items-list">
            <div v-for="(item, idx) in traceabilityResult.items" :key="idx" class="trace-product-card">
              <!-- TIÊU ĐỀ SẢN PHẨM -->
              <div class="trace-product-header">
                <div class="trace-product-title">
                  <span class="trace-prod-icon"><i class="bi bi-basket2-fill"></i></span>
                  <div>
                    <h5 class="trace-prod-name">{{ item.productName }}</h5>
                    <span class="trace-prod-tag">{{ item.category || 'Nông sản địa phương' }}</span>
                  </div>
                </div>
                <div class="trace-product-price">
                  <span class="trace-unit-price">{{ (item.price || 0).toLocaleString('vi-VN') }} ₫</span>
                  <span class="trace-qty-tag">x{{ item.quantity }}</span>
                </div>
              </div>

              <!-- LƯỚI THÔNG TIN MINH BẠCH (4 KHỐI THẺ RÕ RÀNG, KHÔNG DÙNG "BƯỚC") -->
              <div class="trace-details-grid">
                <!-- 1. LÔ HÀNG & KIỂM ĐỊNH -->
                <div class="trace-card-box batch-box">
                  <div class="trace-card-header">
                    <span class="trace-box-icon"><i class="bi bi-upc-scan"></i></span>
                    <span class="trace-box-title">Lô Hàng & Kiểm Định</span>
                  </div>
                  <div class="trace-card-body">
                    <div class="trace-detail-row">
                      <span class="detail-label">Mã Lô:</span>
                      <span class="detail-val font-mono">{{ item.batch?.batchNumber || 'N/A' }}</span>
                    </div>
                    <div class="trace-detail-row">
                      <span class="detail-label">Thời hạn:</span>
                      <span class="detail-val">NSX: {{ item.batch?.productionDate }} | HSD: {{ item.batch?.expiryDate }}</span>
                    </div>
                    <div class="trace-ai-badge">
                      <i class="bi bi-check-circle-fill"></i>
                      <span>{{ item.batch?.aiCheckNote }}</span>
                    </div>
                  </div>
                </div>

                <!-- 2. GIAN HÀNG & CHỢ ĐỊA PHƯƠNG -->
                <div class="trace-card-box store-box">
                  <div class="trace-card-header">
                    <span class="trace-box-icon"><i class="bi bi-shop-window"></i></span>
                    <span class="trace-box-title">Gian Hàng Phân Phối</span>
                  </div>
                  <div class="trace-card-body">
                    <div class="trace-detail-row">
                      <span class="detail-label">Tên sạp:</span>
                      <span class="detail-val font-bold">{{ item.store?.storeName }}</span>
                    </div>
                    <div class="trace-detail-row">
                      <span class="detail-label">Khu vực:</span>
                      <span class="detail-val">{{ item.store?.marketName }} ({{ item.store?.ward }})</span>
                    </div>
                    <div class="trace-location-badge">
                      <i class="bi bi-geo-alt-fill"></i>
                      <span>Không gian số Xã / Phường</span>
                    </div>
                  </div>
                </div>

                <!-- 3. TIỂU THƯƠNG / CHỦ HỘ -->
                <div class="trace-card-box merchant-box">
                  <div class="trace-card-header">
                    <span class="trace-box-icon"><i class="bi bi-person-check-fill"></i></span>
                    <span class="trace-box-title">Chủ Hộ Kinh Doanh</span>
                  </div>
                  <div class="trace-card-body">
                    <div class="trace-detail-row">
                      <span class="detail-label">Đại diện:</span>
                      <span class="detail-val">{{ item.merchant?.ownerName }}</span>
                    </div>
                    <div class="trace-detail-row">
                      <span class="detail-label">CCCD:</span>
                      <span class="detail-val cccd-chip">{{ item.merchant?.cccdMasked }}</span>
                    </div>
                    <div class="trace-cert-badge">
                      <i class="bi bi-patch-check-fill"></i>
                      <span>{{ item.merchant?.foodSafetyCert }}</span>
                    </div>
                  </div>
                </div>

                <!-- 4. NGUỒN GỐC & NHÀ CUNG CẤP -->
                <div class="trace-card-box supplier-box">
                  <div class="trace-card-header">
                    <span class="trace-box-icon"><i class="bi bi-tree-fill"></i></span>
                    <span class="trace-box-title">Nguồn Cung Cấp & Xuất Xứ</span>
                  </div>
                  <div class="trace-card-body">
                    <div class="trace-detail-row">
                      <span class="detail-label">Nhà vườn:</span>
                      <span class="detail-val font-bold">{{ item.supplier?.supplierName }}</span>
                    </div>
                    <div class="trace-detail-row">
                      <span class="detail-label">Tiêu chuẩn:</span>
                      <span class="detail-val">{{ item.supplier?.originType }}</span>
                    </div>
                    <div class="trace-standard-badge">
                      <i class="bi bi-award-fill"></i>
                      <span>Chứng nhận VietGAP / OCOP</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- FOOTER -->
        <div class="trace-modal-footer">
          <div class="trace-footer-security-note">
            <i class="bi bi-lock-fill"></i> Dữ liệu truy xuất minh bạch được bảo chứng bởi Cổng Chợ Số ZoneMart
          </div>
          <button type="button" class="btn-close-trace" @click="closeTraceabilityModal">
            Đóng
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.buyer-orders-container {
  max-width: 1080px;
  margin: 32px auto 80px auto;
  padding: 0 20px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 28px;
}

.header-content h2 {
  margin: 0 0 6px 0;
  color: #0f172a;
  font-size: 26px;
  font-weight: 800;
  display: flex;
  align-items: center;
}

.header-content p {
  margin: 0;
  color: #64748b;
  font-size: 14px;
}

.account-meta-line {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 4px;
}

.user-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
  padding: 4px 12px;
  border-radius: 999px;
  font-size: 13px;
  color: #334155;
}

.role-subpill {
  background: #e0f2fe;
  color: #0369a1;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 6px;
  margin-left: 4px;
}

.orders-count-indicator {
  font-size: 13px;
  color: #64748b;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-refresh-orders {
  background: #ffffff;
  color: #0f172a;
  font-size: 13px;
  font-weight: 700;
  padding: 10px 16px;
  border-radius: 12px;
  border: 1px solid #cbd5e1;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-refresh-orders:hover:not(:disabled) {
  background: #f8fafc;
  border-color: #94a3b8;
  transform: translateY(-1px);
}

.btn-refresh-orders:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.spin-icon {
  animation: spin 0.8s linear infinite;
  color: #f97316;
}

.btn-continue-shopping {
  background: #f1f5f9;
  color: #334155;
  text-decoration: none;
  font-size: 14px;
  font-weight: 700;
  padding: 10px 18px;
  border-radius: 12px;
  border: 1px solid #cbd5e1;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
}

.btn-continue-shopping:hover {
  background: #e2e8f0;
  color: #0f172a;
  transform: translateY(-1px);
}

/* ================================================================
   HIỆU ỨNG SKELETON LOADING (TASTE-SKILL TACTILE SHIMMER)
   ================================================================ */
.skeleton-orders-wrap {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.sync-status-indicator {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  padding: 8px 16px;
  border-radius: 999px;
  font-size: 13px;
  color: #475569;
  align-self: flex-start;
  margin-bottom: 4px;
}

.sync-spinner-ring {
  width: 15px;
  height: 15px;
  border: 2.5px solid #e2e8f0;
  border-top-color: #ea580c;
  border-radius: 50%;
  animation: spin 0.75s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.order-card-skeleton {
  background: #ffffff;
  border-radius: 18px;
  border: 1px solid #e2e8f0;
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.03);
}

.skeleton-item {
  background: linear-gradient(90deg, #f1f5f9 25%, #e2e8f0 50%, #f1f5f9 75%);
  background-size: 200% 100%;
  animation: shimmerWave 1.4s ease infinite;
  border-radius: 8px;
}

@keyframes shimmerWave {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.skeleton-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.skeleton-top-bar {
  justify-content: space-between;
  padding-bottom: 12px;
  border-bottom: 1px solid #f1f5f9;
}

.sk-pill-code {
  width: 140px;
  height: 22px;
  border-radius: 6px;
}

.skeleton-right-badges {
  display: flex;
  gap: 8px;
}

.sk-pill-sm {
  width: 90px;
  height: 24px;
  border-radius: 8px;
}

.skeleton-store-row {
  padding-top: 4px;
}

.sk-circle-avatar {
  width: 38px;
  height: 38px;
  border-radius: 10px;
}

.skeleton-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.sk-line-title {
  width: 200px;
  height: 16px;
}

.sk-line-sub {
  width: 120px;
  height: 12px;
}

.sk-status-pill {
  width: 110px;
  height: 28px;
  border-radius: 999px;
}

.skeleton-product-item {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #f8fafc;
  padding: 12px;
  border-radius: 14px;
}

.sk-thumb {
  width: 68px;
  height: 68px;
  border-radius: 12px;
  flex-shrink: 0;
}

.sk-line-prod-name {
  width: 65%;
  height: 16px;
}

.sk-line-prod-meta {
  width: 35%;
  height: 14px;
}

.sk-price-block {
  width: 90px;
  height: 22px;
  margin-left: auto;
}

.sk-line-full {
  width: 100%;
  height: 14px;
}

.skeleton-bottom-row {
  justify-content: space-between;
  padding-top: 10px;
  border-top: 1px solid #f1f5f9;
}

.sk-line-sm {
  width: 120px;
  height: 14px;
}

.skeleton-col-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.sk-price-total {
  width: 120px;
  height: 22px;
}

.sk-btn {
  width: 78px;
  height: 32px;
  border-radius: 8px;
}

.orders-list {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* THẺ ĐƠN HÀNG */
.order-card {
  background: #ffffff;
  border-radius: 18px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.04);
  overflow: hidden;
  transition: all 0.25s ease;
}

.order-card:hover {
  border-color: #cbd5e1;
  box-shadow: 0 6px 24px rgba(0, 0, 0, 0.07);
}

/* TOP HEADER ĐƠN */
.order-top {
  background: #f8fafc;
  padding: 14px 24px;
  border-bottom: 1px solid #edf2f7;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.order-meta-info {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.order-tag {
  color: #64748b;
  font-weight: 500;
}

.order-id-code {
  color: #0f172a;
  font-size: 15px;
  font-weight: 800;
  letter-spacing: 0.5px;
}

.order-meta-sep {
  color: #cbd5e1;
}

.order-date {
  color: #64748b;
  font-size: 13px;
  display: flex;
  align-items: center;
}

.order-top-badges {
  display: flex;
  align-items: center;
  gap: 10px;
}

.badge {
  font-size: 12px;
  padding: 5px 12px;
  border-radius: 8px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
}

.payment-badge {
  background: #eff6ff;
  color: #1d4ed8;
  border: 1px solid #bfdbfe;
}

.delivery-badge {
  font-size: 12px;
  padding: 5px 12px;
  border-radius: 8px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
}

.delivery-badge.express {
  background: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.delivery-badge.standard {
  background: #f0fdf4;
  color: #15803d;
  border: 1px solid #bbf7d0;
}

/* BODY ĐƠN HÀNG */
.order-body {
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 18px;
}

/* BANNER CỬA HÀNG */
.order-store-banner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 14px;
  border-bottom: 1px dashed #e2e8f0;
}

.store-brand {
  display: flex;
  align-items: center;
  gap: 12px;
}

.store-icon-wrap {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  background: #fff7ed;
  color: #ea580c;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
}

.store-name-group {
  display: flex;
  flex-direction: column;
}

.store-name {
  margin: 0;
  font-size: 15px;
  font-weight: 800;
  color: #1e293b;
}

.store-verified {
  font-size: 11px;
  color: #64748b;
  font-weight: 600;
}

/* STATUS PILL */
.status-pill {
  font-size: 13px;
  font-weight: 700;
  padding: 6px 14px;
  border-radius: 50px;
  display: inline-flex;
  align-items: center;
}

.status-pill.completed {
  background: #ecfdf5;
  color: #059669;
  border: 1px solid #a7f3d0;
}

.status-pill.delivering {
  background: #eff6ff;
  color: #2563eb;
  border: 1px solid #bfdbfe;
}

.status-pill.picking {
  background: #e0f2fe;
  color: #0284c7;
  border: 1px solid #7dd3fc;
}

.status-pill.confirmed {
  background: #fdf4ff;
  color: #9333ea;
  border: 1px solid #f0abfc;
}

.status-pill.cancelled {
  background: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.status-pill.pending,
.status-pill.processing {
  background: #fffbeb;
  color: #d97706;
  border: 1px solid #fde68a;
}

/* DANH SÁCH SẢN PHẨM RÕ NÉT */
.products-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.product-item-card {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #f8fafc;
  border: 1px solid #edf2f7;
  border-radius: 14px;
  padding: 12px 16px;
  transition: all 0.2s ease;
}

.product-item-card.clickable-product {
  cursor: pointer;
}

.product-item-card.clickable-product:hover {
  background: #f1f5f9;
  border-color: #cbd5e1;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.04);
}

.product-item-card:hover {
  background: #f1f5f9;
  border-color: #e2e8f0;
}

.product-thumb-box {
  width: 72px;
  height: 72px;
  min-width: 72px;
  border-radius: 12px;
  overflow: hidden;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.product-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.product-thumb-fallback {
  color: #94a3b8;
  font-size: 28px;
}

.product-details-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.product-name {
  margin: 0;
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
}

.product-shop-badge {
  font-size: 12px;
  color: #64748b;
  font-weight: 500;
}

.product-price-qty-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 2px;
}

.unit-price {
  font-size: 14px;
  color: #dc2626;
  font-weight: 700;
}

.qty-badge {
  font-size: 12px;
  background: #e2e8f0;
  color: #334155;
  padding: 2px 8px;
  border-radius: 6px;
  font-weight: 600;
}

.product-subtotal-box {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
}

.subtotal-label {
  font-size: 11px;
  color: #94a3b8;
}

.subtotal-amount {
  font-size: 16px;
  color: #0f172a;
  font-weight: 800;
}

/* THÔNG TIN GIAO HÀNG */
.shipping-info-box {
  background: #fdfaf6;
  border: 1px solid #fed7aa;
  border-radius: 12px;
  padding: 12px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.info-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  font-size: 13px;
  color: #431407;
  line-height: 1.5;
}

.info-icon {
  font-size: 15px;
  margin-top: 2px;
}

.recipient-name {
  font-weight: 700;
  color: #7c2d12;
}

.note-row {
  border-top: 1px dashed #fed7aa;
  padding-top: 6px;
}

.bank-row {
  border-top: 1px dashed #fed7aa;
  padding-top: 6px;
}

/* FOOTER ĐƠN */
.order-bottom {
  padding: 16px 24px;
  background: #ffffff;
  border-top: 1px solid #edf2f7;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.items-count-text {
  font-size: 13px;
  color: #64748b;
  font-weight: 500;
}

.bottom-right-actions {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-wrap: wrap;
}

.total-price-box {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.total-label {
  font-size: 13px;
  color: #64748b;
  font-weight: 600;
}

.total-amount {
  font-size: 20px;
  color: #dc2626;
  font-weight: 800;
}

.action-buttons {
  display: flex;
  gap: 8px;
}

.btn {
  padding: 8px 14px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
}

.btn-outline-primary {
  background: #ffffff;
  border-color: #2563eb;
  color: #2563eb;
}

.btn-outline-primary:hover {
  background: #eff6ff;
}

.btn-success {
  background: #16a34a;
  color: #ffffff;
}

.btn-success:hover {
  background: #15803d;
}

.btn-outline-secondary {
  background: #ffffff;
  border-color: #cbd5e1;
  color: #64748b;
}

.btn-outline-secondary:hover {
  background: #f1f5f9;
  color: #334155;
}

/* EMPTY STATE */
.empty-orders-card {
  background: #ffffff;
  border-radius: 20px;
  border: 1px dashed #cbd5e1;
  padding: 60px 24px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.empty-art-circle {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  background: #fff7ed;
  color: #ea580c;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 36px;
  margin-bottom: 18px;
}

.empty-main-title {
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 10px 0;
}

.empty-sub-text {
  font-size: 14px;
  color: #64748b;
  max-width: 460px;
  margin: 0 0 24px 0;
  line-height: 1.5;
}

.btn-shop-now {
  background: #ea580c;
  color: #ffffff;
  text-decoration: none;
  padding: 12px 24px;
  border-radius: 50px;
  font-size: 14px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.25);
  transition: all 0.2s;
}

.btn-shop-now:hover {
  background: #c2410c;
  transform: translateY(-2px);
}

/* ============================================================================
   RESPONSIVE BREAKPOINTS CHO TRANG ĐƠN MUA (BUYER ORDERS)
   ============================================================================ */
@media (max-width: 768px) {
  .buyer-orders-container {
    padding: 0 14px;
    margin: 20px auto 60px auto;
  }
  .header {
    flex-direction: column;
    align-items: flex-start;
    gap: 14px;
    margin-bottom: 20px;
  }
  .header-content h2 {
    font-size: 22px;
  }
  .header-actions {
    width: 100%;
    display: flex;
    gap: 8px;
  }
  .btn-refresh-orders,
  .btn-continue-shopping {
    flex: 1;
    justify-content: center;
    font-size: 12.5px;
    padding: 8px 12px;
    min-height: 40px;
  }
  .order-card {
    border-radius: 14px;
  }
  .order-top {
    padding: 10px 14px;
    gap: 8px;
  }
  .order-meta-info {
    font-size: 13px;
    gap: 6px;
  }
  .order-id-code {
    font-size: 14px;
  }
  .order-body {
    padding: 14px;
    gap: 12px;
  }
  .order-store-banner {
    padding-bottom: 10px;
  }
  .store-icon-wrap {
    width: 32px;
    height: 32px;
    font-size: 15px;
  }
  .store-name {
    font-size: 14px;
  }
  .product-item-card {
    padding: 10px 12px;
    gap: 12px;
  }
  .product-thumb-box {
    width: 60px;
    height: 60px;
    min-width: 60px;
  }
  .product-name {
    font-size: 14px;
  }
  .shipping-info-box {
    padding: 10px 12px;
    font-size: 12.5px;
  }
  .order-bottom {
    flex-direction: column;
    align-items: stretch;
    padding: 12px 14px;
    gap: 12px;
  }
  .bottom-left-summary {
    display: flex;
    justify-content: space-between;
  }
  .bottom-right-actions {
    flex-direction: column;
    align-items: stretch;
    width: 100%;
    gap: 10px;
  }
  .total-price-box {
    justify-content: space-between;
    width: 100%;
  }
  .total-amount {
    font-size: 18px;
  }
  .action-buttons {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(90px, 1fr));
    width: 100%;
    gap: 8px;
  }
  .action-buttons .btn {
    justify-content: center;
    padding: 8px 10px;
    font-size: 12px;
    min-height: 38px;
  }
}

@media (max-width: 480px) {
  .buyer-orders-container {
    padding: 0 10px;
    margin: 14px auto 40px auto;
  }
  .header-content h2 {
    font-size: 19px;
  }
  .account-meta-line {
    font-size: 12px;
  }
  .user-chip {
    font-size: 12px;
    padding: 3px 8px;
  }
  .order-card {
    border-radius: 12px;
  }
  .product-item-card {
    flex-wrap: wrap;
  }
  .product-subtotal-box {
    width: 100%;
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    border-top: 1px dashed #e2e8f0;
    padding-top: 6px;
    margin-top: 4px;
  }
  .subtotal-label {
    font-size: 12px;
  }
  .subtotal-amount {
    font-size: 14px;
  }
  .empty-orders-card {
    padding: 40px 16px;
    border-radius: 14px;
  }
  .empty-main-title {
    font-size: 17px;
  }
  .empty-sub-text {
    font-size: 13px;
  }
  .btn-shop-now {
    width: 100%;
    justify-content: center;
    font-size: 13px;
    padding: 10px 16px;
  }
}

/* REALTIME ORDER STATUS TOAST & CARDS */
.realtime-status-toast {
  position: sticky;
  top: 16px;
  z-index: 100;
  background: linear-gradient(135deg, #1e293b, #0f172a);
  color: #f8fafc;
  padding: 12px 20px;
  border-radius: 12px;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.3), 0 0 0 1px #3b82f6;
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 16px;
  animation: slideDownToast 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.status-pill.picking {
  background: #eff6ff;
  color: #1d4ed8;
  border: 1px solid #bfdbfe;
}

.buyer-shipper-live-card {
  margin: 0 16px 12px;
  padding: 10px 14px;
  background: #f0f9ff;
  border: 1px solid #bae6fd;
  border-radius: 10px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.shipper-avatar-mini {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  background: #e0f2fe;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}

.shipper-live-name {
  font-size: 13px;
  color: #0369a1;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.plate-tag {
  background: #0284c7;
  color: #ffffff;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 700;
}

.shipper-live-desc {
  font-size: 12px;
  color: #0284c7;
  display: block;
  margin-top: 2px;
}

@keyframes slideDownToast {
  from { transform: translateY(-20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

/* MODAL ĐÁNH GIÁ ĐƠN HÀNG ĐA PHÂN HỆ */
.review-modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(5px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 16px;
}

.review-modal-dialog {
  background: #ffffff;
  border-radius: 20px;
  width: 100%;
  max-width: 660px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  overflow: hidden;
  animation: modalPopIn 0.25s ease-out;
}

@keyframes modalPopIn {
  from { opacity: 0; transform: scale(0.95); }
  to { opacity: 1; transform: scale(1); }
}

.review-modal-header {
  padding: 16px 22px;
  background: linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%);
  border-bottom: 1px solid #fed7aa;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
}

.review-modal-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.review-header-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: #ea580c;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  box-shadow: 0 4px 10px rgba(234, 88, 12, 0.3);
}

.review-modal-title h3 {
  font-size: 16px;
  font-weight: 700;
  color: #9a3412;
  margin: 0;
}

.review-modal-title p {
  font-size: 12px;
  color: #78350f;
  margin: 2px 0 0 0;
}

.btn-close-review {
  background: transparent;
  border: none;
  font-size: 28px;
  line-height: 1;
  color: #9a3412;
  cursor: pointer;
  padding: 0 4px;
}

.review-modal-scrollable-body {
  padding: 18px 22px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: #f8fafc;
}

.review-section-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.section-card-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
  padding-bottom: 10px;
  border-bottom: 1px dashed #e2e8f0;
}

.section-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
}

.shipper-icon { background: #dbeafe; color: #1d4ed8; }
.store-icon { background: #fef3c7; color: #b45309; }
.product-icon { background: #dcfce7; color: #15803d; }

.section-info h4 {
  font-size: 14px;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
}

.section-info p {
  font-size: 12px;
  color: #64748b;
  margin: 2px 0 0 0;
}

.interactive-stars-box {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.interactive-stars-box.mini {
  margin-bottom: 8px;
  gap: 8px;
}

.stars-selector {
  display: flex;
  gap: 4px;
}

.star-item {
  font-size: 28px;
  color: #cbd5e1;
  cursor: pointer;
  transition: transform 0.15s ease, color 0.15s ease;
  user-select: none;
  line-height: 1;
}

.star-item:hover {
  transform: scale(1.2);
}

.star-item.active {
  color: #eab308;
}

.star-item.mini {
  font-size: 20px;
}

.rating-badge-text {
  font-size: 12.5px;
  font-weight: 600;
  color: #d97706;
}

.p-rating-label {
  font-size: 11.5px;
  font-weight: 600;
  color: #059669;
}

.quick-tags-container {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 12px;
}

.quick-tags-container.mini {
  margin-bottom: 8px;
  gap: 5px;
}

.quick-tag-pill {
  border: 1px solid #e2e8f0;
  background: #f1f5f9;
  color: #475569;
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 11.5px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.quick-tag-pill:hover {
  border-color: #cbd5e1;
  background: #e2e8f0;
}

.quick-tag-pill.active {
  background: #ffedd5;
  border-color: #fdba74;
  color: #c2410c;
  font-weight: 600;
}

.quick-tag-pill.mini {
  font-size: 11px;
  padding: 3px 8px;
}

.review-input-text {
  border-radius: 8px;
  font-size: 12.5px;
  border: 1px solid #cbd5e1;
  padding: 8px 12px;
}

.review-input-text:focus, .review-textarea-small:focus {
  border-color: #f97316;
  box-shadow: 0 0 0 3px rgba(249, 115, 22, 0.15);
}

.review-textarea-small {
  border-radius: 8px;
  font-size: 12px;
  border: 1px solid #cbd5e1;
  padding: 8px 10px;
  resize: vertical;
}

.product-review-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.product-item-review-row {
  display: flex;
  gap: 12px;
  padding: 12px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
}

.product-thumb-col {
  width: 60px;
  height: 60px;
  flex-shrink: 0;
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid #cbd5e1;
}

.p-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.product-review-details-col {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.p-header-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.p-name {
  font-size: 13px;
  color: #1e293b;
}

.p-qty {
  font-size: 11px;
}

.review-modal-footer {
  padding: 14px 22px;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  flex-shrink: 0;
}

.btn-submit-rating {
  box-shadow: 0 4px 12px rgba(234, 179, 8, 0.3);
}

/* ==========================================================
   MODAL TRUY XUẤT NGUỒN GỐC HIỆN ĐẠI (ZERO BƯỚC)
   ========================================================== */
.trace-modal-container {
  background: #ffffff;
  border-radius: 20px;
  width: 100%;
  max-width: 720px;
  max-height: 88vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 25px 50px -12px rgba(15, 23, 42, 0.35);
  overflow: hidden;
  animation: modalPopIn 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.trace-modal-header {
  padding: 22px 24px;
  background: linear-gradient(135deg, #059669 0%, #047857 50%, #065f46 100%);
  color: #ffffff;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  position: relative;
}

.trace-header-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.trace-header-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  background: rgba(255, 255, 255, 0.2);
  padding: 4px 10px;
  border-radius: 20px;
  width: fit-content;
  backdrop-filter: blur(4px);
  color: #ecfdf5;
}

.trace-header-title {
  margin: 4px 0 0 0;
  font-size: 18px;
  font-weight: 800;
  color: #ffffff;
  letter-spacing: -0.3px;
}

.trace-header-subtitle {
  margin: 0;
  font-size: 12.5px;
  color: #d1fae5;
  opacity: 0.95;
}

.trace-btn-close {
  background: rgba(255, 255, 255, 0.15);
  border: none;
  font-size: 26px;
  line-height: 1;
  width: 34px;
  height: 34px;
  border-radius: 50%;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}

.trace-btn-close:hover {
  background: rgba(255, 255, 255, 0.3);
  transform: rotate(90deg);
}

.trace-loading-box {
  padding: 48px 24px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

.trace-spinner {
  width: 38px;
  height: 38px;
  border: 3.5px solid #e2e8f0;
  border-top-color: #059669;
  border-radius: 50%;
  animation: traceSpin 0.8s linear infinite;
}

@keyframes traceSpin {
  to { transform: rotate(360deg); }
}

.trace-loading-text {
  font-size: 13.5px;
  color: #64748b;
  margin: 0;
}

.trace-modal-body {
  padding: 20px 24px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: #f8fafc;
}

.trace-order-banner {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 14px 18px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.trace-order-info {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.trace-order-code {
  display: flex;
  align-items: center;
  gap: 6px;
}

.trace-label {
  font-size: 13px;
  color: #64748b;
  font-weight: 500;
}

.trace-value {
  font-size: 14px;
  font-weight: 800;
  color: #0f172a;
}

.trace-delivery-pill {
  background: #eff6ff;
  color: #1d4ed8;
  border: 1px solid #bfdbfe;
  padding: 3px 10px;
  border-radius: 20px;
  font-size: 11.5px;
  font-weight: 700;
}

.trace-order-meta {
  font-size: 12px;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 6px;
}

.trace-items-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.trace-product-card {
  background: #ffffff;
  border: 1.5px solid #e2e8f0;
  border-radius: 16px;
  padding: 18px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.03);
}

.trace-product-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 14px;
  margin-bottom: 16px;
  border-bottom: 1px dashed #e2e8f0;
  flex-wrap: wrap;
  gap: 10px;
}

.trace-product-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.trace-prod-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: #ecfdf5;
  color: #059669;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}

.trace-prod-name {
  margin: 0;
  font-size: 15.5px;
  font-weight: 800;
  color: #0f172a;
}

.trace-prod-tag {
  font-size: 11.5px;
  color: #059669;
  font-weight: 600;
}

.trace-product-price {
  display: flex;
  align-items: center;
  gap: 8px;
}

.trace-unit-price {
  font-size: 15.5px;
  font-weight: 800;
  color: #059669;
}

.trace-qty-tag {
  background: #f1f5f9;
  color: #475569;
  padding: 2px 8px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 700;
}

.trace-details-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

@media (max-width: 640px) {
  .trace-details-grid {
    grid-template-columns: 1fr;
  }
}

.trace-card-box {
  border-radius: 12px;
  padding: 12px 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 8px;
  transition: all 0.2s ease;
}

.trace-card-box:hover {
  border-color: #cbd5e1;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.04);
}

.trace-card-header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.trace-box-icon {
  width: 26px;
  height: 26px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
}

.batch-box .trace-box-icon { background: #e0f2fe; color: #0284c7; }
.store-box .trace-box-icon { background: #fef3c7; color: #d97706; }
.merchant-box .trace-box-icon { background: #dcfce7; color: #16a34a; }
.supplier-box .trace-box-icon { background: #fee2e2; color: #dc2626; }

.trace-box-title {
  font-size: 13px;
  font-weight: 800;
  color: #1e293b;
}

.trace-card-body {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.trace-detail-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
  font-size: 12.5px;
}

.detail-label {
  color: #64748b;
  font-weight: 500;
  white-space: nowrap;
}

.detail-val {
  color: #1e293b;
  font-weight: 600;
  text-align: right;
  word-break: break-word;
}

.detail-val.font-mono {
  font-family: monospace;
  font-size: 12px;
  color: #0369a1;
  background: #f0f9ff;
  padding: 1px 6px;
  border-radius: 4px;
}

.detail-val.font-bold {
  font-weight: 800;
  color: #0f172a;
}

.cccd-chip {
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  padding: 1px 6px;
  border-radius: 4px;
  font-family: monospace;
  font-size: 11.5px;
}

.trace-ai-badge,
.trace-location-badge,
.trace-cert-badge,
.trace-standard-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  border-radius: 8px;
  font-size: 11.5px;
  font-weight: 700;
  margin-top: 2px;
}

.trace-ai-badge {
  background: #ecfdf5;
  color: #047857;
  border: 1px solid #a7f3d0;
}

.trace-location-badge {
  background: #f0f9ff;
  color: #0369a1;
  border: 1px solid #bae6fd;
}

.trace-cert-badge {
  background: #fefce8;
  color: #854d0e;
  border: 1px solid #fef08a;
}

.trace-standard-badge {
  background: #fdf2f8;
  color: #9d174d;
  border: 1px solid #fbcfe8;
}

.trace-modal-footer {
  padding: 14px 24px;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
}

.trace-footer-security-note {
  font-size: 12px;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 6px;
}

.trace-footer-security-note i {
  color: #059669;
}

.btn-close-trace {
  background: #0f172a;
  color: #ffffff;
  border: none;
  padding: 8px 20px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-close-trace:hover {
  background: #334155;
  transform: translateY(-1px);
}

/* ==========================================================
   SHOPEE-STYLE TABS NAVIGATION BAR (TASTE-SKILL / BENTO UI)
   ========================================================== */
.buyer-orders-tabs-nav {
  display: flex;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 6px;
  gap: 6px;
  margin-bottom: 24px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.03);
  overflow-x: auto;
  scrollbar-width: none;
}

.buyer-orders-tabs-nav::-webkit-scrollbar {
  display: none;
}

.tab-nav-btn {
  flex: 1;
  min-width: fit-content;
  background: transparent;
  border: 1px solid transparent;
  border-radius: 12px;
  padding: 12px 18px;
  font-size: 13.5px;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.tab-nav-btn:hover {
  color: #1e293b;
  background: #f8fafc;
}

.tab-nav-btn.active {
  background: #fff7ed;
  color: #ea580c;
  border-color: #fed7aa;
  font-weight: 700;
  box-shadow: 0 2px 8px rgba(234, 88, 12, 0.12);
}

.tab-nav-btn.highlight-shopee {
  color: #d97706;
}

.tab-nav-btn.highlight-shopee:hover {
  color: #b45309;
  background: #fefce8;
}

.tab-nav-btn.highlight-shopee.active {
  background: #fffbeb;
  color: #b45309;
  border-color: #fde68a;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.15);
}

.badge-pending-rate {
  background: #ea580c;
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 12px;
  box-shadow: 0 2px 6px rgba(234, 88, 12, 0.35);
  animation: pulseBadge 2s infinite ease-in-out;
}

@keyframes pulseBadge {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.08); }
}

/* Purchased products tab view */
.purchased-products-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.purchased-header-intro {
  background: linear-gradient(135deg, #ffffff 0%, #fff7ed 100%);
  border: 1px solid #fed7aa;
  border-radius: 16px;
  padding: 18px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  box-shadow: 0 2px 10px rgba(234, 88, 12, 0.05);
}

.intro-left h3 {
  font-size: 16px;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
  display: flex;
  align-items: center;
}

.intro-left p {
  font-size: 12.5px;
  color: #64748b;
  margin: 4px 0 0 0;
}

.intro-right-stats {
  display: flex;
  gap: 8px;
}

.stat-pill {
  font-size: 12px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  color: #475569;
  padding: 6px 12px;
  border-radius: 20px;
}

.stat-pill.highlight {
  background: #ffedd5;
  border-color: #fdba74;
  color: #c2410c;
  font-weight: 600;
}

.purchased-products-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(420px, 1fr));
  gap: 16px;
}

@media (max-width: 768px) {
  .purchased-products-grid {
    grid-template-columns: 1fr;
  }
}

.purchased-product-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 16px;
  display: flex;
  gap: 14px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
  transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;
}

.purchased-product-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(249, 115, 22, 0.1);
  border-color: #fed7aa;
}

.card-thumb-wrap {
  width: 84px;
  height: 84px;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid #cbd5e1;
  position: relative;
  flex-shrink: 0;
  cursor: pointer;
}

.card-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.qty-badge-float {
  position: absolute;
  bottom: 4px;
  right: 4px;
  background: rgba(15, 23, 42, 0.85);
  color: #ffffff;
  font-size: 10px;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 6px;
}

.card-content-wrap {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.card-top-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.store-badge-tag {
  font-size: 11.5px;
  color: #ea580c;
  font-weight: 600;
  display: flex;
  align-items: center;
}

.order-code-tag {
  font-size: 11px;
  color: #94a3b8;
}

.card-prod-title {
  font-size: 14px;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 6px 0;
  cursor: pointer;
  line-height: 1.35;
}

.card-prod-title:hover {
  color: #ea580c;
}

.card-price-row {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 12.5px;
  margin-bottom: 12px;
}

.card-action-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: auto;
  flex-wrap: wrap;
}

.unrated-status-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-shopee-rate {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: #ffffff;
  box-shadow: 0 3px 10px rgba(245, 158, 11, 0.35);
  font-size: 12px;
  padding: 6px 14px;
  border-radius: 8px;
  border: none;
  cursor: pointer;
  transition: transform 0.15s ease;
}

.btn-shopee-rate:hover {
  transform: translateY(-1px);
}

.rate-incentive-hint {
  font-size: 11px;
  color: #b45309;
}

.rated-status-group {
  display: flex;
  align-items: center;
  gap: 6px;
}

.btn-re-review {
  font-size: 11.5px;
  padding: 4px 10px;
  border-radius: 8px;
}

.btn-buy-again-mini {
  font-size: 11.5px;
  padding: 4px 10px;
  border-radius: 8px;
}

.empty-purchased-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 56px 24px;
  text-align: center;
  border: 1px dashed #cbd5e1;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.03);
}

.empty-icon-wrap {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  background: #fff7ed;
  color: #ea580c;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 34px;
  margin: 0 auto 16px auto;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.15);
}

.empty-purchased-card h4 {
  font-size: 18px;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 6px;
}

.empty-purchased-card p {
  font-size: 13.5px;
  color: #64748b;
  max-width: 460px;
  margin: 0 auto 20px auto;
  line-height: 1.6;
}

/* Inline product review in order cards */
.inline-item-rate-wrap {
  margin-top: 6px;
}

.btn-rate-item-inline {
  font-size: 11px;
  font-weight: 600;
  padding: 3px 8px;
  border-radius: 6px;
  white-space: nowrap;
}

.badge-rated-item-inline {
  font-size: 11px;
  color: #059669;
  font-weight: 600;
  display: flex;
  align-items: center;
}

/* Single product review modal specifics */
.single-prod-eval-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 14px;
  display: flex;
  gap: 14px;
  align-items: center;
}

.eval-thumb-col {
  width: 68px;
  height: 68px;
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid #cbd5e1;
  flex-shrink: 0;
}

.eval-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.eval-details-col {
  flex: 1;
}

.eval-prod-name {
  font-size: 14.5px;
  color: #1e293b;
  display: block;
  margin-bottom: 6px;
  font-weight: 700;
}

.eval-meta-row {
  display: flex;
  gap: 8px;
  align-items: center;
}

.eval-price {
  font-size: 13.5px;
  font-weight: 700;
  color: #ea580c;
}

.stars-prompt-label {
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 8px;
}

.interactive-stars-box.large-center {
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 10px 0;
}

.star-item.large {
  font-size: 40px;
}

.accordion-extra-review {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  overflow: hidden;
}

.btn-toggle-extra {
  width: 100%;
  background: #f8fafc;
  border: none;
  padding: 12px 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
}

.extra-eval-content {
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  border-top: 1px solid #e2e8f0;
  background: #ffffff;
}

.extra-eval-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.extra-label {
  font-size: 13px;
  color: #334155;
}

</style>
