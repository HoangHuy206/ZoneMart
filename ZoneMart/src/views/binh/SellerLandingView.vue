<script setup lang="ts">
/**
 * ================================================================
 * TRANG CHỦ & ĐĂNG KÝ GIAN HÀNG (ZONEMART SELLER PORTAL) - Phụ trách: Bình
 * - Logo thương hiệu: ZoneMart SELLER (Giao diện sạch không khung box)
 * - Tông màu chủ đạo: Cam ấm ZoneMart (#D94E15), Đỏ sẫm (#291811), Vàng kim (#FFF7ED)
 * - 100% Icon được vẽ bằng SVG Code sắc nét & hiệu ứng hiện đại.
 * - Công cụ ước tính doanh thu gian hàng số tương tác (Revenue Estimator).
 * - 6 Quyền lợi độc quyền, Quy trình 3 bước mở shop, Bảng Đăng Ký Mở Gian Hàng.
 * - Trung tâm hỗ trợ Gian Hàng gửi yêu cầu trực tiếp về Gmail (hh9393100@gmail.com).
 * - Footer đầy đủ chuẩn ZoneMart dành riêng cho Seller.
 * ================================================================
 */
import { ref, reactive, computed, watch, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

// Trạng thái Bước Form Đăng Ký (Step 1 -> Step 2 -> Step 3)
const currentStep = ref<1 | 2 | 3>(1);

// Form State Đăng ký Gian Hàng
const form = reactive({
  // BƯỚC 1: THÔNG TIN TIỆM
  storeName: "",
  category: "Thực phẩm & Nhu yếu phẩm",
  address: "",
  openHours: "07:00 - 22:00",
  phoneEmail: "",
  password: "",
  confirmPassword: "",

  // BƯỚC 2: XÁC THỰC CHỦ TIỆM & GIẤY TỜ
  ownerFullName: "",
  cccdFrontImage: "",
  cccdBackImage: "",
  foodSafetyCertImage: "",

  // BƯỚC 3: THANH TOÁN
  bankName: "Vietcombank (VCB)",
  bankAccountNumber: ""
});

const showPassword = ref(false);
const showConfirmPassword = ref(false);
const isAgreedToTerms = ref(false);

const isLoading = ref(false);
const errorMessage = ref("");
const showSuccessModal = ref(false);

// Custom Dropdowns state
const isCategoryDropdownOpen = ref(false);
const isOpenHoursDropdownOpen = ref(false);
const isBankDropdownOpen = ref(false);

const categoriesList = [
  "Thực phẩm & Nhu yếu phẩm",
  "Trái cây & Đồ uống",
  "Cơm & Thức ăn nhanh",
  "Đồ gia dụng tiện ích",
  "Thời trang & Phụ kiện",
  "Mỹ phẩm & Chăm sóc cá nhân"
];

const openHoursOptions = [
  "07:00 - 22:00 (Mở cả ngày & Buổi tối)",
  "06:00 - 21:00 (Mở sớm - Đóng sớm)",
  "08:00 - 20:00 (Giờ hành chính)",
  "07:00 - 23:00 (Phục vụ đêm)",
  "24/24 (Mở cửa 24/7 toàn thời gian)",
  "Tùy chỉnh khung giờ (Tự chọn giờ mở & đóng)"
];

const selectedPreset = ref("07:00 - 22:00 (Mở cả ngày & Buổi tối)");
const customOpenTime = ref("07:00");
const customCloseTime = ref("22:00");

const isCustomOpenHours = computed(() => selectedPreset.value.includes("Tùy chỉnh"));

// Danh sách các mốc thời gian từ 05:00 sáng đến 02:00 đêm (mỗi mốc cách nhau 30 phút)
const timeSlots = [
  { value: "05:00", minutes: 300, label: "05:00 (Sáng)" },
  { value: "05:30", minutes: 330, label: "05:30 (Sáng)" },
  { value: "06:00", minutes: 360, label: "06:00 (Sáng)" },
  { value: "06:30", minutes: 390, label: "06:30 (Sáng)" },
  { value: "07:00", minutes: 420, label: "07:00 (Sáng)" },
  { value: "07:30", minutes: 450, label: "07:30 (Sáng)" },
  { value: "08:00", minutes: 480, label: "08:00 (Sáng)" },
  { value: "08:30", minutes: 510, label: "08:30 (Sáng)" },
  { value: "09:00", minutes: 540, label: "09:00 (Sáng)" },
  { value: "09:30", minutes: 570, label: "09:30 (Sáng)" },
  { value: "10:00", minutes: 600, label: "10:00 (Sáng)" },
  { value: "10:30", minutes: 630, label: "10:30 (Sáng)" },
  { value: "11:00", minutes: 660, label: "11:00 (Trưa)" },
  { value: "11:30", minutes: 690, label: "11:30 (Trưa)" },
  { value: "12:00", minutes: 720, label: "12:00 (Trưa)" },
  { value: "12:30", minutes: 750, label: "12:30 (Chiều)" },
  { value: "13:00", minutes: 780, label: "13:00 (Chiều)" },
  { value: "13:30", minutes: 810, label: "13:30 (Chiều)" },
  { value: "14:00", minutes: 840, label: "14:00 (Chiều)" },
  { value: "14:30", minutes: 870, label: "14:30 (Chiều)" },
  { value: "15:00", minutes: 900, label: "15:00 (Chiều)" },
  { value: "15:30", minutes: 930, label: "15:30 (Chiều)" },
  { value: "16:00", minutes: 960, label: "16:00 (Chiều)" },
  { value: "16:30", minutes: 990, label: "16:30 (Chiều)" },
  { value: "17:00", minutes: 1020, label: "17:00 (Chiều)" },
  { value: "17:30", minutes: 1050, label: "17:30 (Chiều)" },
  { value: "18:00", minutes: 1080, label: "18:00 (Tối)" },
  { value: "18:30", minutes: 1110, label: "18:30 (Tối)" },
  { value: "19:00", minutes: 1140, label: "19:00 (Tối)" },
  { value: "19:30", minutes: 1170, label: "19:30 (Tối)" },
  { value: "20:00", minutes: 1200, label: "20:00 (Tối)" },
  { value: "20:30", minutes: 1230, label: "20:30 (Tối)" },
  { value: "21:00", minutes: 1260, label: "21:00 (Tối)" },
  { value: "21:30", minutes: 1290, label: "21:30 (Tối)" },
  { value: "22:00", minutes: 1320, label: "22:00 (Đêm)" },
  { value: "22:30", minutes: 1350, label: "22:30 (Đêm)" },
  { value: "23:00", minutes: 1380, label: "23:00 (Đêm)" },
  { value: "23:30", minutes: 1410, label: "23:30 (Đêm)" },
  { value: "00:00", minutes: 1440, label: "00:00 (Đêm)" },
  { value: "00:30", minutes: 1470, label: "00:30 (Đêm)" },
  { value: "01:00", minutes: 1500, label: "01:00 (Đêm)" },
  { value: "01:30", minutes: 1530, label: "01:30 (Đêm)" },
  { value: "02:00", minutes: 1560, label: "02:00 (Đêm)" }
];

// Danh sách các mốc mở cửa (từ 05:00 sáng đến 22:00)
const openTimeOptions = computed(() => {
  return timeSlots.filter(t => t.minutes <= 1320);
});

// Danh sách các mốc đóng cửa phải cách giờ mở cửa ít nhất 4 tiếng (240 phút)
const closeTimeOptions = computed(() => {
  const currentOpenObj = timeSlots.find(t => t.value === customOpenTime.value);
  const minCloseMinutes = (currentOpenObj?.minutes ?? 420) + 240;
  return timeSlots.filter(t => t.minutes >= minCloseMinutes);
});

const onOpenTimeChange = () => {
  const validCloses = closeTimeOptions.value;
  const currentCloseObj = timeSlots.find(t => t.value === customCloseTime.value);
  const openObj = timeSlots.find(t => t.value === customOpenTime.value);
  
  if (!currentCloseObj || !openObj || (currentCloseObj.minutes - openObj.minutes < 240)) {
    if (validCloses.length > 0) {
      customCloseTime.value = validCloses[0].value;
    }
  }
  updateCustomOpenHours();
};

const updateCustomOpenHours = () => {
  if (isCustomOpenHours.value) {
    form.openHours = `${customOpenTime.value} - ${customCloseTime.value}`;
  }
};

const banksList = [
  "Vietcombank (VCB)",
  "MB Bank (MB)",
  "Techcombank (TCB)",
  "VietinBank (CTG)",
  "BIDV",
  "VPBank",
  "Agribank",
  "TPBank"
];

const isOpenTimeDropdownOpen = ref(false);
const isCloseTimeDropdownOpen = ref(false);

const selectedOpenTimeLabel = computed(() => {
  return timeSlots.find(t => t.value === customOpenTime.value)?.label || customOpenTime.value;
});

const selectedCloseTimeLabel = computed(() => {
  return timeSlots.find(t => t.value === customCloseTime.value)?.label || customCloseTime.value;
});

const toggleCategoryDropdown = () => {
  isCategoryDropdownOpen.value = !isCategoryDropdownOpen.value;
  isOpenHoursDropdownOpen.value = false;
  isBankDropdownOpen.value = false;
  isOpenTimeDropdownOpen.value = false;
  isCloseTimeDropdownOpen.value = false;
};

const selectCategory = (cat: string) => {
  form.category = cat;
  isCategoryDropdownOpen.value = false;
};

const toggleOpenHoursDropdown = () => {
  isOpenHoursDropdownOpen.value = !isOpenHoursDropdownOpen.value;
  isCategoryDropdownOpen.value = false;
  isBankDropdownOpen.value = false;
  isOpenTimeDropdownOpen.value = false;
  isCloseTimeDropdownOpen.value = false;
};

const selectOpenHours = (preset: string) => {
  selectedPreset.value = preset;
  if (preset.includes("Tùy chỉnh")) {
    form.openHours = `${customOpenTime.value} - ${customCloseTime.value}`;
  } else if (preset.includes("24/24")) {
    form.openHours = "24/24";
  } else {
    form.openHours = preset.split(" (")[0];
  }
  isOpenHoursDropdownOpen.value = false;
};

const toggleOpenTimeDropdown = () => {
  isOpenTimeDropdownOpen.value = !isOpenTimeDropdownOpen.value;
  isCloseTimeDropdownOpen.value = false;
  isCategoryDropdownOpen.value = false;
  isOpenHoursDropdownOpen.value = false;
  isBankDropdownOpen.value = false;
};

const toggleCloseTimeDropdown = () => {
  isCloseTimeDropdownOpen.value = !isCloseTimeDropdownOpen.value;
  isOpenTimeDropdownOpen.value = false;
  isCategoryDropdownOpen.value = false;
  isOpenHoursDropdownOpen.value = false;
  isBankDropdownOpen.value = false;
};

const selectCustomOpenTime = (timeVal: string) => {
  customOpenTime.value = timeVal;
  isOpenTimeDropdownOpen.value = false;
  onOpenTimeChange();
};

const selectCustomCloseTime = (timeVal: string) => {
  customCloseTime.value = timeVal;
  isCloseTimeDropdownOpen.value = false;
  updateCustomOpenHours();
};

const toggleBankDropdown = () => {
  isBankDropdownOpen.value = !isBankDropdownOpen.value;
  isCategoryDropdownOpen.value = false;
  isOpenHoursDropdownOpen.value = false;
  isOpenTimeDropdownOpen.value = false;
  isCloseTimeDropdownOpen.value = false;
};

const selectBank = (bank: string) => {
  form.bankName = bank;
  isBankDropdownOpen.value = false;
};

// Đóng dropdown khi click outside
const handleDocumentClick = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (!target.closest('.custom-select-container')) {
    isCategoryDropdownOpen.value = false;
    isOpenHoursDropdownOpen.value = false;
    isBankDropdownOpen.value = false;
    isOpenTimeDropdownOpen.value = false;
    isCloseTimeDropdownOpen.value = false;
  }
};

// ================= MASCOT CHUẨN NILBUILD/PAGE-MASCOT (ĐẦU 3D XOAY THEO CHUỘT) =================
const DIRECTIONS = [
  'up-left',
  'up',
  'up-right',
  'left',
  'center',
  'right',
  'down-left',
  'down',
  'down-right',
] as const;

const REACTIONS = [
  'blink',
  'heart',
  'sparkle',
  'surprised',
  'wink',
  'bashful',
  'sleepy',
  'dizzy',
  'delighted',
] as const;

