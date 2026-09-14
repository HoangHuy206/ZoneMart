<script setup lang="ts">
/**
 * ============================================================================
 * TRANG THANH TOÁN (CHECKOUT VIEW) - ZONEMART
 * Phong cách thiết kế: Warm Humanist & Terracotta (Nông sản & Bách hóa 10km)
 * Kết nối 100% Database MongoDB Atlas qua Backend ASP.NET Core (.NET 9)
 * Phụ trách: Thắng
 * ============================================================================
 */
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useCart } from '../../composables/useCart';
import { useAuth } from '../../composables/useAuth';
import {
  orderService,
  type CheckoutPayload,
} from '../../services/orderService';

const router = useRouter();
const cart = useCart();
const auth = useAuth();

// 1. Quản lý Địa chỉ giao hàng
interface DeliveryAddress {
  id: string;
  tag: string;
  recipientName: string;
  phone: string;
  detail: string;
  isDefault: boolean;
  hubDistance: string;
}

const savedAddresses = ref<DeliveryAddress[]>([
  {
    id: 'addr_1',
    tag: 'Nhà riêng',
    recipientName: auth.currentUser.value?.fullName || 'Nguyễn Văn An',
    phone: auth.currentUser.value?.phoneEmail?.includes('@')
      ? '0912 345 678'
      : auth.currentUser.value?.phoneEmail || '0912 345 678',
    detail: 'Số 18, Ngõ 245 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội',
    isDefault: true,
    hubDistance: '1.2 km',
  },
  {
    id: 'addr_2',
    tag: 'Văn phòng',
    recipientName: auth.currentUser.value?.fullName || 'Nguyễn Văn An',
    phone: '0988 776 655',
    detail: 'Tầng 8, Tòa nhà Tech Tower, Phố Duy Tân, Quận Cầu Giấy, Hà Nội',
    isDefault: false,
    hubDistance: '2.8 km',
  },
]);

const selectedAddressId = ref<string>('addr_1');
const currentAddress = computed(() => {
  return (
    savedAddresses.value.find((a) => a.id === selectedAddressId.value) ||
    savedAddresses.value[0]
  );
});

// Modal chỉnh sửa / Thêm địa chỉ mới
const isAddressModalOpen = ref(false);
const editingAddress = ref({
  tag: 'Nhà riêng',
  recipientName: '',
  phone: '',
  detail: '',
});

const openAddressModal = () => {
  editingAddress.value = {
    tag: currentAddress.value.tag,
    recipientName: currentAddress.value.recipientName,
    phone: currentAddress.value.phone,
    detail: currentAddress.value.detail,
  };
  isAddressModalOpen.value = true;
};

const saveCustomAddress = () => {
  if (
    !editingAddress.value.recipientName.trim() ||
    !editingAddress.value.phone.trim() ||
    !editingAddress.value.detail.trim()
  ) {
    alert('Vui lòng điền đầy đủ họ tên, số điện thoại và địa chỉ giao hàng!');
    return;
  }
  const newAddrId = `addr_${Date.now()}`;
  savedAddresses.value.push({
    id: newAddrId,
    tag: editingAddress.value.tag || 'Địa chỉ mới',
    recipientName: editingAddress.value.recipientName,
    phone: editingAddress.value.phone,
    detail: editingAddress.value.detail,
    isDefault: false,
    hubDistance: '1.5 km',
  });
  selectedAddressId.value = newAddrId;
  isAddressModalOpen.value = false;
  triggerToast('Đã cập nhật địa chỉ nhận hàng thành công!');
};

// 2. Phương thức vận chuyển
type DeliveryType = 'express' | 'standard' | 'scheduled';
const selectedDeliveryType = ref<DeliveryType>('express');
const scheduledTimeSlot = ref('11:30 - 12:00 Hôm nay');

const deliveryFeeCalculated = computed(() => {
  const storeCount = Math.max(1, cart.activeStoresCount.value);
  if (selectedDeliveryType.value === 'express') {
    return storeCount * 18000; // 18.000đ/quán giao siêu tốc
  } else if (selectedDeliveryType.value === 'scheduled') {
    return storeCount * 16000;
  }
  return storeCount * 15000; // Tiêu chuẩn 15.000đ/quán
});

// 3. Phương thức thanh toán
type PaymentMethod = 'ONLINE_QR' | 'ZONEPAY_WALLET' | 'COD' | 'CREDIT_CARD';
const selectedPaymentMethod = ref<PaymentMethod>('ONLINE_QR');

// Số dư ví ZonePay lấy từ Database
const walletBalance = computed(() => {
  return auth.currentUser.value?.walletBalance ?? 1250000;
});

const isWalletSufficient = computed(() => {
  return walletBalance.value >= finalTotal.value;
});

// 4. Voucher & Tính tiền
const voucherInput = ref('');
const voucherAlert = ref<{ type: 'success' | 'error'; text: string } | null>(
  null,
);

const applyVoucherCode = (code?: string) => {
  const targetCode = code || voucherInput.value;
  if (!targetCode.trim()) {
    voucherAlert.value = { type: 'error', text: 'Vui lòng nhập mã giảm giá!' };
    return;
  }
  const res = cart.applyVoucher(targetCode);
  if (res.success) {
    voucherAlert.value = { type: 'success', text: res.message };
    voucherInput.value = '';
    triggerToast(res.message);
  } else {
    voucherAlert.value = { type: 'error', text: res.message };
  }
};

const discountAmount = computed(() => {
  return cart.voucherDiscount.value;
});

const finalTotal = computed(() => {
  return Math.max(
    0,
    cart.subTotal.value - discountAmount.value + deliveryFeeCalculated.value,
  );
});

// 5. Toast & Loading state
const toastMsg = ref('');
const showToast = ref(false);
const triggerToast = (msg: string) => {
  toastMsg.value = msg;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 2600);
};

// Modal Đặt hàng thành công
const isSubmitting = ref(false);
const showSuccessModal = ref(false);
const createdOrderId = ref('');
const copiedAccount = ref(false);

const copyAccountNumber = () => {
  navigator.clipboard.writeText('0912345678');
  copiedAccount.value = true;
  triggerToast('Đã sao chép số tài khoản MB Bank!');
  setTimeout(() => {
    copiedAccount.value = false;
  }, 2000);
};

