/**
 * API Configuration & Intelligent Server Discovery for ZoneMart
 * Tự động nhận diện, dò tìm và phân giải địa chỉ máy chủ Backend (C# .NET Kestrel)
 * Hoạt động mượt mà 100% tự động trên cả Web Localhost lẫn Mobile App (Android APK & iOS IPA)
 * mà không cần người dùng phải bấm nút hay nhập IP thủ công.
 */

const KNOWN_PORT = '5000';
const DEFAULT_LAN_IP = '172.16.28.71';

let cachedDiscoveredUrl: string | null = null;
let lastDiscoveredAt = 0;
let discoveryPromise: Promise<string | null> | null = null;

/**
 * Kiểm tra xem một địa chỉ máy chủ có đang phản hồi ZoneMart Health hay không
 */
async function probeServerEndpoint(baseUrl: string, timeoutMs = 1200): Promise<boolean> {
  const clean = baseUrl.trim().replace(/\/+$/, '');
  if (!clean) return false;
  try {
    const controller = new AbortController();
    const tid = setTimeout(() => controller.abort(), timeoutMs);
    const res = await fetch(`${clean}/api/health`, {
      method: 'GET',
      signal: controller.signal,
      headers: { 'Accept': 'application/json' },
    });
    clearTimeout(tid);
    if (res.ok) {
      const data = await res.json().catch(() => null);
      if (data && (data.active === true || data.status === 'Healthy' || data.service?.includes('ZoneMart'))) {
        return true;
      }
      return true;
    }
    return false;
  } catch {
    return false;
  }
}

/**
 * Thử đồng thời danh sách các địa chỉ IP ứng viên, trả về địa chỉ đầu tiên phản hồi
 */
function raceCandidates(candidates: string[], timeoutMs: number): Promise<string | null> {
  return new Promise((resolve) => {
    let resolved = false;
    let remaining = candidates.length;

    if (remaining === 0) {
      resolve(null);
      return;
    }

    candidates.forEach((cand) => {
      probeServerEndpoint(cand, timeoutMs)
        .then((isAlive) => {
          if (resolved) return;
          if (isAlive) {
            resolved = true;
            resolve(cand);
          } else {
            remaining--;
            if (remaining === 0) resolve(null);
          }
        })
        .catch(() => {
          if (resolved) return;
          remaining--;
          if (remaining === 0) resolve(null);
        });
    });
  });
}

/**
 * Sinh danh sách các địa chỉ IP trong dải mạng cục bộ (Subnet) có khả năng cao là máy tính dev
 */
function generateSubnetCandidates(): string[] {
  const list: string[] = [];

  // 1. Dải 172.16.28.x (Wi-Fi hiện tại của máy tính dev)
  list.push(`http://172.16.28.71:${KNOWN_PORT}`);
  for (let i = 1; i <= 100; i++) {
    if (i !== 71) list.push(`http://172.16.28.${i}:${KNOWN_PORT}`);
  }

  // 2. Dải 192.168.99.x (Wi-Fi HHT WIFI)
  list.push(`http://192.168.99.27:${KNOWN_PORT}`);
  for (let i = 2; i <= 60; i++) {
    if (i !== 27) list.push(`http://192.168.99.${i}:${KNOWN_PORT}`);
  }

  // 2. Dải 192.168.5.x (Wi-Fi văn phòng / nhà riêng trước đó)
  list.push(`http://192.168.5.115:${KNOWN_PORT}`);
  for (let i = 100; i <= 150; i++) {
    if (i !== 115) list.push(`http://192.168.5.${i}:${KNOWN_PORT}`);
  }
  for (let i = 2; i <= 40; i++) {
    list.push(`http://192.168.5.${i}:${KNOWN_PORT}`);
  }

  // 3. Dải 192.168.1.x (Router gia đình phổ biến nhất)
  for (let i = 100; i <= 140; i++) {
    list.push(`http://192.168.1.${i}:${KNOWN_PORT}`);
  }
  for (let i = 2; i <= 30; i++) {
    list.push(`http://192.168.1.${i}:${KNOWN_PORT}`);
  }

  // 4. Dải 192.168.0.x
  for (let i = 100; i <= 130; i++) {
    list.push(`http://192.168.0.${i}:${KNOWN_PORT}`);
  }
  for (let i = 2; i <= 20; i++) {
    list.push(`http://192.168.0.${i}:${KNOWN_PORT}`);
  }

  // 5. Điểm phát sóng di động (Hotspot 4G/5G)
  for (let i = 1; i <= 15; i++) {
    list.push(`http://172.20.10.${i}:${KNOWN_PORT}`);
    list.push(`http://192.168.43.${i}:${KNOWN_PORT}`);
  }

  return list;
}

/**
 * Ghi nhận và lưu URL máy chủ đã dò tìm thành công
 */
