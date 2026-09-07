<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const searchQuery = ref("");
const cartCount = ref(0);

const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({ path: "/products", query: { q: searchQuery.value } });
  } else {
    router.push("/products");
  }
};
</script>

<template>
  <div class="market-home-page">
    <!-- TOP HEADER / SEARCH BAR -->
    <header class="market-top-header">
      <div class="top-header-inner">
        <!-- Logo / Brand Title -->
        <router-link to="/" class="brand-title-link">
          <span class="brand-name-text">LOCAL MARKET</span>
        </router-link>

        <!-- Search Pill -->
        <div class="search-pill-container">
          <span class="search-icon-lens">🔍</span>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search"
            class="search-pill-input"
            @keyup.enter="handleSearch"
          />
        </div>

        <!-- Action Icons: Profile & Cart -->
        <div class="header-user-actions">
          <router-link to="/profile" class="header-icon-btn" title="Profile Info">
            <span class="btn-icon">👤</span>
          </router-link>

          <router-link to="/cart" class="header-icon-btn" title="Cart">
            <span class="btn-icon">🛒</span>
            <span class="cart-badge-pill">{{ cartCount }}</span>
          </router-link>
        </div>
      </div>
    </header>

    <!-- HERO BANNER SECTION -->
    <section class="market-hero-section">
      <div class="hero-banner-card">
        <!-- Dark gradient overlay for typography readability -->
        <div class="hero-shade-overlay"></div>

        <div class="hero-banner-content">
          <!-- Left Text Block -->
          <div class="hero-text-block">
            <h1 class="hero-heading-title">
              Local Market<br />Local Vendors
            </h1>
            <p class="hero-description-text">
              Prime our assonars in moonyship with local vendors commanalats.
            </p>
            <button class="btn-explore-pill" @click="router.push('/products')">
              EXPLORE MARKET
            </button>
          </div>

          <!-- Right Line-Art Graphics -->
          <div class="hero-lineart-block">
            <svg class="lineart-graphic-svg" viewBox="0 0 220 220" fill="none">
              <!-- Stylized bowl and fruits -->
              <circle cx="110" cy="110" r="85" stroke="#f97316" stroke-width="2.5" stroke-dasharray="6 6" opacity="0.4"/>
              <path d="M65 130 C 65 175, 155 175, 155 130 Z" stroke="#ea580c" stroke-width="4.5" fill="rgba(234, 88, 12, 0.25)"/>
              <circle cx="95" cy="115" r="20" stroke="#ea580c" stroke-width="4" fill="rgba(234, 88, 12, 0.35)"/>
              <circle cx="125" cy="115" r="20" stroke="#ea580c" stroke-width="4" fill="rgba(234, 88, 12, 0.35)"/>
              <path d="M95 95 Q 100 82 108 88" stroke="#ea580c" stroke-width="3.5" stroke-linecap="round" fill="none"/>
              <path d="M125 95 Q 130 82 138 88" stroke="#ea580c" stroke-width="3.5" stroke-linecap="round" fill="none"/>
              <!-- Radiating arcs -->
              <path d="M165 95 A 40 40 0 0 1 185 135" stroke="#ea580c" stroke-width="3" stroke-linecap="round" fill="none" opacity="0.6"/>
              <path d="M175 85 A 55 55 0 0 1 200 140" stroke="#ea580c" stroke-width="2.5" stroke-linecap="round" fill="none" opacity="0.4"/>
            </svg>
          </div>
        </div>
      </div>
    </section>

    <!-- 3 REGISTRATION CARDS (BUYER, SELLER, SHIPPER) -->
    <section class="registration-cards-section">
      <div class="cards-layout-grid">
        <!-- CARD 1: BUYER REGISTRATION -->
        <div class="reg-action-card">
          <div class="reg-icon-square">
            <svg class="reg-svg-icon" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
            </svg>
          </div>
          <h3 class="reg-card-title">BUYER REGISTRATION</h3>
          <p class="reg-card-desc">
            Get action navigation buyer access from engage to pay access.
          </p>
          <button class="btn-card-action" @click="router.push('/register-buyer')">
            EXPLORE MARKET
          </button>
        </div>

        <!-- CARD 2: SELLER REGISTRATION -->
        <div class="reg-action-card">
          <div class="reg-icon-square">
            <svg class="reg-svg-icon" viewBox="0 0 24 24" fill="currentColor">
              <path d="M20 4H4v2h16V4zm1 10v-2l-1-5H4l-1 5v2h1v6h10v-6h4v6h2v-6h1zm-9 4H6v-4h6v4z"/>
            </svg>
          </div>
          <h3 class="reg-card-title">SELLER REGISTRATION</h3>
          <p class="reg-card-desc">
            Unist a towark pots and seller access from funding local market.
          </p>
          <button class="btn-card-action" @click="router.push('/register-seller')">
            SELLER MARKET
          </button>
        </div>

        <!-- CARD 3: SHIPPER REGISTRATION -->
        <div class="reg-action-card">
          <div class="reg-icon-square">
            <svg class="reg-svg-icon" viewBox="0 0 24 24" fill="currentColor">
              <path d="M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4zM6 18.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zm13.5-9l1.96 2.5H17V9.5h2.5zm-1.5 9c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z"/>
            </svg>
          </div>
          <h3 class="reg-card-title">SHIPPER REGISTRATION</h3>
          <p class="reg-card-desc">
            Shipper access to evallgure access, send inspectors and shipper secealy.
          </p>
          <button class="btn-card-action" @click="router.push('/shipper')">
            SHIPPER MARKET
          </button>
        </div>
      </div>
    </section>

    <!-- FEATURED CATEGORY SECTION -->
    <section class="featured-category-section">
      <div class="featured-header-row">
        <h2 class="featured-heading-text">Featured Category</h2>
        <button class="btn-view-all-link" @click="router.push('/products')">
          View All
        </button>
      </div>

      <!-- 4 Category Tiles Grid -->
      <div class="category-cards-grid">
        <!-- 1: FRESH PRODUCE -->
        <div class="cat-feature-card" @click="router.push('/products')">
          <div class="cat-card-header">
            <h4>FRESH<br />PRODUCE</h4>
          </div>
          <div class="cat-card-visual apple-visual">
            <svg class="cat-decor-svg" viewBox="0 0 100 100" fill="currentColor">
              <path d="M50 30 C40 10, 20 20, 25 45 C30 70, 50 85, 50 85 C50 85, 70 70, 75 45 C80 20, 60 10, 50 30 Z" fill="rgba(255, 255, 255, 0.25)"/>
            </svg>
          </div>
        </div>

        <!-- 2: ARTISAN GOODS -->
        <div class="cat-feature-card" @click="router.push('/products')">
          <div class="cat-card-header">
            <h4>ARTISAN<br />GOODS</h4>
          </div>
          <div class="cat-card-visual">
            <img
              src="https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=400&q=80"
              alt="Artisan Goods"
              class="cat-photo"
            />
          </div>
        </div>

        <!-- 3: LOCAL SERVICES -->
        <div class="cat-feature-card" @click="router.push('/products')">
          <div class="cat-card-header">
            <h4>LOCAL<br />SERVICES</h4>
          </div>
          <div class="cat-card-visual">
            <img
              src="https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=400&q=80"
              alt="Local Services"
              class="cat-photo"
            />
          </div>
        </div>

        <!-- 4: CRLORS' & PACKAGE -->
        <div class="cat-feature-card" @click="router.push('/products')">
          <div class="cat-card-header">
            <h4>CRLORS' &<br />PACKAGE</h4>
          </div>
          <div class="cat-card-visual">
            <img
              src="https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=400&q=80"
              alt="Package"
              class="cat-photo"
            />
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* ==========================================================================
   GLOBAL PAGE STYLES (WARM IVORY / CREAM & TERRACOTTA #ba441b)
   ========================================================================== */
.market-home-page {
  background-color: #fdfaf6;
  min-height: 100vh;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  color: #2b231d;
  padding-bottom: 70px;
}

/* ==========================================================================
   TOP HEADER BAR
   ========================================================================== */
.market-top-header {
  background-color: #fcf8f3;
  padding: 16px clamp(20px, 3.5vw, 60px);
  width: 100%;
  border-bottom: 1px solid #ebdcd1;
}

.top-header-inner {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
}

.brand-title-link {
  text-decoration: none;
}

.brand-name-text {
  font-size: 26px;
  font-weight: 800;
  color: #ba441b;
  letter-spacing: 0.5px;
}

/* Search Pill */
.search-pill-container {
  flex: 1;
  max-width: 650px;
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon-lens {
  position: absolute;
  left: 16px;
  font-size: 14px;
  color: #9c8e84;
  pointer-events: none;
}

.search-pill-input {
  width: 100%;
  padding: 11px 20px 11px 42px;
  background-color: #f6efe8;
  border: 1px solid #ebdcd1;
  border-radius: 24px;
  font-size: 14px;
  color: #3b2c23;
  outline: none;
  transition: all 0.2s;
}

.search-pill-input:focus {
  background-color: #ffffff;
  border-color: #ba441b;
  box-shadow: 0 0 0 3px rgba(186, 68, 27, 0.1);
}

.search-pill-input::placeholder {
  color: #a89a90;
}

/* Header User Actions */
.header-user-actions {
  display: flex;
  align-items: center;
  gap: 20px;
}

.header-icon-btn {
  text-decoration: none;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.btn-icon {
  font-size: 22px;
  color: #3b2c23;
  transition: transform 0.15s;
}

.header-icon-btn:hover .btn-icon {
  transform: scale(1.1);
}

.cart-badge-pill {
  position: absolute;
  top: -4px;
  right: -10px;
  background-color: #ea580c;
  color: #ffffff;
  font-size: 10px;
  font-weight: 800;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* ==========================================================================
   HERO BANNER SECTION (FULL WIDTH)
   ========================================================================== */
.market-hero-section {
  width: 100%;
  margin: 18px 0 28px 0;
  padding: 0 clamp(20px, 3.5vw, 60px);
}

.hero-banner-card {
  position: relative;
  border-radius: 24px;
  overflow: hidden;
  background-image: url("https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=1600&q=80");
  background-size: cover;
  background-position: center 30%;
  min-height: 480px;
  width: 100%;
  display: flex;
  align-items: center;
  box-shadow: 0 10px 30px rgba(78, 42, 23, 0.1);
}

.hero-shade-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: linear-gradient(
    90deg,
    rgba(25, 18, 14, 0.85) 0%,
    rgba(40, 25, 18, 0.68) 50%,
    rgba(35, 22, 16, 0.42) 100%
  );
}

.hero-banner-content {
  position: relative;
  z-index: 2;
  width: 100%;
  padding: 56px clamp(24px, 4vw, 60px);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.hero-text-block {
  max-width: 620px;
}

.hero-heading-title {
  font-size: clamp(36px, 4vw, 54px);
  line-height: 1.15;
  font-weight: 800;
  color: #ffffff;
  margin: 0 0 18px 0;
  text-shadow: 0 2px 8px rgba(0, 0, 0, 0.4);
}

.hero-description-text {
  font-size: clamp(15px, 1.3vw, 18px);
  line-height: 1.55;
  color: rgba(255, 255, 255, 0.92);
  margin: 0 0 28px 0;
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.4);
}

.btn-explore-pill {
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  padding: 14px 32px;
  border-radius: 28px;
  font-size: 15px;
  font-weight: 800;
  letter-spacing: 0.8px;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(186, 68, 27, 0.4);
  transition: all 0.2s;
}

.btn-explore-pill:hover {
  background-color: #a33813;
  transform: translateY(-2px);
  box-shadow: 0 6px 18px rgba(186, 68, 27, 0.5);
}

.hero-lineart-block {
  width: 220px;
  height: 220px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.lineart-graphic-svg {
  width: 100%;
  height: 100%;
}

/* ==========================================================================
   3 REGISTRATION CARDS (FULL WIDTH)
   ========================================================================== */
.registration-cards-section {
  width: 100%;
  margin: 16px 0 44px 0;
  padding: 0 clamp(20px, 3.5vw, 60px);
}

.cards-layout-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
  width: 100%;
}

.reg-action-card {
  background-color: #fdfaf5;
  border: 1.5px solid #cb774c;
  border-radius: 20px;
  padding: 28px 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  box-shadow: 0 6px 20px rgba(92, 45, 23, 0.06);
  transition: transform 0.2s, box-shadow 0.2s;
}

.reg-action-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 28px rgba(92, 45, 23, 0.12);
}

.reg-icon-square {
  width: 62px;
  height: 62px;
  background-color: #ea580c;
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 18px;
  box-shadow: 0 4px 12px rgba(234, 88, 12, 0.3);
}

.reg-svg-icon {
  width: 32px;
  height: 32px;
  color: #ffffff;
}

.reg-card-title {
  margin: 0 0 10px 0;
  font-size: 16px;
  font-weight: 800;
  color: #2b231d;
  letter-spacing: 0.5px;
}

.reg-card-desc {
  font-size: 14px;
  line-height: 1.5;
  color: #6a5e55;
  margin: 0 0 22px 0;
  min-height: 48px;
}

.btn-card-action {
  width: 100%;
  background-color: #ba441b;
  color: #ffffff;
  border: none;
  padding: 12px 20px;
  border-radius: 24px;
  font-size: 14px;
  font-weight: 800;
  letter-spacing: 0.6px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-card-action:hover {
  background-color: #a33813;
}

/* ==========================================================================
   FEATURED CATEGORY SECTION (FULL WIDTH)
   ========================================================================== */
.featured-category-section {
  width: 100%;
  margin: 0 0 20px 0;
  padding: 0 clamp(20px, 3.5vw, 60px);
}

.featured-header-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 20px;
}

.featured-heading-text {
  margin: 0;
  font-size: 24px;
  font-weight: 800;
  color: #2b231d;
}

.btn-view-all-link {
  background: transparent;
  border: none;
  color: #6a5e55;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  padding: 4px;
  transition: color 0.2s;
}

.btn-view-all-link:hover {
  color: #ba441b;
  text-decoration: underline;
}

.category-cards-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  width: 100%;
}

.cat-feature-card {
  background-color: #ea580c;
  border-radius: 18px;
  overflow: hidden;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  box-shadow: 0 6px 18px rgba(234, 88, 12, 0.15);
  transition: transform 0.2s, box-shadow 0.2s;
}

.cat-feature-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 24px rgba(234, 88, 12, 0.25);
}

.cat-card-header {
  padding: 18px 16px 14px 16px;
  color: #ffffff;
}

.cat-card-header h4 {
  margin: 0;
  font-size: 16px;
  font-weight: 800;
  letter-spacing: 0.5px;
  line-height: 1.25;
}

.cat-card-visual {
  width: 100%;
  height: 150px;
  overflow: hidden;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.apple-visual {
  background: radial-gradient(circle, #f97316 0%, #ea580c 100%);
}

.cat-decor-svg {
  width: 70px;
  height: 70px;
}

.cat-photo {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}

.cat-feature-card:hover .cat-photo {
  transform: scale(1.08);
}

/* ==========================================================================
   RESPONSIVE STYLES
   ========================================================================== */
@media (max-width: 900px) {
  .hero-heading-title {
    font-size: 34px;
  }
  .cards-layout-grid {
    grid-template-columns: 1fr;
  }
  .category-cards-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 600px) {
  .hero-banner-content {
    padding: 32px 20px;
  }
  .hero-heading-title {
    font-size: 28px;
  }
  .hero-lineart-block {
    display: none;
  }
  .category-cards-grid {
    grid-template-columns: 1fr;
  }
  .search-pill-container {
    display: none;
  }
}
</style>

