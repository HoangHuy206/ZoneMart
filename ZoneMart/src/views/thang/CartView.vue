<script setup lang="ts">
/**
 * ============================================================================
 * GIỎ HÀNG (CART VIEW) - ZONEMART
 * Phong cách thiết kế: Warm Humanist & Terracotta (Nông sản & Chợ địa phương 10km)
 * Phụ trách: Thắng
 * ============================================================================
 */
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useCart } from '../../composables/useCart';

const router = useRouter();
const cart = useCart();

// State nhập mã giảm giá
const voucherInput = ref('');
const voucherAlert = ref<{ type: 'success' | 'error'; text: string } | null>(
  null,
);

// State hiển thị Toast thông báo thêm món nhanh
const toastMessage = ref('');
const showToast = ref(false);

const triggerToast = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2400);
};

// Áp dụng voucher từ input hoặc chọn nhanh
const handleApplyVoucher = (code?: string) => {
  const codeToApply = code || voucherInput.value;
  if (!codeToApply.trim()) {
    voucherAlert.value = { type: 'error', text: 'Vui lòng nhập mã giảm giá!' };
    return;
  }
  const res = cart.applyVoucher(codeToApply);
  if (res.success) {
    voucherAlert.value = { type: 'success', text: res.message };
    voucherInput.value = '';
    triggerToast(res.message);
  } else {
    voucherAlert.value = { type: 'error', text: res.message };
  }
};

// Danh sách 4 sản phẩm gợi ý mua kèm (Cross-sell)
const suggestedProducts = [
  {
    storeId: 'st_1',
    storeName: 'ZoneMart Bách Hóa Cầu Giấy',
    distanceKm: 1.2,
    deliveryTime: '15 - 20 phút',
    id: 'p_egg',
    name: 'Trứng Gà Ta Ăn Thóc Chuẩn Sạch (Hộp 10 quả)',
    price: 38000,
    originalPrice: 48000,
    unit: 'Hộp 10 quả',
    image:
      'https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?auto=format&fit=crop&w=400&q=80',
  },
  {
    storeId: 'st_2',
    storeName: 'Siêu Thị Trái Cây Xanh',
    distanceKm: 2.5,
    deliveryTime: '20 - 25 phút',
    id: 'p4',
    name: 'Nước Ép Cam Sành Tươi Nguyên Chất 100%',
    price: 32000,
    originalPrice: 40000,
    unit: 'Chai 350ml',
    image:
      'https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=400&q=80',
  },
  {
    storeId: 'st_1',
    storeName: 'ZoneMart Bách Hóa Cầu Giấy',
    distanceKm: 1.2,
    deliveryTime: '15 - 20 phút',
    id: 'p_veg',
    name: 'Rau Muống Hữu Cơ Ba Vì Chuẩn VietGAP',
    price: 18000,
    originalPrice: 22000,
    unit: 'Bó 500g',
    image:
      'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80',
  },
  {
    storeId: 'st_3',
    storeName: 'Tiệm Bánh Mì Zone',
    distanceKm: 2.8,
    deliveryTime: '15 - 20 phút',
    id: 'p3',
    name: 'Combo Bánh Mì Chảo Nóng Hổi Kèm Pate',
    price: 45000,
    originalPrice: 55000,
    unit: 'Phần 1 người',
    image:
      'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=400&q=80',
  },
];

const handleAddSuggested = (prod: any) => {
  cart.addItem(
    {
      storeId: prod.storeId,
      storeName: prod.storeName,
      distanceKm: prod.distanceKm,
      deliveryTime: prod.deliveryTime,
    },
    {
      id: prod.id,
      name: prod.name,
      price: prod.price,
      originalPrice: prod.originalPrice,
      unit: prod.unit,
      image: prod.image,
    },
  );
  triggerToast(`Đã thêm "${prod.name}" vào giỏ hàng!`);
};

// Chuyển sang trang thanh toán
const handleProceedToCheckout = () => {
  if (cart.selectedItemsCount.value === 0) {
    alert('Vui lòng chọn ít nhất 1 sản phẩm để tiến hành thanh toán!');
    return;
  }
  router.push('/checkout');
};
</script>

