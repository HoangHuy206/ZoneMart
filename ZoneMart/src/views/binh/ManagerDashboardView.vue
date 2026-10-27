<script setup lang="ts">
/**
 * ================================================================
 * PHÂN HỆ BAN QUẢN LÝ CA TRỰC (MANAGER DASHBOARD) - ZONEMART
 * Phong cách thiết kế: Concept Dashboard Ban Quản Lý Cụm Chợ Số
 * Màu sắc nhận diện: Ngọc Lục Bảo (Emerald/Teal #0d9488 / #059669),
 * Nền Slate (#0f172a), Nền Sáng (#f8fafc), Xanh dương (#2563eb)
 * 100% Icon Code Lucide (Tuyệt đối không dùng emoji)
 * Quyền hạn: Quản lý khu chợ được phân công, duyệt hồ sơ, sản phẩm,
 * không can thiệp tài khoản Admin và không quản lý ca trực khác.
 * ================================================================
 */
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { useAuth } from "../../composables/useAuth";
import { apiFetch } from "../../utils/apiConfig";
import {
  LayoutDashboard,
  Users,
  ShieldCheck,
  UserCheck,
  History,
  Store,
  Bike,
  Package,
  ShoppingBag,
  MapPin,
  LogOut,
  Search,
  Bell,
  BellOff,
  CheckCheck,
  ChevronRight,
  Eye,
  Wallet,
  TrendingUp,
  Hourglass,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Trash2,
  Star,
  Sparkles,
  Activity,
  Unlock,
  X,
  Menu,
  Clock,
  Handshake,
  Info,
  CreditCard,
  PackageCheck,
  Check,
  Loader2,
  Mail,
  Phone,
  Building2
} from "lucide-vue-next";
import { useProductModeration } from "../../composables/useProductModeration";

const router = useRouter();
const auth = useAuth();

// Thông tin định danh Manager đang đăng nhập
const currentActorRole = ref<string>("Manager");
const currentActorEmail = computed(() => {
  return auth.currentUser.value?.phoneEmail || (auth.currentUser.value as any)?.email || "manager@zonemart.vn";
});
const currentActorName = computed(() => {
  return auth.currentUser.value?.fullName || "Cán Bộ Quản Lý Ca Trực";
});

// Các Tabs điều hướng của Ban Quản Lý (Không có tab "managers" theo đúng yêu cầu)
const activeTab = ref<"dashboard" | "accounts" | "kyc" | "products" | "markets" | "audit">("dashboard");

// Quản lý Drawer Menu trên Mobile
const isMobileMenuOpen = ref<boolean>(false);
const selectManagerTab = (tab: "dashboard" | "accounts" | "kyc" | "products" | "markets" | "audit", searchTarget?: any) => {
  activeTab.value = tab;
  if (searchTarget) {
    searchTargetTab.value = searchTarget;
  }
  isMobileMenuOpen.value = false;
  if (tab === "markets") {
    fetchMarketsOverview();
  }
};

// Toast notification
const toastMessage = ref<string>("");
const toastType = ref<"success" | "danger" | "warning" | "info">("success");
const showToastState = ref<boolean>(false);
const activeZoomImage = ref<string | null>(null);
let toastTimer: any = null;

const showToast = (msg: string, type: "success" | "danger" | "warning" | "info" = "success") => {
  toastMessage.value = msg;
  toastType.value = type;
  showToastState.value = true;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    showToastState.value = false;
  }, 3500);
};

// Interface biểu đồ tuần
interface ChartDay {
  dayLabel: string;
  date: string;
  revenue: number;
  orderCount: number;
  heightPercent: number;
}

const defaultWeeklyChart: ChartDay[] = [
  { dayLabel: "Thứ 2", date: "", revenue: 0, orderCount: 0, heightPercent: 10 },
  { dayLabel: "Thứ 3", date: "", revenue: 0, orderCount: 0, heightPercent: 10 },
  { dayLabel: "Thứ 4", date: "", revenue: 0, orderCount: 0, heightPercent: 10 },
  { dayLabel: "Hôm nay", date: "", revenue: 0, orderCount: 0, heightPercent: 10 },
  { dayLabel: "Thứ 6", date: "", revenue: 0, orderCount: 0, heightPercent: 10 },
  { dayLabel: "Thứ 7", date: "", revenue: 0, orderCount: 0, heightPercent: 10 },
  { dayLabel: "CN", date: "", revenue: 0, orderCount: 0, heightPercent: 10 }
];

// Stats từ MongoDB CSDL Thật
const stats = ref({
  totalUsers: 0,
  totalStores: 0,
  totalShippers: 0,
  totalProducts: 0,
  pendingShops: 0,
  pendingShippers: 0,
  lockedUsers: 0,
  totalOrders: 0,
  onlineShippers: 0,
  deliveringShippers: 0,
  totalRevenue: 0,
  shopCommission: 0,
  shipperFees: 0,
  totalViews: 0,
  chartWeekly: defaultWeeklyChart,
  activeShipperFeed: [] as any[]
});

// Accounts Data List
interface UserAccount {
  id: string;
  userCode?: string;
  email: string;
  phoneEmail?: string;
  fullName: string;
  avatarUrl?: string;
  primaryRole: string;
  isBuyer: boolean;
  isSeller: boolean;
  isShipper: boolean;
  isAdmin: boolean;
  isManager: boolean;
  accountStatus: string;
  lockUntil?: string;
  violationCount: number;
  createdAt: string;
  cccdNumber?: string;
  storeDetails?: any;
  shipperDetails?: any;
  permissions?: string[];
}

const users = ref<UserAccount[]>([]);
const auditLogs = ref<any[]>([]);

// Header search state
const searchQuery = ref<string>("");
const searchTargetTab = ref<"all" | "buyer" | "seller" | "shipper" | "manager">("all");
const selectedStatusFilter = ref<string>("All");

// KYC Approval Modal
const showKycModal = ref<boolean>(false);
const selectedKycItem = ref<any>(null);
const selectedKycType = ref<"seller" | "shipper">("seller");
const activeKycImage = ref<string>("");
const activeDocTab = ref<"front" | "back" | "cert" | "license">("front");
const rejectReasonInput = ref<string>("");
const showRejectReasonPopup = ref<boolean>(false);
const isSubmittingReject = ref<boolean>(false);

const presetRejectReasons = computed(() => {
  if (selectedKycType.value === "seller") {
    return [
      "Ảnh CCCD bị mờ, lóa sáng hoặc mất góc",
      "Thiếu Giấy chứng nhận An toàn thực phẩm (ATVSTP)",
      "Họ tên / Số CCCD không trùng khớp tài liệu",
      "Giấy tờ tải lên đã hết hạn hiệu lực",
      "Hình ảnh chụp nghi ngờ bị chỉnh sửa hoặc cắt ghép"
    ];
  } else {
    return [
      "Ảnh CCCD bị mờ, lóa sáng hoặc mất góc",
      "Giấy phép lái xe (GPLX) không hợp lệ hoặc hết hạn",
      "Biển số xe / Cà vẹt xe không rõ ràng hoặc không khớp",
      "Họ tên / Số CCCD không trùng khớp hồ sơ",
      "Hình ảnh chụp nghi ngờ bị chỉnh sửa hoặc cắt ghép"
    ];
  }
});

const applyPresetReason = (reason: string) => {
  if (!rejectReasonInput.value.trim()) {
    rejectReasonInput.value = reason;
  } else if (!rejectReasonInput.value.includes(reason)) {
    rejectReasonInput.value += ` • ${reason}`;
  }
};

const isOcrScanning = ref<boolean>(false);
const ocrExtractedSuccess = ref<boolean>(false);

// Discipline & Deletion modals
const showDisciplineModal = ref<boolean>(false);
const selectedUserForPunish = ref<UserAccount | null>(null);
const disciplineLevel = ref<number>(1);
const disciplineReason = ref<string>("");

const showDeleteModal = ref<boolean>(false);
const selectedUserForDelete = ref<UserAccount | null>(null);
const deleteConfirmText = ref<string>("");

// ================================================================
// QUẢN LÝ KHU VỰC & CHỢ SỐ PHỤ TRÁCH (CHỈ XEM CHỢ ĐƯỢC PHÂN CÔNG)
// ================================================================
const DEFAULT_INITIAL_MARKET = {
  id: "mkt_default_01",
  marketCode: "MKT_A",
  marketName: "Chợ A (Chợ Nhà Xanh)",
  wardName: "Phường Dịch Vọng Hậu",
  districtName: "Quận Cầu Giấy",
  managementBoardName: "Ban Quản Lý Cụm Chợ Số 01",
  boardLeader: "Quản Lý Ca Trực BQL",
  boardPhone: "kv3@gmail.com",
  address: "Phố Phan Văn Trường, Dịch Vọng Hậu, Cầu Giấy",
  totalStalls: 120,
  status: "Active",
  stores: [
    {
      storeId: "st_01",
      storeCode: "SEL-01",
      storeName: "Nông Sản Sạch Ba Vì",
      ownerFullName: "Bùi Đình Bình",
      phoneEmail: "mai.tran@zonemart.vn",
      stallNumber: "Sạp A12 - Khu Rau Củ",
      category: "Rau củ hữu cơ & Nông sản tươi",
      status: "Approved",
      openHours: "07:00 - 22:00",
      bankName: "Vietcombank (VCB)",
      address: "Khu A - Chợ Nhà Xanh, Cầu Giấy"
    },
    {
      storeId: "st_02",
      storeCode: "SEL-02",
      storeName: "Fresh Fruits Shop",
      ownerFullName: "Trần Thị Mai",
      phoneEmail: "freshfruits@zonemart.vn",
      stallNumber: "Sạp B05 - Trái Cây",
      category: "Trái cây 4 mùa VietGAP",
      status: "Approved",
      openHours: "07:00 - 22:00",
      bankName: "Techcombank",
      address: "Khu B - Chợ Nhà Xanh, Cầu Giấy"
    }
  ]
};

const loadInitialMarkets = (): any[] => {
  try {
    const saved = localStorage.getItem("zonemart_cached_markets_overview");
    if (saved) {
      const parsed = JSON.parse(saved);
      if (Array.isArray(parsed) && parsed.length > 0) return parsed;
    }
  } catch {}
  return [DEFAULT_INITIAL_MARKET];
};

const marketsOverview = ref<any[]>(loadInitialMarkets());
const isLoadingMarkets = ref<boolean>(false);
const totalWardsCount = ref<number>(1);
const totalBoardsCount = ref<number>(1);

// Chợ được phân công quản lý cho tài khoản Manager này (Tối ưu hóa tức thì 0ms, không lag)
const assignedMarket = computed(() => {
  const curUser = auth.currentUser.value;
  if (!marketsOverview.value || marketsOverview.value.length === 0) return DEFAULT_INITIAL_MARKET;
  if (!curUser) return marketsOverview.value[0] || DEFAULT_INITIAL_MARKET;

  const curId = (curUser.id || "").toLowerCase().trim();
  const curEmail = (curUser.phoneEmail || (curUser as any).email || "").toLowerCase().trim();
  const curName = (curUser.fullName || "").toLowerCase().trim();
  const curPhone = ((curUser as any).phone || "").toLowerCase().trim();

  // Khớp chợ theo ID Ban Quản Lý, Email/Số điện thoại trưởng ban, hoặc Tên Ban Quản Lý
  const found = marketsOverview.value.find((m: any) => {
    const mBId = (m.managementBoardId || "").toLowerCase().trim();
    const mBName = (m.managementBoardName || "").toLowerCase().trim();
    const mLeader = (m.boardLeader || "").toLowerCase().trim();
    const mPhone = (m.boardPhone || "").toLowerCase().trim();

    return (
      (curId && mBId === curId) ||
      (curEmail && (mPhone === curEmail || mBName.includes(curEmail) || mLeader.includes(curEmail))) ||
      (curPhone && mPhone === curPhone) ||
      (curName && (mBName.includes(curName) || mLeader.includes(curName)))
    );
  });

  // Fallback an toàn: nếu tài khoản quản lý mới chưa gán chợ cụ thể, hiển thị chợ mặc định
  return found || marketsOverview.value[0] || DEFAULT_INITIAL_MARKET;
});

const isMarketActive = (m: any): boolean => {
  if (!m) return false;
  const st = (m.status || "").trim().toLowerCase();
  return st === "active" || st === "hoạt động" || st === "đang hoạt động";
};

// Helper kiểm tra tài khoản chưa được duyệt hoặc bị từ chối
const isPendingOrUnapproved = (u: UserAccount) => {
  if (u.accountStatus && (u.accountStatus.toLowerCase() === "pending" || u.accountStatus.toLowerCase() === "rejected")) return true;
  if (u.storeDetails && (u.storeDetails.status === "Pending" || u.storeDetails.status === "pending" || u.storeDetails.status === "Rejected" || u.storeDetails.status === "rejected")) return true;
  if (u.shipperDetails && (u.shipperDetails.status === "Pending" || u.shipperDetails.status === "pending" || u.shipperDetails.status === "Rejected" || u.shipperDetails.status === "rejected")) return true;
  return false;
};

// Helper nhận biết tài khoản Quản trị viên cấp cao (Admin) để ngăn chặn can thiệp
const isSuperAdminAccount = (u: UserAccount | any): boolean => {
  if (!u) return false;
  const r = (u.primaryRole || "").toLowerCase().trim();
  const e = (u.email || u.phoneEmail || "").toLowerCase().trim();
  return u.isAdmin === true || r === "admin" || r === "administrator" || e === "admin@zonemart.vn";
};

// Computed Metrics
const buyerCount = computed(() => users.value.filter(u => u.isBuyer && !u.isSeller && !u.isShipper && !u.isAdmin && !isPendingOrUnapproved(u)).length);
const sellerCount = computed(() => users.value.filter(u => u.isSeller && !isPendingOrUnapproved(u)).length);
const shipperCount = computed(() => users.value.filter(u => u.isShipper && !isPendingOrUnapproved(u)).length);
const managerAndAdminCount = computed(() => users.value.filter(u => (u.isManager || u.isAdmin) && !isPendingOrUnapproved(u)).length);
const pendingCount = computed(() => stats.value.pendingShops + stats.value.pendingShippers);
const totalUserCount = computed(() => stats.value.totalUsers || users.value.length || 18);

// Product Moderation Composable (Kiểm duyệt & Quản lý sản phẩm)
const productModeration = useProductModeration();
const { 
  allProducts, 
  activeProducts, 
  pendingReviewProducts, 
  rejectedProducts, 
  approveProductByManager, 
  rejectProductByManager, 
  fetchRealProductsFromBackend
} = productModeration;

const pendingProductsCount = computed(() => pendingReviewProducts.value.length);
const totalProductsCount = computed(() => allProducts.value.length);
const selectedProductFilterTab = ref<"all" | "active" | "pending" | "rejected">("all");
const productFilterSearchQuery = ref<string>("");

const displayedProducts = computed(() => {
  let list = allProducts.value || [];
  if (selectedProductFilterTab.value === "active") {
    list = list.filter(p => p.status === "active");
  } else if (selectedProductFilterTab.value === "pending") {
    list = list.filter(p => p.status === "pending_review");
  } else if (selectedProductFilterTab.value === "rejected") {
    list = list.filter(p => p.status === "rejected_need_edit" || p.status === "deleted_violation");
  }

  if (productFilterSearchQuery.value.trim()) {
    const q = productFilterSearchQuery.value.toLowerCase().trim();
    list = list.filter(p =>
      (p.name && p.name.toLowerCase().includes(q)) ||
      (p.storeName && p.storeName.toLowerCase().includes(q)) ||
      (p.category && p.category.toLowerCase().includes(q)) ||
      (p.sellerEmail && p.sellerEmail.toLowerCase().includes(q))
    );
  }
  return list;
});

