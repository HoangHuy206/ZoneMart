<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import logoEmblem from '../../assets/logo_emblem.png';

const emit = defineEmits<{
  (e: 'complete'): void;
}>();

const isVisible = ref(true);
const isFadingOut = ref(false);
const animationStage = ref<'stage-hidden' | 'stage-emerge' | 'stage-straighten' | 'stage-shine'>('stage-hidden');

let emergeTimer: any = null;
let straightenTimer: any = null;
let shineTimer: any = null;
let dismissTimer: any = null;

const dismiss = () => {
  if (isFadingOut.value) return;
  isFadingOut.value = true;
  emit('complete');

  setTimeout(() => {
    isVisible.value = false;
  }, 450);
};

const startAnimation = () => {
  isVisible.value = true;
  isFadingOut.value = false;
  animationStage.value = 'stage-hidden';

  if (emergeTimer) clearTimeout(emergeTimer);
  if (straightenTimer) clearTimeout(straightenTimer);
  if (shineTimer) clearTimeout(shineTimer);
  if (dismissTimer) clearTimeout(dismissTimer);

  // Giai đoạn 1: Logo bắt đầu từ trong suốt và hiện lên dần dần (fade in + trồi lên từ chiều sâu)
  emergeTimer = setTimeout(() => {
    animationStage.value = 'stage-emerge';
  }, 60);

  // Giai đoạn 2: Logo từ góc nghiêng 3D lượn đứng thẳng lên trung tâm mượt mà (0.95s)
  straightenTimer = setTimeout(() => {
    animationStage.value = 'stage-straighten';
  }, 950);

  // Giai đoạn 3: Vệt sáng quét bóng loáng & thương hiệu ZoneMart hiện ra (1.8s)
  shineTimer = setTimeout(() => {
    animationStage.value = 'stage-shine';
  }, 1800);

  // Giai đoạn 4: Hoàn tất chuyển cảnh vào trang web (3.3s)
  dismissTimer = setTimeout(() => {
    dismiss();
  }, 3300);
};

const onKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Escape' && isVisible.value) {
    dismiss();
  }
};

onMounted(() => {
  startAnimation();
  window.addEventListener('keydown', onKeydown);
  window.addEventListener('zonemart:replay-intro', startAnimation);
});

onUnmounted(() => {
  emit('complete');
  if (emergeTimer) clearTimeout(emergeTimer);
  if (straightenTimer) clearTimeout(straightenTimer);
  if (shineTimer) clearTimeout(shineTimer);
  if (dismissTimer) clearTimeout(dismissTimer);
  window.removeEventListener('keydown', onKeydown);
  window.removeEventListener('zonemart:replay-intro', startAnimation);
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="isVisible"
      class="studio-intro-overlay"
      :class="{ 'fade-exit': isFadingOut }"
      @click="dismiss"
      role="dialog"
      aria-modal="true"
      aria-label="ZoneMart 3D Intro"
    >
   

      <!-- Nền Studio ánh sáng trắng ngọc -->
      <div class="studio-lighting-vignette"></div>
      <div class="studio-ambient-glow"></div>

      <!-- Sân khấu 3D trung tâm -->
      <div class="studio-stage">
        <!-- Bóng tiếp xúc mặt sàn tối ưu hóa theo nhịp hiện dần -->
        <div class="floor-shadow-layer" :class="animationStage"></div>

        <!-- Khối 3D Logo Scene với hiệu ứng hiện lên dần dần (Fade-in & Rise-up) -->
        <div class="logo-3d-scene" :class="animationStage">
          <!-- Hào quang xanh ngọc lục bảo mượt mà -->
          <div class="emerald-neon-aura"></div>

          <!-- Lớp hiệu ứng chiều sâu 3D -->
          <div class="extrusion-bevel">
            <img :src="logoEmblem" alt="" class="emblem-img bevel-img" />
          </div>

          <!-- Mặt trước Logo chính (Front Face) -->
          <div class="logo-front-face">
            <img
              :src="logoEmblem"
              alt="ZoneMart Logo"
              class="emblem-img main-img"
              @error="($event.target as HTMLImageElement).src = '/logo_clean.png'"
            />

            <!-- Tia sáng phản quang quét bóng loáng bằng GPU -->
            <div class="specular-glint-beam" :class="{ 'run-shine': animationStage === 'stage-shine' }"></div>
          </div>
        </div>

        <!-- Chữ thương hiệu ZoneMart xuất hiện thanh lịch bên dưới logo -->
        <div class="brand-reveal-block" :class="{ 'show-brand': animationStage === 'stage-straighten' || animationStage === 'stage-shine' }">
          <h1 class="brand-name">
            <span class="text-navy">ZONE</span><span class="text-emerald">MART</span>
          </h1>
          <div class="brand-tagline-wrap">
            <span class="tagline-line"></span>
            <p class="brand-tagline">GIAO HÀNG HỎA TỐC • NÔNG SẢN TƯƠI SẠCH</p>
            <span class="tagline-line"></span>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
/* Container toàn màn hình chuẩn Studio */
.studio-intro-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999999;
  background: radial-gradient(circle at 50% 46%, #ffffff 0%, #f4f5f9 60%, #e6e9f0 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  user-select: none;
  overflow: hidden;
  opacity: 1;
  transform: translateZ(0);
  will-change: opacity, transform;
  transition: opacity 0.45s ease-out, transform 0.45s ease-out;
  cursor: pointer;
  perspective: 1200px;
}

.studio-intro-overlay.fade-exit {
  opacity: 0;
  transform: scale(1.03) translateZ(0);
  pointer-events: none;
}

/* Nút bỏ qua góc trên bên phải */
.intro-skip-pill {
  position: absolute;
  top: 24px;
  right: 28px;
  z-index: 30;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 18px;
  background: rgba(255, 255, 255, 0.95);
  border: 1px solid rgba(0, 0, 0, 0.08);
  border-radius: 999px;
  color: #374151;
  font-size: 0.85rem;
  font-weight: 600;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.06);
  cursor: pointer;
  transition: transform 0.2s ease, background 0.2s ease;
  will-change: transform;
}

.intro-skip-pill:hover {
  background: #ffffff;
  color: #059669;
  transform: translateY(-1px);
}

/* Ánh sáng nền studio */
.studio-lighting-vignette {
  position: absolute;
  inset: 0;
  background: radial-gradient(ellipse at center, transparent 45%, rgba(210, 218, 230, 0.45) 100%);
  pointer-events: none;
}

.studio-ambient-glow {
  position: absolute;
  width: 500px;
  height: 500px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(16, 185, 129, 0.12) 0%, transparent 70%);
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%) translateZ(0);
  pointer-events: none;
}

