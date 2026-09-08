<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG NHẬP (LOGIN) - Phụ trách: Bình
 * ================================================================
 */
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const phoneOrEmail = ref("");
const password = ref("");
const rememberMe = ref(false);

const handleLogin = () => {
  if (!phoneOrEmail.value || !password.value) {
    alert("Vui lòng điền đầy đủ thông tin đăng nhập!");
    return;
  }
  alert("Đăng nhập thành công! Chào mừng bạn quay trở lại ZoneMart.");
  router.push("/profile");
};
</script>

<template>
  <div class="auth-container">
    <div class="auth-card">
      <div class="auth-header">
        <img src="/logo.png" alt="ZoneMart" class="logo-auth" />
        <h2>Chào Mừng Trở Lại!</h2>
        <p>Đăng nhập để mua sắm hoặc quản lý cửa hàng / chuyến giao hàng.</p>
      </div>

      <form @submit.prevent="handleLogin" class="auth-form">
        <div class="form-group">
          <label>Số điện thoại hoặc Email:</label>
          <input 
            v-model="phoneOrEmail" 
            type="text" 
            placeholder="VD: 0912345678 hoặc email@zonemart.vn" 
            class="input-field" 
            required 
          />
        </div>

        <div class="form-group">
          <label>Mật khẩu:</label>
          <input 
            v-model="password" 
            type="password" 
            placeholder="Nhập mật khẩu của bạn..." 
            class="input-field" 
            required 
          />
        </div>

        <div class="form-actions">
          <label class="remember-me">
            <input type="checkbox" v-model="rememberMe" />
            <span>Ghi nhớ đăng nhập</span>
          </label>
          <a href="#" class="forgot-link">Quên mật khẩu?</a>
        </div>

        <button type="submit" class="btn btn-primary btn-block">
          ĐĂNG NHẬP
        </button>

        <div class="divider"><span>HOẶC</span></div>

        <div class="register-links">
          <p>Chưa có tài khoản? <router-link to="/register">Đăng ký Người Mua</router-link></p>
          <p>Muốn hợp tác? <router-link to="/register-seller">Đăng ký Cửa Hàng (Seller)</router-link></p>
        </div>
      </form>
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

.form-actions { display: flex; justify-content: space-between; align-items: center; font-size: 13px; margin-bottom: 20px; }
.remember-me { display: flex; align-items: center; gap: 6px; cursor: pointer; color: #475569; }
.forgot-link { color: #2563eb; text-decoration: none; }

.btn { border: none; cursor: pointer; padding: 13px; border-radius: 8px; font-weight: 700; font-size: 14px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-block { width: 100%; }

.divider { text-align: center; margin: 20px 0; position: relative; }
.divider::before { content: ""; position: absolute; top: 50%; left: 0; width: 100%; height: 1px; background: #e2e8f0; }
.divider span { position: relative; background: #fff; padding: 0 10px; font-size: 11px; color: #94a3b8; }

.register-links { font-size: 13px; text-align: center; color: #64748b; }
.register-links a { color: #2563eb; font-weight: 600; text-decoration: none; }
</style>
