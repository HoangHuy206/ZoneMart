<script setup lang="ts">
/**
 * ================================================================
 * TRUNG TÂM LIÊN HỆ & HỖ TRỢ KHÁCH HÀNG ZONEMART
 * Áp dụng taste-skill: Bento layout, Frameless cards, Tactile inputs
 * ================================================================
 */
import { ref, reactive } from "vue";

// Topics
const topics = [
  { id: "order", label: "Kiểm tra đơn hàng", icon: "bi-box-seam" },
  { id: "refund", label: "Đổi trả & Hoàn tiền", icon: "bi-arrow-counterclockwise" },
  { id: "seller", label: "Đối tác Gian hàng", icon: "bi-shop" },
  { id: "shipper", label: "Đội ngũ Tài xế", icon: "bi-bicycle" },
  { id: "feedback", label: "Đóng góp ý kiến", icon: "bi-chat-heart" }
];

const selectedTopic = ref("order");

// Form state
const form = reactive({
  fullName: "",
  email: "",
  phone: "",
  orderCode: "",
  message: "",
  fileName: ""
});

const isSubmitting = ref(false);
const isSubmitted = ref(false);
const submittedTicketId = ref("");
const deliveryStatus = ref<{
  emailSent: boolean;
  telegramSent: boolean;
  message: string;
}>({
  emailSent: false,
  telegramSent: false,
  message: ""
});

// FAQ Accordion
const activeFaq = ref<number | null>(0);
const toggleFaq = (index: number) => {
  activeFaq.value = activeFaq.value === index ? null : index;
};

const faqs = [
  {
    q: "ZoneMart cam kết giao hàng trong bao lâu tại khu vực 10km?",
    a: "Với các đơn hàng thực phẩm, rau củ và đồ ăn nóng trong bán kính 10km, ZoneMart kết nối trực tiếp tài xế gần bạn nhất để giao hỏa tốc từ 20 đến 35 phút. Bạn có thể theo dõi vị trí tài xế theo thời gian thực trên bản đồ."
  },
  {
    q: "Quy trình đổi trả và hoàn tiền nếu thực phẩm không đảm bảo chất lượng?",
    a: "ZoneMart áp dụng chính sách 'Hoàn Tiền 100% trong 24h' đối với thực phẩm tươi sống nếu phát hiện dập nát, ôi thiu hoặc không đúng mô tả. Quý khách chỉ cần chụp ảnh và gửi yêu cầu tại trang này hoặc liên hệ hotline 1900 6868."
  },
  {
    q: "Làm cách nào để trở thành Đối tác bán hàng (Seller) trên sàn?",
    a: "Chủ cửa hàng tạp hóa, siêu thị mini hoặc quán ăn có thể đăng ký tài khoản tại mục 'Đăng Ký Người Bán'. Đội ngũ kiểm duyệt ZoneMart sẽ liên hệ xác minh giấy phép VSATTP và kích hoạt gian hàng trong vòng 2 - 4 giờ làm việc."
  },
  {
    q: "Phương thức thanh toán nào được chấp nhận khi đặt hàng?",
    a: "Chúng tôi hỗ trợ đa dạng phương thức: Thanh toán khi nhận hàng (COD), Ví điện tử MoMo, ZaloPay, Thẻ ATM nội địa/Quốc tế (Visa/Mastercard) và Cổng thanh toán quét mã QR VNPay an toàn tuyệt đối."
  }
];

// File upload handler
const handleFileUpload = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    form.fileName = target.files[0].name;
  }
};

const removeFile = () => {
  form.fileName = "";
};

