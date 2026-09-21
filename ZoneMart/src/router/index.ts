import { createRouter, createWebHistory } from "vue-router";
import { huyRoutes } from "./modules/huy.routes";
import { thangRoutes } from "./modules/thang.routes";
import { binhRoutes } from "./modules/binh.routes";

/**
 * TỔNG HỢP ROUTER CHO DỰ ÁN ZONEMART
 * Lưu ý: Tách theo từng module cá nhân để tránh Merge Conflict trên GitHub!
 */
const routes = [...thangRoutes, ...binhRoutes, ...huyRoutes];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to) {
    if (to.hash) {
      return {
        el: to.hash,
        behavior: 'smooth'
      };
    }
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
      if (parsed) {
        isLoggedIn = true;
        const pRole = (parsed.role || parsed.primaryRole || '').toLowerCase().trim();
        const pEmail = (parsed.phoneEmail || parsed.email || '').toLowerCase().trim();
        if (
          parsed.isAdmin === true ||
          pRole === 'admin' ||
          pRole === 'administrator' ||
          pEmail === 'admin@zonemart.vn'
        ) {
          userRole = 'admin';
        } else if (pRole) {
          userRole = pRole;
        }
      }
    }
    if (!isLoggedIn && localStorage.getItem('isLoggedIn') === 'true') {
      isLoggedIn = true;
      userRole = (localStorage.getItem('userRole') || 'buyer').toLowerCase().trim();
    }
  } catch {
    isLoggedIn = false;
    userRole = 'guest';
  }

  // Bỏ qua kiểm tra phân quyền cho chính trang 403 và 404
  if (to.path === '/403' || to.path === '/404') {
    return next();
  }

  const normalizedUserRole = userRole.toLowerCase().trim();

  // 1. Nếu route yêu cầu vai trò cụ thể (RBAC - Role Based Access Control)
  if (to.meta.roles && Array.isArray(to.meta.roles)) {
    const allowedRoles = (to.meta.roles as string[]).map((r) => r.toLowerCase().trim());

    // Nếu chưa đăng nhập mà truy cập trang phân quyền
    if (!isLoggedIn || normalizedUserRole === 'guest') {
      return next({
        path: '/login',
        query: {
          redirect: to.fullPath,
          reason: 'auth_required',
          required: allowedRoles.join(', '),
        },
      });
    }

    // Nếu đã đăng nhập nhưng SAI QUYỀN VAI TRÒ -> BÁO LỖI LUÔN, CHUYỂN ĐẾN TRANG 403
    if (!allowedRoles.includes(normalizedUserRole)) {
      console.warn(`[RBAC] Truy cập bị từ chối vào ${to.path}. Vai trò hiện tại: [${normalizedUserRole}], yêu cầu: [${allowedRoles.join(', ')}]`);
      return next({ path: '/403' });
    }
  }

  // 2. Nếu route yêu cầu đăng nhập chung (bất kỳ tài khoản nào đã login)
  if (to.meta.requiresAuth && !isLoggedIn) {
    return next({
      path: '/login',
      query: {
        redirect: to.fullPath,
        reason: 'auth_required',
      },
    });
  }

  // 3. Nếu đã đăng nhập mà truy cập lại trang Đăng Nhập hoặc Đăng Ký
  if (isLoggedIn && (to.path === '/login' || to.path === '/register')) {
    if (normalizedUserRole === 'admin') return next({ path: '/admin' });
    if (normalizedUserRole === 'seller') return next({ path: '/seller' });
    if (normalizedUserRole === 'shipper') return next({ path: '/shipper' });
    return next({ path: '/' });
  }

  next();
});

export default router;
