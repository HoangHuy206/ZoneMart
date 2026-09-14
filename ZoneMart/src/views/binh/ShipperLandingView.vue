<script setup lang="ts">
/**
 * ================================================================
 * TRANG CHỦ & ĐĂNG KÝ SHIPPER HIỆN ĐẠI (ZONEMART DRIVER PORTAL) - Phụ trách: Bình
 * Giao diện hiện đại 2026:
 * - Logo ZoneMart với chữ DRIVER màu xanh neon bên dưới (Không khung box).
 * - Hình minh họa Hero kích thước lớn với hiệu ứng Floating Animation mượt mà.
 * - 100% Icon được vẽ bằng SVG Code rực rỡ, hiệu ứng viền phát sáng & Micro-animations.
 * - Công cụ tính thu nhập dự kiến tương tác (Interactive Income Calculator).
 * - Bảng mốc thưởng nổ đơn & Góc câu hỏi thường gặp FAQ.
 * - Form đăng ký Neumorphism/Apple-style cao cấp với Drag & Drop Upload card.
 * - Footer đầy đủ chuẩn ZoneMart dành riêng cho Shipper.
 * ================================================================
 */
import { ref, reactive, computed, onMounted, onUnmounted } from "vue";
import { useRouter, useRoute } from "vue-router";

const router = useRouter();
const route = useRoute();

// Trạng thái Bước Form Đăng Ký Shipper (Step 1 -> Step 2 -> Step 3)
const currentStep = ref<1 | 2 | 3>(1);

// Dữ liệu Form Đăng Ký Shipper
const form = reactive({
  // BƯỚC 1: THÔNG TIN CÁ NHÂN & ĐĂNG NHẬP
  fullName: "",
  phoneNumber: "",
  password: "",
  confirmPassword: "",
  cccdNumber: "",
  cccdFrontImage: "",
  cccdBackImage: "",
  avatarUrl: "",

  // BƯỚC 2: XE CỘ & BẰNG LÁI
  licensePlate: "",
  vehicleType: "Xe máy xăng",
  vehicleModel: "Honda Wave Alpha",
  drivingLicenseImage: "",

  // BƯỚC 3: KHU VỰC & NGÂN HÀNG
  operatingArea: "Quận Cầu Giấy (Dịch Vọng, Dịch Vọng Hậu, Yên Hòa, Trung Hòa, Mai Dịch, Nghĩa Tân, Nghĩa Đô)",
  bankName: "Vietcombank (VCB)",
  bankAccountNumber: "",
  agreeTerms: false
});

const isLoading = ref(false);
const errorMessage = ref("");
const showSuccessModal = ref(false);

// Eye toggle state for password fields
const showPassword = ref(false);
const showConfirmPassword = ref(false);

// Form Gửi Yêu Cầu Hỗ Trợ Trực Tuyến dành riêng cho Shipper (Gửi đến hh9393100@gmail.com)
const supportForm = reactive({
  fullName: "",
  email: "",
  phone: "",
  orderCode: "",
  topic: "Sự cố ứng dụng & GPS",
  message: ""
});
const isSupportLoading = ref(false);
const supportSuccessMsg = ref("");
const supportErrorMsg = ref("");

const submitSupportTicket = async () => {
  supportSuccessMsg.value = "";
  supportErrorMsg.value = "";

  if (!supportForm.fullName.trim() || !supportForm.email.trim() || !supportForm.message.trim()) {
    supportErrorMsg.value = "Vui lòng nhập đầy đủ Họ tên, Email và Nội dung cần hỗ trợ!";
    return;
  }

  isSupportLoading.value = true;
  try {
    const res = await fetch("http://localhost:5000/api/support/ticket", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        fullName: supportForm.fullName.trim(),
        email: supportForm.email.trim(),
        phone: supportForm.phone.trim(),
        orderCode: supportForm.orderCode.trim(),
        topic: supportForm.topic,
        message: supportForm.message.trim()
      })
    });
    const data = await res.json();
    if (data.success || res.ok) {
      supportSuccessMsg.value = `Yêu cầu hỗ trợ của tài xế đã được gửi thành công đến ban quản trị (hh9393100@gmail.com)! Mã phiếu: ${data.ticketCode || 'ZM-CSKH'}`;
      supportForm.fullName = "";
      supportForm.email = "";
      supportForm.phone = "";
      supportForm.orderCode = "";
      supportForm.message = "";
    } else {
      supportErrorMsg.value = data.message || "Không thể gửi yêu cầu hỗ trợ. Vui lòng thử lại!";
    }
  } catch (err: any) {
    supportErrorMsg.value = "Không thể kết nối đến máy chủ hỗ trợ. Vui lòng kiểm tra kết nối mạng!";
  } finally {
    isSupportLoading.value = false;
  }
};

// Công cụ tính thu nhập dự kiến (Interactive Calculator)
const dailyHours = ref(8);
const estimatedIncome = computed(() => {
  const baseDaily = dailyHours.value * 70000;
  const bonusDaily = dailyHours.value >= 8 ? 150000 : dailyHours.value >= 6 ? 80000 : 0;
  const totalDaily = baseDaily + bonusDaily;
  const monthly = totalDaily * 26;
  return monthly.toLocaleString('vi-VN') + ' đ';
});

// FAQ Accordion State
const openFaqIndex = ref<number | null>(0);
const toggleFaq = (index: number) => {
  openFaqIndex.value = openFaqIndex.value === index ? null : index;
};

const faqs = [
  {
    q: "Tôi cần những giấy tờ gì để đăng ký làm tài xế ZoneMart Driver?",
    a: "Bạn chỉ cần chuẩn bị 3 loại giấy tờ cơ bản: Căn cước công dân (CCCD 12 số), Bằng lái xe máy (GPLX) và Giấy đăng ký xe. Hồ sơ được thẩm định trực tuyến trong 24 giờ."
  },
  {
    q: "ZoneMart Driver có quy định thời gian chạy đơn cố định không?",
    a: "Không! Bạn hoàn toàn tự do 100% về thời gian. Bất cứ khi nào rảnh rỗi, bạn chỉ cần mở ứng dụng và bật chế độ Nhận đơn để bắt đầu tăng thu nhập."
  },
  {
    q: "Cước phí và tiền thưởng được thanh toán như thế nào?",
    a: "Cước phí và tiền thưởng nổ đơn được cộng trực tiếp vào Ví tài xế ngay sau khi hoàn thành mỗi đơn hàng. Bạn có thể rút tiền về tài khoản ngân hàng liên kết bất cứ lúc nào 24/7."
  },
  {
    q: "Bán kính giao hàng của ZoneMart Driver là bao xa?",
    a: "Tất cả đơn hàng nông sản và thực phẩm tươi sạch của ZoneMart đều nằm trong bán kính hỏa tốc tối đa 3km quanh khu vực bạn đăng ký, giúp bạn di chuyển ngắn, tiết kiệm xăng xe và giao hàng nhanh chóng."
  }
];

// Custom Dropdowns state
const isAreaDropdownOpen = ref(false);
const isVehicleTypeDropdownOpen = ref(false);
const isModelDropdownOpen = ref(false);
const isBankDropdownOpen = ref(false);

const vehicleTypes = ["Xe máy xăng", "Xe máy điện"];

const vehicleModelsMap: Record<string, string[]> = {
  "Xe máy xăng": [
    "Honda Wave Alpha",
    "Honda Vision",
    "Honda Air Blade",
    "Honda Winner X",
    "Yamaha Exciter 150/155",
    "Yamaha Sirius / Sirius FI",
    "Honda Lead / Future 125i"
  ],
  "Xe máy điện": [
    "VinFast Feliz S",
    "VinFast Klara S",
    "VinFast Evo200 / Evo200 Lite",
    "Dat Bike Quantum"
  ]
};

// TOÀN BỘ DANH SÁCH QUẬN, PHƯỜNG, XÃ, THỊ XÃ TẠI THÀNH PHỐ HÀ NỘI
const operatingAreas = [
  "Quận Cầu Giấy (Dịch Vọng, Dịch Vọng Hậu, Yên Hòa, Trung Hòa, Mai Dịch, Nghĩa Tân, Nghĩa Đô)",
  "Quận Đống Đa (Láng Hạ, Láng Upper/Lower, Ô Chợ Dừa, Kim Liên, Văn Miếu, Khâm Thiên, Thịnh Quang...)",
  "Quận Thanh Xuân (Thanh Xuân Bắc, Thanh Xuân Trung, Khương Trung, Nhân Chính, Phương Liệt...)",
  "Quận Nam Từ Liêm (Mỹ Đình 1, Mỹ Đình 2, Mễ Trì, Trung Văn, Tây Mỗ, Đại Mỗ, Phú Đô...)",
  "Quận Bắc Từ Liêm (Minh Khai, Phú Diễn, Phúc Diễn, Xuân Đỉnh, Cổ Nhuế, Đông Ngạc...)",
  "Quận Ba Đình (Đội Cấn, Kim Mã, Giảng Võ, Ngọc Khánh, Liễu Giai, Điện Biên, Trúc Bạch...)",
  "Quận Hoàn Kiếm (Hàng Bạc, Hàng Bông, Hàng Đào, Tràng Tiền, Lý Thái Tổ, Phan Chu Trinh...)",
  "Quận Hai Bà Trưng (Bách Khoa, Minh Khai, Trương Định, Bạch Mai, Vĩnh Tuy, Đồng Tâm...)",
  "Quận Tây Hồ (Thụy Khuê, Quảng An, Nhật Tân, Xuân La, Bưởi, Yên Phụ, Phú Thượng...)",
  "Quận Hoàng Mai (Hoàng Văn Thụ, Giáp Bát, Tân Mai, Định Công, Linh Đàm, Yên Sở...)",
  "Quận Long Biên (Gia Thụy, Ngọc Lâm, Bồ Đề, Sài Đồng, Thạch Bàn, Đức Giang, Việt Hưng...)",
  "Thị xã Sơn Tây (Lê Lợi, Quang Trung, Ngô Quyền, Trung Hưng, Viên Sơn, Sơn Lộc...)",
  "Huyện Gia Lâm (Trâu Quỳ, Yên Viên, Đa Tốn, Bát Tràng, Dương Xá, Cổ Bi...)",
  "Huyện Đông Anh (Đông Anh, Nguyên Khê, Tiên Dương, Vân Nội, Kim Nỗ, Hải Bối...)",
  "Huyện Thanh Trì (Văn Điển, Tân Triều, Thanh Liệt, Ngũ Hiệp, Ngọc Hồi...)",
  "Huyện Hoài Đức (Trạm Trôi, An Khánh, Đức Giang, Kim Chung, Vân Canh, Di Trạch...)",
  "Huyện Thạch Thất (Liên Quan, Bình Yên, Thạch Hòa, Tân Xã, Cần Kiệm...)",
  "Huyện Quốc Oai (Quốc Oai, Sài Sơn, Phượng Cách, Ngọc Liệp, Tuyết Nghĩa...)",
  "Huyện Chương Mỹ (Chúc Sơn, Xuân Mai, Phụng Châu, Tiên Phương...)",
  "Huyện Đan Phượng (Phùng, Tân Lập, Tân Hội, Song Phượng...)",
  "Huyện Sóc Sơn (Sóc Sơn, Phù Linh, Trung Giã, Mai Đình, Tiên Dược...)",
  "Tất cả các Quận, Phường & Xã tại TP. Hà Nội"
];

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