function setDiscoveredServer(url: string) {
  const clean = url.trim().replace(/\/+$/, '');
  cachedDiscoveredUrl = clean;
  lastDiscoveredAt = Date.now();
  try {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('zonemart_api_server_url', clean);
    }
  } catch {}
  console.log(`[ZoneMart Auto-Discovery] Kết nối máy chủ thành công: ${clean}`);
}

/**
 * Tự động dò tìm địa chỉ IP máy chủ Backend trong mạng Wi-Fi LAN
 */
export async function autoDiscoverServer(force = false): Promise<string | null> {
  if (!force && cachedDiscoveredUrl && (Date.now() - lastDiscoveredAt < 30000)) {
    return cachedDiscoveredUrl;
  }

  if (discoveryPromise) {
    return discoveryPromise;
  }

  discoveryPromise = (async () => {
    try {
      // 1. Nhóm ưu tiên hàng đầu: IP đã lưu, IP hiện tại, IP localhost, IP emulator
      const priorityCandidates: string[] = [];

      try {
        const saved = typeof localStorage !== 'undefined' ? localStorage.getItem('zonemart_api_server_url') : null;
        if (saved && saved.trim() && !priorityCandidates.includes(saved.trim())) {
          priorityCandidates.push(saved.trim().replace(/\/+$/, ''));
        }
      } catch {}

      if (cachedDiscoveredUrl && !priorityCandidates.includes(cachedDiscoveredUrl)) {
        priorityCandidates.push(cachedDiscoveredUrl);
      }

      const standardCandidates = [
        `http://${DEFAULT_LAN_IP}:${KNOWN_PORT}`,
        `http://10.0.2.2:${KNOWN_PORT}`,
        `http://localhost:${KNOWN_PORT}`,
        `http://127.0.0.1:${KNOWN_PORT}`,
      ];

      if (
        typeof window !== 'undefined' &&
        window.location.hostname &&
        window.location.hostname !== 'localhost' &&
        window.location.hostname !== '127.0.0.1' &&
        window.location.hostname !== ''
      ) {
        standardCandidates.unshift(`http://${window.location.hostname}:${KNOWN_PORT}`);
      }

      for (const cand of standardCandidates) {
        if (!priorityCandidates.includes(cand)) {
          priorityCandidates.push(cand);
        }
      }

      // Thử đồng thời các ứng viên ưu tiên (1200ms)
      const priorityWinner = await raceCandidates(priorityCandidates, 1200);
      if (priorityWinner) {
        setDiscoveredServer(priorityWinner);
        return priorityWinner;
      }

      // 2. Nhóm quét mở rộng (nếu máy tính đổi IP khác trong subnet)
      const subnetList = generateSubnetCandidates();
      const BATCH_SIZE = 15;
      for (let i = 0; i < subnetList.length; i += BATCH_SIZE) {
        const batch = subnetList.slice(i, i + BATCH_SIZE);
        const batchWinner = await raceCandidates(batch, 800);
        if (batchWinner) {
          setDiscoveredServer(batchWinner);
          return batchWinner;
        }
      }

      return null;
    } finally {
      discoveryPromise = null;
    }
  })();

  return discoveryPromise;
}

/**
 * Lấy Base API URL hiện hành
 */
export function getBaseApiUrl(): string {
  if (typeof window === 'undefined') return '';

  const hostname = window.location.hostname || '';
  const isLocalhost = hostname === 'localhost' || hostname === '127.0.0.1';

  // 1. Nếu đang mở từ Localhost (Web Desktop dev): Luôn dùng relative URL '' để qua Vite Proxy tới 127.0.0.1:5000
  // Tuyệt đối không dùng IP LAN cũ trong localStorage vì người dùng đổi mạng Wi-Fi sẽ bị treo kết nối!
  if (isLocalhost) {
    return '';
  }

  // 2. Nếu đang mở từ trình duyệt trên điện thoại qua IP Wi-Fi (ví dụ http://172.16.28.71:5173):
  // Backend cùng nằm trên máy tính này nên hostname chính là IP của backend cổng 5000
  if (hostname && !isLocalhost && !hostname.includes('capacitor')) {
    return `http://${hostname}:${KNOWN_PORT}`;
  }

  // 3. URL đã được hệ thống tự động dò tìm thấy
  if (cachedDiscoveredUrl) {
    return cachedDiscoveredUrl;
  }

  // 4. Cấu hình đã lưu trong localStorage từ lần trước
  try {
    const saved = localStorage.getItem('zonemart_api_server_url');
    if (saved && saved.trim()) {
      return saved.trim().replace(/\/+$/, '');
    }
  } catch {}

  // 5. Biến môi trường Vite (.env)
  try {
    const envUrl = (import.meta as any).env?.VITE_API_BASE_URL;
    if (envUrl && typeof envUrl === 'string' && envUrl.trim()) {
      return envUrl.trim().replace(/\/+$/, '');
    }
  } catch {}

  // 6. Môi trường Native App (Capacitor Android / iOS)
  const isCapacitor =
    window.location.protocol === 'capacitor:' ||
    window.location.protocol === 'ionic:' ||
    (hostname === 'localhost' && !window.location.port);

  if (isCapacitor) {
    return `http://${DEFAULT_LAN_IP}:${KNOWN_PORT}`;
  }

  // 7. Mặc định trên Web Desktop (Vite proxy)
  return '';
}