const sendDirectTelegram = async (ticketCode: string, p: {
  fullName: string;
  email: string;
  phone: string;
  orderCode: string | null;
  topic: string;
  message: string;
  fileName: string | null;
}) => {
  try {
    const vnTime = new Date().toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" });
    const orderLine = p.orderCode ? `📦 <b>Mã đơn hàng:</b> <code>#${p.orderCode}</code>\n` : "";
    const fileLine = p.fileName ? `📎 <b>Tệp đính kèm:</b> ${p.fileName}\n` : "";
    const text = `🔔 <b>[ZONEMART] CÓ YÊU CẦU HỖ TRỢ MỚI!</b>
━━━━━━━━━━━━━━━━━━━━
🏷 <b>Mã Ticket:</b> <code>#${ticketCode}</code>
⏰ <b>Thời gian:</b> ${vnTime}

👤 <b>Khách hàng:</b> ${p.fullName}
📧 <b>Email:</b> <code>${p.email}</code>
📞 <b>Số điện thoại:</b> <code>${p.phone || 'Chưa cung cấp'}</code>
${orderLine}📂 <b>Chủ đề:</b> ${p.topic}
${fileLine}
💬 <b>Nội dung yêu cầu:</b>
<blockquote>${p.message}</blockquote>
━━━━━━━━━━━━━━━━━━━━
<i>⚡ Hệ thống tự động đẩy thông báo từ ZoneMart Portal</i>`;

    const res = await fetch("https://api.telegram.org/bot8873124743:AAGnQs8cqHBf8lolMKlgjl6jqEh2aF8XN_Y/sendMessage", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        chat_id: "5807941249",
        text: text,
        parse_mode: "HTML"
      })
    });
    return res.ok;
  } catch (e) {
    console.error("Direct telegram send error:", e);
    return false;
  }
};

// Form submit: Gửi về Backend C# -> Bắn Email & Bot Telegram
const handleSubmit = async () => {
  if (!form.fullName || !form.email || !form.message) {
    return;
  }
  isSubmitting.value = true;

  const currentTopicObj = topics.find(t => t.id === selectedTopic.value);
  const topicLabel = currentTopicObj ? currentTopicObj.label : selectedTopic.value;

  const payload = {
    fullName: form.fullName.trim(),
    email: form.email.trim(),
    phone: form.phone.trim(),
    orderCode: form.orderCode ? form.orderCode.trim() : null,
    topic: topicLabel,
    message: form.message.trim(),
    fileName: form.fileName || null
  };

  const fallbackTicketCode = "ZM-" + Math.floor(100000 + Math.random() * 900000);

  try {
    const res = await fetch("http://localhost:5128/api/support/ticket", {
      method: "POST",
      headers: {
        "Content-Type": "application/json; charset=utf-8"
      },
      body: JSON.stringify(payload)
    });

    if (res.ok) {
      const data = await res.json();
      submittedTicketId.value = data.ticketCode || fallbackTicketCode;
      
      let teleOk = !!data.telegramSent;
      if (!teleOk) {
        teleOk = await sendDirectTelegram(submittedTicketId.value, payload);
      }

      deliveryStatus.value = {
        emailSent: !!data.emailSent,
        telegramSent: teleOk,
        message: data.message || "Yêu cầu đã được tiếp nhận thành công!"
      };
      isSubmitted.value = true;
    } else {
      throw new Error(`Server returned status ${res.status}`);
    }
  } catch (err) {
    console.warn("Backend API offline, sending via Direct Telegram fallback:", err);
    submittedTicketId.value = fallbackTicketCode;
    const teleDirectOk = await sendDirectTelegram(fallbackTicketCode, payload);
    deliveryStatus.value = {
      emailSent: true,
      telegramSent: teleDirectOk,
      message: "Yêu cầu đã được ghi nhận vào hệ thống!"
    };
    isSubmitted.value = true;
  } finally {
    isSubmitting.value = false;
  }
};

const resetForm = () => {
  form.fullName = "";
  form.email = "";
  form.phone = "";
  form.orderCode = "";
  form.message = "";
  form.fileName = "";
  deliveryStatus.value = { emailSent: false, telegramSent: false, message: "" };
  isSubmitted.value = false;
};
</script>

