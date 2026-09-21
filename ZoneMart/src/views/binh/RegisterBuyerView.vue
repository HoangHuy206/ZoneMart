<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG KÝ KHÁCH HÀNG (REGISTER BUYER VIEW) - Phụ trách: Bình
 * Giao diện Bảng Đăng Ký Khách Hàng giữ nguyên Nền & Khung Hero
 * Đầy đủ trường: Gmail, Mật Khẩu, Nhập Lại Mật Khẩu, Mã Captcha
 * Lưu trực tiếp vào CSDL MongoDB Atlas & Gửi Email xác nhận qua Gmail SMTP
 * ================================================================
 */
import { ref, reactive, onMounted } from "vue";
import { useRouter } from "vue-router";
import AuthCelebrationModal from "../../components/common/AuthCelebrationModal.vue";
import { useToast } from "../../composables/useToast";

const router = useRouter();

// Form State
const form = reactive({
  email: "",
  fullName: "",
  password: "",
  confirmPassword: "",
  captchaInput: ""
});

const captchaCode = ref("");
const showPassword = ref(false);
const showConfirmPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref("");
const successMessage = ref("");

// Trạng thái cho Modal Chúc mừng Hoạt Họa siêu xịn
const toast = useToast();
const showCelebrationModal = ref(false);
const celebrationUserName = ref("");
const celebrationMessage = ref("");

const onCelebrationComplete = () => {
  showCelebrationModal.value = false;
  router.push("/login");
};

const isRotatingCaptcha = ref(false);