<template>
  <div class="cart-page-wrapper">
    <!-- 1. THANH TIẾN TRÌNH ĐẶT HÀNG (STEPPER) -->
    <div class="checkout-stepper-container">
      <div class="stepper-track">
        <div class="step-node active">
          <div class="node-circle"><i class="bi bi-cart3"></i></div>
          <span class="node-label">1. Giỏ Hàng</span>
        </div>
        <div class="step-connector"></div>
        <div class="step-node">
          <div class="node-circle"><i class="bi bi-credit-card"></i></div>
          <span class="node-label">2. Thanh Toán</span>
        </div>
        <div class="step-connector"></div>
        <div class="step-node">
          <div class="node-circle"><i class="bi bi-truck"></i></div>
          <span class="node-label">3. Giao Hỏa Tốc</span>
        </div>
      </div>
    </div>

    <!-- 2. NỘI DUNG CHÍNH NẾU CÓ SẢN PHẨM -->
    <div v-if="cart.cartStores.value.length > 0" class="cart-main-grid">
      <!-- CỘT TRÁI: DANH SÁCH SẢN PHẨM THEO TỪNG CỬA HÀNG -->
      <div class="cart-items-column">
        <!-- Banner thông báo Freeship thông minh -->
        <div class="freeship-smart-banner">
          <div class="banner-icon-wrap">
            <i class="bi bi-lightning-charge-fill"></i>
          </div>
          <div class="banner-content">
            <div class="banner-title-row">
              <span v-if="cart.freeshipRemaining.value > 0" class="banner-text">
                Mua thêm
                <strong
                  >{{
                    cart.freeshipRemaining.value.toLocaleString('vi-VN')
                  }}
                  ₫</strong
                >
                để nhận ưu đãi <strong>MIỄN PHÍ GIAO HÀNG HỎA TỐC 10KM</strong>!
              </span>
              <span v-else class="banner-text text-success-bold">
                🎉 Đơn hàng của bạn đã đạt điều kiện
                <strong>MIỄN PHÍ VẬN CHUYỂN HỎA TỐC 10KM</strong>!
              </span>
            </div>
            <div class="progress-bar-bg">
              <div
                class="progress-bar-fill"
                :style="{ width: `${cart.freeshipProgress.value}%` }"
              ></div>
            </div>
          </div>
        </div>

        <!-- Thanh công cụ giỏ hàng: Chọn tất cả & Thao tác nhanh -->
        <div class="cart-toolbar-card">
          <label class="custom-checkbox-wrap">
            <input
              type="checkbox"
              v-model="cart.isAllSelected.value"
              class="real-checkbox"
            />
            <span class="custom-checkmark"></span>
            <span class="toolbar-select-all-text">
              Chọn tất cả (<strong>{{ cart.totalCount.value }}</strong> sản
              phẩm)
            </span>
          </label>
          <button
            v-if="cart.selectedItemsCount.value > 0"
            type="button"
            class="btn-clear-selected"
            @click="cart.clearCart()"
            title="Xóa toàn bộ giỏ hàng"
          >
            <i class="bi bi-trash3"></i> Xóa toàn bộ
          </button>
        </div>

        <!-- DANH SÁCH TỪNG CỬA HÀNG (SUB-ORDERS) -->
        <div
          v-for="store in cart.cartStores.value"
          :key="store.storeId"
          class="store-group-panel"
        >
          <!-- HEADER CỬA HÀNG -->
          <div class="store-panel-head">
            <div class="store-head-left">
              <label class="custom-checkbox-wrap store-checkbox">
                <input
                  type="checkbox"
                  :checked="store.items.every((i) => i.selected)"
                  @change="
                    (e: any) =>
                      cart.toggleStoreSelect(store.storeId, e.target.checked)
                  "
                  class="real-checkbox"
                />
                <span class="custom-checkmark"></span>
              </label>
              <div class="store-info-title">
                <span class="store-badge-icon"><i class="bi bi-shop"></i></span>
                <strong class="store-name-txt">{{ store.storeName }}</strong>
                <span class="store-distance-badge">
                  <i class="bi bi-geo-alt-fill"></i> Cách bạn
                  {{ store.distanceKm }} km
                </span>
              </div>
            </div>
            <div class="store-head-right">
              <span class="express-eta-tag">
                <i class="bi bi-lightning-charge-fill"></i>
                {{ store.deliveryTime }}
              </span>
            </div>
          </div>

          <!-- DANH SÁCH MÓN TRONG CỬA HÀNG -->
          <div class="store-items-body">
            <div
              v-for="item in store.items"
              :key="item.id"
              class="cart-item-row"
              :class="{ 'item-unselected': !item.selected }"
            >
              <!-- Checkbox chọn từng món -->
              <label class="custom-checkbox-wrap item-checkbox">
                <input
                  type="checkbox"
                  v-model="item.selected"
                  class="real-checkbox"
                />
                <span class="custom-checkmark"></span>
              </label>

              <!-- Ảnh món -->
              <div class="item-thumb-box">
                <img
                  :src="item.image"
                  :alt="item.name"
                  class="item-thumb-img"
                />
              </div>

              <!-- Chi tiết món -->
              <div class="item-meta-box">
                <h4 class="item-name-heading" :title="item.name">
                  {{ item.name }}
                </h4>
                <div class="item-unit-tag" v-if="item.unit">
                  Quy cách: {{ item.unit }}
                </div>
                <div class="item-price-row">
                  <span class="current-price"
                    >{{ item.price.toLocaleString('vi-VN') }} ₫</span
                  >
                  <span v-if="item.originalPrice" class="original-price">
                    {{ item.originalPrice.toLocaleString('vi-VN') }} ₫
                  </span>
                </div>
              </div>

              <!-- Bộ tăng giảm số lượng -->
              <div class="item-quantity-stepper">
                <button
                  type="button"
                  class="stepper-btn"
                  @click="cart.decreaseQty(store.storeId, item.id)"
                  title="Giảm số lượng"
                  aria-label="Giảm số lượng"
                >
                  <i class="bi bi-dash"></i>
                </button>
                <span class="stepper-value">{{ item.quantity }}</span>
                <button
                  type="button"
                  class="stepper-btn"
                  @click="cart.increaseQty(store.storeId, item.id)"
                  title="Tăng số lượng"
                  aria-label="Tăng số lượng"
                >
                  <i class="bi bi-plus"></i>
                </button>
              </div>

              <!-- Thành tiền -->
              <div class="item-line-total">
                {{ (item.price * item.quantity).toLocaleString('vi-VN') }} ₫
              </div>

              <!-- Nút xóa món -->
              <button
                type="button"
                class="btn-delete-item"
                @click="cart.removeItem(store.storeId, item.id)"
                title="Xóa món khỏi giỏ"
                aria-label="Xóa món"
              >
                <i class="bi bi-trash3-fill"></i>
              </button>
            </div>
          </div>

          <!-- LỜI NHẮN CHO QUÁN / NHÀ VƯỜN -->
          <div class="store-note-row">
            <div class="store-note-wrap">
              <i class="bi bi-chat-square-dots store-note-icon"></i>
              <input
                type="text"
                :value="store.note || ''"
                @input="
                  (e: any) =>
                    cart.updateStoreNote(store.storeId, e.target.value)
                "
                class="store-note-input"
                placeholder="Lời nhắn cho nhà vườn / tiệm (VD: Lấy quả chín vừa, thái lát sẵn...)"
              />
            </div>
          </div>
        </div>

        <!-- 3. KHU VỰC GỢI Ý MUA KÈM (CROSS-SELL) -->
        <div class="cart-suggestions-section">
          <div class="suggestions-header">
            <div class="suggestions-title-block">
              <span class="suggestions-badge">TIỆN ÍCH MUA KÈM</span>
              <h3 class="suggestions-heading">Thường Được Đặt Cùng Nhau</h3>
            </div>
            <router-link to="/products" class="see-more-link">
              Xem tất cả <i class="bi bi-chevron-right"></i>
            </router-link>
          </div>

          <div class="suggestions-grid">
            <div
              v-for="sug in suggestedProducts"
              :key="sug.id"
              class="suggested-card"
            >
              <div class="sug-img-wrap">
                <img :src="sug.image" :alt="sug.name" class="sug-img" />
              </div>
              <div class="sug-info">
                <div class="sug-store-name">{{ sug.storeName }}</div>
                <h4 class="sug-prod-name" :title="sug.name">{{ sug.name }}</h4>
                <div class="sug-price-row">
                  <span class="sug-price"
                    >{{ sug.price.toLocaleString('vi-VN') }} ₫</span
                  >
                  <span v-if="sug.originalPrice" class="sug-original"
                    >{{ sug.originalPrice.toLocaleString('vi-VN') }} ₫</span
                  >
                </div>
                <button
                  type="button"
                  class="btn-quick-add"
                  @click="handleAddSuggested(sug)"
                  title="Thêm nhanh vào giỏ"
                >
                  <i class="bi bi-cart-plus-fill"></i> Thêm Nhanh
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- CỘT PHẢI: TỔNG KẾT ĐƠN HÀNG (ORDER SUMMARY) -->
      <div class="cart-summary-column">
        <div class="order-summary-card">
          <h3 class="summary-card-title">Tóm Tắt Đơn Hàng</h3>

          <!-- KHUNG MÃ GIẢM GIÁ VOUCHER ZONEMART -->
          <div class="voucher-box-section">
            <label class="voucher-box-label">
              <i class="bi bi-ticket-perforated-fill"></i> Mã Giảm Giá ZoneMart
            </label>
            <div class="voucher-input-row">
              <input
                v-model="voucherInput"
                type="text"
                placeholder="Nhập mã giảm giá..."
                class="voucher-input-field"
                @keyup.enter="handleApplyVoucher()"
              />
              <button
                type="button"
                class="btn-apply-voucher"
                @click="handleApplyVoucher()"
              >
                Áp Dụng
              </button>
            </div>

            <!-- Thông báo trạng thái voucher -->
            <div
              v-if="voucherAlert"
              class="voucher-alert-msg"
              :class="voucherAlert.type"
            >
              <i
                :class="
                  voucherAlert.type === 'success'
                    ? 'bi-check-circle-fill'
                    : 'bi-exclamation-circle-fill'
                "
              ></i>
              <span>{{ voucherAlert.text }}</span>
            </div>

            <!-- Voucher đang kích hoạt -->
            <div v-if="cart.currentVoucher.value" class="active-voucher-pill">
              <div class="voucher-pill-content">
                <strong>{{ cart.currentVoucher.value.code }}</strong
                >:
                <span>{{ cart.currentVoucher.value.title }}</span>
              </div>
              <button
                type="button"
                class="btn-remove-voucher"
                @click="cart.removeVoucher()"
                title="Bỏ voucher"
              >
                <i class="bi bi-x"></i>
              </button>
            </div>

            <!-- Gợi ý voucher 1-chạm -->
            <div class="voucher-quick-list">
              <span class="quick-title">Mã ưu đãi gợi ý:</span>
              <div class="quick-badges-wrap">
                <button
                  v-for="v in cart.AVAILABLE_VOUCHERS"
                  :key="v.code"
                  type="button"
                  class="voucher-chip-btn"
                  :class="{ active: cart.appliedVoucherCode.value === v.code }"
                  @click="handleApplyVoucher(v.code)"
                >
                  <span class="chip-code">{{ v.code }}</span>
                  <span class="chip-desc">{{ v.title }}</span>
                </button>
              </div>
            </div>
          </div>

          <!-- BẢNG TÍNH TIỀN CHI TIẾT -->
          <div class="cost-breakdown-table">
            <div class="cost-row">
              <span class="cost-label">
                Tạm tính (<strong>{{ cart.selectedItemsCount.value }}</strong>
                món):
              </span>
              <strong class="cost-value"
                >{{ cart.subTotal.value.toLocaleString('vi-VN') }} ₫</strong
              >
            </div>

            <div
              v-if="cart.voucherDiscount.value > 0"
              class="cost-row discount-row"
            >
              <span class="cost-label">
                <i class="bi bi-tag-fill"></i> Giảm giá Voucher:
              </span>
              <strong class="cost-value text-success"
                >-{{
                  cart.voucherDiscount.value.toLocaleString('vi-VN')
                }}
                ₫</strong
              >
            </div>

            <div class="cost-row">
              <span class="cost-label">
                Phí giao hỏa tốc ({{ cart.activeStoresCount.value }} quán):
              </span>
              <strong class="cost-value"
                >{{
                  cart.estimatedShipping.value.toLocaleString('vi-VN')
                }}
                ₫</strong
              >
            </div>

            <div class="cost-divider"></div>

            <div class="cost-row grand-total-row">
              <span class="grand-label">Tổng thanh toán:</span>
              <strong class="grand-value"
                >{{ cart.totalAmount.value.toLocaleString('vi-VN') }} ₫</strong
              >
            </div>
            <div class="vat-note">(Đã bao gồm thuế và các phụ phí nếu có)</div>
          </div>

          <!-- NÚT TIẾN HÀNH THANH TOÁN -->
          <button
            type="button"
            class="btn-proceed-checkout"
            :disabled="cart.selectedItemsCount.value === 0"
            @click="handleProceedToCheckout"
          >
            <span
              >TIẾN HÀNH ĐẶT HÀNG ({{ cart.selectedItemsCount.value }})</span
            >
            <i class="bi bi-arrow-right-short"></i>
          </button>
          <p
            v-if="cart.selectedItemsCount.value === 0"
            class="no-selection-warning"
          >
            ⚠️ Vui lòng tick chọn ít nhất 1 món để chốt đơn hàng!
          </p>

          <!-- CAM KẾT SÀN ZONEMART -->
          <div class="zonemart-trust-card">
            <h5 class="trust-heading">Yên Tâm Mua Sắm Tại ZoneMart</h5>
            <ul class="trust-list">
              <li>
                <i class="bi bi-lightning-charge-fill text-terracotta"></i>
                <span>Giao hàng hỏa tốc trong 10km (15 - 30 phút).</span>
              </li>
              <li>
                <i class="bi bi-shield-fill-check text-green"></i>
                <span
                  >Nông sản kiểm định VietGAP & Vệ sinh an toàn thực phẩm.</span
                >
              </li>
              <li>
                <i class="bi bi-arrow-repeat text-blue"></i>
                <span
                  >Đổi trả 100% trong vòng 24 giờ nếu hàng dập nát, hư
                  hỏng.</span
                >
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>

    <!-- 3. GIAO DIỆN GIỎ HÀNG RỖNG (EMPTY STATE) -->
    <div v-else class="empty-cart-container">
      <div class="empty-card-inner">
        <div class="empty-art-circle">
          <i class="bi bi-basket3-fill"></i>
        </div>
        <h2 class="empty-main-title">Giỏ Hàng Của Bạn Đang Trống!</h2>
        <p class="empty-sub-text">
          Chưa có món hàng nào trong giỏ. Hãy dạo quanh các gian hàng nhà vườn
          và tiểu thương địa phương quanh bạn để chọn những nông sản tươi ngon
          nhất nhé!
        </p>
        <div class="empty-actions">
          <router-link to="/products" class="btn-shop-now">
            <i class="bi bi-grid-fill"></i>
            <span>Khám Phá Nông Sản Quanh Bạn</span>
          </router-link>
          <router-link to="/" class="btn-back-home">
            <span>Về Trang Chủ</span>
          </router-link>
        </div>
      </div>
    </div>

    <!-- TOAST THÔNG BÁO NHANH -->
    <Transition name="fade-toast">
      <div v-if="showToast" class="cart-floating-toast">
        <i class="bi bi-check-circle-fill"></i>
        <span>{{ toastMessage }}</span>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* ============================================================================
   PALETTE: WARM HUMANIST & TERRACOTTA
   Nền: #FAF7F2 | Nâu đậm: #2B1B14 | Cam đất: #D85A2A | Viền: #EBDCD3
   ============================================================================ */