/* Khung sân khấu trung tâm */
.studio-stage {
  position: relative;
  z-index: 10;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  transform-style: preserve-3d;
}

/* Bóng tiếp xúc mặt sàn co giãn theo nhịp trồi lên */
.floor-shadow-layer {
  position: absolute;
  width: 260px;
  height: 70px;
  top: 60%;
  left: 50%;
  border-radius: 50%;
  background: radial-gradient(ellipse at center, rgba(15, 23, 42, 0.3) 0%, rgba(15, 23, 42, 0.06) 50%, transparent 75%);
  pointer-events: none;
  transform-origin: center center;
  will-change: transform, opacity;
  opacity: 0;
  transform: translate(-30%, 55px) rotate(-15deg) scale(0.3);
  transition: transform 0.85s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.85s ease;
}

.floor-shadow-layer.stage-emerge {
  opacity: 0.8;
  transform: translate(-30%, 35px) rotate(-15deg) scaleX(1.3) scaleY(0.65);
}

.floor-shadow-layer.stage-straighten,
.floor-shadow-layer.stage-shine {
  opacity: 0.55;
  transform: translate(-50%, 40px) rotate(0deg) scaleX(1) scaleY(1);
  transition: transform 0.95s cubic-bezier(0.2, 0.8, 0.2, 1), opacity 0.95s ease;
}

/* Khối 3D Logo Scene - Ban đầu ẩn hoàn toàn và mờ ảo, sau đó hiện dần dần */
.logo-3d-scene {
  position: relative;
  width: 250px;
  height: 190px;
  display: flex;
  align-items: center;
  justify-content: center;
  transform-style: preserve-3d;
  will-change: transform, opacity, filter;
  /* Trạng thái khởi điểm: Hoàn toàn trong suốt (opacity: 0), nằm sâu bên dưới và hơi mờ */
  opacity: 0;
  transform: rotateX(60deg) rotateY(-20deg) rotateZ(24deg) scale(0.55) translateY(85px) translateZ(-40px);
  filter: blur(10px) brightness(0.5);
  transition: transform 0.9s cubic-bezier(0.16, 1, 0.3, 1),
              opacity 0.9s cubic-bezier(0.16, 1, 0.3, 1),
              filter 0.9s cubic-bezier(0.16, 1, 0.3, 1);
}

