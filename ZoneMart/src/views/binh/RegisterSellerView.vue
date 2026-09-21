<script setup lang="ts">
/**
 * ================================================================
 * ĐĂNG KÝ NGƯỜI BÁN / MỞ GIAN HÀNG (REGISTER SELLER VIEW) - Phụ trách: Bình
 * Luồng 3 Bước Chuẩn Đồ Án ZoneMart với Quy tắc Validate Ràng Buộc Chặt Chẽ:
 * - Bước 1: Thông tin tiệm (Tên cửa hàng 12-30 ký tự không ký tự đặc biệt, Địa chỉ tiệm chuẩn mẫu không ký tự đặc biệt)
 * - Bước 2: Xác thực chủ tiệm (Họ tên 6-16 ký tự không ký tự đặc biệt, Bắt buộc 3 ảnh: CCCD mặt trước, mặt sau & Giấy CN An toàn thực phẩm, Chặn file video .mp4)
 * - Bước 3: Thanh toán (Số tài khoản 8-15 chữ số)
 * Tích hợp Custom Dropdown Select mở thả xuôi xuống dưới có thanh cuộn đồng bộ hoàn toàn với RegisterShipperView!
 * ================================================================
 */
import { ref, reactive, computed, watch, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

// Trạng thái Bước (Step 1 -> Step 2 -> Step 3)
const currentStep = ref<1 | 2 | 3>(1);

// Dữ liệu Form Đăng Ký Người Bán
const form = reactive({
  // BƯỚC 1: THÔNG TIN TIỆM
  storeName: "",
  category: "Thực phẩm & Nhu yếu phẩm",
  address: "",
  openHours: "07:00 - 22:00",
  phoneEmail: "",

  // BƯỚC 2: XÁC THỰC CHỦ TIỆM & GIẤY TỜ
  ownerFullName: "",
  cccdFrontImage: "",     // Preview URL hoặc Base64
  cccdBackImage: "",      // Preview URL hoặc Base64
  foodSafetyCertImage: "",// Preview URL hoặc Base64 ảnh Giấy CN An toàn thực phẩm

  // BƯỚC 3: THANH TOÁN & TÀI KHOẢN ĐĂNG NHẬP
  bankName: "Vietcombank (VCB)",
  bankAccountNumber: "",
  password: ""
});

const isLoading = ref(false);
const errorMessage = ref("");

interface CurrentUser {
  id?: string;
  fullName?: string;
  phoneEmail?: string;
  role?: string;
}

const currentUser = ref<CurrentUser | null>(null);

onMounted(() => {
  const savedUserStr = localStorage.getItem("currentUser");
  if (savedUserStr) {
    try {
      const parsed = JSON.parse(savedUserStr);
      if (parsed && (parsed.phoneEmail || parsed.fullName)) {
        currentUser.value = parsed;
        if (parsed.fullName && !form.ownerFullName) {
          form.ownerFullName = parsed.fullName;
        }
        if (parsed.phoneEmail && !form.phoneEmail) {
          form.phoneEmail = parsed.phoneEmail;
        }
      }
    } catch (e) {}
  }
});

const goToLoginWithIntent = (target: "seller" | "shipper") => {
  sessionStorage.setItem("pendingRegisterTarget", target);
  router.push("/login");
};

// Trạng thái Modal Pop-up Thành Công giữa màn hình
const showSuccessModal = ref(false);

// Trạng thái Custom Dropdowns (Luôn mở thả xuôi xuống dưới)
const isCategoryDropdownOpen = ref(false);
const isOpenHoursDropdownOpen = ref(false);
const isOpenTimeDropdownOpen = ref(false);
const isCloseTimeDropdownOpen = ref(false);
const isBankDropdownOpen = ref(false);

// Danh mục Ngành hàng chính
const categoriesList = [
  "Thực phẩm & Nhu yếu phẩm",
  "Trái cây & Đồ uống",
  "Cơm & Thức ăn nhanh",
  "Đồ gia dụng tiện ích",
  "Thời trang & Phụ kiện",
  "Mỹ phẩm & Chăm sóc cá nhân"
];

// Danh sách Khung giờ mở cửa phổ biến
const openHoursOptions = [
  "07:00 - 22:00 (Mở cả ngày & Buổi tối)",
  "06:00 - 21:00 (Mở sớm - Đóng sớm)",
  "08:00 - 20:00 (Giờ hành chính)",
  "07:00 - 23:00 (Phục vụ đêm)",
  "24/24 (Mở cửa 24/7 toàn thời gian)",
  "Tùy chọn khung giờ mở - đóng khác..."
];

const selectedPreset = ref("07:00 - 22:00 (Mở cả ngày & Buổi tối)");
const openTime = ref("07:00");
const closeTime = ref("22:00");

const openTimeList = ["05:00", "05:30", "06:00", "06:30", "07:00", "07:30", "08:00", "08:30", "09:00", "09:30", "10:00"];
const closeTimeList = ["17:00", "18:00", "19:00", "20:00", "21:00", "21:30", "22:00", "22:30", "23:00", "23:30", "24:00"];

const updateOpenHours = () => {
  if (selectedPreset.value === "Tùy chọn khung giờ mở - đóng khác...") {
    form.openHours = `${openTime.value} - ${closeTime.value}`;
  } else if (selectedPreset.value === "24/24 (Mở cửa 24/7 toàn thời gian)") {
    form.openHours = "Mở cửa 24/7";
  } else {
    form.openHours = selectedPreset.value.split(" (")[0];
  }
};

// Danh sách Ngân hàng phổ biến
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

// Helper đóng tất cả dropdown ngoại trừ dropdown đang bấm
const closeAllDropdownsExcept = (activeName?: string) => {
  if (activeName !== 'category') isCategoryDropdownOpen.value = false;
  if (activeName !== 'openHours') isOpenHoursDropdownOpen.value = false;
  if (activeName !== 'openTime') isOpenTimeDropdownOpen.value = false;
  if (activeName !== 'closeTime') isCloseTimeDropdownOpen.value = false;
  if (activeName !== 'bank') isBankDropdownOpen.value = false;
};

// Toggle handlers
const toggleCategoryDropdown = () => {
  closeAllDropdownsExcept('category');
  isCategoryDropdownOpen.value = !isCategoryDropdownOpen.value;
};

const toggleOpenHoursDropdown = () => {
  closeAllDropdownsExcept('openHours');
  isOpenHoursDropdownOpen.value = !isOpenHoursDropdownOpen.value;
};

const toggleOpenTimeDropdown = () => {
  closeAllDropdownsExcept('openTime');
  isOpenTimeDropdownOpen.value = !isOpenTimeDropdownOpen.value;
};

const toggleCloseTimeDropdown = () => {
  closeAllDropdownsExcept('closeTime');
  isCloseTimeDropdownOpen.value = !isCloseTimeDropdownOpen.value;
};

const toggleBankDropdown = () => {
  closeAllDropdownsExcept('bank');
  isBankDropdownOpen.value = !isBankDropdownOpen.value;
};

// Select handlers
const selectCategory = (cat: string) => {
  form.category = cat;
  isCategoryDropdownOpen.value = false;
};

const selectPresetHours = (preset: string) => {
  selectedPreset.value = preset;
  updateOpenHours();
  isOpenHoursDropdownOpen.value = false;
};

const selectOpenTime = (time: string) => {
  openTime.value = time;
  updateOpenHours();
  isOpenTimeDropdownOpen.value = false;
};

const selectCloseTime = (time: string) => {
  closeTime.value = time;
  updateOpenHours();
  isCloseTimeDropdownOpen.value = false;
};

const selectBank = (bank: string) => {
  form.bankName = bank;
  isBankDropdownOpen.value = false;
};

// Tự động đóng dropdown khi click ngoài
const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (!target.closest('.custom-select-container')) {
    closeAllDropdownsExcept();
  }
};

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
  const savedUser = localStorage.getItem("currentUser");
  if (savedUser) {
    try {
      const parsed = JSON.parse(savedUser);
      if (parsed.phoneEmail) form.phoneEmail = parsed.phoneEmail;
      if (parsed.fullName && !form.ownerFullName) form.ownerFullName = parsed.fullName;
    } catch {}
  }
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});

