import { ref } from 'vue';
import { apiFetch } from '../utils/apiConfig';

export const storeStatusVersion = ref(0);

// Helper loại bỏ dấu tiếng Việt để so sánh chuỗi không dấu
export function removeVietnameseTones(str: string): string {
  if (!str) return '';
  str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, 'a');
  str = str.replace(/è|é|ẹ|ề|ế|ệ|ể|ễ/g, 'e');
  str = str.replace(/ì|í|ị|ỉ|ĩ/g, 'i');
  str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, 'o');
  str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, 'u');
  str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, 'y');
  str = str.replace(/đ/g, 'd');
  str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, 'A');
  str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, 'E');
  str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, 'I');
  str = str.replace(/Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, 'O');
  str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, 'U');
  str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, 'Y');
  str = str.replace(/Đ/g, 'D');
  return str;
}

// Chuẩn hóa tên gian hàng (bỏ tiền tố phổ biến, khoảng trắng thừa)
export function normalizeStoreKey(rawName?: string | null): string {
  if (!rawName) return '';
  let s = rawName.toLowerCase().trim();
  s = s.replace(/^(gian\s+hàng|sap\s+hàng|sạp\s+hàng|cửa\s+hàng|tiệm\s+bánh|tiệm|vựa|vườn\s+rau|hợp\s+tác\s+xã|tổng\s+kho)\s+/i, '');
  return s.trim();
}

/**
 * Cập nhật local map và localStorage với dữ liệu từ server hoặc SSE
 */
export function applyStoreStatusToStorage(storeIdentifier: string, isOpen: boolean, extra?: { email?: string; id?: string }): void {
  if (typeof localStorage === 'undefined') return;

  const raw = (storeIdentifier || '').toLowerCase().trim();
  if (!raw) return;

  const norm = normalizeStoreKey(raw);
  const rawNoTone = removeVietnameseTones(raw).toLowerCase().trim();
  const normNoTone = removeVietnameseTones(norm).toLowerCase().trim();
  const strVal = String(isOpen);

  const keys = [
    raw,
    norm,
    rawNoTone,
    normNoTone,
    `gian hàng ${raw}`,
    `gian hàng ${norm}`,
    `gian hang ${rawNoTone}`,
    `gian hang ${normNoTone}`
  ];

  if (extra?.email) {
    keys.push(extra.email.toLowerCase().trim());
  }
  if (extra?.id) {
    keys.push(extra.id.toLowerCase().trim());
  }

  // 1. Cập nhật direct keys
  for (const k of keys) {
    if (k) localStorage.setItem(`zonemart_store_open_${k}`, strVal);
  }

  // 2. Cập nhật zonemart_closed_stores
  let closedMap: Record<string, boolean> = {};
  try {
    const rawMap = localStorage.getItem('zonemart_closed_stores');
    if (rawMap) closedMap = JSON.parse(rawMap);
  } catch {}

  for (const k of keys) {
    if (k) closedMap[k] = isOpen;
  }
  try {
    localStorage.setItem('zonemart_closed_stores', JSON.stringify(closedMap));
  } catch {}

  // 3. Nếu là test gian hàng hoặc khớp seller hiện tại
  if (raw.includes('test gian') || norm.includes('test gian') || normNoTone.includes('test gian')) {
    localStorage.setItem('zonemart_store_open_baconbao206@gmail.com', strVal);
  }

  storeStatusVersion.value++;
}

/**
 * Đồng bộ danh sách trạng thái các gian hàng từ Backend Server qua HTTP
 */
export async function fetchStoreStatusesFromServer(): Promise<void> {
  try {
    const res = await apiFetch('/api/realtime/store-statuses');
    if (res.ok) {
      const data = await res.json();
      if (data && data.statuses && typeof data.statuses === 'object') {
        for (const [storeName, isOpen] of Object.entries(data.statuses)) {
          applyStoreStatusToStorage(storeName, Boolean(isOpen));
        }
      }
    }
  } catch (e) {
    // offline or server restarting
  }
}

/**
 * Kiểm tra xem gian hàng có đang tạm đóng cửa nghỉ nhận đơn hay không.
 * Trả về:
 * - true  => Gian hàng đang TẠM ĐÓNG CỬA (Nghỉ nhận đơn hỏa tốc)
 * - false => Gian hàng đang MỞ CỬA nhận đơn bình thường
 */