type Direction = (typeof DIRECTIONS)[number];
type Reaction = (typeof REACTIONS)[number];

const CLOCKWISE: Direction[] = [
  'right',
  'down-right',
  'down',
  'down-left',
  'left',
  'up-left',
  'up',
  'up-right',
];

const SECTOR = (Math.PI * 2) / CLOCKWISE.length;
const HYSTERESIS = 0.12;
const DEAD_ZONE = 60;

const PAYOFFS: Reaction[] = ['heart', 'sparkle', 'delighted', 'wink', 'surprised'];
const BOOP_PAYOFF = 120;
const BOOP_END = 600;
const DIZZY_AFTER = 4;
const DIZZY_WINDOW = 1600;
const DIZZY_END = 1100;

// Mascot character: 'chef' (tiệm ẩm thực / đối tác bán hàng)
const activeMascot = ref<'chef' | 'cap' | 'drone' | 'otter'>('chef');
const mascotDirectionsUrl = computed(() => `/mascots/${activeMascot.value}-directions.webp`);
const mascotReactionsUrl = computed(() => `/mascots/${activeMascot.value}-reactions.webp`);

const mascotButtonRef = ref<HTMLElement | null>(null);
const currentDirection = ref<Direction>('center');
const currentReaction = ref<Reaction | null>(null);
const isSquashing = ref(false);

const mascotTipsByStep: Record<number, string[]> = {
  1: [
    "Chào bạn! Hãy đặt tên tiệm thật độc đáo nhé! 🛒✨",
    "Địa chỉ chi tiết giúp shipper tìm shop nhận hàng cực nhanh! 📍⚡",
    "ZoneMart đồng hành cùng bạn tiếp cận hàng triệu khách! 🧡"
  ],
  2: [
    "Chủ tiệm chụp CCCD 2 mặt thật rõ nét nha! 🪪📸",
    "Đừng quên ảnh giấy chứng nhận an toàn thực phẩm nhé! 📑🥗",
    "Hồ sơ của bạn được ZoneMart mã hóa bảo mật 100%! 🔒"
  ],
  3: [
    "Nhập số tài khoản chính xác để nhận tiền doanh thu tự động! 💳💰",
    "Chỉ còn bước cuối này thôi là tiệm sẽ được duyệt mở ngay! 🎉🏪",
    "ZoneMart chúc shop khai trương buôn may bán đắt, ngập đơn! 🚀🔥"
  ]
};

const currentTipIndex = ref(0);
const customMascotMessage = ref<string | null>(null);
const isBubblePopping = ref(false);
const isMascotAlert = ref(false);

const currentMascotMsg = computed(() => {
  if (customMascotMessage.value) return customMascotMessage.value;
  const list = mascotTipsByStep[currentStep.value] || mascotTipsByStep[1];
  return list[currentTipIndex.value % list.length];
});

// Tính toán vị trí góc nhìn đầu trong sprite sheet 3x3
const directionStyle = computed(() => {
  const index = DIRECTIONS.indexOf(currentDirection.value);
  const posX = (index % 3) * 50;
  const posY = Math.floor(index / 3) * 50;
  return {
    backgroundPosition: `${posX}% ${posY}%`,
    opacity: currentReaction.value ? 0 : 1
  };
});

// Biểu cảm khi được click (poke / boop)
const reactionStyle = computed(() => {
  const index = REACTIONS.indexOf(currentReaction.value ?? 'blink');
  const posX = (index % 3) * 50;
  const posY = Math.floor(index / 3) * 50;
  return {
    backgroundPosition: `${posX}% ${posY}%`,
    opacity: currentReaction.value ? 1 : 0
  };
});

let boopCount = 0;
let lastBoopAt = 0;
let reactionTimer: any = null;

const handleMascotBoop = () => {
  if (reactionTimer) clearTimeout(reactionTimer);

  const now = Date.now();
  boopCount = now - lastBoopAt < DIZZY_WINDOW ? boopCount + 1 : 1;
  lastBoopAt = now;

  // Hiệu ứng nhún nảy (squash physics)
  isSquashing.value = true;
  setTimeout(() => {
    isSquashing.value = false;
  }, 420);

  if (boopCount >= DIZZY_AFTER) {
    boopCount = 0;
    currentReaction.value = 'dizzy';
    reactionTimer = setTimeout(() => {
      currentReaction.value = null;
    }, DIZZY_END);
  } else {
    currentReaction.value = 'blink';
    reactionTimer = setTimeout(() => {
      currentReaction.value = PAYOFFS[(boopCount - 1) % PAYOFFS.length];
      reactionTimer = setTimeout(() => {
        currentReaction.value = null;
      }, BOOP_END);
    }, BOOP_PAYOFF);
  }

  isBubblePopping.value = true;
  currentTipIndex.value = currentTipIndex.value + 1;
  setTimeout(() => {
    isBubblePopping.value = false;
  }, 250);
};

// Tự động kích hoạt Mascot hiển thị cảnh báo khi người dùng nhập thiếu hoặc sai
watch(errorMessage, (newVal) => {
  if (newVal) {
    customMascotMessage.value = `⚠️ ${newVal}`;
    isMascotAlert.value = true;
    isBubblePopping.value = true;
    currentReaction.value = 'surprised'; // Đầu mascot ngạc nhiên chú ý
    isSquashing.value = true;
    setTimeout(() => {
      isSquashing.value = false;
      isBubblePopping.value = false;
    }, 380);
    setTimeout(() => {
      if (currentReaction.value === 'surprised') {
        currentReaction.value = null;
      }
    }, 3500);
  } else {
    isMascotAlert.value = false;
    customMascotMessage.value = null;
  }
});

// Khi người dùng chỉnh sửa dữ liệu, tự động xóa cảnh báo cũ
watch(
  [
    () => form.storeName,
    () => form.address,
    () => form.ownerFullName,
    () => form.cccdFrontImage,
    () => form.cccdBackImage,
    () => form.foodSafetyCertImage,
    () => form.bankAccountNumber
  ],
  () => {
    if (isMascotAlert.value) {
      errorMessage.value = "";
    }
  }
);

// Cập nhật câu nói chào mừng khi đổi bước form
watch(currentStep, () => {
  currentTipIndex.value = 0;
  customMascotMessage.value = null;
  isMascotAlert.value = false;
  isBubblePopping.value = true;
  setTimeout(() => {
    isBubblePopping.value = false;
  }, 250);
});

let lastSector = -1;
function wrap(angle: number) {
  return Math.atan2(Math.sin(angle), Math.cos(angle));
}

// Hàm xoay hướng đầu theo vị trí con trỏ chuột chuẩn nilbuild/page-mascot
function handlePointerMove(e: PointerEvent | MouseEvent) {
  const button = mascotButtonRef.value;
  if (!button) return;

  const box = button.getBoundingClientRect();
  const dx = e.clientX - (box.left + box.width / 2);
  const dy = e.clientY - (box.top + box.height / 2);

  if (Math.hypot(dx, dy) < DEAD_ZONE) {
    lastSector = -1;
    currentDirection.value = 'center';
    return;
  }

  const angle = Math.atan2(dy, dx);
  if (lastSector !== -1 && Math.abs(wrap(angle - lastSector * SECTOR)) < SECTOR / 2 + HYSTERESIS) {
    return;
  }

  lastSector = (Math.round(angle / SECTOR) + CLOCKWISE.length) % CLOCKWISE.length;
  currentDirection.value = CLOCKWISE[lastSector];
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick);
  window.addEventListener("pointermove", handlePointerMove, { passive: true });
});

onUnmounted(() => {
  document.removeEventListener('click', handleDocumentClick);
  window.removeEventListener("pointermove", handlePointerMove);
});

// Xử lý Chuyển bước
const goToNextStep = () => {
  errorMessage.value = "";

  if (currentStep.value === 1) {
    if (!form.storeName.trim() || form.storeName.length < 12 || form.storeName.length > 30) {
      errorMessage.value = "Tên cửa hàng phải từ 12 đến 30 ký tự!";
      return;
    }
    if (!form.address.trim()) {
      errorMessage.value = "Vui lòng nhập Địa chỉ tiệm!";
      return;
    }
    const cleanEmail = form.phoneEmail.trim().toLowerCase();
    if (!cleanEmail || !cleanEmail.endsWith('@gmail.com')) {
      errorMessage.value = "Địa chỉ Gmail liên hệ không hợp lệ! (Bắt buộc phải đuôi @gmail.com)";
      return;
    }
    if (!form.password.trim() || form.password.length < 6 || form.password.length > 16) {
      errorMessage.value = "Mật khẩu tạo tài khoản phải từ 6 đến 16 ký tự!";
      return;
    }
    if (form.password.trim() !== form.confirmPassword.trim()) {
      errorMessage.value = "Mật khẩu nhập lại không trùng khớp! Vui lòng kiểm tra lại.";
      return;
    }
    if (!form.openHours.trim()) {
      errorMessage.value = "Vui lòng chọn Khung giờ mở cửa kinh doanh!";
      return;
    }
    if (isCustomOpenHours.value) {
      const openObj = timeSlots.find(t => t.value === customOpenTime.value);
      const closeObj = timeSlots.find(t => t.value === customCloseTime.value);
      if (!openObj || !closeObj || (closeObj.minutes - openObj.minutes < 240)) {
        errorMessage.value = "Thời gian mở cửa và đóng cửa tiệm phải cách nhau ít nhất 4 tiếng!";
        return;
      }
    }
    currentStep.value = 2;
  } else if (currentStep.value === 2) {
    if (!form.ownerFullName.trim()) {
      errorMessage.value = "Vui lòng nhập Họ tên chủ tiệm!";
      return;
    }
    if (!form.cccdFrontImage || !form.cccdBackImage || !form.foodSafetyCertImage) {
      errorMessage.value = "Vui lòng tải đủ 3 ảnh: CCCD Mặt trước, Mặt sau và Giấy CN An toàn thực phẩm!";
      return;
    }
    currentStep.value = 3;
  }
};

const goToPrevStep = () => {
  errorMessage.value = "";
  if (currentStep.value > 1) {
    currentStep.value = (currentStep.value - 1) as 1 | 2 | 3;
  }
};

// Xử lý Upload Ảnh
const handleFileUpload = (e: Event, field: 'cccdFrontImage' | 'cccdBackImage' | 'foodSafetyCertImage') => {
  const file = (e.target as HTMLInputElement).files?.[0];
  if (!file) return;

  if (file.type.startsWith('video/')) {
    errorMessage.value = "Chỉ chấp nhận tệp hình ảnh (.jpg, .png, .webp). Không tải file video!";
    return;
  }

  const reader = new FileReader();
  reader.onload = (event) => {
    form[field] = event.target?.result as string;
  };
  reader.readAsDataURL(file);
};

// Gửi Form Đăng Ký Gian Hàng
const handleSubmitSeller = async () => {
  errorMessage.value = "";
  if (!isAgreedToTerms.value) {
    errorMessage.value = "Vui lòng tích chọn cam kết tuân thủ quy định trước khi nộp hồ sơ!";
    return;
  }
  const accountNumberTrimmed = form.bankAccountNumber.trim();
  if (!accountNumberTrimmed || !/^\d{8,15}$/.test(accountNumberTrimmed)) {
    errorMessage.value = "Số tài khoản ngân hàng phải từ 8 đến 15 chữ số!";
    return;
  }

  isLoading.value = true;

  try {
    await fetch("/api/auth/register-seller", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        storeName: form.storeName.trim(),
        category: form.category,
        address: form.address.trim(),
        openHours: form.openHours.trim(),
        phoneEmail: form.phoneEmail.trim(),
        password: form.password.trim(),
        ownerFullName: form.ownerFullName.trim(),
        cccdFrontImage: form.cccdFrontImage,
        cccdBackImage: form.cccdBackImage,
        foodSafetyCertImage: form.foodSafetyCertImage,
        bankName: form.bankName,
        bankAccountNumber: accountNumberTrimmed
      })
    }).catch(() => null);

    showSuccessModal.value = true;
  } catch (err: any) {
    errorMessage.value = err.message || "Có lỗi xảy ra khi gửi đơn đăng ký.";
  } finally {
    isLoading.value = false;
  }
};

