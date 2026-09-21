<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG NHẬP (LOGIN VIEW) - Phụ trách: Bình
 * Giao diện Bảng Đăng Nhập Đơn Giản & Tự Động Nhận Diện Vai Trò
 * Tích hợp Bảng Quên Mật Khẩu với Gmail OTP 120s (dobinh225599@gmail.com)
 * ================================================================
 */
import { ref, reactive } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import ForgotPasswordForm from './ForgotPasswordForm.vue';
import { useAuth, type UserRole } from '../../composables/useAuth';
import { apiFetch } from '../../utils/apiConfig';
import AuthCelebrationModal from '../../components/common/AuthCelebrationModal.vue';
import FaceScanModal from '../../components/auth/FaceScanModal.vue';
import { useToast } from '../../composables/useToast';

const router = useRouter();
const route = useRoute();
const auth = useAuth();

// Hàm điều hướng sau khi đăng nhập thành công
const navigateAfterLogin = (role: string) => {
  const redirectTarget = route.query.redirect as string;
  if (redirectTarget && redirectTarget !== '/login') {
    if (redirectTarget.startsWith('/admin') && role !== 'admin') {
      router.push('/403');
      return;
    }
    router.push(redirectTarget);
    return;
  }
  if (role === 'admin') {
    router.push('/admin');
  } else if (role === 'seller') {
    router.push('/seller');
  } else if (role === 'shipper') {
    router.push('/shipper');
  } else {
    router.push('/');
  }
};

// Chế độ xem: false = Đăng nhập, true = Quên mật khẩu
const isForgotPasswordMode = ref(false);

// Form State
const form = reactive({
  account: '',
  password: '',
  rememberMe: true,
});

const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

const showNotFoundModal = ref(false);
const modalErrorMessage = ref('');
const modalErrorType = ref<
  'ACCOUNT_NOT_FOUND' | 'INCORRECT_PASSWORD' | 'ACCOUNT_PENDING_APPROVAL'
>('ACCOUNT_NOT_FOUND');

// Trạng thái cho Modal Chúc mừng Hoạt Họa siêu xịn
const toast = useToast();
const showCelebrationModal = ref(false);
const celebrationUserName = ref('');
const celebrationRoleName = ref('');
const celebrationMessage = ref('');
let pendingRole = 'buyer';

const onCelebrationComplete = () => {
  showCelebrationModal.value = false;
  navigateAfterLogin(pendingRole);
};

// Trạng thái & Handler cho Đăng nhập Face ID AI UniFace
const showFaceLoginModal = ref(false);

const openFaceIdLoginModal = () => {
  showFaceLoginModal.value = true;
};

const handleFaceLoginSuccess = (data: any) => {
  if (!data || !data.user) return;
  const loggedUser = data.user;
  const rawRole = (loggedUser.role || '').toLowerCase().trim();
  const isAdminUser =
    rawRole === 'admin' ||
    loggedUser.isAdmin === true ||
    (loggedUser.phoneEmail || '').toLowerCase() === 'admin@zonemart.vn';

  const detectedRole: UserRole = isAdminUser ? 'admin' : (rawRole as UserRole) || 'buyer';

  // Cập nhật useAuth Singleton toàn sàn
  auth.login({
    id: loggedUser.id || `usr_${Date.now()}`,
    fullName: loggedUser.fullName || 'Người Dùng ZoneMart',
    phoneEmail: loggedUser.phoneEmail || '',
    phone: loggedUser.phone || (!loggedUser.phoneEmail?.includes('@') ? loggedUser.phoneEmail : undefined),
    role: detectedRole,
    avatarUrl: loggedUser.avatarUrl,
    walletBalance: loggedUser.walletBalance,
    storeName: loggedUser.storeName,
  });

  const userWithStatus: any = {
    id: loggedUser.id || `usr_${Date.now()}`,
    fullName: loggedUser.fullName || '',
    phoneEmail: loggedUser.phoneEmail || '',
    phone: loggedUser.phone || undefined,
    role: detectedRole,
    isAdmin: isAdminUser,
    avatarUrl: loggedUser.avatarUrl,
    walletBalance: loggedUser.walletBalance,
    storeName: loggedUser.storeName,
    sellerStatus: loggedUser.sellerStatus,
    rejectReason: loggedUser.rejectReason,
  };
  localStorage.setItem('zonemart_user', JSON.stringify(userWithStatus));
  localStorage.setItem('isLoggedIn', 'true');
  localStorage.setItem('userRole', detectedRole);
  if (loggedUser) {
    loggedUser.role = detectedRole;
    loggedUser.isAdmin = isAdminUser;
    localStorage.setItem('currentUser', JSON.stringify(loggedUser));
  }
  localStorage.removeItem('sellerRegisteredEmail');
  localStorage.removeItem('sellerRegisteredPassword');

  pendingRole = detectedRole;
  celebrationUserName.value = loggedUser.fullName || 'Người Dùng';
  const roleMap: Record<string, string> = {
    buyer: 'Khách Hàng',
    seller: 'Chủ Gian Hàng',
    shipper: 'Tài Xế Giao Hàng',
    admin: 'Quản Trị Viên',
  };
  celebrationRoleName.value = roleMap[detectedRole] || 'Thành Viên';
  celebrationMessage.value = `Nhận diện sinh trắc học Face ID thành công (${data.similarity ? Math.round(data.similarity * 100) : 98}% khớp)! Đang chuyển hướng...`;

  toast.success(`Nhận diện thành công: ${celebrationUserName.value}!`, 'Đăng Nhập Face ID');
  showCelebrationModal.value = true;
};

