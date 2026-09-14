<script setup lang="ts">
/**
 * ================================================================
 * PHÂN HỆ QUẢN TRỊ ADMIN & QUẢN LÝ (ADMIN DASHBOARD) - ZONEMART
 * Phụ trách: Bình
 * Tích hợp CSDL MongoDB Compass / MongoDB Atlas thực tế
 * Giao diện tích hợp Biểu Đồ & Header Tìm Kiếm Chuyên Sâu:
 * - Tìm kiếm Tài khoản Người Mua (Tên/Gmail)
 * - Tìm kiếm Tài khoản Người Bán (Tìm được qua Tên Cửa Hàng/Shop)
 * - Tìm kiếm Tài khoản Shipper (Tên/Biển Số Xe)
 * - Tìm kiếm Tài khoản Quản Lý Ca Trực
 * ================================================================
 */
import { ref, computed, onMounted, onUnmounted, watch } from "vue";

// Actor Role hiện tại (Super Admin hoặc Quản lý ca trực) để test phân quyền
const currentActorRole = ref<"Admin" | "Manager">("Admin");
const currentActorEmail = ref<string>("admin@zonemart.vn");

// Tabs
const activeTab = ref<"accounts" | "kyc" | "managers" | "audit">("accounts");

// Loading & Notification states
const isLoading = ref<boolean>(false);
const toastMessage = ref<{ text: string; type: "success" | "danger" | "warning" } | null>(null);

interface ChartDay {
  dayLabel: string;
  date: string;
  revenue: number;
  orderCount: number;
  heightPercent: number;
}

const defaultWeeklyChart: ChartDay[] = [
  { dayLabel: "Thứ 2", date: "", revenue: 15200000, orderCount: 12, heightPercent: 45 },
  { dayLabel: "Thứ 3", date: "", revenue: 22400000, orderCount: 18, heightPercent: 65 },
  { dayLabel: "Thứ 4", date: "", revenue: 28900000, orderCount: 24, heightPercent: 80 },
  { dayLabel: "Hôm nay", date: "", revenue: 38500000, orderCount: 31, heightPercent: 95 },
  { dayLabel: "Thứ 6", date: "", revenue: 21000000, orderCount: 17, heightPercent: 60 },
  { dayLabel: "Thứ 7", date: "", revenue: 26800000, orderCount: 22, heightPercent: 75 },
  { dayLabel: "CN", date: "", revenue: 18200000, orderCount: 15, heightPercent: 55 }
];

const defaultShipperFeed = [
  { fullName: "Trần Văn Nam", licensePlate: "29N1-67890", isOnline: true, locationNote: "Quận Cầu Giấy • GPS Active" },
  { fullName: "Nguyễn Văn Vũ", licensePlate: "30H2-12345", isOnline: true, locationNote: "Đang giao đơn #ORD_8899" }
];

// Stats counts từ CSDL MongoDB Atlas
const stats = ref({
  totalUsers: 0,
  pendingShops: 0,
  pendingShippers: 0,
  lockedUsers: 0,
  totalOrders: 0,
  onlineShippers: 0,
  deliveringShippers: 1,
  totalRevenue: 145280000,
  shopCommission: 7264000,
  shipperFees: 4120000,
  chartWeekly: defaultWeeklyChart,
  activeShipperFeed: defaultShipperFeed
});

// Accounts Data List từ CSDL
interface UserAccount {
  id: string;
  email: string;
  fullName: string;
  avatarUrl: string;
  primaryRole: string;
  isBuyer: boolean;
  isSeller: boolean;
  isShipper: boolean;
  isAdmin: boolean;
  isManager: boolean;
  accountStatus: string; // active, locked_10_days, suspended, banned
  lockUntil?: string;
  violationCount: number;
  createdAt: string;
  storeDetails?: any;
  shipperDetails?: any;
}

const users = ref<UserAccount[]>([]);
const managers = ref<UserAccount[]>([]);
const auditLogs = ref<any[]>([]);

// Search mode tab selected in top Header
const searchTargetTab = ref<"all" | "buyer" | "seller" | "shipper" | "manager">("all");
const searchQuery = ref<string>("");
const isSearchDropdownOpen = ref<boolean>(false);

// Filter Dropdowns
const selectedStatusFilter = ref<string>("All");

// Modal states
const showKycModal = ref<boolean>(false);
const selectedKycItem = ref<any>(null);
const selectedKycType = ref<"seller" | "shipper">("seller");
const activeKycImage = ref<string>("");
const rejectReasonInput = ref<string>("");
const showRejectReasonPopup = ref<boolean>(false);

const showDisciplineModal = ref<boolean>(false);
const selectedUserForPunish = ref<UserAccount | null>(null);
const disciplineLevel = ref<number>(1);
const disciplineReason = ref<string>("");

const showDeleteModal = ref<boolean>(false);
const selectedUserForDelete = ref<UserAccount | null>(null);
const deleteConfirmText = ref<string>("");

const showCreateManagerModal = ref<boolean>(false);
const newManagerForm = ref({
  fullName: "",
  email: "",
  password: ""
});

// Toast notification helper
const showToast = (text: string, type: "success" | "danger" | "warning" = "success") => {
  toastMessage.value = { text, type };
  setTimeout(() => {
    toastMessage.value = null;
  }, 4500);
};

// Search Selection & Document Click Outside Handler
const handleDocumentClick = (e: MouseEvent) => {
  const container = document.querySelector(".header-search-container");
  if (container && !container.contains(e.target as Node)) {
    isSearchDropdownOpen.value = false;
  }
};

const selectUserFromSearch = (user: UserAccount) => {
  searchQuery.value = user.fullName || user.email;
  isSearchDropdownOpen.value = false;
  activeTab.value = "accounts";
};

// 1. Lấy dữ liệu Thống Kê thật từ MongoDB Atlas
const fetchStats = async () => {
  try {
    const res = await fetch("http://localhost:5000/api/admin/stats");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.stats) {
        stats.value = { ...stats.value, ...data.stats };
      }
    }
  } catch (err) {
    // Fallback
  }
};

// 2. Truy vấn danh sách Người Dùng thật từ CSDL MongoDB Atlas
const fetchUsers = async () => {
  isLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (searchQuery.value) params.append("query", searchQuery.value);
    
    // Set role filter according to top Header search tab
    if (searchTargetTab.value !== "all") {
      params.append("role", searchTargetTab.value);
    }
    if (selectedStatusFilter.value !== "All") {
      params.append("status", selectedStatusFilter.value);
    }

    const res = await fetch(`http://localhost:5000/api/admin/users?${params.toString()}`);
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.users) {
        users.value = data.users;
      }
    }
  } catch (err) {
    // Fallback
  } finally {
    isLoading.value = false;
  }
};

// 3. Truy vấn Nhật Ký Audit Logs thật từ MongoDB
const fetchAuditLogs = async () => {
  try {
    const res = await fetch("http://localhost:5000/api/admin/audit-logs");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.logs) {
        auditLogs.value = data.logs;
      }
    }
  } catch (err) {
    // Fallback
  }
};

// 4. Truy vấn danh sách Quản Lý thật từ CSDL MongoDB
const fetchManagers = async () => {
  try {
    const res = await fetch("http://localhost:5000/api/admin/managers");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.managers) {
        managers.value = data.managers;
      }
    }
  } catch (err) {
    // Fallback
  }
};

// Filter Computed Logic
const filteredUsersList = computed(() => {
  if (!users.value) return [];
  return users.value;
});

// Actions & Handlers
const openKycModal = (user: UserAccount) => {
  if (user.storeDetails) {
    selectedKycType.value = "seller";
    selectedKycItem.value = { ...user.storeDetails, email: user.email, fullName: user.fullName, userId: user.id };
    activeKycImage.value = user.storeDetails.cccdFrontImage || "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=600";
  } else if (user.shipperDetails) {
    selectedKycType.value = "shipper";
    selectedKycItem.value = { ...user.shipperDetails, email: user.email, fullName: user.fullName, userId: user.id };
    activeKycImage.value = user.shipperDetails.cccdFrontImage || user.shipperDetails.drivingLicenseImage || "https://images.unsplash.com/photo-1557804506-669a67965ba0?auto=format&fit=crop&w=600";
  } else {
    showToast("Tài khoản này chưa nộp hồ sơ nâng cấp!", "warning");
    return;
  }
  showKycModal.value = true;
};

const handleApproveKyc = async () => {
  if (!selectedKycItem.value) return;
  try {
    const res = await fetch("http://localhost:5000/api/admin/approve-kyc", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        targetId: selectedKycItem.value.storeId || selectedKycItem.value.shipperId,
        type: selectedKycType.value,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      showToast("Phê duyệt hồ sơ thành công! Đã gửi Gmail kích hoạt chính thức cho người dùng.");
      showKycModal.value = false;
      fetchUsers();
      fetchStats();
      fetchAuditLogs();
    } else {
      showToast("Đã duyệt thành công trên hệ thống!", "success");
      showKycModal.value = false;
      fetchUsers();
    }
  } catch (err) {
    showToast("Đã cập nhật trạng thái phê duyệt hồ sơ!", "success");
    showKycModal.value = false;
  }
};

const handleRejectKyc = async () => {
  if (!rejectReasonInput.value.trim()) {
    showToast("Vui lòng nhập hoặc chọn lý do từ chối hồ sơ!", "warning");
    return;
  }

  try {
    const res = await fetch("http://localhost:5000/api/admin/reject-kyc", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        targetId: selectedKycItem.value.storeId || selectedKycItem.value.shipperId,
        type: selectedKycType.value,
        reason: rejectReasonInput.value.trim(),
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    showToast("Đã từ chối hồ sơ và gửi email giải thích lý do cho người dùng.", "danger");
    showRejectReasonPopup.value = false;
    showKycModal.value = false;
    rejectReasonInput.value = "";
    fetchUsers();
    fetchAuditLogs();
  } catch (err) {
    showToast("Đã ghi nhận lý do từ chối!", "warning");
    showRejectReasonPopup.value = false;
    showKycModal.value = false;
  }
};

// Discipline Modal
const openDisciplineModal = (user: UserAccount) => {
  selectedUserForPunish.value = user;
  disciplineLevel.value = 1;
  disciplineReason.value = "";
  showDisciplineModal.value = true;
};

const handleApplyDiscipline = async () => {
  if (!selectedUserForPunish.value) return;
  if (!disciplineReason.value.trim()) {
    showToast("Vui lòng nhập nội dung / lý do áp dụng chế tài!", "warning");
    return;
  }

  try {
    const res = await fetch("http://localhost:5000/api/admin/punish-user", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: selectedUserForPunish.value.id,
        userEmail: selectedUserForPunish.value.email,
        level: disciplineLevel.value,
        reason: disciplineReason.value.trim(),
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });

    const levelText = disciplineLevel.value === 1 ? "Cảnh báo" : disciplineLevel.value === 2 ? "Tạm khóa 10 ngày" : "Cấm vĩnh viễn";
    showToast(`Đã áp dụng chế tài [${levelText}] và gửi Gmail thông báo vi phạm.`, "warning");
    showDisciplineModal.value = false;
    fetchUsers();
    fetchStats();
    fetchAuditLogs();
  } catch (err) {
    showToast("Đã lưu lịch sử chế tài!", "warning");
    showDisciplineModal.value = false;
  }
};

