/**
 * ================================================================
 * COMPOSABLE DỮ LIỆU GIAN HÀNG RIÊNG BIỆT CHO SELLER DASHBOARD
 * Cung cấp thông số, đơn hàng, khách hàng, sản phẩm và đánh giá
 * độc nhất cho từng gian hàng (hoàn toàn không bị trùng lặp).
 * ================================================================
 */

export interface SellerOrderItem {
  name: string;
  qty: number;
  price: number;
}

export interface SellerOrder {
  id: string;
  orderCode: string;
  productSummary: string;
  customerName: string;
  customerPhone: string;
  customerAddress: string;
  orderDate: string;
  priceFormatted: string;
  paymentMethod: "Chuyển khoản QR" | "Thẻ ngân hàng" | "Tiền mặt COD";
  status: "Đang xử lý" | "Hoàn thành" | "Chờ xác nhận" | "Đang giao hàng";
  items: SellerOrderItem[];
  shipperInfo?: {
    name: string;
    phone: string;
    licensePlate: string;
  };
}

export interface SellerCustomer {
  id: string;
  name: string;
  avatarBadge: string;
  phone: string;
  address: string;
  tag: string;
}

export interface SellerTopProduct {
  id: string;
  name: string;
  salesText: string;
  statusText: string;
  stockText: string;
  image: string;
}

export interface SellerFeedback {
  id: string;
  author: string;
  comment: string;
  rating: number;
}

export interface SellerStarterProduct {
  id: string;
  name: string;
  category: string;
  price: number;
  unit: string;
  stock: number;
  image: string;
  aiSafetyScore: number;
  aiMatchScore: number;
}

export interface SellerStoreProfile {
  id: string;
  name: string;
  category: string;
  address: string;
  phone: string;
  openHours: string;
  bankName: string;
  bankAccount: string;
  accountHolder: string;
  radiusKm: number;
  rating: number;
  avgOrderValue: string;
  totalOrders: string;
  lifetimeValue: string;
  trendAvgOrder: string;
  trendTotalOrders: string;
  trendLifetimeValue: string;
  chartMonthlyRev: string;
  chartMonthlyOrders: string;
  topProducts: SellerTopProduct[];
  customers: SellerCustomer[];
  orders: SellerOrder[];
  feedbacks: SellerFeedback[];
  starterProducts: SellerStarterProduct[];
}

