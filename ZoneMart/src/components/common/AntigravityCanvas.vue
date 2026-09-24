<script setup lang="ts">
/**
 * ===================================================================
 * AntigravityCanvas.vue - Hiệu ứng Hạt Chống Trọng Lực (Antigravity Animation)
 * Port chuẩn 100% từ repo https://github.com/davidpelayo/antigravity-animation
 * - Khối hình học: Tròn, Vuông bo góc, Tam giác đều (Google Design)
 * - Tương tác né chuột nhạy bén (Repel Force theo con trỏ chuột)
 * - Trôi lơ lửng chống trọng lực (Upward Antigravity Float)
 * - 60/120fps siêu mượt, retina canvas display
 * - pointer-events: none 100% không ảnh hưởng đến bất kỳ thao tác click nào
 * ===================================================================
 */
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';

interface Props {
  particleCount?: number;
  speed?: number;
  gravity?: number; // Âm: bay lên trời (antigravity), Dương: rơi xuống
  colors?: string[];
  shape?: 'mixed' | 'circle' | 'square' | 'triangle';
  interactive?: boolean;
  interactionRadius?: number;
  opacity?: number;
  fullScreen?: boolean;
  zIndex?: number;
}

const props = withDefaults(defineProps<Props>(), {
  particleCount: 50,
  speed: 0.9,
  gravity: -0.05,
  colors: () => [
    '#4285F4', // Google Blue
    '#EA4335', // Google Red
    '#FBBC05', // Google Yellow
    '#34A853', // Google Green
    '#38BDF8', // Sky Blue
    '#818CF8', // Indigo
    '#A78BFA', // Purple
  ],
  shape: 'mixed',
  interactive: true,
  interactionRadius: 180,
  opacity: 0.85,
  fullScreen: true,
  zIndex: 1,
});

const canvasRef = ref<HTMLCanvasElement | null>(null);
const containerRef = ref<HTMLDivElement | null>(null);

let animationFrameId: number | null = null;
let resizeObserver: ResizeObserver | null = null;

class Particle {
  x: number = 0;
  y: number = 0;
  size: number = 0;
  vx: number = 0;
  vy: number = 0;
  color: string = '';
  rotation: number = 0;
  rotationSpeed: number = 0;
  type: number = 0; // 0 = Circle, 1 = Square, 2 = Triangle
  depth: number = 1;

  constructor(w: number, h: number, randomY = true) {
    this.init(w, h, randomY);
  }

  init(w: number, h: number, randomY = false) {
    this.x = Math.random() * (w || window.innerWidth || 800);
    this.y = randomY
      ? Math.random() * (h || window.innerHeight || 600)
      : props.gravity < 0
      ? (h || window.innerHeight || 600) + 40
      : -40;

    // Kích thước chuẩn rõ ràng: 14px -> 26px
    this.size = Math.random() * 12 + 14;

    // Vận tốc ngẫu nhiên
    this.vx = (Math.random() - 0.5) * 2 * props.speed;
    this.vy = (Math.random() - 0.5) * 2 * props.speed;

    // Lực đẩy antigravity hướng lên trên
    if (props.gravity < 0) {
      this.vy -= (Math.random() * 1.2 + 0.5) * Math.abs(props.gravity) * 20;
    } else {
      this.vy += (Math.random() * 1.2 + 0.5) * Math.abs(props.gravity) * 20;
    }

    const palette = props.colors && props.colors.length > 0
      ? props.colors
      : ['#4285F4', '#EA4335', '#FBBC05', '#34A853'];
    this.color = palette[Math.floor(Math.random() * palette.length)];
    this.rotation = Math.random() * Math.PI * 2;
    this.rotationSpeed = (Math.random() - 0.5) * 0.04;

    if (props.shape === 'circle') this.type = 0;
    else if (props.shape === 'square') this.type = 1;
    else if (props.shape === 'triangle') this.type = 2;
    else this.type = Math.floor(Math.random() * 3);

    // Chiều sâu không gian 3D parallax
    this.depth = Math.random() * 0.6 + 0.7; // 0.7 -> 1.3
  }

  update(mouseX: number, mouseY: number, w: number, h: number) {
    // Gia tốc trọng lực / chống trọng lực
    this.vy += props.gravity * 0.1 * this.depth;

    // Chuyển động
    this.x += this.vx * this.depth;
    this.y += this.vy * this.depth;
    this.rotation += this.rotationSpeed;

    // Tương tác né chuột nhạy bén (Repel Force)
    if (props.interactive && mouseX > -500 && mouseY > -500) {
      const dx = this.x - mouseX;
      const dy = this.y - mouseY;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist < props.interactionRadius && dist > 0) {
        const force = (props.interactionRadius - dist) / props.interactionRadius;
        const angle = Math.atan2(dy, dx);
        const push = force * 6.0; // Lực đẩy né chuột mạnh mẽ
        this.vx += Math.cos(angle) * push;
        this.vy += Math.sin(angle) * push;
      }
    }

    // Ma sát không khí
    this.vx *= 0.96;
    this.vy *= 0.96;

    // Bọc quanh cạnh màn hình
    if (this.x < -60) this.x = w + 60;
    if (this.x > w + 60) this.x = -60;

