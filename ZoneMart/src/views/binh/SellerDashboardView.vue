<script setup lang="ts">
import { ref, reactive, computed } from "vue";

// Trạng thái mở cửa / nhận đơn hỏa tốc
const isStoreOpen = ref(true);
const toastMessage = ref("");
const showToast = ref(false);

const triggerToast = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 3000);
};

const toggleStoreOpen = () => {
  isStoreOpen.value = !isStoreOpen.value;
  triggerToast(
    isStoreOpen.value
      ? "Đã mở cửa gian hàng! Sẵn sàng nhận đơn hỏa tốc 10km."
      : "Đã tạm đóng cửa gian hàng. Khách hàng sẽ không thể đặt món mới lúc này."
  );
};

// Tab hiện tại trong dashboard: 'orders' | 'products' | 'settings'
const activeTab = ref<"orders" | "products" | "settings">("orders");

// Dữ liệu gian hàng
const storeInfo = reactive({
  id: "ZM-S882",
  name: "Vườn Rau Ba Vì - Nông Sản Sạch VietGAP",
  category: "Thực phẩm & Rau củ quả",
  address: "Số 48 đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội",
  phone: "0988 123 456",
  openHours: "06:30 - 21:30",
  bankName: "Vietcombank (VCB)",
  bankAccount: "1029384756",
  accountHolder: "NGUYEN VAN BA",
  radiusKm: 10,
  todayRevenue: 1850000,
  rating: 4.9,
  reviewsCount: 186
});

// Danh sách đơn hàng cần xử lý
interface OrderItem {
  name: string;
  qty: number;
  price: number;
}

interface SellerOrder {
  id: string;
  orderCode: string;
  customerName: string;
  customerPhone: string;
  customerAddress: string;
  distanceKm: number;
  timeAgo: string;
  paymentMethod: "ONLINE_QR" | "COD";
  isPaid: boolean;
  deliveryType: "express" | "standard";
  items: OrderItem[];
  totalAmount: number;
  status: "pending" | "preparing" | "delivering" | "completed" | "cancelled";
  shipperInfo?: {
    name: string;
    phone: string;
    licensePlate: string;
  };
}

const orders = ref<SellerOrder[]>([
  {
    id: "ord-01",
    orderCode: "ORD-98214",
    customerName: "Chị Mai Lan",
    customerPhone: "0912 345 678",
    customerAddress: "P.502 Chung cư Dịch Vọng, Cầu Giấy, Hà Nội",
    distanceKm: 1.2,
    timeAgo: "5 phút trước",
    paymentMethod: "ONLINE_QR",
    isPaid: true,
    deliveryType: "express",
    items: [
      { name: "Rau muống hữu cơ Ba Vì", qty: 2, price: 18000 },
      { name: "Cà chua bi Đà Lạt", qty: 1, price: 35000 },
      { name: "Trứng gà ta thảo mộc (Vỉ 10 quả)", qty: 1, price: 45000 }
    ],
    totalAmount: 116000,
    status: "pending"
  },
  {
    id: "ord-02",
    orderCode: "ORD-98215",
    customerName: "Anh Hoàng Minh",
    customerPhone: "0987 654 321",
    customerAddress: "Số 18 ngõ 20 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy",
    distanceKm: 2.5,
    timeAgo: "15 phút trước",
    paymentMethod: "ONLINE_QR",
    isPaid: true,
    deliveryType: "express",
    items: [
      { name: "Thịt ba chỉ heo tươi sạch", qty: 2, price: 65000 },
      { name: "Xà lách mỡ thủy canh", qty: 1, price: 22000 }
    ],
    totalAmount: 152000,
    status: "preparing",
    shipperInfo: {
      name: "Trần Văn Bình",
      phone: "0934 888 999",
      licensePlate: "29M1-9999"
    }
  },
  {
    id: "ord-03",
    orderCode: "ORD-98209",
    customerName: "Cô Thu Hà",
    customerPhone: "0903 111 222",
    customerAddress: "Số 92 đường Trần Thái Tông, Cầu Giấy",
    distanceKm: 0.8,
    timeAgo: "32 phút trước",
    paymentMethod: "COD",
    isPaid: false,
    deliveryType: "express",
    items: [
      { name: "Dưa leo baby giòn ngọt", qty: 1, price: 25000 },
      { name: "Khoai tây vàng Đà Lạt", qty: 1, price: 30000 }
    ],
    totalAmount: 55000,
    status: "delivering",
    shipperInfo: {
      name: "Nguyễn Văn Nam",
      phone: "0987 654 321",
      licensePlate: "29M1-8888"
    }
  },
  {
    id: "ord-04",
    orderCode: "ORD-98188",
    customerName: "Bác Tuấn Hưng",
    customerPhone: "0977 444 555",
    customerAddress: "Số 104 Xuân Thủy, Cầu Giấy, Hà Nội",
    distanceKm: 1.6,
    timeAgo: "1 giờ trước",
    paymentMethod: "ONLINE_QR",
    isPaid: true,
    deliveryType: "standard",
    items: [
      { name: "Cam sành Hàm Yên (1kg)", qty: 2, price: 42000 }
    ],
    totalAmount: 84000,
    status: "completed"
  }
]);

// Bộ lọc đơn hàng
const orderFilter = ref<"all" | "pending" | "preparing" | "delivering" | "completed">("all");

const filteredOrders = computed(() => {
  if (orderFilter.value === "all") return orders.value;
  return orders.value.filter(o => o.status === orderFilter.value);
});

const pendingCount = computed(() => orders.value.filter(o => o.status === "pending").length);
const preparingCount = computed(() => orders.value.filter(o => o.status === "preparing").length);