const closeSuccessModal = () => {
  showSuccessModal.value = false;
  currentStep.value = 1;
  form.storeName = "";
  form.address = "";
  form.phoneEmail = "";
  form.password = "";
  form.confirmPassword = "";
  form.ownerFullName = "";
  form.cccdFrontImage = "";
  form.cccdBackImage = "";
  form.foodSafetyCertImage = "";
  form.bankAccountNumber = "";
  form.openHours = "07:00 - 22:00";
  selectedPreset.value = "07:00 - 22:00 (Mở cả ngày & Buổi tối)";
  customOpenTime.value = "07:00";
  customCloseTime.value = "22:00";
  isAgreedToTerms.value = false;

  router.push("/register-seller");
  window.scrollTo({ top: 0, behavior: "smooth" });
};

const scrollToRegisterForm = () => {
  const el = document.getElementById("register-form-section");
  if (el) {
    el.scrollIntoView({ behavior: "smooth" });
  }
};

// Công cụ ước tính doanh thu gian hàng số (Interactive Revenue Estimator)
const dailyOrders = ref(50);
const estimatedRevenue = computed(() => {
  const avgOrderValue = 150000;
  const monthlyTotal = dailyOrders.value * avgOrderValue * 30;
  return monthlyTotal.toLocaleString('vi-VN') + ' đ';
});

// Accordion FAQ State
const openFaqIndex = ref<number | null>(0);
const toggleFaq = (index: number) => {
  openFaqIndex.value = openFaqIndex.value === index ? null : index;
};

const sellerFaqs = [
  {
    q: "Mở gian hàng trên ZoneMart Seller có mất phí khởi tạo hay duy trì không?",
    a: "Hoàn toàn miễn phí 100%! ZoneMart cam kết không thu bất kỳ khoản phí khởi tạo hay phí duy trì hàng tháng nào từ nhà vườn và tiểu thương."
  },
  {
    q: "Tôi cần chuẩn bị những giấy tờ gì để đăng ký mở gian hàng số?",
    a: "Bạn chỉ cần chuẩn bị 3 thông tin cơ bản: Căn cước công dân (CCCD 12 số) chủ tiệm, Giấy chứng nhận An toàn vệ sinh thực phẩm (ATTP) và Số tài khoản ngân hàng nhận tiền."
  },
  {
    q: "Doanh thu bán hàng được đối soát và rút về tài khoản ngân hàng như thế nào?",
    a: "Tiền bán hàng được tự động đối soát chính xác và cộng vào Ví Seller ngay sau khi hoàn thành đơn. Bạn có thể tự do rút tiền về tài khoản ngân hàng liên kết 24/7 chỉ trong 30 giây."
  },
  {
    q: "ZoneMart hỗ trợ giao hàng hỏa tốc cho gian hàng của tôi bằng cách nào?",
    a: "Đội ngũ ZoneMart Driver hùng hậu bao quanh bán kính 10km sẽ trực tiếp đến tận gian hàng của bạn nhận hàng và giao đến tay người mua chỉ trong vòng 30 phút."
  }
];

// Form Gửi Yêu Cầu Hỗ Trợ Gian Hàng Trực Tuyến (Gửi về hh9393100@gmail.com)
const sellerSupportForm = reactive({
  fullName: "",
  email: "",
  phone: "",
  storeCode: "",
  topic: "Sự cố gian hàng & App",
  message: ""
});
const isSupportLoading = ref(false);
const supportSuccessMsg = ref("");
const supportErrorMsg = ref("");

const submitSellerSupportTicket = async () => {
  supportSuccessMsg.value = "";
  supportErrorMsg.value = "";

  if (!sellerSupportForm.fullName.trim() || !sellerSupportForm.email.trim() || !sellerSupportForm.message.trim()) {
    supportErrorMsg.value = "Vui lòng nhập đầy đủ Họ tên chủ tiệm, Email và Nội dung cần hỗ trợ!";
    return;
  }

  isSupportLoading.value = true;
  try {
    const res = await fetch("/api/support/ticket", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        fullName: sellerSupportForm.fullName.trim(),
        email: sellerSupportForm.email.trim(),
        phone: sellerSupportForm.phone.trim(),
        orderCode: sellerSupportForm.storeCode.trim(),
        topic: sellerSupportForm.topic,
        message: sellerSupportForm.message.trim()
      })
    });
    const data = await res.json();
    if (data.success || res.ok) {
      supportSuccessMsg.value = `Yêu cầu hỗ trợ của gian hàng đã được gửi thành công đến ban quản trị (hh9393100@gmail.com)! Mã phiếu: ${data.ticketCode || 'ZM-SELLER-CSKH'}`;
      sellerSupportForm.fullName = "";
      sellerSupportForm.email = "";
      sellerSupportForm.phone = "";
      sellerSupportForm.storeCode = "";
      sellerSupportForm.message = "";
    } else {
      supportErrorMsg.value = data.message || "Không thể gửi yêu cầu hỗ trợ. Vui lòng thử lại!";
    }
  } catch (err: any) {
    supportErrorMsg.value = "Không thể kết nối đến máy chủ hỗ trợ. Vui lòng kiểm tra kết nối mạng!";
  } finally {
    isSupportLoading.value = false;
  }
};
</script>

