<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const searchQuery = ref("");
const cartCount = ref(0);

// Search handler
const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({ path: "/products", query: { q: searchQuery.value } });
  } else {
    router.push("/products");
  }
};

// Featured Categories matching mockup
const featuredCategories = [
  {
    id: 1,
    title: "FRESH PRODUCE",
    sub: "Rau củ & Trái cây tươi",
    image: "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=600&q=80",
    link: "/products?cat=fresh"
  },
  {
    id: 2,
    title: "ARTISAN GOODS",
    sub: "Đặc sản & Hàng thủ công",
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=600&q=80",
    link: "/products?cat=artisan"
  },
  {
    id: 3,
    title: "LOCAL SERVICES",
    sub: "Ẩm thực & Quán ăn địa phương",
    image: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=600&q=80",
    link: "/products?cat=food"
  },
  {
    id: 4,
    title: "COLORS' & PACKAGE",
    sub: "Bách hóa & Đồ tiêu dùng",
    image: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=600&q=80",
    link: "/products?cat=grocery"
  }
];

// Cửa hàng gần bạn (trong bán kính 10km)
const nearbyShops = [
  { 
    id: "s1", 
    name: "ZoneMart Bách Hóa Cầu Giấy", 
    distance: "1.2 km", 
    rating: "4.9", 
    address: "245 Cầu Giấy, Hà Nội",
    time: "15-20 phút",
    img: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=500&q=80" 
  },
  { 
    id: "s2", 
    name: "Siêu Thị Trái Cây Tươi Xanh", 
    distance: "2.5 km", 
    rating: "4.8", 
    address: "88 Trần Thái Tông, Cầu Giấy",
    time: "20-25 phút",
    img: "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=500&q=80" 
  },
  { 
    id: "s3", 
    name: "Tiệm Bánh Mì & Cà Phê Zone", 
    distance: "2.8 km", 
    rating: "4.7", 
    address: "12 Hồ Tùng Mậu, Mai Dịch",
    time: "15-25 phút",
    img: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=500&q=80" 
  }
];

// Sản phẩm bán chạy hôm nay
const hotProducts = [
  {
    id: "p1",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (500g)",
    price: 185000,
    oldPrice: 220000,
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    img: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=400&q=80",
    isExpress: true
  },
  {
    id: "p2",
    name: "Dâu Tây Đà Lạt Tươi Ngọt Hộp 500g",
    price: 95000,
    oldPrice: 125000,
    store: "Siêu Thị Trái Cây Xanh",
    distance: "2.5 km",
    img: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400&q=80",
    isExpress: true
  },
  {
    id: "p3",
    name: "Combo Cơm Gà Nướng Mật Ong + Canh",
    price: 55000,
    oldPrice: 65000,
    store: "Tiệm Bánh Mì & Cơm Zone",
    distance: "2.8 km",
    img: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80",
    isExpress: true
  },
  {
    id: "p4",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị",
    price: 190000,
    oldPrice: 215000,
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    img: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80",
    isExpress: true
  }
];

const handleAddToCart = (name: string) => {
  cartCount.value++;
  alert(`Đã thêm "${name}" vào giỏ hàng!`);
};
</script>

