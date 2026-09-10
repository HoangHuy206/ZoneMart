import type { RouteRecordRaw } from "vue-router";

/**
 * ================================================================
 * CÁC ROUTE DO THẮNG PHỤ TRÁCH:
 * 1. Homepage (Trang chủ ZoneMart)
 * 2. Giỏ hàng (Cart)
 * 3. Thanh toán (Checkout - Luồng 3)
 * 4. Giao diện Buyer (Theo dõi đơn hàng - Luồng 3 & 4)
 * ================================================================
 */
export const thangRoutes: RouteRecordRaw[] = [
  {
    path: "/",
    name: "Home",
    component: () => import("../../views/thang/HomeView.vue"),
    meta: { title: "Trang Chủ - ZoneMart" }
  },
  {
    path: "/cart",
    name: "Cart",
    component: () => import("../../views/thang/CartView.vue"),
    meta: { title: "Giỏ Hàng - ZoneMart", requiresAuth: true }
  },
  {
    path: "/checkout",
    name: "Checkout",
    component: () => import("../../views/thang/CheckoutView.vue"),
    meta: { title: "Thanh Toán Đơn Hàng - ZoneMart", requiresAuth: true }
  },
  {
    path: "/buyer-orders",
    name: "BuyerOrders",
    component: () => import("../../views/thang/BuyerOrdersView.vue"),
    meta: { title: "Đơn Mua Của Bạn - ZoneMart", requiresAuth: true }
  }
];
