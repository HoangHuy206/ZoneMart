<script setup lang="ts">
import { ref, reactive, computed, nextTick, watch, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuth } from '../../composables/useAuth';
import { useCart } from '../../composables/useCart';
import { useToast } from '../../composables/useToast';
import { orderRealtimeService } from '../../services/orderRealtimeService';

const router = useRouter();
const auth = useAuth();
const cart = useCart();
const toast = useToast();

const isOpen = ref(false);
const userMessage = ref('');
const isLoading = ref(false);
const chatContainer = ref<HTMLElement | null>(null);
const textareaRef = ref<HTMLTextAreaElement | null>(null);

function adjustTextareaHeight() {
  nextTick(() => {
    if (textareaRef.value) {
      textareaRef.value.style.height = '38px';
      const scrollH = textareaRef.value.scrollHeight;
      if (scrollH > 38) {
        const newH = Math.min(scrollH, 110);
        textareaRef.value.style.height = `${newH}px`;
      }
    }
  });
}

watch(userMessage, () => {
  adjustTextareaHeight();
});

// Thu âm giọng nói gửi cho AI tư vấn (Web Speech API)
const isRecordingVoice = ref(false);
let speechRecognitionInstance: any = null;

function toggleVoiceRecording() {
  if (isRecordingVoice.value) {
    stopVoiceRecording(true);
  } else {
    startVoiceRecording();
  }
}

function startVoiceRecording() {
  const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
  if (!SpeechRecognition) {
    toast.warning("Trình duyệt hiện tại chưa hỗ trợ nhận diện giọng nói Web Speech. Bạn vui lòng sử dụng Google Chrome, Microsoft Edge hoặc Cốc Cốc để thu âm nhé!", "Trình Duyệt Không Hỗ Trợ");
    return;
  }

  try {
    if (speechRecognitionInstance) {
      try { speechRecognitionInstance.abort(); } catch (e) {}
    }

    const recognition = new SpeechRecognition();
    recognition.lang = 'vi-VN';
    recognition.interimResults = true;
    recognition.continuous = false;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => {
      isRecordingVoice.value = true;
    };

    recognition.onresult = (event: any) => {
      let interim = '';
      let final = '';
      for (let i = event.resultIndex; i < event.results.length; ++i) {
        if (event.results[i].isFinal) {
          final += event.results[i][0].transcript;
        } else {
          interim += event.results[i][0].transcript;
        }
      }
      const combined = (final || interim).trim();
      if (combined) {
        userMessage.value = combined;
        adjustTextareaHeight();
      }
    };

    recognition.onerror = (event: any) => {
      console.warn("Speech recognition error:", event.error);
      isRecordingVoice.value = false;
    };

    recognition.onend = () => {
      isRecordingVoice.value = false;
      if (userMessage.value.trim()) {
        sendMessage();
      }
    };

    speechRecognitionInstance = recognition;
    recognition.start();
  } catch (err) {
    console.error("Lỗi khi mở microphone:", err);
    isRecordingVoice.value = false;
  }
}

function stopVoiceRecording(andSend = true) {
  if (speechRecognitionInstance) {
    try {
      speechRecognitionInstance.stop();
    } catch (e) {}
  }
  isRecordingVoice.value = false;
  if (andSend && userMessage.value.trim()) {
    sendMessage();
  }
}

interface ChatMessage {
  id: string;
  sender: 'user' | 'ai';
  text: string;
  htmlText?: string;
  timestamp: string;
  provider?: string;
  products?: any[];
  stores?: any[];
  actionIntent?: string | null;
  qrCodeUrl?: string;
  totalAmount?: number;
  orderCode?: string;
  isPaid?: boolean;
  summaryData?: any;
  isPassword?: boolean;
  selectedTopic?: string;
}

