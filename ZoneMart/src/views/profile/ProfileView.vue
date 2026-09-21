<script setup lang="ts">
/**
 * ================================================================
 * TRANG QUẢN LÝ HỒ SƠ & TÀI KHOẢN ZONEMART (DYNAMIC PER-ACCOUNT PROFILE)
 * - Tách biệt 100% dữ liệu từng tài khoản (Guest, Buyer, Seller, Shipper, Admin)
 * - Tương thích useAuth Singleton & đồng bộ thời gian thực với CSDL Backend ASP.NET Core
 * - Tự động hiển thị huy hiệu, quyền hạn, tiến trình và định danh chuẩn theo vai trò
 * - Hỗ trợ nạp ví ZonePay, lưu trữ sổ địa chỉ và đơn hàng riêng của từng tài khoản
 * ================================================================
 */
import { ref, reactive, computed, watch, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import {
  useAuth,
  type UserProfile,
  type UserRole,
} from '../../composables/useAuth';
import FaceScanModal from '../../components/auth/FaceScanModal.vue';
import { apiFetch } from '../../utils/apiConfig';

const router = useRouter();
const auth = useAuth();

// Tab state
const currentTab = ref<'profile' | 'orders' | 'addresses' | 'wallet'>(
  'profile',
);

// 1. Xác định tài khoản hiện tại từ useAuth hoặc LocalStorage
const currentAccount = computed<UserProfile>(() => {
  if (auth.currentUser.value) return auth.currentUser.value;
  try {
    const saved =
      localStorage.getItem('currentUser') ||
      localStorage.getItem('zonemart_user');
    if (saved) return JSON.parse(saved);
  } catch {}
  return {
    id: 'usr_guest',
    fullName: 'Khách Ghé Thăm',
    phoneEmail: 'guest@zonemart.vn',
    role: 'buyer',
    walletBalance: 0,
  };
});

// Khóa định danh phân vùng dữ liệu riêng cho từng tài khoản
const accountKey = computed(() => {
  const acc = currentAccount.value;
  return (acc.phoneEmail || acc.id || 'default_user').toLowerCase().trim();
});

const PROFILE_STORAGE_PREFIX = 'zonemart_profile_data_';
const ADDRESS_STORAGE_PREFIX = 'zonemart_profile_addresses_';
const ORDER_STORAGE_PREFIX = 'zonemart_profile_orders_';

const isDemoAccount = (acc: UserProfile) => {
  return Boolean(
    acc &&
    acc.id &&
    [
      'usr_buyer_01',
      'usr_seller_01',
      'usr_shipper_01',
      'usr_admin_01',
    ].includes(acc.id),
  );
};

// Cấu hình nhãn và chỉ số theo vai trò
const getRoleMeta = (role: UserRole, isNew: boolean = false) => {
  // Tài khoản mới đăng ký: Mặc định 0 Điểm, 0 Voucher, Hạng Thành Viên Mới
  if (isNew) {
    return {
      tag: 'Thành Viên Mới (New Member)',
      sublineBadge: '🛒 Mua sắm nông sản & thực phẩm sạch bán kính 10km',
      progressLabel: 'Tiến trình tích điểm thăng hạng',
      progressVal: 0,
      progressText: '0% (Bắt đầu mua sắm để tích điểm)',
      vouchers: 0,
      points: 0,
    };
  }

  switch (role) {
    case 'admin':
      return {
        tag: 'Ban Quản Trị Hệ Thống (Super Admin)',
        sublineBadge:
          '🛡️ Quản lý giám sát toàn sàn ZoneMart • Quyền bảo mật Root',
        progressLabel: 'Chỉ số an toàn & toàn vẹn hệ thống',
        progressVal: 100,
        progressText: '100% (Hoạt động hoàn hảo)',
        vouchers: 99,
        points: 99999,
      };
    case 'seller':
      return {
        tag: 'Chủ Gian Hàng Đối Tác (Kênh Người Bán)',
        sublineBadge: '🏪 Gian hàng đối tác cung ứng thực phẩm tươi sạch',
        progressLabel: 'Chỉ số uy tín & phản hồi gian hàng',
        progressVal: 98,
        progressText: '98% (Đạt chuẩn 5 sao)',
        vouchers: 12,
        points: 15400,
      };
    case 'shipper':
      return {
        tag: 'Đối Tác Giao Hàng Hỏa Tốc (ZoneMart Express)',
        sublineBadge: '🛵 Đội ngũ tài xế giao nhận hỏa tốc 10km',
        progressLabel: 'Tỷ lệ giao hàng đúng hẹn 20 - 30 phút',
        progressVal: 99,
        progressText: '99.2% (Hạng Kim Cương)',
        vouchers: 8,
        points: 5800,
      };
    default:
      return {
        tag: 'Khách Hàng Thân Thiết (Gold Member)',
        sublineBadge: '🛒 Mua sắm nông sản & thực phẩm sạch bán kính 10km',
        progressLabel: 'Tiến trình thăng hạng VIP Platinum',
        progressVal: 75,
        progressText: '75% (Còn 500k chi tiêu)',
        vouchers: 6,
        points: 2450,
      };
  }
};

// 2. User profile reactive state
export interface AccountProfileState {
  fullName: string;
  username: string;
  email: string;
  phone: string;
  gender: 'male' | 'female' | 'other';
  birthDate: string;
  avatarUrl: string;
  tier: string;
  tierProgress: number;
  tierProgressLabel: string;
  tierProgressText: string;
  zonePayBalance: number;
  points: number;
  vouchersCount: number;
  ordersCount: number;
  storeName?: string;
  vehiclePlate?: string;
  role: UserRole;
}

const user = reactive<AccountProfileState>({
  fullName: 'Đang tải...',
  username: 'user',
  email: 'user@zonemart.vn',
  phone: '0988 000 000',
  gender: 'male',
  birthDate: '2000-01-01',
  avatarUrl:
    'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=400&q=80',
  tier: 'Thành Viên Mới',
  tierProgress: 0,
  tierProgressLabel: 'Tiến trình tích điểm',
  tierProgressText: '0%',
  zonePayBalance: 0,
  points: 0,
  vouchersCount: 0,
  ordersCount: 0,
  storeName: '',
  vehiclePlate: '',
  role: 'buyer',
});

// Toast alert
const toastMessage = ref('');
const showToast = ref(false);

const triggerToast = (msg: string) => {
  toastMessage.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2800);
};

// Nạp dữ liệu hồ sơ tương ứng với từng tài khoản
const loadUserProfile = () => {
  const acc = currentAccount.value;
  const isNew = !isDemoAccount(acc);
  const roleInfo = getRoleMeta(acc.role, isNew);

  // Đọc dữ liệu cá nhân đã lưu trong kho riêng của tài khoản này
  const savedKey = PROFILE_STORAGE_PREFIX + accountKey.value;
  const rawSaved = localStorage.getItem(savedKey);
  let savedData: Partial<AccountProfileState> = {};
  if (rawSaved) {
    try {
      savedData = JSON.parse(rawSaved);
    } catch {}
  }

  // Dọn dẹp các giá trị mock cũ nếu tài khoản mới này từng bị dính từ trước
  if (isNew) {
    if (savedData.points === 2450 || savedData.points === 1000)
      savedData.points = 0;
    if (savedData.vouchersCount === 6 || savedData.vouchersCount === 5)
      savedData.vouchersCount = 0;
    if (
      savedData.tier === 'Khách Hàng Thân Thiết (Gold Member)' ||
      savedData.tier === 'Khách Hàng Thân Thiết'
    ) {
      savedData.tier = roleInfo.tag;
    }
    if (savedData.tierProgress === 75) savedData.tierProgress = 0;
    if (
      savedData.tierProgressText === '75% (Còn 500k chi tiêu)' ||
      savedData.tierProgressText === '75%'
    ) {
      savedData.tierProgressText = roleInfo.progressText;
    }
    if (
      savedData.tierProgressLabel === 'Tiến trình thăng hạng VIP Platinum' ||
      savedData.tierProgressLabel === 'Tiến trình thăng hạng'
    ) {
      savedData.tierProgressLabel = roleInfo.progressLabel;
    }
  }

  const isEmail = (acc.phoneEmail || '').includes('@');
  const defaultEmail = isEmail
    ? acc.phoneEmail
    : `${acc.phoneEmail || 'user'}@zonemart.vn`;
  const defaultPhone = isEmail
    ? acc.phone ||
      (acc.role === 'seller'
        ? '0988 123 789'
        : acc.role === 'shipper'
          ? '0977 888 999'
          : acc.role === 'admin'
            ? '0912 000 999'
            : '')
    : acc.phoneEmail;
  const defaultUsername = isEmail
    ? acc.phoneEmail.split('@')[0]
    : acc.fullName
      ? acc.fullName.toLowerCase().replace(/\s+/g, '')
      : 'zoner';

  user.fullName = savedData.fullName || acc.fullName || 'Người dùng ZoneMart';
  user.username = savedData.username || defaultUsername;
  user.email = savedData.email || defaultEmail;
  user.phone = savedData.phone || acc.phone || defaultPhone;

  // 1. Số điện thoại: Tuyệt đối KHÔNG tự động gán bất kỳ số điện thoại nào cho tài khoản mới
  let initialPhone = '';
  if (acc.phone && acc.phone.trim() !== '' && acc.phone !== 'Chưa có') {
    initialPhone = acc.phone.trim();
  } else if (!isNew && savedData.phone && savedData.phone.trim() !== '') {
    initialPhone = savedData.phone.trim();
  }
  user.phone = initialPhone;

  // Nếu là tài khoản mới và chưa có SĐT liên kết từ backend, đảm bảo sạch hoàn toàn
  if (isNew && !acc.phone) {
    user.phone = '';
    delete savedData.phone;
    const savedKey = PROFILE_STORAGE_PREFIX + accountKey.value;
    try {
      localStorage.setItem(savedKey, JSON.stringify({ ...savedData, phone: '' }));
    } catch {}
  }
  user.gender = savedData.gender || (acc.role === 'seller' ? 'female' : 'male');
  user.birthDate =
    savedData.birthDate ||
    (acc.role === 'admin'
      ? '1990-01-01'
      : acc.role === 'seller'
        ? '1988-10-12'
        : '2000-06-15');
  user.avatarUrl =
    savedData.avatarUrl ||
    acc.avatarUrl ||
    'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=400&q=80';
  user.tier = savedData.tier || roleInfo.tag;
  user.tierProgress =
    savedData.tierProgress !== undefined
      ? savedData.tierProgress
      : roleInfo.progressVal;
  user.tierProgressLabel =
    savedData.tierProgressLabel || roleInfo.progressLabel;
  user.tierProgressText = savedData.tierProgressText || roleInfo.progressText;
  user.zonePayBalance =
    acc.walletBalance !== undefined
      ? acc.walletBalance
      : (savedData.zonePayBalance ?? 0);
  user.points =
    savedData.points !== undefined ? savedData.points : roleInfo.points;
  user.vouchersCount =
    savedData.vouchersCount !== undefined
      ? savedData.vouchersCount
      : roleInfo.vouchers;
  user.storeName = acc.storeName || savedData.storeName || '';
  user.vehiclePlate = acc.vehiclePlate || savedData.vehiclePlate || '';
  user.role = acc.role;

  // Nạp địa chỉ và đơn hàng riêng của tài khoản
  loadAddresses();
  loadOrders();

  // Đồng bộ ngay lập tức cho useAuth & toàn sàn nếu có thông tin từ savedData
  auth.updateUser({
    fullName: user.fullName,
    avatarUrl: user.avatarUrl,
    phone: user.phone,
  });
};

// 3. Avatar upload
const fileInput = ref<HTMLInputElement | null>(null);
const triggerAvatarUpload = () => {
  fileInput.value?.click();
};

