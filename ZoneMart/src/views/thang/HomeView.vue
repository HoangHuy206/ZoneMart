<template>
  <div class="zilly-store font-sans text-slate-800 antialiased">
    <!-- FLOATING CART WIDGET (ON RIGHT EDGE) -->
    <div class="floating-cart-badge" @click="router.push('/cart')" title="Xem giỏ hàng">
      <div class="floating-cart-icon"><i class="bi bi-bag-check-fill"></i></div>
      <div class="floating-cart-info">
        <span class="floating-count">{{ cartCount }} Item</span>
        <span class="floating-price">$0.00</span>
      </div>
    </div>

    <!-- 1. TOP BAR -->
    <div class="top-bar">
      <div class="container-fluid top-bar-content">
        <div class="top-left">
          <span><i class="bi bi-geo-alt"></i> 23/A Mark Street Road, Da Nang City</span>
          <span class="divider">|</span>
          <span><i class="bi bi-envelope"></i> info@zonemart.com</span>
        </div>
        <div class="top-center">
          <button class="arrow-btn"><i class="bi bi-chevron-left"></i></button>
          <span class="promo-text">
            Try <strong>ZoneMart</strong> for free <span class="highlight-yellow" @click="router.push('/register-seller')">Open store right now</span>
          </span>
          <button class="arrow-btn"><i class="bi bi-chevron-right"></i></button>
        </div>
        <div class="top-right">
          <span>English <i class="bi bi-chevron-down"></i></span>
          <span class="divider">|</span>
          <span>USD <i class="bi bi-chevron-down"></i></span>
        </div>
      </div>
    </div>

    <!-- 2. MAIN HEADER (LOGO, SEARCH, ACTIONS) -->
    <header class="main-header">
      <div class="container-fluid header-container">
        <!-- Logo -->
        <router-link to="/" class="logo-brand">
          <div class="logo-icon">
            <svg viewBox="0 0 36 36" fill="none" class="bag-svg">
              <path d="M7 11H29L26 31H10L7 11Z" fill="#ba441b" />
              <path d="M13 11V7C13 4.79086 14.7909 3 17 3H19C21.2091 3 23 4.79086 23 7V11" stroke="#9a3412" stroke-width="3" stroke-linecap="round" />
              <circle cx="18" cy="18" r="4" fill="#fbbf24" />
            </svg>
          </div>
          <span class="logo-text">Zone<span class="brand-sub">Mart</span></span>
        </router-link>

        <!-- Search Bar with Category Select -->
        <div class="search-box-wrapper">
          <div class="category-dropdown" @click="toggleCategoryDropdown">
            <span>{{ selectedCategory }}</span>
            <i class="bi bi-chevron-down ms-2"></i>
            <div class="dropdown-list" v-if="showCategoryDropdown">
              <div 
                v-for="cat in searchCategories" 
                :key="cat" 
                class="dropdown-item"
                @click.stop="selectCategory(cat)"
              >
                {{ cat }}
              </div>
            </div>
          </div>
          <input 
            type="text" 
            v-model="searchQuery" 
            placeholder="Type Your Products ..." 
            class="search-input"
            @keyup.enter="handleSearch"
          />
          <button class="search-btn" @click="handleSearch">
            <span>Search</span>
            <i class="bi bi-search ms-2"></i>
          </button>
        </div>

        <!-- User Actions -->
        <div class="header-actions">
          <router-link to="/profile" class="action-item" title="Tài khoản">
            <i class="bi bi-person"></i>
          </router-link>
          <div class="action-item" title="Yêu thích">
            <i class="bi bi-heart"></i>
            <span class="badge-count">{{ wishlistCount }}</span>
          </div>
          <router-link to="/cart" class="action-item" title="Giỏ hàng">
            <i class="bi bi-bag"></i>
            <span class="badge-count">{{ cartCount }}</span>
          </router-link>
          <button class="menu-toggle-btn" title="Menu">
            <i class="bi bi-list"></i>
          </button>
        </div>
      </div>
    </header>

    <!-- 3. NAVIGATION BAR -->
    <nav class="nav-bar">
      <div class="container-fluid nav-container">
        <ul class="nav-links">
          <li class="nav-item has-dropdown">
            <router-link to="/" class="nav-link active">Home <i class="bi bi-chevron-down"></i></router-link>
          </li>
          <li class="nav-item has-dropdown">
            <router-link to="/products" class="nav-link">Pages <i class="bi bi-chevron-down"></i></router-link>
          </li>
          <li class="nav-item has-dropdown">
            <router-link to="/products" class="nav-link">Shop <i class="bi bi-chevron-down"></i></router-link>
          </li>
          <li class="nav-item has-dropdown">
            <router-link to="/register-seller" class="nav-link">Vendor <i class="bi bi-chevron-down"></i></router-link>
          </li>
          <li class="nav-item has-dropdown">
            <router-link to="/shipper" class="nav-link">Elements <i class="bi bi-chevron-down"></i></router-link>
          </li>
          <li class="nav-item has-dropdown">
            <router-link to="/map" class="nav-link">Blog <i class="bi bi-chevron-down"></i></router-link>
          </li>
          <li class="nav-item">
            <router-link to="/contact" class="nav-link">Contact</router-link>
          </li>
        </ul>

        <div class="nav-right">
          <div class="weekly-discount">
            <i class="bi bi-percent discount-icon"></i>
            <span>Weekly Discount!</span>
          </div>
          <div class="hotline-badge">
            <i class="bi bi-telephone-fill"></i>
            <div>
              <span class="hotline-label">Hotline Number</span>
              <span class="hotline-number">+9888-256-666</span>
            </div>
          </div>
        </div>
      </div>
    </nav>

    <!-- 4. CATEGORIES HORIZONTAL BAR (7 IN 1 ROW - FULL WIDTH) -->
    <section class="category-strip">
      <div class="container-fluid">
        <div class="category-list">
          <div 
            v-for="(cat, index) in categoryPills" 
            :key="index" 
            class="category-pill-card"
            @click="router.push('/products')"
          >
            <div class="pill-icon-wrap" :style="{ backgroundColor: cat.bgColor }">
              <img :src="cat.image" :alt="cat.name" class="pill-img" />
            </div>
            <div class="pill-info">
              <h4 class="pill-title">{{ cat.name }}</h4>
              <p class="pill-subtitle">{{ cat.productsCount }} Products</p>
            </div>
            <span class="pill-dots"><i class="bi bi-three-dots-vertical"></i></span>
          </div>
        </div>
      </div>
    </section>

    <!-- 5. HERO / BANNER GRID -->
    <section class="hero-banners-section">
      <div class="container-fluid">
        <div class="banners-grid">
          <!-- Main Left Big Banner -->
          <div class="banner-large" style="background-image: url('https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=1200&q=80')">
            <div class="banner-overlay"></div>
            <div class="banner-content">
              <div class="tag-ribbon">100% Farm Fresh Food</div>
              <h1 class="hero-title">
                Fresh <span class="cursive-text">Organic</span>
              </h1>
              <h2 class="hero-subtitle">Food For All</h2>
              <div class="hero-price">$59.00</div>
              <button class="btn-shop-green" @click="shopNow('Fresh Organic Food')">
                Shop Now
              </button>
            </div>
          </div>

          <!-- Right Grid (3 smaller cards) -->
          <div class="banners-right-group">
            <!-- Top Right: Premium Honeynuts -->
            <div class="banner-card banner-honeynuts">
              <div class="card-text">
                <h3>Premium Honeynuts</h3>
                <p>100% Salted Organic Nuts</p>
                <div class="card-price">$15.00</div>
                <button class="btn-shop-pill" @click="shopNow('Premium Honeynuts')">Shop Now</button>
              </div>
              <div class="card-img-wrap">
                <img src="https://images.unsplash.com/photo-1536599018102-9f803c140fc1?auto=format&fit=crop&w=400&q=80" alt="Honeynuts" />
              </div>
            </div>

            <!-- Bottom Row: 2 Small Cards -->
            <div class="banner-bottom-row">
              <!-- Diaper card -->
              <div class="banner-small-card diaper-card">
                <div class="card-text">
                  <h4>New Baby Diaper</h4>
                  <p>Top Quality Product</p>
                  <button class="btn-shop-pill btn-white" @click="shopNow('Baby Diaper')">Shop Now</button>
                </div>
                <img src="https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=300&q=80" alt="Baby Diaper" class="diaper-img" />
              </div>

              <!-- Face wash card -->
              <div class="banner-small-card facewash-card">
                <div class="discount-circle">
                  <span>15%</span>
                  <small>OFF</small>
                </div>
                <div class="card-text">
                  <h4>Dark wash FaceWash</h4>
                  <p>All Fixed Size</p>
                  <button class="btn-shop-pill btn-white" @click="shopNow('FaceWash')">Shop Now</button>
                </div>
                <img src="https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=300&q=80" alt="Face Wash" class="facewash-img" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 6. FEATURED PRODUCTS SECTION -->
    <section class="featured-products-section">
      <div class="container-fluid">
        <!-- Header & Filter Tabs -->
        <div class="section-header-flex">
          <h2 class="section-heading">Featured Products</h2>
          <div class="filter-tabs-wrapper">
            <ul class="filter-tabs">
              <li 
                v-for="tab in filterTabs" 
                :key="tab"
                :class="{ active: currentTab === tab }"
                @click="currentTab = tab"
              >
                {{ tab }}
              </li>
            </ul>
            <div class="slider-nav-arrows">
              <button class="slider-btn" @click="prevProductSlide"><i class="bi bi-chevron-left"></i></button>
              <button class="slider-btn" @click="nextProductSlide"><i class="bi bi-chevron-right"></i></button>
            </div>
          </div>
        </div>

        <!-- Products Grid (6 cards) -->
        <div class="products-grid">
          <div 
            v-for="product in filteredProducts" 
            :key="product.id" 
            class="product-card"
          >
            <div class="product-top-bar">
              <span class="prod-category">{{ product.category }}</span>
              <button class="btn-wishlist" @click="toggleWishlist(product)">
                <i :class="product.isLiked ? 'bi bi-heart-fill text-danger' : 'bi bi-heart'"></i>
              </button>
            </div>

            <div class="product-thumb" @click="router.push('/products')">
              <img :src="product.image" :alt="product.name" />
            </div>

            <div class="product-tags">
              <span v-for="tag in product.weights" :key="tag" class="weight-badge">{{ tag }}</span>
            </div>

            <div class="product-pricing">
              <span class="price-val">{{ product.price }}</span>
              <span v-if="product.oldPrice" class="old-price">{{ product.oldPrice }}</span>
              <span v-if="product.discount" class="discount-badge">{{ product.discount }}</span>
            </div>

            <h3 class="product-title" :title="product.name" @click="router.push('/products')">{{ product.name }}</h3>

            <div class="product-rating">
              <div class="stars">
                <i v-for="star in 5" :key="star" class="bi bi-star-fill" :class="{ 'star-active': star <= Math.floor(product.rating) }"></i>
              </div>
              <span class="rating-num">({{ product.rating.toFixed(2) }})</span>
            </div>

            <button class="btn-select-options" @click="addToCart(product)">
              <i class="bi bi-basket me-2"></i> Select Options
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- 7. PROMO MIDDLE STRIP BANNER -->
    <section class="promo-strip-section">
      <div class="container-fluid">
        <div class="promo-banner-container">
          <div class="promo-text-area">
            <span class="sub-text">The brand New Collection Shea</span>
            <h3 class="main-text">Nourshing Women Body Lotions</h3>
          </div>
          
          <div class="pricing-start-badge">
            <span class="label">Our Pricing<br>Start</span>
            <div class="price-bubble">$45.00</div>
          </div>

          <div class="promo-bottles-img">
            <img src="https://images.unsplash.com/photo-1608248597359-57e3f890cf28?auto=format&fit=crop&w=600&q=80" alt="Body Lotions Collection" />
          </div>
        </div>
      </div>
    </section>

    <!-- 8. TOP SELLER USERS SECTION -->
    <section class="top-sellers-section">
      <div class="container-fluid">
        <div class="sellers-header">
          <div class="d-flex align-items-center gap-3">
            <h2 class="section-heading">Top Seller Users</h2>
            <div class="header-line"></div>
          </div>
          <router-link to="/products" class="see-more-link">See More <i class="bi bi-chevron-right ms-1"></i></router-link>
        </div>

        <div class="sellers-grid">
          <div 
            v-for="seller in sellers" 
            :key="seller.id" 
            class="seller-card"
          >
            <div class="seller-avatar">
              <img :src="seller.avatar" :alt="seller.name" />
            </div>
            <div class="seller-info">
              <span v-if="seller.isFeatured" class="badge-featured">Featured</span>
              <h4 class="seller-name">{{ seller.name }}</h4>
              <div class="stars">
                <i v-for="s in 5" :key="s" class="bi bi-star-fill star-active"></i>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 9. DEAL OF THE WEEK SECTION -->
    <section class="deal-of-week-section">
      <div class="container-fluid">
        <div class="deal-wrapper-box">
          <!-- Deal Header with Countdown Timer -->
          <div class="deal-header">
            <h2 class="deal-title">Deal Of The Week</h2>
            <div class="countdown-group">
              <div class="time-box">
                <span class="time-num">{{ countdown.days }}</span>
                <span class="time-unit">Day</span>
              </div>
              <div class="time-box">
                <span class="time-num">{{ countdown.hours }}</span>
                <span class="time-unit">Hr</span>
              </div>
              <div class="time-box">
                <span class="time-num">{{ countdown.mins }}</span>
                <span class="time-unit">Min</span>
              </div>
              <div class="time-box">
                <span class="time-num">{{ countdown.secs }}</span>
                <span class="time-unit">Secs</span>
              </div>
            </div>
          </div>

          <!-- Deal Content Grid -->
          <div class="deal-items-grid">
            <!-- Left Promo Banner with Veggie -->
            <div class="deal-left-banner">
              <div class="deal-left-content">
                <span class="deal-badge-organic">100% Organic</span>
                <h3>Fresh Organic & Healthy Food</h3>
                <p>Save up to 40% on organic farm vegetables</p>
                <button class="btn-deal-shop" @click="shopNow('Deal of Week')">Explore Deal</button>
              </div>
              <img src="https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=500&q=80" alt="Deal Organic" class="deal-hero-img" />
            </div>

            <!-- Deal Product Card 1 -->
            <div class="product-card deal-card">
              <div class="product-top-bar">
                <span class="prod-category">Beverage</span>
                <button class="btn-wishlist"><i class="bi bi-heart"></i></button>
              </div>
              <div class="product-thumb" @click="router.push('/products')">
                <img src="https://images.unsplash.com/photo-1566478989037-eec170784d0b?auto=format&fit=crop&w=300&q=80" alt="Lay's Classic" />
              </div>
              <div class="product-pricing">
                <span class="price-val">$12.00</span>
                <span class="old-price">$21.00</span>
              </div>
              <h3 class="product-title">Delicious Lay's Potato Chips, Classic, 8 oz Bag</h3>
              <div class="product-rating">
                <div class="stars">
                  <i v-for="star in 5" :key="star" class="bi bi-star-fill" :class="{ 'star-active': star <= 4 }"></i>
                </div>
                <span class="rating-num">(4.00)</span>
              </div>
              <button class="btn-select-options mt-2" @click="cartCount++">
                <i class="bi bi-basket me-2"></i> Add to Cart
              </button>
            </div>

            <!-- Deal Product Card 2 -->
            <div class="product-card deal-card">
              <div class="product-top-bar">
                <span class="prod-category">Beverage</span>
                <button class="btn-wishlist"><i class="bi bi-heart"></i></button>
              </div>
              <div class="product-thumb" @click="router.push('/products')">
                <img src="https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=300&q=80" alt="SunChips Minis" />
              </div>
              <div class="product-pricing">
                <span class="price-val">$22.00</span>
              </div>
              <h3 class="product-title">SunChips Minis, Garden Salsa Flavored Caniste...</h3>
              <div class="product-rating">
                <div class="stars">
                  <i v-for="star in 5" :key="star" class="bi bi-star-fill" :class="{ 'star-active': star <= 4 }"></i>
                </div>
                <span class="rating-num">(4.00)</span>
              </div>
              <button class="btn-select-options mt-2" @click="cartCount++">
                <i class="bi bi-basket me-2"></i> Add to Cart
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();