<template>
  <div class="seller-portal-page">
    
    <!-- 1. HEADER CAO CẤP SELLER: ZONEMART SELLER (SẠCH KHÔNG KHUNG BOX) -->
    <header class="seller-header-modern">
      <div class="header-inner">
        <!-- LOGO ZONEMART SELLER (CHỮ SELLER KHÔNG KHUNG, NẰM NGAY DƯỚI CHỮ ZONEMART) -->
        <router-link to="/" class="seller-brand-logo">
          <img src="/logo.png" alt="ZoneMart Logo" class="brand-img" />
          <div class="brand-text-stack">
            <div class="main-brand-title">
              Zone<span class="highlight-mart">Mart</span>
            </div>
            <div class="seller-sub-title">SELLER</div>
          </div>
        </router-link>

        <!-- MENU ĐIỀU HƯỚNG MỞ RỘNG (GIỐNG BÊN SHIPPER) -->
        <nav class="seller-nav-menu">
          <a href="#hero" class="nav-item">Trang Chủ Seller</a>
          <a href="#revenue-calculator" class="nav-item">Tính Doanh Thu</a>
          <a href="#benefits" class="nav-item">Quyền Lợi Vàng</a>
          <a href="#steps" class="nav-item">Quy Trình Mở Shop</a>
          <a href="#faq" class="nav-item">Hỏi Đáp FAQ</a>
          <button @click="scrollToRegisterForm" class="btn-nav-action-orange">Đăng Ký Gian Hàng</button>
          <router-link to="/login?role=seller" class="btn-nav-login">Đăng Nhập</router-link>
        </nav>
      </div>
    </header>

    <!-- 2. HERO BANNER SANG TRỌNG -->
    <section id="hero" class="seller-hero-wrapper">
      <div class="hero-bg-glow"></div>
      <div class="hero-content-grid">
        
        <!-- CỘT NỘI DUNG BÊN TRÁI -->
        <div class="hero-left-col">
          <div class="hero-badge-pill">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.5">
              <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
            </svg>
            <span>KÊNH BÁN HÀNG DÀNH CHO NHÀ VƯỜN & TIỂU THƯƠNG</span>
          </div>

          <h1 class="hero-main-title">
            Mở Gian Hàng Số <span class="text-gradient-orange">ZoneMart</span> <br />
            Tiếp Cận Hàng Ngàn <span class="text-highlight-orange">Đơn Hàng 10km</span>
          </h1>

          <p class="hero-sub-text">
            Miễn phí 100% phí khởi tạo gian hàng. Đưa nông sản tươi ngon và thực phẩm sạch từ trang trại, nhà vườn của bạn đến trực tiếp tay người tiêu dùng.
          </p>

          <div class="hero-action-row">
            <button @click="scrollToRegisterForm" class="btn-hero-glow-orange">
              <span>ĐĂNG KÝ BÁN HÀNG NGAY</span>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </button>
            
            <a href="#revenue-calculator" class="btn-hero-glass-orange">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
                <line x1="8" y1="21" x2="16" y2="21"></line>
                <line x1="12" y1="17" x2="12" y2="21"></line>
              </svg>
              <span>Ước Tính Doanh Thu</span>
            </a>
          </div>

          <!-- DẢI THỐNG KÊ NHANH -->
          <div class="hero-stats-card">
            <div class="stat-box">
              <span class="stat-val">0đ</span>
              <span class="stat-lbl">Phí mở gian hàng</span>
            </div>
            <div class="stat-divider"></div>
            <div class="stat-box">
              <span class="stat-val">50k+</span>
              <span class="stat-lbl">Khách mua trong 10km</span>
            </div>
            <div class="stat-divider"></div>
            <div class="stat-box">
              <span class="stat-val">24h</span>
              <span class="stat-lbl">Duyệt shop thần tốc</span>
            </div>
          </div>
        </div>

        <!-- CỘT HÌNH MINH HỌA BÊN PHẢI -->
        <div class="hero-right-col">
          <div class="hero-image-container">
            <div class="image-glow-backdrop-orange"></div>
            <img
              src="/images/anhxoanendkybanhang_clean.png"
              alt="ZoneMart Seller Illustration"
              class="hero-large-illustration"
            />
          </div>
        </div>

      </div>
    </section>

    <!-- 3. CÔNG CỤ TÍNH DOANH THU DỰ KIẾN (INTERACTIVE CALCULATOR) -->
    <section id="revenue-calculator" class="calculator-section">
      <div class="calc-card-container">
        <div class="calc-header text-center">
          <span class="section-tag-pill-orange">CÔNG CỤ DỰ BÁO TƯƠNG TÁC</span>
          <h2 class="section-heading">Ước Tính Doanh Thu Hàng Tháng Của Gian Hàng</h2>
          <p class="calc-sub-text">Kéo thanh trượt số lượng đơn hàng dự kiến mỗi ngày để xem tiềm năng thu nhập cùng ZoneMart</p>
        </div>

        <div class="calc-body-grid">
          <div class="calc-slider-box">
            <div class="slider-label-row">
              <span>Số lượng đơn hàng/ngày:</span>
              <strong class="hours-val-orange">{{ dailyOrders }} đơn hàng / ngày</strong>
            </div>
            <input 
              type="range" 
              min="10" 
              max="200" 
              step="5" 
              v-model.number="dailyOrders" 
              class="custom-range-slider-orange"
            />
            <div class="range-marks">
              <span>10 đơn</span>
              <span>50 đơn</span>
              <span>100 đơn</span>
              <span>200 đơn</span>
            </div>
          </div>

          <div class="calc-result-box-orange">
            <div class="result-title">DOANH THU ƯỚC TÍNH / THÁNG</div>
            <div class="result-amount">{{ estimatedRevenue }}</div>
            <div class="result-desc">Cước phí & giá bán nông sản công khai minh bạch. Tiền về ví Seller 24/7 tức thì sau khi shipper giao hàng thành công.</div>
            <button @click="scrollToRegisterForm" class="btn-calc-apply-orange">
              MỞ GIAN HÀNG NGAY
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- 4. 6 QUYỀN LỢI ĐỘC QUYỀN VỚI 100% SVG CODE ICONS -->
    <section id="benefits" class="benefits-grid-section">
      <div class="text-center margin-bottom-lg">
        <span class="section-tag-pill-orange">TẠI SAO NÊN CHỌN ZONEMART SELLER?</span>
        <h2 class="section-heading">6 Quyền Lợi Vàng Dành Cho Chủ Gian Hàng Số</h2>
      </div>

      <div class="benefits-6-grid">
        <!-- Card 1 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap orange-theme">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2">
              <rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect>
              <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path>
            </svg>
          </div>
          <h3>0đ Phí Khởi Tạo</h3>
          <p>Cam kết 100% không thu bất kỳ khoản phí tạo gian hàng hay phí duy trì định kỳ hàng tháng nào từ chủ tiệm.</p>
        </div>

        <!-- Card 2 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap green-theme">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
              <circle cx="9" cy="7" r="4"></circle>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
              <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
            </svg>
          </div>
          <h3>Tiếp Cận 50k+ Khách Hàng</h3>
          <p>Thuật toán định vị thông minh kết nối gian hàng với hàng ngàn khách mua thực phẩm sạch trong bán kính 10km.</p>
        </div>

        <!-- Card 3 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap yellow-theme">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#D97706" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <polyline points="12 6 12 12 16 14"></polyline>
            </svg>
          </div>
          <h3>Duyệt Shop Trong 24h</h3>
          <p>Ban quản lý thẩm định hồ sơ trực tuyến và cấp tài khoản kinh doanh chính thức cho tiệm chỉ trong 24 giờ.</p>
        </div>

        <!-- Card 4 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap purple-theme">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#8B5CF6" stroke-width="2">
              <rect x="1" y="3" width="15" height="13"></rect>
              <polygon points="16 8 20 8 23 11 23 16 16 16 16 8"></polygon>
              <circle cx="5.5" cy="18.5" r="2.5"></circle>
              <circle cx="18.5" cy="18.5" r="2.5"></circle>
            </svg>
          </div>
          <h3>Shipper Đến Lấy Tận Nơi</h3>
          <p>Đội ngũ ZoneMart Driver phủ khắp các quận huyện, đến tận tiệm nhận đồ và giao hỏa tốc chỉ trong 30 phút.</p>
        </div>

        <!-- Card 5 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap blue-theme">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
              <line x1="12" y1="1" x2="12" y2="23"></line>
              <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
            </svg>
          </div>
          <h3>Rút Tiền Ví Seller 24/7</h3>
          <p>Doanh thu nổ đơn tự động đối soát chính xác, rút tiền về tài khoản ngân hàng liên kết tức thì trong 30s.</p>
        </div>

        <!-- Card 6 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap red-theme">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#EF4444" stroke-width="2">
              <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path>
            </svg>
          </div>
          <h3>CSKH & Hỗ Trợ 24/7</h3>
          <p>Đội ngũ chuyên viên tổng đài hotline 1900 6868 sẵn sàng tư vấn & giải quyết sự cố đơn hàng lập tức.</p>
        </div>
      </div>
    </section>

    <!-- 5. QUY TRÌNH 3 BƯỚC MỞ GIAN HÀNG -->
    <section id="steps" class="steps-flow-section">
      <div class="steps-title-centered-block">
        <h2 class="section-heading-centered-full">Quy Trình 3 Bước Để Mở Gian Hàng Số</h2>
      </div>

      <div class="steps-flow-container">
        <div class="step-flow-card">
          <div class="step-badge-num">1</div>
          <h3>Điền Thông Tin Tiệm</h3>
          <p>Nhập tên cửa hàng, địa chỉ, danh mục sản phẩm và khung giờ mở cửa kinh doanh.</p>
        </div>

        <div class="step-flow-arrow">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.5">
            <polyline points="9 18 15 12 9 6"></polyline>
          </svg>
        </div>

        <div class="step-flow-card">
          <div class="step-badge-num">2</div>
          <h3>Xác Thực Giấy Tờ</h3>
          <p>Cung cấp CCCD chủ tiệm và Giấy chứng nhận An toàn vệ sinh thực phẩm (ATTP).</p>
        </div>

        <div class="step-flow-arrow">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.5">
            <polyline points="9 18 15 12 9 6"></polyline>
          </svg>
        </div>

        <div class="step-flow-card">
          <div class="step-badge-num">3</div>
          <h3>Nhận Duyệt & Đăng Bán</h3>
          <p>Ban quản lý thẩm định hồ sơ trong 24h và hỗ trợ tiệm nhận đơn hàng nổ liền tay.</p>
        </div>
      </div>
    </section>

    <!-- 6. FORM ĐĂNG KÝ GIAN HÀNG INTEGRATED -->
    <section id="register-form-section" class="form-section">
      <div class="form-container-card">
        <!-- MASCOT PEELING / PEEKING OVER FORM (MÔ HÌNH 3D NHÂN VẬT XOAY ĐẦU THEO CHUỘT) -->
        <div 
          class="seller-mascot-peek-container" 
          title="Nhấp vào em để xem phản ứng & nhận mẹo bán hàng!"
        >
          <!-- Bong bóng thoại tương tác linh hoạt: đổi giao diện cảnh báo nổi bật khi thiếu thông tin -->
          <div class="mascot-speech-bubble" :class="{ 'bubble-pop': isBubblePopping, 'is-alert-bubble': isMascotAlert }">
            <span class="bubble-text">{{ currentMascotMsg }}</span>
            <span class="bubble-arrow"></span>
          </div>

          <!-- Component Mascot chuyển hướng đầu 9 hướng chuẩn sprite sheet nilbuild/page-mascot -->
          <div 
            ref="mascotButtonRef" 
            class="mascot-sprite-button" 
            @click="handleMascotBoop"
          >
            <div class="mascot-sprite-stage" :class="{ 'is-squashing': isSquashing }">
              <span 
                class="mascot-sprite-layer" 
                :style="{ backgroundImage: `url(${mascotDirectionsUrl})`, ...directionStyle }"
              ></span>
              <span 
                class="mascot-sprite-layer" 
                :style="{ backgroundImage: `url(${mascotReactionsUrl})`, ...reactionStyle }"
              ></span>
            </div>
            <div class="mascot-counter-edge"></div>
          </div>
        </div>

        <div class="form-card-header text-center">
          <h2 class="form-title-centered">BẢNG ĐĂNG KÝ MỞ GIAN HÀNG</h2>
          <p class="form-subtitle-centered">Vui lòng điền chính xác thông tin bên dưới để Ban quản lý tài xế & gian hàng thẩm định trong 24h</p>
        </div>

        <!-- Stepper Bar -->
        <div class="stepper-bar">
          <div class="step-item" :class="{ active: currentStep >= 1 }">
            <div class="step-badge">1</div>
            <span>Thông tin tiệm</span>
          </div>
          <div class="step-line" :class="{ active: currentStep >= 2 }"></div>
          <div class="step-item" :class="{ active: currentStep >= 2 }">
            <div class="step-badge">2</div>
            <span>Xác thực chủ tiệm</span>
          </div>
          <div class="step-line" :class="{ active: currentStep >= 3 }"></div>
          <div class="step-item" :class="{ active: currentStep >= 3 }">
            <div class="step-badge">3</div>
            <span>Thanh toán</span>
          </div>
        </div>

        <div v-if="errorMessage" class="msg-box error">⚠️ {{ errorMessage }}</div>

        <!-- BƯỚC 1 -->
        <form v-if="currentStep === 1" @submit.prevent="goToNextStep" class="form-body">
          <!-- Hàng 1: Tên cửa hàng (Full Width) -->
          <div class="field-item">
            <label class="field-label">Tên cửa hàng <span class="req">*</span> <span class="hint">(12 - 30 ký tự)</span></label>
            <input v-model="form.storeName" type="text" class="field-input" placeholder="VD: Nông Sản Sạch ZoneMart Cầu Giấy" required />
          </div>

          <!-- Hàng 2: Địa chỉ tiệm bên trái, Danh mục hàng bán chính bên phải (2-Column) -->
          <div class="form-row-2col">
            <div class="field-item">
              <label class="field-label">Địa chỉ tiệm <span class="req">*</span></label>
              <input v-model="form.address" type="text" class="field-input" placeholder="VD: Số 125 Đường Nguyễn Trãi, Thanh Xuân, Hà Nội" required />
            </div>

            <div class="field-item custom-select-container" :class="{ 'dropdown-open': isCategoryDropdownOpen }">
              <label class="field-label">Danh mục hàng bán chính <span class="req">*</span></label>
              <div class="custom-select-trigger" @click.stop="toggleCategoryDropdown">
                <span>{{ form.category }}</span>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>
              <div v-if="isCategoryDropdownOpen" class="custom-dropdown-menu">
                <div v-for="cat in categoriesList" :key="cat" class="dropdown-item" @click.stop="selectCategory(cat)">
                  {{ cat }}
                </div>
              </div>
            </div>
          </div>

          <!-- Hàng 3: Địa chỉ Gmail (Full Width) -->
          <div class="field-item">
            <label class="field-label">Địa chỉ Gmail đăng ký gian hàng <span class="req">*</span> <span class="hint">(Bắt buộc đuôi @gmail.com)</span></label>
            <input v-model="form.phoneEmail" type="email" class="field-input" placeholder="VD: cuahang@gmail.com" required />
          </div>

          <!-- Hàng 4: Mật khẩu bên trái, Nhập lại mật khẩu bên phải (2-Column) -->
          <div class="form-row-2col">
            <div class="field-item">
              <label class="field-label">Mật khẩu tạo tài khoản <span class="req">*</span> <span class="hint">(Từ 6 - 16 ký tự)</span></label>
              <div class="input-with-eye-wrapper">
                <input 
                  v-model="form.password" 
                  :type="showPassword ? 'text' : 'password'" 
                  class="field-input input-with-eye" 
                  placeholder="Nhập mật khẩu (6 - 16 ký tự)" 
                  maxlength="16"
                  required 
                />
                <button type="button" class="eye-toggle-btn-orange" @click="showPassword = !showPassword" :title="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'">
                  <svg v-if="!showPassword" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                    <circle cx="12" cy="12" r="3"></circle>
                  </svg>
                  <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                    <line x1="1" y1="1" x2="23" y2="23"></line>
                  </svg>
                </button>
              </div>
            </div>

            <div class="field-item">
              <label class="field-label">Nhập lại mật khẩu <span class="req">*</span> <span class="hint">(Xác nhận trùng khớp)</span></label>
              <div class="input-with-eye-wrapper">
                <input 
                  v-model="form.confirmPassword" 
                  :type="showConfirmPassword ? 'text' : 'password'" 
                  class="field-input input-with-eye" 
                  placeholder="Nhập lại mật khẩu xác nhận" 
                  maxlength="16"
                  required 
                />
                <button type="button" class="eye-toggle-btn-orange" @click="showConfirmPassword = !showConfirmPassword" :title="showConfirmPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'">
                  <svg v-if="!showConfirmPassword" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                    <circle cx="12" cy="12" r="3"></circle>
                  </svg>
                  <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                    <line x1="1" y1="1" x2="23" y2="23"></line>
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <!-- Hàng 5: Khung giờ mở cửa kinh doanh (Dưới hàng Mật khẩu) -->
          <div class="field-item custom-select-container" :class="{ 'dropdown-open': isOpenHoursDropdownOpen }">
            <label class="field-label">Khung giờ mở cửa kinh doanh <span class="req">*</span></label>
            <div class="custom-select-trigger" @click.stop="toggleOpenHoursDropdown">
              <span>{{ selectedPreset }}</span>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2">
                <polyline points="6 9 12 15 18 9"></polyline>
              </svg>
            </div>
            <div v-if="isOpenHoursDropdownOpen" class="custom-dropdown-menu">
              <div v-for="preset in openHoursOptions" :key="preset" class="dropdown-item" @click.stop="selectOpenHours(preset)">
                {{ preset }}
              </div>
            </div>
          </div>

          <!-- BẢNG TỰ CHỌN GIỜ MỞ VÀ ĐÓNG CỬA (CUSTOM DROPDOWN CHUẨN ZONEMART, XỔ XUỐNG DƯỚI) -->
          <div v-if="isCustomOpenHours" class="form-row-2col custom-time-box">
            <!-- Giờ mở cửa tiệm -->
            <div class="field-item custom-select-container" :class="{ 'dropdown-open': isOpenTimeDropdownOpen }">
              <label class="field-label">Giờ mở cửa tiệm <span class="req">*</span> <span class="hint">(Từ 05:00 sáng)</span></label>
              <div class="custom-select-trigger" @click.stop="toggleOpenTimeDropdown">
                <span>{{ selectedOpenTimeLabel }}</span>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>
              <div v-if="isOpenTimeDropdownOpen" class="custom-dropdown-menu">
                <div 
                  v-for="t in openTimeOptions" 
                  :key="'open-' + t.value" 
                  class="dropdown-item" 
                  :class="{ selected: customOpenTime === t.value }"
                  @click.stop="selectCustomOpenTime(t.value)"
                >
                  {{ t.label }}
                </div>
              </div>
            </div>

            <!-- Giờ đóng cửa tiệm -->
            <div class="field-item custom-select-container" :class="{ 'dropdown-open': isCloseTimeDropdownOpen }">
              <label class="field-label">Giờ đóng cửa tiệm <span class="req">*</span> <span class="hint">(Đến 02:00 đêm)</span></label>
              <div class="custom-select-trigger" @click.stop="toggleCloseTimeDropdown">
                <span>{{ selectedCloseTimeLabel }}</span>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>
              <div v-if="isCloseTimeDropdownOpen" class="custom-dropdown-menu">
                <div 
                  v-for="t in closeTimeOptions" 
                  :key="'close-' + t.value" 
                  class="dropdown-item" 
                  :class="{ selected: customCloseTime === t.value }"
                  @click.stop="selectCustomCloseTime(t.value)"
                >
                  {{ t.label }}
                </div>
              </div>
            </div>
          </div>

          <div class="form-actions-centered">
            <button type="submit" class="btn-submit-step-orange">
              <span>TIẾP THEO BƯỚC 2</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </button>
          </div>
        </form>

        <!-- BƯỚC 2 -->
        <form v-if="currentStep === 2" @submit.prevent="goToNextStep" class="form-body">
          <div class="field-item">
            <label class="field-label">Họ và tên chủ tiệm <span class="req">*</span></label>
            <input v-model="form.ownerFullName" type="text" class="field-input" placeholder="VD: Nguyễn Văn A" required />
          </div>

          <!-- GIAO DIỆN TẢI ẢNH HIỆN ĐẠI DẠNG CARD KÉO THẢ GIỐNG SHIPPER -->
          <div class="upload-cards-grid full-row">
            <!-- Card 1: CCCD Mặt Trước -->
            <div class="upload-drop-card" :class="{ 'has-image': form.cccdFrontImage }">
              <label class="upload-card-label">
                <input type="file" accept="image/*" class="hidden-file-input" @change="e => handleFileUpload(e, 'cccdFrontImage')" />
                <div v-if="!form.cccdFrontImage" class="upload-card-content">
                  <div class="upload-icon-circle orange">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2">
                      <rect x="3" y="4" width="18" height="16" rx="2"></rect>
                      <circle cx="9" cy="10" r="2"></circle>
                      <line x1="15" y1="8" x2="17" y2="8"></line>
                      <line x1="15" y1="12" x2="17" y2="12"></line>
                    </svg>
                  </div>
                  <div class="upload-text-block">
                    <span class="upload-title">CCCD Mặt Trước <span class="req">*</span></span>
                    <span class="upload-subtext">Nhấp để tải ảnh mặt trước</span>
                  </div>
                </div>
                <div v-else class="upload-preview-wrap">
                  <img :src="form.cccdFrontImage" alt="CCCD Trước" class="upload-preview-img" />
                  <div class="upload-change-badge">✓ Đã tải lên (Bấm để đổi)</div>
                </div>
              </label>
            </div>

            <!-- Card 2: CCCD Mặt Sau -->
            <div class="upload-drop-card" :class="{ 'has-image': form.cccdBackImage }">
              <label class="upload-card-label">
                <input type="file" accept="image/*" class="hidden-file-input" @change="e => handleFileUpload(e, 'cccdBackImage')" />
                <div v-if="!form.cccdBackImage" class="upload-card-content">
                  <div class="upload-icon-circle orange">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2">
                      <rect x="3" y="4" width="18" height="16" rx="2"></rect>
                      <line x1="7" y1="8" x2="17" y2="8"></line>
                      <line x1="7" y1="12" x2="17" y2="12"></line>
                      <line x1="7" y1="16" x2="13" y2="16"></line>
                    </svg>
                  </div>
                  <div class="upload-text-block">
                    <span class="upload-title">CCCD Mặt Sau <span class="req">*</span></span>
                    <span class="upload-subtext">Nhấp để tải ảnh mặt sau</span>
                  </div>
                </div>
                <div v-else class="upload-preview-wrap">
                  <img :src="form.cccdBackImage" alt="CCCD Sau" class="upload-preview-img" />
                  <div class="upload-change-badge">✓ Đã tải lên (Bấm để đổi)</div>
                </div>
              </label>
            </div>

            <!-- Card 3: Giấy ATTP -->
            <div class="upload-drop-card" :class="{ 'has-image': form.foodSafetyCertImage }">
              <label class="upload-card-label">
                <input type="file" accept="image/*" class="hidden-file-input" @change="e => handleFileUpload(e, 'foodSafetyCertImage')" />
                <div v-if="!form.foodSafetyCertImage" class="upload-card-content">
                  <div class="upload-icon-circle orange">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2">
                      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                      <polyline points="9 12 11 14 15 10"></polyline>
                    </svg>
                  </div>
                  <div class="upload-text-block">
                    <span class="upload-title">Giấy CN An Toàn TP <span class="req">*</span></span>
                    <span class="upload-subtext">Nhấp để tải Giấy ATTP / ĐKKD</span>
                  </div>
                </div>
                <div v-else class="upload-preview-wrap">
                  <img :src="form.foodSafetyCertImage" alt="Giấy ATTP" class="upload-preview-img" />
                  <div class="upload-change-badge">✓ Đã tải lên (Bấm để đổi)</div>
                </div>
              </label>
            </div>
          </div>

          <div class="step-btn-row">
            <button type="button" class="btn-prev" @click="goToPrevStep">← Quay lại</button>
            <button type="submit" class="btn-submit-step-orange">
              <span>TIẾP THEO BƯỚC 3</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </button>
          </div>
        </form>

        <!-- BƯỚC 3 -->
        <form v-if="currentStep === 3" @submit.prevent="handleSubmitSeller" class="form-body">
          <!-- Hàng 1: Ngân hàng bên trái, Số tài khoản bên phải (2-Column) -->
          <div class="form-row-2col">
            <div class="field-item custom-select-container" :class="{ 'dropdown-open': isBankDropdownOpen }">
              <label class="field-label">Ngân hàng nhận doanh thu <span class="req">*</span></label>
              <div class="custom-select-trigger" @click.stop="toggleBankDropdown">
                <span>{{ form.bankName }}</span>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>
              <div v-if="isBankDropdownOpen" class="custom-dropdown-menu">
                <div v-for="b in banksList" :key="b" class="dropdown-item" @click.stop="selectBank(b)">
                  {{ b }}
                </div>
              </div>
            </div>

            <div class="field-item">
              <label class="field-label">Số tài khoản ngân hàng <span class="req">*</span> <span class="hint">(8 - 15 số)</span></label>
              <input v-model="form.bankAccountNumber" type="text" class="field-input" placeholder="VD: 1012345678" required />
            </div>
          </div>

          <!-- Dòng Cam kết tuân thủ quy định Seller -->
          <div class="checkbox-terms-container">
            <label class="stylish-checkbox-label">
              <input type="checkbox" v-model="isAgreedToTerms" class="custom-chk-box-orange" />
              <span>Tôi cam kết tuân thủ nghiêm chỉnh <b>Quy định kinh doanh</b> & <b>Quy tắc ứng xử ZoneMart Seller</b>.</span>
            </label>
          </div>

          <div class="step-btn-row">
            <button type="button" class="btn-prev" @click="goToPrevStep">← Quay lại</button>
            <button 
              type="submit" 
              class="btn-submit-final-orange" 
              :class="{ 'btn-disabled': !isAgreedToTerms || isLoading }"
              :disabled="!isAgreedToTerms || isLoading"
            >
              <span v-if="!isLoading">NỘP HỒ SƠ ĐĂNG KÝ GIAN HÀNG</span>
              <span v-else>ĐANG NỘP HỒ SƠ...</span>
            </button>
          </div>
        </form>
      </div>
    </section>

    <!-- 7. HỎI ĐÁP FAQ & GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN DÀNH RIÊNG CHO SELLER (GỬI VỀ hh9393100@gmail.com) -->
    <section id="faq" class="faq-support-split-section">
      <div class="faq-support-grid">
        
        <!-- BÊN TRÁI: FORM GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN CHỦ GIAN HÀNG -->
        <div class="seller-support-form-card">
          <div class="support-card-header">
            <span class="section-tag-pill-orange">HỎI ĐÁP & HỖ TRỢ TRỰC TUYẾN</span>
            <h3>Trung Tâm Hỗ Trợ Gian Hàng ZoneMart</h3>
            <p>Điền thông tin sự cố để ban quản lý hỗ trợ xử lý trực tiếp về Gmail (hh9393100@gmail.com)</p>
          </div>

          <div class="support-contact-bar-orange">
            <div class="bar-item"><i class="bi bi-telephone-fill"></i> Hotline: 1900 6868</div>
            <div class="bar-item"><i class="bi bi-envelope-fill"></i> Email tiếp nhận: hh9393100@gmail.com</div>
            <div class="bar-item"><i class="bi bi-clock-fill"></i> Xử lý: &lt; 15 phút</div>
          </div>

          <form @submit.prevent="submitSellerSupportTicket" class="support-form-body">
            <div v-if="supportSuccessMsg" class="msg-alert-banner success">
              ✅ {{ supportSuccessMsg }}
            </div>
            <div v-if="supportErrorMsg" class="msg-alert-banner error">
              ⚠️ {{ supportErrorMsg }}
            </div>

            <div class="form-fields-grid">
              <div class="input-field-group">
                <label class="input-label-stylish">Họ và tên chủ tiệm <span class="req">*</span></label>
                <input v-model="sellerSupportForm.fullName" type="text" class="modern-input" placeholder="VD: Nguyễn Văn A" required />
              </div>

              <div class="input-field-group">
                <label class="input-label-stylish">Địa chỉ Email liên hệ <span class="req">*</span></label>
                <input v-model="sellerSupportForm.email" type="email" class="modern-input" placeholder="email@example.com" required />
              </div>

              <div class="input-field-group">
                <label class="input-label-stylish">Số điện thoại *</label>
                <input v-model="sellerSupportForm.phone" type="tel" class="modern-input" placeholder="0912345678" required />
              </div>

              <div class="input-field-group">
                <label class="input-label-stylish">Mã Gian Hàng / Tên Shop (nếu có)</label>
                <input v-model="sellerSupportForm.storeCode" type="text" class="modern-input" placeholder="VD: ZM-SHOP-8899" />
              </div>

              <div class="input-field-group full-row">
                <label class="input-label-stylish">Vấn đề cần hỗ trợ</label>
                <div class="topic-options-row">
                  <button 
                    type="button" 
                    v-for="t in ['Sự cố gian hàng & App', 'Đăng tải sản phẩm & Giá', 'Nạp/Rút doanh thu', 'Chiết khấu & Tiền thưởng', 'Đóng góp ý kiến']"
                    :key="t"
                    class="topic-btn"
                    :class="{ active: sellerSupportForm.topic === t }"
                    @click="sellerSupportForm.topic = t"
                  >
                    {{ t }}
                  </button>
                </div>
              </div>

              <div class="input-field-group full-row">
                <label class="input-label-stylish">Nội dung chi tiết <span class="req">*</span></label>
                <textarea v-model="sellerSupportForm.message" class="modern-textarea" rows="4" placeholder="Mô tả chi tiết vướng mắc gian hàng của bạn để ban quản lý xử lý nhanh nhất..." required></textarea>
              </div>
            </div>

            <button type="submit" class="btn-submit-support-orange" :disabled="isSupportLoading">
              <span v-if="!isSupportLoading">GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN</span>
              <span v-else>ĐANG GỬI YÊU CẦU...</span>
            </button>
          </form>
        </div>

        <!-- BÊN PHẢI: THẮC MẮC CỦA CHỦ GIAN HÀNG MỚI (ACCORDION FAQ) -->
        <div class="seller-faq-side-card">
          <div class="faq-card-header">
            <h3>Thắc Mắc Của Chủ Gian Hàng Mới</h3>
            <p>Các câu hỏi thường gặp khi bắt đầu kinh doanh trên sàn ZoneMart</p>
          </div>

          <div class="faq-accordion-list">
            <div 
              v-for="(faq, idx) in sellerFaqs" 
              :key="idx" 
              class="faq-accordion-item"
              :class="{ open: openFaqIndex === idx }"
            >
              <div class="faq-question-header" @click="toggleFaq(idx)">
                <h4>{{ faq.q }}</h4>
                <svg class="faq-chevron" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2.5">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>
              <div v-if="openFaqIndex === idx" class="faq-answer-body">
                <p>{{ faq.a }}</p>
              </div>
            </div>
          </div>
        </div>

      </div>
    </section>

    <!-- 8. FOOTER CAO CẤP DÀNH RIÊNG CHO ZONEMART SELLER (NHƯ BÊN SHIPPER) -->
    <footer class="seller-footer-dark">
      <div class="footer-inner-grid">
        <!-- Cột 1: Thông tin thương hiệu -->
        <div class="f-col brand-col">
          <div class="f-brand-logo">
            <img src="/logo.png" alt="ZoneMart Logo" class="f-logo-img" />
            <div class="f-brand-stack">
              <span class="f-main-title">Zone<span class="highlight-mart">Mart</span></span>
              <span class="f-sub-title-orange">SELLER PORTAL</span>
            </div>
          </div>

          <p class="f-brand-desc">
            Cổng thông tin & hỗ trợ chủ gian hàng kinh doanh ZoneMart Seller. Kết nối hàng triệu đơn hàng nông sản, đồ sạch tận tay người tiêu dùng.
          </p>

          <ul class="footer-contact-list">
            <li>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
              <span><b>Trụ sở:</b> Tòa nhà ZoneMart, Khu Công Nghệ Cao, TP. Hà Nội</span>
            </li>
            <li>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>
              <span><b>Tổng đài hỗ trợ 24/7:</b> 1900 6868 (8:00 - 21:00)</span>
            </li>
            <li>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path><polyline points="22,6 12,13 2,6"></polyline></svg>
              <span><b>Email hỗ trợ gian hàng:</b> seller@zonemart.vn</span>
            </li>
          </ul>
        </div>

        <!-- Cột 2: VỀ ZONEMART SELLER -->
        <div class="f-col">
          <h4 class="f-col-heading">VỀ ZONEMART SELLER</h4>
          <ul class="f-links-list">
            <li><a href="#hero">Giới thiệu ZoneMart Seller</a></li>
            <li><a href="#benefits">Quy chế hoạt động ứng dụng</a></li>
            <li><a href="#benefits">Tiêu chuẩn gian hàng nông sản sạch</a></li>
            <li><a href="#revenue-calculator">Bảng phí chiết khấu 0%</a></li>
            <li><a href="#faq">Tin tức & Mẹo bán hàng hiệu quả</a></li>
            <li><a href="#register-form-section">Đăng ký gian hàng mới</a></li>
          </ul>
        </div>

        <!-- Cột 3: HỖ TRỢ GIAN HÀNG 24/7 -->
        <div class="f-col">
          <h4 class="f-col-heading">HỖ TRỢ GIAN HÀNG 24/7</h4>
          <ul class="f-links-list">
            <li><a href="#faq">Trung tâm hỗ trợ gian hàng 24/7</a></li>
            <li><a href="#steps">Hướng dẫn đăng sản phẩm & giá</a></li>
            <li><a href="#benefits">Chính sách phí & hoa hồng 0%</a></li>
            <li><a href="#benefits">Quy trình rút tiền 30s về ngân hàng</a></li>
            <li><a href="#faq">Giải quyết sự cố đơn hàng</a></li>
            <li><a href="#benefits">Chính sách bảo vệ quyền lợi người bán</a></li>
          </ul>
        </div>

        <!-- Cột 4: HỢP TÁC & PHÁT TRIỂN -->
        <div class="f-col">
          <h4 class="f-col-heading">HỢP TÁC & PHÁT TRIỂN</h4>
          <ul class="f-links-list">
            <li><router-link to="/register-shipper">Cổng Đăng Ký Tài Xế Shipper</router-link></li>
            <li><router-link to="/register-seller">Cổng Đăng Ký Gian Hàng Seller</router-link></li>
            <li><router-link to="/">Trang Chủ Sàn ZoneMart</router-link></li>
            <li><router-link to="/login?role=seller">Đăng Nhập Tài Khoản Hệ Thống</router-link></li>
            <li><router-link to="/admin">Cổng Quản Trị Viên Admin</router-link></li>
          </ul>
        </div>

        <!-- Cột 5: RÚT DOANH THU & THANH TOÁN -->
        <div class="f-col">
          <h4 class="f-col-heading">RÚT DOANH THU & THANH TOÁN</h4>
          <div class="payment-tags-grid">
            <span class="pay-tag">Vietcombank</span>
            <span class="pay-tag">MB Bank</span>
            <span class="pay-tag">Techcombank</span>
            <span class="pay-tag">BIDV</span>
            <span class="pay-tag">Agribank</span>
            <span class="pay-tag highlight-orange">Rút Doanh Thu 24/7</span>
            <span class="pay-tag">VNPAY QR</span>
            <span class="pay-tag">Ví MoMo</span>
          </div>

          <h4 class="f-col-heading margin-top-md">KẾT NỐI VỚI ZONEMART</h4>
          <div class="social-icons-row">
            <a href="#" class="social-btn"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"></path></svg></a>
            <a href="#" class="social-btn"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"></path></svg></a>
            <a href="#" class="social-btn"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="2" width="20" height="20" rx="5" ry="5"></rect><path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"></path><line x1="17.5" y1="6.5" x2="17.51" y2="6.5"></line></svg></a>
          </div>
        </div>
      </div>

      <div class="footer-bottom-bar">
        <p>© 2026 ZoneMart E-Commerce Platform. Cổng thông tin & Đăng ký Gian hàng ZoneMart Seller chính thức. Nông sản & Đồ sạch hỏa tốc 10km.</p>
      </div>
    </footer>

    <!-- MODAL THÀNH CÔNG -->
    <Transition name="fade-modal">
      <div v-if="showSuccessModal" class="modal-backdrop" @click.self="closeSuccessModal">
        <div class="modal-card-stylish">
          <div class="modal-badge-icon">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2.5">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
          </div>
          <h3>Đăng Ký Mở Gian Hàng Thành Công!</h3>
          <p>
            Cảm ơn bạn đã nộp hồ sơ hợp tác bán hàng cùng ZoneMart. Thư xác nhận đã được gửi về Gmail của bạn. Đội ngũ thẩm định sẽ kiểm tra và duyệt gian hàng trong vòng 24 giờ.
          </p>
          <button @click="closeSuccessModal" class="btn-modal-action-orange">QUAY LẠI TRANG CHỦ SELLER</button>
        </div>
      </div>
    </Transition>

  </div>
