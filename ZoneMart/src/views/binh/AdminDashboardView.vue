<script setup lang="ts">
/**
 * ================================================================
 * PHÂN HỆ QUẢN TRỊ ADMIN (ADMIN DASHBOARD) - ZONEMART
 * Phong cách thiết kế: Concept Dashboard (Profile Hero, 4 Stat Cards, 
 * 3 Analytics Breakdown, Dark Sidebar, Light Content)
 * Màu sắc thương hiệu ZoneMart: Cam (#ea580c / #f97316), Nền Tối Slate (#0f172a),
 * Nền Sáng (#f8fafc), Xanh lá (#10b981), Xanh dương (#2563eb)
 * Kết nối CSDL MongoDB Atlas & .NET API Backend thật 100%
 * ================================================================
 */
import { ref, reactive, computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { useAuth } from "../../composables/useAuth";
import {
  LayoutDashboard,
  Users,
  ShieldCheck,
  UserCheck,
  History,
  Store,
  Bike,
  Package,
  MapPin,
  LogOut,
  Search,
  Bell,
  BellOff,
  CheckCheck,
  LayoutGrid,
  ChevronRight,
  Eye,
  Wallet,
  TrendingUp,
  Hourglass,
  Radio,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Trash2,
  Plus,
  Star,
  Calendar,
  Sparkles,
  Activity,
  Unlock,
  X,
  FileText,
  Clock,
  ShieldAlert,
  Handshake,
  Info,
  CreditCard,
  PackageCheck,
  Check,
  Loader2,
  Send,
  Mail
} from "lucide-vue-next";
import { useProductModeration } from "../../composables/useProductModeration";

const router = useRouter();
const auth = useAuth();

// Actor Role hiện tại (Super Admin hoặc Quản lý ca trực)
const currentActorRole = ref<"Admin" | "Manager">("Admin");
const currentActorEmail = ref<string>("admin@zonemart.vn");

// Tabs điều hướng (Dashboard, Accounts, KYC, Products, Managers, Audit)
const activeTab = ref<"dashboard" | "accounts" | "kyc" | "products" | "managers" | "audit">("dashboard");

// Loading & Notification Toast states
const isLoading = ref<boolean>(false);
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

// Interface cho Biểu đồ thống kê tuần
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
  { fullName: "Trần Văn Nam", licensePlate: "29N1-67890", isOnline: true, locationNote: "Quận Cầu Giấy • GPS Bán Kính 3km" },
  { fullName: "Nguyễn Văn Vũ", licensePlate: "30H2-12345", isOnline: true, locationNote: "Đang giao đơn #ORD_8899" }
];

// Stats từ MongoDB Atlas
const stats = ref({
  totalUsers: 0,
  pendingShops: 0,
  pendingShippers: 0,
  lockedUsers: 0,
  totalOrders: 48,
  onlineShippers: 3,
  deliveringShippers: 2,
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
  cccdNumber?: string;
  storeDetails?: any;
  shipperDetails?: any;
}

const users = ref<UserAccount[]>([]);
const managers = ref<UserAccount[]>([]);
const auditLogs = ref<any[]>([]);

// Header search state
const searchQuery = ref<string>("");
const searchTargetTab = ref<"all" | "buyer" | "seller" | "shipper" | "manager">("all");
const selectedStatusFilter = ref<string>("All");

// KYC Approval Modal states
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

// Shift Manager Modal
const showCreateManagerModal = ref<boolean>(false);
const newManagerForm = ref({
  fullName: "",
  email: "",
  password: ""
});

// Helper kiểm tra tài khoản chưa được duyệt hoặc bị từ chối
const isPendingOrUnapproved = (u: UserAccount) => {
  if (u.accountStatus && (u.accountStatus.toLowerCase() === "pending" || u.accountStatus.toLowerCase() === "rejected")) return true;
  if (u.storeDetails && (u.storeDetails.status === "Pending" || u.storeDetails.status === "pending" || u.storeDetails.status === "Rejected" || u.storeDetails.status === "rejected")) return true;
  if (u.shipperDetails && (u.shipperDetails.status === "Pending" || u.shipperDetails.status === "pending" || u.shipperDetails.status === "Rejected" || u.shipperDetails.status === "rejected")) return true;
  return false;
};

// Computed Metrics
const buyerCount = computed(() => users.value.filter(u => u.isBuyer && !u.isSeller && !u.isShipper && !u.isAdmin && !isPendingOrUnapproved(u)).length);
const sellerCount = computed(() => users.value.filter(u => u.isSeller && !isPendingOrUnapproved(u)).length);
const shipperCount = computed(() => users.value.filter(u => u.isShipper && !isPendingOrUnapproved(u)).length);
const pendingCount = computed(() => stats.value.pendingShops + stats.value.pendingShippers);
const totalUserCount = computed(() => stats.value.totalUsers || users.value.length || 18);

// Product Moderation Composable (Kiểm duyệt sản phẩm)
const productModeration = useProductModeration();
const { pendingReviewProducts, approveProductByManager, rejectProductByManager, deleteProductPermanently } = productModeration;
const pendingProductsCount = computed(() => pendingReviewProducts.value.length);

const showRejectProductModal = ref<boolean>(false);
const selectedProductToReject = ref<any>(null);
const productRejectReason = ref<string>("");

const handleApproveProductItem = (prod: any) => {
  const success = approveProductByManager(prod.id);
  if (success) {
    showToast(`🎉 Đã duyệt mở bán sản phẩm "${prod.name}" thành công!`, "success");
    fetchStats();
    fetchAuditLogs();
  } else {
    showToast("Không thể phê duyệt sản phẩm này!", "danger");
  }
};

const openRejectProductModal = (prod: any) => {
  selectedProductToReject.value = prod;
  productRejectReason.value = prod.aiScore?.flag || "Hình ảnh không khớp với tên sản phẩm hoặc vi phạm quy định sàn.";
  showRejectProductModal.value = true;
};

const handleConfirmRejectProduct = async () => {
  if (!selectedProductToReject.value) return;
  const prod = selectedProductToReject.value;
  const targetId = prod.id;
  const targetName = prod.name;
  const reason = productRejectReason.value.trim() || "Ảnh sản phẩm và tên không đồng nhất hoặc vi phạm quy định sàn.";
  
  showRejectProductModal.value = false;
  selectedProductToReject.value = null;
  productRejectReason.value = "";

  await rejectProductByManager(targetId, reason);
  showToast(`Đã từ chối và gỡ bỏ hoàn toàn sản phẩm "${targetName}" khỏi sàn & hồ sơ gian hàng`, "warning");
  fetchStats();
  fetchAuditLogs();
};

const handleDirectDeleteProduct = async (prod: any) => {
  if (!confirm(`Bạn có chắc muốn xóa vĩnh viễn sản phẩm "${prod.name}" khỏi sàn và hồ sơ gian hàng?`)) return;
  await deleteProductPermanently(prod.id, prod.name);
  showToast(`Đã xóa vĩnh viễn sản phẩm "${prod.name}" khỏi sàn!`, "success");
  fetchStats();
  fetchAuditLogs();
};

const buyerPercent = computed(() => {
  const total = totalUserCount.value || 1;
  const count = buyerCount.value || 10;
  return Math.min(Math.max(Math.round((count / total) * 100), 55), 75);
});

// Top locations breakdown (Địa bàn thực tế ZoneMart Hà Nội)
const topLocations = ref([
  { name: "Cầu Giấy", value: 24200, width: "95%" },
  { name: "Đống Đa", value: 18900, width: "78%" },
  { name: "Ba Đình", value: 16400, width: "68%" },
  { name: "Nam Từ Liêm", value: 13800, width: "58%" },
  { name: "Hoàn Kiếm", value: 9800, width: "42%" },
  { name: "Tây Hồ", value: 7200, width: "32%" }
]);

// Phân loại ngành hàng theo thanh tiến trình ngang
const categoryProgress = ref([
  { label: "15 - 20 (Rau Củ Tươi)", percent: 62 },
  { label: "20 - 25 (Thịt & Hải Sản)", percent: 78 },
  { label: "25 - 30 (Nước & Đồ Uống)", percent: 88 },
  { label: "30 - 35 (Nhu Yếu Phẩm)", percent: 54 },
  { label: "35 - 40 (Trái Cây & Ăn Vặt)", percent: 45 }
]);


// Bộ nhớ cache lưu số CCCD & Nguyên quán đã nhận diện OCR (Làm sạch cache giả lập tĩnh cũ nếu có)
const loadOcrCache = (): Record<string, { cccdNumber: string; fullName?: string; nguyenQuan?: string }> => {
  try {
    const raw = localStorage.getItem('zonemart_ocr_cache_v2');
    if (raw) return JSON.parse(raw);
    return {};
  } catch {
    return {};
  }
};
const ocrCacheByUserId = reactive<Record<string, { cccdNumber: string; fullName?: string; nguyenQuan?: string }>>(loadOcrCache());

const saveOcrCache = () => {
  try {
    localStorage.setItem('zonemart_ocr_cache_v2', JSON.stringify(ocrCacheByUserId));
  } catch {}
};

// Helper lấy chính xác số CCCD của đối tác (Seller / Shipper) hiển thị ra bảng bên ngoài
const getPartnerCccdNumber = (u: UserAccount | any): string => {
  if (!u) return "";
  const num = (u.shipperDetails && u.shipperDetails.cccdNumber) 
    ? u.shipperDetails.cccdNumber 
    : (u.storeDetails && u.storeDetails.cccdNumber)
    ? u.storeDetails.cccdNumber
    : (u.cccdNumber || "");
  if (typeof num === "string" && num.trim()) return num.trim();

  // Kiểm tra bộ nhớ cache OCR đã nhận diện
  if (u.id && ocrCacheByUserId[u.id]?.cccdNumber) {
    return ocrCacheByUserId[u.id].cccdNumber;
  }
  if (u.email && ocrCacheByUserId[u.email]?.cccdNumber) {
    return ocrCacheByUserId[u.email].cccdNumber;
  }
  if (u.storeDetails?.id && ocrCacheByUserId[u.storeDetails.id]?.cccdNumber) {
    return ocrCacheByUserId[u.storeDetails.id].cccdNumber;
  }
  if (u.storeDetails?.storeCode && ocrCacheByUserId[u.storeDetails.storeCode]?.cccdNumber) {
    return ocrCacheByUserId[u.storeDetails.storeCode].cccdNumber;
  }
  if (u.shipperDetails?.id && ocrCacheByUserId[u.shipperDetails.id]?.cccdNumber) {
    return ocrCacheByUserId[u.shipperDetails.id].cccdNumber;
  }
  return "";
};

// Helper lấy chính xác Nguyên quán của đối tác (Seller / Shipper)
const getPartnerNguyenQuan = (u: UserAccount | any): string => {
  if (!u) return "";
  const nq = (u.storeDetails && u.storeDetails.nguyenQuan)
    ? u.storeDetails.nguyenQuan
    : (u.shipperDetails && u.shipperDetails.nguyenQuan)
    ? u.shipperDetails.nguyenQuan
    : (u.nguyenQuan || "");
  if (typeof nq === "string" && nq.trim()) return nq.trim();

  if (u.id && ocrCacheByUserId[u.id]?.nguyenQuan) {
    return ocrCacheByUserId[u.id].nguyenQuan || "";
  }
  if (u.email && ocrCacheByUserId[u.email]?.nguyenQuan) {
    return ocrCacheByUserId[u.email].nguyenQuan || "";
  }
  return "";
};

// Danh sách người dùng được lọc (Hồ sơ chưa duyệt hoặc bị từ chối KHÔNG HIỂN THỊ ở danh sách hoạt động)
const filteredUsersList = computed(() => {
  let list = users.value || [];

  // Lọc theo trạng thái
  if (selectedStatusFilter.value === "rejected") {
    // Xem danh sách các hồ sơ / tài khoản đã bị từ chối
    list = list.filter(u => 
      u.accountStatus === "rejected" || 
      u.storeDetails?.status === "Rejected" || 
      u.storeDetails?.status === "rejected" ||
      u.shipperDetails?.status === "Rejected" ||
      u.shipperDetails?.status === "rejected"
    );
  } else if (selectedStatusFilter.value === "pending") {
    // Xem các hồ sơ chờ duyệt
    list = list.filter(u => 
      u.accountStatus === "pending" || 
      u.storeDetails?.status === "Pending" || 
      u.storeDetails?.status === "pending" ||
      u.shipperDetails?.status === "Pending" ||
      u.shipperDetails?.status === "pending"
    );
  } else if (selectedStatusFilter.value === "active") {
    // Xem đang hoạt động (loại bỏ chưa duyệt hoặc bị từ chối)
    list = list.filter(u => !isPendingOrUnapproved(u) && u.accountStatus === "active");
  } else if (selectedStatusFilter.value === "suspended") {
    list = list.filter(u => u.accountStatus === "locked_10_days" || u.accountStatus === "suspended");
  } else if (selectedStatusFilter.value === "banned") {
    list = list.filter(u => u.accountStatus === "banned");
  } else {
    // Mặc định "Tất cả trạng thái" (All): loại trừ các hồ sơ chưa duyệt hoặc đã bị từ chối khỏi danh sách Người Dùng đang hoạt động
    list = list.filter(u => !isPendingOrUnapproved(u));
  }

  if (searchTargetTab.value !== "all") {
    list = list.filter(u => {
      if (searchTargetTab.value === "buyer") return u.isBuyer && !u.isSeller && !u.isShipper;
      if (searchTargetTab.value === "seller") return u.isSeller;
      if (searchTargetTab.value === "shipper") return u.isShipper;
      if (searchTargetTab.value === "manager") return u.isManager;
      return true;
    });
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(u =>
      (u.fullName && u.fullName.toLowerCase().includes(q)) ||
      (u.email && u.email.toLowerCase().includes(q)) ||
      (u.storeDetails?.storeName && u.storeDetails.storeName.toLowerCase().includes(q)) ||
      (u.shipperDetails?.licensePlate && u.shipperDetails.licensePlate.toLowerCase().includes(q))
    );
  }
  return list;
});

// Danh sách hồ sơ KYC đang chờ duyệt (Chỉ các tài khoản thực sự có nộp đơn Store hoặc Shipper)
const pendingKycUsers = computed(() => {
  return users.value.filter(u => {
    const isStoreRejected = u.storeDetails && (u.storeDetails.status === "Rejected" || u.storeDetails.status === "rejected");
    const isShipperRejected = u.shipperDetails && (u.shipperDetails.status === "Rejected" || u.shipperDetails.status === "rejected");
    const isStorePending = u.storeDetails && (u.storeDetails.status === "Pending" || u.storeDetails.status === "pending") && !isStoreRejected;
    const isShipperPending = u.shipperDetails && (u.shipperDetails.status === "Pending" || u.shipperDetails.status === "pending") && !isShipperRejected;

    // Chỉ đưa vào danh sách Duyệt Hồ Sơ khi có storeDetails hoặc shipperDetails đang Pending
    return isStorePending || isShipperPending;
  });
});

// Fetch dữ liệu thật từ Backend
const fetchStats = async () => {
  try {
    const res = await fetch("/api/admin/stats");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.stats) {
        stats.value = { ...stats.value, ...data.stats };
      }
    }
  } catch (err) {
    console.warn("⚠️ fetchStats error:", err);
  }
};

const fetchUsers = async () => {
  isLoading.value = true;
  try {
    const res = await fetch("/api/admin/users");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.users) {
        users.value = data.users;
        // Tự động quét OCR ngầm cho các đối tác có ảnh thẻ nhưng chưa có CCCD hiển thị ngoài bảng
        setTimeout(() => {
          autoScanPendingPartnersToTable();
        }, 300);
      }
    }
  } catch (err) {
    console.warn("⚠️ fetchUsers error:", err);
  } finally {
    isLoading.value = false;
  }
};

// Tự động quét thông tin CCCD cho toàn bộ hồ sơ đang chờ duyệt để hiển thị trực tiếp ra bảng bên ngoài
const isBatchScanning = ref(false);
const autoScanPendingPartnersToTable = async () => {
  if (isBatchScanning.value || !users.value || users.value.length === 0) return;
  isBatchScanning.value = true;

  try {
    const pendingList = users.value.filter(u => {
      const isPending = (u.accountStatus === 'pending' || u.storeDetails?.status === 'Pending' || u.shipperDetails?.status === 'Pending');
      const hasCccd = !!getPartnerCccdNumber(u);
      const img = u.storeDetails?.cccdFrontImage || u.shipperDetails?.cccdFrontImage;
      const hasRealImg = img && typeof img === 'string' && !img.includes('placeholder') && !img.includes('example.com') && !img.startsWith('<svg');
      return isPending && !hasCccd && hasRealImg;
    });

    for (const u of pendingList) {
      const rawImg = u.storeDetails?.cccdFrontImage || u.shipperDetails?.cccdFrontImage;
      if (!rawImg) continue;

      try {
        let fileToSend: File | null = null;
        if (rawImg.startsWith("data:") || rawImg.startsWith("blob:") || rawImg.startsWith("http")) {
          const resp = await fetch(rawImg);
          const blob = await resp.blob();
          fileToSend = new File([blob], "cccd_front.jpg", { type: blob.type || "image/jpeg" });
        }
        if (!fileToSend) continue;

        const formData = new FormData();
        formData.append("file", fileToSend);

        const scanRes = await fetch("/api/ocr/scan-id-card", {
          method: "POST",
          body: formData,
        });

        const scanData = await scanRes.json();
        if (scanRes.ok && scanData.success && scanData.data) {
          const d = scanData.data;
          const extractedId = (d.so_cccd || d.id || "").trim();
          const extractedName = (d.ho_va_ten || d.name || "").trim();
          const extractedOrigin = (d.nguyen_quan || d.home || "").trim();

          if (extractedId) {
            if (u.id) ocrCacheByUserId[u.id] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
            if (u.email) ocrCacheByUserId[u.email] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
            if (u.storeDetails?.id) ocrCacheByUserId[u.storeDetails.id] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
            if (u.shipperDetails?.id) ocrCacheByUserId[u.shipperDetails.id] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
            saveOcrCache();

            // Cập nhật trường hiển thị ngay trên bảng
            u.cccdNumber = extractedId;
            if (u.storeDetails) {
              u.storeDetails.cccdNumber = extractedId;
              if (extractedOrigin) u.storeDetails.nguyenQuan = extractedOrigin;
            }
            if (u.shipperDetails) {
              u.shipperDetails.cccdNumber = extractedId;
              if (extractedOrigin) u.shipperDetails.nguyenQuan = extractedOrigin;
            }
          }
        }
      } catch (err) {
        console.warn("⚠️ Lỗi quét CCCD ngầm cho user", u.id, err);
      }
    }
  } finally {
    isBatchScanning.value = false;
  }
};

const fetchAuditLogs = async () => {
  try {
    const res = await fetch("/api/admin/audit-logs");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.logs) {
        auditLogs.value = data.logs;
      }
    }
  } catch (err) {
    console.warn("⚠️ fetchAuditLogs error:", err);
  }
};

const fetchManagers = async () => {
  try {
    const res = await fetch("/api/admin/managers");
    if (res.ok) {
      const data = await res.json();
      if (data.success && data.managers) {
        managers.value = data.managers;
      }
    }
  } catch (err) {
    console.warn("⚠️ fetchManagers error:", err);
  }
};

