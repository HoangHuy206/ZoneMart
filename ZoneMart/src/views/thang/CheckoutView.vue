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

const router = useRouter();
const cart = useCart();
const auth = useAuth();

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
  if (toastTimer) clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    showToast.value = false;
  }, 3200);
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

  const { landmark, distanceKm } = findNearestLandmark(lat, lng);

  try {
    const esriUrl = `https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode?f=pjson&location=${lng},${lat}`;
    const res = await fetch(esriUrl, { signal: AbortSignal.timeout(4500) });
    if (res.ok) {
      const data = await res.json();
      if (data && data.address) {
        const addrObj = data.address;
        const match = addrObj.Match_addr || addrObj.LongLabel || addrObj.Address;
        if (match) {
          const cleanAddr = match.replace(/,s*(VNM|Vietnam|Việt Nam)$/i, "").trim();
          editForm.value.address = cleanAddr;
          triggerToast("📍 Đã cập nhật địa chỉ: " + cleanAddr);
          isGeocoding.value = false;
          return;
        }
      }
    }
  } catch (e) {}

  try {
    const bdcUrl = `https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lng}&localityLanguage=vi`;
    const bdcRes = await fetch(bdcUrl, { signal: AbortSignal.timeout(4000) });
    if (bdcRes.ok) {
      const bdcData = await bdcRes.json();
      if (bdcData) {
        const parts = [
          bdcData.locality || "",
          bdcData.city || "",
          bdcData.principalSubdivision || "Hà Nội"
        ].filter(Boolean);
        if (parts.length > 0) {
          const resolved = distanceKm < 0.8
            ? landmark.name
            : `${parts.join(", ")} (Tọa độ: ${lat.toFixed(4)}, ${lng.toFixed(4)})`;
          editForm.value.address = resolved;
          triggerToast("📍 Đã cập nhật địa chỉ: " + resolved);
          isGeocoding.value = false;
          return;
        }
      }
    }
  } catch (e) {}

  isGeocoding.value = false;
  const fallback = distanceKm < 1.5
    ? landmark.name
    : `Gần ${landmark.name} (Tọa độ: ${lat.toFixed(4)}, ${lng.toFixed(4)})`;
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

