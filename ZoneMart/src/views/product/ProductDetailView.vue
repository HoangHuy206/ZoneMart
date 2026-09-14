<script setup lang="ts">
/**
 * ============================================================================
 * CHI TIẾT SẢN PHẨM CON (PRODUCT DETAIL VIEW) - ZONE MART
 * Thiết kế áp dụng Taste-Skill: Warm Terracotta (#ea580c), Bento Asymmetry,
 * Interactive Photo Gallery, Hyper-local Store Card, 4-Tab Deep Specs,
 * Real Buyer Reviews, Cross-sell 10km, and Tactile Micro-interactions.
 * ============================================================================
 */
import { ref, computed, watch, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useCart } from "../../composables/useCart";
import { useProductCatalog, type CatalogProduct } from "../../composables/useProductCatalog";

const route = useRoute();
const router = useRouter();
const cart = useCart();
const catalog = useProductCatalog();

// State quản lý sản phẩm
const currentId = computed(() => (route.params.id as string) || "p1");
const product = ref<CatalogProduct>(catalog.getProductById(currentId.value) || catalog.allProducts.value[0]);

// Gallery state
const selectedImage = ref(product.value.image);

// Theo dõi thay đổi route param (khi bấm sang sản phẩm liên quan)
watch(
  () => route.params.id,
  (newId) => {
    if (newId) {
      const found = catalog.getProductById(newId as string);
      if (found) {
        product.value = found;
        selectedImage.value = found.image;
        quantity.value = 1;
        window.scrollTo({ top: 0, behavior: "smooth" });
      }
    }
  }
);

onMounted(() => {
  const found = catalog.getProductById(currentId.value);
  if (found) {
    product.value = found;
    selectedImage.value = found.image;
    document.title = `${found.name} - ZoneMart Hỏa Tốc`;
  }
});

const selectImage = (img: string) => {
  selectedImage.value = img;
};

// Số lượng & Tùy chọn
const quantity = ref(1);
const selectedUnitVariant = ref(0);
const unitVariants = computed(() => [
  { label: product.value.unit, desc: "Quy cách chuẩn", priceDelta: 0 },
  { label: "Combo Gia Đình (x2)", desc: "Tiết kiệm thêm 5%", priceDelta: product.value.price * 0.95 }
]);

const currentCalculatedPrice = computed(() => {
  if (selectedUnitVariant.value === 1) {
    return product.value.price + unitVariants.value[1].priceDelta;
  }
  return product.value.price;
});

// Tab điều hướng chi tiết
const activeTab = ref<"desc" | "origin" | "storage" | "reviews">("desc");

// Yêu thích & Chia sẻ
const isFavorite = ref(false);
const toggleFavorite = () => {
  isFavorite.value = !isFavorite.value;
  showToastMessage(
    isFavorite.value
      ? `Đã lưu "${product.value.name}" vào danh sách yêu thích!`
      : `Đã bỏ lưu khỏi danh sách yêu thích.`
  );
};

const copyShareLink = async () => {
  try {
    await navigator.clipboard.writeText(window.location.href);
    showToastMessage("Đã sao chép liên kết sản phẩm vào bộ nhớ tạm!");
  } catch {
    showToastMessage("Liên kết: " + window.location.href);
  }
};

// Toast notification
const toastMessage = ref("");
const showToast = ref(false);
let toastTimer: any = null;

const showToastMessage = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  if (toastTimer) clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    showToast.value = false;
  }, 2600);
};

// Thêm vào giỏ hàng
const handleAddToCart = () => {
  const storeInfo = {
    storeId: product.value.store.id,
    storeName: product.value.store.name,
    distanceKm: product.value.store.distanceKm,
    deliveryTime: product.value.store.deliveryTime
  };

  const itemData = {
    id: product.value.id,
    name: product.value.name,
    price: currentCalculatedPrice.value,
    originalPrice: product.value.oldPrice,
    image: product.value.image,
    unit: selectedUnitVariant.value === 0 ? product.value.unit : "Combo x2"
  };

  cart.addItem(storeInfo, itemData, quantity.value);
  showToastMessage(`Đã thêm ${quantity.value} "${product.value.name}" vào giỏ hàng!`);
};

// Mua ngay hỏa tốc (Thêm vào giỏ và sang trang Checkout ngay)
const handleBuyNow = () => {
  handleAddToCart();
  router.push("/checkout");
};

// Thêm nhanh từ danh sách gợi ý liên quan
const handleAddRelated = (item: CatalogProduct) => {
  cart.addItem(
    {
      storeId: item.store.id,
      storeName: item.store.name,
      distanceKm: item.store.distanceKm,
      deliveryTime: item.store.deliveryTime
    },
    {
      id: item.id,
      name: item.name,
      price: item.price,
      originalPrice: item.oldPrice,
      image: item.image,
      unit: item.unit
    },
    1
  );
  showToastMessage(`Đã thêm nhanh "${item.name}" vào giỏ!`);
};

// Danh sách sản phẩm cùng khu vực 10km
const relatedProducts = computed(() => {
  return catalog.getRelatedProducts(product.value.id, 4);
});
</script>

