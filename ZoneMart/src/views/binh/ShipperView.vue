<script setup lang="ts">
/**
 * ================================================================
 * CỔNG TRANG CHỦ TÀI XẾ (DRIVER DASHBOARD WITH LIVE MAP) - Phụ trách: Bình
 * Tính năng chính:
 * 1. Nút gạt BẬT / TẮT hoạt động (Active/Inactive Toggle)
 * 2. Bản đồ tương tác Leaflet hiển thị vị trí Shipper, Shop, Khách hàng & Tuyến đường 3km
 * 3. Luồng nhận đơn hàng hỏa tốc, xác nhận lấy hàng và hoàn tất đơn
 * 4. Thống kê thu nhập ca làm việc hôm nay
 * ================================================================
 */
import { ref, reactive, onMounted, onUnmounted, watch, nextTick } from "vue";
import { useRouter } from "vue-router";
import { useAuth } from "../../composables/useAuth";
import { useToast } from "../../composables/useToast";
import { orderRealtimeService, type RealtimeOrder } from "../../services/orderRealtimeService";
import FaceScanModal from "../../components/auth/FaceScanModal.vue";
import { apiFetch } from "../../utils/apiConfig";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

const router = useRouter();
const auth = useAuth();
const toast = useToast();

// Quản lý nổ đơn hàng mới trong bán kính 3km
const incomingOrder = ref<RealtimeOrder | null>(null);
const showIncomingModal = ref(false);
const incomingCountdown = ref(30);
let incomingTimer: any = null;
const incomingDistance = ref(0);
let unsubscribeOrderDispatch: (() => void) | null = null;
let unsubscribeStatusChanged: (() => void) | null = null;
const ignoredOrderIds = new Set<string>();

// 1. Trạng thái hoạt động (Mặc định: Tạm nghỉ)
const isOnline = ref(false);

// 2. Trạng thái đơn hàng: "idle" (Mặc định: Chưa có đơn hàng, đang trống)
const currentStep = ref<"idle" | "accepted" | "picked" | "delivered">("idle");

// 3. Tọa độ khởi tạo mặc định (Cầu Giấy, Hà Nội)
const driverLocation = ref({
  lat: 21.0333,
  lng: 105.7944,
  name: "Vị trí của bạn"
});

// Dữ liệu đơn hàng (Mặc định: null - không có dữ liệu gì)
const activeOrder = ref<any>(null);

// Tab điều hướng: 'map' (Bản đồ & Bật tắt hoạt động) | 'earnings' (Thu nhập) | 'trips' (Cuốc xe) | 'profile' (Tài khoản)
const currentTab = ref<'map' | 'earnings' | 'trips' | 'profile'>('map');

interface CompletedTrip {
  id: string;
  time: string;
  storeName: string;
  customerName: string;
  shippingFee: number;
  distanceKm: number;
  status: string;
}

const tripHistory = ref<CompletedTrip[]>([]);

const switchTab = (tab: 'map' | 'earnings' | 'trips' | 'profile') => {
  currentTab.value = tab;
  if (tab === 'map') {
    nextTick(() => {
      window.scrollTo(0, 0);
      map?.invalidateSize();
    });
  }
};

// Thống kê hôm nay của tài xế (Mặc định: 0 hết)
const shiftStats = ref({
  todayEarnings: 0,
  completedOrders: 0,
  onlineHours: "0h 00m",
  totalKm: 0.0
});

// Dynamic thông tin tài xế shipper đăng nhập từ localStorage & Backend MongoDB
const driverProfile = reactive({
  fullName: "Tài xế ZoneMart",
  phoneEmail: "",
  licensePlate: "Chưa cập nhật",
  vehicleType: "Xe máy xăng",
  vehicleModel: "Xe máy",
  operatingArea: "Quận Cầu Giấy, Hà Nội (Bán kính 5km)",
  avatarUrl: "",
  shipperCode: "00001",
  status: "Pending"
});

const loadDriverProfile = async () => {
  const savedUserStr = localStorage.getItem("currentUser");
  if (savedUserStr) {
    try {
      const u = JSON.parse(savedUserStr);
      const resolvedName = u.fullName || u.name || u.displayName || (u.shipperDetails && (u.shipperDetails.fullName || u.shipperDetails.name));
      if (resolvedName) driverProfile.fullName = resolvedName;
      if (u.phoneEmail) driverProfile.phoneEmail = u.phoneEmail;
      const resolvedAvatar = u.avatarUrl || u.avatar || (u.shipperDetails && (u.shipperDetails.avatarUrl || u.shipperDetails.avatar));
      if (resolvedAvatar) driverProfile.avatarUrl = resolvedAvatar;

      if (u.shipperDetails) {
        if (u.shipperDetails.fullName || u.shipperDetails.name) driverProfile.fullName = u.shipperDetails.fullName || u.shipperDetails.name;
        if (u.shipperDetails.licensePlate) driverProfile.licensePlate = u.shipperDetails.licensePlate;
        if (u.shipperDetails.vehicleModel) driverProfile.vehicleModel = u.shipperDetails.vehicleModel;
        if (u.shipperDetails.vehicleType) driverProfile.vehicleType = u.shipperDetails.vehicleType;
        if (u.shipperDetails.operatingArea) driverProfile.operatingArea = u.shipperDetails.operatingArea;
        if (u.shipperDetails.shipperCode) driverProfile.shipperCode = u.shipperDetails.shipperCode;
        if (u.shipperDetails.status) driverProfile.status = u.shipperDetails.status;
        if (u.shipperDetails.avatarUrl || u.shipperDetails.avatar) driverProfile.avatarUrl = u.shipperDetails.avatarUrl || u.shipperDetails.avatar;
      }

      // Query API endpoint để đồng bộ thông tin mới nhất từ CSDL MongoDB Atlas
      const accountQuery = u.phoneEmail || u.phoneNumber || driverProfile.phoneEmail;
      if (accountQuery) {
        const res = await fetch(`/api/auth/shipper-profile?account=${encodeURIComponent(accountQuery)}`).catch(() => null);
        if (res && res.ok) {
          const data = await res.json();
          if (data.success && data.shipper) {
            const sh = data.shipper;
            if (sh.fullName) driverProfile.fullName = sh.fullName;
            if (sh.licensePlate) driverProfile.licensePlate = sh.licensePlate;
            if (sh.vehicleModel) driverProfile.vehicleModel = sh.vehicleModel;
            if (sh.vehicleType) driverProfile.vehicleType = sh.vehicleType;
            if (sh.operatingArea) driverProfile.operatingArea = sh.operatingArea;
            if (sh.shipperCode) driverProfile.shipperCode = sh.shipperCode;
            if (sh.status) driverProfile.status = sh.status;
            if (sh.avatarUrl) driverProfile.avatarUrl = sh.avatarUrl;
          }
        }
      }
    } catch (e) {
      console.error("Lỗi khi nạp thông tin tài xế:", e);
    }
  }
};

// Nạp ngay thông tin tài xế đã đăng nhập từ localStorage & MongoDB
loadDriverProfile();

// Toast thông báo
const toastMsg = ref("");
const showToast = ref(false);
const triggerToast = (msg: string) => {
  toastMsg.value = msg;
  showToast.value = true;
  setTimeout(() => showToast.value = false, 2800);
};

// ==================== THIẾT LẬP FACE-ID CHO TÀI XẾ ====================
const showFaceModal = ref(false);
const isFaceAuthEnabled = ref(false);
const faceRegisteredAt = ref("");
const isCheckingFaceStatus = ref(false);
const isDisablingFace = ref(false);

const getShipperUserIdentifier = () => {
  const clean = (driverProfile.phoneEmail || "").toLowerCase().trim();
  if (clean) return clean;
  const authUser = auth.currentUser.value;
  if (authUser?.phoneEmail || authUser?.phone || authUser?.id) {
    return (authUser.phoneEmail || authUser.phone || authUser.id).toLowerCase().trim();
  }
  const savedUserStr = localStorage.getItem("currentUser") || localStorage.getItem("zonemart_user");
  if (savedUserStr) {
    try {
      const u = JSON.parse(savedUserStr);
      return (u.phoneEmail || u.phoneNumber || u.phone || u.email || u.id || "").toLowerCase().trim();
    } catch {}
  }
  return "";
};

const checkFaceStatus = async () => {
  const userId = getShipperUserIdentifier();
  if (!userId || userId === "usr_guest") {
    isFaceAuthEnabled.value = false;
    faceRegisteredAt.value = "";
    return;
  }
  isCheckingFaceStatus.value = true;
  try {
    const res = await apiFetch(`/api/auth/face/status?userId=${encodeURIComponent(userId)}`);
    if (res && res.ok) {
      const data = await res.json();
      if (data.success) {
        isFaceAuthEnabled.value = Boolean(data.faceAuthEnabled);
        faceRegisteredAt.value = data.registeredAt || "";
      }
    }
  } catch (e) {
    console.error("Lỗi kiểm tra Face ID tài xế:", e);
  } finally {
    isCheckingFaceStatus.value = false;
  }
};

const openFaceRegisterModal = () => {
  const userId = getShipperUserIdentifier();
  if (!userId) {
    triggerToast("Vui lòng đăng nhập tài khoản tài xế để thiết lập Face ID!");
    return;
  }
  showFaceModal.value = true;
};

const handleFaceRegisterSuccess = (payload: any) => {
  isFaceAuthEnabled.value = true;
  faceRegisteredAt.value = payload.registeredAt || new Date().toLocaleString("vi-VN");
  triggerToast("🎉 Đã kích hoạt và lưu khuôn mặt thành công cho tài xế!");
};

const handleDisableFace = async () => {
  const userId = getShipperUserIdentifier();
  if (!userId) return;
  if (!confirm("Bạn có chắc chắn muốn tắt tính năng đăng nhập bằng khuôn mặt Face-ID cho tài xế này?")) {
    return;
  }
  isDisablingFace.value = true;
  try {
    const res = await apiFetch("/api/auth/face/disable", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId }),
    });
    const data = await res.json();
    if (res.ok && data.success) {
      isFaceAuthEnabled.value = false;
      faceRegisteredAt.value = "";
      triggerToast("Đã hủy kích hoạt Face ID cho tài xế.");
    } else {
      triggerToast(data.message || "Không thể hủy Face ID.");
    }
  } catch (e: any) {
    triggerToast(e.message || "Lỗi khi hủy Face ID.");
  } finally {
    isDisablingFace.value = false;
  }
};

watch(currentTab, (newTab) => {
  if (newTab === "profile") {
    checkFaceStatus();
  }
});

// 4. Biến quản lý bản đồ Google Maps (Leaflet engine)
let map: L.Map | null = null;
let tileLayer: L.TileLayer | null = null;
let driverMarker: L.Marker | null = null;
let storeMarker: L.Marker | null = null;
let customerMarker: L.Marker | null = null;
let radiusCircle: L.Circle | null = null;
let routeLineCasing: L.Polyline | null = null;
let routeLine: L.Polyline | null = null;
let accuracyCircle: L.Circle | null = null;
let watchId: number | null = null;

// Chế độ bản đồ Google Maps: 'roadmap' (Bản đồ chuẩn) hoặc 'satellite' (Vệ tinh hybrid)
const mapType = ref<'roadmap' | 'satellite'>('roadmap');
const showTraffic = ref(false);
const isLocating = ref(false);
const gpsAccuracy = ref<number | null>(null);
const locationAddress = ref("Chế độ tạm nghỉ • Gạt 'Bật Hoạt Động' để định vị GPS");

// Tạo icon tùy biến chuẩn Google Maps
const driverIcon = L.divIcon({
  className: "custom-gm-icon",
  html: `
    <div class="gm-driver-beacon">
      <div class="gm-beacon-wave"></div>
      <div class="gm-beacon-core">
        <span class="gm-core-icon"><i class="bi bi-bicycle"></i></span>
      </div>
    </div>
  `,
  iconSize: [44, 44],
  iconAnchor: [22, 22]
});

const storeIcon = L.divIcon({
  className: "custom-gm-icon",
  html: `
    <div class="gm-marker-pin store-pin">
      <div class="gm-pin-bubble">
        <span class="gm-pin-icon"><i class="bi bi-shop"></i></span>
      </div>
      <div class="gm-pin-point"></div>
      <div class="gm-pin-shadow"></div>
    </div>
  `,
  iconSize: [36, 46],
  iconAnchor: [18, 44],
  popupAnchor: [0, -42]
});

const customerIcon = L.divIcon({
  className: "custom-gm-icon",
  html: `
    <div class="gm-marker-pin customer-pin">
      <div class="gm-pin-bubble">
        <span class="gm-pin-dot"></span>
      </div>
      <div class="gm-pin-point"></div>
      <div class="gm-pin-shadow"></div>
    </div>
  `,
  iconSize: [36, 46],
  iconAnchor: [18, 44],
  popupAnchor: [0, -42]
});

// Cập nhật lớp hiển thị Google Maps (Bản đồ / Vệ tinh / Giao thông)
const updateMapLayers = () => {
  if (!map) return;
  if (tileLayer) {
    map.removeLayer(tileLayer);
  }

  let tileUrl = 'https://mt{s}.google.com/vt/lyrs=m&hl=vi&x={x}&y={y}&z={z}';
  if (mapType.value === 'satellite') {
    tileUrl = 'https://mt{s}.google.com/vt/lyrs=y&hl=vi&x={x}&y={y}&z={z}';
  } else if (showTraffic.value) {
    tileUrl = 'https://mt{s}.google.com/vt/lyrs=m,traffic&hl=vi&x={x}&y={y}&z={z}';
  }

  tileLayer = L.tileLayer(tileUrl, {
    subdomains: ['0', '1', '2', '3'],
    maxZoom: 20
  }).addTo(map);

  if (radiusCircle) radiusCircle.bringToFront();
  if (routeLineCasing) routeLineCasing.bringToFront();
  if (routeLine) routeLine.bringToFront();
};

// Chuyển đổi qua lại giữa Bản đồ đường sá và Vệ tinh
const toggleMapType = () => {
  mapType.value = mapType.value === 'roadmap' ? 'satellite' : 'roadmap';
  updateMapLayers();
  triggerToast(mapType.value === 'satellite' ? 'Đã chuyển sang chế độ Vệ tinh Google Maps' : 'Đã chuyển sang chế độ Bản đồ Google Maps');
};

// Bật/Tắt dữ liệu giao thông trực tiếp Google Traffic
const toggleTraffic = () => {
  showTraffic.value = !showTraffic.value;
  updateMapLayers();
  triggerToast(showTraffic.value ? 'Đã bật dữ liệu Giao thông trực tiếp Google' : 'Đã tắt lớp dữ liệu Giao thông');
};

// Khởi tạo bản đồ Google Maps
const initMap = () => {
  const container = document.getElementById("shipperMap");
  if (!container || map) return;

  map = L.map("shipperMap", {
    center: [driverLocation.value.lat, driverLocation.value.lng],
    zoom: 13.8,
    zoomControl: false,
    attributionControl: false
  });

  updateMapLayers();

  // Vòng bán kính quét đơn 3km Google Style
  radiusCircle = L.circle([driverLocation.value.lat, driverLocation.value.lng], {
    radius: 3000,
    color: isOnline.value ? "#2563eb" : "#94a3b8",
    fillColor: isOnline.value ? "#3b82f6" : "#94a3b8",
    fillOpacity: isOnline.value ? 0.12 : 0.05,
    weight: 2.5,
    dashArray: isOnline.value ? "8, 6" : "6, 6"
  }).addTo(map);

  radiusCircle.bindTooltip("", {
    permanent: false,
    direction: "top"
  });

  // Ghim vị trí tài xế
  driverMarker = L.marker([driverLocation.value.lat, driverLocation.value.lng], {
    icon: driverIcon,
    title: "Vị trí của bạn"
  }).addTo(map).bindPopup(`
    <div class="gm-infowindow">
      <div class="gm-iw-tag text-primary">VỊ TRÍ CỦA BẠN</div>
      <h4 class="gm-iw-title">Tài xế ZoneMart Driver</h4>
      <p class="gm-iw-desc">${isOnline.value ? "🟢 Đang trực tuyến sẵn sàng nhận đơn" : "⚪ Đang tạm nghỉ"}</p>
      <div class="gm-iw-meta">Gạt BẬT HOẠT ĐỘNG để định vị GPS chính xác</div>
    </div>
  `);

  renderOrderOnMap();
};

