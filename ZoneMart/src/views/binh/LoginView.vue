<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG NHẬP (LOGIN VIEW) - Phụ trách: Bình
 * Giao diện Bảng Đăng Nhập Đơn Giản & Tự Động Nhận Diện Vai Trò
 * Tích hợp Bảng Quên Mật Khẩu với Gmail OTP 120s (dobinh225599@gmail.com)
 * ================================================================
 */
import { ref, reactive } from "vue";
import { useRouter } from "vue-router";
import ForgotPasswordForm from "./ForgotPasswordForm.vue";
import { useAuth, DEMO_USERS, type UserRole } from "../../composables/useAuth";

const router = useRouter();
const auth = useAuth();

// Chế độ xem: false = Đăng nhập, true = Quên mật khẩu
const isForgotPasswordMode = ref(false);

// Form State
const form = reactive({
  account: "",
  password: "",
  rememberMe: true
});

const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref("");
const successMessage = ref("");

const showNotFoundModal = ref(false);
const modalErrorMessage = ref("");
const modalErrorType = ref<"ACCOUNT_NOT_FOUND" | "INCORRECT_PASSWORD">("ACCOUNT_NOT_FOUND");

const togglePassword = () => {
  showPassword.value = !showPassword.value;
};

// Đăng nhập nhanh 1 chạm theo vai trò mẫu (thuận tiện cho test giao diện)
const quickLogin = (role: "buyer" | "seller" | "shipper" | "admin") => {
  const demo = DEMO_USERS[role];
  auth.login(demo);
  successMessage.value = `Đăng nhập thành công với vai trò ${auth.roleLabel.value} (${demo.fullName})!`;
  setTimeout(() => {
    if (role === "seller" || role === "admin") {
      router.push("/admin");
    } else if (role === "shipper") {
      router.push("/shipper");
    } else {
      router.push("/");
    }
  }, 600);
};