</template>

<style scoped>
/* MAIN CONTAINER & BACKGROUND CHUẨN ĐĂNG NHẬP SELLER (#F0FDF4 -> #DCFCE7 MINT GREEN) */
.seller-landing-container {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: radial-gradient(circle at 40% 30%, #F0FDF4 0%, #DCFCE7 60%, #F0FDF4 100%);
  color: #0F172A;
  min-height: 100vh;
}

/* HEADER CAO CẤP SELLER (LOGO SẠCH CHỮ SELLER KHÔNG KHUNG BOX) */
.seller-header-modern {
  position: sticky;
  top: 0;
  z-index: 100;
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(12px);
  border-bottom: 1.5px solid #A7F3D0;
  box-shadow: 0 4px 20px rgba(16, 185, 129, 0.06);
}

.header-inner {
  max-width: 1320px;
  margin: 0 auto;
  padding: 10px 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.seller-brand-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}

.brand-img {
  height: 42px;
  width: auto;
}

.brand-text-stack {
  display: flex;
  flex-direction: column;
  line-height: 1;
}

.main-brand-title {
  font-size: 22px;
  font-weight: 900;
  color: #0F172A;
  letter-spacing: -0.5px;
}

.highlight-mart {
  color: #10B981;
}

.seller-sub-title {
  font-size: 11px;
  font-weight: 900;
  color: #10B981;
  letter-spacing: 2px;
  text-transform: uppercase;
  margin-top: 2px;
}

.seller-nav-menu {
  display: flex;
  align-items: center;
  gap: 16px;
}

.nav-item {
  color: #475569;
  text-decoration: none;
  font-size: 13.5px;
  font-weight: 700;
  transition: color 0.2s;
  white-space: nowrap;
}

.nav-item:hover {
  color: #10B981;
}

.btn-nav-action-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  padding: 9.5px 20px;
  border-radius: 30px;
  font-weight: 800;
  font-size: 13.5px;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(16, 185, 129, 0.25);
  transition: all 0.2s;
  white-space: nowrap;
}

