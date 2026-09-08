<script setup lang="ts">
/**
 * ================================================================
 * THANH TOÁN (CHECKOUT) - Phụ trách: Thắng
 * ================================================================
 */
import { ref, computed } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const shippingAddress = ref("Ngõ 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội");
const buyerPhone = ref("0912 345 678");
const buyerName = ref("Hoàng Huy");

const distanceKm = ref(2.5); // Khoảng cách <= 3km
const shopCount = ref(2);

const isExpressEligible = computed(() => distanceKm.value <= 3.0);
const isExpressSelected = ref(true);
const paymentMethod = ref<"COD" | "ONLINE_QR">("ONLINE_QR");

const shippingFee = computed(() => {
  if (isExpressEligible.value && isExpressSelected.value) {
    return Math.round((15000 * shopCount.value) * 1.5);
  } else {
    return 15000 * shopCount.value;
  }
});

const itemsTotal = ref(560000);
const finalTotal = computed(() => itemsTotal.value + shippingFee.value);

const handleSelectExpress = (val: boolean) => {
  isExpressSelected.value = val;
  if (val) paymentMethod.value = "ONLINE_QR";
};

const handlePlaceOrder = () => {
  alert("🎉 Đặt hàng thành công! Đơn hàng của bạn đang được chuyển tới Cửa Hàng xác nhận.");
  router.push("/buyer-orders");
};
</script>

