import { ref, computed, watch } from 'vue';
import { useAuth } from './useAuth';
import { cartService } from '../services/cartService';

export interface CartItem {
  id: string;
  name: string;
  price: number;
  originalPrice?: number;
  quantity: number;
  image: string;
  unit?: string;
  selected: boolean;
}

export interface CartStoreGroup {
  storeId: string;
  storeName: string;
  distanceKm: number;
  deliveryTime: string;
  note?: string;
  items: CartItem[];
}

export interface Voucher {
  code: string;
  title: string;
  discountType: 'fixed' | 'percent' | 'shipping';
  discountValue: number;
  minOrder: number;
  description: string;
}

export const AVAILABLE_VOUCHERS: Voucher[] = [
  {
    code: 'FREESHIP10K',
    title: 'Miễn Phí Ship Đi Chợ',
    discountType: 'shipping',
    discountValue: 10000,
    minOrder: 50000,
    description: 'Miễn phí vận chuyển cho đơn đi chợ từ 50.000 ₫',
  },
  {
    code: 'LOCALFRESH',
    title: 'Giảm 10% Nông Sản Tươi',
    discountType: 'percent',
    discountValue: 10,
    minOrder: 80000,
    description: 'Giảm 10% tối đa 20.000 ₫ cho đơn nông sản từ 80.000 ₫',
  },
  {
    code: 'CHAOBANMOI',
    title: 'Quà Chào Bạn Mới',
    discountType: 'fixed',
    discountValue: 10000,
    minOrder: 50000,
    description: 'Giảm trực tiếp 10.000 ₫ cho đơn hàng đi chợ từ 50.000 ₫',
  },
];

/**
 * Xác định mã định danh giỏ hàng của chủ sở hữu hiện tại:
 * - Nếu đã đăng nhập: user_{id_or_email}
 * - Nếu là khách vãng lai: guest_{unique_id}
 * Giúp mỗi người dùng có một giỏ hàng độc lập 100%, không ai giống ai.
 */
export function getCartOwnerKey(): string {
  try {
    const auth = useAuth();
    const user = auth.currentUser.value;
    if (user && (user.id || user.phoneEmail)) {
      const raw = user.id || user.phoneEmail;
      return 'user_' + raw.toLowerCase().replace(/[^a-z0-9_-]/g, '_');
    }
  } catch (e) {
    // Có thể throw nếu gọi ngoài ngữ cảnh vue, tiếp tục fallback
  }

  try {
    const saved =
      localStorage.getItem('currentUser') ||
      localStorage.getItem('zonemart_user');
    if (saved) {
      const parsed = JSON.parse(saved);
      if (parsed && (parsed.id || parsed.phoneEmail || parsed.email)) {
        const raw = parsed.id || parsed.phoneEmail || parsed.email;
        return 'user_' + raw.toLowerCase().replace(/[^a-z0-9_-]/g, '_');
      }
    }
  } catch (e) {
    // ignore
  }

  try {
    let guestId = localStorage.getItem('zonemart_guest_id');
    if (!guestId) {
      guestId =
        'guest_' +
        Math.random().toString(36).substring(2, 9) +
        '_' +
        Date.now().toString(36);
      localStorage.setItem('zonemart_guest_id', guestId);
    }
    return guestId;
  } catch (e) {
    return 'guest_default';
  }
}

export function getStorageKeyForOwner(ownerKey?: string): string {
  const owner = ownerKey || getCartOwnerKey();
  return `zonemart_cart_${owner}`;
}

function cleanGuestCaches() {
  try {
    const keysToRemove: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (
        key &&
        (key.startsWith('zonemart_cart_guest_') ||
          key === 'zonemart_cart' ||
          key === 'zonemart_guest_id')
      ) {
        keysToRemove.push(key);
      }
    }
    keysToRemove.forEach((k) => localStorage.removeItem(k));
  } catch (e) {
    // ignore
  }
}

