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
    <header v-if="!$route.meta.hideHeader" class="navbar">
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

    <!-- Footer ZoneMart Chuẩn SEO Quốc Tế & Schema.org -->
    <footer v-if="!$route.meta.hideFooter" class="app-footer" role="contentinfo" itemscope itemtype="https://schema.org/Organization">
      <div class="footer-container">
        <!-- Main 4-Column Grid -->
        <div class="footer-main-grid">
          <!-- Cột 1: Thông tin thương hiệu & Doanh nghiệp (NAP - Name, Address, Phone) -->
          <div class="footer-col brand-col">
            <div class="footer-logo-row">
              <img src="/logo.png" alt="ZoneMart Sàn Thương Mại Điện Tử Giao Hàng Hỏa Tốc" class="footer-logo-img" itemprop="logo" />
              <div class="footer-logo-text">
                <span class="f-logo-title" itemprop="name">Zone<span class="f-highlight">Mart</span></span>
                <span class="f-logo-slogan">Giao hàng hỏa tốc trong 10km</span>
              </div>
            </div>

            <p class="footer-company-desc" itemprop="description">
              Hệ sinh thái thương mại điện tử đa điểm (Multi-Vendor) tiên phong kết nối người tiêu dùng với các siêu thị mini, tiệm thực phẩm sạch và quán ăn quanh khu vực bán kính 10km. Cam kết giao hỏa tốc 20 - 30 phút.
            </p>

            <div class="company-contact-list">
              <div class="contact-line" itemprop="address" itemscope itemtype="https://schema.org/PostalAddress">
                <span class="c-icon">📍</span>
                <span itemprop="streetAddress">245 Cầu Giấy, P. Dịch Vọng</span>, 
                <span itemprop="addressLocality">Hà Nội</span>
              </div>
              <div class="contact-line">
                <span class="c-icon">📞</span>
                <span>Hotline: <strong itemprop="telephone">1900 6868</strong> (8h00 - 22h00)</span>
              </div>
              <div class="contact-line">
                <span class="c-icon">✉️</span>
                <span>Email: <a href="mailto:hotro@zonemart.vn" itemprop="email">hotro@zonemart.vn</a></span>
              </div>
              <div class="contact-line">
                <span class="c-icon">🏢</span>
                <span>Mã số thuế: <strong>0109887766</strong> do Sở KH&ĐT cấp</span>
              </div>
            </div>
          </div>

          <!-- Cột 2: Danh mục mua sắm nổi bật (SEO Internal Links) -->
          <div class="footer-col">
            <h3 class="footer-col-title">Danh Mục Nổi Bật</h3>
            <ul class="footer-nav-list">
              <li>
                <router-link to="/products" title="Thực phẩm tươi sống giao nhanh">
                  🥩 Thực phẩm tươi & Thịt nhập khẩu
                </router-link>
              </li>
              <li>
                <router-link to="/products" title="Rau củ quả sạch VietGAP">
                  🥗 Rau củ quả sạch VietGAP
                </router-link>
              </li>
              <li>
                <router-link to="/products" title="Món ăn nóng hổi giao hỏa tốc">
                  🍱 Món ngon ăn liền & Cơm trưa
                </router-link>
              </li>
              <li>
                <router-link to="/products" title="Đồ uống & Trái cây đặc sản">
                  🧃 Đồ uống & Trái cây tươi Đà Lạt
                </router-link>
              </li>
              <li>
                <router-link to="/products" title="Nhu yếu phẩm gia đình">
                  🧴 Nhu yếu phẩm & Bách hóa
                </router-link>
              </li>
              <li>
                <router-link to="/map" title="Bản đồ quán ngon trong bán kính 10km">
                  🗺️ Tìm quán ngon theo bán kính 10km
                </router-link>
              </li>
            </ul>
          </div>

          <!-- Cột 3: Chính sách & Hỗ trợ khách hàng (E-E-A-T Trust Signals) -->
          <div class="footer-col">
            <h3 class="footer-col-title">Chính Sách & Hỗ Trợ</h3>
            <ul class="footer-nav-list">
              <li>
                <router-link to="/contact" title="Hướng dẫn đặt hàng và giao siêu tốc">
                  ⚡ Quy trình giao hàng hỏa tốc ≤ 10km
                </router-link>
              </li>
              <li>
                <router-link to="/contact" title="Chính sách bảo hành và đổi trả thực phẩm">
                  🔄 Chính sách đổi trả & Hoàn tiền 100%
                </router-link>
              </li>
              <li>
                <router-link to="/contact" title="Chính sách bảo mật dữ liệu khách hàng">
                  🔒 Bảo mật thông tin & Dữ liệu
                </router-link>
              </li>
              <li>
                <router-link to="/contact" title="Điều khoản sử dụng dịch vụ ZoneMart">
                  📜 Điều khoản sử dụng dịch vụ
                </router-link>
              </li>
              <li>
                <router-link to="/contact" title="Hướng dẫn phương thức thanh toán an toàn">
                  💳 Phương thức thanh toán COD & Ví điện tử
                </router-link>
              </li>
              <li>
                <router-link to="/contact" title="Giải quyết khiếu nại khách hàng">
                  💬 Trung tâm trợ giúp & CSKH 24/7
                </router-link>
              </li>
            </ul>
          </div>

          <!-- Cột 4: Kênh đối tác & Chứng nhận (Sitemap & Trust Badges) -->
          <div class="footer-col">
            <h3 class="footer-col-title">Hợp Tác & Chứng Nhận</h3>
            <ul class="footer-nav-list">
              <li>
                <router-link to="/register-seller" title="Đăng ký mở gian hàng đối tác ZoneMart">
                  🏪 Mở gian hàng đối tác bán hàng
                </router-link>
              </li>
              <li>
                <router-link to="/shipper" title="Đăng ký trở thành tài xế giao hàng ZoneMart">
                  🛵 Gia nhập đội ngũ tài xế Shipper
                </router-link>
              </li>
              <li>
                <router-link to="/admin" title="Cổng thông tin quản trị hệ thống">
                  🛡️ Cổng quản trị sàn (Admin Portal)
                </router-link>
              </li>
              <li>
                <router-link to="/profile" title="Hồ sơ thành viên và ví ZonePay">
                  👤 Quản lý hồ sơ & Ví tích điểm
                </router-link>
              </li>
            </ul>

            <!-- Chứng nhận Bộ Công Thương & An Toàn -->
            <div class="certifications-box">
              <div class="bocongthuong-badge" title="Website Thương mại điện tử đã thông báo với Bộ Công Thương">
                <span class="badge-check">✔</span>
                <div class="bct-text">
                  <strong>ĐÃ THÔNG BÁO</strong>
                  <small>Bộ Công Thương</small>
                </div>
              </div>
              <div class="secure-ssl-badge" title="Chứng chỉ bảo mật SSL 256-bit an toàn tuyệt đối">
                🔒 SSL Secure 256-bit
              </div>
            </div>

            <!-- Phương thức thanh toán -->
            <div class="payment-methods-row">
              <span class="pay-chip" title="Tiền mặt khi nhận hàng">COD</span>
              <span class="pay-chip" title="Cổng VNPay">VNPay</span>
              <span class="pay-chip" title="Ví MoMo">MoMo</span>
              <span class="pay-chip" title="Ví ZaloPay">ZaloPay</span>
              <span class="pay-chip" title="Thẻ Visa/Mastercard">Visa</span>
            </div>
          </div>
        </div>

        <!-- Divider Line -->
        <div class="footer-bottom-divider"></div>

        <!-- Bottom Credits & Copyright -->
        <div class="footer-bottom-row">
         
          <p class="footer-copyright">
            © 2026 ZoneMart Platform. Bản quyền thuộc về ZoneMart Project Team. All rights reserved.
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

