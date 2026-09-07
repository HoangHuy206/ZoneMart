<script setup lang="ts">
/**
 * ================================================================
 * FORM BẢN ĐỒ THỜI GIAN THỰC (REAL-TIME INTERACTIVE MAP) - Phụ trách: Huy
 * Tính năng thời gian thực:
 * 1. GPS Live & Kéo thả ghim vị trí cập nhật tọa độ tức thì
 * 2. Vòng tròn bán kính 10km co giãn mượt mà thời gian thực
 * 3. Tự động tính khoảng cách Haversine & phân loại Hỏa Tốc (<= 3km) vs Thường
 * 4. Vẽ tuyến đường giao hàng thời gian thực (Live Polyline)
 * 5. Trình mô phỏng tài xế Shipper di chuyển trên bản đồ từng giây (Luồng 4)
 * ================================================================
 */
import { ref, onMounted, onUnmounted, computed, watch } from "vue";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

// 1. TỌA ĐỘ VỊ TRÍ KHÁCH HÀNG (Mặc định Cầu Giấy, Hà Nội)
const userLocation = ref({
  lat: 21.0333,
  lng: 105.7944,
  address: "Quận Cầu Giấy, TP. Hà Nội"
});

// Bán kính lọc (1km -> 15km, chuẩn Luồng 1 là 10km)
const radiusKm = ref<number>(10);

// Danh sách các Cửa Hàng mẫu trong hệ thống ZoneMart
const storeList = ref([
  {
    id: "st_1",
    name: "ZoneMart Bách Hóa Cầu Giấy",
    address: "245 Cầu Giấy, Hà Nội",
    lat: 21.0345,
    lng: 105.7990,
    openHours: "07:00 - 22:00",
    category: "Thực phẩm tươi sống",
    rating: 4.9
  },
  {
    id: "st_2",
    name: "Siêu Thị Trái Cây Tươi Xanh",
    address: "88 Trần Thái Tông, Cầu Giấy",
    lat: 21.0310,
    lng: 105.7870,
    openHours: "08:00 - 21:30",
    category: "Trái cây & Đồ uống",
    rating: 4.8
  },
  {
    id: "st_3",
    name: "Tiệm Bánh Mì & Cà Phê Zone",
    address: "12 Hồ Tùng Mậu, Mai Dịch",
    lat: 21.0380,
    lng: 105.7760,
    openHours: "06:30 - 23:00",
    category: "Thức ăn nhanh",
    rating: 4.7
  },
  {
    id: "st_4",
    name: "Tổng Kho Tiêu Dùng Mỹ Đình",
    address: "Lê Đức Thọ, Nam Từ Liêm",
    lat: 21.0250,
    lng: 105.7680,
    openHours: "08:00 - 21:00",
    category: "Gia dụng & Đời sống",
    rating: 4.6
  },
  {
    id: "st_5",
    name: "ZoneMart Đại Siêu Thị Tây Hồ",
    address: "Võ Chí Công, Tây Hồ",
    lat: 21.0650,
    lng: 105.8050,
    openHours: "08:00 - 22:30",
    category: "Bách hóa tổng hợp",
    rating: 4.9
  },
  {
    id: "st_6",
    name: "Kho Nông Sản Hà Đông (Ngoài 10km)",
    address: "Quang Trung, Hà Đông",
    lat: 20.9700,
    lng: 105.7700,
    openHours: "07:00 - 20:00",
    category: "Nông sản",
    rating: 4.3
  }
]);

// Cửa hàng đang được chọn để định tuyến
const selectedStoreId = ref<string>("st_1");
const selectedStore = computed(() => {
  return storeList.value.find(s => s.id === selectedStoreId.value) || storeList.value[0];
});

// CÔNG THỨC HAVERSINE TÍNH KHOẢNG CÁCH THỜI GIAN THỰC
const calculateDistance = (lat1: number, lon1: number, lat2: number, lon2: number): number => {
  const R = 6371; // Bán kính Trái Đất (km)
  const dLat = (lat2 - lat1) * (Math.PI / 180);
  const dLon = (lon2 - lon1) * (Math.PI / 180);
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * (Math.PI / 180)) * Math.cos(lat2 * (Math.PI / 180)) *
    Math.sin(dLon / 2) * Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return Math.round(R * c * 10) / 10;
};

