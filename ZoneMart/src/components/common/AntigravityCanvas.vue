<script setup lang="ts">
/**
 * ===================================================================
 * AntigravityCanvas.vue - Hiệu ứng Hạt Chống Trọng Lực (Antigravity Floating)
 * Dựa trên thuật toán vật lý từ repo davidpelayo/antigravity-animation
 * Tối ưu hóa GPU Canvas 2D + HiDPI + Tương tác đẩy hạt theo chuột / cảm ứng
 * ===================================================================
 */
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';

interface Props {
  particleCount?: number;
  speed?: number;
  gravity?: number; // Âm là bay lên (antigravity), dương là rơi xuống
  colors?: string[];
  shape?: 'mixed' | 'circle' | 'square' | 'triangle';
  interactive?: boolean;
  interactionRadius?: number;
  opacity?: number;
  fullScreen?: boolean;
  blurEffect?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  particleCount: 45,
  speed: 0.8,
  gravity: -0.05,
  colors: () => [
    '#EA580C', // Cam đậm ZoneMart
    '#F97316', // Cam tươi
    '#10B981', // Xanh lá tươi
    '#059669', // Xanh lá đậm
    '#3B82F6', // Xanh công nghệ
    '#F59E0B', // Vàng cam
    '#FED7AA', // Cam pastel
    '#D1FAE5', // Xanh pastel
  ],
  shape: 'mixed',
  interactive: true,
  interactionRadius: 150,
  opacity: 0.85,
  fullScreen: true,
  blurEffect: false,
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
  type: number = 0; // 0: Circle, 1: Square, 2: Triangle
  depth: number = 1;

  constructor(w: number, h: number, randomY = true) {
    this.init(w, h, randomY);
  }

  init(w: number, h: number, randomY = false) {
    this.x = Math.random() * (w || 100);
    this.y = randomY ? Math.random() * (h || 100) : (props.gravity < 0 ? h + 40 : -40);
    this.size = Math.random() * 14 + 6;

    this.vx = (Math.random() - 0.5) * 1.8 * props.speed;
    this.vy = (Math.random() - 0.5) * 1.8 * props.speed;

    if (props.gravity < 0) {
      this.vy -= Math.random() * 0.8 * Math.abs(props.gravity);
    } else {
      this.vy += Math.random() * 0.8 * Math.abs(props.gravity);
    }

    const palette = props.colors.length > 0 ? props.colors : ['#EA580C', '#10B981', '#3B82F6'];
    this.color = palette[Math.floor(Math.random() * palette.length)];
    this.rotation = Math.random() * Math.PI * 2;
    this.rotationSpeed = (Math.random() - 0.5) * 0.04;

    if (props.shape === 'circle') this.type = 0;
    else if (props.shape === 'square') this.type = 1;
    else if (props.shape === 'triangle') this.type = 2;
    else this.type = Math.floor(Math.random() * 3);

    this.depth = Math.random() * 0.9 + 0.6; // 0.6 -> 1.5 tạo chiều sâu 3D
  }

  update(mouseX: number, mouseY: number, w: number, h: number) {
    // Gia tốc trọng lực / chống trọng lực
    this.vy += props.gravity * 0.05 * this.depth;

    // Di chuyển theo vận tốc và độ sâu parallax
    this.x += this.vx * this.depth;
    this.y += this.vy * this.depth;
    this.rotation += this.rotationSpeed;

    // Tương tác chuột (Đẩy hạt né xa)
    if (props.interactive && mouseX > -500 && mouseY > -500) {
      const dx = this.x - mouseX;
      const dy = this.y - mouseY;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist < props.interactionRadius) {
        const force = (props.interactionRadius - dist) / props.interactionRadius;
        const angle = Math.atan2(dy, dx);
        const push = force * 3.8;
        this.vx += Math.cos(angle) * push;
        this.vy += Math.sin(angle) * push;
      }
    }

    // Ma sát không khí
    this.vx *= 0.97;
    this.vy *= 0.97;

    // Cuộn vòng lại khi ra khỏi màn hình
    if (this.x < -40) this.x = w + 40;
    if (this.x > w + 40) this.x = -40;

