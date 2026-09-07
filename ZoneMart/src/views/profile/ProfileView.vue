<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

// Tab navigation in sidebar
const activeTab = ref<"personal" | "orders" | "stalls">("personal");

// Profile form state matching screenshot
const profileData = ref({
  firstName: "Alex",
  lastName: "Huy",
  password: "••••••••••••",
  email: "alex.huy@zonemart.vn",
  personalInfoName: "alex_zone2026",
  savePasswordOption: "SAVE Password",
  avatarUrl: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=400&q=80"
});

// Toast notification
const showToast = ref(false);
const toastMessage = ref("");

const triggerToast = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 3000);
};

// Handle Save Changes
const handleSaveChanges = () => {
  triggerToast("Đã lưu thay đổi hồ sơ thành công! (Saved Changes)");
};

// Handle Avatar file selection
const fileInputRef = ref<HTMLInputElement | null>(null);
const handleAvatarClick = () => {
  fileInputRef.value?.click();
};

const onFileSelected = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    const file = target.files[0];
    const reader = new FileReader();
    reader.onload = (e) => {
      if (e.target?.result) {
        profileData.value.avatarUrl = e.target.result as string;
        triggerToast("Ảnh đại diện đã được cập nhật!");
      }
    };
    reader.readAsDataURL(file);
  }
};

