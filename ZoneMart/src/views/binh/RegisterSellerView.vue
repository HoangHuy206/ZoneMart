<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG KÝ NGƯỜI BÁN / MỞ GIAN HÀNG (REGISTER SELLER VIEW) - Phụ trách: Bình
 * Luồng 3 Bước Chuẩn Đồ Án ZoneMart với Quy tắc Validate Ràng Buộc Chặt Chẽ:
 * - Bước 1: Thông tin tiệm (Tên cửa hàng 12-30 ký tự không ký tự đặc biệt, Địa chỉ tiệm chuẩn mẫu không ký tự đặc biệt)
 * - Bước 2: Xác thực chủ tiệm (Họ tên 6-16 ký tự không ký tự đặc biệt, Bắt buộc 3 ảnh: CCCD mặt trước, mặt sau & Giấy CN An toàn thực phẩm, Chặn file video .mp4)
 * - Bước 3: Thanh toán (Số tài khoản 8-15 chữ số)
 * Tích hợp Custom Dropdown Select mở thả xuôi xuống dưới có thanh cuộn đồng bộ hoàn toàn với RegisterShipperView!
 * ================================================================
 */
import { ref, reactive, computed, watch, onMounted, onUnmounted, nextTick } from "vue";
import { useRouter } from "vue-router";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import {
  INNER_DISTRICTS,
  OUTER_DISTRICTS,
  ALL_HANOI_DISTRICTS,
  ALL_HANOI_WARDS,
  ALL_HANOI_MARKETS,
  getWardsForDistrict,
  getMarketsForLocation,
  type WardItem,
  type MarketItem,
} from "../../data/hanoiAdminUnits";
import { getSafeCurrentLocation } from "../../composables/useSafeLocation";

const router = useRouter();

// Trạng thái Bước (Step 1 -> Step 2 -> Step 3)
const currentStep = ref<1 | 2 | 3>(1);

// Trạng thái khu vực & định vị GPS
const isCustomDistrict = ref(false);
const isAutoLocating = ref(false);

const selectedDistrictValue = computed(() => {
  if (isCustomDistrict.value) return "__custom_district__";
  return form.district || "Quận Cầu Giấy";
});

const systemWards = ref<WardItem[]>([...ALL_HANOI_WARDS]);
const systemMarkets = ref<MarketItem[]>([...ALL_HANOI_MARKETS]);

// Danh sách xã / phường tương ứng chính xác 100% với quận được chọn
const wardsInSelectedDistrict = computed(() => {
  if (!form.district || isCustomDistrict.value) return [];
  const distNorm = form.district.toLowerCase().trim();
  let list = systemWards.value.filter(w => {
    if (!w.districtName) return false;
    const d = w.districtName.toLowerCase().trim();
    return d === distNorm || distNorm.includes(d) || d.includes(distNorm);
  });
  if (list.length === 0) {
    list = getWardsForDistrict(form.district);
  }
  return list;
});

// Danh sách chợ truyền thống được lọc chuẩn theo Quận & Xã/Phường
const filteredMarkets = computed(() => {
  if (!form.district || isCustomDistrict.value) return [];
  const dist = form.district;
  const wardId = form.wardId;
  
  // 1. Ưu tiên lọc theo phường cụ thể
  if (wardId && wardId !== "__custom_ward__") {
    const list = systemMarkets.value.filter(m => m.wardId === wardId);
    if (list.length > 0) return list;
  }

  // 2. Lọc theo quận/huyện
  const dNorm = dist.toLowerCase().trim();
  const inDistrict = systemMarkets.value.filter(m => {
    if (!m.districtName) return false;
    const md = m.districtName.toLowerCase().trim();
    return md === dNorm || dNorm.includes(md) || md.includes(dNorm);
  });
  if (inDistrict.length > 0) return inDistrict;

  // 3. Fallback qua helper chuẩn
  return getMarketsForLocation(dist, wardId);
});

const selectedMarketInfo = computed(() => {
  if (!form.marketId || form.marketId === '__custom__') return null;
  return systemMarkets.value.find(m => m.id === form.marketId) || null;
});

const loadSystemWardsAndMarkets = async () => {
  try {
    const [wRes, mRes] = await Promise.all([
      fetch("/api/MarketManagement/wards").then(r => r.json()).catch(() => null),
      fetch("/api/MarketManagement/markets").then(r => r.json()).catch(() => null)
    ]);
    if (wRes && wRes.success && Array.isArray(wRes.data) && wRes.data.length > 0) {
      const dbWardMap = new Map<string, any>();
      wRes.data.forEach((w: any) => {
        if (w.wardName) dbWardMap.set(w.wardName.trim().toLowerCase(), w);
        if (w.id) dbWardMap.set(w.id, w);
      });

      const merged = ALL_HANOI_WARDS.map(w => {
        const dbW = dbWardMap.get(w.wardName.trim().toLowerCase());
        if (dbW) {
          return { ...w, id: dbW.id || w.id, wardCode: dbW.wardCode || w.wardCode };
        }
        return w;
      });

      const existingIds = new Set(merged.map(w => w.id));
      wRes.data.forEach((w: any) => {
        if (!existingIds.has(w.id)) {
          merged.push(w);
          existingIds.add(w.id);
        }
      });
      systemWards.value = merged;
    } else {
      systemWards.value = [...ALL_HANOI_WARDS];
    }

    if (mRes && mRes.success && Array.isArray(mRes.data) && mRes.data.length > 0) {
      const dbMarketMap = new Map<string, any>();
      mRes.data.forEach((m: any) => {
        if (m.marketName) dbMarketMap.set(m.marketName.trim().toLowerCase(), m);
        if (m.id) dbMarketMap.set(m.id, m);
      });

      const mergedM = ALL_HANOI_MARKETS.map(m => {
        const dbM = dbMarketMap.get(m.marketName.trim().toLowerCase());
        if (dbM) {
          return { ...m, id: dbM.id || m.id, marketCode: dbM.marketCode || m.marketCode };
        }
        return m;
      });

      const existingMarketIds = new Set(mergedM.map(m => m.id));
      mRes.data.forEach((m: any) => {
        if (!existingMarketIds.has(m.id)) {
          mergedM.push(m);
          existingMarketIds.add(m.id);
        }
      });
      systemMarkets.value = mergedM;
    } else {
      systemMarkets.value = [...ALL_HANOI_MARKETS];
    }
  } catch (err) {
    console.warn("Could not load wards/markets:", err);
    systemWards.value = [...ALL_HANOI_WARDS];
    systemMarkets.value = [...ALL_HANOI_MARKETS];
  } finally {
    if (!form.district) form.district = "Quận Cầu Giấy";
    if (!form.wardId) {
      const firstW = wardsInSelectedDistrict.value[0] || systemWards.value[0];
      if (firstW) {
        form.wardId = firstW.id;
        form.ward = firstW.wardName;
      }
    }
    if (!form.marketId && filteredMarkets.value.length > 0) {
      form.marketId = filteredMarkets.value[0].id;
      form.marketName = filteredMarkets.value[0].marketName;
      if (!form.address) form.address = filteredMarkets.value[0].address || "";
    }
  }
};

const onDistrictChange = (e: any) => {
  const chosenDistrict = e.target.value;
  if (chosenDistrict === "__custom_district__") {
    isCustomDistrict.value = true;
    form.district = "";
    form.wardId = "__custom_ward__";
    form.ward = "";
    form.marketId = "";
    form.marketName = "";
  } else {
    isCustomDistrict.value = false;
    form.district = chosenDistrict;
    
    // Đồng bộ ngay lập tức xã/phường cho quận này
    const distNorm = chosenDistrict.toLowerCase().trim();
    let wards = systemWards.value.filter(w => {
      if (!w.districtName) return false;
      const d = w.districtName.toLowerCase().trim();
      return d === distNorm || distNorm.includes(d) || d.includes(distNorm);
    });
    if (wards.length === 0) {
      wards = getWardsForDistrict(chosenDistrict);
    }

    if (wards.length > 0) {
      const firstW = wards[0];
      form.wardId = firstW.id;
      form.ward = firstW.wardName;

      // Tìm chợ truyền thống phù hợp
      const markets = getMarketsForLocation(chosenDistrict, firstW.id);
      if (markets.length > 0) {
        form.marketId = markets[0].id;
        form.marketName = markets[0].marketName;
        form.address = markets[0].address || `${firstW.wardName}, ${chosenDistrict}, Hà Nội`;
      } else {
        form.marketId = "__custom__";
        form.marketName = `Chợ ${firstW.wardName.replace(/^(Phường|Xã|Thị trấn)\s+/i, '')}`;
        form.address = `${firstW.wardName}, ${chosenDistrict}, Hà Nội`;
      }
    } else {
      form.wardId = "__custom_ward__";
      form.ward = "";
      form.marketId = "__custom__";
      form.marketName = "";
    }
  }
};

const onWardChange = (e: any) => {
  onWardSelect(e.target.value);
};

const onWardSelect = (chosenWardId: string) => {
  form.wardId = chosenWardId;
  if (chosenWardId === "__custom_ward__") {
    form.ward = "";
    form.marketId = "__custom__";
    form.marketName = "";
    return;
  }
  const w = systemWards.value.find(x => x.id === chosenWardId) || ALL_HANOI_WARDS.find(x => x.id === chosenWardId);
  if (w) {
    form.ward = w.wardName;
    if (w.districtName && !isCustomDistrict.value) form.district = w.districtName;
  }
  
  // Cập nhật chợ tương ứng
  const markets = getMarketsForLocation(form.district, chosenWardId);
  if (markets.length > 0) {
    form.marketName = markets[0].marketName;
    form.marketId = markets[0].id;
    if (!form.address || form.address.includes("Chợ") || form.address.includes("Hà Nội")) {
      form.address = markets[0].address || `${form.ward}, ${form.district}, Hà Nội`;
    }
  } else {
    form.marketName = `Chợ ${form.ward ? form.ward.replace(/^(Phường|Xã|Thị trấn)\s+/i, '') : ''}`;
    form.marketId = "__custom__";
    if (!form.address || form.address.includes("Chợ") || form.address.includes("Hà Nội")) {
      form.address = `${form.ward}, ${form.district}, Hà Nội`;
    }
  }
};

const onMarketChange = (e: any) => {
  const val = e.target.value;
  if (val === "__custom__") {
    form.marketId = "__custom__";
    form.marketName = "";
  } else {
    form.marketId = val;
    const m = systemMarkets.value.find(x => x.id === val) || ALL_HANOI_MARKETS.find(x => x.id === val);
    if (m) {
      form.marketName = m.marketName;
      if (m.address) {
        form.address = m.address;
      }
    }
  }
};

// Định vị GPS 1 chạm
const autoDetectLocation = async () => {
  isAutoLocating.value = true;
  try {
    const loc = await getSafeCurrentLocation();
    if (loc && loc.safeLocationName) {
      const locText = loc.safeLocationName;
      let matchedDistrict = "";
      for (const d of ALL_HANOI_DISTRICTS) {
        const dClean = d.replace(/^(Quận|Huyện|Thị xã)\s+/i, '').toLowerCase().trim();
        if (locText.toLowerCase().includes(d.toLowerCase()) || locText.toLowerCase().includes(dClean)) {
          matchedDistrict = d;
          break;
        }
      }

      if (matchedDistrict) {
        form.district = matchedDistrict;
        isCustomDistrict.value = false;
        
        const wards = getWardsForDistrict(matchedDistrict);
        let matchedWard = null;
        for (const w of wards) {
          const wClean = w.wardName.replace(/^(Phường|Xã|Thị trấn)\s+/i, '').toLowerCase().trim();
          if (locText.toLowerCase().includes(w.wardName.toLowerCase()) || locText.toLowerCase().includes(wClean)) {
            matchedWard = w;
            break;
          }
        }

        if (matchedWard) {
          onWardSelect(matchedWard.id);
        } else if (wards.length > 0) {
          onWardSelect(wards[0].id);
        }

        if (!form.address) {
          form.address = locText;
        }
        if (loc.lat && loc.lon) {
          form.lat = loc.lat;
          form.lng = loc.lon;
        }
      }
    }
  } catch (err: any) {
    console.error("GPS auto-detect error:", err);
  } finally {
    isAutoLocating.value = false;
  }
};

// Dữ liệu Form Đăng Ký Người Bán
const form = reactive({
  // BƯỚC 1: THÔNG TIN TIỆM
  storeName: "",
  sellerType: "traditional_market",
  ward: "",
  wardId: "",
  district: "",
  marketName: "",
  marketId: "",
  stallNumber: "",
  category: "Thực phẩm & Nhu yếu phẩm",
  address: "",
  lat: 0,
  lng: 0,
  openHours: "07:00 - 22:00",
  phoneEmail: "",

  // BƯỚC 2: XÁC THỰC CHỦ TIỆM & GIẤY TỜ
  ownerFullName: "",
  cccdNumber: "",         // Số CCCD chính xác (nếu có hoặc để trống)
  nguyenQuan: "",         // Nguyên quán từ CCCD
  cccdFrontImage: "",     // Preview URL hoặc Base64
  cccdBackImage: "",      // Preview URL hoặc Base64
  foodSafetyCertImage: "",// Preview URL hoặc Base64 ảnh Giấy CN An toàn thực phẩm

  // BƯỚC 3: THANH TOÁN & TÀI KHOẢN ĐĂNG NHẬP
  bankName: "Vietcombank (VCB)",
  bankAccountNumber: "",
  password: ""
});

const isLoading = ref(false);
const errorMessage = ref("");

interface CurrentUser {
  id?: string;
  fullName?: string;
  phoneEmail?: string;
  role?: string;
  cccdNumber?: string;
  nguyenQuan?: string;
}

const currentUser = ref<CurrentUser | null>(null);

onMounted(() => {
  const savedUserStr = localStorage.getItem("currentUser");
  if (savedUserStr) {
    try {
      const parsed = JSON.parse(savedUserStr);
      if (parsed && (parsed.phoneEmail || parsed.fullName)) {
        currentUser.value = parsed;
        if (parsed.fullName && !form.ownerFullName) {
          form.ownerFullName = parsed.fullName;
        }
        if (parsed.phoneEmail && !form.phoneEmail) {
          form.phoneEmail = parsed.phoneEmail;
        }
        if (parsed.cccdNumber && !form.cccdNumber) {
          form.cccdNumber = parsed.cccdNumber;
        }
        if (parsed.nguyenQuan && !form.nguyenQuan) {
          form.nguyenQuan = parsed.nguyenQuan;
        }
      }
    } catch (e) {}
  }
});

const goToLoginWithIntent = (target: "seller" | "shipper") => {
  sessionStorage.setItem("pendingRegisterTarget", target);
  router.push("/seller-login");
};

// Trạng thái Modal Pop-up Thành Công giữa màn hình
const showSuccessModal = ref(false);

// Trạng thái Custom Dropdowns (Luôn mở thả xuôi xuống dưới)
const isCategoryDropdownOpen = ref(false);
const isOpenHoursDropdownOpen = ref(false);
const isOpenTimeDropdownOpen = ref(false);
const isCloseTimeDropdownOpen = ref(false);
const isBankDropdownOpen = ref(false);

// Danh mục Ngành hàng chính
const categoriesList = [
  "Thực phẩm & Nhu yếu phẩm",
  "Trái cây & Đồ uống",
  "Cơm & Thức ăn nhanh",
  "Đồ gia dụng tiện ích",
  "Thời trang & Phụ kiện",
  "Mỹ phẩm & Chăm sóc cá nhân"
];

