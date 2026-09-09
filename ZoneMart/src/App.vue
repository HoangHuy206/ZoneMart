<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import logoImg from './assets/logo.png';
import { useAuth, type UserRole } from './composables/useAuth';

const router = useRouter();
const auth = useAuth();

const isMobileMenuOpen = ref(false);
const isUserDropdownOpen = ref(false);
const searchQuery = ref('');
const cartItemCount = ref(3);

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value;
};

const toggleUserDropdown = () => {
  isUserDropdownOpen.value = !isUserDropdownOpen.value;
};

const closeDropdowns = () => {
  isUserDropdownOpen.value = false;
  isMobileMenuOpen.value = false;
};

const handleHeaderSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({
      path: '/products',
      query: { search: searchQuery.value.trim() },
    });
  } else {
    router.push('/products');
  }
};

const handleLogout = () => {
  auth.logout();
  closeDropdowns();
  router.push('/');
};

const handleRoleSwitch = (role: Exclude<UserRole, 'guest'>) => {
  auth.switchRole(role);
  closeDropdowns();
};
</script>

<template>
  <div class="app-wrapper" @click="isUserDropdownOpen = false">
    <!-- Header chuẩn theo hình Sample Mockup (Tự động ẩn ở trang 404 qua meta.hideHeader) -->
    <header v-if="!$route.meta.hideHeader" class="navbar">
      <div class="nav-container">
        <!-- 1. Logo thương hiệu bên trái -->
        <router-link to="/" class="brand-logo" @click="closeDropdowns">
          <img :src="logoImg" alt="ZoneMart Logo" class="brand-logo-img" />
          <div class="brand-text-block">
            <span class="brand-name"
              >Zone<span class="highlight">Mart</span></span
            >
            <span class="brand-tagline">Giao hàng hỏa tốc 10km</span>
          </div>
        </router-link>

        <!-- 2. Thanh tìm kiếm chính giữa -->
        <div class="header-search-wrap">
          <i class="bi bi-search search-icon" aria-hidden="true"></i>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm kiếm nông sản, thực phẩm tươi..."
            aria-label="Tìm kiếm sản phẩm"
            @keyup.enter="handleHeaderSearch"
          />
          <button
            v-if="searchQuery"
            type="button"
            class="clear-search-btn"
            aria-label="Xóa từ khóa tìm kiếm"
            @click="searchQuery = ''"
          >
            <i class="bi bi-x-circle-fill" aria-hidden="true"></i>
          </button>
        </div>

        <!-- 3. Khu vực bên phải: Tự động đổi theo vai trò Khách ghé thăm (Guest) hoặc Đã đăng nhập -->
        <!-- 3. Khu vực bên phải: Tự động đổi theo từng vai trò -->
        <div class="nav-right-actions">
          <!-- A. Navigation links trên Desktop theo vai trò -->
          <nav class="desktop-links">
            <!-- 1. Guest Links (Khách ghé thăm) -->
            <template v-if="!auth.isLoggedIn.value">
              <router-link
                to="/products"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-grid menu-mini-icon"></i>
                <span>Sản Phẩm</span>
              </router-link>
              <router-link
                to="/contact"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-headset menu-mini-icon"></i>
                <span>Liên Hệ</span>
              </router-link>
            </template>

            <!-- 2. Buyer Links (Khách hàng) -->
            <template v-else-if="auth.currentRole.value === 'buyer'">
              <router-link
                to="/products"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-grid menu-mini-icon"></i>
                <span>Sản Phẩm</span>
              </router-link>
              <router-link
                to="/buyer-orders"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-bag-check menu-mini-icon"></i>
                <span>Đơn Mua</span>
              </router-link>
              <router-link
                to="/profile"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-wallet2 menu-mini-icon"></i>
                <span>Ví Tiền</span>
              </router-link>
              <router-link
                to="/contact"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-headset menu-mini-icon"></i>
                <span>Hỗ Trợ</span>
              </router-link>
            </template>

            <!-- 3. Seller Links (Chủ gian hàng) -->
            <template v-else-if="auth.currentRole.value === 'seller'">
              <router-link
                to="/seller"
                class="quick-link highlight-pill seller-pill"
                active-class="active"
              >
                <i class="bi bi-shop menu-mini-icon"></i>
                <span>Kênh Bán Hàng</span>
              </router-link>
              <router-link
                to="/products"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-box-seam menu-mini-icon"></i>
                <span>Sản Phẩm Sàn</span>
              </router-link>
              <router-link
                to="/profile"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-cash-stack menu-mini-icon"></i>
                <span>Ví Doanh Thu</span>
              </router-link>
              <router-link
                to="/contact"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-patch-question menu-mini-icon"></i>
                <span>Hỗ Trợ Shop</span>
              </router-link>
            </template>

            <!-- 4. Shipper Links (Tài xế giao hàng) -->
            <template v-else-if="auth.currentRole.value === 'shipper'">
              <router-link
                to="/shipper"
                class="quick-link highlight-pill shipper-pill"
                active-class="active"
              >
                <i class="bi bi-bicycle menu-mini-icon"></i>
                <span>Nhận Đơn 10km</span>
              </router-link>
              <router-link
                to="/profile"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-cash-coin menu-mini-icon"></i>
                <span>Ví Thu Nhập</span>
              </router-link>
              <router-link
                to="/contact"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-telephone-outbound menu-mini-icon"></i>
                <span>Cứu Hộ / Trợ Giúp</span>
              </router-link>
            </template>

            <!-- 5. Admin Links (Quản trị viên) -->
            <template v-else-if="auth.currentRole.value === 'admin'">
              <router-link
                to="/admin"
                class="quick-link highlight-pill admin-pill"
                active-class="active"
              >
                <i class="bi bi-shield-check menu-mini-icon"></i>
                <span>Dashboard Admin</span>
              </router-link>
              <router-link
                to="/products"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-eye menu-mini-icon"></i>
                <span>Giám Sát Sàn</span>
              </router-link>
              <router-link
                to="/profile"
                class="quick-link"
                active-class="active"
              >
                <i class="bi bi-gear menu-mini-icon"></i>
                <span>Hệ Thống</span>
              </router-link>
            </template>
          </nav>

          <!-- B. Khi là Guest: 2 nút Đăng Ký và Đăng Nhập -->
          <div v-if="!auth.isLoggedIn.value" class="guest-auth-actions">
            <router-link to="/register" class="btn-auth-outline">
              Đăng Ký
            </router-link>
            <router-link to="/login" class="btn-auth-solid">
              Đăng Nhập
            </router-link>
          </div>

          <!-- C. Khi đã đăng nhập: Action Buttons & User Menu -->
          <div v-else class="logged-in-actions">
            <!-- Icon Tài khoản với Menu Dropdown -->
            <div class="user-menu-wrapper" @click.stop>
              <button
                class="icon-btn user-btn"
                title="Tài khoản của bạn"
                aria-label="Tài khoản của bạn"
                @click="toggleUserDropdown"
              >
                <svg
                  viewBox="0 0 24 24"
                  width="22"
                  height="22"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.8"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  aria-hidden="true"
                >
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                  <circle cx="12" cy="7" r="4"></circle>
                </svg>
              </button>

              <div v-if="isUserDropdownOpen" class="user-dropdown">
                <div class="dropdown-header">
                  <strong>Tài Khoản ZoneMart</strong>
                </div>
                <router-link
                  to="/profile"
                  class="dropdown-item"
                  @click="closeDropdowns"
                >
                  <i class="bi bi-person-circle menu-icon" aria-hidden="true"></i>
                  <span>Hồ Sơ & Ví Tiền</span>
                </router-link>
                <router-link
                  to="/buyer-orders"
                  class="dropdown-item"
                  @click="closeDropdowns"
                >
                  <i class="bi bi-bag-check menu-icon" aria-hidden="true"></i>
                  <span>Đơn Mua Của Bạn</span>
                </router-link>
                <router-link
                  to="/seller"
                  class="dropdown-item"
                  @click="closeDropdowns"
                >
                  <i class="bi bi-shop menu-icon" aria-hidden="true"></i>
                  <span>Kênh Quản Lý Bán Hàng</span>
                </router-link>
                <div class="dropdown-divider"></div>
                <router-link
                  to="/shipper"
                  class="dropdown-item"
                  @click="closeDropdowns"
                >
                  <i class="bi bi-bicycle menu-icon" aria-hidden="true"></i>
                  <span>Cổng Shipper</span>
                </router-link>
                <router-link
                  to="/admin"
                  class="dropdown-item"
                  @click="closeDropdowns"
                >
                  <i class="bi bi-shield-check menu-icon" aria-hidden="true"></i>
                  <span>Bảng Điều Khiển Admin</span>
                </router-link>
                <router-link
                  to="/contact"
                  class="dropdown-item"
                  @click="closeDropdowns"
                >
                  <i class="bi bi-headset menu-icon" aria-hidden="true"></i>
                  <span>Liên Hệ Hỗ Trợ</span>
                </router-link>
                <div class="dropdown-divider"></div>
                <button class="dropdown-item logout-btn" @click="handleLogout">
                  <i class="bi bi-box-arrow-right menu-icon" aria-hidden="true"></i>
                  <span>Đăng Xuất</span>
                </button>
              </div>
            </div>

            <!-- Icon Giỏ hàng kèm huy hiệu số lượng -->
            <router-link to="/cart" class="icon-btn cart-btn" title="Giỏ hàng" aria-label="Xem giỏ hàng">
              <svg
                viewBox="0 0 24 24"
                width="20"
                height="20"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                stroke-linecap="round"
                stroke-linejoin="round"
                aria-hidden="true"
              >
                <circle cx="9" cy="21" r="1"></circle>
                <circle cx="20" cy="21" r="1"></circle>
                <path
                  d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"
                ></path>
              </svg>
              <span class="cart-badge">{{ cartItemCount }}</span>
            </router-link>

            <!-- Lối tắt nhanh cho Shipper: Nút Trạng Thái Sẵn Sàng -->
            <router-link
              v-if="auth.currentRole.value === 'shipper'"
              to="/shipper"
              class="shipper-ready-tag"
              title="Xem đơn hàng mới"
            >
              <span class="status-dot-pulse"></span>
              <span>Sẵn Sàng</span>
            </router-link>

            <!-- User Menu Chip & Dropdown -->
            <div class="user-menu-wrapper" @click.stop>
              <button
                class="user-profile-chip"
                :class="auth.roleBadgeClass.value"
                title="Tài khoản của bạn"
                aria-label="Tài khoản của bạn"
                @click="toggleUserDropdown"
              >
                <img
                  v-if="auth.currentUser.value?.avatarUrl"
                  :src="auth.currentUser.value.avatarUrl"
                  :alt="auth.currentUser.value.fullName"
                  class="user-chip-avatar"
                />
                <span v-else class="user-chip-avatar-placeholder">
                  {{ auth.currentUser.value?.fullName?.charAt(0) || 'U' }}
                </span>

                <div class="user-chip-info">
                  <span class="user-chip-name">{{
                    auth.currentUser.value?.fullName
                  }}</span>
                  <span class="user-chip-role-badge">
                    {{ auth.roleLabel.value }}
                  </span>
                </div>
                <i class="bi bi-chevron-down chip-arrow"></i>
              </button>

              <!-- Dropdown Menu -->
              <div v-if="isUserDropdownOpen" class="user-dropdown">
                <!-- Dropdown Header Card -->
                <div
                  class="dropdown-header-card"
                  :class="auth.roleBadgeClass.value"
                >
                  <div class="dropdown-header-top">
                    <img
                      v-if="auth.currentUser.value?.avatarUrl"
                      :src="auth.currentUser.value.avatarUrl"
                      :alt="auth.currentUser.value.fullName"
                      class="dropdown-avatar"
                    />
                    <span v-else class="dropdown-avatar-placeholder">
                      {{ auth.currentUser.value?.fullName?.charAt(0) || 'U' }}
                    </span>
                    <div class="dropdown-user-details">
                      <strong class="dropdown-user-name">{{
                        auth.currentUser.value?.fullName
                      }}</strong>
                      <span class="dropdown-badge-pill">{{
                        auth.roleLabel.value
                      }}</span>
                    </div>
                  </div>
                  <div class="dropdown-header-sub">
                    <span class="dropdown-email-text">{{
                      auth.currentUser.value?.phoneEmail
                    }}</span>
                    <span
                      v-if="auth.currentUser.value?.storeName"
                      class="dropdown-store-text"
                    >
                      🏪 {{ auth.currentUser.value.storeName }}
                    </span>
                    <span
                      v-if="auth.currentUser.value?.vehiclePlate"
                      class="dropdown-vehicle-text"
                    >
                      🛵 {{ auth.currentUser.value.vehiclePlate }}
                    </span>
                    <div class="dropdown-wallet-row">
                      <span class="wallet-lbl">Số dư ví:</span>
                      <strong class="wallet-val">{{
                        auth.formatVND(auth.currentUser.value?.walletBalance)
                      }}</strong>
                    </div>
                  </div>
                </div>

                <!-- Dropdown Items theo Vai Trò -->
                <div class="dropdown-body-links">
                  <!-- 1. BUYER ITEMS -->
                  <template v-if="auth.currentRole.value === 'buyer'">
                    <router-link
                      to="/profile"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-person-circle menu-icon"></i>
                      <span>Hồ Sơ & Ví Cá Nhân</span>
                    </router-link>
                    <router-link
                      to="/buyer-orders"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-bag-check menu-icon"></i>
                      <span>Đơn Mua Của Bạn</span>
                    </router-link>
                    <router-link
                      to="/cart"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-cart3 menu-icon"></i>
                      <span>Giỏ Hàng ({{ cartItemCount }})</span>
                    </router-link>
                    <router-link
                      to="/contact"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-headset menu-icon"></i>
                      <span>Trung Tâm Hỗ Trợ 24/7</span>
                    </router-link>
                  </template>

                  <!-- 2. SELLER ITEMS -->
                  <template v-else-if="auth.currentRole.value === 'seller'">
                    <router-link
                      to="/admin"
                      class="dropdown-item highlight-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-shop menu-icon"></i>
                      <span>Kênh Bán Hàng & Đơn Shop</span>
                    </router-link>
                    <router-link
                      to="/profile"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-cash-stack menu-icon"></i>
                      <span>Ví Doanh Thu & Rút Tiền</span>
                    </router-link>
                    <router-link
                      to="/products"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-box-seam menu-icon"></i>
                      <span>Xem Mặt Hàng Trên Sàn</span>
                    </router-link>
                    <router-link
                      to="/contact"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-patch-question menu-icon"></i>
                      <span>Hỗ Trợ Đối Tác Bán Hàng</span>
                    </router-link>
                  </template>

                  <!-- 3. SHIPPER ITEMS -->
                  <template v-else-if="auth.currentRole.value === 'shipper'">
                    <router-link
                      to="/shipper"
                      class="dropdown-item highlight-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-bicycle menu-icon"></i>
                      <span>Bảng Nhận Đơn Hỏa Tốc</span>
                    </router-link>
                    <router-link
                      to="/profile"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-cash-coin menu-icon"></i>
                      <span>Ví Thu Nhập & Chuyến Đi</span>
                    </router-link>
                    <router-link
                      to="/contact"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-telephone-outbound menu-icon"></i>
                      <span>Tổng Đài Hỗ Trợ Sự Cố</span>
                    </router-link>
                  </template>

                  <!-- 4. ADMIN ITEMS -->
                  <template v-else-if="auth.currentRole.value === 'admin'">
                    <router-link
                      to="/admin"
                      class="dropdown-item highlight-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-shield-check menu-icon"></i>
                      <span>Bảng Điều Khiển Quản Trị</span>
                    </router-link>
                    <router-link
                      to="/products"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-eye menu-icon"></i>
                      <span>Kiểm Duyệt Sản Phẩm Sàn</span>
                    </router-link>
                    <router-link
                      to="/profile"
                      class="dropdown-item"
                      @click="closeDropdowns"
                    >
                      <i class="bi bi-gear menu-icon"></i>
                      <span>Cài Đặt Hệ Thống</span>
                    </router-link>
                  </template>
                </div>

                <!-- ROLE SWITCHER DÀNH CHO TEST TRỰC QUAN -->
                <div class="dropdown-role-switcher">
                  <div class="switcher-title">
                    <i class="bi bi-arrow-repeat"></i>
                    <span>Chuyển vai trò thử nghiệm:</span>
                  </div>
                  <div class="switcher-buttons">
                    <button
                      type="button"
                      class="switch-btn"
                      :class="{ active: auth.currentRole.value === 'buyer' }"
                      @click="handleRoleSwitch('buyer')"
                      title="Chuyển sang Khách Hàng"
                    >
                      👤 Buyer
                    </button>
                    <button
                      type="button"
                      class="switch-btn"
                      :class="{ active: auth.currentRole.value === 'seller' }"
                      @click="handleRoleSwitch('seller')"
                      title="Chuyển sang Chủ Cửa Hàng"
                    >
                      🌱 Seller
                    </button>
                    <button
                      type="button"
                      class="switch-btn"
                      :class="{ active: auth.currentRole.value === 'shipper' }"
                      @click="handleRoleSwitch('shipper')"
                      title="Chuyển sang Tài Xế"
                    >
                      🛵 Shipper
                    </button>
                    <button
                      type="button"
                      class="switch-btn"
                      :class="{ active: auth.currentRole.value === 'admin' }"
                      @click="handleRoleSwitch('admin')"
                      title="Chuyển sang Admin"
                    >
                      👑 Admin
                    </button>
                  </div>
                </div>

                <div class="dropdown-divider"></div>
                <button class="dropdown-item logout-btn" @click="handleLogout">
                  <i class="bi bi-box-arrow-right menu-icon"></i>
                  <span>Đăng Xuất</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Mobile Toggle Button -->
          <button
            class="mobile-toggle"
            :aria-label="isMobileMenuOpen ? 'Đóng Menu' : 'Mở Menu'"
            :aria-expanded="isMobileMenuOpen"
            @click.stop="toggleMobileMenu"
          >
            <i
              class="bi"
              :class="isMobileMenuOpen ? 'bi-x-lg' : 'bi-list'"
              aria-hidden="true"
            ></i>
          </button>
        </div>
      </div>

      <!-- Menu trên điện thoại -->
      <nav
        v-if="isMobileMenuOpen"
        class="mobile-menu"
        aria-label="Menu điều hướng di động"
      >
        <!-- Guest Mobile Links -->
        <template v-if="!auth.isLoggedIn.value">
          <router-link
            to="/products"
            class="mobile-link"
            @click="closeDropdowns"
          >
            <i class="bi bi-grid-fill menu-icon" aria-hidden="true"></i>
            <span>Sản Phẩm</span>
          </router-link>
          <router-link
            to="/contact"
            class="mobile-link"
            @click="closeDropdowns"
          >
            <i class="bi bi-headset menu-icon" aria-hidden="true"></i>
            <span>Liên Hệ</span>
          </router-link>
          <div class="mobile-divider"></div>
          <div class="mobile-auth-grid">
            <router-link
              to="/register"
              class="btn-auth-outline mobile-btn"
              @click="closeDropdowns"
            >
              <i class="bi bi-person-plus-fill"></i>
              <span>Đăng Ký</span>
            </router-link>
            <router-link
              to="/login"
              class="btn-auth-solid mobile-btn"
              @click="closeDropdowns"
            >
              <i class="bi bi-box-arrow-in-right"></i>
              <span>Đăng Nhập</span>
            </router-link>
          </div>
        </template>

        <!-- Logged-in Mobile Links -->
        <template v-else>
          <!-- User info card on mobile -->
          <div class="mobile-user-card" :class="auth.roleBadgeClass.value">
            <div class="mobile-user-name">
              {{ auth.currentUser.value?.fullName }}
            </div>
            <span class="mobile-role-tag">{{ auth.roleLabel.value }}</span>
          </div>

          <!-- Buyer Mobile -->
          <template v-if="auth.currentRole.value === 'buyer'">
            <router-link
              to="/products"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-grid-fill menu-icon"></i>
              <span>Sản Phẩm</span>
            </router-link>
            <router-link to="/cart" class="mobile-link" @click="closeDropdowns">
              <i class="bi bi-cart3 menu-icon"></i>
              <span>Giỏ Hàng ({{ cartItemCount }})</span>
            </router-link>
            <router-link
              to="/buyer-orders"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-bag-check menu-icon"></i>
              <span>Đơn Mua</span>
            </router-link>
            <router-link
              to="/profile"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-person-circle menu-icon"></i>
              <span>Hồ Sơ & Ví</span>
            </router-link>
          </template>

          <!-- Seller Mobile -->
          <template v-else-if="auth.currentRole.value === 'seller'">
            <router-link
              to="/seller"
              class="mobile-link highlight-mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-shop menu-icon"></i>
              <span>Kênh Bán Hàng & Đơn</span>
            </router-link>
            <router-link
              to="/products"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-box-seam menu-icon"></i>
              <span>Sản Phẩm Sàn</span>
            </router-link>
            <router-link
              to="/profile"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-cash-stack menu-icon"></i>
              <span>Ví Doanh Thu</span>
            </router-link>
          </template>

          <!-- Shipper Mobile -->
          <template v-else-if="auth.currentRole.value === 'shipper'">
            <router-link
              to="/shipper"
              class="mobile-link highlight-mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-bicycle menu-icon"></i>
              <span>Nhận Đơn Hỏa Tốc</span>
            </router-link>
            <router-link
              to="/profile"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-cash-coin menu-icon"></i>
              <span>Ví Thu Nhập</span>
            </router-link>
          </template>

          <!-- Admin Mobile -->
          <template v-else-if="auth.currentRole.value === 'admin'">
            <router-link
              to="/admin"
              class="mobile-link highlight-mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-shield-check menu-icon"></i>
              <span>Bảng Điều Khiển Admin</span>
            </router-link>
            <router-link
              to="/products"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-eye menu-icon"></i>
              <span>Giám Sát Sàn</span>
            </router-link>
            <router-link
              to="/profile"
              class="mobile-link"
              @click="closeDropdowns"
            >
              <i class="bi bi-gear menu-icon"></i>
              <span>Hệ Thống</span>
            </router-link>
          </template>

          <router-link
            to="/contact"
            class="mobile-link"
            @click="closeDropdowns"
          >
            <i class="bi bi-bag-check menu-icon" aria-hidden="true"></i>
            <span>Đơn Mua</span>
          </router-link>
          <router-link
            to="/profile"
            class="mobile-link"
            @click="closeDropdowns"
          >
            <i class="bi bi-person-circle menu-icon" aria-hidden="true"></i>
            <span>Hồ Sơ & Ví</span>
          </router-link>
          <router-link
            to="/seller"
            class="mobile-link"
            @click="closeDropdowns"
          >
            <i class="bi bi-shop menu-icon" aria-hidden="true"></i>
            <span>Kênh Bán Hàng</span>
          </router-link>
          <router-link
            to="/shipper"
            class="mobile-link"
            @click="closeDropdowns"
          >
            <i class="bi bi-bicycle menu-icon" aria-hidden="true"></i>
            <span>Cổng Shipper</span>
          </router-link>
          <router-link to="/admin" class="mobile-link" @click="closeDropdowns">
            <i class="bi bi-shield-check menu-icon" aria-hidden="true"></i>
            <span>Admin</span>
          </router-link>

          <!-- Mobile Role Switcher -->
          <div class="mobile-role-switcher">
            <span class="switcher-lbl">Chuyển vai trò test:</span>
            <div class="mobile-switch-btns">
              <button
                type="button"
                :class="{ active: auth.currentRole.value === 'buyer' }"
                @click="handleRoleSwitch('buyer')"
              >
                Buyer
              </button>
              <button
                type="button"
                :class="{ active: auth.currentRole.value === 'seller' }"
                @click="handleRoleSwitch('seller')"
              >
                Seller
              </button>
              <button
                type="button"
                :class="{ active: auth.currentRole.value === 'shipper' }"
                @click="handleRoleSwitch('shipper')"
              >
                Shipper
              </button>
              <button
                type="button"
                :class="{ active: auth.currentRole.value === 'admin' }"
                @click="handleRoleSwitch('admin')"
              >
                Admin
              </button>
            </div>
          </div>

          <div class="mobile-divider"></div>
          <button class="mobile-link logout-link" @click="handleLogout">
            <i class="bi bi-box-arrow-right menu-icon" aria-hidden="true"></i>
            <span>Đăng Xuất</span>
          </button>
        </template>
      </nav>
    </header>

    <!-- Nội dung chính được điều hướng qua Router -->
    <main class="main-content">
      <router-view />
    </main>

    <!-- Footer ZoneMart – Chuẩn sàn Thương Mại Điện Tử (Tự động ẩn ở trang 404 qua meta.hideFooter) -->
    <footer v-if="!$route.meta.hideFooter" class="app-footer">
      <div class="footer-main-container">
        <!-- Cột 1: Thông tin thương hiệu, Liên hệ & Trụ sở -->
        <div class="footer-col col-brand">
          <div class="footer-logo-row">
            <img :src="logoImg" alt="ZoneMart" class="footer-logo-img" />
            <div class="footer-brand-title">
              <h4>ZoneMart</h4>
              <span>Giao Hàng Hỏa Tốc 10km</span>
            </div>
          </div>
          <p class="footer-tagline">
            Sàn thương mại điện tử kết nối trực tiếp Nhà Vườn, Tiểu Thương với
            Khách Hàng lân cận trong bán kính 10km.
          </p>
          <ul class="footer-contact-list">
            <li>
              <i class="bi bi-geo-alt-fill footer-info-icon"></i>
              <span
                ><strong>Trụ sở:</strong> Tòa nhà ZoneMart, Khu Công Nghệ Cao,
                TP. Hà Nội</span
              >
            </li>
            <li>
              <i class="bi bi-telephone-fill footer-info-icon"></i>
              <span
                ><strong>Tổng đài hỗ trợ:</strong> 1900 6868 (8:00 - 21:00 hàng
                ngày)</span
              >
            </li>
            <li>
              <i class="bi bi-envelope-fill footer-info-icon"></i>
              <span><strong>Email hỗ trợ:</strong> support@zonemart.vn</span>
            </li>
            <li>
              <i class="bi bi-clock-fill footer-info-icon"></i>
              <span
                ><strong>Thời gian hoạt động:</strong> 06:00 - 22:00 (Cả Thứ 7,
                CN)</span
              >
            </li>
          </ul>
        </div>

        <!-- Cột 2: Về ZoneMart -->
        <div class="footer-col">
          <h5 class="footer-heading">VỀ ZONEMART</h5>
          <ul class="footer-links">
            <li>
              <router-link to="/contact">Giới thiệu về ZoneMart</router-link>
            </li>
            <li><a href="#rules">Quy chế hoạt động sàn TMĐT</a></li>
            <li><a href="#safety">Tiêu chuẩn nông sản VietGAP</a></li>
            <li><a href="#news">Tin tức & Mẹo tiêu dùng sạch</a></li>
            <li><a href="#careers">Tuyển dụng Shipper & Nhân sự</a></li>
          </ul>
        </div>

        <!-- Cột 3: Hỗ Trợ Khách Hàng -->
        <div class="footer-col">
          <h5 class="footer-heading">HỖ TRỢ KHÁCH HÀNG</h5>
          <ul class="footer-links">
            <li>
              <router-link to="/contact">Trung tâm hỗ trợ 24/7</router-link>
            </li>
            <li><a href="#guide">Hướng dẫn đặt mua & Chọn nhà vườn</a></li>
            <li><a href="#shipping">Chính sách giao hỏa tốc 10km</a></li>
            <li><a href="#refund">Chính sách đổi trả & Hoàn tiền</a></li>
            <li><a href="#dispute">Giải quyết tranh chấp khiếu nại</a></li>
            <li><a href="#privacy">Chính sách bảo mật thông tin</a></li>
          </ul>
        </div>

        <!-- Cột 4: Hợp Tác & Thanh Toán -->
        <div class="footer-col">
          <h5 class="footer-heading">HỢP TÁC & PHÁT TRIỂN</h5>
          <ul class="footer-links">
            <li>
              <router-link to="/register-seller"
                >Mở gian hàng Nông Dân / Shop</router-link
              >
            </li>
            <li>
              <router-link to="/register-shipper"
                >Đăng ký làm Tài xế Shipper</router-link
              >
            </li>
            <li>
              <router-link to="/admin">Cổng quản trị viên Admin</router-link>
            </li>
          </ul>

          <h5 class="footer-heading footer-subheading-mt">
            PHƯƠNG THỨC THANH TOÁN
          </h5>
          <div class="payment-badges">
            <span class="pay-tag">COD (Tiền mặt)</span>
            <span class="pay-tag">Ví ZoneMart</span>
            <span class="pay-tag">VNPAY QR</span>
            <span class="pay-tag">MoMo</span>
          </div>

          <h5 class="footer-heading footer-subheading-mt">
            KẾT NỐI VỚI CHÚNG TÔI
          </h5>
          <div class="social-links">
            <a
              href="https://facebook.com"
              target="_blank"
              rel="noopener noreferrer"
              class="social-icon social-facebook"
              title="Facebook ZoneMart"
            >
              <i class="bi bi-facebook"></i>
            </a>
            <a
              href="https://zalo.me"
              target="_blank"
              rel="noopener noreferrer"
              class="social-icon social-zalo"
              title="Zalo ZoneMart"
            >
              <span class="zalo-speech-bubble">Zalo</span>
            </a>
            <a
              href="https://youtube.com"
              target="_blank"
              rel="noopener noreferrer"
              class="social-icon social-youtube"
              title="YouTube ZoneMart"
            >
              <i class="bi bi-youtube"></i>
            </a>
            <a
              href="https://tiktok.com"
              target="_blank"
              rel="noopener noreferrer"
              class="social-icon social-tiktok"
              title="TikTok ZoneMart"
            >
              <i class="bi bi-tiktok"></i>
            </a>
          </div>
        </div>
      </div>

      <!-- Dòng Bản quyền & Ghi chú thành viên dưới cùng -->
      <div class="footer-bottom-bar">
        <div class="footer-bottom-container">
          <div class="member-credits"></div>
          <p class="copyright">
            © 2026 ZoneMart E-Commerce Platform. Nền tảng thương mại điện tử
            giao hàng siêu tốc 10km.
          </p>
        </div>
      </div>
    </footer>
  </div>