function getCurrentTime(): string {
  const now = new Date();
  return now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

// Lấy vị trí / địa chỉ hiện tại của khách hàng
const userGpsAddress = ref<string>(localStorage.getItem('zonemart_gps_address') || '');
const isLocating = ref<boolean>(false);

const userLocation = computed(() => {
  if (userGpsAddress.value && userGpsAddress.value.trim()) return userGpsAddress.value.trim();
  const uAddr = (auth.currentUser.value as any)?.address;
  if (uAddr && typeof uAddr === 'string' && uAddr.trim()) return uAddr.trim();
  const saved = localStorage.getItem('zonemart_user_address');
  if (saved && saved.trim()) return saved.trim();
  return '';
});

// Câu chào động theo thời gian thực (Sáng/Trưa/Chiều/Tối) & tài khoản người dùng
const welcomeMessage = computed<ChatMessage>(() => {
  const isLogged = auth.isLoggedIn.value;
  const userName = auth.currentUser.value?.fullName?.trim();

  const currentHour = new Date().getHours();
  let timeGreeting = "buổi sáng tốt lành";
  let timeIcon = "☀️";
  if (currentHour >= 11 && currentHour < 13) {
    timeGreeting = "buổi trưa";
    timeIcon = "🌤️";
  } else if (currentHour >= 13 && currentHour < 18) {
    timeGreeting = "buổi chiều";
    timeIcon = "⛅";
  } else if (currentHour >= 18 || currentHour < 5) {
    timeGreeting = "buổi tối";
    timeIcon = "🌙";
  }

  if (isLogged && userName) {
    return {
      id: 'welcome-1',
      sender: 'ai',
      text: `Chào ${timeGreeting} ${timeIcon} ${userName}! Mình là trợ lý AI của ZoneMart, bạn cần mình tư vấn gì ạ ?`,
      htmlText: `Chào ${timeGreeting} ${timeIcon} <strong style="color: #D94E15; font-weight: 800;">${userName}</strong>! Mình là trợ lý AI của ZoneMart, bạn cần mình tư vấn gì ạ ?`,
      timestamp: getCurrentTime(),
      provider: 'ZoneMart AI Assistant'
    };
  } else {
    return {
      id: 'welcome-1',
      sender: 'ai',
      text: `Chào ${timeGreeting} ${timeIcon} bạn nhé! Mình là trợ lý AI của ZoneMart, bạn cần mình tư vấn gì ạ ?`,
      timestamp: getCurrentTime(),
      provider: 'ZoneMart AI Assistant'
    };
  }
});

// Đọc/Ghi/Xoá lịch sử chat lưu trong sessionStorage để giữ nguyên khi chuyển giữa các trang
function loadChatHistory(): ChatMessage[] {
  try {
    const saved = sessionStorage.getItem('zonemart_ai_chat_history');
    return saved ? JSON.parse(saved) : [];
  } catch {
    return [];
  }
}

function saveChatHistory(history: ChatMessage[]) {
  try {
    sessionStorage.setItem('zonemart_ai_chat_history', JSON.stringify(history));
  } catch {}
}

const userChatHistory = ref<ChatMessage[]>(loadChatHistory());

const registrationState = ref<string>('NONE');
const regDraft = ref<any>({});
const supportState = ref<string>('NONE');
const supportDraft = ref<any>({});
const selectedSupportTopic = ref<string>('');

const supportTopics = [
  { name: 'Kiểm tra đơn hàng', icon: 'bi-box-seam' },
  { name: 'Đổi trả & Hoàn tiền', icon: 'bi-arrow-left-right' },
  { name: 'Đối tác Gian hàng', icon: 'bi-shop' },
  { name: 'Đội ngũ Tài xế', icon: 'bi-bicycle' },
  { name: 'Đóng góp ý kiến', icon: 'bi-chat-heart' }
];

function chooseSupportTopic(msg: any, topicName: string) {
  msg.selectedTopic = topicName;
  selectedSupportTopic.value = topicName;
  sendMessage(topicName);
}

// Trạng thái nhập mật khẩu có icon con mắt ẩn/hiện
const isPasswordStep = computed(() => {
  const s = registrationState.value || '';
  return s.includes('PASSWORD') || s.includes('PASS');
});

const isConfirmPasswordStep = computed(() => {
  const s = registrationState.value || '';
  return s.includes('CONFIRM');
});

const showPasswordInput = ref(false);
const passwordInputRef = ref<HTMLInputElement | null>(null);

watch(registrationState, (newVal) => {
  if (newVal && (newVal.includes('PASSWORD') || newVal.includes('PASS'))) {
    showPasswordInput.value = false;
    nextTick(() => {
      passwordInputRef.value?.focus();
    });
  }
});

// Trạng thái mở/che mật khẩu trong bảng tổng kết tin nhắn
const revealedPasswords = reactive<Record<string, boolean>>({});
function toggleRevealPassword(msgId: string) {
  revealedPasswords[msgId] = !revealedPasswords[msgId];
}

function handleTextClick(e: MouseEvent, msgId: string) {
  const target = (e.target as HTMLElement).closest('.btn-eye-toggle-inline');
  if (target) {
    toggleRevealPassword(msgId);
  }
}

function formatMessageHtml(msg: ChatMessage): string {
  // 0. Xử lý tin nhắn người dùng nhập mật khẩu (hiển thị trực tiếp trong bong bóng chat rõ nét, không dùng hộp trắng đè lên)
  if (msg.sender === 'user' && msg.isPassword) {
    const isRevealed = revealedPasswords[msg.id] || false;
    const pwdText = msg.text || '';
    const maskedStr = '••••••••';
    const displayVal = isRevealed ? pwdText : maskedStr;
    return `<span class="user-pwd-plain">${displayVal}</span>`;
  }

  let text = msg.htmlText || msg.text || '';
  if (!text) return '';

  // 1. Xử lý mật khẩu masked [MASKED_PWD:...] trong tin nhắn của AI
  if (text.includes('[MASKED_PWD:')) {
    const isRevealed = revealedPasswords[msg.id] || false;
    text = text.replace(/\[MASKED_PWD:([^\]]+)\]/g, (_match, pwd) => {
      const displayVal = isRevealed ? pwd : '••••••••';
      const eyeIcon = isRevealed ? 'bi-eye' : 'bi-eye-slash';
      const title = isRevealed ? 'Bấm để ẩn mật khẩu' : 'Bấm để xem mật khẩu';
      return `<span class="inline-password-tag"><code>${displayVal}</code> <button type="button" class="btn-eye-toggle-inline" title="${title}"><i class="bi ${eyeIcon}"></i></button></span>`;
    });
  }

  // 2. Chuẩn hoá các ký tự xuống dòng CRLF -> LF và gộp bớt khoảng cách trống dư thừa
  text = text.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
  text = text.replace(/\n\s*\n\s*\n+/g, '\n\n');
  text = text.trim();

  // 3. Đường kẻ phân cách --- chuyển thành thẻ <hr class="chat-divider"/>
  text = text.replace(/(?:^|\n)\s*---\s*(?:\n|$)/g, '\n<hr class="chat-divider"/>\n');

  // 4. Render định dạng Markdown cơ bản (in đậm **, code ``, in nghiêng *)
  text = text.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  text = text.replace(/`([^`]+)`/g, '<code class="chat-code">$1</code>');

  // 4a. Format câu luồng hỏi (thay thế emoji số hoặc icon bước bằng huy hiệu code Bootstrap Icons đẹp mắt)
  text = text.replace(/(?:[0-9]️⃣|🔟|📌|👤|📧|📞|📦|📝|🖼️)*\s*(?:<strong>)?(?:\*\*)?(?:Câu|Bước)\s*(\d+)\s*(?:\/|trên)\s*(\d+)(?:\s*\(([^)]+)\))?:?(?:\*\*)?(?:<\/strong>)?/gi, (_m, qNum, total, sub) => {
    const subText = sub ? ` (${sub})` : '';
    return `<span class="q-step-badge"><i class="bi bi-patch-question-fill me-1"></i> Bước ${qNum} / ${total}${subText}:</span>`;
  });
  text = text.replace(/(?:[0-9]️⃣|🔟|📌|👤|📧|📞|📦|📝|🖼️)*\s*(?:<strong>)?(?:\*\*)?Bước\s*7(?:\s*\(([^)]+)\))?:?(?:\*\*)?(?:<\/strong>)?(?:\s*Bạn có muốn gửi kèm Hình Ảnh Minh Họa)?/gi, (_m, sub) => {
    const subText = sub ? ` (${sub})` : ' (Ảnh sự cố)';
    return `<span class="q-step-badge"><i class="bi bi-camera-fill me-1"></i> Bước 7${subText}:</span> Bạn có muốn gửi kèm Hình Ảnh Minh Họa`;
  });

  // 4b. Format các cách gửi hỗ trợ / đăng ký (Cách 1, Cách 2) thành icon code Bootstrap đẹp mắt
  text = text.replace(/1️⃣\s*(?:\*\*)?Cách 1:?([^*:\n<]+)(?:\*\*)?:?/gi, '<div class="method-step-item"><span class="method-code-icon" style="color: #EA580C; background: #FFF7ED;"><i class="bi bi-journal-text"></i></span><strong>Cách 1:$1</strong></div>');
  text = text.replace(/2️⃣\s*(?:\*\*)?Cách 2:?([^*:\n<]+)(?:\*\*)?:?/gi, '<div class="method-step-item"><span class="method-code-icon" style="color: #6366F1; background: #EEF2FF;"><i class="bi bi-lightning-charge-fill"></i></span><strong>Cách 2:$1</strong></div>');

  // 4c. Format tiêu đề bảng tổng hợp và tiêu đề hoàn tất thành badge header sang trọng
  text = text.replace(/📋\s*(?:\*\*)?BẢNG TỔNG HỢP YÊU CẦU HỖ TRỢ TRỰC TUYẾN(?:\*\*)?/gi, '<div class="summary-table-header"><i class="bi bi-clipboard-data-fill me-2" style="color: #6366F1;"></i>BẢNG TỔNG HỢP YÊU CẦU HỖ TRỢ TRỰC TUYẾN</div>');
  text = text.replace(/🎉\s*(?:\*\*)?ZoneMart đã tiếp nhận yêu cầu hỗ trợ của bạn thành công!(?:\*\*)?/gi, '<div class="summary-table-header success-header"><i class="bi bi-check-circle-fill me-2" style="color: #10B981;"></i>ZoneMart đã tiếp nhận yêu cầu hỗ trợ của bạn thành công!</div>');

  // 4d. Format các hàng tổng kết thông tin hồ sơ có icon thành container code icon riêng biệt, sang trọng giống icon ngân hàng
  const summaryIconMap: Record<string, { icon: string; color: string; bg: string }> = {
    '🏪': { icon: 'bi-shop', color: '#EA580C', bg: '#FFF7ED' },
    '📍': { icon: 'bi-geo-alt-fill', color: '#EF4444', bg: '#FEF2F2' },
    '⏰': { icon: 'bi-clock-fill', color: '#F59E0B', bg: '#FFFBEB' },
    '👤': { icon: 'bi-person-fill', color: '#2563EB', bg: '#EFF6FF' },
    '🆔': { icon: 'bi-person-badge-fill', color: '#7C3AED', bg: '#F5F3FF' },
    '🪪': { icon: 'bi-person-badge-fill', color: '#7C3AED', bg: '#F5F3FF' },
    '📄': { icon: 'bi-file-earmark-text-fill', color: '#0284C7', bg: '#F0F9FF' },
    '📜': { icon: 'bi-file-earmark-check-fill', color: '#0D9488', bg: '#F0FDFA' },
    '💳': { icon: 'bi-bank2', color: '#10B981', bg: '#ECFDF5' },
    '🔑': { icon: 'bi-key-fill', color: '#D97706', bg: '#FEFCE8' },
    '🛵': { icon: 'bi-truck', color: '#0284C7', bg: '#F0F9FF' },
    '🏍️': { icon: 'bi-bicycle', color: '#0284C7', bg: '#F0F9FF' },
    '📧': { icon: 'bi-envelope-fill', color: '#DB2777', bg: '#FDF2F8' },
    '📋': { icon: 'bi-clipboard-check-fill', color: '#059669', bg: '#ECFDF5' },
    '✅': { icon: 'bi-check-circle-fill', color: '#10B981', bg: '#ECFDF5' },
    '⏳': { icon: 'bi-hourglass-split', color: '#EA580C', bg: '#FFF7ED' },
    '🎉': { icon: 'bi-award-fill', color: '#F59E0B', bg: '#FFFBEB' },
    '📌': { icon: 'bi-pin-angle-fill', color: '#EF4444', bg: '#FEF2F2' },
    '🎫': { icon: 'bi-ticket-detailed-fill', color: '#6366F1', bg: '#EEF2FF' },
    '📞': { icon: 'bi-telephone-fill', color: '#10B981', bg: '#ECFDF5' },
    '📦': { icon: 'bi-box-seam-fill', color: '#8B5CF6', bg: '#F5F3FF' },
    '📝': { icon: 'bi-pencil-fill', color: '#F59E0B', bg: '#FFFBEB' },
    '🖼️': { icon: 'bi-image-fill', color: '#06B6D4', bg: '#ECFEFF' }
  };

  const lines = text.split('\n');
  for (let i = 0; i < lines.length; i++) {
    let rawLine = lines[i].trim();
    if (rawLine.startsWith('- ') || rawLine.startsWith('* ')) {
      rawLine = rawLine.substring(2).trim();
    }
    // Tuyệt đối không bọc các dòng câu hỏi luồng bước hỏi thành summary-line-item
    if (rawLine.includes('q-step-badge') || rawLine.includes('Bước') || rawLine.includes('Câu')) {
      continue;
    }
    const matchedKey = Object.keys(summaryIconMap).find(k => rawLine.startsWith(k));
    if (matchedKey) {
      const rest = rawLine.slice(matchedKey.length).trim();
      const cfg = summaryIconMap[matchedKey];
      lines[i] = `<div class="summary-line-item"><span class="summary-code-icon" style="color: ${cfg.color}; background: ${cfg.bg};"><i class="bi ${cfg.icon}"></i></span><span class="summary-line-text">${rest}</span></div>`;
    }
  }
  text = lines.join('\n');

  // 4e. Thay thế các icon emoji trong các câu phản hồi thông báo thành icon code HTML Bootstrap đẹp mắt
  text = text.replace(/! 🛵/g, '! <i class="bi bi-truck code-inline-icon" style="color: #0284C7;"></i>');
  text = text.replace(/! 🏍️/g, '! <i class="bi bi-bicycle code-inline-icon" style="color: #0284C7;"></i>');
  text = text.replace(/! 📍/g, '! <i class="bi bi-geo-alt-fill code-inline-icon" style="color: #EF4444;"></i>');
  text = text.replace(/! 💳/g, '! <i class="bi bi-bank2 code-inline-icon" style="color: #10B981;"></i>');
  text = text.replace(/! 📜/g, '! <i class="bi bi-file-earmark-check-fill code-inline-icon" style="color: #10B981;"></i>');
  text = text.replace(/! 🆔/g, '! <i class="bi bi-person-badge-fill code-inline-icon" style="color: #7C3AED;"></i>');
  text = text.replace(/! 👤/g, '! <i class="bi bi-person-fill code-inline-icon" style="color: #2563EB;"></i>');
  text = text.replace(/! 🏪/g, '! <i class="bi bi-shop code-inline-icon" style="color: #EA580C;"></i>');
  text = text.replace(/! 📝/g, '! <i class="bi bi-journal-text code-inline-icon" style="color: #0284C7;"></i>');
  text = text.replace(/! 🎧/g, '! <i class="bi bi-headset code-inline-icon" style="color: #6366F1;"></i>');
  text = text.replace(/! 🎫/g, '! <i class="bi bi-ticket-detailed-fill code-inline-icon" style="color: #6366F1;"></i>');
  text = text.replace(/! 📌/g, '! <i class="bi bi-pin-angle-fill code-inline-icon" style="color: #EF4444;"></i>');
  text = text.replace(/💡 \*\(/g, '<i class="bi bi-lightbulb-fill me-1" style="color: #F59E0B;"></i> *(');
  text = text.replace(/✅/g, '<i class="bi bi-check-circle-fill code-inline-icon" style="color: #10B981;"></i>');
  text = text.replace(/⚠️/g, '<i class="bi bi-exclamation-triangle-fill code-inline-icon" style="color: #F59E0B;"></i>');
  text = text.replace(/👉/g, '<i class="bi bi-arrow-right-circle-fill code-inline-icon" style="color: #EA580C;"></i>');
  text = text.replace(/🚀/g, '<i class="bi bi-send-fill code-inline-icon" style="color: #10B981;"></i>');
  text = text.replace(/📸/g, '<i class="bi bi-camera-fill code-inline-icon" style="color: #0284C7;"></i>');
  text = text.replace(/⏭️/g, '<i class="bi bi-skip-forward-circle-fill code-inline-icon" style="color: #64748B;"></i>');
  text = text.replace(/❌/g, '<i class="bi bi-x-circle-fill code-inline-icon" style="color: #EF4444;"></i>');

  // 5. Chuyển đổi \n thành <br/> duy nhất (không bị lặp dòng)
  text = text.replace(/\n/g, '<br/>');

  // 6. Xoá bỏ <br/> thừa thãi bao quanh đường kẻ ngang <hr> và các hàng tổng kết
  text = text.replace(/<br\s*\/?>\s*(<hr[^>]*>)\s*<br\s*\/?>/gi, '$1');
  text = text.replace(/(<hr[^>]*>)\s*<br\s*\/?>/gi, '$1');
  text = text.replace(/<br\s*\/?>\s*(<hr[^>]*>)/gi, '$1');
  text = text.replace(/<br\s*\/?>\s*(<div class="summary-line-item">)/gi, '$1');
  text = text.replace(/(<\/div>)\s*<br\s*\/?>/gi, '$1');

  return text;
}

// Danh sách danh mục, khung giờ, ngân hàng, xe, khu vực cho AI chọn trực tiếp
const sellerCategories = [
  "Thực phẩm & Nhu yếu phẩm",
  "Trái cây & Đồ uống",
  "Cơm & Thức ăn nhanh",
  "Đồ gia dụng tiện ích",
  "Thời trang & Phụ kiện",
  "Mỹ phẩm & Chăm sóc cá nhân"
];

const sellerOpenHoursList = [
  "07:00 - 22:00 (Mở cả ngày & Buổi tối)",
  "06:00 - 21:00 (Mở sớm - Đóng sớm)",
  "08:00 - 20:00 (Giờ hành chính)",
  "07:00 - 23:00 (Phục vụ đêm)",
  "24/24 (Mở cửa 24/7 toàn thời gian)",
  "Tùy chỉnh khung giờ..."
];

const banksList = [
  "Vietcombank (VCB)",
  "MB Bank (MB)",
  "Techcombank (TCB)",
  "VietinBank (CTG)",
  "BIDV",
  "VPBank",
  "Agribank",
  "TPBank",
  "ACB",
  "Sacombank"
];

const shipperVehicleTypes = ["Xe máy xăng", "Xe máy điện"];

const shipperGasModels = [
  "Honda Wave Alpha",
  "Honda Vision",
  "Honda Air Blade",
  "Honda SH Mode",
  "Honda SH 125i/150i",
  "Honda Winner X",
  "Yamaha Exciter 150/155",
  "Yamaha Sirius"
];

const shipperElectricModels = [
  "VinFast Feliz S",
  "VinFast Klara S",
  "VinFast Evo200",
  "Dat Bike Quantum"
];

const shipperAreas = [
  "Quận Cầu Giấy",
  "Quận Đống Đa",
  "Quận Ba Đình",
  "Quận Thanh Xuân",
  "Quận Hai Bà Trưng",
  "Quận Hoàng Mai",
  "Quận Nam Từ Liêm",
  "Quận Bắc Từ Liêm",
  "Quận Hà Đông",
  "Quận Tây Hồ",
  "Quận Hoàn Kiếm",
  "Quận Long Biên",
  "Thị xã Sơn Tây",
  "Huyện Đông Anh",
  "Huyện Gia Lâm",
  "Huyện Thanh Trì",
  "Huyện Hoài Đức",
  "Huyện Đan Phượng",
  "Huyện Thường Tín",
  "Huyện Sóc Sơn",
  "Huyện Mê Linh",
  "Huyện Quốc Oai",
  "Huyện Thạch Thất",
  "Huyện Chương Mỹ",
  "Huyện Ba Vì",
  "Huyện Phú Xuyên",
  "Huyện Ứng Hòa",
  "Huyện Mỹ Đức",
  "Tất cả các quận, xã"
];

// Xử lý tải ảnh trong đoạn chat AI
const uploadedPreviews = reactive<Record<string, string>>({});
const uploadedCounts = reactive<Record<string, number>>({});
const activeUploadIntent = ref<string>('');
const fileUploadInputRef = ref<HTMLInputElement | null>(null);

function isUploadIntent(intent?: string | null): boolean {
  if (!intent) return false;
  return intent.includes('UPLOAD');
}

function getUploadTitle(intent?: string | null): string {
  switch (intent) {
    case 'SELLER_UPLOAD_CCCD_FRONT':
    case 'SHIPPER_UPLOAD_CCCD_FRONT':
      return 'CCCD Mặt Trước *';
    case 'SELLER_UPLOAD_CCCD_BACK':
    case 'SHIPPER_UPLOAD_CCCD_BACK':
      return 'CCCD Mặt Sau *';
    case 'SELLER_UPLOAD_FOOD_SAFETY':
      return 'Giấy CN An Toàn Thực Phẩm *';
    case 'SHIPPER_UPLOAD_PORTRAIT':
      return 'Ảnh Chân Dung Tài Xế *';
    case 'SHIPPER_UPLOAD_GPLX':
      return 'Bằng Lái Xe (GPLX) *';
    case 'SUPPORT_UPLOAD_ATTACHMENT':
      return 'Ảnh Minh Họa Sự Cố (Tùy chọn)';
    default:
      return 'Tải Ảnh Lên *';
  }
}

function getUploadSubtitle(intent?: string | null): string {
  switch (intent) {
    case 'SELLER_UPLOAD_CCCD_FRONT':
    case 'SHIPPER_UPLOAD_CCCD_FRONT':
      return 'Nhấp để tải ảnh mặt trước';
    case 'SELLER_UPLOAD_CCCD_BACK':
    case 'SHIPPER_UPLOAD_CCCD_BACK':
      return 'Nhấp để tải ảnh mặt sau';
    case 'SELLER_UPLOAD_FOOD_SAFETY':
      return 'Nhấp để tải ảnh Giấy CN ATTP / GPKD';
    case 'SHIPPER_UPLOAD_PORTRAIT':
      return 'Nhấp để tải ảnh chân dung';
    case 'SHIPPER_UPLOAD_GPLX':
      return 'Nhấp để tải ảnh bằng lái';
    case 'SUPPORT_UPLOAD_ATTACHMENT':
      return 'Nhấp để tải ảnh minh họa sự cố / đơn hàng';
    default:
      return 'Nhấp để tải ảnh lên';
  }
}

function triggerUploadForIntent(intent: string) {
  activeUploadIntent.value = intent;
  fileUploadInputRef.value?.click();
}

function handleFileUploadChange(e: Event) {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = (event) => {
    const dataUrl = event.target?.result as string;
    const intent = activeUploadIntent.value;

    // Kiểm tra xem intent ảnh này đã từng được tải lên trước đó chưa
    const isReupload = !!uploadedPreviews[intent] || (uploadedCounts[intent] || 0) > 0;
    uploadedCounts[intent] = (uploadedCounts[intent] || 0) + 1;
    uploadedPreviews[intent] = dataUrl;

    // Lưu ảnh thật (chuỗi base64 data URL) vào regDraft để gửi lên Server lưu MongoDB
    if (!regDraft.value) regDraft.value = {};
    if (intent === 'SELLER_UPLOAD_CCCD_FRONT' || intent === 'SHIPPER_UPLOAD_CCCD_FRONT') {
      regDraft.value.cccdFrontImage = dataUrl;
    } else if (intent === 'SELLER_UPLOAD_CCCD_BACK' || intent === 'SHIPPER_UPLOAD_CCCD_BACK') {
      regDraft.value.cccdBackImage = dataUrl;
    } else if (intent === 'SELLER_UPLOAD_FOOD_SAFETY') {
      regDraft.value.foodSafetyImage = dataUrl;
    } else if (intent === 'SHIPPER_UPLOAD_PORTRAIT') {
      regDraft.value.portraitImage = dataUrl;
    } else if (intent === 'SHIPPER_UPLOAD_GPLX') {
      regDraft.value.drivingLicenseImage = dataUrl;
    } else if (intent === 'SUPPORT_UPLOAD_ATTACHMENT' || intent === 'SUPPORT_EDIT_UPLOAD') {
      if (!supportDraft.value) supportDraft.value = {};
      supportDraft.value.AttachedImage = dataUrl;
    }

    const title = getUploadTitle(intent).replace(' *', '');
    const uploadText = isReupload
      ? `[Ảnh ${title} đã tải lên một lần nữa thành công]`
      : `[Ảnh ${title} đã tải lên thành công]`;

    sendMessage(uploadText);
    input.value = '';
  };
  reader.readAsDataURL(file);
}

// Thông tin tài khoản hiện tại đã đăng nhập để dùng luôn cho hỗ trợ
const currentLoggedInSupportUser = computed(() => {
  const u = auth.currentUser.value;
  if (!u) return null;
  const name = u.fullName?.trim();
  const email = (u as any).email?.trim() || (u.phoneEmail?.includes('@') ? u.phoneEmail.trim() : '');
  if (!name && !email) return null;
  return {
    fullName: name || 'Khách Hàng',
    email: email || 'Chưa liên kết email'
  };
});

function useCurrentAccountInfo(user: { fullName: string; email: string }) {
  sendMessage(`[DÙNG THÔNG TIN TÀI KHOẢN]: ${user.fullName} | ${user.email}`);
}
async function requestBrowserLocation(forNearbyStores: boolean = false, radiusKm: number = 5, forOrderAddress: boolean = false) {
  if (!navigator.geolocation) {
    userChatHistory.value.push({
      id: Date.now().toString(),
      sender: 'ai',
      text: '⚠️ Trình duyệt của bạn không hỗ trợ định vị GPS. Bạn có thể nhập trực tiếp địa chỉ hiện tại vào ô chat để mình hỗ trợ nhé!',
      timestamp: getCurrentTime(),
      provider: 'ZoneMart AI Assistant'
    });
    return;
  }

  isLocating.value = true;
  navigator.geolocation.getCurrentPosition(
    async (pos) => {
      try {
        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;
        let resolvedAddr = '';
        try {
          const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`, {
            headers: { 'Accept-Language': 'vi' }
          });
          if (res.ok) {
            const geo = await res.json();
            resolvedAddr = geo.display_name || geo.address?.suburb || geo.address?.city || '';
          }
        } catch {}

        if (!resolvedAddr) {
          resolvedAddr = `Tọa độ GPS (${lat.toFixed(4)}, ${lng.toFixed(4)})`;
        }

        userGpsAddress.value = resolvedAddr;
        localStorage.setItem('zonemart_gps_address', resolvedAddr);
        localStorage.setItem('zonemart_user_address', resolvedAddr);

        if (forOrderAddress) {
          await sendMessage(resolvedAddr);
        } else if (forNearbyStores) {
          await sendMessage(`[TÌM QUÁN BÁN KÍNH ${radiusKm}KM]: Vị trí của tôi là ${resolvedAddr}`);
        } else {
          await sendMessage(`[VỊ TRÍ GPS]: Tôi đang ở ${resolvedAddr}`);
        }
      } catch (e) {
        userChatHistory.value.push({
          id: Date.now().toString(),
          sender: 'ai',
          text: '⚠️ Không thể giải mã tên đường từ GPS. Bạn vui lòng gõ trực tiếp tên đường/quận vào đây để mình hỗ trợ nhé!',
          timestamp: getCurrentTime(),
          provider: 'ZoneMart AI Assistant'
        });
      } finally {
        isLocating.value = false;
        scrollToBottom();
      }
    },
    (_err) => {
      isLocating.value = false;
      const errorMsg = forOrderAddress
        ? 'Oh no bạn không cho phép mình thì sẽ không biết được vị trí hiện tại của bạn đâu ạ huhu 😢 Bạn vui lòng nhập trực tiếp tên đường/số nhà vào ô chat để mình giao đồ cho bạn nhé! 😊'
        : 'Oh no bạn không cho phép mình thì sẽ không biết được vị trí hiện tại của bạn đâu ạ huhu 😢 Bạn có thể bấm vào biểu tượng cài đặt/ổ khóa 🔒 trên thanh địa chỉ của trình duyệt, chọn "Cho phép truy cập vị trí" (Location: Allow) rồi thử lại, hoặc bạn có thể gõ trực tiếp tên đường/quận vào đây để mình hỗ trợ nhé! 😊';

      userChatHistory.value.push({
        id: Date.now().toString(),
        sender: 'ai',
        text: errorMsg,
        timestamp: getCurrentTime(),
        provider: 'ZoneMart AI Assistant'
      });
      scrollToBottom();
    },
    { timeout: 10000, enableHighAccuracy: true }
  );
}

