<script setup lang="ts">
/**
 * ================================================================
 * QUẢN LÝ ĐƠN MUA (BUYER ORDERS) - Phụ trách: Thắng
 * ================================================================
 */
import { ref } from "vue";

const orders = ref([
  {
    orderId: "ORD_98213",
    date: "07/09/2026 18:30",
    total: 395000,
    paymentMethod: "ONLINE_QR",
    deliveryType: "express",
    subOrders: [
      {
        subId: "SUB_01",
        storeName: "ZoneMart Cầu Giấy",
        items: "Thịt Bò Mỹ Nhập Khẩu (x2), Gạo ST25 (x1)",
        status: "delivering",
        statusText: "🛵 Shipper đang giao hàng tới bạn",
        statusText: "Shipper đang giao hàng tới bạn",
        shipperInfo: "Nguyễn Văn Nam (29M1-8888)",
        shipperPhone: "0987 654 321"
      }
    ]
  },
  {
    orderId: "ORD_97842",
    date: "05/09/2026 12:15",
    total: 125000,
    paymentMethod: "COD",
    deliveryType: "standard",
    subOrders: [
      {
        subId: "SUB_02",
        storeName: "Siêu Thị Trái Cây Xanh",
        items: "Dâu Tây Đà Lạt (x1)",
        status: "completed",
        statusText: "✅ Giao hàng thành công",
        statusText: "Giao hàng thành công",
        shipperInfo: "Trần Đình Trọng (29H2-4321)",
        shipperPhone: "0912 333 444"
      }
    ]
  }
]);

const handleConfirmReceived = (subId: string) => {
  alert(`Cảm ơn bạn đã xác nhận nhận hàng cho đơn #${subId}! Đơn hàng đã hoàn tất.`);
};

const handleReportIssue = (subId: string) => {
  alert(`Đã gửi yêu cầu khiếu nại cho đơn #${subId}. CSKH ZoneMart sẽ liên hệ hỗ trợ bạn!`);
};
</script>

<template>
  <div class="buyer-orders-container">
    <div class="header">
      <h2>📦 Đơn Mua Của Bạn</h2>
      <h2><i class="bi bi-box-seam me-2" aria-hidden="true"></i>Đơn Mua Của Bạn</h2>
      <p>Theo dõi trực tiếp trạng thái các đơn hàng đang giao và lịch sử mua sắm.</p>
    </div>

    <div class="orders-list">
      <div v-for="order in orders" :key="order.orderId" class="order-card">
        <div class="order-top">
          <div>
            <strong>Đơn hàng: #{{ order.orderId }}</strong>
            <span class="date">{{ order.date }}</span>
            <span class="order-date">{{ order.date }}</span>
          </div>
          <div class="badges">
            <span class="badge express" v-if="order.deliveryType === 'express'">⚡ Hỏa Tốc</span>
            <span class="badge pay">{{ order.paymentMethod === 'ONLINE_QR' ? 'Đã thanh toán QR' : 'COD (Tiền mặt)' }}</span>
          </div>
          <span class="delivery-badge" :class="order.deliveryType">
            {{ order.deliveryType === 'express' ? 'Hỏa tốc 10km' : 'Giao thường' }}
          </span>
        </div>

        <div class="sub-orders">
          <div v-for="sub in order.subOrders" :key="sub.subId" class="sub-card">
            <div class="sub-head">
              <span class="store-name">🏪 {{ sub.storeName }}</span>
              <span class="status-badge" :class="sub.status">{{ sub.statusText }}</span>
              <span class="store-name"><i class="bi bi-shop"></i> {{ sub.storeName }}</span>
              <span class="status-badge" :class="sub.status">
                <i v-if="sub.status === 'delivering'" class="bi bi-bicycle me-1" aria-hidden="true"></i>
                <i v-else-if="sub.status === 'completed'" class="bi bi-check-circle-fill me-1" aria-hidden="true"></i>
                {{ sub.statusText }}
              </span>
            </div>
            <p class="items-text">🛒 {{ sub.items }}</p>
            <p class="items-text"><i class="bi bi-bag-check"></i> {{ sub.items }}</p>

            <div v-if="sub.status === 'delivering'" class="shipper-bar">
              <div class="s-info">
                <span>🛵 Tài xế: <strong>{{ sub.shipperInfo }}</strong></span>
                <span>☎️ <strong>{{ sub.shipperPhone }}</strong></span>
                <span><i class="bi bi-bicycle"></i> Tài xế: <strong>{{ sub.shipperInfo }}</strong></span>
                <span><i class="bi bi-telephone-fill"></i> <strong>{{ sub.shipperPhone }}</strong></span>
              </div>
              <div class="actions">
                <button class="btn btn-sm btn-success" @click="handleConfirmReceived(sub.subId)">
                  ✅ Đã nhận được hàng
                  <i class="bi bi-check-circle-fill"></i> Đã nhận được hàng
                </button>
                <button class="btn btn-sm btn-danger" @click="handleReportIssue(sub.subId)">
                  ⚠️ Báo cáo sự cố
                  <i class="bi bi-exclamation-triangle-fill"></i> Báo cáo sự cố
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="order-bottom">
          <span>Tổng tiền:</span>
          <strong class="total-val">{{ order.total.toLocaleString("vi-VN") }} ₫</strong>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.buyer-orders-container { max-width: 1050px; margin: 30px auto 60px auto; padding: 0 20px; }
.header h2 { margin: 0 0 6px 0; color: #0f172a; font-size: 24px; }
.header p { margin: 0 0 24px 0; color: #64748b; font-size: 14px; }

.orders-list { display: flex; flex-direction: column; gap: 20px; }
.order-card { background: #fff; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; }

.order-top { background: #f8fafc; padding: 14px 20px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; }
.date { color: #94a3b8; font-size: 13px; margin-left: 10px; }
.badges { display: flex; gap: 8px; }
.badge { font-size: 12px; padding: 3px 8px; border-radius: 6px; font-weight: 600; }
.badge.express { background: #fee2e2; color: #dc2626; }
.badge.pay { background: #dbeafe; color: #1e40af; }

.sub-orders { padding: 16px 20px; display: flex; flex-direction: column; gap: 12px; }
.sub-card { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 14px; }
.sub-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.store-name { font-weight: 700; color: #1e293b; font-size: 14px; }
.status-badge { font-size: 13px; font-weight: 600; }
.status-badge.delivering { color: #2563eb; }
.status-badge.completed { color: #16a34a; }
.items-text { margin: 0 0 10px 0; font-size: 13px; color: #475569; }

.shipper-bar { background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 8px; padding: 10px 14px; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; }
.s-info { display: flex; gap: 14px; font-size: 13px; color: #1e40af; }
.actions { display: flex; gap: 8px; }

.order-bottom { padding: 14px 20px; border-top: 1px solid #f1f5f9; display: flex; justify-content: flex-end; align-items: center; gap: 10px; }
.total-val { font-size: 18px; color: #dc2626; }

.btn { border: none; cursor: pointer; padding: 6px 12px; border-radius: 6px; font-weight: 600; font-size: 12px; }
.btn-success { background: #16a34a; color: #fff; }
.btn-danger { background: transparent; border: 1px solid #fca5a5; color: #dc2626; }
</style>