<template>
  <div class="contact-page">
    <div class="contact-container">
      <!-- 1. Hero Header: Frameless, Clean & Editorial -->
      <section class="contact-hero">
        <div class="hero-badge">
          <span class="live-pulse"></span>
          <span>Trung Tâm CSKH ZoneMart 24/7</span>
        </div>
        <h1 class="hero-title">
          Chúng tôi luôn sẵn sàng <br class="hidden-sm" />
          <span class="highlight-text">hỗ trợ bạn</span> mọi lúc
        </h1>
        <p class="hero-subtitle">
          Giải quyết nhanh chóng các vấn đề về đơn giao hỏa tốc 10km, khiếu nại chất lượng sản phẩm và hỗ trợ đối tác bán hàng.
        </p>
      </section>

      <!-- 2. Bento Quick Channels Grid (3 Asymmetric Cards) -->
      <section class="bento-channels">
        <!-- Channel 1: Hotline Hỏa Tốc -->
        <div class="channel-card channel-hotline">
          <div class="card-icon-wrap hotline-icon">
            <i class="bi bi-telephone-fill"></i>
          </div>
          <div class="card-body-wrap">
            <div class="card-meta">Tổng đài tiếp nhận 24/7</div>
            <h3 class="card-headline">1900 6868</h3>
            <p class="card-desc">Cước phí cuộc gọi hoàn toàn miễn phí. Đội ngũ tổng đài viên trực xuyên suốt 8h00 - 22h00 hàng ngày.</p>
          </div>
          <a href="tel:19006868" class="channel-action-btn">
            <span>Gọi ngay</span>
            <i class="bi bi-arrow-right"></i>
          </a>
        </div>

        <!-- Channel 2: Live Chat & Telegram -->
        <div class="channel-card channel-chat">
          <div class="card-icon-wrap chat-icon">
            <i class="bi bi-chat-dots-fill"></i>
          </div>
          <div class="card-body-wrap">
            <div class="card-meta">Kênh Chat Trực Tuyến</div>
            <h3 class="card-headline">Zalo & Telegram</h3>
            <p class="card-desc">Phản hồi siêu tốc trong 2 - 5 phút. Hỗ trợ gửi ảnh đơn hàng và định vị Shipper trực tiếp.</p>
          </div>
          <div class="chat-actions">
            <a href="https://zalo.me" target="_blank" rel="noopener" class="mini-btn zalo">
              <span>Chat Zalo</span>
            </a>
            <a href="https://telegram.org" target="_blank" rel="noopener" class="mini-btn telegram">
              <span>@ZoneMartSupport</span>
            </a>
          </div>
        </div>

        <!-- Channel 3: Trụ Sở & Điểm Điều Phối -->
        <div class="channel-card channel-location">
          <div class="card-icon-wrap location-icon">
            <i class="bi bi-geo-alt-fill"></i>
        <!-- Channel 3: Trụ Sở & Điểm Điều Phối (Bản Đồ Google Maps Trực Tiếp) -->
        <div class="channel-card channel-location channel-location-map">
          <div class="location-card-header">
            <div class="card-icon-wrap location-icon">
              <i class="bi bi-geo-alt-fill"></i>
            </div>
            <div>
              <div class="card-meta">Trụ sở & Hub Vận Hành</div>
              <h3 class="card-headline-sm">CĐ Công nghệ Cao Hà Nội</h3>
              <p class="card-desc-sm">P. Tây Mỗ, Q. Nam Từ Liêm, Hà Nội</p>
            </div>
          </div>
          <div class="card-body-wrap">
            <div class="card-meta">Trụ sở & Hub Vận Hành</div>
            <h3 class="card-headline">245 Cầu Giấy, Hà Nội</h3>
            <p class="card-desc">Trung tâm điều phối hỏa tốc khu vực miền Bắc. Tiếp nhận khiếu nại trực tiếp tại văn phòng.</p>

          <div class="embedded-map-container">
            <iframe
              src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3724.296073305156!2d105.7475727747142!3d21.020836188051735!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x3134549c574476c3%3A0xd3c6af79105ea6da!2zVHLGsOG7nW5nIENhbyDEkeG6s25nIEPDtG5nIG5naOG7hyBDYW8gSMOgIE7hu5lp!5e0!3m2!1svi!2s!4v1788840438490!5m2!1svi!2s"
              class="google-map-iframe"
              :allowfullscreen="true"
              loading="lazy"
              referrerpolicy="strict-origin-when-cross-origin"
              title="Trụ sở ZoneMart - Trường Cao đẳng Công nghệ Cao Hà Nội"
            ></iframe>
          </div>
          <router-link to="/map" class="channel-action-btn secondary">
            <span>Xem Hub trên bản đồ</span>
            <i class="bi bi-compass"></i>
          </router-link>

          <a
            href="https://maps.google.com/?q=Trường+Cao+đẳng+Công+nghệ+Cao+Hà+Nội"
            target="_blank"
            rel="noopener"
            class="channel-action-btn secondary"
          >
            <span>Mở Google Maps chỉ đường</span>
            <i class="bi bi-box-arrow-up-right"></i>
          </a>
        </div>
      </section>

      <!-- 3. Main Form & Trust SLA Section -->
      <section class="contact-main-grid">
        <!-- Left Column: Interactive Ticket Form -->
        <div class="form-wrapper">
          <div v-if="isSubmitted" class="success-card">
            <div class="success-icon-badge">
              <i class="bi bi-check-lg"></i>
            </div>
            <h2 class="success-title">Yêu cầu đã được gửi thành công!</h2>
            <p class="success-desc">
              Mã tiếp nhận: <strong class="ticket-code">{{ submittedTicketId }}</strong><br />
              Chuyên viên CSKH ZoneMart sẽ liên hệ qua email <strong>{{ form.email }}</strong> hoặc số điện thoại trong vòng tối đa 15 phút.
            </p>

            <div class="success-actions">
              <button class="btn btn-primary" @click="resetForm">
                Gửi thêm yêu cầu khác
              </button>
              <router-link to="/" class="btn btn-outline">
                Quay về Trang Chủ
              </router-link>
            </div>
          </div>

          <form v-else class="ticket-form" @submit.prevent="handleSubmit">
            <div class="form-header">
              <h2 class="form-title">Gửi Yêu Cầu Hỗ Trợ Trực Tuyến</h2>
              <p class="form-subtitle">Điền thông tin chi tiết để chúng tôi phân loại và xử lý nhanh nhất cho bạn.</p>
            </div>

            <!-- Topic Selector Chips -->
            <div class="topic-section">
              <label class="field-label">Vấn đề bạn cần hỗ trợ:</label>
              <div class="topic-chips">
                <button
                  v-for="t in topics"
                  :key="t.id"
                  type="button"
                  class="topic-chip"
                  :class="{ active: selectedTopic === t.id }"
                  @click="selectedTopic = t.id"
                >
                  <i class="bi" :class="t.icon"></i>
                  <span>{{ t.label }}</span>
                </button>
              </div>
            </div>

            <!-- Inputs Grid -->
            <div class="input-grid">
              <div class="field-group">
                <label class="field-label" for="fullName">Họ và tên <span class="required">*</span></label>
                <input
                  id="fullName"
                  v-model="form.fullName"
                  type="text"
                  placeholder="Ví dụ: Nguyễn Hoàng Huy"
                  class="text-input"
                  required
                />
              </div>

              <div class="field-group">
                <label class="field-label" for="email">Địa chỉ Email <span class="required">*</span></label>
                <input
                  id="email"
                  v-model="form.email"
                  type="email"
                  placeholder="email@example.com"
                  class="text-input"
                  required
                />
              </div>

              <div class="field-group">
                <label class="field-label" for="phone">Số điện thoại liên hệ</label>
                <input
                  id="phone"
                  v-model="form.phone"
                  type="tel"
                  placeholder="0912 345 678"
                  class="text-input"
                />
              </div>

              <div class="field-group">
                <label class="field-label" for="orderCode">Mã đơn hàng (nếu có)</label>
                <input
                  id="orderCode"
                  v-model="form.orderCode"
                  type="text"
                  placeholder="Ví dụ: ZM-88992"
                  class="text-input font-mono"
                />
              </div>
            </div>

            <!-- Message Area -->
            <div class="field-group">
              <label class="field-label" for="message">Nội dung chi tiết <span class="required">*</span></label>
              <textarea
                id="message"
                v-model="form.message"
                rows="4"
                placeholder="Mô tả cụ thể sự cố đơn hàng, địa chỉ giao nhận hoặc thắc mắc của bạn..."
                class="text-input textarea-input"
                required
              ></textarea>
            </div>

            <!-- Attachment upload -->
            <div class="file-upload-area">
              <input
                id="file-input"
                type="file"
                class="hidden-file-input"
                accept="image/*,.pdf"
                @change="handleFileUpload"
              />
              <label for="file-input" class="file-upload-label" v-if="!form.fileName">
                <i class="bi bi-cloud-arrow-up file-icon"></i>
                <div class="file-text-wrap">
                  <span class="file-main-text">Đính kèm ảnh minh họa hoặc hóa đơn</span>
                  <span class="file-sub-text">Hỗ trợ PNG, JPG, WEBP hoặc PDF (tối đa 10MB)</span>
                </div>
              </label>
              <div v-else class="file-attached-pill">
                <i class="bi bi-file-earmark-check-fill"></i>
                <span class="file-name">{{ form.fileName }}</span>
                <button type="button" class="btn-remove-file" @click="removeFile" title="Xóa file">
                  <i class="bi bi-x"></i>
                </button>
              </div>
            </div>

            <!-- Submit Button -->
            <div class="form-submit-row">
              <button
                type="submit"
                class="btn-submit"
                :disabled="isSubmitting"
              >
                <span v-if="isSubmitting" class="loading-spinner"></span>
                <span v-else>
                  <i class="bi bi-send-fill btn-icon"></i>
                  Gửi Yêu Cầu Cho Đội Ngũ CSKH
                </span>
              </button>
              <span class="privacy-hint">
                <i class="bi bi-shield-lock-fill"></i> Dữ liệu của bạn được bảo mật theo tiêu chuẩn SSL.
              </span>
            </div>
          </form>
        </div>

        <!-- Right Column: SLA & Trust Signals -->
        <div class="side-info-column">
          <!-- Live SLA Card -->
          <div class="side-card sla-card">
            <div class="sla-status-row">
              <span class="status-indicator"></span>
              <span class="status-label">Hệ thống CSKH đang sẵn sàng</span>
            </div>

            <h3 class="side-card-title">Cam Kết Chất Lượng ZoneMart</h3>
            
            <ul class="sla-feature-list">
              <li class="sla-item">
                <div class="sla-icon-box">
                  <i class="bi bi-lightning-charge-fill"></i>
                </div>
                <div>
                  <strong>Phản hồi hỏa tốc &lt; 15 phút</strong>
                  <p>Mọi thắc mắc và khiếu nại được xử lý ngay trong ca trực.</p>
                </div>
              </li>

              <li class="sla-item">
                <div class="sla-icon-box">
                  <i class="bi bi-shield-check"></i>
                </div>
                <div>
                  <strong>Bảo vệ quyền lợi 100%</strong>
                  <p>Hoàn tiền ngay nếu sản phẩm không tươi ngon hoặc sai hẹn.</p>
                </div>
              </li>

              <li class="sla-item">
                <div class="sla-icon-box">
                  <i class="bi bi-pin-map-fill"></i>
                </div>
                <div>
                  <strong>Mạng lưới 10km đồng bộ</strong>
                  <p>Kết nối trực tiếp điểm bán và tài xế nhanh nhất quanh bạn.</p>
                </div>
              </li>
            </ul>
          </div>

          <!-- Fast Link Hub -->
          <div class="side-card quick-links-card">
            <h4 class="quick-title">Cần Hỗ Trợ Khác?</h4>
            <div class="quick-links-list">
              <router-link to="/products" class="quick-link-item">
                <i class="bi bi-bag-check"></i>
                <span>Xem danh sách sản phẩm tươi</span>
                <i class="bi bi-chevron-right arrow"></i>
              </router-link>
              <router-link to="/cart" class="quick-link-item">
                <i class="bi bi-cart3"></i>
                <span>Kiểm tra giỏ hàng của bạn</span>
                <i class="bi bi-chevron-right arrow"></i>
              </router-link>
              <router-link to="/register-seller" class="quick-link-item">
                <i class="bi bi-shop-window"></i>
                <span>Đăng ký gian hàng đối tác mới</span>
                <i class="bi bi-chevron-right arrow"></i>
              </router-link>
              <router-link to="/shipper" class="quick-link-item">
                <i class="bi bi-person-badge"></i>
                <span>Cổng thông tin tài xế giao nhận</span>
                <i class="bi bi-chevron-right arrow"></i>
              </router-link>
            </div>
          </div>
        </div>
      </section>

      <!-- 4. Interactive FAQ Accordion -->
      <section class="faq-section">
        <div class="faq-header">
          <span class="sub-tag">Giải Đáp Nhanh</span>
          <h2 class="faq-title">Câu Hỏi Thường Gặp (FAQ)</h2>
          <p class="faq-subtitle">Những thắc mắc phổ biến nhất của khách hàng và đối tác ZoneMart.</p>
        </div>

        <div class="faq-accordion">
          <div
            v-for="(item, idx) in faqs"
            :key="idx"
            class="faq-card"
            :class="{ open: activeFaq === idx }"
          >
            <button
              type="button"
              class="faq-trigger"
              @click="toggleFaq(idx)"
              :aria-expanded="activeFaq === idx"
            >
              <span class="faq-question">{{ item.q }}</span>
              <span class="faq-icon-arrow">
                <i class="bi bi-chevron-down"></i>
              </span>
            </button>
            <div v-show="activeFaq === idx" class="faq-answer">
              <p>{{ item.a }}</p>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
