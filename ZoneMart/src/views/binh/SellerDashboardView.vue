<script setup lang="ts">
/**
 * ================================================================
 * KÊNH QUẢN LÝ NGƯỜI BÁN ZONEMART (SELLER DASHBOARD)
 * Thiết kế giao diện hiện đại chuẩn Dashboard theo mẫu tham chiếu
 * Giữ nguyên bảng màu thương hiệu ZoneMart & đầy đủ tính năng thực tiễn:
 * - Sidebar Menu đầy đủ (Overview, Products, Customer, Orders, Shipment, Store Setting, Partner, Feedback, Help)
 * - Upgrade Pro Card góc trái
 * - Top Bar: Search input bo tròn + Mic, Switch Chế độ mua hàng, Toggle mở tiệm, User Profile
 * - 3 KPI Cards (AVG Order Value dark card, Total Orders, Lifetime Value)
 * - Sales Overtime Dual-line SVG Chart
 * - Top Selling Product List
 * - Latest Orders Data Table & Order Workflow (Hỏa tốc 10km)
 * ================================================================
 */
import { ref, reactive, computed, onMounted } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

// Điều hướng Tab chính trong Sidebar
type NavKey =
  | "overview"
  | "products"
  | "customers"
  | "orders"
  | "shipment"
  | "settings"
  | "partner"
  | "feedback"
  | "support";

const activeNav = ref<NavKey>("overview");

// Thông tin người dùng đăng nhập
const currentUser = reactive({
  name: "Bác Ba (Vườn Rau Ba Vì)",
  email: "seller@zonemart.vn",
  avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80"
});

onMounted(() => {
  const savedUser = localStorage.getItem("currentUser");
  if (savedUser) {
    try {
      const parsed = JSON.parse(savedUser);
      if (parsed.fullName) currentUser.name = parsed.fullName;
      if (parsed.phoneEmail) currentUser.email = parsed.phoneEmail;
      if (parsed.avatarUrl) currentUser.avatar = parsed.avatarUrl;
    } catch { }
  }
});

// Tên hiển thị chào mừng lịch sự
const greetingName = computed(() => {
  if (!currentUser.name) return "Bác Ba";
  const clean = currentUser.name.split("(")[0].trim();
  return clean || currentUser.name;
});

// Trạng thái mở cửa / nhận đơn hỏa tốc
const isStoreOpen = ref(true);
const toastMessage = ref("");
const showToast = ref(false);
const isUserMenuOpen = ref(false);
const searchQuery = ref("");

const triggerToast = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 3200);
};

const toggleStoreOpen = () => {
  isStoreOpen.value = !isStoreOpen.value;
  triggerToast(
    isStoreOpen.value
      ? "Đã mở cửa gian hàng! Sẵn sàng nhận đơn hỏa tốc 10km."
      : "Đã tạm đóng cửa gian hàng. Khách hàng sẽ không thể đặt đơn mới lúc này."
  );
};

// Chuyển nhanh sang chế độ người mua
const switchToBuyerMode = () => {
  localStorage.setItem("userRole", "buyer");
  router.push("/");
};

// Đăng xuất
const handleLogout = () => {
  localStorage.removeItem("isLoggedIn");
  localStorage.removeItem("userRole");
  localStorage.removeItem("currentUser");
  router.push("/login");
};

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
  avgOrderValue: "77.210 đ",
  totalOrders: "2,107",
  lifetimeValue: "54.800.000 đ"
});

// Danh sách đơn hàng
interface OrderItem {
  name: string;
  qty: number;
  price: number;
}

interface SellerOrder {
  id: string;
  orderCode: string;
  productSummary: string;
  customerName: string;
  customerPhone: string;
  customerAddress: string;
  orderDate: string;
  priceFormatted: string;
  paymentMethod: "Chuyển khoản QR" | "Thẻ ngân hàng" | "Tiền mặt COD";
  status: "Đang xử lý" | "Hoàn thành" | "Chờ xác nhận" | "Đang giao hàng";
  items: OrderItem[];
  shipperInfo?: {
    name: string;
    phone: string;
    licensePlate: string;
  };
}

const orders = ref<SellerOrder[]>([
  {
    id: "ord-01",
    orderCode: "#2456JL",
    productSummary: "Rau muống hữu cơ Ba Vì (x2)",
    customerName: "Chị Mai Lan",
    customerPhone: "0912 345 678",
    customerAddress: "P.502 Chung cư Dịch Vọng, Cầu Giấy, Hà Nội",
    orderDate: "12 Thg 1, 12:23",
    priceFormatted: "134.000 đ",
    paymentMethod: "Chuyển khoản QR",
    status: "Đang xử lý",
    items: [
      { name: "Rau muống hữu cơ Ba Vì", qty: 2, price: 18000 },
      { name: "Cà chua bi Đà Lạt", qty: 1, price: 35000 },
      { name: "Trứng gà ta thảo mộc", qty: 1, price: 45000 }
    ]
  },
  {
    id: "ord-02",
    orderCode: "#5435DF",
    productSummary: "Thịt ba chỉ tươi sạch (x2)",
    customerName: "Anh Hoàng Minh",
    customerPhone: "0987 654 321",
    customerAddress: "Số 18 ngõ 20 Hồ Tùng Mậu, Cầu Giấy",
    orderDate: "01 Thg 5, 13:13",
    priceFormatted: "152.000 đ",
    paymentMethod: "Thẻ ngân hàng",
    status: "Hoàn thành",
    items: [
      { name: "Thịt ba chỉ heo tươi sạch", qty: 2, price: 65000 },
      { name: "Xà lách mỡ thủy canh", qty: 1, price: 22000 }
    ],
    shipperInfo: {
      name: "Trần Văn Bình",
      phone: "0934 888 999",
      licensePlate: "29M1-9999"
    }
  },
  {
    id: "ord-03",
    orderCode: "#9876XC",
    productSummary: "Dưa leo baby & Khoai tây Đà Lạt",
    customerName: "Cô Thu Hà",
    customerPhone: "0903 111 222",
    customerAddress: "Số 92 đường Trần Thái Tông, Cầu Giấy",
    orderDate: "20 Thg 9, 09:08",
    priceFormatted: "441.000 đ",
    paymentMethod: "Chuyển khoản QR",
    status: "Hoàn thành",
    items: [
      { name: "Dưa leo baby giòn ngọt", qty: 2, price: 25000 },
      { name: "Khoai tây vàng Đà Lạt", qty: 3, price: 30000 },
      { name: "Nấm đùi gà hữu cơ", qty: 2, price: 45000 }
    ]
  },
  {
    id: "ord-04",
    orderCode: "#7721AQ",
    productSummary: "Cam sành Hàm Yên (2kg)",
    customerName: "Bác Tuấn Hưng",
    customerPhone: "0977 444 555",
    customerAddress: "Số 104 Xuân Thủy, Cầu Giấy, Hà Nội",
    orderDate: "15 Thg 10, 15:45",
    priceFormatted: "84.000 đ",
    paymentMethod: "Tiền mặt COD",
    status: "Đang xử lý",
    items: [
      { name: "Cam sành Hàm Yên (1kg)", qty: 2, price: 42000 }
    ]
  },
  {
    id: "ord-05",
    orderCode: "#1189MN",
    productSummary: "Táo Envy Mỹ & Dâu tây Mộc Châu",
    customerName: "Nguyễn Thị Ngọc",
    customerPhone: "0966 888 222",
    customerAddress: "Toà Keangnam Landmark 72, Nam Từ Liêm",
    orderDate: "02 Thg 11, 10:15",
    priceFormatted: "295.000 đ",
    paymentMethod: "Chuyển khoản QR",
    status: "Đang xử lý",
    items: [
      { name: "Táo Envy Mỹ nhập khẩu", qty: 1, price: 125000 },
      { name: "Dâu tây Mộc Châu Sơn La", qty: 1, price: 170000 }
    ]
  }
]);

// Danh sách sản phẩm bán chạy (Top Selling Products)
interface TopProduct {
  id: string;
  name: string;
  salesText: string;
  statusText: string;
  stockText: string;
  image: string;
}

const topProducts = ref<TopProduct[]>([
  {
    id: "tp-1",
    name: "Rau muống hữu cơ Ba Vì VietGAP",
    salesText: "12,429 Đã bán",
    statusText: "Còn hàng",
    stockText: "Còn 135 tồn kho",
    image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=150&q=80"
  },
  {
    id: "tp-2",
    name: "Thịt ba chỉ heo sạch chuẩn CP",
    salesText: "1,543 Đã bán",
    statusText: "Còn hàng",
    stockText: "Còn 78 tồn kho",
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=150&q=80"
  },
  {
    id: "tp-3",
    name: "Cà chua bi Đà Lạt mọng nước",
    salesText: "7,222 Đã bán",
    statusText: "Còn hàng",
    stockText: "Còn 465 tồn kho",
    image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=150&q=80"
  }
]);

