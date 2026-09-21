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
import { ref, reactive, computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { orderRealtimeService } from "../../services/orderRealtimeService";
import {
  useProductModeration,
  type ModeratedProduct,
  type AIScanResult
} from "../../composables/useProductModeration";

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
  name: "Chủ Gian Hàng",
  email: "",
  avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80"
});

onMounted(async () => {
  const savedUser = localStorage.getItem("currentUser") || localStorage.getItem("zonemart_user");
  if (!savedUser) {
    router.replace({
      path: "/login",
      query: { redirect: "/seller", reason: "auth_required", required: "seller" }
    });
    return;
  }
  try {
    const parsed = JSON.parse(savedUser);
    if (!parsed || (parsed.role !== 'seller' && parsed.role !== 'admin')) {
      router.replace('/403');
      return;
    }
    if (parsed.fullName) currentUser.name = parsed.fullName;
    if (parsed.phoneEmail) currentUser.email = parsed.phoneEmail;
    if (parsed.avatarUrl) currentUser.avatar = parsed.avatarUrl;

    // Khởi tạo hồ sơ gian hàng, danh mục sản phẩm, đơn hàng & KPIs theo đúng tài khoản người bán
    await loadSellerStoreProfile(parsed);
    loadSellerOrdersAndKPIs();
    loadSellerFeedbacks();

    // Kiểm tra tính hợp lệ của tài khoản với backend (nếu tài khoản đã bị Admin xóa khỏi hệ thống)
    const emailToCheck = parsed.phoneEmail || parsed.email;
    if (emailToCheck && parsed.role !== 'admin' && !parsed.isAdmin) {
      fetch(`/api/auth/verify-session?email=${encodeURIComponent(emailToCheck)}`)
        .then((res) => {
          if (res.status === 404 || res.status === 401) {
            localStorage.removeItem("isLoggedIn");
            localStorage.removeItem("userRole");
            localStorage.removeItem("currentUser");
            localStorage.removeItem("zonemart_user");
            alert("Tài khoản của bạn đã bị xóa khỏi hệ thống! Vui lòng liên hệ Ban Quản Trị.");
            router.replace("/login");
          }
        })
        .catch(() => {});
    }
  } catch { }

  // Lắng nghe thông báo tài xế đã giao đơn hàng thành công theo thời gian thực
  unsubscribeSellerOrder = orderRealtimeService.onOrderStatusChanged(({ orderId, status, order }) => {
    if (status === "completed") {
      deliveredNotification.value = {
        show: true,
        orderCode: order.orderCode || order.id || orderId,
        items: order.items || [],
        customerName: order.customer?.name || "Khách hàng ZoneMart",
        customerAddress: order.customer?.address || "Khu vực Cầu Giấy, Hà Nội",
        total: order.total || 0
      };

      // Tự động cập nhật trạng thái đơn trong danh sách quản lý đơn của tiệm
      const foundOrd = orders.value.find(
        (o) => o.id === orderId || o.orderCode === orderId || o.orderCode === '#' + orderId
      );
      if (foundOrd) {
        foundOrd.status = "Hoàn thành";
      } else {
        orders.value.unshift({
          id: orderId,
          orderCode: order.orderCode || `#${orderId.slice(-6).toUpperCase()}`,
          productSummary: (order.items || []).map((it: any) => `${it.name} (x${it.quantity || it.qty || 1})`).join(", ") || "Đơn hàng mới",
          customerName: order.customer?.name || "Khách hàng ZoneMart",
          customerPhone: order.customer?.phone || "0912 345 678",
          customerAddress: order.customer?.address || "Hà Nội",
          orderDate: "Vừa xong",
          priceFormatted: `${(order.total || 0).toLocaleString("vi-VN")} đ`,
          paymentMethod: (order.paymentMethod as any) || "Chuyển khoản QR",
          status: "Hoàn thành",
          items: (order.items || []).map((it: any) => ({
            name: it.name,
            qty: it.quantity || it.qty || 1,
            price: it.price || 0
          }))
        });
      }

      const cleanEmail = (currentUser.email || "").toLowerCase().trim();
      if (cleanEmail) {
        localStorage.setItem(`zonemart_seller_orders_${cleanEmail}`, JSON.stringify(orders.value));
      }
      recalculateKPIs();

      triggerToast(`🎉 Tài xế đã giao đơn hàng #${order.orderCode || order.id} thành công!`);
    }
  });
});

onUnmounted(() => {
  if (unsubscribeSellerOrder) unsubscribeSellerOrder();
});

// Thông báo giao đơn hàng thành công nổi bật dành cho người bán
const deliveredNotification = ref<{
  show: boolean;
  orderCode: string;
  items: { id?: string; productId?: string; name: string; quantity: number; image?: string; price: number }[];
  customerName: string;
  customerAddress: string;
  total: number;
} | null>(null);

let unsubscribeSellerOrder: (() => void) | null = null;

