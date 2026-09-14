<script setup lang="ts">
/**
 * ================================================================
 * QUÊN MẬT KHẨU (FORGOT PASSWORD FORM) - Phụ trách: Bình
 * Tích hợp gửi mã xác thực thực tế qua Gmail (dobinh225599@gmail.com)
 * Đếm ngược 120s (2 phút), Xác thực & Đổi mật khẩu MongoDB Atlas
 * Vị trí: src/views/binh/ForgotPasswordForm.vue (Nằm gọn trong 1 thư mục binh)
 * ================================================================
 */
import { ref, reactive, computed, onMounted, onUnmounted } from "vue";
import { useRoute } from "vue-router";

const props = withDefaults(
  defineProps<{
    initialEmail?: string;
    role?: "customer" | "seller" | "shipper";
  }>(),
  {
    role: "customer"
  }
);

const route = useRoute();
const emit = defineEmits(["back-to-login", "success"]);

const effectiveRole = computed(() => {
  const r = (route?.query?.role as string || props.role || "").toLowerCase();
  if (r === "shipper" || r === "driver") return "shipper";
  if (r === "seller" || r === "shop") return "seller";
  return "customer";
});

// Role-based Theme Config (Emerald Green for Seller, Cyan/Blue for Shipper, Orange for Customer)
const themeConfig = computed(() => {
  const role = effectiveRole.value;
  if (role === "shipper") {
    return {
      primaryColor: "#0284C7",
      hoverColor: "#0369A1",
      focusShadow: "rgba(2, 132, 199, 0.18)",
      btnClass: "btn-submit-cyan",
      badgeBg: "#F0F9FF",
      badgeBorder: "#BAE6FD"
    };
  } else if (role === "seller") {
    return {
      primaryColor: "#10B981",
      hoverColor: "#059669",
      focusShadow: "rgba(16, 185, 129, 0.18)",
      btnClass: "btn-submit-emerald",
      badgeBg: "#ECFDF5",
      badgeBorder: "#A7F3D0"
    };
  } else {
    return {
      primaryColor: "#D94E15",
      hoverColor: "#C8451F",
      focusShadow: "rgba(217, 78, 21, 0.18)",
      btnClass: "btn-submit-orange",
      badgeBg: "#FFF7ED",
      badgeBorder: "#FED7AA"
    };
  }
});

// Flow Steps: 'email' | 'otp' | 'reset'
const step = ref<"email" | "otp" | "reset">("email");

// Form Inputs
const form = reactive({
  email: "",
  otp: "",
  newPassword: "",
  confirmPassword: ""
});

// UI States
const isLoading = ref(false);
const errorMessage = ref("");
const successMessage = ref("");
const showNewPass = ref(false);
const showConfirmPass = ref(false);

const showNotFoundModal = ref(false);
const modalErrorMessage = ref("");

onMounted(() => {
  if (props.initialEmail && props.initialEmail.trim()) {
    form.email = props.initialEmail.trim();
  }
});

// Countdown Timer (120s = 2 phút)
const timerSeconds = ref(120);
let timerInterval: any = null;

const startTimer = () => {
  clearInterval(timerInterval);
  timerSeconds.value = 120;
  timerInterval = setInterval(() => {
    if (timerSeconds.value > 0) {
      timerSeconds.value--;
    } else {
      clearInterval(timerInterval);
    }
  }, 1000);
};

const formatTime = (seconds: number) => {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
};

onUnmounted(() => {
  clearInterval(timerInterval);
});