/* Giai đoạn 1: Hiện lên dần dần (Fade in + trồi lên vị trí nghiêng 3D) */
.logo-3d-scene.stage-emerge {
  opacity: 1;
  transform: rotateX(50deg) rotateY(-15deg) rotateZ(18deg) scale(0.8) translateY(35px) translateZ(0);
  filter: blur(0px) brightness(1);
}

/* Giai đoạn 2: Lượn đứng thẳng lên trung tâm */
.logo-3d-scene.stage-straighten,
.logo-3d-scene.stage-shine {
  opacity: 1;
  transform: rotateX(0deg) rotateY(0deg) rotateZ(0deg) scale(1) translateY(0) translateZ(0);
  filter: blur(0px) brightness(1);
  transition: transform 0.95s cubic-bezier(0.2, 0.8, 0.2, 1);
}

/* Hào quang viền xanh ngọc */
.emerald-neon-aura {
  position: absolute;
  inset: -10px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(16, 185, 129, 0.35) 0%, rgba(0, 240, 118, 0.1) 60%, transparent 80%);
  pointer-events: none;
  transform: translateZ(-10px);
}

/* Lớp vát tạo khối 3D đúc nổi */
.extrusion-bevel {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  pointer-events: none;
  transform: translateZ(-8px) translateY(5px) translateX(3px);
  opacity: 0.75;
}

.bevel-img {
  filter: drop-shadow(0 0 6px rgba(16, 185, 129, 0.8)) drop-shadow(8px 12px 14px rgba(0, 0, 0, 0.18));
}

/* Mặt trước logo chính */
.logo-front-face {
  position: relative;
  z-index: 5;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  transform: translateZ(2px);
  overflow: hidden;
  border-radius: 16px;
}

.emblem-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  backface-visibility: hidden;
}

.main-img {
  filter: drop-shadow(0 4px 8px rgba(16, 185, 129, 0.35));
}

/* Vệt sáng quét phản quang bóng loáng */
.specular-glint-beam {
  position: absolute;
  top: -50%;
  left: 0;
  width: 50%;
  height: 200%;
  background: linear-gradient(
    90deg,
    transparent 0%,
    rgba(255, 255, 255, 0.2) 25%,
    rgba(255, 255, 255, 0.9) 50%,
    rgba(52, 211, 153, 0.6) 70%,
    transparent 100%
  );
  transform: rotate(30deg) translateX(-250%);
  pointer-events: none;
  will-change: transform, opacity;
  opacity: 0;
}

.specular-glint-beam.run-shine {
  animation: shineSwipeGpu 1.1s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

@keyframes shineSwipeGpu {
  0% {
    opacity: 0;
    transform: rotate(30deg) translateX(-250%);
  }
  20% {
    opacity: 1;
  }
  80% {
    opacity: 1;
  }
  100% {
    opacity: 0;
    transform: rotate(30deg) translateX(350%);
  }
}

/* Khối chữ thương hiệu ZoneMart xuất hiện thanh lịch */
.brand-reveal-block {
  margin-top: 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  opacity: 0;
  transform: translateY(16px);
  will-change: transform, opacity;
  transition: transform 0.75s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.75s ease;
  pointer-events: none;
}

.brand-reveal-block.show-brand {
  opacity: 1;
  transform: translateY(0);
}

.brand-name {
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, sans-serif;
  font-size: 2.35rem;
  font-weight: 800;
  letter-spacing: 0.12em;
  margin: 0 0 6px 0;
  line-height: 1.1;
}

.text-navy {
  color: #1e293b;
}

.text-emerald {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.brand-tagline-wrap {
  display: flex;
  align-items: center;
  gap: 10px;
}

.tagline-line {
  width: 28px;
  height: 1.5px;
  background: rgba(16, 185, 129, 0.5);
}

.brand-tagline {
  color: #64748b;
  font-size: 0.8rem;
  font-weight: 600;
  letter-spacing: 0.25em;
  margin: 0;
  text-transform: uppercase;
}

/* Mobile Responsive */
@media (max-width: 640px) {
  .logo-3d-scene {
    width: 190px;
    height: 150px;
  }
  .brand-name {
    font-size: 1.8rem;
    letter-spacing: 0.08em;
  }
  .brand-tagline {
    font-size: 0.65rem;
    letter-spacing: 0.16em;
  }
  .intro-skip-pill {
    top: 16px;
    right: 16px;
    padding: 6px 14px;
    font-size: 0.8rem;
  }
}
</style>