export function isStoreClosed(storeIdentifier?: string | null): boolean {
  // Kích hoạt tính phản ứng cho computed / watcher / template Vue
  void storeStatusVersion.value;
  if (!storeIdentifier) return false;

  const raw = storeIdentifier.toLowerCase().trim();
  if (!raw) return false;

  const norm = normalizeStoreKey(raw);
  const rawNoTone = removeVietnameseTones(raw).toLowerCase().trim();
  const normNoTone = removeVietnameseTones(norm).toLowerCase().trim();

  try {
    if (typeof localStorage === 'undefined') return false;

    // 1. Kiểm tra trạng thái của người bán hiện tại vừa chỉnh sửa toggle
    const currentStoreOpen = localStorage.getItem('zonemart_current_store_open');
    if (currentStoreOpen === 'false') {
      const curEmail = (localStorage.getItem('zonemart_current_seller_email') || '').toLowerCase().trim();
      const curStore = (localStorage.getItem('zonemart_current_seller_store_name') || '').toLowerCase().trim();
      const curStoreNorm = normalizeStoreKey(curStore);

      // Nếu tên gian hàng đang duyệt trùng hoặc chứa gian hàng người bán đang đóng
      if (curStore && (raw === curStore || norm === curStoreNorm || raw.includes(curStoreNorm) || curStoreNorm.includes(norm))) {
        return true;
      }
      if (curEmail && (raw.includes(curEmail) || curEmail.includes(raw))) {
        return true;
      }
      // Tài khoản test gian hàng (baconbao206@gmail.com)
      if (raw.includes('test gian') || norm.includes('test gian') || normNoTone.includes('test gian')) {
        return true;
      }
    }

    // 2. Kiểm tra map danh sách tổng hợp trong zonemart_closed_stores
    const closedStoresMapRaw = localStorage.getItem('zonemart_closed_stores');
    if (closedStoresMapRaw) {
      try {
        const closedMap = JSON.parse(closedStoresMapRaw);
        if (
          closedMap[raw] === false ||
          closedMap[norm] === false ||
          closedMap[rawNoTone] === false ||
          closedMap[normNoTone] === false ||
          closedMap[`gian hàng ${raw}`] === false ||
          closedMap[`gian hàng ${norm}`] === false ||
          closedMap[`gian hang ${rawNoTone}`] === false ||
          closedMap[`gian hang ${normNoTone}`] === false
        ) {
          return true;
        }

        // Fuzzy match: nếu có key trong closedMap mang giá trị false và khớp một phần
        for (const [k, v] of Object.entries(closedMap)) {
          if (v === false) {
            const kNorm = removeVietnameseTones(k).toLowerCase().trim();
            if (
              (rawNoTone.length >= 4 && (rawNoTone.includes(kNorm) || kNorm.includes(rawNoTone))) ||
              (normNoTone.length >= 4 && (normNoTone.includes(kNorm) || kNorm.includes(normNoTone)))
            ) {
              return true;
            }
          }
        }

        // Nếu có key rõ ràng là true
        if (
          closedMap[raw] === true ||
          closedMap[norm] === true ||
          closedMap[rawNoTone] === true ||
          closedMap[normNoTone] === true
        ) {
          return false;
        }
      } catch {}
    }

    // 3. Kiểm tra các direct keys trong localStorage
    const keysToCheck = [
      `zonemart_store_open_${raw}`,
      `zonemart_store_open_${norm}`,
      `zonemart_store_open_${rawNoTone}`,
      `zonemart_store_open_${normNoTone}`,
      `zonemart_store_open_gian hàng ${raw}`,
      `zonemart_store_open_gian hàng ${norm}`,
    ];

    for (const key of keysToCheck) {
      const val = localStorage.getItem(key);
      if (val === 'false') return true;
    }

    // Khớp tài khoản test gian hàng nếu email baconbao206 đang tạm đóng
    if (raw.includes('test gian') || norm.includes('test gian') || normNoTone.includes('test gian')) {
      const emailStatus = localStorage.getItem('zonemart_store_open_baconbao206@gmail.com');
      if (emailStatus === 'false') return true;
    }

    // Nếu có key rõ ràng là 'true'
    for (const key of keysToCheck) {
      const val = localStorage.getItem(key);
      if (val === 'true') return false;
    }
  } catch {}

  return false;
}

/**
 * Đặt trạng thái Mở cửa / Tạm đóng cho gian hàng và phát tín hiệu đồng bộ toàn hệ thống.
 */
export function setStoreOpenStatus(
  storeName: string,
  isOpen: boolean,
  extra?: { email?: string; id?: string }
): void {
  try {
    if (typeof localStorage === 'undefined') return;

    applyStoreStatusToStorage(storeName, isOpen, extra);

    const strVal = String(isOpen);
    localStorage.setItem('zonemart_current_store_open', strVal);
    localStorage.setItem('zonemart_current_seller_store_name', storeName);

    // Phát custom event cho component trong cùng window
    window.dispatchEvent(
      new CustomEvent('zonemart:store-status-changed', {
        detail: { isOpen, storeName, email: extra?.email, id: extra?.id }
      })
    );

    // Đồng bộ lên Backend Server qua HTTP API để broadcast realtime xuyên trình duyệt
    apiFetch('/api/realtime/store-status', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        storeName,
        isOpen,
        email: extra?.email,
        id: extra?.id
      })
    }).catch(err => {
      console.warn('[useStoreOpenStatus] Error syncing store status to server:', err);
    });
  } catch {}

  storeStatusVersion.value++;
}

// Lắng nghe sự kiện để kích hoạt reactivity cho tất cả view
if (typeof window !== 'undefined') {
  window.addEventListener('zonemart:store-status-changed', (e: any) => {
    if (e.detail && typeof e.detail.isOpen === 'boolean' && e.detail.storeName) {
      applyStoreStatusToStorage(e.detail.storeName, e.detail.isOpen, {
        email: e.detail.email,
        id: e.detail.id
      });
    } else {
      storeStatusVersion.value++;
    }
  });

  window.addEventListener('storage', (e) => {
    if (
      e.key &&
      (e.key.startsWith('zonemart_store_open_') ||
        e.key === 'zonemart_current_store_open' ||
        e.key === 'zonemart_closed_stores')
    ) {
      storeStatusVersion.value++;
    }
  });

  // Tải trạng thái các gian hàng từ server ngay khi khởi động
  fetchStoreStatusesFromServer();

  // Polling fallback dự phòng định kỳ mỗi 4 giây
  setInterval(() => {
    if (typeof document !== 'undefined' && document.hidden) return;
    fetchStoreStatusesFromServer();
  }, 4000);
}

export function useStoreOpenStatus() {
  return {
    isStoreClosed,
    setStoreOpenStatus,
    storeStatusVersion,
    normalizeStoreKey,
    removeVietnameseTones,
    fetchStoreStatusesFromServer,
    applyStoreStatusToStorage
  };
}