<template>
  <div class="product-detail-page">
    <!-- Toast Popup thông báo xúc giác -->
    <transition name="toast-slide">
      <div v-if="showToast" class="tactile-toast">
        <div class="toast-icon"><i class="bi bi-check2-circle"></i></div>
        <div class="toast-content">{{ toastMessage }}</div>
      </div>
    </transition>

    <div class="detail-wrapper">
      <!-- 1. BREADCRUMBS & TOP BAR -->
      <nav class="top-nav-bar" aria-label="Breadcrumb">
        <button class="btn-back-nav" @click="router.push('/products')">
          <i class="bi bi-arrow-left"></i>
          <span>Danh sách sản phẩm</span>
        </button>

        <ol class="breadcrumb-trail">
          <li><router-link to="/">Trang chủ</router-link></li>
          <li class="separator"><i class="bi bi-chevron-right"></i></li>
          <li><router-link to="/products">Sản phẩm</router-link></li>
          <li class="separator"><i class="bi bi-chevron-right"></i></li>
          <li class="category-pill">{{ product.categoryName }}</li>
          <li class="separator"><i class="bi bi-chevron-right"></i></li>
          <li class="current-item" :title="product.name">{{ product.name }}</li>
        </ol>

        <div class="top-actions">
          <button
            class="btn-icon-action"
            :class="{ active: isFavorite }"
            title="Lưu vào danh sách yêu thích"
            @click="toggleFavorite"
          >
            <i :class="isFavorite ? 'bi bi-heart-fill' : 'bi bi-heart'"></i>
          </button>
          <button class="btn-icon-action" title="Chia sẻ sản phẩm" @click="copyShareLink">
            <i class="bi bi-share"></i>
          </button>
        </div>
      </nav>

      <!-- 2. MAIN PRODUCT SHOWCASE (BENTO 2 COLUMNS) -->
      <section class="main-showcase-bento">
        <!-- CỘT TRÁI: INTERACTIVE GALLERY & BADGES -->
        <div class="gallery-col">
          <div class="hero-image-wrap">
            <img
              :src="selectedImage"
              :alt="product.name"
              class="hero-photo"
              loading="eager"
            />

            <!-- Top Floating Trust Badges -->
            <div class="floating-badge-group">
              <span v-if="product.certification" class="badge-pill badge-cert">
                <i class="bi bi-patch-check-fill"></i>
                {{ product.certification.type }}
              </span>
              <span v-if="product.badge" class="badge-pill badge-highlight">
                <i class="bi bi-fire"></i>
                {{ product.badge }}
              </span>
            </div>

            <!-- Bottom Distance & ETA Chip -->
            <div class="badge-pill badge-express">
              <span class="pulse-dot"></span>
              <i class="bi bi-lightning-charge-fill"></i>
              Giao hỏa tốc {{ product.store.distanceKm }}km ({{ product.store.deliveryTime }})
            </div>
          </div>

          <!-- Thumbnail Selector Strip -->
          <div v-if="product.gallery && product.gallery.length > 1" class="thumbnail-strip">
            <button
              v-for="(photo, idx) in product.gallery"
              :key="idx"
              class="thumb-btn"
              :class="{ active: selectedImage === photo }"
              @click="selectImage(photo)"
            >
              <img :src="photo" :alt="`${product.name} góc chụp ${idx + 1}`" />
            </button>
          </div>

          <!-- ZoneMart Trust Banner -->
          <div class="zonemart-guarantee-card">
            <div class="guarantee-icon"><i class="bi bi-shield-lock-fill"></i></div>
            <div class="guarantee-body">
              <strong>Cam Kết Chất Lượng ZoneMart</strong>
              <p>100% tươi mới trong ngày • Bồi hoàn gấp đôi nếu dập nát • Thùng xe lạnh chuyên dụng</p>
            </div>
          </div>
        </div>

        <!-- CỘT PHẢI: COMMERCIAL INFO & HYPER-LOCAL VENDOR -->
        <div class="commercial-col">
          <!-- Category & Farm Origin Tag -->
          <div class="meta-headline-row">
            <span class="category-chip">{{ product.categoryName }}</span>
            <span class="farm-origin-tag">
              <i class="bi bi-geo-alt"></i> {{ product.specs.origin }}
            </span>
          </div>

          <!-- Product Title -->
          <h1 class="product-main-title">{{ product.name }}</h1>

          <!-- Rating & Sold Stats Row -->
          <div class="rating-sold-row">
            <div class="stars-wrap">
              <i class="bi bi-star-fill star-icon"></i>
              <strong class="score-num">{{ product.rating.toFixed(1) }}</strong>
              <span class="reviews-count">({{ product.reviewsCount }} đánh giá)</span>
            </div>
            <div class="sold-stat-badge">
              <i class="bi bi-bag-check"></i> Đã bán {{ product.sold }}
            </div>
            <div class="stock-status-pill">
              <i class="bi bi-box-seam"></i> Còn {{ product.stock }} sản phẩm
            </div>
          </div>

          <!-- Pricing Bento Box -->
          <div class="pricing-bento-card">
            <div class="price-digits-row">
              <span class="currency-symbol">₫</span>
              <span class="price-highlight">{{ currentCalculatedPrice.toLocaleString("vi-VN") }}</span>
              <span v-if="product.oldPrice" class="old-price-strike">
                {{ (product.oldPrice * (selectedUnitVariant === 1 ? 1.9 : 1)).toLocaleString("vi-VN") }} ₫
              </span>
              <span v-if="product.discountBadge" class="discount-pill">
                {{ product.discountBadge }}
              </span>
            </div>
            <div class="pricing-subtext">
              <i class="bi bi-info-circle"></i>
              Giá đã bao gồm thuế VAT và tem kiểm định an toàn thực phẩm ZoneMart
            </div>
          </div>

          <!-- Hyper-local Store Vendor Card -->
          <div class="vendor-store-card">
            <div class="store-avatar-box">
              <i class="bi bi-shop"></i>
            </div>
            <div class="store-details">
              <div class="store-name-line">
                <strong>{{ product.store.name }}</strong>
                <span class="verified-badge"><i class="bi bi-check-circle-fill"></i> Chính hãng</span>
              </div>
              <p class="store-address-text">
                <i class="bi bi-geo-alt-fill text-danger"></i> {{ product.store.address }}
              </p>
              <div class="store-micro-metrics">
                <span>Cách bạn <strong>{{ product.store.distanceKm }}km</strong></span>
                <span class="dot-sep">•</span>
                <span>Giao khoảng <strong>{{ product.store.deliveryTime }}</strong></span>
                <span class="dot-sep">•</span>
                <span>Đánh giá <strong>{{ product.store.rating }}★</strong></span>
              </div>
            </div>
            <div class="store-actions-wrap">
              <button class="btn-store-chat" title="Chat hỏi về sản phẩm" @click="showToastMessage('Hộp thoại tư vấn với Shop đang được kết nối...')">
                <i class="bi bi-chat-dots-fill"></i> Chat
              </button>
            </div>
          </div>

          <!-- Unit / Variant Options -->
          <div class="unit-selector-section">
            <label class="section-micro-label">Chọn quy cách đóng gói:</label>
            <div class="variant-chips-grid">
              <button
                v-for="(v, idx) in unitVariants"
                :key="idx"
                class="variant-chip-btn"
                :class="{ active: selectedUnitVariant === idx }"
                @click="selectedUnitVariant = idx"
              >
                <div class="variant-chip-label">{{ v.label }}</div>
                <div class="variant-chip-desc">{{ v.desc }}</div>
              </button>
            </div>
          </div>

          <!-- Quantity Selector & Action CTA Buttons -->
          <div class="purchase-action-group">
            <div class="quantity-controller">
              <label class="section-micro-label d-block mb-1">Số lượng:</label>
              <div class="stepper-box">
                <button
                  class="btn-step"
                  :disabled="quantity <= 1"
                  @click="quantity = Math.max(1, quantity - 1)"
                  aria-label="Giảm số lượng"
                >
                  <i class="bi bi-dash"></i>
                </button>
                <input
                  v-model.number="quantity"
                  type="number"
                  min="1"
                  :max="product.stock"
                  class="step-input"
                  aria-label="Số lượng sản phẩm"
                />
                <button
                  class="btn-step"
                  :disabled="quantity >= product.stock"
                  @click="quantity = Math.min(product.stock, quantity + 1)"
                  aria-label="Tăng số lượng"
                >
                  <i class="bi bi-plus"></i>
                </button>
              </div>
            </div>

            <div class="buttons-horizontal-row">
              <button class="btn-add-cart-tactile" @click="handleAddToCart">
                <i class="bi bi-bag-plus-fill"></i>
                <span>Thêm Vào Giỏ</span>
              </button>

              <button class="btn-buy-now-tactile" @click="handleBuyNow">
                <i class="bi bi-lightning-charge-fill"></i>
                <span>Mua Ngay Hỏa Tốc</span>
              </button>
            </div>
          </div>

          <!-- 4 Trust Micro-Feature Pills -->
          <div class="trust-pills-row">
            <div class="trust-mini-pill">
              <i class="bi bi-truck text-primary"></i>
              <span>Giao xe lạnh 15-25p</span>
            </div>
            <div class="trust-mini-pill">
              <i class="bi bi-arrow-repeat text-success"></i>
              <span>Đổi trả 2h nếu dập lỗi</span>
            </div>
            <div class="trust-mini-pill">
              <i class="bi bi-shield-check text-warning"></i>
              <span>Nông sản kiểm định AI</span>
            </div>
            <div class="trust-mini-pill">
              <i class="bi bi-credit-card text-info"></i>
              <span>Nhận hàng rồi trả tiền</span>
            </div>
          </div>
        </div>
      </section>

      <!-- 3. DEEP CONTENT MULTI-TAB PANEL -->
      <section class="deep-tabs-section">
        <!-- Tab Headers Bar -->
        <div class="tabs-header-bar">
          <button
            class="tab-btn"
            :class="{ active: activeTab === 'desc' }"
            @click="activeTab = 'desc'"
          >
            <i class="bi bi-card-text"></i>
            <span>Mô Tả & Dinh Dưỡng</span>
          </button>

          <button
            class="tab-btn"
            :class="{ active: activeTab === 'origin' }"
            @click="activeTab = 'origin'"
          >
            <i class="bi bi-qr-code-scan"></i>
            <span>Nguồn Gốc & Chứng Nhận AI</span>
          </button>

          <button
            class="tab-btn"
            :class="{ active: activeTab === 'storage' }"
            @click="activeTab = 'storage'"
          >
            <i class="bi bi-snow"></i>
            <span>Bảo Quản & Hướng Dẫn</span>
          </button>

          <button
            class="tab-btn"
            :class="{ active: activeTab === 'reviews' }"
            @click="activeTab = 'reviews'"
          >
            <i class="bi bi-chat-quote-fill"></i>
            <span>Đánh Giá Khách Hàng ({{ product.reviewsCount }})</span>
          </button>
        </div>

        <!-- Tab Body Containers -->
        <div class="tabs-content-body">
          <!-- TAB 1: MÔ TẢ & DINH DƯỠNG -->
          <div v-if="activeTab === 'desc'" class="tab-pane-content">
            <div class="desc-article">
              <h3 class="tab-section-heading">Chi Tiết Nông Sản & Nguồn Năng Lượng Tươi Sạch</h3>
              <p class="lead-paragraph">{{ product.description }}</p>

              <div class="highlights-card">
                <h4><i class="bi bi-stars text-warning"></i> Điểm nổi bật vượt trội:</h4>
                <ul>
                  <li v-for="(h, idx) in product.highlights" :key="idx">
                    <i class="bi bi-check-circle-fill text-success"></i>
                    <span>{{ h }}</span>
                  </li>
                </ul>
              </div>

              <!-- Bảng thành phần dinh dưỡng -->
              <h4 class="tab-sub-heading">Giá Trị Dinh Dưỡng (Khẩu phần: {{ product.nutrition.servingSize }})</h4>
              <div class="nutrition-specs-grid">
                <div class="nutrition-item">
                  <span class="nutri-label">Năng Lượng</span>
                  <strong class="nutri-val">{{ product.nutrition.calories }}</strong>
                </div>
                <div class="nutrition-item">
                  <span class="nutri-label">Chất Đạm (Protein)</span>
                  <strong class="nutri-val">{{ product.nutrition.protein }}</strong>
                </div>
                <div class="nutrition-item">
                  <span class="nutri-label">Chất Béo Lành Mạnh</span>
                  <strong class="nutri-val">{{ product.nutrition.fat }}</strong>
                </div>
                <div class="nutrition-item">
                  <span class="nutri-label">Carbohydrate</span>
                  <strong class="nutri-val">{{ product.nutrition.carbs }}</strong>
                </div>
                <div class="nutrition-item full-width">
                  <span class="nutri-label">Khoáng Chất & Vitamin</span>
                  <strong class="nutri-val">{{ product.nutrition.minerals }}</strong>
                </div>
              </div>

              <!-- Quy cách đóng gói -->
              <h4 class="tab-sub-heading mt-4">Thông Số Kỹ Thuật Sản Phẩm</h4>
              <table class="specs-table">
                <tbody>
                  <tr>
                    <th>Xuất xứ</th>
                    <td>{{ product.specs.origin }}</td>
                  </tr>
                  <tr>
                    <th>Thương hiệu / Nhà vườn</th>
                    <td>{{ product.specs.brand }}</td>
                  </tr>
                  <tr>
                    <th>Quy cách đóng gói</th>
                    <td>{{ product.specs.packingStandard }}</td>
                  </tr>
                  <tr>
                    <th>Trọng lượng tịnh</th>
                    <td>{{ product.specs.weight }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- TAB 2: NGUỒN GỐC & CHỨNG NHẬN AI -->
          <div v-if="activeTab === 'origin'" class="tab-pane-content">
            <div class="origin-bento-grid">
              <!-- Cột 1: Thông tin chứng nhận & QR Traceability -->
              <div class="origin-card-box">
                <div class="cert-header">
                  <div class="cert-badge-large">
                    <i class="bi bi-shield-fill-check"></i>
                  </div>
                  <div>
                    <h4>Chứng Nhận Tiêu Chuẩn Nông Sản</h4>
                    <p class="text-muted">Mã số lưu trữ trên hệ thống dữ liệu ATTP Quốc Gia</p>
                  </div>
                </div>

                <div v-if="product.certification" class="cert-fields">
                  <div class="cert-row">
                    <span class="cert-lbl">Tiêu chuẩn:</span>
                    <strong class="cert-val text-success">{{ product.certification.type }}</strong>
                  </div>
                  <div class="cert-row">
                    <span class="cert-lbl">Số hiệu chứng chỉ:</span>
                    <strong class="cert-val">{{ product.certification.certNo }}</strong>
                  </div>
                  <div class="cert-row">
                    <span class="cert-lbl">Cơ quan cấp:</span>
                    <span class="cert-val">{{ product.certification.issuedBy }}</span>
                  </div>
                  <div class="cert-row">
                    <span class="cert-lbl">Ngày cấp / Hạn dùng:</span>
                    <span class="cert-val">{{ product.certification.issuedDate }} - {{ product.certification.expiryDate }}</span>
                  </div>
                </div>

                <!-- Mô phỏng QR Code Truy xuất -->
                <div class="traceability-qr-box">
                  <div class="qr-mock">
                    <i class="bi bi-qr-code"></i>
                  </div>
                  <div class="qr-text">
                    <strong>Mã QR Truy Xuất Nguồn Gốc</strong>
                    <p>Quét bằng ứng dụng ZoneMart hoặc Zalo để tra cứu nhật ký thu hoạch và nhiệt độ xe lạnh theo thời gian thực.</p>
                  </div>
                </div>
              </div>

              <!-- Cột 2: Báo cáo kiểm định AI Vision Guard -->
              <div class="origin-card-box ai-audit-card">
                <div class="ai-audit-header">
                  <div class="ai-shield-radar">
                    <i class="bi bi-cpu-fill"></i>
                  </div>
                  <div>
                    <h4>ZoneMart AI Vision Guard System</h4>
                    <p>Báo cáo thẩm định an toàn thực phẩm bằng thị giác máy tính</p>
                  </div>
                </div>

                <div class="ai-scores-container">
                  <div class="score-circle-card">
                    <div class="circle-num">{{ product.aiAudit?.safetyScore || 98 }}%</div>
                    <span class="circle-label">Chỉ Số An Toàn</span>
                  </div>
                  <div class="score-circle-card">
                    <div class="circle-num">{{ product.aiAudit?.matchScore || 99 }}%</div>
                    <span class="circle-label">Độ Khớp Thị Giác</span>
                  </div>
                </div>

                <div class="ai-log-details">
                  <div class="log-line">
                    <i class="bi bi-check-circle-fill text-success"></i>
                    <span><strong>Đối soát hình ảnh:</strong> Ảnh chụp thật khớp 100% với tên hàng, không phát hiện giả mạo bao bì.</span>
                  </div>
                  <div class="log-line">
                    <i class="bi bi-check-circle-fill text-success"></i>
                    <span><strong>Bộ lọc an toàn:</strong> Không phát hiện chất tẩy rửa, phụ gia cấm hoặc dấu hiệu dập ôi thiu.</span>
                  </div>
                  <div class="log-line">
                    <i class="bi bi-info-circle-fill text-primary"></i>
                    <span><strong>Thời điểm AI phê duyệt:</strong> {{ product.aiAudit?.verificationDate || '11/09/2026' }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- TAB 3: HƯỚNG DẪN BẢO QUẢN & CHẾ BIẾN -->
          <div v-if="activeTab === 'storage'" class="tab-pane-content">
            <div class="storage-guide-container">
              <div class="storage-card">
                <div class="storage-icon"><i class="bi bi-thermometer-snow"></i></div>
                <div class="storage-details">
                  <h4>Điều Kiện Bảo Quản Tối Ưu</h4>
                  <p>{{ product.specs.storage }}</p>
                  <div class="temp-badge">
                    <i class="bi bi-clock-history"></i> Hạn sử dụng: <strong>{{ product.specs.shelfLife }}</strong>
                  </div>
                </div>
              </div>

              <div class="cooking-tips-card">
                <h4><i class="bi bi-lightbulb-fill text-warning"></i> Bí Quyết Đầu Bếp & Mẹo Chế Biến Ngon:</h4>
                <div class="tips-timeline">
                  <div v-for="(tip, idx) in product.cookingTips" :key="idx" class="tip-step">
                    <div class="step-badge">Bước {{ idx + 1 }}</div>
                    <p class="step-text">{{ tip }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- TAB 4: ĐÁNH GIÁ KHÁCH HÀNG THỰC TẾ -->
          <div v-if="activeTab === 'reviews'" class="tab-pane-content">
            <div class="reviews-section-layout">
              <!-- Cột trái: Tổng kết điểm số & thanh sao -->
              <div class="review-summary-card">
                <div class="big-rating-score">{{ product.rating.toFixed(1) }}</div>
                <div class="stars-gold">
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                </div>
                <p class="text-muted mb-3">{{ product.reviewsCount }} nhận xét từ người mua thực tế</p>

                <!-- Thanh phân bổ điểm số -->
                <div class="stars-breakdown">
                  <div class="star-bar-row">
                    <span>5 ★</span>
                    <div class="progress-bar"><div class="fill" style="width: 92%"></div></div>
                    <span>92%</span>
                  </div>
                  <div class="star-bar-row">
                    <span>4 ★</span>
                    <div class="progress-bar"><div class="fill" style="width: 8%"></div></div>
                    <span>8%</span>
                  </div>
                  <div class="star-bar-row">
                    <span>3 ★</span>
                    <div class="progress-bar"><div class="fill" style="width: 0%"></div></div>
                    <span>0%</span>
                  </div>
                </div>
              </div>

              <!-- Cột phải: Danh sách đánh giá chi tiết -->
              <div class="reviews-list-col">
                <div v-if="product.reviews && product.reviews.length > 0" class="reviews-cards-stack">
                  <div v-for="rv in product.reviews" :key="rv.id" class="single-review-card">
                    <div class="review-author-row">
                      <img :src="rv.avatar" :alt="rv.author" class="author-avatar" />
                      <div>
                        <strong>{{ rv.author }}</strong>
                        <div class="verified-buyer-tag">
                          <i class="bi bi-bag-check-fill text-success"></i> Đã mua tại ZoneMart
                        </div>
                      </div>
                      <span class="review-date">{{ rv.date }}</span>
                    </div>

                    <div class="review-stars-row">
                      <i v-for="s in rv.rating" :key="s" class="bi bi-star-fill text-warning"></i>
                    </div>

                    <p class="review-comment-text">{{ rv.comment }}</p>

                    <div v-if="rv.photos && rv.photos.length > 0" class="review-photos-grid">
                      <img v-for="(p, pIdx) in rv.photos" :key="pIdx" :src="p" alt="Ảnh thực tế người mua" />
                    </div>

                    <div class="review-helpful-action">
                      <button class="btn-helpful">
                        <i class="bi bi-hand-thumbs-up"></i> Hữu ích ({{ rv.helpfulCount }})
                      </button>
                    </div>
                  </div>
                </div>

                <!-- Trường hợp sản phẩm mới có đánh giá tự động -->
                <div v-else class="reviews-cards-stack">
                  <div class="single-review-card">
                    <div class="review-author-row">
                      <img src="https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=120&q=80" alt="Khách hàng" class="author-avatar" />
                      <div>
                        <strong>Nguyễn Văn Tuấn (Cầu Giấy)</strong>
                        <div class="verified-buyer-tag">
                          <i class="bi bi-bag-check-fill text-success"></i> Đã mua tại ZoneMart
                        </div>
                      </div>
                      <span class="review-date">Vừa xong</span>
                    </div>
                    <div class="review-stars-row">
                      <i v-for="s in 5" :key="s" class="bi bi-star-fill text-warning"></i>
                    </div>
                    <p class="review-comment-text">
                      Giao hàng siêu nhanh chỉ trong 18 phút, sản phẩm được bọc gói rất kỹ càng. Độ tươi ngon đúng như mô tả trên ứng dụng!
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- 4. HYPER-LOCAL CROSS-SELL SECTION (10KM) -->
      <section class="cross-sell-section">
        <div class="cross-sell-header">
          <div>
            <span class="section-tag-pill">GỢI Ý KHU VỰC</span>
            <h3 class="section-title">Sản Phẩm Tương Tự Trong Bán Kính 10km</h3>
          </div>
          <button class="btn-view-all-link" @click="router.push('/products')">
            <span>Xem tất cả</span>
            <i class="bi bi-arrow-right"></i>
          </button>
        </div>

        <div class="related-cards-grid">
          <div
            v-for="rel in relatedProducts"
            :key="rel.id"
            class="related-tactile-card"
            @click="router.push(`/products/${rel.id}`)"
          >
            <div class="rel-media-box">
              <img :src="rel.image" :alt="rel.name" loading="lazy" />
              <span v-if="rel.discountBadge" class="rel-badge">{{ rel.discountBadge }}</span>
              <div class="rel-distance-chip">
                <i class="bi bi-geo-alt-fill"></i> {{ rel.store.distanceKm }}km
              </div>
            </div>

            <div class="rel-card-body">
              <div class="rel-store-name">{{ rel.store.name }}</div>
              <h4 class="rel-product-title" :title="rel.name">{{ rel.name }}</h4>

              <div class="rel-price-row">
                <div class="rel-prices">
                  <span class="rel-current-price">{{ rel.price.toLocaleString("vi-VN") }} ₫</span>
                  <span v-if="rel.oldPrice" class="rel-old-price">{{ rel.oldPrice.toLocaleString("vi-VN") }} ₫</span>
                </div>
                <button
                  class="btn-rel-add"
                  title="Thêm nhanh vào giỏ hàng"
                  @click.stop="handleAddRelated(rel)"
                >
                  <i class="bi bi-bag-plus-fill"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>

    <!-- 5. STICKY MOBILE ACTION BAR -->
    <div class="sticky-mobile-bar">
      <div class="sticky-price-side">
        <span class="sticky-label">Tổng cộng:</span>
        <strong class="sticky-price">{{ (currentCalculatedPrice * quantity).toLocaleString("vi-VN") }} ₫</strong>
      </div>
      <div class="sticky-actions-side">
        <button class="btn-sticky-cart" @click="handleAddToCart" title="Thêm vào giỏ">
          <i class="bi bi-bag-plus"></i>
        </button>
        <button class="btn-sticky-buy" @click="handleBuyNow">
          <span>Mua Ngay Hỏa Tốc</span>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ============================================================================
   TASTE-SKILL STYLING: WARM HUMANIST, EDITORIAL COMMERCE & TACTILE CONTROLS
   Palette: Terracotta #ea580c, #c2410c, Warm Cream #faf7f2, Slate #0f172a
   ============================================================================ */
.product-detail-page {
  width: 100%;
  min-height: 100vh;
  background-color: #f8fafc;
  color: #0f172a;
  padding: 24px 20px 80px 20px;
  font-family: inherit;
}

.detail-wrapper {
  max-width: 1240px;
  margin: 0 auto;
}

/* 1. TOP BREADCRUMB NAV */
.top-nav-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  padding: 10px 16px;
  background: #ffffff;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.btn-back-nav {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #f1f5f9;
  border: none;
  color: #334155;
  font-weight: 600;
  font-size: 13px;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-back-nav:hover {
  background: #ea580c;
  color: #ffffff;
}

.breadcrumb-trail {
  display: flex;
  align-items: center;
  gap: 8px;
  list-style: none;
  margin: 0;
  padding: 0;
  font-size: 13px;
  color: #64748b;
  overflow: hidden;
}

.breadcrumb-trail a {
  color: #64748b;
  text-decoration: none;
  transition: color 0.15s;
}

.breadcrumb-trail a:hover {
  color: #ea580c;
}

.breadcrumb-trail .separator {
  font-size: 11px;
  color: #cbd5e1;
}

.category-pill {
  background: #fff7ed;
  color: #ea580c;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 6px;
  font-size: 12px;
}

.current-item {
  color: #0f172a;
  font-weight: 600;
  max-width: 260px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.top-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-icon-action {
  width: 36px;
  height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  color: #64748b;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-icon-action:hover {
  background: #fff7ed;
  color: #ea580c;
  border-color: #fdba74;
}

.btn-icon-action.active {
  background: #fee2e2;
  color: #ef4444;
  border-color: #fca5a5;
}

/* 2. MAIN BENTO SHOWCASE (2 COLUMNS) */
.main-showcase-bento {
  display: grid;
  grid-template-columns: 1fr 1.25fr;
  gap: 36px;
  background: #ffffff;
  border-radius: 24px;
  border: 1px solid #e2e8f0;
  padding: 32px;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.05);
  margin-bottom: 36px;
}

/* Column Left: Gallery */
.gallery-col {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.hero-image-wrap {
  position: relative;
  width: 100%;
  height: 420px;
  border-radius: 20px;
  overflow: hidden;
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
}

.hero-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s ease;
}

.hero-photo:hover {
  transform: scale(1.03);
}

.floating-badge-group {
  position: absolute;
  top: 14px;
  left: 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  z-index: 2;
}

.badge-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 999px;
  backdrop-filter: blur(8px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
}

.badge-cert {
  background: rgba(22, 163, 74, 0.95);
  color: #ffffff;
}

.badge-highlight {
  background: rgba(234, 88, 12, 0.95);
  color: #ffffff;
}

.badge-express {
  position: absolute;
  bottom: 14px;
  left: 14px;
  right: 14px;
  background: rgba(15, 23, 42, 0.85);
  color: #ffffff;
  border-radius: 12px;
  padding: 8px 14px;
  font-size: 12px;
  font-weight: 600;
  z-index: 2;
}

.pulse-dot {
  width: 8px;
  height: 8px;
  background-color: #22c55e;
  border-radius: 50%;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.4);
  animation: pulse-ring 1.8s infinite;
}

@keyframes pulse-ring {
  0% { box-shadow: 0 0 0 0 rgba(34, 197, 94, 0.7); }
  70% { box-shadow: 0 0 0 6px rgba(34, 197, 94, 0); }
  100% { box-shadow: 0 0 0 0 rgba(34, 197, 94, 0); }
}

.thumbnail-strip {
  display: flex;
  gap: 12px;
  overflow-x: auto;
  padding: 4px 0;
}

.thumb-btn {
  width: 76px;
  height: 76px;
  border-radius: 12px;
  overflow: hidden;
  border: 2px solid transparent;
  background: #f8fafc;
  padding: 0;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;
}

.thumb-btn img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.thumb-btn.active {
  border-color: #ea580c;
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(234, 88, 12, 0.25);
}

.zonemart-guarantee-card {
  display: flex;
  align-items: center;
  gap: 14px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  border-radius: 14px;
  padding: 12px 16px;
}

.guarantee-icon {
  font-size: 24px;
  color: #16a34a;
}

.guarantee-body strong {
  display: block;
  font-size: 13px;
  color: #166534;
}

.guarantee-body p {
  margin: 2px 0 0 0;
  font-size: 11px;
  color: #15803d;
}

/* Column Right: Commercial Panel */
.commercial-col {
  display: flex;
  flex-direction: column;
}

.meta-headline-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}

.category-chip {
  background: #fff7ed;
  color: #ea580c;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  padding: 4px 10px;
  border-radius: 6px;
}

.farm-origin-tag {
  font-size: 13px;
  color: #64748b;
  font-weight: 500;
}

.product-main-title {
  font-size: 26px;
  font-weight: 800;
  color: #0f172a;
  line-height: 1.35;
  margin: 6px 0 12px 0;
}

.rating-sold-row {
  display: flex;
  align-items: center;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid #f1f5f9;
  margin-bottom: 18px;
  font-size: 13px;
}

.stars-wrap {
  display: flex;
  align-items: center;
  gap: 4px;
}

.star-icon {
  color: #f59e0b;
}

.score-num {
  font-weight: 700;
  color: #0f172a;
}

.reviews-count {
  color: #64748b;
}

.sold-stat-badge, .stock-status-pill {
  color: #64748b;
}

/* Pricing Bento */
.pricing-bento-card {
  background: #fff7ed;
  border: 1px solid #ffedd5;
  border-radius: 16px;
  padding: 16px 20px;
  margin-bottom: 20px;
}

.price-digits-row {
  display: flex;
  align-items: baseline;
  gap: 10px;
}

.currency-symbol {
  font-size: 20px;
  font-weight: 700;
  color: #ea580c;
}

.price-highlight {
  font-size: 34px;
  font-weight: 800;
  color: #ea580c;
  letter-spacing: -0.5px;
}

.old-price-strike {
  font-size: 16px;
  color: #94a3b8;
  text-decoration: line-through;
}

.discount-pill {
  background: #fee2e2;
  color: #dc2626;
  font-size: 12px;
  font-weight: 800;
  padding: 2px 8px;
  border-radius: 6px;
}

.pricing-subtext {
  font-size: 12px;
  color: #9a3412;
  margin-top: 6px;
  display: flex;
  align-items: center;
  gap: 6px;
}

/* Hyper-local Vendor Card */
.vendor-store-card {
  display: flex;
  align-items: center;
  gap: 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 14px 16px;
  margin-bottom: 20px;
}

.store-avatar-box {
  width: 44px;
  height: 44px;
  background: #ea580c;
  color: #ffffff;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
}

.store-details {
  flex-grow: 1;
}

.store-name-line {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: #0f172a;
}

.verified-badge {
  font-size: 11px;
  color: #16a34a;
  background: #ecfdf5;
  padding: 2px 6px;
  border-radius: 4px;
  font-weight: 600;
}

.store-address-text {
  margin: 3px 0 4px 0;
  font-size: 12px;
  color: #64748b;
}

.store-micro-metrics {
  font-size: 12px;
  color: #475569;
  display: flex;
  align-items: center;
  gap: 6px;
}

.dot-sep {
  color: #cbd5e1;
}

.btn-store-chat {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #334155;
  font-weight: 600;
  font-size: 12px;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}

.btn-store-chat:hover {
  background: #ea580c;
  color: #ffffff;
  border-color: #ea580c;
}

/* Packaging Variant selector */
.unit-selector-section {
  margin-bottom: 20px;
}

.section-micro-label {
  font-size: 13px;
  font-weight: 700;
  color: #475569;
  margin-bottom: 8px;
  display: block;
}

.variant-chips-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

.variant-chip-btn {
  background: #ffffff;
  border: 1.5px solid #e2e8f0;
  border-radius: 12px;
  padding: 10px 14px;
  text-align: left;
  cursor: pointer;
  transition: all 0.2s;
}

.variant-chip-btn:hover {
  border-color: #fdba74;
}

.variant-chip-btn.active {
  border-color: #ea580c;
  background: #fff7ed;
}

.variant-chip-label {
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
}

.variant-chip-desc {
  font-size: 11px;
  color: #64748b;
  margin-top: 2px;
}

/* Purchase Actions */
.purchase-action-group {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-bottom: 22px;
}

.quantity-controller {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stepper-box {
  display: inline-flex;
  align-items: center;
  border: 1.5px solid #cbd5e1;
  border-radius: 10px;
  background: #ffffff;
  overflow: hidden;
}

.btn-step {
  width: 38px;
  height: 40px;
  border: none;
  background: #f8fafc;
  color: #334155;
  font-size: 16px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}

.btn-step:hover:not(:disabled) {
  background: #ea580c;
  color: #ffffff;
}

.btn-step:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.step-input {
  width: 50px;
  height: 40px;
  border: none;
  text-align: center;
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  outline: none;
}

.buttons-horizontal-row {
  display: grid;
  grid-template-columns: 1.1fr 1.4fr;
  gap: 12px;
}

.btn-add-cart-tactile {
  background: #fff7ed;
  color: #ea580c;
  border: 1.5px solid #fdba74;
  border-radius: 12px;
  padding: 14px;
  font-size: 15px;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: all 0.2s;
}

.btn-add-cart-tactile:hover {
  background: #ea580c;
  color: #ffffff;
  border-color: #ea580c;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.25);
}

.btn-buy-now-tactile {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  color: #ffffff;
  border: none;
  border-radius: 12px;
  padding: 14px;
  font-size: 15px;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  box-shadow: 0 4px 16px rgba(234, 88, 12, 0.35);
  transition: all 0.2s;
}

.btn-buy-now-tactile:hover {
  background: linear-gradient(135deg, #c2410c 0%, #9a3412 100%);
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(234, 88, 12, 0.45);
}

/* 4 Trust Micro Pills */
.trust-pills-row {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px;
  padding-top: 14px;
  border-top: 1px solid #f1f5f9;
}

.trust-mini-pill {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #475569;
}

/* 3. MULTI-TAB DEEP CONTENT SECTION */
.deep-tabs-section {
  background: #ffffff;
  border-radius: 24px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.05);
  margin-bottom: 36px;
  overflow: hidden;
}

.tabs-header-bar {
  display: flex;
  border-bottom: 1px solid #e2e8f0;
  background: #f8fafc;
  overflow-x: auto;
}

.tab-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 18px 24px;
  border: none;
  background: transparent;
  color: #64748b;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  border-bottom: 3px solid transparent;
  transition: all 0.2s;
}

.tab-btn:hover {
  color: #ea580c;
}

.tab-btn.active {
  color: #ea580c;
  background: #ffffff;
  border-bottom-color: #ea580c;
}

.tabs-content-body {
  padding: 32px;
}

.tab-section-heading {
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
  margin-bottom: 12px;
}

.lead-paragraph {
  font-size: 15px;
  line-height: 1.7;
  color: #334155;
  margin-bottom: 24px;
}

.highlights-card {
  background: #faf7f2;
  border-left: 4px solid #ea580c;
  border-radius: 0 14px 14px 0;
  padding: 18px 24px;
  margin-bottom: 28px;
}

.highlights-card h4 {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  margin-bottom: 12px;
}

.highlights-card ul {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.highlights-card li {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  color: #334155;
}

.tab-sub-heading {
  font-size: 16px;
  font-weight: 700;
  color: #0f172a;
  margin: 20px 0 14px 0;
}

.nutrition-specs-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  margin-bottom: 24px;
}

.nutrition-item {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.nutrition-item.full-width {
  grid-column: 1 / -1;
}

.nutri-label {
  font-size: 12px;
  color: #64748b;
}

.nutri-val {
  font-size: 15px;
  color: #0f172a;
}

.specs-table {
  width: 100%;
  border-collapse: collapse;
}

.specs-table th, .specs-table td {
  padding: 12px 16px;
  border-bottom: 1px solid #f1f5f9;
  font-size: 14px;
}

.specs-table th {
  width: 240px;
  color: #64748b;
  font-weight: 600;
  background: #f8fafc;
}

.specs-table td {
  color: #0f172a;
}

/* Tab 2: Origin & AI Bento */
.origin-bento-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

.origin-card-box {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 18px;
  padding: 24px;
}

.cert-header, .ai-audit-header {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 18px;
}

.cert-badge-large {
  width: 48px;
  height: 48px;
  background: #ecfdf5;
  color: #16a34a;
  font-size: 24px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.ai-shield-radar {
  width: 48px;
  height: 48px;
  background: #fff7ed;
  color: #ea580c;
  font-size: 24px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.cert-fields {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 20px;
}

.cert-row {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  padding-bottom: 8px;
  border-bottom: 1px dashed #cbd5e1;
}

.traceability-qr-box {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #ffffff;
  border: 1.5px dashed #ea580c;
  border-radius: 14px;
  padding: 14px;
}

.qr-mock {
  font-size: 48px;
  color: #0f172a;
  line-height: 1;
}

.qr-text strong {
  display: block;
  font-size: 14px;
  color: #ea580c;
}

.qr-text p {
  margin: 4px 0 0 0;
  font-size: 12px;
  color: #64748b;
}

.ai-scores-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
  margin-bottom: 20px;
}

.score-circle-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 16px;
  text-align: center;
}

.circle-num {
  font-size: 28px;
  font-weight: 800;
  color: #ea580c;
}

.circle-label {
  font-size: 12px;
  color: #64748b;
  font-weight: 600;
}

.ai-log-details {
  display: flex;
  flex-direction: column;
  gap: 10px;
  font-size: 13px;
}

.log-line {
  display: flex;
  gap: 8px;
  line-height: 1.4;
}

/* Tab 3: Storage & Cooking Guide */
.storage-guide-container {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.storage-card {
  display: flex;
  align-items: center;
  gap: 20px;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 16px;
  padding: 20px;
}

.storage-icon {
  font-size: 36px;
  color: #2563eb;
}

.temp-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #ffffff;
  color: #1e40af;
  font-size: 13px;
  padding: 4px 10px;
  border-radius: 8px;
  margin-top: 8px;
}

.cooking-tips-card {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 24px;
}

.tips-timeline {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-top: 14px;
}

.tip-step {
  display: flex;
  gap: 14px;
  align-items: flex-start;
}

.step-badge {
  background: #ea580c;
  color: #ffffff;
  font-size: 12px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
  flex-shrink: 0;
}

.step-text {
  margin: 0;
  font-size: 14px;
  color: #334155;
  line-height: 1.5;
}

/* Tab 4: Reviews */
.reviews-section-layout {
  display: grid;
  grid-template-columns: 280px 1fr;
  gap: 32px;
}

.review-summary-card {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 24px;
  text-align: center;
  height: fit-content;
}

.big-rating-score {
  font-size: 54px;
  font-weight: 900;
  color: #ea580c;
  line-height: 1;
  margin-bottom: 6px;
}

.stars-gold {
  color: #f59e0b;
  font-size: 18px;
  margin-bottom: 6px;
}

.stars-breakdown {
  display: flex;
  flex-direction: column;
  gap: 8px;
  text-align: left;
  font-size: 12px;
  color: #64748b;
}

.star-bar-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.star-bar-row span {
  width: 24px;
}

.star-bar-row .progress-bar {
  flex-grow: 1;
  height: 8px;
  background: #e2e8f0;
  border-radius: 999px;
  overflow: hidden;
}

.star-bar-row .progress-bar .fill {
  height: 100%;
  background: #f59e0b;
  border-radius: 999px;
}

.reviews-cards-stack {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.single-review-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.03);
}

.review-author-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}

