<script setup lang="ts">
/**
 * ================================================================
 * TRANG CHỦ (HOMEPAGE) - Phụ trách: Thắng
 * Mô tả: Sàn thương mại điện tử ZoneMart giao hàng hỏa tốc 10km
 * ================================================================
 */
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const searchQuery = ref("");

// Danh mục ngành hàng
const categories = [
  { id: 1, name: "Thực Phẩm Tươi", icon: "🥩", count: "120+ quán" },
  { id: 2, name: "Trái Cây Nhập Khẩu", icon: "🍇", count: "85+ quán" },
  { id: 3, name: "Cơm & Đồ Ăn Nhanh", icon: "🍱", count: "210+ quán" },
  { id: 4, name: "Đồ Uống & Trà Sữa", icon: "🧋", count: "95+ quán" },
  { id: 5, name: "Bách Hóa & Gia Dụng", icon: "🛒", count: "60+ quán" }
];

// Cửa hàng gần bạn (trong bán kính 10km)
const nearbyShops = [
  { 
    id: "s1", 
    name: "ZoneMart Bách Hóa Cầu Giấy", 
    distance: "1.2 km", 
    rating: "4.9", 
    address: "245 Cầu Giấy, Hà Nội",
    time: "15-20 phút",
    img: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=500" 
  },
  { 
    id: "s2", 
    name: "Siêu Thị Trái Cây Tươi Xanh", 
    distance: "2.5 km", 
    rating: "4.8", 
    address: "88 Trần Thái Tông, Cầu Giấy",
    time: "20-25 phút",
    img: "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=500" 
  },
  { 
    id: "s3", 
    name: "Tiệm Bánh Mì & Cà Phê Zone", 
    distance: "2.8 km", 
    rating: "4.7", 
    address: "12 Hồ Tùng Mậu, Mai Dịch",
    time: "15-25 phút",
    img: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=500" 
  }
];

// Sản phẩm bán chạy hôm nay
const hotProducts = [
  {
    id: "p1",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (500g)",
    price: 185000,
    oldPrice: 220000,
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    img: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=400",
    isExpress: true
  },
  {
    id: "p2",
    name: "Dâu Tây Đà Lạt Tươi Ngọt Hộp 500g",
    price: 95000,
    oldPrice: 125000,
    store: "Siêu Thị Trái Cây Xanh",
    distance: "2.5 km",
    img: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400",
    isExpress: true
  },
  {
    id: "p3",
    name: "Combo Cơm Gà Nướng Mật Ong + Canh",
    price: 55000,
    oldPrice: 65000,
    store: "Tiệm Bánh Mì & Cơm Zone",
    distance: "2.8 km",
    img: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400",
    isExpress: true
  },
  {
    id: "p4",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị",
    price: 190000,
    oldPrice: 215000,
    store: "ZoneMart Cầu Giấy",
    distance: "1.2 km",
    img: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400",
    isExpress: true
  }
];

const handleAddToCart = (name: string) => {
  alert(`Đã thêm "${name}" vào giỏ hàng!`);
};

const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({ path: "/products", query: { q: searchQuery.value } });
  } else {
    router.push("/products");
  }
};
</script>

