<script setup lang="ts">
/**
 * ============================================================================
 * TRANG DANH SÁCH SẢN PHẨM (PRODUCT LIST VIEW) - ZONE MART
 * TRANG DANH SÁCH SẢN PHẨM & GIAN HÀNG (CATALOG & STORES VIEW) - ZONE MART
 * Thiết kế áp dụng Taste-Skill: Anti-Slop, Bento Asymmetry, Frameless, Tactile
 * Design Read: Hyper-local on-demand grocery & dining catalog with appetizing
 * editorial commerce language, warm terracotta tones, and tactile interactions.
 * Hỗ trợ tìm kiếm linh hoạt: Sản phẩm HOẶC Gian hàng
 * ============================================================================
 */
import { ref, computed, watch, onMounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useProductCatalog } from "../../composables/useProductCatalog";
import { useProductCatalog, type CatalogStore } from "../../composables/useProductCatalog";
import { useCart } from "../../composables/useCart";

const router = useRouter();
const route = useRoute();
const cart = useCart();
const catalog = useProductCatalog();

// State lọc & tìm kiếm
const searchQuery = ref("");
const selectedCategory = ref("all");
const maxRadiusKm = ref(10);
const sortBy = ref("popular"); // 'popular' | 'distance' | 'price-asc' | 'price-desc' | 'rating'
const activeSearchTab = ref<"product" | "store">("product"); // Tab tìm kiếm: 'product' hoặc 'store'
const selectedStoreFilter = ref<string>(""); // Tên gian hàng cụ thể nếu người dùng đang lọc theo gian hàng
const toastMessage = ref("");
const showToast = ref(false);

const showNotification = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2400);
};