// Xử lý đơn hàng
const handleAcceptOrder = (orderId: string) => {
  const ord = orders.value.find(o => o.id === orderId);
  if (ord) {
    ord.status = "preparing";
    ord.shipperInfo = {
      name: "Trần Văn Bình",
      phone: "0934 888 999",
      licensePlate: "29M1-9999"
    };
    triggerToast(`Đã xác nhận đơn #${ord.orderCode}! Hệ thống đã điều phối Shipper hỏa tốc gần nhất.`);
  }
};

const handleHandoverShipper = (orderId: string) => {
  const ord = orders.value.find(o => o.id === orderId);
  if (ord) {
    ord.status = "delivering";
    triggerToast(`Đã bàn giao đơn #${ord.orderCode} cho tài xế ${ord.shipperInfo?.name}! Đang giao tới khách.`);
  }
};

const handleCancelOrder = (orderId: string) => {
  if (confirm("Bạn có chắc chắn muốn từ chối / hủy đơn hàng này?")) {
    const ord = orders.value.find(o => o.id === orderId);
    if (ord) {
      ord.status = "cancelled";
      triggerToast(`Đã hủy đơn hàng #${ord.orderCode}.`);
    }
  }
};

// Quản lý sản phẩm của gian hàng
interface SellerProduct {
  id: string;
  name: string;
  category: string;
  price: number;
  unit: string;
  stock: number;
  image: string;
  isAvailable: boolean;
}