<template>
  <div class="home-container">
    <!-- Hero Banner Hiện Đại -->
    <section class="hero-banner">
      <div class="hero-content">
        <div class="badge-pill">⚡ Sàn Thương Mại Giao Hàng Hỏa Tốc</div>
        <h1 class="hero-title">
          Mua Sắm Tiện Lợi & Giao Nhanh <span class="text-orange">Trong Bán Kính 10km</span>
        </h1>
        <p class="hero-sub">
          Hàng ngàn món ngon, thực phẩm sạch và đồ gia dụng từ các cửa hàng uy tín quanh khu vực của bạn. Nhận hàng siêu tốc từ 15-25 phút.
        </p>

        <!-- Thanh tìm kiếm nhanh -->
        <div class="search-box">
          <span class="search-icon">🔍</span>
          <input 
            v-model="searchQuery" 
            type="text" 
            placeholder="Tìm món ăn, thực phẩm, tên cửa hàng gần bạn..." 
            @keyup.enter="handleSearch"
          />
          <button class="btn-search" @click="handleSearch">Tìm Kiếm</button>
        </div>

        <div class="quick-tags">
          <span>Gợi ý nhanh:</span>
          <a @click="router.push('/products')">Thịt bò Mỹ</a>
          <a @click="router.push('/products')">Dâu tây</a>
          <a @click="router.push('/products')">Bánh mì chảo</a>
          <a @click="router.push('/map')">📍 Xem bản đồ 10km</a>
        </div>
      </div>

      <div class="hero-visual">
        <div class="visual-card">
          <img src="/logo.png" alt="ZoneMart" class="logo-hero" />
          <h3>Giao Hàng Siêu Tốc &le; 3km</h3>
          <p>Tối ưu tuyến đường thông minh, tài xế nhận đơn lập tức theo Luồng 4.</p>
          <div class="perk-row">
            <span>🛡️ AI Kiểm Duyệt</span>
            <span>⚡ Hỏa Tốc QR</span>
            <span>📍 Map Live</span>
          </div>
        </div>
      </div>
    </section>

    <!-- Danh mục nổi bật -->
    <section class="section-wrap">
      <div class="section-heading">
        <h2>Danh Mục Mua Sắm Nổi Bật</h2>
        <p>Tìm kiếm nhanh các mặt hàng thiết yếu theo nhu cầu</p>
      </div>

      <div class="category-grid">
        <div 
          v-for="cat in categories" 
          :key="cat.id" 
          class="cat-item"
          @click="router.push('/products')"
        >
          <div class="cat-icon">{{ cat.icon }}</div>
          <h4>{{ cat.name }}</h4>
          <span class="cat-count">{{ cat.count }}</span>
        </div>
      </div>
    </section>

    <!-- Cửa hàng đối tác gần bạn (Luồng 1) -->
    <section class="section-wrap">
      <div class="section-heading flex-between">
        <div>
          <h2>Cửa Hàng Đối Tác Quanh Bạn (Bán Kính 10km)</h2>
          <p>Các gian hàng đã được xác minh địa chỉ và giấy phép kinh doanh</p>
        </div>
        <button class="btn-link" @click="router.push('/map')">Xem trên bản đồ ➜</button>
      </div>

      <div class="shop-grid">
        <div v-for="shop in nearbyShops" :key="shop.id" class="shop-card">
          <div class="shop-img-box">
            <img :src="shop.img" :alt="shop.name" />
            <span class="dist-chip">📍 {{ shop.distance }}</span>
          </div>
          <div class="shop-body">
            <div class="shop-title-row">
              <h4>{{ shop.name }}</h4>
              <span class="rating">⭐ {{ shop.rating }}</span>
            </div>
            <p class="addr">{{ shop.address }}</p>
            <div class="shop-footer">
              <span class="time">⏰ {{ shop.time }}</span>
              <button class="btn-order" @click="router.push('/products')">Vào Gian Hàng</button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Sản phẩm bán chạy hôm nay -->
    <section class="section-wrap">
      <div class="section-heading flex-between">
        <div>
          <h2>🔥 Món Ngon & Nhu Yếu Phẩm Bán Chạy</h2>
          <p>Giao ngay hỏa tốc, đặt là có trong 15-25 phút</p>
        </div>
        <button class="btn-link" @click="router.push('/products')">Xem tất cả ➜</button>
      </div>

      <div class="product-grid">
        <div 
          v-for="prod in hotProducts" 
          :key="prod.id" 
          class="prod-card"
          @click="router.push(`/products/${prod.id}`)"
        >
          <div class="prod-img-box">
            <img :src="prod.img" :alt="prod.name" />
            <span class="express-badge" v-if="prod.isExpress">⚡ Hỏa Tốc</span>
          </div>
          <div class="prod-body">
            <span class="store-name">🏪 {{ prod.store }} ({{ prod.distance }})</span>
            <h4 class="prod-name" :title="prod.name">{{ prod.name }}</h4>
            <div class="price-row">
              <div class="price-group">
                <span class="price-current">{{ prod.price.toLocaleString("vi-VN") }} ₫</span>
                <span class="price-old">{{ prod.oldPrice.toLocaleString("vi-VN") }} ₫</span>
              </div>
              <button class="btn-add" @click.stop="handleAddToCart(prod.name)">+ Mua</button>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.home-container {
  max-width: 1250px;
  margin: 0 auto;
  padding: 30px 20px 60px 20px;
}

