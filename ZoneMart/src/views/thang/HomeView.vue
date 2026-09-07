<script setup lang="ts">
/**
 * ================================================================
 * TRANG CHỦ (HOME VIEW) - Phụ trách: Thắng
 * Hệ thống sàn TMĐT Giao hàng hỏa tốc ZoneMart trong bán kính 10km
 * Thiết kế giao diện tràn viền hiện đại, không dùng khung viền cứng
 * ================================================================
 */
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

// State tìm kiếm & thông báo
const searchQuery = ref("");
const toastMessage = ref("");
const showToast = ref(false);

const showNotification = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2500);
};

// Tìm kiếm
const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({ path: "/products", query: { q: searchQuery.value.trim() } });
  } else {
    router.push("/products");
  }
};

const searchWithTag = (tag: string) => {
  router.push({ path: "/products", query: { q: tag } });
};

// Danh mục ngành hàng (Icons + Màu nền pastel nhẹ, không viền)
const categories = [
  { id: "food", name: "Thực Phẩm Tươi", count: 64, icon: "🥩", bg: "#fef2f2", color: "#dc2626" },
  { id: "veggie", name: "Rau Củ Quả", count: 48, icon: "🥗", bg: "#f0fdf4", color: "#16a34a" },
  { id: "fastfood", name: "Món Ăn Nóng", count: 85, icon: "🍱", bg: "#fff7ed", color: "#ea580c" },
  { id: "beverage", name: "Đồ Uống & Trà", count: 32, icon: "🧃", bg: "#eff6ff", color: "#2563eb" },
  { id: "bakery", name: "Bánh Mì & Ngọt", count: 29, icon: "🥐", bg: "#fdf4ff", color: "#9333ea" },
  { id: "household", name: "Nhu Yếu Phẩm", count: 53, icon: "🧴", bg: "#ecfdf5", color: "#059669" }
];

// Cửa hàng & Quán ngon đối tác trong bán kính 10km
const nearbyShops = [
  {
    id: "s1",
    name: "ZoneMart Bách Hóa Cầu Giấy",
    tag: "Bách hóa tổng hợp",
    distance: "1.2 km",
    rating: 4.9,
    reviews: 240,
    time: "15 - 20 phút",
    badge: "Freeship ≤ 3km",
    badgeColor: "#16a34a",
    address: "245 Cầu Giấy, P. Dịch Vọng, Hà Nội",
    image: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "s2",
    name: "Siêu Thị Trái Cây Tươi Xanh",
    tag: "Hoa quả nhập khẩu",
    distance: "2.5 km",
    rating: 4.8,
    reviews: 185,
    time: "20 - 25 phút",
    badge: "Giảm 15% Đơn Đầu",
    badgeColor: "#ea580c",
    address: "88 Trần Thái Tông, Dịch Vọng Hậu",
    image: "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "s3",
    name: "Tiệm Bánh Mì & Cà Phê Zone",
    tag: "Đồ ăn nhanh & Cafe",
    distance: "2.8 km",
    rating: 4.9,
    reviews: 320,
    time: "15 - 20 phút",
    badge: "Quán Nổi Bật",
    badgeColor: "#2563eb",
    address: "12 Hồ Tùng Mậu, Mai Dịch",
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "s4",
    name: "Bếp Cơm Niêu & Cơm Tấm Sài Gòn",
    tag: "Cơm trưa văn phòng",
    distance: "3.4 km",
    rating: 4.7,
    reviews: 154,
    time: "25 - 30 phút",
    badge: "Giao Nóng Hổi",
    badgeColor: "#dc2626",
    address: "56 Nguyễn Phong Sắc, Cầu Giấy",
    image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80"
  }
];

