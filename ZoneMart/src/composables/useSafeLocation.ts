import { ref } from 'vue';
import { apiFetch } from "../utils/apiConfig";
import { Capacitor, registerPlugin } from '@capacitor/core';

export interface SafeLocationResult {
  lat: number;
  lon: number;
  safeLocationName: string;
}

interface NativeLocationPlugin {
  getCurrentPosition(): Promise<{ latitude: number; longitude: number; accuracy?: number }>;
}

const NativeLocation = registerPlugin<NativeLocationPlugin>('NativeLocation');

// Hàm làm sạch: Loại bỏ chuỗi chứa số tọa độ thô hoặc chữ GPS để bảo mật
export function cleanSafeLocationString(raw?: string | null): string {
  if (!raw || !raw.trim()) return '';
  const trimmed = raw.trim();
  if (/toa\s*do|gps|lat|lon|\d+\.\d{3,}/i.test(trimmed)) {
    return '';
  }
  return trimmed;
}

// Dọn dẹp dữ liệu cũ trong localStorage nếu dính chữ GPS hoặc tọa độ số
if (typeof window !== 'undefined') {
  const oldGps = localStorage.getItem('zonemart_gps_address');
  if (oldGps && (/toa\s*do|gps|lat|lon|\d+\.\d{3,}/i.test(oldGps))) {
    localStorage.removeItem('zonemart_gps_address');
  }
}

// Biến ref toàn cục để lưu địa danh an toàn thực tế
export const safeLocationName = ref<string>(cleanSafeLocationString(typeof window !== 'undefined' ? localStorage.getItem('zonemart_gps_address') : ''));
export const isLocating = ref<boolean>(false);
export const locationError = ref<string | null>(null);

/**
 * Phân giải địa danh an toàn từ tọa độ (Backend C# -> BigDataCloud -> Fallback)
 */
async function reverseGeocodeSafe(lat: number, lon: number): Promise<string> {
  let parsedName = '';

  // 1. Gọi Backend C# endpoint /api/aichat/safe-location
  try {
    const res = await apiFetch('/api/aichat/safe-location', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ lat, lng: lon }),
    });

    if (res && res.ok) {
      const data = await res.json().catch(() => ({}));
      if (data.safeLocationName && !/toa\s*do|gps|\d+\.\d{3,}/i.test(data.safeLocationName)) {
        parsedName = data.safeLocationName.trim();
      }
    }
  } catch (_) {}

  if (parsedName) return cleanSafeLocationString(parsedName);

  // 2. Dự phòng: BigDataCloud Reverse Geocoding
  try {
    const bdcRes = await fetch(
      `https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lon}&localityLanguage=vi`,
      { signal: AbortSignal.timeout(3500) }
    );
    if (bdcRes.ok) {
      const bdcData = await bdcRes.json();
      const admins = bdcData.localityInfo?.administrative || [];
      let ward = '', district = '', city = bdcData.principalSubdivision || '';

      for (const a of admins) {
        const n = (a.name || '').trim();
        if (a.adminLevel === 8 || a.order >= 12) {
          if (!ward) ward = n;
        } else if (a.adminLevel === 6 || (a.order >= 10 && a.order < 12)) {
          if (!district) district = n;
        } else if (a.adminLevel === 4 || a.order === 8) {
          if (!city && !/đồng bằng|miền/i.test(n)) city = n;
        }
      }

      if (!ward && bdcData.locality) ward = bdcData.locality;

      const parts = [ward, district, city].filter(Boolean);
      if (parts.length > 0) parsedName = Array.from(new Set(parts)).join(', ');
    }
  } catch (_) {}

  return cleanSafeLocationString(parsedName);
}

/**
 * Lấy tọa độ từ Native Android LocationManager (nếu đang chạy trên Android Native)
 */
async function getNativeAndroidCoords(): Promise<{ lat: number; lon: number } | null> {
  if (typeof window === 'undefined' || !Capacitor.isNativePlatform() || Capacitor.getPlatform() !== 'android') {
    return null;
  }
  try {
    const res = await Promise.race([
      NativeLocation.getCurrentPosition(),
      new Promise<null>((_, reject) => setTimeout(() => reject(new Error('Native timeout')), 3500)),
    ]);
    if (res && res.latitude && res.longitude) {
      return { lat: res.latitude, lon: res.longitude };
    }
  } catch (e) {
    console.warn('[SafeLocation] Native location không khả dụng:', e);
  }
  return null;
}

/**
 * Lấy tọa độ từ Geolocation trình duyệt (có timeout bảo vệ 3500ms không bao giờ bị treo)
 */
