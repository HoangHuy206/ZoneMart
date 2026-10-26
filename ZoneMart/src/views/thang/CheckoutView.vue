<script setup lang="ts">
/**
 * ================================================================
 * THANH TOÁN (CHECKOUT) - ZONEMART
 * Hỗ trợ thay đổi địa chỉ nhận hàng linh hoạt & lưu riêng biệt 100%
 * theo từng tài khoản (Account-Isolated Storage) cho các lần mua sau.
 * Tích hợp BẢN ĐỒ ĐỊNH VỊ (Leaflet + Google Maps Tiles + GPS định vị).
 * Tích hợp THANH TOÁN VIETINBANK (101882796072 - DOAN HOANG HUY).
 * Kết nối API / Webhook Tingo Pay:
 * - Khi chưa nhận được tiền: Báo "Hệ thống đang xử lý... Chưa nhận được tiền"
 * - Khi tiền đã vào: Tự động hiện Animation nút tích xanh thành công!
 * ================================================================
 */
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from "vue";
import { useRouter } from "vue-router";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { useCart } from "../../composables/useCart";
import { useAuth } from "../../composables/useAuth";
import { useToast } from "../../composables/useToast";
import { orderRealtimeService } from "../../services/orderRealtimeService";
import { useProductModeration } from "../../composables/useProductModeration";
import { getSafeCurrentLocation } from "../../composables/useSafeLocation";
import { apiFetch, getApiUrl } from "../../utils/apiConfig";

const router = useRouter();
const cart = useCart();
const auth = useAuth();
const toast = useToast();
const productModeration = useProductModeration();

// Khóa phân vùng lưu địa chỉ riêng cho từng tài khoản
const ADDRESS_STORAGE_PREFIX = "zonemart_user_address_";

const getAccountKey = (): string => {
  const acc = auth.currentUser.value;
  if (acc) {
    return (acc.phoneEmail || acc.id || acc.phone || "user")
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9_-]/g, "_");
  }
  let guestId = localStorage.getItem("zonemart_guest_id");
  if (!guestId) {
    guestId = "guest_" + Math.random().toString(36).substring(2, 9);
    localStorage.setItem("zonemart_guest_id", guestId);
  }
  return guestId;
};

// State thông tin nhận hàng
const shippingAddress = ref("Ngõ 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội");
const buyerPhone = ref("0396222614");
const buyerName = ref("Khách Hàng");
const deliveryNote = ref("");
const currentCoords = ref<{ lat: number; lng: number }>({ lat: 21.0333, lng: 105.7983 });

// Interface & State Quản lý Sổ Địa Chỉ & Địa Chỉ Mặc Định
export interface SavedAddress {
  id: string;
  name: string;
  phone: string;
  address: string;
  note?: string;
  lat: number;
  lng: number;
  label?: string; // "Nhà riêng" | "Văn phòng"
  isDefault: boolean;
}

const savedAddresses = ref<SavedAddress[]>([]);
const showAddressListModal = ref(false);
const isEditingNew = ref(false);
const editingAddressId = ref<string | null>(null);
const isSetAsDefault = ref(false);
const addressLabel = ref<'Nhà riêng' | 'Văn phòng'>('Nhà riêng');

const currentSelectedAddress = computed(() => {
  return savedAddresses.value.find(a => a.address === shippingAddress.value) ||
         savedAddresses.value.find(a => a.isDefault) ||
         (savedAddresses.value.length > 0 ? savedAddresses.value[0] : null);
});

// State Modal thay đổi địa chỉ
const showEditAddressModal = ref(false);
const editForm = ref({
  name: "",
  phone: "",
  address: "",
  note: "",
  lat: 21.0333,
  lng: 105.7983
});

// Toast notification
const toastMessage = ref("");
const showToast = ref(false);
let toastTimer: any = null;

const triggerToast = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  toast.info(msg, "Thông Báo");
  if (toastTimer) clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    showToast.value = false;
  }, 3000);
};

// ================================================================
// QUẢN LÝ BẢN ĐỒ ĐỊNH VỊ LEAFLET & GEOCODING THÔNG MINH
// ================================================================
let mapInstance: L.Map | null = null;
let markerInstance: L.Marker | null = null;
const isLocating = ref(false);
const isGeocoding = ref(false);
const mapSearchQuery = ref("");

const VIETNAM_LANDMARKS = [
  { keywords: ["đh sư phạm thể dục thể thao", "dh su pham the duc the thao", "the duc the thao", "tdtt", "phụng châu", "phung chau", "chương mỹ"], name: "Trường ĐH Sư phạm Thể dục Thể thao Hà Nội, Phụng Châu, Chương Mỹ, Hà Nội", lat: 20.9380, lng: 105.7082 },
  { keywords: ["đh quốc gia", "dh quoc gia", "vnu", "144 xuân thủy", "xuan thuy"], name: "Đại học Quốc Gia Hà Nội, 144 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội", lat: 21.0372, lng: 105.7820 },
  { keywords: ["đh sư phạm hà nội", "dh su pham ha noi", "hnue", "136 xuân thủy"], name: "Đại học Sư phạm Hà Nội, 136 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội", lat: 21.0368, lng: 105.7832 },
  { keywords: ["đh bách khoa", "dh bach khoa", "hust", "đại cồ việt", "dai co viet"], name: "Đại học Bách Khoa Hà Nội, Số 1 Đại Cồ Việt, Bách Khoa, Hai Bà Trưng, Hà Nội", lat: 21.0056, lng: 105.8433 },
  { keywords: ["đh kinh tế quốc dân", "dh kinh te quoc dan", "neu", "giải phóng"], name: "Đại học Kinh tế Quốc dân, 207 Giải Phóng, Đồng Tâm, Hai Bà Trưng, Hà Nội", lat: 21.0003, lng: 105.8427 },
  { keywords: ["đh xây dựng", "dh xay dung", "huce"], name: "Đại học Xây dựng Hà Nội, 55 Giải Phóng, Đồng Tâm, Hai Bà Trưng, Hà Nội", lat: 21.0038, lng: 105.8420 },
  { keywords: ["đh ngoại thương", "dh ngoai thuong", "ftu", "chùa láng", "chua lang"], name: "Đại học Ngoại Thương, 91 Chùa Láng, Láng Thượng, Đống Đa, Hà Nội", lat: 21.0227, lng: 105.8038 },
  { keywords: ["học viện ngoại giao", "hoc vien ngoai giao", "dav"], name: "Học viện Ngoại giao, 69 Chùa Láng, Láng Thượng, Đống Đa, Hà Nội", lat: 21.0219, lng: 105.8048 },
  { keywords: ["đh luật hà nội", "dh luat ha noi", "hlu", "nguyễn chí thanh"], name: "Đại học Luật Hà Nội, 87 Nguyễn Chí Thanh, Láng Thượng, Đống Đa, Hà Nội", lat: 21.0202, lng: 105.8093 },
  { keywords: ["học viện ngân hàng", "hoc vien ngan hang", "bav", "chùa bộc"], name: "Học viện Ngân hàng, 12 Chùa Bộc, Quang Trung, Đống Đa, Hà Nội", lat: 21.0084, lng: 105.8288 },
  { keywords: ["đh y hà nội", "dh y ha noi", "hmu", "tôn thất tùng"], name: "Đại học Y Hà Nội, Số 1 Tôn Thất Tùng, Kim Liên, Đống Đa, Hà Nội", lat: 21.0040, lng: 105.8306 },
  { keywords: ["đh thương mại", "dh thuong mai", "tmu", "hồ tùng mậu"], name: "Đại học Thương mại, 79 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội", lat: 21.0370, lng: 105.7745 },
  { keywords: ["đh công nghiệp", "dh cong nghiep", "haui", "nhổn", "nhon", "cầu diễn"], name: "Đại học Công nghiệp Hà Nội, 298 Cầu Diễn, Minh Khai, Bắc Từ Liêm, Hà Nội", lat: 21.0537, lng: 105.7351 },
  { keywords: ["học viện bưu chính viễn thông", "ptit", "mỗ lao", "trần phú hà đông"], name: "Học viện Công nghệ Bưu chính Viễn thông, Km10 Trần Phú, Mộ Lao, Hà Đông, Hà Nội", lat: 20.9808, lng: 105.7876 },
  { keywords: ["đh kiến trúc", "dh kien truc", "hau", "văn quán"], name: "Đại học Kiến trúc Hà Nội, Km10 Trần Phú, Văn Quán, Hà Đông, Hà Nội", lat: 20.9822, lng: 105.7898 },
  { keywords: ["đh hà nội", "dh ha noi", "hanu", "nguyễn trãi"], name: "Đại học Hà Nội, Km9 Nguyễn Trãi, Trung Văn, Nam Từ Liêm, Hà Nội", lat: 20.9912, lng: 105.7958 },
  { keywords: ["keangnam", "landmark 72", "phạm hùng"], name: "Keangnam Landmark 72, Phạm Hùng, Mễ Trì, Nam Từ Liêm, Hà Nội", lat: 21.0173, lng: 105.7838 },
  { keywords: ["royal city", "vincom royal city", "72 nguyễn trãi"], name: "Vinhomes Royal City, 72A Nguyễn Trãi, Thượng Đình, Thanh Xuân, Hà Nội", lat: 21.0028, lng: 105.8158 },
  { keywords: ["times city", "vincom times city", "458 minh khai"], name: "Vinhomes Times City, 458 Minh Khai, Vĩnh Tuy, Hai Bà Trưng, Hà Nội", lat: 20.9950, lng: 105.8680 },
  { keywords: ["smart city", "vincom smart city", "tây mỗ"], name: "Vinhomes Smart City, Tây Mỗ, Nam Từ Liêm, Hà Nội", lat: 21.0016, lng: 105.7483 },
  { keywords: ["aeon mall hà đông", "aeon ha dong", "dương nội"], name: "AEON Mall Hà Đông, Khu đô thị Dương Nội, Hà Đông, Hà Nội", lat: 20.9788, lng: 105.7535 },
  { keywords: ["svđ mỹ đình", "sân vận động mỹ đình", "svd my dinh", "lê đức thọ"], name: "Sân vận động Quốc gia Mỹ Đình, Lê Đức Thọ, Mỹ Đình 1, Nam Từ Liêm, Hà Nội", lat: 21.0205, lng: 105.7640 },
  { keywords: ["hồ gươm", "hồ hoàn kiếm", "ho guom", "đinh tiên hoàng", "tràng tiền"], name: "Hồ Hoàn Kiếm, Hàng Trống, Hoàn Kiếm, Hà Nội", lat: 21.0285, lng: 105.8542 },
  { keywords: ["cầu giấy", "cau giay", "dịch vọng", "dich vong"], name: "Số 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội", lat: 21.0333, lng: 105.7983 },
  { keywords: ["nam từ liêm", "nam tu liem", "mỹ đình", "my dinh"], name: "Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội", lat: 21.0125, lng: 105.7725 },
  { keywords: ["ba đình", "ba dinh", "kim mã", "liễu giai"], name: "Kim Mã, Ba Đình, Hà Nội", lat: 21.0345, lng: 105.8235 },
  { keywords: ["đống đa", "dong da", "chùa láng", "láng hạ"], name: "Chùa Láng, Láng Thượng, Đống Đa, Hà Nội", lat: 21.0180, lng: 105.8270 },
  { keywords: ["hoàn kiếm", "hoan kiem", "phố cổ", "tràng tiền"], name: "Tràng Tiền, Hoàn Kiếm, Hà Nội", lat: 21.0285, lng: 105.8542 },
  { keywords: ["thanh xuân", "thanh xuan", "nguyễn trãi"], name: "Nguyễn Trãi, Thanh Xuân Bắc, Thanh Xuân, Hà Nội", lat: 20.9930, lng: 105.8050 },
  { keywords: ["hà đông", "ha dong", "quang trung", "trần phú"], name: "Quang Trung, Hà Cầu, Hà Đông, Hà Nội", lat: 20.9700, lng: 105.7750 },
  { keywords: ["tây hồ", "tay ho", "lạc long quân", "quảng an"], name: "Lạc Long Quân, Bưởi, Tây Hồ, Hà Nội", lat: 21.0600, lng: 105.8200 },
  { keywords: ["chương mỹ", "chuong my", "chúc sơn", "phụng châu"], name: "Phụng Châu, Chương Mỹ, Hà Nội", lat: 20.9380, lng: 105.7082 }
];

const removeVietnameseTones = (str: string): string => {
  return str
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .trim();
};

const createPinIcon = () => {
  return L.divIcon({
    className: "custom-checkout-pin",
    html: `
      <div class="checkout-pin-wrapper">
        <div class="pin-pulse"></div>
        <div class="pin-marker">
          <i class="bi bi-geo-alt-fill"></i>
        </div>
        <div class="pin-shadow"></div>
      </div>
    `,
    iconSize: [40, 48],
    iconAnchor: [20, 46],
    popupAnchor: [0, -42]
  });
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

const resolveAddressFromCoords = async (lat: number, lng: number) => {
  isGeocoding.value = true;
  editForm.value.lat = lat;
  editForm.value.lng = lng;

  // Phòng vệ: Nếu kinh độ = 0 hoặc ngoài lãnh thổ Việt Nam thì đưa về Hà Nội
  if (lng < 100 || lng > 115 || lat < 8 || lat > 24) {
    console.warn("[CheckoutView] Tọa độ ngoài Việt Nam:", lat, lng);
    lat = 21.0215;
    lng = 105.7497;
    editForm.value.lat = lat;
    editForm.value.lng = lng;
  }

  // 1. Ưu tiên gọi Backend C# endpoint /api/aichat/safe-location
  try {
    const res = await apiFetch('/api/aichat/safe-location', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ lat, lon: lng, lng }),
    });
    if (res && res.ok) {
      const data = await res.json().catch(() => ({}));
      if (data.safeLocationName && !/toa\s*do|gps|\d+\.\d{3,}/i.test(data.safeLocationName)) {
        editForm.value.address = data.safeLocationName.trim();
        triggerToast("📍 Đã cập nhật địa chỉ: " + editForm.value.address);
        isGeocoding.value = false;
        return;
      }
    }
  } catch (e) {}

  // 2. Dự phòng qua Esri Reverse Geocode
  try {
    const esriUrl = `https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode?f=pjson&location=${lng},${lat}`;
    const res = await fetch(esriUrl, { signal: AbortSignal.timeout(4000) });
    if (res.ok) {
      const data = await res.json();
      if (data && data.address) {
        const addrObj = data.address;
        const match = addrObj.Match_addr || addrObj.LongLabel || addrObj.Address;
        if (match) {
          const cleanAddr = match.replace(/,\s*(VNM|Vietnam|Việt Nam)$/i, "").trim();
          editForm.value.address = cleanAddr;
          triggerToast("📍 Đã cập nhật địa chỉ: " + cleanAddr);
          isGeocoding.value = false;
          return;
        }
      }
    }
  } catch (e) {}

  // 3. Dự phòng BigDataCloud
  try {
    const bdcUrl = `https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lng}&localityLanguage=vi`;
    const bdcRes = await fetch(bdcUrl, { signal: AbortSignal.timeout(3500) });
    if (bdcRes.ok) {
      const bdcData = await bdcRes.json();
      if (bdcData) {
        const parts = [
          bdcData.locality || "",
          bdcData.city || "",
          bdcData.principalSubdivision || "Hà Nội"
        ].filter(Boolean);
        if (parts.length > 0) {
          const resolved = parts.join(", ");
          editForm.value.address = resolved;
          triggerToast("📍 Đã cập nhật địa chỉ: " + resolved);
          isGeocoding.value = false;
          return;
        }
      }
    }
  } catch (e) {}

  isGeocoding.value = false;
  const { landmark } = findNearestLandmark(lat, lng);
  const fallback = `Gần ${landmark.name}, Hà Nội`;
  editForm.value.address = fallback;
  triggerToast("📍 Đã định vị: " + fallback);
};