// Danh sách Khung giờ mở cửa phổ biến
const openHoursOptions = [
  "07:00 - 22:00 (Mở cả ngày & Buổi tối)",
  "06:00 - 21:00 (Mở sớm - Đóng sớm)",
  "08:00 - 20:00 (Giờ hành chính)",
  "07:00 - 23:00 (Phục vụ đêm)",
  "24/24 (Mở cửa 24/7 toàn thời gian)",
  "Tùy chọn khung giờ mở - đóng khác..."
];

const selectedPreset = ref("07:00 - 22:00 (Mở cả ngày & Buổi tối)");
const openTime = ref("07:00");
const closeTime = ref("22:00");

const openTimeList = ["05:00", "05:30", "06:00", "06:30", "07:00", "07:30", "08:00", "08:30", "09:00", "09:30", "10:00"];
const closeTimeList = ["17:00", "18:00", "19:00", "20:00", "21:00", "21:30", "22:00", "22:30", "23:00", "23:30", "24:00"];

const updateOpenHours = () => {
  if (selectedPreset.value === "Tùy chọn khung giờ mở - đóng khác...") {
    form.openHours = `${openTime.value} - ${closeTime.value}`;
  } else if (selectedPreset.value === "24/24 (Mở cửa 24/7 toàn thời gian)") {
    form.openHours = "Mở cửa 24/7";
  } else {
    form.openHours = selectedPreset.value.split(" (")[0];
  }
};

// Danh sách Ngân hàng phổ biến
const banksList = [
  "Vietcombank (VCB)",
  "MB Bank (MB)",
  "Techcombank (TCB)",
  "VietinBank (CTG)",
  "BIDV",
  "VPBank",
  "Agribank",
  "TPBank",
  "ACB",
  "Sacombank"
];

// Helper đóng tất cả dropdown ngoại trừ dropdown đang bấm
const closeAllDropdownsExcept = (activeName?: string) => {
  if (activeName !== 'category') isCategoryDropdownOpen.value = false;
  if (activeName !== 'openHours') isOpenHoursDropdownOpen.value = false;
  if (activeName !== 'openTime') isOpenTimeDropdownOpen.value = false;
  if (activeName !== 'closeTime') isCloseTimeDropdownOpen.value = false;
  if (activeName !== 'bank') isBankDropdownOpen.value = false;
};

// Toggle handlers
const toggleCategoryDropdown = () => {
  closeAllDropdownsExcept('category');
  isCategoryDropdownOpen.value = !isCategoryDropdownOpen.value;
};

const toggleOpenHoursDropdown = () => {
  closeAllDropdownsExcept('openHours');
  isOpenHoursDropdownOpen.value = !isOpenHoursDropdownOpen.value;
};

const toggleOpenTimeDropdown = () => {
  closeAllDropdownsExcept('openTime');
  isOpenTimeDropdownOpen.value = !isOpenTimeDropdownOpen.value;
};

const toggleCloseTimeDropdown = () => {
  closeAllDropdownsExcept('closeTime');
  isCloseTimeDropdownOpen.value = !isCloseTimeDropdownOpen.value;
};

const toggleBankDropdown = () => {
  closeAllDropdownsExcept('bank');
  isBankDropdownOpen.value = !isBankDropdownOpen.value;
};

// Select handlers
const selectCategory = (cat: string) => {
  form.category = cat;
  isCategoryDropdownOpen.value = false;
};

const selectPresetHours = (preset: string) => {
  selectedPreset.value = preset;
  updateOpenHours();
  isOpenHoursDropdownOpen.value = false;
};

const selectOpenTime = (time: string) => {
  openTime.value = time;
  updateOpenHours();
  isOpenTimeDropdownOpen.value = false;
};

const selectCloseTime = (time: string) => {
  closeTime.value = time;
  updateOpenHours();
  isCloseTimeDropdownOpen.value = false;
};

const selectBank = (bank: string) => {
  form.bankName = bank;
  isBankDropdownOpen.value = false;
};

// Tự động đóng dropdown khi click ngoài
const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (!target.closest('.custom-select-container')) {
    closeAllDropdownsExcept();
  }
};

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
  loadSystemWardsAndMarkets();
  const savedUser = localStorage.getItem("currentUser");
  if (savedUser) {
    try {
      const parsed = JSON.parse(savedUser);
      if (parsed.phoneEmail) form.phoneEmail = parsed.phoneEmail;
      if (parsed.fullName && !form.ownerFullName) form.ownerFullName = parsed.fullName;
    } catch {}
  }
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  if (mapInstance) {
    try {
      mapInstance.remove();
      mapInstance = null;
    } catch (e) {}
  }
});

// ================= ĐỊNH VỊ BẢN ĐỒ THỜI GIAN THỰC CHO GIAN HÀNG =================
const isLocationModalOpen = ref(false);
const tempLat = ref(21.0333);
const tempLng = ref(105.7983);
const tempLocationAddress = ref("");
const mapSearchQuery = ref("");
const isLocatingGps = ref(false);
const isSearchingLocation = ref(false);
const isGeocodingAddress = ref(false);
const showLocationSuccessToast = ref(false);

let mapInstance: L.Map | null = null;
let markerInstance: L.Marker | null = null;

const VIETNAM_LANDMARKS = [
  { keywords: ["keangnam", "landmark 72", "phạm hùng"], name: "Keangnam Landmark 72, Phạm Hùng, Mễ Trì, Nam Từ Liêm, Hà Nội", lat: 21.0173, lng: 105.7838 },
  { keywords: ["royal city", "vincom royal city", "72 nguyễn trãi"], name: "Vinhomes Royal City, 72A Nguyễn Trãi, Thượng Đình, Thanh Xuân, Hà Nội", lat: 21.0028, lng: 105.8158 },
  { keywords: ["times city", "vincom times city", "458 minh khai"], name: "Vinhomes Times City, 458 Minh Khai, Vĩnh Tuy, Hai Bà Trưng, Hà Nội", lat: 20.9950, lng: 105.8680 },
  { keywords: ["smart city", "vincom smart city", "tây mỗ"], name: "Vinhomes Smart City, Tây Mỗ, Nam Từ Liêm, Hà Nội", lat: 21.0016, lng: 105.7483 },
  { keywords: ["aeon mall hà đông", "aeon ha dong", "dương nội"], name: "AEON Mall Hà Đông, Khu đô thị Dương Nội, Hà Đông, Hà Nội", lat: 20.9788, lng: 105.7535 },
  { keywords: ["svđ mỹ đình", "sân vận động mỹ đình", "lê đức thọ"], name: "Sân vận động Quốc gia Mỹ Đình, Lê Đức Thọ, Mỹ Đình 1, Nam Từ Liêm, Hà Nội", lat: 21.0205, lng: 105.7640 },
  { keywords: ["hồ gươm", "hồ hoàn kiếm", "đinh tiên hoàng", "tràng tiền"], name: "Hồ Hoàn Kiếm, Hàng Trống, Hoàn Kiếm, Hà Nội", lat: 21.0285, lng: 105.8542 },
  { keywords: ["cầu giấy", "dịch vọng", "xuân thủy"], name: "Số 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội", lat: 21.0333, lng: 105.7983 },
  { keywords: ["nam từ liêm", "mỹ đình"], name: "Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội", lat: 21.0125, lng: 105.7725 },
  { keywords: ["ba đình", "kim mã", "liễu giai"], name: "Kim Mã, Ba Đình, Hà Nội", lat: 21.0345, lng: 105.8235 },
  { keywords: ["đống đa", "chùa láng", "láng hạ"], name: "Chùa Láng, Láng Thượng, Đống Đa, Hà Nội", lat: 21.0180, lng: 105.8270 },
  { keywords: ["thanh xuân", "nguyễn trãi"], name: "Nguyễn Trãi, Thanh Xuân Bắc, Thanh Xuân, Hà Nội", lat: 20.9930, lng: 105.8050 },
  { keywords: ["hà đông", "quang trung", "trần phú"], name: "Quang Trung, Hà Cầu, Hà Đông, Hà Nội", lat: 20.9700, lng: 105.7750 },
  { keywords: ["tây hồ", "lạc long quân", "quảng an"], name: "Lạc Long Quân, Bưởi, Tây Hồ, Hà Nội", lat: 21.0600, lng: 105.8200 },
  { keywords: ["mai dịch", "hồ tùng mậu"], name: "Số 36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội", lat: 21.0366, lng: 105.7788 }
];

const removeVietnameseTones = (str: string): string => {
  return str
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .trim();
};

