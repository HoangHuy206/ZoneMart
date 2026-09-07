import { createRouter, createWebHistory } from "vue-router";
import { huyRoutes } from "./modules/huy.routes";
import { thangRoutes } from "./modules/thang.routes";
import { binhRoutes } from "./modules/binh.routes";

/**
 * TỔNG HỢP ROUTER CHO DỰ ÁN ZONEMART
 * Lưu ý: Tách theo từng module cá nhân để tránh Merge Conflict trên GitHub!
 */
const routes = [
  ...thangRoutes,
  ...binhRoutes,
  ...huyRoutes
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 };
  }
});

router.beforeEach((to, _from, next) => {
  if (to.meta.title) {
    document.title = to.meta.title as string;
  }
  next();
});

export default router;