// Xử lý Đăng Nhập
const handleLogin = async () => {
  errorMessage.value = "";
  successMessage.value = "";

  if (!form.account.trim() || !form.password.trim()) {
    modalErrorMessage.value = "Vui lòng nhập đầy đủ số điện thoại / email và mật khẩu!";
    modalErrorType.value = "ACCOUNT_NOT_FOUND";
    showNotFoundModal.value = true;
    return;
  }

  isLoading.value = true;

  try {
    // 1. Thử kết nối API Backend (C# .NET Core)
    const res = await fetch("http://localhost:5000/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        account: form.account.trim(),
        password: form.password
      })
    }).catch(() => null);

    if (res) {
      const data = await res.json();
      if (res.ok && data.success) {
        successMessage.value = data.message || "Đăng nhập thành công! Đang chuyển hướng...";
        const detectedRole = (data.user?.role as UserRole) || "buyer";

        // Lưu thông tin người dùng vào useAuth state & localStorage
        auth.login({
          id: data.user?.id || `usr_${Date.now()}`,
          fullName: data.user?.fullName || form.account.trim(),
          phoneEmail: data.user?.phoneEmail || form.account.trim(),
          role: detectedRole,
          avatarUrl: data.user?.avatarUrl,
          walletBalance: data.user?.walletBalance,
          storeName: data.user?.storeName
        });

        setTimeout(() => {
          if (detectedRole === "seller" || detectedRole === "admin") {
            router.push("/admin");
          } else if (detectedRole === "shipper") {
            router.push("/shipper");
          } else {
            router.push("/");
          }
        }, 800);
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
      // 2. Fallback ngoại tuyến: Khi chưa bật server Backend, tự động khớp tài khoản demo hoặc tạo phiên buyer
      const acc = form.account.trim().toLowerCase();
      let matchedRole: UserRole = "buyer";
      if (acc.includes("seller") || acc.includes("mai") || acc.includes("shop")) matchedRole = "seller";
      else if (acc.includes("shipper") || acc.includes("nam") || acc.includes("driver")) matchedRole = "shipper";
      else if (acc.includes("admin") || acc.includes("quan tri")) matchedRole = "admin";

      quickLogin(matchedRole);
    }

  } catch (error: any) {
    modalErrorMessage.value = error.message || "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại sau.";
    modalErrorType.value = "ACCOUNT_NOT_FOUND";
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
            <button type="button" class="back-home-btn" @click="router.push('/')" title="Về trang chủ">
              ← Quay lại
            </button>
          </div>

          <!-- TIÊU ĐỀ ĐĂNG NHẬP -->
          <div class="card-brand-header">
            <h1 class="form-main-heading">ĐĂNG NHẬP</h1>
            <p class="form-sub-heading">Nhập thông tin tài khoản của bạn để truy cập ZoneMart</p>
          </div>

          <!-- THÔNG BÁO THÀNH CÔNG -->
          <div v-if="successMessage" class="msg-box success">✅ {{ successMessage }}</div>

          <!-- FORM NHẬP THÔNG TIN -->
          <form @submit.prevent="handleLogin" class="form-body">
            <!-- Số điện thoại / Email -->
            <div class="field-item">
              <label class="field-label">Số điện thoại hoặc Email</label>
              <input
                v-model="form.account"
                type="text"
                class="field-input"
                placeholder="VD: 0912345678 hoặc email@gmail.com"
                required
              />
            </div>

            <!-- Mật khẩu -->
            <div class="field-item">
              <div class="label-row-between">
                <label class="field-label">Mật khẩu</label>
                <a href="#" @click.prevent="isForgotPasswordMode = true" class="forgot-pass-link">Quên mật khẩu?</a>
              </div>
              <div class="password-wrapper">
                <input
                  v-model="form.password"
                  :type="showPassword ? 'text' : 'password'"
                  class="field-input"
                  placeholder="Nhập mật khẩu..."
                  required
                />
                <button type="button" class="eye-toggle-btn" @click="togglePassword" title="Ẩn/Hiện mật khẩu">
                  <svg v-if="!showPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                    <circle cx="12" cy="12" r="3"></circle>
                  </svg>
                  <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2">
                    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                    <line x1="1" y1="1" x2="23" y2="23"></line>
                  </svg>
                </button>
              </div>
            </div>

            <!-- Checkbox Ghi nhớ -->
            <div class="form-options-row">
              <label class="custom-checkbox-label">
                <input type="checkbox" v-model="form.rememberMe" class="custom-checkbox" />
                <span>Ghi nhớ đăng nhập trên thiết bị này</span>
              </label>
            </div>

            <!-- Nút Đăng Nhập -->
            <button type="submit" class="btn-submit-orange" :disabled="isLoading">
              <span v-if="!isLoading">ĐĂNG NHẬP NGAY ➔</span>
              <span v-else>ĐANG XỬ LÝ...</span>
            </button>

            <!-- BỘ CHỌN ĐĂNG NHẬP NHANH 4 VAI TRÒ (TEST / DEMO GIAO DIỆN) -->
            <div class="quick-demo-roles-box">
              <div class="demo-roles-title">
                <i class="bi bi-lightning-charge-fill"></i>
                <span>Hoặc chọn nhanh vai trò để trải nghiệm:</span>
              </div>
              <div class="demo-roles-grid">
                <button type="button" class="demo-role-chip buyer" @click="quickLogin('buyer')" title="Đăng nhập Khách Hàng">
                  <span class="role-icon">👤</span>
                  <span class="role-txt">Buyer</span>
                </button>
                <button type="button" class="demo-role-chip seller" @click="quickLogin('seller')" title="Đăng nhập Chủ Gian Hàng">
                  <span class="role-icon">🌱</span>
                  <span class="role-txt">Seller</span>
                </button>
                <button type="button" class="demo-role-chip shipper" @click="quickLogin('shipper')" title="Đăng nhập Tài Xế">
                  <span class="role-icon">🛵</span>
                  <span class="role-txt">Shipper</span>
                </button>
                <button type="button" class="demo-role-chip admin" @click="quickLogin('admin')" title="Đăng nhập Quản Trị Viên">
                  <span class="role-icon">👑</span>
                  <span class="role-txt">Admin</span>
                </button>
              </div>
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
                <router-link to="/register" class="link-orange-bold">Tạo tài khoản Khách Hàng</router-link>
              </div>
              <div class="cta-sub-line">
                <span>Muốn mở gian hàng? </span>
                <router-link to="/register-seller" class="link-secondary">Đăng ký bán hàng với ZoneMart</router-link>
              </div>
              <div class="cta-sub-line" style="margin-top: 2px;">
                <span>Đăng ký làm shipper? </span>
                <router-link to="/register-shipper" class="link-secondary">Đăng ký đối tác giao hàng</router-link>
              </div>
            </div>
          </form>
        </template>

        <!-- TRẠNG THÁI 2: FORM QUÊN MẬT KHẨU WITH GMAIL OTP -->
        <template v-else>
          <ForgotPasswordForm 
            :initial-email="form.account"
            @back-to-login="isForgotPasswordMode = false"
            @success="isForgotPasswordMode = false; successMessage = 'Đổi mật khẩu thành công! Bạn có thể đăng nhập ngay với mật khẩu mới.';"
          />
        </template>

      </div>

    </div>

    <!-- MODAL THÔNG BÁO TÀI KHOẢN / MẬT KHẨU (BÓNG MỜ NỀN ĐÈ GIỮA MÀN HÌNH) -->
    <Transition name="fade-modal">
      <div v-if="showNotFoundModal" class="modal-backdrop-overlay" @click.self="showNotFoundModal = false">
        <div class="modal-pop-card">
          
          <!-- TH 1: MẬT KHẨU KHÔNG ĐÚNG (ICON DẤU TRÒN CÓ CHỮ X) -->
          <div v-if="modalErrorType === 'INCORRECT_PASSWORD'" class="modal-icon-badge error-x-badge">
            <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="#EF4444" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="15" y1="9" x2="9" y2="15"></line>
              <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
          </div>

          <!-- TH 2: KHÔNG TÌM THẤY TÀI KHOẢN (ICON KÍNH LÚP) -->
          <div v-else class="modal-icon-badge">
            <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
              <line x1="8" y1="11" x2="14" y2="11"></line>
            </svg>
          </div>

          <h3 class="modal-heading-title">
            {{ modalErrorType === 'INCORRECT_PASSWORD' ? 'Mật khẩu không đúng' : 'Không tìm thấy tài khoản' }}
          </h3>
          
          <p class="modal-body-text">
            {{ modalErrorMessage }}
          </p>

          <div class="modal-action-buttons">
            <button type="button" class="btn-modal-close" @click="showNotFoundModal = false">
              Đóng thông báo
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* ÉP NỔI FONT CHỮ CHUẨN KHÔNG BỊ LỖI FONT */
*, input, button, select, textarea, label {
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
}

/* PAGE CONTAINER VỪA KHÍT 1 MÀN HÌNH (NO SCROLLBAR, LÙI ẢNH SÁT LỀ TRÁI) */
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

/* CỘT PHẢI - BẢNG ĐĂNG NHẬP TRÔI NỔI */
.login-card-floating {
  width: 100%;
  max-width: 430px;
  background: #FFFFFF;
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
  color: #D94E15;
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
  color: #D94E15;
  margin: 0 0 4px 0;
  letter-spacing: 0.8px;
}

.form-sub-heading {
  font-size: 12.5px;
  color: #64748B;
  margin: 0;
}

/* MESSAGES */
.msg-box {
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 12px;
  margin-bottom: 14px;
  font-weight: 600;
}
.msg-box.error { background: #FEF2F2; color: #B91C1C; border: 1px solid #FECACA; }
.msg-box.success { background: #F0FDF4; color: #15803D; border: 1px solid #BBF7D0; }

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
  color: #0F172A;
}

.forgot-pass-link {
  font-size: 11.5px;
  color: #D94E15;
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
  accent-color: #D94E15;
  width: 15px;
  height: 15px;
  cursor: pointer;
}

/* SUBMIT BUTTON - WARM ORANGE PILL */
.btn-submit-orange {
  width: 100%;
  padding: 11px;
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
  margin-top: 2px;
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
  gap: 5px;
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

.link-secondary {
  color: #2563EB;
  font-weight: 700;
  text-decoration: none;
}
.link-secondary:hover {
  text-decoration: underline;
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

.btn-modal-register {
  width: 100%;
  padding: 11px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
}

.btn-modal-register:hover {
  background: #C8451F;
  transform: translateY(-1px);
}

.btn-modal-close {
  width: 100%;
  padding: 9px;
  background: transparent;
  color: #64748B;
  border: none;
  font-size: 12.5px;
  font-weight: 700;
  cursor: pointer;
  transition: color 0.2s ease;
}

.btn-modal-close:hover {
  color: #0F172A;
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
</style>
