<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG NHẬP (LOGIN VIEW) - Phụ trách: Bình
 * Hỗ trợ 3 Vai Trò Tách Biệt:
 * 1. Khách Hàng (Customer Login) -> ?role=customer / mặc định
 * 2. Gian Hàng (Seller Login) -> ?role=seller
 * 3. Tài Xế Shipper (Driver Login) -> ?role=shipper
 * ================================================================
 */
import { ref, reactive, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import ForgotPasswordForm from "./ForgotPasswordForm.vue";

const router = useRouter();
const route = useRoute();

// Chế độ xem: false = Đăng nhập, true = Quên mật khẩu
const isForgotPasswordMode = ref(false);

// Nhận diện vai trò từ Query Param (?role=shipper | ?role=seller | ?role=customer)
const activeRole = computed(() => {
  const r = (route.query.role as string || "").toLowerCase();
  if (r === "shipper" || r === "driver") return "shipper";
  if (r === "seller" || r === "shop") return "seller";
  return "customer";
});

// Cấu hình linh hoạt giao diện & màu sắc theo vai trò
const config = computed(() => {
  if (activeRole.value === "shipper") {
    return {
      roleKey: "shipper",
      title: "ĐĂNG NHẬP TÀI XẾ DRIVER",
      titleColor: "#0284C7",
      subtitle: "Nhập thông tin tài khoản tài xế để bắt đầu bật app & chạy đơn ZoneMart",
      illustrationImg: "/images/anhxoanendkyshipper_clean.png",
      illustrationAlt: "ZoneMart Driver Scooter Illustration",
      bgStyle: "linear-gradient(180deg, #F0F9FF 0%, #E0F2FE 40%, #F8FAFC 100%)",
      btnClass: "btn-submit-cyan",
      backTarget: "/register-shipper",
      backLabel: "Quay lại trang ZoneMart Driver",
      registerText: "Đăng ký Tài khoản Tài xế",
      registerLink: "/register-shipper#shipper-register-card"
    };
  } else if (activeRole.value === "seller") {
    return {
      roleKey: "seller",
      title: "ĐĂNG NHẬP GIAN HÀNG SELLER",
      titleColor: "#10B981",
      subtitle: "Nhập thông tin tài khoản gian hàng để quản lý cửa hàng ZoneMart",
      illustrationImg: "/images/anhxoanendkybanhang_clean.png",
      illustrationAlt: "ZoneMart Seller Illustration",
      bgStyle: "radial-gradient(circle at 40% 30%, #F0FDF4 0%, #DCFCE7 60%, #F0FDF4 100%)",
      btnClass: "btn-submit-emerald",
      backTarget: "/register-seller",
      backLabel: "Quay lại trang ZoneMart Seller",
      registerText: "Đăng ký Gian hàng Seller",
      registerLink: "/register-seller"
    };
  } else {
    return {
      roleKey: "customer",
      title: "ĐĂNG NHẬP KHÁCH HÀNG",
      titleColor: "#D94E15",
      subtitle: "Nhập thông tin tài khoản của bạn để truy cập ZoneMart",
      illustrationImg: "/images/anhxoanen_clean.png",
      illustrationAlt: "ZoneMart Giao Hàng Hỏa Tốc",
      bgStyle: "radial-gradient(circle at 40% 30%, #FFFDF9 0%, #FAF5EF 60%, #F3ECE2 100%)",
      btnClass: "btn-submit-orange",
      backTarget: "/",
      backLabel: "Quay lại Trang Chủ ZoneMart",
      registerText: "Tạo tài khoản Khách Hàng",
      registerLink: "/register"
    };
  }
});

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
const modalErrorType = ref<'ACCOUNT_NOT_FOUND' | 'INCORRECT_PASSWORD'>(
  'ACCOUNT_NOT_FOUND',
);

const togglePassword = () => {
  showPassword.value = !showPassword.value;
};

const handleBackHome = () => {
  router.push(config.value.backTarget);
};

const handleRegisterClick = (e: MouseEvent) => {
  if (activeRole.value === "shipper") {
    e.preventDefault();
    router.push({ path: "/register-shipper", hash: "#shipper-register-card", query: { register: "true" } }).then(() => {
      setTimeout(() => {
        const el = document.getElementById("shipper-register-card");
        if (el) {
          el.scrollIntoView({ behavior: "smooth" });
        }
      }, 200);
    });
  } else if (activeRole.value === "seller") {
    e.preventDefault();
    router.push({ path: "/register-seller", hash: "#seller-register-card", query: { register: "true" } }).then(() => {
      setTimeout(() => {
        const el = document.getElementById("seller-register-card");
        if (el) {
          el.scrollIntoView({ behavior: "smooth" });
        }
      }, 200);
    });
  }
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
    const res = await fetch("http://localhost:5000/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        account: form.account.trim(),
        password: form.password,
        role: activeRole.value
      })
    }).catch(() => null);

    if (res) {
      const data = await res.json();
      if (res.ok && data.success) {
        localStorage.setItem("isLoggedIn", "true");
        if (data.user) {
          localStorage.setItem("currentUser", JSON.stringify(data.user));
          localStorage.setItem("userRole", data.user.role || activeRole.value);
        }

        successMessage.value = data.message || "Đăng nhập thành công! Đang chuyển hướng...";
        const detectedRole = data.user?.role || activeRole.value;
        setTimeout(() => {
          if (activeRole.value === "shipper" || detectedRole === "shipper") {
            router.push("/shipper");
          } else if (activeRole.value === "seller" || detectedRole === "seller") {
            router.push("/seller");
          } else if (detectedRole === "admin") {
            router.push("/admin");
          } else {
            router.push("/");
          }
        }, 1100);
      } else {
        modalErrorMessage.value = data.message || "Không tìm thấy tài khoản";
        if (data.errorType === "INCORRECT_PASSWORD" || (data.message && data.message.toLowerCase().includes("mật khẩu"))) {
          modalErrorType.value = "INCORRECT_PASSWORD";
        } else {
          modalErrorType.value = "ACCOUNT_NOT_FOUND";
        }
        showNotFoundModal.value = true;
      }
    } else {
      // Offline / Fallback local demo login when backend server is not running
      localStorage.setItem("isLoggedIn", "true");
      const localRole = activeRole.value;
      const demoUser = {
        id: `demo_${Date.now()}`,
        phoneEmail: form.account.trim(),
        fullName: form.account.trim().split('@')[0],
        role: localRole
      };
      localStorage.setItem("currentUser", JSON.stringify(demoUser));
      localStorage.setItem("userRole", localRole);
      successMessage.value = "Đăng nhập thành công (Chế độ Ngoại tuyến)! Đang chuyển hướng...";
      setTimeout(() => {
        if (localRole === "shipper") {
          router.push("/shipper");
        } else if (localRole === "seller") {
          router.push("/seller");
        } else {
          router.push("/");
        }
      }, 1100);
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
  <div class="login-page-container" :style="{ background: config.bgStyle }">
    <!-- KHUNG TỔNG THỂ 2 CỘT: ẢNH BÊN TRÁI + BẢNG ĐĂNG NHẬP BÊN PHẢI -->
    <div class="login-hero-layout">
      
      <!-- CỘT TRÁI: ẢNH MINH HỌA (XE SHIPPER DÀNH CHO TÀI XẾ, XE THỰC PHẨM DÀNH CHO KHÁCH HÀNG/SELLER) -->
      <div class="login-illustration-side">
        <img
          :src="config.illustrationImg"
          :alt="config.illustrationAlt"
          class="illustration-img"
          :class="{ 'driver-img-style': config.roleKey === 'shipper' }"
        />
      </div>

      <!-- CỘT PHẢI: BẢNG ĐĂNG NHẬP FLOATING CARD -->
      <div class="login-card-floating" :class="`role-${config.roleKey}`">
        
        <!-- TRẠNG THÁI 1: FORM ĐĂNG NHẬP -->
        <template v-if="!isForgotPasswordMode">
          <!-- THANH ĐIỀU HƯỚNG QUAY LẠI TRANG CHỦ RIÊNG -->
          <div class="card-top-bar">
            <button type="button" class="back-home-btn" @click="handleBackHome" :title="config.backLabel">
              ← Quay lại
            </button>
          </div>

          <!-- TIÊU ĐỀ ĐĂNG NHẬP -->
          <div class="card-brand-header">
            <h1 class="form-main-heading" :style="{ color: config.titleColor }">{{ config.title }}</h1>
            <p class="form-sub-heading">{{ config.subtitle }}</p>
          </div>

          <!-- THÔNG BÁO THÀNH CÔNG -->
          <div v-if="successMessage" class="msg-box success">
            ✅ {{ successMessage }}
          </div>

          <!-- FORM NHẬP THÔNG TIN (CHUẨN SEO SEMANTIC & ACCESSIBILITY) -->
          <form @submit.prevent="handleLogin" class="form-body" method="post" action="/api/auth/login" novalidate itemscope itemtype="https://schema.org/WebPage">
            <!-- Số điện thoại / Email -->
            <div class="field-item">
              <div class="label-row-between">
                <label for="login-account" class="field-label">Tài khoản đăng nhập <span class="required-star">*</span></label>
                <span class="zalo-hint-tag"><i class="bi bi-shield-check"></i> SĐT liên kết / Email</span>
              </div>
              <div class="input-icon-wrapper">
                <i class="bi bi-person-fill input-leading-icon"></i>
                <input
                  id="login-account"
                  name="username"
                  v-model="form.account"
                  type="text"
                  autocomplete="username"
                  class="field-input has-leading-icon"
                  placeholder="Nhập SĐT hoặc Email của bạn"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <!-- Mật khẩu -->
            <div class="field-item">
              <div class="label-row-between">
                <label class="field-label">Mật khẩu</label>
                <a href="#" @click.prevent="isForgotPasswordMode = true" class="forgot-pass-link" :style="{ color: config.titleColor }">Quên mật khẩu?</a>
              </div>
              <div class="password-wrapper input-icon-wrapper">
                <i class="bi bi-lock-fill input-leading-icon"></i>
                <input
                  id="login-password"
                  name="password"
                  v-model="form.password"
                  :type="showPassword ? 'text' : 'password'"
                  autocomplete="current-password"
                  class="field-input has-leading-icon has-trailing-btn"
                  placeholder="Nhập mật khẩu..."
                  required
                  aria-required="true"
                />
                <button
                  type="button"
                  class="eye-toggle-btn"
                  @click="togglePassword"
                  :title="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                  :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                >
                  <i :class="showPassword ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill'"></i>
                </button>
              </div>
            </div>

            <!-- Checkbox Ghi nhớ -->
            <div class="form-options-row">
              <label class="custom-checkbox-label">
                <input type="checkbox" v-model="form.rememberMe" class="custom-checkbox" :style="{ accentColor: config.titleColor }" />
                <span>Ghi nhớ đăng nhập trên thiết bị này</span>
              </label>
            </div>

            <!-- Nút Đăng Nhập -->
            <button type="submit" :class="config.btnClass" :disabled="isLoading">
              <span v-if="!isLoading">ĐĂNG NHẬP NGAY ➔</span>
              <span v-else>ĐANG XỬ LÝ...</span>
            </button>

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
                <router-link
                  :to="config.registerLink"
                  class="link-role-bold"
                  :style="{ color: config.titleColor }"
                  @click="handleRegisterClick"
                >
                  {{ config.registerText }}
                </router-link>
              </div>
            </div>
          </form>
        </template>

        <!-- TRẠNG THÁI 2: FORM QUÊN MẬT KHẨU -->
        <template v-else>
          <ForgotPasswordForm
            :initial-email="form.account"
            :role="activeRole"
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

    <!-- MODAL THÔNG BÁO TÀI KHOẢN / MẬT KHẨU KHÔNG ĐÚNG -->
    <Transition name="fade-modal">
      <div
        v-if="showNotFoundModal"
        class="modal-backdrop-overlay"
        @click.self="showNotFoundModal = false"
      >
        <div class="modal-pop-card">
          <div v-if="modalErrorType === 'INCORRECT_PASSWORD'" class="modal-icon-badge error-x-badge">
            <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="#EF4444" stroke-width="2.5">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="15" y1="9" x2="9" y2="15"></line>
              <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
          </div>
          <div v-else class="modal-icon-badge">
            <svg width="34" height="34" viewBox="0 0 24 24" fill="none" :stroke="config.titleColor" stroke-width="2.2">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
              <line x1="8" y1="11" x2="14" y2="11"></line>
            </svg>
          </div>
          <h3 class="modal-heading-title">
            {{
              modalErrorType === 'INCORRECT_PASSWORD'
                ? 'Mật khẩu không đúng'
                : 'Không tìm thấy tài khoản'
            }}
          </h3>
          <p class="modal-body-text">{{ modalErrorMessage }}</p>
          <div class="modal-action-buttons">
            <button
              type="button"
              class="btn-modal-close"
              @click="showNotFoundModal = false"
            >
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

.login-page-container {
  width: 100vw;
  height: 100vh;
  max-height: 100vh;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  padding: 16px 40px 16px 24px;
  box-sizing: border-box;
  transition: background 0.3s ease;
}

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
  transition: all 0.3s ease;
}