</template>

<style>
/* Reset & Base Styles */
* {
  box-sizing: border-box;
}
body {
  margin: 0;
  padding: 0;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background-color: #faf7f2;
  color: #2b1b14;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.app-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  width: 100%;
  max-width: 100%;
  overflow-x: hidden;
}

.main-content {
  flex-grow: 1;
  width: 100%;
  max-width: 100%;
  overflow-x: hidden;
}

/* ==========================================================
   HEADER THEO THIẾT KẾ LOCAL MARKET & PHÂN QUYỀN GUEST
   ========================================================== */

.navbar {
  background: #ffffff;
  border-bottom: 1.5px solid #ebdcd3;
  position: sticky;
  top: 0;
  z-index: 1000;
  box-shadow: 0 2px 10px rgba(43, 27, 20, 0.03);
}

.nav-container {
  width: 100%;
  max-width: 100%;
  margin: 0;
  padding: 0 clamp(16px, 3.5vw, 48px);
  height: 70px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
}

/* 1. BRAND LOGO */
.brand-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  flex-shrink: 0;
}
.brand-logo-img {
  height: 48px;
  width: auto;
  object-fit: contain;
  border-radius: 6px;
  transition: transform 0.2s ease;
}
.brand-logo:hover .brand-logo-img {
  transform: scale(1.05);
}
.brand-text-block {
  display: flex;
  flex-direction: column;
}
.brand-name {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 19px;
  font-weight: 850;
  letter-spacing: -0.3px;
  color: #1e3a5f;
  line-height: 1.1;
}
.brand-name .highlight {
  color: #d85a2a;
}
.brand-tagline {
  font-size: 11px;
  font-weight: 600;
  color: #7b6960;
  letter-spacing: 0.2px;
  margin-top: 1px;
}