/* Hero Banner */
.hero-banner {
  background: linear-gradient(135deg, #1a2f50 0%, #0f172a 100%);
  color: #fff;
  border-radius: 24px;
  padding: 50px 44px;
  display: grid;
  grid-template-columns: 1.3fr 0.9fr;
  gap: 36px;
  align-items: center;
  box-shadow: 0 20px 30px -10px rgba(15, 23, 42, 0.25);
  margin-bottom: 50px;
}
@media (max-width: 900px) {
  .hero-banner { grid-template-columns: 1fr; padding: 36px 24px; }
}

.badge-pill {
  background: rgba(226, 125, 43, 0.2);
  color: #fb923c;
  border: 1px solid #ea580c;
  padding: 4px 14px;
  border-radius: 20px;
  font-size: 13px;
  font-weight: 700;
  display: inline-block;
  margin-bottom: 16px;
}

.hero-title {
  font-size: 36px;
  line-height: 1.25;
  margin: 0 0 16px 0;
  font-weight: 800;
}
.text-orange { color: #e27d2b; }

.hero-sub {
  color: #94a3b8;
  font-size: 16px;
  line-height: 1.6;
  margin: 0 0 28px 0;
  max-width: 600px;
}

.search-box {
  background: #fff;
  border-radius: 50px;
  padding: 6px 8px 6px 18px;
  display: flex;
  align-items: center;
  box-shadow: 0 10px 25px rgba(0,0,0,0.2);
  max-width: 540px;
}
.search-icon { font-size: 18px; margin-right: 10px; color: #64748b; }
.search-box input {
  flex-grow: 1;
  border: none;
  outline: none;
  font-size: 15px;
  color: #0f172a;
}
.btn-search {
  background: #e27d2b;
  color: #fff;
  border: none;
  padding: 12px 24px;
  border-radius: 40px;
  font-weight: 700;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-search:hover { background: #c2410c; }

.quick-tags {
  margin-top: 18px;
  font-size: 13px;
  color: #94a3b8;
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.quick-tags a {
  color: #38bdf8;
  text-decoration: none;
  cursor: pointer;
}
.quick-tags a:hover { text-decoration: underline; }

.visual-card {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 20px;
  padding: 30px;
  text-align: center;
  backdrop-filter: blur(8px);
}
.logo-hero {
  height: 90px;
  margin-bottom: 12px;
  object-fit: contain;
}
.visual-card h3 { margin: 0 0 8px 0; font-size: 20px; color: #f8fafc; }
.visual-card p { margin: 0 0 20px 0; color: #94a3b8; font-size: 14px; }
.perk-row { display: flex; justify-content: center; gap: 12px; font-size: 12px; color: #cbd5e1; font-weight: 600; }
.perk-row span { background: rgba(255,255,255,0.08); padding: 4px 10px; border-radius: 12px; }

/* Sections */
.section-wrap { margin-bottom: 50px; }
.section-heading h2 { margin: 0 0 6px 0; font-size: 24px; color: #0f172a; }
.section-heading p { margin: 0; color: #64748b; font-size: 14px; }
.flex-between { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px; }
.btn-link { background: none; border: none; color: #2563eb; font-weight: 700; cursor: pointer; font-size: 14px; }

/* Category Grid */
.category-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 16px;
  margin-top: 24px;
}
.cat-item {
  background: #fff;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  padding: 22px;
  text-align: center;
  cursor: pointer;
  transition: all 0.25s;
}
.cat-item:hover {
  transform: translateY(-4px);
  border-color: #e27d2b;
  box-shadow: 0 10px 20px -5px rgba(226, 125, 43, 0.15);
}
.cat-icon { font-size: 38px; margin-bottom: 8px; }
.cat-item h4 { margin: 0 0 4px 0; font-size: 15px; color: #0f172a; }
.cat-count { font-size: 12px; color: #64748b; }

/* Shop Grid */
.shop-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 24px;
}
.shop-card {
  background: #fff;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  overflow: hidden;
  transition: all 0.2s;
}
.shop-card:hover { border-color: #cbd5e1; box-shadow: 0 10px 20px -5px rgba(0,0,0,0.06); }
.shop-img-box { position: relative; width: 100%; height: 170px; }
.shop-img-box img { width: 100%; height: 100%; object-fit: cover; }
.dist-chip {
  position: absolute;
  bottom: 10px;
  left: 10px;
  background: rgba(15, 23, 42, 0.85);
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
}
.shop-body { padding: 18px; }
.shop-title-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.shop-title-row h4 { margin: 0; font-size: 16px; color: #0f172a; }
.rating { font-size: 13px; font-weight: 700; color: #eab308; }
.addr { font-size: 13px; color: #64748b; margin: 0 0 14px 0; }
.shop-footer { display: flex; justify-content: space-between; align-items: center; border-top: 1px solid #f1f5f9; padding-top: 12px; }
.time { font-size: 12px; color: #16a34a; font-weight: 600; }
.btn-order {
  background: #eff6ff;
  color: #2563eb;
  border: 1px solid #bfdbfe;
  padding: 6px 14px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 13px;
  cursor: pointer;
}
.btn-order:hover { background: #2563eb; color: #fff; }

/* Product Grid */
.product-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 20px;
}
.prod-card {
  background: #fff;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  flex-direction: column;
}
.prod-card:hover { transform: translateY(-4px); box-shadow: 0 10px 20px -5px rgba(0,0,0,0.06); }
.prod-img-box { position: relative; height: 180px; }
.prod-img-box img { width: 100%; height: 100%; object-fit: cover; }
.express-badge {
  position: absolute;
  top: 10px;
  left: 10px;
  background: #dc2626;
  color: #fff;
  font-size: 11px;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 6px;
}
.prod-body { padding: 16px; display: flex; flex-direction: column; flex-grow: 1; }
.store-name { font-size: 11px; color: #64748b; margin-bottom: 6px; }
.prod-name {
  font-size: 14px;
  color: #0f172a;
  margin: 0 0 12px 0;
  line-height: 1.4;
  height: 40px;
  overflow: hidden;
}
.price-row { display: flex; justify-content: space-between; align-items: flex-end; margin-top: auto; }
.price-group { display: flex; flex-direction: column; }
.price-current { font-size: 17px; font-weight: 800; color: #dc2626; }
.price-old { font-size: 12px; color: #94a3b8; text-decoration: line-through; }
.btn-add {
  background: #2563eb;
  color: #fff;
  border: none;
  padding: 6px 14px;
  border-radius: 8px;
  font-weight: 700;
  font-size: 13px;
  cursor: pointer;
}
.btn-add:hover { background: #1d4ed8; }
</style>