<template>
  <div class="checkout-container">
    <div class="checkout-header">
      <h2>💳 Thanh Toán & Đặt Hàng</h2>
      <p>Kiểm tra địa chỉ, chọn phương thức giao hàng và tiến hành chốt đơn.</p>
    </div>

    <div class="checkout-grid">
      <!-- Cột trái: Thông tin nhận hàng -->
      <div class="left-col">
        <div class="card-box">
          <h3>📍 1. Địa Chỉ Nhận Hàng</h3>
          <h3><i class="bi bi-geo-alt-fill text-danger me-2" aria-hidden="true"></i>1. Địa Chỉ Nhận Hàng</h3>
          <div class="addr-content">
            <div class="user-row">
              <strong>{{ buyerName }}</strong> ({{ buyerPhone }})
            </div>
            <p class="addr-text">{{ shippingAddress }}</p>
            <span class="dist-badge">📏 Khoảng cách ước tính: {{ distanceKm }} km</span>
            <span class="dist-badge"><i class="bi bi-compass me-1" aria-hidden="true"></i> Khoảng cách ước tính: {{ distanceKm }} km</span>
          </div>
        </div>

        <div class="card-box">
          <h3>🚚 2. Phương Thức Vận Chuyển</h3>
          <h3><i class="bi bi-truck text-primary me-2" aria-hidden="true"></i>2. Phương Thức Vận Chuyển</h3>
          <div class="shipping-options">
            <div 
              v-if="isExpressEligible"
              class="opt-item"
              :class="{ selected: isExpressSelected }"
              @click="handleSelectExpress(true)"
            >
              <div class="opt-head">
                <strong>⚡ Giao Hỏa Tốc Siêu Tốc (&le; 3km)</strong>
                <strong><i class="bi bi-lightning-charge-fill text-warning me-1" aria-hidden="true"></i> Giao Hỏa Tốc Siêu Tốc (&le; 3km)</strong>
                <span class="price">{{ (Math.round((15000 * shopCount) * 1.5)).toLocaleString('vi-VN') }} ₫</span>
              </div>
              <p class="sub">Nhận hàng trong 15-25 phút. <em>(Chỉ áp dụng thanh toán qua QR Code)</em></p>
            </div>

            <div 
              class="opt-item"
              :class="{ selected: !isExpressSelected }"
              @click="handleSelectExpress(false)"
            >
              <div class="opt-head">
                <strong>🛵 Giao Hàng Tiêu Chuẩn</strong>
                <strong><i class="bi bi-bicycle text-success me-1" aria-hidden="true"></i> Giao Hàng Tiêu Chuẩn</strong>
                <span class="price">{{ (15000 * shopCount).toLocaleString('vi-VN') }} ₫</span>
              </div>
              <p class="sub">Nhận hàng trong 30-45 phút. Hỗ trợ Tiền mặt COD và QR Code.</p>
            </div>
          </div>
        </div>

        <div class="card-box">
          <h3>💰 3. Phương Thức Thanh Toán</h3>
          <h3><i class="bi bi-credit-card-2-front text-warning me-2" aria-hidden="true"></i>3. Phương Thức Thanh Toán</h3>
          <div class="pay-options">
            <label class="pay-item" :class="{ active: paymentMethod === 'ONLINE_QR' }">
              <input type="radio" value="ONLINE_QR" v-model="paymentMethod" />
              <div>
                <strong>Chuyển Khoản QR Code (Ưu Tiên)</strong>
                <p>Khuyên dùng - Áp dụng cho mọi đơn hàng bao gồm Hỏa Tốc</p>
              </div>
            </label>

            <label class="pay-item" :class="{ active: paymentMethod === 'COD', disabled: isExpressSelected }">
              <input type="radio" value="COD" v-model="paymentMethod" :disabled="isExpressSelected" />
              <div>
                <strong>Tiền Mặt Khi Nhận Hàng (COD)</strong>
                <p v-if="isExpressSelected">⚠️ Không hỗ trợ COD khi chọn Giao Hỏa Tốc</p>
                <p v-if="isExpressSelected"><i class="bi bi-exclamation-triangle-fill text-warning me-1" aria-hidden="true"></i> Không hỗ trợ COD khi chọn Giao Hỏa Tốc</p>
                <p v-else>Thanh toán trực tiếp cho Shipper khi nhận món</p>
              </div>
            </label>
          </div>
        </div>
      </div>

      <!-- Cột phải: Chi tiết thanh toán -->
      <div class="right-col">
        <div class="card-box sticky-box">
          <h3>Chi Tiết Thanh Toán</h3>

          <div class="fee-row">
            <span>Tiền hàng ({{ shopCount }} quán):</span>
            <strong>{{ itemsTotal.toLocaleString("vi-VN") }} ₫</strong>
          </div>
          <div class="fee-row">
            <span>Phí giao hàng:</span>
            <strong>{{ shippingFee.toLocaleString("vi-VN") }} ₫</strong>
          </div>

          <div class="divider"></div>

          <div class="fee-row total-row">
            <span>Tổng cộng:</span>
            <strong class="total-price">{{ finalTotal.toLocaleString("vi-VN") }} ₫</strong>
          </div>

          <div v-if="paymentMethod === 'ONLINE_QR'" class="qr-box">
            <p class="qr-text">Mã QR thanh toán nhanh:</p>
            <img src="https://api.qrserver.com/v1/create-qr-code/?size=140x140&data=ZONEMART_PAYMENT" alt="QR" />
            <span class="qr-sub">Ngân hàng MB Bank • ZoneMart</span>
          </div>

          <button class="btn btn-primary btn-block" @click="handlePlaceOrder">
            🚀 XÁC NHẬN ĐẶT HÀNG
            <i class="bi bi-bag-check-fill me-2" aria-hidden="true"></i> XÁC NHẬN ĐẶT HÀNG
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.checkout-container { max-width: 1150px; margin: 30px auto 60px auto; padding: 0 20px; }
.checkout-header h2 { margin: 0 0 6px 0; color: #0f172a; font-size: 24px; }
.checkout-header p { margin: 0 0 26px 0; color: #64748b; font-size: 14px; }

.checkout-grid { display: grid; grid-template-columns: 1.8fr 1fr; gap: 24px; }
@media (max-width: 850px) { .checkout-grid { grid-template-columns: 1fr; } }

.card-box { background: #fff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 22px; margin-bottom: 20px; }
.card-box h3 { margin: 0 0 16px 0; font-size: 16px; color: #0f172a; border-bottom: 1px solid #f1f5f9; padding-bottom: 10px; }

.addr-content .user-row { font-size: 15px; color: #0f172a; margin-bottom: 4px; }
.addr-text { font-size: 14px; color: #475569; margin: 0 0 10px 0; }
.dist-badge { font-size: 12px; background: #f0fdf4; color: #166534; padding: 4px 10px; border-radius: 6px; font-weight: 600; }

.shipping-options, .pay-options { display: flex; flex-direction: column; gap: 10px; }
.opt-item, .pay-item {
  border: 1px solid #e2e8f0; border-radius: 10px; padding: 14px; cursor: pointer; background: #f8fafc; transition: all 0.2s;
}
.opt-item.selected, .pay-item.active { border-color: #2563eb; background: #eff6ff; }
.pay-item.disabled { opacity: 0.5; cursor: not-allowed; }
.opt-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px; }
.opt-head .price { font-weight: 700; color: #0f172a; }
.sub, .pay-item p { margin: 0; font-size: 12px; color: #64748b; }
.pay-item { display: flex; align-items: center; gap: 12px; }

.sticky-box { position: sticky; top: 90px; }
.fee-row { display: flex; justify-content: space-between; font-size: 14px; margin-bottom: 12px; color: #475569; }
.divider { height: 1px; background: #e2e8f0; margin: 16px 0; }
.total-row { font-size: 16px; color: #0f172a; }
.total-price { font-size: 24px; color: #dc2626; }

.qr-box { text-align: center; background: #f8fafc; padding: 14px; border-radius: 10px; border: 1px dashed #cbd5e1; margin: 16px 0; }
.qr-text { margin: 0 0 8px 0; font-size: 12px; color: #475569; font-weight: 600; }
.qr-sub { display: block; font-size: 11px; color: #64748b; margin-top: 6px; }

.btn { border: none; cursor: pointer; padding: 14px; border-radius: 10px; font-weight: 700; font-size: 15px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-block { width: 100%; }
</style>