.author-avatar {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  object-fit: cover;
}

.verified-buyer-tag {
  font-size: 11px;
  color: #16a34a;
}

.review-date {
  margin-left: auto;
  font-size: 12px;
  color: #94a3b8;
}

.review-stars-row {
  margin-bottom: 8px;
  font-size: 13px;
}

.review-comment-text {
  font-size: 14px;
  color: #334155;
  line-height: 1.6;
  margin-bottom: 12px;
}

.review-photos-grid {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
}

.review-photos-grid img {
  width: 72px;
  height: 72px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid #e2e8f0;
}

.btn-helpful {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #64748b;
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-helpful:hover {
  background: #f1f5f9;
  color: #0f172a;
}

/* 4. CROSS-SELL 10KM SECTION */
.cross-sell-section {
  margin-top: 40px;
}

.cross-sell-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 20px;
}

.section-tag-pill {
  font-size: 11px;
  font-weight: 800;
  color: #ea580c;
  letter-spacing: 1px;
}

.section-title {
  font-size: 22px;
  font-weight: 800;
  color: #0f172a;
  margin: 4px 0 0 0;
}

.btn-view-all-link {
  background: none;
  border: none;
  color: #ea580c;
  font-weight: 700;
  font-size: 14px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.related-cards-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
}

.related-tactile-card {
  background: #ffffff;
  border-radius: 18px;
  border: 1px solid #e2e8f0;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.25s ease;
}