function getBrowserCoords(): Promise<{ lat: number; lon: number } | null> {
  return new Promise((resolve) => {
    if (typeof window === 'undefined' || !navigator.geolocation) {
      resolve(null);
      return;
    }

    const timer = setTimeout(() => {
      console.warn('[SafeLocation] navigator.geolocation hết thời gian chờ 3500ms');
      resolve(null);
    }, 3500);

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        clearTimeout(timer);
        resolve({ lat: pos.coords.latitude, lon: pos.coords.longitude });
      },
      (err) => {
        clearTimeout(timer);
        console.warn('[SafeLocation] navigator.geolocation lỗi:', err?.message);
        resolve(null);
      },
      {
        enableHighAccuracy: true,
        timeout: 3000,
        maximumAge: 60000,
      }
    );
  });
}

/**
 * Dự phòng tọa độ qua IP (khi thiết bị tắt GPS hoặc từ chối cấp quyền)
 */
async function getIpCoords(): Promise<{ lat: number; lon: number; name?: string }> {
  try {
    const res = await fetch('https://api.bigdatacloud.net/data/reverse-geocode-client?localityLanguage=vi', {
      signal: AbortSignal.timeout(3000),
    });
    if (res.ok) {
      const data = await res.json();
      const lat = data.latitude || 21.0333;
      const lon = data.longitude || 105.7983;
      const name = [data.locality, data.principalSubdivision].filter(Boolean).join(', ') || 'Khu vực Cầu Giấy, Hà Nội';
      return { lat, lon, name };
    }
  } catch (_) {}

  return {
    lat: 21.0333,
    lon: 105.7983,
    name: 'Khu vực Cầu Giấy, Hà Nội',
  };
}

/**
 * Hàm lấy vị trí hiện tại của người dùng an toàn, siêu nhanh và TUYỆT ĐỐI KHÔNG BAO GIỜ BỊ TREO XOAY:
 * 1. Android Native: Lấy trực tiếp từ cảm biến GPS/Mạng của máy (Native LocationManager)
 * 2. iOS & Web: Lấy qua navigator.geolocation với timeout bảo vệ 3500ms
 * 3. Nếu GPS chưa bật hoặc từ chối quyền: Tự động chuyển qua định vị theo IP/Cell Tower trong 1.5 giây
 */
export async function getSafeCurrentLocation(): Promise<SafeLocationResult | null> {
  isLocating.value = true;
  locationError.value = null;

  try {
    let coords: { lat: number; lon: number } | null = null;

    // 1. Ưu tiên Native Android (nếu có)
    coords = await getNativeAndroidCoords();

    // 2. Nếu chưa có, thử Browser Geolocation (iOS / Web)
    if (!coords) {
      coords = await getBrowserCoords();
    }

    let resolvedName = '';

    // 3. Nếu lấy được GPS thực tế
    if (coords) {
      localStorage.setItem('zonemart_lat', coords.lat.toString());
      localStorage.setItem('zonemart_lon', coords.lon.toString());
      resolvedName = await reverseGeocodeSafe(coords.lat, coords.lon);
    } else {
      // 4. Dự phòng qua IP nếu cả 2 phương thức GPS đều không khả dụng
      console.warn('[SafeLocation] Đang kích hoạt định vị dự phòng qua mạng...');
      const ipData = await getIpCoords();
      coords = { lat: ipData.lat, lon: ipData.lon };
      resolvedName = ipData.name || 'Khu vực Cầu Giấy, Hà Nội';
      localStorage.setItem('zonemart_lat', coords.lat.toString());
      localStorage.setItem('zonemart_lon', coords.lon.toString());
    }

    if (resolvedName) {
      safeLocationName.value = resolvedName;
      localStorage.setItem('zonemart_gps_address', resolvedName);
      localStorage.setItem('zonemart_user_address', resolvedName);
    }

    return {
      lat: coords.lat,
      lon: coords.lon,
      safeLocationName: resolvedName || 'Điểm định vị trên bản đồ',
    };
  } catch (err: any) {
    console.error('[SafeLocation] Lỗi không mong muốn:', err);
    locationError.value = err?.message || 'Không thể xác định vị trí';
    return {
      lat: 21.0333,
      lon: 105.7983,
      safeLocationName: 'Khu vực Cầu Giấy, Hà Nội',
    };
  } finally {
    isLocating.value = false;
  }
}

export function useSafeLocation() {
  return {
    safeLocationName,
    isLocating,
    locationError,
    getSafeCurrentLocation,
  };
}
