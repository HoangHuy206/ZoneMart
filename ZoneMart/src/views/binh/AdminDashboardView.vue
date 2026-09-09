<script setup lang="ts">
/**
 * ================================================================
 * TRANG QUẢN TRỊ ADMIN & MANAGER - Phụ trách: Bình
 * ================================================================
 */
import { ref } from "vue";

const activeTab = ref<"partners" | "products" | "disputes">("partners");

const pendingPartners = ref([
  {
    id: "part_1",
    type: "seller",
    name: "Tiệm Trà & Nước Ép Tươi Zone",
    applicant: "Trần Thị Mai",
    cccd: "001201012345",
    address: "56 Nguyễn Phong Sắc, Cầu Giấy",
    date: "07/09/2026"
  },
  {
    id: "part_2",
    type: "shipper",
    name: "Tài Xế: Lê Hoàng Nam",
    applicant: "Lê Hoàng Nam",
    cccd: "034200008899",
    address: "Xe Wave Alpha - 29N1-67890",
    date: "07/09/2026"
  }
]);

const suspiciousProducts = ref([
  {
    id: "prod_sp1",
    storeName: "Bách Hóa Cầu Giấy",
    productName: "Thịt Heo Rừng Sạch Đóng Khay",
    aiFlag: "Nghi ngờ: Ảnh chụp chưa khớp 100% với tên sản phẩm",
    price: 130000,
    image: "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=150"
  }
]);

const disputes = ref([
  {
    id: "disp_1",
    orderId: "ORD_6655",
    storeName: "Siêu Thị Trái Cây Xanh",
    shipperName: "Nguyễn Văn Nam",
    customerReason: "Khách báo không nghe máy kịp",
    amount: 195000
  }
]);

const handleApprovePartner = (id: string) => {
  pendingPartners.value = pendingPartners.value.filter(p => p.id !== id);
  alert("Đã DUYỆT đối tác! Tài khoản đã được kích hoạt.");
};

const handleRejectPartner = (id: string) => {
  pendingPartners.value = pendingPartners.value.filter(p => p.id !== id);
  alert("Đã TỪ CHỐI hồ sơ và gửi email yêu cầu bổ sung.");
};

const handleApproveProduct = (id: string) => {
  suspiciousProducts.value = suspiciousProducts.value.filter(p => p.id !== id);
  alert("Cho phép sản phẩm hiển thị trên sàn!");
};

const handleRejectProduct = (id: string) => {
  suspiciousProducts.value = suspiciousProducts.value.filter(p => p.id !== id);
  alert("Đã ẨN sản phẩm khỏi danh mục hiển thị!");
};

const handleResolveRefund = () => {
  alert("Đã phán quyết hoàn tiền vào ví người mua.");
};

const handleResolveShop = () => {
  alert("Đã hủy đơn và hoàn hàng về phía Shop.");
};
</script>