/**
 * Phân giải đường dẫn API đầy đủ
 */
export function getApiUrl(path: string): string {
  const cleanPath = path.startsWith('/') ? path : '/' + path;
  const apiPath = cleanPath.startsWith('/api') ? cleanPath : '/api' + cleanPath;
  const base = getBaseApiUrl();
  return base ? `${base}${apiPath}` : apiPath;
}

/**
 * Gửi HTTP request có cơ chế tự động dò tìm lại IP và thử lại khi gặp sự cố mạng
 */
export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const targetUrl = getApiUrl(path);

  // Hàm thực thi fetch kèm controller timeout riêng cho từng lượt gọi
  const execute = async (url: string, timeoutSec = 15): Promise<Response> => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutSec * 1000);
    try {
      const res = await fetch(url, {
        ...init,
        signal: init?.signal || controller.signal,
      });
      clearTimeout(timeoutId);
      return res;
    } catch (err) {
      clearTimeout(timeoutId);
      throw err;
    }
  };

  const currentUrl = getApiUrl(path);

  try {
    const res = await execute(currentUrl, 15);

    // Xử lý fallback cho Vite proxy nếu trả về 404, 502, 504
    if (
      (res.status === 404 || res.status === 502 || res.status === 504) &&
      typeof window !== 'undefined' &&
      (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') &&
      !currentUrl.startsWith('http')
    ) {
      try {
        return await fetch(`http://127.0.0.1:${KNOWN_PORT}${currentUrl}`, init);
      } catch {}
    }

    return res;
  } catch (err: any) {
    // Nếu đang ở localhost mà fetch relative URL gặp sự cố, thử ngay http://127.0.0.1:5000 trực tiếp (không đợi auto-discover)
    if (typeof window !== 'undefined' && (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')) {
      try {
        const cleanPath = path.startsWith('/') ? path : '/' + path;
        const apiPath = cleanPath.startsWith('/api') ? cleanPath : '/api' + cleanPath;
        return await fetch(`http://127.0.0.1:${KNOWN_PORT}${apiPath}`, init);
      } catch (directErr) {}
    }

    // Tự động kích hoạt cơ chế Auto-Discovery quét tìm IP mới của máy chủ
    try {
      console.warn('[ZoneMart] Đang tự động dò tìm máy chủ backend...');
      const discovered = await autoDiscoverServer(true);
      if (discovered) {
        const cleanPath = path.startsWith('/') ? path : '/' + path;
        const apiPath = cleanPath.startsWith('/api') ? cleanPath : '/api' + cleanPath;
        const retryUrl = `${discovered}${apiPath}`;
        console.log(`[ZoneMart] Tự động thử lại với máy chủ: ${retryUrl}`);
        return await execute(retryUrl, 15);
      }
    } catch (retryErr) {}

    // Fallback nếu URL relative gặp sự cố
    if (typeof window !== 'undefined' && !targetUrl.startsWith('http')) {
      const isLocalhost = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
      if (isLocalhost) {
        try {
          return await fetch('http://127.0.0.1:5000' + targetUrl, init);
        } catch (fallbackErr) {}
      } else {
        try {
          return await fetch(`http://${DEFAULT_LAN_IP}:5000` + targetUrl, init);
        } catch (fallbackErr) {}
      }
    }

    // Thông báo lỗi thân thiện, bảo mật, không để lộ mã nguồn hay cơ sở dữ liệu
    const isTimeout = err?.name === 'AbortError';
    const friendlyMsg = isTimeout
      ? 'Hệ thống phản hồi chậm hoặc mạng gián đoạn. Vui lòng kiểm tra lại kết nối Wi-Fi!'
      : 'Không thể kết nối đến máy chủ ZoneMart. Vui lòng đảm bảo máy tính chạy server và điện thoại chung mạng Wi-Fi!';

    throw new Error(friendlyMsg);
  }
}

// Tự động kích hoạt dò tìm máy chủ trong nền khi ứng dụng khởi động
if (typeof window !== 'undefined') {
  setTimeout(() => {
    autoDiscoverServer(false).catch(() => {});
  }, 100);
}