/* 2. SEARCH BAR CHÍNH GIỮA */
.header-search-wrap {
  flex: 1;
  max-width: 440px;
  display: flex;
  align-items: center;
  background: #fbf5ef;
  border: 1px solid #ebd9ce;
  border-radius: 50px;
  padding: 6px 16px;
  transition: all 0.25s ease;
}
.header-search-wrap:focus-within {
  background: #ffffff;
  border-color: #d85a2a;
  box-shadow: 0 0 0 3px rgba(216, 90, 42, 0.12);
}
.header-search-wrap .search-icon {
  font-size: 15px;
  margin-right: 10px;
  color: #968379;
  flex-shrink: 0;
  transition: color 0.2s ease;
}
.header-search-wrap:focus-within .search-icon {
  color: #d85a2a;
}
.clear-search-btn {
  background: none;
  border: none;
  color: #a49187;
  cursor: pointer;
  padding: 0;
  display: flex;
  align-items: center;
  font-size: 15px;
  margin-left: 6px;
  transition: color 0.15s ease;
}
.clear-search-btn:hover {
  color: #ea580c;
}
.header-search-wrap input {
  flex: 1;
  border: none;
  outline: none;
  background: transparent;
  font-size: 13.5px;
  color: #2b1b14;
}
.header-search-wrap input::placeholder {
  color: #968379;
}