// Đọc query từ URL (khi chuyển từ Home hoặc tìm kiếm)
onMounted(() => {
  if (route.query.q) {
    searchQuery.value = String(route.query.q);
// Đồng bộ từ URL query params (hỗ trợ cả search, q, cat, type, store)
const syncFromRoute = () => {
  const qVal = route.query.search !== undefined ? route.query.search : route.query.q;
  if (qVal !== undefined) {
    searchQuery.value = String(qVal || "");
  }
  if (route.query.cat) {
    selectedCategory.value = String(route.query.cat);
  if (route.query.cat !== undefined) {
    selectedCategory.value = String(route.query.cat || "all");
  }
});

watch(
  () => route.query,
  (newQuery) => {
    if (newQuery.q !== undefined) searchQuery.value = String(newQuery.q || "");
    if (newQuery.cat !== undefined) selectedCategory.value = String(newQuery.cat || "all");
  if (route.query.type === "store") {
    activeSearchTab.value = "store";
  } else if (route.query.type === "product") {
    activeSearchTab.value = "product";
  }
);
  if (route.query.store) {
    selectedStoreFilter.value = String(route.query.store);
    activeSearchTab.value = "product";
  } else {
    selectedStoreFilter.value = "";
  }
};

onMounted(syncFromRoute);
watch(() => route.query, syncFromRoute);

// Danh mục ngành hàng với biểu tượng Bootstrap Icons chuẩn nhận diện
const categories = [
  { id: "all", name: "Tất cả", icon: "bi-grid-fill", count: 12 },
  { id: "food", name: "Thực phẩm tươi", icon: "bi-fire", count: 4 },
  { id: "veggie", name: "Rau củ VietGAP", icon: "bi-flower1", count: 3 },
  { id: "fastfood", name: "Món ăn nóng", icon: "bi-cup-hot-fill", count: 2 },
  { id: "beverage", name: "Đồ uống & Trái cây", icon: "bi-cup-straw", count: 3 },
  { id: "household", name: "Nhu yếu phẩm", icon: "bi-basket2-fill", count: 2 }
];

// Danh sách sản phẩm đầy đủ với dữ liệu chân thực
interface ProductItem {
  id: string;
  name: string;
  category: string;
  categoryName: string;
  price: number;
  oldPrice?: number;
  discountBadge?: string;
  storeName: string;
  distanceKm: number;
  deliveryTime: string;
  rating: number;
  reviews: number;
  sold: number;
  badge?: string;
  unit: string;
  image: string;
}


// Lọc và sắp xếp theo điều kiện
const filteredProducts = computed(() => {
  const combined: ProductItem[] = catalog.allProducts.value.map((p) => ({
    id: p.id,
    name: p.name,
    category: p.category,
    categoryName: p.categoryName,
    price: p.price,
    oldPrice: p.oldPrice,
    discountBadge: p.discountBadge,
    storeName: p.store?.name || "ZoneMart Cầu Giấy",
    distanceKm: p.store?.distanceKm || 1.5,
    deliveryTime: p.store?.deliveryTime || "15 - 20 phút",
    rating: p.rating,
    reviews: p.reviewsCount,
    sold: p.sold,
    badge: p.badge,
    unit: p.unit,
    image: p.image,
  }));

  const q = searchQuery.value.trim().toLowerCase();

  let result = combined.filter((p) => {
    const matchCategory =
      selectedCategory.value === "all" || p.category === selectedCategory.value;
    const matchQuery =
      !searchQuery.value.trim() ||
      p.name.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      p.storeName.toLowerCase().includes(searchQuery.value.toLowerCase());
      !q ||
      p.name.toLowerCase().includes(q) ||
      p.categoryName.toLowerCase().includes(q) ||
      p.storeName.toLowerCase().includes(q);
    const matchDistance = p.distanceKm <= maxRadiusKm.value;
    return matchCategory && matchQuery && matchDistance;
    const matchStore =
      !selectedStoreFilter.value ||
      p.storeName.toLowerCase() === selectedStoreFilter.value.toLowerCase();

    return matchCategory && matchQuery && matchDistance && matchStore;
  });

  // Sắp xếp
  if (sortBy.value === "price-asc") {
    result.sort((a, b) => a.price - b.price);
  } else if (sortBy.value === "price-desc") {
    result.sort((a, b) => b.price - a.price);
  } else if (sortBy.value === "distance") {
    result.sort((a, b) => a.distanceKm - b.distanceKm);
  } else if (sortBy.value === "rating") {
    result.sort((a, b) => b.rating - a.rating);
  } else {
    // popular
    result.sort((a, b) => b.sold - a.sold);
  }

  return result;
});

// Lọc gian hàng theo điều kiện
const filteredStores = computed<CatalogStore[]>(() => {
  const q = searchQuery.value.trim().toLowerCase();
  let list = catalog.allStores.value;

  if (q) {
    list = list.filter((s) => {
      const matchName = s.name.toLowerCase().includes(q);
      const matchAddress = s.address.toLowerCase().includes(q);
      const matchCategory = s.categoryName ? s.categoryName.toLowerCase().includes(q) : false;
      const matchProduct = s.products.some(
        (p) =>
          p.name.toLowerCase().includes(q) ||
          p.categoryName.toLowerCase().includes(q)
      );
      return matchName || matchAddress || matchCategory || matchProduct;
    });
  }

  list = list.filter((s) => s.distanceKm <= maxRadiusKm.value);

  if (sortBy.value === "distance") {
    return [...list].sort((a, b) => a.distanceKm - b.distanceKm);
  } else if (sortBy.value === "rating") {
    return [...list].sort((a, b) => b.rating - a.rating);
  } else {
    return [...list].sort((a, b) => b.totalProducts - a.totalProducts || b.rating - a.rating);
  }
});

// Thông tin gian hàng đang được chọn (nếu có lọc theo gian hàng)
const activeStoreInfo = computed(() => {
  if (!selectedStoreFilter.value) return null;
  return catalog.allStores.value.find(
    (s) => s.name.toLowerCase() === selectedStoreFilter.value.toLowerCase()
  );
});

// Chuyển tab tìm kiếm: 'product' hoặc 'store'
const setSearchTab = (tab: "product" | "store") => {
  activeSearchTab.value = tab;
  router.replace({
    path: "/products",
    query: {
      ...route.query,
      type: tab,
    },
  });
};

// Chọn một gian hàng để xem danh sách sản phẩm của riêng gian hàng đó
const selectStore = (storeName: string) => {
  selectedStoreFilter.value = storeName;
  activeSearchTab.value = "product";
  router.replace({
    path: "/products",
    query: {
      ...route.query,
      store: storeName,
      type: "product",
    },
  });
};

// Hủy lọc gian hàng để xem tất cả
const clearStoreFilter = () => {
  selectedStoreFilter.value = "";
  const newQuery = { ...route.query };
  delete newQuery.store;
  router.replace({ path: "/products", query: newQuery });
};

// Chuyển sang trang chi tiết
const goToDetail = (id: string) => {
  router.push(`/products/${id}`);
};

// Thêm vào giỏ hàng thực tế với Toast tương tác mượt mà
const onAddToCart = (p: ProductItem) => {
  cart.addItem(
    {
      storeId: 'store_' + (p.storeName || 'default').toLowerCase().replace(/\s+/g, '_'),
      storeName: p.storeName || 'ZoneMart Đối Tác',
      distanceKm: p.distanceKm || 1.5,
      deliveryTime: p.deliveryTime || '15 - 20 phút',
    },
    {
      id: p.id,
      name: p.name,
      price: p.price,
      originalPrice: p.oldPrice,
      unit: p.unit,
      image: p.image,
    }
  );
  showNotification(`Đã thêm "${p.name}" vào giỏ hàng!`);
};

// Reset bộ lọc
const resetFilters = () => {
  searchQuery.value = "";
  selectedCategory.value = "all";
  maxRadiusKm.value = 10;
  sortBy.value = "popular";
  selectedStoreFilter.value = "";
  activeSearchTab.value = "product";
  router.replace({ path: "/products" });
};
</script>

<template>
  <div class="product-catalog-page">
    <!-- Toast Popup phản hồi nhanh -->
    <transition name="toast-fade">
      <div v-if="showToast" class="toast-floating-chip">
        <span class="toast-check">✔</span>
        <span class="toast-text">{{ toastMessage }}</span>
      </div>
    </transition>

    <div class="catalog-container">
      <!-- 1. BENTO HERO BANNER (ASYMMETRICAL SPOTLIGHT) -->
      <section class="bento-spotlight-section">
        <!-- Card 1 (65% width): Tiêu điểm ẩm thực & nông sản hôm nay -->
        <div class="bento-card-main">
          <div class="bento-text-side">
            <div class="live-gps-pill">
              <span class="gps-dot"></span>
              <span class="gps-label">GIAO HỎA TỐC BÁN KÍNH 10KM</span>
            </div>
            <h1 class="spotlight-title">
              Nông Sản Sạch &<br />
              <span class="text-gradient-warm">Món Ngon Địa Phương</span>
            </h1>
            <p class="spotlight-desc">
              Tất cả sản phẩm được chuẩn bị từ các cửa hàng uy tín gần bạn, giao tận cửa giữ trọn độ tươi nóng chỉ trong 20 phút.
            </p>

            <div class="spotlight-search-bar">
              <!-- Nút chuyển đổi nhanh chế độ tìm ngay trên banner -->
              <button
                type="button"
                class="spotlight-mode-pill"
                :title="activeSearchTab === 'store' ? 'Đang tìm: Gian hàng. Bấm để đổi sang Sản phẩm' : 'Đang tìm: Sản phẩm. Bấm để đổi sang Gian hàng'"
                @click="setSearchTab(activeSearchTab === 'product' ? 'store' : 'product')"
              >
                <i :class="activeSearchTab === 'store' ? 'bi bi-shop' : 'bi bi-box-seam-fill'"></i>
                <span>{{ activeSearchTab === 'store' ? 'Gian hàng' : 'Sản phẩm' }}</span>
                <i class="bi bi-arrow-left-right switch-icon"></i>
              </button>

              <div class="spotlight-search-divider"></div>

              <i class="bi bi-search lens-icon" aria-hidden="true"></i>
              <input
                v-model="searchQuery"
                type="text"
                placeholder="Tìm thịt tươi, rau sạch, bún chả, cơm tấm..."
                :placeholder="activeSearchTab === 'store' ? 'Tìm tên gian hàng, quán ăn, siêu thị gần bạn...' : 'Tìm thịt tươi, rau sạch, bún chả, cơm tấm...'"
                class="spotlight-input"
                aria-label="Tìm kiếm sản phẩm thực phẩm"
                aria-label="Tìm kiếm sản phẩm hoặc gian hàng"
              />
              <button
                v-if="searchQuery"
                type="button"
                class="btn-clear-search"
                aria-label="Xóa tìm kiếm"
                @click="searchQuery = ''"
                title="Xóa tìm kiếm"
              >
                <i class="bi bi-x-lg" aria-hidden="true"></i>
              </button>
            </div>
          </div>

          <div class="bento-visual-side">
            <img
              src="https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=700&q=80"
              alt="ZoneMart Fresh Catalog"
              class="bento-photo"
            />
            <div class="store-floating-tag">
              <span class="icon">⚡</span>
              <div>
                <strong>Cửa hàng gần bạn</strong>
                <small>Cam kết đúng giờ</small>
              </div>
            </div>
          </div>
        </div>

        <!-- Card 2 (35% width): Thẻ cam kết & tốc độ giao hàng -->
        <div class="bento-card-sub">
          <div class="sub-badge-speed">
            <span><i class="bi bi-lightning-charge-fill me-1" aria-hidden="true"></i> 15 - 25 PHÚT</span>
          </div>
          <h3 class="sub-card-title">Càng Gần Càng Nhanh, Càng Tươi</h3>
          <p class="sub-card-desc">
            Shipper ZoneMart sử dụng định vị GPS để tối ưu quãng đường ngắn nhất từ gian hàng đến nhà bạn.
          </p>

          <div class="radius-slider-box">
            <div class="radius-slider-header">
              <span class="slider-title">Bán kính quét quán:</span>
              <span class="slider-value">≤ {{ maxRadiusKm }} km</span>
            </div>
            <input
              type="range"
              min="1"
              max="10"
              step="0.5"
              v-model.number="maxRadiusKm"
              class="range-slider"
            />
            <div class="slider-ticks">
              <span>1km (Siêu gần)</span>
              <span>5km</span>
              <span>10km (Tối đa)</span>
            </div>
          </div>
        </div>
      </section>

      <!-- 2. BỘ LỌC DANH MỤC & ĐIỀU KHIỂN SẮP XẾP (FRAMELESS BAR) -->
      <!-- 2. BỘ CHUYỂN ĐỔI CHẾ ĐỘ TÌM KIẾM: SẢN PHẨM HOẶC GIAN HÀNG -->
      <section class="search-mode-nav-section">
        <div class="search-mode-tabs-wrap">
          <button
            class="search-mode-tab-btn"
            :class="{ active: activeSearchTab === 'product' }"
            @click="setSearchTab('product')"
          >
            <i class="bi bi-box-seam-fill"></i>
            <span class="tab-label">Sản phẩm</span>
            <span class="tab-count-pill">{{ filteredProducts.length }}</span>
          </button>

          <button
            class="search-mode-tab-btn"
            :class="{ active: activeSearchTab === 'store' }"
            @click="setSearchTab('store')"
          >
            <i class="bi bi-shop"></i>
            <span class="tab-label">Gian hàng & Quán</span>
            <span class="tab-count-pill">{{ filteredStores.length }}</span>
          </button>
        </div>

        <!-- Thẻ hiển thị từ khóa / bộ lọc đang kích hoạt -->
        <div class="active-query-meta" v-if="searchQuery || selectedStoreFilter">
          <span v-if="searchQuery" class="active-query-tag">
            <span>Từ khóa: "<strong>{{ searchQuery }}</strong>"</span>
            <button class="btn-remove-tag" @click="searchQuery = ''" title="Bỏ từ khóa"><i class="bi bi-x"></i></button>
          </span>
          <span v-if="selectedStoreFilter" class="active-query-tag store-tag">
            <span>Gian hàng: <strong>{{ selectedStoreFilter }}</strong></span>
            <button class="btn-remove-tag" @click="clearStoreFilter" title="Xem tất cả gian hàng"><i class="bi bi-x"></i></button>
          </span>
        </div>
      </section>

      <!-- BANNER TIÊU ĐIỂM KHI ĐANG XEM SẢN PHẨM CỦA MỘT GIAN HÀNG CỤ THỂ -->
      <transition name="fade">
        <div v-if="selectedStoreFilter && activeStoreInfo" class="store-spotlight-card">
          <div class="store-spotlight-left">
            <img :src="activeStoreInfo.avatar" :alt="activeStoreInfo.name" class="store-spotlight-avatar" />
            <div class="store-spotlight-info">
              <div class="store-spotlight-badge-row">
                <span class="spotlight-tag"><i class="bi bi-shop"></i> GIAN HÀNG ĐANG CHỌN</span>
                <span v-if="activeStoreInfo.isVerified" class="spotlight-verified"><i class="bi bi-patch-check-fill"></i> Đã xác thực ATTP</span>
              </div>
              <h2 class="store-spotlight-title">{{ activeStoreInfo.name }}</h2>
              <div class="store-spotlight-sub">
                <span><i class="bi bi-geo-alt-fill text-orange"></i> {{ activeStoreInfo.address }}</span>
                <span class="dot-sep">•</span>
                <span><i class="bi bi-lightning-charge-fill text-orange"></i> Giao {{ activeStoreInfo.deliveryTime }}</span>
                <span class="dot-sep">•</span>
                <span>⭐ {{ activeStoreInfo.rating.toFixed(1) }} ({{ activeStoreInfo.reviewsCount || 48 }} đánh giá)</span>
              </div>
            </div>
          </div>
          <button class="btn-exit-store-filter" @click="clearStoreFilter">
            <i class="bi bi-x-circle-fill"></i>
            <span>Xem tất cả gian hàng</span>
          </button>
        </div>
      </transition>

      <!-- 3. BỘ LỌC DANH MỤC & ĐIỀU KHIỂN SẮP XẾP (FRAMELESS BAR) -->
      <section class="filter-controls-bar">
        <!-- Category Pills -->
        <div class="category-pills-row">
        <!-- Category Pills (Chỉ hiện khi ở tab Sản Phẩm) -->
        <div v-if="activeSearchTab === 'product'" class="category-pills-row">
          <button
            v-for="cat in categories"
            :key="cat.id"
            :class="['cat-pill-btn', { active: selectedCategory === cat.id }]"
            @click="selectedCategory = cat.id"
          >
            <i class="bi cat-pill-icon" :class="cat.icon" aria-hidden="true"></i>
            <span class="cat-pill-name">{{ cat.name }}</span>
            <span class="cat-pill-count">{{ cat.count }}</span>
          </button>
        </div>
        <div v-else class="store-filter-intro">
          <span class="store-intro-text"><i class="bi bi-geo-alt-fill text-orange me-1"></i> Các gian hàng uy tín phục vụ giao siêu tốc trong <strong>{{ maxRadiusKm }}km</strong></span>
        </div>

        <!-- Sort controls -->
        <div class="sort-action-group">
          <label class="sort-label" for="sort-select">Sắp xếp:</label>
          <select id="sort-select" v-model="sortBy" class="sort-dropdown-select" aria-label="Sắp xếp sản phẩm">
            <option value="popular">Bán chạy nhất</option>
            <option value="distance">Gần bạn nhất</option>
            <option value="rating">Đánh giá cao nhất</option>
            <option value="price-asc">Giá: Thấp đến cao</option>
            <option value="price-desc">Giá: Cao đến thấp</option>
          <select id="sort-select" v-model="sortBy" class="sort-dropdown-select" aria-label="Sắp xếp">
            <template v-if="activeSearchTab === 'product'">
              <option value="popular">Bán chạy nhất</option>
              <option value="distance">Gần bạn nhất</option>
              <option value="rating">Đánh giá cao nhất</option>
              <option value="price-asc">Giá: Thấp đến cao</option>
              <option value="price-desc">Giá: Cao đến thấp</option>
            </template>
            <template v-else>
              <option value="popular">Nhiều món & Uy tín nhất</option>
              <option value="distance">Gần bạn nhất</option>
              <option value="rating">Đánh giá cao nhất</option>
            </template>
          </select>
        </div>
      </section>

      <!-- 3. KẾT QUẢ ĐANG HIỂN THỊ -->
      <!-- 4. KẾT QUẢ ĐANG HIỂN THỊ -->
      <div class="filter-meta-status">
        <span class="found-text">
        <span v-if="activeSearchTab === 'product'" class="found-text">
          Tìm thấy <strong>{{ filteredProducts.length }}</strong> sản phẩm phù hợp trong bán kính <strong>{{ maxRadiusKm }}km</strong>
        </span>
        <span v-else class="found-text">
          Tìm thấy <strong>{{ filteredStores.length }}</strong> gian hàng uy tín trong bán kính <strong>{{ maxRadiusKm }}km</strong>
        </span>

        <button
          v-if="selectedCategory !== 'all' || searchQuery || maxRadiusKm < 10 || sortBy !== 'popular'"
          v-if="(activeSearchTab === 'product' && selectedCategory !== 'all') || searchQuery || selectedStoreFilter || maxRadiusKm < 10 || sortBy !== 'popular'"
          class="btn-reset-filter"
          @click="resetFilters"
        >
          <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
          <span>Đặt lại bộ lọc</span>
        </button>
      </div>

      <!-- 4. LƯỚI SẢN PHẨM KHÔNG KHUNG VIỀN (TASTE-SKILL PRODUCT GRID) -->
      <div v-if="filteredProducts.length > 0" class="products-tactile-grid">
        <div
          v-for="p in filteredProducts"
          :key="p.id"
          class="product-tactile-card"
          @click="goToDetail(p.id)"
        >
          <!-- Media Wrapper -->
          <div class="product-media-wrap">
            <img :src="p.image" :alt="p.name" class="product-thumb-photo" loading="lazy" />
      <!-- 5A. TAB SẢN PHẨM: LƯỚI SẢN PHẨM KHÔNG KHUNG VIỀN (TASTE-SKILL PRODUCT GRID) -->
      <div v-if="activeSearchTab === 'product'">
        <div v-if="filteredProducts.length > 0" class="products-tactile-grid">
          <div
            v-for="p in filteredProducts"
            :key="p.id"
            class="product-tactile-card"
            @click="goToDetail(p.id)"
          >
            <!-- Media Wrapper -->
            <div class="product-media-wrap">
              <img :src="p.image" :alt="p.name" class="product-thumb-photo" loading="lazy" />

            <!-- Live Distance Chip -->
            <div class="badge-distance-live">
              <span class="pulse-dot"></span>
              <span>{{ p.distanceKm }} km • {{ p.deliveryTime }}</span>
            </div>
              <!-- Live Distance Chip -->
              <div class="badge-distance-live">
                <span class="pulse-dot"></span>
                <span>{{ p.distanceKm }} km • {{ p.deliveryTime }}</span>
              </div>

            <!-- Discount Badge -->
            <span v-if="p.discountBadge" class="badge-discount-ribbon">
              {{ p.discountBadge }}
            </span>
              <!-- Discount Badge -->
              <span v-if="p.discountBadge" class="badge-discount-ribbon">
                {{ p.discountBadge }}
              </span>

            <!-- Feature Tag -->
            <span v-if="p.badge" class="badge-fresh-tag">
              {{ p.badge }}
            </span>
          </div>

          <!-- Card Body -->
          <div class="product-card-body">
            <div class="store-info-line">
              <span class="store-name-text">
                <i class="bi bi-shop store-inline-icon" aria-hidden="true"></i>
                {{ p.storeName }}
              <!-- Feature Tag -->
              <span v-if="p.badge" class="badge-fresh-tag">
                {{ p.badge }}
              </span>
              <span class="category-name-chip">{{ p.categoryName }}</span>
            </div>

            <h3 class="product-item-title" :title="p.name">
              {{ p.name }}
            </h3>
            <!-- Card Body -->
            <div class="product-card-body">
              <div class="store-info-line">
                <span class="store-name-text" :title="p.storeName" @click.stop="selectStore(p.storeName)">
                  <i class="bi bi-shop store-inline-icon" aria-hidden="true"></i>
                  {{ p.storeName }}
                </span>
                <span class="category-name-chip">{{ p.categoryName }}</span>
              </div>

            <div class="product-rating-meta">
              <span class="star-rating">
                <i class="bi bi-star-fill star-icon" aria-hidden="true"></i>
                {{ p.rating.toFixed(1) }}
              </span>
              <span class="sold-stat">Đã bán {{ p.sold }}</span>
              <span class="unit-stat">• {{ p.unit }}</span>
            </div>
              <h3 class="product-item-title" :title="p.name">
                {{ p.name }}
              </h3>

            <div class="product-price-bottom-row">
              <div class="pricing-wrap">
                <span class="price-val">{{ p.price.toLocaleString("vi-VN") }} ₫</span>
                <span v-if="p.oldPrice" class="old-price-val">
                  {{ p.oldPrice.toLocaleString("vi-VN") }} ₫
              <div class="product-rating-meta">
                <span class="star-rating">
                  <i class="bi bi-star-fill star-icon" aria-hidden="true"></i>
                  {{ p.rating.toFixed(1) }}
                </span>
                <span class="sold-stat">Đã bán {{ p.sold }}</span>
                <span class="unit-stat">• {{ p.unit }}</span>
              </div>

              <button
                class="btn-add-tactile"
                title="Thêm vào giỏ hàng"
                aria-label="Thêm vào giỏ hàng"
                @click.stop="onAddToCart(p)"
              >
                <i class="bi bi-bag-plus-fill" aria-hidden="true"></i>
                <span>Thêm</span>
              </button>
              <div class="product-price-bottom-row">
                <div class="pricing-wrap">
                  <span class="price-val">{{ p.price.toLocaleString("vi-VN") }} ₫</span>
                  <span v-if="p.oldPrice" class="old-price-val">
                    {{ p.oldPrice.toLocaleString("vi-VN") }} ₫
                  </span>
                </div>

                <button
                  class="btn-add-tactile"
                  title="Thêm vào giỏ hàng"
                  aria-label="Thêm vào giỏ hàng"
                  @click.stop="onAddToCart(p)"
                >
                  <i class="bi bi-bag-plus-fill" aria-hidden="true"></i>
                  <span>Thêm</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty State khi không tìm thấy sản phẩm -->
        <div v-else class="empty-results-box">
          <div class="empty-icon-circle">
            <i class="bi bi-box-seam" aria-hidden="true"></i>
          </div>
          <h3>Không tìm thấy sản phẩm nào!</h3>
          <p>
            Không có sản phẩm nào khớp với tìm kiếm "<strong>{{ searchQuery }}</strong>" hoặc trong bán kính <strong>{{ maxRadiusKm }}km</strong>.
          </p>
          <!-- Gợi ý xem gian hàng nếu có gian hàng khớp -->
          <div v-if="filteredStores.length > 0" class="empty-cross-action-box">
            <p class="cross-hint-msg">
              <i class="bi bi-info-circle-fill text-orange me-1"></i> Có <strong>{{ filteredStores.length }}</strong> gian hàng liên quan đến từ khóa này!
            </p>
            <button class="btn-switch-cross-tab" @click="setSearchTab('store')">
              <i class="bi bi-shop me-1"></i>
              <span>Xem các gian hàng ngay</span>
              <i class="bi bi-arrow-right ms-1"></i>
            </button>
          </div>
          <button v-else class="btn-restore-filters" @click="resetFilters">
            <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
            <span>Xem tất cả sản phẩm</span>
          </button>
        </div>
      </div>

      <!-- Empty State khi không tìm thấy món -->
      <div v-else class="empty-results-box">
        <div class="empty-icon-circle">
          <i class="bi bi-search" aria-hidden="true"></i>
      <!-- 5B. TAB GIAN HÀNG: LƯỚI GIAN HÀNG TẬN NƠI (TASTE-SKILL STORE CARDS) -->
      <div v-else>
        <div v-if="filteredStores.length > 0" class="stores-tactile-grid">
          <div
            v-for="store in filteredStores"
            :key="store.id"
            class="store-tactile-card"
            @click="selectStore(store.name)"
          >
            <!-- Store Media / Cover Banner -->
            <div class="store-card-media">
              <img :src="store.coverImage" :alt="store.name" class="store-cover-photo" loading="lazy" />
              <div class="store-dist-badge">
                <span class="pulse-dot"></span>
                <span>{{ store.distanceKm }} km • {{ store.deliveryTime }}</span>
              </div>
              <div class="store-category-tag">
                {{ store.categoryName || 'Bách hóa & Ẩm thực' }}
              </div>
            </div>

            <!-- Store Info Body -->
            <div class="store-card-body">
              <div class="store-avatar-heading-row">
                <img :src="store.avatar" :alt="store.name" class="store-profile-avatar" />
                <div class="store-titles">
                  <h3 class="store-name-heading" :title="store.name">
                    {{ store.name }}
                    <i v-if="store.isVerified" class="bi bi-patch-check-fill verified-icon" title="Gian hàng chính thức ZoneMart"></i>
                  </h3>
                  <div class="store-rating-row">
                    <span class="store-star-rating">
                      <i class="bi bi-star-fill"></i>
                      <strong>{{ store.rating.toFixed(1) }}</strong>
                    </span>
                    <span class="store-reviews-text">({{ store.reviewsCount || 48 }} đánh giá)</span>
                    <span class="store-open-time"><i class="bi bi-clock"></i> {{ store.openHours }}</span>
                  </div>
                </div>
              </div>

              <div class="store-address-line">
                <i class="bi bi-geo-alt-fill store-pin-icon"></i>
                <span>{{ store.address }}</span>
              </div>

              <!-- Preview món nổi bật của gian hàng -->
              <div class="store-items-preview-box" v-if="store.products.length > 0">
                <div class="preview-box-header">
                  <span>Món nổi bật tại quán ({{ store.products.length }} món):</span>
                </div>
                <div class="preview-items-row">
                  <div
                    v-for="item in store.products.slice(0, 3)"
                    :key="item.id"
                    class="preview-item-chip"
                    :title="item.name"
                    @click.stop="goToDetail(item.id)"
                  >
                    <img :src="item.image" :alt="item.name" class="preview-item-img" />
                    <div class="preview-item-details">
                      <span class="preview-item-name">{{ item.name }}</span>
                      <span class="preview-item-price">{{ item.price.toLocaleString('vi-VN') }} ₫</span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Card Action Button -->
              <div class="store-card-action-row">
                <button class="btn-explore-store" @click.stop="selectStore(store.name)">
                  <i class="bi bi-shop-window"></i>
                  <span>Ghé thăm gian hàng ({{ store.products.length }} món)</span>
                  <i class="bi bi-arrow-right arrow-anim"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
        <h3>Không tìm thấy sản phẩm nào!</h3>
        <p>
          Không có sản phẩm nào khớp với tìm kiếm "<strong>{{ searchQuery }}</strong>" hoặc trong bán kính <strong>{{ maxRadiusKm }}km</strong>.
        </p>
        <button class="btn-restore-filters" @click="resetFilters">
          <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
          <span>Xem tất cả sản phẩm</span>
        </button>

        <!-- Empty State khi không tìm thấy gian hàng -->
        <div v-else class="empty-results-box">
          <div class="empty-icon-circle">
            <i class="bi bi-shop" aria-hidden="true"></i>
          </div>
          <h3>Không tìm thấy gian hàng nào!</h3>
          <p>
            Không có gian hàng nào khớp với tìm kiếm "<strong>{{ searchQuery }}</strong>" hoặc trong bán kính <strong>{{ maxRadiusKm }}km</strong>.
          </p>
          <!-- Gợi ý xem sản phẩm nếu có sản phẩm khớp -->
          <div v-if="filteredProducts.length > 0" class="empty-cross-action-box">
            <p class="cross-hint-msg">
              <i class="bi bi-info-circle-fill text-orange me-1"></i> Có <strong>{{ filteredProducts.length }}</strong> sản phẩm liên quan đến từ khóa này!
            </p>
            <button class="btn-switch-cross-tab" @click="setSearchTab('product')">
              <i class="bi bi-box-seam-fill me-1"></i>
              <span>Xem các sản phẩm ngay</span>
              <i class="bi bi-arrow-right ms-1"></i>
            </button>
          </div>
          <button v-else class="btn-restore-filters" @click="resetFilters">
            <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
            <span>Xem tất cả gian hàng</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ==========================================================================
   TASTE-SKILL: FRAMELESS, TACTILE, BENTO-INSPIRED CATALOG
   ========================================================================== */
.product-catalog-page {
  width: 100%;
  min-height: 100%;
  background-color: #f8fafc;
  color: #0f172a;
  padding: 32px 24px 80px 24px;
}

.catalog-container {
  max-width: 1340px;
  margin: 0 auto;
}

/* Toast Notification */
.toast-floating-chip {
  position: fixed;
  top: 85px;
  right: 25px;
  background-color: #0f172a;
  color: #ffffff;
  padding: 12px 22px;
  border-radius: 14px;
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.2);
  z-index: 9999;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  font-weight: 700;
  border: none;
}
.toast-check {
  color: #22c55e;
  font-weight: 900;
}
.toast-fade-enter-active, .toast-fade-leave-active {
  transition: all 0.3s ease;
}
.toast-fade-enter-from, .toast-fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

/* ==========================================================================
   1. BENTO SPOTLIGHT (ASYMMETRICAL 65/35)
   ========================================================================== */
.bento-spotlight-section {
  display: grid;
  grid-template-columns: 1.6fr 1fr;
  gap: 24px;
  margin-bottom: 36px;
}

.bento-card-main {
  background: #ffffff;
  border: none;
  border-radius: 28px;
  padding: 36px;
  display: grid;
  grid-template-columns: 1.2fr 0.8fr;
  gap: 24px;
  align-items: center;
  box-shadow: 0 4px 25px -3px rgba(0, 0, 0, 0.04);
}

.live-gps-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #eff6ff;
  color: #2563eb;
  padding: 5px 14px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.5px;
  margin-bottom: 14px;
}

.gps-dot {
  width: 8px;
  height: 8px;
  background-color: #2563eb;
  border-radius: 50%;
  animation: pulse-ring 1.8s infinite;
}

@keyframes pulse-ring {
  0% { transform: scale(0.9); opacity: 1; }
  50% { transform: scale(1.4); opacity: 0.5; }
  100% { transform: scale(0.9); opacity: 1; }
}

.spotlight-title {
  margin: 0 0 12px 0;
  font-size: 32px;
  font-weight: 900;
  line-height: 1.2;
  letter-spacing: -0.6px;
  color: #0f172a;
}

.text-gradient-warm {
  background: linear-gradient(90deg, #ea580c 0%, #f97316 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.spotlight-desc {
  font-size: 13px;
  line-height: 1.6;
  color: #64748b;
  margin: 0 0 22px 0;
}

.spotlight-search-bar {
  display: flex;
  align-items: center;
  background: #f1f5f9;
  border-radius: 16px;
  padding: 10px 16px;
  border: none;
  background: #ffffff;
  border-radius: 18px;
  padding: 6px 16px 6px 8px;
  border: 1.5px solid #e2e8f0;
  position: relative;
  transition: all 0.25s ease;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.03);
}

.spotlight-search-bar:focus-within {
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}

.spotlight-mode-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #fff7ed;
  border: 1px solid #fed7aa;
  border-radius: 12px;
  padding: 6px 12px;
  font-size: 12px;
  font-weight: 800;
  color: #ea580c;
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.spotlight-mode-pill:hover {
  background: #ffedd5;
  transform: translateY(-1px);
}

.spotlight-mode-pill .switch-icon {
  font-size: 10px;
  opacity: 0.6;
}

.spotlight-search-divider {
  width: 1px;
  height: 20px;
  background: #cbd5e1;
  margin: 0 10px 0 8px;
  flex-shrink: 0;
}

.lens-icon {
  font-size: 16px;
  color: #64748b;
  margin-right: 10px;
}

.spotlight-input {
  flex: 1;
  border: none;
  background: transparent;
  outline: none;
  font-size: 13px;
  color: #0f172a;
}

.spotlight-input::placeholder {
  color: #94a3b8;
}

.btn-clear-search {
  background: none;
  border: none;
  font-size: 14px;
  color: #94a3b8;
  cursor: pointer;
}

.bento-visual-side {
  position: relative;
  height: 220px;
}

.bento-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 20px;
  border: none;
}

