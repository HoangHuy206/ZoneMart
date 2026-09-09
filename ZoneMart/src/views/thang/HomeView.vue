<script setup lang="ts">
import { useRouter } from 'vue-router';

const router = useRouter();

// 1. Dữ liệu 3 thẻ cổng thông tin vai trò (Role Portals)
const portalCards = [
  {
    id: 'buyer',
    badge: 'NGƯỜI TIÊU DÙNG',
    title: 'ĐĂNG KÝ NGƯỜI MUA',
    tagline: 'Nông sản sạch & tươi ngon',
    description:
      'Mua thực phẩm tươi, rau củ chuẩn VietGAP từ nhà vườn lân cận. Giao nhanh 10km.',
    btnText: 'KHÁM PHÁ CHỢ',
    btnRoute: '/products',
    iconType: 'user',
  },
  {
    id: 'seller',
    badge: 'NHÀ VƯỜN & TIỂU THƯƠNG',
    title: 'ĐĂNG KÝ BÁN HÀNG',
    tagline: 'Mở gian hàng số địa phương',
    description:
      'Tiếp cận khách hàng xung quanh, nhận đơn tức thì và tối ưu doanh số bán hàng.',
    btnText: 'ĐĂNG KÝ SHOP',
    btnRoute: '/register-seller',
    iconType: 'store',
  },
  {
    id: 'shipper',
    badge: 'ĐỐI TÁC GIAO HÀNG',
    title: 'ĐĂNG KÝ SHIPPER',
    tagline: 'Giao siêu tốc bán kính 10km',
    description:
      'Linh hoạt thời gian, nhận đơn khu vực gần nhà, thu nhập minh bạch nhận liền tay.',
    btnText: 'CỔNG TÀI XẾ',
    btnRoute: '/register-shipper',
    iconType: 'truck',
  },
];

// 2. Dữ liệu danh mục nổi bật (Featured Categories)
const featuredCategories = [
  {
    id: 1,
    name: 'RAU CỦ QUẢ TƯƠI',
    desc: 'Thu hoạch trong ngày, tươi chuẩn VietGAP',
    tag: 'Tươi sống',
    count: '150+ Mặt hàng',
    image:
      'https://images.unsplash.com/photo-1610348725531-843dff563e2c?auto=format&fit=crop&w=700&q=80',
  },
  {
    id: 2,
    name: 'ĐỒ THỦ CÔNG & CHẾ BIẾN',
    desc: 'Bánh mứt, gia vị & món ăn truyền thống',
    tag: 'Thủ công',
    count: '60+ Gian hàng',
    image:
      'https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=700&q=80',
  },
  {
    id: 3,
    name: 'ĐẶC SẢN ĐỊA PHƯƠNG',
    desc: 'Hương vị quê nhà đặc trưng các vùng miền',
    tag: 'Đặc sản',
    count: '85+ Sản phẩm',
    image:
      'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=700&q=80',
  },
  {
    id: 4,
    name: 'BÁCH HÓA & ĐỒ ĐÓNG GÓI',
    desc: 'Mật ong rừng, ngũ cốc, hạt sấy bổ dưỡng',
    tag: 'Bảo quản tốt',
    count: '95+ Mặt hàng',
    image:
      'https://images.unsplash.com/photo-1588964895597-cfccd6e2dbf9?auto=format&fit=crop&w=700&q=80',
  },
];

// 3. Sản phẩm tươi mới nổi bật trong bán kính 10km (Hot Products)
const featuredProducts = [
  {
    id: 1,
    name: 'Dâu Tây Đà Lạt Giống Nhật (Hộp 500g)',
    farm: 'Nông Trại Xanh Lạc Dương',
    distance: '1.8 km',
    deliveryTime: '25 phút',
    price: 95000,
    originalPrice: 120000,
    discount: '-21%',
    rating: 4.9,
    sold: 142,
    tag: 'Thu hoạch sáng nay',
    image:
      'https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 2,
    name: 'Xà Lách Mỡ Thủy Canh VietGAP (1kg)',
    farm: 'Vườn Rau Hữu Cơ Bác Ba',
    distance: '0.9 km',
    deliveryTime: '18 phút',
    price: 35000,
    originalPrice: 45000,
    discount: '-22%',
    rating: 5.0,
    sold: 389,
    tag: 'Bán chạy số 1',
    image:
      'https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 3,
    name: 'Bưởi Da Xanh Bến Tre Loại 1 (Trái 1.5kg)',
    farm: 'Vựa Bưởi Sáu Thảo Bến Tre',
    distance: '3.4 km',
    deliveryTime: '35 phút',
    price: 68000,
    originalPrice: 85000,
    discount: '-20%',
    rating: 4.8,
    sold: 96,
    tag: 'Đặc sản chuẩn gốc',
    image:
      'https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 4,
    name: 'Mật Ong Hoa Cà Phê Gia Lai (Chai 500ml)',
    farm: 'Hợp Tác Xã Nông Sản Cao Nguyên',
    distance: '2.5 km',
    deliveryTime: '30 phút',
    price: 135000,
    originalPrice: 165000,
    discount: '-18%',
    rating: 4.9,
    sold: 215,
    tag: 'Nguyên chất 100%',
    image:
      'https://images.unsplash.com/photo-1587049352851-8d4e89133924?auto=format&fit=crop&w=600&q=80',
  },
];