// Vẽ tuyến đường & các điểm đơn hàng chuẩn Google Navigation (Tự động tìm con đường ngắn nhất qua OSRM)
const renderOrderOnMap = async () => {
  if (!map) return;

  // Xóa layers cũ
  if (storeMarker) map.removeLayer(storeMarker);
  if (customerMarker) map.removeLayer(customerMarker);
  if (routeLineCasing) map.removeLayer(routeLineCasing);
  if (routeLine) map.removeLayer(routeLine);

  if (isOnline.value && currentStep.value !== "idle" && currentStep.value !== "delivered" && activeOrder.value) {
    const storeLat = activeOrder.value.store?.lat ?? 21.0345;
    const storeLng = activeOrder.value.store?.lng ?? 105.7960;
    const custLat = activeOrder.value.customer?.lat ?? 21.0333;
    const custLng = activeOrder.value.customer?.lng ?? 105.7983;

    // Ghim Shop
    storeMarker = L.marker([storeLat, storeLng], {
      icon: storeIcon
    }).addTo(map).bindPopup(`
      <div class="gm-infowindow">
        <div class="gm-iw-tag text-blue">ĐIỂM LẤY HÀNG</div>
        <h4 class="gm-iw-title">${activeOrder.value.store?.name || 'Quán ZoneMart'}</h4>
        <p class="gm-iw-desc"><i class="bi bi-geo-alt-fill text-primary"></i> ${activeOrder.value.store?.address || 'Khu vực Cầu Giấy'}</p>
        <div class="gm-iw-rating"><i class="bi bi-star-fill text-warning"></i> 4.9 <span class="text-muted">(1,240 đánh giá) • Mở cửa</span></div>
      </div>
    `);

    // Ghim Khách hàng
    customerMarker = L.marker([custLat, custLng], {
      icon: customerIcon
    }).addTo(map).bindPopup(`
      <div class="gm-infowindow">
        <div class="gm-iw-tag text-danger">ĐIỂM GIAO HÀNG</div>
        <h4 class="gm-iw-title">${activeOrder.value.customer?.name || 'Khách Hàng'}</h4>
        <p class="gm-iw-desc"><i class="bi bi-geo-alt-fill text-danger"></i> ${activeOrder.value.customer?.address || 'Địa chỉ nhận hàng'}</p>
        <div class="gm-iw-meta"><i class="bi bi-telephone-fill"></i> ${activeOrder.value.customer?.phone || ''} • Hỏa tốc 3km</div>
      </div>
    `);

    // Tự động định vị con đường ngắn nhất thực tế qua OSRM Driving Engine
    let waypoints: [number, number][] = [];
    if (currentStep.value === "accepted") {
      // Tuyến đường ngắn nhất từ Vị trí Tài xế -> Quán
      waypoints = await orderRealtimeService.fetchShortestRoute(
        driverLocation.value.lat,
        driverLocation.value.lng,
        storeLat,
        storeLng
      );
    } else {
      // Tuyến đường ngắn nhất từ Vị trí Hiện Tại / Quán -> Khách Hàng
      waypoints = await orderRealtimeService.fetchShortestRoute(
        driverLocation.value.lat,
        driverLocation.value.lng,
        custLat,
        custLng
      );
    }

    if (!map) return;

    // Tuyến đường Google Maps kép: Casing ngoài + Inner line màu sáng
    routeLineCasing = L.polyline(waypoints, {
      color: currentStep.value === "accepted" ? "#1a73e8" : "#137333",
      weight: 8,
      opacity: 0.95,
      lineCap: "round",
      lineJoin: "round"
    }).addTo(map);

    routeLine = L.polyline(waypoints, {
      color: currentStep.value === "accepted" ? "#4285f4" : "#34a853",
      weight: 5,
      opacity: 1,
      lineCap: "round",
      lineJoin: "round"
    }).addTo(map);

    // Fit view bao trọn lộ trình ngắn nhất
    if (waypoints.length > 0) {
      const bounds = L.latLngBounds(waypoints);
      map.fitBounds(bounds, { padding: [60, 60] });
    }
  }
};

// Lấy tên địa chỉ thực tế từ tọa độ qua reverse geocode tiếng Việt
const fetchAddressName = async (lat: number, lng: number) => {
  try {
    const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&accept-language=vi`);
    if (res.ok) {
      const data = await res.json();
      if (data && data.display_name) {
        const parts = data.display_name.split(",");
        const shortAddr = parts.slice(0, 3).join(", ").trim();
        locationAddress.value = shortAddr;

        if (driverMarker) {
          driverMarker.bindPopup(`
            <div class="gm-infowindow">
              <div class="gm-iw-tag text-primary">VỊ TRÍ THỰC TẾ QUA GPS</div>
              <h4 class="gm-iw-title">Tài xế ZoneMart (Bạn)</h4>
              <p class="gm-iw-desc"><i class="bi bi-geo-alt-fill text-primary"></i> ${shortAddr}</p>
              <div class="gm-iw-meta">Sai số GPS: ±${gpsAccuracy.value || 5}m • Đang trực tuyến</div>
            </div>
          `).openPopup();
        }
      }
    }
  } catch (e) {
    console.debug("Reverse geocode fallback", e);
  }
};

// Định vị chính xác vị trí tài xế qua GPS của thiết bị
const locateAndTrackDriver = (isSilent = false) => {
  if (!navigator.geolocation) {
    triggerToast("Trình duyệt không hỗ trợ Geolocation GPS");
    return;
  }

  isLocating.value = true;
  if (!isSilent) {
    triggerToast("📡 Đang lấy tọa độ GPS thực tế từ thiết bị của bạn...");
  }

  navigator.geolocation.getCurrentPosition(
    (pos) => {
      isLocating.value = false;
      const lat = pos.coords.latitude;
      const lng = pos.coords.longitude;
      const accuracy = Math.round(pos.coords.accuracy);
      gpsAccuracy.value = accuracy;

      // Cập nhật tọa độ tài xế
      driverLocation.value.lat = lat;
      driverLocation.value.lng = lng;
      driverLocation.value.name = "Vị trí thực tế của bạn";
      locationAddress.value = `GPS chính xác (±${accuracy}m): ${lat.toFixed(5)}, ${lng.toFixed(5)}`;

      // Cập nhật marker tài xế
      if (driverMarker) {
        driverMarker.setLatLng([lat, lng]);
        driverMarker.setPopupContent(`
          <div class="gm-infowindow">
            <div class="gm-iw-tag text-primary">VỊ TRÍ THỰC TẾ QUA GPS</div>
            <h4 class="gm-iw-title">Tài xế ZoneMart (Bạn)</h4>
            <p class="gm-iw-desc">🟢 Đang trực tuyến tại vị trí hiện tại</p>
            <div class="gm-iw-meta">Độ chính xác GPS: ±${accuracy}m • ${lat.toFixed(4)}, ${lng.toFixed(4)}</div>
          </div>
        `);
      }

      // Cập nhật vòng bán kính 5km quanh tài xế
      if (radiusCircle) {
        radiusCircle.setLatLng([lat, lng]);
      }

      // Vòng tròn độ chính xác GPS (Accuracy Circle)
      if (map) {
        if (accuracyCircle) map.removeLayer(accuracyCircle);
        accuracyCircle = L.circle([lat, lng], {
          radius: Math.max(accuracy, 20),
          color: "#60a5fa",
          fillColor: "#93c5fd",
          fillOpacity: 0.25,
          weight: 1.5
        }).addTo(map);

        // Di chuyển camera mượt mà đến vị trí thực tế, zoom 13.8 để bao quát trọn vẹn vòng quét 3km
        const targetZoom = (activeOrder.value && activeOrder.value.status !== 'delivered') ? 15 : 13.8;
        map.flyTo([lat, lng], targetZoom, {
          animate: true,
          duration: 1.2
        });

        setTimeout(() => {
          driverMarker?.openPopup();
        }, 1300);
      }

      triggerToast(`🎯 Đã định vị chính xác vị trí của bạn (sai số ±${accuracy}m)!`);

      // Lấy tên đường tiếng Việt
      fetchAddressName(lat, lng);

      // Cập nhật lại đường đi đơn hàng nếu có
      renderOrderOnMap();
    },
    (err) => {
      isLocating.value = false;
      console.warn("Lỗi Geolocation:", err);
      let errorMsg = "Không thể định vị GPS (Vui lòng chọn 'Cho phép' khi trình duyệt hỏi quyền vị trí)";
      if (err.code === err.PERMISSION_DENIED) {
        errorMsg = "Bạn chưa cấp quyền truy cập vị trí trên trình duyệt!";
      } else if (err.code === err.TIMEOUT) {
        errorMsg = "Quá thời gian định vị GPS, dùng vị trí khu vực Cầu Giấy.";
      }
      triggerToast(errorMsg);

      if (map) {
        map.setView([driverLocation.value.lat, driverLocation.value.lng], 13.8);
        driverMarker?.openPopup();
      }
    },
    {
      enableHighAccuracy: true,
      timeout: 10000,
      maximumAge: 0
    }
  );
};

// Theo dõi di chuyển GPS thời gian thực khi đang hoạt động
const startLocationWatch = () => {
  if (!navigator.geolocation) return;
  stopLocationWatch();
  watchId = navigator.geolocation.watchPosition(
    (pos) => {
      if (!isOnline.value) return;
      const lat = pos.coords.latitude;
      const lng = pos.coords.longitude;
      const accuracy = Math.round(pos.coords.accuracy);
      gpsAccuracy.value = accuracy;
      driverLocation.value.lat = lat;
      driverLocation.value.lng = lng;

      if (driverMarker) {
        driverMarker.setLatLng([lat, lng]);
      }
      if (radiusCircle) {
        radiusCircle.setLatLng([lat, lng]);
      }
      if (accuracyCircle) {
        accuracyCircle.setLatLng([lat, lng]);
        accuracyCircle.setRadius(Math.max(accuracy, 20));
      }
    },
    (err) => console.debug("Watch position err:", err),
    {
      enableHighAccuracy: true,
      maximumAge: 3000,
      timeout: 15000
    }
  );
};

const stopLocationWatch = () => {
  if (watchId !== null) {
    navigator.geolocation.clearWatch(watchId);
    watchId = null;
  }
};

// Điều hướng tâm bản đồ về tài xế & định vị lại GPS
const recenterMap = () => {
  locateAndTrackDriver(false);
};

// Xử lý kiểm tra và kích hoạt nổ đơn hỏa tốc trong bán kính 3km
const triggerIncomingOrderCheck = (order: RealtimeOrder) => {
  if (!order || !order.id) return;

  // 1. Chỉ nổ đơn khi Shipper đang BẬT HOẠT ĐỘNG
  if (!isOnline.value) {
    console.log("[ShipperView] Tài xế đang TẮT hoạt động -> Không nổ đơn.");
    return;
  }

  // 2. Chỉ nổ đơn khi đơn đã được quán XÁC NHẬN (chờ tài xế)
  if (order.status !== "confirmed") {
    console.log("[ShipperView] Đơn chưa được Quán xác nhận -> Không nổ đơn.");
    return;
  }

  // 3. Nếu tài xế đã bỏ qua / hủy bỏ đơn này thì không nổ lại
  if (ignoredOrderIds.has(order.id)) {
    return;
  }

  // 4. Nếu đang bận giao đơn khác thì không nổ đơn đè
  if (currentStep.value !== "idle" && currentStep.value !== "delivered") {
    console.log("[ShipperView] Tài xế đang bận đơn khác -> Không nổ đơn.");
    return;
  }

  // 5. Tính khoảng cách thực tế từ vị trí hiện tại của Shipper tới Quán (Haversine)
  const storeLat = order.store?.lat ?? 21.0345;
  const storeLng = order.store?.lng ?? 105.7960;
  const dist = orderRealtimeService.calculateDistanceKm(
    driverLocation.value.lat,
    driverLocation.value.lng,
    storeLat,
    storeLng
  );

  incomingDistance.value = dist;

  // QUY TẮC: Trong vòng bán kính 3km thì LẬP TỨC NỔ ĐƠN, ngược lại nếu > 3km thì KHÔNG NỔ ĐƠN
  if (dist <= 3.0) {
    console.log(`[ShipperView] Đơn #${order.id} nằm trong bán kính 3km (${dist} km) -> NỔ ĐƠN!`);
    incomingOrder.value = order;
    showIncomingModal.value = true;
    incomingCountdown.value = 30;

    // Phát chuông báo nổ đơn
    orderRealtimeService.playOrderAlertSound();
    triggerToast(`🚨 NỔ ĐƠN HỎA TỐC! Quán cách bạn ${dist} km (Bán kính <= 3km)`);

    if (incomingTimer) clearInterval(incomingTimer);
    incomingTimer = setInterval(() => {
      incomingCountdown.value--;
      if (incomingCountdown.value <= 0) {
        clearInterval(incomingTimer);
        showIncomingModal.value = false;
        incomingOrder.value = null;
      }
    }, 1000);
  } else {
    console.log(`[ShipperView] Đơn nằm ngoài bán kính 3km (${dist} km) -> Bỏ qua, không nổ đơn.`);
    triggerToast(`ℹ️ Có đơn mới ngoài bán kính 3km (${dist} km) - Không nổ đơn.`);
  }
};

// Xử lý bật/tắt online
const toggleOnline = () => {
  isOnline.value = !isOnline.value;
  if (isOnline.value) {
    currentTab.value = 'map';
    triggerToast("🟢 Đã BẬT nhận đơn! Đang quét radar trong bán kính 3km...");
    if (radiusCircle) {
      radiusCircle.setStyle({
        color: "#2563eb",
        fillColor: "#3b82f6",
        fillOpacity: 0.12,
        weight: 2.5,
        dashArray: "8, 6"
      });
      radiusCircle.bringToBack();
    }
    // Tự động định vị ngay lập tức vị trí thực tế của người dùng
    locateAndTrackDriver(false);
    startLocationWatch();
  } else {
    stopLocationWatch();
    triggerToast("🔴 Đã TẮT nhận đơn. Bạn đang ở trạng thái tạm nghỉ.");
    if (radiusCircle) {
      radiusCircle.setStyle({
        color: "#94a3b8",
        fillColor: "#94a3b8",
        fillOpacity: 0.05,
        weight: 1.5,
        dashArray: "6, 6"
      });
    }
    if (accuracyCircle && map) {
      map.removeLayer(accuracyCircle);
      accuracyCircle = null;
    }
  }
  nextTick(() => {
    map?.invalidateSize();
    renderOrderOnMap();
  });
};

