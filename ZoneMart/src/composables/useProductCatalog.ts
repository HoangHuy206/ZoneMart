import { computed } from "vue";
import { useProductModeration } from "./useProductModeration";

export interface ProductReview {
  id: string;
  author: string;
  avatar: string;
  rating: number;
  date: string;
  comment: string;
  photos?: string[];
  helpfulCount: number;
  verified: boolean;
}

export interface CatalogProduct {
  id: string;
  name: string;
  category: "food" | "veggie" | "fastfood" | "beverage" | "household";
  categoryName: string;
  price: number;
  oldPrice?: number;
  discountBadge?: string;
  unit: string;
  image: string;
  gallery: string[];
  badge?: string;
  certification?: {
    type: "VietGAP" | "OCOP 4 Sao" | "Organic" | "ATTP Quốc Gia";
    certNo: string;
    issuedBy: string;
    issuedDate: string;
    expiryDate: string;
  };
  store: {
    id: string;
    name: string;
    address: string;
    distanceKm: number;
    deliveryTime: string;
    rating: number;
    totalProducts: number;
    isVerified: boolean;
  };
  rating: number;
  reviewsCount: number;
  sold: number;
  stock: number;
  description: string;
  highlights: string[];
  specs: {
    origin: string;
    brand: string;
    weight: string;
    shelfLife: string;
    storage: string;
    packingStandard: string;
  };
  nutrition: {
    servingSize: string;
    calories: string;
    protein: string;
    fat: string;
    carbs: string;
    minerals: string;
  };
  cookingTips: string[];
  reviews: ProductReview[];
  aiAudit?: {
    safetyScore: number;
    matchScore: number;
    verificationDate: string;
    inspector: string;
    notes: string;
  };
}

export interface CatalogStore {
  id: string;
  name: string;
  address: string;
  distanceKm: number;
  deliveryTime: string;
  rating: number;
  reviewsCount?: number;
  totalProducts: number;
  isVerified: boolean;
  categoryName?: string;
  avatar: string;
  coverImage: string;
  openHours?: string;
  products: CatalogProduct[];
}