async function findNearbyStores(radiusKm: number = 5) {
  const currentAddress = userGpsAddress.value || userLocation.value || localStorage.getItem('zonemart_gps_address') || localStorage.getItem('zonemart_user_address');
  const hasRealAddress = currentAddress && 
                         currentAddress !== 'Khu vực Cầu Giấy / Hà Nội' && 
                         currentAddress !== 'Chưa xác định cụ thể';

  if (hasRealAddress) {
    await sendMessage(`[TÌM QUÁN BÁN KÍNH ${radiusKm}KM]: Vị trí của tôi là ${currentAddress}`);
    return;
  }

  // Tự động hỏi quyền truy cập vị trí của web ngay lập tức mà không cần bấm thủ công
  userChatHistory.value.push({
    id: Date.now().toString(),
    sender: 'user',
    text: `Tìm quán ngon bán kính ${radiusKm}km gần vị trí của tôi`,
    timestamp: getCurrentTime()
  });
  scrollToBottom();

  requestBrowserLocation(true, radiusKm);
}
const orderState = ref<{
  step: string;
  cartItems: any[];
  pendingItem: any;
  address?: string;
  shippingMethod?: string;
  shippingFee?: number;
  createdOrderCode?: string;
  paymentMethod?: string;
  isPaid?: boolean;
}>({
  step: 'IDLE',
  cartItems: [],
  pendingItem: null
});

// Helper trích xuất mã đơn hàng từ tin nhắn (hỗ trợ cả qrCodeUrl, text và orderCode)
function extractOrderCodeFromMsg(m: any): string {
  if (!m) return '';
  if (m.orderCode && typeof m.orderCode === 'string' && m.orderCode.trim()) {
    return m.orderCode.trim().toUpperCase();
  }
  if (m.qrCodeUrl && typeof m.qrCodeUrl === 'string') {
    const match = m.qrCodeUrl.match(/addInfo=([A-Z0-9_-]+)/i) || m.qrCodeUrl.match(/(ZM\d{4,8})/i);
    if (match) return match[1].toUpperCase();
  }
  if (m.text && typeof m.text === 'string') {
    const match = m.text.match(/(ZM\d{4,8})/i);
    if (match) return match[1].toUpperCase();
  }
  return '';
}

// ================================================================
// QUẢN LÝ THANH TOÁN QR THỜI GIAN THỰC TỪ TRỢ LÝ AI (GIỐNG CHECKOUT THỦ CÔNG)
// ================================================================
const currentAiOrderCode = ref<string>('');
const currentAiQrTotal = ref<number>(0);
const isAiPaymentConfirmed = ref<boolean>(false);
const aiRedirectCountdown = ref<number>(3);
let aiRedirectInterval: any = null;
let aiPaymentPollInterval: any = null;
let aiSseSource: EventSource | null = null;
let aiVietQrSocket: WebSocket | null = null;
let aiBroadcastChannel: BroadcastChannel | null = null;
let aiStorageListener: ((e: StorageEvent) => void) | null = null;
let aiMessageListener: ((e: MessageEvent) => void) | null = null;

// Âm thanh chúc mừng thanh toán thành công (chuẩn AudioContext như CheckoutView)
const playPaymentSuccessChime = () => {
  try {
    const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;

    const notes = [
      { freq: 523.25, time: 0, dur: 0.12 },
      { freq: 659.25, time: 0.12, dur: 0.12 },
      { freq: 783.99, time: 0.24, dur: 0.14 },
      { freq: 1046.50, time: 0.38, dur: 0.5 }
    ];

    notes.forEach(n => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = "triangle";
      osc.frequency.setValueAtTime(n.freq, now + n.time);
      gain.gain.setValueAtTime(0.25, now + n.time);
      gain.gain.exponentialRampToValueAtTime(0.001, now + n.time + n.dur);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start(now + n.time);
      osc.stop(now + n.time + n.dur);
    });
  } catch (e) {}
};

// Đăng ký thông tin đơn hàng lên server trung gian
const syncAiOrderToServer = async (code: string, total: number, items: any[]) => {
  try {
    await fetch('/api/payment/register-order', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        code,
        total,
        buyerName: auth.currentUser.value?.fullName || auth.currentUser.value?.phoneEmail || 'Khách hàng AI ZoneMart',
        items: (items || []).map(i => ({
          name: i.productName || i.name || 'Sản phẩm ZoneMart',
          price: Number(i.price) || 0,
          quantity: Number(i.quantity) || 1,
          image: i.imageUrl || i.image || '/images/anhxoanen.png',
          shop: i.storeName || i.shop || 'ZoneMart Store'
        }))
      })
    });
  } catch (e) {}
};

// Dừng toàn bộ các kết nối lắng nghe
const stopAiPaymentListener = () => {
  if (aiPaymentPollInterval) {
    clearInterval(aiPaymentPollInterval);
    aiPaymentPollInterval = null;
  }
  if (aiSseSource) {
    try { aiSseSource.close(); } catch(e) {}
    aiSseSource = null;
  }
  if (aiVietQrSocket) {
    try { aiVietQrSocket.close(); } catch(e) {}
    aiVietQrSocket = null;
  }
  if (aiBroadcastChannel) {
    try { aiBroadcastChannel.close(); } catch(e) {}
    aiBroadcastChannel = null;
  }
  if (aiStorageListener && typeof window !== 'undefined') {
    window.removeEventListener('storage', aiStorageListener);
    aiStorageListener = null;
  }
  if (aiMessageListener && typeof window !== 'undefined') {
    window.removeEventListener('message', aiMessageListener);
    aiMessageListener = null;
  }
};

// Chuyển hướng tới trang Đơn mua của tôi
const goToBuyerOrdersDirectly = () => {
  stopAiPaymentListener();
  if (aiRedirectInterval) {
    clearInterval(aiRedirectInterval);
    aiRedirectInterval = null;
  }
  orderState.value = { step: 'IDLE', cartItems: [], pendingItem: null };
  isOpen.value = false;
  router.push('/buyer-orders').catch(() => {
    window.location.href = '/buyer-orders';
  });
};

// Khi tiền thực sự vào tài khoản TPBank -> Kích hoạt hiệu ứng thành công, lưu đơn & chuyển trang sau 3 giây
const onAiPaymentConfirmed = async (paidAmount?: number) => {
  if (isAiPaymentConfirmed.value) return;
  isAiPaymentConfirmed.value = true;
  stopAiPaymentListener();

  let code = currentAiOrderCode.value;
  if (!code) {
    const lastQr = [...userChatHistory.value].reverse().find(m => extractOrderCodeFromMsg(m));
    if (lastQr) code = extractOrderCodeFromMsg(lastQr);
  }
  if (!code && (orderState.value as any)?.createdOrderCode) {
    code = (orderState.value as any).createdOrderCode;
  }
  if (!code) {
    code = 'ZM' + Math.floor(100000 + Math.random() * 900000);
  }
  code = code.trim().toUpperCase();
  currentAiOrderCode.value = code;

  const finalAmount = paidAmount || currentAiQrTotal.value || 40000;
  const now = new Date();
  const dateStr = now.toLocaleDateString("vi-VN") + " " + now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

  // Đánh dấu tất cả tin nhắn QR trong khung chat là Đã thanh toán và xóa để QR biến mất hoàn toàn
  userChatHistory.value.forEach(m => {
    if (m.actionIntent === 'SHOW_QR_PAYMENT' || extractOrderCodeFromMsg(m) === code || m.qrCodeUrl) {
      m.isPaid = true;
      m.actionIntent = null;
      m.qrCodeUrl = undefined;
    }
  });
  saveChatHistory(userChatHistory.value);

  const cartItemsList = (orderState.value.cartItems && orderState.value.cartItems.length > 0)
    ? orderState.value.cartItems
    : [
        {
          productId: 'prod_ai_default',
          productName: 'Đơn hàng thực phẩm AI ZoneMart',
          price: finalAmount,
          quantity: 1,
          imageUrl: '/images/anhxoanen.png',
          storeName: 'Vườn Rau Ba Vì (Cầu Giấy)'
        }
      ];

  const formattedItems = cartItemsList.map((item: any) => ({
    id: item.productId || item.id || ('prod_' + Date.now()),
    productId: item.productId || item.id || ('prod_' + Date.now()),
    name: item.productName || item.name || 'Sản phẩm ZoneMart',
    price: Number(item.price) || 0,
    quantity: Number(item.quantity) || 1,
    image: item.imageUrl || item.image || '/images/anhxoanen.png',
    shop: item.storeName || item.shop || 'Vườn Rau Ba Vì (Cầu Giấy)'
  }));

  const buyerName = auth.currentUser.value?.fullName || auth.currentUser.value?.phoneEmail || 'Khách Hàng ZoneMart';
  const buyerPhone = (auth.currentUser.value as any)?.phone || auth.currentUser.value?.phoneEmail || '0988 888 888';
  const shippingAddress = orderState.value.address || userLocation.value || 'Số 48 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội';
  const shippingMethod = orderState.value.shippingMethod || 'Giao Hàng Hỏa Tốc (15-25 phút)';
  const firstShopName = formattedItems[0]?.shop || 'Vườn Rau Ba Vì (Cầu Giấy)';

  const newOrder = {
    id: code,
    orderId: code,
    orderCode: code,
    date: dateStr,
    createdAt: dateStr,
    total: finalAmount,
    totalAmount: finalAmount,
    status: 'Đã thanh toán',
    orderStatus: 'Processing',
    paymentStatus: 'Paid',
    isPaid: true,
    itemsCount: formattedItems.length,
    items: formattedItems,
    shippingAddress,
    recipientName: buyerName,
    recipientPhone: buyerPhone,
    deliveryNote: 'Đặt hàng qua Trợ Lý AI Chuyên Gia ZoneMart (Đã quét QR nhận tiền thực tế)',
    shippingMethod,
    paymentMethod: 'Chuyển khoản QR',
    paidAt: new Date().toISOString(),
    location: { lat: 21.0333, lng: 105.7983 }
  };

  // 1. Lưu vào Profile Orders của User & Guest theo tất cả candidate keys
  const acc = auth.currentUser.value;
  const candidateKeys = new Set<string>();
  if (acc) {
    if (acc.phoneEmail) candidateKeys.add("zonemart_profile_orders_" + acc.phoneEmail.toLowerCase().trim());
    if (acc.id) candidateKeys.add("zonemart_profile_orders_" + acc.id.toLowerCase().trim());
    if ((acc as any).phone) candidateKeys.add("zonemart_profile_orders_" + (acc as any).phone.toLowerCase().trim());
  }
  candidateKeys.add("zonemart_profile_orders_guest");

  candidateKeys.forEach(k => {
    try {
      const existing = JSON.parse(localStorage.getItem(k) || '[]');
      existing.unshift(newOrder);
      localStorage.setItem(k, JSON.stringify(existing));
    } catch (e) {}
  });

  // 2. Tích điểm thưởng
  const ownerKey = (acc?.phoneEmail || acc?.id || 'guest').toLowerCase().trim();
  const profileKey = 'zonemart_profile_data_' + ownerKey;
  try {
    const profile = JSON.parse(localStorage.getItem(profileKey) || '{}');
    const earnedPoints = Math.round(finalAmount / 1000);
    profile.points = (profile.points || 0) + earnedPoints;
    localStorage.setItem(profileKey, JSON.stringify(profile));
  } catch (e) {}

  // 3. Xoá giỏ hàng
  try {
    cart.clearCart();
  } catch (e) {}

  // 4. Phát tán sự kiện nổ đơn thời gian thực tới Shipper & Seller
  try {
    orderRealtimeService.dispatchNewOrder({
      id: code,
      orderCode: code,
      total: finalAmount,
      shippingFee: (orderState.value as any).shippingFee || 15000,
      paymentMethod: 'Chuyển khoản QR',
      createdAt: dateStr,
      status: 'pending',
      statusText: 'Đang chờ tài xế nhận đơn',
      customer: {
        lat: 21.0333,
        lng: 105.7983,
        name: buyerName,
        phone: buyerPhone,
        address: shippingAddress
      },
      store: {
        lat: 21.0345,
        lng: 105.7960,
        name: firstShopName,
        address: 'Số 48 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội',
        phone: '0988 123 456'
      },
      items: formattedItems
    });
  } catch (e) {
    console.warn('Lỗi phát đơn thời gian thực:', e);
  }

  // 5. Cập nhật Backend Database và Cache
  try {
    await fetch('http://localhost:5000/api/aichat/confirm-bank-transfer', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ orderCode: code, amount: finalAmount })
    });
  } catch (e) {}

  // 6. Phát âm thanh chúc mừng & tin nhắn trong Chat (nếu chưa có tin chúc mừng)
  playPaymentSuccessChime();

  const hasSuccessMsg = userChatHistory.value.some(m => m.text?.includes('Bạn đã thanh toán thành công') || m.text?.includes('đã nhận được tiền'));
  if (!hasSuccessMsg) {
    const successAiMsg: ChatMessage = {
      id: (Date.now() + 2).toString(),
      sender: 'ai',
      text: 'Bạn đã thanh toán thành công 3 giây nữa sẽ chuyển qua trang trạng thái đơn hàng ạ',
      timestamp: getCurrentTime(),
      provider: 'ZoneMart AI Assistant'
    };
    userChatHistory.value.push(successAiMsg);
    saveChatHistory(userChatHistory.value);
    scrollToBottom();
  }

  // 7. Đếm ngược 3 giây chuyển qua trang /buyer-orders
  aiRedirectCountdown.value = 3;
  if (aiRedirectInterval) clearInterval(aiRedirectInterval);
  aiRedirectInterval = setInterval(() => {
    aiRedirectCountdown.value--;
    if (aiRedirectCountdown.value <= 0) {
      clearInterval(aiRedirectInterval);
      aiRedirectInterval = null;
      goToBuyerOrdersDirectly();
    }
  }, 1000);
};

// Khi người dùng chọn thanh toán Tiền Mặt (COD) -> Lưu đơn hàng và 3 giây sau tự động chuyển qua trang đơn hàng
const onAiCashOrderConfirmed = async (createdOrder?: any, totalAmount?: number) => {
  stopAiPaymentListener();

  let code = createdOrder?.orderId || createdOrder?.orderCode || currentAiOrderCode.value;
  if (!code && (orderState.value as any)?.createdOrderCode) {
    code = (orderState.value as any).createdOrderCode;
  }
  if (!code) {
    code = 'ZM' + Math.floor(100000 + Math.random() * 900000);
  }
  code = code.trim().toUpperCase();
  currentAiOrderCode.value = code;

  const finalAmount = totalAmount || createdOrder?.total || currentAiQrTotal.value || 40000;
  const now = new Date();
  const dateStr = now.toLocaleDateString("vi-VN") + " " + now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

  const cartItemsList = (orderState.value.cartItems && orderState.value.cartItems.length > 0)
    ? orderState.value.cartItems
    : (createdOrder?.items || [
        {
          productId: 'prod_ai_default',
          productName: 'Đơn hàng thực phẩm AI ZoneMart',
          price: finalAmount,
          quantity: 1,
          imageUrl: '/images/anhxoanen.png',
          storeName: 'Vườn Rau Ba Vì (Cầu Giấy)'
        }
      ]);

  const formattedItems = cartItemsList.map((item: any) => ({
    id: item.productId || item.id || ('prod_' + Date.now()),
    productId: item.productId || item.id || ('prod_' + Date.now()),
    name: item.productName || item.name || 'Sản phẩm ZoneMart',
    price: Number(item.price) || 0,
    quantity: Number(item.quantity) || 1,
    image: item.imageUrl || item.image || '/images/anhxoanen.png',
    shop: item.storeName || item.shop || 'Vườn Rau Ba Vì (Cầu Giấy)'
  }));

  const buyerName = auth.currentUser.value?.fullName || auth.currentUser.value?.phoneEmail || 'Khách Hàng ZoneMart';
  const buyerPhone = (auth.currentUser.value as any)?.phone || auth.currentUser.value?.phoneEmail || '0988 888 888';
  const shippingAddress = orderState.value.address || userLocation.value || 'Số 48 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội';
  const shippingMethod = orderState.value.shippingMethod || 'Giao Hàng Hỏa Tốc (15-25 phút)';
  const firstShopName = formattedItems[0]?.shop || 'Vườn Rau Ba Vì (Cầu Giấy)';

  const newOrder = {
    id: code,
    orderId: code,
    orderCode: code,
    date: dateStr,
    createdAt: dateStr,
    total: finalAmount,
    totalAmount: finalAmount,
    status: 'Chờ xác nhận',
    orderStatus: 'Processing',
    paymentStatus: 'Unpaid',
    isPaid: false,
    itemsCount: formattedItems.length,
    items: formattedItems,
    shippingAddress,
    recipientName: buyerName,
    recipientPhone: buyerPhone,
    deliveryNote: 'Đặt hàng qua Trợ Lý AI ZoneMart (Thanh toán tiền mặt COD khi nhận hàng)',
    shippingMethod,
    paymentMethod: 'Tiền mặt khi nhận hàng (COD)',
    paidAt: null,
    location: { lat: 21.0333, lng: 105.7983 }
  };

  // 1. Lưu vào Profile Orders của User & Guest
  const acc = auth.currentUser.value;
  const candidateKeys = new Set<string>();
  if (acc) {
    if (acc.phoneEmail) candidateKeys.add("zonemart_profile_orders_" + acc.phoneEmail.toLowerCase().trim());
    if (acc.id) candidateKeys.add("zonemart_profile_orders_" + acc.id.toLowerCase().trim());
    if ((acc as any).phone) candidateKeys.add("zonemart_profile_orders_" + (acc as any).phone.toLowerCase().trim());
  }
  candidateKeys.add("zonemart_profile_orders_guest");

  candidateKeys.forEach(k => {
    try {
      const existing = JSON.parse(localStorage.getItem(k) || '[]');
      existing.unshift(newOrder);
      localStorage.setItem(k, JSON.stringify(existing));
    } catch (e) {}
  });

  // 2. Xoá giỏ hàng
  try {
    cart.clearCart();
  } catch (e) {}

  // 3. Phát tán sự kiện nổ đơn thời gian thực tới Shipper & Seller
  try {
    orderRealtimeService.dispatchNewOrder({
      id: code,
      orderCode: code,
      total: finalAmount,
      shippingFee: (orderState.value as any).shippingFee || 15000,
      paymentMethod: 'Tiền mặt khi nhận hàng (COD)',
      createdAt: dateStr,
      status: 'pending',
      statusText: 'Đang chờ tài xế nhận đơn',
      customer: {
        lat: 21.0333,
        lng: 105.7983,
        name: buyerName,
        phone: buyerPhone,
        address: shippingAddress
      },
      store: {
        lat: 21.0345,
        lng: 105.7960,
        name: firstShopName,
        address: 'Số 48 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội',
        phone: '0988 123 456'
      },
      items: formattedItems
    });
  } catch (e) {
    console.warn('Lỗi phát đơn thời gian thực:', e);
  }

  // 4. Phát âm thanh xác nhận
  playPaymentSuccessChime();

  // 5. Đếm ngược 3 giây chuyển qua trang /buyer-orders
  aiRedirectCountdown.value = 3;
  if (aiRedirectInterval) clearInterval(aiRedirectInterval);
  aiRedirectInterval = setInterval(() => {
    aiRedirectCountdown.value--;
    if (aiRedirectCountdown.value <= 0) {
      clearInterval(aiRedirectInterval);
      aiRedirectInterval = null;
      goToBuyerOrdersDirectly();
    }
  }, 1000);
};

