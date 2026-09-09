<script setup lang="ts">
/**
 * ================================================================
 * TRANG QUẢN LÝ HỒ SƠ & TÀI KHOẢN ZONEMART (NEW PROFILE CENTER)
 * Thiết kế hoàn toàn mới: Tràn viền, Bento Layout, Thẻ ví ZonePay
 * ================================================================
 */
import { ref, reactive } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

// Tab state
const currentTab = ref<"profile" | "orders" | "addresses" | "wallet">("profile");

// User profile state
const user = reactive({
  fullName: "Nguyễn Hoàng Huy",
  username: "hoanghuy206",
  email: "alex.huy@zonemart.vn",
  phone: "0988 776 655",
  gender: "male",
  birthDate: "2000-06-15",
  avatarUrl: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=400&q=80",
  tier: "Hạng Vàng (Gold Member)",
  tierProgress: 75,
  zonePayBalance: 1250000,
  points: 2450,
  vouchersCount: 6,
  ordersCount: 28
});

// Toast alert
const toastMessage = ref("");
const showToast = ref(false);

const triggerToast = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2800);
};

// Avatar upload
const fileInput = ref<HTMLInputElement | null>(null);
const triggerAvatarUpload = () => {
  fileInput.value?.click();
};

const handleAvatarChange = (e: Event) => {
  const target = e.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    const file = target.files[0];
    const reader = new FileReader();
    reader.onload = (event) => {
      if (event.target?.result) {
        user.avatarUrl = event.target.result as string;
        triggerToast("Cập nhật ảnh đại diện thành công!");
      }
    };
    reader.readAsDataURL(file);
  }
};

// Save profile changes
const isSaving = ref(false);
const handleSaveProfile = () => {
  isSaving.value = true;
  setTimeout(() => {
    isSaving.value = false;
    triggerToast("Đã lưu thông tin hồ sơ thành công!");
  }, 600);
};

// Mock Recent Orders
const orders = ref([
  {
    id: "ZM-9982",
    store: "Cơm Tấm Sài Gòn 10km",
    date: "Hôm nay, 11:30",
    status: "delivering",
    statusText: "Tài xế đang giao (Khoảng 12 phút nữa)",
    items: "1x Cơm sườn bì chả đặc biệt, 1x Canh rong biển thịt bằm",
    total: 85000,
    storeDistance: "1.8 km"
  },
  {
    id: "ZM-9812",
    store: "Rau Củ Tươi VietGAP Cầu Giấy",
    date: "07/09/2026, 17:45",
    status: "completed",
    statusText: "Giao thành công",
    items: "1kg Cải ngọt VietGAP, 500g Cà chua bi, 1 nải Chuối tiêu",
    total: 115000,
    storeDistance: "2.4 km"
  },
  {
    id: "ZM-9430",
    store: "ZoneMart Fresh Meat & Seafood",
    date: "04/09/2026, 09:15",
    status: "completed",
    statusText: "Giao thành công",
    items: "500g Ba chỉ bò Mỹ cắt lát, 1 vỉ Nấm kim châm Hàn Quốc",
    total: 175000,
    storeDistance: "3.1 km"
  }
]);

// Mock Saved Addresses
const addresses = ref([
  {
    id: 1,
    title: "Nhà riêng",
    receiver: "Nguyễn Hoàng Huy",
    phone: "0988 776 655",
    detail: "Số 18, Ngõ 245 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội",
    isDefault: true,
    distanceTag: "Bán kính Hub: 1.2km"
  },
  {
    id: 2,
    title: "Văn phòng công ty",
    receiver: "Nguyễn Hoàng Huy",
    phone: "0988 776 655",
    detail: "Tầng 8, Tòa nhà Tech Tower, Phố Duy Tân, Quận Cầu Giấy, Hà Nội",
    isDefault: false,
    distanceTag: "Bán kính Hub: 3.5km"
  }
]);

const setDefaultAddress = (id: number) => {
  addresses.value.forEach(a => a.isDefault = (a.id === id));
  triggerToast("Đã đặt làm địa chỉ giao hàng mặc định!");
};

