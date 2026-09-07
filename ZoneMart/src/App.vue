<script setup lang="ts">
import { ref } from "vue";

const isMobileMenuOpen = ref(false);

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value;
};
</script>

<template>
  <div class="app-wrapper">
    <!-- Header ZoneMart: Chỉ gồm Trang Chủ, Sản Phẩm, Giỏ Hàng, Đăng Nhập -->
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

        <!-- 2. Menu Điều Hướng (Chỉ 4 mục theo yêu cầu) -->
        <nav class="nav-links-wrap" :class="{ 'open': isMobileMenuOpen }">
          <!-- 1. Trang Chủ -->
          <router-link to="/" class="nav-item-link" active-class="active" exact-active-class="active">
            Trang Chủ
          </router-link>

          <!-- 2. Sản Phẩm -->
          <router-link to="/products" class="nav-item-link" active-class="active">
            Sản Phẩm
          </router-link>

          <!-- 3. Giỏ Hàng -->
          <router-link to="/cart" class="nav-item-link cart-link" active-class="active">
            <span class="cart-icon">🛒</span>
            <span>Giỏ Hàng</span>
            <span class="cart-badge">2</span>
          </router-link>

          <!-- 4. Đăng Nhập -->
          <router-link to="/login" class="nav-login-btn">
            Đăng Nhập
          </router-link>
        </nav>

        <!-- Nút Menu Mobile cho màn hình nhỏ -->
        <button class="mobile-toggle-btn" @click="toggleMobileMenu" title="Mở menu">
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
   HEADER / NAVBAR GỌN GÀNG, CHUYÊN NGHIỆP
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
  max-width: 1320px;
  margin: 0 auto;
  padding: 0 24px;
  height: 72px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

/* Logo */
.brand-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: #1a2f50;
}
.brand-logo-img {
  height: 46px;
  width: auto;
  object-fit: contain;
}
.brand-text-wrap {
  display: flex;
  flex-direction: column;
}
.logo-text {
  font-size: 22px;
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
  letter-spacing: 0.3px;
}
.tag-10km {
  font-size: 11px;
  background: #eff6ff;
  color: #2563eb;
  padding: 3px 9px;
  border-radius: 12px;
  font-weight: 800;
  margin-left: 4px;
  border: none;
}

/* Menu Điều Hướng (Chỉ 4 mục) */
.nav-links-wrap {
  display: flex;
  align-items: center;
  gap: 16px;
}

.nav-item-link {
  text-decoration: none;
  font-size: 15px;
  font-weight: 600;
  color: #334155;
  padding: 8px 16px;
  border-radius: 12px;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.nav-item-link:hover {
  color: #ea580c;
  background: #fff7ed;
}

.nav-item-link.active {
  color: #ea580c;
  font-weight: 800;
  background: #fff7ed;
}

/* Giỏ Hàng Link */
.cart-link {
  background: #f8fafc;
  padding: 8px 16px;
  border-radius: 14px;
}

.cart-link:hover {
  background: #ffedd5;
  color: #ea580c;
}

.cart-icon {
  font-size: 16px;
}

.cart-badge {
  background: #ea580c;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 20px;
  margin-left: 2px;
}

/* Nút Đăng Nhập */
.nav-login-btn {
  background: #0f172a;
  color: #ffffff !important;
  font-size: 14px;
  font-weight: 700;
  text-decoration: none;
  padding: 9px 20px;
  border-radius: 12px;
  transition: all 0.2s ease;
  margin-left: 6px;
  white-space: nowrap;
}

.nav-login-btn:hover {
  background: #ea580c;
  transform: translateY(-1px);
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.28);
}

/* Nút Menu Mobile */
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
   RESPONSIVE
   ========================================================================== */
@media (max-width: 768px) {
  .mobile-toggle-btn {
    display: block;
  }
  .nav-links-wrap {
    display: none;
    position: absolute;
    top: 72px;
    left: 0;
    width: 100%;
    background: #ffffff;
    flex-direction: column;
    padding: 16px 20px;
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.08);
    gap: 8px;
    align-items: stretch;
  }
  .nav-links-wrap.open {
    display: flex;
  }
  .nav-item-link {
    width: 100%;
    padding: 12px 14px;
  }
  .nav-login-btn {
    text-align: center;
    margin-left: 0;
    margin-top: 6px;
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
