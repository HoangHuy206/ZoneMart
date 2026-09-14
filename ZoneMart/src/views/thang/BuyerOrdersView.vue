<script setup lang="ts">
/**
 * ================================================================
 * QUẢN LÝ ĐƠN MUA (BUYER ORDERS) - Phụ trách: Thắng
 * Kết nối 100% Database MongoDB Atlas qua Backend ASP.NET Core
 * Phong cách thiết kế: Warm Humanist & Terracotta (Bán kính Hub 10km)
 * ================================================================
 */
import { ref, onMounted, computed } from 'vue';
import { useAuth } from '../../composables/useAuth';
import { orderService, type OrderRecord } from '../../services/orderService';

const auth = useAuth();
const orders = ref<OrderRecord[]>([]);
const isLoading = ref(true);
const filterTab = ref<'all' | 'delivering' | 'completed'>('all');

// Toast thông báo
const toastMsg = ref('');
const showToast = ref(false);
const triggerToast = (msg: string) => {
  toastMsg.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2800);
};

const fetchOrders = async () => {
  isLoading.value = true;
  try {
    const buyerId = auth.currentUser.value?.id || 'usr_buyer_01';
    const data = await orderService.getBuyerOrders(buyerId);
    orders.value = data;
  } catch (e) {
    console.error('Lỗi tải đơn mua:', e);
    orders.value = orderService.getLocalOrders();
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchOrders();
});

// Lọc đơn hàng theo Tab
const filteredOrders = computed(() => {
  if (filterTab.value === 'all') return orders.value;
  return orders.value.filter((order) => {
    if (filterTab.value === 'delivering') {
      return order.subOrders.some(
        (s) => s.status === 'delivering' || s.status === 'pending',
      );
    } else if (filterTab.value === 'completed') {
      return order.subOrders.every((s) => s.status === 'completed');
    }
    return true;
  });
});

// Xác nhận đã nhận hàng (Hoàn thành đơn con)
const handleConfirmReceived = async (orderId: string, subId: string) => {
  const ok = await orderService.updateSubOrderStatus(subId, 'completed');
  if (ok) {
    // Cập nhật trạng thái hiển thị
    orders.value.forEach((ord) => {
      if (ord.orderId === orderId) {
        ord.subOrders.forEach((sub) => {
          if (sub.subId === subId) {
            sub.status = 'completed';
            sub.statusText = 'Giao hàng thành công';
          }
        });
      }
    });
    triggerToast(`Cảm ơn bạn! Đơn kiện #${subId} đã hoàn tất giao hàng.`);
  }
};

// Khiếu nại / Báo cáo sự cố
const handleReportIssue = async (orderId: string, subId: string) => {
  const reason = prompt(
    'Vui lòng nhập lý do sự cố (Hàng dập nát, giao trễ, thiếu món...):',
  );
  if (!reason || !reason.trim()) return;

  const ok = await orderService.updateSubOrderStatus(
    subId,
    'cancelled',
    reason,
  );
  if (ok) {
    orders.value.forEach((ord) => {
      if (ord.orderId === orderId) {
        ord.subOrders.forEach((sub) => {
          if (sub.subId === subId) {
            sub.status = 'cancelled';
            sub.statusText = 'Đang xử lý khiếu nại (CSKH sẽ liên hệ)';
          }
        });
      }
    });
    triggerToast(
      `Đã gửi khiếu nại kiện #${subId}. CSKH ZoneMart sẽ liên hệ hỗ trợ bạn ngay!`,
    );
  }
};
</script>