// 4. Quy trình hoạt động 4 bước siêu tốc của ZoneMart
const howItWorks = [
  {
    step: '01',
    title: 'Định Vị Tự Động',
    desc: 'Hệ thống tự động quét và gợi ý các nhà vườn, cửa hàng uy tín trong bán kính 10km quanh bạn.',
    icon: 'bi-geo-alt-fill',
  },
  {
    step: '02',
    title: 'Chọn Nông Sản Tươi',
    desc: 'Thoải mái lựa chọn nông sản thu hoạch trong ngày, rõ ràng xuất xứ và kiểm định VietGAP.',
    icon: 'bi-bag-check-fill',
  },
  {
    step: '03',
    title: 'Nhà Vườn Đóng Gói',
    desc: 'Chủ shop chuẩn bị đơn ngay lập tức, đóng gói kỹ lưỡng đảm bảo độ tươi mới tối đa.',
    icon: 'bi-box-seam-fill',
  },
  {
    step: '04',
    title: 'Shipper Giao Siêu Tốc',
    desc: 'Tài xế nhận đơn gần bạn, giao tận cửa nhà chỉ trong vòng 30 - 45 phút.',
    icon: 'bi-bicycle',
  },
];

// 5. Nhà vườn & Gian hàng tiêu biểu địa phương
const featuredVendors = [
  {
    id: 1,
    name: 'Vườn Rau Hữu Cơ Bác Ba',
    owner: 'Bác Ba (15 năm trồng rau sạch)',
    distance: '0.9 km',
    productsCount: '45+ loại rau củ',
    rating: 5.0,
    avatar:
      'https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80',
    cover:
      'https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 2,
    name: 'Nông Trại Xanh Lạc Dương',
    owner: 'HTX Nông Nghiệp Công Nghệ Cao',
    distance: '1.8 km',
    productsCount: '30+ loại củ quả & hoa quả',
    rating: 4.9,
    avatar:
      'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80',
    cover:
      'https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 3,
    name: 'Vựa Trái Cây Sáu Thảo Bến Tre',
    owner: 'Nhà vườn Sáu Thảo',
    distance: '3.4 km',
    productsCount: '25+ đặc sản miệt vườn',
    rating: 4.8,
    avatar:
      'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80',
    cover:
      'https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=600&q=80',
  },
];

// 6. Đánh giá thực tế từ cộng đồng
const testimonials = [
  {
    id: 1,
    name: 'Chị Mai Lan',
    role: 'Khách mua hàng (Quận 7, TP.HCM)',
    avatar:
      'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=150&q=80',
    comment:
      'Rau xà lách và cà chua đặt lúc 9h30 mà 10h10 shipper đã bấm chuông giao tận cửa. Rau còn đọng sương tươi rói, không bị dập nát như đặt sàn xa.',
    rating: 5,
  },
  {
    id: 2,
    name: 'Bác Ba',
    role: 'Chủ Vườn Rau Bác Ba',
    avatar:
      'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=150&q=80',
    comment:
      'Nhờ có ZoneMart mà rau vườn nhà tôi thu hoạch sáng sớm là bán hết sạch cho bà con xung quanh. Không lo bị thương lái ép giá hay ứ đọng.',
    rating: 5,
  },
  {
    id: 3,
    name: 'Anh Minh Tuấn',
    role: 'Tài xế Shipper ZoneMart',
    avatar:
      'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=150&q=80',
    comment:
      'Chạy đơn trong bán kính 10km quanh phường rất quen đường, đơn nổ liên tục từ các tiệm gần nhà, giao nhanh nên được khách đánh giá 5 sao suốt.',
    rating: 5,
  },
];

// 7. Cam kết đặc trưng của ZoneMart
const highlights = [
  {
    title: 'Giao nhanh trong 10km',
    desc: 'Định vị thông minh, thực phẩm đến tay bạn trong 30-45 phút.',
  },
  {
    title: 'Nông sản tận gốc',
    desc: 'Nguồn gốc minh bạch từ nhà vườn và tiểu thương địa phương uy tín.',
  },
  {
    title: 'Hỗ trợ kinh tế địa phương',
    desc: 'Đồng hành cùng nhà vườn số hóa và tạo việc làm cho tài xế khu vực.',
  },
];

const formatPrice = (value: number) => {
  return value.toLocaleString('vi-VN') + ' đ';
};

const goToRoute = (path: string) => {
  router.push(path);
};
</script>

