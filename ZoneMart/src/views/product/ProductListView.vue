<script setup lang="ts">
/**
 * ============================================================================
 * TRANG DANH SÁCH SẢN PHẨM (PRODUCT LIST VIEW) - ZONE MART
 * Thiết kế áp dụng Taste-Skill: Anti-Slop, Bento Asymmetry, Frameless, Tactile
 * Design Read: Hyper-local on-demand grocery & dining catalog with appetizing
 * editorial commerce language, warm terracotta tones, and tactile interactions.
 * ============================================================================
 */
import { ref, computed, watch, onMounted } from "vue";
import { useRouter, useRoute } from "vue-router";

const router = useRouter();
const route = useRoute();

// State lọc & tìm kiếm
const searchQuery = ref("");
const selectedCategory = ref("all");
const maxRadiusKm = ref(10);
const sortBy = ref("popular"); // 'popular' | 'distance' | 'price-asc' | 'price-desc' | 'rating'
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
  }
  if (route.query.cat) {
    selectedCategory.value = String(route.query.cat);
  }
});

watch(
  () => route.query,
  (newQuery) => {
    if (newQuery.q !== undefined) searchQuery.value = String(newQuery.q || "");
    if (newQuery.cat !== undefined) selectedCategory.value = String(newQuery.cat || "all");
  }
);

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

const allProducts = ref<ProductItem[]>([
  {
    id: "p1",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 185000,
    oldPrice: 220000,
    discountBadge: "-16%",
    storeName: "ZoneMart Cầu Giấy",
    distanceKm: 1.2,
    deliveryTime: "15 - 20 phút",
    rating: 5.0,
    reviews: 84,
    sold: 142,
    badge: "Bán chạy nhất",
    unit: "Khay 500g",
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p2",
    name: "Hộp Dâu Tây Đà Lạt Tươi Ngọt Chuẩn VietGAP",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 95000,
    oldPrice: 125000,
    discountBadge: "-24%",
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    deliveryTime: "20 - 25 phút",
    rating: 4.9,
    reviews: 128,
    sold: 218,
    badge: "Mới hái sáng nay",
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p3",
    name: "Combo Bánh Mì Chảo Nóng Hổi Kèm Pate & Xúc Xích",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 45000,
    oldPrice: 55000,
    discountBadge: "-18%",
    storeName: "Tiệm Bánh Mì Zone",
    distanceKm: 2.8,
    deliveryTime: "15 - 20 phút",
    rating: 4.8,
    reviews: 210,
    sold: 340,
    badge: "Giao nóng giòn",
    unit: "Phần 1 người",
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p4",
    name: "Nước Ép Cam Sành Tươi Nguyên Chất 100%",
    category: "beverage",
    categoryName: "Đồ uống",
    price: 32000,
    oldPrice: 40000,
    discountBadge: "-20%",
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    deliveryTime: "15 - 20 phút",
    rating: 4.9,
    reviews: 95,
    sold: 180,
    badge: "Vắt tươi trực tiếp",
    unit: "Chai 350ml",
    image: "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p5",
    name: "Bộ Nồi Inox 3 Đáy Cao Cấp Nấu Bếp Từ",
    category: "household",
    categoryName: "Đồ gia dụng",
    price: 420000,
    oldPrice: 520000,
    discountBadge: "-19%",
    storeName: "Tổng Kho Gia Dụng Mỹ Đình",
    distanceKm: 5.4,
    deliveryTime: "30 - 35 phút",
    rating: 4.7,
    reviews: 42,
    sold: 65,
    unit: "Bộ 3 nồi",
    image: "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p6",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
    category: "food",
    categoryName: "Nhu yếu phẩm",
    price: 190000,
    oldPrice: 225000,
    discountBadge: "-15%",
    storeName: "ZoneMart Cầu Giấy",
    distanceKm: 1.2,
    deliveryTime: "15 - 20 phút",
    rating: 5.0,
    reviews: 64,
    sold: 96,
    badge: "Gạo ngon thế giới",
    unit: "Túi 5kg",
    image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p7",
    name: "Rau Xà Lách Xoăn Thủy Canh Hữu Cơ Đà Lạt",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 25000,
    oldPrice: 32000,
    discountBadge: "-22%",
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    deliveryTime: "20 - 25 phút",
    rating: 4.9,
    reviews: 58,
    sold: 110,
    unit: "Gói 300g",
    image: "https://images.unsplash.com/photo-1550411294-b3b1bf5bece1?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p8",
    name: "Cá Hồi Na Uy Tươi Phi Lê Cắt Miếng",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 245000,
    oldPrice: 280000,
    discountBadge: "-12%",
    storeName: "ZoneMart Cầu Giấy",
    distanceKm: 1.2,
    deliveryTime: "15 - 20 phút",
    rating: 5.0,
    reviews: 76,
    sold: 88,
    badge: "Tươi sống bảo quản lạnh",
    unit: "Khay 300g",
    image: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p9",
    name: "Cà Chua Bi Socola Ngọt Đậm Vị VietGAP",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 35000,
    oldPrice: 45000,
    discountBadge: "-22%",
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    deliveryTime: "20 - 25 phút",
    rating: 4.8,
    reviews: 90,
    sold: 165,
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p10",
    name: "Cơm Tấm Sườn Bì Chả Đặc Biệt Nóng Hổi",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 52000,
    oldPrice: 60000,
    discountBadge: "-13%",
    storeName: "Bếp Cơm Niêu & Cơm Tấm",
    distanceKm: 3.4,
    deliveryTime: "20 - 25 phút",
    rating: 4.9,
    reviews: 142,
    sold: 310,
    badge: "Kèm canh & nước mắm",
    unit: "Hộp 1 suất",
    image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p11",
    name: "Bơ Sáp 034 Đặc Sản Lâm Đồng Dẻo Béo",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 65000,
    oldPrice: 85000,
    discountBadge: "-23%",
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    deliveryTime: "20 - 25 phút",
    rating: 4.9,
    reviews: 67,
    sold: 130,
    unit: "Kg",
    image: "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=600&q=80"
  },
  {
    id: "p12",
    name: "Nước Rửa Bát Hữu Cơ Quế & Chanh Gừng 800ml",
    category: "household",
    categoryName: "Đồ dùng gia đình",
    price: 48000,
    oldPrice: 60000,
    discountBadge: "-20%",
    storeName: "Tổng Kho Gia Dụng Mỹ Đình",
    distanceKm: 5.4,
    deliveryTime: "30 - 35 phút",
    rating: 4.8,
    reviews: 35,
    sold: 95,
    unit: "Chai 800ml",
    image: "https://images.unsplash.com/photo-1585670270608-b404fb8802a6?auto=format&fit=crop&w=600&q=80"
  }
]);