// Khoảng cách thời gian thực tới cửa hàng đang chọn
const liveDistance = computed(() => {
  return calculateDistance(
    userLocation.value.lat,
    userLocation.value.lng,
    selectedStore.value.lat,
    selectedStore.value.lng
  );
});

// Phân loại phí ship & Hỏa Tốc thời gian thực theo LUỒNG 3
const liveShipping = computed(() => {
  const dist = liveDistance.value;
  const isExpressEligible = dist <= 3.0; // <= 3km đủ điều kiện Hỏa Tốc
  const expressFee = isExpressEligible ? Math.round((15000 * 1) * 1.5) : null;
  const standardFee = Math.round(15000 + (dist > 3 ? (dist - 3) * 4000 : 0));
  const etaMinutes = isExpressEligible ? Math.round(dist * 5 + 10) : Math.round(dist * 7 + 15);

  return {
    dist,
    isExpressEligible,
    expressFee,
    standardFee,
    etaMinutes
  };
});

// Danh sách cửa hàng nằm trong bán kính đã lọc
const storesInRadius = computed(() => {
  return storeList.value.map(s => {
    const d = calculateDistance(userLocation.value.lat, userLocation.value.lng, s.lat, s.lng);
    return { ...s, distanceKm: d };
  }).filter(s => s.distanceKm <= radiusKm.value)
    .sort((a, b) => a.distanceKm - b.distanceKm);
});

// 2. BIẾN QUẢN LÝ BẢN ĐỒ LEAFLET
let map: L.Map | null = null;
let userMarker: L.Marker | null = null;
let radiusCircle: L.Circle | null = null;
let storeMarkers: L.Marker[] = [];
let routeLine: L.Polyline | null = null;
let shipperMarker: L.Marker | null = null;

// Khởi tạo các Custom Icon SVG sắc nét
const userIcon = L.divIcon({
  className: "custom-leaflet-pin user-pin",
  html: `<div class="user-pulse"></div><div class="user-center">📍</div>`,
  iconSize: [40, 40],
  iconAnchor: [20, 20]
});

const storeIcon = L.divIcon({
  className: "custom-leaflet-pin store-pin",
  html: `<div class="store-badge">🏪</div>`,
  iconSize: [36, 36],
  iconAnchor: [18, 18]
});

const shipperIcon = L.divIcon({
  className: "custom-leaflet-pin shipper-pin",
  html: `<div class="shipper-avatar">🛵💨</div>`,
  iconSize: [42, 42],
  iconAnchor: [21, 21]
});

// KHỞI TẠO BẢN ĐỒ KHI COMPONENT MOUNT
onMounted(() => {
  initMap();
});

onUnmounted(() => {
  stopSimulation();
  if (map) {
    map.remove();
    map = null;
  }
});

