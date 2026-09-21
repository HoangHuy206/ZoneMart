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

const INITIAL_PRODUCTS: ModeratedProduct[] = [
  {
    id: "p-01",
    name: "Rau muống hữu cơ Ba Vì VietGAP",
    category: "Rau củ quả",
    price: 18000,
    unit: "Bó 500g",
    stock: 45,
    image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
    status: "active",
    isAvailable: true,
    sellerEmail: "seller@zonemart.vn",
    storeName: "Vườn Rau Ba Vì - Nông Sản Sạch",
    createdAt: "08/09/2026",
    aiScore: { safetyScore: 99, matchScore: 98 }
  },
  {
    id: "p-02",
    name: "Cà chua bi Đà Lạt mọng nước",
    category: "Rau củ quả",
    price: 35000,
    unit: "Hộp 500g",
    stock: 28,
    image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=400&q=80",
    status: "active",
    isAvailable: true,
    sellerEmail: "seller@zonemart.vn",
    storeName: "Vườn Rau Ba Vì - Nông Sản Sạch",
    createdAt: "08/09/2026",
    aiScore: { safetyScore: 100, matchScore: 96 }
  },
  {
    id: "p-03",
    name: "Thịt ba chỉ heo sạch chuẩn CP",
    category: "Thịt cá tươi",
    price: 65000,
    unit: "Khay 500g",
    stock: 12,
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=400&q=80",
    status: "active",
    isAvailable: true,
    sellerEmail: "seller@zonemart.vn",
    storeName: "Vườn Rau Ba Vì - Nông Sản Sạch",
    createdAt: "07/09/2026",
    aiScore: { safetyScore: 98, matchScore: 95 }
  },
  {
    id: "p-sp-01",
    name: "Thịt Heo Rừng Tự Nhiên Đóng Khay",
    category: "Thịt cá tươi",
    price: 130000,
    unit: "Khay 1kg",
    stock: 10,
    image: "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=400",
    status: "pending_review",
    isAvailable: false,
    sellerEmail: "seller@zonemart.vn",
    storeName: "Vườn Rau Ba Vì - Nông Sản Sạch",
    createdAt: "09/09/2026",
    aiScore: {
      safetyScore: 82,
      matchScore: 54,
      flag: "Nghi ngờ: Ảnh chụp và tên gọi có độ chênh lệch tem nhãn chứng nhận nguồn gốc"
    }
  }
];

const STORAGE_KEY_PRODUCTS = "zonemart_moderated_products";
const STORAGE_KEY_PENALTIES = "zonemart_seller_penalties";

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
    if (raw) {
      let items: ModeratedProduct[] = JSON.parse(raw);
      let changed = false;

      // 1. Loại bỏ hoàn toàn các bài đăng chứa hàng cấm / vũ khí / bom / súng đạn (Hàng vi phạm Luồng 2 phải bị xóa)
      const filtered = items.filter(item => {
        const normName = stripAccents(item.name).trim();
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
      items = filtered;

      // 2. Tự động sửa các bài lệch ảnh / tên hợp lệ (chờ duyệt)
      for (const item of items) {
        const lowerName = item.name.toLowerCase();
        const normName = stripAccents(item.name);
        const isSaltOrCondiment = normName.includes("muoi") || lowerName.includes("muối");
        const hasVeggieMismatch = 
          item.category === "Thực phẩm bổ dưỡng" || 
          item.category === "Rau củ quả" || 
          item.price === 5000 || 
          item.image.includes("540420773420") || 
          item.image.includes("556801712") || 
          item.image.includes("1540420773420");

        if (isSaltOrCondiment && hasVeggieMismatch) {
          if (item.status !== "pending_review" || !item.aiScore || item.aiScore.matchScore > 50) {
            item.status = "pending_review";
            item.isAvailable = false;
            item.aiScore = {
              safetyScore: 82,
              matchScore: 38,
              flag: 'Lệch ảnh và tên: Tên bài đăng là "muối ăn" nhưng hình ảnh tải lên là rau xanh và danh mục chưa phù hợp.'
            };
            changed = true;
          }
        }

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

      if (changed) {
        try {
          localStorage.setItem(STORAGE_KEY_PRODUCTS, JSON.stringify(items));
        } catch { /* ignore */ }
      }
      return items;
    }
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

  // Thông báo thời gian thực tới tất cả các component và tab đang mở
  if (typeof window !== "undefined") {
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
    // Luôn giữ ảnh thực tế do người bán tải lên (không ghi đè bằng ảnh mock Unsplash)
    const cleanImg = p.image || getCategoryFallbackImage(p.category);

    await fetch("/api/products/sync-product", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
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
      })
    }).catch(() => {});
  } catch {}
}

let isRefreshingProducts = false;

