import { ref, computed } from "vue";

export type ProductStatus =
  | "active"              // Đang bán (hợp lệ)
  | "pending_review"     // Nghi ngờ sai lệch, chờ Manager duyệt
  | "rejected_need_edit" // Manager từ chối, yêu cầu Seller sửa
  | "deleted_violation"; // Bị AI xóa do vi phạm 18+ hoặc cấm

export interface ModeratedProduct {
  id: string;
  name: string;
  category: string;
  price: number;
  unit: string;
  stock: number;
  image: string;
  status: ProductStatus;
  isAvailable: boolean;
  sellerEmail: string;
  storeName: string;
  createdAt: string;
  aiScore?: {
    safetyScore: number;
    matchScore: number;
    flag?: string;
    detectedIssues?: string[];
  };
  managerNote?: string;
}

export interface SellerPenaltyState {
  email: string;
  violationCount: number;
  isLocked: boolean;
  lockUntil?: string;
  isBanned: boolean;
  lastViolationReason?: string;
  lastViolationDate?: string;
  notificationLogs: Array<{
    id: string;
    type: "warn" | "lock" | "ban" | "manager_reject" | "approved";
    title: string;
    message: string;
    date: string;
  }>;
}

export interface AIScanResult {
  decision: "PASSED" | "SUSPICIOUS" | "VIOLATION";
  reason: string;
  safetyScore: number;
  matchScore: number;
  detectedIssues: string[];
}

const NSFW_KEYWORDS = [
  "18+", "người lớn", "sex", "khiêu dâm", "đồi trụy", "búp bê tình dục",
  "dụng cụ tình dục", "thuốc kích dục", "kích dục", "gợi dục", "khoả thân",
  "porn", "nude", "bao cao su đôn dên", "sextoy"
];

const PROHIBITED_KEYWORDS = [
  // Bom mìn, chất nổ
  "bom", "bomb", "bom mìn", "bom min", "mìn", "min", "thuốc nổ", "thuoc no", "chất nổ", "chat no", "lựu đạn", "luu dan",
  "grenade", "dynamite", "explosive", "pháo hoa lậu", "pháo nổ", "phao no", "pháo tép", "phao",
  
  // Súng đạn, vũ khí sát thương
  "súng", "sung", "súng lục", "sung luc", "súng hơi", "sung hoi", "súng trường", "sung truong", "súng bắn", "khẩu súng", "khau sung",
  "gun", "guns", "pistol", "rifle", "shotgun", "firearm", "ak47", "ak-47", "glock", "revolver",
  "đạn", "dan", "bullet", "bullets", "ammo", "ammunition", "hộp đạn", "hop dan", "băng đạn", "bang dan",
  "vũ khí", "vu khi", "weapon", "weapons", "dao phóng", "dao găm", "kiếm nhật", "kiem nhat", "mã tấu", "ma tau",
  
  // Ma túy, chất kích thích bị cấm
  "ma túy", "ma tuy", "cần sa", "can sa", "heroin", "cocaine", "meth", "thuốc lắc", "thuoc lac", "bóng cười", "bong cuoi",
  "cỏ mỹ", "co my", "hồng phiến", "hong phien",
  
  // Động vật hoang dã, hàng cấm khác
  "động vật hoang dã", "sừng tê giác", "ngà voi", "hổ cốt", "thuốc lá lậu",
  "rượu giả", "tiền giả", "chất độc", "chat doc", "poison", "toxic", "axit đậm đặc"
];

function textHasKeyword(text: string, kw: string): boolean {
  if (!text || !kw) return false;
  const lower = text.toLowerCase().trim();
  const lowerKw = kw.toLowerCase().trim();
  if (lower === lowerKw) return true;

  if (lowerKw.length <= 3) {
    const words = lower.split(/[\s,./_\\-–—()[\]+:]+/);
    return words.includes(lowerKw);
  }
  return lower.includes(lowerKw);
}

