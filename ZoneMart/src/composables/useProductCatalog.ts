import { ref, computed } from "vue";
import { useProductModeration } from "./useProductModeration";

const catalogVersion = ref(0);

if (typeof window !== "undefined") {
  window.addEventListener("zonemart:products-changed", () => {
    catalogVersion.value++;
  });
  window.addEventListener("storage", (e) => {
    if (e.key === "zonemart_moderated_products") {
      catalogVersion.value++;
    }
  });
}

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
    reviewsCount?: number;
    openHours?: string;
    totalProducts: number;
    isVerified: boolean;
    categoryName?: string;
    avatar?: string;
    coverImage?: string;
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

// 8 GIAN HÀNG CHÍNH THỨC VỚI 32 SẢN PHẨM HOÀN TOÀN ĐỘC NHẤT, THÔNG SỐ VÀ CHỈ SỐ KHÁC BIỆT 100%
export const CATALOG_PRODUCTS: CatalogProduct[] = [
  // ==========================================
  // GIAN HÀNG 1: Vườn Rau Hữu Cơ Bác Ba
  // ==========================================
  {
    id: "p1",
    name: "Rau Xà Lách Mỡ Thủy Canh VietGAP",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 35000,
    oldPrice: 45000,
    discountBadge: "-22%",
    unit: "1kg",
    image: "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Bán chạy #1",
    certification: {
      type: "VietGAP",
      certNo: "VG-HN-2026-0812",
      issuedBy: "Sở Nông Nghiệp & PTNT Hà Nội",
      issuedDate: "12/01/2026",
      expiryDate: "12/01/2027"
    },
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 168,
    sold: 389,
    stock: 80,
    description: "Xà lách mỡ canh tác theo phương pháp thủy canh tuần hoàn khép kín tại trang trại Ba Vì. Lá rau dày, giòn ngọt, hoàn toàn không sử dụng thuốc trừ sâu hay chất kích thích sinh trưởng.",
    highlights: [
      "Thu hoạch tươi mới 2 cữ mỗi ngày (sáng sớm & đầu giờ chiều)",
      "Không tồn dư thuốc BVTV, đạt chuẩn VietGAP",
      "Giữ trọn vị giòn mát, bảo quản ngăn mát 5-7 ngày"
    ],
    specs: {
      origin: "Ba Vì, Hà Nội",
      brand: "Vườn Rau Bác Ba",
      weight: "1kg (3 - 4 cây)",
      shelfLife: "5-7 ngày trong ngăn mát tủ lạnh",
      storage: "Bọc giấy báo hoặc màng bọc thực phẩm, để ngăn rau củ",
      packingStandard: "Túi màng thở sinh học tự phân hủy"
    },
    nutrition: {
      servingSize: "100g",
      calories: "15 kcal",
      protein: "1.4 g",
      fat: "0.2 g",
      carbs: "2.9 g",
      minerals: "Vitamin A (148% DV), Vitamin K, Folate"
    },
    cookingTips: ["Rửa nhẹ nhàng, ngâm nước đá 3 phút trước khi ăn để tăng độ giòn khi làm salad."],
    reviews: [
      {
        id: "rv101",
        author: "Chị Mai Lan",
        avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=120&q=80",
        rating: 5,
        date: "Hôm nay 08:30",
        comment: "Rau rất tươi, cuống còn chảy mủ trắng, giao nhanh trong 15 phút là tới!",
        helpfulCount: 15,
        verified: true
      }
    ]
  },
  {
    id: "p2",
    name: "Rau Muống Hữu Cơ Ba Vì Non Xanh",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 18000,
    oldPrice: 24000,
    discountBadge: "-25%",
    unit: "Bó 500g",
    image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80"],
    badge: "Mới ngắt sáng nay",
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 124,
    sold: 520,
    stock: 120,
    description: "Rau muống ngọn nhỏ, thân giòn đanh, ngắt ngọn non mơn mởn. Luộc nước trong veo, xào tỏi xanh mướt không bị nát hay đen cọng.",
    highlights: ["Tưới bằng nguồn nước giếng khoáng tự nhiên Ba Vì", "Luộc nước xanh ngọt mát tự nhiên", "Đạt chuẩn an toàn tuyệt đối"],
    specs: {
      origin: "Vườn rau hữu cơ Ba Vì, Hà Nội",
      brand: "Vườn Rau Bác Ba",
      weight: "Bó 500g (± 30g)",
      shelfLife: "3-4 ngày",
      storage: "Ngăn mát tủ lạnh",
      packingStandard: "Bó dây lạt rơm truyền thống"
    },
    nutrition: {
      servingSize: "100g",
      calories: "19 kcal",
      protein: "2.6 g",
      fat: "0.2 g",
      carbs: "3.1 g",
      minerals: "Sắt, Canxi, Chất xơ"
    },
    cookingTips: ["Luộc lửa to ngập nước, cho chút muối để rau giữ màu xanh ngọc bích."],
    reviews: []
  },
  {
    id: "p3",
    name: "Cà Chua Bi Socola Ngọt Đậm Vị Hữu Cơ",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 42000,
    oldPrice: 52000,
    discountBadge: "-19%",
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80"],
    badge: "Vị ngọt đặc biệt",
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 92,
    sold: 215,
    stock: 50,
    description: "Giống cà chua bi màu nâu sẫm socola giàu chất chống oxy hóa Anthocyanin. Vỏ mỏng căng mọng, vị ngọt đậm đà, ít hạt.",
    highlights: ["Độ ngọt Brix 9.5 cao gấp đôi cà chua thường", "Trồng giá thể xơ dừa vi sinh sạch sẽ", "Ăn sống như trái cây tráng miệng"],
    specs: {
      origin: "Vườn thực nghiệm công nghệ cao Ba Vì",
      brand: "Vườn Rau Bác Ba",
      weight: "500g",
      shelfLife: "7-10 ngày",
      storage: "Nhiệt độ phòng thoáng mát hoặc ngăn mát",
      packingStandard: "Hộp nhựa có lỗ thông gió"
    },
    nutrition: {
      servingSize: "100g",
      calories: "22 kcal",
      protein: "1.1 g",
      fat: "0.2 g",
      carbs: "4.8 g",
      minerals: "Lycopene, Vitamin C, Kali"
    },
    cookingTips: ["Ngon nhất khi rửa sạch ăn liền hoặc trộn cùng xà lách thủy canh."],
    reviews: []
  },
  {
    id: "p4",
    name: "Bí Xanh Thơm Cắt Khoanh Tiêu Chuẩn VietGAP",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 25000,
    oldPrice: 32000,
    discountBadge: "-21%",
    unit: "Trái 1kg",
    image: "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=800&q=80"],
    badge: "Đậm vị canh quê",
    store: {
      id: "store_vuon_rau_bac_ba",
      name: "Vườn Rau Hữu Cơ Bác Ba",
      address: "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội",
      distanceKm: 0.8,
      deliveryTime: "12 - 18 phút",
      rating: 5.0,
      reviewsCount: 384,
      openHours: "06:00 - 20:30",
      totalProducts: 42,
      isVerified: true,
      categoryName: "Rau củ hữu cơ VietGAP",
      avatar: "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 65,
    sold: 164,
    stock: 40,
    description: "Bí đao chanh thơm dẻo, ruột đặc ít hạt. Nấu canh sườn, canh tôm ngọt lịm hoặc ép nước thanh nhiệt cơ thể.",
    highlights: ["Ruột xanh dẻo, thơm ngát mùi lá nếp", "Không xơ, không chua ruột", "Đã gọt vỏ và đóng khay hút chân không sạch sẽ"],
    specs: {
      origin: "Ba Vì, Hà Nội",
      brand: "Vườn Rau Bác Ba",
      weight: "1kg (± 50g)",
      shelfLife: "5 ngày trong ngăn mát",
      storage: "Ngăn mát tủ lạnh",
      packingStandard: "Hút chân không khay thực phẩm"
    },
    nutrition: {
      servingSize: "100g",
      calories: "13 kcal",
      protein: "0.6 g",
      fat: "0.1 g",
      carbs: "3.0 g",
      minerals: "Nước 96%, Canxi, Magie"
    },
    cookingTips: ["Nấu canh với tôm nõn hoặc sườn non, rắc chút tiêu và hành ngò thơm."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 2: Nông Trại Xanh Lạc Dương
  // ==========================================
  {
    id: "p5",
    name: "Hộp Dâu Tây Giống Hana Nhật Bản Hái Sáng Nay",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 95000,
    oldPrice: 125000,
    discountBadge: "-24%",
    unit: "Hộp 500g",
    image: "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1518635017480-d471b404ab6e?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Đặc sản Lạc Dương",
    certification: {
      type: "VietGAP",
      certNo: "VG-LD-2026-0914",
      issuedBy: "Sở Nông Nghiệp Lâm Đồng",
      issuedDate: "02/02/2026",
      expiryDate: "02/02/2027"
    },
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 142,
    sold: 310,
    stock: 65,
    description: "Dâu tây giống Nhật Hana trồng nhà kính vùng cao Lạc Dương, không khí sương mờ giúp quả mọng nước, thơm lừng và ngọt sắc.",
    highlights: ["Bay hàng không về Hà Nội trong ngày", "Cuống xanh tươi nguyên phấn dâu", "Đóng hộp chống va đập chuyên dụng"],
    specs: {
      origin: "Lạc Dương, Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "500g (24-28 quả)",
      shelfLife: "3-5 ngày trong tủ lạnh",
      storage: "Để ngăn mát, không rửa khi chưa ăn",
      packingStandard: "Hộp nhựa dập lỗ thoát ẩm"
    },
    nutrition: {
      servingSize: "100g",
      calories: "32 kcal",
      protein: "0.7 g",
      fat: "0.3 g",
      carbs: "7.7 g",
      minerals: "Vitamin C 59mg, Axit Folic"
    },
    cookingTips: ["Ăn kèm sữa chua không đường hoặc làm bánh ngọt."],
    reviews: []
  },
  {
    id: "p6",
    name: "Bơ Sáp 034 Đặc Sản Lâm Đồng Dẻo Quánh",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 75000,
    oldPrice: 90000,
    discountBadge: "-16%",
    unit: "Kg 2-3 trái",
    image: "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=800&q=80"],
    badge: "Bơ đầu mùa tuyển chọn",
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 88,
    sold: 188,
    stock: 45,
    description: "Bơ sáp dáng dài 034 nức tiếng Bảo Lộc - Lạc Dương. Thịt quả vàng ươm dẻo như sáp, hạt tiêu nhỏ tí xíu, béo ngậy tự nhiên.",
    highlights: ["Bơ già cây chín tự nhiên, không ngâm ủ hóa chất", "Tỷ lệ sáp đặc trên 85%", "Bao đổi trả 1 đổi 1 nếu bị sượng"],
    specs: {
      origin: "Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "1kg (2-3 trái dài)",
      shelfLife: "2-4 ngày khi chín",
      storage: "Nhiệt độ phòng chờ chín, chín bỏ ngăn mát",
      packingStandard: "Bọc lưới xốp từng quả"
    },
    nutrition: {
      servingSize: "100g",
      calories: "160 kcal",
      protein: "2.0 g",
      fat: "14.7 g (chất béo lành mạnh)",
      carbs: "8.5 g",
      minerals: "Kali, Vitamin E, Lutein"
    },
    cookingTips: ["Dầm cùng sữa đặc và đá xay, hoặc làm sốt bơ trứng ăn kèm bánh mì."],
    reviews: []
  },
  {
    id: "p7",
    name: "Khoai Lang Mật Đà Lạt Nướng Chảy Mật Thơm Lừng",
    category: "veggie",
    categoryName: "Nông sản tươi",
    price: 45000,
    oldPrice: 55000,
    discountBadge: "-18%",
    unit: "Túi 1kg",
    image: "https://images.unsplash.com/photo-1596097635121-14b63b7a0c19?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1596097635121-14b63b7a0c19?auto=format&fit=crop&w=800&q=80"],
    badge: "Chảy mật ngọt lịm",
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 110,
    sold: 240,
    stock: 70,
    description: "Khoai lang giống Nhật trồng trên đất đỏ bazan Lạc Dương, củ đã ủ đủ ngày nên nướng hoặc luộc là tứa mật óng ả ngọt bùi.",
    highlights: ["Củ thon dài đều tay, không xơ", "Đã ủ xuống đường đạt độ ngọt tối đa", "Hấp nướng nồi chiên không dầu siêu ngon"],
    specs: {
      origin: "Đà Lạt, Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "1kg (4-6 củ)",
      shelfLife: "15 ngày",
      storage: "Để nơi khô ráo, tránh ánh nắng trực tiếp",
      packingStandard: "Túi lưới thông thoáng"
    },
    nutrition: {
      servingSize: "100g",
      calories: "86 kcal",
      protein: "1.6 g",
      fat: "0.1 g",
      carbs: "20.1 g",
      minerals: "Chất xơ, Vitamin A, Mangan"
    },
    cookingTips: ["Nướng nồi chiên không dầu 180°C trong 35 phút để mật tứa ra thơm ngát."],
    reviews: []
  },
  {
    id: "p8",
    name: "Ớt Chuông Sweet Palermo Giòn Ngọt Thanh Mát",
    category: "veggie",
    categoryName: "Rau củ sạch",
    price: 55000,
    oldPrice: 70000,
    discountBadge: "-21%",
    unit: "Túi 500g",
    image: "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=800&q=80"],
    badge: "Siêu giàu Vitamin C",
    store: {
      id: "store_nong_trai_lac_duong",
      name: "Nông Trại Xanh Lạc Dương",
      address: "112 Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.6,
      deliveryTime: "18 - 25 phút",
      rating: 4.9,
      reviewsCount: 256,
      openHours: "06:30 - 21:30",
      totalProducts: 38,
      isVerified: true,
      categoryName: "Trái cây & Củ quả Đà Lạt",
      avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 75,
    sold: 135,
    stock: 35,
    description: "Giống ớt ngọt cao cấp Palermo nhập khẩu từ Hà Lan, trồng công nghệ cao tại Lạc Dương. Không cay nồng, giòn sần sật và mọng nước ngọt thanh.",
    highlights: ["Lượng Vitamin C gấp 3 lần quả cam", "Ăn sống chấm sốt mè rang tuyệt đỉnh", "Trẻ nhỏ cũng thích mê vì không hăng cay"],
    specs: {
      origin: "Lạc Dương, Lâm Đồng",
      brand: "Lạc Dương Green Farm",
      weight: "500g (3-4 quả mix màu)",
      shelfLife: "7-10 ngày trong tủ lạnh",
      storage: "Bọc màng thực phẩm để ngăn mát",
      packingStandard: "Khay bọc màng co"
    },
    nutrition: {
      servingSize: "100g",
      calories: "28 kcal",
      protein: "1.0 g",
      fat: "0.2 g",
      carbs: "6.0 g",
      minerals: "Vitamin C 150mg, Vitamin B6"
    },
    cookingTips: ["Cắt lát xào bò hoặc ăn sống kèm sốt mè rang thanh mát."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 3: Vựa Trái Cây Sáu Thảo Miền Tây
  // ==========================================
  {
    id: "p9",
    name: "Bưởi Da Xanh Bến Tre Loại 1 Trái Mọng Tép Đỏ",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 68000,
    oldPrice: 85000,
    discountBadge: "-20%",
    unit: "Trái 1.5kg",
    image: "https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=800&q=80"],
    badge: "Đặc sản Bến Tre",
    certification: {
      type: "VietGAP",
      certNo: "VG-BT-2026-302",
      issuedBy: "Sở NN Bến Tre",
      issuedDate: "10/01/2026",
      expiryDate: "10/01/2027"
    },
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 104,
    sold: 295,
    stock: 80,
    description: "Bưởi da xanh cắt tại vườn Mỏ Cày Bắc (Bến Tre). Vỏ mỏng dính, tép bưởi màu hồng đỏ căng mọng nước, vị ngọt thanh không hề đắng hậu.",
    highlights: ["Tuyển chọn trái từ 1.4kg - 1.6kg tròn đều", "Tép róc dễ bóc, giòn mọng ngọt lịm", "Chưng bàn thờ sang trọng, ăn bổ dưỡng"],
    specs: {
      origin: "Châu Thành, Bến Tre",
      brand: "Vựa Bưởi Sáu Thảo",
      weight: "1.4kg - 1.6kg",
      shelfLife: "20-30 ngày",
      storage: "Nhiệt độ phòng thoáng gió",
      packingStandard: "Bọc túi lưới có quai xách"
    },
    nutrition: {
      servingSize: "100g tép bưởi",
      calories: "38 kcal",
      protein: "0.8 g",
      fat: "0.1 g",
      carbs: "9.6 g",
      minerals: "Vitamin C, Naringin, Kali"
    },
    cookingTips: ["Gọt vỏ tách tép chấm muối tôm Tây Ninh chua cay tê lưỡi."],
    reviews: []
  },
  {
    id: "p10",
    name: "Cam Sành Hàm Yên Mọng Nước Ngọt Tự Nhiên",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 38000,
    oldPrice: 48000,
    discountBadge: "-21%",
    unit: "Kg",
    image: "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80"],
    badge: "Vắt nước cực ngọt",
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 88,
    sold: 410,
    stock: 150,
    description: "Cam sành trái to tròn, vỏ mỏng rám nắng đúng chuẩn cam già cây. Vắt được cực nhiều nước, vị ngọt đậm đà thơm ngát.",
    highlights: ["Cam già cành mọng nước, không sượng xơ", "Vắt nước không cần thêm đường", "Tươi ngon bồi bổ sức khỏe hàng ngày"],
    specs: {
      origin: "Hàm Yên, Tuyên Quang & Vĩnh Long",
      brand: "Vựa Sáu Thảo",
      weight: "1kg (3-4 quả)",
      shelfLife: "7-10 ngày",
      storage: "Nơi râm mát hoặc ngăn mát tủ lạnh",
      packingStandard: "Túi lưới chuyên dụng"
    },
    nutrition: {
      servingSize: "100g nước cam",
      calories: "45 kcal",
      protein: "0.7 g",
      fat: "0.2 g",
      carbs: "10.4 g",
      minerals: "Vitamin C 53mg, Hesperidin"
    },
    cookingTips: ["Vắt lấy nước uống cùng vài viên đá giải nhiệt sảng khoái."],
    reviews: []
  },
  {
    id: "p11",
    name: "Xoài Cát Hòa Lộc Chín Cây Vàng Óng Thơm Lừng",
    category: "beverage",
    categoryName: "Trái cây tươi",
    price: 85000,
    oldPrice: 105000,
    discountBadge: "-19%",
    unit: "Kg",
    image: "https://images.unsplash.com/photo-1553279768-865429fa0078?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1553279768-865429fa0078?auto=format&fit=crop&w=800&q=80"],
    badge: "Vua của các loài xoài",
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 96,
    sold: 180,
    stock: 40,
    description: "Xoài cát Hòa Lộc Tiền Giang chuẩn gốc. Trái thuôn dài, da vàng mịn màng, thịt quả dày chắc mịn không hề có xơ, ngọt lịm sắc nét.",
    highlights: ["Chín tự nhiên tỏa hương thơm lừng cả gian phòng", "Thịt quả dẻo mịn tan trên đầu lưỡi", "Độ ngọt sắc đặc trưng nức tiếng miền Tây"],
    specs: {
      origin: "Cái Bè, Tiền Giang",
      brand: "Vựa Sáu Thảo",
      weight: "1kg (2 trái)",
      shelfLife: "3-5 ngày khi chín",
      storage: "Nhiệt độ phòng",
      packingStandard: "Bọc xốp chống trầy xước"
    },
    nutrition: {
      servingSize: "100g",
      calories: "60 kcal",
      protein: "0.8 g",
      fat: "0.4 g",
      carbs: "15.0 g",
      minerals: "Vitamin A, Vitamin C, Đồng"
    },
    cookingTips: ["Cắt hạt lựu ăn kèm xôi nếp cốt dừa béo ngậy."],
    reviews: []
  },
  {
    id: "p12",
    name: "Dừa Xiêm Xanh Bến Tre Ngọt Mát Đã Gọt Trọc",
    category: "beverage",
    categoryName: "Đồ uống tươi",
    price: 22000,
    oldPrice: 28000,
    discountBadge: "-21%",
    unit: "Trái",
    image: "https://images.unsplash.com/photo-1550258987-190a2d41a8ba?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1550258987-190a2d41a8ba?auto=format&fit=crop&w=800&q=80"],
    badge: "Ngọt thanh tự nhiên",
    store: {
      id: "store_vua_trai_cay_sau_thao",
      name: "Vựa Trái Cây Sáu Thảo Miền Tây",
      address: "45 Nguyễn Khang, Yên Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 2.4,
      deliveryTime: "22 - 30 phút",
      rating: 4.8,
      reviewsCount: 192,
      openHours: "07:00 - 22:00",
      totalProducts: 30,
      isVerified: true,
      categoryName: "Hoa quả nhiệt đới miệt vườn",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 154,
    sold: 350,
    stock: 90,
    description: "Dừa xiêm xanh bánh tẻ gọt vỏ kim cương tiện lợi. Nước dừa ngọt lịm thanh mát, cùi dừa non mềm dẻo nạo ăn cực ngon miệng.",
    highlights: ["Đã gọt sẵn chỉ cần cắm ống hút là uống", "Nước dừa ngọt thanh tự nhiên 100%", "Giàu chất điện giải bù nước thể thao"],
    specs: {
      origin: "Giồng Trôm, Bến Tre",
      brand: "Vựa Sáu Thảo",
      weight: "1 trái (300-350ml nước)",
      shelfLife: "10-15 ngày trong tủ mát",
      storage: "Bảo quản lạnh",
      packingStandard: "Bọc màng co thực phẩm từng trái"
    },
    nutrition: {
      servingSize: "100ml nước dừa",
      calories: "19 kcal",
      protein: "0.7 g",
      fat: "0.2 g",
      carbs: "3.7 g",
      minerals: "Chất điện giải Kali, Natri, Magie"
    },
    cookingTips: ["Ướp lạnh trước 30 phút uống sảng khoái tột đỉnh."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 4: Thực Phẩm Tươi Sống ZoneMart Cầu Giấy
  // ==========================================
  {
    id: "p13",
    name: "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng USDA Choice",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 185000,
    oldPrice: 220000,
    discountBadge: "-16%",
    unit: "Khay 500g",
    image: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80",
    gallery: [
      "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80"
    ],
    badge: "Thịt mát chuẩn USDA",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "USDA-VN-88429/2026",
      issuedBy: "Cục Thú Y & Kiểm Dịch",
      issuedDate: "15/01/2026",
      expiryDate: "15/01/2027"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 184,
    sold: 560,
    stock: 95,
    description: "Thịt bò Black Angus nhập khẩu Mỹ chuẩn hạng USDA Choice. Vân mỡ cẩm thạch xen kẽ đều tăm tắp, thịt mềm mọng tan trong miệng khi áp chảo hay nướng.",
    highlights: ["100% Thịt bò Black Angus tươi ngon", "Bảo quản công nghệ mát MAP 0-4°C", "Cắt lát dày tiêu chuẩn bít tết hoặc nhúng lẩu"],
    specs: {
      origin: "Hoa Kỳ (Nebraska Farms)",
      brand: "ZoneMart Premium Beef",
      weight: "500g",
      shelfLife: "7 ngày ngăn mát, 6 tháng ngăn đông",
      storage: "Nhiệt độ 0-4°C",
      packingStandard: "Khay sinh học kháng khuẩn hút chân không"
    },
    nutrition: {
      servingSize: "100g",
      calories: "217 kcal",
      protein: "26.1 g",
      fat: "11.8 g",
      carbs: "0 g",
      minerals: "Sắt, Kẽm, Vitamin B12"
    },
    cookingTips: ["Áp chảo 2-3 phút mỗi mặt với bơ tỏi và lá hương thảo."],
    reviews: []
  },
  {
    id: "p14",
    name: "Cá Hồi Na Uy Tươi Phi Lê Cắt Miếng Trong Ngày",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 195000,
    oldPrice: 235000,
    discountBadge: "-17%",
    unit: "Khay 300g",
    image: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=800&q=80"],
    badge: "Ăn sống Sashimi",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "ATTP-QC-2026-99",
      issuedBy: "Cục An Toàn Thực Phẩm",
      issuedDate: "10/01/2026",
      expiryDate: "10/01/2027"
    },
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 142,
    sold: 320,
    stock: 50,
    description: "Cá hồi Đại Tây Dương nhập khẩu nguyên con từ vùng biển lạnh Na Uy, phi lê tươi mới mỗi sáng tại kho lạnh ZoneMart. Đạt chuẩn ăn gỏi Sashimi.",
    highlights: ["Thịt cá cam óng vân mỡ trắng ngần", "Giàu Omega-3 và DHA tự nhiên", "Kèm gừng hồng Nhật và nước tương Kikkoman"],
    specs: {
      origin: "Na Uy (Norwegian Salmon)",
      brand: "ZoneMart Seafood",
      weight: "Khay 300g",
      shelfLife: "3 ngày ngăn mát, 3 tháng ngăn đông",
      storage: "0-2°C trong ngăn mát",
      packingStandard: "Khay lót giấy thấm hút chân không"
    },
    nutrition: {
      servingSize: "100g",
      calories: "208 kcal",
      protein: "20.4 g",
      fat: "13.4 g (Omega-3 2.5g)",
      carbs: "0 g",
      minerals: "Vitamin D, B12, Selen"
    },
    cookingTips: ["Ăn sống chấm wasabi hoặc áp chảo sốt bơ chanh cực đỉnh."],
    reviews: []
  },
  {
    id: "p15",
    name: "Thịt Gà Ta Thả Vườn Làm Sạch Nguyên Con",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 160000,
    oldPrice: 195000,
    discountBadge: "-18%",
    unit: "Con 1.4kg",
    image: "https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=800&q=80"],
    badge: "Da vàng thịt săn",
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 115,
    sold: 280,
    stock: 40,
    description: "Gà ta thả đồi Ba Vì ăn ngô thóc, thịt săn chắc, thơm ngọt và da vàng giòn sần sật. Đã mổ moi làm sạch lông khử khuẩn đóng túi lạnh.",
    highlights: ["Không ăn cám tăng trọng, nuôi tự nhiên 6 tháng", "Thịt thơm không bị bở nát", "Tặng kèm lá chanh tươi và muối tiêu hảo hạng"],
    specs: {
      origin: "Ba Vì, Hà Nội",
      brand: "Gà Đồi Ba Vì",
      weight: "1.3kg - 1.5kg / con",
      shelfLife: "3 ngày ngăn mát, 1 tháng ngăn đông",
      storage: "Bảo quản 0-4°C",
      packingStandard: "Đóng túi hút chân không"
    },
    nutrition: {
      servingSize: "100g thịt gà",
      calories: "165 kcal",
      protein: "31.0 g",
      fat: "3.6 g",
      carbs: "0 g",
      minerals: "Kẽm, Magie, Vitamin B6"
    },
    cookingTips: ["Luộc lửa nhỏ 20 phút cùng vài lát gừng, ngâm nước đá cho da giòn."],
    reviews: []
  },
  {
    id: "p16",
    name: "Cánh Gà Tươi CP Loại 1 Đóng Khay Tiệt Trùng",
    category: "food",
    categoryName: "Thực phẩm tươi",
    price: 72000,
    oldPrice: 85000,
    discountBadge: "-15%",
    unit: "Khay 500g",
    image: "https://images.unsplash.com/photo-1567620832903-9fc6debc209f?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1567620832903-9fc6debc209f?auto=format&fit=crop&w=800&q=80"],
    badge: "Chuẩn tiệt trùng CP",
    store: {
      id: "store_zonemart_cau_giay",
      name: "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
      address: "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
      distanceKm: 1.2,
      deliveryTime: "15 - 20 phút",
      rating: 5.0,
      reviewsCount: 420,
      openHours: "06:00 - 22:00",
      totalProducts: 65,
      isVerified: true,
      categoryName: "Thịt cá tươi & Hải sản lạnh",
      avatar: "https://images.unsplash.com/photo-1578916171728-46686eac8d58?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 160,
    sold: 390,
    stock: 65,
    description: "Cánh gà tươi giữa khúc ngon nhất, da mỏng ít mỡ. Thích hợp làm món cánh gà chiên mắm, nướng mật ong hay sốt me chua cay.",
    highlights: ["Chuỗi khép kín 3F Feed-Farm-Food CP", "Tươi mới không đông lạnh nhiều lần", "Đều cánh, thịt dày mọng nước"],
    specs: {
      origin: "Việt Nam (Hệ thống CP)",
      brand: "CP Fresh Meat",
      weight: "Khay 500g (5-6 khúc cánh)",
      shelfLife: "5 ngày ngăn mát",
      storage: "Nhiệt độ 0-4°C",
      packingStandard: "Khay MAP kín khí"
    },
    nutrition: {
      servingSize: "100g",
      calories: "203 kcal",
      protein: "18.3 g",
      fat: "14.4 g",
      carbs: "0 g",
      minerals: "Collagen, Phốt pho, Sắt"
    },
    cookingTips: ["Khứa nhẹ cánh ướp mắm tỏi ớt 20 phút chiên giòn rụm."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 5: Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba
  // ==========================================
  {
    id: "p17",
    name: "Cơm Tấm Sườn Bì Chả Đặc Biệt Kèm Canh Chua",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 55000,
    oldPrice: 65000,
    discountBadge: "-15%",
    unit: "Suất",
    image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80"],
    badge: "Bán chạy giờ trưa",
    certification: {
      type: "ATTP Quốc Gia",
      certNo: "ATTP-HN-5512",
      issuedBy: "Chi Cục ATTP Hà Nội",
      issuedDate: "15/12/2025",
      expiryDate: "15/12/2026"
    },
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 220,
    sold: 680,
    stock: 80,
    description: "Đĩa cơm tấm gạo thơm dẻo, miếng sườn cốt lết nướng than hoa mật ong xém cạnh thơm nức mũi, chả trứng chưng vàng óng, bì sợi giòn dai và mỡ hành tóp mỡ béo ngậy.",
    highlights: ["Sườn ướp công thức gia truyền nướng than hoa", "Gạo tấm thơm chuẩn vị Sài Gòn", "Đóng hộp giữ nhiệt giao nóng hổi"],
    specs: {
      origin: "Chế biến tươi tại Bếp Cô Ba Cầu Giấy",
      brand: "Cơm Tấm Cô Ba",
      weight: "Suất 550g",
      shelfLife: "Dùng ngon trong 45 phút",
      storage: "Dùng ngay khi còn nóng",
      packingStandard: "Hộp bã mía giữ nhiệt 3 ngăn sạch sẽ"
    },
    nutrition: {
      servingSize: "1 suất đầy đủ",
      calories: "680 kcal",
      protein: "32 g",
      fat: "24 g",
      carbs: "82 g",
      minerals: "Đầy đủ dưỡng chất năng lượng"
    },
    cookingTips: ["Chan nước mắm ớt tỏi chua ngọt ngập miếng sườn để vị đậm đà nhất."],
    reviews: []
  },
  {
    id: "p18",
    name: "Cơm Gà Xối Mỡ Da Giòn Nóng Hổi Sốt Tỏi Ớt",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 50000,
    oldPrice: 60000,
    discountBadge: "-17%",
    unit: "Suất",
    image: "https://images.unsplash.com/photo-1562967914-608f82629710?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1562967914-608f82629710?auto=format&fit=crop&w=800&q=80"],
    badge: "Da giòn thịt mềm",
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.7,
    reviewsCount: 185,
    sold: 510,
    stock: 75,
    description: "Đùi gà góc tư chiên xối mỡ giòn rụm lớp da ngoài, thịt bên trong mềm ngọt mọng nước chấm sốt tỏi ớt đặc chế ăn cùng cơm đảo cà chua đỏ au thơm nức.",
    highlights: ["Gà xối mỡ da giòn rụm không ngấy dầu", "Cơm rang tơi xốp đỏ cam màu cà chua", "Kèm dưa chuột muối chua và canh rong biển"],
    specs: {
      origin: "Bếp Cô Ba Hà Nội",
      brand: "Cơm Tấm Cô Ba",
      weight: "Suất 500g",
      shelfLife: "Dùng nóng trong 45 phút",
      storage: "Dùng liền khi nhận",
      packingStandard: "Hộp giấy giữ nhiệt thân thiện môi trường"
    },
    nutrition: {
      servingSize: "1 suất",
      calories: "650 kcal",
      protein: "35 g",
      fat: "22 g",
      carbs: "75 g",
      minerals: "Protein, Sắt, Kẽm"
    },
    cookingTips: ["Chấm sốt ớt chua cay tự làm của quán để giải ngấy tuyệt đối."],
    reviews: []
  },
  {
    id: "p19",
    name: "Canh Cua Rau Đay Cà Pháo Nấu Nồi Đất Chuẩn Vị",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 40000,
    oldPrice: 50000,
    discountBadge: "-20%",
    unit: "Tô lớn",
    image: "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80"],
    badge: "Món ngon quê nhà",
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 130,
    sold: 290,
    stock: 45,
    description: "Cua đồng giã tay đóng gạch dày cộp nổi tảng, nấu với rau đay, mướp hương thơm nức mũi. Kèm bát cà pháo giòn tan chống ngấy ngày hè.",
    highlights: ["100% Cua đồng tự nhiên giã tay", "Gạch cua đóng bánh dày ngọt lịm", "Kèm hũ cà muối giòn tan truyền thống"],
    specs: {
      origin: "Bếp Cô Ba Hà Nội",
      brand: "Cơm Tấm Cô Ba",
      weight: "Tô 600ml kèm cà pháo",
      shelfLife: "Dùng trong ngày",
      storage: "Ăn nóng",
      packingStandard: "Tô giấy giữ nhiệt chịu nhiệt cao"
    },
    nutrition: {
      servingSize: "1 tô",
      calories: "180 kcal",
      protein: "14 g",
      fat: "6 g",
      carbs: "18 g",
      minerals: "Canxi tự nhiên dồi dào"
    },
    cookingTips: ["Ăn kèm cơm trắng nóng hổi và vài quả cà pháo giòn tan."],
    reviews: []
  },
  {
    id: "p20",
    name: "Trà Tắc Mật Ong Khổng Lồ Mát Lạnh Giải Nhiệt",
    category: "beverage",
    categoryName: "Đồ uống",
    price: 18000,
    oldPrice: 25000,
    discountBadge: "-28%",
    unit: "Ly 700ml",
    image: "https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80"],
    badge: "Siêu giải nhiệt",
    store: {
      id: "store_bep_com_co_ba",
      name: "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
      address: "56 Nguyễn Chánh, Trung Hòa, Cầu Giấy, Hà Nội",
      distanceKm: 3.1,
      deliveryTime: "20 - 30 phút",
      rating: 4.7,
      reviewsCount: 310,
      openHours: "09:30 - 21:00",
      totalProducts: 25,
      isVerified: true,
      categoryName: "Cơm niêu & Món ăn gia đình nóng",
      avatar: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 245,
    sold: 740,
    stock: 120,
    description: "Trà lài ủ lạnh thơm nức pha cùng nước cốt tắc tươi và mật ong hoa rừng nguyên chất. Chua ngọt sảng khoái, đập tan cơn khát ngày hè oi ả.",
    highlights: ["Mật ong rừng tự nhiên thanh dịu", "Tắc tươi vắt liền không đắng vỏ", "Cốc khổng lồ 700ml uống thỏa thích"],
    specs: {
      origin: "Bếp Cô Ba Hà Nội",
      brand: "Cô Ba Drinks",
      weight: "Ly 700ml",
      shelfLife: "Uống ngon nhất trong 3 tiếng",
      storage: "Để lạnh",
      packingStandard: "Ly PP nắp kín dán màng ép nhiệt"
    },
    nutrition: {
      servingSize: "1 ly",
      calories: "120 kcal",
      protein: "0.5 g",
      fat: "0 g",
      carbs: "30 g",
      minerals: "Vitamin C, Chất chống oxy hóa EGCG"
    },
    cookingTips: ["Lắc đều với đá lạnh trước khi cắm ống hút thưởng thức."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 6: Tiệm Bánh Mì & Cà Phê Zone Sáng
  // ==========================================
  {
    id: "p21",
    name: "Combo Bánh Mì Chảo Nóng Hổi Kèm Pate & Xúc Xích",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 45000,
    oldPrice: 55000,
    discountBadge: "-18%",
    unit: "Phần",
    image: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80"],
    badge: "Ăn sáng điểm 10",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 180,
    sold: 490,
    stock: 60,
    description: "Chảo bánh mì nóng hổi gồm trứng ốp la lòng đào, pate gan Hải Phòng tự làm béo ngậy, xúc xích giòn rụm và sốt cà chua đậm đà kèm bánh mì nướng giòn tan.",
    highlights: ["Pate nhà làm thơm bùi ngậy béo", "Bánh mì nướng nóng hổi giòn rụm", "Giao nhanh chỉ 15 phút kịp giờ làm"],
    specs: {
      origin: "Xuân Thủy, Cầu Giấy, Hà Nội",
      brand: "Tiệm Bánh Mì Zone",
      weight: "Phần 450g",
      shelfLife: "30-40 phút",
      storage: "Ăn nóng ngay",
      packingStandard: "Hộp bã mía giữ nhiệt cao cấp"
    },
    nutrition: {
      servingSize: "1 suất",
      calories: "520 kcal",
      protein: "24 g",
      fat: "22 g",
      carbs: "58 g",
      minerals: "Canxi, Sắt, Vitamin A"
    },
    cookingTips: ["Chấm bánh mì ngập sốt pate trứng lòng đào béo ngậy."],
    reviews: []
  },
  {
    id: "p22",
    name: "Bánh Mì Kẹp Thịt Nướng Ngũ Vị Giòn Rụm",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 28000,
    oldPrice: 35000,
    discountBadge: "-20%",
    unit: "Ổ",
    image: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"],
    badge: "Siêu đắt khách",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 230,
    sold: 820,
    stock: 150,
    description: "Ổ bánh mì đặc ruột vỏ giòn tan kẹp ngập tràn thịt nạc vai nướng sả ngũ vị thơm lừng, dưa leo giòn mát, đồ chua chua ngọt và sốt bơ trứng nhà làm.",
    highlights: ["Thịt nướng xém cạnh thơm nức mũi", "Pate bơ mịn màng béo thơm", "Bánh giòn không bị vụn nát"],
    specs: {
      origin: "Cầu Giấy, Hà Nội",
      brand: "Tiệm Bánh Mì Zone",
      weight: "Ổ 220g",
      shelfLife: "Ăn ngon trong 2 tiếng",
      storage: "Nhiệt độ phòng",
      packingStandard: "Túi giấy thực phẩm thân thiện môi trường"
    },
    nutrition: {
      servingSize: "1 ổ",
      calories: "410 kcal",
      protein: "19 g",
      fat: "14 g",
      carbs: "52 g",
      minerals: "Protein, Chất xơ"
    },
    cookingTips: ["Ăn kèm chút tương ớt cay nồng bùng nổ hương vị."],
    reviews: []
  },
  {
    id: "p23",
    name: "Cà Phê Sữa Đá Pha Phin Đậm Đà Chuẩn Gu",
    category: "beverage",
    categoryName: "Đồ uống",
    price: 22000,
    oldPrice: 28000,
    discountBadge: "-21%",
    unit: "Cốc 450ml",
    image: "https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?auto=format&fit=crop&w=800&q=80"],
    badge: "Chuẩn vị pha phin",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 190,
    sold: 610,
    stock: 100,
    description: "Hạt Robusta Buôn Ma Thuột rang mộc mạc pha phin nhôm truyền thống, hòa quyện sữa đặc béo ngậy. Đậm đà đánh thức mọi giác quan bắt đầu ngày làm việc mới.",
    highlights: ["100% Cà phê nguyên chất không tẩm bắp", "Vị đắng đầm ấm hậu ngọt sâu", "Đá riêng đảm bảo cà phê không bị nhạt"],
    specs: {
      origin: "Buôn Ma Thuột, Đắk Lắk",
      brand: "Zone Morning Coffee",
      weight: "Cốc 450ml",
      shelfLife: "Uống ngon nhất trong ngày",
      storage: "Bảo quản lạnh",
      packingStandard: "Cốc giấy nắp tim quấn màng bảo vệ"
    },
    nutrition: {
      servingSize: "1 cốc",
      calories: "160 kcal",
      protein: "3.5 g",
      fat: "4.0 g",
      carbs: "28 g",
      minerals: "Caffeine 120mg"
    },
    cookingTips: ["Khuấy đều đá và sữa để cảm nhận độ sánh mịn đặc trưng."],
    reviews: []
  },
  {
    id: "p24",
    name: "Bánh Bao Trứng Muối Xá Xíu Nóng Hổi Vừa Ra Lò",
    category: "fastfood",
    categoryName: "Món ăn nóng",
    price: 20000,
    oldPrice: 25000,
    discountBadge: "-20%",
    unit: "Cái",
    image: "https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80"],
    badge: "Vỏ xốp nhân ngập",
    store: {
      id: "store_tiem_banh_mi_zone",
      name: "Tiệm Bánh Mì & Cà Phê Zone Sáng",
      address: "18 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
      distanceKm: 1.9,
      deliveryTime: "12 - 18 phút",
      rating: 4.85,
      reviewsCount: 175,
      openHours: "05:30 - 18:00",
      totalProducts: 20,
      isVerified: true,
      categoryName: "Điểm tâm sáng & Cà phê nóng",
      avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 110,
    sold: 340,
    stock: 50,
    description: "Vỏ bánh trắng mịn xốp mềm ngòn ngọt, nhân thịt nạc vai xá xíu đậm đà, mộc nhĩ nấm hương cùng nguyên 1 quả trứng muối bùi bùi béo ngậy.",
    highlights: ["Hấp nóng trực tiếp trong tủ giữ nhiệt", "Nhân đầy đặn không ngấy mỡ", "Ăn sáng vừa nhanh gọn vừa chắc dạ"],
    specs: {
      origin: "Cầu Giấy, Hà Nội",
      brand: "Tiệm Bánh Mì Zone",
      weight: "Cái 180g",
      shelfLife: "Trong ngày",
      storage: "Ăn nóng",
      packingStandard: "Túi giấy giữ nhiệt lót lá chuối"
    },
    nutrition: {
      servingSize: "1 cái",
      calories: "320 kcal",
      protein: "14 g",
      fat: "10 g",
      carbs: "44 g",
      minerals: "Chất đạm, Canxi"
    },
    cookingTips: ["Dùng ngay khi bánh còn bốc khói ngun ngút."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 7: Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc
  // ==========================================
  {
    id: "p25",
    name: "Mật Ong Rừng Hoa Cà Phê Gia Lai Nguyên Chất 100%",
    category: "food",
    categoryName: "Đặc sản vùng cao",
    price: 135000,
    oldPrice: 165000,
    discountBadge: "-18%",
    unit: "Chai 500ml",
    image: "https://images.unsplash.com/photo-1587049352851-8d4e89133924?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1587049352851-8d4e89133924?auto=format&fit=crop&w=800&q=80"],
    badge: "OCOP 4 Sao",
    certification: {
      type: "OCOP 4 Sao",
      certNo: "OCOP-GL-2026-88",
      issuedBy: "UBND Tỉnh Gia Lai",
      issuedDate: "18/01/2026",
      expiryDate: "18/01/2029"
    },
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 135,
    sold: 375,
    stock: 70,
    description: "Mật ong hoa cà phê nguyên chất mùa hoa nở rộ đất Tây Nguyên. Màu vàng cánh gián óng ả, vị ngọt sắc thanh dịu không gắt cổ, đặc quánh thơm lừng.",
    highlights: ["Không pha đường, không đun nấu cô đặc", "Chai thủy tinh nút bần cao cấp", "Bảo quản tự nhiên không biến đổi chất"],
    specs: {
      origin: "Ia Grai, Gia Lai",
      brand: "HTX Tây Bắc Farm",
      weight: "Chai thủy tinh 500ml",
      shelfLife: "2 năm",
      storage: "Để nơi thoáng mát, không để tủ lạnh",
      packingStandard: "Chai thủy tinh niêm phong nắp thiếc"
    },
    nutrition: {
      servingSize: "1 muỗng (20g)",
      calories: "64 kcal",
      protein: "0.1 g",
      fat: "0 g",
      carbs: "17.3 g",
      minerals: "Chất kháng khuẩn tự nhiên, Enzyme"
    },
    cookingTips: ["Pha với nước ấm và chanh tươi uống buổi sáng thanh lọc đường ruột."],
    reviews: []
  },
  {
    id: "p26",
    name: "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
    category: "food",
    categoryName: "Nhu yếu phẩm",
    price: 175000,
    oldPrice: 210000,
    discountBadge: "-17%",
    unit: "Túi 5kg",
    image: "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=800&q=80"],
    badge: "Gạo ngon nhất thế giới",
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 5.0,
    reviewsCount: 290,
    sold: 820,
    stock: 110,
    description: "Gạo ST25 lúa tôm Sóc Trăng chuẩn tem chống giả. Hạt gạo dài trắng trong, khi nấu tỏa hương lá dứa thơm nức, cơm dẻo dai ngọt hạt dù để nguội.",
    highlights: ["Gạo đạt danh hiệu Gạo ngon nhất thế giới", "Canh tác luân canh lúa - tôm sạch an toàn", "Bao bì chống ẩm giữ trọn hương vị"],
    specs: {
      origin: "Mỹ Xuyên, Sóc Trăng",
      brand: "Gạo Ông Cua Chính Hãng",
      weight: "Túi 5kg",
      shelfLife: "12 tháng",
      storage: "Bảo quản nơi khô ráo, tránh ẩm ướt",
      packingStandard: "Túi màng nhôm bảo quản có khóa zip"
    },
    nutrition: {
      servingSize: "100g gạo",
      calories: "349 kcal",
      protein: "8.0 g",
      fat: "0.8 g",
      carbs: "77.5 g",
      minerals: "Chỉ số đường huyết GI thấp"
    },
    cookingTips: ["Đong tỉ lệ 1 bát gạo : 1 bát nước, không cần ngâm gạo trước khi nấu."],
    reviews: []
  },
  {
    id: "p27",
    name: "Nấm Hương Rừng Sa Pa Thơm Nức Phơi Tự Nhiên",
    category: "food",
    categoryName: "Nông sản khô",
    price: 85000,
    oldPrice: 110000,
    discountBadge: "-23%",
    unit: "Gói 200g",
    image: "https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&w=800&q=80"],
    badge: "Nấm hương chân nhỏ",
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 82,
    sold: 165,
    stock: 45,
    description: "Nấm hương rừng mọc tự nhiên trên thân cây gỗ mục vùng cao Sa Pa. Cánh nấm dày, viền uốn cong, khi ngâm nở thơm ngát đặc trưng không nấm trồng nào sánh được.",
    highlights: ["Phơi nắng tự nhiên trên sườn đồi Hoàng Liên", "Thịt nấm giòn ngọt thơm nức mũi", "Nấu canh măng, xào gà hay nhồi thịt đều đỉnh"],
    specs: {
      origin: "Sa Pa, Lào Cai",
      brand: "HTX Tây Bắc Farm",
      weight: "Gói 200g",
      shelfLife: "18 tháng",
      storage: "Nơi khô ráo",
      packingStandard: "Túi zip tráng bạc chống ẩm"
    },
    nutrition: {
      servingSize: "100g",
      calories: "296 kcal",
      protein: "14.5 g",
      fat: "1.2 g",
      carbs: "65.0 g",
      minerals: "Lentinan, Vitamin D tự nhiên"
    },
    cookingTips: ["Ngâm nước ấm 20 phút, giữ lại nước ngâm nấm để nấu canh ngọt lịm."],
    reviews: []
  },
  {
    id: "p28",
    name: "Thịt Trâu Gác Bếp Sơn La Chuẩn Vị Hạt Dổi Mắc Khén",
    category: "food",
    categoryName: "Đặc sản vùng cao",
    price: 240000,
    oldPrice: 290000,
    discountBadge: "-17%",
    unit: "Gói 300g",
    image: "https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?auto=format&fit=crop&w=800&q=80"],
    badge: "Đặc sản Tây Bắc",
    store: {
      id: "store_dac_san_tay_bac",
      name: "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
      address: "88 Lê Đức Thọ, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
      distanceKm: 4.3,
      deliveryTime: "30 - 40 phút",
      rating: 4.9,
      reviewsCount: 148,
      openHours: "07:30 - 21:30",
      totalProducts: 32,
      isVerified: true,
      categoryName: "Đặc sản rừng & Nhu yếu phẩm",
      avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 145,
    sold: 290,
    stock: 35,
    description: "Thịt bắp trâu tươi ướp mắc khén, hạt dổi rừng, ớt bột và hun khói than củi nhãn theo bí quyết người Thái đen. Từng thớ thịt đỏ hồng đượm khói, xé sợi chấm chẩm chéo cực đã.",
    highlights: ["100% Thịt bắp trâu tươi tươi ngon", "Hun khói củi nhãn truyền thống", "Tặng kèm hũ chẩm chéo ướt Tây Bắc"],
    specs: {
      origin: "Mộc Châu, Sơn La",
      brand: "HTX Tây Bắc Farm",
      weight: "Gói 300g hút chân không",
      shelfLife: "6 tháng ngăn đông",
      storage: "Ngăn đá tủ lạnh",
      packingStandard: "Hút chân không túi đôi"
    },
    nutrition: {
      servingSize: "100g",
      calories: "240 kcal",
      protein: "48 g",
      fat: "4.5 g",
      carbs: "2 g",
      minerals: "Giàu đạm, ít chất béo"
    },
    cookingTips: ["Quay lò vi sóng 30 giây hoặc hấp cách thủy 5 phút cho mềm rồi đập dập xé sợi."],
    reviews: []
  },

  // ==========================================
  // GIAN HÀNG 8: Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình
  // ==========================================
  {
    id: "p29",
    name: "Bộ Nồi Inox 3 Đáy Cao Cấp Nấu Mọi Loại Bếp Từ",
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
    badge: "Bảo hành 5 năm",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.75,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Hóa phẩm hữu cơ",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 118,
    sold: 190,
    stock: 25,
    description: "Bộ 3 nồi Inox SUS304 chuẩn an toàn sức khỏe kích thước 16-20-24cm. Đáy 3 lớp truyền nhiệt nhanh, tỏa nhiệt đều, chống cháy khét thức ăn trên bếp từ và bếp gas.",
    highlights: ["Chất liệu Inox 304 sáng bóng không gỉ sét", "Đáy từ bắt nhiệt siêu nhạy", "Vung kính cường lực viền inox sang trọng"],
    specs: {
      origin: "Việt Nam (Tiêu chuẩn xuất khẩu Châu Âu)",
      brand: "Mỹ Đình Smart Home",
      weight: "Bộ 3 nồi kèm vung (3.8kg)",
      shelfLife: "Độ bền trên 10 năm",
      storage: "Vệ sinh lau khô sau khi sử dụng",
      packingStandard: "Thùng carton chèn xốp định hình"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Chứng nhận ATTP LFGB Đức"
    },
    cookingTips: ["Dùng giẻ mềm lau rửa để giữ độ bóng loáng như gương."],
    reviews: []
  },
  {
    id: "p30",
    name: "Nước Rửa Bát Hữu Cơ Tinh Dầu Quế & Chanh Gừng",
    category: "household",
    categoryName: "Đồ dùng gia đình",
    price: 48000,
    oldPrice: 60000,
    discountBadge: "-20%",
    unit: "Chai 800ml",
    image: "https://images.unsplash.com/photo-1585421514738-01798e348b17?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1585421514738-01798e348b17?auto=format&fit=crop&w=800&q=80"],
    badge: "Bảo vệ da tay",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.75,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Hóa phẩm hữu cơ",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.8,
    reviewsCount: 165,
    sold: 430,
    stock: 85,
    description: "Nước rửa chén chiết xuất từ enzyme bồ hòn lên men tự nhiên và tinh dầu quế ấm nồng. Đánh bay dầu mỡ cứng đầu, khử tanh triệt để và an toàn cho da tay nhạy cảm.",
    highlights: ["Không hóa chất độc hại, bọt xà phòng sinh học", "Hương quế chanh dễ chịu khử sạch mùi tanh", "Dùng được cho cả đồ ăn dặm của em bé"],
    specs: {
      origin: "Hà Nội, Việt Nam",
      brand: "EcoZone Organic",
      weight: "Chai 800ml vòi nhấn tiện lợi",
      shelfLife: "24 tháng",
      storage: "Tránh ánh nắng trực tiếp",
      packingStandard: "Chai nhựa tái chế thân thiện môi trường"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Đạt chuẩn sinh học Ecocert"
    },
    cookingTips: ["Nhấn 1 giọt ra miếng bọt biển ướt tạo bọt xốp nhẹ nhàng."],
    reviews: []
  },
  {
    id: "p31",
    name: "Chảo Chống Dính Vân Đá Ceramic Đáy Từ Siêu Bền",
    category: "household",
    categoryName: "Đồ gia dụng",
    price: 185000,
    oldPrice: 230000,
    discountBadge: "-20%",
    unit: "Chiếc 26cm",
    image: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=800&q=80"],
    badge: "Chống dính vân đá",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.75,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Hóa phẩm hữu cơ",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.7,
    reviewsCount: 94,
    sold: 260,
    stock: 35,
    description: "Chảo chiên rán đường kính 26cm phủ lớp men gốm Ceramic vân đá hoa cương chống trầy xước. Chiên trứng không cần dầu mỡ, tay cầm cách nhiệt chống bỏng an toàn.",
    highlights: ["Lớp chống dính Ceramic không chứa chì hay PFOA", "Đáy từ đúc nguyên khối chống phồng đáy", "Dễ dàng lau chùi chỉ với một chiếc khăn mềm"],
    specs: {
      origin: "Việt Nam",
      brand: "Mỹ Đình Smart Home",
      weight: "850g",
      shelfLife: "Bảo hành 24 tháng",
      storage: "Treo nơi khô ráo",
      packingStandard: "Hộp giấy bảo vệ chuyên dụng"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Chứng chỉ FDA Hoa Kỳ"
    },
    cookingTips: ["Dùng thìa gỗ hoặc muôi silicon để bảo vệ lớp chống dính bền lâu."],
    reviews: []
  },
  {
    id: "p32",
    name: "Túi Rác Tự Hủy Sinh Học Thân Thiện Môi Trường",
    category: "household",
    categoryName: "Đồ dùng gia đình",
    price: 35000,
    oldPrice: 45000,
    discountBadge: "-22%",
    unit: "Cuộn 3 túi",
    image: "https://images.unsplash.com/photo-1530587191325-3db32d826c18?auto=format&fit=crop&w=800&q=80",
    gallery: ["https://images.unsplash.com/photo-1530587191325-3db32d826c18?auto=format&fit=crop&w=800&q=80"],
    badge: "Bảo vệ môi trường",
    store: {
      id: "store_tong_kho_my_dinh",
      name: "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
      address: "102 Hàm Nghi, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
      distanceKm: 5.2,
      deliveryTime: "35 - 45 phút",
      rating: 4.75,
      reviewsCount: 215,
      openHours: "08:00 - 22:30",
      totalProducts: 50,
      isVerified: true,
      categoryName: "Đồ gia dụng & Hóa phẩm hữu cơ",
      avatar: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=200&q=80",
      coverImage: "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80"
    },
    rating: 4.9,
    reviewsCount: 145,
    sold: 610,
    stock: 120,
    description: "Bộ 3 cuộn túi rác tự hủy sinh học làm từ tinh bột ngô thiên nhiên. Dẻo dai dai bền, đáy xếp hình sao chống rò rỉ nước rác và tự phân hủy sau 180 ngày chôn lấp.",
    highlights: ["Chất liệu dẻo dai khó rách thủng", "Đường cắt xé tiện lợi, đáy kín nước", "Thân thiện 100% với môi trường sống"],
    specs: {
      origin: "Việt Nam",
      brand: "EcoZone Organic",
      weight: "Lốc 3 cuộn (1kg - cỡ 55x65cm)",
      shelfLife: "3 năm",
      storage: "Nơi khô ráo",
      packingStandard: "Lốc bọc màng co"
    },
    nutrition: {
      servingSize: "Không áp dụng",
      calories: "0",
      protein: "0",
      fat: "0",
      carbs: "0",
      minerals: "Chứng nhận TUV Austria OK Biobased"
    },
    cookingTips: ["Dùng lót thùng rác phòng khách, bếp hoặc văn phòng làm việc."],
    reviews: []
  }
];

