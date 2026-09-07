<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

// State
const searchQuery = ref("");
const selectedCategory = ref("All Categories");
const cartCount = ref(0);
const wishlistCount = ref(0);
const activeProductTab = ref("All");
const wishlistedIds = ref<number[]>([]);

// Category navigation
const categories = [
  { name: "Vegetables", count: 6, icon: "🥗", bg: "#fef3ee" },
  { name: "Fresh Fruits", count: 8, icon: "🍊", bg: "#fef3ee" },
  { name: "Desserts", count: 9, icon: "🧁", bg: "#fef3ee" },
  { name: "Drinks & Juice", count: 6, icon: "🧃", bg: "#fef3ee" },
  { name: "Fish & Meats", count: 6, icon: "🐟", bg: "#fef3ee" },
  { name: "Pets & Animals", count: 4, icon: "🐶", bg: "#fef3ee" },
  { name: "Beverage", count: 8, icon: "☕", bg: "#fef3ee" }
];

// Featured Products
interface Product {
  id: number;
  name: string;
  category: string;
  image: string;
  price: string;
  oldPrice?: string;
  discountBadge?: string;
  weights: string[];
  rating: number;
  reviewScore: string;
}

const allProducts = ref<Product[]>([
  {
    id: 1,
    name: "Russet Idaho Potatoes Fresh Premium Fruit and Produce",
    category: "Vegetables",
    image: "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=500&auto=format&fit=crop&q=80",
    price: "$30.00 - $38.00",
    discountBadge: "-16%",
    weights: ["100gm", "500gm"],
    rating: 5,
    reviewScore: "5.00"
  },
  {
    id: 2,
    name: "Aptamil Gold+ ProNutra Biotik Stage 1 Infant Formula",
    category: "Desserts",
    image: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=500&auto=format&fit=crop&q=80",
    price: "$25.00 - $30.00",
    discountBadge: "-44%",
    weights: ["100gm", "375ml"],
    rating: 5,
    reviewScore: "5.00"
  },
  {
    id: 3,
    name: "Whole Foods Market, Organic Trimmed Green Peas Fresh Bag",
    category: "Vegetables",
    image: "https://images.unsplash.com/photo-1592394533824-9440e5d68530?w=500&auto=format&fit=crop&q=80",
    price: "$3.00 - $8.00",
    discountBadge: "-77%",
    weights: ["100gm", "500gm"],
    rating: 5,
    reviewScore: "5.00"
  },
  {
    id: 4,
    name: "Whole Foods Market, Romaine Hearts Salad Bag Fresh Farm",
    category: "Vegetables",
    image: "https://images.unsplash.com/photo-1550411294-b3b1bf5bece1?w=500&auto=format&fit=crop&q=80",
    price: "$19.00",
    oldPrice: "$22.00",
    discountBadge: "-14%",
    weights: ["100gm"],
    rating: 5,
    reviewScore: "5.00"
  },
  {
    id: 5,
    name: "Red Rock Deli Style Potato Chips, Lime & Cracked Pepper",
    category: "Beverage",
    image: "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=500&auto=format&fit=crop&q=80",
    price: "$34.00",
    oldPrice: "$45.00",
    discountBadge: "-24%",
    weights: ["100gm"],
    rating: 5,
    reviewScore: "5.00"
  },
  {
    id: 6,
    name: "Fresh and Sweet Watermelon Delights for Refreshing Days",
    category: "Vegetables",
    image: "https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=500&auto=format&fit=crop&q=80",
    price: "$18.00 - $45.00",
    discountBadge: "-33%",
    weights: ["375ml", "500gm"],
    rating: 4,
    reviewScore: "4.00"
  }
]);

// Filter products based on selected tab
const filteredProducts = computed(() => {
  if (activeProductTab.value === "All") return allProducts.value;
  return allProducts.value.filter(p => p.category === activeProductTab.value);
});

// Top Sellers
const topSellers = [
  {
    id: 1,
    name: "Eleanor Pena",
    avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?w=200&auto=format&fit=crop&q=80",
    rating: 5,
    featured: false
  },
  {
    id: 2,
    name: "Dianne Russell",
    avatar: "https://images.unsplash.com/photo-1592417817098-8f3d6910985b?w=200&auto=format&fit=crop&q=80",
    rating: 5,
    featured: true
  },
  {
    id: 3,
    name: "Michel Richard",
    avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&auto=format&fit=crop&q=80",
    rating: 5,
    featured: false
  },
  {
    id: 4,
    name: "Marvin McKinney",
    avatar: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=200&auto=format&fit=crop&q=80",
    rating: 5,
    featured: false
  }
];

// Deal of the Week Products
const dealProducts = [
  {
    id: 101,
    name: "Delicious Lay's Potato Chips, Classic, 8 oz Bag",
    category: "Beverage",
    image: "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400&auto=format&fit=crop&q=80",
    price: "$12.00",
    oldPrice: "$21.00",
    discountBadge: "-43%",
    rating: 4,
    reviewScore: "4.00"
  },
  {
    id: 102,
    name: "SunChips Minis, Garden Salsa Flavored Canister 7 oz",
    category: "Beverage",
    image: "https://images.unsplash.com/photo-1621447504864-d8686e12698c?w=400&auto=format&fit=crop&q=80",
    price: "$22.00",
    discountBadge: "-20%",
    rating: 4,
    reviewScore: "4.00"
  },
  {
    id: 103,
    name: "Farm Fresh Russet Potatoes Organic Harvest Bag",
    category: "Vegetables",
    image: "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=400&auto=format&fit=crop&q=80",
    price: "$15.00",
    oldPrice: "$25.00",
    discountBadge: "-40%",
    rating: 5,
    reviewScore: "5.00"
  }
];

