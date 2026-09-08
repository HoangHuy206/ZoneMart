<script setup lang="ts">
/**
 * ================================================================
 * CỔNG TÀI XẾ (SHIPPER DASHBOARD) - Phụ trách: Bình
 * ================================================================
 */
import { ref } from "vue";

const isOnline = ref(true);
const currentStep = ref<"idle" | "accepted" | "picked" | "delivered">("accepted");

const activeOrder = ref({
  subOrderId: "SUB_7749",
  deliveryType: "express",
  store: {
    name: "ZoneMart Cầu Giấy",
    address: "245 Cầu Giấy, Hà Nội",
    phone: "024 1234 5678"
  },
  customer: {
    name: "Hoàng Huy",
    address: "165 Cầu Giấy, Dịch Vọng, Hà Nội",
    phone: "0912 345 678"
  },
  shippingFee: 33750,
  distanceKm: 2.2,
  items: "Thịt Bò Mỹ (x2), Gạo ST25 (x1)"
});

const handleConfirmPicked = () => {
  currentStep.value = "picked";
  alert("✅ Đã lấy hàng thành công! Hãy di chuyển tới địa chỉ khách hàng.");
};

const handleConfirmDelivered = () => {
  currentStep.value = "delivered";
  alert("🎉 Đã giao hàng tới khách! Đang chờ khách xác nhận đơn.");
};

const handleReportBoom = () => {
  const reason = prompt("Lý do khách không nhận hàng:");
  if (reason) {
    alert(`Đã báo cáo Boom hàng: "${reason}". Seller sẽ duyệt nhận lại hàng.`);
    currentStep.value = "idle";
  }
};
</script>

<template>
  <div class="shipper-container">
    <div class="header-box">
      <div class="s-profile">
        <div class="avatar">🛵</div>
        <div>
          <h2>Tài Xế ZoneMart Express</h2>
          <p>Biển số: <strong>29M1-9999</strong> • Honda Airblade</p>
        </div>
      </div>

      <div class="toggle-online">
        <label class="switch">
          <input type="checkbox" v-model="isOnline" />
          <span class="slider round"></span>
        </label>
        <span class="status-text" :class="{ on: isOnline }">
          {{ isOnline ? "🟢 Đang BẬT nhận đơn" : "🔴 Đang TẮT nhận đơn" }}
        </span>
      </div>
    </div>

    <div v-if="isOnline" class="main-card">
      <div v-if="currentStep !== 'idle'" class="order-flow">
        <div class="badge-row">
          <span class="b-exp" v-if="activeOrder.deliveryType === 'express'">⚡ Hỏa Tốc (Thu nhập: {{ activeOrder.shippingFee.toLocaleString('vi-VN') }} ₫)</span>
          <span class="b-dist">Cự ly: {{ activeOrder.distanceKm }} km</span>
        </div>

        <div class="steps-nav">
          <span :class="{ active: currentStep === 'accepted' || currentStep === 'picked' || currentStep === 'delivered' }">1. Lấy hàng tại Shop</span>
          <span>➜</span>
          <span :class="{ active: currentStep === 'picked' || currentStep === 'delivered' }">2. Giao cho Khách</span>
          <span>➜</span>
          <span :class="{ active: currentStep === 'delivered' }">3. Hoàn tất</span>
        </div>

        <div v-if="currentStep === 'accepted'" class="point-box">
          <h4>Chặng 1: Dẫn đường tới Cửa Hàng</h4>
          <p class="target">🏪 {{ activeOrder.store.name }}</p>
          <p class="addr">📍 {{ activeOrder.store.address }} • ☎️ {{ activeOrder.store.phone }}</p>
          <p class="items">Món cần lấy: {{ activeOrder.items }}</p>
          <button class="btn btn-primary" @click="handleConfirmPicked">📦 ĐÃ LẤY HÀNG TẠI SHOP ➜</button>
        </div>

        <div v-else-if="currentStep === 'picked'" class="point-box destination">
          <h4>Chặng 2: Dẫn đường tới Khách Hàng</h4>
          <p class="target">👤 {{ activeOrder.customer.name }}</p>
          <p class="addr">📍 {{ activeOrder.customer.address }} • ☎️ {{ activeOrder.customer.phone }}</p>
          <div class="action-row">
            <button class="btn btn-success" @click="handleConfirmDelivered">🎉 ĐÃ GIAO CHO KHÁCH</button>
            <button class="btn btn-danger" @click="handleReportBoom">⚠️ Báo Cáo Boom Hàng</button>
          </div>
        </div>

        <div v-else class="done-box">
          <h3>✅ Đã giao xong đơn hàng!</h3>
          <p>Thù lao <strong>{{ activeOrder.shippingFee.toLocaleString('vi-VN') }} ₫</strong> sẽ được cộng vào ví của bạn.</p>
          <button class="btn btn-primary" @click="currentStep = 'idle'">Nhận chuyến tiếp theo</button>
        </div>
      </div>

      <div v-else class="waiting-box">
        <h3>Đang quét đơn hàng xung quanh bạn (10km)...</h3>
        <p>Hệ thống sẽ tự động thông báo khi có khách đặt hàng gần bạn.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.shipper-container { max-width: 900px; margin: 30px auto 60px auto; padding: 0 20px; }