function loadSavedCart(): CartStoreGroup[] {
  // Dọn dẹp cache cũ và cache guest
  cleanGuestCaches();

  // NẾU CHƯA ĐĂNG NHẬP: LUÔN TRẢ VỀ GIỎ HÀNG RỖNG (0 MÓN)
  try {
    const saved =
      localStorage.getItem('currentUser') ||
      localStorage.getItem('zonemart_user');
    if (!saved) {
      return [];
    }
  } catch (e) {
    return [];
  }

  try {
    const key = getStorageKeyForOwner();
    const saved = localStorage.getItem(key);
    if (saved) {
      const parsed = JSON.parse(saved);
      if (Array.isArray(parsed)) {
        // Tự động loại bỏ dữ liệu mock seed cũ nếu còn tồn đọng trong localStorage
        const cleanList = parsed.filter(
          (s: any) =>
            s.note !== 'Chọn khay thịt tươi mới về sáng nay giúp em nhé!' &&
            s.note !== 'Lấy dâu tây quả to mọng nhé tiệm.',
        );
        if (cleanList.length !== parsed.length) {
          localStorage.setItem(key, JSON.stringify(cleanList));
        }
        return cleanList;
      }
    }
  } catch (e) {
    console.error('Lỗi đọc giỏ hàng từ localStorage:', e);
  }

  // MẶC ĐỊNH LUÔN RỖNG KHI MỚI VÀO (0 MÓN)
  return [];
}

// Trạng thái giỏ hàng Reactive Singleton
const cartStores = ref<CartStoreGroup[]>(loadSavedCart());
const appliedVoucherCode = ref<string>(''); // Không tự áp dụng mã giảm giá khi giỏ hàng trống

function persistCart(syncToDb = true) {
  const auth = useAuth();

  try {
    const key = getStorageKeyForOwner();
    localStorage.setItem(key, JSON.stringify(cartStores.value));
  } catch (e) {
    console.error('Lỗi lưu giỏ hàng vào localStorage:', e);
  }

  if (syncToDb && auth.isLoggedIn.value) {
    try {
      const userId = auth.currentUser.value?.id;
      if (userId) {
        cartService.saveUserCart(
          userId,
          cartStores.value,
          appliedVoucherCode.value,
        );
      }
    } catch {}
  }
}

// Nạp giỏ hàng từ Database MongoDB Atlas
async function loadCartFromDatabase(userId?: string) {
  const auth = useAuth();
  const targetId = userId || auth.currentUser.value?.id;
  if (!targetId) {
    cartStores.value = [];
    appliedVoucherCode.value = '';
    return;
  }

  try {
    const dbData = await cartService.fetchUserCart(targetId);
    if (dbData && Array.isArray(dbData.stores)) {
      // Loại bỏ các món mock seed cũ nếu có
      const cleanStores = dbData.stores.filter(
        (s) =>
          s.note !== 'Chọn khay thịt tươi mới về sáng nay giúp em nhé!' &&
          s.note !== 'Lấy dâu tây quả to mọng nhé tiệm.',
      );
      cartStores.value = cleanStores;
      appliedVoucherCode.value =
        cleanStores.length > 0 ? dbData.voucherCode || '' : '';
      persistCart(false);
    } else {
      cartStores.value = [];
      appliedVoucherCode.value = '';
      persistCart(false);
    }
  } catch (e) {
    console.warn('Lỗi nạp giỏ hàng từ database, tiếp tục dùng local:', e);
  }
}

// Đồng bộ chuyển đổi giỏ hàng khi người dùng đăng nhập / đăng xuất / đổi vai trò
const auth = useAuth();
watch(
  () =>
    auth.currentUser.value
      ? `${auth.currentUser.value.id || ''}_${auth.currentUser.value.phoneEmail || ''}`
      : null,
  (userKey) => {
    cleanGuestCaches();
    if (!userKey || !auth.isLoggedIn.value) {
      cartStores.value = [];
      appliedVoucherCode.value = '';
    } else {
      cartStores.value = loadSavedCart();
      const targetId =
        auth.currentUser.value?.id || auth.currentUser.value?.phoneEmail;
      if (targetId) {
        loadCartFromDatabase(targetId);
      }
    }
  },
  { immediate: true },
);

/**
 * Ước tính trọng lượng món hàng (kg) dựa vào đơn vị quy cách và số lượng
 */
export function estimateItemWeightKg(item: CartItem): number {
  const u = `${item.unit || ''} ${item.name || ''}`.toLowerCase();
  let unitKg = 0.5; // Mặc định 0.5kg cho 1 món thực phẩm thông thường

  if (u.includes('10kg')) unitKg = 10.0;
  else if (u.includes('5kg')) unitKg = 5.0;
  else if (u.includes('3kg')) unitKg = 3.0;
  else if (u.includes('2kg') || u.includes('2l') || u.includes('2 lít')) unitKg = 2.0;
  else if (u.includes('1kg') || u.includes('1l') || u.includes('1 lít') || u.includes('1000ml')) unitKg = 1.0;
  else if (u.includes('800ml') || u.includes('850g')) unitKg = 0.85;
  else if (u.includes('500g') || u.includes('500ml')) unitKg = 0.5;
  else if (u.includes('300g') || u.includes('380g')) unitKg = 0.38;
  else if (u.includes('200g')) unitKg = 0.2;
  else if (u.includes('100g')) unitKg = 0.1;

  return Math.max(0.1, unitKg * (item.quantity || 1));
}