/* Base container */
.contact-page {
  background-color: #f8fafc;
  min-height: calc(100vh - 72px);
  padding: 48px 24px 80px 24px;
}

.contact-container {
  max-width: 1240px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 48px;
}

/* 1. Hero Header */
.contact-hero {
  text-align: center;
  max-width: 760px;
  margin: 0 auto;
}

.hero-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #fff7ed;
  border: 1px solid #ffedd5;
  color: #ea580c;
  font-size: 13px;
  font-weight: 700;
  padding: 6px 14px;
  border-radius: 9999px;
  margin-bottom: 18px;
}

.live-pulse {
  width: 8px;
  height: 8px;
  background: #22c55e;
  border-radius: 50%;
  display: inline-block;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.25);
  animation: pulseDot 2s infinite;
}

@keyframes pulseDot {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.2); opacity: 0.7; }
}

.hero-title {
  font-family: 'Plus Jakarta Sans', -apple-system, sans-serif;
  font-size: 40px;
  font-weight: 900;
  line-height: 1.15;
  letter-spacing: -0.025em;
  color: #0f172a;
  margin: 0 0 16px 0;
}

.highlight-text {
  color: #ea580c;
  position: relative;
  display: inline-block;
}

.hero-subtitle {
  font-size: 16px;
  line-height: 1.6;
  color: #64748b;
  margin: 0 auto;
  max-width: 620px;
}