<template>
  <div class="homepage-container">
    <!-- ==========================================================
         1. FIRST FOLD: HERO BANNER & 3 THẺ VAI TRÒ
         ========================================================== -->
    <div class="first-fold-wrapper">
      <!-- HERO BANNER -->
      <section class="hero-section">
        <div class="hero-card">
          <div class="hero-overlay"></div>
          <div class="hero-content">
            <span class="hero-subheading">Nông Sản & Đặc Sản Địa Phương</span>
            <h1 class="hero-title">Chợ Địa Phương – Nông Dân Thân Thiện</h1>
            <p class="hero-description">
              Cầu nối trực tiếp giữa nhà vườn địa phương và gia đình bạn. Tươi
              ngon mỗi ngày, hỗ trợ cộng đồng nông sản với dịch vụ giao hàng
              nhanh trong bán kính 10km.
            </p>
            <div class="hero-actions">
              <button class="btn-primary-warm" @click="goToRoute('/products')">
                KHÁM PHÁ CHỢ
              </button>
              <button class="btn-secondary-warm" @click="goToRoute('/map')">
                XEM TRÊN BẢN ĐỒ
              </button>
            </div>
          </div>

          <!-- Họa tiết trang trí thủ công góc phải -->
          <div class="hero-decoration">
            <svg
              class="deco-svg"
              viewBox="0 0 200 200"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
            >
              <circle
                cx="100"
                cy="100"
                r="80"
                stroke="rgba(255,255,255,0.2)"
                stroke-width="2"
                stroke-dasharray="6 6"
              />
              <path
                d="M60 130 C70 90, 130 90, 140 130"
                stroke="rgba(255,255,255,0.4)"
                stroke-width="3"
                stroke-linecap="round"
              />
              <circle cx="100" cy="75" r="16" fill="rgba(255,255,255,0.5)" />
            </svg>
          </div>
        </div>
      </section>

      <!-- 3 THẺ ROLE PORTALS ĐÈ LÊN CHÂN HERO THEO MẪU MOCKUP -->
      <section class="portals-section">
        <div class="portals-grid">
          <div v-for="card in portalCards" :key="card.id" class="portal-card">
            <div class="portal-icon-box">
              <svg
                v-if="card.iconType === 'user'"
                viewBox="0 0 24 24"
                width="24"
                height="24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              <svg
                v-else-if="card.iconType === 'store'"
                viewBox="0 0 24 24"
                width="24"
                height="24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                <polyline points="9 22 9 12 15 12 15 22"></polyline>
              </svg>
              <svg
                v-else-if="card.iconType === 'truck'"
                viewBox="0 0 24 24"
                width="24"
                height="24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <rect x="1" y="3" width="15" height="13"></rect>
                <polygon points="16 8 20 8 23 11 23 16 16 16 8"></polygon>
                <circle cx="5.5" cy="18.5" r="2.5"></circle>
                <circle cx="18.5" cy="18.5" r="2.5"></circle>
              </svg>
            </div>

            <h3 class="portal-title">{{ card.title }}</h3>
            <p class="portal-tagline">{{ card.tagline }}</p>
            <p class="portal-desc">{{ card.description }}</p>

            <button class="portal-btn" @click="goToRoute(card.btnRoute)">
              {{ card.btnText }}
            </button>
          </div>
        </div>
      </section>
    </div>

    <!-- ==========================================================
         2. DANH MỤC NỔI BẬT (FEATURED CATEGORIES)
         ========================================================== -->
    <section class="section-container">
      <div class="section-header">
        <div class="header-left">
          <h2 class="section-title">Danh Mục Nổi Bật</h2>
          <p class="section-subtitle">
            Khám phá các mặt hàng địa phương tươi ngon và đặc sắc nhất
          </p>
        </div>
        <router-link to="/products" class="view-all-link">
          Xem Tất Cả Danh Mục ➔
        </router-link>
      </div>

      <div class="categories-grid">
        <div
          v-for="cat in featuredCategories"
          :key="cat.id"
          class="category-card"
          @click="goToRoute('/products')"
        >
          <div class="category-img-wrap">
            <img
              :src="cat.image"
              :alt="cat.name"
              class="category-img"
              loading="lazy"
            />
            <span class="category-tag">{{ cat.tag }}</span>
          </div>
          <div class="category-info">
            <h4 class="category-name">{{ cat.name }}</h4>
            <p class="category-desc">{{ cat.desc }}</p>
            <div class="category-footer">
              <span class="category-count">{{ cat.count }}</span>
              <span class="category-arrow">Khám phá ➔</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         3. SẢN PHẨM BÁN CHẠY TRONG BÁN KÍNH 10KM (HOT PRODUCTS)
         ========================================================== -->
    <section class="section-container">
      <div class="section-header">
        <div class="header-left">
          <div class="badge-tag-hot"><i class="bi bi-fire me-1" aria-hidden="true"></i> GIAO HỎA TỐC 10KM</div>
          <h2 class="section-title">Nông Sản Tươi Mới Hôm Nay</h2>
          <p class="section-subtitle">
            Thu hoạch trực tiếp từ các nhà vườn lân cận, giao tận nhà trong
            30-45 phút
          </p>
        </div>
        <router-link to="/products" class="view-all-link">
          Xem Tất Cả Sản Phẩm ➔
        </router-link>
      </div>

      <div class="products-grid">
        <div
          v-for="item in featuredProducts"
          :key="item.id"
          class="product-card"
          @click="goToRoute('/products')"
        >
          <!-- Ảnh sản phẩm kèm Tag khuyến mãi -->
          <div class="product-img-wrap">
            <img
              :src="item.image"
              :alt="item.name"
              class="product-img"
              loading="lazy"
            />
            <span class="product-discount-tag">{{ item.discount }}</span>
            <span class="product-status-tag">{{ item.tag }}</span>
          </div>

          <!-- Thông tin sản phẩm -->
          <div class="product-body">
            <div class="product-location-row">
              <span class="loc-badge"><i class="bi bi-geo-alt-fill"></i> {{ item.distance }}</span>
              <span class="time-badge"><i class="bi bi-lightning-charge-fill"></i> {{ item.deliveryTime }}</span>
            </div>
            <h4 class="product-title">{{ item.name }}</h4>
            <p class="product-farm"><i class="bi bi-shop"></i> {{ item.farm }}</p>

            <div class="product-rating-row">
              <span class="stars"><i class="bi bi-star-fill text-warning"></i> {{ item.rating }}</span>
              <span class="sold-count">Đã bán {{ item.sold }}</span>
            </div>

            <div class="product-price-row">
              <div class="price-box">
                <span class="current-price">{{ formatPrice(item.price) }}</span>
                <span class="original-price">{{
                  formatPrice(item.originalPrice)
                }}</span>
              </div>
              <button class="btn-add-cart" title="Xem chi tiết & Mua" aria-label="Chọn mua sản phẩm">
                <i class="bi bi-bag-plus-fill"></i> Chọn Mua
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         4. QUY TRÌNH HOẠT ĐỘNG 4 BƯỚC (HOW ZONEMART WORKS)
         ========================================================== -->
    <section class="section-container how-it-works-section">
      <div class="how-header">
        <span class="how-badge">MÔ HÌNH VẬN HÀNH THÔNG MINH</span>
        <h2 class="section-title text-center">
          ZoneMart Hoạt Động Như Thế Nào?
        </h2>
        <p class="section-subtitle text-center">
          Giao hàng siêu tốc trong bán kính 10km nhờ công nghệ kết nối tự động
          giữa Người Mua, Nhà Vườn và Tài Xế
        </p>
      </div>

      <div class="how-steps-grid">
        <div v-for="step in howItWorks" :key="step.step" class="step-card">
          <div class="step-number">{{ step.step }}</div>
          <div class="step-icon">
            <i class="bi" :class="step.icon" aria-hidden="true"></i>
          </div>
          <h4 class="step-title">{{ step.title }}</h4>
          <p class="step-desc">{{ step.desc }}</p>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         5. NHÀ VƯỜN & GIAN HÀNG TIÊU BIỂU ĐỊA PHƯƠNG
         ========================================================== -->
    <section class="section-container">
      <div class="section-header">
        <div class="header-left">
          <h2 class="section-title">Gian Hàng Đối Tác Tiêu Biểu</h2>
          <p class="section-subtitle">
            Ghé thăm những nhà vườn hữu cơ và vựa trái cây chuẩn chất lượng
            quanh bạn
          </p>
        </div>
        <router-link to="/products" class="view-all-link">
          <span>Xem Tất Cả Gian Hàng</span>
          <i class="bi bi-arrow-right"></i>
        </router-link>
      </div>

      <div class="vendors-grid">
        <div
          v-for="v in featuredVendors"
          :key="v.id"
          class="vendor-card"
          @click="goToRoute('/products')"
        >
          <div class="vendor-cover-wrap">
            <img
              :src="v.cover"
              :alt="v.name"
              class="vendor-cover"
              loading="lazy"
            />
            <div class="vendor-avatar-wrap">
              <img :src="v.avatar" :alt="v.owner" class="vendor-avatar" />
            </div>
          </div>
          <div class="vendor-body">
            <h4 class="vendor-name">{{ v.name }}</h4>
            <p class="vendor-owner">{{ v.owner }}</p>
            <div class="vendor-meta-row">
              <span><i class="bi bi-geo-alt-fill text-danger"></i> Cách bạn {{ v.distance }}</span>
              <span><i class="bi bi-star-fill text-warning"></i> {{ v.rating }}</span>
            </div>
            <p class="vendor-stock"><i class="bi bi-box-seam text-primary"></i> {{ v.productsCount }}</p>
            <button class="vendor-btn">
              <span>Ghé Thăm Gian Hàng</span>
              <i class="bi bi-arrow-right"></i>
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         6. ĐÁNH GIÁ THỰC TẾ TỪ CỘNG ĐỒNG (TESTIMONIALS)
         ========================================================== -->
    <section class="section-container testimonials-section">
      <div class="section-header text-center-header">
        <span class="how-badge">Ý KIẾN KHÁCH HÀNG</span>
        <h2 class="section-title">Cộng Đồng Nói Gì Về ZoneMart?</h2>
        <p class="section-subtitle">
          Trải nghiệm mua sắm sạch, bán hàng số và giao nhận hỏa tốc từ người
          dùng thực tế
        </p>
      </div>

      <div class="testimonials-grid">
        <div v-for="t in testimonials" :key="t.id" class="testimonial-card">
          <div class="stars-row">
            <i v-for="n in t.rating" :key="n" class="bi bi-star-fill text-warning"></i>
          </div>
          <p class="testimonial-quote">“{{ t.comment }}”</p>
          <div class="testimonial-author">
            <img :src="t.avatar" :alt="t.name" class="author-avatar" />
            <div>
              <h5 class="author-name">{{ t.name }}</h5>
              <span class="author-role">{{ t.role }}</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         7. BANNER KÊU GỌI HỢP TÁC (CTA BANNER)
         ========================================================== -->
    <section class="section-container">
      <div class="partner-cta-card">
        <div class="partner-cta-content">
          <span class="cta-mini-tag">ĐỒNG HÀNH CÙNG ZONEMART</span>
          <h2 class="cta-title">
            Bạn Là Nhà Vườn Hoặc Muốn Tăng Thu Nhập Cùng Shipper?
          </h2>
          <p class="cta-desc">
            ZoneMart miễn phí 100% phí mở gian hàng cho tiểu thương và cung cấp
            luồng đơn hàng liên tục trong bán kính 10km cho tài xế.
          </p>
          <div class="cta-btn-group">
            <button
              class="btn-partner-seller"
              @click="goToRoute('/register-seller')"
            >
              <i class="bi bi-shop me-2" aria-hidden="true"></i> ĐĂNG KÝ BÁN HÀNG NGAY
            </button>
            <button class="btn-partner-shipper" @click="goToRoute('/register-shipper')">
              <i class="bi bi-bicycle me-2" aria-hidden="true"></i> GIA NHẬP ĐỘI TÀI XẾ
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         8. ĐIỂM NHẤN CAM KẾT 10KM
         ========================================================== -->
    <section class="section-container">
      <div class="highlights-card">
        <div
          class="highlight-item"
          v-for="(item, idx) in highlights"
          :key="idx"
        >
          <div class="highlight-bullet">0{{ idx + 1 }}</div>
          <div class="highlight-text">
            <h4>{{ item.title }}</h4>
            <p>{{ item.desc }}</p>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* ==========================================================
   HOMEPAGE WARM HUMANIST & FULL-WIDTH STYLES
   Màu chủ đạo: Terracotta/Cam đất (#D85A2A), Nền kem ấm (#FAF7F2)
   ========================================================== */

.homepage-container {
  width: 100%;
  min-height: calc(100vh - 70px);
  background-color: #faf7f2;
  padding: 12px clamp(16px, 3.5vw, 48px) 60px clamp(16px, 3.5vw, 48px);
  box-sizing: border-box;
  font-family:
    -apple-system, BlinkMacSystemFont, 'Plus Jakarta Sans', 'Segoe UI', Roboto,
    sans-serif;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  color: #2b1b14;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

/* SECTION CHUNG */
.section-container {
  margin-bottom: 52px;
  width: 100%;
}
.section-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 22px;
}
.section-title {
  font-family: 'Georgia', 'Merriweather', 'Playfair Display', serif;
  font-size: clamp(22px, 2.3vw, 28px);
  font-weight: 700;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  font-size: clamp(22px, 2.3vw, 30px);
  font-weight: 800;
  color: #2b1b14;
  margin: 0 0 4px 0;
  letter-spacing: -0.3px;
  letter-spacing: -0.4px;
}
.section-subtitle {
  font-size: 13.5px;
  color: #7b6960;
  margin: 0;
}
.view-all-link {
  font-size: 13.5px;
  font-weight: 700;
  color: #d85a2a;
  text-decoration: none;
  white-space: nowrap;
  transition: transform 0.2s;
}
.view-all-link:hover {
  text-decoration: underline;
  transform: translateX(3px);
}

/* 1. FIRST FOLD: HERO BANNER */
.first-fold-wrapper {
  display: flex;
  flex-direction: column;
  margin-bottom: 48px;
}
.hero-section {
  position: relative;
  width: 100%;
}
.hero-card {
  position: relative;
  min-height: 290px;
  border-radius: 18px;
  overflow: hidden;
  background-image: url('https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=1600&q=80');
  background-size: cover;
  background-position: center 35%;
  display: flex;
  align-items: center;
  box-shadow: 0 10px 28px -6px rgba(43, 27, 20, 0.14);
  padding-bottom: 50px;
}
.hero-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(
    90deg,
    rgba(34, 18, 12, 0.9) 0%,
    rgba(60, 29, 17, 0.78) 45%,
    rgba(216, 90, 42, 0.38) 100%
  );
  z-index: 1;
}
.hero-content {
  position: relative;
  z-index: 2;
  max-width: 680px;
  padding: 28px clamp(20px, 3.5vw, 40px);
  color: #ffffff;
}
.hero-subheading {
  display: inline-block;
  background: rgba(255, 255, 255, 0.15);
  backdrop-filter: blur(6px);
  color: #ffe0d0;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.8px;
  text-transform: uppercase;
  padding: 4px 12px;
  border-radius: 20px;
  margin-bottom: 10px;
  border: 1px solid rgba(255, 255, 255, 0.22);
}
.hero-title {
  font-family: 'Georgia', 'Merriweather', 'Playfair Display', serif;
  font-size: clamp(22px, 2.6vw, 36px);
  font-weight: 700;
  line-height: 1.2;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  font-size: clamp(24px, 2.8vw, 38px);
  font-weight: 850;
  line-height: 1.25;
  margin: 0 0 10px 0;
  letter-spacing: -0.4px;
  letter-spacing: -0.5px;
  color: #fff9f5;
}
.hero-description {
  font-size: clamp(13px, 1vw, 15px);
  line-height: 1.5;
  color: #f7e6dc;
  margin: 0 0 18px 0;
}
.hero-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}
.btn-primary-warm {
  background: #d85a2a;
  color: #ffffff;
  border: none;
  font-weight: 700;
  font-size: 13px;
  letter-spacing: 0.4px;
  padding: 10px 22px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 4px 14px rgba(216, 90, 42, 0.35);
}
.btn-primary-warm:hover {
  background: #ea6431;
  transform: translateY(-2px);
}
.btn-secondary-warm {
  background: rgba(255, 255, 255, 0.12);
  backdrop-filter: blur(6px);
  color: #ffffff;
  border: 1.5px solid rgba(255, 255, 255, 0.45);
  font-weight: 600;
  font-size: 13px;
  padding: 9px 20px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.2s ease;
}
.btn-secondary-warm:hover {
  background: rgba(255, 255, 255, 0.25);
  border-color: #ffffff;
}
.hero-decoration {
  position: absolute;
  right: 24px;
  top: 24px;
  z-index: 2;
  width: 130px;
  height: 130px;
  opacity: 0.35;
  pointer-events: none;
}
.deco-svg {
  width: 100%;
  height: 100%;
}