const togglePassword = () => {
  showPassword.value = !showPassword.value;
};

// Xử lý Đăng Nhập
const handleLogin = async () => {
  errorMessage.value = '';
  successMessage.value = '';

  if (!form.account.trim() || !form.password.trim()) {
    modalErrorMessage.value =
      'Vui lòng nhập đầy đủ số điện thoại / email và mật khẩu!';
    modalErrorType.value = 'ACCOUNT_NOT_FOUND';
    showNotFoundModal.value = true;
    return;
  }

  isLoading.value = true;

  try {
    // 1. Kết nối API Backend (hỗ trợ cả localhost và link port)
    const res = await apiFetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        account: form.account.trim(),
        password: form.password,
      }),
    }).catch(() => null);

    if (res) {
      const data = await res.json();
      if (res.ok && data.success) {
        if (data.user?.sellerStatus === 'Pending') {
          successMessage.value = data.message || `Hồ sơ mở gian hàng '${data.user.storeName || ''}' đang chờ Ban Quản Lý phê duyệt! Bạn có thể tiếp tục mua sắm...`;
        } else if (data.user?.sellerStatus === 'Rejected') {
          successMessage.value = `Hồ sơ mở gian hàng của bạn đã bị từ chối: ${data.user.rejectReason || 'Không đạt yêu cầu'}. Bạn có thể tiếp tục mua sắm...`;
        } else {
          successMessage.value =
            data.message || 'Đăng nhập thành công! Đang chuyển hướng...';
        }
        const rawRole = (data.user?.role || '').toLowerCase().trim();
        const isAdminUser =
          rawRole === 'admin' ||
          data.user?.isAdmin === true ||
          form.account.trim().toLowerCase() === 'admin' ||
          form.account.trim().toLowerCase() === 'admin@zonemart.vn';

        const detectedRole: UserRole = isAdminUser ? 'admin' : (rawRole as UserRole) || 'buyer';

        // Lưu thông tin người dùng vào useAuth state & localStorage
        auth.login({
          id: data.user?.id || `usr_${Date.now()}`,
          fullName: data.user?.fullName || form.account.trim(),
          phoneEmail: data.user?.phoneEmail || form.account.trim(),
          phone: data.user?.phone || (!form.account.includes('@') ? form.account.trim() : undefined),
          role: detectedRole,
          avatarUrl: data.user?.avatarUrl,
          walletBalance: data.user?.walletBalance,
          storeName: data.user?.storeName,
        });

        // Ghi đè vào zonemart_user để lưu cả sellerStatus và rejectReason
        const userWithStatus: any = {
          id: data.user?.id || `usr_${Date.now()}`,
          fullName: data.user?.fullName || form.account.trim(),
          phoneEmail: data.user?.phoneEmail || form.account.trim(),
          phone: data.user?.phone || (!form.account.includes('@') ? form.account.trim() : undefined),
          role: detectedRole,
          isAdmin: isAdminUser,
          avatarUrl: data.user?.avatarUrl,
          walletBalance: data.user?.walletBalance,
          storeName: data.user?.storeName,
          sellerStatus: data.user?.sellerStatus,
          rejectReason: data.user?.rejectReason,
        };
        localStorage.setItem('zonemart_user', JSON.stringify(userWithStatus));

        localStorage.setItem("isLoggedIn", "true");
        localStorage.setItem("userRole", detectedRole);
        if (data.user) {
          data.user.role = detectedRole;
          data.user.isAdmin = isAdminUser;
          localStorage.setItem('currentUser', JSON.stringify(data.user));
        }
        localStorage.removeItem('sellerRegisteredEmail');
        localStorage.removeItem('sellerRegisteredPassword');

        pendingRole = detectedRole;
        celebrationUserName.value = data.user?.fullName || form.account.trim();
        const roleMap: Record<string, string> = {
          buyer: 'Khách Hàng',
          seller: 'Chủ Gian Hàng',
          shipper: 'Tài Xế Giao Hàng',
          admin: 'Quản Trị Viên',
        };
        celebrationRoleName.value = roleMap[detectedRole] || 'Thành Viên';

        if (data.user?.sellerStatus === 'Pending') {
          celebrationMessage.value = `Hồ sơ mở gian hàng '${data.user.storeName || ''}' đang chờ phê duyệt. Bạn có thể tiếp tục mua sắm...`;
        } else {
          celebrationMessage.value = `Đăng nhập thành công! Đang chuyển hướng vào hệ thống ZoneMart...`;
        }

        toast.success(`Chào mừng ${celebrationUserName.value} đã trở lại!`, 'Đăng Nhập Thành Công');
        showCelebrationModal.value = true;
      } else {
        modalErrorMessage.value = data.message || 'Không tìm thấy tài khoản';
        toast.error(modalErrorMessage.value, 'Đăng Nhập Thất Bại');
        if (data.errorType === 'ACCOUNT_PENDING_APPROVAL') {
          modalErrorType.value = 'ACCOUNT_PENDING_APPROVAL';
        } else if (
          data.errorType === 'INCORRECT_PASSWORD' ||
          (data.message && data.message.toLowerCase().includes('mật khẩu'))
        ) {
          modalErrorType.value = 'INCORRECT_PASSWORD';
        } else {
          modalErrorType.value = 'ACCOUNT_NOT_FOUND';
        }
        showNotFoundModal.value = true;
      }
    } else {
      modalErrorMessage.value =
        'Không thể kết nối đến máy chủ Backend (C# / Database). Vui lòng kiểm tra kết nối mạng hoặc thử lại!';
      modalErrorType.value = 'ACCOUNT_NOT_FOUND';
      showNotFoundModal.value = true;
    }
  } catch (error: any) {
    modalErrorMessage.value =
      error.message || 'Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại sau.';
    modalErrorType.value = 'ACCOUNT_NOT_FOUND';
    showNotFoundModal.value = true;
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <div class="login-page-container">
    <!-- KHUNG TỔNG THỂ 2 CỘT: ẢNH BÊN TRÁI + BẢNG ĐĂNG NHẬP BÊN PHẢI -->
    <div class="login-hero-layout">
      <!-- CỘT TRÁI: ẢNH MINH HỌA XE ZONEMART TÁCH SẠCH NỀN -->
      <div class="login-illustration-side">
        <img
          src="/images/anhxoanen_clean.png"
          alt="ZoneMart Giao Hàng Hỏa Tốc"
          class="illustration-img"
        />
      </div>

      <!-- CỘT PHẢI: BẢNG ĐĂNG NHẬP / QUÊN MẬT KHẨU TRÔI NỔI (FLOATING CARD) -->
      <div class="login-card-floating">
        <!-- TRẠNG THÁI 1: FORM ĐĂNG NHẬP -->
        <template v-if="!isForgotPasswordMode">
          <!-- THANH ĐIỀU HƯỚNG QUAY LẠI TRANG CHỦ Ở MÉP TRÁI BẢNG -->
          <div class="card-top-bar">
            <button
              type="button"
              class="back-home-btn"
              @click="router.push('/')"
              title="Về trang chủ"
            >
              ← Quay lại
            </button>
          </div>

          <!-- TIÊU ĐỀ ĐĂNG NHẬP -->
          <div class="card-brand-header">
            <h1 class="form-main-heading">ĐĂNG NHẬP</h1>
            <p class="form-sub-heading">
              Nhập thông tin tài khoản của bạn để truy cập ZoneMart
            </p>
          </div>

          <!-- THÔNG BÁO YÊU CẦU ĐĂNG NHẬP -->
          <div
            v-if="route.query.reason === 'auth_required'"
            class="msg-box warning"
          >
            🔒 Vui lòng đăng nhập tài khoản để tiếp tục truy cập trang này.
          </div>

          <!-- THÔNG BÁO THÀNH CÔNG -->
          <div v-if="successMessage" class="msg-box success">
            ✅ {{ successMessage }}
          </div>

          <!-- FORM NHẬP THÔNG TIN -->
          <form @submit.prevent="handleLogin" class="form-body">
            <!-- Số điện thoại / Email -->
            <div class="field-item">
              <div class="label-row-between">
                <label class="field-label">Tài khoản đăng nhập</label>
                <span class="zalo-hint-tag"><i class="bi bi-shield-check"></i> SĐT Zalo / Email</span>
              </div>
              <input
                v-model="form.account"
                type="text"
                class="field-input"
                placeholder="Nhập SĐT Zalo đã liên kết hoặc Email"
                required
              />
            </div>

            <!-- Mật khẩu -->
            <div class="field-item">
              <div class="label-row-between">
                <label class="field-label">Mật khẩu</label>
                <a
                  href="#"
                  @click.prevent="isForgotPasswordMode = true"
                  class="forgot-pass-link"
                  >Quên mật khẩu?</a
                >
              </div>
              <div class="password-wrapper">
                <input
                  v-model="form.password"
                  :type="showPassword ? 'text' : 'password'"
                  class="field-input"
                  placeholder="Nhập mật khẩu..."
                  required
                />
                <button
                  type="button"
                  class="eye-toggle-btn"
                  @click="togglePassword"
                  title="Ẩn/Hiện mật khẩu"
                >
                  <svg
                    v-if="!showPassword"
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="#9CA3AF"
                    stroke-width="2"
                  >
                    <path
                      d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"
                    ></path>
                    <circle cx="12" cy="12" r="3"></circle>
                  </svg>
                  <svg
                    v-else
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="#9CA3AF"
                    stroke-width="2"
                  >
                    <path
                      d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"
                    ></path>
                    <line x1="1" y1="1" x2="23" y2="23"></line>
                  </svg>
                </button>
              </div>
            </div>

            <!-- Checkbox Ghi nhớ -->
            <div class="form-options-row">
              <label class="custom-checkbox-label">
                <input
                  type="checkbox"
                  v-model="form.rememberMe"
                  class="custom-checkbox"
                />
                <span>Ghi nhớ đăng nhập trên thiết bị này</span>
              </label>
            </div>

            <!-- Hàng Nút Đăng Nhập & Icon Face ID Quét Nhanh -->
            <div class="login-action-row">
              <button
                type="submit"
                class="btn-submit-orange"
                :disabled="isLoading"
              >
                <span v-if="!isLoading">ĐĂNG NHẬP NGAY ➔</span>
                <span v-else>ĐANG XỬ LÝ...</span>
              </button>

              <button
                type="button"
                class="btn-face-quick-icon"
                @click="openFaceIdLoginModal"
                title="Đăng nhập nhanh bằng Face ID (1-Chạm)"
              >
                <!-- Authentic Apple iPhone Face ID Icon -->
                <svg
                  class="apple-faceid-svg"
                  width="25"
                  height="25"
                  viewBox="0 0 80 80"
                  fill="currentColor"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <!-- Top-Left Corner -->
                  <path d="M4.114,21.943 L4.114,13.029 C4.114,7.993 7.993,4.114 13.029,4.114 L21.943,4.114 C23.079,4.114 24.000,3.193 24.000,2.057 C24.000,0.921 23.079,0.000 21.943,0.000 L13.029,0.000 C5.721,0.000 0.000,5.721 0.000,13.029 L0.000,21.943 C0.000,23.079 0.921,24.000 2.057,24.000 C3.193,24.000 4.114,23.079 4.114,21.943 Z" />
                  <!-- Top-Right Corner -->
                  <path d="M75.886,21.943 L75.886,13.029 C75.886,7.993 72.007,4.114 66.971,4.114 L58.057,4.114 C56.921,4.114 56.000,3.193 56.000,2.057 C56.000,0.921 56.921,0.000 58.057,0.000 L66.971,0.000 C74.279,0.000 80.000,5.721 80.000,13.029 L80.000,21.943 C80.000,23.079 79.079,24.000 77.943,24.000 C76.807,24.000 75.886,23.079 75.886,21.943 Z" />
                  <!-- Bottom-Left Corner -->
                  <path d="M4.114,58.057 L4.114,66.971 C4.114,72.007 7.993,75.886 13.029,75.886 L21.943,75.886 C23.079,75.886 24.000,76.807 24.000,77.943 C24.000,79.079 23.079,80.000 21.943,80.000 L13.029,80.000 C5.721,80.000 0.000,74.279 0.000,66.971 L0.000,58.057 C0.000,56.921 0.921,56.000 2.057,56.000 C3.193,56.000 4.114,56.921 4.114,58.057 Z" />
                  <!-- Bottom-Right Corner -->
                  <path d="M75.886,58.057 L75.886,66.971 C75.886,72.007 72.007,75.886 66.971,75.886 L58.057,75.886 C56.921,75.886 56.000,76.807 56.000,77.943 C56.000,79.079 56.921,80.000 58.057,80.000 L66.971,80.000 C74.279,80.000 80.000,74.279 80.000,66.971 L80.000,58.057 C80.000,56.921 79.079,56.000 77.943,56.000 C76.807,56.000 75.886,56.921 75.886,58.057 Z" />
                  <!-- Left Eye -->
                  <path d="M21.754,30.213 L21.754,35.931 C21.754,37.114 22.650,38.073 23.754,38.073 C24.859,38.073 25.754,37.114 25.754,35.931 L25.754,30.213 C25.754,29.030 24.859,28.070 23.754,28.070 C22.650,28.070 21.754,29.030 21.754,30.213 Z" />
                  <!-- Right Eye -->
                  <path d="M54.737,30.213 L54.737,35.931 C54.737,37.114 55.632,38.073 56.737,38.073 C57.841,38.073 58.737,37.114 58.737,35.931 L58.737,30.213 C58.737,29.030 57.841,28.070 56.737,28.070 C55.632,28.070 54.737,29.030 54.737,30.213 Z" />
                  <!-- Nose -->
                  <path d="M40,30.175 L40,44.912 C40,45.855 39.539,46.316 38.591,46.316 L37.193,46.316 C36.030,46.316 35.088,47.258 35.088,48.421 C35.088,49.584 36.030,50.526 37.193,50.526 L38.591,50.526 C41.863,50.526 44.211,48.182 44.211,44.912 L44.211,30.175 C44.211,29.013 43.268,28.070 42.105,28.070 C40.943,28.070 40,29.013 40,30.175 Z" />
                  <!-- Smile Mouth -->
                  <path d="M25.932,59.083 C29.833,62.724 34.558,64.561 40,64.561 C45.442,64.561 50.167,62.724 54.068,59.083 C54.918,58.290 54.964,56.957 54.171,56.107 C53.377,55.257 52.045,55.211 51.195,56.005 C48.079,58.913 44.382,60.351 40,60.351 C35.618,60.351 31.921,58.913 28.805,56.005 C27.955,55.211 26.623,55.257 25.829,56.107 C25.036,56.957 25.082,58.290 25.932,59.083 Z" />
                </svg>
              </button>
            </div>

            <!-- Đường kẻ phân cách OR -->
            <div class="divider-row">
              <span class="line"></span>
              <span class="or-text">HOẶC DÙNG TÀI KHOẢN KHÁC</span>
              <span class="line"></span>
            </div>

            <!-- Các liên kết đăng ký -->
            <div class="register-cta-box">
              <div class="cta-line">
                <span>Bạn chưa có tài khoản? </span>
                <router-link to="/register" class="link-orange-bold"
                  >Tạo tài khoản Khách Hàng</router-link
                >
              </div>
              <div class="cta-sub-line">
                <span>Muốn mở gian hàng? </span>
                <router-link to="/register-seller" target="_blank" class="link-secondary"
                  >Đăng ký bán hàng với ZoneMart</router-link
                >
              </div>
              <div class="cta-sub-line" style="margin-top: 2px">
                <span>Đăng ký làm shipper? </span>
                <router-link to="/register-shipper" target="_blank" class="link-secondary"
                  >Đăng ký đối tác giao hàng</router-link
                >
              </div>
            </div>
          </form>
        </template>

        <!-- TRẠNG THÁI 2: FORM QUÊN MẬT KHẨU WITH GMAIL OTP -->
        <template v-else>
          <ForgotPasswordForm
            :initial-email="form.account"
            @back-to-login="isForgotPasswordMode = false"
            @success="
              isForgotPasswordMode = false;
              successMessage =
                'Đổi mật khẩu thành công! Bạn có thể đăng nhập ngay với mật khẩu mới.';
            "
          />
        </template>
      </div>
    </div>

    <!-- MODAL THÔNG BÁO TÀI KHOẢN / MẬT KHẨU (BÓNG MỜ NỀN ĐÈ GIỮA MÀN HÌNH) -->
    <Transition name="fade-modal">
      <div
        v-if="showNotFoundModal"
        class="modal-backdrop-overlay"
        @click.self="showNotFoundModal = false"
      >
        <div class="modal-pop-card">
          <!-- TH 1: HỒ SƠ ĐANG CHỜ PHÊ DUYỆT (ICON ĐỒNG HỒ CÁT XOAY ĐANG CHẠY) -->
          <div
            v-if="modalErrorType === 'ACCOUNT_PENDING_APPROVAL'"
            class="modal-icon-badge pending-badge"
          >
            <svg
              class="hourglass-animated"
              width="36"
              height="36"
              viewBox="0 0 24 24"
              fill="none"
              stroke="#D97706"
              stroke-width="2.2"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <path d="M5 22h14"></path>
              <path d="M5 2h14"></path>
              <path d="M17 22v-4.172a2 2 0 0 0-.586-1.414L12 12l-4.414 4.414A2 2 0 0 0 7 17.828V22"></path>
              <path d="M7 2v4.172a2 2 0 0 0 .586 1.414L12 12l4.414-4.414A2 2 0 0 0 17 6.172V2"></path>
            </svg>
          </div>

          <!-- TH 2: MẬT KHẨU KHÔNG ĐÚNG (ICON DẤU TRÒN CÓ CHỮ X) -->
          <div
            v-else-if="modalErrorType === 'INCORRECT_PASSWORD'"
            class="modal-icon-badge error-x-badge"
          >
            <svg
              width="34"
              height="34"
              viewBox="0 0 24 24"
              fill="none"
              stroke="#EF4444"
              stroke-width="2.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="15" y1="9" x2="9" y2="15"></line>
              <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
          </div>

          <!-- TH 3: KHÔNG TÌM THẤY TÀI KHOẢN (ICON KÍNH LÚP) -->
          <div v-else class="modal-icon-badge">
            <svg
              width="34"
              height="34"
              viewBox="0 0 24 24"
              fill="none"
              stroke="#D94E15"
              stroke-width="2.2"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
              <line x1="8" y1="11" x2="14" y2="11"></line>
            </svg>
          </div>

          <h3 class="modal-heading-title">
            {{
              modalErrorType === 'ACCOUNT_PENDING_APPROVAL'
                ? 'Hồ Sơ Đang Chờ Phê Duyệt'
                : modalErrorType === 'INCORRECT_PASSWORD'
                ? 'Mật khẩu không đúng'
                : 'Không tìm thấy tài khoản'
            }}
          </h3>

          <p class="modal-body-text">
            {{ modalErrorMessage }}
          </p>

          <div class="modal-action-buttons">
            <button
              type="button"
              class="btn-modal-close"
              :class="{ 'btn-modal-pending': modalErrorType === 'ACCOUNT_PENDING_APPROVAL' }"
              @click="showNotFoundModal = false"
            >
              {{ modalErrorType === 'ACCOUNT_PENDING_APPROVAL' ? 'Đã hiểu, tôi sẽ chờ duyệt' : 'Đóng thông báo' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- MODAL QUÉT KHUÔN MẶT ĐĂNG NHẬP FACE ID UNIFACE -->
    <FaceScanModal
      v-model="showFaceLoginModal"
      mode="login"
      @success="handleFaceLoginSuccess"
    />

    <!-- MODAL CHÚC MỪNG ĐĂNG NHẬP THÀNH CÔNG VỚI ANIMATION ĐẲNG CẤP -->
    <AuthCelebrationModal
      v-model="showCelebrationModal"
      mode="login"
      :user-name="celebrationUserName"
      :role-name="celebrationRoleName"
      :message="celebrationMessage"
      :countdown-ms="1700"
      @complete="onCelebrationComplete"
    />
  </div>
</template>

<style scoped>
/* ÉP NỔI FONT CHỮ CHUẨN KHÔNG BỊ LỖI FONT */
*,
input,
button,
select,
textarea,
label {
  font-family:
    'Plus Jakarta Sans',
    system-ui,
    -apple-system,
    BlinkMacSystemFont,
    'Segoe UI',
    Roboto,
    Helvetica,
    Arial,
    sans-serif !important;
}

/* PAGE CONTAINER VỪA KHÍT 1 MÀN HÌNH (NO SCROLLBAR, LÙI ẢNH SÁT LỀ TRÁI) */
.login-page-container {
  width: 100vw;
  height: 100vh;
  max-height: 100vh;
  overflow: hidden;
  background: radial-gradient(
    circle at 40% 30%,
    #fffdf9 0%,
    #faf5ef 60%,
    #f3ece2 100%
  );
  display: flex;
  align-items: center;
  justify-content: flex-start;
  padding: 16px 40px 16px 24px;
  box-sizing: border-box;
}

/* HERO LAYOUT 2 CỘT: ẢNH SIÊU TO LÙI TRÁI + BẢNG DỊCH THEO VỀ TRÁI */
.login-hero-layout {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 36px;
  width: 100%;
  max-width: 1220px;
  height: 100%;
  max-height: 580px;
}

/* CỘT TRÁI - ẢNH MINH HỌA SIÊU TO & LÙI SÁT MÉP TRÁI */
.login-illustration-side {
  flex: 1.4;
  max-width: 700px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  position: relative;
}

.illustration-img {
  width: 100%;
  max-width: 680px;
  max-height: 520px;
  object-fit: contain;
  position: relative;
  z-index: 1;
  filter: drop-shadow(0 16px 32px rgba(217, 78, 21, 0.15));
}

/* CỘT PHẢI - BẢNG ĐĂNG NHẬP TRÔI NỔI */
.login-card-floating {
  width: 100%;
  max-width: 430px;
  background: #ffffff;
  border-radius: 24px;
  padding: 24px 28px 22px 28px;
  box-shadow:
    0 20px 45px -10px rgba(15, 23, 42, 0.12),
    0 8px 20px -5px rgba(217, 78, 21, 0.08),
    0 0 0 1px rgba(240, 230, 220, 0.8);
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  z-index: 2;
  position: relative;
}

/* NÚT QUAY LẠI BÊN TRÁI MÉP BẢNG */
.card-top-bar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 8px;
}

.back-home-btn {
  background: transparent;
  border: none;
  color: #64748b;
  font-size: 12.5px;
  font-weight: 700;
  cursor: pointer;
  padding: 2px 0;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  transition: all 0.2s ease;
}

.back-home-btn:hover {
  color: #d94e15;
  transform: translateX(-2px);
}

/* HEADER BẢNG ĐĂNG NHẬP */
.card-brand-header {
  text-align: center;
  margin-bottom: 18px;
}

.form-main-heading {
  font-size: 24px;
  font-weight: 900;
  color: #d94e15;
  margin: 0 0 4px 0;
  letter-spacing: 0.8px;
}

.form-sub-heading {
  font-size: 12.5px;
  color: #64748b;
  margin: 0;
}

/* MESSAGES */
.msg-box {
  padding: 10px 14px;
  border-radius: 12px;
  font-size: 12.5px;
  margin-bottom: 14px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
  animation: msgSlideDown 0.35s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05);
}
@keyframes msgSlideDown {
  from {
    opacity: 0;
    transform: translateY(-8px) scale(0.98);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}
.msg-box.error {
  background: #fef2f2;
  color: #b91c1c;
  border: 1px solid #fecaca;
  box-shadow: 0 4px 14px rgba(239, 68, 68, 0.12);
}
.msg-box.success {
  background: #f0fdf4;
  color: #15803d;
  border: 1px solid #bbf7d0;
  box-shadow: 0 4px 14px rgba(16, 185, 129, 0.15);
}
.msg-box.warning {
  background: #fffbeb;
  color: #b45309;
  border: 1px solid #fde68a;
  box-shadow: 0 4px 14px rgba(245, 158, 11, 0.12);
}

/* FORM FIELDS */
.form-body {
  display: flex;
  flex-direction: column;
  gap: 13px;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.label-row-between {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.field-label {
  font-size: 12.5px;
  font-weight: 700;
  color: #0f172a;
}

.forgot-pass-link {
  font-size: 11.5px;
  color: #d94e15;
  font-weight: 700;
  text-decoration: none;
}
.forgot-pass-link:hover {
  text-decoration: underline;
}

.input-icon-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.input-leading-icon {
  position: absolute;
  left: 12px;
  font-size: 14px;
  pointer-events: none;
}

.field-input {
  width: 100%;
  padding: 9.5px 14px;
  border-radius: 12px;
  border: 1.5px solid #e2e8f0;
  background: #f8fafc;
  font-size: 13px;
  color: #0f172a;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s ease;
  font-family:
    'Plus Jakarta Sans',
    system-ui,
    -apple-system,
    sans-serif !important;
}

.field-input:focus {
  background: #ffffff;
  border-color: #d94e15;
  box-shadow: 0 0 0 3px rgba(217, 78, 21, 0.14);
}

.password-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.password-wrapper .field-input {
  padding-left: 12px;
  padding-right: 38px;
}

.eye-toggle-btn {
  position: absolute;
  right: 12px;
  background: none;
  border: none;
  cursor: pointer;
  padding: 2px;
  display: flex;
  align-items: center;
}

/* CHECKBOX ROW */
.form-options-row {
  margin-top: 1px;
}

.custom-checkbox-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: #475569;
  cursor: pointer;
  font-weight: 500;
}

.custom-checkbox {
  accent-color: #d94e15;
  width: 15px;
  height: 15px;
  cursor: pointer;
}

/* SUBMIT BUTTON ROW & WARM ORANGE PILL */
.login-action-row {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  margin-top: 4px;
}

.btn-submit-orange {
  flex: 1;
  height: 44px;
  padding: 0 18px;
  background: #d94e15;
  color: #ffffff;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  letter-spacing: 0.5px;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-submit-orange:hover:not(:disabled) {
  background: #c8451f;
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(217, 78, 21, 0.38);
}

.btn-submit-orange:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

/* QUICK FACE ID BIOMETRIC ICON BUTTON */
.btn-face-quick-icon {
  width: 44px;
  height: 44px;
  flex-shrink: 0;
  border-radius: 14px;
  background: linear-gradient(135deg, #0f172a, #1e293b);
  border: 1.5px solid rgba(56, 189, 248, 0.45);
  color: #38bdf8;
  font-size: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.25), 0 0 12px rgba(56, 189, 248, 0.15);
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  padding: 0;
  position: relative;
  overflow: hidden;
}

.btn-face-quick-icon::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(56, 189, 248, 0.3), transparent);
  transition: left 0.5s ease;
}

.btn-face-quick-icon:hover::before {
  left: 100%;
}

.btn-face-quick-icon:hover {
  background: linear-gradient(135deg, #0284c7, #2563eb);
  border-color: #38bdf8;
  color: #ffffff;
  transform: translateY(-1.5px) scale(1.04);
  box-shadow: 0 6px 20px rgba(37, 99, 235, 0.45), 0 0 16px rgba(56, 189, 248, 0.4);
}

.btn-face-quick-icon:active {
  transform: translateY(0) scale(0.96);
}

.apple-faceid-svg {
  width: 22px;
  height: 22px;
  width: 25px;
  height: 25px;
  display: block;
  transition: transform 0.25s ease;
}

.btn-face-quick-icon:hover .apple-faceid-svg {
  transform: scale(1.08);
}

/* QUICK DEMO ROLES */
.quick-demo-roles-box {
  background: #fdfaf6;
  border: 1px dashed #ebdcd3;
  border-radius: 12px;
  padding: 8px 10px;
  margin: 4px 0;
}
.demo-roles-title {
  font-size: 11px;
  font-weight: 700;
  color: #7a6358;
  display: flex;
  align-items: center;
  gap: 5px;
  margin-bottom: 6px;
}
.demo-roles-title i {
  color: #d85a2a;
  font-size: 12px;
}
.demo-roles-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 6px;
}
.demo-role-chip {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 6px 4px;
  border-radius: 8px;
  border: 1px solid #ebdcd3;
  background: #ffffff;
  cursor: pointer;
  transition: all 0.2s ease;
}
.demo-role-chip .role-icon {
  font-size: 14px;
}
.demo-role-chip .role-txt {
  font-size: 10px;
  font-weight: 700;
  color: #4a3830;
}
.demo-role-chip.buyer:hover {
  background: #eef6ff;
  border-color: #93c5fd;
  transform: translateY(-2px);
}
.demo-role-chip.seller:hover {
  background: #fff7ed;
  border-color: #fdba74;
  transform: translateY(-2px);
}
.demo-role-chip.shipper:hover {
  background: #f0fdf4;
  border-color: #86efac;
  transform: translateY(-2px);
}
.demo-role-chip.admin:hover {
  background: #faf5ff;
  border-color: #d8b4fe;
  transform: translateY(-2px);
}

/* DIVIDER */
.divider-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 2px 0;
}