const initOrUpdateMap = () => {
  nextTick(() => {
    setTimeout(() => {
      const container = document.getElementById("checkoutAddressMap");
      if (!container) return;

      const targetLat = editForm.value.lat || 21.0333;
      const targetLng = editForm.value.lng || 105.7983;

      if (!mapInstance) {
        mapInstance = L.map("checkoutAddressMap", {
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

        markerInstance.bindTooltip("📍 Kéo ghim tới đúng vị trí của bạn", {
          permanent: false,
          direction: "top"
        });

        markerInstance.on("dragend", (e: any) => {
          const latLng = e.target.getLatLng();
          resolveAddressFromCoords(latLng.lat, latLng.lng);
        });

        mapInstance.on("click", (e: L.LeafletMouseEvent) => {
          const { lat, lng } = e.latlng;
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
    }, 120);
  });
};

const locateUserCurrentPosition = async () => {
  isLocating.value = true;
  triggerToast("Đang xác định vị trí của bạn...");

  try {
    const loc = await getSafeCurrentLocation();
    if (loc && loc.lat && loc.lon) {
      editForm.value.lat = loc.lat;
      editForm.value.lng = loc.lon;
      if (loc.safeLocationName) {
        editForm.value.address = loc.safeLocationName;
      }
      if (mapInstance) {
        mapInstance.flyTo([loc.lat, loc.lon], 17);
      }
      if (markerInstance) {
        markerInstance.setLatLng([loc.lat, loc.lon]);
      }
      if (loc.safeLocationName) {
        triggerToast("📍 Đã định vị vị trí: " + loc.safeLocationName);
      } else {
        resolveAddressFromCoords(loc.lat, loc.lon);
      }
      return;
    }
    triggerToast("Không thể lấy GPS chính xác. Đã hiển thị vị trí gần bạn nhất trên bản đồ!");
  } catch (err) {
    console.warn("Lỗi SafeLocation:", err);
    triggerToast("Không thể lấy GPS. Bạn hãy chạm trực tiếp trên bản đồ để chọn điểm!");
  } finally {
    isLocating.value = false;
  }
};

const searchLocationOnMap = async () => {
  const rawQ = mapSearchQuery.value.trim();
  if (!rawQ) {
    triggerToast("Vui lòng nhập tên địa điểm hoặc đường phố cần tìm!");
    return;
  }

  const cleanQ = removeVietnameseTones(rawQ);

  const matchedLocal = VIETNAM_LANDMARKS.find(item => {
    const itemClean = removeVietnameseTones(item.name);
    if (itemClean.includes(cleanQ)) return true;
    return item.keywords.some(k => {
      const kClean = removeVietnameseTones(k);
      return cleanQ.includes(kClean) || kClean.includes(cleanQ);
    });
  });

  if (matchedLocal) {
    if (mapInstance) mapInstance.flyTo([matchedLocal.lat, matchedLocal.lng], 16);
    if (markerInstance) markerInstance.setLatLng([matchedLocal.lat, matchedLocal.lng]);
    editForm.value.lat = matchedLocal.lat;
    editForm.value.lng = matchedLocal.lng;
    editForm.value.address = matchedLocal.name;
    triggerToast(`Đã tìm thấy: ${matchedLocal.name}`);
    return;
  }

  try {
    const esriSearchUrl = `https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates?f=pjson&singleLine=${encodeURIComponent(rawQ + ", Hà Nội")}&maxLocations=5`;
    const res = await fetch(esriSearchUrl, { signal: AbortSignal.timeout(4500) });
    if (res.ok) {
      const data = await res.json();
      if (data.candidates && data.candidates.length > 0) {
        const top = data.candidates[0];
        const lat = top.location.y;
        const lng = top.location.x;
        const cleanAddr = top.address.replace(/,s*(VNM|Vietnam|Việt Nam)$/i, "").trim();

        if (mapInstance) mapInstance.flyTo([lat, lng], 16);
        if (markerInstance) markerInstance.setLatLng([lat, lng]);
        editForm.value.lat = lat;
        editForm.value.lng = lng;
        editForm.value.address = cleanAddr;
        triggerToast(`Đã tìm thấy: ${cleanAddr}`);
        return;
      }
    }
  } catch (e) {}

  triggerToast(`Không tìm thấy "${rawQ}". Bạn hãy gõ tên quận/trường học hoặc chạm trên bản đồ!`);
};

const selectDistrict = (d: typeof VIETNAM_LANDMARKS[0]) => {
  editForm.value.lat = d.lat;
  editForm.value.lng = d.lng;
  editForm.value.address = d.name;
  if (mapInstance) mapInstance.flyTo([d.lat, d.lng], 16);
  if (markerInstance) markerInstance.setLatLng([d.lat, d.lng]);
  triggerToast(`Đã chọn: ${d.name}`);
};

// ================================================================
// QUẢN LÝ THANH TOÁN VIETINBANK (101882796072 - DOAN HOANG HUY)
// XỬ LÝ TRẠNG THÁI:
// 1. "Hệ thống đang xử lý... Chưa nhận được tiền"
// 2. Chỉ khi nhận được tiền từ API / Webhook -> Bật Nút Tích Xanh!
// ================================================================

const generatePaymentCode = () => {
  const num = Math.floor(100000 + Math.random() * 900000);
  return "ZM" + num;
};
const orderPaymentCode = ref(generatePaymentCode());

const vietinBankConfig = ref({
  bankId: "TPB",
  accountNo: "28122068866",
  accountName: "DOAN HOANG HUY",
  template: "qr_only"
});

// Trạng thái thanh toán: 'IDLE' | 'PROCESSING' | 'SUCCESS'
const paymentState = ref<"IDLE" | "PROCESSING" | "SUCCESS">("IDLE");
const autoRedirectTimer = ref(3);
let redirectInterval: any = null;
let pollTimer: any = null;
let broadcastChannel: BroadcastChannel | null = null;

// Sinh URL VietQR thuần túy chuẩn xác
const vietQrImageUrl = computed(() => {
  const amount = finalTotal.value;
  const memo = encodeURIComponent(orderPaymentCode.value);
  const accName = encodeURIComponent(vietinBankConfig.value.accountName);
  return `https://img.vietqr.io/image/${vietinBankConfig.value.bankId}-${vietinBankConfig.value.accountNo}-${vietinBankConfig.value.template}.png?amount=${amount}&addInfo=${memo}&accountName=${accName}`;
});

// Sao chép nhanh nội dung chuyển tiền
const copyApiValue = (text: string, label: string) => {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(text);
    triggerToast('Đã sao chép ' + label + ': ' + text);
  }
};

const copyMemo = () => {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(orderPaymentCode.value);
    triggerToast("Đã sao chép nội dung: " + orderPaymentCode.value);
  }
};

// Âm thanh chúc mừng thanh toán thành công
const playPaymentSuccessChime = () => {
  try {
    const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;

    const notes = [
      { freq: 523.25, time: 0, dur: 0.12 },
      { freq: 659.25, time: 0.12, dur: 0.12 },
      { freq: 783.99, time: 0.24, dur: 0.14 },
      { freq: 1046.50, time: 0.38, dur: 0.5 }
    ];

    notes.forEach(n => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = "triangle";
      osc.frequency.setValueAtTime(n.freq, now + n.time);
      gain.gain.setValueAtTime(0.25, now + n.time);
      gain.gain.exponentialRampToValueAtTime(0.001, now + n.time + n.dur);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start(now + n.time);
      osc.stop(now + n.time + n.dur);
    });
  } catch (e) {}
};

// Tự động tìm vị trí & tọa độ GPS thực tế của Gian Hàng / Người Bán
const resolveStoreDispatchLocation = (shopName: string) => {
  try {
    const clean = (shopName || "").trim().toLowerCase();
    
    // 1. Kiểm tra cache toàn bộ gian hàng đã đăng ký có GPS
    const allStores = JSON.parse(localStorage.getItem("zonemart_all_registered_stores") || "{}");
    if (allStores[clean]) {
      const s = allStores[clean];
      return {
        lat: s.lat || 21.0345,
        lng: s.lng || 105.7960,
        name: s.name || shopName,
        address: s.address || "Hà Nội",
        phone: s.phone || "0988 123 456"
      };
    }

    // 2. Tìm trong danh sách seller stores
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i);
      if (k && k.startsWith("zonemart_seller_store_")) {
        const raw = localStorage.getItem(k);
        if (raw) {
          const parsed = JSON.parse(raw);
          if (parsed.name && (parsed.name.toLowerCase() === clean || clean.includes(parsed.name.toLowerCase()) || parsed.name.toLowerCase().includes(clean))) {
            return {
              lat: parsed.lat || 21.0345,
              lng: parsed.lng || 105.7960,
              name: parsed.name,
              address: parsed.address || "Hà Nội",
              phone: parsed.phone || "0988 123 456"
            };
          }
        }
      }
    }
  } catch (e) {}

  // 3. Mặc định dự phòng (Cầu Giấy, Hà Nội)
  return {
    lat: 21.0345,
    lng: 105.7960,
    name: shopName,
    address: "Số 48 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội",
    phone: "0988 123 456"
  };
};

// KHI TIỀN THỰC SỰ VÀO -> BẬT HIỆU ỨNG THÀNH CÔNG VÀ LƯU ĐƠN
const onPaymentConfirmed = (paidAmount?: number) => {
  if (!auth.isLoggedIn.value) {
    console.warn("Chưa đăng nhập tài khoản, không thể lưu đơn hàng.");
    return;
  }
  if (paymentState.value === "SUCCESS") return;
  paymentState.value = "SUCCESS";

  // Giải phóng tức thì toàn bộ kết nối mạng SSE, Polling, WebSocket để không làm nghẽn luồng tải trang
  if (pollTimer) {
    clearInterval(pollTimer);
    pollTimer = null;
  }
  if (sseSource) {
    try { sseSource.close(); } catch (e) {}
    sseSource = null;
  }
  if (vietQrSocket) {
    try { vietQrSocket.close(); } catch (e) {}
    vietQrSocket = null;
  }
  if (broadcastChannel) {
    try { broadcastChannel.close(); } catch (e) {}
    broadcastChannel = null;
  }

  const finalAmount = paidAmount || finalTotal.value;
  const orderId = orderPaymentCode.value;
  const now = new Date();
  const dateStr = now.toLocaleDateString("vi-VN") + " " + now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

  const firstShopName = selectedCartItems.value[0]?.shop || "Vườn Rau Ba Vì (Cầu Giấy)";
  const storeDispatch = resolveStoreDispatchLocation(firstShopName);

  const newOrder = {
    id: orderId,
    date: dateStr,
    total: finalAmount,
    status: "Đã thanh toán",
    store: storeDispatch,
    itemsCount: cart.selectedItemsCount.value,
    items: selectedCartItems.value.map(item => ({
      name: item.name,
      price: item.price,
      quantity: item.quantity,
      image: item.image,
      shop: item.shop
    })),
    shippingAddress: shippingAddress.value,
    recipientName: buyerName.value,
    recipientPhone: buyerPhone.value,
    deliveryNote: deliveryNote.value,
    shippingMethod: isExpressSelected.value ? "Hỏa Tốc Siêu Tốc" : "Tiêu Chuẩn",
    paymentMethod: "Chuyển khoản QR",
    paidAt: new Date().toISOString(),
    location: currentCoords.value
  };

  const acc = auth.currentUser.value;
  const ownerKey = (acc?.phoneEmail || acc?.id || "guest").toLowerCase().trim();
  const orderKey = "zonemart_profile_orders_" + ownerKey;
  try {
    const existing = JSON.parse(localStorage.getItem(orderKey) || "[]");
    existing.unshift(newOrder);
    localStorage.setItem(orderKey, JSON.stringify(existing));
  } catch (e) {}

  // Trừ số lượng tồn kho của sản phẩm sau khi thanh toán thành công
  try {
    productModeration.reduceStockForPurchasedItems(selectedCartItems.value);
    const deductedOrders: string[] = JSON.parse(localStorage.getItem("zonemart_deducted_stock_orders") || "[]");
    if (!deductedOrders.includes(orderId)) {
      deductedOrders.push(orderId);
      localStorage.setItem("zonemart_deducted_stock_orders", JSON.stringify(deductedOrders));
    }
  } catch (err) {
    console.warn("Lỗi trừ tồn kho sản phẩm khi thanh toán thành công:", err);
  }

  // Phát tán sự kiện nổ đơn thời gian thực tới Shipper & Seller
  try {
    const firstShopName = selectedCartItems.value[0]?.shop || "Vườn Rau Ba Vì (Cầu Giấy)";
    orderRealtimeService.dispatchNewOrder({
      id: orderId,
      orderCode: orderId,
      total: 0, // Khách quét mã QR thanh toán thành công -> Nổ đơn bên gian hàng là 0đ
      shippingFee: shippingFee.value || 5000,
      paymentMethod: "Chuyển khoản QR",
      createdAt: dateStr,
      status: "pending",
      statusText: "Đã thanh toán • Chờ quán xác nhận",
      customer: {
        lat: currentCoords.value?.lat || 21.0333,
        lng: currentCoords.value?.lng || 105.7983,
        name: buyerName.value,
        phone: buyerPhone.value,
        address: shippingAddress.value
      },
      store: resolveStoreDispatchLocation(firstShopName),
      items: selectedCartItems.value.map(item => ({
        id: item.id,
        name: item.name,
        price: item.price,
        quantity: item.quantity,
        image: item.image,
        shop: item.shop
      }))
    });
  } catch (e) {
    console.warn("Lỗi phát đơn thời gian thực:", e);
  }

  const profileKey = "zonemart_profile_data_" + ownerKey;
  try {
    const profile = JSON.parse(localStorage.getItem(profileKey) || "{}");
    const earnedPoints = Math.round(finalAmount / 1000);
    profile.points = (profile.points || 0) + earnedPoints;
    localStorage.setItem(profileKey, JSON.stringify(profile));
  } catch (e) {}

  cart.clearCart();
  playPaymentSuccessChime();
  triggerToast("🎉 Tiền đã vào tài khoản TPBank! Thanh toán thành công.");

  autoRedirectTimer.value = 1;
  if (redirectInterval) clearInterval(redirectInterval);
  redirectInterval = setInterval(() => {
    autoRedirectTimer.value--;
    if (autoRedirectTimer.value <= 0) {
      if (redirectInterval) {
        clearInterval(redirectInterval);
        redirectInterval = null;
      }
      goToBuyerOrders();
    }
  }, 1000);
};