// Kích hoạt toàn diện hệ thống lắng nghe thời gian thực (6 kênh đa luồng song song liên tục, không ngắt quãng)
const startAiPaymentListener = (orderCode: string, amount: number) => {
  if (!orderCode) return;
  const cleanCode = orderCode.trim().toUpperCase();
  currentAiOrderCode.value = cleanCode;
  currentAiQrTotal.value = amount || 40000;
  isAiPaymentConfirmed.value = false;
  stopAiPaymentListener();

  // 0. Đăng ký thông tin đơn hàng lên server
  syncAiOrderToServer(cleanCode, currentAiQrTotal.value, orderState.value.cartItems || []);

  // Kiểm tra tức thì một lần trước khi vào vòng lặp
  fetch(`/api/payment/check?code=${encodeURIComponent(cleanCode)}&amount=${currentAiQrTotal.value}&bankAccount=28122068866`)
    .then(r => r.json())
    .then(data => {
      if (data && data.paid) {
        console.log('>>> [AI IMMEDIATE VITE CHECK CONFIRMED]:', data);
        onAiPaymentConfirmed(data.amount ? Number(data.amount) : undefined);
      }
    })
    .catch(() => {});

  fetch(`http://localhost:5000/api/aichat/check-qr-payment?orderCode=${encodeURIComponent(cleanCode)}`)
    .then(r => r.json())
    .then(data => {
      if (data && data.isPaid) {
        console.log('>>> [AI IMMEDIATE BACKEND CHECK CONFIRMED]:', data);
        onAiPaymentConfirmed();
      }
    })
    .catch(() => {});

  // 1. Kênh Server-Sent Events (SSE) bắt tức thì trong 0.1s
  try {
    if (typeof EventSource !== 'undefined') {
      const sseUrl = `/api/payment/events?code=${encodeURIComponent(cleanCode)}`;
      aiSseSource = new EventSource(sseUrl);
      aiSseSource.onmessage = (e) => {
        try {
          const data = JSON.parse(e.data);
          if (data && data.paid && data.code && data.code.toUpperCase() === cleanCode) {
            console.log('>>> [AI SSE CONFIRMED PAYMENT]:', data);
            onAiPaymentConfirmed(data.amount ? Number(data.amount) : undefined);
          }
        } catch (err) {}
      };
      aiSseSource.onerror = () => {
        try { aiSseSource?.close(); } catch (e) {}
      };
    }
  } catch (e) {}

  // 2. Kênh WebSocket VietQR trực tiếp từ trình duyệt
  try {
    const wsUrl = "wss://api.vietqr.org/vqr/socket?clientId=customer-zonemart-user26685";
    aiVietQrSocket = new WebSocket(wsUrl);
    aiVietQrSocket.onmessage = (event) => {
      try {
        const data = typeof event.data === "string" ? JSON.parse(event.data) : event.data;
        const rawText = `${data.orderId || ""} ${data.content || ""} ${data.addInfo || ""} ${data.description || ""}`.toUpperCase();
        if (rawText.includes(cleanCode) || data.orderId === cleanCode) {
          console.log('>>> [AI VIETQR WS CONFIRMED]:', data);
          onAiPaymentConfirmed(data.amount ? Number(data.amount) : undefined);
        }
      } catch (err) {}
    };
  } catch (e) {}

  // 3. Kênh BroadcastChannel đồng bộ đa tab
  try {
    if (typeof window !== 'undefined' && window.BroadcastChannel) {
      aiBroadcastChannel = new BroadcastChannel("zonemart_vietqr_payment");
      aiBroadcastChannel.onmessage = (event) => {
        const data = event.data;
        if (data && (data.content?.includes(cleanCode) || data.orderId === cleanCode)) {
          onAiPaymentConfirmed(data.amount);
        }
      };
    }
  } catch (e) {}

  // 4. Kênh Window Message
  aiMessageListener = (event: MessageEvent) => {
    if (event.data?.type === "TINGO_PAY_SUCCESS" && event.data?.code === cleanCode) {
      onAiPaymentConfirmed(event.data?.amount);
    }
  };
  window.addEventListener("message", aiMessageListener);

  // 5. Kênh Storage Event (bắt biến động LocalStorage từ các tab khác)
  aiStorageListener = (event: StorageEvent) => {
    if (event.key === "zonemart_incoming_transaction") {
      try {
        const tx = JSON.parse(event.newValue || "{}");
        if (tx && (tx.content?.includes(cleanCode) || tx.code === cleanCode)) {
          onAiPaymentConfirmed(tx.amount);
        }
      } catch (e) {}
    }
  };
  window.addEventListener("storage", aiStorageListener);

  // 6. Kênh Polling liên tục mỗi 1 giây (Query cả Vite Server lẫn .NET Backend không bao giờ tự dừng)
  aiPaymentPollInterval = setInterval(async () => {
    if (isAiPaymentConfirmed.value) {
      stopAiPaymentListener();
      return;
    }

    // 6a. Kiểm tra qua Vite Dev Server
    try {
      const checkUrl = `/api/payment/check?code=${encodeURIComponent(cleanCode)}&amount=${currentAiQrTotal.value}&bankAccount=28122068866`;
      const res = await fetch(checkUrl);
      if (res.ok) {
        const data = await res.json();
        if (data && data.paid && data.code && data.code.toUpperCase() === cleanCode) {
          console.log('>>> [AI VITE POLL CONFIRMED]:', data);
          onAiPaymentConfirmed(data.amount);
          return;
        }
      }
    } catch (e) {}

    // 6b. Kiểm tra qua Backend C# (.NET) - Backend trực tiếp kiểm tra Telegram Bot & Database
    try {
      const beRes = await fetch(`http://localhost:5000/api/aichat/check-qr-payment?orderCode=${encodeURIComponent(cleanCode)}`)
        .catch(() => fetch(`/api/aichat/check-qr-payment?orderCode=${encodeURIComponent(cleanCode)}`));
      if (beRes && beRes.ok) {
        const beData = await beRes.json();
        if (beData && beData.isPaid) {
          console.log('>>> [AI BACKEND POLL CONFIRMED]:', beData);
          onAiPaymentConfirmed();
          return;
        }
      }
    } catch (e) {}

    // 6c. Kiểm tra LocalStorage
    const storedStatus = localStorage.getItem("vietqr_paid_" + cleanCode);
    if (storedStatus) {
      onAiPaymentConfirmed();
      return;
    }
  }, 1000);
};


function clearChatHistory() {
  stopAiPaymentListener();
  if (aiRedirectInterval) {
    clearInterval(aiRedirectInterval);
    aiRedirectInterval = null;
  }
  userChatHistory.value = [];
  userMessage.value = '';
  registrationState.value = 'NONE';
  orderState.value = { step: 'IDLE', cartItems: [], pendingItem: null };
  try {
    sessionStorage.removeItem('zonemart_ai_chat_history');
  } catch {}
}

onMounted(() => {
  // Lần đầu xuất hiện bong bóng gợi ý sau 3 giây khi vào trang
  bubbleShowTimer = setTimeout(triggerNextBubble, 3000);

  // Kiểm tra xem trong lịch sử chat có đơn hàng QR nào chưa thanh toán không để tự động kết nối và đối soát
  const lastQrMsg = [...userChatHistory.value].reverse().find(m => (m.actionIntent === 'SHOW_QR_PAYMENT' || m.qrCodeUrl) && !m.isPaid);
  if (lastQrMsg) {
    const oCode = extractOrderCodeFromMsg(lastQrMsg) || (orderState.value as any)?.createdOrderCode;
    const total = lastQrMsg.totalAmount || 40000;
    if (oCode) {
      console.log('>>> [AI ASSISTANT MOUNTED] Phục hồi lắng nghe thanh toán mã:', oCode);
      startAiPaymentListener(oCode, total);
    }
  }
});

onUnmounted(() => {
  stopAiPaymentListener();
  if (aiRedirectInterval) {
    clearInterval(aiRedirectInterval);
    aiRedirectInterval = null;
  }
  if (bubbleShowTimer) clearTimeout(bubbleShowTimer);
  if (bubbleHideTimer) clearTimeout(bubbleHideTimer);
});

// Lưu ID/Email của tài khoản hiện tại để phát hiện ĐĂNG NHẬP / ĐĂNG XUẤT thực sự
const currentUserId = ref<string | null>(auth.currentUser.value?.id || auth.currentUser.value?.phoneEmail || null);

// CHỈ xoá lịch sử trò chuyện khi thực sự có hành động ĐĂNG NHẬP hoặc ĐĂNG XUẤT (chuyển đổi ID tài khoản)
watch(
  () => auth.currentUser.value?.id || auth.currentUser.value?.phoneEmail || null,
  (newId, oldId) => {
    if (newId !== oldId) {
      currentUserId.value = newId;
      clearChatHistory();
    }
  }
);

// Lắng nghe sự kiện Đăng xuất / Đăng nhập từ useAuth.ts
if (typeof window !== 'undefined') {
  window.addEventListener('zonemart:ai-chat-reset', () => {
    clearChatHistory();
  });
  window.addEventListener('zonemart:user-updated', (event: any) => {
    const updatedUser = event?.detail;
    const newId = updatedUser?.id || updatedUser?.phoneEmail || null;
    if (newId !== currentUserId.value) {
      currentUserId.value = newId;
      clearChatHistory();
    }
  });
}

// Tự động lưu lịch sử chat khi có tin nhắn mới (kể cả khi chuyển trang)
watch(
  userChatHistory,
  (newHistory) => {
    saveChatHistory(newHistory);
  },
  { deep: true }
);

const messages = computed(() => {
  return [welcomeMessage.value, ...userChatHistory.value];
});

// Danh sách câu thoại gợi ý không icon, thi thoảng hiển thị phía trên avatar
const bubblePhrases = [
  "Bạn cần giúp gì không ạ?",
  "Ở đây có mình tư vấn nè",
  "Mình ở đây để tư vấn 24/7 nhaa",
  "Cần tìm món ngon hay quán hot không bạn?",
  "Bạn muốn AI hỗ trợ đặt đồ hỏa tốc không?",
  "Hỏi mình bất cứ điều gì về ZoneMart nhé!"
];
const currentPhraseIndex = ref(0);
const isBubbleVisible = ref(false);
let bubbleShowTimer: any = null;
let bubbleHideTimer: any = null;

function triggerNextBubble() {
  if (isOpen.value) {
    isBubbleVisible.value = false;
    return;
  }
  // Hiện bong bóng thoại
  isBubbleVisible.value = true;
  // Hiện tầm 3 giây rồi biến mất từ từ
  bubbleHideTimer = setTimeout(() => {
    isBubbleVisible.value = false;
    currentPhraseIndex.value = (currentPhraseIndex.value + 1) % bubblePhrases.length;
    // Chờ 8 giây mới hiện cái tiếp theo
    bubbleShowTimer = setTimeout(triggerNextBubble, 8000);
  }, 3000);
}

watch(isOpen, (newVal) => {
  if (newVal) {
    isBubbleVisible.value = false;
    if (bubbleHideTimer) clearTimeout(bubbleHideTimer);
    if (bubbleShowTimer) clearTimeout(bubbleShowTimer);

    // Khi người dùng mở chat, tự động kiểm tra xem có mã QR chưa thanh toán để kết nối đối soát ngay
    if (!isAiPaymentConfirmed.value) {
      const activeQr = [...userChatHistory.value].reverse().find(m => (m.actionIntent === 'SHOW_QR_PAYMENT' || m.qrCodeUrl) && !m.isPaid);
      if (activeQr) {
        const cCode = extractOrderCodeFromMsg(activeQr) || (orderState.value as any)?.createdOrderCode || currentAiOrderCode.value;
        const total = activeQr.totalAmount || currentAiQrTotal.value || 40000;
        if (cCode) {
          startAiPaymentListener(cCode, total);
        }
      }
    }
  } else {
    if (bubbleShowTimer) clearTimeout(bubbleShowTimer);
    bubbleShowTimer = setTimeout(triggerNextBubble, 8000);
  }
});

function toggleChat() {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    scrollToBottom();
  }
}

function scrollToBottom() {
  nextTick(() => {
    if (chatContainer.value) {
      chatContainer.value.scrollTop = chatContainer.value.scrollHeight;
    }
  });
}