// Quản lý sản phẩm (Tab Products)
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
    stock: 60,
    image: "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  },
  {
    id: "p-05",
    name: "Cam sành Hàm Yên mọng nước",
    category: "Trái cây tươi",
    price: 42000,
    unit: "Kg",
    stock: 35,
    image: "https://images.unsplash.com/photo-1582979512210-99b6a53386f9?auto=format&fit=crop&w=400&q=80",
    isAvailable: true
  }
]);

// Modal thêm sản phẩm mới
const showAddProductModal = ref(false);
const newProductForm = reactive({
  name: "",
  category: "Rau củ quả",
  price: 0,
  unit: "Bó 500g",
  stock: 20,
  image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80"
});

const handleSaveProduct = () => {
  if (!newProductForm.name.trim() || newProductForm.price <= 0) {
    triggerToast("Vui lòng nhập tên và giá bán hợp lệ!");
    return;
  }
  products.value.unshift({
    id: `p-${Date.now()}`,
    name: newProductForm.name.trim(),
    category: newProductForm.category,
    price: newProductForm.price,
    unit: newProductForm.unit,
    stock: newProductForm.stock,
    image: newProductForm.image,
    isAvailable: true
  });
  showAddProductModal.value = false;
  newProductForm.name = "";
  newProductForm.price = 0;
  triggerToast("Đã thêm sản phẩm mới vào gian hàng thành công!");
};

// Xử lý đơn hàng
const handleActionOrder = (order: SellerOrder) => {
  if (order.status === "Đang xử lý") {
    order.status = "Hoàn thành";
    triggerToast(`Đơn hàng ${order.orderCode} đã hoàn tất và bàn giao thành công!`);
  } else {
    triggerToast(`Đang xem chi tiết đơn hàng ${order.orderCode}`);
  }
};

// Filtered orders for table
const displayedOrders = computed(() => {
  if (!searchQuery.value.trim()) return orders.value;
  const q = searchQuery.value.toLowerCase().trim();
  return orders.value.filter(
    o =>
      o.orderCode.toLowerCase().includes(q) ||
      o.customerName.toLowerCase().includes(q) ||
      o.productSummary.toLowerCase().includes(q)
  );
});
</script>

