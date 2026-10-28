# 🌱 ZoneMart - Nền Tảng Thương Mại Nông Sản & Giao Hàng Hỏa Tốc

<div align="center">

<p align="center">
  <img src="https://img.shields.io/badge/Vue.js_3-35495E?style=for-the-badge&logo=vuedotjs&logoColor=4FC08D" height="28" alt="Vue.js 3" />
  <img src="https://img.shields.io/badge/ASP.NET_Core_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" height="28" alt="ASP.NET Core 8" />
  <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white" height="28" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white" height="28" alt="Tailwind CSS" />
  <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" height="28" alt="Docker" />
</p>

**Cầu nối trực tiếp giữa Nhà vườn địa phương – Người tiêu dùng – Tài xế giao hàng hỏa tốc trong khu vực lân cận.**

</div>

---

## ✨ Trải Nghiệm Giao Diện Tổng Quan

<div align="center">
  <img src="docs/images/hero-overview.png" alt="ZoneMart Overview" width="90%" style="border-radius: 8px;" />
</div>

---

## 🌿 Hệ Sinh Thái Đa Nền Tảng (Multi-Role Ecosystem)

| 🌾 Kênh Nhà Vườn & Tiểu Thương (Merchant Portal) | ⚡ Cổng Điều Phối Tài Xế (Shipper Dispatch) |
| :---: | :---: |
| <img src="docs/images/merchant-dashboard.png" width="100%" alt="Merchant Dashboard" /> | <img src="docs/images/shipper-map.png" width="100%" alt="Shipper Map" /> |
| **Quản trị doanh thu, kho hàng & trạng thái mở tiệm** | **Bản đồ radar GPS nhận đơn hỏa tốc thời gian thực** |

| 🥗 Trải Nghiệm Người Tiêu Dùng (Customer Portal) | 🛡️ Trung Tâm Hỗ Trợ & CSKH (Support Center) |
| :---: | :---: |
| <img src="docs/images/products-radar.png" width="100%" alt="Products Radar" /> | <img src="docs/images/support-ticket.png" width="100%" alt="Support Ticket" /> |
| **Tìm kiếm nông sản VietGAP theo cự ly GPS gần nhất** | **Gửi ticket khiếu nại, cam kết phản hồi dưới 15 phút** |

---

## 🗺️ Quy Trình Vận Hành & Điều Phối Đơn Hàng

```mermaid
sequenceDiagram
    autonumber
    actor C as Khách Hàng
    participant S as Hệ Thống ZoneMart
    actor M as Nhà Vườn (Seller)
    actor D as Tài Xế (Shipper)

    C->>S: Đặt nông sản (Theo vị trí gần nhất)
    S-->>M: Bắn thông báo đơn mới (Real-time)
    M->>S: Xác nhận & Đóng gói sản phẩm
    S-->>D: Điều phối đơn qua Bản đồ Radar GPS
    D->>M: Đến sạp nhận hàng
    D->>C: Giao hàng hỏa tốc (Tối ưu tuyến đường)
    C->>S: Xác nhận nhận hàng & Đánh giá
```

---

## ⚙️ Kiến Trúc & Ngăn Xếp Công Nghệ (Tech Stack)

| Lớp kiến trúc | Công nghệ & Thư viện sử dụng |
| :--- | :--- |
| **Frontend** | Vue 3 (Composition API), Vite, TypeScript, Pinia, Tailwind CSS |
| **Backend** | ASP.NET Core (.NET 8), Clean Architecture, Entity Framework Core |
| **Bản đồ & Realtime** | OpenStreetMap / Leaflet, SignalR (WebSockets) |
| **Lưu trữ & Cache** | PostgreSQL / SQL Server, Redis Cache |
| **Bảo mật & Vận hành**| Docker Compose, JWT Authentication, RBAC |

---

## 🚀 Hướng Dẫn Khởi Chạy Nhanh (Quick Start)

```bash
# 1. Clone repository
git clone https://github.com/HoangHuy206/ZoneMart.git
cd ZoneMart

# 2. Khởi chạy nhanh toàn bộ hệ thống bằng Docker
docker-compose up -d --build
```
* **Frontend**: `http://localhost:5173`
* **Swagger API**: `http://localhost:5000/swagger`

---

## 👥 Đội Ngũ Phát Triển (Development Team)

Dự án được nghiên cứu, thiết kế và phát triển bởi:

* 👨‍💻 **Đoàn Hoàng Huy**
* 👨‍💻 **Đỗ Văn Bình**
* 👨‍💻 **Lê Đức Thắng**

---

## 📬 Liên Hệ & Bản Quyền

* 📧 **Email hỗ trợ**: [hotrozonemart@gmail.com](mailto:hotrozonemart@gmail.com)
* 🎯 **Mục đích**: Dự án phục vụ nghiên cứu, học tập và xây dựng giải pháp thương mại điện tử nông sản địa phương.
* 🛡️ **Bản quyền**: © 2026 ZoneMart Team. All rights reserved.
