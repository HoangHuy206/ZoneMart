import { computed } from "vue";
import { useBreakpoints, breakpointsBootstrapV5, useWindowSize } from "@vueuse/core";

/**
 * useResponsive - ZoneMart Multi-Device Responsive Composable
 * Hỗ trợ nhận diện thiết bị (Mobile, Tablet, Desktop) và kích thước màn hình theo chuẩn Bootstrap 5.
 * 
 * Breakpoints:
 * - xs: < 576px (Điện thoại nhỏ)
 * - sm: >= 576px (Điện thoại lớn / Phablet)
 * - md: >= 768px (Tablet)
 * - lg: >= 992px (Laptop / Desktop nhỏ)
 * - xl: >= 1200px (Desktop)
 * - xxl: >= 1400px (Màn hình lớn)
 */
export function useResponsive() {
  const breakpoints = useBreakpoints(breakpointsBootstrapV5);
  const { width, height } = useWindowSize();

  // Nhận diện theo thiết bị
  const isSmallMobile = breakpoints.smaller("sm");           // < 576px
  const isMobile = breakpoints.smaller("md");                // < 768px (Điện thoại)
  const isTablet = breakpoints.between("md", "lg");          // 768px - 991px (Tablet)
  const isDesktop = breakpoints.greaterOrEqual("lg");        // >= 992px (Desktop/Laptop)
  const isLargeDesktop = breakpoints.greaterOrEqual("xl");   // >= 1200px

  // Hướng màn hình
  const isPortrait = computed(() => height.value > width.value);
  const isLandscape = computed(() => width.value >= height.value);

  // Nhận diện cảm ứng
  const isTouchDevice = computed(() => {
    return typeof window !== "undefined" && ("ontouchstart" in window || navigator.maxTouchPoints > 0);
  });

  return {
    breakpoints,
    width,
    height,
    isSmallMobile,
    isMobile,
    isTablet,
    isDesktop,
    isLargeDesktop,
    isPortrait,
    isLandscape,
    isTouchDevice,
  };
}