// Danh sách sản phẩm hot (đồng bộ giá VNĐ với ProductListView & CartView)
const products = [
  {
    id: "p1",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (500g)",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 185000,
    oldPrice: 220000,
    discount: "-16%",
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    rating: 5.0,
    sold: 142,
    isExpress: true,
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=500&q=80"
  },
  {
    id: "p2",
    name: "Hộp Dâu Tây Đà Lạt Tươi Ngọt Chuẩn VietGAP (500g)",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 95000,
    oldPrice: 125000,
    discount: "-24%",
    store: "Siêu Thị Trái Cây Xanh",
    distance: "2.5 km",
    rating: 4.9,
    sold: 218,
    isExpress: true,
    image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=500&q=80"
  },
  {
    id: "p3",
    name: "Combo Bánh Mì Chảo Nóng Hổi Kèm Pate & Xúc Xích",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 45000,
    oldPrice: 55000,
    discount: "-18%",
    store: "Tiệm Bánh Mì Zone",
    distance: "2.8 km",
    rating: 4.8,
    sold: 340,
    isExpress: true,
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=500&q=80"
  },
  {
    id: "p4",
    name: "Nước Ép Cam Tươi Nguyên Chất 100% Không Đường",
    category: "beverage",
    categoryName: "Đồ uống",
    price: 32000,
    oldPrice: 40000,
    discount: "-20%",
    store: "Siêu Thị Trái Cây Xanh",
    distance: "2.5 km",
    rating: 4.9,
    sold: 180,
    isExpress: true,
    image: "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=500&q=80"
  },
  {
    id: "p5",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
    category: "food",
    categoryName: "Nhu yếu phẩm",
    price: 190000,
    oldPrice: 225000,
    discount: "-15%",
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    rating: 5.0,
    sold: 96,
    isExpress: true,
    image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=500&q=80"
  },
  {
    id: "p6",
    name: "Rau Xà Lách Xoăn Thủy Canh Hữu Cơ Đà Lạt (300g)",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 25000,
    oldPrice: 32000,
    discount: "-22%",
    store: "Siêu Thị Trái Cây Xanh",
    distance: "2.5 km",
    rating: 4.9,
    sold: 110,
    isExpress: true,
    image: "https://images.unsplash.com/photo-1550411294-b3b1bf5bece1?auto=format&fit=crop&w=500&q=80"
  }
];

// Lọc sản phẩm theo tab
const activeProductTab = ref("all");

const filteredProducts = computed(() => {
  if (activeProductTab.value === "all") return products;
  return products.filter((p) => p.category === activeProductTab.value);
});

// Thêm vào giỏ hàng
const handleAddToCart = (productName: string) => {
  showNotification(`Đã thêm "${productName}" vào giỏ hàng!`);
};

// Đồng hồ đếm ngược Flash Deal
const countdownDays = ref("02");
const countdownHours = ref("08");
const countdownMins = ref("45");
const countdownSecs = ref("30");
let timer: any = null;

const startTimer = () => {
  let totalSec = 2 * 86400 + 8 * 3600 + 45 * 60 + 30;
  timer = setInterval(() => {
    if (totalSec <= 0) {
      clearInterval(timer);
      return;
    }
    totalSec--;
    const d = Math.floor(totalSec / 86400);
    const h = Math.floor((totalSec % 86400) / 3600);
    const m = Math.floor((totalSec % 3600) / 60);
    const s = totalSec % 60;
    countdownDays.value = d < 10 ? `0${d}` : `${d}`;
    countdownHours.value = h < 10 ? `0${h}` : `${h}`;
    countdownMins.value = m < 10 ? `0${m}` : `${m}`;
    countdownSecs.value = s < 10 ? `0${s}` : `${s}`;
  }, 1000);
};

onMounted(() => {
  startTimer();
});

onUnmounted(() => {
  if (timer) clearInterval(timer);
});
</script>