<template>
  <div class="seller-app-layout">
    <!-- ==================== 1. SIDEBAR TRÁI HIỆN ĐẠI ==================== -->
    <aside class="seller-sidebar">
      <!-- Logo thương hiệu -->
      <div class="sidebar-brand-container">
        <router-link to="/seller" class="brand-link">
          <div class="brand-logo-badge">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round">
              <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
              <line x1="3" y1="6" x2="21" y2="6"></line>
              <path d="M16 10a4 4 0 0 1-8 0"></path>
            </svg>
          </div>
          <div class="brand-text-block">
            <span class="brand-title">ZoneMart</span>
            <span class="brand-role-tag">Kênh Người Bán</span>
          </div>
        </router-link>
      </div>

      <!-- Navigation Menu -->
      <nav class="sidebar-nav">
        <!-- 1. Overview -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'overview' }"
          @click="activeNav = 'overview'"
        >
          <i class="bi bi-grid-fill nav-icon"></i>
          <span class="nav-label">Tổng quan</span>
        </button>

        <!-- 2. Products -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'products' }"
          @click="activeNav = 'products'"
        >
          <i class="bi bi-box-seam nav-icon"></i>
          <span class="nav-label">Sản phẩm</span>
        </button>

        <!-- 3. Customer -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'customers' }"
          @click="activeNav = 'customers'"
        >
          <i class="bi bi-people nav-icon"></i>
          <span class="nav-label">Khách hàng</span>
        </button>

        <!-- 4. Orders -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'orders' }"
          @click="activeNav = 'orders'"
        >
          <i class="bi bi-bag-check nav-icon"></i>
          <span class="nav-label">Đơn hàng</span>
          <span v-if="orders.length" class="nav-badge-pill">{{ orders.length }}</span>
        </button>

        <!-- 5. Shipment -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'shipment' }"
          @click="activeNav = 'shipment'"
        >
          <i class="bi bi-truck nav-icon"></i>
          <span class="nav-label">Vận chuyển</span>
        </button>

        <!-- 6. Store Setting -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'settings' }"
          @click="activeNav = 'settings'"
        >
          <i class="bi bi-shop nav-icon"></i>
          <span class="nav-label">Cài đặt gian hàng</span>
        </button>

        <!-- 7. Platform Partner -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'partner' }"
          @click="activeNav = 'partner'"
        >
          <i class="bi bi-share nav-icon"></i>
          <span class="nav-label">Đối tác nền tảng</span>
        </button>

        <!-- 8. Feedback -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'feedback' }"
          @click="activeNav = 'feedback'"
        >
          <i class="bi bi-chat-square-text nav-icon"></i>
          <span class="nav-label">Đánh giá & Phản hồi</span>
        </button>

        <!-- 9. Help & Support -->
        <button
          type="button"
          class="nav-item-btn"
          :class="{ active: activeNav === 'support' }"
          @click="activeNav = 'support'"
        >
          <i class="bi bi-question-circle nav-icon"></i>
          <span class="nav-label">Trợ giúp & Hỗ trợ</span>
        </button>
      </nav>

    
    </aside>

    <!-- ==================== 2. MAIN CONTAINER ==================== -->
    <div class="seller-main-wrapper">
      <!-- TOP NAVIGATION BAR -->
      <header class="seller-top-bar">
        <!-- Search Input tròn với Mic -->
        <div class="search-box-pill">
          <i class="bi bi-search search-icon"></i>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm kiếm đơn hàng, sản phẩm, khách hàng..."
            class="search-input-field"
          />
          <button type="button" class="mic-btn" title="Tìm kiếm giọng nói">
            <i class="bi bi-mic"></i>
          </button>
        </div>

        <!-- Right action cluster -->
        <div class="top-bar-right">
          <!-- Chuyển sang chế độ mua hàng -->
          <button
            type="button"
            class="btn-buyer-switch"
            @click="switchToBuyerMode"
            title="Chuyển sang giao diện Mua hàng ZoneMart"
          >
            <i class="bi bi-bag-check me-1"></i>
            <span>Chế độ Người Mua</span>
          </button>

          <!-- Toggle mở/đóng cửa hàng -->
          <div class="store-status-toggle" :class="{ 'is-open': isStoreOpen }" @click="toggleStoreOpen">
            <span class="status-indicator-dot"></span>
            <span class="status-text">{{ isStoreOpen ? "Mở tiệm nhận đơn" : "Tạm đóng" }}</span>
          </div>

          <!-- User Profile Dropdown Capsule -->
          <div class="user-profile-capsule" @click="isUserMenuOpen = !isUserMenuOpen">
            <img :src="currentUser.avatar" alt="User Avatar" class="profile-avatar-img" />
            <div class="profile-info-text">
              <span class="profile-name">{{ currentUser.name }}</span>
              <span class="profile-email">{{ currentUser.email }}</span>
            </div>
            <i class="bi bi-chevron-down chevron-icon" :class="{ rotated: isUserMenuOpen }"></i>

            <!-- Dropdown Menu -->
            <div v-if="isUserMenuOpen" class="user-dropdown-popup" @click.stop>
              <div class="dropdown-header">
                <b>{{ currentUser.name }}</b>
                <small>{{ storeInfo.name }}</small>
              </div>
              <button type="button" class="dropdown-link" @click="activeNav = 'settings'; isUserMenuOpen = false">
                <i class="bi bi-gear me-2"></i> Cài đặt gian hàng
              </button>
              <button type="button" class="dropdown-link" @click="switchToBuyerMode">
                <i class="bi bi-cart3 me-2"></i> Vào trang mua sắm
              </button>
              <div class="dropdown-divider"></div>
              <button type="button" class="dropdown-link text-danger" @click="handleLogout">
                <i class="bi bi-box-arrow-right me-2"></i> Đăng xuất
              </button>
            </div>
          </div>
        </div>
      </header>

      <!-- BODY CONTENT THEO TAB -->
      <main class="seller-content-body">
        <!-- ==================== TAB 1: OVERVIEW ==================== -->
        <section v-if="activeNav === 'overview'" class="overview-view-container">
          <!-- Heading Welcome -->
          <div class="welcome-heading-section anim-welcome">
            <h1 class="welcome-title">
              Chào mừng trở lại, <span class="bold-name">{{ greetingName }} !</span>
            </h1>
            <p class="welcome-subtitle">Tổng quan hiệu suất bán hàng & doanh thu hôm nay của gian hàng</p>
          </div>

          <!-- TOP 3 KPI STAT CARDS -->
          <div class="kpi-cards-grid">
            <!-- Card 1: AVG . Order Value (Dark Card nổi bật) -->
            <div class="kpi-card dark-kpi-card anim-kpi-1">
              <div class="kpi-card-header">
                <span class="kpi-label">Giá Trị Đơn Trung Bình</span>
                <div class="kpi-icon-badge dark-badge">
                  <i class="bi bi-stack"></i>
                </div>
              </div>
              <div class="kpi-value-row">
                <span class="kpi-number">{{ storeInfo.avgOrderValue }}</span>
              </div>
              <div class="kpi-trend-row green-trend">
                <span class="trend-pct">+ 3.16%</span>
                <span class="trend-sub">So với tháng trước</span>
              </div>
            </div>

            <!-- Card 2: Total Orders -->
            <div class="kpi-card white-kpi-card anim-kpi-2">
              <div class="kpi-card-header">
                <span class="kpi-label">Tổng Đơn Hàng</span>
                <div class="kpi-icon-badge white-badge">
                  <i class="bi bi-clipboard-data"></i>
                </div>
              </div>
              <div class="kpi-value-row">
                <span class="kpi-number">{{ storeInfo.totalOrders }}</span>
              </div>
              <div class="kpi-trend-row red-trend">
                <span class="trend-pct">- 1.18%</span>
                <span class="trend-sub">So với tháng trước</span>
              </div>
            </div>

            <!-- Card 3: Lifetime Value -->
            <div class="kpi-card white-kpi-card anim-kpi-3">
              <div class="kpi-card-header">
                <span class="kpi-label">Doanh Thu Tích Lũy</span>
                <div class="kpi-icon-badge white-badge">
                  <i class="bi bi-pie-chart"></i>
                </div>
              </div>
              <div class="kpi-value-row">
                <span class="kpi-number">{{ storeInfo.lifetimeValue }}</span>
              </div>
              <div class="kpi-trend-row green-trend">
                <span class="trend-pct">+ 2.24%</span>
                <span class="trend-sub">So với tháng trước</span>
              </div>
            </div>
          </div>

          <!-- MIDDLE SECTION: SALES OVERTIME CHART & TOP SELLING PRODUCT -->
          <div class="middle-dashboard-grid">
            <!-- 1. CỘT TRÁI: SALES OVERTIME CHART -->
            <div class="dashboard-panel-card chart-panel anim-chart">
              <div class="panel-header-row">
                <h3 class="panel-title">Doanh Thu Theo Thời Gian</h3>
                <div class="chart-legend-group">
                  <div class="legend-item purple-legend">
                    <span class="legend-dot purple-dot"></span>
                    <span>Doanh thu</span>
                  </div>
                  <div class="legend-item blue-legend">
                    <span class="legend-dot blue-dot"></span>
                    <span>Đơn hàng</span>
                  </div>
                  <button type="button" class="chart-menu-btn" title="Tùy chọn hiển thị">
                    <i class="bi bi-list"></i>
                  </button>
                </div>
              </div>

              <!-- SVG Interactive Dual Line Chart -->
              <div class="chart-canvas-container">
                <svg viewBox="0 0 650 240" class="smooth-line-svg" preserveAspectRatio="none">
                  <defs>
                    <linearGradient id="purpleGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stop-color="#A855F7" stop-opacity="0.28" />
                      <stop offset="100%" stop-color="#A855F7" stop-opacity="0.0" />
                    </linearGradient>
                    <linearGradient id="blueGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stop-color="#3B82F6" stop-opacity="0.18" />
                      <stop offset="100%" stop-color="#3B82F6" stop-opacity="0.0" />
                    </linearGradient>
                  </defs>

                  <!-- Grid lines -->
                  <line x1="50" y1="30" x2="630" y2="30" stroke="#F1F5F9" stroke-width="1" />
                  <line x1="50" y1="80" x2="630" y2="80" stroke="#F1F5F9" stroke-width="1" />
                  <line x1="50" y1="130" x2="630" y2="130" stroke="#F1F5F9" stroke-width="1" />
                  <line x1="50" y1="180" x2="630" y2="180" stroke="#F1F5F9" stroke-width="1" />
                  <line x1="50" y1="210" x2="630" y2="210" stroke="#E2E8F0" stroke-width="1" />

                  <!-- Y-Axis labels -->
                  <text x="15" y="34" class="axis-label">20 tr</text>
                  <text x="15" y="84" class="axis-label">15 tr</text>
                  <text x="15" y="134" class="axis-label">10 tr</text>
                  <text x="15" y="184" class="axis-label">5 tr</text>
                  <text x="25" y="214" class="axis-label">0 đ</text>

                  <!-- Area under purple curve -->
                  <path
                    class="chart-area-path"
                    d="M 60 170 C 110 130, 140 120, 180 150 C 220 180, 260 160, 310 140 C 350 120, 390 160, 440 140 C 490 120, 520 135, 570 155 C 600 170, 620 145, 630 140 L 630 210 L 60 210 Z"
                    fill="url(#purpleGradient)"
                  />

                  <!-- Purple curve (Revenue) -->
                  <path
                    class="chart-line-revenue"
                    d="M 60 170 C 110 130, 140 120, 180 150 C 220 180, 260 160, 310 140 C 350 120, 390 160, 440 140 C 490 120, 520 135, 570 155 C 600 170, 620 145, 630 140"
                    fill="none"
                    stroke="#A855F7"
                    stroke-width="2.8"
                    stroke-linecap="round"
                  />

                  <!-- Blue curve (Orders) -->
                  <path
                    class="chart-line-order"
                    d="M 60 190 C 110 180, 140 170, 180 180 C 220 190, 260 175, 310 165 C 350 155, 390 175, 440 160 C 490 150, 520 160, 570 180 C 600 190, 620 165, 630 150"
                    fill="none"
                    stroke="#3B82F6"
                    stroke-width="2.4"
                    stroke-linecap="round"
                  />

                  <!-- Dotted Indicator at Aug (x=310) -->
                  <line class="chart-guide-line" x1="310" y1="120" x2="310" y2="210" stroke="#CBD5E1" stroke-dasharray="3,3" stroke-width="1.5" />
                  <circle class="chart-pulse-dot dot-purple" cx="310" cy="140" r="5" fill="#A855F7" stroke="#FFFFFF" stroke-width="2" />
                  <circle class="chart-pulse-dot dot-blue" cx="310" cy="165" r="5" fill="#3B82F6" stroke="#FFFFFF" stroke-width="2" />
                </svg>

                <!-- Floating Average Tooltip Overlay -->
                <div class="chart-floating-tooltip animated-tooltip">
                  <span class="tooltip-title">Trung bình</span>
                  <div class="tooltip-line">
                    <span class="dot p-dot"></span>
                    <span class="date">Thg 8, 2026</span>
                    <span class="val">18.5 tr</span>
                  </div>
                  <div class="tooltip-line">
                    <span class="dot b-dot"></span>
                    <span class="date">Thg 8, 2026</span>
                    <span class="val">142 đơn</span>
                  </div>
                </div>

                <!-- X-Axis Month labels -->
                <div class="x-axis-row">
                  <span>Thg 6</span>
                  <span>Thg 7</span>
                  <span class="active-month">Thg 8</span>
                  <span>Thg 9</span>
                  <span>Thg 10</span>
                  <span>Thg 11</span>
                  <span>Thg 12</span>
                  <span>Thg 1</span>
                </div>
              </div>
            </div>

            <!-- 2. CỘT PHẢI: TOP SELLING PRODUCT -->
            <div class="dashboard-panel-card top-product-panel anim-top-product">
              <div class="panel-header-row">
                <h3 class="panel-title">Sản Phẩm Bán Chạy Nhất</h3>
                <button type="button" class="btn-see-all" @click="activeNav = 'products'">
                  Xem Tất Cả
                </button>
              </div>

              <!-- Product items list -->
              <div class="top-products-list">
                <div v-for="item in topProducts" :key="item.id" class="top-product-item">
                  <img :src="item.image" :alt="item.name" class="top-product-thumb" />
                  <div class="top-product-info">
                    <h5 class="product-name-title">{{ item.name }}</h5>
                    <span class="product-sales-count">{{ item.salesText }}</span>
                  </div>
                  <div class="top-product-status-col">
                    <span class="status-available-badge">
                      <span class="mini-dot"></span> {{ item.statusText }}
                    </span>
                    <span class="stock-remaining-text">{{ item.stockText }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- BOTTOM SECTION: LATEST ORDERS TABLE -->
          <div class="dashboard-panel-card table-panel anim-table">
            <div class="panel-header-row">
              <h3 class="panel-title">Đơn Hàng Gần Đây</h3>
              <div class="table-actions-group">
                <button type="button" class="table-tool-btn" @click="triggerToast('Tùy chỉnh cột hiển thị')">
                  <i class="bi bi-sliders me-1"></i> Tùy biến
                </button>
                <button type="button" class="table-tool-btn" @click="triggerToast('Bộ lọc đơn hàng')">
                  <i class="bi bi-funnel me-1"></i> Bộ lọc
                </button>
                <button type="button" class="table-tool-btn" @click="triggerToast('Đã xuất danh sách đơn hàng ra file Excel!')">
                  <i class="bi bi-download me-1"></i> Xuất file
                </button>
              </div>
            </div>

            <!-- Clean Modern Data Table -->
            <div class="table-responsive-box">
              <table class="modern-data-table">
                <thead>
                  <tr>
                    <th>Mã đơn</th>
                    <th>Sản phẩm</th>
                    <th>Thời gian đặt</th>
                    <th>Tổng tiền</th>
                    <th>Thanh toán</th>
                    <th>Trạng thái</th>
                    <th class="text-center">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="ord in displayedOrders" :key="ord.id">
                    <td class="order-id-cell">{{ ord.orderCode }}</td>
                    <td class="product-cell">
                      <b>{{ ord.productSummary }}</b>
                      <small class="customer-sub">{{ ord.customerName }} ({{ ord.customerPhone }})</small>
                    </td>
                    <td class="date-cell">{{ ord.orderDate }}</td>
                    <td class="price-cell">{{ ord.priceFormatted }}</td>
                    <td class="payment-cell">{{ ord.paymentMethod }}</td>
                    <td class="status-cell">
                      <span
                        class="order-badge"
                        :class="{
                          'badge-processing': ord.status === 'Đang xử lý',
                          'badge-completed': ord.status === 'Hoàn thành',
                          'badge-pending': ord.status === 'Chờ xác nhận'
                        }"
                      >
                        {{ ord.status }}
                      </span>
                    </td>
                    <td class="action-cell text-center">
                      <button
                        type="button"
                        class="btn-row-action"
                        @click="handleActionOrder(ord)"
                        title="Thao tác xử lý"
                      >
                        <i class="bi bi-three-dots"></i>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 2: PRODUCTS ==================== -->
        <section v-else-if="activeNav === 'products'" class="tab-page-container">
          <div class="tab-header-flex">
            <div>
              <h2 class="tab-heading">Quản Lý Sản Phẩm Gian Hàng</h2>
              <p class="tab-subheading">Xem, thêm mới, sửa giá và quản lý tồn kho nông sản VietGAP</p>
            </div>
            <button type="button" class="btn-brand-primary" @click="showAddProductModal = true">
              <i class="bi bi-plus-lg me-1"></i> Thêm Sản Phẩm Mới
            </button>
          </div>

          <div class="dashboard-panel-card">
            <div class="table-responsive-box">
              <table class="modern-data-table">
                <thead>
                  <tr>
                    <th>Hình ảnh</th>
                    <th>Tên sản phẩm</th>
                    <th>Danh mục</th>
                    <th>Đơn vị</th>
                    <th>Giá niêm yết</th>
                    <th>Tồn kho</th>
                    <th>Trạng thái</th>
                    <th class="text-center">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="prod in products" :key="prod.id">
                    <td>
                      <img :src="prod.image" :alt="prod.name" class="table-prod-img" />
                    </td>
                    <td><b>{{ prod.name }}</b></td>
                    <td>{{ prod.category }}</td>
                    <td>{{ prod.unit }}</td>
                    <td class="price-cell">{{ prod.price.toLocaleString('vi-VN') }} đ</td>
                    <td>{{ prod.stock }}</td>
                    <td>
                      <span class="badge-status-pill" :class="prod.isAvailable ? 'available' : 'out-of-stock'">
                        {{ prod.isAvailable ? 'Đang bán' : 'Hết hàng' }}
                      </span>
                    </td>
                    <td class="text-center">
                      <button
                        type="button"
                        class="btn-action-pill"
                        @click="prod.isAvailable = !prod.isAvailable; triggerToast('Đã cập nhật trạng thái sản phẩm!')"
                      >
                        {{ prod.isAvailable ? 'Tạm ngưng' : 'Bật bán' }}
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 3: CUSTOMERS ==================== -->
        <section v-else-if="activeNav === 'customers'" class="tab-page-container">
          <div class="tab-header-flex">
            <div>
              <h2 class="tab-heading">Khách Hàng Thân Thiết</h2>
              <p class="tab-subheading">Danh sách khách hàng quen trong bán kính 10km của gian hàng</p>
            </div>
          </div>

          <div class="dashboard-panel-card">
            <div class="customer-cards-grid">
              <div class="customer-item-card">
                <div class="customer-avatar-badge">ML</div>
                <div class="customer-details">
                  <h4>Chị Mai Lan</h4>
                  <p>0912 345 678 • Cầu Giấy, Hà Nội</p>
                  <span class="customer-tag">Khách VIP • 18 đơn hàng</span>
                </div>
              </div>
              <div class="customer-item-card">
                <div class="customer-avatar-badge">HM</div>
                <div class="customer-details">
                  <h4>Anh Hoàng Minh</h4>
                  <p>0987 654 321 • Mai Dịch, Hà Nội</p>
                  <span class="customer-tag">Khách quen • 12 đơn hàng</span>
                </div>
              </div>
              <div class="customer-item-card">
                <div class="customer-avatar-badge">TH</div>
                <div class="customer-details">
                  <h4>Cô Thu Hà</h4>
                  <p>0903 111 222 • Dịch Vọng, Hà Nội</p>
                  <span class="customer-tag">Khách quen • 9 đơn hàng</span>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 4: ORDERS ==================== -->
        <section v-else-if="activeNav === 'orders'" class="tab-page-container">
          <div class="tab-header-flex">
            <div>
              <h2 class="tab-heading">Quy Trình Xử Lý Đơn Hàng</h2>
              <p class="tab-subheading">Xác nhận đóng gói và điều phối tài xế giao hỏa tốc 10km</p>
            </div>
          </div>

          <div class="orders-flow-grid">
            <div v-for="ord in orders" :key="ord.id" class="order-flow-card">
              <div class="order-flow-header">
                <span class="order-flow-code">{{ ord.orderCode }}</span>
                <span class="order-badge" :class="ord.status === 'Hoàn thành' ? 'badge-completed' : 'badge-processing'">
                  {{ ord.status }}
                </span>
              </div>
              <p class="order-customer-info">
                <b>{{ ord.customerName }}</b> — {{ ord.customerPhone }}<br />
                <span class="address-text">{{ ord.customerAddress }}</span>
              </p>
              <div class="order-items-preview">
                <div v-for="(it, idx) in ord.items" :key="idx" class="item-line">
                  <span>{{ it.name }} (x{{ it.qty }})</span>
                  <b>{{ (it.price * it.qty).toLocaleString('vi-VN') }} đ</b>
                </div>
              </div>
              <div class="order-total-footer">
                <span>Tổng tiền: <b>{{ ord.priceFormatted }}</b> ({{ ord.paymentMethod }})</span>
                <button
                  type="button"
                  class="btn-order-next"
                  @click="handleActionOrder(ord)"
                >
                  {{ ord.status === 'Hoàn thành' ? 'Đã hoàn tất' : 'Xác nhận & Giao shipper' }}
                </button>
              </div>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 5: SHIPMENT ==================== -->
        <section v-else-if="activeNav === 'shipment'" class="tab-page-container">
          <div class="tab-header-flex">
            <div>
              <h2 class="tab-heading">Vận Chuyển Hỏa Tốc ZoneMart 10km</h2>
              <p class="tab-subheading">Mạng lưới tài xế đối tác giao hàng siêu tốc trong 30-45 phút</p>
            </div>
          </div>

          <div class="dashboard-panel-card">
            <div class="shipment-status-box">
              <div class="shipment-stat-item">
                <i class="bi bi-geo-alt-fill text-danger stat-icon"></i>
                <div>
                  <h4>Bán kính giao hàng</h4>
                  <p>10 km quanh vị trí tiệm (Cầu Giấy)</p>
                </div>
              </div>
              <div class="shipment-stat-item">
                <i class="bi bi-lightning-charge-fill text-warning stat-icon"></i>
                <div>
                  <h4>Thời gian cam kết</h4>
                  <p>30 - 45 phút kể từ lúc đóng gói</p>
                </div>
              </div>
              <div class="shipment-stat-item">
                <i class="bi bi-shield-check text-success stat-icon"></i>
                <div>
                  <h4>Đối tác Shipper</h4>
                  <p>Tài xế đã xác minh CCCD & bằng lái</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 6: STORE SETTING ==================== -->
        <section v-else-if="activeNav === 'settings'" class="tab-page-container">
          <div class="tab-header-flex">
            <div>
              <h2 class="tab-heading">Cài Đặt Gian Hàng</h2>
              <p class="tab-subheading">Quản lý địa chỉ cửa hàng, giờ hoạt động và tài khoản VietQR nhận tiền</p>
            </div>
          </div>

          <div class="settings-two-cols">
            <div class="dashboard-panel-card">
              <h4 class="settings-card-title">Thông Tin Cửa Hàng</h4>
              <div class="settings-field">
                <label>Tên gian hàng</label>
                <input v-model="storeInfo.name" type="text" class="settings-input" />
              </div>
              <div class="settings-field">
                <label>Địa chỉ đón khách & lấy hàng</label>
                <input v-model="storeInfo.address" type="text" class="settings-input" />
              </div>
              <div class="settings-field">
                <label>Khung giờ mở cửa</label>
                <input v-model="storeInfo.openHours" type="text" class="settings-input" />
              </div>
            </div>

            <div class="dashboard-panel-card">
              <h4 class="settings-card-title">Tài Khoản Nhận Tiền VietQR</h4>
              <div class="settings-field">
                <label>Ngân hàng nhận thanh toán</label>
                <input v-model="storeInfo.bankName" type="text" class="settings-input" />
              </div>
              <div class="settings-field">
                <label>Số tài khoản ngân hàng</label>
                <input v-model="storeInfo.bankAccount" type="text" class="settings-input" />
              </div>
              <div class="settings-field">
                <label>Tên chủ thẻ thụ hưởng</label>
                <input v-model="storeInfo.accountHolder" type="text" class="settings-input" />
              </div>
              <button type="button" class="btn-brand-primary mt-3" @click="triggerToast('Đã lưu thông tin cài đặt thành công!')">
                Lưu Thay Đổi
              </button>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 7: PLATFORM PARTNER ==================== -->
        <section v-else-if="activeNav === 'partner'" class="tab-page-container">
          <div class="dashboard-panel-card text-center p-5">
            <i class="bi bi-award-fill text-warning fs-1 mb-3"></i>
            <h3 class="mb-2">Đối Tác Bán Lẻ Chính Thức ZoneMart</h3>
            <p class="text-muted max-w-md mx-auto">
              Gian hàng của bạn được hưởng mức chiết khấu 0% phí nền tảng trong 12 tháng đầu tiên dành cho nông sản địa phương.
            </p>
          </div>
        </section>

        <!-- ==================== TAB 8: FEEDBACK ==================== -->
        <section v-else-if="activeNav === 'feedback'" class="tab-page-container">
          <div class="dashboard-panel-card">
            <h3 class="panel-title mb-3">Đánh Giá & Nhận Xét Của Khách Hàng (4.9 / 5 ⭐)</h3>
            <div class="feedback-list">
              <div class="feedback-item">
                <div class="feedback-header">
                  <b>Chị Lan Hương</b>
                  <span class="stars">⭐⭐⭐⭐⭐</span>
                </div>
                <p>Rau muống rất tươi ngon, giao hỏa tốc 20 phút là tới nơi!</p>
              </div>
              <div class="feedback-item">
                <div class="feedback-header">
                  <b>Anh Minh Quân</b>
                  <span class="stars">⭐⭐⭐⭐⭐</span>
                </div>
                <p>Thịt ba chỉ đóng khay sạch sẽ, tem VietGAP rõ ràng. Sẽ ủng hộ shop dài lâu.</p>
              </div>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 9: HELP & SUPPORT ==================== -->
        <section v-else-if="activeNav === 'support'" class="tab-page-container">
          <div class="dashboard-panel-card">
            <h3 class="panel-title mb-3">Trung Tâm Hỗ Trợ Kênh Người Bán</h3>
            <div class="support-channels-grid">
              <div class="support-channel-item">
                <i class="bi bi-telephone-fill text-primary"></i>
                <h5>Hotline Hỗ Trợ Kỹ Thuật</h5>
                <p>1900 8888 (24/7)</p>
              </div>
              <div class="support-channel-item">
                <i class="bi bi-chat-dots-fill text-success"></i>
                <h5>Trò Chuyện Trực Tuyến</h5>
                <p>Zalo OA: ZoneMart Partner</p>
              </div>
            </div>
          </div>
        </section>
      </main>
    </div>

    <!-- ==================== MODAL THÊM SẢN PHẨM ==================== -->
    <div v-if="showAddProductModal" class="modal-backdrop-overlay" @click.self="showAddProductModal = false">
      <div class="modal-card-box">
        <div class="modal-header-row">
          <h4>Thêm Sản Phẩm Mới</h4>
          <button type="button" class="btn-close-modal" @click="showAddProductModal = false">✕</button>
        </div>
        <div class="modal-body-fields">
          <div class="field-item">
            <label>Tên sản phẩm</label>
            <input v-model="newProductForm.name" type="text" class="field-input" placeholder="VD: Bắp cải hữu cơ Đà Lạt" />
          </div>
          <div class="field-grid-2">
            <div class="field-item">
              <label>Giá bán (VNĐ)</label>
              <input v-model.number="newProductForm.price" type="number" class="field-input" placeholder="25000" />
            </div>
            <div class="field-item">
              <label>Đơn vị tính</label>
              <input v-model="newProductForm.unit" type="text" class="field-input" placeholder="Bó 500g" />
            </div>
          </div>
          <div class="field-item">
            <label>Số lượng tồn kho</label>
            <input v-model.number="newProductForm.stock" type="number" class="field-input" placeholder="50" />
          </div>
          <div class="field-item">
            <label>URL Hình ảnh</label>
            <input v-model="newProductForm.image" type="text" class="field-input" />
          </div>
        </div>
        <div class="modal-footer-row">
          <button type="button" class="btn-cancel-gray" @click="showAddProductModal = false">Hủy</button>
          <button type="button" class="btn-brand-primary" @click="handleSaveProduct">Lưu Sản Phẩm</button>
        </div>
      </div>
    </div>

    <!-- TOAST NOTIFICATION -->
    <Transition name="fade-toast">
      <div v-if="showToast" class="seller-toast-notification">
        <i class="bi bi-check-circle-fill text-success me-2"></i>
        <span>{{ toastMessage }}</span>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* ==================== GLOBAL FONT & HIGH-CONTRAST RESET ==================== */