.header-box { background: #fff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 20px 24px; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 16px; margin-bottom: 24px; }
.s-profile { display: flex; align-items: center; gap: 14px; }
.avatar { font-size: 36px; background: #f3e8ff; padding: 10px; border-radius: 50%; }
.s-profile h2 { margin: 0 0 4px 0; font-size: 18px; color: #0f172a; }
.s-profile p { margin: 0; font-size: 13px; color: #64748b; }

.toggle-online { display: flex; align-items: center; gap: 12px; }
.status-text { font-size: 14px; font-weight: 700; color: #dc2626; }
.status-text.on { color: #16a34a; }

.switch { position: relative; display: inline-block; width: 48px; height: 26px; }
.switch input { opacity: 0; width: 0; height: 0; }
.slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: #cbd5e1; transition: .4s; border-radius: 34px; }
.slider:before { position: absolute; content: ""; height: 18px; width: 18px; left: 4px; bottom: 4px; background-color: white; transition: .4s; border-radius: 50%; }
input:checked + .slider { background-color: #16a34a; }
input:checked + .slider:before { transform: translateX(22px); }

.main-card { background: #fff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 24px; }
.badge-row { display: flex; justify-content: space-between; margin-bottom: 20px; }
.b-exp { background: #fee2e2; color: #dc2626; font-size: 13px; font-weight: 700; padding: 6px 12px; border-radius: 6px; }
.b-dist { background: #eff6ff; color: #1e40af; font-size: 13px; font-weight: 700; padding: 6px 12px; border-radius: 6px; }

.steps-nav { display: flex; justify-content: space-around; background: #f8fafc; padding: 12px; border-radius: 10px; margin-bottom: 20px; font-size: 13px; color: #94a3b8; font-weight: 600; }
.steps-nav .active { color: #2563eb; font-weight: 800; }

.point-box { background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 12px; padding: 20px; }
.point-box.destination { background: #eff6ff; border-color: #bfdbfe; }
.point-box h4 { margin: 0 0 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; }
.target { font-size: 18px; font-weight: 800; color: #0f172a; margin: 0 0 6px 0; }
.addr { font-size: 14px; color: #334155; margin: 0 0 8px 0; }
.items { font-size: 13px; color: #64748b; margin: 0 0 16px 0; }
.action-row { display: flex; gap: 10px; }

.done-box, .waiting-box { text-align: center; padding: 40px; }
.btn { border: none; cursor: pointer; padding: 12px 20px; border-radius: 8px; font-weight: 700; font-size: 14px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-success { background: #16a34a; color: #fff; }
.btn-danger { background: #dc2626; color: #fff; }
</style>