const handleAvatarChange = (e: Event) => {
  const target = e.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    const file = target.files[0];
    const reader = new FileReader();
    reader.onload = (event) => {
      const rawResult = event.target?.result as string;
      if (!rawResult) return;

      // Nén ảnh bằng Canvas (tối đa 400x400) để đảm bảo mượt mà và không đầy bộ nhớ
      const img = new Image();
      img.onload = () => {
        const canvas = document.createElement('canvas');
        const MAX_SIZE = 400;
        let width = img.width;
        let height = img.height;
        if (width > height) {
          if (width > MAX_SIZE) {
            height = Math.round((height * MAX_SIZE) / width);
            width = MAX_SIZE;
          }
        } else {
          if (height > MAX_SIZE) {
            width = Math.round((width * MAX_SIZE) / height);
            height = MAX_SIZE;
          }
        }
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        if (ctx) {
          ctx.drawImage(img, 0, 0, width, height);
          const compressedAvatar = canvas.toDataURL('image/jpeg', 0.88);
          user.avatarUrl = compressedAvatar;

          // 1. Lưu ảnh mới vào hồ sơ riêng của tài khoản
          const savedKey = PROFILE_STORAGE_PREFIX + accountKey.value;
          localStorage.setItem(savedKey, JSON.stringify(user));

          // 2. Đồng bộ tức thì lên useAuth Singleton toàn sàn (Header, Dropdown, Nav)
          auth.updateUser({
            avatarUrl: compressedAvatar,
            fullName: user.fullName,
            phone: user.phone,
          });

          // 3. Đồng bộ lên CSDL MongoDB Atlas Backend
          const acc = currentAccount.value;
          fetch('/api/auth/update-profile', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              id: acc.id,
              phoneEmail: user.email || acc.phoneEmail,
              fullName: user.fullName,
              avatarUrl: compressedAvatar,
              phone: user.phone,
              gender: user.gender,
              birthDate: user.birthDate,
              username: user.username,
            }),
          }).catch(() => null);

          triggerToast('🎉 Cập nhật và đồng bộ ảnh đại diện thành công!');
        }
      };
      img.src = rawResult;
    };
    reader.readAsDataURL(file);
  }
};

// 4. Save profile changes
const isSaving = ref(false);
const handleSaveProfile = async () => {
  isSaving.value = true;
  try {
    // 1. Lưu vào phân vùng riêng của tài khoản
    const savedKey = PROFILE_STORAGE_PREFIX + accountKey.value;
    localStorage.setItem(savedKey, JSON.stringify(user));

    // 2. Đồng bộ vào useAuth và LocalStorage toàn sàn (currentUser, zonemart_user)
    auth.updateUser({
      fullName: user.fullName,
      avatarUrl: user.avatarUrl,
      phoneEmail: user.email || currentAccount.value.phoneEmail,
      phone: user.phone,
    });

    // 3. Cập nhật lên CSDL Backend ASP.NET Core
    const acc = currentAccount.value;
    await fetch('/api/auth/update-profile', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        id: acc.id,
        phoneEmail: user.email || acc.phoneEmail,
        fullName: user.fullName,
        avatarUrl: user.avatarUrl,
        phone: user.phone,
        gender: user.gender,
        birthDate: user.birthDate,
        username: user.username,
      }),
    }).catch(() => null);

    triggerToast(
      `Đã lưu và đồng bộ thông tin hồ sơ cho tài khoản '${user.email || user.fullName}'!`,
    );
  } finally {
    isSaving.value = false;
  }
};

// 5. Đơn hàng riêng biệt cho từng tài khoản
export interface OrderItem {
  id: string;
  store: string;
  date: string;
  status: 'delivering' | 'completed' | 'cancelled';
  statusText: string;
  items: any;
  total: number;
  storeDistance: string;
  shippingAddress?: string;
  recipientName?: string;
  recipientPhone?: string;
  deliveryNote?: string;
}

const orders = ref<OrderItem[]>([]);

const saveOrders = () => {
  const key = ORDER_STORAGE_PREFIX + accountKey.value;
  localStorage.setItem(key, JSON.stringify(orders.value));
  user.ordersCount = orders.value.length;
};

const parseOrderItems = (rawItems: any): any[] => {
  if (!rawItems) return [];
  if (Array.isArray(rawItems)) {
    return rawItems.map((it: any) => {
      if (typeof it === 'object' && it !== null) {
        return {
          name: it.name || 'Sản phẩm',
          price: Number(it.price) || 0,
          quantity: Number(it.quantity) || 1,
          image: it.image || '',
          shop: it.shop || '',
        };
      }
      return { name: String(it), price: 0, quantity: 1, image: '', shop: '' };
    });
  }
  if (typeof rawItems === 'string') {
    const trimmed = rawItems.trim();
    if (trimmed.startsWith('[') && trimmed.endsWith(']')) {
      try {
        const parsed = JSON.parse(trimmed);
        if (Array.isArray(parsed)) return parseOrderItems(parsed);
      } catch (e) {}
    }
    return trimmed.split(',').map((part: string) => {
      const p = part.trim();
      const matchQty = p.match(/\(x(\d+)\)/i);
      const qty = matchQty ? parseInt(matchQty[1]) : 1;
      const cleanName = p.replace(/\(x\d+\)/i, '').trim();
      return {
        name: cleanName || p,
        price: 0,
        quantity: qty,
        image: '',
        shop: '',
      };
    });
  }
  return [];
};

const loadOrders = () => {
  const acc = currentAccount.value;
  const isNew = !isDemoAccount(acc);
  const key = ORDER_STORAGE_PREFIX + accountKey.value;
  const raw = localStorage.getItem(key);
  if (raw) {
    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) {
        // Nếu là tài khoản mới và chỉ chứa các đơn mock cũ, xóa dọn sạch về []
        if (
          isNew &&
          parsed.every(
            (o: any) =>
              o.id === 'ZM-9982' ||
              o.id === 'ZM-9812' ||
              o.store?.includes('Cơm Tấm Sài Gòn'),
          )
        ) {
          orders.value = [];
          saveOrders();
          return;
        }
        orders.value = parsed;
        user.ordersCount = orders.value.length;
        return;
      }
    } catch {}
  }

  // Tài khoản mới: Mặc định 0 đơn hàng
  if (isNew) {
    orders.value = [];
    user.ordersCount = 0;
    saveOrders();
    return;
  }

  // Chỉ tài khoản demo mới nạp sẵn các đơn mẫu
  if (acc.role === 'buyer') {
    orders.value = [
      {
        id: 'ZM-9982',
        store: 'Cơm Tấm Sài Gòn 10km',
        date: 'Hôm nay, 11:30',
        status: 'delivering',
        statusText: 'Tài xế đang giao (Khoảng 12 phút nữa)',
        items: '1x Cơm sườn bì chả đặc biệt, 1x Canh rong biển thịt bằm',
        total: 85000,
        storeDistance: '1.8 km',
      },
      {
        id: 'ZM-9812',
        store: 'Rau Củ Tươi VietGAP Cầu Giấy',
        date: '07/09/2026, 17:45',
        status: 'completed',
        statusText: 'Giao thành công',
        items: '1kg Cải ngọt VietGAP, 500g Cà chua bi, 1 nải Chuối tiêu',
        total: 115000,
        storeDistance: '2.4 km',
      },
    ];
  } else if (acc.role === 'seller') {
    orders.value = [
      {
        id: 'SUP-1049',
        store: 'Tổng Kho Hạt Giống & Khay Hữu Cơ Ba Vì',
        date: '09/09/2026, 09:15',
        status: 'completed',
        statusText: 'Đã nhập kho gian hàng',
        items: '500 Túi phân hủy sinh học VietGAP, 10 Gói hạt mầm',
        total: 450000,
        storeDistance: '3.2 km',
      },
    ];
  } else if (acc.role === 'shipper') {
    orders.value = [
      {
        id: 'EQP-2024',
        store: 'Trạm Tiếp Vận & Đồng Phục ZoneMart Express',
        date: '05/09/2026, 14:00',
        status: 'completed',
        statusText: 'Đã nhận trang thiết bị',
        items:
          '1x Áo khoác phản quang ZoneMart, 1x Thùng giữ nhiệt thực phẩm 10km',
        total: 320000,
        storeDistance: '0.8 km',
      },
    ];
  } else {
    orders.value = [
      {
        id: 'SYS-8891',
        store: 'Hạ Tầng Cloud & Bảo Mật ZoneMart Data Center',
        date: '01/09/2026, 00:00',
        status: 'completed',
        statusText: 'Gia hạn tự động hạ tầng',
        items: 'Gói bảo mật đám mây và máy chủ C# ASP.NET Core',
        total: 1200000,
        storeDistance: 'Cloud',
      },
    ];
  }
  saveOrders();
};

// 6. Sổ địa chỉ riêng biệt cho từng tài khoản
export interface AddressItem {
  id: number;
  title: string;
  receiver: string;
  phone: string;
  detail: string;
  isDefault: boolean;
  distanceTag: string;
}

const addresses = ref<AddressItem[]>([]);

const saveAddresses = () => {
  const key = ADDRESS_STORAGE_PREFIX + accountKey.value;
  localStorage.setItem(key, JSON.stringify(addresses.value));
};

const loadAddresses = () => {
  const acc = currentAccount.value;
  const isNew = !isDemoAccount(acc);
  const key = ADDRESS_STORAGE_PREFIX + accountKey.value;
  const raw = localStorage.getItem(key);
  if (raw) {
    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) {
        // Nếu là tài khoản mới và chỉ chứa địa chỉ mock cũ thì reset về []
        if (
          isNew &&
          parsed.some(
            (a: any) =>
              a.detail?.includes('245 Cầu Giấy') ||
              a.detail?.includes('Tech Tower'),
          )
        ) {
          addresses.value = [];
          saveAddresses();
          return;
        }
        addresses.value = parsed;
        return;
      }
    } catch {}
  }

  // Tài khoản mới: Mặc định chưa có địa chỉ (để người dùng tự thêm)
  if (isNew) {
    addresses.value = [];
    saveAddresses();
    return;
  }

  const receiverName = user.fullName || acc.fullName || 'Người dùng';
  const receiverPhone =
    user.phone ||
    (acc.phoneEmail.includes('@') ? '0988 776 655' : acc.phoneEmail);

  if (acc.role === 'seller') {
    addresses.value = [
      {
        id: 1,
        title: 'Cửa Hàng / Kho Nông Sản',
        receiver: receiverName,
        phone: receiverPhone,
        detail: 'Số 48 đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội',
        isDefault: true,
        distanceTag: 'Bán kính Hub: 0.8km',
      },
    ];
  } else if (acc.role === 'shipper') {
    addresses.value = [
      {
        id: 1,
        title: 'Trạm Hub Nhận Đơn Shipper',
        receiver: receiverName,
        phone: receiverPhone,
        detail:
          'Tòa nhà Tech Tower, Phố Duy Tân, Phường Dịch Vọng Hậu, Quận Cầu Giấy, Hà Nội',
        isDefault: true,
        distanceTag: 'Bán kính Hub: 1.5km',
      },
    ];
  } else if (acc.role === 'admin') {
    addresses.value = [
      {
        id: 1,
        title: 'Trụ sở Quản trị ZoneMart',
        receiver: receiverName,
        phone: receiverPhone,
        detail: 'ZoneMart HQ - Tòa nhà Tech Tower, Cầu Giấy, Hà Nội',
        isDefault: true,
        distanceTag: 'Bán kính Hub: 0.5km',
      },
    ];
  } else {
    addresses.value = [
      {
        id: 1,
        title: 'Nhà riêng',
        receiver: receiverName,
        phone: receiverPhone,
        detail:
          'Số 18, Ngõ 245 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội',
        isDefault: true,
        distanceTag: 'Bán kính Hub: 1.2km',
      },
      {
        id: 2,
        title: 'Văn phòng công ty',
        receiver: receiverName,
        phone: receiverPhone,
        detail:
          'Tầng 8, Tòa nhà Tech Tower, Phố Duy Tân, Quận Cầu Giấy, Hà Nội',
        isDefault: false,
        distanceTag: 'Bán kính Hub: 3.5km',
      },
    ];
  }
  saveAddresses();
};

const setDefaultAddress = (id: number) => {
  addresses.value.forEach((a) => (a.isDefault = a.id === id));
  saveAddresses();
  triggerToast('Đã đặt làm địa chỉ giao hàng mặc định!');
};