/* 3 ROLE PORTAL CARDS */
.portals-section {
  position: relative;
  z-index: 10;
  margin-top: -45px;
  width: 100%;
}
.portals-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: clamp(14px, 2vw, 24px);
}
.portal-card {
  background: #ffffff;
  border: 1.5px solid #eed9cc;
  border-radius: 16px;
  padding: 20px 18px 18px 18px;
  text-align: center;
  box-shadow: 0 8px 24px -4px rgba(43, 27, 20, 0.08);
  display: flex;
  flex-direction: column;
  align-items: center;
  transition: all 0.25s ease;
}
.portal-card:hover {
  transform: translateY(-4px);
  border-color: #d85a2a;
  box-shadow: 0 14px 30px -4px rgba(216, 90, 42, 0.18);
}
.portal-icon-box {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: #fdf1ea;
  color: #d85a2a;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
  border: 1px solid #f9ded0;
}
.portal-title {
  font-size: 15px;
  font-weight: 800;
  letter-spacing: 0.4px;
  color: #2b1b14;
  margin: 0 0 4px 0;
  text-transform: uppercase;
}
.portal-tagline {
  font-size: 12px;
  font-weight: 600;
  color: #d85a2a;
  margin: 0 0 8px 0;
}
.portal-desc {
  font-size: 13px;
  line-height: 1.45;
  color: #6b5a51;
  margin: 0 0 16px 0;
  flex-grow: 1;
}
.portal-btn {
  width: 100%;
  background: #d85a2a;
  color: #ffffff;
  border: none;
  font-weight: 700;
  font-size: 12px;
  letter-spacing: 0.4px;
  padding: 10px 14px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
}
.portal-btn:hover {
  background: #bf4a1f;
  box-shadow: 0 4px 12px rgba(216, 90, 42, 0.3);
}