<template>
  <div class="home-page-frameless">
    <!-- Toast thông báo -->
    <transition name="fade">
      <div v-if="showToast" class="toast-popup">
        <span class="toast-icon">✅</span>
        <span>{{ toastMessage }}</span>
      </div>
    </transition>

    <!-- 1. HERO BANNER CHÍNH (FULL BLEED, NO BORDER) -->
    <section class="hero-frameless-section">
      <div class="hero-inner-container">
        <div class="hero-text-col">
          <div class="hero-lightning-pill">
            <span class="pill-spark">⚡</span>
            <span class="pill-title">GIAO HÀNG SIÊU TỐC TRONG BÁN KÍNH 10KM</span>
          </div>

          <h1 class="hero-main-heading">
            Thực Phẩm Tươi Sạch & Món Ngon<br />
            <span class="gradient-accent-text">Giao Tận Cửa Chỉ 20 Phút</span>
          </h1>

          <p class="hero-subtext">
            ZoneMart kết nối trực tiếp bạn với các cửa hàng tạp hóa, siêu thị trái cây và quán ăn quanh khu vực. Định vị GPS chuẩn xác, nhận hàng nóng hổi ngay lập tức.
          </p>

          <!-- Thanh tìm kiếm không khung (shadow mềm, borderless) -->
          <div class="search-frameless-bar">
            <span class="search-icon-symbol">🔍</span>
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Bạn muốn tìm món ngon hoặc thực phẩm gì quanh 10km hôm nay?..."
              class="search-frameless-input"
              @keyup.enter="handleSearch"
            />
            <button class="btn-search-frameless" @click="handleSearch">
              Tìm Ngay
            </button>
          </div>

          <!-- Từ khóa gợi ý -->
          <div class="hero-tags-group">
            <span class="tag-lead">Gợi ý nhanh:</span>
            <button class="tag-chip" @click="searchWithTag('Thịt bò')">Thịt bò tươi</button>
            <button class="tag-chip" @click="searchWithTag('Dâu tây')">Dâu tây Đà Lạt</button>
            <button class="tag-chip" @click="searchWithTag('Bánh mì')">Bánh mì chảo</button>
            <button class="tag-chip" @click="searchWithTag('Gạo ST25')">Gạo ST25</button>
            <button class="tag-chip map-chip" @click="router.push('/map')">
              🗺️ Xem bản đồ 10km
            </button>
          </div>
        </div>

        <!-- Visual / Graphic bên phải -->
        <div class="hero-visual-col">
          <div class="hero-img-stack">
            <img
              src="https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=900&q=80"
              alt="ZoneMart Delivery Food"
              class="hero-primary-photo"
            />

            <!-- Floating Card 1: Shipper live status -->
            <div class="floating-stat-badge stat-shipper">
              <div class="stat-icon-circle">🛵</div>
              <div class="stat-info">
                <span class="stat-label">Shipper ZoneMart</span>
                <span class="stat-val">Cách bạn 1.2km • 15 phút</span>
              </div>
            </div>

            <!-- Floating Card 2: Rating -->
            <div class="floating-stat-badge stat-quality">
              <div class="stat-icon-circle gold">⭐</div>
              <div class="stat-info">
                <span class="stat-val">4.9 / 5.0 Điểm</span>
                <span class="stat-label">Từ 10.000+ cư dân</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 2. DANH MỤC NGÀNH HÀNG (7 PILLS HIỆN ĐẠI, KHÔNG KHUNG) -->
    <section class="section-container">
      <div class="section-title-wrap">
        <div>
          <h2 class="section-main-title">Danh Mục Nổi Bật</h2>
          <p class="section-subtitle">Khám phá các nhóm nhu yếu phẩm và ẩm thực được giao nhiều nhất hôm nay</p>
        </div>
        <button class="link-view-all" @click="router.push('/products')">
          Xem tất cả sản phẩm ➔
        </button>
      </div>

      <div class="categories-frameless-grid">
        <div
          v-for="cat in categories"
          :key="cat.id"
          class="cat-frameless-card"
          @click="router.push({ path: '/products', query: { cat: cat.id } })"
        >
          <div class="cat-icon-bubble" :style="{ backgroundColor: cat.bg }">
            <span class="cat-icon-emoji">{{ cat.icon }}</span>
          </div>
          <h3 class="cat-name-text">{{ cat.name }}</h3>
          <span class="cat-count-text">{{ cat.count }} sản phẩm</span>
        </div>
      </div>
    </section>

    <!-- 3. PHÂN HỆ THÀNH VIÊN (BUYER, SELLER, SHIPPER) -->
    <section class="section-container">
      <div class="roles-frameless-grid">
        <!-- Khách hàng -->
        <div class="role-card-frameless role-buyer" @click="router.push('/products')">
          <div class="role-card-top">
            <span class="role-tag-chip">🛍️ Dành Cho Khách Hàng</span>
            <span class="role-dist-tag">≤ 10km Hỏa Tốc</span>
          </div>
          <h3 class="role-title">Mua Sắm & Đặt Đồ Ăn Tiện Lợi</h3>
          <p class="role-desc">
            Xem thực đơn các cửa hàng xung quanh, đo khoảng cách chính xác, nhận hàng tận tay trong 20 phút.
          </p>
          <div class="role-action-link">
            <span>Khám phá sản phẩm ngay</span>
            <span class="arrow-sym">➔</span>
          </div>
        </div>

        <!-- Chủ quán -->
        <div class="role-card-frameless role-seller" @click="router.push('/register-seller')">
          <div class="role-card-top">
            <span class="role-tag-chip">🏪 Dành Cho Chủ Quán</span>
            <span class="role-dist-tag">Miễn Phí Đăng Ký</span>
          </div>
          <h3 class="role-title">Mở Gian Hàng Cùng ZoneMart</h3>
          <p class="role-desc">
            Tiếp cận ngay hàng nghìn khách hàng tiềm năng sinh sống trong bán kính 10km xung quanh cửa hàng.
          </p>
          <div class="role-action-link">
            <span>Đăng ký bán hàng ngay</span>
            <span class="arrow-sym">➔</span>
          </div>
        </div>

        <!-- Shipper -->
        <div class="role-card-frameless role-shipper" @click="router.push('/shipper')">
          <div class="role-card-top">
            <span class="role-tag-chip">🛵 Dành Cho Tài Xế</span>
            <span class="role-dist-tag">Thu Nhập Hấp Dẫn</span>
          </div>
          <h3 class="role-title">Gia Nhập Đội Xe Giao Hàng</h3>
          <p class="role-desc">
            Chủ động thời gian, nhận các đơn giao siêu tốc cự ly gần dưới 3km và đơn thường trong khu vực 10km.
          </p>
          <div class="role-action-link">
            <span>Truy cập cổng shipper</span>
            <span class="arrow-sym">➔</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 4. QUÁN NGON & CỬA HÀNG GẦN BẠN (10KM GPS) -->
    <section class="section-container">
      <div class="section-title-wrap">
        <div>
          <div class="badge-inline-green">📍 ĐỊNH VỊ GPS</div>
          <h2 class="section-main-title">Cửa Hàng Gần Bạn (Trong Bán Kính 10km)</h2>
          <p class="section-subtitle">Các siêu thị mini, tiệm hoa quả và quán ăn có thời gian chuẩn bị & giao hàng nhanh nhất</p>
        </div>
        <button class="link-view-all" @click="router.push('/map')">
          🗺️ Xem trên bản đồ trực quan ➔
        </button>
      </div>

      <div class="shops-frameless-grid">
        <div
          v-for="shop in nearbyShops"
          :key="shop.id"
          class="shop-frameless-card"
          @click="router.push('/products')"
        >
          <div class="shop-media-wrap">
            <img :src="shop.image" :alt="shop.name" class="shop-photo" />
            <span class="shop-distance-pill">📍 {{ shop.distance }}</span>
            <span class="shop-promo-pill" :style="{ backgroundColor: shop.badgeColor }">
              {{ shop.badge }}
            </span>
          </div>

          <div class="shop-body-wrap">
            <div class="shop-meta-line">
              <span class="shop-category-label">{{ shop.tag }}</span>
              <span class="shop-rating-label">⭐ {{ shop.rating }} ({{ shop.reviews }})</span>
            </div>

            <h3 class="shop-name-heading">{{ shop.name }}</h3>
            <p class="shop-address-text">{{ shop.address }}</p>

            <div class="shop-bottom-status">
              <span class="shop-eta-tag">⚡ {{ shop.time }}</span>
              <span class="shop-view-action">Vào quán ➔</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 5. SẢN PHẨM BÁN CHẠY HỎA TỐC (HOT PRODUCTS) -->
    <section class="section-container">
      <div class="section-title-wrap">
        <div>
          <h2 class="section-main-title">Sản Phẩm Đang Được Mua Nhiều</h2>
          <p class="section-subtitle">Các mặt hàng tươi sống, đồ ăn và nhu yếu phẩm chất lượng cao có sẵn giao ngay</p>
        </div>

        <!-- Filter tabs -->
        <div class="filter-tabs-cluster">
          <button
            :class="['filter-pill-btn', { active: activeProductTab === 'all' }]"
            @click="activeProductTab = 'all'"
          >
            Tất cả
          </button>
          <button
            :class="['filter-pill-btn', { active: activeProductTab === 'food' }]"
            @click="activeProductTab = 'food'"
          >
            Thực phẩm tươi
          </button>
          <button
            :class="['filter-pill-btn', { active: activeProductTab === 'veggie' }]"
            @click="activeProductTab = 'veggie'"
          >
            Rau củ quả
          </button>
          <button
            :class="['filter-pill-btn', { active: activeProductTab === 'fastfood' }]"
            @click="activeProductTab = 'fastfood'"
          >
            Món ăn liền
          </button>
          <button
            :class="['filter-pill-btn', { active: activeProductTab === 'beverage' }]"
            @click="activeProductTab = 'beverage'"
          >
            Đồ uống
          </button>
        </div>
      </div>

      <div class="products-frameless-grid">
        <div
          v-for="p in filteredProducts"
          :key="p.id"
          class="product-frameless-card"
        >
          <div class="product-media-box" @click="router.push('/products')">
            <img :src="p.image" :alt="p.name" class="product-img-fit" />
            <span class="product-express-badge" v-if="p.isExpress">⚡ Hỏa Tốc</span>
            <span class="product-discount-badge" v-if="p.discount">{{ p.discount }}</span>
          </div>

          <div class="product-info-box">
            <div class="product-sub-row">
              <span class="prod-shop-name">🏪 {{ p.store }}</span>
              <span class="prod-dist-text">{{ p.distance }}</span>
            </div>

            <h3 class="product-name-title" :title="p.name" @click="router.push('/products')">
              {{ p.name }}
            </h3>

            <div class="product-rating-row">
              <span class="stars-gold">★★★★★</span>
              <span class="sold-num">Đã bán {{ p.sold }}</span>
            </div>

            <div class="product-price-action-row">
              <div class="price-stack">
                <span class="price-current">{{ p.price.toLocaleString("vi-VN") }} ₫</span>
                <span v-if="p.oldPrice" class="price-original">
                  {{ p.oldPrice.toLocaleString("vi-VN") }} ₫
                </span>
              </div>

              <button
                class="btn-quick-cart"
                title="Thêm vào giỏ hàng"
                @click="handleAddToCart(p.name)"
              >
                🛒 Thêm
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 6. FLASH DEAL GIỜ VÀNG TRONG BÁN KÍNH 10KM -->
    <section class="section-container">
      <div class="flash-deal-banner">
        <div class="deal-banner-content">
          <span class="deal-top-badge">🔥 DEAL CHỚP NHOÁNG HÔM NAY</span>
          <h2 class="deal-headline">Giảm Tới 50% Khi Mua Đơn Hàng Đầu Tiên</h2>
          <p class="deal-description">
            Áp dụng cho toàn bộ cửa hàng đối tác trong bán kính 10km. Nhập mã <strong>ZONEMART10</strong> khi thanh toán để được miễn phí vận chuyển siêu tốc.
          </p>

          <!-- Countdown timer không khung -->
          <div class="countdown-row">
            <div class="countdown-tile">
              <span class="num">{{ countdownDays }}</span>
              <span class="unit">Ngày</span>
            </div>
            <span class="colon">:</span>
            <div class="countdown-tile">
              <span class="num">{{ countdownHours }}</span>
              <span class="unit">Giờ</span>
            </div>
            <span class="colon">:</span>
            <div class="countdown-tile">
              <span class="num">{{ countdownMins }}</span>
              <span class="unit">Phút</span>
            </div>
            <span class="colon">:</span>
            <div class="countdown-tile">
              <span class="num">{{ countdownSecs }}</span>
              <span class="unit">Giây</span>
            </div>
          </div>

          <div class="deal-cta-row">
            <button class="btn-deal-primary" @click="router.push('/products')">
              Săn Deal Ngay ➔
            </button>
            <button class="btn-deal-secondary" @click="router.push('/cart')">
              Xem Giỏ Hàng
            </button>
          </div>
        </div>

        <div class="deal-banner-visual">
          <img
            src="https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80"
            alt="Organic Produce Deal"
            class="deal-hero-image"
          />
        </div>
      </div>
    </section>

    <!-- 7. CAM KẾT DỊCH VỤ ZONEMART (NO BORDERS) -->
    <section class="section-container commitments-bottom">
      <div class="commitments-frameless-grid">
        <div class="commit-item">
          <div class="commit-icon-box" style="background-color: #eff6ff; color: #2563eb;">
            ⚡
          </div>
          <div class="commit-text">
            <h4>Giao Hàng Dưới 30 Phút</h4>
            <p>Đội ngũ shipper cơ động nhận đơn và giao ngay trong bán kính 10km quanh bạn.</p>
          </div>
        </div>

        <div class="commit-item">
          <div class="commit-icon-box" style="background-color: #f0fdf4; color: #16a34a;">
            🥬
          </div>
          <div class="commit-text">
            <h4>100% Tươi Ngon Chuẩn Vị</h4>
            <p>Nông sản, thực phẩm tươi sống và món ăn được đóng gói cẩn thận, đảm bảo vệ sinh.</p>
          </div>
        </div>

        <div class="commit-item">
          <div class="commit-icon-box" style="background-color: #fff7ed; color: #ea580c;">
            🏷️
          </div>
          <div class="commit-text">
            <h4>Giá Cả & Phí Minh Bạch</h4>
            <p>Khoảng cách GPS chính xác, cước phí hiển thị rõ ràng không có bất kỳ phụ phí ẩn.</p>
          </div>
        </div>

        <div class="commit-item">
          <div class="commit-icon-box" style="background-color: #fdf4ff; color: #a855f7;">
            💬
          </div>
          <div class="commit-text">
            <h4>Hỗ Trợ Tận Tâm 24/7</h4>
            <p>Đội ngũ chăm sóc khách hàng và hỗ trợ đổi trả nhanh chóng khi sản phẩm có vấn đề.</p>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* ==========================================================================
   RESET VÀ THIẾT KẾ TRÀN VIỀN - HOÀN TOÀN KHÔNG DÙNG KHUNG VIỀN (BORDER: NONE)
   ========================================================================== */
