<script setup lang="ts">
/**
 * ============================================================================
 * TRANG DANH SÁCH SẢN PHẨM & GIAN HÀNG (CATALOG & STORES VIEW) - ZONE MART
 * Thiết kế áp dụng Taste-Skill: Anti-Slop, Bento Asymmetry, Frameless, Tactile
 * Hỗ trợ tìm kiếm linh hoạt: Sản phẩm HOẶC Gian hàng
 * ============================================================================
 */
import { ref, computed, watch, onMounted, onUnmounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useProductCatalog, inferSubCategoryAndPrep, type CatalogStore } from "../../composables/useProductCatalog";
import { useCart } from "../../composables/useCart";
import { useToast } from "../../composables/useToast";

const router = useRouter();
const route = useRoute();
const cart = useCart();
const catalog = useProductCatalog();

// State lọc & tìm kiếm
const searchQuery = ref("");
const selectedCategory = ref("all");
const selectedSubCategory = ref("all"); // Lọc phân tầng cấp 2 (ví dụ: Thịt gà sống, Gà rán KFC...)
const selectedPrepType = ref("all"); // Phân loại chế biến: 'all' | 'raw' (sống) | 'cooked' (chín/nóng) | 'fresh' | 'packaged'
const sortBy = ref("popular"); // 'popular' | 'distance' | 'price-asc' | 'price-desc' | 'rating'
const activeSearchTab = ref<"product" | "store">("product"); // Tab tìm kiếm: 'product' hoặc 'store'
const selectedStoreFilter = ref<string>(""); // Tên gian hàng cụ thể nếu người dùng đang lọc theo gian hàng
const toastMessage = ref("");
const showToast = ref(false);

const toast = useToast();
const isCatalogLoading = ref(true);
let loadingTimer: any = null;

const triggerLoadingEffect = (ms = 350) => {
  isCatalogLoading.value = true;
  if (loadingTimer) clearTimeout(loadingTimer);
  window.dispatchEvent(new CustomEvent('zonemart:loading-start'));
  loadingTimer = setTimeout(() => {
    isCatalogLoading.value = false;
    window.dispatchEvent(new CustomEvent('zonemart:loading-finish'));
  }, ms);
};

const showNotification = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2400);
};

// Hàm chuẩn hóa chuỗi tiếng Việt không dấu để tìm kiếm không dấu / có dấu đều khớp
function removeVietnameseTones(str: string): string {
  return str
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase();
}

// Đồng bộ từ URL query params (hỗ trợ cả search, q, cat, type, store)
const syncFromRoute = () => {
  const qVal = route.query.search !== undefined ? route.query.search : route.query.q;
  if (qVal !== undefined) {
    searchQuery.value = String(qVal || "");
  }
  if (route.query.cat !== undefined) {
    selectedCategory.value = String(route.query.cat || "all");
  } else if (qVal) {
    // Khi người dùng tìm kiếm từ khóa mới mà URL không gán cố định danh mục, tự động reset về "all"
    selectedCategory.value = "all";
  }
  if (route.query.subcat !== undefined) {
    selectedSubCategory.value = String(route.query.subcat || "all");
  } else {
    selectedSubCategory.value = "all";
  }
  if (route.query.prep !== undefined) {
    selectedPrepType.value = String(route.query.prep || "all");
  } else {
    selectedPrepType.value = "all";
  }
  if (route.query.type === "store") {
    activeSearchTab.value = "store";
  } else if (route.query.type === "product") {
    activeSearchTab.value = "product";
  }
  if (route.query.store) {
    selectedStoreFilter.value = String(route.query.store);
    activeSearchTab.value = "product";
  } else {
    selectedStoreFilter.value = "";
  }
};

const handleCatalogRatingsUpdated = () => {
  catalog.refreshCatalog();
  catalog.fetchRatingsSummary();
};

onMounted(() => {
  catalog.fetchRatingsSummary();
  catalog.refreshCatalog();
  syncFromRoute();
  triggerLoadingEffect(420);

  window.addEventListener('zonemart:ratings-updated', handleCatalogRatingsUpdated);
  window.addEventListener('zonemart:products-changed', handleCatalogRatingsUpdated);
});

onUnmounted(() => {
  window.removeEventListener('zonemart:ratings-updated', handleCatalogRatingsUpdated);
  window.removeEventListener('zonemart:products-changed', handleCatalogRatingsUpdated);
});
watch(() => route.query, () => {
  catalog.refreshCatalog();
  syncFromRoute();
  triggerLoadingEffect(300);
});
watch(() => selectedCategory.value, () => {
  selectedSubCategory.value = "all";
  triggerLoadingEffect(250);
});
watch([selectedSubCategory, selectedPrepType], () => {
  triggerLoadingEffect(200);
});
watch(() => activeSearchTab.value, () => {
  triggerLoadingEffect(280);
});

// Danh mục ngành hàng gốc (Cấp 1)
const categoryDefinitions = [
  { id: "all", name: "Tất cả", icon: "bi-grid-fill" },
  { id: "food", name: "Thực phẩm tươi", icon: "bi-fire" },
  { id: "veggie", name: "Rau củ VietGAP", icon: "bi-flower1" },
  { id: "fastfood", name: "Món ăn nóng", icon: "bi-cup-hot-fill" },
  { id: "beverage", name: "Đồ uống & Trái cây", icon: "bi-cup-straw" },
  { id: "household", name: "Nhu yếu phẩm", icon: "bi-basket2-fill" }
];