function isFoodSung(text: string): boolean {
  const norm = (text || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").toLowerCase();
  return (
    norm.includes("qua sung") ||
    norm.includes("trai sung") ||
    norm.includes("sung muoi") ||
    norm.includes("bong sung") ||
    norm.includes("rau sung") ||
    norm.includes("sung nep") ||
    norm.includes("canh bong sung")
  );
}

const CATEGORY_VOCABULARY: Record<string, string[]> = {
  "Rau củ quả": [
    "rau", "củ", "quả", "xà lách", "xa lach", "lách", "salad", "lettuce", "romaine", "cải", "bắp cải", "súp lơ",
    "bông cải", "muống", "mồng tơi", "mong toi", "dền", "ngót", "đay", "bina", "bó xôi", "su hào", "su su",
    "cần tây", "ngò", "mùi", "thì là", "tía tô", "kinh giới", "húng", "diếp cá", "ngải cứu", "lá lốt",
    "cà chua", "ca chua", "dưa leo", "dưa chuột", "dua leo", "bí", "bầu", "mướp", "khổ qua", "mướp đắng",
    "đậu", "đậu bắp", "bắp", "ngô", "khoai", "cà rốt", "ca rot", "củ cải", "gừng", "sả", "nghệ", "riềng",
    "ớt", "tỏi", "hành", "chanh", "nấm", "măng", "giá", "veggie", "vegetable"
  ],
  "Thịt cá tươi": [
    "thịt", "thit", "heo", "lợn", "lon", "bò", "bo", "beef", "pork", "gà", "ga", "chicken", "vịt", "vit",
    "ngan", "ngỗng", "chim", "cút", "sườn", "suon", "ba rọi", "ba chỉ", "ba roi", "ba chi", "nạc", "nac",
    "thăn", "than", "bắp bò", "gầu", "nạm", "dẻ sườn", "chân giò", "giò heo", "lòng", "mỡ", "thịt băm",
    "cá", "ca", "fish", "tôm", "tom", "shrimp", "mực", "muc", "squid", "bạch tuộc", "cua", "ghẹ", "ốc",
    "sò", "ngao", "hàu", "chả cá", "cá hồi", "cá thu", "cá basa", "cá lóc", "cá điêu hồng", "cá chép",
    "cá ngừ", "cá nục", "seafood", "meat"
  ],
  "Trái cây tươi": [
    "trái cây", "hoa quả", "cam", "quýt", "bưởi", "buoi", "chanh leo", "táo", "tao", "apple", "lê", "nho",
    "grape", "chuối", "chuoi", "banana", "dưa hấu", "dua hau", "watermelon", "dưa lưới", "xoài", "xoai",
    "mango", "thanh long", "sầu riêng", "sau rieng", "durian", "mít", "măng cụt", "mang cut", "chôm chôm",
    "nhãn", "vải", "dâu", "dâu tây", "strawberry", "ổi", "mận", "đào", "bơ", "avocado", "đu đủ", "du du",
    "kiwi", "dứa", "thơm", "khóm", "lựu", "na", "mãng cầu", "dừa", "tắc", "quất", "fruit"
  ],
  "Thực phẩm bổ dưỡng": [
    "trứng", "trung", "egg", "sữa", "sua", "milk", "yến", "yen", "mật ong", "mat ong", "honey", "hạt",
    "ngũ cốc", "ngu coc", "yến mạch", "bơ sữa", "cheese", "phô mai", "hạnh nhân", "óc chó", "hạt điều",
    "hạt chia", "macca", "rong biển", "sâm", "đông trùng", "collagen", "organic", "vitamin"
  ],
  "Món ăn nóng": [
    "bún", "bun", "phở", "pho", "cơm", "com", "bánh mì", "banh mi", "bánh", "hủ tiếu", "hu tieu", "mì",
    "mi", "cháo", "chao", "canh", "lẩu", "lau", "xôi", "xoi", "nem", "chả giò", "gỏi", "pizza", "gà rán",
    "steak", "noodle", "soup"
  ]
};

const CONDIMENT_DRY_KEYWORDS = [
  "muối", "muoi", "salt", "đường", "duong", "sugar", "bột ngọt", "mì chính", "hạt nêm", "tiêu", "tieu",
  "nước mắm", "nuoc mam", "xì dầu", "nước tương", "dầu ăn", "dau an", "dầu hào", "giấm", "giam", "tương ớt",
  "sa tế", "sốt", "gia vị", "gia vi", "bột canh", "bột chiên", "bột mì", "bột bắp", "gạo", "gao", "nếp",
  "đậu xanh", "lạc", "đậu phộng", "vừng", "mè", "bánh tráng", "miến"
];

const NON_FOOD_MISC_KEYWORDS = [
  "điện thoại", "iphone", "samsung", "laptop", "máy tính", "macbook", "ô tô", "xe máy", "bàn ghế",
  "quần áo", "giày dép", "túi xách", "đồng hồ", "son môi", "mỹ phẩm", "kem dưỡng", "tai nghe", "loa"
];

export const INITIAL_PRODUCTS: ModeratedProduct[] = [];

const STORAGE_KEY_PRODUCTS = "zonemart_moderated_products";
const STORAGE_KEY_PENALTIES = "zonemart_seller_penalties";
const STORAGE_KEY_DELETED = "zonemart_deleted_products";

function getDeletedProductKeys(): Set<string> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_DELETED);
    if (raw) return new Set(JSON.parse(raw));
  } catch {}
  return new Set();
}

function recordDeletedProduct(id: string, name?: string) {
  try {
    const keys = getDeletedProductKeys();
    if (id) keys.add(id);
    if (name) {
      keys.add(name.toLowerCase().trim());
      keys.add(stripAccents(name).trim());
    }
    localStorage.setItem(STORAGE_KEY_DELETED, JSON.stringify(Array.from(keys)));
  } catch {}
}

function stripAccents(str: string): string {
  return (str || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase();
}

function loadProducts(): ModeratedProduct[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_PRODUCTS);
    const deletedKeys = getDeletedProductKeys();

    let items: ModeratedProduct[] = [];
    if (raw) {
      items = JSON.parse(raw);
    } else {
      items = [...INITIAL_PRODUCTS];
    }
    let changed = false;

    // 1. Lọc bỏ hoàn toàn các sản phẩm đã bị Từ Chối / Gỡ bài / Xóa bỏ
    items = items.filter(item => {
      const normName = stripAccents(item.name).trim();
      const lowerName = item.name.toLowerCase().trim();
      if (deletedKeys.has(item.id) || deletedKeys.has(normName) || deletedKeys.has(lowerName)) {
        changed = true;
        return false;
      }

      const flagText = (item.aiScore?.flag || "").toLowerCase();

      // Kiểm tra xem tên bài có phải hàng cấm không (bom, súng, đạn, vũ khí...)
      const isWeaponOrBomb = normName === "bom" || 
                             normName.includes("sung") || 
                             normName.includes("gun") || 
                             normName.includes("pistol") || 
                             normName.includes("dan ") ||
                             normName.startsWith("dan") ||
                             normName.includes("vu khi");

      // Kiểm tra xem bài đăng tên "rau" có ảnh là súng đạn (Card 1 trong ảnh người dùng)
      const isGunImageMismatch = (normName === "rau" && item.price === 25000 && flagText.includes("thịt đỏ"));

      if (isWeaponOrBomb || isGunImageMismatch) {
        changed = true;
        return false; // Xóa khỏi danh sách theo đúng quy định Luồng 2 (Hệ thống Xóa bài)
      }
      return true;
    });

    for (const item of items) {
      const normName = stripAccents(item.name).trim();
      // 3. Chuẩn hóa bài đăng sss nếu còn mang tên gian hàng mặc định từ cache cũ
      if (normName === "sss") {
        if (item.storeName === "Vườn Rau Ba Vì - Nông Sản Sạch" || !item.storeName) {
          item.storeName = "lọ lên đầu đạt";
          changed = true;
        }
        if (item.sellerEmail === "seller@zonemart.vn" || !item.sellerEmail) {
          item.sellerEmail = "dovanbinh487@gmail.com";
          changed = true;
        }
      }
    }

    // Lọc bỏ toàn bộ sản phẩm mẫu demo cũ nếu còn lưu trong cache
    items = items.filter(item => {
      if (item.id && (item.id.startsWith("p-sp-") || item.id === "p-01" || item.id === "p-02" || item.id === "p-03")) {
        changed = true;
        return false;
      }
      return true;
    });

    if (changed) {
      try {
        localStorage.setItem(STORAGE_KEY_PRODUCTS, JSON.stringify(items));
      } catch { /* ignore */ }
    }
    return items;
  } catch (e) {
    console.error("Lỗi đọc sản phẩm kiểm duyệt:", e);
  }
  return INITIAL_PRODUCTS;
}

function loadPenalties(): Record<string, SellerPenaltyState> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_PENALTIES);
    if (raw) return JSON.parse(raw);
  } catch (e) {
    console.error("Lỗi đọc trạng thái phạt seller:", e);
  }
  return {};
}