// Xử lý Chốt Đặt Hàng
const handlePlaceOrder = async () => {
  if (cart.selectedItemsCount.value === 0) {
    alert('Không có sản phẩm nào được chọn để thanh toán!');
    router.push('/cart');
    return;
  }

  if (
    selectedPaymentMethod.value === 'ZONEPAY_WALLET' &&
    !isWalletSufficient.value
  ) {
    alert(
      'Số dư Ví ZonePay không đủ! Vui lòng chọn phương thức Chuyển khoản QR hoặc Tiền mặt COD.',
    );
    return;
  }

  isSubmitting.value = true;

  try {
    // Chuẩn bị payload gửi lên Database MongoDB Atlas
    const payload: CheckoutPayload = {
      buyerId: auth.currentUser.value?.id || 'usr_buyer_01',
      buyerName: currentAddress.value.recipientName,
      buyerPhone: currentAddress.value.phone,
      shippingAddress: currentAddress.value.detail,
      paymentMethod: selectedPaymentMethod.value,
      deliveryType: selectedDeliveryType.value,
      deliveryTimeSlot:
        selectedDeliveryType.value === 'scheduled'
          ? scheduledTimeSlot.value
          : undefined,
      voucherCode: cart.appliedVoucherCode.value || undefined,
      voucherDiscount: discountAmount.value,
      shippingFee: deliveryFeeCalculated.value,
      subTotal: cart.subTotal.value,
      totalAmount: finalTotal.value,
      stores: cart.selectedStoreGroups.value.map((store) => ({
        storeId: store.storeId,
        storeName: store.storeName,
        distanceKm: store.distanceKm,
        deliveryTime: store.deliveryTime,
        note: store.note,
        items: store.items.map((item) => ({
          id: item.id,
          name: item.name,
          price: item.price,
          quantity: item.quantity,
          image: item.image,
          unit: item.unit,
        })),
      })),
    };

    const res = await orderService.checkoutOrder(payload);

    if (res.success) {
      createdOrderId.value = res.orderId || `ZM_${Date.now()}`;

      // Xóa các món đã mua khỏi giỏ hàng
      cart.removeSelectedItems();

      // Hiển thị modal chúc mừng đặt hàng thành công
      showSuccessModal.value = true;
    } else {
      alert(res.message || 'Có lỗi xảy ra khi tạo đơn hàng. Vui lòng thử lại!');
    }
  } catch (error: any) {
    console.error('Lỗi thanh toán:', error);
    alert('Đã xảy ra lỗi trong quá trình tạo đơn hàng. Vui lòng thử lại sau.');
  } finally {
    isSubmitting.value = false;
  }
};

const goToOrdersTracking = () => {
  showSuccessModal.value = false;
  router.push('/buyer-orders');
};
</script>