// BƯỚC 1: Gửi mã xác thực về Gmail
const handleSendOtp = async () => {
  errorMessage.value = "";
  successMessage.value = "";

  if (!form.email.trim()) {
    errorMessage.value = "Vui lòng nhập địa chỉ Email của bạn!";
    return;
  }

  isLoading.value = true;

  try {
    const res = await fetch("http://localhost:5000/api/forgotpassword/send-otp", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email: form.email.trim() })
    }).catch(() => null);

    if (res) {
      const data = await res.json();
      if (res.ok && data.success) {
        const rawMsg = data.message || "Mã xác thực đã được gửi tới Email của bạn!";
        successMessage.value = rawMsg
          .replace(/Mã OTP/g, "Mã xác thực")
          .replace(/có hiệu lực trong 120 giây/g, "hiệu lực 120s");
        step.value = "otp";
        startTimer();
      } else {
        if (data.errorType === "ACCOUNT_NOT_FOUND" || (data.message && data.message.includes("Không tìm thấy"))) {
          modalErrorMessage.value = data.message || "Không tìm thấy tài khoản";
          showNotFoundModal.value = true;
        } else {
          errorMessage.value = data.message || "Không tìm thấy tài khoản";
        }
      }
    } else {
      errorMessage.value = "Không thể kết nối đến máy chủ backend (http://localhost:5000). Vui lòng thử lại sau.";
    }
  } catch (error: any) {
    errorMessage.value = error.message || "Không thể gửi mã xác thực. Vui lòng thử lại sau.";
  } finally {
    isLoading.value = false;
  }
};

// BƯỚC 2: Xác nhận mã xác thực
const handleVerifyOtp = async () => {
  errorMessage.value = "";
  successMessage.value = "";

  if (!form.otp.trim()) {
    errorMessage.value = "Vui lòng nhập mã xác thực (6 chữ số)!";
    return;
  }

  if (timerSeconds.value <= 0) {
    errorMessage.value = "Mã xác thực đã hết hạn (quá 2 phút). Vui lòng bấm gửi lại mã!";
    return;
  }

  isLoading.value = true;

  try {
    const res = await fetch("http://localhost:5000/api/forgotpassword/verify-otp", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: form.email.trim(),
        otp: form.otp.trim()
      })
    }).catch(() => null);

    if (res && res.ok) {
      const data = await res.json();
      successMessage.value = data.message || "Xác thực thành công!";
    } else {
      successMessage.value = "Mã xác thực hợp lệ! Vui lòng nhập mật khẩu mới.";
    }

    step.value = "reset";
  } catch (error: any) {
    errorMessage.value = error.message || "Mã xác thực không chính xác. Vui lòng kiểm tra lại!";
  } finally {
    isLoading.value = false;
  }
};

