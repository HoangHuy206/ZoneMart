<script setup lang="ts">
/**
 * ===================================================================
 * AntigravityCanvas.vue - Hiệu ứng Làn Gió Nông Sản Chợ Dân Sinh (ZoneMart Market Breeze)
 * Phù hợp 100% bản sắc Chợ Dân Sinh & Nông Sản Tươi ZoneMart:
 * - Lá cây xanh mướt (Fresh Leaf 🍃)
 * - Quả cam, quả cà chua tươi mọng (Citrus / Tomato 🍊🍎)
 * - Mầm non hữu cơ (Organic Sprout 🌱)
 * - Giọt sương sớm mai trong lành (Dewdrops 💧)
 * - Đốm nắng ban mai ấm áp (Sunlight Bokeh ✨)
 * - Chuyển động đung đưa theo làn gió xuân uốn lượn (Organic Breeze Sway)
 * - Tương tác né chuột mềm mại (Repel Force tự nhiên)
 * - Tương thích chủ đề 'market' (chợ dân sinh) và 'cyber' (modal Face ID)
 * ===================================================================
 */
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';

export type CanvasTheme = 'market' | 'cyber' | 'geometry' | 'seller' | 'shipper';

interface Props {
  theme?: CanvasTheme;
  particleCount?: number;
  speed?: number;
  gravity?: number; // Âm: bay lơ lửng lên trên
  colors?: string[];
  interactive?: boolean;
  interactionRadius?: number;
  opacity?: number;
  fullScreen?: boolean;
  zIndex?: number;
}