// Lọc và sắp xếp theo điều kiện
const filteredProducts = computed(() => {
  let result = allProducts.value.filter((p) => {
    const matchCategory =
      selectedCategory.value === "all" || p.category === selectedCategory.value;
    const matchQuery =
      !searchQuery.value.trim() ||
      p.name.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      p.storeName.toLowerCase().includes(searchQuery.value.toLowerCase());
    const matchDistance = p.distanceKm <= maxRadiusKm.value;
    return matchCategory && matchQuery && matchDistance;
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

// Chuyển sang trang chi tiết
const goToDetail = (id: string) => {
  router.push(`/products/${id}`);
};

// Thêm vào giỏ hàng với Toast tương tác mượt mà
const onAddToCart = (name: string) => {
  showNotification(`Đã thêm "${name}" vào giỏ hàng!`);
};

// Reset bộ lọc
const resetFilters = () => {
  searchQuery.value = "";
  selectedCategory.value = "all";
  maxRadiusKm.value = 10;
  sortBy.value = "popular";
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
              <i class="bi bi-search lens-icon" aria-hidden="true"></i>
              <input
                v-model="searchQuery"
                type="text"
                placeholder="Tìm thịt tươi, rau sạch, bún chả, cơm tấm..."
                class="spotlight-input"
                aria-label="Tìm kiếm sản phẩm thực phẩm"
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
      <section class="filter-controls-bar">
        <!-- Category Pills -->
        <div class="category-pills-row">
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

        <!-- Sort controls -->
        <div class="sort-action-group">
          <label class="sort-label" for="sort-select">Sắp xếp:</label>
          <select id="sort-select" v-model="sortBy" class="sort-dropdown-select" aria-label="Sắp xếp sản phẩm">
            <option value="popular">Bán chạy nhất</option>
            <option value="distance">Gần bạn nhất</option>
            <option value="rating">Đánh giá cao nhất</option>
            <option value="price-asc">Giá: Thấp đến cao</option>
            <option value="price-desc">Giá: Cao đến thấp</option>
          </select>
        </div>
      </section>

      <!-- 3. KẾT QUẢ ĐANG HIỂN THỊ -->
      <div class="filter-meta-status">
        <span class="found-text">
          Tìm thấy <strong>{{ filteredProducts.length }}</strong> sản phẩm phù hợp trong bán kính <strong>{{ maxRadiusKm }}km</strong>
        </span>

        <button
          v-if="selectedCategory !== 'all' || searchQuery || maxRadiusKm < 10 || sortBy !== 'popular'"
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

            <!-- Live Distance Chip -->
            <div class="badge-distance-live">
              <span class="pulse-dot"></span>
              <span>{{ p.distanceKm }} km • {{ p.deliveryTime }}</span>
            </div>

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
              </span>
              <span class="category-name-chip">{{ p.categoryName }}</span>
            </div>

            <h3 class="product-item-title" :title="p.name">
              {{ p.name }}
            </h3>

            <div class="product-rating-meta">
              <span class="star-rating">
                <i class="bi bi-star-fill star-icon" aria-hidden="true"></i>
                {{ p.rating.toFixed(1) }}
              </span>
              <span class="sold-stat">Đã bán {{ p.sold }}</span>
              <span class="unit-stat">• {{ p.unit }}</span>
            </div>

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
                @click.stop="onAddToCart(p.name)"
              >
                <i class="bi bi-bag-plus-fill" aria-hidden="true"></i>
                <span>Thêm</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State khi không tìm thấy món -->
      <div v-else class="empty-results-box">
        <div class="empty-icon-circle">
          <i class="bi bi-search" aria-hidden="true"></i>
        </div>
        <h3>Không tìm thấy sản phẩm nào!</h3>
        <p>
          Không có sản phẩm nào khớp với tìm kiếm "<strong>{{ searchQuery }}</strong>" hoặc trong bán kính <strong>{{ maxRadiusKm }}km</strong>.
        </p>
        <button class="btn-restore-filters" @click="resetFilters">
          <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
          <span>Xem tất cả sản phẩm</span>
        </button>
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
  position: relative;
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
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1200px) {
  .products-tactile-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 960px) {
  .bento-spotlight-section {
    grid-template-columns: 1fr;
  }
  .products-tactile-grid {
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
    grid-template-columns: 1fr;
  }
  .filter-controls-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .sort-action-group {
    justify-content: space-between;
  }
}
</style>