.store-floating-tag {
  position: absolute;
  bottom: 14px;
  left: 14px;
  background: rgba(15, 23, 42, 0.9);
  backdrop-filter: blur(10px);
  color: #ffffff;
  padding: 8px 14px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 12px;
}
.store-floating-tag .icon {
  font-size: 18px;
}
.store-floating-tag strong {
  display: block;
}
.store-floating-tag small {
  color: #94a3b8;
}

/* Sub bento card */
.bento-card-sub {
  background: linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%);
  border: none;
  border-radius: 28px;
  padding: 32px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.sub-badge-speed {
  display: inline-block;
  align-self: flex-start;
  font-size: 11px;
  font-weight: 800;
  color: #ea580c;
  background: #ffffff;
  padding: 4px 12px;
  border-radius: 12px;
  margin-bottom: 12px;
}

.sub-card-title {
  margin: 0 0 8px 0;
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
}

.sub-card-desc {
  font-size: 13px;
  line-height: 1.5;
  color: #475569;
  margin: 0 0 20px 0;
}

/* Radius Slider Box */
.radius-slider-box {
  background: #ffffff;
  padding: 16px;
  border-radius: 18px;
  border: none;
}

.radius-slider-header {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  font-weight: 700;
  margin-bottom: 10px;
}