// Tên hiển thị chào mừng lịch sự
const greetingName = computed(() => {
  if (!currentUser.name) return "Chủ gian hàng";
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
  id: "ZM-S001",
  name: "Gian Hàng ZoneMart",
  category: "Thực phẩm & Nhu yếu phẩm",
  address: "Số 48 đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội",
  phone: "0988 123 456",
  openHours: "07:00 - 22:00",
  bankName: "Vietcombank (VCB)",
  bankAccount: "1029384756",
  accountHolder: "CHỦ GIAN HÀNG",
  radiusKm: 10,
  avgOrderValue: "0 đ",
  totalOrders: "0",
  lifetimeValue: "0 đ"
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

const orders = ref<SellerOrder[]>([]);

// Danh sách sản phẩm bán chạy (Top Selling Products)
interface TopProduct {
  id: string;
  name: string;
  salesText: string;
  statusText: string;
  stockText: string;
  image: string;
}

// Khách hàng thân thiết
interface SellerCustomer {
  id: string;
  name: string;
  avatarBadge: string;
  phone: string;
  address: string;
  tag: string;
}

// Đánh giá & phản hồi
interface SellerFeedbackItem {
  id: string;
  author: string;
  comment: string;
  rating: number;
}

const sellerFeedbacks = ref<SellerFeedbackItem[]>([]);

const feedbackRatingText = computed(() => {
  if (sellerFeedbacks.value.length === 0) return "Chưa có đánh giá";
  const avg = sellerFeedbacks.value.reduce((a, b) => a + (b.rating || 5), 0) / sellerFeedbacks.value.length;
  return `${avg.toFixed(1)} / 5 ⭐`;
});

const storeDistrictName = computed(() => {
  if (!storeInfo.address) return "Cầu Giấy, Hà Nội";
  const parts = storeInfo.address.split(",");
  return parts.length > 1 ? parts[parts.length - 2].trim() : parts[0].trim();
});

// ================================================================
// QUẢN LÝ SẢN PHẨM & AI KIỂM DUYỆT (LUỒNG 2 ZONEMART)
// ================================================================
const productModeration = useProductModeration();

// Danh sách sản phẩm của riêng seller hiện tại
const products = computed(() => {
  const cleanEmail = (currentUser.email || "").toLowerCase().trim();
  const currentStore = (storeInfo.name || "").toLowerCase().trim();

  return productModeration.allProducts.value.filter((p) => {
    const pEmail = (p.sellerEmail || "").toLowerCase().trim();
    const pStore = (p.storeName || "").toLowerCase().trim();

    if (cleanEmail === "seller@zonemart.vn") {
      return !pEmail || pEmail === "seller@zonemart.vn";
    }

    return (cleanEmail && pEmail === cleanEmail) || (currentStore && pStore === currentStore);
  });
});

// Danh sách sản phẩm bán chạy nhất sinh động từ danh mục sản phẩm của tiệm
const topProducts = computed<TopProduct[]>(() => {
  if (products.value.length === 0) return [];

  return products.value.slice(0, 5).map((prod) => {
    let soldQty = 0;
    orders.value.forEach((ord) => {
      ord.items?.forEach((it) => {
        const itName = (it.name || "").toLowerCase().trim();
        const pName = prod.name.toLowerCase().trim();
        if (itName.includes(pName) || pName.includes(itName)) {
          soldQty += (it.qty || 1);
        }
      });
    });

    const isAvailable = prod.isAvailable && prod.stock > 0 && prod.status === "active";
    const statusText = isAvailable ? "Còn hàng" : (prod.stock <= 0 ? "Hết hàng" : "Chờ duyệt");
    const stockText = prod.stock > 0 ? `Còn ${prod.stock.toLocaleString('vi-VN')} tồn kho` : "Hết tồn kho";

    return {
      id: prod.id,
      name: prod.name,
      salesText: `${soldQty.toLocaleString('vi-VN')} Đã bán`,
      statusText,
      stockText,
      image: prod.image || "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=150&q=80"
    };
  });
});

// Khách hàng thân thiết sinh động từ các đơn hàng thực tế
const customers = computed<SellerCustomer[]>(() => {
  const map = new Map<string, {
    id: string;
    name: string;
    avatarBadge: string;
    phone: string;
    address: string;
    count: number;
    totalAmount: number;
  }>();

  orders.value.forEach((ord) => {
    const key = ord.customerPhone?.trim() || ord.customerName?.trim() || ord.id;
    if (!key) return;

    const priceNum = parseInt((ord.priceFormatted || "").replace(/\D/g, ""), 10) || 0;
    const existing = map.get(key);
    if (existing) {
      existing.count += 1;
      existing.totalAmount += priceNum;
    } else {
      const rawName = ord.customerName?.trim() || "Khách Hàng";
      const parts = rawName.split(/\s+/);
      let badge = "KH";
      if (parts.length >= 2) {
        badge = (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
      } else if (parts.length === 1 && parts[0].length >= 2) {
        badge = parts[0].slice(0, 2).toUpperCase();
      }

      map.set(key, {
        id: `cus-${key}`,
        name: rawName,
        avatarBadge: badge,
        phone: ord.customerPhone || "Chưa có SĐT",
        address: ord.customerAddress || "Khu vực Hà Nội",
        count: 1,
        totalAmount: priceNum
      });
    }
  });

  return Array.from(map.values()).map((c) => ({
    id: c.id,
    name: c.name,
    avatarBadge: c.avatarBadge,
    phone: c.phone,
    address: c.address,
    tag: c.count >= 3 ? `Khách VIP • ${c.count} đơn hàng` : `Khách quen • ${c.count} đơn hàng`
  }));
});

// Tải thông tin hồ sơ gian hàng cho riêng seller hiện tại
const loadSellerStoreProfile = async (parsedUser: any) => {
  const cleanEmail = (parsedUser.phoneEmail || parsedUser.email || currentUser.email || "").toLowerCase().trim();
  const userName = parsedUser.fullName || parsedUser.name || currentUser.name || "Chủ Gian Hàng";
  const userStoreName = parsedUser.storeName || parsedUser.storeDetails?.storeName || "";

  // 1. Kiểm tra trong localStorage theo email của seller
  const localSaved = localStorage.getItem(`zonemart_seller_store_${cleanEmail}`);
  if (localSaved) {
    try {
      const parsedStore = JSON.parse(localSaved);
      if (parsedStore.name) storeInfo.name = parsedStore.name;
      if (parsedStore.category) storeInfo.category = parsedStore.category;
      if (parsedStore.address) storeInfo.address = parsedStore.address;
      if (parsedStore.phone) storeInfo.phone = parsedStore.phone;
      if (parsedStore.openHours) storeInfo.openHours = parsedStore.openHours;
      if (parsedStore.bankName) storeInfo.bankName = parsedStore.bankName;
      if (parsedStore.bankAccount) storeInfo.bankAccount = parsedStore.bankAccount;
      if (parsedStore.accountHolder) storeInfo.accountHolder = parsedStore.accountHolder;
      if (parsedStore.id) storeInfo.id = parsedStore.id;
    } catch {}
  } else if (cleanEmail === "seller@zonemart.vn") {
    // Tài khoản demo chính của hệ thống
    storeInfo.id = "ZM-S882";
    storeInfo.name = "Vườn Rau Ba Vì - Nông Sản Sạch VietGAP";
    storeInfo.category = "Thực phẩm & Rau củ quả";
    storeInfo.address = "Số 48 đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội";
    storeInfo.phone = "0988 123 456";
    storeInfo.openHours = "06:30 - 21:30";
    storeInfo.bankName = "Vietcombank (VCB)";
    storeInfo.bankAccount = "1029384756";
    storeInfo.accountHolder = "NGUYEN VAN BA";
  } else {
    // Khởi tạo tên gian hàng theo đúng thông tin tài khoản của seller
    const fallbackStoreName = userStoreName || `Gian Hàng ${userName}`;
    storeInfo.name = fallbackStoreName;
    storeInfo.category = "Thực phẩm & Nhu yếu phẩm";
    storeInfo.address = "Số 48 đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội";
    storeInfo.phone = parsedUser.phone || cleanEmail || "0988 123 456";
    storeInfo.openHours = "07:00 - 22:00";
    storeInfo.bankName = "Vietcombank (VCB)";
    storeInfo.bankAccount = "1029384756";
    storeInfo.accountHolder = userName.toUpperCase();
    storeInfo.id = `ZM-S${Math.abs(cleanEmail.split("").reduce((a: number, b: string) => ((a << 5) - a + b.charCodeAt(0)) | 0, 0)) % 9000 + 1000}`;

    // Lưu lại cho các lần truy cập sau
    localStorage.setItem(`zonemart_seller_store_${cleanEmail}`, JSON.stringify({
      id: storeInfo.id,
      name: storeInfo.name,
      category: storeInfo.category,
      address: storeInfo.address,
      phone: storeInfo.phone,
      openHours: storeInfo.openHours,
      bankName: storeInfo.bankName,
      bankAccount: storeInfo.bankAccount,
      accountHolder: storeInfo.accountHolder
    }));
  }

  // 2. Tra cứu thêm từ backend API nếu có để đồng bộ hồ sơ đã đăng ký trong CSDL MongoDB
  try {
    const res = await fetch(`/api/auth/seller-profile?account=${encodeURIComponent(cleanEmail)}`).catch(() => null);
    if (res && res.ok) {
      const data = await res.json();
      if (data && data.store) {
        const s = data.store;
        if (s.storeName) storeInfo.name = s.storeName;
        if (s.category) storeInfo.category = s.category;
        if (s.address) storeInfo.address = s.address;
        if (s.openHours) storeInfo.openHours = s.openHours;
        if (s.bankName) storeInfo.bankName = s.bankName;
        if (s.bankAccountNumber) storeInfo.bankAccount = s.bankAccountNumber;
        if (s.ownerFullName) storeInfo.accountHolder = s.ownerFullName;
        if (s.storeCode || s.id) storeInfo.id = s.storeCode || s.id;
        if (s.phoneEmail) storeInfo.phone = s.phoneEmail;

        localStorage.setItem(`zonemart_seller_store_${cleanEmail}`, JSON.stringify({
          id: storeInfo.id,
          name: storeInfo.name,
          category: storeInfo.category,
          address: storeInfo.address,
          phone: storeInfo.phone,
          openHours: storeInfo.openHours,
          bankName: storeInfo.bankName,
          bankAccount: storeInfo.bankAccount,
          accountHolder: storeInfo.accountHolder
        }));
      }
    }
  } catch {}
};

// Tính toán lại các KPI thực tế dựa trên danh sách đơn
const recalculateKPIs = () => {
  const totalCount = orders.value.length;
  storeInfo.totalOrders = totalCount.toLocaleString("vi-VN");

  let totalRev = 0;
  orders.value.forEach(ord => {
    const rawNum = parseInt((ord.priceFormatted || "").replace(/\D/g, ""), 10) || 0;
    totalRev += rawNum;
  });

  storeInfo.lifetimeValue = `${totalRev.toLocaleString("vi-VN")} đ`;
  const avg = totalCount > 0 ? Math.round(totalRev / totalCount) : 0;
  storeInfo.avgOrderValue = `${avg.toLocaleString("vi-VN")} đ`;
};

// Tải danh sách đơn hàng thực tế của gian hàng
const loadSellerOrdersAndKPIs = () => {
  const cleanEmail = (currentUser.email || "").toLowerCase().trim();
  const currentStore = (storeInfo.name || "").toLowerCase().trim();

  let sellerOrderList: SellerOrder[] = [];

  // 1. Kiểm tra đơn hàng được lưu riêng cho seller này
  const localOrdersRaw = localStorage.getItem(`zonemart_seller_orders_${cleanEmail}`);
  if (localOrdersRaw) {
    try {
      sellerOrderList = JSON.parse(localOrdersRaw);
    } catch {}
  }

  // 2. Quét qua toàn bộ các đơn hàng người mua trong hệ thống (zonemart_profile_orders_*)
  try {
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i);
      if (k && k.startsWith("zonemart_profile_orders_")) {
        const raw = localStorage.getItem(k);
        if (raw) {
          const buyerOrders = JSON.parse(raw);
          if (Array.isArray(buyerOrders)) {
            buyerOrders.forEach((bo: any) => {
              const boStoreName = (bo.store?.name || "").toLowerCase().trim();
              const isMatchStore = currentStore && (boStoreName === currentStore || boStoreName.includes(currentStore) || currentStore.includes(boStoreName));

              const hasMatchingItem = Array.isArray(bo.items) && bo.items.some((it: any) => {
                const itName = (it.name || "").toLowerCase().trim();
                return products.value.some((p) => {
                  const pName = p.name.toLowerCase().trim();
                  return pName === itName || itName.includes(pName) || pName.includes(itName);
                });
              });

              if (isMatchStore || hasMatchingItem) {
                const ordId = bo.orderId || bo.id || `ord-${Math.random().toString(36).substr(2, 6)}`;
                const code = bo.orderCode || (bo.id ? `#${bo.id.slice(-6).toUpperCase()}` : "#ZM" + Math.floor(1000 + Math.random() * 9000));

                const existingIdx = sellerOrderList.findIndex(o => o.id === ordId || o.orderCode === code);
                const formattedPrice = typeof bo.total === "number"
                  ? `${bo.total.toLocaleString("vi-VN")} đ`
                  : (bo.priceFormatted || "0 đ");

                const mappedItems: OrderItem[] = Array.isArray(bo.items) ? bo.items.map((it: any) => ({
                  name: it.name || "Sản phẩm",
                  qty: it.quantity || it.qty || 1,
                  price: it.price || 0
                })) : [];

                const mappedSummary = mappedItems.length > 0
                  ? mappedItems.map(it => `${it.name} (x${it.qty})`).join(", ")
                  : (bo.productSummary || "Đơn hàng nông sản");

                let mappedStatus: SellerOrder["status"] = "Đang xử lý";
                if (bo.status === "completed" || bo.status === "Hoàn thành") mappedStatus = "Hoàn thành";
                else if (bo.status === "delivering" || bo.status === "picking") mappedStatus = "Đang giao hàng";

                const newOrd: SellerOrder = {
                  id: ordId,
                  orderCode: code,
                  productSummary: mappedSummary,
                  customerName: bo.customer?.name || bo.customerName || "Khách hàng ZoneMart",
                  customerPhone: bo.customer?.phone || bo.customerPhone || "0912 345 678",
                  customerAddress: bo.customer?.address || bo.shippingAddress || bo.customerAddress || "Khu vực Hà Nội",
                  orderDate: bo.createdAt || bo.orderDate || "Hôm nay",
                  priceFormatted: formattedPrice,
                  paymentMethod: bo.paymentMethod || "Chuyển khoản QR",
                  status: mappedStatus,
                  items: mappedItems,
                  shipperInfo: bo.shipper
                };

                if (existingIdx >= 0) {
                  sellerOrderList[existingIdx] = { ...sellerOrderList[existingIdx], ...newOrd };
                } else {
                  sellerOrderList.unshift(newOrd);
                }
              }
            });
          }
        }
      }
    }
  } catch {}

  // 3. Nếu là tài khoản mẫu Ba Vì và chưa có đơn thật, giữ demo Ba Vì
  if (cleanEmail === "seller@zonemart.vn" && sellerOrderList.length === 0) {
    sellerOrderList = [
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
      }
    ];
  }

  orders.value = sellerOrderList;
  if (cleanEmail) {
    localStorage.setItem(`zonemart_seller_orders_${cleanEmail}`, JSON.stringify(orders.value));
  }
  recalculateKPIs();
};

// Tải đánh giá của gian hàng
const loadSellerFeedbacks = () => {
  const cleanEmail = (currentUser.email || "").toLowerCase().trim();
  const saved = localStorage.getItem(`zonemart_seller_feedbacks_${cleanEmail}`);
  if (saved) {
    try {
      sellerFeedbacks.value = JSON.parse(saved);
      return;
    } catch {}
  }

  if (cleanEmail === "seller@zonemart.vn") {
    sellerFeedbacks.value = [
      {
        id: "fb-1",
        author: "Chị Lan Hương",
        comment: "Rau muống rất tươi ngon, giao hỏa tốc 20 phút là tới nơi!",
        rating: 5
      },
      {
        id: "fb-2",
        author: "Anh Minh Quân",
        comment: "Thịt ba chỉ đóng khay sạch sẽ, tem VietGAP rõ ràng. Sẽ ủng hộ shop dài lâu.",
        rating: 5
      }
    ];
  } else {
    sellerFeedbacks.value = [];
  }
};

// Lưu thay đổi cài đặt gian hàng (Tab 6)
const handleSaveStoreSettings = async () => {
  const cleanEmail = (currentUser.email || "").toLowerCase().trim();
  if (!cleanEmail) {
    triggerToast("Lỗi: Không xác định được email người bán!");
    return;
  }

  localStorage.setItem(`zonemart_seller_store_${cleanEmail}`, JSON.stringify({
    id: storeInfo.id,
    name: storeInfo.name,
    category: storeInfo.category,
    address: storeInfo.address,
    phone: storeInfo.phone,
    openHours: storeInfo.openHours,
    bankName: storeInfo.bankName,
    bankAccount: storeInfo.bankAccount,
    accountHolder: storeInfo.accountHolder
  }));

  try {
    const rawUser = localStorage.getItem("currentUser");
    if (rawUser) {
      const u = JSON.parse(rawUser);
      u.storeName = storeInfo.name;
      localStorage.setItem("currentUser", JSON.stringify(u));
    }
    const rawZm = localStorage.getItem("zonemart_user");
    if (rawZm) {
      const zm = JSON.parse(rawZm);
      zm.storeName = storeInfo.name;
      localStorage.setItem("zonemart_user", JSON.stringify(zm));
    }
  } catch {}

  try {
    await fetch("/api/auth/update-store-profile", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        account: cleanEmail,
        storeName: storeInfo.name,
        category: storeInfo.category,
        address: storeInfo.address,
        openHours: storeInfo.openHours,
        bankName: storeInfo.bankName,
        bankAccountNumber: storeInfo.bankAccount,
        ownerFullName: storeInfo.accountHolder
      })
    }).catch(() => {});
  } catch {}

  triggerToast(`Đã lưu thông tin cài đặt gian hàng "${storeInfo.name}" thành công!`);
};

