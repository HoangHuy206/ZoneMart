import re
import os

MD_PATH = r"C:\Users\kayy0\.gemini\antigravity\brain\ff1a3665-6bfa-430b-8f81-e5f1fb5e7295\BaoCao_TotNghiep_ZoneMart.md"

with open(MD_PATH, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Update Use Case subsections 2.2.3 to 2.2.19 to include their respective figure captions
usecases = [
    ("### 2.2.3 Use Case đăng nhập", "Hình 2.4: Sơ đồ Use Case đăng nhập."),
    ("### 2.2.4 Use Case đăng ký tài khoản", "Hình 2.5: Sơ đồ Use Case đăng ký tài khoản."),
    ("### 2.2.5 Use Case tìm kiếm, xem sản phẩm", "Hình 2.6: Sơ đồ Use Case tìm kiếm, xem sản phẩm trong bán kính siêu cục bộ <= 3 km."),
    ("### 2.2.6 Use Case quản lý giỏ hàng", "Hình 2.7: Sơ đồ Use Case quản lý giỏ hàng."),
    ("### 2.2.7 Use Case đặt hàng và thanh toán", "Hình 2.8: Sơ đồ Use Case đặt hàng và thanh toán."),
    ("### 2.2.8 Use Case theo dõi đơn mua và gửi yêu cầu hỗ trợ", "Hình 2.9: Sơ đồ Use Case theo dõi đơn mua và gửi yêu cầu hỗ trợ."),
    ("### 2.2.9 Use Case quản lý tài khoản cá nhân", "Hình 2.10: Sơ đồ Use Case quản lý tài khoản cá nhân."),
    ("### 2.2.10 Use Case quản lý sản phẩm", "Hình 2.11: Sơ đồ Use Case quản lý sản phẩm."),
    ("### 2.2.11 Use Case xử lý đơn hàng của người bán", "Hình 2.12: Sơ đồ Use Case xử lý đơn hàng của người bán."),
    ("### 2.2.12 Use Case quản lý gian hàng", "Hình 2.13: Sơ đồ Use Case quản lý gian hàng."),
    ("### 2.2.13 Use Case nhận và giao đơn hàng", "Hình 2.14: Sơ đồ Use Case nhận và giao đơn hàng của tài xế."),
    ("### 2.2.14 Use Case quản lý thu nhập và hồ sơ tài xế", "Hình 2.15: Sơ đồ Use Case quản lý thu nhập và hồ sơ tài xế."),
    ("### 2.2.15 Use Case thẩm định hồ sơ đối tác", "Hình 2.16: Sơ đồ Use Case thẩm định hồ sơ đối tác."),
    ("### 2.2.16 Use Case kiểm duyệt sản phẩm", "Hình 2.17: Sơ đồ Use Case kiểm duyệt sản phẩm."),
    ("### 2.2.17 Use Case quản lý người dùng", "Hình 2.18: Sơ đồ Use Case quản lý người dùng."),
    ("### 2.2.18 Use Case quản lý tài khoản quản lý (ca trực)", "Hình 2.19: Sơ đồ Use Case quản lý tài khoản quản lý."),
    ("### 2.2.19 Use Case thống kê và nhật ký hệ thống", "Hình 2.20: Sơ đồ Use Case thống kê và nhật ký hệ thống.")
]

for heading, fig_caption in usecases:
    if fig_caption not in text:
        # Find next heading or end of block
        pattern = re.escape(heading) + r'([\s\S]*?)(?=\n###|\n##|$)'
        m = re.search(pattern, text)
        if m:
            block = m.group(0)
            new_block = block.rstrip() + f"\n\n{fig_caption}\n"
            text = text.replace(block, new_block, 1)

# 2. Add UI Screenshot figure captions in Chapter 3
ui_items = [
    ("- **Giao diện Đăng nhập đa phương thức (Hình 3.1):**", "Hình 3.1: Giao diện Đăng nhập đa phương thức (Email/SĐT & Face ID)."),
    ("- **Giao diện Đăng ký tài khoản (Hình 3.2):**", "Hình 3.2: Giao diện Đăng ký tài khoản khách hàng."),
    ("- **Giao diện Quên mật khẩu qua Gmail OTP (Hình 3.3):**", "Hình 3.3: Giao diện Quên mật khẩu qua mã OTP gửi về Gmail."),
    ("- **Trang chủ & Bản đồ bán kính lân cận (Hình 3.4):**", "Hình 3.4: Giao diện Trang chủ ZoneMart hiển thị sản phẩm theo khu vực địa lý."),
    ("- **Bộ lọc bán kính siêu cục bộ <= 3 km (Hình 3.5):**", "Hình 3.5: Giao diện Bộ lọc bán kính giao hàng siêu cục bộ <= 3 km trên bản đồ số."),
    ("- **Trang Chi tiết sản phẩm & Chứng nhận AI (Hình 3.6):**", "Hình 3.6: Giao diện Chi tiết sản phẩm với Chứng nhận kiểm duyệt an toàn AI."),
    ("- **Giỏ hàng thông minh (Hình 3.7):**", "Hình 3.7: Giao diện Giỏ hàng thông minh phân nhóm theo gian hàng lân cận."),
    ("- **Giao diện Thanh toán & Quét mã VietQR tự động (Hình 3.8):**", "Hình 3.8: Giao diện Thanh toán tách đơn con & Quét mã VietQR động / SePay."),
    ("- **Trang Theo dõi đơn hàng & Khiếu nại (Hình 3.9):**", "Hình 3.9: Giao diện Theo dõi trạng thái đơn hàng thời gian thực & Bản đồ lộ trình."),
    ("- **Bảng điều khiển Gian hàng (Seller Dashboard - Hình 3.10):**", "Hình 3.10: Giao diện Bảng điều khiển Kênh Người bán (Seller Dashboard)."),
    ("- **Module Đăng sản phẩm tích hợp phản hồi AI tức thời (Hình 3.11):**", "Hình 3.11: Giao diện Đăng bán sản phẩm với phản hồi kiểm duyệt AI trực quan."),
    ("- **Module Xử lý đơn hàng hỏa tốc (Hình 3.12):**", "Hình 3.12: Giao diện Xử lý đơn hàng hỏa tốc tại gian hàng đối tác."),
    ("- **Ra-đa quét đơn hỏa tốc trong bán kính 3 km (Hình 3.13):**", "Hình 3.13: Giao diện Ra-đa nhận đơn hỏa tốc dành cho Tài xế (Shipper Radar 3 km)."),
    ("- **Giao diện Lộ trình và Cập nhật trạng thái (Hình 3.14):**", "Hình 3.14: Giao diện Lộ trình di chuyển và Cập nhật trạng thái giao hàng của Tài xế."),
    ("- **Quản lý Ví tài xế và Rút tiền (Hình 3.15):**", "Hình 3.15: Giao diện Quản lý ví tài xế, Lịch sử cuốc xe và Yêu cầu rút tiền."),
    ("- **Bảng điều khiển trung tâm (Hình 3.16):**", "Hình 3.16: Giao diện Bảng điều khiển trung tâm Quản trị viên (Admin Hub)."),
    ("- **Module Thẩm định hồ sơ đối tác KYC (Hình 3.17):**", "Hình 3.17: Giao diện Thẩm định hồ sơ đối tác KYC (Gian hàng & Tài xế)."),
    ("- **Module Kiểm duyệt sản phẩm gắn cờ AI (Hình 3.18):**", "Hình 3.18: Giao diện Kiểm duyệt sản phẩm gắn cờ AI nghi ngờ (Duyệt tay thủ công).")
]

for trigger, fig_cap in ui_items:
    if fig_cap not in text and trigger in text:
        text = text.replace(trigger, f"{fig_cap}\n\n{trigger}")

with open(MD_PATH, "w", encoding="utf-8") as f:
    f.write(text)

print("[SUCCESS] All figure captions cleanly embedded in Markdown!")