// Countdown timer state
const days = ref("04");
const hours = ref("18");
const minutes = ref("35");
const seconds = ref("42");
let timerInterval: any = null;

const startCountdown = () => {
  let totalSecs = 4 * 86400 + 18 * 3600 + 35 * 60 + 42;
  timerInterval = setInterval(() => {
    if (totalSecs <= 0) {
      clearInterval(timerInterval);
      return;
    }
    totalSecs--;
    const d = Math.floor(totalSecs / 86400);
    const h = Math.floor((totalSecs % 86400) / 3600);
    const m = Math.floor((totalSecs % 3600) / 60);
    const s = totalSecs % 60;
    days.value = d < 10 ? `0${d}` : `${d}`;
    hours.value = h < 10 ? `0${h}` : `${h}`;
    minutes.value = m < 10 ? `0${m}` : `${m}`;
    seconds.value = s < 10 ? `0${s}` : `${s}`;
  }, 1000);
};

// Toggle wishlist
const toggleWishlist = (id: number) => {
  if (wishlistedIds.value.includes(id)) {
    wishlistedIds.value = wishlistedIds.value.filter(item => item !== id);
    wishlistCount.value = Math.max(0, wishlistCount.value - 1);
  } else {
    wishlistedIds.value.push(id);
    wishlistCount.value++;
  }
};

// Add to cart
const addToCart = () => {
  cartCount.value++;
};

// Search handling
const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({ path: "/products", query: { q: searchQuery.value } });
  } else {
    router.push("/products");
  }
};

onMounted(() => {
  startCountdown();
});

onUnmounted(() => {
  if (timerInterval) clearInterval(timerInterval);
});
</script>