// Top Bar & State
const searchQuery = ref('');
const selectedCategory = ref('All Categories');
const showCategoryDropdown = ref(false);
const wishlistCount = ref(0);
const cartCount = ref(0);

const searchCategories = [
  'All Categories',
  'Vegetables',
  'Fresh Fruits',
  'Desserts',
  'Drinks & Juice',
  'Fish & Meats',
  'Pets & Animals',
  'Beverage'
];

const toggleCategoryDropdown = () => {
  showCategoryDropdown.value = !showCategoryDropdown.value;
};

const selectCategory = (cat: string) => {
  selectedCategory.value = cat;
  showCategoryDropdown.value = false;
};

const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({ path: '/products', query: { q: searchQuery.value } });
  } else {
    router.push('/products');
  }
};

// Horizontal Categories Strip (7 Items)
const categoryPills = ref([
  {
    name: 'Vegetables',
    productsCount: 6,
    image: 'https://images.unsplash.com/photo-1518977676601-b53f82aba655?auto=format&fit=crop&w=150&q=80',
    bgColor: '#fff7ed'
  },
  {
    name: 'Fresh Fruits',
    productsCount: 8,
    image: 'https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=150&q=80',
    bgColor: '#ffedd5'
  },
  {
    name: 'Desserts',
    productsCount: 9,
    image: 'https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=150&q=80',
    bgColor: '#fdf2f8'
  },
  {
    name: 'Drinks & Juice',
    productsCount: 6,
    image: 'https://images.unsplash.com/photo-1600271886742-f049cd451bba?auto=format&fit=crop&w=150&q=80',
    bgColor: '#fef3ee'
  },
  {
    name: 'Fish & Meats',
    productsCount: 6,
    image: 'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=150&q=80',
    bgColor: '#eff6ff'
  },
  {
    name: 'Pets & Animals',
    productsCount: 4,
    image: 'https://images.unsplash.com/photo-1583511655857-d19b40a7a54e?auto=format&fit=crop&w=150&q=80',
    bgColor: '#fffbeb'
  },
  {
    name: 'Beverage',
    productsCount: 8,
    image: 'https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=150&q=80',
    bgColor: '#faf5ff'
  }
]);