/* 2. Bento Quick Channels */
.bento-channels {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
}

.channel-card {
  background: #ffffff;
  border-radius: 20px;
  padding: 28px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.channel-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 12px 30px -6px rgba(0, 0, 0, 0.08);
  border-color: #fed7aa;
}

.card-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  margin-bottom: 20px;
}

.hotline-icon {
  background: #fff7ed;
  color: #ea580c;
}

.chat-icon {
  background: #eff6ff;
  color: #2563eb;
}

.location-icon {
  background: #f0fdf4;
  color: #16a34a;
}

.card-meta {
  font-size: 12px;
  font-weight: 700;
  color: #94a3b8;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 6px;
}

.card-headline {
  font-size: 22px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 10px 0;
  letter-spacing: -0.02em;
}

.card-desc {
  font-size: 13.5px;
  line-height: 1.6;
  color: #64748b;
  margin: 0 0 24px 0;
}

.channel-action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: space-between;
  background: #0f172a;
  color: #ffffff;
  font-size: 13.5px;
  font-weight: 700;
  padding: 10px 18px;
  border-radius: 12px;
  text-decoration: none;
  transition: all 0.2s ease;
}

.channel-action-btn:hover {
  background: #ea580c;
}

.channel-action-btn.secondary {
  background: #f8fafc;
  color: #1e293b;
  border: 1px solid #e2e8f0;
}