<template>
  <div class="zilly-style-home">
    <!-- 1. TOP ANNOUNCEMENT / INFO BAR -->
    <div class="top-info-bar">
      <div class="site-container info-bar-content">
        <div class="info-left">
          <span class="info-item">
            <span class="info-icon">📍</span> 23/A Mark Street Road, Da Nang City
          </span>
          <span class="info-divider">|</span>
          <span class="info-item">
            <span class="info-icon">✉️</span> info@zonemart.com
          </span>
        </div>
        <div class="info-right">
          <span class="promo-hint">‹ Try ZoneMart for free</span>
          <router-link to="/register-seller" class="open-store-link">Open store right now</router-link>
          <span class="promo-arrow">›</span>
        </div>
      </div>
    </div>

    <!-- 2. MAIN HEADER (BRAND + SEARCH + ICONS) -->
    <header class="main-header">
      <div class="site-container header-inner">
        <!-- Logo -->
        <router-link to="/" class="brand-logo">
          <div class="brand-icon-wrap">
            <svg viewBox="0 0 24 24" class="bag-svg" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/>
              <line x1="3" y1="6" x2="21" y2="6"/>
              <path d="M16 10a4 4 0 0 1-8 0"/>
            </svg>
          </div>
          <span class="brand-title">Zone<span class="brand-accent">Mart</span></span>
        </router-link>

        <!-- Search Bar with Category Dropdown -->
        <div class="search-cluster">
          <div class="category-dropdown-btn">
            <span>{{ selectedCategory }}</span>
            <span class="dropdown-caret">▾</span>
          </div>
          <div class="cluster-divider"></div>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Type Your Products ..."
            class="search-text-input"
            @keyup.enter="handleSearch"
          />
          <button class="search-submit-btn" @click="handleSearch">
            <span>Search</span>
            <span class="search-btn-icon">🔍</span>
          </button>
        </div>

        <!-- Action Icons: User, Wishlist, Cart -->
        <div class="header-action-group">
          <router-link to="/profile" class="header-action-btn" title="Hồ sơ cá nhân">
            <span class="action-icon">👤</span>
          </router-link>

          <div class="header-action-btn" title="Yêu thích">
            <span class="action-icon">💛</span>
            <span class="action-badge">{{ wishlistCount }}</span>
          </div>

          <router-link to="/cart" class="header-action-btn" title="Giỏ hàng">
            <span class="action-icon">🛒</span>
            <span class="action-badge">{{ cartCount }}</span>
          </router-link>

          <button class="header-action-btn hamburger-btn" title="Menu">
            <span class="action-icon">☰</span>
          </button>
        </div>
      </div>
    </header>

    <!-- 3. SUB-NAV MENU & HOTLINE -->
    <nav class="sub-nav-bar">
      <div class="site-container sub-nav-inner">
        <ul class="nav-menu-list">
          <li class="nav-menu-item active">
            <router-link to="/">Home ▾</router-link>
          </li>
          <li class="nav-menu-item">
            <router-link to="/products">Pages ▾</router-link>
          </li>
          <li class="nav-menu-item">
            <router-link to="/products">Shop ▾</router-link>
          </li>
          <li class="nav-menu-item">
            <router-link to="/register-seller">Vendor ▾</router-link>
          </li>
          <li class="nav-menu-item">
            <router-link to="/shipper">Elements ▾</router-link>
          </li>
          <li class="nav-menu-item">
            <router-link to="/map">Blog ▾</router-link>
          </li>
          <li class="nav-menu-item">
            <router-link to="/contact">Contact</router-link>
          </li>
        </ul>

        <div class="sub-nav-right">
          <div class="weekly-discount-tag">
            <span class="discount-icon">🏷️</span>
            <span>Weekly Discount!</span>
          </div>
          <div class="hotline-pill">
            <span class="hotline-icon">📞</span>
            <div class="hotline-text">
              <span class="hotline-label">Hotline Number</span>
              <span class="hotline-num">+988-256-666</span>
            </div>
          </div>
        </div>
      </div>
    </nav>

    <!-- 4. CATEGORIES ROW -->
    <section class="categories-section">
      <div class="site-container">
        <div class="categories-row">
          <div
            v-for="(cat, idx) in categories"
            :key="idx"
            class="category-pill-card"
            @click="router.push('/products')"
          >
            <div class="cat-circle-avatar" :style="{ backgroundColor: cat.bg }">
              <span class="cat-emoji">{{ cat.icon }}</span>
            </div>
            <div class="cat-details">
              <h4 class="cat-title">{{ cat.name }}</h4>
              <p class="cat-count">{{ cat.count }} Products</p>
            </div>
            <span class="cat-dot-menu">⋮</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 5. HERO BANNERS GRID -->
    <section class="hero-banners-section">
      <div class="site-container hero-grid">
        <!-- Main Large Banner (Left) -->
        <div class="hero-main-card">
          <div class="hero-main-content">
            <span class="farm-fresh-badge">100% Farm Fresh Food</span>
            <h1 class="hero-fresh-title">
              Fresh Organic<br />
              <span class="script-subtitle">Food For All</span>
            </h1>
            <div class="hero-price-tag">$59.00</div>
            <button class="btn-shop-now" @click="router.push('/products')">
              Shop Now
            </button>
          </div>
          <div class="hero-main-visual">
            <img
              src="https://images.unsplash.com/photo-1540420773420-3366772f4999?w=900&auto=format&fit=crop&q=80"
              alt="Fresh Organic Food Platter"
              class="hero-food-img"
            />
          </div>
        </div>

        <!-- Right Banner Column -->
        <div class="hero-side-column">
          <!-- Top Side Banner (Honeynuts) -->
          <div class="side-banner-card top-nuts-card">
            <div class="side-card-text">
              <h3 class="side-card-title">Premium Honeynuts</h3>
              <p class="side-card-sub">100% Salted Organic Nuts</p>
              <div class="side-card-price">$15.00</div>
              <button class="btn-side-shop" @click="router.push('/products')">
                Shop Now
              </button>
            </div>
            <div class="side-card-media">
              <img
                src="https://images.unsplash.com/photo-1509440159596-0249088772ff?w=500&auto=format&fit=crop&q=80"
                alt="Organic Nuts"
                class="side-media-img"
              />
            </div>
          </div>

          <!-- Bottom Split Banners -->
          <div class="side-banner-split-row">
            <!-- Split 1: Baby Diaper -->
            <div class="mini-promo-card baby-card">
              <div class="mini-text">
                <h4 class="mini-title">New Baby Diaper</h4>
                <span class="mini-tag">Top Quality Product</span>
                <button class="btn-mini-shop" @click="router.push('/products')">
                  Shop Now
                </button>
              </div>
              <div class="mini-img-wrap">
                <img
                  src="https://images.unsplash.com/photo-1519689680058-324335c77eba?w=400&auto=format&fit=crop&q=80"
                  alt="Baby Diaper"
                  class="mini-card-img"
                />
              </div>
            </div>

            <!-- Split 2: Dark Wash FaceWash -->
            <div class="mini-promo-card facewash-card">
              <div class="discount-circle-pill">
                <span>15%</span>
                <span class="off-text">OFF</span>
              </div>
              <div class="mini-text">
                <h4 class="mini-title">Dark wash FaceWash</h4>
                <span class="mini-tag">All Fixed Size</span>
                <button class="btn-mini-shop" @click="router.push('/products')">
                  Shop Now
                </button>
              </div>
              <div class="mini-img-wrap">
                <img
                  src="https://images.unsplash.com/photo-1556228720-195a672e8a03?w=400&auto=format&fit=crop&q=80"
                  alt="Face Wash"
                  class="mini-card-img"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 6. FEATURED PRODUCTS SECTION -->
    <section class="featured-products-section">
      <div class="site-container">
        <!-- Section Header with Filter Tabs -->
        <div class="section-head-bar">
          <h2 class="section-heading">Featured Products</h2>
          <div class="section-controls">
            <div class="filter-tab-buttons">
              <button
                :class="['filter-btn', { active: activeProductTab === 'All' }]"
                @click="activeProductTab = 'All'"
              >
                All
              </button>
              <button
                :class="['filter-btn', { active: activeProductTab === 'Desserts' }]"
                @click="activeProductTab = 'Desserts'"
              >
                Desserts
              </button>
              <button
                :class="['filter-btn', { active: activeProductTab === 'Vegetables' }]"
                @click="activeProductTab = 'Vegetables'"
              >
                Vegetables
              </button>
              <button
                :class="['filter-btn', { active: activeProductTab === 'Beverage' }]"
                @click="activeProductTab = 'Beverage'"
              >
                Beverage
              </button>
            </div>
            <div class="arrow-nav-group">
              <button class="nav-arrow-btn" aria-label="Previous">‹</button>
              <button class="nav-arrow-btn" aria-label="Next">›</button>
            </div>
          </div>
        </div>

        <!-- 6 Products Grid -->
        <div class="products-grid">
          <div
            v-for="prod in filteredProducts"
            :key="prod.id"
            class="product-card"
          >
            <!-- Top Card Header: Category Tag & Heart -->
            <div class="card-top-row">
              <span class="product-cat-tag">{{ prod.category }}</span>
              <button
                class="heart-toggle-btn"
                :class="{ active: wishlistedIds.includes(prod.id) }"
                @click="toggleWishlist(prod.id)"
              >
                {{ wishlistedIds.includes(prod.id) ? "❤️" : "🤍" }}
              </button>
            </div>

            <!-- Product Image -->
            <div class="product-media-wrap" @click="router.push('/products')">
              <img :src="prod.image" :alt="prod.name" class="product-photo" />
            </div>

            <!-- Weight pills -->
            <div class="weight-tags-row">
              <span v-for="(w, widx) in prod.weights" :key="widx" class="weight-tag">
                {{ w }}
              </span>
            </div>

            <!-- Price & Discount -->
            <div class="product-price-row">
              <span class="price-current">{{ prod.price }}</span>
              <span v-if="prod.oldPrice" class="price-old">{{ prod.oldPrice }}</span>
              <span v-if="prod.discountBadge" class="discount-badge">
                {{ prod.discountBadge }}
              </span>
            </div>

            <!-- Product Title -->
            <h3 class="product-card-title" :title="prod.name" @click="router.push('/products')">
              {{ prod.name }}
            </h3>

            <!-- Rating Stars -->
            <div class="product-stars-row">
              <span class="stars-gold">★★★★★</span>
              <span class="review-score">({{ prod.reviewScore }})</span>
            </div>

            <!-- Select Options Button -->
            <button class="btn-select-options" @click="addToCart">
              <span class="basket-icon">🧺</span>
              <span>Select Options</span>
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- 7. WIDE PROMO BANNER (SHEA LOTION) -->
    <section class="wide-promo-section">
      <div class="site-container">
        <div class="wide-promo-card">
          <div class="wide-promo-left">
            <span class="promo-overline">The brand New Collection Shea</span>
            <h2 class="promo-main-heading">Nourishing Women Body Lotions</h2>
          </div>
          <div class="wide-promo-center">
            <span class="pricing-label">Our Pricing Start</span>
            <div class="pricing-oval-tag">
              <span>$45.00</span>
              <div class="oval-rays"></div>
            </div>
          </div>
          <div class="wide-promo-right">
            <img
              src="https://images.unsplash.com/photo-1608248597359-57e3f890cf28?w=600&auto=format&fit=crop&q=80"
              alt="Women Body Lotions Collection"
              class="lotion-bottles-img"
            />
          </div>
        </div>
      </div>
    </section>

    <!-- 8. TOP SELLER USERS SECTION -->
    <section class="top-sellers-section">
      <div class="site-container">
        <div class="sellers-head-row">
          <div class="sellers-title-wrap">
            <h2 class="sellers-heading">Top Seller Users</h2>
            <div class="heading-accent-line"></div>
          </div>
          <router-link to="/products" class="see-more-link">
            See More ›
          </router-link>
        </div>

        <div class="sellers-grid">
          <div
            v-for="seller in topSellers"
            :key="seller.id"
            class="seller-card"
          >
            <div class="seller-avatar-wrap">
              <img :src="seller.avatar" :alt="seller.name" class="seller-avatar-img" />
            </div>
            <div class="seller-info">
              <span v-if="seller.featured" class="seller-featured-tag">Featured</span>
              <h4 class="seller-name">{{ seller.name }}</h4>
              <div class="seller-stars">★★★★★</div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 9. DEAL OF THE WEEK SECTION -->
    <section class="deal-week-section">
      <div class="site-container">
        <div class="deal-week-card">
          <!-- Deal Header with Live Countdown -->
          <div class="deal-head-bar">
            <h2 class="deal-heading">Deal Of The Week</h2>
            <div class="deal-countdown-cluster">
              <div class="countdown-unit-box">
                <span class="countdown-num">{{ days }}</span>
                <span class="countdown-label">Day</span>
              </div>
              <div class="countdown-unit-box">
                <span class="countdown-num">{{ hours }}</span>
                <span class="countdown-label">Hr</span>
              </div>
              <div class="countdown-unit-box">
                <span class="countdown-num">{{ minutes }}</span>
                <span class="countdown-label">Min</span>
              </div>
              <div class="countdown-unit-box">
                <span class="countdown-num">{{ seconds }}</span>
                <span class="countdown-label">Secs</span>
              </div>
            </div>
          </div>

          <!-- Deal Content Grid -->
          <div class="deal-body-grid">
            <!-- Left Feature Item Banner -->
            <div class="deal-feature-spotlight">
              <img
                src="https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600&auto=format&fit=crop&q=80"
                alt="Deal spotlight"
                class="spotlight-img"
              />
              <div class="spotlight-overlay">
                <span class="spotlight-tag">Special Weekly Offer</span>
                <h3 class="spotlight-title">Organic Farm Bundle</h3>
                <span class="spotlight-discount">Up to 50% OFF</span>
                <button class="btn-spotlight-shop" @click="router.push('/products')">
                  Shop Deal
                </button>
              </div>
            </div>

            <!-- Right Deal Products Grid -->
            <div class="deal-items-row">
              <div
                v-for="item in dealProducts"
                :key="item.id"
                class="deal-product-item"
              >
                <div class="deal-item-head">
                  <span class="deal-item-cat">{{ item.category }}</span>
                  <span class="deal-item-heart">🤍</span>
                </div>
                <div class="deal-item-media" @click="router.push('/products')">
                  <img :src="item.image" :alt="item.name" class="deal-item-img" />
                </div>
                <div class="deal-item-price-row">
                  <span class="deal-curr-price">{{ item.price }}</span>
                  <span v-if="item.oldPrice" class="deal-old-price">{{ item.oldPrice }}</span>
                  <span class="deal-badge">{{ item.discountBadge }}</span>
                </div>
                <h4 class="deal-item-title">{{ item.name }}</h4>
                <div class="deal-item-stars">
                  <span class="stars-gold">★★★★☆</span>
                  <span class="review-score">({{ item.reviewScore }})</span>
                </div>
                <button class="btn-deal-add" @click="addToCart">
                  <span class="basket-icon">🧺</span>
                  <span>Add to Cart</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* ==========================================================
   THEME PALETTE (Terracotta / Burnt Orange Brand Theme)
   Primary: #ba441b / #c2410c / #ea580c
   Dark: #1e293b / #0f172a
   Light BGs: #fdfaf6 / #f8fafc / #ffffff
   Accents: #f97316 / #ea580c / #ffedd5
   ========================================================== */

