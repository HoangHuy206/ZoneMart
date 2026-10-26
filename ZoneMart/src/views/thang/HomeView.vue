<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useCart } from '../../composables/useCart';
import { useToast } from '../../composables/useToast';
import { useProductCatalog } from '../../composables/useProductCatalog';
import { useAuth } from '../../composables/useAuth';

const router = useRouter();
const auth = useAuth();
const cart = useCart();
const toast = useToast();
const catalog = useProductCatalog();

const toastMessage = ref('');
let toastTimer: any = null;
const showToast = (msg: string) => {
  toastMessage.value = msg;
  if (toastTimer) clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    toastMessage.value = '';
  }, 2500);
  toast.success(msg, 'Giỏ Hàng ZoneMart');
};

let observer: IntersectionObserver | null = null;

// ==========================================================
// HIỆU ỨNG TƯƠNG TÁC 3D PARALLAX CHO SẠP CHỢ DÂN SINH
// ==========================================================
const heroRotateX = ref(0);
const heroRotateY = ref(0);

const onHeroMouseMove = (e: MouseEvent) => {
  if (typeof window !== 'undefined' && window.innerWidth < 1024) return;
  const target = e.currentTarget as HTMLElement;
  if (!target) return;
  const rect = target.getBoundingClientRect();
  const x = (e.clientX - rect.left) / rect.width - 0.5;
  const y = (e.clientY - rect.top) / rect.height - 0.5;
  heroRotateY.value = +(x * 14).toFixed(2);
  heroRotateX.value = +(-y * 12).toFixed(2);
};

const resetHeroTilt = () => {
  heroRotateX.value = 0;
  heroRotateY.value = 0;
};

// Dữ liệu "Nhịp sống chợ dân sinh" chạy trực tiếp
const liveMarketActivities = [
  {
    id: 1,
    role: 'buyer',
    icon: 'bi-basket2-fill',
    text: 'Chị Thu Hà (cách 1.1km) vừa mua 2 bó cải ngọt & 1kg cà chua tại Vườn Rau Bác Ba',
    time: 'Vừa xong',
  },
  {
    id: 2,
    role: 'seller',
    icon: 'bi-shop-window',
    text: 'Sạp Trái Cây Cô Tư vừa lên kệ 15 phần Xoài Cát Hòa Lộc chín cây tươi rói',
    time: '1 phút trước',
  },
  {
    id: 3,
    role: 'shipper',
    icon: 'bi-scooter',
    text: 'Tài xế Minh Tuấn đang giao đơn nông sản #ZM892, dự kiến tới bếp sau 12 phút',
    time: '2 phút trước',
  },
  {
    id: 4,
    role: 'buyer',
    icon: 'bi-heart-fill',
    text: 'Cô Ngọc Lan vừa đánh giá 5 sao cho Gian Hàng Đặc Sản Quê Hương',
    time: '4 phút trước',
  },
];

const observeNewElements = () => {
  if (typeof document === 'undefined') return;
  const revealElements = document.querySelectorAll('.scroll-reveal:not(.is-revealed)');
  revealElements.forEach((el) => {
    if (observer) {
      observer.observe(el);
    } else {
      el.classList.add('is-revealed');
    }
  });
};

onMounted(async () => {
  catalog.refreshCatalog();
  await nextTick();
  if (typeof window !== 'undefined' && 'IntersectionObserver' in window) {
    observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-revealed');
            observer?.unobserve(entry.target);
          }
        });
      },
      {
        root: null,
        rootMargin: '120px 0px 80px 0px',
        threshold: 0.02,
      }
    );

    observeNewElements();
  } else if (typeof document !== 'undefined') {
    document.querySelectorAll('.scroll-reveal').forEach((el) => {
      el.classList.add('is-revealed');
    });
  }

  // Safety fallback: Tự động reveal tất cả elements sau 500ms để không bao giờ bị ẩn/mất khi lướt
  setTimeout(() => {
    if (typeof document !== 'undefined') {
      document.querySelectorAll('.scroll-reveal:not(.is-revealed)').forEach((el) => {
        el.classList.add('is-revealed');
      });
    }
  }, 500);

  startSlideTimer();
});

onUnmounted(() => {
  if (observer) {
    observer.disconnect();
    observer = null;
  }
  if (slideTimer) {
    clearInterval(slideTimer);
    slideTimer = null;
  }
});

// ==========================================================
// 0. DỮ LIỆU BANNER SLIDER (4 SLIDES NÔNG SẢN CHỢ DÂN SINH)
// Sử dụng chính xác hình ảnh nhận diện của dự án ZoneMart
// ==========================================================
export interface HeroSlide {
  id: string;
  badge: string;
  title: string;
  description: string;
  primaryBtnText: string;
  primaryBtnRoute: string;
  primaryBtnNewTab?: boolean;
  secondaryBtnText: string;
  secondaryBtnRoute: string;
  secondaryBtnNewTab?: boolean;
  bgImage: string;
  modelImage: string;
  modelAlt: string;
  roleTag: string;
  topBadgeLeft: {
    icon: string;
    bgClass: string;
    title: string;
    desc: string;
  };
  topBadgeRight: {
    icon: string;
    bgClass: string;
    title: string;
    desc: string;
  };
  bottomChipText: string;
}

const heroSlides: HeroSlide[] = [
  {
    id: 'intro',
    badge: 'NÔNG SẢN & ĐẶC SẢN ĐỊA PHƯƠNG',
    title: 'Chợ Địa Phương – Nông Dân Thân Thiện',
    description:
      'Cầu nối trực tiếp giữa nhà vườn địa phương và gia đình bạn. Tươi ngon mỗi ngày, hỗ trợ cộng đồng nông sản với dịch vụ giao hàng nhanh trong bán kính 10km.',
    primaryBtnText: 'KHÁM PHÁ CHỢ',
    primaryBtnRoute: '/products',
    primaryBtnNewTab: false,
    secondaryBtnText: 'XEM TRÊN BẢN ĐỒ',
    secondaryBtnRoute: '/products',
    secondaryBtnNewTab: false,
    bgImage:
      'https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=1600&q=80',
    modelImage: '/images/anhxoanendkybanhang_clean.png',
    modelAlt: 'Mô hình Người Bán Nông Sản ZoneMart',
    roleTag: '350+ Nhà Vườn Địa Phương',
    topBadgeLeft: {
      icon: 'bi-shield-check',
      bgClass: 'bg-leaf',
      title: 'Chuẩn VietGAP & AI',
      desc: 'Rau củ thu hoạch sáng sớm',
    },
    topBadgeRight: {
      icon: 'bi-geo-alt-fill',
      bgClass: 'bg-terracotta',
      title: 'Chợ Dân Sinh 10km',
      desc: 'Giao siêu tốc tận bếp',
    },
    bottomChipText: 'Kết nối trực tiếp: Nhà Vườn ➔ Tài Xế ➔ Bếp Nhà Bạn',
  },
  {
    id: 'seller',
    badge: 'ĐỒNG HÀNH CÙNG NHÀ VƯỜN & TIỂU THƯƠNG',
    title: 'Mở Gian Hàng Số – Bán Hàng Dễ Dàng',
    description:
      'Đưa nông sản & đặc sản địa phương lên sàn số nhanh chóng. AI kiểm duyệt an toàn, tiếp cận hàng nghìn khách hàng lân cận và tăng trưởng doanh thu vượt bậc.',
    primaryBtnText: 'ĐĂNG KÝ SHOP',
    primaryBtnRoute: '/register-seller',
    primaryBtnNewTab: false,
    secondaryBtnText: 'CẨM NANG NGƯỜI BÁN',
    secondaryBtnRoute: '/seller-landing',
    secondaryBtnNewTab: false,
    bgImage:
      'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=1600&q=80',
    modelImage: '/images/anhxoanendkybanhang_clean.png',
    modelAlt: 'Mô hình Nhà Vườn & Tiểu Thương ZoneMart',
    roleTag: 'Mở Gian Hàng Số 0đ',
    topBadgeLeft: {
      icon: 'bi-patch-check-fill',
      bgClass: 'bg-leaf',
      title: 'AI Duyệt Sản Phẩm',
      desc: 'Lên kệ nhanh chỉ 3 phút',
    },
    topBadgeRight: {
      icon: 'bi-graph-up-arrow',
      bgClass: 'bg-terracotta',
      title: 'Đơn nổ liên tục!',
      desc: 'Tiếp cận 10.000+ cư dân',
    },
    bottomChipText: 'Bán kính 10km • Chủ động nguồn hàng tươi ngon',
  },
  {
    id: 'shipper',
    badge: 'ĐỐI TÁC GIAO HÀNG ZONE SHIPPER',
    title: 'Giao Hàng Siêu Tốc – Chủ Động Thu Nhập',
    description:
      'Giao hàng siêu tốc trong bán kính 10km, nhận đơn tiện lợi gần nhà, không lo đi xa. Thu nhập minh bạch, trả thưởng nhanh chóng và linh hoạt thời gian chạy đơn.',
    primaryBtnText: 'CỔNG TÀI XẾ',
    primaryBtnRoute: '/register-shipper',
    primaryBtnNewTab: false,
    secondaryBtnText: 'QUY CHẾ TÀI XẾ',
    secondaryBtnRoute: '/shipper-landing',
    secondaryBtnNewTab: false,
    bgImage:
      'https://images.unsplash.com/photo-1580674684081-7617fbf3d745?auto=format&fit=crop&w=1600&q=80',
    modelImage: '/images/anhxoanendkyshipper_clean.png',
    modelAlt: 'Mô hình Tài Xế Giao Hàng Zone Shipper',
    roleTag: 'Tài Xế Zone Shipper 5km',
    topBadgeLeft: {
      icon: 'bi-lightning-charge-fill',
      bgClass: 'bg-gold',
      title: 'Giao Siêu Tốc 15 - 30p',
      desc: 'Định vị GPS thời gian thực',
    },
    topBadgeRight: {
      icon: 'bi-wallet2',
      bgClass: 'bg-terracotta',
      title: 'Nhận đơn gần nhà 5km',
      desc: 'Thu nhập nhận liền tay',
    },
    bottomChipText: 'Thùng giữ nhiệt nông sản – Đơn nổ liên tục cả ngày',
  },
  {
    id: 'buyer',
    badge: 'TRẢI NGHIỆM ĐI CHỢ THÔNG MINH',
    title: 'Đi Chợ Online Tươi Chuẩn – Giao Tận Bếp',
    description:
      'Hàng trăm món tươi sạch, rau củ quả chuẩn VietGAP được kiểm định thị giác bằng AI. Đặt hàng dễ dàng, theo dõi lộ trình thời gian thực và nhận hàng chỉ sau 15-30 phút.',
    primaryBtnText: 'ĐI CHỢ NGAY',
    primaryBtnRoute: '/products',
    primaryBtnNewTab: false,
    secondaryBtnText: 'KHÁM PHÁ SẢN PHẨM',
    secondaryBtnRoute: '/products',
    secondaryBtnNewTab: false,
    bgImage:
      'https://images.unsplash.com/photo-1610348725531-843dff563e2c?auto=format&fit=crop&w=1600&q=80',
    modelImage: '/images/anhxoanen_clean.png',
    modelAlt: 'Mô hình Người Tiêu Dùng Đi Chợ Online ZoneMart',
    roleTag: 'Nông Sản Chuẩn Tươi 100%',
    topBadgeLeft: {
      icon: 'bi-eye-fill',
      bgClass: 'bg-leaf',
      title: 'Kiểm Định Thị Giác AI',
      desc: 'Độ tươi đạt 99.8%',
    },
    topBadgeRight: {
      icon: 'bi-basket2-fill',
      bgClass: 'bg-terracotta',
      title: 'Đổi trả miễn phí 1-1',
      desc: 'Giao tận cửa nhà',
    },
    bottomChipText: 'Tiết kiệm 45 phút mỗi ngày – Bữa ăn gia đình trọn vẹn',
  },
];