// Saved stalls mock data
const savedStalls = ref([
  { id: 1, name: "Cơm Tấm Sài Gòn 10km", address: "123 Cầu Giấy, Hà Nội", rating: 4.8, image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80" },
  { id: 2, name: "Trà Sữa ZoneTea & Coffee", address: "88 Trần Thái Tông", rating: 4.9, image: "https://images.unsplash.com/photo-1558857563-b371033873b8?auto=format&fit=crop&w=200&q=80" },
  { id: 3, name: "Bánh Mì Chảo Bờ Hồ", address: "45 Nguyễn Khánh Toàn", rating: 4.7, image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80" }
]);
</script>

<template>
  <div class="profile-layout-container">
    <!-- Hidden File Input for Avatar -->
    <input
      type="file"
      ref="fileInputRef"
      accept="image/*"
      class="hidden-file-input"
      @change="onFileSelected"
    />

    <!-- Toast Alert -->
    <div v-if="showToast" class="toast-notification">
      ✓ {{ toastMessage }}
    </div>

    <!-- MAIN WRAPPER CARD -->
    <div class="market-card-wrapper">
      <!-- LEFT SIDEBAR -->
      <aside class="sidebar-panel">
        <!-- LOGO SECTION -->
        <div class="sidebar-logo">
          <img src="/logo.png" alt="ZoneMart Logo" class="sidebar-brand-img" />
          <span class="logo-text">ZoneMart</span>
        </div>

        <!-- SIDEBAR NAVIGATION MENU -->
        <nav class="sidebar-nav">
          <button
            class="nav-btn"
            :class="{ active: activeTab === 'personal' }"
            @click="activeTab = 'personal'"
          >
            <span class="nav-icon">👤</span>
            <span class="nav-label">Personal Info</span>
          </button>

          <button
            class="nav-btn"
            :class="{ active: activeTab === 'orders' }"
            @click="activeTab = 'orders'"
          >
            <span class="nav-icon">📋</span>
            <span class="nav-label">Orders</span>
          </button>

          <button
            class="nav-btn"
            :class="{ active: activeTab === 'stalls' }"
            @click="activeTab = 'stalls'"
          >
            <span class="nav-icon">🔖</span>
            <span class="nav-label">Saved Stalls</span>
          </button>
        </nav>
      </aside>

      <!-- RIGHT MAIN CONTENT -->
      <main class="content-panel">
        <!-- TOP HEADER BAR -->
        <header class="top-header">
          <h1 class="market-brand-title">LOCAL MARKET</h1>

          <div class="user-profile-badge">
            <img :src="profileData.avatarUrl" alt="Avatar small" class="header-avatar" />
            <div class="badge-text">
              <span class="badge-title">Profile Info</span>
              <span class="badge-email">@meiohumia.com</span>
            </div>
            <span class="dropdown-arrow">⌵</span>
          </div>
        </header>

        <!-- VIEW: PERSONAL INFO (EXACT SCREENSHOT LAYOUT) -->
        <div v-if="activeTab === 'personal'" class="profile-main-body">
          <h2 class="section-heading">USER PROFILE</h2>

          <!-- TOP 4 INPUTS GRID (First Name, Last Name, Password, Email) -->
          <div class="top-input-grid">
            <div class="input-box-wrapper">
              <input
                v-model="profileData.firstName"
                type="text"
                placeholder="First Name"
                class="pill-input"
              />
            </div>
            <div class="input-box-wrapper">
              <input
                v-model="profileData.lastName"
                type="text"
                placeholder="Last Name"
                class="pill-input"
              />
            </div>
            <div class="input-box-wrapper">
              <input
                v-model="profileData.password"
                type="password"
                placeholder="Password"
                class="pill-input"
              />
            </div>
            <div class="input-box-wrapper">
              <input
                v-model="profileData.email"
                type="email"
                placeholder="Email"
                class="pill-input"
              />
            </div>
          </div>

          <!-- MIDDLE SECTION (AVATAR LEFT + STACKED FIELDS RIGHT) -->
          <div class="middle-form-section">
            <!-- LEFT: CIRCULAR AVATAR -->
            <div class="avatar-col">
              <div class="avatar-circle-wrap" @click="handleAvatarClick" title="Nhấn để đổi ảnh đại diện">
                <img :src="profileData.avatarUrl" alt="User avatar" class="circle-avatar-img" />
                <div class="avatar-upload-overlay">
                  <span>📷 Đổi ảnh</span>
                </div>
              </div>
              <p class="avatar-label-title">User avatar</p>
              <p class="avatar-label-sub">Bo góc tròn</p>
            </div>

            <!-- RIGHT: DETAILED FORM FIELDS -->
            <div class="details-col">
              <!-- Group 1: Personal info (First Name + Last Name stacked) -->
              <div class="field-group">
                <label class="group-label">Personal info</label>
                <div class="stacked-inputs-box">
                  <input
                    v-model="profileData.firstName"
                    type="text"
                    placeholder="First Name"
                    class="stacked-input top-border-input"
                  />
                  <input
                    v-model="profileData.lastName"
                    type="text"
                    placeholder="Last Name"
                    class="stacked-input bottom-border-input"
                  />
                </div>
              </div>

              <!-- Group 2: Email address (Personal infoname) -->
              <div class="field-group">
                <label class="group-label">Email address</label>
                <div class="input-box-wrapper">
                  <input
                    v-model="profileData.personalInfoName"
                    type="text"
                    placeholder="Personal infoname"
                    class="pill-input"
                  />
                </div>
              </div>

              <!-- Group 3: Email address / SAVE Password Dropdown -->
              <div class="field-group">
                <label class="group-label">Email address</label>
                <div class="dropdown-input-wrapper">
                  <select v-model="profileData.savePasswordOption" class="pill-input select-pill">
                    <option value="SAVE Password">SAVE Password</option>
                    <option value="Update Password">Update Password</option>
                    <option value="Reset Password">Reset Password</option>
                  </select>
                  <span class="select-caret">⌵</span>
                </div>
              </div>
            </div>
          </div>

          <!-- BOTTOM ACTION BUTTON -->
          <div class="bottom-action-bar">
            <button class="btn-save-changes" @click="handleSaveChanges">
              SAVE CHANGES
            </button>
          </div>
        </div>

        <!-- VIEW: ORDERS (QUẢN LÝ ĐƠN HÀNG) -->
        <div v-else-if="activeTab === 'orders'" class="tab-content-panel">
          <h2 class="section-heading">ORDERS (ĐƠN HÀNG CỦA BẠN)</h2>
          <p class="tab-desc">Theo dõi các đơn hàng giao nhanh trong bán kính 10km của bạn.</p>
          <div class="orders-quick-box">
            <div class="order-item-card">
              <div class="order-header-row">
                <span class="order-id">Đơn #ZM-8921</span>
                <span class="order-status-tag">🛵 Đang giao hàng</span>
              </div>
              <p class="order-shop">Cơm Tấm Sài Gòn • 1.8km</p>
              <p class="order-total">Tổng tiền: <strong>75.000 ₫</strong></p>
            </div>
            <button class="btn-secondary-link" @click="router.push('/buyer-orders')">
              👉 Xem chi tiết toàn bộ đơn mua
            </button>
          </div>
        </div>

        <!-- VIEW: SAVED STALLS (QUÁN ĐÃ LƯU) -->
        <div v-else-if="activeTab === 'stalls'" class="tab-content-panel">
          <h2 class="section-heading">SAVED STALLS (QUÁN ĐÃ LƯU)</h2>
          <p class="tab-desc">Các quán ăn & cửa hàng yêu thích gần bạn trong bán kính 10km.</p>
          <div class="stalls-grid">
            <div v-for="stall in savedStalls" :key="stall.id" class="stall-item-card">
              <img :src="stall.image" :alt="stall.name" class="stall-thumb" />
              <div class="stall-info">
                <h4>{{ stall.name }}</h4>
                <p>{{ stall.address }}</p>
                <span class="rating-badge">⭐ {{ stall.rating }}</span>
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
   GLOBAL LAYOUT & CONTAINERS
   ========================================================================== */
.profile-layout-container {
  min-height: 85vh;
  background-color: #f7f3ee;
  padding: 30px 16px 60px 16px;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  color: #2b231d;
}

.hidden-file-input {
  display: none;
}

/* Toast Message */
.toast-notification {
  position: fixed;
  top: 80px;
  right: 24px;
  background-color: #15803d;
  color: #ffffff;
  padding: 12px 20px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 14px;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.15);
  z-index: 9999;
  animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
  from { transform: translateX(50px); opacity: 0; }
  to { transform: translateX(0); opacity: 1; }
}

/* MAIN CARD CONTAINER (SPLIT INTO SIDEBAR + CONTENT) */
.market-card-wrapper {
  width: 100%;
  max-width: 980px;
  background-color: #fcf9f5;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(78, 42, 23, 0.08);
  display: flex;
  min-height: 640px;
  border: 1px solid #ebdcd1;
}

/* ==========================================================================
   LEFT SIDEBAR (TERRACOTTA THEME #b9441a)
   ========================================================================== */
.sidebar-panel {
  width: 230px;
  background-color: #ba441b;
  color: #ffffff;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  padding: 24px 0;
}

/* Sidebar Logo */
.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 24px 28px 24px;
}