// Chuyển tới trang Đơn hàng của tôi
const goToBuyerOrders = () => {
  if (redirectInterval) {
    clearInterval(redirectInterval);
    redirectInterval = null;
  }
  if (pollTimer) {
    clearInterval(pollTimer);
    pollTimer = null;
  }
  if (sseSource) {
    try { sseSource.close(); } catch (e) {}
    sseSource = null;
  }
  if (vietQrSocket) {
    try { vietQrSocket.close(); } catch (e) {}
    vietQrSocket = null;
  }
  if (broadcastChannel) {
    try { broadcastChannel.close(); } catch (e) {}
    broadcastChannel = null;
  }

  // Chuyển hướng ngay tức thì và dự phòng tự động
  const safetyFallback = setTimeout(() => {
    if (window.location.pathname !== "/buyer-orders") {
      window.location.replace("/buyer-orders");
    }
  }, 350);

  router.replace("/buyer-orders")
    .then(() => {
      clearTimeout(safetyFallback);
    })
    .catch(() => {
      clearTimeout(safetyFallback);
      window.location.replace("/buyer-orders");
    });
};

// Tự động kết nối WebSocket VietQR trực tiếp từ trình duyệt
let vietQrSocket: WebSocket | null = null;
const initVietQrSocket = () => {
  try {
    const wsUrl = "wss://api.vietqr.org/vqr/socket?clientId=customer-zonemart-user26685";
    vietQrSocket = new WebSocket(wsUrl);
    vietQrSocket.onopen = () => {
      console.log(">>> [BROWSER VIETQR WS CONNECTED] Đang lắng nghe tiền về tự động...");
    };
    vietQrSocket.onmessage = (event) => {
      try {
        const data = typeof event.data === "string" ? JSON.parse(event.data) : event.data;
        console.log(">>> [BROWSER VIETQR WS RECEIVED]:", data);
        const rawText = `${data.orderId || ""} ${data.content || ""} ${data.addInfo || ""} ${data.description || ""}`.toUpperCase();
        if (rawText.includes(orderPaymentCode.value.toUpperCase()) || data.orderId === orderPaymentCode.value) {
          onPaymentConfirmed(data.amount ? Number(data.amount) : undefined);
        }
      } catch (err) {}
    };
    vietQrSocket.onclose = () => {
      if (paymentState.value !== "SUCCESS") {
        setTimeout(initVietQrSocket, 2500);
      }
    };
    vietQrSocket.onerror = () => {
      try { vietQrSocket?.close(); } catch (e) {}
    };
  } catch (e) {}
};

// Tự động kết nối Server-Sent Events (SSE) để bắt biến động số dư tức thì trong 0.1s
let sseSource: EventSource | null = null;
const initPaymentSSE = () => {
  try {
    if (typeof EventSource !== "undefined") {
      if (sseSource) sseSource.close();
      const sseUrl = getApiUrl(`/api/payment/events?code=${encodeURIComponent(orderPaymentCode.value)}`);
      sseSource = new EventSource(sseUrl);
      sseSource.onmessage = (e) => {
        try {
          const data = JSON.parse(e.data);
          const incomingCode = String(data?.code || "").toUpperCase();
          const targetCode = orderPaymentCode.value.toUpperCase();
          const isCodeMatch = incomingCode === targetCode || incomingCode.startsWith(targetCode) || targetCode.startsWith(incomingCode);
          if (data && data.paid && isCodeMatch) {
            console.log(">>> [SSE PAYMENT CONFIRMED]:", data);
            onPaymentConfirmed(data.amount ? Number(data.amount) : undefined);
          }
        } catch (err) {}
      };
      sseSource.onerror = () => {
        try { sseSource?.close(); } catch (e) {}
      };
    }
  } catch (e) {}
};

const syncOrderDetailsToServer = async () => {
  try {
    const items = selectedCartItems.value.map(i => ({
      name: i.name,
      price: i.price,
      quantity: i.quantity,
      image: i.image,
      shop: i.shop,
    }));
    await apiFetch('/api/payment/register-order', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        code: orderPaymentCode.value,
        total: finalTotal.value,
        buyerName: buyerName.value,
        items,
      }),
    });
  } catch (e) {}
};

const startPaymentListener = () => {
  initPaymentSSE();
  syncOrderDetailsToServer();

  try {
    if (window.BroadcastChannel) {
      broadcastChannel = new BroadcastChannel("zonemart_vietqr_payment");
      broadcastChannel.onmessage = (event) => {
        const data = event.data;
        if (data && (data.content?.includes(orderPaymentCode.value) || data.orderId === orderPaymentCode.value)) {
          onPaymentConfirmed(data.amount);
        }
      };
    }
  } catch (e) {}

  window.addEventListener("message", (event) => {
    if (event.data?.type === "TINGO_PAY_SUCCESS" && event.data?.code === orderPaymentCode.value) {
      onPaymentConfirmed(event.data?.amount);
    }
  });

  window.addEventListener("storage", (event) => {
    if (event.key === "zonemart_incoming_transaction") {
      try {
        const tx = JSON.parse(event.newValue || "{}");
        if (tx && (tx.content?.includes(orderPaymentCode.value) || tx.code === orderPaymentCode.value)) {
          onPaymentConfirmed(tx.amount);
        }
      } catch (e) {}
    }
  });

  if (pollTimer) clearInterval(pollTimer);
  pollTimer = setInterval(async () => {
    if (paymentState.value === "SUCCESS") {
      clearInterval(pollTimer);
      return;
    }
    // Đối soát tự động qua API Server thời gian thực (yêu cầu khớp chính xác mã đơn)
    try {
      const checkUrl = `/api/payment/check?code=${encodeURIComponent(orderPaymentCode.value)}&amount=${finalTotal.value}&bankAccount=${encodeURIComponent(vietinBankConfig.value.accountNo)}`;
      const res = await apiFetch(checkUrl);
      if (res.ok) {
        const data = await res.json();
        const incomingCode = String(data?.code || "").toUpperCase();
        const targetCode = orderPaymentCode.value.toUpperCase();
        const isCodeMatch = incomingCode === targetCode || incomingCode.startsWith(targetCode) || targetCode.startsWith(incomingCode);
        if (data && data.paid && isCodeMatch) {
          onPaymentConfirmed(data.amount);
          return;
        }
      }
    } catch (e) {}

    const storedStatus = localStorage.getItem("vietqr_paid_" + orderPaymentCode.value);
    if (storedStatus) {
      onPaymentConfirmed();
    }
  }, 1000);
};

// Hệ thống tự động lắng nghe và đối soát 100% qua SSE và Polling /api/payment/check

// Hệ thống tự động lắng nghe và đối soát 100% qua SSE và Polling /api/payment/check
// Đã kết nối API thật - Tự động đối soát qua Webhook /api/payment/check

// Modal giải thích & hướng dẫn kết nối API Tingo Pay
const showApiHelpModal = ref(false);

// Quản lý Sổ Địa Chỉ Giao Hàng & Tự Động Chọn Địa Chỉ Mặc Định
const getAddressBookKey = () => "zonemart_address_book_" + getAccountKey();

const saveAddressBookToStorage = () => {
  const key = getAddressBookKey();
  localStorage.setItem(key, JSON.stringify(savedAddresses.value));
};

const applyAddressToCheckout = (addr: SavedAddress) => {
  buyerName.value = addr.name;
  buyerPhone.value = addr.phone;
  shippingAddress.value = addr.address;
  deliveryNote.value = addr.note || "";
  currentCoords.value = { lat: addr.lat, lng: addr.lng };
  localStorage.setItem('zonemart_user_address', addr.address);
  localStorage.setItem('zonemart_lat', addr.lat.toString());
  localStorage.setItem('zonemart_lon', addr.lng.toString());
};

const loadAddressForCurrentAccount = () => {
  const bookKey = getAddressBookKey();
  try {
    const rawBook = localStorage.getItem(bookKey);
    if (rawBook) {
      const list: SavedAddress[] = JSON.parse(rawBook);
      if (Array.isArray(list) && list.length > 0) {
        savedAddresses.value = list;
        // Tự động chọn địa chỉ mặc định (hoặc địa chỉ đầu tiên)
        const defaultAddr = list.find(a => a.isDefault) || list[0];
        applyAddressToCheckout(defaultAddr);
        return;
      }
    }
  } catch (e) {}

  // Fallback từ localStorage đơn lẻ cũ hoặc thông tin user
  const oldKey = ADDRESS_STORAGE_PREFIX + getAccountKey();
  let initialName = "";
  let initialPhone = "";
  let initialAddr = "";
  let initialNote = "";
  let initialLat = 21.0215;
  let initialLng = 105.7497;

  try {
    const saved = localStorage.getItem(oldKey);
    if (saved) {
      const parsed = JSON.parse(saved);
      if (parsed.name) initialName = parsed.name;
      if (parsed.phone) initialPhone = parsed.phone;
      if (parsed.address) initialAddr = parsed.address;
      if (parsed.note) initialNote = parsed.note;
      if (parsed.lat && parsed.lng) {
        initialLat = parsed.lat;
        initialLng = parsed.lng;
      }
    }
  } catch (e) {}

  const u = auth.currentUser.value;
  if (!initialName) initialName = u?.fullName || u?.phoneEmail?.split("@")[0] || "Khách Hàng ZoneMart";
  if (!initialPhone) initialPhone = u?.phone || (u?.phoneEmail && /^0\d+$/.test(u.phoneEmail) ? u.phoneEmail : "0396222614");
  if (!initialAddr) initialAddr = localStorage.getItem('zonemart_user_address') || localStorage.getItem('zonemart_gps_address') || (u && (u as any).address) || "Xuân Phương, Nam Từ Liêm, Hà Nội";
  const userLat = localStorage.getItem('zonemart_lat');
  const userLon = localStorage.getItem('zonemart_lon');
  if (userLat && userLon) {
    initialLat = parseFloat(userLat);
    initialLng = parseFloat(userLon);
  }

  const initialSaved: SavedAddress = {
    id: "addr_" + Date.now(),
    name: initialName,
    phone: initialPhone,
    address: initialAddr,
    note: initialNote,
    lat: initialLat,
    lng: initialLng,
    label: "Nhà riêng",
    isDefault: true
  };

  savedAddresses.value = [initialSaved];
  saveAddressBookToStorage();
  applyAddressToCheckout(initialSaved);
};

const openAddressList = () => {
  showAddressListModal.value = true;
};

const selectSavedAddress = (addr: SavedAddress) => {
  applyAddressToCheckout(addr);
  showAddressListModal.value = false;
  triggerToast(`Đã chọn địa chỉ giao hàng: ${addr.address}`);
};

const setAsDefaultAddress = (addrId: string) => {
  savedAddresses.value.forEach(a => {
    a.isDefault = (a.id === addrId);
  });
  saveAddressBookToStorage();
  const defaultAddr = savedAddresses.value.find(a => a.id === addrId);
  if (defaultAddr) {
    applyAddressToCheckout(defaultAddr);
  }
  triggerToast("Đã thiết lập làm địa chỉ giao hàng mặc định!");
};

const deleteSavedAddress = (addrId: string) => {
  if (savedAddresses.value.length <= 1) {
    toast.warning("Bạn cần giữ lại ít nhất 1 địa chỉ giao hàng!", "Không thể xóa");
    return;
  }
  const target = savedAddresses.value.find(a => a.id === addrId);
  const wasDefault = target?.isDefault;
  savedAddresses.value = savedAddresses.value.filter(a => a.id !== addrId);
  if (wasDefault && savedAddresses.value.length > 0) {
    savedAddresses.value[0].isDefault = true;
    applyAddressToCheckout(savedAddresses.value[0]);
  }
  saveAddressBookToStorage();
  triggerToast("Đã xóa địa chỉ thành công!");
};

const openAddNewAddress = () => {
  isEditingNew.value = true;
  editingAddressId.value = null;
  isSetAsDefault.value = savedAddresses.value.length === 0;
  addressLabel.value = "Nhà riêng";
  editForm.value = {
    name: buyerName.value || "",
    phone: buyerPhone.value || "",
    address: "",
    note: "",
    lat: currentCoords.value?.lat || 21.0215,
    lng: currentCoords.value?.lng || 105.7497
  };
  showAddressListModal.value = false;
  showEditAddressModal.value = true;
  initOrUpdateMap();
};

const openEditExistingAddress = (addr: SavedAddress) => {
  isEditingNew.value = false;
  editingAddressId.value = addr.id;
  isSetAsDefault.value = addr.isDefault;
  addressLabel.value = (addr.label as any) || "Nhà riêng";
  editForm.value = {
    name: addr.name,
    phone: addr.phone,
    address: addr.address,
    note: addr.note || "",
    lat: addr.lat,
    lng: addr.lng
  };
  showAddressListModal.value = false;
  showEditAddressModal.value = true;
  initOrUpdateMap();
};

onMounted(() => {
  if (!auth.isLoggedIn.value) {
    toast.warning("Vui lòng đăng nhập tài khoản để thanh toán đơn hàng!", "Yêu Cầu Đăng Nhập");
    router.replace({
      path: "/login",
      query: {
        redirect: "/checkout",
        reason: "auth_required"
      }
    });
    return;
  }
  loadAddressForCurrentAccount();
  initVietQrSocket();
  startPaymentListener();

  // Tải trước (Prefetch) trang Đơn Hàng để chuyển trang tức thì khi thanh toán thành công
  import("../../views/thang/BuyerOrdersView.vue").catch(() => {});
});