/* ==========================================================================
   FOOTER CHUẨN SEO QUỐC TẾ
   ========================================================================== */
.app-footer {
  background: #0b1329;
  color: #94a3b8;
  padding: 64px 24px 30px 24px;
  margin-top: 60px;
  font-size: 13px;
}

.footer-container {
  max-width: 1320px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 36px;
}

.footer-main-grid {
  display: grid;
  grid-template-columns: 1.4fr 1fr 1fr 1.1fr;
  gap: 36px;
}

/* Brand col */
.footer-logo-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.footer-logo-img {
  height: 42px;
  width: auto;
  object-fit: contain;
}

.f-logo-title {
  font-size: 22px;
  font-weight: 900;
  color: #ffffff;
  letter-spacing: -0.5px;
  line-height: 1.1;
}

.f-highlight {
  color: #ea580c;
}

.f-logo-slogan {
  display: block;
  font-size: 11px;
  color: #64748b;
  font-weight: 600;
  letter-spacing: 0.2px;
}

.footer-company-desc {
  font-size: 13px;
  line-height: 1.65;
  color: #94a3b8;
  margin: 0 0 18px 0;
}

.company-contact-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.contact-line {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #cbd5e1;
}

.contact-line strong {
  color: #f8fafc;
}

.contact-line a {
  color: #38bdf8;
  text-decoration: none;
}