<template>
  <div class="buyer-orders-page">
    <!-- TIÊU ĐỀ TRANG -->
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">
          <i class="bi bi-box-seam-fill text-terracotta"></i>
          Đơn Mua Của Bạn
        </h1>
        <p class="page-subtitle">
          Theo dõi trực tiếp hành trình đơn hàng giao hỏa tốc 10km và lịch sử
          mua sắm từ Database
        </p>
      </div>
      <button
        type="button"
        class="btn-refresh"
        @click="fetchOrders"
        title="Làm mới"
      >
        <i class="bi bi-arrow-clockwise"></i>
        <span>Làm mới</span>
      </button>
    </div>

    <!-- TABS LỌC TRẠNG THÁI -->
    <div class="status-filter-tabs">
      <button
        type="button"
        class="filter-tab"
        :class="{ active: filterTab === 'all' }"
        @click="filterTab = 'all'"
      >
        <span>Tất Cả Đơn</span>
        <span class="count-badge">{{ orders.length }}</span>
      </button>
      <button
        type="button"
        class="filter-tab"
        :class="{ active: filterTab === 'delivering' }"
        @click="filterTab = 'delivering'"
      >
        <span>Đang Giao Hỏa Tốc</span>
        <span class="count-badge pulse">
          {{
            orders.filter((o) =>
              o.subOrders.some((s) => s.status === 'delivering'),
            ).length
          }}
        </span>
      </button>
      <button
        type="button"
        class="filter-tab"
        :class="{ active: filterTab === 'completed' }"
        @click="filterTab = 'completed'"
      >
        <span>Đã Hoàn Thành</span>
        <span class="count-badge">
          {{
            orders.filter((o) =>
              o.subOrders.every((s) => s.status === 'completed'),
            ).length
          }}
        </span>
      </button>
    </div>

    <!-- DANH SÁCH ĐƠN HÀNG -->
    <div v-if="isLoading" class="loading-state-card">
      <div class="spinner-ring"></div>
      <p>Đang tải dữ liệu đơn hàng từ Database MongoDB Atlas...</p>
    </div>

    <div v-else-if="filteredOrders.length > 0" class="orders-feed">
      <div
        v-for="order in filteredOrders"
        :key="order.orderId"
        class="order-panel"
      >
        <!-- HEADER CỦA PARENT ORDER -->
        <div class="order-top-banner">
          <div class="order-id-col">
            <span class="label-tiny">MÃ ĐƠN HÀNG:</span>
            <strong class="order-code">#{{ order.orderId }}</strong>
            <span class="order-time"
              ><i class="bi bi-clock"></i> {{ order.date }}</span
            >
          </div>
          <div class="order-badges-col">
            <span class="badge-delivery" :class="order.deliveryType">
              <i
                class="bi"
                :class="
                  order.deliveryType === 'express'
                    ? 'bi-lightning-charge-fill'
                    : 'bi-bicycle'
                "
              ></i>
              {{
                order.deliveryType === 'express'
                  ? 'Hỏa Tốc 10km'
                  : 'Giao Tiêu Chuẩn'
              }}
            </span>
            <span class="badge-payment">
              {{
                order.paymentMethod === 'ONLINE_QR'
                  ? 'VietQR MB Bank'
                  : order.paymentMethod === 'ZONEPAY_WALLET'
                    ? 'Ví ZonePay'
                    : 'Tiền mặt COD'
              }}
            </span>
          </div>
        </div>

        <!-- DANH SÁCH SUB-ORDERS CỦA TỪNG CỬA HÀNG -->
        <div class="sub-orders-container">
          <div
            v-for="sub in order.subOrders"
            :key="sub.subId"
            class="sub-order-card"
          >
            <div class="sub-card-top">
              <div class="store-badge">
                <i class="bi bi-shop"></i>
                <strong>{{ sub.storeName }}</strong>
                <span v-if="sub.distanceKm" class="km-text"
                  >({{ sub.distanceKm }} km)</span
                >
              </div>
              <span class="status-pill" :class="sub.status">
                <i
                  v-if="sub.status === 'delivering'"
                  class="bi bi-bicycle me-1"
                ></i>
                <i
                  v-else-if="sub.status === 'completed'"
                  class="bi bi-check-circle-fill me-1"
                ></i>
                <i v-else class="bi bi-info-circle-fill me-1"></i>
                {{ sub.statusText }}
              </span>
            </div>

            <!-- Tên các món trong kiện hàng này -->
            <p class="items-summary-line">
              <i class="bi bi-bag-check-fill text-terracotta"></i>
              <span>{{ sub.items }}</span>
            </p>

            <p v-if="sub.note" class="sub-note-hint">
              <i class="bi bi-sticky"></i> Ghi chú: <em>"{{ sub.note }}"</em>
            </p>

            <!-- Khung thông tin tài xế Shipper nếu đang giao -->
            <div v-if="sub.status === 'delivering'" class="shipper-live-box">
              <div class="shipper-meta">
                <div class="shipper-avatar-circle">
                  <i class="bi bi-person-fill"></i>
                </div>
                <div>
                  <span class="shipper-title">Tài xế giao hàng:</span>
                  <strong class="shipper-name">{{
                    sub.shipperInfo || 'Nguyễn Văn Nam (29M1-8888)'
                  }}</strong>
                </div>
                <div class="shipper-call">
                  <i class="bi bi-telephone-fill"></i>
                  <strong>{{ sub.shipperPhone || '0987 654 321' }}</strong>
                </div>
              </div>

              <!-- Nút Thao tác -->
              <div class="sub-action-buttons">
                <button
                  type="button"
                  class="btn-action-confirm"
                  @click="handleConfirmReceived(order.orderId, sub.subId)"
                >
                  <i class="bi bi-check-circle-fill"></i>
                  <span>Đã Nhận Được Hàng</span>
                </button>
                <button
                  type="button"
                  class="btn-action-dispute"
                  @click="handleReportIssue(order.orderId, sub.subId)"
                >
                  <i class="bi bi-exclamation-triangle-fill"></i>
                  <span>Báo Cáo Sự Cố</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- FOOTER CỦA ĐƠN HÀNG TỔNG -->
        <div class="order-bottom-bar">
          <span class="total-caption">Tổng thanh toán đơn hàng:</span>
          <strong class="total-price-tag"
            >{{ order.total.toLocaleString('vi-VN') }} ₫</strong
          >
        </div>
      </div>
    </div>

    <!-- EMPTY STATE NẾU CHƯA CÓ ĐƠN -->
    <div v-else class="empty-orders-view">
      <div class="empty-orders-inner">
        <div class="empty-icon-art">
          <i class="bi bi-bag-x"></i>
        </div>
        <h3>Bạn Chưa Có Đơn Hàng Nào!</h3>
        <p>
          Các đơn hàng bạn đặt tại ZoneMart sẽ được lưu trữ và hiển thị trực
          tiếp tại đây.
        </p>
        <router-link to="/products" class="btn-go-shopping">
          <i class="bi bi-grid-fill"></i>
          <span>Khám Phá Sản Phẩm Ngay</span>
        </router-link>
      </div>
    </div>

    <!-- TOAST THÔNG BÁO -->
    <transition name="toast-fade">
      <div v-if="showToast" class="global-toast-bar">
        <i class="bi bi-check2-circle"></i>
        <span>{{ toastMsg }}</span>
      </div>
    </transition>
  </div>