const showRejectProductModal = ref<boolean>(false);
const selectedProductToReject = ref<any>(null);
const productRejectReason = ref<string>("");
const isSubmittingRejectProduct = ref<boolean>(false);

const openRejectProductModal = (prod: any) => {
  selectedProductToReject.value = prod;
  productRejectReason.value = "";
  showRejectProductModal.value = true;
};

const handleApproveProductItem = async (prod: any) => {
  const ok = await approveProductByManager(prod.id);
  if (ok) {
    showToast(`Đã phê duyệt mở bán sản phẩm "${prod.name}" thành công!`, "success");
    const newLog = {
      timestamp: "Vừa xong",
      actorEmail: currentActorEmail.value,
      actorRole: currentActorRole.value,
      action: "Phê duyệt sản phẩm",
      targetUser: prod.storeName || prod.sellerEmail || "Gian hàng",
      details: `Ban Quản Lý duyệt mở bán sản phẩm [${prod.name}] (Giá: ${Number(prod.price).toLocaleString('vi-VN')} ₫)`
    };
    saveLocalManagerLog(newLog);
    auditLogs.value.unshift(newLog);
  } else {
    showToast(`Không thể duyệt sản phẩm. Vui lòng thử lại!`, "danger");
  }
};

const handleConfirmRejectProduct = async () => {
  if (!selectedProductToReject.value) return;
  const reason = productRejectReason.value.trim() || "Chưa đạt tiêu chuẩn an toàn thực phẩm hoặc vi phạm quy định sàn.";
  isSubmittingRejectProduct.value = true;
  try {
    const ok = await rejectProductByManager(selectedProductToReject.value.id, reason);
    if (ok) {
      showToast(`Đã từ chối sản phẩm "${selectedProductToReject.value.name}"!`, "warning");
      const newLog = {
        timestamp: "Vừa xong",
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value,
        action: "Từ chối sản phẩm",
        targetUser: selectedProductToReject.value.storeName || selectedProductToReject.value.sellerEmail || "Gian hàng",
        details: `Ban Quản Lý từ chối sản phẩm [${selectedProductToReject.value.name}]. Lý do: ${reason}`
      };
      saveLocalManagerLog(newLog);
      auditLogs.value.unshift(newLog);
      showRejectProductModal.value = false;
    } else {
      showToast("Không thể từ chối sản phẩm!", "danger");
    }
  } finally {
    isSubmittingRejectProduct.value = false;
  }
};

// Donut Gauge SVG logic CSDL Thật
const buyerStrokeDash = computed(() => {
  const total = totalUserCount.value || 1;
  const bCount = buyerCount.value || 1;
  const percent = Math.min(Math.round((bCount / total) * 100), 100);
  const circumference = 2 * Math.PI * 62;
  const fillLength = (percent / 100) * circumference;
  return `${fillLength} ${circumference}`;
});

const buyerRatioPercent = computed(() => {
  const total = totalUserCount.value || 1;
  const bCount = buyerCount.value || 1;
  return Math.min(Math.round((bCount / total) * 100), 100);
});

// Top locations
const topLocations = computed(() => {
  const counts: Record<string, number> = {};
  (marketsOverview.value || []).forEach((m: any) => {
    const dist = (m.districtName || "Cầu Giấy").replace(/^Quận\s+/i, "").trim();
    const stCount = (m.stores || []).length;
    counts[dist] = (counts[dist] || 0) + stCount;
  });

  const entries = Object.entries(counts);
  if (entries.length === 0) {
    return [
      { name: "Cầu Giấy", count: 0, width: "30%" },
      { name: "Đống Đa", count: 0, width: "20%" },
      { name: "Ba Đình", count: 0, width: "20%" }
    ];
  }
  const maxVal = Math.max(...entries.map(e => e[1]), 1);
  return entries
    .sort((a, b) => b[1] - a[1])
    .slice(0, 5)
    .map(([name, count]) => ({
      name,
      count,
      width: `${Math.max(Math.round((count / maxVal) * 100), 18)}%`
    }));
});

// Filter users list
const filteredUsersList = computed(() => {
  let list = users.value || [];

  if (selectedStatusFilter.value === "rejected") {
    list = list.filter(u => 
      u.accountStatus === "rejected" || 
      u.storeDetails?.status === "Rejected" || 
      u.shipperDetails?.status === "Rejected"
    );
  } else if (selectedStatusFilter.value === "pending") {
    list = list.filter(u => 
      u.accountStatus === "pending" || 
      u.storeDetails?.status === "Pending" || 
      u.shipperDetails?.status === "Pending"
    );
  } else if (selectedStatusFilter.value === "active") {
    list = list.filter(u => !isPendingOrUnapproved(u) && u.accountStatus === "active");
  } else if (selectedStatusFilter.value === "suspended") {
    list = list.filter(u => u.accountStatus === "locked_10_days" || u.accountStatus === "suspended");
  } else if (selectedStatusFilter.value === "banned") {
    list = list.filter(u => u.accountStatus === "banned");
  } else {
    list = list.filter(u => !isPendingOrUnapproved(u));
  }

  // Filter theo Target Tab
  if (searchTargetTab.value === "buyer") {
    list = list.filter(u => u.isBuyer && !u.isSeller && !u.isShipper && !u.isAdmin);
  } else if (searchTargetTab.value === "seller") {
    list = list.filter(u => u.isSeller);
  } else if (searchTargetTab.value === "shipper") {
    list = list.filter(u => u.isShipper);
  } else if (searchTargetTab.value === "manager") {
    list = list.filter(u => u.isManager || u.isAdmin);
  }

  // Search filter
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(u =>
      (u.fullName && u.fullName.toLowerCase().includes(q)) ||
      (u.email && u.email.toLowerCase().includes(q)) ||
      (u.phoneEmail && u.phoneEmail.toLowerCase().includes(q)) ||
      (u.cccdNumber && u.cccdNumber.toLowerCase().includes(q)) ||
      (u.storeDetails?.storeName && u.storeDetails.storeName.toLowerCase().includes(q)) ||
      (u.shipperDetails?.licensePlate && u.shipperDetails.licensePlate.toLowerCase().includes(q))
    );
  }

  return list;
});

// Pending KYC Users
const pendingKycUsers = computed(() => {
  return users.value.filter(u =>
    u.accountStatus === "pending" ||
    (u.storeDetails && (u.storeDetails.status === "Pending" || u.storeDetails.status === "pending")) ||
    (u.shipperDetails && (u.shipperDetails.status === "Pending" || u.shipperDetails.status === "pending"))
  );
});

// Filter audit logs for Manager (Tách biệt hoàn toàn, chỉ hiển thị nhật ký của Ban Quản Lý, loại bỏ Admin)
const auditFilterType = ref<"all" | "mine" | "kyc" | "discipline" | "products">("all");
const localManagerLogs = ref<any[]>([]);

const loadLocalManagerLogs = () => {
  try {
    const saved = localStorage.getItem("zonemart_manager_audit_logs");
    if (saved) {
      localManagerLogs.value = JSON.parse(saved);
    } else {
      const initialLogs = [
        {
          timestamp: "Hôm nay 08:00",
          actorEmail: currentActorEmail.value,
          actorRole: "Ban Quản Lý",
          action: "Tiếp nhận ca trực",
          targetUser: assignedMarket.value?.marketName || "Chợ A (Chợ Nhà Xanh)",
          details: `Cán bộ [${currentActorName.value}] tiếp nhận giám sát vận hành và đối soát chợ.`
        },
        {
          timestamp: "Hôm nay 08:30",
          actorEmail: currentActorEmail.value,
          actorRole: "Ban Quản Lý",
          action: "Đối soát gian hàng",
          targetUser: "Toàn bộ sạp chợ",
          details: "Kiểm tra danh sách tiểu thương mở sạp kinh doanh trong ngày theo sơ đồ phân công."
        },
        {
          timestamp: "Hôm nay 09:15",
          actorEmail: currentActorEmail.value,
          actorRole: "Ban Quản Lý",
          action: "Rà soát sản phẩm",
          targetUser: "Sàn ZoneMart",
          details: "Đối chiếu danh mục hàng hóa niêm yết, chứng nhận an toàn thực phẩm trên hệ thống."
        }
      ];
      localManagerLogs.value = initialLogs;
      localStorage.setItem("zonemart_manager_audit_logs", JSON.stringify(initialLogs));
    }
  } catch {}
};

const saveLocalManagerLog = (newLog: any) => {
  localManagerLogs.value.unshift(newLog);
  try {
    localStorage.setItem("zonemart_manager_audit_logs", JSON.stringify(localManagerLogs.value.slice(0, 50)));
  } catch {}
};

