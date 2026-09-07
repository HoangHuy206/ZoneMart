<script setup lang="ts">
import { ref } from "vue";

const isMobileMenuOpen = ref(false);

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value;
};
</script>

<template>
  <div class="app-wrapper">
    <!-- Navbar ZoneMart -->
    <header class="navbar">
      <div class="nav-container">
        <!-- Logo -->
        <router-link to="/" class="brand-logo">
          <span class="logo-icon">🚀</span>
          <span class="logo-text">Zone<span class="highlight">Mart</span></span>
          <span class="tag-10km">10km Delivery</span>
        </router-link>

        <!-- Navigation Links cho cả 3 thành viên -->
        <nav class="nav-links" :class="{ 'open': isMobileMenuOpen }">
          <!-- Huy -->
          <router-link to="/products" class="nav-item" active-class="active">
            🛍️ Sản Phẩm
          </router-link>
          <router-link to="/map" class="nav-item" active-class="active">
            🗺️ Bản Đồ
          </router-link>
          <router-link to="/contact" class="nav-item" active-class="active">
            📞 Liên Hệ
          </router-link>
          <router-link to="/profile" class="nav-item" active-class="active">
            👤 Hồ Sơ/Ví
          </router-link>

          <!-- Thắng -->
          <router-link to="/cart" class="nav-item" active-class="active">
            🛒 Giỏ Hàng
          </router-link>
          <router-link to="/buyer-orders" class="nav-item" active-class="active">
            📦 Đơn Mua
          </router-link>

          <!-- Bình -->
          <router-link to="/shipper" class="nav-item shipper-nav" active-class="active">
            🛵 Shipper
          </router-link>
          <router-link to="/admin" class="nav-item admin-nav" active-class="active">
            🛡️ Admin
          </router-link>
          <router-link to="/login" class="nav-item login-btn">
            Đăng Nhập
          </router-link>
        </nav>

        <!-- Mobile Menu Toggle -->
        <button class="mobile-toggle" @click="toggleMobileMenu">
          ☰
        </button>
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
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell, "Open Sans", "Helvetica Neue", sans-serif;
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

/* Header & Navbar */
.navbar {
  background: #ffffff;
  border-bottom: 1px solid #e2e8f0;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 2px 4px rgba(0,0,0,0.02);
}

.nav-container {
  max-width: 1250px;
  margin: 0 auto;
  padding: 0 20px;
  height: 68px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.brand-logo {
  display: flex;
  align-items: center;
  gap: 8px;
  text-decoration: none;
  color: #0f172a;
}
.logo-icon { font-size: 24px; }
.logo-text { font-size: 22px; font-weight: 800; letter-spacing: -0.5px; }
.logo-text .highlight { color: #f97316; }
.tag-10km {
  font-size: 11px;
  background: #eff6ff;
  color: #2563eb;
  padding: 2px 8px;
  border-radius: 12px;
  font-weight: 700;
  margin-left: 6px;
  border: 1px solid #dbeafe;
}

.nav-links {
  display: flex;
  align-items: center;
  gap: 4px;
}

.nav-item {
  text-decoration: none;
  font-size: 13px;
  font-weight: 600;
  color: #475569;
  padding: 8px 10px;
  border-radius: 8px;
  transition: all 0.2s;
  white-space: nowrap;
}
.nav-item:hover {
  background: #f1f5f9;
  color: #0f172a;
}
.nav-item.active {
  background: #eff6ff;
  color: #2563eb;
}

.shipper-nav.active { background: #f3e8ff; color: #7e22ce; }
.admin-nav.active { background: #fee2e2; color: #dc2626; }

.login-btn {
  background: #0f172a;
  color: #ffffff !important;
  margin-left: 6px;
  padding: 8px 14px;
}
.login-btn:hover {
  background: #334155 !important;
}

.mobile-toggle {
  display: none;
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
}

@media (max-width: 1050px) {
  .mobile-toggle { display: block; }
  .nav-links {
    display: none;
    position: absolute;
    top: 68px;
    left: 0;
    width: 100%;
    background: #fff;
    flex-direction: column;
    padding: 16px 20px;
    border-bottom: 1px solid #e2e8f0;
    box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1);
  }
  .nav-links.open { display: flex; }
  .nav-item { width: 100%; padding: 10px; }
}

/* Footer */
.app-footer {
  background: #0f172a;
  color: #94a3b8;
  padding: 30px 20px 20px 20px;
  margin-top: 50px;
}
.footer-container {
  max-width: 1250px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
}
.footer-brand h4 { margin: 0 0 6px 0; color: #f8fafc; font-size: 16px; }
.footer-brand p { margin: 0; font-size: 13px; }
.footer-meta { text-align: right; }
.member-credits { font-size: 12px; color: #cbd5e1; margin-bottom: 6px; line-height: 1.6; }
.member-credits strong { color: #38bdf8; }
.copyright { margin: 0; font-size: 12px; color: #64748b; }
</style>
