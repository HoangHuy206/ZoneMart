<script setup lang="ts">
/**
 * ================================================================
 * HỒ SƠ CÁ NHÂN & VÍ TIỀN (PROFILE) - Phụ trách: Huy
 * ================================================================
 */
import { ref } from "vue";

const user = ref({
  phone_email: "huyhoangzz@zonemart.vn",
  full_name: "Hoàng Huy",
  avatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=200",
  wallet_balance: 450000,
  is_buyer: true,
  is_seller: false,
  is_shipper: false,
  address: "123 Đường Cầu Giấy, Quận Cầu Giấy, Hà Nội",
  role_application_status: "none"
});

const isEditing = ref(false);
const showUpgradeModal = ref(false);
const upgradeType = ref<"seller" | "shipper">("seller");
const upgradeForm = ref({ cccd: "", nameOrVehicle: "" });

const handleSaveProfile = () => {
  isEditing.value = false;
  alert("Cập nhật thông tin tài khoản thành công!");
};

const handleOpenUpgrade = (type: "seller" | "shipper") => {
  upgradeType.value = type;
  showUpgradeModal.value = true;
};

const handleSubmitUpgrade = () => {
  if (!upgradeForm.value.cccd || !upgradeForm.value.nameOrVehicle) {
    alert("Vui lòng điền đầy đủ số CCCD và thông tin đăng ký!");
    return;
  }
  user.value.role_application_status = "pending";
  showUpgradeModal.value = false;
  alert(`Đơn đăng ký làm ${upgradeType.value === 'seller' ? 'Người bán hàng' : 'Tài xế giao hàng'} đã được gửi duyệt!`);
};
</script>

<template>
  <div class="profile-container">
    <!-- Header Profile -->
    <div class="profile-card hero-card">
      <div class="avatar-section">
        <img :src="user.avatar" alt="Avatar" class="avatar-img" />
        <div class="user-meta">
          <h2>{{ user.full_name }}</h2>
          <p class="user-email">✉️ {{ user.phone_email }}</p>
          <div class="badge-group">
            <span class="badge buyer-badge" v-if="user.is_buyer">🛍️ Người mua</span>
            <span class="badge seller-badge" v-if="user.is_seller">🏪 Chủ cửa hàng</span>
            <span class="badge shipper-badge" v-if="user.is_shipper">🛵 Tài xế Shipper</span>
          </div>
        </div>
      </div>

      <div class="wallet-card">
        <div class="wallet-title">Ví Tiền Nội Bộ ZoneMart</div>
        <div class="wallet-amount">{{ user.wallet_balance.toLocaleString("vi-VN") }} ₫</div>
        <div class="wallet-actions">
          <button class="btn btn-sm btn-primary">+ Nạp tiền</button>
          <button class="btn btn-sm btn-outline">Lịch sử GD</button>
        </div>
      </div>
    </div>

    <!-- Thông tin & Đối tác -->
    <div class="profile-grid">
      <div class="profile-card">
        <div class="card-header">
          <h3>👤 Thông Tin Cá Nhân</h3>
          <button class="btn btn-sm btn-outline" @click="isEditing = !isEditing">
            {{ isEditing ? "Hủy" : "Chỉnh sửa" }}
          </button>
        </div>

        <div class="form-group">
          <label>Họ và tên:</label>
          <input v-if="isEditing" v-model="user.full_name" type="text" class="input-field" />
          <p v-else class="text-val">{{ user.full_name }}</p>
        </div>

        <div class="form-group">
          <label>Số điện thoại / Email:</label>
          <p class="text-val">{{ user.phone_email }}</p>
        </div>

        <div class="form-group">
          <label>Địa chỉ nhận hàng mặc định:</label>
          <input v-if="isEditing" v-model="user.address" type="text" class="input-field" />
          <p v-else class="text-val">{{ user.address }}</p>
        </div>

        <button v-if="isEditing" class="btn btn-primary btn-block" @click="handleSaveProfile">
          Lưu thay đổi
        </button>
      </div>

      <div class="profile-card">
        <div class="card-header">
          <h3>🚀 Nâng Cấp Đối Tác (Seller / Shipper)</h3>
        </div>
        <p class="desc-text">Bạn muốn mở cửa hàng kinh doanh hoặc đăng ký chạy xe giao hàng cùng ZoneMart?</p>

        <div v-if="user.role_application_status === 'pending'" class="status-box pending">
          ⏳ Hồ sơ đăng ký đang được <strong>Manager xét duyệt</strong>. Kết quả sẽ được gửi qua Email!
        </div>

        <div class="partner-options" v-else>
          <div class="partner-box">
            <h4>🏪 Mở Gian Hàng (Seller)</h4>
            <p>Đăng bán sản phẩm cho khách hàng trong bán kính 10km.</p>
            <button class="btn btn-secondary" @click="handleOpenUpgrade('seller')">Đăng ký làm Shop</button>
          </div>

          <div class="partner-box">
            <h4>🛵 Đăng Ký Tài Xế (Shipper)</h4>
            <p>Nhận các đơn hỏa tốc &le; 3km và đơn thường với thù lao hấp dẫn.</p>
            <button class="btn btn-secondary" @click="handleOpenUpgrade('shipper')">Đăng ký Shipper</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal Form -->
    <div v-if="showUpgradeModal" class="modal-backdrop">
      <div class="modal-content">
        <h3>Đăng ký: {{ upgradeType === 'seller' ? 'Chủ Cửa Hàng' : 'Tài Xế Shipper' }}</h3>
        <p class="sub-text">Hồ sơ sẽ được chuyển cho Manager kiểm tra theo Luồng 1.</p>

        <div class="form-group">
          <label>Số CCCD:</label>
          <input v-model="upgradeForm.cccd" type="text" placeholder="12 số CCCD" class="input-field" />
        </div>

        <div class="form-group">
          <label>{{ upgradeType === 'seller' ? 'Tên Cửa hàng:' : 'Biển số & Loại xe:' }}</label>
          <input v-model="upgradeForm.nameOrVehicle" type="text" placeholder="Nhập thông tin..." class="input-field" />
        </div>

        <div class="modal-actions">
          <button class="btn btn-outline" @click="showUpgradeModal = false">Hủy</button>
          <button class="btn btn-primary" @click="handleSubmitUpgrade">Gửi duyệt hồ sơ</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.profile-container { max-width: 1050px; margin: 30px auto 60px auto; padding: 0 20px; }