.slider-title {
  color: #475569;
}

.slider-value {
  color: #ea580c;
  font-weight: 800;
}

.range-slider {
  width: 100%;
  accent-color: #ea580c;
  cursor: pointer;
}

.slider-ticks {
  display: flex;
  justify-content: space-between;
  font-size: 10px;
  color: #94a3b8;
  margin-top: 6px;
  font-weight: 600;
}

/* ==========================================================================
   TABS CHUYỂN ĐỔI CHẾ ĐỘ TÌM KIẾM: SẢN PHẨM / GIAN HÀNG
   ========================================================================== */
.search-mode-nav-section {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-bottom: 24px;
}

.search-mode-tabs-wrap {
  display: inline-flex;
  background: #ffffff;
  padding: 6px;
  border-radius: 20px;
  border: 1.5px solid #e2e8f0;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.03);
  gap: 6px;
}

.search-mode-tab-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  background: transparent;
  padding: 10px 22px;
  border-radius: 14px;
  font-size: 14px;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  transition: all 0.2s ease;
}

.search-mode-tab-btn:hover {
  color: #ea580c;
  background: #fff7ed;
}

.search-mode-tab-btn.active {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.3);
}

.tab-count-pill {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 20px;
  background: #f1f5f9;
  color: #64748b;
  font-weight: 800;
}