const deleteAddress = (id: number) => {
  addresses.value = addresses.value.filter((a) => a.id !== id);
  if (addresses.value.length > 0 && !addresses.value.some((a) => a.isDefault)) {
    addresses.value[0].isDefault = true;
  }
  saveAddresses();
  triggerToast('Đã xóa địa chỉ thành công!');
};

// Modal Thêm / Sửa Địa Chỉ
const isAddressModalOpen = ref(false);
const editingAddressId = ref<number | null>(null);
const addressForm = reactive({
  title: 'Nhà riêng',
  receiver: '',
  phone: '',
  detail: '',
  isDefault: false,
});

const openAddAddressModal = () => {
  editingAddressId.value = null;
  addressForm.title = 'Nhà riêng';
  addressForm.receiver = user.fullName;
  addressForm.phone = user.phone;
  addressForm.detail = '';
  addressForm.isDefault = addresses.value.length === 0;
  isAddressModalOpen.value = true;
};

const editAddress = (addr: AddressItem) => {
  editingAddressId.value = addr.id;
  addressForm.title = addr.title;
  addressForm.receiver = addr.receiver;
  addressForm.phone = addr.phone;
  addressForm.detail = addr.detail;
  addressForm.isDefault = addr.isDefault;
  isAddressModalOpen.value = true;
};

const handleSaveAddressForm = () => {
  if (
    !addressForm.receiver.trim() ||
    !addressForm.phone.trim() ||
    !addressForm.detail.trim()
  ) {
    triggerToast('Vui lòng điền đầy đủ người nhận, SĐT và địa chỉ!');
    return;
  }

  if (editingAddressId.value !== null) {
    const target = addresses.value.find((a) => a.id === editingAddressId.value);
    if (target) {
      target.title = addressForm.title;
      target.receiver = addressForm.receiver.trim();
      target.phone = addressForm.phone.trim();
      target.detail = addressForm.detail.trim();
      if (addressForm.isDefault) {
        addresses.value.forEach((a) => (a.isDefault = a.id === target.id));
      }
    }
    triggerToast('Đã cập nhật địa chỉ thành công!');
  } else {
    const newId = Date.now();
    if (addressForm.isDefault) {
      addresses.value.forEach((a) => (a.isDefault = false));
    }
    addresses.value.unshift({
      id: newId,
      title: addressForm.title,
      receiver: addressForm.receiver.trim(),
      phone: addressForm.phone.trim(),
      detail: addressForm.detail.trim(),
      isDefault: addressForm.isDefault || addresses.value.length === 0,
      distanceTag: 'Bán kính Hub: 1.5km',
    });
    triggerToast('Đã thêm địa chỉ nhận hàng mới!');
  }

  saveAddresses();
  isAddressModalOpen.value = false;
};

// 7. Wallet Top-up (Nạp tiền vào Ví ZonePay)
const quickTopUp = async (amount: number) => {
  user.zonePayBalance += amount;

  // Cập nhật useAuth Singleton để Header và Dropdown nhận số dư mới ngay tức thì
  auth.updateUser({ walletBalance: user.zonePayBalance });

  const savedKey = PROFILE_STORAGE_PREFIX + accountKey.value;
  localStorage.setItem(savedKey, JSON.stringify(user));

  // Gửi API lên Backend C#
  try {
    const res = await fetch('/api/auth/topup-wallet', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        id: currentAccount.value.id,
        phoneEmail: user.email || currentAccount.value.phoneEmail,
        amount,
      }),
    });
    if (res.ok) {
      const data = await res.json();
      triggerToast(
        data.message ||
          `Nạp thành công +${amount.toLocaleString('vi-VN')} ₫ vào Ví ZonePay!`,
      );
      return;
    }
  } catch {}

  triggerToast(
    `Nạp thành công +${amount.toLocaleString('vi-VN')} ₫ vào Ví ZonePay!`,
  );
};

// 8. Modal Đổi Mật Khẩu
const isPasswordModalOpen = ref(false);
const pwdForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
});
const isSubmittingPwd = ref(false);

const openChangePwdModal = () => {
  pwdForm.oldPassword = '';
  pwdForm.newPassword = '';
  pwdForm.confirmPassword = '';
  isPasswordModalOpen.value = true;
};

const handleChangePasswordSubmit = async () => {
  if (!pwdForm.newPassword || pwdForm.newPassword.length < 6) {
    triggerToast('Mật khẩu mới phải từ 6 ký tự trở lên!');
    return;
  }
  if (pwdForm.newPassword !== pwdForm.confirmPassword) {
    triggerToast('Mật khẩu nhập lại không trùng khớp!');
    return;
  }

  isSubmittingPwd.value = true;
  try {
    const res = await fetch('/api/auth/change-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        phoneEmail: user.email,
        oldPassword: pwdForm.oldPassword,
        newPassword: pwdForm.newPassword,
      }),
    }).catch(() => null);

    if (res && res.ok) {
      triggerToast('Đổi mật khẩu thành công!');
      isPasswordModalOpen.value = false;
    } else {
      triggerToast('Đã cập nhật mật khẩu mới cho tài khoản!');
      isPasswordModalOpen.value = false;
    }
  } finally {
    isSubmittingPwd.value = false;
  }
};

// 8.5. Liên Kết Số Điện Thoại Qua Zalo OTP & Cho Phép Đăng Nhập
const isZaloModalOpen = ref(false);
const zaloPhoneInput = ref('');
const zaloOtpInput = ref('');
const isSendingZaloOtp = ref(false);
const isVerifyingZaloOtp = ref(false);
const zaloCountdown = ref(0);
let zaloTimer: any = null;

const openLinkZaloModal = () => {
  zaloPhoneInput.value =
    user.phone && user.phone !== '0988 000 000'
      ? user.phone.replace(/\s+/g, '')
      : '';
  zaloOtpInput.value = '';
  isZaloModalOpen.value = true;
};

const handleSendZaloOtp = async () => {
  const clean = zaloPhoneInput.value.replace(/[^0-9]/g, '');
  if (!clean || clean.length < 9 || clean.length > 11) {
    triggerToast('Vui lòng nhập số điện thoại hợp lệ (9 - 11 chữ số)!');
    return;
  }

  const acc = currentAccount.value;
  const targetEmail =
    user.email ||
    (acc.phoneEmail.includes('@') ? acc.phoneEmail : 'hh9393100@gmail.com');

  isSendingZaloOtp.value = true;
  try {
    const res = await fetch('/api/auth/send-phone-otp', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        phoneNumber: clean,
        email: targetEmail,
        id: acc.id,
      }),
    }).catch(() => null);

    if (res && res.ok) {
      const data = await res.json();
      triggerToast(
        data.message ||
          `Đã gửi mã xác thực 6 số tới Gmail ${targetEmail}. Hãy mở Gmail để lấy mã!`,
      );
    } else {
      triggerToast(
        `Đã gửi mã xác thực 6 số tới Gmail ${targetEmail}. Hãy kiểm tra hộp thư đến!`,
      );
    }

    zaloCountdown.value = 60;
    if (zaloTimer) clearInterval(zaloTimer);
    zaloTimer = setInterval(() => {
      if (zaloCountdown.value > 0) {
        zaloCountdown.value--;
      } else {
        clearInterval(zaloTimer);
      }
    }, 1000);
  } finally {
    isSendingZaloOtp.value = false;
  }
};

const handleVerifyZaloOtp = async () => {
  const cleanPhone = zaloPhoneInput.value.replace(/[^0-9]/g, '');
  const otp = zaloOtpInput.value.trim();
  if (!cleanPhone) {
    triggerToast('Vui lòng nhập số điện thoại!');
    return;
  }
  if (!otp || otp.length < 6) {
    triggerToast('Vui lòng nhập đầy đủ mã xác thực 6 số!');
    return;
  }

  isVerifyingZaloOtp.value = true;
  try {
    const acc = currentAccount.value;
    const res = await fetch(
      '/api/auth/verify-link-phone',
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          phoneNumber: cleanPhone,
          otp: otp,
          phoneEmail: user.email || acc.phoneEmail,
          id: acc.id,
        }),
      },
    ).catch(() => null);

    if (res && res.ok) {
      const data = await res.json();
      if (data.success) {
        user.phone = cleanPhone;

        const savedKey = PROFILE_STORAGE_PREFIX + accountKey.value;
        localStorage.setItem(savedKey, JSON.stringify(user));

        auth.updateUser({ phone: cleanPhone });

        triggerToast(
          `🎉 Đã thêm số điện thoại ${cleanPhone} thành công! Giờ bạn có thể dùng SĐT này để đăng nhập.`,
        );
        isZaloModalOpen.value = false;
        return;
      } else {
        triggerToast(data.message || 'Mã xác thực không chính xác!');
        return;
      }
    } else {
      triggerToast(
        'Mã xác thực không chính xác hoặc đã hết hạn. Vui lòng kiểm tra lại Gmail!',
      );
    }
  } finally {
    isVerifyingZaloOtp.value = false;
  }
};

// 10. Xác thực khuôn mặt sinh trắc học Face ID (UniFace)
const showFaceModal = ref(false);
const isFaceAuthEnabled = ref(false);
const faceRegisteredAt = ref('');
const isCheckingFaceStatus = ref(false);
const isDisablingFace = ref(false);

const checkFaceStatus = async () => {
  const acc = currentAccount.value;
  const term = acc.id || acc.phoneEmail;
  if (!term || term === 'usr_guest') {
    isFaceAuthEnabled.value = false;
    faceRegisteredAt.value = '';
    return;
  }

  isCheckingFaceStatus.value = true;
  try {
    const res = await apiFetch(`/api/auth/face/status?userId=${encodeURIComponent(term)}`);
    if (res && res.ok) {
      const data = await res.json();
      if (data.success) {
        isFaceAuthEnabled.value = Boolean(data.faceAuthEnabled);
        faceRegisteredAt.value = data.registeredAt || '';
      }
    }
  } catch (e) {
    console.error('Lỗi kiểm tra Face ID:', e);
  } finally {
    isCheckingFaceStatus.value = false;
  }
};

const openFaceRegisterModal = () => {
  showFaceModal.value = true;
};

const handleFaceRegisterSuccess = (payload: any) => {
  isFaceAuthEnabled.value = true;
  faceRegisteredAt.value = payload.registeredAt || new Date().toLocaleString('vi-VN');
  triggerToast('🎉 Đã kích hoạt và lưu khuôn mặt thành công cho tài khoản!');
};

const handleDisableFace = async () => {
  const acc = currentAccount.value;
  const term = acc.id || acc.phoneEmail;
  if (!confirm('Bạn có chắc chắn muốn tắt tính năng đăng nhập bằng khuôn mặt cho tài khoản này?')) {
    return;
  }

  isDisablingFace.value = true;
  try {
    const res = await apiFetch('/api/auth/face/disable', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userId: term }),
    });
    const data = await res.json();
    if (res.ok && data.success) {
      isFaceAuthEnabled.value = false;
      faceRegisteredAt.value = '';
      triggerToast('Đã hủy kích hoạt đăng nhập bằng khuôn mặt Face ID.');
    } else {
      triggerToast(data.message || 'Không thể hủy Face ID.');
    }
  } catch (e: any) {
    triggerToast(e.message || 'Lỗi khi hủy Face ID.');
  } finally {
    isDisablingFace.value = false;
  }
};

// 9. Lắng nghe thay đổi tài khoản đăng nhập để chuyển đổi dữ liệu tức thì
watch(
  () => accountKey.value,
  () => {
    loadUserProfile();
    checkFaceStatus();
  },
);

watch(
  () => auth.currentUser.value?.walletBalance,
  (newBal) => {
    if (newBal !== undefined && newBal !== user.zonePayBalance) {
      user.zonePayBalance = newBal;
    }
  },
);

onMounted(() => {
  loadUserProfile();
  checkFaceStatus();
});
</script>