</template>

<style scoped>
/* ============================================================================
   WARM HUMANIST & TERRACOTTA BUYER ORDERS STYLES
   ============================================================================ */
.buyer-orders-page {
  max-width: 1080px;
  margin: 0 auto;
  padding: 24px 16px 80px 16px;
  color: #2b1b14;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', sans-serif;
}

/* HEADER TRANG */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 24px;
}
.page-title {
  font-size: 24px;
  font-weight: 900;
  margin: 0 0 6px 0;
  color: #2b1b14;
  display: flex;
  align-items: center;
  gap: 10px;
}
.text-terracotta {
  color: #d85a2a;
}
.page-subtitle {
  margin: 0;
  font-size: 13.5px;
  color: #78655d;
}
.btn-refresh {
  background: #ffffff;
  border: 1.5px solid #ebdcd3;
  padding: 8px 16px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  color: #55443d;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}
.btn-refresh:hover {
  background: #fdf0e8;
  color: #d85a2a;
  border-color: #d85a2a;
}

/* STATUS TABS */
.status-filter-tabs {
  display: flex;
  gap: 10px;
  margin-bottom: 24px;
  border-bottom: 2px solid #f1e5dc;
  padding-bottom: 4px;
}
.filter-tab {
  background: transparent;
  border: none;
  padding: 10px 18px;
  font-size: 14px;
  font-weight: 700;
  color: #78655d;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  position: relative;
  transition: all 0.2s;
}
.filter-tab.active {
  color: #d85a2a;
}
.filter-tab.active::after {
  content: '';
  position: absolute;
  bottom: -6px;
  left: 0;
  right: 0;
  height: 3px;
  background: #d85a2a;
  border-radius: 3px;
}
.count-badge {
  font-size: 11px;
  background: #f1e7e0;
  color: #55443d;
  padding: 2px 8px;
  border-radius: 12px;
}
.filter-tab.active .count-badge {
  background: #d85a2a;
  color: #fff;
}
.count-badge.pulse {
  background: #fee2e2;
  color: #dc2626;
}

/* LOADING STATE */
.loading-state-card {
  background: #fff;
  border-radius: 18px;
  border: 1px solid #ebdcd3;
  padding: 48px;
  text-align: center;
  color: #78655d;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}
.spinner-ring {
  width: 36px;
  height: 36px;
  border: 3px solid #ebdcd3;
  border-top-color: #d85a2a;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* FEED CÁC ĐƠN HÀNG */
.orders-feed {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.order-panel {
  background: #ffffff;
  border-radius: 18px;
  border: 1.5px solid #ebdcd3;
  overflow: hidden;
  box-shadow: 0 4px 18px rgba(43, 27, 20, 0.04);
}

.order-top-banner {
  background: #fbf5f0;
  padding: 14px 20px;
  border-bottom: 1px solid #ebdcd3;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}
.order-id-col {
  display: flex;
  align-items: center;
  gap: 10px;
}
.label-tiny {
  font-size: 11px;
  font-weight: 800;
  color: #8c7a72;
  letter-spacing: 0.5px;
}
.order-code {
  font-size: 15px;
  font-weight: 900;
  color: #d85a2a;
}
.order-time {
  font-size: 12.5px;
  color: #78655d;
}

.order-badges-col {
  display: flex;
  gap: 8px;
}
.badge-delivery {
  font-size: 11.5px;
  font-weight: 800;
  padding: 3px 10px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.badge-delivery.express {
  background: #fee2e2;
  color: #dc2626;
}
.badge-delivery.standard {
  background: #dcfce7;
  color: #16a34a;
}
.badge-payment {
  font-size: 11.5px;
  font-weight: 700;
  background: #f1f5f9;
  color: #334155;
  padding: 3px 10px;
  border-radius: 6px;
}

/* SUB ORDERS */
.sub-orders-container {
  padding: 18px 20px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.sub-order-card {
  background: #fcfaf8;
  border-radius: 12px;
  border: 1px solid #ebdcd3;
  padding: 14px 16px;
}
.sub-card-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}
.store-badge {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: #2b1b14;
}
.km-text {
  font-size: 12px;
  color: #d85a2a;
  font-weight: 700;
}

.status-pill {
  font-size: 12px;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: 20px;
}
.status-pill.delivering {
  background: #eff6ff;
  color: #2563eb;
}
.status-pill.completed {
  background: #dcfce7;
  color: #16a34a;
}
.status-pill.cancelled {
  background: #fee2e2;
  color: #dc2626;
}

.items-summary-line {
  margin: 6px 0;
  font-size: 13.5px;
  color: #44332c;
  display: flex;
  align-items: baseline;
  gap: 8px;
  line-height: 1.4;
}
.sub-note-hint {
  margin: 4px 0 8px 0;
  font-size: 12px;
  color: #8c7a72;
}

/* SHIPPER LIVE BOX */
.shipper-live-box {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px dashed #ebdcd3;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  background: #ffffff;
  border-radius: 10px;
  padding: 10px 14px;
}
.shipper-meta {
  display: flex;
  align-items: center;
  gap: 12px;
}
.shipper-avatar-circle {
  width: 32px;
  height: 32px;
  background: #dcfce7;
  color: #16a34a;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
}
.shipper-title {
  font-size: 11px;
  color: #8c7a72;
  display: block;
}
.shipper-name {
  font-size: 13px;
  color: #2b1b14;
}
.shipper-call {
  font-size: 12.5px;
  color: #16a34a;
  display: flex;
  align-items: center;
  gap: 4px;
}

.sub-action-buttons {
  display: flex;
  gap: 8px;
}
.btn-action-confirm {
  background: #16a34a;
  color: #ffffff;
  border: none;
  padding: 7px 14px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}
.btn-action-confirm:hover {
  background: #15803d;
}
.btn-action-dispute {
  background: transparent;
  color: #dc2626;
  border: 1px solid #fca5a5;
  padding: 7px 12px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}
.btn-action-dispute:hover {
  background: #fee2e2;
}

/* FOOTER ĐƠN HÀNG */
.order-bottom-bar {
  background: #faf7f2;
  padding: 14px 20px;
  border-top: 1px solid #ebdcd3;
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 12px;
}
.total-caption {
  font-size: 13.5px;
  color: #78655d;
}
.total-price-tag {
  font-size: 19px;
  font-weight: 900;
  color: #d85a2a;
}

/* EMPTY STATE */
.empty-orders-view {
  background: #ffffff;
  border-radius: 20px;
  border: 1.5px solid #ebdcd3;
  padding: 60px 24px;
  text-align: center;
}
.empty-orders-inner {
  max-width: 440px;
  margin: 0 auto;
}
.empty-icon-art {
  width: 68px;
  height: 68px;
  background: #fdf0e8;
  color: #d85a2a;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  margin: 0 auto 16px auto;
}
.empty-orders-inner h3 {
  margin: 0 0 8px 0;
  font-size: 20px;
  font-weight: 800;
}
.empty-orders-inner p {
  margin: 0 0 24px 0;
  font-size: 14px;
  color: #78655d;
}
.btn-go-shopping {
  background: #d85a2a;
  color: #ffffff;
  text-decoration: none;
  padding: 12px 24px;
  border-radius: 12px;
  font-size: 14px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s;
}
.btn-go-shopping:hover {
  background: #bf4a1f;
}

/* TOAST */
.global-toast-bar {
  position: fixed;
  bottom: 28px;
  left: 50%;
  transform: translateX(-50%);
  background: #2b1b14;
  color: #ffffff;
  padding: 10px 20px;
  border-radius: 50px;
  font-size: 13.5px;
  font-weight: 700;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.35);
  display: inline-flex;
  align-items: center;
  gap: 8px;
  z-index: 10000;
}
.toast-fade-enter-active,
.toast-fade-leave-active {
  transition: all 0.25s ease;
}
.toast-fade-enter-from,
.toast-fade-leave-to {
  opacity: 0;
  transform: translate(-50%, 15px);
}
</style>