.search-mode-tab-btn.active .tab-count-pill {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
}

.active-query-meta {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.active-query-tag {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #fff7ed;
  color: #c2410c;
  border: 1px solid #ffedd5;
  padding: 6px 14px;
  border-radius: 50px;
  font-size: 12.5px;
  font-weight: 600;
}

.active-query-tag.store-tag {
  background: #eff6ff;
  color: #1d4ed8;
  border-color: #dbeafe;
}

.btn-remove-tag {
  border: none;
  background: rgba(0, 0, 0, 0.08);
  color: inherit;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  transition: all 0.15s;
}

.btn-remove-tag:hover {
  background: rgba(0, 0, 0, 0.2);
}

/* BANNER TIÊU ĐIỂM GIAN HÀNG ĐANG CHỌN */
.store-spotlight-card {
  background: #ffffff;
  border: 1.5px solid #fed7aa;
  border-radius: 22px;
  padding: 18px 24px;
  margin-bottom: 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 20px;
  box-shadow: 0 6px 22px rgba(234, 88, 12, 0.08);
  flex-wrap: wrap;
}

.store-spotlight-left {
  display: flex;
  align-items: center;
  gap: 18px;
}

.store-spotlight-avatar {
  width: 58px;
  height: 58px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #fed7aa;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.store-spotlight-badge-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 4px;
}

.spotlight-tag {
  font-size: 10.5px;
  font-weight: 800;
  letter-spacing: 0.5px;
  background: #ffedd5;
  color: #ea580c;
  padding: 3px 10px;
  border-radius: 20px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.spotlight-verified {
  font-size: 11px;
  color: #16a34a;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.store-spotlight-title {
  margin: 0 0 6px 0;
  font-size: 20px;
  font-weight: 900;
  color: #0f172a;
}

.store-spotlight-sub {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #64748b;
  flex-wrap: wrap;
}

.text-orange {
  color: #ea580c;
}

.dot-sep {
  color: #cbd5e1;
}

.btn-exit-store-filter {
  background: #f1f5f9;
  color: #475569;
  border: 1px solid #e2e8f0;
  padding: 10px 18px;
  border-radius: 12px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s;
}

.btn-exit-store-filter:hover {
  background: #fee2e2;
  color: #dc2626;
  border-color: #fca5a5;
}

.store-filter-intro {
  font-size: 14px;
  color: #475569;
  font-weight: 600;
}

/* ==========================================================================
   2. FILTER CONTROLS BAR (FRAMELESS)
   ========================================================================== */
.filter-controls-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 20px;
}

.category-pills-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.cat-pill-btn {
  background: #ffffff;
  border: none;
  border-radius: 14px;
  padding: 8px 16px;
  font-size: 13px;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.03);
  transition: all 0.2s ease;
}