.home-page-frameless {
  width: 100%;
  min-height: 100%;
  background-color: #f8fafc;
  color: #1e293b;
  overflow-x: hidden;
}

/* Toast popup */
.toast-popup {
  position: fixed;
  top: 85px;
  right: 25px;
  background-color: #0f172a;
  color: #ffffff;
  padding: 12px 22px;
  border-radius: 14px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  z-index: 9999;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  font-weight: 600;
}
.toast-icon {
  font-size: 18px;
}
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s, transform 0.3s;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

/* Container đệm thoáng, không bao bọc khung */
.section-container {
  width: 100%;
  max-width: 1320px;
  margin: 0 auto;
  padding: 48px 24px;
}

.section-title-wrap {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 28px;
  flex-wrap: wrap;
  gap: 16px;
}

.badge-inline-green {
  display: inline-block;
  font-size: 11px;
  font-weight: 800;
  color: #16a34a;
  background: #dcfce7;
  padding: 3px 10px;
  border-radius: 20px;
  margin-bottom: 6px;
  letter-spacing: 0.5px;
}

.section-main-title {
  margin: 0;
  font-size: 26px;
  font-weight: 800;
  color: #0f172a;
  letter-spacing: -0.4px;
}

.section-subtitle {
  margin: 6px 0 0 0;
  font-size: 14px;
  color: #64748b;
}