async function sendMessage(customText?: string) {
  const textToSend = customText || userMessage.value.trim();
  if (!textToSend || isLoading.value) return;

  const isPwd = !customText && isPasswordStep.value;

  const userMsgObj: ChatMessage = {
    id: Date.now().toString(),
    sender: 'user',
    text: textToSend,
    timestamp: getCurrentTime(),
    isPassword: isPwd
  };

  userChatHistory.value.push(userMsgObj);
  if (!customText) userMessage.value = '';
  scrollToBottom();
  isLoading.value = true;

  try {
    const historyPayload = messages.value.slice(-6).map(m => ({
      sender: m.sender,
      text: m.text
    }));

    const response = await fetch('http://localhost:5000/api/aichat/chat', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        message: textToSend,
        userRole: auth.currentRole.value,
        userEmail: auth.currentUser.value?.phoneEmail,
        userLocation: userLocation.value,
        history: historyPayload,
        registrationState: registrationState.value,
        orderState: orderState.value,
        regDraft: regDraft.value,
        supportState: supportState.value,
        supportDraft: supportDraft.value
      })
    });

    if (response.ok) {
      const data = await response.json();
      if (data.registrationState !== undefined) {
        registrationState.value = data.registrationState;
      }
      if (data.regDraft !== undefined) {
        regDraft.value = data.regDraft;
      }
      if (data.supportState !== undefined) {
        supportState.value = data.supportState;
      }
      if (data.supportDraft !== undefined) {
        supportDraft.value = data.supportDraft;
      }
      if (data.orderState !== undefined) {
        orderState.value = data.orderState;
      }

      // Khi hiển thị mã QR thanh toán hoặc đang ở bước chờ thanh toán QR:
      // Kích hoạt toàn bộ hệ thống lắng nghe thời gian thực (SSE, WebSocket, Polling 1s, BroadcastChannel...)
      const activeOrderCode = data.orderCode || extractOrderCodeFromMsg(data) || (data.orderState as any)?.createdOrderCode || (orderState.value as any)?.createdOrderCode;
      const activeTotal = data.totalAmount || (data.orderState as any)?.grandTotal || ((orderState.value as any)?.cartItems?.reduce((s: number, i: any) => s + (i.price * i.quantity), 0) + ((orderState.value as any)?.shippingFee || 15000)) || 40000;

      if (data.actionIntent === 'SHOW_QR_PAYMENT' || orderState.value?.step === 'AWAITING_QR_PAYMENT') {
        if (activeOrderCode) {
          startAiPaymentListener(activeOrderCode, activeTotal);
        }
      }

      // Xử lý chuyển sang trang theo dõi đơn hàng sau khi hoàn tất đặt hàng qua AI
      if (data.actionIntent === 'REDIRECT_TO_BUYER_ORDERS' || data.actionIntent === 'AUTO_ADD_TO_CART_AND_CHECKOUT') {
        const isCash = (data.orderState as any)?.paymentMethod?.includes('Tiền Mặt') || 
                       (data.orderState as any)?.paymentMethod?.includes('COD') || 
                       (data.createdOrder as any)?.paymentMethod?.includes('Tiền Mặt') || 
                       (data.createdOrder as any)?.paymentMethod?.includes('COD');

        if (isCash) {
          onAiCashOrderConfirmed(data.createdOrder, data.totalAmount || activeTotal);
        } else {
          onAiPaymentConfirmed(data.totalAmount || (data.createdOrder as any)?.total || activeTotal);
        }
      }

      const aiMsgObj: ChatMessage = {
        id: (Date.now() + 1).toString(),
        sender: 'ai',
        text: data.reply || 'Dạ mình luôn sẵn sàng hỗ trợ bạn tại ZoneMart!',
        htmlText: data.htmlText,
        timestamp: getCurrentTime(),
        provider: 'ZoneMart AI Assistant',
        products: data.products,
        stores: data.stores,
        actionIntent: data.actionIntent,
        summaryData: data.summaryData,
        qrCodeUrl: data.qrCodeUrl,
        totalAmount: data.totalAmount || activeTotal,
        orderCode: activeOrderCode,
        isPaid: false
      };
      userChatHistory.value.push(aiMsgObj);

      // Tự động yêu cầu quyền vị trí từ trình duyệt nếu server phản hồi yêu cầu định vị
      if (data.actionIntent === 'ACTION_REQUEST_LOCATION') {
        requestBrowserLocation(false);
      } else if (data.actionIntent === 'ACTION_REQUEST_LOCATION_FOR_STORES') {
        requestBrowserLocation(true, 5);
      } else if (data.actionIntent === 'ACTION_REQUEST_LOCATION_FOR_ORDER_ADDRESS') {
        requestBrowserLocation(false, 5, true);
      }
    } else {
      userChatHistory.value.push({
        id: (Date.now() + 1).toString(),
        sender: 'ai',
        text: 'Dạ hệ thống đang xử lý dữ liệu. Bạn có thể hỏi về gợi ý món ăn, top quán hot hoặc đăng ký tài khoản ZoneMart nhé!',
        timestamp: getCurrentTime(),
        provider: 'ZoneMart AI Assistant'
      });
    }
  } catch (err) {
    userChatHistory.value.push({
      id: (Date.now() + 1).toString(),
      sender: 'ai',
      text: 'Dạ mình là Trợ Lý AI Chuyên Gia ZoneMart. Bạn muốn gợi ý món ngon hỏa tốc hay hỗ trợ đăng ký tài khoản Seller/Shipper ạ?',
      timestamp: getCurrentTime(),
      provider: 'ZoneMart AI Assistant'
    });
  } finally {
    isLoading.value = false;
    scrollToBottom();
  }
}

function handleAction(type: string) {
  if (type === 'REGISTER_SELLER' || type === 'seller_manual') {
    router.push('/register-seller');
    isOpen.value = false;
  } else if (type === 'REGISTER_SHIPPER' || type === 'shipper_manual') {
    router.push('/register-shipper');
    isOpen.value = false;
  } else if (type === 'REGISTER_BUYER' || type === 'buyer_manual') {
    router.push('/register');
    isOpen.value = false;
  } else if (type === 'buyer_login') {
    router.push('/login');
    isOpen.value = false;
  } else if (type === 'support_manual') {
    router.push('/contact');
    isOpen.value = false;
  } else if (type === 'go_home') {
    router.push('/');
    isOpen.value = false;
  }
}

function goToProduct(id: string) {
  router.push(`/products/${id}`);
  isOpen.value = false;
}

// Kiểm tra xem nút hành động của tin nhắn này có phải là câu hỏi mới nhất đang chờ phản hồi hay không
function isActionActive(msg: ChatMessage, index: number): boolean {
  if (msg.actionIntent === 'SHOW_QR_PAYMENT') return true;
  if (isLoading.value) return false;

  // Nếu sau tin nhắn này đã có tin nhắn của người dùng gửi (người dùng đã trả lời bước này)
  const hasUserReplyAfter = messages.value.slice(index + 1).some(m => m.sender === 'user');
  if (hasUserReplyAfter) return false;

  // Nếu sau tin nhắn này đã có tin nhắn AI khác có actionIntent (đã sang câu hỏi tiếp theo)
  const hasLaterActionIntent = messages.value.slice(index + 1).some(m => !!m.actionIntent);
  if (hasLaterActionIntent) return false;

  // Chỉ kích hoạt nếu đây là tin nhắn cuối cùng có actionIntent trong danh sách
  const lastActionIndex = messages.value.map(m => !!m.actionIntent).lastIndexOf(true);
  return index === lastActionIndex;
}
</script>