const locateUserCurrentPosition = () => {
  if (!navigator.geolocation) {
    triggerToast("Trình duyệt của bạn chưa hỗ trợ định vị GPS!");
    return;
  }
  isLocating.value = true;
  triggerToast("Đang xác định vị trí GPS chính xác của bạn...");

  navigator.geolocation.getCurrentPosition(
    (pos) => {
      isLocating.value = false;
      const { latitude, longitude } = pos.coords;
      editForm.value.lat = latitude;
      editForm.value.lng = longitude;

      if (mapInstance) {
        mapInstance.flyTo([latitude, longitude], 17);
      }
      if (markerInstance) {
        markerInstance.setLatLng([latitude, longitude]);
      }
      resolveAddressFromCoords(latitude, longitude);
    },
    (err) => {
      isLocating.value = false;
      console.warn("Lỗi GPS:", err);
      triggerToast("Không thể lấy GPS. Bạn hãy chạm trực tiếp trên bản đồ để chọn điểm!");
    },
    { enableHighAccuracy: true, timeout: 8000, maximumAge: 10000 }
  );
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

// KHI TIỀN THỰC SỰ VÀO -> BẬT HIỆU ỨNG THÀNH CÔNG VÀ LƯU ĐƠN
const onPaymentConfirmed = (paidAmount?: number) => {
  if (paymentState.value === "SUCCESS") return;
  paymentState.value = "SUCCESS";
  if (pollTimer) clearInterval(pollTimer);

  const finalAmount = paidAmount || finalTotal.value;
  const orderId = orderPaymentCode.value;
  const now = new Date();
  const dateStr = now.toLocaleDateString("vi-VN") + " " + now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

  const newOrder = {
    id: orderId,
    date: dateStr,
    total: finalAmount,
    status: "Đã thanh toán (TPBank)",
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
    paymentMethod: "Chuyển khoản TPBank",
    paidAt: new Date().toISOString(),
    bankRef: "TPBank • 28122068866 (DOAN HOANG HUY)",
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

  autoRedirectTimer.value = 2;
  if (redirectInterval) clearInterval(redirectInterval);
  redirectInterval = setInterval(() => {
    autoRedirectTimer.value--;
    if (autoRedirectTimer.value <= 0) {
      clearInterval(redirectInterval);
      goToBuyerOrders();
    }
  }, 1000);
};

// Chuyển tới trang Đơn hàng của tôi
const goToBuyerOrders = () => {
  if (redirectInterval) clearInterval(redirectInterval);
  if (pollTimer) clearInterval(pollTimer);
  try {
    vietQrSocket?.close();
  } catch (e) {}
  router.push("/buyer-orders").catch(() => {
    window.location.href = "/buyer-orders";
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
      const sseUrl = `/api/payment/events?code=${encodeURIComponent(orderPaymentCode.value)}`;
      sseSource = new EventSource(sseUrl);
      sseSource.onmessage = (e) => {
        try {
          const data = JSON.parse(e.data);
          if (data && data.paid && data.code && data.code.toUpperCase() === orderPaymentCode.value.toUpperCase()) {
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

const startPaymentListener = () => {
  initPaymentSSE();

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
      const res = await fetch(checkUrl);
      if (res.ok) {
        const data = await res.json();
        if (data && data.paid && data.code && data.code.toUpperCase() === orderPaymentCode.value.toUpperCase()) {
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

const isCheckingPayment = ref(false);

const handleUserClickPaid = async () => {
  if (paymentState.value === "SUCCESS") {
    goToBuyerOrders();
    return;
  }
  isCheckingPayment.value = true;
  try {
    const checkUrl = `/api/payment/check?code=${encodeURIComponent(orderPaymentCode.value)}&amount=${finalTotal.value}&bankAccount=${encodeURIComponent(vietinBankConfig.value.accountNo)}`;
    const res = await fetch(checkUrl);
    if (res.ok) {
      const data = await res.json();
      if (data && data.paid && data.code && data.code.toUpperCase() === orderPaymentCode.value.toUpperCase()) {
        isCheckingPayment.value = false;
        onPaymentConfirmed(data.amount);
        return;
      }
    }
  } catch (e) {}

  setTimeout(() => {
    isCheckingPayment.value = false;
    triggerToast(`⏳ Đang tiếp tục quét... Chưa có biến động tiền vào tài khoản ${vietinBankConfig.value.bankId} (${vietinBankConfig.value.accountNo}).`);
  }, 500);
};

// Đã kết nối API thật - Tự động đối soát qua Webhook /api/payment/check

// Modal giải thích & hướng dẫn kết nối API Tingo Pay
const showApiHelpModal = ref(false);

// Nạp địa chỉ riêng biệt của tài khoản hiện tại
const loadAddressForCurrentAccount = () => {
  const key = ADDRESS_STORAGE_PREFIX + getAccountKey();
  try {
    const saved = localStorage.getItem(key);
    if (saved) {
      const parsed = JSON.parse(saved);
      if (parsed.address) shippingAddress.value = parsed.address;
      if (parsed.name) buyerName.value = parsed.name;
      if (parsed.phone) buyerPhone.value = parsed.phone;
      if (parsed.note !== undefined) deliveryNote.value = parsed.note;
      if (parsed.lat && parsed.lng) {
        currentCoords.value = { lat: parsed.lat, lng: parsed.lng };
      }
      return;
    }
  } catch (e) {}

  const u = auth.currentUser.value;
  if (u) {
    buyerName.value = u.fullName || u.phoneEmail?.split("@")[0] || "Khách Hàng";
    buyerPhone.value = u.phone || (u.phoneEmail && /^0\d+$/.test(u.phoneEmail) ? u.phoneEmail : "0396222614");
    shippingAddress.value = "Ngõ 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội";
    deliveryNote.value = "";
    currentCoords.value = { lat: 21.0333, lng: 105.7983 };
  } else {
    buyerName.value = "Khách Hàng ZoneMart";
    buyerPhone.value = "0912 345 678";
    shippingAddress.value = "Ngõ 165 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội";
    deliveryNote.value = "";
    currentCoords.value = { lat: 21.0333, lng: 105.7983 };
  }
};

onMounted(() => {
  loadAddressForCurrentAccount();
  initVietQrSocket();
  startPaymentListener();
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
  editForm.value = {
    name: buyerName.value,
    phone: buyerPhone.value,
    address: shippingAddress.value,
    note: deliveryNote.value,
    lat: currentCoords.value.lat,
    lng: currentCoords.value.lng
  };
  showEditAddressModal.value = true;
  initOrUpdateMap();
};

const closeEditAddress = () => {
  showEditAddressModal.value = false;
};

const saveAddress = () => {
  const trimmedName = editForm.value.name.trim();
  const trimmedPhone = editForm.value.phone.trim();
  const trimmedAddress = editForm.value.address.trim();

  if (!trimmedName) {
    alert("Vui lòng nhập họ và tên người nhận hàng!");
    return;
  }
  if (!trimmedPhone || trimmedPhone.length < 9) {
    alert("Vui lòng nhập số điện thoại người nhận hợp lệ!");
    return;
  }
  if (!trimmedAddress || trimmedAddress.length < 6) {
    alert("Vui lòng định vị trên bản đồ hoặc nhập địa chỉ nhận hàng chi tiết!");
    return;
  }

  buyerName.value = trimmedName;
  buyerPhone.value = trimmedPhone;
  shippingAddress.value = trimmedAddress;
  deliveryNote.value = editForm.value.note.trim();
  currentCoords.value = { lat: editForm.value.lat, lng: editForm.value.lng };

  const storageData = {
    name: buyerName.value,
    phone: buyerPhone.value,
    address: shippingAddress.value,
    lat: editForm.value.lat,
    lng: editForm.value.lng,
    note: deliveryNote.value,
    updatedAt: new Date().toISOString()
  };

  const key = ADDRESS_STORAGE_PREFIX + getAccountKey();
  localStorage.setItem(key, JSON.stringify(storageData));

  showEditAddressModal.value = false;
  triggerToast("Đã lưu địa chỉ định vị riêng cho tài khoản của bạn!");
};

// Phí giao hàng & tổng tiền
const shopCount = computed(() => cart.activeStoresCount.value || 1);
const isExpressSelected = ref(true);
const paymentMethod = ref<"COD" | "ONLINE_QR">("ONLINE_QR");

const shippingFee = computed(() => {
  if (isExpressSelected.value) {
    return Math.round((15000 * shopCount.value) * 1.5);
  } else {
    return 15000 * shopCount.value;
  }
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
  if (selectedCartItems.value.length === 0) {
    alert("Bạn chưa chọn món nào để thanh toán! Vui lòng quay lại giỏ hàng và chọn món.");
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

  const newOrder = {
    id: orderId,
    date: dateStr,
    total: finalTotal.value,
    status: "Chờ xác nhận (COD)",
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

  alert(`🎉 Đặt hàng thành công mã #${orderId}!\nĐơn hàng sẽ được giao tới: ${shippingAddress.value}\nNgười nhận: ${buyerName.value} (${buyerPhone.value})`);
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
            <button
              type="button"
              class="btn-edit-addr"
              @click="openEditAddress"
              title="Chỉnh sửa & định vị địa chỉ nhận hàng"
            >
              <i class="bi bi-geo-alt me-1"></i> Định Vị / Thay Đổi
            </button>
          </div>

          <div class="addr-content">
            <div class="user-info-row">
              <span class="user-fullname">{{ buyerName }}</span>
              <span class="user-phone">({{ buyerPhone }})</span>
              <span class="badge-account-isolated">
                <i class="bi bi-shield-check"></i> Đã lưu theo tài khoản
              </span>
            </div>

            <p class="addr-text">
              <i class="bi bi-house-door-fill text-muted me-1"></i>
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
                  Giao Hỏa Tốc Siêu Tốc
                </strong>
                <span class="price">{{ (Math.round((15000 * shopCount) * 1.5)).toLocaleString('vi-VN') }} ₫</span>
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
                  Giao Hàng Tiêu Chuẩn
                </strong>
                <span class="price">{{ (15000 * shopCount).toLocaleString('vi-VN') }} ₫</span>
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

            <label class="pay-item" :class="{ active: paymentMethod === 'COD', disabled: isExpressSelected }">
              <input type="radio" value="COD" v-model="paymentMethod" :disabled="isExpressSelected" />
              <div>
                <strong>Tiền Mặt Khi Nhận Hàng (COD)</strong>
                <p v-if="isExpressSelected">
                  <i class="bi bi-exclamation-triangle-fill text-warning me-1" aria-hidden="true"></i>
                  Không hỗ trợ COD khi chọn Giao Hỏa Tốc
                </p>
                <p v-else>Thanh toán trực tiếp cho Shipper khi nhận món</p>
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

            <!-- TRƯỜNG HỢP 2: KHI TIỀN VÀO -> HIỆN HIỆU ỨNG ANIMATION NÚT TÍCH NGAY TẠI ĐÂY -->
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

                <div class="success-inline-badge">
                  <span>Mã GD: <strong>#{{ orderPaymentCode }}</strong></span>
                  <span class="dot-sep">•</span>
                  <span class="price-val">{{ finalTotal.toLocaleString('vi-VN') }} ₫</span>
                </div>

                <div class="redirect-progress-bar">
                  <div class="progress-fill"></div>
                </div>
                <p class="redirect-sub">Tự động chuyển tới đơn hàng sau {{ autoRedirectTimer }}s...</p>

                <button type="button" class="btn-go-orders-inline" @click="goToBuyerOrders">
                  <i class="bi bi-bag-check-fill me-1"></i> Xem Đơn Hàng Ngay
                </button>
              </div>
            </template>
          </div>

          <!-- TRẠNG THÁI TỰ ĐỘNG ĐỐI SOÁT 100% -->
          <div v-if="paymentMethod === 'ONLINE_QR' && paymentState !== 'SUCCESS'" class="auto-active-banner">
            <div class="auto-spinner"></div>
            <div>
              <div class="auto-title">ĐANG TỰ ĐỘNG LẮNG NGHE TÀI KHOẢN TPBANK</div>
              <div class="auto-desc">Bạn chỉ cần chuyển khoản theo mã QR. Tiền vào hệ thống sẽ tự động xác nhận ngay tức thì!</div>
            </div>
          </div>
          <button
            v-if="paymentMethod === 'ONLINE_QR' && paymentState !== 'SUCCESS'"
            class="btn-check-status-subtle"
            @click="handleUserClickPaid"
            type="button"
            :disabled="isCheckingPayment"
          >
            <i class="bi me-1" :class="isCheckingPayment ? 'bi-arrow-repeat spin-icon' : 'bi-arrow-repeat'"></i>
            {{ isCheckingPayment ? 'Đang đối soát số dư với ngân hàng TPBank...' : 'Kiểm tra trạng thái tiền về ngay' }}
          </button>
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
  border-bottom: 1px solid #f1f5f9;
  padding-bottom: 12px;
  margin-bottom: 16px;
}

.card-box-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
  color: #0f172a;
}

.btn-edit-addr {
  background: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
  font-size: 13px;
  font-weight: 700;
  padding: 7px 15px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
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
  align-items: center;
  margin-bottom: 4px;
}

.opt-head .price {
  font-weight: 800;
  color: #ea580c;
  font-size: 15px;
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
   KHUNG QR CHUYỂN KHOẢN TINH GỌN (VIETINBANK)
   ============================================================================ */
.qr-checkout-card {
  background: #fafaf9;
  border: 1.5px solid #e2e8f0;
  border-radius: 18px;
  padding: 18px 16px;
  margin: 18px 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.04);
  transition: all 0.3s ease;
}

.payment-status-radar {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  font-weight: 700;
  color: #c2410c;
  background: #ffedd5;
  padding: 5px 14px;
  border-radius: 999px;
  margin-bottom: 14px;
}

.radar-dot {
  width: 8px;
  height: 8px;
  background: #ea580c;
  border-radius: 50%;
  box-shadow: 0 0 0 0 rgba(234, 88, 12, 0.7);
  animation: radar-pulse 1.6s infinite cubic-bezier(0.66, 0, 0, 1);
}

@keyframes radar-pulse {
  to {
    box-shadow: 0 0 0 10px rgba(234, 88, 12, 0);
  }
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

.qr-image-wrapper {
  background: #ffffff;
  padding: 12px;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
  margin-bottom: 14px;
}

.vietqr-clean-img {
  width: 195px;
  height: 195px;
  display: block;
  border-radius: 8px;
  object-fit: contain;
}

/* HỘP NỘI DUNG CHUYỂN TIỀN DUY NHẤT */
.memo-only-box {
  width: 100%;
  background: #ffffff;
  border: 1.5px dashed #cbd5e1;
  border-radius: 12px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  margin-bottom: 10px;
}

.memo-label {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.memo-copy-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.memo-code {
  font-size: 18px;
  font-weight: 900;
  color: #ea580c;
  font-family: monospace;
  letter-spacing: 1px;
}

.btn-copy-memo {
  background: #fff7ed;
  border: 1px solid #fed7aa;
  color: #ea580c;
  font-size: 11px;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-copy-memo:hover {
  background: #ea580c;
  color: #ffffff;
}

.qr-helper-text {
  font-size: 11.5px;
  color: #475569;
  text-align: center;
  line-height: 1.4;
  margin-bottom: 10px;
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

.btn-simulate-payment {
  background: #ffffff;
  border: 1px dashed #ea580c;
  color: #ea580c;
  font-size: 11.5px;
  font-weight: 700;
  padding: 7px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.btn-simulate-payment:hover {
  background: #ea580c;
  color: #ffffff;
}

/* ANIMATION NÚT TÍCH XANH */
.qr-success-inline-box {
  width: 100%;
  padding: 10px 4px 6px 4px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  animation: success-fade-in 0.4s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes success-fade-in {
  0% { opacity: 0; transform: scale(0.92); }
  100% { opacity: 1; transform: scale(1); }
}

.success-animation-circle {
  width: 80px;
  height: 80px;
  margin: 6px auto 14px auto;
  position: relative;
}

.checkmark-svg {
  width: 80px;
  height: 80px;
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
  50% { transform: scale3d(1.1, 1.1, 1); }
}

@keyframes checkmark-fill {
  100% { box-shadow: inset 0 0 0 40px #ecfdf5; }
}

.success-inline-title {
  margin: 0 0 4px 0;
  font-size: 19px;
  font-weight: 800;
  color: #166534;
}

.success-inline-desc {
  margin: 0 0 12px 0;
  font-size: 12.5px;
  color: #475569;
}

.success-inline-badge {
  background: #ffffff;
  border: 1px solid #bbf7d0;
  color: #166534;
  font-size: 12.5px;
  padding: 6px 14px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 14px;
  box-shadow: 0 2px 6px rgba(22, 163, 74, 0.1);
}

.success-inline-badge .price-val {
  font-weight: 800;
  color: #15803d;
}

.dot-sep {
  color: #86efac;
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
  font-size: 11.5px;
  color: #64748b;
  font-weight: 600;
}

.btn-go-orders-inline {
  background: linear-gradient(135deg, #16a34a 0%, #15803d 100%);
  color: #ffffff;
  border: none;
  width: 100%;
  padding: 10px;
  border-radius: 10px;
  font-size: 13.5px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 3px 10px rgba(22, 163, 74, 0.3);
  transition: all 0.2s;
}

.btn-go-orders-inline:hover {
  background: linear-gradient(135deg, #15803d 0%, #166534 100%);
  transform: translateY(-1px);
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

</style>