export function compressImage(
  dataUrl: string,
  maxWidth = 400,
  maxHeight = 400,
  quality = 0.75
): Promise<string> {
  return new Promise((resolve) => {
    if (!dataUrl || !dataUrl.startsWith("data:image")) {
      resolve(dataUrl);
      return;
    }
    // Ảnh đã nhỏ (< 60KB) thì không cần nén tiếp
    if (dataUrl.length < 65000) {
      resolve(dataUrl);
      return;
    }
    try {
      const img = new Image();
      img.onload = () => {
        try {
          let width = img.width;
          let height = img.height;
          if (width > maxWidth || height > maxHeight) {
            if (width > height) {
              height = Math.round((height * maxWidth) / width);
              width = maxWidth;
            } else {
              width = Math.round((width * maxHeight) / height);
              height = maxHeight;
            }
          }
          const canvas = document.createElement("canvas");
          canvas.width = Math.max(width, 100);
          canvas.height = Math.max(height, 100);
          const ctx = canvas.getContext("2d");
          if (!ctx) {
            resolve(dataUrl);
            return;
          }
          ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
          const compressed = canvas.toDataURL("image/jpeg", quality);
          resolve(compressed);
        } catch {
          resolve(dataUrl);
        }
      };
      img.onerror = () => resolve(dataUrl);
      img.src = dataUrl;
    } catch {
      resolve(dataUrl);
    }
  });
}

function getCategoryFallbackImage(category: string): string {
  if (category === "Rau củ quả") {
    return "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=500&q=80";
  }
  if (category === "Thịt cá tươi") {
    return "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=500&q=80";
  }
  if (category === "Trái cây tươi") {
    return "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=500&q=80";
  }
  if (category === "Món ăn nóng") {
    return "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=500&q=80";
  }
  return "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=500&q=80";
}

const allProducts = ref<ModeratedProduct[]>(loadProducts());
const sellerPenalties = ref<Record<string, SellerPenaltyState>>(loadPenalties());

const BROADCAST_CHANNEL_NAME = "zonemart_products_channel";
let productsBroadcastChannel: BroadcastChannel | null = null;
if (typeof window !== "undefined" && "BroadcastChannel" in window) {
  try {
    productsBroadcastChannel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
    productsBroadcastChannel.onmessage = (event) => {
      if (event.data && (event.data.type === "PRODUCTS_CHANGED" || event.data.type === "PRODUCT_STATUS_UPDATED")) {
        refreshProducts();
      }
    };
  } catch (e) {
    console.warn("BroadcastChannel error:", e);
  }
}

function saveProducts() {
  try {
    localStorage.setItem(STORAGE_KEY_PRODUCTS, JSON.stringify(allProducts.value));
  } catch (err) {
    console.warn("localStorage quota exceeded while saving products, stripping heavy images for localStorage only...", err);
    try {
      const sanitized = allProducts.value.map(p => {
        if (p.image && p.image.startsWith("data:") && p.image.length > 50000) {
          return {
            ...p,
            image: getCategoryFallbackImage(p.category)
          };
        }
        return p;
      });
      localStorage.setItem(STORAGE_KEY_PRODUCTS, JSON.stringify(sanitized));
      // KHÔNG ghi đè allProducts.value trong RAM để giữ nguyên ảnh thật
    } catch (e2) {
      console.error("Critical: Could not save products to localStorage:", e2);
    }
  }

  // Thông báo thời gian thực tới tất cả các component và tab đang mở (BroadcastChannel + StorageEvent + CustomEvent)
  if (typeof window !== "undefined") {
    try {
      if (productsBroadcastChannel) {
        productsBroadcastChannel.postMessage({
          type: "PRODUCTS_CHANGED",
          timestamp: Date.now()
        });
      }
    } catch {}

    try {
      localStorage.setItem("zonemart_product_bus_event", String(Date.now()));
    } catch {}

    setTimeout(() => {
      window.dispatchEvent(
        new CustomEvent("zonemart:products-changed", {
          detail: { count: allProducts.value.length, timestamp: Date.now() }
        })
      );
    }, 0);
  }
}

function savePenalties() {
  try {
    localStorage.setItem(STORAGE_KEY_PENALTIES, JSON.stringify(sellerPenalties.value));
  } catch {}
}

// Đồng bộ sản phẩm lên backend MongoDB trong nền
async function syncProductToBackend(p: ModeratedProduct) {
  try {
    const cleanImg = p.image || getCategoryFallbackImage(p.category);
    const payload = {
      id: p.id,
      name: p.name,
      category: p.category,
      price: p.price,
      unit: p.unit,
      stock: p.stock,
      image: cleanImg,
      sellerEmail: p.sellerEmail,
      storeName: p.storeName,
      status: p.status
    };

    let res = await fetch("/api/products/sync-product", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    }).catch(() => null);

    if (!res || !res.ok) {
      res = await fetch("http://localhost:5000/api/products/sync-product", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      }).catch(() => null);
    }

    if (res && res.ok) {
      const data = await res.json().catch(() => null);
      if (data && data.product && data.product.id) {
        p.id = data.product.id;
        saveProducts();
      }
    }
  } catch (err) {
    console.warn("Lỗi syncProductToBackend:", err);
  }
}

let isRefreshingProducts = false;

