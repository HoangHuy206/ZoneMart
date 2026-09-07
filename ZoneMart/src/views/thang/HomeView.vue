<script setup lang="ts">
/**
 * ================================================================
 * TRANG CHỦ (HOMEPAGE) - ZONEMART
 * Sàn Thương Mại Điện Tử & Giao Hàng Hỏa Tốc Bán Kính 10km
 * ================================================================
 */
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const searchQuery = ref("");

// Tìm kiếm nhanh
const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({ path: "/products", query: { q: searchQuery.value } });
  } else {
    router.push("/products");
  }
};

// Danh mục ngành hàng chính
const categories = [
  { id: "fresh", name: "Thực Phẩm Tươi", icon: "🥩", count: "140+ quán", color: "#fef2f2", textColor: "#dc2626" },
  { id: "fruit", name: "Trái Cây Tươi", icon: "🍎", count: "95+ quán", color: "#f0fdf4", textColor: "#16a34a" },
  { id: "food", name: "Cơm & Món Nóng", icon: "🍱", count: "230+ quán", color: "#fff7ed", textColor: "#ea580c" },
  { id: "drink", name: "Trà Sữa & Cà Phê", icon: "🧋", count: "110+ quán", color: "#fdf4ff", textColor: "#c026d3" },
  { id: "grocery", name: "Bách Hóa & Gia Dụng", icon: "🛒", count: "80+ quán", color: "#eff6ff", textColor: "#2563eb" },
  { id: "express", name: "Siêu Tốc ≤ 3km", icon: "⚡", count: "Giao 20 phút", color: "#fefce8", textColor: "#ca8a04" }
];

// Banner quảng cáo / Khuyến mãi
const banners = [
  {
    title: "Giao Hàng Siêu Tốc Trong Bán Kính 10km",
    subtitle: "Hàng ngàn món ngon & thực phẩm tươi sống từ cửa hàng quanh bạn. Nhận hàng chỉ trong 20 - 30 phút!",
    tag: "⚡ CAM KẾT ĐÚNG GIỜ",
    btnText: "Khám Phá Quán Gần Bạn",
    link: "/products",
    bg: "linear-gradient(135deg, #ea580c 0%, #c2410c 100%)",
    image: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=600&q=80"
  }
];

// Cửa hàng đối tác gần bạn (trong bán kính 10km)
const nearbyShops = [
  { 
    id: "s1", 
    name: "ZoneMart Bách Hóa Cầu Giấy", 
    distance: "1.2 km", 
    rating: "4.9", 
    address: "245 Cầu Giấy, P. Dịch Vọng, Hà Nội",
    time: "15-20 phút",
    badge: "Freeship ≤ 3km",
    img: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=500&q=80" 
  },
  { 
    id: "s2", 
    name: "Siêu Thị Trái Cây Tươi Xanh", 
    distance: "2.5 km", 
    rating: "4.8", 
    address: "88 Trần Thái Tông, Cầu Giấy",
    time: "20-25 phút",
    badge: "Giảm 15%",
    img: "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=500&q=80" 
  },
  { 
    id: "s3", 
    name: "Tiệm Bánh Mì & Cà Phê Zone", 
    distance: "2.8 km", 
    rating: "4.7", 
    address: "12 Hồ Tùng Mậu, Mai Dịch",
    time: "15-25 phút",
    badge: "Quán ngon",
    img: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=500&q=80" 
  }
];

// Sản phẩm bán chạy hôm nay
const hotProducts = [
  {
    id: "p1",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (Khay 500g)",
    price: 185000,
    oldPrice: 220000,
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    img: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=400&q=80",
    isExpress: true,
    sold: 142
  },
  {
    id: "p2",
    name: "Dâu Tây Đà Lạt Tươi Ngọt Hộp 500g",
    price: 95000,
    oldPrice: 125000,
    store: "Siêu Thị Trái Cây Xanh",
    distance: "2.5 km",
    img: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400&q=80",
    isExpress: true,
    sold: 89
  },
  {
    id: "p3",
    name: "Combo Cơm Gà Nướng Mật Ong + Canh Chua",
    price: 55000,
    oldPrice: 65000,
    store: "Tiệm Bánh Mì & Cơm Zone",
    distance: "2.8 km",
    img: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80",
    isExpress: true,
    sold: 215
  },
  {
    id: "p4",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Ngon",
    price: 190000,
    oldPrice: 215000,
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    img: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80",
    isExpress: true,
    sold: 76
  }
];

// Thêm vào giỏ hàng
const handleAddToCart = (prod: any) => {
  alert(`Đã thêm "${prod.name}" vào giỏ hàng!`);
};
</script>

