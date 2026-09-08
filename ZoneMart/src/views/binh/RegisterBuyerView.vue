<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG KÝ NGƯỜI MUA (REGISTER BUYER) - Phụ trách: Bình
 * ================================================================
 */
import { ref, onUnmounted } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const step = ref<"form" | "otp">("form");
const phoneOrEmail = ref("");
const password = ref("");
const confirmPassword = ref("");
const otpCode = ref("");

const countdown = ref(120);
let timer: any = null;

const startCountdown = () => {
  countdown.value = 120;
  if (timer) clearInterval(timer);
  timer = setInterval(() => {
    if (countdown.value > 0) {
      countdown.value--;
    } else {
      clearInterval(timer);
    }
  }, 1000);
};

onUnmounted(() => {
  if (timer) clearInterval(timer);
});

const handleSendOTP = () => {
  if (!phoneOrEmail.value || !password.value) {
    alert("Vui lòng điền đầy đủ thông tin!");
    return;
  }
  if (password.value !== confirmPassword.value) {
    alert("Mật khẩu xác nhận không khớp!");
    return;
  }

  step.value = "otp";
  startCountdown();
  alert(`Mã OTP đã được gửi tới ${phoneOrEmail.value}. Mã có thời hạn 2 phút theo Luồng 1!`);
};

const handleResendOTP = () => {
  startCountdown();
  alert("Đã cấp lại mã OTP mới!");
};

const handleVerifyOTP = () => {
  if (countdown.value <= 0) {
    alert("Mã OTP đã quá hạn! Vui lòng bấm 'Cấp lại mã mới'.");
    return;
  }
  if (!otpCode.value || otpCode.value.length < 4) {
    alert("Vui lòng nhập mã OTP hợp lệ!");
    return;
  }

  alert("🎉 Đăng ký tài khoản thành công! Chào mừng bạn gia nhập ZoneMart.");
  router.push("/profile");
};
</script>

<template>
  <div class="auth-container">
    <div class="auth-card">
      <div class="auth-header">
        <img src="/logo.png" alt="ZoneMart" class="logo-auth" />
        <h2>Tạo Tài Khoản Mua Sắm</h2>
        <p>Đăng ký nhận ưu đãi giao hàng hỏa tốc trong bán kính 10km.</p>
      </div>

      <form v-if="step === 'form'" @submit.prevent="handleSendOTP">
        <div class="form-group">
          <label>Số điện thoại hoặc Email:</label>
          <input v-model="phoneOrEmail" type="text" placeholder="VD: 0988123456" class="input-field" required />
        </div>

        <div class="form-group">
          <label>Mật khẩu:</label>
          <input v-model="password" type="password" placeholder="Tối thiểu 6 ký tự..." class="input-field" required />
        </div>

        <div class="form-group">
          <label>Xác nhận mật khẩu:</label>
          <input v-model="confirmPassword" type="password" placeholder="Nhập lại mật khẩu..." class="input-field" required />
        </div>

        <button type="submit" class="btn btn-primary btn-block">
          TIẾP TỤC: NHẬN MÃ OTP (2 PHÚT) ➜
        </button>
      </form>

      <div v-else class="otp-step">
        <div class="otp-info">
          <p>Mã xác thực đã gửi tới: <strong>{{ phoneOrEmail }}</strong></p>
          <div class="timer-badge" :class="{ expired: countdown === 0 }">
            ⏰ Thời hạn còn lại: <strong>{{ Math.floor(countdown / 60) }}:{{ (countdown % 60).toString().padStart(2, '0') }}</strong>
          </div>
        </div>

        <div class="form-group">
          <label>Nhập mã OTP:</label>
          <input v-model="otpCode" type="text" placeholder="------" maxlength="6" class="input-field otp-input" />
        </div>

        <button class="btn btn-primary btn-block" :disabled="countdown === 0" @click="handleVerifyOTP">
          XÁC THỰC & ĐĂNG KÝ
        </button>

        <div class="resend-box">
          <button v-if="countdown === 0" class="btn-resend" @click="handleResendOTP">
            🔄 Mã hết hạn: Cấp lại mã mới
          </button>
          <button class="btn-back" @click="step = 'form'">⬅ Sửa lại SĐT/Email</button>
        </div>
      </div>

      <div class="footer-link">
        Đã có tài khoản? <router-link to="/login">Đăng nhập</router-link>
      </div>
    </div>
  </div>
</template>

<style scoped>
.auth-container { max-width: 440px; margin: 50px auto 70px auto; padding: 0 20px; }
.auth-card { background: #fff; border-radius: 20px; border: 1px solid #e2e8f0; padding: 36px 30px; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.05); }
.auth-header { text-align: center; margin-bottom: 24px; }
.logo-auth { height: 60px; object-fit: contain; margin-bottom: 10px; }
.auth-header h2 { margin: 0 0 6px 0; color: #0f172a; font-size: 22px; }
.auth-header p { margin: 0; color: #64748b; font-size: 13px; }

.form-group { margin-bottom: 16px; }
.form-group label { display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 6px; }
.input-field { width: 100%; padding: 12px 14px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 14px; box-sizing: border-box; }
.otp-input { text-align: center; font-size: 24px; letter-spacing: 6px; font-weight: 800; }

.otp-info { text-align: center; margin-bottom: 20px; font-size: 13px; color: #475569; }
.timer-badge { display: inline-block; background: #eff6ff; color: #1e40af; padding: 6px 14px; border-radius: 20px; margin-top: 8px; border: 1px solid #bfdbfe; font-size: 14px; }
.timer-badge.expired { background: #fee2e2; color: #dc2626; border-color: #fca5a5; }

.resend-box { margin-top: 16px; text-align: center; display: flex; flex-direction: column; gap: 8px; }
.btn-resend { background: none; border: 1px solid #f97316; color: #ea580c; padding: 8px; border-radius: 6px; font-weight: 600; cursor: pointer; }
.btn-back { background: none; border: none; color: #64748b; cursor: pointer; font-size: 13px; }

.btn { border: none; cursor: pointer; padding: 13px; border-radius: 8px; font-weight: 700; font-size: 14px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-primary:disabled { background: #cbd5e1; cursor: not-allowed; }
.btn-block { width: 100%; }

.footer-link { text-align: center; margin-top: 20px; font-size: 13px; color: #64748b; }
.footer-link a { color: #2563eb; font-weight: 600; text-decoration: none; }
</style>