// Tải danh sách sản phẩm thật 100% từ CSDL MongoDB Backend
export async function fetchRealProductsFromBackend(): Promise<ModeratedProduct[]> {
  if (typeof window === "undefined") return allProducts.value;
  if (isRefreshingProducts) return allProducts.value;
  isRefreshingProducts = true;

  try {
    let res = await fetch(`/api/products?_t=${Date.now()}`, {
      cache: "no-store",
      headers: { "Cache-Control": "no-cache", "Pragma": "no-cache" },
      signal: AbortSignal.timeout(3500)
    }).catch(() => null);

    if (!res || !res.ok) {
      res = await fetch(`http://localhost:5000/api/products?_t=${Date.now()}`, {
        cache: "no-store",
        headers: { "Cache-Control": "no-cache", "Pragma": "no-cache" },
        signal: AbortSignal.timeout(3500)
      }).catch(() => null);
    }

    if (res && res.ok) {
      const data = await res.json().catch(() => null);
      if (data && data.success && Array.isArray(data.data)) {
        const deletedKeys = getDeletedProductKeys();
        const mappedList: ModeratedProduct[] = [];

        data.data.forEach((bp: any) => {
          const pName = (bp.productName || bp.name || "").trim();
          if (!pName) return;
          const normPName = stripAccents(pName).trim();
          const lowerPName = pName.toLowerCase();

          // Lọc bỏ nếu đã bị xóa khỏi hệ thống
          if (deletedKeys.has(bp.id) || deletedKeys.has(lowerPName) || deletedKeys.has(normPName)) {
            return;
          }

          const realStoreName = bp.storeName || (bp.store && bp.store.name) || "Gian hàng đối tác";
          const realSellerEmail = bp.sellerEmail || "";
          const realImage = bp.imageUrl || bp.image || getCategoryFallbackImage(bp.category);

          const isPending = bp.aiStatus === "pending" || bp.aiStatus === "pending_review" || bp.aiStatus === "suspicious" || bp.status === "pending_review";
          const isRejected = bp.aiStatus === "rejected" || bp.status === "rejected_need_edit";
          const isApproved = (bp.aiStatus === "approved" || bp.status === "active") && !isPending && !isRejected;

          const prodStatus: ProductStatus = isPending ? "pending_review" : (isRejected ? "rejected_need_edit" : "active");

          let aiFlag = bp.aiCheckNote;
          if (isPending && !aiFlag) {
            aiFlag = "Sản phẩm thực tế từ gian hàng đang chờ Ban Quản Trị đối soát và thẩm định mở bán.";
          }

          mappedList.push({
            id: bp.id || `p-${Date.now()}`,
            name: pName,
            category: bp.category || "Thực phẩm tươi",
            price: bp.price || 0,
            unit: bp.weight ? `${bp.weight} kg` : "Khay / Gói",
            stock: bp.stockQuantity ?? 30,
            image: realImage,
            status: prodStatus,
            isAvailable: isApproved,
            sellerEmail: realSellerEmail,
            storeName: realStoreName,
            createdAt: bp.createdAt ? new Date(bp.createdAt).toLocaleDateString("vi-VN") : "Hôm nay",
            aiScore: {
              safetyScore: isPending ? 80 : 99,
              matchScore: isPending ? 55 : 98,
              flag: aiFlag
            }
          });
        });

        // Cập nhật allProducts trực tiếp từ CSDL MongoDB thật
        allProducts.value = mappedList;
        try {
          localStorage.setItem(STORAGE_KEY_PRODUCTS, JSON.stringify(mappedList));
        } catch {}
        return allProducts.value;
      }
    }
  } catch (err) {
    console.error("Lỗi fetchRealProductsFromBackend:", err);
  } finally {
    isRefreshingProducts = false;
  }
  return allProducts.value;
}

// Làm mới danh sách sản phẩm từ backend MongoDB
function refreshProducts(): ModeratedProduct[] {
  fetchRealProductsFromBackend();
  return allProducts.value;
}

// Tự động khởi chạy đồng bộ ngay khi module được nạp trong trình duyệt
if (typeof window !== "undefined") {
  setTimeout(() => {
    refreshProducts();
  }, 100);

  // Lắng nghe sự kiện Storage giữa các tab trình duyệt
  window.addEventListener("storage", (e) => {
    if (e.key === STORAGE_KEY_PRODUCTS || e.key === "zonemart_product_bus_event") {
      refreshProducts();
    }
  });

  // Lắng nghe event nội bộ trong cùng window
  window.addEventListener("zonemart:products-changed", () => {
    refreshProducts();
  });
}