.btn-nav-action-orange:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1.5px);
  box-shadow: 0 6px 18px rgba(16, 185, 129, 0.35);
}

.btn-nav-login {
  color: #059669;
  background: #ECFDF5;
  border: 1px solid #A7F3D0;
  padding: 8.5px 18px;
  border-radius: 30px;
  text-decoration: none;
  font-weight: 800;
  font-size: 13.5px;
  transition: all 0.2s;
  white-space: nowrap;
}

.btn-nav-login:hover {
  background: #D1FAE5;
}

/* HERO SECTION */
.seller-hero-wrapper {
  position: relative;
  background: linear-gradient(180deg, #F0FDF4 0%, #DCFCE7 100%);
  padding: 48px 24px 60px 24px;
  overflow: hidden;
  border-bottom: 1px solid #A7F3D0;
}

.hero-content-grid {
  max-width: 1320px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 1.1fr 0.9fr;
  gap: 36px;
  align-items: center;
}

.hero-badge-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #ECFDF5;
  border: 1px solid #A7F3D0;
  padding: 6px 16px;
  border-radius: 30px;
  color: #047857;
  font-size: 12.5px;
  font-weight: 800;
  margin-bottom: 20px;
}

.hero-main-title {
  font-size: 40px;
  font-weight: 900;
  line-height: 1.2;
  color: #0F172A;
  margin-bottom: 16px;
  letter-spacing: -0.5px;
}

.text-gradient-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.text-highlight-orange {
  color: #059669;
}

.hero-sub-text {
  font-size: 15.5px;
  color: #475569;
  line-height: 1.6;
  margin-bottom: 28px;
  max-width: 600px;
}

.hero-action-row {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 36px;
}

.btn-hero-glow-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  padding: 14px 28px;
  border-radius: 30px;
  font-weight: 900;
  font-size: 14.5px;
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  box-shadow: 0 10px 25px rgba(16, 185, 129, 0.35);
  transition: all 0.25s ease;
}

.btn-hero-glow-orange:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-2px);
  box-shadow: 0 14px 30px rgba(16, 185, 129, 0.45);
}

.btn-hero-glass-orange {
  background: #ffffff;
  color: #047857;
  border: 1.5px solid #A7F3D0;
  padding: 13px 24px;
  border-radius: 30px;
  font-weight: 800;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
  transition: all 0.2s;
}

.btn-hero-glass-orange:hover {
  background: #ECFDF5;
}

.hero-stats-card {
  display: flex;
  align-items: center;
  gap: 20px;
  background: #ffffff;
  padding: 18px 24px;
  border-radius: 20px;
  border: 1.5px solid #A7F3D0;
  box-shadow: 0 10px 25px rgba(16, 185, 129, 0.05);
  max-width: 540px;
}

