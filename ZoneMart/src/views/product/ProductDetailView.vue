<script setup lang="ts">
/**
 * ================================================================
 * CHI TIẾT SẢN PHẨM - Phụ trách: Huy
 * ================================================================
 */
import { ref } from "vue";
import { useRoute, useRouter } from "vue-router";

const route = useRoute();
const router = useRouter();

const quantity = ref(1);

const product = ref({
  id: route.params.id || "p1",
  name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (500g)",
  category: "Thực phẩm tươi sống",
  price: 185000,
  weight: 0.5,
  description: "Thịt bò Mỹ nhập khẩu đóng khay tiêu chuẩn an toàn thực phẩm. Sản phẩm đã qua kiểm định nghiêm ngặt của ZoneMart và hệ thống AI kiểm duyệt chất lượng.",
  image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800",
  store: {
    name: "ZoneMart Cầu Giấy",
    address: "245 Cầu Giấy, Hà Nội",
    distanceKm: 1.2
  },
  stockQuantity: 45
});

const addToCart = () => {
  alert(`Đã thêm ${quantity.value} sản phẩm "${product.value.name}" vào giỏ hàng!`);
};
</script>

<template>
  <div class="detail-container">
    <button class="back-link" @click="router.back()">
      <i class="bi bi-arrow-left"></i> Quay lại danh sách sản phẩm
    </button>

    <div class="product-layout">
      <div class="img-col">
        <img :src="product.image" :alt="product.name" class="main-img" />
        <div class="ai-badge">
          <i class="bi bi-shield-check"></i> Đã kiểm duyệt nguồn gốc VietGAP & Vệ sinh ATTP
        </div>
      </div>

      <div class="info-col">
        <span class="cat-tag">{{ product.category }}</span>
        <h1 class="title">{{ product.name }}</h1>

        <div class="store-info-box">
          <div class="s-left">
            <span class="icon"><i class="bi bi-shop"></i></span>
            <div>
              <strong>{{ product.store.name }}</strong>
              <p><i class="bi bi-geo-alt-fill"></i> {{ product.store.address }} (Cách bạn {{ product.store.distanceKm }}km)</p>
            </div>
          </div>
          <span class="chip-express" v-if="product.store.distanceKm <= 3">
            <i class="bi bi-lightning-charge-fill"></i> Hỗ trợ Hỏa Tốc
          </span>
        </div>

        <div class="price-row">
          <span class="price-num">{{ product.price.toLocaleString("vi-VN") }} ₫</span>
          <span class="stock-info">Tồn kho: {{ product.stockQuantity }} sản phẩm</span>
        </div>

        <div class="desc-content">
          <h3>Mô tả sản phẩm</h3>
          <p>{{ product.description }}</p>
          <span class="weight">
            <i class="bi bi-box-seam"></i> Trọng lượng đóng gói: <strong>{{ product.weight }} kg</strong>
          </span>
        </div>

        <div class="buy-actions">
          <div class="qty-btn-group">
            <button @click="quantity = Math.max(1, quantity - 1)">-</button>
            <input type="number" v-model.number="quantity" min="1" aria-label="Số lượng sản phẩm" />
            <button @click="quantity++">+</button>
          </div>
          <button class="btn-cart" @click="addToCart">
            <i class="bi bi-bag-plus-fill"></i> Thêm Vào Giỏ Hàng
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.detail-container { max-width: 1100px; margin: 30px auto 60px auto; padding: 0 20px; }
.back-link { background: none; border: none; color: #2563eb; font-weight: 600; cursor: pointer; margin-bottom: 20px; font-size: 14px; }

.product-layout { display: grid; grid-template-columns: 1fr 1.2fr; gap: 36px; background: #fff; border-radius: 20px; border: 1px solid #e2e8f0; padding: 32px; }
@media (max-width: 768px) { .product-layout { grid-template-columns: 1fr; } }

.img-col { display: flex; flex-direction: column; gap: 14px; }
.main-img { width: 100%; height: 380px; object-fit: cover; border-radius: 14px; }
.ai-badge { background: #ecfdf5; color: #065f46; padding: 10px 14px; border-radius: 8px; font-size: 13px; font-weight: 600; border: 1px solid #a7f3d0; text-align: center; }

.cat-tag { font-size: 12px; color: #64748b; font-weight: 700; text-transform: uppercase; }
.title { margin: 8px 0 16px 0; font-size: 24px; color: #0f172a; }

.store-info-box { background: #f8fafc; padding: 14px; border-radius: 12px; border: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.s-left { display: flex; align-items: center; gap: 12px; }
.s-left .icon { font-size: 24px; }
.s-left p { margin: 2px 0 0 0; font-size: 12px; color: #64748b; }
.chip-express { background: #fee2e2; color: #dc2626; font-size: 11px; font-weight: 700; padding: 3px 8px; border-radius: 6px; }

.price-row { display: flex; align-items: baseline; gap: 16px; margin-bottom: 20px; }
.price-num { font-size: 30px; font-weight: 800; color: #dc2626; }
.stock-info { font-size: 13px; color: #64748b; }

.desc-content { border-top: 1px solid #f1f5f9; padding-top: 16px; margin-bottom: 24px; }
.desc-content h3 { margin: 0 0 8px 0; font-size: 16px; color: #0f172a; }
.desc-content p { color: #475569; font-size: 14px; line-height: 1.6; margin: 0 0 10px 0; }
.weight { font-size: 13px; color: #334155; }

.buy-actions { display: flex; gap: 16px; align-items: center; }
.qty-btn-group { display: flex; border: 1px solid #cbd5e1; border-radius: 8px; overflow: hidden; }
.qty-btn-group button { background: #f8fafc; border: none; width: 38px; height: 42px; cursor: pointer; font-size: 18px; }
.qty-btn-group input { width: 50px; text-align: center; border: none; font-size: 15px; font-weight: 600; }
.btn-cart { flex-grow: 1; background: #2563eb; color: #fff; border: none; height: 44px; border-radius: 8px; font-weight: 700; font-size: 15px; cursor: pointer; }
.btn-cart:hover { background: #1d4ed8; }
</style>