<template>
  <div class="checkout-page-wrapper">
    <!-- 1. THANH TIẾN TRÌNH ĐẶT HÀNG (STEPPER TRACK) -->
    <div class="checkout-stepper-container">
      <div class="stepper-track">
        <router-link to="/cart" class="step-node completed">
          <div class="node-circle"><i class="bi bi-check-lg"></i></div>
          <span class="node-label">1. Giỏ Hàng</span>
        </router-link>
        <div class="step-connector active"></div>
        <div class="step-node active">
          <div class="node-circle"><i class="bi bi-credit-card-fill"></i></div>
          <span class="node-label">2. Thanh Toán & Đặt Hàng</span>
        </div>
        <div class="step-connector"></div>
        <div class="step-node">
          <div class="node-circle"><i class="bi bi-truck"></i></div>
          <span class="node-label">3. Giao Hỏa Tốc (10km)</span>
        </div>
      </div>
    </div>

    <!-- 2. NỘI DUNG CHÍNH NẾU CÓ MÓN ĐƯỢC CHỌN -->
    <div v-if="cart.selectedItemsCount.value > 0" class="checkout-main-grid">
      <!-- ================= CỘT TRÁI: CHI TIẾT ĐẶT HÀNG (65%) ================= -->
      <div class="checkout-details-column">
        <!-- KHỐI 1: ĐỊA CHỈ NHẬN HÀNG -->
        <section class="checkout-card address-card">
          <div class="card-title-row">
            <div class="title-with-icon">
              <span class="icon-circle icon-terracotta">
                <i class="bi bi-geo-alt-fill"></i>
              </span>
              <div>
                <h3 class="card-main-title">1. Địa Chỉ Nhận Hàng</h3>
                <p class="card-sub-title">
                  Giao nhanh trong phạm vi 10km từ các Hub nông sản
                </p>
              </div>
            </div>
            <button
              type="button"
              class="btn-text-action"
              @click="openAddressModal"
            >
              <i class="bi bi-pencil-square"></i>
              <span>Thay đổi địa chỉ</span>
            </button>
          </div>

          <!-- Lựa chọn nhanh địa chỉ -->
          <div class="saved-address-chips">
            <button
              v-for="addr in savedAddresses"
              :key="addr.id"
              type="button"
              class="address-chip"
              :class="{ selected: selectedAddressId === addr.id }"
              @click="selectedAddressId = addr.id"
            >
              <i
                class="bi"
                :class="
                  addr.tag === 'Nhà riêng'
                    ? 'bi-house-door-fill'
                    : 'bi-building'
                "
              ></i>
              <span>{{ addr.tag }}</span>
              <span v-if="addr.isDefault" class="default-badge">Mặc định</span>
            </button>
          </div>

          <!-- Thông tin địa chỉ đang chọn -->
          <div class="active-address-box">
            <div class="recipient-row">
              <span class="recipient-name">{{
                currentAddress.recipientName
              }}</span>
              <span class="recipient-divider">•</span>
              <span class="recipient-phone">{{ currentAddress.phone }}</span>
              <span class="hub-radius-badge">
                <i class="bi bi-radar"></i> Bán kính:
                {{ currentAddress.hubDistance }}
              </span>
            </div>
            <p class="recipient-address-text">{{ currentAddress.detail }}</p>
          </div>
        </section>

        <!-- KHỐI 2: DANH SÁCH MÓN THEO CỬA HÀNG (SUB-ORDERS) -->
        <section class="checkout-card store-items-card">
          <div class="card-title-row">
            <div class="title-with-icon">
              <span class="icon-circle icon-green">
                <i class="bi bi-shop-window"></i>
              </span>
              <div>
                <h3 class="card-main-title">
                  2. Kiện Hàng Theo Cửa Hàng ({{ cart.activeStoresCount.value }}
                  Quán)
                </h3>
                <p class="card-sub-title">
                  Đơn hàng được chia thành các kiện riêng để Shipper lấy nhanh
                  nhất
                </p>
              </div>
            </div>
            <router-link to="/cart" class="btn-text-action">
              <i class="bi bi-arrow-left"></i>
              <span>Sửa giỏ hàng</span>
            </router-link>
          </div>

          <!-- Danh sách từng cửa hàng -->
          <div class="stores-group-list">
            <div
              v-for="store in cart.selectedStoreGroups.value"
              :key="store.storeId"
              class="store-package-block"
            >
              <div class="store-package-head">
                <div class="store-brand">
                  <i class="bi bi-shop"></i>
                  <span class="store-name-text">{{ store.storeName }}</span>
                </div>
                <div class="store-delivery-tag">
                  <i class="bi bi-clock-history"></i>
                  <span
                    >Dự kiến: {{ store.deliveryTime || '15 - 25 phút' }}</span
                  >
                  <span class="distance-text">({{ store.distanceKm }} km)</span>
                </div>
              </div>

              <!-- Danh sách món trong quán này -->
              <div class="package-items-table">
                <div
                  v-for="item in store.items"
                  :key="item.id"
                  class="package-item-row"
                >
                  <img :src="item.image" :alt="item.name" class="item-thumb" />
                  <div class="item-info-col">
                    <h4 class="item-name">{{ item.name }}</h4>
                    <span v-if="item.unit" class="item-unit">{{
                      item.unit
                    }}</span>
                  </div>
                  <div class="item-qty-col">x{{ item.quantity }}</div>
                  <div class="item-price-col">
                    {{ (item.price * item.quantity).toLocaleString('vi-VN') }} ₫
                  </div>
                </div>
              </div>

              <!-- Ghi chú cho quán này -->
              <div class="store-note-row">
                <i class="bi bi-chat-left-dots"></i>
                <input
                  type="text"
                  class="store-note-input"
                  v-model="store.note"
                  placeholder="Ghi chú cho quán (ví dụ: lấy quả chín, gói cẩn thận...)"
                />
              </div>
            </div>
          </div>
        </section>

        <!-- KHỐI 3: PHƯƠNG THỨC VẬN CHUYỂN -->
        <section class="checkout-card shipping-card">
          <div class="card-title-row">
            <div class="title-with-icon">
              <span class="icon-circle icon-orange">
                <i class="bi bi-lightning-charge-fill"></i>
              </span>
              <div>
                <h3 class="card-main-title">3. Phương Thức Vận Chuyển 10km</h3>
                <p class="card-sub-title">
                  Đội ngũ Shipper ZoneMart giao hỏa tốc bằng thùng giữ nhiệt
                </p>
              </div>
            </div>
          </div>

          <div class="shipping-option-grid">
            <!-- Hỏa Tốc -->
            <label
              class="shipping-radio-card"
              :class="{ selected: selectedDeliveryType === 'express' }"
            >
              <input
                type="radio"
                name="deliveryType"
                value="express"
                v-model="selectedDeliveryType"
              />
              <div class="shipping-card-body">
                <div class="shipping-card-top">
                  <div class="shipping-title-wrap">
                    <span class="badge-express-fast">
                      <i class="bi bi-lightning-charge-fill"></i> HỎA TỐC
                    </span>
                    <strong>Giao Siêu Tốc (15 - 25 phút)</strong>
                  </div>
                  <span class="shipping-fee-val">
                    {{
                      (
                        18000 * Math.max(1, cart.activeStoresCount.value)
                      ).toLocaleString('vi-VN')
                    }}
                    ₫
                  </span>
                </div>
                <p class="shipping-desc">
                  Ưu tiên số 1: Shipper nhận đơn và lấy hàng ngay tại vườn/quán
                  giao thẳng đến bạn.
                </p>
              </div>
            </label>

            <!-- Tiêu Chuẩn -->
            <label
              class="shipping-radio-card"
              :class="{ selected: selectedDeliveryType === 'standard' }"
            >
              <input
                type="radio"
                name="deliveryType"
                value="standard"
                v-model="selectedDeliveryType"
              />
              <div class="shipping-card-body">
                <div class="shipping-card-top">
                  <div class="shipping-title-wrap">
                    <span class="badge-standard">
                      <i class="bi bi-bicycle"></i> TIÊU CHUẨN
                    </span>
                    <strong>Giao Tiêu Chuẩn (30 - 45 phút)</strong>
                  </div>
                  <span class="shipping-fee-val">
                    {{
                      (
                        15000 * Math.max(1, cart.activeStoresCount.value)
                      ).toLocaleString('vi-VN')
                    }}
                    ₫
                  </span>
                </div>
                <p class="shipping-desc">
                  Tối ưu chi phí cho đơn hàng trong bán kính 3 - 10km quanh khu
                  vực Hub.
                </p>
              </div>
            </label>

            <!-- Hẹn Giờ -->
            <label
              class="shipping-radio-card"
              :class="{ selected: selectedDeliveryType === 'scheduled' }"
            >
              <input
                type="radio"
                name="deliveryType"
                value="scheduled"
                v-model="selectedDeliveryType"
              />
              <div class="shipping-card-body">
                <div class="shipping-card-top">
                  <div class="shipping-title-wrap">
                    <span class="badge-scheduled">
                      <i class="bi bi-calendar-check"></i> HẸN GIỜ
                    </span>
                    <strong>Giao Theo Khung Giờ Chọn</strong>
                  </div>
                  <span class="shipping-fee-val">
                    {{
                      (
                        16000 * Math.max(1, cart.activeStoresCount.value)
                      ).toLocaleString('vi-VN')
                    }}
                    ₫
                  </span>
                </div>
                <p class="shipping-desc">
                  Chọn khung giờ thuận tiện để nhận đồ ăn trưa hoặc bữa tối ấm
                  áp.
                </p>
                <div
                  v-if="selectedDeliveryType === 'scheduled'"
                  class="scheduled-selector-wrap"
                >
                  <select v-model="scheduledTimeSlot" class="timeslot-select">
                    <option value="11:30 - 12:00 Hôm nay">
                      11:30 - 12:00 Trưa nay
                    </option>
                    <option value="12:00 - 12:30 Hôm nay">
                      12:00 - 12:30 Trưa nay
                    </option>
                    <option value="17:30 - 18:00 Hôm nay">
                      17:30 - 18:00 Chiều tối nay
                    </option>
                    <option value="18:30 - 19:00 Hôm nay">
                      18:30 - 19:00 Chiều tối nay
                    </option>
                    <option value="08:00 - 09:00 Sáng mai">
                      08:00 - 09:00 Sáng mai
                    </option>
                  </select>
                </div>
              </div>
            </label>
          </div>
        </section>

        <!-- KHỐI 4: PHƯƠNG THỨC THANH TOÁN -->
        <section class="checkout-card payment-card">
          <div class="card-title-row">
            <div class="title-with-icon">
              <span class="icon-circle icon-blue">
                <i class="bi bi-wallet2"></i>
              </span>
              <div>
                <h3 class="card-main-title">4. Phương Thức Thanh Toán</h3>
                <p class="card-sub-title">
                  Bảo mật mã hóa đa tầng, hỗ trợ VietQR và Ví ZonePay
                </p>
              </div>
            </div>
          </div>

          <div class="payment-method-options">
            <!-- 1. VietQR Code -->
            <label
              class="pay-method-radio-box"
              :class="{ selected: selectedPaymentMethod === 'ONLINE_QR' }"
            >
              <div class="radio-label-top">
                <div class="radio-left">
                  <input
                    type="radio"
                    name="payMethod"
                    value="ONLINE_QR"
                    v-model="selectedPaymentMethod"
                  />
                  <div class="pay-info">
                    <span class="pay-name">
                      <i class="bi bi-qr-code-scan text-primary"></i>
                      <strong>Chuyển Khoản VietQR (Khuyên Dùng)</strong>
                    </span>
                    <span class="pay-subtext"
                      >Quét mã QR tự động điền đúng số tiền và nội dung qua mọi
                      App Ngân hàng & MoMo</span
                    >
                  </div>
                </div>
                <span class="recommend-badge">Tiện lợi nhất</span>
              </div>

              <!-- Khung hiển thị VietQR chi tiết khi được chọn -->
              <div
                v-if="selectedPaymentMethod === 'ONLINE_QR'"
                class="vietqr-live-preview"
              >
                <div class="qr-code-visual">
                  <img
                    :src="`https://api.qrserver.com/v1/create-qr-code/?size=160x160&data=https://zonemart.vn/pay?amt=${finalTotal}&msg=ZM_ORDER`"
                    alt="VietQR ZoneMart"
                    class="qr-img"
                  />
                  <span class="qr-scan-hint"
                    ><i class="bi bi-phone"></i> Quét mã để trả
                    {{ finalTotal.toLocaleString('vi-VN') }} ₫</span
                  >
                </div>
                <div class="bank-details-side">
                  <div class="bank-row">
                    <span class="lbl">Ngân hàng:</span>
                    <strong class="val">MB Bank (Quân Đội)</strong>
                  </div>
                  <div class="bank-row">
                    <span class="lbl">Số tài khoản:</span>
                    <div class="copy-acc-wrap">
                      <strong class="val highlight">0912 345 678</strong>
                      <button
                        type="button"
                        class="btn-copy-sm"
                        @click="copyAccountNumber"
                      >
                        <i
                          class="bi"
                          :class="
                            copiedAccount ? 'bi-check-all' : 'bi-clipboard'
                          "
                        ></i>
                        <span>{{
                          copiedAccount ? 'Đã chép' : 'Sao chép'
                        }}</span>
                      </button>
                    </div>
                  </div>
                  <div class="bank-row">
                    <span class="lbl">Chủ tài khoản:</span>
                    <strong class="val"
                      >CTCP THƯƠNG MẠI ZONEMART VIETNAM</strong
                    >
                  </div>
                  <div class="bank-row">
                    <span class="lbl">Số tiền:</span>
                    <strong class="val text-terracotta fs-16"
                      >{{ finalTotal.toLocaleString('vi-VN') }} ₫</strong
                    >
                  </div>
                  <p class="qr-safe-note">
                    <i class="bi bi-shield-check text-success"></i> Hệ thống tự
                    động xác nhận sau 5 - 10 giây khi chuyển khoản thành công.
                  </p>
                </div>
              </div>
            </label>

            <!-- 2. Ví ZonePay -->
            <label
              class="pay-method-radio-box"
              :class="{ selected: selectedPaymentMethod === 'ZONEPAY_WALLET' }"
            >
              <div class="radio-label-top">
                <div class="radio-left">
                  <input
                    type="radio"
                    name="payMethod"
                    value="ZONEPAY_WALLET"
                    v-model="selectedPaymentMethod"
                  />
                  <div class="pay-info">
                    <span class="pay-name">
                      <i class="bi bi-wallet-fill text-warning"></i>
                      <strong>Ví Điện Tử ZonePay</strong>
                    </span>
                    <span class="pay-subtext">
                      Số dư ví hiện tại:
                      <strong
                        :class="
                          isWalletSufficient ? 'text-success' : 'text-danger'
                        "
                        >{{ walletBalance.toLocaleString('vi-VN') }} ₫</strong
                      >
                    </span>
                  </div>
                </div>
                <span
                  class="wallet-status-tag"
                  :class="isWalletSufficient ? 'sufficient' : 'insufficient'"
                >
                  {{ isWalletSufficient ? 'Đủ số dư' : 'Chưa đủ số dư' }}
                </span>
              </div>
              <p
                v-if="
                  selectedPaymentMethod === 'ZONEPAY_WALLET' &&
                  !isWalletSufficient
                "
                class="wallet-warn-msg"
              >
                ⚠️ Số dư Ví ZonePay của bạn hiện còn thiếu
                {{ (finalTotal - walletBalance).toLocaleString('vi-VN') }} ₫ để
                chốt đơn này. Vui lòng chọn VietQR hoặc COD!
              </p>
            </label>

            <!-- 3. Tiền mặt khi nhận hàng (COD) -->
            <label
              class="pay-method-radio-box"
              :class="{ selected: selectedPaymentMethod === 'COD' }"
            >
              <div class="radio-label-top">
                <div class="radio-left">
                  <input
                    type="radio"
                    name="payMethod"
                    value="COD"
                    v-model="selectedPaymentMethod"
                  />
                  <div class="pay-info">
                    <span class="pay-name">
                      <i class="bi bi-cash-stack text-success"></i>
                      <strong>Tiền Mặt Khi Nhận Hàng (COD)</strong>
                    </span>
                    <span class="pay-subtext"
                      >Thanh toán tiền mặt trực tiếp cho Shipper khi hàng được
                      giao tới tận tay</span
                    >
                  </div>
                </div>
              </div>
            </label>

            <!-- 4. Thẻ ATM / Visa / Master -->
            <label
              class="pay-method-radio-box"
              :class="{ selected: selectedPaymentMethod === 'CREDIT_CARD' }"
            >
              <div class="radio-label-top">
                <div class="radio-left">
                  <input
                    type="radio"
                    name="payMethod"
                    value="CREDIT_CARD"
                    v-model="selectedPaymentMethod"
                  />
                  <div class="pay-info">
                    <span class="pay-name">
                      <i class="bi bi-credit-card text-info"></i>
                      <strong
                        >Thẻ ATM Nội Địa / Thẻ Quốc Tế (Visa, Master)</strong
                      >
                    </span>
                    <span class="pay-subtext"
                      >Cổng thanh toán bảo mật tiêu chuẩn ngân hàng Việt
                      Nam</span
                    >
                  </div>
                </div>
              </div>
            </label>
          </div>
        </section>
      </div>

      <!-- ================= CỘT PHẢI: TỔNG KẾT & XÁC NHẬN (35%) ================= -->
      <div class="checkout-summary-column">
        <div class="summary-sticky-card">
          <h3 class="summary-header">Tóm Tắt Đơn Hàng</h3>

          <!-- Mã giảm giá Voucher -->
          <div class="checkout-voucher-section">
            <span class="section-label"
              ><i class="bi bi-ticket-perforated-fill text-terracotta"></i> Mã
              ưu đãi ZoneMart</span
            >
            <div class="voucher-input-bar">
              <input
                type="text"
                v-model="voucherInput"
                placeholder="Nhập mã (FREESHIP10K...)"
                class="voucher-box-input"
              />
              <button
                type="button"
                class="btn-apply-voucher"
                @click="applyVoucherCode()"
              >
                Áp Dụng
              </button>
            </div>

            <div
              v-if="voucherAlert"
              class="voucher-msg-pill"
              :class="voucherAlert.type"
            >
              {{ voucherAlert.text }}
            </div>

            <!-- Gợi ý voucher -->
            <div class="voucher-suggestions-row">
              <button
                v-for="v in cart.AVAILABLE_VOUCHERS"
                :key="v.code"
                type="button"
                class="v-quick-chip"
                :class="{ active: cart.appliedVoucherCode.value === v.code }"
                @click="applyVoucherCode(v.code)"
              >
                {{ v.code }}
              </button>
            </div>
          </div>

          <!-- Bảng chi tiết tính tiền -->
          <div class="summary-breakdown-list">
            <div class="breakdown-row">
              <span class="label"
                >Tiền hàng ({{ cart.selectedItemsCount.value }} sản phẩm):</span
              >
              <strong class="value"
                >{{ cart.subTotal.value.toLocaleString('vi-VN') }} ₫</strong
              >
            </div>

            <div class="breakdown-row">
              <span class="label"
                >Phí giao hàng ({{ cart.activeStoresCount.value }} quán):</span
              >
              <strong class="value"
                >{{ deliveryFeeCalculated.toLocaleString('vi-VN') }} ₫</strong
              >
            </div>

            <div v-if="discountAmount > 0" class="breakdown-row text-success">
              <span class="label"
                ><i class="bi bi-tag-fill"></i> Giảm giá Voucher:</span
              >
              <strong class="value"
                >-{{ discountAmount.toLocaleString('vi-VN') }} ₫</strong
              >
            </div>

            <div class="breakdown-divider"></div>

            <div class="breakdown-row total-highlight-row">
              <span class="total-label">Tổng thanh toán:</span>
              <div class="total-price-col">
                <strong class="total-number"
                  >{{ finalTotal.toLocaleString('vi-VN') }} ₫</strong
                >
                <span class="vat-tag">(Đã bao gồm VAT & phụ phí)</span>
              </div>
            </div>
          </div>

          <!-- Nút Xác nhận đặt hàng -->
          <button
            type="button"
            class="btn-submit-order"
            :disabled="
              isSubmitting ||
              (selectedPaymentMethod === 'ZONEPAY_WALLET' &&
                !isWalletSufficient)
            "
            @click="handlePlaceOrder"
          >
            <span v-if="!isSubmitting">
              <i class="bi bi-bag-check-fill"></i>
              XÁC NHẬN ĐẶT HÀNG ({{ finalTotal.toLocaleString('vi-VN') }} ₫)
            </span>
            <span v-else class="submitting-spinner">
              <span class="spinner-dot"></span> Đang tạo đơn vào Database...
            </span>
          </button>

          <!-- Cam kết sàn ZoneMart -->
          <div class="order-trust-box">
            <div class="trust-item">
              <i class="bi bi-shield-fill-check text-success"></i>
              <span>Lưu đơn bảo mật trên MongoDB Atlas</span>
            </div>
            <div class="trust-item">
              <i class="bi bi-lightning-charge-fill text-terracotta"></i>
              <span>Giao hỏa tốc 15 - 25 phút trong 10km</span>
            </div>
            <div class="trust-item">
              <i class="bi bi-arrow-repeat text-primary"></i>
              <span>Hoàn tiền 100% nếu sản phẩm lỗi, dập nát</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 3. TRƯỜNG HỢP GIỎ HÀNG KHÔNG CÓ MÓN NÀO ĐƯỢC CHỌN -->
    <div v-else class="empty-checkout-wrap">
      <div class="empty-checkout-box">
        <div class="empty-icon-circle">
          <i class="bi bi-cart-x"></i>
        </div>
        <h2>Chưa Có Sản Phẩm Nào Được Chọn!</h2>
        <p>
          Vui lòng quay lại Giỏ hàng và tick chọn các món bạn muốn thanh toán
          trước khi tiếp tục.
        </p>
        <router-link to="/cart" class="btn-return-cart">
          <i class="bi bi-arrow-left"></i>
          <span>Quay Lại Giỏ Hàng</span>
        </router-link>
      </div>
    </div>

    <!-- MODAL CHỈNH SỬA / THÊM ĐỊA CHỈ NHẬN HÀNG -->
    <div
      v-if="isAddressModalOpen"
      class="modal-backdrop"
      @click.self="isAddressModalOpen = false"
    >
      <div class="address-modal-card">
        <div class="modal-head">
          <h3>Thay Đổi Địa Chỉ Nhận Hàng</h3>
          <button
            type="button"
            class="btn-close-modal"
            @click="isAddressModalOpen = false"
          >
            <i class="bi bi-x-lg"></i>
          </button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Phân loại địa chỉ</label>
            <div class="tag-buttons">
              <button
                type="button"
                class="tag-btn"
                :class="{ active: editingAddress.tag === 'Nhà riêng' }"
                @click="editingAddress.tag = 'Nhà riêng'"
              >
                Nhà riêng
              </button>
              <button
                type="button"
                class="tag-btn"
                :class="{ active: editingAddress.tag === 'Văn phòng' }"
                @click="editingAddress.tag = 'Văn phòng'"
              >
                Văn phòng
              </button>
            </div>
          </div>
          <div class="form-group">
            <label>Họ và tên người nhận</label>
            <input
              type="text"
              v-model="editingAddress.recipientName"
              placeholder="Ví dụ: Nguyễn Văn An"
            />
          </div>
          <div class="form-group">
            <label>Số điện thoại</label>
            <input
              type="tel"
              v-model="editingAddress.phone"
              placeholder="Ví dụ: 0912 345 678"
            />
          </div>
          <div class="form-group">
            <label>Địa chỉ chi tiết (Số nhà, ngõ, đường, phường, quận)</label>
            <textarea
              v-model="editingAddress.detail"
              rows="3"
              placeholder="Số 18, Ngõ 245 Cầu Giấy, Hà Nội..."
            ></textarea>
          </div>
        </div>
        <div class="modal-foot">
          <button
            type="button"
            class="btn-cancel"
            @click="isAddressModalOpen = false"
          >
            Hủy
          </button>
          <button type="button" class="btn-save" @click="saveCustomAddress">
            Lưu Địa Chỉ Này
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL CHÚC MỪNG ĐẶT HÀNG THÀNH CÔNG -->
    <div v-if="showSuccessModal" class="modal-backdrop success-backdrop">
      <div class="success-modal-card">
        <div class="success-check-icon">
          <i class="bi bi-check2-circle"></i>
        </div>
        <h2>ĐẶT HÀNG THÀNH CÔNG!</h2>
        <p class="success-subtext">
          Đơn hàng của bạn đã được ghi nhận vào Cơ sở dữ liệu ZoneMart và đang
          được gửi tới các cửa hàng để chuẩn bị.
        </p>

        <div class="success-order-summary-box">
          <div class="sum-row">
            <span>Mã đơn hàng:</span>
            <strong class="highlight-code">#{{ createdOrderId }}</strong>
          </div>
          <div class="sum-row">
            <span>Tổng thanh toán:</span>
            <strong class="text-terracotta"
              >{{ finalTotal.toLocaleString('vi-VN') }} ₫</strong
            >
          </div>
          <div class="sum-row">
            <span>Hình thức giao:</span>
            <span>{{
              selectedDeliveryType === 'express'
                ? 'Hỏa Tốc Siêu Tốc (15 - 25 phút)'
                : 'Giao Tiêu Chuẩn 10km'
            }}</span>
          </div>
          <div class="sum-row">
            <span>Thanh toán qua:</span>
            <span>{{
              selectedPaymentMethod === 'ONLINE_QR'
                ? 'VietQR MB Bank'
                : selectedPaymentMethod === 'ZONEPAY_WALLET'
                  ? 'Ví ZonePay'
                  : 'Tiền mặt COD'
            }}</span>
          </div>
        </div>

        <button
          type="button"
          class="btn-view-order-tracking"
          @click="goToOrdersTracking"
        >
          <i class="bi bi-bicycle"></i>
          <span>THEO DÕI ĐƠN HÀNG TRỰC TIẾP</span>
        </button>
      </div>
    </div>

    <!-- TOAST THÔNG BÁO -->
    <transition name="toast-fade">
      <div v-if="showToast" class="global-toast-bar">
        <i class="bi bi-info-circle-fill"></i>
        <span>{{ toastMsg }}</span>
      </div>
    </transition>
  </div>