// Toàn bộ nhật ký Ban Quản Lý (loại bỏ hoàn toàn log của Super Admin / admin@zonemart.vn)
const managerAuditLogs = computed(() => {
  const allDb = auditLogs.value || [];
  const myEmail = (currentActorEmail.value || "").toLowerCase().trim();
  
  const fromDb = allDb.filter(l => {
    const actorEmail = (l.actorEmail || "").toLowerCase().trim();
    const actorRole = (l.actorRole || "").toLowerCase().trim();
    const isSuperAdmin = actorRole === "admin" || actorRole === "administrator" || actorEmail === "admin@zonemart.vn";
    if (isSuperAdmin) return false;
    
    return actorEmail === myEmail || 
           actorRole.includes("manager") || 
           actorRole.includes("ban quản lý") || 
           actorRole.includes("quản lý") ||
           actorRole.includes("bql");
  });

  const merged = [...localManagerLogs.value, ...fromDb];
  const seen = new Set();
  return merged.filter(item => {
    const key = `${item.timestamp}_${item.action}_${item.details}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
});

const filteredAuditLogs = computed(() => {
  let list = managerAuditLogs.value || [];
  if (auditFilterType.value === "mine") {
    const myEmail = currentActorEmail.value.toLowerCase().trim();
    list = list.filter(l => (l.actorEmail || "").toLowerCase().trim() === myEmail);
  } else if (auditFilterType.value === "kyc") {
    list = list.filter(l => (l.action || "").toLowerCase().includes("kyc") || (l.action || "").toLowerCase().includes("duyệt hồ sơ") || (l.action || "").toLowerCase().includes("hồ sơ"));
  } else if (auditFilterType.value === "discipline") {
    list = list.filter(l => (l.action || "").toLowerCase().includes("kỷ luật") || (l.action || "").toLowerCase().includes("khóa"));
  } else if (auditFilterType.value === "products") {
    list = list.filter(l => (l.action || "").toLowerCase().includes("sản phẩm") || (l.action || "").toLowerCase().includes("chợ") || (l.action || "").toLowerCase().includes("gian hàng"));
  }
  return list;
});

// Fetch Stats từ Backend
const fetchStats = async () => {
  try {
    const res = await apiFetch(`/api/admin/stats?_t=${Date.now()}`);
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.stats) {
        stats.value = data.stats;
      }
    }
  } catch (err) {
    console.warn("fetchStats error:", err);
  }
};

// Fetch Users từ Backend
const fetchUsers = async () => {
  try {
    const res = await apiFetch(`/api/admin/users?_t=${Date.now()}`);
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.users) {
        users.value = data.users;
      }
    }
  } catch (err) {
    console.warn("fetchUsers error:", err);
  }
};

// Fetch Markets Overview (để lấy chợ được phân công) - Tối ưu siêu tốc với cache
const fetchMarketsOverview = async (isManualRefresh = false) => {
  // Chỉ hiển thị trạng thái loading spinner khi người dùng chủ động bấm "Làm Mới Chợ"
  if (isManualRefresh) {
    isLoadingMarkets.value = true;
  }
  try {
    const url = isManualRefresh
      ? `/api/MarketManagement/markets-overview?force=true&_t=${Date.now()}`
      : `/api/MarketManagement/markets-overview`;

    const res = await apiFetch(url);
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.markets && data.markets.length > 0) {
        const processed = (data.markets || []).map((m: any) => ({
          ...m,
          stores: (m.stores || []).filter((s: any) => {
            const st = (s.status || "").toLowerCase();
            return st === "approved" || st === "active";
          })
        }));
        marketsOverview.value = processed;
        totalWardsCount.value = data.totalWards || 1;
        totalBoardsCount.value = data.totalBoards || 1;
        try {
          localStorage.setItem("zonemart_cached_markets_overview", JSON.stringify(processed));
        } catch {}
        if (isManualRefresh) {
          showToast("Đã đồng bộ thông tin Cụm Chợ Số phụ trách từ CSDL!", "success");
        }
      }
    }
  } catch (err) {
    console.warn("fetchMarketsOverview error:", err);
    if (isManualRefresh) {
      showToast("Không thể tải dữ liệu chợ. Vui lòng thử lại!", "danger");
    }
  } finally {
    isLoadingMarkets.value = false;
  }
};

// Fetch Audit Logs từ Backend
const fetchAuditLogs = async () => {
  try {
    const res = await apiFetch("/api/admin/audit-logs");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.logs) {
        auditLogs.value = data.logs;
      }
    }
  } catch (err) {
    console.warn("fetchAuditLogs error:", err);
  }
};

// Notifications Realtime
interface AdminNotificationItem {
  id: string;
  type: "seller" | "shipper" | "kyc" | "system";
  title: string;
  message: string;
  targetUserId: string;
  targetName: string;
  isRead: boolean;
  createdAt: string;
}

const notificationsList = ref<AdminNotificationItem[]>([]);
const unreadNotificationsCount = ref<number>(0);
const isNotificationOpen = ref<boolean>(false);
let notifPollInterval: any = null;

const fetchNotifications = async () => {
  try {
    const res = await apiFetch(`/api/admin/notifications?_t=${Date.now()}`);
    if (res.ok) {
      const data = await res.json();
      if (data.success) {
        notificationsList.value = data.notifications || [];
        unreadNotificationsCount.value = data.unreadCount || 0;
      }
    }
  } catch (err) {
    console.warn("fetchNotifications error:", err);
  }
};

const handleMarkAllRead = async () => {
  try {
    await apiFetch("/api/admin/notifications/mark-read", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ markAll: true })
    });
    notificationsList.value.forEach(n => n.isRead = true);
    unreadNotificationsCount.value = 0;
  } catch {}
};

// Helper hình ảnh giấy tờ
const getValidDocImage = (imgUrl: string | undefined, _type?: "front" | "back" | "cert" | "license") => {
  if (imgUrl && typeof imgUrl === "string" && imgUrl.trim().length > 10) {
    return imgUrl;
  }
  return "https://images.unsplash.com/photo-1557804506-669a67965ba0?auto=format&fit=crop&w=600";
};

// KYC Modal Handlers
const openKycModal = (user: UserAccount, preferredType?: "seller" | "shipper") => {
  activeDocTab.value = "front";
  isOcrScanning.value = false;
  ocrExtractedSuccess.value = false;
  const isShipperPending = user.shipperDetails && (user.shipperDetails.status === "Pending" || user.shipperDetails.status === "pending");
  const chooseShipper = preferredType === "shipper" || isShipperPending;

  if (chooseShipper && user.shipperDetails) {
    selectedKycType.value = "shipper";
    selectedKycItem.value = {
      ...user.shipperDetails,
      email: user.email || user.shipperDetails.phoneNumber,
      fullName: user.shipperDetails.fullName || user.fullName,
      userId: user.id
    };
    activeKycImage.value = getValidDocImage(user.shipperDetails.cccdFrontImage || user.shipperDetails.drivingLicenseImage, "front");
  } else if (user.storeDetails) {
    selectedKycType.value = "seller";
    selectedKycItem.value = {
      ...user.storeDetails,
      email: user.email || user.storeDetails.phoneEmail,
      fullName: user.storeDetails.ownerFullName || user.fullName,
      userId: user.id
    };
    activeKycImage.value = getValidDocImage(user.storeDetails.cccdFrontImage, "front");
  } else {
    selectedKycType.value = "seller";
    selectedKycItem.value = {
      fullName: user.fullName || "Đối Tác ZoneMart",
      email: user.email || "",
      phoneEmail: user.email || "",
      storeName: `Gian Hàng: ${user.fullName || "ZoneMart"}`,
      category: "Nông sản & Thực phẩm",
      address: "Chợ được phân công",
      userId: user.id,
      id: user.id
    };
    activeKycImage.value = getValidDocImage(undefined, "front");
  }
  showKycModal.value = true;
};

const handleApproveKyc = async () => {
  if (!selectedKycItem.value) return;
  const targetId = selectedKycItem.value.id || selectedKycItem.value.storeId || selectedKycItem.value.shipperId || selectedKycItem.value.email;
  const currentType = selectedKycType.value;

  try {
    const res = await apiFetch("/api/admin/approve-partner", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        partnerId: targetId,
        type: currentType,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      showToast(`Ban Quản Lý đã phê duyệt hồ sơ ${currentType === 'seller' ? 'Gian Hàng' : 'Tài Xế'} thành công!`, "success");
      showKycModal.value = false;
      await fetchUsers();
      await fetchStats();
      await fetchMarketsOverview();
      await fetchAuditLogs();
    } else {
      showToast("Không thể phê duyệt hồ sơ!", "danger");
    }
  } catch {
    showToast("Lỗi kết nối khi phê duyệt hồ sơ!", "danger");
  }
};

const handleConfirmRejectKyc = async () => {
  if (!selectedKycItem.value) return;
  const reason = rejectReasonInput.value.trim() || "Hồ sơ không đạt tiêu chuẩn quy định của Ban Quản Lý.";
  isSubmittingReject.value = true;
  try {
    const res = await apiFetch("/api/admin/reject-partner", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        partnerId: selectedKycItem.value.id || selectedKycItem.value.storeId || selectedKycItem.value.shipperId,
        type: selectedKycType.value,
        reason,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      showToast(`Đã từ chối hồ sơ đối tác!`, "warning");
      showRejectReasonPopup.value = false;
      showKycModal.value = false;
      await fetchUsers();
      await fetchStats();
      await fetchAuditLogs();
    } else {
      showToast("Không thể từ chối hồ sơ!", "danger");
    }
  } finally {
    isSubmittingReject.value = false;
  }
};

// Kỷ luật tài khoản (Chặn với Admin)
const openDisciplineModal = (user: UserAccount) => {
  if (isSuperAdminAccount(user)) {
    showToast("Không thể áp dụng kỷ luật đối với Quản trị viên cấp cao!", "warning");
    return;
  }
  selectedUserForPunish.value = user;
  disciplineLevel.value = 1;
  disciplineReason.value = "";
  showDisciplineModal.value = true;
};

const handleApplyDiscipline = async () => {
  if (!selectedUserForPunish.value) return;
  try {
    const res = await apiFetch("/api/admin/discipline-user", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: selectedUserForPunish.value.id,
        userEmail: selectedUserForPunish.value.email,
        level: Number(disciplineLevel.value),
        reason: disciplineReason.value.trim() || "Vi phạm quy chế cụm chợ ZoneMart",
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.message || "Đã áp dụng chế tài kỷ luật thành công!", "warning");
      showDisciplineModal.value = false;
      await fetchUsers();
      await fetchStats();
      await fetchAuditLogs();
    } else {
      showToast("Không thể áp dụng kỷ luật tài khoản!", "danger");
    }
  } catch {
    showToast("Lỗi kết nối khi xử lý kỷ luật!", "danger");
  }
};

// Mở khóa nhanh (Chặn với Admin)
const handleUnlockUser = async (user: UserAccount) => {
  try {
    const res = await apiFetch("/api/admin/unlock-user", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: user.id,
        userEmail: user.email,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      showToast(`Đã khôi phục hoạt động cho ${user.fullName}!`, "success");
      await fetchUsers();
      await fetchStats();
      await fetchAuditLogs();
    } else {
      showToast("Không thể mở khóa tài khoản!", "danger");
    }
  } catch {
    showToast("Lỗi kết nối khi mở khóa tài khoản!", "danger");
  }
};

// Xóa tài khoản (Chặn với Admin)
const openDeleteModal = (user: UserAccount) => {
  if (isSuperAdminAccount(user)) {
    showToast("Không thể xóa tài khoản Quản trị viên cấp cao!", "warning");
    return;
  }
  selectedUserForDelete.value = user;
  deleteConfirmText.value = "";
  showDeleteModal.value = true;
};

const handleConfirmDelete = async () => {
  if (!selectedUserForDelete.value || deleteConfirmText.value.trim().toUpperCase() !== "XÓA") {
    showToast("Vui lòng gõ chữ 'XÓA' để xác nhận thao tác!", "warning");
    return;
  }
  try {
    const deletedUser = selectedUserForDelete.value;
    const res = await apiFetch("/api/admin/delete-user", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: deletedUser.id,
        email: deletedUser.email,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      showToast(`Đã xóa tài khoản #${deletedUser.id} (${deletedUser.fullName})!`, "success");
      showDeleteModal.value = false;
      await fetchUsers();
      await fetchStats();
      await fetchAuditLogs();
    } else {
      showToast("Không thể xóa tài khoản!", "danger");
    }
  } catch {
    showToast("Đã xảy ra lỗi khi xóa tài khoản!", "danger");
  }
};

const handleLogout = () => {
  auth.logout();
  router.push("/login");
};

// Khởi chạy khi tải trang
onMounted(() => {
  loadLocalManagerLogs();
  fetchStats();
  fetchUsers();
  fetchAuditLogs();
  fetchNotifications();
  fetchMarketsOverview();
  fetchRealProductsFromBackend();

  notifPollInterval = setInterval(() => {
    fetchNotifications();
  }, 4000);
});

onUnmounted(() => {
  if (notifPollInterval) clearInterval(notifPollInterval);
});
</script>

<template>
  <div class="manager-root-layout">
    <!-- 1. TOPBAR HEADER (Nền trắng thanh lịch, Brand ZoneMart, Badge BAN QUẢN LÝ Teal) -->
    <header class="manager-topbar">
      <div class="topbar-left">
        <button
          type="button"
          class="mobile-menu-toggle-btn"
          @click="isMobileMenuOpen = !isMobileMenuOpen"
          :aria-label="isMobileMenuOpen ? 'Đóng menu' : 'Mở menu ban quản lý'"
        >
          <X v-if="isMobileMenuOpen" :size="20" />
          <Menu v-else :size="20" />
        </button>

        <router-link to="/" class="brand-link" title="Về trang chủ ZoneMart">
          <span class="brand-zone">Zone</span><span class="brand-mart">Mart</span>
          <span class="brand-badge-manager">BAN QUẢN LÝ</span>
        </router-link>
      </div>

      <div class="topbar-right">
        <!-- Ô tìm kiếm nhanh -->
        <div class="search-input-pill">
          <Search :size="16" class="search-icon" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm người dùng, gian hàng, biển số xe..."
            @input="activeTab !== 'dashboard' ? (activeTab = 'accounts') : null"
          />
          <button v-if="searchQuery" class="clear-search-btn" @click="searchQuery = ''">
            <X :size="14" />
          </button>
        </div>

        <!-- Chuông thông báo Real-time -->
        <div class="notification-dropdown-wrapper">
          <button
            class="topbar-icon-btn notification-btn"
            :class="{ active: isNotificationOpen }"
            title="Thông báo ban quản lý"
            @click.stop="isNotificationOpen = !isNotificationOpen"
          >
            <Bell :size="19" />
            <span v-if="unreadNotificationsCount > 0" class="notif-badge-pill">
              <span class="ping-ring-pill"></span>
              <span class="badge-number">{{ unreadNotificationsCount > 99 ? '99+' : unreadNotificationsCount }}</span>
            </span>
          </button>

          <!-- Notification Popover -->
          <div v-if="isNotificationOpen" class="notification-popover" @click.stop>
            <div class="popover-header">
              <div class="popover-title">
                <Bell :size="16" class="title-icon" />
                <span>Thông Báo Ca Trực</span>
                <span v-if="unreadNotificationsCount > 0" class="unread-pill">{{ unreadNotificationsCount }} mới</span>
              </div>
              <button
                v-if="unreadNotificationsCount > 0"
                class="mark-all-read-btn"
                @click="handleMarkAllRead"
              >
                <CheckCheck :size="14" /> Đọc hết
              </button>
            </div>

            <div class="popover-scroll-body">
              <div v-if="notificationsList.length === 0" class="empty-notif-box">
                <BellOff :size="28" class="text-muted mb-2" />
                <p>Không có thông báo mới trong ca trực!</p>
              </div>
              <div
                v-for="notif in notificationsList.slice(0, 8)"
                :key="notif.id"
                class="notif-item-row"
                :class="{ unread: !notif.isRead }"
              >
                <div class="notif-type-icon">
                  <Store v-if="notif.type === 'seller'" :size="15" />
                  <Bike v-else-if="notif.type === 'shipper'" :size="15" />
                  <CheckCircle2 v-else :size="15" />
                </div>
                <div class="notif-content">
                  <div class="notif-item-header">
                    <span class="notif-item-title">{{ notif.title }}</span>
                  </div>
                  <p class="notif-item-msg">{{ notif.message }}</p>
                </div>
              </div>
            </div>

            <div class="popover-footer">
              <button class="view-all-kyc-btn" @click="activeTab = 'kyc'; isNotificationOpen = false">
                <span>Xem danh sách hồ sơ cần duyệt ({{ pendingCount }})</span>
                <ChevronRight :size="14" />
              </button>
            </div>
          </div>
        </div>

        <!-- Chợ phụ trách Pill Badge -->
        <div class="assigned-market-pill-header" v-if="assignedMarket">
          <MapPin :size="13" class="text-emerald" />
          <span class="mkt-name">{{ assignedMarket.marketName }}</span>
        </div>

        <!-- Avatar Ban Quản Lý -->
        <div class="topbar-avatar-wrap" @click="selectManagerTab('dashboard')">
          <img
            :src="auth.currentUser.value?.avatarUrl || 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=150'"
            alt="Manager Avatar"
            class="topbar-avatar-img"
          />
          <span class="manager-online-badge"></span>
        </div>
      </div>
    </header>

    <!-- 2. BỐ CỤC CHÍNH (SIDEBAR SLATE TỐI + MAIN CONTENT NỀN SÁNG) -->
    <div class="manager-body-layout">
      <!-- Backdrop mobile -->
      <transition name="fade-backdrop">
        <div
          v-if="isMobileMenuOpen"
          class="sidebar-backdrop"
          @click="isMobileMenuOpen = false"
        ></div>
      </transition>

      <!-- LEFT SIDEBAR -->
      <aside class="manager-sidebar" :class="{ 'is-mobile-open': isMobileMenuOpen }">
        <div class="sidebar-mobile-header">
          <div class="sidebar-mobile-title">
            <span class="brand-zone text-white">Zone</span><span class="brand-mart text-emerald">Mart</span>
            <span class="badge bg-teal-subtle text-teal ms-2">BQL CA TRỰC</span>
          </div>
          <button type="button" class="sidebar-close-btn" @click="isMobileMenuOpen = false">
            <X :size="18" />
          </button>
        </div>

        <!-- NHÓM MENU CHÍNH -->
        <div class="sidebar-group">
          <div class="sidebar-heading">QUẢN LÝ VẬN HÀNH</div>
          <ul class="sidebar-nav">
            <li
              class="nav-item"
              :class="{ active: activeTab === 'dashboard' }"
              @click="selectManagerTab('dashboard')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <LayoutDashboard :size="17" />
                </div>
                <span>Dashboard</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <li
              class="nav-item"
              :class="{ active: activeTab === 'accounts' && searchTargetTab === 'all' }"
              @click="selectManagerTab('accounts', 'all')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <Users :size="17" />
                </div>
                <span>Người Dùng</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <li
              class="nav-item"
              :class="{ active: activeTab === 'kyc' }"
              @click="selectManagerTab('kyc')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <ShieldCheck :size="17" />
                </div>
                <span>Duyệt Hồ Sơ</span>
              </div>
              <span v-if="pendingCount > 0" class="nav-count-badge">{{ pendingCount }}</span>
              <ChevronRight v-else :size="15" class="nav-arrow" />
            </li>

            <li
              class="nav-item"
              :class="{ active: activeTab === 'products' }"
              @click="selectManagerTab('products')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <PackageCheck :size="17" />
                </div>
                <span>Sản Phẩm & Duyệt</span>
              </div>
              <span v-if="pendingProductsCount > 0" class="nav-count-badge badge-pending">{{ pendingProductsCount }}</span>
              <ChevronRight v-else :size="15" class="nav-arrow" />
            </li>

            <!-- TAB KHU VỰC & CHỢ SỐ PHỤ TRÁCH -->
            <li
              class="nav-item"
              :class="{ active: activeTab === 'markets' }"
              @click="selectManagerTab('markets')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <MapPin :size="17" />
                </div>
                <span>Chợ Phụ Trách</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <!-- TAB NHẬT KÝ CA TRỰC QUẢN LÝ -->
            <li
              class="nav-item"
              :class="{ active: activeTab === 'audit' }"
              @click="selectManagerTab('audit')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <History :size="17" />
                </div>
                <span>Nhật Ký Ca Trực</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>
          </ul>
        </div>

        <!-- NHÓM PHÂN HỆ ĐỐI TÁC -->
        <div class="sidebar-group">
          <div class="sidebar-heading">ĐỐI TÁC & HOẠT ĐỘNG</div>
          <ul class="sidebar-nav">
            <li
              class="nav-item"
              :class="{ active: activeTab === 'accounts' && searchTargetTab === 'seller' }"
              @click="selectManagerTab('accounts', 'seller')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <Store :size="17" />
                </div>
                <span>Gian Hàng (Shop)</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <li
              class="nav-item"
              :class="{ active: activeTab === 'accounts' && searchTargetTab === 'shipper' }"
              @click="selectManagerTab('accounts', 'shipper')"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <Bike :size="17" />
                </div>
                <span>Tài Xế Shipper</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <!-- VÀO CỬA HÀNG (DƯỚI CHỖ TÀI XẾ) -->
            <li class="nav-item nav-item-shopping" @click="router.push('/')">
              <div class="nav-label-box">
                <div class="nav-icon-wrapper icon-shopping">
                  <ShoppingBag :size="17" />
                </div>
                <span>Vào cửa hàng</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>
          </ul>
        </div>

        <!-- Nút Đăng Xuất An Toàn -->
        <div class="sidebar-footer-action">
          <button class="sidebar-logout-btn" @click="handleLogout">
            <LogOut :size="16" />
            <span>Đăng Xuất</span>
          </button>
        </div>
      </aside>

      <!-- MAIN CONTENT AREA -->
      <main class="manager-main-content">
        <!-- ========================================== -->
        <!-- TAB 1: DASHBOARD CHÍNH CHO BAN QUẢN LÝ -->
        <!-- (BỎ PHẦN HỒ SƠ CHỜ PHÊ DUYỆT THEO YÊU CẦU) -->
        <!-- ========================================== -->
        <div v-if="activeTab === 'dashboard'" class="dashboard-concept-container">
          <!-- A. KHUNG PROFILE HERO CỦA MANAGER -->
          <div class="hero-profile-card">
            <div class="hero-top-row">
              <div class="hero-avatar-circle">
                <img
                  :src="auth.currentUser.value?.avatarUrl || 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=250'"
                  alt="Manager Avatar"
                  class="hero-avatar-img"
                />
              </div>

              <div class="hero-info-column">
                <div class="hero-name-rating">
                  <h2 class="hero-full-name">
                    {{ currentActorName }}
                  </h2>
                  <div class="hero-stars-badge">
                    <div class="stars-gold-wrap">
                      <Star v-for="i in 5" :key="i" :size="13" fill="#10b981" stroke="#10b981" />
                    </div>
                    <span class="reviews-count">Ca Trực Hoạt Động Chuẩn</span>
                  </div>
                </div>

                <div class="hero-meta-row">
                  <span class="meta-item">
                    <ShieldCheck :size="14" class="meta-icon text-emerald" />
                    Vai trò: Ban Quản Lý Ca Trực
                  </span>
                  <span class="meta-item">
                    <Activity :size="14" class="meta-icon meta-live-dot" />
                    Trực Tuyến: 24/7 Giám Sát Chợ
                  </span>
                  <span class="meta-item" v-if="assignedMarket">
                    <MapPin :size="14" class="meta-icon text-danger" />
                    Khu vực: <strong>{{ assignedMarket.marketName }}</strong>
                  </span>
                </div>
              </div>
            </div>

            <!-- Dải 6 chỉ số kênh chính -->
            <div class="hero-channels-strip">
              <div class="channel-metric-cell cell-blue" @click="activeTab = 'accounts'; searchTargetTab = 'buyer'" title="Xem khách mua">
                <div class="metric-icon-box box-blue">
                  <Users :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ totalUserCount.toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Người dùng</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-orange" @click="activeTab = 'accounts'; searchTargetTab = 'seller'" title="Xem gian hàng">
                <div class="metric-icon-box box-orange">
                  <Store :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ sellerCount.toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Gian hàng</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-green" @click="activeTab = 'accounts'; searchTargetTab = 'shipper'" title="Xem shipper">
                <div class="metric-icon-box box-green">
                  <Bike :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ shipperCount.toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Shipper</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-cyan" @click="activeTab = 'products'" title="Xem sản phẩm">
                <div class="metric-icon-box box-cyan">
                  <Package :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ totalProductsCount.toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Sản phẩm</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-purple" title="Đơn hàng">
                <div class="metric-icon-box box-purple">
                  <ShoppingBag :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ stats.totalOrders.toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Đơn hàng</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-amber" @click="activeTab = 'kyc'" title="Hồ sơ chờ duyệt">
                <div class="metric-icon-box box-amber">
                  <Hourglass :size="18" class="hourglass-animated" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ pendingCount }}</span>
                  <span class="metric-lbl">Chờ duyệt</span>
                </div>
              </div>
            </div>
          </div>

          <!-- B. 4 STAT CARDS -->
          <div class="stat-cards-grid">
            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Total Views</span>
                  <span class="trend-pill trend-up"><TrendingUp :size="11" /> +14.2%</span>
                </div>
                <div class="stat-number">{{ (stats.totalViews || (totalUserCount * 15 + stats.totalOrders * 8)).toLocaleString('vi-VN') }}</div>
                <div class="stat-subtext">Lượt tương tác & truy cập</div>
              </div>
              <div class="stat-badge-squircle badge-cyan">
                <Eye :size="24" stroke-width="2.2" />
              </div>
            </div>

            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Total Followers</span>
                  <span class="trend-pill trend-up"><TrendingUp :size="11" /> +8.5%</span>
                </div>
                <div class="stat-number">{{ totalUserCount.toLocaleString('vi-VN') }}</div>
                <div class="stat-subtext">Khách hàng & Thành viên</div>
              </div>
              <div class="stat-badge-squircle badge-purple">
                <Users :size="24" stroke-width="2.2" />
              </div>
            </div>

            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Partnerships</span>
                  <span class="trend-pill trend-neutral"><Hourglass :size="11" /> {{ pendingCount }} Chờ KYC</span>
                </div>
                <div class="stat-number">{{ (sellerCount + shipperCount).toLocaleString('vi-VN') }}</div>
                <div class="stat-subtext">Shop & Shipper hoạt động</div>
              </div>
              <div class="stat-badge-squircle badge-teal">
                <Handshake :size="24" stroke-width="2.2" />
              </div>
            </div>

            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Total Earned</span>
                  <span class="trend-pill trend-up"><TrendingUp :size="11" /> {{ stats.totalOrders }} đơn</span>
                </div>
                <div class="stat-number stat-currency">{{ stats.totalRevenue.toLocaleString('vi-VN') }} ₫</div>
                <div class="stat-subtext">Doanh thu sàn ZoneMart</div>
              </div>
              <div class="stat-badge-squircle badge-yellow">
                <Wallet :size="24" stroke-width="2.2" />
              </div>
            </div>
          </div>

          <!-- C. 3 ANALYTICS BREAKDOWN CARDS -->
          <div class="analytics-cards-grid">
            <div class="analytics-card">
              <div class="analytics-card-title">Cơ Cấu Người Dùng</div>
              <div class="donut-chart-container">
                <div class="donut-svg-wrapper">
                  <svg viewBox="0 0 160 160" class="donut-svg">
                    <circle cx="80" cy="80" r="62" fill="none" stroke="#f1f5f9" stroke-width="18" />
                    <circle
                      cx="80"
                      cy="80"
                      r="62"
                      fill="none"
                      stroke="#0d9488"
                      stroke-width="18"
                      :stroke-dasharray="buyerStrokeDash"
                      stroke-linecap="round"
                      transform="rotate(-90 80 80)"
                    />
                  </svg>
                  <div class="donut-center-text">
                    <span class="donut-val">{{ buyerRatioPercent }}%</span>
                    <span class="donut-lbl">Khách hàng</span>
                  </div>
                </div>
                <div class="donut-legend-list">
                  <div class="legend-row">
                    <span class="legend-color-dot" style="background-color: #0d9488;"></span>
                    <span class="legend-name">Khách mua hàng:</span>
                    <strong class="legend-val">{{ buyerCount }}</strong>
                  </div>
                  <div class="legend-row">
                    <span class="legend-color-dot" style="background-color: #2563eb;"></span>
                    <span class="legend-name">Chủ gian hàng:</span>
                    <strong class="legend-val">{{ sellerCount }}</strong>
                  </div>
                  <div class="legend-row">
                    <span class="legend-color-dot" style="background-color: #10b981;"></span>
                    <span class="legend-name">Tài xế giao hàng:</span>
                    <strong class="legend-val">{{ shipperCount }}</strong>
                  </div>
                </div>
              </div>
            </div>

            <div class="analytics-card">
              <div class="analytics-card-title">Phân Bố Độ Tuổi & Mua Sắm</div>
              <div class="age-distribution-list">
                <div class="age-bar-item">
                  <div class="age-label-row">
                    <span class="age-bracket">18 - 24 Tuổi (Sinh viên)</span>
                    <span class="age-percent">38%</span>
                  </div>
                  <div class="age-progress-track">
                    <div class="age-progress-fill fill-teal" style="width: 38%;"></div>
                  </div>
                </div>
                <div class="age-bar-item">
                  <div class="age-label-row">
                    <span class="age-bracket">25 - 34 Tuổi (Dân văn phòng)</span>
                    <span class="age-percent">44%</span>
                  </div>
                  <div class="age-progress-track">
                    <div class="age-progress-fill fill-blue" style="width: 44%;"></div>
                  </div>
                </div>
                <div class="age-bar-item">
                  <div class="age-label-row">
                    <span class="age-bracket">35 - 54 Tuổi (Hộ gia đình)</span>
                    <span class="age-percent">15%</span>
                  </div>
                  <div class="age-progress-track">
                    <div class="age-progress-fill fill-cyan" style="width: 15%;"></div>
                  </div>
                </div>
                <div class="age-bar-item">
                  <div class="age-label-row">
                    <span class="age-bracket">55+ Tuổi (Nội trợ)</span>
                    <span class="age-percent">3%</span>
                  </div>
                  <div class="age-progress-track">
                    <div class="age-progress-fill fill-purple" style="width: 3%;"></div>
                  </div>
                </div>
              </div>
            </div>

            <div class="analytics-card">
              <div class="analytics-card-title">Top Khu Vực Hoạt Động</div>
              <div class="top-locations-list">
                <div v-for="loc in topLocations" :key="loc.name" class="location-item-row">
                  <div class="loc-text-row">
                    <span class="loc-name">
                      <MapPin :size="12" class="me-1 text-danger" />
                      {{ loc.name }}
                    </span>
                    <span class="loc-count">{{ loc.count }} cơ sở</span>
                  </div>
                  <div class="loc-bar-track">
                    <div class="loc-bar-fill fill-teal" :style="{ width: loc.width }"></div>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <!-- LƯU Ý: Phần "Hồ Sơ Đối Tác Đang Chờ Phê Duyệt" đã được loại bỏ hoàn toàn đối với form Quản Lý theo yêu cầu! -->
        </div>

        <!-- ========================================== -->
        <!-- TAB 2: QUẢN LÝ NGƯỜI DÙNG & PHÂN QUYỀN -->
        <!-- (LOẠI BỎ QUYỀN SỬA/XÓA TÀI KHOẢN ADMIN) -->
        <!-- ========================================== -->
        <div v-else-if="activeTab === 'accounts'" class="tab-content-container">
          <div class="content-header-card">
            <div class="header-title-box">
              <h2 class="content-heading">Quản Lý Người Dùng & Phân Quyền</h2>
              <p class="content-sub">Tìm kiếm, kiểm tra trạng thái và áp dụng chế tài kỷ luật cho đối tác tiểu thương & khách hàng</p>
            </div>

            <div class="filter-controls-row">
              <div class="filter-tabs-pills">
                <button
                  class="filter-pill"
                  :class="{ active: searchTargetTab === 'all' }"
                  @click="searchTargetTab = 'all'"
                >
                  <Users :size="13" /> Tất cả ({{ users.length }})
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: searchTargetTab === 'buyer' }"
                  @click="searchTargetTab = 'buyer'"
                >
                  <Users :size="13" /> Khách Hàng ({{ buyerCount }})
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: searchTargetTab === 'seller' }"
                  @click="searchTargetTab = 'seller'"
                >
                  <Store :size="13" /> Chủ Shop ({{ sellerCount }})
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: searchTargetTab === 'shipper' }"
                  @click="searchTargetTab = 'shipper'"
                >
                  <Bike :size="13" /> Shipper ({{ shipperCount }})
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: searchTargetTab === 'manager' }"
                  @click="searchTargetTab = 'manager'"
                >
                  <ShieldCheck :size="13" /> Cán Bộ BQL ({{ managerAndAdminCount }})
                </button>
              </div>

              <select v-model="selectedStatusFilter" class="status-select-box">
                <option value="All">Tất cả trạng thái</option>
                <option value="active">Đang hoạt động</option>
                <option value="pending">Chờ duyệt</option>
                <option value="rejected">Bị từ chối</option>
                <option value="suspended">Tạm đình chỉ</option>
                <option value="banned">Bị cấm</option>
              </select>
            </div>
          </div>

          <div class="table-card">
            <div class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th style="min-width: 260px;">Tài Khoản / Người Dùng</th>
                    <th style="min-width: 175px;">Vai Trò Sàn</th>
                    <th style="min-width: 160px;">Trạng Thái</th>
                    <th style="min-width: 130px;">Số Lần Vi Phạm</th>
                    <th style="min-width: 130px;">Ngày Tham Gia</th>
                    <th style="min-width: 130px;" class="text-center">Hành Động</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="user in filteredUsersList" :key="user.id">
                    <td>
                      <div class="table-user-cell">
                        <img :src="user.avatarUrl || 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=120'" class="table-avatar" />
                        <div>
                          <strong>{{ user.fullName }}</strong>
                          <small>{{ user.email }}</small>
                          <small v-if="user.storeDetails && (user.storeDetails.status === 'Approved' || user.isSeller)" class="text-emerald">
                            <Store :size="11" class="inline-icon" /> {{ user.storeDetails.storeName }}
                          </small>
                          <small v-if="user.shipperDetails && (user.shipperDetails.status === 'Approved' || user.isShipper)" class="text-blue">
                            <Bike :size="11" class="inline-icon" /> {{ user.shipperDetails.licensePlate }}
                          </small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div class="d-flex flex-wrap gap-1 align-items-center">
                        <span v-if="user.isAdmin" class="badge-role-admin">
                          <ShieldCheck :size="11" /> Quản Trị Viên
                        </span>
                        <span v-if="user.isManager" class="badge-role-manager">
                          <UserCheck :size="11" /> Ban Quản Lý
                        </span>
                        <span v-if="user.isSeller" class="badge-role-seller">
                          <Store :size="11" /> Chủ Gian Hàng
                        </span>
                        <span v-if="user.isShipper" class="badge-role-shipper">
                          <Bike :size="11" /> Tài Xế Shipper
                        </span>
                        <span v-if="!user.isAdmin && !user.isManager && !user.isSeller && !user.isShipper" class="badge-role-buyer">
                          <Users :size="11" /> Khách Hàng
                        </span>
                      </div>
                    </td>
                    <td>
                      <span
                        class="status-pill"
                        :class="{
                          'status-active': user.accountStatus === 'active',
                          'status-pending': user.accountStatus === 'pending',
                          'status-rejected': user.accountStatus === 'rejected',
                          'status-locked': user.accountStatus === 'locked_10_days' || user.accountStatus === 'suspended',
                          'status-banned': user.accountStatus === 'banned'
                        }"
                      >
                        {{ user.accountStatus === 'active' ? 'Hoạt động' : user.accountStatus === 'locked_10_days' ? 'Khóa 10 ngày' : user.accountStatus === 'suspended' ? 'Đình chỉ' : user.accountStatus === 'banned' ? 'Đã cấm' : 'Chờ duyệt' }}
                      </span>
                    </td>
                    <td>
                      <span class="violation-badge" :class="{ 'has-violation': user.violationCount > 0 }">
                        {{ user.violationCount || 0 }} lần
                      </span>
                    </td>
                    <td>
                      <small class="text-muted">{{ user.createdAt || 'Mới tham gia' }}</small>
                    </td>
                    <td class="text-center">
                      <!-- LOẠI BỎ TOÀN BỘ QUYỀN SỬA/KỶ LUẬT/XÓA ĐỐI VỚI ADMIN TRÊN TRANG QUẢN LÝ -->
                      <div v-if="isSuperAdminAccount(user)" class="admin-protected-tag mx-auto" title="Tài khoản Quản trị viên cấp cao - Ban Quản Lý không có quyền can thiệp">
                        <ShieldCheck :size="12" class="text-secondary" />
                        <span>Admin (Bảo vệ)</span>
                      </div>
                      <div v-else class="actions-inline-btns center-actions">
                        <!-- Ban Quản Lý không có quyền phân quyền nhanh (chỉ Admin) -->
                        <button v-if="user.accountStatus === 'active'" class="action-btn btn-punish" title="Áp dụng chế tài kỷ luật" @click="openDisciplineModal(user)">
                          <AlertTriangle :size="14" />
                        </button>
                        <button v-else-if="user.accountStatus === 'locked_10_days' || user.accountStatus === 'suspended'" class="action-btn btn-unlock" title="Mở khóa tài khoản" @click="handleUnlockUser(user)">
                          <Unlock :size="14" />
                        </button>
                        <button class="action-btn btn-delete" title="Xóa tài khoản" @click="openDeleteModal(user)">
                          <Trash2 :size="14" />
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- ========================================== -->
        <!-- TAB 3: THẨM ĐỊNH HỒ SƠ ĐỐI TÁC (KYC) -->
        <!-- ========================================== -->
        <div v-else-if="activeTab === 'kyc'" class="tab-content-container">
          <div class="content-header-card">
            <div class="header-title-box">
              <h2 class="content-heading">Thẩm Định & Duyệt Hồ Sơ Đối Tác</h2>
              <p class="content-sub">Đối soát thông tin CCCD, chứng nhận vệ sinh ATVSTP và giấy tờ xe của Chủ Tiệm & Tài Xế</p>
            </div>
            <div class="header-actions">
              <span class="count-tag-pill">
                <Hourglass :size="13" class="hourglass-animated text-amber" />
                <span>{{ pendingKycUsers.length }} hồ sơ chờ duyệt</span>
              </span>
            </div>
          </div>

          <div class="table-card">
            <div v-if="pendingKycUsers.length === 0" class="empty-state-card">
              <CheckCircle2 :size="48" class="text-success mb-2" />
              <h3>Đã thẩm định hoàn tất!</h3>
              <p>Hiện không có hồ sơ đối tác nào đang chờ Ban Quản Lý duyệt.</p>
            </div>
            <div v-else class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th>Người nộp hồ sơ</th>
                    <th>Loại đối tác</th>
                    <th>Thông tin kinh doanh</th>
                    <th>Trạng thái</th>
                    <th class="text-end">Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="user in pendingKycUsers" :key="user.id">
                    <td>
                      <div class="table-user-cell">
                        <img :src="user.avatarUrl || 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=120'" class="table-avatar" />
                        <div>
                          <strong>{{ user.fullName }}</strong>
                          <small>{{ user.email }}</small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span v-if="user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')" class="badge-role-shipper">Tài Xế Shipper</span>
                      <span v-else class="badge-role-seller">Gian Hàng (Shop)</span>
                    </td>
                    <td>
                      <span v-if="user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')">
                        {{ user.shipperDetails.licensePlate }} • {{ user.shipperDetails.vehicleModel || 'Xe máy' }}
                      </span>
                      <span v-else-if="user.storeDetails">
                        {{ user.storeDetails.storeName }} • {{ user.storeDetails.category }}
                      </span>
                      <span v-else>Đang chờ hoàn thiện</span>
                    </td>
                    <td>
                      <span class="status-pill status-pending">
                        <Hourglass :size="12" class="hourglass-animated me-1" /> Chờ BQL Duyệt
                      </span>
                    </td>
                    <td class="text-end">
                      <button class="btn-table-action btn-kyc-review" @click="openKycModal(user, (user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')) ? 'shipper' : 'seller')">
                        <Eye :size="14" class="me-1" />
                        <span>Xem & Duyệt</span>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- ========================================== -->
        <!-- TAB 4: QUẢN LÝ & KIỂM DUYỆT SẢN PHẨM SÀN -->
        <!-- ========================================== -->
        <div v-else-if="activeTab === 'products'" class="tab-content-container">
          <div class="content-header-card d-flex flex-wrap justify-content-between align-items-center gap-3">
            <div class="header-title-box">
              <div class="d-flex align-items-center gap-3">
                <div class="header-icon-circle bg-teal-gradient">
                  <PackageCheck :size="24" class="text-white" />
                </div>
                <div>
                  <h2 class="content-heading mb-0">Quản Lý & Kiểm Duyệt Sản Phẩm Sàn</h2>
                  <p class="content-sub mb-0 mt-1">
                    Thẩm định chất lượng và duyệt mở bán cho toàn bộ <strong>{{ allProducts.length }}</strong> sản phẩm tiểu thương
                  </p>
                </div>
              </div>
            </div>
            <div class="header-actions">
              <button class="btn-primary-action" @click="fetchRealProductsFromBackend">
                <Activity :size="15" class="me-1" />
                <span>Làm Mới Sản Phẩm</span>
              </button>
            </div>
          </div>

          <!-- Bộ lọc sản phẩm -->
          <div class="product-toolbar-card mb-3">
            <div class="d-flex flex-wrap justify-content-between align-items-center gap-3">
              <div class="prod-filter-pills-group">
                <button
                  class="prod-pill-btn"
                  :class="{ active: selectedProductFilterTab === 'all' }"
                  @click="selectedProductFilterTab = 'all'"
                >
                  <Package :size="14" />
                  <span>Tất Cả ({{ allProducts.length }})</span>
                </button>
                <button
                  class="prod-pill-btn pill-active-style"
                  :class="{ active: selectedProductFilterTab === 'active' }"
                  @click="selectedProductFilterTab = 'active'"
                >
                  <CheckCircle2 :size="14" />
                  <span>Đang Mở Bán ({{ activeProducts.length }})</span>
                </button>
                <button
                  class="prod-pill-btn pill-pending-style"
                  :class="{ active: selectedProductFilterTab === 'pending' }"
                  @click="selectedProductFilterTab = 'pending'"
                >
                  <Hourglass :size="14" />
                  <span>Chờ BQL Duyệt ({{ pendingReviewProducts.length }})</span>
                </button>
                <button
                  class="prod-pill-btn pill-rejected-style"
                  :class="{ active: selectedProductFilterTab === 'rejected' }"
                  @click="selectedProductFilterTab = 'rejected'"
                >
                  <XCircle :size="14" />
                  <span>Bị Từ Chối ({{ rejectedProducts.length }})</span>
                </button>
              </div>

              <div class="prod-search-input-box">
                <Search :size="15" class="search-icon" />
                <input
                  v-model="productFilterSearchQuery"
                  type="text"
                  class="prod-search-control"
                  placeholder="Tìm tên sản phẩm, gian hàng, danh mục..."
                />
              </div>
            </div>
          </div>

          <!-- Bảng sản phẩm -->
          <div class="table-card">
            <div v-if="displayedProducts.length === 0" class="empty-state-card" style="padding: 50px 20px; text-align: center;">
              <CheckCircle2 v-if="selectedProductFilterTab === 'pending'" :size="48" class="text-success mb-2" />
              <Package v-else :size="48" class="text-muted mb-2" />
              <h4>{{ selectedProductFilterTab === 'pending' ? 'Hiện không có sản phẩm nào cần duyệt!' : 'Không tìm thấy sản phẩm phù hợp' }}</h4>
            </div>

            <div v-else class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th style="min-width: 280px;">Sản Phẩm & Hình Ảnh</th>
                    <th style="min-width: 220px;">Gian Hàng Sở Hữu</th>
                    <th style="min-width: 130px;">Giá Niêm Yết</th>
                    <th style="min-width: 100px;">Tồn Kho</th>
                    <th style="min-width: 190px;">Trạng Thái & AI Scan</th>
                    <th style="min-width: 160px;" class="text-end">Hành Động</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="prod in displayedProducts" :key="prod.id">
                    <td>
                      <div class="product-info-cell">
                        <div class="product-thumb-wrapper" @click="activeZoomImage = prod.image" title="Xem ảnh">
                          <img :src="prod.image || 'https://images.unsplash.com/photo-1540420773420-3366772f4999?w=150'" class="product-thumb-img" />
                          <div class="thumb-zoom-hint"><Eye :size="12" /></div>
                        </div>
                        <div class="product-meta-text">
                          <strong class="product-name-title">{{ prod.name }}</strong>
                          <div class="d-flex align-items-center gap-1 mt-1">
                            <span class="category-pill-tag">{{ prod.category }}</span>
                            <span class="unit-pill-tag">{{ prod.unit || '0.5 kg' }}</span>
                          </div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div class="store-info-meta-cell">
                        <div class="store-name-line">
                          <Store :size="14" class="text-emerald flex-shrink-0" />
                          <strong class="store-name-text">{{ prod.storeName || 'Gian Hàng' }}</strong>
                        </div>
                        <div class="store-email-line">
                          <Mail :size="12" class="text-muted flex-shrink-0" />
                          <span class="store-email-text">{{ prod.sellerEmail || 'shop@zonemart.vn' }}</span>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div class="product-price-box">
                        <span class="price-amount">{{ Number(prod.price).toLocaleString('vi-VN') }}</span>
                        <span class="price-currency">₫</span>
                      </div>
                    </td>
                    <td>
                      <span class="badge-stock-pill" :class="{ 'stock-low': prod.stock < 10 }">
                        {{ prod.stock }} SP
                      </span>
                    </td>
                    <td>
                      <div v-if="prod.status === 'active'" class="product-status-flow">
                        <span class="status-pill status-active">
                          <span class="status-dot-pulse bg-emerald"></span>
                          <span>Đang Mở Bán</span>
                        </span>
                        <span class="ai-verified-tag">
                          <Sparkles :size="11" class="text-primary" />
                          <span>AI: An toàn</span>
                        </span>
                      </div>
                      <div v-else-if="prod.status === 'pending_review'" class="product-status-flow">
                        <span class="status-pill status-pending">
                          <Hourglass :size="12" />
                          <span>Chờ BQL Duyệt</span>
                        </span>
                      </div>
                      <div v-else class="product-status-flow">
                        <span class="status-pill status-rejected">
                          <XCircle :size="12" />
                          <span>Bị Từ Chối</span>
                        </span>
                      </div>
                    </td>
                    <td class="text-end">
                      <div class="actions-inline-btns">
                        <button
                          v-if="prod.status !== 'active'"
                          class="btn-row-action btn-row-approve"
                          @click="handleApproveProductItem(prod)"
                          title="Duyệt mở bán sản phẩm này"
                        >
                          <Check :size="13" />
                          <span>Duyệt Bán</span>
                        </button>
                        <button
                          v-if="prod.status !== 'rejected_need_edit' && prod.status !== 'deleted_violation'"
                          class="btn-row-action btn-row-reject"
                          @click="openRejectProductModal(prod)"
                          title="Từ chối sản phẩm này"
                        >
                          <XCircle :size="13" />
                          <span>Từ Chối</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- ================================================================ -->
        <!-- TAB 5: KHU VỰC & CHỢ SỐ PHỤ TRÁCH                               -->
        <!-- (CHỈ HIỆN CHỢ ĐƯỢC PHÂN CÔNG, DANH SÁCH GIAN HÀNG & CHỦ TIỆM,   -->
        <!--  KHÔNG HIỆN SELLER CHƯA GẮN CHỢ, KHÔNG CÓ NÚT ĐIỀU CHUYỂN BQL)  -->
        <!-- ================================================================ -->
        <div v-else-if="activeTab === 'markets'" class="tab-content-container markets-tab-content">
          <!-- HEADER BANNER -->
          <div class="content-header-card d-flex flex-wrap justify-content-between align-items-center gap-3">
            <div class="header-title-box">
              <div class="d-flex align-items-center gap-3">
                <div class="header-icon-circle bg-teal-gradient">
                  <Building2 :size="24" class="text-white" />
                </div>
                <div>
                  <h2 class="content-heading mb-0">Cụm Chợ Số Phân Công Phụ Trách</h2>
                  <p class="content-sub mb-0 mt-1">
                    Khu vực chợ truyền thống do Ban Quản Lý trực tiếp phụ trách số hóa, giám sát tiểu thương và vị trí quầy sạp
                  </p>
                </div>
              </div>
            </div>
            <div class="header-actions">
              <button class="btn-primary-action" :disabled="isLoadingMarkets" @click="fetchMarketsOverview(true)">
                <Loader2 v-if="isLoadingMarkets" :size="15" class="spinner-animated me-1" />
                <Activity v-else :size="15" class="me-1" />
                <span>{{ isLoadingMarkets ? 'Đang Đồng Bộ...' : 'Làm Mới Chợ' }}</span>
              </button>
            </div>
          </div>

          <!-- THÔNG BÁO NẾU CHƯA CÓ DỮ LIỆU CHỢ -->
          <div v-if="!assignedMarket" class="empty-state-card" style="padding: 60px 20px; text-align: center;">
            <Building2 :size="48" class="text-muted mb-2" />
            <h3>Chưa tìm thấy Cụm Chợ Số phân công</h3>
            <p>Vui lòng liên hệ Quản Trị Viên cấp cao để được gán chợ phụ trách trong hệ thống.</p>
          </div>

          <!-- NỘI DUNG CHI TIẾT CHỢ ĐƯỢC PHÂN CÔNG (FULL WIDTH TOÀN MÀN HÌNH) -->
          <div v-else class="assigned-market-full-view">
            <!-- 1. HERO CARD THÔNG TIN CỤM CHỢ SỐ PHỤ TRÁCH -->
            <div class="selected-market-hero-card">
              <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
                <div>
                  <div class="d-flex align-items-center gap-2 mb-1">
                    <h3 class="selected-market-title mb-0">{{ assignedMarket.marketName }}</h3>
                    <span :class="isMarketActive(assignedMarket) ? 'badge-status-open' : 'badge-status-closed'">
                      <span :class="isMarketActive(assignedMarket) ? 'dot-online' : 'dot-offline'"></span>
                      <span>{{ isMarketActive(assignedMarket) ? 'Đang Mở Cửa' : 'Không Hoạt Động' }}</span>
                    </span>
                    <span class="badge-market-code">Mã: {{ assignedMarket.marketCode }}</span>
                  </div>

                  <p class="selected-market-sub mb-2">
                    <MapPin :size="14" class="text-danger me-1 flex-shrink-0" />
                    <span>{{ assignedMarket.address }} • <strong>{{ assignedMarket.wardName }}</strong> ({{ assignedMarket.districtName }})</span>
                  </p>

                  <div class="bql-assigned-info-box">
                    <div class="bql-icon-badge">
                      <ShieldCheck :size="16" />
                    </div>
                    <div class="bql-text-wrap">
                      <div class="bql-main-title">
                        Ban Quản Lý phụ trách: <strong>{{ assignedMarket.managementBoardName || currentActorName }}</strong>
                      </div>
                      <div class="bql-sub-contact">
                        <span>Cán bộ trực ca: <strong>{{ currentActorName }}</strong></span>
                        <span class="contact-sep">•</span>
                        <span>Đường dây nóng hỗ trợ: <strong>{{ assignedMarket.boardPhone || currentActorEmail }}</strong></span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- TỶ LỆ LẤP ĐẦY GIAN HÀNG -->
                <div class="market-hero-metrics-pill">
                  <div class="metric-block">
                    <span class="m-lbl">Gian hàng tại chợ:</span>
                    <strong class="m-val">{{ assignedMarket.stores?.length || 0 }} / {{ assignedMarket.totalStalls || 100 }} sạp</strong>
                  </div>
                  <div class="stalls-progress-bar mt-2">
                    <div
                      class="stalls-fill"
                      :style="{ width: Math.min(100, Math.round(((assignedMarket.stores?.length || 0) / (assignedMarket.totalStalls || 100)) * 100)) + '%' }"
                    ></div>
                  </div>
                </div>
              </div>
            </div>

            <!-- 2. BẢNG DANH SÁCH GIAN HÀNG & THÔNG TIN CHỦ TIỆM CỦA CHỢ ĐƯỢC PHÂN CÔNG -->
            <div class="market-stores-table-card mt-3">
              <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                  <h4 class="stores-section-title mb-1 d-flex align-items-center gap-2">
                    <Store :size="18" class="text-emerald" />
                    <span>Danh Sách Gian Hàng & Thông Tin Chủ Tiệm ({{ assignedMarket.stores?.length || 0 }})</span>
                  </h4>
                  <p class="stores-section-sub mb-0 text-muted">
                    Toàn bộ tiểu thương đã được thẩm định & đăng ký kinh doanh tại {{ assignedMarket.marketName }}
                  </p>
                </div>
              </div>

              <!-- Empty state khi chợ chưa có tiểu thương -->
              <div v-if="!assignedMarket.stores || assignedMarket.stores.length === 0" class="empty-market-stores-box">
                <Store :size="36" class="text-muted mb-2" />
                <h5>Chưa có gian hàng nào đăng ký tại {{ assignedMarket.marketName }}</h5>
                <p>Tiểu thương khi được thẩm định đăng ký gian hàng tại khu vực này sẽ xuất hiện tự động tại đây.</p>
              </div>

              <!-- Table chi tiết tiểu thương -->
              <div v-else class="table-responsive-wrapper">
                <table class="concept-table">
                  <thead>
                    <tr>
                      <th style="min-width: 230px;">Gian Hàng & Mã Tiệm</th>
                      <th style="min-width: 230px;">Chủ Tiệm (Người Bán)</th>
                      <th style="min-width: 170px;">Vị Trí Sạp / Quầy</th>
                      <th style="min-width: 140px;">Giờ Mở Cửa</th>
                      <th style="min-width: 130px;">Trạng Thái</th>
                      <th style="min-width: 110px;" class="text-end">Hành Động</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="st in assignedMarket.stores" :key="st.storeId">
                      <td>
                        <div class="table-user-cell">
                          <img :src="st.avatarUrl || 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200'" class="table-avatar" />
                          <div>
                            <strong class="d-block">{{ st.storeName }}</strong>
                            <small class="text-muted">Mã: {{ st.storeCode || 'SEL-00' }}</small>
                            <div class="mt-1">
                              <span class="category-pill-tag">{{ st.category }}</span>
                            </div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <div class="owner-meta-cell">
                          <strong class="text-dark d-block">{{ st.ownerFullName }}</strong>
                          <small class="d-flex align-items-center gap-1 text-muted">
                            <Phone :size="11" class="text-muted flex-shrink-0" />
                            <span>{{ st.phoneEmail }}</span>
                          </small>
                          <small v-if="st.cccdNumber" class="d-flex align-items-center gap-1 text-muted">
                            <CreditCard :size="11" class="text-muted flex-shrink-0" />
                            <span>CCCD: {{ st.cccdNumber }}</span>
                          </small>
                          <small v-if="st.nguyenQuan" class="d-block text-muted">Quê quán: {{ st.nguyenQuan }}</small>
                        </div>
                      </td>
                      <td>
                        <div class="stall-badge-pill">
                          <span class="badge-stall-tag d-inline-flex align-items-center gap-1">
                            <MapPin :size="11" class="text-emerald flex-shrink-0" />
                            <span>{{ st.stallNumber || 'Quầy sạp tại chợ' }}</span>
                          </span>
                          <small class="d-block text-muted mt-1">{{ st.address }}</small>
                        </div>
                      </td>
                      <td>
                        <small class="fw-bold text-dark d-block">{{ st.openHours || '07:00 - 22:00' }}</small>
                        <small v-if="st.bankName" class="d-block text-muted">{{ st.bankName }}</small>
                      </td>
                      <td>
                        <span
                          class="status-pill"
                          :class="{
                            'status-active': st.status === 'Approved' || st.status === 'active',
                            'status-pending': st.status === 'Pending' || st.status === 'pending',
                            'status-rejected': st.status === 'Rejected' || st.status === 'rejected'
                          }"
                        >
                          {{ (st.status === 'Approved' || st.status === 'active') ? 'Đang Bán' : (st.status === 'Rejected' || st.status === 'rejected') ? 'Bị Từ Chối' : 'Chờ Duyệt' }}
                        </span>
                      </td>
                      <td class="text-end">
                        <span class="badge-stall-approved" style="font-size: 11px; padding: 4px 10px; border-radius: 6px; background: #ecfdf5; color: #059669; font-weight: 700; display: inline-flex; align-items: center; gap: 4px;">
                          <CheckCircle2 :size="13" /> Đã vào chợ
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <!-- LƯU Ý: Không hiển thị gian hàng tự do chưa gắn chợ và không có nút điều chuyển BQL theo đúng yêu cầu! -->
          </div>
        </div>

        <!-- ================================================================ -->
        <!-- TAB 6: NHẬT KÝ CA TRỰC QUẢN LÝ (MANAGER AUDIT TRAIL)              -->
        <!-- ================================================================ -->
        <div v-else-if="activeTab === 'audit'" class="tab-content-container">
          <div class="content-header-card">
            <div class="header-title-box">
              <h2 class="content-heading">Nhật Ký Hoạt Động Ca Trực Ban Quản Lý</h2>
              <p class="content-sub">Theo dõi toàn bộ lịch sử phân quyền, thẩm định hồ sơ, kiểm duyệt sản phẩm và xử lý vi phạm</p>
            </div>

            <div class="filter-controls-row">
              <div class="filter-tabs-pills">
                <button
                  class="filter-pill"
                  :class="{ active: auditFilterType === 'all' }"
                  @click="auditFilterType = 'all'"
                >
                  <History :size="13" /> Tất cả nhật ký ({{ managerAuditLogs.length }})
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: auditFilterType === 'mine' }"
                  @click="auditFilterType = 'mine'"
                >
                  <UserCheck :size="13" /> Hoạt động của tôi
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: auditFilterType === 'kyc' }"
                  @click="auditFilterType = 'kyc'"
                >
                  <ShieldCheck :size="13" /> Thẩm định KYC
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: auditFilterType === 'products' }"
                  @click="auditFilterType = 'products'"
                >
                  <PackageCheck :size="13" /> Sản phẩm & Chợ
                </button>
                <button
                  class="filter-pill"
                  :class="{ active: auditFilterType === 'discipline' }"
                  @click="auditFilterType = 'discipline'"
                >
                  <AlertTriangle :size="13" /> Kỷ luật & Khóa
                </button>
              </div>
            </div>
          </div>

          <div class="table-card">
            <div v-if="filteredAuditLogs.length === 0" class="empty-state-card">
              <History :size="48" class="text-muted mb-2" />
              <h3>Chưa có nhật ký hoạt động nào</h3>
              <p>Mọi hành động thẩm định, phân quyền và kiểm duyệt của ca trực sẽ được ghi vết tự động tại đây.</p>
            </div>
            <div v-else class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th style="min-width: 140px;">Thời Gian</th>
                    <th style="min-width: 170px;">Cán Bộ Thực Hiện</th>
                    <th style="min-width: 160px;">Hành Động</th>
                    <th style="min-width: 190px;">Đối Tượng Tác Động</th>
                    <th style="min-width: 280px;">Nội Dung Chi Tiết</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(log, idx) in filteredAuditLogs" :key="idx">
                    <td>
                      <small class="text-muted d-flex align-items-center gap-1">
                        <Clock :size="12" class="text-muted" />
                        <span>{{ log.timestamp || 'Vừa xong' }}</span>
                      </small>
                    </td>
                    <td>
                      <strong class="d-block text-dark">{{ log.actorEmail || currentActorEmail }}</strong>
                      <span class="badge-role-manager" style="font-size: 10px; padding: 2px 6px;">
                        {{ log.actorRole || 'Manager' }}
                      </span>
                    </td>
                    <td>
                      <span class="badge-action-type">
                        {{ log.action }}
                      </span>
                    </td>
                    <td>
                      <strong class="text-dark">{{ log.targetUser || 'Hệ thống' }}</strong>
                    </td>
                    <td>
                      <small class="text-secondary">{{ log.details }}</small>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>
    </div>

    <!-- ================================================================ -->
    <!-- CÁC MODAL HỆ THỐNG DÀNH CHO BAN QUẢN LÝ                         -->
    <!-- ================================================================ -->

    <!-- MODAL THẨM ĐỊNH HỒ SƠ KYC -->
    <div v-if="showKycModal && selectedKycItem" class="modal-backdrop-custom" @click.self="showKycModal = false">
      <div class="modal-dialog-card modal-lg">
        <div class="modal-header-row">
          <h3 class="modal-heading d-flex align-items-center gap-2">
            <ShieldCheck :size="20" class="text-emerald" />
            <span>Thẩm Định Hồ Sơ: {{ selectedKycItem.fullName }} ({{ selectedKycType === 'seller' ? 'Chủ Shop' : 'Tài Xế' }})</span>
          </h3>
          <button class="modal-close-btn" @click="showKycModal = false"><X :size="18" /></button>
        </div>

        <div class="modal-body-scroll">
          <div class="kyc-preview-split">
            <div class="kyc-doc-viewer">
              <img :src="activeKycImage" alt="Giấy tờ thẩm định" class="kyc-full-doc-img" />
              <div class="doc-tabs-switch mt-2">
                <button class="doc-tab-btn" :class="{ active: activeDocTab === 'front' }" @click="activeDocTab = 'front'; activeKycImage = getValidDocImage(selectedKycItem.cccdFrontImage, 'front')">
                  Mặt trước CCCD
                </button>
                <button class="doc-tab-btn" :class="{ active: activeDocTab === 'back' }" @click="activeDocTab = 'back'; activeKycImage = getValidDocImage(selectedKycItem.cccdBackImage, 'back')">
                  Mặt sau CCCD
                </button>
              </div>
            </div>

            <div class="kyc-info-summary">
              <h4 class="mb-2 fw-bold text-dark">Thông Tin Đối Soát</h4>
              <div class="info-kv-row">
                <span class="kv-k">Họ và tên:</span>
                <strong class="kv-v">{{ selectedKycItem.fullName }}</strong>
              </div>
              <div class="info-kv-row">
                <span class="kv-k">Số CCCD:</span>
                <strong class="kv-v text-danger">{{ selectedKycItem.cccdNumber || 'Chưa nhận diện' }}</strong>
              </div>
              <div class="info-kv-row">
                <span class="kv-k">Gmail/SĐT:</span>
                <span class="kv-v">{{ selectedKycItem.email || selectedKycItem.phoneEmail }}</span>
              </div>
              <div class="info-kv-row" v-if="selectedKycType === 'seller'">
                <span class="kv-k">Tên Gian Hàng:</span>
                <strong class="kv-v text-emerald">{{ selectedKycItem.storeName }}</strong>
              </div>
              <div class="info-kv-row" v-if="selectedKycType === 'shipper'">
                <span class="kv-k">Biển số xe:</span>
                <strong class="kv-v text-blue">{{ selectedKycItem.licensePlate }}</strong>
              </div>
            </div>
          </div>
        </div>

        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showKycModal = false">Đóng</button>
          <button class="btn-modal-reject" @click="showRejectReasonPopup = true">
            <XCircle :size="15" class="me-1" />
            <span>Từ Chối Hồ Sơ</span>
          </button>
          <button class="btn-modal-approve" @click="handleApproveKyc">
            <Check :size="15" class="me-1" />
            <span>Phê Duyệt Hồ Sơ</span>
          </button>
        </div>
      </div>
    </div>

    <!-- POPUP TỪ CHỐI HỒ SƠ KYC -->
    <div v-if="showRejectReasonPopup" class="modal-backdrop-custom nested-backdrop">
      <div class="modal-dialog-card modal-sm">
        <div class="modal-header-row">
          <h4 class="modal-heading text-danger">Lý Do Từ Chối Hồ Sơ</h4>
          <button class="modal-close-btn" @click="showRejectReasonPopup = false"><X :size="16" /></button>
        </div>
        <div class="modal-body-scroll">
          <p class="text-muted small mb-2">Chọn nhanh lý do hoặc nhập chi tiết để phản hồi cho đối tác:</p>
          <div class="preset-reasons-list mb-2">
            <button
              v-for="r in presetRejectReasons"
              :key="r"
              type="button"
              class="preset-reason-btn"
              @click="applyPresetReason(r)"
            >
              {{ r }}
            </button>
          </div>
          <textarea v-model="rejectReasonInput" class="modal-form-control" rows="3" placeholder="Nhập lý do từ chối..."></textarea>
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" :disabled="isSubmittingReject" @click="showRejectReasonPopup = false">Hủy</button>
          <button class="btn-modal-reject" :disabled="isSubmittingReject" @click="handleConfirmRejectKyc">
            <Loader2 v-if="isSubmittingReject" :size="14" class="spinner-animated me-1" />
            <span>Xác Nhận Từ Chối</span>
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL KỶ LUẬT TÀI KHOẢN -->
    <div v-if="showDisciplineModal && selectedUserForPunish" class="modal-backdrop-custom" @click.self="showDisciplineModal = false">
      <div class="modal-dialog-card modal-sm">
        <div class="modal-header-row">
          <h4 class="modal-heading text-warning d-flex align-items-center gap-2">
            <AlertTriangle :size="18" class="text-warning" />
            <span>Kỷ Luật: {{ selectedUserForPunish.fullName }}</span>
          </h4>
          <button class="modal-close-btn" @click="showDisciplineModal = false"><X :size="16" /></button>
        </div>
        <div class="modal-body-scroll">
          <div class="modal-field-group mb-3">
            <label class="modal-form-label fw-bold">Mức độ chế tài:</label>
            <select v-model="disciplineLevel" class="modal-form-select">
              <option :value="1">Mức 1: Cảnh cáo & Ghi nhận 1 lần vi phạm</option>
              <option :value="2">Mức 2: Tạm khóa tài khoản 10 ngày</option>
              <option :value="3">Mức 3: Đình chỉ vĩnh viễn (Cấm kinh doanh)</option>
            </select>
          </div>
          <div class="modal-field-group">
            <label class="modal-form-label fw-bold">Lý do vi phạm:</label>
            <textarea v-model="disciplineReason" class="modal-form-control" rows="3" placeholder="Nhập lý do vi phạm quy chế chợ..."></textarea>
          </div>
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showDisciplineModal = false">Hủy</button>
          <button class="btn-modal-reject" @click="handleApplyDiscipline">
            <span>Áp Dụng Kỷ Luật</span>
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL XÁC NHẬN XÓA TÀI KHOẢN -->
    <div v-if="showDeleteModal && selectedUserForDelete" class="modal-backdrop-custom" @click.self="showDeleteModal = false">
      <div class="modal-dialog-card modal-sm">
        <div class="modal-header-row">
          <h4 class="modal-heading text-danger">Xóa Vĩnh Viễn Tài Khoản</h4>
          <button class="modal-close-btn" @click="showDeleteModal = false"><X :size="16" /></button>
        </div>
        <div class="modal-body-scroll">
          <p class="text-danger small mb-2">Thao tác này sẽ xóa vĩnh viễn tài khoản <strong>{{ selectedUserForDelete.fullName }}</strong> ({{ selectedUserForDelete.email }}). Vui lòng gõ chữ <strong>XÓA</strong> để xác nhận:</p>
          <input v-model="deleteConfirmText" type="text" class="modal-form-control" placeholder="Gõ chữ XÓA..." />
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showDeleteModal = false">Hủy</button>
          <button class="btn-modal-delete" @click="handleConfirmDelete">
            <span>Xác Nhận Xóa</span>
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL TỪ CHỐI DUYỆT SẢN PHẨM -->
    <div v-if="showRejectProductModal && selectedProductToReject" class="modal-backdrop-custom" @click.self="showRejectProductModal = false">
      <div class="modal-dialog-card modal-sm">
        <div class="modal-header-row">
          <h4 class="modal-heading text-danger">Từ Chối Sản Phẩm: {{ selectedProductToReject.name }}</h4>
          <button class="modal-close-btn" @click="showRejectProductModal = false"><X :size="16" /></button>
        </div>
        <div class="modal-body-scroll">
          <label class="modal-form-label fw-bold">Lý do từ chối:</label>
          <textarea v-model="productRejectReason" class="modal-form-control" rows="3" placeholder="Nhập lý do sản phẩm không đạt chuẩn..."></textarea>
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" :disabled="isSubmittingRejectProduct" @click="showRejectProductModal = false">Hủy</button>
          <button class="btn-modal-reject" :disabled="isSubmittingRejectProduct" @click="handleConfirmRejectProduct">
            <Loader2 v-if="isSubmittingRejectProduct" :size="14" class="spinner-animated me-1" />
            <span>Xác Nhận Từ Chối</span>
          </button>
        </div>
      </div>
    </div>

    <!-- POPUP XEM PHÓNG TO ẢNH -->
    <div v-if="activeZoomImage" class="modal-backdrop-custom image-zoom-backdrop" @click="activeZoomImage = null">
      <div class="zoom-image-wrapper">
        <img :src="activeZoomImage" alt="Zoom" class="zoomed-img" />
        <button class="zoom-close-btn" @click="activeZoomImage = null"><X :size="20" /></button>
      </div>
    </div>

    <!-- TOAST THÔNG BÁO -->
    <div v-if="showToastState" class="concept-toast-banner" :class="'toast-' + toastType">
      <CheckCircle2 v-if="toastType === 'success'" :size="18" />
      <AlertTriangle v-else-if="toastType === 'warning'" :size="18" />
      <XCircle v-else-if="toastType === 'danger'" :size="18" />
      <Info v-else :size="18" />
      <span>{{ toastMessage }}</span>
    </div>
  </div>
