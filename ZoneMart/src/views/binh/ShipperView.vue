<script setup lang="ts">
/**
 * ================================================================
 * CỔNG TRANG CHỦ TÀI XẾ (DRIVER DASHBOARD WITH LIVE MAP) - Phụ trách: Bình
 * Tính năng chính:
 * 1. Nút gạt BẬT / TẮT hoạt động (Active/Inactive Toggle)
 * 2. Bản đồ tương tác Leaflet hiển thị vị trí Shipper, Shop, Khách hàng & Tuyến đường 10km
 * 3. Luồng nhận đơn hàng hỏa tốc, xác nhận lấy hàng và hoàn tất đơn
 * 4. Thống kê thu nhập ca làm việc hôm nay
 * ================================================================
 */
import { ref, onMounted, onUnmounted, watch, nextTick } from "vue";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

// 1. Trạng thái hoạt động (Active / Inactive)
const isOnline = ref(true);
// 1. Trạng thái hoạt động (Mặc định: Tạm nghỉ)
const isOnline = ref(false);

// 2. Trạng thái đơn hàng: "idle" (đang chờ) | "accepted" (đi lấy) | "picked" (đi giao) | "delivered" (xong)
const currentStep = ref<"idle" | "accepted" | "picked" | "delivered">("accepted");
// 2. Trạng thái đơn hàng: "idle" (Mặc định: Chưa có đơn hàng, đang trống)
const currentStep = ref<"idle" | "accepted" | "picked" | "delivered">("idle");

// 3. Tọa độ thực tế (Khu vực Cầu Giấy, Hà Nội)
// 3. Tọa độ khởi tạo mặc định (Cầu Giấy, Hà Nội)
const driverLocation = ref({
  lat: 21.0333,
  lng: 105.7944,
  name: "Vị trí của bạn"
});

const activeOrder = ref({
  orderId: "ZM-7749",
  deliveryType: "express",
  shippingFee: 38500,
  distanceKm: 2.2,
  items: "2x Thịt Ba Chỉ Bò Mỹ, 1x Gạo ST25 (5kg), 1x Nấm Kim Châm",
  notes: "Gọi trước khi đến 5 phút, giao lên tầng 5 phòng 502",
  store: {
    name: "ZoneMart Bách Hóa Cầu Giấy",
    address: "245 Cầu Giấy, P. Dịch Vọng, Hà Nội",
    phone: "024 1234 5678",
    lat: 21.0360,
    lng: 105.7985
  },
  customer: {
    name: "Anh Hoàng Huy",
    address: "Số 165 Cầu Giấy, P. Dịch Vọng, Hà Nội",
    phone: "0912 345 678",
    lat: 21.0315,
    lng: 105.7910
  }
});
// Dữ liệu đơn hàng (Mặc định: null - không có dữ liệu gì)
const activeOrder = ref<any>(null);

// Thống kê hôm nay của tài xế
// Thống kê hôm nay của tài xế (Mặc định: 0 hết)
const shiftStats = ref({
  todayEarnings: 425000,
  completedOrders: 11,
  onlineHours: "4h 30m",
  totalKm: 26.8
  todayEarnings: 0,
  completedOrders: 0,
  onlineHours: "0h 00m",
  totalKm: 0.0
});

// Toast thông báo
const toastMsg = ref("");
const showToast = ref(false);
const triggerToast = (msg: string) => {
  toastMsg.value = msg;
  showToast.value = true;
  setTimeout(() => showToast.value = false, 2800);
};

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
const isFullscreen = ref(false);
const isLocating = ref(false);
const gpsAccuracy = ref<number | null>(null);
const locationAddress = ref("Khu vực: Cầu Giấy, Hà Nội • Google Maps GPS");
const locationAddress = ref("Chế độ tạm nghỉ • Gạt 'Bật Hoạt Động' để định vị GPS");