onUnmounted(() => {
  if (pollTimer) clearInterval(pollTimer);
  if (redirectInterval) clearInterval(redirectInterval);
  if (broadcastChannel) broadcastChannel.close();
  if (sseSource) {
    try { sseSource.close(); } catch (e) {}
  }
  if (vietQrSocket) {
    try { vietQrSocket.close(); } catch (e) {}
  }
});

watch(
  () => auth.currentUser.value,
  () => {
    loadAddressForCurrentAccount();
  }
);

const openEditAddress = () => {
  // Mở sửa địa chỉ hiện tại đang chọn
  const cur = currentSelectedAddress.value;
  if (cur) {
    openEditExistingAddress(cur);
  } else {
    openAddNewAddress();
  }
};

const closeEditAddress = () => {
  showEditAddressModal.value = false;
};

const saveAddress = () => {
  const trimmedName = editForm.value.name.trim();
  const trimmedPhone = editForm.value.phone.trim();
  const trimmedAddress = editForm.value.address.trim();

  if (!trimmedName) {
    toast.warning("Vui lòng nhập họ và tên người nhận hàng!", "Thiếu Họ Tên");
    return;
  }
  if (!trimmedPhone || trimmedPhone.length < 9) {
    toast.warning("Vui lòng nhập số điện thoại người nhận hợp lệ!", "Số Điện Thoại");
    return;
  }
  if (!trimmedAddress || trimmedAddress.length < 6) {
    toast.warning("Vui lòng định vị trên bản đồ hoặc nhập địa chỉ nhận hàng chi tiết!", "Thiếu Địa Chỉ");
    return;
  }

  // Nếu người dùng chọn đặt làm mặc định
  if (isSetAsDefault.value) {
    savedAddresses.value.forEach(a => a.isDefault = false);
  }

  let targetAddr: SavedAddress;

  if (isEditingNew.value || !editingAddressId.value) {
    targetAddr = {
      id: "addr_" + Date.now(),
      name: trimmedName,
      phone: trimmedPhone,
      address: trimmedAddress,
      note: editForm.value.note.trim(),
      lat: editForm.value.lat,
      lng: editForm.value.lng,
      label: addressLabel.value,
      isDefault: isSetAsDefault.value || savedAddresses.value.length === 0
    };
    savedAddresses.value.unshift(targetAddr);
  } else {
    const existing = savedAddresses.value.find(a => a.id === editingAddressId.value);
    if (existing) {
      existing.name = trimmedName;
      existing.phone = trimmedPhone;
      existing.address = trimmedAddress;
      existing.note = editForm.value.note.trim();
      existing.lat = editForm.value.lat;
      existing.lng = editForm.value.lng;
      existing.label = addressLabel.value;
      if (isSetAsDefault.value) existing.isDefault = true;
      targetAddr = existing;
    } else {
      targetAddr = {
        id: "addr_" + Date.now(),
        name: trimmedName,
        phone: trimmedPhone,
        address: trimmedAddress,
        note: editForm.value.note.trim(),
        lat: editForm.value.lat,
        lng: editForm.value.lng,
        label: addressLabel.value,
        isDefault: isSetAsDefault.value
      };
      savedAddresses.value.unshift(targetAddr);
    }
  }

  saveAddressBookToStorage();
  applyAddressToCheckout(targetAddr);

  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('zonemart:location-updated', {
      detail: { safeLocationName: targetAddr.address, lat: targetAddr.lat, lon: targetAddr.lng }
    }));
  }

  showEditAddressModal.value = false;
  triggerToast(targetAddr.isDefault ? "Đã lưu và đặt làm địa chỉ giao hàng mặc định!" : "Đã lưu địa chỉ giao hàng thành công!");
};

// Phí giao hàng & tổng tiền (Mô hình chợ dân sinh / đi chợ gần nhà - Bán kính <= 5km)
const shopCount = computed(() => cart.activeStoresCount.value || 1);
const isExpressSelected = ref(true);
const paymentMethod = ref<"COD" | "ONLINE_QR">("ONLINE_QR");

// Đạt mốc Freeship đi chợ từ 99.000 ₫
const isFreeship = computed(() => (cart.subTotal.value || 0) >= 99000);

const standardFee = computed(() => {
  return cart.cartStores.value
    .filter((store) => store.items.some((i) => i.selected))
    .reduce((sum, store) => {
      const feeInfo = cart.calculateStoreShippingFee(store, false);
      return sum + feeInfo.totalFee;
    }, 0) || 5000;
});

const expressFee = computed(() => {
  return cart.cartStores.value
    .filter((store) => store.items.some((i) => i.selected))
    .reduce((sum, store) => {
      const feeInfo = cart.calculateStoreShippingFee(store, true);
      return sum + feeInfo.totalFee;
    }, 0) || 8000;
});

const shippingFee = computed(() => {
  if (isFreeship.value) return 0;
  return isExpressSelected.value ? expressFee.value : standardFee.value;
});

const itemsTotal = computed(() => cart.subTotal.value || 0);
const finalTotal = computed(() => itemsTotal.value + shippingFee.value);

const selectedCartItems = computed(() => {
  return cart.cartStores.value.flatMap(store =>
    store.items.filter(item => item.selected).map(item => ({
      ...item,
      shop: store.storeName
    }))
  );
});

const handleSelectExpress = (val: boolean) => {
  isExpressSelected.value = val;
  if (val) paymentMethod.value = "ONLINE_QR";
};

// Đặt hàng COD
const handlePlaceOrder = () => {
  if (!auth.isLoggedIn.value) {
    toast.warning("Vui lòng đăng nhập tài khoản để tiến hành đặt hàng!", "Yêu Cầu Đăng Nhập");
    router.push({
      path: "/login",
      query: {
        redirect: "/checkout",
        reason: "auth_required"
      }
    });
    return;
  }
  if (selectedCartItems.value.length === 0) {
    toast.info("Bạn chưa chọn món nào để thanh toán! Vui lòng quay lại giỏ hàng và chọn món.", "Giỏ Hàng Rỗng");
    router.push("/cart");
    return;
  }

  if (paymentMethod.value === "ONLINE_QR") {
    triggerToast("Vui lòng mở ứng dụng Ngân hàng để quét mã QR bên cạnh!");
    return;
  }

  const orderId = orderPaymentCode.value;
  const now = new Date();
  const dateStr = now.toLocaleDateString("vi-VN") + " " + now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

  const firstShopName = selectedCartItems.value[0]?.shop || "Vườn Rau Ba Vì (Cầu Giấy)";
  const storeDispatch = resolveStoreDispatchLocation(firstShopName);

  const newOrder = {
    id: orderId,
    date: dateStr,
    total: finalTotal.value,
    status: "Chờ xác nhận (COD)",
    store: storeDispatch,
    itemsCount: cart.selectedItemsCount.value,
    items: selectedCartItems.value.map(item => ({
      name: item.name,
      price: item.price,
      quantity: item.quantity,
      image: item.image,
      shop: item.shop
    })),
    shippingAddress: shippingAddress.value,
    recipientName: buyerName.value,
    recipientPhone: buyerPhone.value,
    deliveryNote: deliveryNote.value,
    shippingMethod: "Tiêu Chuẩn",
    paymentMethod: "Tiền mặt COD",
    location: currentCoords.value
  };

  const acc = auth.currentUser.value;
  const ownerKey = (acc?.phoneEmail || acc?.id || "guest").toLowerCase().trim();
  const orderKey = "zonemart_profile_orders_" + ownerKey;
  try {
    const existing = JSON.parse(localStorage.getItem(orderKey) || "[]");
    existing.unshift(newOrder);
    localStorage.setItem(orderKey, JSON.stringify(existing));
  } catch (e) {}

  // Trừ số lượng tồn kho của sản phẩm khi đặt hàng COD
  try {
    productModeration.reduceStockForPurchasedItems(selectedCartItems.value);
    const deductedOrders: string[] = JSON.parse(localStorage.getItem("zonemart_deducted_stock_orders") || "[]");
    if (!deductedOrders.includes(orderId)) {
      deductedOrders.push(orderId);
      localStorage.setItem("zonemart_deducted_stock_orders", JSON.stringify(deductedOrders));
    }
  } catch (err) {
    console.warn("Lỗi trừ tồn kho sản phẩm khi đặt hàng COD:", err);
  }

  // Phát tán sự kiện nổ đơn thời gian thực tới Shipper & Seller
  try {
    const firstShopName = selectedCartItems.value[0]?.shop || "Vườn Rau Ba Vì (Cầu Giấy)";
    orderRealtimeService.dispatchNewOrder({
      id: orderId,
      orderCode: orderId,
      total: finalTotal.value,
      shippingFee: shippingFee.value || 5000,
      paymentMethod: "Tiền mặt COD",
      createdAt: dateStr,
      status: "pending",
      statusText: "Chờ quán xác nhận",
      customer: {
        lat: currentCoords.value?.lat || 21.0333,
        lng: currentCoords.value?.lng || 105.7983,
        name: buyerName.value,
        phone: buyerPhone.value,
        address: shippingAddress.value
      },
      store: resolveStoreDispatchLocation(firstShopName),
      items: selectedCartItems.value.map(item => ({
        id: item.id,
        name: item.name,
        price: item.price,
        quantity: item.quantity,
        image: item.image,
        shop: item.shop
      }))
    });
  } catch (e) {
    console.warn("Lỗi phát đơn thời gian thực COD:", e);
  }

  toast.success(`Đơn hàng #${orderId} đã được tạo thành công! Sẽ giao tới ${buyerName.value} (${buyerPhone.value}).`, "🎉 Đặt Hàng Thành Công", 5000);
  cart.clearCart();
  router.push("/buyer-orders");
};
</script>