</template>

<style scoped>
/* ================================================================
   BẢNG MÀU THIẾT KẾ DÀNH RIÊNG CHO BAN QUẢN LÝ (EMERALD / TEAL THEME)
   ================================================================ */
.manager-root-layout,
.manager-root-layout * {
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
  box-sizing: border-box;
}

.manager-root-layout {
  min-height: 100vh;
  height: 100vh;
  max-height: 100vh;
  background-color: #f8fafc;
  color: #1e293b;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* 1. TOPBAR */
.manager-topbar {
  height: 64px;
  flex-shrink: 0;
  background-color: #ffffff;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.topbar-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.mobile-menu-toggle-btn {
  display: none;
  background: none;
  border: 1px solid #e2e8f0;
  padding: 6px;
  border-radius: 6px;
  cursor: pointer;
  color: #0f172a;
}

.brand-link {
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 22px;
  font-weight: 800;
}
.brand-zone {
  color: #0f172a;
}
.brand-mart {
  color: #0d9488;
}
.brand-badge-manager {
  font-size: 10px;
  font-weight: 800;
  background: linear-gradient(135deg, #0d9488 0%, #059669 100%);
  color: #ffffff;
  padding: 3px 9px;
  border-radius: 9999px;
  margin-left: 8px;
  letter-spacing: 0.6px;
  box-shadow: 0 2px 4px rgba(13, 148, 136, 0.25);
}

.topbar-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.search-input-pill {
  position: relative;
  display: flex;
  align-items: center;
}
.search-input-pill input {
  background-color: #f1f5f9;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 8px 32px 8px 34px;
  font-size: 13px;
  color: #1e293b;
  width: 260px;
  transition: all 0.2s ease;
}
.search-input-pill input:focus {
  outline: none;
  border-color: #0d9488;
  background-color: #ffffff;
  box-shadow: 0 0 0 3px rgba(13, 148, 136, 0.12);
  width: 320px;
}
.search-icon {
  position: absolute;
  left: 10px;
  color: #94a3b8;
  pointer-events: none;
}
.clear-search-btn {
  position: absolute;
  right: 8px;
  background: none;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.assigned-market-pill-header {
  display: flex;
  align-items: center;
  gap: 6px;
  background-color: #f0fdfa;
  border: 1px solid #ccfbf1;
  color: #0f766e;
  font-size: 12px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 8px;
}

.topbar-icon-btn {
  width: 38px;
  height: 38px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  color: #64748b;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  position: relative;
  transition: all 0.2s ease;
}
.topbar-icon-btn:hover {
  background-color: #f0fdfa;
  color: #0d9488;
  border-color: #99f6e4;
}
.topbar-icon-btn.active {
  background-color: #ccfbf1;
  color: #0f766e;
  border-color: #0d9488;
}

.notif-badge-pill {
  position: absolute;
  top: -4px;
  right: -4px;
  background-color: #ef4444;
  color: #ffffff;
  font-size: 10px;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 9999px;
  border: 2px solid #ffffff;
}
.ping-ring-pill {
  position: absolute;
  inset: -2px;
  border-radius: 9999px;
  border: 2px solid #ef4444;
  animation: ping 1.5s cubic-bezier(0, 0, 0.2, 1) infinite;
}

.topbar-avatar-wrap {
  position: relative;
  cursor: pointer;
}
.topbar-avatar-img {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #0d9488;
}
.manager-online-badge {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 10px;
  height: 10px;
  background-color: #10b981;
  border-radius: 50%;
  border: 2px solid #ffffff;
}

/* 2. BODY LAYOUT & SIDEBAR */
.manager-body-layout {
  display: flex;
  flex: 1;
  height: calc(100vh - 64px);
  overflow: hidden;
}

.manager-sidebar {
  width: 250px;
  flex-shrink: 0;
  background-color: #0f172a;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 12px 0;
  overflow-y: auto;
  scrollbar-width: none;
  -ms-overflow-style: none;
  border-right: 1px solid #1e293b;
}
.manager-sidebar::-webkit-scrollbar {
  display: none;
  width: 0;
  height: 0;
}

.sidebar-mobile-header {
  display: none;
  padding: 10px 14px;
  border-bottom: 1px solid #1e293b;
  align-items: center;
  justify-content: space-between;
}
.sidebar-close-btn {
  background: none;
  border: none;
  color: #94a3b8;
  cursor: pointer;
}

.sidebar-group {
  padding: 0 10px;
  margin-bottom: 10px;
}
.sidebar-heading {
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 1px;
  color: #64748b;
  padding: 0 10px;
  margin-bottom: 5px;
}

.sidebar-nav {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.nav-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6.5px 10px;
  border-radius: 9px;
  color: #94a3b8;
  font-size: 12.5px;
  font-weight: 600;
  cursor: pointer;
  background-color: transparent;
  border: 1px solid transparent;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}
.nav-item:hover {
  background-color: rgba(30, 41, 59, 0.85);
  color: #f8fafc;
  border-color: rgba(51, 65, 85, 0.6);
  transform: translateX(2px);
}
.nav-item.active {
  background: linear-gradient(90deg, rgba(13, 148, 136, 0.25) 0%, rgba(15, 23, 42, 0.95) 100%);
  color: #2dd4bf;
  border: 1px solid rgba(20, 184, 166, 0.4);
  font-weight: 700;
  box-shadow: 0 4px 12px rgba(13, 148, 136, 0.15);
}
.nav-label-box {
  display: flex;
  align-items: center;
  gap: 9px;
}
.nav-icon-wrapper {
  width: 28px;
  height: 28px;
  border-radius: 7px;
  background-color: rgba(255, 255, 255, 0.04);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #94a3b8;
  transition: all 0.2s ease;
  flex-shrink: 0;
}
.nav-item:hover .nav-icon-wrapper {
  background-color: rgba(13, 148, 136, 0.2);
  color: #2dd4bf;
}
.nav-item.active .nav-icon-wrapper {
  background: linear-gradient(135deg, #0d9488 0%, #059669 100%);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(13, 148, 136, 0.4);
}

/* Nav Item Shopping Special */
.nav-item-shopping {
  background: rgba(13, 148, 136, 0.08);
  border: 1px dashed rgba(20, 184, 166, 0.35);
  color: #5eead4;
}
.nav-item-shopping:hover {
  background: linear-gradient(90deg, rgba(13, 148, 136, 0.3) 0%, rgba(13, 148, 136, 0.1) 100%);
  border-color: #14b8a6;
  color: #ffffff;
}
.nav-item-shopping .icon-shopping {
  background-color: rgba(13, 148, 136, 0.25);
  color: #2dd4bf;
}
.nav-arrow {
  color: #475569;
  transition: transform 0.2s ease;
}
.nav-item:hover .nav-arrow {
  color: #94a3b8;
  transform: translateX(2px);
}
.nav-item.active .nav-arrow {
  color: #14b8a6;
}

.nav-count-badge {
  background-color: #ef4444;
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 7px;
  border-radius: 9999px;
}
.nav-count-badge.badge-pending {
  background-color: #f97316;
}

.sidebar-footer-action {
  padding: 10px 12px;
  border-top: 1px solid #1e293b;
}
.sidebar-logout-btn {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
  background-color: #1e293b;
  color: #ef4444;
  border: 1px solid #334155;
  border-radius: 8px;
  padding: 8px 12px;
  font-size: 12.5px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}
.sidebar-logout-btn:hover {
  background-color: #ef4444;
  color: #ffffff;
  border-color: #ef4444;
}

/* 3. MAIN CONTENT */
.manager-main-content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
  background-color: #f8fafc;
}

/* A. HERO PROFILE CARD */
.hero-profile-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 22px 24px;
  margin-bottom: 24px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.02);
}
.hero-top-row {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 20px;
}
.hero-avatar-circle {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  padding: 3px;
  background: linear-gradient(135deg, #0d9488 0%, #10b981 100%);
  flex-shrink: 0;
}
.hero-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #ffffff;
}

.hero-info-column {
  flex: 1;
}
.hero-name-rating {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 6px;
}
.hero-full-name {
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}
.hero-stars-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  background-color: #f0fdfa;
  padding: 3px 8px;
  border-radius: 6px;
}
.stars-gold-wrap {
  display: flex;
  gap: 2px;
}
.reviews-count {
  font-size: 11px;
  font-weight: 700;
  color: #0d9488;
}