/* 3. RIGHT ACTIONS */
.nav-right-actions {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-shrink: 0;
}

.desktop-links {
  display: flex;
  align-items: center;
  gap: 6px;
}
.quick-link {
  text-decoration: none;
  font-size: 14px;
  font-weight: 600;
  color: #55443d;
  padding: 8px 14px;
  border-radius: 8px;
  transition: all 0.2s;
}
.quick-link:hover {
  color: #d85a2a;
  background: #fdf5f0;
}
.quick-link.active {
  color: #d85a2a;
  background: #fbf0e8;
}

/* 2 NÚT ĐĂNG KÝ / ĐĂNG NHẬP CHO KHÁCH (GUEST) */
.guest-auth-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-auth-outline {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
  background: transparent;
  color: #d85a2a;
  border: 1.5px solid #d85a2a;
  padding: 8px 18px;
  border-radius: 50px;
  font-size: 13.5px;
  font-weight: 600;
  transition: all 0.2s ease;
  white-space: nowrap;
}
.btn-auth-outline:hover {
  background: #fdf2eb;
  border-color: #bf4a1f;
  color: #bf4a1f;
}

.btn-auth-solid {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
  background: #d85a2a;
  color: #ffffff !important;
  border: 1.5px solid #d85a2a;
  padding: 8px 20px;
  border-radius: 50px;
  font-size: 13.5px;
  font-weight: 700;
  box-shadow: 0 4px 12px rgba(216, 90, 42, 0.22);
  transition: all 0.2s ease;
  white-space: nowrap;
}
.btn-auth-solid:hover {
  background: #bf4a1f;
  border-color: #bf4a1f;
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(216, 90, 42, 0.32);
}