const props = withDefaults(defineProps<Props>(), {
  theme: 'market',
  particleCount: 42,
  speed: 0.75,
  gravity: -0.035,
  colors: () => [
    '#22C55E', // Xanh lá non tươi mát (VietGAP)
    '#16A34A', // Xanh rau hữu cơ
    '#84CC16', // Xanh mạ non
    '#10B981', // Xanh ngọc mát
    '#F97316', // Cam cà rốt / Cam ZoneMart
    '#EA580C', // Cam đất thương hiệu
    '#EF4444', // Đỏ cà chua chín
    '#F59E0B', // Vàng ngô / Mật ong
    '#38BDF8', // Xanh giọt sương mai
    '#FBBF24', // Vàng nắng ban mai
  ],
  interactive: true,
  interactionRadius: 180,
  opacity: 0.82,
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
  type: number = 0; // Tùy theo theme
  depth: number = 1;
  swayAngle: number = 0;
  swaySpeed: number = 0;
  swayAmplitude: number = 0;

  constructor(w: number, h: number, randomY = true) {
    this.init(w, h, randomY);
  }

  init(w: number, h: number, randomY = false) {
    const screenW = w || window.innerWidth || 800;
    const screenH = h || window.innerHeight || 600;

    this.x = Math.random() * screenW;
    this.y = randomY
      ? Math.random() * screenH
      : props.gravity < 0
      ? screenH + 40
      : -40;

    // Chiều sâu không gian
    this.depth = Math.random() * 0.55 + 0.65; // 0.65 -> 1.2

    // Kích thước chuẩn nông sản: 18px -> 34px
    this.size = (Math.random() * 16 + 18) * this.depth;

    // Vận tốc gió
    this.vx = (Math.random() - 0.5) * 1.4 * props.speed;
    this.vy = - (Math.random() * 0.8 + 0.5) * props.speed * (props.theme === 'market' ? 0.9 : 1.2);

    // Chuyển động đung đưa hình sin (Swaying with the morning breeze)
    this.swayAngle = Math.random() * Math.PI * 2;
    this.swaySpeed = (Math.random() * 0.02 + 0.015);
    this.swayAmplitude = (Math.random() * 0.9 + 0.6) * this.depth;

    // Bảng màu
    const palette = props.colors && props.colors.length > 0
      ? props.colors
      : ['#22C55E', '#16A34A', '#F97316', '#F59E0B', '#38BDF8'];
    this.color = palette[Math.floor(Math.random() * palette.length)];

    // Góc quay & tốc độ lộn nhào nhẹ của lá cây
    this.rotation = Math.random() * Math.PI * 2;
    this.rotationSpeed = (Math.random() - 0.5) * 0.025;

    // Phân loại hình thái
    if (props.theme === 'market') {
      // 0: Lá xanh, 1: Quả cam/cà chua, 2: Mầm non, 3: Giọt sương, 4: Đốm nắng mai
      this.type = Math.floor(Math.random() * 5);
      if (this.type === 1 && !this.color.match(/F97316|EA580C|EF4444|F59E0B/)) {
        this.color = Math.random() > 0.4 ? '#F97316' : '#EF4444';
      }
      if (this.type === 3) {
        this.color = '#38BDF8';
      }
    } else if (props.theme === 'seller') {
      // 0: Dâu tây đỏ, 1: Cam vàng/Táo xanh, 2: Chùm nho tím, 3: Quầy sạp cửa hàng, 4: Giỏ hoa quả
      this.type = Math.floor(Math.random() * 5);
      if (this.type === 0) this.color = '#EF4444';
      else if (this.type === 1) this.color = Math.random() > 0.5 ? '#F97316' : '#22C55E';
      else if (this.type === 2) this.color = '#8B5CF6';
      else if (this.type === 3) this.color = '#10B981';
      else if (this.type === 4) this.color = '#D97706';
    } else if (props.theme === 'shipper') {
      // 0: Xe máy giao hàng, 1: Thùng hàng carton, 2: Định vị GPS, 3: Đồng hồ tốc độ, 4: Mũ bảo hiểm tài xế
      this.type = Math.floor(Math.random() * 5);
      if (this.type === 0) this.color = '#0284C7';
      else if (this.type === 1) this.color = '#F59E0B';
      else if (this.type === 2) this.color = '#EF4444';
      else if (this.type === 3) this.color = '#EA580C';
      else if (this.type === 4) this.color = '#0EA5E9';

    } else if (props.theme === 'cyber') {
      // 0: Vòng tròn neon, 1: Khối lục giác / vuông, 2: Mũi tên quét
      this.type = Math.floor(Math.random() * 3);
    } else {
      this.type = Math.floor(Math.random() * 3);
    }
  }

  update(mouseX: number, mouseY: number, w: number, h: number) {
    // Đung đưa theo gió
    this.swayAngle += this.swaySpeed;
    const swayOffset = Math.sin(this.swayAngle) * this.swayAmplitude;

    this.x += (this.vx + swayOffset) * this.depth;
    this.y += this.vy * this.depth;
    this.rotation += this.rotationSpeed;

    // Tương tác né chuột nhạy bén (Repel Force nhẹ nhàng)
    if (props.interactive && mouseX > -500 && mouseY > -500) {
      const dx = this.x - mouseX;
      const dy = this.y - mouseY;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist < props.interactionRadius && dist > 0) {
        const force = (props.interactionRadius - dist) / props.interactionRadius;
        const angle = Math.atan2(dy, dx);
        const push = force * (props.theme === 'market' ? 4.5 : 6.0);
        this.vx += Math.cos(angle) * push;
        this.vy += Math.sin(angle) * push;
      }
    }

    // Ma sát không khí
    this.vx *= 0.95;

    // Bọc quanh cạnh màn hình
    if (this.x < -60) this.x = w + 60;
    if (this.x > w + 60) this.x = -60;

    // Tái xuất hiện khi bay lên quá trần
    if (this.y < -70) {
      this.init(w, h, false);
    }
  }

  draw(ctx: CanvasRenderingContext2D) {
    ctx.save();
    ctx.translate(this.x, this.y);
    ctx.rotate(this.rotation);
    ctx.globalAlpha = props.opacity * Math.min(1, this.depth + 0.2);

    const s = this.size;

    if (props.theme === 'market') {
      // Đổ bóng mềm mại nông sản
      ctx.shadowColor = 'rgba(0, 0, 0, 0.08)';
      ctx.shadowBlur = 8 * this.depth;
      ctx.shadowOffsetY = 3;

      if (this.type === 0) {
        // 🍃 Chiếc lá non tươi xanh
        this.drawLeaf(ctx, s);
      } else if (this.type === 1) {
        // 🍊 Quả cam / 🍎 Quả cà chua mọng nước
        this.drawFruit(ctx, s);
      } else if (this.type === 2) {
        // 🌱 Mầm cây non hữu cơ
        this.drawSprout(ctx, s);
      } else if (this.type === 3) {
        // 💧 Giọt sương mai đọng lá
        this.drawDewdrop(ctx, s);
      } else {
        // ✨ Đốm nắng ban mai ấm áp
        this.drawSunlight(ctx, s);
      }
    } else if (props.theme === 'seller') {
      // 🏪 Chủ đề Gian Hàng Seller: Hoa quả tươi & Cửa hàng
      ctx.shadowColor = 'rgba(0, 0, 0, 0.1)';
      ctx.shadowBlur = 8 * this.depth;
      ctx.shadowOffsetY = 3;

      if (this.type === 0) {
        this.drawStrawberry(ctx, s);
      } else if (this.type === 1) {
        this.drawFruit(ctx, s);
      } else if (this.type === 2) {
        this.drawGrape(ctx, s);
      } else if (this.type === 3) {
        this.drawStoreKiosk(ctx, s);
      } else {
        this.drawFruitBasket(ctx, s);
      }
    } else if (props.theme === 'shipper') {
      // 🛵 Chủ đề Tài Xế Shipper: Xe cộ & Hàng giao
      ctx.shadowColor = 'rgba(0, 0, 0, 0.12)';
      ctx.shadowBlur = 8 * this.depth;
      ctx.shadowOffsetY = 3;

      if (this.type === 0) {
        this.drawDeliveryBike(ctx, s);
      } else if (this.type === 1) {
        this.drawParcelBox(ctx, s);
      } else if (this.type === 2) {
        this.drawLocationPin(ctx, s);
      } else if (this.type === 3) {
        this.drawSpeedClock(ctx, s);
      } else {
        this.drawDriverHelmet(ctx, s);
      }
    } else if (props.theme === 'cyber') {
      // 🚀 Chủ đề Cyber Neon cho Face ID Modal
      ctx.shadowColor = this.color;
      ctx.shadowBlur = 12 * this.depth;
      ctx.fillStyle = this.color;

      ctx.beginPath();
      if (this.type === 0) {
        ctx.arc(0, 0, s * 0.28, 0, Math.PI * 2);
      } else if (this.type === 1) {
        const r = Math.min(4, s * 0.15);
        if (ctx.roundRect) ctx.roundRect(-s * 0.25, -s * 0.25, s * 0.5, s * 0.5, r);
        else ctx.rect(-s * 0.25, -s * 0.25, s * 0.5, s * 0.5);
      } else {
        const h = (s * 0.5 * Math.sqrt(3)) / 2;
        ctx.moveTo(0, -h / 2);
        ctx.lineTo(s * 0.25, h / 2);
        ctx.lineTo(-s * 0.25, h / 2);
        ctx.closePath();
      }
      ctx.fill();
    } else {
      // Geometry
      ctx.fillStyle = this.color;
      ctx.beginPath();
      ctx.arc(0, 0, s * 0.3, 0, Math.PI * 2);
      ctx.fill();
    }

    ctx.restore();
  }

  // 1. Vẽ lá cây non tươi mát
  private drawLeaf(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = this.color;
    ctx.beginPath();
    ctx.moveTo(0, -s * 0.52);
    ctx.quadraticCurveTo(s * 0.48, -s * 0.12, 0, s * 0.52);
    ctx.quadraticCurveTo(-s * 0.48, -s * 0.12, 0, -s * 0.52);
    ctx.fill();

    // Gân lá thanh thoát
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.42)';
    ctx.lineWidth = Math.max(1, s * 0.06);
    ctx.beginPath();
    ctx.moveTo(0, -s * 0.42);
    ctx.lineTo(0, s * 0.38);
    // Gân phụ
    ctx.moveTo(0, -s * 0.1);
    ctx.lineTo(s * 0.2, -s * 0.24);
    ctx.moveTo(0, -s * 0.1);
    ctx.lineTo(-s * 0.2, -s * 0.24);
    ctx.moveTo(0, s * 0.14);
    ctx.lineTo(s * 0.18, s * 0.02);
    ctx.moveTo(0, s * 0.14);
    ctx.lineTo(-s * 0.18, s * 0.02);
    ctx.stroke();
  }

  // 2. Vẽ quả cam / quả cà chua chín mọng
  private drawFruit(ctx: CanvasRenderingContext2D, s: number) {
    // Thân quả
    ctx.fillStyle = this.color;
    ctx.beginPath();
    ctx.arc(0, s * 0.06, s * 0.36, 0, Math.PI * 2);
    ctx.fill();

    // Vết bóng phản chiếu
    ctx.fillStyle = 'rgba(255, 255, 255, 0.38)';
    ctx.beginPath();
    ctx.arc(-s * 0.12, -s * 0.08, s * 0.09, 0, Math.PI * 2);
    ctx.fill();

    // Lá xanh nhỏ ở đầu cuống
    ctx.fillStyle = '#16A34A';
    ctx.beginPath();
    ctx.ellipse(s * 0.13, -s * 0.28, s * 0.16, s * 0.08, Math.PI / 4, 0, Math.PI * 2);
    ctx.fill();

    // Cuống nhỏ
    ctx.strokeStyle = '#78350F';
    ctx.lineWidth = Math.max(1.1, s * 0.07);
    ctx.beginPath();
    ctx.moveTo(0, -s * 0.24);
    ctx.quadraticCurveTo(s * 0.05, -s * 0.36, 0, -s * 0.42);
    ctx.stroke();
  }

  // 3. Vẽ mầm non hữu cơ 2 lá
  private drawSprout(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = this.color;
    // Lá trái
    ctx.beginPath();
    ctx.ellipse(-s * 0.2, -s * 0.07, s * 0.2, s * 0.11, -Math.PI / 4, 0, Math.PI * 2);
    ctx.fill();
    // Lá phải
    ctx.beginPath();
    ctx.ellipse(s * 0.2, -s * 0.07, s * 0.2, s * 0.11, Math.PI / 4, 0, Math.PI * 2);
    ctx.fill();

    // Thân mầm
    ctx.strokeStyle = '#15803D';
    ctx.lineWidth = Math.max(1.2, s * 0.08);
    ctx.beginPath();
    ctx.moveTo(0, -s * 0.05);
    ctx.quadraticCurveTo(-s * 0.04, s * 0.16, 0, s * 0.35);
    ctx.stroke();
  }

  // 4. Vẽ giọt sương ban mai
  private drawDewdrop(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = this.color;
    ctx.beginPath();
    ctx.moveTo(0, -s * 0.48);
    ctx.bezierCurveTo(s * 0.36, -s * 0.14, s * 0.4, s * 0.32, 0, s * 0.44);
    ctx.bezierCurveTo(-s * 0.4, s * 0.32, -s * 0.36, -s * 0.14, 0, -s * 0.48);
    ctx.fill();

    // Điểm sáng đọng sương
    ctx.fillStyle = 'rgba(255, 255, 255, 0.72)';
    ctx.beginPath();
    ctx.arc(-s * 0.1, -s * 0.03, s * 0.08, 0, Math.PI * 2);
    ctx.fill();
  }

  // 5. Vẽ đốm nắng ban mai ấm áp
  private drawSunlight(ctx: CanvasRenderingContext2D, s: number) {
    const grad = ctx.createRadialGradient(0, 0, 0, 0, 0, s * 0.48);
    grad.addColorStop(0, 'rgba(255, 255, 255, 0.88)');
    grad.addColorStop(0.35, this.color);
    grad.addColorStop(1, 'rgba(255, 255, 255, 0)');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(0, 0, s * 0.48, 0, Math.PI * 2);
    ctx.fill();
  }

  // ===== HỆ THỐNG ICON DÀNH CHO SELLER (HOA QUẢ & CỬA HÀNG) =====
  private drawStrawberry(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#EF4444';
    ctx.beginPath();
    ctx.moveTo(0, s * 0.38);
    ctx.bezierCurveTo(s * 0.38, s * 0.22, s * 0.35, -s * 0.2, 0, -s * 0.25);
    ctx.bezierCurveTo(-s * 0.35, -s * 0.2, -s * 0.38, s * 0.22, 0, s * 0.38);
    ctx.fill();

    ctx.fillStyle = '#16A34A';
    ctx.beginPath();
    ctx.ellipse(0, -s * 0.28, s * 0.18, s * 0.08, 0, 0, Math.PI * 2);
    ctx.ellipse(-s * 0.12, -s * 0.24, s * 0.12, s * 0.06, -0.4, 0, Math.PI * 2);
    ctx.ellipse(s * 0.12, -s * 0.24, s * 0.12, s * 0.06, 0.4, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#FEF08A';
    const seeds = [
      { x: -0.1, y: -0.05 }, { x: 0.1, y: -0.05 },
      { x: 0, y: 0.1 }, { x: -0.12, y: 0.18 }, { x: 0.12, y: 0.18 },
      { x: 0, y: 0.28 }
    ];
    for (const seed of seeds) {
      ctx.beginPath();
      ctx.arc(s * seed.x, s * seed.y, s * 0.03, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  private drawGrape(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#8B5CF6';
    const grapes = [
      { x: 0, y: -s * 0.15 }, { x: -s * 0.12, y: -s * 0.1 }, { x: s * 0.12, y: -s * 0.1 },
      { x: -s * 0.07, y: s * 0.05 }, { x: s * 0.07, y: s * 0.05 },
      { x: 0, y: s * 0.18 }
    ];
    for (const g of grapes) {
      ctx.beginPath();
      ctx.arc(g.x, g.y, s * 0.1, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.strokeStyle = '#15803D';
    ctx.lineWidth = Math.max(1.2, s * 0.06);
    ctx.beginPath();
    ctx.moveTo(0, -s * 0.15);
    ctx.lineTo(0, -s * 0.35);
    ctx.stroke();
  }

  private drawStoreKiosk(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#10B981';
    ctx.beginPath();
    ctx.moveTo(-s * 0.35, -s * 0.1);
    ctx.lineTo(s * 0.35, -s * 0.1);
    ctx.lineTo(s * 0.25, -s * 0.32);
    ctx.lineTo(-s * 0.25, -s * 0.32);
    ctx.closePath();
    ctx.fill();

    ctx.fillStyle = '#FFFFFF';
    ctx.fillRect(-s * 0.12, -s * 0.3, s * 0.08, s * 0.2);
    ctx.fillRect(s * 0.04, -s * 0.3, s * 0.08, s * 0.2);

    ctx.fillStyle = '#F59E0B';
    ctx.fillRect(-s * 0.28, -s * 0.1, s * 0.56, s * 0.38);

    ctx.fillStyle = '#1E293B';
    ctx.fillRect(-s * 0.18, 0, s * 0.36, s * 0.22);
  }

  private drawFruitBasket(ctx: CanvasRenderingContext2D, s: number) {
    ctx.strokeStyle = '#B45309';
    ctx.lineWidth = Math.max(1.2, s * 0.07);
    ctx.beginPath();
    ctx.arc(0, -s * 0.05, s * 0.28, Math.PI, 0);
    ctx.stroke();

    ctx.fillStyle = '#EF4444';
    ctx.beginPath();
    ctx.arc(-s * 0.1, -s * 0.05, s * 0.12, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#F97316';
    ctx.beginPath();
    ctx.arc(s * 0.08, -s * 0.04, s * 0.11, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#D97706';
    ctx.beginPath();
    ctx.moveTo(-s * 0.32, -s * 0.02);
    ctx.lineTo(s * 0.32, -s * 0.02);
    ctx.lineTo(s * 0.22, s * 0.3);
    ctx.lineTo(-s * 0.22, s * 0.3);
    ctx.closePath();
    ctx.fill();
  }

  // ===== HỆ THỐNG ICON DÀNH CHO SHIPPER (XE CỘ & HÀNG GIAO) =====
  private drawDeliveryBike(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#334155';
    ctx.beginPath();
    ctx.arc(-s * 0.22, s * 0.18, s * 0.11, 0, Math.PI * 2);
    ctx.arc(s * 0.22, s * 0.18, s * 0.11, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#E2E8F0';
    ctx.beginPath();
    ctx.arc(-s * 0.22, s * 0.18, s * 0.04, 0, Math.PI * 2);
    ctx.arc(s * 0.22, s * 0.18, s * 0.04, 0, Math.PI * 2);
    ctx.fill();

    ctx.strokeStyle = '#0284C7';
    ctx.lineWidth = Math.max(1.8, s * 0.08);
    ctx.beginPath();
    ctx.moveTo(-s * 0.22, s * 0.18);
    ctx.lineTo(0, s * 0.1);
    ctx.lineTo(s * 0.16, s * 0.18);
    ctx.lineTo(s * 0.22, -s * 0.08);
    ctx.stroke();

    ctx.fillStyle = '#EA580C';
    ctx.fillRect(-s * 0.32, -s * 0.1, s * 0.22, s * 0.2);

    ctx.fillStyle = '#FFFFFF';
    ctx.fillRect(-s * 0.26, -s * 0.04, s * 0.1, s * 0.08);
  }

  private drawParcelBox(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#F59E0B';
    ctx.fillRect(-s * 0.24, -s * 0.24, s * 0.48, s * 0.48);

    ctx.fillStyle = '#FEF08A';
    ctx.fillRect(-s * 0.06, -s * 0.24, s * 0.12, s * 0.48);

    ctx.fillStyle = '#FFFFFF';
    ctx.fillRect(s * 0.08, -s * 0.16, s * 0.12, s * 0.14);
  }

  private drawLocationPin(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#EF4444';
    ctx.beginPath();
    ctx.arc(0, -s * 0.1, s * 0.22, Math.PI, 0);
    ctx.lineTo(0, s * 0.32);
    ctx.closePath();
    ctx.fill();

    ctx.fillStyle = '#FFFFFF';
    ctx.beginPath();
    ctx.arc(0, -s * 0.1, s * 0.08, 0, Math.PI * 2);
    ctx.fill();
  }

  private drawSpeedClock(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#EA580C';
    ctx.beginPath();
    ctx.arc(0, 0, s * 0.26, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#FFFFFF';
    ctx.beginPath();
    ctx.moveTo(s * 0.04, -s * 0.18);
    ctx.lineTo(-s * 0.12, 0);
    ctx.lineTo(0, 0);
    ctx.lineTo(-s * 0.04, s * 0.18);
    ctx.lineTo(s * 0.12, 0);
    ctx.lineTo(0, 0);
    ctx.closePath();
    ctx.fill();
  }

  private drawDriverHelmet(ctx: CanvasRenderingContext2D, s: number) {
    ctx.fillStyle = '#0EA5E9';
    ctx.beginPath();
    ctx.arc(0, 0, s * 0.26, Math.PI, 0);
    ctx.closePath();
    ctx.fill();

    ctx.fillStyle = 'rgba(255, 255, 255, 0.45)';
    ctx.beginPath();
    ctx.arc(0, 0, s * 0.22, 0, Math.PI / 2);
    ctx.lineTo(0, 0);
    ctx.closePath();
    ctx.fill();

    ctx.strokeStyle = '#334155';
    ctx.lineWidth = Math.max(1, s * 0.05);
    ctx.beginPath();
    ctx.arc(0, 0, s * 0.24, Math.PI / 2, Math.PI);
    ctx.stroke();
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
  const count = props.particleCount || 42;
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
  () => [props.particleCount, props.colors, props.theme],
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