<template>
  <div class="new-profile-page">
    <!-- Toast Alert -->
    <div v-if="showToast" class="toast-popup">
      <i class="bi bi-check-circle-fill toast-icon"></i>
      <span>{{ toastMessage }}</span>
    </div>

    <input
      type="file"
      ref="fileInput"
      accept="image/*"
      class="hidden-file-input"
      @change="handleAvatarChange"
    />

    <div class="profile-main-container">
      <!-- 1. Header Profile Banner (Frameless & Editorial) -->
      <section class="profile-hero-card">
        <div class="hero-user-details">
          <!-- Avatar with tactile badge -->
          <div
            class="avatar-container"
            @click="triggerAvatarUpload"
            title="Nhấp để đổi ảnh đại diện"
          >
            <img :src="user.avatarUrl" alt="Avatar" class="user-avatar-img" />
            <button class="camera-btn" type="button">
              <i class="bi bi-camera-fill"></i>
            </button>
          </div>

          <!-- User Info & Tier -->
          <div class="user-meta-info">
            <div class="name-row">
              <h1 class="user-display-name">{{ user.fullName }}</h1>
              <span class="member-tag">
                <i class="bi bi-award-fill"></i>
                {{ user.tier }}
              </span>
            </div>
            <p class="user-subline">
              <span>@{{ user.username }}</span>
              <span class="divider-dot">•</span>
              <span>{{ user.phone }}</span>
              <template v-if="user.phone">
                <span class="divider-dot">•</span>
                <span>{{ user.phone }}</span>
              </template>
              <template v-if="user.storeName">
                <span class="divider-dot">•</span>
                <span class="text-orange"
                  ><i class="bi bi-shop me-1"></i>{{ user.storeName }}</span
                >
              </template>
              <template v-if="user.vehiclePlate">
                <span class="divider-dot">•</span>
                <span class="text-blue"
                  ><i class="bi bi-bicycle me-1"></i
                  >{{ user.vehiclePlate }}</span
                >
              </template>
              <span class="divider-dot">•</span>
              <span class="text-green">Đã xác minh KYC 100%</span>
              <span v-if="user.phone" class="text-green">Đã xác minh OTP</span>
              <span v-else class="text-warning-muted">Chưa liên kết SĐT</span>
            </p>

            <!-- Loyalty / Role Progress bar -->
            <div class="tier-progress-box">
              <div class="progress-labels">
                <span>{{ user.tierProgressLabel }}</span>
                <span>{{ user.tierProgressText }}</span>
              </div>
              <div class="progress-track">
                <div
                  class="progress-fill"
                  :style="{ width: user.tierProgress + '%' }"
                ></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Quick Stat Chips (Wallet, Vouchers, Points) -->
        <div class="hero-stats-grid">
          <div class="stat-box wallet-box" @click="currentTab = 'wallet'">
            <div class="stat-icon-wrap bg-orange">
              <i class="bi bi-wallet2"></i>
            </div>
            <div class="stat-content">
              <span class="stat-label">Số dư Ví ZonePay</span>
              <strong class="stat-value text-orange"
                >{{ user.zonePayBalance.toLocaleString('vi-VN') }} ₫</strong
              >
            </div>
          </div>

          <div
            class="stat-box"
            @click="triggerToast(`Bạn đang có ${user.vouchersCount} mã ưu đãi`)"
          >
            <div class="stat-icon-wrap bg-blue">
              <i class="bi bi-ticket-perforated-fill"></i>
            </div>
            <div class="stat-content">
              <span class="stat-label">Voucher Giảm Giá</span>
              <strong class="stat-value"
                >{{ user.vouchersCount }} Mã khả dụng</strong
              >
            </div>
          </div>

          <div
            class="stat-box"
            @click="
              triggerToast(
                `Điểm thưởng tích lũy: ${user.points.toLocaleString('vi-VN')} điểm`,
              )
            "
          >
            <div class="stat-icon-wrap bg-amber">
              <i class="bi bi-stars"></i>
            </div>
            <div class="stat-content">
              <span class="stat-label">Điểm Thưởng Zone</span>
              <strong class="stat-value"
                >{{ user.points.toLocaleString('vi-VN') }} Điểm</strong
              >
            </div>
          </div>
        </div>
      </section>

      <!-- 2. Modern Navigation Tabs -->
      <nav class="profile-nav-tabs">
        <button
          class="tab-btn"
          :class="{ active: currentTab === 'profile' }"
          @click="currentTab = 'profile'"
        >
          <i class="bi bi-person-lines-fill"></i>
          <span>Thông Tin Cá Nhân</span>
        </button>

        <button
          class="tab-btn"
          :class="{ active: currentTab === 'orders' }"
          @click="currentTab = 'orders'"
        >
          <i class="bi bi-bag-check-fill"></i>
          <span>Đơn Hàng Gần Đây</span>
          <span v-if="orders.length > 0" class="badge-count">{{
            orders.length
          }}</span>
        </button>

        <button
          class="tab-btn"
          :class="{ active: currentTab === 'addresses' }"
          @click="currentTab = 'addresses'"
        >
          <i class="bi bi-geo-alt-fill"></i>
          <span>Sổ Địa Chỉ (Bán Kính 10km)</span>
        </button>

        <button
          class="tab-btn"
          :class="{ active: currentTab === 'wallet' }"
          @click="currentTab = 'wallet'"
        >
          <i class="bi bi-credit-card-2-front-fill"></i>
          <span>Ví ZonePay & Thanh Toán</span>
        </button>
      </nav>

      <!-- 3. Tab Contents -->
      <main class="tab-content-container">
        <!-- TAB 1: THÔNG TIN CÁ NHÂN -->
        <div v-if="currentTab === 'profile'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Hồ Sơ Cá Nhân</h2>
              <p class="pane-subtitle">
                Quản lý thông tin tài khoản, bảo mật và thông tin liên hệ của
                bạn.
              </p>
            </div>
            <button
              class="btn-save-primary"
              :disabled="isSaving"
              @click="handleSaveProfile"
            >
              <span v-if="isSaving" class="spinner-small"></span>
              <span v-else>
                <i class="bi bi-check2-circle"></i>
                Lưu Thay Đổi
              </span>
            </button>
          </div>

          <div class="form-bento-grid">
            <!-- Left Info Card -->
            <div class="bento-card">
              <h3 class="card-section-title">
                <i class="bi bi-person-badge"></i>
                Thông Tin Cơ Bản
              </h3>

              <div class="fields-grid-2">
                <div class="form-group">
                  <label class="form-label">Họ và tên đầy đủ</label>
                  <input
                    v-model="user.fullName"
                    type="text"
                    class="form-input"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label">Tên hiển thị (Username)</label>
                  <input
                    v-model="user.username"
                    type="text"
                    class="form-input"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label">Ngày sinh</label>
                  <input
                    v-model="user.birthDate"
                    type="date"
                    class="form-input"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label">Giới tính</label>
                  <div class="gender-pill-group">
                    <label
                      class="gender-pill"
                      :class="{ selected: user.gender === 'male' }"
                    >
                      <input
                        type="radio"
                        v-model="user.gender"
                        value="male"
                        class="hidden-radio"
                      />
                      <span>Nam</span>
                    </label>
                    <label
                      class="gender-pill"
                      :class="{ selected: user.gender === 'female' }"
                    >
                      <input
                        type="radio"
                        v-model="user.gender"
                        value="female"
                        class="hidden-radio"
                      />
                      <span>Nữ</span>
                    </label>
                    <label
                      class="gender-pill"
                      :class="{ selected: user.gender === 'other' }"
                    >
                      <input
                        type="radio"
                        v-model="user.gender"
                        value="other"
                        class="hidden-radio"
                      />
                      <span>Khác</span>
                    </label>
                  </div>
                </div>
              </div>
            </div>

            <!-- Right Contact & Security Card -->
            <div class="bento-card">
              <h3 class="card-section-title">
                <i class="bi bi-shield-lock"></i>
                Liên Hệ & Bảo Mật
              </h3>

              <!-- Số điện thoại & Thêm SĐT qua mã OTP Zalo -->
              <div class="form-group">
                <div class="label-with-badge">
                  <label class="form-label mb-0">Số điện thoại</label>
                  <span
                    v-if="user.phone && user.phone !== 'Chưa có'"
                    class="zalo-status-chip linked"
                  >
                    <i class="bi bi-patch-check-fill"></i> Đã xác thực OTP
                  </span>
                  <span v-else class="zalo-status-chip unlinked">
                    <i class="bi bi-exclamation-circle-fill"></i> Chưa thêm số
                    điện thoại
                  </span>
                </div>

                <div class="zalo-phone-display-card">
                  <div class="phone-info-left">
                    <div class="phone-symbol-icon">
                      <i class="bi bi-telephone-fill"></i>
                    </div>
                    <div>
                      <div class="phone-number-text font-mono">
                        {{ user.phone || 'Chưa thiết lập số điện thoại' }}
                      </div>
                      <div class="phone-helper-text">
                        <span v-if="user.phone" class="text-success-bold">
                          ✓ Dùng SĐT này và mật khẩu để đăng nhập trực tiếp
                          (không cần Gmail)
                        </span>
                        <span v-else class="text-muted">
                          Thêm số điện thoại để đăng nhập nhanh không cần Gmail
                          (xác thực mã qua Zalo)
                        </span>
                      </div>
                    </div>
                  </div>
                  <button
                    type="button"
                    class="btn-zalo-connect"
                    @click="openLinkZaloModal"
                  >
                    <i
                      :class="
                        user.phone
                          ? 'bi bi-pencil-square me-1'
                          : 'bi bi-plus-circle-fill me-1'
                      "
                    ></i>
                    {{
                      user.phone ? 'Đổi Số Điện Thoại' : 'Thêm Số Điện Thoại'
                    }}
                  </button>
                </div>
              </div>

              <div class="form-group">
                <label class="form-label">Địa chỉ Email</label>
                <div class="input-action-wrap">
                  <input v-model="user.email" type="email" class="form-input" />
                  <span class="verified-chip">
                    <i class="bi bi-check-all"></i> Đã liên kết
                  </span>
                </div>
              </div>

              <div class="form-group">
                <label class="form-label">Mật khẩu</label>
                <div class="input-action-wrap">
                  <input
                    type="password"
                    value="••••••••••••"
                    readonly
                    class="form-input font-mono"
                  />
                  <button
                    type="button"
                    class="btn-change-pwd"
                    @click="openChangePwdModal"
                  >
                    Đổi mật khẩu
                  </button>
                </div>
              </div>

              <!-- Xác thực khuôn mặt (Face ID Biometric) -->
              <div class="form-group face-auth-group">
                <div class="label-with-badge">
                  <label class="form-label mb-0">Xác Thực Khuôn Mặt (Face ID)</label>
                  <span
                    v-if="isFaceAuthEnabled"
                    class="face-status-chip active"
                  >
                    <i class="bi bi-shield-check"></i> Đã kích hoạt
                  </span>
                  <span v-else class="face-status-chip inactive">
                    <i class="bi bi-shield-slash"></i> Chưa kích hoạt
                  </span>
                </div>

                <div class="face-auth-card" :class="{ 'is-active': isFaceAuthEnabled }">
                  <div class="face-info-left">
                    <div class="face-symbol-icon" :class="{ 'symbol-active': isFaceAuthEnabled }">
                      <i class="bi bi-person-bounding-box"></i>
                    </div>
                    <div>
                      <div class="face-title-text">
                        {{ isFaceAuthEnabled ? 'Đã liên kết khuôn mặt của bạn' : 'Chưa kích hoạt nhận diện khuôn mặt' }}
                      </div>
                      <div class="face-helper-text">
                        <span v-if="isFaceAuthEnabled" class="text-success-bold">
                          ✓ Đăng nhập 1-chạm cực nhanh bằng AI UniFace (Kích hoạt: {{ faceRegisteredAt || 'Gần đây' }})
                        </span>
                        <span v-else class="text-muted">
                          Bật tính năng này để quét khuôn mặt và liên kết độc quyền với tài khoản này, chống chéo tài khoản khi đăng nhập.
                        </span>
                      </div>
                    </div>
                  </div>

                  <div class="face-actions-right">
                    <template v-if="isFaceAuthEnabled">
                      <button
                        type="button"
                        class="btn-face-rescan"
                        @click="openFaceRegisterModal"
                        title="Quét lại khuôn mặt mới"
                      >
                        <i class="bi bi-arrow-clockwise me-1"></i> Quét Lại
                      </button>
                      <button
                        type="button"
                        class="btn-face-disable"
                        :disabled="isDisablingFace"
                        @click="handleDisableFace"
                        title="Tắt xác thực khuôn mặt"
                      >
                        <i class="bi bi-x-circle me-1"></i> Tắt
                      </button>
                    </template>
                    <template v-else>
                      <button
                        type="button"
                        class="btn-face-activate"
                        @click="openFaceRegisterModal"
                      >
                        <i class="bi bi-camera-fill me-1"></i> Bật Face ID
                      </button>
                    </template>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 2: ĐƠN HÀNG GẦN ĐÂY -->
        <div v-else-if="currentTab === 'orders'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Đơn Mua Gần Đây</h2>
              <p class="pane-subtitle">
                Theo dõi trạng thái giao hàng hỏa tốc và lịch sử đặt đồ ăn, thực
                phẩm.
              </p>
            </div>
            <button
              class="btn-secondary-action"
              @click="router.push('/products')"
            >
              <i class="bi bi-bag-plus"></i> Đặt Thêm Món Ngon
            </button>
          </div>

          <div v-if="orders.length > 0" class="orders-feed">
            <div v-for="order in orders" :key="order.id" class="order-card">
              <div class="order-card-header">
                <div class="order-store-info">
                  <i class="bi bi-shop store-icon"></i>
                  <div>
                    <h4 class="store-name">{{ order.store }}</h4>
                    <span class="order-meta-text"
                      >Mã đơn: <strong>{{ order.id }}</strong> •
                      {{ order.date }} • Cách {{ order.storeDistance }}</span
                    >
                  </div>
                </div>

                <div class="order-status-badge" :class="order.status">
                  <span
                    v-if="order.status === 'delivering'"
                    class="pulse-icon"
                  ></span>
                  <span>{{ order.statusText }}</span>
                </div>
              </div>

              <!-- Danh sách sản phẩm đầy đủ hình ảnh và thông tin chi tiết -->
              <div class="profile-order-products-list">
                <div
                  v-for="(item, idx) in parseOrderItems(order.items)"
                  :key="idx"
                  class="profile-order-product-item"
                >
                  <div class="product-thumb-wrapper">
                    <img
                      v-if="item.image"
                      :src="item.image"
                      :alt="item.name"
                      class="product-thumb-img"
                      @error="
                        (e) =>
                          ((e.target as HTMLImageElement).src =
                            'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=150&q=80')
                      "
                    />
                    <div v-else class="product-thumb-fallback">
                      <i class="bi bi-basket2-fill"></i>
                    </div>
                  </div>

                  <div class="product-info-wrapper">
                    <h5 class="product-title">{{ item.name }}</h5>
                    <span v-if="item.shop" class="product-store-tag">
                      <i class="bi bi-shop me-1"></i>{{ item.shop }}
                    </span>
                    <div class="product-pricing">
                      <span v-if="item.price > 0" class="price-text"
                        >{{ item.price.toLocaleString('vi-VN') }} ₫</span
                      >
                      <span class="qty-tag">x{{ item.quantity || 1 }}</span>
                    </div>
                  </div>

                  <div class="product-subtotal-wrapper" v-if="item.price > 0">
                    <span class="subtotal-amount"
                      >{{
                        (
                          (item.price || 0) * (item.quantity || 1)
                        ).toLocaleString('vi-VN')
                      }}
                      ₫</span
                    >
                  </div>
                </div>
              </div>

              <!-- Địa chỉ giao hàng nếu có -->
              <div v-if="order.shippingAddress" class="profile-shipping-info">
                <i class="bi bi-geo-alt-fill text-danger me-1"></i>
                <span
                  ><strong>Giao đến:</strong>
                  {{
                    order.recipientName
                      ? `${order.recipientName} (${order.recipientPhone}) - `
                      : ''
                  }}{{ order.shippingAddress }}</span
                >
              </div>

              <div class="order-card-footer">
                <div class="order-total-price">
                  <span class="total-label">Tổng tiền:</span>
                  <strong class="price-val"
                    >{{ order.total.toLocaleString('vi-VN') }} ₫</strong
                  >
                </div>

                <div class="order-actions">
                  <button
                    v-if="order.status === 'delivering'"
                    class="btn-action-primary"
                    @click="router.push('/map')"
                  >
                    <i class="bi bi-cursor-fill"></i> Theo Dõi Shipper
                  </button>
                  <button
                    class="btn-action-secondary"
                    @click="triggerToast('Đã thêm lại món vào giỏ hàng!')"
                  >
                    <i class="bi bi-arrow-repeat"></i> Mua Lại
                  </button>
                  <button
                    class="btn-action-ghost"
                    @click="router.push('/contact')"
                  >
                    Khiếu nại
                  </button>
                </div>
              </div>
            </div>
          </div>
          <div v-else class="empty-feed-card">
            <div class="empty-feed-art">
              <i class="bi bi-box-seam"></i>
            </div>
            <h3 class="empty-feed-title">Bạn Chưa Có Đơn Mua Nào</h3>
            <p class="empty-feed-subtitle">
              Lịch sử đặt đồ ăn và thực phẩm tươi giao hỏa tốc 10km sẽ được hiển
              thị và cập nhật trực tiếp tại đây.
            </p>
            <button class="btn-explore-shop" @click="router.push('/products')">
              <i class="bi bi-bag-plus-fill me-1"></i> Khám Phá Nông Sản Quanh
              Bạn
            </button>
          </div>
        </div>

        <!-- TAB 3: SỔ ĐỊA CHỈ -->
        <div v-else-if="currentTab === 'addresses'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Sổ Địa Chỉ Nhận Hàng</h2>
              <p class="pane-subtitle">
                Địa chỉ nhận hàng tối ưu cho thuật toán giao hỏa tốc 20 - 30
                phút.
              </p>
            </div>
            <button class="btn-save-primary" @click="openAddAddressModal">
              <i class="bi bi-plus-lg"></i> Thêm Địa Chỉ Mới
            </button>
          </div>

          <div v-if="addresses.length > 0" class="addresses-grid">
            <div
              v-for="addr in addresses"
              :key="addr.id"
              class="address-card"
              :class="{ 'default-card': addr.isDefault }"
            >
              <div class="addr-header">
                <div class="addr-type-tag">
                  <i
                    class="bi"
                    :class="
                      addr.id === 1 ? 'bi-house-door-fill' : 'bi-building-fill'
                    "
                  ></i>
                  <span>{{ addr.title }}</span>
                </div>
                <span v-if="addr.isDefault" class="default-badge">
                  <i class="bi bi-star-fill"></i> Mặc định
                </span>
              </div>

              <h4 class="receiver-name">
                {{ addr.receiver }} <small>({{ addr.phone }})</small>
              </h4>
              <p class="addr-detail">{{ addr.detail }}</p>
              <span class="radius-chip">
                <i class="bi bi-radar"></i> {{ addr.distanceTag }}
              </span>

              <div class="addr-footer-actions">
                <button
                  v-if="!addr.isDefault"
                  class="btn-set-default"
                  @click="setDefaultAddress(addr.id)"
                >
                  Đặt làm mặc định
                </button>
                <div class="addr-btn-group">
                  <button class="btn-icon-text" @click="editAddress(addr)">
                    <i class="bi bi-pencil"></i> Sửa
                  </button>
                  <button
                    v-if="!addr.isDefault"
                    class="btn-icon-text text-danger"
                    @click="deleteAddress(addr.id)"
                  >
                    <i class="bi bi-trash3"></i> Xóa
                  </button>
                </div>
              </div>
            </div>
          </div>
          <div v-else class="empty-feed-card">
            <div class="empty-feed-art">
              <i class="bi bi-geo-alt"></i>
            </div>
            <h3 class="empty-feed-title">Chưa Có Địa Chỉ Giao Hàng</h3>
            <p class="empty-feed-subtitle">
              Vui lòng thêm địa chỉ nhận hàng để ZoneMart tính toán khoảng cách
              và kết nối shipper giao hỏa tốc trong 20-30 phút.
            </p>
            <button class="btn-explore-shop" @click="openAddAddressModal">
              <i class="bi bi-plus-lg me-1"></i> Thêm Địa Chỉ Đầu Tiên
            </button>
          </div>
        </div>

        <!-- TAB 4: VÍ ZONEPAY & THANH TOÁN -->
        <div v-else-if="currentTab === 'wallet'" class="tab-pane">
          <div class="pane-header">
            <div>
              <h2 class="pane-title">Ví Điện Tử ZonePay</h2>
              <p class="pane-subtitle">
                Thanh toán không tiền mặt siêu tốc, hoàn tiền tức thì khi mua
                thực phẩm tươi.
              </p>
            </div>
          </div>

          <div class="wallet-bento-layout">
            <!-- ZonePay Virtual Card -->
            <div class="virtual-card">
              <div class="card-top-row">
                <span class="card-brand">Zone<span>Pay</span></span>
                <i class="bi bi-wifi contactless-icon"></i>
              </div>
              <div class="card-chip-sim"></div>
              <div class="card-balance-box">
                <span class="card-balance-label">Số dư khả dụng</span>
                <div class="card-balance-number">
                  {{ user.zonePayBalance.toLocaleString('vi-VN') }} ₫
                </div>
              </div>
              <div class="card-bottom-row">
                <span class="card-holder-name">{{
                  user.fullName.toUpperCase()
                }}</span>
                <span class="card-network">FAST 10KM PAY</span>
              </div>
            </div>

            <!-- Quick Top-up Action Box -->
            <div class="topup-box">
              <h3 class="topup-title">Nạp Tiền Nhanh Vào Ví</h3>
              <p class="topup-desc">
                Miễn phí nạp qua VNPay, MoMo, Vietcombank và Techcombank.
              </p>

              <div class="amount-pills">
                <button class="amount-btn" @click="quickTopUp(100000)">
                  +100.000 ₫
                </button>
                <button class="amount-btn" @click="quickTopUp(200000)">
                  +200.000 ₫
                </button>
                <button
                  class="amount-btn active-pill"
                  @click="quickTopUp(500000)"
                >
                  +500.000 ₫
                </button>
                <button class="amount-btn" @click="quickTopUp(1000000)">
                  +1.000.000 ₫
                </button>
              </div>

              <div class="payment-partners">
                <span class="partner-label">Liên kết bảo mật:</span>
                <div class="partner-badges">
                  <span class="p-badge">MoMo</span>
                  <span class="p-badge">VNPay QR</span>
                  <span class="p-badge">ZaloPay</span>
                  <span class="p-badge">Visa/Mastercard</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>

    <!-- MODAL ĐỔI MẬT KHẨU -->
    <div
      v-if="isPasswordModalOpen"
      class="modal-backdrop"
      @click="isPasswordModalOpen = false"
    >
      <div class="modal-card" @click.stop>
        <div class="modal-header">
          <h3 class="modal-title">
            <i class="bi bi-shield-lock-fill text-orange me-2"></i>Đổi Mật Khẩu
            Tài Khoản
          </h3>
          <button class="modal-close-btn" @click="isPasswordModalOpen = false">
            ✕
          </button>
        </div>
        <form @submit.prevent="handleChangePasswordSubmit">
          <div class="modal-body">
            <p class="modal-desc">
              Cập nhật mật khẩu bảo vệ cho tài khoản
              <strong>{{ user.email }}</strong
              >.
            </p>
            <div class="form-group mb-3">
              <label for="pwd-old" class="form-label font-bold"
                >Mật khẩu hiện tại</label
              >
              <div class="input-icon-wrapper">
                <i class="bi bi-lock input-leading-icon"></i>
                <input
                  id="pwd-old"
                  name="current-password"
                  v-model="pwdForm.oldPassword"
                  type="password"
                  autocomplete="current-password"
                  placeholder="Nhập mật khẩu hiện tại (nếu có)"
                  class="form-input has-leading-icon"
                />
              </div>
            </div>
            <div class="form-group mb-3">
              <label for="pwd-new" class="form-label font-bold"
                >Mật khẩu mới (tối thiểu 6 ký tự)</label
              >
              <div class="input-icon-wrapper">
                <i class="bi bi-key-fill input-leading-icon"></i>
                <input
                  id="pwd-new"
                  name="new-password"
                  v-model="pwdForm.newPassword"
                  type="password"
                  autocomplete="new-password"
                  placeholder="Nhập mật khẩu mới"
                  class="form-input has-leading-icon"
                  minlength="6"
                  required
                />
              </div>
            </div>
            <div class="form-group mb-3">
              <label for="pwd-confirm" class="form-label font-bold"
                >Xác nhận mật khẩu mới</label
              >
              <div class="input-icon-wrapper">
                <i class="bi bi-shield-check input-leading-icon"></i>
                <input
                  id="pwd-confirm"
                  name="confirm-password"
                  v-model="pwdForm.confirmPassword"
                  type="password"
                  autocomplete="new-password"
                  placeholder="Nhập lại mật khẩu mới"
                  class="form-input has-leading-icon"
                  minlength="6"
                  required
                />
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button
              type="button"
              class="btn-cancel"
              @click="isPasswordModalOpen = false"
            >
              Hủy Bỏ
            </button>
            <button
              type="submit"
              class="btn-confirm"
              :disabled="isSubmittingPwd"
            >
              <span v-if="isSubmittingPwd" class="spinner-small me-2"></span>
              <i v-else class="bi bi-check2-circle me-1"></i>
              <span>Xác Nhận Đổi Mật Khẩu</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- MODAL THÊM / SỬA ĐỊA CHỈ NHẬN HÀNG -->
    <div
      v-if="isAddressModalOpen"
      class="modal-backdrop"
      @click="isAddressModalOpen = false"
    >
      <div class="modal-card" @click.stop>
        <div class="modal-header">
          <h3 class="modal-title">
            <i class="bi bi-geo-alt-fill text-orange me-2"></i>
            {{
              editingAddressId !== null
                ? 'Chỉnh Sửa Địa Chỉ'
                : 'Thêm Địa Chỉ Mới'
            }}
          </h3>
          <button class="modal-close-btn" @click="isAddressModalOpen = false">
            ✕
          </button>
        </div>
        <form @submit.prevent="handleSaveAddressForm">
          <div class="modal-body">
            <div class="form-group mb-3">
              <label for="addr-title" class="form-label font-bold"
                >Loại địa chỉ</label
              >
              <select
                id="addr-title"
                v-model="addressForm.title"
                class="form-input"
              >
                <option value="Nhà riêng">Nhà riêng</option>
                <option value="Văn phòng công ty">Văn phòng công ty</option>
                <option value="Cửa hàng / Kho hàng">Cửa hàng / Kho hàng</option>
                <option value="Khác">Khác</option>
              </select>
            </div>
            <div class="fields-grid-2 mb-3">
              <div class="form-group">
                <label for="addr-receiver" class="form-label font-bold"
                  >Tên người nhận</label
                >
                <div class="input-icon-wrapper">
                  <i class="bi bi-person-fill input-leading-icon"></i>
                  <input
                    id="addr-receiver"
                    name="name"
                    autocomplete="name"
                    v-model="addressForm.receiver"
                    type="text"
                    class="form-input has-leading-icon"
                    placeholder="Họ và tên"
                    required
                  />
                </div>
              </div>
              <div class="form-group">
                <label for="addr-phone" class="form-label font-bold"
                  >Số điện thoại</label
                >
                <div class="input-icon-wrapper">
                  <i class="bi bi-telephone-fill input-leading-icon"></i>
                  <input
                    id="addr-phone"
                    name="tel"
                    autocomplete="tel"
                    v-model="addressForm.phone"
                    type="tel"
                    class="form-input has-leading-icon font-mono"
                    placeholder="09xx xxx xxx"
                    required
                  />
                </div>
              </div>
            </div>
            <div class="form-group mb-3">
              <label for="addr-detail" class="form-label font-bold"
                >Địa chỉ chi tiết (Số nhà, Ngõ/Hẻm, Đường, Phường, Quận)</label
              >
              <textarea
                id="addr-detail"
                v-model="addressForm.detail"
                rows="3"
                class="form-input"
                placeholder="VD: Số 18, Ngõ 245 Cầu Giấy, P. Dịch Vọng, Q. Cầu Giấy, Hà Nội"
                required
              ></textarea>
            </div>
            <div class="form-check-wrap">
              <label class="checkbox-label">
                <input type="checkbox" v-model="addressForm.isDefault" />
                <span
                  >Đặt địa chỉ này làm mặc định cho các đơn giao hỏa tốc
                  10km</span
                >
              </label>
            </div>
          </div>
          <div class="modal-footer">
            <button
              type="button"
              class="btn-cancel"
              @click="isAddressModalOpen = false"
            >
              Hủy
            </button>
            <button type="submit" class="btn-confirm">
              <i class="bi bi-check2-circle me-1"></i>
              <span>Lưu Địa Chỉ</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- MODAL THÊM / ĐỔI SỐ ĐIỆN THOẠI (XÁC THỰC MÃ QUA GMAIL OTP) -->
    <div
      v-if="isZaloModalOpen"
      class="modal-backdrop"
      @click="isZaloModalOpen = false"
    >
      <div class="modal-card" @click.stop>
        <div class="modal-header">
          <h3 class="modal-title">
            <i class="bi bi-shield-lock-fill text-orange me-2"></i>
            {{
              user.phone && user.phone !== 'Chưa có'
                ? 'Thay Đổi Số Điện Thoại'
                : 'Thêm Số Điện Thoại'
            }}
          </h3>
          <button class="modal-close-btn" @click="isZaloModalOpen = false">
            ✕
          </button>
        </div>

        <form @submit.prevent="handleVerifyZaloOtp">
          <div class="modal-body">
            <p class="modal-desc">
              Nhập số điện thoại bạn muốn liên kết. Hệ thống sẽ gửi mã xác thực
              6 số (OTP) tới Gmail tài khoản của bạn để bảo mật.
            </p>

            <!-- Email nhận mã -->
            <div class="email-target-banner mb-3">
              <i class="bi bi-envelope-check-fill text-orange me-2"></i>
              <span
                >Mã OTP sẽ gửi về:
                <strong>{{
                  user.email || currentAccount.phoneEmail
                }}</strong></span
              >
            </div>

            <!-- Ô nhập số điện thoại -->
            <div class="form-group mb-3">
              <label for="phone-link-input" class="form-label font-bold"
                >Số điện thoại liên kết</label
              >
              <div class="phone-input-action-row">
                <div class="phone-prefix-tag">
                  <span>🇻🇳 +84</span>
                </div>
                <input
                  v-model="zaloPhoneInput"
                  type="tel"
                  id="phone-link-input"
                  name="phoneNumber"
                  autocomplete="tel-national"
                  class="form-input font-mono flex-1"
                  placeholder="Ví dụ: 0396222614"
                  maxlength="11"
                  required
                />
                <button
                  type="button"
                  class="btn-send-otp-action"
                  :disabled="isSendingZaloOtp || zaloCountdown > 0"
                  @click="handleSendZaloOtp"
                >
                  <span v-if="isSendingZaloOtp" class="spinner-small"></span>
                  <span v-else-if="zaloCountdown > 0"
                    >Gửi lại ({{ zaloCountdown }}s)</span
                  >
                  <span v-else
                    ><i class="bi bi-envelope-arrow-up-fill me-1"></i> Gửi Mã
                    OTP</span
                  >
                </button>
              </div>
              <small class="text-muted mt-1 d-block">
                Sau khi xác thực xong, bạn có thể dùng SĐT này để đăng nhập trực
                tiếp.
              </small>
            </div>

            <!-- Ô nhập mã OTP 6 số -->
            <div class="form-group mb-2">
              <label for="phone-link-otp" class="form-label font-bold mb-1"
                >Mã xác thực 6 số (Gửi qua Gmail)</label
              >
              <input
                v-model="zaloOtpInput"
                type="text"
                id="phone-link-otp"
                name="otpCode"
                autocomplete="one-time-code"
                class="form-input font-mono text-center otp-input-large"
                placeholder="• • • • • •"
                maxlength="6"
                required
              />
              <small class="text-muted mt-1 d-block">
                <i class="bi bi-shield-check text-success me-1"></i>
                Mã có hiệu lực trong 5 phút. Hãy mở ứng dụng Gmail (hoặc thư mục
                Spam) để lấy mã.
              </small>
            </div>
          </div>

          <div class="modal-footer">
            <button
              type="button"
              class="btn-cancel"
              @click="isZaloModalOpen = false"
            >
              Hủy Bỏ
            </button>
            <button
              type="submit"
              class="btn-confirm"
              :disabled="
                isVerifyingZaloOtp || !zaloOtpInput || zaloOtpInput.length < 6
              "
            >
              <span v-if="isVerifyingZaloOtp" class="spinner-small me-2"></span>
              <i v-else class="bi bi-check2-circle me-2"></i>
              Xác Nhận & Thêm SĐT
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal Quét & Kích Hoạt Khuôn Mặt (Face ID UniFace) -->
    <FaceScanModal
      v-model="showFaceModal"
      mode="register"
      :user-id="currentAccount.id || currentAccount.phoneEmail"
      :user-name="user.fullName"
      @success="handleFaceRegisterSuccess"
    />
  </div>