// Danh mục phân tầng cấp 2 (Subcategories theo từng danh mục cha)
export interface SubCategoryDef {
  id: string;
  name: string;
  icon: string;
  parentCategory: string;
}

const subCategoryDefinitions: SubCategoryDef[] = [
  // Thực phẩm tươi sống (food)
  { id: "all", name: "Tất cả đồ tươi", icon: "bi-grid", parentCategory: "food" },
  { id: "poultry", name: "Thịt gà & Gia cầm tươi", icon: "bi-egg-fill", parentCategory: "food" },
  { id: "beef_pork", name: "Thịt bò & Thịt heo", icon: "bi-shield-check", parentCategory: "food" },
  { id: "seafood", name: "Cá & Hải sản tươi", icon: "bi-water", parentCategory: "food" },
  { id: "egg_tofu", name: "Trứng & Đậu phụ", icon: "bi-circle", parentCategory: "food" },

  // Món ăn nóng / Đồ chín (fastfood)
  { id: "all", name: "Tất cả món nóng", icon: "bi-grid", parentCategory: "fastfood" },
  { id: "fried_chicken", name: "Gà rán & Fast food (KFC)", icon: "bi-fire", parentCategory: "fastfood" },
  { id: "rice_noodle", name: "Cơm & Mì / Phở nóng", icon: "bi-cup-hot-fill", parentCategory: "fastfood" },
  { id: "breakfast_bread", name: "Bánh mì & Ăn sáng", icon: "bi-sunset-fill", parentCategory: "fastfood" },
  { id: "snack", name: "Đồ ăn vặt & Ăn nhanh", icon: "bi-stars", parentCategory: "fastfood" },

  // Rau củ VietGAP (veggie)
  { id: "all", name: "Tất cả rau củ", icon: "bi-grid", parentCategory: "veggie" },
  { id: "leafy", name: "Rau ăn lá tươi", icon: "bi-flower1", parentCategory: "veggie" },
  { id: "root_fruit", name: "Củ & Quả nấu canh", icon: "bi-apple", parentCategory: "veggie" },
  { id: "mushroom_spices", name: "Nấm & Rau gia vị", icon: "bi-tree-fill", parentCategory: "veggie" },

  // Đồ uống & Trái cây (beverage)
  { id: "all", name: "Tất cả thức uống & quả", icon: "bi-grid", parentCategory: "beverage" },
  { id: "fruit", name: "Trái cây tươi Đà Lạt", icon: "bi-basket", parentCategory: "beverage" },
  { id: "tea_coffee", name: "Trà & Cà phê pha máy", icon: "bi-cup-hot", parentCategory: "beverage" },
  { id: "juice_smoothie", name: "Nước ép & Sinh tố", icon: "bi-cup-straw", parentCategory: "beverage" },

  // Nhu yếu phẩm (household)
  { id: "all", name: "Tất cả đồ dùng", icon: "bi-grid", parentCategory: "household" },
  { id: "kitchen", name: "Gia vị & Bếp núc", icon: "bi-box-seam", parentCategory: "household" },
  { id: "cleaning", name: "Chăm sóc nhà cửa", icon: "bi-sparkles", parentCategory: "household" }
];

// Phân loại trạng thái chế biến (Prep Type Filter - Sống vs Chín ăn ngay)
const prepTypeFilters = [
  { id: "all", name: "Tất cả trạng thái", icon: "bi-layers-fill" },
  { id: "raw", name: "Thịt tươi sống / Sơ chế", icon: "bi-snow" },
  { id: "cooked", name: "Món chín / Ăn ngay (KFC...)", icon: "bi-fire" },
  { id: "fresh", name: "Rau quả tươi sạch", icon: "bi-flower1" },
  { id: "packaged", name: "Đóng gói / Khô", icon: "bi-box-seam" },
];

// Danh sách sản phẩm đầy đủ với dữ liệu chân thực
interface ProductItem {
  id: string;
  name: string;
  category: string;
  categoryName: string;
  subCategory: string;
  prepType: "raw" | "cooked" | "fresh" | "packaged";
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
  description?: string;
}

const allProductItems = computed<ProductItem[]>(() => {
  return catalog.allProducts.value.map((p) => {
    const inferred = inferSubCategoryAndPrep(p);
    let img = p.image || "/images/nuoc_rua_bat_huu_co.jpg";
    if (
      (img && (img.includes("1585421514738") || img.includes("1584308666744"))) ||
      (p.name && (p.name.toLowerCase().includes("nước rửa bát") || p.name.toLowerCase().includes("nuoc rua bat")))
    ) {
      img = "/images/nuoc_rua_bat_huu_co.jpg";
    }
    return {
      id: p.id,
      name: p.name,
      category: p.category,
      categoryName: p.categoryName,
      subCategory: p.subCategory || inferred.subCategory,
      prepType: (p.prepType || inferred.prepType) as "raw" | "cooked" | "fresh" | "packaged",
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
      image: img,
      description: p.description || "",
    };
  });
});

