/**
 * API Configuration Helper cho ZoneMart
 * Tự động phân giải địa chỉ Backend Server cho cả Web Localhost lẫn Mobile App (iOS / Android)
 */

export function getBaseApiUrl(): string {
  if (typeof window === 'undefined') return '';

  // 1. Cấu hình tùy chọn từ localStorage (ví dụ dev hoặc người dùng đổi link Server VPS / Ngrok)
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
    // Kết nối đến máy chủ Backend chạy trên máy tính dev trong mạng Wi-Fi
    return 'http://192.168.1.16:5000';
  }

  // 4. Mặc định trên Web Desktop (Vite proxy xử lý qua relative path)
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

  try {
    const res = await fetch(targetUrl, init);
    // Nếu kết quả trả về 404 từ Vite dev server trên localhost, tự động fallback gọi cổng 5000
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
  } catch (err) {
    // Fallback cho native app nếu URL relative gặp sự cố
    if (typeof window !== 'undefined' && !targetUrl.startsWith('http')) {
      try {
        return await fetch('http://192.168.1.16:5000' + targetUrl, init);
      } catch (fallbackErr) {
        throw fallbackErr;
      }
    }
    throw err;
  }
}