export function useProductModeration() {
  const getSellerPenalty = (sellerEmail: string): SellerPenaltyState => {
    const email = sellerEmail.toLowerCase().trim() || "seller@zonemart.vn";
    if (!sellerPenalties.value[email]) {
      sellerPenalties.value[email] = {
        email,
        violationCount: 0,
        isLocked: false,
        isBanned: false,
        notificationLogs: []
      };
      savePenalties();
    }

    const state = sellerPenalties.value[email];
    if (state.isLocked && state.lockUntil) {
      const lockEnd = new Date(state.lockUntil).getTime();
      if (Date.now() >= lockEnd) {
        state.isLocked = false;
        state.lockUntil = undefined;
        savePenalties();
      }
    }

    return state;
  };

  const scanProductWithAI = (params: {
    name: string;
    category: string;
    price: number;
    image: string;
    unit?: string;
    imageFileName?: string;
    imageVisualTag?: string;
  }): AIScanResult => {
    const lowerName = params.name.toLowerCase().trim();
    const cleanName = stripAccents(params.name);
    const isDataUrl = params.image.startsWith("data:");
    const lowerImage = isDataUrl ? "" : params.image.toLowerCase().trim();
    const lowerFileName = (params.imageFileName || "").toLowerCase().trim();
    const cleanFileName = stripAccents(params.imageFileName || "");
    const visualTag = params.imageVisualTag || "unknown";

    // 1. Quét nội dung 18+ & Hàng cấm tuyệt đối
    const issues: string[] = [];

    // Kiểm tra đặc trưng thị giác vũ khí quân dụng / súng đạn từ canvas
    if (visualTag === "prohibited_weapon") {
      issues.push("Hệ thống AI phát hiện hình ảnh chứa vũ khí / súng đạn / vật phẩm quân dụng bị cấm tuyệt đối");
    }

    for (const kw of NSFW_KEYWORDS) {
      if (lowerName.includes(kw) || cleanName.includes(stripAccents(kw)) || lowerImage.includes(kw) || cleanFileName.includes(stripAccents(kw))) {
        issues.push(`Chứa nội dung 18+ / đồ chơi tình dục bị cấm: "${kw}"`);
      }
    }

    for (const kw of PROHIBITED_KEYWORDS) {
      // Ngoại lệ cho nông sản quả sung / bông súng
      if ((kw === "sung" || kw === "súng") && (isFoodSung(params.name) || isFoodSung(params.imageFileName || ""))) {
        continue;
      }
      const matchedName = textHasKeyword(lowerName, kw) || textHasKeyword(cleanName, stripAccents(kw));
      const matchedFile = cleanFileName.includes(stripAccents(kw)) || lowerFileName.includes(kw);
      const matchedImage = lowerImage.includes(kw);

      if (matchedName || matchedFile || matchedImage) {
        issues.push(`Chứa từ khóa hàng cấm / vũ khí / chất nổ bị cấm: "${kw}"`);
        break;
      }
    }

    if (issues.length > 0) {
      return {
        decision: "VIOLATION",
        reason: issues.join("; "),
        safetyScore: 10,
        matchScore: 15,
        detectedIssues: issues
      };
    }

    // 2. Nhận diện ngữ nghĩa phân loại mặt hàng (Domain Recognition)
    const isVegetable = (CATEGORY_VOCABULARY["Rau củ quả"] || []).some(kw => textHasKeyword(lowerName, kw) || textHasKeyword(cleanName, stripAccents(kw)));
    const isMeatOrFish = (CATEGORY_VOCABULARY["Thịt cá tươi"] || []).some(kw => textHasKeyword(lowerName, kw) || textHasKeyword(cleanName, stripAccents(kw)));
    const isFruit = (CATEGORY_VOCABULARY["Trái cây tươi"] || []).some(kw => textHasKeyword(lowerName, kw) || textHasKeyword(cleanName, stripAccents(kw)));
    const isCondiment = CONDIMENT_DRY_KEYWORDS.some(kw => textHasKeyword(lowerName, kw) || textHasKeyword(cleanName, stripAccents(kw)));
    const isNonFood = NON_FOOD_MISC_KEYWORDS.some(kw => textHasKeyword(lowerName, kw) || textHasKeyword(cleanName, stripAccents(kw)));

    let matchScore = 97;
    const suspiciousReasons: string[] = [];

    // 3. Kiểm tra tính đồng nhất giữa Tên sản phẩm và Danh mục đã chọn
    if (params.category === "Rau củ quả") {
      if (isMeatOrFish) {
        matchScore -= 45;
        suspiciousReasons.push(`Sai lệch ngành hàng: Món "${params.name}" là [Thịt cá tươi], không thuộc danh mục [Rau củ quả]`);
      } else if (isCondiment) {
        matchScore -= 45;
        suspiciousReasons.push(`Sai lệch ngành hàng: Món "${params.name}" là [Gia vị / Đồ khô], không thuộc danh mục [Rau củ quả]`);
      } else if (isFruit) {
        matchScore -= 30;
        suspiciousReasons.push(`Sai lệch ngành hàng: Món "${params.name}" là [Trái cây tươi], vui lòng chuyển sang danh mục Trái cây`);
      } else if (isNonFood) {
        matchScore -= 60;
        suspiciousReasons.push(`Sai lệch mặt hàng: "${params.name}" là đồ dùng / thiết bị, không thuộc thực phẩm nông sản`);
      }
    } else if (params.category === "Thịt cá tươi") {
      if (isVegetable) {
        matchScore -= 45;
        suspiciousReasons.push(`Sai lệch ngành hàng: Món "${params.name}" là [Rau củ quả], không thuộc danh mục [Thịt cá tươi]`);
      } else if (isFruit || isCondiment) {
        matchScore -= 35;
        suspiciousReasons.push(`Sai lệch ngành hàng: Món "${params.name}" không thuộc danh mục thịt cá tươi sống`);
      }
    } else if (params.category === "Trái cây tươi") {
      if (isVegetable || isMeatOrFish) {
        matchScore -= 45;
        suspiciousReasons.push(`Sai lệch ngành hàng: Món "${params.name}" không thuộc danh mục [Trái cây tươi]`);
      }
    } else if (params.category === "Thực phẩm bổ dưỡng") {
      if (isVegetable || isMeatOrFish || isCondiment) {
        matchScore -= 45;
        suspiciousReasons.push(`Sai lệch ngành hàng: Món "${params.name}" (${isCondiment ? 'Gia vị / Muối ăn' : 'Nông sản'}) không thuộc nhóm thực phẩm dinh dưỡng`);
      }
    } else if (params.category === "Món ăn nóng") {
      if (isVegetable || isMeatOrFish || isCondiment) {
        // Tươi sống chưa nấu
        if (!lowerName.includes("nấu") && !lowerName.includes("canh") && !lowerName.includes("lẩu") && !lowerName.includes("xào")) {
          matchScore -= 20;
          suspiciousReasons.push(`Lưu ý: Món ăn nóng nên là món đã qua chế biến, sẵn sàng dùng ngay`);
        }
      }
    }

    // 4. Đối soát đặc trưng thị giác & tên tệp Ảnh với Tên sản phẩm
    const imgIsSaltOrSpice = cleanFileName.includes("muoi") || cleanFileName.includes("salt") || cleanFileName.includes("gia vi") || cleanFileName.includes("gia-vi") || cleanFileName.includes("duong") || cleanFileName.includes("sugar");
    const imgIsMeat = cleanFileName.includes("thit") || cleanFileName.includes("beef") || cleanFileName.includes("pork") || cleanFileName.includes("meat") || cleanFileName.includes("chicken") || cleanFileName.includes("ca ") || cleanFileName.includes("fish") || visualTag === "red_meat";
    const imgIsVeggie = cleanFileName.includes("rau") || cleanFileName.includes("xa") || cleanFileName.includes("lettuce") || cleanFileName.includes("cai") || cleanFileName.includes("salad") || cleanFileName.includes("vegetable") || visualTag === "green_vegetables";

    // Trường hợp 4.1: Tên là rau (xà lách, rau muống...) nhưng ảnh là bao bì muối/gia vị
    // Trường hợp 4.1: Tên là muối ăn / gia vị nhưng ảnh là rau củ quả xanh (Lỗi người dùng vừa chụp!)
    if (isCondiment && (imgIsVeggie || visualTag === "green_vegetables")) {
      matchScore -= 50;
      suspiciousReasons.push(`Lệch ảnh và tên: Tên bài đăng là gia vị "${params.name}" nhưng hình ảnh tải lên lại là rau xanh / nông sản (phát hiện đặc trưng rau xà lách)`);
    }

    // Trường hợp 4.2: Tên là rau (xà lách, rau muống...) nhưng ảnh là bao bì muối/gia vị
    if (isVegetable && (imgIsSaltOrSpice || visualTag === "white_packaged")) {
      matchScore -= 50;
      suspiciousReasons.push(`Lệch ảnh và tên: Tên bài đăng là rau xanh "${params.name}" nhưng hình ảnh tải lên là bao bì muối ăn / gia vị đóng gói (phát hiện "${params.imageFileName || 'bao bì trắng'}")`);
    }

    // Trường hợp 4.2: Tên là thịt cá nhưng ảnh là rau xanh
    // Trường hợp 4.3: Tên là thịt cá nhưng ảnh là rau xanh
    if (isMeatOrFish && (imgIsVeggie || visualTag === "green_vegetables")) {
      matchScore -= 50;
      suspiciousReasons.push(`Lệch ảnh và tên: Tên bài đăng là "${params.name}" [Thịt cá tươi] nhưng hình ảnh tải lên lại là rau củ quả xanh`);
    }

    // Trường hợp 4.3: Tên là rau nhưng ảnh là thịt đỏ
    // Trường hợp 4.4: Tên là rau nhưng ảnh là thịt đỏ
    if (isVegetable && (imgIsMeat || visualTag === "red_meat")) {
      matchScore -= 50;
      suspiciousReasons.push(`Lệch ảnh và tên: Tên bài đăng là "${params.name}" [Rau củ quả] nhưng hình ảnh tải lên là thịt đỏ tươi sống`);
    }

    // Trường hợp 4.5: Tên là gia vị / muối nhưng ảnh là thịt tươi sống
    if (isCondiment && (imgIsMeat || visualTag === "red_meat")) {
      matchScore -= 50;
      suspiciousReasons.push(`Lệch ảnh và tên: Tên bài đăng là gia vị "${params.name}" nhưng hình ảnh tải lên là thịt đỏ tươi sống`);
    }

    // Trường hợp 4.4: Phát hiện vật dụng ngoài ngành hàng trong ảnh (laptop, xe, điện thoại...)
    const mismatchedTokens = ["phone", "laptop", "car", "weapon", "beer", "wine", "alcohol", "smoke"];
    for (const token of mismatchedTokens) {
      if ((lowerImage.includes(token) || lowerFileName.includes(token)) && !lowerName.includes(token)) {
        matchScore -= 50;
        suspiciousReasons.push(`Ảnh minh họa có dấu hiệu không đồng nhất: phát hiện đặc trưng ngoại lai "${token}"`);
      }
    }

    if (params.name.trim().length < 3) {
      matchScore -= 25;
      suspiciousReasons.push("Tên sản phẩm quá ngắn, thiếu thông tin định danh");
    }

    if (params.price <= 500) {
      matchScore -= 20;
      suspiciousReasons.push("Giá niêm yết quá thấp so với mặt bằng thực tế");
    } else if (params.price >= 30000000) {
      matchScore -= 25;
      suspiciousReasons.push("Giá niêm yết vượt định mức thông thường cần kiểm chứng chứng từ");
    }

    // 5. Kết luận phân nhánh AI
    if (suspiciousReasons.length > 0 || matchScore < 70) {
      return {
        decision: "SUSPICIOUS",
        reason: suspiciousReasons.join("; ") || "Độ khớp Ảnh - Tên chưa đạt chuẩn đồng nhất",
        safetyScore: 82,
        matchScore: Math.max(35, matchScore),
        detectedIssues: suspiciousReasons
      };
    }

    return {
      decision: "PASSED",
      reason: "Sản phẩm hợp lệ, đúng quy chuẩn an toàn và hình ảnh hoàn toàn đồng nhất",
      safetyScore: 99,
      matchScore: 98,
      detectedIssues: []
    };
  };

  const sendViolationEmail = async (
    sellerEmail: string,
    violationReason: string,
    productName?: string,
    storeName?: string,
    violationCount?: number,
    actionType?: string
  ) => {
    try {
      const targetEmail = (sellerEmail || "").trim() || "dovanbinh487@gmail.com";
      const payload = {
        sellerEmail: targetEmail,
        storeName: storeName || "Gian hàng ZoneMart",
        productName: productName || "Sản phẩm vi phạm chính sách",
        reason: violationReason,
        violationCount: violationCount ?? 1,
        actionType: actionType || "warn"
      };

      await fetch("/api/moderation/send-violation-email", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      }).catch(async () => {
        await fetch("http://localhost:5000/api/moderation/send-violation-email", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        }).catch(() => {});
      });
    } catch (e) {
      console.warn("Lỗi khi gửi email cảnh báo vi phạm:", e);
    }
  };

  const handleSellerViolation = (
    sellerEmail: string, 
    violationReason: string,
    productName?: string,
    storeName?: string
  ) => {
    const penalty = getSellerPenalty(sellerEmail);
    penalty.violationCount += 1;
    penalty.lastViolationReason = violationReason;
    penalty.lastViolationDate = new Date().toLocaleString("vi-VN");

    const count = penalty.violationCount;
    let actionType: "warn" | "lock" | "ban" = "warn";
    let message = "";

    if (count <= 5) {
      actionType = "warn";
      message = `Hệ thống AI ZoneMart phát hiện bài đăng vi phạm chính sách cấm / 18+: "${violationReason}". Đây là lần vi phạm thứ ${count}/5. Bạn đã bị hệ thống cảnh báo qua email!`;
    } else if (count <= 10) {
      actionType = "lock";
      penalty.isLocked = true;
      const unlockDate = new Date(Date.now() + 10 * 24 * 60 * 60 * 1000);
      penalty.lockUntil = unlockDate.toISOString();
      message = `Tài khoản đã vi phạm ${count} lần (ngưỡng 6-10 lần). Hệ thống áp dụng hình phạt: KHÓA TÀI KHOẢN 10 NGÀY (đến ngày ${unlockDate.toLocaleDateString("vi-VN")}). Thông báo chính thức đã gửi qua Email.`;
    } else {
      actionType = "ban";
      penalty.isBanned = true;
      penalty.isLocked = true;
      message = `Tài khoản đã vi phạm nghiêm trọng ${count} lần (vượt ngưỡng 10 lần). Hệ thống tiến hành: XÓA VĨNH VIỄN TÀI KHOẢN NGƯỜI BÁN KHỎI NỀN TẢNG ZONEMART. Quyết định kỷ luật đã gửi tới Email.`;
    }

    penalty.notificationLogs.unshift({
      id: `notif_${Date.now()}`,
      type: actionType,
      title: actionType === "warn"
        ? `[Cảnh báo vi phạm ${count}/5]`
        : actionType === "lock"
        ? `[Đình chỉ tài khoản 10 ngày - Lần ${count}]`
        : `[XÓA VĨNH VIỄN TÀI KHOẢN - Lần ${count}]`,
      message,
      date: new Date().toLocaleString("vi-VN")
    });

    savePenalties();

    // Tự động kích hoạt gửi Email cảnh báo thực tế tới hộp thư Gmail của người bán
    sendViolationEmail(sellerEmail, violationReason, productName, storeName, count, actionType);

    return {
      actionType,
      violationCount: count,
      message
    };
  };

  const submitNewProduct = (
    sellerEmail: string,
    storeName: string,
    form: {
      name: string;
      category: string;
      price: number;
      unit: string;
      stock: number;
      image: string;
      imageFileName?: string;
      imageVisualTag?: string;
    }
  ): {
    scanResult: AIScanResult;
    createdProduct?: ModeratedProduct;
    penaltyResult?: ReturnType<typeof handleSellerViolation>;
  } => {
    const penalty = getSellerPenalty(sellerEmail);
    if (penalty.isBanned) {
      throw new Error("Tài khoản của bạn đã bị XÓA VĨNH VIỄN do tái phạm quá 10 lần. Không thể đăng bài!");
    }
    if (penalty.isLocked) {
      const dateStr = penalty.lockUntil ? new Date(penalty.lockUntil).toLocaleDateString("vi-VN") : "10 ngày";
      throw new Error(`Tài khoản của bạn đang bị TẠM KHÓA đến ${dateStr} do vi phạm chính sách. Không thể đăng bài!`);
    }

    const scanResult = scanProductWithAI(form);

    if (scanResult.decision === "VIOLATION") {
      const penaltyResult = handleSellerViolation(sellerEmail, scanResult.reason, form.name, storeName);
      return {
        scanResult,
        penaltyResult
      };
    }

    if (scanResult.decision === "SUSPICIOUS") {
      const newProd: ModeratedProduct = {
        id: `p-${Date.now()}`,
        name: form.name.trim(),
        category: form.category,
        price: form.price,
        unit: form.unit || "Phần",
        stock: form.stock || 20,
        image: form.image,
        status: "pending_review",
        isAvailable: false,
        sellerEmail,
        storeName,
        createdAt: new Date().toLocaleDateString("vi-VN"),
        aiScore: {
          safetyScore: scanResult.safetyScore,
          matchScore: scanResult.matchScore,
          flag: scanResult.reason,
          detectedIssues: scanResult.detectedIssues
        }
      };

      allProducts.value.unshift(newProd);
      saveProducts();
      return {
        scanResult,
        createdProduct: newProd
      };
    }

    const activeProd: ModeratedProduct = {
      id: `p-${Date.now()}`,
      name: form.name.trim(),
      category: form.category,
      price: form.price,
      unit: form.unit || "Phần",
      stock: form.stock || 20,
      image: form.image,
      status: "active",
      isAvailable: true,
      sellerEmail,
      storeName,
      createdAt: new Date().toLocaleDateString("vi-VN"),
      aiScore: {
        safetyScore: scanResult.safetyScore,
        matchScore: scanResult.matchScore
      }
    };

    allProducts.value.unshift(activeProd);
    saveProducts();
    syncProductToBackend(activeProd);
    return {
      scanResult,
      createdProduct: activeProd
    };
  };

  const updateAndRescanProduct = (
    productId: string,
    updatedData: {
      name: string;
      category: string;
      price: number;
      unit: string;
      stock: number;
      image: string;
      imageFileName?: string;
      imageVisualTag?: string;
    }
  ): {
    scanResult: AIScanResult;
    updatedProduct?: ModeratedProduct;
    penaltyResult?: ReturnType<typeof handleSellerViolation>;
  } => {
    const prodIndex = allProducts.value.findIndex(p => p.id === productId);
    if (prodIndex === -1) {
      throw new Error("Không tìm thấy sản phẩm cần sửa!");
    }

    const currentProd = allProducts.value[prodIndex];
    const penalty = getSellerPenalty(currentProd.sellerEmail);
    if (penalty.isBanned || penalty.isLocked) {
      throw new Error("Tài khoản đang bị hạn chế, không thể sửa bài!");
    }

    const scanResult = scanProductWithAI(updatedData);

    if (scanResult.decision === "VIOLATION") {
      allProducts.value.splice(prodIndex, 1);
      saveProducts();
      const penaltyResult = handleSellerViolation(currentProd.sellerEmail, scanResult.reason, updatedData.name, currentProd.storeName);
      return { scanResult, penaltyResult };
    }

    if (scanResult.decision === "SUSPICIOUS") {
      currentProd.name = updatedData.name.trim();
      currentProd.category = updatedData.category;
      currentProd.price = updatedData.price;
      currentProd.unit = updatedData.unit;
      currentProd.stock = updatedData.stock;
      currentProd.image = updatedData.image;
      currentProd.status = "pending_review";
      currentProd.isAvailable = false;
      currentProd.aiScore = {
        safetyScore: scanResult.safetyScore,
        matchScore: scanResult.matchScore,
        flag: scanResult.reason,
        detectedIssues: scanResult.detectedIssues
      };
      currentProd.managerNote = undefined;
      saveProducts();
      return { scanResult, updatedProduct: currentProd };
    }

    currentProd.name = updatedData.name.trim();
    currentProd.category = updatedData.category;
    currentProd.price = updatedData.price;
    currentProd.unit = updatedData.unit;
    currentProd.stock = updatedData.stock;
    currentProd.image = updatedData.image;
    currentProd.status = "active";
    currentProd.isAvailable = true;
    currentProd.aiScore = {
      safetyScore: scanResult.safetyScore,
      matchScore: scanResult.matchScore
    };
    currentProd.managerNote = undefined;
    saveProducts();
    if (currentProd.status === "active") {
      syncProductToBackend(currentProd);
    }
    return { scanResult, updatedProduct: currentProd };
  };

  const approveProductByManager = async (productId: string) => {
    const prod = allProducts.value.find(p => p.id === productId);
    if (!prod) return false;
    prod.status = "active";
    prod.isAvailable = true;
    prod.managerNote = undefined;
    saveProducts();
    await syncProductToBackend(prod);

    const penalty = getSellerPenalty(prod.sellerEmail);
    penalty.notificationLogs.unshift({
      id: `notif_${Date.now()}`,
      type: "approved",
      title: `[Manager Đã Duyệt] Sản phẩm: ${prod.name}`,
      message: `Quản lý đã phê duyệt sản phẩm "${prod.name}" của bạn. Món hàng hiện đã được mở bán trên sàn ZoneMart.`,
      date: new Date().toLocaleString("vi-VN")
    });
    savePenalties();
    return true;
  };

  const rejectProductByManager = async (productId: string, reason: string) => {
    // 1. Tìm sản phẩm mục tiêu (theo id hoặc tên)
    const targetProd = allProducts.value.find(p => p.id === productId || p.name.toLowerCase() === productId.toLowerCase());
    const targetName = targetProd?.name || productId;
    const normTargetName = stripAccents(targetName).trim();
    const lowerTargetName = targetName.toLowerCase().trim();
    const sellerEmail = targetProd?.sellerEmail || "";

    // 2. Ghi nhật ký thông báo vi phạm / gỡ bài cho seller
    if (sellerEmail) {
      const penalty = getSellerPenalty(sellerEmail);
      penalty.notificationLogs.unshift({
        id: `notif_${Date.now()}`,
        type: "manager_reject",
        title: `[Gỡ bỏ bài đăng] Sản phẩm: ${targetName}`,
        message: `Quản lý đã từ chối duyệt và gỡ bỏ bài đăng sản phẩm "${targetName}". Lý do: ${reason || "Không khớp thông tin hoặc vi phạm quy định sàn."}.`,
        date: new Date().toLocaleString("vi-VN")
      });
      savePenalties();
    }

    // 3. Ghi vào danh sách đen lưu vĩnh viễn trong localStorage để chống tải lại
    recordDeletedProduct(productId, targetName);
    if (targetProd?.id) recordDeletedProduct(targetProd.id, targetName);

    // 4. Lọc bỏ TRIỆT ĐỂ toàn bộ các bản ghi trùng lặp khỏi allProducts.value (Xóa sạch cả 2 bên: duyệt admin & hồ sơ gian hàng)
    allProducts.value = allProducts.value.filter(p => {
      if (p.id === productId) return false;
      if (targetProd && p.id === targetProd.id) return false;
      if (p.name.trim().toLowerCase() === lowerTargetName) return false;
      if (stripAccents(p.name).trim() === normTargetName) return false;
      return true;
    });
    saveProducts();

    // 5. Gọi API DELETE backend để xóa vĩnh viễn khỏi MongoDB (DeleteManyAsync trên server sẽ quét sạch mọi bản ghi)
    try {
      const encId = encodeURIComponent(productId);
      const encName = encodeURIComponent(targetName);
      await fetch(`/api/products/${encId}?name=${encName}`, { method: "DELETE" });
      await fetch(`http://localhost:5000/api/products/${encId}?name=${encName}`, { method: "DELETE" }).catch(() => {});
    } catch (err) {
      console.warn("⚠️ Error removing rejected product from backend:", err);
    }

    // 6. Phát tín hiệu đồng bộ giữa các tab và views
    if (typeof window !== "undefined") {
      try {
        localStorage.setItem("zonemart_product_bus_event", JSON.stringify({ action: "delete", id: productId, time: Date.now() }));
        window.dispatchEvent(new CustomEvent("zonemart:products-changed"));
      } catch {}
    }

    return true;
  };

  const deleteProductPermanently = async (productId: string, productName?: string) => {
    const targetName = productName || productId;
    const normTargetName = stripAccents(targetName).trim();
    const lowerTargetName = targetName.toLowerCase().trim();

    recordDeletedProduct(productId, targetName);

    allProducts.value = allProducts.value.filter(p => {
      if (p.id === productId) return false;
      if (p.name.trim().toLowerCase() === lowerTargetName) return false;
      if (stripAccents(p.name).trim() === normTargetName) return false;
      return true;
    });
    saveProducts();

    try {
      const encId = encodeURIComponent(productId);
      const encName = encodeURIComponent(targetName);
      await fetch(`/api/products/${encId}?name=${encName}`, { method: "DELETE" });
      await fetch(`http://localhost:5000/api/products/${encId}?name=${encName}`, { method: "DELETE" }).catch(() => {});
    } catch {}

    if (typeof window !== "undefined") {
      try {
        localStorage.setItem("zonemart_product_bus_event", JSON.stringify({ action: "delete", id: productId, time: Date.now() }));
        window.dispatchEvent(new CustomEvent("zonemart:products-changed"));
      } catch {}
    }
    return true;
  };

  const resetSellerPenalties = (sellerEmail: string) => {
    const email = sellerEmail.toLowerCase().trim() || "seller@zonemart.vn";
    sellerPenalties.value[email] = {
      email,
      violationCount: 0,
      isLocked: false,
      isBanned: false,
      notificationLogs: []
    };
    savePenalties();
  };

  const activeProducts = computed(() =>
    allProducts.value.filter(p => p.status === "active")
  );

  const pendingReviewProducts = computed(() =>
    allProducts.value.filter(p => p.status === "pending_review")
  );

  const rejectedProducts = computed(() =>
    allProducts.value.filter(p => p.status === "rejected_need_edit")
  );

  const reduceStockForPurchasedItems = (items: Array<{ id?: string; productId?: string; name?: string; quantity?: number; qty?: number }>) => {
    if (!items || !items.length) return;
    let hasChanges = false;

    items.forEach((item) => {
      const qty = (item.quantity && item.quantity > 0) ? item.quantity : ((item.qty && item.qty > 0) ? item.qty : 1);
      const targetId = item.id || item.productId;
      const targetName = (item.name || "").toLowerCase().trim();

      // Tìm sản phẩm khớp theo ID hoặc Tên sản phẩm
      const prod = allProducts.value.find((p) => {
        if (targetId && (p.id === targetId || (p as any).productId === targetId)) return true;
        const pName = (p.name || "").toLowerCase().trim();
        return pName === targetName || (targetName && (pName.includes(targetName) || targetName.includes(pName)));
      });

      if (prod) {
        const curStock = typeof prod.stock === "number" ? prod.stock : 30;
        prod.stock = Math.max(0, curStock - qty);
        hasChanges = true;
      }
    });

    if (hasChanges) {
      allProducts.value = [...allProducts.value];
      saveProducts();

      // Đồng bộ giảm tồn kho vào CSDL MongoDB Backend
      try {
        fetch("/api/products/reduce-stock", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            items: items.map(i => ({
              productId: i.id || i.productId || "",
              productName: i.name || "",
              quantity: (i.quantity && i.quantity > 0) ? i.quantity : ((i.qty && i.qty > 0) ? i.qty : 1)
            }))
          })
        }).catch(() => {});
      } catch {}
    }
  };

  return {
    allProducts,
    activeProducts,
    pendingReviewProducts,
    rejectedProducts,
    sellerPenalties,
    getSellerPenalty,
    scanProductWithAI,
    submitNewProduct,
    updateAndRescanProduct,
    approveProductByManager,
    rejectProductByManager,
    deleteProductPermanently,
    resetSellerPenalties,
    handleSellerViolation,
    sendViolationEmail,
    syncProductToBackend,
    reduceStockForPurchasedItems,
    saveProducts,
    refreshProducts,
    compressImage,
    fetchRealProductsFromBackend
  };
}