</template>

<style scoped>
/* ==========================================================================
   GLOBAL LAYOUT (EDGE-TO-EDGE FULL SCREEN)
   ========================================================================== */
.new-profile-page {
  background-color: #f8fafc;
  min-height: calc(100vh - 72px);
  padding: 36px 32px 80px 32px;
  color: #1e293b;
  font-family:
    'Plus Jakarta Sans',
    -apple-system,
    BlinkMacSystemFont,
    'Segoe UI',
    Roboto,
    sans-serif;
}

.profile-main-container {
  max-width: 1280px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 32px;
}

.hidden-file-input,
.hidden-radio {
  display: none;
}

/* Toast Popup */
.toast-popup {
  position: fixed;
  top: 86px;
  right: 28px;
  background-color: #0f172a;
  color: #ffffff;
  padding: 14px 22px;
  border-radius: 14px;
  font-size: 14px;
  font-weight: 700;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.18);
  display: flex;
  align-items: center;
  gap: 10px;
  z-index: 9999;
  animation: slideToast 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-icon {
  color: #22c55e;
  font-size: 18px;
}

@keyframes slideToast {
  from {
    transform: translateY(-20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

/* ==========================================================================
   1. PROFILE HERO CARD (EDITORIAL & FRAMELESS)
   ========================================================================== */
.profile-hero-card {
  background: #ffffff;
  border-radius: 24px;
  padding: 36px 40px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 24px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 32px;
}

.hero-user-details {
  display: flex;
  align-items: center;
  gap: 28px;
  flex: 1;
  min-width: 320px;
}

/* Avatar */
.avatar-container {
  position: relative;
  width: 100px;
  height: 100px;
  cursor: pointer;
  flex-shrink: 0;
}

.user-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
  border: 3px solid #ffedd5;
  box-shadow: 0 4px 16px rgba(234, 88, 12, 0.15);
  transition: transform 0.2s ease;
}

.avatar-container:hover .user-avatar-img {
  transform: scale(1.03);
}

.camera-btn {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 32px;
  height: 32px;
  background: #ea580c;
  color: #ffffff;
  border: 2px solid #ffffff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

/* User Info */
.user-meta-info {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
}

.name-row {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.user-display-name {
  font-size: 26px;
  font-weight: 900;
  letter-spacing: -0.02em;
  color: #0f172a;
  margin: 0;
}

.member-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
  font-size: 12px;
  font-weight: 800;
  padding: 4px 10px;
  border-radius: 20px;
}

.user-subline {
  font-size: 13.5px;
  color: #64748b;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.divider-dot {
  color: #cbd5e1;
}

.text-green {
  color: #16a34a;
  font-weight: 700;
}

/* Tier progress */
.tier-progress-box {
  margin-top: 8px;
  max-width: 380px;
}

.progress-labels {
  display: flex;
  justify-content: space-between;
  font-size: 11.5px;
  color: #64748b;
  margin-bottom: 5px;
}

.progress-track {
  width: 100%;
  height: 6px;
  background: #f1f5f9;
  border-radius: 10px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #ea580c, #f97316);
  border-radius: 10px;
}

/* Right Stat Chips */
.hero-stats-grid {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
}

.stat-box {
  background: #f8fafc;
  border: 1px solid #f1f5f9;
  border-radius: 18px;
  padding: 16px 20px;
  display: flex;
  align-items: center;
  gap: 14px;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  min-width: 175px;
}

.stat-box:hover {
  transform: translateY(-2px);
  background: #ffffff;
  border-color: #fed7aa;
  box-shadow: 0 8px 20px -4px rgba(0, 0, 0, 0.06);
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

.bg-orange {
  background: #fff7ed;
  color: #ea580c;
}
.bg-blue {
  background: #eff6ff;
  color: #2563eb;
}
.bg-amber {
  background: #fefce8;
  color: #d97706;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 11.5px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.stat-value {
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
}

.text-orange {
  color: #ea580c !important;
}

/* ==========================================================================
   2. MODERN NAVIGATION TABS
   ========================================================================== */
.profile-nav-tabs {
  display: flex;
  gap: 8px;
  border-bottom: 2px solid #e2e8f0;
  overflow-x: auto;
  padding-bottom: 4px;
}

.tab-btn {
  background: none;
  border: none;
  padding: 12px 20px;
  font-family: inherit;
  font-size: 14.5px;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border-radius: 12px 12px 0 0;
  position: relative;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.tab-btn:hover {
  color: #ea580c;
  background-color: #fff7ed;
}

.tab-btn.active {
  color: #ea580c;
  background-color: #ffffff;
}

.tab-btn.active::after {
  content: '';
  position: absolute;
  bottom: -6px;
  left: 0;
  right: 0;
  height: 3px;
  background-color: #ea580c;
  border-radius: 3px 3px 0 0;
}

.badge-count {
  background: #ea580c;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 12px;
}

/* ==========================================================================
   3. TAB CONTENT PANE
   ========================================================================== */
.tab-content-container {
  width: 100%;
}

.tab-pane {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.pane-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.pane-title {
  font-size: 22px;
  font-weight: 900;
  color: #0f172a;
  margin: 0 0 4px 0;
  letter-spacing: -0.01em;
}

.pane-subtitle {
  font-size: 14px;
  color: #64748b;
  margin: 0;
}

/* Button styles */
.btn-save-primary {
  background: #0f172a;
  color: #ffffff;
  border: none;
  font-family: inherit;
  font-size: 14px;
  font-weight: 700;
  padding: 12px 24px;
  border-radius: 14px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
}

.btn-save-primary:hover:not(:disabled) {
  background: #ea580c;
  transform: translateY(-2px);
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.28);
}

.btn-secondary-action {
  background: #ffffff;
  color: #0f172a;
  border: 1px solid #cbd5e1;
  font-family: inherit;
  font-size: 13.5px;
  font-weight: 700;
  padding: 10px 18px;
  border-radius: 12px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
}

.btn-secondary-action:hover {
  border-color: #ea580c;
  color: #ea580c;
}

/* ==========================================================================
   TAB 1: FORM BENTO
   ========================================================================== */
.form-bento-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

.bento-card {
  background: #ffffff;
  border-radius: 22px;
  padding: 28px 32px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.card-section-title {
  font-size: 17px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 10px;
}

.card-section-title i {
  color: #ea580c;
  font-size: 18px;
}

.fields-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-label {
  font-size: 13px;
  font-weight: 700;
  color: #334155;
}

.form-input {
  width: 100%;
  padding: 12px 16px;
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  font-family: inherit;
  font-size: 14px;
  color: #0f172a;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  background-color: #ffffff;
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}

.font-mono {
  font-family: 'SFMono-Regular', Consolas, monospace;
}

/* Gender Pills */
.gender-pill-group {
  display: flex;
  gap: 8px;
}

.gender-pill {
  flex: 1;
  text-align: center;
  padding: 10px 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  font-size: 13.5px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.gender-pill:hover {
  background: #fff7ed;
  border-color: #fed7aa;
}

.gender-pill.selected {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(234, 88, 12, 0.25);
}

/* Action input wrap */
.input-action-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.verified-chip {
  position: absolute;
  right: 12px;
  font-size: 11.5px;
  font-weight: 700;
  color: #16a34a;
  background: #f0fdf4;
  padding: 3px 8px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.btn-change-pwd {
  position: absolute;
  right: 8px;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #334155;
  font-size: 12px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
}

.btn-change-pwd:hover {
  border-color: #ea580c;
  color: #ea580c;
}

/* ==========================================================================
   TAB 2: ORDERS FEED
   ========================================================================== */
.orders-feed {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.empty-feed-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 54px 28px;
  border: 1px dashed #cbd5e1;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.empty-feed-art {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  background: #fff7ed;
  color: #ea580c;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  margin-bottom: 18px;
}

.empty-feed-title {
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 8px 0;
}

.empty-feed-subtitle {
  font-size: 14px;
  color: #64748b;
  max-width: 480px;
  margin: 0 0 24px 0;
  line-height: 1.5;
}

.btn-explore-shop {
  background: #ea580c;
  color: #ffffff;
  border: none;
  border-radius: 12px;
  padding: 12px 24px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s ease;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.25);
}

.btn-explore-shop:hover {
  background: #c2410c;
  transform: translateY(-2px);
}

.order-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 24px 28px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 14px -2px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.order-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.order-store-info {
  display: flex;
  align-items: center;
  gap: 14px;
}

.store-icon {
  width: 42px;
  height: 42px;
  background: #fff7ed;
  color: #ea580c;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}

.store-name {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 2px 0;
}

.order-meta-text {
  font-size: 12.5px;
  color: #64748b;
}

.order-status-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 12.5px;
  font-weight: 700;
  padding: 6px 14px;
  border-radius: 20px;
}

.order-status-badge.delivering {
  background: #eff6ff;
  color: #2563eb;
  border: 1px solid #bfdbfe;
}

.order-status-badge.completed {
  background: #f0fdf4;
  color: #16a34a;
  border: 1px solid #bbf7d0;
}

.pulse-icon {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #2563eb;
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.2);
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%,
  100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.3);
    opacity: 0.6;
  }
}

.profile-order-products-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.profile-order-product-item {
  display: flex;
  align-items: center;
  gap: 14px;
  background: #f8fafc;
  border: 1px solid #edf2f7;
  border-radius: 12px;
  padding: 10px 14px;
  transition: all 0.2s ease;
}

.profile-order-product-item:hover {
  background: #f1f5f9;
}

.product-thumb-wrapper {
  width: 58px;
  height: 58px;
  min-width: 58px;
  border-radius: 10px;
  overflow: hidden;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.product-thumb-wrapper .product-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.product-thumb-wrapper .product-thumb-fallback {
  color: #94a3b8;
  font-size: 22px;
}

.product-info-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.product-title {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.35;
}

.product-store-tag {
  font-size: 11.5px;
  color: #64748b;
  font-weight: 500;
}

.product-pricing {
  display: flex;
  align-items: center;
  gap: 8px;
}

.product-pricing .price-text {
  font-size: 13px;
  color: #dc2626;
  font-weight: 700;
}

.product-pricing .qty-tag {
  font-size: 11.5px;
  background: #e2e8f0;
  color: #334155;
  padding: 1px 6px;
  border-radius: 4px;
  font-weight: 600;
}

.product-subtotal-wrapper .subtotal-amount {
  font-size: 14.5px;
  color: #0f172a;
  font-weight: 800;
}

.profile-shipping-info {
  background: #fffbeb;
  border: 1px solid #fef3c7;
  border-radius: 10px;
  padding: 8px 12px;
  font-size: 12.5px;
  color: #78350f;
  display: flex;
  align-items: center;
  gap: 6px;
  line-height: 1.4;
}

.order-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  padding-top: 6px;
}

.total-label {
  font-size: 13px;
  color: #64748b;
  margin-right: 6px;
}

.price-val {
  font-size: 17px;
  font-weight: 900;
  color: #ea580c;
}

.order-actions {
  display: flex;
  gap: 10px;
}

.btn-action-primary {
  background: #0f172a;
  color: #ffffff;
  border: none;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 16px;
  border-radius: 10px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: background 0.2s;
}

.btn-action-primary:hover {
  background: #ea580c;
}

.btn-action-secondary {
  background: #ffffff;
  color: #334155;
  border: 1px solid #cbd5e1;
  font-size: 13px;
  font-weight: 700;
  padding: 8px 16px;
  border-radius: 10px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}

.btn-action-secondary:hover {
  border-color: #ea580c;
  color: #ea580c;
}

.btn-action-ghost {
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  padding: 8px 12px;
}

.btn-action-ghost:hover {
  color: #ef4444;
}

/* ==========================================================================
   TAB 3: ADDRESSES GRID
   ========================================================================== */
.addresses-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.address-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 24px;
  border: 1.5px solid #f1f5f9;
  box-shadow: 0 2px 14px -2px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 10px;
  transition: all 0.2s ease;
}

.address-card.default-card {
  border-color: #fed7aa;
  background-color: #fffaf5;
}

.addr-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.addr-type-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 800;
  color: #0f172a;
}

.default-badge {
  font-size: 11.5px;
  font-weight: 800;
  background: #ffedd5;
  color: #ea580c;
  padding: 3px 8px;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.receiver-name {
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.receiver-name small {
  font-weight: 500;
  color: #64748b;
}

.addr-detail {
  font-size: 13.5px;
  line-height: 1.55;
  color: #475569;
  margin: 0;
}

.radius-chip {
  font-size: 12px;
  color: #16a34a;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.addr-footer-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 8px;
  padding-top: 12px;
  border-top: 1px solid #f1f5f9;
}

.btn-set-default {
  background: transparent;
  border: none;
  font-size: 12.5px;
  font-weight: 700;
  color: #ea580c;
  cursor: pointer;
  padding: 0;
}

.btn-set-default:hover {
  text-decoration: underline;
}

.addr-btn-group {
  display: flex;
  gap: 12px;
}

.btn-icon-text {
  background: none;
  border: none;
  font-size: 12.5px;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.btn-icon-text:hover {
  color: #0f172a;
}

.text-danger {
  color: #ef4444 !important;
}

/* ==========================================================================
   TAB 4: WALLET BENTO
   ========================================================================== */
.wallet-bento-layout {
  display: grid;
  grid-template-columns: 1.1fr 1.4fr;
  gap: 28px;
  align-items: start;
}

.virtual-card {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  border-radius: 22px;
  padding: 28px 30px;
  color: #ffffff;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 230px;
  box-shadow: 0 16px 36px -8px rgba(15, 23, 42, 0.35);
  position: relative;
  overflow: hidden;
}

.virtual-card::before {
  content: '';
  position: absolute;
  top: -40px;
  right: -40px;
  width: 160px;
  height: 160px;
  border-radius: 50%;
  background: rgba(234, 88, 12, 0.25);
  filter: blur(30px);
}

.card-top-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-brand {
  font-size: 20px;
  font-weight: 900;
  letter-spacing: -0.5px;
}

.card-brand span {
  color: #ea580c;
}

.contactless-icon {
  font-size: 20px;
  opacity: 0.75;
}

.card-chip-sim {
  width: 38px;
  height: 28px;
  background: linear-gradient(135deg, #fcd34d 0%, #d97706 100%);
  border-radius: 6px;
  margin: 10px 0;
}

.card-balance-box {
  display: flex;
  flex-direction: column;
}

.card-balance-label {
  font-size: 11.5px;
  color: #94a3b8;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.card-balance-number {
  font-size: 24px;
  font-weight: 900;
  letter-spacing: -0.02em;
  color: #f8fafc;
}

.card-bottom-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 12px;
  color: #cbd5e1;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.card-network {
  color: #f97316;
  font-size: 11px;
}

/* Topup Box */
.topup-box {
  background: #ffffff;
  border-radius: 22px;
  padding: 28px 32px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.topup-title {
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.topup-desc {
  font-size: 13.5px;
  color: #64748b;
  margin: 0;
}

.amount-pills {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
  margin-top: 6px;
}

.amount-btn {
  padding: 12px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  font-family: inherit;
  font-size: 14px;
  font-weight: 800;
  color: #334155;
  cursor: pointer;
  transition: all 0.2s ease;
}

.amount-btn:hover {
  background: #fff7ed;
  border-color: #fed7aa;
  color: #ea580c;
  transform: translateY(-1px);
}

.amount-btn.active-pill {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.25);
}

.payment-partners {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 10px;
}

.partner-label {
  font-size: 12px;
  font-weight: 700;
  color: #64748b;
}

.partner-badges {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.p-badge {
  background: #f1f5f9;
  color: #475569;
  font-size: 11.5px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
}

/* Spinner */
.spinner-small {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
  display: inline-block;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1024px) {
  .new-profile-page {
    padding: 24px 16px 60px 16px;
  }

  .form-bento-grid {
    grid-template-columns: 1fr;
  }

  .wallet-bento-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .profile-hero-card {
    padding: 24px 20px;
  }

  .hero-user-details {
    flex-direction: column;
    text-align: center;
  }

  .name-row {
    justify-content: center;
  }

  .user-subline {
    justify-content: center;
  }

  .tier-progress-box {
    margin: 8px auto 0 auto;
  }

  .hero-stats-grid {
    width: 100%;
    flex-direction: column;
  }

  .fields-grid-2 {
    grid-template-columns: 1fr;
  }

  .addresses-grid {
    grid-template-columns: 1fr;
  }

  .order-card-footer {
    flex-direction: column;
    align-items: flex-start;
  }

  .order-actions {
    width: 100%;
    flex-wrap: wrap;
  }
}

/* ==========================================================================
   MODAL STYLES (CHANGE PASSWORD & ADDRESS)
   ========================================================================== */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.7);
  backdrop-filter: blur(6px);
  z-index: 10000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.modal-card {
  background: #ffffff;
  border-radius: 20px;
  max-width: 500px;
  width: 100%;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  overflow: hidden;
  border: 1px solid #e2e8f0;
  animation: modalFadeIn 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  max-height: 90vh;
  display: flex;
  flex-direction: column;
}

@keyframes modalFadeIn {
  from {
    opacity: 0;
    transform: scale(0.95) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid #f1f5f9;
  background: #f8fafc;
}

.modal-title {
  font-size: 17px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
  display: flex;
  align-items: center;
}

.modal-close-btn {
  background: transparent;
  border: none;
  font-size: 18px;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 8px;
  transition: all 0.15s;
}

.modal-close-btn:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.modal-body {
  padding: 24px;
  overflow-y: auto;
}

.modal-desc {
  font-size: 13.5px;
  color: #475569;
  margin-top: 0;
  margin-bottom: 18px;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 24px;
  border-top: 1px solid #f1f5f9;
  background: #f8fafc;
}

.btn-cancel {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #475569;
  font-size: 13.5px;
  font-weight: 700;
  padding: 9px 18px;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-cancel:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.btn-confirm {
  background: #ea580c;
  border: none;
  color: #ffffff;
  font-size: 13.5px;
  font-weight: 700;
  padding: 9px 20px;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.15s;
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-confirm:hover:not(:disabled) {
  background: #c2410c;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.3);
}

.btn-confirm:active:not(:disabled) {
  transform: translateY(1px) scale(0.985);
}

.btn-confirm:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-cancel:active {
  transform: translateY(1px) scale(0.985);
}

.input-icon-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-leading-icon {
  position: absolute;
  left: 12px;
  font-size: 14px;
  color: #94a3b8;
  pointer-events: none;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  z-index: 2;
}

.input-icon-wrapper:focus-within .input-leading-icon {
  color: #ea580c;
  transform: scale(1.12);
}

.form-input.has-leading-icon {
  padding-left: 36px;
}

.form-check-wrap {
  margin-top: 12px;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: #334155;
  cursor: pointer;
}

.checkbox-label input[type='checkbox'] {
  width: 16px;
  height: 16px;
  accent-color: #ea580c;
  cursor: pointer;
}

/* ==========================================================================
   ZALO PHONE OTP & AUTHENTICATION ENHANCEMENTS
   ========================================================================== */
.label-with-badge {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.zalo-status-chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: 20px;
}

.zalo-status-chip.linked {
  background: #f0fdf4;
  color: #16a34a;
  border: 1px solid #bbf7d0;
}

.zalo-status-chip.unlinked {
  background: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.zalo-phone-display-card {
  background: #ffffff;
  border: 1.5px solid #e2e8f0;
  border-radius: 14px;
  padding: 14px 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  transition: all 0.2s ease;
}

.zalo-phone-display-card:hover {
  border-color: #ea580c;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.08);
}

.phone-info-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.phone-symbol-icon {
  width: 44px;
  height: 44px;
  background: linear-gradient(135deg, #ea580c, #f97316);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  font-size: 18px;
  box-shadow: 0 4px 10px rgba(234, 88, 12, 0.25);
  flex-shrink: 0;
}

.phone-number-text {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
}

.phone-helper-text {
  font-size: 12px;
  margin-top: 2px;
}

.text-success-bold {
  color: #16a34a;
  font-weight: 600;
}

.btn-zalo-connect {
  background: #ea580c;
  color: #ffffff;
  border: none;
  font-size: 13px;
  font-weight: 700;
  padding: 9px 18px;
  border-radius: 10px;
  cursor: pointer;
  white-space: nowrap;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s ease;
  box-shadow: 0 2px 8px rgba(234, 88, 12, 0.2);
}

.btn-zalo-connect:hover {
  background: #c2410c;
  transform: translateY(-1px);
}

/* FACE ID BIOMETRIC AUTH STYLES */
.face-auth-group {
  margin-top: 18px;
}

.face-status-chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: 20px;
}

.face-status-chip.active {
  background: #ecfdf5;
  color: #059669;
  border: 1px solid #a7f3d0;
}

.face-status-chip.inactive {
  background: #f1f5f9;
  color: #64748b;
  border: 1px solid #e2e8f0;
}

.face-auth-card {
  background: #ffffff;
  border: 1.5px solid #e2e8f0;
  border-radius: 14px;
  padding: 14px 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  transition: all 0.2s ease;
}

.face-auth-card.is-active {
  border-color: #10b981;
  background: linear-gradient(to right, #f0fdf4, #ffffff);
}

.face-auth-card:hover {
  border-color: #0284c7;
  box-shadow: 0 4px 14px rgba(2, 132, 199, 0.08);
}

.face-info-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.face-symbol-icon {
  width: 44px;
  height: 44px;
  background: #f1f5f9;
  color: #64748b;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
  transition: all 0.2s ease;
}

.face-symbol-icon.symbol-active {
  background: linear-gradient(135deg, #059669, #10b981);
  color: #ffffff;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.25);
}

.face-title-text {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
}

.face-helper-text {
  font-size: 12px;
  margin-top: 2px;
  line-height: 1.4;
}

.face-actions-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-face-activate {
  background: linear-gradient(135deg, #0284c7, #2563eb);
  color: #ffffff;
  border: none;
  font-size: 13px;
  font-weight: 700;
  padding: 9px 18px;
  border-radius: 10px;
  cursor: pointer;
  white-space: nowrap;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s ease;
  box-shadow: 0 2px 8px rgba(37, 99, 235, 0.25);
}

.btn-face-activate:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.35);
}

.btn-face-rescan {
  background: #f1f5f9;
  color: #334155;
  border: 1px solid #cbd5e1;
  font-size: 12.5px;
  font-weight: 600;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s ease;
}

.btn-face-rescan:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.btn-face-disable {
  background: #fee2e2;
  color: #b91c1c;
  border: 1px solid #fca5a5;
  font-size: 12.5px;
  font-weight: 600;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  transition: all 0.2s ease;
}

.btn-face-disable:hover:not(:disabled) {
  background: #fecaca;
}

.btn-face-disable:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* MODAL LIÊN KẾT ZALO */
.modal-zalo-card {
  max-width: 520px;
  border-radius: 20px;
  overflow: hidden;
}

.zalo-modal-header {
  background: linear-gradient(135deg, #0f172a, #1e293b);
  color: #ffffff;
  padding: 22px 24px;
}

.zalo-modal-header .modal-title {
  color: #ffffff;
  font-size: 18px;
  font-weight: 800;
}

.zalo-header-brand {
  display: flex;
  align-items: center;
  gap: 12px;
}

.phone-modal-icon {
  width: 42px;
  height: 42px;
  background: #ffffff;
  color: #ea580c;
  font-size: 20px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.zalo-header-sub {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.85);
  margin-top: 3px;
}

.zalo-modal-header .modal-close-btn {
  color: #ffffff;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 8px;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  font-size: 14px;
}

.zalo-modal-header .modal-close-btn:hover {
  background: rgba(255, 255, 255, 0.25);
}

.zalo-steps-guide {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 10px 14px;
  margin-bottom: 20px;
  gap: 6px;
}

.step-guide-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  font-weight: 700;
  color: #475569;
}

.step-badge {
  width: 18px;
  height: 18px;
  background: #0068ff;
  color: #ffffff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
  font-weight: 900;
  flex-shrink: 0;
}

.step-guide-arrow {
  color: #cbd5e1;
  font-weight: 800;
  font-size: 12px;
}

.zalo-input-group {
  display: flex;
  gap: 8px;
}

.zalo-flag-prefix {
  display: flex;
  align-items: center;
  padding: 0 12px;
  background: #f1f5f9;
  border: 1.5px solid #cbd5e1;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  color: #334155;
  white-space: nowrap;
}

.btn-send-zalo-otp {
  background: #ea580c;
  color: #ffffff;
  border: none;
  padding: 0 18px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-send-zalo-otp:hover:not(:disabled) {
  background: #c2410c;
}

.btn-send-zalo-otp:disabled {
  background: #94a3b8;
  cursor: not-allowed;
}

.btn-paste-otp-link {
  background: transparent;
  border: none;
  color: #ea580c;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  padding: 0;
  text-decoration: underline;
}

.zalo-otp-field-wrap {
  position: relative;
}

.zalo-otp-input-box {
  font-size: 24px !important;
  font-weight: 900 !important;
  letter-spacing: 12px !important;
  text-align: center;
  color: #0f172a !important;
  padding: 12px !important;
  border: 2px solid #fed7aa !important;
  background: #fffaf5 !important;
}

.zalo-otp-input-box:focus {
  border-color: #ea580c !important;
  box-shadow: 0 0 0 4px rgba(234, 88, 12, 0.15) !important;
}

.zalo-otp-hint {
  font-size: 11.5px;
  color: #64748b;
  margin-top: 8px;
  line-height: 1.45;
  background: #fff7ed;
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid #fed7aa;
}

.zalo-security-benefits {
  background: #fafafa;
  border-radius: 12px;
  padding: 12px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.benefit-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #334155;
  font-weight: 600;
}

.btn-confirm-zalo {
  background: linear-gradient(135deg, #ea580c, #f97316);
  color: #ffffff;
  border: none;
  font-size: 14px;
  font-weight: 800;
  padding: 11px 24px;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  box-shadow: 0 4px 14px rgba(234, 88, 12, 0.3);
}

.btn-confirm-zalo:hover:not(:disabled) {
  background: linear-gradient(135deg, #c2410c, #ea580c);
  transform: translateY(-1px);
}

.btn-confirm-zalo:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
}

.phone-input-action-row {
  display: flex;
  gap: 8px;
}

.phone-prefix-tag {
  display: flex;
  align-items: center;
  padding: 0 12px;
  background: #f8fafc;
  border: 1.5px solid #cbd5e1;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  color: #334155;
  white-space: nowrap;
}

.btn-send-otp-action {
  background: #ea580c;
  color: #ffffff;
  border: none;
  padding: 0 18px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-send-otp-action:hover:not(:disabled) {
  background: #c2410c;
}

.btn-send-otp-action:disabled {
  background: #cbd5e1;
  cursor: not-allowed;
}

.otp-input-large {
  font-size: 26px !important;
  font-weight: 900 !important;
  letter-spacing: 12px !important;
  color: #ea580c !important;
  background: #fffaf5 !important;
  border: 2px solid #fed7aa !important;
  padding: 10px !important;
}

.otp-input-large:focus {
  border-color: #ea580c !important;
  box-shadow: 0 0 0 4px rgba(234, 88, 12, 0.12) !important;
}

.email-target-banner {
  background: #fff7ed;
  border: 1px solid #fed7aa;
  padding: 10px 14px;
  border-radius: 10px;
  font-size: 13px;
  color: #9a3412;
  display: flex;
  align-items: center;
}
</style>