.sidebar-brand-img {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  object-fit: cover;
  background-color: #ffffff;
  padding: 2px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.18);
}

.sidebar-logo .logo-text {
  font-size: 20px;
  font-weight: 800;
  letter-spacing: 0.5px;
  color: #ffffff;
}

/* Sidebar Navigation Items */
.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 0 12px;
}

.nav-btn {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 12px 16px;
  background: transparent;
  border: none;
  color: rgba(255, 255, 255, 0.88);
  font-size: 15px;
  font-weight: 500;
  text-align: left;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.nav-btn:hover {
  background-color: rgba(255, 255, 255, 0.12);
  color: #ffffff;
}

.nav-btn.active {
  background-color: rgba(0, 0, 0, 0.12);
  color: #ffffff;
  font-weight: 700;
}

.nav-icon {
  font-size: 17px;
}

/* ==========================================================================
   RIGHT CONTENT PANEL
   ========================================================================== */
.content-panel {
  flex: 1;
  padding: 24px 36px 36px 36px;
  display: flex;
  flex-direction: column;
  background-color: #fdfaf6;
}

/* TOP HEADER */
.top-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.market-brand-title {
  margin: 0;
  font-size: 22px;
  font-weight: 800;
  color: #ba441b;
  letter-spacing: 0.5px;
}

.user-profile-badge {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 8px;
  transition: background 0.2s;
}

.user-profile-badge:hover {
  background-color: #f4eae0;
}

.header-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  object-fit: cover;
}

.badge-text {
  display: flex;
  flex-direction: column;
  text-align: left;
}

.badge-title {
  font-size: 13px;
  font-weight: 700;
  color: #2b231d;
  line-height: 1.2;
}

.badge-email {
  font-size: 11px;
  color: #7b6f67;
  line-height: 1.2;
}

.dropdown-arrow {
  font-size: 11px;
  color: #7b6f67;
  margin-left: 2px;
}

/* ==========================================================================
   PROFILE BODY: SECTION TITLE & INPUTS
   ========================================================================== */
.profile-main-body {
  display: flex;
  flex-direction: column;
}

.section-heading {
  margin: 0 0 16px 0;
  font-size: 20px;
  font-weight: 800;
  color: #221d19;
  letter-spacing: 0.5px;
}

/* TOP 4-INPUT GRID */
.top-input-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px 18px;
  margin-bottom: 24px;
}

.input-box-wrapper {
  position: relative;
  width: 100%;
}

.pill-input {
  width: 100%;
  padding: 11px 18px;
  background-color: #fdf7f0;
  border: 1.5px solid #cb774c;
  border-radius: 12px;
  font-size: 14px;
  color: #3b2c23;
  outline: none;
  box-sizing: border-box;
  transition: border-color 0.2s, background-color 0.2s;
}

.pill-input:focus {
  border-color: #ba441b;
  background-color: #ffffff;
  box-shadow: 0 0 0 3px rgba(186, 68, 27, 0.12);
}

.pill-input::placeholder {
  color: #5c4b40;
  opacity: 0.85;
}

/* ==========================================================================
   MIDDLE SECTION: AVATAR + FORM DETAILS
   ========================================================================== */
.middle-form-section {
  display: grid;
  grid-template-columns: 180px 1fr;
  gap: 28px;
  align-items: flex-start;
  margin-bottom: 28px;
}

/* Left: Avatar Column */
.avatar-col {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.avatar-circle-wrap {
  width: 130px;
  height: 130px;
  border-radius: 50%;
  overflow: hidden;
  position: relative;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  margin-bottom: 12px;
  border: 2px solid #ecdcd2;
}

.circle-avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.avatar-upload-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.45);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 600;
  opacity: 0;
  transition: opacity 0.2s;
}