.link-view-all {
  background: transparent;
  border: none;
  color: #ea580c;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  padding: 6px 0;
  transition: transform 0.2s;
}
.link-view-all:hover {
  transform: translateX(4px);
}

/* ==========================================================================
   1. HERO BANNER
   ========================================================================== */
.hero-frameless-section {
  width: 100%;
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 60%, #334155 100%);
  color: #ffffff;
  padding: 60px 24px 80px 24px;
  position: relative;
  overflow: hidden;
}

.hero-inner-container {
  max-width: 1320px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 1.15fr 0.85fr;
  gap: 40px;
  align-items: center;
}

.hero-lightning-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  padding: 6px 16px;
  border-radius: 30px;
  margin-bottom: 20px;
}

.pill-spark {
  color: #facc15;
  font-size: 15px;
}

.pill-title {
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.6px;
  color: #f8fafc;
}

.hero-main-heading {
  font-size: 42px;
  font-weight: 900;
  line-height: 1.2;
  margin: 0 0 18px 0;
  letter-spacing: -0.8px;
}

.gradient-accent-text {
  background: linear-gradient(90deg, #f97316 0%, #fb923c 50%, #facc15 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.hero-subtext {
  font-size: 15px;
  line-height: 1.65;
  color: #cbd5e1;
  margin: 0 0 28px 0;
  max-width: 580px;
}

/* Thanh search không viền */
.search-frameless-bar {
  display: flex;
  align-items: center;
  background: #ffffff;
  border-radius: 18px;
  padding: 6px 8px 6px 18px;
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.25);
  max-width: 620px;
  border: none;
}