</template>

<style scoped>
/* ============================================================================
   WARM HUMANIST & TERRACOTTA DESIGN STYLES FOR CHECKOUT VIEW
   Bảng màu: #FAF7F2 (Kem), #D85A2A (Đất nung), #2B1B14 (Nâu trầm), #EBDCD3
   ============================================================================ */
.checkout-page-wrapper {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px 16px 80px 16px;
  color: #2b1b14;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', sans-serif;
}

/* 1. STEPPER TRACK */
.checkout-stepper-container {
  margin-bottom: 28px;
}
.stepper-track {
  display: flex;
  align-items: center;
  justify-content: center;
  max-width: 680px;
  margin: 0 auto;
}
.step-node {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  text-decoration: none;
  color: #8c7a72;
  transition: all 0.2s ease;
}
.step-node.active {
  color: #d85a2a;
  font-weight: 700;
}
.step-node.completed {
  color: #16a34a;
  font-weight: 600;
}
.node-circle {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #f1e7e0;
  color: #8c7a72;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  transition: all 0.25s ease;
}
.step-node.active .node-circle {
  background: #d85a2a;
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(216, 90, 42, 0.35);
}
.step-node.completed .node-circle {
  background: #dcfce7;
  color: #16a34a;
}
.node-label {
  font-size: 13px;
  white-space: nowrap;
}
.step-connector {
  flex: 1;
  height: 2px;
  background: #ebdcd3;
  margin: 0 12px 22px 12px;
}
.step-connector.active {
  background: #d85a2a;
}