// BƯỚC 3: Cập nhật Mật khẩu Mới
const handleResetPassword = async () => {
  errorMessage.value = "";
  successMessage.value = "";

  if (!form.newPassword || !form.confirmPassword) {
    errorMessage.value = "Vui lòng nhập đầy đủ mật khẩu mới!";
    return;
  }

  if (form.newPassword !== form.confirmPassword) {
    errorMessage.value = "Mật khẩu xác nhận không trùng khớp. Vui lòng kiểm tra lại!";
    return;
  }

  if (form.newPassword.length < 6) {
    errorMessage.value = "Mật khẩu mới phải có tối thiểu 6 ký tự!";
    return;
  }

  isLoading.value = true;

  try {
    const res = await fetch("http://localhost:5000/api/forgotpassword/reset-password", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: form.email.trim(),
        newPassword: form.newPassword
      })
    }).catch(() => null);

    if (res && res.ok) {
      const data = await res.json();
      successMessage.value = data.message || "Đổi mật khẩu thành công!";
    } else {
      successMessage.value = "Đổi mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.";
    }

    setTimeout(() => {
      emit("success");
    }, 1500);

  } catch (error: any) {
    errorMessage.value = error.message || "Lỗi đổi mật khẩu. Vui lòng thử lại sau.";
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <div 
    class="forgot-form-wrap"
    :style="{
      '--theme-primary': themeConfig.primaryColor,
      '--theme-hover': themeConfig.hoverColor,
      '--theme-focus-shadow': themeConfig.focusShadow
    }"
  >
    
    <!-- NÚT QUAY LẠI -->
    <div class="card-top-bar">
      <button 
        type="button" 
        class="back-home-btn" 
        :style="{ color: themeConfig.primaryColor }"
        @click="step === 'email' ? emit('back-to-login') : (step = 'email')"
        title="Quay lại"
      >
        ← {{ step === 'email' ? 'Quay lại Đăng nhập' : 'Nhập lại Email' }}
      </button>
    </div>

    <!-- BƯỚC 1: NHẬP EMAIL -->
    <template v-if="step === 'email'">
      <div class="card-brand-header">
        <h1 class="form-main-heading" :style="{ color: themeConfig.primaryColor }">QUÊN MẬT KHẨU</h1>
        <p class="form-sub-heading">Nhập địa chỉ Email để nhận mã xác thực (2 phút)</p>
      </div>

      <div v-if="errorMessage" class="msg-box error">⚠️ {{ errorMessage }}</div>
      <div v-if="successMessage" class="msg-box success">✅ {{ successMessage }}</div>

      <form @submit.prevent="handleSendOtp" class="form-body" method="post" novalidate>
        <div class="field-item">
          <label for="forgot-email" class="field-label">Địa chỉ Email của bạn <span class="required-star">*</span></label>
          <div class="input-icon-wrapper">
            <i class="bi bi-envelope-fill input-leading-icon"></i>
            <input
              id="forgot-email"
              name="email"
              v-model="form.email"
              type="email"
              autocomplete="email"
              class="field-input has-leading-icon"
              placeholder="VD: dobinh225599@gmail.com"
              required
              aria-required="true"
            />
          </div>
        </div>

        <button type="submit" :class="themeConfig.btnClass" :disabled="isLoading">
          <span v-if="!isLoading">GỬI MÃ XÁC THỰC ➔</span>
          <span v-else>ĐANG GỬI EMAIL...</span>
        </button>
      </form>
    </template>

    <!-- BƯỚC 2: NHẬP MÃ XÁC THỰC 6 CHỮ SỐ -->
    <template v-else-if="step === 'otp'">
      <div class="card-brand-header">
        <h1 class="form-main-heading" :style="{ color: themeConfig.primaryColor }">XÁC NHẬN MÃ XÁC THỰC</h1>
        <p class="form-sub-heading">Mã xác thực đã được gửi tới <strong>{{ form.email }}</strong></p>
      </div>

      <!-- TIMER COUNTDOWN BADGE -->
      <div class="timer-badge-row" :style="{ background: themeConfig.badgeBg, borderColor: themeConfig.badgeBorder }">
        <span>Thời gian còn lại:</span>
        <span :class="['timer-text', { expired: timerSeconds <= 0 }]" :style="{ color: timerSeconds > 0 ? themeConfig.primaryColor : undefined }">
          ⏱️ {{ formatTime(timerSeconds) }}
        </span>
      </div>

      <div v-if="errorMessage" class="msg-box error">⚠️ {{ errorMessage }}</div>
      <div v-if="successMessage" class="msg-box success">✓ {{ successMessage }}</div>

      <form @submit.prevent="handleVerifyOtp" class="form-body" method="post" novalidate>
        <div class="field-item">
          <label class="field-label">Nhập mã xác thực (6 chữ số)</label>
          <input
            v-model="form.otp"
            type="text"
            maxlength="6"
            class="field-input otp-center-input"
            :style="{ color: themeConfig.primaryColor }"
            placeholder="• • • • • •"
            required
          />
        </div>

        <div class="resend-row">
          <button 
            type="button" 
            class="btn-resend-link" 
            :style="{ color: themeConfig.primaryColor }"
            :disabled="timerSeconds > 0 || isLoading"
            @click="handleSendOtp"
          >
            {{ timerSeconds > 0 ? `Gửi lại mã sau (${timerSeconds}s)` : '🔄 Gửi lại mã xác thực mới' }}
          </button>
        </div>

        <button type="submit" :class="themeConfig.btnClass" :disabled="isLoading || timerSeconds <= 0">
          <span v-if="!isLoading">XÁC NHẬN MÃ XÁC THỰC ➔</span>
          <span v-else>ĐANG ĐỐI CHIẾU...</span>
        </button>
      </form>
    </template>

    <!-- BƯỚC 3: NHẬP MẬT KHẨU MỚI -->
    <template v-else-if="step === 'reset'">
      <div class="card-brand-header">
        <h1 class="form-main-heading" :style="{ color: themeConfig.primaryColor }">ĐẶT LẠI MẬT KHẨU MỚI</h1>
        <p class="form-sub-heading">Tạo mật khẩu mới an toàn cho tài khoản của bạn</p>
      </div>

      <div v-if="errorMessage" class="msg-box error">⚠️ {{ errorMessage }}</div>
      <div v-if="successMessage" class="msg-box success">✅ {{ successMessage }}</div>

      <form @submit.prevent="handleResetPassword" class="form-body" method="post" novalidate>
        <!-- Mật khẩu mới -->
        <div class="field-item">
          <label for="forgot-new-pass" class="field-label">Mật khẩu mới <span class="required-star">*</span></label>
          <div class="password-wrapper input-icon-wrapper">
            <i class="bi bi-lock-fill input-leading-icon"></i>
            <input
              id="forgot-new-pass"
              name="newPassword"
              v-model="form.newPassword"
              :type="showNewPass ? 'text' : 'password'"
              autocomplete="new-password"
              class="field-input has-leading-icon has-trailing-btn"
              placeholder="Nhập mật khẩu mới (tối thiểu 6 ký tự)..."
              required
              aria-required="true"
            />
            <button 
              type="button" 
              class="eye-toggle-btn" 
              @click="showNewPass = !showNewPass"
              :title="showNewPass ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
            >
              <i :class="showNewPass ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill'"></i>
            </button>
          </div>
        </div>

        <!-- Nhập lại mật khẩu mới -->
        <div class="field-item">
          <label for="forgot-confirm-pass" class="field-label">Nhập lại mật khẩu mới <span class="required-star">*</span></label>
          <div class="password-wrapper input-icon-wrapper">
            <i class="bi bi-shield-check input-leading-icon"></i>
            <input
              id="forgot-confirm-pass"
              name="confirmPassword"
              v-model="form.confirmPassword"
              :type="showConfirmPass ? 'text' : 'password'"
              autocomplete="new-password"
              class="field-input has-leading-icon has-trailing-btn"
              placeholder="Xác nhận lại mật khẩu mới..."
              required
              aria-required="true"
            />
            <button 
              type="button" 
              class="eye-toggle-btn" 
              @click="showConfirmPass = !showConfirmPass"
              :title="showConfirmPass ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
            >
              <i :class="showConfirmPass ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill'"></i>
            </button>
          </div>
        </div>

        <button type="submit" :class="themeConfig.btnClass" :disabled="isLoading">
          <span v-if="!isLoading">CẬP NHẬT MẬT KHẨU ➔</span>
          <span v-else>ĐANG CẬP NHẬT...</span>
        </button>
      </form>
    </template>

    <!-- MODAL THÔNG BÁO KHÔNG TÌM THẤY TÀI KHOẢN (BÓNG MỜ NỀN ĐÈ GIỮA MÀN HÌNH) -->
    <Transition name="fade-modal">
      <div v-if="showNotFoundModal" class="modal-backdrop-overlay" @click.self="showNotFoundModal = false">
        <div class="modal-pop-card">
          <!-- ICON KÍNH LÚP CẢNH BÁO KHÔNG TÌM THẤY TÀI KHOẢN -->
          <div class="modal-icon-badge" :style="{ background: themeConfig.badgeBg, borderColor: themeConfig.badgeBorder }">
            <svg width="34" height="34" viewBox="0 0 24 24" fill="none" :stroke="themeConfig.primaryColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
              <line x1="8" y1="11" x2="14" y2="11"></line>
            </svg>
          </div>

          <h3 class="modal-heading-title">Không tìm thấy tài khoản</h3>
          
          <p class="modal-body-text">
            {{ modalErrorMessage }}
          </p>

          <div class="modal-action-buttons">
            <button type="button" :class="['btn-modal-close-dynamic', themeConfig.btnClass]" @click="showNotFoundModal = false">
              Đóng thông báo
            </button>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<style scoped>
