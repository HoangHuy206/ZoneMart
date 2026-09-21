<script setup lang="ts">
import { useToast } from '../../composables/useToast';

const { toasts, remove } = useToast();
</script>

<template>
  <div class="zonemart-toast-container" aria-live="polite" aria-atomic="true">
    <TransitionGroup name="toast-anim" tag="div" class="toast-stack-wrapper">
      <div
        v-for="item in toasts"
        :key="item.id"
        :class="['zm-toast-card', `zm-toast-${item.type}`]"
        role="alert"
      >
        <!-- Icon hoạt họa sinh động -->
        <div class="zm-toast-icon-box">
          <!-- Success checkmark SVG animated -->
          <svg
            v-if="item.type === 'success'"
            class="zm-svg-icon icon-success-pulse"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" class="check-circle"></path>
            <polyline points="22 4 12 14.01 9 11.01" class="check-tick"></polyline>
          </svg>

          <!-- Error icon SVG animated -->
          <svg
            v-else-if="item.type === 'error'"
            class="zm-svg-icon icon-error-shake"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="15" y1="9" x2="9" y2="15"></line>
            <line x1="9" y1="9" x2="15" y2="15"></line>
          </svg>

          <!-- Warning icon SVG -->
          <svg
            v-else-if="item.type === 'warning'"
            class="zm-svg-icon icon-warning-glow"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
            <line x1="12" y1="9" x2="12" y2="13"></line>
            <line x1="12" y1="17" x2="12.01" y2="17"></line>
          </svg>

          <!-- Info icon SVG -->
          <svg
            v-else
            class="zm-svg-icon icon-info-rotate"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="16" x2="12" y2="12"></line>
            <line x1="12" y1="8" x2="12.01" y2="8"></line>
          </svg>
        </div>

        <!-- Nội dung thông báo -->
        <div class="zm-toast-body">
          <div v-if="item.title" class="zm-toast-title">{{ item.title }}</div>
          <div class="zm-toast-message">{{ item.message }}</div>
        </div>

        <!-- Nút đóng -->
        <button
          type="button"
          class="zm-toast-close"
          @click="remove(item.id)"
          aria-label="Đóng thông báo"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>

        <!-- Thanh tiến trình đếm ngược thời gian -->
        <div
          v-if="item.duration && item.duration > 0"
          class="zm-toast-progress"
          :style="{ animationDuration: `${item.duration}ms` }"
        ></div>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.zonemart-toast-container {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 999999;
  pointer-events: none;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  max-width: 420px;
  width: calc(100vw - 32px);
}

.toast-stack-wrapper {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
}

.zm-toast-card {
  pointer-events: auto;
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 18px 16px 16px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  box-shadow: 0 12px 36px -4px rgba(15, 23, 42, 0.15),
              0 4px 12px -2px rgba(15, 23, 42, 0.08),
              0 0 0 1px rgba(226, 232, 240, 0.9);
  transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
  will-change: transform, opacity;
}

.zm-toast-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 16px 42px -4px rgba(15, 23, 42, 0.22),
              0 0 0 1px rgba(203, 213, 225, 1);
}