/* 2. DANH MỤC NỔI BẬT */
.categories-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: clamp(12px, 1.6vw, 20px);
}
.category-card {
  background: #ffffff;
  border-radius: 14px;
  overflow: hidden;
  border: 1px solid #ebdcd3;
  cursor: pointer;
  box-shadow: 0 4px 14px -2px rgba(43, 27, 20, 0.05);
  transition: all 0.25s ease;
  display: flex;
  flex-direction: column;
}
.category-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 22px -4px rgba(216, 90, 42, 0.15);
  border-color: #d85a2a;
}
.category-img-wrap {
  position: relative;
  height: 150px;
  overflow: hidden;
  background: #fdf3ed;
}
.category-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.35s ease;
}
.category-card:hover .category-img {
  transform: scale(1.06);
}
.category-tag {
  position: absolute;
  top: 8px;
  left: 8px;
  background: rgba(43, 27, 20, 0.72);
  backdrop-filter: blur(4px);
  color: #ffffff;
  font-size: 11px;
  font-weight: 600;
  padding: 3px 9px;
  border-radius: 20px;
}
.category-info {
  padding: 14px;
  display: flex;
  flex-direction: column;
  flex-grow: 1;
}
.category-name {
  font-size: 14px;
  font-weight: 700;
  color: #2b1b14;
  margin: 0 0 4px 0;
  line-height: 1.3;
}
.category-desc {
  font-size: 12px;
  color: #7b6960;
  margin: 0 0 12px 0;
  line-height: 1.4;
  flex-grow: 1;
}
.category-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-top: 1px dashed #eddcd3;
  padding-top: 8px;
}
.category-count {
  font-size: 12px;
  font-weight: 600;
  color: #d85a2a;
}
.category-arrow {
  font-size: 11px;
  font-weight: 700;
  color: #8f796f;
  transition: color 0.2s;
}
.category-card:hover .category-arrow {
  color: #d85a2a;
}