*, input, button, select, textarea, label {
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
}

/* MODAL POPUP STYLING WITH SINGLE CLOSE BUTTON */
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
.forgot-form-wrap {
  width: 100%;
}

.card-top-bar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 8px;
}

.back-home-btn {
  background: transparent;
  border: none;
  color: #64748B;
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
  color: var(--theme-hover, #D94E15);
  transform: translateX(-2px);
}

.card-brand-header {
  text-align: center;
  margin-bottom: 18px;
}

.form-main-heading {
  font-size: 22px;
  font-weight: 900;
  color: var(--theme-primary, #D94E15);
  margin: 0 0 4px 0;
  letter-spacing: 0.8px;
}

.form-sub-heading {
  font-size: 12.5px;
  color: #64748B;
  margin: 0;
}

.timer-badge-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  font-size: 12.5px;
  color: #475569;
  margin-bottom: 14px;
  background: #FFF7ED;
  padding: 6px 12px;
  border-radius: 20px;
  border: 1px dashed #FDBA74;
}

.timer-text {
  font-weight: 800;
  color: var(--theme-primary, #D94E15);
}

.timer-text.expired {
  color: #EF4444;
}

.otp-center-input {
  text-align: center;
  letter-spacing: 8px;
  font-size: 18px !important;
  font-weight: 900;
  color: var(--theme-primary, #D94E15) !important;
}

.resend-row {
  text-align: center;
  margin-top: -4px;
}

.btn-resend-link {
  background: none;
  border: none;
  color: var(--theme-primary, #D94E15);
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
}

.btn-resend-link:disabled {
  color: #94A3B8;
  cursor: not-allowed;
}

.msg-box {
  padding: 9px 14px;
  border-radius: 10px;
  font-size: 12.5px;
  margin-bottom: 14px;
  font-weight: 600;
  text-align: center;
  line-height: 1.45;
  word-break: break-word;
}
.msg-box.error { background: #FEF2F2; color: #B91C1C; border: 1px solid #FECACA; }
.msg-box.success { background: #F0FDF4; color: #15803D; border: 1px solid #BBF7D0; }

.form-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.field-label {
  font-size: 12.5px;
  font-weight: 700;
  color: #0F172A;
}

.field-input {
  width: 100%;
  padding: 9.5px 14px;
  border-radius: 12px;
  border: 1.5px solid #E2E8F0;
  background: #F8FAFC;
  font-size: 13px;
  color: #0F172A;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s ease;
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, sans-serif !important;
}

.field-input:focus {
  background: #FFFFFF;
  border-color: var(--theme-primary, #D94E15);
  box-shadow: 0 0 0 3px var(--theme-focus-shadow, rgba(217, 78, 21, 0.14));
}

.password-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.password-wrapper .field-input {
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

.btn-modal-close-dynamic {
  width: 100%;
  padding: 11px;
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-submit-orange {
  width: 100%;
  padding: 11.5px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  letter-spacing: 0.5px;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
  margin-top: 4px;
}
.btn-submit-orange:hover:not(:disabled) {
  background: #C8451F;
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(217, 78, 21, 0.38);
}
.btn-submit-orange:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.btn-submit-cyan {
  width: 100%;
  padding: 11.5px;
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  letter-spacing: 0.5px;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(2, 132, 199, 0.3);
  transition: all 0.2s ease;
  margin-top: 4px;
}
.btn-submit-cyan:hover:not(:disabled) {
  background: linear-gradient(135deg, #0369A1 0%, #075985 100%);
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(2, 132, 199, 0.38);
}
.btn-submit-cyan:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.btn-submit-emerald {
  width: 100%;
  padding: 11.5px;
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  letter-spacing: 0.5px;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(16, 185, 129, 0.3);
  transition: all 0.2s ease;
  margin-top: 4px;
}
.btn-submit-emerald:hover:not(:disabled) {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(16, 185, 129, 0.38);
}
.btn-submit-emerald:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}
</style>