const currentSlide = ref(0);
let slideTimer: any = null;
const isHoveringSlider = ref(false);

const nextSlide = () => {
  currentSlide.value = (currentSlide.value + 1) % heroSlides.length;
  resetSlideTimer();
};

const prevSlide = () => {
  currentSlide.value =
    (currentSlide.value - 1 + heroSlides.length) % heroSlides.length;
  resetSlideTimer();
};

const goToSlide = (idx: number) => {
  currentSlide.value = idx;
  resetSlideTimer();
};

const startSlideTimer = () => {
  if (slideTimer) clearInterval(slideTimer);
  slideTimer = setInterval(() => {
    if (!isHoveringSlider.value) {
      currentSlide.value = (currentSlide.value + 1) % heroSlides.length;
    }
  }, 5500);
};

const resetSlideTimer = () => {
  startSlideTimer();
};

const onSliderMouseEnter = () => {
  isHoveringSlider.value = true;
};

const onSliderMouseLeave = () => {
  isHoveringSlider.value = false;
  resetHeroTilt();
};

// Hỗ trợ Touch Swipe trên điện thoại
let touchStartX = 0;
let touchEndX = 0;

const onTouchStart = (e: TouchEvent) => {
  touchStartX = e.changedTouches[0].screenX;
};

const onTouchEnd = (e: TouchEvent) => {
  touchEndX = e.changedTouches[0].screenX;
  const diff = touchEndX - touchStartX;
  if (Math.abs(diff) > 40) {
    if (diff < 0) {
      nextSlide();
    } else {
      prevSlide();
    }
  }
};

const portalCards = [
  {
    id: 'buyer',
    badge: 'NGƯỜI TIÊU DÙNG',
    title: 'KHÁM PHÁ SẢN PHẨM',
    tagline: 'Nông sản sạch & tươi ngon',
    description:
      'Mua thực phẩm tươi, rau củ chuẩn VietGAP từ nhà vườn lân cận. Giao nhanh 10km.',
    btnText: 'KHÁM PHÁ CHỢ',
    btnRoute: '/products',
    avatar: '/images/anhxoanen_clean.png',
    iconType: 'user',
    openNewTab: false,
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
    avatar: '/images/anhxoanendkybanhang_clean.png',
    iconType: 'store',
    openNewTab: false,
  },
  {
    id: 'shipper',
    badge: 'ĐỐI TÁC GIAO HÀNG',
    title: 'ĐĂNG KÝ SHIPPER',
    tagline: 'Giao siêu tốc bán kính 5km',
    description:
      'Linh hoạt thời gian, nhận đơn khu vực gần nhà, thu nhập minh bạch nhận liền tay.',
    btnText: 'CỔNG TÀI XẾ',
    btnRoute: '/register-shipper',
    avatar: '/images/anhxoanendkyshipper_clean.png',
    iconType: 'truck',
    openNewTab: false,
  },
];

// 2. Dữ liệu danh mục nổi bật (Featured Categories)
const featuredCategories = [
  {
    id: 1,
    name: 'Rau Củ Quả Tươi',
    desc: 'Thu hoạch trong ngày, tươi chuẩn VietGAP',
    tag: 'Tươi sống',
    count: '150+ Mặt hàng',
    catQuery: 'veggie',
    image:
      'https://images.unsplash.com/photo-1610348725531-843dff563e2c?auto=format&fit=crop&w=700&q=80',
  },
  {
    id: 2,
    name: 'Đồ Thủ Công & Chế Biến',
    desc: 'Bánh mứt, gia vị & món ăn truyền thống',
    tag: 'Thủ công',
    count: '60+ Gian hàng',
    catQuery: 'food',
    image:
      'https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=700&q=80',
  },
  {
    id: 3,
    name: 'Đặc Sản Địa Phương',
    desc: 'Hương vị quê nhà đặc trưng các vùng miền',
    tag: 'Đặc sản',
    count: '85+ Sản phẩm',
    catQuery: 'beverage',
    image:
      'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=700&q=80',
  },
  {
    id: 4,
    name: 'Bách Hóa & Đóng Gói',
    desc: 'Mật ong rừng, ngũ cốc, hạt sấy bổ dưỡng',
    tag: 'Bảo quản tốt',
    count: '95+ Mặt hàng',
    catQuery: 'household',
    image:
      'https://images.unsplash.com/photo-1588964895597-cfccd6e2dbf9?auto=format&fit=crop&w=700&q=80',
  },
];

// 3. Sản phẩm tươi mới nổi bật trong bán kính 10km (Hot Products)
const featuredProducts = computed(() => {
  if (catalog.allProducts.value && catalog.allProducts.value.length > 0) {
    const freshFoods = catalog.allProducts.value.filter(
      (p) => p.category !== 'household' && !p.name.toLowerCase().includes('rác')
    );
    return freshFoods.slice(0, 8).map((p, idx) => ({
      id: p.id,
      name: p.name,
      farm: p.store?.name || 'Gian hàng ZoneMart',
      distance: `${p.store?.distanceKm || 1.2} km`,
      deliveryTime: p.store?.deliveryTime || '15 - 20 phút',
      price: p.price,
      originalPrice: p.oldPrice || Math.round(p.price * 1.18),
      discount: p.discountBadge || `-${15 + (idx % 10)}%`,
      rating: p.rating || 5.0,
      sold: p.sold || 120 + idx * 15,
      tag: p.badge || 'Nông sản sạch',
      image:
        p.image ||
        'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=600&q=80',
    }));
  }
  return [];
});

// 4. Quy trình hoạt động 4 bước siêu tốc của ZoneMart
const howItWorks = [
  {
    step: '01',
    title: 'Định Vị Tự Động',
    desc: 'Hệ thống tự động quét và gợi ý các nhà vườn, cửa hàng uy tín trong bán kính 5km quanh bạn.',
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
    desc: 'Tài xế nhận đơn gần bạn, giao tận cửa nhà.',
    icon: 'bi-bicycle',
  },
];

// 5. Nhà vườn & Gian hàng tiêu biểu địa phương (Đồng bộ thời gian thực từ Database, có fallback bảo vệ)
const defaultFallbackVendors = [
  {
    id: 'store_dalat_farm',
    name: "Nông Trại Hữu Cơ Đà Lạt O'Farm",
    owner: 'Rau củ quả tươi sạch Đà Lạt',
    distance: '1.2 km',
    deliveryTime: '15 - 20 phút',
    productsCount: '48+ sản phẩm',
    rating: 5.0,
    reviews: 182,
    avatar: 'https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80',
    cover: 'https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 'store_vua_trai_cay',
    name: 'Vựa Trái Cây Miệt Vườn Chín Mọng',
    owner: 'Trái cây đặc sản 3 miền',
    distance: '1.8 km',
    deliveryTime: '20 - 25 phút',
    productsCount: '36+ sản phẩm',
    rating: 4.9,
    reviews: 145,
    avatar: 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80',
    cover: 'https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 'store_thit_sach',
    name: 'Thịt Heo Sinh Học & Bò Tươi Sạch',
    owner: 'Thực phẩm tươi sống VietGAP',
    distance: '2.1 km',
    deliveryTime: '15 - 25 phút',
    productsCount: '29+ sản phẩm',
    rating: 5.0,
    reviews: 210,
    avatar: 'https://images.unsplash.com/photo-1588854337236-6889d631faa8?auto=format&fit=crop&w=200&q=80',
    cover: 'https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 'store_hai_san',
    name: 'Hải Sản Biển Sống Vũng Tàu',
    owner: 'Tôm cua ghẹ tươi sống mỗi ngày',
    distance: '2.5 km',
    deliveryTime: '25 - 30 phút',
    productsCount: '42+ sản phẩm',
    rating: 4.9,
    reviews: 198,
    avatar: 'https://images.unsplash.com/photo-1534483509719-3feaee7c30da?auto=format&fit=crop&w=200&q=80',
    cover: 'https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 'store_nam_sach',
    name: 'Trang Trại Nấm & Rau Mầm Hữu Cơ',
    owner: 'Nấm tươi các loại giàu dinh dưỡng',
    distance: '1.5 km',
    deliveryTime: '15 - 20 phút',
    productsCount: '25+ sản phẩm',
    rating: 5.0,
    reviews: 120,
    avatar: 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80',
    cover: 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=600&q=80',
  },
  {
    id: 'store_ga_ta',
    name: 'Gà Ta Thả Vườn & Trứng Gà Sạch',
    owner: 'Trại chăn thả sinh học tự nhiên',
    distance: '3.0 km',
    deliveryTime: '20 - 30 phút',
    productsCount: '18+ sản phẩm',
    rating: 4.8,
    reviews: 95,
    avatar: 'https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=200&q=80',
    cover: 'https://images.unsplash.com/photo-1516467508483-a7212febe31a?auto=format&fit=crop&w=600&q=80',
  },
];

const featuredVendors = computed(() => {
  if (catalog.allStores.value && catalog.allStores.value.length > 0) {
    return catalog.allStores.value.slice(0, 6).map((s, idx) => ({
      id: s.id || idx + 1,
      name: s.name,
      owner:
        s.categoryName ||
        (s.address ? s.address.split(',')[0].trim() : 'Đối tác địa phương uy tín'),
      distance: `${s.distanceKm || 1.2} km`,
      deliveryTime: s.deliveryTime || '15 - 20 phút',
      productsCount: `${s.totalProducts || s.products?.length || 1}+ sản phẩm`,
      rating: s.rating || 5.0,
      reviews: s.reviewsCount || 100,
      avatar:
        s.avatar ||
        'https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80',
      cover:
        s.coverImage ||
        'https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=600&q=80',
    }));
  }
  return defaultFallbackVendors;
});

watch(
  () => featuredVendors.value,
  async () => {
    await nextTick();
    observeNewElements();
  },
  { deep: true }
);

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

