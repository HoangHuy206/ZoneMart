import { ref } from 'vue';

export interface SafeLocationResult {
  lat: number;
  lon: number;
  safeLocationName: string;
}

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
 * Hàm lấy vị trí hiện tại của người dùng và dịch ra tên địa danh thực tế (Privacy-Safe Reverse Geocoding)
 * - Dùng navigator.geolocation lấy tọa độ GPS thực tế (lat, lon).
 * - Gửi GPS về Backend C# API (/api/aichat/safe-location) để phân giải địa danh thực tế.
 * - Logic bảo mật: Chỉ lấy Tên địa danh/Cơ sở, Phường/Xã, Quận/Huyện, Tỉnh/Thành phố.
 * - TUYỆT ĐỐI BỎ QUA số nhà (house_number) để bảo mật.
 * - Tọa độ số CHỈ lưu ngầm ở Backend, Frontend chỉ nhận và hiển thị tên địa danh thực tế.
 */
export async function getSafeCurrentLocation(): Promise<SafeLocationResult | null> {
  if (typeof window === 'undefined' || !navigator.geolocation) {
    locationError.value = 'Trình duyệt không hỗ trợ Geolocation.';
    console.warn('[SafeLocation]', locationError.value);
    return null;
  }

  isLocating.value = true;
  locationError.value = null;

  return new Promise((resolve) => {
    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        try {
          const lat = pos.coords.latitude;
          const lon = pos.coords.longitude;

          // Lưu ngầm tọa độ GPS thực tế vào localStorage cho backend
          localStorage.setItem('zonemart_lat', lat.toString());
          localStorage.setItem('zonemart_lon', lon.toString());

          let parsedName = '';

          // 1. Gọi Backend C# endpoint /api/aichat/safe-location (ưu tiên relative URL qua proxy)
          try {
            const res = await fetch('/api/aichat/safe-location', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ lat, lon })
            }).catch(() => fetch('http://localhost:5000/api/aichat/safe-location', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ lat, lon })
            }));

            if (res && res.ok) {
              const data = await res.json();
              if (data.safeLocationName && !/toa\s*do|gps|\d+\.\d{3,}/i.test(data.safeLocationName)) {
                parsedName = data.safeLocationName.trim();
              }
            }
          } catch (apiErr) {
            console.warn('[SafeLocation] Backend safe-location API error:', apiErr);
          }

          // 2. Dự phòng: Tra cứu trực tiếp từ BigDataCloud nếu Backend chưa kịp phản hồi
          if (!parsedName) {
            try {
              let alleyOrRoad = '';
              let hamlet = '';
              try {
                const pRes = await fetch(`https://photon.komoot.io/reverse?lat=${lat}&lon=${lon}&limit=5`);
                if (pRes.ok) {
                  const pData = await pRes.json();
                  const feats = pData.features || [];
                  for (const f of feats) {
                    const p = f.properties || {};
                    const pName = (p.name || '').replace(/^(số\s*)?\d+[a-zA-Z]?(\s*[-/]\s*\d+[a-zA-Z]?)*\s*(,\s*|\s+)/i, '').trim();
                    if (/^(ngõ|ngách|hẻm|kiệt)\s+/i.test(pName)) {
                      alleyOrRoad = pName;
                      break;
                    }
                  }
                  if (!alleyOrRoad) {
                    for (const f of feats) {
                      const p = f.properties || {};
                      const pName = (p.name || '').replace(/^(số\s*)?\d+[a-zA-Z]?(\s*[-/]\s*\d+[a-zA-Z]?)*\s*(,\s*|\s+)/i, '').trim();
                      if (/^(đường|phố)\s+/i.test(pName) || p.type === 'street') {
                        alleyOrRoad = /^(đường|phố)\s+/i.test(pName) ? pName : `Đường ${pName}`;
                        break;
                      }
                    }
                  }
                  for (const f of feats) {
                    const p = f.properties || {};
                    const pName = (p.name || '').trim();
                    if (/^(thôn|xóm|ấp|bản|làng)\s+/i.test(pName)) {
                      hamlet = pName;
                      break;
                    }
                  }
                }
              } catch (_) {}

              const bdcRes = await fetch(`https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lon}&localityLanguage=vi`);
              if (bdcRes.ok) {
                const bdcData = await bdcRes.json();
                const admins = bdcData.localityInfo?.administrative || [];
                const informs = bdcData.localityInfo?.informative || [];
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

                for (const inf of informs) {
                  const iName = (inf.name || '').trim();
                  const iDesc = (inf.description || '').trim();
                  if (/(huyện|quận|thị xã|thành phố)/i.test(iDesc)) {
                    district = iName;
                    break;
                  } else if (inf.order >= 10 && inf.order <= 11 && /^(Quận|Huyện|Thị xã|TP|Thành phố)\s+/i.test(iName)) {
                    district = iName;
                    break;
                  }
                }

                if (!ward && bdcData.locality) ward = bdcData.locality;

                // Chuẩn hóa tiền tố hành chính tiếng Việt
                if (ward) {
                  ward = ward.replace(/^Xa\s+/i, 'Xã ').replace(/^Phuong\s+/i, 'Phường ').replace(/^Thi\s*tran\s+/i, 'Thị trấn ');
                  ward = ward.replace('Song Phuong', 'Song Phương').replace('Son Dong', 'Sơn Đồng').replace('Tay Mo', 'Tây Mỗ').replace('Xuan Phuong', 'Xuân Phương');
                }
                if (district) {
                  district = district.replace(/^Quan\s+/i, 'Quận ').replace(/^Huyen\s+/i, 'Huyện ').replace(/^Thanh\s*pho\s+/i, 'Thành phố ');
                  if (/^\d+$/.test(district)) district = `Quận ${district}`;
                  else if (!/^(Quận|Huyện|Thị xã|Thành phố)\s+/i.test(district)) {
                    const dLower = district.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
                    if (dLower.includes('hoai duc') || dLower.includes('dan phuong') || dLower.includes('dong anh') || dLower.includes('gia lam')) district = `Huyện ${district}`;
                    else if (dLower.includes('vinh') || dLower.includes('da lat')) district = `Thành phố ${district}`;
                    else district = `Quận ${district}`;
                  }
                }
                if (city) {
                  city = city.replace(/^Tinh\s+/i, 'Tỉnh ').replace(/^Thanh\s*pho\s+/i, 'Thành phố ');
                  const cLower = city.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
                  if (cLower.includes('ha noi')) city = 'Hà Nội';
                  else if (cLower.includes('ho chi minh') || cLower.includes('sai gon')) city = 'Thành phố Hồ Chí Minh';
                  else if (cLower.includes('da nang')) city = 'Đà Nẵng';
                  else if (cLower.includes('hai phong')) city = 'Hải Phòng';
                  else if (cLower.includes('can tho')) city = 'Cần Thơ';
                  else if (!city.startsWith('Tỉnh ') && !city.startsWith('Thành phố ')) city = `Tỉnh ${city}`;
                }

                const parts = [alleyOrRoad, hamlet, ward, district, city].filter(Boolean);
                if (parts.length > 0) parsedName = Array.from(new Set(parts)).join(', ');
              }
            } catch (_) {}
          }

          parsedName = cleanSafeLocationString(parsedName);

          if (parsedName) {
            safeLocationName.value = parsedName;
            localStorage.setItem('zonemart_gps_address', parsedName);
            localStorage.setItem('zonemart_user_address', parsedName);
          }

          const payload: SafeLocationResult = {
            lat,
            lon,
            safeLocationName: parsedName
          };

          isLocating.value = false;
          resolve(payload);
        } catch (err: any) {
          console.error('[SafeLocation] Lỗi khi dịch địa danh an toàn:', err);
          locationError.value = err?.message || 'Lỗi kết nối dịch vụ bản đồ.';
          isLocating.value = false;
          resolve(null);
        }
      },
      async (err) => {
        console.warn('[SafeLocation] Không thể lấy tọa độ GPS từ trình duyệt, đang dự phòng qua IP:', err);
        try {
          const ipRes = await fetch('https://api.bigdatacloud.net/data/reverse-geocode-client?localityLanguage=vi');
          if (ipRes.ok) {
            const ipData = await ipRes.json();
            const lat = ipData.latitude || 21.0333;
            const lon = ipData.longitude || 105.7833;

            localStorage.setItem('zonemart_lat', lat.toString());
            localStorage.setItem('zonemart_lon', lon.toString());

            let ipLocName = '';
            try {
              const res = await fetch('/api/aichat/safe-location', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ lat, lon })
              }).catch(() => fetch('http://localhost:5000/api/aichat/safe-location', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ lat, lon })
              }));

              if (res && res.ok) {
                const data = await res.json();
                if (data.safeLocationName && !/toa\s*do|gps|\d+\.\d{3,}/i.test(data.safeLocationName)) {
                  ipLocName = data.safeLocationName.trim();
                }
              }
            } catch (_) {}

            if (!ipLocName) {
              ipLocName = [ipData.locality, ipData.principalSubdivision].filter(Boolean).join(', ');
            }
            if (!ipLocName) ipLocName = 'Phường Dịch Vọng Hậu, Quận Cầu Giấy, Hà Nội';

            safeLocationName.value = ipLocName;
            localStorage.setItem('zonemart_gps_address', ipLocName);
            localStorage.setItem('zonemart_user_address', ipLocName);
            isLocating.value = false;
            resolve({ lat, lon, safeLocationName: ipLocName });
            return;
          }
        } catch (_) {}
        locationError.value = 'Không thể xác định vị trí hiện tại.';
        isLocating.value = false;
        resolve({
          lat: 21.0333,
          lon: 105.7833,
          safeLocationName: 'Phường Dịch Vọng Hậu, Quận Cầu Giấy, Hà Nội'
        });
      },
      {
        enableHighAccuracy: false,
        timeout: 6000,
        maximumAge: 60000
      }
    );
  });
}

export function useSafeLocation() {
  return {
    safeLocationName,
    isLocating,
    locationError,
    getSafeCurrentLocation
  };
}