<template>
  <div class="checkout-container">
    <!-- Toast Popup thông báo -->
    <transition name="toast-pop">
      <div v-if="showToast" class="checkout-toast">
        <i class="bi bi-check-circle-fill text-success"></i>
        <span>{{ toastMessage }}</span>
      </div>
    </transition>

    <div class="checkout-header">
      <h2>💳 Thanh Toán & Đặt Hàng</h2>
      <p>Kiểm tra địa chỉ, chọn phương thức giao hàng và quét mã chuyển khoản VietinBank.</p>
    </div>

    <div class="checkout-grid">
      <!-- Cột trái: Thông tin nhận hàng & Vận chuyển -->
      <div class="left-col">
        <!-- 1. ĐỊA CHỈ NHẬN HÀNG CÓ NÚT THAY ĐỔI VÀ ĐỊNH VỊ MAP -->
        <div class="card-box address-box">
          <div class="card-box-header">
            <h3>
              <i class="bi bi-geo-alt-fill text-danger me-2" aria-hidden="true"></i>
              1. Địa Chỉ Nhận Hàng
            </h3>
            <div class="addr-actions-wrap">
              <button
                type="button"
                class="btn-addr-book"
                @click="openAddressList"
                title="Quản lý sổ địa chỉ giao hàng"
              >
                <i class="bi bi-journal-bookmark me-1"></i> Sổ Địa Chỉ ({{ savedAddresses.length }})
              </button>
              <button
                type="button"
                class="btn-edit-addr"
                @click="openAddNewAddress"
                title="Thêm địa chỉ giao hàng mới"
              >
                <i class="bi bi-plus-circle me-1"></i> Thêm Địa Chỉ
              </button>
            </div>
          </div>

          <div class="addr-content">
            <div class="user-info-row">
              <span class="user-fullname">{{ buyerName }}</span>
              <span class="user-phone">({{ buyerPhone }})</span>
              <span v-if="currentSelectedAddress?.isDefault" class="badge-default-pill">
                <i class="bi bi-check2-circle"></i> Mặc định
              </span>
              <span v-if="currentSelectedAddress?.label" class="badge-label-pill">
                {{ currentSelectedAddress.label }}
              </span>
              <button type="button" class="btn-change-addr-link" @click="openEditAddress" title="Sửa hoặc định vị lại địa chỉ này">
                [Sửa / Định vị]
              </button>
              <button type="button" class="btn-change-addr-link" @click="openAddressList" title="Chọn từ danh sách địa chỉ đã lưu">
                [Đổi địa chỉ]
              </button>
            </div>

            <p class="addr-text">
              <i class="bi bi-geo-alt-fill text-danger me-1"></i>
              {{ shippingAddress }}
            </p>

            <div v-if="deliveryNote" class="delivery-note-tag">
              <i class="bi bi-card-text text-warning me-1"></i>
              <strong>Ghi chú:</strong> {{ deliveryNote }}
            </div>
          </div>
        </div>

        <!-- 2. PHƯƠNG THỨC VẬN CHUYỂN -->
        <div class="card-box">
          <h3>
            <i class="bi bi-truck text-primary me-2" aria-hidden="true"></i>
            2. Phương Thức Vận Chuyển
          </h3>
          <div class="shipping-options">
            <div
              class="opt-item"
              :class="{ selected: isExpressSelected }"
              @click="handleSelectExpress(true)"
            >
              <div class="opt-head">
                <strong>
                  <i class="bi bi-lightning-charge-fill text-warning me-1" aria-hidden="true"></i>
                  Giao Hỏa Tốc Đi Chợ (15-25 phút)
                </strong>
                <span class="price">
                  <span v-if="isFreeship" class="text-success">0 ₫ (Freeship)</span>
                  <span v-else>{{ expressFee.toLocaleString('vi-VN') }} ₫</span>
                </span>
              </div>
              <p class="sub">Nhận hàng trong 15-25 phút. <em>(Ưu tiên phục vụ, quét mã chuyển khoản tự động)</em></p>
            </div>

            <div
              class="opt-item"
              :class="{ selected: !isExpressSelected }"
              @click="handleSelectExpress(false)"
            >
              <div class="opt-head">
                <strong>
                  <i class="bi bi-bicycle text-success me-1" aria-hidden="true"></i>
                  Giao Tiêu Chuẩn Tiết Kiệm (30-45 phút)
                </strong>
                <span class="price">
                  <span v-if="isFreeship" class="text-success">0 ₫ (Freeship)</span>
                  <span v-else>{{ standardFee.toLocaleString('vi-VN') }} ₫</span>
                </span>
              </div>
              <p class="sub">Nhận hàng trong 30-45 phút. Hỗ trợ Tiền mặt COD và Chuyển khoản QR.</p>
            </div>
          </div>
        </div>

        <!-- 3. PHƯƠNG THỨC THANH TOÁN -->
        <div class="card-box">
          <h3>
            <i class="bi bi-credit-card-2-front text-warning me-2" aria-hidden="true"></i>
            3. Phương Thức Thanh Toán
          </h3>
          <div class="pay-options">
            <label class="pay-item" :class="{ active: paymentMethod === 'ONLINE_QR' }">
              <input type="radio" value="ONLINE_QR" v-model="paymentMethod" />
              <div>
                <strong>Chuyển Khoản QR Ngân Hàng (Tự Động Xác Nhận Khi Tiền Vào)</strong>
                <p>Quét mã qua app ngân hàng - Tiền vào hệ thống tự động xác nhận thành công ngay tại đây</p>
              </div>
            </label>

            <label class="pay-item" :class="{ active: paymentMethod === 'COD' }">
              <input type="radio" value="COD" v-model="paymentMethod" />
              <div>
                <strong>Tiền Mặt Khi Nhận Hàng (COD)</strong>
                <p>Thanh toán trực tiếp tiền mặt cho Shipper khi nhận món tận tay</p>
              </div>
            </label>
          </div>
        </div>
      </div>

      <!-- Cột phải: Chi tiết đơn hàng & Thanh toán QR -->
      <div class="right-col">
        <div class="card-box sticky-box">
          <h3>Chi Tiết Thanh Toán</h3>

          <div class="fee-row">
            <span>Tiền hàng ({{ shopCount }} quán):</span>
            <strong>{{ itemsTotal.toLocaleString("vi-VN") }} ₫</strong>
          </div>
          <div class="fee-row">
            <span>Phí giao hàng:</span>
            <strong>{{ shippingFee.toLocaleString("vi-VN") }} ₫</strong>
          </div>

          <div class="divider"></div>

          <div class="fee-row total-row">
            <span>Tổng cộng:</span>
            <strong class="total-price">{{ finalTotal.toLocaleString("vi-VN") }} ₫</strong>
          </div>

          <!-- KHUNG THANH TOÁN QR VIETINBANK (TỰ ĐỘNG HIỆN NÚT TÍCH KHI TIỀN VÀO) -->
          <div v-if="paymentMethod === 'ONLINE_QR'" class="qr-checkout-card">
            <!-- TRƯỜNG HỢP 1: CHƯA THANH TOÁN -> HIỆN MÃ QR & MỖI NỘI DUNG CHUYỂN TIỀN -->
            <template v-if="paymentState !== 'SUCCESS'">
              <!-- Trạng thái radar -->
              <div v-if="paymentState === 'IDLE'" class="payment-status-radar">
                <span class="radar-dot"></span>
                <span class="radar-text">Đang chờ quét mã & nhận tiền...</span>
              </div>

              <!-- Trạng thái: HỆ THỐNG ĐANG XỬ LÝ (KHI ĐANG ĐỐI SOÁT VỚI TƯƠNG TÁC TỪ KHÁCH) -->
              <div v-else-if="paymentState === 'PROCESSING'" class="payment-status-radar">
                <span class="radar-dot"></span>
                <span class="radar-text">Đang tự động đối soát giao dịch ngân hàng...</span>
              </div>

              <!-- HIỂN THỊ DANH SÁCH SẢN PHẨM THANH TOÁN (HÌNH ẢNH + TÊN) -->
              <div v-if="selectedCartItems && selectedCartItems.length > 0" class="qr-products-summary-card">
                <div class="summary-card-header">
                  <i class="bi bi-bag-check-fill text-primary me-1"></i>
                  <span>Sản phẩm thanh toán ({{ selectedCartItems.length }})</span>
                </div>
                <div class="summary-card-items">
                  <div v-for="item in selectedCartItems" :key="item.id" class="qr-product-item-row">
                    <img :src="item.image" :alt="item.name" class="qr-product-item-thumb" />
                    <div class="qr-product-item-info">
                      <div class="qr-product-item-name" :title="item.name">{{ item.name }}</div>
                      <div class="qr-product-item-meta">
                        <span class="qr-product-item-qty">x{{ item.quantity }}</span>
                        <span class="qr-product-item-price">{{ (item.price * item.quantity).toLocaleString('vi-VN') }} ₫</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Ảnh QR sạch qr_only (không logo, không chữ vietqr hay mb) -->
              <div class="qr-image-wrapper">
                <img
                  :src="vietQrImageUrl"
                  alt="Mã QR Chuyển Khoản"
                  class="vietqr-clean-img"
                />
              </div>

              <!-- CHỈ GIỮ LẠI MỖI NỘI DUNG CHUYỂN TIỀN THEO YÊU CẦU -->
              <div class="memo-only-box">
                <span class="memo-label">Nội dung chuyển tiền:</span>
                <div class="memo-copy-row">
                  <strong class="memo-code">{{ orderPaymentCode }}</strong>
                  <button type="button" class="btn-copy-memo" @click="copyMemo" title="Sao chép nội dung">
                    <i class="bi bi-clipboard me-1"></i> Sao chép
                  </button>
                </div>
              </div>

              <div class="qr-helper-text">
                <i class="bi bi-shield-check text-success me-1"></i>
                <span>Quét mã qua app ngân hàng, tiền vào hệ thống tự động xác nhận</span>
              </div>
            </template>

            <!-- TRƯỜNG HỢP 2: KHI TIỀN VÀO -> HIỆN HIỆU ỨNG ANIMATION NÚT TÍCH & BIÊN LAI XÁC NHẬN SANG XỊN -->
            <template v-else>
              <div class="qr-success-inline-box">
                <div class="success-animation-circle">
                  <svg class="checkmark-svg" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 52 52">
                    <circle class="checkmark-circle" cx="26" cy="26" r="25" fill="none"/>
                    <path class="checkmark-check" fill="none" d="M14.1 27.2l7.1 7.2 16.7-16.8"/>
                  </svg>
                </div>

                <h4 class="success-inline-title">Thanh Toán Thành Công!</h4>
                <p class="success-inline-desc">Đã nhận được tiền qua tài khoản TPBank</p>

                <!-- KHUNG TÓM TẮT SẢN PHẨM ĐÃ THANH TOÁN (HÌNH ẢNH + TÊN) -->
                <div v-if="selectedCartItems && selectedCartItems.length > 0" class="success-products-preview-box">
                  <div class="success-products-header">
                    <span class="sp-title"><i class="bi bi-box2-heart-fill text-danger me-1"></i> Món hàng đã thanh toán</span>
                    <span class="badge-item-count">{{ selectedCartItems.length }} món</span>
                  </div>
                  <div class="success-products-list">
                    <div v-for="item in selectedCartItems" :key="item.id" class="success-product-chip">
                      <img :src="item.image" :alt="item.name" class="success-product-thumb" />
                      <div class="success-product-details">
                        <span class="success-product-name">{{ item.name }}</span>
                        <div class="success-product-pricing">
                          <span class="success-product-qty">SL: {{ item.quantity }}</span>
                          <span class="success-product-price">{{ (item.price * item.quantity).toLocaleString('vi-VN') }} ₫</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- BIÊN LAI GIAO DỊCH TINH GỌN -->
                <div class="success-receipt-card">
                  <div class="receipt-row">
                    <span class="receipt-label">Mã giao dịch</span>
                    <span class="receipt-value memo-val">#{{ orderPaymentCode }}</span>
                  </div>
                  <div class="receipt-row">
                    <span class="receipt-label">Tổng tiền</span>
                    <span class="receipt-value amount-val">{{ finalTotal.toLocaleString('vi-VN') }} ₫</span>
                  </div>
                  <div class="receipt-row">
                    <span class="receipt-label">Ngân hàng</span>
                    <span class="receipt-value">TPBank (DOAN HOANG HUY)</span>
                  </div>
                </div>

                <div class="redirect-progress-bar">
                  <div class="progress-fill"></div>
                </div>
                <p class="redirect-sub">Tự động chuyển tới đơn hàng sau {{ autoRedirectTimer }}s...</p>

                <button type="button" class="btn-go-orders-inline" @click="goToBuyerOrders">
                  <i class="bi bi-bag-check-fill me-1"></i> Xem Chi Tiết Đơn Hàng
                </button>
              </div>
            </template>
          </div>

          <!-- TRẠNG THÁI TỰ ĐỘNG ĐỐI SOÁT 100% -->
          <div v-if="paymentMethod === 'ONLINE_QR' && paymentState !== 'SUCCESS'" class="auto-active-banner">
            <div class="auto-spinner"></div>
            <div>
              <div class="auto-title">ĐANG TỰ ĐỘNG LẮNG NGHE TÀI KHOẢN TPBANK</div>
              <div class="auto-desc">Bạn chỉ cần chuyển khoản theo mã QR. Tiền vào hệ thống sẽ tự động xác nhận và chuyển trang ngay tức thì!</div>
            </div>
          </div>

          <button
            v-else-if="paymentMethod === 'ONLINE_QR' && paymentState === 'SUCCESS'"
            class="btn btn-success-solid btn-block"
            @click="goToBuyerOrders"
          >
            <i class="bi bi-check2-circle me-2"></i> ĐÃ THANH TOÁN THÀNH CÔNG (XEM ĐƠN)
          </button>
          <button
            v-else
            class="btn btn-primary btn-block"
            @click="handlePlaceOrder"
          >
            <i class="bi bi-bag-check-fill me-2"></i> XÁC NHẬN ĐẶT HÀNG (COD)
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL HƯỚNG DẪN KẾT NỐI API TINGO PAY / VIETQR -->
    <div v-if="showApiHelpModal" class="modal-backdrop-overlay" @click.self="showApiHelpModal = false">
      <div class="modal-dialog-card modal-dialog-medium">
        <div class="modal-header-line">
          <div class="modal-title-wrap">
            <div class="modal-icon-badge">
              <i class="bi bi-cpu-fill"></i>
            </div>
            <div>
              <h4>Cách Kết Nối API Tingo Pay Nhận Tiền Tự Động</h4>
              <p>Giải thích cơ chế đồng bộ biến động số dư ngân hàng</p>
            </div>
          </div>
          <button type="button" class="btn-close-modal" @click="showApiHelpModal = false">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <div class="modal-body-form api-help-content">
          <div class="live-status-card">
            <div class="status-indicator-dot pulse"></div>
            <div>
              <div class="status-card-title">CỔNG API THANH TOÁN TỰ ĐỘNG ĐANG TRỰC TUYẾN</div>
              <div class="status-card-sub">Máy chủ đã sẵn sàng nhận Webhook biến động số dư TPBank (28122068866)</div>
            </div>
          </div>

          <div class="config-table-card">
            <h6 class="config-card-header"><i class="bi bi-sliders me-2 text-primary"></i>Thông tin cần điền vào trang Tingo Pay của bạn:</h6>
            
            <div class="config-item-row">
              <div class="config-label">1. Tên Merchant:</div>
              <div class="config-value-box">
                <code>ZONEMART</code>
                <button type="button" class="btn-copy-small" @click="copyApiValue('ZONEMART', 'Tên Merchant')">
                  <i class="bi bi-clipboard me-1"></i> Sao chép
                </button>
              </div>
            </div>

            <div class="config-item-row">
              <div class="config-label">2. Username khách hàng:</div>
              <div class="config-value-box">
                <code>zonemart</code>
                <button type="button" class="btn-copy-small" @click="copyApiValue('zonemart', 'Username')">
                  <i class="bi bi-clipboard me-1"></i> Sao chép
                </button>
              </div>
            </div>

            <div class="config-item-row">
              <div class="config-label">3. Password khách hàng:</div>
              <div class="config-value-box">
                <code>ZoneMart@2026</code>
                <button type="button" class="btn-copy-small" @click="copyApiValue('ZoneMart@2026', 'Password')">
                  <i class="bi bi-clipboard me-1"></i> Sao chép
                </button>
              </div>
            </div>

            <div class="config-item-row highlight-row">
              <div class="config-label">4. URL kết nối *:</div>
              <div class="config-value-box">
                <code class="url-text">https://monogram-dilute-tightly.ngrok-free.dev</code>
                <button type="button" class="btn-copy-small" @click="copyApiValue('https://monogram-dilute-tightly.ngrok-free.dev', 'URL kết nối')">
                  <i class="bi bi-clipboard me-1"></i> Sao chép
                </button>
              </div>
            </div>

            <div class="config-item-row highlight-row">
              <div class="config-label">5. URL Path:</div>
              <div class="config-value-box">
                <code class="url-text">/bank/api/transaction-sync</code>
                <button type="button" class="btn-copy-small" @click="copyApiValue('/bank/api/transaction-sync', 'URL Path')">
                  <i class="bi bi-clipboard me-1"></i> Sao chép
                </button>
                <span class="text-muted fst-italic" style="font-size: 11px;">(Hoặc để trống đều được)</span>
              </div>
            </div>
          </div>

          <div class="step-guide-item mt-3">
            <h6><i class="bi bi-check2-circle text-success me-2"></i>Quy trình Test Thật:</h6>
            <ol class="mb-0 ps-3">
              <li>Bạn điền các thông tin trên vào trang Tingo Pay đang mở rồi bấm <strong>Tiếp tục / Test Get Token</strong>.</li>
              <li>Dùng app ngân hàng bất kỳ quét mã QR trên màn hình thanh toán.</li>
              <li>Chuyển số tiền và đúng <strong>Nội dung: {{ orderPaymentCode }}</strong>.</li>
              <li>Khi tiền vào TPBank, Tingo Pay sẽ bắn tín hiệu về cổng API. Website ZoneMart sẽ tự động chuyển sang <strong>Nút Tích Xanh</strong> ngay lập tức!</li>
            </ol>
          </div>
        </div>

        <div class="modal-footer-line">
          <button type="button" class="btn-modal-save" @click="showApiHelpModal = false">
            Đã Hiểu
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL POPUP THAY ĐỔI ĐỊA CHỈ & ĐỊNH VỊ BẢN ĐỒ (MAP LOCATOR) -->
    <div v-if="showEditAddressModal" class="modal-backdrop-overlay" @click.self="closeEditAddress">
      <div class="modal-dialog-card modal-dialog-large">
        <div class="modal-header-line">
          <div class="modal-title-wrap">
            <div class="modal-icon-badge">
              <i class="bi bi-geo-alt-fill"></i>
            </div>
            <div>
              <h4>Định Vị & Thay Đổi Địa Chỉ Nhận Hàng</h4>
              <p>Chạm hoặc kéo ghim trên bản đồ để định vị điểm nhận hàng chính xác</p>
            </div>
          </div>
          <button type="button" class="btn-close-modal" @click="closeEditAddress" aria-label="Đóng">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <div class="modal-body-form">
          <div class="form-row-2">
            <div class="form-group">
              <label class="form-label">Họ và tên người nhận <span class="text-danger">*</span></label>
              <div class="input-with-icon">
                <i class="bi bi-person input-icon"></i>
                <input
                  type="text"
                  class="custom-input"
                  v-model="editForm.name"
                  placeholder="VD: Nguyễn Văn An"
                />
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">Số điện thoại liên hệ <span class="text-danger">*</span></label>
              <div class="input-with-icon">
                <i class="bi bi-telephone input-icon"></i>
                <input
                  type="tel"
                  class="custom-input"
                  v-model="editForm.phone"
                  placeholder="VD: 0987 654 321"
                />
              </div>
            </div>
          </div>

          <!-- LỰA CHỌN NHÃN ĐỊA CHỈ & THIẾT LẬP MẶC ĐỊNH -->
          <div class="form-row-2 label-and-default-row">
            <div class="form-group">
              <label class="form-label">Nhãn địa chỉ</label>
              <div class="label-choice-group">
                <button
                  type="button"
                  class="choice-btn"
                  :class="{ active: addressLabel === 'Nhà riêng' }"
                  @click="addressLabel = 'Nhà riêng'"
                >
                  <i class="bi bi-house-door-fill me-1"></i> Nhà riêng
                </button>
                <button
                  type="button"
                  class="choice-btn"
                  :class="{ active: addressLabel === 'Văn phòng' }"
                  @click="addressLabel = 'Văn phòng'"
                >
                  <i class="bi bi-building me-1"></i> Văn phòng
                </button>
              </div>
            </div>

            <div class="form-group d-flex align-items-center pt-2">
              <label class="custom-checkbox-wrap">
                <input
                  type="checkbox"
                  v-model="isSetAsDefault"
                  class="real-checkbox"
                />
                <span class="custom-checkmark"></span>
                <span class="checkbox-text-bold">
                  Đặt làm địa chỉ nhận hàng mặc định
                  <small class="d-block text-muted" style="font-size: 11px; font-weight: normal;">
                    Tự động chọn cho các lần thanh toán sau
                  </small>
                </span>
              </label>
            </div>
          </div>

          <!-- BẢN ĐỒ ĐỊNH VỊ -->
          <div class="map-section-block">
            <div class="map-toolbar-line">
              <div class="map-label-group">
                <span class="map-section-title">
                  <i class="bi bi-map-fill text-danger me-1"></i> Bản Đồ Định Vị
                </span>
                <span v-if="isGeocoding" class="geocoding-spinner">
                  <i class="bi bi-arrow-repeat spin-icon"></i> Đang tải địa chỉ...
                </span>
                <span v-else class="map-coords-badge">
                  <i class="bi bi-pin-map-fill me-1"></i> {{ editForm.lat.toFixed(4) }}, {{ editForm.lng.toFixed(4) }}
                </span>
              </div>

              <button
                type="button"
                class="btn-gps-locate"
                @click="locateUserCurrentPosition"
                :disabled="isLocating"
                title="Lấy vị trí GPS hiện tại và cập nhật địa chỉ ngay lập tức"
              >
                <i class="bi" :class="isLocating ? 'bi-arrow-repeat spin-icon' : 'bi-crosshair'"></i>
                <span>{{ isLocating ? 'Đang định vị...' : 'Vị Trí Của Tôi' }}</span>
              </button>
            </div>

            <div class="map-search-bar">
              <i class="bi bi-search search-icon"></i>
              <input
                type="text"
                v-model="mapSearchQuery"
                @keydown.enter.prevent="searchLocationOnMap"
                placeholder="Tìm nhanh: Tên trường học, toà nhà, đường phố, quận..."
                class="map-search-input"
              />
              <button type="button" class="btn-quick-search" @click="searchLocationOnMap">
                Tìm
              </button>
            </div>

            <div class="map-wrapper">
              <div id="checkoutAddressMap" class="checkout-map-element"></div>
              <div class="map-overlay-guide">
                <i class="bi bi-hand-index-thumb me-1"></i> Nhấp bản đồ hoặc kéo ghim đỏ để chọn vị trí
              </div>
            </div>

            <div class="quick-district-chips">
              <span class="hint-text">Gợi ý khu vực:</span>
              <button
                v-for="d in VIETNAM_LANDMARKS.slice(0, 10)"
                :key="d.name"
                type="button"
                class="district-chip"
                @click="selectDistrict(d)"
              >
                + {{ d.name.split(',')[0] }}
              </button>
            </div>
          </div>

          <div class="form-group">
            <div class="label-with-badge">
              <label class="form-label">
                Địa chỉ nhận hàng chi tiết <span class="text-danger">*</span>
              </label>
              <span class="auto-filled-badge">
                <i class="bi bi-geo-alt-fill text-danger me-1"></i> Cập nhật tự động theo bản đồ
              </span>
            </div>
            <textarea
              rows="3"
              class="custom-textarea highlight-textarea"
              v-model="editForm.address"
              placeholder="VD: Số 18, Ngõ 165 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội"
            ></textarea>
            <div class="hint-subtext">
              <i class="bi bi-info-circle me-1"></i> Bạn có thể bổ sung thêm số phòng, số nhà, tầng lầu cụ thể vào ô trên nếu cần.
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Ghi chú cho tài xế</label>
            <div class="input-with-icon">
              <i class="bi bi-chat-left-dots input-icon"></i>
              <input
                type="text"
                class="custom-input"
                v-model="editForm.note"
                placeholder="VD: Bấm chuông tầng 2, gửi bác bảo vệ"
              />
            </div>
          </div>
        </div>

        <div class="modal-footer-line">
          <button type="button" class="btn-modal-cancel" @click="closeEditAddress">
            Hủy Bỏ
          </button>
          <button type="button" class="btn-modal-save" @click="saveAddress">
            <i class="bi bi-check2-circle me-1"></i> Lưu Địa Chỉ Này
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL SỔ ĐỊA CHỈ GIAO HÀNG (ADDRESS BOOK MODAL) -->
    <div v-if="showAddressListModal" class="modal-backdrop-overlay" @click.self="showAddressListModal = false">
      <div class="modal-dialog-card modal-dialog-medium">
        <div class="modal-header-line">
          <div class="modal-title-wrap">
            <div class="modal-icon-badge">
              <i class="bi bi-journal-bookmark-fill"></i>
            </div>
            <div>
              <h4>Địa Chỉ Giao Hàng Của Bạn</h4>
              <p>Chọn địa chỉ nhận hàng hoặc thêm địa chỉ mới để tự động lưu mặc định</p>
            </div>
          </div>
          <button type="button" class="btn-close-modal" @click="showAddressListModal = false" aria-label="Đóng">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <div class="modal-body-list">
          <div v-if="savedAddresses.length === 0" class="empty-addr-box">
            <i class="bi bi-geo-alt"></i>
            <p>Chưa có địa chỉ nào được lưu. Hãy thêm địa chỉ đầu tiên của bạn!</p>
          </div>

          <div
            v-for="addr in savedAddresses"
            :key="addr.id"
            class="saved-addr-card"
            :class="{ active: addr.address === shippingAddress, 'is-default': addr.isDefault }"
            @click="selectSavedAddress(addr)"
          >
            <div class="addr-card-header">
              <div class="addr-card-name-row">
                <input
                  type="radio"
                  name="selected_delivery_addr"
                  :checked="addr.address === shippingAddress"
                  class="addr-radio"
                  @click.stop="selectSavedAddress(addr)"
                />
                <strong class="addr-person-name">{{ addr.name }}</strong>
                <span class="addr-person-phone">({{ addr.phone }})</span>
                <span v-if="addr.isDefault" class="badge-default-pill">
                  <i class="bi bi-check-circle-fill"></i> Mặc định
                </span>
                <span v-if="addr.label" class="badge-label-pill">
                  {{ addr.label }}
                </span>
              </div>
              <div class="addr-card-actions" @click.stop>
                <button
                  type="button"
                  class="btn-addr-action btn-edit-action"
                  @click="openEditExistingAddress(addr)"
                  title="Chỉnh sửa hoặc định vị lại"
                >
                  <i class="bi bi-pencil-square"></i> Sửa
                </button>
                <button
                  v-if="!addr.isDefault"
                  type="button"
                  class="btn-addr-action btn-setdefault-action"
                  @click="setAsDefaultAddress(addr.id)"
                  title="Đặt làm địa chỉ nhận hàng mặc định"
                >
                  <i class="bi bi-star"></i> Đặt mặc định
                </button>
                <button
                  v-if="savedAddresses.length > 1"
                  type="button"
                  class="btn-addr-action btn-delete-action"
                  @click="deleteSavedAddress(addr.id)"
                  title="Xóa địa chỉ này"
                >
                  <i class="bi bi-trash3"></i>
                </button>
              </div>
            </div>

            <p class="addr-card-detail">
              <i class="bi bi-geo-alt-fill text-danger me-1"></i> {{ addr.address }}
            </p>
            <div v-if="addr.note" class="addr-card-note">
              <i class="bi bi-card-text text-warning me-1"></i> Ghi chú: {{ addr.note }}
            </div>
          </div>
        </div>

        <div class="modal-footer-line d-flex justify-content-between align-items-center">
          <button type="button" class="btn-modal-cancel" @click="showAddressListModal = false">
            Đóng
          </button>
          <button type="button" class="btn-modal-save" @click="openAddNewAddress">
            <i class="bi bi-plus-circle me-1"></i> Thêm Địa Chỉ Mới
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.checkout-container {
  max-width: 1150px;
  margin: 30px auto 60px auto;
  padding: 0 20px;
}

