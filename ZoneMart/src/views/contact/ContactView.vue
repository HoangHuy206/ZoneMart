<script setup lang="ts">
/**
 * ================================================================
 * LIÊN HỆ & HỖ TRỢ (CONTACT) - Phụ trách: Huy
 * ================================================================
 */
import { ref } from "vue";

const form = ref({
  fullName: "",
  email: "",
  phone: "",
  topic: "order_issue",
  message: ""
});

const isSubmitted = ref(false);

const handleSubmit = () => {
  if (!form.value.fullName || !form.value.email || !form.value.message) {
    alert("Vui lòng điền đầy đủ thông tin!");
    return;
  }
  isSubmitted.value = true;
};
</script>

<template>
  <div class="contact-container">
    <div class="contact-header">
      <h2>📞 Trung Tâm Hỗ Trợ & Liên Hệ ZoneMart</h2>
      <p>Đội ngũ chăm sóc khách hàng ZoneMart luôn sẵn sàng hỗ trợ bạn 24/7</p>
    </div>

    <div class="contact-grid">
      <div class="info-card">
        <h3>Thông Tin Liên Lạc</h3>
        <p class="desc">Bạn có thắc mắc về đơn hàng, tài khoản Seller/Shipper hoặc muốn khiếu nại?</p>

        <div class="contact-methods">
          <div class="method-item">
            <span class="icon">📍</span>
            <div>
              <strong>Trụ sở ZoneMart:</strong>
              <p>Tòa nhà Công Nghệ, Quận Cầu Giấy, TP. Hà Nội</p>
            </div>
          </div>

          <div class="method-item">
            <span class="icon">☎️</span>
            <div>
              <strong>Hotline Hỗ Trợ 24/7:</strong>
              <p class="highlight">1900 8888 (Miễn phí cước gọi)</p>
            </div>
          </div>

          <div class="method-item">
            <span class="icon">✉️</span>
            <div>
              <strong>Email CSKH:</strong>
              <p>hotro@zonemart.vn</p>
            </div>
          </div>

          <div class="method-item">
            <span class="icon">💬</span>
            <div>
              <strong>Kênh Telegram Hỗ Trợ:</strong>
              <p>@ZoneMartSupportBot</p>
            </div>
          </div>
        </div>
      </div>

      <div class="form-card">
        <div v-if="isSubmitted" class="success-box">
          <div class="success-icon">🎉</div>
          <h3>Gửi phản hồi thành công!</h3>
          <p>Cảm ơn bạn đã liên hệ. Đội ngũ CSKH sẽ phản hồi qua email <strong>{{ form.email }}</strong> trong vòng 15 phút.</p>
          <button class="btn btn-primary" @click="isSubmitted = false">Gửi tin nhắn khác</button>
        </div>

        <form v-else @submit.prevent="handleSubmit">
          <h3>Gửi Yêu Cầu Hỗ Trợ</h3>

          <div class="form-group">
            <label>Họ và tên của bạn:</label>
            <input v-model="form.fullName" type="text" placeholder="Nguyễn Văn A" class="input-field" required />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Địa chỉ Email:</label>
              <input v-model="form.email" type="email" placeholder="email@example.com" class="input-field" required />
            </div>
            <div class="form-group">
              <label>Số điện thoại:</label>
              <input v-model="form.phone" type="tel" placeholder="0912 345 678" class="input-field" />
            </div>
          </div>

          <div class="form-group">
            <label>Chủ đề cần hỗ trợ:</label>
            <select v-model="form.topic" class="input-field">
              <option value="order_issue">Khiếu nại / Kiểm tra đơn hàng</option>
              <option value="seller_partner">Hỗ trợ Cửa hàng (Seller Partner)</option>
              <option value="shipper_support">Hỗ trợ Tài xế (Shipper)</option>
              <option value="wallet_refund">Vấn đề Số dư Ví & Hoàn tiền</option>
              <option value="other">Ý kiến đóng góp khác</option>
            </select>
          </div>

          <div class="form-group">
            <label>Nội dung chi tiết:</label>
            <textarea v-model="form.message" rows="4" placeholder="Mô tả cụ thể vấn đề của bạn..." class="input-field" required></textarea>
          </div>

          <button type="submit" class="btn btn-primary btn-block">🚀 Gửi Tin Nhắn Cho ZoneMart</button>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.contact-container { max-width: 1100px; margin: 30px auto 60px auto; padding: 0 20px; }
.contact-header { text-align: center; margin-bottom: 30px; }
.contact-header h2 { margin: 0 0 8px 0; color: #0f172a; font-size: 26px; }
.contact-header p { margin: 0; color: #64748b; font-size: 15px; }

.contact-grid { display: grid; grid-template-columns: 1fr 1.3fr; gap: 28px; }
@media (max-width: 768px) { .contact-grid { grid-template-columns: 1fr; } }

.info-card { background: linear-gradient(135deg, #1a2f50 0%, #0f172a 100%); color: #fff; border-radius: 20px; padding: 32px; }
.info-card h3 { margin: 0 0 10px 0; font-size: 20px; }
.desc { color: #94a3b8; font-size: 14px; margin-bottom: 24px; line-height: 1.5; }

.contact-methods { display: flex; flex-direction: column; gap: 20px; }
.method-item { display: flex; gap: 14px; align-items: flex-start; }
.method-item .icon { font-size: 22px; }
.method-item strong { font-size: 14px; color: #cbd5e1; }
.method-item p { margin: 2px 0 0 0; font-size: 14px; color: #f8fafc; }
.method-item .highlight { color: #38bdf8; font-weight: 700; }

.form-card { background: #fff; border-radius: 20px; border: 1px solid #e2e8f0; padding: 32px; }
.form-card h3 { margin: 0 0 20px 0; font-size: 18px; color: #0f172a; }

.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
@media (max-width: 500px) { .form-row { grid-template-columns: 1fr; } }

.form-group { margin-bottom: 16px; }
.form-group label { display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 6px; }
.input-field { width: 100%; padding: 11px 14px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 14px; box-sizing: border-box; }

.btn { border: none; cursor: pointer; padding: 13px 20px; border-radius: 8px; font-weight: 700; font-size: 14px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-block { width: 100%; }

.success-box { text-align: center; padding: 40px 20px; }
.success-icon { font-size: 48px; margin-bottom: 12px; }
.success-box h3 { color: #16a34a; margin-bottom: 8px; }
.success-box p { color: #64748b; font-size: 14px; margin-bottom: 20px; }
</style>