.hero-meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 18px;
  font-size: 13px;
  color: #64748b;
}
.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
}
.meta-icon {
  color: #94a3b8;
}
.meta-live-dot {
  color: #10b981;
  animation: pulse 1.8s infinite;
}

/* CHANNELS STRIP */
.hero-channels-strip {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 12px;
  padding-top: 16px;
  border-top: 1px solid #f1f5f9;
}
.channel-metric-cell {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 10px;
  background-color: #f8fafc;
  cursor: pointer;
  transition: all 0.2s ease;
}
.channel-metric-cell:hover {
  background-color: #f0fdfa;
  transform: translateY(-2px);
}
.metric-icon-box {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.box-blue { background-color: #dbeafe; color: #2563eb; }
.box-orange { background-color: #ffedd5; color: #ea580c; }
.box-green { background-color: #dcfce7; color: #16a34a; }
.box-cyan { background-color: #e0f2fe; color: #0284c7; }
.box-purple { background-color: #f3e8ff; color: #9333ea; }
.box-amber { background-color: #fef3c7; color: #d97706; }

.metric-text-group {
  display: flex;
  flex-direction: column;
}
.metric-num {
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
}
.metric-lbl {
  font-size: 11px;
  color: #64748b;
  font-weight: 600;
}

/* B. STAT CARDS */
.stat-cards-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}
.stat-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
}
.stat-info {
  display: flex;
  flex-direction: column;
}
.stat-title-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}
.stat-title {
  font-size: 12px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
}
.trend-pill {
  font-size: 10px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: 9999px;
  display: flex;
  align-items: center;
  gap: 3px;
}
.trend-up { background-color: #dcfce7; color: #15803d; }
.trend-neutral { background-color: #f1f5f9; color: #475569; }

.stat-number {
  font-size: 22px;
  font-weight: 800;
  color: #0f172a;
}
.stat-currency {
  color: #0d9488;
}
.stat-subtext {
  font-size: 11px;
  color: #94a3b8;
  margin-top: 2px;
}

.stat-badge-squircle {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.badge-cyan { background-color: #e0f2fe; color: #0284c7; }
.badge-purple { background-color: #f3e8ff; color: #9333ea; }
.badge-teal { background-color: #ccfbf1; color: #0f766e; }
.badge-yellow { background-color: #fef9c3; color: #ca8a04; }

/* C. ANALYTICS CARDS GRID */
.analytics-cards-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}
.analytics-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 18px;
}
.analytics-card-title {
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  margin-bottom: 16px;
}

.donut-chart-container {
  display: flex;
  align-items: center;
  gap: 16px;
}
.donut-svg-wrapper {
  position: relative;
  width: 120px;
  height: 120px;
  flex-shrink: 0;
}
.donut-svg {
  width: 100%;
  height: 100%;
}
.donut-center-text {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}
.donut-val {
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
}
.donut-lbl {
  font-size: 10px;
  color: #64748b;
}

.donut-legend-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}
.legend-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
}
.legend-color-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.legend-name { color: #64748b; flex: 1; }
.legend-val { color: #0f172a; }

.age-distribution-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.age-label-row {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  margin-bottom: 4px;
}
.age-bracket { color: #475569; font-weight: 600; }
.age-percent { color: #0f172a; font-weight: 700; }
.age-progress-track {
  height: 7px;
  background-color: #f1f5f9;
  border-radius: 9999px;
  overflow: hidden;
}
.age-progress-fill { height: 100%; border-radius: 9999px; }
.fill-teal { background-color: #0d9488; }
.fill-blue { background-color: #2563eb; }
.fill-cyan { background-color: #06b6d4; }
.fill-purple { background-color: #8b5cf6; }

.top-locations-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.location-item-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.loc-text-row {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
}
.loc-name { font-weight: 600; color: #334155; }
.loc-count { color: #64748b; }
.loc-bar-track {
  height: 6px;
  background-color: #f1f5f9;
  border-radius: 9999px;
  overflow: hidden;
}
.loc-bar-fill { height: 100%; border-radius: 9999px; }

/* TAB CONTAINER & TABLES */
.tab-content-container {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.content-header-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 18px 20px;
}
.content-heading {
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}
.content-sub {
  font-size: 13px;
  color: #64748b;
  margin: 4px 0 0 0;
}

.header-icon-circle {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.bg-teal-gradient { background: linear-gradient(135deg, #0d9488 0%, #059669 100%); }

.filter-controls-row {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin-top: 14px;
  padding-top: 14px;
  border-top: 1px solid #f1f5f9;
}
.filter-tabs-pills {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.filter-pill {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  color: #475569;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}
.filter-pill:hover {
  background-color: #f8fafc;
  color: #0f172a;
}
.filter-pill.active {
  background-color: #0d9488;
  color: #ffffff;
  border-color: #0d9488;
}

.status-select-box {
  padding: 6px 12px;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  font-size: 12px;
  color: #334155;
  background-color: #ffffff;
  font-weight: 600;
}

.table-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
}
.table-responsive-wrapper {
  overflow-x: auto;
}
.concept-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}
.concept-table th {
  background-color: #f8fafc;
  color: #475569;
  font-weight: 700;
  text-align: left;
  padding: 12px 16px;
  border-bottom: 1px solid #e2e8f0;
  white-space: nowrap;
}
.concept-table td {
  padding: 14px 16px;
  border-bottom: 1px solid #f1f5f9;
  vertical-align: middle;
}
.concept-table tr:hover td {
  background-color: #f0fdfa;
}

.table-user-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}
.table-avatar {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  object-fit: cover;
  border: 1px solid #e2e8f0;
}
.table-user-cell strong {
  display: block;
  color: #0f172a;
}
.table-user-cell small {
  display: block;
  color: #64748b;
  font-size: 11px;
}

/* Role Badges */
.badge-role-admin { background-color: #fee2e2; color: #dc2626; padding: 2px 7px; border-radius: 6px; font-size: 11px; font-weight: 700; display: inline-flex; align-items: center; gap: 4px; }
.badge-role-manager { background-color: #ccfbf1; color: #0f766e; padding: 2px 7px; border-radius: 6px; font-size: 11px; font-weight: 700; display: inline-flex; align-items: center; gap: 4px; }
.badge-role-seller { background-color: #ffedd5; color: #ea580c; padding: 2px 7px; border-radius: 6px; font-size: 11px; font-weight: 700; display: inline-flex; align-items: center; gap: 4px; }
.badge-role-shipper { background-color: #dcfce7; color: #16a34a; padding: 2px 7px; border-radius: 6px; font-size: 11px; font-weight: 700; display: inline-flex; align-items: center; gap: 4px; }
.badge-role-buyer { background-color: #f1f5f9; color: #475569; padding: 2px 7px; border-radius: 6px; font-size: 11px; font-weight: 600; display: inline-flex; align-items: center; gap: 4px; }

/* Status Pills */
.status-pill {
  font-size: 11px;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 9999px;
  display: inline-flex;
  align-items: center;
}
.status-active { background-color: #dcfce7; color: #15803d; }
.status-pending { background-color: #fef3c7; color: #b45309; }
.status-rejected { background-color: #fee2e2; color: #b91c1c; }
.status-locked { background-color: #ffedd5; color: #c2410c; }
.status-banned { background-color: #1e293b; color: #f8fafc; }

.violation-badge {
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
  background-color: #f1f5f9;
  padding: 2px 8px;
  border-radius: 6px;
}
.violation-badge.has-violation {
  background-color: #fee2e2;
  color: #dc2626;
  font-weight: 700;
}

/* Action Buttons */
.actions-inline-btns {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 6px;
}
.actions-inline-btns.center-actions {
  justify-content: center !important;
  margin: 0 auto;
}
.admin-protected-tag.mx-auto {
  margin-left: auto;
  margin-right: auto;
}
.action-btn {
  width: 30px;
  height: 30px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}
.btn-role { color: #0d9488; }
.btn-role:hover { background-color: #f0fdfa; border-color: #0d9488; }
.btn-punish { color: #f59e0b; }
.btn-punish:hover { background-color: #fef3c7; border-color: #f59e0b; }
.btn-unlock { color: #10b981; }
.btn-unlock:hover { background-color: #dcfce7; border-color: #10b981; }
.btn-delete { color: #ef4444; }
.btn-delete:hover { background-color: #fee2e2; border-color: #ef4444; }

/* Admin Protected Tag (Hiển thị khi ở tài khoản Admin) */
.admin-protected-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  font-weight: 700;
  color: #64748b;
  background-color: #f1f5f9;
  padding: 4px 8px;
  border-radius: 6px;
  border: 1px dashed #cbd5e1;
}

/* NÚT THẨM ĐỊNH & DUYỆT HỒ SƠ KYC ĐẸP MẮT */
.btn-table-action,
.btn-kyc-review {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 15px;
  border-radius: 8px;
  background: linear-gradient(135deg, #0d9488 0%, #059669 100%);
  color: #ffffff;
  font-size: 12px;
  font-weight: 700;
  border: none;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(13, 148, 136, 0.28);
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.btn-table-action:hover,
.btn-kyc-review:hover {
  background: linear-gradient(135deg, #0f766e 0%, #047857 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(13, 148, 136, 0.4);
  color: #ffffff;
}
.btn-table-action:active,
.btn-kyc-review:active {
  transform: translateY(0);
}

/* CHỢ PHỤ TRÁCH VIEW */
.assigned-market-full-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.selected-market-hero-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 20px;
}
.selected-market-title {
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
}
.badge-status-open {
  background-color: #dcfce7;
  color: #15803d;
  font-size: 11px;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 9999px;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}
.dot-online {
  width: 6px;
  height: 6px;
  background-color: #16a34a;
  border-radius: 50%;
}
.badge-market-code {
  background-color: #f1f5f9;
  color: #475569;
  font-size: 11px;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 6px;
}
.selected-market-sub {
  font-size: 13px;
  color: #475569;
}

.bql-assigned-info-box {
  display: flex;
  align-items: center;
  gap: 12px;
  background-color: #f0fdfa;
  border: 1px solid #ccfbf1;
  padding: 10px 14px;
  border-radius: 10px;
}
.bql-icon-badge {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background-color: #0d9488;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.bql-main-title {
  font-size: 13px;
  color: #0f766e;
}
.bql-sub-contact {
  font-size: 12px;
  color: #475569;
  margin-top: 2px;
}
.contact-sep {
  margin: 0 6px;
  color: #cbd5e1;
}

.market-hero-metrics-pill {
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 12px 16px;
  min-width: 220px;
}
.metric-block {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
}
.m-lbl { color: #64748b; }
.m-val { color: #0f172a; }
.stalls-progress-bar {
  height: 7px;
  background-color: #e2e8f0;
  border-radius: 9999px;
  overflow: hidden;
}
.stalls-fill {
  height: 100%;
  background-color: #0d9488;
  border-radius: 9999px;
}

.market-stores-table-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 18px;
}
.stores-section-title {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
}
.stores-section-sub {
  font-size: 12px;
}

.badge-stall-tag {
  background-color: #f0fdfa;
  color: #0f766e;
  border: 1px solid #99f6e4;
  padding: 2px 7px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
}

/* PRODUCT MODERATION STYLES */
.product-toolbar-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 12px 16px;
}
.prod-filter-pills-group {
  display: flex;
  gap: 6px;
}
.prod-pill-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  font-size: 12px;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s ease;
}
.prod-pill-btn:hover { background-color: #f8fafc; }
.prod-pill-btn.active { background-color: #0d9488; color: #ffffff; border-color: #0d9488; }
.prod-search-input-box {
  position: relative;
  width: 280px;
}
.prod-search-control {
  width: 100%;
  padding: 7px 12px 7px 32px;
  font-size: 12px;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
}

.product-info-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}
.product-thumb-wrapper {
  position: relative;
  width: 44px;
  height: 44px;
  border-radius: 8px;
  overflow: hidden;
  cursor: pointer;
  flex-shrink: 0;
  border: 1px solid #e2e8f0;
}
.product-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.thumb-zoom-hint {
  position: absolute;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.4);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s ease;
}
.product-thumb-wrapper:hover .thumb-zoom-hint { opacity: 1; }
.product-name-title { color: #0f172a; font-size: 13px; }
.category-pill-tag { background-color: #f1f5f9; color: #475569; padding: 1px 6px; border-radius: 4px; font-size: 10px; font-weight: 600; }
.unit-pill-tag { background-color: #f0fdfa; color: #0f766e; padding: 1px 6px; border-radius: 4px; font-size: 10px; font-weight: 600; }

.price-amount { font-size: 14px; font-weight: 800; color: #0f172a; }
.price-currency { font-size: 11px; color: #94a3b8; margin-left: 2px; }
.badge-stock-pill { background-color: #f1f5f9; color: #334155; padding: 2px 7px; border-radius: 6px; font-size: 11px; font-weight: 700; }
.badge-stock-pill.stock-low { background-color: #fee2e2; color: #dc2626; }

.btn-row-action {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 5px 10px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  border: none;
  transition: all 0.2s ease;
}
.btn-row-approve { background-color: #dcfce7; color: #15803d; }
.btn-row-approve:hover { background-color: #15803d; color: #ffffff; }
.btn-row-reject { background-color: #fee2e2; color: #dc2626; }
.btn-row-reject:hover { background-color: #dc2626; color: #ffffff; }

/* BỐ CỤC TRẠNG THÁI & AI SCAN (ĐANG MỞ BÁN Ở TRÊN, AI AN TOÀN Ở DƯỚI) */
.product-status-flow {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 5px;
}
.ai-verified-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background-color: #eff6ff;
  color: #2563eb;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  white-space: nowrap;
}
.ai-verified-tag.ai-pending-tag {
  background-color: #fffbeb;
  color: #b45309;
}
.ai-verified-tag.ai-rejected-tag {
  background-color: #fef2f2;
  color: #b91c1c;
}

/* MODALS */
.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background-color: rgba(15, 23, 42, 0.65);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 16px;
}
.nested-backdrop { z-index: 1100; }
.modal-dialog-card {
  background-color: #ffffff;
  border-radius: 14px;
  width: 100%;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
}
.modal-sm { max-width: 420px; }
.modal-md { max-width: 580px; }
.modal-lg { max-width: 780px; }

.modal-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #e2e8f0;
}
.modal-heading {
  font-size: 16px;
  font-weight: 800;
  margin: 0;
  color: #0f172a;
}
.modal-close-btn {
  background: none;
  border: none;
  color: #94a3b8;
  cursor: pointer;
}
.modal-body-scroll {
  padding: 20px;
  overflow-y: auto;
}
.modal-footer-row {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding: 14px 20px;
  border-top: 1px solid #e2e8f0;
  background-color: #f8fafc;
  border-bottom-left-radius: 14px;
  border-bottom-right-radius: 14px;
}

.modal-form-control, .modal-form-select {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 13px;
}
.btn-primary-action {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #0d9488;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 8px 14px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}
.btn-primary-action:hover { background-color: #0f766e; }

.btn-modal-cancel {
  background-color: #ffffff;
  border: 1px solid #cbd5e1;
  color: #475569;
  padding: 8px 14px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}
.btn-modal-approve {
  background-color: #0d9488;
  color: #ffffff;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}
.btn-modal-reject {
  background-color: #fee2e2;
  color: #dc2626;
  border: 1px solid #fecaca;
  padding: 8px 14px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}
.btn-modal-save-role {
  background-color: #0d9488;
  color: #ffffff;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}
.btn-modal-delete {
  background-color: #ef4444;
  color: #ffffff;
  border: none;
  padding: 8px 14px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}

/* KYC SPLIT */
.kyc-preview-split {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}
.kyc-full-doc-img {
  width: 100%;
  max-height: 240px;
  object-fit: contain;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background-color: #f8fafc;
}
.doc-tabs-switch {
  display: flex;
  gap: 6px;
}
.doc-tab-btn {
  flex: 1;
  padding: 6px 10px;
  font-size: 11px;
  font-weight: 600;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  cursor: pointer;
}
.doc-tab-btn.active {
  background-color: #0d9488;
  color: #ffffff;
  border-color: #0d9488;
}

.info-kv-row {
  display: flex;
  justify-content: space-between;
  padding: 6px 0;
  border-bottom: 1px solid #f1f5f9;
  font-size: 12px;
}
.kv-k { color: #64748b; }
.kv-v { color: #0f172a; }

/* ROLE CARDS IN MODAL (THIẾT KẾ ĐỒNG ĐỀU, Ô CHECKBOX RÕ RÀNG, KHÔNG DƯ KHOẢNG TRỐNG) */
.role-cards-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}
.role-select-card {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 16px;
  border: 1.5px solid #e2e8f0;
  border-radius: 12px;
  cursor: pointer;
  background-color: #ffffff;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  min-height: 104px;
}
.role-select-card:hover {
  border-color: #0d9488;
  background-color: #f8fafc;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.04);
}
.role-select-card.selected {
  border-color: #0d9488;
  background-color: #f0fdfa;
  box-shadow: 0 4px 14px rgba(13, 148, 136, 0.12);
}
.role-checkbox {
  width: 20px;
  height: 20px;
  margin-top: 3px;
  accent-color: #0d9488;
  cursor: pointer;
  flex-shrink: 0;
}
.role-card-content {
  flex: 1;
  display: flex;
  flex-direction: column;
}
.role-card-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}
.role-card-header strong {
  font-size: 13.5px;
  font-weight: 700;
  color: #0f172a;
}
.role-icon-box {
  width: 28px;
  height: 28px;
  border-radius: 7px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.bg-blue { background-color: #eff6ff; color: #2563eb; }
.bg-orange { background-color: #fff7ed; color: #ea580c; }
.bg-green { background-color: #f0fdf4; color: #16a34a; }
.bg-teal { background-color: #ccfbf1; color: #0f766e; }
.role-card-desc {
  font-size: 12px;
  color: #64748b;
  line-height: 1.45;
  margin: 0;
}

.preset-buttons-row {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 6px;
}
.preset-btn {
  padding: 4px 10px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
  background-color: #f8fafc;
  color: #334155;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
}
.preset-btn:hover {
  background-color: #0d9488;
  color: #ffffff;
  border-color: #0d9488;
}

/* NOTIFICATION POPOVER (KHÔNG BỊ TRÀN HAY ĐẨY RA NGOÀI MÀN HÌNH) */
.notification-dropdown-wrapper {
  position: relative;
}
.notification-popover {
  position: absolute;
  top: calc(100% + 10px);
  right: -130px;
  width: 360px;
  max-width: calc(100vw - 32px);
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  box-shadow: 0 12px 28px -4px rgba(0, 0, 0, 0.16), 0 8px 12px -6px rgba(0, 0, 0, 0.08);
  overflow: hidden;
  z-index: 500;
}
.popover-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid #f1f5f9;
}
.popover-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
}
.unread-pill {
  font-size: 10px;
  background-color: #ef4444;
  color: #ffffff;
  padding: 1px 6px;
  border-radius: 9999px;
}
.mark-all-read-btn {
  background: none;
  border: none;
  color: #0d9488;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
}
.popover-scroll-body {
  max-height: 280px;
  overflow-y: auto;
}
.notif-item-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px 16px;
  border-bottom: 1px solid #f8fafc;
}
.notif-item-row.unread { background-color: #f0fdfa; }
.notif-type-icon {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  background-color: #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #0d9488;
  flex-shrink: 0;
}
.notif-item-title { font-size: 12px; font-weight: 700; color: #0f172a; }
.notif-item-msg { font-size: 11px; color: #64748b; margin: 2px 0 0 0; }
.popover-footer {
  padding: 10px;
  background-color: #f8fafc;
  border-top: 1px solid #f1f5f9;
}
.view-all-kyc-btn {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  background: none;
  border: none;
  color: #0d9488;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
}

/* TOAST */
.concept-toast-banner {
  position: fixed;
  bottom: 24px;
  right: 24px;
  padding: 12px 18px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  z-index: 2000;
  animation: slideInUp 0.3s ease;
}
.toast-success { background-color: #0d9488; color: #ffffff; }
.toast-warning { background-color: #f59e0b; color: #ffffff; }
.toast-danger { background-color: #ef4444; color: #ffffff; }
.toast-info { background-color: #2563eb; color: #ffffff; }

/* ZOOM */
.image-zoom-backdrop {
  z-index: 2500;
  cursor: zoom-out;
}
.zoom-image-wrapper {
  position: relative;
  max-width: 85vw;
  max-height: 85vh;
}
.zoomed-img {
  max-width: 100%;
  max-height: 85vh;
  border-radius: 12px;
  border: 3px solid #ffffff;
}
.zoom-close-btn {
  position: absolute;
  top: -12px;
  right: -12px;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background-color: #ef4444;
  color: #ffffff;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

@keyframes ping {
  75%, 100% {
    transform: scale(2);
    opacity: 0;
  }
}
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}
@keyframes slideInUp {
  from { transform: translateY(20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

/* RESPONSIVE */
@media (max-width: 1024px) {
  .hero-channels-strip { grid-template-columns: repeat(3, 1fr); }
  .stat-cards-grid { grid-template-columns: repeat(2, 1fr); }
  .analytics-cards-grid { grid-template-columns: 1fr; }
}
@media (max-width: 768px) {
  .mobile-menu-toggle-btn { display: flex; }
  .manager-sidebar {
    position: fixed;
    top: 64px;
    bottom: 0;
    left: 0;
    z-index: 500;
    transform: translateX(-100%);
    transition: transform 0.3s ease;
  }
  .manager-sidebar.is-mobile-open { transform: translateX(0); }
  .sidebar-mobile-header { display: flex; }
  .sidebar-backdrop {
    position: fixed;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 400;
  }
  .search-input-pill input { width: 140px; }
  .search-input-pill input:focus { width: 180px; }
  .hero-channels-strip { grid-template-columns: repeat(2, 1fr); }
  .stat-cards-grid { grid-template-columns: 1fr; }
  .kyc-preview-split { grid-template-columns: 1fr; }
  .role-cards-grid { grid-template-columns: 1fr; }
}
</style>