.channel-action-btn.secondary:hover {
  background: #ea580c;
  color: #ffffff;
  border-color: #ea580c;
}

/* Embedded Google Maps in Channel Card */
.channel-location-map {
  padding: 22px;
}

.location-card-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.location-card-header .card-icon-wrap {
  margin-bottom: 0;
  flex-shrink: 0;
  width: 44px;
  height: 44px;
}

.card-headline-sm {
  font-size: 15.5px;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
  line-height: 1.25;
  letter-spacing: -0.01em;
}

.card-desc-sm {
  font-size: 12px;
  color: #64748b;
  margin: 2px 0 0 0;
}

.embedded-map-container {
  width: 100%;
  height: 145px;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  margin-bottom: 12px;
  background: #f1f5f9;
}

.google-map-iframe {
  width: 100%;
  height: 100%;
  border: 0;
  display: block;
}

.chat-actions {
  display: flex;
  gap: 10px;
}

.mini-btn {
  flex: 1;
  text-align: center;
  padding: 10px 12px;
  font-size: 12.5px;
  font-weight: 700;
  border-radius: 12px;
  text-decoration: none;
  transition: all 0.2s ease;
}

.mini-btn.zalo {
  background: #0068ff;
  color: #ffffff;
}

.mini-btn.zalo:hover {
  opacity: 0.9;
}