/* 2. GRID BỐ CỤC CHÍNH */
.checkout-main-grid {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 24px;
  align-items: flex-start;
}
@media (max-width: 960px) {
  .checkout-main-grid {
    grid-template-columns: 1fr;
  }
}

/* CỘT TRÁI - CHI TIẾT ĐẶT HÀNG */
.checkout-details-column {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.checkout-card {
  background: #ffffff;
  border-radius: 18px;
  border: 1.5px solid #ebdcd3;
  padding: 22px;
  box-shadow: 0 4px 16px rgba(43, 27, 20, 0.04);
}

.card-title-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 18px;
  border-bottom: 1px dashed #f0e4dc;
  padding-bottom: 12px;
}

.title-with-icon {
  display: flex;
  align-items: center;
  gap: 12px;
}

.icon-circle {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
}
.icon-terracotta {
  background: #fdf0e8;
  color: #d85a2a;
}
.icon-green {
  background: #f0fdf4;
  color: #16a34a;
}
.icon-orange {
  background: #fff7ed;
  color: #ea580c;
}
.icon-blue {
  background: #eff6ff;
  color: #2563eb;
}

.card-main-title {
  margin: 0;
  font-size: 16px;
  font-weight: 800;
  color: #2b1b14;
}
.card-sub-title {
  margin: 2px 0 0 0;
  font-size: 12.5px;
  color: #78655d;
}