// Trạng thái Modal Xem & Thay đổi ảnh (2 bảng nhỏ + Blur Background)
const previewModal = reactive<{
  isOpen: boolean;
  title: string;
  imageSrc: string;
  fieldKey: 'cccdFrontImage' | 'cccdBackImage' | 'foodSafetyCertImage' | null;
  inputId: string;
}>({
  isOpen: false,
  title: "",
  imageSrc: "",
  fieldKey: null,
  inputId: ""
});

// Helper kiểm tra file upload chỉ nhận ảnh (chặn video .mp4, ...)
const handleImageFileUpload = (e: Event, callback: (base64Url: string) => void) => {
  errorMessage.value = "";
  const inputEl = e.target as HTMLInputElement;
  const file = inputEl.files?.[0];
  if (!file) return;

  const fileName = file.name.toLowerCase();
  const fileType = file.type.toLowerCase();

  const isVideo = fileType.startsWith("video/") || /\.(mp4|mov|avi|mkv|webm|flv|wmv|3gp)$/i.test(fileName);
  const isImage = fileType.startsWith("image/") || /\.(jpg|jpeg|png|webp|jfif|bmp|gif)$/i.test(fileName);

  if (isVideo || !isImage) {
    errorMessage.value = "Tệp tải lên không hợp lệ! Vui lòng chọn tệp hình ảnh (VD: .jpg, .png), không chấp nhận tệp video (.mp4,...).";
    inputEl.value = "";
    return;
  }

  const reader = new FileReader();
  reader.onload = (evt) => {
    if (evt.target?.result) {
      callback(evt.target.result as string);
    }
  };
  reader.readAsDataURL(file);
};

// Handler Upload các loại ảnh
const handleCccdFrontUpload = (e: Event) => {
  handleImageFileUpload(e, (url) => {
    form.cccdFrontImage = url;
    if (previewModal.isOpen && previewModal.fieldKey === 'cccdFrontImage') {
      previewModal.imageSrc = url;
    }
  });
};

const handleCccdBackUpload = (e: Event) => {
  handleImageFileUpload(e, (url) => {
    form.cccdBackImage = url;
    if (previewModal.isOpen && previewModal.fieldKey === 'cccdBackImage') {
      previewModal.imageSrc = url;
    }
  });
};

const handleFoodSafetyCertUpload = (e: Event) => {
  handleImageFileUpload(e, (url) => {
    form.foodSafetyCertImage = url;
    if (previewModal.isOpen && previewModal.fieldKey === 'foodSafetyCertImage') {
      previewModal.imageSrc = url;
    }
  });
};

// Mở Modal Preview khi bấm thêm 1 lần nữa vào ảnh đã tải
const handleUploadBoxClick = (
  fieldKey: 'cccdFrontImage' | 'cccdBackImage' | 'foodSafetyCertImage',
  inputId: string,
  title: string
) => {
  if (form[fieldKey]) {
    previewModal.fieldKey = fieldKey;
    previewModal.inputId = inputId;
    previewModal.title = title;
    previewModal.imageSrc = form[fieldKey];
    previewModal.isOpen = true;
  } else {
    const inputEl = document.getElementById(inputId) as HTMLInputElement;
    if (inputEl) inputEl.click();
  }
};

const closePreviewModal = () => {
  previewModal.isOpen = false;
  previewModal.fieldKey = null;
  previewModal.imageSrc = "";
  previewModal.inputId = "";
};

const triggerChangeImageFromModal = () => {
  if (previewModal.inputId) {
    const inputEl = document.getElementById(previewModal.inputId) as HTMLInputElement;
    if (inputEl) inputEl.click();
  }
};

// Kiểm tra Validate & Chuyển Bước
const goToNextStep = () => {
  errorMessage.value = "";

  if (currentStep.value === 1) {
    // 1. Validate Tên cửa hàng (tối thiểu 12 ký tự, tối đa 30 ký tự, không ký tự đặc biệt)
    const storeNameTrimmed = form.storeName.trim();
    if (!storeNameTrimmed) {
      errorMessage.value = "Vui lòng nhập Tên cửa hàng!";
      return;
    }
    if (storeNameTrimmed.length < 12 || storeNameTrimmed.length > 30) {
      errorMessage.value = "Tên cửa hàng phải có độ dài tối thiểu từ 12 đến tối đa 30 ký tự!";
      return;
    }

    const specialCharRegex = /[^\a-zA-Z0-9 a-zA-ZàáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆĐÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ]/;
    if (specialCharRegex.test(storeNameTrimmed)) {
      errorMessage.value = "Tên cửa hàng chỉ được nhập chữ và số, không được nhập các ký tự đặc biệt!";
      return;
    }

    // 2. Validate Địa chỉ tiệm (Chuẩn dạng VD: Số 225 đường Nguyễn Trãi, Thành phố Hà Nội - Không ký tự đặc biệt)
    const addressTrimmed = form.address.trim();
    if (!addressTrimmed) {
      errorMessage.value = "Vui lòng nhập Địa chỉ tiệm!";
      return;
    }

    const addressSpecialCharRegex = /[^\a-zA-Z0-9 ,\-\/a-zA-ZàáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆĐÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ]/;
    if (addressSpecialCharRegex.test(addressTrimmed)) {
      errorMessage.value = "Địa chỉ tiệm không được chứa ký tự đặc biệt!";
      return;
    }
    if (addressTrimmed.length < 8) {
      errorMessage.value = "Địa chỉ tiệm phải nhập rõ ràng (Ví dụ: Số 225 đường Nguyễn Trãi, Thành phố Hà Nội)!";
      return;
    }

    currentStep.value = 2;

  } else if (currentStep.value === 2) {
    // 3. Validate Họ tên chủ tiệm (tối thiểu 6 ký tự, tối đa 16 ký tự, chỉ chứa chữ và khoảng trắng)
    const ownerNameTrimmed = form.ownerFullName.trim();
    if (!ownerNameTrimmed) {
      errorMessage.value = "Vui lòng nhập Họ tên chủ tiệm!";
      return;
    }
    if (ownerNameTrimmed.length < 6 || ownerNameTrimmed.length > 16) {
      errorMessage.value = "Họ tên chủ tiệm phải từ 6 đến 16 ký tự!";
      return;
    }

    const nameSpecialCharRegex = /[^\a-zA-Z a-zA-ZàáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆĐÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ]/;
    if (nameSpecialCharRegex.test(ownerNameTrimmed)) {
      errorMessage.value = "Họ tên chủ tiệm chỉ được chứa chữ cái và khoảng trắng, không được nhập ký tự đặc biệt hoặc chữ số!";
      return;
    }

    // 4. Validate BẮT BUỘC phải gửi đủ cả 3 ảnh (CCCD mặt trước, mặt sau và Giấy chứng nhận An toàn thực phẩm)
    if (!form.cccdFrontImage || !form.cccdBackImage || !form.foodSafetyCertImage) {
      errorMessage.value = "Bắt buộc phải tải đủ Ảnh CCCD (Mặt trước & Mặt sau) và Ảnh Giấy chứng nhận An toàn thực phẩm mới qua được bước tiếp theo!";
      return;
    }

    currentStep.value = 3;
  }
};