// Xử lý chấp nhận đơn hàng nổ trong bán kính 3km
const handleAcceptIncomingOrder = async () => {
  if (!incomingOrder.value) return;
  if (incomingTimer) clearInterval(incomingTimer);
  showIncomingModal.value = false;

  const order = incomingOrder.value;
  const itemsText = Array.isArray(order.items)
    ? order.items.map((i: any) => `${i.name} (x${i.quantity})`).join(", ")
    : "Sản phẩm ZoneMart";

  activeOrder.value = {
    ...order,
    orderId: order.id,
    distanceKm: incomingDistance.value,
    items: itemsText
  };
  currentStep.value = "accepted";

  // Thông báo tới Buyer & Seller thời gian thực: "Tài xế đang đến quán lấy hàng"
  orderRealtimeService.acceptOrder(order.id, driverProfile);
  triggerToast(`Đã nhận đơn #${order.id}! Đang tự động định vị tuyến đường ngắn nhất đến Quán...`);

  await renderOrderOnMap();
};

const handleDeclineOrder = () => {
  if (incomingTimer) clearInterval(incomingTimer);
  showIncomingModal.value = false;
  if (incomingOrder.value) {
    ignoredOrderIds.add(incomingOrder.value.id);
  }
  incomingOrder.value = null;
  triggerToast("Đã bỏ qua đơn. Đơn hàng vẫn ở trạng thái chờ tài xế khác.");
};

// Xử lý luồng đơn hàng: 1. Đã lấy hàng thành công tại quán -> vẽ đường ngắn nhất tới Khách
const handleConfirmPicked = async () => {
  currentStep.value = "picked";
  if (activeOrder.value) {
    const orderId = activeOrder.value.id || activeOrder.value.orderId;
    // Thông báo tới Buyer thời gian thực: "Tài xế đang tới vị trí của bạn"
    orderRealtimeService.confirmOrderPicked(orderId);
  }
  triggerToast("Đã lấy đơn hàng thành công! Đang tự động định vị tuyến đường ngắn nhất đến Khách...");
  await renderOrderOnMap();
};

// Xử lý luồng đơn hàng: 2. Đã giao thành công tới khách hàng
const handleConfirmDelivered = () => {
  currentStep.value = "delivered";
  if (activeOrder.value) {
    const orderId = activeOrder.value.id || activeOrder.value.orderId;
    // Thông báo tới Buyer & Seller thời gian thực: "Giao thành công"
    orderRealtimeService.confirmOrderDelivered(orderId);

    const fee = activeOrder.value.shippingFee || 15000;
    shiftStats.value.todayEarnings += fee;
    shiftStats.value.completedOrders += 1;
    shiftStats.value.totalKm = Number((shiftStats.value.totalKm + (activeOrder.value.distanceKm || 1.5)).toFixed(1));

    // Ghi nhận vào lịch sử cuốc xe
    const now = new Date();
    const timeStr = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
    tripHistory.value.unshift({
      id: orderId,
      time: timeStr,
      storeName: activeOrder.value.store?.name || "ZoneMart Store",
      customerName: activeOrder.value.customer?.name || "Khách Hàng",
      shippingFee: fee,
      distanceKm: activeOrder.value.distanceKm || 1.5,
      status: "Giao thành công"
    });

    triggerToast(`🎉 Đã giao hàng thành công! +${fee.toLocaleString('vi-VN')} ₫ vào ví.`);
  }
  renderOrderOnMap();
};

const handleReportIssue = () => {
  const reason = prompt("Nhập lý do báo cáo sự cố (Khách không nhận / Shop đóng cửa):");
  if (reason) {
    toast.info(`Đã ghi nhận báo cáo: "${reason}". Tổng đài CSKH 1900 6868 sẽ hỗ trợ bạn ngay.`, "Báo Cáo Sự Cố");
    currentStep.value = "idle";
    renderOrderOnMap();
  }
};

watch(isOnline, () => {
  renderOrderOnMap();
});

// Xử lý Đăng Xuất An Toàn cho Tài Xế
const handleLogout = () => {
  if (isOnline.value) {
    isOnline.value = false;
  }
  auth.logout();
  toast.success("Đã đăng xuất tài khoản tài xế an toàn!", "Đăng Xuất");
  router.push({ path: "/login", query: { logout: "success" } });
};

onMounted(() => {
  const role = auth.currentRole.value;
  const rawUser = auth.currentUser.value as any;
  const hasShipperAccess = role === "shipper" || role === "admin" || rawUser?.isShipper === true || rawUser?.shipperStatus === "Approved";
  if (!hasShipperAccess) {
    router.replace("/403");
    return;
  }
  if (role !== "shipper" && role !== "admin" && (rawUser?.isShipper || rawUser?.shipperStatus === "Approved")) {
    auth.updateUser({ role: 'shipper' });
    localStorage.setItem('userRole', 'shipper');
  }

  // Kiểm tra tính hợp lệ của tài khoản với backend (nếu tài khoản đã bị Admin xóa)
  const emailToCheck = auth.currentUser.value?.phoneEmail || auth.currentUser.value?.phone;
  if (emailToCheck && role !== 'admin') {
    fetch(`http://localhost:5000/api/auth/verify-session?email=${encodeURIComponent(emailToCheck)}`)
    fetch(`/api/auth/verify-session?email=${encodeURIComponent(emailToCheck)}`)
      .then((res) => {
        if (res.status === 404 || res.status === 401) {
          auth.logout();
          toast.error("Tài khoản của bạn đã bị xóa khỏi hệ thống! Vui lòng liên hệ Ban Quản Trị.", "Tài Khoản Không Hợp Lệ");
          router.replace("/login");
        }
      })
      .catch(() => {});
  }

  loadDriverProfile();
  checkFaceStatus();
  initMap();

  // 1. Lắng nghe sự kiện quán đã XÁC NHẬN đơn hàng -> Sẵn sàng nổ đơn trong bán kính 3km
  unsubscribeOrderDispatch = orderRealtimeService.onOrderDispatchReady((order) => {
    triggerIncomingOrderCheck(order);
  });

  // 2. Lắng nghe trạng thái đơn chuyển sang 'confirmed'
  unsubscribeStatusChanged = orderRealtimeService.onOrderStatusChanged(({ status, order }) => {
    if (status === "confirmed" && order) {
      triggerIncomingOrderCheck(order);
    }
  });

  // Khóa cuộn trang toàn cục để thanh menu và nút công tắc trên di động không bao giờ bị trôi/mất
  document.documentElement.style.overflow = 'hidden';
  document.body.style.overflow = 'hidden';

  // Nếu đang mở hoạt động sẵn, tự động định vị vị trí người dùng & kiểm tra đơn chờ
  if (isOnline.value) {
    locateAndTrackDriver(true);
    startLocationWatch();
  }
});

onUnmounted(() => {
  document.documentElement.style.overflow = '';
  document.body.style.overflow = '';
  if (incomingTimer) clearInterval(incomingTimer);
  if (unsubscribeOrderDispatch) unsubscribeOrderDispatch();
  if (unsubscribeStatusChanged) unsubscribeStatusChanged();
  stopLocationWatch();
  if (map) {
    map.remove();
    map = null;
  }
});
</script>