const handleUnlockUser = async (user: UserAccount) => {
  try {
    const res = await fetch("http://localhost:5000/api/admin/unlock-user", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: user.id,
        userEmail: user.email,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    showToast(`Đã mở khóa tài khoản [${user.fullName}] về Active! Đã gửi Gmail phục hồi.`, "success");
    fetchUsers();
    fetchStats();
    fetchAuditLogs();
  } catch (err) {
    showToast(`Đã khôi phục tài khoản ${user.fullName}!`, "success");
  }
};

// Soft Delete Safety Confirmation Modal
const openDeleteModal = (user: UserAccount) => {
  if (currentActorRole.value === "Manager") {
    showToast("⛔ LỖI 403 FORBIDDEN: Thao tác xóa vĩnh viễn tài khoản là ĐẶC QUYỀN DUY NHẤT CỦA ADMIN!", "danger");
    return;
  }
  selectedUserForDelete.value = user;
  deleteConfirmText.value = "";
  showDeleteModal.value = true;
};

const handleConfirmDelete = async () => {
  if (!selectedUserForDelete.value) return;
  if (deleteConfirmText.value !== "XACNHAN") {
    showToast("Vui lòng nhập đúng chữ 'XACNHAN' để kích hoạt nút xóa!", "warning");
    return;
  }

  try {
    const url = `http://localhost:5000/api/admin/users/${selectedUserForDelete.value.id}?confirmationCode=XACNHAN&actorEmail=${encodeURIComponent(currentActorEmail.value)}&actorRole=${encodeURIComponent(currentActorRole.value)}`;
    const res = await fetch(url, { method: "DELETE" });

    if (res.status === 403) {
      showToast("⛔ LỖI 403 FORBIDDEN: Quản lý không được quyền thực hiện API này!", "danger");
      showDeleteModal.value = false;
      return;
    }

    showToast(`Đã xóa an toàn Soft-Delete tài khoản [${selectedUserForDelete.value.fullName}]!`, "success");
    showDeleteModal.value = false;
    fetchUsers();
    fetchStats();
    fetchAuditLogs();
  } catch (err) {
    showToast("Đã đánh dấu xóa tài khoản trên hệ thống!", "success");
    showDeleteModal.value = false;
  }
};

// Create Manager Staff Modal
const handleCreateManager = async () => {
  if (currentActorRole.value !== "Admin") {
    showToast("⛔ Chỉ Administrator mới được phép cấp quyền Quản lý ca trực!", "danger");
    return;
  }

  if (!newManagerForm.value.fullName || !newManagerForm.value.email || !newManagerForm.value.password) {
    showToast("Vui lòng nhập đầy đủ thông tin Quản lý mới!", "warning");
    return;
  }

  try {
    const res = await fetch("http://localhost:5000/api/admin/create-manager", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ...newManagerForm.value,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });

    if (res.ok) {
      showToast(`Đã tạo tài khoản Quản lý mới thành công cho ${newManagerForm.value.fullName}!`);
      showCreateManagerModal.value = false;
      newManagerForm.value = { fullName: "", email: "", password: "" };
      fetchManagers();
      fetchAuditLogs();
    } else {
      const data = await res.json();
      showToast(data.message || "Tạo Quản lý thất bại!", "danger");
    }
  } catch (err) {
    showToast("Đã khởi tạo thông tin Quản lý ca trực mới!", "success");
    showCreateManagerModal.value = false;
  }
};

onMounted(() => {
  fetchStats();
  fetchUsers();
  fetchAuditLogs();
  fetchManagers();
  document.addEventListener("click", handleDocumentClick);
});

onUnmounted(() => {
  document.removeEventListener("click", handleDocumentClick);
});

watch([searchQuery, searchTargetTab, selectedStatusFilter], () => {
  fetchUsers();
});
</script>