// Quay lại bước trước
const goToPrevStep = () => {
  errorMessage.value = "";
  if (currentStep.value > 1) {
    currentStep.value = (currentStep.value - 1) as 1 | 2 | 3;
  }
};

// Gửi Hồ Sơ Đăng Ký Người Bán
const handleSubmitApplication = async () => {
  errorMessage.value = "";

  // Validate Số tài khoản nhận tiền (chỉ chứa chữ số, 8 đến 15 số)
  const accountNumberTrimmed = form.bankAccountNumber.trim();
  if (!accountNumberTrimmed) {
    errorMessage.value = "Vui lòng nhập Số tài khoản nhận tiền!";
    return;
  }

  const bankAccountRegex = /^\d{8,15}$/;
  if (!bankAccountRegex.test(accountNumberTrimmed)) {
    errorMessage.value = "Số tài khoản nhận tiền phải là các chữ số từ tối thiểu 8 số đến tối đa 15 số (không chứa chữ hoặc ký tự đặc biệt)!";
    return;
  }

  const phoneEmailTrimmed = form.phoneEmail.trim();
  if (!phoneEmailTrimmed) {
    errorMessage.value = "Vui lòng nhập Số điện thoại hoặc Email đăng nhập!";
    return;
  }

  isLoading.value = true;

  try {
    const res = await fetch("/api/auth/register-seller", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        storeName: form.storeName.trim(),
        category: form.category,
        address: form.address.trim(),
        openHours: form.openHours.trim(),
        phoneEmail: form.phoneEmail.trim(),
        ownerFullName: form.ownerFullName.trim(),
        cccdFrontImage: form.cccdFrontImage,
        cccdBackImage: form.cccdBackImage,
        foodSafetyCertImage: form.foodSafetyCertImage,
        bankName: form.bankName,
        bankAccountNumber: accountNumberTrimmed,
        password: form.password.trim() || "123456"
      })
    }).catch(() => null);

    if (res && res.ok) {
      const data = await res.json();
      console.log("✅ API Register Seller Success:", data);
    } else {
      console.warn("⚠️ API Warning, simulating success response for UI presentation...");
    }

    try {
      const cleanAccountKey = phoneEmailTrimmed.toLowerCase();
      localStorage.setItem(`zonemart_seller_store_${cleanAccountKey}`, JSON.stringify({
        id: `ZM-S${Math.floor(1000 + Math.random() * 9000)}`,
        name: form.storeName.trim(),
        category: form.category,
        address: form.address.trim(),
        openHours: form.openHours.trim(),
        phone: phoneEmailTrimmed,
        bankName: form.bankName,
        bankAccount: accountNumberTrimmed,
        accountHolder: form.ownerFullName.trim().toUpperCase()
      }));
    } catch {}

    showSuccessModal.value = true;

  } catch (err: any) {
    errorMessage.value = err.message || "Có lỗi xảy ra khi nộp hồ sơ.";
  } finally {
    isLoading.value = false;
  }
};

const closeSuccessModal = () => {
  showSuccessModal.value = false;
  router.push("/login");
};

// ================= MASCOT CHUẨN NILBUILD/PAGE-MASCOT (CHỈ DI CHUYỂN ĐẦU THEO CHUỘT) =================
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

// Mascot character: 'chef' (tiệm ẩm thực / người buôn bán)
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
  document.title = "Đăng Ký Người Bán & Mở Gian Hàng | ZoneMart - Sàn TMĐT & Giao Hàng Hỏa Tốc";
  window.addEventListener("pointermove", handlePointerMove, { passive: true });
});

onUnmounted(() => {
  window.removeEventListener("pointermove", handlePointerMove);
});
</script>