// Tạo icon tùy biến chuẩn Google Maps
const driverIcon = L.divIcon({
  className: "custom-gm-icon",
  html: `
    <div class="gm-driver-beacon">
      <div class="gm-beacon-wave"></div>
      <div class="gm-beacon-core">
        <span class="gm-core-icon">🛵</span>
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
        <span class="gm-pin-icon">🏪</span>
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
  triggerToast(mapType.value === 'satellite' ? '🛰️ Đã chuyển sang chế độ Vệ tinh Google Maps' : '🗺️ Đã chuyển sang chế độ Bản đồ Google Maps');
};

// Bật/Tắt dữ liệu giao thông trực tiếp Google Traffic
const toggleTraffic = () => {
  showTraffic.value = !showTraffic.value;
  updateMapLayers();
  triggerToast(showTraffic.value ? '🚦 Đã bật dữ liệu Giao thông trực tiếp Google' : '🚦 Đã tắt lớp dữ liệu Giao thông');
};

// Zoom Google Maps
const zoomIn = () => {
  map?.zoomIn();
};

const zoomOut = () => {
  map?.zoomOut();
};

// Toàn màn hình
const toggleFullscreen = () => {
  const elem = document.querySelector('.map-card-wrapper') as HTMLElement | null;
  if (!elem) return;
  if (!document.fullscreenElement) {
    elem.requestFullscreen?.().then(() => {
      isFullscreen.value = true;
      setTimeout(() => map?.invalidateSize(), 200);
    });
  } else {
    document.exitFullscreen?.().then(() => {
      isFullscreen.value = false;
      setTimeout(() => map?.invalidateSize(), 200);
    });
  }
};

// Khởi tạo bản đồ Google Maps
const initMap = () => {
  const container = document.getElementById("shipperMap");
  if (!container || map) return;

  map = L.map("shipperMap", {
    center: [driverLocation.value.lat, driverLocation.value.lng],
    zoom: 15,
    zoomControl: false,
    attributionControl: false
  });

  updateMapLayers();

  // Vòng bán kính quét đơn Google Style
  radiusCircle = L.circle([driverLocation.value.lat, driverLocation.value.lng], {
    radius: 3000,
    color: "#1a73e8",
    fillColor: "#4285f4",
    color: isOnline.value ? "#1a73e8" : "#94a3b8",
    fillColor: isOnline.value ? "#4285f4" : "#94a3b8",
    fillOpacity: 0.08,
    weight: 2,
    dashArray: "6, 6"
  }).addTo(map);

  // Ghim vị trí tài xế
  driverMarker = L.marker([driverLocation.value.lat, driverLocation.value.lng], {
    icon: driverIcon,
    title: "Vị trí của bạn"
  }).addTo(map).bindPopup(`
    <div class="gm-infowindow">
      <div class="gm-iw-tag text-primary">VỊ TRÍ CỦA BẠN</div>
      <h4 class="gm-iw-title">Tài xế ZoneMart Driver</h4>
      <p class="gm-iw-desc">🟢 Đang trực tuyến sẵn sàng nhận đơn</p>
      <div class="gm-iw-meta">Độ chính xác GPS: &lt; 5m • Cầu Giấy</div>
      <p class="gm-iw-desc">${isOnline.value ? "🟢 Đang trực tuyến sẵn sàng nhận đơn" : "⚪ Đang tạm nghỉ"}</p>
      <div class="gm-iw-meta">Gạt BẬT HOẠT ĐỘNG để định vị GPS chính xác</div>
    </div>
  `);

  renderOrderOnMap();
};

// Vẽ tuyến đường & các điểm đơn hàng chuẩn Google Navigation
const renderOrderOnMap = () => {
  if (!map) return;

  // Xóa layers cũ
  if (storeMarker) map.removeLayer(storeMarker);
  if (customerMarker) map.removeLayer(customerMarker);
  if (routeLineCasing) map.removeLayer(routeLineCasing);
  if (routeLine) map.removeLayer(routeLine);

  if (isOnline.value && currentStep.value !== "idle" && currentStep.value !== "delivered") {
  if (isOnline.value && currentStep.value !== "idle" && currentStep.value !== "delivered" && activeOrder.value) {
    // Ghim Shop
    storeMarker = L.marker([activeOrder.value.store.lat, activeOrder.value.store.lng], {
      icon: storeIcon
    }).addTo(map).bindPopup(`
      <div class="gm-infowindow">
        <div class="gm-iw-tag text-blue">ĐIỂM LẤY HÀNG</div>
        <h4 class="gm-iw-title">${activeOrder.value.store.name}</h4>
        <p class="gm-iw-desc">📍 ${activeOrder.value.store.address}</p>
        <div class="gm-iw-rating">★ 4.9 <span class="text-muted">(1,240 đánh giá) • Mở cửa</span></div>
      </div>
    `);

    // Ghim Khách hàng
    customerMarker = L.marker([activeOrder.value.customer.lat, activeOrder.value.customer.lng], {
      icon: customerIcon
    }).addTo(map).bindPopup(`
      <div class="gm-infowindow">
        <div class="gm-iw-tag text-danger">ĐIỂM GIAO HÀNG</div>
        <h4 class="gm-iw-title">${activeOrder.value.customer.name}</h4>
        <p class="gm-iw-desc">📍 ${activeOrder.value.customer.address}</p>
        <div class="gm-iw-meta">📞 ${activeOrder.value.customer.phone} • Hỏa tốc 10km</div>
      </div>
    `);

    // Tọa độ chặng đi
    const waypoints: [number, number][] = currentStep.value === "accepted"
      ? [
          [driverLocation.value.lat, driverLocation.value.lng],
          [activeOrder.value.store.lat, activeOrder.value.store.lng]
        ]
      : [
          [activeOrder.value.store.lat, activeOrder.value.store.lng],
          [activeOrder.value.customer.lat, activeOrder.value.customer.lng]
        ];

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

    // Fit view bao trọn lộ trình
    const bounds = L.latLngBounds(waypoints);
    map.fitBounds(bounds, { padding: [60, 60] });
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
        locationAddress.value = `📍 ${shortAddr}`;

        if (driverMarker) {
          driverMarker.bindPopup(`
            <div class="gm-infowindow">
              <div class="gm-iw-tag text-primary">VỊ TRÍ THỰC TẾ QUA GPS</div>
              <h4 class="gm-iw-title">Tài xế ZoneMart (Bạn)</h4>
              <p class="gm-iw-desc">📍 ${shortAddr}</p>
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

      // Cập nhật vòng bán kính 10km quanh tài xế
      if (radiusCircle) {
        radiusCircle.setLatLng([lat, lng]);
      }

      // Vòng tròn độ chính xác GPS (Accuracy Circle)
      if (map) {
        if (accuracyCircle) map.removeLayer(accuracyCircle);
        accuracyCircle = L.circle([lat, lng], {
          radius: Math.max(accuracy, 20),
          color: "#1a73e8",
          fillColor: "#4285f4",
          fillOpacity: 0.12,
          weight: 1
        }).addTo(map);

        // Di chuyển camera mượt mà đến vị trí thực tế
        map.flyTo([lat, lng], 16, {
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
      let errorMsg = "⚠️ Không thể định vị GPS (Vui lòng chọn 'Cho phép' khi trình duyệt hỏi quyền vị trí)";
      if (err.code === err.PERMISSION_DENIED) {
        errorMsg = "⚠️ Bạn chưa cấp quyền truy cập vị trí trên trình duyệt!";
      } else if (err.code === err.TIMEOUT) {
        errorMsg = "⏳ Quá thời gian định vị GPS, dùng vị trí khu vực Cầu Giấy.";
      }
      triggerToast(errorMsg);

      if (map) {
        map.setView([driverLocation.value.lat, driverLocation.value.lng], 15);
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

// Xử lý bật/tắt online
const toggleOnline = () => {
  isOnline.value = !isOnline.value;
  if (isOnline.value) {
    triggerToast("🟢 Đã BẬT nhận đơn! Đang quét GPS định vị vị trí của bạn...");
    if (radiusCircle) {
      radiusCircle.setStyle({ color: "#1a73e8", fillColor: "#4285f4" });
    }
    // Tự động định vị ngay lập tức vị trí thực tế của người dùng
    locateAndTrackDriver(false);
    startLocationWatch();
  } else {
    stopLocationWatch();
    locationAddress.value = "Chế độ tạm nghỉ • GPS tạm dừng";
    triggerToast("🔴 Đã TẮT nhận đơn. Bạn đang ở trạng thái tạm nghỉ.");
    if (radiusCircle) {
      radiusCircle.setStyle({ color: "#94a3b8", fillColor: "#94a3b8" });
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

// Xử lý luồng đơn hàng
const handleConfirmPicked = () => {
  currentStep.value = "picked";
  triggerToast("📦 Đã lấy hàng tại Shop! Hãy di chuyển tới địa chỉ khách.");
  renderOrderOnMap();
};

const demoOrderTemplate = {
  orderId: "ZM-7749",
  deliveryType: "express",
  shippingFee: 38500,
  distanceKm: 2.2,
  items: "2x Thịt Ba Chỉ Bò Mỹ, 1x Gạo ST25 (5kg), 1x Nấm Kim Châm",
  notes: "Gọi trước khi đến 5 phút, giao lên tầng 5 phòng 502",
  store: {
    name: "ZoneMart Bách Hóa Cầu Giấy",
    address: "245 Cầu Giấy, P. Dịch Vọng, Hà Nội",
    phone: "024 1234 5678",
    lat: 21.0360,
    lng: 105.7985
  },
  customer: {
    name: "Anh Hoàng Huy",
    address: "Số 165 Cầu Giấy, P. Dịch Vọng, Hà Nội",
    phone: "0912 345 678",
    lat: 21.0315,
    lng: 105.7910
  }
};

const handleConfirmDelivered = () => {
  currentStep.value = "delivered";
  shiftStats.value.todayEarnings += activeOrder.value.shippingFee;
  shiftStats.value.completedOrders += 1;
  triggerToast(`🎉 Đã giao hàng thành công! +${activeOrder.value.shippingFee.toLocaleString('vi-VN')} ₫ vào ví.`);
  if (activeOrder.value) {
    shiftStats.value.todayEarnings += activeOrder.value.shippingFee;
    shiftStats.value.completedOrders += 1;
    shiftStats.value.totalKm = Number((shiftStats.value.totalKm + activeOrder.value.distanceKm).toFixed(1));
    triggerToast(`🎉 Đã giao hàng thành công! +${activeOrder.value.shippingFee.toLocaleString('vi-VN')} ₫ vào ví.`);
  }
  renderOrderOnMap();
};

const handleAcceptNewOrder = () => {
  activeOrder.value = { ...demoOrderTemplate };
  currentStep.value = "accepted";
  triggerToast("⚡ Đã nhận đơn hàng mới! Hãy di chuyển đến quán.");
  renderOrderOnMap();
};

const handleReportIssue = () => {
  const reason = prompt("Nhập lý do báo cáo sự cố (Khách không nhận / Shop đóng cửa):");
  if (reason) {
    alert(`Đã ghi nhận báo cáo: "${reason}". Tổng đài CSKH 1900 6868 sẽ hỗ trợ bạn ngay.`);
    currentStep.value = "idle";
    renderOrderOnMap();
  }
};

watch(isOnline, () => {
  renderOrderOnMap();
});

const onFullscreenChange = () => {
  isFullscreen.value = !!document.fullscreenElement;
  setTimeout(() => map?.invalidateSize(), 200);
};

onMounted(() => {
  initMap();
  document.addEventListener("fullscreenchange", onFullscreenChange);
  // Nếu đang mở hoạt động sẵn, tự động định vị vị trí người dùng
  if (isOnline.value) {
    locateAndTrackDriver(true);
    startLocationWatch();
  }
});

onUnmounted(() => {
  stopLocationWatch();
  document.removeEventListener("fullscreenchange", onFullscreenChange);
  if (map) {
    map.remove();
    map = null;
  }
});
</script>

<template>
  <div class="shipper-dashboard">
    <!-- Toast Popup -->
    <div v-if="showToast" class="toast-bar">
      <i class="bi bi-info-circle-fill"></i>
      <span>{{ toastMsg }}</span>
    </div>

    <div class="dashboard-container">
      <!-- 1. Top Driver Status Bar -->
      <header class="driver-header-card">
        <div class="driver-profile-info">
          <div class="driver-avatar-box">
            <span class="avatar-emoji">🛵</span>
            <span class="online-indicator" :class="{ 'is-active': isOnline }"></span>
          </div>

          <div class="driver-text-meta">
            <div class="name-badge-row">
              <h1 class="driver-name">Trần Văn Bình</h1>
              <span class="rating-pill">⭐ 4.98 (520 cuốc)</span>
              <span class="rating-pill">⭐ 5.0 (0 cuốc)</span>
            </div>
            <p class="vehicle-info">
              <span>Honda Airblade 150</span>
              <span class="dot-sep">•</span>
              <span class="license-plate">29M1-9999</span>
              <span class="dot-sep">•</span>
              <span class="hub-text">Khu vực: Cầu Giấy (10km)</span>
            </p>
          </div>
        </div>

        <!-- BIG TACTILE TOGGLE SWITCH (BẬT / TẮT HOẠT ĐỘNG) -->
        <div class="toggle-action-wrapper">
          <div class="toggle-status-desc">
            <span class="status-title">{{ isOnline ? "Đang BẬT Nhận Đơn" : "Đang TẠM NGHỈ" }}</span>
            <small class="status-subtitle">{{ isOnline ? "Sẵn sàng nhận cuốc hỏa tốc" : "Tắt sóng quét đơn hàng" }}</small>
          </div>

          <button
            class="active-toggle-btn"
            :class="{ 'online': isOnline, 'offline': !isOnline }"
            @click="toggleOnline"
            :title="isOnline ? 'Nhấn để tạm nghỉ' : 'Nhấn để bật hoạt động'"
          >
            <span class="toggle-thumb">
              <i class="bi" :class="isOnline ? 'bi-lightning-charge-fill' : 'bi-power'"></i>
            </span>
            <span class="toggle-label">{{ isOnline ? "BẬT HOẠT ĐỘNG" : "TẮT HOẠT ĐỘNG" }}</span>
          </button>
        </div>
      </header>

      <!-- 2. Shift Quick Summary (4 Stats) -->
      <section class="shift-stats-row">
        <div class="stat-card">
          <div class="stat-icon-wrap bg-green">
            <i class="bi bi-cash-stack"></i>
          </div>
          <div>
            <span class="stat-caption">Thu nhập hôm nay</span>
            <strong class="stat-figure text-green">{{ shiftStats.todayEarnings.toLocaleString('vi-VN') }} ₫</strong>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrap bg-blue">
            <i class="bi bi-box-seam-fill"></i>
          </div>
          <div>
            <span class="stat-caption">Số cuốc hoàn tất</span>
            <strong class="stat-figure">{{ shiftStats.completedOrders }} đơn</strong>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrap bg-orange">
            <i class="bi bi-clock-history"></i>
          </div>
          <div>
            <span class="stat-caption">Thời gian Online</span>
            <strong class="stat-figure">{{ shiftStats.onlineHours }}</strong>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrap bg-purple">
            <i class="bi bi-speedometer2"></i>
          </div>
          <div>
            <span class="stat-caption">Quãng đường đã chạy</span>
            <strong class="stat-figure">{{ shiftStats.totalKm }} km</strong>
          </div>
        </div>
      </section>

      <!-- 3. Main Operational View: Map + Mission Panel -->
      <div class="operational-grid">
        <!-- MAP SECTION (GOOGLE MAPS AUTHENTIC UI) -->
        <section class="map-card-wrapper" :class="{ 'is-fullscreen': isFullscreen }">
          <!-- 1. Google Maps Floating Search Bar & Status Chips (Top) -->
          <div class="gm-top-controls">
            <div class="gm-search-box">
              <div class="gm-search-icon">
                <i class="bi bi-geo-alt-fill text-danger"></i>
              </div>
              <input
                type="text"
                class="gm-search-input"
                :value="locationAddress"
                readonly
              />
              <div class="gm-search-actions">
                <button class="gm-icon-btn" title="Định vị tâm bản đồ" @click="recenterMap">
                  <i class="bi bi-cursor-fill text-primary"></i>
                </button>
                <div class="gm-search-divider"></div>
                <button class="gm-icon-btn" title="Tìm kiếm trên Google Maps" @click="triggerToast('Đang kết nối cơ sở dữ liệu Google Maps...')">
                  <i class="bi bi-search"></i>
                </button>
              </div>
            </div>

            <div class="gm-status-chips">
              <!-- Live Traffic Toggle Chip -->
              <button
                class="gm-chip gm-traffic-chip"
                :class="{ 'chip-active': showTraffic }"
                @click="toggleTraffic"
                title="Bật/Tắt dữ liệu giao thông trực tiếp Google"
              >
                <span class="traffic-dots">
                  <span class="dot dot-green"></span>
                  <span class="dot dot-orange"></span>
                  <span class="dot dot-red"></span>
                </span>
                <span>Giao thông</span>
              </button>

              <!-- Radar Radius Info Chip -->
              <div class="gm-chip gm-radius-chip">
                <i class="bi bi-broadcast"></i>
                <span>{{ isOnline ? "Bán kính: 10km" : "Ngoại tuyến" }}</span>
              </div>
            </div>
          </div>

          <!-- 2. Google Maps Layer Switcher & Watermark (Bottom Left) -->
          <div class="gm-bottom-left-controls">
            <div
              class="gm-layer-card"
              @click="toggleMapType"
              :title="mapType === 'roadmap' ? 'Xem chế độ vệ tinh Google' : 'Xem chế độ bản đồ Google'"
            >
              <div class="gm-layer-thumb" :class="mapType === 'roadmap' ? 'thumb-satellite' : 'thumb-roadmap'">
                <span class="gm-layer-badge">{{ mapType === 'roadmap' ? 'Vệ tinh' : 'Bản đồ' }}</span>
              </div>
            </div>

            <!-- Official Google Logo Watermark -->
            <div class="gm-watermark-logo">
              <span class="g-blue">G</span><span class="g-red">o</span><span class="g-yellow">o</span><span class="g-blue">g</span><span class="g-green">l</span><span class="g-red">e</span>
            </div>
          </div>

          <!-- 3. Google Maps Floating Controls Stack (Bottom Right) -->
          <div class="gm-bottom-right-controls">
            <!-- Fullscreen Button -->
            <button class="gm-ctrl-btn" @click="toggleFullscreen" :title="isFullscreen ? 'Thu nhỏ' : 'Toàn màn hình'">
              <i class="bi" :class="isFullscreen ? 'bi-fullscreen-exit' : 'bi-arrows-fullscreen'"></i>
            </button>

            <!-- My Location Crosshair -->
            <button
              class="gm-ctrl-btn gm-my-location"
              :class="{ 'is-locating': isLocating }"
              @click="recenterMap"
              title="Định vị vị trí GPS chính xác của bạn"
            >
              <svg viewBox="0 0 24 24" width="20" height="20" :fill="isLocating ? '#1a73e8' : '#5f6368'">
                <path d="M12 8c-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4-1.79-4-4-4zm8.94 3A8.994 8.994 0 0 0 13 3.06V1h-2v2.06A8.994 8.994 0 0 0 3.06 11H1v2h2.06A8.994 8.994 0 0 0 11 20.94V23h2v-2.06A8.994 8.994 0 0 0 20.94 13H23v-2h-2.06zM12 19c-3.87 0-7-3.13-7-7s3.13-7 7-7 7 3.13 7 7-3.13 7-7 7z"/>
              </svg>
            </button>

            <!-- Street View Pegman (Yellow Man) -->
            <button
              class="gm-ctrl-btn gm-pegman"
              @click="triggerToast('Chế độ xem phố Google Street View: Đang tải hình ảnh 360°...')"
              title="Chế độ xem phố Street View"
            >
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path d="M12 2c1.1 0 2 .9 2 2s-.9 2-2 2-2-.9-2-2 .9-2 2-2zm4 7h-2.5v13h-3v-6h-1v6h-3V9H4c-.55 0-1-.45-1-1s.45-1 1-1h12c.55 0 1 .45 1 1s-.45 1-1 1z" fill="#f4b400"/>
              </svg>
            </button>

            <!-- Google Maps Zoom Group -->
            <div class="gm-zoom-group">
              <button class="gm-zoom-btn" @click="zoomIn" title="Phóng to">+</button>
              <div class="gm-zoom-divider"></div>
              <button class="gm-zoom-btn" @click="zoomOut" title="Thu nhỏ">−</button>
            </div>
          </div>

          <!-- 4. Google Maps Footer Legal Notice -->
          <div class="gm-footer-legal">
            <span>Dữ liệu bản đồ ©2026 Google • Điều khoản</span>
          </div>

          <!-- Leaflet Map Element -->
          <div id="shipperMap" class="leaflet-map-element" :class="{ 'map-offline': !isOnline }"></div>

          <!-- Offline Dim Overlay -->
          <div v-if="!isOnline" class="offline-map-overlay">
            <div class="offline-prompt-box">
              <i class="bi bi-moon-stars-fill prompt-icon"></i>
              <h3>Bạn đang tạm nghỉ</h3>
              <p>Gạt nút "BẬT HOẠT ĐỘNG" phía trên để hệ thống quét đơn hàng hỏa tốc gần bạn nhất.</p>
              <button class="btn btn-primary" @click="toggleOnline">
                <i class="bi bi-power"></i> Bật Hoạt Động Ngay
              </button>
            </div>
          </div>
        </section>

        <!-- MISSION / ORDER CONTROLLER PANEL -->
        <aside class="mission-panel">
          <!-- CASE 1: ĐANG TRỰC TUYẾN & CÓ ĐƠN HÀNG HOẠT ĐỘNG -->
          <div v-if="isOnline && currentStep !== 'idle' && currentStep !== 'delivered'" class="active-mission-card">
          <div v-if="isOnline && currentStep !== 'idle' && currentStep !== 'delivered' && activeOrder" class="active-mission-card">
            <div class="mission-header-row">
              <span class="badge-express">
                <i class="bi bi-lightning-charge-fill"></i>
                HỎA TỐC 10KM
              </span>
              <span class="fee-tag">Thù lao: <strong>{{ activeOrder.shippingFee.toLocaleString('vi-VN') }} ₫</strong></span>
            </div>

            <!-- Steps tracker -->
            <div class="delivery-steps-bar">
              <div class="step-item" :class="{ 'done': currentStep === 'accepted' || currentStep === 'picked', 'current': currentStep === 'accepted' }">
                <span class="step-circle">1</span>
                <span class="step-name">Lấy hàng tại Shop</span>
              </div>
              <div class="step-line" :class="{ 'active': currentStep === 'picked' }"></div>
              <div class="step-item" :class="{ 'done': currentStep === 'picked', 'current': currentStep === 'picked' }">
                <span class="step-circle">2</span>
                <span class="step-name">Giao cho Khách</span>
              </div>
            </div>

            <!-- Step 1: Đi lấy hàng tại Shop -->
            <div v-if="currentStep === 'accepted'" class="checkpoint-box shop-checkpoint">
              <div class="cp-header">
                <i class="bi bi-shop-window cp-icon"></i>
                <div>
                  <span class="cp-caption">ĐIỂM LẤY HÀNG (CÁCH 1.2 KM)</span>
                  <h3 class="cp-title">{{ activeOrder.store.name }}</h3>
                </div>
              </div>

              <p class="cp-address">
                <i class="bi bi-geo-alt-fill"></i> {{ activeOrder.store.address }}
              </p>

              <div class="cp-items-list">
                <strong>Món cần lấy:</strong> {{ activeOrder.items }}
              </div>

              <div class="cp-action-row">
                <a :href="'tel:' + activeOrder.store.phone" class="btn btn-call">
                  <i class="bi bi-telephone-fill"></i> Gọi Quán
                </a>
                <button class="btn btn-primary flex-1" @click="handleConfirmPicked">
                  📦 ĐÃ LẤY ĐỦ HÀNG ➜
                </button>
              </div>
            </div>

            <!-- Step 2: Giao hàng tới Khách -->
            <div v-else-if="currentStep === 'picked'" class="checkpoint-box customer-checkpoint">
              <div class="cp-header">
                <i class="bi bi-person-check-fill cp-icon text-green"></i>
                <div>
                  <span class="cp-caption">ĐIỂM GIAO HÀNG (CÁCH 1.0 KM)</span>
                  <h3 class="cp-title">{{ activeOrder.customer.name }}</h3>
                </div>
              </div>

              <p class="cp-address">
                <i class="bi bi-geo-alt-fill"></i> {{ activeOrder.customer.address }}
              </p>

              <div class="cp-notes" v-if="activeOrder.notes">
                <i class="bi bi-chat-left-text"></i> Ghi chú khách: <em>"{{ activeOrder.notes }}"</em>
              </div>

              <div class="cp-action-row">
                <a :href="'tel:' + activeOrder.customer.phone" class="btn btn-call">
                  <i class="bi bi-telephone-fill"></i> Gọi Khách
                </a>
                <button class="btn btn-success flex-1" @click="handleConfirmDelivered">
                  🎉 XÁC NHẬN ĐÃ GIAO XONG
                </button>
              </div>

              <button class="btn-report-link" @click="handleReportIssue">
                ⚠️ Khách không nhận / Báo cáo sự cố
              </button>
            </div>
          </div>

          <!-- CASE 2: ĐƠN HÀNG VỪA GIAO XONG -->
          <div v-else-if="isOnline && currentStep === 'delivered'" class="mission-status-card finished-card">
          <div v-else-if="isOnline && currentStep === 'delivered' && activeOrder" class="mission-status-card finished-card">
            <div class="status-icon-circle bg-green">
              <i class="bi bi-check-lg"></i>
            </div>
            <h3>Giao Hàng Thành Công!</h3>
            <p>Thù lao <strong>+{{ activeOrder.shippingFee.toLocaleString('vi-VN') }} ₫</strong> đã được chuyển trực tiếp vào ví tài xế của bạn.</p>
            <button class="btn btn-primary w-100" @click="currentStep = 'idle'">
              Tiếp tục tìm đơn mới
            </button>
          </div>

          <!-- CASE 3: ĐANG QUÉT ĐƠN TRONG BÁN KÍNH 10KM -->
          <div v-else-if="isOnline && currentStep === 'idle'" class="mission-status-card scanning-card">
            <div class="radar-scan-anim">
              <div class="radar-circle-1"></div>
              <div class="radar-circle-2"></div>
              <i class="bi bi-radar radar-icon"></i>
            </div>
            <h3>Đang quét đơn hàng gần bạn...</h3>
            <p>Hệ thống tự động tìm đơn thực phẩm & đồ ăn nóng trong bán kính 10km khu vực Cầu Giấy.</p>
            <div class="demo-trigger-box">
              <small>Thử nghiệm nhận đơn:</small>
              <button class="btn btn-outline-primary" @click="handleAcceptNewOrder">
                ⚡ Nhận Đơn Hỏa Tốc Mới (Demo)
              </button>
            </div>
          </div>

          <!-- CASE 4: OFFLINE -->
          <div v-else class="mission-status-card offline-card">
            <div class="status-icon-circle bg-slate">
              <i class="bi bi-cup-hot-fill"></i>
            </div>
            <h3>Ca làm việc đang tạm dừng</h3>
            <p>Bật hoạt động để tiếp tục nhận cuốc giao hỏa tốc từ các đối tác siêu thị mini và quán ăn.</p>
            <button class="btn btn-primary w-100" @click="toggleOnline">
              Bật Hoạt Động Ngay
            </button>
          </div>
        </aside>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ==========================================================================
   GLOBAL LAYOUT
   ========================================================================== */
.shipper-dashboard {
  background-color: #f8fafc;
  min-height: calc(100vh - 72px);
  padding: 28px 24px 72px 24px;
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  color: #1e293b;
}

.dashboard-container {
  max-width: 1320px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* Toast Bar */
.toast-bar {
  position: fixed;
  top: 86px;
  right: 28px;
  background-color: #0f172a;
  color: #ffffff;
  padding: 12px 20px;
  border-radius: 12px;
  font-size: 13.5px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 8px;
  z-index: 9999;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  animation: slideToast 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes slideToast {
  from { transform: translateY(-20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

/* ==========================================================================
   1. TOP DRIVER STATUS BAR
   ========================================================================== */
.driver-header-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 24px 28px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
}

.driver-profile-info {
  display: flex;
  align-items: center;
  gap: 18px;
}

.driver-avatar-box {
  position: relative;
  width: 56px;
  height: 56px;
  background: #fff7ed;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.avatar-emoji {
  font-size: 30px;
}

.online-indicator {
  position: absolute;
  bottom: -2px;
  right: -2px;
  width: 15px;
  height: 15px;
  border-radius: 50%;
  background: #ef4444;
  border: 2.5px solid #ffffff;
}

.online-indicator.is-active {
  background: #22c55e;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.25);
  animation: pulseGreen 2s infinite;
}

@keyframes pulseGreen {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.15); }
}

.driver-text-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.name-badge-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.driver-name {
  font-size: 22px;
  font-weight: 900;
  color: #0f172a;
  margin: 0;
  letter-spacing: -0.02em;
}

.rating-pill {
  font-size: 12px;
  font-weight: 800;
  background: #fefce8;
  color: #d97706;
  padding: 3px 8px;
  border-radius: 20px;
  border: 1px solid #fef08a;
}

.vehicle-info {
  font-size: 13px;
  color: #64748b;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.license-plate {
  font-weight: 800;
  color: #0f172a;
  background: #f1f5f9;
  padding: 2px 6px;
  border-radius: 6px;
}

.dot-sep {
  color: #cbd5e1;
}

/* Big Toggle Button */
.toggle-action-wrapper {
  display: flex;
  align-items: center;
  gap: 16px;
}

.toggle-status-desc {
  text-align: right;
}

.status-title {
  display: block;
  font-size: 14px;
  font-weight: 800;
  color: #0f172a;
}

.status-subtitle {
  font-size: 12px;
  color: #64748b;
}

.active-toggle-btn {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 8px 18px 8px 10px;
  border-radius: 9999px;
  border: none;
  cursor: pointer;
  font-family: inherit;
  font-size: 13.5px;
  font-weight: 800;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.active-toggle-btn.online {
  background: #22c55e;
  color: #ffffff;
  box-shadow: 0 4px 16px rgba(34, 197, 94, 0.35);
}

.active-toggle-btn.online:hover {
  background: #16a34a;
  transform: scale(1.02);
}

.active-toggle-btn.offline {
  background: #e2e8f0;
  color: #475569;
}

.active-toggle-btn.offline:hover {
  background: #cbd5e1;
  color: #0f172a;
}

.toggle-thumb {
  width: 32px;
  height: 32px;
  background: #ffffff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
}

.active-toggle-btn.online .toggle-thumb {
  color: #22c55e;
}

.active-toggle-btn.offline .toggle-thumb {
  color: #64748b;
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

/* Radar scanning anim */
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

.btn-primary {
  background: #0f172a;
  color: #ffffff;
}

.btn-primary:hover {
  background: #ea580c;
  transform: translateY(-1px);
}

.btn-success {
  background: #16a34a;
  color: #ffffff;
}

.btn-success:hover {
  background: #15803d;
  transform: translateY(-1px);
}

.btn-outline-primary {
  background: #fff7ed;
  color: #ea580c;
  border: 1px dashed #fdba74;
}

.btn-outline-primary:hover {
  background: #ea580c;
  color: #ffffff;
}

.w-100 {
  width: 100%;
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

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1024px) {
  .operational-grid {
    grid-template-columns: 1fr;
  }
  
  .shift-stats-row {
    grid-template-columns: repeat(2, 1fr);
  }

  .map-card-wrapper {
    height: 440px;
  }
}

@media (max-width: 640px) {
  .shipper-dashboard {
    padding: 16px 14px 48px 14px;
  }

  .driver-header-card {
    flex-direction: column;
    align-items: stretch;
  }

  .toggle-action-wrapper {
    justify-content: space-between;
  }

  .shift-stats-row {
    grid-template-columns: 1fr;
  }
}
</style>