const goToCategory = (catKey?: string) => {
  if (catKey) {
    router.push({ path: '/products', query: { cat: catKey, type: 'product' } });
  } else {
    router.push('/products');
  }
};

const goToStore = (storeName: string) => {
  router.push({
    path: '/products',
    query: { store: storeName, type: 'product' },
  });
};

const goToPortal = (btnRoute: string, _openNewTab?: boolean) => {
  if (btnRoute) {
    if (auth.isLoggedIn.value) {
      if (btnRoute === '/register-seller' || btnRoute === '/seller-landing' || btnRoute === '/seller-form') {
        router.push({ path: '/profile', query: { tab: 'seller-reg' } });
        return;
      }
      if (btnRoute === '/register-shipper' || btnRoute === '/shipper-landing' || btnRoute === '/shipper-form') {
        router.push({ path: '/profile', query: { tab: 'shipper-reg' } });
        return;
      }
    }
    router.push(btnRoute);
  }
};

const handleAddToCartHome = (item: any) => {
  cart.addItem(
    {
      storeId: 'store_' + (item.farm || 'default').toLowerCase().replace(/\s+/g, '_'),
      storeName: item.farm || 'ZoneMart Đối Tác',
      distanceKm: parseFloat(item.distance) || 1.5,
      deliveryTime: item.deliveryTime || '25 phút',
    },
    {
      id: 'prod_' + item.id,
      name: item.name,
      price: item.price,
      originalPrice: item.originalPrice,
      unit: item.tag || 'Phần',
      image: item.image,
    }
  );
  showToast(`Đã thêm "${item.name}" vào giỏ hàng thành công!`);
};
</script>