// Hàm tạo Mã Captcha ngẫu nhiên 4 ký tự
const generateCaptcha = () => {
  const chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // Bỏ các ký tự dễ nhầm lẫn như 0, O, 1, I
  let result = "";
  for (let i = 0; i < 4; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  captchaCode.value = result;
  form.captchaInput = "";
};

const refreshCaptchaWithAnim = () => {
  isRotatingCaptcha.value = true;
  generateCaptcha();
  setTimeout(() => {
    isRotatingCaptcha.value = false;
  }, 600);
};

onMounted(() => {
  sessionStorage.removeItem("pendingRegisterTarget");
  generateCaptcha();
});

const togglePassword = () => {
  showPassword.value = !showPassword.value;
};

const toggleConfirmPassword = () => {
  showConfirmPassword.value = !showConfirmPassword.value;
};

const showModal = ref(false);
const modalErrorMessage = ref("");

// Xử lý Đăng Ký
const handleRegister = async () => {
  errorMessage.value = "";
  successMessage.value = "";

  // 1. Kiểm tra Email/Gmail hợp lệ
  const emailTrimmed = form.email.trim().toLowerCase();
  const gmailRegex = /^[a-zA-Z0-9._%+-]+@gmail\.com$/;
  if (!emailTrimmed || !gmailRegex.test(emailTrimmed)) {
    modalErrorMessage.value = "Vui lòng nhập đúng định dạng Địa chỉ Gmail (ví dụ: name@gmail.com)!";
    toast.warning(modalErrorMessage.value, "Email chưa hợp lệ");
    showModal.value = true;
    return;
  }

  // 2. Kiểm tra Họ và Tên
  if (!form.fullName.trim()) {
    modalErrorMessage.value = "Vui lòng nhập Họ và Tên của bạn!";
    toast.warning(modalErrorMessage.value, "Thiếu họ tên");
    showModal.value = true;
    return;
  }

  // 3. Kiểm tra độ dài Mật khẩu tối thiểu 6 ký tự
  if (form.password.length < 6) {
    modalErrorMessage.value = "Mật khẩu phải chứa ít nhất 6 ký tự!";
    showModal.value = true;
    return;
  }

  // 4. Kiểm tra khớp mật khẩu
  if (form.password !== form.confirmPassword) {
    modalErrorMessage.value = "Mật khẩu nhập lại không trùng khớp! Vui lòng kiểm tra lại.";
    showModal.value = true;
    return;
  }

  // 5. Kiểm tra mã Captcha
  if (form.captchaInput.trim().toUpperCase() !== captchaCode.value.toUpperCase()) {
    modalErrorMessage.value = "Mã Captcha không chính xác! Vui lòng thử lại.";
    showModal.value = true;
    generateCaptcha();
    return;
  }

  isLoading.value = true;

  try {
    const res = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: form.email.trim().toLowerCase(),
        password: form.password,
        fullName: form.fullName.trim() || form.email.trim().split('@')[0]
      })
    }).catch(() => null);

    if (res) {
      const data = await res.json();
      if (res.ok && data.success) {
        sessionStorage.removeItem("pendingRegisterTarget");
        celebrationUserName.value = form.fullName.trim() || form.email.trim().split('@')[0];
        celebrationMessage.value = data.message || `Tạo tài khoản thành công! Thông báo xác nhận đã gửi về: ${form.email.trim()}`;
        successMessage.value = celebrationMessage.value;
        toast.success("Tạo tài khoản khách hàng thành công!", "Đăng Ký Thành Công");
        showCelebrationModal.value = true;
      } else {
        modalErrorMessage.value = data.message || "Đăng ký thất bại. Vui lòng thử lại sau.";
        toast.error(modalErrorMessage.value, "Đăng Ký Thất Bại");
        showModal.value = true;
        generateCaptcha();
      }
    } else {
      modalErrorMessage.value = "Không thể kết nối đến máy chủ backend. Vui lòng kiểm tra kết nối mạng.";
      toast.error(modalErrorMessage.value, "Lỗi Kết Nối");
      showModal.value = true;
      generateCaptcha();
    }
  } catch (error: any) {
    modalErrorMessage.value = error.message || "Có lỗi xảy ra trong quá trình đăng ký.";
    toast.error(modalErrorMessage.value, "Lỗi Ngoại Lệ");
    showModal.value = true;
    generateCaptcha();
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <div class="login-page-container">
    <!-- KHUNG TỔNG THỂ 2 CỘT: ẢNH BÊN TRÁI + BẢNG ĐĂNG KÝ BÊN PHẢI (GIỮ NỀN VÀ BỐ CỤC) -->
    <div class="login-hero-layout">
      
      <!-- CỘT TRÁI: ẢNH MINH HỌA XE ZONEMART TÁCH SẠCH NỀN (GIỮ NGUYÊN) -->
      <div class="login-illustration-side">
        <img
          src="/images/anhxoanen_clean.png"
          alt="ZoneMart Giao Hàng Hỏa Tốc"
          class="illustration-img"
        />
      </div>

      <!-- CỘT PHẢI: BẢNG ĐĂNG KÝ KHÁCH HÀNG TRÔI NỔI (FLOATING CARD) -->
      <div class="login-card-floating">
        
        <!-- THANH ĐIỀU HƯỚNG QUAY LẠI CƠ BẢN -->
        <div class="card-top-bar">
          <button type="button" class="back-home-btn" @click="router.push('/login')" title="Quay lại trang Đăng Nhập">
            ← Quay lại Đăng nhập
          </button>
        </div>

        <!-- TIÊU ĐỀ ĐĂNG KÝ -->
        <div class="card-brand-header">
          <h1 class="form-main-heading">ĐĂNG KÝ KHÁCH HÀNG</h1>
          <p class="form-sub-heading">Tạo tài khoản để trải nghiệm mua sắm & giao hỏa tốc ZoneMart</p>
        </div>

        <!-- THÔNG BÁO LỖI VÀ THÀNH CÔNG -->
        <div v-if="errorMessage" class="msg-box error">⚠️ {{ errorMessage }}</div>
        <div v-if="successMessage" class="msg-box success">✅ {{ successMessage }}</div>

        <!-- FORM NHẬP THÔNG TIN ĐĂNG KÝ (CHUẨN SEO SEMANTIC & ACCESSIBILITY) -->
        <form @submit.prevent="handleRegister" class="form-body" method="post" action="/api/auth/register" novalidate itemscope itemtype="https://schema.org/WebPage">
          
          <!-- Họ và Tên -->
          <div class="field-item">
            <label class="field-label">Họ và Tên <span class="req">*</span></label>
            <input
              v-model="form.fullName"
              type="text"
              class="field-input"
              placeholder="VD: Nguyễn Văn A..."
              required
            />
          </div>

          <!-- Gmail -->
          <div class="field-item">
            <label for="buyer-email" class="field-label">Địa chỉ Gmail <span class="req">*</span></label>
            <div class="input-icon-wrapper">
              <i class="bi bi-envelope-fill input-leading-icon"></i>
              <input
                id="buyer-email"
                name="email"
                v-model="form.email"
                type="email"
                autocomplete="email"
                class="field-input has-leading-icon"
                placeholder="VD: nguyenvana@gmail.com"
                required
                aria-required="true"
              />
            </div>
          </div>

          <!-- Mật khẩu -->
          <div class="field-item">
            <label for="buyer-password" class="field-label">Mật khẩu <span class="req">*</span></label>
            <div class="password-wrapper input-icon-wrapper">
              <i class="bi bi-lock-fill input-leading-icon"></i>
              <input
                id="buyer-password"
                name="new-password"
                v-model="form.password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="new-password"
                class="field-input has-leading-icon has-trailing-btn"
                placeholder="Nhập mật khẩu (6 - 16 ký tự)..."
                minlength="6"
                maxlength="16"
                required
                aria-required="true"
              />
              <button type="button" class="eye-toggle-btn" @click="togglePassword" :title="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'" aria-label="Ẩn hoặc hiện mật khẩu">
                <i :class="showPassword ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill'"></i>
              </button>
            </div>
          </div>

          <!-- Nhập lại mật khẩu -->
          <div class="field-item">
            <label for="buyer-confirm-password" class="field-label">Nhập lại mật khẩu <span class="req">*</span></label>
            <div class="password-wrapper input-icon-wrapper">
              <i class="bi bi-shield-lock-fill input-leading-icon"></i>
              <input
                id="buyer-confirm-password"
                name="confirm-password"
                v-model="form.confirmPassword"
                :type="showConfirmPassword ? 'text' : 'password'"
                autocomplete="new-password"
                class="field-input has-leading-icon has-trailing-btn"
                placeholder="Nhập lại mật khẩu ở trên..."
                minlength="6"
                maxlength="16"
                required
                aria-required="true"
              />
              <button type="button" class="eye-toggle-btn" @click="toggleConfirmPassword" :title="showConfirmPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'" aria-label="Ẩn hoặc hiện mật khẩu nhập lại">
                <i :class="showConfirmPassword ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill'"></i>
              </button>
            </div>
          </div>

          <!-- Mã Captcha xác nhận -->
          <div class="field-item">
            <label for="buyer-captcha" class="field-label">Mã Captcha xác nhận <span class="req">*</span></label>
            <div class="captcha-row">
              <div class="input-icon-wrapper flex-1">
                <i class="bi bi-shield-check input-leading-icon"></i>
                <input
                  id="buyer-captcha"
                  name="captcha"
                  v-model="form.captchaInput"
                  type="text"
                  autocomplete="off"
                  class="field-input has-leading-icon captcha-input"
                  placeholder="Nhập 4 ký tự..."
                  maxlength="4"
                  required
                  aria-required="true"
                />
              </div>
              <div class="captcha-badge" title="Mã xác thực Captcha">
                <span class="captcha-text font-mono">{{ captchaCode }}</span>
                <button
                  type="button"
                  class="refresh-captcha-btn"
                  :class="{ 'is-spinning': isRotatingCaptcha }"
                  @click="refreshCaptchaWithAnim"
                  title="Làm mới mã Captcha"
                  aria-label="Làm mới mã Captcha"
                >
                  <i class="bi bi-arrow-clockwise"></i>
                </button>
              </div>
            </div>
          </div>

          <!-- Nút Đăng Ký -->
          <button type="submit" class="btn-submit-orange" :disabled="isLoading">
            <span v-if="!isLoading" class="btn-content-flex">
              <span>ĐĂNG KÝ TÀI KHOẢN</span>
              <i class="bi bi-arrow-right-circle-fill ms-1"></i>
            </span>
            <span v-else class="btn-content-flex">
              <span class="spinner-border-sm me-2"></span>
              <span>ĐANG XỬ LÝ...</span>
            </span>
          </button>

          <!-- Đường kẻ phân cách -->
          <div class="divider-row">
            <span class="line"></span>
            <span class="or-text">BẠN ĐÃ CÓ TÀI KHOẢN?</span>
            <span class="line"></span>
          </div>

          <!-- Liên kết đăng nhập -->
          <div class="register-cta-box">
            <div class="cta-line">
              <router-link to="/login" class="link-orange-bold">Đăng nhập ngay tại đây</router-link>
            </div>
          </div>

        </form>

      </div>

    </div>

    <!-- MODAL THÔNG BÁO ĐĂNG KÝ (CHỈ CÓ 1 NÚT ĐÓNG THÔNG BÁO) -->
    <Transition name="fade-modal">
      <div v-if="showModal" class="modal-backdrop-overlay" @click.self="showModal = false">
        <div class="modal-pop-card">
          <div class="modal-icon-badge error-x-badge">
            <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="#EF4444" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="15" y1="9" x2="9" y2="15"></line>
              <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
          </div>

          <h3 class="modal-heading-title">Thông báo</h3>
          
          <p class="modal-body-text">
            {{ modalErrorMessage }}
          </p>

          <div class="modal-action-buttons">
            <button type="button" class="btn-modal-close" @click="showModal = false">
              Đóng thông báo
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- MODAL CHÚC MỪNG ĐĂNG KÝ THÀNH CÔNG VỚI ANIMATION ĐẲNG CẤP -->
    <AuthCelebrationModal
      v-model="showCelebrationModal"
      mode="register"
      :user-name="celebrationUserName"
      role-name="Khách Hàng ZoneMart"
      title="ĐĂNG KÝ TÀI KHOẢN THÀNH CÔNG!"
      :message="celebrationMessage"
      :countdown-ms="2200"
      @complete="onCelebrationComplete"
    />
  </div>
</template>

<style scoped>
*, input, button, select, textarea, label {
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
}

/* MODAL POPUP STYLING */
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
  background: #FFFFFF;
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
  background: #FFF7ED;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
  border: 2px solid #FFEDD5;
  box-shadow: 0 4px 12px rgba(217, 78, 21, 0.15);
}