.btn-text-action {
  background: transparent;
  border: none;
  color: #d85a2a;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  text-decoration: none;
  padding: 4px 8px;
  border-radius: 6px;
  transition: all 0.2s ease;
}
.btn-text-action:hover {
  background: #fdf0e8;
}

/* ĐỊA CHỈ NHẬN HÀNG */
.saved-address-chips {
  display: flex;
  gap: 10px;
  margin-bottom: 14px;
}
.address-chip {
  background: #fcf9f6;
  border: 1.5px solid #ebdcd3;
  padding: 6px 14px;
  border-radius: 50px;
  font-size: 13px;
  font-weight: 600;
  color: #55443d;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}
.address-chip.selected {
  background: #fdf0e8;
  border-color: #d85a2a;
  color: #d85a2a;
}
.default-badge {
  font-size: 10.5px;
  background: #d85a2a;
  color: #fff;
  padding: 1px 6px;
  border-radius: 10px;
}

.active-address-box {
  background: #fcfaf8;
  border-radius: 12px;
  border: 1px solid #f1e5dc;
  padding: 14px 16px;
}
.recipient-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 6px;
  flex-wrap: wrap;
}
.recipient-name {
  font-weight: 800;
  font-size: 15px;
  color: #2b1b14;
}
.recipient-phone {
  font-weight: 700;
  color: #55443d;
  font-size: 14px;
}
.hub-radius-badge {
  margin-left: auto;
  font-size: 12px;
  font-weight: 700;
  background: #ecfdf5;
  color: #059669;
  padding: 3px 10px;
  border-radius: 20px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.recipient-address-text {
  margin: 0;
  font-size: 13.5px;
  color: #55443d;
  line-height: 1.5;
}

/* DANH SÁCH KIỆN HÀNG CỬA HÀNG */
.stores-group-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.store-package-block {
  background: #fcfaf8;
  border-radius: 14px;
  border: 1px solid #ebdcd3;
  overflow: hidden;
}
.store-package-head {
  background: #f7ede6;
  padding: 10px 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #ebdcd3;
}
.store-brand {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 800;
  font-size: 14px;
  color: #2b1b14;
}
.store-delivery-tag {
  font-size: 12px;
  color: #8c7a72;
  display: flex;
  align-items: center;
  gap: 6px;
}
.distance-text {
  font-weight: 700;
  color: #d85a2a;
}

.package-items-table {
  padding: 10px 16px;
}
.package-item-row {
  display: grid;
  grid-template-columns: 44px 1fr auto auto;
  gap: 14px;
  align-items: center;
  padding: 8px 0;
  border-bottom: 1px solid #f3ece6;
}
.package-item-row:last-child {
  border-bottom: none;
}
.item-thumb {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid #ebdcd3;
}
.item-name {
  margin: 0;
  font-size: 13.5px;
  font-weight: 700;
  color: #2b1b14;
}
.item-unit {
  font-size: 12px;
  color: #8c7a72;
}
.item-qty-col {
  font-size: 13px;
  color: #78655d;
  font-weight: 600;
}
.item-price-col {
  font-size: 14px;
  font-weight: 800;
  color: #2b1b14;
}

.store-note-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px;
  background: #ffffff;
  border-top: 1px solid #f1e5dc;
  color: #8c7a72;
}
.store-note-input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 12.5px;
  color: #2b1b14;
}
.store-note-input::placeholder {
  color: #a89a93;
}