.checkout-header h2 {
  margin: 0 0 6px 0;
  color: #0f172a;
  font-size: 24px;
  font-weight: 800;
}

.checkout-header p {
  margin: 0 0 26px 0;
  color: #64748b;
  font-size: 14px;
}

.checkout-grid {
  display: grid;
  grid-template-columns: 1.8fr 1fr;
  gap: 24px;
}

@media (max-width: 850px) {
  .checkout-grid {
    grid-template-columns: 1fr;
  }
}

.card-box {
  background: #ffffff;
  border-radius: 18px;
  border: 1px solid #e2e8f0;
  padding: 24px;
  margin-bottom: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

.card-box-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  border-bottom: 1px solid #f1f5f9;
  padding-bottom: 12px;
  margin-bottom: 16px;
}

.card-box-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
  color: #0f172a;
  flex: 1;
  min-width: 150px;
}

.btn-edit-addr {
  background: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
  font-size: 13px;
  font-weight: 700;
  padding: 7px 14px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  white-space: nowrap;
  flex-shrink: 0;
}

.btn-edit-addr:hover {
  background: #ea580c;
  color: #ffffff;
  border-color: #ea580c;
  box-shadow: 0 2px 8px rgba(234, 88, 12, 0.25);
}

.card-box h3 {
  margin: 0 0 16px 0;
  font-size: 16px;
  color: #0f172a;
  border-bottom: 1px solid #f1f5f9;
  padding-bottom: 12px;
}

.addr-content .user-info-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-bottom: 8px;
}

.user-fullname {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
}

.user-phone {
  font-size: 14px;
  font-weight: 600;
  color: #475569;
}

