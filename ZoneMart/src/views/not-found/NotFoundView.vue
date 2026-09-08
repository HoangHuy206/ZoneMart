<script setup lang="ts">
import { useRouter } from "vue-router";

const router = useRouter();

const goHome = () => {
  router.push("/");
};

const goProducts = () => {
  router.push("/products");
};
</script>

<template>
  <div class="not-found-page">
    <div class="not-found-container">
      <!-- Center Gate & Padlock Vector Illustration -->
      <div class="illustration-wrapper">
        <svg 
          class="gate-svg" 
          viewBox="0 0 320 280" 
          fill="none" 
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden="true"
        >
          <!-- Ground baseline (Orange) -->
          <line x1="28" y1="248" x2="292" y2="248" stroke="#ea580c" stroke-width="7" stroke-linecap="round" />

          <!-- Outer Orange Portal Arch -->
          <path 
            d="M 52 248 V 52 Q 52 38 66 38 H 254 Q 268 38 268 52 V 248" 
            stroke="#ea580c" 
            stroke-width="7" 
            stroke-linecap="round" 
            stroke-linejoin="round" 
            fill="none" 
          />

          <!-- Iron Gate Hinges (Dark Charcoal) -->
          <rect x="46" y="82" width="7" height="15" rx="2" fill="#18181b" />
          <rect x="46" y="196" width="7" height="15" rx="2" fill="#18181b" />
          <rect x="267" y="82" width="7" height="15" rx="2" fill="#18181b" />
          <rect x="267" y="196" width="7" height="15" rx="2" fill="#18181b" />

          <!-- Left Gate Leaf -->
          <path 
            d="M 62 236 V 90 Q 95 62 156 54 V 236 Z" 
            stroke="#18181b" 
            stroke-width="4.5" 
            stroke-linejoin="round" 
            fill="none" 
          />
          
          <!-- Left Leaf Vertical Bars -->
          <line x1="85.5" y1="236" x2="85.5" y2="76" stroke="#18181b" stroke-width="3.5" stroke-linecap="round" />
          <line x1="109" y1="236" x2="109" y2="67" stroke="#18181b" stroke-width="3.5" stroke-linecap="round" />
          <line x1="132.5" y1="236" x2="132.5" y2="59" stroke="#18181b" stroke-width="3.5" stroke-linecap="round" />

          <!-- Right Gate Leaf -->
          <path 
            d="M 258 236 V 90 Q 225 62 164 54 V 236 Z" 
            stroke="#18181b" 
            stroke-width="4.5" 
            stroke-linejoin="round" 
            fill="none" 
          />

          <!-- Right Leaf Vertical Bars -->
          <line x1="187.5" y1="236" x2="187.5" y2="59" stroke="#18181b" stroke-width="3.5" stroke-linecap="round" />
          <line x1="211" y1="236" x2="211" y2="67" stroke="#18181b" stroke-width="3.5" stroke-linecap="round" />
          <line x1="234.5" y1="236" x2="234.5" y2="76" stroke="#18181b" stroke-width="3.5" stroke-linecap="round" />

          <!-- Center Padlock (Interactive Hover Group) -->
          <g class="padlock-group">
            <!-- Padlock Shackle Arch (Orange) -->
            <path 
              d="M 143 148 V 126 C 143 111 177 111 177 126 V 148" 
              stroke="#ea580c" 
              stroke-width="8" 
              stroke-linecap="round" 
              fill="none" 
            />
            <!-- Padlock Body (Rounded Rect Orange) -->
            <rect x="131" y="142" width="58" height="50" rx="9" fill="#ea580c" />
            <!-- Keyhole (White) -->
            <circle cx="160" cy="161" r="5" fill="#ffffff" />
            <path 
              d="M 157.5 162 L 162.5 162 L 164 176 C 164 177.5 162 178 160 178 C 158 178 156 177.5 156 176 Z" 
              fill="#ffffff" 
            />
          </g>
        </svg>
      </div>

      <!-- Typography & Messages -->
      <div class="content-wrapper">
        <h1 class="error-title">404 - ACCESS DENIED</h1>
        <p class="error-desc">
          It seems this market is temporarily closed or you do not have permission to access it.
        </p>

        <!-- Navigation Actions -->
        <div class="actions-row">
          <button class="btn btn-primary" @click="goHome">
            <span class="btn-arrow">←</span> Quay Về Trang Chủ
          </button>
          <button class="btn btn-secondary" @click="goProducts">
            🛍️ Khám Phá Sản Phẩm
          </button>
        </div>

        <!-- Help hint -->
        <p class="help-text">
          Đường dẫn bạn vừa truy cập không tồn tại hoặc đã thay đổi địa chỉ.
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Page Layout: Warm cream eggshell background full screen */
.not-found-page {
  min-height: 100vh;
  background-color: #faf8f5;
  background-image: radial-gradient(#e2e8f0 0.8px, transparent 0.8px);
  background-size: 24px 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px 24px;
}

.not-found-container {
  width: 100%;
  max-width: 580px;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
}

/* Illustration Wrapper */
.illustration-wrapper {
  width: 100%;
  max-width: 320px;
  margin: 0 auto 32px auto;
  display: flex;
  justify-content: center;
}

.gate-svg {
  width: 100%;
  height: auto;
  overflow: visible;
  filter: drop-shadow(0 8px 24px rgba(0, 0, 0, 0.04));
  animation: floatIn 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

.padlock-group {
  transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
  transform-origin: 160px 165px;
}

.gate-svg:hover .padlock-group {
  transform: scale(1.06) rotate(-2deg);
}

/* Content & Typography */
.content-wrapper {
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.error-title {
  font-family: 'Playfair Display', Didot, 'Bodoni MT', Georgia, serif;
  font-size: 34px;
  font-weight: 900;
  letter-spacing: 0.02em;
  color: #18181b;
  margin: 0 0 16px 0;
  line-height: 1.2;
}

.error-desc {
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  font-size: 15px;
  line-height: 1.65;
  color: #52525b;
  max-width: 420px;
  margin: 0 0 32px 0;
  font-weight: 500;
}

/* Actions Row */
.actions-row {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 20px;
}

.btn {
  font-family: 'Plus Jakarta Sans', -apple-system, sans-serif;
  font-size: 14px;
  font-weight: 700;
  padding: 12px 24px;
  border-radius: 9999px;
  border: none;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  text-decoration: none;
}

.btn-primary {
  background: #18181b;
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(24, 24, 27, 0.18);
}

.btn-primary:hover {
  background: #ea580c;
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(234, 88, 12, 0.28);
}

.btn-arrow {
  font-size: 16px;
  transition: transform 0.2s ease;
}

.btn-primary:hover .btn-arrow {
  transform: translateX(-3px);
}

.btn-secondary {
  background: #ffffff;
  color: #334155;
  border: 1px solid #e2e8f0;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.03);
}

.btn-secondary:hover {
  background: #fff7ed;
  border-color: #fdba74;
  color: #ea580c;
  transform: translateY(-2px);
}

.help-text {
  font-size: 12px;
  color: #94a3b8;
  margin: 0;
  font-weight: 500;
}

/* Animation */
@keyframes floatIn {
  0% {
    opacity: 0;
    transform: translateY(16px) scale(0.96);
  }
  100% {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Responsive adjustments */
@media (max-width: 640px) {
  .not-found-page {
    padding: 32px 16px 56px 16px;
  }

  .illustration-wrapper {
    max-width: 260px;
    margin-bottom: 24px;
  }

  .error-title {
    font-size: 26px;
  }

  .error-desc {
    font-size: 14px;
    max-width: 320px;
    margin-bottom: 24px;
  }

  .actions-row {
    flex-direction: column;
    width: 100%;
  }

  .btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