.search-icon-symbol {
  font-size: 18px;
  color: #94a3b8;
  margin-right: 10px;
}

.search-frameless-input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 14px;
  color: #1e293b;
  background: transparent;
  padding: 8px 0;
}

.search-frameless-input::placeholder {
  color: #94a3b8;
}

.btn-search-frameless {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  color: #ffffff;
  border: none;
  padding: 12px 24px;
  border-radius: 12px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.btn-search-frameless:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.4);
}

.hero-tags-group {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 20px;
}

.tag-lead {
  font-size: 13px;
  color: #94a3b8;
}

.tag-chip {
  background: rgba(255, 255, 255, 0.12);
  color: #f1f5f9;
  border: none;
  padding: 5px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.tag-chip:hover {
  background: rgba(255, 255, 255, 0.25);
}

.map-chip {
  background: rgba(37, 99, 235, 0.3);
  color: #93c5fd;
}

.map-chip:hover {
  background: rgba(37, 99, 235, 0.5);
}

/* Visual stack */
.hero-img-stack {
  position: relative;
  width: 100%;
  height: 400px;
}

.hero-primary-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 28px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.35);
  border: none;
}

.floating-stat-badge {
  position: absolute;
  background: rgba(15, 23, 42, 0.9);
  backdrop-filter: blur(12px);
  padding: 10px 16px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  color: #ffffff;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.3);
  border: none;
}

.stat-shipper {
  bottom: 25px;
  left: -20px;
}

.stat-quality {
  top: 25px;
  right: -15px;
}

.stat-icon-circle {
  width: 40px;
  height: 40px;
  background: #ea580c;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}

.stat-icon-circle.gold {
  background: #eab308;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 11px;
  color: #94a3b8;
}

.stat-val {
  font-size: 13px;
  font-weight: 800;
}

/* ==========================================================================
   2. CATEGORIES GRID (NO BORDER)
   ========================================================================== */
.categories-frameless-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 18px;
}

.cat-frameless-card {
  background: #ffffff;
  border: none;
  border-radius: 20px;
  padding: 22px 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  cursor: pointer;
  box-shadow: 0 4px 18px -2px rgba(0, 0, 0, 0.04);
  transition: all 0.25s ease;
}

.cat-frameless-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 12px 25px -4px rgba(234, 88, 12, 0.14);
}

.cat-icon-bubble {
  width: 58px;
  height: 58px;
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
}

.cat-icon-emoji {
  font-size: 28px;
}

.cat-name-text {
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  margin: 0 0 4px 0;
}