.badge-account-isolated {
  font-size: 11px;
  font-weight: 700;
  color: #047857;
  background: #ecfdf5;
  border: 1px solid #a7f3d0;
  padding: 2px 8px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.addr-text {
  font-size: 15px;
  color: #334155;
  line-height: 1.5;
  margin: 0 0 10px 0;
}

.delivery-note-tag {
  font-size: 13px;
  color: #854d0e;
  background: #fefce8;
  border: 1px solid #fef08a;
  padding: 6px 12px;
  border-radius: 8px;
  display: inline-block;
}

/* Shipping & Payment Options */
.shipping-options,
.pay-options {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.opt-item,
.pay-item {
  border: 1.5px solid #e2e8f0;
  border-radius: 14px;
  padding: 16px;
  cursor: pointer;
  background: #ffffff;
  transition: all 0.2s;
  box-sizing: border-box;
}

.pay-item {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.pay-item > div {
  flex: 1;
  min-width: 0;
}

.pay-item strong {
  display: block;
  font-size: 14px;
  line-height: 1.35;
  word-break: break-word;
}

.opt-item:hover,
.pay-item:hover {
  border-color: #fdba74;
}

.opt-item.selected,
.pay-item.active {
  border-color: #ea580c;
  background: #fff7ed;
}

.pay-item.disabled {
  opacity: 0.5;
  cursor: not-allowed;
  background: #f8fafc;
}

.opt-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 10px;
  margin-bottom: 4px;
}

.opt-head strong {
  flex: 1;
  min-width: 0;
  word-break: break-word;
}

.opt-head .price {
  font-weight: 800;
  color: #ea580c;
  font-size: 15px;
  white-space: nowrap !important;
  flex-shrink: 0;
  margin-left: auto;
}

.sub,
.pay-item p {
  margin: 0;
  font-size: 13px;
  color: #64748b;
}

.pay-item {
  display: flex;
  align-items: center;
  gap: 14px;
}

.pay-item input[type="radio"] {
  accent-color: #ea580c;
  width: 18px;
  height: 18px;
}

/* Right Col: Summary Sticky */
.sticky-box {
  position: sticky;
  top: 90px;
}

.fee-row {
  display: flex;
  justify-content: space-between;
  font-size: 14px;
  margin-bottom: 12px;
  color: #475569;
}

.divider {
  height: 1px;
  background: #e2e8f0;
  margin: 16px 0;
}

.total-row {
  font-size: 16px;
  font-weight: 700;
  color: #0f172a;
}

.total-price {
  font-size: 26px;
  font-weight: 800;
  color: #ea580c;
}

/* ============================================================================
   KHUNG QR CHUYỂN KHOẢN TINH GỌN & BIÊN LAI THANH TOÁN
   ============================================================================ */
.qr-checkout-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 20px;
  padding: 20px 18px;
  margin: 18px 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  box-shadow: 0 4px 20px -2px rgba(15, 23, 42, 0.05), 0 2px 6px -1px rgba(15, 23, 42, 0.02);
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
}

.payment-status-radar {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  font-weight: 600;
  color: #0d9488;
  background: #f0fdfa;
  border: 1px solid #ccfbf1;
  padding: 5px 14px;
  border-radius: 9999px;
  margin-bottom: 14px;
  letter-spacing: 0.1px;
}

.radar-dot {
  width: 8px;
  height: 8px;
  background: #0d9488;
  border-radius: 50%;
  box-shadow: 0 0 0 0 rgba(13, 148, 136, 0.6);
  animation: radar-pulse 1.8s infinite cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes radar-pulse {
  0% { box-shadow: 0 0 0 0 rgba(13, 148, 136, 0.6); }
  70% { box-shadow: 0 0 0 8px rgba(13, 148, 136, 0); }
  100% { box-shadow: 0 0 0 0 rgba(13, 148, 136, 0); }
}

.payment-status-processing {
  background: #fefce8;
  border: 1px solid #fef08a;
  color: #854d0e;
  font-size: 12px;
  font-weight: 600;
  padding: 8px 12px;
  border-radius: 10px;
  margin-bottom: 14px;
  line-height: 1.4;
  text-align: center;
}

.spin-icon {
  display: inline-block;
  animation: spin 1s infinite linear;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* THẺ TÓM TẮT SẢN PHẨM TRƯỚC KHI QUÉT QR */
.qr-products-summary-card {
  width: 100%;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 12px 14px;
  margin-bottom: 14px;
}

.summary-card-header {
  font-size: 11.5px;
  font-weight: 700;
  color: #475569;
  text-transform: uppercase;
  letter-spacing: 0.4px;
  display: flex;
  align-items: center;
  margin-bottom: 8px;
}

.summary-card-items {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 160px;
  overflow-y: auto;
  padding-right: 4px;
}

.summary-card-items::-webkit-scrollbar {
  width: 4px;
}

.summary-card-items::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 4px;
}

.qr-product-item-row {
  display: flex;
  align-items: center;
  gap: 10px;
  background: #ffffff;
  border: 1px solid #f1f5f9;
  border-radius: 10px;
  padding: 6px 10px;
  transition: all 0.15s ease;
}

.qr-product-item-row:hover {
  border-color: #e2e8f0;
  transform: translateY(-1px);
}

.qr-product-item-thumb {
  width: 38px;
  height: 38px;
  border-radius: 8px;
  object-fit: cover;
  flex-shrink: 0;
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
}

.qr-product-item-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.qr-product-item-name {
  font-size: 12px;
  font-weight: 600;
  color: #1e293b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.qr-product-item-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 11px;
}

.qr-product-item-qty {
  color: #64748b;
  font-weight: 500;
}

.qr-product-item-price {
  color: #ea580c;
  font-weight: 700;
}

/* KHUNG QR */
.qr-image-wrapper {
  background: #ffffff;
  padding: 14px;
  border-radius: 18px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 8px 24px -4px rgba(15, 23, 42, 0.06), 0 2px 6px -1px rgba(15, 23, 42, 0.02);
  margin-bottom: 14px;
  transition: transform 0.25s ease, box-shadow 0.25s ease;
}

.qr-image-wrapper:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 28px -4px rgba(15, 23, 42, 0.08);
}

.vietqr-clean-img {
  width: 195px;
  height: 195px;
  display: block;
  border-radius: 10px;
  object-fit: contain;
}

/* HỘP NỘI DUNG CHUYỂN TIỀN HIỆN ĐẠI */
.memo-only-box {
  width: 100%;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  margin-bottom: 12px;
  transition: border-color 0.2s, background-color 0.2s;
}

.memo-only-box:hover {
  background: #f1f5f9;
  border-color: #cbd5e1;
}

.memo-label {
  font-size: 11px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.6px;
}

.memo-copy-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.memo-code {
  font-size: 19px;
  font-weight: 800;
  color: #ea580c;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  letter-spacing: 1px;
}

.btn-copy-memo {
  background: #ffffff;
  border: 1px solid #fed7aa;
  color: #ea580c;
  font-size: 11.5px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 8px;
  cursor: pointer;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  transition: all 0.15s ease;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.btn-copy-memo:hover {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
  box-shadow: 0 2px 6px rgba(234, 88, 12, 0.25);
  transform: translateY(-1px);
}

.btn-copy-memo:active {
  transform: translateY(0);
}

.qr-helper-text {
  font-size: 11.5px;
  color: #64748b;
  text-align: center;
  line-height: 1.5;
  margin-bottom: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.api-guide-row {
  margin-bottom: 10px;
}

.btn-api-info {
  background: transparent;
  border: none;
  color: #0284c7;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;
}

.btn-api-info:hover {
  color: #0369a1;
}



/* ANIMATION NÚT TÍCH XANH & BIÊN LAI CAO CẤP */
.qr-success-inline-box {
  width: 100%;
  padding: 8px 4px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  animation: success-fade-in 0.4s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes success-fade-in {
  0% { opacity: 0; transform: scale(0.94); }
  100% { opacity: 1; transform: scale(1); }
}

.success-animation-circle {
  width: 72px;
  height: 72px;
  margin: 4px auto 12px auto;
  position: relative;
}

.checkmark-svg {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  display: block;
  stroke-width: 3.5;
  stroke: #16a34a;
  stroke-miterlimit: 10;
  box-shadow: inset 0 0 0 #16a34a;
  animation: checkmark-fill 0.4s ease-in-out 0.4s forwards, checkmark-scale 0.3s ease-in-out 0.9s both;
}

.checkmark-circle {
  stroke-dasharray: 166;
  stroke-dashoffset: 166;
  stroke-width: 3.5;
  stroke-miterlimit: 10;
  stroke: #16a34a;
  fill: #ecfdf5;
  animation: checkmark-stroke 0.6s cubic-bezier(0.65, 0, 0.45, 1) forwards;
}

.checkmark-check {
  transform-origin: 50% 50%;
  stroke-dasharray: 48;
  stroke-dashoffset: 48;
  stroke: #16a34a;
  stroke-width: 4;
  animation: checkmark-stroke 0.35s cubic-bezier(0.65, 0, 0.45, 1) 0.45s forwards;
}

@keyframes checkmark-stroke {
  100% { stroke-dashoffset: 0; }
}

@keyframes checkmark-scale {
  0%, 100% { transform: none; }
  50% { transform: scale3d(1.08, 1.08, 1); }
}

@keyframes checkmark-fill {
  100% { box-shadow: inset 0 0 0 38px #ecfdf5; }
}

.success-inline-title {
  margin: 4px 0 4px 0;
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
  letter-spacing: -0.02em;
}

.success-inline-desc {
  margin: 0 0 14px 0;
  font-size: 13px;
  color: #64748b;
}

/* KHUNG DANH SÁCH MÓN ĐÃ THANH TOÁN (CARD SUCCESS) */
.success-products-preview-box {
  width: 100%;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 12px 14px;
  margin-bottom: 14px;
  text-align: left;
}

.success-products-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.sp-title {
  font-size: 11.5px;
  font-weight: 700;
  color: #475569;
  text-transform: uppercase;
  letter-spacing: 0.4px;
  display: flex;
  align-items: center;
}

.badge-item-count {
  background: #e2e8f0;
  color: #334155;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 999px;
}

.success-products-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 140px;
  overflow-y: auto;
  padding-right: 4px;
}

.success-products-list::-webkit-scrollbar {
  width: 4px;
}

.success-products-list::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 4px;
}

.success-product-chip {
  display: flex;
  align-items: center;
  gap: 10px;
  background: #ffffff;
  border: 1px solid #f1f5f9;
  border-radius: 10px;
  padding: 6px 10px;
}

.success-product-thumb {
  width: 40px;
  height: 40px;
  border-radius: 8px;
  object-fit: cover;
  flex-shrink: 0;
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
}

.success-product-details {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.success-product-name {
  font-size: 12.5px;
  font-weight: 600;
  color: #0f172a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.success-product-pricing {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 11.5px;
}

.success-product-qty {
  color: #64748b;
  font-weight: 500;
}

.success-product-price {
  color: #16a34a;
  font-weight: 700;
}

/* BIÊN LAI GIAO DỊCH */
.success-receipt-card {
  width: 100%;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 12px 14px;
  margin-bottom: 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.receipt-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12px;
  padding-bottom: 6px;
  border-bottom: 1px dashed #f1f5f9;
}

.receipt-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.receipt-label {
  color: #64748b;
  font-weight: 500;
}

.receipt-value {
  color: #0f172a;
  font-weight: 600;
}

.receipt-value.memo-val {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-weight: 800;
  color: #ea580c;
}

.receipt-value.amount-val {
  font-size: 14px;
  font-weight: 800;
  color: #16a34a;
}

.redirect-progress-bar {
  width: 100%;
  height: 4px;
  background: #e2e8f0;
  border-radius: 999px;
  overflow: hidden;
  margin-bottom: 6px;
}

.progress-fill {
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, #10b981, #059669);
  animation: progress-countdown 3s linear forwards;
}

@keyframes progress-countdown {
  0% { width: 100%; }
  100% { width: 0%; }
}

.redirect-sub {
  margin: 0 0 14px 0;
  font-size: 12px;
  color: #64748b;
  font-weight: 600;
}

.btn-go-orders-inline {
  background: #0f172a;
  color: #ffffff;
  border: none;
  width: 100%;
  padding: 12px;
  border-radius: 12px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.15);
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.btn-go-orders-inline:hover {
  background: #1e293b;
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.2);
}

.btn {
  border: none;
  cursor: pointer;
  padding: 14px;
  border-radius: 12px;
  font-weight: 700;
  font-size: 15px;
  transition: all 0.2s;
}

.btn-primary {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.3);
}

.btn-primary:hover {
  background: linear-gradient(135deg, #c2410c 0%, #9a3412 100%);
  transform: translateY(-1px);
  box-shadow: 0 6px 18px rgba(234, 88, 12, 0.4);
}

.btn-pay-now {
  background: linear-gradient(135deg, #16a34a 0%, #15803d 100%);
  box-shadow: 0 4px 14px rgba(22, 163, 74, 0.3);
}

.btn-pay-now:hover {
  background: linear-gradient(135deg, #15803d 0%, #166534 100%);
  box-shadow: 0 6px 18px rgba(22, 163, 74, 0.4);
}

.btn-success-solid {
  background: #16a34a;
  color: #ffffff;
}

.btn-block {
  width: 100%;
}

/* API HELP CONTENT */
.modal-dialog-medium {
  max-width: 580px;
}

.api-help-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
  font-size: 13.5px;
  line-height: 1.55;
  color: #334155;
}

.help-box-callout {
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 12px;
  padding: 14px;
}

.help-box-callout h5 {
  margin: 0 0 6px 0;
  color: #1e40af;
  font-size: 15px;
  font-weight: 700;
}

.help-box-callout p {
  margin: 0;
  color: #1e3a8a;
}

.step-guide-item h6 {
  margin: 0 0 6px 0;
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
}

.step-guide-item p {
  margin: 0 0 6px 0;
}

.step-guide-item ul {
  margin: 0;
  padding-left: 20px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.step-guide-item code {
  background: #f1f5f9;
  color: #ea580c;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 12px;
}

/* MODALS CHUNG */
.modal-backdrop-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1050;
  padding: 16px;
}

.modal-dialog-card {
  background: #ffffff;
  width: 100%;
  max-width: 700px;
  max-height: 94vh;
  border-radius: 20px;
  box-shadow: 0 20px 40px -10px rgba(0, 0, 0, 0.25);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  animation: modal-pop 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modal-pop {
  0% { opacity: 0; transform: scale(0.95) translateY(10px); }
  100% { opacity: 1; transform: scale(1) translateY(0); }
}

.modal-header-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 18px 24px;
  border-bottom: 1px solid #f1f5f9;
  background: #faf7f2;
  flex-shrink: 0;
}

.modal-title-wrap {
  display: flex;
  align-items: center;
  gap: 14px;
}

.modal-icon-badge {
  width: 44px;
  height: 44px;
  background: #fff7ed;
  color: #ea580c;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
}

.modal-title-wrap h4 {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
}

.modal-title-wrap p {
  margin: 2px 0 0 0;
  font-size: 12px;
  color: #64748b;
}

.btn-close-modal {
  width: 34px;
  height: 34px;
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 18px;
  cursor: pointer;
  border-radius: 8px;
  transition: all 0.15s;
}

.btn-close-modal:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.modal-body-form {
  padding: 18px 24px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  overflow-y: auto;
}

.form-row-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

@media (max-width: 580px) {
  .form-row-2 { grid-template-columns: 1fr; }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.label-with-badge {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
}

.auto-filled-badge {
  font-size: 11px;
  font-weight: 700;
  color: #ea580c;
  background: #fff7ed;
  padding: 2px 8px;
  border-radius: 6px;
  border: 1px solid #fed7aa;
}

.form-label {
  font-size: 13px;
  font-weight: 700;
  color: #334155;
}

.hint-subtext {
  font-size: 12px;
  color: #64748b;
}

.input-with-icon {
  position: relative;
  display: flex;
  align-items: center;
}

.input-icon {
  position: absolute;
  left: 12px;
  color: #94a3b8;
  font-size: 14px;
}

.custom-input {
  width: 100%;
  height: 40px;
  padding: 0 12px 0 36px;
  border: 1.5px solid #cbd5e1;
  border-radius: 10px;
  font-size: 14px;
  color: #0f172a;
  outline: none;
  transition: border-color 0.2s;
}

.custom-input:focus {
  border-color: #ea580c;
}

.custom-textarea {
  width: 100%;
  padding: 10px 12px;
  border: 1.5px solid #cbd5e1;
  border-radius: 10px;
  font-size: 14px;
  color: #0f172a;
  outline: none;
  font-family: inherit;
  resize: vertical;
  transition: all 0.2s;
  line-height: 1.45;
}

.custom-textarea:focus {
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.15);
}

.highlight-textarea {
  background: #fdfefe;
  font-weight: 600;
  color: #1e293b;
}

/* MAP SECTION */
.map-section-block {
  background: #fafaf9;
  border: 1.5px solid #e2e8f0;
  border-radius: 14px;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.map-toolbar-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.map-label-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.map-section-title {
  font-size: 14px;
  font-weight: 800;
  color: #0f172a;
  display: flex;
  align-items: center;
}

.map-coords-badge {
  font-size: 11px;
  font-weight: 600;
  color: #475569;
  background: #f1f5f9;
  padding: 3px 8px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
}

.geocoding-spinner {
  font-size: 11px;
  color: #ea580c;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 5px;
}

.btn-gps-locate {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  border: none;
  color: #ffffff;
  font-size: 12px;
  font-weight: 700;
  padding: 7px 14px;
  border-radius: 8px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
  box-shadow: 0 2px 6px rgba(234, 88, 12, 0.3);
}

.btn-gps-locate:hover {
  background: linear-gradient(135deg, #c2410c 0%, #9a3412 100%);
  transform: translateY(-1px);
}

.map-search-bar {
  display: flex;
  align-items: center;
  position: relative;
}

.map-search-bar .search-icon {
  position: absolute;
  left: 12px;
  color: #94a3b8;
  font-size: 13px;
}

.map-search-input {
  width: 100%;
  height: 38px;
  padding: 0 60px 0 32px;
  border: 1.5px solid #cbd5e1;
  border-radius: 8px;
  font-size: 13px;
  outline: none;
  background: #ffffff;
  transition: border-color 0.2s;
}

.map-search-input:focus {
  border-color: #ea580c;
}

.btn-quick-search {
  position: absolute;
  right: 4px;
  top: 4px;
  bottom: 4px;
  background: #ea580c;
  border: none;
  color: #ffffff;
  font-size: 12px;
  font-weight: 700;
  padding: 0 14px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-quick-search:hover {
  background: #c2410c;
}

.map-wrapper {
  position: relative;
  width: 100%;
  height: 250px;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid #cbd5e1;
}

.checkout-map-element {
  width: 100%;
  height: 100%;
  z-index: 1;
}

.map-overlay-guide {
  position: absolute;
  bottom: 8px;
  left: 50%;
  transform: translateX(-50%);
  background: rgba(15, 23, 42, 0.82);
  color: #ffffff;
  font-size: 11px;
  font-weight: 600;
  padding: 4px 12px;
  border-radius: 999px;
  pointer-events: none;
  z-index: 10;
  white-space: nowrap;
  backdrop-filter: blur(3px);
}

.quick-district-chips {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 2px;
}

.district-chip {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #334155;
  font-size: 11px;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.district-chip:hover {
  background: #ea580c;
  color: #ffffff;
  border-color: #ea580c;
}

/* MODAL FOOTER */
.modal-footer-line {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 24px;
  background: #f8fafc;
  border-top: 1px solid #f1f5f9;
  flex-shrink: 0;
}

.btn-modal-cancel {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #475569;
  font-weight: 600;
  font-size: 14px;
  padding: 10px 20px;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-modal-cancel:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.btn-modal-save {
  background: linear-gradient(135deg, #ea580c 0%, #c2410c 100%);
  border: none;
  color: #ffffff;
  font-weight: 700;
  font-size: 14px;
  padding: 10px 22px;
  border-radius: 10px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  box-shadow: 0 2px 10px rgba(234, 88, 12, 0.3);
  transition: all 0.2s;
}

.btn-modal-save:hover {
  background: linear-gradient(135deg, #c2410c 0%, #9a3412 100%);
  transform: translateY(-1px);
}

/* Toast Floating */
.checkout-toast {
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
  font-size: 14px;
  font-weight: 600;
  box-shadow: 0 10px 25px -3px rgba(0, 0, 0, 0.3);
  z-index: 2000;
}

.toast-pop-enter-active,
.toast-pop-leave-active {
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-pop-enter-from,
.toast-pop-leave-to {
  opacity: 0;
  transform: translate(-50%, -20px);
}

/* LEAFLET CUSTOM PIN */
:deep(.custom-checkout-pin) {
  background: transparent !important;
  border: none !important;
}

:deep(.checkout-pin-wrapper) {
  position: relative;
  width: 40px;
  height: 48px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-start;
}

:deep(.pin-marker) {
  width: 36px;
  height: 36px;
  background: linear-gradient(135deg, #ea580c 0%, #dc2626 100%);
  color: #ffffff;
  border-radius: 50% 50% 50% 0;
  transform: rotate(-45deg);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.5);
  border: 2px solid #ffffff;
  cursor: grab;
  transition: transform 0.15s ease;
  z-index: 2;
}

:deep(.pin-marker:active) {
  cursor: grabbing;
  transform: rotate(-45deg) scale(1.15);
}

:deep(.pin-marker i) {
  transform: rotate(45deg);
  font-size: 18px;
}

:deep(.pin-shadow) {
  width: 14px;
  height: 5px;
  background: rgba(15, 23, 42, 0.35);
  border-radius: 50%;
  position: absolute;
  bottom: 1px;
  z-index: 1;
}

:deep(.pin-pulse) {
  position: absolute;
  bottom: -2px;
  width: 22px;
  height: 22px;
  background: rgba(234, 88, 12, 0.35);
  border-radius: 50%;
  animation: pin-pulse-wave 2s infinite ease-out;
  z-index: 0;
}

@keyframes pin-pulse-wave {
  0% { transform: scale(0.4); opacity: 1; }
  100% { transform: scale(2.2); opacity: 0; }
}

/* API Config & Live Status Modal Styles */
.live-status-card {
  display: flex;
  align-items: center;
  gap: 12px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  border-radius: 12px;
  padding: 14px 18px;
  margin-bottom: 16px;
}

.status-indicator-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: #16a34a;
  flex-shrink: 0;
}

.status-indicator-dot.pulse {
  animation: pulse-green 1.5s infinite;
}

@keyframes pulse-green {
  0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(22, 163, 74, 0.7); }
  70% { transform: scale(1); box-shadow: 0 0 0 8px rgba(22, 163, 74, 0); }
  100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(22, 163, 74, 0); }
}

.status-card-title {
  font-size: 13px;
  font-weight: 800;
  color: #15803d;
  letter-spacing: 0.5px;
}

.status-card-sub {
  font-size: 12px;
  color: #166534;
  margin-top: 2px;
}

.config-table-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.config-card-header {
  font-size: 13px;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 4px;
}

.config-item-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 10px;
  background: #f8fafc;
  border-radius: 8px;
  border: 1px solid #f1f5f9;
}

.config-item-row.highlight-row {
  background: #eff6ff;
  border-color: #dbeafe;
}

.config-label {
  font-size: 12.5px;
  font-weight: 600;
  color: #475569;
  flex-shrink: 0;
}

.config-value-box {
  display: flex;
  align-items: center;
  gap: 8px;
  overflow: hidden;
}

.config-value-box code {
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  background: rgba(15, 23, 42, 0.06);
  padding: 2px 6px;
  border-radius: 4px;
  color: #0f172a;
  white-space: nowrap;
}

.config-value-box code.url-text {
  font-size: 11px;
  color: #1d4ed8;
  max-width: 260px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.btn-copy-small {
  border: none;
  background: #e2e8f0;
  color: #334155;
  font-size: 11px;
  font-weight: 600;
  padding: 4px 8px;
  border-radius: 6px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  transition: all 0.15s;
  flex-shrink: 0;
}

.btn-copy-small:hover {
  background: #0284c7;
  color: #ffffff;
}

.auto-active-banner {
  display: flex;
  align-items: center;
  gap: 12px;
  background: #f0fdf4;
  border: 1.5px solid #86efac;
  padding: 12px 16px;
  border-radius: 12px;
  margin-top: 14px;
}

.auto-spinner {
  width: 20px;
  height: 20px;
  border: 2.5px solid #bbf7d0;
  border-top-color: #16a34a;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  flex-shrink: 0;
}

.auto-title {
  font-size: 12px;
  font-weight: 800;
  color: #15803d;
  letter-spacing: 0.3px;
}

.auto-desc {
  font-size: 11.5px;
  color: #166534;
  margin-top: 2px;
  line-height: 1.35;
}

.btn-check-status-subtle {
  display: block;
  width: 100%;
  text-align: center;
  background: transparent;
  border: none;
  color: #64748b;
  font-size: 12px;
  font-weight: 600;
  margin-top: 8px;
  cursor: pointer;
  padding: 6px;
}

.btn-check-status-subtle:hover {
  color: #0f172a;
  text-decoration: underline;
}


/* ============================================================================
   RESPONSIVE MOBILE & IOS FIX CHO TRANG THANH TOÁN (CHECKOUT)
   ============================================================================ */
@media (max-width: 768px) {
  .checkout-container {
    margin: 14px auto 140px auto !important;
    padding: 0 12px !important;
    width: 100% !important;
    max-width: 100vw !important;
    box-sizing: border-box !important;
    overflow-x: hidden !important;
  }

  .checkout-header {
    margin-bottom: 16px;
  }

  .checkout-header h2 {
    font-size: 20px !important;
  }

  .checkout-header p {
    font-size: 13px !important;
    margin-bottom: 16px !important;
  }

  .checkout-grid {
    display: flex !important;
    flex-direction: column !important;
    gap: 14px !important;
    width: 100% !important;
    box-sizing: border-box !important;
  }

  .left-col,
  .right-col {
    width: 100% !important;
    max-width: 100% !important;
    box-sizing: border-box !important;
    min-width: 0 !important;
  }

  .card-box {
    padding: 16px 14px !important;
    border-radius: 16px !important;
    margin-bottom: 14px !important;
    width: 100% !important;
    box-sizing: border-box !important;
    overflow: hidden !important;
  }

  .card-box-header {
    display: flex !important;
    justify-content: space-between !important;
    align-items: center !important;
    gap: 8px !important;
    flex-wrap: wrap !important;
    margin-bottom: 12px !important;
    padding-bottom: 10px !important;
  }

  .card-box-header h3 {
    font-size: 15px !important;
    margin: 0 !important;
    flex: 1 !important;
    min-width: 140px !important;
  }

  .btn-edit-addr {
    padding: 6px 11px !important;
    font-size: 12px !important;
    white-space: nowrap !important;
    border-radius: 8px !important;
    flex-shrink: 0 !important;
  }

  .addr-content .user-info-row {
    display: flex !important;
    align-items: center !important;
    gap: 6px !important;
    flex-wrap: wrap !important;
    margin-bottom: 6px !important;
  }

  .user-fullname {
    font-size: 15px !important;
  }

  .user-phone {
    font-size: 13.5px !important;
  }

  .badge-account-isolated {
    font-size: 10.5px !important;
    padding: 2px 6px !important;
    white-space: nowrap !important;
  }

  .addr-text {
    font-size: 13.5px !important;
    line-height: 1.45 !important;
    word-break: break-word !important;
    margin-bottom: 8px !important;
  }

  .delivery-note-tag {
    font-size: 12px !important;
    padding: 5px 10px !important;
    word-break: break-word !important;
  }

  .opt-item,
  .pay-item {
    padding: 12px !important;
    border-radius: 12px !important;
    box-sizing: border-box !important;
    width: 100% !important;
  }

  .opt-head {
    display: flex !important;
    justify-content: space-between !important;
    align-items: flex-start !important;
    gap: 8px !important;
    margin-bottom: 4px !important;
  }

  .opt-head strong {
    font-size: 13.5px !important;
    line-height: 1.35 !important;
    flex: 1 !important;
    min-width: 0 !important;
    word-break: break-word !important;
  }

  .opt-head .price {
    font-size: 14px !important;
    white-space: nowrap !important;
    flex-shrink: 0 !important;
    margin-left: auto !important;
  }

  .sub,
  .pay-item p {
    font-size: 12px !important;
    line-height: 1.4 !important;
    word-break: break-word !important;
  }

  .pay-item {
    display: flex !important;
    align-items: flex-start !important;
    gap: 10px !important;
  }

  .pay-item input[type="radio"] {
    margin-top: 3px !important;
  }

  .pay-item > div {
    flex: 1 !important;
    min-width: 0 !important;
  }

  .pay-item strong {
    font-size: 13.5px !important;
    line-height: 1.35 !important;
    word-break: break-word !important;
  }

  .sticky-box {
    position: static !important;
  }

  .qr-checkout-card {
    padding: 16px 12px !important;
    border-radius: 16px !important;
    width: 100% !important;
    box-sizing: border-box !important;
  }

  .vietqr-clean-img {
    max-width: 180px !important;
    height: auto !important;
    aspect-ratio: 1 !important;
  }

  .memo-only-box {
    padding: 10px 12px !important;
    width: 100% !important;
    box-sizing: border-box !important;
  }

  .memo-copy-row {
    gap: 8px !important;
    flex-wrap: wrap !important;
  }

  .memo-code {
    font-size: 17px !important;
    word-break: break-all !important;
  }

  .auto-active-banner {
    padding: 10px 12px !important;
    gap: 10px !important;
    box-sizing: border-box !important;
    width: 100% !important;
  }

  .auto-title {
    font-size: 11px !important;
    word-break: break-word !important;
  }

  .auto-desc {
    font-size: 10.5px !important;
    word-break: break-word !important;
  }

  .modal-dialog-medium,
  .modal-dialog-card {
    width: calc(100vw - 20px) !important;
    max-width: calc(100vw - 20px) !important;
    margin: 10px auto !important;
    padding: 16px !important;
    border-radius: 16px !important;
  }
}


/* SỔ ĐỊA CHỈ & ĐỊA CHỈ MẶC ĐỊNH */
.addr-actions-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.btn-addr-book {
  background: #fff;
  border: 1px solid #d85a2a;
  color: #d85a2a;
  padding: 6px 12px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
}

.btn-addr-book:hover {
  background: #fff7ed;
  color: #c2410c;
}

.badge-default-pill {
  background: #ecfdf5;
  color: #059669;
  border: 1px solid #a7f3d0;
  padding: 2px 8px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.badge-label-pill {
  background: #eff6ff;
  color: #2563eb;
  border: 1px solid #bfdbfe;
  padding: 2px 8px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 600;
}

.btn-change-addr-link {
  background: none;
  border: none;
  color: #d85a2a;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  padding: 0 4px;
  text-decoration: underline;
}

.label-choice-group {
  display: flex;
  gap: 8px;
}

.choice-btn {
  flex: 1;
  padding: 8px 12px;
  border: 1px solid #cbd5e1;
  background: #ffffff;
  color: #475569;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.choice-btn.active {
  background: #d85a2a;
  color: #ffffff;
  border-color: #d85a2a;
}

.checkbox-text-bold {
  font-size: 13px;
  font-weight: 600;
  color: #1e293b;
}

/* MODAL SỔ ĐỊA CHỈ */
.modal-dialog-medium {
  max-width: 620px;
  width: 95%;
}

.modal-body-list {
  padding: 16px;
  max-height: 480px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.empty-addr-box {
  text-align: center;
  padding: 40px 20px;
  color: #94a3b8;
}

.empty-addr-box i {
  font-size: 48px;
  margin-bottom: 12px;
}

.saved-addr-card {
  border: 1.5px solid #e2e8f0;
  border-radius: 12px;
  padding: 14px 16px;
  background: #ffffff;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.saved-addr-card:hover {
  border-color: #cbd5e1;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.saved-addr-card.active {
  border-color: #d85a2a;
  background: #fffbf9;
}

.saved-addr-card.is-default {
  border-left: 4px solid #059669;
}

.addr-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 6px;
}

.addr-card-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.addr-radio {
  accent-color: #d85a2a;
  width: 18px;
  height: 18px;
  cursor: pointer;
}

.addr-person-name {
  font-size: 14px;
  font-weight: 700;
  color: #1e293b;
}

.addr-person-phone {
  font-size: 13px;
  color: #64748b;
}

.addr-card-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

.btn-addr-action {
  background: none;
  border: none;
  padding: 4px 8px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-edit-action {
  color: #2563eb;
}
.btn-edit-action:hover {
  background: #eff6ff;
}

.btn-setdefault-action {
  color: #059669;
}
.btn-setdefault-action:hover {
  background: #ecfdf5;
}

.btn-delete-action {
  color: #ef4444;
}
.btn-delete-action:hover {
  background: #fef2f2;
}

.addr-card-detail {
  margin: 4px 0 0 26px;
  font-size: 13px;
  color: #334155;
  line-height: 1.4;
}

.addr-card-note {
  margin: 4px 0 0 26px;
  font-size: 12px;
  color: #d97706;
}

</style>