/* 3. SẢN PHẨM BÁN CHẠY (HOT PRODUCTS) */
.badge-tag-hot {
  display: inline-block;
  font-size: 11px;
  font-weight: 800;
  color: #d85a2a;
  background: #fdf1ea;
  border: 1px solid #f9d8c8;
  padding: 3px 10px;
  border-radius: 20px;
  margin-bottom: 6px;
}
.products-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: clamp(14px, 1.8vw, 22px);
}
.product-card {
  background: #ffffff;
  border: 1px solid #eadad0;
  border-radius: 14px;
  overflow: hidden;
  box-shadow: 0 4px 16px -2px rgba(43, 27, 20, 0.06);
  cursor: pointer;
  transition: all 0.25s ease;
  display: flex;
  flex-direction: column;
}
.product-card:hover {
  transform: translateY(-4px);
  border-color: #d85a2a;
  box-shadow: 0 12px 28px -4px rgba(216, 90, 42, 0.16);
}
.product-img-wrap {
  position: relative;
  height: 170px;
  overflow: hidden;
  background: #fcf4ee;
}
.product-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.35s ease;
}
.product-card:hover .product-img {
  transform: scale(1.06);
}
.product-discount-tag {
  position: absolute;
  top: 8px;
  left: 8px;
  background: #d85a2a;
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 3px 8px;
  border-radius: 6px;
}
.product-status-tag {
  position: absolute;
  bottom: 8px;
  left: 8px;
  background: rgba(34, 18, 12, 0.72);
  backdrop-filter: blur(4px);
  color: #ffffff;
  font-size: 10.5px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 12px;
}
.product-body {
  padding: 14px;
  display: flex;
  flex-direction: column;
  flex-grow: 1;
}
.product-location-row {
  display: flex;
  justify-content: space-between;
  font-size: 11.5px;
  font-weight: 600;
  margin-bottom: 6px;
}
.loc-badge {
  color: #d85a2a;
  background: #fdf2eb;
  padding: 2px 7px;
  border-radius: 4px;
}
.time-badge {
  color: #2b7a4b;
  background: #eef8f2;
  padding: 2px 7px;
  border-radius: 4px;
}
.product-title {
  font-size: 14px;
  font-weight: 700;
  color: #2b1b14;
  margin: 0 0 4px 0;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  min-height: 38px;
}
.product-farm {
  font-size: 12px;
  color: #7b6960;
  margin: 0 0 10px 0;
}
.product-rating-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11.5px;
  color: #8c796f;
  margin-bottom: 12px;
}
.stars {
  font-weight: 700;
  color: #d85a2a;
}
.product-price-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  border-top: 1px dashed #eddcd3;
  padding-top: 10px;
}
.price-box {
  display: flex;
  flex-direction: column;
}
.current-price {
  font-size: 15px;
  font-weight: 800;
  color: #d85a2a;
}
.original-price {
  font-size: 11.5px;
  text-decoration: line-through;
  color: #a39187;
}
.btn-add-cart {
  background: #2b1b14;
  color: #ffffff;
  border: none;
  font-size: 11.5px;
  font-weight: 700;
  padding: 7px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-add-cart:hover {
  background: #d85a2a;
}

/* 4. HOW IT WORKS */
.how-it-works-section {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 18px;
  padding: 36px clamp(18px, 3vw, 36px);
  box-shadow: 0 6px 20px -2px rgba(43, 27, 20, 0.04);
}
.how-header {
  text-align: center;
  margin-bottom: 30px;
}
.how-badge {
  display: inline-block;
  font-size: 11.5px;
  font-weight: 800;
  letter-spacing: 0.8px;
  color: #d85a2a;
  background: #fdf2eb;
  padding: 4px 12px;
  border-radius: 20px;
  margin-bottom: 8px;
}
.text-center {
  text-align: center;
}
.how-steps-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
}
.step-card {
  position: relative;
  background: #fffaf7;
  border: 1px solid #f2e2d8;
  border-radius: 14px;
  padding: 24px 18px;
  text-align: center;
  transition: transform 0.2s;
}
.step-card:hover {
  transform: translateY(-3px);
  border-color: #d85a2a;
}
.step-number {
  position: absolute;
  top: 10px;
  right: 14px;
  font-family: 'Georgia', serif;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', sans-serif;
  font-size: 18px;
  font-weight: 800;
  color: #e8d0c2;
}
.step-icon {
  font-size: 36px;
  margin-bottom: 12px;
}
.step-title {
  font-size: 15px;
  font-weight: 700;
  color: #2b1b14;
  margin: 0 0 6px 0;
}
.step-desc {
  font-size: 12.5px;
  color: #6b5a51;
  line-height: 1.5;
  margin: 0;
}