// Trạng thái vi phạm của Seller hiện tại
const currentSellerPenalty = computed(() =>
  productModeration.getSellerPenalty(currentUser.email)
);

// Trạng thái AI Scanning & Modal Kết Quả
const isScanningAI = ref(false);
const scanStepText = ref("");
const scanProgress = ref(0);
const lastScanResult = ref<AIScanResult | null>(null);
const showScanResultModal = ref(false);
const penaltyNotice = ref<{ actionType: string; violationCount: number; message: string } | null>(null);

// Modal thêm / sửa sản phẩm
const showAddProductModal = ref(false);
const isEditingMode = ref(false);
const editingProductId = ref<string | null>(null);

const fileInputRef = ref<HTMLInputElement | null>(null);
const isDraggingFile = ref(false);
const uploadedFileName = ref("");
const uploadedFileSize = ref("");
const customCategoryInput = ref("");
const imageVisualTag = ref<string>("unknown");

const newProductForm = reactive({
  name: "",
  category: "Rau củ quả",
  price: 25000,
  unit: "Bó 500g",
  stock: 30,
  image: ""
});

const triggerFileInput = () => {
  fileInputRef.value?.click();
};

const analyzeImagePixels = (dataUrl: string): Promise<string> => {
  return new Promise((resolve) => {
    if (!dataUrl) {
      imageVisualTag.value = "unknown";
      resolve("unknown");
      return;
    }
    const img = new Image();
    img.onload = () => {
      try {
        const canvas = document.createElement("canvas");
        canvas.width = 48;
        canvas.height = 48;
        const ctx = canvas.getContext("2d", { willReadFrequently: true });
        if (!ctx) {
          imageVisualTag.value = "unknown";
          resolve("unknown");
          return;
        }
        ctx.drawImage(img, 0, 0, 48, 48);
        const imgData = ctx.getImageData(0, 0, 48, 48).data;
        let whiteBgCount = 0;
        let greenCount = 0;
        let redCount = 0;
        let darkSteelCount = 0;
        let brassBulletCount = 0;
        let nonBgCount = 0;
        const pixelCount = imgData.length / 4;

        for (let i = 0; i < imgData.length; i += 4) {
          const r = imgData[i];
          const g = imgData[i + 1];
          const b = imgData[i + 2];
          const a = imgData[i + 3];

          // Bỏ qua pixel trong suốt hoặc nền trắng studio ảnh sản phẩm
          if (a < 30) {
            whiteBgCount++;
            continue;
          }

          const maxVal = Math.max(r, g, b);
          const minVal = Math.min(r, g, b);
          const isStudioWhite = minVal > 210 && (maxVal - minVal) < 30;

          if (isStudioWhite) {
            whiteBgCount++;
            continue;
          }

          nonBgCount++;

          // Nhận diện đen sắt thép súng / vũ khí kim loại (r,g,b < 65, độ bão hòa thấp)
          if (r < 65 && g < 65 && b < 65 && (maxVal - minVal < 25)) {
            darkSteelCount++;
          }
          // Nhận diện sắc vàng đồng (vỏ đạn, đầu đạn kim loại: r > 130, g > 95, b < 75)
          else if (r > 130 && g > 95 && b < 75 && r > b * 1.5) {
            brassBulletCount++;
          }
          // Nhận diện sắc xanh rau củ tươi (rau xà lách, rau muống, cải...)
          else if ((g > r * 1.12 && g > b * 1.05 && g > 45) || (g > 70 && g > r + 10 && g > b + 10)) {
            greenCount++;
          }
          // Nhận diện sắc đỏ / hồng tươi sống (thịt, cá đỏ)
          else if (r > g * 1.25 && r > b * 1.15 && r > 70) {
            redCount++;
          }
        }

        const darkSteelRatio = darkSteelCount / pixelCount;
        const brassBulletRatio = brassBulletCount / pixelCount;
        const greenSubjectRatio = nonBgCount > 0 ? greenCount / nonBgCount : 0;
        const greenTotalRatio = greenCount / pixelCount;
        const redSubjectRatio = nonBgCount > 0 ? redCount / nonBgCount : 0;
        const redTotalRatio = redCount / pixelCount;

        let tag = "neutral";
        // Phát hiện vũ khí súng đạn: Kim loại đen thép súng kết hợp vỏ đạn hoặc nền đỏ tương phản, không có rau
        const isWeaponPattern = (darkSteelRatio >= 0.12 && (brassBulletRatio >= 0.012 || redTotalRatio >= 0.15) && greenTotalRatio < 0.05) ||
                                (darkSteelRatio >= 0.22 && greenTotalRatio < 0.05 && redSubjectRatio < 0.35);

        if (isWeaponPattern) {
          tag = "prohibited_weapon";
        } else if (greenSubjectRatio >= 0.18 || greenTotalRatio >= 0.08) {
          tag = "green_vegetables";
        } else if (redSubjectRatio >= 0.18 || redTotalRatio >= 0.08) {
          tag = "red_meat";
        } else if (whiteBgCount / pixelCount > 0.60 && greenSubjectRatio < 0.10 && redSubjectRatio < 0.10) {
          tag = "white_packaged";
        }

        imageVisualTag.value = tag;
        resolve(tag);
      } catch {
        imageVisualTag.value = "unknown";
        resolve("unknown");
      }
    };
    img.onerror = () => {
      imageVisualTag.value = "unknown";
      resolve("unknown");
    };
    img.src = dataUrl;
  });
};

const processUploadedFile = (file: File) => {
  if (!file.type.startsWith("image/")) {
    triggerToast("Vui lòng chọn tệp hình ảnh hợp lệ (PNG, JPG, WEBP)!");
    return;
  }
  if (file.size > 10 * 1024 * 1024) {
    triggerToast("Dung lượng tệp ảnh vượt quá 10MB!");
    return;
  }
  uploadedFileName.value = file.name;
  uploadedFileSize.value = (file.size / 1024).toFixed(1) + " KB";

  const reader = new FileReader();
  reader.onload = async (e) => {
    const rawDataUrl = (e.target?.result as string) || "";
    // Nén ảnh tự động để đảm bảo dung lượng siêu nhẹ (~20KB) không bao giờ tràn bộ nhớ
    const dataUrl = await productModeration.compressImage(rawDataUrl, 400, 400, 0.75);
    newProductForm.image = dataUrl;
    uploadedFileSize.value = (Math.round(dataUrl.length * 0.75) / 1024).toFixed(1) + " KB (Đã nén chuẩn)";
    await analyzeImagePixels(dataUrl);
  };
  reader.readAsDataURL(file);
};

const handleFileUpload = (event: Event) => {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0];
  if (file) {
    processUploadedFile(file);
  }
  target.value = "";
};

const handleFileDrop = (event: DragEvent) => {
  isDraggingFile.value = false;
  const file = event.dataTransfer?.files?.[0];
  if (file) {
    processUploadedFile(file);
  }
};

const clearUploadedImage = () => {
  newProductForm.image = "";
  uploadedFileName.value = "";
  uploadedFileSize.value = "";
  imageVisualTag.value = "unknown";
};

// Mở modal thêm sản phẩm mới
const openCreateProduct = () => {
  const penalty = currentSellerPenalty.value;
  if (penalty.isBanned) {
    triggerToast("Tài khoản của bạn đã bị XÓA VĨNH VIỄN do tái phạm > 10 lần. Không thể đăng bài!");
    return;
  }
  if (penalty.isLocked) {
    const lockDate = penalty.lockUntil ? new Date(penalty.lockUntil).toLocaleDateString("vi-VN") : "10 ngày";
    triggerToast(`Tài khoản đang bị TẠM KHÓA đến ${lockDate} do vi phạm chính sách!`);
    return;
  }
  isEditingMode.value = false;
  editingProductId.value = null;
  newProductForm.name = "";
  newProductForm.category = "Rau củ quả";
  customCategoryInput.value = "";
  newProductForm.price = 25000;
  newProductForm.unit = "Bó 500g";
  newProductForm.stock = 30;
  newProductForm.image = "";
  uploadedFileName.value = "";
  uploadedFileSize.value = "";
  showAddProductModal.value = true;
};

// B6: Sửa bài bị Manager từ chối hoặc cần cập nhật -> Kích hoạt AI quét lại
const openEditProduct = (prod: ModeratedProduct) => {
  const penalty = currentSellerPenalty.value;
  if (penalty.isBanned || penalty.isLocked) {
    triggerToast("Tài khoản đang bị giới hạn, không thể sửa bài!");
    return;
  }
  isEditingMode.value = true;
  editingProductId.value = prod.id;
  newProductForm.name = prod.name;
  
  const standardCategories = ["Rau củ quả", "Thịt cá tươi", "Trái cây tươi", "Thực phẩm bổ dưỡng", "Món ăn nóng"];
  if (standardCategories.includes(prod.category)) {
    newProductForm.category = prod.category;
    customCategoryInput.value = "";
  } else {
    newProductForm.category = "Khác";
    customCategoryInput.value = prod.category;
  }

  newProductForm.price = prod.price;
  newProductForm.unit = prod.unit;
  newProductForm.stock = prod.stock;
  newProductForm.image = prod.image;
  uploadedFileName.value = prod.name ? `${prod.name}.jpg` : "Ảnh sản phẩm hiện tại";
  uploadedFileSize.value = "";
  showAddProductModal.value = true;
};