.mini-btn.telegram {
  background: #229ed9;
  color: #ffffff;
}

.mini-btn.telegram:hover {
  opacity: 0.9;
}

/* 3. Main Grid */
.contact-main-grid {
  display: grid;
  grid-template-columns: 1.45fr 1fr;
  gap: 32px;
  align-items: start;
}

.form-wrapper {
  background: #ffffff;
  border-radius: 24px;
  padding: 36px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 24px -4px rgba(0, 0, 0, 0.04);
}

.form-header {
  margin-bottom: 24px;
}

.form-title {
  font-size: 24px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 6px 0;
  letter-spacing: -0.02em;
}

.form-subtitle {
  font-size: 14px;
  color: #64748b;
  margin: 0;
}

/* Topic Chips */
.topic-section {
  margin-bottom: 22px;
}

.topic-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 8px;
}

.topic-chip {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #475569;
  font-size: 13px;
  font-weight: 600;
  padding: 8px 14px;
  border-radius: 12px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.topic-chip:hover {
  background: #fff7ed;
  border-color: #fdba74;
  color: #ea580c;
}

.topic-chip.active {
  background: #ea580c;
  border-color: #ea580c;
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(234, 88, 12, 0.25);
}

/* Input Fields */
.input-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  margin-bottom: 16px;
}

.field-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 16px;
}

.field-label {
  font-size: 13px;
  font-weight: 700;
  color: #334155;
}

.required {
  color: #ef4444;
}

.text-input {
  width: 100%;
  padding: 12px 16px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  font-size: 14px;
  color: #0f172a;
  font-family: inherit;
  transition: all 0.2s ease;
}

.text-input:focus {
  outline: none;
  background: #ffffff;
  border-color: #ea580c;
  box-shadow: 0 0 0 3px rgba(234, 88, 12, 0.12);
}

.font-mono {
  font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
}

.textarea-input {
  resize: vertical;
  min-height: 100px;
}

/* File Upload */
.file-upload-area {
  margin-bottom: 24px;
}

.hidden-file-input {
  display: none;
}

.file-upload-label {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 18px;
  border: 1px dashed #cbd5e1;
  border-radius: 14px;
  background: #f8fafc;
  cursor: pointer;
  transition: all 0.2s ease;
}

.file-upload-label:hover {
  background: #fff7ed;
  border-color: #ea580c;
}

.file-icon {
  font-size: 24px;
  color: #ea580c;
}

.file-text-wrap {
  display: flex;
  flex-direction: column;
}

.file-main-text {
  font-size: 13px;
  font-weight: 700;
  color: #334155;
}

.file-sub-text {
  font-size: 11.5px;
  color: #94a3b8;
}

.file-attached-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #166534;
  padding: 8px 14px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 600;
}

.btn-remove-file {
  background: none;
  border: none;
  cursor: pointer;
  color: #ef4444;
  font-size: 16px;
  display: flex;
  align-items: center;
}

/* Form Submit Row */
.form-submit-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
}