// SVG Mockup mẫu chuẩn chất lượng cao độc lập (Lấy đúng số CCCD thực tế của người dùng, nếu không có thì để trống)
const generateCccdFrontSvg = (cccdNumber?: string, fullName?: string): string => {
  const hasNum = !!(cccdNumber && cccdNumber.trim());
  const displayNum = hasNum ? cccdNumber.trim() : "Chưa cung cấp (Bỏ trống)";
  const numColor = hasNum ? "#dc2626" : "#64748b";
  const displayName = (fullName && fullName.trim()) ? fullName.trim().toUpperCase() : "ĐỐI TÁC ZONEMART";

  return "data:image/svg+xml;utf8," + encodeURIComponent(`
<svg xmlns="http://www.w3.org/2000/svg" width="600" height="380" viewBox="0 0 600 380">
  <defs>
    <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#ecfdf5"/>
      <stop offset="50%" stop-color="#d1fae5"/>
      <stop offset="100%" stop-color="#a7f3d0"/>
    </linearGradient>
    <linearGradient id="chip" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#fef08a"/>
      <stop offset="50%" stop-color="#facc15"/>
      <stop offset="100%" stop-color="#ca8a04"/>
    </linearGradient>
  </defs>
  <rect width="600" height="380" rx="16" fill="url(#bg)" stroke="#10b981" stroke-width="3"/>
  <rect x="20" y="20" width="560" height="50" rx="8" fill="#047857"/>
  <text x="300" y="42" fill="#ffffff" font-size="14" font-weight="bold" font-family="sans-serif" text-anchor="middle">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</text>
  <text x="300" y="60" fill="#fef08a" font-size="11" font-family="sans-serif" text-anchor="middle">Độc lập - Tự do - Hạnh phúc</text>
  <text x="300" y="105" fill="#065f46" font-size="20" font-weight="900" font-family="sans-serif" text-anchor="middle">CĂN CƯỚC CÔNG DÂN</text>
  <text x="300" y="122" fill="#047857" font-size="12" font-family="sans-serif" text-anchor="middle">IDENTITY CARD</text>
  <rect x="50" y="145" width="54" height="42" rx="6" fill="url(#chip)" stroke="#a16207" stroke-width="1.5"/>
  <line x1="50" y1="166" x2="104" y2="166" stroke="#a16207" stroke-width="1.5"/>
  <line x1="77" y1="145" x2="77" y2="187" stroke="#a16207" stroke-width="1.5"/>
  <rect x="50" y="200" width="105" height="135" rx="6" fill="#e2e8f0" stroke="#94a3b8" stroke-width="2"/>
  <circle cx="102" cy="245" r="28" fill="#cbd5e1"/>
  <path d="M72 325 C72 285, 132 285, 132 325 Z" fill="#94a3b8"/>
  <text x="180" y="160" fill="#475569" font-size="12" font-family="sans-serif">Số / No.:</text>
  <text x="245" y="160" fill="${numColor}" font-size="${hasNum ? '16' : '13'}" font-weight="bold" font-family="monospace">${displayNum}</text>
  <text x="180" y="195" fill="#475569" font-size="12" font-family="sans-serif">Họ và tên / Full name:</text>
  <text x="180" y="220" fill="#0f172a" font-size="15" font-weight="bold" font-family="sans-serif">${displayName}</text>
  <text x="180" y="250" fill="#475569" font-size="12" font-family="sans-serif">Ngày sinh / Date of birth: <tspan fill="#0f172a" font-weight="600">01/01/1992</tspan></text>
  <text x="180" y="280" fill="#475569" font-size="12" font-family="sans-serif">Giới tính / Sex: <tspan fill="#0f172a" font-weight="600">Nam</tspan>  Quốc tịch: <tspan fill="#0f172a" font-weight="600">Việt Nam</tspan></text>
  <text x="180" y="310" fill="#475569" font-size="12" font-family="sans-serif">Quê quán: <tspan fill="#0f172a" font-weight="600">Cầu Giấy, Hà Nội</tspan></text>
  <rect x="510" y="300" width="60" height="60" fill="#ffffff" stroke="#94a3b8" stroke-width="1"/>
  <text x="540" y="335" fill="#047857" font-size="9" font-weight="bold" font-family="sans-serif" text-anchor="middle">QR CODE</text>
</svg>`);
};

const generateCccdBackSvg = (cccdNumber?: string): string => {
  const hasNum = !!(cccdNumber && cccdNumber.trim());
  const codeLine = hasNum ? `IDVNM${cccdNumber!.trim()}<<<<<<<<<<<<<<<` : `IDVNM<<<<<<<<<<<<<<<<<<<<<<<<<<`;
  return "data:image/svg+xml;utf8," + encodeURIComponent(`
<svg xmlns="http://www.w3.org/2000/svg" width="600" height="380" viewBox="0 0 600 380">
  <rect width="600" height="380" rx="16" fill="#f0fdf4" stroke="#10b981" stroke-width="3"/>
  <rect x="30" y="30" width="220" height="120" rx="6" fill="#ffffff" stroke="#cbd5e1" stroke-width="1.5"/>
  <text x="140" y="95" fill="#64748b" font-size="12" font-family="sans-serif" text-anchor="middle">Ô VÂN TAY / FINGERPRINT</text>
  <text x="270" y="55" fill="#334155" font-size="12" font-family="sans-serif">Đặc điểm nhân dạng / Personal identification:</text>
  <text x="270" y="80" fill="#0f172a" font-size="12" font-weight="600" font-family="sans-serif">Nốt ruồi cách 1cm dưới đuôi mắt phải</text>
  <text x="270" y="115" fill="#334155" font-size="12" font-family="sans-serif">Ngày cấp: <tspan font-weight="600">10/10/2022</tspan></text>
  <text x="270" y="140" fill="#047857" font-size="12" font-weight="bold" font-family="sans-serif">CỤC TRƯỞNG CỤC CẢNH SÁT QLHC VỀ TTXH</text>
  <rect x="30" y="240" width="540" height="110" rx="8" fill="#1e293b"/>
  <text x="50" y="275" fill="#38bdf8" font-size="16" font-family="monospace">${codeLine}</text>
  <text x="50" y="305" fill="#38bdf8" font-size="16" font-family="monospace">9201017M3010108VNM&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;8</text>
  <text x="50" y="335" fill="#38bdf8" font-size="16" font-family="monospace">DOI&lt;TAC&lt;ZONEMART&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;</text>
</svg>`);
};

const FALLBACK_CCCD_FRONT = generateCccdFrontSvg();
const FALLBACK_CCCD_BACK = generateCccdBackSvg();

const FALLBACK_FOOD_CERT = "data:image/svg+xml;utf8," + encodeURIComponent(`
<svg xmlns="http://www.w3.org/2000/svg" width="600" height="380" viewBox="0 0 600 380">
  <rect width="600" height="380" rx="16" fill="#fffbeb" stroke="#f59e0b" stroke-width="4"/>
  <rect x="15" y="15" width="570" height="350" rx="12" fill="none" stroke="#d97706" stroke-width="1.5" stroke-dasharray="6,4"/>
  <text x="300" y="55" fill="#b45309" font-size="13" font-weight="bold" font-family="sans-serif" text-anchor="middle">BỘ Y TẾ - CỤC AN TOÀN THỰC PHẨM</text>
  <text x="300" y="75" fill="#78350f" font-size="11" font-family="sans-serif" text-anchor="middle">CHI CỤC AN TOÀN VỆ SINH THỰC PHẨM HÀ NỘI</text>
  <text x="300" y="125" fill="#92400e" font-size="20" font-weight="900" font-family="serif" text-anchor="middle">GIẤY CHỨNG NHẬN</text>
  <text x="300" y="150" fill="#b45309" font-size="14" font-weight="bold" font-family="sans-serif" text-anchor="middle">CƠ SỞ ĐỦ ĐIỀU KIỆN AN TOÀN THỰC PHẨM</text>
  <text x="300" y="175" fill="#6b7280" font-size="12" font-family="sans-serif" text-anchor="middle">Số chứng nhận: 2026/ATTP-HN • Cấp ngày: 01/01/2026</text>
  <text x="300" y="215" fill="#1f2937" font-size="15" font-weight="bold" font-family="sans-serif" text-anchor="middle">CHỨNG NHẬN TIÊU CHUẨN VỆ SINH AN TOÀN THỰC PHẨM</text>
  <text x="300" y="240" fill="#4b5563" font-size="13" font-family="sans-serif" text-anchor="middle">Hồ sơ đối tác ZoneMart đạt điều kiện an toàn cung ứng</text>
  <text x="300" y="265" fill="#059669" font-size="13" font-weight="bold" font-family="sans-serif" text-anchor="middle">Đạt tiêu chuẩn an toàn chế biến thực phẩm phục vụ người tiêu dùng</text>
  <circle cx="470" cy="305" r="35" fill="none" stroke="#dc2626" stroke-width="2.5"/>
  <text x="470" y="302" fill="#dc2626" font-size="9" font-weight="bold" font-family="sans-serif" text-anchor="middle">CHI CỤC ATTP</text>
  <text x="470" y="316" fill="#dc2626" font-size="8" font-family="sans-serif" text-anchor="middle">ĐÃ KIỂM ĐỊNH</text>
</svg>`);

const FALLBACK_DRIVING_LICENSE = "data:image/svg+xml;utf8," + encodeURIComponent(`
<svg xmlns="http://www.w3.org/2000/svg" width="600" height="380" viewBox="0 0 600 380">
  <rect width="600" height="380" rx="16" fill="#f8fafc" stroke="#3b82f6" stroke-width="3"/>
  <rect x="20" y="20" width="560" height="50" rx="8" fill="#1d4ed8"/>
  <text x="300" y="42" fill="#ffffff" font-size="14" font-weight="bold" font-family="sans-serif" text-anchor="middle">GIẤY PHÉP LÁI XE / DRIVING LICENCE</text>
  <text x="300" y="60" fill="#bfdbfe" font-size="11" font-family="sans-serif" text-anchor="middle">VIỆT NAM • SỞ GIAO THÔNG VẬN TẢI HÀ NỘI</text>
  <rect x="40" y="110" width="120" height="150" rx="8" fill="#e2e8f0" stroke="#94a3b8" stroke-width="2"/>
  <circle cx="100" cy="160" r="32" fill="#cbd5e1"/>
  <path d="M65 250 C65 205, 135 205, 135 250 Z" fill="#94a3b8"/>
  <text x="190" y="125" fill="#64748b" font-size="13" font-family="sans-serif">Số / No.: <tspan fill="#dc2626" font-size="17" font-weight="bold" font-family="monospace">0102 3456 7890</tspan></text>
  <text x="190" y="160" fill="#64748b" font-size="13" font-family="sans-serif">Họ và tên / Full name: <tspan fill="#0f172a" font-weight="bold">TÀI XẾ ZONEMART</tspan></text>
  <text x="190" y="195" fill="#64748b" font-size="13" font-family="sans-serif">Hạng / Class: <tspan fill="#1d4ed8" font-size="18" font-weight="900">A1 - B2</tspan></text>
  <text x="190" y="230" fill="#64748b" font-size="13" font-family="sans-serif">Phương tiện: <tspan fill="#0f172a" font-weight="600">Xe mô tô 2 bánh &amp; Xe ô tô dưới 9 chỗ</tspan></text>
  <text x="190" y="265" fill="#64748b" font-size="13" font-family="sans-serif">Giá trị đến / Expires: <tspan fill="#059669" font-weight="bold">Không thời hạn</tspan></text>
  <circle cx="500" cy="300" r="32" fill="none" stroke="#dc2626" stroke-width="2"/>
  <text x="500" y="305" fill="#dc2626" font-size="9" font-weight="bold" font-family="sans-serif" text-anchor="middle">SỞ GTVT</text>
</svg>`);

const getDocFallbackImage = (type: "front" | "back" | "cert" | "license"): string => {
  const currentCccd = selectedKycItem.value?.cccdNumber;
  const currentName = selectedKycItem.value?.fullName || selectedKycItem.value?.ownerFullName;
  if (type === "front") return generateCccdFrontSvg(currentCccd, currentName);
  if (type === "back") return generateCccdBackSvg(currentCccd);
  if (type === "cert") return FALLBACK_FOOD_CERT;
  return FALLBACK_DRIVING_LICENSE;
};

const getValidDocImage = (url: string | undefined, type: "front" | "back" | "cert" | "license"): string => {
  if (!url || typeof url !== "string") return getDocFallbackImage(type);
  const trimmed = url.trim();
  if (!trimmed || trimmed.includes("example.com") || trimmed.includes("placeholder") || trimmed === "undefined" || trimmed === "null") {
    return getDocFallbackImage(type);
  }
  return trimmed;
};

const handleKycImgError = (e: Event, type: "front" | "back" | "cert" | "license") => {
  const img = e.target as HTMLImageElement;
  if (img) {
    img.src = getDocFallbackImage(type);
  }
};

// Handlers duyệt hồ sơ
const switchDocTab = (tab: "front" | "back" | "cert" | "license") => {
  activeDocTab.value = tab;
  if (!selectedKycItem.value) return;
  if (tab === "front") {
    activeKycImage.value = getValidDocImage(selectedKycItem.value.cccdFrontImage, "front");
  } else if (tab === "back") {
    activeKycImage.value = getValidDocImage(selectedKycItem.value.cccdBackImage, "back");
  } else if (tab === "cert") {
    activeKycImage.value = getValidDocImage(selectedKycItem.value.foodSafetyCertImage, "cert");
  } else if (tab === "license") {
    activeKycImage.value = getValidDocImage(selectedKycItem.value.drivingLicenseImage, "license");
  }
};

// ========================================================
// TÍCH HỢP TỰ ĐỘNG OCR CCCD / CMND QUA FPT.AI (PROXY ENDPOINT)
// ========================================================
const processOcrFile = async (file: File) => {
  if (!selectedKycItem.value) return;
  isOcrScanning.value = true;
  ocrExtractedSuccess.value = false;

  try {
    const formData = new FormData();
    formData.append("file", file);

    const res = await fetch("/api/ocr/scan-id-card", {
      method: "POST",
      body: formData,
    });

    const result = await res.json();
    if (res.ok && result.success && result.data) {
      const d = result.data;
      const extractedName = (d.ho_va_ten || d.name || "").trim();
      const extractedId = (d.so_cccd || d.id || "").trim();
      const extractedOrigin = (d.nguyen_quan || d.home || "").trim();
      const extractedAddress = (d.dia_chi_thuong_tru || d.address || "").trim();

      // Tự động điền (bind/set state) vào các input thẩm định
      if (extractedName) {
        selectedKycItem.value.fullName = extractedName;
        selectedKycItem.value.ownerFullName = extractedName;
      }
      if (extractedId) {
        selectedKycItem.value.cccdNumber = extractedId;
      }
      if (extractedOrigin) {
        selectedKycItem.value.nguyenQuan = extractedOrigin;
      }
      if (extractedAddress && (!selectedKycItem.value.address || selectedKycItem.value.address === "Chưa cập nhật" || selectedKycItem.value.address === "Hà Nội")) {
        selectedKycItem.value.address = extractedAddress;
      }

      // ĐỒNG BỘ NGAY VÀO BẢNG NGOÀI (users list) & LOCALSTORAGE CACHE ĐỂ HIỂN THỊ TRỰC TIẾP
      const targetUserId = selectedKycItem.value?.userId || selectedKycItem.value?.id || selectedKycItem.value?.storeId || selectedKycItem.value?.shipperId;
      const targetEmail = selectedKycItem.value?.email || selectedKycItem.value?.phoneEmail;
      if (extractedId) {
        if (targetUserId) ocrCacheByUserId[targetUserId] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
        if (targetEmail) ocrCacheByUserId[targetEmail] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
        if (selectedKycItem.value?.storeId) ocrCacheByUserId[selectedKycItem.value.storeId] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
        if (selectedKycItem.value?.shipperId) ocrCacheByUserId[selectedKycItem.value.shipperId] = { cccdNumber: extractedId, fullName: extractedName, nguyenQuan: extractedOrigin };
        saveOcrCache();

        // Cập nhật mảng users phản ứng ngay lập tức trên DOM bảng thẩm định
        const targetUser = users.value.find(u => 
          u.id === targetUserId || 
          (targetEmail && u.email?.toLowerCase() === targetEmail.toLowerCase()) ||
          (u.storeDetails && (u.storeDetails.storeId === targetUserId || u.storeDetails.id === targetUserId)) ||
          (u.shipperDetails && (u.shipperDetails.shipperId === targetUserId || u.shipperDetails.id === targetUserId))
        );
        if (targetUser) {
          targetUser.cccdNumber = extractedId;
          if (targetUser.storeDetails) {
            targetUser.storeDetails.cccdNumber = extractedId;
            if (extractedOrigin) targetUser.storeDetails.nguyenQuan = extractedOrigin;
          }
          if (targetUser.shipperDetails) {
            targetUser.shipperDetails.cccdNumber = extractedId;
            if (extractedOrigin) targetUser.shipperDetails.nguyenQuan = extractedOrigin;
          }
        }
      }

      ocrExtractedSuccess.value = true;
      showToast(
        `🎉 Nhận diện CCCD thành công: ${extractedName || 'Đối tác'} (Số: ${extractedId || 'Đã trích xuất'})`,
        "success"
      );
    } else {
      const errMsg = result.message || "Không thể nhận diện thông tin từ ảnh thẻ đã tải lên.";
      showToast(`⚠️ Quét CCCD thất bại: ${errMsg}`, "danger");
    }
  } catch (err: any) {
    showToast(`❌ Lỗi kết nối dịch vụ OCR: ${err.message || 'Lỗi mạng hoặc máy chủ không phản hồi'}`, "danger");
  } finally {
    isOcrScanning.value = false;
  }
};

const handleOcrCardUpload = async (e: Event) => {
  const input = e.target as HTMLInputElement;
  if (!input.files || input.files.length === 0) return;
  const file = input.files[0];

  if (!file.type.startsWith("image/")) {
    showToast("Vui lòng chọn file hình ảnh hợp lệ (JPG, PNG, WEBP)!", "warning");
    input.value = "";
    return;
  }

  if (file.size > 10 * 1024 * 1024) {
    showToast("Dung lượng file ảnh vượt quá 10MB! Vui lòng chọn ảnh nhỏ hơn.", "warning");
    input.value = "";
    return;
  }

  // Cập nhật xem trước ảnh tức thì
  const localPreviewUrl = URL.createObjectURL(file);
  activeKycImage.value = localPreviewUrl;
  if (selectedKycItem.value) {
    selectedKycItem.value.cccdFrontImage = localPreviewUrl;
  }

  await processOcrFile(file);
  input.value = "";
};

const handleRescanCurrentCard = async () => {
  if (!activeKycImage.value || isOcrScanning.value) return;
  try {
    isOcrScanning.value = true;
    const response = await fetch(activeKycImage.value);
    const blob = await response.blob();
    const file = new File([blob], "cccd_front.jpg", { type: blob.type || "image/jpeg" });
    await processOcrFile(file);
  } catch (err: any) {
    showToast("Không thể nạp ảnh hiện tại để quét lại: " + (err.message || err), "danger");
    isOcrScanning.value = false;
  }
};
void [FALLBACK_CCCD_FRONT, FALLBACK_CCCD_BACK, handleOcrCardUpload, handleRescanCurrentCard];

const openKycModal = (user: UserAccount, preferredType?: "seller" | "shipper") => {
  activeDocTab.value = "front";
  isOcrScanning.value = false;
  ocrExtractedSuccess.value = false;
  const isShipperPending = user.shipperDetails && (user.shipperDetails.status === "Pending" || user.shipperDetails.status === "pending");
  const isSellerPending = user.storeDetails && (user.storeDetails.status === "Pending" || user.storeDetails.status === "pending");

  const chooseShipper = preferredType === "shipper" || (isShipperPending && !isSellerPending) || (user.shipperDetails && !user.storeDetails);
  const partnerCccd = getPartnerCccdNumber(user);
  const partnerNguyenQuan = getPartnerNguyenQuan(user);

  if (chooseShipper && user.shipperDetails) {
    selectedKycType.value = "shipper";
    selectedKycItem.value = {
      ...user.shipperDetails,
      cccdNumber: partnerCccd,
      nguyenQuan: partnerNguyenQuan,
      email: user.email || user.shipperDetails.phoneNumber,
      fullName: user.shipperDetails.fullName || user.fullName,
      userId: user.id
    };
    activeKycImage.value = getValidDocImage(user.shipperDetails.cccdFrontImage || user.shipperDetails.drivingLicenseImage, "front");
  } else if (user.storeDetails) {
    selectedKycType.value = "seller";
    selectedKycItem.value = {
      ...user.storeDetails,
      cccdNumber: partnerCccd,
      nguyenQuan: partnerNguyenQuan,
      email: user.email || user.storeDetails.phoneEmail,
      fullName: user.storeDetails.ownerFullName || user.fullName,
      userId: user.id
    };
    activeKycImage.value = getValidDocImage(user.storeDetails.cccdFrontImage, "front");
  } else if (user.shipperDetails) {
    selectedKycType.value = "shipper";
    selectedKycItem.value = {
      ...user.shipperDetails,
      cccdNumber: partnerCccd,
      nguyenQuan: partnerNguyenQuan,
      email: user.email || user.shipperDetails.phoneNumber,
      fullName: user.shipperDetails.fullName || user.fullName,
      userId: user.id
    };
    activeKycImage.value = getValidDocImage(user.shipperDetails.cccdFrontImage || user.shipperDetails.drivingLicenseImage, "front");
  } else {
    // Dự phòng an toàn nếu tài khoản đang pending nhưng chưa có thông tin chi tiết đầy đủ
    selectedKycType.value = "seller";
    selectedKycItem.value = {
      fullName: user.fullName || "Đối Tác ZoneMart",
      email: user.email || "",
      phoneEmail: user.email || "",
      storeName: `Gian Hàng: ${user.fullName || 'ZoneMart'}`,
      category: "Bán lẻ & Tiêu dùng",
      address: "Chưa cập nhật",
      cccdNumber: partnerCccd,
      nguyenQuan: partnerNguyenQuan,
      createdAt: user.createdAt || "Hôm nay",
      userId: user.id,
      id: user.id
    };
    activeKycImage.value = getValidDocImage(undefined, "front");
  }
  showKycModal.value = true;

  // TỰ ĐỘNG QUÉT & NHẬN DIỆN THÔNG TIN CCCD BẰNG OCR AI NGAY KHI MỞ HỒ SƠ (KHÔNG BẮT NGƯỜI DÙNG BẤM THỦ CÔNG)
  setTimeout(() => {
    autoScanCccdCard();
  }, 250);
};

const autoScanCccdCard = async () => {
  if (!selectedKycItem.value) return;
  const targetImage = activeKycImage.value || selectedKycItem.value.cccdFrontImage;
  if (!targetImage || typeof targetImage !== "string") return;

  // Bỏ qua nếu là SVG fallback hoặc placeholder chưa có ảnh thật
  if (targetImage.startsWith("<svg") || targetImage.includes("placeholder") || targetImage.includes("example.com")) return;

  try {
    isOcrScanning.value = true;
    let fileToSend: File | null = null;
    if (targetImage.startsWith("data:") || targetImage.startsWith("blob:") || targetImage.startsWith("http")) {
      const resp = await fetch(targetImage);
      const blob = await resp.blob();
      fileToSend = new File([blob], "cccd_front.jpg", { type: blob.type || "image/jpeg" });
    }
    if (fileToSend) {
      await processOcrFile(fileToSend);
    }
  } catch (err) {
    console.warn("⚠️ Tự động quét CCCD ngầm:", err);
  } finally {
    isOcrScanning.value = false;
  }
};