export interface StoreShippingFeeResult {
  distanceFee: number;
  weightFee: number;
  expressFee: number;
  totalFee: number;
  distanceKm: number;
  totalWeightKg: number;
  isOutOfRange: boolean;
}

/**
 * Tính phí vận chuyển theo cự ly (bán kính tối đa 5km) và khối lượng món hàng:
 * - 0 - 1.5 km: 5.000 ₫
 * - 1.5 - 3.0 km: 8.000 ₫
 * - 3.0 - 5.0 km: 12.000 ₫
 * - > 5.0 km: Vượt quá bán kính phục vụ
 * - Phụ phí khối lượng: 3kg - 5kg (+3.000 ₫), >= 5kg (+5.000 ₫)
 * - Phụ phí hỏa tốc 15-20p: +3.000 ₫
 */
export function calculateStoreShippingFee(store: CartStoreGroup, isExpress = false): StoreShippingFeeResult {
  const selectedItems = (store.items || []).filter((i) => i.selected);
  if (selectedItems.length === 0) {
    return {
      distanceFee: 0,
      weightFee: 0,
      expressFee: 0,
      totalFee: 0,
      distanceKm: 0,
      totalWeightKg: 0,
      isOutOfRange: false
    };
  }

  const rawDist = Number(store.distanceKm) || 0.8;
  const dist = Math.round(rawDist * 10) / 10;
  const isOutOfRange = dist > 5.0;

  // 1. Phí theo khoảng cách (Km)
  let distanceFee = 5000; // Mặc định bán kính gần 0 - 1.5km: 5.000 ₫
  if (dist > 1.5 && dist <= 3.0) {
    distanceFee = 8000;
  } else if (dist > 3.0) {
    distanceFee = 12000;
  }

  // 2. Phụ phí khối lượng món hàng
  const totalWeightKg = Math.round(
    selectedItems.reduce((acc, it) => acc + estimateItemWeightKg(it), 0) * 10
  ) / 10;
  let weightFee = 0;
  if (totalWeightKg >= 5.0) {
    weightFee = 5000;
  } else if (totalWeightKg >= 3.0) {
    weightFee = 3000;
  }

  // 3. Phụ phí hỏa tốc
  const expressFee = isExpress ? 3000 : 0;

  const totalFee = distanceFee + weightFee + expressFee;

  return {
    distanceFee,
    weightFee,
    expressFee,
    totalFee,
    distanceKm: dist,
    totalWeightKg,
    isOutOfRange
  };
}