.profile-card { background: #ffffff; border-radius: 16px; padding: 24px; border: 1px solid #e2e8f0; }

.hero-card {
  display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 20px; margin-bottom: 24px;
  background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
}
.avatar-section { display: flex; align-items: center; gap: 20px; }
.avatar-img { width: 90px; height: 90px; border-radius: 50%; border: 3px solid #2563eb; object-fit: cover; }
.user-meta h2 { margin: 0 0 4px 0; color: #1e293b; }
.user-email { margin: 0 0 10px 0; color: #64748b; font-size: 14px; }
.badge-group { display: flex; gap: 8px; }
.badge { font-size: 12px; padding: 4px 10px; border-radius: 20px; font-weight: 600; }
.buyer-badge { background: #dbeafe; color: #1e40af; }
.seller-badge { background: #ffedd5; color: #c2410c; }
.shipper-badge { background: #f3e8ff; color: #7e22ce; }

.wallet-card { background: #0f172a; color: #fff; padding: 18px 24px; border-radius: 14px; text-align: right; }
.wallet-title { font-size: 13px; color: #94a3b8; }
.wallet-amount { font-size: 26px; font-weight: 800; color: #4ade80; margin: 6px 0 12px 0; }
.wallet-actions { display: flex; gap: 8px; justify-content: flex-end; }

.profile-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
@media (max-width: 768px) { .profile-grid { grid-template-columns: 1fr; } }

.card-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #f1f5f9; padding-bottom: 12px; margin-bottom: 16px; }
.card-header h3 { margin: 0; font-size: 18px; color: #0f172a; }

.form-group { margin-bottom: 16px; }
.form-group label { display: block; font-size: 13px; font-weight: 600; color: #475569; margin-bottom: 6px; }
.text-val { font-size: 15px; color: #0f172a; margin: 0; font-weight: 500; }
.input-field { width: 100%; padding: 10px 12px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 14px; box-sizing: border-box; }
.desc-text { color: #64748b; font-size: 14px; margin-bottom: 16px; }

.partner-box { background: #f8fafc; border: 1px dashed #cbd5e1; padding: 16px; border-radius: 10px; margin-bottom: 14px; }
.partner-box h4 { margin: 0 0 4px 0; font-size: 15px; color: #1e293b; }
.partner-box p { font-size: 13px; color: #64748b; margin: 0 0 10px 0; }
.status-box.pending { background: #fef9c3; color: #854d0e; padding: 14px; border-radius: 8px; font-size: 14px; border: 1px solid #fde047; }

.btn { border: none; cursor: pointer; padding: 10px 18px; border-radius: 8px; font-weight: 600; font-size: 14px; }
.btn-sm { padding: 6px 12px; font-size: 12px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-secondary { background: #e27d2b; color: #fff; }
.btn-secondary:hover { background: #c2410c; }
.btn-outline { background: transparent; border: 1px solid #cbd5e1; color: #334155; }
.btn-block { width: 100%; margin-top: 10px; }

.modal-backdrop { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 999; }
.modal-content { background: #fff; padding: 28px; border-radius: 14px; width: 90%; max-width: 480px; }
.modal-content h3 { margin-top: 0; color: #0f172a; }
.modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
</style>