*, input, button, select, textarea, label {
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
  box-sizing: border-box;
}

/* Ép buộc màu chữ và nền cho input luôn rõ ràng, chống lỗi Dark Mode hệ điều hành */
input, select, textarea {
  color-scheme: light !important;
  color: #0F172A !important;
}

h1, h2, h3, h4, h5, h6 {
  color: #0F172A !important;
}

.seller-app-layout {
  display: flex;
  width: 100%;
  max-width: 100%;
  min-height: 100vh;
  background-color: #F8F9FA;
  color: #0F172A;
  overflow-x: hidden;
  box-sizing: border-box;
}

/* ==================== 1. SIDEBAR TRÁI ==================== */
.seller-sidebar {
  width: 250px;
  min-width: 250px;
  height: 100vh;
  background-color: #FFFFFF;
  border-right: 1px solid #F1F5F9;
  display: flex;
  flex-direction: column;
  padding: 24px 18px 20px 18px;
  position: sticky;
  top: 0;
  z-index: 100;
}

.sidebar-brand-container {
  margin-bottom: 28px;
  padding-left: 6px;
}

.brand-link {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}

.brand-logo-badge {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: #FFF7ED;
  border: 1px solid #FED7AA;
  display: flex;
  align-items: center;
  justify-content: center;
}