<template>
  <Teleport to="body">
    <div class="ai-widget-wrapper">
    <!-- Bong Bóng Lời Thoại Gợi Ý (Thi thoảng mới hiện phía trên avatar) -->
    <transition name="bubble-pop">
      <div
        v-if="!isOpen && isBubbleVisible"
        class="ai-speech-bubble"
        @click="toggleChat"
        title="Bấm để chat với Trợ Lý AI"
      >
        <div class="bubble-content">
          <span class="bubble-text">
            {{ bubblePhrases[currentPhraseIndex] }}
          </span>
        </div>
        <div class="bubble-tail"></div>
      </div>
    </transition>

    <!-- Khung Nút Bấm Nổi Tròn Kèm Hiệu Ứng Sóng Âm -->
    <div class="trigger-container">
      <!-- Vòng sóng âm phát tỏa nhẹ nhàng xung quanh avatar khi chưa mở chat -->
      <div v-if="!isOpen" class="soundwave-wrapper">
        <span class="soundwave-ring ring-1"></span>
        <span class="soundwave-ring ring-2"></span>
        <span class="soundwave-ring ring-3"></span>
      </div>

      <button
        class="ai-trigger-btn"
        :class="{ 'is-active': isOpen }"
        title="Hỏi Trợ Lý AI ZoneMart 24/7"
        @click="toggleChat"
      >
        <div class="avatar-ring">
          <div class="img-crop-box">
            <img src="/images/anhchattuvanAI_full_clean.png" alt="AI Avatar" class="ai-avatar-img" />
          </div>
          <span class="online-dot"></span>
        </div>
      </button>
    </div>

    <!-- Khung Cửa Sổ Chat AI Glassmorphism -->
    <transition name="chat-slide">
      <div v-if="isOpen" class="ai-chat-window">
        <!-- Header Cửa Sổ Chat -->
        <div class="chat-header">
          <div class="header-left">
            <div class="header-avatar-wrap">
              <div class="avatar-crop-box">
                <img src="/images/anhchattuvanAI_full_clean.png" alt="AI Avatar" class="header-avatar" />
              </div>
              <span class="status-pulse"></span>
            </div>
            <div class="header-info">
              <h3 class="header-title">Trợ Lý AI ZoneMart 24/7</h3>
              <p class="header-status">⚡ Trực tuyến • Tự động phản hồi</p>
            </div>
          </div>
          <button class="close-btn" title="Đóng cửa sổ" @click="isOpen = false">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <!-- Input ẩn để tải ảnh giấy tờ (CCCD, ATTP, Bằng lái, Chân dung) theo chuẩn giao diện thủ công -->
        <input
          type="file"
          ref="fileUploadInputRef"
          accept="image/*"
          style="display: none;"
          @change="handleFileUploadChange"
        />

        <!-- Thẻ Gợi Ý Nhanh (Quick Action Chips) -->
        <div class="quick-chips-bar">
          <button class="chip-item" @click="sendMessage('Nay thời tiết nóng quá tôi nên ăn món gì đỡ nóng?')">
            ☀️ Trời nóng giải nhiệt
          </button>
          <button class="chip-item" @click="sendMessage('Trời se lạnh nên ăn món gì ấm nóng ngon?')">
            🌧️ Trời se lạnh ấm nóng
          </button>
          <button class="chip-item" @click="sendMessage('Tôi muốn nhờ AI tự động chọn và đặt đồ hộ tôi')">
            🛍️ AI Chọn & Đặt Đồ Hộ
          </button>
          <button class="chip-item" @click="sendMessage('Gợi ý cho tôi các món ăn có nước nóng hổi')">
            🍲 Món có nước
          </button>
          <button class="chip-item" @click="sendMessage('Gợi ý các món ăn khô như Cơm, Bánh mì')">
            🍱 Món ăn khô
          </button>
          <button class="chip-item" @click="sendMessage('Cho tôi danh sách Top Quán Best Seller')">
            🔥 Top Quán Hot
          </button>
          <button class="chip-item" @click="sendMessage('Tôi muốn AI đăng ký hộ làm Seller Gian Hàng')">
            🏪 AI Đăng ký Seller
          </button>
          <button class="chip-item" @click="sendMessage('Tôi muốn AI đăng ký hộ làm Tài xế Shipper')">
            🛵 AI Đăng ký Shipper
          </button>
          <button class="chip-item" @click="sendMessage('Tôi muốn AI đăng ký hộ làm Khách hàng')">
            👤 AI Đăng ký Khách hàng
          </button>
          <button class="chip-item" @click="sendMessage('Tôi muốn gửi yêu cầu hỗ trợ trực tuyến')">
            🎧 Hỗ trợ trực tuyến
          </button>
        </div>

        <!-- Khung Nội Dung Tin Nhắn -->
        <div class="chat-messages-body" ref="chatContainer">
          <div
            v-for="(msg, index) in messages"
            :key="msg.id"
            class="message-row"
            :class="msg.sender === 'user' ? 'row-user' : 'row-ai'"
          >
            <div v-if="msg.sender === 'ai'" class="ai-avatar-mini">
              <div class="avatar-crop-box">
                <img src="/images/anhchattuvanAI_full_clean.png" alt="AI" />
              </div>
            </div>

            <div class="message-bubble-wrap">
              <div class="message-bubble">
                <p class="msg-text" @click="handleTextClick($event, msg.id)" v-html="formatMessageHtml(msg)"></p>

                <!-- Render Thẻ Sản Phẩm Nổi Bật (Nếu Có) -->
                <div v-if="msg.products && msg.products.length > 0" class="cards-grid">
                  <div
                    v-for="p in msg.products"
                    :key="p.id"
                    class="mini-card product-mini-card"
                    @click="goToProduct(p.id)"
                  >
                    <div class="card-main-info">
                      <img
                        :src="(p as any).image || (p as any).imageUrl || '/images/anhxoanen.png'"
                        :alt="(p as any).name || (p as any).productName"
                        class="card-thumb"
                        @error="(e) => ((e.target as HTMLImageElement).src = '/images/anhxoanen.png')"
                      />
                      <div class="card-content">
                        <h4 class="card-title" :title="(p as any).name || (p as any).productName">{{ (p as any).name || (p as any).productName || 'Món ngon ZoneMart' }}</h4>
                        <span class="card-store"><i class="bi bi-shop me-1"></i>{{ (p as any).storeName || 'ZoneMart Store' }}</span>
                      </div>
                    </div>
                    <div class="card-footer-row">
                      <div class="card-price">{{ (p as any).price ? (p as any).price.toLocaleString('vi-VN') + ' ₫' : '' }}</div>
                      <button class="btn-card-act">Xem món <i class="bi bi-arrow-right-short"></i></button>
                    </div>
                  </div>
                </div>

                <!-- Render Thẻ Quán Hot Best Seller (Nếu Có) -->
                <div v-if="msg.stores && msg.stores.length > 0" class="cards-grid">
                  <div
                    v-for="s in msg.stores"
                    :key="(s as any).id"
                    class="mini-card store-mini-card"
                  >
                    <div class="card-main-info">
                      <img
                        :src="(s as any).avatar || (s as any).avatarUrl || 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80'"
                        :alt="(s as any).name || (s as any).storeName"
                        class="card-thumb store-thumb"
                        @error="(e) => ((e.target as HTMLImageElement).src = 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80')"
                      />
                      <div class="card-content">
                        <h4 class="card-title">{{ (s as any).name || (s as any).storeName || 'Cửa hàng ZoneMart' }}</h4>
                        <p class="card-sub"><i class="bi bi-geo-alt me-1"></i>{{ (s as any).address || 'Khu vực Cầu Giấy, Hà Nội' }}</p>
                      </div>
                    </div>
                    <div class="card-footer-row">
                      <span class="rating-badge">⭐ {{ (s as any).rating || '4.9' }} Best Seller</span>
                    </div>
                  </div>
                </div>

                <!-- Interactive Buttons Cho 2 CÁCH ĐĂNG KÝ (Thủ công vs AI Đăng ký hộ) -->
                <div
                  v-if="msg.actionIntent"
                  class="action-buttons-wrap"
                  :class="{ 'actions-disabled': !isActionActive(msg, index) }"
                >
                  <div
                    v-if="!isActionActive(msg, index) && msg.actionIntent !== 'SHOW_QR_PAYMENT' && msg.actionIntent !== 'BUYER_REGISTER_SUCCESS' && msg.actionIntent !== 'SELLER_REGISTER_SUCCESS' && msg.actionIntent !== 'SHIPPER_REGISTER_SUCCESS'"
                    class="action-completed-badge"
                  >
                    <i class="bi bi-check-circle-fill" style="color: #94a3b8; font-size: 11px;"></i> Đã hoàn thành lựa chọn ở bước này
                  </div>
                  <!-- SELLER -->
                  <template v-if="msg.actionIntent === 'REGISTER_SELLER'">
                    <button class="btn-intent btn-intent-manual" @click="handleAction('seller_manual')">
                      <i class="bi bi-journal-text me-1"></i> Cách 1: Đi tới Form Đăng Ký Seller
                    </button>
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Tôi muốn AI đăng ký hộ làm Gian hàng Seller')">
                      <i class="bi bi-lightning-charge me-1"></i> Cách 2: Nhờ AI Đăng Ký Hộ Trực Tiếp
                    </button>
                  </template>

                  <!-- SHIPPER -->
                  <template v-else-if="msg.actionIntent === 'REGISTER_SHIPPER'">
                    <button class="btn-intent btn-intent-manual" @click="handleAction('shipper_manual')">
                      <i class="bi bi-journal-text me-1"></i> Cách 1: Đi tới Form Đăng Ký Shipper
                    </button>
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Tôi muốn AI đăng ký hộ làm Tài xế Shipper')">
                      <i class="bi bi-lightning-charge me-1"></i> Cách 2: Nhờ AI Đăng Ký Hộ Trực Tiếp
                    </button>
                  </template>

                  <!-- BUYER / KHÁCH HÀNG -->
                  <template v-else-if="msg.actionIntent === 'REGISTER_BUYER'">
                    <button class="btn-intent btn-intent-manual" @click="handleAction('buyer_manual')">
                      <i class="bi bi-journal-text me-1"></i> Cách 1: Đi tới Form Đăng Ký Khách Hàng
                    </button>
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Tôi muốn AI đăng ký hộ làm Khách hàng')">
                      <i class="bi bi-lightning-charge me-1"></i> Cách 2: Nhờ AI Đăng Ký Hộ Trực Tiếp
                    </button>
                  </template>

                  <!-- GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN (THỦ CÔNG VS AI TƯ VẤN GỬI HỘ) -->
                  <template v-else-if="msg.actionIntent === 'SUPPORT_CHOICE'">
                    <button class="btn-intent btn-intent-manual" @click="handleAction('support_manual')">
                      <i class="bi bi-journal-text me-1"></i> Cách 1: Đi tới Form Hỗ Trợ Thủ Công
                    </button>
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Tôi muốn AI hỗ trợ gửi yêu cầu trực tiếp')">
                      <i class="bi bi-lightning-charge me-1"></i> Cách 2: Nhờ AI Tư Vấn Gửi Hộ Trực Tiếp
                    </button>
                  </template>

                  <!-- BƯỚC 1: CHỌN 1 TRONG 5 VẤN ĐỀ CẦN HỖ TRỢ -->
                  <template v-else-if="msg.actionIntent === 'SUPPORT_CHOOSE_TOPIC'">
                    <div v-if="msg.selectedTopic" class="selected-topic-wrap" style="width: 100%;">
                      <div class="selected-topic-badge" style="background: linear-gradient(135deg, #10b981, #059669); color: white; padding: 8px 14px; border-radius: 10px; font-weight: 700; font-size: 13.5px; display: inline-flex; align-items: center; gap: 8px; box-shadow: 0 2px 8px rgba(16, 185, 129, 0.25);">
                        <i class="bi bi-check-circle-fill" style="font-size: 15px;"></i>
                        <span>Đã chọn: {{ msg.selectedTopic }}</span>
                      </div>
                    </div>
                    <div v-else class="chips-selector-grid">
                      <button
                        v-for="t in supportTopics"
                        :key="t.name"
                        class="btn-intent btn-choice-chip"
                        @click="chooseSupportTopic(msg, t.name)"
                      >
                        <i class="bi me-1" :class="t.icon" style="color: #D94E15;"></i> {{ t.name }}
                      </button>
                    </div>
                  </template>

                  <!-- BƯỚC 2: HỎI TÊN (CÓ NÚT DÙNG THÔNG TIN TÀI KHOẢN NẾU ĐÃ ĐĂNG NHẬP) -->
                  <template v-else-if="msg.actionIntent === 'SUPPORT_NAME_INPUT'">
                    <button
                      v-if="currentLoggedInSupportUser"
                      class="btn-intent btn-intent-ai"
                      style="background: linear-gradient(135deg, #3b82f6, #2563eb); color: #fff; font-weight: 700; box-shadow: 0 4px 12px rgba(37, 99, 235, 0.25); white-space: normal; text-align: center; justify-content: center; width: 100%; max-width: 100%;"
                      @click="useCurrentAccountInfo(currentLoggedInSupportUser)"
                    >
                      <i class="bi bi-person-check-fill me-1"></i> Dùng thông tin tài khoản hiện tại
                    </button>
                  </template>

                  <!-- BƯỚC 5: MÃ ĐƠN HÀNG (CÓ NÚT BỎ QUA - BỎ ICON TUA EMOJI) -->
                  <template v-else-if="msg.actionIntent === 'SUPPORT_ORDER_CODE_OPTIONAL'">
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Bỏ qua')">
                      <i class="bi bi-skip-forward-circle me-1"></i> Bỏ Qua Bước Này (Không Có Mã Đơn)
                    </button>
                  </template>

                  <!-- BƯỚC 8: BẢNG TỔNG HỢP & NÚT GỬI / SỬA / HỦY (BỎ CẢ 2 EMOJI TRÊN NÚT) -->
                  <template v-else-if="msg.actionIntent === 'SUPPORT_SUBMIT_CONFIRM'">
                    <button
                      class="btn-intent btn-intent-ai"
                      style="background: linear-gradient(135deg, #10b981, #059669); color: #fff; font-weight: 700; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);"
                      @click="sendMessage('Xác nhận gửi yêu cầu hỗ trợ')"
                    >
                      <i class="bi bi-send-check-fill me-1"></i> Gửi Yêu Cầu Hỗ Trợ Ngay
                    </button>
                    <button
                      class="btn-intent btn-intent-manual"
                      style="border-color: #f59e0b; color: #b45309; background: #fffbeb;"
                      @click="sendMessage('Sửa lại yêu cầu')"
                    >
                      <i class="bi bi-pencil-square me-1"></i> Sửa Lại Yêu Cầu
                    </button>
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Hủy yêu cầu')">
                      <i class="bi bi-x-circle me-1"></i> Hủy Yêu Cầu
                    </button>
                  </template>

                  <!-- BƯỚC SỬA YÊU CẦU: CHỌN 1 TRONG 4 MỤC ĐƯỢC PHÉP SỬA -->
                  <template v-else-if="msg.actionIntent === 'SUPPORT_CHOOSE_EDIT_FIELD'">
                    <div class="chips-selector-grid">
                      <button class="btn-intent btn-choice-chip" @click="sendMessage('Sửa số điện thoại')">
                        <i class="bi bi-telephone-fill me-1" style="color: #10b981;"></i> Số điện thoại
                      </button>
                      <button class="btn-intent btn-choice-chip" @click="sendMessage('Sửa mã đơn hàng')">
                        <i class="bi bi-box-seam-fill me-1" style="color: #8b5cf6;"></i> Mã đơn hàng
                      </button>
                      <button class="btn-intent btn-choice-chip" @click="sendMessage('Sửa nội dung yêu cầu')">
                        <i class="bi bi-pencil-fill me-1" style="color: #f59e0b;"></i> Nội dung yêu cầu
                      </button>
                      <button class="btn-intent btn-choice-chip" @click="sendMessage('Sửa ảnh sự cố')">
                        <i class="bi bi-image-fill me-1" style="color: #06b6d4;"></i> Ảnh sự cố
                      </button>
                    </div>
                    <button class="btn-intent btn-intent-manual" style="margin-top: 6px; width: 100%; justify-content: center;" @click="sendMessage('Quay lại')">
                      <i class="bi bi-arrow-left-circle-fill me-1"></i> Quay lại bảng tổng kết
                    </button>
                  </template>

                  <!-- NÚT QUAY LẠI KHI ĐANG NHẬP SỬA 1 MỤC -->
                  <template v-else-if="msg.actionIntent === 'SUPPORT_EDIT_BACK_BTN'">
                    <button class="btn-intent btn-intent-manual" style="margin-top: 6px; width: 100%; justify-content: center;" @click="sendMessage('Quay lại')">
                      <i class="bi bi-arrow-left-circle-fill me-1"></i> Quay lại bảng tổng kết
                    </button>
                  </template>

                  <!-- ĐĂNG NHẬP ĐỂ AI ĐẶT ĐỒ HỘ -->
                  <template v-else-if="msg.actionIntent === 'SHOW_LOGIN_BTN'">
                    <button class="btn-intent btn-intent-ai" @click="handleAction('buyer_login')">
                      <i class="bi bi-box-arrow-in-right me-1"></i> 🔑 Đăng Nhập Ngay Để AI Đặt Đồ Hộ
                    </button>
                  </template>

                  <!-- BẬT ĐỊNH VỊ VỊ TRÍ CỦA WEB -->
                  <template v-else-if="msg.actionIntent === 'ACTION_REQUEST_LOCATION' || msg.actionIntent === 'ACTION_REQUEST_LOCATION_FOR_STORES'">
                    <button class="btn-intent btn-intent-ai" :disabled="isLocating" @click="() => requestBrowserLocation(msg.actionIntent === 'ACTION_REQUEST_LOCATION_FOR_STORES', 5)" style="background: linear-gradient(135deg, #ea580c, #f97316); color: #fff; font-weight: 700; box-shadow: 0 4px 12px rgba(234, 88, 12, 0.25);">
                      <span v-if="isLocating" class="spinner-border spinner-border-sm me-1" role="status"></span>
                      <i v-else class="bi bi-geo-alt-fill me-1" style="color: #fff;"></i>
                      {{ isLocating ? 'Đang kích hoạt GPS & xác định vị trí...' : '📍 Bật Định Vị Vị Trí Của Bạn' }}
                    </button>
                  </template>

                  <!-- CẤP QUYỀN ĐỊNH VỊ ĐỊA CHỈ NHẬN HÀNG -->
                  <template v-else-if="msg.actionIntent === 'ACTION_REQUEST_LOCATION_FOR_ORDER_ADDRESS'">
                    <button class="btn-intent btn-intent-ai" :disabled="isLocating" @click="() => requestBrowserLocation(false, 5, true)" style="background: linear-gradient(135deg, #16a34a, #22c55e); color: #fff; font-weight: 700; box-shadow: 0 4px 12px rgba(22, 163, 74, 0.25);">
                      <span v-if="isLocating" class="spinner-border spinner-border-sm me-1" role="status"></span>
                      <i v-else class="bi bi-geo-alt-fill me-1" style="color: #fff;"></i>
                      {{ isLocating ? 'Đang lấy vị trí hiện tại...' : '📍 Cấp Quyền Lấy Địa Chỉ Hiện Tại' }}
                    </button>
                  </template>

                  <!-- SỬA GIỎ HÀNG (CÓ / KHÔNG) -->
                  <template v-else-if="msg.actionIntent === 'SHOW_YES_NO_EDIT_CART_BUTTONS'">
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Có')">
                      <i class="bi bi-pencil-square me-1"></i> Có (Thay Đổi Món / Số Lượng)
                    </button>
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Không')">
                      <i class="bi bi-arrow-right-circle me-1"></i> Không (Tiếp Tục)
                    </button>
                  </template>

                  <!-- ĐẶT THÊM ĐỒ KHÁC (CÓ / KHÔNG) -->
                  <template v-else-if="msg.actionIntent === 'SHOW_YES_NO_ADD_MORE_BUTTONS'">
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Có')">
                      <i class="bi bi-plus-circle me-1"></i> Có (Đặt Thêm Món Khác)
                    </button>
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Không')">
                      <i class="bi bi-arrow-right-circle me-1"></i> Không (Tiếp Tục)
                    </button>
                  </template>

                  <!-- NÚT QUAY LẠI TIẾP TỤC ĐẶT ĐỒ NẾU BẤM NHẦM CÓ -->
                  <template v-else-if="msg.actionIntent === 'SHOW_BACK_TO_CART_SUMMARY_BUTTON'">
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Quay lại tiếp tục đặt đồ')">
                      <i class="bi bi-arrow-left-circle-fill me-1"></i> ⬅️ Quay Lại Giỏ Hàng & Tiếp Tục Đặt Đồ
                    </button>
                  </template>

                  <!-- THAY ĐỔI ĐỊA CHỈ (CÓ / KHÔNG) -->
                  <template v-else-if="msg.actionIntent === 'SHOW_ADDRESS_CHOICE_BUTTONS'">
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Thay đổi địa chỉ')">
                      <i class="bi bi-geo-alt me-1"></i> Thay Đổi Địa Chỉ Nhận Hàng
                    </button>
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Giữ nguyên địa chỉ')">
                      <i class="bi bi-check-circle me-1"></i> Giữ Nguyên Địa Chỉ
                    </button>
                  </template>

                  <!-- CHỌN PHƯƠNG THỨC VẬN CHUYỂN -->
                  <template v-else-if="msg.actionIntent === 'SHOW_SHIPPING_METHOD_BUTTONS'">
                    <button class="btn-intent btn-capability" @click="sendMessage('Giao Hàng Hỏa Tốc')">
                      <i class="bi bi-lightning-charge-fill me-2" style="color: #f59e0b; font-size: 14px;"></i> Giao Hàng Hỏa Tốc (15-25 phút) - 22.500đ
                    </button>
                    <button class="btn-intent btn-capability" @click="sendMessage('Giao Hàng Tiêu Chuẩn')">
                      <i class="bi bi-truck me-2" style="color: #3b82f6; font-size: 14px;"></i> Giao Hàng Tiêu Chuẩn (30-45 phút) - 15.000đ
                    </button>
                  </template>

                  <!-- CHỌN PHƯƠNG THỨC THANH TOÁN -->
                  <template v-else-if="msg.actionIntent === 'SHOW_PAYMENT_METHOD_BUTTONS'">
                    <button class="btn-intent btn-intent-ai" @click="sendMessage('Chuyển Khoản QR Bank')">
                      <i class="bi bi-qr-code-scan me-1"></i> Chuyển Khoản QR Bank Ngân Hàng
                    </button>
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Tiền Mặt COD')">
                      <i class="bi bi-cash-stack me-1"></i> Tiền Mặt Khi Nhận Hàng (COD)
                    </button>
                  </template>

                  <!-- HỦY SỬA GIỎ HÀNG (GIỮ NGUYÊN GIỎ HÀNG) -->
                  <template v-else-if="msg.actionIntent === 'SHOW_CANCEL_EDIT_CART_BUTTON'">
                    <button class="btn-intent btn-intent-manual" @click="sendMessage('Hủy thay đổi')">
                      <i class="bi bi-x-circle me-1"></i> ❌ Hủy Thay Đổi & Giữ Nguyên Giỏ Hàng
                    </button>
                  </template>

                  <!-- MÃ QR THANH TOÁN (Biến mất hoàn toàn khi đã thanh toán) -->
                  <template v-else-if="msg.actionIntent === 'SHOW_QR_PAYMENT' && !msg.isPaid && !isAiPaymentConfirmed">
                    <div class="qr-payment-box" style="text-align: center; margin: 10px 0; background: #fff; padding: 14px; border-radius: 12px; border: 1.5px solid #fed7aa; box-shadow: 0 4px 14px rgba(0,0,0,0.06);">
                      <img
                        :src="msg.qrCodeUrl || `https://img.vietqr.io/image/TPB-28122068866-qr_only.png?amount=${msg.totalAmount || 40000}&addInfo=${encodeURIComponent(msg.orderCode || currentAiOrderCode || 'ZoneMartOrder')}&accountName=DOAN%20HOANG%20HUY`"
                        @error="(e) => ((e.target as HTMLImageElement).src = 'https://api.qrserver.com/v1/create-qr-code/?size=250x250&data=ZoneMartOrder_' + (msg.totalAmount || 40000))"
                        alt="VietQR Payment"
                        style="width: 200px; height: 200px; object-fit: contain; border-radius: 10px; border: 1px solid #cbd5e1; display: block; margin: 0 auto; background: #fff;"
                      />
                      <p style="margin-top: 10px; font-weight: 800; color: #d97706; font-size: 16px; margin-bottom: 6px;">
                        TỔNG TIỀN: {{ msg.totalAmount ? msg.totalAmount.toLocaleString('vi-VN') + ' ₫' : '' }}
                      </p>
                      <div style="margin-top: 8px; font-size: 12px; color: #059669; font-weight: 600; display: flex; align-items: center; justify-content: center; gap: 6px; background: #ecfdf5; padding: 8px 10px; border-radius: 8px; border: 1px solid #a7f3d0;">
                        <span class="spinner-border spinner-border-sm" role="status" style="width: 13px; height: 13px; color: #059669;"></span>
                        ⚡ Hệ thống đang lắng nghe & kiểm tra giao dịch chuyển tiền thời gian thực...
                      </div>
                    </div>
                  </template>

                  <!-- TÍNH NĂNG AI CÓ THỂ TƯ VẤN (CÁC NÚT BẤM THEO HÀNG) -->
                  <template v-else-if="msg.actionIntent === 'SHOW_CAPABILITIES'">
                    <button class="btn-intent btn-capability" @click="sendMessage('Tôi muốn nhờ AI tự động chọn và đặt đồ hộ tôi')">
                      <i class="bi bi-bag-check-fill me-2" style="color: #ec4899; font-size: 14px;"></i> AI Tự Động Chọn & Đặt Đồ Hộ
                    </button>
                    <button class="btn-intent btn-capability" @click="sendMessage('Nay thời tiết nóng quá tôi nên ăn món gì đỡ nóng?')">
                      <i class="bi bi-sun-fill me-2" style="color: #f59e0b; font-size: 14px;"></i> Gợi ý món giải nhiệt khi trời nóng
                    </button>
                    <button class="btn-intent btn-capability" @click="sendMessage('Trời se lạnh nên ăn món gì ấm nóng ngon?')">
                      <i class="bi bi-snow me-2" style="color: #06b6d4; font-size: 14px;"></i> Gợi ý món ấm nóng khi trời lạnh
                    </button>
                    <button class="btn-intent btn-capability" @click="findNearbyStores(5)">
                      <i class="bi bi-geo-alt-fill me-2" style="color: #ef4444; font-size: 14px;"></i> Tìm quán ngon bán kính 5km
                    </button>
                    <button class="btn-intent btn-capability" @click="sendMessage('Tôi muốn AI đăng ký hộ làm Khách hàng')">
                      <i class="bi bi-person-badge-fill me-2" style="color: #3b82f6; font-size: 14px;"></i> AI Đăng ký hộ Khách hàng
                    </button>
                    <button class="btn-intent btn-capability" @click="sendMessage('Tôi muốn mở shop gian hàng Seller')">
                      <i class="bi bi-shop me-2" style="color: #10b981; font-size: 14px;"></i> AI Đăng ký hộ Gian hàng Seller
                    </button>
                    <button class="btn-intent btn-capability" @click="sendMessage('Tôi muốn làm tài xế Shipper')">
                      <i class="bi bi-truck me-2" style="color: #f97316; font-size: 14px;"></i> AI Đăng ký hộ Tài xế Shipper
                    </button>
                    <button class="btn-intent btn-capability" @click="sendMessage('Tôi muốn gửi yêu cầu hỗ trợ trực tuyến')">
                      <i class="bi bi-headset me-2" style="color: #6366f1; font-size: 14px;"></i> Gửi Yêu Cầu Hỗ Trợ Trực Tuyến
                    </button>
                  </template>

                  <!-- SELLER DANH MỤC HÀNG BÁN CHÍNH (NÚT BẤM TOÀN BỘ DANH MỤC TRONG THỦ CÔNG) -->
                  <template v-else-if="msg.actionIntent === 'SELLER_CHOOSE_CATEGORY'">
                    <div class="chips-selector-grid">
                      <button
                        v-for="cat in sellerCategories"
                        :key="cat"
                        class="btn-intent btn-choice-chip"
                        @click="sendMessage(cat)"
                      >
                        <i class="bi bi-tag-fill me-1" style="color: #D94E15;"></i> {{ cat }}
                      </button>
                    </div>
                  </template>

                  <!-- SELLER KHUNG GIỜ KINH DOANH (NÚT BẤM TOÀN BỘ KHUNG GIỜ TRONG THỦ CÔNG) -->
                  <template v-else-if="msg.actionIntent === 'SELLER_CHOOSE_OPENHOURS'">
                    <div class="chips-selector-grid">
                      <button
                        v-for="h in sellerOpenHoursList"
                        :key="h"
                        class="btn-intent btn-choice-chip"
                        @click="sendMessage(h)"
                      >
                        <i class="bi bi-clock-fill me-1" style="color: #0284C7;"></i> {{ h }}
                      </button>
                    </div>
                  </template>

                  <!-- SELLER & SHIPPER NGÂN HÀNG (NÚT BẤM 10 NGÂN HÀNG) -->
                  <template v-else-if="msg.actionIntent === 'SELLER_CHOOSE_BANK' || msg.actionIntent === 'SHIPPER_CHOOSE_BANK'">
                    <div class="chips-selector-grid">
                      <button
                        v-for="bank in banksList"
                        :key="bank"
                        class="btn-intent btn-choice-chip"
                        @click="sendMessage(bank)"
                      >
                        <i class="bi bi-bank2 me-1" style="color: #10B981;"></i> {{ bank }}
                      </button>
                    </div>
                  </template>

                  <!-- SHIPPER LOẠI XE (XE MÁY XĂNG / XE MÁY ĐIỆN) -->
                  <template v-else-if="msg.actionIntent === 'SHIPPER_CHOOSE_VEHICLE_TYPE'">
                    <div class="chips-selector-grid">
                      <button
                        v-for="vt in shipperVehicleTypes"
                        :key="vt"
                        class="btn-intent btn-choice-chip"
                        @click="sendMessage(vt)"
                      >
                        <i :class="vt.includes('điện') ? 'bi bi-lightning-charge-fill' : 'bi bi-fuel-pump-fill'" class="me-1" style="color: #F59E0B;"></i> {{ vt }}
                      </button>
                    </div>
                  </template>

                  <!-- SHIPPER DÒNG XE / MẪU XE (HONDA, YAMAHA HOẶC VINFAST, DAT BIKE) -->
                  <template v-else-if="msg.actionIntent === 'SHIPPER_CHOOSE_VEHICLE_MODEL'">
                    <div class="chips-selector-grid">
                      <button
                        v-for="vm in (regDraft.vehicleType?.includes('điện') ? shipperElectricModels : shipperGasModels)"
                        :key="vm"
                        class="btn-intent btn-choice-chip"
                        @click="sendMessage(vm)"
                      >
                        <i class="bi bi-bicycle me-1" style="color: #0284C7;"></i> {{ vm }}
                      </button>
                    </div>
                  </template>

                  <!-- SHIPPER KHU VỰC CHẠY CHÍNH (QUẬN, HUYỆN, XÃ CHỈ HIỆN TÊN KHÔNG CÓ NGOẶC ĐƠN + TẤT CẢ) -->
                  <template v-else-if="msg.actionIntent === 'SHIPPER_CHOOSE_AREA'">
                    <div class="chips-selector-grid area-chips-grid">
                      <button
                        v-for="area in shipperAreas"
                        :key="area"
                        class="btn-intent btn-choice-chip"
                        @click="sendMessage(area)"
                      >
                        <i class="bi bi-geo-alt-fill me-1" style="color: #EF4444;"></i> {{ area }}
                      </button>
                    </div>
                  </template>

                  <!-- Ô TẢI ẢNH GIẤY TỜ CHUẨN THIẾT KẾ VIỀN NÉT ĐỨT XANH (CCCD, ATTP, CHÂN DUNG, GPLX, ẢNH SỰ CỐ) -->
                  <template v-else-if="isUploadIntent(msg.actionIntent)">
                    <div class="chat-upload-box" @click="triggerUploadForIntent(msg.actionIntent!)">
                      <div class="upload-icon-circle">
                        <i class="bi bi-camera"></i>
                      </div>
                      <div class="upload-title">{{ getUploadTitle(msg.actionIntent) }}</div>
                      <div class="upload-sub">{{ getUploadSubtitle(msg.actionIntent) }}</div>
                      <div v-if="uploadedPreviews[msg.actionIntent!]" class="upload-preview-row">
                        <img :src="uploadedPreviews[msg.actionIntent!]" alt="Preview" class="upload-preview-thumb" />
                        <span class="upload-badge"><i class="bi bi-check2-circle me-1"></i> Đã tải ảnh lên</span>
                      </div>
                    </div>
                    <button
                      v-if="msg.actionIntent === 'SUPPORT_UPLOAD_ATTACHMENT' || msg.actionIntent === 'SUPPORT_EDIT_UPLOAD'"
                      class="btn-intent btn-intent-manual"
                      style="margin-top: 8px; width: 100%; justify-content: center;"
                      @click="sendMessage('Bỏ qua không gửi ảnh')"
                    >
                      <i class="bi bi-skip-forward-circle me-1"></i> Bỏ Qua Không Gửi Ảnh
                    </button>
                  </template>

                  <!-- ĐĂNG KÝ KHÁCH HÀNG THÀNH CÔNG: DÒNG NHỎ HỎI VÀ NÚT ĐĂNG NHẬP NGAY -->
                  <template v-else-if="msg.actionIntent === 'BUYER_REGISTER_SUCCESS'">
                    <div class="reg-success-actions">
                      <button class="btn-intent btn-intent-ai" @click="handleAction('buyer_login')">
                        <i class="bi bi-box-arrow-in-right me-1"></i> 🔑 Đăng Nhập Ngay
                      </button>
                    </div>
                  </template>

                  <!-- ĐĂNG KÝ SELLER & SHIPPER THÀNH CÔNG: NÚT QUAY VỀ TRANG CHỦ -->
                  <template v-else-if="msg.actionIntent === 'SELLER_REGISTER_SUCCESS' || msg.actionIntent === 'SHIPPER_REGISTER_SUCCESS'">
                    <div class="reg-success-actions">
                      <button class="btn-intent btn-intent-home" @click="handleAction('go_home')">
                        <i class="bi bi-house-door me-1"></i> 🏠 Quay Về Trang Chủ
                      </button>
                    </div>
                  </template>
                </div>

                <div class="msg-meta">
                  <span class="msg-time">{{ msg.timestamp }}</span>
                  <span v-if="msg.provider" class="msg-provider">{{ msg.provider }}</span>
                </div>
              </div>
            </div>

            <!-- Nút icon con mắt nằm NGOÀI bên TRÁI bong bóng chat mật khẩu người dùng -->
            <button
              v-if="msg.sender === 'user' && msg.isPassword"
              type="button"
              class="btn-user-outside-eye"
              :title="revealedPasswords[msg.id] ? 'Bấm để ẩn mật khẩu (••••••••)' : 'Bấm để xem mật khẩu thật'"
              @click.stop="toggleRevealPassword(msg.id)"
            >
              <i :class="revealedPasswords[msg.id] ? 'bi bi-eye' : 'bi bi-eye-slash'"></i>
            </button>
          </div>

          <!-- Typing Indicator khi đang chờ AI phản hồi -->
          <div v-if="isLoading" class="message-row row-ai">
            <div class="ai-avatar-mini">
              <div class="avatar-crop-box">
                <img src="/images/anhchattuvanAI_full_clean.png" alt="AI" />
              </div>
            </div>
            <div class="message-bubble typing-bubble">
              <span class="typing-dot"></span>
              <span class="typing-dot"></span>
              <span class="typing-dot"></span>
            </div>
          </div>
        </div>

        <!-- Input Chat (Tự động xuống dòng khi nhập tin nhắn dài, hoặc ô mật khẩu có mắt) -->
        <div class="chat-footer">
          <!-- Khi ở bước nhập mật khẩu: hiển thị input password kèm nút icon đôi mắt để che/hiện mật khẩu -->
          <div v-if="isPasswordStep" class="password-input-wrapper">
            <input
              ref="passwordInputRef"
              :type="showPasswordInput ? 'text' : 'password'"
              v-model="userMessage"
              class="chat-password-input"
              :placeholder="isConfirmPasswordStep ? 'Nhập lại mật khẩu để xác nhận trùng khớp...' : 'Nhập mật khẩu (từ 6 - 16 ký tự)...'"
              @keydown.enter.exact.prevent="sendMessage()"
            />
            <button
              type="button"
              class="password-eye-btn"
              :title="showPasswordInput ? 'Bấm để ẩn mật khẩu (***)' : 'Bấm để xem mật khẩu thật'"
              @click="showPasswordInput = !showPasswordInput"
            >
              <i :class="showPasswordInput ? 'bi bi-eye' : 'bi bi-eye-slash'"></i>
            </button>
          </div>
          <textarea
            v-else
            ref="textareaRef"
            v-model="userMessage"
            :placeholder="isRecordingVoice ? '🎙️ Đang lắng nghe giọng nói của bạn...' : 'Hỏi AI về món ăn, quán ngon, cách đăng ký...'"
            class="chat-textarea"
            rows="1"
            @input="adjustTextareaHeight"
            @keydown.enter.exact.prevent="sendMessage()"
          ></textarea>
          <button
            type="button"
            class="mic-btn"
            :class="{ 'is-recording': isRecordingVoice }"
            :title="isRecordingVoice ? 'Đang thu âm... Bấm để dừng và gửi' : 'Thu âm giọng nói gửi cho AI'"
            @click="toggleVoiceRecording"
          >
            <i :class="isRecordingVoice ? 'bi bi-mic-fill' : 'bi bi-mic'"></i>
          </button>
          <button
            class="send-btn"
            :disabled="!userMessage.trim() || isLoading"
            @click="sendMessage()"
          >
            <i class="bi bi-send-fill"></i>
          </button>
        </div>
      </div>
    </transition>
  </div>
  </Teleport>
