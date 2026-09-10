import type { RouteRecordRaw } from "vue-router";

/**
 * ================================================================
 * CÁC ROUTE DO BÌNH PHỤ TRÁCH:
 * 1. Đăng nhập (Login)
 * 2. Đăng ký người mua (Register Buyer - OTP 2 phút - Luồng 1)
 * 3. Đăng ký người bán (Register Seller - Chờ duyệt - Luồng 1)
 * 4. Shipper (Dashboard nhận đơn & giao hàng - Luồng 4)
 * 5. Admin (Bảng điều khiển duyệt Shop, AI & Khiếu nại)
 * ================================================================
 */
export const binhRoutes: RouteRecordRaw[] = [
  {
    path: "/login",
    name: "Login",
    component: () => import("../../views/binh/LoginView.vue"),
    meta: { title: "Đăng Nhập - ZoneMart", hideHeader: true, hideFooter: true }
  },
  {
    path: "/register",
    name: "RegisterBuyer",
    component: () => import("../../views/binh/RegisterBuyerView.vue"),
    meta: { title: "Đăng Ký Khách Hàng - ZoneMart", hideHeader: true, hideFooter: true }
  },
  {
    path: "/register-seller",
    name: "RegisterSeller",
    component: () => import("../../views/binh/RegisterSellerView.vue"),
    meta: { title: "Đăng Ký Người Bán - ZoneMart", hideHeader: true, hideFooter: true }
  },
  {
    path: "/register-shipper",
    name: "RegisterShipper",
    component: () => import("../../views/binh/RegisterShipperView.vue"),
    meta: { title: "Đăng Ký Tài Xế Shipper - ZoneMart", hideHeader: true, hideFooter: true }
  },
  {
    path: "/shipper",
    name: "Shipper",
    component: () => import("../../views/binh/ShipperView.vue"),
    meta: {
      title: "Cổng Tài Xế Shipper - ZoneMart",
      hideHeader: true,
      hideFooter: true,
      requiresAuth: true,
      roles: ["shipper", "admin"]
    }
  },
  {
    path: "/admin",
    name: "Admin",
    component: () => import("../../views/binh/AdminDashboardView.vue"),
    meta: {
      title: "Bảng Điều Khiển Admin - ZoneMart",
      requiresAuth: true,
      roles: ["admin"]
    }
  },
  {
    path: "/seller",
    name: "SellerDashboard",
    component: () => import("../../views/binh/SellerDashboardView.vue"),
    meta: {
      title: "Kênh Quản Lý Bán Hàng - ZoneMart",
      hideHeader: true,
      hideFooter: true,
      requiresAuth: true,
      roles: ["seller", "admin"]
    }
  }
];
