import type { RouteRecordRaw } from 'vue-router';

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
    path: '/login',
    name: 'Login',
    component: () => import('../../views/binh/LoginView.vue'),
    meta: { title: 'Đăng Nhập - ZoneMart', hideHeader: true, hideFooter: true },
  },
  {
    path: '/register',
    name: 'RegisterBuyer',
    component: () => import('../../views/binh/RegisterBuyerView.vue'),
    meta: { title: 'Đăng Ký Khách Hàng - ZoneMart', hideHeader: true, hideFooter: true },
  },
  {
    path: '/register-seller',
    name: 'RegisterSeller',
    component: () => import('../../views/binh/SellerLandingView.vue'),
    meta: { title: 'ZoneMart Seller - Mở Gian Hàng Kinh Doanh', hideHeader: true, hideFooter: true },
  },
  {
    path: '/seller-form',
    name: 'SellerForm',
    component: () => import('../../views/binh/RegisterSellerView.vue'),
    meta: { title: 'Đăng Ký Mở Gian Hàng - ZoneMart', hideHeader: true, hideFooter: true },
  },
  {
    path: '/seller-landing',
    name: 'SellerLanding',
    component: () => import('../../views/binh/SellerLandingView.vue'),
    meta: { title: 'ZoneMart Seller - Mở Gian Hàng Kinh Doanh', hideHeader: true, hideFooter: true },
  },
  {
    path: '/register-shipper',
    name: 'RegisterShipper',
    component: () => import('../../views/binh/ShipperLandingView.vue'),
    meta: { title: 'ZoneMart Driver - Gia Nhập Đội Ngũ Tài Xế', hideHeader: true, hideFooter: true },
  },
  {
    path: '/shipper-form',
    name: 'ShipperForm',
    component: () => import('../../views/binh/RegisterShipperView.vue'),
    meta: { title: 'Đăng Ký Tài Xế - ZoneMart Driver', hideHeader: true, hideFooter: true },
  },
  {
    path: '/shipper-landing',
    name: 'ShipperLanding',
    component: () => import('../../views/binh/ShipperLandingView.vue'),
    meta: { title: 'ZoneMart Driver - Gia Nhập Đội Ngũ Tài Xế', hideHeader: true, hideFooter: true },
  },
  {
    path: '/shipper',
    name: 'Shipper',
    component: () => import('../../views/binh/ShipperView.vue'),
    meta: {
      title: 'Cổng Tài Xế Shipper - ZoneMart',
      hideHeader: true,
      hideFooter: true,
    },
  },
  {
    path: '/seller',
    name: 'SellerDashboard',
    component: () => import('../../views/binh/SellerDashboardView.vue'),
    meta: {
      title: 'Kênh Quản Lý Bán Hàng - ZoneMart Seller',
      hideHeader: true,
      hideFooter: true,
    },
  },
  {
    path: '/admin',
    name: 'Admin',
    component: () => import('../../views/binh/AdminDashboardView.vue'),
    meta: { title: 'Bảng Điều Khiển Quản Trị - ZoneMart Admin', hideHeader: true, hideFooter: true },
  },
];