const toggleAreaDropdown = () => {
  isAreaDropdownOpen.value = !isAreaDropdownOpen.value;
  isVehicleTypeDropdownOpen.value = false;
  isModelDropdownOpen.value = false;
  isBankDropdownOpen.value = false;
};

const selectArea = (area: string) => {
  form.operatingArea = area;
  isAreaDropdownOpen.value = false;
};

const toggleVehicleTypeDropdown = () => {
  isVehicleTypeDropdownOpen.value = !isVehicleTypeDropdownOpen.value;
  isAreaDropdownOpen.value = false;
  isModelDropdownOpen.value = false;
  isBankDropdownOpen.value = false;
};

const selectVehicleType = (type: string) => {
  form.vehicleType = type;
  isVehicleTypeDropdownOpen.value = false;
  const models = vehicleModelsMap[type];
  if (models && models.length > 0) {
    form.vehicleModel = models[0];
  }
};

const toggleModelDropdown = () => {
  isModelDropdownOpen.value = !isModelDropdownOpen.value;
  isAreaDropdownOpen.value = false;
  isVehicleTypeDropdownOpen.value = false;
  isBankDropdownOpen.value = false;
};

const selectModel = (model: string) => {
  form.vehicleModel = model;
  isModelDropdownOpen.value = false;
};

const toggleBankDropdown = () => {
  isBankDropdownOpen.value = !isBankDropdownOpen.value;
  isAreaDropdownOpen.value = false;
  isVehicleTypeDropdownOpen.value = false;
  isModelDropdownOpen.value = false;
};

const selectBank = (bank: string) => {
  form.bankName = bank;
  isBankDropdownOpen.value = false;
};

const handleDocumentClick = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (!target.closest('.custom-select-container')) {
    isAreaDropdownOpen.value = false;
    isVehicleTypeDropdownOpen.value = false;
    isModelDropdownOpen.value = false;
    isBankDropdownOpen.value = false;
  }
};

onMounted(() => {
  document.addEventListener('click', handleDocumentClick);

  // Tự động lướt xuống Bảng đăng ký tài xế nếu điều hướng từ Đăng nhập hoặc URL Hash
  if (window.location.hash === "#shipper-register-card" || route.hash === "#shipper-register-card" || route.query.register === "true") {
    setTimeout(() => {
      scrollToRegisterForm();
    }, 200);
  }
});

onUnmounted(() => {
  document.removeEventListener('click', handleDocumentClick);
});