.cat-pill-btn:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.cat-pill-btn.active {
  background: #ea580c;
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.25);
}

.cat-pill-count {
  font-size: 11px;
  background: rgba(0, 0, 0, 0.08);
  padding: 2px 6px;
  border-radius: 10px;
}

.cat-pill-btn.active .cat-pill-count {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
}

.sort-action-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.sort-label {
  font-size: 13px;
  font-weight: 600;
  color: #64748b;
}

.sort-dropdown-select {
  background: #ffffff;
  border: none;
  padding: 8px 14px;
  border-radius: 12px;
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
  outline: none;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.03);
}

/* Meta status line */
.filter-meta-status {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 22px;
  font-size: 13px;
  color: #64748b;
}

.btn-reset-filter {
  background: none;
  border: none;
  color: #ea580c;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}

/* ==========================================================================
   3. TACTILE PRODUCT GRID (NO BORDERS)
   ========================================================================== */
.products-tactile-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 22px;
}

.product-tactile-card {
  background: #ffffff;
  border: none;
  border-radius: 22px;
  overflow: hidden;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.04);
  transition: all 0.25s ease;
}

.product-tactile-card:hover {
  transform: translateY(-6px);
  box-shadow: 0 16px 30px -4px rgba(0, 0, 0, 0.09);
}

