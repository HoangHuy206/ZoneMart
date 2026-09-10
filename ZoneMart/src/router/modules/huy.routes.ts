import type { RouteRecordRaw } from "vue-router";

/**
 * CÁC ROUTE DO HUY PHỤ TRÁCH:
 * 1. Profile (Thông tin cá nhân & ví tiền)
 * 2. 404 (Trang không tìm thấy)
 * 3. Liên Hệ (Contact & Support)
 * 4. Trang SP (Danh sách & Chi tiết sản phẩm)
 */
export const huyRoutes: RouteRecordRaw[] = [
  {
    path: "/profile",
    name: "Profile",
    component: () => import("../../views/profile/ProfileView.vue"),
    meta: { title: "Hồ Sơ Cá Nhân - ZoneMart", requiresAuth: true }
  },
  {
    path: "/products",
    name: "Products",
    component: () => import("../../views/product/ProductListView.vue"),
    meta: { title: "Danh Sách Sản Phẩm - ZoneMart" }
  },
  {
    path: "/products/:id",
    name: "ProductDetail",
    component: () => import("../../views/product/ProductDetailView.vue"),
    meta: { title: "Chi Tiết Sản Phẩm - ZoneMart" }
  },
  {
    path: "/contact",
    name: "Contact",
    component: () => import("../../views/contact/ContactView.vue"),
    meta: { title: "Liên Hệ & Hỗ Trợ - ZoneMart" }
  },
  {
    path: "/404",
    name: "NotFound",
    component: () => import("../../views/not-found/NotFoundView.vue"),
    meta: { title: "404 - ACCESS DENIED", hideHeader: true, hideFooter: true }
  },
  {
    path: "/:pathMatch(.*)*",
    name: "NotFoundCatchAll",
    component: () => import("../../views/not-found/NotFoundView.vue"),
    meta: { title: "404 - ACCESS DENIED", hideHeader: true, hideFooter: true }
  }
];