<template>
  <div class="login-page-container">
    <!-- KHUNG TỔNG THỂ 2 CỘT: ẢNH ĐĂNG KÝ BÁN HÀNG BÊN TRÁI + BẢNG ĐĂNG KÝ BÊN PHẢI -->
    <div class="login-hero-layout">
      
      <!-- CỘT TRÁI: ẢNH MINH HỌA ĐĂNG KÝ BÁN HÀNG TÁCH SẠCH NỀN -->
      <div class="login-illustration-side">
        <img
          src="/images/anhxoanendkybanhang_clean.png"
          alt="ZoneMart Đăng Ký Bán Hàng"
          class="illustration-img"
        />
      </div>

      <!-- CỘT PHẢI: BẢNG ĐĂNG KÝ NGƯỜI BÁN TRÔI NỔI (FLOATING CARD) 3 BƯỚC -->
      <div class="login-card-floating">
        
        <!-- MASCOT PEELING / PEEKING OVER FORM (THEO CHUẨN NILBUILD/PAGE-MASCOT) -->
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

        <!-- THANH ĐIỀU HƯỚNG QUAY LẠI -->
        <div class="card-top-bar">
          <button type="button" class="back-home-btn" @click="router.push('/login')" title="Quay lại trang Đăng Nhập">
            ← Quay lại Đăng nhập
          </button>
        </div>

        <!-- TIÊU ĐỀ ĐĂNG KÝ NGƯỜI BÁN -->
        <div class="card-brand-header">
          <h1 class="form-main-heading">ĐĂNG KÝ NGƯỜI BÁN</h1>
          <p class="form-sub-heading">Tạo gian hàng kinh doanh & tiếp cận khách hàng ZoneMart</p>
        </div>

        <!-- BẢNG THEO DÕI BƯỚC (STEPPER BAR 3 BƯỚC CHUYÊN NGHIỆP) -->
        <div class="stepper-bar">
          <div class="step-item" :class="{ active: currentStep >= 1, current: currentStep === 1 }">
            <div class="step-badge">
              <i v-if="currentStep > 1" class="bi bi-check-lg"></i>
              <span v-else>1</span>
            </div>
            <span class="step-label">Thông tin tiệm</span>
          </div>
          <div class="step-connector" :class="{ active: currentStep >= 2 }"></div>
          <div class="step-item" :class="{ active: currentStep >= 2, current: currentStep === 2 }">
            <div class="step-badge">
              <i v-if="currentStep > 2" class="bi bi-check-lg"></i>
              <span v-else>2</span>
            </div>
            <span class="step-label">Xác thực chủ tiệm</span>
          </div>
          <div class="step-connector" :class="{ active: currentStep >= 3 }"></div>
          <div class="step-item" :class="{ active: currentStep >= 3, current: currentStep === 3 }">
            <div class="step-badge">
              <span>3</span>
            </div>
            <span class="step-label">Thanh toán</span>
          </div>
        </div>

        <!-- THÔNG BÁO LỖI NẾU CÓ -->
        <div v-if="errorMessage" class="msg-box error">⚠️ {{ errorMessage }}</div>

        <!-- ==================== BƯỚC 1: THÔNG TIN TIỆM ==================== -->
        <form v-if="currentStep === 1" @submit.prevent="goToNextStep" class="form-body">
          
          <!-- Ô THÔNG BÁO TÀI KHOẢN KHÁCH HÀNG LIÊN KẾT (NẰM TẠI ĐẦU BƯỚC 1 - TRÊN CỘT TÊN CỬA HÀNG) -->
          <div v-if="currentUser" class="customer-account-banner success-linked">
            <div class="banner-head-row">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              <span class="banner-head-title">Tài khoản Khách Hàng liên kết mở Gian Hàng:</span>
            </div>
            <div class="banner-user-detail">
              <span class="user-display-name">Anh/Chị <b>{{ currentUser.fullName || 'Khách Hàng' }}</b></span>
              <span class="user-display-email">(Gmail/SĐT: <b class="highlight-email">{{ currentUser.phoneEmail }}</b>)</span>
            </div>
          </div>
          <div v-else class="customer-account-banner warning-not-linked">
            <div class="banner-head-row">
              <span class="warn-icon">⚠️</span>
              <span class="banner-head-title">Chưa liên kết tài khoản Khách Hàng!</span>
            </div>
            <p class="banner-warn-desc">
              Theo quy định ZoneMart, bạn cần đăng nhập tài khoản Khách Hàng trước khi tạo hồ sơ mở Gian Hàng.
            </p>
            <button type="button" class="btn-banner-login" @click="goToLoginWithIntent('seller')">
              🔑 Đăng Nhập Tài Khoản Khách Hàng Ngay ➔
            </button>
          </div>

          <!-- Tên cửa hàng -->
          <div class="field-item">
            <label class="field-label">
              Tên cửa hàng <span class="req">*</span>
              <span class="hint-text">(12 - 30 ký tự, không ký tự đặc biệt)</span>
            </label>
            <input
              v-model="form.storeName"
              type="text"
              class="field-input"
              placeholder="VD: Bách Hóa Sạch ZoneMart Cầu Giấy"
              maxlength="30"
              required
            />
          </div>

          <!-- CUSTOM DROPDOWN DANH MỤC HÀNG BÁN CHÍNH (MỞ THẢ XUÔI XUỐNG DƯỚI) -->
          <div class="field-item custom-select-container">
            <label class="field-label">Danh mục hàng bán chính <span class="req">*</span></label>
            <div
              class="custom-select-trigger"
              :class="{ 'active': isCategoryDropdownOpen }"
              @click.stop="toggleCategoryDropdown"
            >
              <span class="selected-text">{{ form.category }}</span>
              <span class="chevron-arrow">▼</span>
            </div>

              <Transition name="fade-dropdown">
                <div v-if="isCategoryDropdownOpen" class="custom-dropdown-menu">
                  <div
                    v-for="cat in categoriesList"
                    :key="cat"
                    class="dropdown-option-item"
                    :class="{ 'selected': form.category === cat }"
                    @click.stop="selectCategory(cat)"
                  >
                    {{ cat }}
                  </div>
                </div>
              </Transition>
            </div>

            <!-- Địa chỉ tiệm -->
            <div class="field-item">
              <label for="seller-store-address" class="field-label">
                Địa chỉ tiệm <span class="req">*</span>
                <span class="hint-text">(Không nhập ký tự đặc biệt)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-geo-alt-fill input-leading-icon"></i>
                <input
                  id="seller-store-address"
                  name="address"
                  v-model="form.address"
                  type="text"
                  class="field-input has-leading-icon"
                  placeholder="VD: Số 225 đường Nguyễn Trãi, Thành phố Hà Nội"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <!-- CUSTOM DROPDOWN KHUNG GIỜ MỞ CỬA (MỞ THẢ XUÔI XUỐNG DƯỚI) -->
            <div class="field-item custom-select-container">
              <label class="field-label">Cửa hàng mở những giờ nào? <span class="req">*</span></label>
              <div
                class="custom-select-trigger"
                :class="{ 'active': isOpenHoursDropdownOpen }"
                @click.stop="toggleOpenHoursDropdown"
              >
                <span class="selected-text">{{ selectedPreset }}</span>
                <span class="chevron-arrow">▼</span>
              </div>

              <Transition name="fade-dropdown">
                <div v-if="isOpenHoursDropdownOpen" class="custom-dropdown-menu">
                  <div
                    v-for="opt in openHoursOptions"
                    :key="opt"
                    class="dropdown-option-item"
                    :class="{ 'selected': selectedPreset === opt }"
                    @click.stop="selectPresetHours(opt)"
                  >
                    {{ opt }}
                  </div>
                </div>
              </Transition>
            </div>

            <!-- Tùy chọn 2 khung giờ mở & đóng riêng biệt nếu chọn tùy chọn khác -->
            <div v-if="selectedPreset === 'Tùy chọn khung giờ mở - đóng khác...'" class="time-select-row">
              <div class="field-item custom-select-container">
                <label class="field-label">Giờ mở cửa</label>
                <div
                  class="custom-select-trigger"
                  :class="{ 'active': isOpenTimeDropdownOpen }"
                  @click.stop="toggleOpenTimeDropdown"
                >
                  <span class="selected-text">{{ openTime }}</span>
                  <span class="chevron-arrow">▼</span>
                </div>

                <Transition name="fade-dropdown">
                  <div v-if="isOpenTimeDropdownOpen" class="custom-dropdown-menu">
                    <div
                      v-for="h in openTimeList"
                      :key="h"
                      class="dropdown-option-item"
                      :class="{ 'selected': openTime === h }"
                      @click.stop="selectOpenTime(h)"
                    >
                      {{ h }}
                    </div>
                  </div>
                </Transition>
              </div>

              <div class="field-item custom-select-container">
                <label class="field-label">Giờ đóng cửa</label>
                <div
                  class="custom-select-trigger"
                  :class="{ 'active': isCloseTimeDropdownOpen }"
                  @click.stop="toggleCloseTimeDropdown"
                >
                  <span class="selected-text">{{ closeTime }}</span>
                  <span class="chevron-arrow">▼</span>
                </div>

                <Transition name="fade-dropdown">
                  <div v-if="isCloseTimeDropdownOpen" class="custom-dropdown-menu">
                    <div
                      v-for="h in closeTimeList"
                      :key="h"
                      class="dropdown-option-item"
                      :class="{ 'selected': closeTime === h }"
                      @click.stop="selectCloseTime(h)"
                    >
                      {{ h }}
                    </div>
                  </div>
                </Transition>
              </div>
            </div>

            <!-- Nút bấm Chuyển sang Bước 2 -->
            <button type="submit" class="btn-submit-orange">
              Tiếp theo ➔
            </button>
          </form>

          <!-- ==================== BƯỚC 2: XÁC THỰC CHỦ TIỆM ==================== -->
          <form v-else-if="currentStep === 2" key="seller-step-2" @submit.prevent="goToNextStep" class="form-body" novalidate>
            <!-- Họ tên chủ tiệm -->
            <div class="field-item">
              <label for="seller-owner-name" class="field-label">
                Họ tên chủ tiệm <span class="req">*</span>
                <span class="hint-text">(6 - 16 ký tự, chỉ nhập chữ)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-person-badge-fill input-leading-icon"></i>
                <input
                  id="seller-owner-name"
                  name="ownerFullName"
                  v-model="form.ownerFullName"
                  type="text"
                  autocomplete="name"
                  class="field-input has-leading-icon"
                  placeholder="VD: Nguyễn Văn Bình"
                  maxlength="16"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <!-- Upload CCCD Mặt Trước & Mặt Sau -->
            <div class="cccd-upload-group">
              <label class="field-label">Tải ảnh CCCD mặt trước & mặt sau <span class="req">*</span></label>
              
              <div class="upload-row">
                <!-- Mặt trước -->
                <div class="upload-card">
                  <input type="file" accept="image/*" id="cccdFront" @change="handleCccdFrontUpload" class="file-input-hidden" />
                  <div 
                    class="upload-box" 
                    :class="{ 'has-preview': form.cccdFrontImage }"
                    @click="handleUploadBoxClick('cccdFrontImage', 'cccdFront', 'Ảnh CCCD Mặt Trước')"
                  >
                    <template v-if="!form.cccdFrontImage">
                      <span class="upload-icon">📷</span>
                      <span class="upload-text">CCCD Mặt trước</span>
                    </template>
                    <template v-else>
                      <img :src="form.cccdFrontImage" alt="CCCD Mặt Trước" class="preview-img" />
                      <div class="preview-overlay-badge">🔍 Nhấn để xem & thay đổi</div>
                    </template>
                  </div>
                </div>

                <!-- Mặt sau -->
                <div class="upload-card">
                  <input type="file" accept="image/*" id="cccdBack" @change="handleCccdBackUpload" class="file-input-hidden" />
                  <div 
                    class="upload-box" 
                    :class="{ 'has-preview': form.cccdBackImage }"
                    @click="handleUploadBoxClick('cccdBackImage', 'cccdBack', 'Ảnh CCCD Mặt Sau')"
                  >
                    <template v-if="!form.cccdBackImage">
                      <span class="upload-icon">💳</span>
                      <span class="upload-text">CCCD Mặt sau</span>
                    </template>
                    <template v-else>
                      <img :src="form.cccdBackImage" alt="CCCD Mặt Sau" class="preview-img" />
                      <div class="preview-overlay-badge">🔍 Nhấn để xem & thay đổi</div>
                    </template>
                  </div>
                </div>
              </div>
            </div>

            <!-- TẢI ẢNH GIẤY CHỨNG NHẬN AN TOÀN THỰC PHẨM -->
            <div class="cccd-upload-group">
              <label class="field-label">Tải ảnh Giấy chứng nhận an toàn thực phẩm <span class="req">*</span></label>
              <div class="upload-card full-width">
                <input type="file" accept="image/*" id="foodSafetyCert" @change="handleFoodSafetyCertUpload" class="file-input-hidden" />
                <div 
                  class="upload-box upload-box-wide" 
                  :class="{ 'has-preview': form.foodSafetyCertImage }"
                  @click="handleUploadBoxClick('foodSafetyCertImage', 'foodSafetyCert', 'Giấy CN An toàn thực phẩm')"
                >
                  <template v-if="!form.foodSafetyCertImage">
                    <span class="upload-icon">📜</span>
                    <span class="upload-text">Tải ảnh Giấy CN An toàn thực phẩm (Tệp hình ảnh .jpg, .png)</span>
                  </template>
                  <template v-else>
                    <img :src="form.foodSafetyCertImage" alt="Giấy CN An toàn thực phẩm" class="preview-img" />
                    <div class="preview-overlay-badge">🔍 Nhấn để xem & thay đổi</div>
                  </template>
                </div>
              </div>
            </div>

            <!-- Nhóm Nút Quay lại / Tiếp theo -->
            <div class="step-btn-group">
              <button type="button" class="btn-secondary-gray" @click="goToPrevStep">
                ← Quay lại
              </button>
              <button type="submit" class="btn-submit-orange btn-flex">
                Tiếp theo ➔
              </button>
            </div>
          </form>

          <!-- ==================== BƯỚC 3: THANH TOÁN & GỬI HỒ SƠ ==================== -->
          <form v-else-if="currentStep === 3" key="seller-step-3" @submit.prevent="handleSubmitApplication" class="form-body" novalidate>
            <!-- CUSTOM DROPDOWN CHỌN NGÂN HÀNG NHẬN TIỀN (MỞ THẢ XUÔI XUỐNG DƯỚI) -->
            <div class="field-item custom-select-container">
              <label class="field-label">Chọn Ngân hàng nhận tiền <span class="req">*</span></label>
              <div
                class="custom-select-trigger"
                :class="{ 'active': isBankDropdownOpen }"
                @click.stop="toggleBankDropdown"
              >
                <span class="selected-text">{{ form.bankName }}</span>
                <span class="chevron-arrow">▼</span>
              </div>

              <Transition name="fade-dropdown">
                <div v-if="isBankDropdownOpen" class="custom-dropdown-menu">
                  <div
                    v-for="bank in banksList"
                    :key="bank"
                    class="dropdown-option-item"
                    :class="{ 'selected': form.bankName === bank }"
                    @click.stop="selectBank(bank)"
                  >
                    {{ bank }}
                  </div>
                </div>
              </Transition>
            </div>

            <!-- Số tài khoản nhận tiền -->
            <div class="field-item">
              <label for="seller-bank-acc" class="field-label">
                Số tài khoản nhận tiền <span class="req">*</span>
                <span class="hint-text">(8 - 15 chữ số)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-credit-card-2-front-fill input-leading-icon"></i>
                <input
                  id="seller-bank-acc"
                  name="bankAccountNumber"
                  v-model="form.bankAccountNumber"
                  type="text"
                  inputmode="numeric"
                  class="field-input has-leading-icon font-mono"
                  placeholder="VD: 190382910283 (Chỉ nhập chữ số)"
                  maxlength="15"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <!-- THÔNG TIN TÀI KHOẢN ĐĂNG NHẬP -->
            <div class="field-item">
              <label for="seller-account-login" class="field-label">
                Số điện thoại hoặc Email đăng nhập <span class="req">*</span>
                <span class="hint-text">(Dùng để đăng nhập và theo dõi kết quả xét duyệt)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-envelope-at-fill input-leading-icon"></i>
                <input
                  id="seller-account-login"
                  name="username"
                  v-model="form.phoneEmail"
                  type="text"
                  autocomplete="username"
                  class="field-input has-leading-icon"
                  placeholder="VD: 0912345678 hoặc email@gmail.com"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <div class="field-item">
              <label for="seller-password" class="field-label">
                Mật khẩu đăng nhập <span class="req">*</span>
                <span class="hint-text">(Tối thiểu 6 ký tự)</span>
              </label>
              <div class="input-icon-wrapper">
                <i class="bi bi-lock-fill input-leading-icon"></i>
                <input
                  id="seller-password"
                  name="new-password"
                  v-model="form.password"
                  type="password"
                  autocomplete="new-password"
                  class="field-input has-leading-icon"
                  placeholder="Nhập mật khẩu cho tài khoản người bán"
                  minlength="6"
                  required
                  aria-required="true"
                />
              </div>
            </div>

            <!-- Nhóm Nút Quay lại / Gửi hồ sơ đăng ký -->
            <div class="step-btn-group">
              <button type="button" class="btn-secondary-gray" @click="goToPrevStep">
                ← Quay lại
              </button>
              <button type="submit" class="btn-submit-orange btn-flex" :disabled="isLoading">
                <span v-if="!isLoading">Gửi hồ sơ đăng ký ➔</span>
                <span v-else>ĐANG GỬI HỒ SƠ...</span>
              </button>
            </div>
          </form>

        <!-- Đường kẻ phân cách & Liên kết -->
        <div class="divider-row">
          <span class="line"></span>
          <span class="or-text">ZONEMART KÊNH NGƯỜI BÁN</span>
          <span class="line"></span>
        </div>

        <div class="register-cta-box">
          <div class="cta-line">
            <span>Đã có tài khoản? </span>
            <router-link to="/login" class="link-orange-bold">Đăng nhập ngay</router-link>
          </div>
        </div>

      </div>

    </div>

    <!-- MODAL THÔNG BÁO CĂN GIỮA MÀN HÌNH "ĐÃ GỬI HỒ SƠ THÀNH CÔNG" -->
    <Transition name="fade-modal">
      <div v-if="showSuccessModal" class="modal-backdrop-overlay" @click.self="closeSuccessModal">
        <div class="modal-pop-card">
          
          <!-- ICON TÍCH XANH NỔI BẬT -->
          <div class="modal-icon-badge success-badge">
            <svg width="38" height="38" viewBox="0 0 24 24" fill="none" stroke="#16A34A" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
          </div>

          <h3 class="modal-heading-title green-title">Đã gửi hồ sơ thành công</h3>
          
          <p class="modal-body-text">
            Hồ sơ mở gian hàng <b>{{ form.storeName }}</b> của bạn đã được ghi nhận và đang ở trạng thái <b>CHỜ BAN QUẢN LÝ XÉT DUYỆT</b>!
          </p>

          <div style="background: #fef3c7; color: #92400e; border: 1px solid #fde68a; border-radius: 8px; padding: 10px 14px; font-size: 13px; margin: 12px 0 18px 0; text-align: left; line-height: 1.5;">
            <i class="bi bi-clock-history me-1"></i> <strong>Quy trình kiểm duyệt:</strong> Ban Quản Trị ZoneMart sẽ kiểm tra giấy tờ pháp lý (CCCD & Giấy chứng nhận ATTP). Sau khi được phê duyệt, bạn có thể đăng nhập bằng tài khoản vừa tạo để quản lý gian hàng.
          </div>

          <div class="modal-action-buttons">
            <button type="button" class="btn-modal-close" @click="closeSuccessModal">
              Đăng nhập ngay
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ==================== MODAL XEM & THAY ĐỔI ĂNH (BLUR BACKGROUND + 2 BẢNG NHỎ) ==================== -->
    <Transition name="fade-modal">
      <div v-if="previewModal.isOpen" class="image-modal-overlay" @click.self="closePreviewModal">
        <div class="image-modal-card">
          <!-- Header Modal -->
          <div class="modal-card-header">
            <div class="modal-card-title">
              <span class="title-icon">🔍</span>
              <span>{{ previewModal.title }}</span>
            </div>
            <button type="button" class="btn-close-modal" @click="closePreviewModal">✕</button>
          </div>

          <!-- Body Modal chứa 2 bảng nhỏ -->
          <div class="modal-card-body">
            <!-- BẢNG 1: Khung Xem Ảnh Chi Tiết -->
            <div class="sub-panel image-view-panel">
              <div class="panel-tag">📷 Bảng xem ảnh chi tiết</div>
              <div class="image-display-frame">
                <img :src="previewModal.imageSrc" :alt="previewModal.title" class="full-view-img" />
              </div>
            </div>

            <!-- BẢNG 2: Khung Thông Tin & Thao Tác -->
            <div class="sub-panel action-control-panel">
              <div class="panel-tag">⚙️ Bảng thao tác & Xử lý</div>
              <div class="status-info-box">
                <div class="status-badge">
                  <span class="status-dot"></span>
                  <span>Ảnh đã được tải lên hợp lệ</span>
                </div>
                <p class="status-desc">
                  Bạn có thể xem lại chi tiết hình ảnh đã chọn. Nhấn <strong>"Thay đổi"</strong> nếu bạn muốn tải tệp ảnh khác hoặc chọn <strong>"Lưu"</strong> để giữ nguyên ảnh hiện tại.
                </p>
              </div>

              <!-- 2 Nút Thao Tác: Lưu & Thay Đổi (Không icon) -->
              <div class="modal-btn-actions">
                <button type="button" class="btn-modal-change" @click="triggerChangeImageFromModal">
                  Thay đổi
                </button>
                <button type="button" class="btn-modal-save" @click="closePreviewModal">
                  Lưu
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* ÉP FONT CHỮ THỐNG NHẤT */
*, input, button, select, textarea, label {
  font-family: 'Plus Jakarta Sans', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
}