// Xử lý lưu & kích hoạt AI Quét Bài (Luồng 2)
const handleSaveProduct = async () => {
  if (!newProductForm.name.trim() || newProductForm.price <= 0) {
    triggerToast("Vui lòng nhập tên và giá bán hợp lệ!");
    return;
  }
  if (newProductForm.category === "Khác" && !customCategoryInput.value.trim()) {
    triggerToast("Vui lòng nhập tên ngành hàng khác!");
    return;
  }
  if (!newProductForm.image) {
    triggerToast("Vui lòng tải lên tệp ảnh cho sản phẩm!");
    return;
  }

  // Tự động nén ảnh nếu là base64 trước khi quét và lưu
  if (newProductForm.image.startsWith("data:")) {
    newProductForm.image = await productModeration.compressImage(newProductForm.image, 400, 400, 0.75);
  }

  // Đảm bảo đối soát pixel ảnh hoàn tất chính xác trước khi đưa vào AI
  const currentVisualTag = await analyzeImagePixels(newProductForm.image);

  const finalCategory = (newProductForm.category === "Khác"
    ? customCategoryInput.value.trim()
    : newProductForm.category);

  showAddProductModal.value = false;
  isScanningAI.value = true;
  scanProgress.value = 15;
  scanStepText.value = "Đang kết nối AI Moderation Engine & quét nội dung...";

  setTimeout(() => {
    scanProgress.value = 55;
    scanStepText.value = "Phát hiện nội dung 18+, từ khóa cấm & đối soát thị giác Ảnh - Tên...";
  }, 600);

  setTimeout(() => {
    scanProgress.value = 90;
    scanStepText.value = "Tổng hợp kết quả thẩm định an toàn & độ tương thích...";
  }, 1200);

  setTimeout(() => {
    isScanningAI.value = false;
    scanProgress.value = 100;

    try {
      if (isEditingMode.value && editingProductId.value) {
        const res = productModeration.updateAndRescanProduct(editingProductId.value, {
          name: newProductForm.name.trim(),
          category: finalCategory,
          price: newProductForm.price,
          unit: newProductForm.unit,
          stock: newProductForm.stock,
          image: newProductForm.image,
          imageFileName: uploadedFileName.value || undefined,
          imageVisualTag: currentVisualTag || imageVisualTag.value
        });
        lastScanResult.value = res.scanResult;
        penaltyNotice.value = res.penaltyResult || null;
      } else {
        const res = productModeration.submitNewProduct(currentUser.email, storeInfo.name, {
          name: newProductForm.name.trim(),
          category: finalCategory,
          price: newProductForm.price,
          unit: newProductForm.unit,
          stock: newProductForm.stock,
          image: newProductForm.image,
          imageFileName: uploadedFileName.value || undefined,
          imageVisualTag: currentVisualTag || imageVisualTag.value
        });
        lastScanResult.value = res.scanResult;
        penaltyNotice.value = res.penaltyResult || null;
      }
      showScanResultModal.value = true;
    } catch (err: any) {
      triggerToast(err.message || "Lỗi xử lý kiểm duyệt AI!");
    }
  }, 1700);
};

// Reset vi phạm demo
const handleResetViolationsDemo = () => {
  productModeration.resetSellerPenalties(currentUser.email);
  triggerToast("Đã reset vi phạm về 0 và mở khóa tài khoản demo thành công!");
};

// Bật/tắt trạng thái bán
const handleToggleAvailable = (prod: ModeratedProduct) => {
  prod.isAvailable = !prod.isAvailable;
  productModeration.saveProducts();
  triggerToast(prod.isAvailable ? `Đã mở bán sản phẩm "${prod.name}"` : `Đã tạm ngưng bán "${prod.name}"`);
};