// Featured Products Section
const filterTabs = ['All', 'Desserts', 'Vegetables', 'Beverage'];
const currentTab = ref('All');

const products = ref([
  {
    id: 1,
    category: 'Vegetables',
    name: 'Russet Idaho Potatoes Fresh Premium Fruit and Produc...',
    image: 'https://images.unsplash.com/photo-1518977676601-b53f82aba655?auto=format&fit=crop&w=400&q=80',
    weights: ['100gm', '500gm'],
    price: '$30.00 - $38.00',
    oldPrice: undefined,
    discount: '-16%',
    rating: 5.00,
    isLiked: false
  },
  {
    id: 2,
    category: 'Desserts',
    name: 'Aptamil Gold+ ProNutra Biotik Stage 1 Infant...',
    image: 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?auto=format&fit=crop&w=400&q=80',
    weights: ['100gm', '375ml'],
    price: '$25.00 - $30.00',
    oldPrice: undefined,
    discount: '-44%',
    rating: 5.00,
    isLiked: false
  },
  {
    id: 3,
    category: 'Vegetables',
    name: 'Whole Foods Market, Organic Trimmed Green...',
    image: 'https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=400&q=80',
    weights: ['100gm', '500gm'],
    price: '$3.00 - $8.00',
    oldPrice: undefined,
    discount: '-77%',
    rating: 5.00,
    isLiked: false
  },
  {
    id: 4,
    category: 'Vegetables',
    name: 'Whole Foods Market, Romaine Hearts Salad Bag...',
    image: 'https://images.unsplash.com/photo-1556801712-76c8eb07bbc9?auto=format&fit=crop&w=400&q=80',
    weights: ['100gm'],
    price: '$19.00',
    oldPrice: '$22.00',
    discount: '-14%',
    rating: 5.00,
    isLiked: false
  },
  {
    id: 5,
    category: 'Beverage',
    name: 'Red Rock Deli Style Potato Chips, Lime & Cracked...',
    image: 'https://images.unsplash.com/photo-1566478989037-eec170784d0b?auto=format&fit=crop&w=400&q=80',
    weights: ['100gm'],
    price: '$34.00',
    oldPrice: '$45.00',
    discount: '-24%',
    rating: 5.00,
    isLiked: false
  },
  {
    id: 6,
    category: 'Vegetables',
    name: 'Fresh and Sweet Watermelon Delights for...',
    image: 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=400&q=80',
    weights: ['375ml', '500gm'],
    price: '$18.00 - $45.00',
    oldPrice: undefined,
    discount: '-33%',
    rating: 4.00,
    isLiked: false
  }
]);