<template>
  <div class="home-container">
    <!-- 1. HERO BANNER CHÍNH CỦA SÀN THƯƠNG MẠI -->
    <section class="hero-section">
      <div class="hero-card-banner" :style="{ background: banners[0].bg }">
        <div class="hero-text-content">
          <div class="hero-badge">
            <span>{{ banners[0].tag }}</span>
          </div>
          <h1 class="hero-main-title">{{ banners[0].title }}</h1>
          <p class="hero-desc">{{ banners[0].subtitle }}</p>

          <!-- Thanh tìm kiếm tích hợp -->
          <div class="hero-search-bar">
            <span class="search-lens">🔍</span>
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Tìm món ăn, thực phẩm, quán ăn trong bán kính 10km..."
              @keyup.enter="handleSearch"
            />
            <button class="btn-search-submit" @click="handleSearch">
              Tìm Kiếm
            </button>
          </div>

          <!-- Quick tags -->
          <div class="hero-quick-keywords">
            <span>Gợi ý phổ biến:</span>
            <a @click="router.push('/products?q=com')">Cơm tấm</a>
            <a @click="router.push('/products?q=thit')">Thịt bò tươi</a>
            <a @click="router.push('/products?q=tra')">Trà sữa</a>
            <a @click="router.push('/map')" class="map-link">🗺️ Tìm trên bản đồ 10km</a>
          </div>
        </div>

        <div class="hero-media-box">
          <img :src="banners[0].image" alt="ZoneMart Delivery" class="hero-promo-img" />
          <div class="floating-promo-badge">
            <span class="badge-icon">🛵</span>
            <div class="badge-text">
              <strong>Giao Hỏa Tốc</strong>
              <small>Dưới 30 phút trong 10km</small>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 2. DANH MỤC NGÀNH HÀNG -->
    <section class="home-section">
      <div class="section-title-bar">
        <div>
          <h2 class="section-heading">Danh Mục Ngành Hàng</h2>
          <p class="section-sub">Lựa chọn nhu yếu phẩm và món ngon theo sở thích của bạn</p>
        </div>
        <button class="btn-text-link" @click="router.push('/products')">
          Xem tất cả danh mục ➜
        </button>
      </div>

      <div class="category-cards-grid">
        <div
          v-for="cat in categories"
          :key="cat.id"
          class="cat-tile"
          @click="router.push('/products')"
        >
          <div class="cat-icon-wrap" :style="{ backgroundColor: cat.color }">
            <span class="cat-emoji">{{ cat.icon }}</span>
          </div>
          <h4 class="cat-name">{{ cat.name }}</h4>
          <span class="cat-count">{{ cat.count }}</span>
        </div>
      </div>
    </section>

    <!-- 3. CÁC PHÂN HỆ THÀNH VIÊN (BUYER / SELLER / SHIPPER) -->
    <section class="home-section">
      <div class="role-promo-grid">
        <!-- Người mua -->
        <div class="role-box buyer-box" @click="router.push('/products')">
          <div class="role-header">
            <span class="role-badge">Dành cho Khách Hàng</span>
            <span class="role-emoji">🛍️</span>
          </div>
          <h3>Đặt Món & Mua Sắm Nhanh</h3>
          <p>Hàng ngàn sản phẩm quanh khu vực của bạn, kiểm tra cự ly GPS và nhận hàng tận cửa.</p>
          <button class="btn-role-nav">Khám Phá Ngay ➜</button>
        </div>

        <!-- Người bán -->
        <div class="role-box seller-box" @click="router.push('/register-seller')">
          <div class="role-header">
            <span class="role-badge">Dành cho Chủ Quán</span>
            <span class="role-emoji">🏪</span>
          </div>
          <h3>Mở Gian Hàng Kinh Doanh</h3>
          <p>Tiếp cận hàng nghìn khách hàng tiềm năng trong bán kính 10km, quản lý đơn hàng thông minh.</p>
          <button class="btn-role-nav">Đăng Ký Bán Hàng ➜</button>
        </div>

        <!-- Tài xế -->
        <div class="role-box shipper-box" @click="router.push('/shipper')">
          <div class="role-header">
            <span class="role-badge">Dành cho Tài Xế</span>
            <span class="role-emoji">🛵</span>
          </div>
          <h3>Gia Nhập Đội Xe Giao Hàng</h3>
          <p>Nhận các đơn giao siêu tốc ≤ 3km và đơn thường trong khu vực với thù lao hấp dẫn.</p>
          <button class="btn-role-nav">Cổng Tài Xế ➜</button>
        </div>
      </div>
    </section>

    <!-- 4. QUÁN NGON & GIAN HÀNG GẦN BẠN (TRONG 10KM) -->
    <section class="home-section">
      <div class="section-title-bar">
        <div>
          <h2 class="section-heading">📍 Quán Ngon & Gian Hàng Gần Bạn</h2>
          <p class="section-sub">Được định vị theo GPS trong bán kính phục vụ 10km của ZoneMart</p>
        </div>
        <button class="btn-text-link" @click="router.push('/map')">
          Mở bản đồ thời gian thực ➜
        </button>
      </div>

      <div class="shops-cards-grid">
        <div
          v-for="shop in nearbyShops"
          :key="shop.id"
          class="shop-item-card"
          @click="router.push('/products')"
        >
          <div class="shop-cover-box">
            <img :src="shop.img" :alt="shop.name" class="shop-cover" />
            <span class="shop-distance-tag">📍 {{ shop.distance }}</span>
            <span class="shop-promo-badge">{{ shop.badge }}</span>
          </div>
          <div class="shop-card-info">
            <div class="shop-name-rating">
              <h4 class="shop-card-name">{{ shop.name }}</h4>
              <span class="shop-star">⭐ {{ shop.rating }}</span>
            </div>
            <p class="shop-card-addr">{{ shop.address }}</p>
            <div class="shop-card-footer">
              <span class="shop-eta-time">⏰ {{ shop.time }}</span>
              <span class="btn-enter-shop">Vào Gian Hàng</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 5. SẢN PHẨM BÁN CHẠY HÔM NAY -->
    <section class="home-section">
      <div class="section-title-bar">
        <div>
          <h2 class="section-heading">🔥 Sản Phẩm & Món Ăn Bán Chạy</h2>
          <p class="section-sub">Được đặt nhiều nhất hôm nay, cam kết nguyên liệu tươi ngon</p>
        </div>
        <button class="btn-text-link" @click="router.push('/products')">
          Xem toàn bộ sản phẩm ➜
        </button>
      </div>

      <div class="products-cards-grid">
        <div
          v-for="prod in hotProducts"
          :key="prod.id"
          class="prod-item-card"
        >
          <div class="prod-thumb-box" @click="router.push(`/products/${prod.id}`)">
            <img :src="prod.img" :alt="prod.name" class="prod-thumb" />
            <span v-if="prod.isExpress" class="prod-express-badge">⚡ Giao ≤ 3km</span>
          </div>
          <div class="prod-content-box">
            <span class="prod-store-label">🏪 {{ prod.store }} • {{ prod.distance }}</span>
            <h4 class="prod-title" @click="router.push(`/products/${prod.id}`)">{{ prod.name }}</h4>
            <div class="prod-meta-sold">Đã bán: {{ prod.sold }}+ suất</div>
            <div class="prod-price-action-row">
              <div class="prod-pricing">
                <span class="price-val">{{ prod.price.toLocaleString('vi-VN') }} ₫</span>
                <span class="price-discount">{{ prod.oldPrice.toLocaleString('vi-VN') }} ₫</span>
              </div>
              <button class="btn-add-cart-fast" @click="handleAddToCart(prod)">
                + Thêm
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 6. CAM KẾT DỊCH VỤ ZONEMART -->
    <section class="commitments-section">
      <div class="commitment-item">
        <span class="commit-icon">⚡</span>
        <div>
          <h4>Giao Hàng Dưới 30 Phút</h4>
          <p>Tối ưu tuyến đường thông minh trong bán kính 10km</p>
        </div>
      </div>

      <div class="commitment-item">
        <span class="commit-icon">🛡️</span>
        <div>
          <h4>Kiểm Duyệt An Toàn</h4>
          <p>100% đối tác có giấy phép kinh doanh và chứng nhận VSATTP</p>
        </div>
      </div>

      <div class="commitment-item">
        <span class="commit-icon">💳</span>
        <div>
          <h4>Thanh Toán Linh Hoạt</h4>
          <p>Hỗ trợ COD, Ví ZonePay nội bộ và Quét mã VietQR tiện lợi</p>
        </div>
      </div>

      <div class="commitment-item">
        <span class="commit-icon">📞</span>
        <div>
          <h4>Hỗ Trợ 24/7</h4>
          <p>Tổng đài chăm sóc khách hàng và giải quyết khiếu nại nhanh chóng</p>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* ==========================================================================
   GLOBAL LAYOUT
   ========================================================================== */
