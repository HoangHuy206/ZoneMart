/**
 * ================================================================
 * AVATAR RESOLVER HELPER (ZoneMart)
 * ================================================================
 * Chuẩn hóa và đồng bộ ảnh đại diện nhất quán 100% giữa các phân hệ:
 * Thanh Header Người Mua, Kênh Người Bán, Thẻ Gian Hàng và Cửa Sổ Chat.
 * Loại bỏ triệt để các ảnh mẫu stock ngẫu nhiên gây lệch nhận diện tài khoản.
 */

export function getCleanAvatar(
  avatarUrl?: string | null,
  fallbackName = "User",
  role: "buyer" | "seller" = "buyer"
): string {
  // Nếu có ảnh chụp / ảnh tải lên thực tế hợp lệ (không phải ảnh stock ngẫu nhiên cũ)
  if (
    avatarUrl &&
    typeof avatarUrl === "string" &&
    avatarUrl.trim().length > 8 &&
    !avatarUrl.includes("photo-1534528741775") && // Loại bỏ ảnh stock cô gái ngẫu nhiên
    !avatarUrl.includes("photo-1578916171728") && // Loại bỏ ảnh stock ông đầu bếp
    !avatarUrl.includes("photo-1595273670150")    // Loại bỏ ảnh placeholder cũ
  ) {
    return avatarUrl.trim();
  }

  // Khởi tạo avatar thương hiệu chuẩn xác theo tên tài khoản / sạp hàng
  const cleanName = (fallbackName || (role === "seller" ? "Gian Hàng" : "Khách Hàng")).trim();
  // Mã màu: Cam ấm (#ea580c) cho Người Mua, Xanh ngọc (#059669) cho Gian Hàng
  const bgColor = role === "seller" ? "059669" : "ea580c";
  
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(cleanName)}&background=${bgColor}&color=fff&bold=true&rounded=true&size=128`;
}