// Danh mục ngành hàng cấp 1 với số lượng ĐỘNG theo đúng từ khóa tìm kiếm và bộ lọc hiện tại
const categories = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  const qNoTone = removeVietnameseTones(q);

  const baseItems = allProductItems.value.filter((p) => {
    const matchQuery =
      !q ||
      p.name.toLowerCase().includes(q) ||
      p.categoryName.toLowerCase().includes(q) ||
      (p.description && p.description.toLowerCase().includes(q)) ||
      removeVietnameseTones(p.name).includes(qNoTone) ||
      removeVietnameseTones(p.categoryName).includes(qNoTone) ||
      (p.description && removeVietnameseTones(p.description).includes(qNoTone));

    const matchStore =
      !selectedStoreFilter.value ||
      p.storeName.toLowerCase() === selectedStoreFilter.value.toLowerCase();

    return matchQuery && matchStore;
  });

  return categoryDefinitions.map((cat) => {
    const count =
      cat.id === "all"
        ? baseItems.length
        : baseItems.filter((p) => p.category === cat.id).length;
    return {
      ...cat,
      count,
    };
  });
});

// Phân tầng con cấp 2 (Hiển thị động theo danh mục cha đang chọn)
const availableSubCategories = computed(() => {
  if (selectedCategory.value === "all") {
    return [];
  }
  const q = searchQuery.value.trim().toLowerCase();
  const qNoTone = removeVietnameseTones(q);

  const baseItems = allProductItems.value.filter((p) => {
    const matchCategory = p.category === selectedCategory.value;
    const matchQuery =
      !q ||
      p.name.toLowerCase().includes(q) ||
      p.categoryName.toLowerCase().includes(q) ||
      (p.description && p.description.toLowerCase().includes(q)) ||
      removeVietnameseTones(p.name).includes(qNoTone) ||
      removeVietnameseTones(p.categoryName).includes(qNoTone) ||
      (p.description && removeVietnameseTones(p.description).includes(qNoTone));

    const matchStore =
      !selectedStoreFilter.value ||
      p.storeName.toLowerCase() === selectedStoreFilter.value.toLowerCase();

    return matchCategory && matchQuery && matchStore;
  });

  const defs = subCategoryDefinitions.filter(
    (s) => s.parentCategory === selectedCategory.value
  );

  return defs.map((sub) => {
    const count =
      sub.id === "all"
        ? baseItems.length
        : baseItems.filter((p) => p.subCategory === sub.id).length;
    return {
      ...sub,
      count,
    };
  });
});

// Bộ lọc trạng thái chế biến (Prep Type: Sống vs Chín/Ăn liền)
const availablePrepTypes = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  const qNoTone = removeVietnameseTones(q);

  const baseItems = allProductItems.value.filter((p) => {
    const matchCategory =
      selectedCategory.value === "all" || p.category === selectedCategory.value;
    const matchSubCategory =
      selectedSubCategory.value === "all" || p.subCategory === selectedSubCategory.value;
    const matchQuery =
      !q ||
      p.name.toLowerCase().includes(q) ||
      p.categoryName.toLowerCase().includes(q) ||
      (p.description && p.description.toLowerCase().includes(q)) ||
      removeVietnameseTones(p.name).includes(qNoTone) ||
      removeVietnameseTones(p.categoryName).includes(qNoTone) ||
      (p.description && removeVietnameseTones(p.description).includes(qNoTone));

    const matchStore =
      !selectedStoreFilter.value ||
      p.storeName.toLowerCase() === selectedStoreFilter.value.toLowerCase();

    return matchCategory && matchSubCategory && matchQuery && matchStore;
  });

  return prepTypeFilters
    .map((filter) => {
      const count =
        filter.id === "all"
          ? baseItems.length
          : baseItems.filter((p) => p.prepType === filter.id).length;
      return {
        ...filter,
        count,
      };
    })
    .filter((f) => f.id === "all" || f.count > 0);
});

// Tổng số sản phẩm khớp từ khóa nếu không bị giới hạn bởi category hiện tại
const totalMatchesWithoutCategory = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  const qNoTone = removeVietnameseTones(q);

  return allProductItems.value.filter((p) => {
    const matchQuery =
      !q ||
      p.name.toLowerCase().includes(q) ||
      p.categoryName.toLowerCase().includes(q) ||
      (p.description && p.description.toLowerCase().includes(q)) ||
      removeVietnameseTones(p.name).includes(qNoTone) ||
      removeVietnameseTones(p.categoryName).includes(qNoTone) ||
      (p.description && removeVietnameseTones(p.description).includes(qNoTone));

    const matchStore =
      !selectedStoreFilter.value ||
      p.storeName.toLowerCase() === selectedStoreFilter.value.toLowerCase();

    return matchQuery && matchStore;
  }).length;
});