.home-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px 20px 60px 20px;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  color: #1e293b;
}

/* ==========================================================================
   1. HERO BANNER
   ========================================================================== */
.hero-section {
  margin-bottom: 36px;
}

.hero-card-banner {
  border-radius: 20px;
  padding: 40px 44px;
  color: #ffffff;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 36px;
  box-shadow: 0 14px 30px rgba(234, 88, 12, 0.22);
}

.hero-text-content {
  flex: 1;
  max-width: 620px;
}

.hero-badge {
  display: inline-block;
  background-color: rgba(255, 255, 255, 0.2);
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}

.hero-main-title {
  font-size: 34px;
  font-weight: 800;
  line-height: 1.25;
  margin: 0 0 12px 0;
  text-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.hero-desc {
  font-size: 15px;
  line-height: 1.55;
  color: rgba(255, 255, 255, 0.92);
  margin: 0 0 24px 0;
}

/* Thanh tìm kiếm */
.hero-search-bar {
  display: flex;
  align-items: center;
  background-color: #ffffff;
  border-radius: 30px;
  padding: 6px 8px 6px 18px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  margin-bottom: 14px;
}

.search-lens {
  font-size: 16px;
  margin-right: 10px;
  color: #64748b;
}

.hero-search-bar input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 14px;
  color: #1e293b;
}