const initMap = () => {
  const mapContainer = document.getElementById("zoneMartMap");
  if (!mapContainer) return;

  // Tạo map Leaflet với tọa độ khách hàng
  map = L.map("zoneMartMap", {
    center: [userLocation.value.lat, userLocation.value.lng],
    zoom: 13,
    zoomControl: false
  });

  L.control.zoom({ position: "bottomright" }).addTo(map);

  // Thêm TileLayer bản đồ OpenStreetMap
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution: "© OpenStreetMap contributors | ZoneMart",
    maxZoom: 19
  }).addTo(map);

  // 1. Thêm Ghim Khách Hàng (KÉO THẢ ĐƯỢC - DRAGGABLE REAL-TIME)
  userMarker = L.marker([userLocation.value.lat, userLocation.value.lng], {
    icon: userIcon,
    draggable: true,
    title: "Vị trí của bạn (Kéo thả để đổi vị trí)"
  }).addTo(map);

  userMarker.bindPopup("<b>👤 Vị trí của bạn</b><br>Kéo ghim hoặc click trên bản đồ để đổi vị trí.");

  // Sự kiện kéo ghim thời gian thực
  userMarker.on("drag", (e: any) => {
    const newPos = e.target.getLatLng();
    userLocation.value.lat = Math.round(newPos.lat * 10000) / 10000;
    userLocation.value.lng = Math.round(newPos.lng * 10000) / 10000;
    userLocation.value.address = `Tọa độ: [${userLocation.value.lat}, ${userLocation.value.lng}]`;
    updateRealTimeVisuals();
  });

  // Sự kiện click trực tiếp lên bản đồ
  map.on("click", (e: L.LeafletMouseEvent) => {
    const { lat, lng } = e.latlng;
    userLocation.value.lat = Math.round(lat * 10000) / 10000;
    userLocation.value.lng = Math.round(lng * 10000) / 10000;
    userLocation.value.address = `Tọa độ: [${userLocation.value.lat}, ${userLocation.value.lng}]`;
    if (userMarker) {
      userMarker.setLatLng([lat, lng]);
    }
    updateRealTimeVisuals();
  });

  // 2. Thêm Vòng Tròn Bán Kính Radar (Mặc định 10km)
  radiusCircle = L.circle([userLocation.value.lat, userLocation.value.lng], {
    radius: radiusKm.value * 1000,
    color: "#22c55e",
    fillColor: "#22c55e",
    fillOpacity: 0.08,
    weight: 2,
    dashArray: "6, 6"
  }).addTo(map);

  // 3. Thêm các Marker Cửa Hàng
  renderStoreMarkers();

  // 4. Vẽ tuyến đường ban đầu
  updateRouteLine();
};

// Vẽ các Marker Cửa Hàng
const renderStoreMarkers = () => {
  if (!map) return;
  storeMarkers.forEach(m => m.remove());
  storeMarkers = [];

  storeList.value.forEach(store => {
    const dist = calculateDistance(userLocation.value.lat, userLocation.value.lng, store.lat, store.lng);
    const inRange = dist <= radiusKm.value;

    const marker = L.marker([store.lat, store.lng], {
      icon: storeIcon,
      opacity: inRange ? 1 : 0.4
    }).addTo(map!);

    marker.bindPopup(`
      <div style="font-size: 13px; font-family: inherit;">
        <strong style="color: #0f172a;">🏪 ${store.name}</strong><br>
        <span style="color: #64748b;">${store.address}</span><br>
        <span style="color: #ea580c; font-weight: bold;">Khoảng cách: ${dist} km</span><br>
        <span style="color: ${dist <= 3 ? '#16a34a' : '#2563eb'}; font-weight: 600;">
          ${dist <= 3 ? '⚡ Hỗ trợ Hỏa Tốc (<=3km)' : '🛵 Ship Thường'}
        </span>
      </div>
    `);

    marker.on("click", () => {
      selectedStoreId.value = store.id;
      updateRouteLine();
    });

    storeMarkers.push(marker);
  });
};

// Cập nhật tuyến đường (Polyline) nối giữa Shop và Khách
const updateRouteLine = () => {
  if (!map) return;
  if (routeLine) {
    routeLine.remove();
    routeLine = null;
  }

  const isExpress = liveShipping.value.isExpressEligible;
  routeLine = L.polyline(
    [
      [selectedStore.value.lat, selectedStore.value.lng],
      [userLocation.value.lat, userLocation.value.lng]
    ],
    {
      color: isExpress ? "#ef4444" : "#2563eb",
      weight: 4,
      dashArray: "8, 8"
    }
  ).addTo(map);
};

// CẬP NHẬT TẤT CẢ PHẦN TỬ THỜI GIAN THỰC
const updateRealTimeVisuals = () => {
  if (radiusCircle) {
    radiusCircle.setLatLng([userLocation.value.lat, userLocation.value.lng]);
    radiusCircle.setRadius(radiusKm.value * 1000);
  }
  renderStoreMarkers();
  updateRouteLine();
};