// Lọc và sắp xếp sản phẩm đa tầng (Category Cấp 1 -> Subcategory Cấp 2 -> Trạng thái Chế biến -> Tìm kiếm & Bán kính)
const filteredProducts = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  const qNoTone = removeVietnameseTones(q);

  let result = allProductItems.value.filter((p) => {
    const matchCategory =
      selectedCategory.value === "all" || p.category === selectedCategory.value;

    const matchSubCategory =
      selectedSubCategory.value === "all" || p.subCategory === selectedSubCategory.value;

    const matchPrepType =
      selectedPrepType.value === "all" || p.prepType === selectedPrepType.value;

    const matchQuery =
      !q ||
      p.name.toLowerCase().includes(q) ||
      p.categoryName.toLowerCase().includes(q) ||
      (p.description && p.description.toLowerCase().includes(q)) ||
      removeVietnameseTones(p.name).includes(qNoTone) ||
      removeVietnameseTones(p.categoryName).includes(qNoTone) ||
      (p.description && removeVietnameseTones(p.description).includes(qNoTone));

    const matchStore =
      !selectedStoreFilter.value ||
      p.storeName.toLowerCase() === selectedStoreFilter.value.toLowerCase();

    return (
      matchCategory &&
      matchSubCategory &&
      matchPrepType &&
      matchQuery &&
      matchStore
    );
  });

  // Sắp xếp
  if (sortBy.value === "price-asc") {
    result.sort((a, b) => a.price - b.price);
  } else if (sortBy.value === "price-desc") {
    result.sort((a, b) => b.price - a.price);
  } else if (sortBy.value === "distance") {
    result.sort((a, b) => a.distanceKm - b.distanceKm);
  } else if (sortBy.value === "rating") {
    result.sort((a, b) => {
      // 1. Điểm đánh giá trung bình cao nhất xếp trước
      const diffRating = (b.rating || 0) - (a.rating || 0);
      if (Math.abs(diffRating) > 0.01) return diffRating;

      // 2. Ưu tiên những món có lượt đánh giá thực tế từ người mua trên hệ thống
      const bRealReviews = (b.reviews && b.reviews.length > 0) ? b.reviews.length : 0;
      const aRealReviews = (a.reviews && a.reviews.length > 0) ? a.reviews.length : 0;
      if (bRealReviews !== aRealReviews) return bRealReviews - aRealReviews;

      // 3. Số lượt đánh giá tổng
      const diffCount = (b.reviewsCount || 0) - (a.reviewsCount || 0);
      if (diffCount !== 0) return diffCount;

      // 4. Lượt bán
      return (b.sold || 0) - (a.sold || 0);
    });
  } else {
    // popular (Bán chạy nhất): Kết hợp lượt bán và ưu tiên đẩy mạnh món 5 sao có đánh giá thực tế lên đầu
    result.sort((a, b) => {
      const bRealReviews = (b.reviews && b.reviews.length > 0) ? b.reviews.length : 0;
      const aRealReviews = (a.reviews && a.reviews.length > 0) ? a.reviews.length : 0;
      const bScore = (b.sold || 0) + (b.rating >= 4.8 ? bRealReviews * 120 : 0);
      const aScore = (a.sold || 0) + (a.rating >= 4.8 ? aRealReviews * 120 : 0);
      return bScore - aScore;
    });
  }

  return result;
});