.avatar-circle-wrap:hover .avatar-upload-overlay {
  opacity: 1;
}

.avatar-label-title {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
  color: #2b231d;
}

.avatar-label-sub {
  margin: 4px 0 0 0;
  font-size: 13px;
  color: #7b6f67;
}

/* Right: Details Column */
.details-col {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.field-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.group-label {
  font-size: 13px;
  font-weight: 700;
  color: #2b231d;
}

/* Stacked Personal Info inputs */
.stacked-inputs-box {
  display: flex;
  flex-direction: column;
  background-color: #fdf7f0;
  border: 1.5px solid #cb774c;
  border-radius: 12px;
  overflow: hidden;
}

.stacked-input {
  width: 100%;
  padding: 10px 18px;
  background: transparent;
  border: none;
  font-size: 14px;
  color: #3b2c23;
  outline: none;
  box-sizing: border-box;
}

.stacked-input:focus {
  background-color: #ffffff;
}

.top-border-input {
  border-bottom: 1px solid #cb774c;
}

/* Dropdown select pill */
.dropdown-input-wrapper {
  position: relative;
  width: 100%;
}

.select-pill {
  appearance: none;
  -webkit-appearance: none;
  cursor: pointer;
  padding-right: 36px;
}

.select-caret {
  position: absolute;
  right: 18px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 13px;
  color: #5c4b40;
  pointer-events: none;
}

/* ==========================================================================
   BOTTOM ACTION BUTTON
   ========================================================================== */
.bottom-action-bar {
  width: 100%;
  margin-top: 10px;
}

.btn-save-changes {
  width: 100%;
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  padding: 14px 24px;
  border-radius: 30px;
  font-size: 15px;
  font-weight: 700;
  letter-spacing: 0.8px;
  cursor: pointer;
  transition: background-color 0.2s, transform 0.1s, box-shadow 0.2s;
  box-shadow: 0 4px 12px rgba(186, 68, 27, 0.25);
}

.btn-save-changes:hover {
  background-color: #a33813;
  box-shadow: 0 6px 16px rgba(186, 68, 27, 0.35);
}

.btn-save-changes:active {
  transform: scale(0.99);
}

/* ==========================================================================
   EXTRA TABS (ORDERS & SAVED STALLS)
   ========================================================================== */
.tab-content-panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.tab-desc {
  font-size: 14px;
  color: #6a5e55;
  margin: -8px 0 12px 0;
}

.order-item-card {
  background-color: #ffffff;
  border: 1.5px solid #ebdcd1;
  border-radius: 12px;
  padding: 16px 20px;
  margin-bottom: 14px;
}

.order-header-row {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
}

.order-id {
  font-weight: 700;
  color: #ba441b;
}

.order-status-tag {
  background-color: #fef3c7;
  color: #92400e;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.order-shop {
  margin: 0 0 6px 0;
  font-size: 14px;
  color: #3b2c23;
}

.order-total {
  margin: 0;
  font-size: 14px;
  color: #15803d;
}

.btn-secondary-link {
  background: transparent;
  border: 1.5px solid #ba441b;
  color: #ba441b;
  padding: 10px 18px;
  border-radius: 20px;
  font-weight: 600;
  cursor: pointer;
  align-self: flex-start;
  transition: all 0.2s;
}

.btn-secondary-link:hover {
  background-color: #ba441b;
  color: #ffffff;
}

.stalls-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 16px;
}

.stall-item-card {
  background-color: #ffffff;
  border: 1.5px solid #ebdcd1;
  border-radius: 12px;
  overflow: hidden;
  transition: transform 0.2s;
}

.stall-item-card:hover {
  transform: translateY(-2px);
}

.stall-thumb {
  width: 100%;
  height: 110px;
  object-fit: cover;
}

.stall-info {
  padding: 12px;
}

.stall-info h4 {
  margin: 0 0 4px 0;
  font-size: 14px;
  color: #2b231d;
}

.stall-info p {
  margin: 0 0 8px 0;
  font-size: 12px;
  color: #7b6f67;
}

.rating-badge {
  font-size: 12px;
  font-weight: 700;
  color: #d97706;
}

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 820px) {
  .market-card-wrapper {
    flex-direction: column;
  }

  .sidebar-panel {
    width: 100%;
    padding: 16px 20px;
  }

  .sidebar-nav {
    flex-direction: row;
    overflow-x: auto;
  }

  .content-panel {
    padding: 20px;
  }

  .middle-form-section {
    grid-template-columns: 1fr;
    justify-items: center;
  }

  .details-col {
    width: 100%;
  }
}

@media (max-width: 540px) {
  .top-input-grid {
    grid-template-columns: 1fr;
  }
}
</style>