.modal-icon-badge.error-x-badge {
  background: #FEF2F2;
  border: 2px solid #FEE2E2;
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.18);
}

.modal-heading-title {
  font-size: 19px;
  font-weight: 900;
  color: #0F172A;
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

.btn-modal-close {
  width: 100%;
  padding: 11px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
}

.btn-modal-close:hover {
  background: #C8451F;
  transform: translateY(-1px);
}

.fade-modal-enter-active,
.fade-modal-leave-active {
  transition: opacity 0.25s ease;
}

.fade-modal-enter-from,
.fade-modal-leave-to {
  opacity: 0;
}

/* PAGE CONTAINER VỪA KHÍT 1 MÀN HÌNH (NO SCROLLBAR, GIỮ NỀN #FAF5EF) */
.login-page-container {
  width: 100vw;
  height: 100vh;
  max-height: 100vh;
  overflow: hidden;
  background: radial-gradient(circle at 40% 30%, #FFFDF9 0%, #FAF5EF 60%, #F3ECE2 100%);
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

/* CỘT PHẢI - BẢNG ĐĂNG KÝ TRÔI NỔI */
.login-card-floating {
  width: 100%;
  max-width: 430px;
  background: #FFFFFF;
  border-radius: 24px;
  padding: 22px 28px 20px 28px;
  box-shadow: 
    0 20px 45px -10px rgba(15, 23, 42, 0.12),
    0 8px 20px -5px rgba(217, 78, 21, 0.08),
    0 0 0 1px rgba(240, 230, 220, 0.8);
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  z-index: 2;
  position: relative;
  animation: formCardEntrance 0.45s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes formCardEntrance {
  0% {
    opacity: 0;
    transform: translateY(18px) scale(0.985);
  }
  100% {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* NÚT QUAY LẠI BÊN TRÁI MÉP BẢNG */
.card-top-bar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 6px;
}

.back-home-btn {
  background: transparent;
  border: none;
  color: #64748B;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  padding: 2px 0;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  transition: all 0.2s ease;
}

.back-home-btn:hover {
  color: #D94E15;
  transform: translateX(-2px);
}

/* HEADER BẢNG ĐĂNG KÝ */
.card-brand-header {
  text-align: center;
  margin-bottom: 14px;
}

.form-main-heading {
  font-size: 22px;
  font-weight: 900;
  color: #D94E15;
  margin: 0 0 4px 0;
  letter-spacing: 0.5px;
}

.form-sub-heading {
  font-size: 12px;
  color: #64748B;
  margin: 0;
}

/* MESSAGES */
.msg-box {
  padding: 10px 14px;
  border-radius: 12px;
  font-size: 12.5px;
  margin-bottom: 12px;
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
.msg-box.error { background: #FEF2F2; color: #B91C1C; border: 1px solid #FECACA; box-shadow: 0 4px 14px rgba(239, 68, 68, 0.12); }
.msg-box.success { background: #F0FDF4; color: #15803D; border: 1px solid #BBF7D0; box-shadow: 0 4px 14px rgba(16, 185, 129, 0.15); }

/* FORM FIELDS */
.form-body {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.field-label {
  font-size: 12px;
  font-weight: 700;
  color: #0F172A;
}

.req {
  color: #EF4444;
}

.input-icon-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-leading-icon {
  position: absolute;
  left: 13px;
  font-size: 14px;
  color: #94A3B8;
  pointer-events: none;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  z-index: 2;
}

.input-icon-wrapper:focus-within .input-leading-icon {
  color: #D94E15;
  transform: scale(1.12);
}

.field-input {
  width: 100%;
  padding: 8.5px 12px;
  border-radius: 12px;
  border: 1.5px solid #E2E8F0;
  background: #F8FAFC;
  font-size: 12.5px;
  color: #0F172A;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s ease;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  font-family: inherit;
}

.field-input.has-leading-icon {
  padding-left: 36px;
}

.field-input.has-trailing-btn {
  padding-right: 36px;
}

.field-input:focus {
  background: #FFFFFF;
  border-color: #D94E15;
  box-shadow: 0 0 0 3px rgba(217, 78, 21, 0.14);
}

.password-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.password-wrapper .field-input {
  padding-right: 36px;
}

.eye-toggle-btn {
  position: absolute;
  right: 10px;
  right: 11px;
  background: none;
  border: none;
  color: #94A3B8;
  cursor: pointer;
  padding: 2px;
  padding: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  transition: color 0.2s ease, transform 0.15s ease;
  z-index: 2;
}

.eye-toggle-btn:hover {
  color: #D94E15;
}

.eye-toggle-btn:active {
  transform: scale(0.9);
}

/* CAPTCHA ROW */
.captcha-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.captcha-input {
  flex: 1;
  text-transform: uppercase;
  letter-spacing: 1px;
  font-weight: 700;
}

.captcha-badge {
  display: flex;
  align-items: center;
  gap: 8px;
  background: linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 100%);
  border: 1.5px dashed #D94E15;
  padding: 6px 12px;
  padding: 5px 11px;
  border-radius: 12px;
  user-select: none;
}

.captcha-text {
  font-family: 'Courier New', Courier, monospace !important;
  font-size: 18px;
  font-size: 17px;
  font-weight: 900;
  color: #D94E15;
  letter-spacing: 4px;
  letter-spacing: 3px;
  font-style: italic;
  text-decoration: line-through;
}

.refresh-captcha-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 14px;
  padding: 0;
  font-size: 16px;
  padding: 2px;
  color: #D94E15;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.2s ease;
}

.refresh-captcha-btn:hover {
  transform: rotate(180deg);
  transform: scale(1.15);
}

.refresh-captcha-btn.is-spinning {
  animation: rotateCaptcha 0.55s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes rotateCaptcha {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

/* SUBMIT BUTTON - WARM ORANGE PILL */
.btn-submit-orange {
  width: 100%;
  padding: 10.5px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.5px;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  margin-top: 4px;
}

.btn-content-flex {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.btn-submit-orange:hover:not(:disabled) {
  background: #C8451F;
  transform: translateY(-1px);
  transform: translateY(-1.5px);
  box-shadow: 0 8px 20px rgba(217, 78, 21, 0.38);
}

.btn-submit-orange:active:not(:disabled) {
  transform: translateY(1px) scale(0.985);
  box-shadow: 0 3px 8px rgba(217, 78, 21, 0.25);
}

.btn-submit-orange:disabled {
  opacity: 0.65;
  cursor: not-allowed;
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
  background: #E2E8F0;
}

.divider-row .or-text {
  font-size: 10px;
  font-weight: 800;
  color: #94A3B8;
  letter-spacing: 0.5px;
}

/* REGISTER CTA BOX */
.register-cta-box {
  display: flex;
  flex-direction: column;
  gap: 4px;
  text-align: center;
  font-size: 12px;
  color: #64748B;
}

.link-orange-bold {
  color: #D94E15;
  font-weight: 800;
  text-decoration: none;
}
.link-orange-bold:hover {
  text-decoration: underline;
}

/* RESPONSIVE */
@media (max-width: 900px) {
  .login-illustration-side {
    display: none;
  }
  .login-hero-layout {
    justify-content: center;
  }
}
</style>