// Lọc gian hàng theo điều kiện
const filteredStores = computed<CatalogStore[]>(() => {
  const q = searchQuery.value.trim().toLowerCase();
  const qNoTone = removeVietnameseTones(q);
  let list = catalog.allStores.value;

  if (q) {
    list = list.filter((s) => {
      const matchName =
        s.name.toLowerCase().includes(q) ||
        removeVietnameseTones(s.name).includes(qNoTone);
      const matchAddress =
        s.address.toLowerCase().includes(q) ||
        removeVietnameseTones(s.address).includes(qNoTone);
      const matchCategory = s.categoryName
        ? s.categoryName.toLowerCase().includes(q) ||
          removeVietnameseTones(s.categoryName).includes(qNoTone)
        : false;
      return matchName || matchAddress || matchCategory;
    });
  }

  if (sortBy.value === "distance") {
    return [...list].sort((a, b) => a.distanceKm - b.distanceKm);
  } else if (sortBy.value === "rating") {
    return [...list].sort((a, b) => {
      const diffRating = (b.rating || 0) - (a.rating || 0);
      if (Math.abs(diffRating) > 0.01) return diffRating;
      return (b.reviewsCount || 0) - (a.reviewsCount || 0);
    });
  } else {
    return [...list].sort((a, b) => {
      const diffRating = (b.rating || 0) - (a.rating || 0);
      if (Math.abs(diffRating) > 0.01) return diffRating;
      return b.totalProducts - a.totalProducts;
    });
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
  toast.success(`Đã thêm "${p.name}" (${p.unit}) vào giỏ hàng!`, 'Giỏ Hàng ZoneMart');
};

// Reset bộ lọc
const resetFilters = () => {
  searchQuery.value = "";
  selectedCategory.value = "all";
  selectedSubCategory.value = "all";
  selectedPrepType.value = "all";
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

            <div class="spotlight-features-row">
              <div class="feature-chip">
                <i class="bi bi-lightning-charge-fill text-warning"></i>
                <span>Giao hỏa tốc 20 phút</span>
              </div>
              <div class="feature-chip">
                <i class="bi bi-patch-check-fill text-success"></i>
                <span>Nông sản chuẩn VietGAP</span>
              </div>
              <div class="feature-chip">
                <i class="bi bi-shield-check text-primary"></i>
                <span>Đồng kiểm khi nhận hàng</span>
              </div>
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
      </section>

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
        <div class="active-query-meta" v-if="searchQuery || selectedStoreFilter || (selectedCategory !== 'all' && selectedSubCategory !== 'all') || selectedPrepType !== 'all'">
          <span v-if="searchQuery" class="active-query-tag">
            <span>Từ khóa: "<strong>{{ searchQuery }}</strong>"</span>
            <button class="btn-remove-tag" @click="searchQuery = ''" title="Bỏ từ khóa"><i class="bi bi-x"></i></button>
          </span>
          <span v-if="selectedSubCategory !== 'all'" class="active-query-tag subcat-tag">
            <span>Nhóm hàng: <strong>{{ subCategoryDefinitions.find(s => s.id === selectedSubCategory && s.parentCategory === selectedCategory)?.name || selectedSubCategory }}</strong></span>
            <button class="btn-remove-tag" @click="selectedSubCategory = 'all'" title="Bỏ chọn nhóm hàng"><i class="bi bi-x"></i></button>
          </span>
          <span v-if="selectedPrepType !== 'all'" class="active-query-tag prep-tag">
            <span>Trạng thái: <strong>{{ prepTypeFilters.find(f => f.id === selectedPrepType)?.name || selectedPrepType }}</strong></span>
            <button class="btn-remove-tag" @click="selectedPrepType = 'all'" title="Bỏ trạng thái"><i class="bi bi-x"></i></button>
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
        <!-- Category Pills (Cấp 1 - Chỉ hiện khi ở tab Sản Phẩm) -->
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
          <span class="store-intro-text"><i class="bi bi-geo-alt-fill text-orange me-1"></i> Các gian hàng uy tín phục vụ giao siêu tốc</span>
        </div>

        <!-- Sort controls -->
        <div class="sort-action-group">
          <label class="sort-label" for="sort-select">Sắp xếp:</label>
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

      <!-- 3B. PHÂN TẦNG CẤP 2 (SUBCATEGORIES THEO DANH MỤC CON) -->
      <transition name="fade">
        <section v-if="activeSearchTab === 'product' && availableSubCategories.length > 0" class="subcategory-pills-row">
          <span class="subcat-title-tag"><i class="bi bi-tags-fill text-orange"></i> Nhóm hàng:</span>
          <div class="subcat-scroll-track">
            <button
              v-for="sub in availableSubCategories"
              :key="sub.id"
              :class="['subcat-pill-btn', { active: selectedSubCategory === sub.id }]"
              @click="selectedSubCategory = sub.id"
            >
              <i class="bi" :class="sub.icon" aria-hidden="true"></i>
              <span class="subcat-name">{{ sub.name }}</span>
              <span class="subcat-count">{{ sub.count }}</span>
            </button>
          </div>
        </section>
      </transition>

      <!-- 3C. BỘ LỌC NGỮ CẢNH: CHẾ BIẾN SỐNG / ĐỒ CHÍN ĂN LIỀN (VÍ DỤ: GÀ SỐNG VS GÀ RÁN KFC) -->
      <transition name="fade">
        <section
          v-if="activeSearchTab === 'product' && availablePrepTypes.length > 1 && (searchQuery || selectedCategory !== 'all')"
          class="prep-filter-bar"
        >
          <span class="prep-title-tag"><i class="bi bi-funnel-fill text-orange"></i> Trạng thái món:</span>
          <div class="prep-chips-track">
            <button
              v-for="pType in availablePrepTypes"
              :key="pType.id"
              :class="['prep-chip-btn', { active: selectedPrepType === pType.id }]"
              @click="selectedPrepType = pType.id"
            >
              <i class="bi" :class="pType.icon"></i>
              <span>{{ pType.name }}</span>
              <span class="prep-count">({{ pType.count }})</span>
            </button>
          </div>
        </section>
      </transition>

      <!-- 4. ĐẶT LẠI BỘ LỌC (KHI CÓ BỘ LỌC ĐANG ÁP DỤNG) -->
      <div
        v-if="(activeSearchTab === 'product' && (selectedCategory !== 'all' || selectedSubCategory !== 'all' || selectedPrepType !== 'all')) || searchQuery || selectedStoreFilter || sortBy !== 'popular'"
        class="filter-meta-status justify-content-end"
      >
        <button
          class="btn-reset-filter"
          @click="resetFilters"
        >
          <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
          <span>Đặt lại bộ lọc</span>
        </button>
      </div>

      <!-- 5A. TAB SẢN PHẨM: LƯỚI SẢN PHẨM KHÔNG KHUNG VIỀN (TASTE-SKILL PRODUCT GRID) -->
      <div v-if="activeSearchTab === 'product'">
        <!-- Skeleton Loading Shimmer khi tải dữ liệu sản phẩm -->
        <div v-if="isCatalogLoading" class="products-tactile-grid skeleton-grid">
          <div v-for="n in 8" :key="'skel-p-' + n" class="skeleton-card">
            <div class="skeleton-shimmer skeleton-media"></div>
            <div class="skeleton-body">
              <div class="skeleton-shimmer skeleton-store-tag"></div>
              <div class="skeleton-shimmer skeleton-title"></div>
              <div class="skeleton-shimmer skeleton-rating"></div>
              <div class="skeleton-bottom-row">
                <div class="skeleton-shimmer skeleton-price"></div>
                <div class="skeleton-shimmer skeleton-btn"></div>
              </div>
            </div>
          </div>
        </div>

        <div v-else-if="filteredProducts.length > 0" class="products-tactile-grid loaded-products-anim">
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
                <span>{{ p.deliveryTime }}</span>
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
                <span class="store-name-text" :title="p.storeName" @click.stop="selectStore(p.storeName)">
                  <i class="bi bi-shop store-inline-icon" aria-hidden="true"></i>
                  {{ p.storeName }}
                </span>
                <span class="category-name-chip">{{ p.categoryName }}</span>
                <span v-if="p.prepType === 'cooked'" class="prep-badge cooked" title="Món nóng ăn ngay, phục vụ liền">
                  <i class="bi bi-fire"></i> Đồ chín
                </span>
                <span v-else-if="p.prepType === 'raw'" class="prep-badge raw" title="Thực phẩm tươi sống">
                  <i class="bi bi-snow"></i> Tươi sống
                </span>
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
            Không có sản phẩm nào khớp với tìm kiếm "<strong>{{ searchQuery }}</strong>".
          </p>
          <!-- Nếu đang lọc danh mục khác mà có kết quả ở các danh mục còn lại -->
          <div v-if="selectedCategory !== 'all' && totalMatchesWithoutCategory > 0" class="empty-cross-action-box mb-3">
            <p class="cross-hint-msg">
              <i class="bi bi-info-circle-fill text-orange me-1"></i> Có <strong>{{ totalMatchesWithoutCategory }}</strong> sản phẩm khớp từ khóa ở các danh mục khác!
            </p>
            <button class="btn-switch-cross-tab" @click="selectedCategory = 'all'">
              <i class="bi bi-grid-fill me-1"></i>
              <span>Xem tất cả danh mục ({{ totalMatchesWithoutCategory }} món)</span>
              <i class="bi bi-arrow-right ms-1"></i>
            </button>
          </div>

          <div v-if="selectedCategory === 'all' || totalMatchesWithoutCategory === 0" class="mt-3">
            <button class="btn-restore-filters" @click="resetFilters">
              <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
              <span>Xem tất cả sản phẩm</span>
            </button>
          </div>
        </div>
      </div>

      <!-- 5B. TAB GIAN HÀNG: LƯỚI GIAN HÀNG TẬN NƠI (TASTE-SKILL STORE CARDS) -->
      <div v-else>
        <!-- Skeleton Loading Shimmer khi tải danh sách gian hàng -->
        <div v-if="isCatalogLoading" class="stores-tactile-grid skeleton-grid">
          <div v-for="n in 4" :key="'skel-s-' + n" class="skeleton-store-card">
            <div class="skeleton-shimmer skeleton-store-cover"></div>
            <div class="skeleton-store-body">
              <div class="skeleton-shimmer skeleton-store-avatar"></div>
              <div class="skeleton-store-lines">
                <div class="skeleton-shimmer skeleton-title" style="width: 70%;"></div>
                <div class="skeleton-shimmer skeleton-rating" style="width: 45%;"></div>
                <div class="skeleton-shimmer skeleton-store-tag" style="width: 85%;"></div>
              </div>
            </div>
          </div>
        </div>

        <div v-else-if="filteredStores.length > 0" class="stores-tactile-grid loaded-products-anim">
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
                <span>{{ store.deliveryTime }}</span>
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

        <!-- Empty State khi không tìm thấy gian hàng -->
        <div v-else class="empty-results-box">
          <div class="empty-icon-circle">
            <i class="bi bi-shop" aria-hidden="true"></i>
          </div>
          <h3>Không tìm thấy gian hàng nào!</h3>
          <p>
            Không có gian hàng nào khớp với tìm kiếm "<strong>{{ searchQuery }}</strong>".
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
  min-height: calc(100vh - 70px);
  background-color: #faf7f2;
  color: #2b1b14;
  padding: 16px clamp(16px, 3.5vw, 48px) 80px clamp(16px, 3.5vw, 48px);
  box-sizing: border-box;
}

.catalog-container {
  width: 100%;
  max-width: 100%;
  margin: 0;
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
   1. BENTO SPOTLIGHT
   ========================================================================== */
.bento-spotlight-section {
  display: block;
  margin-bottom: 28px;
}

.bento-card-main {
  background: #ffffff;
  border: 1.5px solid #ebdcd3;
  border-radius: 20px;
  padding: 28px 36px;
  display: grid;
  grid-template-columns: 1.35fr 0.95fr;
  gap: 28px;
  align-items: center;
  box-shadow: 0 4px 18px rgba(43, 27, 20, 0.04);
}

.live-gps-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #fff8f3;
  color: #d85a2a;
  border: 1px solid #fed7aa;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}

.gps-dot {
  width: 8px;
  height: 8px;
  background-color: #d85a2a;
  border-radius: 50%;
  animation: pulse-ring 1.8s infinite;
}

@keyframes pulse-ring {
  0% { transform: scale(0.9); opacity: 1; }
  50% { transform: scale(1.4); opacity: 0.5; }
  100% { transform: scale(0.9); opacity: 1; }
}

.spotlight-title {
  margin: 0 0 10px 0;
  font-size: 30px;
  font-weight: 900;
  line-height: 1.2;
  letter-spacing: -0.5px;
  color: #2b1b14;
}

.text-gradient-warm {
  background: linear-gradient(90deg, #d85a2a 0%, #ea580c 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.spotlight-desc {
  font-size: 13px;
  line-height: 1.6;
  color: #64748b;
  margin: 0 0 18px 0;
}

.spotlight-features-row {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 14px;
}

.feature-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #334155;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 12px;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
}

.bento-visual-side {
  position: relative;
  height: 100%;
  min-height: 200px;
  max-height: 250px;
  border-radius: 16px;
  overflow: hidden;
}

.bento-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 16px;
  border: none;
}

.store-floating-tag {
  position: absolute;
  bottom: 12px;
  left: 12px;
  background: rgba(43, 27, 20, 0.88);
  backdrop-filter: blur(10px);
  color: #ffffff;
  padding: 6px 12px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11.5px;
}
.store-floating-tag .icon {
  font-size: 16px;
}
.store-floating-tag strong {
  display: block;
}
.store-floating-tag small {
  color: #cbd5e1;
}

/* Sub bento card */
.bento-card-sub {
  background: linear-gradient(135deg, #fffaf5 0%, #ffeedd 100%);
  border: 1.5px solid #eed8cb;
  border-radius: 20px;
  padding: 26px 28px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  box-shadow: 0 4px 18px rgba(43, 27, 20, 0.04);
}

.sub-badge-speed {
  display: inline-block;
  align-self: flex-start;
  font-size: 11px;
  font-weight: 800;
  color: #d85a2a;
  background: #ffffff;
  border: 1px solid #fed7aa;
  padding: 4px 12px;
  border-radius: 12px;
  margin-bottom: 12px;
}

.sub-card-title {
  margin: 0 0 8px 0;
  font-size: 20px;
  font-weight: 800;
  color: #2b1b14;
}

.sub-card-desc {
  font-size: 13px;
  line-height: 1.5;
  color: #5d4a40;
  margin: 0 0 16px 0;
}

/* Radius Slider Box */
.radius-slider-box {
  background: #ffffff;
  padding: 16px;
  border-radius: 16px;
  border: 1px solid #eed8cb;
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
  gap: 14px;
  margin-bottom: 18px;
}

.search-mode-tabs-wrap {
  display: inline-flex;
  background: #ffffff;
  padding: 5px;
  border-radius: 16px;
  border: 1.5px solid #ebdcd3;
  box-shadow: 0 2px 10px rgba(43, 27, 20, 0.03);
  gap: 6px;
}

.search-mode-tab-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  background: transparent;
  padding: 8px 20px;
  border-radius: 12px;
  font-size: 13.5px;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  transition: all 0.2s ease;
}

.search-mode-tab-btn:hover {
  color: #d85a2a;
  background: #fff8f3;
}

.search-mode-tab-btn.active {
  background: #d85a2a;
  color: #ffffff;
  box-shadow: 0 4px 12px rgba(216, 90, 42, 0.25);
}

.tab-count-pill {
  font-size: 11px;
  padding: 2px 7px;
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
  background: #fff8f3;
  color: #d85a2a;
  border: 1px solid #fed7aa;
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

.active-query-tag.subcat-tag {
  background: #fdf2f8;
  color: #9d174d;
  border-color: #fbcfe8;
}

.active-query-tag.prep-tag {
  background: #f0fdf4;
  color: #15803d;
  border-color: #bbf7d0;
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
  gap: 14px;
  margin-bottom: 18px;
}

.category-pills-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.cat-pill-btn {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 12px;
  padding: 7px 14px;
  font-size: 12.5px;
  font-weight: 700;
  color: #4a3830;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 7px;
  box-shadow: 0 2px 6px rgba(43, 27, 20, 0.02);
  transition: all 0.2s ease;
}

.cat-pill-btn:hover {
  background: #fff8f3;
  color: #d85a2a;
  border-color: #f08c5c;
}

.cat-pill-btn.active {
  background: #d85a2a;
  color: #ffffff;
  border-color: #d85a2a;
  box-shadow: 0 4px 12px rgba(216, 90, 42, 0.22);
}

.cat-pill-count {
  font-size: 10.5px;
  background: rgba(0, 0, 0, 0.06);
  padding: 1px 6px;
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
  color: #7b6960;
}

.sort-dropdown-select {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  padding: 7px 14px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  color: #2b1b14;
  outline: none;
  cursor: pointer;
}
.sort-dropdown-select:focus {
  border-color: #d85a2a;
}

/* ==========================================================================
   2B. SUBCATEGORY PILLS (PHÂN TẦNG CẤP 2)
   ========================================================================== */
.subcategory-pills-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 10px;
  margin-bottom: 12px;
  padding: 10px 16px;
  background: #fdfaf8;
  border: 1px dashed #eed9cf;
  border-radius: 14px;
  flex-wrap: wrap;
}

.subcat-title-tag {
  font-size: 12px;
  font-weight: 700;
  color: #925338;
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.subcat-scroll-track {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  align-items: center;
}

.subcat-pill-btn {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 20px;
  padding: 5px 12px;
  font-size: 11.5px;
  font-weight: 600;
  color: #5d4941;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s cubic-bezier(0.2, 0.8, 0.2, 1);
}

.subcat-pill-btn:hover {
  background: #fff3ec;
  border-color: #f08c5c;
  color: #d85a2a;
  transform: translateY(-1px);
}

.subcat-pill-btn.active {
  background: #2b1b14;
  border-color: #2b1b14;
  color: #ffffff;
  box-shadow: 0 3px 8px rgba(43, 27, 20, 0.18);
}

.subcat-count {
  font-size: 10px;
  background: rgba(0, 0, 0, 0.05);
  padding: 1px 6px;
  border-radius: 10px;
}

.subcat-pill-btn.active .subcat-count {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
}

/* ==========================================================================
   2C. CONTEXTUAL PREP FILTER (LỌC TRẠNG THÁI SỐNG VS CHÍN ĂN LIỀN)
   ========================================================================== */
.prep-filter-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 6px;
  margin-bottom: 14px;
  padding: 8px 14px;
  background: #ffffff;
  border: 1px solid #eedcd3;
  border-radius: 12px;
  flex-wrap: wrap;
  box-shadow: 0 2px 8px rgba(43, 27, 20, 0.03);
}

.prep-title-tag {
  font-size: 11.5px;
  font-weight: 700;
  color: #7b6960;
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.prep-chips-track {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.prep-chip-btn {
  background: #fbf9f8;
  border: 1px solid #e7d8cf;
  border-radius: 8px;
  padding: 4px 10px;
  font-size: 11.5px;
  font-weight: 600;
  color: #55443c;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.18s ease;
}

.prep-chip-btn:hover {
  background: #fff4ee;
  border-color: #f08c5c;
  color: #d85a2a;
}

.prep-chip-btn.active {
  background: #d85a2a;
  border-color: #d85a2a;
  color: #ffffff;
  box-shadow: 0 2px 6px rgba(216, 90, 42, 0.2);
}

.prep-count {
  font-size: 10px;
  color: #9c8a80;
}

.prep-chip-btn.active .prep-count {
  color: rgba(255, 255, 255, 0.9);
}

/* Badge hiển thị trên từng Product Card */
.prep-badge {
  font-size: 10px;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 3px;
  margin-left: 4px;
}

.prep-badge.cooked {
  background: #feece5;
  color: #c93b12;
  border: 1px solid #f9cdb9;
}

.prep-badge.raw {
  background: #eef6ff;
  color: #1971c2;
  border: 1px solid #cfe2ff;
}

/* Meta status line */
.filter-meta-status {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 22px;
  font-size: 13px;
  color: #64748b;
}

.filter-meta-status.justify-content-end {
  justify-content: flex-end;
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
  .products-tactile-grid,
  .stores-tactile-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 960px) {
  .bento-spotlight-section {
    grid-template-columns: 1fr;
  }
  .products-tactile-grid,
  .stores-tactile-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .product-catalog-page {
    padding: 12px 14px 60px 14px;
  }
  .search-mode-tabs-wrap {
    width: 100%;
    display: flex;
  }
  .search-mode-tab-btn {
    flex: 1;
    justify-content: center;
    padding: 9px 12px;
  }
  .filter-controls-bar {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
  }
  .sort-action-group {
    justify-content: space-between;
    width: 100%;
  }
}

@media (max-width: 600px) {
  .bento-card-main {
    grid-template-columns: 1fr;
    padding: 18px 16px;
    gap: 16px;
  }
  .bento-visual-side {
    height: 150px;
    min-height: 150px;
  }
  .bento-card-sub {
    padding: 18px 16px;
  }
  .spotlight-title {
    font-size: 22px;
    line-height: 1.25;
  }
  .spotlight-desc {
    font-size: 12.5px;
    margin-bottom: 12px;
  }
  .category-pills-row,
  .subcategory-pills-row,
  .prep-filter-bar {
    flex-wrap: nowrap;
    overflow-x: auto;
    padding-bottom: 6px;
    -webkit-overflow-scrolling: touch;
    scrollbar-width: none;
  }
  .category-pills-row::-webkit-scrollbar,
  .subcategory-pills-row::-webkit-scrollbar,
  .prep-filter-bar::-webkit-scrollbar {
    display: none;
  }
  .subcat-scroll-track,
  .prep-chips-track {
    flex-wrap: nowrap;
    flex-shrink: 0;
  }
  .cat-pill-btn,
  .subcat-pill-btn,
  .prep-chip-btn {
    flex-shrink: 0;
    padding: 6px 12px;
    font-size: 12px;
  }
  .products-tactile-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 10px;
  }
  .stores-tactile-grid {
    grid-template-columns: 1fr;
    gap: 14px;
  }
  .product-card-media {
    height: 120px;
  }
  .product-card-body {
    padding: 10px 8px 8px 8px;
  }
  .product-item-title {
    font-size: 12px;
    line-height: 1.35;
    min-height: 32px;
    margin-bottom: 3px;
    -webkit-line-clamp: 2;
  }
  .product-rating-meta {
    margin-bottom: 6px;
    font-size: 10px;
    gap: 4px;
  }
  .product-price-bottom-row {
    flex-direction: column;
    align-items: stretch;
    gap: 6px;
  }
  .price-val {
    font-size: 13.5px;
  }
  .old-price-val {
    font-size: 10px;
  }
  .btn-add-tactile {
    width: 100%;
    justify-content: center;
    padding: 6px 8px;
    font-size: 11px;
    border-radius: 8px;
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

/* SKELETON SHIMMER LOADING & STAGGER ANIMATION */
.skeleton-card {
  background: #ffffff;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 4px 16px rgba(15, 23, 42, 0.05);
  border: 1px solid #f1f5f9;
  display: flex;
  flex-direction: column;
}

.skeleton-shimmer {
  background: linear-gradient(
    90deg,
    #f1f5f9 0%,
    #e2e8f0 50%,
    #f1f5f9 100%
  );
  background-size: 200% 100%;
  animation: shimmerWave 1.4s infinite ease-in-out;
  border-radius: 8px;
}

@keyframes shimmerWave {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

.skeleton-media {
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: 0;
}

.skeleton-body {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.skeleton-store-tag {
  width: 50%;
  height: 14px;
}

.skeleton-title {
  width: 85%;
  height: 20px;
}

.skeleton-rating {
  width: 40%;
  height: 14px;
}

.skeleton-bottom-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 6px;
}

.skeleton-price {
  width: 45%;
  height: 22px;
}

.skeleton-btn {
  width: 60px;
  height: 32px;
  border-radius: 12px;
}

.skeleton-store-card {
  background: #ffffff;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 4px 16px rgba(15, 23, 42, 0.05);
  border: 1px solid #f1f5f9;
}

.skeleton-store-cover {
  width: 100%;
  height: 140px;
  border-radius: 0;
}

.skeleton-store-body {
  padding: 16px;
  display: flex;
  gap: 14px;
  align-items: flex-start;
}

.skeleton-store-avatar {
  width: 52px;
  height: 52px;
  min-width: 52px;
  border-radius: 50%;
}

.skeleton-store-lines {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* Stagger animation on loaded */
.loaded-products-anim {
  animation: fadeInGrid 0.4s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

@keyframes fadeInGrid {
  from {
    opacity: 0;
    transform: translateY(12px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
