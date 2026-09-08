<script setup lang="ts">
/**
 * ================================================================
 * GIỎ HÀNG (CART) - Phụ trách: Thắng
 * ================================================================
 */
import { ref, computed } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const cartStores = ref([
  {
    storeId: "st_1",
    storeName: "ZoneMart Bách Hóa Cầu Giấy",
    distanceKm: 1.2,
    items: [
      {
        id: "p1",
        name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (500g)",
        price: 185000,
        quantity: 2,
        image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=150"
      },
      {
        id: "p6",
        name: "Gạo ST25 Ông Cua Túi 5kg",
        price: 190000,
        quantity: 1,
        image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=150"
      }
    ]
  },
  {
    storeId: "st_2",
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    items: [
      {
        id: "p2",
        name: "Hộp Dâu Tây Đà Lạt Tươi Ngọt (500g)",
        price: 95000,
        quantity: 1,
        image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=150"
      }
    ]
  }
]);

const increaseQty = (item: any) => { item.quantity++; };
const decreaseQty = (sIdx: number, iIdx: number) => {
  const item = cartStores.value[sIdx].items[iIdx];
  if (item.quantity > 1) {
    item.quantity--;
  } else {
    cartStores.value[sIdx].items.splice(iIdx, 1);
    if (cartStores.value[sIdx].items.length === 0) cartStores.value.splice(sIdx, 1);
  }
};
const removeItem = (sIdx: number, iIdx: number) => {
  cartStores.value[sIdx].items.splice(iIdx, 1);
  if (cartStores.value[sIdx].items.length === 0) cartStores.value.splice(sIdx, 1);
};

const subTotal = computed(() => {
  return cartStores.value.reduce((total, store) => {
    return total + store.items.reduce((sum, item) => sum + item.price * item.quantity, 0);
  }, 0);
});

const estimatedShipping = computed(() => cartStores.value.length * 15000);
const totalAmount = computed(() => subTotal.value + estimatedShipping.value);

const proceedToCheckout = () => {
  if (cartStores.value.length === 0) {
    alert("Giỏ hàng của bạn đang trống!");
    return;
  }
  router.push("/checkout");
};
</script>

<template>
  <div class="cart-container">
    <div class="cart-header">
      <h2>🛒 Giỏ Hàng Của Bạn</h2>
      <h2><i class="bi bi-cart3"></i> Giỏ Hàng Của Bạn</h2>
      <p>Các món hàng được gom nhóm theo từng Cửa Hàng (Sub-Order) theo thiết kế ZoneMart.</p>
    </div>

    <div v-if="cartStores.length > 0" class="cart-layout">
      <!-- Cột danh sách món hàng -->
      <div class="cart-list">
        <div v-for="(store, sIdx) in cartStores" :key="store.storeId" class="store-group-card">
          <div class="store-head">
            <span class="store-title">🏪 {{ store.storeName }}</span>
            <span class="store-title"><i class="bi bi-shop"></i> {{ store.storeName }}</span>
            <span class="store-dist">Cách bạn {{ store.distanceKm }} km • Hỗ trợ Hỏa Tốc</span>
          </div>

          <div class="items-list">
            <div v-for="(item, iIdx) in store.items" :key="item.id" class="cart-item">
              <img :src="item.image" :alt="item.name" class="item-img" />
              <div class="item-details">
                <h4 class="item-name">{{ item.name }}</h4>
                <div class="item-price">{{ item.price.toLocaleString("vi-VN") }} ₫</div>
              </div>

              <div class="qty-box">
                <button @click="decreaseQty(sIdx, iIdx)">-</button>
                <span>{{ item.quantity }}</span>
                <button @click="increaseQty(item)">+</button>
              </div>

              <div class="item-total">
                {{ (item.price * item.quantity).toLocaleString("vi-VN") }} ₫
              </div>

              <button class="btn-del" @click="removeItem(sIdx, iIdx)" title="Xóa">✕</button>
              <button class="btn-del" @click="removeItem(sIdx, iIdx)" title="Xóa món" aria-label="Xóa món khỏi giỏ">
                <i class="bi bi-trash3"></i>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Cột tổng kết đơn hàng -->
      <div class="cart-summary-col">
        <div class="summary-card">
          <h3>Tóm Tắt Đơn Hàng</h3>
          
          <div class="summary-row">
            <span>Tiền hàng ({{ cartStores.length }} quán):</span>
            <strong>{{ subTotal.toLocaleString("vi-VN") }} ₫</strong>
          </div>

          <div class="summary-row">
            <span>Phí ship cơ bản (15k/quán):</span>
            <strong>{{ estimatedShipping.toLocaleString("vi-VN") }} ₫</strong>
          </div>

          <div class="divider"></div>

          <div class="summary-row total-row">
            <span>Tổng cộng:</span>
            <strong class="grand-total">{{ totalAmount.toLocaleString("vi-VN") }} ₫</strong>
          </div>

          <p class="note">
            💡 <em>Khoảng cách &le; 3km bạn có thể chọn giao Hỏa Tốc ở bước thanh toán.</em>
            <i class="bi bi-lightbulb-fill text-warning"></i> <em>Khoảng cách &le; 3km bạn có thể chọn giao Hỏa Tốc ở bước thanh toán.</em>
          </p>

          <button class="btn btn-primary btn-block" @click="proceedToCheckout">
            Chốt Thông Tin Đặt Hàng ➜
            <span>Chốt Thông Tin Đặt Hàng</span>
            <i class="bi bi-arrow-right"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Giỏ hàng rỗng -->
    <div v-else class="empty-cart">
      <div class="empty-icon">🛒</div>
      <div class="empty-icon"><i class="bi bi-cart-x"></i></div>
      <h3>Giỏ hàng đang trống!</h3>
      <p>Hãy dạo quanh một vòng và chọn cho mình những món đồ ưng ý nhé.</p>
      <button class="btn btn-primary" @click="router.push('/products')">Xem Sản Phẩm Ngay</button>
      <button class="btn btn-primary" @click="router.push('/products')">
        <i class="bi bi-bag"></i> Xem Sản Phẩm Ngay
      </button>
    </div>
  </div>
