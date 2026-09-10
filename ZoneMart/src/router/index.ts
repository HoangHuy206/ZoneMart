import { createRouter, createWebHistory } from 'vue-router';
import { huyRoutes } from './modules/huy.routes';
import { thangRoutes } from './modules/thang.routes';
import { binhRoutes } from './modules/binh.routes';

/**
 * TỔNG HỢP ROUTER CHO DỰ ÁN ZONEMART
 * Lưu ý: Tách theo từng module cá nhân để tránh Merge Conflict trên GitHub!
 */
const routes = [...thangRoutes, ...binhRoutes, ...huyRoutes];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 };
  },
});

router.beforeEach((to, _from, next) => {
  // Cập nhật tiêu đề trang
  if (to.meta.title) {
    document.title = to.meta.title as string;
  }

  // Xác thực trạng thái đăng nhập từ localStorage
  let isLoggedIn = false;
  let userRole = 'guest';

  try {
    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
      const parsed = JSON.parse(savedUser);
      if (parsed && parsed.role) {
        isLoggedIn = true;
        userRole = parsed.role;
      }
    }
    if (!isLoggedIn && localStorage.getItem('isLoggedIn') === 'true') {
      isLoggedIn = true;
      userRole = localStorage.getItem('userRole') || 'buyer';
    }
  } catch {
    isLoggedIn = false;
    userRole = 'guest';
  }

  // 1. Nếu route yêu cầu đăng nhập mà người dùng chưa đăng nhập (guest)
  if (to.meta.requiresAuth && !isLoggedIn) {
    return next({
      path: '/login',
      query: {
        redirect: to.fullPath,
        reason: 'auth_required',
      },
    });
  }

  // 2. Nếu route yêu cầu vai trò cụ thể
  if (to.meta.roles && Array.isArray(to.meta.roles)) {
    const allowedRoles = to.meta.roles as string[];
    if (!allowedRoles.includes(userRole)) {
      return next({ path: '/' });
    }
  }

  // 3. Nếu đã đăng nhập mà truy cập lại trang Đăng Nhập hoặc Đăng Ký
  if (isLoggedIn && (to.path === '/login' || to.path === '/register')) {
    return next({ path: '/' });
  }

  next();
});

export default router;