.zilly-style-home {
  width: 100%;
  min-height: 100vh;
  background-color: #fbf9f6;
  color: #1e293b;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell, "Open Sans", "Helvetica Neue", sans-serif;
  overflow-x: hidden;
}

/* Base Responsive Container */
.site-container {
  width: 100%;
  max-width: 1380px;
  margin: 0 auto;
  padding: 0 clamp(16px, 2.5vw, 40px);
}

/* ==========================================================
   1. TOP INFO BAR
   ========================================================== */
.top-info-bar {
  background-color: #ba441b;
  color: #ffffff;
  font-size: 0.82rem;
  padding: 8px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.info-bar-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.info-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.info-item {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  opacity: 0.95;
}

.info-divider {
  opacity: 0.4;
}

.info-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.promo-hint {
  opacity: 0.9;
}

.open-store-link {
  color: #ffedd5;
  font-weight: 700;
  text-decoration: underline;
  transition: color 0.2s;
}

.open-store-link:hover {
  color: #ffffff;
}

.promo-arrow {
  font-weight: bold;
}

/* ==========================================================
   2. MAIN HEADER (BRAND + SEARCH + ICONS)
   ========================================================== */
.main-header {
  background: #ffffff;
  border-bottom: 1px solid #f1f5f9;
  padding: 16px 0;
}

.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
}