// Theo dõi thay đổi bán kính -> Cập nhật vòng tròn Leaflet ngay lập tức
watch(radiusKm, () => {
  updateRealTimeVisuals();
});

// Theo dõi thay đổi cửa hàng đang chọn
watch(selectedStoreId, () => {
  updateRouteLine();
});

// ĐỊNH VỊ GPS THỜI GIAN THỰC
const isLocating = ref(false);
const handleGetCurrentLocation = () => {
  if (!navigator.geolocation) {
    alert("Trình duyệt của bạn không hỗ trợ Geolocation!");
    return;
  }

  isLocating.value = true;
  navigator.geolocation.getCurrentPosition(
    (position) => {
      isLocating.value = false;
      const { latitude, longitude } = position.coords;
      userLocation.value.lat = Math.round(latitude * 10000) / 10000;
      userLocation.value.lng = Math.round(longitude * 10000) / 10000;
      userLocation.value.address = "Vị trí GPS thực tế của bạn";

      if (userMarker) {
        userMarker.setLatLng([latitude, longitude]);
      }
      if (map) {
        map.flyTo([latitude, longitude], 14, { duration: 1.2 });
      }
      updateRealTimeVisuals();
    },
    (err) => {
      isLocating.value = false;
      alert("Không thể lấy vị trí: " + err.message + ". Bản đồ đang dùng vị trí mẫu tại Hà Nội.");
    },
    { enableHighAccuracy: true, timeout: 5000 }
  );
};

// 3. TRÌNH MÔ PHỎNG SHIPPER DI CHUYỂN LIVE (LUỒNG 4)
const isSimulating = ref(false);
const simProgress = ref(0); // 0% -> 100%
const simEta = ref(0);
let simInterval: any = null;

const startSimulation = () => {
  if (!map) return;
  stopSimulation();

  isSimulating.value = true;
  simProgress.value = 0;
  simEta.value = liveShipping.value.etaMinutes;

  const startLat = selectedStore.value.lat;
  const startLng = selectedStore.value.lng;
  const endLat = userLocation.value.lat;
  const endLng = userLocation.value.lng;

  // Tạo Shipper Marker nếu chưa có
  if (!shipperMarker) {
    shipperMarker = L.marker([startLat, startLng], {
      icon: shipperIcon,
      zIndexOffset: 1000
    }).addTo(map);
  } else {
    shipperMarker.setLatLng([startLat, startLng]);
  }

  // Chạy mô phỏng di chuyển mượt mà (60 bước)
  const totalSteps = 100;
  let currentStep = 0;

  simInterval = setInterval(() => {
    currentStep++;
    simProgress.value = currentStep;
    simEta.value = Math.max(0, Math.round((1 - currentStep / totalSteps) * liveShipping.value.etaMinutes));

    const curLat = startLat + (endLat - startLat) * (currentStep / totalSteps);
    const curLng = startLng + (endLng - startLng) * (currentStep / totalSteps);

    if (shipperMarker) {
      shipperMarker.setLatLng([curLat, curLng]);
    }

    if (currentStep >= totalSteps) {
      stopSimulation();
      alert("🎉 ĐƠN HÀNG ĐÃ TỚI NƠI! Tài xế Shipper đã giao thành công tới bạn theo Luồng 4.");
    }
  }, 100);
};

const stopSimulation = () => {
  if (simInterval) {
    clearInterval(simInterval);
    simInterval = null;
  }
  isSimulating.value = false;
};
</script>

