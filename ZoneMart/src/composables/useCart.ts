import { ref, computed } from "vue";

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
  discountType: "fixed" | "percent" | "shipping";
  discountValue: number;
  minOrder: number;
  description: string;
}

export const AVAILABLE_VOUCHERS: Voucher[] = [
  {
    code: "FREESHIP10K",
    title: "Miễn Phí Ship Hỏa Tốc",
    discountType: "shipping",
    discountValue: 15000,
    minOrder: 150000,
    description: "Giảm 15.000 ₫ phí vận chuyển cho đơn từ 150.000 ₫"
  },
  {
    code: "LOCALFRESH",
    title: "Giảm 10% Nông Sản Tươi",
    discountType: "percent",
    discountValue: 10,
    minOrder: 200000,
    description: "Giảm 10% tối đa 30.000 ₫ cho đơn nông sản từ 200.000 ₫"
  },
  {
    code: "CHAOBANMOI",
    title: "Quà Chào Bạn Mới",
    discountType: "fixed",
    discountValue: 20000,
    minOrder: 100000,
    description: "Giảm trực tiếp 20.000 ₫ cho đơn hàng từ 100.000 ₫"
  }
];

const INITIAL_CART: CartStoreGroup[] = [
  {
    storeId: "st_1",
    storeName: "ZoneMart Bách Hóa Cầu Giấy",
    distanceKm: 1.2,
    deliveryTime: "15 - 20 phút",
    note: "Chọn khay thịt tươi mới về sáng nay giúp em nhé!",
    items: [
      {
        id: "p1",
        name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng",
        price: 185000,
        originalPrice: 220000,
        quantity: 2,
        unit: "Khay 500g",
        image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=600&q=80",
        selected: true
      },
      {
        id: "p6",
        name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
        price: 190000,
        originalPrice: 225000,
        quantity: 1,
        unit: "Túi 5kg",
        image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=600&q=80",
        selected: true
      }
    ]
  },
  {
    storeId: "st_2",
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    deliveryTime: "20 - 25 phút",
    note: "Lấy dâu tây quả to mọng nhé tiệm.",
    items: [
      {
        id: "p2",
        name: "Hộp Dâu Tây Đà Lạt Tươi Ngọt Chuẩn VietGAP",
        price: 95000,
        originalPrice: 125000,
        quantity: 1,
        unit: "Hộp 500g",
        image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=600&q=80",
        selected: true
      }
    ]
  }
];

function loadSavedCart(): CartStoreGroup[] {
  try {
    const saved = localStorage.getItem("zonemart_cart");
    if (saved) {
      const parsed = JSON.parse(saved);
      if (Array.isArray(parsed) && parsed.length > 0) return parsed;
    }
  } catch (e) {
    console.error("Lỗi đọc giỏ hàng từ localStorage:", e);
  }
  return INITIAL_CART;
}

// Trạng thái giỏ hàng Reactive Singleton
const cartStores = ref<CartStoreGroup[]>(loadSavedCart());
const appliedVoucherCode = ref<string>("FREESHIP10K");

function persistCart() {
  try {
    localStorage.setItem("zonemart_cart", JSON.stringify(cartStores.value));
  } catch (e) {
    console.error("Lỗi lưu giỏ hàng vào localStorage:", e);
  }
}

