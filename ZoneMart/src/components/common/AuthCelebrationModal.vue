<script setup lang="ts">
import { ref, onUnmounted, watch } from 'vue';

const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    mode?: 'login' | 'register' | 'success';
    userName?: string;
    roleName?: string;
    title?: string;
    message?: string;
    countdownMs?: number;
  }>(),
  {
    mode: 'login',
    userName: '',
    roleName: '',
    title: '',
    message: '',
    countdownMs: 1800,
  }
);

const emit = defineEmits<{
  (e: 'update:modelValue', val: boolean): void;
  (e: 'complete'): void;
}>();

const remainingProgress = ref(100);
let progressTimer: any = null;
let completeTimer: any = null;

const startCountdown = () => {
  remainingProgress.value = 100;
  const startTime = Date.now();
  const duration = props.countdownMs || 1800;

  if (progressTimer) clearInterval(progressTimer);
  if (completeTimer) clearTimeout(completeTimer);

  progressTimer = setInterval(() => {
    const elapsed = Date.now() - startTime;
    const progress = Math.max(0, 100 - (elapsed / duration) * 100);
    remainingProgress.value = progress;
    if (progress <= 0) {
      clearInterval(progressTimer);
    }
  }, 16);

  completeTimer = setTimeout(() => {
    emit('complete');
  }, duration);
};

watch(
  () => props.modelValue,
  (val) => {
    if (val) {
      startCountdown();
    } else {
      if (progressTimer) clearInterval(progressTimer);
      if (completeTimer) clearTimeout(completeTimer);
    }
  },
  { immediate: true }
);

onUnmounted(() => {
  if (progressTimer) clearInterval(progressTimer);
  if (completeTimer) clearTimeout(completeTimer);
});

const handleSkip = () => {
  if (progressTimer) clearInterval(progressTimer);
  if (completeTimer) clearTimeout(completeTimer);
  emit('complete');
};
</script>

<template>
  <Transition name="celebrate-backdrop">
    <div v-if="modelValue" class="zm-celebrate-backdrop">
      <!-- Lớp hạt phát sáng / pháo hoa ăn mừng -->
      <div class="zm-confetti-container" aria-hidden="true">
        <span
          v-for="i in 18"
          :key="i"
          :class="['confetti-dot', `dot-${i}`]"
        ></span>
      </div>

      <!-- Card hiển thị chính giữa -->
      <div class="zm-celebrate-card">
        <!-- Vòng hào quang sáng phía sau icon -->
        <div class="zm-icon-glow-ring"></div>

        <!-- Biểu tượng checkmark hoạt họa Apple-style -->
        <div class="zm-animated-badge">
          <svg
            class="zm-svg-check"
            viewBox="0 0 52 52"
            fill="none"
            stroke="currentColor"
          >
            <circle
              class="zm-svg-circle"
              cx="26"
              cy="26"
              r="24"
              stroke-width="3"
            />
            <path
              class="zm-svg-tick"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="4"
              d="M14.1 27.2l7.1 7.2 16.7-16.8"
            />
          </svg>
        </div>

        <!-- Tiêu đề lớn -->
        <h2 class="zm-celebrate-title">
          {{
            title ||
            (mode === 'login'
              ? 'ĐĂNG NHẬP THÀNH CÔNG!'
              : mode === 'register'
              ? 'TẠO TÀI KHOẢN THÀNH CÔNG!'
              : 'HOÀN TẤT THÀNH CÔNG!')
          }}
        </h2>

        <!-- Tên người dùng & Huy hiệu vai trò -->
        <div v-if="userName || roleName" class="zm-user-info-pill">
          <span v-if="userName" class="zm-user-name">
            <i class="bi bi-person-fill text-orange me-1"></i>
            {{ userName }}
          </span>
          <span v-if="roleName" class="zm-role-badge">
            {{ roleName }}
          </span>
        </div>

        <!-- Lời nhắn phụ -->
        <p class="zm-celebrate-desc">
          {{
            message ||
            (mode === 'login'
              ? 'Chào mừng bạn trở lại với hệ sinh thái ZoneMart! Đang tải dữ liệu...'
              : 'Chào mừng bạn gia nhập ZoneMart! Vui lòng chờ trong giây lát...')
          }}
        </p>

        <!-- Thanh đếm ngược tiến trình chuyển hướng -->
        <div class="zm-redirect-progress-bar">
          <div
            class="zm-progress-fill"
            :style="{ width: `${100 - remainingProgress}%` }"
          ></div>
        </div>

        <div class="zm-countdown-hint">
          <span>Đang chuyển hướng tự động...</span>
          <button type="button" class="btn-skip-wait" @click="handleSkip">
            Vào ngay ➔
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.zm-celebrate-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1000000;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  padding: 20px;
}

.zm-celebrate-card {
  position: relative;
  width: 100%;
  max-width: 440px;
  background: #ffffff;
  border-radius: 28px;
  padding: 36px 32px 30px;
  text-align: center;
  box-shadow: 0 25px 60px -15px rgba(234, 88, 12, 0.25),
              0 15px 40px -10px rgba(15, 23, 42, 0.3),
              0 0 0 1px rgba(254, 215, 170, 0.6);
  display: flex;
  flex-direction: column;
  align-items: center;
  overflow: hidden;
  animation: cardPopIn 0.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
}

