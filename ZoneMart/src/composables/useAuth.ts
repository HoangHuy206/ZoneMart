import { ref, computed } from 'vue';

export type UserRole = 'guest' | 'buyer' | 'seller' | 'shipper' | 'admin';

export interface UserProfile {
  id: string;
  fullName: string;
  phoneEmail: string;
  role: UserRole;
  avatarUrl?: string;
  walletBalance?: number;
  storeName?: string;
  vehiclePlate?: string;
}

// 4 bộ tài khoản mẫu chuẩn đại diện cho 4 vai trò
export const DEMO_USERS: Record<Exclude<UserRole, 'guest'>, UserProfile> = {
  buyer: {
    id: 'usr_buyer_01',
    fullName: 'Nguyễn Văn An',
    phoneEmail: 'an.nguyen@zonemart.vn',
    role: 'buyer',
    avatarUrl:
      'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=120',
    walletBalance: 250000,
  },
  seller: {
    id: 'usr_seller_01',
    fullName: 'Trần Thị Mai',
    phoneEmail: 'mai.tran@zonemart.vn',
    role: 'seller',
    storeName: 'Nông Sản Hữu Cơ Ba Vì',
    avatarUrl:
      'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=120',
    walletBalance: 12850000,
  },
  shipper: {
    id: 'usr_shipper_01',
    fullName: 'Lê Hoàng Nam',
    phoneEmail: 'nam.le@zonemart.vn',
    role: 'shipper',
    vehiclePlate: '29N1-67890 (Wave Alpha)',
    avatarUrl:
      'https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?auto=format&fit=crop&w=120',
    walletBalance: 850000,
  },
  admin: {
    id: 'usr_admin_01',
    fullName: 'Quản Trị Viên ZoneMart',
    phoneEmail: 'admin@zonemart.vn',
    role: 'admin',
    avatarUrl:
      'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?auto=format&fit=crop&w=120',
    walletBalance: 50000000,
  },
};

function loadInitialUser(): UserProfile | null {
  try {
    const saved = localStorage.getItem('currentUser');
    if (saved) {
      return JSON.parse(saved);
    }
    // Tương thích ngược với key isLoggedIn cũ
    const isOldLogged = localStorage.getItem('isLoggedIn') === 'true';
    const oldRole = localStorage.getItem('userRole') as Exclude<
      UserRole,
      'guest'
    >;
    if (isOldLogged && oldRole && DEMO_USERS[oldRole]) {
      return DEMO_USERS[oldRole];
    }
  } catch (e) {
    console.error('Lỗi đọc trạng thái tài khoản:', e);
  }
  return null;
}

// Trạng thái Singleton chia sẻ toàn ứng dụng
const currentUser = ref<UserProfile | null>(loadInitialUser());

export function useAuth() {
  const isLoggedIn = computed(() => currentUser.value !== null);
  const currentRole = computed<UserRole>(
    () => currentUser.value?.role || 'guest',
  );

  const roleLabel = computed(() => {
    switch (currentRole.value) {
      case 'buyer':
        return 'Khách Hàng';
      case 'seller':
        return 'Chủ Cửa Hàng';
      case 'shipper':
        return 'Tài Xế Shipper';
      case 'admin':
        return 'Quản Trị Viên';
      default:
        return 'Khách Ghé Thăm';
    }
  });

  const roleBadgeClass = computed(() => {
    switch (currentRole.value) {
      case 'buyer':
        return 'badge-role-buyer';
      case 'seller':
        return 'badge-role-seller';
      case 'shipper':
        return 'badge-role-shipper';
      case 'admin':
        return 'badge-role-admin';
      default:
        return 'badge-role-guest';
    }
  });

  const formatVND = (val?: number) => {
    if (val === undefined || val === null) return '0 đ';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(val);
  };

  const login = (user: UserProfile) => {
    currentUser.value = user;
    localStorage.setItem('currentUser', JSON.stringify(user));
    localStorage.setItem('isLoggedIn', 'true');
    localStorage.setItem('userRole', user.role);
  };

  const logout = () => {
    currentUser.value = null;
    localStorage.removeItem('currentUser');
    localStorage.removeItem('isLoggedIn');
    localStorage.removeItem('userRole');
  };

  const switchRole = (role: Exclude<UserRole, 'guest'>) => {
    const demo = DEMO_USERS[role];
    if (demo) {
      login(demo);
    }
  };

  return {
    currentUser,
    isLoggedIn,
    currentRole,
    roleLabel,
    roleBadgeClass,
    formatVND,
    login,
    logout,
    switchRole,
    DEMO_USERS,
  };
}