// 1. CSDL DỮ LIỆU ĐẶC TRƯNG CHO 9 GIAN HÀNG TIÊU BIỂU (KÈM TEST GIAN HÀNG)
const KNOWN_STORE_PROFILES: Record<string, SellerStoreProfile> = {
  // GIAN HÀNG TEST (Người dùng đang đăng nhập kiểm thử)
  "test gian hang": {
    id: "ZM-TEST01",
    name: "Test Gian Hàng",
    category: "Nông sản sạch & Bách hóa tổng hợp",
    address: "Số 15 ngõ 68 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội",
    phone: "0912 888 777",
    openHours: "07:00 - 21:30",
    bankName: "Vietcombank (VCB)",
    bankAccount: "9988 2468 1357",
    accountHolder: "TEST GIAN HANG",
    radiusKm: 10,
    rating: 4.9,
    avgOrderValue: "95.500 đ",
    totalOrders: "1,480",
    lifetimeValue: "46.200.000 đ",
    trendAvgOrder: "+ 3.85%",
    trendTotalOrders: "+ 2.10%",
    trendLifetimeValue: "+ 4.30%",
    chartMonthlyRev: "16.4 tr",
    chartMonthlyOrders: "128 đơn",
    topProducts: [
      {
        id: "tp-test-1",
        name: "Set Nông Sản Xanh Tuyển Chọn Test Tiệm",
        salesText: "1,680 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 85 tồn kho",
        image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-test-2",
        name: "Bưởi Da Xanh Tuyển Chọn Loại 1",
        salesText: "1,150 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 42 tồn kho",
        image: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-test-3",
        name: "Gạo Lứt Huyết Rồng Hữu Cơ (Túi 2kg)",
        salesText: "820 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 120 tồn kho",
        image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-test-1",
        name: "Chị Bích Phương",
        avatarBadge: "BP",
        phone: "0915 234 567",
        address: "Mai Dịch, Cầu Giấy, Hà Nội",
        tag: "Khách VIP • 15 đơn hàng"
      },
      {
        id: "cus-test-2",
        name: "Anh Quang Huy",
        avatarBadge: "QH",
        phone: "0982 345 678",
        address: "Xuân Thủy, Cầu Giấy, Hà Nội",
        tag: "Khách quen • 11 đơn hàng"
      },
      {
        id: "cus-test-3",
        name: "Chị Ngọc Ánh",
        avatarBadge: "NA",
        phone: "0978 456 789",
        address: "Nghĩa Đô, Cầu Giấy, Hà Nội",
        tag: "Khách quen • 8 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-test-01",
        orderCode: "#8821TX",
        productSummary: "Set Nông Sản Xanh (x1) & Bưởi Da Xanh (x1)",
        customerName: "Chị Bích Phương",
        customerPhone: "0915 234 567",
        customerAddress: "P.302 Chung cư Mai Dịch, Cầu Giấy, Hà Nội",
        orderDate: "Hôm nay, 10:25",
        priceFormatted: "120.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Set Nông Sản Xanh Tuyển Chọn", qty: 1, price: 45000 },
          { name: "Bưởi Da Xanh Tuyển Chọn", qty: 1, price: 75000 }
        ]
      },
      {
        id: "ord-test-02",
        orderCode: "#3314TX",
        productSummary: "Gạo Lứt Huyết Rồng (x1) & Mật Ong Rừng (x1)",
        customerName: "Anh Quang Huy",
        customerPhone: "0982 345 678",
        customerAddress: "Số 45 ngõ 12 Xuân Thủy, Cầu Giấy, Hà Nội",
        orderDate: "Hôm qua, 15:10",
        priceFormatted: "250.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Gạo Lứt Huyết Rồng Hữu Cơ (Túi 2kg)", qty: 1, price: 85000 },
          { name: "Mật Ong Rừng Hoa Bạc Hà (500ml)", qty: 1, price: 165000 }
        ],
        shipperInfo: {
          name: "Vũ Đình Trọng",
          phone: "0934 112 233",
          licensePlate: "29P1-8866"
        }
      },
      {
        id: "ord-test-03",
        orderCode: "#9945TX",
        productSummary: "Set Nông Sản Xanh (x2)",
        customerName: "Chị Ngọc Ánh",
        customerPhone: "0978 456 789",
        customerAddress: "Số 88 đường Nghĩa Đô, Cầu Giấy",
        orderDate: "16 Thg 9, 09:30",
        priceFormatted: "90.000 đ",
        paymentMethod: "Tiền mặt COD",
        status: "Hoàn thành",
        items: [
          { name: "Set Nông Sản Xanh Tuyển Chọn", qty: 2, price: 45000 }
        ]
      },
      {
        id: "ord-test-04",
        orderCode: "#7728TX",
        productSummary: "Bưởi Da Xanh Tuyển Chọn (x2)",
        customerName: "Bác Thế Hùng",
        customerPhone: "0904 112 334",
        customerAddress: "Số 120 Hoàng Quốc Việt, Cầu Giấy, Hà Nội",
        orderDate: "15 Thg 9, 14:15",
        priceFormatted: "150.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Bưởi Da Xanh Tuyển Chọn Loại 1", qty: 2, price: 75000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-test-1",
        author: "Chị Bích Phương",
        comment: "Gian hàng phục vụ rất tận tình, rau củ tươi non đóng gói hút chân không sạch sẽ!",
        rating: 5
      },
      {
        id: "fb-test-2",
        author: "Anh Quang Huy",
        comment: "Giao hàng hỏa tốc trong 25 phút, gạo lứt thơm ngon bùi dẻo, ủng hộ quán lâu dài.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p-test-1",
        name: "Set Nông Sản Xanh Tuyển Chọn Test Tiệm",
        category: "Rau củ quả",
        price: 45000,
        unit: "Combo 1kg",
        stock: 85,
        image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p-test-2",
        name: "Bưởi Da Xanh Tuyển Chọn Loại 1",
        category: "Trái cây tươi",
        price: 75000,
        unit: "Quả 1.4kg",
        stock: 42,
        image: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 97
      },
      {
        id: "p-test-3",
        name: "Gạo Lứt Huyết Rồng Hữu Cơ (Túi 2kg)",
        category: "Đặc sản khô",
        price: 85000,
        unit: "Túi 2kg",
        stock: 120,
        image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p-test-4",
        name: "Mật Ong Rừng Hoa Bạc Hà Tự Nhiên (500ml)",
        category: "Đặc sản khô",
        price: 165000,
        unit: "Chai 500ml",
        stock: 50,
        image: "https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 95
      }
    ]
  },

  // GIAN HÀNG 1: Vườn Rau Hữu Cơ Bác Ba
  "vuon rau huu co bac ba": {
    id: "ZM-S882",
    name: "Vườn Rau Hữu Cơ Bác Ba",
    category: "Rau củ hữu cơ VietGAP",
    address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
    phone: "0988 123 456",
    openHours: "06:00 - 20:30",
    bankName: "Vietcombank (VCB)",
    bankAccount: "1029384756",
    accountHolder: "NGUYEN VAN BA",
    radiusKm: 10,
    rating: 5.0,
    avgOrderValue: "68.500 đ",
    totalOrders: "2,450",
    lifetimeValue: "65.200.000 đ",
    trendAvgOrder: "+ 3.16%",
    trendTotalOrders: "+ 2.40%",
    trendLifetimeValue: "+ 4.12%",
    chartMonthlyRev: "18.5 tr",
    chartMonthlyOrders: "142 đơn",
    topProducts: [
      {
        id: "tp-bb-1",
        name: "Rau muống Ba Vì hữu cơ non mướt",
        salesText: "3,620 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 150 bó",
        image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-bb-2",
        name: "Xà lách mỡ Ba Vì thủy canh giòn ngọt",
        salesText: "2,840 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 80 cây",
        image: "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-bb-3",
        name: "Cà chua bi socola giống F1 Đà Lạt",
        salesText: "2,150 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 95 hộp",
        image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-bb-1",
        name: "Chị Mai Lan",
        avatarBadge: "ML",
        phone: "0912 345 678",
        address: "Cầu Giấy, Hà Nội",
        tag: "Khách VIP • 18 đơn hàng"
      },
      {
        id: "cus-bb-2",
        name: "Bác Tuấn Hưng",
        avatarBadge: "TH",
        phone: "0977 444 555",
        address: "Xuân Thủy, Hà Nội",
        tag: "Khách quen • 14 đơn hàng"
      },
      {
        id: "cus-bb-3",
        name: "Chị Bích Phương",
        avatarBadge: "BP",
        phone: "0915 234 567",
        address: "Mai Dịch, Hà Nội",
        tag: "Khách quen • 9 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-bb-01",
        orderCode: "#2456JL",
        productSummary: "Xà lách mỡ (x2) & Cà chua bi (x1)",
        customerName: "Chị Mai Lan",
        customerPhone: "0912 345 678",
        customerAddress: "P.502 Chung cư Dịch Vọng, Cầu Giấy, Hà Nội",
        orderDate: "Hôm nay, 11:20",
        priceFormatted: "76.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Xà lách mỡ Ba Vì thủy canh", qty: 2, price: 22000 },
          { name: "Cà chua bi socola giống F1", qty: 1, price: 32000 }
        ]
      },
      {
        id: "ord-bb-02",
        orderCode: "#5435DF",
        productSummary: "Rau muống Ba Vì (x3) & Bí xanh thơm (x2)",
        customerName: "Bác Tuấn Hưng",
        customerPhone: "0977 444 555",
        customerAddress: "Số 18 ngõ 20 Hồ Tùng Mậu, Cầu Giấy",
        orderDate: "Hôm qua, 14:15",
        priceFormatted: "104.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Rau muống Ba Vì hữu cơ", qty: 3, price: 18000 },
          { name: "Bí xanh thơm Ba Bể", qty: 2, price: 25000 }
        ],
        shipperInfo: {
          name: "Trần Văn Bình",
          phone: "0934 888 999",
          licensePlate: "29M1-9999"
        }
      },
      {
        id: "ord-bb-03",
        orderCode: "#9876XC",
        productSummary: "Cà chua bi socola (x2) & Rau muống (x2)",
        customerName: "Chị Bích Phương",
        customerPhone: "0915 234 567",
        customerAddress: "Số 92 đường Trần Thái Tông, Cầu Giấy",
        orderDate: "16 Thg 9, 09:08",
        priceFormatted: "100.000 đ",
        paymentMethod: "Tiền mặt COD",
        status: "Hoàn thành",
        items: [
          { name: "Cà chua bi socola giống F1", qty: 2, price: 32000 },
          { name: "Rau muống Ba Vì hữu cơ", qty: 2, price: 18000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-bb-1",
        author: "Chị Mai Lan",
        comment: "Rau xà lách và rau muống thu hoạch sớm đọng sương tươi rói, không bị dập nát.",
        rating: 5
      },
      {
        id: "fb-bb-2",
        author: "Bác Tuấn Hưng",
        comment: "Cà chua bi socola ngọt thanh, nấu canh hay ăn sống đều rất ngon.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p1",
        name: "Xà lách mỡ Ba Vì thủy canh",
        category: "Rau củ hữu cơ",
        price: 22000,
        unit: "Cây 300g",
        stock: 50,
        image: "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p2",
        name: "Rau muống Ba Vì hữu cơ non mướt",
        category: "Rau củ hữu cơ",
        price: 18000,
        unit: "Bó 500g",
        stock: 80,
        image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p3",
        name: "Cà chua bi socola giống F1 Đà Lạt",
        category: "Rau củ hữu cơ",
        price: 32000,
        unit: "Hộp 500g",
        stock: 45,
        image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 96
      },
      {
        id: "p4",
        name: "Bí xanh thơm Ba Bể hữu cơ",
        category: "Rau củ hữu cơ",
        price: 25000,
        unit: "Quả ~800g",
        stock: 35,
        image: "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 97,
        aiMatchScore: 95
      }
    ]
  },

  // GIAN HÀNG 2: Nông Trại Xanh Lạc Dương
  "nong trai xanh lac duong": {
    id: "ZM-LD202",
    name: "Nông Trại Xanh Lạc Dương",
    category: "Trái cây & Củ quả cao nguyên",
    address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
    phone: "0968 234 567",
    openHours: "06:30 - 21:30",
    bankName: "Techcombank (TCB)",
    bankAccount: "190345678910",
    accountHolder: "HOANG DINH DUC",
    radiusKm: 10,
    rating: 4.9,
    avgOrderValue: "118.000 đ",
    totalOrders: "1,820",
    lifetimeValue: "82.400.000 đ",
    trendAvgOrder: "+ 4.25%",
    trendTotalOrders: "+ 3.10%",
    trendLifetimeValue: "+ 5.18%",
    chartMonthlyRev: "24.2 tr",
    chartMonthlyOrders: "168 đơn",
    topProducts: [
      {
        id: "tp-ld-1",
        name: "Dâu tây Hana Mộc Châu giống Nhật",
        salesText: "2,100 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 65 hộp",
        image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-ld-2",
        name: "Bơ sáp 034 Lâm Đồng dẻo béo",
        salesText: "1,650 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 90 kg",
        image: "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-ld-3",
        name: "Khoai lang mật Lạc Dương ngọt dẻo",
        salesText: "1,200 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 140 kg",
        image: "https://images.unsplash.com/photo-1596097635121-14b63b7a0c19?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-ld-1",
        name: "Anh Hoàng Minh",
        avatarBadge: "HM",
        phone: "0987 654 321",
        address: "Duy Tân, Cầu Giấy, Hà Nội",
        tag: "Khách VIP • 21 đơn hàng"
      },
      {
        id: "cus-ld-2",
        name: "Chị Thu Thủy",
        avatarBadge: "TT",
        phone: "0936 123 888",
        address: "Trần Thái Tông, Cầu Giấy",
        tag: "Khách VIP • 16 đơn hàng"
      },
      {
        id: "cus-ld-3",
        name: "Anh Quốc Bảo",
        avatarBadge: "QB",
        phone: "0942 555 777",
        address: "Dịch Vọng Hậu, Hà Nội",
        tag: "Khách quen • 11 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-ld-01",
        orderCode: "#3182LD",
        productSummary: "Dâu tây Hana (x1) & Bơ sáp 034 (x1)",
        customerName: "Anh Hoàng Minh",
        customerPhone: "0987 654 321",
        customerAddress: "Số 25 ngõ 15 Duy Tân, Cầu Giấy, Hà Nội",
        orderDate: "Hôm nay, 10:15",
        priceFormatted: "130.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Dâu tây Hana Mộc Châu", qty: 1, price: 85000 },
          { name: "Bơ sáp 034 Lâm Đồng", qty: 1, price: 45000 }
        ]
      },
      {
        id: "ord-ld-02",
        orderCode: "#4921LD",
        productSummary: "Ớt chuông Palermo (x2) & Khoai lang mật (x2)",
        customerName: "Chị Thu Thủy",
        customerPhone: "0936 123 888",
        customerAddress: "Toà PVI Tower, 1 Phạm Văn Bạch, Cầu Giấy",
        orderDate: "Hôm qua, 16:40",
        priceFormatted: "146.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Ớt chuông Sweet Palermo", qty: 2, price: 35000 },
          { name: "Khoai lang mật Lạc Dương", qty: 2, price: 38000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-ld-1",
        author: "Anh Hoàng Minh",
        comment: "Dâu tây Hana thơm nức nở, quả mọng đỏ đều và cực kỳ ngọt!",
        rating: 5
      },
      {
        id: "fb-ld-2",
        author: "Chị Thu Thủy",
        comment: "Bơ sáp 034 dẻo vàng ươm, khoai mật nướng chảy mật thơm lừng.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p5",
        name: "Dâu tây Hana Mộc Châu giống Nhật",
        category: "Trái cây & Củ quả",
        price: 85000,
        unit: "Hộp 500g",
        stock: 65,
        image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p6",
        name: "Bơ sáp 034 Lâm Đồng dẻo béo",
        category: "Trái cây & Củ quả",
        price: 45000,
        unit: "Kg",
        stock: 90,
        image: "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 97
      },
      {
        id: "p7",
        name: "Khoai lang mật Lạc Dương ngọt dẻo",
        category: "Trái cây & Củ quả",
        price: 38000,
        unit: "Túi 1kg",
        stock: 140,
        image: "https://images.unsplash.com/photo-1596097635121-14b63b7a0c19?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p8",
        name: "Ớt chuông Sweet Palermo giòn ngọt",
        category: "Trái cây & Củ quả",
        price: 35000,
        unit: "Khay 500g",
        stock: 60,
        image: "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 97
      }
    ]
  },

  // GIAN HÀNG 3: Vựa Trái Cây Sáu Thảo Miền Tây
  "vua trai cay sau thao mien tay": {
    id: "ZM-ST303",
    name: "Vựa Trái Cây Sáu Thảo Miền Tây",
    category: "Trái cây miệt vườn tươi ngọt",
    address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
    phone: "0972 345 888",
    openHours: "07:00 - 22:00",
    bankName: "MB Bank",
    bankAccount: "0972345888",
    accountHolder: "TRAN THI THAO",
    radiusKm: 10,
    rating: 4.8,
    avgOrderValue: "142.000 đ",
    totalOrders: "1,150",
    lifetimeValue: "74.800.000 đ",
    trendAvgOrder: "+ 2.80%",
    trendTotalOrders: "+ 1.95%",
    trendLifetimeValue: "+ 3.60%",
    chartMonthlyRev: "21.6 tr",
    chartMonthlyOrders: "135 đơn",
    topProducts: [
      {
        id: "tp-st-1",
        name: "Bưởi da xanh Bến Tre ruột hồng",
        salesText: "1,240 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 70 quả",
        image: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-st-2",
        name: "Xoài cát Hòa Lộc Tiền Giang loại 1",
        salesText: "980 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 55 kg",
        image: "https://images.unsplash.com/photo-1553279768-865429fa0078?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-st-3",
        name: "Cam sành Hàm Yên mọng nước",
        salesText: "1,450 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 120 kg",
        image: "https://images.unsplash.com/photo-1547514701-42782101795e?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-st-1",
        name: "Cô Thu Hà",
        avatarBadge: "TH",
        phone: "0903 111 222",
        address: "Nguyễn Khang, Cầu Giấy",
        tag: "Khách VIP • 19 đơn hàng"
      },
      {
        id: "cus-st-2",
        name: "Anh Tiến Dũng",
        avatarBadge: "TD",
        phone: "0988 776 655",
        address: "Trung Kính, Hà Nội",
        tag: "Khách quen • 13 đơn hàng"
      },
      {
        id: "cus-st-3",
        name: "Chị Kim Oanh",
        avatarBadge: "KO",
        phone: "0914 889 900",
        address: "Yên Hòa, Cầu Giấy",
        tag: "Khách quen • 10 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-st-01",
        orderCode: "#6612ST",
        productSummary: "Bưởi da xanh (x2) & Dừa xiêm gọt trọc (x2)",
        customerName: "Cô Thu Hà",
        customerPhone: "0903 111 222",
        customerAddress: "Số 92 đường Trần Thái Tông, Cầu Giấy",
        orderDate: "Hôm nay, 09:40",
        priceFormatted: "166.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Bưởi da xanh Bến Tre ruột hồng", qty: 2, price: 65000 },
          { name: "Dừa xiêm xanh gọt trọc", qty: 2, price: 18000 }
        ]
      },
      {
        id: "ord-st-02",
        orderCode: "#5541ST",
        productSummary: "Xoài cát Hòa Lộc (x2) & Cam sành (x2)",
        customerName: "Anh Tiến Dũng",
        customerPhone: "0988 776 655",
        customerAddress: "Số 104 Xuân Thủy, Cầu Giấy, Hà Nội",
        orderDate: "Hôm qua, 15:30",
        priceFormatted: "220.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Xoài cát Hòa Lộc Tiền Giang", qty: 2, price: 78000 },
          { name: "Cam sành Hàm Yên mọng nước", qty: 2, price: 32000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-st-1",
        author: "Cô Thu Hà",
        comment: "Bưởi da xanh tép mọng nước ngọt thanh, dừa ngọt lịm uống rất đã khát!",
        rating: 5
      },
      {
        id: "fb-st-2",
        author: "Anh Tiến Dũng",
        comment: "Xoài cát thơm lừng, thịt quả vàng ươm không có xơ.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p9",
        name: "Bưởi da xanh Bến Tre ruột hồng",
        category: "Trái cây miệt vườn",
        price: 65000,
        unit: "Quả ~1.3kg",
        stock: 70,
        image: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p10",
        name: "Cam sành Hàm Yên mọng nước",
        category: "Trái cây miệt vườn",
        price: 32000,
        unit: "Kg",
        stock: 120,
        image: "https://images.unsplash.com/photo-1547514701-42782101795e?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 97
      },
      {
        id: "p11",
        name: "Xoài cát Hòa Lộc Tiền Giang loại 1",
        category: "Trái cây miệt vườn",
        price: 78000,
        unit: "Kg",
        stock: 55,
        image: "https://images.unsplash.com/photo-1553279768-865429fa0078?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p12",
        name: "Dừa xiêm xanh gọt trọc Bến Tre",
        category: "Trái cây miệt vườn",
        price: 18000,
        unit: "Trái",
        stock: 90,
        image: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      }
    ]
  },

  // GIAN HÀNG 4: Thực Phẩm Tươi Sống ZoneMart Cầu Giấy
  "thuc pham tuoi song zonemart cau giay": {
    id: "ZM-FRESH404",
    name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
    category: "Thịt cá & Hải sản tươi sống",
    address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
    phone: "0963 888 999",
    openHours: "06:00 - 22:00",
    bankName: "VietinBank",
    bankAccount: "108822334455",
    accountHolder: "ZONEMART FRESH HUB",
    radiusKm: 10,
    rating: 5.0,
    avgOrderValue: "228.000 đ",
    totalOrders: "2,980",
    lifetimeValue: "145.000.000 đ",
    trendAvgOrder: "+ 5.10%",
    trendTotalOrders: "+ 4.35%",
    trendLifetimeValue: "+ 6.20%",
    chartMonthlyRev: "38.5 tr",
    chartMonthlyOrders: "198 đơn",
    topProducts: [
      {
        id: "tp-cg-1",
        name: "Ba chỉ bò Mỹ Black Angus thái lẩu",
        salesText: "2,150 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 65 khay",
        image: "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-cg-2",
        name: "Cá hồi tươi Na Uy fillet chuẩn sashimi",
        salesText: "1,840 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 40 khay",
        image: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-cg-3",
        name: "Gà ta thả vườn Tiên Yên nguyên con",
        salesText: "980 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 25 con",
        image: "https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-cg-1",
        name: "Anh Minh Trí",
        avatarBadge: "MT",
        phone: "0985 667 788",
        address: "Cầu Giấy, Hà Nội",
        tag: "Khách VIP • 25 đơn hàng"
      },
      {
        id: "cus-cg-2",
        name: "Chị Phương Thảo",
        avatarBadge: "PT",
        phone: "0912 998 877",
        address: "Chùa Hà, Cầu Giấy",
        tag: "Khách VIP • 18 đơn hàng"
      },
      {
        id: "cus-cg-3",
        name: "Bác Quang Vinh",
        avatarBadge: "QV",
        phone: "0904 332 211",
        address: "Nguyễn Phong Sắc, Cầu Giấy",
        tag: "Khách quen • 14 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-cg-01",
        orderCode: "#7723CG",
        productSummary: "Ba chỉ bò Mỹ (x2) & Cá hồi Na Uy (x1)",
        customerName: "Anh Minh Trí",
        customerPhone: "0985 667 788",
        customerAddress: "Toà Tháp Đôi Discovery Complex, Cầu Giấy",
        orderDate: "Hôm nay, 11:45",
        priceFormatted: "415.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Ba chỉ bò Mỹ Black Angus thái lẩu", qty: 2, price: 115000 },
          { name: "Cá hồi tươi Na Uy fillet sashimi", qty: 1, price: 185000 }
        ]
      },
      {
        id: "ord-cg-02",
        orderCode: "#8814CG",
        productSummary: "Gà ta thả vườn (x1) & Cánh gà CP (x2)",
        customerName: "Chị Phương Thảo",
        customerPhone: "0912 998 877",
        customerAddress: "Số 56 phố Chùa Hà, Cầu Giấy",
        orderDate: "Hôm qua, 17:10",
        priceFormatted: "285.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Gà ta thả vườn Tiên Yên", qty: 1, price: 175000 },
          { name: "Cánh gà tươi CP cắt khúc", qty: 2, price: 55000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-cg-1",
        author: "Anh Minh Trí",
        comment: "Cá hồi Na Uy phi lê tươi rói ăn sashimi cực béo, bảo quản hộp xốp đá rất cẩn thận!",
        rating: 5
      },
      {
        id: "fb-cg-2",
        author: "Chị Phương Thảo",
        comment: "Thịt bò Mỹ cuộn đều đẹp, gà ta thả vườn thịt chắc thơm ngon.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p13",
        name: "Ba chỉ bò Mỹ Black Angus thái lẩu",
        category: "Thịt cá tươi sống",
        price: 115000,
        unit: "Khay 500g",
        stock: 65,
        image: "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p14",
        name: "Cá hồi tươi Na Uy fillet chuẩn sashimi",
        category: "Thịt cá tươi sống",
        price: 185000,
        unit: "Khay 300g",
        stock: 40,
        image: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p15",
        name: "Gà ta thả vườn Tiên Yên nguyên con",
        category: "Thịt cá tươi sống",
        price: 175000,
        unit: "Con ~1.4kg",
        stock: 25,
        image: "https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 96
      },
      {
        id: "p16",
        name: "Cánh gà tươi CP cắt khúc loại A",
        category: "Thịt cá tươi sống",
        price: 55000,
        unit: "Khay 500g",
        stock: 50,
        image: "https://images.unsplash.com/photo-1527477396000-e27163b481c2?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 97
      }
    ]
  },

  // GIAN HÀNG 5: Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba
  "bep com nieu & am thuc nong co ba": {
    id: "ZM-CB505",
    name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
    category: "Cơm niêu & Món nóng gia đình",
    address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
    phone: "0934 567 890",
    openHours: "09:30 - 21:00",
    bankName: "ACB",
    bankAccount: "246813579",
    accountHolder: "LE THI BA",
    radiusKm: 10,
    rating: 4.7,
    avgOrderValue: "86.000 đ",
    totalOrders: "3,210",
    lifetimeValue: "98.300.000 đ",
    trendAvgOrder: "+ 3.80%",
    trendTotalOrders: "+ 2.90%",
    trendLifetimeValue: "+ 4.50%",
    chartMonthlyRev: "26.4 tr",
    chartMonthlyOrders: "215 đơn",
    topProducts: [
      {
        id: "tp-cb-1",
        name: "Cơm tấm sườn bì chả nướng than hoa",
        salesText: "3,120 Đã bán",
        statusText: "Còn hàng",
        stockText: "Sẵn sàng phục vụ",
        image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-cb-2",
        name: "Cơm đùi gà xối mỡ da giòn sốt mắm tỏi",
        salesText: "2,450 Đã bán",
        statusText: "Còn hàng",
        stockText: "Sẵn sàng phục vụ",
        image: "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-cb-3",
        name: "Set Canh Cua Rau Đay Cà Pháo & Thịt Rang",
        salesText: "1,890 Đã bán",
        statusText: "Còn hàng",
        stockText: "Sẵn sàng phục vụ",
        image: "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-cb-1",
        name: "Bạn Tuấn Anh",
        avatarBadge: "TA",
        phone: "0971 223 344",
        address: "Keangnam, Cầu Giấy",
        tag: "Khách VIP • 32 đơn hàng"
      },
      {
        id: "cus-cb-2",
        name: "Chị Hồng Vân",
        avatarBadge: "HV",
        phone: "0946 778 899",
        address: "Trung Hòa Nhân Chính",
        tag: "Khách VIP • 22 đơn hàng"
      },
      {
        id: "cus-cb-3",
        name: "Anh Thế Bảo",
        avatarBadge: "TB",
        phone: "0989 334 411",
        address: "Nguyễn Chánh, Hà Nội",
        tag: "Khách quen • 15 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-cb-01",
        orderCode: "#1289CB",
        productSummary: "Cơm tấm sườn (x2) & Trà tắc mật ong (x2)",
        customerName: "Bạn Tuấn Anh",
        customerPhone: "0971 223 344",
        customerAddress: "Toà Keangnam Landmark 72, Cầu Giấy",
        orderDate: "Hôm nay, 12:10",
        priceFormatted: "140.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Cơm tấm sườn bì chả nướng than hoa", qty: 2, price: 55000 },
          { name: "Trà tắc mật ong hoa nhãn 500ml", qty: 2, price: 15000 }
        ]
      },
      {
        id: "ord-cb-02",
        orderCode: "#3342CB",
        productSummary: "Cơm đùi gà xối mỡ (x2) & Set Canh Cua (x1)",
        customerName: "Chị Hồng Vân",
        customerPhone: "0946 778 899",
        customerAddress: "Chung cư N05 Hoàng Đạo Thúy, Cầu Giấy",
        orderDate: "Hôm qua, 18:25",
        priceFormatted: "181.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Cơm đùi gà xối mỡ da giòn", qty: 2, price: 58000 },
          { name: "Set Canh Cua Rau Đay Cà Pháo", qty: 1, price: 65000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-cb-1",
        author: "Bạn Tuấn Anh",
        comment: "Cơm tấm miếng sườn to ướp rất đậm vị, giao tới văn phòng vẫn còn bốc khói!",
        rating: 5
      },
      {
        id: "fb-cb-2",
        author: "Chị Hồng Vân",
        comment: "Canh cua mồng tơi chuẩn vị quê nhà, cà pháo giòn rụm rất ngon miệng.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p17",
        name: "Cơm tấm sườn bì chả nướng than hoa",
        category: "Cơm nóng",
        price: 55000,
        unit: "Suất",
        stock: 100,
        image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p18",
        name: "Cơm đùi gà xối mỡ da giòn sốt mắm tỏi",
        category: "Cơm nóng",
        price: 58000,
        unit: "Suất",
        stock: 90,
        image: "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 97
      },
      {
        id: "p19",
        name: "Set Canh Cua Rau Đay Cà Pháo & Thịt Rang",
        category: "Món gia đình",
        price: 65000,
        unit: "Set",
        stock: 50,
        image: "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p20",
        name: "Trà tắc mật ong hoa nhãn mát lạnh 500ml",
        category: "Đồ uống",
        price: 15000,
        unit: "Ly 500ml",
        stock: 120,
        image: "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      }
    ]
  },

  // GIAN HÀNG 6: Tiệm Bánh Mì & Cà Phê Zone Sáng
  "tiem banh mi & ca phe zone sang": {
    id: "ZM-ZS606",
    name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
    category: "Bánh mì, Điểm tâm & Cà phê",
    address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
    phone: "0918 223 344",
    openHours: "05:30 - 18:00",
    bankName: "VPBank",
    bankAccount: "1988223344",
    accountHolder: "PHAN MINH DUC",
    radiusKm: 10,
    rating: 4.85,
    avgOrderValue: "46.000 đ",
    totalOrders: "4,120",
    lifetimeValue: "58.700.000 đ",
    trendAvgOrder: "+ 2.10%",
    trendTotalOrders: "+ 5.20%",
    trendLifetimeValue: "+ 3.80%",
    chartMonthlyRev: "17.8 tr",
    chartMonthlyOrders: "285 đơn",
    topProducts: [
      {
        id: "tp-zs-1",
        name: "Bánh mì chảo đặc biệt Zone Sáng",
        salesText: "3,420 Đã bán",
        statusText: "Còn hàng",
        stockText: "Sẵn sàng phục vụ",
        image: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-zs-2",
        name: "Bánh mì kẹp thịt nướng sả bơ",
        salesText: "2,810 Đã bán",
        statusText: "Còn hàng",
        stockText: "Sẵn sàng phục vụ",
        image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-zs-3",
        name: "Cà phê Robusta Buôn Ma Thuột pha phin",
        salesText: "1,950 Đã bán",
        statusText: "Còn hàng",
        stockText: "Sẵn sàng phục vụ",
        image: "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-zs-1",
        name: "Sinh viên Nam Khánh",
        avatarBadge: "NK",
        phone: "0962 114 455",
        address: "ĐH Sư Phạm Hà Nội",
        tag: "Khách VIP • 28 đơn hàng"
      },
      {
        id: "cus-zs-2",
        name: "Chị Thanh Mai",
        avatarBadge: "TM",
        phone: "0945 882 233",
        address: "Tòa HITC Xuân Thủy",
        tag: "Khách VIP • 19 đơn hàng"
      },
      {
        id: "cus-zs-3",
        name: "Bạn Quỳnh Trang",
        avatarBadge: "QT",
        phone: "0973 445 566",
        address: "Dịch Vọng Hậu, Cầu Giấy",
        tag: "Khách quen • 12 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-zs-01",
        orderCode: "#7741ZS",
        productSummary: "Bánh mì chảo đặc biệt (x1) & Cà phê pha phin (x1)",
        customerName: "Sinh viên Nam Khánh",
        customerPhone: "0962 114 455",
        customerAddress: "Ký túc xá ĐH Sư Phạm Hà Nội, 136 Xuân Thủy",
        orderDate: "Hôm nay, 07:30",
        priceFormatted: "64.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Bánh mì chảo đặc biệt Zone Sáng", qty: 1, price: 42000 },
          { name: "Cà phê Robusta Buôn Ma Thuột pha phin", qty: 1, price: 22000 }
        ]
      },
      {
        id: "ord-zs-02",
        orderCode: "#8892ZS",
        productSummary: "Bánh mì kẹp thịt nướng (x2) & Bánh bao xá xíu (x2)",
        customerName: "Chị Thanh Mai",
        customerPhone: "0945 882 233",
        customerAddress: "Tầng 6, Toà nhà HITC, 239 Xuân Thủy",
        orderDate: "Hôm qua, 08:15",
        priceFormatted: "92.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Bánh mì kẹp thịt nướng sả bơ", qty: 2, price: 28000 },
          { name: "Bánh bao thượng hạng nhân xá xíu", qty: 2, price: 18000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-zs-1",
        author: "Sinh viên Nam Khánh",
        comment: "Bánh mì giòn rụm nóng hổi, pate nhà làm ngậy thơm, cà phê sáng giao siêu tốc 10 phút!",
        rating: 5
      },
      {
        id: "fb-zs-2",
        author: "Chị Thanh Mai",
        comment: "Bánh bao nhân thịt xá xíu cắn ngập miệng, cà phê đậm đà tỉnh táo cả ngày làm việc.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p21",
        name: "Bánh mì chảo đặc biệt Zone Sáng",
        category: "Bánh mì & Điểm tâm",
        price: 42000,
        unit: "Phần",
        stock: 80,
        image: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p22",
        name: "Bánh mì kẹp thịt nướng sả bơ thơm lừng",
        category: "Bánh mì & Điểm tâm",
        price: 28000,
        unit: "Ổ",
        stock: 100,
        image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p23",
        name: "Cà phê Robusta Buôn Ma Thuột pha phin",
        category: "Cà phê & Đồ uống",
        price: 22000,
        unit: "Ly 350ml",
        stock: 150,
        image: "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 97
      },
      {
        id: "p24",
        name: "Bánh bao thượng hạng nhân thịt nấm xá xíu",
        category: "Bánh mì & Điểm tâm",
        price: 18000,
        unit: "Cái",
        stock: 60,
        image: "https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      }
    ]
  },

  // GIAN HÀNG 7: Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc
  "hop tac xa dac san vung cao tay bac": {
    id: "ZM-TB707",
    name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
    category: "Đặc sản vùng cao & Nông sản khô",
    address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
    phone: "0987 112 233",
    openHours: "07:30 - 21:30",
    bankName: "Agribank",
    bankAccount: "146020588990",
    accountHolder: "LO VAN TIEN",
    radiusKm: 10,
    rating: 4.9,
    avgOrderValue: "185.000 đ",
    totalOrders: "940",
    lifetimeValue: "52.100.000 đ",
    trendAvgOrder: "+ 3.40%",
    trendTotalOrders: "+ 2.15%",
    trendLifetimeValue: "+ 4.05%",
    chartMonthlyRev: "15.6 tr",
    chartMonthlyOrders: "88 đơn",
    topProducts: [
      {
        id: "tp-tb-1",
        name: "Thịt trâu gác bếp chuẩn vị Tây Bắc",
        salesText: "650 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 35 gói",
        image: "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-tb-2",
        name: "Gạo ST25 Ông Cua chính hãng Sóc Trăng",
        salesText: "1,420 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 80 bao",
        image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-tb-3",
        name: "Mật ong hoa cà phê nguyên chất Tây Bắc",
        salesText: "890 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 45 chai",
        image: "https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-tb-1",
        name: "Anh Trọng Hiếu",
        avatarBadge: "TH",
        phone: "0913 445 566",
        address: "Lê Đức Thọ, Mỹ Đình",
        tag: "Khách VIP • 17 đơn hàng"
      },
      {
        id: "cus-tb-2",
        name: "Bác Ngọc Lan",
        avatarBadge: "NL",
        phone: "0982 778 899",
        address: "Vinhomes Gardenia Mỹ Đình",
        tag: "Khách VIP • 14 đơn hàng"
      },
      {
        id: "cus-tb-3",
        name: "Chị Thu Trang",
        avatarBadge: "TT",
        phone: "0968 334 455",
        address: "Mỹ Đình 2, Nam Từ Liêm",
        tag: "Khách quen • 9 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-tb-01",
        orderCode: "#9931TB",
        productSummary: "Thịt trâu gác bếp (x1) & Nấm hương Sa Pa (x1)",
        customerName: "Anh Trọng Hiếu",
        customerPhone: "0913 445 566",
        customerAddress: "Chung cư FLC Landmark, Lê Đức Thọ, Nam Từ Liêm",
        orderDate: "Hôm nay, 10:50",
        priceFormatted: "295.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Thịt trâu gác bếp chuẩn Tây Bắc", qty: 1, price: 210000 },
          { name: "Nấm hương khô rừng Sa Pa", qty: 1, price: 85000 }
        ]
      },
      {
        id: "ord-tb-02",
        orderCode: "#1128TB",
        productSummary: "Gạo ST25 túi 5kg (x1) & Mật ong hoa cà phê (x1)",
        customerName: "Bác Ngọc Lan",
        customerPhone: "0982 778 899",
        customerAddress: "Khu đô thị Vinhomes Gardenia, Hàm Nghi",
        orderDate: "Hôm qua, 14:00",
        priceFormatted: "280.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Gạo ST25 Ông Cua túi 5kg", qty: 1, price: 185000 },
          { name: "Mật ong hoa cà phê nguyên chất", qty: 1, price: 95000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-tb-1",
        author: "Anh Trọng Hiếu",
        comment: "Thịt trâu gác bếp chuẩn vị Tây Bắc dậy mùi mắc khén hạt dổi, xé sợi chấm chẩm chéo cực đỉnh!",
        rating: 5
      },
      {
        id: "fb-tb-2",
        author: "Bác Ngọc Lan",
        comment: "Gạo ST25 chuẩn cơm dẻo thơm hạt dài, nấm hương rừng Sa Pa thơm ngào ngạt.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p25",
        name: "Mật ong hoa cà phê nguyên chất Tây Bắc",
        category: "Đặc sản khô",
        price: 95000,
        unit: "Chai 500ml",
        stock: 45,
        image: "https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p26",
        name: "Gạo ST25 Ông Cua chính hãng Sóc Trăng",
        category: "Lương thực",
        price: 185000,
        unit: "Túi 5kg",
        stock: 80,
        image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p27",
        name: "Nấm hương khô rừng Sa Pa thượng hạng",
        category: "Đặc sản khô",
        price: 85000,
        unit: "Hộp 200g",
        stock: 50,
        image: "https://images.unsplash.com/photo-1509358271058-acd22cc93898?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 97
      },
      {
        id: "p28",
        name: "Thịt trâu gác bếp chuẩn vị Tây Bắc",
        category: "Đặc sản khô",
        price: 210000,
        unit: "Gói 250g",
        stock: 35,
        image: "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      }
    ]
  },

  // GIAN HÀNG 8: Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình
  "tong kho do gia dung & tieu dung my dinh": {
    id: "ZM-GD808",
    name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
    category: "Đồ gia dụng & Tiêu dùng tiện ích",
    address: "102 Hàm Nghi, Cầu Diễn, Nam Từ Liêm, Hà Nội",
    phone: "0976 554 433",
    openHours: "08:00 - 22:30",
    bankName: "BIDV",
    bankAccount: "21510001234567",
    accountHolder: "VU MANH CUONG",
    radiusKm: 10,
    rating: 4.75,
    avgOrderValue: "198.000 đ",
    totalOrders: "1,340",
    lifetimeValue: "88.500.000 đ",
    trendAvgOrder: "+ 4.60%",
    trendTotalOrders: "+ 3.70%",
    trendLifetimeValue: "+ 4.90%",
    chartMonthlyRev: "22.4 tr",
    chartMonthlyOrders: "120 đơn",
    topProducts: [
      {
        id: "tp-gd-1",
        name: "Bộ nồi inox 5 đáy cao cấp chống dính",
        salesText: "540 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 28 bộ",
        image: "https://images.unsplash.com/photo-1584990347449-3079b765b214?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-gd-2",
        name: "Chảo chống dính đá Ceramic cao cấp",
        salesText: "820 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 45 chiếc",
        image: "https://images.unsplash.com/photo-1583778176476-4a8b02a64c01?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: "tp-gd-3",
        name: "Nước rửa bát hữu cơ hương quế gừng",
        salesText: "1,150 Đã bán",
        statusText: "Còn hàng",
        stockText: "Còn 90 can",
        image: "https://images.unsplash.com/photo-1585670210693-e7fdd16b142e?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: "cus-gd-1",
        name: "Chị Thu Hương",
        avatarBadge: "TH",
        phone: "0904 889 900",
        address: "The Manor Mỹ Đình",
        tag: "Khách VIP • 16 đơn hàng"
      },
      {
        id: "cus-gd-2",
        name: "Anh Việt Hùng",
        avatarBadge: "VH",
        phone: "0915 667 788",
        address: "Hàm Nghi, Nam Từ Liêm",
        tag: "Khách quen • 12 đơn hàng"
      },
      {
        id: "cus-gd-3",
        name: "Chị Mai Phương",
        avatarBadge: "MP",
        phone: "0983 221 144",
        address: "Vinaconex 7, Cầu Diễn",
        tag: "Khách quen • 10 đơn hàng"
      }
    ],
    orders: [
      {
        id: "ord-gd-01",
        orderCode: "#2234GD",
        productSummary: "Chảo đá Ceramic (x1) & Nước rửa bát hữu cơ (x1)",
        customerName: "Chị Thu Hương",
        customerPhone: "0904 889 900",
        customerAddress: "Khu đô thị The Manor, Mỹ Đình 1, Nam Từ Liêm",
        orderDate: "Hôm nay, 11:05",
        priceFormatted: "310.000 đ",
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: "Chảo chống dính đá Ceramic", qty: 1, price: 245000 },
          { name: "Nước rửa bát hữu cơ thảo mộc", qty: 1, price: 65000 }
        ]
      },
      {
        id: "ord-gd-02",
        orderCode: "#5567GD",
        productSummary: "Bộ nồi inox 5 đáy (x1) & Túi rác sinh học (x2)",
        customerName: "Anh Việt Hùng",
        customerPhone: "0915 667 788",
        customerAddress: "Số 88 Hàm Nghi, Cầu Diễn, Nam Từ Liêm",
        orderDate: "Hôm qua, 15:20",
        priceFormatted: "650.000 đ",
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Bộ nồi inox 5 đáy cao cấp", qty: 1, price: 580000 },
          { name: "Túi đựng rác sinh học (1kg)", qty: 2, price: 35000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: "fb-gd-1",
        author: "Chị Thu Hương",
        comment: "Chảo đá ceramic chiên trứng không dính tí nào, dùng từ bắt nhiệt cực nhanh!",
        rating: 5
      },
      {
        id: "fb-gd-2",
        author: "Anh Việt Hùng",
        comment: "Bộ nồi inox dày dặn sáng bóng, nước rửa bát quế gừng rửa sạch thơm không hại da tay.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: "p29",
        name: "Bộ nồi inox 5 đáy cao cấp chống dính",
        category: "Gia dụng nhà bếp",
        price: 580000,
        unit: "Bộ 3 nồi",
        stock: 28,
        image: "https://images.unsplash.com/photo-1584990347449-3079b765b214?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 100,
        aiMatchScore: 99
      },
      {
        id: "p30",
        name: "Nước rửa bát hữu cơ hương quế gừng can 2L",
        category: "Chất tẩy rửa hữu cơ",
        price: 65000,
        unit: "Can 2 lít",
        stock: 90,
        image: "https://images.unsplash.com/photo-1585670210693-e7fdd16b142e?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: "p31",
        name: "Chảo chống dính đá Ceramic cao cấp 26cm",
        category: "Gia dụng nhà bếp",
        price: 245000,
        unit: "Chiếc",
        stock: 45,
        image: "https://images.unsplash.com/photo-1583778176476-4a8b02a64c01?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 97
      },
      {
        id: "p32",
        name: "Túi đựng rác sinh học tự phân hủy cuộn 1kg",
        category: "Tiêu dùng tiện ích",
        price: 35000,
        unit: "Cuộn 1kg",
        stock: 150,
        image: "https://images.unsplash.com/photo-1610557892470-55d9e80c0bce?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      }
    ]
  }
};