<template>
  <div class="homepage-container">
    <!-- Bối cảnh 3D Ambient Blobs tạo chiều sâu không gian -->
    <div class="ambient-bg-decor" aria-hidden="true">
      <div class="ambient-orb orb-terracotta"></div>
      <div class="ambient-orb orb-leaf"></div>
      <div class="ambient-orb orb-gold"></div>
    </div>

    <!-- Thông báo thêm giỏ hàng nhẹ nhàng -->
    <Transition name="toast-fade">
      <div v-if="toastMessage" class="home-toast-notice">
        <i class="bi bi-check2-circle text-success me-2 fs-5"></i>
        <span>{{ toastMessage }}</span>
      </div>
    </Transition>

    <!-- ==========================================================
         1. FIRST FOLD: HERO BANNER 3D & 3 THẺ VAI TRÒ + NHỊP SỐNG CHỢ
         ========================================================== -->
    <div class="first-fold-wrapper">
      <!-- HERO BANNER SLIDER KÈM MÔ HÌNH 3D CON NGƯỜI MUA BÁN CHỢ DÂN SINH -->
      <section
        class="hero-section hero-slider-container"
        @mouseenter="onSliderMouseEnter"
        @mouseleave="onSliderMouseLeave"
        @mousemove="onHeroMouseMove"
        @touchstart.passive="onTouchStart"
        @touchend.passive="onTouchEnd"
      >
        <div class="hero-slider-track-wrapper">
          <div
            class="hero-slider-track"
            :style="{ transform: `translate3d(-${currentSlide * 100}%, 0, 0)` }"
          >
            <div
              v-for="slide in heroSlides"
              :key="slide.id"
              class="hero-card"
              :style="{ backgroundImage: `url('${slide.bgImage}')` }"
            >
              <div class="hero-overlay"></div>

              <!-- Hạt nắng lấp lánh buổi chợ sớm -->
              <div class="hero-sun-particles" aria-hidden="true">
                <span class="particle p1"></span>
                <span class="particle p2"></span>
                <span class="particle p3"></span>
                <span class="particle p4"></span>
              </div>

              <div class="hero-inner-grid">
                <!-- NỘI DUNG CHÍNH HERO BANNER (THOÁNG ĐÃNG, KHÔNG HÌNH ẢNH RƯỜM RÀ) -->
                <div class="hero-content">
                  <div class="hero-badge-row">
                    <span class="live-pulse-dot"></span>
                    <span class="hero-subheading">{{ slide.badge }}</span>
                  </div>
                  <h1 class="hero-title">{{ slide.title }}</h1>
                  <p class="hero-description">{{ slide.description }}</p>

                  <!-- Thống kê nhanh tạo niềm tin Chợ Dân Sinh -->
                  <div class="hero-mini-metrics">
                    <div class="metric-pill">
                      <i class="bi bi-people-fill"></i>
                      <span><strong>350+</strong> Tiểu thương & Nhà vườn</span>
                    </div>
                    <div class="metric-pill">
                      <i class="bi bi-lightning-charge-fill"></i>
                      <span>Giao nhanh <strong>15 - 30 phút</strong></span>
                    </div>
                    <div class="metric-pill">
                      <i class="bi bi-shield-check"></i>
                      <span>Chuẩn <strong>VietGAP & AI</strong></span>
                    </div>

                  </div>

                  <div class="hero-actions">
                    <button
                      class="btn-primary-warm btn-3d-push"
                      @click="goToPortal(slide.primaryBtnRoute, slide.primaryBtnNewTab)"
                    >
                      <span>{{ slide.primaryBtnText }}</span>
                      <i class="bi bi-arrow-right-short fs-5"></i>
                    </button>
                    <button
                      class="btn-secondary-warm"
                      @click="goToPortal(slide.secondaryBtnRoute, slide.secondaryBtnNewTab)"
                    >
                      {{ slide.secondaryBtnText }}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Nút điều hướng Previous / Next -->
        <button
          type="button"
          class="slider-nav-btn prev-btn"
          @click.stop="prevSlide"
          aria-label="Slide trước"
          title="Slide trước"
        >
          <i class="bi bi-chevron-left"></i>
        </button>
        <button
          type="button"
          class="slider-nav-btn next-btn"
          @click.stop="nextSlide"
          aria-label="Slide tiếp theo"
          title="Slide tiếp theo"
        >
          <i class="bi bi-chevron-right"></i>
        </button>

        <!-- Chấm tròn chuyển slide (Pagination Dots) -->
        <div class="slider-dots">
          <button
            v-for="(_, idx) in heroSlides"
            :key="'dot-' + idx"
            type="button"
            class="slider-dot"
            :class="{ active: currentSlide === idx }"
            @click.stop="goToSlide(idx)"
            :aria-label="'Chuyển tới slide ' + (idx + 1)"
          >
            <span class="slider-dot-bar"></span>
          </button>
        </div>
      </section>

      <!-- 3 THẺ ROLE PORTALS 3D ĐÈ LÊN CHÂN HERO -->
      <section class="portals-section">
        <div class="portals-grid">
          <div
            v-for="card in portalCards"
            :key="card.id"
            class="portal-card-3d-wrap"
            @click="goToPortal(card.btnRoute, card.openNewTab)"
          >
            <div class="portal-card card-3d-inner">
              <div class="card-glare" aria-hidden="true"></div>

              <!-- Avatar mô hình 3D thực tế theo từng vai trò -->
              <div class="portal-avatar-box pop-3d-layer">
                <div class="portal-avatar-glow" :class="'avatar-glow-' + card.id"></div>
                <img
                  :src="card.avatar"
                  :alt="card.title"
                  class="portal-model-avatar"
                  loading="lazy"
                />
              </div>

              <div class="portal-card-main-content">
                <span class="portal-role-badge">{{ card.badge }}</span>
                <h3 class="portal-title pop-3d-text">{{ card.title }}</h3>
                <p class="portal-tagline">{{ card.tagline }}</p>
                <p class="portal-desc">{{ card.description }}</p>
              </div>

              <button
                class="portal-btn pop-3d-btn"
                @click.stop="goToPortal(card.btnRoute, card.openNewTab)"
              >
                <span>{{ card.btnText }}</span>
                <i class="bi bi-arrow-up-right-circle-fill ms-1"></i>
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- THANH NHỊP SỐNG CHỢ DÂN SINH (LIVE MARKET ACTIVITY BAR) -->
      <div class="live-market-ticker scroll-reveal">
        <div class="ticker-label">
          <span class="live-dot-red"></span>
          <span>CHỢ ĐANG HỌP SÔI ĐỘNG</span>
        </div>
        <div class="ticker-track-wrap">
          <div class="ticker-track">
            <div
              v-for="act in [...liveMarketActivities, ...liveMarketActivities]"
              :key="act.id + '-' + Math.random()"
              class="ticker-item"
            >
              <i class="bi" :class="[act.icon, 'role-color-' + act.role]"></i>
              <span class="ticker-text">{{ act.text }}</span>
              <span class="ticker-time">{{ act.time }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ==========================================================
         2. DANH MỤC NỔI BẬT (FEATURED CATEGORIES - 3D TILT CARDS)
         ========================================================== -->
    <section class="section-container">
      <div class="section-header scroll-reveal">
        <div class="header-left">
          <div class="section-eyebrow">
            <i class="bi bi-grid-1x2-fill me-1"></i> SẠP HÀNG TRUYỀN THỐNG & HIỆN ĐẠI
          </div>
          <h2 class="section-title">Danh Mục Nổi Bật</h2>
          <p class="section-subtitle">
            Khám phá các mặt hàng địa phương tươi ngon và đặc sắc nhất
          </p>
        </div>
        <router-link to="/products" class="view-all-link">
          <span>Xem Tất Cả </span><span class="view-all-extra">Danh Mục </span>➔
        </router-link>
      </div>

      <div class="categories-grid">
        <div
          v-for="(cat, idx) in featuredCategories"
          :key="cat.id"
          class="category-card-wrap scroll-reveal"
          :style="{ '--reveal-delay': `${idx * 0.07}s` }"
          @click="goToCategory(cat.catQuery)"
        >
          <div class="category-card card-3d-inner">
            <div class="category-img-wrap">
              <img
                :src="cat.image"
                :alt="cat.name"
                class="category-img"
                loading="lazy"
              />
              <div class="category-img-gradient"></div>
              <span class="category-tag pop-3d-badge">{{ cat.tag }}</span>
            </div>
            <div class="category-info">
              <h4 class="category-name">{{ cat.name }}</h4>
              <p class="category-desc">{{ cat.desc }}</p>
              <div class="category-footer">
                <span class="category-count">
                  <i class="bi bi-basket3-fill me-1"></i>{{ cat.count }}
                </span>
                <span class="category-arrow">
                  <span class="arrow-text">Khám phá </span>➔
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         3. SẢN PHẨM BÁN CHẠY TRONG BÁN KÍNH 10KM (HOT PRODUCTS 3D)
         ========================================================== -->
    <section class="section-container">
      <div class="section-header scroll-reveal">
        <div class="header-left">
          <div class="badge-tag-hot">
            <i class="bi bi-fire me-1" aria-hidden="true"></i> GIAO HỎA TỐC 5KM - 10KM
          </div>
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
          v-for="(item, idx) in featuredProducts"
          :key="item.id"
          class="product-card-wrap scroll-reveal"
          :style="{ '--reveal-delay': `${(idx % 4) * 0.07}s` }"
          @click="goToRoute('/products')"
        >
          <div class="product-card card-3d-inner">
            <!-- Ảnh sản phẩm kèm Tag khuyến mãi 3D -->
            <div class="product-img-wrap">
              <img
                :src="item.image"
                :alt="item.name"
                class="product-img"
                loading="lazy"
              />
              <span class="product-discount-tag pop-3d-badge">{{ item.discount }}</span>
              <span class="product-status-tag">
                <i class="bi bi-patch-check-fill text-success me-1"></i>{{ item.tag }}
              </span>
            </div>

            <!-- Thông tin sản phẩm -->
            <div class="product-body">
              <div class="product-location-row">
                <span class="loc-badge">
                  <i class="bi bi-geo-alt-fill"></i> {{ item.distance }}
                </span>
                <span class="time-badge">
                  <i class="bi bi-lightning-charge-fill"></i> {{ item.deliveryTime }}
                </span>
              </div>
              <h4 class="product-title">{{ item.name }}</h4>
              <p class="product-farm">
                <i class="bi bi-shop text-terracotta me-1"></i> {{ item.farm }}
              </p>

              <div class="product-rating-row">
                <span class="stars">
                  <i class="bi bi-star-fill text-warning"></i> {{ item.rating }}
                </span>
                <span class="sold-count">Đã bán {{ item.sold }}</span>
              </div>

              <div class="product-price-row">
                <div class="price-box">
                  <span class="current-price">{{ formatPrice(item.price) }}</span>
                  <span class="original-price">{{
                    formatPrice(item.originalPrice)
                  }}</span>
                </div>
                <button
                  class="btn-add-cart"
                  title="Thêm vào giỏ hàng"
                  aria-label="Chọn mua sản phẩm"
                  @click.stop="handleAddToCartHome(item)"
                >
                  <i class="bi bi-bag-plus-fill"></i> Chọn Mua
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         4. QUY TRÌNH HOẠT ĐỘNG 4 BƯỚC (HOW ZONEMART WORKS 3D)
         ========================================================== -->
    <section class="section-container how-it-works-section">
      <div class="how-header scroll-reveal">
        <span class="how-badge">MÔ HÌNH VẬN HÀNH THÔNG MINH</span>
        <h2 class="section-title text-center">
          ZoneMart Kết Nối Chợ Dân Sinh Thế Nào?
        </h2>
        <p class="section-subtitle text-center">
          Giao hàng siêu tốc trong bán kính 5km - 10km nhờ công nghệ kết nối tự động
          giữa Người Mua, Nhà Vườn và Tài Xế
        </p>
      </div>

      <div class="how-steps-grid">
        <div
          v-for="(step, idx) in howItWorks"
          :key="step.step"
          class="step-card-wrap scroll-reveal"
          :style="{ '--reveal-delay': `${idx * 0.09}s` }"
        >
          <div class="step-card card-3d-inner">
            <div class="step-icon-wrap pop-3d-layer">
              <div class="step-icon-ring"></div>
              <div class="step-icon">
                <i class="bi" :class="step.icon" aria-hidden="true"></i>
              </div>
            </div>
            <div class="step-content">
              <div class="step-title-row">
                <h4 class="step-title">{{ step.title }}</h4>
                <span class="step-number">{{ step.step }}</span>
              </div>
              <p class="step-desc">{{ step.desc }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         5. NHÀ VƯỜN & GIAN HÀNG TIÊU BIỂU ĐỊA PHƯƠNG
         ========================================================== -->
    <section class="section-container">
      <div class="section-header scroll-reveal">
        <div class="header-left">
          <div class="section-eyebrow">
            <i class="bi bi-award-fill me-1"></i> TIỂU THƯƠNG UY TÍN QUANH BẠN
          </div>
          <h2 class="section-title">Gian Hàng Đối Tác Tiêu Biểu</h2>
          <p class="section-subtitle">
            Ghé thăm những nhà vườn hữu cơ và vựa trái cây chuẩn chất lượng
            quanh bạn
          </p>
        </div>
        <router-link to="/products?type=store" class="view-all-link">
          <span>Xem Tất Cả Gian Hàng</span>
          <i class="bi bi-arrow-right ms-1"></i>
        </router-link>
      </div>

      <div class="vendors-grid">
        <div
          v-for="(v, idx) in featuredVendors"
          :key="v.id"
          class="vendor-card-wrap is-revealed"
          :style="{ '--reveal-delay': `${(idx % 3) * 0.08}s` }"
          @click="goToStore(v.name)"
        >
          <div class="vendor-card card-3d-inner">
            <div class="vendor-cover-wrap">
              <img
                :src="v.cover"
                :alt="v.name"
                class="vendor-cover"
                loading="lazy"
              />
              <div class="vendor-cover-overlay"></div>
              <span class="vendor-live-tag">
                <span class="dot-online"></span> Đang mở cửa
              </span>
              <div class="vendor-avatar-wrap pop-3d-layer">
                <img :src="v.avatar" :alt="v.owner" class="vendor-avatar" />
              </div>
            </div>
            <div class="vendor-body">
              <h4 class="vendor-name">{{ v.name }}</h4>
              <p class="vendor-owner">{{ v.owner }}</p>
              <div class="vendor-meta-row">
                <span>
                  <i class="bi bi-geo-alt-fill text-danger"></i> Cách bạn {{ v.distance }}
                </span>
                <span>
                  <i class="bi bi-star-fill text-warning"></i> {{ v.rating }}
                </span>
              </div>
              <p class="vendor-stock">
                <i class="bi bi-box-seam text-primary me-1"></i> {{ v.productsCount }}
              </p>
              <button class="vendor-btn" @click.stop="goToStore(v.name)">
                <span>Ghé Thăm Sạp Hàng</span>
                <i class="bi bi-arrow-right"></i>
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         6. ĐÁNH GIÁ THỰC TẾ TỪ CỘNG ĐỒNG (TESTIMONIALS)
         ========================================================== -->
    <section class="section-container testimonials-section">
      <div class="testimonials-header">
        <span class="how-badge">TIẾNG NÓI BÀ CON CHỢ DÂN SINH</span>
        <h2 class="section-title">Cộng Đồng Nói Gì Về ZoneMart?</h2>
        <p class="section-subtitle">
          Trải nghiệm mua sắm sạch, bán hàng số và giao nhận hỏa tốc từ người
          dùng thực tế
        </p>
      </div>

      <div class="testimonials-grid">
        <div
          v-for="(t, idx) in testimonials"
          :key="t.id"
          class="testimonial-card-wrap scroll-reveal"
          :style="{ '--reveal-delay': `${idx * 0.09}s` }"
        >
          <div class="testimonial-card card-3d-inner">
            <div class="quote-watermark" aria-hidden="true">“</div>
            <div class="stars-row">
              <i
                v-for="n in t.rating"
                :key="n"
                class="bi bi-star-fill text-warning me-1"
              ></i>
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
      </div>
    </section>

    <!-- ==========================================================
         7. BANNER KÊU GỌI HỢP TÁC (CTA BANNER 3D)
         ========================================================== -->
    <section class="section-container">
      <div class="partner-cta-card scroll-reveal">
        <div class="cta-3d-glow" aria-hidden="true"></div>
        <div class="partner-cta-content">
          <span class="cta-mini-tag">ĐỒNG HÀNH CÙNG ZONEMART</span>
          <h2 class="cta-title">
            Bạn Là Nhà Vườn Hoặc Muốn Tăng Thu Nhập Cùng Shipper?
          </h2>
          <p class="cta-desc">
            ZoneMart miễn phí 100% phí mở gian hàng cho tiểu thương và cung cấp
            luồng đơn hàng liên tục trong bán kính 5km - 10km cho tài xế.
          </p>
          <div class="cta-btn-group">
            <button
              class="btn-partner-seller"
              @click="goToPortal('/register-seller')"
            >
              <i class="bi bi-shop me-2" aria-hidden="true"></i> ĐĂNG KÝ BÁN HÀNG NGAY
            </button>
            <button
              class="btn-partner-shipper"
              @click="goToPortal('/register-shipper')"
            >
              <i class="bi bi-bicycle me-2" aria-hidden="true"></i> GIA NHẬP ĐỘI TÀI XẾ
            </button>
          </div>
        </div>

        <!-- Huy hiệu 3D trang trí góc phải Banner CTA -->
        <div class="cta-visual-badge" aria-hidden="true">
          <div class="cta-circle-outer">
            <div class="cta-circle-inner">
              <i class="bi bi-shop-window"></i>
              <span>100%</span>
              <small>Miễn Phí Mở Sạp</small>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==========================================================
         8. ĐIỂM NHẤN CAM KẾT 10KM
         ========================================================== -->
    <section class="section-container">
      <div class="highlights-card scroll-reveal">
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
   HOMEPAGE WARM HUMANIST & 3D INTERACTIVE MARKET STYLES
   Màu chủ đạo: Terracotta (#D85A2A), Nền kem ấm (#FAF7F2), Xanh Lá (#15803D)
   ========================================================== */

/* ==========================================================
   HỆ THỐNG SCROLL REVEAL ỔN ĐỊNH (TÁCH BIỆT VỚI HOVER 3D)
   ========================================================== */
.scroll-reveal {
  opacity: 0;
  transform: translate3d(0, 28px, 0);
  transition:
    opacity 0.65s cubic-bezier(0.16, 1, 0.3, 1),
    transform 0.65s cubic-bezier(0.16, 1, 0.3, 1);
  transition-delay: var(--reveal-delay, 0s);
  will-change: opacity, transform;
}

.scroll-reveal.is-revealed {
  opacity: 1;
  transform: translate3d(0, 0, 0);
}

@media (prefers-reduced-motion: reduce) {
  .scroll-reveal {
    opacity: 1 !important;
    transform: none !important;
    transition: none !important;
  }
}

/* ==========================================================
   KHUNG CHÍNH & AMBIENT 3D BACKGROUND
   ========================================================== */
.homepage-container {
  position: relative;
  width: 100%;
  min-height: calc(100vh - 70px);
  background-color: #faf7f2;
  padding: 12px clamp(16px, 3.5vw, 48px) 60px clamp(16px, 3.5vw, 48px);
  box-sizing: border-box;
  font-family:
    'Plus Jakarta Sans', 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont,
    'Segoe UI', Roboto, sans-serif;
  color: #2b1b14;
  overflow-x: hidden;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.ambient-bg-decor {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 0;
  overflow: hidden;
}
.ambient-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(90px);
  opacity: 0.35;
}
.orb-terracotta {
  width: 420px;
  height: 420px;
  background: radial-gradient(circle, #f97316 0%, transparent 70%);
  top: 180px;
  left: -140px;
}
.orb-leaf {
  width: 480px;
  height: 480px;
  background: radial-gradient(circle, #22c55e 0%, transparent 70%);
  top: 950px;
  right: -180px;
  opacity: 0.2;
}
.orb-gold {
  width: 380px;
  height: 380px;
  background: radial-gradient(circle, #f59e0b 0%, transparent 70%);
  bottom: 400px;
  left: 15%;
  opacity: 0.22;
}

/* SECTION CHUNG */
.section-container {
  position: relative;
  z-index: 1;
  margin-bottom: 56px;
  width: 100%;
}
.section-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 24px;
}
.section-eyebrow {
  display: inline-flex;
  align-items: center;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.7px;
  color: #d85a2a;
  margin-bottom: 4px;
  text-transform: uppercase;
}
.section-title {
  font-size: clamp(22px, 2.3vw, 30px);
  font-weight: 800;
  color: #2b1b14;
  margin: 0 0 4px 0;
  letter-spacing: -0.4px;
}
.section-subtitle {
  font-size: 13.5px;
  color: #7b6960;
  margin: 0;
}
.view-all-link {
  display: inline-flex;
  align-items: center;
  font-size: 13.5px;
  font-weight: 700;
  color: #d85a2a;
  text-decoration: none;
  white-space: nowrap;
  padding: 6px 14px;
  border-radius: 50px;
  background: rgba(216, 90, 42, 0.08);
  transition: all 0.25s ease;
}
.view-all-link:hover {
  background: #d85a2a;
  color: #ffffff;
  transform: translateX(4px);
}

/* ==========================================================
   1. FIRST FOLD: HERO BANNER & KHUNG CẢNH CHỢ DÂN SINH 3D
   ========================================================== */
.first-fold-wrapper {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  margin-bottom: 52px;
}
.hero-section {
  position: relative;
  width: 100%;
}
.hero-slider-container {
  border-radius: 22px;
  overflow: hidden;
  box-shadow:
    0 20px 45px -12px rgba(43, 27, 20, 0.25),
    0 0 0 1px rgba(255, 255, 255, 0.15) inset;
}
.hero-slider-track-wrapper {
  width: 100%;
  overflow: hidden;
}
.hero-slider-track {
  display: flex;
  width: 100%;
  transition: transform 0.7s cubic-bezier(0.22, 1, 0.36, 1);
  will-change: transform;
}
.hero-card {
  position: relative;
  flex: 0 0 100%;
  width: 100%;
  min-height: 250px;
  border-radius: 18px;
  overflow: hidden;
  background-size: cover;
  background-position: center 35%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding-bottom: 26px;
  box-sizing: border-box;
}
.hero-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(
    105deg,
    rgba(28, 14, 8, 0.95) 0%,
    rgba(48, 22, 12, 0.88) 46%,
    rgba(180, 67, 24, 0.68) 100%
  );
  z-index: 1;
}

/* Hạt nắng chợ sớm */
.hero-sun-particles {
  position: absolute;
  inset: 0;
  z-index: 1;
  pointer-events: none;
}
.particle {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(254, 240, 138, 0.8), transparent 70%);
  animation: floatParticle 6s ease-in-out infinite;
}
.particle.p1 {
  width: 120px;
  height: 120px;
  top: -20px;
  right: 28%;
}
.particle.p2 {
  width: 16px;
  height: 16px;
  top: 25%;
  left: 45%;
  animation-delay: 1.2s;
}
.particle.p3 {
  width: 12px;
  height: 12px;
  bottom: 30%;
  left: 38%;
  animation-delay: 2.5s;
}
.particle.p4 {
  width: 20px;
  height: 20px;
  top: 18%;
  right: 8%;
  animation-delay: 0.7s;
}

@keyframes floatParticle {
  0%,
  100% {
    transform: translateY(0) scale(1);
    opacity: 0.45;
  }
  50% {
    transform: translateY(-14px) scale(1.15);
    opacity: 0.85;
  }
}

.hero-inner-grid {
  position: relative;
  z-index: 2;
  width: 100%;
  max-width: 820px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 24px clamp(16px, 3.5vw, 40px) 14px clamp(16px, 3.5vw, 40px);
  box-sizing: border-box;
}

.hero-content {
  max-width: 780px;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  color: #ffffff;
}

.hero-badge-row {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  background: rgba(255, 255, 255, 0.14);
  backdrop-filter: blur(8px);
  padding: 4px 14px;
  border-radius: 30px;
  margin-bottom: 8px;
  border: 1px solid rgba(255, 255, 255, 0.25);
}

.live-pulse-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #4ade80;
  box-shadow: 0 0 0 0 rgba(74, 222, 128, 0.7);
  animation: pulseGreen 2s infinite;
}

@keyframes pulseGreen {
  0% {
    box-shadow: 0 0 0 0 rgba(74, 222, 128, 0.7);
  }
  70% {
    box-shadow: 0 0 0 8px rgba(74, 222, 128, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(74, 222, 128, 0);
  }
}

.hero-subheading {
  color: #ffe4d4;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.8px;
  text-transform: uppercase;
}

.hero-title {
  font-size: clamp(20px, 2.3vw, 29px);
  font-weight: 850;
  line-height: 1.25;
  margin: 0 0 8px 0;
  letter-spacing: -0.4px;
  color: #fff9f5;
  text-shadow: 0 3px 12px rgba(0, 0, 0, 0.35);
  text-align: center;
}

.hero-description {
  font-size: clamp(12.5px, 0.95vw, 14px);
  line-height: 1.5;
  color: #f7e6dc;
  margin: 0 0 12px 0;
  max-width: 620px;
  text-align: center;
}

.hero-mini-metrics {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 8px;
  margin-bottom: 16px;
}

.metric-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  color: #fff3eb;
  background: rgba(20, 10, 5, 0.45);
  border: 1px solid rgba(255, 255, 255, 0.18);
  padding: 4px 11px;
  border-radius: 8px;
  backdrop-filter: blur(6px);
}

.metric-pill i {
  color: #fb923c;
}

.hero-actions {
  display: flex;
  gap: 10px;
  justify-content: center;
  flex-wrap: wrap;
}

.btn-primary-warm {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  background: linear-gradient(135deg, #ea6431 0%, #c84b1c 100%);
  color: #ffffff;
  border: none;
  font-weight: 800;
  font-size: 12.5px;
  letter-spacing: 0.3px;
  padding: 8px 22px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow:
    0 6px 16px -3px rgba(216, 90, 42, 0.55),
    0 2px 0 rgba(255, 255, 255, 0.25) inset;
}

.btn-primary-warm:hover {
  transform: translateY(-2px) scale(1.02);
  box-shadow: 0 10px 22px -4px rgba(216, 90, 42, 0.75);
}

.btn-secondary-warm {
  background: rgba(255, 255, 255, 0.12);
  backdrop-filter: blur(6px);
  color: #ffffff;
  border: 1.2px solid rgba(255, 255, 255, 0.4);
  font-weight: 700;
  font-size: 12.5px;
  padding: 8px 20px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.25s ease;
}

.btn-secondary-warm:hover {
  background: rgba(255, 255, 255, 0.25);
  border-color: #ffffff;
  transform: translateY(-2px);
}

/* Nút điều hướng Slider */
.slider-nav-btn {
  position: absolute;
  top: calc(50% - 19px);
  z-index: 6;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.18);
  backdrop-filter: blur(8px);
  border: 1.5px solid rgba(255, 255, 255, 0.45);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  cursor: pointer;
  transition: all 0.25s ease;
  opacity: 0;
}
.hero-slider-container:hover .slider-nav-btn {
  opacity: 1;
}
.slider-nav-btn.prev-btn {
  left: 14px;
}
.slider-nav-btn.next-btn {
  right: 14px;
}
.slider-nav-btn:hover {
  background: #d85a2a;
  border-color: #ffffff;
  transform: scale(1.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
}

/* Thanh chấm điều hướng Slider */
.slider-dots {
  position: absolute;
  bottom: 8px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 6;
  display: flex;
  align-items: center;
  gap: 6px;
  background: rgba(20, 10, 6, 0.45);
  backdrop-filter: blur(8px);
  padding: 3px 10px;
  border-radius: 20px;
  border: 1px solid rgba(255, 255, 255, 0.2);
}
.slider-dot {
  background: none;
  border: none;
  padding: 3px 0;
  cursor: pointer;
  display: flex;
  align-items: center;
}
.slider-dot-bar {
  display: block;
  width: 8px;
  height: 4px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.35);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.slider-dot.active .slider-dot-bar {
  width: 28px;
  background: #ff7a45;
  box-shadow: 0 0 10px rgba(255, 122, 69, 0.85);
}

/* ==========================================================
   3 ROLE PORTAL CARDS (3D DEPTH POP-OUT VỚI AVATAR MÔ HÌNH THỰC TẾ)
   ========================================================== */
.portals-section {
  position: relative;
  z-index: 10;
  margin-top: -16px;
  width: 100%;
}
.portals-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: clamp(14px, 2vw, 24px);
}
.portal-card-3d-wrap {
  perspective: 900px;
  cursor: pointer;
}
.portal-card {
  position: relative;
  height: 100%;
  background: #ffffff;
  border: 1.5px solid #eed9cc;
  border-radius: 18px;
  padding: 22px 20px 20px 20px;
  text-align: center;
  box-shadow: 0 10px 28px -6px rgba(43, 27, 20, 0.08);
  display: flex;
  flex-direction: column;
  align-items: center;
  transform-style: preserve-3d;
  transition:
    transform 0.35s cubic-bezier(0.16, 1, 0.3, 1),
    box-shadow 0.35s ease,
    border-color 0.25s ease;
  overflow: hidden;
}
.portal-card-3d-wrap:hover .portal-card {
  transform: translateY(-7px) rotateX(4deg);
  border-color: #d85a2a;
  box-shadow:
    0 20px 36px -8px rgba(216, 90, 42, 0.22),
    0 4px 0 #d85a2a;
}
.card-glare {
  position: absolute;
  top: -50%;
  left: -60%;
  width: 50%;
  height: 200%;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.65),
    transparent
  );
  transform: rotate(25deg);
  transition: left 0.65s ease;
  pointer-events: none;
}
.portal-card-3d-wrap:hover .card-glare {
  left: 130%;
}
.portal-role-badge {
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.6px;
  color: #9a5b3e;
  background: #fdf4ee;
  padding: 3px 10px;
  border-radius: 20px;
  margin-bottom: 10px;
}