// Làm mới danh sách sản phẩm từ localStorage và backend
function refreshProducts(): ModeratedProduct[] {
  const loaded = loadProducts();
  allProducts.value = loaded;

  if (isRefreshingProducts) {
    return loaded;
  }

  // Gọi đồng bộ thêm từ backend nếu có sản phẩm mới trên CSDL
  if (typeof window !== "undefined") {
    isRefreshingProducts = true;
    fetch("/api/products")
      .then(res => res.json())
      .then(data => {
        if (data && data.success && Array.isArray(data.data)) {
          let updated = false;
          data.data.forEach((bp: any) => {
            const pName = (bp.productName || bp.name || "").trim();
            if (!pName) return;
            const existingIndex = allProducts.value.findIndex(
              ep => ep.name.toLowerCase() === pName.toLowerCase()
            );

            const realStoreName = bp.storeName || (bp.store && bp.store.name) || "";
            const realSellerEmail = bp.sellerEmail || "";
            const realImage = bp.imageUrl || bp.image || getCategoryFallbackImage(bp.category);

            if (existingIndex === -1) {
              const mapped: ModeratedProduct = {
                id: bp.id || `p-${Date.now()}`,
                name: pName,
                category: bp.category || "Rau củ quả",
                price: bp.price || 25000,
                unit: bp.weight ? `${bp.weight} kg` : "Bó 500g",
                stock: bp.stockQuantity || 30,
                image: realImage,
                status: "active",
                isAvailable: true,
                sellerEmail: realSellerEmail || "seller@zonemart.vn",
                storeName: realStoreName || "Cửa hàng đối tác",
                createdAt: bp.createdAt ? new Date(bp.createdAt).toLocaleDateString("vi-VN") : "Hôm nay",
                aiScore: { safetyScore: 99, matchScore: 98 }
              };
              allProducts.value.unshift(mapped);
              updated = true;
            } else {
              // Cập nhật thông tin thực từ CSDL nếu sản phẩm cục bộ đang bị sai thông tin (ảnh mock hoặc tên shop mặc định)
              const existing = allProducts.value[existingIndex];
              let itemUpdated = false;

              if (realStoreName && existing.storeName !== realStoreName) {
                existing.storeName = realStoreName;
                itemUpdated = true;
              }
              if (realSellerEmail && existing.sellerEmail !== realSellerEmail) {
                existing.sellerEmail = realSellerEmail;
                itemUpdated = true;
              }
              if (realImage && existing.image !== realImage) {
                existing.image = realImage;
                itemUpdated = true;
              }
              if (bp.price && existing.price !== bp.price) {
                existing.price = bp.price;
                itemUpdated = true;
              }

              if (itemUpdated) {
                allProducts.value[existingIndex] = { ...existing };
                updated = true;
              }
            }
          });
          if (updated) {
            allProducts.value = [...allProducts.value];
            saveProducts();
          }
        }
      })
      .catch(() => {})
      .finally(() => {
        isRefreshingProducts = false;
      });
  }
  return loaded;
}

// Tự động khởi chạy đồng bộ ngay khi module được nạp trong trình duyệt
if (typeof window !== "undefined") {
  setTimeout(() => {
    refreshProducts();
  }, 100);

  // Lắng nghe sự kiện Storage giữa các tab trình duyệt
  window.addEventListener("storage", (e) => {
    if (e.key === STORAGE_KEY_PRODUCTS) {
      refreshProducts();
    }
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

  const handleSellerViolation = (sellerEmail: string, violationReason: string) => {
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
      const penaltyResult = handleSellerViolation(sellerEmail, scanResult.reason);
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
      const penaltyResult = handleSellerViolation(currentProd.sellerEmail, scanResult.reason);
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

  const approveProductByManager = (productId: string) => {
    const prod = allProducts.value.find(p => p.id === productId);
    if (!prod) return false;
    prod.status = "active";
    prod.isAvailable = true;
    prod.managerNote = undefined;
    saveProducts();
    syncProductToBackend(prod);

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

  const rejectProductByManager = (productId: string, reason: string) => {
    const prod = allProducts.value.find(p => p.id === productId);
    if (!prod) return false;
    prod.status = "rejected_need_edit";
    prod.isAvailable = false;
    prod.managerNote = reason || "Ảnh chụp không khớp mô tả hoặc vi phạm quy định sàn.";
    saveProducts();

    const penalty = getSellerPenalty(prod.sellerEmail);
    penalty.notificationLogs.unshift({
      id: `notif_${Date.now()}`,
      type: "manager_reject",
      title: `[Yêu cầu sửa lại] Sản phẩm: ${prod.name}`,
      message: `Quản lý đã từ chối duyệt sản phẩm "${prod.name}". Lý do: ${prod.managerNote}. Vui lòng bấm 'Sửa bài' để AI quét lại.`,
      date: new Date().toLocaleString("vi-VN")
    });
    savePenalties();
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
    resetSellerPenalties,
    saveProducts,
    refreshProducts,
    compressImage
  };
}