/**
 * Chuẩn hóa chuỗi tìm kiếm khóa gian hàng
 */
function normalizeKey(str: string): string {
  return (str || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .replace(/[^a-z0-9]/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

/**
 * Hàm sinh dữ liệu ngẫu nhiên nhưng có tính xác định (deterministic) dựa trên chuỗi tên gian hàng
 */
function generateDynamicProfile(rawName: string, _email: string, ownerName?: string): SellerStoreProfile {
  const norm = normalizeKey(rawName || "Gian Hàng Địa Phương");
  let seed = 0;
  for (let i = 0; i < norm.length; i++) {
    seed = (seed * 31 + norm.charCodeAt(i)) >>> 0;
  }

  const cleanStoreName = rawName.split("(")[0].trim() || "Gian Hàng Tiêu Biểu";
  const cleanOwner = ownerName || cleanStoreName;
  const storeId = `ZM-DYN${(seed % 900) + 100}`;

  const avgValue = 50000 + (seed % 170) * 1000;
  const ordersCount = 650 + (seed % 3200);
  const revenueTr = 25 + (seed % 115);
  const monthlyTr = 12 + (seed % 25);
  const monthlyOrders = 80 + (seed % 180);

  return {
    id: storeId,
    name: cleanStoreName,
    category: "Nông sản & Bách hóa sạch địa phương",
    address: `Số ${(seed % 150) + 1} đường Cầu Giấy, Phường Dịch Vọng, Cầu Giấy, Hà Nội`,
    phone: `09${(seed % 80 + 10)} ${(seed % 900 + 100)} ${(seed % 900 + 100)}`,
    openHours: "07:00 - 21:30",
    bankName: "Vietcombank (VCB)",
    bankAccount: `${seed % 9000000000 + 1000000000}`,
    accountHolder: cleanOwner.toUpperCase(),
    radiusKm: 10,
    rating: Number((4.6 + ((seed % 4) * 0.1)).toFixed(1)),
    avgOrderValue: `${avgValue.toLocaleString("vi-VN")} đ`,
    totalOrders: ordersCount.toLocaleString("en-US"),
    lifetimeValue: `${revenueTr.toLocaleString("vi-VN")}.000.000 đ`,
    trendAvgOrder: `+ ${(2.5 + ((seed % 30) / 10)).toFixed(2)}%`,
    trendTotalOrders: `+ ${(1.8 + ((seed % 25) / 10)).toFixed(2)}%`,
    trendLifetimeValue: `+ ${(3.2 + ((seed % 35) / 10)).toFixed(2)}%`,
    chartMonthlyRev: `${monthlyTr} tr`,
    chartMonthlyOrders: `${monthlyOrders} đơn`,
    topProducts: [
      {
        id: `tp-dyn-${seed}-1`,
        name: `Combo Nông Sản Chọn Lọc (${cleanStoreName})`,
        salesText: `${(seed % 1500 + 500).toLocaleString("en-US")} Đã bán`,
        statusText: "Còn hàng",
        stockText: `Còn ${(seed % 80 + 30)} tồn kho`,
        image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: `tp-dyn-${seed}-2`,
        name: `Trái Cây Tươi Đóng Hộp Đặc Sản`,
        salesText: `${(seed % 1200 + 400).toLocaleString("en-US")} Đã bán`,
        statusText: "Còn hàng",
        stockText: `Còn ${(seed % 50 + 20)} tồn kho`,
        image: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=400&q=80"
      },
      {
        id: `tp-dyn-${seed}-3`,
        name: `Đặc Sản Vùng Miền Đóng Túi Hút Chân Không`,
        salesText: `${(seed % 800 + 250).toLocaleString("en-US")} Đã bán`,
        statusText: "Còn hàng",
        stockText: `Còn ${(seed % 90 + 40)} tồn kho`,
        image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80"
      }
    ],
    customers: [
      {
        id: `cus-dyn-${seed}-1`,
        name: "Chị Phương Mai",
        avatarBadge: "PM",
        phone: "0918 345 678",
        address: "Cầu Giấy, Hà Nội",
        tag: `Khách VIP • ${(seed % 15 + 12)} đơn hàng`
      },
      {
        id: `cus-dyn-${seed}-2`,
        name: "Anh Tuấn Hùng",
        avatarBadge: "TH",
        phone: "0982 554 433",
        address: "Xuân Thủy, Cầu Giấy",
        tag: `Khách quen • ${(seed % 10 + 8)} đơn hàng`
      },
      {
        id: `cus-dyn-${seed}-3`,
        name: "Cô Thanh Nhàn",
        avatarBadge: "TN",
        phone: "0903 887 766",
        address: "Dịch Vọng, Hà Nội",
        tag: `Khách quen • ${(seed % 8 + 5)} đơn hàng`
      }
    ],
    orders: [
      {
        id: `ord-dyn-${seed}-01`,
        orderCode: `#${(seed % 8000 + 1000)}ZM`,
        productSummary: `Combo Nông Sản (${cleanStoreName})`,
        customerName: "Chị Phương Mai",
        customerPhone: "0918 345 678",
        customerAddress: "Số 18 ngõ 20 Hồ Tùng Mậu, Cầu Giấy",
        orderDate: "Hôm nay, 10:15",
        priceFormatted: `${(avgValue + 15000).toLocaleString("vi-VN")} đ`,
        paymentMethod: "Chuyển khoản QR",
        status: "Đang xử lý",
        items: [
          { name: `Combo Nông Sản Chọn Lọc (${cleanStoreName})`, qty: 1, price: avgValue + 15000 }
        ]
      },
      {
        id: `ord-dyn-${seed}-02`,
        orderCode: `#${(seed % 8000 + 1001)}ZM`,
        productSummary: `Trái Cây Tươi Đóng Hộp Đặc Sản`,
        customerName: "Anh Tuấn Hùng",
        customerPhone: "0982 554 433",
        customerAddress: "Số 45 Trần Thái Tông, Cầu Giấy, Hà Nội",
        orderDate: "Hôm qua, 15:40",
        priceFormatted: `${(avgValue + 35000).toLocaleString("vi-VN")} đ`,
        paymentMethod: "Thẻ ngân hàng",
        status: "Hoàn thành",
        items: [
          { name: "Trái Cây Tươi Đóng Hộp Đặc Sản", qty: 1, price: avgValue + 35000 }
        ]
      }
    ],
    feedbacks: [
      {
        id: `fb-dyn-${seed}-1`,
        author: "Chị Phương Mai",
        comment: `Gian hàng ${cleanStoreName} phục vụ rất chu đáo, đóng gói sạch sẽ và giao hàng hỏa tốc đúng cam kết!`,
        rating: 5
      },
      {
        id: `fb-dyn-${seed}-2`,
        author: "Anh Tuấn Hùng",
        comment: "Sản phẩm chất lượng tươi ngon, sẽ tiếp tục ủng hộ tiệm thường xuyên.",
        rating: 5
      }
    ],
    starterProducts: [
      {
        id: `p-dyn-${seed}-1`,
        name: `Combo Nông Sản Chọn Lọc (${cleanStoreName})`,
        category: "Nông sản sạch",
        price: avgValue,
        unit: "Phần",
        stock: 50,
        image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 99,
        aiMatchScore: 98
      },
      {
        id: `p-dyn-${seed}-2`,
        name: `Trái Cây Tươi Đóng Hộp Đặc Sản`,
        category: "Trái cây",
        price: avgValue + 20000,
        unit: "Hộp 1kg",
        stock: 35,
        image: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=400&q=80",
        aiSafetyScore: 98,
        aiMatchScore: 97
      }
    ]
  };
}

/**
 * Tra cứu hồ sơ dữ liệu hoàn chỉnh của một gian hàng bất kỳ
 */
export function getStoreProfile(storeName: string, email: string, ownerName?: string): SellerStoreProfile {
  const normName = normalizeKey(storeName);
  const normEmail = normalizeKey(email);

  // 1. Kiểm tra khớp chính xác hoặc khớp từ khóa trong danh sách 9 gian hàng tiêu biểu
  for (const [key, profile] of Object.entries(KNOWN_STORE_PROFILES)) {
    if (normName === key || normName.includes(key) || key.includes(normName)) {
      return JSON.parse(JSON.stringify(profile));
    }
  }

  // 2. Kiểm tra nếu email hoặc tên có từ khóa 'test'
  if (normName.includes("test") || normEmail.includes("test") || normEmail.includes("baconbao")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["test gian hang"]));
  }

  // 3. Kiểm tra các ngành nghề phổ biến
  if (normName.includes("banh mi") || normName.includes("ca phe") || normName.includes("bakery")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["tiem banh mi & ca phe zone sang"]));
  }
  if (normName.includes("trai cay") || normName.includes("hoa qua")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["vua trai cay sau thao mien tay"]));
  }
  if (normName.includes("thit") || normName.includes("ca hoi") || normName.includes("bo my")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["thuc pham tuoi song zonemart cau giay"]));
  }
  if (normName.includes("com nieu") || normName.includes("quan com") || normName.includes("bep")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["bep com nieu & am thuc nong co ba"]));
  }
  if (normName.includes("dac san") || normName.includes("tay bac") || normName.includes("gao")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["hop tac xa dac san vung cao tay bac"]));
  }
  if (normName.includes("gia dung") || normName.includes("noi inox") || normName.includes("chao")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["tong kho do gia dung & tieu dung my dinh"]));
  }
  if (normName.includes("rau") || normName.includes("vietgap")) {
    return JSON.parse(JSON.stringify(KNOWN_STORE_PROFILES["vuon rau huu co bac ba"]));
  }

  // 4. Sinh dữ liệu xác định (deterministic) cho mọi tên gian hàng mới khác
  return generateDynamicProfile(storeName, email, ownerName);
}