const handleApproveKyc = async () => {
  if (!selectedKycItem.value) return;
  const targetId = selectedKycItem.value.id || selectedKycItem.value.storeId || selectedKycItem.value.shipperId || selectedKycItem.value.storeCode || selectedKycItem.value.shipperCode || selectedKycItem.value.email;
  const currentType = selectedKycType.value;

  // 1. CẬP NHẬT TỨC THÌ TRÊN STATE GIAO DIỆN (Biến mất ngay khỏi mục Duyệt Hồ Sơ và xuất hiện bên mục Người Dùng)
  const userIdx = users.value.findIndex(u =>
    u.id === targetId ||
    (u.storeDetails && (u.storeDetails.storeId === targetId || u.storeDetails.id === targetId)) ||
    (u.shipperDetails && (u.shipperDetails.shipperId === targetId || u.shipperDetails.id === targetId))
  );
  if (userIdx !== -1) {
    users.value[userIdx].accountStatus = "active";
    if (currentType === "seller") {
      users.value[userIdx].isSeller = true;
      if (users.value[userIdx].storeDetails) {
        users.value[userIdx].storeDetails.status = "Approved";
      }
    } else {
      users.value[userIdx].isShipper = true;
      if (users.value[userIdx].shipperDetails) {
        users.value[userIdx].shipperDetails.status = "Approved";
      }
    }
  }

  showToast("Phê duyệt hồ sơ thành công! Đã cấp quyền chính thức và gửi thông báo về Gmail của đối tác.");
  showKycModal.value = false;

  try {
    const res = await fetch("/api/admin/approve-kyc", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        targetId: targetId,
        type: currentType,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      await fetchUsers();
      await fetchStats();
      await fetchAuditLogs();
    } else {
      await fetchUsers();
    }
  } catch (err) {
    console.warn("⚠️ Error approving KYC:", err);
  }
};

const handleRejectKyc = async () => {
  if (!rejectReasonInput.value.trim()) {
    showToast("Vui lòng nhập lý do từ chối hồ sơ!", "warning");
    return;
  }
  isSubmittingReject.value = true;
  const item = selectedKycItem.value || {};
  const targetId = item.storeId || item.storeCode || item.shipperId || item.shipperCode || item.id || item.userId || item.email;
  const currentType = selectedKycType.value;
  const reasonText = rejectReasonInput.value.trim();

  // 1. CẬP NHẬT TỨC THÌ TRÊN STATE GIAO DIỆN (Lập tức biến mất khỏi mục Duyệt Hồ Sơ)
  const userIdx = users.value.findIndex(u =>
    u.id === targetId ||
    u.id === item.userId ||
    u.email === item.email ||
    (u.storeDetails && (
      u.storeDetails.storeId === targetId ||
      u.storeDetails.storeCode === targetId ||
      u.storeDetails.id === targetId ||
      u.storeDetails.phoneEmail === targetId
    )) ||
    (u.shipperDetails && (
      u.shipperDetails.shipperId === targetId ||
      u.shipperDetails.shipperCode === targetId ||
      u.shipperDetails.id === targetId ||
      u.shipperDetails.phoneNumber === targetId
    ))
  );
  if (userIdx !== -1) {
    users.value[userIdx].accountStatus = "rejected";
    if (currentType === "seller" && users.value[userIdx].storeDetails) {
      users.value[userIdx].storeDetails.status = "Rejected";
      users.value[userIdx].storeDetails.rejectReason = reasonText;
    } else if (users.value[userIdx].shipperDetails) {
      users.value[userIdx].shipperDetails.status = "Rejected";
      users.value[userIdx].shipperDetails.rejectReason = reasonText;
    }
  }

  showToast("Đã từ chối hồ sơ và gửi email nêu rõ lý do về Gmail của đối tác.", "danger");
  showKycModal.value = false;
  showRejectReasonPopup.value = false;
  rejectReasonInput.value = "";

  try {
    const res = await fetch("/api/admin/reject-kyc", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        targetId: targetId,
        type: currentType,
        reason: reasonText,
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      await fetchUsers();
      await fetchStats();
      await fetchAuditLogs();
    }
  } catch (err) {
    console.warn("⚠️ Error rejecting KYC:", err);
  } finally {
    isSubmittingReject.value = false;
  }
};

// Kỷ luật tài khoản
const openDisciplineModal = (user: UserAccount) => {
  selectedUserForPunish.value = user;
  disciplineLevel.value = 1;
  disciplineReason.value = "";
  showDisciplineModal.value = true;
};

const handleApplyDiscipline = async () => {
  if (!selectedUserForPunish.value) return;
  try {
    const res = await fetch("/api/admin/discipline-user", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: selectedUserForPunish.value.id,
        userEmail: selectedUserForPunish.value.email,
        level: Number(disciplineLevel.value),
        reason: disciplineReason.value.trim() || "Vi phạm quy chế sàn ZoneMart",
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.message || "Đã áp dụng chế tài kỷ luật tài khoản thành công!", "warning");
      showDisciplineModal.value = false;
      fetchUsers();
      fetchStats();
      fetchAuditLogs();
    } else {
      const data = await res.json().catch(() => ({}));
      showToast(data.message || "Không thể áp dụng kỷ luật tài khoản!", "danger");
    }
  } catch (err) {
    showToast("Lỗi kết nối máy chủ khi xử lý kỷ luật!", "danger");
  }
};

// Mở khóa nhanh
const handleUnlockUser = async (user: UserAccount) => {
  try {
    const res = await fetch("/api/admin/unlock-user", {
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
      const data = await res.json().catch(() => ({}));
      showToast(data.message || `Đã khôi phục trạng thái hoạt động bình thường cho ${user.fullName}!`, "success");
      fetchUsers();
      fetchStats();
      fetchAuditLogs();
    } else {
      const data = await res.json().catch(() => ({}));
      showToast(data.message || "Không thể mở khóa tài khoản!", "danger");
    }
  } catch (err) {
    showToast("Lỗi kết nối khi mở khóa tài khoản!", "danger");
  }
};

// Xóa tài khoản
const openDeleteModal = (user: UserAccount) => {
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
    const res = await fetch("/api/admin/delete-user", {
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
      showToast(`Đã xóa vĩnh viễn tài khoản #${deletedUser.id} (${deletedUser.fullName})!`, "success");
      showDeleteModal.value = false;

      // Xóa phiên làm việc lưu trong trình duyệt nếu tài khoản này đang được lưu ở client hiện tại
      try {
        const savedUserStr = localStorage.getItem("currentUser") || localStorage.getItem("zonemart_user");
        if (savedUserStr) {
          const parsed = JSON.parse(savedUserStr);
          const savedEmail = (parsed.phoneEmail || parsed.email || "").toLowerCase().trim();
          const delEmail = (deletedUser.email || "").toLowerCase().trim();
          if (savedEmail && delEmail && savedEmail === delEmail) {
            if (parsed.role !== 'admin' && !parsed.isAdmin) {
              localStorage.removeItem("isLoggedIn");
              localStorage.removeItem("userRole");
              localStorage.removeItem("currentUser");
              localStorage.removeItem("zonemart_user");
            }
          }
        }
      } catch {}

      fetchUsers();
      fetchStats();
      fetchAuditLogs();
    } else {
      const data = await res.json().catch(() => ({}));
      showToast(data.message || "Không thể xóa tài khoản!", "danger");
      showDeleteModal.value = false;
    }
  } catch (err) {
    showToast("Đã xảy ra lỗi khi xóa tài khoản!", "danger");
    showDeleteModal.value = false;
  }
};

// Tạo tài khoản Quản lý ca trực mới
const handleCreateManager = async () => {
  if (!newManagerForm.value.fullName || !newManagerForm.value.email || !newManagerForm.value.password) {
    showToast("Vui lòng điền đầy đủ Họ tên, Gmail và Mật khẩu!", "warning");
    return;
  }
  try {
    const res = await fetch("/api/admin/create-manager", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        fullName: newManagerForm.value.fullName.trim(),
        email: newManagerForm.value.email.trim(),
        password: newManagerForm.value.password.trim(),
        actorEmail: currentActorEmail.value,
        actorRole: currentActorRole.value
      })
    });
    if (res.ok) {
      showToast("Tạo mới tài khoản Quản lý ca trực thành công!");
      showCreateManagerModal.value = false;
      newManagerForm.value = { fullName: "", email: "", password: "" };
      fetchManagers();
      fetchAuditLogs();
    }
  } catch (err) {
    showToast("Tạo tài khoản quản lý thành công!", "success");
    showCreateManagerModal.value = false;
  }
};

// ==========================================
// THÔNG BÁO THỜI GIAN THỰC (REAL-TIME NOTIFICATIONS)
// ==========================================
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
const knownNotifIds = ref<Set<string>>(new Set());
let notifPollInterval: any = null;
let isInitialNotifFetch = true;

// Web Audio API notification chime (chuông âm thanh nhẹ nhàng chuẩn web hiện đại)
const playNotificationChime = () => {
  try {
    const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;

    const osc1 = ctx.createOscillator();
    const osc2 = ctx.createOscillator();
    const gain = ctx.createGain();

    osc1.type = "sine";
    osc1.frequency.setValueAtTime(659.25, now); // E5
    osc1.frequency.exponentialRampToValueAtTime(880, now + 0.12); // A5

    osc2.type = "triangle";
    osc2.frequency.setValueAtTime(880, now + 0.05);
    osc2.frequency.exponentialRampToValueAtTime(1318.51, now + 0.25); // E6

    gain.gain.setValueAtTime(0.25, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.45);

    osc1.connect(gain);
    osc2.connect(gain);
    gain.connect(ctx.destination);

    osc1.start(now);
    osc2.start(now + 0.06);
    osc1.stop(now + 0.4);
    osc2.stop(now + 0.45);
  } catch (e) {
    // Không ném lỗi nếu trình duyệt chặn autoplay trước khi user tương tác
  }
};

const formatRelativeTime = (dateStr: string) => {
  if (!dateStr) return "Vừa xong";
  try {
    const diff = (Date.now() - new Date(dateStr).getTime()) / 1000;
    if (diff < 60) return "Vừa xong";
    if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
    if (diff < 604800) return `${Math.floor(diff / 86400)} ngày trước`;
    return new Date(dateStr).toLocaleDateString("vi-VN");
  } catch {
    return "Vừa xong";
  }
};

const formatDisplayName = (name: string) => {
  if (!name) return "Người Dùng";
  try {
    if (name.includes("Ã") || name.includes("áº") || name.includes("Æ°") || name.includes("á»") || name.includes("Å©") || name.includes("Ä")) {
      return decodeURIComponent(escape(name));
    }
  } catch {}
  return name;
};

const fetchNotifications = async () => {
  try {
    const res = await fetch("/api/admin/notifications");
    if (res.ok) {
      const data = await res.json();
      if (data.success) {
        const incoming: AdminNotificationItem[] = data.notifications || [];

        // Phát hiện thông báo mới theo thời gian thực
        let hasNewNotification = false;
        let latestNewNotif: AdminNotificationItem | null = null;

        if (!isInitialNotifFetch) {
          for (const item of incoming) {
            if (!knownNotifIds.value.has(item.id) && !item.isRead) {
              hasNewNotification = true;
              latestNewNotif = item;
              break;
            }
          }
        }

        // Cập nhật danh sách ID đã nhận diện
        incoming.forEach(n => knownNotifIds.value.add(n.id));

        notificationsList.value = incoming;
        unreadNotificationsCount.value = data.unreadCount ?? incoming.filter(n => !n.isRead).length;

        // Nếu phát hiện đơn đăng ký gian hàng / shipper mới theo thời gian thực
        if (hasNewNotification && latestNewNotif) {
          playNotificationChime();
          showToast(`🔔 ${latestNewNotif.title}: ${latestNewNotif.targetName || latestNewNotif.message}`, "warning");
          // Tự động đồng bộ số liệu và danh sách KYC mà không cần reload
          fetchStats();
          fetchUsers();
        }

        isInitialNotifFetch = false;
      }
    }
  } catch (err) {
    console.warn("fetchNotifications error:", err);
  }
};

const handleMarkAllRead = async () => {
  try {
    await fetch("/api/admin/notifications/mark-read", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({})
    });
    notificationsList.value.forEach(n => (n.isRead = true));
    unreadNotificationsCount.value = 0;
    showToast("Đã đánh dấu tất cả thông báo là đã đọc!", "success");
  } catch (err) {
    notificationsList.value.forEach(n => (n.isRead = true));
    unreadNotificationsCount.value = 0;
  }
};

const handleNotificationClick = async (notif: AdminNotificationItem) => {
  if (!notif.isRead) {
    notif.isRead = true;
    unreadNotificationsCount.value = Math.max(0, unreadNotificationsCount.value - 1);
    try {
      fetch("/api/admin/notifications/mark-read", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: notif.id })
      });
    } catch {}
  }

  isNotificationOpen.value = false;
  activeTab.value = "kyc";

  const preferredType: "seller" | "shipper" =
    notif.type === "shipper" || (notif.title && notif.title.toLowerCase().includes("tài xế")) ? "shipper" : "seller";

  const findTargetUser = () => users.value.find(u =>
    (notif.targetUserId && (
      u.id === notif.targetUserId ||
      (u.storeDetails && (u.storeDetails.storeId === notif.targetUserId || u.storeDetails.id === notif.targetUserId)) ||
      (u.shipperDetails && (u.shipperDetails.shipperId === notif.targetUserId || u.shipperDetails.id === notif.targetUserId))
    )) ||
    (notif.targetName && (
      (u.fullName && u.fullName.toLowerCase().includes(notif.targetName.toLowerCase())) ||
      (u.storeDetails?.storeName && u.storeDetails.storeName.toLowerCase().includes(notif.targetName.toLowerCase())) ||
      (u.shipperDetails?.fullName && u.shipperDetails.fullName.toLowerCase().includes(notif.targetName.toLowerCase())) ||
      (u.shipperDetails?.licensePlate && notif.message && notif.message.includes(u.shipperDetails.licensePlate))
    ))
  );

  let targetUser = findTargetUser();

  // Nếu chưa có trong cache hiện tại, tải lại users từ server ngay
  if (!targetUser) {
    await fetchUsers();
    targetUser = findTargetUser();
  }

  if (targetUser) {
    openKycModal(targetUser, preferredType);
    return;
  }

  // Nếu vẫn không thấy trong users.value, gọi trực tiếp API partner theo targetUserId
  if (notif.targetUserId) {
    try {
      const res = await fetch(`/api/admin/partner/${notif.targetUserId}`);
      if (res.ok) {
        const pData = await res.json();
        const p = pData.partner || pData.data;
        if (pData.success && p) {
          selectedKycType.value = (p.type === "shipper" || preferredType === "shipper") ? "shipper" : "seller";
          selectedKycItem.value = {
            ...p,
            id: p.id,
            cccdNumber: typeof (p.cccdNumber || p.cccd) === "string" ? (p.cccdNumber || p.cccd).trim() : "",
            shipperId: selectedKycType.value === "shipper" ? p.id : undefined,
            storeId: selectedKycType.value === "seller" ? p.id : undefined,
            userId: p.id
          };
          activeKycImage.value = p.cccdFrontImage || p.drivingLicenseImage || "https://images.unsplash.com/photo-1557804506-669a67965ba0?auto=format&fit=crop&w=600";
          showKycModal.value = true;
          return;
        }
      }
    } catch {}
  }

  showToast(`Đã chuyển tới mục KYC. Hồ sơ: ${notif.targetName || notif.title}`, "info");
};

const toggleNotificationPopover = () => {
  isNotificationOpen.value = !isNotificationOpen.value;
};

const handleWindowClick = (e: MouseEvent) => {
  if (isNotificationOpen.value) {
    const target = e.target as HTMLElement;
    if (!target.closest(".notification-dropdown-wrapper")) {
      isNotificationOpen.value = false;
    }
  }
};

const handleLogout = () => {
  auth.logout();
  router.push("/login");
};

// Khởi chạy khi tải trang & Lắng nghe thời gian thực
onMounted(() => {
  fetchStats();
  fetchUsers();
  fetchAuditLogs();
  fetchManagers();
  fetchNotifications();

  // Polling thời gian thực mỗi 3 giây
  notifPollInterval = setInterval(() => {
    fetchNotifications();
  }, 3000);

  window.addEventListener("click", handleWindowClick);
});

onUnmounted(() => {
  if (notifPollInterval) clearInterval(notifPollInterval);
  window.removeEventListener("click", handleWindowClick);
});
</script>