<template>
  <div class="map-wrapper">
    <!-- Thanh điều khiển công cụ thời gian thực -->
    <div class="map-controls-bar">
      <div class="brand-sub">
        <h2>🗺️ Bản Đồ Điều Phối & Bán Kính Thời Gian Thực</h2>
        <p class="desc">
          📍 <strong>Vị trí của bạn:</strong> {{ userLocation.address }} 
          <span class="coord-tag">[{{ userLocation.lat }}, {{ userLocation.lng }}]</span>
        </p>
      </div>

      <div class="actions-group">
        <!-- Nút định vị GPS -->
        <button class="btn btn-gps" :disabled="isLocating" @click="handleGetCurrentLocation">
          {{ isLocating ? "⏳ Đang dò tìm..." : "🎯 Định Vị GPS Của Tôi" }}
        </button>

        <!-- Thanh trượt điều chỉnh bán kính thời gian thực -->
        <div class="radius-slider-box">
          <div class="slider-header">
            <span>Bán kính quét:</span>
            <strong class="km-highlight">{{ radiusKm }} km</strong>
            <span class="sub-rule">(Chuẩn Luồng 1: 10km)</span>
          </div>
          <input 
            type="range" 
            min="1" 
            max="15" 
            step="0.5" 
            v-model.number="radiusKm" 
            class="range-slider"
          />
        </div>
      </div>
    </div>

    <!-- Layout chính: Bản đồ Leaflet & Cột thông số Real-time -->
    <div class="main-map-grid">
      <!-- Cột trái: Bản đồ tương tác Leaflet -->
      <div class="map-viewport-card">
        <div id="zoneMartMap" class="leaflet-container-box"></div>

        <!-- Hướng dẫn thao tác nhanh đè trên map -->
        <div class="map-floating-hint">
          💡 <em>Kéo thả ghim đỏ hoặc click trực tiếp lên bất kỳ điểm nào trên bản đồ để thay đổi vị trí.</em>
        </div>

        <!-- Bảng thông số tuyến đường thời gian thực -->
        <div class="route-panel">
          <div class="route-header">
            <div class="route-title">
              <span>Tuyến Giao Hàng Trực Tiếp:</span>
              <h4>🏪 {{ selectedStore.name }} ➜ 👤 Bạn</h4>
            </div>

            <!-- Nút chạy mô phỏng Shipper di chuyển -->
            <button 
              v-if="!isSimulating" 
              class="btn btn-simulate" 
              @click="startSimulation"
            >
              🛵 Mô Phỏng Shipper Chạy Live
            </button>
            <button 
              v-else 
              class="btn btn-stop" 
              @click="stopSimulation"
            >
              ⏸ Dừng Mô Phỏng ({{ simProgress }}%)
            </button>
          </div>

          <!-- Thanh tiến độ mô phỏng khi đang chạy -->
          <div v-if="isSimulating" class="simulation-progress-bar">
            <div class="progress-fill" :style="{ width: `${simProgress}%` }"></div>
            <div class="sim-meta">
              <span>🛵 Tài xế đang trên đường giao (Tốc độ: 35 km/h)</span>
              <span>Dự kiến đến: <strong>{{ simEta }} phút</strong></span>
            </div>
          </div>

          <!-- Bảng cước phí & Điều kiện Hỏa Tốc (Luồng 3) -->
          <div class="metrics-grid">
            <div class="metric-item">
              <span class="label">Khoảng cách thực:</span>
              <span class="value">{{ liveDistance }} km</span>
            </div>

            <div class="metric-item">
              <span class="label">Điều kiện dịch vụ:</span>
              <span class="badge badge-express" v-if="liveShipping.isExpressEligible">
                ⚡ Đạt chuẩn Hỏa Tốc (&le; 3km)
              </span>
              <span class="badge badge-standard" v-else>
                📦 Giao Thường (> 3km)
              </span>
            </div>

            <div class="metric-item" v-if="liveShipping.isExpressEligible">
              <span class="label">Phí Hỏa Tốc (QR Code):</span>
              <span class="value text-orange">{{ liveShipping.expressFee?.toLocaleString('vi-VN') }} ₫</span>
            </div>

            <div class="metric-item">
              <span class="label">Phí Tiêu Chuẩn (COD/QR):</span>
              <span class="value text-blue">{{ liveShipping.standardFee.toLocaleString('vi-VN') }} ₫</span>
            </div>

            <div class="metric-item">
              <span class="label">Thời gian giao ước tính:</span>
              <span class="value">~ {{ liveShipping.etaMinutes }} phút</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Cột phải: Danh sách các cửa hàng trong bán kính -->
      <div class="stores-sidebar">
        <div class="sidebar-head">
          <h3>Cửa Hàng Trong Bán Kính</h3>
          <span class="count-badge">{{ storesInRadius.length }} quán</span>
        </div>
        <p class="sidebar-sub">Tự động cập nhật tức thì theo bán kính <strong>{{ radiusKm }}km</strong></p>

        <div class="store-scroll-list">
          <div 
            v-for="store in storesInRadius" 
            :key="store.id"
            class="store-item-tile"
            :class="{ active: selectedStoreId === store.id }"
            @click="selectedStoreId = store.id"
          >
            <div class="tile-top">
              <strong class="name">{{ store.name }}</strong>
              <span class="distance">{{ store.distanceKm }} km</span>
            </div>
            <p class="addr">📍 {{ store.address }}</p>
            <div class="tile-bottom">
              <span class="cat">{{ store.category }}</span>
              <span class="express-chip" v-if="store.distanceKm <= 3">⚡ Hỏa Tốc</span>
            </div>
          </div>

          <div v-if="storesInRadius.length === 0" class="empty-state">
            ⚠️ Không có cửa hàng nào trong bán kính {{ radiusKm }}km.<br>
            Hãy kéo thanh trượt để tăng bán kính!
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.map-wrapper {
  max-width: 1280px;
  margin: 20px auto;
  padding: 0 20px;
}