const products = ref<SellerProduct[]>([
  {
    id: "p-01",
    name: "Rau muống hữu cơ Ba Vì",
    category: "Rau củ quả",
    price: 18000,
    unit: "Bó 500g",
    stock: 45,
    image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  },
  {
    id: "p-02",
    name: "Cà chua bi Đà Lạt mọng nước",
    category: "Rau củ quả",
    price: 35000,
    unit: "Hộp 500g",
    stock: 28,
    image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  },
  {
    id: "p-03",
    name: "Thịt ba chỉ heo sạch chuẩn CP",
    category: "Thịt cá tươi",
    price: 65000,
    unit: "Khay 500g",
    stock: 12,
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  },
  {
    id: "p-04",
    name: "Trứng gà ta thảo mộc thiên nhiên",
    category: "Thực phẩm bổ dưỡng",
    price: 45000,
    unit: "Vỉ 10 quả",
    stock: 50,
    image: "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  },
  {
    id: "p-05",
    name: "Xà lách mỡ thủy canh tươi giòn",
    category: "Rau củ quả",
    price: 22000,
    unit: "Túi 400g",
    stock: 5,
    image: "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  },
  {
    id: "p-06",
    name: "Cam sành Hàm Yên mọng nước",
    category: "Trái cây tươi",
    price: 42000,
    unit: "1 kg",
    stock: 0,
    image: "https://images.unsplash.com/photo-1582979512210-99b6a53386f9?auto=format&fit=crop&w=400&q=80",
    isAvailable: false
  }
]);

const productSearch = ref("");
const filteredProducts = computed(() => {
  if (!productSearch.value.trim()) return products.value;
  const q = productSearch.value.toLowerCase().trim();
  return products.value.filter(p => p.name.toLowerCase().includes(q) || p.category.toLowerCase().includes(q));
});

const toggleProductAvailability = (prodId: string) => {
  const p = products.value.find(item => item.id === prodId);
  if (p) {
    p.isAvailable = !p.isAvailable;
    triggerToast(
      p.isAvailable
        ? `Đã bật hiển thị bán món: "${p.name}".`
        : `Đã tạm ẩn món: "${p.name}" (Hết hàng).`
    );
  }
};

const handleDeleteProduct = (prodId: string) => {
  const p = products.value.find(item => item.id === prodId);
  if (p && confirm(`Bạn có chắc muốn xóa sản phẩm "${p.name}"?`)) {
    products.value = products.value.filter(item => item.id !== prodId);
    triggerToast(`Đã xóa sản phẩm "${p.name}".`);
  }
};

// Modal Thêm Món Mới
const isAddModalOpen = ref(false);
const newProduct = reactive({
  name: "",
  category: "Rau củ quả",
  price: 25000,
  unit: "Kg",
  stock: 20,
  image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80"
});

const handleCreateProduct = () => {
  if (!newProduct.name.trim()) {
    alert("Vui lòng nhập tên sản phẩm!");
    return;
  }
  if (newProduct.price <= 0) {
    alert("Vui lòng nhập giá hợp lệ!");
    return;
  }

  products.value.unshift({
    id: `p-${Date.now()}`,
    name: newProduct.name.trim(),
    category: newProduct.category,
    price: Number(newProduct.price),
    unit: newProduct.unit,
    stock: Number(newProduct.stock),
    image: newProduct.image || "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  });

  isAddModalOpen.value = false;
  // Reset form
  newProduct.name = "";
  newProduct.price = 25000;
  newProduct.stock = 20;

  triggerToast("Đã thêm sản phẩm mới thành công lên gian hàng!");
};

// Lưu cài đặt gian hàng
const handleSaveSettings = () => {
  triggerToast("Đã lưu thông tin cài đặt gian hàng thành công!");
};
</script>

<template>
  <div class="seller-dashboard-container">
    <!-- Toast thông báo nổi -->
    <transition name="toast-fade">
      <div v-if="showToast" class="toast-popup" role="alert">
        <i class="bi bi-check-circle-fill me-2 text-success"></i>
        <span>{{ toastMessage }}</span>
      </div>
    </transition>

    <!-- 1. BANNER CHUYỂN NHANH CHẾ ĐỘ MUA HÀNG DÀNH CHO SELLER (ĐÁP ỨNG YÊU CẦU NGƯỜI BÁN VẪN MUA ĐƯỢC HÀNG) -->
    <div class="buyer-mode-banner">
      <div class="bmb-left">
        <div class="bmb-icon-box">
          <i class="bi bi-bag-heart-fill"></i>
        </div>
        <div class="bmb-text">
          <div class="bmb-title-row">
            <h3 class="bmb-title">Kênh Bán Hàng ZoneMart • Bác Ba</h3>
            <span class="role-pill"><i class="bi bi-shop me-1"></i> Đối Tác Bán Hàng</span>
          </div>
          <p class="bmb-sub">
            Là Người Bán, bạn hoàn toàn có thể mua hàng từ các gian hàng khác với giao hỏa tốc 10km như khách hàng bình thường!
          </p>
        </div>
      </div>
      <div class="bmb-actions">
        <router-link to="/products" class="btn btn-shop-action">
          <i class="bi bi-basket2-fill me-1"></i> Khám Phá Chợ Mua Sắm
        </router-link>
        <router-link to="/cart" class="btn btn-cart-action">
          <i class="bi bi-cart3 me-1"></i> Giỏ Hàng
        </router-link>
        <router-link to="/buyer-orders" class="btn btn-my-orders-action">
          <i class="bi bi-bag-check me-1"></i> Đơn Đã Mua
        </router-link>
      </div>
    </div>

    <!-- 2. STORE HEADER & LIVE TOGGLE BAR -->
    <div class="store-overview-card">
      <div class="store-brand-left">
        <div class="store-avatar-box">
          <i class="bi bi-shop-window"></i>
        </div>
        <div class="store-meta-info">
          <div class="store-title-row">
            <h2>{{ storeInfo.name }}</h2>
            <span class="badge-partner-id">Mã: {{ storeInfo.id }}</span>
            <span class="badge-rating"><i class="bi bi-star-fill text-warning me-1"></i>{{ storeInfo.rating }} ({{ storeInfo.reviewsCount }} đánh giá)</span>
          </div>
          <p class="store-address-text">
            <i class="bi bi-geo-alt-fill text-danger me-1"></i> {{ storeInfo.address }}
          </p>
          <div class="store-badges-row">
            <span class="s-tag"><i class="bi bi-clock-fill me-1"></i> Giờ mở cửa: {{ storeInfo.openHours }}</span>
            <span class="s-tag"><i class="bi bi-lightning-charge-fill text-warning me-1"></i> Giao hỏa tốc bán kính ≤ {{ storeInfo.radiusKm }}km</span>
            <span class="s-tag"><i class="bi bi-telephone-fill me-1"></i> Hotline: {{ storeInfo.phone }}</span>
          </div>
        </div>
      </div>

      <!-- Toggle trạng thái mở cửa gian hàng -->
      <div class="store-status-toggle">
        <div class="status-indicator" :class="{ 'is-open': isStoreOpen }">
          <span class="pulse-dot"></span>
          <strong>{{ isStoreOpen ? "Đang Mở Cửa Nhận Đơn" : "Tạm Đóng Cửa" }}</strong>
        </div>
        <button 
          class="btn btn-toggle-state" 
          :class="isStoreOpen ? 'btn-danger-outline' : 'btn-success-outline'" 
          @click="toggleStoreOpen"
        >
          <i :class="isStoreOpen ? 'bi bi-pause-circle-fill me-1' : 'bi bi-play-circle-fill me-1'"></i>
          {{ isStoreOpen ? "Tạm Dừng Nhận Đơn" : "Bật Nhận Đơn Ngay" }}
        </button>
      </div>
    </div>

    <!-- 3. KPI STATS METRIC CARDS -->
    <div class="metrics-grid">
      <div class="metric-card">
        <div class="metric-icon-box bg-emerald">
          <i class="bi bi-cash-stack"></i>
        </div>
        <div class="metric-body">
          <span class="metric-label">Doanh Thu Hôm Nay</span>
          <h3 class="metric-value">{{ storeInfo.todayRevenue.toLocaleString("vi-VN") }} ₫</h3>
          <span class="metric-trend text-success"><i class="bi bi-arrow-up-right me-1"></i>+14.2% so với hôm qua</span>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon-box bg-orange">
          <i class="bi bi-box-seam-fill"></i>
        </div>
        <div class="metric-body">
          <span class="metric-label">Đơn Hàng Hôm Nay</span>
          <h3 class="metric-value">{{ orders.length }} Đơn</h3>
          <span class="metric-trend text-warning" v-if="pendingCount > 0">
            <i class="bi bi-exclamation-circle-fill me-1"></i>{{ pendingCount }} đơn cần chuẩn bị
          </span>
          <span class="metric-trend text-muted" v-else>Đã xử lý tất cả</span>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon-box bg-blue">
          <i class="bi bi-grid-3x3-gap-fill"></i>
        </div>
        <div class="metric-body">
          <span class="metric-label">Sản Phẩm Đang Bán</span>
          <h3 class="metric-value">{{ products.length }} Món</h3>
          <span class="metric-trend text-primary"><i class="bi bi-check2-all me-1"></i>Sẵn sàng giao 10km</span>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon-box bg-purple">
          <i class="bi bi-star-fill"></i>
        </div>
        <div class="metric-body">
          <span class="metric-label">Uy Tín Gian Hàng</span>
          <h3 class="metric-value">4.9 / 5.0</h3>
          <span class="metric-trend text-success"><i class="bi bi-patch-check-fill me-1"></i>Đối tác chuẩn VietGAP</span>
        </div>
      </div>
    </div>

    <!-- 4. DASHBOARD TABS NAVIGATION -->
    <div class="dashboard-tabs-bar">
      <button 
        class="tab-btn" 
        :class="{ active: activeTab === 'orders' }" 
        @click="activeTab = 'orders'"
      >
        <i class="bi bi-receipt-cutoff me-2"></i>
        <span>Quản Lý Đơn Hàng</span>
        <span v-if="pendingCount > 0" class="tab-badge">{{ pendingCount }}</span>
      </button>

      <button 
        class="tab-btn" 
        :class="{ active: activeTab === 'products' }" 
        @click="activeTab = 'products'"
      >
        <i class="bi bi-egg-fried me-2"></i>
        <span>Sản Phẩm & Tồn Kho</span>
        <span class="tab-count">({{ products.length }})</span>
      </button>

      <button 
        class="tab-btn" 
        :class="{ active: activeTab === 'settings' }" 
        @click="activeTab = 'settings'"
      >
        <i class="bi bi-sliders me-2"></i>
        <span>Cài Đặt Gian Hàng & 10km</span>
      </button>
    </div>

    <!-- TAB 1: QUẢN LÝ ĐƠN HÀNG -->
    <div v-if="activeTab === 'orders'" class="tab-content-wrapper">
      <div class="orders-filter-bar">
        <div class="filter-pills">
          <button 
            class="pill-btn" 
            :class="{ active: orderFilter === 'all' }" 
            @click="orderFilter = 'all'"
          >
            Tất cả ({{ orders.length }})
          </button>
          <button 
            class="pill-btn" 
            :class="{ active: orderFilter === 'pending' }" 
            @click="orderFilter = 'pending'"
          >
            Chờ xác nhận ({{ pendingCount }})
          </button>
          <button 
            class="pill-btn" 
            :class="{ active: orderFilter === 'preparing' }" 
            @click="orderFilter = 'preparing'"
          >
            Đang chuẩn bị ({{ preparingCount }})
          </button>
          <button 
            class="pill-btn" 
            :class="{ active: orderFilter === 'delivering' }" 
            @click="orderFilter = 'delivering'"
          >
            Đang giao hỏa tốc
          </button>
          <button 
            class="pill-btn" 
            :class="{ active: orderFilter === 'completed' }" 
            @click="orderFilter = 'completed'"
          >
            Đã hoàn thành
          </button>
        </div>
      </div>

      <!-- Danh sách thẻ đơn hàng -->
      <div v-if="filteredOrders.length > 0" class="orders-list">
        <div v-for="ord in filteredOrders" :key="ord.id" class="order-card" :class="'border-' + ord.status">
          <div class="order-card-header">
            <div class="order-id-group">
              <span class="order-code">#{{ ord.orderCode }}</span>
              <span class="order-time"><i class="bi bi-clock me-1"></i>{{ ord.timeAgo }}</span>
              <span class="delivery-badge" :class="ord.deliveryType">
                <i class="bi bi-lightning-charge-fill me-1"></i> Hỏa tốc {{ ord.distanceKm }}km
              </span>
            </div>
            <div class="order-status-badge" :class="ord.status">
              <span v-if="ord.status === 'pending'">Chờ xác nhận</span>
              <span v-else-if="ord.status === 'preparing'">Đang đóng gói</span>
              <span v-else-if="ord.status === 'delivering'">Shipper đang giao</span>
              <span v-else-if="ord.status === 'completed'">Giao thành công</span>
              <span v-else-if="ord.status === 'cancelled'">Đã hủy</span>
            </div>
          </div>

          <!-- Thông tin khách hàng & địa chỉ -->
          <div class="order-customer-box">
            <div class="customer-info-line">
              <strong><i class="bi bi-person-fill text-primary me-1"></i> {{ ord.customerName }}</strong>
              <a :href="'tel:' + ord.customerPhone" class="customer-phone-link">
                <i class="bi bi-telephone-fill me-1"></i> {{ ord.customerPhone }}
              </a>
            </div>
            <p class="customer-addr">
              <i class="bi bi-geo-alt-fill text-danger me-1"></i> {{ ord.customerAddress }}
            </p>
          </div>

          <!-- Danh sách món đặt -->
          <div class="order-items-list">
            <div v-for="(it, idx) in ord.items" :key="idx" class="order-item-row">
              <span class="item-name"><i class="bi bi-check2 text-success me-1"></i> {{ it.name }}</span>
              <span class="item-qty">x{{ it.qty }}</span>
              <span class="item-price">{{ (it.price * it.qty).toLocaleString("vi-VN") }} ₫</span>
            </div>
          </div>

          <!-- Shipper phụ trách nếu có -->
          <div v-if="ord.shipperInfo" class="shipper-assigned-bar">
            <i class="bi bi-bicycle text-success me-2 fs-5"></i>
            <div>
              <span>Tài xế nhận đơn: <strong>{{ ord.shipperInfo.name }}</strong> ({{ ord.shipperInfo.licensePlate }})</span>
              <a :href="'tel:' + ord.shipperInfo.phone" class="shipper-tel">
                <i class="bi bi-telephone-outbound-fill me-1"></i> {{ ord.shipperInfo.phone }}
              </a>
            </div>
          </div>

          <!-- Tổng kết & Nút thao tác -->
          <div class="order-card-footer">
            <div class="order-pay-info">
              <span class="pay-method">
                <i :class="ord.paymentMethod === 'ONLINE_QR' ? 'bi bi-qr-code text-primary' : 'bi bi-cash-coin text-warning'"></i>
                {{ ord.paymentMethod === 'ONLINE_QR' ? 'Đã thanh toán Online QR' : 'Thu tiền mặt COD' }}
              </span>
              <div class="total-wrap">
                <span class="total-label">Tổng tiền hàng:</span>
                <strong class="total-val">{{ ord.totalAmount.toLocaleString("vi-VN") }} ₫</strong>
              </div>
            </div>

            <div class="order-action-btns">
              <!-- Nút khi đơn mới pending -->
              <template v-if="ord.status === 'pending'">
                <button class="btn btn-sm btn-danger" @click="handleCancelOrder(ord.id)">
                  <i class="bi bi-x-circle me-1"></i> Từ Chối
                </button>
                <button class="btn btn-sm btn-primary-warm" @click="handleAcceptOrder(ord.id)">
                  <i class="bi bi-box-seam me-1"></i> Xác Nhận & Gọi Shipper
                </button>
              </template>

              <!-- Nút khi đang chuẩn bị -->
              <template v-else-if="ord.status === 'preparing'">
                <button class="btn btn-sm btn-success" @click="handleHandoverShipper(ord.id)">
                  <i class="bi bi-bicycle me-1"></i> Bàn Giao Cho Shipper
                </button>
              </template>

              <!-- Khi đang giao -->
              <template v-else-if="ord.status === 'delivering'">
                <span class="text-delivering">
                  <i class="bi bi-clock-history me-1"></i> Shipper đang giao tới khách...
                </span>
              </template>

              <!-- Khi đã hoàn thành -->
              <template v-else-if="ord.status === 'completed'">
                <span class="text-completed">
                  <i class="bi bi-check-circle-fill text-success me-1"></i> Đơn hoàn tất • Đã cộng tiền vào ví
                </span>
              </template>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="empty-state-box">
        <i class="bi bi-inbox fs-1 text-muted mb-2"></i>
        <h4>Không có đơn hàng nào</h4>
        <p>Hiện không có đơn hàng nào trong mục này. Khi khách đặt món trong bán kính 10km, đơn sẽ xuất hiện ngay lập tức.</p>
      </div>
    </div>

    <!-- TAB 2: QUẢN LÝ SẢN PHẨM & TỒN KHO -->
    <div v-if="activeTab === 'products'" class="tab-content-wrapper">
      <div class="products-top-toolbar">
        <div class="search-input-box">
          <i class="bi bi-search"></i>
          <input 
            v-model="productSearch" 
            type="text" 
            placeholder="Tìm theo tên sản phẩm, danh mục..." 
          />
        </div>

        <button class="btn btn-primary-warm" @click="isAddModalOpen = true">
          <i class="bi bi-plus-lg me-1"></i> Thêm Sản Phẩm Mới
        </button>
      </div>

      <!-- Bảng danh sách sản phẩm -->
      <div class="products-table-card">
        <table class="seller-table">
          <thead>
            <tr>
              <th>Hình ảnh</th>
              <th>Tên sản phẩm</th>
              <th>Danh mục</th>
              <th>Đơn giá</th>
              <th>Đơn vị</th>
              <th>Tồn kho</th>
              <th>Trạng thái bán</th>
              <th>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="prod in filteredProducts" :key="prod.id">
              <td>
                <img :src="prod.image" :alt="prod.name" class="prod-thumb" />
              </td>
              <td>
                <strong class="prod-title">{{ prod.name }}</strong>
              </td>
              <td>
                <span class="cat-pill">{{ prod.category }}</span>
              </td>
              <td>
                <strong class="price-text">{{ prod.price.toLocaleString("vi-VN") }} ₫</strong>
              </td>
              <td>
                <span class="text-muted">{{ prod.unit }}</span>
              </td>
              <td>
                <span class="stock-pill" :class="{ 'low-stock': prod.stock <= 5 }">
                  {{ prod.stock }} {{ prod.unit }}
                </span>
              </td>
              <td>
                <button 
                  class="status-toggle-badge" 
                  :class="prod.isAvailable ? 'available' : 'unavailable'"
                  @click="toggleProductAvailability(prod.id)"
                  :title="prod.isAvailable ? 'Bấm để chuyển sang Hết hàng' : 'Bấm để bật Còn hàng'"
                >
                  <i :class="prod.isAvailable ? 'bi bi-check-circle-fill me-1' : 'bi bi-dash-circle-fill me-1'"></i>
                  {{ prod.isAvailable ? 'Đang Bán' : 'Tạm Ẩn' }}
                </button>
              </td>
              <td>
                <div class="action-icons-wrap">
                  <button 
                    class="btn-icon-action text-danger" 
                    title="Xóa sản phẩm"
                    @click="handleDeleteProduct(prod.id)"
                  >
                    <i class="bi bi-trash3-fill"></i>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- TAB 3: CÀI ĐẶT GIAN HÀNG & 10KM -->
    <div v-if="activeTab === 'settings'" class="tab-content-wrapper">
      <div class="settings-grid">
        <!-- Thông tin cơ bản -->
        <div class="settings-card">
          <h3 class="card-heading"><i class="bi bi-shop me-2 text-primary"></i> Thông Tin Gian Hàng</h3>
          <div class="form-group">
            <label>Tên Gian Hàng / Cửa Hàng:</label>
            <input v-model="storeInfo.name" type="text" class="form-control" />
          </div>

          <div class="form-group">
            <label>Địa Chỉ Nhà Vườn / Gian Hàng (Định Vị GPS):</label>
            <input v-model="storeInfo.address" type="text" class="form-control" />
          </div>

          <div class="form-row-2">
            <div class="form-group">
              <label>Số Điện Thoại Quán:</label>
              <input v-model="storeInfo.phone" type="text" class="form-control" />
            </div>
            <div class="form-group">
              <label>Khung Giờ Mở Cửa:</label>
              <input v-model="storeInfo.openHours" type="text" class="form-control" />
            </div>
          </div>

          <div class="form-group">
            <label class="d-flex justify-content-between">
              <span>Bán Kính Giao Hàng Hỏa Tốc:</span>
              <strong class="text-primary">{{ storeInfo.radiusKm }} km (Tối đa 10km)</strong>
            </label>
            <input 
              v-model.number="storeInfo.radiusKm" 
              type="range" 
              min="1" 
              max="10" 
              step="0.5" 
              class="radius-range-slider" 
            />
            <small class="text-muted">Shipper ZoneMart sẽ chỉ nhận đơn từ khách hàng trong bán kính này để đảm bảo giao tươi trong 30-45 phút.</small>
          </div>
        </div>

        <!-- Tài khoản thanh toán nhận tiền -->
        <div class="settings-card">
          <h3 class="card-heading"><i class="bi bi-bank me-2 text-success"></i> Tài Khoản Nhận Doanh Thu VietQR</h3>
          
          <div class="form-group">
            <label>Ngân Hàng Nhận Tiền:</label>
            <input v-model="storeInfo.bankName" type="text" class="form-control" />
          </div>

          <div class="form-group">
            <label>Số Tài Khoản Ngân Hàng:</label>
            <input v-model="storeInfo.bankAccount" type="text" class="form-control" />
          </div>

          <div class="form-group">
            <label>Chủ Tài Khoản (In hoa không dấu):</label>
            <input v-model="storeInfo.accountHolder" type="text" class="form-control" />
          </div>

          <div class="vietqr-preview-box">
            <p class="qr-preview-desc">Mã QR nhận chuyển khoản trực tiếp từ khách hàng:</p>
            <img 
              src="https://api.qrserver.com/v1/create-qr-code/?size=140x140&data=ZONEMART_SELLER_PAYOUT" 
              alt="VietQR Payout" 
              class="qr-img" 
            />
            <span class="qr-sub">{{ storeInfo.bankName }} • {{ storeInfo.bankAccount }}</span>
          </div>

          <button class="btn btn-primary-warm w-100 mt-3" @click="handleSaveSettings">
            <i class="bi bi-check2-circle me-1"></i> Lưu Cấu Hình Gian Hàng
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL THÊM SẢN PHẨM MỚI -->
    <div v-if="isAddModalOpen" class="modal-backdrop" @click="isAddModalOpen = false">
      <div class="modal-dialog-box" @click.stop>
        <div class="modal-header">
          <h3><i class="bi bi-plus-circle-fill text-primary me-2"></i> Thêm Sản Phẩm Mới</h3>
          <button class="modal-close-btn" @click="isAddModalOpen = false">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <form @submit.prevent="handleCreateProduct" class="modal-body">
          <div class="form-group">
            <label>Tên nông sản / món hàng (*):</label>
            <input 
              v-model="newProduct.name" 
              type="text" 
              class="form-control" 
              placeholder="VD: Rau xà lách thủy canh, Cà chua bi..." 
              required 
            />
          </div>

          <div class="form-row-2">
            <div class="form-group">
              <label>Danh mục:</label>
              <select v-model="newProduct.category" class="form-control">
                <option value="Rau củ quả">Rau củ quả</option>
                <option value="Trái cây tươi">Trái cây tươi</option>
                <option value="Thịt cá tươi">Thịt cá tươi sống</option>
                <option value="Thực phẩm bổ dưỡng">Thực phẩm dinh dưỡng</option>
                <option value="Gia vị & Đồ khô">Gia vị & Đồ khô</option>
              </select>
            </div>

            <div class="form-group">
              <label>Đơn vị tính:</label>
              <input 
                v-model="newProduct.unit" 
                type="text" 
                class="form-control" 
                placeholder="VD: Kg, Bó 500g, Hộp..." 
                required 
              />
            </div>
          </div>

          <div class="form-row-2">
            <div class="form-group">
              <label>Đơn giá bán (₫) (*):</label>
              <input 
                v-model.number="newProduct.price" 
                type="number" 
                min="1000" 
                step="1000" 
                class="form-control" 
                required 
              />
            </div>

            <div class="form-group">
              <label>Số lượng tồn kho ban đầu:</label>
              <input 
                v-model.number="newProduct.stock" 
                type="number" 
                min="0" 
                class="form-control" 
                required 
              />
            </div>
          </div>

          <div class="form-group">
            <label>URL Hình ảnh sản phẩm:</label>
            <input 
              v-model="newProduct.image" 
              type="url" 
              class="form-control" 
              placeholder="https://..." 
            />
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="isAddModalOpen = false">
              Hủy
            </button>
            <button type="submit" class="btn btn-primary-warm">
              <i class="bi bi-cloud-arrow-up-fill me-1"></i> Đăng Bán Sản Phẩm
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.seller-dashboard-container {
  max-width: 1200px;
  margin: 20px auto 60px auto;
  padding: 0 16px;
  font-family: inherit;
  color: #2b1b14;
}

/* Toast Popup */
.toast-popup {
  position: fixed;
  top: 80px;
  right: 24px;
  background: #ffffff;
  border: 1px solid #10b981;
  border-left: 5px solid #10b981;
  padding: 12px 20px;
  border-radius: 8px;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.15);
  z-index: 9999;
  display: flex;
  align-items: center;
  font-size: 14px;
  font-weight: 600;
  color: #0f172a;
}
.toast-fade-enter-active,
.toast-fade-leave-active {
  transition: all 0.3s ease;
}
.toast-fade-enter-from,
.toast-fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

/* 1. BUYER MODE BANNER */
.buyer-mode-banner {
  background: linear-gradient(135deg, #fff5eb 0%, #ffedd5 100%);
  border: 1.5px solid #fdba74;
  border-radius: 16px;
  padding: 16px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.06);
}
@media (max-width: 850px) {
  .buyer-mode-banner {
    flex-direction: column;
    align-items: flex-start;
  }
}
.bmb-left {
  display: flex;
  align-items: center;
  gap: 14px;
}
.bmb-icon-box {
  width: 44px;
  height: 44px;
  background: #ea580c;
  color: #ffffff;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  flex-shrink: 0;
}
.bmb-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.bmb-title {
  font-size: 16px;
  font-weight: 800;
  color: #9a3412;
  margin: 0;
}
.role-pill {
  background: #fed7aa;
  color: #7c2d12;
  font-size: 11.5px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 12px;
}
.bmb-sub {
  font-size: 13px;
  color: #7c2d12;
  margin: 3px 0 0 0;
}
.bmb-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  flex-shrink: 0;
}
.btn-shop-action {
  background: #ea580c;
  color: #ffffff;
  text-decoration: none;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 14px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s;
}
.btn-shop-action:hover {
  background: #c2410c;
  transform: translateY(-1px);
}
.btn-cart-action,
.btn-my-orders-action {
  background: #ffffff;
  color: #9a3412;
  border: 1px solid #fdba74;
  text-decoration: none;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 12px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s;
}
.btn-cart-action:hover,
.btn-my-orders-action:hover {
  background: #fffaf0;
  border-color: #ea580c;
}

/* 2. STORE OVERVIEW CARD */
.store-overview-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 20px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 20px;
  margin-bottom: 20px;
}
@media (max-width: 850px) {
  .store-overview-card {
    flex-direction: column;
    align-items: flex-start;
  }
}
.store-brand-left {
  display: flex;
  align-items: center;
  gap: 16px;
}
.store-avatar-box {
  width: 56px;
  height: 56px;
  background: #fff7ed;
  border: 1.5px solid #fed7aa;
  color: #ea580c;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  flex-shrink: 0;
}
.store-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-bottom: 4px;
}
.store-title-row h2 {
  font-size: 19px;
  font-weight: 850;
  color: #0f172a;
  margin: 0;
}
.badge-partner-id {
  background: #f1f5f9;
  color: #475569;
  font-size: 12px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 6px;
}
.badge-rating {
  font-size: 12.5px;
  font-weight: 700;
  color: #475569;
}
.store-address-text {
  font-size: 13px;
  color: #64748b;
  margin: 0 0 6px 0;
}
.store-badges-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
.s-tag {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #475569;
  font-size: 12px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 6px;
}