const filteredProducts = computed(() => {
  if (currentTab.value === 'All') return products.value;
  return products.value.filter(p => p.category.toLowerCase() === currentTab.value.toLowerCase());
});

const toggleWishlist = (product: any) => {
  product.isLiked = !product.isLiked;
  if (product.isLiked) wishlistCount.value++;
  else wishlistCount.value = Math.max(0, wishlistCount.value - 1);
};

const addToCart = (_product: any) => {
  cartCount.value++;
};

const shopNow = (_title: string) => {
  router.push('/products');
};

const prevProductSlide = () => {
  const first = products.value.shift();
  if (first) products.value.push(first);
};

const nextProductSlide = () => {
  const last = products.value.pop();
  if (last) products.value.unshift(last);
};

// Top Sellers Data
const sellers = ref([
  {
    id: 1,
    name: 'Eleanor Pena',
    isFeatured: false,
    avatar: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 2,
    name: 'Dianne Russell',
    isFeatured: true,
    avatar: 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 3,
    name: 'Michel Richard',
    isFeatured: false,
    avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 4,
    name: 'Marvin McKinney',
    isFeatured: false,
    avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=160&q=80'
  }
]);

// Countdown Timer logic
const countdown = ref({
  days: '04',
  hours: '18',
  mins: '35',
  secs: '42'
});