export function useCart() {
  // Tổng số lượng tất cả món trong giỏ (dùng cho Badge Header)
  const totalCount = computed(() => {
    return cartStores.value.reduce((total, store) => {
      return total + store.items.reduce((sum, item) => sum + item.quantity, 0);
    }, 0);
  });

  // Tổng số lượng món ĐƯỢC CHỌN thanh toán
  const selectedItemsCount = computed(() => {
    return cartStores.value.reduce((total, store) => {
      return total + store.items.filter((i) => i.selected).reduce((sum, item) => sum + item.quantity, 0);
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
    return cartStores.value.filter((store) => store.items.some((i) => i.selected)).length;
  });

  // Phí ship cơ bản: 15.000 ₫/quán
  const baseShippingFee = computed(() => {
    return activeStoresCount.value * 15000;
  });

  // Voucher đang áp dụng
  const currentVoucher = computed(() => {
    if (!appliedVoucherCode.value) return null;
    return AVAILABLE_VOUCHERS.find((v) => v.code === appliedVoucherCode.value) || null;
  });

  // Giá trị giảm từ voucher
  const voucherDiscount = computed(() => {
    const v = currentVoucher.value;
    if (!v) return 0;
    if (subTotal.value < v.minOrder) return 0;

    if (v.discountType === "shipping") {
      return Math.min(v.discountValue, baseShippingFee.value);
    } else if (v.discountType === "percent") {
      const discount = Math.round((subTotal.value * v.discountValue) / 100);
      return Math.min(discount, 30000); // Tối đa 30.000 ₫
    } else if (v.discountType === "fixed") {
      return v.discountValue;
    }
    return 0;
  });

  // Phí ship sau giảm
  const estimatedShipping = computed(() => {
    if (currentVoucher.value?.discountType === "shipping") {
      return Math.max(0, baseShippingFee.value - voucherDiscount.value);
    }
    return baseShippingFee.value;
  });

  // Giảm giá vào tiền hàng
  const itemDiscount = computed(() => {
    if (currentVoucher.value?.discountType !== "shipping") {
      return voucherDiscount.value;
    }
    return 0;
  });

  // Tổng thanh toán cuối cùng
  const totalAmount = computed(() => {
    if (selectedItemsCount.value === 0) return 0;
    return Math.max(0, subTotal.value - itemDiscount.value + estimatedShipping.value);
  });

  // Kiểm tra đã chọn tất cả chưa
  const isAllSelected = computed({
    get() {
      if (cartStores.value.length === 0) return false;
      return cartStores.value.every((store) =>
        store.items.every((item) => item.selected)
      );
    },
    set(val: boolean) {
      toggleAllSelect(val);
    }
  });

  // Mốc Freeship hỏa tốc: 300.000 ₫
  const freeshipThreshold = 300000;
  const freeshipRemaining = computed(() => Math.max(0, freeshipThreshold - subTotal.value));
  const freeshipProgress = computed(() => {
    if (subTotal.value >= freeshipThreshold) return 100;
    return Math.min(100, Math.round((subTotal.value / freeshipThreshold) * 100));
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
  const addItem = (
    storeInfo: { storeId: string; storeName: string; distanceKm: number; deliveryTime?: string },
    itemData: { id: string; name: string; price: number; originalPrice?: number; image: string; unit?: string }
  ) => {
    let store = cartStores.value.find((s) => s.storeId === storeInfo.storeId);
    if (!store) {
      store = {
        storeId: storeInfo.storeId,
        storeName: storeInfo.storeName,
        distanceKm: storeInfo.distanceKm,
        deliveryTime: storeInfo.deliveryTime || "15 - 20 phút",
        items: []
      };
      cartStores.value.push(store);
    }

    const existingItem = store.items.find((i) => i.id === itemData.id);
    if (existingItem) {
      existingItem.quantity++;
      existingItem.selected = true;
    } else {
      store.items.push({
        ...itemData,
        quantity: 1,
        selected: true
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
    const found = AVAILABLE_VOUCHERS.find((v) => v.code.toUpperCase() === code.trim().toUpperCase());
    if (!found) {
      return { success: false, message: "Mã giảm giá không hợp lệ hoặc đã hết hạn!" };
    }
    if (subTotal.value < found.minOrder) {
      return {
        success: false,
        message: `Đơn hàng tối thiểu ${found.minOrder.toLocaleString("vi-VN")} ₫ để dùng mã này!`
      };
    }
    appliedVoucherCode.value = found.code;
    return { success: true, message: `Áp dụng thành công mã "${found.code}"!` };
  };

  // Hủy voucher
  const removeVoucher = () => {
    appliedVoucherCode.value = "";
  };

  // Xóa toàn bộ giỏ hàng
  const clearCart = () => {
    cartStores.value = [];
    persistCart();
  };

  return {
    cartStores,
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
    AVAILABLE_VOUCHERS
  };
}