/* Status toggle */
.store-status-toggle {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 8px;
}
@media (max-width: 850px) {
  .store-status-toggle {
    align-items: flex-start;
  }
}
.status-indicator {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  font-weight: 700;
  color: #64748b;
}
.status-indicator.is-open {
  color: #16a34a;
}
.pulse-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #94a3b8;
}
.status-indicator.is-open .pulse-dot {
  background: #16a34a;
  box-shadow: 0 0 0 3px rgba(22, 163, 74, 0.2);
}
.btn-toggle-state {
  border: 1.5px solid;
  background: transparent;
  font-size: 12.5px;
  font-weight: 700;
  padding: 6px 14px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-danger-outline {
  color: #dc2626;
  border-color: #fca5a5;
}
.btn-danger-outline:hover {
  background: #fef2f2;
}
.btn-success-outline {
  color: #16a34a;
  border-color: #86efac;
}
.btn-success-outline:hover {
  background: #f0fdf4;
}

/* 3. METRICS GRID */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}
@media (max-width: 992px) {
  .metrics-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
@media (max-width: 540px) {
  .metrics-grid {
    grid-template-columns: 1fr;
  }
}
.metric-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 14px;
}
.metric-icon-box {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  color: #ffffff;
  flex-shrink: 0;
}
.bg-emerald { background: #10b981; }
.bg-orange { background: #f97316; }
.bg-blue { background: #2563eb; }
.bg-purple { background: #8b5cf6; }

.metric-body {
  display: flex;
  flex-direction: column;
}
.metric-label {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 2px;
}
.metric-value {
  font-size: 18px;
  font-weight: 850;
  color: #0f172a;
  margin: 0 0 2px 0;
}
.metric-trend {
  font-size: 11px;
  font-weight: 700;
}

/* 4. DASHBOARD TABS BAR */
.dashboard-tabs-bar {
  display: flex;
  gap: 8px;
  border-bottom: 2px solid #e2e8f0;
  margin-bottom: 20px;
  overflow-x: auto;
}
.tab-btn {
  background: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  margin-bottom: -2px;
  padding: 12px 18px;
  font-size: 14px;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  white-space: nowrap;
  transition: all 0.2s;
}
.tab-btn:hover {
  color: #ea580c;
}
.tab-btn.active {
  color: #ea580c;
  border-bottom-color: #ea580c;
}
.tab-badge {
  background: #ef4444;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 1px 7px;
  border-radius: 12px;
  margin-left: 6px;
}
.tab-count {
  font-size: 12px;
  color: #94a3b8;
  margin-left: 4px;
}

/* ORDERS FILTER */
.orders-filter-bar {
  margin-bottom: 16px;
}
.filter-pills {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
.pill-btn {
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
  padding: 6px 14px;
  border-radius: 20px;
  font-size: 12.5px;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s;
}
.pill-btn:hover {
  background: #e2e8f0;
}
.pill-btn.active {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
}

/* ORDER CARDS */
.orders-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.order-card {
  background: #ffffff;
  border: 1.5px solid #e2e8f0;
  border-radius: 14px;
  padding: 18px 20px;
  transition: all 0.2s;
}
.order-card:hover {
  border-color: #cbd5e1;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.04);
}
.order-card.border-pending {
  border-left: 4px solid #f59e0b;
}
.order-card.border-preparing {
  border-left: 4px solid #3b82f6;
}
.order-card.border-delivering {
  border-left: 4px solid #10b981;
}

.order-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.order-id-group {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.order-code {
  font-size: 15px;
  font-weight: 850;
  color: #0f172a;
}
.order-time {
  font-size: 12px;
  color: #64748b;
}
.delivery-badge {
  font-size: 11px;
  font-weight: 800;
  padding: 2px 8px;
  border-radius: 6px;
}
.delivery-badge.express {
  background: #fef3c7;
  color: #b45309;
}
.delivery-badge.standard {
  background: #f1f5f9;
  color: #475569;
}

.order-status-badge {
  font-size: 12px;
  font-weight: 800;
  padding: 3px 10px;
  border-radius: 20px;
}
.order-status-badge.pending { background: #fef3c7; color: #b45309; }
.order-status-badge.preparing { background: #dbeafe; color: #1d4ed8; }
.order-status-badge.delivering { background: #dcfce7; color: #15803d; }
.order-status-badge.completed { background: #f1f5f9; color: #475569; }
.order-status-badge.cancelled { background: #fee2e2; color: #b91c1c; }

.order-customer-box {
  background: #f8fafc;
  border-radius: 10px;
  padding: 10px 14px;
  margin-bottom: 12px;
}
.customer-info-line {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
  font-size: 13px;
}
.customer-phone-link {
  color: #2563eb;
  text-decoration: none;
  font-weight: 700;
}
.customer-addr {
  font-size: 12.5px;
  color: #475569;
  margin: 0;
}

.order-items-list {
  border-top: 1px dashed #e2e8f0;
  border-bottom: 1px dashed #e2e8f0;
  padding: 10px 0;
  margin-bottom: 12px;
}
.order-item-row {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  padding: 3px 0;
}
.item-name {
  color: #1e293b;
  font-weight: 600;
}
.item-qty {
  color: #64748b;
  font-weight: 700;
}
.item-price {
  color: #0f172a;
  font-weight: 700;
}

.shipper-assigned-bar {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  border-radius: 8px;
  padding: 8px 12px;
  display: flex;
  align-items: center;
  margin-bottom: 12px;
  font-size: 12.5px;
  color: #166534;
}
.shipper-tel {
  margin-left: 10px;
  color: #15803d;
  font-weight: 700;
  text-decoration: none;
}

.order-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}
.order-pay-info {
  display: flex;
  align-items: center;
  gap: 16px;
}
.pay-method {
  font-size: 12px;
  color: #64748b;
  font-weight: 600;
}
.total-wrap {
  display: flex;
  align-items: baseline;
  gap: 6px;
}
.total-label {
  font-size: 12px;
  color: #64748b;
}
.total-val {
  font-size: 16px;
  font-weight: 850;
  color: #ea580c;
}

.order-action-btns {
  display: flex;
  gap: 8px;
  align-items: center;
}
.btn-primary-warm {
  background: #ea580c;
  color: #ffffff;
  border: none;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-primary-warm:hover {
  background: #c2410c;
}
.text-delivering {
  font-size: 12.5px;
  font-weight: 700;
  color: #16a34a;
}
.text-completed {
  font-size: 12.5px;
  font-weight: 700;
  color: #475569;
}

/* EMPTY STATE */
.empty-state-box {
  background: #ffffff;
  border: 1px dashed #cbd5e1;
  border-radius: 14px;
  padding: 48px 24px;
  text-align: center;
  color: #64748b;
}
.empty-state-box h4 {
  color: #0f172a;
  margin: 6px 0;
}
.empty-state-box p {
  font-size: 13.5px;
  max-width: 420px;
  margin: 0 auto;
}

/* TAB 2: PRODUCTS TOOLBAR */
.products-top-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}
.search-input-box {
  display: flex;
  align-items: center;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 8px 14px;
  min-width: 280px;
}
.search-input-box i {
  color: #94a3b8;
  margin-right: 8px;
}
.search-input-box input {
  border: none;
  outline: none;
  width: 100%;
  font-size: 13px;
}

.products-table-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  overflow-x: auto;
}
.seller-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}
.seller-table th {
  background: #f8fafc;
  color: #475569;
  font-weight: 700;
  text-align: left;
  padding: 12px 16px;
  border-bottom: 1px solid #e2e8f0;
  white-space: nowrap;
}
.seller-table td {
  padding: 12px 16px;
  border-bottom: 1px solid #f1f5f9;
  vertical-align: middle;
}
.prod-thumb {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  object-fit: cover;
}
.prod-title {
  color: #0f172a;
  font-weight: 700;
}
.cat-pill {
  background: #f1f5f9;
  color: #475569;
  font-size: 11.5px;
  font-weight: 600;
  padding: 3px 8px;
  border-radius: 6px;
}
.price-text {
  color: #ea580c;
  font-weight: 800;
}
.stock-pill {
  font-weight: 700;
  color: #16a34a;
}
.stock-pill.low-stock {
  color: #dc2626;
}

.status-toggle-badge {
  border: none;
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}
.status-toggle-badge.available {
  background: #dcfce7;
  color: #15803d;
}
.status-toggle-badge.unavailable {
  background: #fee2e2;
  color: #b91c1c;
}
.btn-icon-action {
  background: transparent;
  border: none;
  cursor: pointer;
  font-size: 15px;
  padding: 6px;
  border-radius: 6px;
  transition: background 0.2s;
}
.btn-icon-action:hover {
  background: #fee2e2;
}

/* TAB 3: SETTINGS */
.settings-grid {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 20px;
}
@media (max-width: 850px) {
  .settings-grid {
    grid-template-columns: 1fr;
  }
}
.settings-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 24px;
}
.card-heading {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 18px 0;
  padding-bottom: 10px;
  border-bottom: 1px solid #f1f5f9;
}
.form-group {
  margin-bottom: 16px;
}
.form-group label {
  display: block;
  font-size: 12.5px;
  font-weight: 700;
  color: #334155;
  margin-bottom: 6px;
}
.form-control {
  width: 100%;
  padding: 9px 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 13.5px;
  box-sizing: border-box;
}
.form-control:focus {
  outline: none;
  border-color: #ea580c;
}
.form-row-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
.radius-range-slider {
  width: 100%;
  accent-color: #ea580c;
}

.vietqr-preview-box {
  background: #f8fafc;
  border: 1px dashed #cbd5e1;
  border-radius: 10px;
  padding: 16px;
  text-align: center;
  margin-top: 14px;
}
.qr-preview-desc {
  font-size: 12px;
  color: #64748b;
  margin: 0 0 10px 0;
}
.qr-img {
  width: 130px;
  height: 130px;
  border-radius: 8px;
  background: #ffffff;
  padding: 6px;
  border: 1px solid #e2e8f0;
}
.qr-sub {
  display: block;
  font-size: 12px;
  font-weight: 700;
  color: #0f172a;
  margin-top: 8px;
}

/* MODAL */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 16px;
}
.modal-dialog-box {
  background: #ffffff;
  width: 100%;
  max-width: 520px;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.2);
}
.modal-header {
  padding: 16px 20px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.modal-header h3 {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}
.modal-close-btn {
  background: transparent;
  border: none;
  font-size: 16px;
  cursor: pointer;
  color: #64748b;
}
.modal-body {
  padding: 20px;
}
.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid #f1f5f9;
}
.btn-secondary {
  background: #f1f5f9;
  color: #475569;
  border: 1px solid #cbd5e1;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
}
</style>