/* KHI ĐÃ ĐĂNG NHẬP */
.logged-in-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  color: #2b1b14;
  background: #ffffff;
  border: 1px solid #ebdcd3;
  cursor: pointer;
  position: relative;
  text-decoration: none;
  transition: all 0.2s;
}
.icon-btn:hover {
  border-color: #d85a2a;
  color: #d85a2a;
  background: #fdf5f0;
}

.cart-badge {
  position: absolute;
  top: -3px;
  right: -3px;
  background: #d85a2a;
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid #ffffff;
}

.menu-mini-icon {
  font-size: 14px;
  margin-right: 4px;
  vertical-align: -1px;
}

/* ROLE HIGHLIGHT PILLS ON DESKTOP NAV */
.highlight-pill {
  border: 1px solid transparent;
  font-weight: 700;
}
.seller-pill {
  background: #fff7ed;
  color: #c2410c !important;
  border-color: #ffedd5;
}
.seller-pill:hover,
.seller-pill.active {
  background: #ffedd5;
  color: #9a3412 !important;
  border-color: #fdba74;
}
.shipper-pill {
  background: #f0fdf4;
  color: #15803d !important;
  border-color: #dcfce7;
}
.shipper-pill:hover,
.shipper-pill.active {
  background: #dcfce7;
  color: #166534 !important;
  border-color: #86efac;
}
.admin-pill {
  background: #faf5ff;
  color: #7e22ce !important;
  border-color: #f3e8ff;
}
.admin-pill:hover,
.admin-pill.active {
  background: #f3e8ff;
  color: #6b21a8 !important;
  border-color: #d8b4fe;
}