@keyframes cardPopIn {
  0% {
    opacity: 0;
    transform: scale(0.85) translateY(20px);
  }
  100% {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

/* Icon & Glow */
.zm-icon-glow-ring {
  position: absolute;
  top: 30px;
  width: 110px;
  height: 110px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(249, 115, 22, 0.3) 0%, rgba(249, 115, 22, 0) 70%);
  filter: blur(12px);
  pointer-events: none;
  animation: glowPulse 2s infinite alternate ease-in-out;
}

@keyframes glowPulse {
  0% { transform: scale(0.9); opacity: 0.6; }
  100% { transform: scale(1.3); opacity: 1; }
}

.zm-animated-badge {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  background: linear-gradient(135deg, #ea580c 0%, #f97316 100%);
  box-shadow: 0 10px 25px -4px rgba(234, 88, 12, 0.5),
              0 0 0 8px rgba(255, 237, 213, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  margin-bottom: 22px;
  z-index: 1;
  animation: badgeBounce 0.6s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
}

@keyframes badgeBounce {
  0% { transform: scale(0); }
  60% { transform: scale(1.15); }
  100% { transform: scale(1); }
}

.zm-svg-check {
  width: 48px;
  height: 48px;
}

.zm-svg-circle {
  stroke: rgba(255, 255, 255, 0.35);
  stroke-dasharray: 166;
  stroke-dashoffset: 166;
  animation: strokeCircleAnim 0.6s cubic-bezier(0.65, 0, 0.45, 1) forwards 0.1s;
}

.zm-svg-tick {
  stroke: #ffffff;
  stroke-dasharray: 48;
  stroke-dashoffset: 48;
  animation: strokeTickAnim 0.45s cubic-bezier(0.65, 0, 0.45, 1) forwards 0.35s;
}

@keyframes strokeCircleAnim {
  to {
    stroke-dashoffset: 0;
  }
}
@keyframes strokeTickAnim {
  to {
    stroke-dashoffset: 0;
  }
}

/* Tiêu đề */
.zm-celebrate-title {
  font-size: 21px;
  font-weight: 800;
  color: #0f172a;
  letter-spacing: -0.5px;
  margin: 0 0 10px;
  line-height: 1.3;
}

/* User pill */
.zm-user-info-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  padding: 6px 14px;
  border-radius: 999px;
  margin-bottom: 12px;
}

.zm-user-name {
  font-size: 13.5px;
  font-weight: 700;
  color: #1e293b;
}

.zm-role-badge {
  background: linear-gradient(135deg, #ea580c 0%, #f97316 100%);
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 6px;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

/* Mô tả */
.zm-celebrate-desc {
  font-size: 14px;
  color: #475569;
  line-height: 1.55;
  margin: 0 0 24px;
  max-width: 360px;
}

/* Thanh progress chuyển hướng */
.zm-redirect-progress-bar {
  width: 100%;
  height: 6px;
  background: #f1f5f9;
  border-radius: 999px;
  overflow: hidden;
  margin-bottom: 12px;
}

.zm-progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #ea580c, #f97316, #fb923c);
  border-radius: 999px;
  transition: width 0.05s linear;
}

.zm-countdown-hint {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12.5px;
  color: #64748b;
}

.btn-skip-wait {
  background: transparent;
  border: none;
  color: #ea580c;
  font-size: 12.5px;
  font-weight: 700;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: all 0.2s;
}

.btn-skip-wait:hover {
  background: #ffedd5;
  color: #c2410c;
}

/* Confetti particles */
.zm-confetti-container {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
}

.confetti-dot {
  position: absolute;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  opacity: 0;
  animation: confettiFall 1.8s ease-out forwards;
}

.dot-1 { left: 15%; top: 20%; background: #ea580c; animation-delay: 0.1s; }
.dot-2 { left: 25%; top: 15%; background: #10b981; animation-delay: 0.2s; }
.dot-3 { left: 35%; top: 25%; background: #3b82f6; animation-delay: 0.15s; }
.dot-4 { left: 45%; top: 18%; background: #f59e0b; animation-delay: 0.25s; }
.dot-5 { left: 55%; top: 22%; background: #ec4899; animation-delay: 0.05s; }
.dot-6 { left: 65%; top: 16%; background: #8b5cf6; animation-delay: 0.3s; }
.dot-7 { left: 75%; top: 24%; background: #10b981; animation-delay: 0.12s; }
.dot-8 { left: 85%; top: 19%; background: #ea580c; animation-delay: 0.22s; }
.dot-9 { left: 20%; top: 35%; background: #f59e0b; animation-delay: 0.18s; }
.dot-10 { left: 30%; top: 40%; background: #ec4899; animation-delay: 0.28s; }
.dot-11 { left: 70%; top: 38%; background: #3b82f6; animation-delay: 0.14s; }
.dot-12 { left: 80%; top: 42%; background: #10b981; animation-delay: 0.32s; }
.dot-13 { left: 10%; top: 50%; background: #8b5cf6; animation-delay: 0.26s; }
.dot-14 { left: 90%; top: 48%; background: #ea580c; animation-delay: 0.08s; }
.dot-15 { left: 50%; top: 10%; background: #f59e0b; animation-delay: 0.16s; }
.dot-16 { left: 40%; top: 45%; background: #10b981; animation-delay: 0.35s; }
.dot-17 { left: 60%; top: 45%; background: #ec4899; animation-delay: 0.24s; }
.dot-18 { left: 50%; top: 52%; background: #3b82f6; animation-delay: 0.19s; }

@keyframes confettiFall {
  0% {
    opacity: 1;
    transform: translateY(0) scale(1) rotate(0);
  }
  100% {
    opacity: 0;
    transform: translateY(180px) scale(0.3) rotate(360deg);
  }
}

/* Backdrop transition */
.celebrate-backdrop-enter-active {
  transition: opacity 0.3s ease;
}
.celebrate-backdrop-leave-active {
  transition: opacity 0.25s ease;
}
.celebrate-backdrop-enter-from,
.celebrate-backdrop-leave-to {
  opacity: 0;
}
</style>