let timerInterval: any = null;
let totalSecs = 4 * 86400 + 18 * 3600 + 35 * 60 + 42;

const updateCountdown = () => {
  if (totalSecs <= 0) {
    if (timerInterval) clearInterval(timerInterval);
    return;
  }
  totalSecs--;
  const d = Math.floor(totalSecs / 86400);
  const h = Math.floor((totalSecs % 86400) / 3600);
  const m = Math.floor((totalSecs % 3600) / 60);
  const s = totalSecs % 60;

  countdown.value.days = d < 10 ? '0' + d : String(d);
  countdown.value.hours = h < 10 ? '0' + h : String(h);
  countdown.value.mins = m < 10 ? '0' + m : String(m);
  countdown.value.secs = s < 10 ? '0' + s : String(s);
};

onMounted(() => {
  timerInterval = setInterval(updateCountdown, 1000);
});

onUnmounted(() => {
  if (timerInterval) clearInterval(timerInterval);
});
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&family=Great+Vibes&display=swap');
@import url('https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css');

/* Global Container Styles */
.zilly-store {
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  color: #2b3445;
  background-color: #ffffff;
  min-height: 100vh;
  width: 100%;
  overflow-x: hidden;
  position: relative;
}

/* FLUID CONTAINER: NO BOXED LIMIT, EXPANDS FULL WIDTH LIKE THE REAL SITE */
.container-fluid {
  width: 100%;
  max-width: 100%;
  padding: 0 clamp(20px, 2.6vw, 52px);
  box-sizing: border-box;
}

/* FLOATING CART WIDGET ON RIGHT EDGE */
.floating-cart-badge {
  position: fixed;
  right: 0;
  top: 55%;
  transform: translateY(-50%);
  background: #ba441b;
  color: #ffffff;
  z-index: 999;
  border-radius: 12px 0 0 12px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  box-shadow: -4px 6px 18px rgba(186, 68, 27, 0.35);
  transition: transform 0.2s ease, background 0.2s ease;
}

.floating-cart-badge:hover {
  background: #9a3412;
  transform: translateY(-50%) translateX(-4px);
}

.floating-cart-icon {
  font-size: 1.4rem;
}

.floating-cart-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  font-size: 0.75rem;
  font-weight: 800;
  line-height: 1.2;
}

.floating-price {
  color: #ffedd5;
  font-size: 0.8rem;
}

/* 1. TOP BAR */
.top-bar {
  background-color: #ba441b;
  color: #ffedd5;
  font-size: 0.84rem;
  padding: 8px 0;
}

.top-bar-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.top-left, .top-right {
  display: flex;
  align-items: center;
  gap: 14px;
}

.top-left i {
  color: #ffedd5;
}

.divider {
  color: rgba(255, 255, 255, 0.3);
}

.top-center {
  display: flex;
  align-items: center;
  gap: 8px;
}

.arrow-btn {
  background: transparent;
  border: none;
  color: #ffedd5;
  cursor: pointer;
  padding: 2px 4px;
  transition: color 0.2s;
}

.arrow-btn:hover {
  color: #ffffff;
}

.promo-text {
  color: #ffffff;
}

.highlight-yellow {
  color: #ffedd5;
  font-weight: 700;
  text-decoration: underline;
  margin-left: 4px;
  cursor: pointer;
}

/* 2. MAIN HEADER */
.main-header {
  background-color: #ffffff;
  padding: 16px 0;
  border-bottom: 1px solid #f1f5f9;
}

.header-container {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 28px;
}

.logo-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
  flex-shrink: 0;
}

.bag-svg {
  width: 38px;
  height: 38px;
}

.logo-text {
  font-size: 1.85rem;
  font-weight: 800;
  color: #1e293b;
  letter-spacing: -0.5px;
}

.brand-sub {
  color: #ba441b;
}

.search-box-wrapper {
  flex: 1;
  max-width: 680px;
  display: flex;
  align-items: center;
  background-color: #ffffff;
  border: 2px solid #ba441b;
  border-radius: 50px;
  padding: 3px 4px 3px 18px;
  position: relative;
}

.category-dropdown {
  position: relative;
  font-size: 0.88rem;
  font-weight: 600;
  color: #374151;
  cursor: pointer;
  padding-right: 14px;
  border-right: 1px solid #e5e7eb;
  white-space: nowrap;
  user-select: none;
}

.dropdown-list {
  position: absolute;
  top: 130%;
  left: 0;
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0,0,0,0.1);
  padding: 8px 0;
  z-index: 100;
  min-width: 170px;
  border: 1px solid #e5e7eb;
}

.dropdown-item {
  padding: 8px 16px;
  font-size: 0.85rem;
  transition: all 0.2s;
}

.dropdown-item:hover {
  background-color: #fff7ed;
  color: #ba441b;
}

.search-input {
  flex: 1;
  border: none;
  outline: none;
  padding: 8px 16px;
  font-size: 0.92rem;
  color: #374151;
}

.search-btn {
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  font-weight: 700;
  font-size: 0.9rem;
  padding: 10px 24px;
  border-radius: 50px;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: background-color 0.2s;
}

.search-btn:hover {
  background-color: #9a3412;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-shrink: 0;
}

.action-item {
  position: relative;
  width: 42px;
  height: 42px;
  border-radius: 50%;
  border: 1px solid #e5e7eb;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #374151;
  font-size: 1.25rem;
  text-decoration: none;
  transition: all 0.2s;
}

.action-item:hover {
  border-color: #ba441b;
  color: #ba441b;
  background-color: #fff7ed;
}