// Wallet Top-up
const quickTopUp = (amount: number) => {
  user.zonePayBalance += amount;
  triggerToast(`Nạp thành công +${amount.toLocaleString('vi-VN')} ₫ vào Ví ZonePay!`);
};
</script>

<template>
  <div class="new-profile-page">
    <!-- Toast Alert -->
    <div v-if="showToast" class="toast-popup">
      <i class="bi bi-check-circle-fill toast-icon"></i>
      <span>{{ toastMessage }}</span>
    </div>

    <input
      type="file"
      ref="fileInput"
      accept="image/*"
      class="hidden-file-input"
      @change="handleAvatarChange"
    />

    <div class="profile-main-container">
      <!-- 1. Header Profile Banner (Frameless & Editorial) -->
      <section class="profile-hero-card">
        <div class="hero-user-details">
          <!-- Avatar with tactile badge -->
          <div class="avatar-container" @click="triggerAvatarUpload" title="Nhấp để đổi ảnh đại diện">
            <img :src="user.avatarUrl" alt="Avatar" class="user-avatar-img" />
            <button class="camera-btn" type="button">
              <i class="bi bi-camera-fill"></i>
            </button>
          </div>

          <!-- User Info & Tier -->
          <div class="user-meta-info">
            <div class="name-row">
              <h1 class="user-display-name">{{ user.fullName }}</h1>
              <span class="member-tag">
                <i class="bi bi-award-fill"></i>
                {{ user.tier }}
              </span>
            </div>
            <p class="user-subline">
              <span>@{{ user.username }}</span>
              <span class="divider-dot">•</span>
              <span>{{ user.phone }}</span>
              <span class="divider-dot">•</span>
              <span class="text-green">Đã xác minh KYC 100%</span>
            </p>

            <!-- Loyalty Progress bar -->
            <div class="tier-progress-box">
              <div class="progress-labels">
                <span>Tiến trình thăng hạng <strong>VIP Platinum</strong></span>
                <span>75% (Còn 500k chi tiêu)</span>
              </div>
              <div class="progress-track">
                <div class="progress-fill" :style="{ width: user.tierProgress + '%' }"></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Quick Stat Chips (Wallet, Vouchers, Points) -->
        <div class="hero-stats-grid">
          <div class="stat-box wallet-box" @click="currentTab = 'wallet'">
            <div class="stat-icon-wrap bg-orange">
              <i class="bi bi-wallet2"></i>
            </div>
            <div class="stat-content">
              <span class="stat-label">Số dư Ví ZonePay</span>
              <strong class="stat-value text-orange">{{ user.zonePayBalance.toLocaleString('vi-VN') }} ₫</strong>
            </div>
          </div>

          <div class="stat-box" @click="triggerToast('Bạn đang có 6 mã freeship 10km')">
            <div class="stat-icon-wrap bg-blue">
              <i class="bi bi-ticket-perforated-fill"></i>
            </div>
            <div class="stat-content">
              <span class="stat-label">Voucher Giảm Giá</span>
              <strong class="stat-value">{{ user.vouchersCount }} Mã khả dụng</strong>
            </div>
          </div>

          <div class="stat-box" @click="triggerToast('Tích lũy khi mua đơn hàng quanh 10km')">
            <div class="stat-icon-wrap bg-amber">
              <i class="bi bi-stars"></i>
            </div>
            <div class="stat-content">
              <span class="stat-label">Điểm Thưởng Zone</span>
              <strong class="stat-value">{{ user.points.toLocaleString('vi-VN') }} Điểm</strong>
            </div>
          </div>
        </div>
      </section>

      <!-- 2. Modern Navigation Tabs -->
      <nav class="profile-nav-tabs">
        <button
          class="tab-btn"
          :class="{ active: currentTab === 'profile' }"
          @click="currentTab = 'profile'"
        >
          <i class="bi bi-person-lines-fill"></i>
          <span>Thông Tin Cá Nhân</span>
        </button>

        <button
          class="tab-btn"
          :class="{ active: currentTab === 'orders' }"
          @click="currentTab = 'orders'"
        >
          <i class="bi bi-bag-check-fill"></i>
          <span>Đơn Hàng Gần Đây</span>
          <span class="badge-count">{{ orders.length }}</span>
        </button>

        <button
          class="tab-btn"
          :class="{ active: currentTab === 'addresses' }"
          @click="currentTab = 'addresses'"
        >
          <i class="bi bi-geo-alt-fill"></i>
          <span>Sổ Địa Chỉ (Bán Kính 10km)</span>
        </button>

        <button
          class="tab-btn"
          :class="{ active: currentTab === 'wallet' }"
          @click="currentTab = 'wallet'"
        >
          <i class="bi bi-credit-card-2-front-fill"></i>
          <span>Ví ZonePay & Thanh Toán</span>
        </button>
      </nav>

      <!-- 3. Tab Contents -->
      <main class="tab-content-container">
        <!-- TAB 1: THÔNG TIN CÁ NHÂN -->
        <div v-if="currentTab === 'profile'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Hồ Sơ Cá Nhân</h2>
              <p class="pane-subtitle">Quản lý thông tin tài khoản, bảo mật và thông tin liên hệ của bạn.</p>
            </div>
            <button class="btn-save-primary" :disabled="isSaving" @click="handleSaveProfile">
              <span v-if="isSaving" class="spinner-small"></span>
              <span v-else>
                <i class="bi bi-check2-circle"></i>
                Lưu Thay Đổi
              </span>
            </button>
          </div>

          <div class="form-bento-grid">
            <!-- Left Info Card -->
            <div class="bento-card">
              <h3 class="card-section-title">
                <i class="bi bi-person-badge"></i>
                Thông Tin Cơ Bản
              </h3>

              <div class="fields-grid-2">
                <div class="form-group">
                  <label class="form-label">Họ và tên đầy đủ</label>
                  <input v-model="user.fullName" type="text" class="form-input" />
                </div>

                <div class="form-group">
                  <label class="form-label">Tên hiển thị (Username)</label>
                  <input v-model="user.username" type="text" class="form-input" />
                </div>

                <div class="form-group">
                  <label class="form-label">Ngày sinh</label>
                  <input v-model="user.birthDate" type="date" class="form-input" />
                </div>

                <div class="form-group">
                  <label class="form-label">Giới tính</label>
                  <div class="gender-pill-group">
                    <label class="gender-pill" :class="{ selected: user.gender === 'male' }">
                      <input type="radio" v-model="user.gender" value="male" class="hidden-radio" />
                      <span>Nam</span>
                    </label>
                    <label class="gender-pill" :class="{ selected: user.gender === 'female' }">
                      <input type="radio" v-model="user.gender" value="female" class="hidden-radio" />
                      <span>Nữ</span>
                    </label>
                    <label class="gender-pill" :class="{ selected: user.gender === 'other' }">
                      <input type="radio" v-model="user.gender" value="other" class="hidden-radio" />
                      <span>Khác</span>
                    </label>
                  </div>
                </div>
              </div>
            </div>

            <!-- Right Contact & Security Card -->
            <div class="bento-card">
              <h3 class="card-section-title">
                <i class="bi bi-shield-lock"></i>
                Liên Hệ & Bảo Mật
              </h3>

              <div class="form-group">
                <label class="form-label">Số điện thoại đăng ký</label>
                <div class="input-action-wrap">
                  <input v-model="user.phone" type="text" class="form-input font-mono" />
                  <span class="verified-chip">
                    <i class="bi bi-patch-check-fill"></i> Đã xác thực OTP
                  </span>
                </div>
              </div>

              <div class="form-group">
                <label class="form-label">Địa chỉ Email</label>
                <div class="input-action-wrap">
                  <input v-model="user.email" type="email" class="form-input" />
                  <span class="verified-chip">
                    <i class="bi bi-check-all"></i> Đã liên kết
                  </span>
                </div>
              </div>

              <div class="form-group">
                <label class="form-label">Mật khẩu</label>
                <div class="input-action-wrap">
                  <input type="password" value="••••••••••••" readonly class="form-input font-mono" />
                  <button type="button" class="btn-change-pwd" @click="triggerToast('Tính năng đổi mật khẩu đang mở')">
                    Đổi mật khẩu
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 2: ĐƠN HÀNG GẦN ĐÂY -->
        <div v-else-if="currentTab === 'orders'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Đơn Mua Gần Đây</h2>
              <p class="pane-subtitle">Theo dõi trạng thái giao hàng hỏa tốc và lịch sử đặt đồ ăn, thực phẩm.</p>
            </div>
            <button class="btn-secondary-action" @click="router.push('/products')">
              <i class="bi bi-bag-plus"></i> Đặt Thêm Món Ngon
            </button>
          </div>

          <div class="orders-feed">
            <div v-for="order in orders" :key="order.id" class="order-card">
              <div class="order-card-header">
                <div class="order-store-info">
                  <i class="bi bi-shop store-icon"></i>
                  <div>
                    <h4 class="store-name">{{ order.store }}</h4>
                    <span class="order-meta-text">Mã đơn: <strong>{{ order.id }}</strong> • {{ order.date }} • Cách {{ order.storeDistance }}</span>
                  </div>
                </div>

                <div class="order-status-badge" :class="order.status">
                  <span v-if="order.status === 'delivering'" class="pulse-icon"></span>
                  <span>{{ order.statusText }}</span>
                </div>
              </div>

              <div class="order-items-preview">
                <i class="bi bi-basket2"></i>
                <span>{{ order.items }}</span>
              </div>

              <div class="order-card-footer">
                <div class="order-total-price">
                  <span class="total-label">Tổng tiền:</span>
                  <strong class="price-val">{{ order.total.toLocaleString('vi-VN') }} ₫</strong>
                </div>

                <div class="order-actions">
                  <button v-if="order.status === 'delivering'" class="btn-action-primary" @click="router.push('/map')">
                    <i class="bi bi-cursor-fill"></i> Theo Dõi Shipper
                  </button>
                  <button class="btn-action-secondary" @click="triggerToast('Đã thêm lại món vào giỏ hàng!')">
                    <i class="bi bi-arrow-repeat"></i> Mua Lại
                  </button>
                  <button class="btn-action-ghost" @click="router.push('/contact')">
                    Khiếu nại
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 3: SỔ ĐỊA CHỈ -->
        <div v-else-if="currentTab === 'addresses'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Sổ Địa Chỉ Nhận Hàng</h2>
              <p class="pane-subtitle">Địa chỉ nhận hàng tối ưu cho thuật toán giao hỏa tốc 20 - 30 phút.</p>
            </div>
            <button class="btn-save-primary" @click="triggerToast('Mở bản đồ chọn tọa độ địa chỉ mới')">
              <i class="bi bi-plus-lg"></i> Thêm Địa Chỉ Mới
            </button>
          </div>

          <div class="addresses-grid">
            <div
              v-for="addr in addresses"
              :key="addr.id"
              class="address-card"
              :class="{ 'default-card': addr.isDefault }"
            >
              <div class="addr-header">
                <div class="addr-type-tag">
                  <i class="bi" :class="addr.id === 1 ? 'bi-house-door-fill' : 'bi-building-fill'"></i>
                  <span>{{ addr.title }}</span>
                </div>
                <span v-if="addr.isDefault" class="default-badge">
                  <i class="bi bi-star-fill"></i> Mặc định
                </span>
              </div>

              <h4 class="receiver-name">{{ addr.receiver }} <small>({{ addr.phone }})</small></h4>
              <p class="addr-detail">{{ addr.detail }}</p>
              <span class="radius-chip">
                <i class="bi bi-radar"></i> {{ addr.distanceTag }}
              </span>

              <div class="addr-footer-actions">
                <button
                  v-if="!addr.isDefault"
                  class="btn-set-default"
                  @click="setDefaultAddress(addr.id)"
                >
                  Đặt làm mặc định
                </button>
                <div class="addr-btn-group">
                  <button class="btn-icon-text" @click="triggerToast('Chỉnh sửa địa chỉ')">
                    <i class="bi bi-pencil"></i> Sửa
                  </button>
                  <button v-if="!addr.isDefault" class="btn-icon-text text-danger" @click="triggerToast('Đã xóa địa chỉ')">
                    <i class="bi bi-trash3"></i> Xóa
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 4: VÍ ZONEPAY & THANH TOÁN -->
        <div v-else-if="currentTab === 'wallet'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Ví Điện Tử ZonePay</h2>
              <p class="pane-subtitle">Thanh toán không tiền mặt siêu tốc, hoàn tiền tức thì khi mua thực phẩm tươi.</p>
            </div>
          </div>

          <div class="wallet-bento-layout">
            <!-- ZonePay Virtual Card -->
            <div class="virtual-card">
              <div class="card-top-row">
                <span class="card-brand">Zone<span>Pay</span></span>
                <i class="bi bi-wifi contactless-icon"></i>
              </div>
              <div class="card-chip-sim"></div>
              <div class="card-balance-box">
                <span class="card-balance-label">Số dư khả dụng</span>
                <div class="card-balance-number">{{ user.zonePayBalance.toLocaleString('vi-VN') }} ₫</div>
              </div>
              <div class="card-bottom-row">
                <span class="card-holder-name">{{ user.fullName.toUpperCase() }}</span>
                <span class="card-network">FAST 10KM PAY</span>
              </div>
            </div>

            <!-- Quick Top-up Action Box -->
            <div class="topup-box">
              <h3 class="topup-title">Nạp Tiền Nhanh Vào Ví</h3>
              <p class="topup-desc">Miễn phí nạp qua VNPay, MoMo, Vietcombank và Techcombank.</p>

              <div class="amount-pills">
                <button class="amount-btn" @click="quickTopUp(100000)">+100.000 ₫</button>
                <button class="amount-btn" @click="quickTopUp(200000)">+200.000 ₫</button>
                <button class="amount-btn active-pill" @click="quickTopUp(500000)">+500.000 ₫</button>
                <button class="amount-btn" @click="quickTopUp(1000000)">+1.000.000 ₫</button>
              </div>

              <div class="payment-partners">
                <span class="partner-label">Liên kết bảo mật:</span>
                <div class="partner-badges">
                  <span class="p-badge">MoMo</span>
                  <span class="p-badge">VNPay QR</span>
                  <span class="p-badge">ZaloPay</span>
                  <span class="p-badge">Visa/Mastercard</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