/* 5. GIAN HÀNG TIÊU BIỂU */
.vendors-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: clamp(14px, 2vw, 24px);
}
.vendor-card {
  background: #ffffff;
  border: 1px solid #eadad0;
  border-radius: 14px;
  overflow: hidden;
  box-shadow: 0 4px 16px -2px rgba(43, 27, 20, 0.05);
  cursor: pointer;
  transition: all 0.25s ease;
}
.vendor-card:hover {
  transform: translateY(-4px);
  border-color: #d85a2a;
  box-shadow: 0 12px 28px -4px rgba(216, 90, 42, 0.16);
}
.vendor-cover-wrap {
  position: relative;
  height: 120px;
}
.vendor-cover {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.vendor-avatar-wrap {
  position: absolute;
  bottom: -24px;
  left: 18px;
  width: 52px;
  height: 52px;
  border-radius: 50%;
  border: 2px solid #ffffff;
  overflow: hidden;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
}
.vendor-avatar {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.vendor-body {
  padding: 32px 18px 18px 18px;
}
.vendor-name {
  font-size: 15px;
  font-weight: 800;
  color: #2b1b14;
  margin: 0 0 2px 0;
}
.vendor-owner {
  font-size: 12px;
  color: #7b6960;
  margin: 0 0 10px 0;
}
.vendor-meta-row {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  font-weight: 600;
  color: #d85a2a;
  margin-bottom: 6px;
}
.vendor-stock {
  font-size: 12px;
  color: #6b5a51;
  margin: 0 0 14px 0;
}
.vendor-btn {
  width: 100%;
  background: #fdf2eb;
  color: #d85a2a;
  border: 1px solid #f6d8c8;
  font-size: 12px;
  font-weight: 700;
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}
.vendor-card:hover .vendor-btn {
  background: #d85a2a;
  color: #ffffff;
}

/* 6. TESTIMONIALS */
.testimonials-section {
  background: #fffaf6;
  border: 1px solid #ebdcd3;
  border-radius: 18px;
  padding: 36px clamp(18px, 3vw, 36px);
}
.text-center-header {
  flex-direction: column;
  align-items: center;
  text-align: center;
  margin-bottom: 28px;
}
.testimonials-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}
.testimonial-card {
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 14px;
  padding: 22px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 4px 14px -2px rgba(43, 27, 20, 0.04);
}
.stars-row {
  margin-bottom: 10px;
  font-size: 13px;
}
.testimonial-quote {
  font-size: 13px;
  line-height: 1.6;
  color: #55443d;
  font-style: italic;
  margin: 0 0 16px 0;
  flex-grow: 1;
}
.testimonial-author {
  display: flex;
  align-items: center;
  gap: 12px;
  border-top: 1px dashed #eddcd3;
  padding-top: 12px;
}
.author-avatar {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  object-fit: cover;
}
.author-name {
  font-size: 13.5px;
  font-weight: 700;
  color: #2b1b14;
  margin: 0 0 2px 0;
}
.author-role {
  font-size: 11px;
  color: #8c796f;
}

/* 7. CTA PARTNER BANNER */
.partner-cta-card {
  background: linear-gradient(135deg, #2b1b14 0%, #3e2216 60%, #d85a2a 150%);
  border-radius: 18px;
  padding: 40px clamp(20px, 4vw, 50px);
  color: #ffffff;
  box-shadow: 0 12px 30px -4px rgba(43, 27, 20, 0.2);
}
.partner-cta-content {
  max-width: 800px;
}
.cta-mini-tag {
  display: inline-block;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.8px;
  color: #ffd6c2;
  background: rgba(255, 255, 255, 0.14);
  padding: 3px 12px;
  border-radius: 20px;
  margin-bottom: 12px;
}
.cta-title {
  font-family: 'Georgia', 'Merriweather', serif;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  font-size: clamp(20px, 2.2vw, 28px);
  font-weight: 700;
  font-weight: 800;
  color: #fff9f5;
  margin: 0 0 12px 0;
  line-height: 1.3;
}
.cta-desc {
  font-size: 14px;
  line-height: 1.6;
  color: #ebd8cd;
  margin: 0 0 24px 0;
}
.cta-btn-group {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
}
.btn-partner-seller {
  background: #d85a2a;
  color: #ffffff;
  border: none;
  font-weight: 700;
  font-size: 13px;
  padding: 12px 24px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 14px rgba(216, 90, 42, 0.4);
}
.btn-partner-seller:hover {
  background: #ea6431;
  transform: translateY(-2px);
}
.btn-partner-shipper {
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
  border: 1.5px solid rgba(255, 255, 255, 0.4);
  font-weight: 600;
  font-size: 13px;
  padding: 11px 22px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-partner-shipper:hover {
  background: rgba(255, 255, 255, 0.25);
  border-color: #ffffff;
}

/* 8. HIGHLIGHTS */
.highlights-card {
  background: #fffaf7;
  border: 1.5px solid #f2dfd5;
  border-radius: 14px;
  padding: 20px 24px;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}
.highlight-item {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}
.highlight-bullet {
  font-family: 'Georgia', serif;
  font-size: 20px;
  font-weight: 700;
  font-family: 'Plus Jakarta Sans', 'Be Vietnam Pro', sans-serif;
  font-size: 18px;
  font-weight: 800;
  color: #d85a2a;
  background: #fbeee7;
  width: 38px;
  height: 38px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.highlight-text h4 {
  font-size: 14px;
  font-weight: 700;
  margin: 0 0 3px 0;
  color: #2b1b14;
}
.highlight-text p {
  font-size: 12px;
  line-height: 1.45;
  color: #6d5b52;
  margin: 0;
}

/* ==========================================================
   RESPONSIVE TOÀN DIỆN
   ========================================================== */

@media (max-width: 1024px) {
  .hero-card {
    min-height: 270px;
  }
  .portals-section {
    margin-top: -30px;
  }
  .categories-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .products-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .how-steps-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 16px;
  }
  .vendors-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .testimonials-grid {
    grid-template-columns: 1fr;
  }
  .highlights-card {
    grid-template-columns: 1fr;
    gap: 16px;
  }
}

@media (max-width: 768px) {
  .homepage-container {
    padding: 10px 12px 40px 12px;
  }
  .first-fold-wrapper {
    margin-bottom: 24px;
  }
  .hero-card {
    min-height: auto;
    padding-bottom: 20px;
    border-radius: 14px;
  }
  .hero-content {
    padding: 20px 14px;
  }
  .hero-actions {
    flex-direction: column;
    gap: 8px;
  }
  .btn-primary-warm,
  .btn-secondary-warm {
    width: 100%;
    text-align: center;
  }
  .hero-decoration {
    display: none;
  }
  .portals-section {
    margin-top: 14px;
    margin-bottom: 20px;
  }
  .portals-grid {
    grid-template-columns: 1fr;
    gap: 12px;
  }
  .portal-card {
    padding: 18px 14px;
  }
  .categories-grid {
    grid-template-columns: 1fr;
    gap: 12px;
  }
  .products-grid {
    grid-template-columns: 1fr;
    gap: 14px;
  }
  .how-steps-grid {
    grid-template-columns: 1fr;
    gap: 12px;
  }
  .vendors-grid {
    grid-template-columns: 1fr;
  }
  .section-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 6px;
  }
  .btn-partner-seller,
  .btn-partner-shipper {
    width: 100%;
    text-align: center;
  }
}
</style>