.badge-count {
  position: absolute;
  top: -4px;
  right: -4px;
  background-color: #ba441b;
  color: #ffffff;
  font-size: 0.68rem;
  font-weight: 800;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.menu-toggle-btn {
  background: transparent;
  border: none;
  font-size: 1.5rem;
  color: #374151;
  cursor: pointer;
}

/* 3. NAVIGATION BAR */
.nav-bar {
  background-color: #ffffff;
  border-bottom: 1px solid #f1f5f9;
  padding: 8px 0;
}

.nav-container {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.nav-links {
  display: flex;
  list-style: none;
  margin: 0;
  padding: 0;
  gap: 32px;
}

.nav-link {
  text-decoration: none;
  color: #374151;
  font-weight: 600;
  font-size: 0.94rem;
  display: flex;
  align-items: center;
  gap: 4px;
  transition: color 0.2s;
}

.nav-link.active, .nav-link:hover {
  color: #ba441b;
}

.nav-right {
  display: flex;
  align-items: center;
  gap: 24px;
}

.weekly-discount {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.9rem;
  font-weight: 700;
  color: #ba441b;
}

.discount-icon {
  background-color: #fff7ed;
  color: #ba441b;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
}

.hotline-badge {
  background-color: #ba441b;
  color: #ffffff;
  padding: 6px 18px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 10px;
}

.hotline-badge i {
  font-size: 1.2rem;
}

.hotline-label {
  display: block;
  font-size: 0.65rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  opacity: 0.85;
}

.hotline-number {
  font-weight: 700;
  font-size: 0.92rem;
}

/* 4. CATEGORIES HORIZONTAL BAR (7 ITEMS IN 1 FULL-WIDTH ROW) */
.category-strip {
  padding: 24px 0 16px;
}

.category-list {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 14px;
  width: 100%;
}

@media (max-width: 1200px) {
  .category-list {
    grid-template-columns: repeat(4, 1fr);
  }
}

@media (max-width: 768px) {
  .category-list {
    grid-template-columns: repeat(2, 1fr);
  }
}

.category-pill-card {
  background-color: #ffffff;
  border-radius: 50px;
  padding: 8px 14px;
  display: flex;
  align-items: center;
  gap: 12px;
  box-shadow: 0 2px 6px rgba(0,0,0,0.03);
  border: 1px solid #f1f5f9;
  transition: all 0.2s;
  cursor: pointer;
  min-width: 0;
}

.category-pill-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(186,68,27,0.12);
  border-color: #fed7aa;
}

.pill-icon-wrap {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  flex-shrink: 0;
}

.pill-img {
  width: 32px;
  height: 32px;
  object-fit: cover;
  border-radius: 50%;
}

.pill-info {
  flex: 1;
  min-width: 0;
}

.pill-title {
  font-size: 0.88rem;
  font-weight: 700;
  margin: 0;
  color: #1f2937;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.pill-subtitle {
  font-size: 0.74rem;
  color: #9ca3af;
  margin: 0;
  white-space: nowrap;
}

.pill-dots {
  color: #d1d5db;
  font-size: 0.8rem;
  flex-shrink: 0;
}

/* 5. HERO / BANNER GRID */
.hero-banners-section {
  padding: 12px 0 28px;
}

.banners-grid {
  display: grid;
  grid-template-columns: 1.65fr 1fr;
  gap: 24px;
  width: 100%;
}

.banner-large {
  position: relative;
  border-radius: 20px;
  background-size: cover;
  background-position: center;
  min-height: 440px;
  display: flex;
  align-items: center;
  padding: 40px 48px;
  overflow: hidden;
  border: 1px solid #fed7aa;
}

.banner-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(90deg, rgba(255,255,255,0.94) 0%, rgba(255,255,255,0.75) 45%, rgba(255,255,255,0.1) 100%);
}

.banner-content {
  position: relative;
  z-index: 2;
  max-width: 420px;
}

.tag-ribbon {
  display: inline-block;
  background-color: #ba441b;
  color: #ffffff;
  font-size: 0.78rem;
  font-weight: 800;
  padding: 5px 16px;
  border-radius: 4px;
  margin-bottom: 16px;
  clip-path: polygon(0% 0%, 92% 0%, 100% 50%, 92% 100%, 0% 100%);
}

.hero-title {
  font-size: 3.2rem;
  font-weight: 800;
  line-height: 1.1;
  color: #111827;
  margin: 0;
}

.cursive-text {
  font-family: 'Great Vibes', cursive;
  color: #ba441b;
  font-weight: 400;
  font-size: 3.8rem;
}

.hero-subtitle {
  font-size: 1.25rem;
  font-weight: 700;
  color: #9a3412;
  margin: 8px 0 14px;
}

.hero-price {
  font-size: 2.2rem;
  font-weight: 800;
  color: #ba441b;
  margin-bottom: 20px;
}

.btn-shop-green {
  background-color: #ba441b;
  color: #ffffff;
  font-weight: 700;
  padding: 12px 32px;
  border-radius: 50px;
  border: none;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 14px rgba(186, 68, 27, 0.3);
}

.btn-shop-green:hover {
  background-color: #9a3412;
  transform: translateY(-2px);
}