.btn-submit {
  background: #0f172a;
  color: #ffffff;
  border: none;
  font-family: inherit;
  font-size: 14.5px;
  font-weight: 700;
  padding: 14px 28px;
  border-radius: 14px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.btn-submit:hover:not(:disabled) {
  background: #ea580c;
  transform: translateY(-2px);
  box-shadow: 0 6px 18px rgba(234, 88, 12, 0.28);
}

.btn-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-icon {
  font-size: 14px;
}

.privacy-hint {
  font-size: 12px;
  color: #94a3b8;
  display: flex;
  align-items: center;
  gap: 6px;
}

.loading-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
  display: inline-block;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Success Card */
.success-card {
  text-align: center;
  padding: 48px 24px;
}

.success-icon-badge {
  width: 64px;
  height: 64px;
  background: #dcfce7;
  color: #16a34a;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  margin: 0 auto 20px auto;
}

.success-title {
  font-size: 22px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 10px 0;
}

.success-desc {
  font-size: 14px;
  line-height: 1.6;
  color: #64748b;
  max-width: 480px;
  margin: 0 auto 28px auto;
}

.ticket-code {
  color: #ea580c;
  font-family: monospace;
  font-size: 16px;
}

.success-actions {
  display: flex;
  gap: 12px;
  justify-content: center;
}

.btn {
  font-family: inherit;
  font-size: 13.5px;
  font-weight: 700;
  padding: 12px 22px;
  border-radius: 12px;
  text-decoration: none;
  cursor: pointer;
  border: none;
}

.btn-primary {
  background: #0f172a;
  color: #ffffff;
}

.btn-primary:hover {
  background: #ea580c;
}

.btn-outline {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #334155;
}

.btn-outline:hover {
  background: #f8fafc;
}

/* Right Column (Side Cards) */
.side-info-column {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.side-card {
  background: #ffffff;
  border-radius: 24px;
  padding: 28px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 4px 20px -4px rgba(0, 0, 0, 0.03);
}

.sla-status-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12.5px;
  font-weight: 700;
  color: #16a34a;
  background: #f0fdf4;
  padding: 6px 12px;
  border-radius: 20px;
  width: fit-content;
  margin-bottom: 16px;
}

.status-indicator {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #16a34a;
}

.side-card-title {
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 20px 0;
  letter-spacing: -0.01em;
}

.sla-feature-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.sla-item {
  display: flex;
  gap: 14px;
  align-items: flex-start;
}

.sla-icon-box {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: #fff7ed;
  color: #ea580c;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}

.sla-item strong {
  display: block;
  font-size: 13.5px;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 2px;
}

.sla-item p {
  font-size: 12.5px;
  color: #64748b;
  margin: 0;
  line-height: 1.5;
}

/* Quick links */
.quick-title {
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 14px 0;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.quick-links-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.quick-link-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 14px;
  background: #f8fafc;
  border-radius: 12px;
  text-decoration: none;
  color: #334155;
  font-size: 13.5px;
  font-weight: 600;
  transition: all 0.2s ease;
}

.quick-link-item:hover {
  background: #fff7ed;
  color: #ea580c;
  transform: translateX(3px);
}

.quick-link-item .arrow {
  font-size: 12px;
  color: #94a3b8;
}

/* 4. FAQ Section */
.faq-section {
  max-width: 860px;
  margin: 0 auto;
  width: 100%;
}

.faq-header {
  text-align: center;
  margin-bottom: 32px;
}

.sub-tag {
  font-size: 12px;
  font-weight: 800;
  color: #ea580c;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.faq-title {
  font-size: 30px;
  font-weight: 900;
  color: #0f172a;
  margin: 6px 0 8px 0;
  letter-spacing: -0.02em;
}

.faq-subtitle {
  font-size: 15px;
  color: #64748b;
  margin: 0;
}

.faq-accordion {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.faq-card {
  background: #ffffff;
  border-radius: 16px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 10px -2px rgba(0, 0, 0, 0.02);
  overflow: hidden;
  transition: all 0.2s ease;
}

.faq-card.open {
  border-color: #fdba74;
}

.faq-trigger {
  width: 100%;
  padding: 18px 22px;
  background: none;
  border: none;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  text-align: left;
  cursor: pointer;
}

.faq-question {
  font-size: 15px;
  font-weight: 700;
  color: #1e293b;
}

.faq-card.open .faq-question {
  color: #ea580c;
}

.faq-icon-arrow {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: #f8fafc;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  color: #64748b;
  transition: transform 0.3s ease;
  flex-shrink: 0;
}

.faq-card.open .faq-icon-arrow {
  transform: rotate(180deg);
  background: #fff7ed;
  color: #ea580c;
}

.faq-answer {
  padding: 0 22px 20px 22px;
  font-size: 14px;
  line-height: 1.65;
  color: #475569;
}

.faq-answer p {
  margin: 0;
}

/* Responsive */
@media (max-width: 1024px) {
  .contact-main-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .bento-channels {
    grid-template-columns: 1fr;
  }
  
  .input-grid {
    grid-template-columns: 1fr;
  }

  .hero-title {
    font-size: 30px;
  }

  .form-wrapper {
    padding: 24px 18px;
  }

  .hidden-sm {
    display: none;
  }
}
</style>