export function useProductCatalog() {
  const moderation = useProductModeration();

  // Danh sách tổng hợp toàn bộ sản phẩm (Gồm catalog tĩnh + sản phẩm do Seller đăng đã được duyệt)
  const allProducts = computed<CatalogProduct[]>(() => {
    // Đảm bảo phụ thuộc phản ứng vào catalogVersion để cập nhật tức thì
    void catalogVersion.value;
    const dynamicActive = moderation.activeProducts.value.map((p) => {
      let cat: "food" | "veggie" | "fastfood" | "beverage" | "household" = "food";
      if (p.category === "Rau củ quả") cat = "veggie";
      else if (p.category === "Thịt cá tươi") cat = "food";
      else if (p.category === "Trái cây tươi") cat = "beverage";
      else if (p.category === "Món ăn nóng") cat = "fastfood";
      else cat = "household";

      const cleanEmail = (p.sellerEmail || "").toLowerCase().trim();
      let storeAddr = "36 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội";
      let storeHours = "06:00 - 20:30";
      let storeAvatar = "https://images.unsplash.com/photo-1595273670150-bd0c3c392e46?auto=format&fit=crop&w=200&q=80";

      if (cleanEmail && typeof localStorage !== "undefined") {
        try {
          const rawStore = localStorage.getItem(`zonemart_seller_store_${cleanEmail}`);
          if (rawStore) {
            const parsed = JSON.parse(rawStore);
            if (parsed.address) storeAddr = parsed.address;
            if (parsed.openHours) storeHours = parsed.openHours;
          }
        } catch {}
      }

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
          name: p.storeName || "Vườn Rau Hữu Cơ Bác Ba",
          address: storeAddr,
          distanceKm: 0.8,
          deliveryTime: "12 - 18 phút",
          rating: 5.0,
          reviewsCount: 384,
          openHours: storeHours,
          totalProducts: 42,
          isVerified: true,
          categoryName: p.category || "Rau củ hữu cơ VietGAP",
          avatar: storeAvatar,
          coverImage: p.image || "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
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

  // Danh sách toàn bộ gian hàng trích xuất tự động từ sản phẩm - Đảm bảo giữ trọn vẹn thông số riêng của từng quán
  const allStores = computed<CatalogStore[]>(() => {
    const storeMap = new Map<string, CatalogStore>();

    for (const p of allProducts.value) {
      if (!p.store || !p.store.name) continue;
      const key = p.store.name.trim();

      if (!storeMap.has(key)) {
        storeMap.set(key, {
          id: p.store.id || `store_${key.toLowerCase().replace(/[^a-z0-9]/g, '_')}`,
          name: p.store.name,
          address: p.store.address || "Hà Nội",
          distanceKm: p.store.distanceKm || 1.5,
          deliveryTime: p.store.deliveryTime || "15 - 20 phút",
          rating: p.store.rating || 5.0,
          reviewsCount: p.store.reviewsCount || p.reviewsCount || 48,
          totalProducts: p.store.totalProducts || 1,
          isVerified: p.store.isVerified !== false,
          categoryName: p.store.categoryName || p.categoryName || "Thực phẩm tươi",
          avatar: p.store.avatar || "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=200&q=80",
          coverImage: p.store.coverImage || p.image,
          openHours: p.store.openHours || "07:00 - 22:00",
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
      CATALOG_PRODUCTS[0]
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

  const refreshCatalog = () => {
    moderation.refreshProducts();
    catalogVersion.value++;
  };

  return {
    allProducts,
    allStores,
    getProductById,
    getRelatedProducts,
    getStoreByIdOrName,
    refreshCatalog,
  };
}

// Lắng nghe sự kiện toàn cục khi sản phẩm thay đổi để tự động re-compute catalog
if (typeof window !== "undefined") {
  window.addEventListener("zonemart:products-changed", () => {
    catalogVersion.value++;
  });
}