.illustration-img.driver-img-style {
  max-width: 660px;
  filter: drop-shadow(0 16px 32px rgba(2, 132, 199, 0.18));
}

.login-card-floating {
  width: 100%;
  max-width: 430px;
  background: #ffffff;
  border-radius: 24px;
  padding: 24px 28px 22px 28px;
  box-shadow:
    0 20px 45px -10px rgba(15, 23, 42, 0.12),
    0 8px 20px -5px rgba(2, 132, 199, 0.08),
    0 0 0 1px rgba(186, 230, 253, 0.8);
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
    transform: translateY(20px) scale(0.98);
  }
  100% {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

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
  color: #0284C7;
  transform: translateX(-2px);
}

.card-brand-header {
  text-align: center;
  margin-bottom: 18px;
}

.form-main-heading {
  font-size: 22px;
  font-weight: 900;
  margin: 0 0 4px 0;
  letter-spacing: 0.5px;
}

.form-sub-heading {
  font-size: 12.5px;
  color: #64748b;
  margin: 0;
  line-height: 1.4;
}

.msg-box {
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 12px;
  margin-bottom: 14px;
  font-weight: 600;
}
.msg-box.error {
  background: #fef2f2;
  color: #b91c1c;
  border: 1px solid #fecaca;
}
.msg-box.success {
  background: #f0fdf4;
  color: #15803d;
  border: 1px solid #bbf7d0;
}
.msg-box.warning {
  background: #fffbeb;
  color: #b45309;
  border: 1px solid #fde68a;
}

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
  font-weight: 700;
  text-decoration: none;
}
.forgot-pass-link:hover {
  text-decoration: underline;
}