/* PHƯƠNG THỨC VẬN CHUYỂN */
.shipping-option-grid {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.shipping-radio-card {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 16px;
  border-radius: 12px;
  border: 1.5px solid #ebdcd3;
  background: #fcfaf8;
  cursor: pointer;
  transition: all 0.2s ease;
}
.shipping-radio-card input {
  margin-top: 3px;
  accent-color: #d85a2a;
}
.shipping-radio-card.selected {
  border-color: #d85a2a;
  background: #fdf2eb;
}
.shipping-card-body {
  flex: 1;
}
.shipping-card-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}
.shipping-title-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}
.badge-express-fast {
  font-size: 11px;
  font-weight: 800;
  background: #fee2e2;
  color: #dc2626;
  padding: 2px 8px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 3px;
}
.badge-standard {
  font-size: 11px;
  font-weight: 800;
  background: #dcfce7;
  color: #16a34a;
  padding: 2px 8px;
  border-radius: 6px;
}
.badge-scheduled {
  font-size: 11px;
  font-weight: 800;
  background: #e0e7ff;
  color: #4338ca;
  padding: 2px 8px;
  border-radius: 6px;
}
.shipping-fee-val {
  font-size: 14px;
  font-weight: 800;
  color: #d85a2a;
}
.shipping-desc {
  margin: 0;
  font-size: 12.5px;
  color: #78655d;
}
.scheduled-selector-wrap {
  margin-top: 10px;
}
.timeslot-select {
  padding: 6px 12px;
  border-radius: 8px;
  border: 1px solid #ebdcd3;
  background: #fff;
  font-size: 13px;
  font-weight: 600;
  color: #2b1b14;
}