/* Hộp avatar mô hình 3D thực tế trong Role Cards */
.portal-avatar-box {
  width: 72px;
  height: 72px;
  border-radius: 20px;
  background: linear-gradient(135deg, #fff5ee 0%, #fde3d3 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
  position: relative;
  border: 1.5px solid #f9ded0;
  box-shadow: 0 8px 20px -4px rgba(216, 90, 42, 0.22);
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.35s ease;
}
.portal-card-3d-wrap:hover .portal-avatar-box {
  transform: translateZ(24px) scale(1.1) translateY(-4px);
  box-shadow: 0 14px 28px -4px rgba(216, 90, 42, 0.35);
}
.portal-model-avatar {
  width: 62px;
  height: 62px;
  object-fit: contain;
  filter: drop-shadow(0 4px 8px rgba(0, 0, 0, 0.22));
  transition: transform 0.35s ease;
}
.portal-card-3d-wrap:hover .portal-model-avatar {
  transform: scale(1.12);
}
.portal-avatar-glow {
  position: absolute;
  inset: -4px;
  border-radius: 24px;
  filter: blur(8px);
  opacity: 0.4;
  z-index: 0;
  pointer-events: none;
}
.avatar-glow-buyer {
  background: rgba(34, 197, 94, 0.35);
}
.avatar-glow-seller {
  background: rgba(234, 88, 12, 0.4);
}
.avatar-glow-shipper {
  background: rgba(245, 158, 11, 0.4);
}
.portal-card-main-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
  flex-grow: 1;
}
.portal-title {
  font-size: 15.5px;
  font-weight: 800;
  letter-spacing: 0.3px;
  color: #2b1b14;
  margin: 0 0 4px 0;
  text-transform: uppercase;
}
.portal-tagline {
  font-size: 12.5px;
  font-weight: 700;
  color: #d85a2a;
  margin: 0 0 8px 0;
}
.portal-desc {
  font-size: 13px;
  line-height: 1.48;
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
  font-size: 12.5px;
  letter-spacing: 0.4px;
  padding: 10px 14px;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.25s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.portal-btn:hover {
  background: #bf4a1f;
  box-shadow: 0 6px 16px rgba(216, 90, 42, 0.35);
}

/* ==========================================================
   THANH NHỊP SỐNG CHỢ DÂN SINH (LIVE TICKER)
   ========================================================== */
.live-market-ticker {
  margin-top: 18px;
  background: #ffffff;
  border: 1px solid #ead8cd;
  border-radius: 14px;
  padding: 9px 14px;
  display: flex;
  align-items: center;
  gap: 14px;
  box-shadow: 0 4px 14px rgba(43, 27, 20, 0.04);
  overflow: hidden;
}
.ticker-label {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  background: #fff1eb;
  color: #c2410c;
  font-size: 11px;
  font-weight: 800;
  padding: 5px 12px;
  border-radius: 8px;
  white-space: nowrap;
  flex-shrink: 0;
  border: 1px solid #fed7aa;
}
.live-dot-red {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #ef4444;
  animation: blinkRed 1.4s infinite;
}
@keyframes blinkRed {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.35;
    transform: scale(1.25);
  }
}
.ticker-track-wrap {
  flex: 1;
  overflow: hidden;
  mask-image: linear-gradient(
    90deg,
    transparent,
    #000 4%,
    #000 96%,
    transparent
  );
}
.ticker-track {
  display: flex;
  align-items: center;
  gap: 36px;
  width: max-content;
  animation: tickerScroll 34s linear infinite;
}
.live-market-ticker:hover .ticker-track {
  animation-play-state: paused;
}
@keyframes tickerScroll {
  0% {
    transform: translate3d(0, 0, 0);
  }
  100% {
    transform: translate3d(-50%, 0, 0);
  }
}
.ticker-item {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 12.5px;
  color: #4b3830;
  white-space: nowrap;
}
.role-color-buyer {
  color: #16a34a;
}
.role-color-seller {
  color: #ea580c;
}
.role-color-shipper {
  color: #2563eb;
}
.ticker-time {
  font-size: 11px;
  color: #9c867b;
  background: #f8f2ec;
  padding: 2px 7px;
  border-radius: 6px;
}

