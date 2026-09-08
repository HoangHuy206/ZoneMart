<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG KÝ CỬA HÀNG (REGISTER SELLER) - Phụ trách: Bình
 * ================================================================
 */
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const form = ref({
  storeName: "",
  address: "",
  cccdNumber: "",
  storeType: "all_day",
  openHours: "07:00-22:00",
  category: "Thực phẩm & Nhu yếu phẩm"
});

const isSubmitted = ref(false);

const handleSubmit = () => {
  if (!form.value.storeName || !form.value.address || !form.value.cccdNumber) {
    alert("Vui lòng điền đầy đủ Tên Shop, Địa chỉ và CCCD!");
    return;
  }
  isSubmitted.value = true;
};
</script>

<template>
  <div class="seller-reg-container">
    <div class="seller-card">
      <div class="reg-header">
        <span class="badge">🏪 Kênh Người Bán ZoneMart</span>
        <h2>Mở Gian Hàng Kinh Doanh</h2>
        <p>Tiếp cận khách hàng trong bán kính 10km quanh cửa hàng của bạn.</p>
      </div>

      <div v-if="isSubmitted" class="success-box">
        <div class="icon">⏳</div>
        <h3>Hồ Sơ Đang Chờ Xét Duyệt</h3>
        <p>
          Hồ sơ mở cửa hàng <strong>{{ form.storeName }}</strong> đã được chuyển tới
          <strong>Manager</strong> để kiểm tra tính hợp lệ của CCCD và địa chỉ theo Luồng 1.
        </p>
        <div class="info-tag">Kết quả sẽ được thông báo qua Email trong vòng 24 giờ.</div>
        <button class="btn btn-primary" @click="router.push('/profile')">Quay Về Trang Cá Nhân</button>
      </div>

      <form v-else @submit.prevent="handleSubmit" class="reg-form">
        <div class="form-group">
          <label>Tên Cửa Hàng / Quán:</label>
          <input v-model="form.storeName" type="text" placeholder="VD: Bách Hóa Sạch ZoneMart Cầu Giấy" class="input-field" required />
        </div>

        <div class="form-group">
          <label>Địa chỉ cửa hàng cụ thể (định vị Map 10km):</label>
          <input v-model="form.address" type="text" placeholder="Số nhà, đường/phố, Quận/Huyện..." class="input-field" required />
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Số CCCD người đại diện:</label>
            <input v-model="form.cccdNumber" type="text" placeholder="12 chữ số CCCD..." class="input-field" required />
          </div>

          <div class="form-group">
            <label>Ngành hàng chính:</label>
            <select v-model="form.category" class="input-field">
              <option value="Thực phẩm & Nhu yếu phẩm">Thực phẩm & Nhu yếu phẩm</option>
              <option value="Trái cây & Đồ uống">Trái cây & Đồ uống</option>
              <option value="Cơm & Thức ăn nhanh">Cơm & Thức ăn nhanh</option>
              <option value="Đồ gia dụng tiện ích">Đồ gia dụng tiện ích</option>
            </select>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Hình thức hoạt động:</label>
            <select v-model="form.storeType" class="input-field">
              <option value="all_day">Mở cả ngày (All day)</option>
              <option value="time_slot">Theo khung giờ (Time slot)</option>
            </select>
          </div>

          <div class="form-group">
            <label>Khung giờ mở cửa:</label>
            <input v-model="form.openHours" type="text" placeholder="VD: 07:00-22:00" class="input-field" />
          </div>
        </div>

        <button type="submit" class="btn btn-primary btn-block">🚀 NỘP HỒ SƠ ĐĂNG KÝ GIAN HÀNG</button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.seller-reg-container { max-width: 680px; margin: 40px auto 70px auto; padding: 0 20px; }
.seller-card { background: #fff; border-radius: 20px; border: 1px solid #e2e8f0; padding: 36px 32px; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.05); }
.reg-header { text-align: center; margin-bottom: 26px; }
.badge { background: #ffedd5; color: #ea580c; font-weight: 700; font-size: 12px; padding: 4px 12px; border-radius: 20px; display: inline-block; margin-bottom: 8px; }
.reg-header h2 { margin: 0 0 6px 0; color: #0f172a; font-size: 24px; }
.reg-header p { margin: 0; color: #64748b; font-size: 14px; }

.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
@media (max-width: 600px) { .form-row { grid-template-columns: 1fr; } }

.form-group { margin-bottom: 16px; }
.form-group label { display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 6px; }
.input-field { width: 100%; padding: 11px 14px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 14px; box-sizing: border-box; }

.btn { border: none; cursor: pointer; padding: 13px; border-radius: 8px; font-weight: 700; font-size: 14px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-block { width: 100%; }

.success-box { text-align: center; padding: 30px 10px; }
.success-box .icon { font-size: 54px; margin-bottom: 12px; }
.success-box h3 { color: #d97706; margin-bottom: 10px; }
.success-box p { color: #475569; font-size: 14px; line-height: 1.6; margin-bottom: 16px; }
.info-tag { background: #f0fdf4; color: #166534; border: 1px solid #bbf7d0; padding: 10px; border-radius: 8px; font-size: 13px; margin-bottom: 24px; }
</style>