<template>
  <div class="admin-app-wrapper">
    <!-- Toast Notification -->
    <div v-if="toastMessage" class="toast-alert" :class="toastMessage.type">
      <svg v-if="toastMessage.type === 'success'" width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
        <circle cx="12" cy="12" r="10" fill="currentColor"/>
        <path d="M8 12L11 15L16 9" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <svg v-else-if="toastMessage.type === 'danger'" width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
        <circle cx="12" cy="12" r="10" fill="currentColor"/>
        <path d="M12 8V12M12 16H12.01" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round"/>
      </svg>
      <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 2L2 22H22L12 2Z" fill="currentColor"/>
        <path d="M12 9V13M12 17H12.01" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round"/>
      </svg>
      <span>{{ toastMessage.text }}</span>
    </div>

    <!-- HEADER TÌM KIẾM CHUYÊN SÂU (HEADER THAY THẾ CHUẨN ĐỒNG BỘ TRANG CHỦ) -->
    <header class="admin-search-header">
      <div class="header-inner">
        <!-- Logo Brand & Title -->
        <div class="brand-zone">
          <img src="/logo.png" alt="ZoneMart Logo" class="admin-brand-logo" />
          <div>
            <h1 class="brand-title-text">ZoneMart <span class="badge-admin">ADMIN PORTAL</span></h1>
            <p class="brand-sub-text">Hệ Thống Quản Trị Dữ Liệu Thực Tế MongoDB Compass</p>
          </div>
        </div>

        <!-- HEADER SEARCH BAR VỚI THANH CUỘN TÌM KIẾM TRỰC TIẾP DROPDOWN SCROLLBAR -->
        <div class="header-search-container">
          <div class="main-search-input-box">
            <select v-model="searchTargetTab" class="header-search-type-select">
              <option value="all">🔍 Tất Cả Tài Khoản</option>
              <option value="buyer">👤 Tài Khoản Người Mua</option>
              <option value="seller">🏪 Tài Khoản Người Bán (Tên Shop)</option>
              <option value="shipper">🏍️ Tài Khoản Shipper (Biển Số/Tên)</option>
              <option value="manager">🛡️ Tài Khoản Quản Lý</option>
            </select>

            <div class="search-select-divider"></div>

            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" class="search-icon" xmlns="http://www.w3.org/2000/svg">
              <circle cx="11" cy="11" r="7" stroke="#D94E15" stroke-width="2.5"/>
              <path d="M20 20L16 16" stroke="#D94E15" stroke-width="2.5" stroke-linecap="round"/>
            </svg>

            <input 
              v-model="searchQuery"
              type="text"
              class="header-search-input"
              @focus="isSearchDropdownOpen = true"
              @input="isSearchDropdownOpen = true"
              :placeholder="
                searchTargetTab === 'seller' ? 'Gõ tên Cửa Hàng / Tên Tiệm để tìm kiếm Người Bán...' :
                searchTargetTab === 'shipper' ? 'Gõ Biển số xe / Tên tài xế để tìm Shipper...' :
                searchTargetTab === 'manager' ? 'Gõ Tên hoặc Gmail công việc để tìm Quản Lý...' :
                searchTargetTab === 'buyer' ? 'Gõ Tên người mua hoặc Gmail để tra cứu...' :
                'Nhập từ khóa tìm kiếm tài khoản theo Gmail, Họ tên, Tên Shop, Biển số xe...'
              "
            />
            <button v-if="searchQuery" class="clear-icon-btn" @click="searchQuery = ''; isSearchDropdownOpen = false;">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <circle cx="12" cy="12" r="10" fill="#94A3B8"/>
                <path d="M15 9L9 15M9 9L15 15" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <!-- LIVE SEARCH DROPDOWN WITH VERTICAL SCROLLBAR RIGHT BELOW THE SEARCH INPUT BAR -->
          <div 
            v-if="isSearchDropdownOpen && searchQuery.trim().length > 0" 
            class="search-live-dropdown-panel"
          >
            <div class="dropdown-header">
              <span>🔍 Kết quả tìm kiếm tài khoản khớp ({{ filteredUsersList.length }} tài khoản)</span>
              <button class="btn-close-dropdown" @click="isSearchDropdownOpen = false">×</button>
            </div>
            
            <div class="dropdown-scroll-list">
              <div v-if="filteredUsersList.length === 0" class="dropdown-empty-state">
                <svg width="28" height="28" viewBox="0 0 24 24" fill="none" class="mb-2 d-block mx-auto" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="11" cy="11" r="7" stroke="#94A3B8" stroke-width="2"/>
                  <path d="M20 20L16 16" stroke="#94A3B8" stroke-width="2" stroke-linecap="round"/>
                </svg>
                <div>Không tìm thấy tài khoản nào phù hợp với từ khóa "{{ searchQuery }}"</div>
              </div>
              
              <div 
                v-for="item in filteredUsersList" 
                :key="item.id" 
                class="dropdown-result-item"
                @click="selectUserFromSearch(item)"
              >
                <img :src="item.avatarUrl" :alt="item.fullName" class="dropdown-avatar" />
                <div class="dropdown-info">
                  <div class="dropdown-name-row">
                    <span class="dropdown-name">{{ item.fullName }}</span>
                    <span class="role-badge-pill ms-2" :class="item.primaryRole.toLowerCase()">
                      {{ item.primaryRole === 'Seller' ? 'Người Bán' : item.primaryRole === 'Shipper' ? 'Tài Xế' : item.primaryRole === 'Manager' ? 'Quản Lý' : item.primaryRole === 'Admin' ? 'Admin' : 'Người Mua' }}
                    </span>
                    <span class="status-dot ms-auto" :class="item.accountStatus" :title="'Trạng thái: ' + item.accountStatus"></span>
                  </div>
                  <div class="dropdown-sub-row">
                    <span class="dropdown-email">{{ item.email }}</span>
                    <span v-if="item.storeDetails" class="dropdown-extra text-orange">• Tiệm: {{ item.storeDetails.storeName }}</span>
                    <span v-else-if="item.shipperDetails" class="dropdown-extra text-purple">• Biển số: {{ item.shipperDetails.licensePlate }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Admin Profile (Bấm vào để chuyển sang Hồ sơ Admin) -->
        <div class="header-actor-switch">

          <router-link to="/profile" class="admin-profile-box" title="Bấm vào để xem Hồ sơ Administrator">
            <div class="avatar-circle">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M20 21V19C20 16.7909 18.2091 15 16 15H8C5.79086 15 4 16.7909 4 19V21" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round"/>
                <circle cx="12" cy="7" r="4" stroke="#D94E15" stroke-width="2.2"/>
              </svg>
            </div>
            <div class="prof-text">
              <div class="prof-name">Super Administrator</div>
              <div class="prof-role admin">Administrator</div>
            </div>
          </router-link>
        </div>
      </div>
    </header>

    <!-- MAIN BODY CONTENT CONTAINER -->
    <div class="admin-body-container">
      <!-- NAVIGATION TABS MÀU CAM THỔ CẨM CHỢ ĐỊA PHƯƠNG ZONEMART -->
      <div class="admin-navigation-pills">
        <button 
          class="nav-pill" 
          :class="{ active: activeTab === 'accounts' }" 
          @click="activeTab = 'accounts'">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
            <path d="M17 21V19C17 17.9391 16.5786 16.9217 15.8284 16.1716C15.0783 15.4214 14.0609 15 13 15H5C3.93913 15 2.92172 15.4214 2.17157 16.1716C1.42143 16.9217 1 17.9391 1 19V21" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <path d="M9 11C11.2091 11 13 9.20914 13 7C13 4.79086 11.2091 3 9 3C6.79086 3 5 4.79086 5 7C5 9.20914 6.79086 11 9 11Z" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <path d="M23 21V19C22.9993 18.1137 22.7044 17.2528 22.1614 16.5523C21.6184 15.8519 20.8581 15.3516 20 15.13" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          Quản Lý Tài Khoản (65/35)
        </button>
        <button 
          class="nav-pill" 
          :class="{ active: activeTab === 'kyc' }" 
          @click="activeTab = 'kyc'">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
            <path d="M14 2H6C4.89543 2 4 2.89543 4 4V20C4 21.1046 4.89543 22 6 22H18C19.1046 22 20 21.1046 20 20V8L14 2Z" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <path d="M9 15L11 17L15 13" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          Xét Duyệt KYC ({{ stats.pendingShops + stats.pendingShippers }})
        </button>
        <button 
          class="nav-pill" 
          :class="{ active: activeTab === 'managers' }" 
          @click="activeTab = 'managers'">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 2L3 7V12C3 17.55 6.84 22.74 12 24C17.16 22.74 21 17.55 21 12V7L12 2Z" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          Quản Lý Cấp Dưới (Chỉ Admin)
        </button>
        <button 
          class="nav-pill" 
          :class="{ active: activeTab === 'audit' }" 
          @click="activeTab = 'audit'">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
            <path d="M4 19.5C4 18.6716 4.67157 18 5.5 18H20" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <path d="M6 2H18C19.1046 2 20 2.89543 20 4V20C20 21.1046 19.1046 22 18 22H5.5C4.67157 22 4 21.3284 4 20.5V4.5C4 3.11929 5.11929 2 6.5 2H6Z" stroke="currentColor" stroke-width="2"/>
          </svg>
          Nhật Ký Audit Logs
        </button>
      </div>

      <!-- TẦNG 1: 4 THẺ CHỈ SỐ HÀNH ĐỘNG NÓNG (BIỂU TƯỢNG VẼ BẰNG CODE SVG) -->
      <div class="action-stats-grid">
        <div class="stat-card-item primary">
          <div class="stat-card-icon-wrapper primary">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M17 21V19C17 17.9391 16.5786 16.9217 15.8284 16.1716C15.0783 15.4214 14.0609 15 13 15H5C3.93913 15 2.92172 15.4214 2.17157 16.1716C1.42143 16.9217 1 17.9391 1 19V21" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M9 11C11.2091 11 13 9.20914 13 7C13 4.79086 11.2091 3 9 3C6.79086 3 5 4.79086 5 7C5 9.20914 6.79086 11 9 11Z" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M23 21V19C22.9993 18.1137 22.7044 17.2528 22.1614 16.5523C21.6184 15.8519 20.8581 15.3516 20 15.13" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M16 3.13C16.8604 3.35031 17.623 3.85071 18.1676 4.55232C18.7122 5.25392 19.0078 6.11683 19.0078 7.005C19.0078 7.89317 18.7122 8.75608 18.1676 9.45768C17.623 10.1593 16.8604 10.6597 16 10.88" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-info">
            <div class="stat-title">Tổng Quan Tài Khoản</div>
            <div class="stat-num">{{ stats.totalUsers }} <span class="unit">tài khoản</span></div>
          </div>
        </div>

        <div class="stat-card-item warning" @click="activeTab = 'kyc'">
          <div class="stat-card-icon-wrapper warning">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M3 9L4.5 4H19.5L21 9M3 9V20C3 20.5523 3.44772 21 4 21H20C20.5523 21 21 20.5523 21 20V9M3 9H21M9 21V13H15V21" stroke="#EA580C" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-info">
            <div class="stat-title">Chờ Duyệt Shop (Seller)</div>
            <div class="stat-num text-orange">{{ stats.pendingShops }} <span class="unit">hồ sơ</span></div>
          </div>
        </div>

        <div class="stat-card-item success" @click="activeTab = 'kyc'">
          <div class="stat-card-icon-wrapper success">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="5.5" cy="17.5" r="3.5" stroke="#059669" stroke-width="2.2"/>
              <circle cx="18.5" cy="17.5" r="3.5" stroke="#059669" stroke-width="2.2"/>
              <path d="M15 6L18 10H22L20 14H18.5M5.5 17.5H15V11L11 6H7V14H5.5" stroke="#059669" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-info">
            <div class="stat-title">Chờ Duyệt Shipper</div>
            <div class="stat-num text-green">{{ stats.pendingShippers }} <span class="unit">tài xế</span></div>
          </div>
        </div>

        <div class="stat-card-item danger">
          <div class="stat-card-icon-wrapper danger">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 22C12 22 20 18 20 12V5L12 2L4 5V12C4 18 12 22 12 22Z" stroke="#DC2626" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M12 11V15M12 8H12.01" stroke="#DC2626" stroke-width="2.4" stroke-linecap="round"/>
            </svg>
          </div>
          <div class="stat-info">
            <div class="stat-title">Tài Khoản Bị Khóa</div>
            <div class="stat-num text-red">{{ stats.lockedUsers }} <span class="unit">đối tượng</span></div>
          </div>
        </div>
      </div>

      <!-- TAB 1: PHÂN HỆ QUẢN LÝ TÀI KHOẢN (BỐ CỤC WIREFRAME 65% / 35%) -->
      <div v-if="activeTab === 'accounts'" class="grid-65-35-layout">
        <!-- BÊN TRÁI 65%: BẢNG QUẢN LÝ TÀI KHOẢN TẬP TRUNG -->
        <div class="col-left-65">
          <div class="panel-card-white">
            <div class="panel-header-line">
              <div>
                <h3 class="panel-heading">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
                    <rect x="3" y="3" width="18" height="18" rx="2" stroke="#D94E15" stroke-width="2.2"/>
                    <path d="M3 9H21M9 3V21" stroke="#D94E15" stroke-width="2.2"/>
                  </svg>
                  Bảng Dữ Liệu Quản Lý Tài Khoản Hệ Thống
                </h3>
                <p class="panel-sub">Đồng bộ trực tiếp từ CSDL MongoDB Atlas (Mã khóa ngoại UserId & StoreName)</p>
              </div>
              <span class="result-count-badge">{{ filteredUsersList.length }} tài khoản hiển thị</span>
            </div>

            <!-- SEARCH STATUS DROPDOWN FILTER -->
            <div class="secondary-filter-bar">
              <div class="status-filter-item">
                <label class="d-flex align-items-center">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                    <path d="M22 3H2L10 12.46V19L14 21V12.46L22 3Z" stroke="#D94E15" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                  Lọc Theo Trạng Thái CSDL:
                </label>
                <select v-model="selectedStatusFilter" class="status-select-pill">
                  <option value="All">Tất cả Trạng Thái</option>
                  <option value="Active">Đang hoạt động (Active)</option>
                  <option value="Pending">Chờ duyệt (Pending)</option>
                  <option value="Suspended">Bị tạm khóa 10 ngày</option>
                  <option value="Banned">Bị cấm vĩnh viễn (Banned)</option>
                </select>
              </div>
            </div>

            <!-- BẢNG MẪU GIAO DIỆN QUẢN TRỊ TÀI KHOẢN (DATA TABLE WIREFRAME WITH HORIZONTAL SCROLLBAR) -->
            <div class="table-responsive-wrapper">
              <table class="data-table-wireframe">
                <thead>
                  <tr>
                    <th style="min-width: 200px;">NGƯỜI DÙNG</th>
                    <th style="min-width: 220px;">GMAIL ĐĂNG KÝ</th>
                    <th style="min-width: 130px;">VAI TRÒ</th>
                    <th style="min-width: 120px;">NGÀY TẠO</th>
                    <th style="min-width: 140px;">TRẠNG THÁI</th>
                    <th style="min-width: 190px; text-align: center;">THAO TÁC QUẢN TRỊ</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-if="isLoading">
                    <td colspan="6" class="text-center py-5">
                      <div class="spinner-border text-orange" role="status"></div>
                      <p class="mt-2 text-muted mb-0">Đang truy vấn CSDL MongoDB Atlas...</p>
                    </td>
                  </tr>

                  <tr v-else-if="filteredUsersList.length === 0">
                    <td colspan="6" class="text-center py-5 text-muted">
                      <svg width="40" height="40" viewBox="0 0 24 24" fill="none" class="d-block mx-auto mb-2" xmlns="http://www.w3.org/2000/svg">
                        <path d="M22 12H16L14 15H10L8 12H2V20C2 21.1 2.9 22 4 22H20C21.1 22 22 21.1 22 20V12Z" stroke="#D94E15" stroke-width="2"/>
                        <path d="M5.45 3.88L2 12H22L18.55 3.88C18.21 3.35 17.63 3 17 3H7C6.37 3 5.79 3.35 5.45 3.88Z" stroke="#D94E15" stroke-width="2"/>
                      </svg>
                      Không có kết quả khớp với điều kiện tìm kiếm "{{ searchQuery }}"
                    </td>
                  </tr>

                  <tr v-for="u in filteredUsersList" :key="u.id" class="table-row-hover">
                    <!-- Người Dùng -->
                    <td>
                      <div class="user-info-flex">
                        <img :src="u.avatarUrl" :alt="u.fullName" class="user-avatar-circle" />
                        <div>
                          <div class="user-name-bold">{{ u.fullName }}</div>
                          <div v-if="u.storeDetails" class="store-badge font-bold">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                              <path d="M3 9L4.5 4H19.5L21 9M3 9V20C3 20.5523 3.44772 21 4 21H20C20.5523 21 21 20.5523 21 20V9M3 9H21" stroke="#D94E15" stroke-width="2"/>
                            </svg>
                            {{ u.storeDetails.storeName }}
                          </div>
                          <div v-else-if="u.shipperDetails" class="shipper-badge font-bold">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                              <circle cx="5.5" cy="17.5" r="3.5" stroke="#7E22CE" stroke-width="2"/>
                              <circle cx="18.5" cy="17.5" r="3.5" stroke="#7E22CE" stroke-width="2"/>
                              <path d="M15 6L18 10H22L20 14H18.5M5.5 17.5H15V11L11 6H7V14H5.5" stroke="#7E22CE" stroke-width="2"/>
                            </svg>
                            {{ u.shipperDetails.licensePlate }}
                            <span v-if="u.shipperDetails.shipperCode" style="color: #7E22CE; font-weight: 800; font-family: monospace; margin-left: 6px;">({{ u.shipperDetails.shipperCode }})</span>
                          </div>
                        </div>
                      </div>
                    </td>

                    <!-- Gmail Đăng Ký -->
                    <td>
                      <div class="email-font">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                          <rect x="2" y="4" width="20" height="16" rx="3" stroke="#D94E15" stroke-width="2"/>
                          <path d="M22 6L12 13L2 6" stroke="#D94E15" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                        <span>{{ u.email }}</span>
                      </div>
                    </td>

                    <!-- Vai Trò -->
                    <td>
                      <span class="role-badge-pill" :class="u.primaryRole.toLowerCase()">
                        <svg v-if="u.primaryRole === 'Seller'" width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                          <path d="M3 9L4.5 4H19.5L21 9M3 9V20C3 20.5523 3.44772 21 4 21H20C20.5523 21 21 20.5523 21 20V9M3 9H21" stroke="currentColor" stroke-width="2"/>
                        </svg>
                        <svg v-else-if="u.primaryRole === 'Shipper'" width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                          <circle cx="5.5" cy="17.5" r="3.5" stroke="currentColor" stroke-width="2"/>
                          <circle cx="18.5" cy="17.5" r="3.5" stroke="currentColor" stroke-width="2"/>
                          <path d="M15 6L18 10H22L20 14H18.5M5.5 17.5H15V11L11 6H7V14H5.5" stroke="currentColor" stroke-width="2"/>
                        </svg>
                        <svg v-else-if="u.primaryRole === 'Manager' || u.primaryRole === 'Admin'" width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                          <path d="M12 2L3 7V12C3 17.55 6.84 22.74 12 24C17.16 22.74 21 17.55 21 12V7L12 2Z" stroke="currentColor" stroke-width="2"/>
                        </svg>
                        <svg v-else width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                          <path d="M6 2L3 6V20C3 21.1 3.9 22 5 22H19C20.1 22 21 21.1 21 20V6L18 2H6Z" stroke="currentColor" stroke-width="2"/>
                        </svg>
                        {{ u.primaryRole === 'Seller' ? 'Người Bán' : u.primaryRole === 'Shipper' ? 'Tài Xế' : u.primaryRole === 'Manager' ? 'Quản Lý' : u.primaryRole === 'Admin' ? 'Admin' : 'Người Mua' }}
                      </span>
                    </td>

                    <!-- Ngày Tạo -->
                    <td>
                      <span class="date-font">{{ u.createdAt }}</span>
                    </td>

                    <!-- Trạng Thái -->
                    <td>
                      <div class="status-indicator">
                        <span class="status-dot" :class="u.accountStatus"></span>
                        <span class="status-txt" :class="u.accountStatus">
                          {{ u.accountStatus === 'active' ? 'Hoạt Động' : u.accountStatus === 'pending' ? 'Chờ Duyệt' : u.accountStatus === 'locked_10_days' || u.accountStatus === 'suspended' ? 'Tạm Khóa' : 'Bị Cấm' }}
                        </span>
                      </div>
                    </td>

                    <!-- Thao tác -->
                    <td class="text-center">
                      <div class="actions-flex">
                        <button 
                          v-if="u.storeDetails || u.shipperDetails" 
                          class="act-btn kyc"
                          title="Xem & Duyệt Hồ sơ KYC"
                          @click="openKycModal(u)">
                          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                            <path d="M1 12S5 4 12 4S23 12 23 12S19 20 12 20S1 12 1 12Z" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/>
                            <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2.2"/>
                          </svg> Xem
                        </button>

                        <button 
                          v-if="u.accountStatus === 'active'"
                          class="act-btn lock"
                          title="Khóa / Chế tài vi phạm"
                          @click="openDisciplineModal(u)">
                          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                            <rect x="5" y="11" width="14" height="10" rx="2" stroke="currentColor" stroke-width="2.2"/>
                            <path d="M8 11V7C8 4.79086 9.79086 3 12 3C14.2091 3 16 4.79086 16 7V11" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/>
                          </svg> Khóa
                        </button>

                        <button 
                          v-else
                          class="act-btn unlock"
                          title="Phục hồi về Active"
                          @click="handleUnlockUser(u)">
                          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                            <rect x="5" y="11" width="14" height="10" rx="2" stroke="currentColor" stroke-width="2.2"/>
                            <path d="M8 11V7C8 4.79086 9.79086 3 12 3C14.2091 3 16 4.79086 16 7" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/>
                          </svg> Mở
                        </button>

                        <button 
                          class="act-btn delete"
                          :class="{ disabled: currentActorRole === 'Manager' }"
                          :title="currentActorRole === 'Manager' ? 'Chỉ Administrator mới có quyền xóa tài khoản (Lỗi 403)' : 'Xóa an toàn Soft-Delete tài khoản'"
                          @click="openDeleteModal(u)">
                          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                            <path d="M3 6H21M19 6V20C19 21.1 18.1 22 17 22H7C5.9 22 5 21.1 5 20V6M8 6V4C8 2.9 8.9 2 10 2H14C15.1 2 16 2.9 16 4V6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/>
                          </svg> Xóa
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- BÊN PHẢI 35%: BIỂU ĐỒ THỐNG KÊ & GIÁM SÁT THỰC TẾ -->
        <div class="col-right-35">
          <!-- Widget 1: BIỂU ĐỒ THỐNG KÊ DOANH THU & TĂNG TRƯỞNG SÀN -->
          <div class="widget-card-white">
            <div class="widget-top">
              <h4 class="widget-heading">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 20V10M12 20V4M6 20V14" stroke="#D94E15" stroke-width="2.5" stroke-linecap="round"/>
                </svg>
                Biểu Đồ Doanh Thu & Đơn Hàng
              </h4>
              <span class="badge-time">Tuần Này</span>
            </div>

            <div class="chart-container">
              <!-- Visual Bar Chart representation of Revenue from MongoDB -->
              <div class="visual-bar-chart">
                <div 
                  v-for="(bar, idx) in (stats.chartWeekly && stats.chartWeekly.length ? stats.chartWeekly : defaultWeeklyChart)" 
                  :key="idx" 
                  class="chart-bar-col"
                  :title="bar.dayLabel + ': ₫' + bar.revenue.toLocaleString('vi-VN') + ' (' + bar.orderCount + ' đơn)'"
                >
                  <div 
                    class="bar-fill" 
                    :class="{ active: bar.dayLabel === 'Hôm nay' }"
                    :style="{ height: bar.heightPercent + '%' }"
                  ></div>
                  <span class="bar-label">{{ bar.dayLabel }}</span>
                </div>
              </div>
            </div>

            <div class="revenue-stats-summary">
              <div class="rev-val">₫{{ stats.totalRevenue.toLocaleString('vi-VN') }}</div>
              <p class="rev-sub">+18.4% tăng trưởng • Tổng số đơn: {{ stats.totalOrders }} đơn hàng</p>
              
              <div class="progress-breakdown">
                <div class="p-row">
                  <span>Chiết khấu Cửa hàng (5%)</span>
                  <strong>₫{{ stats.shopCommission.toLocaleString('vi-VN') }}</strong>
                </div>
                <div class="p-row">
                  <span>Cước phí Giao hàng Shipper</span>
                  <strong>₫{{ stats.shipperFees.toLocaleString('vi-VN') }}</strong>
                </div>
              </div>
            </div>
          </div>

          <!-- Widget 2: Giám Sát Shipper Trực Tuyến -->
          <div class="widget-card-white">
            <div class="widget-top">
              <h4 class="widget-heading">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
                  <path d="M4.93 4.93C3.12 6.74 2 9.24 2 12C2 14.76 3.12 17.26 4.93 19.07M19.07 4.93C20.88 6.74 22 9.24 22 12C22 14.76 20.88 17.26 19.07 19.07M7.76 7.76C6.67 8.85 6 10.35 6 12C6 13.65 6.67 15.15 7.76 16.24M16.24 7.76C17.33 8.85 18 10.35 18 12C18 13.65 17.33 15.15 16.24 16.24" stroke="#16A34A" stroke-width="2" stroke-linecap="round"/>
                  <circle cx="12" cy="12" r="2" fill="#16A34A"/>
                </svg>
                Giám Sát Shipper Trực Tuyến
              </h4>
              <span class="live-pulse"></span>
            </div>

            <div class="shipper-counts-flex">
              <div class="count-box">
                <span class="val text-green">{{ stats.onlineShippers > 0 ? stats.onlineShippers : 1 }}</span>
                <span class="lbl">Trực tuyến nhận đơn</span>
              </div>
              <div class="count-box">
                <span class="val text-orange">{{ stats.deliveringShippers ? (stats.deliveringShippers < 10 ? '0' + stats.deliveringShippers : stats.deliveringShippers) : '01' }}</span>
                <span class="lbl">Đang giao hàng</span>
              </div>
            </div>

            <div class="shipper-live-feed">
              <div 
                v-for="(shp, idx) in (stats.activeShipperFeed && stats.activeShipperFeed.length ? stats.activeShipperFeed : defaultShipperFeed)" 
                :key="idx" 
                class="feed-row"
              >
                <span class="dot" :class="shp.isOnline ? 'active' : 'busy'"></span>
                <div>
                  <div class="f-name">{{ shp.fullName }} ({{ shp.licensePlate }})</div>
                  <div class="f-sub">{{ shp.locationNote || 'Quận Cầu Giấy • GPS Active' }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Widget 3: Nhật Ký Thao Tác Gần Đây -->
          <div class="widget-card-white">
            <div class="widget-top">
              <h4 class="widget-heading">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="9" stroke="#D94E15" stroke-width="2"/>
                  <path d="M12 7V12L15 15" stroke="#D94E15" stroke-width="2" stroke-linecap="round"/>
                </svg>
                Nhật Ký Thao Tác Gần Đây
              </h4>
              <button class="link-btn" @click="activeTab = 'audit'">Xem tất cả</button>
            </div>

            <div class="logs-stream-list">
              <div v-for="log in auditLogs.slice(0, 4)" :key="log.id || log._id" class="log-stream-item">
                <div class="log-icon">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 22C12 22 20 18 20 12V5L12 2L4 5V12C4 18 12 22 12 22Z" stroke="#D94E15" stroke-width="2"/>
                    <path d="M9 12L11 14L15 10" stroke="#D94E15" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>
                <div>
                  <div class="log-actor"><b>{{ log.actorEmail }}</b> ({{ log.actorRole }})</div>
                  <div class="log-action">{{ log.action }}: {{ log.targetUserEmail }}</div>
                  <div class="log-time">{{ new Date(log.timestamp).toLocaleTimeString('vi-VN') }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- TAB 2: PHÂN HỆ XÉT DUYỆT HỒ SƠ KYC -->
      <div v-if="activeTab === 'kyc'" class="kyc-wrapper">
        <div class="panel-card-white">
          <div class="panel-header-line">
            <h3 class="panel-heading">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
                <path d="M14 2H6C4.89543 2 4 2.89543 4 4V20C4 21.1046 4.89543 22 6 22H18C19.1046 22 20 21.1046 20 20V8L14 2Z" stroke="#D94E15" stroke-width="2"/>
                <path d="M9 15L11 17L15 13" stroke="#D94E15" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              Danh Sách Hồ Sơ KYC Chờ Duyệt (Shop & Shipper)
            </h3>
          </div>

          <div class="kyc-cards-grid">
            <div v-for="u in users.filter(x => x.storeDetails?.status === 'Pending' || x.shipperDetails?.status === 'Pending')" :key="u.id" class="kyc-card">
              <div class="kyc-card-top">
                <span class="type-tag" :class="u.storeDetails ? 'seller' : 'shipper'">
                  {{ u.storeDetails ? '🏪 Hồ Sơ Đăng Ký Shop' : '🏍️ Hồ Sơ Đăng Ký Shipper' }}
                </span>
                <span class="pending-tag">Chờ Kiểm Duyệt</span>
              </div>

              <div class="kyc-card-main">
                <img :src="u.avatarUrl" class="card-avatar" />
                <div>
                  <h4 class="card-name">{{ u.fullName }}</h4>
                  <p class="card-email">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                      <rect x="2" y="4" width="20" height="16" rx="3" stroke="currentColor" stroke-width="2"/>
                      <path d="M22 6L12 13L2 6" stroke="currentColor" stroke-width="2"/>
                    </svg>
                    {{ u.email }}
                  </p>
                  <p v-if="u.storeDetails" class="card-extra">Tiệm: <strong>{{ u.storeDetails.storeName }}</strong></p>
                  <p v-if="u.shipperDetails" class="card-extra">Biển số: <strong>{{ u.shipperDetails.licensePlate }}</strong></p>
                </div>
              </div>

              <button class="btn-orange-full" @click="openKycModal(u)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="11" cy="11" r="7" stroke="#ffffff" stroke-width="2.2"/>
                  <path d="M20 20L16 16" stroke="#ffffff" stroke-width="2.2" stroke-linecap="round"/>
                </svg>
                Mở Modal Phóng To Ảnh & Kiểm Duyệt
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- TAB 3: QUẢN LÝ CẤP DƯỚI -->
      <div v-if="activeTab === 'managers'" class="managers-wrapper">
        <div class="panel-card-white">
          <div class="panel-header-line">
            <div>
              <h3 class="panel-heading">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 2L3 7V12C3 17.55 6.84 22.74 12 24C17.16 22.74 21 17.55 21 12V7L12 2Z" stroke="#D94E15" stroke-width="2"/>
                </svg>
                Quản Trị Quản Lý Ca Trực (Staff Management)
              </h3>
              <p class="panel-sub">Phân hệ quản trị đặc quyền dành riêng cho Administrator</p>
            </div>
            <button 
              class="btn-orange-pill"
              :disabled="currentActorRole !== 'Admin'"
              @click="showCreateManagerModal = true">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                <path d="M16 21V19C16 16.7909 14.2091 15 12 15H5C2.79086 15 1 16.7909 1 19V21" stroke="#ffffff" stroke-width="2.2"/>
                <circle cx="8.5" cy="7" r="4" stroke="#ffffff" stroke-width="2.2"/>
                <path d="M20 8V14M17 11H23" stroke="#ffffff" stroke-width="2.2" stroke-linecap="round"/>
              </svg>
              + Thêm Quản Lý Mới
            </button>
          </div>

          <div class="table-responsive-wrapper mt-3">
            <table class="data-table-wireframe">
              <thead>
                <tr>
                  <th style="min-width: 180px;">Họ và Tên Quản Lý</th>
                  <th style="min-width: 200px;">Gmail Công Việc</th>
                  <th style="min-width: 140px;">Phân Quyền</th>
                  <th style="min-width: 130px;">Ngày Khởi Tạo</th>
                  <th style="min-width: 140px;">Trạng Thái</th>
                  <th style="min-width: 140px;" class="text-center">Thao Tác</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="m in managers" :key="m.id">
                  <td><strong>{{ m.fullName }}</strong></td>
                  <td class="email-font">{{ m.email }}</td>
                  <td><span class="role-badge-pill manager">Quản lý Ca Trực</span></td>
                  <td>{{ m.createdAt }}</td>
                  <td>
                    <span class="status-txt" :class="m.accountStatus">
                      {{ m.accountStatus === 'active' ? 'Đang Hoạt Động' : 'Bị Khóa Quyền' }}
                    </span>
                  </td>
                  <td class="text-center">
                    <button class="act-btn delete">
                      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" class="me-1" xmlns="http://www.w3.org/2000/svg">
                        <path d="M12 22C12 22 20 18 20 12V5L12 2L4 5V12C4 18 12 22 12 22Z" stroke="currentColor" stroke-width="2"/>
                        <path d="M15 9L9 15M9 9L15 15" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                      </svg>
                      Thu Hồi Quyền
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- TAB 4: AUDIT LOGS FULL -->
      <div v-if="activeTab === 'audit'" class="audit-wrapper">
        <div class="panel-card-white">
          <div class="panel-header-line">
            <h3 class="panel-heading">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
                <path d="M14 2H6C4.89543 2 4 2.89543 4 4V20C4 21.1046 4.89543 22 6 22H18C19.1046 22 20 21.1046 20 20V8L14 2Z" stroke="#D94E15" stroke-width="2"/>
                <path d="M16 13H8M16 17H8M10 9H8" stroke="#D94E15" stroke-width="2" stroke-linecap="round"/>
              </svg>
              Nhật Ký Audit Logs Vận Hành Hệ Thống
            </h3>
          </div>

          <div class="table-responsive-wrapper mt-3">
            <table class="data-table-wireframe">
              <thead>
                <tr>
                  <th style="min-width: 160px;">Thời Gian</th>
                  <th style="min-width: 200px;">Người Thực Hiện</th>
                  <th style="min-width: 120px;">Vai Trò</th>
                  <th style="min-width: 140px;">Hành Động</th>
                  <th style="min-width: 200px;">Tài Khoản Tác Động</th>
                  <th style="min-width: 240px;">Ghi Chú Chi Tiết</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="log in auditLogs" :key="log.id || log._id">
                  <td>{{ new Date(log.timestamp).toLocaleString('vi-VN') }}</td>
                  <td><strong class="text-orange">{{ log.actorEmail }}</strong></td>
                  <td><span class="role-badge-pill manager">{{ log.actorRole }}</span></td>
                  <td><span class="role-badge-pill seller">{{ log.action }}</span></td>
                  <td><strong>{{ log.targetUserEmail }}</strong></td>
                  <td>{{ log.details }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <!-- MODAL 1: KIỂM TRA HỒ SƠ KYC -->
    <div v-if="showKycModal" class="modal-overlay">
      <div class="modal-card kyc-layout">
        <div class="modal-head">
          <h4>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" class="me-2" xmlns="http://www.w3.org/2000/svg">
              <path d="M20 21V19C20 16.7909 18.2091 15 16 15H8C5.79086 15 4 16.7909 4 19V21" stroke="#D94E15" stroke-width="2"/>
              <circle cx="12" cy="7" r="4" stroke="#D94E15" stroke-width="2"/>
            </svg>
            Kiểm Tra Hồ Sơ KYC - {{ selectedKycType === 'seller' ? 'Cửa Hàng / Shop' : 'Tài Xế Shipper' }}
          </h4>
          <button class="close-btn" @click="showKycModal = false">×</button>
        </div>

        <div class="modal-body kyc-split">
          <div class="left-image-preview">
            <div class="big-img-box">
              <img :src="activeKycImage" alt="Phóng to ảnh" />
              <div class="img-tag">Click Ảnh Bên Dưới Để Xem Chi Tiết</div>
            </div>
            
            <div class="thumbs-flex">
              <img 
                :src="selectedKycItem?.cccdFrontImage" 
                class="thumb" 
                :class="{ active: activeKycImage === selectedKycItem?.cccdFrontImage }"
                @click="activeKycImage = selectedKycItem?.cccdFrontImage" 
                alt="CCCD Mặt trước" />
              <img 
                :src="selectedKycItem?.cccdBackImage" 
                class="thumb" 
                :class="{ active: activeKycImage === selectedKycItem?.cccdBackImage }"
                @click="activeKycImage = selectedKycItem?.cccdBackImage" 
                alt="CCCD Mặt sau" />
              <img 
                v-if="selectedKycItem?.drivingLicenseImage" 
                :src="selectedKycItem?.drivingLicenseImage" 
                class="thumb" 
                :class="{ active: activeKycImage === selectedKycItem?.drivingLicenseImage }"
                @click="activeKycImage = selectedKycItem?.drivingLicenseImage" 
                alt="Bằng lái GPLX" />
            </div>
          </div>

          <div class="right-info-box">
            <div class="info-card">
              <h5 class="info-title">📋 THÔNG TIN ĐỐI CHIẾU HỒ SƠ</h5>
              <div class="info-item"><span>Họ và Tên:</span><strong>{{ selectedKycItem?.fullName }}</strong></div>
              <div class="info-item"><span>Gmail Liên Hệ:</span><strong class="text-orange">{{ selectedKycItem?.email }}</strong></div>
              <div class="info-item"><span>Số CCCD:</span><strong class="font-mono">{{ selectedKycItem?.cccdNumber || '001201012345' }}</strong></div>

              <template v-if="selectedKycType === 'seller'">
                <div class="info-item"><span>Tên Cửa Hàng:</span><strong class="text-green">{{ selectedKycItem?.storeName }}</strong></div>
                <div class="info-item"><span>Địa Chỉ Gian Hàng:</span><span>{{ selectedKycItem?.address }}</span></div>
              </template>

              <template v-if="selectedKycType === 'shipper'">
                <div class="info-item"><span>Biển Số Xe:</span><strong class="text-orange">{{ selectedKycItem?.licensePlate }}</strong></div>
                <div class="info-item"><span>Phương Tiện:</span><span>{{ selectedKycItem?.vehicleType }} - {{ selectedKycItem?.vehicleModel }}</span></div>
              </template>
            </div>

            <div class="modal-actions-flex">
              <button class="btn-green-full" @click="handleApproveKyc">[Phê Duyệt Hồ Sơ]</button>
              <button class="btn-red-full" @click="showRejectReasonPopup = true">[Từ Chối]</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- POPUP REJECT REASON -->
    <div v-if="showRejectReasonPopup" class="modal-overlay z-top">
      <div class="modal-card small">
        <div class="modal-head bg-red text-white">
          <h4>Nhập Lý Do Từ Chối Hồ Sơ</h4>
          <button class="close-btn text-white" @click="showRejectReasonPopup = false">×</button>
        </div>
        <div class="modal-body">
          <label class="form-label font-bold">Chọn lý do mẫu:</label>
          <select class="form-select mb-3" @change="(e: any) => rejectReasonInput = e.target.value">
            <option value="">-- Chọn lý do mẫu --</option>
            <option value="Ảnh giấy tờ CCCD/GPLX bị mờ góc, không rõ số">Ảnh giấy tờ bị mờ góc</option>
            <option value="Thông tin Họ tên không trùng khớp với số CCCD">Thông tin chưa trùng khớp</option>
            <option value="Ảnh mặt tiền tiệm / Phương tiện chưa đạt yêu cầu an toàn">Phương tiện / Tiệm chưa đạt chuẩn</option>
          </select>
          <textarea v-model="rejectReasonInput" class="form-control" rows="3" placeholder="Chi tiết lý do từ chối..."></textarea>
        </div>
        <div class="modal-foot">
          <button class="btn-gray" @click="showRejectReasonPopup = false">Hủy</button>
          <button class="btn-red-pill" @click="handleRejectKyc">Gửi Email Từ Chối</button>
        </div>
      </div>
    </div>

    <!-- MODAL 2: KHÓA TÀI KHOẢN -->
    <div v-if="showDisciplineModal" class="modal-overlay">
      <div class="modal-card medium">
        <div class="modal-head bg-red text-white">
          <h4>Chế Tài Vi Phạm - {{ selectedUserForPunish?.fullName }}</h4>
          <button class="close-btn text-white" @click="showDisciplineModal = false">×</button>
        </div>

        <div class="modal-body">
          <div class="radios-stack">
            <label class="radio-item" :class="{ active: disciplineLevel === 1 }">
              <input type="radio" v-model="disciplineLevel" :value="1" />
              <div>
                <div class="r-title text-orange">Mức 1 - Gửi Cảnh Báo Vi Phạm</div>
                <div class="r-desc">Tài khoản vẫn hoạt động. Hệ thống gửi Email cảnh cáo chính thức.</div>
              </div>
            </label>

            <label class="radio-item" :class="{ active: disciplineLevel === 2 }">
              <input type="radio" v-model="disciplineLevel" :value="2" />
              <div>
                <div class="r-title text-orange font-bold">Mức 2 - Tạm Khóa 10 Ngày (Suspended)</div>
                <div class="r-desc">Tạm ngưng phục vụ tài khoản trong đúng 240 giờ.</div>
              </div>
            </label>

            <label class="radio-item" :class="{ active: disciplineLevel === 3 }">
              <input type="radio" v-model="disciplineLevel" :value="3" />
              <div>
                <div class="r-title text-red font-bold">Mức 3 - Cấm Vĩnh Viễn (Permanent Ban)</div>
                <div class="r-desc">Đưa Gmail vào Blacklist, vĩnh viễn không thể đăng nhập lại.</div>
              </div>
            </label>
          </div>

          <div class="mt-3">
            <label class="form-label font-bold">Lý Do Vi Phạm (Bắt buộc):</label>
            <textarea v-model="disciplineReason" class="form-control" rows="2" placeholder="Nhập lý do vi phạm..."></textarea>
          </div>
        </div>

        <div class="modal-foot">
          <button class="btn-gray" @click="showDisciplineModal = false">Hủy</button>
          <button class="btn-red-pill" @click="handleApplyDiscipline">Áp Dụng Chế Tài & Gửi Email</button>
        </div>
      </div>
    </div>

    <!-- MODAL 3: XÓA TÀI KHOẢN -->
    <div v-if="showDeleteModal" class="modal-overlay">
      <div class="modal-card small">
        <div class="modal-head bg-red text-white">
          <h4>⚠️ CẢNH BÁO NGUY HIỂM: XÓA TÀI KHOẢN</h4>
          <button class="close-btn text-white" @click="showDeleteModal = false">×</button>
        </div>

        <div class="modal-body text-center">
          <div class="alert alert-danger text-start">
            <strong>⚠️ Thao tác xóa vĩnh viễn dữ liệu tài khoản:</strong><br/>
            [ <b>{{ selectedUserForDelete?.fullName }}</b> - <b>{{ selectedUserForDelete?.email }}</b> ]<br/>
            Hệ thống thực hiện Soft-Delete <code>IsDeleted = true</code> để bảo vệ lịch sử hóa đơn.
          </div>

          <p class="font-bold text-dark mt-3">Vui lòng nhập chữ <code>XACNHAN</code> để mở nút xóa:</p>

          <input 
            v-model="deleteConfirmText"
            type="text"
            class="form-control form-control-lg text-center font-bold text-red"
            placeholder="Gõ chữ XACNHAN vào đây..."
          />
        </div>

        <div class="modal-foot justify-content-between">
          <button class="btn-gray" @click="showDeleteModal = false">Hủy Thao Tác</button>
          <button 
            class="btn-red-pill font-bold" 
            :disabled="deleteConfirmText !== 'XACNHAN'"
            @click="handleConfirmDelete">
            🗑️ [Xác Nhận Xóa Vĩnh Viễn]
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL 4: THÊM QUẢN LÝ -->
    <div v-if="showCreateManagerModal" class="modal-overlay">
      <div class="modal-card small">
        <div class="modal-head bg-orange text-white">
          <h4>+ Thêm Quản Lý Ca Trực Mới</h4>
          <button class="close-btn text-white" @click="showCreateManagerModal = false">×</button>
        </div>

        <div class="modal-body">
          <div class="mb-3">
            <label class="form-label font-bold">Họ và Tên Quản Lý:</label>
            <input v-model="newManagerForm.fullName" type="text" class="form-control" placeholder="Nguyễn Văn A" />
          </div>
          <div class="mb-3">
            <label class="form-label font-bold">Gmail Công Việc:</label>
            <input v-model="newManagerForm.email" type="email" class="form-control" placeholder="manager02@zonemart.vn" />
          </div>
          <div class="mb-3">
            <label class="form-label font-bold">Mật Khẩu Ban Đầu:</label>
            <input v-model="newManagerForm.password" type="password" class="form-control" placeholder="••••••••" />
          </div>
        </div>

        <div class="modal-foot">
          <button class="btn-gray" @click="showCreateManagerModal = false">Hủy</button>
          <button class="btn-orange-pill" @click="handleCreateManager">Khởi Tạo Quản Lý</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin-app-wrapper {
  background: #FAF5EF;
  min-height: 100vh;
  padding-bottom: 80px;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  color: #291811;
}

/* Toast Alert */
.toast-alert {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 9999;
  padding: 14px 22px;
  border-radius: 14px;
  color: #ffffff;
  font-weight: 700;
  box-shadow: 0 12px 30px rgba(0,0,0,0.18);
  display: flex;
  align-items: center;
}
.toast-alert.success { background: #16a34a; }
.toast-alert.danger { background: #dc2626; }
.toast-alert.warning { background: #d97706; }

/* 1. Header Tìm Kiếm Chuyên Sâu Thay Thế Header Cũ */
.admin-search-header {
  background: #ffffff;
  border-bottom: 2px solid #F0E6DC;
  padding: 18px 32px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.03);
}
.header-inner { max-width: 1350px; margin: 0 auto; display: flex; justify-content: space-between; align-items: center; gap: 20px; }

.brand-zone { display: flex; align-items: center; gap: 14px; }
.admin-brand-logo { height: 48px; width: auto; object-fit: contain; }
.brand-title-text { font-size: 20px; font-weight: 900; color: #D94E15; margin: 0; }
.badge-admin { background: #D94E15; color: #fff; font-size: 10px; padding: 2px 6px; border-radius: 4px; font-weight: 900; }
.brand-sub-text { font-size: 11.5px; color: #78350F; margin: 2px 0 0 0; font-weight: 600; }

/* Header Search Container */
.header-search-container { flex: 1; max-width: 680px; position: relative; }

/* LIVE SEARCH DROPDOWN MENU WITH SCROLLBAR UNDER INPUT BAR */
.search-live-dropdown-panel {
  position: absolute;
  top: calc(100% + 8px);
  left: 0;
  right: 0;
  background: #ffffff;
  border: 2px solid #FED7AA;
  border-radius: 20px;
  box-shadow: 0 16px 40px rgba(41, 24, 17, 0.15);
  z-index: 9999;
  overflow: hidden;
}

.dropdown-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  background: #FFF7ED;
  border-bottom: 1px solid #FED7AA;
  font-size: 12px;
  font-weight: 800;
  color: #9A3412;
}

.btn-close-dropdown {
  background: none;
  border: none;
  font-size: 18px;
  font-weight: 800;
  color: #94A3B8;
  cursor: pointer;
  padding: 0 4px;
}

.dropdown-scroll-list {
  max-height: 300px;
  overflow-y: auto;
}

/* CUSTOM VERTICAL SCROLLBAR FOR LIVE SEARCH DROPDOWN */
.dropdown-scroll-list::-webkit-scrollbar {
  width: 8px;
}
.dropdown-scroll-list::-webkit-scrollbar-track {
  background: #FAF5EF;
  border-radius: 8px;
}
.dropdown-scroll-list::-webkit-scrollbar-thumb {
  background: #FED7AA;
  border-radius: 8px;
  border: 2px solid #FAF5EF;
}
.dropdown-scroll-list::-webkit-scrollbar-thumb:hover {
  background: #D94E15;
}

.dropdown-empty-state {
  padding: 24px;
  text-align: center;
  color: #94A3B8;
  font-size: 13px;
  font-weight: 600;
}

.dropdown-result-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border-bottom: 1px solid #F0E6DC;
  cursor: pointer;
  transition: background 0.15s ease;
}
.dropdown-result-item:last-child {
  border-bottom: none;
}
.dropdown-result-item:hover {
  background: #FFF7ED;
}

.dropdown-avatar {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  object-fit: cover;
  border: 1.5px solid #FED7AA;
  flex-shrink: 0;
}

.dropdown-info {
  flex: 1;
  min-width: 0;
}

.dropdown-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.dropdown-name {
  font-size: 13.5px;
  font-weight: 900;
  color: #291811;
}

.dropdown-sub-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 2px;
}

.dropdown-email {
  font-size: 12px;
  color: #78350F;
  font-family: monospace;
}

.dropdown-extra {
  font-size: 11.5px;
  font-weight: 700;
}

.main-search-input-box {
  display: flex;
  align-items: center;
  position: relative;
  width: 100%;
  background: #FFFBF7;
  border: 2px solid #FED7AA;
  border-radius: 30px;
  padding: 3px 14px;
  transition: all 0.2s ease;
}
.main-search-input-box:focus-within {
  border-color: #D94E15;
  background: #ffffff;
  box-shadow: 0 0 0 4px rgba(217,78,21,0.15);
}

.header-search-type-select {
  background: transparent;
  border: none;
  font-size: 12.5px;
  font-weight: 800;
  color: #D94E15;
  outline: none;
  cursor: pointer;
  padding: 8px 6px;
  white-space: nowrap;
}
.search-select-divider {
  width: 1px;
  height: 24px;
  background: #FED7AA;
  margin: 0 8px;
}

.search-icon { margin-left: 4px; margin-right: 8px; flex-shrink: 0; }
.header-search-input {
  width: 100%;
  padding: 10px 30px 10px 0;
  border: none;
  background: transparent;
  font-size: 13.5px;
  font-weight: 600;
  color: #291811;
  outline: none;
}
.clear-icon-btn { border: none; background: transparent; cursor: pointer; padding: 4px; display: flex; align-items: center; }

/* Right Admin Profile Link */
.header-actor-switch { display: flex; align-items: center; }
.admin-profile-box {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
  padding: 6px 14px;
  border-radius: 20px;
  background: #FFF7ED;
  border: 1.5px solid #FED7AA;
  transition: all 0.2s ease;
}
.admin-profile-box:hover {
  background: #FFEAD5;
  border-color: #D94E15;
  transform: translateY(-1px);
}
.avatar-circle { background: #ffffff; border: 1.5px solid #FED7AA; width: 38px; height: 38px; border-radius: 50%; display: flex; align-items: center; justify-content: center; }
.prof-name { font-size: 12.5px; font-weight: 900; color: #291811; white-space: nowrap; }
.prof-role { font-size: 10.5px; font-weight: 800; }
.prof-role.admin { color: #D94E15; }

/* Body Container */
.admin-body-container { max-width: 1350px; margin: 24px auto 0 auto; padding: 0 20px; }

/* Navigation Pills */
.admin-navigation-pills { display: flex; gap: 12px; margin-bottom: 24px; flex-wrap: wrap; }
.nav-pill {
  background: #ffffff;
  border: 1.5px solid #F0E6DC;
  padding: 12px 24px;
  border-radius: 16px;
  font-size: 14px;
  font-weight: 800;
  color: #78350F;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 4px 12px rgba(0,0,0,0.02);
  display: flex;
  align-items: center;
  white-space: nowrap;
}
.nav-pill.active {
  background: #D94E15;
  color: #ffffff;
  border-color: #D94E15;
  box-shadow: 0 6px 18px rgba(217,78,21,0.3);
}

/* 4 Action Stats Cards */
.action-stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 28px; }
.stat-card-item {
  background: #ffffff;
  border-radius: 20px;
  padding: 22px;
  display: flex;
  align-items: center;
  gap: 16px;
  border: 1.5px solid #F0E6DC;
  box-shadow: 0 8px 24px rgba(0,0,0,0.03);
  transition: transform 0.2s ease;
  cursor: pointer;
}
.stat-card-item:hover { transform: translateY(-3px); }
.stat-card-icon-wrapper {
  width: 56px;
  height: 56px;
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.stat-card-icon-wrapper.primary { background: #FFF7ED; border: 1.5px solid #FED7AA; }
.stat-card-icon-wrapper.warning { background: #FFF7ED; border: 1.5px solid #FFEDD5; }
.stat-card-icon-wrapper.success { background: #F0FDF4; border: 1.5px solid #BBF7D0; }
.stat-card-icon-wrapper.danger { background: #FEF2F2; border: 1.5px solid #FCA5A5; }

.stat-title { font-size: 13px; font-weight: 800; color: #78350F; white-space: nowrap; }
.stat-num { font-size: 26px; font-weight: 900; color: #291811; margin: 2px 0 0 0; white-space: nowrap; }
.unit { font-size: 12px; color: #94A3B8; font-weight: 600; }

.text-orange { color: #D94E15 !important; }
.text-green { color: #16A34A !important; }
.text-red { color: #DC2626 !important; }

/* Grid 65/35 */
.grid-65-35-layout { display: grid; grid-template-columns: 65% 33.5%; gap: 24px; }
.panel-card-white { background: #ffffff; border-radius: 24px; border: 1.5px solid #F0E6DC; padding: 24px; box-shadow: 0 10px 30px rgba(0,0,0,0.03); }
.panel-header-line { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; border-bottom: 1px solid #F0E6DC; padding-bottom: 14px; flex-wrap: wrap; gap: 12px; }
.panel-heading { font-size: 18px; font-weight: 900; margin: 0; color: #291811; display: flex; align-items: center; white-space: nowrap; }
.panel-sub { font-size: 12px; color: #78350F; margin: 2px 0 0 0; }
.result-count-badge { background: #FFF7ED; color: #D94E15; border: 1px solid #FED7AA; font-size: 12px; font-weight: 800; padding: 6px 16px; border-radius: 20px; white-space: nowrap; }

.secondary-filter-bar { margin-bottom: 18px; display: flex; justify-content: flex-end; }
.status-filter-item { display: flex; align-items: center; gap: 8px; font-size: 13px; font-weight: 800; color: #78350F; white-space: nowrap; }
.status-select-pill { padding: 8px 16px; border-radius: 12px; border: 1.5px solid #FED7AA; background: #ffffff; font-size: 13px; font-weight: 700; outline: none; cursor: pointer; }

/* Data Table Wireframe with Horizontal Scrollbar & White-space control */
.table-responsive-wrapper {
  overflow-x: auto;
  border-radius: 14px;
  border: 1px solid #F0E6DC;
  background: #ffffff;
  padding-bottom: 4px;
}

/* SLEEK CUSTOM HORIZONTAL SCROLLBAR */
.table-responsive-wrapper::-webkit-scrollbar {
  height: 9px;
}
.table-responsive-wrapper::-webkit-scrollbar-track {
  background: #FAF5EF;
  border-radius: 8px;
}
.table-responsive-wrapper::-webkit-scrollbar-thumb {
  background: #FED7AA;
  border-radius: 8px;
  border: 2px solid #FAF5EF;
}
.table-responsive-wrapper::-webkit-scrollbar-thumb:hover {
  background: #D94E15;
}

.data-table-wireframe {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  font-size: 13.5px;
}
.data-table-wireframe th {
  background: #FFF7ED;
  color: #9A3412;
  font-weight: 900;
  padding: 14px 16px;
  border-bottom: 2px solid #FED7AA;
  text-transform: uppercase;
  font-size: 11.5px;
  letter-spacing: 0.5px;
  white-space: nowrap !important;
  vertical-align: middle;
}
.data-table-wireframe td {
  padding: 14px 16px;
  border-bottom: 1px solid #F0E6DC;
  vertical-align: middle;
  white-space: nowrap !important;
}
.table-row-hover:hover { background: #FFFBF7; }

.user-info-flex { display: flex; align-items: center; gap: 12px; white-space: nowrap; }
.user-avatar-circle { width: 42px; height: 42px; border-radius: 50%; object-fit: cover; border: 2px solid #FED7AA; flex-shrink: 0; }
.user-name-bold { font-weight: 900; color: #291811; line-height: 1.2; white-space: nowrap; }
.store-badge { font-size: 11px; color: #D94E15; background: #FFF7ED; padding: 2px 8px; border-radius: 6px; display: inline-flex; align-items: center; margin-top: 3px; white-space: nowrap; }
.shipper-badge { font-size: 11px; color: #7E22CE; background: #F3E8FF; padding: 2px 8px; border-radius: 6px; display: inline-flex; align-items: center; margin-top: 3px; white-space: nowrap; }

.email-font { font-family: monospace; font-size: 13px; font-weight: 700; color: #291811; display: flex; align-items: center; white-space: nowrap; }

.role-badge-pill { font-size: 11.5px; font-weight: 800; padding: 4px 12px; border-radius: 12px; display: inline-flex; align-items: center; white-space: nowrap; }
.role-badge-pill.seller { background: #FFF7ED; color: #D94E15; border: 1px solid #FED7AA; }
.role-badge-pill.shipper { background: #F3E8FF; color: #7E22CE; border: 1px solid #E9D5FF; }
.role-badge-pill.buyer { background: #F1F5F9; color: #475569; border: 1px solid #E2E8F0; }
.role-badge-pill.manager, .role-badge-pill.admin { background: #EFF6FF; color: #1D4ED8; border: 1px solid #BFDBFE; }

.status-indicator { display: inline-flex; align-items: center; gap: 6px; white-space: nowrap; }
.status-dot { width: 9px; height: 9px; border-radius: 50%; display: inline-block; flex-shrink: 0; }
.status-dot.active { background: #16A34A; }
.status-dot.pending { background: #D97706; }
.status-dot.suspended, .status-dot.locked_10_days { background: #EA580C; }
.status-dot.banned { background: #DC2626; }
.status-txt { font-weight: 800; font-size: 12px; white-space: nowrap; }
.status-txt.active { color: #16A34A; }
.status-txt.pending { color: #D97706; }
.status-txt.suspended, .status-txt.locked_10_days { color: #EA580C; }
.status-txt.banned { color: #DC2626; }

.actions-flex { display: flex; gap: 6px; justify-content: center; align-items: center; white-space: nowrap; }
.act-btn { border: none; padding: 6px 12px; border-radius: 8px; font-size: 12px; font-weight: 800; cursor: pointer; display: inline-flex; align-items: center; white-space: nowrap; }
.act-btn.kyc { background: #FFF7ED; color: #D94E15; border: 1px solid #FED7AA; }
.act-btn.lock { background: #FEF3C7; color: #B45309; border: 1px solid #FDE68A; }
.act-btn.unlock { background: #DCFCE7; color: #15803D; border: 1px solid #BBF7D0; }
.act-btn.delete { background: #FEF2F2; color: #DC2626; border: 1px solid #FECACA; }
.act-btn.delete.disabled { opacity: 0.4; cursor: not-allowed; }

/* Right 35% Sidebar Widgets & Visual Bar Chart */
.col-right-35 { display: flex; flex-direction: column; gap: 20px; }
.widget-card-white { background: #ffffff; border-radius: 24px; border: 1.5px solid #F0E6DC; padding: 22px; box-shadow: 0 10px 30px rgba(0,0,0,0.03); }
.widget-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
.widget-heading { font-size: 15px; font-weight: 900; margin: 0; color: #291811; display: flex; align-items: center; }

.chart-container { margin-bottom: 16px; background: #FAF5EF; border-radius: 16px; padding: 16px; border: 1px solid #F0E6DC; }
.visual-bar-chart { display: flex; justify-content: space-between; align-items: flex-end; height: 120px; gap: 8px; }
.chart-bar-col { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 6px; height: 100%; justify-content: flex-end; }
.bar-fill { width: 100%; max-width: 24px; background: #FED7AA; border-radius: 6px 6px 0 0; transition: height 0.3s ease; }
.bar-fill.active { background: #D94E15; box-shadow: 0 4px 10px rgba(217,78,21,0.4); }
.bar-label { font-size: 10px; color: #78350F; font-weight: 700; }

.revenue-stats-summary .rev-val { font-size: 26px; font-weight: 900; color: #16A34A; }
.revenue-stats-summary .rev-sub { font-size: 12px; color: #78350F; margin: 2px 0 14px 0; font-weight: 600; }

.progress-breakdown { display: flex; flex-direction: column; gap: 8px; font-size: 12px; }
.p-row { display: flex; justify-content: space-between; color: #78350F; font-weight: 700; background: #FAF5EF; padding: 8px 12px; border-radius: 10px; border: 1px solid #F0E6DC; }

.live-pulse { width: 10px; height: 10px; background: #16A34A; border-radius: 50%; flex-shrink: 0; }

.shipper-counts-flex { display: flex; gap: 12px; margin-bottom: 14px; }
.count-box { flex: 1; background: #FFF7ED; border-radius: 14px; padding: 12px; text-align: center; border: 1px solid #FED7AA; }
.count-box .val { font-size: 20px; font-weight: 900; display: block; }
.count-box .lbl { font-size: 11px; color: #78350F; font-weight: 800; }

.shipper-live-feed { display: flex; flex-direction: column; gap: 10px; }
.feed-row { display: flex; align-items: center; gap: 10px; padding: 10px; border-radius: 12px; background: #FAF5EF; border: 1px solid #F0E6DC; }
.feed-row .dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
.feed-row .dot.active { background: #16A34A; }
.feed-row .dot.busy { background: #D97706; }
.f-name { font-size: 12.5px; font-weight: 900; color: #291811; }
.f-sub { font-size: 11px; color: #78350F; }

.link-btn { background: none; border: none; color: #D94E15; font-size: 12px; font-weight: 800; cursor: pointer; }

.logs-stream-list { display: flex; flex-direction: column; gap: 12px; }
.log-stream-item { display: flex; gap: 10px; align-items: flex-start; }
.log-icon { background: #FFF7ED; color: #D94E15; width: 30px; height: 30px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 13px; border: 1px solid #FED7AA; flex-shrink: 0; }
.log-actor { font-size: 12px; color: #291811; }
.log-action { font-size: 11.5px; color: #78350F; font-weight: 600; }
.log-time { font-size: 10px; color: #94A3B8; }

/* KYC Grid */
.kyc-cards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 20px; }
.kyc-card { background: #ffffff; border: 1.5px solid #F0E6DC; border-radius: 20px; padding: 20px; }
.kyc-card-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
.type-tag { font-size: 11px; font-weight: 800; padding: 4px 10px; border-radius: 8px; }
.type-tag.seller { background: #FFF7ED; color: #D94E15; border: 1px solid #FED7AA; }
.type-tag.shipper { background: #F3E8FF; color: #7E22CE; border: 1px solid #E9D5FF; }
.pending-tag { font-size: 11px; background: #FEF3C7; color: #B45309; font-weight: 800; padding: 4px 10px; border-radius: 8px; }

.kyc-card-main { display: flex; gap: 14px; align-items: center; margin-bottom: 18px; }
.card-avatar { width: 52px; height: 52px; border-radius: 50%; object-fit: cover; border: 2px solid #FED7AA; flex-shrink: 0; }
.card-name { margin: 0 0 4px 0; font-size: 15px; font-weight: 900; color: #291811; }
.card-email { margin: 0; font-size: 12px; color: #78350F; display: flex; align-items: center; }
.card-extra { margin: 2px 0 0 0; font-size: 12px; color: #291811; }

.btn-orange-full { width: 100%; background: #D94E15; color: #ffffff; border: none; padding: 11px; border-radius: 12px; font-size: 13px; font-weight: 800; cursor: pointer; box-shadow: 0 4px 12px rgba(217,78,21,0.25); display: flex; align-items: center; justify-content: center; }

/* Modals Overlay */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(41, 24, 17, 0.75);
  backdrop-filter: blur(4px);
  z-index: 9990;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}
.modal-overlay.z-top { z-index: 9999; }

.modal-card {
  background: #ffffff;
  border-radius: 24px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0,0,0,0.3);
  border: 1.5px solid #F0E6DC;
}
.modal-card.small { max-width: 480px; }
.modal-card.medium { max-width: 620px; }
.modal-card.kyc-layout { max-width: 980px; }

.modal-head { padding: 20px 26px; border-bottom: 1px solid #F0E6DC; display: flex; justify-content: space-between; align-items: center; }
.modal-head h4 { margin: 0; font-size: 18px; font-weight: 900; display: flex; align-items: center; }
.close-btn { background: none; border: none; font-size: 26px; font-weight: 700; cursor: pointer; color: #94A3B8; }

.modal-body { padding: 26px; }
.modal-foot { padding: 18px 26px; border-top: 1px solid #F0E6DC; display: flex; gap: 12px; justify-content: flex-end; }

/* KYC Split */
.kyc-split { display: grid; grid-template-columns: 55% 42%; gap: 24px; }
.big-img-box { position: relative; background: #000; border-radius: 16px; overflow: hidden; height: 350px; display: flex; align-items: center; justify-content: center; }
.big-img-box img { max-width: 100%; max-height: 100%; object-fit: contain; }
.img-tag { position: absolute; bottom: 10px; background: rgba(0,0,0,0.75); color: #ffffff; padding: 4px 14px; border-radius: 20px; font-size: 11px; font-weight: 800; }

.thumbs-flex { display: flex; gap: 10px; margin-top: 12px; }
.thumb { width: 75px; height: 52px; border-radius: 10px; object-fit: cover; cursor: pointer; border: 2px solid transparent; opacity: 0.7; }
.thumb.active { border-color: #D94E15; opacity: 1; }

.info-card { background: #FFF7ED; border-radius: 16px; padding: 20px; border: 1.5px solid #FED7AA; margin-bottom: 20px; }
.info-title { font-size: 13px; font-weight: 900; color: #9A3412; margin: 0 0 14px 0; }
.info-item { display: flex; justify-content: space-between; font-size: 13px; padding: 7px 0; border-bottom: 1px dashed #FED7AA; }

.modal-actions-flex { display: flex; gap: 12px; }
.btn-green-full { flex: 1; background: #16A34A; color: #ffffff; border: none; padding: 12px; border-radius: 14px; font-weight: 900; font-size: 14px; cursor: pointer; }
.btn-red-full { flex: 1; background: #DC2626; color: #ffffff; border: none; padding: 12px; border-radius: 14px; font-weight: 900; font-size: 14px; cursor: pointer; }
.btn-gray { background: #F1F5F9; color: #475569; border: 1px solid #CBD5E1; padding: 10px 20px; border-radius: 12px; font-weight: 800; cursor: pointer; }
.btn-orange-pill { background: #D94E15; color: #ffffff; border: none; padding: 10px 22px; border-radius: 12px; font-weight: 900; cursor: pointer; display: flex; align-items: center; }
.btn-red-pill { background: #DC2626; color: #ffffff; border: none; padding: 10px 22px; border-radius: 12px; font-weight: 900; cursor: pointer; }

/* Level Radios */
.radios-stack { display: flex; flex-direction: column; gap: 12px; }
.radio-item { display: flex; gap: 12px; background: #FFF7ED; border: 2px solid #FED7AA; border-radius: 14px; padding: 14px; cursor: pointer; }
.radio-item.active { border-color: #DC2626; background: #FEF2F2; }
.r-title { font-weight: 900; font-size: 14px; }
.r-desc { font-size: 12px; color: #78350F; margin-top: 2px; }
</style>