.cat-count-text {
  font-size: 12px;
  color: #64748b;
}

/* ==========================================================================
   3. ROLE ECOSYSTEM CARDS (NO BORDER)
   ========================================================================== */
.roles-frameless-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
}

.role-card-frameless {
  border: none;
  border-radius: 24px;
  padding: 30px;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  transition: all 0.25s ease;
}

.role-card-frameless:hover {
  transform: translateY(-5px);
  box-shadow: 0 16px 30px -6px rgba(0, 0, 0, 0.08);
}

.role-buyer {
  background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
}

.role-seller {
  background: linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%);
}

.role-shipper {
  background: linear-gradient(135deg, #fdf4ff 0%, #fae8ff 100%);
}

.role-card-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14px;
}

.role-tag-chip {
  font-size: 12px;
  font-weight: 800;
  color: #1e293b;
  background: rgba(255, 255, 255, 0.8);
  padding: 4px 12px;
  border-radius: 12px;
}

.role-dist-tag {
  font-size: 11px;
  font-weight: 700;
  color: #475569;
}

.role-title {
  margin: 0 0 8px 0;
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
}

.role-desc {
  font-size: 13px;
  line-height: 1.6;
  color: #475569;
  margin: 0 0 20px 0;
  flex: 1;
}

.role-action-link {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 13px;
  font-weight: 800;
  color: #0f172a;
  background: #ffffff;
  padding: 10px 18px;
  border-radius: 14px;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.04);
}

.arrow-sym {
  font-size: 15px;
  transition: transform 0.2s;
}

.role-card-frameless:hover .arrow-sym {
  transform: translateX(4px);
}

/* ==========================================================================
   4. SHOPS GRID (NO BORDER)
   ========================================================================== */
.shops-frameless-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
}

.shop-frameless-card {
  background: #ffffff;
  border: none;
  border-radius: 22px;
  overflow: hidden;
  cursor: pointer;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.05);
  transition: all 0.25s ease;
  display: flex;
  flex-direction: column;
}

.shop-frameless-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 14px 30px -4px rgba(0, 0, 0, 0.09);
}

.shop-media-wrap {
  position: relative;
  height: 150px;
}

.shop-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border: none;
}

.shop-distance-pill {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(15, 23, 42, 0.85);
  color: #ffffff;
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
}

.shop-promo-pill {
  position: absolute;
  top: 10px;
  right: 10px;
  color: #ffffff;
  padding: 4px 10px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 800;
}

.shop-body-wrap {
  padding: 16px;
  display: flex;
  flex-direction: column;
  flex: 1;
}

.shop-meta-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.shop-category-label {
  font-size: 11px;
  color: #64748b;
  font-weight: 600;
}

.shop-rating-label {
  font-size: 12px;
  font-weight: 700;
  color: #eab308;
}

.shop-name-heading {
  margin: 0 0 6px 0;
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
}

.shop-address-text {
  font-size: 12px;
  color: #64748b;
  margin: 0 0 14px 0;
  line-height: 1.4;
}

.shop-bottom-status {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 10px;
}

.shop-eta-tag {
  font-size: 12px;
  font-weight: 700;
  color: #16a34a;
}

.shop-view-action {
  font-size: 12px;
  font-weight: 800;
  color: #ea580c;
}

/* ==========================================================================
   5. PRODUCTS GRID (NO BORDER)
   ========================================================================== */
.filter-tabs-cluster {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.filter-pill-btn {
  background: #ffffff;
  border: none;
  padding: 8px 18px;
  border-radius: 14px;
  font-size: 13px;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.03);
  transition: all 0.2s;
}

.filter-pill-btn:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.filter-pill-btn.active {
  background: #ea580c;
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.25);
}

.products-frameless-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 18px;
}

.product-frameless-card {
  background: #ffffff;
  border: none;
  border-radius: 20px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 4px 18px -2px rgba(0, 0, 0, 0.04);
  transition: all 0.25s ease;
}

.product-frameless-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 14px 28px -4px rgba(0, 0, 0, 0.09);
}

.product-media-box {
  position: relative;
  height: 150px;
  cursor: pointer;
  overflow: hidden;
}

.product-img-fit {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border: none;
  transition: transform 0.3s ease;
}

.product-frameless-card:hover .product-img-fit {
  transform: scale(1.06);
}

.product-express-badge {
  position: absolute;
  top: 8px;
  left: 8px;
  background: #dc2626;
  color: #ffffff;
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 10px;
  font-weight: 800;
}

.product-discount-badge {
  position: absolute;
  top: 8px;
  right: 8px;
  background: #ea580c;
  color: #ffffff;
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 10px;
  font-weight: 800;
}

.product-info-box {
  padding: 14px;
  display: flex;
  flex-direction: column;
  flex: 1;
}