.brand-text-block {
  display: flex;
  flex-direction: column;
}

.brand-title {
  font-size: 19px;
  font-weight: 900;
  color: #0F172A !important;
  letter-spacing: -0.3px;
  line-height: 1.1;
}

.brand-role-tag {
  font-size: 10px;
  font-weight: 700;
  color: #D94E15 !important;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
  overflow-y: auto;
  padding-right: 2px;
}

.sidebar-nav::-webkit-scrollbar {
  width: 4px;
}
.sidebar-nav::-webkit-scrollbar-thumb {
  background: #E2E8F0;
  border-radius: 4px;
}

.nav-item-btn {
  display: flex;
  align-items: center;
  gap: 14px;
  width: 100%;
  padding: 10px 14px;
  background: transparent;
  border: none;
  border-radius: 12px;
  color: #334155 !important;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.25s ease;
  text-align: left;
  position: relative;
}

.nav-icon {
  font-size: 17px;
  color: #64748B;
  transition: all 0.2s ease;
}

.nav-item-btn:hover {
  background: #F1F5F9;
  color: #0F172A !important;
  transform: translateX(3px);
}

.nav-item-btn:hover .nav-icon {
  color: #0F172A;
}

.nav-item-btn.active {
  background: #0F172A !important;
  color: #FFFFFF !important;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.2);
}

.nav-item-btn.active .nav-label,
.nav-item-btn.active .nav-icon {
  color: #FFFFFF !important;
}

.nav-badge-pill {
  margin-left: auto;
  background: #D94E15;
  color: #FFFFFF !important;
  font-size: 10.5px;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 12px;
}

/* Upgrade Pro Card góc dưới */
.sidebar-upgrade-card {
  background: #0F172A;
  border-radius: 18px;
  padding: 18px 16px;
  color: #FFFFFF !important;
  margin-top: 14px;
  box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.25);
  transition: transform 0.3s ease;
}