// Xử lý đơn hàng
const handleActionOrder = (order: SellerOrder) => {
  if (order.status === "Đang xử lý" || order.status === "Chờ xác nhận") {
    order.status = "Hoàn thành";
    const cleanEmail = (currentUser.email || "").toLowerCase().trim();
    if (cleanEmail) {
      localStorage.setItem(`zonemart_seller_orders_${cleanEmail}`, JSON.stringify(orders.value));
    }
    recalculateKPIs();
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
    <!-- MODAL THÔNG BÁO GIAO ĐƠN HÀNG THÀNH CÔNG CHO SELLER -->
    <transition name="modal-fade">
      <div v-if="deliveredNotification && deliveredNotification.show" class="seller-delivery-modal-overlay">
        <div class="seller-delivery-modal">
          <div class="seller-modal-header">
            <div class="seller-modal-icon bg-green">
              <i class="bi bi-check2-circle"></i>
            </div>
            <div class="seller-modal-title">
              <h3>🎉 ĐÃ GIAO ĐƠN HÀNG THÀNH CÔNG!</h3>
              <p>Mã đơn hàng: <strong>#{{ deliveredNotification.orderCode }}</strong></p>
            </div>
            <button class="btn-close-seller-modal" @click="deliveredNotification.show = false">
              <i class="bi bi-x-lg"></i>
            </button>
          </div>

          <div class="seller-modal-body">
            <div class="delivery-details-card">
              <div class="card-caption">
                <i class="bi bi-box-seam-fill text-primary me-1"></i>
                <span>Danh sách sản phẩm đã giao tới khách:</span>
              </div>

              <div class="delivered-items-scroller">
                <div v-for="(it, idx) in deliveredNotification.items" :key="idx" class="delivered-prod-row">
                  <img v-if="it.image" :src="it.image" class="prod-thumb-img" />
                  <div v-else class="prod-thumb-img fallback">
                    <i class="bi bi-basket2"></i>
                  </div>
                  <div class="prod-details">
                    <strong class="prod-name">{{ it.name }}</strong>
                    <div class="prod-meta">
                      <span class="prod-code">Mã SP: #{{ it.id || it.productId || ('SP-' + (idx + 1)) }}</span>
                      <span class="prod-qty">Số lượng: <strong>x{{ it.quantity }}</strong></span>
                      <span class="prod-price" v-if="it.price > 0">{{ (it.price * it.quantity).toLocaleString('vi-VN') }} ₫</span>
                    </div>
                  </div>
                </div>
              </div>

              <div class="customer-delivery-info">
                <i class="bi bi-person-check-fill text-success"></i>
                <div>
                  <strong>Người nhận: {{ deliveredNotification.customerName }}</strong>
                  <p>{{ deliveredNotification.customerAddress }}</p>
                </div>
              </div>
            </div>
          </div>

          <div class="seller-modal-footer">
            <button type="button" class="btn-seller-confirm" @click="deliveredNotification.show = false">
              <i class="bi bi-check2-all me-1"></i> ĐÃ XÁC NHẬN ĐƠN HÀNG
            </button>
          </div>
        </div>
      </div>
    </transition>

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
                    <span class="val">{{ storeInfo.avgOrderValue }}</span>
                  </div>
                  <div class="tooltip-line">
                    <span class="dot b-dot"></span>
                    <span class="date">Thg 8, 2026</span>
                    <span class="val">142 đơn</span>
                    <span class="val">{{ storeInfo.totalOrders }} đơn</span>
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
              <div v-if="topProducts.length > 0" class="top-products-list">
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
              <div v-else class="text-center py-4 empty-products-box">
                <div class="empty-icon-circle">
                  <i class="bi bi-box-seam"></i>
                </div>
                <h5 class="empty-title">Gian hàng chưa có sản phẩm nào</h5>
                <p class="empty-desc">Đăng sản phẩm đầu tiên để bắt đầu tiếp cận khách hàng và nhận đơn hàng hỏa tốc!</p>
                <button type="button" class="btn-add-product-cta" @click="openCreateProduct">
                  <i class="bi bi-plus-circle-fill me-1"></i> Đăng sản phẩm mới
                </button>
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
            <div v-if="displayedOrders.length > 0" class="table-responsive-box">
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
                          'badge-pending': ord.status === 'Chờ xác nhận',
                          'badge-delivering': ord.status === 'Đang giao hàng'
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
            <div v-else class="text-center py-5 text-muted">
              <i class="bi bi-inbox fs-1 text-secondary mb-2 d-block"></i>
              <p class="mb-0">Chưa có đơn hàng nào được ghi nhận gần đây cho gian hàng này.</p>
            </div>
          </div>
        </section>

        <!-- ==================== TAB 2: PRODUCTS ==================== -->
        <section v-else-if="activeNav === 'products'" class="tab-page-container">
          <!-- THANH CẢNH BÁO VI PHẠM SELLER (NẾU CÓ) -->
          <div
            v-if="currentSellerPenalty.violationCount > 0"
            class="seller-violation-banner"
            :class="{
              'banner-banned': currentSellerPenalty.isBanned,
              'banner-locked': currentSellerPenalty.isLocked && !currentSellerPenalty.isBanned,
              'banner-warn': !currentSellerPenalty.isLocked && !currentSellerPenalty.isBanned
            }"
          >
            <div class="violation-banner-left">
              <i
                class="bi"
                :class="
                  currentSellerPenalty.isBanned
                    ? 'bi-x-octagon-fill text-danger'
                    : currentSellerPenalty.isLocked
                    ? 'bi-shield-lock-fill text-warning'
                    : 'bi-exclamation-triangle-fill text-warning'
                "
              ></i>
              <div>
                <h4 v-if="currentSellerPenalty.isBanned" class="violation-title text-danger">
                  TÀI KHOẢN ĐÃ BỊ XÓA VĨNH VIỄN DO TÁI PHẠM QUÁ 10 LẦN
                </h4>
                <h4 v-else-if="currentSellerPenalty.isLocked" class="violation-title text-warning">
                  TÀI KHOẢN ĐANG BỊ KHÓA 10 NGÀY (Lần vi phạm: {{ currentSellerPenalty.violationCount }}/10)
                </h4>
                <h4 v-else class="violation-title">
                  CẢNH BÁO VI PHẠM CHÍNH SÁCH ĐĂNG BÀI: {{ currentSellerPenalty.violationCount }}/5 LẦN
                </h4>
                <p class="violation-desc">
                  <span v-if="currentSellerPenalty.isBanned">
                    Hệ thống AI đã xóa vĩnh viễn tư cách người bán của bạn theo Luồng 2. Mọi chức năng đăng bài bị vô hiệu.
                  </span>
                  <span v-else-if="currentSellerPenalty.isLocked">
                    Bạn đã bị đình chỉ đăng bài đến {{ currentSellerPenalty.lockUntil ? new Date(currentSellerPenalty.lockUntil).toLocaleDateString('vi-VN') : '10 ngày' }}. Email thông báo xử phạt đã gửi tới {{ currentUser.email }}.
                  </span>
                  <span v-else>
                    Lý do gần nhất: "{{ currentSellerPenalty.lastViolationReason }}". Email cảnh báo đã gửi tới {{ currentUser.email }}. Nếu tái phạm từ lần thứ 6 sẽ bị KHÓA TÀI KHOẢN 10 NGÀY!
                  </span>
                </p>
              </div>
            </div>
            <button
              type="button"
              class="btn-reset-demo"
              @click="handleResetViolationsDemo"
              title="Khôi phục trạng thái để kiểm thử tiếp"
            >
              <i class="bi bi-arrow-counterclockwise me-1"></i> Reset Demo Vi Phạm
            </button>
          </div>

          <!-- HEADER TAB -->
          <div class="tab-header-flex">
            <div>
              <h2 class="tab-heading">Quản Lý Sản Phẩm Gian Hàng</h2>
              <p class="tab-subheading">
                Tích hợp AI Kiểm Duyệt Bài Đăng (Luồng 2: Quét 18+, hàng cấm & đối soát Ảnh - Tên)
              </p>
            </div>
            <button
              type="button"
              class="btn-brand-primary"
              @click="openCreateProduct"
              :disabled="currentSellerPenalty.isBanned || currentSellerPenalty.isLocked"
            >
              <i class="bi bi-plus-lg me-1"></i> Thêm Sản Phẩm Mới
            </button>
          </div>

          <!-- BẢNG DANH SÁCH SẢN PHẨM -->
          <div class="dashboard-panel-card">
            <div class="table-responsive-box">
              <table class="modern-data-table">
                <thead>
                  <tr>
                    <th>Hình ảnh</th>
                    <th>Tên sản phẩm & Thông tin AI</th>
                    <th>Danh mục</th>
                    <th>Đơn vị</th>
                    <th>Giá niêm yết</th>
                    <th>Tồn kho</th>
                    <th>Trạng thái Luồng 2</th>
                    <th class="text-center">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="prod in products" :key="prod.id">
                    <td>
                      <img :src="prod.image" :alt="prod.name" class="table-prod-img" />
                    </td>
                    <td>
                      <div class="product-title-wrap">
                        <b>{{ prod.name }}</b>
                        <div v-if="prod.aiScore" class="ai-meta-tag">
                          <span class="ai-badge-match">
                            <i class="bi bi-robot"></i> Độ khớp: {{ prod.aiScore.matchScore }}%
                          </span>
                        </div>
                        <!-- Ghi chú từ chối của Manager (Nếu có) -->
                        <div v-if="prod.status === 'rejected_need_edit' && prod.managerNote" class="manager-reject-box">
                          <i class="bi bi-exclamation-triangle-fill text-danger me-1"></i>
                          <span>Manager yêu cầu sửa: "{{ prod.managerNote }}"</span>
                        </div>
                        <!-- Cờ nghi ngờ của AI (Nếu có) -->
                        <div v-else-if="prod.status === 'pending_review' && prod.aiScore?.flag" class="ai-flag-box">
                          <i class="bi bi-info-circle-fill text-warning me-1"></i>
                          <span>AI cờ: {{ prod.aiScore.flag }}</span>
                        </div>
                      </div>
                    </td>
                    <td>{{ prod.category }}</td>
                    <td>{{ prod.unit }}</td>
                    <td class="price-cell">{{ prod.price.toLocaleString('vi-VN') }} đ</td>
                    <td>{{ prod.stock }}</td>
                    <td>
                      <!-- Trạng thái 1: Đang bán -->
                      <span v-if="prod.status === 'active'" class="badge-status-pill available">
                        <i class="bi bi-check-circle-fill me-1"></i> Đang bán
                      </span>
                      <!-- Trạng thái 2: Chờ Manager duyệt (Nghi ngờ) -->
                      <span v-else-if="prod.status === 'pending_review'" class="badge-status-pill pending-ai">
                        <i class="bi bi-hourglass-split me-1"></i> Chờ Manager duyệt
                      </span>
                      <!-- Trạng thái 3: Manager từ chối -> Yêu cầu sửa -->
                      <span v-else-if="prod.status === 'rejected_need_edit'" class="badge-status-pill rejected-edit">
                        <i class="bi bi-exclamation-octagon-fill me-1"></i> Cần sửa lại
                      </span>
                      <!-- Trạng thái khác -->
                      <span v-else class="badge-status-pill out-of-stock">
                        Tạm ẩn
                      </span>
                    </td>
                    <td class="text-center">
                      <!-- Nếu sản phẩm bị Manager từ chối: Nút Sửa bài (Quét lại AI) -->
                      <button
                        v-if="prod.status === 'rejected_need_edit'"
                        type="button"
                        class="btn-rescan-pill"
                        @click="openEditProduct(prod)"
                        title="Chỉnh sửa nội dung và gửi AI quét lại từ đầu"
                      >
                        <i class="bi bi-arrow-repeat me-1"></i> Sửa bài (Quét lại)
                      </button>
                      <!-- Nếu sản phẩm đang chờ duyệt: Hiển thị trạng thái chờ -->
                      <span v-else-if="prod.status === 'pending_review'" class="pending-admin-label">
                        <i class="bi bi-shield-lock me-1"></i> Chờ duyệt ở /admin
                      </span>
                      <!-- Nếu đang bán: Nút Bật/Tắt -->
                      <button
                        v-else
                        type="button"
                        class="btn-action-pill"
                        @click="handleToggleAvailable(prod)"
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
            <div v-if="customers.length > 0" class="customer-cards-grid">
              <div v-for="c in customers" :key="c.id" class="customer-item-card">
                <div class="customer-avatar-badge">{{ c.avatarBadge }}</div>
                <div class="customer-details">
                  <h4>{{ c.name }}</h4>
                  <p>{{ c.phone }} • {{ c.address }}</p>
                  <span class="customer-tag">{{ c.tag }}</span>
                </div>
              </div>
            </div>
            <div v-else class="text-center py-5 text-muted">
              <i class="bi bi-people fs-1 text-secondary mb-2 d-block"></i>
              <h5 class="fw-bold text-dark">Chưa có khách hàng thân thiết</h5>
              <p class="mb-0">Danh sách khách hàng quen sẽ tự động được cập nhật khi có người mua đặt đơn hàng tại gian hàng của bạn.</p>
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

          <div v-if="orders.length > 0" class="orders-flow-grid">
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
          <div v-else class="dashboard-panel-card text-center py-5 text-muted">
            <i class="bi bi-box2 fs-1 text-secondary mb-2 d-block"></i>
            <h5 class="fw-bold text-dark">Chưa có đơn hàng nào</h5>
            <p class="mb-0">Gian hàng của bạn hiện chưa phát sinh đơn đặt hàng mới. Khi người mua đặt món, đơn hàng sẽ nổ tại đây theo thời gian thực!</p>
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
                  <p>10 km quanh vị trí tiệm ({{ storeDistrictName }})</p>
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
              <button type="button" class="btn-brand-primary mt-3" @click="handleSaveStoreSettings">
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
              Gian hàng <b>{{ storeInfo.name }}</b> được hưởng mức chiết khấu 0% phí nền tảng trong 12 tháng đầu tiên dành cho nông sản & bán lẻ địa phương.
            </p>
          </div>
        </section>

        <!-- ==================== TAB 8: FEEDBACK ==================== -->
        <section v-else-if="activeNav === 'feedback'" class="tab-page-container">
          <div class="dashboard-panel-card">
            <h3 class="panel-title mb-3">Đánh Giá & Nhận Xét Của Khách Hàng ({{ feedbackRatingText }})</h3>
            <div v-if="sellerFeedbacks.length > 0" class="feedback-list">
              <div v-for="fb in sellerFeedbacks" :key="fb.id" class="feedback-item">
                <div class="feedback-header">
                  <b>{{ fb.author }}</b>
                  <span class="stars">{{ '⭐'.repeat(fb.rating || 5) }}</span>
                </div>
                <p>{{ fb.comment }}</p>
              </div>
            </div>
            <div v-else class="text-center py-5 text-muted">
              <i class="bi bi-chat-square-heart fs-1 text-secondary mb-2 d-block"></i>
              <h5 class="fw-bold text-dark">Chưa có đánh giá nào</h5>
              <p class="mb-0">Gian hàng chưa nhận được đánh giá từ người mua. Phản hồi và sao đánh giá sẽ hiển thị tại đây khi khách hàng hoàn tất trải nghiệm mua sắm.</p>
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

    <!-- ==================== 1. MODAL THÊM / SỬA SẢN PHẨM ==================== -->
    <div v-if="showAddProductModal" class="modal-backdrop-overlay" @click.self="showAddProductModal = false">
      <div class="modal-card-box modal-lg modern-product-modal">
        <div class="modal-header-row">
          <div class="modal-title-with-badge">
            <div class="modal-header-icon">
              <i :class="isEditingMode ? 'bi bi-pencil-square' : 'bi bi-bag-plus-fill'"></i>
            </div>
            <div>
              <h4>{{ isEditingMode ? 'Chỉnh Sửa Sản Phẩm' : 'Thêm Sản Phẩm Mới' }}</h4>
              <p class="modal-subtitle">Sản phẩm được bảo vệ & kiểm duyệt tự động bởi hệ thống AI ZoneMart</p>
            </div>
            <span class="ai-shield-tag"><i class="bi bi-robot"></i> AI Active</span>
          </div>
          <button type="button" class="btn-close-modal" @click="showAddProductModal = false" title="Đóng">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <div class="modal-body-fields">
          <div class="field-item">
            <label>Tên sản phẩm <span class="text-danger">*</span></label>
            <input
              v-model="newProductForm.name"
              type="text"
              class="field-input"
              placeholder="VD: Xà lách mỡ VietGAP Ba Vì, Thịt ba chỉ tươi..."
              required
            />
          </div>

          <div class="field-grid-2">
            <div class="field-item">
              <label>Danh mục ngành hàng <span class="text-danger">*</span></label>
              <select v-model="newProductForm.category" class="field-input field-select">
                <option value="Rau củ quả">Rau củ quả</option>
                <option value="Thịt cá tươi">Thịt cá tươi</option>
                <option value="Trái cây tươi">Trái cây tươi</option>
                <option value="Thực phẩm bổ dưỡng">Thực phẩm bổ dưỡng</option>
                <option value="Món ăn nóng">Món ăn nóng</option>
                <option value="Khác">Khác (Ngành hàng khác)</option>
              </select>
            </div>
            <div class="field-item">
              <label>Đơn vị tính</label>
              <input v-model="newProductForm.unit" type="text" class="field-input" placeholder="VD: Bó 500g, Khay 1kg..." />
            </div>
          </div>

          <!-- Nhập ngành hàng khác khi chọn Khác -->
          <div v-if="newProductForm.category === 'Khác'" class="field-item custom-category-box">
            <label>Tên ngành hàng tùy chỉnh <span class="text-danger">*</span></label>
            <input
              v-model="customCategoryInput"
              type="text"
              class="field-input"
              placeholder="VD: Đồ khô & Gia vị, Nông sản chế biến, Bánh kẹo handmade..."
              required
            />
            <small class="custom-cat-hint">
              <i class="bi bi-info-circle me-1"></i> Nhập chính xác tên ngành hàng để người mua và AI dễ dàng phân loại sản phẩm.
            </small>
          </div>

          <div class="field-grid-2">
            <div class="field-item">
              <label>Giá niêm yết <span class="text-danger">*</span></label>
              <div class="input-suffix-wrapper">
                <input v-model.number="newProductForm.price" type="number" class="field-input with-suffix" placeholder="25000" />
                <span class="input-suffix-text">VNĐ</span>
              </div>
            </div>
            <div class="field-item">
              <label>Số lượng tồn kho</label>
              <div class="input-suffix-wrapper">
                <input v-model.number="newProductForm.stock" type="number" class="field-input with-suffix" placeholder="50" />
                <span class="input-suffix-text">Món / Gói</span>
              </div>
            </div>
          </div>

          <div class="field-item">
            <label>Hình ảnh sản phẩm <span class="text-danger">*</span></label>
            
            <!-- Hidden input file -->
            <input
              ref="fileInputRef"
              type="file"
              accept="image/png, image/jpeg, image/jpg, image/webp, image/gif"
              style="display: none;"
              @change="handleFileUpload"
            />

            <!-- Dropzone khi chưa có ảnh -->
            <div
              v-if="!newProductForm.image"
              class="upload-dropzone"
              :class="{ 'is-dragging': isDraggingFile }"
              @click="triggerFileInput"
              @dragover.prevent="isDraggingFile = true"
              @dragleave.prevent="isDraggingFile = false"
              @drop.prevent="handleFileDrop"
            >
              <div class="dropzone-content">
                <div class="dropzone-icon-circle">
                  <i class="bi bi-cloud-arrow-up-fill"></i>
                </div>
                <div class="dropzone-text">
                  <p class="dropzone-main-text">
                    <span class="text-primary-link">Bấm để tải tệp ảnh lên</span> hoặc kéo thả ảnh vào đây
                  </p>
                  <p class="dropzone-sub-text">Hỗ trợ JPG, PNG, WEBP (Khuyên dùng ảnh chụp thật, tối đa 10MB)</p>
                </div>
              </div>
            </div>

            <!-- Card hiển thị khi đã chọn/tải ảnh -->
            <div v-else class="image-uploaded-card">
              <div class="uploaded-card-left">
                <img :src="newProductForm.image" alt="Uploaded Preview" class="uploaded-preview-img" />
                <div class="uploaded-file-details">
                  <div class="uploaded-filename">{{ uploadedFileName || 'Tệp hình ảnh sản phẩm' }}</div>
                  <div class="uploaded-filesize">
                    <span v-if="uploadedFileSize">{{ uploadedFileSize }} • </span>
                    <span class="text-success fw-bold"><i class="bi bi-shield-check"></i> Sẵn sàng quét AI</span>
                  </div>
                </div>
              </div>
              <div class="uploaded-card-actions">
                <button type="button" class="btn-change-image" @click="triggerFileInput" title="Chọn file ảnh khác">
                  <i class="bi bi-arrow-repeat me-1"></i> Đổi ảnh
                </button>
                <button type="button" class="btn-remove-image" @click="clearUploadedImage" title="Xóa tệp ảnh này">
                  <i class="bi bi-trash3-fill"></i>
                </button>
              </div>
            </div>

            <div class="ai-image-note">
              <div class="ai-note-icon">
                <i class="bi bi-shield-lock-fill"></i>
              </div>
              <div class="ai-note-text">
                <b>Bảo vệ quyền lợi & kiểm duyệt chất lượng:</b>
                <span> Hệ thống AI sẽ phân tích thị giác hình ảnh để kiểm tra hàng cấm, độ tươi mới & tính tương quan với tên sản phẩm.</span>
              </div>
            </div>
          </div>
        </div>

        <div class="modal-footer-row">
          <button type="button" class="btn-cancel-gray" @click="showAddProductModal = false">
            <i class="bi bi-x-circle me-1"></i> Hủy Bỏ
          </button>
          <button type="button" class="btn-save-product-primary" @click="handleSaveProduct">
            <i class="bi bi-robot me-1"></i>
            {{ isEditingMode ? 'Lưu & Quét Lại AI ➔' : 'Gửi Bài & Quét AI ➔' }}
          </button>
        </div>
      </div>
    </div>

    <!-- ==================== 2. AI SCANNING OVERLAY (RADAR HUD) ==================== -->
    <div v-if="isScanningAI" class="ai-scanning-overlay">
      <div class="ai-scan-card">
        <div class="radar-box">
          <div class="radar-circle circle-1"></div>
          <div class="radar-circle circle-2"></div>
          <div class="radar-circle circle-3"></div>
          <div class="radar-beam"></div>
          <div class="radar-center-bot">
            <i class="bi bi-robot"></i>
          </div>
        </div>

        <h3 class="ai-scan-title">AI Đang Quét Bài Đăng Theo Luồng 2...</h3>
        <p class="ai-scan-step-text">{{ scanStepText }}</p>

        <!-- Progress bar -->
        <div class="scan-progress-track">
          <div class="scan-progress-fill" :style="{ width: `${scanProgress}%` }"></div>
        </div>
        <span class="scan-percent">{{ scanProgress }}% Hoàn tất</span>

        <div class="ai-check-bullets">
          <span class="check-item"><i class="bi bi-shield-check text-success"></i> Bộ lọc 18+ & Khiêu dâm</span>
          <span class="check-item"><i class="bi bi-shield-check text-success"></i> Hàng quốc cấm & Vũ khí</span>
          <span class="check-item"><i class="bi bi-search text-primary"></i> Đối soát thị giác Ảnh - Tên</span>
        </div>
      </div>
    </div>

    <!-- ==================== 3. MODAL KẾT QUẢ AI QUÉT BÀI (LUỒNG 2) ==================== -->
    <div v-if="showScanResultModal && lastScanResult" class="modal-backdrop-overlay" @click.self="showScanResultModal = false">
      <div class="modal-card-box result-card-box">
        <!-- NHÁNH 1: VI PHẠM NGHIÊM TRỌNG (18+, HÀNG CẤM) -->
        <template v-if="lastScanResult.decision === 'VIOLATION'">
          <div class="result-header-box violation">
            <div class="result-icon-badge danger">
              <i class="bi bi-x-octagon-fill"></i>
            </div>
            <h3 class="result-title text-danger">HỆ THỐNG XÓA BÀI: PHÁT HIỆN VI PHẠM CHÍNH SÁCH!</h3>
            <p class="result-subtitle">Hệ thống AI đã xóa bỏ bài đăng ngay lập tức theo quy định Luồng 2</p>
          </div>

          <div class="result-body-content">
            <!-- Lý do vi phạm -->
            <div class="violation-reason-panel">
              <h5><i class="bi bi-exclamation-triangle-fill text-danger me-1"></i> Nội dung vi phạm:</h5>
              <p class="reason-text">{{ lastScanResult.reason }}</p>
            </div>

            <!-- Khung xử phạt tích lũy -->
            <div v-if="penaltyNotice" class="penalty-status-panel">
              <div class="penalty-badge-row">
                <span class="penalty-count-badge">
                  Số lần vi phạm tích lũy: <strong>{{ penaltyNotice.violationCount }} lần</strong>
                </span>
                <span
                  class="penalty-level-tag"
                  :class="{
                    'tag-warn': penaltyNotice.actionType === 'warn',
                    'tag-lock': penaltyNotice.actionType === 'lock',
                    'tag-ban': penaltyNotice.actionType === 'ban'
                  }"
                >
                  {{
                    penaltyNotice.actionType === 'warn'
                      ? 'Cảnh báo Email (<= 5 lần)'
                      : penaltyNotice.actionType === 'lock'
                      ? 'Khóa TK 10 ngày (6-10 lần)'
                      : 'Xóa vĩnh viễn TK (> 10 lần)'
                  }}
                </span>
              </div>

              <!-- Mô phỏng hộp thư Email thông báo kỷ luật -->
              <div class="simulated-email-box">
                <div class="email-box-header">
                  <i class="bi bi-envelope-exclamation-fill text-danger me-1"></i>
                  <span>Thông báo kỷ luật tự động đã gửi tới: <strong>{{ currentUser.email }}</strong></span>
                </div>
                <div class="email-box-body">
                  <p>{{ penaltyNotice.message }}</p>
                </div>
              </div>
            </div>
          </div>

          <div class="result-footer-box">
            <button type="button" class="btn-cancel-gray" @click="showScanResultModal = false">
              Đã hiểu & Đóng
            </button>
            <button type="button" class="btn-reset-danger" @click="handleResetViolationsDemo(); showScanResultModal = false">
              <i class="bi bi-arrow-counterclockwise me-1"></i> Reset Vi Phạm Để Test Tiếp
            </button>
          </div>
        </template>

        <!-- NHÁNH 2: NGHI NGỜ SAI LỆCH (ẢNH - TÊN KHÔNG KHỚP) -->
        <template v-else-if="lastScanResult.decision === 'SUSPICIOUS'">
          <div class="result-header-box suspicious">
            <div class="result-icon-badge warning">
              <i class="bi bi-question-diamond-fill"></i>
            </div>
            <h3 class="result-title text-warning">ĐƯA VÀO DANH SÁCH CHỜ (PENDING MANAGER)</h3>
            <p class="result-subtitle">AI phát hiện nghi ngờ sai lệch giữa Ảnh chụp và Tên sản phẩm</p>
          </div>

          <div class="result-body-content">
            <div class="scores-summary-row">
              <div class="score-card">
                <span class="score-title">Độ khớp Ảnh - Tên</span>
                <span class="score-val text-warning">{{ lastScanResult.matchScore }}%</span>
                <span class="score-sub">Chưa đạt ngưỡng 70%</span>
              </div>
              <div class="score-card">
                <span class="score-title">Chỉ số an toàn</span>
                <span class="score-val text-success">{{ lastScanResult.safetyScore }}%</span>
                <span class="score-sub">Không có hàng cấm/18+</span>
              </div>
            </div>

            <div class="suspicious-reason-box">
              <h5><i class="bi bi-info-circle-fill text-warning me-1"></i> Chi tiết cờ nghi ngờ của AI:</h5>
              <p>{{ lastScanResult.reason }}</p>
            </div>

            <div class="manager-handover-box">
              <i class="bi bi-person-badge-fill text-primary fs-3"></i>
              <div>
                <h6>Chuyển tiếp cho Quản Lý (Manager) kiểm tra thủ công</h6>
                <p>
                  Món hàng đã được tạm đưa vào danh sách chờ. Manager tại trang <strong>/admin</strong> sẽ kiểm tra:
                  <br />• Nếu Manager <strong>Chấp nhận</strong>: Sản phẩm sẽ tự động kích hoạt Đang Bán.
                  <br />• Nếu Manager <strong>Từ chối</strong>: Món hàng sẽ bị ẩn và gửi lý do yêu cầu bạn Sửa Bài.
                </p>
              </div>
            </div>
          </div>

          <div class="result-footer-box">
            <button type="button" class="btn-cancel-gray" @click="showScanResultModal = false">
              Đóng
            </button>
            <router-link to="/admin" class="btn-brand-primary" @click="showScanResultModal = false">
              <i class="bi bi-box-arrow-up-right me-1"></i> Đến Trang /admin Để Duyệt Ngay ➔
            </router-link>
          </div>
        </template>

        <!-- NHÁNH 3: HỢP LỆ (PASS 100%) -->
        <template v-else>
          <div class="result-header-box passed">
            <div class="result-icon-badge success">
              <i class="bi bi-check-circle-fill"></i>
            </div>
            <h3 class="result-title text-success">KIỂM DUYỆT THÀNH CÔNG: SẢN PHẨM HỢP LỆ!</h3>
            <p class="result-subtitle">Sản phẩm đạt chuẩn an toàn & khớp hoàn toàn giữa tên và hình ảnh</p>
          </div>

          <div class="result-body-content">
            <div class="scores-summary-row">
              <div class="score-card">
                <span class="score-title">Độ khớp Ảnh - Tên</span>
                <span class="score-val text-success">{{ lastScanResult.matchScore }}%</span>
                <span class="score-sub">Tuyệt đối an tâm</span>
              </div>
              <div class="score-card">
                <span class="score-title">Chỉ số an toàn</span>
                <span class="score-val text-success">{{ lastScanResult.safetyScore }}%</span>
                <span class="score-sub">Không có vi phạm</span>
              </div>
            </div>

            <div class="passed-success-box">
              <i class="bi bi-shop-window text-success fs-3"></i>
              <div>
                <h6>Hệ thống: Sản phẩm Đang Bán (B7)</h6>
                <p>Món hàng đã được thêm vào gian hàng của bạn và đồng bộ trực tiếp lên trang mua sắm khách hàng (/products).</p>
              </div>
            </div>
          </div>

          <div class="result-footer-box">
            <button type="button" class="btn-brand-primary" @click="showScanResultModal = false">
              <i class="bi bi-check-lg me-1"></i> Hoàn Tất & Xem Gian Hàng
            </button>
          </div>
        </template>
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