/* SHIPPER READY STATUS TAG */
.shipper-ready-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #f0fdf4;
  border: 1px solid #86efac;
  color: #15803d;
  font-size: 12px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 50px;
  text-decoration: none;
  transition: all 0.2s;
}
.shipper-ready-tag:hover {
  background: #dcfce7;
  transform: translateY(-1px);
}
.status-dot-pulse {
  width: 8px;
  height: 8px;
  background: #22c55e;
  border-radius: 50%;
  box-shadow: 0 0 0 0 rgba(34, 197, 94, 0.7);
  animation: pulseGreen 1.8s infinite;
}
@keyframes pulseGreen {
  0% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(34, 197, 94, 0.7);
  }
  70% {
    transform: scale(1);
    box-shadow: 0 0 0 6px rgba(34, 197, 94, 0);
  }
  100% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(34, 197, 94, 0);
  }
}

/* USER PROFILE CHIP (BUTTON HEADER) */
.user-menu-wrapper {
  position: relative;
}
.user-profile-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #ffffff;
  border: 1.5px solid #ebdcd3;
  padding: 4px 10px 4px 4px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.2s ease;
}
.user-profile-chip:hover {
  border-color: #d85a2a;
  background: #fdfaf6;
  box-shadow: 0 4px 12px rgba(216, 90, 42, 0.08);
}
.user-chip-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  object-fit: cover;
  border: 1.5px solid #ebdcd3;
}
.user-chip-avatar-placeholder {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: #d85a2a;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 13px;
}
.user-chip-info {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  text-align: left;
}
.user-chip-name {
  font-size: 12.5px;
  font-weight: 700;
  color: #2b1b14;
  max-width: 110px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.2;
}
.user-chip-role-badge {
  font-size: 10px;
  font-weight: 700;
  border-radius: 4px;
  padding: 0 4px;
  margin-top: 1px;
}
.badge-role-buyer .user-chip-role-badge {
  background: #eff6ff;
  color: #2563eb;
}
.badge-role-seller .user-chip-role-badge {
  background: #fff7ed;
  color: #ea580c;
}
.badge-role-shipper .user-chip-role-badge {
  background: #f0fdf4;
  color: #16a34a;
}
.badge-role-admin .user-chip-role-badge {
  background: #faf5ff;
  color: #9333ea;
}
.chip-arrow {
  font-size: 11px;
  color: #9ca3af;
  margin-left: 2px;
  transition: transform 0.2s;
}

