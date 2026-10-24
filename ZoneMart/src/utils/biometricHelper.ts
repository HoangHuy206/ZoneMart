import {
  browserSupportsWebAuthn,
  platformAuthenticatorIsAvailable,
  startAuthentication,
  startRegistration,
  type AuthenticationResponseJSON,
  type RegistrationResponseJSON,
} from '@simplewebauthn/browser';
import { apiFetch } from './apiConfig';

export type BiometricType =
  | 'iphone-faceid'
  | 'iphone-touchid'
  | 'android-biometric'
  | 'mac-touchid'
  | 'windows-hello'
  | 'generic';

export interface BiometricDeviceInfo {
  type: BiometricType;
  label: string;
  subLabel: string;
  actionTitle: string;
  iconType: 'faceid' | 'touchid' | 'fingerprint' | 'shield' | 'windows';
  isWebAuthnSupported: boolean;
  isPlatformAvailable: boolean;
  isConditionalSupported: boolean;
}

/**
 * Tự động phân tích thiết bị & phần cứng sinh trắc học hiện tại:
 * - iPhone X trở lên: Face ID
 * - iPhone 8 trở xuống, iPhone SE: Touch ID
 * - Android: Vân tay / Khuôn mặt (BiometricPrompt)
 * - Mac: Touch ID
 * - Windows: Windows Hello
 */
export async function detectBiometricDevice(): Promise<BiometricDeviceInfo> {
  const isWebAuthnSupported = browserSupportsWebAuthn();
  let isPlatformAvailable = false;
  let isConditionalSupported = false;

  if (isWebAuthnSupported && typeof window !== 'undefined') {
    try {
      isPlatformAvailable = await platformAuthenticatorIsAvailable();
    } catch {
      isPlatformAvailable = false;
    }

    try {
      if (
        window.PublicKeyCredential &&
        typeof window.PublicKeyCredential.isConditionalMediationAvailable === 'function'
      ) {
        isConditionalSupported = await window.PublicKeyCredential.isConditionalMediationAvailable();
      }
    } catch {
      isConditionalSupported = false;
    }
  }

  const ua = typeof navigator !== 'undefined' ? navigator.userAgent : '';
  const isIOS =
    /iPad|iPhone|iPod/.test(ua) ||
    (typeof navigator !== 'undefined' && navigator.platform === 'MacIntel' && (navigator.maxTouchPoints || 0) > 1);
  const isAndroid = /Android/i.test(ua);
  const isMac = !isIOS && /Macintosh|MacIntel/i.test(ua);
  const isWindows = /Windows/i.test(ua);

  // Mặc định ban đầu
  let type: BiometricType = 'generic';
  let label = 'Sinh trắc học';
  let subLabel = 'Xác thực nhanh an toàn';
  let actionTitle = 'Đăng nhập bằng Sinh Trắc Học';
  let iconType: BiometricDeviceInfo['iconType'] = 'shield';

  if (isIOS) {
    // Phân biệt iPhone X trở lên (Face ID) vs iPhone cũ / SE (Touch ID) qua tỷ lệ màn hình (Aspect Ratio >= 2.0)
    let isFaceIdIPhone = false;
    if (typeof window !== 'undefined' && window.screen) {
      const screenLongSide = Math.max(window.screen.width, window.screen.height);
      const screenShortSide = Math.min(window.screen.width, window.screen.height);
      const aspectRatio = screenLongSide / (screenShortSide || 1);
      // iPhone X, XS, XR, 11, 12, 13, 14, 15, 16 đều có aspect ratio ~ 2.16 -> >= 2.0
      // iPhone 6, 7, 8, SE 2, SE 3 có aspect ratio 16:9 = 1.777 -> < 2.0
      isFaceIdIPhone = aspectRatio >= 2.0;
    }

    if (isFaceIdIPhone) {
      type = 'iphone-faceid';
      label = 'Face ID';
      subLabel = 'Nhận diện khuôn mặt Apple';
      actionTitle = 'Đăng nhập nhanh bằng Face ID (1-Chạm)';
      iconType = 'faceid';
    } else {
      type = 'iphone-touchid';
      label = 'Touch ID';
      subLabel = 'Cảm biến vân tay Apple';
      actionTitle = 'Đăng nhập nhanh bằng Touch ID';
      iconType = 'touchid';
    }
  } else if (isAndroid) {
    type = 'android-biometric';
    label = 'Vân tay / Khuôn mặt Android';
    subLabel = 'Xác thực sinh trắc học Android';
    actionTitle = 'Đăng nhập nhanh bằng Vân tay / Khuôn mặt';
    iconType = 'fingerprint';
  } else if (isMac) {
    type = 'mac-touchid';
    label = 'Touch ID trên Mac';
    subLabel = 'Bảo mật vân tay Apple Silicon / Magic Keyboard';
    actionTitle = 'Đăng nhập nhanh bằng Touch ID trên Mac';
    iconType = 'touchid';
  } else if (isWindows) {
    type = 'windows-hello';
    label = 'Windows Hello';
    subLabel = 'Vân tay hoặc khuôn mặt Windows Hello';
    actionTitle = 'Đăng nhập nhanh bằng Windows Hello';
    iconType = 'windows';
  }

  return {
    type,
    label,
    subLabel,
    actionTitle,
    iconType,
    isWebAuthnSupported,
    isPlatformAvailable,
    isConditionalSupported,
  };
}