/* Control Bar */
.map-controls-bar {
  background: #ffffff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  padding: 18px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.02);
}
.brand-sub h2 { margin: 0 0 6px 0; font-size: 20px; color: #0f172a; }
.desc { margin: 0; color: #64748b; font-size: 13px; }
.coord-tag { color: #2563eb; font-weight: 600; margin-left: 6px; }

.actions-group { display: flex; align-items: center; gap: 20px; flex-wrap: wrap; }

.radius-slider-box {
  background: #f8fafc;
  padding: 8px 16px;
  border-radius: 10px;
  border: 1px solid #e2e8f0;
}
.slider-header { font-size: 13px; color: #334155; margin-bottom: 4px; display: flex; align-items: center; gap: 6px; }
.km-highlight { color: #16a34a; font-size: 15px; }
.sub-rule { font-size: 11px; color: #64748b; }
.range-slider { width: 180px; cursor: pointer; }

/* Grid */
.main-map-grid {
  display: grid;
  grid-template-columns: 1fr 340px;
  gap: 20px;
}
@media (max-width: 950px) {
  .main-map-grid { grid-template-columns: 1fr; }
}

.map-viewport-card {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.leaflet-container-box {
  height: 480px;
  width: 100%;
  position: relative;
  z-index: 1;
}

.map-floating-hint {
  background: rgba(15, 23, 42, 0.85);
  color: #f8fafc;
  padding: 8px 14px;
  font-size: 12px;
  text-align: center;
  backdrop-filter: blur(4px);
}

/* Route & Metrics */
.route-panel { padding: 20px; background: #ffffff; border-top: 1px solid #f1f5f9; }
.route-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 14px;
  margin-bottom: 16px;
}
.route-title span { font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 700; letter-spacing: 0.5px; }
.route-title h4 { margin: 4px 0 0 0; font-size: 17px; color: #0f172a; }

.simulation-progress-bar {
  background: #f1f5f9;
  border-radius: 10px;
  padding: 3px;
  margin-bottom: 16px;
  position: relative;
  overflow: hidden;
}
.progress-fill {
  background: linear-gradient(90deg, #3b82f6, #f97316);
  height: 6px;
  border-radius: 6px;
  transition: width 0.1s linear;
}
.sim-meta { display: flex; justify-content: space-between; font-size: 12px; color: #475569; padding: 6px 8px 2px 8px; }

.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 12px;
  background: #f8fafc;
  padding: 14px;
  border-radius: 10px;
  border: 1px solid #e2e8f0;
}
.metric-item { display: flex; flex-direction: column; gap: 4px; }
.metric-item .label { font-size: 11px; color: #64748b; font-weight: 600; }
.metric-item .value { font-size: 15px; font-weight: 700; color: #0f172a; }
.text-orange { color: #ea580c !important; }
.text-blue { color: #2563eb !important; }

.badge { font-size: 11px; font-weight: 700; padding: 3px 8px; border-radius: 6px; display: inline-block; width: fit-content; }
.badge-express { background: #fee2e2; color: #b91c1c; }
.badge-standard { background: #eff6ff; color: #1e40af; }

/* Sidebar */
.stores-sidebar {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  padding: 20px;
  display: flex;
  flex-direction: column;
}
.sidebar-head { display: flex; justify-content: space-between; align-items: center; }
.sidebar-head h3 { margin: 0; font-size: 16px; color: #0f172a; }
.count-badge { background: #22c55e; color: #fff; font-size: 12px; font-weight: 700; padding: 2px 8px; border-radius: 12px; }
.sidebar-sub { margin: 4px 0 16px 0; font-size: 12px; color: #64748b; }

.store-scroll-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  max-height: 520px;
  overflow-y: auto;
}
.store-item-tile {
  padding: 14px;
  border-radius: 10px;
  border: 1px solid #e2e8f0;
  background: #f8fafc;
  cursor: pointer;
  transition: all 0.2s;
}
.store-item-tile:hover { border-color: #cbd5e1; background: #fff; }
.store-item-tile.active {
  border-color: #f97316;
  background: #fff7ed;
  box-shadow: 0 4px 6px -1px rgba(249, 115, 22, 0.1);
}

.tile-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px; }
.tile-top .name { font-size: 13px; color: #0f172a; }
.tile-top .distance { font-size: 12px; font-weight: 800; color: #ea580c; }
.addr { font-size: 11px; color: #64748b; margin: 0 0 8px 0; }
.tile-bottom { display: flex; justify-content: space-between; align-items: center; }
.cat { font-size: 11px; background: #e2e8f0; color: #475569; padding: 2px 6px; border-radius: 4px; }
.express-chip { font-size: 10px; background: #fee2e2; color: #dc2626; font-weight: 700; padding: 2px 6px; border-radius: 4px; }

.empty-state { text-align: center; padding: 40px 10px; color: #94a3b8; font-size: 13px; line-height: 1.5; }

/* Buttons */
.btn {
  border: none;
  cursor: pointer;
  padding: 10px 18px;
  border-radius: 8px;
  font-weight: 700;
  font-size: 13px;
  transition: all 0.2s;
}
.btn-gps { background: #0f172a; color: #fff; }
.btn-gps:hover { background: #1e293b; }
.btn-simulate { background: #2563eb; color: #fff; }
.btn-simulate:hover { background: #1d4ed8; }
.btn-stop { background: #dc2626; color: #fff; }
</style>

<style>
/* CSS Toàn cục cho Custom Leaflet Markers */
.custom-leaflet-pin {
  background: transparent;
  border: none;
}
.user-pin {
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
}
.user-pulse {
  position: absolute;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: rgba(37, 99, 235, 0.4);
  animation: leafletPulse 2s infinite;
}
.user-center {
  font-size: 24px;
  z-index: 2;
}
@keyframes leafletPulse {
  0% { transform: scale(0.6); opacity: 1; }
  100% { transform: scale(1.6); opacity: 0; }
}

.store-pin .store-badge {
  font-size: 24px;
  background: #fff;
  border: 2px solid #f97316;
  border-radius: 50%;
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 6px rgba(0,0,0,0.15);
}

.shipper-pin .shipper-avatar {
  font-size: 26px;
  background: #fff;
  border: 2px solid #2563eb;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 10px rgba(0,0,0,0.25);
  animation: bounce 1s infinite alternate;
}
@keyframes bounce {
  from { transform: translateY(0); }
  to { transform: translateY(-4px); }
}
</style>
