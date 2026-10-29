# 📋 Danh Sách Kế Hoạch & Tiến Độ Dự Án ZoneMart

> File này là nguồn dữ liệu thực tế duy nhất cho hệ thống báo cáo Telegram (06h, 12h, 23h).
> Khi ai làm xong việc, chỉ cần đổi `[ ]` thành `[x]`. Bot sẽ tự động đọc file này và báo cáo chính xác 100%.

## 🚀 Đang Triển Khai (In Progress)
- [ ] Hoàn thiện và kiểm thử tính năng Chat giữa Người mua và Shop (ChatController, SellerChatManager)
- [ ] Tích hợp hiển thị sao và bình luận đánh giá sản phẩm (Review.cs, ProductDetailView)
- [ ] Tinh chỉnh giao diện và xử lý đơn hàng người mua (BuyerOrdersView, OrdersController)
- [ ] Cập nhật số liệu thống kê bảng điều khiển người bán (SellerDashboardView)
- [ ] Kiểm tra đồng bộ trạng thái đơn hàng thời gian thực qua Realtime Service (orderRealtimeService)
- [ ] Rà soát bộ lọc phân tầng danh mục và chi tiết sản phẩm (ProductListView, useProductCatalog)

## 📌 Hạng Mục Tiếp Theo (Backlog)
- [ ] Viết Unit Test cho các API thanh toán và đơn hàng trong server/Controllers
- [ ] Tối ưu hóa hiệu năng và tốc độ tải trang danh mục sản phẩm (LCP / INP)
- [ ] Rà soát bảo mật API và các quy tắc phân quyền người dùng

## ✅ Đã Hoàn Thành (Done)
- [x] Thiết lập hệ thống tự động audit dự án và báo cáo Telegram 3 mốc (06:00, 12:00, 23:00)
- [x] Tích hợp GitHub Actions chạy 24/7 trên Cloud (tắt máy tính vẫn báo về bình thường)
- [x] Nhận diện tài khoản push GitHub và thống kê đóng góp của từng thành viên