.product-sub-row {
  display: flex;
  justify-content: space-between;
  font-size: 11px;
  color: #64748b;
  margin-bottom: 4px;
}

.product-name-title {
  margin: 0 0 6px 0;
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
  height: 36px;
  overflow: hidden;
  cursor: pointer;
}

.product-rating-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  color: #94a3b8;
  margin-bottom: 10px;
}

.stars-gold {
  color: #eab308;
  letter-spacing: 1px;
}

.product-price-action-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-top: auto;
}

.price-stack {
  display: flex;
  flex-direction: column;
}

.price-current {
  font-size: 15px;
  font-weight: 800;
  color: #ea580c;
}

.price-original {
  font-size: 11px;
  color: #94a3b8;
  text-decoration: line-through;
}

.btn-quick-cart {
  background: #0f172a;
  color: #ffffff;
  border: none;
  padding: 6px 12px;
  border-radius: 10px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-quick-cart:hover {
  background: #ea580c;
}

/* ==========================================================================
   6. FLASH DEAL BANNER (NO BORDER)
   ========================================================================== */
.flash-deal-banner {
  background: linear-gradient(135deg, #fff7ed 0%, #ffedd5 50%, #fed7aa 100%);
  border: none;
  border-radius: 28px;
  padding: 44px;
  display: grid;
  grid-template-columns: 1.2fr 0.8fr;
  gap: 30px;
  align-items: center;
  box-shadow: 0 10px 30px -5px rgba(234, 88, 12, 0.12);
}

.deal-top-badge {
  display: inline-block;
  font-size: 11px;
  font-weight: 800;
  color: #ea580c;
  background: #ffffff;
  padding: 5px 14px;
  border-radius: 20px;
  margin-bottom: 12px;
}

.deal-headline {
  margin: 0 0 12px 0;
  font-size: 28px;
  font-weight: 900;
  color: #0f172a;
  line-height: 1.25;
}

.deal-description {
  margin: 0 0 24px 0;
  font-size: 14px;
  line-height: 1.6;
  color: #475569;
}

.countdown-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 26px;
}

.countdown-tile {
  background: #0f172a;
  color: #ffffff;
  border-radius: 14px;
  padding: 10px 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  min-width: 52px;
}

.countdown-tile .num {
  font-size: 18px;
  font-weight: 800;
  line-height: 1;
}

.countdown-tile .unit {
  font-size: 10px;
  color: #94a3b8;
  margin-top: 3px;
}

.colon {
  font-size: 20px;
  font-weight: 800;
  color: #ea580c;
}

.deal-cta-row {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.btn-deal-primary {
  background: #ea580c;
  color: #ffffff;
  border: none;
  padding: 12px 24px;
  border-radius: 14px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-deal-primary:hover {
  background: #c2410c;
  transform: translateY(-2px);
}

.btn-deal-secondary {
  background: #ffffff;
  color: #0f172a;
  border: none;
  padding: 12px 20px;
  border-radius: 14px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-deal-secondary:hover {
  background: #f1f5f9;
}

.deal-banner-visual {
  height: 280px;
}

.deal-hero-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 20px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
  border: none;
}

/* ==========================================================================
   7. COMMITMENTS (NO BORDER)
   ========================================================================== */
.commitments-frameless-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  background: #ffffff;
  border: none;
  border-radius: 24px;
  padding: 30px;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.04);
}

.commit-item {
  display: flex;
  align-items: flex-start;
  gap: 16px;
}

.commit-icon-box {
  width: 48px;
  height: 48px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  flex-shrink: 0;
}

.commit-text h4 {
  margin: 0 0 4px 0;
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
}

.commit-text p {
  margin: 0;
  font-size: 12px;
  color: #64748b;
  line-height: 1.5;
}

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1200px) {
  .categories-frameless-grid {
    grid-template-columns: repeat(3, 1fr);
  }
  .shops-frameless-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .products-frameless-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 992px) {
  .hero-inner-container {
    grid-template-columns: 1fr;
  }
  .hero-img-stack {
    height: 280px;
  }
  .roles-frameless-grid {
    grid-template-columns: 1fr;
  }
  .flash-deal-banner {
    grid-template-columns: 1fr;
    padding: 30px;
  }
  .deal-banner-visual {
    height: 200px;
  }
  .commitments-frameless-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .hero-main-heading {
    font-size: 28px;
  }
  .categories-frameless-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .shops-frameless-grid {
    grid-template-columns: 1fr;
  }
  .products-frameless-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .commitments-frameless-grid {
    grid-template-columns: 1fr;
  }
  .search-frameless-bar {
    flex-direction: column;
    padding: 12px;
    gap: 10px;
  }
  .btn-search-frameless {
    width: 100%;
  }
}
</style>