.banners-right-group {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.banner-card.banner-honeynuts {
  background-color: #fff8f0;
  border-radius: 20px;
  padding: 24px 30px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 205px;
  overflow: hidden;
  border: 1px solid #ffedd5;
}

.banner-card .card-text h3 {
  font-size: 1.35rem;
  font-weight: 800;
  margin: 0 0 4px;
}

.banner-card .card-text p {
  font-size: 0.82rem;
  color: #6b7280;
  margin: 0 0 10px;
}

.card-price {
  font-size: 1.5rem;
  font-weight: 800;
  color: #ba441b;
  margin-bottom: 12px;
}

.btn-shop-pill {
  background-color: #ffffff;
  color: #1f2937;
  border: 1px solid #e5e7eb;
  padding: 6px 20px;
  border-radius: 50px;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-shop-pill:hover {
  background-color: #ba441b;
  color: #ffffff;
  border-color: #ba441b;
}

.card-img-wrap img {
  max-height: 155px;
  object-fit: contain;
  border-radius: 12px;
}

.banner-bottom-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 18px;
  flex: 1;
}

.banner-small-card {
  border-radius: 20px;
  padding: 20px 22px;
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  overflow: hidden;
  height: 205px;
}

.diaper-card {
  background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
  color: #1e293b;
  border: 1px solid #bfdbfe;
}

.diaper-card h4 {
  font-size: 1.15rem;
  font-weight: 800;
  margin: 0;
}

.diaper-card p {
  font-size: 0.78rem;
  opacity: 0.9;
  margin: 4px 0 16px;
}

.diaper-img {
  position: absolute;
  bottom: -10px;
  right: -10px;
  width: 120px;
  height: 120px;
  object-fit: cover;
  border-radius: 50%;
  opacity: 0.9;
}

.facewash-card {
  background: linear-gradient(135deg, #fff1f2 0%, #ffe4e6 100%);
  color: #1f2937;
  border: 1px solid #fecdd3;
}

.facewash-card h4 {
  font-size: 1.15rem;
  font-weight: 800;
  margin: 0;
}

.facewash-card p {
  font-size: 0.78rem;
  color: #6b7280;
  margin: 4px 0 16px;
}

.discount-circle {
  position: absolute;
  top: 16px;
  right: 16px;
  background-color: #ba441b;
  color: #ffffff;
  width: 48px;
  height: 48px;
  border-radius: 50%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  font-size: 0.85rem;
  line-height: 1;
  z-index: 2;
}

.discount-circle small {
  font-size: 0.6rem;
}

.facewash-img {
  position: absolute;
  bottom: 10px;
  right: 10px;
  width: 100px;
  height: 100px;
  object-fit: contain;
}

/* 6. FEATURED PRODUCTS SECTION */
.featured-products-section {
  padding: 30px 0;
}

.section-header-flex {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.section-heading {
  font-size: 1.65rem;
  font-weight: 800;
  color: #111827;
  margin: 0;
}

.filter-tabs-wrapper {
  display: flex;
  align-items: center;
  gap: 24px;
}

.filter-tabs {
  display: flex;
  list-style: none;
  margin: 0;
  padding: 0;
  gap: 18px;
}

.filter-tabs li {
  font-size: 0.92rem;
  font-weight: 600;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
  padding-bottom: 2px;
  position: relative;
}

.filter-tabs li.active, .filter-tabs li:hover {
  color: #ba441b;
  font-weight: 700;
}

.filter-tabs li.active::after {
  content: '';
  position: absolute;
  left: 0;
  bottom: -2px;
  width: 100%;
  height: 2px;
  background-color: #ba441b;
  border-radius: 2px;
}

.slider-nav-arrows {
  display: flex;
  gap: 8px;
}

.slider-btn {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  width: 34px;
  height: 34px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: #6b7280;
  transition: all 0.2s;
}

.slider-btn:hover {
  border-color: #ba441b;
  color: #ba441b;
  background-color: #fff7ed;
}

.products-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 16px;
  width: 100%;
}

.product-card {
  background: #ffffff;
  border-radius: 16px;
  border: 1px solid #f1f5f9;
  padding: 16px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  transition: all 0.25s;
}

.product-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 25px rgba(186, 68, 27, 0.08);
  border-color: #fed7aa;
}

.product-top-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.prod-category {
  font-size: 0.74rem;
  color: #9ca3af;
  font-weight: 600;
}

.btn-wishlist {
  background: transparent;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  font-size: 1rem;
}

.text-danger {
  color: #dc2626 !important;
}

.product-thumb {
  height: 140px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 10px 0;
  cursor: pointer;
}

.product-thumb img {
  max-height: 125px;
  max-width: 100%;
  object-fit: contain;
  transition: transform 0.3s;
}

.product-card:hover .product-thumb img {
  transform: scale(1.06);
}

.product-tags {
  display: flex;
  gap: 6px;
  margin-bottom: 8px;
}

.weight-badge {
  background: #f8fafc;
  font-size: 0.7rem;
  padding: 2px 7px;
  border-radius: 4px;
  color: #64748b;
  font-weight: 600;
  border: 1px solid #e2e8f0;
}

.product-pricing {
  display: flex;
  align-items: baseline;
  gap: 6px;
  margin-bottom: 6px;
}

.price-val {
  font-weight: 800;
  font-size: 0.98rem;
  color: #ba441b;
}

.old-price {
  font-size: 0.78rem;
  color: #9ca3af;
  text-decoration: line-through;
}

.discount-badge {
  background: #ba441b;
  color: #ffffff;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 4px;
}

.product-title {
  font-size: 0.88rem;
  font-weight: 600;
  line-height: 1.35;
  color: #1f2937;
  height: 38px;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  margin: 0 0 8px;
  cursor: pointer;
}

.product-title:hover {
  color: #ba441b;
}

.product-rating {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 12px;
}

.stars {
  color: #d1d5db;
  font-size: 0.75rem;
}

.star-active {
  color: #fbbf24;
}

.rating-num {
  font-size: 0.72rem;
  color: #9ca3af;
}

.btn-select-options {
  background-color: #fff7ed;
  color: #ba441b;
  border: 1px solid #fed7aa;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  width: 100%;
}

.btn-select-options:hover {
  background-color: #ba441b;
  color: #ffffff;
  border-color: #ba441b;
}

/* 7. PROMO MIDDLE STRIP BANNER */
.promo-strip-section {
  padding: 20px 0 35px;
}

.promo-banner-container {
  background: linear-gradient(90deg, #fef3ec 0%, #fae8de 60%, #f6ddcf 100%);
  border-radius: 16px;
  padding: 28px 48px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  position: relative;
  overflow: hidden;
  border: 1px solid #fed7aa;
}

.promo-text-area .sub-text {
  font-size: 1.1rem;
  font-weight: 600;
  color: #475569;
  display: block;
}

.promo-text-area .main-text {
  font-size: 1.8rem;
  font-weight: 800;
  color: #111827;
  margin: 4px 0 0;
}

.pricing-start-badge {
  display: flex;
  align-items: center;
  gap: 12px;
}

.pricing-start-badge .label {
  font-size: 1.1rem;
  font-weight: 800;
  line-height: 1.2;
  color: #1f2937;
}

.price-bubble {
  background-color: #ba441b;
  color: #ffffff;
  font-weight: 800;
  font-size: 1.5rem;
  padding: 10px 24px;
  border-radius: 50px;
  box-shadow: 0 4px 14px rgba(186,68,27,0.3);
}

.promo-bottles-img img {
  height: 95px;
  border-radius: 10px;
  object-fit: cover;
}

/* 8. TOP SELLER USERS */
.top-sellers-section {
  padding: 20px 0 35px;
}

.sellers-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.header-line {
  width: 40px;
  height: 3px;
  background-color: #ba441b;
  border-radius: 2px;
}

.see-more-link {
  color: #6b7280;
  text-decoration: none;
  font-size: 0.88rem;
  font-weight: 600;
  display: flex;
  align-items: center;
}

.see-more-link:hover {
  color: #ba441b;
}

.sellers-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  width: 100%;
}

.seller-card {
  background: #ffffff;
  border-radius: 16px;
  border: 1px solid #f1f5f9;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: all 0.2s;
}

.seller-card:hover {
  box-shadow: 0 8px 20px rgba(186,68,27,0.08);
  border-color: #fed7aa;
}

.seller-avatar {
  width: 64px;
  height: 64px;
  border-radius: 12px;
  overflow: hidden;
  flex-shrink: 0;
}

.seller-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.badge-featured {
  background-color: #ffedd5;
  color: #ba441b;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 4px;
  display: inline-block;
  margin-bottom: 2px;
}

.seller-name {
  font-size: 0.98rem;
  font-weight: 700;
  color: #111827;
  margin: 2px 0 4px;
}

/* 9. DEAL OF THE WEEK */
.deal-of-week-section {
  padding: 20px 0 50px;
}

.deal-wrapper-box {
  background: #ffffff;
  border: 2px solid #ba441b;
  border-radius: 20px;
  padding: 28px;
  width: 100%;
}

.deal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.deal-title {
  font-size: 1.65rem;
  font-weight: 800;
  color: #111827;
  margin: 0;
}

.countdown-group {
  display: flex;
  gap: 8px;
}

.time-box {
  background-color: #ba441b;
  color: #ffffff;
  width: 50px;
  height: 50px;
  border-radius: 10px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.time-num {
  font-size: 1.1rem;
  font-weight: 800;
  line-height: 1;
}

.time-unit {
  font-size: 0.62rem;
  text-transform: uppercase;
  font-weight: 600;
  opacity: 0.9;
}

.deal-items-grid {
  display: grid;
  grid-template-columns: 1.2fr 0.9fr 0.9fr;
  gap: 20px;
  width: 100%;
}

.deal-left-banner {
  background: #fdf6ec;
  border: 1px solid #fed7aa;
  border-radius: 16px;
  padding: 28px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  position: relative;
  overflow: hidden;
}

.deal-left-content {
  max-width: 240px;
  z-index: 2;
}

.deal-badge-organic {
  color: #ba441b;
  font-weight: 700;
  font-size: 0.8rem;
  text-transform: uppercase;
}

.deal-left-content h3 {
  font-size: 1.35rem;
  font-weight: 800;
  color: #0f172a;
  margin: 8px 0;
}

.deal-left-content p {
  font-size: 0.82rem;
  color: #64748b;
  margin-bottom: 16px;
}

.btn-deal-shop {
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  font-weight: 700;
  padding: 8px 18px;
  border-radius: 50px;
  font-size: 0.84rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-deal-shop:hover {
  background-color: #9a3412;
}

.deal-hero-img {
  width: 140px;
  height: 140px;
  object-fit: cover;
  border-radius: 50%;
}

/* Responsive adjustments */
@media (max-width: 1200px) {
  .products-grid {
    grid-template-columns: repeat(3, 1fr);
  }
  .sellers-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 992px) {
  .banners-grid {
    grid-template-columns: 1fr;
  }
  .deal-items-grid {
    grid-template-columns: 1fr;
  }
  .nav-links {
    display: none;
  }
}

@media (max-width: 768px) {
  .top-left, .top-right {
    display: none;
  }
  .top-bar-content {
    justify-content: center;
  }
  .header-container {
    flex-wrap: wrap;
  }
  .search-box-wrapper {
    order: 3;
    width: 100%;
    max-width: 100%;
  }
  .products-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .banner-bottom-row {
    grid-template-columns: 1fr;
  }
  .promo-banner-container {
    flex-direction: column;
    gap: 16px;
    text-align: center;
  }
}
</style>