/* ==========================================================
   2. DANH MỤC NỔI BẬT (3D CARDS)
   ========================================================== */
.categories-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: clamp(12px, 1.6vw, 20px);
}
.category-card-wrap {
  perspective: 900px;
  cursor: pointer;
}
.category-card {
  height: 100%;
  background: #ffffff;
  border-radius: 16px;
  overflow: hidden;
  border: 1px solid #ebdcd3;
  box-shadow: 0 6px 18px -4px rgba(43, 27, 20, 0.06);
  transition:
    transform 0.32s cubic-bezier(0.16, 1, 0.3, 1),
    box-shadow 0.32s ease,
    border-color 0.25s ease;
  display: flex;
  flex-direction: column;
}
.category-card-wrap:hover .category-card {
  transform: translateY(-6px) rotateX(3deg);
  box-shadow: 0 16px 30px -6px rgba(216, 90, 42, 0.18);
  border-color: #d85a2a;
}
.category-img-wrap {
  position: relative;
  height: 155px;
  overflow: hidden;
  background: #fdf3ed;
}
.category-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.45s cubic-bezier(0.16, 1, 0.3, 1);
}
.category-card-wrap:hover .category-img {
  transform: scale(1.08);
}
.category-img-gradient {
  position: absolute;
  inset: 0;
  background: linear-gradient(to top, rgba(35, 18, 10, 0.35), transparent 60%);
}
.category-tag {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(43, 27, 20, 0.78);
  backdrop-filter: blur(6px);
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 20px;
}
.category-info {
  padding: 15px;
  display: flex;
  flex-direction: column;
  flex-grow: 1;
}
.category-name {
  font-size: 14.5px;
  font-weight: 800;
  color: #2b1b14;
  margin: 0 0 4px 0;
  line-height: 1.3;
}
.category-desc {
  font-size: 12px;
  color: #7b6960;
  margin: 0 0 12px 0;
  line-height: 1.45;
  flex-grow: 1;
}
.category-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-top: 1px dashed #eddcd3;
  padding-top: 10px;
}
.category-count {
  font-size: 12px;
  font-weight: 700;
  color: #d85a2a;
}
.category-arrow {
  font-size: 11.5px;
  font-weight: 700;
  color: #8f796f;
  transition:
    color 0.2s,
    transform 0.2s;
}
.category-card-wrap:hover .category-arrow {
  color: #d85a2a;
  transform: translateX(3px);
}

/* ==========================================================
   3. SẢN PHẨM BÁN CHẠY (HOT PRODUCTS 3D)
   ========================================================== */
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
.product-card-wrap {
  perspective: 900px;
  cursor: pointer;
}
.product-card {
  height: 100%;
  background: #ffffff;
  border: 1px solid #eadad0;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 6px 18px -4px rgba(43, 27, 20, 0.06);
  transition:
    transform 0.3s cubic-bezier(0.16, 1, 0.3, 1),
    box-shadow 0.3s ease,
    border-color 0.25s ease;
  display: flex;
  flex-direction: column;
}
.product-card-wrap:hover .product-card {
  transform: translateY(-6px) rotateX(2.5deg);
  border-color: #d85a2a;
  box-shadow: 0 18px 32px -6px rgba(216, 90, 42, 0.18);
}
.product-img-wrap {
  position: relative;
  height: 175px;
  overflow: hidden;
  background: #fcf4ee;
}
.product-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s cubic-bezier(0.16, 1, 0.3, 1);
}
.product-card-wrap:hover .product-img {
  transform: scale(1.07);
}
.product-discount-tag {
  position: absolute;
  top: 10px;
  left: 10px;
  background: linear-gradient(135deg, #ea580c, #c2410c);
  color: #ffffff;
  font-size: 11px;
  font-weight: 800;
  padding: 3px 9px;
  border-radius: 8px;
  box-shadow: 0 4px 10px rgba(216, 90, 42, 0.35);
}
.product-status-tag {
  position: absolute;
  bottom: 8px;
  left: 8px;
  background: rgba(28, 16, 10, 0.78);
  backdrop-filter: blur(4px);
  color: #ffffff;
  font-size: 10.5px;
  font-weight: 600;
  padding: 3px 9px;
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
  font-size: 11px;
  font-weight: 700;
  margin-bottom: 6px;
}
.loc-badge {
  color: #c2410c;
  background: #fff2eb;
  padding: 2px 7px;
  border-radius: 5px;
}
.time-badge {
  color: #15803d;
  background: #eef8f2;
  padding: 2px 7px;
  border-radius: 5px;
}
.product-title {
  font-size: 14.5px;
  font-weight: 700;
  color: #2b1b14;
  margin: 0 0 4px 0;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  min-height: 39px;
}
.text-terracotta {
  color: #d85a2a;
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
  font-size: 15.5px;
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
  padding: 8px 13px;
  border-radius: 9px;
  cursor: pointer;
  transition: all 0.22s ease;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}
.btn-add-cart:hover {
  background: #d85a2a;
  transform: scale(1.05);
  box-shadow: 0 6px 14px rgba(216, 90, 42, 0.3);
}

/* ==========================================================
   4. HOW IT WORKS (4 BƯỚC KẾT NỐI 3D)
   ========================================================== */
.how-it-works-section {
  background: linear-gradient(180deg, #ffffff 0%, #fff9f5 100%);
  border: 1px solid #ebdcd3;
  border-radius: 22px;
  padding: 40px clamp(18px, 3vw, 38px);
  box-shadow: 0 10px 28px -6px rgba(43, 27, 20, 0.05);
}
.how-header {
  text-align: center;
  margin-bottom: 32px;
}
.how-badge {
  display: inline-block;
  font-size: 11.5px;
  font-weight: 800;
  letter-spacing: 0.8px;
  color: #d85a2a;
  background: #fdf2eb;
  padding: 4px 14px;
  border-radius: 20px;
  margin-bottom: 8px;
  border: 1px solid #f8dac9;
}
.text-center {
  text-align: center;
}
.how-steps-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
}
.step-card-wrap {
  perspective: 800px;
}
.step-card {
  height: 100%;
  position: relative;
  background: #ffffff;
  border: 1.5px solid #f2e2d8;
  border-radius: 16px;
  padding: 26px 18px;
  text-align: center;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  display: flex;
  flex-direction: column;
  align-items: center;
}
.step-card-wrap:hover .step-card {
  transform: translateY(-6px);
  border-color: #d85a2a;
  box-shadow: 0 14px 26px -6px rgba(216, 90, 42, 0.15);
}
.step-icon-wrap {
  position: relative;
  width: 64px;
  height: 64px;
  border-radius: 18px;
  background: #fff5ee;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 14px;
  transition: transform 0.3s ease;
}
.step-card-wrap:hover .step-icon-wrap {
  transform: scale(1.1) rotate(5deg);
  background: #d85a2a;
}
.step-icon {
  font-size: 30px;
  color: #d85a2a;
  transition: color 0.3s ease;
}
.step-card-wrap:hover .step-icon {
  color: #ffffff;
}
.step-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
}
.step-title-row {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 6px;
}
.step-number {
  position: absolute;
  top: 12px;
  right: 14px;
  font-size: 18px;
  font-weight: 800;
  color: #e8d0c2;
}
.step-title {
  font-size: 15.5px;
  font-weight: 800;
  color: #2b1b14;
  margin: 0;
}
.step-desc {
  font-size: 12.5px;
  color: #6b5a51;
  line-height: 1.52;
  margin: 0;
}