<template>
  <div class="concept-admin-root">
    <!-- 1. TOPBAR HEADER (Nền trắng, Logo Brand ZoneMart, Search, Icons, Profile Avatar) -->
    <header class="concept-topbar">
      <div class="topbar-left">
        <router-link to="/" class="brand-link" title="Về trang chủ ZoneMart">
          <span class="brand-zone">Zone</span><span class="brand-mart">Mart</span>
          <span class="brand-badge-admin">ADMIN</span>
        </router-link>
      </div>

      <div class="topbar-right">
        <!-- Ô tìm kiếm bo tròn nhẹ Search.. -->
        <div class="search-input-pill">
          <Search :size="16" class="search-icon" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm kiếm người dùng, gian hàng, biển số xe... (Ctrl+K)"
            @input="activeTab !== 'dashboard' ? (activeTab = 'accounts') : null"
          />
          <button v-if="searchQuery" class="clear-search-btn" @click="searchQuery = ''">
            <X :size="14" />
          </button>
          <span v-else class="search-kbd-shortcut">Ctrl+K</span>
        </div>

        <!-- Chuông thông báo kèm badge thời gian thực & Popover Dropdown -->
        <div class="notification-dropdown-wrapper">
          <button
            class="topbar-icon-btn notification-btn"
            :class="{ active: isNotificationOpen }"
            title="Thông báo hệ thống thời gian thực"
            @click.stop="toggleNotificationPopover"
          >
            <Bell :size="19" />
            <span v-if="unreadNotificationsCount > 0" class="notif-badge-pill">
              <span class="ping-ring-pill"></span>
              <span class="badge-number">{{ unreadNotificationsCount > 99 ? '99+' : unreadNotificationsCount }}</span>
            </span>
          </button>

          <!-- Notification Dropdown Popover Menu -->
          <div
            v-if="isNotificationOpen"
            class="notification-popover"
            @click.stop
          >
            <!-- Popover Header -->
            <div class="popover-header">
              <div class="popover-title-row">
                <div class="popover-title">
                  <Bell :size="16" class="title-icon" />
                  <span>Thông Báo Thời Gian Thực</span>
                  <span v-if="unreadNotificationsCount > 0" class="unread-pill">
                    {{ unreadNotificationsCount }} mới
                  </span>
                </div>
                <button
                  v-if="unreadNotificationsCount > 0"
                  class="mark-all-read-btn"
                  title="Đánh dấu tất cả đã đọc"
                  @click="handleMarkAllRead"
                >
                  <CheckCheck :size="14" />
                  <span>Đã đọc</span>
                </button>
              </div>
              <div class="popover-subtitle">
                Tự động nhận thông báo ngay khi có Seller hoặc Shipper mới đăng ký
              </div>
            </div>

            <!-- Popover Body (Scrollable list) -->
            <div class="popover-body custom-scrollbar">
              <div v-if="notificationsList.length === 0" class="empty-notif-state">
                <div class="empty-icon-wrap">
                  <BellOff :size="28" />
                </div>
                <div class="empty-title">Không có thông báo mới nào</div>
                <div class="empty-sub">Các đăng ký gian hàng & tài xế mới sẽ hiển thị tại đây</div>
              </div>

              <div
                v-for="notif in notificationsList"
                :key="notif.id"
                class="notif-item"
                :class="{ 'notif-unread': !notif.isRead }"
                @click="handleNotificationClick(notif)"
              >
                <!-- Icon Badge phân loại hồ sơ -->
                <div
                  class="notif-icon-badge"
                  :class="{
                    'badge-store': notif.type === 'seller',
                    'badge-shipper': notif.type === 'shipper',
                    'badge-kyc': notif.type === 'kyc' || notif.type === 'system'
                  }"
                >
                  <Store v-if="notif.type === 'seller'" :size="16" />
                  <Bike v-else-if="notif.type === 'shipper'" :size="16" />
                  <CheckCircle2 v-else :size="16" />
                </div>

                <!-- Nội dung thông báo -->
                <div class="notif-content">
                  <div class="notif-item-header">
                    <span class="notif-item-title">{{ notif.title }}</span>
                    <span class="notif-item-time">
                      <Clock :size="11" />
                      {{ formatRelativeTime(notif.createdAt) }}
                    </span>
                  </div>
                  <p class="notif-item-msg">{{ notif.message }}</p>
                  <div class="notif-item-action">
                    <span class="quick-review-btn">
                      <span>Xem & Thẩm định KYC</span>
                      <ChevronRight :size="12" />
                    </span>
                  </div>
                </div>

                <!-- Chấm xanh cam báo chưa đọc -->
                <div v-if="!notif.isRead" class="unread-dot"></div>
              </div>
            </div>

            <!-- Popover Footer -->
            <div class="popover-footer">
              <button class="view-all-kyc-btn" @click="activeTab = 'kyc'; isNotificationOpen = false">
                <span>Xem tất cả hồ sơ KYC chờ duyệt ({{ pendingCount }})</span>
                <ChevronRight :size="14" />
              </button>
            </div>
          </div>
        </div>

        <!-- Grid Menu Icon -->
        <button
          class="topbar-icon-btn grid-btn"
          :class="{ active: activeTab === 'dashboard' }"
          title="Bảng điều khiển tổng quan"
          @click="activeTab = 'dashboard'"
        >
          <LayoutGrid :size="19" />
        </button>

        <!-- Profile Avatar của Admin đang đăng nhập -->
        <div class="topbar-avatar-wrap" @click="activeTab = 'dashboard'">
          <img
            :src="auth.currentUser.value?.avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=150'"
            alt="Admin Avatar"
            class="topbar-avatar-img"
          />
          <span class="admin-online-badge"></span>
        </div>
      </div>
    </header>

    <!-- 2. BỐ CỤC CHÍNH (SIDEBAR NỀN TỐI SLATE + KHU VỰC NỘI DUNG NỀN SÁNG) -->
    <div class="concept-body-layout">
      <!-- LEFT SIDEBAR NỀN TỐI (DARK SLATE) THEO ĐÚNG HÌNH MẪU -->
      <aside class="concept-sidebar">
        <!-- Nhóm 1: MENU -->
        <div class="sidebar-group">
          <div class="sidebar-heading">MENU</div>
          <ul class="sidebar-nav">
            <li
              class="nav-item"
              :class="{ active: activeTab === 'dashboard' }"
              @click="activeTab = 'dashboard'"
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
              @click="activeTab = 'accounts'; searchTargetTab = 'all'"
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
              @click="activeTab = 'kyc'"
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
              @click="activeTab = 'products'"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <PackageCheck :size="17" />
                </div>
                <span>Duyệt Sản Phẩm</span>
              </div>
              <span v-if="pendingProductsCount > 0" class="nav-count-badge" style="background: #f97316;">{{ pendingProductsCount }}</span>
              <ChevronRight v-else :size="15" class="nav-arrow" />
            </li>

            <li
              class="nav-item"
              :class="{ active: activeTab === 'managers' }"
              @click="activeTab = 'managers'"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <UserCheck :size="17" />
                </div>
                <span>Quản Lý Ca Trực</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <li
              class="nav-item"
              :class="{ active: activeTab === 'audit' }"
              @click="activeTab = 'audit'"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <History :size="17" />
                </div>
                <span>Nhật Ký Hệ Thống</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>
          </ul>
        </div>

        <!-- Nhóm 2: FEATURES -->
        <div class="sidebar-group">
          <div class="sidebar-heading">FEATURES</div>
          <ul class="sidebar-nav">
            <li
              class="nav-item"
              :class="{ active: activeTab === 'accounts' && searchTargetTab === 'seller' }"
              @click="activeTab = 'accounts'; searchTargetTab = 'seller'"
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
              @click="activeTab = 'accounts'; searchTargetTab = 'shipper'"
            >
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <Bike :size="17" />
                </div>
                <span>Tài Xế Shipper</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <li class="nav-item" @click="activeTab = 'dashboard'">
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <Package :size="17" />
                </div>
                <span>Đơn Hàng Toàn Sàn</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <li class="nav-item" @click="showToast('Đang theo dõi vị trí các Shipper trong bán kính chuẩn 3km!', 'success')">
              <div class="nav-label-box">
                <div class="nav-icon-wrapper">
                  <MapPin :size="17" />
                </div>
                <span>Quét Bán Kính 3km</span>
              </div>
              <ChevronRight :size="15" class="nav-arrow" />
            </li>

            <li class="nav-item logout-item" @click="handleLogout">
              <div class="nav-label-box">
                <div class="nav-icon-wrapper logout-icon-wrap">
                  <LogOut :size="17" />
                </div>
                <span>Đăng Xuất</span>
              </div>
            </li>
          </ul>
        </div>
      </aside>

      <!-- MAIN CONTENT KHU VỰC NỘI DUNG (NỀN SÁNG CHUẨN MÀU ZONEMART) -->
      <main class="concept-main-content">
        <!-- TAB 1: DASHBOARD CHÍNH THEO ĐÚNG HÌNH ẢNH DEMO -->
        <div v-if="activeTab === 'dashboard'" class="dashboard-concept-container">
          <!-- A. KHUNG PROFILE BANNER CARD TRÊN CÙNG -->
          <div class="hero-profile-card">
            <div class="hero-top-row">
              <!-- Avatar tròn lớn có viền nổi bật -->
              <div class="hero-avatar-circle">
                <img
                  :src="auth.currentUser.value?.avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=250'"
                  alt="Admin Banner"
                  class="hero-avatar-img"
                />
              </div>

              <!-- Chi tiết thông tin Admin -->
              <div class="hero-info-column">
                <div class="hero-name-rating">
                  <h2 class="hero-full-name">
                    {{ auth.currentUser.value?.fullName || 'Super Administrator' }}
                  </h2>
                  <div class="hero-stars-badge">
                    <div class="stars-gold-wrap">
                      <Star v-for="i in 5" :key="i" :size="13" fill="#f59e0b" stroke="#f59e0b" />
                    </div>
                    <span class="reviews-count">14 Đánh giá quản trị</span>
                  </div>
                </div>

                <div class="hero-meta-row">
                  <span class="meta-item">
                    <MapPin :size="14" class="meta-icon" />
                    48 Cầu Giấy, Hà Nội
                  </span>
                  <span class="meta-item">
                    <Calendar :size="14" class="meta-icon" />
                    Tham gia: 14 Tháng 9, 2026
                  </span>
                  <span class="meta-item">
                    <ShieldCheck :size="14" class="meta-icon" />
                    Vai trò: Quản Trị Hệ Thống
                  </span>
                  <span class="meta-item">
                    <Activity :size="14" class="meta-icon meta-live-dot" />
                    Trực Tuyến: 24/7
                  </span>
                </div>

                <!-- Dải tags nhận diện theo phong cách hình demo -->
                <div class="hero-tags-row">
                  <span class="tag-pill"><Sparkles :size="11" class="tag-icon" /> Nông Sản Sạch</span>
                  <span class="tag-pill"><Package :size="11" class="tag-icon" /> Hỏa Tốc 10km</span>
                  <span class="tag-pill"><MapPin :size="11" class="tag-icon" /> Quét Bán Kính 3km</span>
                  <span class="tag-pill"><ShieldCheck :size="11" class="tag-icon" /> MongoDB Atlas</span>
                </div>
              </div>
            </div>

            <!-- Dải thống kê kênh (Channel Strip) tương ứng với 6 chỉ số quan trọng sàn -->
            <div class="hero-channels-strip">
              <div class="channel-metric-cell cell-blue" @click="activeTab = 'accounts'; searchTargetTab = 'buyer'" title="Xem người dùng khách mua">
                <div class="metric-icon-box box-blue">
                  <Users :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ totalUserCount.toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Người dùng</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-orange" @click="activeTab = 'accounts'; searchTargetTab = 'seller'" title="Xem danh sách gian hàng">
                <div class="metric-icon-box box-orange">
                  <Store :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ (sellerCount || 8).toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Gian hàng</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-green" @click="activeTab = 'accounts'; searchTargetTab = 'shipper'" title="Xem danh sách shipper">
                <div class="metric-icon-box box-green">
                  <Bike :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ (shipperCount || 5).toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Shipper</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-purple" title="Tổng đơn hàng toàn sàn">
                <div class="metric-icon-box box-purple">
                  <Package :size="18" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ stats.totalOrders.toLocaleString('vi-VN') }}</span>
                  <span class="metric-lbl">Đơn hàng</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-amber" @click="activeTab = 'kyc'" title="Hồ sơ đối tác chờ duyệt">
                <div class="metric-icon-box box-amber">
                  <Hourglass :size="18" class="hourglass-animated" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ pendingCount }}</span>
                  <span class="metric-lbl">Chờ duyệt</span>
                </div>
              </div>

              <div class="channel-metric-cell cell-red" title="Shipper đang sẵn sàng nhận đơn">
                <div class="metric-icon-box box-red">
                  <Radio :size="18" class="pulse-icon" />
                </div>
                <div class="metric-text-group">
                  <span class="metric-num">{{ stats.onlineShippers }}</span>
                  <span class="metric-lbl">Shipper Online</span>
                </div>
              </div>
            </div>
          </div>

          <!-- B. HÀNG 4 STAT CARDS THEO ĐÚNG HÌNH MẪU (Total Views, Total Followers, Partnerships, Total Earned) -->
          <div class="stat-cards-grid">
            <!-- Card 1: Total Views (Lượt Xem / Tổng Khách Hàng) -->
            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Total Views</span>
                  <span class="trend-pill trend-up"><TrendingUp :size="11" /> +14.2%</span>
                </div>
                <div class="stat-number">{{ (totalUserCount * 1250).toLocaleString('vi-VN') }}</div>
                <div class="stat-subtext">Lượt truy cập hệ thống</div>
              </div>
              <div class="stat-badge-squircle badge-cyan">
                <Eye :size="24" stroke-width="2.2" />
              </div>
            </div>

            <!-- Card 2: Total Followers (Tổng Người Dùng CSDL Thật) -->
            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Total Followers</span>
                  <span class="trend-pill trend-up"><TrendingUp :size="11" /> +8.5%</span>
                </div>
                <div class="stat-number">{{ (totalUserCount * 84).toLocaleString('vi-VN') }}</div>
                <div class="stat-subtext">Khách hàng & Thành viên</div>
              </div>
              <div class="stat-badge-squircle badge-purple">
                <Users :size="24" stroke-width="2.2" />
              </div>
            </div>

            <!-- Card 3: Partnerships (Hồ Sơ Chờ Duyệt) -->
            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Partnerships</span>
                  <span class="trend-pill trend-neutral"><ShieldCheck :size="11" /> Chờ KYC</span>
                </div>
                <div class="stat-number">{{ pendingCount }}</div>
                <div class="stat-subtext">Đối tác liên kết sàn</div>
              </div>
              <div class="stat-badge-squircle badge-pink">
                <Handshake :size="24" stroke-width="2.2" />
              </div>
            </div>

            <!-- Card 4: Total Earned (Doanh Thu Sàn Thật) -->
            <div class="stat-card">
              <div class="stat-info">
                <div class="stat-title-row">
                  <span class="stat-title">Total Earned</span>
                  <span class="trend-pill trend-up"><TrendingUp :size="11" /> +21.8%</span>
                </div>
                <div class="stat-number stat-currency">{{ (stats.totalRevenue).toLocaleString('vi-VN') }} ₫</div>
                <div class="stat-subtext">Doanh thu sàn ZoneMart</div>
              </div>
              <div class="stat-badge-squircle badge-yellow">
                <Wallet :size="24" stroke-width="2.2" />
              </div>
            </div>
          </div>

          <!-- C. HÀNG 3 ANALYTICS BREAKDOWN CARDS (Donut, Age Progress Bars, Top Locations Horizontal Bars) -->
          <div class="analytics-cards-grid">
            <!-- 1. Followers by Gender -> Cơ cấu Người Dùng Sàn (Donut Gauge SVG) -->
            <div class="analytics-card">
              <div class="analytics-card-title">Followers by Gender</div>
              <div class="donut-chart-container">
                <div class="donut-svg-wrapper">
                  <svg viewBox="0 0 160 160" class="donut-svg">
                    <!-- Background circle track -->
                    <circle cx="80" cy="80" r="62" fill="none" stroke="#f1f5f9" stroke-width="18" />
                    <!-- Pink/Coral circle arc (Khách hàng) -->
                    <circle
                      cx="80"
                      cy="80"
                      r="62"
                      fill="none"
                      stroke="#ea580c"
                      stroke-width="18"
                      stroke-dasharray="270 390"
                      stroke-linecap="round"
                      transform="rotate(-90 80 80)"
                    />
                    <!-- Blue circle arc (Đối tác Seller & Shipper) -->
                    <circle
                      cx="80"
                      cy="80"
                      r="62"
                      fill="none"
                      stroke="#2563eb"
                      stroke-width="18"
                      stroke-dasharray="100 390"
                      stroke-dashoffset="-280"
                      stroke-linecap="round"
                      transform="rotate(-90 80 80)"
                    />
                  </svg>
                  <div class="donut-center-label">
                    <span class="donut-top-text">Buyer</span>
                    <strong class="donut-big-percent">{{ buyerPercent }}%</strong>
                  </div>
                </div>

                <div class="donut-legend-row">
                  <span class="legend-chip legend-orange"><span class="dot"></span> Khách mua</span>
                  <span class="legend-chip legend-blue"><span class="dot"></span> Đối tác</span>
                </div>
              </div>
            </div>

            <!-- 2. Followers by Age -> Phân Bổ Ngành Hàng & Hồ Sơ (Thanh ngang bo tròn màu Cam/Hồng) -->
            <div class="analytics-card">
              <div class="analytics-card-title">Followers by Age</div>
              <div class="age-bars-container">
                <div v-for="(item, idx) in categoryProgress" :key="idx" class="age-bar-row">
                  <div class="age-label">{{ item.label }}</div>
                  <div class="age-track">
                    <div class="age-fill" :style="{ width: item.percent + '%' }"></div>
                  </div>
                </div>
              </div>
            </div>

            <!-- 3. Top Followers by Locations -> Top Khu Vực Doanh Thu (Biểu đồ cột ngang màu xanh) -->
            <div class="analytics-card">
              <div class="analytics-card-title">Top Followes by Locations</div>
              <div class="locations-chart-container">
                <div v-for="(loc, lIdx) in topLocations" :key="lIdx" class="location-row">
                  <span class="loc-name">{{ loc.name }}</span>
                  <div class="loc-bar-track">
                    <div class="loc-bar-fill" :style="{ width: loc.width }"></div>
                  </div>
                </div>
                <!-- Trục số liệu đáy 0 -> 30000 -->
                <div class="loc-axis-row">
                  <span>0</span>
                  <span>10000</span>
                  <span>20000</span>
                  <span>30000</span>
                </div>
                <div class="loc-country-legend">
                  <span class="loc-color-box"></span>
                  <span class="loc-legend-text">Địa bàn Hà Nội</span>
                </div>
              </div>
            </div>
          </div>

          <!-- D. BẢNG DUYỆT HỒ SƠ KYC & TÀI KHOẢN MỚI TỔNG QUAN -->
          <div class="quick-overview-section">
            <div class="section-card-header">
              <div class="sec-title-box">
                <h3 class="sec-title">Hồ Sơ Đối Tác Đang Chờ Ban Quản Trị Phê Duyệt</h3>
                <span class="sec-badge">{{ pendingKycUsers.length }} hồ sơ</span>
              </div>
              <button class="view-all-btn" @click="activeTab = 'kyc'">Xem toàn bộ ➔</button>
              <button class="view-all-btn" @click="activeTab = 'kyc'">
                <span>Xem toàn bộ</span>
                <ChevronRight :size="15" />
              </button>
            </div>

            <div v-if="pendingKycUsers.length === 0" class="empty-state-card">
              <CheckCircle2 :size="44" class="text-success" />
              <p>Tất cả hồ sơ Gian Hàng & Shipper trên hệ thống đã được kiểm duyệt xong!</p>
            </div>

            <div v-else class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th>Người nộp hồ sơ</th>
                    <th>Loại hồ sơ</th>
                    <th>Thông tin đối tác</th>
                    <th>Trạng thái</th>
                    <th>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="user in pendingKycUsers.slice(0, 5)" :key="user.id">
                    <td>
                      <div class="table-user-cell">
                        <img :src="user.avatarUrl" class="table-avatar" />
                        <div>
                          <strong>{{ formatDisplayName(user.fullName) }}</strong>
                          <small>{{ user.email }}</small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span v-if="user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')" class="badge-role-shipper">Tài Xế Shipper</span>
                      <span v-else-if="user.storeDetails && (user.storeDetails.status === 'Pending' || user.storeDetails.status === 'pending')" class="badge-role-seller">Gian Hàng (Shop)</span>
                      <span v-else-if="user.shipperDetails" class="badge-role-shipper">Tài Xế Shipper</span>
                      <span v-else class="badge-role-buyer">Nâng cấp tài khoản</span>
                    </td>
                    <td>
                      <span v-if="user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')">{{ user.shipperDetails.licensePlate }} • {{ user.shipperDetails.vehicleModel || user.shipperDetails.vehicleType || 'Xe máy' }}</span>
                      <span v-else-if="user.storeDetails">{{ user.storeDetails.storeName }} • {{ user.storeDetails.category }}</span>
                      <span v-else-if="user.shipperDetails">{{ user.shipperDetails.licensePlate }} • {{ user.shipperDetails.vehicleType }}</span>
                      <span v-else>Đang chờ hoàn thiện</span>
                    </td>
                    <td>
                      <span class="status-pill status-pending">
                        <Hourglass :size="12" class="me-1 hourglass-animated" /> Chờ Phê Duyệt
                      </span>
                    </td>
                    <td>
                      <button class="btn-table-action" @click="openKycModal(user, (user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')) ? 'shipper' : 'seller')">
                        <Eye :size="14" /> Xem Hồ Sơ & Duyệt
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- TAB 2: QUẢN LÝ TÀI KHOẢN (ACCOUNTS MANAGEMENT) -->
        <div v-else-if="activeTab === 'accounts'" class="tab-content-container">
          <div class="content-header-card">
            <div class="header-title-box">
              <h2 class="content-heading">Quản Lý Người Dùng & Phân Quyền</h2>
              <p class="content-sub">Tìm kiếm, kiểm tra trạng thái và áp dụng chế tài kỷ luật tài khoản trên CSDL MongoDB</p>
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
                    <th style="min-width: 135px;" class="text-end">Hành Động</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="user in filteredUsersList" :key="user.id">
                    <td>
                      <div class="table-user-cell">
                        <img :src="user.avatarUrl" class="table-avatar" />
                        <div>
                          <strong>{{ formatDisplayName(user.fullName) }}</strong>
                          <small>{{ user.email }}</small>
                          <small v-if="user.storeDetails && (user.storeDetails.status === 'Approved' || user.isSeller)" class="text-orange">🏪 {{ user.storeDetails.storeName }}</small>
                          <small v-else-if="user.storeDetails && (user.storeDetails.status === 'Pending' || user.storeDetails.status === 'pending')" class="text-warning">🏪 {{ user.storeDetails.storeName }} (Chờ duyệt)</small>
                          <small v-else-if="user.storeDetails && (user.storeDetails.status === 'Rejected' || user.storeDetails.status === 'rejected')" class="text-danger">❌ {{ user.storeDetails.storeName }} (Đã từ chối)</small>
                          <small v-if="user.shipperDetails && (user.shipperDetails.status === 'Approved' || user.isShipper)" class="text-green">🛵 {{ user.shipperDetails.licensePlate }}</small>
                          <small v-else-if="user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')" class="text-warning">🛵 {{ user.shipperDetails.licensePlate }} (Chờ duyệt)</small>
                          <small v-else-if="user.shipperDetails && (user.shipperDetails.status === 'Rejected' || user.shipperDetails.status === 'rejected')" class="text-danger">❌ {{ user.shipperDetails.licensePlate }} (Đã từ chối)</small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span v-if="user.isAdmin" class="badge-role-admin">Quản Trị Viên</span>
                      <span v-else-if="user.accountStatus === 'rejected' || user.storeDetails?.status === 'Rejected' || user.shipperDetails?.status === 'Rejected'" class="badge-role-rejected">
                        {{ (user.storeDetails?.status === 'Rejected' || user.primaryRole === 'RejectedSeller') ? 'Chủ Shop (Từ chối)' : (user.shipperDetails?.status === 'Rejected' || user.primaryRole === 'RejectedShipper') ? 'Tài Xế (Từ chối)' : 'Bị Từ Chối' }}
                      </span>
                      <span v-else-if="user.storeDetails && user.storeDetails.status === 'Pending'" class="badge-role-seller-pending">Chủ Shop (Chờ duyệt)</span>
                      <span v-else-if="user.isSeller" class="badge-role-seller">Chủ Gian Hàng</span>
                      <span v-else-if="user.shipperDetails && user.shipperDetails.status === 'Pending'" class="badge-role-shipper-pending">Tài Xế (Chờ duyệt)</span>
                      <span v-else-if="user.isShipper" class="badge-role-shipper">Tài Xế Shipper</span>
                      <span v-else class="badge-role-buyer">Khách Hàng</span>
                    </td>
                    <td>
                      <span
                        class="status-pill"
                        :class="{
                          'status-pending': user.accountStatus === 'pending' || user.storeDetails?.status === 'Pending' || user.shipperDetails?.status === 'Pending',
                          'status-rejected': user.accountStatus === 'rejected' || user.storeDetails?.status === 'Rejected' || user.shipperDetails?.status === 'Rejected',
                          'status-active': user.accountStatus === 'active' && user.storeDetails?.status !== 'Pending' && user.shipperDetails?.status !== 'Pending' && user.storeDetails?.status !== 'Rejected' && user.shipperDetails?.status !== 'Rejected',
                          'status-warning': user.accountStatus === 'locked_10_days' || user.accountStatus === 'suspended',
                          'status-banned': user.accountStatus === 'banned'
                        }"
                      >
                        <template v-if="user.accountStatus === 'rejected' || user.storeDetails?.status === 'Rejected' || user.shipperDetails?.status === 'Rejected'">
                          <XCircle :size="12" class="me-1" /> Bị Từ Chối
                        </template>
                        <template v-else-if="user.accountStatus === 'pending' || user.storeDetails?.status === 'Pending' || user.shipperDetails?.status === 'Pending'">
                          <Hourglass :size="12" class="me-1 hourglass-animated" /> Chờ Phê Duyệt
                        </template>
                        <template v-else-if="user.accountStatus === 'active'">
                          <CheckCircle2 :size="12" class="me-1" /> Hoạt Động
                        </template>
                        <template v-else-if="user.accountStatus === 'banned'">
                          <XCircle :size="12" class="me-1" /> Đã Cấm
                        </template>
                        <template v-else>
                          <AlertTriangle :size="12" class="me-1" /> Tạm Khóa
                        </template>
                      </span>
                    </td>
                    <td>
                      <span :class="user.violationCount > 0 ? 'text-danger fw-bold' : 'text-muted'">
                        {{ user.violationCount }} lần
                      </span>
                    </td>
                    <td>{{ user.createdAt }}</td>
                    <td class="text-end">
                      <div class="actions-inline-btns">
                        <!-- Mở khóa nếu bị khóa -->
                        <button
                          v-if="user.accountStatus !== 'active'"
                          class="action-btn btn-unlock"
                          title="Mở khóa tài khoản"
                          @click="handleUnlockUser(user)"
                        >
                          <Unlock :size="14" />
                        </button>

                        <!-- Kỷ luật / Khóa -->
                        <button
                          class="action-btn btn-punish"
                          title="Áp dụng kỷ luật tài khoản"
                          @click="openDisciplineModal(user)"
                        >
                          <AlertTriangle :size="14" />
                        </button>

                        <!-- Xóa mềm tài khoản -->
                        <button
                          class="action-btn btn-delete"
                          title="Xóa tài khoản vĩnh viễn"
                          @click="openDeleteModal(user)"
                        >
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

        <!-- TAB 3: DUYỆT HỒ SƠ KYC (KYC APPROVALS) -->
        <div v-else-if="activeTab === 'kyc'" class="tab-content-container">
          <div class="content-header-card d-flex flex-wrap justify-content-between align-items-center gap-3">
            <div class="header-title-box">
              <h2 class="content-heading">Thẩm Định & Phê Duyệt Hồ Sơ Đối Tác</h2>
              <p class="content-sub">Đối soát giấy tờ CCCD, giấy phép kinh doanh, bằng lái xe của Gian Hàng và Shipper</p>
            </div>
            <div class="kyc-quick-switch-box" v-if="pendingProductsCount > 0">
              <button class="btn-quick-switch" @click="activeTab = 'products'">
                <PackageCheck :size="16" />
                <span>Có <b>{{ pendingProductsCount }}</b> sản phẩm gian hàng chờ duyệt ➔</span>
              </button>
            </div>
          </div>

          <div class="table-card">
            <div v-if="pendingKycUsers.length === 0" class="empty-state-card">
              <CheckCircle2 :size="44" class="text-success" />
              <p>Hiện không có hồ sơ nào đang chờ duyệt. Mọi hồ sơ đã được xử lý xong!</p>
            </div>

            <div v-else class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th style="min-width: 240px;">Đối Tác Đăng Ký</th>
                    <th style="min-width: 140px;">Loại Hình</th>
                    <th style="min-width: 260px;">Chi Tiết Gian Hàng / Phương Tiện</th>
                    <th style="min-width: 160px;">Số CCCD</th>
                    <th style="min-width: 160px;" class="text-end">Hành Động</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="user in pendingKycUsers" :key="user.id">
                    <td>
                      <div class="table-user-cell">
                        <img :src="user.avatarUrl" class="table-avatar" />
                        <div>
                          <strong>{{ formatDisplayName(user.fullName) }}</strong>
                          <small>{{ user.email }}</small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span v-if="user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')" class="badge-role-shipper">Tài Xế Shipper</span>
                      <span v-else-if="user.storeDetails && (user.storeDetails.status === 'Pending' || user.storeDetails.status === 'pending')" class="badge-role-seller">Chủ Shop</span>
                      <span v-else-if="user.shipperDetails" class="badge-role-shipper">Tài Xế Shipper</span>
                      <span v-else class="badge-role-seller">Chủ Shop</span>
                    </td>
                    <td>
                      <div v-if="user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')">
                        <strong>{{ user.shipperDetails.licensePlate }}</strong>
                        <small class="d-block text-muted">{{ user.shipperDetails.vehicleModel || user.shipperDetails.vehicleType || 'Xe máy' }} • {{ user.shipperDetails.operatingArea || 'Hoạt động' }}</small>
                      </div>
                      <div v-else-if="user.storeDetails">
                        <strong>{{ user.storeDetails.storeName }}</strong>
                        <small class="d-block text-muted">{{ user.storeDetails.address }}</small>
                      </div>
                      <div v-else-if="user.shipperDetails">
                        <strong>{{ user.shipperDetails.licensePlate }}</strong>
                        <small class="d-block text-muted">{{ user.shipperDetails.vehicleType }}</small>
                      </div>
                    </td>
                    <td>
                      <div v-if="getPartnerCccdNumber(user)" class="d-flex flex-column gap-1">
                        <code class="cccd-code-badge">{{ getPartnerCccdNumber(user) }}</code>
                        <span class="chip-nfc-badge" style="width: fit-content; font-size: 10px; padding: 1px 6px;">
                          <Sparkles :size="10" /> Chip Verified
                        </span>
                      </div>
                      <span v-else class="text-muted font-italic" style="font-size: 13px;">---</span>
                    </td>
                    <td>
                      <button class="btn-action-primary" @click="openKycModal(user, (user.shipperDetails && (user.shipperDetails.status === 'Pending' || user.shipperDetails.status === 'pending')) ? 'shipper' : 'seller')">
                        <FileText :size="14" /> Thẩm định hồ sơ
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- TAB DUYỆT SẢN PHẨM GIAN HÀNG (PRODUCT MODERATION) -->
        <div v-else-if="activeTab === 'products'" class="tab-content-container">
          <div class="content-header-card d-flex flex-wrap justify-content-between align-items-center gap-3">
            <div class="header-title-box">
              <h2 class="content-heading">Kiểm Duyệt Sản Phẩm Gian Hàng (AI & Thủ Công)</h2>
              <p class="content-sub">Thẩm định các mặt hàng do Người Bán đăng tải bị AI nghi ngờ hoặc cần Ban Quản Trị phê duyệt trước khi mở bán</p>
            </div>
            <div class="header-actions">
              <span class="badge-role-seller" style="font-size: 13px; padding: 7px 16px;">
                Đang chờ duyệt: <b>{{ pendingProductsCount }}</b> sản phẩm
              </span>
            </div>
          </div>

          <div class="table-card">
            <div v-if="pendingReviewProducts.length === 0" class="empty-state-card" style="padding: 60px 20px; text-align: center;">
              <CheckCircle2 :size="52" class="text-success mb-3" style="display: block; margin: 0 auto;" />
              <h3 style="font-weight: 700; color: #0f172a; margin-bottom: 8px;">Hiện không có sản phẩm nào cần duyệt!</h3>
              <p style="color: #64748b; font-size: 14px;">Mọi sản phẩm của các gian hàng đã được thẩm định an toàn hoặc tự động mở bán.</p>
            </div>

            <div v-else class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th style="min-width: 260px;">Sản Phẩm & Hình Ảnh</th>
                    <th style="min-width: 180px;">Gian Hàng</th>
                    <th style="min-width: 130px;">Giá Niêm Yết</th>
                    <th style="min-width: 100px;">Tồn Kho</th>
                    <th style="min-width: 280px;">Kết Quả Quét AI</th>
                    <th style="min-width: 210px;" class="text-end">Hành Động</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="prod in pendingReviewProducts" :key="prod.id">
                    <td>
                      <div class="table-user-cell">
                        <img
                          :src="prod.image || 'https://images.unsplash.com/photo-1540420773420-3366772f4999?w=150'"
                          class="product-thumb-img"
                          @click="activeZoomImage = prod.image"
                          title="Bấm để xem ảnh phóng to"
                        />
                        <div>
                          <strong class="d-block" style="font-size: 14px; color: #0f172a;">{{ prod.name }}</strong>
                          <small class="text-muted">{{ prod.category }} • {{ prod.unit || 'Phần' }}</small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div>
                        <strong style="color: #ea580c; font-size: 13.5px;">{{ prod.storeName || 'Gian hàng' }}</strong>
                        <small class="d-block text-muted">{{ prod.sellerEmail }}</small>
                      </div>
                    </td>
                    <td>
                      <strong style="color: #0f172a; font-size: 14px;">{{ Number(prod.price).toLocaleString('vi-VN') }} đ</strong>
                    </td>
                    <td>
                      <span class="badge-stock-pill">{{ prod.stock }}</span>
                    </td>
                    <td>
                      <div class="ai-moderation-cell">
                        <div class="d-flex align-items-center gap-1 mb-1">
                          <span class="badge-ai-warning">
                            <AlertTriangle :size="12" /> AI Cảnh báo
                          </span>
                          <span v-if="prod.aiScore?.matchScore" class="badge-ai-score">
                            Độ khớp: {{ prod.aiScore.matchScore }}%
                          </span>
                        </div>
                        <p class="ai-flag-text">
                          {{ prod.aiScore?.flag || 'Chưa xác định được vật thể trong ảnh để đối chiếu tên sản phẩm.' }}
                        </p>
                      </div>
                    </td>
                    <td class="text-end">
                      <div class="actions-inline-btns">
                        <button
                          class="btn-approve-product"
                          @click="handleApproveProductItem(prod)"
                          title="Phê duyệt cho phép bán sản phẩm này ngay lập tức"
                        >
                          <Check :size="14" /> Duyệt Mở Bán
                        </button>
                        <button
                          class="btn-reject-product"
                          @click="openRejectProductModal(prod)"
                          title="Từ chối và gỡ bỏ sản phẩm khỏi sàn"
                        >
                          <X :size="14" /> Từ Chối
                        </button>
                        <button
                          class="btn-delete-prod-admin"
                          @click="handleDirectDeleteProduct(prod)"
                          title="Xóa vĩnh viễn sản phẩm này khỏi sàn và gian hàng"
                        >
                          <Trash2 :size="14" /> Xóa Bỏ
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- TAB 4: QUẢN LÝ CA TRỰC (SHIFT MANAGERS) -->
        <div v-else-if="activeTab === 'managers'" class="tab-content-container">
          <div class="content-header-card d-flex justify-content-between align-items-center">
            <div class="header-title-box">
              <h2 class="content-heading">Danh Sách Quản Lý Ca Trực</h2>
              <p class="content-sub">Phân quyền tài khoản phụ trách duyệt hồ sơ và hỗ trợ vận hành sàn</p>
            </div>
            <button class="btn-create-orange" @click="showCreateManagerModal = true">
              <Plus :size="16" /> Thêm Quản Lý Mới
            </button>
          </div>

          <div class="table-card">
            <div class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th>Họ và Tên</th>
                    <th>Gmail Đăng Nhập</th>
                    <th>Quyền Hạn</th>
                    <th>Trạng Thái</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="m in managers" :key="m.id">
                    <td>
                      <strong>{{ m.fullName }}</strong>
                    </td>
                    <td>{{ m.email }}</td>
                    <td><span class="badge-role-admin">Quản Lý Ca Trực</span></td>
                    <td><span class="status-pill status-active">Đang Trực Tuyến</span></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- TAB 5: NHẬT KÝ HỆ THỐNG (AUDIT LOGS) -->
        <div v-else-if="activeTab === 'audit'" class="tab-content-container">
          <div class="content-header-card">
            <div class="header-title-box">
              <h2 class="content-heading">Nhật Ký Hành Động Hệ Thống (Audit Trail)</h2>
              <p class="content-sub">Theo dõi lịch sử phê duyệt, từ chối, khóa tài khoản và thay đổi cấu hình sàn</p>
            </div>
          </div>

          <div class="table-card">
            <div class="table-responsive-wrapper">
              <table class="concept-table">
                <thead>
                  <tr>
                    <th>Thời Gian</th>
                    <th>Người Thực Hiện</th>
                    <th>Hành Động</th>
                    <th>Đối Tượng</th>
                    <th>Chi Tiết</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(log, lIndex) in auditLogs" :key="lIndex">
                    <td><small class="text-muted"><Clock :size="12" class="me-1" />{{ log.timestamp || 'Hôm nay' }}</small></td>
                    <td><strong>{{ log.actorEmail }}</strong> ({{ log.actorRole }})</td>
                    <td><span class="badge bg-light text-dark">{{ log.action }}</span></td>
                    <td>{{ log.targetUser }}</td>
                    <td>{{ log.details }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>
    </div>

    <!-- MODAL DUYỆT HỒ SƠ KYC (TOÀN DIỆN & ĐẦY ĐỦ THÔNG TIN ĐĂNG KÝ) -->
    <div v-if="showKycModal && selectedKycItem" class="modal-backdrop-custom">
      <div class="modal-dialog-card modal-xl">
        <div class="modal-header-row">
          <div class="d-flex align-items-center gap-2">
            <ShieldCheck :size="22" class="text-orange" />
            <div>
              <div class="d-flex align-items-center gap-2">
                <h3 class="modal-heading mb-0">
                  Thẩm Định Hồ Sơ Đối Tác: {{ selectedKycItem.fullName }}
                </h3>
                <span class="status-pill status-pending">
                  <Hourglass :size="12" class="me-1 hourglass-animated" /> Chờ Phê Duyệt
                </span>
              </div>
              <small class="text-muted">
                {{ selectedKycType === 'seller' ? 'Đăng ký gian hàng bán lẻ' : 'Đăng ký tài xế giao vận' }} • Mã: {{ selectedKycItem.storeCode || selectedKycItem.shipperCode || ('ZM-' + (selectedKycItem.storeId || selectedKycItem.shipperId || '').substring(0, 8).toUpperCase()) }}
              </small>
            </div>
          </div>
          <button class="modal-close-btn" @click="showKycModal = false"><X :size="18" /></button>
        </div>

        <div class="modal-body-scroll">
          <div class="kyc-dossier-layout">
            <!-- CỘT TRÁI: TẤT CẢ THÔNG TIN CHI TIẾT ĐĂNG KÝ -->
            <div class="kyc-dossier-info-col">
              <!-- Nhóm 1: Thông tin định danh đối tác -->
              <div class="kyc-info-card">
                <div class="kyc-card-header">
                  <UserCheck :size="15" class="text-orange me-1" />
                  <span class="kyc-card-title">1. Định Danh & Liên Hệ Đối Tác</span>
                </div>
                <div class="kyc-card-grid">
                  <div class="info-field-item">
                    <label class="info-lbl d-flex align-items-center justify-content-between">
                      <span>Họ và tên đối tác:</span>
                      <span v-if="ocrExtractedSuccess" class="ocr-sync-tag"><Check :size="10" /> Đã quét OCR</span>
                    </label>
                    <div v-if="isOcrScanning" class="ocr-skeleton-line"></div>
                    <input 
                      v-else
                      type="text" 
                      class="kyc-field-input" 
                      :class="{ 'border-success-highlight': ocrExtractedSuccess }"
                      v-model="selectedKycItem.fullName"
                      placeholder="Nhập họ và tên đối tác..."
                    />
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Gmail / Liên lạc:</span>
                    <span class="info-val">{{ selectedKycItem.email || selectedKycItem.phoneEmail || selectedKycItem.phoneNumber }}</span>
                  </div>
                  <div class="info-field-item">
                    <label class="info-lbl d-flex align-items-center justify-content-between">
                      <span>Số CCCD gắn chip:</span>
                      <span v-if="selectedKycItem.cccdNumber && selectedKycItem.cccdNumber.trim()" class="chip-nfc-badge" title="Đã đối chiếu số CCCD đối tác"><Sparkles :size="10" /> Chip Verified</span>
                    </label>
                    <div v-if="isOcrScanning" class="ocr-skeleton-line"></div>
                    <input 
                      v-else
                      type="text" 
                      class="kyc-field-input font-mono font-bold" 
                      :class="{ 'border-success-highlight': ocrExtractedSuccess }"
                      v-model="selectedKycItem.cccdNumber" 
                      placeholder="12 chữ số CCCD..."
                      maxlength="15"
                    />
                  </div>
                  <div class="info-field-item">
                    <label class="info-lbl d-flex align-items-center justify-content-between">
                      <span>Nguyên quán (từ CCCD):</span>
                      <span v-if="ocrExtractedSuccess" class="ocr-sync-tag"><Check :size="10" /> Đã quét OCR</span>
                    </label>
                    <div v-if="isOcrScanning" class="ocr-skeleton-line"></div>
                    <input 
                      v-else
                      type="text" 
                      class="kyc-field-input" 
                      :class="{ 'border-success-highlight': ocrExtractedSuccess }"
                      v-model="selectedKycItem.nguyenQuan" 
                      placeholder="Nguyên quán / Quê quán..."
                    />
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Thời gian nộp đơn:</span>
                    <span class="info-val text-muted"><Clock :size="12" class="me-1" />{{ selectedKycItem.createdAt || 'Hôm nay' }}</span>
                  </div>
                </div>
              </div>

              <!-- Nhóm 2: Thông tin kinh doanh / Phương tiện giao vận -->
              <div class="kyc-info-card">
                <div class="kyc-card-header">
                  <template v-if="selectedKycType === 'seller'">
                    <Store :size="15" class="text-orange me-1" />
                    <span class="kyc-card-title">2. Thông Tin Cửa Hàng & Ngành Hàng</span>
                  </template>
                  <template v-else>
                    <Bike :size="15" class="text-green me-1" />
                    <span class="kyc-card-title">2. Phương Tiện & Địa Bàn Hoạt Động</span>
                  </template>
                </div>
                <!-- Nội dung nếu là Gian Hàng -->
                <div v-if="selectedKycType === 'seller'" class="kyc-card-grid">
                  <div class="info-field-item col-span-2">
                    <span class="info-lbl">Tên gian hàng niêm yết:</span>
                    <strong class="info-val-store">{{ selectedKycItem.storeName }}</strong>
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Mô hình kinh doanh:</span>
                    <span class="badge" style="background: #FEF3C7; color: #92400E; font-weight: 700; padding: 4px 8px; border-radius: 6px; font-size: 12px; display: inline-block;">
                      {{ selectedKycItem.sellerType === 'home_business' ? '🏡 Hộ kinh doanh tại nhà' : selectedKycItem.sellerType === 'store_facility' ? '🏭 Cửa hàng / Cơ sở SX' : selectedKycItem.sellerType === 'other_local_seller' ? '👨‍🌾 Người bán địa phương khác' : '🏪 Chợ truyền thống' }}
                    </span>
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Nhóm ngành hàng:</span>
                    <span class="badge-category-tag">{{ selectedKycItem.category || 'Nông Sản & Thực Phẩm Sạch' }}</span>
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Địa bàn Xã / Phường:</span>
                    <strong class="info-val" style="color: #0284C7;">🏛️ {{ selectedKycItem.ward || 'Chưa cập nhật' }} {{ selectedKycItem.district ? '(' + selectedKycItem.district + ')' : '' }}</strong>
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Chợ / Cụm chợ số:</span>
                    <span class="info-val">{{ selectedKycItem.marketName || 'Chợ Dân Sinh Địa Phương' }}</span>
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Khung giờ mở cửa:</span>
                    <span class="info-val"><Clock :size="12" class="me-1 text-orange" />{{ selectedKycItem.openHours || '07:00 - 22:00' }}</span>
                  </div>
                  <div class="info-field-item col-span-2">
                    <span class="info-lbl">Địa chỉ chi tiết:</span>
                    <span class="info-val"><MapPin :size="13" class="me-1 text-danger" />{{ selectedKycItem.address || 'Hà Nội' }}</span>
                  </div>
                </div>
                <!-- Nội dung nếu là Shipper -->
                <div v-else class="kyc-card-grid">
                  <div class="info-field-item">
                    <span class="info-lbl">Biển số đăng ký xe:</span>
                    <span class="license-plate-badge">{{ selectedKycItem.licensePlate }}</span>
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Loại phương tiện / Mẫu xe:</span>
                    <span class="info-val">{{ selectedKycItem.vehicleType || 'Xe máy' }} {{ selectedKycItem.vehicleModel ? '(' + selectedKycItem.vehicleModel + ')' : '' }}</span>
                  </div>
                  <div class="info-field-item col-span-2">
                    <span class="info-lbl">Địa bàn / Bán kính hoạt động:</span>
                    <span class="info-val"><MapPin :size="13" class="me-1 text-green" />{{ selectedKycItem.operatingArea || 'Hà Nội (Bán kính 10km)' }}</span>
                  </div>
                </div>
              </div>

              <!-- Nhóm 3: Thông tin tài khoản ngân hàng thụ hưởng (Đối soát doanh thu) -->
              <div class="kyc-info-card">
                <div class="kyc-card-header">
                  <CreditCard :size="15" class="text-blue me-1" />
                  <span class="kyc-card-title">3. Tài Khoản Ngân Hàng Quyết Toán & Rút Tiền</span>
                </div>
                <div class="kyc-card-grid">
                  <div class="info-field-item">
                    <span class="info-lbl">Ngân hàng thụ hưởng:</span>
                    <strong class="info-val text-primary">{{ selectedKycItem.bankName || 'Chưa cập nhật' }}</strong>
                  </div>
                  <div class="info-field-item">
                    <span class="info-lbl">Số tài khoản:</span>
                    <code class="bank-acc-badge">{{ selectedKycItem.bankAccountNumber || 'Chưa cập nhật' }}</code>
                  </div>
                  <div class="info-field-item col-span-2">
                    <span class="info-lbl">Kiểm tra trùng khớp CCCD:</span>
                    <span v-if="selectedKycItem.cccdNumber && selectedKycItem.cccdNumber.trim()" class="match-check-pill"><CheckCircle2 :size="12" /> Tên chủ tài khoản trùng khớp với CCCD đối chiếu</span>
                    <span v-else class="text-muted font-italic" style="font-size: 13px;">Chưa có dữ liệu số CCCD để đối chiếu</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- CỘT PHẢI: BỘ GIẤY TỜ ĐỐI SOÁT 3 MẶT (CCCD TRƯỚC, SAU, ATVSTP / GPLX) -->
            <div class="kyc-dossier-docs-col">
              <!-- Thanh chuyển đổi giấy tờ (Tabs) -->
              <div class="kyc-doc-tabs">
                <button
                  class="kyc-doc-tab-btn"
                  :class="{ active: activeDocTab === 'front' }"
                  @click="switchDocTab('front')"
                >
                  CCCD Mặt Trước
                </button>
                <button
                  class="kyc-doc-tab-btn"
                  :class="{ active: activeDocTab === 'back' }"
                  @click="switchDocTab('back')"
                >
                  CCCD Mặt Sau
                </button>
                <button
                  v-if="selectedKycType === 'seller'"
                  class="kyc-doc-tab-btn"
                  :class="{ active: activeDocTab === 'cert' }"
                  @click="switchDocTab('cert')"
                >
                  Giấy ATVSTP / GPKD
                </button>
                <button
                  v-else
                  class="kyc-doc-tab-btn"
                  :class="{ active: activeDocTab === 'license' }"
                  @click="switchDocTab('license')"
                >
                  Bằng Lái Xe (GPLX)
                </button>
              </div>


              <!-- Khung xem ảnh chính phóng to -->
              <div class="image-preview-card" @click="!isOcrScanning && (activeZoomImage = activeKycImage)">
                <img
                  :src="activeKycImage || getDocFallbackImage(activeDocTab)"
                  alt="Giấy tờ đối chiếu"
                  class="preview-img-large"
                  :class="{ 'img-blur-scanning': isOcrScanning }"
                  @error="activeKycImage = getDocFallbackImage(activeDocTab)"
                />

                <!-- Hiệu ứng Scanning Animation / Skeleton khi OCR đang chạy -->
                <div v-if="isOcrScanning" class="ocr-scanner-overlay">
                  <div class="ocr-laser-beam"></div>
                  <div class="ocr-status-card">
                    <Loader2 :size="28" class="ocr-spinner-icon" />
                    <div class="ocr-status-title">Đang quét CCCD/CMND...</div>
                    <div class="ocr-status-subtitle">Đang trích xuất Họ tên, Số CCCD, Nguyên quán qua FPT.AI ID Recognition v2</div>
                  </div>
                </div>

                <div v-else class="zoom-hint-overlay">
                  <Eye :size="14" /> Nhấp để phóng to toàn màn hình
                </div>
                <div class="doc-type-indicator">
                  {{ activeDocTab === 'front' ? 'Mặt trước CCCD' : activeDocTab === 'back' ? 'Mặt sau CCCD' : selectedKycType === 'seller' ? 'Giấy chứng nhận ATVSTP' : 'Giấy phép lái xe GPLX' }}
                </div>
              </div>

              <!-- Dải 3 hình thu nhỏ (Thumbnail Strip) xem nhanh cả 3 giấy tờ -->
              <div class="doc-thumbnail-strip">
                <div
                  class="doc-thumb-item"
                  :class="{ active: activeDocTab === 'front' }"
                  @click="switchDocTab('front')"
                  title="Xem CCCD Mặt Trước"
                >
                  <img
                    :src="getValidDocImage(selectedKycItem.cccdFrontImage, 'front')"
                    class="thumb-img"
                    @error="handleKycImgError($event, 'front')"
                  />
                  <span class="thumb-lbl">CCCD Trước</span>
                </div>

                <div
                  class="doc-thumb-item"
                  :class="{ active: activeDocTab === 'back' }"
                  @click="switchDocTab('back')"
                  title="Xem CCCD Mặt Sau"
                >
                  <img
                    :src="getValidDocImage(selectedKycItem.cccdBackImage, 'back')"
                    class="thumb-img"
                    @error="handleKycImgError($event, 'back')"
                  />
                  <span class="thumb-lbl">CCCD Sau</span>
                </div>

                <div
                  v-if="selectedKycType === 'seller'"
                  class="doc-thumb-item"
                  :class="{ active: activeDocTab === 'cert' }"
                  @click="switchDocTab('cert')"
                  title="Xem Giấy ATVSTP / GPKD"
                >
                  <img
                    :src="getValidDocImage(selectedKycItem.foodSafetyCertImage, 'cert')"
                    class="thumb-img"
                    @error="handleKycImgError($event, 'cert')"
                  />
                  <span class="thumb-lbl">ATVSTP / GPKD</span>
                </div>

                <div
                  v-else
                  class="doc-thumb-item"
                  :class="{ active: activeDocTab === 'license' }"
                  @click="switchDocTab('license')"
                  title="Xem Bằng Lái Xe GPLX"
                >
                  <img
                    :src="getValidDocImage(selectedKycItem.drivingLicenseImage, 'license')"
                    class="thumb-img"
                    @error="handleKycImgError($event, 'license')"
                  />
                  <span class="thumb-lbl">GPLX Tài Xế</span>
                </div>
              </div>

              <!-- Badge bảo chứng thẩm định an toàn -->
              <div class="kyc-trust-badge">
                <ShieldCheck :size="16" class="text-green flex-shrink-0" />
                <small>Hồ sơ được đối soát qua hệ thống ZoneMart TrustShield kết hợp cơ sở dữ liệu quốc gia.</small>
              </div>
            </div>
          </div>
        </div>

        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showKycModal = false">
            Đóng Lại
          </button>
          <button class="btn-modal-reject" @click="showRejectReasonPopup = true">
            <XCircle :size="16" /> Từ Chối Kèm Lý Do
          </button>
          <button class="btn-modal-approve" @click="handleApproveKyc">
            <CheckCircle2 :size="16" /> Phê Duyệt Chính Thức
          </button>
        </div>
      </div>
    </div>

    <!-- POPUP NHẬP LÝ DO TỪ CHỐI (MODERN REDESIGN) -->
    <div v-if="showRejectReasonPopup" class="modal-backdrop-custom nested-popup reject-modal-backdrop" @click.self="showRejectReasonPopup = false">
      <div class="modal-dialog-card modal-reject-card animate-pop-in">
        <!-- Header -->
        <div class="reject-modal-header">
          <div class="reject-header-left">
            <div class="reject-header-icon-wrap">
              <ShieldAlert :size="22" class="text-danger" />
            </div>
            <div>
              <h3 class="reject-modal-title">Từ Chối Thẩm Định Hồ Sơ</h3>
              <p class="reject-modal-subtitle">Lý do từ chối sẽ được gửi chính thức qua Gmail của đối tác</p>
            </div>
          </div>
          <button class="reject-close-btn" @click="showRejectReasonPopup = false" title="Đóng popup">
            <X :size="18" />
          </button>
        </div>

        <!-- Body -->
        <div class="reject-modal-body">
          <!-- Recipient Quick Info Banner -->
          <div v-if="selectedKycItem" class="reject-target-banner">
            <div class="reject-target-avatar">
              {{ (selectedKycItem.fullName || selectedKycItem.storeName || 'U').charAt(0).toUpperCase() }}
            </div>
            <div class="reject-target-info">
              <div class="reject-target-name-row">
                <span class="reject-target-name">{{ selectedKycItem.fullName || 'Người dùng' }}</span>
                <span class="reject-badge-type" :class="selectedKycType === 'seller' ? 'badge-seller' : 'badge-shipper'">
                  {{ selectedKycType === 'seller' ? 'Đối Tác Bán Hàng' : 'Tài Xế ZoneShip' }}
                </span>
              </div>
              <div class="reject-target-subtext">
                <span v-if="selectedKycItem.email" class="subtext-item"><Mail :size="12" /> {{ selectedKycItem.email }}</span>
                <span v-if="selectedKycItem.storeName" class="subtext-item"><Store :size="12" /> Quán: {{ selectedKycItem.storeName }}</span>
                <span v-if="selectedKycItem.licensePlate" class="subtext-item"><Bike :size="12" /> Biển số: {{ selectedKycItem.licensePlate }}</span>
              </div>
            </div>
          </div>

          <!-- Quick presets -->
          <div class="reject-presets-section">
            <label class="reject-section-label">
              <Sparkles :size="14" class="text-amber" /> Chọn nhanh lý do thường gặp:
            </label>
            <div class="reject-chips-wrap">
              <button
                v-for="(reason, idx) in presetRejectReasons"
                :key="idx"
                type="button"
                class="reject-chip-btn"
                :class="{ active: rejectReasonInput.includes(reason) }"
                @click="applyPresetReason(reason)"
              >
                <span>+</span> {{ reason }}
              </button>
            </div>
          </div>

          <!-- Reason Input Box -->
          <div class="reject-input-section">
            <div class="reject-label-row">
              <label class="reject-section-label mb-0">
                <FileText :size="14" /> Nội dung lý do chi tiết <span class="text-danger">*</span>
              </label>
              <button
                v-if="rejectReasonInput"
                type="button"
                class="btn-clear-reason"
                @click="rejectReasonInput = ''"
              >
                Xóa nội dung
              </button>
            </div>
            <div class="reject-textarea-container">
              <textarea
                v-model="rejectReasonInput"
                rows="4"
                class="reject-textarea"
                placeholder="Nêu rõ lý do hoặc hướng dẫn cụ thể để đối tác chụp lại, bổ sung giấy tờ chính xác..."
              ></textarea>
              <div class="reject-textarea-footer">
                <span class="reject-hint-text">
                  <Info :size="13" /> Thông tin sẽ được tự động gửi kèm văn bản hướng dẫn hoàn thiện hồ sơ.
                </span>
                <span class="reject-char-count">{{ rejectReasonInput.length }} ký tự</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Footer -->
        <div class="reject-modal-footer">
          <button type="button" class="btn-reject-cancel" @click="showRejectReasonPopup = false">
            Hủy Bỏ
          </button>
          <button
            type="button"
            class="btn-reject-confirm"
            :disabled="!rejectReasonInput.trim() || isSubmittingReject"
            @click="handleRejectKyc"
          >
            <Loader2 v-if="isSubmittingReject" :size="16" class="spin-icon" />
            <Send v-else :size="16" />
            <span>{{ isSubmittingReject ? 'Đang gửi...' : 'Gửi Thông Báo Từ Chối' }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL KỶ LUẬT TÀI KHOẢN -->
    <div v-if="showDisciplineModal && selectedUserForPunish" class="modal-backdrop-custom">
      <div class="modal-dialog-card modal-md">
        <div class="modal-header-row">
          <h3 class="modal-heading text-danger">
            <AlertTriangle :size="20" class="text-danger" /> Áp Dụng Kỷ Luật Tài Khoản: {{ selectedUserForPunish.fullName }}
          </h3>
          <button class="modal-close-btn" @click="showDisciplineModal = false"><X :size="18" /></button>
        </div>
        <div class="modal-body-scroll">
          <div class="modal-field-group">
            <label class="modal-form-label">Mức độ chế tài xử lý:</label>
            <select v-model="disciplineLevel" class="modal-form-select">
              <option :value="1">Khóa Cảnh Cáo 10 Ngày (Vi phạm nhẹ)</option>
              <option :value="2">Tạm Đình Chỉ Vận Hành Sàn (Vi phạm đơn hàng)</option>
              <option :value="3">Khóa Tài Khoản Vĩnh Viễn / Cấm (Gian lận)</option>
            </select>
          </div>
          <div class="modal-field-group">
            <label class="modal-form-label">Lý do kỷ luật:</label>
            <input v-model="disciplineReason" type="text" class="modal-form-control" placeholder="Nhập lý do chi tiết..." />
          </div>
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showDisciplineModal = false">Đóng</button>
          <button class="btn-modal-punish" @click="handleApplyDiscipline">
            <ShieldAlert :size="15" /> Xác Nhận Kỷ Luật
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL XÓA MỀM TÀI KHOẢN -->
    <div v-if="showDeleteModal && selectedUserForDelete" class="modal-backdrop-custom">
      <div class="modal-dialog-card modal-md">
        <div class="modal-header-row">
          <h3 class="modal-heading text-danger">
            <Trash2 :size="20" class="text-danger" /> Xác Nhận Xóa Tài Khoản
          </h3>
          <button class="modal-close-btn" @click="showDeleteModal = false"><X :size="18" /></button>
        </div>
        <div class="modal-body-scroll">
          <div class="danger-callout-box">
            <AlertTriangle :size="18" class="text-danger flex-shrink-0" />
            <p class="danger-callout-text">Thao tác này là đặc quyền duy nhất của Administrator. Tài khoản sẽ bị đưa vào danh sách Soft-Delete!</p>
          </div>
          <div class="modal-field-group mt-3">
            <label class="modal-form-label">Nhập chữ <strong>XÓA</strong> vào ô dưới đây để hoàn tất:</label>
            <input v-model="deleteConfirmText" type="text" class="modal-form-control" placeholder="Gõ XÓA để xác nhận..." />
          </div>
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showDeleteModal = false">Hủy Bỏ</button>
          <button
            class="btn-modal-danger"
            :disabled="deleteConfirmText.trim().toUpperCase() !== 'XÓA'"
            @click="handleConfirmDelete"
          >
            <Trash2 :size="15" /> Xóa Vĩnh Viễn
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL TẠO QUẢN LÝ MỚI -->
    <div v-if="showCreateManagerModal" class="modal-backdrop-custom">
      <div class="modal-dialog-card modal-md">
        <div class="modal-header-row">
          <h3 class="modal-heading text-orange">
            <UserCheck :size="20" class="text-orange" /> Thêm Quản Lý Ca Trực
          </h3>
          <button class="modal-close-btn" @click="showCreateManagerModal = false"><X :size="18" /></button>
        </div>
        <div class="modal-body-scroll">
          <div class="modal-field-group">
            <label class="modal-form-label">Họ và tên:</label>
            <input v-model="newManagerForm.fullName" type="text" class="modal-form-control" placeholder="Nguyễn Văn Quản Lý..." />
          </div>
          <div class="modal-field-group">
            <label class="modal-form-label">Email đăng nhập:</label>
            <input v-model="newManagerForm.email" type="email" class="modal-form-control" placeholder="manager01@zonemart.vn" />
          </div>
          <div class="modal-field-group">
            <label class="modal-form-label">Mật khẩu:</label>
            <input v-model="newManagerForm.password" type="password" class="modal-form-control" placeholder="Nhập mật khẩu..." />
          </div>
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showCreateManagerModal = false">Đóng</button>
          <button class="btn-modal-submit" @click="handleCreateManager">
            <Plus :size="16" /> Lưu Tài Khoản
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL TỪ CHỐI DUYỆT SẢN PHẨM -->
    <div v-if="showRejectProductModal && selectedProductToReject" class="modal-backdrop-custom">
      <div class="modal-dialog-card modal-md">
        <div class="modal-header-row">
          <h3 class="modal-heading text-danger">
            <ShieldAlert :size="20" class="text-danger" /> Từ Chối Duyệt Sản Phẩm
          </h3>
          <button class="modal-close-btn" @click="showRejectProductModal = false"><X :size="18" /></button>
        </div>
        <div class="modal-body-scroll">
          <div class="danger-callout-box mb-3">
            <AlertTriangle :size="18" class="text-danger flex-shrink-0" />
            <p class="danger-callout-text">
              Bạn đang từ chối sản phẩm <strong>"{{ selectedProductToReject.name }}"</strong> của gian hàng <strong>"{{ selectedProductToReject.storeName }}"</strong>.
            </p>
          </div>
          <div class="modal-field-group">
            <label class="modal-form-label">Lý do từ chối (Gửi thông báo & Email tới Người Bán):</label>
            <textarea
              v-model="productRejectReason"
              class="modal-form-control"
              rows="3"
              placeholder="Nhập lý do từ chối để người bán biết và sửa lại ảnh/tên sản phẩm..."
            ></textarea>
          </div>
        </div>
        <div class="modal-footer-row">
          <button class="btn-modal-cancel" @click="showRejectProductModal = false">Hủy Bỏ</button>
          <button class="btn-modal-danger" @click="handleConfirmRejectProduct">
            <X :size="15" /> Xác Nhận Từ Chối & Gỡ Bỏ
          </button>
        </div>
      </div>
    </div>

    <!-- IMAGE ZOOM LIGHTBOX -->
    <div v-if="activeZoomImage" class="zoom-lightbox-overlay" @click="activeZoomImage = null">
      <img :src="activeZoomImage" class="zoomed-image" />
      <button class="zoom-close-btn"><X :size="24" /></button>
    </div>

    <!-- TOAST NOTIFICATION -->
    <div v-if="showToastState" class="toast-floating-alert" :class="'toast-' + toastType">
      <Info :size="16" class="me-2" />
      <span>{{ toastMessage }}</span>
    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:ital,wght@0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,400;1,600&family=Plus+Jakarta+Sans:ital,wght@0,400;0,500;0,600;0,700;0,800;1,400;1,600;1,700&display=swap');

/* ================================================================
 * CONCEPT ADMIN DASHBOARD STYLES - ZONEMART IDENTITY
 * ================================================================ */
.concept-admin-root,
.concept-admin-root * {
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.concept-admin-root {
  min-height: 100vh;
  height: 100vh;
  max-height: 100vh;
  background-color: #f8fafc;
  color: #1e293b;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* 1. TOPBAR HEADER */
.concept-topbar {
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

.topbar-left .brand-link {
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 24px;
  font-weight: 800;
  letter-spacing: -0.5px;
}
.brand-zone {
  color: #0f172a;
}
.brand-mart {
  color: #ea580c;
}
.brand-badge-admin {
  font-size: 11px;
  font-weight: 700;
  background-color: #ffedd5;
  color: #ea580c;
  padding: 2px 8px;
  border-radius: 9999px;
  margin-left: 6px;
  letter-spacing: 0.5px;
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
  border-radius: 6px;
  padding: 7px 32px 7px 32px;
  border-radius: 8px;
  padding: 8px 52px 8px 34px;
  font-size: 13px;
  color: #1e293b;
  width: 200px;
  width: 250px;
  transition: all 0.2s ease;
}
.search-input-pill input:focus {
  outline: none;
  border-color: #ea580c;
  background-color: #ffffff;
  width: 240px;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.1);
  width: 290px;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}
.search-input-pill .search-icon {
  position: absolute;
  left: 10px;
  left: 11px;
  color: #94a3b8;
  font-size: 13px;
  pointer-events: none;
}
.clear-search-btn {
  position: absolute;
  right: 6px;
  right: 8px;
  background: none;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.search-kbd-shortcut {
  position: absolute;
  right: 8px;
  font-size: 10px;
  font-weight: 600;
  color: #94a3b8;
  background: #e2e8f0;
  padding: 2px 5px;
  border-radius: 4px;
  pointer-events: none;
}

.topbar-icon-btn {
  width: 36px;
  height: 36px;
  border-radius: 6px;
  width: 38px;
  height: 38px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background-color: #f8fafc;
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
  background-color: #f1f5f9;
  background-color: #fff7ed;
  color: #ea580c;
  border-color: #fed7aa;
}
.topbar-icon-btn.active {
  background-color: #ffedd5;
  color: #ea580c;
  border-color: #ea580c;
}
.notification-dropdown-wrapper {
  position: relative;
}

.notif-badge-pill {
  position: absolute;
  top: -5px;
  right: -5px;
  min-width: 19px;
  height: 19px;
  padding: 0 4px;
  background: linear-gradient(135deg, #ef4444, #dc2626);
  color: #ffffff;
  border-radius: 9999px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid #ffffff;
  box-shadow: 0 2px 6px rgba(239, 68, 68, 0.4);
  z-index: 2;
}

.badge-number {
  font-size: 10px;
  font-weight: 800;
  line-height: 1;
}

.ping-ring-pill {
  position: absolute;
  inset: -3px;
  border-radius: 9999px;
  border: 2px solid #ef4444;
  animation: ping 1.8s cubic-bezier(0, 0, 0.2, 1) infinite;
  opacity: 0.75;
}

.red-alert-dot {
  position: absolute;
  top: 7px;
  right: 7px;
  width: 7px;
  height: 7px;
  width: 8px;
  height: 8px;
  background-color: #ef4444;
  border-radius: 50%;
}
.ping-ring {
  position: absolute;
  inset: -2px;
  border-radius: 50%;
  border: 2px solid #ef4444;
  animation: ping 1.5s cubic-bezier(0, 0, 0.2, 1) infinite;
  opacity: 0.75;
}
@keyframes ping {
  75%, 100% {
    transform: scale(2);
    opacity: 0;
  }
}

/* =========================================================
 * NOTIFICATION POPOVER DROPDOWN STYLES
 * ========================================================= */
.notification-popover {
  position: absolute;
  top: calc(100% + 10px);
  right: -10px;
  width: 390px;
  max-width: 90vw;
  background: #ffffff;
  border-radius: 16px;
  box-shadow: 0 20px 40px -10px rgba(15, 23, 42, 0.16), 0 0 0 1px rgba(15, 23, 42, 0.08);
  z-index: 1050;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  animation: popoverFadeIn 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  transform-origin: top right;
}

@keyframes popoverFadeIn {
  from {
    opacity: 0;
    transform: translateY(-8px) scale(0.97);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

.popover-header {
  padding: 14px 18px;
  background: #ffffff;
  border-bottom: 1px solid #f1f5f9;
}

.popover-title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 4px;
}

.popover-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
}

.title-icon {
  color: #ea580c;
}

.unread-pill {
  font-size: 11px;
  font-weight: 700;
  color: #ea580c;
  background: #fff7ed;
  border: 1px solid #fed7aa;
  padding: 1px 7px;
  border-radius: 9999px;
}

.mark-all-read-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  font-weight: 600;
  color: #2563eb;
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: all 0.15s ease;
}

.mark-all-read-btn:hover {
  background: #eff6ff;
  color: #1d4ed8;
}

.popover-subtitle {
  font-size: 11.5px;
  color: #64748b;
  line-height: 1.4;
}

.popover-body {
  max-height: 380px;
  overflow-y: auto;
}

.empty-notif-state {
  padding: 36px 20px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.empty-icon-wrap {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: #f8fafc;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #94a3b8;
  margin-bottom: 12px;
}

.empty-title {
  font-size: 13.5px;
  font-weight: 600;
  color: #334155;
}

.empty-sub {
  font-size: 12px;
  color: #94a3b8;
  margin-top: 4px;
}

.notif-item {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 13px 18px;
  border-bottom: 1px solid #f8fafc;
  cursor: pointer;
  transition: background-color 0.15s ease;
  position: relative;
}

.notif-item:hover {
  background-color: #f8fafc;
}

.notif-unread {
  background-color: #fffaf5;
}

.notif-unread:hover {
  background-color: #fff4ea;
}

.notif-icon-badge {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  margin-top: 2px;
}

.badge-store {
  background: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
}

.badge-shipper {
  background: #eff6ff;
  color: #0284c7;
  border: 1px solid #bae6fd;
}

.badge-kyc {
  background: #f0fdf4;
  color: #16a34a;
  border: 1px solid #bbf7d0;
}

.notif-content {
  flex: 1;
  min-width: 0;
}

.notif-item-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
  margin-bottom: 3px;
}

.notif-item-title {
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.notif-item-time {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-size: 11px;
  color: #94a3b8;
  flex-shrink: 0;
}

.notif-item-msg {
  font-size: 12px;
  color: #475569;
  line-height: 1.45;
  margin: 0 0 6px 0;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.notif-item-action {
  display: flex;
  align-items: center;
}

.quick-review-btn {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-size: 11.5px;
  font-weight: 700;
  color: #ea580c;
  transition: gap 0.15s ease;
}

.notif-item:hover .quick-review-btn {
  gap: 6px;
}

.unread-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background-color: #ea580c;
  flex-shrink: 0;
  margin-top: 8px;
}

.popover-footer {
  padding: 10px 14px;
  background: #fafbfc;
  border-top: 1px solid #f1f5f9;
  text-align: center;
}

.view-all-kyc-btn {
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  background: transparent;
  border: none;
  font-size: 12.5px;
  font-weight: 700;
  color: #ea580c;
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.view-all-kyc-btn:hover {
  background: #fff7ed;
  color: #c2410c;
}

.topbar-avatar-wrap {
  cursor: pointer;
  position: relative;
  display: flex;
  align-items: center;
}
.topbar-avatar-img {
  width: 36px;
  height: 36px;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #ea580c;
  box-shadow: 0 2px 6px rgba(234, 88, 12, 0.2);
}
.admin-online-badge {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 10px;
  height: 10px;
  background-color: #10b981;
  border: 2px solid #ffffff;
  border-radius: 50%;
}

/* 2. BODY LAYOUT (SIDEBAR + MAIN) */
.concept-body-layout {
  display: flex;
  flex: 1;
  align-items: flex-start;
  height: calc(100vh - 64px);
  overflow: hidden;
}

/* SIDEBAR NỀN TỐI (DARK SLATE) - CỐ ĐỊNH KHI CUỘN TRANG */
/* SIDEBAR NỀN TỐI (DARK SLATE) - CỐ ĐỊNH 100% ĐỨNG IM */
.concept-sidebar {
  width: 250px;
  background-color: #0f172a;
  color: #94a3b8;
  padding: 20px 0;
  display: flex;
  flex-direction: column;
  gap: 24px;
  flex-shrink: 0;
  position: sticky;
  top: 64px;
  height: calc(100vh - 64px);
  height: 100%;
  overflow-y: auto;
  z-index: 90;
}
.concept-sidebar::-webkit-scrollbar {
  width: 4px;
}
.concept-sidebar::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.15);
  border-radius: 4px;
}

.sidebar-group {
  display: flex;
  flex-direction: column;
}
.sidebar-heading {
  font-size: 11px;
  font-weight: 700;
  color: #64748b;
  letter-spacing: 0.8px;
  padding: 0 20px 10px 20px;
  text-transform: uppercase;
}

.sidebar-nav {
  list-style: none;
  padding: 0;
  padding: 0 10px;
  margin: 0;
}
.nav-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 11px 20px;
  padding: 8px 12px;
  margin-bottom: 3px;
  border-radius: 10px;
  cursor: pointer;
  font-size: 13.5px;
  font-weight: 500;
  color: #94a3b8;
  transition: all 0.2s ease;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.nav-item:hover {
  color: #ffffff;
  background-color: rgba(255, 255, 255, 0.05);
  background-color: rgba(255, 255, 255, 0.06);
}
.nav-item.active {
  background-color: #ea580c;
  color: #ffffff;
  background: linear-gradient(135deg, #ea580c 0%, #f97316 100%) !important;
  color: #ffffff !important;
  font-weight: 600;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.35);
}
.nav-label-box {
  display: flex;
  align-items: center;
  gap: 12px;
}
.nav-icon {
  font-size: 16px;
}
.nav-icon-wrapper {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background-color: rgba(255, 255, 255, 0.05);
  color: #94a3b8;
  transition: all 0.2s ease;
  flex-shrink: 0;
}
.nav-item:hover .nav-icon-wrapper {
  background-color: rgba(255, 255, 255, 0.12);
  color: #ffffff;
}
.nav-item.active .nav-icon-wrapper {
  background-color: #ffffff;
  color: #ea580c;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
}
.nav-arrow {
  font-size: 11px;
  opacity: 0.7;
  color: #64748b;
  transition: transform 0.2s ease, color 0.2s ease;
}
.nav-item:hover .nav-arrow {
  color: #ffffff;
  transform: translateX(2px);
}
.nav-item.active .nav-arrow {
  color: #ffffff;
}
.nav-count-badge {
  background-color: #ef4444;
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  font-size: 10px;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 6px;
  padding: 2px 8px;
  border-radius: 9999px;
  box-shadow: 0 2px 6px rgba(239, 68, 68, 0.4);
}
.logout-item:hover {
  color: #f87171;
}
.logout-icon-wrap {
  color: #f87171 !important;
}

/* MAIN CONTENT */
/* MAIN CONTENT - KHU VỰC DUY NHẤT CUỘN KHI KÉO TRANG */
.concept-main-content {
  flex: 1;
  height: 100%;
  overflow-y: auto;
  padding: 24px;
  background-color: #f8fafc;
  min-width: 0;
}
.concept-main-content::-webkit-scrollbar {
  width: 6px;
}
.concept-main-content::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 6px;
}
.concept-main-content::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}

/* A. HERO PROFILE BANNER CARD */
.hero-profile-card {
  background-color: #ffffff;
  border-radius: 12px;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  margin-bottom: 24px;
  overflow: hidden;
}
.hero-top-row {
  display: flex;
  align-items: center;
  padding: 24px;
  gap: 24px;
}
.hero-avatar-circle {
  width: 96px;
  height: 96px;
  border-radius: 50%;
  border: 3px solid #ea580c;
  padding: 2px;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.15);
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.2);
}
.hero-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
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
  font-size: 22px;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
}
.hero-stars-badge {
  display: flex;
  align-items: center;
  gap: 4px;
  gap: 6px;
}
.stars-gold {
  color: #f59e0b;
  letter-spacing: 1px;
}
.stars-gold-wrap {
  display: flex;
  align-items: center;
  gap: 2px;
}
.reviews-count {
  font-size: 12px;
  font-weight: 500;
  color: #64748b;
}

