<script setup lang="ts">
import { ref } from "vue";

const isMobileMenuOpen = ref(false);

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value;
};
</script>

<template>
  <div class="app-wrapper">
    <!-- Header ZoneMart Hiện Đại, Chuyên Nghiệp -->
    <header class="navbar">
      <div class="nav-container">
        <!-- 1. Brand Logo -->
        <router-link to="/" class="brand-logo">
          <img src="/logo.png" alt="ZoneMart Logo" class="brand-logo-img" />
          <div class="brand-text-wrap">
            <span class="logo-text">Zone<span class="highlight">Mart</span></span>
            <span class="logo-slogan">Giao hàng hỏa tốc</span>
          </div>
          <span class="tag-10km">10km</span>
        </router-link>

        <!-- 2. Menu Điều Hướng Chính (Gọn gàng, tinh tế) -->
        <nav class="nav-main-menu" :class="{ 'open': isMobileMenuOpen }">
          <router-link to="/" class="nav-link-item" active-class="active" exact-active-class="active">
            Trang Chủ
          </router-link>
          <router-link to="/products" class="nav-link-item" active-class="active">
            Sản Phẩm
          </router-link>
          <router-link to="/map" class="nav-link-item" active-class="active">
            Bản Đồ 10km
          </router-link>
          <router-link to="/contact" class="nav-link-item" active-class="active">
            Liên Hệ
          </router-link>

          <!-- Mobile Only Menu items -->
          <div class="mobile-extra-group" v-if="isMobileMenuOpen">
            <div class="mobile-divider"></div>
            <router-link to="/buyer-orders" class="nav-link-item">📦 Đơn Mua</router-link>
            <router-link to="/cart" class="nav-link-item">🛒 Giỏ Hàng</router-link>
            <router-link to="/profile" class="nav-link-item">👤 Hồ Sơ & Ví</router-link>
            <router-link to="/shipper" class="nav-link-item">🛵 Cổng Shipper</router-link>
            <router-link to="/admin" class="nav-link-item">🛡️ Quản Trị Admin</router-link>
            <router-link to="/login" class="nav-link-item mobile-login-link">Đăng Nhập</router-link>
          </div>
        </nav>

        <!-- 3. Khu Vực Tác Vụ Phía Phải (Đối Tác, Giỏ Hàng, Đơn Mua, Đăng Nhập) -->
        <div class="header-right-actions">
          <!-- Phân hệ Đối Tác -->
          <div class="portals-cluster">
            <router-link to="/shipper" class="portal-pill shipper-pill" title="Cổng dành cho tài xế giao hàng">
              🛵 Shipper
            </router-link>
            <router-link to="/admin" class="portal-pill admin-pill" title="Trang quản trị hệ thống">
              🛡️ Admin
            </router-link>
          </div>

          <div class="action-divider-line"></div>

          <!-- Đơn Mua -->
          <router-link to="/buyer-orders" class="action-btn-item" title="Đơn mua của tôi">
            <span class="action-icon">📦</span>
            <span class="action-text">Đơn Mua</span>
          </router-link>

          <!-- Giỏ Hàng Nổi Bật -->
          <router-link to="/cart" class="cart-pill-btn" title="Xem giỏ hàng">
            <span class="cart-icon">🛒</span>
            <span class="cart-label">Giỏ Hàng</span>
            <span class="cart-count-badge">2</span>
          </router-link>

          <!-- Tài Khoản / Profile -->
          <router-link to="/profile" class="user-profile-icon" title="Hồ sơ tài khoản & Ví">
            <span>👤</span>
          </router-link>

          <!-- Nút Đăng Nhập -->
          <router-link to="/login" class="btn-primary-login">
            Đăng Nhập
          </router-link>

          <!-- Nút menu mobile -->
          <button class="mobile-toggle-btn" @click="toggleMobileMenu" title="Mở menu">
            ☰
          </button>
        </div>
      </div>
    </header>

    <!-- Nội dung chính được điều hướng qua Router -->
    <main class="main-content">
      <router-view />
    </main>

    <!-- Footer ZoneMart -->
    <footer class="app-footer">
      <div class="footer-container">
        <div class="footer-brand">
          <h4>ZoneMart Multi-Vendor & On-Demand Delivery Platform</h4>
          <p>Hệ thống thương mại điện tử giao hàng siêu tốc trong bán kính 10km.</p>
        </div>
        <div class="footer-meta">
          <div class="member-credits">
            <span>🚀 <strong>Huy</strong>: Profile, Map, Sản Phẩm, Liên Hệ, 404</span> | 
            <span><strong>Thắng</strong>: Home, Giỏ Hàng, Thanh Toán, Đơn Mua</span> | 
            <span><strong>Bình</strong>: Auth, Đăng Ký Shop, Shipper, Admin</span>
          </div>
          <p class="copyright">© 2026 ZoneMart Project Team. All rights reserved.</p>
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
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell, "Open Sans", "Helvetica Neue", sans-serif;
  background-color: #f8fafc;
  color: #1e293b;
}

.app-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.main-content {
  flex-grow: 1;
}

/* ==========================================================================
   NAVBAR & HEADER HIỆN ĐẠI
   ========================================================================== */