/* USER DROPDOWN CARD */
.user-dropdown {
  position: absolute;
  top: 50px;
  right: 0;
  width: 290px;
  background: #ffffff;
  border-radius: 16px;
  border: 1px solid #ebdcd3;
  box-shadow: 0 16px 40px rgba(43, 27, 20, 0.14);
  padding: 0;
  overflow: hidden;
  z-index: 1001;
  animation: fadeIn 0.18s ease-out;
}
@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(-6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.dropdown-header-card {
  padding: 14px 16px;
  border-bottom: 1px solid #f3e7df;
  background: #faf7f2;
}
.dropdown-header-card.badge-role-buyer {
  background: linear-gradient(135deg, #f8faff 0%, #edf4fe 100%);
}
.dropdown-header-card.badge-role-seller {
  background: linear-gradient(135deg, #fffaf5 0%, #fff0e5 100%);
}
.dropdown-header-card.badge-role-shipper {
  background: linear-gradient(135deg, #f7fdf9 0%, #edfcf1 100%);
}
.dropdown-header-card.badge-role-admin {
  background: linear-gradient(135deg, #faf7fe 0%, #f3ebfc 100%);
}

.dropdown-header-top {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 8px;
}
.dropdown-avatar {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #ffffff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}
.dropdown-avatar-placeholder {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background: #d85a2a;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  font-weight: 700;
  border: 2px solid #ffffff;
}
.dropdown-user-details {
  display: flex;
  flex-direction: column;
}
.dropdown-user-name {
  font-size: 14px;
  color: #2b1b14;
  line-height: 1.2;
}
.dropdown-badge-pill {
  font-size: 10.5px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: 4px;
  display: inline-block;
  margin-top: 3px;
  width: fit-content;
}
.badge-role-buyer .dropdown-badge-pill {
  background: #dbeafe;
  color: #1d4ed8;
}
.badge-role-seller .dropdown-badge-pill {
  background: #ffedd5;
  color: #c2410c;
}
.badge-role-shipper .dropdown-badge-pill {
  background: #dcfce7;
  color: #15803d;
}
.badge-role-admin .dropdown-badge-pill {
  background: #f3e8ff;
  color: #7e22ce;
}

.dropdown-header-sub {
  font-size: 11.5px;
  color: #6b7280;
  display: flex;
  flex-direction: column;
  gap: 3px;
}
.dropdown-email-text {
  word-break: break-all;
}
.dropdown-store-text,
.dropdown-vehicle-text {
  color: #4b5563;
  font-weight: 600;
}
.dropdown-wallet-row {
  margin-top: 4px;
  padding-top: 4px;
  border-top: 1px dashed rgba(0, 0, 0, 0.08);
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.dropdown-wallet-row .wallet-lbl {
  font-size: 11px;
  color: #71717a;
}
.dropdown-wallet-row .wallet-val {
  font-size: 13px;
  color: #d85a2a;
}

.dropdown-body-links {
  padding: 6px 0;
}
.dropdown-item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  text-align: left;
  background: none;
  border: none;
  padding: 9px 16px;
  font-size: 13px;
  font-weight: 500;
  color: #4a3830;
  text-decoration: none;
  cursor: pointer;
  transition: all 0.15s;
}
.dropdown-item .menu-icon {
  font-size: 15px;
  color: #9c887e;
  width: 18px;
  text-align: center;
  transition: color 0.15s;
}
.dropdown-item:hover {
  background: #fdf5f0;
  color: #d85a2a;
}
.dropdown-item:hover .menu-icon {
  color: #d85a2a;
}
.dropdown-item.highlight-item {
  font-weight: 600;
  color: #d85a2a;
}
.dropdown-item.highlight-item .menu-icon {
  color: #d85a2a;
}

/* DEV ROLE SWITCHER INSIDE DROPDOWN */
.dropdown-role-switcher {
  background: #fbf7f4;
  border-top: 1px solid #f0e2d8;
  border-bottom: 1px solid #f0e2d8;
  padding: 8px 12px;
}
.switcher-title {
  font-size: 10.5px;
  font-weight: 700;
  color: #7c685e;
  margin-bottom: 6px;
  display: flex;
  align-items: center;
  gap: 4px;
}
.switcher-buttons {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 4px;
}
.switch-btn {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 6px;
  font-size: 10px;
  font-weight: 700;
  padding: 4px 2px;
  cursor: pointer;
  color: #57463f;
  transition: all 0.15s;
}
.switch-btn:hover {
  background: #f3eae3;
  border-color: #d85a2a;
}
.switch-btn.active {
  background: #d85a2a;
  color: #ffffff;
  border-color: #d85a2a;
}

.dropdown-divider {
  height: 1px;
  background: #f3e7df;
  margin: 4px 0;
}
.logout-btn {
  color: #dc2626 !important;
  font-weight: 600;
}
.logout-btn .menu-icon {
  color: #dc2626 !important;
}

/* MOBILE ROLE STYLES */
.mobile-user-card {
  background: #faf5ef;
  border: 1px solid #ebdcd3;
  border-radius: 10px;
  padding: 10px 12px;
  margin-bottom: 6px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.mobile-user-name {
  font-size: 13.5px;
  font-weight: 700;
  color: #2b1b14;
}
.mobile-role-tag {
  font-size: 11px;
  font-weight: 700;
  background: #d85a2a;
  color: #ffffff;
  padding: 2px 8px;
  border-radius: 50px;
}
.highlight-mobile-link {
  color: #d85a2a !important;
  font-weight: 700;
  background: #fff8f3;
}
.mobile-role-switcher {
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px dashed #e8d8ce;
}
.mobile-role-switcher .switcher-lbl {
  font-size: 11px;
  font-weight: 700;
  color: #8c776c;
  display: block;
  margin-bottom: 6px;
}
.mobile-switch-btns {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 4px;
}
.mobile-switch-btns button {
  padding: 6px 2px;
  font-size: 11px;
  font-weight: 700;
  border: 1px solid #ebdcd3;
  border-radius: 6px;
  background: #ffffff;
  cursor: pointer;
}
.mobile-switch-btns button.active {
  background: #d85a2a;
  color: #ffffff;
  border-color: #d85a2a;
}

/* Mobile Toggle */
.mobile-toggle {
  display: none;
  background: none;
  border: 1px solid #ebdcd3;
  border-radius: 8px;
  font-size: 20px;
  padding: 6px 10px;
  cursor: pointer;
  color: #2b1b14;
}

/* Mobile Menu */
.mobile-menu {
  display: none;
  position: absolute;
  top: 70px;
  left: 0;
  width: 100%;
  background: #ffffff;
  border-bottom: 1.5px solid #ebdcd3;
  padding: 16px 20px;
  box-shadow: 0 10px 20px rgba(43, 27, 20, 0.08);
  flex-direction: column;
  gap: 8px;
}
.mobile-link {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  font-size: 14px;
  font-weight: 600;
  color: #4a3830;
  padding: 10px 12px;
  border-radius: 8px;
  transition: all 0.2s;
  background: none;
  border: none;
  text-align: left;
  cursor: pointer;
}
.mobile-link .menu-icon {
  font-size: 16px;
  color: #d85a2a;
  flex-shrink: 0;
}
.mobile-link:hover {
  background: #fdf5f0;
  color: #d85a2a;
}
.mobile-divider {
  height: 1px;
  background: #ebdcd3;
  margin: 8px 0;
}
.mobile-auth-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin-top: 6px;
}
.mobile-btn {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  text-align: center;
}
.logout-link {
  color: #dc2626;
}

/* Responsive */
@media (max-width: 860px) {
  .header-search-wrap {
    display: none;
  }
  .desktop-links {
    display: none;
  }
  .guest-auth-actions {
    display: none;
  }
  .logged-in-actions {
    display: none;
  }
  .mobile-toggle {
    display: block;
  }
  .mobile-menu {
    display: flex;
  }
}

/* ==========================================================
   FOOTER CHUẨN SÀN THƯƠNG MẠI ĐIỆN TỬ
   ========================================================== */
.app-footer {
  background: #19100a;
  color: #a49187;
  padding: 50px 0 0 0;
  margin-top: 50px;
  border-top: 3px solid #3c2419;
  font-size: 13.5px;
}

.footer-main-container {
  width: 100%;
  max-width: 100%;
  margin: 0;
  padding: 0 clamp(16px, 3.5vw, 48px) 40px;
  display: grid;
  grid-template-columns: 1.4fr 1fr 1fr 1.1fr;
  gap: clamp(20px, 3vw, 40px);
}

.footer-col {
  display: flex;
  flex-direction: column;
}

.footer-logo-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}
.footer-logo-img {
  height: 44px;
  width: auto;
  border-radius: 6px;
  background: #ffffff;
  padding: 2px;
}
.footer-brand-title h4 {
  margin: 0;
  color: #fff9f5;
  font-size: 18px;
  font-weight: 800;
  letter-spacing: -0.2px;
}
.footer-brand-title span {
  font-size: 11px;
  color: #d85a2a;
  font-weight: 600;
}

.footer-tagline {
  font-size: 13px;
  line-height: 1.6;
  color: #c7b5ab;
  margin: 0 0 16px 0;
}

.footer-contact-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
  font-size: 12.5px;
  color: #b5a297;
}
.footer-contact-list li {
  display: flex;
  align-items: flex-start;
  gap: 9px;
  line-height: 1.5;
}
.footer-info-icon {
  color: #ea580c;
  font-size: 13.5px;
  flex-shrink: 0;
  margin-top: 2px;
}
.footer-contact-list strong {
  color: #f1dfd5;
}

.footer-heading {
  font-size: 13.5px;
  font-weight: 700;
  letter-spacing: 0.8px;
  color: #fff9f5;
  margin: 0 0 16px 0;
  text-transform: uppercase;
}
.footer-subheading-mt {
  margin-top: 20px;
}

.footer-links {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.footer-links a {
  color: #a49187;
  text-decoration: none;
  transition: all 0.2s;
}
.footer-links a:hover {
  color: #d85a2a;
  padding-left: 4px;
}

.payment-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.pay-tag {
  background: #2b1c14;
  color: #e5d4cb;
  border: 1px solid #4a3427;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 600;
}

.social-links {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 10px;
}
.social-icon {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: #2b1c14;
  border: 1px solid #4a3427;
  text-decoration: none;
  font-size: 18px;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}
.social-icon:hover {
  transform: translateY(-3px);
}

/* Facebook: Xanh dương đặc trưng */
.social-icon.social-facebook {
  color: #1877f2;
}
.social-icon.social-facebook:hover {
  background: #1877f2;
  border-color: #1877f2;
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(24, 119, 242, 0.45);
}

/* Zalo: Bong bóng chat xanh Zalo đặc trưng */
.zalo-speech-bubble {
  position: relative;
  background: #0068ff;
  color: #ffffff;
  font-family:
    -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  font-weight: 800;
  font-size: 10px;
  line-height: 1;
  padding: 3.5px 5.5px;
  border-radius: 5px;
  letter-spacing: -0.2px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}
.zalo-speech-bubble::after {
  content: '';
  position: absolute;
  bottom: -3px;
  left: 5px;
  width: 0;
  height: 0;
  border-left: 3px solid transparent;
  border-right: 3px solid transparent;
  border-top: 4px solid #0068ff;
  transition: border-top-color 0.2s ease;
}
.social-icon.social-zalo:hover {
  background: #0068ff;
  border-color: #0068ff;
  box-shadow: 0 4px 14px rgba(0, 104, 255, 0.45);
}
.social-icon.social-zalo:hover .zalo-speech-bubble {
  background: #ffffff;
  color: #0068ff;
}
.social-icon.social-zalo:hover .zalo-speech-bubble::after {
  border-top-color: #ffffff;
}

/* YouTube: Đỏ tươi đặc trưng */
.social-icon.social-youtube {
  color: #ff0000;
}
.social-icon.social-youtube:hover {
  background: #ff0000;
  border-color: #ff0000;
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(255, 0, 0, 0.45);
}

/* TikTok: Đen viền neon đặc trưng */
.social-icon.social-tiktok {
  color: #ffffff;
}
.social-icon.social-tiktok:hover {
  background: #000000;
  border-color: #fe2c55;
  color: #fe2c55;
  box-shadow: 0 4px 14px rgba(254, 44, 85, 0.45);
}

/* Bottom Bar */
.footer-bottom-bar {
  background: #110b07;
  border-top: 1px solid #29180f;
  padding: 16px 0;
}
.footer-bottom-container {
  width: 100%;
  max-width: 100%;
  padding: 0 clamp(16px, 3.5vw, 48px);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}
.member-credits {
  font-size: 12px;
  color: #b5a297;
  line-height: 1.6;
}
.member-credits strong {
  color: #f28b5b;
}
.copyright {
  margin: 0;
  font-size: 12px;
  color: #7b6960;
}

/* Footer Responsive */
@media (max-width: 992px) {
  .footer-main-container {
    grid-template-columns: repeat(2, 1fr);
    gap: 30px;
  }
}
@media (max-width: 600px) {
  .footer-main-container {
    grid-template-columns: 1fr;
    gap: 28px;
  }
  .footer-bottom-container {
    flex-direction: column;
    text-align: center;
  }
}
</style>