/* ==========================================================================
   GLOBAL LAYOUT (EDGE-TO-EDGE FULL SCREEN)
   ========================================================================== */
.new-profile-page {
  background-color: #f8fafc;
  min-height: calc(100vh - 72px);
  padding: 36px 32px 80px 32px;
  color: #1e293b;
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
}

.profile-main-container {
  max-width: 1280px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 32px;
}

.hidden-file-input,
.hidden-radio {
  display: none;
}

/* Toast Popup */
.toast-popup {
  position: fixed;
  top: 86px;
  right: 28px;
  background-color: #0f172a;
  color: #ffffff;
  padding: 14px 22px;
  border-radius: 14px;
  font-size: 14px;
  font-weight: 700;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.18);
  display: flex;
  align-items: center;
  gap: 10px;
  z-index: 9999;
  animation: slideToast 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-icon {
  color: #22c55e;
  font-size: 18px;
}

@keyframes slideToast {
  from { transform: translateY(-20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

/* ==========================================================================
   1. PROFILE HERO CARD (EDITORIAL & FRAMELESS)
   ========================================================================== */
.profile-hero-card {
  background: #ffffff;
  border-radius: 24px;
  padding: 36px 40px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 24px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 32px;
}

.hero-user-details {
  display: flex;
  align-items: center;
  gap: 28px;
  flex: 1;
  min-width: 320px;
}

/* Avatar */
.avatar-container {
  position: relative;
  width: 100px;
  height: 100px;
  cursor: pointer;
  flex-shrink: 0;
}

.user-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
  border: 3px solid #ffedd5;
  box-shadow: 0 4px 16px rgba(234, 88, 12, 0.15);
  transition: transform 0.2s ease;
}

.avatar-container:hover .user-avatar-img {
  transform: scale(1.03);
}

.camera-btn {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 32px;
  height: 32px;
  background: #ea580c;
  color: #ffffff;
  border: 2px solid #ffffff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

/* User Info */
.user-meta-info {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
}

.name-row {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.user-display-name {
  font-size: 26px;
  font-weight: 900;
  letter-spacing: -0.02em;
  color: #0f172a;
  margin: 0;
}

.member-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
  font-size: 12px;
  font-weight: 800;
  padding: 4px 10px;
  border-radius: 20px;
}

.user-subline {
  font-size: 13.5px;
  color: #64748b;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.divider-dot {
  color: #cbd5e1;
}

.text-green {
  color: #16a34a;
  font-weight: 700;
}

/* Tier progress */
.tier-progress-box {
  margin-top: 8px;
  max-width: 380px;
}

.progress-labels {
  display: flex;
  justify-content: space-between;
  font-size: 11.5px;
  color: #64748b;
  margin-bottom: 5px;
}

.progress-track {
  width: 100%;
  height: 6px;
  background: #f1f5f9;
  border-radius: 10px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #ea580c, #f97316);
  border-radius: 10px;
}

/* Right Stat Chips */
.hero-stats-grid {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
}

.stat-box {
  background: #f8fafc;
  border: 1px solid #f1f5f9;
  border-radius: 18px;
  padding: 16px 20px;
  display: flex;
  align-items: center;
  gap: 14px;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  min-width: 175px;
}

.stat-box:hover {
  transform: translateY(-2px);
  background: #ffffff;
  border-color: #fed7aa;
  box-shadow: 0 8px 20px -4px rgba(0, 0, 0, 0.06);
}

.stat-icon-wrap {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 19px;
  flex-shrink: 0;
}

.bg-orange { background: #fff7ed; color: #ea580c; }
.bg-blue { background: #eff6ff; color: #2563eb; }
.bg-amber { background: #fefce8; color: #d97706; }

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 11.5px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.stat-value {
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
}

.text-orange {
  color: #ea580c !important;
}

/* ==========================================================================
   2. MODERN NAVIGATION TABS
   ========================================================================== */
.profile-nav-tabs {
  display: flex;
  gap: 8px;
  border-bottom: 2px solid #e2e8f0;
  overflow-x: auto;
  padding-bottom: 4px;
}

.tab-btn {
  background: none;
  border: none;
  padding: 12px 20px;
  font-family: inherit;
  font-size: 14.5px;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border-radius: 12px 12px 0 0;
  position: relative;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.tab-btn:hover {
  color: #ea580c;
  background-color: #fff7ed;
}

.tab-btn.active {
  color: #ea580c;
  background-color: #ffffff;
}

.tab-btn.active::after {
  content: '';
  position: absolute;
  bottom: -6px;
  left: 0;
  right: 0;
  height: 3px;
  background-color: #ea580c;
  border-radius: 3px 3px 0 0;
}

.badge-count {
  background: #ea580c;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 12px;
}

/* ==========================================================================
   3. TAB CONTENT PANE
   ========================================================================== */
.tab-content-container {
  width: 100%;
}

.tab-pane {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.pane-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.pane-title {
  font-size: 22px;
  font-weight: 900;
  color: #0f172a;
  margin: 0 0 4px 0;
  letter-spacing: -0.01em;
}

.pane-subtitle {
  font-size: 14px;
  color: #64748b;
  margin: 0;
}

/* Button styles */
.btn-save-primary {
  background: #0f172a;
  color: #ffffff;
  border: none;
  font-family: inherit;
  font-size: 14px;
  font-weight: 700;
  padding: 12px 24px;
  border-radius: 14px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
}

.btn-save-primary:hover:not(:disabled) {
  background: #ea580c;
  transform: translateY(-2px);
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.28);
}

.btn-secondary-action {
  background: #ffffff;
  color: #0f172a;
  border: 1px solid #cbd5e1;
  font-family: inherit;
  font-size: 13.5px;
  font-weight: 700;
  padding: 10px 18px;
  border-radius: 12px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
}

.btn-secondary-action:hover {
  border-color: #ea580c;
  color: #ea580c;
}

/* ==========================================================================
   TAB 1: FORM BENTO
   ========================================================================== */
.form-bento-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

.bento-card {
  background: #ffffff;
  border-radius: 22px;
  padding: 28px 32px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.card-section-title {
  font-size: 17px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 10px;
}

.card-section-title i {
  color: #ea580c;
  font-size: 18px;
}

.fields-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-label {
  font-size: 13px;
  font-weight: 700;
  color: #334155;
}

.form-input {
  width: 100%;
  padding: 12px 16px;
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  font-family: inherit;
  font-size: 14px;
  color: #0f172a;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  background-color: #ffffff;
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}

.font-mono {
  font-family: 'SFMono-Regular', Consolas, monospace;
}

/* Gender Pills */
.gender-pill-group {
  display: flex;
  gap: 8px;
}

.gender-pill {
  flex: 1;
  text-align: center;
  padding: 10px 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  font-size: 13.5px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.gender-pill:hover {
  background: #fff7ed;
  border-color: #fed7aa;
}

.gender-pill.selected {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(234, 88, 12, 0.25);
}

/* Action input wrap */
.input-action-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.verified-chip {
  position: absolute;
  right: 12px;
  font-size: 11.5px;
  font-weight: 700;
  color: #16a34a;
  background: #f0fdf4;
  padding: 3px 8px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.btn-change-pwd {
  position: absolute;
  right: 8px;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #334155;
  font-size: 12px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
}

.btn-change-pwd:hover {
  border-color: #ea580c;
  color: #ea580c;
}

/* ==========================================================================
   TAB 2: ORDERS FEED
   ========================================================================== */
.orders-feed {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.order-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 24px 28px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 14px -2px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.order-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.order-store-info {
  display: flex;
  align-items: center;
  gap: 14px;
}

.store-icon {
  width: 42px;
  height: 42px;
  background: #fff7ed;
  color: #ea580c;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}

.store-name {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 2px 0;
}

.order-meta-text {
  font-size: 12.5px;
  color: #64748b;
}

.order-status-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 12.5px;
  font-weight: 700;
  padding: 6px 14px;
  border-radius: 20px;
}

.order-status-badge.delivering {
  background: #eff6ff;
  color: #2563eb;
  border: 1px solid #bfdbfe;
}

.order-status-badge.completed {
  background: #f0fdf4;
  color: #16a34a;
  border: 1px solid #bbf7d0;
}

.pulse-icon {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #2563eb;
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.2);
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.3); opacity: 0.6; }
}

.order-items-preview {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13.5px;
  color: #475569;
  background: #f8fafc;
  padding: 10px 16px;
  border-radius: 10px;
}

.order-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  padding-top: 6px;
}

.total-label {
  font-size: 13px;
  color: #64748b;
  margin-right: 6px;
}

.price-val {
  font-size: 17px;
  font-weight: 900;
  color: #ea580c;
}

.order-actions {
  display: flex;
  gap: 10px;
}

.btn-action-primary {
  background: #0f172a;
  color: #ffffff;
  border: none;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 16px;
  border-radius: 10px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: background 0.2s;
}

.btn-action-primary:hover {
  background: #ea580c;
}

.btn-action-secondary {
  background: #ffffff;
  color: #334155;
  border: 1px solid #cbd5e1;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 16px;
  border-radius: 10px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}

.btn-action-secondary:hover {
  border-color: #ea580c;
  color: #ea580c;
}

.btn-action-ghost {
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  padding: 8px 12px;
}

.btn-action-ghost:hover {
  color: #ef4444;
}

/* ==========================================================================
   TAB 3: ADDRESSES GRID
   ========================================================================== */
.addresses-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.address-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 24px;
  border: 1.5px solid #f1f5f9;
  box-shadow: 0 2px 14px -2px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 10px;
  transition: all 0.2s ease;
}

.address-card.default-card {
  border-color: #fed7aa;
  background-color: #fffaf5;
}

.addr-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.addr-type-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 800;
  color: #0f172a;
}

.default-badge {
  font-size: 11.5px;
  font-weight: 800;
  background: #ffedd5;
  color: #ea580c;
  padding: 3px 8px;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.receiver-name {
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.receiver-name small {
  font-weight: 500;
  color: #64748b;
}

.addr-detail {
  font-size: 13.5px;
  line-height: 1.55;
  color: #475569;
  margin: 0;
}

.radius-chip {
  font-size: 12px;
  color: #16a34a;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.addr-footer-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 8px;
  padding-top: 12px;
  border-top: 1px solid #f1f5f9;
}

.btn-set-default {
  background: transparent;
  border: none;
  font-size: 12.5px;
  font-weight: 700;
  color: #ea580c;
  cursor: pointer;
  padding: 0;
}

.btn-set-default:hover {
  text-decoration: underline;
}

.addr-btn-group {
  display: flex;
  gap: 12px;
}

.btn-icon-text {
  background: none;
  border: none;
  font-size: 12.5px;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.btn-icon-text:hover {
  color: #0f172a;
}

.text-danger {
  color: #ef4444 !important;
}

/* ==========================================================================
   TAB 4: WALLET BENTO
   ========================================================================== */
.wallet-bento-layout {
  display: grid;
  grid-template-columns: 1.1fr 1.4fr;
  gap: 28px;
  align-items: start;
}

.virtual-card {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  border-radius: 22px;
  padding: 28px 30px;
  color: #ffffff;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 230px;
  box-shadow: 0 16px 36px -8px rgba(15, 23, 42, 0.35);
  position: relative;
  overflow: hidden;
}

.virtual-card::before {
  content: '';
  position: absolute;
  top: -40px;
  right: -40px;
  width: 160px;
  height: 160px;
  border-radius: 50%;
  background: rgba(234, 88, 12, 0.25);
  filter: blur(30px);
}

.card-top-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-brand {
  font-size: 20px;
  font-weight: 900;
  letter-spacing: -0.5px;
}

.card-brand span {
  color: #ea580c;
}

.contactless-icon {
  font-size: 20px;
  opacity: 0.75;
}

.card-chip-sim {
  width: 38px;
  height: 28px;
  background: linear-gradient(135deg, #fcd34d 0%, #d97706 100%);
  border-radius: 6px;
  margin: 10px 0;
}

.card-balance-box {
  display: flex;
  flex-direction: column;
}

.card-balance-label {
  font-size: 11.5px;
  color: #94a3b8;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.card-balance-number {
  font-size: 24px;
  font-weight: 900;
  letter-spacing: -0.02em;
  color: #f8fafc;
}

.card-bottom-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 12px;
  color: #cbd5e1;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.card-network {
  color: #f97316;
  font-size: 11px;
}

/* Topup Box */
.topup-box {
  background: #ffffff;
  border-radius: 22px;
  padding: 28px 32px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.topup-title {
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.topup-desc {
  font-size: 13.5px;
  color: #64748b;
  margin: 0;
}

.amount-pills {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
  margin-top: 6px;
}

.amount-btn {
  padding: 12px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  font-family: inherit;
  font-size: 14px;
  font-weight: 800;
  color: #334155;
  cursor: pointer;
  transition: all 0.2s ease;
}

.amount-btn:hover {
  background: #fff7ed;
  border-color: #fed7aa;
  color: #ea580c;
  transform: translateY(-1px);
}

.amount-btn.active-pill {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.25);
}

.payment-partners {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 10px;
}

.partner-label {
  font-size: 12px;
  font-weight: 700;
  color: #64748b;
}

.partner-badges {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.p-badge {
  background: #f1f5f9;
  color: #475569;
  font-size: 11.5px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
}

/* Spinner */
.spinner-small {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
  display: inline-block;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1024px) {
  .new-profile-page {
    padding: 24px 16px 60px 16px;
  }

  .form-bento-grid {
    grid-template-columns: 1fr;
  }

  .wallet-bento-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .profile-hero-card {
    padding: 24px 20px;
  }

  .hero-user-details {
    flex-direction: column;
    text-align: center;
  }

  .name-row {
    justify-content: center;
  }

  .user-subline {
    justify-content: center;
  }

  .tier-progress-box {
    margin: 8px auto 0 auto;
  }

  .hero-stats-grid {
    width: 100%;
    flex-direction: column;
  }

  .fields-grid-2 {
    grid-template-columns: 1fr;
  }

  .addresses-grid {
    grid-template-columns: 1fr;
  }

  .order-card-footer {
    flex-direction: column;
    align-items: flex-start;
  }

  .order-actions {
    width: 100%;
    flex-wrap: wrap;
  }
}
</style>