</template>

<style scoped>
.ai-widget-wrapper {
  position: fixed;
  bottom: 4%;
  right: 3.5%;
  z-index: 9999;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
}

/* Container bao bọc avatar và sóng âm */
.trigger-container {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 58px;
  height: 58px;
}

/* Bong bóng lời thoại gợi ý thi thoảng xuất hiện phía trên avatar */
.ai-speech-bubble {
  position: absolute;
  right: 0;
  bottom: 74px;
  background: #FFFFFF;
  border: 1.5px solid #FED7AA;
  /* Đổ bóng đa tầng cực kỳ rõ nét & nổi bật (box-shadow) */
  box-shadow: 
    0 16px 36px -6px rgba(15, 23, 42, 0.18),
    0 8px 18px -4px rgba(249, 115, 22, 0.22),
    0 2px 8px rgba(0, 0, 0, 0.06);
  border-radius: 18px;
  padding: 9px 18px;
  cursor: pointer;
  white-space: nowrap;
  user-select: none;
  z-index: 10;
  transition: transform 0.25s cubic-bezier(0.34, 1.56, 0.64, 1), box-shadow 0.25s ease, border-color 0.25s ease;
}

.ai-speech-bubble:hover {
  transform: translateY(-2px) scale(1.02);
  box-shadow: 
    0 20px 42px -6px rgba(15, 23, 42, 0.22),
    0 10px 22px -4px rgba(249, 115, 22, 0.28),
    0 4px 10px rgba(0, 0, 0, 0.08);
  border-color: #FDBA74;
}

.bubble-content {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 22px;
}

.bubble-text {
  display: inline-block;
  font-size: 13.5px;
  font-weight: 600;
  color: #0f172a;
  letter-spacing: -0.01em;
  /* Bóng của chữ đoạn chat tạo độ nổi khối sắc nét */
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.06);
}

/* Đuôi bong bóng thoại chỉ thẳng xuống avatar bên dưới */
.bubble-tail {
  position: absolute;
  bottom: -8px;
  right: 22px;
  width: 0;
  height: 0;
  border-left: 7px solid transparent;
  border-right: 7px solid transparent;
  border-top: 8px solid #FFFFFF;
  filter: drop-shadow(0 3px 2px rgba(15, 23, 42, 0.16));
}

/* Hiệu ứng xuất hiện nhanh gọn và biến mất từ từ cực kỳ mượt mà */
.bubble-pop-enter-active {
  transition: all 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.bubble-pop-leave-active {
  transition: all 0.7s cubic-bezier(0.4, 0, 0.2, 1);
}

.bubble-pop-enter-from {
  opacity: 0;
  transform: scale(0.85) translateY(12px);
}

.bubble-pop-leave-to {
  opacity: 0;
  transform: scale(0.92) translateY(-8px);
}

/* Vòng sóng âm phát tỏa nhẹ nhàng xung quanh avatar */
.soundwave-wrapper {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 58px;
  height: 58px;
  pointer-events: none;
  z-index: 0;
}

.soundwave-ring {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  border: 2px solid rgba(249, 115, 22, 0.55);
  background: radial-gradient(circle, rgba(251, 146, 60, 0.18) 0%, rgba(249, 115, 22, 0) 70%);
  opacity: 0;
  animation: soundwave-pulse 2.8s cubic-bezier(0.215, 0.61, 0.355, 1) infinite;
}

.soundwave-ring.ring-1 {
  animation-delay: 0s;
}

.soundwave-ring.ring-2 {
  animation-delay: 0.9s;
}

.soundwave-ring.ring-3 {
  animation-delay: 1.8s;
}

@keyframes soundwave-pulse {
  0% {
    transform: scale(0.95);
    opacity: 0.85;
    border-color: rgba(249, 115, 22, 0.7);
  }
  50% {
    opacity: 0.45;
  }
  100% {
    transform: scale(1.85);
    opacity: 0;
    border-color: rgba(249, 115, 22, 0);
  }
}

/* Floating Avatar Button - Pure 58px Circle (Nhỏ lại 0.25x, đẩy sang trái 3.5%) */
.ai-trigger-btn {
  position: relative;
  z-index: 2;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 58px;
  height: 58px;
  background: #FFFFFF;
  border: 2.5px solid #FF6B2B;
  border-radius: 50%;
  padding: 3px;
  box-shadow: 0 10px 28px rgba(217, 78, 21, 0.35);
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.ai-trigger-btn:hover {
  transform: translateY(-4px) scale(1.08);
  border-color: #D94E15;
  box-shadow: 0 14px 35px rgba(217, 78, 21, 0.5);
}

.avatar-ring {
  position: relative;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  overflow: visible;
}

.img-crop-box,
.avatar-crop-box {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  overflow: hidden;
  background: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
}

.ai-avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
  transition: transform 0.3s ease;
}

.ai-trigger-btn:hover .ai-avatar-img {
  transform: scale(1.08);
}

.online-dot {
  position: absolute;
  bottom: -1px;
  right: -1px;
  width: 14px;
  height: 14px;
  background-color: #22C55E;
  border: 2px solid #FFFFFF;
  border-radius: 50%;
  box-shadow: 0 2px 5px rgba(0,0,0,0.25);
  z-index: 2;
}

/* Window Chat */
.ai-chat-window {
  position: absolute;
  bottom: 72px;
  right: 0;
  width: 395px;
  max-width: calc(100vw - 28px);
  height: 560px;
  max-height: calc(100vh - 120px);
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(16px);
  border: 1.5px solid #FED7AA;
  border-radius: 24px;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.15);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* Header */
.chat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 18px;
  background: linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 100%);
  border-bottom: 1px solid #FED7AA;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-avatar-wrap {
  position: relative;
  width: 42px;
  height: 42px;
}

.header-avatar-wrap .avatar-crop-box {
  border: 1.5px solid #D94E15;
}