.sidebar-upgrade-card:hover {
  transform: translateY(-2px);
}

.upgrade-card-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}

.upgrade-icon-box {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.12);
  display: flex;
  align-items: center;
  justify-content: center;
}

.upgrade-title {
  font-size: 16px;
  font-weight: 800;
  margin: 0;
  color: #FFFFFF !important;
}

.upgrade-desc {
  font-size: 11.5px;
  color: #CBD5E1 !important;
  line-height: 1.45;
  margin: 0 0 14px 0;
}

.btn-upgrade-action {
  width: 100%;
  padding: 9px;
  background: #FFFFFF;
  color: #0F172A !important;
  border: none;
  border-radius: 10px;
  font-size: 12.5px;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-upgrade-action:hover {
  background: #F8FAFC;
  transform: translateY(-1px);
}

/* ==================== 2. MAIN WRAPPER & TOP BAR ==================== */
.seller-main-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  width: calc(100% - 250px);
  max-width: calc(100% - 250px);
  overflow-x: hidden;
}

.seller-top-bar {
  height: 76px;
  padding: 0 36px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: transparent;
  gap: 20px;
}

/* Search Box bo tròn viên nang */
.search-box-pill {
  width: 340px;
  height: 44px;
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 30px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 10px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.02);
  transition: all 0.25s ease;
}

.search-box-pill:focus-within {
  border-color: #0F172A;
  box-shadow: 0 0 0 3px rgba(15, 23, 42, 0.08);
}

.search-icon {
  font-size: 15px;
  color: #64748B;
}

.search-input-field {
  flex: 1;
  border: none;
  background: transparent;
  outline: none;
  font-size: 13px;
  color: #0F172A !important;
  font-weight: 500;
}

.search-input-field::placeholder {
  color: #94A3B8 !important;
}

.mic-btn {
  background: none;
  border: none;
  color: #64748B;
  cursor: pointer;
  padding: 4px;
  display: flex;
  align-items: center;
  transition: color 0.2s;
}

.mic-btn:hover {
  color: #0F172A;
}

.top-bar-right {
  display: flex;
  align-items: center;
  gap: 14px;
}

.btn-buyer-switch {
  display: inline-flex;
  align-items: center;
  padding: 8px 15px;
  background: #FFF7ED;
  color: #D94E15 !important;
  border: 1.5px solid #FED7AA;
  border-radius: 20px;
  font-size: 12.5px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-buyer-switch:hover {
  background: #FFEDD5;
  transform: translateY(-1px);
}

.store-status-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 14px;
  background: #F1F5F9;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 700;
  color: #64748B !important;
  cursor: pointer;
  transition: all 0.25s ease;
  user-select: none;
  border: 1px solid #E2E8F0;
}

.status-indicator-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #94A3B8;
  transition: all 0.2s;
}

.store-status-toggle.is-open {
  background: #DCFCE7;
  color: #15803D !important;
  border-color: #BBF7D0;
}

.store-status-toggle.is-open .status-indicator-dot {
  background: #22C55E;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.25);
  animation: pulseStoreDot 2s infinite;
}

.user-profile-capsule {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  position: relative;
  user-select: none;
  padding: 4px 8px;
  border-radius: 24px;
  transition: background 0.2s;
}

.user-profile-capsule:hover {
  background: #FFFFFF;
}

.profile-avatar-img {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  object-fit: cover;
  border: 1.5px solid #E2E8F0;
}

.profile-info-text {
  display: flex;
  flex-direction: column;
}

.profile-name {
  font-size: 12.5px;
  font-weight: 800;
  color: #0F172A !important;
  line-height: 1.2;
}

.profile-email {
  font-size: 10.5px;
  color: #64748B !important;
  line-height: 1.2;
}

.chevron-icon {
  font-size: 11px;
  color: #64748B;
  transition: transform 0.2s ease;
}

.chevron-icon.rotated {
  transform: rotate(180deg);
}

.user-dropdown-popup {
  position: absolute;
  top: calc(100% + 10px);
  right: 0;
  width: 220px;
  background: #FFFFFF;
  border-radius: 14px;
  border: 1px solid #E2E8F0;
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.12);
  padding: 8px;
  z-index: 999;
}

.dropdown-header {
  padding: 8px 10px;
  border-bottom: 1px solid #F1F5F9;
  margin-bottom: 6px;
}

.dropdown-header b {
  display: block;
  font-size: 12.5px;
  color: #0F172A !important;
}

.dropdown-header small {
  font-size: 11px;
  color: #64748B !important;
}

.dropdown-link {
  width: 100%;
  display: flex;
  align-items: center;
  padding: 8px 10px;
  background: none;
  border: none;
  border-radius: 8px;
  font-size: 12.5px;
  font-weight: 600;
  color: #334155 !important;
  cursor: pointer;
  text-align: left;
  transition: background 0.15s;
}

.dropdown-link:hover {
  background: #F8FAFC;
  color: #0F172A !important;
}

.dropdown-link.text-danger {
  color: #EF4444 !important;
}

.dropdown-divider {
  height: 1px;
  background: #F1F5F9;
  margin: 6px 0;
}

/* ==================== 3. CONTENT BODY & OVERVIEW ANIMATIONS ==================== */
.seller-content-body {
  flex: 1;
  padding: 0 36px 40px 36px;
  overflow-y: auto;
  overflow-x: hidden;
  width: 100%;
  max-width: 100%;
}

.overview-view-container {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 100%;
  overflow-x: hidden;
}

/* ==================== KEYFRAMES CHO TỔNG QUAN ==================== */
@keyframes sellerFadeInUp {
  0% {
    opacity: 0;
    transform: translateY(18px);
  }
  100% {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes drawChartLine {
  0% {
    stroke-dashoffset: 1000;
  }
  100% {
    stroke-dashoffset: 0;
  }
}

@keyframes fadeInArea {
  0% {
    opacity: 0;
  }
  100% {
    opacity: 1;
  }
}

@keyframes pulsePoint {
  0% {
    r: 4.5;
    stroke-width: 2;
  }
  50% {
    r: 6.5;
    stroke-width: 3.5;
  }
  100% {
    r: 4.5;
    stroke-width: 2;
  }
}

@keyframes floatTooltip {
  0% {
    transform: translateY(0px);
  }
  100% {
    transform: translateY(-6px);
  }
}

@keyframes pulseStoreDot {
  0% {
    box-shadow: 0 0 0 0 rgba(34, 197, 94, 0.7);
  }
  70% {
    box-shadow: 0 0 0 8px rgba(34, 197, 94, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(34, 197, 94, 0);
  }
}

/* Staggered entrance animations */
.anim-welcome {
  animation: sellerFadeInUp 0.5s cubic-bezier(0.16, 1, 0.3, 1) both;
}

.anim-kpi-1 {
  animation: sellerFadeInUp 0.5s cubic-bezier(0.16, 1, 0.3, 1) 0.08s both;
}

.anim-kpi-2 {
  animation: sellerFadeInUp 0.5s cubic-bezier(0.16, 1, 0.3, 1) 0.16s both;
}

.anim-kpi-3 {
  animation: sellerFadeInUp 0.5s cubic-bezier(0.16, 1, 0.3, 1) 0.24s both;
}

.anim-chart {
  animation: sellerFadeInUp 0.6s cubic-bezier(0.16, 1, 0.3, 1) 0.32s both;
}

.anim-top-product {
  animation: sellerFadeInUp 0.6s cubic-bezier(0.16, 1, 0.3, 1) 0.4s both;
}

.anim-table {
  animation: sellerFadeInUp 0.6s cubic-bezier(0.16, 1, 0.3, 1) 0.48s both;
}

/* Welcome Header */
.welcome-heading-section {
  margin-top: 4px;
}

.welcome-title {
  font-size: 29px;
  font-weight: 600;
  color: #0F172A !important;
  margin: 0;
  letter-spacing: -0.5px;
}

.welcome-title .bold-name {
  font-weight: 900;
  color: #0F172A !important;
}

.welcome-subtitle {
  font-size: 13.5px;
  color: #475569 !important;
  font-weight: 500;
  margin: 4px 0 0 0;
}

/* ==================== 3 KPI CARDS ==================== */
.kpi-cards-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 20px;
  width: 100%;
  max-width: 100%;
}

.kpi-card {
  border-radius: 20px;
  padding: 22px 24px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.03);
  position: relative;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  overflow: hidden;
}

.kpi-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 16px 32px -4px rgba(15, 23, 42, 0.08);
}

.kpi-card:hover .kpi-icon-badge {
  transform: scale(1.1) rotate(4deg);
}

/* Dark Card (AVG Order Value) */
.dark-kpi-card {
  background: #0F172A;
  color: #FFFFFF !important;
}

