/**
 * ================================================================
 * CANONICAL STORE BRANDING & AVATAR REGISTRY (ZoneMart)
 * ================================================================
 * Chuẩn hóa 100% hình ảnh đại diện (Avatar) và ảnh bìa (Cover Image)
 * cho toàn bộ các gian hàng trong hệ thống ZoneMart.
 * Đảm bảo đồng nhất tuyệt đối giữa:
 * - Trang Chủ (HomeView - Gian Hàng Đối Tác Tiêu Biểu)
 * - Trang Danh Sách Sản Phẩm (ProductListView - Tab Gian Hàng & Banner Tiêu Điểm)
 * - Trang Chi Tiết Sản Phẩm (ProductDetailView - Thẻ Gian Hàng)
 * - Kênh Quản Lý Người Bán (SellerDashboardView)
 * - Giỏ Hàng & Thanh Toán (CartView, CheckoutView)
 */

export interface StoreBrandingMeta {
  avatar: string;
  coverImage: string;
}

// Bảng ánh xạ Avatar & Ảnh Bìa chuẩn xác cho từng gian hàng thực tế
export const CANONICAL_STORE_BRANDING: Record<string, StoreBrandingMeta> = {
  // 1. Gian Hàng Thử Nghiệm (Test Gian Hàng - baconbao206@gmail.com)
  "test gian hang": {
    avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
  },
  "test gian hàng": {
    avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
  },
  "gian hàng test gian hàng": {
    avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
  },

  // 2. Vườn Rau Hữu Cơ Bác Ba (Ba Vì)
  "vuon rau huu co bac ba": {
    avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
  },
  "vườn rau hữu cơ bác ba": {
    avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
  },
  "vườn rau ba vì - nông sản sạch vietgap": {
    avatar: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
  },

  // 3. Nông Sản Sạch Chúc Sơn – Chương Mỹ
  "nong san sach chuc son - chuong my": {
    avatar: "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1574943320219-553eb213f72d?auto=format&fit=crop&w=800&q=80"
  },
  "nông sản sạch chúc sơn - chương mỹ": {
    avatar: "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1574943320219-553eb213f72d?auto=format&fit=crop&w=800&q=80"
  },
  "nông sản sạch chúc sơn – chương mỹ": {
    avatar: "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1574943320219-553eb213f72d?auto=format&fit=crop&w=800&q=80"
  },

  // 4. Bếp Nóng Yên Nghĩa & Biên Giang (Món ăn nóng & Ẩm thực)
  "bep nong yen nghia & bien giang": {
    avatar: "https://images.unsplash.com/photo-1577219491135-ce391730fb2c?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
  },
  "bếp nóng yên nghĩa & biên giang": {
    avatar: "https://images.unsplash.com/photo-1577219491135-ce391730fb2c?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
  },

  // 5. Vựa Thịt Sạch & Hải Sản Ba La - Hà Đông
  "vua thit sach & hai san ba la - ha dong": {
    avatar: "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80"
  },
  "vựa thịt sạch & hải sản ba la - hà đông": {
    avatar: "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80"
  },

  // 6. Tiệm Trái Cây & Nước Ép Zone Fruits
  "tiem trai cay & nuoc ep zone fruits": {
    avatar: "https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=800&q=80"
  },
  "tiệm trái cây & nước ép zone fruits": {
    avatar: "https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=800&q=80"
  },

  // 7. Nông Trại Xanh Lạc Dương
  "nong trai xanh lac duong": {
    avatar: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
  },
  "nông trại xanh lạc dương": {
    avatar: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80"
  },

  // 8. Vựa Trái Cây Sáu Thảo Miền Tây
  "vua trai cay sau thao mien tay": {
    avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
  },
  "vựa trái cây sáu thảo miền tây": {
    avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80"
  },

  // 9. Thực Phẩm Tươi Sống ZoneMart Cầu Giấy
  "thuc pham tuoi song zonemart cau giay": {
    avatar: "https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
  },
  "thực phẩm tươi sống zonemart cầu giấy": {
    avatar: "https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80"
  },

  // 10. Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba
  "bep com nieu & am thuc nong co ba": {
    avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
  },
  "bếp cơm niêu & ẩm thực nóng cô ba": {
    avatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80"
  },

  // 11. Tiệm Bánh Mì & Cà Phê Zone Sáng
  "tiem banh mi & ca phe zone sang": {
    avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
  },
  "tiệm bánh mì & cà phê zone sáng": {
    avatar: "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80"
  },

  // 12. Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc
  "hop tac xa dac san vung cao tay bac": {
    avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1516253593875-bd7ba052fbc5?auto=format&fit=crop&w=800&q=80"
  },
  "hợp tác xã đặc sản vùng cao tây bắc": {
    avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1516253593875-bd7ba052fbc5?auto=format&fit=crop&w=800&q=80"
  },

  // 13. Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình
  "tong kho do gia dung & tieu dung my dinh": {
    avatar: "https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=800&q=80"
  },
  "tổng kho đồ gia dụng & tiêu dùng mỹ đình": {
    avatar: "https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&w=200&q=80",
    coverImage: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=800&q=80"
  }
};

/**
 * Trích xuất Avatar chuẩn nhất quán của một gian hàng
 */
export function getStoreAvatar(storeName?: string | null, customAvatar?: string | null): string {
  if (!storeName) return "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80";

  const key = storeName.toLowerCase().trim();

  // 1. Kiểm tra trong danh mục chuẩn định danh
  if (CANONICAL_STORE_BRANDING[key]?.avatar) {
    return CANONICAL_STORE_BRANDING[key].avatar;
  }

  // 2. Kiểm tra khớp mờ (chứa tên)
  for (const [k, v] of Object.entries(CANONICAL_STORE_BRANDING)) {
    if (key.includes(k) || k.includes(key)) {
      return v.avatar;
    }
  }

  // 3. Nếu là Test Gian Hàng hoặc người bán đang đăng nhập
  if (key.includes("test gian") || key.includes("test tiệm")) {
    return "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=200&q=80";
  }

  // 4. Ưu tiên customAvatar nếu hợp lệ
  if (customAvatar && typeof customAvatar === "string" && customAvatar.length > 10 && !customAvatar.includes("photo-1542838132")) {
    return customAvatar;
  }

  // 5. Fallback tạo Avatar nhận diện thương hiệu theo chữ cái
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(storeName)}&background=059669&color=fff&bold=true&rounded=true&size=128`;
}

/**
 * Trích xuất Ảnh Bìa (Cover Image) chuẩn nhất quán của một gian hàng
 */
export function getStoreCover(storeName?: string | null, customCover?: string | null): string {
  if (!storeName) return "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80";

  const key = storeName.toLowerCase().trim();

  if (CANONICAL_STORE_BRANDING[key]?.coverImage) {
    return CANONICAL_STORE_BRANDING[key].coverImage;
  }

  for (const [k, v] of Object.entries(CANONICAL_STORE_BRANDING)) {
    if (key.includes(k) || k.includes(key)) {
      return v.coverImage;
    }
  }

  if (customCover && typeof customCover === "string" && customCover.length > 10) {
    return customCover;
  }

  return "https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=800&q=80";
}