/* ==========================================================
   5. GIAN HÀNG TIÊU BIỂU (3D VENDOR CARDS)
   ========================================================== */
.vendors-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: clamp(14px, 2vw, 24px);
}
.vendor-card-wrap {
  perspective: 900px;
  cursor: pointer;
  opacity: 1 !important;
  visibility: visible !important;
  transform: translate3d(0, 0, 0) !important;
}
.vendor-card {
  height: 100%;
  background: #ffffff;
  border: 1px solid #eadad0;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 6px 18px -4px rgba(43, 27, 20, 0.06);
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  display: flex;
  flex-direction: column;
}
.vendor-card-wrap:hover .vendor-card {
  transform: translateY(-6px) rotateX(2.5deg);
  border-color: #d85a2a;
  box-shadow: 0 18px 32px -6px rgba(216, 90, 42, 0.18);
}
.vendor-cover-wrap {
  position: relative;
  height: 126px;
}
.vendor-cover {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.45s ease;
}
.vendor-card-wrap:hover .vendor-cover {
  transform: scale(1.06);
}
.vendor-cover-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(to top, rgba(25, 12, 6, 0.45), transparent 65%);
}
.vendor-live-tag {
  position: absolute;
  top: 10px;
  right: 10px;
  background: rgba(20, 12, 8, 0.72);
  backdrop-filter: blur(4px);
  color: #ffffff;
  font-size: 10.5px;
  font-weight: 700;
  padding: 3px 9px;
  border-radius: 20px;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}
.dot-online {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #4ade80;
}
.vendor-avatar-wrap {
  position: absolute;
  bottom: -24px;
  left: 18px;
  width: 56px;
  height: 56px;
  border-radius: 50%;
  border: 3px solid #ffffff;
  overflow: hidden;
  box-shadow: 0 6px 14px rgba(0, 0, 0, 0.18);
  background: #ffffff;
}
.vendor-avatar {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.vendor-body {
  padding: 32px 18px 18px 18px;
  display: flex;
  flex-direction: column;
  flex-grow: 1;
}
.vendor-name {
  font-size: 15.5px;
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
  font-weight: 700;
  color: #d85a2a;
  margin-bottom: 6px;
}
.vendor-stock {
  font-size: 12px;
  color: #6b5a51;
  margin: 0 0 14px 0;
}
.vendor-btn {
  margin-top: auto;
  width: 100%;
  background: #fdf2eb;
  color: #d85a2a;
  border: 1px solid #f6d8c8;
  font-size: 12.5px;
  font-weight: 700;
  padding: 9px 12px;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.22s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}
.vendor-card-wrap:hover .vendor-btn {
  background: #d85a2a;
  color: #ffffff;
  border-color: #d85a2a;
}

/* ==========================================================
   6. TESTIMONIALS (TIẾNG NÓI CỘNG ĐỒNG)
   ========================================================== */
.testimonials-section {
  background: #fffaf6;
  border: 1px solid #ebdcd3;
  border-radius: 22px;
  padding: 38px clamp(18px, 3vw, 38px);
}
.testimonials-header {
  display: flex !important;
  flex-direction: column !important;
  align-items: center !important;
  text-align: center !important;
  margin-bottom: 26px;
}
.testimonials-header .how-badge {
  margin-bottom: 6px;
}
.testimonials-header .section-title {
  margin: 0 0 6px 0;
  text-align: center;
}
.testimonials-header .section-subtitle {
  text-align: center;
  margin: 0 auto;
  max-width: 540px;
}
.testimonials-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}
.testimonial-card {
  position: relative;
  height: 100%;
  background: #ffffff;
  border: 1px solid #ebdcd3;
  border-radius: 16px;
  padding: 24px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 6px 16px -4px rgba(43, 27, 20, 0.05);
  transition: transform 0.28s ease, border-color 0.28s ease;
  overflow: hidden;
}
.testimonial-card-wrap:hover .testimonial-card {
  transform: translateY(-5px);
  border-color: #d85a2a;
}
.quote-watermark {
  position: absolute;
  top: 4px;
  right: 16px;
  font-family: Georgia, serif;
  font-size: 72px;
  line-height: 1;
  color: rgba(216, 90, 42, 0.08);
  pointer-events: none;
}
.stars-row {
  margin-bottom: 10px;
  font-size: 13px;
}
.testimonial-quote {
  font-size: 13.5px;
  line-height: 1.62;
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
  width: 44px;
  height: 44px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #fde3d3;
}
.author-name {
  font-size: 14px;
  font-weight: 700;
  color: #2b1b14;
  margin: 0 0 2px 0;
}
.author-role {
  font-size: 11.5px;
  color: #8c796f;
}

/* ==========================================================
   7. CTA PARTNER BANNER 3D
   ========================================================== */
.partner-cta-card {
  position: relative;
  background: linear-gradient(135deg, #2b1b14 0%, #432315 55%, #c84d1e 130%);
  border-radius: 22px;
  padding: 42px clamp(22px, 4vw, 52px);
  color: #ffffff;
  box-shadow: 0 16px 36px -6px rgba(43, 27, 20, 0.25);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 28px;
  overflow: hidden;
}
.cta-3d-glow {
  position: absolute;
  width: 320px;
  height: 320px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(251, 146, 60, 0.35), transparent 70%);
  right: -60px;
  top: -80px;
  pointer-events: none;
}
.partner-cta-content {
  position: relative;
  z-index: 2;
  max-width: 720px;
}
.cta-mini-tag {
  display: inline-block;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.8px;
  color: #ffd6c2;
  background: rgba(255, 255, 255, 0.14);
  padding: 4px 12px;
  border-radius: 20px;
  margin-bottom: 12px;
}
.cta-title {
  font-size: clamp(20px, 2.2vw, 28px);
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
  transition: all 0.25s ease;
  box-shadow: 0 6px 18px rgba(216, 90, 42, 0.45);
}
.btn-partner-seller:hover {
  background: #ea6431;
  transform: translateY(-2px);
}
.btn-partner-shipper {
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
  border: 1.5px solid rgba(255, 255, 255, 0.4);
  font-weight: 700;
  font-size: 13px;
  padding: 11px 22px;
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.25s ease;
}
.btn-partner-shipper:hover {
  background: rgba(255, 255, 255, 0.25);
  border-color: #ffffff;
}

