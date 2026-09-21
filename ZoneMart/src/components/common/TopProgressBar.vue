<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const isVisible = ref(false);
const progress = ref(0);
let trickleTimer: any = null;
let hideTimer: any = null;

const start = () => {
  if (hideTimer) clearTimeout(hideTimer);
  if (trickleTimer) clearInterval(trickleTimer);

  progress.value = 18;
  isVisible.value = true;

  trickleTimer = setInterval(() => {
    if (progress.value < 85) {
      // Tăng ngẫu nhiên nhẹ nhàng
      const step = Math.random() * 12 + 4;
      progress.value = Math.min(85, progress.value + step);
    }
  }, 120);
};

const finish = () => {
  if (trickleTimer) clearInterval(trickleTimer);
  progress.value = 100;

  hideTimer = setTimeout(() => {
    isVisible.value = false;
    setTimeout(() => {
      progress.value = 0;
    }, 200);
  }, 320);
};

onMounted(() => {
  router.beforeEach((_to, _from, next) => {
    start();
    next();
  });

  router.afterEach(() => {
    finish();
  });

  // Cho phép gọi thủ công từ bất kỳ đâu qua sự kiện window
  window.addEventListener('zonemart:loading-start', start);
  window.addEventListener('zonemart:loading-finish', finish);
});

onUnmounted(() => {
  if (trickleTimer) clearInterval(trickleTimer);
  if (hideTimer) clearTimeout(hideTimer);
  window.removeEventListener('zonemart:loading-start', start);
  window.removeEventListener('zonemart:loading-finish', finish);
});
</script>

<template>
  <div
    v-show="isVisible"
    class="zm-top-progress-container"
    aria-hidden="true"
  >
    <div
      class="zm-top-progress-bar"
      :style="{
        width: `${progress}%`,
        opacity: isVisible ? 1 : 0
      }"
    >
      <!-- Đầu vệt sáng phát quang -->
      <div class="zm-progress-glow-head"></div>
    </div>
  </div>
</template>

<style scoped>
.zm-top-progress-container {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  width: 100%;
  height: 3.5px;
  z-index: 9999999;
  pointer-events: none;
  background: transparent;
}

.zm-top-progress-bar {
  height: 100%;
  background: linear-gradient(90deg, #ea580c 0%, #f97316 60%, #fbbf24 100%);
  border-radius: 0 4px 4px 0;
  position: relative;
  transition: width 0.22s ease-out, opacity 0.25s ease;
  box-shadow: 0 0 10px rgba(234, 88, 12, 0.65), 0 0 4px rgba(251, 191, 36, 0.4);
}

.zm-progress-glow-head {
  position: absolute;
  right: 0;
  top: -2px;
  width: 80px;
  height: 7.5px;
  background: radial-gradient(ellipse at center, #ffffff 0%, rgba(251, 191, 36, 0.8) 40%, rgba(234, 88, 12, 0) 100%);
  border-radius: 50%;
  filter: blur(1.5px);
  animation: headPulse 0.8s infinite alternate ease-in-out;
}

@keyframes headPulse {
  0% {
    transform: scaleX(0.85);
    opacity: 0.75;
  }
  100% {
    transform: scaleX(1.15);
    opacity: 1;
  }
}
</style>
