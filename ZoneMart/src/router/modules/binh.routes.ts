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
    meta: { title: "Đăng Nhập - ZoneMart" }
  },
  {
    path: "/register",
    name: "RegisterBuyer",
    component: () => import("../../views/binh/RegisterBuyerView.vue"),
    meta: { title: "Đăng Ký Người Mua - ZoneMart" }
  },
  {
    path: "/register-seller",
    name: "RegisterSeller",
    component: () => import("../../views/binh/RegisterSellerView.vue"),
    meta: { title: "Đăng Ký Người Bán - ZoneMart" }
  },
  {
    path: "/shipper",
    name: "Shipper",
    component: () => import("../../views/binh/ShipperView.vue"),
    meta: { title: "Cổng Tài Xế Shipper - ZoneMart" }
  },
  {
    path: "/admin",
    name: "Admin",
    component: () => import("../../views/binh/AdminDashboardView.vue"),
    meta: { title: "Bảng Điều Khiển Admin - ZoneMart" }
  }
];