.btn-search-submit {
  background-color: #ea580c;
  color: #ffffff;
  border: none;
  padding: 10px 22px;
  border-radius: 24px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-search-submit:hover {
  background-color: #c2410c;
}

.hero-quick-keywords {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.88);
}

.hero-quick-keywords a {
  color: #ffffff;
  text-decoration: underline;
  cursor: pointer;
  font-weight: 500;
}

.hero-quick-keywords .map-link {
  background-color: rgba(255, 255, 255, 0.25);
  padding: 2px 8px;
  border-radius: 12px;
  text-decoration: none;
  font-weight: 700;
}

.hero-media-box {
  position: relative;
  width: 320px;
  height: 240px;
  flex-shrink: 0;
}

.hero-promo-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 16px;
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.2);
}

.floating-promo-badge {
  position: absolute;
  bottom: -12px;
  left: -16px;
  background-color: #ffffff;
  color: #1e293b;
  padding: 10px 16px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  gap: 10px;
  box-shadow: 0 6px 18px rgba(0, 0, 0, 0.15);
}

.badge-icon {
  font-size: 24px;
}

.badge-text strong {
  display: block;
  font-size: 13px;
  color: #ea580c;
}

.badge-text small {
  font-size: 11px;
  color: #64748b;
}

/* ==========================================================================
   2. SECTION TITLE BAR
   ========================================================================== */
.home-section {
  margin-bottom: 44px;
}

.section-title-bar {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 20px;
}

.section-heading {
  font-size: 22px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 4px 0;
}

.section-sub {
  font-size: 13px;
  color: #64748b;
  margin: 0;
}

.btn-text-link {
  background: transparent;
  border: none;
  color: #ea580c;
  font-weight: 700;
  font-size: 14px;
  cursor: pointer;
  padding: 4px 8px;
  transition: opacity 0.2s;
}

.btn-text-link:hover {
  opacity: 0.8;
  text-decoration: underline;
}

/* ==========================================================================
   3. CATEGORY GRID
   ========================================================================== */
.category-cards-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 14px;
}

.cat-tile {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 18px 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s;
}

.cat-tile:hover {
  transform: translateY(-4px);
  border-color: #ea580c;
  box-shadow: 0 8px 20px rgba(234, 88, 12, 0.12);
}

.cat-icon-wrap {
  width: 52px;
  height: 52px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 10px;
}

.cat-emoji {
  font-size: 26px;
}

.cat-name {
  font-size: 13px;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 4px 0;
}

.cat-count {
  font-size: 11px;
  color: #64748b;
}

/* ==========================================================================
   4. ROLE PROMO GRID
   ========================================================================== */
.role-promo-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.role-box {
  padding: 24px;
  border-radius: 18px;
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s;
  display: flex;
  flex-direction: column;
}

.role-box:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.08);
}

.buyer-box {
  background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
  border: 1px solid #bfdbfe;
}

.seller-box {
  background: linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%);
  border: 1px solid #fed7aa;
}