// Xử lý Chuyển bước
const goToNextStep = () => {
  errorMessage.value = "";

  if (currentStep.value === 1) {
    if (!form.fullName.trim() || form.fullName.length < 6 || form.fullName.length > 16) {
      errorMessage.value = "Họ tên thật tài xế phải từ 6 đến 16 ký tự!";
      return;
    }
    
    // RÀNG BUỘC GMAIL BẮT BUỘC KẾT THÚC BẰNG @gmail.com
    const cleanEmail = form.phoneNumber.trim().toLowerCase();
    if (!cleanEmail || !cleanEmail.endsWith('@gmail.com') || !/^[a-zA-Z0-9._%+-]+@gmail\.com$/.test(cleanEmail)) {
      errorMessage.value = "Địa chỉ Gmail đăng ký phải đúng định dạng và có đuôi @gmail.com (VD: name@gmail.com)!";
      return;
    }

    // RÀNG BUỘC MẬT KHẨU TỪ 6 ĐẾN 16 KÝ TỰ
    if (!form.password.trim() || form.password.length < 6 || form.password.length > 16) {
      errorMessage.value = "Mật khẩu tạo tài khoản tài xế phải có độ dài từ 6 đến 16 ký tự!";
      return;
    }
    if (form.password.trim() !== form.confirmPassword.trim()) {
      errorMessage.value = "Mật khẩu và Nhập lại mật khẩu không trùng khớp!";
      return;
    }
    if (!form.cccdNumber.trim() || !/^\d{12}$/.test(form.cccdNumber.trim())) {
      errorMessage.value = "Số Căn cước công dân phải đúng 12 chữ số!";
      return;
    }
    if (!form.avatarUrl || !form.cccdFrontImage || !form.cccdBackImage) {
      errorMessage.value = "Vui lòng tải đủ 3 hình ảnh: Chân dung, CCCD Mặt trước & Mặt sau!";
      return;
    }
    currentStep.value = 2;
  } else if (currentStep.value === 2) {
    if (!form.licensePlate.trim()) {
      errorMessage.value = "Vui lòng nhập Biển số xe!";
      return;
    }
    if (!form.drivingLicenseImage) {
      errorMessage.value = "Bắt buộc tải ảnh Bằng lái xe (GPLX)!";
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

// Xử lý Upload file ảnh
const handleFileUpload = (e: Event, field: 'cccdFrontImage' | 'cccdBackImage' | 'avatarUrl' | 'drivingLicenseImage') => {
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

// Gửi Đăng ký Shipper
const handleSubmitShipper = async () => {
  errorMessage.value = "";
  const accountNumberTrimmed = form.bankAccountNumber.trim();
  if (!accountNumberTrimmed || !/^\d{8,15}$/.test(accountNumberTrimmed)) {
    errorMessage.value = "Số tài khoản nhận cước phải từ 8 đến 15 chữ số!";
    return;
  }

  if (!form.agreeTerms) {
    errorMessage.value = "Vui lòng đồng ý tuân thủ luật giao thông & quy tắc ứng xử ZoneMart!";
    return;
  }

  isLoading.value = true;

  try {
    const res = await fetch("http://localhost:5000/api/auth/register-shipper", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        fullName: form.fullName.trim(),
        phoneNumber: form.phoneNumber.trim(),
        password: form.password.trim(),
        cccdNumber: form.cccdNumber.trim(),
        cccdFrontImage: form.cccdFrontImage,
        cccdBackImage: form.cccdBackImage,
        avatarUrl: form.avatarUrl,
        licensePlate: form.licensePlate.trim().toUpperCase(),
        vehicleType: form.vehicleType,
        vehicleModel: form.vehicleModel,
        drivingLicenseImage: form.drivingLicenseImage,
        operatingArea: form.operatingArea,
        bankName: form.bankName,
        bankAccountNumber: accountNumberTrimmed
      })
    }).catch(() => null);

    if (res && res.ok) {
      const data = await res.json().catch(() => null);
      const registeredUser = {
        phoneEmail: form.phoneNumber.trim(),
        fullName: form.fullName.trim(),
        avatarUrl: form.avatarUrl,
        role: "shipper",
        shipperDetails: {
          shipperCode: data?.shipperCode || "00001",
          fullName: form.fullName.trim(),
          phoneNumber: form.phoneNumber.trim(),
          licensePlate: form.licensePlate.trim().toUpperCase(),
          vehicleType: form.vehicleType,
          vehicleModel: form.vehicleModel,
          operatingArea: form.operatingArea,
          status: "Pending",
          avatarUrl: form.avatarUrl
        }
      };
      localStorage.setItem("currentUser", JSON.stringify(registeredUser));
      localStorage.setItem("isLoggedIn", "true");
    }

    showSuccessModal.value = true;
  } catch (err: any) {
    errorMessage.value = err.message || "Có lỗi xảy ra khi nộp hồ sơ.";
  } finally {
    isLoading.value = false;
  }
};


const closeSuccessModal = () => {
  showSuccessModal.value = false;
  router.push("/register-shipper");
  window.scrollTo({ top: 0, behavior: "smooth" });
};

const scrollToRegisterForm = () => {
  const el = document.getElementById("shipper-register-card");
  if (el) {
    el.scrollIntoView({ behavior: "smooth" });
  }
};
</script>

<template>
  <div class="shipper-portal-page">
    
    <!-- 1. HEADER CAO CẤP DÀNH RIÊNG CHO ZONEMART DRIVER -->
    <header class="driver-header-modern">
      <div class="header-inner">
        <!-- LOGO ZONEMART DRIVER MỚI (CHỮ DRIVER KHÔNG KHUNG, NẰM NGAY DƯỚI CHỮ ZONEMART) -->
        <router-link to="/" class="driver-brand-logo">
          <img src="/logo.png" alt="ZoneMart Logo" class="brand-img" />
          <div class="brand-text-stack">
            <div class="main-brand-title">
              Zone<span class="highlight-mart">Mart</span>
            </div>
            <div class="driver-sub-title">DRIVER</div>
          </div>
        </router-link>

        <!-- MENU ĐIỀU HƯỚNG -->
        <nav class="driver-nav-menu">
          <a href="#hero" class="nav-item">Trang Chủ Driver</a>
          <a href="#income-calculator" class="nav-item">Tính Thu Nhập</a>
          <a href="#benefits" class="nav-item">Quyền Lợi Vàng</a>
          <a href="#faq" class="nav-item">Hỏi Đáp FAQ</a>
          <button @click="scrollToRegisterForm" class="btn-nav-primary">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
              <circle cx="8.5" cy="7" r="4"></circle>
              <line x1="20" y1="8" x2="20" y2="14"></line>
              <line x1="23" y1="11" x2="17" y2="11"></line>
            </svg>
            Đăng Ký Tài Xế
          </button>
          <router-link to="/login?role=shipper" class="btn-nav-login">Đăng Nhập</router-link>
        </nav>
      </div>
    </header>

    <!-- 2. HERO BANNER VỚI HÌNH MINH HỌA XE SHIPPER KÍCH THƯỚC LỚN & ANIMATION -->
    <section id="hero" class="driver-hero-wrapper">
      <div class="hero-bg-glow"></div>
      <div class="hero-content-grid">
        
        <!-- CỘT NỘI DUNG BÊN TRÁI -->
        <div class="hero-left-col">
          <div class="hero-badge-pill">
            <span>ĐỘI NGŨ GIAO HÀNG HỎA TỐC BÁN KÍNH 3KM</span>
          </div>

          <h1 class="hero-main-title">
            Gia Nhập Đội Ngũ <span class="text-gradient">ZoneMart Driver</span> <br />
            Thu Nhập Đột Phá <span class="text-highlight">15 - 20 Triệu/Tháng</span>
          </h1>

          <p class="hero-sub-text">
            Chủ động thời gian 100%, chạy đơn giao nông sản & thực phẩm sạch khu vực gần nhà. Cước phí công khai minh bạch, nạp rút tiền 24/7 tức thì về thẻ ngân hàng.
          </p>

          <div class="hero-action-row">
            <button @click="scrollToRegisterForm" class="btn-hero-glow">
              <span>GIA NHẬP ĐỘI TÀI XẾ NGAY</span>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </button>
            
            <a href="#income-calculator" class="btn-hero-glass">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
                <line x1="8" y1="21" x2="16" y2="21"></line>
                <line x1="12" y1="17" x2="12" y2="21"></line>
              </svg>
              <span>Bảng Tính Thu Nhập</span>
            </a>
          </div>

          <!-- DẢI THỐNG KÊ NHANH -->
          <div class="hero-stats-card">
            <div class="stat-box">
              <span class="stat-val">15 - 20 Tr</span>
              <span class="stat-lbl">Thu nhập ước tính/tháng</span>
            </div>
            <div class="stat-divider"></div>
            <div class="stat-box">
              <span class="stat-val">&lt; 3 km</span>
              <span class="stat-lbl">Bán kính giao ngắn</span>
            </div>
            <div class="stat-divider"></div>
            <div class="stat-box">
              <span class="stat-val">+300.000đ</span>
              <span class="stat-lbl">Thưởng mốc nổ đơn/ngày</span>
            </div>
          </div>
        </div>

        <!-- CỘT HÌNH MINH HỌA BÊN PHẢI -->
        <div class="hero-right-col">
          <div class="hero-image-floating-container">
            <div class="image-glow-backdrop"></div>
            <img
              src="/images/anhxoanendkyshipper_clean.png"
              alt="ZoneMart Driver Scooter Illustration"
              class="hero-large-illustration"
            />
          </div>
        </div>

      </div>
    </section>

    <!-- 3. CÔNG CỤ TÍNH THU NHẬP DỰ KIẾN (INTERACTIVE INCOME CALCULATOR) -->
    <section id="income-calculator" class="calculator-section">
      <div class="calc-card-glass">
        <div class="calc-header text-center">
          <span class="section-tag-pill">CÔNG CỤ ƯỚC TÍNH THU NHẬP</span>
          <h2 class="section-heading">Bạn Muốn Nhận Bao Nhiêu Tiền Mỗi Tháng?</h2>
          <p class="section-subtext">Kéo chọn số giờ bạn muốn chạy đơn mỗi ngày để ước tính mức thu nhập thực nhận cùng ZoneMart Driver</p>
        </div>

        <div class="calc-body-grid">
          <div class="calc-slider-box">
            <div class="slider-label-row">
              <span>Thời gian chạy đơn dự kiến:</span>
              <span class="hours-val"><b>{{ dailyHours }}</b> Giờ / Ngày</span>
            </div>
            
            <input
              type="range"
              min="4"
              max="12"
              step="1"
              v-model.number="dailyHours"
              class="custom-range-slider"
            />
            
            <div class="range-marks">
              <span>4h (Bán thời gian)</span>
              <span>8h (Toàn thời gian)</span>
              <span>12h (Chuyên nghiệp)</span>
            </div>
          </div>

          <div class="calc-result-box">
            <div class="result-title">THU NHẬP DỰ KIẾN THỰC NHẬN:</div>
            <div class="result-amount">{{ estimatedIncome }}<span class="month-unit"> / Tháng</span></div>
            <p class="result-desc">
              *Bao gồm cước phí chuyến đi + Thưởng mốc đơn nổ ngày (Tính trung bình 26 ngày làm việc/tháng).
            </p>
            <button @click="scrollToRegisterForm" class="btn-calc-apply">ĐĂNG KÝ NHẬN MỨC THU NHẬP NÀY ➔</button>
          </div>
        </div>
      </div>
    </section>

    <!-- 4. 6 LỢI ÍCH VÀNG KHI LÀM TÀI XẾ ZONEMART DRIVER -->
    <section id="benefits" class="benefits-grid-section">
      <div class="section-title-block text-center">
        <span class="section-tag-pill">QUYỀN LỢI ĐỘC QUYỀN</span>
        <h2 class="section-heading">6 Lý Do Bạn Nên Chọn ZoneMart Driver</h2>
      </div>

      <div class="benefits-6-grid">
        
        <!-- CARD 1 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap cyan-theme">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <polygon points="1 6 1 22 8 18 16 22 23 18 23 2 16 6 8 2 1 6"></polygon>
              <line x1="8" y1="2" x2="8" y2="18"></line>
              <line x1="16" y1="6" x2="16" y2="22"></line>
            </svg>
          </div>
          <h3>Quãng Đường Ngắn &lt; 3km</h3>
          <p>Tất cả đơn hàng giao nông sản & thực phẩm đều nằm trong bán kính 3km quanh khu vực bạn chọn, giúp tiết kiệm xăng xe tối đa.</p>
        </div>

        <!-- CARD 2 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap green-theme">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="2" y="4" width="20" height="16" rx="2"></rect>
              <line x1="6" y1="12" x2="18" y2="12"></line>
              <line x1="12" y1="8" x2="12" y2="16"></line>
            </svg>
          </div>
          <h3>Chiết Khấu Thấp & Cước Cao</h3>
          <p>Tài xế giữ lại 85 - 90% cước phí chuyến đi. Không thu các khoản phí chiết khấu ẩn hay phí duy trì vô lý.</p>
        </div>

        <!-- CARD 3 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap orange-theme">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#D94E15" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="10"></circle>
              <polyline points="12 6 12 12 16 14"></polyline>
            </svg>
          </div>
          <h3>Thời Gian Tự Do 100%</h3>
          <p>Hoàn toàn chủ động lịch làm việc. Bạn chỉ cần bật app nhận đơn bất cứ khi nào sẵn sàng (Sáng, Trưa, Tối hoặc 24/7).</p>
        </div>

        <!-- CARD 4 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap purple-theme">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#8B5CF6" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 12V8H6a2 2 0 0 1-2-2c0-1.1.9-2 2-2h12v4"></path>
              <path d="M4 6v12c0 1.1.9 2 2 2h14v-4"></path>
              <path d="M18 12a2 2 0 0 0-2 2c0 1.1.9 2 2 2h4v-4h-4z"></path>
            </svg>
          </div>
          <h3>Thưởng Mốc Đơn Hàng Ngày</h3>
          <p>Hệ thống thưởng nổ đơn hàng ngày cực đỉnh lên đến 300.000đ khi hoàn thành các mốc đơn từ 15 đến 25 chuyến/ngày.</p>
        </div>

        <!-- CARD 5 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap blue-theme">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#2563EB" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="12" y1="1" x2="12" y2="23"></line>
              <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
            </svg>
          </div>
          <h3>Rút Tiền Cước Tức Thì 24/7</h3>
          <p>Nạp/Rút tiền ví tài xế tức thì về bất kỳ tài khoản ngân hàng liên kết nào (VCB, MB, TCB...) trong vòng 30 giây.</p>
        </div>

        <!-- CARD 6 -->
        <div class="benefit-card-modern">
          <div class="card-svg-icon-wrap red-theme">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#EF4444" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
            </svg>
          </div>
          <h3>Bảo Hiểm & Hỗ Trợ 24/7</h3>
          <p>Đội ngũ tổng đài hỗ trợ sự cố đường dài 24/7 cùng gói bảo hiểm tai nạn cá nhân bảo vệ tài xế an tâm chạy đơn.</p>
        </div>

      </div>
    </section>

    <!-- 5. QUY TRÌNH 3 BƯỚC THAM GIA ĐƠN GIẢN -->
    <section class="steps-flow-section">
      <div class="steps-title-centered-block text-center">
        <h2 class="section-heading-centered-full">Quy Trình 3 Bước Để Trở Thành Tài Xế</h2>
      </div>

      <div class="steps-flow-container">
        
        <div class="step-card-box">
          <div class="step-badge-num">1</div>
          <div class="step-svg-wrap">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
              <polyline points="14 2 14 8 20 8"></polyline>
              <line x1="16" y1="13" x2="8" y2="13"></line>
              <line x1="16" y1="17" x2="8" y2="17"></line>
            </svg>
          </div>
          <h4>Điền Thông Tin Hồ Sơ</h4>
          <p>Cung cấp họ tên, SĐT và tải lên ảnh chân dung, CCCD cùng Bằng lái xe (GPLX).</p>
        </div>

        <div class="flow-connector-arrow">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.5">
            <line x1="5" y1="12" x2="19" y2="12"></line>
            <polyline points="12 5 19 12 12 19"></polyline>
          </svg>
        </div>

        <div class="step-card-box">
          <div class="step-badge-num">2</div>
          <div class="step-svg-wrap">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
          </div>
          <h4>Admin Thẩm Định 24h</h4>
          <p>Ban quản lý tài xế ZoneMart kiểm tra hồ sơ và gửi email thông báo phê duyệt.</p>
        </div>

        <div class="flow-connector-arrow">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.5">
            <line x1="5" y1="12" x2="19" y2="12"></line>
            <polyline points="12 5 19 12 12 19"></polyline>
          </svg>
        </div>

        <div class="step-card-box">
          <div class="step-badge-num">3</div>
          <div class="step-svg-wrap">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
              <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
            </svg>
          </div>
          <h4>Bật App & Chạy Đơn</h4>
          <p>Đăng nhập ứng dụng ZoneMart Driver, bật vị trí GPS và nhận đơn hàng đầu tiên!</p>
        </div>

      </div>
    </section>

    <!-- 6. FORM ĐĂNG KÝ SHIPPER CAO CẤP DẠNG FLOATING CARD (NEUMORPHISM/APPLE STYLE) -->
    <section id="shipper-register-card" class="form-section-floating">
      <div class="modern-form-card">
        
        <div class="form-title-header text-center">
          <h2 class="form-card-title-centered">BẢNG ĐĂNG KÝ TÀI XẾ SHIPPER</h2>
          <p class="form-card-sub-centered">Điền thông tin bên dưới để Ban quản lý tài xế thẩm định & cấp tài khoản trong 24h</p>
        </div>

        <!-- STEPPER BAR NANG (PILLS) -->
        <div class="pills-stepper-bar">
          <div class="pill-step" :class="{ active: currentStep >= 1 }">
            <span class="pill-num">1</span>
            <span class="pill-txt">Thông tin cá nhân</span>
          </div>
          <div class="pill-line" :class="{ active: currentStep >= 2 }"></div>
          <div class="pill-step" :class="{ active: currentStep >= 2 }">
            <span class="pill-num">2</span>
            <span class="pill-txt">Phương tiện & GPLX</span>
          </div>
          <div class="pill-line" :class="{ active: currentStep >= 3 }"></div>
          <div class="pill-step" :class="{ active: currentStep >= 3 }">
            <span class="pill-num">3</span>
            <span class="pill-txt">Khu vực & Ngân hàng</span>
          </div>
        </div>

        <!-- THÔNG BÁO LỖI -->
        <div v-if="errorMessage" class="msg-alert-banner">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#EF4444" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          <span>{{ errorMessage }}</span>
        </div>

        <!-- BƯỚC 1: THÔNG TIN CÁ NHÂN -->
        <form v-if="currentStep === 1" @submit.prevent="goToNextStep" class="form-fields-grid">
          
          <div class="input-field-group">
            <label class="input-label-stylish">
              Họ tên thật tài xế <span class="req">*</span>
              <span class="hint-tag">(6 - 16 ký tự)</span>
            </label>
            <div class="input-with-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              <input v-model="form.fullName" type="text" class="modern-input" placeholder="VD: Nguyễn Văn A" maxlength="16" required />
            </div>
          </div>

          <div class="input-field-group">
            <label class="input-label-stylish">
              Địa chỉ Gmail đăng ký tài xế <span class="req">*</span>
              <span class="hint-tag">(Bắt buộc đuôi @gmail.com)</span>
            </label>
            <div class="input-with-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2">
                <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
                <polyline points="22,6 12,13 2,6"></polyline>
              </svg>
              <input v-model="form.phoneNumber" type="email" class="modern-input" placeholder="VD: nguyenvanA@gmail.com" required />
            </div>
          </div>

          <div class="input-field-group">
            <label class="input-label-stylish">
              Mật khẩu tạo tài khoản <span class="req">*</span>
              <span class="hint-tag">(Từ 6 - 16 ký tự)</span>
            </label>
            <div class="input-with-icon input-with-eye-toggle">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2" class="input-left-icon">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
              </svg>
              <input 
                v-model="form.password" 
                :type="showPassword ? 'text' : 'password'" 
                class="modern-input" 
                placeholder="Nhập mật khẩu (6 - 16 ký tự)" 
                maxlength="16"
                required 
              />
              <button type="button" class="eye-toggle-btn" @click="showPassword = !showPassword" :title="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'">
                <svg v-if="!showPassword" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
                <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                </svg>
              </button>
            </div>
          </div>

          <div class="input-field-group">
            <label class="input-label-stylish">
              Nhập lại mật khẩu <span class="req">*</span>
              <span class="hint-tag">(Xác nhận trùng khớp)</span>
            </label>
            <div class="input-with-icon input-with-eye-toggle">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2" class="input-left-icon">
                <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                <polyline points="9 12 11 14 15 10"></polyline>
              </svg>
              <input 
                v-model="form.confirmPassword" 
                :type="showConfirmPassword ? 'text' : 'password'" 
                class="modern-input" 
                placeholder="Nhập lại mật khẩu xác nhận" 
                maxlength="16"
                required 
              />
              <button type="button" class="eye-toggle-btn" @click="showConfirmPassword = !showConfirmPassword" :title="showConfirmPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'">
                <svg v-if="!showConfirmPassword" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
                <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                </svg>
              </button>
            </div>
          </div>

          <div class="input-field-group full-row">
            <label class="input-label-stylish">
              Số Căn cước công dân (CCCD) <span class="req">*</span>
              <span class="hint-tag">(Đúng 12 số)</span>
            </label>
            <div class="input-with-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2">
                <rect x="3" y="4" width="18" height="16" rx="2"></rect>
                <line x1="7" y1="8" x2="11" y2="8"></line>
                <line x1="7" y1="12" x2="17" y2="12"></line>
              </svg>
              <input v-model="form.cccdNumber" type="text" class="modern-input" placeholder="VD: 001203001234" maxlength="12" required />
            </div>
          </div>

          <!-- UPLOAD IMAGE DRAG & DROP CARDS -->
          <div class="upload-cards-grid full-row">
            
            <div class="upload-drop-card">
              <label class="upload-card-label">
                <div class="upload-icon-circle">
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                    <polyline points="17 8 12 3 7 8"></polyline>
                    <line x1="12" y1="3" x2="12" y2="15"></line>
                  </svg>
                </div>
                <span class="upload-txt-title">Ảnh Chân Dung *</span>
                <span class="upload-txt-sub">Nhấp chọn ảnh rõ mặt</span>
                <input type="file" accept="image/*" class="hidden-file-input" @change="e => handleFileUpload(e, 'avatarUrl')" />
              </label>
              <div v-if="form.avatarUrl" class="upload-preview-box">
                <img :src="form.avatarUrl" alt="Avatar Preview" />
                <span class="preview-badge">✓ Đã tải lên</span>
              </div>
            </div>

            <div class="upload-drop-card">
              <label class="upload-card-label">
                <div class="upload-icon-circle">
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                    <rect x="3" y="4" width="18" height="16" rx="2"></rect>
                    <line x1="7" y1="8" x2="11" y2="8"></line>
                  </svg>
                </div>
                <span class="upload-txt-title">CCCD Mặt Trước *</span>
                <span class="upload-txt-sub">Nhấp để tải ảnh mặt trước</span>
                <input type="file" accept="image/*" class="hidden-file-input" @change="e => handleFileUpload(e, 'cccdFrontImage')" />
              </label>
              <div v-if="form.cccdFrontImage" class="upload-preview-box">
                <img :src="form.cccdFrontImage" alt="CCCD Front Preview" />
                <span class="preview-badge">✓ Đã tải lên</span>
              </div>
            </div>

            <div class="upload-drop-card">
              <label class="upload-card-label">
                <div class="upload-icon-circle">
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                    <rect x="3" y="4" width="18" height="16" rx="2"></rect>
                    <line x1="7" y1="12" x2="17" y2="12"></line>
                  </svg>
                </div>
                <span class="upload-txt-title">CCCD Mặt Sau *</span>
                <span class="upload-txt-sub">Nhấp để tải ảnh mặt sau</span>
                <input type="file" accept="image/*" class="hidden-file-input" @change="e => handleFileUpload(e, 'cccdBackImage')" />
              </label>
              <div v-if="form.cccdBackImage" class="upload-preview-box">
                <img :src="form.cccdBackImage" alt="CCCD Back Preview" />
                <span class="preview-badge">✓ Đã tải lên</span>
              </div>
            </div>

          </div>

          <div class="full-row form-actions-row form-actions-centered">
            <button type="submit" class="btn-step-next-glow">
              <span>TIẾP THEO BƯỚC 2 (XE & GPLX)</span>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </button>
          </div>

        </form>

        <!-- BƯỚC 2: PHƯƠNG TIỆN & GPLX -->
        <form v-if="currentStep === 2" @submit.prevent="goToNextStep" class="form-fields-grid">
          
          <div class="input-field-group full-row">
            <label class="input-label-stylish">
              Biển số xe máy <span class="req">*</span>
              <span class="hint-tag">(Tự động VIẾT HOA chữ)</span>
            </label>
            <div class="input-with-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2">
                <rect x="1" y="3" width="15" height="13"></rect>
                <polygon points="16 8 20 8 23 11 23 16 16 16 8"></polygon>
                <circle cx="5.5" cy="18.5" r="2.5"></circle>
                <circle cx="18.5" cy="18.5" r="2.5"></circle>
              </svg>
              <input v-model="form.licensePlate" type="text" class="modern-input uppercase-input" placeholder="VD: 29X1-838.86" required />
            </div>
          </div>

          <div class="input-field-group custom-select-container" :class="{ 'dropdown-open': isVehicleTypeDropdownOpen }">
            <label class="input-label-stylish">Loại xe đăng ký <span class="req">*</span></label>
            <div class="custom-select-trigger-stylish" @click.stop="toggleVehicleTypeDropdown">
              <span>{{ form.vehicleType }}</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6B7280" stroke-width="2">
                <polyline points="6 9 12 15 18 9"></polyline>
              </svg>
            </div>
            <div v-if="isVehicleTypeDropdownOpen" class="custom-dropdown-menu-stylish">
              <div v-for="t in vehicleTypes" :key="t" class="dropdown-item-stylish" @click.stop="selectVehicleType(t)">
                {{ t }}
              </div>
            </div>
          </div>

          <div class="input-field-group custom-select-container" :class="{ 'dropdown-open': isModelDropdownOpen }">
            <label class="input-label-stylish">Dòng xe / Mẫu xe <span class="req">*</span></label>
            <div class="custom-select-trigger-stylish" @click.stop="toggleModelDropdown">
              <span>{{ form.vehicleModel }}</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6B7280" stroke-width="2">
                <polyline points="6 9 12 15 18 9"></polyline>
              </svg>
            </div>
            <div v-if="isModelDropdownOpen" class="custom-dropdown-menu-stylish">
              <div v-for="m in vehicleModelsMap[form.vehicleType]" :key="m" class="dropdown-item-stylish" @click.stop="selectModel(m)">
                {{ m }}
              </div>
            </div>
          </div>

          <div class="upload-drop-card full-row">
            <label class="upload-card-label">
              <div class="upload-icon-circle">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                  <circle cx="10" cy="13" r="2"></circle>
                </svg>
              </div>
              <span class="upload-txt-title">Ảnh Bằng Lái Xe (GPLX) *</span>
              <span class="upload-txt-sub">Nhấp để tải tệp ảnh bằng lái</span>
              <input type="file" accept="image/*" class="hidden-file-input" @change="e => handleFileUpload(e, 'drivingLicenseImage')" />
            </label>
            <div v-if="form.drivingLicenseImage" class="upload-preview-box">
              <img :src="form.drivingLicenseImage" alt="GPLX Preview" />
              <span class="preview-badge">✓ Đã tải lên</span>
            </div>
          </div>

          <div class="full-row step-btn-row-stylish">
            <button type="button" class="btn-prev-stylish" @click="goToPrevStep">← Quay lại Bước 1</button>
            <button type="submit" class="btn-step-next-glow">
              <span>TIẾP THEO BƯỚC 3</span>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </button>
          </div>

        </form>

        <!-- BƯỚC 3: KHU VỰC & NGÂN HÀNG -->
        <form v-if="currentStep === 3" @submit.prevent="handleSubmitShipper" class="form-fields-grid">
          
          <div class="input-field-group custom-select-container full-row" :class="{ 'dropdown-open': isAreaDropdownOpen }">
            <label class="input-label-stylish">Khu vực đăng ký chạy chính <span class="req">*</span></label>
            <div class="custom-select-trigger-stylish" @click.stop="toggleAreaDropdown">
              <span>{{ form.operatingArea }}</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6B7280" stroke-width="2">
                <polyline points="6 9 12 15 18 9"></polyline>
              </svg>
            </div>
            <div v-if="isAreaDropdownOpen" class="custom-dropdown-menu-stylish">
              <div v-for="a in operatingAreas" :key="a" class="dropdown-item-stylish" @click.stop="selectArea(a)">
                {{ a }}
              </div>
            </div>
          </div>

          <div class="input-field-group custom-select-container" :class="{ 'dropdown-open': isBankDropdownOpen }">
            <label class="input-label-stylish">Ngân hàng nhận cước <span class="req">*</span></label>
            <div class="custom-select-trigger-stylish" @click.stop="toggleBankDropdown">
              <span>{{ form.bankName }}</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6B7280" stroke-width="2">
                <polyline points="6 9 12 15 18 9"></polyline>
              </svg>
            </div>
            <div v-if="isBankDropdownOpen" class="custom-dropdown-menu-stylish">
              <div v-for="b in banksList" :key="b" class="dropdown-item-stylish" @click.stop="selectBank(b)">
                {{ b }}
              </div>
            </div>
          </div>

          <div class="input-field-group">
            <label class="input-label-stylish">
              Số tài khoản ngân hàng <span class="req">*</span>
              <span class="hint-tag">(8 - 15 số)</span>
            </label>
            <div class="input-with-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#9CA3AF" stroke-width="2">
                <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
                <line x1="1" y1="10" x2="23" y2="10"></line>
              </svg>
              <input v-model="form.bankAccountNumber" type="text" class="modern-input" placeholder="VD: 1012345678" required />
            </div>
          </div>

          <div class="full-row terms-checkbox-stylish">
            <label class="stylish-checkbox-label">
              <input type="checkbox" v-model="form.agreeTerms" class="custom-chk-box" />
              <span>Tôi cam kết tuân thủ nghiêm chỉnh <b>Luật Giao thông đường bộ</b> & <b>Quy tắc ứng xử ZoneMart Driver</b>.</span>
            </label>
          </div>

          <div class="full-row step-btn-row-stylish">
            <button type="button" class="btn-prev-stylish" @click="goToPrevStep">← Quay lại Bước 2</button>
            <button 
              type="submit" 
              class="btn-submit-final-glow" 
              :class="{ 'btn-disabled': !form.agreeTerms || isLoading }"
              :disabled="isLoading || !form.agreeTerms"
            >
              <span v-if="!isLoading">NỘP HỒ SƠ ĐĂNG KÝ TÀI XẾ</span>
              <span v-else>ĐANG NỘP HỒ SƠ...</span>
            </button>
          </div>

        </form>

      </div>
    </section>

    <!-- 7. GÓC CÂU HỎI THƯỜNG GẶP & GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN -->
    <section id="faq" class="faq-support-split-section">
      <div class="section-title-block text-center">
        <span class="section-tag-pill">HỎI ĐÁP & HỖ TRỢ TRỰC TUYẾN</span>
        <h2 class="section-heading">Trung Tâm Hỗ Trợ Tài Xế ZoneMart</h2>
      </div>

      <div class="faq-support-grid">
        
        <!-- BÊN TRÁI: GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN DÀNH RIÊNG CHO SHIPPER -->
        <div class="shipper-support-form-card">
          <div class="support-card-header">
            <h3>Gửi Yêu Cầu Hỗ Trợ Trực Tuyến</h3>
            <p>Điền thông tin sự cố để ban quản lý hỗ trợ xử lý trực tiếp về Gmail (hh9393100@gmail.com)</p>

            <!-- DẢI THÔNG TIN CSKH NHANH ĐỂ LẤP ĐẦY KHOẢNG TRỐNG -->
            <div class="support-cskh-quick-info">
              <div class="info-pill-item">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.2">
                  <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path>
                </svg>
                <span><b>Hotline:</b> 1900 6868</span>
              </div>
              <div class="info-pill-item">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.2">
                  <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
                  <polyline points="22,6 12,13 2,6"></polyline>
                </svg>
                <span><b>Email tiếp nhận:</b> hh9393100@gmail.com</span>
              </div>
              <div class="info-pill-item">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2.2">
                  <circle cx="12" cy="12" r="10"></circle>
                  <polyline points="12 6 12 12 16 14"></polyline>
                </svg>
                <span><b>Xử lý:</b> &lt; 15 phút</span>
              </div>
            </div>
          </div>

          <div v-if="supportSuccessMsg" class="msg-alert-banner success-alert">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
            <span>{{ supportSuccessMsg }}</span>
          </div>

          <div v-if="supportErrorMsg" class="msg-alert-banner">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#EF4444" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{{ supportErrorMsg }}</span>
          </div>

          <form @submit.prevent="submitSupportTicket" class="compact-support-form">
            <div class="form-row-2col">
              <div class="input-field-group">
                <label class="input-label-stylish">Họ và tên *</label>
                <input v-model="supportForm.fullName" type="text" class="modern-input-small" placeholder="VD: Nguyễn Văn A" required />
              </div>
              <div class="input-field-group">
                <label class="input-label-stylish">Địa chỉ Email *</label>
                <input v-model="supportForm.email" type="email" class="modern-input-small" placeholder="email@example.com" required />
              </div>
            </div>

            <div class="form-row-2col">
              <div class="input-field-group">
                <label class="input-label-stylish">Số điện thoại *</label>
                <input v-model="supportForm.phone" type="text" class="modern-input-small" placeholder="0912345678" required />
              </div>
              <div class="input-field-group">
                <label class="input-label-stylish">Mã tài xế / Mã đơn (nếu có)</label>
                <input v-model="supportForm.orderCode" type="text" class="modern-input-small" placeholder="VD: ZM-8899" />
              </div>
            </div>

            <div class="input-field-group">
              <label class="input-label-stylish">Vấn đề cần hỗ trợ</label>
              <div class="topic-pills-row">
                <button
                  type="button"
                  v-for="t in ['Sự cố ứng dụng & GPS', 'Đổi xe & GPLX', 'Nạp/Rút cước phí', 'Thu nhập & Tiền thưởng', 'Đóng góp ý kiến']"
                  :key="t"
                  class="topic-btn-pill"
                  :class="{ active: supportForm.topic === t }"
                  @click="supportForm.topic = t"
                >
                  {{ t }}
                </button>
              </div>
            </div>

            <div class="input-field-group">
              <label class="input-label-stylish">Nội dung chi tiết *</label>
              <textarea v-model="supportForm.message" class="modern-textarea-small" rows="3" placeholder="Mô tả cụ thể sự cố hoặc thắc mắc của bạn..." required></textarea>
            </div>

            <button type="submit" class="btn-send-support" :disabled="isSupportLoading">
              <span v-if="!isSupportLoading">GỬI YÊU CẦU HỖ TRỢ TRỰC TUYẾN ➔</span>
              <span v-else>ĐANG GỬI...</span>
            </button>
          </form>
        </div>

        <!-- BÊN PHẢI: THẮC MẮC CỦA TÀI XẾ MỚI (CÂU HỎI THƯỜNG GẶP GỌN GÀNG BÊN PHẢI) -->
        <div class="shipper-faq-right-card">
          <div class="faq-card-header">
            <h3>Thắc Mắc Của Tài Xế Mới</h3>
            <p>Các câu hỏi thường gặp khi gia nhập ZoneMart Driver</p>
          </div>

          <div class="faq-list-container-compact">
            <div
              v-for="(item, idx) in faqs"
              :key="idx"
              class="faq-item-card"
              :class="{ open: openFaqIndex === idx }"
              @click="toggleFaq(idx)"
            >
              <div class="faq-question-header">
                <h3>{{ item.q }}</h3>
                <div class="faq-toggle-icon">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.5">
                    <polyline points="6 9 12 15 18 9"></polyline>
                  </svg>
                </div>
              </div>
              <div v-if="openFaqIndex === idx" class="faq-answer-body">
                <p>{{ item.a }}</p>
              </div>
            </div>
          </div>

          <!-- BỔ SUNG NỘI DUNG LẤP ĐẦY KHOẢNG TRỐNG BÊN PHẢI: QUY TẮC VÀNG TÀI XẾ ZONEMART -->
          <div class="faq-bottom-guideline-box">
            <div class="guideline-header">
              <div class="guideline-icon">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2.2">
                  <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                  <polyline points="9 12 11 14 15 10"></polyline>
                </svg>
              </div>
              <h4>Quy Tắc Vàng Dành Cho Tài Xế ZoneMart</h4>
            </div>

            <ul class="guideline-list">
              <li>
                <span class="g-bullet">1</span>
                <span><b>Đồng Phục & Nón Bảo Hiểm:</b> Nhận áo, nón bảo hiểm ZoneMart Driver miễn phí ngay khi được duyệt hồ sơ.</span>
              </li>
              <li>
                <span class="g-bullet">2</span>
                <span><b>Thái Độ Giao Hàng 5 Star:</b> Niềm nở, giao tận tay hàng tươi sạch cho khách hàng.</span>
              </li>
              <li>
                <span class="g-bullet">3</span>
                <span><b>Cước Phí Minh Bạch:</b> Giữ lại 85-90% cước phí chuyến đi, nạp/rút tiền ví 24/7 trong 30 giây.</span>
              </li>
            </ul>

            <div class="guideline-footer-hotline">
              <span>Hỗ trợ khẩn cấp đường xa 24/7: <b>1900 6868</b></span>
            </div>
          </div>

        </div>

      </div>
    </section>

    <!-- 8. FOOTER CHUẨN ĐẦY ĐỦ DÀNH RIÊNG CHO ZONEMART DRIVER -->
    <footer class="driver-footer-rich">
      <div class="footer-top-grid">
        
        <!-- CỘT 1: BRAND & TỔNG ĐÀI HỖ TRỢ -->
        <div class="footer-col brand-col">
          <div class="footer-brand-logo">
            <img src="/logo.png" alt="ZoneMart Logo" class="f-logo-img" />
            <div class="f-brand-text">
              <span class="f-brand-name">Zone<span class="highlight">Mart</span></span>
              <span class="f-driver-tag">DRIVER PORTAL</span>
            </div>
          </div>
          <p class="f-brand-desc">
            Cổng thông tin & hỗ trợ tài xế giao hàng hỏa tốc ZoneMart Driver. Kết nối hàng triệu đơn hàng nông sản, đồ sạch tận tay người tiêu dùng.
          </p>

          <ul class="footer-contact-list">
            <li>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
                <circle cx="12" cy="10" r="3"></circle>
              </svg>
              <span><b>Trụ sở:</b> Tòa nhà ZoneMart, Khu Công Nghệ Cao, TP. Hà Nội</span>
            </li>
            <li>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path>
              </svg>
              <span><b>Tổng đài hỗ trợ 24/7:</b> 1900 6868 (8:00 - 21:00)</span>
            </li>
            <li>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
                <polyline points="22,6 12,13 2,6"></polyline>
              </svg>
              <span><b>Email hỗ trợ tài xế:</b> driver@zonemart.vn</span>
            </li>
            <li>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#0284C7" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <polyline points="12 6 12 12 16 14"></polyline>
              </svg>
              <span><b>Thời gian chạy đơn:</b> 24/7 Linh hoạt thời gian</span>
            </li>
          </ul>
        </div>

        <!-- CỘT 2: VỀ ZONEMART DRIVER -->
        <div class="footer-col">
          <h4 class="f-col-heading">VỀ ZONEMART DRIVER</h4>
          <ul class="f-links-list">
            <li><a href="#hero">Giới thiệu ZoneMart Driver</a></li>
            <li><a href="#benefits">Quy chế hoạt động ứng dụng tài xế</a></li>
            <li><a href="#benefits">Tiêu chuẩn giao hàng hỏa tốc 3km</a></li>
            <li><a href="#income-calculator">Bảng cước phí & thưởng mốc đơn</a></li>
            <li><a href="#faq">Tin tức & Mẹo tài xế an toàn</a></li>
            <li><a href="#shipper-register-card">Đăng ký gia nhập đội ngũ tài xế</a></li>
          </ul>
        </div>

        <!-- CỘT 3: HỖ TRỢ TÀI XẾ 24/7 -->
        <div class="footer-col">
          <h4 class="f-col-heading">HỖ TRỢ TÀI XẾ 24/7</h4>
          <ul class="f-links-list">
            <li><a href="#faq">Trung tâm hỗ trợ tài xế 24/7</a></li>
            <li><a href="#faq">Hướng dẫn nhận đơn & sử dụng App Driver</a></li>
            <li><a href="#benefits">Chính sách cước phí & hoa hồng 0%</a></li>
            <li><a href="#benefits">Quy trình rút cước 30s về ngân hàng</a></li>
            <li><a href="#benefits">Giải quyết sự cố giao hàng đường dài</a></li>
            <li><a href="#benefits">Chính sách bảo hiểm chuyến đi tài xế</a></li>
          </ul>
        </div>

        <!-- CỘT 4: HỢP TÁC & PHÁT TRIỂN -->
        <div class="footer-col">
          <h4 class="f-col-heading">HỢP TÁC & PHÁT TRIỂN</h4>
          <ul class="f-links-list">
            <li><router-link to="/register-shipper">Cổng Đăng Ký Tài Xế Shipper</router-link></li>
            <li><router-link to="/register-seller">Cổng Đăng Ký Gian Hàng Seller</router-link></li>
            <li><router-link to="/">Trang Chủ Sàn ZoneMart</router-link></li>
            <li><router-link to="/login?role=shipper">Đăng Nhập Tài Khoản Hệ Thống</router-link></li>
            <li><router-link to="/admin">Cổng Quản Trị Viên Admin</router-link></li>
          </ul>
        </div>

        <!-- CỘT 5: PHƯƠNG THỨC RÚT CƯỚC & KẾT NỐI -->
        <div class="footer-col">
          <h4 class="f-col-heading">RÚT CƯỚC & THANH TOÁN</h4>
          <div class="payment-tags-grid">
            <span class="pay-tag">Thẻ ATM Ngân hàng</span>
            <span class="pay-tag highlight">Ví ZoneDriver 24/7</span>
            <span class="pay-tag">VNPAY QR</span>
            <span class="pay-tag">Ví MoMo</span>
          </div>

          <h4 class="f-col-heading mt-4">KẾT NỐI VỚI ZONEMART</h4>
          <div class="social-icons-row">
            <a href="#" class="social-btn" title="Facebook">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
              </svg>
            </a>

            <!-- Zalo SVG -->
            <a href="#" class="social-btn zalo-btn" title="Zalo">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.48 2 12c0 2.17.69 4.19 1.87 5.84L2 22l4.34-1.78C7.93 21.28 9.9 22 12 22c5.52 0 10-4.48 10-10S17.52 2 12 2zm1 14h-2v-2h2v2zm0-4h-2V7h2v5z"/>
              </svg>
            </a>

            <!-- YouTube -->
            <a href="#" class="social-btn" title="YouTube">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
                <path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/>
              </svg>
            </a>

            <!-- TikTok -->
            <a href="#" class="social-btn" title="TikTok">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12.525.02c1.31-.02 2.61-.01 3.91-.02.08 1.53.63 3.09 1.75 4.17 1.12 1.11 2.7 1.62 4.24 1.79v4.03c-1.44-.05-2.89-.35-4.2-.97-.57-.26-1.1-.59-1.62-1-.01 2.92.01 5.84-.02 8.75-.08 1.4-.54 2.79-1.35 3.94-1.31 1.92-3.58 3.17-5.91 3.21-1.43.08-2.86-.31-4.08-1.03-2.02-1.19-3.44-3.37-3.65-5.71-.28-3.08 1.43-6.08 4.2-7.39.92-.44 1.93-.68 2.95-.73v4.07c-.77.06-1.54.3-2.2.71-1.07.67-1.74 1.88-1.71 3.14.03 1.54 1.05 2.9 2.53 3.26.96.24 1.98.11 2.85-.36.95-.51 1.58-1.46 1.62-2.54.04-3.31.02-6.62.03-9.93z"/>
              </svg>
            </a>
          </div>
        </div>

      </div>

      <div class="footer-bottom-line text-center">
        <p>© 2026 ZoneMart E-Commerce Platform. Cổng thông tin & Đăng ký Tài xế ZoneMart Driver chính thức. Giao hàng hỏa tốc 3km.</p>
      </div>
    </footer>

    <!-- 9. MODAL POPUP THÀNH CÔNG -->
    <Transition name="fade-modal">
      <div v-if="showSuccessModal" class="modal-backdrop-overlay" @click.self="closeSuccessModal">
        <div class="modal-success-card text-center">
          <div class="modal-icon-badge-glow">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#10B981" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
          </div>
          <h3 class="modal-success-title">Đăng Ký Tài Xế Shipper Thành Công!</h3>
          <p class="modal-success-desc">
            Cảm ơn bạn đã nộp hồ sơ gia nhập đội ngũ tài xế <b>ZoneMart Driver</b>. Hồ sơ của bạn đã được ghi nhận và thư cảm ơn xác nhận đã được gửi về Gmail <b>dobinh225599@gmail.com</b>. Đội ngũ thẩm định sẽ kiểm tra và kích hoạt tài khoản trong 24h.
          </p>
          <div class="modal-btn-row-centered">
            <button @click="closeSuccessModal" class="btn-modal-action-primary">TRỞ VỀ TRANG CHỦ DRIVER</button>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<style scoped>
/* GENERAL STYLES & VARIABLES */
.shipper-portal-page {
  font-family: 'Inter', system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
  color: #1F2937;
  background: #F8FAFC;
  min-height: 100vh;
}

/* HEADER MODERN WITH LOGO DRIVER WITHOUT BOX BACKGROUND */
.driver-header-modern {
  position: sticky;
  top: 0;
  z-index: 100;
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(12px);
  border-bottom: 1px solid #E2E8F0;
  box-shadow: 0 4px 20px rgba(2, 132, 199, 0.04);
}

.header-inner {
  max-width: 1240px;
  margin: 0 auto;
  padding: 12px 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

/* BRAND LOGO DESIGN: DRIVER IS CYAN NEON TEXT BELOW ZONEMART (NO BOX) */
.driver-brand-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
}

.brand-img {
  height: 44px;
  width: auto;
  object-fit: contain;
}

.brand-text-stack {
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.main-brand-title {
  font-size: 23px;
  font-weight: 900;
  color: #1F2937;
  line-height: 1.1;
  letter-spacing: -0.4px;
}

.main-brand-title .highlight-mart {
  color: #D94E15;
}

.driver-sub-title {
  font-size: 11.5px;
  font-weight: 900;
  color: #0284C7; /* CYAN NEON COLOR */
  letter-spacing: 2px;
  margin-top: 1px;
}

.driver-nav-menu {
  display: flex;
  align-items: center;
  gap: 20px;
}

.nav-item {
  color: #4B5563;
  text-decoration: none;
  font-weight: 600;
  font-size: 14.5px;
  transition: all 0.2s;
}

.nav-item:hover {
  color: #0284C7;
}

.btn-nav-primary {
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #ffffff;
  border: none;
  padding: 10px 20px;
  border-radius: 12px;
  font-weight: 800;
  font-size: 13.5px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 14px rgba(2, 132, 199, 0.25);
  transition: all 0.2s;
}

.btn-nav-primary:hover {
  transform: translateY(-1.5px);
  box-shadow: 0 6px 18px rgba(2, 132, 199, 0.35);
}

.btn-nav-login {
  color: #0284C7;
  border: 1.5px solid #BAE6FD;
  background: #F0F9FF;
  padding: 9px 18px;
  border-radius: 12px;
  font-weight: 700;
  font-size: 13.5px;
  text-decoration: none;
  transition: all 0.2s;
}

.btn-nav-login:hover {
  background: #E0F2FE;
}

/* HERO SECTION WITH ENLARGED 1.5X IMAGE & NO ANIMATION */
.driver-hero-wrapper {
  position: relative;
  padding: 40px 24px 45px 24px;
  background: linear-gradient(180deg, #F0F9FF 0%, #E0F2FE 40%, #F8FAFC 100%);
  overflow: hidden;
}

.hero-bg-glow {
  position: absolute;
  top: -100px;
  right: -50px;
  width: 600px;
  height: 600px;
  background: radial-gradient(circle, rgba(56, 189, 248, 0.15) 0%, rgba(255,255,255,0) 70%);
  pointer-events: none;
}

.hero-content-grid {
  max-width: 1280px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  gap: 40px;
}

.hero-left-col {
  position: relative;
  z-index: 5;
}

.hero-badge-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #ffffff;
  border: 1.5px solid #BAE6FD;
  color: #0369A1;
  font-size: 12px;
  font-weight: 800;
  padding: 6px 16px;
  border-radius: 30px;
  box-shadow: 0 4px 12px rgba(2, 132, 199, 0.06);
  margin-bottom: 20px;
}

.hero-main-title {
  font-size: 42px;
  font-weight: 900;
  line-height: 1.22;
  color: #0F172A;
  margin-bottom: 18px;
  letter-spacing: -0.5px;
}

.text-gradient {
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.text-highlight {
  color: #D94E15;
}

.hero-sub-text {
  font-size: 16px;
  color: #475569;
  line-height: 1.65;
  margin-bottom: 32px;
  max-width: 600px;
}

.hero-action-row {
  display: flex;
  gap: 16px;
  margin-bottom: 40px;
}

.btn-hero-glow {
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #ffffff;
  border: none;
  padding: 16px 30px;
  border-radius: 16px;
  font-weight: 900;
  font-size: 15.5px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 10px;
  box-shadow: 0 10px 25px rgba(2, 132, 199, 0.3);
  transition: all 0.25s;
}

.btn-hero-glow:hover {
  transform: translateY(-2px);
  box-shadow: 0 14px 30px rgba(2, 132, 199, 0.4);
}

.btn-hero-glass {
  background: #ffffff;
  color: #0369A1;
  border: 1.5px solid #BAE6FD;
  padding: 16px 24px;
  border-radius: 16px;
  font-weight: 800;
  font-size: 14.5px;
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.03);
}

.btn-hero-glass:hover {
  background: #F0F9FF;
}

.hero-stats-card {
  display: flex;
  align-items: center;
  gap: 20px;
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(10px);
  border: 1.5px solid #BAE6FD;
  padding: 18px 24px;
  border-radius: 20px;
  box-shadow: 0 8px 25px rgba(2, 132, 199, 0.06);
  max-width: 560px;
}

.stat-val {
  display: block;
  font-size: 22px;
  font-weight: 900;
  color: #0284C7;
}

.stat-lbl {
  font-size: 12px;
  color: #64748B;
  font-weight: 600;
}

.stat-divider {
  width: 1px;
  height: 32px;
  background: #CBD5E1;
}

/* HERO RIGHT ILLUST - SITS CLEANLY TO THE RIGHT WITHOUT OVERLAPPING TEXT */
.hero-right-col {
  position: relative;
  z-index: 1;
  display: flex;
  justify-content: flex-end;
  align-items: center;
}

.hero-image-floating-container {
  position: relative;
  width: 100%;
  max-width: 660px;
}

.image-glow-backdrop {
  position: absolute;
  inset: -20px;
  background: radial-gradient(circle, rgba(56, 189, 248, 0.25) 0%, rgba(255,255,255,0) 70%);
  border-radius: 50%;
  filter: blur(20px);
}

.hero-large-illustration {
  width: 100%;
  max-width: 100%;
  height: auto;
  object-fit: contain;
  transform: scale(1.2);
  transform-origin: center right;
  filter: drop-shadow(0 20px 30px rgba(2, 132, 199, 0.15));
}

.floating-floating-badge {
  position: absolute;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(12px);
  border: 1.5px solid #BAE6FD;
  padding: 10px 16px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.08);
  animation: floatBadge 5s ease-in-out infinite;
}

.badge-top {
  top: 40px;
  left: -20px;
}

.badge-bottom {
  bottom: 30px;
  right: -10px;
  animation-delay: 2.5s;
}

@keyframes floatBadge {
  0%, 100% { transform: translateY(0px); }
  50% { transform: translateY(-8px); }
}

.badge-icon-box {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.badge-icon-box.cyan { background: #E0F2FE; }
.badge-icon-box.green { background: #D1FAE5; }

.b-title {
  display: block;
  font-size: 13px;
  font-weight: 800;
  color: #0F172A;
}

.b-sub {
  font-size: 11px;
  color: #64748B;
}

/* CALCULATOR SECTION */
.calculator-section {
  max-width: 1240px;
  margin: 32px auto;
  padding: 0 24px;
}

.calc-card-glass {
  background: linear-gradient(135deg, #ffffff 0%, #F0F9FF 100%);
  border: 1.5px solid #BAE6FD;
  border-radius: 28px;
  padding: 36px 44px;
  box-shadow: 0 12px 35px rgba(2, 132, 199, 0.06);
}

.calc-header { margin-bottom: 28px; }

.calc-body-grid {
  display: grid;
  grid-template-columns: 1.2fr 0.8fr;
  gap: 36px;
  align-items: center;
}

.calc-slider-box {
  background: #ffffff;
  padding: 24px;
  border-radius: 20px;
  border: 1px solid #E2E8F0;
}

.slider-label-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 15px;
  color: #334155;
  font-weight: 700;
  margin-bottom: 16px;
}

.hours-val {
  color: #0284C7;
  font-size: 18px;
}

.custom-range-slider {
  width: 100%;
  accent-color: #0284C7;
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

.calc-result-box {
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #ffffff;
  padding: 28px;
  border-radius: 22px;
  box-shadow: 0 10px 25px rgba(2, 132, 199, 0.3);
}

.result-title {
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 1px;
  opacity: 0.9;
  margin-bottom: 8px;
}

.result-amount {
  font-size: 34px;
  font-weight: 900;
  line-height: 1.1;
  margin-bottom: 12px;
}

.month-unit { font-size: 16px; font-weight: 600; opacity: 0.8; }

.result-desc {
  font-size: 12px;
  opacity: 0.85;
  line-height: 1.5;
  margin-bottom: 20px;
}

.btn-calc-apply {
  width: 100%;
  background: #ffffff;
  color: #0369A1;
  border: none;
  padding: 13px;
  border-radius: 12px;
  font-weight: 900;
  font-size: 13.5px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-calc-apply:hover { background: #F0F9FF; }

/* 6 BENEFITS GRID SECTION WITH HAND-CRAFTED SVG ICONS */
.benefits-grid-section {
  max-width: 1240px;
  margin: 36px auto;
  padding: 0 24px;
}

.section-tag-pill {
  display: inline-block;
  background: #E0F2FE;
  color: #0369A1;
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
  margin-bottom: 28px;
}

.benefits-6-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
}

.benefit-card-modern {
  background: #ffffff;
  padding: 28px 22px;
  border-radius: 22px;
  border: 1.5px solid #E2E8F0;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.03);
  transition: all 0.25s ease;
}

.benefit-card-modern:hover {
  transform: translateY(-4px);
  border-color: #BAE6FD;
  box-shadow: 0 14px 30px rgba(2, 132, 199, 0.1);
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

.card-svg-icon-wrap.cyan-theme { background: #E0F2FE; }
.card-svg-icon-wrap.green-theme { background: #D1FAE5; }
.card-svg-icon-wrap.orange-theme { background: #FFF7ED; }
.card-svg-icon-wrap.purple-theme { background: #F3E8FF; }
.card-svg-icon-wrap.blue-theme { background: #DBEAFE; }
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

/* STEPS FLOW SECTION WITH CENTERED TITLE & RIGHT TAG PILL */
.steps-flow-section {
  background: #F0F9FF;
  padding: 36px 24px;
  border-top: 1px solid #BAE6FD;
  border-bottom: 1px solid #BAE6FD;
  margin: 36px 0;
}

.steps-title-centered-block {
  width: 100%;
  text-align: center;
  margin: 0 auto 32px auto;
}

.section-heading-centered-full {
  font-size: 30px;
  font-weight: 900;
  color: #0F172A;
  text-align: center;
  margin: 0 auto;
  display: block;
}

.steps-flow-container {
  max-width: 1100px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.step-card-box {
  background: #ffffff;
  border-radius: 22px;
  padding: 26px 20px;
  text-align: center;
  flex: 1;
  border: 1.5px solid #BAE6FD;
  position: relative;
}

.step-badge-num {
  position: absolute;
  top: -14px;
  left: 50%;
  transform: translateX(-50%);
  background: #0284C7;
  color: #ffffff;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  font-weight: 900;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
}

.step-svg-wrap {
  margin-top: 10px;
  margin-bottom: 14px;
}

.step-card-box h4 {
  font-size: 17px;
  font-weight: 800;
  color: #0F172A;
  margin-bottom: 8px;
}

.step-card-box p {
  font-size: 13.5px;
  color: #64748B;
  line-height: 1.5;
}

.flow-connector-arrow {
  padding: 0 16px;
  opacity: 0.6;
}

/* MODERN FORM SECTION (NEUMORPHISM / APPLE FLOATING CARD) */
.form-section-floating {
  max-width: 860px;
  margin: 36px auto;
  padding: 0 24px;
}

.modern-form-card {
  background: #ffffff;
  border-radius: 30px;
  padding: 38px 46px;
  border: 1.5px solid #BAE6FD;
  box-shadow: 0 15px 45px rgba(2, 132, 199, 0.08);
}

.form-title-header {
  text-align: center;
  width: 100%;
  margin-bottom: 28px;
}

.form-card-title-centered {
  font-size: 26px;
  font-weight: 900;
  color: #0F172A;
  text-align: center;
  margin: 0 0 8px 0;
  display: block;
}

.form-card-sub-centered {
  font-size: 14px;
  color: #64748B;
  text-align: center;
  margin: 0 auto;
  display: block;
}

.pills-stepper-bar {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  margin-bottom: 36px;
}

.pill-step {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border-radius: 30px;
  background: #F1F5F9;
  color: #64748B;
  font-size: 13px;
  font-weight: 700;
}

.pill-step.active {
  background: #E0F2FE;
  color: #0369A1;
  border: 1px solid #BAE6FD;
}

.pill-num {
  width: 22px;
  height: 22px;
  background: #CBD5E1;
  color: #ffffff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11.5px;
}

.pill-step.active .pill-num {
  background: #0284C7;
}

.pill-line {
  width: 30px;
  height: 2px;
  background: #E2E8F0;
}

.pill-line.active { background: #0284C7; }

.msg-alert-banner {
  background: #FEF2F2;
  border: 1.5px solid #FECACA;
  color: #991B1B;
  padding: 12px 16px;
  border-radius: 14px;
  font-size: 13.5px;
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 24px;
}

.form-fields-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.full-row { grid-column: span 2; }

.input-label-stylish {
  display: block;
  font-size: 13.5px;
  font-weight: 700;
  color: #334155;
  margin-bottom: 8px;
}

.req { color: #EF4444; }
.hint-tag { font-weight: 400; color: #64748B; font-size: 12px; }

.input-with-icon {
  position: relative;
  display: flex;
  align-items: center;
}

.input-with-icon > svg.input-left-icon,
.input-with-icon > svg:not(.eye-toggle-btn svg) {
  position: absolute;
  left: 14px;
  pointer-events: none;
}

.modern-input {
  width: 100%;
  padding: 13px 16px 13px 44px;
  border-radius: 14px;
  border: 1.5px solid #CBD5E1;
  font-size: 14.5px;
  outline: none;
  background: #ffffff;
  transition: all 0.2s;
}

.modern-input.uppercase-input {
  text-transform: uppercase;
  font-weight: 800;
  letter-spacing: 0.5px;
}

.modern-input:focus {
  border-color: #0284C7;
  box-shadow: 0 0 0 4px rgba(2, 132, 199, 0.12);
}

/* DRAG & DROP UPLOAD CARDS */
.upload-cards-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.upload-drop-card {
  border: 2px dashed #BAE6FD;
  background: #F0F9FF;
  border-radius: 18px;
  padding: 20px;
  text-align: center;
  position: relative;
  transition: all 0.2s;
}

.upload-drop-card:hover {
  border-color: #0284C7;
  background: #E0F2FE;
}

.upload-card-label {
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.upload-icon-circle {
  width: 44px;
  height: 44px;
  background: #ffffff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 10px;
  box-shadow: 0 4px 10px rgba(2, 132, 199, 0.1);
}

.upload-txt-title {
  font-size: 13px;
  font-weight: 800;
  color: #0F172A;
}

.upload-txt-sub {
  font-size: 11.5px;
  color: #64748B;
  margin-top: 2px;
}

.hidden-file-input { display: none; }

.upload-preview-box {
  margin-top: 10px;
  position: relative;
}

.upload-preview-box img {
  max-height: 80px;
  border-radius: 10px;
  border: 1px solid #CBD5E1;
}

.preview-badge {
  display: block;
  font-size: 11px;
  font-weight: 800;
  color: #059669;
  margin-top: 4px;
}

/* CUSTOM DROPDOWNS STYLISH */
.custom-select-container {
  position: relative;
  z-index: 20;
}

.custom-select-container.dropdown-open {
  z-index: 120;
}

.custom-select-trigger-stylish {
  padding: 13px 16px;
  border-radius: 14px;
  border: 1.5px solid #CBD5E1;
  background: #ffffff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14.5px;
  font-weight: 600;
  cursor: pointer;
  user-select: none;
}

.custom-dropdown-menu-stylish {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  background: #ffffff;
  border: 1.5px solid #BAE6FD;
  border-radius: 14px;
  box-shadow: 0 12px 30px rgba(2, 132, 199, 0.16);
  z-index: 150;
  max-height: 200px;
  overflow-y: auto;
  padding: 4px 0;
}

.custom-dropdown-menu-stylish::-webkit-scrollbar {
  width: 6px;
}

.custom-dropdown-menu-stylish::-webkit-scrollbar-track {
  background: #F0F9FF;
  border-radius: 10px;
}

.custom-dropdown-menu-stylish::-webkit-scrollbar-thumb {
  background: #0284C7;
  border-radius: 10px;
}

.dropdown-item-stylish {
  padding: 11px 18px;
  font-size: 14px;
  cursor: pointer;
  transition: background 0.15s;
}

.dropdown-item-stylish:hover {
  background: #E0F2FE;
  color: #0284C7;
  font-weight: 700;
}

/* BUTTONS SIDE BY SIDE SINGLE ROW */
.step-btn-row-stylish {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-top: 14px;
  width: 100%;
}

.btn-prev-stylish {
  background: #F1F5F9;
  color: #475569;
  border: 1.5px solid #CBD5E1;
  padding: 0 20px;
  height: 48px;
  border-radius: 14px;
  font-weight: 800;
  font-size: 13.5px;
  cursor: pointer;
  white-space: nowrap;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all 0.2s ease;
}

.btn-prev-stylish:hover {
  background: #E2E8F0;
  color: #0F172A;
}

.btn-step-next-glow, .btn-submit-final-glow {
  flex: 1;
  height: 48px;
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #ffffff;
  border: none;
  padding: 0 20px;
  border-radius: 14px;
  font-size: 14px;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  white-space: nowrap;
  box-shadow: 0 6px 18px rgba(2, 132, 199, 0.22);
  transition: all 0.2s ease;
}

.btn-step-next-glow:hover, .btn-submit-final-glow:hover {
  transform: translateY(-1px);
  box-shadow: 0 8px 22px rgba(2, 132, 199, 0.32);
}

.stylish-checkbox-label {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13.5px;
  color: #475569;
  cursor: pointer;
}

.custom-chk-box {
  width: 18px;
  height: 18px;
  accent-color: #0284C7;
}

/* FAQ & SUPPORT SPLIT SECTION */
.faq-support-split-section {
  max-width: 1320px;
  margin: 36px auto;
  padding: 0 24px;
}

.faq-support-grid {
  display: grid;
  grid-template-columns: 1.15fr 0.85fr;
  gap: 32px;
  align-items: start;
}

/* SHIPPER SUPPORT FORM CARD (LEFT SIDE) */
.shipper-support-form-card {
  background: #ffffff;
  border: 1.5px solid #BAE6FD;
  border-radius: 24px;
  padding: 32px;
  box-shadow: 0 10px 30px rgba(2, 132, 199, 0.06);
}

.support-card-header h3 {
  font-size: 20px;
  font-weight: 900;
  color: #0F172A;
}

.support-card-header p {
  font-size: 13px;
  color: #64748B;
  margin-top: 4px;
  margin-bottom: 12px;
}

.support-cskh-quick-info {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
  margin-bottom: 20px;
  padding: 10px 14px;
  background: #F0F9FF;
  border: 1px solid #BAE6FD;
  border-radius: 14px;
}

.info-pill-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: #334155;
}

.info-pill-item b {
  color: #0284C7;
}

.msg-alert-banner.success-alert {
  background: #ECFDF5;
  border-color: #A7F3D0;
  color: #065F46;
}

.compact-support-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.form-row-2col {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.modern-input-small {
  width: 100%;
  padding: 10px 14px;
  border-radius: 12px;
  border: 1.5px solid #CBD5E1;
  font-size: 13.5px;
  outline: none;
  background: #ffffff;
  transition: all 0.2s;
}

.modern-input-small:focus {
  border-color: #0284C7;
  box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.12);
}

.modern-textarea-small {
  width: 100%;
  padding: 10px 14px;
  border-radius: 12px;
  border: 1.5px solid #CBD5E1;
  font-size: 13.5px;
  outline: none;
  resize: vertical;
  font-family: inherit;
  transition: all 0.2s;
}

.modern-textarea-small:focus {
  border-color: #0284C7;
  box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.12);
}

.topic-pills-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.topic-btn-pill {
  background: #F1F5F9;
  color: #475569;
  border: 1px solid #CBD5E1;
  font-size: 12px;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: 20px;
  cursor: pointer;
  transition: all 0.2s;
}

.topic-btn-pill.active, .topic-btn-pill:hover {
  background: #0284C7;
  color: #ffffff;
  border-color: #0284C7;
}

.btn-send-support {
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #ffffff;
  border: none;
  padding: 14px;
  border-radius: 14px;
  font-weight: 900;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 6px 18px rgba(2, 132, 199, 0.2);
}

.btn-send-support:hover {
  transform: translateY(-1.5px);
  box-shadow: 0 8px 22px rgba(2, 132, 199, 0.3);
}

/* SHIPPER FAQ RIGHT CARD */
.shipper-faq-right-card {
  background: #ffffff;
  border: 1.5px solid #E2E8F0;
  border-radius: 24px;
  padding: 32px;
  box-shadow: 0 6px 20px rgba(0,0,0,0.02);
}

.faq-card-header h3 {
  font-size: 20px;
  font-weight: 900;
  color: #0F172A;
}

.faq-card-header p {
  font-size: 13px;
  color: #64748B;
  margin-top: 4px;
  margin-bottom: 20px;
}

.faq-list-container-compact {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.faq-item-card {
  background: #F8FAFC;
  border: 1.5px solid #E2E8F0;
  border-radius: 16px;
  padding: 16px 20px;
  cursor: pointer;
  transition: all 0.2s;
}

.faq-item-card:hover { border-color: #BAE6FD; background: #ffffff; }

.faq-question-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
}

.faq-question-header h3 {
  font-size: 14.5px;
  font-weight: 800;
  color: #0F172A;
  margin: 0;
  line-height: 1.4;
}

.faq-toggle-icon {
  transition: transform 0.25s;
  flex-shrink: 0;
}

.faq-item-card.open .faq-toggle-icon {
  transform: rotate(180deg);
}

.faq-answer-body {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid #E2E8F0;
  font-size: 13.5px;
  color: #475569;
  line-height: 1.55;
}

/* PHẦN BỔ SUNG LẤP ĐẦY KHOẢNG TRỐNG BÊN PHẢI UNDER FAQ */
.faq-bottom-guideline-box {
  margin-top: 24px;
  padding: 20px;
  background: linear-gradient(135deg, #F0F9FF 0%, #E0F2FE 100%);
  border: 1.5px solid #BAE6FD;
  border-radius: 18px;
}

.guideline-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 14px;
}

.guideline-icon {
  width: 36px;
  height: 36px;
  background: #ffffff;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 10px rgba(2, 132, 199, 0.12);
}

.guideline-header h4 {
  font-size: 15px;
  font-weight: 800;
  color: #0369A1;
  margin: 0;
}

.guideline-list {
  list-style: none;
  padding: 0;
  margin: 0 0 14px 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.guideline-list li {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  font-size: 12.5px;
  color: #334155;
  line-height: 1.45;
}

.g-bullet {
  width: 20px;
  height: 20px;
  background: #0284C7;
  color: #ffffff;
  border-radius: 50%;
  font-size: 11px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  margin-top: 1px;
}

.guideline-footer-hotline {
  background: #ffffff;
  padding: 8px 14px;
  border-radius: 10px;
  font-size: 12px;
  color: #475569;
  text-align: center;
  border: 1px solid #BAE6FD;
}

.guideline-footer-hotline b {
  color: #D94E15;
  font-size: 13px;
}

/* RICH FOOTER DÀNH RIÊNG CHO SHIPPER - STREAMLINED WIDE & BALANCED LAYOUT */
.driver-footer-rich {
  background: #0F172A;
  color: #94A3B8;
  padding: 70px 48px 30px 48px;
  font-size: 13.5px;
  border-top: 3px solid #0284C7;
}

.footer-top-grid {
  max-width: 1360px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 1.8fr 1fr 1fr 1fr 1fr;
  gap: 28px;
  padding-bottom: 50px;
  border-bottom: 1px solid #1E293B;
}

.footer-brand-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.f-logo-img { height: 42px; width: auto; }

.f-brand-name {
  font-size: 22px;
  font-weight: 900;
  color: #ffffff;
}

.f-brand-name .highlight { color: #D94E15; }

.f-driver-tag {
  display: block;
  font-size: 10px;
  font-weight: 900;
  color: #38BDF8;
  letter-spacing: 1px;
}

.f-brand-desc {
  font-size: 13px;
  line-height: 1.6;
  margin-bottom: 20px;
  color: #94A3B8;
}

.footer-contact-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.footer-contact-list li {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 12.5px;
  white-space: nowrap;
}

.footer-contact-list li b { color: #F1F5F9; }

.f-col-heading {
  font-size: 13.5px;
  font-weight: 900;
  color: #ffffff;
  letter-spacing: 0.5px;
  margin-bottom: 18px;
  white-space: nowrap;
}

.f-links-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.f-links-list a {
  color: #94A3B8;
  text-decoration: none;
  font-size: 13px;
  white-space: nowrap;
  transition: color 0.2s;
}

.f-links-list a:hover { color: #38BDF8; }

.payment-tags-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.pay-tag {
  background: #1E293B;
  color: #CBD5E1;
  font-size: 11.5px;
  font-weight: 700;
  padding: 5px 10px;
  border-radius: 8px;
  border: 1px solid #334155;
  white-space: nowrap;
}

.pay-tag.highlight {
  background: #0284C7;
  color: #ffffff;
  border: none;
}

.social-icons-row {
  display: flex;
  gap: 10px;
}

.social-btn {
  width: 36px;
  height: 36px;
  background: #1E293B;
  color: #ffffff;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
  transition: all 0.2s;
}

.social-btn:hover {
  background: #0284C7;
  transform: translateY(-2px);
}

.footer-bottom-line {
  max-width: 1360px;
  margin: 24px auto 0 auto;
  font-size: 12.5px;
  color: #64748B;
}

/* MODAL POPUP THÀNH CÔNG */
.modal-backdrop-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 99999;
  padding: 20px;
  box-sizing: border-box;
}

.modal-success-card {
  background: #ffffff;
  padding: 38px 32px;
  border-radius: 28px;
  width: 100%;
  max-width: 500px;
  box-shadow: 0 25px 50px -12px rgba(2, 132, 199, 0.25);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  animation: modalScaleUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.modal-icon-badge-glow {
  width: 72px;
  height: 72px;
  background: #ECFDF5;
  border: 2px solid #A7F3D0;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 20px auto;
  box-shadow: 0 8px 20px rgba(16, 185, 129, 0.15);
}

.modal-success-title {
  font-size: 22px;
  font-weight: 900;
  color: #0F172A;
  margin: 0 0 14px 0;
  text-align: center;
  width: 100%;
  line-height: 1.3;
}

.modal-success-desc {
  font-size: 14px;
  color: #475569;
  line-height: 1.6;
  margin: 0 0 26px 0;
  text-align: center;
  width: 100%;
}

.modal-success-desc b {
  color: #0284C7;
}

.modal-btn-row-centered {
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
}

.btn-modal-action-primary {
  background: linear-gradient(135deg, #0284C7 0%, #0369A1 100%);
  color: #ffffff;
  border: none;
  padding: 13px 36px;
  border-radius: 30px;
  font-weight: 900;
  font-size: 14px;
  letter-spacing: 0.5px;
  cursor: pointer;
  display: inline-block;
  margin: 0 auto;
  box-shadow: 0 8px 22px rgba(2, 132, 199, 0.3);
  transition: all 0.2s ease;
}

.btn-modal-action-primary:hover {
  transform: translateY(-1.5px);
  box-shadow: 0 10px 25px rgba(2, 132, 199, 0.4);
}

.form-actions-centered {
  display: flex !important;
  justify-content: center !important;
  align-items: center !important;
  width: 100% !important;
  margin-top: 20px;
}

.form-actions-centered .btn-step-next-glow {
  margin: 0 auto !important;
  display: flex !important;
}

.btn-submit-final-glow.btn-disabled,
.btn-submit-final-glow:disabled {
  background: #94a3b8 !important;
  border-color: #94a3b8 !important;
  color: #ffffff !important;
  cursor: not-allowed !important;
  box-shadow: none !important;
  opacity: 0.65 !important;
  transform: none !important;
}

/* EYE TOGGLE BUTTON STYLING FOR PASSWORD FIELDS */
.input-with-eye-toggle {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-with-eye-toggle .modern-input {
  padding-right: 48px !important;
}

.eye-toggle-btn {
  position: absolute !important;
  right: 12px !important;
  left: auto !important;
  top: 50% !important;
  transform: translateY(-50%) !important;
  background: transparent !important;
  border: none !important;
  cursor: pointer !important;
  padding: 6px !important;
  display: flex !important;
  align-items: center !important;
  justify-content: center !important;
  color: #64748B !important;
  border-radius: 8px !important;
  transition: all 0.2s ease !important;
  z-index: 10 !important;
  pointer-events: auto !important;
}

.eye-toggle-btn:hover {
  color: #0284C7 !important;
  background: rgba(2, 132, 199, 0.12) !important;
}

.eye-toggle-btn svg {
  position: static !important;
  left: auto !important;
  top: auto !important;
  transform: none !important;
  pointer-events: none !important;
}
</style>