.divider-row .line {
  flex: 1;
  height: 1px;
  background: #e2e8f0;
}

.divider-row .or-text {
  font-size: 10px;
  font-weight: 800;
  color: #94a3b8;
  letter-spacing: 0.5px;
}

/* REGISTER CTA BOX */
.register-cta-box {
  display: flex;
  flex-direction: column;
  gap: 5px;
  text-align: center;
  font-size: 12px;
  color: #64748b;
}

.link-orange-bold {
  color: #d94e15;
  font-weight: 800;
  text-decoration: none;
}
.link-orange-bold:hover {
  text-decoration: underline;
}

.link-secondary {
  color: #2563eb;
  font-weight: 700;
  text-decoration: none;
}
.link-secondary:hover {
  text-decoration: underline;
}

/* DEMO QUICK BAR */
.demo-quick-bar {
  margin-top: 14px;
  padding-top: 12px;
  border-top: 1px dashed #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.demo-title {
  font-size: 11.5px;
  font-weight: 700;
  color: #64748b;
  text-align: center;
}
.demo-tags-wrap {
  display: flex;
  justify-content: center;
  gap: 6px;
  flex-wrap: wrap;
}
.demo-btn {
  background: #f8fafc;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  padding: 5px 10px;
  font-size: 11.5px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
}
.demo-btn.seller {
  color: #c2410c;
  background: #fff7ed;
  border-color: #fdba74;
}
.demo-btn.seller:hover {
  background: #ea580c;
  color: #ffffff;
}
.demo-btn.buyer {
  color: #15803d;
  background: #f0fdf4;
  border-color: #86efac;
}
.demo-btn.buyer:hover {
  background: #16a34a;
  color: #ffffff;
}
.demo-btn.shipper {
  color: #1d4ed8;
  background: #eff6ff;
  border-color: #93c5fd;
}
.demo-btn.shipper:hover {
  background: #2563eb;
  color: #ffffff;
}