.dark-kpi-card:hover {
  box-shadow: 0 16px 36px -4px rgba(15, 23, 42, 0.35);
}

.dark-kpi-card .kpi-label {
  color: #94A3B8 !important;
}

.dark-kpi-card .kpi-number {
  color: #FFFFFF !important;
}

.white-kpi-card {
  background: #FFFFFF;
  border: 1.5px solid #F1F5F9;
  color: #0F172A !important;
}

.white-kpi-card .kpi-label {
  color: #475569 !important;
  font-size: 13.5px;
  font-weight: 700;
}

.white-kpi-card .kpi-number {
  color: #0F172A !important;
}

.kpi-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.kpi-label {
  font-size: 13px;
  font-weight: 700;
}

.kpi-icon-badge {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.dark-badge {
  background: rgba(255, 255, 255, 0.14);
  color: #FFFFFF !important;
}

.white-badge {
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  color: #0F172A !important;
}

.kpi-value-row {
  margin-bottom: 8px;
}

.kpi-number {
  font-size: 32px;
  font-weight: 900;
  letter-spacing: -0.8px;
}

.kpi-trend-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 600;
}

.green-trend .trend-pct {
  color: #16A34A !important;
  font-weight: 800;
}

.red-trend .trend-pct {
  color: #EF4444 !important;
  font-weight: 800;
}

.trend-sub {
  color: #64748B !important;
  font-weight: 600;
}

.dark-kpi-card .trend-sub {
  color: #94A3B8 !important;
}

/* ==================== MIDDLE ROW: CHART & TOP PRODUCT ==================== */
.middle-dashboard-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr);
  gap: 20px;
  width: 100%;
  max-width: 100%;
}

.dashboard-panel-card {
  background: #FFFFFF;
  border: 1.5px solid #F1F5F9;
  border-radius: 20px;
  padding: 24px;
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.03);
  transition: box-shadow 0.3s ease;
  min-width: 0;
  max-width: 100%;
  overflow: hidden;
}

.panel-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.panel-title {
  font-size: 17px;
  font-weight: 800;
  color: #0F172A !important;
  margin: 0;
  letter-spacing: -0.2px;
}

.chart-legend-group {
  display: flex;
  align-items: center;
  gap: 16px;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12.5px;
  font-weight: 700;
  color: #334155 !important;
}

.legend-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
}

.purple-dot { background: #A855F7; }
.blue-dot { background: #3B82F6; }

.chart-menu-btn {
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s ease;
}

.chart-menu-btn:hover {
  background: #F1F5F9;
  color: #0F172A;
}

.chart-canvas-container {
  position: relative;
  width: 100%;
  overflow: hidden;
}

.smooth-line-svg {
  width: 100%;
  max-width: 100%;
  height: 190px;
  overflow: hidden;
}

/* Đường vẽ biểu đồ có animation vẽ lượn sóng */
.chart-line-revenue {
  stroke-dasharray: 1000;
  stroke-dashoffset: 1000;
  animation: drawChartLine 1.6s cubic-bezier(0.16, 1, 0.3, 1) 0.2s forwards;
}

.chart-line-order {
  stroke-dasharray: 1000;
  stroke-dashoffset: 1000;
  animation: drawChartLine 1.8s cubic-bezier(0.16, 1, 0.3, 1) 0.35s forwards;
}

.chart-area-path {
  animation: fadeInArea 1.4s ease 0.6s forwards;
  opacity: 0;
}

.chart-pulse-dot {
  animation: pulsePoint 2.4s ease-in-out infinite;
  transform-origin: center;
}

.axis-label {
  font-size: 11px;
  font-weight: 700;
  fill: #64748B !important;
}

.x-axis-row {
  display: flex;
  justify-content: space-between;
  padding: 6px 20px 0 50px;
  font-size: 11.5px;
  font-weight: 700;
  color: #64748B !important;
}

.x-axis-row span {
  transition: all 0.2s ease;
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 4px;
}

.x-axis-row span:hover,
.x-axis-row .active-month {
  color: #0F172A !important;
  font-weight: 900;
  background: #F1F5F9;
}

/* Tooltip nổi trên biểu đồ có animation lơ lửng */
.chart-floating-tooltip {
  position: absolute;
  top: 38px;
  left: 45%;
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  box-shadow: 0 10px 25px -4px rgba(15, 23, 42, 0.15);
  border-radius: 12px;
  padding: 8px 14px;
  pointer-events: none;
}

.animated-tooltip {
  animation: floatTooltip 3.2s ease-in-out infinite alternate;
}

.tooltip-title {
  font-size: 10.5px;
  font-weight: 800;
  color: #475569 !important;
  display: block;
  margin-bottom: 4px;
}

.tooltip-line {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  color: #0F172A !important;
  font-weight: 700;
  margin-bottom: 3px;
}

.tooltip-line .dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.tooltip-line .dot.p-dot { background: #A855F7; }
.tooltip-line .dot.b-dot { background: #3B82F6; }

.tooltip-line .date {
  color: #475569 !important;
  font-size: 10px;
  font-weight: 600;
}

.tooltip-line .val {
  font-weight: 900;
  margin-left: auto;
  color: #0F172A !important;
}

/* TOP SELLING PRODUCT PANEL */
.btn-see-all {
  background: #F8FAFC;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  padding: 6px 12px;
  font-size: 11.5px;
  font-weight: 700;
  color: #1E293B !important;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-see-all:hover {
  background: #F1F5F9;
  color: #0F172A !important;
  border-color: #94A3B8;
}

.top-products-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.top-product-item {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 8px 10px;
  border-radius: 12px;
  transition: all 0.25s ease;
}

.top-product-item:hover {
  background: #F8FAFC;
  transform: translateX(4px);
}

.top-product-thumb {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  object-fit: cover;
  background: #F1F5F9;
  transition: transform 0.25s ease;
}

.top-product-item:hover .top-product-thumb {
  transform: scale(1.08);
}

.top-product-info {
  flex: 1;
  min-width: 0;
}

.product-name-title {
  font-size: 13.5px;
  font-weight: 800;
  color: #0F172A !important;
  margin: 0 0 3px 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.product-sales-count {
  font-size: 11.5px;
  color: #64748B !important;
  font-weight: 600;
}

.top-product-status-col {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 3px;
}

.status-available-badge {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 11px;
  font-weight: 800;
  color: #16A34A !important;
}

.status-available-badge .mini-dot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: #16A34A;
}

.stock-remaining-text {
  font-size: 10.5px;
  color: #64748B !important;
  font-weight: 600;
}

/* ==================== BOTTOM SECTION: LATEST ORDERS TABLE ==================== */
.table-panel {
  padding: 24px;
}

.table-actions-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.table-tool-btn {
  background: #FFFFFF;
  border: 1.5px solid #CBD5E1;
  border-radius: 8px;
  padding: 6px 14px;
  font-size: 12px;
  font-weight: 700;
  color: #334155 !important;
  cursor: pointer;
  transition: all 0.2s ease;
}

.table-tool-btn:hover {
  background: #F8FAFC;
  color: #0F172A !important;
  border-color: #0F172A;
}

.table-responsive-box {
  width: 100%;
  overflow-x: auto;
}

.modern-data-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

.modern-data-table th {
  padding: 12px 14px;
  font-size: 12.5px;
  font-weight: 700;
  color: #475569 !important;
  border-bottom: 1.5px solid #E2E8F0;
}

.modern-data-table td {
  padding: 16px 14px;
  font-size: 13px;
  color: #0F172A !important;
  border-bottom: 1px solid #F1F5F9;
  vertical-align: middle;
}

.modern-data-table tbody tr {
  transition: all 0.2s ease;
}

.modern-data-table tbody tr:hover {
  background: #F8FAFC;
}

.order-id-cell {
  font-weight: 800;
  color: #0F172A !important;
}

.product-cell {
  display: flex;
  flex-direction: column;
}

.product-cell b {
  font-weight: 700;
  color: #0F172A !important;
}

.customer-sub {
  font-size: 11.5px;
  color: #64748B !important;
  font-weight: 500;
  margin-top: 2px;
}

.date-cell {
  color: #475569 !important;
  font-size: 12.5px;
  font-weight: 500;
  white-space: nowrap;
}

.price-cell {
  font-weight: 900;
  color: #0F172A !important;
  white-space: nowrap;
}

.payment-cell {
  color: #334155 !important;
  font-weight: 600;
}

.order-badge {
  display: inline-block;
  padding: 4px 11px;
  border-radius: 12px;
  font-size: 11.5px;
  font-weight: 700;
}

.badge-processing {
  color: #1D4ED8 !important;
  background: #DBEAFE;
}

.badge-completed {
  color: #15803D !important;
  background: #DCFCE7;
}

.badge-pending {
  color: #B45309 !important;
  background: #FEF3C7;
}

.btn-row-action {
  background: none;
  border: none;
  color: #64748B;
  font-size: 18px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: all 0.2s ease;
}

.btn-row-action:hover {
  background: #E2E8F0;
  color: #0F172A;
}

/* ==================== SUB-TABS (PRODUCTS, ORDERS, SETTINGS...) ==================== */
.tab-page-container {
  display: flex;
  flex-direction: column;
  gap: 20px;
  animation: sellerFadeInUp 0.4s cubic-bezier(0.16, 1, 0.3, 1) both;
}

.tab-header-flex {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.tab-heading {
  font-size: 24px;
  font-weight: 800;
  color: #0F172A !important;
  margin: 0;
}

.tab-subheading {
  font-size: 13.5px;
  color: #475569 !important;
  font-weight: 500;
  margin: 4px 0 0 0;
}

.btn-brand-primary {
  padding: 10px 18px;
  background: #D94E15;
  color: #FFFFFF !important;
  border: none;
  border-radius: 12px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-brand-primary:hover {
  background: #C8451F;
  transform: translateY(-1px);
}

.table-prod-img {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  object-fit: cover;
}

.badge-status-pill {
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 700;
}

.badge-status-pill.available {
  background: #DCFCE7;
  color: #15803D !important;
}

.badge-status-pill.out-of-stock {
  background: #FEE2E2;
  color: #B91C1C !important;
}

.btn-action-pill {
  padding: 5px 12px;
  border-radius: 8px;
  background: #F8FAFC;
  border: 1px solid #CBD5E1;
  font-size: 11.5px;
  font-weight: 700;
  color: #1E293B !important;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-action-pill:hover {
  background: #F1F5F9;
  color: #0F172A !important;
}

/* SETTINGS FORM - HIGH CONTRAST */
.settings-two-cols {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.settings-card-title {
  font-size: 17px;
  font-weight: 800;
  color: #0F172A !important;
  margin: 0 0 16px 0;
}

.settings-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 14px;
}

.settings-field label {
  font-size: 13px;
  font-weight: 700;
  color: #1E293B !important;
}

.settings-input {
  width: 100%;
  padding: 10px 14px;
  border-radius: 10px;
  border: 1.5px solid #CBD5E1;
  background: #FFFFFF !important;
  color: #0F172A !important;
  font-size: 13.5px;
  font-weight: 600;
  outline: none;
  transition: all 0.2s ease;
}

.settings-input:focus {
  background: #FFFFFF !important;
  border-color: #D94E15;
  box-shadow: 0 0 0 3px rgba(217, 78, 21, 0.12);
}

/* CUSTOMER CARDS */
.customer-cards-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.customer-item-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 14px;
  transition: all 0.25s ease;
}

.customer-item-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.04);
}

.customer-avatar-badge {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: #0F172A;
  color: #FFFFFF !important;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  font-size: 14px;
}

.customer-details h4 {
  margin: 0;
  font-size: 14.5px;
  font-weight: 800;
  color: #0F172A !important;
}

.customer-details p {
  margin: 2px 0 6px 0;
  font-size: 12px;
  color: #475569 !important;
  font-weight: 500;
}

.customer-tag {
  font-size: 10.5px;
  font-weight: 700;
  color: #D94E15 !important;
  background: #FFF7ED;
  border: 1px solid #FED7AA;
  padding: 2px 8px;
  border-radius: 6px;
}

/* ORDERS FLOW GRID */
.orders-flow-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 18px;
}

.order-flow-card {
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 16px;
  padding: 18px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.02);
  transition: all 0.25s ease;
}

.order-flow-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.05);
}