</template>

<style scoped>
.cart-container { max-width: 1150px; margin: 30px auto 60px auto; padding: 0 20px; }
.cart-header h2 { margin: 0 0 6px 0; color: #0f172a; font-size: 24px; }
.cart-header p { margin: 0 0 24px 0; color: #64748b; font-size: 14px; }

.cart-layout { display: grid; grid-template-columns: 1.8fr 1fr; gap: 24px; }
@media (max-width: 800px) { .cart-layout { grid-template-columns: 1fr; } }

.store-group-card { background: #fff; border-radius: 16px; border: 1px solid #e2e8f0; margin-bottom: 20px; overflow: hidden; }
.store-head { background: #f8fafc; padding: 14px 20px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; }
.store-title { font-weight: 700; font-size: 14px; color: #0f172a; }
.store-dist { font-size: 12px; color: #e27d2b; font-weight: 600; }

.items-list { padding: 16px 20px; display: flex; flex-direction: column; gap: 16px; }
.cart-item { display: flex; align-items: center; gap: 16px; border-bottom: 1px solid #f1f5f9; padding-bottom: 14px; }
.cart-item:last-child { border-bottom: none; padding-bottom: 0; }

.item-img { width: 68px; height: 68px; border-radius: 10px; object-fit: cover; }
.item-details { flex-grow: 1; }
.item-name { margin: 0 0 4px 0; font-size: 14px; color: #1e293b; }
.item-price { font-size: 13px; color: #64748b; }

.qty-box { display: flex; align-items: center; border: 1px solid #cbd5e1; border-radius: 6px; overflow: hidden; }
.qty-box button { background: #f8fafc; border: none; width: 30px; height: 30px; cursor: pointer; font-weight: 700; }
.qty-box span { padding: 0 10px; font-size: 13px; font-weight: 600; }

.item-total { font-weight: 800; color: #0f172a; min-width: 90px; text-align: right; font-size: 15px; }
.btn-del { background: none; border: none; color: #94a3b8; cursor: pointer; font-size: 16px; margin-left: 8px; }
.btn-del:hover { color: #ef4444; }

.summary-card { background: #fff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 22px; position: sticky; top: 90px; }
.summary-card h3 { margin: 0 0 16px 0; font-size: 17px; color: #0f172a; }
.summary-row { display: flex; justify-content: space-between; font-size: 14px; margin-bottom: 12px; color: #475569; }
.divider { height: 1px; background: #e2e8f0; margin: 16px 0; }
.total-row { font-size: 16px; color: #0f172a; }
.grand-total { font-size: 22px; color: #ef4444; }

.note { font-size: 12px; color: #64748b; line-height: 1.5; margin: 14px 0 20px 0; }
.btn { border: none; cursor: pointer; padding: 13px 20px; border-radius: 10px; font-weight: 700; font-size: 14px; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-block { width: 100%; }

.empty-cart { text-align: center; padding: 70px 20px; background: #fff; border-radius: 20px; border: 1px solid #e2e8f0; }
.empty-icon { font-size: 64px; margin-bottom: 14px; }
.empty-cart h3 { margin: 0 0 8px 0; color: #0f172a; }
.empty-cart p { color: #64748b; margin: 0 0 20px 0; }
</style>