/* Biến thể màu sắc & Glow */
.zm-toast-success {
  border-left: 4px solid #10b981;
  box-shadow: 0 12px 36px -4px rgba(16, 185, 129, 0.18),
              0 4px 12px -2px rgba(15, 23, 42, 0.08),
              0 0 0 1px rgba(16, 185, 129, 0.2);
}
.zm-toast-success .zm-toast-icon-box {
  background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
  color: #059669;
}
.zm-toast-success .zm-toast-progress {
  background: linear-gradient(90deg, #10b981, #059669);
}

.zm-toast-error {
  border-left: 4px solid #ef4444;
  box-shadow: 0 12px 36px -4px rgba(239, 68, 68, 0.18),
              0 4px 12px -2px rgba(15, 23, 42, 0.08),
              0 0 0 1px rgba(239, 68, 68, 0.2);
}
.zm-toast-error .zm-toast-icon-box {
  background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
  color: #dc2626;
}
.zm-toast-error .zm-toast-progress {
  background: linear-gradient(90deg, #ef4444, #b91c1c);
}

.zm-toast-warning {
  border-left: 4px solid #f59e0b;
  box-shadow: 0 12px 36px -4px rgba(245, 158, 11, 0.18),
              0 4px 12px -2px rgba(15, 23, 42, 0.08),
              0 0 0 1px rgba(245, 158, 11, 0.2);
}
.zm-toast-warning .zm-toast-icon-box {
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
  color: #d97706;
}
.zm-toast-warning .zm-toast-progress {
  background: linear-gradient(90deg, #f59e0b, #d97706);
}

.zm-toast-info {
  border-left: 4px solid #ea580c;
  box-shadow: 0 12px 36px -4px rgba(234, 88, 12, 0.18),
              0 4px 12px -2px rgba(15, 23, 42, 0.08),
              0 0 0 1px rgba(234, 88, 12, 0.2);
}
.zm-toast-info .zm-toast-icon-box {
  background: linear-gradient(135deg, #ffedd5 0%, #fed7aa 100%);
  color: #ea580c;
}
.zm-toast-info .zm-toast-progress {
  background: linear-gradient(90deg, #ea580c, #f97316);
}

/* Hộp icon */
.zm-toast-icon-box {
  width: 40px;
  height: 40px;
  min-width: 40px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.zm-svg-icon {
  width: 22px;
  height: 22px;
}

/* Hoạt họa cho từng icon */
.icon-success-pulse .check-tick {
  stroke-dasharray: 20;
  stroke-dashoffset: 20;
  animation: strokeTick 0.5s cubic-bezier(0.65, 0, 0.45, 1) forwards 0.15s;
}
.icon-success-pulse .check-circle {
  stroke-dasharray: 60;
  stroke-dashoffset: 60;
  animation: strokeCircle 0.45s ease-out forwards;
}

@keyframes strokeCircle {
  to {
    stroke-dashoffset: 0;
  }
}
@keyframes strokeTick {
  to {
    stroke-dashoffset: 0;
  }
}

.icon-error-shake {
  animation: shakeAnim 0.45s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
}
@keyframes shakeAnim {
  10%, 90% { transform: translate3d(-1px, 0, 0); }
  20%, 80% { transform: translate3d(2px, 0, 0); }
  30%, 50%, 70% { transform: translate3d(-3px, 0, 0); }
  40%, 60% { transform: translate3d(3px, 0, 0); }
}

.icon-warning-glow {
  animation: pulseGlow 1.5s infinite alternate ease-in-out;
}
@keyframes pulseGlow {
  0% { transform: scale(0.95); opacity: 0.85; }
  100% { transform: scale(1.06); opacity: 1; }
}

.icon-info-rotate {
  animation: popBounce 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
}
@keyframes popBounce {
  0% { transform: scale(0.6) rotate(-15deg); }
  100% { transform: scale(1) rotate(0); }
}

/* Thân chữ */
.zm-toast-body {
  flex: 1;
  min-width: 0;
}

.zm-toast-title {
  font-size: 14.5px;
  font-weight: 700;
  color: #0f172a;
  margin-bottom: 2px;
  line-height: 1.3;
}

.zm-toast-message {
  font-size: 13.5px;
  font-weight: 500;
  color: #334155;
  line-height: 1.45;
  word-break: break-word;
}

/* Nút tắt */
.zm-toast-close {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 6px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  flex-shrink: 0;
}

.zm-toast-close:hover {
  color: #0f172a;
  background: rgba(148, 163, 184, 0.18);
  transform: rotate(90deg);
}

/* Thanh đếm ngược */
.zm-toast-progress {
  position: absolute;
  bottom: 0;
  left: 0;
  height: 3.5px;
  width: 100%;
  animation-name: progressCountdown;
  animation-timing-function: linear;
  animation-fill-mode: forwards;
}

@keyframes progressCountdown {
  from {
    width: 100%;
  }
  to {
    width: 0%;
  }
}

/* Vue TransitionGroup Styles */
.toast-anim-enter-active {
  animation: toastSlideIn 0.35s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.toast-anim-leave-active {
  animation: toastSlideOut 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  position: absolute;
  width: 100%;
}
.toast-anim-move {
  transition: transform 0.3s ease;
}

@keyframes toastSlideIn {
  from {
    opacity: 0;
    transform: translateX(100px) scale(0.85);
  }
  to {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
}

@keyframes toastSlideOut {
  from {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
  to {
    opacity: 0;
    transform: translateX(60px) scale(0.9);
  }
}

@media (max-width: 640px) {
  .zonemart-toast-container {
    top: 14px;
    right: 16px;
    left: 16px;
    width: auto;
    max-width: none;
    align-items: stretch;
  }
}
</style>