.cta-visual-badge {
  position: relative;
  z-index: 2;
  flex-shrink: 0;
}
.cta-circle-outer {
  width: 148px;
  height: 148px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.1);
  border: 1.5px dashed rgba(255, 237, 213, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  animation: floatParticle 4.5s ease-in-out infinite;
}
.cta-circle-inner {
  width: 118px;
  height: 118px;
  border-radius: 50%;
  background: linear-gradient(135deg, #ea580c, #b43e12);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.3);
  text-align: center;
}
.cta-circle-inner i {
  font-size: 22px;
  color: #fde68a;
}
.cta-circle-inner span {
  font-size: 22px;
  font-weight: 900;
  line-height: 1.1;
}
.cta-circle-inner small {
  font-size: 10px;
  color: #ffedd5;
}

/* ==========================================================
   8. HIGHLIGHTS CAM KẾT 10KM
   ========================================================== */
.highlights-card {
  background: #fffaf7;
  border: 1.5px solid #f2dfd5;
  border-radius: 16px;
  padding: 22px 26px;
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
  font-size: 18px;
  font-weight: 800;
  color: #d85a2a;
  background: #fbeee7;
  width: 40px;
  height: 40px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.highlight-text h4 {
  font-size: 14.5px;
  font-weight: 700;
  margin: 0 0 3px 0;
  color: #2b1b14;
}
.highlight-text p {
  font-size: 12.5px;
  line-height: 1.45;
  color: #6d5b52;
  margin: 0;
}

/* ==========================================================
   RESPONSIVE TOÀN DIỆN (TABLET & MOBILE)
   ========================================================== */
@media (max-width: 1024px) {
  .hero-inner-grid {
    padding: 36px 20px 48px 20px;
  }
  .portals-section {
    margin-top: -12px;
  }
  .categories-grid,
  .products-grid,
  .how-steps-grid,
  .vendors-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .testimonials-grid,
  .highlights-card {
    grid-template-columns: 1fr;
  }
  .cta-visual-badge {
    display: none;
  }
}

@media (max-width: 860px) {
  .homepage-container {
    padding: 0 0 36px 0;
    width: 100%;
    max-width: 100vw;
    box-sizing: border-box;
    overflow-x: hidden;
  }
  .first-fold-wrapper {
    margin-top: 0;
    margin-bottom: 16px;
    width: 100%;
    box-sizing: border-box;
  }
  /* Tràn viền khớp đáy header 100% không dùng margin âm để tránh tràn và mất cạnh phải */
  .hero-section {
    margin: 0;
    width: 100%;
    box-sizing: border-box;
  }
  .hero-slider-container {
    border-radius: 0 0 18px 18px;
    box-shadow: 0 6px 20px -4px rgba(43, 27, 20, 0.16);
    width: 100%;
    box-sizing: border-box;
  }
  .hero-card {
    min-height: 165px;
    padding-bottom: 16px;
    border-radius: 0 0 18px 18px;
    background-position: center center;
    box-shadow: none;
    width: 100%;
    box-sizing: border-box;
  }
  .hero-overlay {
    background: linear-gradient(
      135deg,
      rgba(28, 12, 6, 0.88) 0%,
      rgba(100, 32, 10, 0.82) 55%,
      rgba(216, 90, 42, 0.72) 100%
    );
  }
  .hero-inner-grid {
    padding: 16px 14px 10px 14px;
    gap: 4px;
    max-width: 100%;
  }
  .hero-content {
    align-items: center;
    text-align: center;
    max-width: 100%;
  }
  .hero-badge-row {
    margin-bottom: 4px;
    padding: 3px 10px;
    border-radius: 20px;
    background: rgba(255, 255, 255, 0.18);
    border: 1px solid rgba(255, 255, 255, 0.32);
  }
  .hero-subheading {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.3px;
    color: #ffeedd;
  }
  .hero-title {
    font-size: clamp(16.5px, 4.6vw, 19.5px);
    font-weight: 850;
    line-height: 1.25;
    margin-bottom: 4px;
    letter-spacing: -0.2px;
    color: #ffffff;
    text-shadow: 0 2px 10px rgba(0, 0, 0, 0.4);
  }
  .hero-description {
    font-size: 11.5px;
    line-height: 1.4;
    margin-bottom: 10px;
    color: #ffede3;
    max-width: 95%;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
  /* Ẩn các badge chỉ số vụn vặt bên trong banner trên điện thoại để banner thoáng đãng, dễ đọc */
  .hero-mini-metrics {
    display: none !important;
  }
  .hero-actions {
    display: flex;
    justify-content: center;
    width: 100%;
    margin-top: 2px;
  }
  .btn-primary-warm {
    padding: 8px 24px;
    font-size: 12px;
    font-weight: 750;
    border-radius: 30px;
    background: linear-gradient(135deg, #f97316 0%, #ea580c 100%);
    box-shadow: 0 4px 14px rgba(234, 88, 12, 0.45);
    color: #ffffff;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    white-space: nowrap;
  }
  /* Ẩn nút phụ trên mobile để tập trung 1 nút CTA chủ đạo rõ ràng, không bị chật chội */
  .btn-secondary-warm {
    display: none !important;
  }
  .slider-nav-btn {
    display: none;
  }
  .slider-dots {
    bottom: 6px;
    padding: 2px 8px;
    border-radius: 12px;
    background: rgba(18, 8, 4, 0.55);
  }
  .portals-section {
    margin-top: 10px;
    padding: 0 12px;
    width: 100%;
    box-sizing: border-box;
  }
  .portals-grid {
    grid-template-columns: 1fr;
    gap: 8px;
    width: 100%;
    box-sizing: border-box;
  }
  .portal-card-3d-wrap {
    perspective: none;
  }
  .portal-card {
    padding: 10px 14px;
    display: flex;
    flex-direction: row;
    align-items: center;
    text-align: left;
    gap: 12px;
    background: #ffffff;
    border: 1px solid #f0ded5;
    border-radius: 14px;
    min-height: unset;
    box-shadow: 0 2px 8px rgba(43, 27, 20, 0.04);
    transition: background 0.15s ease, transform 0.15s ease;
  }
  .portal-card:active {
    background: #fdf5f0;
    transform: scale(0.99);
  }
  .card-glare {
    display: none;
  }
  .portal-avatar-box {
    width: 48px;
    height: 48px;
    margin-bottom: 0;
    flex-shrink: 0;
    border-radius: 12px;
    border: 1px solid #f7ded1;
    background: linear-gradient(135deg, #fff7f2 0%, #feeddd 100%);
    box-shadow: 0 2px 8px rgba(216, 90, 42, 0.12);
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .portal-model-avatar {
    width: 38px;
    height: 38px;
    object-fit: contain;
    filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.15));
  }
  .portal-card-main-content {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    justify-content: center;
    text-align: left;
  }
  .portal-role-badge {
    font-size: 9px;
    font-weight: 750;
    letter-spacing: 0.3px;
    padding: 1px 7px;
    border-radius: 10px;
    margin-bottom: 2px;
    display: inline-block;
    background: #fff1eb;
    color: #c2410c;
  }
  .portal-title {
    font-size: 13.5px;
    font-weight: 800;
    line-height: 1.25;
    margin-bottom: 2px;
    color: #2b1b14;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 100%;
  }
  .portal-tagline {
    font-size: 11px;
    font-weight: 600;
    line-height: 1.25;
    margin-bottom: 0;
    color: #8c6a59;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 100%;
  }
  .portal-desc {
    display: none;
  }
  /* Nút mũi tên tròn thanh lịch trên mobile, không chiếm diện tích chữ của card */
  .portal-btn {
    width: 34px;
    height: 34px;
    min-width: 34px;
    padding: 0;
    border-radius: 50%;
    background: #fff3ec;
    color: #ea580c;
    border: 1px solid #fed7aa;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    box-shadow: 0 2px 6px rgba(234, 88, 12, 0.12);
    transition: all 0.2s ease;
  }
  .portal-btn span {
    display: none;
  }
  .portal-btn i {
    margin: 0 !important;
    font-size: 16px;
    color: #ea580c;
  }
  .portal-card:active .portal-btn {
    background: #ea580c;
    color: #ffffff;
  }
  .portal-card:active .portal-btn i {
    color: #ffffff;
  }
  .live-market-ticker {
    flex-direction: row;
    align-items: center;
    gap: 8px;
    padding: 7px 12px;
    background: #ffffff;
    border: 1px solid #eedcd2;
    border-radius: 20px;
    margin: 10px 12px 0 12px;
    box-shadow: 0 2px 6px rgba(43, 27, 20, 0.04);
    box-sizing: border-box;
  }
  .ticker-label {
    padding: 2px 8px;
    font-size: 9.5px;
    font-weight: 800;
    white-space: nowrap;
    border-radius: 12px;
    flex-shrink: 0;
  }
  .ticker-track-wrap {
    flex: 1;
    width: auto;
    overflow: hidden;
  }
  /* Section Header Chung trên Mobile */
  .section-container {
    margin-bottom: 28px;
    padding: 0 12px;
    width: 100%;
    box-sizing: border-box;
  }
  .section-header {
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
    gap: 8px;
    width: 100%;
    box-sizing: border-box;
  }
  .header-left {
    flex: 1;
    min-width: 0;
  }
  .section-eyebrow {
    font-size: 9.5px;
    font-weight: 800;
    letter-spacing: 0.4px;
    color: #ea580c;
    margin-bottom: 2px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .section-title {
    font-size: 16.5px;
    font-weight: 850;
    color: #2b1b14;
    margin: 0;
    line-height: 1.25;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .section-subtitle {
    display: none; /* Ẩn mô tả dài để tiêu đề hiển thị trọn vẹn trên 1 dòng, không bị ngắt chữ */
  }
  .view-all-link {
    font-size: 11px;
    font-weight: 750;
    color: #ea580c;
    background: #fff3ec;
    border: 1px solid #fed7aa;
    padding: 4px 10px;
    border-radius: 20px;
    white-space: nowrap;
    flex-shrink: 0;
    text-decoration: none;
    transition: all 0.2s ease;
  }
  .view-all-link:active {
    background: #ea580c;
    color: #ffffff;
  }
  .view-all-extra {
    display: none; /* Rút gọn "Xem Tất Cả ➔" trên mobile */
  }

  /* Sạp hàng truyền thống & hiện đại (Danh mục nổi bật) trên Mobile */
  .categories-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
    width: 100%;
    box-sizing: border-box;
  }
  .category-card-wrap {
    perspective: none;
    min-width: 0;
    width: 100%;
    box-sizing: border-box;
  }
  .category-card {
    min-width: 0;
    width: 100%;
    border-radius: 14px;
    border: 1px solid #f0ded5;
    background: #ffffff;
    box-shadow: 0 2px 8px rgba(43, 27, 20, 0.05);
    transition: transform 0.15s ease;
    box-sizing: border-box;
  }
  .category-card:active {
    transform: scale(0.98);
  }
  .category-img-wrap {
    height: 98px;
    border-radius: 13px 13px 0 0;
  }
  .category-tag {
    top: 6px;
    left: 6px;
    font-size: 9px;
    font-weight: 750;
    padding: 2px 7px;
    border-radius: 10px;
    background: rgba(28, 12, 6, 0.72);
    backdrop-filter: blur(4px);
    color: #ffffff;
    border: 0.5px solid rgba(255, 255, 255, 0.25);
  }
  .category-info {
    padding: 8px 10px 10px 10px;
    display: flex;
    flex-direction: column;
    flex-grow: 1;
  }
  .category-name {
    font-size: 13px;
    font-weight: 800;
    color: #2b1b14;
    line-height: 1.25;
    margin: 0 0 2px 0;
    min-height: unset;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .category-desc {
    font-size: 10.5px;
    color: #8c776c;
    line-height: 1.3;
    margin: 0 0 6px 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .category-footer {
    border-top: 1px solid #f6ebe3;
    padding-top: 6px;
    margin-top: auto;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  .category-count {
    font-size: 10.5px;
    font-weight: 750;
    color: #ea580c;
    white-space: nowrap;
    display: flex;
    align-items: center;
    gap: 3px;
  }
  .category-arrow {
    font-size: 11px;
    font-weight: 800;
    color: #ea580c;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: #fff3ec;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }
  .category-arrow .arrow-text {
    display: none; /* Ẩn chữ "Khám phá" chỉ giữ icon mũi tên ➔ để không bị vỡ 2 dòng */
  }
  .category-card:active .category-arrow {
    background: #ea580c;
    color: #ffffff;
  }

  /* Sản phẩm & Nhà vườn chung grid 2 cột */
  .products-grid,
  .vendors-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
    width: 100%;
    box-sizing: border-box;
  }
  .product-card-wrap,
  .vendor-card-wrap {
    min-width: 0;
    width: 100%;
    box-sizing: border-box;
  }
  .product-card,
  .vendor-card {
    min-width: 0;
    width: 100%;
    box-sizing: border-box;
  }
  .product-img-wrap {
    height: 125px;
  }
  .product-body {
    padding: 10px 8px;
  }
  .product-title {
    font-size: 12.5px;
    min-height: 32px;
  }
  .product-price-row {
    flex-direction: column;
    align-items: stretch;
    gap: 6px;
  }
  .btn-add-cart {
    width: 100%;
    justify-content: center;
  }
  .how-steps-grid {
    grid-template-columns: 1fr;
    gap: 10px;
  }
  .step-card {
    flex-direction: row;
    text-align: left;
    padding: 14px;
    gap: 12px;
  }
  .step-icon-wrap {
    width: 44px;
    height: 44px;
    margin-bottom: 0;
    flex-shrink: 0;
  }
  .step-content {
    align-items: flex-start;
  }
  .step-title-row {
    width: 100%;
    justify-content: space-between;
  }
  .step-number {
    position: static;
    font-size: 13px;
  }
  .vendor-cover-wrap {
    height: 80px;
  }
  .vendor-avatar-wrap {
    width: 38px;
    height: 38px;
    bottom: -16px;
    left: 8px;
  }
  .vendor-body {
    padding: 22px 8px 8px 8px;
  }
  .vendor-name {
    font-size: 12.5px;
  }
  .testimonials-grid {
    display: flex;
    gap: 12px;
    overflow-x: auto;
    scroll-snap-type: x mandatory;
    padding-bottom: 6px;
    scrollbar-width: none;
  }
  .testimonials-grid::-webkit-scrollbar {
    display: none;
  }
  .testimonial-card-wrap {
    flex: 0 0 82vw;
    scroll-snap-align: start;
  }
  .btn-partner-seller,
  .btn-partner-shipper {
    width: 100%;
    text-align: center;
  }
}

.home-toast-notice {
  position: fixed;
  bottom: 28px;
  right: 28px;
  z-index: 999999;
  background: #0f172a;
  color: #f8fafc;
  padding: 12px 22px;
  border-radius: 12px;
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.25);
  display: flex;
  align-items: center;
  font-size: 14px;
  font-weight: 600;
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.toast-fade-enter-active,
.toast-fade-leave-active {
  transition:
    opacity 0.3s ease,
    transform 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}
.toast-fade-enter-from,
.toast-fade-leave-to {
  opacity: 0;
  transform: translateY(12px) scale(0.96);
}
</style>