.c-icon {
  font-size: 14px;
  flex-shrink: 0;
}

/* Columns */
.footer-col-title {
  font-size: 15px;
  font-weight: 800;
  color: #ffffff;
  margin: 0 0 20px 0;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  position: relative;
  padding-bottom: 8px;
}

.footer-col-title::after {
  content: '';
  position: absolute;
  left: 0;
  bottom: 0;
  width: 32px;
  height: 2px;
  background: #ea580c;
  border-radius: 2px;
}

.footer-nav-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.footer-nav-list a {
  color: #94a3b8;
  text-decoration: none;
  font-size: 13px;
  transition: all 0.2s ease;
  display: inline-block;
}

.footer-nav-list a:hover {
  color: #ea580c;
  transform: translateX(3px);
}

/* Certifications & Badges */
.certifications-box {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 18px;
  align-items: center;
}

.bocongthuong-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #1e293b;
  border: 1px solid #334155;
  padding: 6px 12px;
  border-radius: 8px;
  color: #ffffff;
}

.badge-check {
  background: #16a34a;
  color: #ffffff;
  font-size: 10px;
  font-weight: 900;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.bct-text strong {
  display: block;
  font-size: 10px;
  font-weight: 800;
  color: #ef4444;
  letter-spacing: 0.5px;
  line-height: 1.1;
}

.bct-text small {
  font-size: 9px;
  color: #94a3b8;
}

.secure-ssl-badge {
  font-size: 11px;
  font-weight: 700;
  color: #34d399;
  background: #064e3b;
  padding: 6px 12px;
  border-radius: 8px;
}

/* Payment Chips */
.payment-methods-row {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  margin-top: 14px;
}

.pay-chip {
  background: #1e293b;
  color: #cbd5e1;
  font-size: 11px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
  border: 1px solid #334155;
}

/* Bottom Row */
.footer-bottom-divider {
  height: 1px;
  background: #1e293b;
  width: 100%;
}

.footer-bottom-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  font-size: 12px;
  color: #64748b;
}

.footer-credits {
  line-height: 1.6;
}

.footer-credits strong {
  color: #38bdf8;
}

.footer-copyright {
  margin: 0;
}

@media (max-width: 1024px) {
  .footer-main-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .footer-main-grid {
    grid-template-columns: 1fr;
    gap: 30px;
  }
  .app-footer {
    padding: 40px 20px 24px 20px;
  }
}
</style>