.brand-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}

.brand-icon-wrap {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background-color: #ffedd5;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ba441b;
}

.bag-svg {
  width: 24px;
  height: 24px;
}

.brand-title {
  font-size: 1.65rem;
  font-weight: 800;
  color: #1e293b;
  letter-spacing: -0.5px;
}

.brand-accent {
  color: #ba441b;
}

/* Search Cluster */
.search-cluster {
  display: flex;
  align-items: center;
  flex: 1;
  max-width: 620px;
  border: 1.5px solid #e2e8f0;
  border-radius: 999px;
  background: #ffffff;
  padding: 4px 6px 4px 18px;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.search-cluster:focus-within {
  border-color: #ba441b;
  box-shadow: 0 0 0 3px rgba(186, 68, 27, 0.12);
}

.category-dropdown-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
  white-space: nowrap;
}

.dropdown-caret {
  font-size: 0.75rem;
  color: #94a3b8;
}

.cluster-divider {
  width: 1px;
  height: 24px;
  background: #e2e8f0;
  margin: 0 14px;
}

.search-text-input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 0.9rem;
  color: #1e293b;
  background: transparent;
}

.search-text-input::placeholder {
  color: #94a3b8;
}

.search-submit-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #ba441b;
  color: #ffffff;
  border: none;
  border-radius: 999px;
  padding: 8px 20px;
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.2s, transform 0.1s;
}

.search-submit-btn:hover {
  background: #9a3412;
}

.search-submit-btn:active {
  transform: scale(0.97);
}

/* User Header Actions */
.header-action-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.header-action-btn {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #f8fafc;
  color: #334155;
  text-decoration: none;
  border: 1px solid #e2e8f0;
  cursor: pointer;
  font-size: 1.1rem;
  transition: all 0.2s;
}

.header-action-btn:hover {
  background: #ffedd5;
  color: #ba441b;
  border-color: #fed7aa;
}

.action-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  background: #ba441b;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: 800;
  min-width: 18px;
  height: 18px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 4px;
  border: 2px solid #ffffff;
}

.hamburger-btn {
  background: #ffffff;
}

/* ==========================================================
   3. SUB-NAV MENU & HOTLINE
   ========================================================== */
.sub-nav-bar {
  background: #ffffff;
  border-bottom: 1px solid #f1f5f9;
  padding: 6px 0;
}

.sub-nav-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.nav-menu-list {
  display: flex;
  align-items: center;
  list-style: none;
  margin: 0;
  padding: 0;
  gap: 28px;
}

.nav-menu-item a {
  text-decoration: none;
  font-size: 0.92rem;
  font-weight: 600;
  color: #334155;
  padding: 10px 0;
  display: inline-block;
  transition: color 0.2s;
}

.nav-menu-item:hover a,
.nav-menu-item.active a {
  color: #ba441b;
}

.sub-nav-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.weekly-discount-tag {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.88rem;
  font-weight: 700;
  color: #ba441b;
}

.hotline-pill {
  display: flex;
  align-items: center;
  gap: 10px;
  background: #ba441b;
  color: #ffffff;
  padding: 8px 18px;
  border-radius: 999px;
  box-shadow: 0 4px 12px rgba(186, 68, 27, 0.25);
}

.hotline-icon {
  font-size: 1.1rem;
}

.hotline-text {
  display: flex;
  flex-direction: column;
}

.hotline-label {
  font-size: 0.68rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  opacity: 0.9;
}

.hotline-num {
  font-size: 0.95rem;
  font-weight: 800;
  letter-spacing: 0.2px;
}

/* ==========================================================
   4. CATEGORIES ROW
   ========================================================== */
.categories-section {
  padding: 24px 0 16px 0;
}

.categories-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 14px;
}

.category-pill-card {
  background: #ffffff;
  border: 1px solid #f1f5f9;
  border-radius: 14px;
  padding: 10px 14px;
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s, border-color 0.2s;
}

.category-pill-card:hover {
  transform: translateY(-2px);
  border-color: #fed7aa;
  box-shadow: 0 6px 16px rgba(186, 68, 27, 0.08);
}

.cat-circle-avatar {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
  flex-shrink: 0;
}

.cat-details {
  flex: 1;
  min-width: 0;
}