/* ==================== UPGRADED MODERN PRODUCT MODAL ==================== */
.modal-card-box.modal-lg.modern-product-modal {
  max-width: 660px;
  width: 95%;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  padding: 0;
  overflow: hidden;
  background: #FFFFFF;
  border-radius: 20px;
  box-shadow: 0 25px 60px -15px rgba(15, 23, 42, 0.4), 0 0 0 1px rgba(226, 232, 240, 0.8);
  border: none;
}

.modern-product-modal .modal-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px 16px;
  border-bottom: 1px solid #F1F5F9;
  background: #FFFFFF;
  margin-bottom: 0;
}

.modern-product-modal .modal-title-with-badge {
  display: flex;
  align-items: center;
  gap: 14px;
}

.modern-product-modal .modal-header-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: linear-gradient(135deg, #FFEDD5 0%, #FED7AA 100%);
  color: #EA580C;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
  box-shadow: 0 4px 10px rgba(234, 88, 12, 0.15);
}

.modern-product-modal .modal-header-row h4 {
  font-size: 17.5px;
  font-weight: 800;
  color: #0F172A !important;
  margin: 0 0 2px 0;
  letter-spacing: -0.2px;
}

.modern-product-modal .modal-subtitle {
  font-size: 12px;
  color: #64748B;
  margin: 0;
}