.product-media-wrap {
  position: relative;
  height: 180px;
  overflow: hidden;
}

.product-thumb-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border: none;
  transition: transform 0.3s ease;
}

.product-tactile-card:hover .product-thumb-photo {
  transform: scale(1.06);
}

/* Distance Chip with live glowing pulse */
.badge-distance-live {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(15, 23, 42, 0.88);
  backdrop-filter: blur(8px);
  color: #ffffff;
  padding: 4px 10px;
  border-radius: 14px;
  font-size: 11px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 6px;
}

.pulse-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #22c55e;
  box-shadow: 0 0 6px #22c55e;
}

.badge-discount-ribbon {
  position: absolute;
  top: 10px;
  right: 10px;
  background: #ea580c;
  color: #ffffff;
  padding: 4px 8px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 800;
}

.badge-fresh-tag {
  position: absolute;
  bottom: 10px;
  left: 10px;
  background: rgba(22, 163, 74, 0.9);
  color: #ffffff;
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 10px;
  font-weight: 700;
}

/* Card Body */
.product-card-body {
  padding: 16px;
  display: flex;
  flex-direction: column;
  flex: 1;
}

.store-info-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 11px;
  color: #64748b;
  margin-bottom: 6px;
}

.category-name-chip {
  background: #f1f5f9;
  padding: 2px 7px;
  border-radius: 6px;
  font-weight: 600;
}

.product-item-title {
  margin: 0 0 8px 0;
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
  height: 38px;
  overflow: hidden;
}

.product-rating-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
  color: #94a3b8;
  margin-bottom: 14px;
}

.star-rating {
  font-weight: 700;
  color: #eab308;
}

.product-price-bottom-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-top: auto;
}

.pricing-wrap {
  display: flex;
  flex-direction: column;
}

.price-val {
  font-size: 17px;
  font-weight: 900;
  color: #ea580c;
  letter-spacing: -0.3px;
}

.old-price-val {
  font-size: 11px;
  color: #94a3b8;
  text-decoration: line-through;
}

.store-inline-icon {
  font-size: 12px;
  color: #ea580c;
  margin-right: 3px;
}

.star-icon {
  color: #f59e0b;
  font-size: 11px;
  margin-right: 2px;
}

