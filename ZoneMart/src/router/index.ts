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
  let currentUserObj: any = null;

  // Ưu tiên cao nhất: Khi có tín hiệu đăng xuất, dọn sạch triệt để và cho phép vào trang Login
  if (to.path === '/login' && (to.query.logout === 'success' || to.query.logout === 'true')) {
    try {
      localStorage.removeItem('currentUser');
      localStorage.removeItem('zonemart_user');
      localStorage.removeItem('isLoggedIn');
      localStorage.removeItem('userRole');
      localStorage.removeItem('role');
      localStorage.removeItem('sellerRegisteredEmail');
      localStorage.removeItem('sellerRegisteredPassword');
    } catch {}
    return next();
  }

  try {
    const savedUser = localStorage.getItem('currentUser') || localStorage.getItem('zonemart_user');
    if (savedUser) {
      const parsed = JSON.parse(savedUser);
      if (parsed) {
        currentUserObj = parsed;
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

    // Danh sách các vai trò hợp lệ mà người dùng đang sở hữu (hỗ trợ tài khoản nâng cấp đa vai trò)
    const effectiveRoles = [normalizedUserRole];
    if (['khách hàng', 'khach hang', 'customer', 'user', 'buyer', 'khach_hang'].includes(normalizedUserRole)) {
      effectiveRoles.push('buyer');
    }
    // Mọi tài khoản đã đăng nhập đều có tư cách người mua hàng trên ZoneMart
    if (isLoggedIn) {
      effectiveRoles.push('buyer');
    }
    if (currentUserObj?.isSeller === true || currentUserObj?.sellerStatus === 'Approved') {
      effectiveRoles.push('seller');
    }
    if (currentUserObj?.isShipper === true || currentUserObj?.shipperStatus === 'Approved') {
      effectiveRoles.push('shipper');
    }
    if (normalizedUserRole === 'admin') {
      effectiveRoles.push('seller', 'shipper', 'buyer');
    }

    const hasAccess = allowedRoles.some((r) => effectiveRoles.includes(r));

    // Nếu đã đăng nhập nhưng SAI QUYỀN VAI TRÒ -> BÁO LỖI LUÔN, CHUYỂN ĐẾN TRANG 403
    if (!hasAccess) {
      console.warn(`[RBAC] Truy cập bị từ chối vào ${to.path}. Vai trò hiện tại: [${effectiveRoles.join(', ')}], yêu cầu: [${allowedRoles.join(', ')}]`);
      return next({ path: '/403' });
    }

    // Nếu người dùng có quyền Seller và đang vào route Seller, tự động đồng bộ vai trò phiên làm việc sang seller
    if (allowedRoles.includes('seller') && normalizedUserRole !== 'seller' && normalizedUserRole !== 'admin') {
      if (currentUserObj) {
        currentUserObj.role = 'seller';
        currentUserObj.isSeller = true;
        localStorage.setItem('currentUser', JSON.stringify(currentUserObj));
        localStorage.setItem('zonemart_user', JSON.stringify(currentUserObj));
        localStorage.setItem('userRole', 'seller');
      }
    }
    // Tương tự cho shipper
    if (allowedRoles.includes('shipper') && normalizedUserRole !== 'shipper' && normalizedUserRole !== 'admin') {
      if (currentUserObj) {
        currentUserObj.role = 'shipper';
        currentUserObj.isShipper = true;
        localStorage.setItem('currentUser', JSON.stringify(currentUserObj));
        localStorage.setItem('zonemart_user', JSON.stringify(currentUserObj));
        localStorage.setItem('userRole', 'shipper');
      }
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