const sanitizeAddress = (raw: string): string => {
  if (!raw) return "";
  let cleaned = raw
    .replace(/,?\s*(Việt Nam|Vietnam|VNM)$/gi, "")
    .replace(/[()[\]{}<>@#$%^&*~_+=]/g, " ")
    .replace(/\s+/g, " ")
    .trim();
  cleaned = cleaned.replace(/^[,\-\/\s]+|[,\-\/\s]+$/g, "");
  return cleaned;
};

const findNearestLandmark = (lat: number, lng: number) => {
  let minDistance = Infinity;
  let nearest = VIETNAM_LANDMARKS[0];
  for (const item of VIETNAM_LANDMARKS) {
    const d = Math.hypot(item.lat - lat, item.lng - lng);
    if (d < minDistance) {
      minDistance = d;
      nearest = item;
    }
  }
  return { landmark: nearest, distanceKm: minDistance * 111 };
};

const createPinIcon = () => {
  return L.divIcon({
    className: "custom-store-pin",
    html: `
      <div class="store-pin-wrapper">
        <div class="pin-pulse"></div>
        <div class="pin-marker">
          <i class="bi bi-shop"></i>
        </div>
        <div class="pin-shadow"></div>
      </div>
    `,
    iconSize: [42, 50],
    iconAnchor: [21, 48],
    popupAnchor: [0, -44]
  });
};

const resolveAddressFromCoords = async (lat: number, lng: number) => {
  isGeocodingAddress.value = true;
  const { landmark, distanceKm } = findNearestLandmark(lat, lng);

  // 1. ArcGIS Reverse Geocode
  try {
    const esriUrl = `https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode?f=pjson&location=${lng},${lat}`;
    const res = await fetch(esriUrl, { signal: AbortSignal.timeout(3500) });
    if (res.ok) {
      const data = await res.json();
      if (data && data.address) {
        const match = data.address.Match_addr || data.address.LongLabel || data.address.Address;
        if (match) {
          tempLocationAddress.value = sanitizeAddress(match);
          isGeocodingAddress.value = false;
          return;
        }
      }
    }
  } catch (e) {}

  // 2. BigDataCloud Reverse Geocode
  try {
    const bdcUrl = `https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lng}&localityLanguage=vi`;
    const bdcRes = await fetch(bdcUrl, { signal: AbortSignal.timeout(3000) });
    if (bdcRes.ok) {
      const bdcData = await bdcRes.json();
      if (bdcData) {
        const parts = [bdcData.locality || "", bdcData.city || "", bdcData.principalSubdivision || "Hà Nội"].filter(Boolean);
        if (parts.length > 0) {
          const resolved = distanceKm < 0.8
            ? landmark.name
            : `${parts.join(", ")}`;
          tempLocationAddress.value = sanitizeAddress(resolved);
          isGeocodingAddress.value = false;
          return;
        }
      }
    }
  } catch (e) {}

  // Fallback Landmark
  tempLocationAddress.value = sanitizeAddress(landmark.name);
  isGeocodingAddress.value = false;
};

const locateUserCurrentPosition = () => {
  if (typeof window === "undefined" || !navigator.geolocation) {
    if (errorMessage) errorMessage.value = "Trình duyệt không hỗ trợ Geolocation GPS!";
    return;
  }

  isLocatingGps.value = true;
  navigator.geolocation.getCurrentPosition(
    (pos) => {
      isLocatingGps.value = false;
      const { latitude, longitude } = pos.coords;
      tempLat.value = latitude;
      tempLng.value = longitude;

      if (mapInstance) {
        mapInstance.flyTo([latitude, longitude], 17, { duration: 1.2 });
      }
      if (markerInstance) {
        markerInstance.setLatLng([latitude, longitude]);
      }
      resolveAddressFromCoords(latitude, longitude);
    },
    (err) => {
      isLocatingGps.value = false;
      console.warn("Lỗi GPS thời gian thực:", err);
      if (!tempLat.value || !tempLng.value) {
        tempLat.value = 21.0333;
        tempLng.value = 105.7983;
      }
      resolveAddressFromCoords(tempLat.value, tempLng.value);
    },
    { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
  );
};

const searchAddressOnMap = async () => {
  const q = mapSearchQuery.value.trim();
  if (!q) return;
  isSearchingLocation.value = true;

  // 1. Tìm trong landmark trước
  const cleanQ = removeVietnameseTones(q);
  const matched = VIETNAM_LANDMARKS.find((item) => {
    const itemClean = removeVietnameseTones(item.name);
    return itemClean.includes(cleanQ) || item.keywords.some((k) => cleanQ.includes(removeVietnameseTones(k)));
  });

  if (matched) {
    tempLat.value = matched.lat;
    tempLng.value = matched.lng;
    tempLocationAddress.value = sanitizeAddress(matched.name);
    if (mapInstance) mapInstance.flyTo([matched.lat, matched.lng], 17);
    if (markerInstance) markerInstance.setLatLng([matched.lat, matched.lng]);
    isSearchingLocation.value = false;
    return;
  }

  // 2. Tra cứu qua Nominatim OpenStreetMap
  try {
    const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(q + ", Việt Nam")}&limit=1&accept-language=vi`;
    const res = await fetch(url, { signal: AbortSignal.timeout(3500) });
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        const lat = parseFloat(data[0].lat);
        const lng = parseFloat(data[0].lon);
        tempLat.value = lat;
        tempLng.value = lng;
        tempLocationAddress.value = sanitizeAddress(data[0].display_name);
        if (mapInstance) mapInstance.flyTo([lat, lng], 17);
        if (markerInstance) markerInstance.setLatLng([lat, lng]);
        isSearchingLocation.value = false;
        return;
      }
    }
  } catch (e) {}

  isSearchingLocation.value = false;
};

const initOrUpdateMap = () => {
  nextTick(() => {
    setTimeout(() => {
      const container = document.getElementById("sellerStoreMap");
      if (!container) return;

      const targetLat = tempLat.value || 21.0333;
      const targetLng = tempLng.value || 105.7983;

      if (!mapInstance) {
        mapInstance = L.map("sellerStoreMap", {
          center: [targetLat, targetLng],
          zoom: 16,
          zoomControl: true
        });

        L.tileLayer("https://mt{s}.google.com/vt/lyrs=m&hl=vi&x={x}&y={y}&z={z}", {
          subdomains: ["0", "1", "2", "3"],
          maxZoom: 20
        }).addTo(mapInstance);

        markerInstance = L.marker([targetLat, targetLng], {
          icon: createPinIcon(),
          draggable: true
        }).addTo(mapInstance);

        markerInstance.bindTooltip("🏪 Kéo ghim đến đúng vị trí cửa hàng của bạn", {
          permanent: false,
          direction: "top"
        });

        markerInstance.on("dragend", (e: any) => {
          const latLng = e.target.getLatLng();
          tempLat.value = latLng.lat;
          tempLng.value = latLng.lng;
          resolveAddressFromCoords(latLng.lat, latLng.lng);
        });

        mapInstance.on("click", (e: L.LeafletMouseEvent) => {
          const { lat, lng } = e.latlng;
          tempLat.value = lat;
          tempLng.value = lng;
          if (markerInstance) {
            markerInstance.setLatLng([lat, lng]);
          }
          resolveAddressFromCoords(lat, lng);
        });
      } else {
        mapInstance.invalidateSize();
        mapInstance.setView([targetLat, targetLng], 16);
        if (markerInstance) {
          markerInstance.setLatLng([targetLat, targetLng]);
        }
      }
    }, 150);
  });
};

const openLocationModal = () => {
  isLocationModalOpen.value = true;
  mapSearchQuery.value = "";

  if (form.lat && form.lng) {
    tempLat.value = form.lat;
    tempLng.value = form.lng;
    tempLocationAddress.value = form.address;
    initOrUpdateMap();
  } else {
    tempLat.value = 21.0333;
    tempLng.value = 105.7983;
    if (form.address) {
      tempLocationAddress.value = form.address;
    }
    initOrUpdateMap();
    // Tự động định vị GPS thời gian thực khi mở lần đầu
    locateUserCurrentPosition();
  }
};

const closeLocationModal = () => {
  isLocationModalOpen.value = false;
};

const saveLocationFromMap = () => {
  if (!tempLat.value || !tempLng.value) {
    errorMessage.value = "Vui lòng chọn tọa độ vị trí!";
    return;
  }
  form.lat = tempLat.value;
  form.lng = tempLng.value;
  if (tempLocationAddress.value && tempLocationAddress.value.trim()) {
    form.address = sanitizeAddress(tempLocationAddress.value.trim());
  }
  isLocationModalOpen.value = false;
  showLocationSuccessToast.value = true;
  setTimeout(() => {
    showLocationSuccessToast.value = false;
  }, 4000);
};

// Trạng thái Modal Xem & Thay đổi ảnh (2 bảng nhỏ + Blur Background)
const previewModal = reactive<{
  isOpen: boolean;
  title: string;
  imageSrc: string;
  fieldKey: 'cccdFrontImage' | 'cccdBackImage' | 'foodSafetyCertImage' | null;
  inputId: string;
}>({
  isOpen: false,
  title: "",
  imageSrc: "",
  fieldKey: null,
  inputId: ""
});

// Helper kiểm tra file upload chỉ nhận ảnh (chặn video .mp4, ...)
const handleImageFileUpload = (e: Event, callback: (base64Url: string) => void) => {
  errorMessage.value = "";
  const inputEl = e.target as HTMLInputElement;
  const file = inputEl.files?.[0];
  if (!file) return;

  const fileName = file.name.toLowerCase();
  const fileType = file.type.toLowerCase();

  const isVideo = fileType.startsWith("video/") || /\.(mp4|mov|avi|mkv|webm|flv|wmv|3gp)$/i.test(fileName);
  const isImage = fileType.startsWith("image/") || /\.(jpg|jpeg|png|webp|jfif|bmp|gif)$/i.test(fileName);

  if (isVideo || !isImage) {
    errorMessage.value = "Tệp tải lên không hợp lệ! Vui lòng chọn tệp hình ảnh (VD: .jpg, .png), không chấp nhận tệp video (.mp4,...).";
    inputEl.value = "";
    return;
  }

  const reader = new FileReader();
  reader.onload = (evt) => {
    if (evt.target?.result) {
      callback(evt.target.result as string);
    }
  };
  reader.readAsDataURL(file);
};

// Handler Upload các loại ảnh
const handleCccdFrontUpload = (e: Event) => {
  const target = e.target as HTMLInputElement;
  const file = target?.files?.[0];
  handleImageFileUpload(e, async (url) => {
    form.cccdFrontImage = url;
    if (previewModal.isOpen && previewModal.fieldKey === 'cccdFrontImage') {
      previewModal.imageSrc = url;
    }

    // Tự động trích xuất Họ tên chủ tiệm & CCCD từ ảnh CCCD Mặt trước bằng OCR API
    if (file) {
      try {
        const formData = new FormData();
        formData.append("file", file);
        const scanRes = await fetch("/api/ocr/scan-id-card", {
          method: "POST",
          body: formData
        });
        const scanData = await scanRes.json();
        if (scanRes.ok && scanData.success && scanData.data) {
          const extractedName = (scanData.data.ho_va_ten || scanData.data.name || "").trim();
          if (extractedName) {
            form.ownerFullName = extractedName;
          }
          const idNum = (scanData.data.so_cmnd_cccd || scanData.data.id || "").trim();
          if (idNum) {
            form.cccdNumber = idNum;
          }
          const homeTown = (scanData.data.que_quan || "").trim();
          if (homeTown) {
            form.nguyenQuan = homeTown;
          }
          return;
        }
      } catch (ocrErr) {
        console.warn("⚠️ Quét CCCD qua /api/ocr/scan-id-card cảnh báo:", ocrErr);
      }
    }

    // Fallback tự động gửi ảnh lên backend để phân tích nhận diện số CCCD và Nguyên Quán
    try {
      const res = await fetch("/api/auth/ocr-cccd", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ image: url })
      });
      if (res.ok) {
        const data = await res.json();
        if (data && data.success) {
          if (data.cccdNumber && typeof data.cccdNumber === "string" && data.cccdNumber.trim()) {
            form.cccdNumber = data.cccdNumber.trim();
          }
          if (data.nguyenQuan && typeof data.nguyenQuan === "string" && data.nguyenQuan.trim()) {
            form.nguyenQuan = data.nguyenQuan.trim();
          }
        }
      }
    } catch (ocrErr) {
      console.warn("⚠️ OCR CCCD Background Warning:", ocrErr);
    }
  });
};

const handleCccdBackUpload = (e: Event) => {
  handleImageFileUpload(e, (url) => {
    form.cccdBackImage = url;
    if (previewModal.isOpen && previewModal.fieldKey === 'cccdBackImage') {
      previewModal.imageSrc = url;
    }
  });
};

const handleFoodSafetyCertUpload = (e: Event) => {
  handleImageFileUpload(e, (url) => {
    form.foodSafetyCertImage = url;
    if (previewModal.isOpen && previewModal.fieldKey === 'foodSafetyCertImage') {
      previewModal.imageSrc = url;
    }
  });
};

// Mở Modal Preview khi bấm thêm 1 lần nữa vào ảnh đã tải
const handleUploadBoxClick = (
  fieldKey: 'cccdFrontImage' | 'cccdBackImage' | 'foodSafetyCertImage',
  inputId: string,
  title: string
) => {
  if (form[fieldKey]) {
    previewModal.fieldKey = fieldKey;
    previewModal.inputId = inputId;
    previewModal.title = title;
    previewModal.imageSrc = form[fieldKey];
    previewModal.isOpen = true;
  } else {
    const inputEl = document.getElementById(inputId) as HTMLInputElement;
    if (inputEl) inputEl.click();
  }
};

const closePreviewModal = () => {
  previewModal.isOpen = false;
  previewModal.fieldKey = null;
  previewModal.imageSrc = "";
  previewModal.inputId = "";
};

const triggerChangeImageFromModal = () => {
  if (previewModal.inputId) {
    const inputEl = document.getElementById(previewModal.inputId) as HTMLInputElement;
    if (inputEl) inputEl.click();
  }
};

// Kiểm tra Validate & Chuyển Bước
const goToNextStep = () => {
  errorMessage.value = "";

  if (currentStep.value === 1) {
    // 1. Validate Tên cửa hàng (tối thiểu 12 ký tự, tối đa 30 ký tự, không ký tự đặc biệt)
    const storeNameTrimmed = form.storeName.trim();
    if (!storeNameTrimmed) {
      errorMessage.value = "Vui lòng nhập Tên cửa hàng!";
      return;
    }
    if (storeNameTrimmed.length < 12 || storeNameTrimmed.length > 30) {
      errorMessage.value = "Tên cửa hàng phải có độ dài tối thiểu từ 12 đến tối đa 30 ký tự!";
      return;
    }

    const specialCharRegex = /[^\a-zA-Z0-9 a-zA-ZàáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆĐÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ]/;
    if (specialCharRegex.test(storeNameTrimmed)) {
      errorMessage.value = "Tên cửa hàng chỉ được nhập chữ và số, không được nhập các ký tự đặc biệt!";
      return;
    }

    // 2. Validate Địa chỉ tiệm (Chuẩn dạng VD: Số 225 đường Nguyễn Trãi, Thành phố Hà Nội - Không ký tự đặc biệt)
    const addressTrimmed = form.address.trim();
    if (!addressTrimmed) {
      errorMessage.value = "Vui lòng nhập Địa chỉ tiệm!";
      return;
    }

    const addressSpecialCharRegex = /[^\a-zA-Z0-9 ,\-\/\.a-zA-ZàáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆĐÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ]/;
    if (addressSpecialCharRegex.test(addressTrimmed)) {
      errorMessage.value = "Địa chỉ tiệm không được chứa ký tự đặc biệt!";
      return;
    }
    if (addressTrimmed.length < 6) {
      errorMessage.value = "Địa chỉ tiệm phải nhập rõ ràng (Ví dụ: Số 225 đường Nguyễn Trãi, Thành phố Hà Nội)!";
      return;
    }

    if (!form.lat || !form.lng) {
      form.lat = 21.0333;
      form.lng = 105.7983;
    }

    currentStep.value = 2;

  } else if (currentStep.value === 2) {
    // Tự động gán Họ tên chủ tiệm nếu chưa có (lấy từ tên tiệm để không chặn người dùng và đảm bảo backend hợp lệ)
    if (!form.ownerFullName || !form.ownerFullName.trim()) {
      form.ownerFullName = `Chủ tiệm ${form.storeName.trim()}`;
    }

    // 4. Validate Số CCCD nếu người dùng có nhập (nếu nhập thì phải đúng 9 hoặc 12 chữ số CMND/CCCD, nếu chưa có thì để trống)
    const cccdTrimmed = form.cccdNumber ? form.cccdNumber.trim() : "";
    if (cccdTrimmed && !/^(\d{9}|\d{12})$/.test(cccdTrimmed)) {
      errorMessage.value = "Số CCCD/CMND phải bao gồm 9 hoặc 12 chữ số (hoặc để trống nếu chưa có)!";
      return;
    }

    // 5. Validate BẮT BUỘC phải gửi đủ cả 3 ảnh (CCCD mặt trước, mặt sau và Giấy chứng nhận An toàn thực phẩm)
    if (!form.cccdFrontImage || !form.cccdBackImage || !form.foodSafetyCertImage) {
      errorMessage.value = "Bắt buộc phải tải đủ Ảnh CCCD (Mặt trước & Mặt sau) và Ảnh Giấy chứng nhận An toàn thực phẩm mới qua được bước tiếp theo!";
      return;
    }

    currentStep.value = 3;
  }
};

const handleCccdNumberInput = (e: Event) => {
  const target = e.target as HTMLInputElement;
  if (target) {
    form.cccdNumber = target.value.replace(/\D/g, '').slice(0, 12);
  }
};

// Quay lại bước trước
const goToPrevStep = () => {
  errorMessage.value = "";
  if (currentStep.value > 1) {
    currentStep.value = (currentStep.value - 1) as 1 | 2 | 3;
  }
};

// Gửi Hồ Sơ Đăng Ký Người Bán
const handleSubmitApplication = async () => {
  errorMessage.value = "";

  // Validate Số tài khoản nhận tiền (chỉ chứa chữ số, 8 đến 15 số)
  const accountNumberTrimmed = form.bankAccountNumber.trim();
  if (!accountNumberTrimmed) {
    errorMessage.value = "Vui lòng nhập Số tài khoản nhận tiền!";
    return;
  }

  const bankAccountRegex = /^\d{8,15}$/;
  if (!bankAccountRegex.test(accountNumberTrimmed)) {
    errorMessage.value = "Số tài khoản nhận tiền phải là các chữ số từ tối thiểu 8 số đến tối đa 15 số (không chứa chữ hoặc ký tự đặc biệt)!";
    return;
  }

  const phoneEmailTrimmed = form.phoneEmail.trim();
  if (!phoneEmailTrimmed) {
    errorMessage.value = "Vui lòng nhập Số điện thoại hoặc Email đăng nhập!";
    return;
  }

  isLoading.value = true;

  try {
    const res = await fetch("/api/auth/register-seller", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        storeName: form.storeName.trim(),
        sellerType: form.sellerType,
        ward: form.ward.trim(),
        wardId: form.wardId,
        district: form.district.trim(),
        marketName: form.marketName.trim(),
        marketId: form.marketId,
        stallNumber: form.stallNumber ? form.stallNumber.trim() : "",
        category: form.category,
        address: form.address.trim(),
        latitude: form.lat || 21.0333,
        longitude: form.lng || 105.7983,
        openHours: form.openHours.trim(),
        phoneEmail: form.phoneEmail.trim(),
        ownerFullName: form.ownerFullName.trim() || `Chủ tiệm ${form.storeName.trim()}`,
        cccdNumber: form.cccdNumber ? form.cccdNumber.trim() : "",
        nguyenQuan: form.nguyenQuan ? form.nguyenQuan.trim() : "",
        cccdFrontImage: form.cccdFrontImage,
        cccdBackImage: form.cccdBackImage,
        foodSafetyCertImage: form.foodSafetyCertImage,
        bankName: form.bankName,
        bankAccountNumber: accountNumberTrimmed,
        password: form.password.trim() || "123456"
      })
    }).catch(() => null);

    if (!res) {
      throw new Error("Không thể kết nối đến máy chủ. Vui lòng kiểm tra lại kết nối mạng!");
    }

    const data = await res.json().catch(() => ({}));
    if (!res.ok || !data.success) {
      const errTxt = data.message || `Đăng ký không thành công (Mã lỗi: ${res.status}). Vui lòng kiểm tra lại thông tin!`;
      errorMessage.value = errTxt;
      isLoading.value = false;
      return;
    }

    console.log("✅ API Register Seller Success:", data);

    try {
      const cleanAccountKey = phoneEmailTrimmed.toLowerCase();
      const storeObj = {
        id: `ZM-S${Math.floor(1000 + Math.random() * 9000)}`,
        name: form.storeName.trim(),
        category: form.category,
        address: form.address.trim(),
        lat: form.lat || 21.0333,
        lng: form.lng || 105.7983,
        openHours: form.openHours.trim(),
        phone: phoneEmailTrimmed,
        cccdNumber: form.cccdNumber ? form.cccdNumber.trim() : "",
        nguyenQuan: form.nguyenQuan ? form.nguyenQuan.trim() : "",
        bankName: form.bankName,
        bankAccount: accountNumberTrimmed,
        accountHolder: form.ownerFullName.trim().toUpperCase()
      };
      localStorage.setItem(`zonemart_seller_store_${cleanAccountKey}`, JSON.stringify(storeObj));

      // Lưu vào cache tổng để luồng giao hàng và checkout tự động nhận diện GPS dẫn đường
      const allStoresCache = JSON.parse(localStorage.getItem("zonemart_all_registered_stores") || "{}");
      allStoresCache[form.storeName.trim().toLowerCase()] = storeObj;
      allStoresCache[cleanAccountKey] = storeObj;
      localStorage.setItem("zonemart_all_registered_stores", JSON.stringify(allStoresCache));
    } catch {}

    showSuccessModal.value = true;

  } catch (err: any) {
    errorMessage.value = err.message || "Có lỗi xảy ra khi nộp hồ sơ.";
  } finally {
    isLoading.value = false;
  }
};

const closeSuccessModal = () => {
  showSuccessModal.value = false;
  router.push("/seller-login");
};

// ================= MASCOT CHUẨN NILBUILD/PAGE-MASCOT (CHỈ DI CHUYỂN ĐẦU THEO CHUỘT) =================
const DIRECTIONS = [
  'up-left',
  'up',
  'up-right',
  'left',
  'center',
  'right',
  'down-left',
  'down',
  'down-right',
] as const;

const REACTIONS = [
  'blink',
  'heart',
  'sparkle',
  'surprised',
  'wink',
  'bashful',
  'sleepy',
  'dizzy',
  'delighted',
] as const;

type Direction = (typeof DIRECTIONS)[number];
type Reaction = (typeof REACTIONS)[number];

const CLOCKWISE: Direction[] = [
  'right',
  'down-right',
  'down',
  'down-left',
  'left',
  'up-left',
  'up',
  'up-right',
];

const SECTOR = (Math.PI * 2) / CLOCKWISE.length;
const HYSTERESIS = 0.12;
const DEAD_ZONE = 60;

const PAYOFFS: Reaction[] = ['heart', 'sparkle', 'delighted', 'wink', 'surprised'];
const BOOP_PAYOFF = 120;
const BOOP_END = 600;
const DIZZY_AFTER = 4;
const DIZZY_WINDOW = 1600;
const DIZZY_END = 1100;

// Mascot character: 'chef' (tiệm ẩm thực / người buôn bán)
const activeMascot = ref<'chef' | 'cap' | 'drone' | 'otter'>('chef');
const mascotDirectionsUrl = computed(() => `/mascots/${activeMascot.value}-directions.webp`);
const mascotReactionsUrl = computed(() => `/mascots/${activeMascot.value}-reactions.webp`);

const mascotButtonRef = ref<HTMLElement | null>(null);
const currentDirection = ref<Direction>('center');
const currentReaction = ref<Reaction | null>(null);
const isSquashing = ref(false);

const mascotTipsByStep: Record<number, string[]> = {
  1: [
    "Chào bạn! Hãy đặt tên tiệm thật độc đáo nhé! 🛒✨",
    "Địa chỉ chi tiết giúp shipper tìm shop nhận hàng cực nhanh! 📍⚡",
    "ZoneMart đồng hành cùng bạn tiếp cận hàng triệu khách! 🧡"
  ],
  2: [
    "Chủ tiệm chụp CCCD 2 mặt thật rõ nét nha! 🪪📸",
    "Đừng quên ảnh giấy chứng nhận an toàn thực phẩm nhé! 📑🥗",
    "Hồ sơ của bạn được ZoneMart mã hóa bảo mật 100%! 🔒"
  ],
  3: [
    "Nhập số tài khoản chính xác để nhận tiền doanh thu tự động! 💳💰",
    "Chỉ còn bước cuối này thôi là tiệm sẽ được duyệt mở ngay! 🎉🏪",
    "ZoneMart chúc shop khai trương buôn may bán đắt, ngập đơn! 🚀🔥"
  ]
};

const currentTipIndex = ref(0);
const customMascotMessage = ref<string | null>(null);
const isBubblePopping = ref(false);
const isMascotAlert = ref(false);

const currentMascotMsg = computed(() => {
  if (customMascotMessage.value) return customMascotMessage.value;
  const list = mascotTipsByStep[currentStep.value] || mascotTipsByStep[1];
  return list[currentTipIndex.value % list.length];
});

// Tính toán vị trí góc nhìn đầu trong sprite sheet 3x3
const directionStyle = computed(() => {
  const index = DIRECTIONS.indexOf(currentDirection.value);
  const posX = (index % 3) * 50;
  const posY = Math.floor(index / 3) * 50;
  return {
    backgroundPosition: `${posX}% ${posY}%`,
    opacity: currentReaction.value ? 0 : 1
  };
});

// Biểu cảm khi được click (poke / boop)
const reactionStyle = computed(() => {
  const index = REACTIONS.indexOf(currentReaction.value ?? 'blink');
  const posX = (index % 3) * 50;
  const posY = Math.floor(index / 3) * 50;
  return {
    backgroundPosition: `${posX}% ${posY}%`,
    opacity: currentReaction.value ? 1 : 0
  };
});

let boopCount = 0;
let lastBoopAt = 0;
let reactionTimer: any = null;

const handleMascotBoop = () => {
  if (reactionTimer) clearTimeout(reactionTimer);

  const now = Date.now();
  boopCount = now - lastBoopAt < DIZZY_WINDOW ? boopCount + 1 : 1;
  lastBoopAt = now;

  // Hiệu ứng nhún nảy (squash physics)
  isSquashing.value = true;
  setTimeout(() => {
    isSquashing.value = false;
  }, 420);

  if (boopCount >= DIZZY_AFTER) {
    boopCount = 0;
    currentReaction.value = 'dizzy';
    reactionTimer = setTimeout(() => {
      currentReaction.value = null;
    }, DIZZY_END);
  } else {
    currentReaction.value = 'blink';
    reactionTimer = setTimeout(() => {
      currentReaction.value = PAYOFFS[(boopCount - 1) % PAYOFFS.length];
      reactionTimer = setTimeout(() => {
        currentReaction.value = null;
      }, BOOP_END);
    }, BOOP_PAYOFF);
  }

  isBubblePopping.value = true;
  currentTipIndex.value = currentTipIndex.value + 1;
  setTimeout(() => {
    isBubblePopping.value = false;
  }, 250);
};

// Tự động kích hoạt Mascot hiển thị cảnh báo khi người dùng nhập thiếu hoặc sai
watch(errorMessage, (newVal) => {
  if (newVal) {
    customMascotMessage.value = `⚠️ ${newVal}`;
    isMascotAlert.value = true;
    isBubblePopping.value = true;
    currentReaction.value = 'surprised'; // Đầu mascot ngạc nhiên chú ý
    isSquashing.value = true;
    setTimeout(() => {
      isSquashing.value = false;
      isBubblePopping.value = false;
    }, 380);
    setTimeout(() => {
      if (currentReaction.value === 'surprised') {
        currentReaction.value = null;
      }
    }, 3500);
  } else {
    isMascotAlert.value = false;
    customMascotMessage.value = null;
  }
});

// Khi người dùng chỉnh sửa dữ liệu, tự động xóa cảnh báo cũ
watch(
  [
    () => form.storeName,
    () => form.address,
    () => form.ownerFullName,
    () => form.cccdFrontImage,
    () => form.cccdBackImage,
    () => form.foodSafetyCertImage,
    () => form.bankAccountNumber
  ],
  () => {
    if (isMascotAlert.value) {
      errorMessage.value = "";
    }
  }
);

// Cập nhật câu nói chào mừng khi đổi bước form
watch(currentStep, () => {
  currentTipIndex.value = 0;
  customMascotMessage.value = null;
  isMascotAlert.value = false;
  isBubblePopping.value = true;
  setTimeout(() => {
    isBubblePopping.value = false;
  }, 250);
});

let lastSector = -1;
function wrap(angle: number) {
  return Math.atan2(Math.sin(angle), Math.cos(angle));
}

// Hàm xoay hướng đầu theo vị trí con trỏ chuột chuẩn nilbuild/page-mascot
function handlePointerMove(e: PointerEvent | MouseEvent) {
  const button = mascotButtonRef.value;
  if (!button) return;

  const box = button.getBoundingClientRect();
  const dx = e.clientX - (box.left + box.width / 2);
  const dy = e.clientY - (box.top + box.height / 2);

  if (Math.hypot(dx, dy) < DEAD_ZONE) {
    lastSector = -1;
    currentDirection.value = 'center';
    return;
  }

  const angle = Math.atan2(dy, dx);
  if (lastSector !== -1 && Math.abs(wrap(angle - lastSector * SECTOR)) < SECTOR / 2 + HYSTERESIS) {
    return;
  }

  lastSector = (Math.round(angle / SECTOR) + CLOCKWISE.length) % CLOCKWISE.length;
  currentDirection.value = CLOCKWISE[lastSector];
}

onMounted(() => {
  document.title = "Đăng Ký Người Bán & Mở Gian Hàng | ZoneMart - Sàn TMĐT & Giao Hàng Hỏa Tốc";
  window.addEventListener("pointermove", handlePointerMove, { passive: true });
});

onUnmounted(() => {
  window.removeEventListener("pointermove", handlePointerMove);
});
</script>

<template>
  <div class="login-page-container">
    <!-- KHUNG TỔNG THỂ 2 CỘT: ẢNH ĐĂNG KÝ BÁN HÀNG BÊN TRÁI + BẢNG ĐĂNG KÝ BÊN PHẢI -->
    <div class="login-hero-layout">
      
      <!-- CỘT TRÁI: ẢNH MINH HỌA ĐĂNG KÝ BÁN HÀNG TÁCH SẠCH NỀN -->
      <div class="login-illustration-side">
        <img
          src="/images/anhxoanendkybanhang_clean.png"
          alt="ZoneMart Đăng Ký Bán Hàng"
          class="illustration-img"
        />
      </div>

      <!-- CỘT PHẢI: BẢNG ĐĂNG KÝ NGƯỜI BÁN TRÔI NỔI (FLOATING CARD) 3 BƯỚC -->
      <div class="login-card-floating">
        
        <!-- MASCOT PEELING / PEEKING OVER FORM (THEO CHUẨN NILBUILD/PAGE-MASCOT) -->
        <div 
          class="seller-mascot-peek-container" 
          title="Nhấp vào em để xem phản ứng & nhận mẹo bán hàng!"
        >
          <!-- Bong bóng thoại tương tác linh hoạt: đổi giao diện cảnh báo nổi bật khi thiếu thông tin -->
          <div class="mascot-speech-bubble" :class="{ 'bubble-pop': isBubblePopping, 'is-alert-bubble': isMascotAlert }">
            <span class="bubble-text">{{ currentMascotMsg }}</span>
            <span class="bubble-arrow"></span>
          </div>

          <!-- Component Mascot chuyển hướng đầu 9 hướng chuẩn sprite sheet nilbuild/page-mascot -->
          <div 
            ref="mascotButtonRef" 
            class="mascot-sprite-button" 
            @click="handleMascotBoop"
          >
            <div class="mascot-sprite-stage" :class="{ 'is-squashing': isSquashing }">
              <span 
                class="mascot-sprite-layer" 
                :style="{ backgroundImage: `url(${mascotDirectionsUrl})`, ...directionStyle }"
              ></span>
              <span 
                class="mascot-sprite-layer" 
                :style="{ backgroundImage: `url(${mascotReactionsUrl})`, ...reactionStyle }"
              ></span>
            </div>
            <div class="mascot-counter-edge"></div>
          </div>
        </div>

        <!-- THANH ĐIỀU HƯỚNG QUAY LẠI -->
        <div class="card-top-bar">
          <button type="button" class="back-home-btn" @click="router.push('/seller-login')" title="Quay lại trang Đăng Nhập">
            ← Quay lại Đăng nhập
          </button>
        </div>

        <!-- TIÊU ĐỀ ĐĂNG KÝ NGƯỜI BÁN -->
        <div class="card-brand-header">
          <h1 class="form-main-heading">ĐĂNG KÝ NGƯỜI BÁN</h1>
          <p class="form-sub-heading">Tạo gian hàng kinh doanh & tiếp cận khách hàng ZoneMart</p>
        </div>

        <!-- BẢNG THEO DÕI BƯỚC (STEPPER BAR 3 BƯỚC CHUYÊN NGHIỆP) -->
        <div class="stepper-bar">
          <div class="step-item" :class="{ active: currentStep >= 1, current: currentStep === 1 }">
            <div class="step-badge">
              <i v-if="currentStep > 1" class="bi bi-check-lg"></i>
              <span v-else>1</span>
            </div>
            <span class="step-label">Thông tin tiệm</span>
          </div>
          <div class="step-connector" :class="{ active: currentStep >= 2 }"></div>
          <div class="step-item" :class="{ active: currentStep >= 2, current: currentStep === 2 }">
            <div class="step-badge">
              <i v-if="currentStep > 2" class="bi bi-check-lg"></i>
              <span v-else>2</span>
            </div>
            <span class="step-label">Xác thực chủ tiệm</span>
          </div>
          <div class="step-connector" :class="{ active: currentStep >= 3 }"></div>
          <div class="step-item" :class="{ active: currentStep >= 3, current: currentStep === 3 }">
            <div class="step-badge">
              <span>3</span>
            </div>
            <span class="step-label">Thanh toán</span>
          </div>
        </div>

        <!-- THÔNG BÁO LỖI NẾU CÓ -->
        <div v-if="errorMessage" class="msg-box error">⚠️ {{ errorMessage }}</div>

        <!-- ==================== BƯỚC 1: THÔNG TIN TIỆM ==================== -->
        <form v-if="currentStep === 1" @submit.prevent="goToNextStep" class="form-body">
          
          <!-- Ô THÔNG BÁO TÀI KHOẢN KHÁCH HÀNG LIÊN KẾT (NẰM TẠI ĐẦU BƯỚC 1 - TRÊN CỘT TÊN CỬA HÀNG) -->
          <div v-if="currentUser" class="customer-account-banner success-linked">
            <div class="banner-head-row">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              <span class="banner-head-title">Tài khoản Khách Hàng liên kết mở Gian Hàng:</span>
            </div>
            <div class="banner-user-detail">
              <span class="user-display-name">Anh/Chị <b>{{ currentUser.fullName || 'Khách Hàng' }}</b></span>
              <span class="user-display-email">(Gmail/SĐT: <b class="highlight-email">{{ currentUser.phoneEmail }}</b>)</span>
            </div>
          </div>
          <div v-else class="customer-account-banner warning-not-linked">
            <div class="banner-head-row">
              <span class="warn-icon">⚠️</span>
              <span class="banner-head-title">Chưa liên kết tài khoản Khách Hàng!</span>
            </div>
            <p class="banner-warn-desc">
              Theo quy định ZoneMart, bạn cần đăng nhập tài khoản Khách Hàng trước khi tạo hồ sơ mở Gian Hàng.
            </p>
            <button type="button" class="btn-banner-login" @click="goToLoginWithIntent('seller')">
              🔑 Đăng Nhập Tài Khoản Khách Hàng Ngay ➔
            </button>
          </div>

          <!-- MÔ HÌNH KINH DOANH ĐỊA PHƯƠNG (4 NHÓM THEO SƠ ĐỒ CHỢ ĐỊA PHƯƠNG SỐ) -->
          <div class="field-item">
            <label class="field-label">
              Mô hình kinh doanh địa phương <span class="req">*</span>
              <span class="hint-text">(Chọn đúng nhóm đối tượng của gian hàng)</span>
            </label>
            <div class="seller-type-grid">
              <label class="seller-type-card" :class="{ 'selected': form.sellerType === 'traditional_market' }" @click="form.sellerType = 'traditional_market'">
                <span class="type-icon">🏪</span>
                <span class="type-title">Chợ truyền thống</span>
                <span class="type-desc">Tiểu thương tại sạp chợ dân sinh</span>
              </label>

              <label class="seller-type-card" :class="{ 'selected': form.sellerType === 'home_business' }" @click="form.sellerType = 'home_business'">
                <span class="type-icon">🏡</span>
                <span class="type-title">Hộ kinh doanh tại nhà</span>
                <span class="type-desc">Chế biến thực phẩm, buôn bán tại gia</span>
              </label>

              <label class="seller-type-card" :class="{ 'selected': form.sellerType === 'store_facility' }" @click="form.sellerType = 'store_facility'">
                <span class="type-icon">🏭</span>
                <span class="type-title">Cửa hàng / Cơ sở SX</span>
                <span class="type-desc">Cửa hàng tạp hóa, xưởng chế biến</span>
              </label>

              <label class="seller-type-card" :class="{ 'selected': form.sellerType === 'other_local_seller' }" @click="form.sellerType = 'other_local_seller'">
                <span class="type-icon">👨‍🌾</span>
                <span class="type-title">Người bán địa phương khác</span>
                <span class="type-desc">Nông dân bán nông sản vườn nhà</span>
              </label>
            </div>
          </div>

          <!-- KHÔNG GIAN THƯƠNG MẠI SỐ CẤP QUẬN & XÃ / PHƯỜNG -->
          <!-- KHU VỰC ĐỊA CHỈ HÀNH CHÍNH (QUẬN/HUYỆN & XÃ/PHƯỜNG) -->
          <div class="field-row-two-cols">
            <div class="field-item">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
                <label class="field-label" style="margin-bottom: 0;">Quận / Huyện (Hà Nội) <span class="req">*</span></label>
                <button
                  type="button"
                  class="btn-gps-detect"
                  :disabled="isAutoLocating"
                  @click="autoDetectLocation"
                  title="Tự động nhận diện Quận/Huyện và Xã/Phường theo vị trí GPS thực tế"
                >
                  <span v-if="isAutoLocating">🔄 Đang định vị...</span>
                  <span v-else>📍 Định vị hiện tại</span>
                </button>
              </div>
              <select
                :value="selectedDistrictValue"
                class="field-input"
                @change="onDistrictChange"
                required
              >
                <optgroup label="⭐ Khu Vực Trọng Điểm Số Hóa">
                  <option value="Quận Cầu Giấy">🏙️ Quận Cầu Giấy (Trung Tâm Thí Điểm Chợ Số)</option>
                </optgroup>
                
                <optgroup label="🏙️ 12 Quận Nội Thành Hà Nội">
                  <option v-for="d in INNER_DISTRICTS" :key="d" :value="d">🏙️ {{ d }}</option>
                </optgroup>

                <optgroup label="🌾 18 Huyện & Thị Xã Ngoại Thành Hà Nội">
                  <option v-for="d in OUTER_DISTRICTS" :key="d" :value="d">🌾 {{ d }}</option>
                </optgroup>

                <optgroup label="🌐 Khu Vực Khác">
                  <option value="__custom_district__">➕ Tỉnh / Quận / Huyện khác (Tự do nhập)</option>
                </optgroup>
              </select>
              <input
                v-if="isCustomDistrict"
                v-model="form.district"
                type="text"
                class="field-input mt-2"
                placeholder="Nhập tên Quận/Huyện hoặc Tỉnh/Thành cụ thể..."
                required
              />
            </div>

            <div class="field-item">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
                <label class="field-label" style="margin-bottom: 0;">
                  Xã / Phường <span class="req">*</span>
                  <span v-if="wardsInSelectedDistrict.length > 0" style="font-size: 11px; color: #10b981; font-weight: 600; margin-left: 4px;">
                    ({{ wardsInSelectedDistrict.length }} đơn vị)
                  </span>
                </label>
              </div>
              <select
                class="field-input"
                :value="form.wardId"
                @change="onWardChange"
                required
              >
                <option v-for="w in wardsInSelectedDistrict" :key="w.id" :value="w.id">
                  🏛️ {{ w.wardName }}
                </option>
                <option value="__custom_ward__">➕ Phường / Xã khác (Tự nhập tên)</option>
              </select>
              <input
                v-if="form.wardId === '__custom_ward__' || wardsInSelectedDistrict.length === 0"
                v-model="form.ward"
                type="text"
                class="field-input mt-2"
                placeholder="Nhập tên Xã / Phường cụ thể..."
                required
              />
            </div>
          </div>

          <!-- CHỌN CHỢ ĐỊA PHƯƠNG & SỐ SẠP / VỊ TRÍ GIAN HÀNG -->
          <div class="field-row-two-cols">
            <div class="field-item">
              <label class="field-label">Chợ Truyền Thống / Điểm Kinh Doanh <span class="req">*</span></label>
              <select
                class="field-input"
                :value="form.marketId || '__custom__'"
                @change="onMarketChange"
                required
              >
                <option v-for="m in filteredMarkets" :key="m.id" :value="m.id">
                  🏪 {{ m.marketName }} ({{ m.totalStalls }} sạp)
                </option>
                <option value="__custom__">➕ Chợ truyền thống khác / Điểm ngoài chợ</option>
              </select>
              <input
                v-if="filteredMarkets.length === 0 || !form.marketId || form.marketId === '__custom__'"
                v-model="form.marketName"
                type="text"
                class="field-input mt-2"
                placeholder="Nhập tên chợ truyền thống hoặc cụm quầy hàng..."
                required
              />
            </div>
            <div class="field-item">
              <label class="field-label">Số sạp / Vị trí quầy trong chợ</label>
              <input
                v-model="form.stallNumber"
                type="text"
                class="field-input"
                placeholder="VD: Sạp 12A - Dãy thực phẩm tươi"
              />
            </div>
          </div>

          <!-- Thẻ preview tóm tắt thông tin chợ đã chọn -->
          <div v-if="selectedMarketInfo" class="market-info-preview-box mb-4">
            <div class="market-preview-header">
              <span class="preview-badge">🏬 {{ selectedMarketInfo.marketName }}</span>
              <span class="stall-badge">Quy mô: {{ selectedMarketInfo.totalStalls || 0 }} sạp</span>
            </div>
            <div class="market-preview-details">
              <span class="preview-item">📍 <strong>Địa chỉ cụm chợ:</strong> {{ selectedMarketInfo.address || 'Đang cập nhật' }}</span>
              <span v-if="selectedMarketInfo.managementBoardName" class="preview-item">🏛️ <strong>Đơn vị quản lý:</strong> {{ selectedMarketInfo.managementBoardName }}</span>
            </div>
          </div>

          <!-- Tên cửa hàng -->
          <div class="field-item">
            <label class="field-label">
              Tên cửa hàng <span class="req">*</span>
              <span class="hint-text">(12 - 30 ký tự, không ký tự đặc biệt)</span>
            </label>
            <input
              v-model="form.storeName"
              type="text"
              class="field-input"
              placeholder="VD: Bách Hóa Sạch ZoneMart Cầu Giấy"
              maxlength="30"
              required
            />
          </div>

          <!-- CUSTOM DROPDOWN DANH MỤC HÀNG BÁN CHÍNH (MỞ THẢ XUÔI XUỐNG DƯỚI) -->
          <div class="field-item custom-select-container">
            <label class="field-label">Danh mục hàng bán chính <span class="req">*</span></label>
            <div
              class="custom-select-trigger"
              :class="{ 'active': isCategoryDropdownOpen }"
              @click.stop="toggleCategoryDropdown"
            >
              <span class="selected-text">{{ form.category }}</span>
              <span class="chevron-arrow">▼</span>
            </div>

              <Transition name="fade-dropdown">
                <div v-if="isCategoryDropdownOpen" class="custom-dropdown-menu">
                  <div
                    v-for="cat in categoriesList"
                    :key="cat"
                    class="dropdown-option-item"
                    :class="{ 'selected': form.category === cat }"
                    @click.stop="selectCategory(cat)"
                  >
                    {{ cat }}
                  </div>
                </div>
              </Transition>
            </div>

            <!-- Địa chỉ tiệm kèm Tính năng Xem vị trí & Bản đồ định vị thời gian thực -->
            <div class="field-item">
              <div class="field-label-row">
                <label for="seller-store-address" class="field-label">
                  Địa chỉ tiệm <span class="req">*</span>
                  <span class="hint-text">(Không nhập ký tự đặc biệt)</span>
                </label>
                <button
                  type="button"
                  class="btn-open-map-badge"
                  @click="openLocationModal"
                  title="Mở bản đồ xem và định vị GPS thời gian thực"
                >
                  <i class="bi bi-geo-alt-fill"></i>
                  <span>Xem vị trí trên bản đồ</span>
                </button>
              </div>

              <div class="input-icon-wrapper address-input-wrapper">
                <i class="bi bi-geo-alt-fill input-leading-icon"></i>
                <input
                  id="seller-store-address"
                  name="address"
                  v-model="form.address"
                  type="text"
                  class="field-input has-leading-icon has-locate-btn"
                  placeholder="VD: Số 225 đường Nguyễn Trãi, Thành phố Hà Nội"
                  required
                  aria-required="true"
                />
                <button
                  type="button"
                  class="btn-inline-locate"
                  @click="openLocationModal"
                  title="Định vị GPS vị trí tiệm"
                >
                  <i class="bi bi-crosshair2"></i> Định vị GPS
                </button>
              </div>

              <!-- Thẻ hiển thị tọa độ thời gian thực đã gán -->
              <div v-if="form.lat && form.lng" class="gps-coords-pill">
                <div class="pill-left">
                  <span class="gps-pulse-dot"></span>
                  <span class="pill-title">Tọa độ thực tế:</span>
                  <code class="coords-code">{{ form.lat.toFixed(5) }}, {{ form.lng.toFixed(5) }}</code>
                  <span class="pill-tag-verified"><i class="bi bi-check-circle-fill"></i> Đã gán dẫn đường</span>
                </div>
                <button type="button" class="btn-pill-reselect" @click="openLocationModal">
                  Sửa vị trí <i class="bi bi-pencil-square"></i>
                </button>
              </div>
            </div>

            <!-- CUSTOM DROPDOWN KHUNG GIỜ MỞ CỬA (MỞ THẢ XUÔI XUỐNG DƯỚI) -->
            <div class="field-item custom-select-container">
              <label class="field-label">Cửa hàng mở những giờ nào? <span class="req">*</span></label>
              <div
                class="custom-select-trigger"
                :class="{ 'active': isOpenHoursDropdownOpen }"
                @click.stop="toggleOpenHoursDropdown"
              >
                <span class="selected-text">{{ selectedPreset }}</span>
                <span class="chevron-arrow">▼</span>
              </div>

              <Transition name="fade-dropdown">
                <div v-if="isOpenHoursDropdownOpen" class="custom-dropdown-menu">
                  <div
                    v-for="opt in openHoursOptions"
                    :key="opt"
                    class="dropdown-option-item"
                    :class="{ 'selected': selectedPreset === opt }"
                    @click.stop="selectPresetHours(opt)"
                  >
                    {{ opt }}
                  </div>
                </div>
              </Transition>
            </div>

            <!-- Tùy chọn 2 khung giờ mở & đóng riêng biệt nếu chọn tùy chọn khác -->
            <div v-if="selectedPreset === 'Tùy chọn khung giờ mở - đóng khác...'" class="time-select-row">
              <div class="field-item custom-select-container">
                <label class="field-label">Giờ mở cửa</label>
                <div
                  class="custom-select-trigger"
                  :class="{ 'active': isOpenTimeDropdownOpen }"
                  @click.stop="toggleOpenTimeDropdown"
                >
                  <span class="selected-text">{{ openTime }}</span>
                  <span class="chevron-arrow">▼</span>
                </div>

                <Transition name="fade-dropdown">
                  <div v-if="isOpenTimeDropdownOpen" class="custom-dropdown-menu">
                    <div
                      v-for="h in openTimeList"
                      :key="h"
                      class="dropdown-option-item"
                      :class="{ 'selected': openTime === h }"
                      @click.stop="selectOpenTime(h)"
                    >
                      {{ h }}
                    </div>
                  </div>
                </Transition>
              </div>

              <div class="field-item custom-select-container">
                <label class="field-label">Giờ đóng cửa</label>
                <div
                  class="custom-select-trigger"
                  :class="{ 'active': isCloseTimeDropdownOpen }"
                  @click.stop="toggleCloseTimeDropdown"
                >
                  <span class="selected-text">{{ closeTime }}</span>
                  <span class="chevron-arrow">▼</span>
                </div>

                <Transition name="fade-dropdown">
                  <div v-if="isCloseTimeDropdownOpen" class="custom-dropdown-menu">
                    <div
                      v-for="h in closeTimeList"
                      :key="h"
                      class="dropdown-option-item"
                      :class="{ 'selected': closeTime === h }"
                      @click.stop="selectCloseTime(h)"
                    >
                      {{ h }}
                    </div>
                  </div>
                </Transition>
              </div>
            </div>

            <!-- Nút bấm Chuyển sang Bước 2 -->
            <button type="submit" class="btn-submit-orange">
              Tiếp theo ➔
            </button>
          </form>

          <!-- ==================== BƯỚC 2: XÁC THỰC GIẤY TỜ ==================== -->
          <form v-else-if="currentStep === 2" key="seller-step-2" @submit.prevent="goToNextStep" class="form-body" novalidate>
            <!-- Số CCCD / CMND -->
            <div class="field-item">
              <label for="seller-cccd-number" class="field-label">
                Số CCCD / CMND
                <span class="hint-text">(Tự động nhận diện từ ảnh hoặc nhập thủ công)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-card-text input-leading-icon"></i>
                <input
                  id="seller-cccd-number"
                  name="cccdNumber"
                  v-model="form.cccdNumber"
                  type="text"
                  inputmode="numeric"
                  class="field-input has-leading-icon font-mono"
                  placeholder="VD: 001203004005 hoặc 215316583 (tự động điền)"
                  maxlength="12"
                  @input="handleCccdNumberInput"
                />
              </div>
            </div>

            <!-- Nguyên Quán (Lấy từ CCCD/CMND) -->
            <div class="field-item">
              <label for="seller-nguyen-quan" class="field-label">
                Nguyên Quán
                <span class="hint-text">(Tự động nhận diện từ CCCD hoặc nhập)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-geo-alt-fill input-leading-icon"></i>
                <input
                  id="seller-nguyen-quan"
                  name="nguyenQuan"
                  v-model="form.nguyenQuan"
                  type="text"
                  class="field-input has-leading-icon"
                  placeholder="VD: Tam Quan Bắc, Hoài Nhơn, Bình Định"
                  maxlength="100"
                />
              </div>
            </div>

            <!-- Upload CCCD Mặt Trước & Mặt Sau -->
            <div class="cccd-upload-group">
              <label class="field-label">Tải ảnh CCCD mặt trước & mặt sau <span class="req">*</span></label>
              
              <div class="upload-row">
                <!-- Mặt trước -->
                <div class="upload-card">
                  <input type="file" accept="image/*" id="cccdFront" @change="handleCccdFrontUpload" class="file-input-hidden" />
                  <div 
                    class="upload-box" 
                    :class="{ 'has-preview': form.cccdFrontImage }"
                    @click="handleUploadBoxClick('cccdFrontImage', 'cccdFront', 'Ảnh CCCD Mặt Trước')"
                  >
                    <template v-if="!form.cccdFrontImage">
                      <span class="upload-icon">📷</span>
                      <span class="upload-text">CCCD Mặt trước</span>
                    </template>
                    <template v-else>
                      <img :src="form.cccdFrontImage" alt="CCCD Mặt Trước" class="preview-img" />
                      <div class="preview-overlay-badge">🔍 Nhấn để xem & thay đổi</div>
                    </template>
                  </div>
                </div>

                <!-- Mặt sau -->
                <div class="upload-card">
                  <input type="file" accept="image/*" id="cccdBack" @change="handleCccdBackUpload" class="file-input-hidden" />
                  <div 
                    class="upload-box" 
                    :class="{ 'has-preview': form.cccdBackImage }"
                    @click="handleUploadBoxClick('cccdBackImage', 'cccdBack', 'Ảnh CCCD Mặt Sau')"
                  >
                    <template v-if="!form.cccdBackImage">
                      <span class="upload-icon">💳</span>
                      <span class="upload-text">CCCD Mặt sau</span>
                    </template>
                    <template v-else>
                      <img :src="form.cccdBackImage" alt="CCCD Mặt Sau" class="preview-img" />
                      <div class="preview-overlay-badge">🔍 Nhấn để xem & thay đổi</div>
                    </template>
                  </div>
                </div>
              </div>
            </div>

            <!-- TẢI ẢNH GIẤY CHỨNG NHẬN AN TOÀN THỰC PHẨM -->
            <div class="cccd-upload-group">
              <label class="field-label">Tải ảnh Giấy chứng nhận an toàn thực phẩm <span class="req">*</span></label>
              <div class="upload-card full-width">
                <input type="file" accept="image/*" id="foodSafetyCert" @change="handleFoodSafetyCertUpload" class="file-input-hidden" />
                <div 
                  class="upload-box upload-box-wide" 
                  :class="{ 'has-preview': form.foodSafetyCertImage }"
                  @click="handleUploadBoxClick('foodSafetyCertImage', 'foodSafetyCert', 'Giấy CN An toàn thực phẩm')"
                >
                  <template v-if="!form.foodSafetyCertImage">
                    <span class="upload-icon">📜</span>
                    <span class="upload-text">Tải ảnh Giấy CN An toàn thực phẩm (Tệp hình ảnh .jpg, .png)</span>
                  </template>
                  <template v-else>
                    <img :src="form.foodSafetyCertImage" alt="Giấy CN An toàn thực phẩm" class="preview-img" />
                    <div class="preview-overlay-badge">🔍 Nhấn để xem & thay đổi</div>
                  </template>
                </div>
              </div>
            </div>

            <!-- Nhóm Nút Quay lại / Tiếp theo -->
            <div class="step-btn-group">
              <button type="button" class="btn-secondary-gray" @click="goToPrevStep">
                ← Quay lại
              </button>
              <button type="submit" class="btn-submit-orange btn-flex">
                Tiếp theo ➔
              </button>
            </div>
          </form>

          <!-- ==================== BƯỚC 3: THANH TOÁN & GỬI HỒ SƠ ==================== -->
          <form v-else-if="currentStep === 3" key="seller-step-3" @submit.prevent="handleSubmitApplication" class="form-body" novalidate>
            <!-- CUSTOM DROPDOWN CHỌN NGÂN HÀNG NHẬN TIỀN (MỞ THẢ XUÔI XUỐNG DƯỚI) -->
            <div class="field-item custom-select-container">
              <label class="field-label">Chọn Ngân hàng nhận tiền <span class="req">*</span></label>
              <div
                class="custom-select-trigger"
                :class="{ 'active': isBankDropdownOpen }"
                @click.stop="toggleBankDropdown"
              >
                <span class="selected-text">{{ form.bankName }}</span>
                <span class="chevron-arrow">▼</span>
              </div>

              <Transition name="fade-dropdown">
                <div v-if="isBankDropdownOpen" class="custom-dropdown-menu">
                  <div
                    v-for="bank in banksList"
                    :key="bank"
                    class="dropdown-option-item"
                    :class="{ 'selected': form.bankName === bank }"
                    @click.stop="selectBank(bank)"
                  >
                    {{ bank }}
                  </div>
                </div>
              </Transition>
            </div>

            <!-- Số tài khoản nhận tiền -->
            <div class="field-item">
              <label for="seller-bank-acc" class="field-label">
                Số tài khoản nhận tiền <span class="req">*</span>
                <span class="hint-text">(8 - 15 chữ số)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-credit-card-2-front-fill input-leading-icon"></i>
                <input
                  id="seller-bank-acc"
                  name="bankAccountNumber"
                  v-model="form.bankAccountNumber"
                  type="text"
                  inputmode="numeric"
                  class="field-input has-leading-icon font-mono"
                  placeholder="VD: 190382910283 (Chỉ nhập chữ số)"
                  maxlength="15"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <!-- THÔNG TIN TÀI KHOẢN ĐĂNG NHẬP -->
            <div class="field-item">
              <label for="seller-account-login" class="field-label">
                Số điện thoại hoặc Email đăng nhập <span class="req">*</span>
                <span class="hint-text">(Dùng để đăng nhập và theo dõi kết quả xét duyệt)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-envelope-at-fill input-leading-icon"></i>
                <input
                  id="seller-account-login"
                  name="username"
                  v-model="form.phoneEmail"
                  type="text"
                  autocomplete="username"
                  class="field-input has-leading-icon"
                  placeholder="VD: 0912345678 hoặc email@gmail.com"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <div class="field-item">
              <label for="seller-password" class="field-label">
                Mật khẩu đăng nhập <span class="req">*</span>
                <span class="hint-text">(Tối thiểu 6 ký tự)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-lock-fill input-leading-icon"></i>
                <input
                  id="seller-password"
                  name="new-password"
                  v-model="form.password"
                  type="password"
                  autocomplete="new-password"
                  class="field-input has-leading-icon"
                  placeholder="Nhập mật khẩu cho tài khoản người bán"
                  minlength="6"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <!-- Nhóm Nút Quay lại / Gửi hồ sơ đăng ký -->
            <div class="step-btn-group">
              <button type="button" class="btn-secondary-gray" @click="goToPrevStep">
                ← Quay lại
              </button>
              <button type="submit" class="btn-submit-orange btn-flex" :disabled="isLoading">
                <span v-if="!isLoading">Gửi hồ sơ đăng ký ➔</span>
                <span v-else>ĐANG GỬI HỒ SƠ...</span>
              </button>
            </div>
          </form>

        <!-- Đường kẻ phân cách & Liên kết -->
        <div class="divider-row">
          <span class="line"></span>
          <span class="or-text">ZONEMART KÊNH NGƯỜI BÁN</span>
          <span class="line"></span>
        </div>

        <div class="register-cta-box">
          <div class="cta-line">
            <span>Đã có tài khoản? </span>
            <router-link to="/seller-login" class="link-orange-bold">Đăng nhập ngay</router-link>
          </div>
        </div>

      </div>

    </div>

    <!-- MODAL THÔNG BÁO CĂN GIỮA MÀN HÌNH "ĐÃ GỬI HỒ SƠ THÀNH CÔNG" -->
    <Transition name="fade-modal">
      <div v-if="showSuccessModal" class="modal-backdrop-overlay" @click.self="closeSuccessModal">
        <div class="modal-pop-card">
          
          <!-- ICON TÍCH XANH NỔI BẬT -->
          <div class="modal-icon-badge success-badge">
            <svg width="38" height="38" viewBox="0 0 24 24" fill="none" stroke="#16A34A" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
          </div>

          <h3 class="modal-heading-title green-title">Đã gửi hồ sơ thành công</h3>
          
          <p class="modal-body-text">
            Hồ sơ mở gian hàng <b>{{ form.storeName }}</b> của bạn đã được ghi nhận và đang ở trạng thái <b>CHỜ BAN QUẢN LÝ XÉT DUYỆT</b>!
          </p>

          <div style="background: #fef3c7; color: #92400e; border: 1px solid #fde68a; border-radius: 8px; padding: 10px 14px; font-size: 13px; margin: 12px 0 18px 0; text-align: left; line-height: 1.5;">
            <i class="bi bi-clock-history me-1"></i> <strong>Quy trình kiểm duyệt:</strong> Ban Quản Trị ZoneMart sẽ kiểm tra giấy tờ pháp lý (CCCD & Giấy chứng nhận ATTP). Sau khi được phê duyệt, bạn có thể đăng nhập bằng tài khoản vừa tạo để quản lý gian hàng.
          </div>

          <div class="modal-action-buttons">
            <button type="button" class="btn-modal-close" @click="closeSuccessModal">
              Đăng nhập ngay
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ==================== MODAL XEM & THAY ĐỔI ĂNH (BLUR BACKGROUND + 2 BẢNG NHỎ) ==================== -->
    <Transition name="fade-modal">
      <div v-if="previewModal.isOpen" class="image-modal-overlay" @click.self="closePreviewModal">
        <div class="image-modal-card">
          <!-- Header Modal -->
          <div class="modal-card-header">
            <div class="modal-card-title">
              <span class="title-icon">🔍</span>
              <span>{{ previewModal.title }}</span>
            </div>
            <button type="button" class="btn-close-modal" @click="closePreviewModal">✕</button>
          </div>

          <!-- Body Modal chứa 2 bảng nhỏ -->
          <div class="modal-card-body">
            <!-- BẢNG 1: Khung Xem Ảnh Chi Tiết -->
            <div class="sub-panel image-view-panel">
              <div class="panel-tag">📷 Bảng xem ảnh chi tiết</div>
              <div class="image-display-frame">
                <img :src="previewModal.imageSrc" :alt="previewModal.title" class="full-view-img" />
              </div>
            </div>

            <!-- BẢNG 2: Khung Thông Tin & Thao Tác -->
            <div class="sub-panel action-control-panel">
              <div class="panel-tag">⚙️ Bảng thao tác & Xử lý</div>
              <div class="status-info-box">
                <div class="status-badge">
                  <span class="status-dot"></span>
                  <span>Ảnh đã được tải lên hợp lệ</span>
                </div>
                <p class="status-desc">
                  Bạn có thể xem lại chi tiết hình ảnh đã chọn. Nhấn <strong>"Thay đổi"</strong> nếu bạn muốn tải tệp ảnh khác hoặc chọn <strong>"Lưu"</strong> để giữ nguyên ảnh hiện tại.
                </p>
              </div>

              <!-- 2 Nút Thao Tác: Lưu & Thay Đổi (Không icon) -->
              <div class="modal-btn-actions">
                <button type="button" class="btn-modal-change" @click="triggerChangeImageFromModal">
                  Thay đổi
                </button>
                <button type="button" class="btn-modal-save" @click="closePreviewModal">
                  Lưu
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ==================== MODAL BẢN ĐỒ XEM & ĐỊNH VỊ VỊ TRÍ GIAN HÀNG ==================== -->
    <Transition name="fade-modal">
      <div v-if="isLocationModalOpen" class="map-modal-overlay" @click.self="closeLocationModal">
        <div class="map-modal-card">
          <!-- Modal Header -->
          <div class="map-modal-header">
            <div class="map-modal-title">
              <div class="map-icon-circle">
                <i class="bi bi-geo-alt-fill"></i>
              </div>
              <div class="title-text-group">
                <h3 class="modal-title-text">Bản Đồ Định Vị Gian Hàng (Thời Gian Thực)</h3>
                <p class="modal-sub-text">Tự động định vị điểm đứng của bạn. Kéo thả ghim đỏ để chỉnh vị trí chính xác phục vụ luồng giao hàng.</p>
              </div>
            </div>
            <button type="button" class="btn-close-modal" @click="closeLocationModal">✕</button>
          </div>

          <!-- Modal Body -->
          <div class="map-modal-body">
            <!-- Toolbar tìm kiếm & Nút GPS thời gian thực -->
            <div class="map-toolbar">
              <div class="map-search-wrapper">
                <i class="bi bi-search search-icon"></i>
                <input
                  type="text"
                  v-model="mapSearchQuery"
                  @keydown.enter.prevent="searchAddressOnMap"
                  placeholder="Tìm đường phố, tòa nhà, địa danh (ấn Enter)..."
                  class="map-search-input"
                />
                <button
                  type="button"
                  class="btn-search-trigger"
                  @click="searchAddressOnMap"
                  :disabled="isSearchingLocation"
                >
                  <span v-if="!isSearchingLocation"><i class="bi bi-search"></i> Tìm</span>
                  <span v-else><i class="bi bi-arrow-repeat spin-icon"></i></span>
                </button>
              </div>

              <button
                type="button"
                class="btn-live-gps-action"
                :class="{ 'is-loading': isLocatingGps }"
                @click="locateUserCurrentPosition"
                title="Bấm để lấy vị trí GPS hiện tại theo thời gian thực"
              >
                <i class="bi" :class="isLocatingGps ? 'bi-arrow-repeat spin-icon' : 'bi-crosshair2'"></i>
                <span>{{ isLocatingGps ? 'Đang dò GPS thực tế...' : 'Lấy vị trí GPS của tôi' }}</span>
              </button>
            </div>

            <!-- Leaflet Map Canvas -->
            <div class="map-canvas-wrapper">
              <div id="sellerStoreMap" class="leaflet-map-canvas"></div>

              <!-- Tip hướng dẫn nổi trên bản đồ -->
              <div class="map-floating-hint">
                <i class="bi bi-hand-index-thumb-fill text-warning"></i>
                <span>Chạm bản đồ hoặc kéo ghim đỏ để chọn vị trí chính xác</span>
              </div>

              <!-- Loading Indicator khi đang tải GPS hoặc Reverse Geocoding -->
              <div v-if="isLocatingGps || isGeocodingAddress" class="map-status-overlay">
                <div class="mini-spinner"></div>
                <span>{{ isLocatingGps ? 'Đang lấy tọa độ GPS chính xác...' : 'Đang nhận diện địa chỉ...' }}</span>
              </div>
            </div>

            <!-- Khung thông tin vị trí đã chọn -->
            <div class="selected-location-panel">
              <div class="location-addr-group">
                <label class="location-addr-label">
                  <i class="bi bi-building me-1 text-primary"></i> Địa chỉ nhận diện (có thể chỉnh sửa nếu cần):
                </label>
                <input
                  type="text"
                  v-model="tempLocationAddress"
                  class="location-addr-input"
                  placeholder="Địa chỉ tự động cập nhật khi kéo ghim..."
                />
              </div>

              <div class="location-meta-row">
                <div class="meta-coords">
                  <i class="bi bi-compass-fill text-success me-1"></i>
                  <span>Tọa độ GPS: </span>
                  <strong>{{ tempLat.toFixed(5) }}, {{ tempLng.toFixed(5) }}</strong>
                </div>
                <div class="meta-note">
                  <span class="delivery-ready-badge">
                    <i class="bi bi-shield-check"></i> Sẵn sàng kết nối dẫn đường Shipper
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="map-modal-footer">
            <button type="button" class="btn-map-cancel" @click="closeLocationModal">
              Đóng
            </button>
            <button type="button" class="btn-map-confirm" @click="saveLocationFromMap">
              <i class="bi bi-check-circle-fill me-1"></i> Lưu & Sử Dụng Vị Trí Này
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Toast thông báo đã lưu vị trí thành công -->
    <Transition name="fade-modal">
      <div v-if="showLocationSuccessToast" class="map-toast-pill">
        <i class="bi bi-check-circle-fill text-success"></i>
        <span>Đã lưu thành công tọa độ & địa chỉ gian hàng cho luồng giao hàng!</span>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* ÉP FONT CHỮ THỐNG NHẤT */
*, input, button, select, textarea, label {
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
}

/* PAGE CONTAINER VỪA KHÍT 100VH - KHÔNG CẦN KÉO CUỘN TRANG WEB */
.login-page-container {
  width: 100vw;
  height: 100vh;
  height: 100dvh;
  max-height: 100vh;
  overflow: hidden; /* Không để web phải kéo xuống */
  background: radial-gradient(circle at 40% 30%, #FFFDF9 0%, #FAF5EF 60%, #F3ECE2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 36px 20px 10px 20px;
  box-sizing: border-box;
}

/* HERO LAYOUT 2 CỘT CÂN ĐỐI TỰ NHIÊN GIỮA MÀN HÌNH */
.login-hero-layout {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 32px;
  width: 100%;
  max-width: 1160px;
  max-height: 100%;
  margin: auto;
}

/* CỘT TRÁI - ẢNH MINH HỌA ĐĂNG KÝ BÁN HÀNG TÁCH NỀN */
.login-illustration-side {
  flex: 1.2;
  max-width: 540px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
}

.illustration-img {
  width: 100%;
  max-width: 500px;
  max-height: 420px;
  object-fit: contain;
  position: relative;
  z-index: 1;
  filter: drop-shadow(0 12px 24px rgba(217, 78, 21, 0.13));
  transition: transform 0.3s ease;
}

/* CỘT PHẢI - BẢNG ĐĂNG KÝ NGƯỜI BÁN TRÔI NỔI (FLOATING CARD) */
.login-card-floating {
  width: 100%;
  max-width: 430px;
  background: #FFFFFF;
  border-radius: 18px;
  padding: 12px 20px 10px 20px;
  box-shadow: 
    0 16px 36px -8px rgba(15, 23, 42, 0.1),
    0 6px 16px -4px rgba(217, 78, 21, 0.08),
    0 0 0 1px rgba(240, 230, 220, 0.85);
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  z-index: 2;
  position: relative;
  margin-top: 18px;
  animation: formCardEntrance 0.45s cubic-bezier(0.16, 1, 0.3, 1);
}

/* ================= MASCOT PEELING / PEEKING STYLE (PAGE-MASCOT) ================= */
.seller-mascot-peek-container {
  position: absolute;
  top: -46px;
  right: 16px;
  z-index: 30;
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  user-select: none;
  perspective: 700px;
}

.mascot-sprite-button {
  position: relative;
  width: 76px;
  height: 76px;
  cursor: pointer;
  user-select: none;
  background: transparent;
  border: none;
  padding: 0;
  display: block;
  flex-shrink: 0;
}

.mascot-sprite-stage {
  position: relative;
  width: 100%;
  height: 100%;
  transform-origin: 50% 85%;
  transition: transform 0.15s ease-out;
  filter: drop-shadow(0 6px 14px rgba(217, 78, 21, 0.2));
}

.mascot-sprite-stage.is-squashing {
  animation: mascotSquashBounce 0.42s ease-in-out;
}

@keyframes mascotSquashBounce {
  0% { transform: scale(1, 1); }
  22% { transform: scale(1.12, 0.86); }
  55% { transform: scale(0.95, 1.07); }
  78% { transform: scale(1.03, 0.98); }
  100% { transform: scale(1, 1); }
}

.mascot-sprite-layer {
  position: absolute;
  inset: 0;
  background-size: 300% 300%;
  background-repeat: no-repeat;
  image-rendering: -webkit-optimize-contrast;
  transition: opacity 0.15s ease;
}

.mascot-counter-edge {
  position: absolute;
  bottom: -2px;
  left: 50%;
  transform: translateX(-50%);
  width: 52px;
  height: 3px;
  background: linear-gradient(90deg, rgba(234, 88, 12, 0) 0%, rgba(234, 88, 12, 0.35) 50%, rgba(234, 88, 12, 0) 100%);
  border-radius: 999px;
}

.mascot-speech-bubble {
  position: relative;
  background: #FFFFFF;
  border: 1.5px solid #FED7AA;
  box-shadow: 0 8px 20px -4px rgba(234, 88, 12, 0.16), 0 2px 8px rgba(0, 0, 0, 0.04);
  border-radius: 12px;
  padding: 6px 12px;
  max-width: 275px;
  margin-bottom: 0;
  transition: all 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
  transform-origin: right center;
}

.mascot-speech-bubble.bubble-pop {
  transform: scale(0.92) translateY(2px);
  opacity: 0.5;
}

.mascot-speech-bubble .bubble-text {
  font-size: 10.5px;
  font-weight: 700;
  color: #9A3412;
  line-height: 1.35;
  display: block;
}

.mascot-speech-bubble .bubble-arrow {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  right: -7px;
  width: 0;
  height: 0;
  border-top: 5px solid transparent;
  border-bottom: 5px solid transparent;
  border-left: 7px solid #FFFFFF;
}

.mascot-speech-bubble .bubble-arrow::before {
  content: "";
  position: absolute;
  top: -6px;
  left: -9px;
  width: 0;
  height: 0;
  border-top: 6px solid transparent;
  border-bottom: 6px solid transparent;
  border-left: 8px solid #FED7AA;
  z-index: -1;
}

/* TRẠNG THÁI CẢNH BÁO KHI NHẬP THIẾU HOẶC SAI THÔNG TIN */
.mascot-speech-bubble.is-alert-bubble {
  background: #FEF2F2;
  border-color: #F87171;
  box-shadow: 0 8px 22px -3px rgba(239, 68, 68, 0.28), 0 2px 8px rgba(0, 0, 0, 0.04);
  animation: mascotAlertWiggle 0.42s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
}

@keyframes mascotAlertWiggle {
  0% { transform: scale(0.95) translateX(0); }
  20% { transform: scale(1.02) translateX(-3px); }
  40% { transform: scale(1.02) translateX(3px); }
  60% { transform: scale(1.01) translateX(-1px); }
  80% { transform: scale(1.01) translateX(1px); }
  100% { transform: scale(1) translateX(0); }
}

.mascot-speech-bubble.is-alert-bubble .bubble-text {
  color: #B91C1C;
  font-weight: 800;
}

.mascot-speech-bubble.is-alert-bubble .bubble-arrow {
  border-left-color: #FEF2F2;
}

.mascot-speech-bubble.is-alert-bubble .bubble-arrow::before {
  border-left-color: #F87171;
}

/* RESPONSIVE TRÁNH TRÀN MÀN HÌNH */
@media (max-width: 1024px) {
  .login-hero-layout {
    flex-direction: column;
    gap: 24px;
  }
  .login-illustration-side {
    display: none;
  }
  .login-card-floating {
    max-width: 480px;
  }
}

@media (max-width: 540px) {
  .login-page-container {
    padding: 30px 14px 20px 14px;
    height: auto;
    min-height: 100vh;
    overflow-y: auto;
  }
  .seller-mascot-peek-container {
    top: -40px;
    right: 12px;
  }
  .mascot-sprite-button {
    width: 68px;
    height: 68px;
  }
  .mascot-speech-bubble {
    max-width: 200px;
    padding: 5px 9px;
    margin-bottom: 0;
  }
  .mascot-speech-bubble .bubble-text {
    font-size: 10px;
  }
  .login-card-floating {
    padding: 14px 14px 12px 14px;
    border-radius: 16px;
  }
}

@keyframes formCardEntrance {
  0% {
    opacity: 0;
    transform: translateY(18px) scale(0.985);
  }
  100% {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* HIỆU ỨNG CHUYỂN BƯỚC MULTI-STEP */
.step-slide-enter-active,
.step-slide-leave-active {
  transition: opacity 0.28s cubic-bezier(0.16, 1, 0.3, 1), transform 0.28s cubic-bezier(0.16, 1, 0.3, 1);
}

.step-slide-enter-from {
  opacity: 0;
  transform: translateX(18px);
}

.step-slide-leave-to {
  opacity: 0;
  transform: translateX(-18px);
}

/* NÚT QUAY LẠI */
.card-top-bar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 2px;
}

.back-home-btn {
  background: transparent;
  border: none;
  color: #64748B;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
  padding: 0;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  transition: all 0.2s ease;
}

.back-home-btn:hover {
  color: #059669;
}

/* HEADER BẢNG */
.card-brand-header {
  text-align: center;
  margin-bottom: 4px;
}

.form-main-heading {
  font-size: 17px;
  font-weight: 900;
  color: #10B981;
  margin: 0 0 1px 0;
  letter-spacing: -0.4px;
}

.form-sub-heading {
  font-size: 10.5px;
  color: #64748B;
  margin: 0;
  font-weight: 500;
}

/* STEPPER BAR (3 BƯỚC) */
.stepper-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
  padding: 0 4px;
}

.step-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1px;
  position: relative;
  z-index: 2;
}

.step-badge {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #F1F5F9;
  color: #94A3B8;
  font-size: 10px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.step-label {
  font-size: 9px;
  font-weight: 700;
  color: #94A3B8;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.step-item.active .step-badge {
  background: #ECFDF5;
  color: #10B981;
}

.step-item.current .step-badge {
  background: #10B981;
  color: #FFFFFF;
  box-shadow: 0 4px 10px rgba(16, 185, 129, 0.35);
}

.step-item.current .step-label {
  color: #10B981;
  font-weight: 800;
}

.step-connector {
  flex: 1;
  height: 2px;
  background: #E2E8F0;
  margin: 0 6px;
  transform: translateY(-7px);
  transition: background 0.3s ease;
}

.step-connector.active {
  background: #10B981;
}

/* MSG ERROR */
.msg-box {
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 600;
  margin-bottom: 8px;
}

.msg-box.error {
  background: #FEF2F2;
  color: #DC2626;
  border: 1px solid #FCA5A5;
}

/* FORM BODY & INPUTS */
.form-body {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.seller-type-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px;
  margin-top: 4px;
}

.seller-type-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 8px 6px;
  border-radius: 8px;
  border: 1.5px solid #E2E8F0;
  background: #FFFFFF;
  cursor: pointer;
  transition: all 0.2s ease;
}

.seller-type-card:hover {
  border-color: #FDBA74;
  background: #FFF7ED;
}

.seller-type-card.selected {
  border-color: #D94E15;
  background: #FFF7ED;
  box-shadow: 0 0 0 1px #D94E15;
}

.seller-type-card .type-icon {
  font-size: 18px;
  margin-bottom: 2px;
}

.seller-type-card .type-title {
  font-size: 11px;
  font-weight: 700;
  color: #1E293B;
  line-height: 1.2;
}

.seller-type-card .type-desc {
  font-size: 9.5px;
  color: #64748B;
  margin-top: 2px;
  line-height: 1.1;
}

.field-row-two-cols {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

/* Thẻ tóm tắt thông tin chợ đã chọn */
.market-info-preview-box {
  background: #f0fdf4;
  border: 1.5px solid #86efac;
  border-radius: 10px;
  padding: 10px 14px;
  margin-top: -2px;
  margin-bottom: 8px;
  display: flex;
  flex-direction: column;
  gap: 5px;
  animation: fadeInPreview 0.25s ease-in-out;
}

@keyframes fadeInPreview {
  from {
    opacity: 0;
    transform: translateY(-4px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.market-preview-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  flex-wrap: wrap;
}

.preview-badge {
  font-size: 12px;
  font-weight: 800;
  color: #166534;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.stall-badge {
  background: #dcfce7;
  color: #15803d;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 9999px;
  border: 1px solid #86efac;
}

.market-preview-details {
  display: flex;
  flex-direction: column;
  gap: 3px;
  font-size: 11.5px;
  color: #374151;
}

.preview-item {
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.preview-item strong {
  color: #1f2937;
  font-weight: 600;
}

.field-label {
  font-size: 10.5px;
  font-weight: 700;
  color: #0F172A;
}

.hint-text {
  font-size: 9px;
  font-weight: 500;
  color: #64748B;
  margin-left: 3px;
}

.req {
  color: #EF4444;
  font-weight: 800;
}

.input-icon-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-leading-icon {
  position: absolute;
  left: 10px;
  font-size: 12.5px;
  color: #94A3B8;
  pointer-events: none;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  z-index: 2;
}

.input-icon-wrapper:focus-within .input-leading-icon {
  color: #D94E15;
  transform: scale(1.1);
}

.field-input, .field-select {
  width: 100%;
  padding: 5px 9px;
  border-radius: 9px;
  border: 1.5px solid #E2E8F0;
  background: #F8FAFC;
  font-size: 11.5px;
  color: #0F172A;
  outline: none;
  box-sizing: border-box;
  min-height: 31px;
  transition: all 0.2s ease;
  font-family: inherit;
}

.field-input.has-leading-icon {
  padding-left: 30px;
}

.field-input:focus, .field-select:focus {
  background: #FFFFFF;
  border-color: #10B981;
  box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.14);
}

/* CUSTOM DROPDOWN SELECT CONTAINER (ĐỒNG BỘ SELLER) */
.custom-select-container {
  position: relative;
}

.custom-select-trigger {
  width: 100%;
  padding: 5px 9px;
  border-radius: 9px;
  border: 1.5px solid #E2E8F0;
  background: #F8FAFC;
  font-size: 11.5px;
  color: #0F172A;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-sizing: border-box;
  min-height: 31px;
  transition: all 0.2s ease;
  user-select: none;
}

.custom-select-trigger:hover,
.custom-select-trigger.active {
  border-color: #10B981;
  background: #FFFFFF;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.14);
}

.selected-text {
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.chevron-arrow {
  font-size: 10px;
  color: #94A3B8;
  transition: transform 0.2s ease;
}

.custom-select-trigger.active .chevron-arrow {
  transform: rotate(180deg);
  color: #10B981;
}

.custom-dropdown-menu {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  margin-top: 4px;
  max-height: 180px;
  overflow-y: auto;
  background: #FFFFFF;
  border: 1.5px solid #10B981;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(15, 23, 42, 0.18);
  z-index: 99999;
  padding: 4px 0;
}

.dropdown-option-item {
  padding: 8px 12px;
  font-size: 11.5px;
  color: #334155;
  cursor: pointer;
  transition: background 0.15s ease;
}

.dropdown-option-item:hover {
  background: #ECFDF5;
  color: #10B981;
  font-weight: 700;
}

.dropdown-option-item.selected {
  background: #10B981;
  color: #FFFFFF;
  font-weight: 700;
}

.fade-dropdown-enter-active,
.fade-dropdown-leave-active {
  transition: all 0.2s ease;
}

.fade-dropdown-enter-from,
.fade-dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* UPLOAD CARDS */
.cccd-upload-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.upload-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.upload-card {
  width: 100%;
}

.upload-card.full-width {
  width: 100%;
}

.file-input-hidden {
  display: none;
}

.upload-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 64px;
  border: 1.5px dashed #10B981;
  background: #F0FDF4;
  border-radius: 10px;
  cursor: pointer;
  overflow: hidden;
  transition: all 0.2s ease;
}

.upload-box-wide {
  height: 52px;
  flex-direction: row;
  gap: 8px;
  padding: 0 10px;
}

.upload-box:hover {
  background: #DCFCE7;
  border-color: #059669;
}

.upload-box.has-preview {
  border-style: solid;
  border-color: #10B981;
  background: #F0FDF4;
}

.upload-icon {
  font-size: 18px;
  margin-bottom: 2px;
}

.upload-text {
  font-size: 10.5px;
  font-weight: 700;
  color: #047857;
  text-align: center;
}

.preview-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* BUTTONS */
.btn-submit-orange {
  width: 100%;
  padding: 7px 12px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 24px;
  font-size: 12px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 3px 10px rgba(217, 78, 21, 0.25);
  transition: all 0.2s ease;
  margin-top: 2px;
}

.btn-submit-orange:hover:not(:disabled) {
  background: #C8451F;
  transform: translateY(-1px);
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.32);
}

.btn-submit-orange:active:not(:disabled) {
  transform: translateY(1px) scale(0.985);
  box-shadow: 0 2px 5px rgba(217, 78, 21, 0.2);
}

.step-btn-group {
  display: flex;
  gap: 6px;
  margin-top: 2px;
}

.btn-secondary-gray {
  flex: 0.8;
  padding: 7px 10px;
  background: #F1F5F9;
  color: #475569;
  border: none;
  border-radius: 24px;
  font-size: 11.5px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-secondary-gray:hover {
  background: #E2E8F0;
  color: #0F172A;
  transform: translateY(-1px);
}

.btn-secondary-gray:active {
  transform: translateY(1px) scale(0.985);
}

.btn-flex {
  flex: 1.2;
}

.divider-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 4px 0 2px 0;
}

.divider-row .line {
  flex: 1;
  height: 1px;
  background: #E2E8F0;
}

.divider-row .or-text {
  font-size: 8.5px;
  font-weight: 800;
  color: #94A3B8;
  letter-spacing: 0.5px;
}

.register-cta-box {
  display: flex;
  flex-direction: column;
  gap: 1px;
  text-align: center;
  font-size: 10px;
  color: #64748B;
}

.link-orange-bold {
  color: #D94E15;
  font-weight: 800;
  text-decoration: none;
}
.link-orange-bold:hover {
  text-decoration: underline;
}

/* MODAL OVERLAY (CĂN GIỮA MÀN HÌNH) */
.modal-backdrop-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(15, 23, 42, 0.55);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 99999;
  padding: 20px;
  box-sizing: border-box;
}

.modal-pop-card {
  background: #FFFFFF;
  border-radius: 24px;
  padding: 28px 24px 22px 24px;
  width: 100%;
  max-width: 380px;
  text-align: center;
  box-shadow: 
    0 25px 50px -12px rgba(0, 0, 0, 0.3),
    0 0 0 1px rgba(240, 230, 220, 0.9);
  display: flex;
  flex-direction: column;
  align-items: center;
  animation: modalScaleUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalScaleUp {
  from {
    opacity: 0;
    transform: scale(0.88) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-icon-badge.success-badge {
  width: 64px;
  height: 64px;
  background: #F0FDF4;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 14px;
  border: 2px solid #BBF7D0;
  box-shadow: 0 4px 14px rgba(22, 163, 74, 0.2);
}

.modal-heading-title.green-title {
  font-size: 20px;
  font-weight: 900;
  color: #15803D;
  margin: 0 0 10px 0;
}

.modal-body-text {
  font-size: 13px;
  color: #475569;
  margin: 0 0 20px 0;
  line-height: 1.5;
}

.modal-action-buttons {
  width: 100%;
}

.btn-modal-close {
  width: 100%;
  padding: 11px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
}

.btn-modal-close:hover {
  background: #C8451F;
}

.time-select-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin-top: -2px;
}

.fade-modal-enter-active,
.fade-modal-leave-active {
  transition: opacity 0.25s ease;
}

.fade-modal-enter-from,
.fade-modal-leave-to {
  opacity: 0;
}

/* ==================== MODAL XEM & THAY ĐỔI ĂNH (BLUR BACKGROUND + 2 BẢNG NHỎ) ==================== */
.image-modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.image-modal-card {
  background: #ffffff;
  width: 100%;
  max-width: 680px;
  border-radius: 16px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.35);
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.2);
  animation: modalPopIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalPopIn {
  from {
    opacity: 0;
    transform: scale(0.92) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: #FAF5EF;
  border-bottom: 1px solid #EAE2D7;
}

.modal-card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
  font-weight: 800;
  color: #2D3748;
}

.btn-close-modal {
  background: #edf2f7;
  border: none;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-size: 16px;
  cursor: pointer;
  color: #4a5568;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.btn-close-modal:hover {
  background: #e2e8f0;
  color: #1a202c;
}

/* 2 Bảng nhỏ (Sub-panels) */
.modal-card-body {
  padding: 20px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

@media (max-width: 640px) {
  .modal-card-body {
    grid-template-columns: 1fr;
  }
}

.sub-panel {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  position: relative;
}

.panel-tag {
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #64748b;
  margin-bottom: 10px;
}

.image-display-frame {
  flex: 1;
  min-height: 180px;
  max-height: 240px;
  background: #ffffff;
  border-radius: 8px;
  border: 1px dashed #cbd5e1;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 6px;
}

.full-view-img {
  max-width: 100%;
  max-height: 220px;
  object-fit: contain;
  border-radius: 6px;
}

.action-control-panel {
  justify-content: space-between;
}

.status-info-box {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #dcfce7;
  color: #166534;
  font-size: 0.82rem;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 20px;
  width: fit-content;
}

.status-dot {
  width: 8px;
  height: 8px;
  background: #22c55e;
  border-radius: 50%;
}

.status-desc {
  font-size: 0.85rem;
  color: #475569;
  line-height: 1.5;
  margin: 0;
}

.modal-btn-actions {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 16px;
}

.btn-modal-change {
  background: #FFF3E0;
  color: #E65100;
  border: 1.5px solid #FFB74D;
  border-radius: 8px;
  padding: 10px 16px;
  font-size: 0.92rem;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-modal-change:hover {
  background: #FFE0B2;
  border-color: #FB8C00;
}

.btn-modal-save {
  background: linear-gradient(135deg, #e65100 0%, #f57c00 100%);
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 11px 16px;
  font-size: 0.92rem;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  box-shadow: 0 4px 12px rgba(230, 81, 0, 0.25);
  transition: all 0.2s ease;
}

.btn-modal-save:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(230, 81, 0, 0.35);
}

/* ================================================================
   Ô THÔNG BÁO TÀI KHOẢN KHÁCH HÀNG LIÊN KẾT (CUSTOMER ACCOUNT BANNER)
   ================================================================ */
.customer-account-banner {
  padding: 6px 10px;
  border-radius: 10px;
  margin-bottom: 6px;
  font-size: 11px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.02);
  transition: all 0.25s ease;
}

.customer-account-banner.success-linked {
  background: #FFF7ED;
  border: 1.5px solid #FFEDD5;
}

.customer-account-banner.warning-not-linked {
  background: #FEF2F2;
  border: 1.5px solid #FECACA;
}

.banner-head-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 800;
  color: #1F2937;
  font-size: 11.5px;
  margin-bottom: 2px;
}

.banner-head-title {
  color: #9A3412;
  letter-spacing: -0.2px;
}

.warning-not-linked .banner-head-title {
  color: #991B1B;
}

.banner-user-detail {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  color: #374151;
  padding-left: 2px;
}

.highlight-email {
  color: #D94E15;
  font-weight: 700;
}

.banner-warn-desc {
  font-size: 12.5px;
  color: #7F1D1D;
  margin: 4px 0 10px 0;
  line-height: 1.5;
}

.btn-banner-login {
  background: linear-gradient(135deg, #D94E15 0%, #EA580C 100%);
  color: #ffffff;
  border: none;
  padding: 9px 16px;
  border-radius: 10px;
  font-weight: 800;
  font-size: 12.5px;
  cursor: pointer;
  box-shadow: 0 4px 10px rgba(217, 78, 21, 0.25);
  transition: all 0.2s ease;
}

.btn-banner-login:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 14px rgba(217, 78, 21, 0.35);
}

.upload-box {
  position: relative;
  cursor: pointer;
}

.preview-overlay-badge {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  color: #ffffff;
  font-size: 0.78rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s ease;
  border-radius: 8px;
  padding: 4px 8px;
  text-align: center;
}

.upload-box.has-preview:hover .preview-overlay-badge {
  opacity: 1;
}

/* ==================== BẢN ĐỒ VỊ TRÍ GIAN HÀNG & GPS THỜI GIAN THỰC ==================== */
.field-label-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}

.btn-open-map-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  background: rgba(37, 99, 235, 0.08);
  color: #2563eb;
  border: 1px solid rgba(37, 99, 235, 0.25);
  border-radius: 999px;
  padding: 4px 10px;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-open-map-badge:hover {
  background: #2563eb;
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(37, 99, 235, 0.25);
}

.address-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.has-locate-btn {
  padding-right: 120px !important;
}

.btn-inline-locate {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  background: #EFF6FF;
  color: #1D4ED8;
  border: 1px solid #BFDBFE;
  border-radius: 8px;
  padding: 6px 12px;
  font-size: 0.78rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  transition: all 0.18s ease;
}

.btn-inline-locate:hover {
  background: #DBEAFE;
  color: #1E40AF;
}

/* GPS Coordinates Pill */
.gps-coords-pill {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #F0FDF4;
  border: 1px solid #BBF7D0;
  border-radius: 10px;
  padding: 8px 12px;
  margin-top: 8px;
  font-size: 0.8rem;
  animation: fadeIn 0.3s ease;
}

.pill-left {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.gps-pulse-dot {
  width: 8px;
  height: 8px;
  background: #16A34A;
  border-radius: 50%;
  box-shadow: 0 0 0 0 rgba(22, 163, 74, 0.7);
  animation: pulseDot 1.8s infinite;
}

@keyframes pulseDot {
  0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(22, 163, 74, 0.7); }
  70% { transform: scale(1); box-shadow: 0 0 0 6px rgba(22, 163, 74, 0); }
  100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(22, 163, 74, 0); }
}

.pill-title {
  color: #166534;
  font-weight: 600;
}

.coords-code {
  background: #DCFCE7;
  color: #15803D;
  padding: 2px 6px;
  border-radius: 4px;
  font-family: monospace;
  font-weight: 700;
  font-size: 0.78rem;
}

.pill-tag-verified {
  color: #15803D;
  font-weight: 700;
  font-size: 0.75rem;
  margin-left: 4px;
}

.btn-pill-reselect {
  background: transparent;
  border: none;
  color: #2563EB;
  font-weight: 700;
  font-size: 0.78rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 3px;
}

.btn-pill-reselect:hover {
  text-decoration: underline;
}

/* ==================== MAP MODAL OVERLAY & CARD ==================== */
.map-modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 99999;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
}

.map-modal-card {
  width: 100%;
  max-width: 760px;
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  border: 1px solid rgba(226, 232, 240, 0.8);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  animation: modalScaleUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  max-height: 94vh;
}

.map-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 24px;
  border-bottom: 1px solid #F1F5F9;
  background: linear-gradient(180deg, #FFFFFF 0%, #F8FAFC 100%);
}

.map-modal-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.map-icon-circle {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: linear-gradient(135deg, #FF6B00 0%, #EA580C 100%);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.3);
}

.title-text-group {
  display: flex;
  flex-direction: column;
}

.modal-title-text {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 800;
  color: #0F172A;
}

.modal-sub-text {
  margin: 2px 0 0 0;
  font-size: 0.8rem;
  color: #64748B;
}

/* Map Modal Body */
.map-modal-body {
  padding: 16px 24px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow-y: auto;
}

/* Map Toolbar */
.map-toolbar {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.map-search-wrapper {
  position: relative;
  flex: 1;
  min-width: 260px;
  display: flex;
  align-items: center;
}

.map-search-wrapper .search-icon {
  position: absolute;
  left: 12px;
  color: #94A3B8;
  font-size: 0.9rem;
}

.map-search-input {
  width: 100%;
  height: 40px;
  padding: 0 80px 0 36px;
  background: #F8FAFC;
  border: 1px solid #CBD5E1;
  border-radius: 10px;
  font-size: 0.85rem;
  color: #0F172A;
  transition: all 0.2s ease;
}

.map-search-input:focus {
  background: #FFFFFF;
  border-color: #EA580C;
  outline: none;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.15);
}

.btn-search-trigger {
  position: absolute;
  right: 5px;
  height: 30px;
  padding: 0 12px;
  background: #EA580C;
  color: #ffffff;
  border: none;
  border-radius: 6px;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.18s;
}

.btn-search-trigger:hover {
  background: #C2410C;
}

.btn-live-gps-action {
  height: 40px;
  padding: 0 16px;
  background: #EFF6FF;
  border: 1px solid #BFDBFE;
  border-radius: 10px;
  color: #1D4ED8;
  font-size: 0.82rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.2s ease;
}

.btn-live-gps-action:hover {
  background: #DBEAFE;
  color: #1E40AF;
  box-shadow: 0 2px 8px rgba(29, 78, 216, 0.15);
}

.btn-live-gps-action.is-loading {
  background: #FEF3C7;
  border-color: #FDE68A;
  color: #D97706;
}

.spin-icon {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* Map Canvas */
.map-canvas-wrapper {
  position: relative;
  width: 100%;
  height: 330px;
  border-radius: 14px;
  overflow: hidden;
  border: 1px solid #CBD5E1;
  box-shadow: inset 0 2px 6px rgba(0, 0, 0, 0.05);
}

.leaflet-map-canvas {
  width: 100%;
  height: 100%;
  z-index: 1;
}

.map-floating-hint {
  position: absolute;
  top: 10px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 1000;
  background: rgba(15, 23, 42, 0.85);
  color: #ffffff;
  padding: 6px 14px;
  border-radius: 999px;
  font-size: 0.78rem;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 6px;
  pointer-events: none;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.map-status-overlay {
  position: absolute;
  inset: 0;
  z-index: 2000;
  background: rgba(255, 255, 255, 0.75);
  backdrop-filter: blur(2px);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  color: #0F172A;
  font-size: 0.85rem;
  font-weight: 700;
}

.mini-spinner {
  width: 28px;
  height: 28px;
  border: 3px solid #E2E8F0;
  border-top-color: #EA580C;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

/* LEAFLET CUSTOM STORE PIN */
:deep(.custom-store-pin) {
  background: transparent !important;
  border: none !important;
}

:deep(.store-pin-wrapper) {
  position: relative;
  width: 42px;
  height: 50px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-start;
}

:deep(.pin-marker) {
  width: 38px;
  height: 38px;
  background: linear-gradient(135deg, #EA580C 0%, #DC2626 100%);
  color: #ffffff;
  border-radius: 50% 50% 50% 0;
  transform: rotate(-45deg);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 14px rgba(220, 38, 38, 0.5);
  border: 2px solid #ffffff;
}

:deep(.pin-marker i) {
  transform: rotate(45deg);
  font-size: 18px;
}

:deep(.pin-pulse) {
  position: absolute;
  top: 6px;
  left: 6px;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  background: rgba(234, 88, 12, 0.4);
  animation: pinPulse 1.6s ease-out infinite;
  pointer-events: none;
}

@keyframes pinPulse {
  0% { transform: scale(0.6); opacity: 1; }
  100% { transform: scale(2.2); opacity: 0; }
}

:deep(.pin-shadow) {
  width: 16px;
  height: 6px;
  background: rgba(0, 0, 0, 0.3);
  border-radius: 50%;
  margin-top: -2px;
  filter: blur(1px);
}

/* Selected Location Panel */
.selected-location-panel {
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  border-radius: 12px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.location-addr-label {
  font-size: 0.78rem;
  font-weight: 700;
  color: #475569;
  display: block;
  margin-bottom: 4px;
}

.location-addr-input {
  width: 100%;
  padding: 8px 12px;
  background: #ffffff;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #0F172A;
}

.location-addr-input:focus {
  border-color: #2563EB;
  outline: none;
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.1);
}

.location-meta-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
  font-size: 0.8rem;
}

.meta-coords {
  color: #334155;
}

.delivery-ready-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: #ECFDF5;
  color: #059669;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 6px;
}

/* Modal Footer */
.map-modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 14px 24px;
  border-top: 1px solid #F1F5F9;
  background: #FAFAFA;
}

.btn-map-cancel {
  padding: 9px 20px;
  background: #F1F5F9;
  color: #475569;
  border: none;
  border-radius: 10px;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.18s;
}

.btn-map-cancel:hover {
  background: #E2E8F0;
  color: #0F172A;
}

.btn-map-confirm {
  padding: 9px 24px;
  background: linear-gradient(135deg, #FF6B00 0%, #EA580C 100%);
  color: #ffffff;
  border: none;
  border-radius: 10px;
  font-size: 0.85rem;
  font-weight: 800;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.35);
  transition: all 0.2s;
}

.btn-map-confirm:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(234, 88, 12, 0.45);
}

/* Toast Notification */
.map-toast-pill {
  position: fixed;
  bottom: 28px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 100000;
  background: #0F172A;
  color: #ffffff;
  padding: 12px 22px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 0.85rem;
  font-weight: 700;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.3);
  animation: slideUpToast 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes slideUpToast {
  from { transform: translate(-50%, 20px); opacity: 0; }
  to { transform: translate(-50%, 0); opacity: 1; }
}

.btn-gps-detect {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  border-radius: 6px;
  padding: 3px 9px;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 1px 3px rgba(16, 185, 129, 0.25);
}
.btn-gps-detect:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
  box-shadow: 0 2px 6px rgba(16, 185, 129, 0.35);
}
.btn-gps-detect:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}
</style>