<template>
  <div class="home-page-wrapper">
    <!-- SUB-NAV / TOP SEARCH BAR (EXACT MOCKUP) -->
    <header class="local-header-bar">
      <div class="header-container">
        <!-- Logo / Brand Title -->
        <router-link to="/" class="brand-link">
          <img src="/logo.png" alt="ZoneMart" class="brand-mini-logo" />
          <span class="brand-text">LOCAL MARKET</span>
        </router-link>

        <!-- Search Pill -->
        <div class="header-search-wrap">
          <span class="search-lens-icon">🔍</span>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search"
            class="header-search-input"
            @keyup.enter="handleSearch"
          />
        </div>

        <!-- User & Cart Icons with Badge -->
        <div class="header-action-icons">
          <router-link to="/profile" class="icon-btn" title="Hồ sơ người dùng">
            <span class="action-icon">👤</span>
          </router-link>

          <router-link to="/cart" class="icon-btn cart-icon-btn" title="Giỏ hàng">
            <span class="action-icon">🛒</span>
            <span class="cart-counter-badge">{{ cartCount }}</span>
          </router-link>
        </div>
      </div>
    </header>

    <!-- HERO BANNER SECTION (EXACT MOCKUP) -->
    <section class="hero-market-section">
      <div class="hero-image-backdrop">
        <!-- Overlay Layer -->
        <div class="hero-dark-overlay"></div>

        <div class="hero-content-inner">
          <!-- Left Text Area -->
          <div class="hero-typography">
            <h1 class="hero-main-heading">
              Local Market<br />Local Vendors
            </h1>
            <p class="hero-subtext">
              Prime our assonars in moonyship with local vendors commanalats.
              Kết nối người mua với hàng trăm tiểu thương & quán ngon trong bán kính 10km.
            </p>
            <button class="btn-explore-hero" @click="router.push('/products')">
              EXPLORE MARKET
            </button>
          </div>

          <!-- Right Stylized Vector Line-art Overlay -->
          <div class="hero-illustrations">
            <svg class="market-lineart-svg" viewBox="0 0 200 200" fill="none">
              <!-- Fruit / basket stylized art -->
              <circle cx="100" cy="100" r="70" stroke="#f97316" stroke-width="3" stroke-dasharray="6 6" opacity="0.4"/>
              <path d="M70 120 C 70 150, 130 150, 130 120 Z" stroke="#ea580c" stroke-width="4" fill="rgba(234, 88, 12, 0.2)"/>
              <circle cx="90" cy="105" r="16" stroke="#ea580c" stroke-width="3" fill="rgba(234, 88, 12, 0.3)"/>
              <circle cx="115" cy="105" r="16" stroke="#ea580c" stroke-width="3" fill="rgba(234, 88, 12, 0.3)"/>
              <path d="M90 89 Q 95 80 100 85" stroke="#ea580c" stroke-width="3" fill="none"/>
              <path d="M115 89 Q 120 80 125 85" stroke="#ea580c" stroke-width="3" fill="none"/>
            </svg>
          </div>
        </div>
      </div>
    </section>

    <!-- 3 REGISTRATION / ROLE ACTION CARDS (EXACT MOCKUP) -->
    <section class="role-cards-container">
      <div class="cards-grid-wrap">
        <!-- CARD 1: BUYER REGISTRATION -->
        <div class="role-action-card">
          <div class="role-icon-box">
            <span class="role-svg-icon">👤</span>
          </div>
          <h3 class="role-card-title">BUYER REGISTRATION</h3>
          <p class="role-card-desc">
            Get action navigation buyer access from engage to pay access. Đặt hàng hỏa tốc và theo dõi tài xế trực tiếp.
          </p>
          <button class="btn-role-action" @click="router.push('/register-buyer')">
            EXPLORE MARKET
          </button>
        </div>

        <!-- CARD 2: SELLER REGISTRATION -->
        <div class="role-action-card">
          <div class="role-icon-box">
            <span class="role-svg-icon">🏪</span>
          </div>
          <h3 class="role-card-title">SELLER REGISTRATION</h3>
          <p class="role-card-desc">
            Unist a towark pots and seller access from funding local market. Mở gian hàng tiếp cận khách hàng trong 10km.
          </p>
          <button class="btn-role-action" @click="router.push('/register-seller')">
            SELLER MARKET
          </button>
        </div>

        <!-- CARD 3: SHIPPER REGISTRATION -->
        <div class="role-action-card">
          <div class="role-icon-box">
            <span class="role-svg-icon">🚚</span>
          </div>
          <h3 class="role-card-title">SHIPPER REGISTRATION</h3>
          <p class="role-card-desc">
            Shipper access to evallgure access, send inspectors and shipper secealy. Gia nhập đội ngũ giao vận ZoneMart.
          </p>
          <button class="btn-role-action" @click="router.push('/shipper')">
            SHIPPER MARKET
          </button>
        </div>
      </div>
    </section>

    <!-- FEATURED CATEGORY SECTION (EXACT MOCKUP) -->
    <section class="featured-category-section">
      <div class="category-section-header">
        <h2 class="featured-title">Featured Category</h2>
        <button class="btn-view-all" @click="router.push('/products')">
          View All
        </button>
      </div>

      <!-- Categories Grid -->
      <div class="featured-cards-grid">
        <div
          v-for="cat in featuredCategories"
          :key="cat.id"
          class="category-tile-card"
          @click="router.push(cat.link)"
        >
          <div class="category-tile-banner">
            <h4 class="cat-banner-title">{{ cat.title }}</h4>
            <span class="cat-banner-sub">{{ cat.sub }}</span>
          </div>
          <div class="category-tile-img-box">
            <img :src="cat.image" :alt="cat.title" class="cat-image" />
          </div>
        </div>
      </div>
    </section>

    <!-- NEARBY SHOPS / LOCAL VENDORS (BÁN KÍNH 10KM) -->
    <section class="local-vendors-section">
      <div class="category-section-header">
        <div>
          <h2 class="featured-title">Quán Ngon & Gian Hàng Gần Bạn (10km)</h2>
          <p class="section-subtext">Các đối tác địa phương được kiểm duyệt chứng thực giấy phép kinh doanh</p>
        </div>
        <button class="btn-view-all" @click="router.push('/map')">
          📍 Xem trên bản đồ ➜
        </button>
      </div>

      <div class="shops-row-grid">
        <div v-for="shop in nearbyShops" :key="shop.id" class="vendor-shop-card">
          <div class="shop-thumb-wrap">
            <img :src="shop.img" :alt="shop.name" class="shop-thumb" />
            <span class="distance-pill">📍 {{ shop.distance }}</span>
          </div>
          <div class="shop-info-wrap">
            <div class="shop-header-line">
              <h4 class="shop-name">{{ shop.name }}</h4>
              <span class="shop-rating">⭐ {{ shop.rating }}</span>
            </div>
            <p class="shop-address">{{ shop.address }}</p>
            <div class="shop-bottom-line">
              <span class="shop-eta">⏰ {{ shop.time }}</span>
              <button class="btn-view-shop" @click="router.push('/products')">
                Xem Quán
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- HOT PRODUCTS SECTION -->
    <section class="hot-products-section">
      <div class="category-section-header">
        <div>
          <h2 class="featured-title">🔥 Món Bán Chạy Trong Ngày</h2>
          <p class="section-subtext">Giao siêu tốc &le; 3km chỉ từ 20-30 phút</p>
        </div>
        <button class="btn-view-all" @click="router.push('/products')">
          Xem tất cả ➜
        </button>
      </div>

      <div class="products-row-grid">
        <div v-for="prod in hotProducts" :key="prod.id" class="item-prod-card">
          <div class="prod-thumb-container">
            <img :src="prod.img" :alt="prod.name" class="prod-thumb" />
            <span v-if="prod.isExpress" class="express-chip">⚡ Giao &le; 3km</span>
          </div>
          <div class="prod-card-body">
            <span class="prod-store-sub">{{ prod.store }} • {{ prod.distance }}</span>
            <h4 class="prod-card-title">{{ prod.name }}</h4>
            <div class="prod-price-action">
              <div class="prod-price-wrap">
                <span class="current-price">{{ prod.price.toLocaleString('vi-VN') }} ₫</span>
                <span class="old-price">{{ prod.oldPrice.toLocaleString('vi-VN') }} ₫</span>
              </div>
              <button class="btn-add-cart" @click="handleAddToCart(prod.name)">
                + Giỏ hàng
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* ==========================================================================
   GLOBAL PAGE THEME (WARM CREAM & TERRACOTTA #ba441b)
   ========================================================================== */
.home-page-wrapper {
  background-color: #fdfaf6;
  min-height: 100vh;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  color: #2b231d;
  padding-bottom: 60px;
}

/* ==========================================================================
   TOP SUB-NAV / HEADER BAR
   ========================================================================== */
.local-header-bar {
  background-color: #fcf8f3;
  border-bottom: 1px solid #ebdcd1;
  padding: 12px 24px;
}

.header-container {
  max-width: 1140px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
}

.brand-link {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}

.brand-mini-logo {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  object-fit: cover;
}

.brand-text {
  font-size: 22px;
  font-weight: 800;
  color: #ba441b;
  letter-spacing: 0.5px;
}

.header-search-wrap {
  flex: 1;
  max-width: 520px;
  position: relative;
  display: flex;
  align-items: center;
}

.search-lens-icon {
  position: absolute;
  left: 14px;
  font-size: 14px;
  color: #9c8e84;
  pointer-events: none;
}

.header-search-input {
  width: 100%;
  padding: 9px 16px 9px 38px;
  background-color: #f6efe8;
  border: 1px solid #e2d2c5;
  border-radius: 20px;
  font-size: 14px;
  color: #3b2c23;
  outline: none;
  transition: all 0.2s;
}

.header-search-input:focus {
  background-color: #ffffff;
  border-color: #ba441b;
  box-shadow: 0 0 0 3px rgba(186, 68, 27, 0.1);
}

.header-action-icons {
  display: flex;
  align-items: center;
  gap: 16px;
}

.icon-btn {
  text-decoration: none;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.action-icon {
  font-size: 20px;
  color: #4a3b32;
  transition: transform 0.15s;
}

.icon-btn:hover .action-icon {
  transform: scale(1.1);
}

.cart-icon-btn {
  display: flex;
  align-items: center;
  gap: 4px;
}

.cart-counter-badge {
  background-color: #ea580c;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* ==========================================================================
   HERO BANNER SECTION
   ========================================================================== */
.hero-market-section {
  max-width: 1140px;
  margin: 16px auto 28px auto;
  padding: 0 16px;
}

.hero-image-backdrop {
  position: relative;
  border-radius: 24px;
  overflow: hidden;
  background-image: url("https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=1600&q=80");
  background-size: cover;
  background-position: center 30%;
  min-height: 420px;
  display: flex;
  align-items: center;
  box-shadow: 0 12px 30px rgba(78, 42, 23, 0.12);
}

.hero-dark-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: linear-gradient(
    90deg,
    rgba(20, 15, 12, 0.82) 0%,
    rgba(40, 25, 18, 0.65) 50%,
    rgba(30, 20, 15, 0.4) 100%
  );
}

.hero-content-inner {
  position: relative;
  z-index: 2;
  width: 100%;
  padding: 48px 44px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.hero-typography {
  max-width: 520px;
}

.hero-main-heading {
  font-size: 44px;
  line-height: 1.15;
  font-weight: 800;
  color: #ffffff;
  margin: 0 0 16px 0;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.4);
}

.hero-subtext {
  font-size: 15px;
  line-height: 1.55;
  color: rgba(255, 255, 255, 0.88);
  margin: 0 0 24px 0;
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.4);
}

.btn-explore-hero {
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  padding: 12px 28px;
  border-radius: 24px;
  font-size: 14px;
  font-weight: 800;
  letter-spacing: 0.8px;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(186, 68, 27, 0.4);
  transition: all 0.2s;
}

.btn-explore-hero:hover {
  background-color: #a33813;
  transform: translateY(-2px);
  box-shadow: 0 6px 18px rgba(186, 68, 27, 0.5);
}

.hero-illustrations {
  width: 180px;
  height: 180px;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0.9;
}

.market-lineart-svg {
  width: 100%;
  height: 100%;
}

/* ==========================================================================
   ROLE ACTION CARDS (BUYER, SELLER, SHIPPER)
   ========================================================================== */
.role-cards-container {
  max-width: 1140px;
  margin: -40px auto 44px auto;
  padding: 0 16px;
  position: relative;
  z-index: 4;
}

.cards-grid-wrap {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.role-action-card {
  background-color: #fffdfa;
  border: 1.5px solid #cb774c;
  border-radius: 18px;
  padding: 24px 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  box-shadow: 0 8px 24px rgba(92, 45, 23, 0.08);
  transition: transform 0.2s, box-shadow 0.2s;
}

.role-action-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 28px rgba(92, 45, 23, 0.14);
}

.role-icon-box {
  width: 56px;
  height: 56px;
  background-color: #ea580c;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16px;
  box-shadow: 0 4px 10px rgba(234, 88, 12, 0.3);
}

.role-svg-icon {
  font-size: 26px;
  color: #ffffff;
}

.role-card-title {
  margin: 0 0 10px 0;
  font-size: 15px;
  font-weight: 800;
  color: #2b231d;
  letter-spacing: 0.5px;
}

.role-card-desc {
  font-size: 13px;
  line-height: 1.45;
  color: #6a5e55;
  margin: 0 0 18px 0;
  min-height: 48px;
}

.btn-role-action {
  width: 100%;
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  padding: 10px 18px;
  border-radius: 20px;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.6px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-role-action:hover {
  background-color: #a33813;
}

/* ==========================================================================
   FEATURED CATEGORY SECTION
   ========================================================================== */
.featured-category-section {
  max-width: 1140px;
  margin: 0 auto 48px auto;
  padding: 0 16px;
}

.category-section-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 20px;
}

.featured-title {
  margin: 0;
  font-size: 22px;
  font-weight: 800;
  color: #2b231d;
}

.section-subtext {
  margin: 4px 0 0 0;
  font-size: 13px;
  color: #7b6f67;
}

.btn-view-all {
  background: transparent;
  border: none;
  color: #ba441b;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  padding: 4px 8px;
  transition: opacity 0.2s;
}

.btn-view-all:hover {
  opacity: 0.8;
  text-decoration: underline;
}

.featured-cards-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

.category-tile-card {
  background-color: #ea580c;
  border-radius: 16px;
  overflow: hidden;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  box-shadow: 0 6px 18px rgba(234, 88, 12, 0.15);
  transition: transform 0.2s, box-shadow 0.2s;
}

.category-tile-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 24px rgba(234, 88, 12, 0.25);
}

