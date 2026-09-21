/**
 * API Configuration Helper cho ZoneMart
 * Hỗ trợ phân giải tự động đường dẫn API khi chạy trên cả Localhost lẫn Link Port (DevTunnels, Ngrok, LAN IP, v.v.)
 */

export function getApiUrl(path: string): string {
  const cleanPath = path.startsWith('/') ? path : ('/' + path);
  if (cleanPath.startsWith('/api')) {
    return cleanPath;
  }
  return '/api' + cleanPath;
}

export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const relativeUrl = getApiUrl(path);

  try {
    const res = await fetch(relativeUrl, init);
    // Nếu kết quả trả về 404 từ Vite dev server, tự động fallback gọi thẳng tới backend ASP.NET Core port 5000
    if (
      res.status === 404 &&
      typeof window !== 'undefined' &&
      (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
    ) {
      try {
        const directRes = await fetch('http://127.0.0.1:5000' + relativeUrl, init);
        return directRes;
      } catch (e) {}
    }
    return res;
  } catch (err) {
    // Nếu chạy trực tiếp trên localhost/127.0.0.1 và proxy gặp sự cố, gọi thẳng tới port 5000
    if (
      typeof window !== 'undefined' &&
      (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
    ) {
      try {
        return await fetch('http://127.0.0.1:5000' + relativeUrl, init);
      } catch (fallbackErr) {
        throw fallbackErr;
      }
    }
    throw err;
  }
}