.cat-title {
  margin: 0;
  font-size: 0.88rem;
  font-weight: 700;
  color: #1e293b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cat-count {
  margin: 2px 0 0 0;
  font-size: 0.75rem;
  color: #94a3b8;
}

.cat-dot-menu {
  color: #cbd5e1;
  font-size: 1.1rem;
}

/* ==========================================================
   5. HERO BANNERS GRID
   ========================================================== */
.hero-banners-section {
  padding: 16px 0 32px 0;
}

.hero-grid {
  display: grid;
  grid-template-columns: 1.35fr 1fr;
  gap: 20px;
}

/* Main Left Card */
.hero-main-card {
  background: #fdf6ec;
  border-radius: 20px;
  overflow: hidden;
  position: relative;
  min-height: 420px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: clamp(24px, 4vw, 48px);
  border: 1px solid #fed7aa;
}

.hero-main-content {
  z-index: 2;
  max-width: 320px;
}

.farm-fresh-badge {
  display: inline-block;
  background: #ba441b;
  color: #ffffff;
  font-size: 0.75rem;
  font-weight: 800;
  padding: 4px 12px;
  border-radius: 999px;
  margin-bottom: 16px;
  letter-spacing: 0.3px;
}

.hero-fresh-title {
  font-size: clamp(2rem, 3.2vw, 3rem);
  font-weight: 900;
  line-height: 1.1;
  color: #0f172a;
  margin: 0 0 16px 0;
}

.script-subtitle {
  font-family: "Georgia", serif;
  font-style: italic;
  color: #ba441b;
  font-weight: 600;
}

.hero-price-tag {
  font-size: 2rem;
  font-weight: 800;
  color: #ba441b;
  margin-bottom: 20px;
}

.btn-shop-now {
  background: #ba441b;
  color: #ffffff;
  border: none;
  font-size: 0.95rem;
  font-weight: 700;
  padding: 12px 28px;
  border-radius: 999px;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 14px rgba(186, 68, 27, 0.3);
}

.btn-shop-now:hover {
  background: #9a3412;
  transform: translateY(-2px);
}

.hero-main-visual {
  position: absolute;
  right: 0;
  top: 0;
  bottom: 0;
  width: 55%;
  overflow: hidden;
}

.hero-food-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  mask-image: linear-gradient(to right, transparent, black 30%);
  -webkit-mask-image: linear-gradient(to right, transparent, black 30%);
}

/* Right Banner Column */
.hero-side-column {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.side-banner-card {
  background: #f8fafc;
  border-radius: 18px;
  overflow: hidden;
  padding: 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border: 1px solid #e2e8f0;
  position: relative;
  min-height: 200px;
}

.top-nuts-card {
  background: #fff8f0;
  border-color: #ffedd5;
}

.side-card-text {
  max-width: 55%;
  z-index: 2;
}

.side-card-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 800;
  color: #0f172a;
}

.side-card-sub {
  margin: 6px 0 10px 0;
  font-size: 0.82rem;
  color: #64748b;
}

.side-card-price {
  font-size: 1.4rem;
  font-weight: 800;
  color: #ba441b;
  margin-bottom: 12px;
}

.btn-side-shop {
  background: #ffffff;
  color: #1e293b;
  border: 1px solid #cbd5e1;
  font-size: 0.82rem;
  font-weight: 700;
  padding: 8px 18px;
  border-radius: 999px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-side-shop:hover {
  background: #ba441b;
  color: #ffffff;
  border-color: #ba441b;
}

.side-card-media {
  width: 42%;
  height: 140px;
  border-radius: 12px;
  overflow: hidden;
}

.side-media-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* Split row */
.side-banner-split-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.mini-promo-card {
  border-radius: 18px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  position: relative;
  min-height: 190px;
  overflow: hidden;
}

.baby-card {
  background: #eff6ff;
  border: 1px solid #dbeafe;
}

.facewash-card {
  background: #fff1f2;
  border: 1px solid #ffe4e6;
}

.mini-title {
  margin: 0;
  font-size: 0.98rem;
  font-weight: 800;
  color: #0f172a;
}

.mini-tag {
  display: block;
  font-size: 0.75rem;
  color: #64748b;
  margin: 4px 0 10px 0;
}

.btn-mini-shop {
  background: #ffffff;
  color: #1e293b;
  border: 1px solid #cbd5e1;
  font-size: 0.78rem;
  font-weight: 700;
  padding: 6px 14px;
  border-radius: 999px;
  cursor: pointer;
  align-self: flex-start;
  transition: all 0.2s;
}

.btn-mini-shop:hover {
  background: #ba441b;
  color: #ffffff;
  border-color: #ba441b;
}

.mini-img-wrap {
  width: 100%;
  height: 80px;
  border-radius: 8px;
  overflow: hidden;
  margin-top: 10px;
}

.mini-card-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.discount-circle-pill {
  position: absolute;
  top: 12px;
  right: 12px;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: #ba441b;
  color: #ffffff;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 900;
  line-height: 1;
}

.off-text {
  font-size: 0.55rem;
  font-weight: 700;
}

/* ==========================================================
   6. FEATURED PRODUCTS SECTION
   ========================================================== */
.featured-products-section {
  padding: 24px 0 40px 0;
}

.section-head-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 16px;
}