.related-tactile-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px -6px rgba(0, 0, 0, 0.08);
  border-color: #fdba74;
}

.rel-media-box {
  position: relative;
  height: 180px;
  overflow: hidden;
  background: #f1f5f9;
}

.rel-media-box img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}

.related-tactile-card:hover .rel-media-box img {
  transform: scale(1.06);
}

.rel-badge {
  position: absolute;
  top: 8px;
  left: 8px;
  background: #dc2626;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 2px 6px;
  border-radius: 4px;
}

.rel-distance-chip {
  position: absolute;
  bottom: 8px;
  right: 8px;
  background: rgba(15, 23, 42, 0.8);
  color: #ffffff;
  font-size: 11px;
  font-weight: 600;
  padding: 3px 8px;
  border-radius: 6px;
}

.rel-card-body {
  padding: 14px;
}

.rel-store-name {
  font-size: 12px;
  color: #64748b;
  margin-bottom: 4px;
}

.rel-product-title {
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  margin: 0 0 12px 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.rel-price-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.rel-prices {
  display: flex;
  flex-direction: column;
}

.rel-current-price {
  font-size: 15px;
  font-weight: 800;
  color: #ea580c;
}

.rel-old-price {
  font-size: 11px;
  color: #94a3b8;
  text-decoration: line-through;
}

.btn-rel-add {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  background: #fff7ed;
  border: 1px solid #fdba74;
  color: #ea580c;
  font-size: 15px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.btn-rel-add:hover {
  background: #ea580c;
  color: #ffffff;
}

/* 5. STICKY MOBILE ACTION BAR */
.sticky-mobile-bar {
  display: none;
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
  padding: 12px 16px;
  box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.08);
  z-index: 99;
  justify-content: space-between;
  align-items: center;
}

.sticky-price-side {
  display: flex;
  flex-direction: column;
}

.sticky-label {
  font-size: 11px;
  color: #64748b;
}

.sticky-price {
  font-size: 18px;
  font-weight: 800;
  color: #ea580c;
}

.sticky-actions-side {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-sticky-cart {
  width: 44px;
  height: 44px;
  background: #fff7ed;
  border: 1.5px solid #fdba74;
  color: #ea580c;
  border-radius: 10px;
  font-size: 18px;
  cursor: pointer;
}

.btn-sticky-buy {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  color: #ffffff;
  border: none;
  border-radius: 10px;
  padding: 0 18px;
  height: 44px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
}

/* TACTILE TOAST NOTIFICATION */
.tactile-toast {
  position: fixed;
  top: 24px;
  left: 50%;
  transform: translateX(-50%);
  background: #0f172a;
  color: #ffffff;
  padding: 12px 24px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  gap: 10px;
  box-shadow: 0 10px 25px -3px rgba(0, 0, 0, 0.3);
  z-index: 1000;
  font-size: 14px;
  font-weight: 600;
}

.toast-icon {
  color: #22c55e;
  font-size: 18px;
}

.toast-slide-enter-active,
.toast-slide-leave-active {
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-slide-enter-from,
.toast-slide-leave-to {
  opacity: 0;
  transform: translate(-50%, -20px);
}

/* RESPONSIVE BREAKPOINTS */
@media (max-width: 1024px) {
  .main-showcase-bento {
    grid-template-columns: 1fr;
    gap: 28px;
  }
  .related-cards-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .nutrition-specs-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .origin-bento-grid {
    grid-template-columns: 1fr;
  }
  .reviews-section-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .product-detail-page {
    padding: 12px 12px 80px 12px;
  }
  .main-showcase-bento {
    padding: 18px;
    border-radius: 18px;
  }
  .hero-image-wrap {
    height: 300px;
  }
  .product-main-title {
    font-size: 22px;
  }
  .price-highlight {
    font-size: 28px;
  }
  .buttons-horizontal-row {
    grid-template-columns: 1fr;
  }
  .sticky-mobile-bar {
    display: flex;
  }
  .tabs-content-body {
    padding: 20px 16px;
  }
  .top-nav-bar .breadcrumb-trail {
    display: none;
  }
}
</style>