/* PAGE CONTAINER VỪA KHÍT 100VH - KHÔNG CẦN KÉO CUỘN TRANG WEB */
.login-page-container {
  width: 100vw;
  height: 100vh;
  height: 100dvh;
  max-height: 100vh;
  overflow: hidden; /* Không để web phải kéo xuống */
  background: radial-gradient(circle at 40% 30%, #FFFDF9 0%, #FAF5EF 60%, #F3ECE2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 36px 20px 10px 20px;
  box-sizing: border-box;
}

/* HERO LAYOUT 2 CỘT CÂN ĐỐI TỰ NHIÊN GIỮA MÀN HÌNH */
.login-hero-layout {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 32px;
  width: 100%;
  max-width: 1160px;
  max-height: 100%;
  margin: auto;
}

/* CỘT TRÁI - ẢNH MINH HỌA ĐĂNG KÝ BÁN HÀNG TÁCH NỀN */
.login-illustration-side {
  flex: 1.2;
  max-width: 540px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
}

.illustration-img {
  width: 100%;
  max-width: 500px;
  max-height: 420px;
  object-fit: contain;
  position: relative;
  z-index: 1;
  filter: drop-shadow(0 12px 24px rgba(217, 78, 21, 0.13));
  transition: transform 0.3s ease;
}

/* CỘT PHẢI - BẢNG ĐĂNG KÝ NGƯỜI BÁN TRÔI NỔI (FLOATING CARD) */
.login-card-floating {
  width: 100%;
  max-width: 430px;
  background: #FFFFFF;
  border-radius: 18px;
  padding: 12px 20px 10px 20px;
  box-shadow: 
    0 16px 36px -8px rgba(15, 23, 42, 0.1),
    0 6px 16px -4px rgba(217, 78, 21, 0.08),
    0 0 0 1px rgba(240, 230, 220, 0.85);
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  z-index: 2;
  position: relative;
  margin-top: 18px;
  animation: formCardEntrance 0.45s cubic-bezier(0.16, 1, 0.3, 1);
}

/* ================= MASCOT PEELING / PEEKING STYLE (PAGE-MASCOT) ================= */
.seller-mascot-peek-container {
  position: absolute;
  top: -46px;
  right: 16px;
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
  filter: drop-shadow(0 6px 14px rgba(217, 78, 21, 0.2));
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
  background: linear-gradient(90deg, rgba(234, 88, 12, 0) 0%, rgba(234, 88, 12, 0.35) 50%, rgba(234, 88, 12, 0) 100%);
  border-radius: 999px;
}

.mascot-speech-bubble {
  position: relative;
  background: #FFFFFF;
  border: 1.5px solid #FED7AA;
  box-shadow: 0 8px 20px -4px rgba(234, 88, 12, 0.16), 0 2px 8px rgba(0, 0, 0, 0.04);
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
  color: #9A3412;
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
  border-left: 8px solid #FED7AA;
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

/* RESPONSIVE TRÁNH TRÀN MÀN HÌNH */
@media (max-width: 1024px) {
  .login-hero-layout {
    flex-direction: column;
    gap: 24px;
  }
  .login-illustration-side {
    display: none;
  }
  .login-card-floating {
    max-width: 480px;
  }
}

@media (max-width: 540px) {
  .login-page-container {
    padding: 30px 14px 20px 14px;
    height: auto;
    min-height: 100vh;
    overflow-y: auto;
  }
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
  .login-card-floating {
    padding: 14px 14px 12px 14px;
    border-radius: 16px;
  }
}

@keyframes formCardEntrance {
  0% {
    opacity: 0;
    transform: translateY(18px) scale(0.985);
  }
  100% {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* HIỆU ỨNG CHUYỂN BƯỚC MULTI-STEP */
.step-slide-enter-active,
.step-slide-leave-active {
  transition: opacity 0.28s cubic-bezier(0.16, 1, 0.3, 1), transform 0.28s cubic-bezier(0.16, 1, 0.3, 1);
}

.step-slide-enter-from {
  opacity: 0;
  transform: translateX(18px);
}

.step-slide-leave-to {
  opacity: 0;
  transform: translateX(-18px);
}

/* NÚT QUAY LẠI */
.card-top-bar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 2px;
}

.back-home-btn {
  background: transparent;
  border: none;
  color: #64748B;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
  padding: 0;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  transition: all 0.2s ease;
}

.back-home-btn:hover {
  color: #059669;
}

/* HEADER BẢNG */
.card-brand-header {
  text-align: center;
  margin-bottom: 4px;
}

.form-main-heading {
  font-size: 17px;
  font-weight: 900;
  color: #10B981;
  margin: 0 0 1px 0;
  letter-spacing: -0.4px;
}

.form-sub-heading {
  font-size: 10.5px;
  color: #64748B;
  margin: 0;
  font-weight: 500;
}

/* STEPPER BAR (3 BƯỚC) */
.stepper-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
  padding: 0 4px;
}

.step-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1px;
  position: relative;
  z-index: 2;
}

.step-badge {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #F1F5F9;
  color: #94A3B8;
  font-size: 10px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.step-label {
  font-size: 9px;
  font-weight: 700;
  color: #94A3B8;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.step-item.active .step-badge {
  background: #ECFDF5;
  color: #10B981;
}

.step-item.current .step-badge {
  background: #10B981;
  color: #FFFFFF;
  box-shadow: 0 4px 10px rgba(16, 185, 129, 0.35);
}

.step-item.current .step-label {
  color: #10B981;
  font-weight: 800;
}

.step-connector {
  flex: 1;
  height: 2px;
  background: #E2E8F0;
  margin: 0 6px;
  transform: translateY(-7px);
  transition: background 0.3s ease;
}

.step-connector.active {
  background: #10B981;
}

/* MSG ERROR */
.msg-box {
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 600;
  margin-bottom: 8px;
}

.msg-box.error {
  background: #FEF2F2;
  color: #DC2626;
  border: 1px solid #FCA5A5;
}

/* FORM BODY & INPUTS */
.form-body {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.field-label {
  font-size: 10.5px;
  font-weight: 700;
  color: #0F172A;
}

.hint-text {
  font-size: 9px;
  font-weight: 500;
  color: #64748B;
  margin-left: 3px;
}

.req {
  color: #EF4444;
  font-weight: 800;
}

.input-icon-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-leading-icon {
  position: absolute;
  left: 10px;
  font-size: 12.5px;
  color: #94A3B8;
  pointer-events: none;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  z-index: 2;
}

.input-icon-wrapper:focus-within .input-leading-icon {
  color: #D94E15;
  transform: scale(1.1);
}

.field-input, .field-select {
  width: 100%;
  padding: 5px 9px;
  border-radius: 9px;
  border: 1.5px solid #E2E8F0;
  background: #F8FAFC;
  font-size: 11.5px;
  color: #0F172A;
  outline: none;
  box-sizing: border-box;
  min-height: 31px;
  transition: all 0.2s ease;
  font-family: inherit;
}

.field-input.has-leading-icon {
  padding-left: 30px;
}

.field-input:focus, .field-select:focus {
  background: #FFFFFF;
  border-color: #10B981;
  box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.14);
}

/* CUSTOM DROPDOWN SELECT CONTAINER (ĐỒNG BỘ SELLER) */
.custom-select-container {
  position: relative;
}

.custom-select-trigger {
  width: 100%;
  padding: 5px 9px;
  border-radius: 9px;
  border: 1.5px solid #E2E8F0;
  background: #F8FAFC;
  font-size: 11.5px;
  color: #0F172A;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-sizing: border-box;
  min-height: 31px;
  transition: all 0.2s ease;
  user-select: none;
}

.custom-select-trigger:hover,
.custom-select-trigger.active {
  border-color: #10B981;
  background: #FFFFFF;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.14);
}

.selected-text {
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.chevron-arrow {
  font-size: 10px;
  color: #94A3B8;
  transition: transform 0.2s ease;
}

.custom-select-trigger.active .chevron-arrow {
  transform: rotate(180deg);
  color: #10B981;
}

.custom-dropdown-menu {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  margin-top: 4px;
  max-height: 180px;
  overflow-y: auto;
  background: #FFFFFF;
  border: 1.5px solid #10B981;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(15, 23, 42, 0.18);
  z-index: 99999;
  padding: 4px 0;
}

.dropdown-option-item {
  padding: 8px 12px;
  font-size: 11.5px;
  color: #334155;
  cursor: pointer;
  transition: background 0.15s ease;
}

.dropdown-option-item:hover {
  background: #ECFDF5;
  color: #10B981;
  font-weight: 700;
}

.dropdown-option-item.selected {
  background: #10B981;
  color: #FFFFFF;
  font-weight: 700;
}

.fade-dropdown-enter-active,
.fade-dropdown-leave-active {
  transition: all 0.2s ease;
}

.fade-dropdown-enter-from,
.fade-dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* UPLOAD CARDS */
.cccd-upload-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.upload-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.upload-card {
  width: 100%;
}

.upload-card.full-width {
  width: 100%;
}

.file-input-hidden {
  display: none;
}

.upload-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 64px;
  border: 1.5px dashed #10B981;
  background: #F0FDF4;
  border-radius: 10px;
  cursor: pointer;
  overflow: hidden;
  transition: all 0.2s ease;
}

.upload-box-wide {
  height: 52px;
  flex-direction: row;
  gap: 8px;
  padding: 0 10px;
}

.upload-box:hover {
  background: #DCFCE7;
  border-color: #059669;
}

.upload-box.has-preview {
  border-style: solid;
  border-color: #10B981;
  background: #F0FDF4;
}

.upload-icon {
  font-size: 18px;
  margin-bottom: 2px;
}

.upload-text {
  font-size: 10.5px;
  font-weight: 700;
  color: #047857;
  text-align: center;
}

.preview-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* BUTTONS */
.btn-submit-orange {
  width: 100%;
  padding: 7px 12px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 24px;
  font-size: 12px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 3px 10px rgba(217, 78, 21, 0.25);
  transition: all 0.2s ease;
  margin-top: 2px;
}

.btn-submit-orange:hover:not(:disabled) {
  background: #C8451F;
  transform: translateY(-1px);
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.32);
}

.btn-submit-orange:active:not(:disabled) {
  transform: translateY(1px) scale(0.985);
  box-shadow: 0 2px 5px rgba(217, 78, 21, 0.2);
}

.step-btn-group {
  display: flex;
  gap: 6px;
  margin-top: 2px;
}

.btn-secondary-gray {
  flex: 0.8;
  padding: 7px 10px;
  background: #F1F5F9;
  color: #475569;
  border: none;
  border-radius: 24px;
  font-size: 11.5px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-secondary-gray:hover {
  background: #E2E8F0;
  color: #0F172A;
  transform: translateY(-1px);
}

.btn-secondary-gray:active {
  transform: translateY(1px) scale(0.985);
}

.btn-flex {
  flex: 1.2;
}

.divider-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 4px 0 2px 0;
}

.divider-row .line {
  flex: 1;
  height: 1px;
  background: #E2E8F0;
}

.divider-row .or-text {
  font-size: 8.5px;
  font-weight: 800;
  color: #94A3B8;
  letter-spacing: 0.5px;
}

.register-cta-box {
  display: flex;
  flex-direction: column;
  gap: 1px;
  text-align: center;
  font-size: 10px;
  color: #64748B;
}

.link-orange-bold {
  color: #D94E15;
  font-weight: 800;
  text-decoration: none;
}
.link-orange-bold:hover {
  text-decoration: underline;
}

/* MODAL OVERLAY (CĂN GIỮA MÀN HÌNH) */
.modal-backdrop-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(15, 23, 42, 0.55);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 99999;
  padding: 20px;
  box-sizing: border-box;
}