<template>
  <div class="shipper-app-screen">
    <!-- Toast Popup -->
    <div v-if="showToast" class="toast-bar">
      <i class="bi bi-info-circle-fill"></i>
      <span>{{ toastMsg }}</span>
    </div>

    <!-- MODAL NỔ ĐƠN HÀNG HỎA TỐC BÁN KÍNH 3KM -->
    <transition name="modal-bounce">
      <div v-if="showIncomingModal && incomingOrder" class="incoming-order-overlay">
        <div class="incoming-order-modal">
          <div class="incoming-modal-header">
            <div class="incoming-badge-pulse">
              <span class="pulse-ring"></span>
              <i class="bi bi-bell-fill"></i>
            </div>
            <div class="incoming-title-box">
              <h3>🚨 NỔ ĐƠN HỎA TỐC BÁN KÍNH 3KM!</h3>
              <p>Quán cách bạn <strong>{{ incomingDistance }} km</strong> (Trong vòng bán kính 3km)</p>
            </div>
            <div class="countdown-badge">
              <i class="bi bi-stopwatch"></i> {{ incomingCountdown }}s
            </div>
          </div>

          <!-- Thanh đếm ngược 30s -->
          <div class="countdown-bar-container">
            <div class="countdown-bar-fill" :style="{ width: (incomingCountdown / 30 * 100) + '%' }"></div>
          </div>

          <div class="incoming-modal-body">
            <!-- Thù lao cước nhận -->
            <div class="fee-banner">
              <span class="fee-label">Thu nhập cuốc xe:</span>
              <strong class="fee-val">+{{ (incomingOrder.shippingFee || 15000).toLocaleString('vi-VN') }} ₫</strong>
            </div>

            <!-- Điểm lấy hàng -->
            <div class="loc-step-item">
              <div class="loc-icon bg-blue"><i class="bi bi-shop"></i></div>
              <div class="loc-details">
                <span class="loc-tag">1. Điểm lấy hàng (Cách bạn {{ incomingDistance }} km)</span>
                <h4 class="loc-name">{{ incomingOrder.store?.name || 'Quán ZoneMart' }}</h4>
                <p class="loc-address">{{ incomingOrder.store?.address || 'Khu vực Cầu Giấy, Hà Nội' }}</p>
              </div>
            </div>

            <!-- Điểm giao hàng -->
            <div class="loc-step-item">
              <div class="loc-icon bg-green"><i class="bi bi-geo-alt-fill"></i></div>
              <div class="loc-details">
                <span class="loc-tag">2. Điểm giao tới khách</span>
                <h4 class="loc-name">{{ incomingOrder.customer?.name || 'Khách Hàng' }} ({{ incomingOrder.customer?.phone || '' }})</h4>
                <p class="loc-address">{{ incomingOrder.customer?.address || 'Địa chỉ khách hàng' }}</p>
              </div>
            </div>

            <!-- Danh sách món hàng -->
            <div class="incoming-items-list" v-if="incomingOrder.items && incomingOrder.items.length > 0">
              <div class="items-head">
                <span>Món hàng cần giao ({{ incomingOrder.items.length }} món):</span>
              </div>
              <div class="incoming-item-row" v-for="(it, idx) in incomingOrder.items" :key="idx">
                <img v-if="it.image" :src="it.image" class="item-thumb" />
                <div v-else class="item-thumb fallback"><i class="bi bi-basket3"></i></div>
                <div class="item-info">
                  <span class="item-title">{{ it.name }}</span>
                  <span class="item-qty">Số lượng: x{{ it.quantity }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="incoming-modal-actions">
            <button type="button" class="btn-decline" @click="handleDeclineOrder">
              <i class="bi bi-x-circle me-1"></i> Bỏ qua
            </button>
            <button type="button" class="btn-accept" @click="handleAcceptIncomingOrder">
              <i class="bi bi-check2-circle me-1"></i> ĐỒNG Ý NHẬN ĐƠN ({{ incomingCountdown }}s)
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 1. FULLSCREEN LEAFLET MAP (TRÀN TOÀN BỘ MÀN HÌNH) -->
    <div class="fullscreen-map-wrapper">
      <div id="shipperMap" class="fullscreen-map" :class="{ 'map-dimmed': !isOnline }"></div>

      <!-- Offline Dim Overlay khi tạm dừng -->
      <div v-if="!isOnline && currentTab === 'map'" class="offline-map-overlay">
        <div class="offline-prompt-box">
          <div class="offline-icon-pulse">
            <i class="bi bi-power"></i>
          </div>
          <h3>Bạn Đang Tạm Nghỉ</h3>
          <p>Nhấn nút <strong>"BẬT HOẠT ĐỘNG"</strong> ở phía dưới để kích hoạt định vị GPS và nhận đơn hỏa tốc 3km.</p>
          <button class="btn btn-primary btn-turn-online" @click="toggleOnline">
            <i class="bi bi-lightning-charge-fill"></i> Bật Hoạt Động Ngay
          </button>
        </div>
      </div>
    </div>

    <!-- 2. TOP FLOATING BAR (TRÊN CÙNG BẢN ĐỒ) -->
    <header class="top-floating-bar" v-if="currentTab === 'map'">
      <!-- Left: Driver Mini Pill -->
      <div class="driver-mini-pill" @click="switchTab('profile')" title="Xem chi tiết hồ sơ tài xế">
        <div class="driver-pill-avatar">
          <img v-if="driverProfile.avatarUrl" :src="driverProfile.avatarUrl" class="avatar-img-circle" style="width: 100%; height: 100%; object-fit: cover; border-radius: 50%;" />
          <i v-else class="bi bi-bicycle text-success"></i>
          <span class="pill-dot" :class="{ 'online': isOnline }"></span>
        </div>
        <div class="driver-pill-info">
          <div class="pill-name-row">
            <strong class="pill-name">{{ driverProfile.fullName }}</strong>
            <span class="pill-rating"><i class="bi bi-star-fill text-warning"></i> 5.0</span>
          </div>
          <span class="pill-status-text">
            {{ isOnline ? "Trực tuyến • 3km" : "Ngoại tuyến • Nghỉ" }}
          </span>
        </div>
      </div>


      <!-- Right: Map Controls Stack (Traffic, Satellite, GPS Recenter) -->
      <div class="map-floating-actions">
        <!-- Live Traffic Toggle -->
        <button
          class="map-action-btn"
          :class="{ 'btn-active': showTraffic }"
          @click="toggleTraffic"
          title="Bật/Tắt dữ liệu Giao thông Google"
        >
          <i class="bi bi-stoplights-fill"></i>
          <span class="action-label">Giao thông</span>
        </button>

        <!-- Satellite Toggle -->
        <button
          class="map-action-btn"
          :class="{ 'btn-active': mapType === 'satellite' }"
          @click="toggleMapType"
          :title="mapType === 'roadmap' ? 'Xem Vệ tinh' : 'Xem Bản đồ'"
        >
          <i class="bi" :class="mapType === 'roadmap' ? 'bi-globe-asia-australia' : 'bi-map'"></i>
          <span class="action-label">{{ mapType === 'roadmap' ? 'Vệ tinh' : 'Bản đồ' }}</span>
        </button>

        <!-- Thu nhập & Cuốc xe -->
        <button
          class="map-action-btn btn-wallet"
          @click="switchTab('earnings')"
          title="Xem Thu Nhập & Lịch Sử Cuốc Xe"
        >
          <i class="bi bi-wallet2 text-blue"></i>
          <span class="action-label">Thu nhập</span>
        </button>

        <!-- GPS Recenter -->
        <button
          class="map-action-btn btn-gps"
          :class="{ 'is-locating': isLocating }"
          @click="recenterMap"
          title="Định vị GPS chính xác của tôi"
        >
          <i class="bi bi-crosshair"></i>
        </button>

        <!-- Nút Đăng Xuất Tài Xế -->
        <button
          class="map-action-btn btn-logout"
          @click="handleLogout"
          title="Đăng xuất khỏi tài khoản tài xế"
        >
          <i class="bi bi-box-arrow-right"></i>
          <span class="action-label">Đăng xuất</span>
        </button>
      </div>
    </header>

    <!-- 3. FLOATING ORDER / RADAR SCANNING CARD (NỔI TRÊN BẢN ĐỒ) -->
    <div v-if="currentTab === 'map'" class="floating-mission-container">
      <!-- Case A: Đang có đơn hàng hoạt động -->
      <div v-if="isOnline && currentStep !== 'idle' && currentStep !== 'delivered' && activeOrder" class="floating-order-card">
        <div class="f-order-header">
          <span class="f-order-badge">
            <i class="bi bi-lightning-charge-fill"></i> HỎA TỐC 3KM
          </span>
          <span class="f-order-fee">Thù lao: <strong>{{ activeOrder.shippingFee.toLocaleString('vi-VN') }} ₫</strong></span>
        </div>

        <!-- Step tracker -->
        <div class="f-step-tracker">
          <div class="f-step" :class="{ 'done': currentStep === 'accepted' || currentStep === 'picked', 'current': currentStep === 'accepted' }">
            <span class="f-step-num">1</span>
            <span class="f-step-txt">Lấy tại Shop</span>
          </div>
          <div class="f-step-arrow">➜</div>
          <div class="f-step" :class="{ 'done': currentStep === 'picked', 'current': currentStep === 'picked' }">
            <span class="f-step-num">2</span>
            <span class="f-step-txt">Giao cho Khách</span>
          </div>
        </div>

        <!-- Step 1: Đi lấy tại Shop -->
        <div v-if="currentStep === 'accepted'" class="f-step-content">
          <div class="f-location-title">
            <i class="bi bi-shop-window text-blue"></i>
            <div>
              <strong>{{ activeOrder.store.name }}</strong>
              <small class="f-addr">{{ activeOrder.store.address }}</small>
            </div>
          </div>
          <div class="f-items-summary">
            <i class="bi bi-box-seam me-1" aria-hidden="true"></i> <em>{{ activeOrder.items }}</em>
          </div>
          <div class="f-actions-row">
            <a :href="'tel:' + activeOrder.store.phone" class="btn btn-call">
              <i class="bi bi-telephone-fill"></i> Gọi Quán
            </a>
            <button class="btn btn-primary flex-1" @click="handleConfirmPicked">
              <i class="bi bi-box-arrow-in-down me-1" aria-hidden="true"></i> ĐÃ LẤY ĐƠN HÀNG THÀNH CÔNG <i class="bi bi-arrow-right ms-1" aria-hidden="true"></i>
            </button>
          </div>
        </div>

        <!-- Step 2: Giao cho Khách -->
        <div v-else-if="currentStep === 'picked'" class="f-step-content">
          <div class="f-location-title">
            <i class="bi bi-person-fill text-green"></i>
            <div>
              <strong>{{ activeOrder.customer.name }}</strong>
              <small class="f-addr">{{ activeOrder.customer.address }}</small>
            </div>
          </div>
          <div class="f-items-summary" v-if="activeOrder.notes">
            <i class="bi bi-chat-left-dots me-1" aria-hidden="true"></i> Ghi chú: <em>"{{ activeOrder.notes }}"</em>
          </div>
          <div class="f-actions-row">
            <a :href="'tel:' + activeOrder.customer.phone" class="btn btn-call">
              <i class="bi bi-telephone-fill"></i> Gọi Khách
            </a>
            <button class="btn btn-success flex-1" @click="handleConfirmDelivered">
              <i class="bi bi-check2-circle me-1" aria-hidden="true"></i> GIAO HÀNG THÀNH CÔNG
            </button>
          </div>
          <button class="btn-report-link" @click="handleReportIssue">
            <i class="bi bi-exclamation-triangle-fill text-warning me-1" aria-hidden="true"></i> Báo cáo sự cố / Khách không nhận
          </button>
        </div>
      </div>

      <!-- Case B: Vừa giao xong -->
      <div v-else-if="isOnline && currentStep === 'delivered' && activeOrder" class="floating-order-card delivered-card">
        <div class="delivered-head">
          <i class="bi bi-check-circle-fill"></i>
          <div>
            <strong>Giao Hàng Thành Công!</strong>
            <p>Đã cộng <strong>+{{ activeOrder.shippingFee.toLocaleString('vi-VN') }} ₫</strong> vào ví tài xế.</p>
          </div>
        </div>
        <button class="btn btn-primary w-100" @click="currentStep = 'idle'">
          Hoàn tất cuốc xe
        </button>
      </div>

      <!-- Case C: Đang tìm đơn (Radar quét đơn hỏa tốc 3km) -->
      <div v-else-if="isOnline" class="floating-radar-chip">
        <div class="radar-dot-pulse"></div>
        <div class="radar-info">
          <strong>Đang Quét Đơn Hỏa Tốc (3km)...</strong>
          <small>Hệ thống tự động nổ chuông khi có quán xác nhận</small>
        </div>
      </div>
    </div>

    <!-- 4. NÚT CÔNG TẮC BẬT / TẮT HOẠT ĐỘNG (MASTER TOGGLE BUTTON) -->
    <div v-if="currentTab === 'map'" class="floating-toggle-bar">
      <button 
        type="button" 
        class="master-toggle-btn" 
        :class="{ 'is-online': isOnline }"
        @click="toggleOnline"
        :title="isOnline ? 'Nhấn để Tắt hoạt động' : 'Nhấn để Bật hoạt động'"
      >
        <span class="toggle-knob">
          <i class="bi" :class="isOnline ? 'bi-lightning-charge-fill' : 'bi-power'"></i>
        </span>
        <span class="toggle-text">
          {{ isOnline ? 'TẮT HOẠT ĐỘNG' : 'BẬT HOẠT ĐỘNG' }}
        </span>
      </button>
    </div>

    <!-- 5. SLIDING OVERLAY PANEL CHO CÁC PHẦN: THU NHẬP / CUỐC XE / HỒ SƠ -->
    <div v-if="currentTab !== 'map'" class="sub-tab-panel-overlay">
      <div class="panel-body-content">
        <!-- TAB 1: THU NHẬP -->
        <div v-if="currentTab === 'earnings'" class="tab-earnings-wrapper">
          <!-- 4 Thẻ Thống Kê -->
          <div class="shift-stats-row">
            <div class="stat-card">
              <div class="stat-icon-wrap bg-green"><i class="bi bi-cash-stack"></i></div>
              <div>
                <span class="stat-caption">Thu nhập hôm nay</span>
                <strong class="stat-figure text-green">{{ shiftStats.todayEarnings.toLocaleString('vi-VN') }} ₫</strong>
              </div>
            </div>
            <div class="stat-card">
              <div class="stat-icon-wrap bg-blue"><i class="bi bi-box-seam-fill"></i></div>
              <div>
                <span class="stat-caption">Số cuốc hoàn tất</span>
                <strong class="stat-figure">{{ shiftStats.completedOrders }} đơn</strong>
              </div>
            </div>
            <div class="stat-card">
              <div class="stat-icon-wrap bg-orange"><i class="bi bi-clock-history"></i></div>
              <div>
                <span class="stat-caption">Thời gian Online</span>
                <strong class="stat-figure">{{ shiftStats.onlineHours }}</strong>
              </div>
            </div>
            <div class="stat-card">
              <div class="stat-icon-wrap bg-purple"><i class="bi bi-speedometer2"></i></div>
              <div>
                <span class="stat-caption">Quãng đường đã chạy</span>
                <strong class="stat-figure">{{ shiftStats.totalKm }} km</strong>
              </div>
            </div>
          </div>

          <!-- Ví tài xế -->
          <div class="wallet-card">
            <div class="wallet-header">
              <div>
                <span class="wallet-subtitle">VÍ TÀI XẾ ZONEMART</span>
                <h3 class="wallet-balance">{{ shiftStats.todayEarnings.toLocaleString('vi-VN') }} ₫</h3>
              </div>
              <div class="wallet-icon-box">
                <i class="bi bi-credit-card-2-front-fill"></i>
              </div>
            </div>

            <div class="wallet-meta">
              <span>Số dư khả dụng: <strong>{{ shiftStats.todayEarnings.toLocaleString('vi-VN') }} ₫</strong></span>
              <span>Hạn mức ký quỹ: <strong>100.000 ₫</strong></span>
            </div>

            <div class="wallet-actions">
              <button class="btn btn-primary flex-1" @click="triggerToast('Yêu cầu rút tiền: Đã gửi lệnh chuyển tiền về tài khoản ngân hàng liên kết!')">
                <i class="bi bi-bank"></i> Rút Về Ngân Hàng
              </button>
              <button class="btn btn-outline-primary" @click="triggerToast('Nạp ví: Mở mã QR ngân hàng để nạp tiền ký quỹ 24/7')">
                <i class="bi bi-plus-circle"></i> Nạp Ví
              </button>
            </div>
          </div>
        </div>

        <!-- TAB 2: CUỐC XE -->
        <div v-else-if="currentTab === 'trips'" class="tab-trips-wrapper">
          <div class="trips-history-card">
            <div class="trips-header">
              <h3><i class="bi bi-journal-text"></i> Danh Sách Cuốc Xe Trong Ngày</h3>
              <span class="trips-count">{{ tripHistory.length }} cuốc</span>
            </div>

            <!-- Empty State -->
            <div v-if="tripHistory.length === 0" class="empty-trips-box">
              <div class="empty-icon-circle"><i class="bi bi-inbox"></i></div>
              <h4>Chưa có cuốc xe nào hôm nay</h4>
              <p>Toàn bộ đơn hoàn thành trong ca làm việc sẽ được lưu chi tiết tiền thù lao và địa chỉ tại đây.</p>
              <button class="btn btn-outline-primary" @click="switchTab('map')">
                <i class="bi bi-geo-alt-fill"></i> Sang Bản Đồ Để Nhận Đơn
              </button>
            </div>

            <!-- Trips list -->
            <div v-else class="trips-list">
              <div v-for="trip in tripHistory" :key="trip.id" class="trip-log-item">
                <div class="trip-time-col">
                  <span class="trip-time">{{ trip.time }}</span>
                  <span class="trip-id">{{ trip.id }}</span>
                </div>
                <div class="trip-route-col">
                  <div class="trip-point">
                    <span class="dot-shop"></span>
                    <span class="point-name">{{ trip.storeName }}</span>
                  </div>
                  <div class="trip-point">
                    <span class="dot-customer"></span>
                    <span class="point-name">{{ trip.customerName }}</span>
                  </div>
                </div>
                <div class="trip-meta-col">
                  <span class="trip-dist">{{ trip.distanceKm }} km</span>
                  <strong class="trip-fee">+{{ trip.shippingFee.toLocaleString('vi-VN') }} ₫</strong>
                  <span class="trip-status-badge">{{ trip.status }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 3: HỒ SƠ TÀI XẾ -->
        <div v-else-if="currentTab === 'profile'" class="tab-profile-wrapper">
          <div class="profile-card">
            <!-- 1. Hàng thông tin đại diện -->
            <div class="profile-avatar-row">
              <div class="p-avatar">
                <img v-if="driverProfile.avatarUrl" :src="driverProfile.avatarUrl" style="width: 100%; height: 100%; object-fit: cover; border-radius: 50%;" />
                <i v-else class="bi bi-bicycle"></i>
              </div>
              <div class="p-info">
                <h3>{{ driverProfile.fullName }}</h3>
                <p>Tài xế Hỏa Tốc • Mã ID: <b style="color: #0284C7;">#{{ driverProfile.shipperCode }}</b> • <i class="bi bi-star-fill text-warning"></i> 5.0</p>
                <span class="verified-badge"><i class="bi bi-patch-check-fill text-primary me-1"></i> Đã xác thực CCCD & Bằng lái</span>
              </div>
            </div>

            <!-- 2. Lưới thông tin phương tiện & khu vực (Gọn gàng 2x2) -->
            <div class="profile-details-grid">
              <div class="detail-item">
                <span class="dt-label">Phương tiện</span>
                <strong>{{ driverProfile.vehicleModel || driverProfile.vehicleType || "Xe máy" }}</strong>
              </div>
              <div class="detail-item">
                <span class="dt-label">Biển số</span>
                <strong>{{ driverProfile.licensePlate || "Chưa cập nhật" }}</strong>
              </div>
              <div class="detail-item">
                <span class="dt-label">Khu vực</span>
                <strong :title="driverProfile.operatingArea">{{ driverProfile.operatingArea ? (driverProfile.operatingArea.length > 28 ? driverProfile.operatingArea.slice(0, 25) + '...' : driverProfile.operatingArea) : "Quận Cầu Giấy (5km)" }}</strong>
              </div>
              <div class="detail-item">
                <span class="dt-label">Trạng thái</span>
                <strong class="text-green">{{ driverProfile.status === "Approved" ? "Đã duyệt" : "Chờ thẩm định" }}</strong>
              </div>
            </div>

            <!-- 3. Cài Đặt Sinh Trắc Học Face-ID (gọn gàng 1 khối duy nhất) -->
            <div class="shipper-face-card-compact">
              <div class="face-compact-left">
                <div class="face-symbol-badge-sm" :class="{ 'is-active': isFaceAuthEnabled }">
                  <i class="bi bi-person-bounding-box"></i>
                </div>
                <div class="face-text-group">
                  <div class="face-title-sm">Bảo Mật AI Face-ID UniFace</div>
                  <div class="face-desc-sm">
                    <span v-if="isFaceAuthEnabled" class="text-success fw-bold">
                      <i class="bi bi-shield-check"></i> Đã kích hoạt 1-chạm
                    </span>
                    <span v-else class="text-muted">
                      Đăng nhập 1-chạm không mật khẩu
                    </span>
                  </div>
                </div>
              </div>
              <div class="face-compact-actions">
                <template v-if="isFaceAuthEnabled">
                  <button type="button" class="btn-face-rescan-sm" @click="openFaceRegisterModal" title="Quét cập nhật lại khuôn mặt">
                    <i class="bi bi-arrow-clockwise"></i> Quét lại
                  </button>
                  <button type="button" class="btn-face-disable-sm" :disabled="isDisablingFace" @click="handleDisableFace" title="Hủy kích hoạt Face ID">
                    <i class="bi bi-x-circle"></i> Tắt
                  </button>
                </template>
                <template v-else>
                  <button type="button" class="btn-face-activate-sm" @click="openFaceRegisterModal">
                    <i class="bi bi-camera-fill me-1"></i> Bật Face ID
                  </button>
                </template>
              </div>
            </div>

            <!-- 4. Tổng đài hỗ trợ 24/7 (gọn gàng 1 dòng) -->
            <a href="tel:19006868" class="compact-hotline-bar" title="Gọi tổng đài hỗ trợ tài xế">
              <div class="hotline-left">
                <i class="bi bi-headset hotline-icon"></i>
                <span>Tổng đài hỗ trợ sự cố 24/7: <strong>1900 6868</strong></span>
              </div>
              <span class="hotline-badge-call"><i class="bi bi-telephone-outbound-fill me-1"></i> Gọi ngay</span>
            </a>

            <!-- 5. Nút Đăng Xuất Tài Xế Nổi Bật Dễ Thấy -->
            <div class="profile-logout-box">
              <button type="button" class="btn btn-shipper-logout" @click="handleLogout">
                <i class="bi bi-box-arrow-right me-2"></i> ĐĂNG XUẤT TÀI KHOẢN TÀI XẾ
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal Quét & Kích Hoạt Khuôn Mặt (Face ID UniFace) cho Shipper -->
    <FaceScanModal
      v-model="showFaceModal"
      mode="register"
      :user-id="getShipperUserIdentifier()"
      :user-name="driverProfile.fullName || 'Tài Xế ZoneMart'"
      @success="handleFaceRegisterSuccess"
    />

    <!-- 6. THANH ĐIỀU HƯỚNG ĐÁY MÀN HÌNH TRÊN ĐIỆN THOẠI (MOBILE BOTTOM NAVIGATION BAR) -->
    <nav class="shipper-bottom-nav">
      <button
        type="button"
        class="bottom-nav-item"
        :class="{ active: currentTab === 'map' }"
        @click="switchTab('map')"
      >
        <div class="bottom-nav-icon-wrap">
          <i class="bi bi-map-fill"></i>
        </div>
        <span>Bản đồ</span>
      </button>

      <button
        type="button"
        class="bottom-nav-item"
        :class="{ active: currentTab === 'trips' }"
        @click="switchTab('trips')"
      >
        <div class="bottom-nav-icon-wrap">
          <i class="bi bi-box-seam-fill"></i>
          <span class="bottom-nav-badge" v-if="activeOrder">1</span>
          <span class="bottom-nav-badge" v-else-if="tripHistory.length > 0">{{ tripHistory.length }}</span>
        </div>
        <span>Cuốc xe</span>
      </button>

      <button
        type="button"
        class="bottom-nav-item"
        :class="{ active: currentTab === 'earnings' }"
        @click="switchTab('earnings')"
      >
        <div class="bottom-nav-icon-wrap">
          <i class="bi bi-wallet2"></i>
        </div>
        <span>Thu nhập</span>
      </button>

      <button
        type="button"
        class="bottom-nav-item"
        :class="{ active: currentTab === 'profile' }"
        @click="switchTab('profile')"
      >
        <div class="bottom-nav-icon-wrap">
          <i class="bi bi-person-circle"></i>
        </div>
        <span>Tài khoản</span>
      </button>
    </nav>
  </div>
</template>

<style scoped>
/* ==========================================================================
   GLOBAL FULLSCREEN DRIVER APP LAYOUT
   ========================================================================== */
.shipper-app-screen {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: #e5e3df;
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  color: #0f172a;
  user-select: none;
  z-index: 100;
}

/* Bottom Navigation Bar (Hiện ở cả máy tính & điện thoại) */
.shipper-bottom-nav {
  display: flex;
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  height: 58px;
  background: #ffffff;
  border-top: 1.5px solid #e2e8f0;
  box-shadow: 0 -4px 18px rgba(0, 0, 0, 0.08);
  z-index: 999;
  align-items: center;
  justify-content: space-around;
  padding: 0 4px;
}

@media (min-width: 769px) {
  /* 1. NÂNG CẤP THANH MENU TRÊN MÁY TÍNH TO, RÕ NÉT, DẠNG DOCK NỔI HIỆN ĐẠI */
  .shipper-bottom-nav {
    max-width: 560px;
    height: 64px;
    bottom: 16px;
    left: 50%;
    right: auto;
    transform: translateX(-50%);
    border-radius: 20px;
    border: 1.5px solid rgba(203, 213, 225, 0.9);
    background: rgba(255, 255, 255, 0.96);
    backdrop-filter: blur(16px);
    box-shadow: 0 16px 40px -8px rgba(15, 23, 42, 0.18), 0 4px 12px rgba(0, 0, 0, 0.05);
    padding: 0 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    overflow: hidden;
  }

  .bottom-nav-item {
    flex: 1;
    max-width: 120px !important;
    height: 48px !important;
    margin: 0 auto !important;
    font-size: 12px !important;
    font-weight: 700 !important;
    gap: 2px !important;
    padding: 4px 8px !important;
    border-radius: 12px !important;
    transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1) !important;
    display: flex !important;
    flex-direction: column !important;
    align-items: center !important;
    justify-content: center !important;
  }

  .bottom-nav-item:hover {
    background: #f1f5f9 !important;
    color: #0f172a !important;
  }

  .bottom-nav-item.active {
    background: #eff6ff !important;
    color: #2563eb !important;
  }

  .bottom-nav-item i {
    font-size: 20px !important;
    line-height: 1 !important;
  }

  /* 2. TÁCH NÚT BẬT/TẮT HOẠT ĐỘNG TRÊN MÁY TÍNH LÊN TRÊN THANH MENU, KHÔNG ĐÈ LÊN NHAU */
  .floating-toggle-bar {
    position: fixed !important;
    bottom: 96px !important;
    left: 0 !important;
    right: 0 !important;
    z-index: 1002 !important;
    display: flex !important;
    justify-content: center !important;
    pointer-events: none !important;
  }

  .master-toggle-btn {
    width: 250px !important;
    height: 52px !important;
    box-shadow: 0 12px 30px rgba(15, 23, 42, 0.3) !important;
  }

  .master-toggle-btn .toggle-text {
    font-size: 14px !important;
  }

  /* CÁC TAB PHỤ PHỦ TOÀN BỘ MÀN HÌNH - KHÔNG BỊ LỘ BẢN ĐỒ DƯỚI */
  .sub-tab-panel-overlay {
    position: fixed !important;
    inset: 0 !important;
    top: 0 !important;
    left: 0 !important;
    right: 0 !important;
    bottom: 0 !important;
    z-index: 100 !important;
    background: #f8fafc !important;
  }

  /* Mission card nổi trên máy tính */
  .floating-mission-container {
    bottom: 165px !important;
  }
}

.bottom-nav-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  background: transparent;
  border: none;
  color: #64748b;
  font-size: 11px;
  font-weight: 700;
  padding: 6px 2px;
  cursor: pointer;
  text-decoration: none;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.bottom-nav-item i {
  font-size: 19px;
  line-height: 1;
  transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.bottom-nav-item.active {
  color: #2563eb;
  font-weight: 800;
}

.bottom-nav-item.active i {
  color: #2563eb;
  transform: scale(1.15);
}

.bottom-nav-icon-wrap {
  position: relative;
  display: inline-flex;
}

.bottom-nav-badge {
  position: absolute;
  top: -5px;
  right: -10px;
  background: #ef4444;
  color: #ffffff;
  font-size: 9px;
  font-weight: 800;
  min-width: 16px;
  height: 16px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 3px;
  box-shadow: 0 2px 5px rgba(239, 68, 68, 0.4);
}

/* Toast Bar */
.toast-bar {
  position: fixed;
  top: 20px;
  left: 50%;
  transform: translateX(-50%);
  background-color: rgba(15, 23, 42, 0.94);
  backdrop-filter: blur(8px);
  color: #ffffff;
  padding: 10px 20px;
  border-radius: 999px;
  font-size: 13px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 8px;
  z-index: 99999;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.25);
  animation: slideToast 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  white-space: nowrap;
}

@keyframes slideToast {
  from { transform: translate(-50%, -20px); opacity: 0; }
  to { transform: translate(-50%, 0); opacity: 1; }
}

/* ==========================================================================
   1. FULLSCREEN LEAFLET MAP
   ========================================================================== */
.fullscreen-map-wrapper {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  z-index: 1;
}

.fullscreen-map {
  width: 100%;
  height: 100%;
  z-index: 1;
  transition: filter 0.35s ease;
}

.fullscreen-map.map-dimmed {
  filter: grayscale(70%) brightness(0.92);
}

/* Offline Dim Overlay */
.offline-map-overlay {
  position: absolute;
  inset: 0;
  z-index: 5;
  background: rgba(15, 23, 42, 0.45);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  pointer-events: auto;
}

.offline-prompt-box {
  background: #ffffff;
  padding: 32px 28px;
  border-radius: 26px;
  max-width: 380px;
  width: 100%;
  text-align: center;
  box-shadow: 0 20px 45px -10px rgba(0, 0, 0, 0.3);
  animation: popIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  display: flex;
  flex-direction: column;
  align-items: center;
}

@keyframes popIn {
  from { transform: scale(0.92); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

.offline-icon-pulse {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: #f1f5f9;
  color: #64748b;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 26px;
  margin-bottom: 14px;
}

.offline-prompt-box h3 {
  font-size: 18px;
  font-weight: 900;
  color: #0f172a;
  margin: 0 0 8px 0;
}

.offline-prompt-box p {
  font-size: 13px;
  color: #64748b;
  line-height: 1.5;
  margin: 0 0 20px 0;
}

.btn-turn-online {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  padding: 13px 20px;
  border-radius: 14px;
  background: #22c55e;
  color: #ffffff;
  font-size: 14.5px;
  font-weight: 800;
  border: none;
  cursor: pointer;
  box-shadow: 0 4px 16px rgba(34, 197, 94, 0.4);
  transition: all 0.2s;
}

.btn-turn-online:hover {
  background: #16a34a;
  transform: translateY(-1px);
}

/* ==========================================================================
   2. TOP FLOATING BAR (TRÊN CÙNG BẢN ĐỒ)
   ========================================================================== */
.top-floating-bar {
  position: absolute;
  top: 14px;
  left: 14px;
  right: 14px;
  z-index: 50;
  display: flex;
  justify-content: space-between;
  align-items: center;
  pointer-events: none;
  gap: 12px;
}

/* Mini Driver Pill */
.driver-mini-pill {
  pointer-events: auto;
  display: flex;
  align-items: center;
  gap: 10px;
  background: rgba(255, 255, 255, 0.94);
  backdrop-filter: blur(12px);
  padding: 6px 14px 6px 8px;
  border-radius: 999px;
  box-shadow: 0 4px 18px rgba(0, 0, 0, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.9);
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.driver-mini-pill:hover {
  transform: scale(1.02);
  background: #ffffff;
}

.driver-pill-avatar {
  position: relative;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #fff7ed;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  flex-shrink: 0;
}

.pill-dot {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #ef4444;
  border: 2px solid #ffffff;
}

.pill-dot.online {
  background: #22c55e;
  box-shadow: 0 0 0 2px rgba(34, 197, 94, 0.3);
  animation: pulseDot 2s infinite;
}

.driver-pill-info {
  display: flex;
  flex-direction: column;
}

.pill-name-row {
  display: flex;
  align-items: center;
  gap: 6px;
}

.pill-name {
  font-size: 13.5px;
  font-weight: 800;
  color: #0f172a;
}

.pill-rating {
  font-size: 10.5px;
  font-weight: 800;
  color: #d97706;
  background: #fefce8;
  padding: 1px 6px;
  border-radius: 8px;
}

.pill-status-text {
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
}

/* Right Map Actions */
.map-floating-actions {
  pointer-events: auto;
  display: flex;
  align-items: center;
  gap: 8px;
}

.map-action-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  height: 38px;
  padding: 0 12px;
  background: rgba(255, 255, 255, 0.94);
  backdrop-filter: blur(12px);
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.9);
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.08);
  font-size: 12.5px;
  font-weight: 700;
  color: #334155;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.map-action-btn:hover {
  background: #ffffff;
  color: #0f172a;
}

.map-action-btn.btn-active {
  background: #eff6ff;
  color: #2563eb;
  border-color: #bfdbfe;
}

.map-action-btn.btn-gps {
  width: 38px;
  padding: 0;
  justify-content: center;
  font-size: 18px;
}

.map-action-btn.btn-gps.is-locating {
  color: #2563eb;
  animation: spin 1s infinite linear;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* ==========================================================================
   3. FLOATING ORDER / RADAR SCANNING CARD (TRÊN BẢN ĐỒ)
   ========================================================================== */
.floating-mission-container {
  position: absolute;
  bottom: 96px;
  left: 14px;
  right: 14px;
  z-index: 45;
  display: flex;
  justify-content: center;
  pointer-events: none;
}

.floating-order-card {
  pointer-events: auto;
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(14px);
  border-radius: 20px;
  padding: 16px 18px;
  max-width: 440px;
  width: 100%;
  box-shadow: 0 12px 36px -4px rgba(0, 0, 0, 0.18);
  border: 1px solid rgba(255, 255, 255, 0.9);
  animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  display: flex;
  flex-direction: column;
  gap: 12px;
}

@keyframes slideUp {
  from { transform: translateY(20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

.f-order-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.f-order-badge {
  font-size: 11px;
  font-weight: 800;
  background: #fff7ed;
  color: #ea580c;
  padding: 3px 8px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.f-order-fee {
  font-size: 12.5px;
  color: #475569;
}

.f-order-fee strong {
  color: #16a34a;
  font-weight: 900;
  font-size: 16px;
}

.f-step-tracker {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #f8fafc;
  padding: 8px 12px;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
}

.f-step {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 700;
  color: #94a3b8;
}

.f-step.current {
  color: #2563eb;
}

.f-step.done {
  color: #16a34a;
}

.f-step-num {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #e2e8f0;
  color: #64748b;
  font-size: 11px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
}

.f-step.current .f-step-num {
  background: #2563eb;
  color: #ffffff;
}

.f-step.done .f-step-num {
  background: #16a34a;
  color: #ffffff;
}

.f-step-arrow {
  color: #cbd5e1;
  font-size: 12px;
}

.f-location-title {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.f-location-title i {
  font-size: 20px;
  flex-shrink: 0;
  margin-top: 2px;
}

.f-location-title strong {
  display: block;
  font-size: 14.5px;
  font-weight: 800;
  color: #0f172a;
  line-height: 1.3;
}

.f-addr {
  display: block;
  font-size: 12px;
  color: #64748b;
  margin-top: 2px;
  line-height: 1.4;
}

.f-items-summary {
  font-size: 12.5px;
  color: #334155;
  background: #f1f5f9;
  padding: 8px 10px;
  border-radius: 10px;
}

.f-actions-row {
  display: flex;
  gap: 8px;
}

.f-actions-row .btn-call {
  padding: 10px 14px;
  border-radius: 12px;
  border: 1px solid #cbd5e1;
  background: #ffffff;
  color: #0f172a;
  font-weight: 700;
  font-size: 13px;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.f-actions-row .btn-primary {
  padding: 10px 14px;
  border-radius: 12px;
  font-weight: 800;
  font-size: 13.5px;
  background: #2563eb;
  color: #ffffff;
  border: none;
  cursor: pointer;
}

.f-actions-row .btn-success {
  padding: 10px 14px;
  border-radius: 12px;
  font-weight: 800;
  font-size: 13.5px;
  background: #16a34a;
  color: #ffffff;
  border: none;
  cursor: pointer;
}

/* Delivered Card */
.delivered-card {
  text-align: center;
}

.delivered-head {
  display: flex;
  align-items: center;
  gap: 12px;
  text-align: left;
}

.delivered-head i {
  font-size: 36px;
  color: #16a34a;
  flex-shrink: 0;
}

.delivered-head strong {
  display: block;
  font-size: 15px;
  color: #0f172a;
}

.delivered-head p {
  margin: 0;
  font-size: 12.5px;
  color: #64748b;
}

/* Floating Radar Chip */
.floating-radar-chip {
  pointer-events: auto;
  background: rgba(15, 23, 42, 0.9);
  backdrop-filter: blur(12px);
  color: #ffffff;
  padding: 9px 14px 9px 12px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  gap: 10px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.25);
  border: 1px solid rgba(255, 255, 255, 0.15);
  max-width: 440px;
  width: 100%;
}

.radar-dot-pulse {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #22c55e;
  box-shadow: 0 0 0 4px rgba(34, 197, 94, 0.3);
  animation: pulseDot 1.6s infinite;
  flex-shrink: 0;
}

.radar-info {
  display: flex;
  flex-direction: column;
}

.radar-info strong {
  font-size: 12.5px;
  font-weight: 700;
  color: #f8fafc;
}

.radar-info small {
  font-size: 10.5px;
  color: #94a3b8;
}

/* ==========================================================================
   4. NÚT CÔNG TẮC BẬT / TẮT HOẠT ĐỘNG (MASTER TOGGLE BUTTON)
   ========================================================================== */
.floating-toggle-bar {
  position: fixed;
  bottom: 72px;
  left: 0;
  right: 0;
  z-index: 1002;
  display: flex;
  justify-content: center;
  padding: 0 16px;
  pointer-events: none;
}

.master-toggle-btn {
  pointer-events: auto;
  position: relative;
  display: flex;
  align-items: center;
  width: 250px;
  height: 52px;
  border-radius: 999px;
  border: 2px solid rgba(255, 255, 255, 0.25);
  background: #1e293b;
  color: #ffffff;
  cursor: pointer;
  box-shadow: 0 10px 28px -4px rgba(15, 23, 42, 0.35), 0 3px 10px rgba(0, 0, 0, 0.1);
  transition: all 0.35s cubic-bezier(0.16, 1, 0.3, 1);
  user-select: none;
  overflow: hidden;
  padding: 0;
}

.master-toggle-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 14px 32px -4px rgba(15, 23, 42, 0.4);
}

.master-toggle-btn:active {
  transform: translateY(0) scale(0.97);
}

.master-toggle-btn.is-online {
  background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
  border-color: #86efac;
  box-shadow: 0 10px 28px -2px rgba(34, 197, 94, 0.48), 0 3px 10px rgba(22, 163, 74, 0.25);
}

.toggle-knob {
  position: absolute;
  top: 5px;
  left: 6px;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: #ffffff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  color: #64748b;
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), color 0.3s;
  z-index: 2;
}

.master-toggle-btn.is-online .toggle-knob {
  transform: translateX(196px);
  color: #16a34a;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}

.toggle-text {
  width: 100%;
  text-align: center;
  font-size: 14.5px;
  font-weight: 800;
  letter-spacing: 0.6px;
  color: #ffffff;
  transition: padding 0.35s cubic-bezier(0.16, 1, 0.3, 1);
  z-index: 1;
  padding-left: 36px;
}

.master-toggle-btn.is-online .toggle-text {
  padding-left: 0;
  padding-right: 36px;
}

/* ==========================================================================
   6. SUB-TAB SLIDING OVERLAY (THU NHẬP / CUỐC XE / HỒ SƠ)
   ========================================================================== */
.sub-tab-panel-overlay {
  position: fixed;
  inset: 0;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 100;
  background: #f8fafc;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  animation: slideUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  -webkit-overflow-scrolling: touch;
}

.panel-body-content {
  padding: 24px 16px;
  padding-bottom: 120px !important; /* Đảm bảo cuộn hết cỡ vẫn thoáng 120px, không bị dock che */
  max-width: 680px;
  margin: 0 auto;
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.tab-earnings-wrapper,
.tab-trips-wrapper,
.tab-profile-wrapper {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

/* ==================== HỒ SƠ TÀI XẾ (PROFILE CARD) TINH GỌN ==================== */
.profile-card {
  background: #ffffff;
  border-radius: 18px;
  padding: 16px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 18px rgba(0, 0, 0, 0.04);
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* 1. Hàng Avatar & Tên Tài Xế */
.profile-avatar-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid #f1f5f9;
}

.p-avatar {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  background: #fff7ed;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  flex-shrink: 0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  border: 2px solid #ffffff;
}

.p-info h3 {
  font-size: 16px;
  font-weight: 800;
  margin: 0 0 2px 0;
  color: #0f172a;
}

.p-info p {
  font-size: 11.5px;
  color: #64748b;
  margin: 0 0 4px 0;
}

.verified-badge {
  display: inline-flex;
  align-items: center;
  font-size: 10.5px;
  font-weight: 700;
  color: #16a34a;
  background: #dcfce7;
  padding: 2px 7px;
  border-radius: 5px;
}

/* 2. Lưới thông tin phương tiện & khu vực (Gọn 2x2) */
.profile-details-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.detail-item {
  background: #f8fafc;
  padding: 8px 10px;
  border-radius: 10px;
  border: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.dt-label {
  font-size: 10px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.detail-item strong {
  font-size: 12.5px;
  font-weight: 800;
  color: #0f172a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 3. Khối Face-ID UniFace tinh gọn */
.shipper-face-card-compact {
  background: #f8fafc;
  border-radius: 12px;
  border: 1.5px solid #e2e8f0;
  padding: 10px 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.face-compact-left {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.face-symbol-badge-sm {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: #e2e8f0;
  color: #64748b;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  flex-shrink: 0;
}

.face-symbol-badge-sm.is-active {
  background: linear-gradient(135deg, #059669, #10b981);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.25);
}

.face-text-group {
  min-width: 0;
}

.face-title-sm {
  font-size: 12.5px;
  font-weight: 800;
  color: #0f172a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.face-desc-sm {
  font-size: 11px;
  color: #64748b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.face-compact-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.btn-face-activate-sm {
  background: linear-gradient(135deg, #0284c7, #2563eb);
  color: #ffffff;
  border: none;
  font-size: 11.5px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  white-space: nowrap;
  box-shadow: 0 2px 6px rgba(37, 99, 235, 0.25);
}

.btn-face-activate-sm:hover {
  filter: brightness(1.08);
}

.btn-face-rescan-sm {
  background: #ffffff;
  color: #334155;
  border: 1px solid #cbd5e1;
  font-size: 11px;
  font-weight: 700;
  padding: 5px 10px;
  border-radius: 7px;
  cursor: pointer;
}

.btn-face-disable-sm {
  background: #fff1f2;
  color: #e11d48;
  border: 1px solid #fecdd3;
  font-size: 11px;
  font-weight: 700;
  padding: 5px 10px;
  border-radius: 7px;
  cursor: pointer;
}

.btn-face-disable-sm:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* 4. Thanh Hotline hỗ trợ 24/7 tinh gọn */
.compact-hotline-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 12px;
  padding: 9px 12px;
  color: #1e40af;
  font-size: 12px;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.2s;
  gap: 8px;
}

.compact-hotline-bar:hover {
  background: #dbeafe;
}

.hotline-left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.hotline-icon {
  font-size: 17px;
  color: #2563eb;
  flex-shrink: 0;
}

.hotline-badge-call {
  background: #2563eb;
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 6px;
  flex-shrink: 0;
  white-space: nowrap;
}

/* 5. Nút Đăng Xuất Tài Xế Nổi Bật */
.profile-logout-box {
  margin-top: 2px;
}

.btn-shipper-logout {
  width: 100%;
  padding: 12px 18px;
  background: #fff1f2;
  color: #e11d48;
  border: 1.5px solid #fecdd3;
  border-radius: 12px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: all 0.2s;
  letter-spacing: 0.3px;
  box-shadow: 0 2px 8px rgba(225, 29, 72, 0.08);
}

.btn-shipper-logout:hover {
  background: #e11d48;
  color: #ffffff;
  border-color: #e11d48;
  box-shadow: 0 4px 14px rgba(225, 29, 72, 0.25);
}

/* ==========================================================================
   2. SHIFT STATS ROW (4 CARDS)
   ========================================================================== */
.shift-stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 18px;
}

.stat-card {
  background: #ffffff;
  border-radius: 18px;
  padding: 18px 20px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 12px -2px rgba(0, 0, 0, 0.03);
  display: flex;
  align-items: center;
  gap: 14px;
}

.stat-icon-wrap {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 19px;
  flex-shrink: 0;
}

.bg-green { background: #f0fdf4; color: #16a34a; }
.bg-blue { background: #eff6ff; color: #2563eb; }
.bg-orange { background: #fff7ed; color: #ea580c; }
.bg-purple { background: #faf5ff; color: #9333ea; }
.bg-slate { background: #f1f5f9; color: #64748b; }

.stat-caption {
  font-size: 11.5px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.stat-figure {
  display: block;
  font-size: 18px;
  font-weight: 900;
  color: #0f172a;
}

.text-green {
  color: #16a34a !important;
}

/* ==========================================================================
   PHẦN RIÊNG: THU NHẬP & CÁC CUỐC XE
   ========================================================================== */
.earnings-view-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
  animation: fadeIn 0.25s ease-out;
}

.earnings-details-grid {
  display: grid;
  grid-template-columns: 1fr 1.6fr;
  gap: 20px;
  align-items: start;
}

/* Ví tài xế */
.wallet-card {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  color: #ffffff;
  border-radius: 20px;
  padding: 24px;
  box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.15);
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.wallet-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.wallet-subtitle {
  font-size: 11px;
  font-weight: 700;
  color: #94a3b8;
  letter-spacing: 1px;
  text-transform: uppercase;
}

.wallet-balance {
  font-size: 30px;
  font-weight: 900;
  color: #38bdf8;
  margin: 4px 0 0 0;
  letter-spacing: -0.5px;
}

.wallet-icon-box {
  width: 48px;
  height: 48px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  color: #38bdf8;
  backdrop-filter: blur(8px);
}

.wallet-meta {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 14px 16px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  font-size: 13px;
  color: #cbd5e1;
}

.wallet-meta strong {
  color: #ffffff;
  font-weight: 700;
}

.wallet-actions {
  display: flex;
  gap: 10px;
}

.wallet-actions .btn-outline-primary {
  border: 1px solid rgba(255, 255, 255, 0.25);
  color: #ffffff;
  background: rgba(255, 255, 255, 0.06);
}

.wallet-actions .btn-outline-primary:hover {
  background: rgba(255, 255, 255, 0.15);
  border-color: #ffffff;
}

/* Lịch sử cuốc xe */
.trips-history-card {
  background: #ffffff;
  border-radius: 20px;
  border: 1px solid #f1f5f9;
  padding: 22px;
  box-shadow: 0 2px 12px -2px rgba(0, 0, 0, 0.03);
  min-height: 280px;
  display: flex;
  flex-direction: column;
}

.trips-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 18px;
  padding-bottom: 14px;
  border-bottom: 1px solid #f1f5f9;
}

.trips-header h3 {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.trips-count {
  font-size: 12px;
  font-weight: 700;
  color: #2563eb;
  background: #eff6ff;
  padding: 4px 10px;
  border-radius: 999px;
}

.empty-trips-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 40px 20px;
  flex: 1;
}

.empty-icon-circle {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: #f8fafc;
  color: #94a3b8;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  margin-bottom: 14px;
}

.empty-trips-box h4 {
  font-size: 15px;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 6px 0;
}

.empty-trips-box p {
  font-size: 13px;
  color: #64748b;
  max-width: 380px;
  margin: 0 0 18px 0;
  line-height: 1.5;
}

.trips-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.trip-log-item {
  display: grid;
  grid-template-columns: 85px 1fr auto;
  gap: 16px;
  align-items: center;
  padding: 14px 16px;
  background: #f8fafc;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  transition: transform 0.15s ease, background 0.15s ease;
}

.trip-log-item:hover {
  background: #f1f5f9;
  transform: translateY(-1px);
}

.trip-time-col {
  display: flex;
  flex-direction: column;
}

.trip-time {
  font-size: 14px;
  font-weight: 800;
  color: #0f172a;
}

.trip-id {
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
  font-family: monospace;
}

.trip-route-col {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.trip-point {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #334155;
}

.dot-shop {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #2563eb;
  flex-shrink: 0;
}

.dot-customer {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #16a34a;
  flex-shrink: 0;
}

.point-name {
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trip-meta-col {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
}

.trip-dist {
  font-size: 11.5px;
  color: #64748b;
  font-weight: 600;
}

.trip-fee {
  font-size: 15px;
  font-weight: 900;
  color: #16a34a;
}

.trip-status-badge {
  font-size: 10px;
  font-weight: 700;
  color: #16a34a;
  background: #dcfce7;
  padding: 2px 6px;
  border-radius: 6px;
}

/* ==========================================================================
   3. OPERATIONAL GRID: MAP + CONTROLLER
   ========================================================================== */
.operational-grid {
  display: grid;
  grid-template-columns: 1.55fr 1fr;
  gap: 24px;
  align-items: start;
}

/* Map Card (Google Maps Experience) */
.map-card-wrapper {
  background: #e5e3df;
  border-radius: 20px;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.06);
  position: relative;
  height: 580px;
}

.map-card-wrapper.is-fullscreen {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100vw;
  height: 100vh;
  z-index: 99999;
  border-radius: 0;
}

.leaflet-map-element {
  width: 100%;
  height: 100%;
  z-index: 1;
  transition: filter 0.3s ease;
}

.leaflet-map-element.map-offline {
  filter: grayscale(80%) opacity(0.65);
}

/* 1. Google Maps Top Bar Controls */
.gm-top-controls {
  position: absolute;
  top: 14px;
  left: 14px;
  right: 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  z-index: 10;
  pointer-events: none;
  flex-wrap: wrap;
}

.gm-search-box {
  pointer-events: auto;
  display: flex;
  align-items: center;
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.22);
  height: 44px;
  padding: 0 8px;
  min-width: 280px;
  max-width: 380px;
  flex: 1;
}

.gm-search-icon {
  width: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 17px;
}

.gm-search-input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 13.5px;
  font-weight: 500;
  color: #202124;
  background: transparent;
  padding: 0 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.gm-search-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.gm-icon-btn {
  background: transparent;
  border: none;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #5f6368;
  cursor: pointer;
  transition: background 0.15s;
}

.gm-icon-btn:hover {
  background: #f1f3f4;
  color: #202124;
}

.gm-search-divider {
  width: 1px;
  height: 20px;
  background: #dadce0;
  margin: 0 4px;
}

.gm-status-chips {
  pointer-events: auto;
  display: flex;
  align-items: center;
  gap: 8px;
}

.gm-chip {
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.18);
  padding: 6px 14px;
  font-size: 12.5px;
  font-weight: 600;
  color: #3c4043;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  border: 1px solid #dadce0;
  cursor: pointer;
  transition: all 0.2s ease;
}

.gm-chip:hover {
  background: #f8f9fa;
}

.gm-chip.chip-active {
  background: #e8f0fe;
  color: #1a73e8;
  border-color: #1a73e8;
}

.traffic-dots {
  display: inline-flex;
  gap: 3px;
}

.traffic-dots .dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}
.traffic-dots .dot-green { background: #34a853; }
.traffic-dots .dot-orange { background: #fbbc04; }
.traffic-dots .dot-red { background: #ea4335; }

.gm-radius-chip {
  cursor: default;
  background: rgba(32, 33, 36, 0.88);
  backdrop-filter: blur(4px);
  color: #ffffff;
  border: none;
}

/* 2. Google Maps Bottom Left Controls */
.gm-bottom-left-controls {
  position: absolute;
  bottom: 22px;
  left: 14px;
  display: flex;
  align-items: flex-end;
  gap: 12px;
  z-index: 10;
}

.gm-layer-card {
  width: 58px;
  height: 58px;
  border-radius: 8px;
  overflow: hidden;
  border: 2px solid #ffffff;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
  cursor: pointer;
  position: relative;
  transition: transform 0.15s ease;
}

.gm-layer-card:hover {
  transform: scale(1.05);
}

.gm-layer-thumb {
  width: 100%;
  height: 100%;
  position: relative;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding-bottom: 2px;
}

.thumb-satellite {
  background: url("https://mt1.google.com/vt/lyrs=s&hl=vi&x=13&y=7&z=4") center/cover no-repeat;
}

.thumb-roadmap {
  background: url("https://mt1.google.com/vt/lyrs=m&hl=vi&x=13&y=7&z=4") center/cover no-repeat;
}

.gm-layer-badge {
  background: rgba(32, 33, 36, 0.85);
  color: #ffffff;
  font-size: 9.5px;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 4px;
  line-height: 1.2;
}

/* Official Google Watermark Logo */
.gm-watermark-logo {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  font-size: 18px;
  font-weight: 700;
  letter-spacing: -0.5px;
  user-select: none;
  background: rgba(255, 255, 255, 0.88);
  padding: 2px 8px;
  border-radius: 4px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12);
  line-height: 1.2;
}

.g-blue { color: #4285F4; }
.g-red { color: #EA4335; }
.g-yellow { color: #FBBC05; }
.g-green { color: #34A853; }

/* 3. Google Maps Bottom Right Controls Stack */
.gm-bottom-right-controls {
  position: absolute;
  bottom: 24px;
  right: 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: center;
  z-index: 10;
}

.gm-ctrl-btn {
  width: 40px;
  height: 40px;
  background: #ffffff;
  border: none;
  border-radius: 6px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #5f6368;
  cursor: pointer;
  transition: background 0.15s;
}

.gm-ctrl-btn:hover {
  background: #f1f3f4;
  color: #202124;
}

.gm-my-location:hover svg {
  fill: #1a73e8;
}

.gm-my-location.is-locating svg {
  animation: spinGps 1s linear infinite;
  fill: #1a73e8;
}

@keyframes spinGps {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.gm-zoom-group {
  width: 40px;
  background: #ffffff;
  border-radius: 6px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.3);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.gm-zoom-btn {
  width: 40px;
  height: 36px;
  background: transparent;
  border: none;
  font-size: 20px;
  color: #5f6368;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.15s;
  user-select: none;
}

.gm-zoom-btn:hover {
  background: #f1f3f4;
  color: #202124;
}

.gm-zoom-divider {
  height: 1px;
  background: #e8eaed;
  margin: 0 4px;
}

/* 4. Footer Legal */
.gm-footer-legal {
  position: absolute;
  bottom: 2px;
  right: 6px;
  font-size: 10px;
  color: #5f6368;
  background: rgba(255, 255, 255, 0.78);
  padding: 1px 6px;
  border-radius: 2px;
  z-index: 9;
  user-select: none;
  pointer-events: none;
}

/* Offline Map Overlay Prompt */
.offline-map-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(248, 250, 252, 0.6);
  backdrop-filter: blur(4px);
  z-index: 15;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

.offline-prompt-box {
  background: #ffffff;
  border-radius: 20px;
  padding: 32px 28px;
  text-align: center;
  max-width: 380px;
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.12);
  border: 1px solid #e2e8f0;
}

.prompt-icon {
  font-size: 38px;
  color: #64748b;
  margin-bottom: 12px;
  display: inline-block;
}

.offline-prompt-box h3 {
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 8px 0;
}

.offline-prompt-box p {
  font-size: 13.5px;
  color: #64748b;
  line-height: 1.55;
  margin: 0 0 20px 0;
}

/* Mission Controller Panel */
.mission-panel {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.active-mission-card {
  background: #ffffff;
  border-radius: 22px;
  padding: 28px;
  border: 1px solid #fed7aa;
  box-shadow: 0 4px 20px -4px rgba(234, 88, 12, 0.12);
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.mission-header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
}

.badge-express {
  background: #fff7ed;
  color: #ea580c;
  font-size: 12px;
  font-weight: 800;
  padding: 4px 10px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border: 1px solid #fdba74;
}

.fee-tag {
  font-size: 13px;
  color: #64748b;
}

.fee-tag strong {
  font-size: 16px;
  color: #16a34a;
}

/* Delivery Steps Bar */
.delivery-steps-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #f8fafc;
  padding: 12px 18px;
  border-radius: 14px;
}

.step-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.step-circle {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: #cbd5e1;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
}

.step-name {
  font-size: 12.5px;
  font-weight: 700;
  color: #64748b;
}

.step-item.current .step-circle {
  background: #ea580c;
}

.step-item.current .step-name {
  color: #ea580c;
}

.step-item.done .step-circle {
  background: #16a34a;
}

.step-item.done .step-name {
  color: #16a34a;
}

.step-line {
  flex: 1;
  height: 2px;
  background: #e2e8f0;
  margin: 0 10px;
}

.step-line.active {
  background: #16a34a;
}

/* Checkpoint Box */
.checkpoint-box {
  background: #f8fafc;
  border-radius: 16px;
  padding: 20px;
  border: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.customer-checkpoint {
  background: #f0fdf4;
  border-color: #bbf7d0;
}

.cp-header {
  display: flex;
  align-items: center;
  gap: 12px;
}

.cp-icon {
  font-size: 24px;
  color: #ea580c;
}

.cp-caption {
  font-size: 11px;
  font-weight: 800;
  color: #64748b;
  letter-spacing: 0.5px;
}

.cp-title {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.cp-address {
  font-size: 13px;
  color: #475569;
  margin: 0;
  line-height: 1.5;
}

.cp-items-list {
  font-size: 13px;
  background: #ffffff;
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  color: #334155;
}

.cp-notes {
  font-size: 12.5px;
  color: #15803d;
  background: #dcfce7;
  padding: 8px 12px;
  border-radius: 8px;
}

.cp-action-row {
  display: flex;
  gap: 10px;
  margin-top: 4px;
}

.btn-call {
  background: #ffffff;
  color: #0f172a;
  border: 1px solid #cbd5e1;
  text-decoration: none;
  font-size: 13px;
  font-weight: 700;
  padding: 10px 16px;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}

.btn-call:hover {
  border-color: #ea580c;
  color: #ea580c;
}

.btn-report-link {
  background: none;
  border: none;
  color: #ef4444;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  text-align: center;
  padding: 6px 0;
}

.btn-report-link:hover {
  text-decoration: underline;
}

/* Status Cards (Finished / Scanning / Offline) */
.mission-status-card {
  background: #ffffff;
  border-radius: 22px;
  padding: 36px 28px;
  text-align: center;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.status-icon-circle {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 26px;
}

.mission-status-card h3 {
  font-size: 18px;
  font-weight: 900;
  color: #0f172a;
  margin: 0;
}

.mission-status-card p {
  font-size: 13.5px;
  color: #64748b;
  margin: 0 0 12px 0;
  line-height: 1.55;
}

/* Utility buttons */
.btn-outline-primary {
  background: #eff6ff;
  color: #2563eb;
  border: 1px solid #bfdbfe;
  padding: 10px 16px;
  border-radius: 12px;
  font-weight: 700;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-outline-primary:hover {
  background: #2563eb;
  color: #ffffff;
}

.btn-primary {
  background: #2563eb;
  color: #ffffff;
  border: none;
  padding: 12px 18px;
  border-radius: 12px;
  font-weight: 800;
  font-size: 13.5px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: background 0.2s;
}

.btn-primary:hover {
  background: #1d4ed8;
}

.btn-success {
  background: #16a34a;
  color: #ffffff;
  border: none;
  padding: 12px 18px;
  border-radius: 12px;
  font-weight: 800;
  font-size: 13.5px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: background 0.2s;
}

.btn-success:hover {
  background: #15803d;
}

.w-100 {
  width: 100%;
}

.radar-scan-anim {
  position: relative;
  width: 72px;
  height: 72px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 8px;
}

.radar-icon {
  font-size: 28px;
  color: #ea580c;
  z-index: 2;
}

.radar-circle-1,
.radar-circle-2 {
  position: absolute;
  border-radius: 50%;
  border: 2px solid #ea580c;
  animation: radarWave 2s infinite cubic-bezier(0.16, 1, 0.3, 1);
}

.radar-circle-2 {
  animation-delay: 0.8s;
}

@keyframes radarWave {
  0% { width: 24px; height: 24px; opacity: 0.8; }
  100% { width: 80px; height: 80px; opacity: 0; }
}

.demo-trigger-box {
  width: 100%;
  border-top: 1px dashed #e2e8f0;
  padding-top: 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.demo-trigger-box small {
  color: #94a3b8;
  font-size: 11px;
}

/* Standard Buttons */
.btn {
  font-family: inherit;
  font-size: 13.5px;
  font-weight: 700;
  padding: 12px 20px;
  border-radius: 12px;
  border: none;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.flex-1 {
  flex: 1;
}

/* ==========================================================================
   GOOGLE MAPS CUSTOM MARKER & INFOWINDOW STYLES
   ========================================================================== */
:deep(.custom-gm-icon) {
  background: transparent;
  border: none;
}

/* 1. Driver Beacon (Google Blue Navigation Beacon) */
:deep(.gm-driver-beacon) {
  position: relative;
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
}

:deep(.gm-beacon-wave) {
  position: absolute;
  top: 0;
  left: 0;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: rgba(66, 133, 244, 0.22);
  border: 1.5px solid rgba(66, 133, 244, 0.6);
  animation: gmBeaconPulse 2s infinite ease-out;
}

@keyframes gmBeaconPulse {
  0% { transform: scale(0.65); opacity: 1; }
  100% { transform: scale(1.4); opacity: 0; }
}

:deep(.gm-beacon-core) {
  position: relative;
  width: 32px;
  height: 32px;
  background: #1a73e8;
  border: 3px solid #ffffff;
  border-radius: 50%;
  box-shadow: 0 2px 8px rgba(26, 115, 232, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2;
}

:deep(.gm-core-icon) {
  font-size: 16px;
  line-height: 1;
}

/* 2. Google Drop Pins (Store & Customer) */
:deep(.gm-marker-pin) {
  position: relative;
  width: 36px;
  height: 46px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

:deep(.gm-pin-bubble) {
  width: 34px;
  height: 34px;
  border-radius: 50% 50% 50% 0;
  transform: rotate(-45deg);
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid #ffffff;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.25);
  z-index: 2;
}

:deep(.gm-marker-pin.store-pin .gm-pin-bubble) {
  background: #1a73e8;
}

:deep(.gm-marker-pin.customer-pin .gm-pin-bubble) {
  background: #ea4335;
}

:deep(.gm-pin-icon) {
  transform: rotate(45deg);
  font-size: 15px;
  line-height: 1;
}

:deep(.gm-pin-dot) {
  width: 10px;
  height: 10px;
  background: #ffffff;
  border-radius: 50%;
  box-shadow: inset 0 1px 2px rgba(0, 0, 0, 0.2);
}

:deep(.gm-pin-shadow) {
  position: absolute;
  bottom: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 16px;
  height: 6px;
  background: rgba(0, 0, 0, 0.25);
  border-radius: 50%;
  filter: blur(1px);
  z-index: 1;
}

/* 3. Google Maps InfoWindow Popup Styling */
:deep(.leaflet-popup-content-wrapper) {
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 7px 1px rgba(0, 0, 0, 0.25);
  padding: 0;
  overflow: hidden;
}

:deep(.leaflet-popup-content) {
  margin: 0;
  line-height: 1.4;
}

:deep(.leaflet-popup-tip) {
  background: #ffffff;
  box-shadow: 0 2px 7px 1px rgba(0, 0, 0, 0.2);
}

:deep(.gm-infowindow) {
  padding: 12px 14px;
  min-width: 210px;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
}

:deep(.gm-iw-tag) {
  font-size: 10.5px;
  font-weight: 700;
  letter-spacing: 0.5px;
  margin-bottom: 2px;
}

:deep(.gm-iw-tag.text-blue) { color: #1a73e8; }
:deep(.gm-iw-tag.text-danger) { color: #ea4335; }
:deep(.gm-iw-tag.text-primary) { color: #1a73e8; }

:deep(.gm-iw-title) {
  font-size: 14px;
  font-weight: 700;
  color: #202124;
  margin: 0 0 4px 0;
}

:deep(.gm-iw-desc) {
  font-size: 12px;
  color: #3c4043;
  margin: 0 0 6px 0;
}

:deep(.gm-iw-meta),
:deep(.gm-iw-rating) {
  font-size: 11.5px;
  color: #70757a;
}
:deep(.gm-iw-rating) {
  color: #e37400;
  font-weight: 600;
}



/* ================================================================
   MODAL NỔ ĐƠN HỎA TỐC BÁN KÍNH 3KM STYLING
   ================================================================ */
.incoming-order-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(8px);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  animation: fadeInModal 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.incoming-order-modal {
  background: #ffffff;
  width: 100%;
  max-width: 480px;
  border-radius: 20px;
  box-shadow: 0 25px 60px -15px rgba(0, 0, 0, 0.4), 0 0 0 2px #3b82f6;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  animation: scaleInModal 0.35s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.incoming-modal-header {
  background: linear-gradient(135deg, #1e3a8a, #2563eb);
  color: #ffffff;
  padding: 18px 20px;
  display: flex;
  align-items: center;
  gap: 14px;
}

.incoming-badge-pulse {
  position: relative;
  width: 44px;
  height: 44px;
  background: #ef4444;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  color: #fff;
  flex-shrink: 0;
}

.pulse-ring {
  position: absolute;
  top: -6px;
  left: -6px;
  right: -6px;
  bottom: -6px;
  border-radius: 50%;
  border: 3px solid #ef4444;
  animation: ringPulse 1.2s infinite ease-out;
}

@keyframes ringPulse {
  0% { transform: scale(0.9); opacity: 1; }
  100% { transform: scale(1.4); opacity: 0; }
}

.incoming-title-box h3 {
  margin: 0;
  font-size: 17px;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: #ffffff;
}

.incoming-title-box p {
  margin: 2px 0 0;
  font-size: 13px;
  color: #bfdbfe;
}

.countdown-badge {
  margin-left: auto;
  background: rgba(255, 255, 255, 0.2);
  padding: 6px 12px;
  border-radius: 20px;
  font-weight: 800;
  font-size: 15px;
  display: flex;
  align-items: center;
  gap: 6px;
  color: #fef08a;
  border: 1px solid rgba(254, 240, 138, 0.4);
}

.countdown-bar-container {
  width: 100%;
  height: 5px;
  background: #e2e8f0;
}

.countdown-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #22c55e, #eab308, #ef4444);
  transition: width 1s linear;
}

.incoming-modal-body {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-height: 55vh;
  overflow-y: auto;
}

.fee-banner {
  background: #f0fdf4;
  border: 1.5px dashed #22c55e;
  border-radius: 12px;
  padding: 10px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.fee-label {
  font-size: 13px;
  font-weight: 600;
  color: #166534;
}

.fee-val {
  font-size: 20px;
  font-weight: 800;
  color: #15803d;
}

.loc-step-item {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  background: #f8fafc;
  padding: 12px;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
}

.loc-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 17px;
  color: #fff;
  flex-shrink: 0;
}

.loc-icon.bg-blue { background: #3b82f6; }
.loc-icon.bg-green { background: #10b981; }

.loc-details {
  display: flex;
  flex-direction: column;
}

.loc-tag {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  color: #64748b;
  letter-spacing: 0.04em;
}

.loc-name {
  margin: 2px 0 1px;
  font-size: 15px;
  font-weight: 700;
  color: #1e293b;
}

.loc-address {
  margin: 0;
  font-size: 13px;
  color: #64748b;
}

.incoming-items-list {
  border-top: 1px solid #f1f5f9;
  padding-top: 10px;
}

.items-head {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 8px;
}

.incoming-item-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.item-thumb {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  object-fit: cover;
}

.item-thumb.fallback {
  background: #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #64748b;
}

.item-info {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex: 1;
}

.item-title {
  font-size: 13px;
  color: #334155;
  font-weight: 500;
}

.item-qty {
  font-size: 12px;
  font-weight: 700;
  color: #2563eb;
  background: #eff6ff;
  padding: 2px 6px;
  border-radius: 6px;
}

.incoming-modal-actions {
  padding: 16px 20px;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
  display: flex;
  gap: 12px;
}

.btn-decline {
  flex: 1;
  padding: 13px;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #64748b;
  font-weight: 700;
  font-size: 14px;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-decline:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.btn-accept {
  flex: 2;
  padding: 13px;
  background: linear-gradient(135deg, #16a34a, #22c55e);
  border: none;
  color: #ffffff;
  font-weight: 800;
  font-size: 14px;
  border-radius: 12px;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(34, 197, 94, 0.4);
  transition: all 0.2s;
}

.btn-accept:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(34, 197, 94, 0.5);
}

@keyframes fadeInModal {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes scaleInModal {
  from { transform: scale(0.85); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

/* ==========================================================================
   NÚT ĐĂNG XUẤT TÀI XẾ (MAP FLOATING & PROFILE TAB)
   ========================================================================== */
.map-action-btn.btn-logout {
  background: #fff1f2;
  border-color: #fecdd3;
  color: #e11d48;
}
.map-action-btn.btn-logout:hover {
  background: #ffe4e6;
  border-color: #fda4af;
  color: #be123c;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(225, 29, 72, 0.25);
}

.profile-logout-box {
  margin-top: 16px;
  padding: 12px 0 4px;
}

.btn-shipper-logout {
  width: 100%;
  padding: 14px 20px;
  background: #fff1f2;
  color: #e11d48;
  border: 1.5px solid #fecdd3;
  border-radius: 14px;
  font-size: 14.5px;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 4px 12px rgba(225, 29, 72, 0.08);
}
.btn-shipper-logout:hover {
  background: #e11d48;
  color: #ffffff;
  border-color: #e11d48;
  transform: translateY(-1px);
  box-shadow: 0 6px 18px rgba(225, 29, 72, 0.28);
}

/* ==========================================================================
   RESPONSIVE CHO ỨNG DỤNG TÀI XẾ TRÊN CÁC THIẾT BỊ (MOBILE, TABLET)
   ========================================================================== */
@media (max-width: 768px) {
  .shipper-app-screen {
    position: fixed !important;
    top: 0 !important;
    left: 0 !important;
    right: 0 !important;
    bottom: 0 !important;
    width: 100% !important;
    height: 100% !important;
    max-height: 100% !important;
    overflow: hidden !important;
    touch-action: none !important;
  }

  /* 1. TOP FLOATING BAR (TRÊN CÙNG BẢN ĐỒ) */
  .top-floating-bar {
    top: 10px !important;
    left: 10px !important;
    right: 10px !important;
    flex-wrap: nowrap !important;
    gap: 8px !important;
    align-items: center !important;
    justify-content: space-between !important;
    z-index: 1003 !important;
  }

  .driver-mini-pill {
    padding: 5px 12px 5px 5px !important;
    height: 48px !important;
    gap: 9px !important;
    max-width: none !important;
    border-radius: 999px !important;
    background: rgba(255, 255, 255, 0.98) !important;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12) !important;
    border: 1.5px solid rgba(226, 232, 240, 0.9) !important;
    flex-shrink: 0 !important;
    min-width: 0 !important;
    display: flex !important;
    align-items: center !important;
  }

  .driver-pill-avatar {
    width: 38px !important;
    height: 38px !important;
    border-radius: 50% !important;
    font-size: 18px !important;
    flex-shrink: 0 !important;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12) !important;
    border: 2px solid #ffffff !important;
    background: #fff7ed !important;
    position: relative !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
  }

  .pill-dot {
    width: 11px !important;
    height: 11px !important;
    border: 2px solid #ffffff !important;
    bottom: -1px !important;
    right: -1px !important;
  }

  .driver-pill-info {
    min-width: 0 !important;
    display: flex !important;
    flex-direction: column !important;
    justify-content: center !important;
  }

  .pill-name-row {
    display: flex !important;
    align-items: center !important;
    gap: 5px !important;
    white-space: nowrap !important;
    min-width: 0 !important;
  }

  .pill-name {
    font-size: 13.5px !important;
    font-weight: 800 !important;
    color: #0f172a !important;
    max-width: 130px !important;
    white-space: nowrap !important;
    overflow: hidden !important;
    text-overflow: ellipsis !important;
    min-width: 0 !important;
    line-height: 1.2 !important;
  }

  .pill-rating {
    font-size: 9.5px !important;
    font-weight: 800 !important;
    color: #d97706 !important;
    background: #fefce8 !important;
    padding: 1px 5px !important;
    border-radius: 5px !important;
    white-space: nowrap !important;
    flex-shrink: 0 !important;
    border: 1px solid #fef08a !important;
  }

  .pill-status-text {
    font-size: 10px !important;
    font-weight: 600 !important;
    white-space: nowrap !important;
    overflow: hidden !important;
    text-overflow: ellipsis !important;
    color: #64748b !important;
    line-height: 1.2 !important;
  }

  /* 2. MAP FLOATING ACTIONS (TOP RIGHT) */
  .map-floating-actions {
    display: flex !important;
    align-items: center !important;
    gap: 6px !important;
    flex-shrink: 0 !important;
  }

  /* Ẩn các nút phụ trên top mobile vì đã có Bottom Navigation */
  .map-floating-actions .btn-wallet,
  .map-floating-actions .btn-logout {
    display: none !important;
  }

  /* Nút chức năng bản đồ: rõ nét, to vừa vặn ngón tay, hiển thị cả icon và chữ */
  .map-action-btn {
    height: 40px !important;
    width: auto !important;
    padding: 0 11px !important;
    border-radius: 999px !important;
    display: inline-flex !important;
    align-items: center !important;
    justify-content: center !important;
    gap: 5px !important;
    font-size: 12px !important;
    font-weight: 700 !important;
    color: #334155 !important;
    background: rgba(255, 255, 255, 0.98) !important;
    border: 1.5px solid rgba(226, 232, 240, 0.9) !important;
    box-shadow: 0 4px 14px rgba(0, 0, 0, 0.1) !important;
    flex-shrink: 0 !important;
    white-space: nowrap !important;
  }

  .map-action-btn i {
    font-size: 15px !important;
  }

  .map-action-btn .action-label {
    display: inline-block !important;
    font-size: 11.5px !important;
    font-weight: 700 !important;
  }

  .map-action-btn.btn-active {
    background: #eff6ff !important;
    color: #2563eb !important;
    border-color: #93c5fd !important;
    font-weight: 800 !important;
  }

  /* GPS RECENTER BUTTON: CHUYỂN XUỐNG GÓC DƯỚI PHẢI VỪA TẦM TAY */
  .map-action-btn.btn-gps {
    position: fixed !important;
    right: 14px !important;
    bottom: 152px !important;
    bottom: calc(142px + env(safe-area-inset-bottom, 12px)) !important;
    width: 44px !important;
    height: 44px !important;
    border-radius: 50% !important;
    background: #ffffff !important;
    color: #2563eb !important;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.18) !important;
    border: 1.5px solid #e2e8f0 !important;
    z-index: 1001 !important;
    font-size: 18px !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
  }

  /* 3. NÚT BẬT / TẮT HOẠT ĐỘNG (MASTER TOGGLE) TRÊN ĐIỆN THOẠI */
  .floating-toggle-bar {
    position: fixed !important;
    bottom: 94px !important;
    bottom: calc(84px + env(safe-area-inset-bottom, 12px)) !important;
    left: 50% !important;
    right: auto !important;
    transform: translateX(-50%) translateZ(0) !important;
    -webkit-transform: translateX(-50%) translateZ(0) !important;
    width: max-content !important;
    max-width: calc(100vw - 32px) !important;
    z-index: 1002 !important;
    display: flex !important;
    justify-content: center !important;
    align-items: center !important;
    padding: 0 !important;
    pointer-events: none !important;
    visibility: visible !important;
    opacity: 1 !important;
  }

  .master-toggle-btn {
    pointer-events: auto !important;
    position: relative !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    width: 220px !important;
    height: 48px !important;
    border-radius: 999px !important;
    background: #1e293b !important;
    color: #ffffff !important;
    border: 2px solid rgba(255, 255, 255, 0.3) !important;
    box-shadow: 0 8px 24px rgba(15, 23, 42, 0.4) !important;
    cursor: pointer !important;
    overflow: hidden !important;
    padding: 0 !important;
    visibility: visible !important;
    opacity: 1 !important;
  }

  .master-toggle-btn.is-online {
    background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%) !important;
    border-color: #86efac !important;
    box-shadow: 0 8px 24px rgba(34, 197, 94, 0.45) !important;
  }

  .master-toggle-btn .toggle-knob {
    position: absolute !important;
    width: 36px !important;
    height: 36px !important;
    top: 4px !important;
    left: 6px !important;
    border-radius: 50% !important;
    background: #ffffff !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    font-size: 16px !important;
    color: #64748b !important;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2) !important;
    transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), color 0.3s !important;
    z-index: 2 !important;
  }

  .master-toggle-btn.is-online .toggle-knob {
    transform: translateX(172px) !important;
    color: #16a34a !important;
  }

  .master-toggle-btn .toggle-text {
    width: 100% !important;
    text-align: center !important;
    font-size: 13.5px !important;
    font-weight: 800 !important;
    letter-spacing: 0.5px !important;
    color: #ffffff !important;
    transition: padding 0.35s cubic-bezier(0.16, 1, 0.3, 1) !important;
    z-index: 1 !important;
    padding-left: 32px !important;
    padding-right: 0 !important;
  }

  .master-toggle-btn.is-online .toggle-text {
    padding-left: 0 !important;
    padding-right: 32px !important;
  }

  /* 4. FLOATING MISSION / RADAR SCANNING CHIP */
  .floating-mission-container {
    position: fixed !important;
    left: 10px !important;
    right: 10px !important;
    bottom: 152px !important;
    bottom: calc(142px + env(safe-area-inset-bottom, 12px)) !important;
    max-width: 440px !important;
    margin: 0 auto !important;
    z-index: 1001 !important;
    pointer-events: none !important;
  }

  .floating-order-card {
    padding: 12px 14px !important;
    border-radius: 16px !important;
    gap: 8px !important;
  }

  .floating-radar-chip {
    padding: 8px 12px !important;
    gap: 8px !important;
  }

  .radar-info strong {
    font-size: 11.5px !important;
  }

  .radar-info small {
    font-size: 9.5px !important;
  }

  /* 5. OFFLINE PROMPT CARD */
  .offline-prompt-box {
    padding: 22px 18px !important;
    max-width: 320px !important;
    width: 90% !important;
    border-radius: 20px !important;
  }

  .offline-icon-pulse {
    width: 48px !important;
    height: 48px !important;
    font-size: 20px !important;
    margin-bottom: 8px !important;
  }

  .offline-prompt-box h3 {
    font-size: 16px !important;
    margin: 0 0 6px 0 !important;
  }

  .offline-prompt-box p {
    font-size: 12px !important;
    margin: 0 0 14px 0 !important;
    line-height: 1.4 !important;
  }

  .btn-turn-online {
    padding: 10px 16px !important;
    font-size: 13px !important;
    border-radius: 10px !important;
  }

  /* 6. SUB-TABS OVERLAY */
  .slide-panel-container {
    width: 100vw !important;
    max-width: 100vw !important;
  }

  /* 6. SUB-TABS OVERLAY */
  .sub-tab-panel-overlay {
    position: fixed !important;
    inset: 0 !important;
    top: 0 !important;
    left: 0 !important;
    right: 0 !important;
    bottom: 0 !important;
    z-index: 900 !important;
    background: #f8fafc !important;
    overflow-y: auto !important;
    -webkit-overflow-scrolling: touch !important;
    touch-action: pan-y !important;
  }

  .panel-body-content {
    padding: 12px 10px !important;
    padding-bottom: 96px !important;
    padding-bottom: calc(88px + env(safe-area-inset-bottom, 12px)) !important;
  }

  .shift-stats-row {
    grid-template-columns: repeat(2, 1fr) !important;
    gap: 8px !important;
  }

  .stat-card {
    padding: 10px 12px !important;
  }

  .stat-figure {
    font-size: 15px !important;
  }

  .profile-details-grid {
    grid-template-columns: 1fr 1fr !important;
    gap: 8px !important;
  }

  .incoming-order-modal {
    width: 95vw !important;
    max-width: 95vw !important;
    margin: 10px !important;
    border-radius: 16px !important;
  }

  /* 7. THANH ĐIỀU HƯỚNG ĐÁY MÀN HÌNH (MOBILE BOTTOM NAV) - DẠNG DOCK NỔI NÂNG CAO TRÁNH THANH ĐỊA CHỈ & VIỀN ĐT */
  .shipper-bottom-nav {
    display: flex !important;
    position: fixed !important;
    bottom: 24px !important;
    bottom: calc(16px + env(safe-area-inset-bottom, 12px)) !important;
    left: 12px !important;
    right: 12px !important;
    width: auto !important;
    height: 58px !important;
    background: #FFFFFF !important;
    border: 1.5px solid #E2E8F0 !important;
    border-radius: 20px !important;
    box-shadow: 0 10px 25px -4px rgba(15, 23, 42, 0.16), 0 4px 10px rgba(0, 0, 0, 0.06) !important;
    z-index: 1000 !important;
    transform: translateZ(0) !important;
    -webkit-transform: translateZ(0) !important;
    will-change: transform !important;
    align-items: center !important;
    justify-content: space-around !important;
    padding: 3px 6px !important;
    visibility: visible !important;
    opacity: 1 !important;
  }

  .bottom-nav-item {
    flex: 1 !important;
    display: flex !important;
    flex-direction: column !important;
    align-items: center !important;
    justify-content: center !important;
    gap: 2px !important;
    height: 100% !important;
    background: transparent !important;
    border: none !important;
    color: #64748B !important;
    font-size: 11px !important;
    font-weight: 700 !important;
    padding: 3px 2px !important;
    cursor: pointer !important;
    text-decoration: none !important;
    border-radius: 14px !important;
    transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1) !important;
  }

  .bottom-nav-item i {
    font-size: 19px !important;
    line-height: 1 !important;
    transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1) !important;
  }

  .bottom-nav-item.active {
    color: #2563EB !important;
    font-weight: 800 !important;
    background: #EFF6FF !important;
  }

  .bottom-nav-item.active i {
    color: #2563EB !important;
    transform: scale(1.1) !important;
  }

  .bottom-nav-icon-wrap {
    position: relative !important;
    display: inline-flex !important;
  }

  .bottom-nav-badge {
    position: absolute !important;
    top: -5px !important;
    right: -10px !important;
    background: #EF4444 !important;
    color: #FFFFFF !important;
    font-size: 9px !important;
    font-weight: 800 !important;
    min-width: 16px !important;
    height: 16px !important;
    border-radius: 8px !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    padding: 0 3px !important;
    box-shadow: 0 2px 5px rgba(239, 68, 68, 0.4) !important;
  }

  .panel-body-content {
    padding-bottom: 74px !important;
  }
}

@media (max-width: 380px) {
  .top-floating-bar {
    gap: 4px !important;
    left: 6px !important;
    right: 6px !important;
  }

  .driver-mini-pill {
    padding: 4px 8px 4px 4px !important;
    height: 44px !important;
    gap: 6px !important;
  }

  .driver-pill-avatar {
    width: 34px !important;
    height: 34px !important;
  }

  .pill-name {
    font-size: 12.5px !important;
    max-width: 105px !important;
  }

  .map-action-btn {
    height: 38px !important;
    padding: 0 8px !important;
    font-size: 11px !important;
    gap: 4px !important;
  }

  .map-action-btn .action-label {
    font-size: 10.5px !important;
  }
}

@media (max-width: 480px) {

  .shift-stats-row {
    grid-template-columns: 1fr 1fr !important;
  }

  .trip-log-item {
    grid-template-columns: 1fr !important;
    gap: 8px !important;
  }

  .trip-meta-col {
    align-items: flex-start !important;
  }

  .incoming-modal-actions {
    flex-direction: column !important;
    gap: 8px !important;
  }

  .btn-decline,
  .btn-accept {
    width: 100% !important;
  }
}
</style>