.cart-page-wrapper {
  max-width: 1240px;
  margin: 24px auto 80px auto;
  padding: 0 20px;
  font-family:
    'Plus Jakarta Sans',
    system-ui,
    -apple-system,
    sans-serif;
  color: #2b1b14;
}

/* 1. STEPPER */
.checkout-stepper-container {
  margin-bottom: 28px;
  padding: 16px 24px;
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 16px;
  box-shadow: 0 2px 10px rgba(43, 27, 20, 0.03);
}
.stepper-track {
  display: flex;
  align-items: center;
  justify-content: center;
  max-width: 600px;
  margin: 0 auto;
}
.step-node {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  color: #9c887e;
  font-size: 13px;
  font-weight: 600;
}
.step-node.active {
  color: #d85a2a;
}
.node-circle {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: #faf7f2;
  border: 1.5px solid #ebdcd3;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  color: #9c887e;
  transition: all 0.25s ease;
}
.step-node.active .node-circle {
  background: #d85a2a;
  color: #ffffff;
  border-color: #d85a2a;
  box-shadow: 0 4px 12px rgba(216, 90, 42, 0.3);
}
.step-connector {
  flex: 1;
  height: 2px;
  background: #ebdcd3;
  margin: 0 16px;
  transform: translateY(-10px);
}