export const CATALOG_PRODUCTS: CatalogProduct[] = [
  {
    id: "p1",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 185000,
    oldPrice: 220000,
    discountBadge: "-16%",
    unit: "Khay 500g",
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1558030006-450675393462?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Bán chạy nhất",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "USDA-VN-88429/2026",
      issuedBy: "Cục Thú Y & Kiểm Dịch Quốc Tế",
      issuedDate: "15/01/2026",
      expiryDate: "15/01/2027"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      totalProducts: 240,
      isVerified: true
    },
    rating: 5.0,
    reviewsCount: 84,
    sold: 142,
    stock: 45,
    description:
      "Thịt bò Mỹ Black Angus nhập khẩu chính ngạch từ Nebraska (Hoa Kỳ), đóng khay chân không tiêu chuẩn công nghệ MAP hiện đại. Phần thịt có vân mỡ cẩm thạch (marbling) xen kẽ đồng đều, mang lại độ mềm mọng tự nhiên và vị ngọt đậm đà đặc trưng khi áp chảo hay nướng lẩu.",
    highlights: [
      "100% Thịt bò Black Angus đạt chuẩn phân hạng USDA Choice",
      "Bảo quản dây chuyền lạnh chuẩn -2°C đến 2°C từ kho đến tận cửa",
      "Đóng khay hút chân không 500g tiện lợi, sạch sẽ, đạt chuẩn ATTP",
      "Giao hỏa tốc bằng thùng bảo ôn chuyên dụng trong 20 phút"
    ],
    specs: {
      origin: "Hoa Kỳ (Nebraska Farms)",
      brand: "ZoneMart Fresh Selection",
      weight: "500g (± 20g)",
      shelfLife: "7 ngày ở ngăn mát (0-4°C), 6 tháng ở ngăn đông (-18°C)",
      storage: "Bảo quản ở nhiệt độ từ 0°C đến 4°C trong ngăn mát tủ lạnh",
      packingStandard: "Khay sinh học kháng khuẩn hút chân không tiêu chuẩn MAP"
    },
    nutrition: {
      servingSize: "100g thịt nạc",
      calories: "217 kcal",
      protein: "26.1 g",
      fat: "11.8 g (giàu Omega-3 & CLA)",
      carbs: "0 g",
      minerals: "Sắt (2.6mg), Kẽm (5.3mg), Vitamin B12 (2.4mcg)"
    },
    cookingTips: [
      "Rã đông tự nhiên trong ngăn mát tủ lạnh từ 4-6 tiếng để giữ trọn vẹn nước ngọt tự nhiên.",
      "Ướp với chút muối hồng Himalaya, tiêu đen xay vỡ và lá hương thảo trong 10 phút trước khi chế biến.",
      "Áp chảo lửa lớn 2-3 phút mỗi mặt để đạt độ chín vừa (Medium-rare) mềm tan trên đầu lưỡi."
    ],
    reviews: [
      {
        id: "rv1",
        author: "Nguyễn Hoàng Nam",
        avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "Hôm qua lúc 18:42",
        comment: "Thịt cực kỳ tươi, màu đỏ hồng tự nhiên không hề bị thâm hay chảy nước. Ship từ Cầu Giấy qua Nguyễn Phong Sắc chưa tới 15 phút, giao đến tay khay thịt vẫn còn lạnh buốt. Tối làm món bò bít tết cả nhà ai cũng khen mềm ngon!",
        photos: [
          "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=300&q=80"
        ],
        helpfulCount: 18,
        verified: true
      },
      {
        id: "rv2",
        author: "Trần Minh Thảo",
        avatar: "https://images.unsplash.com/photo-1517841905240-472988babdf9?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "2 ngày trước",
        comment: "Khay 500g đầy đặn, vân mỡ rất đẹp. Mình cuốn nấm kim châm ăn lẩu thì bá cháy luôn. Đóng gói rất xịn sò, có cả nhãn kiểm định AI.",
        helpfulCount: 9,
        verified: true
      }
    ],
    aiAudit: {
      safetyScore: 99,
      matchScore: 98,
      verificationDate: "11/09/2026 - 08:30",
      inspector: "ZoneMart Vision Guard AI v4.2",
      notes: "Sản phẩm đạt độ tươi 100%, phổ màu thịt đỏ tươi tự nhiên, vân mỡ đạt chuẩn phân hạng USDA. Không phát hiện chất bảo quản hay tạp chất."
    }
  },
  {
    id: "p2",
    name: "Hộp Dâu Tây Đà Lạt Tươi Ngọt Chuẩn VietGAP",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 95000,
    oldPrice: 125000,
    discountBadge: "-24%",
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1518635017480-d471b404ab6e?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1543158266-0066955047b1?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1587393855524-087f83d95bc9?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Mới hái sáng nay",
    certification: {
      type: "VietGAP",
      certNo: "VG-LD-2026-0914",
      issuedBy: "Trung Tâm Chất Lượng Nông Lâm Thủy Sản Lâm Đồng",
      issuedDate: "02/02/2026",
      expiryDate: "02/02/2027"
    },
    store: {
      id: "store_sieu_thi_trai_cay_xanh",
      name: "Siêu Thị Trái Cây Xanh",
      address: "112 Trần Thái Tông, Cầu Giấy, Hà Nội",
      distanceKm: 2.5,
      deliveryTime: "20 - 25 phút",
      rating: 4.9,
      totalProducts: 180,
      isVerified: true
    },
    rating: 4.9,
    reviewsCount: 128,
    sold: 218,
    stock: 60,
    description:
      "Dâu tây giống Hana Nhật Bản trồng theo mô hình nhà kính thủy canh hữu cơ tại vùng đồi Lạc Dương (Đà Lạt). Từng quả dâu được hái thủ công vào sáng sớm, cuống xanh tươi mơn mởn, trái chín đỏ mọng, vị ngọt thanh và thơm nức đặc trưng.",
    highlights: [
      "Giống Hana Nhật Bản thơm ngọt, không bị chua gắt",
      "Canh tác hoàn toàn không thuốc trừ sâu hóa học, đạt chuẩn VietGAP",
      "Vận chuyển hàng không hàng ngày về Hà Nội giữ trọn phấn dâu",
      "Từng quả được bọc xốp chống va đập, bảo quản lạnh tối ưu"
    ],
    specs: {
      origin: "Lạc Dương, TP. Đà Lạt, Lâm Đồng",
      brand: "Nông Trại Dâu Xanh Đà Lạt",
      weight: "500g (Hộp khoảng 24-28 quả)",
      shelfLife: "3-5 ngày trong ngăn mát tủ lạnh",
      storage: "Không rửa trước khi cho vào tủ lạnh, giữ cuống khô ráo",
      packingStandard: "Hộp nhựa PET chuyên dụng có lỗ thoáng khí chống đọng sương"
    },
    nutrition: {
      servingSize: "100g dâu tây tươi",
      calories: "32 kcal",
      protein: "0.7 g",
      fat: "0.3 g",
      carbs: "7.7 g (chỉ số GI thấp)",
      minerals: "Vitamin C (58.8mg - 98% DV), Folate, Kali, Mangan"
    },
    cookingTips: [
      "Rửa nhẹ nhàng dưới vòi nước chảy ngay trước khi ăn, ngâm nước muối loãng 3 phút.",
      "Thưởng thức trực tiếp để cảm nhận trọn vị ngọt thơm hoặc kết hợp sữa chua Hy Lạp buổi sáng.",
      "Xay sinh tố dâu kèm hạt chia bổ sung năng lượng tươi lành cả ngày."
    ],
    reviews: [
      {
        id: "rv-d1",
        author: "Vũ Phương Mai",
        avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "Sáng nay lúc 09:15",
        comment: "Dâu to, đỏ đều và rất thơm. Không có quả nào bị dập cả. Con gái mình thích mê, ăn hết nửa hộp trong một nốt nhạc!",
        helpfulCount: 14,
        verified: true
      }
    ],
    aiAudit: {
      safetyScore: 99,
      matchScore: 99,
      verificationDate: "11/09/2026 - 06:15",
      inspector: "ZoneMart Vision Guard AI v4.2",
      notes: "Tỷ lệ quả nguyên vẹn đạt 100%, cuống tươi xanh, không phát hiện vết dập rách."
    }
  },
  {
    id: "p3",
    name: "Combo Bánh Mì Chảo Nóng Hổi Kèm Pate & Xúc Xích",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 45000,
    oldPrice: 55000,
    discountBadge: "-18%",
    unit: "Phần 1 người",
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1586190848861-99aa4a171e90?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Giao nóng giòn",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "ATTP-HN-33290",
      issuedBy: "Chi Cục An Toàn Vệ Sinh Thực Phẩm Hà Nội",
      issuedDate: "10/11/2025",
      expiryDate: "10/11/2026"
    },
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì Zone",
      address: "18 Xuân Thủy, Cầu Giấy, Hà Nội",
      distanceKm: 2.8,
      deliveryTime: "15 - 20 phút",
      rating: 4.8,
      totalProducts: 45,
      isVerified: true
    },
    rating: 4.8,
    reviewsCount: 210,
    sold: 340,
    stock: 50,
    description:
      "Chảo bánh mì nóng hổi gồm trứng ốp la lòng đào béo ngậy, pate gan Hải Phòng tự làm thơm lừng béo ngậy, xúc xích nướng giòn rụm, thịt băm viên sốt cà chua đậm đà kèm bánh mì nóng giòn tan và dưa leo ngâm chua ngọt giải ngấy.",
    highlights: [
      "Pate gan nhà làm theo công thức truyền thống, thơm bùi không tanh",
      "Bánh mì được nướng nóng giòn ngay trước khi tài xế nhận đơn",
      "Đựng trong hộp giấy giữ nhiệt chuyên dụng giữ độ nóng suốt 30 phút",
      "Kèm đầy đủ sốt tương ớt Chin-su và nước tương đậm đà"
    ],
    specs: {
      origin: "Chế biến tươi tại Bếp Tiệm Bánh Mì Zone Cầu Giấy",
      brand: "Bánh Mì Zone",
      weight: "1 Phần ăn nóng (450g kèm 1 ổ bánh mì)",
      shelfLife: "Dùng ngon nhất trong vòng 45 phút sau khi nhận",
      storage: "Nên dùng ngay khi còn nóng hổi",
      packingStandard: "Hộp bã mía giữ nhiệt thân thiện môi trường"
    },
    nutrition: {
      servingSize: "1 suất tiêu chuẩn",
      calories: "520 kcal",
      protein: "24 g",
      fat: "22 g",
      carbs: "58 g",
      minerals: "Canxi, Sắt, Vitamin A"
    },
    cookingTips: [
      "Nếu để nguội, có thể cho phần sốt vào lò vi sóng quay 40 giây ở mức trung bình.",
      "Bánh mì có thể nướng lại bằng nồi chiên không dầu 160°C trong 2 phút để giòn rụm như vừa ra lò."
    ],
    reviews: [
      {
        id: "rv-bm1",
        author: "Lê Quốc Bảo",
        avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "Hôm qua lúc 12:30",
        comment: "Trưa văn phòng đói bụng đặt combo này quá hợp lý. Giao tới vẫn bốc khói nghi ngút, bánh mì giòn tan, pate ngậy thơm chấm đẫm sốt ngon nhức nách!",
        helpfulCount: 22,
        verified: true
      }
    ]
  },
  {
    id: "p4",
    name: "Nước Ép Cam Sành Tươi Nguyên Chất 100%",
    category: "beverage",
    categoryName: "Đồ uống",
    price: 32000,
    oldPrice: 40000,
    discountBadge: "-20%",
    unit: "Chai 350ml",
    image: "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1600271886742-f049cd451bba?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Vắt tươi trực tiếp",
    certification: {
      type: "VietGAP",
      certNo: "VG-HG-2026-118",
      issuedBy: "Sở Nông Nghiệp Hậu Giang",
      issuedDate: "05/01/2026",
      expiryDate: "05/01/2027"
    },
    store: {
      id: "store_sieu_thi_trai_cay_xanh",
      name: "Siêu Thị Trái Cây Xanh",
      address: "112 Trần Thái Tông, Cầu Giấy, Hà Nội",
      distanceKm: 2.5,
      deliveryTime: "15 - 20 phút",
      rating: 4.9,
      totalProducts: 180,
      isVerified: true
    },
    rating: 4.9,
    reviewsCount: 95,
    sold: 180,
    stock: 70,
    description:
      "Nước ép cam sành miền Tây nguyên chất 100%, vắt trực tiếp từ những trái cam mọng nước chọn lọc khi khách vừa bấm đặt đơn. Hoàn toàn không pha nước, không đường hóa học, không chất bảo quản, giữ trọn vẹn vị chua ngọt thanh mát và hàm lượng Vitamin C dồi dào.",
    highlights: [
      "100% Cam sành miền Tây tươi mọng nước",
      "Vắt máy ép chậm lấy trọn nước ngọt không bị đắng vỏ",
      "Đóng chai thủy tinh kháng khuẩn niêm phong nắp nhôm",
      "Tăng cường sức đề kháng và thanh lọc cơ thể"
    ],
    specs: {
      origin: "Vùng trồng cam sành Tam Bình, Vĩnh Long",
      brand: "Fresh Zone Drinks",
      weight: "350ml",
      shelfLife: "24 giờ trong ngăn mát tủ lạnh (1-4°C)",
      storage: "Bảo quản lạnh, lắc đều trước khi uống",
      packingStandard: "Chai thủy tinh thực phẩm có màng niêm phong"
    },
    nutrition: {
      servingSize: "1 chai 350ml",
      calories: "140 kcal",
      protein: "2.1 g",
      fat: "0.2 g",
      carbs: "33 g",
      minerals: "Vitamin C (120mg - 200% DV), Kali, Flavonoids"
    },
    cookingTips: [
      "Ngon nhất khi uống lạnh trực tiếp hoặc thêm vài viên đá nhỏ.",
      "Uống vào buổi sáng sau bữa ăn để cơ thể hấp thu tối đa dưỡng chất."
    ],
    reviews: [
      {
        id: "rv-c1",
        author: "Hoàng Yến",
        avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "3 ngày trước",
        comment: "Cam vắt rất ngọt tự nhiên, không bị khé cổ. Đóng chai thủy tinh nhìn xinh và sạch sẽ lắm!",
        helpfulCount: 8,
        verified: true
      }
    ]
  },
  {
    id: "p5",
    name: "Bộ Nồi Inox 3 Đáy Cao Cấp Nấu Bếp Từ",
    category: "household",
    categoryName: "Đồ gia dụng",
    price: 420000,
    oldPrice: 520000,
    discountBadge: "-19%",
    unit: "Bộ 3 nồi",
    image: "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1585670270608-b404fb8802a6?auto=format&fit=crop&w=800&q=80"
    ],
    store: {
      id: "store_tong_kho_gia_dung_my_dinh",
      name: "Tổng Kho Gia Dụng Mỹ Đình",
      address: "88 Lê Đức Thọ, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.4,
      deliveryTime: "30 - 35 phút",
      rating: 4.7,
      totalProducts: 520,
      isVerified: true
    },
    rating: 4.7,
    reviewsCount: 42,
    sold: 65,
    stock: 25,
    description:
      "Bộ 3 nồi Inox 304 cao cấp cấu tạo 3 đáy dập nguyên khối giúp truyền nhiệt nhanh, tỏa nhiệt đều và chống khét dính đáy nồi. Thích hợp cho mọi loại bếp: bếp từ, hồng ngoại, bếp gas.",
    highlights: [
      "Chất liệu Inox 304 chống gỉ sét tuyệt đối",
      "Vung kính cường lực viền inox chịu nhiệt chống va đập",
      "Quai tán đinh chịu lực chắc chắn, cách nhiệt an toàn"
    ],
    specs: {
      origin: "Việt Nam",
      brand: "Inox Mart Cook",
      weight: "3.2 kg (Kích thước: 16cm, 20cm, 24cm)",
      shelfLife: "Bảo hành 5 năm",
      storage: "Rửa sạch lau khô sau khi sử dụng",
      packingStandard: "Thùng carton chèn xốp định hình"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "-",
      protein: "-",
      fat: "-",
      carbs: "-",
      minerals: "-"
    },
    cookingTips: [
      "Không đun nồi không có thực phẩm trên bếp ở nhiệt độ cao.",
      "Sử dụng miếng rửa bát mềm để giữ độ sáng bóng của inox lâu dài."
    ],
    reviews: []
  },
  {
    id: "p6",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
    category: "food",
    categoryName: "Nhu yếu phẩm",
    price: 190000,
    oldPrice: 225000,
    discountBadge: "-15%",
    unit: "Túi 5kg",
    image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1536304993881-ff6e9eefa2a6?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Gạo ngon thế giới",
    certification: {
      type: "OCOP 4 Sao",
      certNo: "OCOP-ST-2025-001",
      issuedBy: "UBND Tỉnh Sóc Trăng",
      issuedDate: "12/04/2025",
      expiryDate: "12/04/2028"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      totalProducts: 240,
      isVerified: true
    },
    rating: 5.0,
    reviewsCount: 64,
    sold: 96,
    stock: 80,
    description:
      "Gạo ST25 chính hãng kỹ sư Hồ Quang Cua, đoạt giải Gạo Ngon Nhất Thế Giới. Hạt gạo thon dài, trắng trong, khi nấu chín hạt cơm mềm dẻo, thơm ngào ngạt mùi lá dứa tự nhiên, đặc biệt cơm để nguội vẫn giữ nguyên độ dẻo ngon.",
    highlights: [
      "Gạo ST25 lúa tôm chính gốc Sóc Trăng",
      "Canh tác hữu cơ trong mô hình ruộng tôm sạch, không hóa chất độc hại",
      "Hạt cơm mềm dai, thơm hương cốm và lá dứa tinh tế"
    ],
    specs: {
      origin: "Sóc Trăng, Đồng Bằng Sông Cửu Long",
      brand: "Gạo Ông Cua (DNTN Hồ Quang Trí)",
      weight: "5 kg",
      shelfLife: "12 tháng kể từ ngày sản xuất",
      storage: "Để nơi khô ráo, thoáng mát, đậy kín nắp thùng gạo",
      packingStandard: "Túi màng nhôm PE có van thở chống mối mọt"
    },
    nutrition: {
      servingSize: "100g gạo trắng nấu chín",
      calories: "130 kcal",
      protein: "2.7 g",
      fat: "0.3 g",
      carbs: "28 g",
      minerals: "Sắt, Magie, Vitamin B1"
    },
    cookingTips: [
      "Đong tỷ lệ 1 bát gạo : 1 đến 1.1 bát nước. Không cần vo gạo quá kỹ để tránh mất lớp cám dưỡng chất.",
      "Sau khi nồi cơm bật nút chín, ủ hơi thêm 10-15 phút để cơm nở đều và thơm ngon nhất."
    ],
    reviews: [
      {
        id: "rv-g1",
        author: "Bác Hùng - Cầu Giấy",
        avatar: "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "4 ngày trước",
        comment: "Gạo chuẩn tem chống giả Ông Cua, nấu lên thơm cả gian bếp. Cả nhà tôi ăn quen gạo này mấy năm nay rồi, đặt ZoneMart giao tận cửa chung cư đỡ phải xách nặng.",
        helpfulCount: 15,
        verified: true
      }
    ]
  },
  {
    id: "p7",
    name: "Rau Xà Lách Xoăn Thủy Canh Hữu Cơ Đà Lạt",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 25000,
    oldPrice: 32000,
    discountBadge: "-22%",
    unit: "Gói 300g",
    image: "https://images.unsplash.com/photo-1550411294-b3b1bf5bece1?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1550411294-b3b1bf5bece1?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=800&q=80"
    ],
    certification: {
      type: "VietGAP",
      certNo: "VG-DL-8821",
      issuedBy: "Chi Cục Trồng Trọt & BVTV Lâm Đồng",
      issuedDate: "10/01/2026",
      expiryDate: "10/01/2027"
    },
    store: {
      id: "store_sieu_thi_trai_cay_xanh",
      name: "Siêu Thị Trái Cây Xanh",
      address: "112 Trần Thái Tông, Cầu Giấy, Hà Nội",
      distanceKm: 2.5,
      deliveryTime: "20 - 25 phút",
      rating: 4.9,
      totalProducts: 180,
      isVerified: true
    },
    rating: 4.9,
    reviewsCount: 58,
    sold: 110,
    stock: 55,
    description:
      "Rau xà lách xoăn xanh trồng thủy canh hồi lưu trong nhà màng tự động tại Đà Lạt. Từng bẹ rau giòn tan mọng nước, không dư lượng thuốc bảo vệ thực vật, vị ngọt thanh không đắng, cực kỳ lý tưởng để làm salad hoặc cuốn thịt nướng.",
    highlights: [
      "Trồng thủy canh nước sạch công nghệ Israel",
      "Lá xoăn bồng bềnh, giữ nguyên rễ tươi khi giao",
      "Rau sạch ăn liền sau khi rửa nhẹ với nước"
    ],
    specs: {
      origin: "Đà Lạt, Lâm Đồng",
      brand: "GreenHydro Farm",
      weight: "300g (Gói 1-2 cây nguyên rễ)",
      shelfLife: "5-7 ngày trong ngăn mát tủ lạnh",
      storage: "Bọc giấy báo hoặc màng bọc thực phẩm cất ngăn mát",
      packingStandard: "Túi nilon đục lỗ thoáng khí nguyên bầu rễ"
    },
    nutrition: {
      servingSize: "100g rau xà lách tươi",
      calories: "15 kcal",
      protein: "1.4 g",
      fat: "0.2 g",
      carbs: "2.9 g",
      minerals: "Chất xơ, Vitamin A, Vitamin K, Folate"
    },
    cookingTips: [
      "Ngâm nước đá lạnh 5 phút trước khi trộn salad để lá rau giòn rụm tối đa."
    ],
    reviews: []
  },
  {
    id: "p8",
    name: "Cá Hồi Na Uy Tươi Phi Lê Cắt Miếng",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 245000,
    oldPrice: 280000,
    discountBadge: "-12%",
    unit: "Khay 300g",
    image: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1467003909585-2f8a72700288?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Tươi sống bảo quản lạnh",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "GLOBALG.A.P-NORWAY-449",
      issuedBy: "Cơ Quan An Toàn Thực Phẩm Na Uy (Mattilsynet)",
      issuedDate: "01/01/2026",
      expiryDate: "01/01/2027"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      totalProducts: 240,
      isVerified: true
    },
    rating: 5.0,
    reviewsCount: 76,
    sold: 88,
    stock: 30,
    description:
      "Cá hồi Đại Tây Dương nhập khẩu tươi nguyên con bằng đường hàng không từ Na Uy, phi lê lọc xương tỉ mỉ trong phòng lạnh vô trùng. Thớ thịt màu cam tươi sáng, vân mỡ trắng béo ngậy, thích hợp ăn sống chuẩn sashimi hoặc áp chảo sốt bơ tỏi.",
    highlights: [
      "Cá hồi tươi bay (Fresh Air Salmon) từ Na Uy, không đông đá",
      "Đầy ắp Omega-3, DHA, EPA tốt cho tim mạch và não bộ",
      "Lọc sạch xương dăm 100%, an toàn cho trẻ nhỏ"
    ],
    specs: {
      origin: "Vùng biển lạnh Na Uy (Leroy / SalMar)",
      brand: "Nordic Fresh Zone",
      weight: "300g (Cắt miếng phi lê thân dày)",
      shelfLife: "3 ngày trong ngăn mát (0-2°C), 3 tháng trong ngăn đông",
      storage: "Bảo quản ngăn mát lạnh nhất hoặc ướp đá vụn",
      packingStandard: "Khay xốp thực phẩm lót giấy thấm hút chân không"
    },
    nutrition: {
      servingSize: "100g cá hồi phi lê",
      calories: "208 kcal",
      protein: "20.4 g",
      fat: "13.4 g (giàu Omega-3 2.5g)",
      carbs: "0 g",
      minerals: "Vitamin D, Vitamin B12, Selen, Kali"
    },
    cookingTips: [
      "Áp chảo mặt da cá trước lửa vừa 4 phút để da giòn rụm, lật mặt thịt 2 phút kèm bơ và tỏi đập dập."
    ],
    reviews: []
  },
  {
    id: "p9",
    name: "Cà Chua Bi Socola Ngọt Đậm Vị VietGAP",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 35000,
    oldPrice: 45000,
    discountBadge: "-22%",
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80"
    ],
    certification: {
      type: "VietGAP",
      certNo: "VG-DL-2026-901",
      issuedBy: "Sở Nông Nghiệp Lâm Đồng",
      issuedDate: "12/01/2026",
      expiryDate: "12/01/2027"
    },
    store: {
      id: "store_sieu_thi_trai_cay_xanh",
      name: "Siêu Thị Trái Cây Xanh",
      address: "112 Trần Thái Tông, Cầu Giấy, Hà Nội",
      distanceKm: 2.5,
      deliveryTime: "20 - 25 phút",
      rating: 4.8,
      totalProducts: 180,
      isVerified: true
    },
    rating: 4.8,
    reviewsCount: 90,
    sold: 165,
    stock: 65,
    description:
      "Cà chua bi socola giống Hà Lan trồng tại Đà Lạt. Vỏ ngoài màu nâu tím đặc trưng, giòn bụp khi cắn, vị ngọt đậm đà xen chút chua nhẹ tinh tế, chứa hàm lượng chất chống oxy hóa Anthocyanin cao gấp 3 lần cà chua thường.",
    highlights: [
      "Giống hạt F1 Hà Lan cho quả tròn mọng, ngọt đậm vị",
      "Giàu lycopene và anthocyanin chống lão hóa",
      "Ăn trực tiếp như trái cây tráng miệng thơm ngon"
    ],
    specs: {
      origin: "Đơn Dương, Lâm Đồng",
      brand: "Eco Berry Farm",
      weight: "500g",
      shelfLife: "7-10 ngày ở nhiệt độ phòng hoặc ngăn mát",
      storage: "Bảo quản nơi thoáng mát",
      packingStandard: "Hộp nhựa trong suốt có lỗ thông gió"
    },
    nutrition: {
      servingSize: "100g cà chua bi",
      calories: "18 kcal",
      protein: "0.9 g",
      fat: "0.2 g",
      carbs: "3.9 g",
      minerals: "Lycopene, Vitamin C, Kali"
    },
    cookingTips: [
      "Rửa sạch để ráo, ăn kèm phô mai mozzarella tươi và sốt balsamic chuẩn vị Ý."
    ],
    reviews: []
  },
  {
    id: "p10",
    name: "Cơm Tấm Sườn Bì Chả Đặc Biệt Nóng Hổi",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 52000,
    oldPrice: 60000,
    discountBadge: "-13%",
    unit: "Hộp 1 suất",
    image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Kèm canh & nước mắm",
    store: {
      id: "store_bep_com_nieu_com_tam",
      name: "Bếp Cơm Niêu & Cơm Tấm",
      address: "56 Nguyễn Chánh, Cầu Giấy, Hà Nội",
      distanceKm: 3.4,
      deliveryTime: "20 - 25 phút",
      rating: 4.9,
      totalProducts: 60,
      isVerified: true
    },
    rating: 4.9,
    reviewsCount: 142,
    sold: 310,
    stock: 40,
    description:
      "Suất cơm tấm hạt tấm nhuyễn thơm dẻo, miếng sườn cốt lết nướng mật ong than hoa đậm đà thơm nức mũi, chả trứng hấp béo ngậy, bì heo trộn thính gạo rang giòn dai, mỡ hành óng ả và đồ chua củ cải cà rốt giòn ngọt.",
    highlights: [
      "Sườn nướng than hoa vàng ươm, đẫm sốt mật ong bí truyền",
      "Nước mắm tỏi ớt kẹo sệt chua ngọt chuẩn vị Sài Gòn",
      "Kèm canh rong biển thịt băm nóng hổi"
    ],
    specs: {
      origin: "Chế biến tươi tại Bếp Cơm Tấm ZoneMart",
      brand: "Bếp Cơm Tấm",
      weight: "1 Suất đầy đặn (550g)",
      shelfLife: "Dùng ngon nhất trong vòng 1 giờ",
      storage: "Ăn ngay khi còn nóng",
      packingStandard: "Hộp bã mía chia ngăn giữ nhiệt cao cấp"
    },
    nutrition: {
      servingSize: "1 suất cơm tấm",
      calories: "680 kcal",
      protein: "32 g",
      fat: "24 g",
      carbs: "82 g",
      minerals: "Đầy đủ dưỡng chất cho bữa chính"
    },
    cookingTips: [
      "Rưới nước mắm tỏi ớt đều lên cơm và sườn trước khi thưởng thức."
    ],
    reviews: []
  },
  {
    id: "p11",
    name: "Bơ Sáp 034 Đặc Sản Lâm Đồng Dẻo Béo",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 65000,
    oldPrice: 85000,
    discountBadge: "-23%",
    unit: "Kg",
    image: "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=800&q=80"
    ],
    certification: {
      type: "VietGAP",
      certNo: "VG-LD-2026-034",
      issuedBy: "Sở Nông Nghiệp Lâm Đồng",
      issuedDate: "02/01/2026",
      expiryDate: "02/01/2027"
    },
    store: {
      id: "store_sieu_thi_trai_cay_xanh",
      name: "Siêu Thị Trái Cây Xanh",
      address: "112 Trần Thái Tông, Cầu Giấy, Hà Nội",
      distanceKm: 2.5,
      deliveryTime: "20 - 25 phút",
      rating: 4.9,
      totalProducts: 180,
      isVerified: true
    },
    rating: 4.9,
    reviewsCount: 67,
    sold: 130,
    stock: 50,
    description:
      "Bơ 034 Bảo Lộc chính gốc dáng thon dài từ 25-35cm, hạt tiêu nhỏ xíu, cơm bơ dày vàng ươm dẻo quánh và béo ngậy. Bơ được hái già cuống, tự chín tự nhiên trong 2-3 ngày, không ngâm thuốc kích chín.",
    highlights: [
      "Cơm vàng béo ngậy như sáp, hạt siêu nhỏ",
      "Trái dài đều đặn 2-3 quả/kg",
      "Đổi trả 1-1 nếu bơ bị sượng hoặc đen chỉ xơ"
    ],
    specs: {
      origin: "Bảo Lộc, Lâm Đồng",
      brand: "Đặc Sản Bơ Tây Nguyên",
      weight: "1 kg (2-3 trái)",
      shelfLife: "Chín trong 2-4 ngày, sau khi chín để ngăn mát 3-5 ngày",
      storage: "Để nơi thoáng mát khi chưa chín, không ủ trong túi nilon kín",
      packingStandard: "Lưới xốp bọc từng quả chống thâm dập"
    },
    nutrition: {
      servingSize: "100g thịt bơ tươi",
      calories: "160 kcal",
      protein: "2 g",
      fat: "14.7 g (axit béo không bão hòa đơn tốt cho tim mạch)",
      carbs: "8.5 g",
      minerals: "Kali (nhiều hơn chuối), Vitamin E, Lutein"
    },
    cookingTips: [
      "Cắt lát ăn cùng bánh mì nướng trứng ốp la hoặc dầm cùng sữa đặc và đá xay."
    ],
    reviews: []
  },
  {
    id: "p12",
    name: "Nước Rửa Bát Hữu Cơ Quế & Chanh Gừng 800ml",
    category: "household",
    categoryName: "Đồ dùng gia đình",
    price: 48000,
    oldPrice: 60000,
    discountBadge: "-20%",
    unit: "Chai 800ml",
    image: "https://images.unsplash.com/photo-1585670270608-b404fb8802a6?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1585670270608-b404fb8802a6?auto=format&fit=crop&w=800&q=80"
    ],
    certification: {
      type: "Organic",
      certNo: "ECOCERT-VN-2025",
      issuedBy: "Tổ Chức Chứng Nhận Hữu Cơ ECOCERT",
      issuedDate: "15/05/2025",
      expiryDate: "15/05/2027"
    },
    store: {
      id: "store_tong_kho_gia_dung_my_dinh",
      name: "Tổng Kho Gia Dụng Mỹ Đình",
      address: "88 Lê Đức Thọ, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.4,
      deliveryTime: "30 - 35 phút",
      rating: 4.8,
      totalProducts: 520,
      isVerified: true
    },
    rating: 4.8,
    reviewsCount: 35,
    sold: 95,
    stock: 120,
    description:
      "Nước rửa chén hữu cơ chiết xuất từ tinh dầu quế tự nhiên, enzym bồ hòn và chanh gừng. Làm sạch dầu mỡ nhanh chóng, khử sạch mùi tanh tanh trên bát đĩa mà không để lại mùi hóa chất, hoàn toàn êm dịu không khô hại da tay.",
    highlights: [
      "99% Thành phần có nguồn gốc thực vật sinh học",
      "Không hóa chất tạo bọt công nghiệp SLS/SLES, không Paraben",
      "An toàn rửa bình sữa và đồ ăn dặm cho trẻ em"
    ],
    specs: {
      origin: "Việt Nam",
      brand: "EcoZone Organic Home",
      weight: "800ml",
      shelfLife: "24 tháng kể từ NSX",
      storage: "Để nơi khô ráo, tránh ánh nắng trực tiếp",
      packingStandard: "Chai nhựa HDPE tái sinh có vòi bơm tiện lợi"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "-",
      protein: "-",
      fat: "-",
      carbs: "-",
      minerals: "-"
    },
    cookingTips: [
      "Bơm 1 lượng nhỏ lên miếng rửa bát ẩm để tạo bọt enzym tự nhiên."
    ],
    reviews: []
  },
  {
    id: "p13",
    name: "Thịt Gà Ta Thả Vườn Làm Sạch Nguyên Con",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 165000,
    oldPrice: 195000,
    discountBadge: "-15%",
    unit: "Con 1.3 - 1.5kg",
    image: "https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Gà ta thả đồi",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "VSATTP-HN-2026",
      issuedBy: "Chi Cục Thú Y Hà Nội",
      issuedDate: "01/01/2026",
      expiryDate: "01/01/2027"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      totalProducts: 240,
      isVerified: true
    },
    rating: 4.9,
    reviewsCount: 52,
    sold: 110,
    stock: 25,
    description: "Gà ta nuôi thả đồi tự nhiên thịt săn chắc, da giòn vàng óng, vị ngọt đậm đà. Đã được làm sạch lông mổ moi hút chân không vệ sinh an toàn thực phẩm.",
    highlights: [
      "Gà ta thả đồi tự nhiên thịt dai thơm ngọt",
      "Làm sạch mổ moi đóng gói bảo quản lạnh",
      "Giao hỏa tốc 20 phút bảo đảm tươi rói"
    ],
    specs: {
      origin: "Ba Vì, Hà Nội",
      brand: "Gà Sạch Đồi Ba Vì",
      weight: "1.3 - 1.5kg/con",
      shelfLife: "3 ngày ngăn mát, 30 ngày ngăn đông",
      storage: "0-4°C trong tủ lạnh",
      packingStandard: "Hút chân không tiêu chuẩn ATTP"
    },
    nutrition: {
      servingSize: "100g",
      calories: "239 kcal",
      protein: "27.3g",
      fat: "13.6g",
      carbs: "0g",
      minerals: "Sắt, Phốt pho, Vitamin A, B3"
    },
    cookingTips: ["Luộc lửa vừa kèm gừng hành hoa tiêu 25 phút để da giòn thịt ngọt."],
    reviews: []
  },
  {
    id: "p14",
    name: "Cánh Gà Tươi CP Loại 1 Đóng Khay",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 82000,
    oldPrice: 95000,
    discountBadge: "-14%",
    unit: "Khay 500g",
    image: "https://images.unsplash.com/photo-1527477321055-43615b629c5e?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1527477321055-43615b629c5e?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Tươi mổ trong ngày",
    store: {
      id: "store_zonemart_cau_giay",
      name: "ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      totalProducts: 240,
      isVerified: true
    },
    rating: 4.8,
    reviewsCount: 38,
    sold: 145,
    stock: 40,
    description: "Cánh gà tươi CP chuẩn thịt sạch, lớp da mỏng ít mỡ, thích hợp chiên nước mắm, nướng mật ong hoặc rim me.",
    highlights: ["Cánh gà tươi sạch mổ trong ngày", "Chuẩn thịt sạch CP an toàn tuyệt đối"],
    specs: {
      origin: "CP Foods Việt Nam",
      brand: "CP Fresh Meat",
      weight: "500g (khoảng 4-6 khúc cánh)",
      shelfLife: "3 ngày ngăn mát",
      storage: "0-4°C",
      packingStandard: "Khay màng bọc thực phẩm hút ẩm"
    },
    nutrition: {
      servingSize: "100g",
      calories: "203 kcal",
      protein: "18.3g",
      fat: "14.2g",
      carbs: "0g",
      minerals: "Canxi, Sắt"
    },
    cookingTips: ["Khứa nhẹ thân cánh ướp gia vị 15 phút trước khi nướng hoặc chiên."],
    reviews: []
  },
  {
    id: "p15",
    name: "Cơm Gà Xối Mỡ Da Giòn Nóng Hổi Kèm Canh",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 55000,
    oldPrice: 65000,
    discountBadge: "-15%",
    unit: "Phần",
    image: "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Nóng giòn thơm nức",
    store: {
      id: "store_bep_com_nieu",
      name: "Bếp Cơm Niêu & Cơm Tấm",
      address: "15 Nguyễn Khang, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.1,
      deliveryTime: "15 - 25 phút",
      rating: 4.9,
      totalProducts: 45,
      isVerified: true
    },
    rating: 4.9,
    reviewsCount: 76,
    sold: 230,
    stock: 60,
    description: "Cơm gà xối mỡ hạt cơm vàng óng nấu từ nước luộc gà, đùi góc tư gà xối mỡ nóng hổi da giòn rụm, thịt mềm ngọt nước kèm dưa chua và canh rong biển.",
    highlights: ["Gà xối mỡ da giòn rụm nóng hổi", "Cơm nấu nước luộc gà béo ngậy vàng ươm"],
    specs: {
      origin: "Chế biến tại bếp đạt chuẩn VSATTP",
      brand: "Bếp Cơm Niêu & Cơm Tấm",
      weight: "1 Suất ăn đầy đặn",
      shelfLife: "Dùng nóng trong vòng 2 giờ",
      storage: "Giữ ấm trong hộp bảo ôn",
      packingStandard: "Hộp bã mía thân thiện môi trường giữ nhiệt"
    },
    nutrition: {
      servingSize: "1 suất (450g)",
      calories: "680 kcal",
      protein: "38g",
      fat: "24g",
      carbs: "78g",
      minerals: "Đầy đủ dinh dưỡng"
    },
    cookingTips: ["Dùng ngay khi còn nóng để cảm nhận trọn vẹn độ giòn của da gà."],
    reviews: []
  }
];