    if (props.gravity < 0) {
      if (this.y < -50) this.init(w, h, false);
    } else {
      if (this.y > h + 50) this.init(w, h, false);
    }
  }

  draw(ctx: CanvasRenderingContext2D) {
    ctx.save();
    ctx.translate(this.x, this.y);
    ctx.rotate(this.rotation);
    ctx.fillStyle = this.color;
    ctx.globalAlpha = props.opacity * (this.depth * 0.7);

    // Đổ bóng nhẹ tạo cảm giác trôi nổi
    ctx.shadowColor = 'rgba(0, 0, 0, 0.08)';
    ctx.shadowBlur = 8 * this.depth;
    ctx.shadowOffsetX = 3 * this.depth;
    ctx.shadowOffsetY = 4 * this.depth;

    const s = this.size * this.depth;

    ctx.beginPath();
    if (this.type === 0) {
      // Hình tròn mượt mà
      ctx.arc(0, 0, s / 2, 0, Math.PI * 2);
    } else if (this.type === 1) {
      // Hình vuông bo nhẹ
      const r = Math.min(3, s * 0.18);
      ctx.roundRect ? ctx.roundRect(-s / 2, -s / 2, s, s, r) : ctx.rect(-s / 2, -s / 2, s, s);
    } else {
      // Hình tam giác đều
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

  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  width = rect.width;
  height = rect.height;

  canvas.width = Math.round(width * dpr);
  canvas.height = Math.round(height * dpr);
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;

  const ctx = canvas.getContext('2d');
  if (ctx) {
    ctx.scale(dpr, dpr);
  }
};

const handleMouseMove = (e: MouseEvent) => {
  if (!canvasRef.value) return;
  const rect = canvasRef.value.getBoundingClientRect();
  mouseX = e.clientX - rect.left;
  mouseY = e.clientY - rect.top;
};

const handleMouseLeave = () => {
  mouseX = -1000;
  mouseY = -1000;
};

const handleTouchMove = (e: TouchEvent) => {
  if (!canvasRef.value || e.touches.length === 0) return;
  const rect = canvasRef.value.getBoundingClientRect();
  mouseX = e.touches[0].clientX - rect.left;
  mouseY = e.touches[0].clientY - rect.top;
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

  if (props.fullScreen) {
    window.addEventListener('resize', updateSize);
    window.addEventListener('mousemove', handleMouseMove);
    window.addEventListener('touchmove', handleTouchMove, { passive: true });
    window.addEventListener('mouseleave', handleMouseLeave);
  } else if (containerRef.value) {
    resizeObserver = new ResizeObserver(() => {
      updateSize();
    });
    resizeObserver.observe(containerRef.value);

    containerRef.value.addEventListener('mousemove', handleMouseMove);
    containerRef.value.addEventListener('touchmove', handleTouchMove, { passive: true });
    containerRef.value.addEventListener('mouseleave', handleMouseLeave);
  }

  animationFrameId = requestAnimationFrame(loop);
});

onBeforeUnmount(() => {
  if (animationFrameId) cancelAnimationFrame(animationFrameId);
  if (resizeObserver) resizeObserver.disconnect();

  window.removeEventListener('resize', updateSize);
  window.removeEventListener('mousemove', handleMouseMove);
  window.removeEventListener('touchmove', handleTouchMove);
  window.removeEventListener('mouseleave', handleMouseLeave);

  if (containerRef.value) {
    containerRef.value.removeEventListener('mousemove', handleMouseMove);
    containerRef.value.removeEventListener('touchmove', handleTouchMove);
    containerRef.value.removeEventListener('mouseleave', handleMouseLeave);
  }
});

watch(
  () => props.particleCount,
  () => {
    initParticles();
  }
);
</script>

<template>
  <div
    ref="containerRef"
    class="antigravity-container"
    :class="{ 'is-fullscreen': fullScreen, 'has-blur': blurEffect }"
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
  z-index: 0;
}

.antigravity-container:not(.is-fullscreen) {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  z-index: 1;
}

.antigravity-canvas {
  display: block;
  pointer-events: auto; /* Cho phép nhận chuột đẩy hạt */
}

.antigravity-container.has-blur .antigravity-canvas {
  filter: blur(1px);
}
</style>