/* MODAL THÔNG BÁO ĐÈ MÀN HÌNH VỚI BÓNG MỜ MỜ (CENTRED OVERLAY) */
.modal-backdrop-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(15, 23, 42, 0.55);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 99999;
  padding: 20px;
  box-sizing: border-box;
}

.modal-pop-card {
  background: #ffffff;
  border-radius: 24px;
  padding: 28px 24px 22px 24px;
  width: 100%;
  max-width: 380px;
  text-align: center;
  box-shadow:
    0 25px 50px -12px rgba(0, 0, 0, 0.3),
    0 0 0 1px rgba(240, 230, 220, 0.9);
  display: flex;
  flex-direction: column;
  align-items: center;
  animation: modalScaleUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalScaleUp {
  from {
    opacity: 0;
    transform: scale(0.88) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-icon-badge {
  width: 60px;
  height: 60px;
  background: #fff7ed;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
  border: 2px solid #ffedd5;
  box-shadow: 0 4px 12px rgba(217, 78, 21, 0.15);
}

.modal-icon-badge.error-x-badge {
  background: #fef2f2;
  border: 2px solid #fee2e2;
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.18);
}

.modal-icon-badge.pending-badge {
  background: #fffbeb;
  border: 2px solid #fde68a;
  box-shadow: 0 4px 14px rgba(217, 119, 6, 0.2);
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

.btn-modal-pending {
  background: linear-gradient(135deg, #d97706 0%, #b45309 100%) !important;
  color: #ffffff !important;
  box-shadow: 0 4px 14px rgba(217, 119, 6, 0.3) !important;
  border-radius: 30px;
  padding: 11px !important;
  font-size: 13px !important;
  border: none !important;
}

.btn-modal-pending:hover {
  background: linear-gradient(135deg, #b45309 0%, #92400e 100%) !important;
  transform: translateY(-1px);
}

.modal-heading-title {
  font-size: 19px;
  font-weight: 900;
  color: #0f172a;
  margin: 0 0 8px 0;
  letter-spacing: 0.3px;
}

.modal-body-text {
  font-size: 13px;
  color: #475569;
  margin: 0 0 20px 0;
  line-height: 1.5;
}

.modal-action-buttons {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.btn-modal-register {
  width: 100%;
  padding: 11px;
  background: #d94e15;
  color: #ffffff;
  border: none;
  border-radius: 30px;
  font-size: 13px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
}

.btn-modal-register:hover {
  background: #c8451f;
  transform: translateY(-1px);
}

.btn-modal-close {
  width: 100%;
  padding: 9px;
  background: transparent;
  color: #64748b;
  border: none;
  font-size: 12.5px;
  font-weight: 700;
  cursor: pointer;
  transition: color 0.2s ease;
}

.btn-modal-close:hover {
  color: #0f172a;
}

/* TRANSITION FADE */
.fade-modal-enter-active,
.fade-modal-leave-active {
  transition: opacity 0.25s ease;
}

.fade-modal-enter-from,
.fade-modal-leave-to {
  opacity: 0;
}

.zalo-hint-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 11.5px;
  font-weight: 700;
  color: #0068ff;
  background: #f0f7ff;
  border: 1px solid #c7e0ff;
  padding: 2px 8px;
  border-radius: 6px;
}
</style>