.header-avatar {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.status-pulse {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 10px;
  height: 10px;
  background: #22C55E;
  border: 1.5px solid #FFFFFF;
  border-radius: 50%;
}

.header-title {
  margin: 0;
  font-size: 15px;
  font-weight: 800;
  color: #9A3412;
}

.header-status {
  margin: 2px 0 0 0;
  font-size: 11.5px;
  color: #166534;
  font-weight: 600;
}

.close-btn {
  background: rgba(255, 255, 255, 0.8);
  border: 1px solid #FED7AA;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #9A3412;
  cursor: pointer;
  transition: all 0.2s;
}

.close-btn:hover {
  background: #D94E15;
  color: #FFFFFF;
}

/* Chips Bar */
.quick-chips-bar {
  display: flex;
  gap: 8px;
  padding: 10px 14px;
  overflow-x: auto;
  background: #FFFBF7;
  border-bottom: 1px solid #F3E8FF;
  scrollbar-width: none;
}

.quick-chips-bar::-webkit-scrollbar {
  display: none;
}

.chip-item {
  background: #FFFFFF;
  border: 1px solid #FED7AA;
  border-radius: 20px;
  padding: 5px 12px;
  font-size: 12px;
  font-weight: 700;
  color: #C2410C;
  white-space: nowrap;
  cursor: pointer;
  transition: all 0.2s;
}

.chip-item:hover {
  background: #D94E15;
  color: #FFFFFF;
  border-color: #D94E15;
}

/* Messages Body */
.chat-messages-body {
  flex: 1;
  padding: 14px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 14px;
  background: #FAF8F5;
}

.message-row {
  display: flex;
  gap: 10px;
  align-items: flex-end;
}

.row-user {
  flex-direction: row-reverse;
}

.ai-avatar-mini {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
}

.ai-avatar-mini .avatar-crop-box {
  border: 1.5px solid #D94E15;
}

.ai-avatar-mini img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.message-bubble-wrap {
  max-width: 90%;
}

.message-bubble {
  padding: 12px 16px;
  border-radius: 18px;
  font-size: 13.5px;
  line-height: 1.55;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

.row-user .message-bubble {
  background: linear-gradient(135deg, #D94E15 0%, #EA580C 100%);
  color: #FFFFFF;
  border-bottom-right-radius: 4px;
}

.row-ai .message-bubble {
  background: #FFFFFF;
  color: #334155;
  border: 1px solid #F1F5F9;
  border-bottom-left-radius: 4px;
}

.msg-text {
  margin: 0;
  white-space: normal;
  word-break: break-word;
  line-height: 1.5;
}

:deep(.chat-divider) {
  border: none;
  border-top: 1px dashed #CBD5E1;
  margin: 8px 0;
}

:deep(.chat-code) {
  background: #F1F5F9;
  color: #D94E15;
  padding: 1px 6px;
  border-radius: 4px;
  font-family: monospace;
  font-size: 12px;
  font-weight: 600;
}

.row-user :deep(.chat-code) {
  background: rgba(255, 255, 255, 0.25);
  color: #FFFFFF;
}

.msg-meta {
  display: flex;
  gap: 8px;
  margin-top: 6px;
  font-size: 10.5px;
  opacity: 0.75;
}

.row-user .msg-meta {
  justify-content: flex-end;
}

/* Mini Cards Grid */
.cards-grid {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 10px;
}

.mini-card {
  display: flex;
  flex-direction: column;
  background: #FFFFFF;
  border: 1.5px solid #FED7AA;
  border-radius: 16px;
  padding: 10px 12px;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.06);
  transition: all 0.25s ease;
}

.mini-card:hover {
  border-color: #FDBA74;
  box-shadow: 0 8px 20px rgba(217, 78, 21, 0.15);
  transform: translateY(-2px);
}

.card-main-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.card-thumb {
  width: 58px;
  height: 58px;
  border-radius: 12px;
  object-fit: cover;
  flex-shrink: 0;
  border: 1px solid #FFEDD5;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.06);
}

.card-content {
  flex: 1;
  min-width: 0;
}

.card-title {
  margin: 0 0 4px 0;
  font-size: 13px;
  font-weight: 700;
  color: #1E293B;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  word-break: break-word;
}

.card-store {
  font-size: 11.5px;
  color: #64748B;
  display: flex;
  align-items: center;
}

.card-sub {
  margin: 0;
  font-size: 11.5px;
  color: #64748B;
}

.card-footer-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px dashed #FED7AA;
}

.card-price {
  font-size: 13.5px;
  font-weight: 800;
  color: #D94E15;
}

.btn-card-act {
  background: linear-gradient(135deg, #FF6B2B 0%, #D94E15 100%);
  color: #FFFFFF;
  border: none;
  padding: 5px 12px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 2px;
  box-shadow: 0 2px 6px rgba(217, 78, 21, 0.25);
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-card-act:hover {
  background: linear-gradient(135deg, #EA580C 0%, #C2410C 100%);
  transform: scale(1.04);
}

.rating-badge {
  font-size: 11px;
  font-weight: 700;
  color: #D97706;
  background: #FEF3C7;
  padding: 3px 8px;
  border-radius: 12px;
}

.action-buttons-wrap {
  margin-top: 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.action-buttons-wrap.actions-disabled {
  opacity: 0.42;
  filter: grayscale(0.9);
  pointer-events: none !important;
  cursor: not-allowed !important;
  user-select: none;
  transition: opacity 0.25s ease, filter 0.25s ease;
}

.action-buttons-wrap.actions-disabled button,
.action-buttons-wrap.actions-disabled .btn-intent,
.action-buttons-wrap.actions-disabled .btn-capability,
.action-buttons-wrap.actions-disabled .btn-choice-chip,
.action-buttons-wrap.actions-disabled .chat-upload-box {
  pointer-events: none !important;
  cursor: not-allowed !important;
  box-shadow: none !important;
  transform: none !important;
  background: #f1f5f9 !important;
  color: #94a3b8 !important;
  border-color: #cbd5e1 !important;
}

.action-buttons-wrap.actions-disabled i {
  color: #94a3b8 !important;
}

.action-completed-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11px;
  font-weight: 700;
  color: #64748b;
  background: #f8fafc;
  padding: 3px 8px;
  border-radius: 6px;
  width: fit-content;
  border: 1px dashed #cbd5e1;
  margin-bottom: 2px;
}

.btn-intent {
  width: 100%;
  padding: 8px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 800;
  border: none;
  cursor: pointer;
  color: #FFFFFF;
  transition: all 0.25s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  white-space: nowrap;
  box-shadow: 0 3px 10px rgba(0, 0, 0, 0.08);
}

.btn-intent-manual {
  background: linear-gradient(135deg, #EA580C 0%, #C2410C 100%);
}

.btn-intent-manual:hover {
  background: linear-gradient(135deg, #C2410C 0%, #9A3412 100%);
  transform: translateY(-1px);
}

.btn-intent-ai {
  background: linear-gradient(135deg, #7C3AED 0%, #4F46E5 100%);
}

.btn-intent-ai:hover {
  background: linear-gradient(135deg, #6D28D9 0%, #4338CA 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 14px rgba(124, 58, 237, 0.35);
}

.btn-capability {
  background: #FFFFFF;
  color: #1E293B;
  border: 1.5px solid #FED7AA;
  font-weight: 700;
  text-align: left;
  justify-content: flex-start;
  padding: 9px 12px;
  font-size: 12.5px;
  border-radius: 10px;
  transition: all 0.2s ease;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.04);
  white-space: normal;
  word-break: break-word;
  line-height: 1.35;
}

.btn-capability:hover {
  background: #FFF7ED;
  color: #D94E15;
  border-color: #D94E15;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(217, 78, 21, 0.15);
}

/* Typing indicator */
.typing-bubble {
  display: flex;
  gap: 4px;
  align-items: center;
  padding: 12px 18px;
}

.typing-dot {
  width: 6px;
  height: 6px;
  background: #D94E15;
  border-radius: 50%;
  animation: typingBounce 1.4s infinite ease-in-out both;
}

.typing-dot:nth-child(1) { animation-delay: -0.32s; }
.typing-dot:nth-child(2) { animation-delay: -0.16s; }

@keyframes typingBounce {
  0%, 80%, 100% { transform: scale(0); }
  40% { transform: scale(1.0); }
}

/* Footer Input */
.chat-footer {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  padding: 10px 12px;
  background: #FFFFFF;
  border-top: 1px solid #F1F5F9;
}

.chat-textarea {
  flex: 1;
  border: 1.5px solid #FED7AA;
  border-radius: 16px;
  padding: 8px 14px;
  font-size: 13px;
  line-height: 1.45;
  outline: none;
  resize: none !important; /* XÓA BỎ MŨI TÊN 2 CHIỀU / RESIZE HANDLE */
  overflow: hidden !important; /* XÓA BỎ THANH CUỘN / MŨI TÊN SCROLLBAR */
  height: 38px;
  min-height: 38px;
  max-height: 120px;
  font-family: inherit;
  transition: border-color 0.2s, height 0.15s ease-out;
  word-break: break-word;
  white-space: pre-wrap;
  box-sizing: border-box;
}

.chat-textarea:focus {
  border-color: #D94E15;
}

.send-btn {
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: opacity 0.2s;
}

.send-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.mic-btn {
  background: #f8fafc;
  color: #64748b;
  border: 1.5px solid #cbd5e1;
  width: 36px;
  height: 36px;
  min-width: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 16px;
  padding: 0;
}

.mic-btn:hover {
  background: #ffedd5;
  color: #ea580c;
  border-color: #fdba74;
}

.mic-btn.is-recording {
  background: #ef4444;
  color: #ffffff;
  border-color: #dc2626;
  animation: pulse-mic 1.2s infinite ease-in-out;
}

@keyframes pulse-mic {
  0% {
    box-shadow: 0 0 0 0 rgba(239, 68, 68, 0.7);
    transform: scale(1);
  }
  50% {
    box-shadow: 0 0 0 8px rgba(239, 68, 68, 0);
    transform: scale(1.08);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(239, 68, 68, 0);
    transform: scale(1);
  }
}

/* Password input with eye toggle */
.password-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  flex: 1;
  background: #FFFFFF;
  border: 1.5px solid #FED7AA;
  border-radius: 16px;
  padding: 2px 6px 2px 14px;
  height: 38px;
  box-sizing: border-box;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.password-input-wrapper:focus-within {
  border-color: #D94E15;
  box-shadow: 0 0 0 3px rgba(217, 78, 21, 0.15);
}

.chat-password-input {
  flex: 1;
  border: none;
  outline: none;
  background: transparent;
  font-size: 13px;
  color: #1E293B;
  padding: 4px 0;
  font-family: inherit;
}

.password-eye-btn {
  background: transparent;
  border: none;
  outline: none;
  color: #64748B;
  padding: 4px 8px;
  cursor: pointer;
  font-size: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  transition: color 0.15s, background-color 0.15s;
}

.password-eye-btn:hover {
  color: #EA580C;
  background-color: #FFEDD5;
}

/* Document Upload Box (Dashed Green Box matching screenshot) */
.chat-upload-box {
  margin-top: 10px;
  margin-bottom: 6px;
  padding: 16px 14px;
  background: #F0FDF4;
  border: 2px dashed #16A34A;
  border-radius: 12px;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s ease;
  user-select: none;
}

.chat-upload-box:hover {
  background: #DCFCE7;
  border-color: #15803D;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(22, 163, 74, 0.15);
}

.upload-icon-circle {
  width: 44px;
  height: 44px;
  background: #16A34A;
  color: #FFFFFF;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  margin: 0 auto 8px auto;
  box-shadow: 0 2px 6px rgba(22, 163, 74, 0.3);
}

.upload-title {
  font-weight: 700;
  font-size: 13px;
  color: #15803D;
  margin-bottom: 2px;
}

.upload-sub {
  font-size: 11.5px;
  color: #4B5563;
}

.upload-preview-row {
  margin-top: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.upload-preview-thumb {
  width: 60px;
  height: 45px;
  object-fit: cover;
  border-radius: 6px;
  border: 1.5px solid #16A34A;
}

.upload-badge {
  font-size: 11.5px;
  font-weight: 600;
  color: #15803D;
  background: #FFFFFF;
  padding: 3px 8px;
  border-radius: 12px;
  border: 1px solid #86EFAC;
}

/* Inline Masked Password with Eye Toggle in Chat bubble */
:deep(.inline-password-tag) {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #F1F5F9;
  border: 1px solid #CBD5E1;
  padding: 1px 8px;
  border-radius: 6px;
  font-family: monospace;
  font-weight: 700;
  color: #0F172A;
  margin: 0 2px;
}

/* Mật khẩu người dùng hiển thị trực tiếp trong bong bóng chat với chữ trắng sắc nét */
.user-pwd-plain {
  display: inline-block;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 15px;
  font-weight: 700;
  letter-spacing: 2px;
  color: #FFFFFF;
  line-height: 1.4;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
}

/* Nút mắt nằm bên ngoài, cạnh trái khung chat mật khẩu người dùng */
.btn-user-outside-eye {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: #FFFFFF;
  border: 1.5px solid #FED7AA;
  color: #EA580C;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  cursor: pointer;
  flex-shrink: 0;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08);
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  margin-bottom: 2px;
}

.btn-user-outside-eye:hover {
  background: #FFF7ED;
  border-color: #EA580C;
  color: #C2410C;
  transform: scale(1.12);
  box-shadow: 0 4px 10px rgba(234, 88, 12, 0.25);
}

:deep(.btn-eye-toggle-inline) {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 0 2px;
  color: #64748B;
  font-size: 14px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: color 0.15s;
}

:deep(.btn-eye-toggle-inline:hover) {
  color: #EA580C;
}

/* Huy hiệu câu luồng hỏi được code bằng Bootstrap Icon đẹp mắt */
:deep(.q-step-badge) {
  display: inline-flex;
  align-items: center;
  background: linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 100%);
  border: 1px solid #FED7AA;
  color: #C2410C;
  font-weight: 700;
  font-size: 11.5px;
  line-height: 1.35;
  padding: 1px 7px;
  border-radius: 12px;
  margin-right: 5px;
  vertical-align: 1px;
  box-shadow: 0 1px 2px rgba(234, 88, 12, 0.06);
}

:deep(.q-step-badge i) {
  color: #EA580C;
  font-size: 11.5px;
}

/* Các dòng tổng kết hồ sơ có icon và khoảng cách giãn thoáng đẹp */
:deep(.summary-line-item) {
  display: flex;
  align-items: center;
  gap: 10px;
  margin: 7px 0;
  line-height: 1.55;
  font-size: 13px;
  word-break: break-word;
}

:deep(.summary-code-icon) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 8px;
  font-size: 14.5px;
  flex-shrink: 0;
  border: 1px solid rgba(0, 0, 0, 0.06);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

:deep(.summary-code-icon i) {
  font-size: 15px;
}

:deep(.code-inline-icon) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  margin-left: 3px;
  vertical-align: -1px;
}

/* Tiêu đề bảng tổng hợp hồ sơ / hỗ trợ */
:deep(.summary-table-header) {
  display: inline-flex;
  align-items: center;
  font-weight: 800;
  font-size: 13.5px;
  color: #1E293B;
  background: #F8FAFC;
  padding: 6px 12px;
  border-radius: 8px;
  border: 1px solid #E2E8F0;
  margin-bottom: 8px;
  letter-spacing: 0.2px;
}

:deep(.summary-table-header.success-header) {
  background: #ECFDF5;
  border-color: #A7F3D0;
  color: #065F46;
}

/* Khung cách 1 & cách 2 */
:deep(.method-step-item) {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 6px 0;
  font-size: 13px;
  color: #1E293B;
}

:deep(.method-code-icon) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  border-radius: 6px;
  font-size: 13.5px;
  flex-shrink: 0;
  border: 1px solid rgba(0, 0, 0, 0.06);
}

:deep(.summary-line-icon) {
  flex-shrink: 0;
  font-size: 15px;
  line-height: 1.4;
  margin-top: 1px;
}

:deep(.summary-line-text) {
  flex: 1;
}

/* Interactive Choice Chips */
.chips-selector-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 8px;
  margin-bottom: 4px;
}

.area-chips-grid {
  max-height: 220px;
  overflow-y: auto;
  padding-right: 4px;
}

.btn-choice-chip {
  background: #FFFFFF;
  color: #1E293B;
  border: 1px solid #CBD5E1;
  border-radius: 18px;
  padding: 6px 12px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
  display: inline-flex;
  align-items: center;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.btn-choice-chip:hover {
  background: #FFF7ED;
  color: #C2410C;
  border-color: #FDBA74;
  transform: translateY(-1px);
  box-shadow: 0 3px 8px rgba(249, 115, 22, 0.18);
}

.reg-success-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 10px;
}

.btn-intent-home {
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #FFFFFF;
  border: none;
  box-shadow: 0 3px 10px rgba(2, 132, 199, 0.25);
  font-weight: 800;
}

.btn-intent-home:hover {
  background: linear-gradient(135deg, #0369A1 0%, #075985 100%);
  color: #FFFFFF;
  transform: translateY(-1px);
}

/* Animations */
.chat-slide-enter-active,
.chat-slide-leave-active {
  transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.chat-slide-enter-from,
.chat-slide-leave-to {
  opacity: 0;
  transform: translateY(20px) scale(0.95);
}
</style>