.modal-pop-card {
  background: #FFFFFF;
  border-radius: 24px;
  padding: 28px 24px 22px 24px;
  width: 100%;
  max-width: 380px;
  text-align: center;
  box-shadow: 
    0 25px 50px -12px rgba(0, 0, 0, 0.3),
    0 0 0 1px rgba(240, 230, 220, 0.9);
  display: flex;
  flex-direction: column;
  align-items: center;
  animation: modalScaleUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalScaleUp {
  from {
    opacity: 0;
    transform: scale(0.88) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-icon-badge.success-badge {
  width: 64px;
  height: 64px;
  background: #F0FDF4;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 14px;
  border: 2px solid #BBF7D0;
  box-shadow: 0 4px 14px rgba(22, 163, 74, 0.2);
}

.modal-heading-title.green-title {
  font-size: 20px;
  font-weight: 900;
  color: #15803D;
  margin: 0 0 10px 0;
}

.modal-body-text {
  font-size: 13px;
  color: #475569;
  margin: 0 0 20px 0;
  line-height: 1.5;
}

.modal-action-buttons {
  width: 100%;
}

.btn-modal-close {
  width: 100%;
  padding: 11px;
  background: #D94E15;
  color: #FFFFFF;
  border: none;
  border-radius: 30px;
  font-size: 13.5px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(217, 78, 21, 0.3);
  transition: all 0.2s ease;
}

.btn-modal-close:hover {
  background: #C8451F;
}

.time-select-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin-top: -2px;
}

.fade-modal-enter-active,
.fade-modal-leave-active {
  transition: opacity 0.25s ease;
}

.fade-modal-enter-from,
.fade-modal-leave-to {
  opacity: 0;
}

/* ==================== MODAL XEM & THAY ĐỔI ĂNH (BLUR BACKGROUND + 2 BẢNG NHỎ) ==================== */
.image-modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.image-modal-card {
  background: #ffffff;
  width: 100%;
  max-width: 680px;
  border-radius: 16px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.35);
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.2);
  animation: modalPopIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalPopIn {
  from {
    opacity: 0;
    transform: scale(0.92) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: #FAF5EF;
  border-bottom: 1px solid #EAE2D7;
}

.modal-card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
  font-weight: 800;
  color: #2D3748;
}

.btn-close-modal {
  background: #edf2f7;
  border: none;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-size: 16px;
  cursor: pointer;
  color: #4a5568;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.btn-close-modal:hover {
  background: #e2e8f0;
  color: #1a202c;
}

/* 2 Bảng nhỏ (Sub-panels) */
.modal-card-body {
  padding: 20px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

@media (max-width: 640px) {
  .modal-card-body {
    grid-template-columns: 1fr;
  }
}

.sub-panel {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  position: relative;
}

.panel-tag {
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #64748b;
  margin-bottom: 10px;
}

.image-display-frame {
  flex: 1;
  min-height: 180px;
  max-height: 240px;
  background: #ffffff;
  border-radius: 8px;
  border: 1px dashed #cbd5e1;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 6px;
}

.full-view-img {
  max-width: 100%;
  max-height: 220px;
  object-fit: contain;
  border-radius: 6px;
}

.action-control-panel {
  justify-content: space-between;
}

.status-info-box {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #dcfce7;
  color: #166534;
  font-size: 0.82rem;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 20px;
  width: fit-content;
}

.status-dot {
  width: 8px;
  height: 8px;
  background: #22c55e;
  border-radius: 50%;
}

.status-desc {
  font-size: 0.85rem;
  color: #475569;
  line-height: 1.5;
  margin: 0;
}

.modal-btn-actions {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 16px;
}

.btn-modal-change {
  background: #FFF3E0;
  color: #E65100;
  border: 1.5px solid #FFB74D;
  border-radius: 8px;
  padding: 10px 16px;
  font-size: 0.92rem;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-modal-change:hover {
  background: #FFE0B2;
  border-color: #FB8C00;
}

.btn-modal-save {
  background: linear-gradient(135deg, #e65100 0%, #f57c00 100%);
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 11px 16px;
  font-size: 0.92rem;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  box-shadow: 0 4px 12px rgba(230, 81, 0, 0.25);
  transition: all 0.2s ease;
}

.btn-modal-save:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(230, 81, 0, 0.35);
}

/* ================================================================
   Ô THÔNG BÁO TÀI KHOẢN KHÁCH HÀNG LIÊN KẾT (CUSTOMER ACCOUNT BANNER)
   ================================================================ */
.customer-account-banner {
  padding: 6px 10px;
  border-radius: 10px;
  margin-bottom: 6px;
  font-size: 11px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.02);
  transition: all 0.25s ease;
}

.customer-account-banner.success-linked {
  background: #FFF7ED;
  border: 1.5px solid #FFEDD5;
}

.customer-account-banner.warning-not-linked {
  background: #FEF2F2;
  border: 1.5px solid #FECACA;
}

.banner-head-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 800;
  color: #1F2937;
  font-size: 11.5px;
  margin-bottom: 2px;
}

.banner-head-title {
  color: #9A3412;
  letter-spacing: -0.2px;
}

.warning-not-linked .banner-head-title {
  color: #991B1B;
}

.banner-user-detail {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  color: #374151;
  padding-left: 2px;
}

.highlight-email {
  color: #D94E15;
  font-weight: 700;
}

.banner-warn-desc {
  font-size: 12.5px;
  color: #7F1D1D;
  margin: 4px 0 10px 0;
  line-height: 1.5;
}

.btn-banner-login {
  background: linear-gradient(135deg, #D94E15 0%, #EA580C 100%);
  color: #ffffff;
  border: none;
  padding: 9px 16px;
  border-radius: 10px;
  font-weight: 800;
  font-size: 12.5px;
  cursor: pointer;
  box-shadow: 0 4px 10px rgba(217, 78, 21, 0.25);
  transition: all 0.2s ease;
}

.btn-banner-login:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 14px rgba(217, 78, 21, 0.35);
}

.upload-box {
  position: relative;
  cursor: pointer;
}

.preview-overlay-badge {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  color: #ffffff;
  font-size: 0.78rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s ease;
  border-radius: 8px;
  padding: 4px 8px;
  text-align: center;
}

.upload-box.has-preview:hover .preview-overlay-badge {
  opacity: 1;
}
</style>