.order-flow-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.order-flow-code {
  font-size: 15px;
  font-weight: 900;
  color: #0F172A !important;
}

.order-customer-info {
  font-size: 12.5px;
  color: #334155 !important;
  margin: 0 0 12px 0;
}

.order-customer-info b {
  color: #0F172A !important;
}

.address-text {
  color: #475569 !important;
  font-size: 11.5px;
  font-weight: 500;
}

.order-items-preview {
  background: #F8FAFC;
  border: 1px solid #F1F5F9;
  border-radius: 10px;
  padding: 10px 12px;
  margin-bottom: 12px;
}

.item-line {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12px;
  margin-bottom: 4px;
}

.item-line span {
  color: #334155 !important;
  font-weight: 600;
}

.item-line b {
  color: #0F172A !important;
  font-weight: 800;
}

.order-total-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12.5px;
  color: #334155 !important;
  font-weight: 600;
}

.btn-order-next {
  padding: 8px 16px;
  background: #0F172A;
  color: #FFFFFF !important;
  border: none;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-order-next:hover {
  background: #1E293B;
  transform: translateY(-1px);
}

/* SHIPMENT STATUS BOX - HIGH CONTRAST FIX */
.shipment-status-box {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.shipment-stat-item {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  padding: 18px;
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 14px;
  transition: all 0.25s ease;
}

.shipment-stat-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.04);
}

.shipment-stat-item .stat-icon {
  font-size: 26px;
}

.shipment-stat-item h4 {
  margin: 0 0 4px 0;
  font-size: 15.5px;
  font-weight: 800;
  color: #0F172A !important;
}

.shipment-stat-item p {
  margin: 0;
  font-size: 13px;
  color: #475569 !important;
  font-weight: 500;
}

/* SUPPORT CHANNELS */
.support-channels-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 18px;
}

.support-channel-item {
  padding: 22px;
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 14px;
  text-align: center;
  transition: all 0.25s ease;
}

.support-channel-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.04);
}

.support-channel-item i {
  font-size: 32px;
  margin-bottom: 10px;
  display: block;
}

.support-channel-item h5 {
  font-size: 15.5px;
  font-weight: 800;
  color: #0F172A !important;
  margin: 0 0 6px 0;
}

.support-channel-item p {
  font-size: 13px;
  color: #475569 !important;
  font-weight: 500;
  margin: 0;
}

/* FEEDBACK LIST */
.feedback-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.feedback-item {
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 14px;
  padding: 16px;
  transition: all 0.2s ease;
}

.feedback-item:hover {
  background: #F8FAFC;
}

.feedback-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}

.feedback-header b {
  color: #0F172A !important;
  font-weight: 800;
  font-size: 14px;
}

.feedback-item p {
  color: #334155 !important;
  font-size: 13px;
  line-height: 1.5;
}

/* ==================== MODAL DIALOG ==================== */
.modal-backdrop-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100%;
  height: 100%;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 99999;
  padding: 20px;
}

.modal-card-box {
  background: #FFFFFF;
  border-radius: 20px;
  width: 100%;
  max-width: 480px;
  padding: 24px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  animation: sellerFadeInUp 0.3s ease both;
}

.modal-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
}

.modal-header-row h4 {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #0F172A !important;
}

.btn-close-modal {
  background: none;
  border: none;
  font-size: 18px;
  color: #64748B;
  cursor: pointer;
  transition: color 0.2s;
}

.btn-close-modal:hover {
  color: #0F172A;
}

.modal-body-fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 20px;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.field-item label {
  font-size: 12.5px;
  font-weight: 700;
  color: #1E293B !important;
}

.field-input {
  width: 100%;
  padding: 10px 13px;
  border: 1.5px solid #CBD5E1;
  border-radius: 10px;
  background: #FFFFFF !important;
  color: #0F172A !important;
  font-size: 13.5px;
  font-weight: 600;
  outline: none;
  transition: all 0.2s ease;
}

.field-input:focus {
  background: #FFFFFF !important;
  border-color: #D94E15;
  box-shadow: 0 0 0 3px rgba(217, 78, 21, 0.12);
}

.field-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.modal-footer-row {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
}

.btn-cancel-gray {
  padding: 9px 16px;
  background: #F1F5F9;
  border: 1px solid #CBD5E1;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  color: #334155 !important;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel-gray:hover {
  background: #E2E8F0;
  color: #0F172A !important;
}

/* ==================== TOAST NOTIFICATION ==================== */
.seller-toast-notification {
  position: fixed;
  bottom: 28px;
  right: 28px;
  background: #0F172A;
  color: #FFFFFF !important;
  padding: 12px 20px;
  border-radius: 12px;
  font-size: 13px;
  font-weight: 700;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  display: flex;
  align-items: center;
  z-index: 999999;
}

.fade-toast-enter-active,
.fade-toast-leave-active {
  transition: all 0.3s ease;
}

.fade-toast-enter-from,
.fade-toast-leave-to {
  opacity: 0;
  transform: translateY(15px);
}

/* ==================== RESPONSIVE ==================== */
@media (max-width: 1100px) {
  .kpi-cards-grid {
    grid-template-columns: 1fr;
  }
  .middle-dashboard-grid {
    grid-template-columns: 1fr;
  }
  .settings-two-cols {
    grid-template-columns: 1fr;
  }
  .customer-cards-grid {
    grid-template-columns: 1fr;
  }
  .shipment-status-box {
    grid-template-columns: 1fr;
  }
}
</style>