/* PHƯƠNG THỨC THANH TOÁN */
.payment-method-options {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.pay-method-radio-box {
  display: flex;
  flex-direction: column;
  padding: 14px 16px;
  border-radius: 14px;
  border: 1.5px solid #ebdcd3;
  background: #fcfaf8;
  cursor: pointer;
  transition: all 0.2s ease;
}
.pay-method-radio-box.selected {
  border-color: #d85a2a;
  background: #ffffff;
  box-shadow: 0 4px 16px rgba(216, 90, 42, 0.08);
}
.radio-label-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.radio-left {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}
.radio-left input {
  margin-top: 3px;
  accent-color: #d85a2a;
}
.pay-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.pay-name {
  font-size: 14px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: #2b1b14;
}
.pay-subtext {
  font-size: 12px;
  color: #78655d;
}
.recommend-badge {
  font-size: 11px;
  font-weight: 800;
  background: #fdf0e8;
  color: #d85a2a;
  padding: 3px 8px;
  border-radius: 6px;
  border: 1px solid #ebdcd3;
}
.wallet-status-tag {
  font-size: 11.5px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 6px;
}
.wallet-status-tag.sufficient {
  background: #dcfce7;
  color: #16a34a;
}
.wallet-status-tag.insufficient {
  background: #fee2e2;
  color: #dc2626;
}
.wallet-warn-msg {
  margin: 8px 0 0 28px;
  font-size: 12px;
  color: #b91c1c;
  font-weight: 600;
}

/* VIETQR LIVE PREVIEW */
.vietqr-live-preview {
  margin-top: 14px;
  padding-top: 14px;
  border-top: 1px dashed #ebdcd3;
  display: grid;
  grid-template-columns: 160px 1fr;
  gap: 18px;
  align-items: center;
  background: #fcf9f6;
  border-radius: 12px;
  padding: 14px;
}
@media (max-width: 600px) {
  .vietqr-live-preview {
    grid-template-columns: 1fr;
    text-align: center;
  }
}
.qr-code-visual {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
}
.qr-img {
  width: 140px;
  height: 140px;
  border-radius: 10px;
  border: 1px solid #ebdcd3;
  background: #fff;
  padding: 4px;
}
.qr-scan-hint {
  font-size: 11.5px;
  font-weight: 700;
  color: #d85a2a;
}
.bank-details-side {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.bank-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
}
.bank-row .lbl {
  color: #78655d;
  width: 110px;
}
.bank-row .val {
  color: #2b1b14;
}
.copy-acc-wrap {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
.highlight {
  color: #d85a2a;
  letter-spacing: 0.5px;
}
.btn-copy-sm {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  padding: 2px 8px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
  color: #55443d;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  transition: all 0.2s;
}
.btn-copy-sm:hover {
  background: #fdf0e8;
  color: #d85a2a;
}
.qr-safe-note {
  margin: 6px 0 0 0;
  font-size: 11.5px;
  color: #059669;
  font-weight: 600;
}

/* ================= CỘT PHẢI: STICKY ORDER SUMMARY ================= */
.summary-sticky-card {
  position: sticky;
  top: 90px;
  background: #ffffff;
  border-radius: 18px;
  border: 1.5px solid #ebdcd3;
  padding: 22px;
  box-shadow: 0 8px 24px rgba(43, 27, 20, 0.06);
}

.summary-header {
  margin: 0 0 16px 0;
  font-size: 18px;
  font-weight: 800;
  color: #2b1b14;
  border-bottom: 1px solid #f0e4dc;
  padding-bottom: 10px;
}

/* VOUCHER SECTION TRONG SUMMARY */
.checkout-voucher-section {
  margin-bottom: 18px;
}
.section-label {
  font-size: 13px;
  font-weight: 700;
  color: #2b1b14;
  margin-bottom: 8px;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.voucher-input-bar {
  display: flex;
  gap: 8px;
}
.voucher-box-input {
  flex: 1;
  border: 1.5px solid #ebdcd3;
  border-radius: 10px;
  padding: 8px 12px;
  font-size: 13px;
  outline: none;
  text-transform: uppercase;
}
.voucher-box-input:focus {
  border-color: #d85a2a;
}
.btn-apply-voucher {
  background: #d85a2a;
  color: #ffffff;
  border: none;
  padding: 8px 14px;
  border-radius: 10px;
  font-weight: 700;
  font-size: 12.5px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-apply-voucher:hover {
  background: #bf4a1f;
}
.voucher-msg-pill {
  margin-top: 6px;
  font-size: 11.5px;
  padding: 4px 8px;
  border-radius: 6px;
}
.voucher-msg-pill.success {
  background: #dcfce7;
  color: #16a34a;
}
.voucher-msg-pill.error {
  background: #fee2e2;
  color: #dc2626;
}
.voucher-suggestions-row {
  display: flex;
  gap: 6px;
  margin-top: 8px;
  flex-wrap: wrap;
}
.v-quick-chip {
  background: #fcf9f6;
  border: 1px dashed #ebdcd3;
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
  color: #78655d;
  cursor: pointer;
  transition: all 0.2s;
}
.v-quick-chip:hover,
.v-quick-chip.active {
  background: #fdf0e8;
  border-color: #d85a2a;
  color: #d85a2a;
}

/* BẢNG TÍNH TIỀN */
.summary-breakdown-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 20px;
}
.breakdown-row {
  display: flex;
  justify-content: space-between;
  font-size: 14px;
  color: #55443d;
}
.breakdown-row .value {
  color: #2b1b14;
}
.breakdown-divider {
  height: 1px;
  background: #ebdcd3;
  margin: 6px 0;
}
.total-highlight-row {
  align-items: baseline;
}
.total-label {
  font-size: 16px;
  font-weight: 800;
  color: #2b1b14;
}
.total-price-col {
  text-align: right;
}
.total-number {
  font-size: 24px;
  font-weight: 900;
  color: #d85a2a;
  display: block;
}
.vat-tag {
  font-size: 11px;
  color: #8c7a72;
}

/* NÚT CHỐT ĐƠN HÀNG */
.btn-submit-order {
  width: 100%;
  background: linear-gradient(135deg, #d85a2a 0%, #be4617 100%);
  color: #ffffff;
  border: none;
  padding: 16px;
  border-radius: 14px;
  font-weight: 800;
  font-size: 15px;
  letter-spacing: 0.3px;
  cursor: pointer;
  box-shadow: 0 8px 20px rgba(216, 90, 42, 0.3);
  transition: all 0.25s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}
.btn-submit-order:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 10px 24px rgba(216, 90, 42, 0.4);
}
.btn-submit-order:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  background: #cbd5e1;
  box-shadow: none;
}
.submitting-spinner {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
.spinner-dot {
  width: 14px;
  height: 14px;
  border: 2px solid #fff;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.order-trust-box {
  margin-top: 18px;
  padding-top: 14px;
  border-top: 1px dashed #f0e4dc;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.trust-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #55443d;
}

/* 3. GIAO DIỆN TRỐNG NẾU KHÔNG CÓ MÓN */
.empty-checkout-wrap {
  min-height: 450px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px 0;
}
.empty-checkout-box {
  background: #ffffff;
  border: 1.5px solid #ebdcd3;
  border-radius: 20px;
  padding: 48px 32px;
  text-align: center;
  max-width: 480px;
}
.empty-icon-circle {
  width: 64px;
  height: 64px;
  background: #fdf0e8;
  color: #d85a2a;
  border-radius: 50%;
  font-size: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 16px auto;
}
.empty-checkout-box h2 {
  margin: 0 0 8px 0;
  font-size: 20px;
  font-weight: 800;
}
.empty-checkout-box p {
  margin: 0 0 24px 0;
  font-size: 14px;
  color: #78655d;
}
.btn-return-cart {
  background: #d85a2a;
  color: #fff;
  text-decoration: none;
  padding: 12px 24px;
  border-radius: 12px;
  font-weight: 700;
  font-size: 14px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s;
}
.btn-return-cart:hover {
  background: #bf4a1f;
}

/* ================= MODAL CHỈNH SỬA ĐỊA CHỈ ================= */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.55);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 16px;
}
.address-modal-card {
  background: #ffffff;
  border-radius: 20px;
  width: 100%;
  max-width: 480px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
  overflow: hidden;
}
.modal-head {
  padding: 16px 20px;
  border-bottom: 1px solid #ebdcd3;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.modal-head h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 800;
  color: #2b1b14;
}
.btn-close-modal {
  background: transparent;
  border: none;
  font-size: 18px;
  color: #8c7a72;
  cursor: pointer;
}
.modal-body {
  padding: 18px 20px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.form-group label {
  font-size: 13px;
  font-weight: 700;
  color: #2b1b14;
}
.form-group input,
.form-group textarea {
  border: 1.5px solid #ebdcd3;
  border-radius: 10px;
  padding: 10px 12px;
  font-size: 13.5px;
  outline: none;
  font-family: inherit;
}
.form-group input:focus,
.form-group textarea:focus {
  border-color: #d85a2a;
}
.tag-buttons {
  display: flex;
  gap: 10px;
}
.tag-btn {
  background: #fcf9f6;
  border: 1.5px solid #ebdcd3;
  padding: 6px 16px;
  border-radius: 30px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}
.tag-btn.active {
  background: #fdf0e8;
  border-color: #d85a2a;
  color: #d85a2a;
  font-weight: 700;
}
.modal-foot {
  padding: 14px 20px;
  border-top: 1px solid #ebdcd3;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.btn-cancel {
  background: #f1f5f9;
  border: none;
  padding: 8px 16px;
  border-radius: 10px;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
}
.btn-save {
  background: #d85a2a;
  border: none;
  padding: 8px 20px;
  border-radius: 10px;
  font-weight: 700;
  color: #ffffff;
  cursor: pointer;
}

/* ================= MODAL ĐẶT HÀNG THÀNH CÔNG ================= */
.success-backdrop {
  background: rgba(15, 23, 42, 0.7);
}
.success-modal-card {
  background: #ffffff;
  border-radius: 24px;
  padding: 36px 28px;
  text-align: center;
  width: 100%;
  max-width: 480px;
  box-shadow: 0 25px 50px rgba(0, 0, 0, 0.25);
  animation: modalScale 0.3s ease-out;
}
@keyframes modalScale {
  from {
    transform: scale(0.9);
    opacity: 0;
  }
  to {
    transform: scale(1);
    opacity: 1;
  }
}
.success-check-icon {
  font-size: 64px;
  color: #16a34a;
  margin-bottom: 12px;
}
.success-modal-card h2 {
  margin: 0 0 8px 0;
  font-size: 22px;
  font-weight: 900;
  color: #16a34a;
}
.success-subtext {
  margin: 0 0 20px 0;
  font-size: 13.5px;
  color: #55443d;
  line-height: 1.5;
}
.success-order-summary-box {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 14px 18px;
  margin-bottom: 24px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  text-align: left;
}
.sum-row {
  display: flex;
  justify-content: space-between;
  font-size: 13.5px;
}
.highlight-code {
  color: #d85a2a;
}
.btn-view-order-tracking {
  width: 100%;
  background: #16a34a;
  color: #ffffff;
  border: none;
  padding: 15px;
  border-radius: 14px;
  font-size: 15px;
  font-weight: 800;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  box-shadow: 0 8px 20px rgba(22, 163, 74, 0.25);
  transition: all 0.2s;
}
.btn-view-order-tracking:hover {
  background: #15803d;
  transform: translateY(-2px);
}

/* ================= TOAST ================= */
.global-toast-bar {
  position: fixed;
  bottom: 28px;
  left: 50%;
  transform: translateX(-50%);
  background: #2b1b14;
  color: #ffffff;
  padding: 10px 20px;
  border-radius: 50px;
  font-size: 13.5px;
  font-weight: 700;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.35);
  display: inline-flex;
  align-items: center;
  gap: 8px;
  z-index: 10000;
}
.toast-fade-enter-active,
.toast-fade-leave-active {
  transition: all 0.25s ease;
}
.toast-fade-enter-from,
.toast-fade-leave-to {
  opacity: 0;
  transform: translate(-50%, 15px);
}
</style>