<template>
  <div class="admin-container">
    <div class="admin-header">
      <div>
        <h2><i class="bi bi-shield-check text-primary me-2" aria-hidden="true"></i>Bảng Điều Khiển Admin & Manager</h2>
        <p>Giám sát duyệt đối tác, kiểm duyệt bài đăng AI và xử lý khiếu nại sàn ZoneMart.</p>
      </div>
      <div class="admin-badge">Super Admin</div>
    </div>

    <div class="admin-tabs">
      <button class="tab-btn" :class="{ active: activeTab === 'partners' }" @click="activeTab = 'partners'">
        <i class="bi bi-people-fill me-1" aria-hidden="true"></i> Duyệt Đối Tác ({{ pendingPartners.length }})
      </button>
      <button class="tab-btn" :class="{ active: activeTab === 'products' }" @click="activeTab = 'products'">
        <i class="bi bi-robot me-1" aria-hidden="true"></i> Duyệt Sản Phẩm AI ({{ suspiciousProducts.length }})
      </button>
      <button class="tab-btn" :class="{ active: activeTab === 'disputes' }" @click="activeTab = 'disputes'">
        <i class="bi bi-shield-exclamation me-1" aria-hidden="true"></i> Tranh Chấp Đơn ({{ disputes.length }})
      </button>
    </div>

    <!-- Tab 1 -->
    <div v-if="activeTab === 'partners'" class="tab-grid">
      <div v-for="item in pendingPartners" :key="item.id" class="item-card">
        <div class="top-row">
          <span class="tag" :class="item.type">
            <i v-if="item.type === 'seller'" class="bi bi-shop me-1" aria-hidden="true"></i>
            <i v-else class="bi bi-bicycle me-1" aria-hidden="true"></i>
            {{ item.type === 'seller' ? 'Cửa hàng' : 'Shipper' }}
          </span>
          <span class="date">{{ item.date }}</span>
        </div>
        <h4>{{ item.name }}</h4>
        <p>Đại diện: <strong>{{ item.applicant }}</strong></p>
        <p>CCCD: <strong>{{ item.cccd }}</strong></p>
        <p>{{ item.address }}</p>
        <div class="actions">
          <button class="btn btn-sm btn-success" @click="handleApprovePartner(item.id)">
            <i class="bi bi-check-lg me-1" aria-hidden="true"></i> Phê Duyệt
          </button>
          <button class="btn btn-sm btn-danger" @click="handleRejectPartner(item.id)">
            <i class="bi bi-x-lg me-1" aria-hidden="true"></i> Từ Chối
          </button>
        </div>
      </div>
    </div>

    <!-- Tab 2 -->
    <div v-if="activeTab === 'products'" class="tab-grid">
      <div v-for="prod in suspiciousProducts" :key="prod.id" class="prod-item-card">
        <img :src="prod.image" :alt="prod.productName" />
        <div>
          <span class="flag"><i class="bi bi-exclamation-triangle-fill text-warning me-1" aria-hidden="true"></i> {{ prod.aiFlag }}</span>
          <h4>{{ prod.productName }}</h4>
          <p>{{ prod.storeName }} • <strong>{{ prod.price.toLocaleString('vi-VN') }} ₫</strong></p>
          <div class="actions">
            <button class="btn btn-sm btn-success" @click="handleApproveProduct(prod.id)">
              <i class="bi bi-check-lg me-1" aria-hidden="true"></i> Cho Phép Bán
            </button>
            <button class="btn btn-sm btn-danger" @click="handleRejectProduct(prod.id)">
              <i class="bi bi-eye-slash-fill me-1" aria-hidden="true"></i> Ẩn Món
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Tab 3 -->
    <div v-if="activeTab === 'disputes'" class="dispute-col">
      <div v-for="d in disputes" :key="d.id" class="d-card">
        <div class="d-head">
          <strong>Đơn hàng: #{{ d.orderId }}</strong>
          <span class="d-val">{{ d.amount.toLocaleString('vi-VN') }} ₫</span>
        </div>
        <p>{{ d.storeName }} | Tài xế: {{ d.shipperName }}</p>
        <p class="reason">Lý do: "{{ d.customerReason }}"</p>
        <div class="actions">
          <button class="btn btn-sm btn-primary" @click="handleResolveRefund">Phán quyết: Hoàn tiền khách</button>
          <button class="btn btn-sm btn-danger" @click="handleResolveShop">Phán quyết: Hoàn hàng về Shop</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin-container { max-width: 1100px; margin: 30px auto 60px auto; padding: 0 20px; }
.admin-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
.admin-header h2 { margin: 0 0 4px 0; color: #0f172a; font-size: 24px; }
.admin-header p { margin: 0; color: #64748b; font-size: 14px; }
.admin-badge { background: #0f172a; color: #fff; font-size: 12px; font-weight: 700; padding: 6px 14px; border-radius: 20px; }

.admin-tabs { display: flex; gap: 10px; margin-bottom: 24px; border-bottom: 1px solid #e2e8f0; padding-bottom: 12px; }
.tab-btn { background: #f1f5f9; border: 1px solid #e2e8f0; padding: 10px 18px; border-radius: 10px; font-size: 14px; font-weight: 600; color: #475569; cursor: pointer; }
.tab-btn.active { background: #2563eb; color: #fff; border-color: #2563eb; }

.tab-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 20px; }
.item-card, .prod-item-card, .d-card { background: #fff; border: 1px solid #e2e8f0; border-radius: 14px; padding: 20px; }

.top-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
.tag { font-size: 12px; font-weight: 700; padding: 3px 8px; border-radius: 6px; }
.tag.seller { background: #ffedd5; color: #c2410c; }
.tag.shipper { background: #f3e8ff; color: #7e22ce; }
.date { font-size: 12px; color: #94a3b8; }
.item-card h4 { margin: 0 0 8px 0; font-size: 16px; color: #0f172a; }
.item-card p { margin: 0 0 4px 0; font-size: 13px; color: #475569; }

.prod-item-card { display: flex; gap: 16px; align-items: center; }
.prod-item-card img { width: 90px; height: 90px; border-radius: 10px; object-fit: cover; }
.flag { font-size: 11px; color: #b45309; background: #fef3c7; padding: 2px 6px; border-radius: 4px; font-weight: 700; }
.prod-item-card h4 { margin: 6px 0 4px 0; font-size: 15px; color: #0f172a; }

.d-card { margin-bottom: 14px; }
.d-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
.d-val { font-weight: 800; color: #dc2626; }
.reason { background: #fef2f2; color: #b91c1c; padding: 8px; border-radius: 6px; font-size: 13px; }

.actions { display: flex; gap: 8px; margin-top: 14px; }
.btn { border: none; cursor: pointer; border-radius: 6px; font-weight: 600; }
.btn-sm { padding: 6px 12px; font-size: 12px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-success { background: #16a34a; color: #fff; }
.btn-danger { background: #dc2626; color: #fff; }
</style>