.category-tile-banner {
  padding: 16px 14px 12px 14px;
  color: #ffffff;
}

.cat-banner-title {
  margin: 0 0 4px 0;
  font-size: 15px;
  font-weight: 800;
  letter-spacing: 0.5px;
}

.cat-banner-sub {
  font-size: 12px;
  opacity: 0.9;
}

.category-tile-img-box {
  width: 100%;
  height: 120px;
  overflow: hidden;
}

.cat-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}

.category-tile-card:hover .cat-image {
  transform: scale(1.06);
}

/* ==========================================================================
   LOCAL VENDORS / NEARBY SHOPS
   ========================================================================== */
.local-vendors-section {
  max-width: 1140px;
  margin: 0 auto 48px auto;
  padding: 0 16px;
}

.shops-row-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.vendor-shop-card {
  background-color: #ffffff;
  border: 1.5px solid #ebdcd1;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.04);
  transition: transform 0.2s, box-shadow 0.2s;
}

.vendor-shop-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.08);
}

.shop-thumb-wrap {
  position: relative;
  height: 150px;
}

.shop-thumb {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.distance-pill {
  position: absolute;
  top: 10px;
  left: 10px;
  background-color: rgba(20, 15, 12, 0.82);
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 20px;
}

.shop-info-wrap {
  padding: 16px;
}

.shop-header-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.shop-name {
  margin: 0;
  font-size: 15px;
  font-weight: 700;
  color: #2b231d;
}

.shop-rating {
  font-size: 13px;
  font-weight: 700;
  color: #ea580c;
}

.shop-address {
  margin: 0 0 14px 0;
  font-size: 12px;
  color: #7b6f67;
}

.shop-bottom-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.shop-eta {
  font-size: 12px;
  color: #524740;
}

.btn-view-shop {
  background-color: #fbf4ee;
  border: 1px solid #ea580c;
  color: #ea580c;
  padding: 6px 14px;
  border-radius: 14px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-view-shop:hover {
  background-color: #ea580c;
  color: #ffffff;
}

/* ==========================================================================
   HOT PRODUCTS SECTION
   ========================================================================== */
.hot-products-section {
  max-width: 1140px;
  margin: 0 auto;
  padding: 0 16px;
}

.products-row-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

.item-prod-card {
  background-color: #ffffff;
  border: 1.5px solid #ebdcd1;
  border-radius: 16px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.04);
  transition: transform 0.2s;
}

.item-prod-card:hover {
  transform: translateY(-3px);
}

.prod-thumb-container {
  position: relative;
  height: 160px;
}

.prod-thumb {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.express-chip {
  position: absolute;
  top: 8px;
  left: 8px;
  background-color: #dc2626;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 3px 8px;
  border-radius: 6px;
}

.prod-card-body {
  padding: 14px;
  display: flex;
  flex-direction: column;
  flex: 1;
}

.prod-store-sub {
  font-size: 11px;
  color: #8c7d73;
  margin-bottom: 4px;
}

.prod-card-title {
  margin: 0 0 12px 0;
  font-size: 14px;
  color: #2b231d;
  line-height: 1.35;
  height: 38px;
  overflow: hidden;
}

.prod-price-action {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-top: auto;
}

.prod-price-wrap {
  display: flex;
  flex-direction: column;
}

.current-price {
  font-size: 16px;
  font-weight: 800;
  color: #ba441b;
}

.old-price {
  font-size: 12px;
  color: #a89a90;
  text-decoration: line-through;
}

.btn-add-cart {
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  padding: 6px 12px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-add-cart:hover {
  background-color: #a33813;
}

/* ==========================================================================
   RESPONSIVE STYLES
   ========================================================================== */
@media (max-width: 992px) {
  .hero-main-heading {
    font-size: 36px;
  }
  .cards-grid-wrap {
    grid-template-columns: 1fr;
  }
  .role-cards-container {
    margin-top: 20px;
  }
  .featured-cards-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .shops-row-grid {
    grid-template-columns: 1fr;
  }
  .products-row-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .hero-content-inner {
    padding: 32px 20px;
  }
  .hero-main-heading {
    font-size: 28px;
  }
  .hero-illustrations {
    display: none;
  }
  .featured-cards-grid {
    grid-template-columns: 1fr;
  }
  .products-row-grid {
    grid-template-columns: 1fr;
  }
  .header-search-wrap {
    display: none;
  }
}
</style>