/**
 * Kiểm tra xem tài khoản đã từng đăng ký Face ID / Passkey nào chưa
 */
export async function checkUserPasskeyStatus(
  username: string
): Promise<{ success: boolean; hasPasskey: boolean; registered: boolean; fullName?: string; devices?: string[] }> {
  try {
    const res = await apiFetch(`/auth/webauthn/check-passkey?username=${encodeURIComponent(username)}`);
    if (!res.ok) return { success: false, hasPasskey: false, registered: false };
    return await res.json();
  } catch {
    return { success: false, hasPasskey: false, registered: false };
  }
}

/**
 * Luồng thực hiện Đăng ký Passkey / Sinh trắc học cho tài khoản hiện tại
 */
export async function registerPasskeyForCurrentUser(
  userCodeOrPhone: string,
  deviceName?: string
): Promise<{ success: boolean; message: string }> {
  try {
    const clientOrigin = typeof window !== 'undefined' ? window.location.origin : '';
    const clientHost = typeof window !== 'undefined' ? window.location.hostname : '';

    const optRes = await apiFetch('/auth/webauthn/register-options', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: userCodeOrPhone,
        deviceName: deviceName || (await detectBiometricDevice()).label,
        clientHost,
        clientOrigin,
      }),
    });

    if (!optRes.ok) {
      const err = await optRes.json().catch(() => ({}));
      return { success: false, message: err.message || 'Không thể khởi tạo phiên đăng ký sinh trắc học' };
    }

    const options = await optRes.json();
    const attResp: RegistrationResponseJSON = await startRegistration({ optionsJSON: options });

    const verifyRes = await apiFetch('/auth/webauthn/register-verify', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: userCodeOrPhone,
        attestationResponse: attResp,
        deviceName: deviceName || (await detectBiometricDevice()).label,
        clientHost,
        clientOrigin,
      }),
    });

    const verifyData = await verifyRes.json().catch(() => ({}));
    if (verifyRes.ok && verifyData.success) {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem('zonemart_passkey_saved', 'true');
        if (userCodeOrPhone) localStorage.setItem('zonemart_passkey_account', userCodeOrPhone);
      }
      return { success: true, message: verifyData.message || 'Đã liên kết sinh trắc học thành công!' };
    } else {
      return { success: false, message: verifyData.message || 'Xác thực khóa sinh trắc học thất bại' };
    }
  } catch (err: any) {
    console.error('Passkey registration error:', err);
    return {
      success: false,
      message: err.name === 'NotAllowedError' ? 'Bạn đã hủy thao tác quét Face ID / Sinh trắc học trên thiết bị.' : (err.message || 'Lỗi khi kích hoạt sinh trắc học'),
    };
  }
}

/**
 * Luồng Đăng nhập 1-chạm bằng Passkey / WebAuthn
 */
export async function loginWithBiometrics(
  usernameHint?: string,
  useAutofill: boolean = false
): Promise<{ success: boolean; data?: any; message: string }> {
  try {
    const clientOrigin = typeof window !== 'undefined' ? window.location.origin : '';
    const clientHost = typeof window !== 'undefined' ? window.location.hostname : '';

    const optRes = await apiFetch('/auth/webauthn/login-options', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: usernameHint || '',
        clientHost,
        clientOrigin,
      }),
    });

    if (!optRes.ok) {
      const err = await optRes.json().catch(() => ({}));
      return { success: false, message: err.message || 'Không thể tạo yêu cầu xác thực sinh trắc học' };
    }

    const options = await optRes.json();
    if (options && options.success === false && options.hasPasskey === false) {
      return { success: false, hasPasskey: false, message: options.message || 'Tài khoản chưa lưu Face ID trên thiết bị.' };
    }

    const asseResp: AuthenticationResponseJSON = await startAuthentication({
      optionsJSON: options,
      useBrowserAutofill: useAutofill,
    });

    const verifyRes = await apiFetch('/auth/webauthn/login-verify', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        assertionResponse: asseResp,
        clientHost,
        clientOrigin,
      }),
    });

    const verifyData = await verifyRes.json().catch(() => ({}));
    if (verifyRes.ok && verifyData.success) {
      return { success: true, data: verifyData, message: verifyData.message || 'Đăng nhập sinh trắc học thành công!' };
    } else {
      return { success: false, message: verifyData.message || 'Xác minh sinh trắc học không khớp.' };
    }
  } catch (err: any) {
    console.error('Biometric login error:', err);
    if (err.name === 'NotAllowedError') {
      return { success: false, message: 'Thao tác quét sinh trắc học đã bị hủy.' };
    }
    return { success: false, message: err.message || 'Đăng nhập sinh trắc học thất bại' };
  }
}