export function useCart() {
  // Tổng số lượng tất cả món trong giỏ (dùng cho Badge Header - nếu chưa đăng nhập luôn trả về 0)
  const totalCount = computed(() => {
    if (!auth.isLoggedIn.value) {
      return 0;
    }
    return cartStores.value.reduce((total, store) => {
      return total + store.items.reduce((sum, item) => sum + item.quantity, 0);
    }, 0);
  });

  // Tổng số lượng món ĐƯỢC CHỌN thanh toán
  const selectedItemsCount = computed(() => {
    return cartStores.value.reduce((total, store) => {
      return (
        total +
        store.items
          .filter((i) => i.selected)
          .reduce((sum, item) => sum + item.quantity, 0)
      );
    }, 0);
  });

  // Tiền hàng các món được chọn
  const subTotal = computed(() => {
    return cartStores.value.reduce((total, store) => {
      return (
        total +
        store.items
          .filter((i) => i.selected)
          .reduce((sum, item) => sum + item.price * item.quantity, 0)
      );
    }, 0);
  });

  // Số cửa hàng có ít nhất 1 món được chọn
  const activeStoresCount = computed(() => {
    return cartStores.value.filter((store) =>
      store.items.some((i) => i.selected),
    ).length;
  });


  // Mốc Freeship đi chợ: 99.000 ₫ (phù hợp với quy mô đơn hàng đi chợ hàng ngày)
  const freeshipThreshold = 99000;
  const isFreeshipEligible = computed(() => subTotal.value >= freeshipThreshold);

  // Phí ship động theo Km cự ly thực tế (bán kính <= 5km) + Khối lượng món hàng:
  // Đơn hàng từ 99.000 ₫ trở lên tự động MIỄN PHÍ VẬN CHUYỂN (0 ₫).
  const baseShippingFee = computed(() => {
    if (activeStoresCount.value === 0) return 0;
    if (isFreeshipEligible.value) return 0;

    return cartStores.value
      .filter((store) => store.items.some((i) => i.selected))
      .reduce((sum, store) => {
        const feeInfo = calculateStoreShippingFee(store, false);
        return sum + feeInfo.totalFee;
      }, 0);
  });

  // Tổng trọng lượng các món đã chọn (kg)
  const selectedTotalWeightKg = computed(() => {
    return Math.round(
      cartStores.value.reduce((total, store) => {
        return (
          total +
          store.items
            .filter((i) => i.selected)
            .reduce((sum, item) => sum + estimateItemWeightKg(item), 0)
        );
      }, 0) * 10
    ) / 10;
  });

  // Kiểm tra có cửa hàng nào vượt quá bán kính 5km không
  const hasOutOfRangeStore = computed(() => {
    return cartStores.value.some((store) =>
      store.items.some((i) => i.selected) && (store.distanceKm || 0) > 5.0
    );
  });

  // Voucher đang áp dụng
  const currentVoucher = computed(() => {
    if (!appliedVoucherCode.value) return null;
    return (
      AVAILABLE_VOUCHERS.find((v) => v.code === appliedVoucherCode.value) ||
      null
    );
  });

  // Giá trị giảm từ voucher
  const voucherDiscount = computed(() => {
    const v = currentVoucher.value;
    if (!v) return 0;
    if (subTotal.value < v.minOrder) return 0;

    if (v.discountType === 'shipping') {
      return Math.min(v.discountValue, baseShippingFee.value);
    } else if (v.discountType === 'percent') {
      const discount = Math.round((subTotal.value * v.discountValue) / 100);
      return Math.min(discount, 30000); // Tối đa 30.000 ₫
    } else if (v.discountType === 'fixed') {
      return v.discountValue;
    }
    return 0;
  });

  // Phí ship sau giảm
  const estimatedShipping = computed(() => {
    if (currentVoucher.value?.discountType === 'shipping') {
      return Math.max(0, baseShippingFee.value - voucherDiscount.value);
    }
    return baseShippingFee.value;
  });

  // Giảm giá vào tiền hàng
  const itemDiscount = computed(() => {
    if (currentVoucher.value?.discountType !== 'shipping') {
      return voucherDiscount.value;
    }
    return 0;
  });

  // Tổng thanh toán cuối cùng
  const totalAmount = computed(() => {
    if (selectedItemsCount.value === 0) return 0;
    return Math.max(
      0,
      subTotal.value - itemDiscount.value + estimatedShipping.value,
    );
  });

  // Kiểm tra đã chọn tất cả chưa
  const isAllSelected = computed({
    get() {
      if (cartStores.value.length === 0) return false;
      return cartStores.value.every((store) =>
        store.items.every((item) => item.selected),
      );
    },
    set(val: boolean) {
      toggleAllSelect(val);
    },
  });

  const freeshipRemaining = computed(() =>
    Math.max(0, freeshipThreshold - subTotal.value),
  );
  const freeshipProgress = computed(() => {
    if (subTotal.value >= freeshipThreshold) return 100;
    return Math.min(
      100,
      Math.round((subTotal.value / freeshipThreshold) * 100),
    );
  });

  // Tăng số lượng món
  const increaseQty = (storeId: string, itemId: string) => {
    const store = cartStores.value.find((s) => s.storeId === storeId);
    if (!store) return;
    const item = store.items.find((i) => i.id === itemId);
    if (item) {
      item.quantity++;
      persistCart();
    }
  };

  // Giảm số lượng món
  const decreaseQty = (storeId: string, itemId: string) => {
    const sIdx = cartStores.value.findIndex((s) => s.storeId === storeId);
    if (sIdx === -1) return;
    const store = cartStores.value[sIdx];
    const iIdx = store.items.findIndex((i) => i.id === itemId);
    if (iIdx === -1) return;

    if (store.items[iIdx].quantity > 1) {
      store.items[iIdx].quantity--;
    } else {
      store.items.splice(iIdx, 1);
      if (store.items.length === 0) {
        cartStores.value.splice(sIdx, 1);
      }
    }
    persistCart();
  };

  // Xóa món khỏi giỏ
  const removeItem = (storeId: string, itemId: string) => {
    const sIdx = cartStores.value.findIndex((s) => s.storeId === storeId);
    if (sIdx === -1) return;
    const store = cartStores.value[sIdx];
    const iIdx = store.items.findIndex((i) => i.id === itemId);
    if (iIdx !== -1) {
      store.items.splice(iIdx, 1);
      if (store.items.length === 0) {
        cartStores.value.splice(sIdx, 1);
      }
      persistCart();
    }
  };

  // Thêm món vào giỏ
  // Thêm món vào giỏ (hỗ trợ số lượng tùy chọn qty)
  const addItem = (
    storeInfo: {
      storeId: string;
      storeName: string;
      distanceKm: number;
      deliveryTime?: string;
    },
    itemData: {
      id: string;
      name: string;
      price: number;
      originalPrice?: number;
      image: string;
      unit?: string;
    },
    qty: number = 1,
  ) => {
    let store = cartStores.value.find((s) => s.storeId === storeInfo.storeId);
    if (!store) {
      store = {
        storeId: storeInfo.storeId,
        storeName: storeInfo.storeName,
        distanceKm: storeInfo.distanceKm,
        deliveryTime: storeInfo.deliveryTime || '15 - 20 phút',
        items: [],
      };
      cartStores.value.push(store);
    }

    const count = Math.max(1, qty);
    const existingItem = store.items.find((i) => i.id === itemData.id);
    if (existingItem) {
      existingItem.quantity += count;
      existingItem.selected = true;
    } else {
      store.items.push({
        ...itemData,
        quantity: count,
        selected: true,
      });
    }
    persistCart();
  };

  // Chọn / bỏ chọn 1 cửa hàng
  const toggleStoreSelect = (storeId: string, selected: boolean) => {
    const store = cartStores.value.find((s) => s.storeId === storeId);
    if (store) {
      store.items.forEach((item) => (item.selected = selected));
      persistCart();
    }
  };

  // Chọn / bỏ chọn tất cả
  const toggleAllSelect = (selected: boolean) => {
    cartStores.value.forEach((store) => {
      store.items.forEach((item) => (item.selected = selected));
    });
    persistCart();
  };

  // Cập nhật ghi chú cửa hàng
  const updateStoreNote = (storeId: string, note: string) => {
    const store = cartStores.value.find((s) => s.storeId === storeId);
    if (store) {
      store.note = note;
      persistCart();
    }
  };

  // Áp dụng voucher
  const applyVoucher = (code: string) => {
    const found = AVAILABLE_VOUCHERS.find(
      (v) => v.code.toUpperCase() === code.trim().toUpperCase(),
    );
    if (!found) {
      return {
        success: false,
        message: 'Mã giảm giá không hợp lệ hoặc đã hết hạn!',
      };
    }
    if (subTotal.value < found.minOrder) {
      return {
        success: false,
        message: `Đơn hàng tối thiểu ${found.minOrder.toLocaleString('vi-VN')} ₫ để dùng mã này!`,
      };
    }
    appliedVoucherCode.value = found.code;
    return { success: true, message: `Áp dụng thành công mã "${found.code}"!` };
  };

  // Hủy voucher
  const removeVoucher = () => {
    appliedVoucherCode.value = '';
  };

  // Xóa toàn bộ giỏ hàng
  const clearCart = () => {
    cartStores.value = [];
    appliedVoucherCode.value = '';
    persistCart();
  };

  // Xóa các món đã chọn sau khi đặt hàng thành công
  const removeSelectedItems = () => {
    cartStores.value = cartStores.value
      .map((store) => ({
        ...store,
        items: store.items.filter((item) => !item.selected),
      }))
      .filter((store) => store.items.length > 0);
    persistCart();
  };

  // Danh sách nhóm cửa hàng có món đang được chọn
  const selectedStoreGroups = computed(() => {
    return cartStores.value
      .map((store) => ({
        ...store,
        items: store.items.filter((item) => item.selected),
      }))
      .filter((store) => store.items.length > 0);
  });

  const refreshCartForUser = () => {
    cartStores.value = loadSavedCart();
    appliedVoucherCode.value = '';
  };

  return {
    cartStores,
    selectedStoreGroups,
    totalCount,
    selectedItemsCount,
    subTotal,
    activeStoresCount,
    baseShippingFee,
    currentVoucher,
    appliedVoucherCode,
    voucherDiscount,
    estimatedShipping,
    totalAmount,
    isAllSelected,
    freeshipThreshold,
    freeshipRemaining,
    freeshipProgress,
    increaseQty,
    decreaseQty,
    removeItem,
    addItem,
    toggleStoreSelect,
    toggleAllSelect,
    updateStoreNote,
    applyVoucher,
    removeVoucher,
    clearCart,
    removeSelectedItems,
    loadCartFromDatabase,
    refreshCartForUser,
    AVAILABLE_VOUCHERS,
    selectedTotalWeightKg,
    hasOutOfRangeStore,
    calculateStoreShippingFee,
    estimateItemWeightKg,
  };
}
