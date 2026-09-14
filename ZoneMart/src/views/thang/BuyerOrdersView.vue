<script setup lang="ts">
/**
 * ================================================================
 * QUẢN LÝ ĐƠN MUA (BUYER ORDERS) - Phụ trách: Thắng
 * Hiển thị đầy đủ hình ảnh, tên, giá, số lượng, cửa hàng và địa chỉ
 * ================================================================
 */
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useAuth } from "../../composables/useAuth";
import { useProductCatalog, type CatalogProduct } from "../../composables/useProductCatalog";

const auth = useAuth();
const router = useRouter();
const catalog = useProductCatalog();
const orders = ref<any[]>([]);

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
  if (!status) return "processing";
  const s = String(status).toLowerCase();
  if (s.includes("thanh toán") || s.includes("completed") || s.includes("thành công")) return "completed";
  if (s.includes("delivering") || s.includes("giao")) return "delivering";
  return "processing";
};

const getOrderStatusText = (status: any): string => {
  if (!status) return "Đang xử lý";
  if (typeof status === "string") {
    const clean = status.replace(/\s*\(TPBank\)/gi, "").replace(/\s*TPBank/gi, "").trim();
    if (clean.includes("Đã thanh toán")) return "Đã thanh toán";
    if (clean === "completed") return "Giao hàng thành công";
    if (clean === "delivering") return "Đang giao hàng (Hỏa tốc)";
    return clean;
  }
  return "Đang xử lý";
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

const loadBuyerOrders = () => {
  const acc = auth.currentUser.value;
  const ownerKey = (acc?.phoneEmail || acc?.id || "guest").toLowerCase().trim();
  const isNew = !acc || !["usr_buyer_01", "usr_seller_01", "usr_shipper_01", "usr_admin_01"].includes(acc.id);
  const key = "zonemart_profile_orders_" + ownerKey;

  const raw = localStorage.getItem(key);
  if (raw) {
    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) {
        if (isNew && parsed.every((o: any) => o.id === "ZM-9982" || o.id === "ZM-9812" || o.orderId === "ORD_98213")) {
          orders.value = [];
          return;
        }
        orders.value = parsed;
        return;
      }
    } catch {}
  }

  // Nếu là tài khoản mới: Mặc định 0 đơn hàng
  if (isNew) {
    orders.value = [];
    return;
  }

  // Chỉ nạp đơn mẫu cho tài khoản demo
  orders.value = [
    {
      orderId: "ORD_98213",
      date: "07/09/2026 18:30",
      total: 395000,
      paymentMethod: "Chuyển khoản TPBank",
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
  ];
};

onMounted(() => {
  loadBuyerOrders();
});

const handleConfirmReceived = (id: string) => {
  alert(`Cảm ơn bạn đã xác nhận nhận hàng cho đơn #${id}! Đơn hàng đã hoàn tất.`);
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
    <div class="header">
      <div class="header-content">
        <h2><i class="bi bi-box-seam-fill me-2 text-primary"></i>Đơn Mua Của Bạn</h2>
        <p>Theo dõi trực tiếp chi tiết sản phẩm, hình ảnh và trạng thái các đơn hàng của bạn.</p>
      </div>
      <router-link to="/products" class="btn-continue-shopping">
        <i class="bi bi-cart-plus me-1"></i> Mua thêm nông sản
      </router-link>
    </div>

    <!-- DANH SÁCH ĐƠN HÀNG -->
    <div v-if="orders.length > 0" class="orders-list">
      <div v-for="order in orders" :key="order.orderId || order.id" class="order-card">
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
              <i v-else class="bi bi-hourglass-split me-1"></i>
              <span>{{ getOrderStatusText(order.status || order.statusText) }}</span>
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

              <!-- Thành tiền sản phẩm -->
              <div class="product-subtotal-box" v-if="item.price > 0">
                <span class="subtotal-label">Thành tiền:</span>
                <strong class="subtotal-amount">{{ formatPrice((item.price || 0) * (item.quantity || 1)) }}</strong>
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
              <button class="btn btn-outline-primary btn-sm" @click="handleBuyAgain(order)" title="Xem và mua lại sản phẩm này">
                <i class="bi bi-arrow-repeat me-1"></i> Mua Lại
              </button>
              <button 
                v-if="getOrderStatusClass(order.status) === 'delivering'" 
                class="btn btn-success btn-sm" 
                @click="handleConfirmReceived(order.orderId || order.id)"
              >
                <i class="bi bi-check-circle-fill me-1"></i> Đã nhận hàng
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
</style>