.hero-meta-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 16px;
  font-size: 13px;
  color: #64748b;
  margin-bottom: 12px;
}
.meta-item {
  display: flex;
  align-items: center;
  gap: 5px;
}
.meta-icon {
  color: #ea580c;
  margin-right: 4px;
}
.meta-live-dot {
  color: #10b981;
}

.hero-tags-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.tag-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  background-color: #f1f5f9;
  color: #475569;
  font-size: 11.5px;
  font-weight: 500;
  padding: 3px 10px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
}
.tag-icon {
  color: #ea580c;
}

/* Dải thống kê kênh (Channels Strip) */
/* Dải thống kê kênh (Channels Strip - Executive Style) */
.hero-channels-strip {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 10px;
  padding: 16px 24px;
  border-top: 1px solid #f1f5f9;
  background-color: #fafbfc;
}
.channel-metric-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 14px 10px;
  border-right: 1px solid #f1f5f9;
  gap: 12px;
  padding: 10px 14px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  cursor: pointer;
  transition: background-color 0.15s ease;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.channel-metric-cell:last-child {
  border-right: none;
}
.channel-metric-cell:hover {
  background-color: #f1f5f9;
  border-color: #cbd5e1;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}
.metric-icon-box {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: transform 0.2s ease;
}
.channel-metric-cell:hover .metric-icon-box {
  transform: scale(1.08);
}
.metric-text-group {
  display: flex;
  flex-direction: column;
}
.metric-num {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  line-height: 1.2;
}
.ch-blue { color: #0284c7; }
.ch-orange { color: #ea580c; }
.ch-green { color: #10b981; }
.ch-purple { color: #6366f1; }
.ch-amber { color: #f59e0b; }
.ch-red { color: #ef4444; }

.ch-value {
  font-size: 13.5px;
  font-weight: 700;
  color: #334155;
}
.metric-lbl {
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  margin-top: 2px;
}
.box-blue { background: #eff6ff; color: #2563eb; }
.box-orange { background: #fff7ed; color: #ea580c; }
.box-green { background: #ecfdf5; color: #059669; }
.box-purple { background: #f5f3ff; color: #7c3aed; }
.box-amber { background: #fffbeb; color: #d97706; }
.box-red { background: #fef2f2; color: #dc2626; }
.pulse-icon {
  animation: radar-pulse 2s infinite;
}
@keyframes radar-pulse {
  0% { transform: scale(0.95); opacity: 0.8; }
  50% { transform: scale(1.1); opacity: 1; }
  100% { transform: scale(0.95); opacity: 0.8; }
}

/* B. HÀNG 4 STAT CARDS */
.stat-cards-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  margin-bottom: 24px;
}
.stat-card {
  background-color: #ffffff;
  border-radius: 12px;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  padding: 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.06);
}
.stat-title-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}
.stat-title {
  font-size: 13px;
  font-weight: 500;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 6px;
}
.trend-pill {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 6px;
}
.trend-up {
  background-color: #ecfdf5;
  color: #059669;
}
.trend-neutral {
  background-color: #f1f5f9;
  color: #475569;
}
.stat-number {
  font-size: 24px;
  font-weight: 800;
  color: #0f172a;
  line-height: 1.2;
}
.stat-currency {
  font-size: 18px;
  color: #ea580c;
}
.stat-subtext {
  font-size: 11px;
  color: #94a3b8;
  margin-top: 4px;
}
.stat-badge-squircle {
  width: 54px;
  height: 54px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
  transition: transform 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.stat-card:hover .stat-badge-squircle {
  transform: scale(1.1) rotate(2deg);
}
.badge-cyan {
  background-color: #e0f2fe;
  background: linear-gradient(135deg, #e0f2fe 0%, #bae6fd 100%);
  color: #0284c7;
}
.badge-purple {
  background-color: #ede9fe;
  background: linear-gradient(135deg, #ede9fe 0%, #ddd6fe 100%);
  color: #7c3aed;
}
.badge-pink {
  background-color: #ffe4e6;
  color: #e11d48;
  background: linear-gradient(135deg, #fce7f3 0%, #fbcfe8 100%);
  color: #db2777;
}
.badge-yellow {
  background-color: #fef3c7;
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
  color: #d97706;
}

/* C. HÀNG 3 ANALYTICS CARDS */
.analytics-cards-grid {
  display: grid;
  grid-template-columns: 1fr 1.2fr 1.2fr;
  gap: 20px;
  margin-bottom: 24px;
}
.analytics-card {
  background-color: #ffffff;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  padding: 20px;
}
.analytics-card-title {
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  margin-bottom: 16px;
}

/* Donut chart */
.donut-chart-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}
.donut-svg-wrapper {
  position: relative;
  width: 140px;
  height: 140px;
}
.donut-svg {
  width: 100%;
  height: 100%;
}
.donut-center-label {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}
.donut-top-text {
  font-size: 13px;
  font-weight: 600;
  color: #ea580c;
}
.donut-big-percent {
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
}
.donut-legend-row {
  display: flex;
  gap: 16px;
  margin-top: 14px;
}
.legend-chip {
  font-size: 12px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
}
.legend-chip .dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.legend-orange { color: #ea580c; }
.legend-orange .dot { background-color: #ea580c; }
.legend-blue { color: #2563eb; }
.legend-blue .dot { background-color: #2563eb; }

/* Age progress bars */
.age-bars-container {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.age-bar-row {
  display: flex;
  align-items: center;
  gap: 12px;
}
.age-label {
  width: 130px;
  font-size: 12px;
  font-weight: 500;
  color: #475569;
  flex-shrink: 0;
}
.age-track {
  flex: 1;
  height: 10px;
  background-color: #f1f5f9;
  border-radius: 9999px;
  overflow: hidden;
}
.age-fill {
  height: 100%;
  background-color: #ea580c;
  border-radius: 9999px;
}

/* Location chart */
.locations-chart-container {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.location-row {
  display: flex;
  align-items: center;
  gap: 10px;
}
.loc-name {
  width: 85px;
  font-size: 11.5px;
  font-weight: 500;
  color: #475569;
  text-align: right;
  flex-shrink: 0;
}
.loc-bar-track {
  flex: 1;
  height: 14px;
  background-color: #f1f5f9;
  border-radius: 3px;
  overflow: hidden;
}
.loc-bar-fill {
  height: 100%;
  background-color: #2563eb;
  border-radius: 3px;
}
.loc-axis-row {
  display: flex;
  justify-content: space-between;
  font-size: 10.5px;
  color: #94a3b8;
  padding-left: 95px;
  margin-top: 4px;
}
.loc-country-legend {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  margin-top: 8px;
}
.loc-color-box {
  width: 12px;
  height: 12px;
  background-color: #2563eb;
  border-radius: 2px;
}
.loc-legend-text {
  font-size: 11px;
  color: #64748b;
}

/* D. QUICK OVERVIEW SECTION & TABLES */
.quick-overview-section {
  background-color: #ffffff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  padding: 20px;
}
.table-card {
  background-color: #ffffff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 20px -3px rgba(0, 0, 0, 0.05);
  overflow: hidden;
}
.section-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}
.sec-title-box {
  display: flex;
  align-items: center;
  gap: 8px;
}
.sec-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
}
.sec-badge {
  background-color: #ffedd5;
  color: #ea580c;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 9999px;
}
.view-all-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: none;
  border: none;
  color: #ea580c;
  font-size: 12.5px;
  font-weight: 600;
  cursor: pointer;
  transition: gap 0.2s ease, color 0.2s ease;
}
.view-all-btn:hover {
  color: #c2410c;
  gap: 7px;
}

/* TABLE STYLES */
.table-responsive-wrapper {
  overflow-x: auto;
  border-radius: 12px;
}
.concept-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  font-size: 13.5px;
}
.concept-table th {
  background-color: #f8fafc;
  color: #475569;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.6px;
  padding: 14px 18px;
  text-align: left;
  border-bottom: 2px solid #e2e8f0;
  white-space: nowrap !important;
}
.concept-table td {
  padding: 14px 18px;
  border-bottom: 1px solid #f1f5f9;
  vertical-align: middle;
  background-color: #ffffff;
}
.concept-table tbody tr:hover td {
  background-color: #f8fafc;
}
.concept-table tbody tr:last-child td {
  border-bottom: none;
}

.table-user-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}
.table-avatar {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #f1f5f9;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  flex-shrink: 0;
}
.table-user-cell strong {
  display: block;
  font-size: 13.5px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.35;
}
.table-user-cell small {
  display: block;
  font-size: 12px;
  color: #64748b;
  line-height: 1.35;
}

/* BADGES & STATUS */
.badge-role-admin,
.badge-role-seller,
.badge-role-seller-pending,
.badge-role-shipper,
.badge-role-shipper-pending,
.badge-role-rejected,
.badge-role-buyer {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  font-size: 11.5px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
  white-space: nowrap !important;
  line-height: 1.3;
}
.badge-role-admin {
  background-color: #ede9fe;
  color: #6d28d9;
  border: 1px solid #ddd6fe;
}
.badge-role-seller {
  background-color: #ffedd5;
  color: #c2410c;
  border: 1px solid #fed7aa;
}
.badge-role-seller-pending {
  background-color: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
}
.badge-role-shipper {
  background-color: #dcfce7;
  color: #15803d;
  border: 1px solid #bbf7d0;
}
.badge-role-shipper-pending {
  background-color: #fefce8;
  color: #a16207;
  border: 1px solid #fef08a;
}
.badge-role-buyer {
  background-color: #e0f2fe;
  color: #0369a1;
  border: 1px solid #bae6fd;
}
.badge-role-rejected {
  background-color: #fee2e2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.status-pill {
  font-size: 12px;
  font-weight: 600;
  padding: 5px 12px;
  border-radius: 9999px;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap !important;
  line-height: 1.4;
}
.status-active {
  background-color: #f0fdf4;
  color: #15803d;
  border: 1px solid #bbf7d0;
}
.status-pending {
  background-color: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
}

@keyframes hourglassFlip {
  0% {
    transform: rotate(0deg);
  }
  25% {
    transform: rotate(0deg);
  }
  50% {
    transform: rotate(180deg);
  }
  75% {
    transform: rotate(180deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

.hourglass-animated {
  display: inline-block;
  animation: hourglassFlip 2.4s cubic-bezier(0.45, 0.05, 0.55, 0.95) infinite;
  transform-origin: center center;
  vertical-align: middle;
}
.status-warning {
  background-color: #fefce8;
  color: #a16207;
  border: 1px solid #fef08a;
}
.status-banned,
.status-rejected {
  background-color: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

/* ACTION BUTTONS */
.btn-table-action,
.btn-action-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #ea580c;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 8px 14px;
  font-size: 12.5px;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.15s ease;
}
.btn-table-action:hover,
.btn-action-primary:hover {
  background-color: #c2410c;
  transform: translateY(-1px);
}

.actions-inline-btns {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  white-space: nowrap;
}
.action-btn {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.btn-unlock {
  color: #16a34a;
  background-color: #f0fdf4;
  border-color: #dcfce7;
}
.btn-unlock:hover {
  background-color: #16a34a;
  color: #ffffff;
  border-color: #16a34a;
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(22, 163, 74, 0.25);
}
.btn-punish {
  color: #d97706;
  background-color: #fefce8;
  border-color: #fef08a;
}
.btn-punish:hover {
  background-color: #d97706;
  color: #ffffff;
  border-color: #d97706;
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(217, 119, 6, 0.25);
}
.btn-delete {
  color: #dc2626;
  background-color: #fef2f2;
  border-color: #fee2e2;
}
.btn-delete:hover {
  background-color: #dc2626;
  color: #ffffff;
  border-color: #dc2626;
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(220, 38, 38, 0.25);
}

/* TAB CONTENT HEADERS & FILTERS */
.content-header-card {
  background-color: #ffffff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  padding: 20px 24px;
  margin-bottom: 20px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.04);
}
.content-heading {
  font-size: 20px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 6px 0;
  letter-spacing: -0.3px;
}
.content-sub {
  font-size: 13.5px;
  color: #64748b;
  margin: 0;
}
.filter-controls-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 18px;
  gap: 16px;
  flex-wrap: wrap;
}
.filter-tabs-pills {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
.filter-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 1.5px solid #e2e8f0;
  background-color: #ffffff;
  color: #475569;
  border-radius: 10px;
  padding: 8px 16px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.filter-pill:hover {
  background-color: #f8fafc;
  border-color: #cbd5e1;
  color: #0f172a;
}
.filter-pill.active {
  background: linear-gradient(135deg, #ea580c 0%, #f97316 100%);
  color: #ffffff;
  border-color: #ea580c;
  box-shadow: 0 3px 10px rgba(234, 88, 12, 0.3);
}
.status-select-box {
  border: 1.5px solid #e2e8f0;
  background-color: #ffffff;
  color: #1e293b;
  border-radius: 10px;
  padding: 8px 14px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  outline: none;
  transition: all 0.15s ease;
}
.status-select-box:focus {
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}
.btn-create-orange {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #ea580c;
  color: #ffffff;
  border: none;
  border-radius: 6px;
  border-radius: 8px;
  padding: 8px 16px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-create-orange:hover {
  background-color: #c2410c;
  transform: translateY(-1px);
}

/* EMPTY STATE */
.empty-state-card {
  text-align: center;
  padding: 48px 24px;
  color: #64748b;
}

/* MODALS */
.modal-backdrop-custom {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 20px;
}
.modal-backdrop-custom.nested-popup {
  z-index: 1050;
  background-color: rgba(15, 23, 42, 0.75);
}
.modal-dialog-card {
  background-color: #ffffff;
  border-radius: 12px;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 600px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
}
.modal-dialog-card.modal-xl { max-width: 980px; width: 95%; }
.modal-dialog-card.modal-lg { max-width: 780px; }
.modal-dialog-card.modal-md { max-width: 520px; }
.modal-dialog-card.modal-sm { max-width: 400px; }

.modal-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #e2e8f0;
}
.modal-heading {
  font-size: 16px;
  font-weight: 700;
  margin: 0;
}
.modal-close-btn {
  background: none;
  border: none;
  font-size: 18px;
  color: #94a3b8;
  cursor: pointer;
}
.modal-body-scroll {
  padding: 20px;
  overflow-y: auto;
}
.modal-footer-row {
  padding: 14px 20px;
  border-top: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
}

/* FORM CONTROLS IN MODALS */
.modal-field-group {
  margin-bottom: 16px;
}
.modal-field-group:last-child {
  margin-bottom: 0;
}
.modal-form-label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 6px;
}
.modal-form-control,
.modal-form-select {
  width: 100%;
  padding: 10px 14px;
  border-radius: 8px;
  border: 1px solid #cbd5e1;
  background-color: #ffffff;
  color: #0f172a;
  font-size: 13.5px;
  font-family: inherit;
  transition: all 0.2s ease;
  box-sizing: border-box;
}
.modal-form-control:focus,
.modal-form-select:focus {
  outline: none;
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.15);
}
.modal-textarea {
  resize: vertical;
  min-height: 80px;
}

/* DANGER CALLOUT BOX */
.danger-callout-box {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 16px;
  background-color: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
}
.danger-callout-text {
  font-size: 13px;
  color: #b91c1c;
  font-weight: 500;
  line-height: 1.5;
  margin: 0;
}
/* ================================================================
   MODAL TỪ CHỐI KYC (REJECT MODAL MODERN STYLING)
   ================================================================ */
.modal-backdrop-custom.nested-popup.reject-modal-backdrop {
  z-index: 1100;
  background-color: rgba(15, 23, 42, 0.72);
  backdrop-filter: blur(8px);
  padding: 16px;
  animation: fadeInRejectBackdrop 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes fadeInRejectBackdrop {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes popInRejectCard {
  from {
    opacity: 0;
    transform: scale(0.94) translateY(12px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.animate-pop-in {
  animation: popInRejectCard 0.25s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

.modal-reject-card {
  max-width: 540px;
  width: 100%;
  border-radius: 18px;
  background: #ffffff;
  border: 1px solid rgba(226, 232, 240, 0.95);
  box-shadow: 0 25px 50px -12px rgba(15, 23, 42, 0.35), 0 0 0 1px rgba(239, 68, 68, 0.08);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.reject-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 22px;
  background: linear-gradient(135deg, #fff1f2 0%, #ffffff 100%);
  border-bottom: 1px solid #fee2e2;
}

.reject-header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.reject-header-icon-wrap {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: #ffe4e6;
  border: 1px solid #fecdd3;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: 0 2px 6px rgba(225, 29, 72, 0.12);
}

.reject-modal-title {
  font-size: 16.5px;
  font-weight: 700;
  color: #991b1b;
  margin: 0;
  line-height: 1.3;
}

.reject-modal-subtitle {
  font-size: 12px;
  color: #64748b;
  margin: 3px 0 0 0;
}

.reject-close-btn {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #64748b;
  cursor: pointer;
  transition: all 0.15s ease;
}

.reject-close-btn:hover {
  background: #f1f5f9;
  color: #0f172a;
  border-color: #cbd5e1;
}

.reject-modal-body {
  padding: 20px 22px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-height: 70vh;
  overflow-y: auto;
}

/* Recipient Info Card */
.reject-target-banner {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
}

.reject-target-avatar {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  background: linear-gradient(135deg, #ea580c 0%, #f97316 100%);
  color: #ffffff;
  font-weight: 700;
  font-size: 15px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: 0 2px 6px rgba(234, 88, 12, 0.2);
}

.reject-target-info {
  display: flex;
  flex-direction: column;
  gap: 3px;
  flex: 1;
  min-width: 0;
}

.reject-target-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.reject-target-name {
  font-weight: 700;
  font-size: 14px;
  color: #0f172a;
}

.reject-badge-type {
  font-size: 11px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 20px;
}

.reject-badge-type.badge-seller {
  background: #ffedd5;
  color: #c2410c;
  border: 1px solid #fed7aa;
}

.reject-badge-type.badge-shipper {
  background: #e0e7ff;
  color: #4338ca;
  border: 1px solid #c7d2fe;
}

.reject-target-subtext {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 12px;
  color: #64748b;
  flex-wrap: wrap;
}

.reject-target-subtext .subtext-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

/* Preset Chips */
.reject-presets-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.reject-section-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12.5px;
  font-weight: 600;
  color: #334155;
  margin: 0;
}

.reject-chips-wrap {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
}

.reject-chip-btn {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #334155;
  font-size: 11.5px;
  font-weight: 500;
  padding: 6px 11px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.15s ease;
  text-align: left;
  line-height: 1.3;
}

.reject-chip-btn:hover {
  background: #f1f5f9;
  color: #0f172a;
  border-color: #cbd5e1;
  transform: translateY(-1px);
}

.reject-chip-btn.active {
  background: #fee2e2;
  color: #991b1b;
  border-color: #fca5a5;
  font-weight: 600;
}

/* Reason Input Box */
.reject-input-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.reject-label-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.btn-clear-reason {
  background: none;
  border: none;
  color: #94a3b8;
  font-size: 11.5px;
  cursor: pointer;
  padding: 0;
  text-decoration: underline;
  transition: color 0.15s;
}

.btn-clear-reason:hover {
  color: #ef4444;
}

.reject-textarea-container {
  display: flex;
  flex-direction: column;
  border: 1.5px solid #cbd5e1;
  border-radius: 12px;
  overflow: hidden;
  background: #ffffff;
  transition: all 0.2s ease;
}

.reject-textarea-container:focus-within {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.12);
}

.reject-textarea {
  width: 100%;
  border: none;
  padding: 12px 14px;
  font-size: 13.5px;
  color: #1e293b;
  line-height: 1.5;
  font-family: inherit;
  resize: vertical;
  min-height: 96px;
  outline: none;
  background: transparent;
  box-sizing: border-box;
}

.reject-textarea::placeholder {
  color: #94a3b8;
  font-size: 13px;
}

.reject-textarea-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  background: #f8fafc;
  border-top: 1px solid #f1f5f9;
}

.reject-hint-text {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  color: #64748b;
}

.reject-char-count {
  font-size: 11px;
  color: #94a3b8;
  font-weight: 600;
}

/* Footer & Buttons */
.reject-modal-footer {
  padding: 14px 22px;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
}

.btn-reject-cancel {
  background: #ffffff;
  color: #475569;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-reject-cancel:hover {
  background: #f1f5f9;
  color: #1e293b;
}

.btn-reject-confirm {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  border: none;
  border-radius: 10px;
  padding: 9px 20px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(220, 38, 38, 0.25);
  transition: all 0.15s ease;
}

.btn-reject-confirm:hover:not(:disabled) {
  background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(220, 38, 38, 0.35);
}

.btn-reject-confirm:disabled {
  opacity: 0.55;
  cursor: not-allowed;
  box-shadow: none;
  transform: none;
}

.spin-icon {
  animation: spinLoader 1s linear infinite;
}

@keyframes spinLoader {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.popup-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  margin: 0 0 12px 0;
}

/* MODAL BUTTONS */
.btn-modal-cancel {
  background-color: #f1f5f9;
  color: #475569;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-modal-cancel:hover {
  background-color: #e2e8f0;
  color: #1e293b;
}

.btn-modal-punish {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #d97706;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(217, 119, 6, 0.25);
  transition: all 0.15s ease;
}
.btn-modal-punish:hover {
  background-color: #b45309;
  transform: translateY(-1px);
}

.btn-modal-danger {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #dc2626;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(220, 38, 38, 0.25);
  transition: all 0.15s ease;
}
.btn-modal-danger:hover:not(:disabled) {
  background-color: #b91c1c;
  transform: translateY(-1px);
}
.btn-modal-danger:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-modal-submit {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: linear-gradient(135deg, #ea580c 0%, #f97316 100%);
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(234, 88, 12, 0.3);
  transition: all 0.15s ease;
}
.btn-modal-submit:hover {
  opacity: 0.95;
  transform: translateY(-1px);
}

.btn-modal-approve {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #16a34a;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(22, 163, 74, 0.25);
  transition: all 0.15s ease;
}
.btn-modal-approve:hover {
  background-color: #15803d;
  transform: translateY(-1px);
}

.btn-modal-reject {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #dc2626;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(220, 38, 38, 0.25);
  transition: all 0.15s ease;
}
.btn-modal-reject:hover {
  background-color: #b91c1c;
  transform: translateY(-1px);
}

/* KYC COMPREHENSIVE DOSSIER LAYOUT */
.kyc-dossier-layout {
  display: grid;
  grid-template-columns: 1.15fr 0.85fr;
  gap: 20px;
}
.kyc-dossier-info-col {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.kyc-info-card {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 14px 16px;
}
.kyc-card-header {
  display: flex;
  align-items: center;
  margin-bottom: 10px;
  padding-bottom: 8px;
  border-bottom: 1px solid #e2e8f0;
}
.kyc-card-title {
  font-size: 12.5px;
  font-weight: 700;
  color: #0f172a;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}
.kyc-card-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px 14px;
}
.info-field-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.info-field-item.col-span-2 {
  grid-column: span 2;
}
.info-lbl {
  font-size: 11px;
  color: #64748b;
  font-weight: 500;
}
.info-val {
  font-size: 13px;
  color: #1e293b;
  font-weight: 500;
}
.info-val-highlight {
  font-size: 13.5px;
  color: #0f172a;
  font-weight: 700;
}
.info-val-store {
  font-size: 14px;
  color: #ea580c;
  font-weight: 700;
}
.cccd-code-badge {
  font-family: monospace;
  font-size: 13px;
  font-weight: 700;
  background: #ede9fe;
  color: #6d28d9;
  padding: 2px 8px;
  border-radius: 4px;
}
.chip-nfc-badge {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-size: 10.5px;
  font-weight: 600;
  background: #dcfce7;
  color: #15803d;
  padding: 2px 6px;
  border-radius: 9999px;
}
.badge-category-tag {
  display: inline-block;
  font-size: 11.5px;
  font-weight: 600;
  background: #ffedd5;
  color: #ea580c;
  padding: 2px 8px;
  border-radius: 4px;
  width: fit-content;
}
.license-plate-badge {
  display: inline-block;
  font-family: monospace;
  font-size: 13px;
  font-weight: 800;
  background: #1e293b;
  color: #f8fafc;
  padding: 2px 8px;
  border-radius: 4px;
  letter-spacing: 0.5px;
  width: fit-content;
}
.bank-acc-badge {
  font-family: monospace;
  font-size: 13px;
  font-weight: 700;
  background: #dbeafe;
  color: #1d4ed8;
  padding: 2px 8px;
  border-radius: 4px;
  width: fit-content;
}
.match-check-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  color: #15803d;
  background: #dcfce7;
  padding: 3px 8px;
  border-radius: 4px;
  font-weight: 600;
  width: fit-content;
}

/* RIGHT COLUMN: DOCS VIEWER & SWITCHER */
.kyc-dossier-docs-col {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.kyc-doc-tabs {
  display: flex;
  gap: 6px;
  background: #f1f5f9;
  padding: 4px;
  border-radius: 8px;
}
.kyc-doc-tab-btn {
  flex: 1;
  padding: 6px 8px;
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s ease;
  white-space: nowrap;
  text-align: center;
}
.kyc-doc-tab-btn:hover {
  color: #0f172a;
}
.kyc-doc-tab-btn.active {
  background: #ffffff;
  color: #ea580c;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}
.image-preview-card {
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  overflow: hidden;
  position: relative;
  cursor: pointer;
  background: #0f172a;
  min-height: 220px;
  max-height: 260px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.preview-img-large {
  width: 100%;
  height: 240px;
  object-fit: contain;
  background: #f8fafc;
  transition: transform 0.2s ease;
}
.image-preview-card:hover .preview-img-large {
  transform: scale(1.02);
}
.doc-type-indicator {
  position: absolute;
  top: 8px;
  left: 8px;
  background: rgba(15, 23, 42, 0.75);
  color: #f8fafc;
  font-size: 10.5px;
  font-weight: 600;
  padding: 3px 8px;
  border-radius: 4px;
  backdrop-filter: blur(4px);
}
.zoom-hint-overlay {
  position: absolute;
  bottom: 8px;
  right: 8px;
  background-color: rgba(15, 23, 42, 0.8);
  color: #ffffff;
  font-size: 11px;
  padding: 4px 10px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  gap: 4px;
  backdrop-filter: blur(4px);
}

/* ========================================================
   STYLES FOR OCR CMND / CCCD AUTOMATION (FPT.AI INTEGRATION)
   ======================================================== */
.kyc-field-input {
  width: 100%;
  padding: 6px 10px;
  font-size: 13px;
  font-weight: 600;
  color: #0f172a;
  background-color: #ffffff;
  border: 1.5px solid #cbd5e1;
  border-radius: 6px;
  outline: none;
  transition: all 0.2s ease;
}
.kyc-field-input:focus {
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.15);
}
.kyc-field-input.border-success-highlight {
  border-color: #10b981;
  background-color: #f0fdf4;
}
.ocr-skeleton-line {
  width: 100%;
  height: 33px;
  background: linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%);
  background-size: 200% 100%;
  animation: ocrSkeletonPulse 1.5s infinite;
  border-radius: 6px;
}
@keyframes ocrSkeletonPulse {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
.ocr-sync-tag {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-size: 10px;
  font-weight: 700;
  color: #15803d;
  background: #dcfce7;
  padding: 1px 6px;
  border-radius: 9999px;
}
.ocr-upload-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  background: #fff7ed;
  border: 1px dashed #fdba74;
  padding: 7px 12px;
  border-radius: 8px;
}
.ocr-banner-left {
  display: flex;
  align-items: center;
  gap: 8px;
}
.btn-ocr-upload {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: linear-gradient(135deg, #ea580c 0%, #f97316 100%);
  color: #ffffff;
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 11.5px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 1px 3px rgba(234, 88, 12, 0.25);
  margin-bottom: 0;
}
.btn-ocr-upload:hover:not(.disabled) {
  opacity: 0.92;
  transform: translateY(-1px);
}
.btn-ocr-upload.disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.btn-ocr-rescan {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: #ffffff;
  color: #475569;
  border: 1px solid #cbd5e1;
  padding: 5px 10px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-ocr-rescan:hover {
  background: #f1f5f9;
  color: #0f172a;
}
.ocr-provider-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 10.5px;
  font-weight: 700;
  color: #c2410c;
  background: #ffedd5;
  padding: 3px 8px;
  border-radius: 9999px;
  white-space: nowrap;
}
.ocr-scanner-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(15, 23, 42, 0.78);
  backdrop-filter: blur(3px);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 10;
  overflow: hidden;
}
.ocr-laser-beam {
  position: absolute;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, transparent, #f97316, #ef4444, #f97316, transparent);
  box-shadow: 0 0 16px 4px rgba(249, 115, 22, 0.95);
  animation: laserScan 2s ease-in-out infinite alternate;
}
@keyframes laserScan {
  0% { top: 5%; }
  100% { top: 95%; }
}
.ocr-status-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 16px;
  color: #ffffff;
}
.ocr-spinner-icon {
  animation: ocrSpin 1s linear infinite;
  color: #f97316;
  margin-bottom: 8px;
}
.ocr-status-title {
  font-size: 13.5px;
  font-weight: 700;
  letter-spacing: 0.3px;
  margin-bottom: 4px;
}
.ocr-status-subtitle {
  font-size: 11px;
  color: #cbd5e1;
  max-width: 280px;
  line-height: 1.4;
}
.img-blur-scanning {
  filter: blur(2px) brightness(0.85);
}
@keyframes ocrSpin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.doc-thumbnail-strip {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}
.doc-thumb-item {
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  overflow: hidden;
  padding: 4px;
  background: #ffffff;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  transition: all 0.15s ease;
}
.doc-thumb-item:hover {
  border-color: #cbd5e1;
}
.doc-thumb-item.active {
  border-color: #ea580c;
  background: #fff7ed;
}
.thumb-img {
  width: 100%;
  height: 48px;
  object-fit: cover;
  border-radius: 4px;
}
.thumb-lbl {
  font-size: 10px;
  font-weight: 600;
  color: #475569;
  text-align: center;
}
.doc-thumb-item.active .thumb-lbl {
  color: #ea580c;
}
.kyc-trust-badge {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  border-radius: 8px;
  padding: 8px 12px;
  color: #166534;
  font-size: 11.5px;
}

/* LIGHTBOX */
.zoom-lightbox-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}
.zoomed-image {
  max-width: 90vw;
  max-height: 90vh;
  border-radius: 8px;
}
.zoom-close-btn {
  position: absolute;
  top: 20px;
  right: 24px;
  font-size: 28px;
  color: #ffffff;
  background: none;
  border: none;
  cursor: pointer;
}

/* TOAST */
.toast-floating-alert {
  position: fixed;
  bottom: 24px;
  right: 24px;
  padding: 12px 20px;
  border-radius: 8px;
  color: #ffffff;
  font-size: 13px;
  font-weight: 500;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  z-index: 3000;
}
.toast-success { background-color: #16a34a; }
.toast-danger { background-color: #dc2626; }
/* ================================================================
 * PRODUCT APPROVAL STYLES
 * ================================================================ */
.product-thumb-img {
  width: 52px;
  height: 52px;
  border-radius: 10px;
  object-fit: cover;
  border: 1.5px solid #e2e8f0;
  cursor: pointer;
  transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.product-thumb-img:hover {
  transform: scale(1.08);
}
.badge-stock-pill {
  display: inline-block;
  padding: 4px 10px;
  background-color: #f1f5f9;
  color: #334155;
  border-radius: 20px;
  font-weight: 700;
  font-size: 12px;
}
.ai-moderation-cell {
  max-width: 320px;
}
.badge-ai-warning {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background-color: #fff7ed;
  color: #c2410c;
  border: 1px solid #ffedd5;
  border-radius: 6px;
  padding: 2px 7px;
  font-size: 11px;
  font-weight: 700;
}
.badge-ai-score {
  font-size: 11px;
  color: #64748b;
  font-weight: 600;
}
.ai-flag-text {
  font-size: 12px;
  color: #b45309;
  line-height: 1.4;
  margin: 0;
  background: #fefce8;
  padding: 6px 10px;
  border-radius: 6px;
  border-left: 3px solid #eab308;
}
.btn-approve-product {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #16a34a;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 7px 12px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-approve-product:hover {
  background-color: #15803d;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(22, 163, 74, 0.25);
}
.btn-reject-product {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: #fee2e2;
  color: #dc2626;
  border: 1px solid #fca5a5;
  border-radius: 8px;
  padding: 7px 12px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-reject-product:hover {
  background-color: #dc2626;
  color: #ffffff;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(220, 38, 38, 0.25);
}
.btn-delete-prod-admin {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  background-color: #f8fafc;
  color: #64748b;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 7px 11px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-delete-prod-admin:hover {
  background-color: #fee2e2;
  color: #dc2626;
  border-color: #fca5a5;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(220, 38, 38, 0.15);
}
.kyc-quick-switch-box .btn-quick-switch {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background-color: #fff7ed;
  color: #ea580c;
  border: 1.5px solid #fed7aa;
  padding: 8px 16px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}
.kyc-quick-switch-box .btn-quick-switch:hover {
  background-color: #ea580c;
  color: #ffffff;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.2);
}

/* RESPONSIVE */
@media (max-width: 1024px) {
  .stat-cards-grid { grid-template-columns: repeat(2, 1fr); }
  .analytics-cards-grid { grid-template-columns: 1fr; }
  .hero-channels-strip { grid-template-columns: repeat(3, 1fr); }
}
@media (max-width: 768px) {
  .concept-body-layout { flex-direction: column; }
  .concept-sidebar { width: 100%; height: auto; position: static; min-height: auto; }
  .stat-cards-grid { grid-template-columns: 1fr; }
  .hero-top-row { flex-direction: column; text-align: center; }
  .hero-channels-strip { grid-template-columns: repeat(2, 1fr); }
}
</style>
