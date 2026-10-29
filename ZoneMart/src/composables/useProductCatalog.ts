import { ref, computed } from "vue";
import { useProductModeration, fetchRealProductsFromBackend } from "./useProductModeration";
import { apiFetch } from "../utils/apiConfig";
import { getCleanAvatar } from "../utils/avatarHelper";
import { currentUserCoords } from "./useSafeLocation";

// Bảng tọa độ GPS thực tế của các cơ sở & gian hàng đối tác ZoneMart tại Hà Nội
export const STORE_COORDINATES_MAP: Record<string, { lat: number; lng: number; address: string }> = {
  "store_vuon_rau_bac_ba": { lat: 21.0371, lng: 105.7766, address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội" },
  "vườn rau hữu cơ bác ba": { lat: 21.0371, lng: 105.7766, address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội" },
  "store_nong_trai_xanh_lac_duong": { lat: 21.0315, lng: 105.7876, address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội" },
  "nông trại xanh lạc dương": { lat: 21.0315, lng: 105.7876, address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội" },
  "store_vua_trai_cay_sau_thao": { lat: 21.0182, lng: 105.8015, address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội" },
  "vựa trái cây sáu thảo miền tây": { lat: 21.0182, lng: 105.8015, address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội" },
  "store_thuc_pham_tuoi_song_zonemart": { lat: 21.0345, lng: 105.7960, address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội" },
  "thực phẩm tươi sống zonemart cầu giấy": { lat: 21.0345, lng: 105.7960, address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội" },
  "gian hàng zonemart": { lat: 21.0345, lng: 105.7960, address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội" },
  "test gian hàng": { lat: 21.0345, lng: 105.7960, address: "206 Cầu Giấy, Hà Nội" },
  "store_bep_com_nieu_co_ba": { lat: 21.0116, lng: 105.7942, address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội" },
  "bếp cơm niêu & ẩm thực nóng cô ba": { lat: 21.0116, lng: 105.7942, address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội" },
  "store_tiem_banh_mi_zone_sang": { lat: 21.0366, lng: 105.7832, address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội" },
  "tiệm bánh mì & cà phê zone sáng": { lat: 21.0366, lng: 105.7832, address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội" },
  "store_hop_tac_xa_tay_bac": { lat: 21.0289, lng: 105.7712, address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội" },
  "hợp tác xã đặc sản vùng cao tây bắc": { lat: 21.0289, lng: 105.7712, address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội" },
  "store_tong_kho_my_dinh": { lat: 21.0388, lng: 105.7645, address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội" },
  "tổng kho đồ gia dụng & tiêu dùng mỹ đình": { lat: 21.0388, lng: 105.7645, address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội" },
  "store_chuc_son": { lat: 20.9250, lng: 105.7050, address: "Khu Chợ Chúc Sơn, Thị trấn Chúc Sơn, Huyện Chương Mỹ, Hà Nội" },
  "nông sản sạch chúc sơn - chương mỹ": { lat: 20.9250, lng: 105.7050, address: "Khu Chợ Chúc Sơn, Thị trấn Chúc Sơn, Huyện Chương Mỹ, Hà Nội" },
  "store_yen_nghia": { lat: 20.9385, lng: 105.7250, address: "Số 12 Đường Biên Giang, Phường Đồng Mai, Hà Đông, Hà Nội" },
  "bếp nóng yên nghĩa & biên giang": { lat: 20.9385, lng: 105.7250, address: "Số 12 Đường Biên Giang, Phường Đồng Mai, Hà Đông, Hà Nội" },
  "store_ba_la": { lat: 20.9580, lng: 105.7610, address: "Ngã 3 Ba La, Phường Phú Lãm, Quận Hà Đông, Hà Nội" },
  "vựa thịt sạch & hải sản ba la - hà đông": { lat: 20.9580, lng: 105.7610, address: "Ngã 3 Ba La, Phường Phú Lãm, Quận Hà Đông, Hà Nội" },
  "store_zone_fruits": { lat: 20.9392, lng: 105.7310, address: "Số 1368 Đường Quang Trung, Yên Nghĩa, Hà Đông, Hà Nội" },
  "tiệm trái cây & nước ép zone fruits": { lat: 20.9392, lng: 105.7310, address: "Số 1368 Đường Quang Trung, Yên Nghĩa, Hà Đông, Hà Nội" },
  "nổi loạn 123456": { lat: 20.9390, lng: 105.7311, address: "1368 Đường Quang Trung, Đồng Mai, Yên Nghĩa, Hà Nội" }
};

export function resolveStoreCoordinates(store: { id?: string; name?: string; address?: string; lat?: number; lng?: number }): { lat: number; lng: number } {
  if (store.lat && store.lng && store.lat > 0 && store.lng > 0) {
    return { lat: store.lat, lng: store.lng };
  }
  const idKey = (store.id || "").toLowerCase().trim();
  const nameKey = (store.name || "").toLowerCase().trim();
  const found = STORE_COORDINATES_MAP[idKey] || STORE_COORDINATES_MAP[nameKey];
  if (found) {
    return { lat: found.lat, lng: found.lng };
  }
  const addr = (store.address || "").toLowerCase();
  for (const [k, v] of Object.entries(STORE_COORDINATES_MAP)) {
    const street = v.address.split(",")[0].toLowerCase().trim();
    if (addr.includes(street)) {
      return { lat: v.lat, lng: v.lng };
    }
  }
  return { lat: 21.0345, lng: 105.7960 };
}

// Công thức Haversine tính khoảng cách đại vòng tròn chuẩn xác theo tọa độ GPS (km)
export function calculateHaversineDistance(
  lat1: number,
  lon1: number,
  lat2: number,
  lon2: number
): number {
  if (!lat1 || !lon1 || !lat2 || !lon2) return 1.2;
  const R = 6371; // Bán kính Trái Đất (km)
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLon = ((lon2 - lon1) * Math.PI) / 180;
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  const dist = R * c;
  return Math.round(dist * 10) / 10;
}

// Ước tính thời gian giao hàng thực tế theo khoảng cách km
export function estimateDeliveryTime(distanceKm: number): string {
  if (distanceKm <= 1.0) return "10 - 15 phút";
  if (distanceKm <= 2.0) return "12 - 18 phút";
  if (distanceKm <= 3.5) return "18 - 25 phút";
  if (distanceKm <= 5.0) return "25 - 35 phút";
  if (distanceKm <= 7.0) return "35 - 45 phút";
  if (distanceKm <= 10.0) return "45 - 60 phút";
  return "60 - 90 phút";
}

const catalogVersion = ref(0);
const STORAGE_KEY_DB_STORES = "zonemart_db_stores";

const getInitialStores = (): any[] => {
  if (typeof window === "undefined") return [];
  try {
    const raw = localStorage.getItem(STORAGE_KEY_DB_STORES);
    if (raw) return JSON.parse(raw);
  } catch {}
  return [];
};

const apiStores = ref<any[]>(getInitialStores());
let isFetchingStores = false;

export const fetchDbStores = async () => {
  if (typeof window === "undefined" || isFetchingStores) return;
  isFetchingStores = true;
  try {
    const res = await apiFetch("/api/products/stores").catch(() => null);
    if (res && res.ok) {
      const json = await res.json().catch(() => null);
      if (json && json.success && Array.isArray(json.data)) {
        apiStores.value = json.data;
        try {
          localStorage.setItem(STORAGE_KEY_DB_STORES, JSON.stringify(json.data));
        } catch {}
        catalogVersion.value++;
      }
    }
  } catch (err) {
    console.warn("Lỗi fetchDbStores:", err);
  } finally {
    isFetchingStores = false;
  }
};

const STORAGE_KEY_RATINGS_SUMMARY = "zonemart_ratings_summary";

const getInitialRatingsSummary = (): any => {
  if (typeof window === "undefined") return { products: {}, stores: {}, shippers: {} };
  try {
    const raw = localStorage.getItem(STORAGE_KEY_RATINGS_SUMMARY);
    if (raw) return JSON.parse(raw);
  } catch {}
  return { products: {}, stores: {}, shippers: {} };
};

export const ratingsSummary = ref<any>(getInitialRatingsSummary());
let isFetchingRatings = false;

export const fetchRatingsSummary = async () => {
  if (typeof window === "undefined" || isFetchingRatings) return;
  isFetchingRatings = true;
  try {
    const res = await apiFetch("/api/orders/reviews/summary").catch(() => null);
    if (res && res.ok) {
      const json = await res.json().catch(() => null);
      if (json && json.success) {
        ratingsSummary.value = {
          products: json.products || {},
          stores: json.stores || {},
          shippers: json.shippers || {}
        };
        try {
          localStorage.setItem(STORAGE_KEY_RATINGS_SUMMARY, JSON.stringify(ratingsSummary.value));
        } catch {}
        catalogVersion.value++;
      }
    }
  } catch (err) {
    console.warn("Lỗi fetchRatingsSummary:", err);
  } finally {
    isFetchingRatings = false;
  }
};

export const recordLocalReview = (review: {
  orderId: string;
  storeName: string;
  storeId?: string;
  storeRating?: number;
  shipperRating?: number;
  productReviews?: Array<{ productId: string; productName: string; rating: number; comment?: string; tags?: string[] }>;
  storeComment?: string;
  buyerName?: string;
}) => {
  const current = ratingsSummary.value || { products: {}, stores: {}, shippers: {} };
  const products = { ...(current.products || {}) };
  const stores = { ...(current.stores || {}) };

  // 1. Cập nhật Đánh giá Gian hàng
  if (review.storeName || review.storeId) {
    const sKeys = [
      (review.storeId || "").trim().toLowerCase(),
      (review.storeName || "").trim().toLowerCase()
    ].filter(Boolean);

    for (const key of sKeys) {
      const prev = stores[key] || { total: 0, count: 0, averageRating: 5.0, comments: [] };
      const sRate = review.storeRating || 5;
      const newTotal = prev.total + sRate;
      const newCount = prev.count + 1;
      stores[key] = {
        total: newTotal,
        count: newCount,
        averageRating: Math.round((newTotal / newCount) * 10) / 10,
        comments: [
          {
            author: review.buyerName || "Khách Hàng ZoneMart",
            avatar: review.buyerAvatar || getCleanAvatar(null, review.buyerName, 'buyer'),
            rating: sRate,
            comment: review.storeComment || "",
            date: new Date().toLocaleDateString("vi-VN")
          },
          ...(prev.comments || [])
        ]
      };
    }
  }

  // 2. Cập nhật Đánh giá Từng Sản phẩm
  if (review.productReviews && Array.isArray(review.productReviews)) {
    for (const pr of review.productReviews) {
      const pKeys = [
        (pr.productId || "").trim().toLowerCase(),
        (pr.productName || "").trim().toLowerCase()
      ].filter(Boolean);

      for (const key of pKeys) {
        const prev = products[key] || { total: 0, count: 0, averageRating: 5.0, comments: [] };
        const newTotal = prev.total + pr.rating;
        const newCount = prev.count + 1;
        products[key] = {
          total: newTotal,
          count: newCount,
          averageRating: Math.round((newTotal / newCount) * 10) / 10,
          comments: [
            {
              author: review.buyerName || "Khách Hàng ZoneMart",
              avatar: review.buyerAvatar || getCleanAvatar(null, review.buyerName, 'buyer'),
              rating: pr.rating,
              comment: pr.comment || "",
              tags: pr.tags || [],
              date: new Date().toLocaleDateString("vi-VN")
            },
            ...(prev.comments || [])
          ]
        };
      }
    }
  }

  ratingsSummary.value = {
    ...current,
    products,
    stores
  };

  if (typeof window !== "undefined") {
    try {
      localStorage.setItem(STORAGE_KEY_RATINGS_SUMMARY, JSON.stringify(ratingsSummary.value));
    } catch {}
    window.dispatchEvent(new CustomEvent("zonemart:ratings-updated"));
    window.dispatchEvent(new CustomEvent("zonemart:products-changed"));
  }
  catalogVersion.value++;
};

if (typeof window !== "undefined") {
  setTimeout(() => {
    fetchDbStores();
    fetchRealProductsFromBackend();
    fetchRatingsSummary();
  }, 50);
  window.addEventListener("zonemart:products-changed", () => {
    catalogVersion.value++;
  });
  window.addEventListener("zonemart:ratings-updated", () => {
    catalogVersion.value++;
  });
  window.addEventListener("storage", (e) => {
    if (
      e.key === "zonemart_moderated_products" ||
      e.key === "zonemart_product_bus_event" ||
      e.key === STORAGE_KEY_DB_STORES ||
      e.key === STORAGE_KEY_RATINGS_SUMMARY
    ) {
      if (e.key === STORAGE_KEY_RATINGS_SUMMARY) {
        try {
          ratingsSummary.value = JSON.parse(e.newValue || "{}");
        } catch {}
      }
      catalogVersion.value++;
    }
  });
}

export interface ProductReview {
  id: string;
  author: string;
  avatar: string;
  rating: number;
  date: string;
  comment: string;
  photos?: string[];
  helpfulCount: number;
  verified: boolean;
}

export interface CatalogProduct {
  id: string;
  distanceKm?: number;
  name: string;
  category: "food" | "veggie" | "fastfood" | "beverage" | "household";
  categoryName: string;
  subCategory?: string;
  prepType?: "raw" | "cooked" | "fresh" | "packaged";
  price: number;
  oldPrice?: number;
  discountBadge?: string;
  unit: string;
  image: string;
  gallery: string[];
  badge?: string;
  certification?: {
    type: "VietGAP" | "OCOP 4 Sao" | "Organic" | "ATTP Quốc Gia";
    certNo: string;
    issuedBy: string;
    issuedDate: string;
    expiryDate: string;
  };
  store: {
    id: string;
    name: string;
    address: string;
    lat?: number;
    lng?: number;
    distanceKm: number;
    deliveryTime: string;
    rating: number;
    reviewsCount?: number;
    openHours?: string;
    totalProducts: number;
    isVerified: boolean;
    categoryName?: string;
    avatar?: string;
    coverImage?: string;
  };
  rating: number;
  reviewsCount: number;
  sold: number;
  stock: number;
  description: string;
  highlights: string[];
  specs: {
    origin: string;
    brand: string;
    weight: string;
    shelfLife: string;
    storage: string;
    packingStandard: string;
  };
  nutrition: {
    servingSize: string;
    calories: string;
    protein: string;
    fat: string;
    carbs: string;
    minerals: string;
  };
  cookingTips: string[];
  reviews: ProductReview[];
  aiAudit?: {
    safetyScore: number;
    matchScore: number;
    verificationDate: string;
    inspector: string;
    notes: string;
  };
}

export interface CatalogStore {
  id: string;
  name: string;
  address: string;
  distanceKm: number;
  deliveryTime: string;
  rating: number;
  reviewsCount?: number;
  totalProducts: number;
  isVerified: boolean;
  categoryName?: string;
  avatar: string;
  coverImage: string;
  openHours?: string;
  products: CatalogProduct[];
}

// HÀM TỰ ĐỘNG PHÂN TẦNG VÀ PHÂN LOẠI TRẠNG THÁI CHẾ BIẾN (CHUẨN HÓA DANH MỤC CẤP 2 VÀ TRẠNG THÁI SỐNG/CHÍN)
export function inferSubCategoryAndPrep(product: {
  name: string;
  category?: string;
  subCategory?: string;
  prepType?: string;
}): { subCategory: string; prepType: "raw" | "cooked" | "fresh" | "packaged" } {
  if (product.subCategory && product.prepType) {
    return {
      subCategory: product.subCategory,
      prepType: product.prepType as "raw" | "cooked" | "fresh" | "packaged",
    };
  }
  const name = (product.name || "").toLowerCase();
  const cat = (product.category || "").toLowerCase();

  // 1. Phân loại Gà & Gia cầm (Gà rán KFC vs Thịt gà sống sơ chế)
  if (
    name.includes("gà rán") ||
    name.includes("kfc") ||
    name.includes("popcorn") ||
    name.includes("chiên giòn") ||
    name.includes("lắc phô mai")
  ) {
    return { subCategory: "fried_chicken", prepType: "cooked" };
  }
  if (
    name.includes("cơm gà") ||
    name.includes("gà nướng") ||
    name.includes("gà xối mỡ") ||
    name.includes("cháo gà")
  ) {
    return { subCategory: "fried_chicken", prepType: "cooked" };
  }
  if (
    name.includes("gà") ||
    name.includes("cánh gà") ||
    name.includes("đùi gà") ||
    name.includes("ức gà") ||
    name.includes("vịt")
  ) {
    if (
      cat === "fastfood" ||
      name.includes("chín") ||
      name.includes("luộc") ||
      name.includes("nóng")
    ) {
      return { subCategory: "fried_chicken", prepType: "cooked" };
    }
    return { subCategory: "poultry", prepType: "raw" };
  }

  // 2. Thịt heo & Thịt bò
  if (
    name.includes("bò") ||
    name.includes("heo") ||
    name.includes("lợn") ||
    name.includes("sườn") ||
    name.includes("ba chỉ") ||
    name.includes("trâu")
  ) {
    if (
      cat === "fastfood" ||
      name.includes("cơm") ||
      name.includes("nướng") ||
      name.includes("kho")
    ) {
      return { subCategory: "rice_noodle", prepType: "cooked" };
    }
    return { subCategory: "beef_pork", prepType: "raw" };
  }

  // 3. Cá & Hải sản tươi
  if (
    name.includes("cá ") ||
    name.includes("tôm") ||
    name.includes("mực") ||
    name.includes("hải sản") ||
    name.includes("cua")
  ) {
    if (cat === "fastfood" || name.includes("canh chua") || name.includes("canh cua")) {
      return { subCategory: "rice_noodle", prepType: "cooked" };
    }
    return { subCategory: "seafood", prepType: "raw" };
  }

  // 4. Trứng & Đậu phụ
  if (name.includes("trứng") || name.includes("đậu phụ") || name.includes("đậu hũ")) {
    if (cat === "fastfood" || name.includes("bánh bao")) {
      return { subCategory: "breakfast_bread", prepType: "cooked" };
    }
    return { subCategory: "egg_tofu", prepType: "raw" };
  }

  // 5. Cơm, Bún, Phở nóng & Món ăn gia đình
  if (
    name.includes("cơm") ||
    name.includes("bún") ||
    name.includes("phở") ||
    name.includes("mì") ||
    name.includes("canh")
  ) {
    return { subCategory: "rice_noodle", prepType: "cooked" };
  }
  if (
    name.includes("bánh mì") ||
    name.includes("bánh bao") ||
    name.includes("xôi") ||
    name.includes("pate")
  ) {
    return { subCategory: "breakfast_bread", prepType: "cooked" };
  }

  // 6. Rau củ VietGAP
  if (
    name.includes("xà lách") ||
    name.includes("rau muống") ||
    name.includes("cải") ||
    name.includes("mồng tơi") ||
    name.includes("rau")
  ) {
    return { subCategory: "leafy", prepType: "fresh" };
  }
  if (
    name.includes("cà chua") ||
    name.includes("bí") ||
    name.includes("khoai") ||
    name.includes("cà rốt") ||
    name.includes("bầu") ||
    name.includes("bắp") ||
    name.includes("ngô")
  ) {
    return { subCategory: "root_fruit", prepType: "fresh" };
  }
  if (
    name.includes("nấm") ||
    name.includes("tỏi") ||
    name.includes("hành") ||
    name.includes("gừng") ||
    name.includes("ớt")
  ) {
    return { subCategory: "mushroom_spices", prepType: "fresh" };
  }

  // 7. Trái cây & Đồ uống
  if (
    name.includes("dâu") ||
    name.includes("bơ sáp") ||
    name.includes("táo") ||
    name.includes("cam") ||
    name.includes("xoài") ||
    name.includes("sầu riêng") ||
    name.includes("hoa quả") ||
    name.includes("trái cây")
  ) {
    return { subCategory: "fruit", prepType: "fresh" };
  }
  if (
    name.includes("cà phê") ||
    name.includes("trà ") ||
    name.includes("cafe")
  ) {
    return { subCategory: "tea_coffee", prepType: "cooked" };
  }
  if (name.includes("nước ép") || name.includes("sinh tố")) {
    return { subCategory: "juice_smoothie", prepType: "fresh" };
  }

  // 8. Đồ gia dụng & Nhu yếu phẩm
  if (
    name.includes("nồi") ||
    name.includes("chảo") ||
    name.includes("hộp") ||
    name.includes("bát") ||
    name.includes("đĩa") ||
    name.includes("bình")
  ) {
    return { subCategory: "kitchen", prepType: "packaged" };
  }
  if (
    name.includes("nước rửa") ||
    name.includes("giặt") ||
    name.includes("lau") ||
    name.includes("tẩy")
  ) {
    return { subCategory: "cleaning", prepType: "packaged" };
  }
  if (
    name.includes("gạo") ||
    name.includes("mật ong") ||
    name.includes("muối") ||
    name.includes("đường") ||
    name.includes("mắm")
  ) {
    return { subCategory: "kitchen", prepType: "packaged" };
  }

  // Mặc định an toàn theo category
  if (cat === "fastfood") return { subCategory: "snack", prepType: "cooked" };
  if (cat === "food") return { subCategory: "poultry", prepType: "raw" };
  if (cat === "veggie") return { subCategory: "leafy", prepType: "fresh" };
  if (cat === "beverage") return { subCategory: "juice_smoothie", prepType: "fresh" };
  return { subCategory: "kitchen", prepType: "packaged" };
}

// 8 GIAN HÀNG CHÍNH THỨC VỚI 32 SẢN PHẨM HOÀN TOÀN ĐỘC NHẤT, THÔNG SỐ VÀ CHỈ SỐ KHÁC BIỆT 100%
export const CATALOG_PRODUCTS: CatalogProduct[] = [
  // ==========================================
  // GIAN HÀNG 1: Vườn Rau Hữu Cơ Bác Ba
  // ==========================================
  {
    id: "p1",
    name: "Rau Xà Lách Mỡ Thủy Canh VietGAP",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 35000,
    oldPrice: 45000,
    discountBadge: "-22%",
    unit: "1kg",
    image: "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Bán chạy #1",
    certification: {
      type: "VietGAP",
      certNo: "VG-HN-2026-0812",
      issuedBy: "Sở Nông Nghiệp & PTNT Hà Nội",
      issuedDate: "12/01/2026",
      expiryDate: "12/01/2027"
    },
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 168,
    sold: 389,
    stock: 80,
    description: "Xà lách mỡ canh tác theo phương pháp thủy canh tuần hoàn khép kín tại trang trại Ba Vì. Lá rau dày, giòn ngọt, hoàn toàn không sử dụng thuốc trừ sâu hay chất kích thích sinh trưởng.",
    highlights: [
      "Thu hoạch tươi mới 2 cữ mỗi ngày (sáng sớm & đầu giờ chiều)",
      "Không tồn dư thuốc BVTV, đạt chuẩn VietGAP",
      "Giữ trọn vị giòn mát, bảo quản ngăn mát 5-7 ngày"
    ],
    specs: {
      origin: "Ba Vì, Hà Nội",
      brand: "Vườn Rau Bác Ba",
      weight: "1kg (3 - 4 cây)",
      shelfLife: "5-7 ngày trong ngăn mát tủ lạnh",
      storage: "Bọc giấy báo hoặc màng bọc thực phẩm, để ngăn rau củ",
      packingStandard: "Túi màng thở sinh học tự phân hủy"
    },
    nutrition: {
      servingSize: "100g",
      calories: "15 kcal",
      protein: "1.4 g",
      fat: "0.2 g",
      carbs: "2.9 g",
      minerals: "Vitamin A (148% DV), Vitamin K, Folate"
    },
    cookingTips: ["Rửa nhẹ nhàng, ngâm nước đá 3 phút trước khi ăn để tăng độ giòn khi làm salad."],
    reviews: [
      {
        id: "rv101",
        author: "Chị Mai Lan",
        avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "Hôm nay 08:30",
        comment: "Rau rất tươi, cuống còn chảy mủ trắng, giao nhanh trong 15 phút là tới!",
        helpfulCount: 15,
        verified: true
      }
    ]
  },
  {
    id: "p2",
    name: "Rau Muống Hữu Cơ Ba Vì Non Xanh",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 18000,
    oldPrice: 24000,
    discountBadge: "-25%",
    unit: "Bó 500g",
    image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80"],
    badge: "Mới ngắt sáng nay",
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 124,
    sold: 520,
    stock: 120,
    description: "Rau muống ngọn nhỏ, thân giòn đanh, ngắt ngọn non mơn mởn. Luộc nước trong veo, xào tỏi xanh mướt không bị nát hay đen cọng.",
    highlights: ["Tưới bằng nguồn nước giếng khoáng tự nhiên Ba Vì", "Luộc nước xanh ngọt mát tự nhiên", "Đạt chuẩn an toàn tuyệt đối"],
    specs: {
      origin: "Vườn rau hữu cơ Ba Vì, Hà Nội",
      brand: "Vườn Rau Bác Ba",
      weight: "Bó 500g (± 30g)",
      shelfLife: "3-4 ngày",
      storage: "Ngăn mát tủ lạnh",
      packingStandard: "Bó dây lạt rơm truyền thống"
    },
    nutrition: {
      servingSize: "100g",
      calories: "19 kcal",
      protein: "2.6 g",
      fat: "0.2 g",
      carbs: "3.1 g",
      minerals: "Sắt, Canxi, Chất xơ"
    },
    cookingTips: ["Luộc lửa to ngập nước, cho chút muối để rau giữ màu xanh ngọc bích."],
    reviews: []
  },
  {
    id: "p3",
    name: "Cà Chua Bi Socola Ngọt Đậm Vị Hữu Cơ",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 42000,
    oldPrice: 52000,
    discountBadge: "-19%",
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80"],
    badge: "Vị ngọt đặc biệt",
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 92,
    sold: 215,
    stock: 50,
    description: "Giống cà chua bi màu nâu sẫm socola giàu chất chống oxy hóa Anthocyanin. Vỏ mỏng căng mọng, vị ngọt đậm đà, ít hạt.",
    highlights: ["Độ ngọt Brix 9.5 cao gấp đôi cà chua thường", "Trồng giá thể xơ dừa vi sinh sạch sẽ", "Ăn sống như trái cây tráng miệng"],
    specs: {
      origin: "Vườn thực nghiệm công nghệ cao Ba Vì",
      brand: "Vườn Rau Bác Ba",
      weight: "500g",
      shelfLife: "7-10 ngày",
      storage: "Nhiệt độ phòng thoáng mát hoặc ngăn mát",
      packingStandard: "Hộp nhựa có lỗ thông gió"
    },
    nutrition: {
      servingSize: "100g",
      calories: "22 kcal",
      protein: "1.1 g",
      fat: "0.2 g",
      carbs: "4.8 g",
      minerals: "Lycopene, Vitamin C, Kali"
    },
    cookingTips: ["Ngon nhất khi rửa sạch ăn liền hoặc trộn cùng xà lách thủy canh."],
    reviews: []
  },
  {
    id: "p4",
    name: "Bí Xanh Thơm Cắt Khoanh Tiêu Chuẩn VietGAP",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 25000,
    oldPrice: 32000,
    discountBadge: "-21%",
    unit: "Trái 1kg",
    image: "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=800&q=80"],
    badge: "Đậm vị canh quê",
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 65,
    sold: 164,
    stock: 40,
    description: "Bí đao chanh thơm dẻo, ruột đặc ít hạt. Nấu canh sườn, canh tôm ngọt lịm hoặc ép nước thanh nhiệt cơ thể.",
    highlights: ["Ruột xanh dẻo, thơm ngát mùi lá nếp", "Không xơ, không chua ruột", "Đã gọt vỏ và đóng khay hút chân không sạch sẽ"],
    specs: {
      origin: "Ba Vì, Hà Nội",
      brand: "Vườn Rau Bác Ba",
      weight: "1kg (± 50g)",
      shelfLife: "5 ngày trong ngăn mát",
      storage: "Ngăn mát tủ lạnh",
      packingStandard: "Hút chân không khay thực phẩm"
    },
    nutrition: {
      servingSize: "100g",
      calories: "13 kcal",
      protein: "0.6 g",
      fat: "0.1 g",
      carbs: "3.0 g",
      minerals: "Nước 96%, Canxi, Magie"
    },
    cookingTips: ["Nấu canh với tôm nõn hoặc sườn non, rắc chút tiêu và hành ngò thơm."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 2: Nông Trại Xanh Lạc Dương
  // ==========================================
  {
    id: "p5",
    name: "Hộp Dâu Tây Giống Hana Nhật Bản Hái Sáng Nay",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 95000,
    oldPrice: 125000,
    discountBadge: "-24%",
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1518635017480-d471b404ab6e?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Đặc sản Lạc Dương",
    certification: {
      type: "VietGAP",
      certNo: "VG-LD-2026-0914",
      issuedBy: "Sở Nông Nghiệp Lâm Đồng",
      issuedDate: "02/02/2026",
      expiryDate: "02/02/2027"
    },
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 142,
    sold: 310,
    stock: 65,
    description: "Dâu tây giống Nhật Hana trồng nhà kính vùng cao Lạc Dương, không khí sương mờ giúp quả mọng nước, thơm lừng và ngọt sắc.",
    highlights: ["Bay hàng không về Hà Nội trong ngày", "Cuống xanh tươi nguyên phấn dâu", "Đóng hộp chống va đập chuyên dụng"],
    specs: {
      origin: "Lạc Dương, Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "500g (24-28 quả)",
      shelfLife: "3-5 ngày trong tủ lạnh",
      storage: "Để ngăn mát, không rửa khi chưa ăn",
      packingStandard: "Hộp nhựa dập lỗ thoát ẩm"
    },
    nutrition: {
      servingSize: "100g",
      calories: "32 kcal",
      protein: "0.7 g",
      fat: "0.3 g",
      carbs: "7.7 g",
      minerals: "Vitamin C 59mg, Axit Folic"
    },
    cookingTips: ["Ăn kèm sữa chua không đường hoặc làm bánh ngọt."],
    reviews: []
  },
  {
    id: "p6",
    name: "Bơ Sáp 034 Đặc Sản Lâm Đồng Dẻo Quánh",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 75000,
    oldPrice: 90000,
    discountBadge: "-16%",
    unit: "Kg 2-3 trái",
    image: "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=800&q=80"],
    badge: "Bơ đầu mùa tuyển chọn",
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 88,
    sold: 188,
    stock: 45,
    description: "Bơ sáp dáng dài 034 nức tiếng Bảo Lộc - Lạc Dương. Thịt quả vàng ươm dẻo như sáp, hạt tiêu nhỏ tí xíu, béo ngậy tự nhiên.",
    highlights: ["Bơ già cây chín tự nhiên, không ngâm ủ hóa chất", "Tỷ lệ sáp đặc trên 85%", "Bao đổi trả 1 đổi 1 nếu bị sượng"],
    specs: {
      origin: "Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "1kg (2-3 trái dài)",
      shelfLife: "2-4 ngày khi chín",
      storage: "Nhiệt độ phòng chờ chín, chín bỏ ngăn mát",
      packingStandard: "Bọc lưới xốp từng quả"
    },
    nutrition: {
      servingSize: "100g",
      calories: "160 kcal",
      protein: "2.0 g",
      fat: "14.7 g (chất béo lành mạnh)",
      carbs: "8.5 g",
      minerals: "Kali, Vitamin E, Lutein"
    },
    cookingTips: ["Dầm cùng sữa đặc và đá xay, hoặc làm sốt bơ trứng ăn kèm bánh mì."],
    reviews: []
  },
  {
    id: "p7",
    name: "Khoai Lang Mật Đà Lạt Nướng Chảy Mật Thơm Lừng",
    category: "veggie",
    categoryName: "Nông sản tươi",
    price: 45000,
    oldPrice: 55000,
    discountBadge: "-18%",
    unit: "Túi 1kg",
    image: "https://images.unsplash.com/photo-1596097635121-14b63b7a0c19?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1596097635121-14b63b7a0c19?auto=format&fit=crop&w=800&q=80"],
    badge: "Chảy mật ngọt lịm",
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 110,
    sold: 240,
    stock: 70,
    description: "Khoai lang giống Nhật trồng trên đất đỏ bazan Lạc Dương, củ đã ủ đủ ngày nên nướng hoặc luộc là tứa mật óng ả ngọt bùi.",
    highlights: ["Củ thon dài đều tay, không xơ", "Đã ủ xuống đường đạt độ ngọt tối đa", "Hấp nướng nồi chiên không dầu siêu ngon"],
    specs: {
      origin: "Đà Lạt, Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "1kg (4-6 củ)",
      shelfLife: "15 ngày",
      storage: "Để nơi khô ráo, tránh ánh nắng trực tiếp",
      packingStandard: "Túi lưới thông thoáng"
    },
    nutrition: {
      servingSize: "100g",
      calories: "86 kcal",
      protein: "1.6 g",
      fat: "0.1 g",
      carbs: "20.1 g",
      minerals: "Chất xơ, Vitamin A, Mangan"
    },
    cookingTips: ["Nướng nồi chiên không dầu 180°C trong 35 phút để mật tứa ra thơm ngát."],
    reviews: []
  },
  {
    id: "p8",
    name: "Ớt Chuông Sweet Palermo Giòn Ngọt Thanh Mát",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 55000,
    oldPrice: 70000,
    discountBadge: "-21%",
    unit: "Túi 500g",
    image: "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=800&q=80"],
    badge: "Siêu giàu Vitamin C",
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 75,
    sold: 135,
    stock: 35,
    description: "Giống ớt ngọt cao cấp Palermo nhập khẩu từ Hà Lan, trồng công nghệ cao tại Lạc Dương. Không cay nồng, giòn sần sật và mọng nước ngọt thanh.",
    highlights: ["Lượng Vitamin C gấp 3 lần quả cam", "Ăn sống chấm sốt mè rang tuyệt đỉnh", "Trẻ nhỏ cũng thích mê vì không hăng cay"],
    specs: {
      origin: "Lạc Dương, Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "500g (3-4 quả mix màu)",
      shelfLife: "7-10 ngày trong tủ lạnh",
      storage: "Bọc màng thực phẩm để ngăn mát",
      packingStandard: "Khay bọc màng co"
    },
    nutrition: {
      servingSize: "100g",
      calories: "28 kcal",
      protein: "1.0 g",
      fat: "0.2 g",
      carbs: "6.0 g",
      minerals: "Vitamin C 150mg, Vitamin B6"
    },
    cookingTips: ["Cắt lát xào bò hoặc ăn sống kèm sốt mè rang thanh mát."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 3: Vựa Trái Cây Sáu Thảo Miền Tây
  // ==========================================
  {
    id: "p9",
    name: "Bưởi Da Xanh Bến Tre Loại 1 Trái Mọng Tép Đỏ",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 68000,
    oldPrice: 85000,
    discountBadge: "-20%",
    unit: "Trái 1.5kg",
    image: "https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=800&q=80"],
    badge: "Đặc sản Bến Tre",
    certification: {
      type: "VietGAP",
      certNo: "VG-BT-2026-302",
      issuedBy: "Sở NN Bến Tre",
      issuedDate: "10/01/2026",
      expiryDate: "10/01/2027"
    },
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 104,
    sold: 295,
    stock: 80,
    description: "Bưởi da xanh cắt tại vườn Mỏ Cày Bắc (Bến Tre). Vỏ mỏng dính, tép bưởi màu hồng đỏ căng mọng nước, vị ngọt thanh không hề đắng hậu.",
    highlights: ["Tuyển chọn trái từ 1.4kg - 1.6kg tròn đều", "Tép róc dễ bóc, giòn mọng ngọt lịm", "Chưng bàn thờ sang trọng, ăn bổ dưỡng"],
    specs: {
      origin: "Châu Thành, Bến Tre",
      brand: "Vựa Bưởi Sáu Thảo",
      weight: "1.4kg - 1.6kg",
      shelfLife: "20-30 ngày",
      storage: "Nhiệt độ phòng thoáng gió",
      packingStandard: "Bọc túi lưới có quai xách"
    },
    nutrition: {
      servingSize: "100g tép bưởi",
      calories: "38 kcal",
      protein: "0.8 g",
      fat: "0.1 g",
      carbs: "9.6 g",
      minerals: "Vitamin C, Naringin, Kali"
    },
    cookingTips: ["Gọt vỏ tách tép chấm muối tôm Tây Ninh chua cay tê lưỡi."],
    reviews: []
  },
  {
    id: "p10",
    name: "Cam Sành Hàm Yên Mọng Nước Ngọt Tự Nhiên",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 38000,
    oldPrice: 48000,
    discountBadge: "-21%",
    unit: "Kg",
    image: "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80"],
    badge: "Vắt nước cực ngọt",
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 88,
    sold: 410,
    stock: 150,
    description: "Cam sành trái to tròn, vỏ mỏng rám nắng đúng chuẩn cam già cây. Vắt được cực nhiều nước, vị ngọt đậm đà thơm ngát.",
    highlights: ["Cam già cành mọng nước, không sượng xơ", "Vắt nước không cần thêm đường", "Tươi ngon bồi bổ sức khỏe hàng ngày"],
    specs: {
      origin: "Hàm Yên, Tuyên Quang & Vĩnh Long",
      brand: "Vựa Sáu Thảo",
      weight: "1kg (3-4 quả)",
      shelfLife: "7-10 ngày",
      storage: "Nơi râm mát hoặc ngăn mát tủ lạnh",
      packingStandard: "Túi lưới chuyên dụng"
    },
    nutrition: {
      servingSize: "100g nước cam",
      calories: "45 kcal",
      protein: "0.7 g",
      fat: "0.2 g",
      carbs: "10.4 g",
      minerals: "Vitamin C 53mg, Hesperidin"
    },
    cookingTips: ["Vắt lấy nước uống cùng vài viên đá giải nhiệt sảng khoái."],
    reviews: []
  },
  {
    id: "p11",
    name: "Xoài Cát Hòa Lộc Chín Cây Vàng Óng Thơm Lừng",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 85000,
    oldPrice: 105000,
    discountBadge: "-19%",
    unit: "Kg",
    image: "https://images.unsplash.com/photo-1553279768-865429fa0078?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1553279768-865429fa0078?auto=format&fit=crop&w=800&q=80"],
    badge: "Vua của các loài xoài",
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 96,
    sold: 180,
    stock: 40,
    description: "Xoài cát Hòa Lộc Tiền Giang chuẩn gốc. Trái thuôn dài, da vàng mịn màng, thịt quả dày chắc mịn không hề có xơ, ngọt lịm sắc nét.",
    highlights: ["Chín tự nhiên tỏa hương thơm lừng cả gian phòng", "Thịt quả dẻo mịn tan trên đầu lưỡi", "Độ ngọt sắc đặc trưng nức tiếng miền Tây"],
    specs: {
      origin: "Cái Bè, Tiền Giang",
      brand: "Vựa Sáu Thảo",
      weight: "1kg (2 trái)",
      shelfLife: "3-5 ngày khi chín",
      storage: "Nhiệt độ phòng",
      packingStandard: "Bọc xốp chống trầy xước"
    },
    nutrition: {
      servingSize: "100g",
      calories: "60 kcal",
      protein: "0.8 g",
      fat: "0.4 g",
      carbs: "15.0 g",
      minerals: "Vitamin A, Vitamin C, Đồng"
    },
    cookingTips: ["Cắt hạt lựu ăn kèm xôi nếp cốt dừa béo ngậy."],
    reviews: []
  },
  {
    id: "p12",
    name: "Dừa Xiêm Xanh Bến Tre Ngọt Mát Đã Gọt Trọc",
    category: "beverage",
    categoryName: "Đồ uống tươi",
    price: 22000,
    oldPrice: 28000,
    discountBadge: "-21%",
    unit: "Trái",
    image: "https://images.unsplash.com/photo-1550258987-190a2d41a8ba?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1550258987-190a2d41a8ba?auto=format&fit=crop&w=800&q=80"],
    badge: "Ngọt thanh tự nhiên",
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 154,
    sold: 350,
    stock: 90,
    description: "Dừa xiêm xanh bánh tẻ gọt vỏ kim cương tiện lợi. Nước dừa ngọt lịm thanh mát, cùi dừa non mềm dẻo nạo ăn cực ngon miệng.",
    highlights: ["Đã gọt sẵn chỉ cần cắm ống hút là uống", "Nước dừa ngọt thanh tự nhiên 100%", "Giàu chất điện giải bù nước thể thao"],
    specs: {
      origin: "Giồng Trôm, Bến Tre",
      brand: "Vựa Sáu Thảo",
      weight: "1 trái (300-350ml nước)",
      shelfLife: "10-15 ngày trong tủ mát",
      storage: "Bảo quản lạnh",
      packingStandard: "Bọc màng co thực phẩm từng trái"
    },
    nutrition: {
      servingSize: "100ml nước dừa",
      calories: "19 kcal",
      protein: "0.7 g",
      fat: "0.2 g",
      carbs: "3.7 g",
      minerals: "Chất điện giải Kali, Natri, Magie"
    },
    cookingTips: ["Ướp lạnh trước 30 phút uống sảng khoái tột đỉnh."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 4: Thực Phẩm Tươi Sống ZoneMart Cầu Giấy
  // ==========================================
  {
    id: "p13",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng USDA Choice",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 185000,
    oldPrice: 220000,
    discountBadge: "-16%",
    unit: "Khay 500g",
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Thịt mát chuẩn USDA",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "USDA-VN-88429/2026",
      issuedBy: "Cục Thú Y & Kiểm Dịch",
      issuedDate: "15/01/2026",
      expiryDate: "15/01/2027"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 184,
    sold: 560,
    stock: 95,
    description: "Thịt bò Black Angus nhập khẩu Mỹ chuẩn hạng USDA Choice. Vân mỡ cẩm thạch xen kẽ đều tăm tắp, thịt mềm mọng tan trong miệng khi áp chảo hay nướng.",
    highlights: ["100% Thịt bò Black Angus tươi ngon", "Bảo quản công nghệ mát MAP 0-4°C", "Cắt lát dày tiêu chuẩn bít tết hoặc nhúng lẩu"],
    specs: {
      origin: "Hoa Kỳ (Nebraska Farms)",
      brand: "ZoneMart Premium Beef",
      weight: "500g",
      shelfLife: "7 ngày ngăn mát, 6 tháng ngăn đông",
      storage: "Nhiệt độ 0-4°C",
      packingStandard: "Khay sinh học kháng khuẩn hút chân không"
    },
    nutrition: {
      servingSize: "100g",
      calories: "217 kcal",
      protein: "26.1 g",
      fat: "11.8 g",
      carbs: "0 g",
      minerals: "Sắt, Kẽm, Vitamin B12"
    },
    cookingTips: ["Áp chảo 2-3 phút mỗi mặt với bơ tỏi và lá hương thảo."],
    reviews: []
  },
  {
    id: "p14",
    name: "Cá Hồi Na Uy Tươi Phi Lê Cắt Miếng Trong Ngày",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 195000,
    oldPrice: 235000,
    discountBadge: "-17%",
    unit: "Khay 300g",
    image: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=800&q=80"],
    badge: "Ăn sống Sashimi",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "ATTP-QC-2026-99",
      issuedBy: "Cục An Toàn Thực Phẩm",
      issuedDate: "10/01/2026",
      expiryDate: "10/01/2027"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 142,
    sold: 320,
    stock: 50,
    description: "Cá hồi Đại Tây Dương nhập khẩu nguyên con từ vùng biển lạnh Na Uy, phi lê tươi mới mỗi sáng tại kho lạnh ZoneMart. Đạt chuẩn ăn gỏi Sashimi.",
    highlights: ["Thịt cá cam óng vân mỡ trắng ngần", "Giàu Omega-3 và DHA tự nhiên", "Kèm gừng hồng Nhật và nước tương Kikkoman"],
    specs: {
      origin: "Na Uy (Norwegian Salmon)",
      brand: "ZoneMart Seafood",
      weight: "Khay 300g",
      shelfLife: "3 ngày ngăn mát, 3 tháng ngăn đông",
      storage: "0-2°C trong ngăn mát",
      packingStandard: "Khay lót giấy thấm hút chân không"
    },
    nutrition: {
      servingSize: "100g",
      calories: "208 kcal",
      protein: "20.4 g",
      fat: "13.4 g (Omega-3 2.5g)",
      carbs: "0 g",
      minerals: "Vitamin D, B12, Selen"
    },
    cookingTips: ["Ăn sống chấm wasabi hoặc áp chảo sốt bơ chanh cực đỉnh."],
    reviews: []
  },
  {
    id: "p15",
    name: "Thịt Gà Ta Thả Vườn Làm Sạch Nguyên Con",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 160000,
    oldPrice: 195000,
    discountBadge: "-18%",
    unit: "Con 1.4kg",
    image: "https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=800&q=80"],
    badge: "Da vàng thịt săn",
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 115,
    sold: 280,
    stock: 40,
    description: "Gà ta thả đồi Ba Vì ăn ngô thóc, thịt săn chắc, thơm ngọt và da vàng giòn sần sật. Đã mổ moi làm sạch lông khử khuẩn đóng túi lạnh.",
    highlights: ["Không ăn cám tăng trọng, nuôi tự nhiên 6 tháng", "Thịt thơm không bị bở nát", "Tặng kèm lá chanh tươi và muối tiêu hảo hạng"],
    specs: {
      origin: "Ba Vì, Hà Nội",
      brand: "Gà Đồi Ba Vì",
      weight: "1.3kg - 1.5kg / con",
      shelfLife: "3 ngày ngăn mát, 1 tháng ngăn đông",
      storage: "Bảo quản 0-4°C",
      packingStandard: "Đóng túi hút chân không"
    },
    nutrition: {
      servingSize: "100g thịt gà",
      calories: "165 kcal",
      protein: "31.0 g",
      fat: "3.6 g",
      carbs: "0 g",
      minerals: "Kẽm, Magie, Vitamin B6"
    },
    cookingTips: ["Luộc lửa nhỏ 20 phút cùng vài lát gừng, ngâm nước đá cho da giòn."],
    reviews: []
  },
  {
    id: "p16",
    name: "Cánh Gà Tươi CP Loại 1 Đóng Khay Tiệt Trùng",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 72000,
    oldPrice: 85000,
    discountBadge: "-15%",
    unit: "Khay 500g",
    image: "https://images.unsplash.com/photo-1567620832903-9fc6debc209f?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1567620832903-9fc6debc209f?auto=format&fit=crop&w=800&q=80"],
    badge: "Chuẩn tiệt trùng CP",
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 160,
    sold: 390,
    stock: 65,
    description: "Cánh gà tươi giữa khúc ngon nhất, da mỏng ít mỡ. Thích hợp làm món cánh gà chiên mắm, nướng mật ong hay sốt me chua cay.",
    highlights: ["Chuỗi khép kín 3F Feed-Farm-Food CP", "Tươi mới không đông lạnh nhiều lần", "Đều cánh, thịt dày mọng nước"],
    specs: {
      origin: "Việt Nam (Hệ thống CP)",
      brand: "CP Fresh Meat",
      weight: "Khay 500g (5-6 khúc cánh)",
      shelfLife: "5 ngày ngăn mát",
      storage: "Nhiệt độ 0-4°C",
      packingStandard: "Khay MAP kín khí"
    },
    nutrition: {
      servingSize: "100g",
      calories: "203 kcal",
      protein: "18.3 g",
      fat: "14.4 g",
      carbs: "0 g",
      minerals: "Collagen, Phốt pho, Sắt"
    },
    cookingTips: ["Khứa nhẹ cánh ướp mắm tỏi ớt 20 phút chiên giòn rụm."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 5: Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba
  // ==========================================
  {
    id: "p17",
    name: "Cơm Tấm Sườn Bì Chả Đặc Biệt Kèm Canh Chua",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 55000,
    oldPrice: 65000,
    discountBadge: "-15%",
    unit: "Suất",
    image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80"],
    badge: "Bán chạy giờ trưa",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "ATTP-HN-5512",
      issuedBy: "Chi Cục ATTP Hà Nội",
      issuedDate: "15/12/2025",
      expiryDate: "15/12/2026"
    },
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 220,
    sold: 680,
    stock: 80,
    description: "Đĩa cơm tấm gạo thơm dẻo, miếng sườn cốt lết nướng than hoa mật ong xém cạnh thơm nức mũi, chả trứng chưng vàng óng, bì sợi giòn dai và mỡ hành tóp mỡ béo ngậy.",
    highlights: ["Sườn ướp công thức gia truyền nướng than hoa", "Gạo tấm thơm chuẩn vị Sài Gòn", "Đóng hộp giữ nhiệt giao nóng hổi"],
    specs: {
      origin: "Chế biến tươi tại Bếp Cô Ba Cầu Giấy",
      brand: "Cơm Tấm Cô Ba",
      weight: "Suất 550g",
      shelfLife: "Dùng ngon trong 45 phút",
      storage: "Dùng ngay khi còn nóng",
      packingStandard: "Hộp bã mía giữ nhiệt 3 ngăn sạch sẽ"
    },
    nutrition: {
      servingSize: "1 suất đầy đủ",
      calories: "680 kcal",
      protein: "32 g",
      fat: "24 g",
      carbs: "82 g",
      minerals: "Đầy đủ dưỡng chất năng lượng"
    },
    cookingTips: ["Chan nước mắm ớt tỏi chua ngọt ngập miếng sườn để vị đậm đà nhất."],
    reviews: []
  },
  {
    id: "p18",
    name: "Cơm Gà Xối Mỡ Da Giòn Nóng Hổi Sốt Tỏi Ớt",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 50000,
    oldPrice: 60000,
    discountBadge: "-17%",
    unit: "Suất",
    image: "https://images.unsplash.com/photo-1562967914-608f82629710?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1562967914-608f82629710?auto=format&fit=crop&w=800&q=80"],
    badge: "Da giòn thịt mềm",
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.7,
    reviewsCount: 185,
    sold: 510,
    stock: 75,
    description: "Đùi gà góc tư chiên xối mỡ giòn rụm lớp da ngoài, thịt bên trong mềm ngọt mọng nước chấm sốt tỏi ớt đặc chế ăn cùng cơm đảo cà chua đỏ au thơm nức.",
    highlights: ["Gà xối mỡ da giòn rụm không ngấy dầu", "Cơm rang tơi xốp đỏ cam màu cà chua", "Kèm dưa chuột muối chua và canh rong biển"],
    specs: {
      origin: "Bếp Cô Ba Hà Nội",
      brand: "Cơm Tấm Cô Ba",
      weight: "Suất 500g",
      shelfLife: "Dùng nóng trong 45 phút",
      storage: "Dùng liền khi nhận",
      packingStandard: "Hộp giấy giữ nhiệt thân thiện môi trường"
    },
    nutrition: {
      servingSize: "1 suất",
      calories: "650 kcal",
      protein: "35 g",
      fat: "22 g",
      carbs: "75 g",
      minerals: "Protein, Sắt, Kẽm"
    },
    cookingTips: ["Chấm sốt ớt chua cay tự làm của quán để giải ngấy tuyệt đối."],
    reviews: []
  },
  {
    id: "p_kfc_chicken",
    name: "Gà Rán Giòn Cay Chuẩn Vị KFC (2 Miếng Đùi & Cánh)",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    subCategory: "fried_chicken",
    prepType: "cooked",
    price: 69000,
    oldPrice: 85000,
    discountBadge: "-19%",
    unit: "Phần 2 miếng",
    image: "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=800&q=80"],
    badge: "Giao nóng giòn 20p",
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.8,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 26,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 245,
    sold: 580,
    stock: 65,
    description: "Gà rán vỏ giòn rụm màu vàng ruộm, thịt bên trong mềm mọng nước đậm vị tiêu ớt chuẩn công thức KFC. Luôn được chiên mới khi có đơn và bọc giấy bạc giữ nhiệt giao ngay.",
    highlights: ["Chiên mới 100% khi nhận đơn", "Vỏ ngoài giòn tan, thịt mọng nước", "Tặng kèm tương cà & tương ớt"],
    specs: {
      origin: "ZoneMart Kitchen",
      brand: "Gà Rán ZoneMart",
      weight: "Phần 350g",
      shelfLife: "Dùng ngon nhất trong vòng 2 giờ",
      storage: "Bảo quản nóng, ăn ngay",
      packingStandard: "Hộp giấy kraft cách nhiệt kèm sốt"
    },
    nutrition: {
      servingSize: "2 miếng",
      calories: "520 kcal",
      protein: "38 g",
      fat: "28 g",
      carbs: "22 g",
      minerals: "Natri, Kali, Phốt pho"
    },
    cookingTips: ["Ăn ngay khi còn nóng giòn để cảm nhận trọn vẹn hương vị."],
    reviews: []
  },
  {
    id: "p19",
    name: "Canh Cua Rau Đay Cà Pháo Nấu Nồi Đất Chuẩn Vị",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 40000,
    oldPrice: 50000,
    discountBadge: "-20%",
    unit: "Tô lớn",
    image: "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80"],
    badge: "Món ngon quê nhà",
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 130,
    sold: 290,
    stock: 45,
    description: "Cua đồng giã tay đóng gạch dày cộp nổi tảng, nấu với rau đay, mướp hương thơm nức mũi. Kèm bát cà pháo giòn tan chống ngấy ngày hè.",
    highlights: ["100% Cua đồng tự nhiên giã tay", "Gạch cua đóng bánh dày ngọt lịm", "Kèm hũ cà muối giòn tan truyền thống"],
    specs: {
      origin: "Bếp Cô Ba Hà Nội",
      brand: "Cơm Tấm Cô Ba",
      weight: "Tô 600ml kèm cà pháo",
      shelfLife: "Dùng trong ngày",
      storage: "Ăn nóng",
      packingStandard: "Tô giấy giữ nhiệt chịu nhiệt cao"
    },
    nutrition: {
      servingSize: "1 tô",
      calories: "180 kcal",
      protein: "14 g",
      fat: "6 g",
      carbs: "18 g",
      minerals: "Canxi tự nhiên dồi dào"
    },
    cookingTips: ["Ăn kèm cơm trắng nóng hổi và vài quả cà pháo giòn tan."],
    reviews: []
  },
  {
    id: "p20",
    name: "Trà Tắc Mật Ong Khổng Lồ Mát Lạnh Giải Nhiệt",
    category: "beverage",
    categoryName: "Đồ uống",
    price: 18000,
    oldPrice: 25000,
    discountBadge: "-28%",
    unit: "Ly 700ml",
    image: "https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80"],
    badge: "Siêu giải nhiệt",
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 245,
    sold: 740,
    stock: 120,
    description: "Trà lài ủ lạnh thơm nức pha cùng nước cốt tắc tươi và mật ong hoa rừng nguyên chất. Chua ngọt sảng khoái, đập tan cơn khát ngày hè oi ả.",
    highlights: ["Mật ong rừng tự nhiên thanh dịu", "Tắc tươi vắt liền không đắng vỏ", "Cốc khổng lồ 700ml uống thỏa thích"],
    specs: {
      origin: "Bếp Cô Ba Hà Nội",
      brand: "Cô Ba Drinks",
      weight: "Ly 700ml",
      shelfLife: "Uống ngon nhất trong 3 tiếng",
      storage: "Để lạnh",
      packingStandard: "Ly PP nắp kín dán màng ép nhiệt"
    },
    nutrition: {
      servingSize: "1 ly",
      calories: "120 kcal",
      protein: "0.5 g",
      fat: "0 g",
      carbs: "30 g",
      minerals: "Vitamin C, Chất chống oxy hóa EGCG"
    },
    cookingTips: ["Lắc đều với đá lạnh trước khi cắm ống hút thưởng thức."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 6: Tiệm Bánh Mì & Cà Phê Zone Sáng
  // ==========================================
  {
    id: "p21",
    name: "Combo Bánh Mì Chảo Nóng Hổi Kèm Pate & Xúc Xích",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 45000,
    oldPrice: 55000,
    discountBadge: "-18%",
    unit: "Phần",
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80"],
    badge: "Ăn sáng điểm 10",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 180,
    sold: 490,
    stock: 60,
    description: "Chảo bánh mì nóng hổi gồm trứng ốp la lòng đào, pate gan Hải Phòng tự làm béo ngậy, xúc xích giòn rụm và sốt cà chua đậm đà kèm bánh mì nướng giòn tan.",
    highlights: ["Pate nhà làm thơm bùi ngậy béo", "Bánh mì nướng nóng hổi giòn rụm", "Giao nhanh chỉ 15 phút kịp giờ làm"],
    specs: {
      origin: "Xuân Thủy, Cầu Giấy, Hà Nội",
      brand: "Tiệm Bánh Mì Zone",
      weight: "Phần 450g",
      shelfLife: "30-40 phút",
      storage: "Ăn nóng ngay",
      packingStandard: "Hộp bã mía giữ nhiệt cao cấp"
    },
    nutrition: {
      servingSize: "1 suất",
      calories: "520 kcal",
      protein: "24 g",
      fat: "22 g",
      carbs: "58 g",
      minerals: "Canxi, Sắt, Vitamin A"
    },
    cookingTips: ["Chấm bánh mì ngập sốt pate trứng lòng đào béo ngậy."],
    reviews: []
  },
  {
    id: "p22",
    name: "Bánh Mì Kẹp Thịt Nướng Ngũ Vị Giòn Rụm",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 28000,
    oldPrice: 35000,
    discountBadge: "-20%",
    unit: "Ổ",
    image: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"],
    badge: "Siêu đắt khách",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 230,
    sold: 820,
    stock: 150,
    description: "Ổ bánh mì đặc ruột vỏ giòn tan kẹp ngập tràn thịt nạc vai nướng sả ngũ vị thơm lừng, dưa leo giòn mát, đồ chua chua ngọt và sốt bơ trứng nhà làm.",
    highlights: ["Thịt nướng xém cạnh thơm nức mũi", "Pate bơ mịn màng béo thơm", "Bánh giòn không bị vụn nát"],
    specs: {
      origin: "Cầu Giấy, Hà Nội",
      brand: "Tiệm Bánh Mì Zone",
      weight: "Ổ 220g",
      shelfLife: "Ăn ngon trong 2 tiếng",
      storage: "Nhiệt độ phòng",
      packingStandard: "Túi giấy thực phẩm thân thiện môi trường"
    },
    nutrition: {
      servingSize: "1 ổ",
      calories: "410 kcal",
      protein: "19 g",
      fat: "14 g",
      carbs: "52 g",
      minerals: "Protein, Chất xơ"
    },
    cookingTips: ["Ăn kèm chút tương ớt cay nồng bùng nổ hương vị."],
    reviews: []
  },
  {
    id: "p23",
    name: "Cà Phê Sữa Đá Pha Phin Đậm Đà Chuẩn Gu",
    category: "beverage",
    categoryName: "Đồ uống",
    price: 22000,
    oldPrice: 28000,
    discountBadge: "-21%",
    unit: "Cốc 450ml",
    image: "https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?auto=format&fit=crop&w=800&q=80"],
    badge: "Chuẩn vị pha phin",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 190,
    sold: 610,
    stock: 100,
    description: "Hạt Robusta Buôn Ma Thuột rang mộc mạc pha phin nhôm truyền thống, hòa quyện sữa đặc béo ngậy. Đậm đà đánh thức mọi giác quan bắt đầu ngày làm việc mới.",
    highlights: ["100% Cà phê nguyên chất không tẩm bắp", "Vị đắng đầm ấm hậu ngọt sâu", "Đá riêng đảm bảo cà phê không bị nhạt"],
    specs: {
      origin: "Buôn Ma Thuột, Đắk Lắk",
      brand: "Zone Morning Coffee",
      weight: "Cốc 450ml",
      shelfLife: "Uống ngon nhất trong ngày",
      storage: "Bảo quản lạnh",
      packingStandard: "Cốc giấy nắp tim quấn màng bảo vệ"
    },
    nutrition: {
      servingSize: "1 cốc",
      calories: "160 kcal",
      protein: "3.5 g",
      fat: "4.0 g",
      carbs: "28 g",
      minerals: "Caffeine 120mg"
    },
    cookingTips: ["Khuấy đều đá và sữa để cảm nhận độ sánh mịn đặc trưng."],
    reviews: []
  },
  {
    id: "p24",
    name: "Bánh Bao Trứng Muối Xá Xíu Nóng Hổi Vừa Ra Lò",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 20000,
    oldPrice: 25000,
    discountBadge: "-20%",
    unit: "Cái",
    image: "https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80"],
    badge: "Vỏ xốp nhân ngập",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 110,
    sold: 340,
    stock: 50,
    description: "Vỏ bánh trắng mịn xốp mềm ngòn ngọt, nhân thịt nạc vai xá xíu đậm đà, mộc nhĩ nấm hương cùng nguyên 1 quả trứng muối bùi bùi béo ngậy.",
    highlights: ["Hấp nóng trực tiếp trong tủ giữ nhiệt", "Nhân đầy đặn không ngấy mỡ", "Ăn sáng vừa nhanh gọn vừa chắc dạ"],
    specs: {
      origin: "Cầu Giấy, Hà Nội",
      brand: "Tiệm Bánh Mì Zone",
      weight: "Cái 180g",
      shelfLife: "Trong ngày",
      storage: "Ăn nóng",
      packingStandard: "Túi giấy giữ nhiệt lót lá chuối"
    },
    nutrition: {
      servingSize: "1 cái",
      calories: "320 kcal",
      protein: "14 g",
      fat: "10 g",
      carbs: "44 g",
      minerals: "Chất đạm, Canxi"
    },
    cookingTips: ["Dùng ngay khi bánh còn bốc khói ngun ngút."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 7: Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc
  // ==========================================
  {
    id: "p25",
    name: "Mật Ong Rừng Hoa Cà Phê Gia Lai Nguyên Chất 100%",
    category: "food",
    categoryName: "Đặc sản vùng cao",
    price: 135000,
    oldPrice: 165000,
    discountBadge: "-18%",
    unit: "Chai 500ml",
    image: "https://images.unsplash.com/photo-1587049352851-8d4e89133924?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1587049352851-8d4e89133924?auto=format&fit=crop&w=800&q=80"],
    badge: "OCOP 4 Sao",
    certification: {
      type: "OCOP 4 Sao",
      certNo: "OCOP-GL-2026-88",
      issuedBy: "UBND Tỉnh Gia Lai",
      issuedDate: "18/01/2026",
      expiryDate: "18/01/2029"
    },
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 135,
    sold: 375,
    stock: 70,
    description: "Mật ong hoa cà phê nguyên chất mùa hoa nở rộ đất Tây Nguyên. Màu vàng cánh gián óng ả, vị ngọt sắc thanh dịu không gắt cổ, đặc quánh thơm lừng.",
    highlights: ["Không pha đường, không đun nấu cô đặc", "Chai thủy tinh nút bần cao cấp", "Bảo quản tự nhiên không biến đổi chất"],
    specs: {
      origin: "Ia Grai, Gia Lai",
      brand: "HTX Tây Bắc Farm",
      weight: "Chai thủy tinh 500ml",
      shelfLife: "2 năm",
      storage: "Để nơi thoáng mát, không để tủ lạnh",
      packingStandard: "Chai thủy tinh niêm phong nắp thiếc"
    },
    nutrition: {
      servingSize: "1 muỗng (20g)",
      calories: "64 kcal",
      protein: "0.1 g",
      fat: "0 g",
      carbs: "17.3 g",
      minerals: "Chất kháng khuẩn tự nhiên, Enzyme"
    },
    cookingTips: ["Pha với nước ấm và chanh tươi uống buổi sáng thanh lọc đường ruột."],
    reviews: []
  },
  {
    id: "p26",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
    category: "food",
    categoryName: "Nhu yếu phẩm",
    price: 175000,
    oldPrice: 210000,
    discountBadge: "-17%",
    unit: "Túi 5kg",
    image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=800&q=80"],
    badge: "Gạo ngon nhất thế giới",
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 290,
    sold: 820,
    stock: 110,
    description: "Gạo ST25 lúa tôm Sóc Trăng chuẩn tem chống giả. Hạt gạo dài trắng trong, khi nấu tỏa hương lá dứa thơm nức, cơm dẻo dai ngọt hạt dù để nguội.",
    highlights: ["Gạo đạt danh hiệu Gạo ngon nhất thế giới", "Canh tác luân canh lúa - tôm sạch an toàn", "Bao bì chống ẩm giữ trọn hương vị"],
    specs: {
      origin: "Mỹ Xuyên, Sóc Trăng",
      brand: "Gạo Ông Cua Chính Hãng",
      weight: "Túi 5kg",
      shelfLife: "12 tháng",
      storage: "Bảo quản nơi khô ráo, tránh ẩm ướt",
      packingStandard: "Túi màng nhôm bảo quản có khóa zip"
    },
    nutrition: {
      servingSize: "100g gạo",
      calories: "349 kcal",
      protein: "8.0 g",
      fat: "0.8 g",
      carbs: "77.5 g",
      minerals: "Chỉ số đường huyết GI thấp"
    },
    cookingTips: ["Đong tỉ lệ 1 bát gạo : 1 bát nước, không cần ngâm gạo trước khi nấu."],
    reviews: []
  },
  {
    id: "p27",
    name: "Nấm Hương Rừng Sa Pa Thơm Nức Phơi Tự Nhiên",
    category: "food",
    categoryName: "Nông sản khô",
    price: 85000,
    oldPrice: 110000,
    discountBadge: "-23%",
    unit: "Gói 200g",
    image: "https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&w=800&q=80"],
    badge: "Nấm hương chân nhỏ",
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 82,
    sold: 165,
    stock: 45,
    description: "Nấm hương rừng mọc tự nhiên trên thân cây gỗ mục vùng cao Sa Pa. Cánh nấm dày, viền uốn cong, khi ngâm nở thơm ngát đặc trưng không nấm trồng nào sánh được.",
    highlights: ["Phơi nắng tự nhiên trên sườn đồi Hoàng Liên", "Thịt nấm giòn ngọt thơm nức mũi", "Nấu canh măng, xào gà hay nhồi thịt đều đỉnh"],
    specs: {
      origin: "Sa Pa, Lào Cai",
      brand: "HTX Tây Bắc Farm",
      weight: "Gói 200g",
      shelfLife: "18 tháng",
      storage: "Nơi khô ráo",
      packingStandard: "Túi zip tráng bạc chống ẩm"
    },
    nutrition: {
      servingSize: "100g",
      calories: "296 kcal",
      protein: "14.5 g",
      fat: "1.2 g",
      carbs: "65.0 g",
      minerals: "Lentinan, Vitamin D tự nhiên"
    },
    cookingTips: ["Ngâm nước ấm 20 phút, giữ lại nước ngâm nấm để nấu canh ngọt lịm."],
    reviews: []
  },
  {
    id: "p28",
    name: "Thịt Trâu Gác Bếp Sơn La Chuẩn Vị Hạt Dổi Mắc Khén",
    category: "food",
    categoryName: "Đặc sản vùng cao",
    price: 240000,
    oldPrice: 290000,
    discountBadge: "-17%",
    unit: "Gói 300g",
    image: "https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?auto=format&fit=crop&w=800&q=80"],
    badge: "Đặc sản Tây Bắc",
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 145,
    sold: 290,
    stock: 35,
    description: "Thịt bắp trâu tươi ướp mắc khén, hạt dổi rừng, ớt bột và hun khói than củi nhãn theo bí quyết người Thái đen. Từng thớ thịt đỏ hồng đượm khói, xé sợi chấm chẩm chéo cực đã.",
    highlights: ["100% Thịt bắp trâu tươi tươi ngon", "Hun khói củi nhãn truyền thống", "Tặng kèm hũ chẩm chéo ướt Tây Bắc"],
    specs: {
      origin: "Mộc Châu, Sơn La",
      brand: "HTX Tây Bắc Farm",
      weight: "Gói 300g hút chân không",
      shelfLife: "6 tháng ngăn đông",
      storage: "Ngăn đá tủ lạnh",
      packingStandard: "Hút chân không túi đôi"
    },
    nutrition: {
      servingSize: "100g",
      calories: "240 kcal",
      protein: "48 g",
      fat: "4.5 g",
      carbs: "2 g",
      minerals: "Giàu đạm, ít chất béo"
    },
    cookingTips: ["Quay lò vi sóng 30 giây hoặc hấp cách thủy 5 phút cho mềm rồi đập dập xé sợi."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 8: Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình
  // ==========================================
  {
    id: "p29",
    name: "Bộ Nồi Inox 3 Đáy Cao Cấp Nấu Mọi Loại Bếp Từ",
    category: "household",
    categoryName: "Đồ gia dụng",
    price: 420000,
    oldPrice: 520000,
    discountBadge: "-19%",
    unit: "Bộ 3 nồi",
    image: "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1585670270608-b404fb8802a6?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Bảo hành 5 năm",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.75,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Hóa phẩm hữu cơ",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 118,
    sold: 190,
    stock: 25,
    description: "Bộ 3 nồi Inox SUS304 chuẩn an toàn sức khỏe kích thước 16-20-24cm. Đáy 3 lớp truyền nhiệt nhanh, tỏa nhiệt đều, chống cháy khét thức ăn trên bếp từ và bếp gas.",
    highlights: ["Chất liệu Inox 304 sáng bóng không gỉ sét", "Đáy từ bắt nhiệt siêu nhạy", "Vung kính cường lực viền inox sang trọng"],
    specs: {
      origin: "Việt Nam (Tiêu chuẩn xuất khẩu Châu Âu)",
      brand: "Mỹ Đình Smart Home",
      weight: "Bộ 3 nồi kèm vung (3.8kg)",
      shelfLife: "Độ bền trên 10 năm",
      storage: "Vệ sinh lau khô sau khi sử dụng",
      packingStandard: "Thùng carton chèn xốp định hình"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Chứng nhận ATTP LFGB Đức"
    },
    cookingTips: ["Dùng giẻ mềm lau rửa để giữ độ bóng loáng như gương."],
    reviews: []
  },
  {
    id: "p30",
    name: "Nước Rửa Bát Hữu Cơ Tinh Dầu Quế & Chanh Gừng",
    category: "household",
    categoryName: "Đồ dùng gia đình",
    price: 48000,
    oldPrice: 60000,
    discountBadge: "-20%",
    unit: "Chai 800ml",
    image: "/images/nuoc_rua_bat_huu_co.jpg",
    gallery: ["/images/nuoc_rua_bat_huu_co.jpg"],
    badge: "Bảo vệ da tay",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.75,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Hóa phẩm hữu cơ",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 165,
    sold: 430,
    stock: 85,
    description: "Nước rửa chén chiết xuất từ enzyme bồ hòn lên men tự nhiên và tinh dầu quế ấm nồng. Đánh bay dầu mỡ cứng đầu, khử tanh triệt để và an toàn cho da tay nhạy cảm.",
    highlights: ["Không hóa chất độc hại, bọt xà phòng sinh học", "Hương quế chanh dễ chịu khử sạch mùi tanh", "Dùng được cho cả đồ ăn dặm của em bé"],
    specs: {
      origin: "Hà Nội, Việt Nam",
      brand: "EcoZone Organic",
      weight: "Chai 800ml vòi nhấn tiện lợi",
      shelfLife: "24 tháng",
      storage: "Tránh ánh nắng trực tiếp",
      packingStandard: "Chai nhựa tái chế thân thiện môi trường"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Đạt chuẩn sinh học Ecocert"
    },
    cookingTips: ["Nhấn 1 giọt ra miếng bọt biển ướt tạo bọt xốp nhẹ nhàng."],
    reviews: []
  },
  {
    id: "p31",
    name: "Chảo Chống Dính Vân Đá Ceramic Đáy Từ Siêu Bền",
    category: "household",
    categoryName: "Đồ gia dụng",
    price: 185000,
    oldPrice: 230000,
    discountBadge: "-20%",
    unit: "Chiếc 26cm",
    image: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=800&q=80"],
    badge: "Chống dính vân đá",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.75,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Hóa phẩm hữu cơ",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.7,
    reviewsCount: 94,
    sold: 260,
    stock: 35,
    description: "Chảo chiên rán đường kính 26cm phủ lớp men gốm Ceramic vân đá hoa cương chống trầy xước. Chiên trứng không cần dầu mỡ, tay cầm cách nhiệt chống bỏng an toàn.",
    highlights: ["Lớp chống dính Ceramic không chứa chì hay PFOA", "Đáy từ đúc nguyên khối chống phồng đáy", "Dễ dàng lau chùi chỉ với một chiếc khăn mềm"],
    specs: {
      origin: "Việt Nam",
      brand: "Mỹ Đình Smart Home",
      weight: "850g",
      shelfLife: "Bảo hành 24 tháng",
      storage: "Treo nơi khô ráo",
      packingStandard: "Hộp giấy bảo vệ chuyên dụng"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Chứng chỉ FDA Hoa Kỳ"
    },
    cookingTips: ["Dùng thìa gỗ hoặc muôi silicon để bảo vệ lớp chống dính bền lâu."],
    reviews: []
  },
  {
    id: "p32",
    name: "Hộp Thủy Tinh Chịu Nhiệt Bảo Quản Thực Phẩm 1000ml",
    category: "household",
    categoryName: "Đồ gia dụng",
    price: 68000,
    oldPrice: 85000,
    discountBadge: "-20%",
    unit: "Hộp 1000ml",
    image: "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=800&q=80"],
    badge: "Thủy tinh cao cấp",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.85,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Tiêu dùng nhà bếp",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 145,
    sold: 610,
    stock: 120,
    description: "Hộp thủy tinh chịu nhiệt Borosilicate cao cấp, nắp đậy gioăng silicon 4 khóa kín hơi tuyệt đối. Dùng an toàn trong lò vi sóng, tủ đông và máy rửa bát.",
    highlights: ["Chịu nhiệt từ -20°C đến 400°C", "Gioăng silicon chống tràn nước 100%", "Bảo quản thực phẩm luôn tươi ngon vệ sinh"],
    specs: {
      origin: "Việt Nam",
      brand: "EcoZone Kitchen",
      weight: "Hộp 1000ml (650g)",
      shelfLife: "Bảo hành 12 tháng",
      storage: "Nơi khô ráo",
      packingStandard: "Hộp giấy carton chống sốc"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Chứng nhận an toàn thực phẩm LFGB & FDA"
    },
    cookingTips: ["Mở van thoát hơi silicon trước khi hâm nóng trong lò vi sóng."],
    reviews: []
  }
];

export function useProductCatalog() {
  const moderation = useProductModeration();

  // Danh sách tổng hợp toàn bộ sản phẩm (Gồm catalog tĩnh + sản phẩm do Seller đăng đã được duyệt)
  const allProducts = computed<CatalogProduct[]>(() => {
    // Đảm bảo phụ thuộc phản ứng vào catalogVersion để cập nhật tức thì
    void catalogVersion.value;
    const dynamicActive = moderation.activeProducts.value.map((p) => {
      let cat: "food" | "veggie" | "fastfood" | "beverage" | "household" = "food";
      const rawCat = (p.category || "").toLowerCase();
      if (rawCat.includes("rau") || rawCat.includes("củ") || rawCat.includes("quả") || rawCat.includes("nông sản") || rawCat.includes("hữu cơ") || rawCat.includes("vietgap")) {
        cat = "veggie";
      } else if (rawCat.includes("món") || rawCat.includes("nóng") || rawCat.includes("cơm") || rawCat.includes("bánh mì") || rawCat.includes("ăn") || rawCat.includes("chế biến")) {
        cat = "fastfood";
      } else if (rawCat.includes("uống") || rawCat.includes("trái cây") || rawCat.includes("nước") || rawCat.includes("hoa quả") || rawCat.includes("trà") || rawCat.includes("sữa")) {
        cat = "beverage";
      } else if (rawCat.includes("gia dụng") || rawCat.includes("gia đình") || rawCat.includes("nhu yếu") || rawCat.includes("hàng ngày")) {
        cat = "household";
      } else {
        cat = "food";
      }

      let cleanEmail = (p.sellerEmail || "").toLowerCase().trim();
      let storeAddr = "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội";
      let storeHours = "06:00 - 20:30";
      let storeAvatar = "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80";
      let storeLat = 21.0345;
      let storeLng = 105.7960;

      if (cleanEmail && typeof localStorage !== "undefined") {
        try {
          const rawStore = localStorage.getItem(`zonemart_seller_store_${cleanEmail}`);
          if (rawStore) {
            const parsed = JSON.parse(rawStore);
            if (parsed.address) storeAddr = parsed.address;
            if (parsed.openHours) storeHours = parsed.openHours;
            if (parsed.lat) storeLat = parsed.lat;
            if (parsed.lng) storeLng = parsed.lng;
          }
        } catch {}
      }

      const dbStore = apiStores.value.find(s => 
        (s.id && (p as any).storeId && String(s.id).toLowerCase() === String((p as any).storeId).toLowerCase()) ||
        (s.storeName && p.storeName && s.storeName.trim().toLowerCase() === p.storeName.trim().toLowerCase())
      );
      if (dbStore) {
        if (dbStore.address) storeAddr = dbStore.address;
        if (dbStore.openHours) storeHours = dbStore.openHours;
        if (dbStore.location?.coordinates && dbStore.location.coordinates.length >= 2) {
          storeLng = dbStore.location.coordinates[0];
          storeLat = dbStore.location.coordinates[1];
        } else if (dbStore.lat && dbStore.lng) {
          storeLat = dbStore.lat;
          storeLng = dbStore.lng;
        }
      }

      const resolvedStoreName = p.storeName || dbStore?.storeName || "Gian Hàng ZoneMart";
      let resolvedImage = p.image || "/images/nuoc_rua_bat_huu_co.jpg";
      if (
        (resolvedImage && (resolvedImage.includes("1585421514738") || resolvedImage.includes("1584308666744"))) ||
        (p.name && (p.name.toLowerCase().includes("nước rửa bát") || p.name.toLowerCase().includes("nuoc rua bat")))
      ) {
        resolvedImage = "/images/nuoc_rua_bat_huu_co.jpg";
      }

      const inferred = inferSubCategoryAndPrep({
        name: p.name,
        category: cat,
        subCategory: (p as any).subCategory,
        prepType: (p as any).prepType
      });

      return {
        id: p.id,
        name: p.name,
        category: cat,
        categoryName: p.category,
        subCategory: (p as any).subCategory || inferred.subCategory,
        prepType: (p as any).prepType || inferred.prepType,
        price: p.price,
        unit: p.unit || "Phần",
        image: resolvedImage,
        gallery: [resolvedImage],
        badge: "Đã Kiểm Duyệt",
        store: {
          id: dbStore?.id || ("store_" + (resolvedStoreName).toLowerCase().replace(/[^a-z0-9]/g, "_")),
          name: resolvedStoreName,
          address: storeAddr,
          lat: storeLat,
          lng: storeLng,
          distanceKm: 0.8,
          deliveryTime: "12 - 18 phút",
          rating: 5.0,
          reviewsCount: 384,
          openHours: storeHours,
          totalProducts: 42,
          isVerified: true,
          categoryName: p.category || "Rau củ hữu cơ VietGAP",
          avatar: storeAvatar,
          coverImage: p.image || "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
        },
        rating: 5.0,
        reviewsCount: 0,
        sold: 42,
        stock: typeof p.stock === 'number' ? p.stock : 50,
        description: p.description || `Sản phẩm ${p.name} tươi sạch thu hoạch trực tiếp từ ${resolvedStoreName}. Đã trải qua quy trình kiểm định an toàn chất lượng của sàn thương mại điện tử ZoneMart.`,
        highlights: [
          "Nông sản sạch thu hoạch tự nhiên trong ngày",
          "Đã vượt qua kiểm định AI an toàn thực phẩm",
          "Giao hỏa tốc giữ trọn độ tươi"
        ],
        specs: {
          origin: "Việt Nam",
          brand: resolvedStoreName,
          weight: p.unit || "Tiêu chuẩn",
          shelfLife: "3-5 ngày trong ngăn mát",
          storage: "Bảo quản ở nhiệt độ mát",
          packingStandard: "Đóng gói tiêu chuẩn ATTP"
        },
        nutrition: {
          servingSize: "100g",
          calories: "Chưa cập nhật",
          protein: "Chưa cập nhật",
          fat: "Chưa cập nhật",
          carbs: "Chưa cập nhật",
          minerals: "Đầy đủ dưỡng chất tự nhiên"
        },
        cookingTips: ["Rửa sạch và chế biến theo khẩu vị gia đình."],
        reviews: [],
        aiAudit: {
          safetyScore: p.aiScore?.safetyScore || 98,
          matchScore: p.aiScore?.matchScore || 95,
          verificationDate: p.createdAt || new Date().toLocaleDateString("vi-VN"),
          inspector: "ZoneMart AI Vision System",
          notes: "Đạt chuẩn an toàn thực phẩm sàn ZoneMart."
        }
      } as CatalogProduct;
    });


  // Hàm tính toán điểm số trung bình động cho từng sản phẩm dựa trên đánh giá thực tế của khách hàng
  const applyDynamicRating = (p: CatalogProduct): CatalogProduct => {
    const summary = ratingsSummary.value;
    if (!summary) return p;

    const pKey = p.id.toLowerCase();
    const nameKey = p.name.toLowerCase().trim();
    const cleanNumId = pKey.startsWith("p") ? pKey.slice(1) : pKey;

    const pStat = summary.products?.[pKey] || summary.products?.[nameKey] || summary.products?.[cleanNumId];
    let finalRating = p.rating || 5.0;
    let finalCount = (p.reviews && p.reviews.length > 0) ? p.reviews.length : 0;
    let finalReviews = [...(p.reviews || [])];

    if (pStat && pStat.count > 0) {
      // Khi sản phẩm có đánh giá thực tế từ người mua:
      // finalCount và finalReviews phản ánh chính xác 100% số lượt đánh giá thực tế, không cộng thêm 14
      if (pStat.comments && pStat.comments.length > 0) {
        const mappedComments: ProductReview[] = pStat.comments.map((c: any, idx: number) => ({
          id: `dyn_rev_${p.id}_${idx}`,
          author: c.author || "Khách Hàng ZoneMart",
          avatar: getCleanAvatar(c.avatar, c.author || "Khách Hàng ZoneMart", "buyer"),
          rating: c.rating || 5,
          date: c.date || "Vừa xong",
          comment: (c.comment || "").trim(),
          helpfulCount: 5,
          verified: true
        }));
        finalReviews = mappedComments;
        finalCount = mappedComments.length;
      } else {
        finalCount = pStat.count;
      }
      finalRating = pStat.averageRating || (pStat.count > 0 ? Math.round((pStat.total / pStat.count) * 10) / 10 : 5.0);
    } else {
      finalCount = p.reviews ? p.reviews.length : 0;
      finalRating = p.rating || 5.0;
    }

    // Cập nhật điểm gian hàng trong sản phẩm
    let storeObj = p.store;
    if (storeObj) {
      const sKey = (storeObj.id || "").toLowerCase();
      const sNameKey = (storeObj.name || "").toLowerCase().trim();
      const sStat = summary.stores?.[sKey] || summary.stores?.[sNameKey];
      if (sStat && sStat.count > 0) {
        const baseCount = storeObj.reviewsCount || 40;
        const baseRating = storeObj.rating || 5.0;
        const totalStars = (baseRating * baseCount) + sStat.total;
        const totalCount = baseCount + sStat.count;
        storeObj = {
          ...storeObj,
          rating: Math.round((totalStars / totalCount) * 10) / 10,
          reviewsCount: totalCount
        };
      }
    }

    return {
      ...p,
      rating: finalRating,
      reviewsCount: finalCount,
      reviews: finalReviews,
      store: storeObj
    };
  };

  // Hàm tính toán khoảng cách và thời gian giao hàng thực tế từ GPS người dùng đến gian hàng của sản phẩm
  const applyDynamicDistance = (p: CatalogProduct): CatalogProduct => {
    let storeObj = p.store;
    if (storeObj) {
      const uCoords = currentUserCoords.value;
      const sCoords = resolveStoreCoordinates(storeObj);
      const realDist = calculateHaversineDistance(
        uCoords.lat,
        uCoords.lng,
        sCoords.lat,
        sCoords.lng
      );
      const realDelivery = estimateDeliveryTime(realDist);
      storeObj = {
        ...storeObj,
        lat: sCoords.lat,
        lng: sCoords.lng,
        distanceKm: realDist,
        deliveryTime: realDelivery
      };
    }
    return {
      ...p,
      distanceKm: storeObj?.distanceKm || 1.2,
      store: storeObj
    };
  };

    // Ưu tiên hiển thị 100% dữ liệu sản phẩm thực từ MongoDB Atlas
    if (dynamicActive.length > 0) {
      return dynamicActive.map(applyDynamicRating).map(applyDynamicDistance);
    }
    // Fallback chỉ khi thiết bị hoàn toàn offline và chưa từng tải được dữ liệu (đồng bộ với overrides tồn kho)
    return CATALOG_PRODUCTS.map(p => {
      let st = p.stock;
      try {
        const rawO = typeof localStorage !== 'undefined' ? localStorage.getItem("zonemart_stock_overrides") : null;
        if (rawO) {
          const o = JSON.parse(rawO);
          if (typeof o[p.id] === 'number') st = o[p.id];
          else if (typeof o[p.name.toLowerCase().trim()] === 'number') st = o[p.name.toLowerCase().trim()];
        }
      } catch {}
      const inferred = inferSubCategoryAndPrep(p);
      return {
        ...p,
        subCategory: p.subCategory || inferred.subCategory,
        prepType: p.prepType || inferred.prepType,
        stock: st
      };
    }).map(applyDynamicRating).map(applyDynamicDistance);
  });

  // Danh sách toàn bộ gian hàng trích xuất tự động từ MongoDB và sản phẩm
  const allStores = computed<CatalogStore[]>(() => {
    void catalogVersion.value;
    const storeMap = new Map<string, CatalogStore>();

    // 1. Nạp tất cả các gian hàng có thật từ database MongoDB
    for (const s of apiStores.value) {
      if (!s.storeName) continue;
      const key = s.storeName.trim();
      const sId = s.id || `store_${key.toLowerCase().replace(/[^a-z0-9]/g, '_')}`;
      storeMap.set(key.toLowerCase(), {
        id: sId,
        name: s.storeName,
        address: s.address || "Số 48 đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội",
        distanceKm: 1.2,
        deliveryTime: "15 - 20 phút",
        rating: 5.0,
        reviewsCount: 168,
        totalProducts: 0,
        isVerified: true,
        categoryName: s.category || "Thực phẩm & Nhu yếu phẩm",
        avatar: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80",
        coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80",
        openHours: s.openHours || "06:30 - 22:00",
        products: [],
      });
    }

    // 2. Gán sản phẩm vào gian hàng tương ứng hoặc tạo mới gian hàng nếu chưa có
    for (const p of allProducts.value) {
      if (!p.store || !p.store.name) continue;
      const lowKey = p.store.name.trim().toLowerCase();

      if (!storeMap.has(lowKey)) {
        storeMap.set(lowKey, {
          id: p.store.id || `store_${lowKey.replace(/[^a-z0-9]/g, '_')}`,
          name: p.store.name,
          address: p.store.address || "Hà Nội",
          distanceKm: p.store.distanceKm || 1.5,
          deliveryTime: p.store.deliveryTime || "15 - 20 phút",
          rating: p.store.rating || 5.0,
          reviewsCount: p.store.reviewsCount || p.reviewsCount || 48,
          totalProducts: 1,
          isVerified: p.store.isVerified !== false,
          categoryName: p.store.categoryName || p.categoryName || "Thực phẩm tươi",
          avatar: p.store.avatar || "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80",
          coverImage: p.store.coverImage || p.image,
          openHours: p.store.openHours || "07:00 - 22:00",
          products: [p],
        });
      } else {
        const store = storeMap.get(lowKey)!;
        if (!store.products.some(sp => sp.id === p.id)) {
          store.products.push(p);
        }
        store.totalProducts = store.products.length;
      }
    }

    return Array.from(storeMap.values()).map(s => {
      const sKey = (s.id || "").toLowerCase();
      const sNameKey = (s.name || "").toLowerCase().trim();
      const sStat = ratingsSummary.value?.stores?.[sKey] || ratingsSummary.value?.stores?.[sNameKey];
      let sRating = s.rating || 5.0;
      let sReviewsCount = s.reviewsCount || 48;
      if (sStat && sStat.count > 0) {
        const baseCount = s.reviewsCount || 48;
        const baseRating = s.rating || 5.0;
        const totalStars = (baseRating * baseCount) + sStat.total;
        const totalCount = baseCount + sStat.count;
        sRating = Math.round((totalStars / totalCount) * 10) / 10;
        sReviewsCount = totalCount;
      }

      // Khoảng cách thực tế từ GPS người dùng đến gian hàng
      const uCoords = currentUserCoords.value;
      const sCoords = resolveStoreCoordinates(s);
      const realDist = calculateHaversineDistance(
        uCoords.lat,
        uCoords.lng,
        sCoords.lat,
        sCoords.lng
      );
      const realDelivery = estimateDeliveryTime(realDist);

      return {
        ...s,
        lat: sCoords.lat,
        lng: sCoords.lng,
        distanceKm: realDist,
        deliveryTime: realDelivery,
        rating: sRating,
        reviewsCount: sReviewsCount
      };
    });
  });

  // Tìm kiếm sản phẩm theo ID (hỗ trợ mã 'p1' lẫn '1')
  const getProductById = (id: string | number): CatalogProduct | undefined => {
    const targetId = String(id).trim().toLowerCase();
    const cleanNumId = targetId.startsWith("p") ? targetId.slice(1) : targetId;

    return (
      allProducts.value.find((p) => p.id.toLowerCase() === targetId) ||
      allProducts.value.find((p) => p.id.toLowerCase() === `p${cleanNumId}`) ||
      allProducts.value.find((p) => p.id.toLowerCase() === cleanNumId) ||
      allProducts.value[0]
    );
  };

  // Lấy danh sách sản phẩm liên quan trong bán kính 10km
  const getRelatedProducts = (currentId: string, limit = 4): CatalogProduct[] => {
    return allProducts.value
      .filter((p) => p.id !== currentId)
      .slice(0, limit);
  };

  const getStoreByIdOrName = (idOrName: string): CatalogStore | undefined => {
    const term = idOrName.toLowerCase().trim();
    return allStores.value.find(
      (s) => s.id.toLowerCase() === term || s.name.toLowerCase() === term
    );
  };

  const refreshCatalog = () => {
    moderation.refreshProducts();
    fetchDbStores();
    catalogVersion.value++;
  };

  return {
    allProducts,
    allStores,
    getProductById,
    getRelatedProducts,
    getStoreByIdOrName,
    refreshCatalog,
    ratingsSummary,
    fetchRatingsSummary,
    recordLocalReview,
    currentUserCoords,
    calculateHaversineDistance,
    estimateDeliveryTime,
    resolveStoreCoordinates,
  };
}

// Lắng nghe sự kiện toàn cục khi sản phẩm hoặc vị trí GPS thay đổi để tự động re-compute catalog
if (typeof window !== "undefined") {
  window.addEventListener("zonemart:products-changed", () => {
    catalogVersion.value++;
  });
  window.addEventListener("zonemart:location-changed", () => {
    catalogVersion.value++;
  });
}