.modern-product-modal .btn-close-modal {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  color: #64748B;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 13px;
}

.modern-product-modal .btn-close-modal:hover {
  background: #FEE2E2;
  border-color: #FECDD3;
  color: #EF4444;
  transform: rotate(90deg);
}

.modern-product-modal .modal-body-fields {
  padding: 20px 24px;
  overflow-y: auto;
  max-height: calc(90vh - 150px);
  margin-bottom: 0;
  gap: 16px;
}

.modern-product-modal .modal-body-fields::-webkit-scrollbar {
  width: 6px;
}

.modern-product-modal .modal-body-fields::-webkit-scrollbar-thumb {
  background: #CBD5E1;
  border-radius: 4px;
}

.modern-product-modal .field-item label {
  font-size: 13px;
  font-weight: 700;
  color: #334155 !important;
  margin-bottom: 6px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.modern-product-modal .field-input {
  background: #F8FAFC !important;
  border: 1.5px solid #E2E8F0;
  border-radius: 12px;
  padding: 10px 14px;
  font-size: 13.5px;
  color: #0F172A !important;
  transition: all 0.2s ease;
}

.modern-product-modal .field-input:focus {
  background: #FFFFFF !important;
  border-color: #EA580C;
  box-shadow: 0 0 0 3.5px rgba(234, 88, 12, 0.12);
}

.modern-product-modal .field-select {
  cursor: pointer;
  appearance: auto;
}

/* Suffix wrapper for Currency & Quantity */
.input-suffix-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-suffix-wrapper .field-input.with-suffix {
  padding-right: 70px;
}

.input-suffix-text {
  position: absolute;
  right: 10px;
  font-size: 11.5px;
  font-weight: 700;
  color: #64748B;
  background: #E2E8F0;
  padding: 3px 8px;
  border-radius: 6px;
  pointer-events: none;
}

/* AI Note card inside modal */
.modern-product-modal .ai-image-note {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  background: #F0FDF4;
  border: 1px solid #BBF7D0;
  border-radius: 12px;
  padding: 12px 14px;
  margin-top: 10px;
}

.modern-product-modal .ai-note-icon {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: #DCFCE7;
  color: #16A34A;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  flex-shrink: 0;
}

.modern-product-modal .ai-note-text {
  font-size: 12px;
  line-height: 1.5;
  color: #166534;
}

.modern-product-modal .ai-note-text b {
  color: #14532D;
}

/* Modal Footer */
.modern-product-modal .modal-footer-row {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 24px;
  background: #F8FAFC;
  border-top: 1px solid #E2E8F0;
}

.btn-save-product-primary {
  background: linear-gradient(135deg, #EA580C 0%, #D94E15 100%);
  color: #FFFFFF !important;
  border: none;
  border-radius: 12px;
  padding: 11px 22px;
  font-size: 13.5px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.28);
  transition: all 0.25s ease;
}

.btn-save-product-primary:hover {
  background: linear-gradient(135deg, #F97316 0%, #EA580C 100%);
  box-shadow: 0 6px 18px rgba(234, 88, 12, 0.38);
  transform: translateY(-1px);
}

.btn-save-product-primary:active {
  transform: translateY(0);
}

/* ==================== OVERVIEW EMPTY PRODUCT CTA ==================== */
.empty-products-box {
  padding: 32px 16px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.empty-icon-circle {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: #FFF7ED;
  color: #EA580C;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 26px;
  margin-bottom: 12px;
  border: 1px solid #FFEDD5;
}

.empty-title {
  font-size: 15px;
  font-weight: 700;
  color: #1E293B;
  margin: 0 0 4px 0;
}

.empty-desc {
  font-size: 12.5px;
  color: #64748B;
  max-width: 320px;
  margin: 0 0 16px 0;
  line-height: 1.45;
}

.btn-add-product-cta {
  background: #02894A;
  color: #FFFFFF !important;
  border: none;
  padding: 9px 18px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  box-shadow: 0 3px 10px rgba(2, 137, 74, 0.25);
  transition: all 0.2s ease;
}

.btn-add-product-cta:hover {
  background: #02733E;
  box-shadow: 0 5px 14px rgba(2, 137, 74, 0.35);
  transform: translateY(-1px);
}

.btn-add-product-cta:active {
  transform: translateY(0);
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

/* ==================== AI MODERATION & VIOLATION BANNER (LUỒNG 2) ==================== */
.seller-violation-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-radius: 14px;
  margin-bottom: 20px;
  gap: 16px;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.04);
}

.banner-warn {
  background: #FFFBEB;
  border: 1.5px solid #FDE68A;
}

.banner-locked {
  background: #FEF3C7;
  border: 1.5px solid #F59E0B;
}

.banner-banned {
  background: #FEF2F2;
  border: 1.5px solid #F87171;
}

.violation-banner-left {
  display: flex;
  align-items: flex-start;
  gap: 14px;
}

.violation-banner-left i {
  font-size: 24px;
  line-height: 1;
}

.violation-title {
  font-size: 15px;
  font-weight: 800;
  margin: 0 0 4px 0;
}

.violation-desc {
  font-size: 13px;
  color: #475569;
  margin: 0;
  line-height: 1.45;
}

.btn-reset-demo {
  background: #FFFFFF;
  border: 1.5px solid #CBD5E1;
  color: #0F172A !important;
  font-size: 12.5px;
  font-weight: 700;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.2s;
}

.btn-reset-demo:hover {
  background: #F1F5F9;
  transform: translateY(-1px);
}

/* AI Flow Demo Bar */
.ai-flow-demo-bar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 14px;
  padding: 12px 18px;
  margin-bottom: 20px;
}

.demo-bar-label {
  font-size: 13px;
  font-weight: 800;
  color: #0F172A;
  display: flex;
  align-items: center;
}

.demo-bar-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.btn-demo-chip {
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 700;
  border: 1px solid transparent;
  cursor: pointer;
  transition: all 0.2s;
  background: #F8FAFC;
  color: #334155 !important;
}

.btn-demo-chip.chip-pass {
  background: #DCFCE7;
  border-color: #86EFAC;
  color: #166534 !important;
}

.btn-demo-chip.chip-suspicious {
  background: #FEF9C3;
  border-color: #FDE047;
  color: #854D0E !important;
}

.btn-demo-chip.chip-nsfw {
  background: #FEE2E2;
  border-color: #FCA5A5;
  color: #991B1B !important;
}

.btn-demo-chip.chip-prohibited {
  background: #F1F5F9;
  border-color: #CBD5E1;
  color: #475569 !important;
}

.btn-demo-chip:hover {
  transform: translateY(-1px);
  filter: brightness(0.96);
}

/* Table Enhancements */
.product-title-wrap b {
  font-size: 14px;
  color: #0F172A;
}

.ai-meta-tag {
  margin-top: 4px;
}

.ai-badge-match {
  font-size: 11px;
  font-weight: 700;
  background: #F1F5F9;
  color: #475569;
  padding: 2px 7px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.manager-reject-box {
  margin-top: 6px;
  padding: 6px 10px;
  background: #FEF2F2;
  border-left: 3px solid #EF4444;
  border-radius: 4px;
  font-size: 11.5px;
  color: #991B1B;
  line-height: 1.4;
}

.ai-flag-box {
  margin-top: 6px;
  padding: 6px 10px;
  background: #FFFBEB;
  border-left: 3px solid #F59E0B;
  border-radius: 4px;
  font-size: 11.5px;
  color: #92400E;
  line-height: 1.4;
}

.badge-status-pill.pending-ai {
  background: #FEF3C7;
  color: #B45309;
}

.badge-status-pill.rejected-edit {
  background: #FEE2E2;
  color: #DC2626;
}

.btn-rescan-pill {
  padding: 6px 14px;
  background: #DC2626;
  color: #FFFFFF !important;
  font-size: 12px;
  font-weight: 700;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 2px 6px rgba(220, 38, 38, 0.2);
}

.btn-rescan-pill:hover {
  background: #B91C1C;
  transform: translateY(-1px);
}

.pending-admin-label {
  font-size: 12px;
  color: #D97706;
  font-weight: 700;
  background: #FFFBEB;
  padding: 4px 8px;
  border-radius: 6px;
  display: inline-block;
}

/* Modal Title & Preset Chips */
.modal-lg {
  max-width: 620px !important;
}

.modal-title-with-badge {
  display: flex;
  align-items: center;
  gap: 10px;
}

.ai-shield-tag {
  font-size: 11.5px;
  font-weight: 800;
  background: #EFF6FF;
  color: #2563EB;
  padding: 3px 8px;
  border-radius: 6px;
  border: 1px solid #BFDBFE;
}

.custom-category-box {
  margin-top: 4px;
  animation: fadeInDown 0.25s ease-out;
}

.custom-cat-hint {
  font-size: 11.5px;
  color: #64748B;
  display: block;
  margin-top: 5px;
}

@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* ==================== UPLOAD TỆP HÌNH ẢNH ==================== */
.upload-dropzone {
  margin-top: 8px;
  border: 2px dashed #CBD5E1;
  border-radius: 14px;
  padding: 24px 16px;
  text-align: center;
  background: #F8FAFC;
  cursor: pointer;
  transition: all 0.25s ease;
}

.upload-dropzone:hover,
.upload-dropzone.is-dragging {
  border-color: #2563EB;
  background: #EFF6FF;
}

.dropzone-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.dropzone-icon-circle {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #E0E7FF;
  color: #4F46E5;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.dropzone-text {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.dropzone-main-text {
  font-size: 13.5px;
  font-weight: 600;
  color: #334155;
  margin: 0;
}

.text-primary-link {
  color: #2563EB;
  text-decoration: underline;
  cursor: pointer;
}

.dropzone-sub-text {
  font-size: 12px;
  color: #94A3B8;
  margin: 0;
}

/* Card hiển thị ảnh đã upload */
.image-uploaded-card {
  margin-top: 8px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  background: #FFFFFF;
  border: 1.5px solid #E2E8F0;
  border-radius: 12px;
  padding: 10px 14px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.uploaded-card-left {
  display: flex;
  align-items: center;
  gap: 12px;
  overflow: hidden;
}

.uploaded-preview-img {
  width: 54px;
  height: 54px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid #CBD5E1;
  flex-shrink: 0;
}

.uploaded-file-details {
  display: flex;
  flex-direction: column;
  gap: 3px;
  overflow: hidden;
}

.uploaded-filename {
  font-size: 13px;
  font-weight: 700;
  color: #0F172A;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 260px;
}

.uploaded-filesize {
  font-size: 11.5px;
  color: #64748B;
}

.uploaded-card-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.btn-change-image {
  background: #F1F5F9;
  border: 1px solid #CBD5E1;
  color: #334155;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-change-image:hover {
  background: #E2E8F0;
  color: #0F172A;
}

.btn-remove-image {
  background: #FEE2E2;
  border: 1px solid #FECDD3;
  color: #DC2626;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-remove-image:hover {
  background: #FCA5A5;
}

.ai-image-note {
  margin-top: 8px;
  display: flex;
  align-items: flex-start;
  gap: 8px;
  font-size: 12px;
  color: #64748B;
  line-height: 1.4;
  background: #F8FAFC;
  padding: 8px 12px;
  border-radius: 8px;
}

/* ==================== AI SCANNING OVERLAY (RADAR HUD) ==================== */
.ai-scanning-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.85);
  backdrop-filter: blur(6px);
  z-index: 999999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.ai-scan-card {
  background: #0F172A;
  border: 1px solid #334155;
  border-radius: 20px;
  padding: 36px 32px;
  width: 100%;
  max-width: 480px;
  text-align: center;
  color: #FFFFFF !important;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
}

.radar-box {
  position: relative;
  width: 120px;
  height: 120px;
  margin: 0 auto 20px auto;
  display: flex;
  align-items: center;
  justify-content: center;
}

.radar-circle {
  position: absolute;
  border-radius: 50%;
  border: 1.5px solid rgba(56, 189, 248, 0.4);
}

.circle-1 { width: 40px; height: 40px; }
.circle-2 { width: 80px; height: 80px; }
.circle-3 { width: 120px; height: 120px; }

.radar-beam {
  position: absolute;
  width: 120px;
  height: 120px;
  border-radius: 50%;
  background: conic-gradient(from 0deg, rgba(56, 189, 248, 0.4) 0deg, transparent 60deg, transparent 360deg);
  animation: spinRadar 1.5s linear infinite;
}

@keyframes spinRadar {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.radar-center-bot {
  position: relative;
  z-index: 2;
  font-size: 32px;
  color: #38BDF8;
}

.ai-scan-title {
  font-size: 18px;
  font-weight: 800;
  margin: 0 0 8px 0;
  color: #FFFFFF !important;
}

.ai-scan-step-text {
  font-size: 13px;
  color: #94A3B8 !important;
  min-height: 22px;
  margin: 0 0 16px 0;
}

.scan-progress-track {
  width: 100%;
  height: 8px;
  background: #1E293B;
  border-radius: 10px;
  overflow: hidden;
  margin-bottom: 8px;
}

.scan-progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #38BDF8, #22C55E);
  transition: width 0.4s ease;
}

.scan-percent {
  font-size: 12px;
  font-weight: 700;
  color: #38BDF8 !important;
  display: block;
  margin-bottom: 18px;
}

.ai-check-bullets {
  display: flex;
  flex-direction: column;
  gap: 8px;
  text-align: left;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 10px;
  padding: 12px 16px;
}

.check-item {
  font-size: 12px;
  color: #CBD5E1 !important;
  display: flex;
  align-items: center;
  gap: 8px;
}

/* ==================== AI SCAN RESULT MODAL ==================== */
.result-card-box {
  max-width: 540px !important;
  padding: 0 !important;
  overflow: hidden;
  border-radius: 20px !important;
}

.result-header-box {
  padding: 28px 24px 20px 24px;
  text-align: center;
}

.result-header-box.violation { background: #FEF2F2; }
.result-header-box.suspicious { background: #FFFBEB; }
.result-header-box.passed { background: #F0FDF4; }

.result-icon-badge {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  margin: 0 auto 12px auto;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 30px;
}

.result-icon-badge.danger { background: #FEE2E2; color: #DC2626; }
.result-icon-badge.warning { background: #FEF3C7; color: #D97706; }
.result-icon-badge.success { background: #DCFCE7; color: #16A34A; }

.result-title {
  font-size: 18px;
  font-weight: 800;
  margin: 0 0 6px 0;
}

.result-subtitle {
  font-size: 13px;
  color: #64748B;
  margin: 0;
}

.result-body-content {
  padding: 20px 24px;
  max-height: 380px;
  overflow-y: auto;
}

.violation-reason-panel {
  background: #FFF1F2;
  border: 1.5px solid #FECDD3;
  border-radius: 12px;
  padding: 14px;
  margin-bottom: 16px;
}

.violation-reason-panel h5 {
  font-size: 13.5px;
  font-weight: 800;
  margin: 0 0 6px 0;
}

.violation-reason-panel .reason-text {
  font-size: 13px;
  color: #9F1239;
  margin: 0;
  line-height: 1.45;
}

.penalty-status-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.penalty-badge-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.penalty-count-badge {
  font-size: 13px;
  color: #0F172A;
}

.penalty-level-tag {
  font-size: 11.5px;
  font-weight: 800;
  padding: 4px 10px;
  border-radius: 6px;
}

.tag-warn { background: #FEF3C7; color: #92400E; }
.tag-lock { background: #FEE2E2; color: #B91C1C; }
.tag-ban { background: #7F1D1D; color: #FFFFFF; }

.simulated-email-box {
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  border-radius: 10px;
  padding: 12px 14px;
}

.email-box-header {
  font-size: 12px;
  color: #334155;
  margin-bottom: 6px;
}

.email-box-body p {
  font-size: 12.5px;
  color: #475569;
  margin: 0;
  line-height: 1.45;
}

.scores-summary-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin-bottom: 16px;
}

.score-card {
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  border-radius: 12px;
  padding: 12px;
  text-align: center;
}

.score-title {
  font-size: 11.5px;
  font-weight: 700;
  color: #64748B;
  display: block;
}

.score-val {
  font-size: 26px;
  font-weight: 800;
  margin: 4px 0;
  display: block;
}

.score-sub {
  font-size: 11px;
  color: #94A3B8;
}

.suspicious-reason-box {
  background: #FFFBEB;
  border: 1px solid #FDE68A;
  border-radius: 10px;
  padding: 12px 14px;
  margin-bottom: 14px;
}

.suspicious-reason-box h5 {
  font-size: 13px;
  font-weight: 800;
  margin: 0 0 6px 0;
}

.suspicious-reason-box p {
  font-size: 12.5px;
  color: #92400E;
  margin: 0;
  line-height: 1.45;
}

.manager-handover-box,
.passed-success-box {
  display: flex;
  gap: 14px;
  align-items: flex-start;
  padding: 14px;
  border-radius: 12px;
}

.manager-handover-box {
  background: #EFF6FF;
  border: 1px solid #BFDBFE;
}

.manager-handover-box h6 {
  font-size: 13.5px;
  font-weight: 800;
  margin: 0 0 4px 0;
  color: #1E40AF;
}

.manager-handover-box p {
  font-size: 12px;
  color: #1E3A8A;
  margin: 0;
  line-height: 1.45;
}

.passed-success-box {
  background: #F0FDF4;
  border: 1px solid #BBF7D0;
}

.passed-success-box h6 {
  font-size: 13.5px;
  font-weight: 800;
  margin: 0 0 4px 0;
  color: #166534;
}

.passed-success-box p {
  font-size: 12px;
  color: #14532D;
  margin: 0;
  line-height: 1.45;
}

.result-footer-box {
  padding: 16px 24px;
  background: #F8FAFC;
  border-top: 1px solid #E2E8F0;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.btn-reset-danger {
  background: #FEE2E2;
  border: 1px solid #FECDD3;
  color: #DC2626 !important;
  font-size: 12.5px;
  font-weight: 700;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-reset-danger:hover {
  background: #FCA5A5;
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

/* ==================== SELLER REALTIME DELIVERY SUCCESS MODAL ==================== */
.seller-delivery-modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(8px);
  z-index: 99999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  animation: sellerFadeIn 0.3s ease-out;
}

.seller-delivery-modal {
  background: #ffffff;
  width: 100%;
  max-width: 520px;
  border-radius: 20px;
  box-shadow: 0 25px 60px -15px rgba(0, 0, 0, 0.4), 0 0 0 2px #22c55e;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  animation: sellerScaleIn 0.35s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.seller-modal-header {
  background: linear-gradient(135deg, #065f46, #059669);
  color: #ffffff;
  padding: 18px 20px;
  display: flex;
  align-items: center;
  gap: 14px;
}

.seller-modal-icon {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  color: #ffffff;
  flex-shrink: 0;
}

.seller-modal-title h3 {
  margin: 0;
  font-size: 17px;
  font-weight: 800;
  color: #ffffff;
}

.seller-modal-title p {
  margin: 2px 0 0;
  font-size: 13px;
  color: #a7f3d0;
}

.btn-close-seller-modal {
  margin-left: auto;
  background: rgba(255, 255, 255, 0.15);
  border: none;
  color: #ffffff;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-close-seller-modal:hover {
  background: rgba(255, 255, 255, 0.3);
}

.seller-modal-body {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-height: 60vh;
  overflow-y: auto;
}

.delivery-details-card {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.card-caption {
  font-size: 13px;
  font-weight: 700;
  color: #334155;
  display: flex;
  align-items: center;
}

.delivered-items-scroller {
  display: flex;
  flex-direction: column;
  gap: 8px;
  background: #f8fafc;
  padding: 12px;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
}

.delivered-prod-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 6px 0;
  border-bottom: 1px dashed #e2e8f0;
}

.delivered-prod-row:last-child {
  border-bottom: none;
}

.prod-thumb-img {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid #e2e8f0;
}

.prod-thumb-img.fallback {
  background: #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  color: #64748b;
}

.prod-details {
  display: flex;
  flex-direction: column;
  flex: 1;
}

.prod-name {
  font-size: 14px;
  font-weight: 700;
  color: #1e293b;
}

.prod-meta {
  font-size: 12px;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 2px;
}

.prod-qty {
  color: #059669;
}

.prod-price {
  margin-left: auto;
  font-weight: 700;
  color: #d97706;
}

.customer-delivery-info {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  padding: 10px 14px;
  border-radius: 10px;
  font-size: 13px;
}

.customer-delivery-info p {
  margin: 2px 0 0;
  font-size: 12px;
  color: #64748b;
}

.seller-modal-footer {
  padding: 14px 20px;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
}

.btn-seller-confirm {
  width: 100%;
  padding: 12px;
  background: linear-gradient(135deg, #059669, #10b981);
  color: #ffffff;
  border: none;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.35);
  transition: all 0.2s;
}

.btn-seller-confirm:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(16, 185, 129, 0.45);
}

@keyframes sellerFadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes sellerScaleIn {
  from { transform: scale(0.85); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}
</style>