export function useProductCatalog() {
  const moderation = useProductModeration();

  // Danh sách tổng hợp toàn bộ sản phẩm (Gồm catalog tĩnh + sản phẩm do Seller đăng đã được duyệt)
  const allProducts = computed<CatalogProduct[]>(() => {
    const dynamicActive = moderation.activeProducts.value.map((p) => {
      let cat: "food" | "veggie" | "fastfood" | "beverage" | "household" = "food";
      if (p.category === "Rau củ quả") cat = "veggie";
      else if (p.category === "Thịt cá tươi") cat = "food";
      else if (p.category === "Trái cây tươi") cat = "beverage";
      else if (p.category === "Món ăn nóng") cat = "fastfood";
      else cat = "household";

      return {
        id: p.id,
        name: p.name,
        category: cat,
        categoryName: p.category,
        price: p.price,
        unit: p.unit || "Phần",
        image: p.image,
        gallery: [p.image],
        badge: "Đã AI Kiểm Duyệt",
        store: {
          id: "store_" + (p.storeName || "seller").toLowerCase().replace(/[^a-z0-9]/g, "_"),
          name: p.storeName || "Nông Sản Sạch Ba Vì",
          address: "Ba Vì, Hà Nội",
          distanceKm: 1.8,
          deliveryTime: "20 - 30 phút",
          rating: 5.0,
          totalProducts: 15,
          isVerified: true
        },
        rating: 5.0,
        reviewsCount: 14,
        sold: 42,
        stock: p.stock || 50,
        description: `Sản phẩm ${p.name} tươi sạch thu hoạch trực tiếp từ trang trại. Đã trải qua quy trình kiểm định AI Vision Guard của sàn thương mại điện tử ZoneMart.`,
        highlights: [
          "Nông sản sạch thu hoạch tự nhiên trong ngày",
          "Đã vượt qua kiểm định AI an toàn thực phẩm",
          "Giao hỏa tốc giữ trọn độ tươi"
        ],
        specs: {
          origin: "Việt Nam",
          brand: p.storeName || "Hộ Nông Dân Địa Phương",
          weight: p.unit || "Tiêu chuẩn",
          shelfLife: "3-5 ngày trong ngăn mát",
          storage: "Bảo quản ở nhiệt độ mát",
          packingStandard: "Đóng gói tiêu chuẩn ATTP"
        },
        nutrition: {
          servingSize: "100g",
          calories: "Chưa cập nhật",
          protein: "Chưa cập nhật",
          fat: "Chưa cập nhật",
          carbs: "Chưa cập nhật",
          minerals: "Đầy đủ dưỡng chất tự nhiên"
        },
        cookingTips: ["Rửa sạch và chế biến theo khẩu vị gia đình."],
        reviews: [],
        aiAudit: {
          safetyScore: p.aiScore?.safetyScore || 98,
          matchScore: p.aiScore?.matchScore || 95,
          verificationDate: p.createdAt || new Date().toLocaleDateString("vi-VN"),
          inspector: "ZoneMart AI Vision System",
          notes: "Đạt chuẩn an toàn thực phẩm sàn ZoneMart."
        }
      } as CatalogProduct;
    });

    return [
      ...dynamicActive.filter((dp) => !CATALOG_PRODUCTS.some((p) => p.id === dp.id)),
      ...CATALOG_PRODUCTS
    ];
  });

  // Danh sách toàn bộ gian hàng trích xuất tự động từ sản phẩm
  const allStores = computed<CatalogStore[]>(() => {
    const storeMap = new Map<string, CatalogStore>();

    for (const p of allProducts.value) {
      if (!p.store || !p.store.name) continue;
      const key = p.store.name.trim();

      if (!storeMap.has(key)) {
        let avatar = "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80";
        let cover = p.image;
        if (key.includes("Cầu Giấy") || key.includes("ZoneMart")) {
          avatar = "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80";
          cover = "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80";
        } else if (key.includes("Trái Cây") || key.includes("Quả")) {
          avatar = "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=200&q=80";
          cover = "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80";
        } else if (key.includes("Bánh Mì") || key.includes("Tiệm")) {
          avatar = "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80";
          cover = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80";
        } else if (key.includes("Gia Dụng") || key.includes("Kho")) {
          avatar = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80";
          cover = "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80";
        } else if (key.includes("Cơm") || key.includes("Bếp")) {
          avatar = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80";
          cover = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80";
        } else if (key.includes("VietGAP") || key.includes("Nông Trại") || key.includes("Rau")) {
          avatar = "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=200&q=80";
          cover = "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80";
        }

        storeMap.set(key, {
          id: p.store.id || `store_${key.toLowerCase().replace(/[^a-z0-9]/g, '_')}`,
          name: p.store.name,
          address: p.store.address || "Hà Nội",
          distanceKm: p.store.distanceKm || 1.5,
          deliveryTime: p.store.deliveryTime || "15 - 20 phút",
          rating: p.store.rating || 5.0,
          reviewsCount: p.reviewsCount || 48,
          totalProducts: p.store.totalProducts || 1,
          isVerified: p.store.isVerified !== false,
          categoryName: p.categoryName || "Thực phẩm tươi",
          avatar,
          coverImage: cover,
          openHours: "07:00 - 22:00",
          products: [p],
        });
      } else {
        const store = storeMap.get(key)!;
        store.products.push(p);
        store.totalProducts = Math.max(store.totalProducts, store.products.length);
      }
    }

    return Array.from(storeMap.values());
  });

  // Tìm kiếm sản phẩm theo ID (hỗ trợ mã 'p1' lẫn '1')
  const getProductById = (id: string | number): CatalogProduct | undefined => {
    const targetId = String(id).trim().toLowerCase();
    const cleanNumId = targetId.startsWith("p") ? targetId.slice(1) : targetId;

    return (
      allProducts.value.find((p) => p.id.toLowerCase() === targetId) ||
      allProducts.value.find((p) => p.id.toLowerCase() === `p${cleanNumId}`) ||
      allProducts.value.find((p) => p.id.toLowerCase() === cleanNumId) ||
      CATALOG_PRODUCTS[0] // Fallback an toàn tới sản phẩm đầu tiên
    );
  };

  // Lấy danh sách sản phẩm liên quan trong bán kính 10km
  const getRelatedProducts = (currentId: string, limit = 4): CatalogProduct[] => {
    return allProducts.value
      .filter((p) => p.id !== currentId)
      .slice(0, limit);
  };

  const getStoreByIdOrName = (idOrName: string): CatalogStore | undefined => {
    const term = idOrName.toLowerCase().trim();
    return allStores.value.find(
      (s) => s.id.toLowerCase() === term || s.name.toLowerCase() === term
    );
  };

  return {
    allProducts,
    allStores,
    getProductById,
    getRelatedProducts,
    getStoreByIdOrName,
  };
}