.btn-add-tactile {
  background: #0f172a;
  color: #ffffff;
  border: none;
  padding: 7px 14px;
  border-radius: 10px;
  font-size: 12px;
  font-weight: 800;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 5px;
  transition: all 0.2s ease;
}

.btn-add-tactile:hover {
  background: #ea580c;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.3);
}

.btn-reset-filter {
  background: none;
  border: none;
  color: #ea580c;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.btn-restore-filters {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

/* Empty State */
.empty-results-box {
  background: #ffffff;
  border-radius: 28px;
  padding: 60px 24px;
  text-align: center;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.03);
}

.empty-icon-circle {
  width: 64px;
  height: 64px;
  background: #f1f5f9;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  margin: 0 auto 16px auto;
}

.empty-results-box h3 {
  margin: 0 0 8px 0;
  font-size: 20px;
  font-weight: 800;
}

.empty-results-box p {
  color: #64748b;
  font-size: 14px;
  margin: 0 0 20px 0;
}

.btn-restore-filters {
  background: #ea580c;
  color: #ffffff;
  border: none;
  padding: 10px 22px;
  border-radius: 12px;
  font-weight: 700;
  cursor: pointer;
}

/* ==========================================================================
   LƯỚI GIAN HÀNG TẬN NƠI (STORES TACTILE GRID)
   ========================================================================== */
.stores-tactile-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 24px;
}

.store-tactile-card {
  background: #ffffff;
  border-radius: 22px;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.03);
  cursor: pointer;
  transition: all 0.25s ease;
  display: flex;
  flex-direction: column;
}

.store-tactile-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.08);
  border-color: #fed7aa;
}

.store-card-media {
  height: 140px;
  position: relative;
  overflow: hidden;
}

.store-cover-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s ease;
}

.store-tactile-card:hover .store-cover-photo {
  transform: scale(1.05);
}

.store-dist-badge {
  position: absolute;
  bottom: 12px;
  left: 12px;
  background: rgba(15, 23, 42, 0.82);
  backdrop-filter: blur(6px);
  color: #ffffff;
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 6px;
}

.store-category-tag {
  position: absolute;
  top: 12px;
  right: 12px;
  background: #ffffff;
  color: #ea580c;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 800;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
}

.store-card-body {
  padding: 18px 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  flex: 1;
}

.store-avatar-heading-row {
  display: flex;
  align-items: flex-end;
  gap: 14px;
}

.store-profile-avatar {
  width: 54px;
  height: 54px;
  border-radius: 50%;
  object-fit: cover;
  border: 3px solid #ffffff;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
  margin-top: -34px;
  position: relative;
  z-index: 2;
  background: #ffffff;
  flex-shrink: 0;
}

.store-titles {
  flex: 1;
  min-width: 0;
}

.store-name-heading {
  margin: 0;
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.verified-icon {
  color: #16a34a;
  font-size: 14px;
  flex-shrink: 0;
}

.store-rating-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #64748b;
  margin-top: 2px;
}

.store-star-rating {
  color: #f59e0b;
  display: flex;
  align-items: center;
  gap: 4px;
}

.store-reviews-text {
  color: #94a3b8;
}

.store-open-time {
  font-size: 11px;
  color: #059669;
  font-weight: 600;
  margin-left: auto;
}

.store-address-line {
  font-size: 12.5px;
  color: #64748b;
  display: flex;
  align-items: flex-start;
  gap: 6px;
  line-height: 1.4;
}

.store-pin-icon {
  color: #ea580c;
  font-size: 13px;
  flex-shrink: 0;
  margin-top: 1px;
}

.store-items-preview-box {
  background: #f8fafc;
  border-radius: 14px;
  padding: 10px 12px;
  border: 1px solid #f1f5f9;
}

.preview-box-header {
  font-size: 11px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  margin-bottom: 6px;
}

.preview-items-row {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding-bottom: 2px;
}

.preview-item-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 4px 8px;
  flex-shrink: 0;
  cursor: pointer;
  transition: all 0.15s;
}

.preview-item-chip:hover {
  border-color: #ea580c;
  background: #fff7ed;
}

.preview-item-img {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  object-fit: cover;
}

.preview-item-details {
  display: flex;
  flex-direction: column;
}

.preview-item-name {
  font-size: 11.5px;
  font-weight: 700;
  color: #334155;
  max-width: 95px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.preview-item-price {
  font-size: 11px;
  font-weight: 800;
  color: #ea580c;
}

.store-card-action-row {
  margin-top: auto;
}

.btn-explore-store {
  width: 100%;
  background: #fff7ed;
  color: #ea580c;
  border: 1.5px solid #fed7aa;
  padding: 11px;
  border-radius: 14px;
  font-size: 13px;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: all 0.2s;
}

.store-tactile-card:hover .btn-explore-store {
  background: #ea580c;
  color: #ffffff;
  border-color: #ea580c;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.25);
}

.store-tactile-card:hover .arrow-anim {
  transform: translateX(4px);
}

.arrow-anim {
  transition: transform 0.2s ease;
}

/* Gợi ý chuyển tab khi kết quả rỗng */
.empty-cross-action-box {
  margin-top: 16px;
  background: #fff7ed;
  border: 1.5px solid #fed7aa;
  padding: 16px 24px;
  border-radius: 18px;
  display: inline-flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.cross-hint-msg {
  margin: 0;
  font-size: 14px;
  color: #9a3412;
  font-weight: 700;
}

.btn-switch-cross-tab {
  background: #ea580c;
  color: #ffffff;
  border: none;
  padding: 10px 20px;
  border-radius: 12px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.25);
}

.btn-switch-cross-tab:hover {
  background: #c2410c;
  transform: translateY(-2px);
  box-shadow: 0 6px 18px rgba(234, 88, 12, 0.35);
}

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1200px) {
  .products-tactile-grid {
  .products-tactile-grid,
  .stores-tactile-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 960px) {
  .bento-spotlight-section {
    grid-template-columns: 1fr;
  }
  .products-tactile-grid {
  .products-tactile-grid,
  .stores-tactile-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 600px) {
  .bento-card-main {
    grid-template-columns: 1fr;
    padding: 24px;
  }
  .bento-visual-side {
    height: 160px;
  }
  .products-tactile-grid {
  .products-tactile-grid,
  .stores-tactile-grid {
    grid-template-columns: 1fr;
  }
  .filter-controls-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .sort-action-group {
    justify-content: space-between;
  }
  .store-spotlight-card {
    flex-direction: column;
    align-items: flex-start;
  }
  .btn-exit-store-filter {
    width: 100%;
    justify-content: center;
  }
}
</style>
