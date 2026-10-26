/**
 * API Configuration Helper cho ZoneMart
 * Tự động phân giải địa chỉ Backend Server cho cả Web Localhost lẫn Mobile App (iOS / Android)
 */

export function getBaseApiUrl(): string {
  if (typeof window === 'undefined') return '';

  // 1. Cấu hình tùy chọn từ localStorage
  try {
    const savedServer = localStorage.getItem('zonemart_api_server_url');
    if (savedServer && savedServer.trim()) {
      return savedServer.trim().replace(/\/+$/, '');
    }
  } catch (e) {}

  // 2. Cấu hình từ biến môi trường Vite (.env)
  try {
    const envUrl = (import.meta as any).env?.VITE_API_BASE_URL;
    if (envUrl && typeof envUrl === 'string' && envUrl.trim()) {
      return envUrl.trim().replace(/\/+$/, '');
    }
  } catch (e) {}

  // 3. Nhận diện môi trường Native App (Capacitor iOS / Android)
  const isCapacitor = 
    window.location.protocol === 'capacitor:' || 
    window.location.protocol === 'ionic:' ||
    (window.location.hostname === 'localhost' && !window.location.port);

  if (isCapacitor) {
    // Kết nối đến máy chủ Backend dev trên máy tính qua mạng Wi-Fi
    return 'http://192.168.5.115:5000';
  }

  // 4. Mặc định trên Web Desktop (Vite proxy)
  return '';
}

export function getApiUrl(path: string): string {
  const cleanPath = path.startsWith('/') ? path : ('/' + path);
  const apiPath = cleanPath.startsWith('/api') ? cleanPath : ('/api' + cleanPath);
  const base = getBaseApiUrl();
  return base ? `${base}${apiPath}` : apiPath;
}

export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const targetUrl = getApiUrl(path);

  // Tạo AbortController với timeout 6 giây để không bao giờ bị treo lâu
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 6000);

  const mergedInit: RequestInit = {
    ...init,
    signal: init?.signal || controller.signal,
  };

  try {
    const res = await fetch(targetUrl, mergedInit);
    clearTimeout(timeoutId);

    // Nếu kết quả trả về 404 từ Vite dev server trên localhost, fallback gọi cổng 5000
    if (
      res.status === 404 &&
      typeof window !== 'undefined' &&
      (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') &&
      !targetUrl.startsWith('http')
    ) {
      try {
        return await fetch('http://127.0.0.1:5000' + targetUrl, init);
      } catch (e) {}
    }
    return res;
  } catch (err: any) {
    clearTimeout(timeoutId);

    // Fallback cho native app nếu URL relative gặp sự cố
    if (typeof window !== 'undefined' && !targetUrl.startsWith('http')) {
      try {
        const baseFallback = getBaseApiUrl() || 'http://192.168.5.115:5000';
        return await fetch(baseFallback + targetUrl, init);
      } catch (fallbackErr) {}
    }

    // Thông báo lỗi thân thiện, bảo mật, không để lộ mã nguồn hay cơ sở dữ liệu
    const isTimeout = err?.name === 'AbortError';
    const friendlyMsg = isTimeout
      ? 'Hệ thống phản hồi chậm hoặc mạng gián đoạn. Vui lòng kiểm tra lại kết nối mạng!'
      : 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng hoặc thử lại sau!';

    const cleanError = new Error(friendlyMsg);
    throw cleanError;
  }
}