.stat-box { display: flex; flex-direction: column; }
.stat-val { font-size: 20px; font-weight: 900; color: #059669; }
.stat-lbl { font-size: 11.5px; color: #64748B; font-weight: 600; }
.stat-divider { width: 1px; height: 32px; background: #A7F3D0; }

.hero-right-col {
  display: flex;
  justify-content: center;
  align-items: center;
}

.hero-image-container {
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
}

.image-glow-backdrop-orange {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 95%;
  height: 95%;
  background: radial-gradient(circle, rgba(16, 185, 129, 0.18) 0%, rgba(255, 255, 255, 0) 70%);
  z-index: 1;
}

.hero-large-illustration {
  position: relative;
  z-index: 2;
  max-width: 120%;
  height: auto;
  max-height: 460px;
  object-fit: contain;
  transform: scale(1.2);
  transform-origin: center right;
  transition: transform 0.3s ease;
}

/* CALCULATOR SECTION */
.calculator-section {
  background: #F0FDF4;
  padding: 50px 24px;
  border-bottom: 1px solid #A7F3D0;
}

.calc-card-container {
  max-width: 1000px;
  margin: 0 auto;
}

.section-tag-pill-orange {
  display: inline-block;
  background: #ECFDF5;
  color: #047857;
  border: 1px solid #A7F3D0;
  font-size: 12px;
  font-weight: 800;
  padding: 5px 14px;
  border-radius: 20px;
  margin-bottom: 10px;
}

.section-heading {
  font-size: 30px;
  font-weight: 900;
  color: #0F172A;
  margin-bottom: 12px;
}

.calc-sub-text { color: #64748B; font-size: 14.5px; margin-bottom: 32px; }

.calc-body-grid {
  display: grid;
  grid-template-columns: 1.2fr 0.8fr;
  gap: 32px;
  align-items: center;
}

.calc-slider-box {
  background: #ffffff;
  padding: 24px;
  border-radius: 20px;
  border: 1.5px solid #A7F3D0;
}

.slider-label-row {
  display: flex;
  justify-content: space-between;
  font-size: 15px;
  font-weight: 700;
  color: #334155;
  margin-bottom: 16px;
}

.hours-val-orange { color: #059669; font-size: 18px; }

.custom-range-slider-orange {
  width: 100%;
  accent-color: #10B981;
  height: 8px;
  border-radius: 4px;
  cursor: pointer;
}

.range-marks {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: #64748B;
  margin-top: 12px;
}

.calc-result-box-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #ffffff;
  padding: 28px;
  border-radius: 22px;
  box-shadow: 0 10px 25px rgba(16, 185, 129, 0.3);
}

.result-title { font-size: 12px; font-weight: 800; opacity: 0.9; margin-bottom: 8px; letter-spacing: 1px; }
.result-amount { font-size: 34px; font-weight: 900; margin-bottom: 12px; }
.result-desc { font-size: 12.5px; opacity: 0.9; line-height: 1.5; margin-bottom: 20px; }

.btn-calc-apply-orange {
  width: 100%;
  background: #ffffff;
  color: #059669;
  border: none;
  padding: 13px;
  border-radius: 12px;
  font-weight: 900;
  font-size: 13.5px;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-calc-apply-orange:hover { background: #ECFDF5; }

/* 6 BENEFITS GRID */
.benefits-grid-section {
  max-width: 1320px;
  margin: 50px auto;
  padding: 0 24px;
}

.margin-bottom-lg { margin-bottom: 36px; }

.benefits-6-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
}

.benefit-card-modern {
  background: #ffffff;
  padding: 28px 22px;
  border-radius: 22px;
  border: 1.5px solid #A7F3D0;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.03);
  transition: all 0.25s ease;
}

.benefit-card-modern:hover {
  transform: translateY(-4px);
  border-color: #6EE7B7;
  box-shadow: 0 14px 30px rgba(16, 185, 129, 0.12);
}

.card-svg-icon-wrap {
  width: 52px;
  height: 52px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16px;
}

.card-svg-icon-wrap.orange-theme { background: #ECFDF5; }
.card-svg-icon-wrap.green-theme { background: #D1FAE5; }
.card-svg-icon-wrap.yellow-theme { background: #FEF3C7; }
.card-svg-icon-wrap.purple-theme { background: #F3E8FF; }
.card-svg-icon-wrap.blue-theme { background: #E0F2FE; }
.card-svg-icon-wrap.red-theme { background: #FEE2E2; }

.benefit-card-modern h3 {
  font-size: 17px;
  font-weight: 800;
  color: #0F172A;
  margin-bottom: 8px;
}

.benefit-card-modern p {
  font-size: 13.5px;
  color: #64748B;
  line-height: 1.55;
}

/* STEPS FLOW SECTION */
.steps-flow-section {
  background: #ECFDF5;
  padding: 42px 24px;
  border-top: 1px solid #A7F3D0;
  border-bottom: 1px solid #A7F3D0;
}

.steps-title-centered-block { width: 100%; text-align: center; margin-bottom: 32px; }
.section-heading-centered-full { font-size: 30px; font-weight: 900; color: #0F172A; margin: 0; }

.steps-flow-container {
  max-width: 1100px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.step-flow-card {
  background: #ffffff;
  border: 1.5px solid #A7F3D0;
  border-radius: 20px;
  padding: 24px 20px;
  flex: 1;
  max-width: 310px;
  text-align: center;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.03);
}

.step-badge-num {
  width: 38px;
  height: 38px;
  background: #10B981;
  color: #ffffff;
  font-weight: 900;
  font-size: 16px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 14px auto;
}

.step-flow-card h3 { font-size: 16.5px; font-weight: 800; color: #0F172A; margin-bottom: 6px; }
.step-flow-card p { font-size: 13px; color: #64748B; line-height: 1.5; margin: 0; }

.step-flow-arrow { display: flex; align-items: center; justify-content: center; padding: 0 10px; }

/* FORM SECTION */
.form-section {
  max-width: 860px;
  margin: 50px auto;
  padding: 0 24px;
}

.form-container-card {
  position: relative;
  background: #ffffff;
  border-radius: 24px;
  border: 1.5px solid #A7F3D0;
  padding: 36px 32px;
  box-shadow: 0 12px 36px rgba(16, 185, 129, 0.08);
}

/* ================= MASCOT PEELING / PEEKING STYLE (MÔ HÌNH 3D NHÂN VẬT) ================= */
.seller-mascot-peek-container {
  position: absolute;
  top: -46px;
  right: 24px;
  z-index: 30;
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  user-select: none;
  perspective: 700px;
}

.mascot-sprite-button {
  position: relative;
  width: 76px;
  height: 76px;
  cursor: pointer;
  user-select: none;
  background: transparent;
  border: none;
  padding: 0;
  display: block;
  flex-shrink: 0;
}

.mascot-sprite-stage {
  position: relative;
  width: 100%;
  height: 100%;
  transform-origin: 50% 85%;
  transition: transform 0.15s ease-out;
  filter: drop-shadow(0 6px 14px rgba(16, 185, 129, 0.25));
}

.mascot-sprite-stage.is-squashing {
  animation: mascotSquashBounce 0.42s ease-in-out;
}

@keyframes mascotSquashBounce {
  0% { transform: scale(1, 1); }
  22% { transform: scale(1.12, 0.86); }
  55% { transform: scale(0.95, 1.07); }
  78% { transform: scale(1.03, 0.98); }
  100% { transform: scale(1, 1); }
}

.mascot-sprite-layer {
  position: absolute;
  inset: 0;
  background-size: 300% 300%;
  background-repeat: no-repeat;
  image-rendering: -webkit-optimize-contrast;
  transition: opacity 0.15s ease;
}

.mascot-counter-edge {
  position: absolute;
  bottom: -2px;
  left: 50%;
  transform: translateX(-50%);
  width: 52px;
  height: 3px;
  background: linear-gradient(90deg, rgba(16, 185, 129, 0) 0%, rgba(16, 185, 129, 0.35) 50%, rgba(16, 185, 129, 1) 100%);
  border-radius: 999px;
}

.mascot-speech-bubble {
  position: relative;
  background: #FFFFFF;
  border: 1.5px solid #A7F3D0;
  box-shadow: 0 8px 20px -4px rgba(16, 185, 129, 0.16), 0 2px 8px rgba(0, 0, 0, 0.04);
  border-radius: 12px;
  padding: 6px 12px;
  max-width: 275px;
  margin-bottom: 0;
  transition: all 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
  transform-origin: right center;
}

.mascot-speech-bubble.bubble-pop {
  transform: scale(0.92) translateY(2px);
  opacity: 0.5;
}

.mascot-speech-bubble .bubble-text {
  font-size: 10.5px;
  font-weight: 700;
  color: #065F46;
  line-height: 1.35;
  display: block;
}

.mascot-speech-bubble .bubble-arrow {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  right: -7px;
  width: 0;
  height: 0;
  border-top: 5px solid transparent;
  border-bottom: 5px solid transparent;
  border-left: 7px solid #FFFFFF;
}

.mascot-speech-bubble .bubble-arrow::before {
  content: "";
  position: absolute;
  top: -6px;
  left: -9px;
  width: 0;
  height: 0;
  border-top: 6px solid transparent;
  border-bottom: 6px solid transparent;
  border-left: 8px solid #A7F3D0;
  z-index: -1;
}

/* TRẠNG THÁI CẢNH BÁO KHI NHẬP THIẾU HOẶC SAI THÔNG TIN */
.mascot-speech-bubble.is-alert-bubble {
  background: #FEF2F2;
  border-color: #F87171;
  box-shadow: 0 8px 22px -3px rgba(239, 68, 68, 0.28), 0 2px 8px rgba(0, 0, 0, 0.04);
  animation: mascotAlertWiggle 0.42s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
}

@keyframes mascotAlertWiggle {
  0% { transform: scale(0.95) translateX(0); }
  20% { transform: scale(1.02) translateX(-3px); }
  40% { transform: scale(1.02) translateX(3px); }
  60% { transform: scale(1.01) translateX(-1px); }
  80% { transform: scale(1.01) translateX(1px); }
  100% { transform: scale(1) translateX(0); }
}

.mascot-speech-bubble.is-alert-bubble .bubble-text {
  color: #B91C1C;
  font-weight: 800;
}

.mascot-speech-bubble.is-alert-bubble .bubble-arrow {
  border-left-color: #FEF2F2;
}

.mascot-speech-bubble.is-alert-bubble .bubble-arrow::before {
  border-left-color: #F87171;
}

.form-card-header {
  text-align: center !important;
  margin-bottom: 24px;
}

.form-title-centered {
  font-size: 24px;
  font-weight: 900;
  color: #0F172A;
  margin-bottom: 6px;
  text-align: center !important;
}

.form-subtitle-centered {
  font-size: 13.5px;
  color: #64748B;
  margin-bottom: 24px;
  text-align: center !important;
}

.stepper-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 32px;
}

.step-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  font-weight: 700;
  color: #94A3B8;
}

.step-item.active { color: #059669; }

.step-badge {
  width: 26px;
  height: 26px;
  border-radius: 50%;
  background: #E2E8F0;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
}

.step-item.active .step-badge { background: #10B981; }

.step-line { flex: 1; height: 2px; background: #E2E8F0; margin: 0 12px; }
.step-line.active { background: #10B981; }

.msg-box.error {
  background: #FEF2F2;
  border: 1.5px solid #FECACA;
  color: #991B1B;
  padding: 12px 16px;
  border-radius: 12px;
  font-size: 13.5px;
  margin-bottom: 20px;
}

.form-body { display: flex; flex-direction: column; gap: 20px; }

.field-item { display: flex; flex-direction: column; gap: 6px; }

.field-label { font-size: 13.5px; font-weight: 700; color: #334155; }
.req { color: #EF4444; }
.hint { font-weight: 400; color: #64748B; font-size: 12px; }

.field-input {
  width: 100%;
  padding: 12px 16px;
  border-radius: 12px;
  border: 1.5px solid #CBD5E1;
  font-size: 14.5px;
  outline: none;
  transition: border-color 0.2s;
}

select.select-stylish {
  cursor: pointer;
  background-color: #ffffff;
  appearance: auto;
  font-weight: 600;
  color: #0F172A;
}

.field-input:focus { border-color: #10B981; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.12); }

/* CUSTOM DROPDOWN OVERLAY (KHÔNG ĐẨY THÔNG TIN PHÍA DƯỚI) */
.custom-select-container {
  position: relative !important;
  z-index: 10;
}

.custom-select-trigger {
  width: 100%;
  padding: 12px 16px;
  border-radius: 12px;
  border: 1.5px solid #CBD5E1;
  background: #ffffff;
  color: #0F172A;
  font-size: 14.5px;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: space-between;
  cursor: pointer;
  box-sizing: border-box;
  transition: all 0.2s ease;
}

.custom-select-trigger:hover,
.custom-select-container.dropdown-open .custom-select-trigger {
  border-color: #10B981;
  background: #ffffff;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.12);
}

.custom-select-container.dropdown-open {
  z-index: 999999 !important;
}

.custom-dropdown-menu {
  position: absolute !important;
  top: calc(100% + 6px) !important;
  left: 0 !important;
  right: 0 !important;
  z-index: 9999999 !important;
  background: #ffffff !important;
  border: 1.5px solid #A7F3D0 !important;
  border-radius: 12px !important;
  box-shadow: 0 10px 25px rgba(0,0,0,0.18) !important;
  max-height: 220px !important;
  overflow-y: auto !important;
}

.dropdown-item {
  padding: 10px 16px;
  cursor: pointer;
  font-size: 13.5px;
  color: #334155;
  transition: background 0.2s;
}

.dropdown-item:hover,
.dropdown-item.selected {
  background: #ECFDF5;
  color: #059669;
  font-weight: 800;
}

/* EYE TOGGLE BUTTON STYLING FOR PASSWORD FIELDS */
.input-with-eye-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-with-eye {
  padding-right: 46px !important;
}

.eye-toggle-btn-orange {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #64748B;
  border-radius: 8px;
  transition: all 0.2s;
  z-index: 10;
}

.eye-toggle-btn-orange:hover {
  color: #059669;
  background: #ECFDF5;
}

/* CUSTOM TIME BOX (CHỈ HIỆN KHI CHỌN TÙY CHỈNH KHUNG GIỜ) */
.custom-time-box {
  position: relative !important;
  z-index: 1 !important;
  background: #ECFDF5;
  border: 1.5px dashed #6EE7B7;
  padding: 14px 16px;
  border-radius: 16px;
  margin-top: 4px;
}

/* UPLOAD CARDS GRID (STEP 2 MODERN UI) */
.upload-cards-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  margin: 8px 0 16px 0;
}

.upload-drop-card {
  background: #ECFDF5;
  border: 2px dashed #6EE7B7;
  border-radius: 16px;
  padding: 20px 12px;
  text-align: center;
  transition: all 0.25s ease;
  cursor: pointer;
  position: relative;
  min-height: 140px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.upload-drop-card:hover {
  background: #D1FAE5;
  border-color: #10B981;
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(16, 185, 129, 0.12);
}

.upload-drop-card.has-image {
  border-style: solid;
  border-color: #22C55E;
  background: #F0FDF4;
  padding: 8px;
}

.upload-card-label {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  cursor: pointer;
}

.hidden-file-input {
  display: none;
}

.upload-icon-circle.orange {
  width: 46px;
  height: 46px;
  border-radius: 50%;
  background: #D1FAE5;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 10px auto;
}

/* 2-COLUMN GRID LAYOUT FOR FORM ROWS */
.form-row-2col {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

@media (max-width: 640px) {
  .form-row-2col {
    grid-template-columns: 1fr;
  }
}

.upload-card-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
}

.upload-text-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  gap: 2px;
}

.upload-title {
  font-size: 12.5px;
  font-weight: 800;
  color: #0F172A;
  margin-bottom: 2px;
  text-align: center;
  line-height: 1.3;
}

.upload-subtext {
  font-size: 10.5px;
  color: #64748B;
  text-align: center;
  line-height: 1.2;
}

/* CHECKBOX COMMITMENT STYLING */
.checkbox-terms-container {
  margin: 6px 0 10px 0;
  display: flex;
  align-items: center;
}

.stylish-checkbox-label {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  font-size: 13px;
  color: #334155;
  user-select: none;
}

.custom-chk-box-orange {
  width: 18px;
  height: 18px;
  accent-color: #10B981;
  cursor: pointer;
  flex-shrink: 0;
}

.upload-preview-wrap {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.upload-preview-img {
  width: 100%;
  max-height: 110px;
  object-fit: cover;
  border-radius: 10px;
  margin-bottom: 6px;
}

.upload-change-badge {
  font-size: 11px;
  font-weight: 800;
  color: #15803D;
  background: #DCFCE7;
  padding: 3px 8px;
  border-radius: 20px;
}

.form-actions-centered { display: flex; justify-content: center; width: 100%; margin-top: 10px; }

.btn-submit-step-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  padding: 13px 32px;
  border-radius: 30px;
  font-weight: 800;
  font-size: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 6px 18px rgba(16, 185, 129, 0.3);
  transition: all 0.2s;
}

.btn-submit-step-orange:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
  box-shadow: 0 8px 22px rgba(16, 185, 129, 0.4);
}

.step-btn-row { display: flex; justify-content: space-between; align-items: center; margin-top: 10px; }

.btn-prev { background: #F1F5F9; color: #475569; border: none; padding: 12px 24px; border-radius: 30px; font-weight: 700; cursor: pointer; }
.btn-prev:hover { background: #E2E8F0; }

.btn-submit-final-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  padding: 13px 32px;
  border-radius: 30px;
  font-weight: 900;
  font-size: 14px;
  cursor: pointer;
  box-shadow: 0 8px 22px rgba(16, 185, 129, 0.35);
  transition: all 0.2s;
}

.btn-submit-final-orange:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
}
.btn-submit-final-orange.btn-disabled,
.btn-submit-final-orange:disabled {
  background: #CBD5E1 !important;
  color: #94A3B8 !important;
  box-shadow: none !important;
  cursor: not-allowed !important;
  transform: none !important;
  opacity: 0.85;
}

/* FAQ & SUPPORT SPLIT SECTION */
.faq-support-split-section {
  max-width: 1320px;
  margin: 50px auto;
  padding: 0 24px;
}

.faq-support-grid {
  display: grid;
  grid-template-columns: 1.15fr 0.85fr;
  gap: 32px;
  align-items: start;
}

.seller-support-form-card {
  background: #ffffff;
  border: 1.5px solid #A7F3D0;
  border-radius: 24px;
  padding: 32px;
  box-shadow: 0 10px 30px rgba(16, 185, 129, 0.06);
}

.support-card-header h3 { font-size: 20px; font-weight: 900; color: #0F172A; margin-top: 6px; }
.support-card-header p { font-size: 13px; color: #64748B; margin-top: 4px; }

.support-contact-bar-orange {
  display: flex;
  gap: 16px;
  background: #ECFDF5;
  border: 1px solid #A7F3D0;
  padding: 10px 16px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 700;
  color: #047857;
  margin: 16px 0 24px 0;
  flex-wrap: wrap;
}

.support-form-body { display: flex; flex-direction: column; gap: 16px; }
.form-fields-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.full-row { grid-column: span 2; }
.input-label-stylish { font-size: 13px; font-weight: 700; color: #334155; margin-bottom: 6px; display: block; }
.modern-input { width: 100%; padding: 11px 14px; border-radius: 12px; border: 1.5px solid #CBD5E1; font-size: 14px; outline: none; }
.modern-input:focus { border-color: #10B981; }
.modern-textarea { width: 100%; padding: 12px 14px; border-radius: 12px; border: 1.5px solid #CBD5E1; font-size: 14px; outline: none; resize: vertical; }

.topic-options-row { display: flex; flex-wrap: wrap; gap: 8px; }
.topic-btn { background: #F8FAFC; border: 1px solid #E2E8F0; color: #64748B; font-size: 12px; font-weight: 700; padding: 6px 12px; border-radius: 20px; cursor: pointer; transition: all 0.2s; }
.topic-btn.active { background: #ECFDF5; border-color: #A7F3D0; color: #059669; }

.btn-submit-support-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  padding: 13px;
  border-radius: 30px;
  font-weight: 800;
  font-size: 13.5px;
  cursor: pointer;
  box-shadow: 0 6px 18px rgba(16, 185, 129, 0.3);
  margin-top: 10px;
}
.btn-submit-support-orange:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
}

.seller-faq-side-card {
  background: #ffffff;
  border: 1.5px solid #A7F3D0;
  border-radius: 24px;
  padding: 32px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.03);
}

.faq-card-header h3 { font-size: 20px; font-weight: 900; color: #0F172A; }
.faq-card-header p { font-size: 13px; color: #64748B; margin-top: 4px; margin-bottom: 20px; }

.faq-accordion-list { display: flex; flex-direction: column; gap: 12px; }
.faq-accordion-item { border: 1.5px solid #A7F3D0; border-radius: 16px; overflow: hidden; background: #F0FDF4; transition: all 0.2s; }
.faq-accordion-item.open { border-color: #6EE7B7; background: #ffffff; box-shadow: 0 4px 14px rgba(16, 185, 129, 0.08); }
.faq-question-header { padding: 16px 20px; display: flex; justify-content: space-between; align-items: center; cursor: pointer; }
.faq-question-header h4 { font-size: 14.5px; font-weight: 800; color: #0F172A; margin: 0; line-height: 1.4; }
.faq-chevron { transition: transform 0.25s; }
.faq-accordion-item.open .faq-chevron { transform: rotate(180deg); }
.faq-answer-body { padding: 0 20px 16px 20px; color: #475569; font-size: 13.5px; line-height: 1.6; border-top: 1px solid #ECFDF5; margin-top: 4px; padding-top: 12px; }

/* FOOTER DARK SELLER */
.seller-footer-dark {
  background: #0F172A;
  color: #CBD5E1;
  padding: 60px 24px 28px 24px;
  border-top: 1px solid #1E293B;
}

.footer-inner-grid {
  max-width: 1360px;
  margin: 0 auto 32px auto;
  display: grid;
  grid-template-columns: 1.3fr 1fr 1fr 1fr 1.1fr;
  gap: 24px;
}

.f-brand-logo { display: flex; align-items: center; gap: 10px; margin-bottom: 16px; }
.f-logo-img { height: 38px; }
.f-brand-stack { display: flex; flex-direction: column; line-height: 1; }
.f-main-title { font-size: 20px; font-weight: 900; color: #ffffff; }
.f-sub-title-orange { font-size: 10.5px; font-weight: 900; color: #10B981; letter-spacing: 2px; }
.f-brand-desc { font-size: 12.5px; color: #94A3B8; line-height: 1.6; margin-bottom: 18px; }

.footer-contact-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 10px; }
.footer-contact-list li { display: flex; align-items: center; gap: 8px; font-size: 12.5px; color: #94A3B8; }
.footer-contact-list li b { color: #F1F5F9; }

.f-col-heading { font-size: 13.5px; font-weight: 900; color: #ffffff; letter-spacing: 0.5px; margin-bottom: 18px; }
.f-links-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 10px; }
.f-links-list a { color: #94A3B8; text-decoration: none; font-size: 13px; transition: color 0.2s; white-space: nowrap; }
.f-links-list a:hover { color: #6EE7B7; }

.payment-tags-grid { display: flex; flex-wrap: wrap; gap: 6px; }
.pay-tag { background: #1E293B; color: #CBD5E1; font-size: 11px; font-weight: 700; padding: 4px 9px; border-radius: 6px; border: 1px solid #334155; white-space: nowrap; }
.pay-tag.highlight-orange { background: #10B981; color: #ffffff; border: none; }

.margin-top-md { margin-top: 20px; }
.social-icons-row { display: flex; gap: 10px; }
.social-btn { width: 34px; height: 34px; border-radius: 50%; background: #1E293B; color: #94A3B8; display: flex; align-items: center; justify-content: center; text-decoration: none; border: 1px solid #334155; transition: all 0.2s; }
.social-btn:hover { background: #10B981; color: #ffffff; border-color: #10B981; }

/* BỎ KHUNG ĐEN FOOTER BOTTOM & CĂN TRÁI CHUẨN SHIPPER */
.footer-bottom-bar {
  max-width: 1360px;
  margin: 24px auto 0 auto;
  border-top: 1px solid #1E293B;
  padding-top: 20px;
  text-align: left;
  font-size: 12.5px;
  color: #64748B;
  background: transparent !important;
  box-shadow: none !important;
}

/* MODAL STYLING */
.modal-backdrop { position: fixed; inset: 0; background: rgba(15, 23, 42, 0.7); backdrop-filter: blur(6px); display: flex; align-items: center; justify-content: center; z-index: 999; padding: 24px; }
.modal-card-stylish { background: #ffffff; border-radius: 24px; padding: 36px; max-width: 480px; width: 100%; text-align: center; box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2); }
.modal-badge-icon { width: 64px; height: 64px; background: #ECFDF5; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 16px auto; }
.modal-card-stylish h3 { font-size: 20px; font-weight: 900; color: #0F172A; margin-bottom: 8px; }
.modal-card-stylish p { font-size: 13.5px; color: #64748B; line-height: 1.6; margin-bottom: 24px; }
.btn-modal-action-orange {
  background: linear-gradient(135deg, #10B981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  padding: 13px 36px;
  border-radius: 30px;
  font-weight: 900;
  font-size: 14px;
  cursor: pointer;
  box-shadow: 0 6px 18px rgba(16, 185, 129, 0.3);
  transition: all 0.2s;
}
.btn-modal-action-orange:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
}

/* RESPONSIVE */
@media (max-width: 1024px) {
  .hero-content-grid, .calc-body-grid, .faq-support-grid, .footer-inner-grid { grid-template-columns: 1fr; }
  .benefits-6-grid { grid-template-columns: repeat(2, 1fr); }
  .steps-flow-container { flex-direction: column; gap: 20px; }
  .step-flow-arrow { transform: rotate(90deg); }
  .upload-cards-grid { grid-template-columns: repeat(2, 1fr); }
}

@media (max-width: 640px) {
  .benefits-6-grid { grid-template-columns: 1fr; }
  .form-fields-grid { grid-template-columns: 1fr; }
  .upload-cards-grid { grid-template-columns: 1fr; }
  .seller-mascot-peek-container {
    top: -40px;
    right: 12px;
  }
  .mascot-sprite-button {
    width: 68px;
    height: 68px;
  }
  .mascot-speech-bubble {
    max-width: 200px;
    padding: 5px 9px;
    margin-bottom: 0;
  }
  .mascot-speech-bubble .bubble-text {
    font-size: 10px;
  }
}
</style>
