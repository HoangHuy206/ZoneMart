<script setup lang="ts">
/**
 * ================================================================
 * TRANG DANH SÁCH SẢN PHẨM - Phụ trách: Huy
 * ================================================================
 */
import { ref, computed } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const selectedCategory = ref("all");
const searchQuery = ref("");
const maxRadiusKm = ref(10);

const categories = [
  { id: "all", name: "Tất cả" },
  { id: "food", name: "Thực phẩm tươi" },
  { id: "beverage", name: "Đồ uống & Trái cây" },
  { id: "fastfood", name: "Thức ăn nhanh" },
  { id: "household", name: "Đồ dùng gia đình" }
];

const products = ref([
  {
    id: "p1",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (500g)",
    category: "food",
    price: 185000,
    storeName: "ZoneMart Cầu Giấy",
    distanceKm: 1.2,
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=400"
  },
  {
    id: "p2",
    name: "Hộp Dâu Tây Đà Lạt Tươi Ngọt (500g)",
    category: "beverage",
    price: 95000,
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400"
  },
  {
    id: "p3",
    name: "Combo Bánh Mì Chảo Nóng Hổi",
    category: "fastfood",
    price: 45000,
    storeName: "Tiệm Bánh Mì Zone",
    distanceKm: 2.8,
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=400"
  },
  {
    id: "p4",
    name: "Nước Ép Cam Tươi Nguyên Chất 100%",
    category: "beverage",
    price: 32000,
    storeName: "Siêu Thị Trái Cây Xanh",
    distanceKm: 2.5,
    image: "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=400"
  },
  {
    id: "p5",
    name: "Bộ Nồi Inox 3 Đáy Cao Cấp",
    category: "household",
    price: 420000,
    storeName: "Tổng Kho Gia Dụng Mỹ Đình",
    distanceKm: 5.4,
    image: "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=400"
  },
  {
    id: "p6",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị",
    category: "food",
    price: 190000,
    storeName: "ZoneMart Cầu Giấy",
    distanceKm: 1.2,
    image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400"
  }
]);

const filteredProducts = computed(() => {
  return products.value.filter((p) => {
    const matchCat = selectedCategory.value === "all" || p.category === selectedCategory.value;
    const matchQuery = p.name.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
                       p.storeName.toLowerCase().includes(searchQuery.value.toLowerCase());
    return matchCat && matchQuery;
  });
});

const onAddToCart = (name: string) => {
  alert(`Đã thêm "${name}" vào giỏ hàng!`);
};

const goToDetail = (id: string) => {
  router.push(`/products/${id}`);
};
</script>

<template>
  <div class="product-page">
    <!-- Header banner -->
    <div class="search-banner">
      <h2>🛒 Tất Cả Sản Phẩm ZoneMart</h2>
      <p>Lọc các món ngon & nhu yếu phẩm từ các cửa hàng trong bán kính <strong>{{ maxRadiusKm }}km</strong></p>
      
      <div class="search-bar">
        <input 
          v-model="searchQuery" 
          type="text" 
          placeholder="Tìm tên món hàng, quán ăn, thương hiệu..." 
          class="search-input" 
        />
      </div>
    </div>

    <!-- Category filter tabs -->
    <div class="filter-row">
      <div class="category-tabs">
        <button 
          v-for="cat in categories" 
          :key="cat.id"
          class="tab-btn"
          :class="{ active: selectedCategory === cat.id }"
          @click="selectedCategory = cat.id"
        >
          {{ cat.name }}
        </button>
      </div>
      <div class="radius-tag">📍 Bán kính tối đa: 10km</div>
    </div>

    <!-- Products Grid -->
    <div class="products-grid">
      <div 
        v-for="p in filteredProducts" 
        :key="p.id" 
        class="product-card"
        @click="goToDetail(p.id)"
      >
        <div class="img-wrap">
          <img :src="p.image" :alt="p.name" />
          <span class="express-chip" v-if="p.distanceKm <= 3">⚡ Hỏa Tốc</span>
        </div>
        <div class="card-body">
          <div class="store-row">
            <span>🏪 {{ p.storeName }}</span>
            <span class="dist">{{ p.distanceKm }} km</span>
          </div>
          <h4 class="title">{{ p.name }}</h4>
          <div class="card-footer">
            <span class="price">{{ p.price.toLocaleString("vi-VN") }} ₫</span>
            <button class="btn-buy" @click.stop="onAddToCart(p.name)">+ Thêm</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.product-page { max-width: 1250px; margin: 0 auto; padding: 30px 20px 60px 20px; }
.search-banner {
  background: linear-gradient(135deg, #1a2f50 0%, #0f172a 100%);
  color: #fff;
  padding: 40px 24px;
  border-radius: 20px;
  text-align: center;
  margin-bottom: 30px;
}
.search-banner h2 { margin: 0 0 8px 0; font-size: 28px; }
.search-banner p { margin: 0 0 20px 0; color: #94a3b8; }
.search-bar { max-width: 500px; margin: 0 auto; }
.search-input {
  width: 100%;
  padding: 13px 20px;
  border-radius: 30px;
  border: none;
  font-size: 15px;
  outline: none;
  box-sizing: border-box;
}

.filter-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; flex-wrap: wrap; gap: 12px; }
.category-tabs { display: flex; gap: 8px; flex-wrap: wrap; }
.tab-btn {
  padding: 8px 18px;
  background: #fff;
  border: 1px solid #cbd5e1;
  border-radius: 20px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  color: #475569;
}
.tab-btn.active { background: #2563eb; color: #fff; border-color: #2563eb; }
.radius-tag { font-size: 13px; color: #1e40af; background: #eff6ff; padding: 6px 14px; border-radius: 20px; font-weight: 600; }

.products-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 20px;
}
.product-card {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  flex-direction: column;
}
.product-card:hover { transform: translateY(-4px); box-shadow: 0 10px 20px -5px rgba(0,0,0,0.06); }
.img-wrap { position: relative; height: 180px; }
.img-wrap img { width: 100%; height: 100%; object-fit: cover; }
.express-chip { position: absolute; top: 10px; left: 10px; background: #dc2626; color: #fff; font-size: 11px; font-weight: 700; padding: 3px 8px; border-radius: 6px; }

.card-body { padding: 16px; display: flex; flex-direction: column; flex-grow: 1; }
.store-row { display: flex; justify-content: space-between; font-size: 11px; color: #64748b; margin-bottom: 6px; }
.dist { font-weight: 700; color: #ea580c; }
.title { font-size: 14px; color: #0f172a; margin: 0 0 12px 0; height: 40px; overflow: hidden; line-height: 1.4; }
.card-footer { display: flex; justify-content: space-between; align-items: center; margin-top: auto; }
.price { font-size: 18px; font-weight: 800; color: #dc2626; }
.btn-buy { background: #2563eb; color: #fff; border: none; padding: 6px 14px; border-radius: 8px; font-weight: 700; font-size: 13px; cursor: pointer; }
.btn-buy:hover { background: #1d4ed8; }
</style>