.shipper-box {
  background: linear-gradient(135deg, #fdf4ff 0%, #fae8ff 100%);
  border: 1px solid #f5d0fe;
}

.role-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.role-badge {
  font-size: 12px;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: 12px;
  background-color: rgba(255, 255, 255, 0.7);
  color: #334155;
}

.role-emoji {
  font-size: 24px;
}

.role-box h3 {
  margin: 0 0 8px 0;
  font-size: 17px;
  font-weight: 800;
  color: #0f172a;
}

.role-box p {
  font-size: 13px;
  line-height: 1.5;
  color: #475569;
  margin: 0 0 16px 0;
  flex: 1;
}

.btn-role-nav {
  background-color: #ffffff;
  border: none;
  padding: 8px 16px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
  cursor: pointer;
  align-self: flex-start;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.06);
}

/* ==========================================================================
   5. SHOPS GRID (QUÁN NGON GẦN BẠN)
   ========================================================================== */
.shops-cards-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.shop-item-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  overflow: hidden;
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s;
}

.shop-item-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.08);
}

.shop-cover-box {
  position: relative;
  height: 150px;
}

.shop-cover {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.shop-distance-tag {
  position: absolute;
  top: 10px;
  left: 10px;
  background-color: rgba(15, 23, 42, 0.85);
  color: #ffffff;
  padding: 3px 10px;
  border-radius: 14px;
  font-size: 11px;
  font-weight: 700;
}

.shop-promo-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  background-color: #ea580c;
  color: #ffffff;
  padding: 3px 8px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 700;
}

.shop-card-info {
  padding: 16px;
}

.shop-name-rating {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.shop-card-name {
  margin: 0;
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
}

.shop-star {
  font-size: 13px;
  font-weight: 700;
  color: #eab308;
}

.shop-card-addr {
  font-size: 12px;
  color: #64748b;
  margin: 0 0 14px 0;
}

.shop-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-top: 1px solid #f1f5f9;
  padding-top: 10px;
}

.shop-eta-time {
  font-size: 12px;
  color: #16a34a;
  font-weight: 600;
}

.btn-enter-shop {
  font-size: 12px;
  font-weight: 700;
  color: #ea580c;
}

/* ==========================================================================
   6. PRODUCTS GRID
   ========================================================================== */
.products-cards-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 18px;
}

.prod-item-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  transition: transform 0.2s, box-shadow 0.2s;
}

.prod-item-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.08);
}

.prod-thumb-box {
  position: relative;
  height: 160px;
  cursor: pointer;
}

.prod-thumb {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.prod-express-badge {
  position: absolute;
  top: 8px;
  left: 8px;
  background-color: #dc2626;
  color: #ffffff;
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
}

.prod-content-box {
  padding: 14px;
  display: flex;
  flex-direction: column;
  flex: 1;
}

.prod-store-label {
  font-size: 11px;
  color: #64748b;
  margin-bottom: 4px;
}

.prod-title {
  margin: 0 0 6px 0;
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
  height: 38px;
  overflow: hidden;
  cursor: pointer;
}

.prod-meta-sold {
  font-size: 11px;
  color: #94a3b8;
  margin-bottom: 12px;
}

.prod-price-action-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-top: auto;
}

.prod-pricing {
  display: flex;
  flex-direction: column;
}

.price-val {
  font-size: 16px;
  font-weight: 800;
  color: #ea580c;
}

.price-discount {
  font-size: 12px;
  color: #94a3b8;
  text-decoration: line-through;
}

.btn-add-cart-fast {
  background-color: #ea580c;
  color: #ffffff;
  border: none;
  padding: 6px 14px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-add-cart-fast:hover {
  background-color: #c2410c;
}

/* ==========================================================================
   7. COMMITMENTS SECTION
   ========================================================================== */
.commitments-section {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 18px;
  padding: 24px;
  margin-top: 20px;
}

.commitment-item {
  display: flex;
  align-items: center;
  gap: 14px;
}

.commit-icon {
  font-size: 28px;
  width: 50px;
  height: 50px;
  background-color: #fff7ed;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.commitment-item h4 {
  margin: 0 0 4px 0;
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
}

.commitment-item p {
  margin: 0;
  font-size: 12px;
  color: #64748b;
  line-height: 1.4;
}

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1024px) {
  .hero-card-banner {
    flex-direction: column;
    padding: 30px;
  }
  .hero-media-box {
    width: 100%;
    height: 200px;
  }
  .category-cards-grid {
    grid-template-columns: repeat(3, 1fr);
  }
  .role-promo-grid {
    grid-template-columns: 1fr;
  }
  .shops-cards-grid {
    grid-template-columns: 1fr;
  }
  .products-cards-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .commitments-section {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .hero-main-title {
    font-size: 24px;
  }
  .category-cards-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .products-cards-grid {
    grid-template-columns: 1fr;
  }
  .commitments-section {
    grid-template-columns: 1fr;
  }
}
</style>