.navbar {
  background: #ffffff;
  border-bottom: 1px solid #f1f5f9;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.04);
}

.nav-container {
  max-width: 1360px;
  margin: 0 auto;
  padding: 0 24px;
  height: 72px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
}

/* 1. Brand Logo */
.brand-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: #1a2f50;
  flex-shrink: 0;
}
.brand-logo-img {
  height: 44px;
  width: auto;
  object-fit: contain;
}
.brand-text-wrap {
  display: flex;
  flex-direction: column;
}
.logo-text {
  font-size: 21px;
  font-weight: 900;
  letter-spacing: -0.5px;
  line-height: 1.1;
  color: #1a2f50;
}
.logo-text .highlight {
  color: #ea580c;
}
.logo-slogan {
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
  letter-spacing: 0.2px;
}
.tag-10km {
  font-size: 11px;
  background: #eff6ff;
  color: #2563eb;
  padding: 3px 8px;
  border-radius: 12px;
  font-weight: 800;
  border: none;
}

/* 2. Main Navigation Menu */
.nav-main-menu {
  display: flex;
  align-items: center;
  gap: 6px;
}

.nav-link-item {
  text-decoration: none;
  font-size: 14px;
  font-weight: 600;
  color: #475569;
  padding: 8px 16px;
  border-radius: 12px;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.nav-link-item:hover {
  background: #fff7ed;
  color: #ea580c;
}

.nav-link-item.active {
  background: #fff7ed;
  color: #ea580c;
  font-weight: 800;
}

/* 3. Header Right Actions */
.header-right-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}

.portals-cluster {
  display: flex;
  align-items: center;
  gap: 6px;
}

.portal-pill {
  font-size: 12px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 10px;
  text-decoration: none;
  transition: all 0.2s;
  white-space: nowrap;
}

.shipper-pill {
  background: #f3e8ff;
  color: #7e22ce;
}
.shipper-pill:hover {
  background: #e9d5ff;
}

.admin-pill {
  background: #fee2e2;
  color: #dc2626;
}
.admin-pill:hover {
  background: #fecaca;
}

.action-divider-line {
  width: 1px;
  height: 24px;
  background: #e2e8f0;
  margin: 0 4px;
}

.action-btn-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  padding: 8px 12px;
  border-radius: 10px;
  text-decoration: none;
  transition: all 0.2s;
  white-space: nowrap;
}

.action-btn-item:hover {
  background: #f1f5f9;
  color: #0f172a;
}

/* Giỏ hàng nổi bật */
.cart-pill-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #fff7ed;
  color: #ea580c;
  padding: 8px 14px;
  border-radius: 12px;
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
  transition: all 0.2s;
  white-space: nowrap;
}

.cart-pill-btn:hover {
  background: #ffedd5;
  transform: translateY(-1px);
}

.cart-count-badge {
  background: #ea580c;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 20px;
  margin-left: 2px;
}

/* Profile Icon */
.user-profile-icon {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  background: #f1f5f9;
  display: flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
  font-size: 16px;
  color: #334155;
  transition: all 0.2s;
}

.user-profile-icon:hover {
  background: #e2e8f0;
}

/* Nút Đăng Nhập */
.btn-primary-login {
  background: #0f172a;
  color: #ffffff !important;
  padding: 8px 18px;
  border-radius: 12px;
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
  transition: all 0.2s;
  white-space: nowrap;
}

.btn-primary-login:hover {
  background: #ea580c;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.25);
}

.mobile-toggle-btn {
  display: none;
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #1e293b;
  padding: 4px;
}

/* ==========================================================================
   RESPONSIVE NAVBAR
   ========================================================================== */
@media (max-width: 1150px) {
  .portals-cluster {
    display: none;
  }
}

@media (max-width: 900px) {
  .mobile-toggle-btn {
    display: block;
  }
  .action-btn-item {
    display: none;
  }
  .nav-main-menu {
    display: none;
    position: absolute;
    top: 72px;
    left: 0;
    width: 100%;
    background: #ffffff;
    flex-direction: column;
    padding: 16px 20px;
    box-shadow: 0 10px 25px rgba(0,0,0,0.1);
    align-items: stretch;
  }
  .nav-main-menu.open {
    display: flex;
  }
  .nav-link-item {
    width: 100%;
    padding: 12px 14px;
  }
  .mobile-extra-group {
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-top: 8px;
  }
  .mobile-divider {
    height: 1px;
    background: #f1f5f9;
    margin: 8px 0;
  }
}

/* Footer */
.app-footer {
  background: #0f172a;
  color: #94a3b8;
  padding: 36px 24px 24px 24px;
  margin-top: 50px;
}
.footer-container {
  max-width: 1320px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
}
.footer-brand h4 { margin: 0 0 6px 0; color: #f8fafc; font-size: 16px; font-weight: 800; }
.footer-brand p { margin: 0; font-size: 13px; }
.footer-meta { text-align: right; }
.member-credits { font-size: 12px; color: #cbd5e1; margin-bottom: 6px; line-height: 1.6; }
.member-credits strong { color: #38bdf8; }
.copyright { margin: 0; font-size: 12px; color: #64748b; }
</style>
