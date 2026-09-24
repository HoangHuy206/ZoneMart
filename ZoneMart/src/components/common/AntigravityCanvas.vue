<script setup lang="ts">
/**
 * ===================================================================
 * AntigravityCanvas.vue - Hiệu ứng Hạt Chống Trọng Lực (Antigravity Floating)
 * Port chuẩn 100% từ repo https://github.com/davidpelayo/antigravity-animation
 * - GPU Sprite Caching: Pre-render hình khối 64px kèm Baked Shadow
 * - Khối hình: Tròn, Vuông bo góc, Tam giác (Google Antigravity Style)
 * - Tương tác chuột toàn màn hình qua window event (né hạt cực nhạy)
 * - Tốc độ 60/120fps mượt mà
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
  speed: 0.85,
  gravity: -0.06,
  colors: () => [
    '#EA4335', // Google Red
    '#FBBC05', // Google Yellow
    '#34A853', // Google Green
    '#4285F4', // Google Blue
    '#EA580C', // ZoneMart Orange
    '#10B981', // ZoneMart Emerald
    '#F59E0B', // Amber
  ],
  shape: 'mixed',
  interactive: true,
  interactionRadius: 160,
  opacity: 0.95,
  fullScreen: true,
  zIndex: 0,
});

const canvasRef = ref<HTMLCanvasElement | null>(null);
const containerRef = ref<HTMLDivElement | null>(null);

let animationFrameId: number | null = null;
let resizeObserver: ResizeObserver | null = null;

// --- GPU SPRITE CACHE (Theo đúng chuẩn davidpelayo/antigravity-animation) ---
const spriteCache: Record<string, HTMLCanvasElement> = {};

function getSprite(color: string, shapeType: number): HTMLCanvasElement {
  const key = `${color}-${shapeType}`;
  if (spriteCache[key]) return spriteCache[key];

  const size = 64;
  const center = size / 2;
  const drawSize = 26;

  const c = document.createElement('canvas');
  c.width = size;
  c.height = size;
  const cx = c.getContext('2d');
  if (!cx) return c;

  // Baked Soft Drop Shadow
  cx.shadowColor = 'rgba(0, 0, 0, 0.15)';
  cx.shadowBlur = 10;
  cx.shadowOffsetX = 3;
  cx.shadowOffsetY = 4;
  cx.fillStyle = color;

  cx.translate(center, center);
  cx.beginPath();

  if (shapeType === 0) {
    // Circle
    cx.arc(0, 0, drawSize / 2, 0, Math.PI * 2);
  } else if (shapeType === 1) {
    // Square with rounded corners
    const r = 4;
    const s = drawSize;
    if (cx.roundRect) {
      cx.roundRect(-s / 2, -s / 2, s, s, r);
    } else {
      cx.rect(-s / 2, -s / 2, s, s);
    }
  } else {
    // Triangle
    const s = drawSize * 1.15;
    const h = (s * Math.sqrt(3)) / 2;
    cx.moveTo(0, -h / 2);
    cx.lineTo(s / 2, h / 2);
    cx.lineTo(-s / 2, h / 2);
    cx.closePath();
  }

  cx.fill();
  spriteCache[key] = c;
  return c;
}

class Particle {
  x: number = 0;
  y: number = 0;
  visualSize: number = 0;
  vx: number = 0;
  vy: number = 0;
  color: string = '';
  rotation: number = 0;
  rotationSpeed: number = 0;
  type: number = 0;
  depth: number = 1;
  sprite: HTMLCanvasElement | null = null;

  constructor(w: number, h: number, randomY = true) {
    this.init(w, h, randomY);
  }

  init(w: number, h: number, randomY = false) {
    this.x = Math.random() * (w || 100);
    this.y = randomY ? Math.random() * (h || 100) : (props.gravity < 0 ? h + 60 : -60);
    this.visualSize = Math.random() * 12 + 16; // Kích thước hạt sắc nét rõ ràng 16px -> 28px

    this.vx = (Math.random() - 0.5) * 2 * props.speed;
    this.vy = (Math.random() - 0.5) * 2 * props.speed;

    if (props.gravity < 0) {
      this.vy -= Math.random() * 1.2 * Math.abs(props.gravity);
    } else {
      this.vy += Math.random() * 1.2 * Math.abs(props.gravity);
    }

    const palette = props.colors.length > 0 ? props.colors : ['#EA4335', '#FBBC05', '#34A853', '#4285F4'];
    this.color = palette[Math.floor(Math.random() * palette.length)];
    this.rotation = Math.random() * Math.PI * 2;
    this.rotationSpeed = (Math.random() - 0.5) * 0.04;

    if (props.shape === 'circle') this.type = 0;
    else if (props.shape === 'square') this.type = 1;
    else if (props.shape === 'triangle') this.type = 2;
    else this.type = Math.floor(Math.random() * 3);

    this.sprite = getSprite(this.color, this.type);
    this.depth = Math.random() * 0.7 + 0.7; // 0.7 -> 1.4 chiều sâu 3D
  }

  update(mouseX: number, mouseY: number, w: number, h: number) {
    this.vy += props.gravity * 0.05 * this.depth;

    this.x += this.vx * this.depth;
    this.y += this.vy * this.depth;
    this.rotation += this.rotationSpeed;

    // Tương tác né chuột nhạy và mượt (Repel Force)
    if (props.interactive && mouseX > -500 && mouseY > -500) {
      const dx = this.x - mouseX;
      const dy = this.y - mouseY;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist < props.interactionRadius) {
        const force = (props.interactionRadius - dist) / props.interactionRadius;
        const angle = Math.atan2(dy, dx);
        const push = force * 5.2;
        this.vx += Math.cos(angle) * push;
        this.vy += Math.sin(angle) * push;
      }
    }

    this.vx *= 0.96;
    this.vy *= 0.96;

    if (this.x < -60) this.x = w + 60;
    if (this.x > w + 60) this.x = -60;

    if (props.gravity < 0) {
      if (this.y < -70) this.init(w, h, false);
    } else {
      if (this.y > h + 70) this.init(w, h, false);
    }
  }

  draw(ctx: CanvasRenderingContext2D) {
    if (!this.sprite) return;
    ctx.save();
    ctx.translate(this.x, this.y);
    ctx.rotate(this.rotation);
    ctx.globalAlpha = props.opacity;

    // Tỉ lệ scale chuẩn 64/26 để kích thước thực tế đúng bằng this.visualSize * depth
    const renderSize = this.visualSize * (64 / 26) * this.depth;
    ctx.drawImage(this.sprite, -renderSize / 2, -renderSize / 2, renderSize, renderSize);

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

  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  width = Math.max(10, rect.width);
  height = Math.max(10, rect.height);

  canvas.width = Math.round(width * dpr);
  canvas.height = Math.round(height * dpr);
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;

  const ctx = canvas.getContext('2d');
  if (ctx) {
    ctx.scale(dpr, dpr);
  }
};

// Global mouse tracker tính toán tọa độ tương đối chuẩn xác
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
  for (let i = 0; i < props.particleCount; i++) {
    particles.push(new Particle(width, height, true));
  }
};

onMounted(() => {
  updateSize();
  initParticles();

  // Luôn bắt mousemove trên window để chuột ở bất cứ đâu cũng đẩy hạt
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
  pointer-events: none;
  overflow: hidden;
  user-select: none;
}

.antigravity-container.is-fullscreen {
  position: fixed;
  inset: 0;
  width: 100vw;
  height: 100vh;
}

.antigravity-container:not(.is-fullscreen) {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

.antigravity-canvas {
  display: block;
  width: 100%;
  height: 100%;
  pointer-events: none;
  will-change: transform;
}
</style>