.section-heading {
  font-size: 1.55rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.section-controls {
  display: flex;
  align-items: center;
  gap: 20px;
}

.filter-tab-buttons {
  display: flex;
  align-items: center;
  gap: 16px;
}

.filter-btn {
  background: none;
  border: none;
  font-size: 0.92rem;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  padding: 4px 0;
  position: relative;
  transition: color 0.2s;
}

.filter-btn:hover,
.filter-btn.active {
  color: #ba441b;
  font-weight: 700;
}

.filter-btn.active::after {
  content: "";
  position: absolute;
  left: 0;
  bottom: -2px;
  width: 100%;
  height: 2px;
  background: #ba441b;
  border-radius: 2px;
}

.arrow-nav-group {
  display: flex;
  align-items: center;
  gap: 6px;
}

.nav-arrow-btn {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  color: #475569;
  font-size: 1.1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.nav-arrow-btn:hover {
  background: #ffedd5;
  border-color: #fed7aa;
  color: #ba441b;
}

/* Products Grid */
.products-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 16px;
}

@media (max-width: 1200px) {
  .products-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 640px) {
  .products-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

.product-card {
  background: #ffffff;
  border: 1px solid #f1f5f9;
  border-radius: 16px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  transition: all 0.2s ease-in-out;
  position: relative;
}

.product-card:hover {
  transform: translateY(-4px);
  border-color: #fed7aa;
  box-shadow: 0 10px 24px rgba(186, 68, 27, 0.08);
}

.card-top-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.product-cat-tag {
  font-size: 0.72rem;
  font-weight: 600;
  color: #94a3b8;
}

.heart-toggle-btn {
  background: none;
  border: none;
  font-size: 0.95rem;
  cursor: pointer;
  padding: 2px;
  transition: transform 0.15s;
}

.heart-toggle-btn:hover {
  transform: scale(1.2);
}

.product-media-wrap {
  width: 100%;
  height: 130px;
  margin: 10px 0;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  overflow: hidden;
  border-radius: 8px;
}

.product-photo {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  transition: transform 0.3s ease;
}

.product-card:hover .product-photo {
  transform: scale(1.06);
}

.weight-tags-row {
  display: flex;
  gap: 6px;
  margin-bottom: 8px;
}

.weight-tag {
  background: #f8fafc;
  color: #64748b;
  border: 1px solid #e2e8f0;
  font-size: 0.68rem;
  font-weight: 600;
  padding: 2px 6px;
  border-radius: 4px;
}

.product-price-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 6px;
}

.price-current {
  font-size: 0.95rem;
  font-weight: 800;
  color: #ba441b;
}

.price-old {
  font-size: 0.78rem;
  color: #94a3b8;
  text-decoration: line-through;
}

.discount-badge {
  background: #ba441b;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: 800;
  padding: 2px 6px;
  border-radius: 4px;
}

.product-card-title {
  margin: 0 0 8px 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: #1e293b;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  min-height: 38px;
  cursor: pointer;
}

.product-card-title:hover {
  color: #ba441b;
}

.product-stars-row {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 12px;
}

.stars-gold {
  color: #f59e0b;
  font-size: 0.8rem;
  letter-spacing: 1px;
}

.review-score {
  font-size: 0.72rem;
  color: #64748b;
}

.btn-select-options {
  margin-top: auto;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  background: #fdf6ec;
  color: #ba441b;
  border: 1px solid #fed7aa;
  border-radius: 8px;
  padding: 8px 0;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-select-options:hover {
  background: #ba441b;
  color: #ffffff;
  border-color: #ba441b;
}

/* ==========================================================
   7. WIDE PROMO BANNER (SHEA LOTION)
   ========================================================== */
.wide-promo-section {
  padding: 16px 0 36px 0;
}

.wide-promo-card {
  background: linear-gradient(90deg, #fef3ec 0%, #fae8de 60%, #f6ddcf 100%);
  border-radius: 20px;
  padding: clamp(20px, 3.5vw, 36px) clamp(24px, 4vw, 48px);
  display: flex;
  align-items: center;
  justify-content: space-between;
  border: 1px solid #fed7aa;
  overflow: hidden;
  position: relative;
  gap: 20px;
}

.wide-promo-left {
  max-width: 440px;
}

.promo-overline {
  font-size: 0.9rem;
  font-weight: 600;
  color: #64748b;
  display: block;
  margin-bottom: 6px;
}

.promo-main-heading {
  margin: 0;
  font-size: clamp(1.4rem, 2.4vw, 2.2rem);
  font-weight: 900;
  color: #0f172a;
  line-height: 1.2;
}

.wide-promo-center {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
}

.pricing-label {
  font-size: 0.88rem;
  font-weight: 800;
  color: #0f172a;
}

.pricing-oval-tag {
  background: #ba441b;
  color: #ffffff;
  font-size: 1.5rem;
  font-weight: 900;
  padding: 10px 24px;
  border-radius: 999px;
  position: relative;
  box-shadow: 0 6px 18px rgba(186, 68, 27, 0.35);
}

.wide-promo-right {
  width: 260px;
  height: 140px;
  border-radius: 12px;
  overflow: hidden;
  flex-shrink: 0;
}

.lotion-bottles-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* ==========================================================
   8. TOP SELLER USERS SECTION
   ========================================================== */
.top-sellers-section {
  padding: 16px 0 36px 0;
}

.sellers-head-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 24px;
}

.sellers-title-wrap {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.sellers-heading {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 800;
  color: #0f172a;
}

.heading-accent-line {
  width: 50px;
  height: 4px;
  background: #ba441b;
  border-radius: 999px;
}

.see-more-link {
  font-size: 0.88rem;
  font-weight: 700;
  color: #ba441b;
  text-decoration: none;
  border: 1px solid #fed7aa;
  padding: 6px 16px;
  border-radius: 999px;
  background: #ffffff;
  transition: all 0.2s;
}

.see-more-link:hover {
  background: #ba441b;
  color: #ffffff;
}

.sellers-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

@media (max-width: 900px) {
  .sellers-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

.seller-card {
  background: #ffffff;
  border: 1px solid #f1f5f9;
  border-radius: 14px;
  padding: 14px;
  display: flex;
  align-items: center;
  gap: 14px;
  transition: all 0.2s;
}

.seller-card:hover {
  border-color: #fed7aa;
  box-shadow: 0 6px 18px rgba(186, 68, 27, 0.08);
}

.seller-avatar-wrap {
  width: 60px;
  height: 60px;
  border-radius: 12px;
  overflow: hidden;
  flex-shrink: 0;
}

.seller-avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.seller-info {
  flex: 1;
}

.seller-featured-tag {
  font-size: 0.65rem;
  font-weight: 700;
  color: #ba441b;
  background: #ffedd5;
  padding: 2px 6px;
  border-radius: 4px;
  display: inline-block;
  margin-bottom: 4px;
}

.seller-name {
  margin: 0 0 4px 0;
  font-size: 0.95rem;
  font-weight: 700;
  color: #0f172a;
}

.seller-stars {
  color: #f59e0b;
  font-size: 0.78rem;
  letter-spacing: 1px;
}

/* ==========================================================
   9. DEAL OF THE WEEK SECTION
   ========================================================== */
.deal-week-section {
  padding: 16px 0 60px 0;
}

.deal-week-card {
  background: #ffffff;
  border: 2.5px solid #ba441b;
  border-radius: 20px;
  padding: clamp(18px, 3vw, 32px);
}

.deal-head-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid #f1f5f9;
}

.deal-heading {
  margin: 0;
  font-size: 1.6rem;
  font-weight: 800;
  color: #0f172a;
}

.deal-countdown-cluster {
  display: flex;
  gap: 8px;
}

.countdown-unit-box {
  background: #ba441b;
  color: #ffffff;
  min-width: 48px;
  height: 48px;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4px;
}

.countdown-num {
  font-size: 1.05rem;
  font-weight: 900;
  line-height: 1;
}

.countdown-label {
  font-size: 0.62rem;
  text-transform: uppercase;
  font-weight: 600;
  opacity: 0.9;
}

.deal-body-grid {
  display: grid;
  grid-template-columns: 1fr 2.2fr;
  gap: 20px;
}

@media (max-width: 992px) {
  .deal-body-grid {
    grid-template-columns: 1fr;
  }
}

.deal-feature-spotlight {
  position: relative;
  border-radius: 16px;
  overflow: hidden;
  min-height: 260px;
}

.spotlight-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.spotlight-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(15, 23, 42, 0.2) 0%, rgba(15, 23, 42, 0.85) 100%);
  padding: 24px;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  color: #ffffff;
}

.spotlight-tag {
  font-size: 0.75rem;
  font-weight: 700;
  color: #ffedd5;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.spotlight-title {
  margin: 6px 0;
  font-size: 1.4rem;
  font-weight: 800;
}

.spotlight-discount {
  font-size: 1.1rem;
  font-weight: 800;
  color: #fed7aa;
  margin-bottom: 12px;
}

.btn-spotlight-shop {
  background: #ba441b;
  color: #ffffff;
  border: none;
  font-size: 0.85rem;
  font-weight: 700;
  padding: 8px 18px;
  border-radius: 999px;
  align-self: flex-start;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-spotlight-shop:hover {
  background: #9a3412;
}

.deal-items-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

@media (max-width: 768px) {
  .deal-items-row {
    grid-template-columns: repeat(2, 1fr);
  }
}

.deal-product-item {
  background: #ffffff;
  border: 1px solid #f1f5f9;
  border-radius: 14px;
  padding: 12px;
  display: flex;
  flex-direction: column;
  transition: all 0.2s;
}

.deal-product-item:hover {
  border-color: #fed7aa;
  box-shadow: 0 8px 20px rgba(186, 68, 27, 0.08);
}

.deal-item-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.deal-item-cat {
  font-size: 0.72rem;
  color: #94a3b8;
  font-weight: 600;
}

.deal-item-heart {
  font-size: 0.9rem;
  cursor: pointer;
}

.deal-item-media {
  width: 100%;
  height: 110px;
  margin: 8px 0;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.deal-item-img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}

.deal-item-price-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 4px;
}

.deal-curr-price {
  font-size: 0.92rem;
  font-weight: 800;
  color: #ba441b;
}

.deal-old-price {
  font-size: 0.75rem;
  color: #94a3b8;
  text-decoration: line-through;
}

.deal-badge {
  background: #ba441b;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: 800;
  padding: 2px 4px;
  border-radius: 4px;
}

.deal-item-title {
  margin: 0 0 6px 0;
  font-size: 0.82rem;
  font-weight: 600;
  color: #1e293b;
  line-height: 1.3;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  min-height: 34px;
}

.deal-item-stars {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 10px;
}

.btn-deal-add {
  margin-top: auto;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  background: #fdf6ec;
  color: #ba441b;
  border: 1px solid #fed7aa;
  border-radius: 8px;
  padding: 6px 0;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-deal-add:hover {
  background: #ba441b;
  color: #ffffff;
  border-color: #ba441b;
}

/* ==========================================================
   RESPONSIVE QUERIES
   ========================================================== */
@media (max-width: 992px) {
  .hero-grid {
    grid-template-columns: 1fr;
  }
  .sub-nav-inner {
    flex-wrap: wrap;
  }
  .nav-menu-list {
    gap: 16px;
    flex-wrap: wrap;
  }
}

@media (max-width: 768px) {
  .info-bar-content {
    justify-content: center;
    text-align: center;
  }
  .header-inner {
    flex-wrap: wrap;
  }
  .search-cluster {
    order: 3;
    max-width: 100%;
    width: 100%;
  }
  .wide-promo-card {
    flex-direction: column;
    text-align: center;
  }
  .wide-promo-right {
    width: 100%;
    height: 160px;
  }
}
</style>