.field-input {
  width: 100%;
  padding: 10px 14px;
  border-radius: 12px;
  border: 1.5px solid #e2e8f0;
  background: #f8fafc;
  font-size: 13.5px;
  color: #0f172a;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s ease;
}

.role-seller .field-input:focus {
  background: #FFFFFF;
  border-color: #10B981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.14);
}

.role-shipper .field-input:focus {
  background: #FFFFFF;
  border-color: #0284C7;
  box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.14);
}

.role-customer .field-input:focus {
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
  padding-left: 12px;
  padding-right: 38px;
}

.eye-toggle-btn {
  position: absolute;
  right: 14px;
  background: none;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  transition: all 0.2s ease;
  z-index: 2;
}

.eye-toggle-btn:hover {
  color: #d94e15;
  transform: scale(1.1);
}

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
  width: 15px;
  height: 15px;
  cursor: pointer;
}

/* BUTTON VARIANTS FOR 3 ROLES */
.btn-submit-orange {
  width: 100%;
  padding: 12px;
  background: linear-gradient(135deg, #d94e15, #ea580c);
  color: #ffffff;
  border: none;
  border-radius: 30px;
  font-size: 14px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 6px 18px rgba(217, 78, 21, 0.3);
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  margin-top: 2px;
}
.btn-submit-orange:hover:not(:disabled) {
  background: linear-gradient(135deg, #c2410c, #d94e15);
  transform: translateY(-2px);
  box-shadow: 0 10px 24px rgba(217, 78, 21, 0.4);
}

.btn-submit-orange:active:not(:disabled) {
  transform: translateY(0) scale(0.98);
  box-shadow: 0 4px 12px rgba(217, 78, 21, 0.25);
}

.btn-submit-cyan {
  width: 100%;
  padding: 11px;
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(2, 132, 199, 0.3);
  transition: all 0.2s ease;
  margin-top: 2px;
}
.btn-submit-cyan:hover:not(:disabled) {
  background: linear-gradient(135deg, #0369A1 0%, #075985 100%);
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(2, 132, 199, 0.4);
}

.btn-submit-emerald {
  width: 100%;
  padding: 11px;
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(16, 185, 129, 0.3);
  transition: all 0.2s ease;
  margin-top: 2px;
}
.btn-submit-emerald:hover:not(:disabled) {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(16, 185, 129, 0.4);
}

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

.register-cta-box {
  display: flex;
  flex-direction: column;
  gap: 5px;
  text-align: center;
  font-size: 12px;
  color: #64748b;
}

.link-role-bold {
  font-weight: 800;
  text-decoration: none;
}
.link-role-bold:hover {
  text-decoration: underline;
}

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
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.3);
  display: flex;
  flex-direction: column;
  align-items: center;
}

.modal-icon-badge {
  width: 60px;
  height: 60px;
  background: #F0F9FF;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
  border: 2px solid #BAE6FD;
}

.modal-icon-badge.error-x-badge {
  background: #FEF2F2;
  border: 2px solid #FEE2E2;
}

.modal-heading-title {
  font-size: 19px;
  font-weight: 900;
  color: #0f172a;
  margin: 0 0 8px 0;
}

.modal-body-text {
  font-size: 13px;
  color: #475569;
  margin: 0 0 20px 0;
  line-height: 1.5;
}

.modal-action-buttons {
  width: 100%;
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
}

.btn-modal-close:hover {
  color: #0f172a;
}

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