/* 2. MAIN GRID */
.cart-main-grid {
  display: grid;
  grid-template-columns: 1fr 390px;
  gap: 28px;
  align-items: start;
}
@media (max-width: 980px) {
  .cart-main-grid {
    grid-template-columns: 1fr;
  }
}

/* 3. FREESHIP SMART BANNER */
.freeship-smart-banner {
  background: linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%);
  border: 1px solid #fed7aa;
  border-radius: 16px;
  padding: 14px 18px;
  margin-bottom: 18px;
  display: flex;
  align-items: center;
  gap: 14px;
}
.banner-icon-wrap {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #d85a2a;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(216, 90, 42, 0.25);
}
.banner-content {
  flex: 1;
}
.banner-title-row {
  font-size: 13.5px;
  color: #431407;
  margin-bottom: 8px;
}
.banner-text strong {
  color: #c2410c;
}
.text-success-bold strong {
  color: #15803d;
}
.progress-bar-bg {
  height: 6px;
  background: rgba(216, 90, 42, 0.15);
  border-radius: 50px;
  overflow: hidden;
}
.progress-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #ea580c 0%, #d85a2a 100%);
  border-radius: 50px;
  transition: width 0.3s ease;
}

/* 4. TOOLBAR */
.cart-toolbar-card {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 14px;
  padding: 12px 18px;
  margin-bottom: 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.toolbar-select-all-text {
  font-size: 14px;
  font-weight: 600;
  color: #2b1b14;
}
.btn-clear-selected {
  background: none;
  border: none;
  color: #94a3b8;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 5px;
  transition: color 0.15s;
}
.btn-clear-selected:hover {
  color: #dc2626;
}

/* CUSTOM CHECKBOX */
.custom-checkbox-wrap {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  user-select: none;
  position: relative;
}
.real-checkbox {
  position: absolute;
  opacity: 0;
  cursor: pointer;
}
.custom-checkmark {
  width: 20px;
  height: 20px;
  border-radius: 6px;
  border: 1.5px solid #cbd5e1;
  background: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s ease;
}
.real-checkbox:checked ~ .custom-checkmark {
  background: #d85a2a;
  border-color: #d85a2a;
}
.real-checkbox:checked ~ .custom-checkmark::after {
  content: '';
  display: block;
  width: 5px;
  height: 10px;
  border: solid #ffffff;
  border-width: 0 2px 2px 0;
  transform: rotate(45deg) translate(-1px, -1px);
}

/* 5. STORE GROUP PANEL */
.store-group-panel {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 18px;
  margin-bottom: 22px;
  overflow: hidden;
  box-shadow: 0 4px 16px rgba(43, 27, 20, 0.03);
}
.store-panel-head {
  background: #faf7f2;
  border-bottom: 1px solid #ebdcd3;
  padding: 14px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}
.store-head-left {
  display: flex;
  align-items: center;
  gap: 12px;
}
.store-info-title {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.store-badge-icon {
  color: #d85a2a;
  font-size: 16px;
}
.store-name-txt {
  font-size: 15px;
  color: #2b1b14;
}
.store-distance-badge {
  font-size: 11.5px;
  font-weight: 600;
  color: #78350f;
  background: #fef3c7;
  padding: 2px 8px;
  border-radius: 50px;
}
.express-eta-tag {
  font-size: 12px;
  font-weight: 700;
  color: #15803d;
  background: #dcfce7;
  border: 1px solid #bbf7d0;
  padding: 4px 10px;
  border-radius: 50px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

/* ITEMS BODY */
.store-items-body {
  padding: 12px 20px;
}
.cart-item-row {
  display: grid;
  grid-template-columns: 24px 76px 1fr 110px 120px 32px;
  align-items: center;
  gap: 16px;
  padding: 16px 0;
  border-bottom: 1px solid #f3e7df;
  transition: opacity 0.2s;
}
.cart-item-row:last-child {
  border-bottom: none;
}
.cart-item-row.item-unselected {
  opacity: 0.6;
}
.item-thumb-box {
  width: 76px;
  height: 76px;
  border-radius: 12px;
  overflow: hidden;
  background: #f8fafc;
  border: 1px solid #ebdcd3;
}
.item-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}
.item-thumb-img:hover {
  transform: scale(1.05);
}
.item-meta-box {
  min-width: 0;
}
.item-name-heading {
  margin: 0 0 4px 0;
  font-size: 14.5px;
  font-weight: 700;
  color: #2b1b14;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.item-unit-tag {
  font-size: 12px;
  color: #71717a;
  margin-bottom: 6px;
}
.item-price-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.current-price {
  font-size: 14.5px;
  font-weight: 700;
  color: #d85a2a;
}
.original-price {
  font-size: 12px;
  color: #a1a1aa;
  text-decoration: line-through;
}

/* STEPPER */
.item-quantity-stepper {
  display: inline-flex;
  align-items: center;
  border: 1.5px solid #ebdcd3;
  border-radius: 8px;
  background: #ffffff;
  overflow: hidden;
}
.stepper-btn {
  width: 30px;
  height: 30px;
  background: #faf7f2;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  color: #4a3830;
  transition: background 0.15s;
}
.stepper-btn:hover {
  background: #f1e4dc;
  color: #d85a2a;
}
.stepper-value {
  width: 36px;
  text-align: center;
  font-size: 13.5px;
  font-weight: 700;
  color: #2b1b14;
}

.item-line-total {
  font-size: 15px;
  font-weight: 800;
  color: #2b1b14;
  text-align: right;
}
.btn-delete-item {
  background: none;
  border: none;
  color: #cbd5e1;
  font-size: 16px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition:
    color 0.15s,
    transform 0.15s;
}
.btn-delete-item:hover {
  color: #dc2626;
  transform: scale(1.1);
}

/* STORE NOTE ROW */
.store-note-row {
  background: #fbf9f6;
  border-top: 1px dashed #ebdcd3;
  padding: 10px 20px;
}
.store-note-wrap {
  display: flex;
  align-items: center;
  gap: 10px;
}
.store-note-icon {
  color: #d85a2a;
  font-size: 14px;
}
.store-note-input {
  flex: 1;
  border: none;
  background: transparent;
  outline: none;
  font-size: 12.5px;
  color: #4a3830;
  font-style: italic;
}
.store-note-input::placeholder {
  color: #a89f91;
  font-style: italic;
}

/* RESPONSIVE ITEM ROW */
@media (max-width: 700px) {
  .cart-item-row {
    grid-template-columns: 24px 60px 1fr 32px;
    gap: 10px;
    align-items: start;
  }
  .item-thumb-box {
    width: 60px;
    height: 60px;
  }
  .item-quantity-stepper {
    grid-column: 3 / 4;
    width: fit-content;
    margin-top: 6px;
  }
  .item-line-total {
    grid-column: 3 / 4;
    text-align: left;
    margin-top: 4px;
  }
  .btn-delete-item {
    grid-column: 4 / 5;
    grid-row: 1 / 2;
  }
}

/* 6. SUGGESTIONS SECTION */
.cart-suggestions-section {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 18px;
  padding: 22px;
  margin-top: 28px;
}
.suggestions-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
}
.suggestions-badge {
  font-size: 11px;
  font-weight: 800;
  color: #d85a2a;
  letter-spacing: 0.5px;
}
.suggestions-heading {
  margin: 2px 0 0 0;
  font-size: 18px;
  color: #2b1b14;
}
.see-more-link {
  font-size: 13px;
  font-weight: 700;
  color: #d85a2a;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.see-more-link:hover {
  text-decoration: underline;
}
.suggestions-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 14px;
}
@media (max-width: 860px) {
  .suggestions-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
@media (max-width: 480px) {
  .suggestions-grid {
    grid-template-columns: 1fr;
  }
}
.suggested-card {
  border: 1px solid #ebdcd3;
  border-radius: 14px;
  overflow: hidden;
  background: #faf7f2;
  display: flex;
  flex-direction: column;
  transition:
    transform 0.2s,
    box-shadow 0.2s;
}
.suggested-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 6px 18px rgba(43, 27, 20, 0.08);
}
.sug-img-wrap {
  width: 100%;
  height: 120px;
  overflow: hidden;
  background: #ffffff;
}
.sug-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}
.suggested-card:hover .sug-img {
  transform: scale(1.06);
}
.sug-info {
  padding: 12px;
  display: flex;
  flex-direction: column;
  flex: 1;
}
.sug-store-name {
  font-size: 11px;
  color: #78350f;
  margin-bottom: 2px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.sug-prod-name {
  margin: 0 0 6px 0;
  font-size: 13px;
  font-weight: 700;
  color: #2b1b14;
  line-height: 1.3;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  height: 34px;
}
.sug-price-row {
  display: flex;
  align-items: baseline;
  gap: 6px;
  margin-bottom: 10px;
}
.sug-price {
  font-size: 14px;
  font-weight: 800;
  color: #d85a2a;
}
.sug-original {
  font-size: 11px;
  color: #9ca3af;
  text-decoration: line-through;
}
.btn-quick-add {
  margin-top: auto;
  width: 100%;
  background: #ffffff;
  border: 1.5px solid #d85a2a;
  color: #d85a2a;
  border-radius: 8px;
  padding: 6px 10px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  transition: all 0.15s;
}
.btn-quick-add:hover {
  background: #d85a2a;
  color: #ffffff;
}

/* 7. ORDER SUMMARY COLUMN (STICKY) */
.order-summary-card {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 20px;
  padding: 22px;
  position: sticky;
  top: 86px;
  box-shadow: 0 6px 24px rgba(43, 27, 20, 0.05);
}
.summary-card-title {
  margin: 0 0 16px 0;
  font-size: 18px;
  font-weight: 800;
  color: #2b1b14;
  padding-bottom: 12px;
  border-bottom: 1px solid #f3e7df;
}

/* VOUCHER SECTION */
.voucher-box-section {
  background: #faf7f2;
  border: 1px solid #ebdcd3;
  border-radius: 14px;
  padding: 14px;
  margin-bottom: 20px;
}
.voucher-box-label {
  font-size: 12.5px;
  font-weight: 700;
  color: #d85a2a;
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
}
.voucher-input-row {
  display: flex;
  gap: 6px;
  margin-bottom: 8px;
}
.voucher-input-field {
  flex: 1;
  border: 1px solid #cbd5e1;
  background: #ffffff;
  border-radius: 8px;
  padding: 8px 10px;
  font-size: 12.5px;
  font-weight: 600;
  text-transform: uppercase;
  outline: none;
}
.voucher-input-field:focus {
  border-color: #d85a2a;
}
.btn-apply-voucher {
  background: #d85a2a;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 8px 14px;
  font-size: 12.5px;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s;
}
.btn-apply-voucher:hover {
  background: #bf4a1f;
}

.voucher-alert-msg {
  font-size: 12px;
  padding: 6px 8px;
  border-radius: 6px;
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  gap: 6px;
}
.voucher-alert-msg.success {
  background: #ecfdf5;
  color: #065f46;
  border: 1px solid #a7f3d0;
}
.voucher-alert-msg.error {
  background: #fef2f2;
  color: #991b1b;
  border: 1px solid #fecaca;
}

.active-voucher-pill {
  background: #fff7ed;
  border: 1px dashed #ea580c;
  border-radius: 8px;
  padding: 6px 10px;
  font-size: 12px;
  color: #9a3412;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}
.btn-remove-voucher {
  background: none;
  border: none;
  color: #ea580c;
  font-size: 16px;
  cursor: pointer;
  line-height: 1;
}

.voucher-quick-list .quick-title {
  font-size: 11px;
  font-weight: 700;
  color: #71717a;
  display: block;
  margin-bottom: 6px;
}
.quick-badges-wrap {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.voucher-chip-btn {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 6px;
  padding: 4px 8px;
  cursor: pointer;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 11px;
  transition: all 0.15s;
  text-align: left;
}
.voucher-chip-btn .chip-code {
  font-weight: 800;
  color: #d85a2a;
}
.voucher-chip-btn .chip-desc {
  color: #64748b;
}
.voucher-chip-btn:hover {
  border-color: #d85a2a;
  background: #fdf5f0;
}
.voucher-chip-btn.active {
  border-color: #d85a2a;
  background: #fff7ed;
}

/* BREAKDOWN TABLE */
.cost-breakdown-table {
  margin-bottom: 20px;
}
.cost-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13.5px;
  color: #4a3830;
  margin-bottom: 10px;
}
.cost-row.discount-row {
  color: #16a34a;
}
.cost-row.discount-row .cost-value {
  color: #16a34a;
}
.cost-divider {
  height: 1px;
  background: #f3e7df;
  margin: 14px 0;
}
.grand-total-row {
  font-size: 16px;
  margin-bottom: 2px;
}
.grand-label {
  font-weight: 800;
  color: #2b1b14;
}
.grand-value {
  font-size: 24px;
  font-weight: 900;
  color: #d85a2a;
}
.vat-note {
  font-size: 11.5px;
  color: #9ca3af;
  text-align: right;
  margin-bottom: 16px;
}

/* CHECKOUT BUTTON */
.btn-proceed-checkout {
  width: 100%;
  background: #d85a2a;
  color: #ffffff;
  border: none;
  border-radius: 12px;
  padding: 14px 18px;
  font-size: 14px;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  box-shadow: 0 6px 18px rgba(216, 90, 42, 0.28);
  transition: all 0.2s ease;
}
.btn-proceed-checkout:hover:not(:disabled) {
  background: #bf4a1f;
  transform: translateY(-2px);
  box-shadow: 0 8px 22px rgba(216, 90, 42, 0.38);
}
.btn-proceed-checkout:disabled {
  background: #cbd5e1;
  color: #64748b;
  cursor: not-allowed;
  box-shadow: none;
}
.no-selection-warning {
  font-size: 12px;
  color: #dc2626;
  text-align: center;
  margin: 8px 0 0 0;
  font-weight: 600;
}

/* TRUST CARD */
.zonemart-trust-card {
  margin-top: 24px;
  padding-top: 18px;
  border-top: 1px solid #f3e7df;
}
.trust-heading {
  margin: 0 0 10px 0;
  font-size: 12.5px;
  font-weight: 700;
  color: #71717a;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.trust-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.trust-list li {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  font-size: 12px;
  color: #52525b;
  line-height: 1.4;
}
.text-terracotta {
  color: #d85a2a;
}
.text-green {
  color: #16a34a;
}
.text-blue {
  color: #2563eb;
}

/* 8. EMPTY STATE */
.empty-cart-container {
  padding: 60px 20px;
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 24px;
  text-align: center;
  box-shadow: 0 6px 20px rgba(43, 27, 20, 0.03);
}
.empty-card-inner {
  max-width: 480px;
  margin: 0 auto;
}
.empty-art-circle {
  width: 96px;
  height: 96px;
  border-radius: 50%;
  background: #faf7f2;
  border: 2px dashed #ebdcd3;
  color: #d85a2a;
  font-size: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 20px auto;
}
.empty-main-title {
  font-size: 22px;
  font-weight: 800;
  color: #2b1b14;
  margin: 0 0 10px 0;
}
.empty-sub-text {
  font-size: 14px;
  color: #64748b;
  line-height: 1.6;
  margin: 0 0 28px 0;
}
.empty-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
  flex-wrap: wrap;
}
.btn-shop-now {
  background: #d85a2a;
  color: #ffffff;
  text-decoration: none;
  padding: 12px 24px;
  border-radius: 50px;
  font-size: 14px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 6px 18px rgba(216, 90, 42, 0.25);
  transition: all 0.2s;
}
.btn-shop-now:hover {
  background: #bf4a1f;
  transform: translateY(-2px);
}
.btn-back-home {
  background: #faf7f2;
  color: #4a3830;
  text-decoration: none;
  padding: 12px 22px;
  border-radius: 50px;
  font-size: 14px;
  font-weight: 600;
  border: 1px solid #ebdcd3;
  transition: all 0.2s;
}
.btn-back-home:hover {
  background: #f1e4dc;
}

/* 9. FLOATING TOAST */
.cart-floating-toast {
  position: fixed;
  bottom: 30px;
  right: 30px;
  background: #2b1b14;
  color: #ffffff;
  padding: 12px 20px;
  border-radius: 12px;
  font-size: 13.5px;
  font-weight: 600;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  display: flex;
  align-items: center;
  gap: 10px;
  z-index: 9999;
}
.cart-floating-toast i {
  color: #4ade80;
  font-size: 16px;
}
.fade-toast-enter-active,
.fade-toast-leave-active {
  transition: all 0.3s ease;
}
.fade-toast-enter-from,
.fade-toast-leave-to {
  opacity: 0;
  transform: translateY(15px);
}
</style>