    // Tái xuất hiện khi bay hết chiều dọc
    if (props.gravity < 0) {
      if (this.y < -70) this.init(w, h, false);
    } else {
      if (this.y > h + 70) this.init(w, h, false);
    }
  }

  draw(ctx: CanvasRenderingContext2D) {
    ctx.save();
    ctx.translate(this.x, this.y);
    ctx.rotate(this.rotation);
    ctx.fillStyle = this.color;
    ctx.globalAlpha = props.opacity;

    // Đổ bóng mô phỏng chiều sâu và phát sáng nhẹ (Glow Effect)
    ctx.shadowColor = this.color;
    ctx.shadowBlur = 12 * this.depth;
    ctx.shadowOffsetX = 0;
    ctx.shadowOffsetY = 2;

    const s = this.size * this.depth;

    ctx.beginPath();
    if (this.type === 0) {
      // Hình tròn mượt mà (Google Circle)
      ctx.arc(0, 0, s / 2, 0, Math.PI * 2);
    } else if (this.type === 1) {
      // Hình vuông bo góc tinh tế (Google Rounded Square)
      const r = Math.min(6, s * 0.25);
      if (ctx.roundRect) {
        ctx.roundRect(-s / 2, -s / 2, s, s, r);
      } else {
        ctx.rect(-s / 2, -s / 2, s, s);
      }
    } else {
      // Hình tam giác đều (Google Triangle)
      const h = (s * Math.sqrt(3)) / 2;
      ctx.moveTo(0, -h / 2);
      ctx.lineTo(s / 2, h / 2);
      ctx.lineTo(-s / 2, h / 2);
      ctx.closePath();
    }

    ctx.fill();
    ctx.restore();
  }
}

let particles: Particle[] = [];
let width = 0;
let height = 0;
let mouseX = -1000;
let mouseY = -1000;

const updateSize = () => {
  if (!canvasRef.value) return;
  const canvas = canvasRef.value;
  const rect = props.fullScreen
    ? { width: window.innerWidth, height: window.innerHeight }
    : containerRef.value
    ? containerRef.value.getBoundingClientRect()
    : { width: window.innerWidth, height: window.innerHeight };

  width = Math.max(10, Math.round(rect.width));
  height = Math.max(10, Math.round(rect.height));

  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  canvas.width = width * dpr;
  canvas.height = height * dpr;
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;

  const ctx = canvas.getContext('2d');
  if (ctx) {
    ctx.scale(dpr, dpr);
  }
};

const handleGlobalMouseMove = (e: MouseEvent) => {
  if (!canvasRef.value) return;
  const rect = canvasRef.value.getBoundingClientRect();
  mouseX = e.clientX - rect.left;
  mouseY = e.clientY - rect.top;
};

const handleGlobalTouchMove = (e: TouchEvent) => {
  if (!canvasRef.value || e.touches.length === 0) return;
  const rect = canvasRef.value.getBoundingClientRect();
  mouseX = e.touches[0].clientX - rect.left;
  mouseY = e.touches[0].clientY - rect.top;
};

const handleGlobalMouseLeave = () => {
  mouseX = -1000;
  mouseY = -1000;
};

const loop = () => {
  if (!canvasRef.value) return;
  const ctx = canvasRef.value.getContext('2d');
  if (ctx && width > 0 && height > 0) {
    ctx.clearRect(0, 0, width, height);

    for (let i = 0; i < particles.length; i++) {
      particles[i].update(mouseX, mouseY, width, height);
      particles[i].draw(ctx);
    }
  }
  animationFrameId = requestAnimationFrame(loop);
};

const initParticles = () => {
  particles = [];
  const count = props.particleCount || 50;
  for (let i = 0; i < count; i++) {
    particles.push(new Particle(width, height, true));
  }
};

onMounted(() => {
  updateSize();
  initParticles();

  window.addEventListener('resize', updateSize);
  window.addEventListener('mousemove', handleGlobalMouseMove);
  window.addEventListener('touchmove', handleGlobalTouchMove, { passive: true });
  document.addEventListener('mouseleave', handleGlobalMouseLeave);

  if (!props.fullScreen && containerRef.value) {
    resizeObserver = new ResizeObserver(() => {
      updateSize();
    });
    resizeObserver.observe(containerRef.value);
  }

  animationFrameId = requestAnimationFrame(loop);
});

onBeforeUnmount(() => {
  if (animationFrameId) cancelAnimationFrame(animationFrameId);
  if (resizeObserver) resizeObserver.disconnect();

  window.removeEventListener('resize', updateSize);
  window.removeEventListener('mousemove', handleGlobalMouseMove);
  window.removeEventListener('touchmove', handleGlobalTouchMove);
  document.removeEventListener('mouseleave', handleGlobalMouseLeave);
});

watch(
  () => [props.particleCount, props.colors, props.shape],
  () => {
    initParticles();
  },
  { deep: true }
);
</script>

<template>
  <div
    ref="containerRef"
    class="antigravity-container"
    :class="{ 'is-fullscreen': fullScreen }"
    :style="{ zIndex: zIndex }"
  >
    <canvas ref="canvasRef" class="antigravity-canvas"></canvas>
  </div>
</template>

<style scoped>
.antigravity-container {
  pointer-events: none !important;
  overflow: hidden;
  user-select: none;
}

.antigravity-container.is-fullscreen {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100vw;
  height: 100vh;
  pointer-events: none !important;
}

.antigravity-container:not(.is-fullscreen) {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100%;
  height: 100%;
  pointer-events: none !important;
}

.antigravity-canvas {
  display: block;
  width: 100%;
  height: 100%;
  pointer-events: none !important;
  will-change: transform;
}
</style>
