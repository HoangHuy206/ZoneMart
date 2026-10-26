import {
  browserSupportsWebAuthn,
  platformAuthenticatorIsAvailable,
  startAuthentication,
  startRegistration,
  type AuthenticationResponseJSON,
  type RegistrationResponseJSON,
} from '@simplewebauthn/browser';
import { Capacitor, registerPlugin } from '@capacitor/core';
import { apiFetch } from './apiConfig';

export interface BiometricAuthPlugin {
  isAvailable(): Promise<{ isAvailable: boolean; biometryType?: string; code?: string; message?: string }>;
  authenticate(options?: {
    title?: string;
    subtitle?: string;
    description?: string;
    negativeButtonText?: string;
  }): Promise<{ success: boolean; message?: string; errorCode?: number }>;
}

export const BiometricAuth = registerPlugin<BiometricAuthPlugin>('BiometricAuth');

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
  title: string;
  description: string;
  actionTitle: string;
  iconType: 'faceid' | 'touchid' | 'fingerprint' | 'shield' | 'windows';
  isWebAuthnSupported: boolean;
  isPlatformAvailable: boolean;
  isConditionalSupported: boolean;
  isNative: boolean;
}

/**
 * Tự động phân tích thiết bị & phần cứng sinh trắc học hiện tại:
 * - Android: Vân tay & Khuôn mặt của máy (BiometricPrompt)
 * - iPhone X trở lên: Face ID
 * - iPhone 8 trở xuống / SE: Touch ID
 * - Mac: Touch ID
 * - Windows: Windows Hello
 */
export async function detectBiometricDevice(): Promise<BiometricDeviceInfo> {
  const isWebAuthnSupported = browserSupportsWebAuthn();
  let isPlatformAvailable = false;
  let isConditionalSupported = false;

  const isNative = typeof window !== 'undefined' && Capacitor.isNativePlatform();
  const platform = isNative ? Capacitor.getPlatform() : 'web';

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

  if (isNative && platform === 'android') {
    try {
      const bioInfo = await BiometricAuth.isAvailable();
      if (bioInfo && bioInfo.isAvailable) {
        isPlatformAvailable = true;
      }
    } catch {
      isPlatformAvailable = true;
    }
  }

  const ua = typeof navigator !== 'undefined' ? navigator.userAgent : '';
  const isIOS =
    platform === 'ios' ||
    /iPad|iPhone|iPod/.test(ua) ||
    (typeof navigator !== 'undefined' && navigator.platform === 'MacIntel' && (navigator.maxTouchPoints || 0) > 1);
  const isAndroid = platform === 'android' || /Android/i.test(ua);
  const isMac = !isIOS && /Macintosh|MacIntel/i.test(ua);
  const isWindows = /Windows/i.test(ua);

  let type: BiometricType = 'generic';
  let label = 'Sinh trắc học';
  let subLabel = 'Xác thực nhanh an toàn';
  let title = 'Thiết Lập Sinh Trắc Học';
  let description = 'Bảo mật tài khoản bằng sinh trắc học thiết bị. Đăng nhập 1-chạm không cần mật khẩu.';
  let actionTitle = 'Bật Sinh Trắc Học';
  let iconType: BiometricDeviceInfo['iconType'] = 'shield';

  if (isAndroid) {
    type = 'android-biometric';
    label = 'Vân tay & Khuôn mặt';
    subLabel = 'Cảm biến vân tay và nhận diện khuôn mặt máy';
    title = 'Thiết Lập Sinh Trắc Học (Vân Tay & Khuôn Mặt)';
    description = 'Bảo mật tài khoản bằng cảm biến vân tay hoặc nhận diện khuôn mặt gốc của điện thoại. Thiết lập 1-chạm đăng nhập siêu tốc không cần mật khẩu.';
    actionTitle = 'Bật Vân Tay / Khuôn Mặt';
    iconType = 'fingerprint';
  } else if (isIOS) {
    let isFaceIdIPhone = false;
    if (typeof window !== 'undefined' && window.screen) {
      const screenLongSide = Math.max(window.screen.width, window.screen.height);
      const screenShortSide = Math.min(window.screen.width, window.screen.height);
      const aspectRatio = screenLongSide / (screenShortSide || 1);
      isFaceIdIPhone = aspectRatio >= 2.0;
    } else {
      isFaceIdIPhone = true;
    }

    if (isFaceIdIPhone) {
      type = 'iphone-faceid';
      label = 'Face ID';
      subLabel = 'Nhận diện khuôn mặt Apple Face ID';
      title = 'Thiết Lập Xác Thực Face ID';
      description = 'Bảo mật tài khoản bằng nhận diện khuôn mặt Face ID của iPhone. Đăng nhập 1-chạm không cần mật khẩu.';
      actionTitle = 'Bật Face ID';
      iconType = 'faceid';
    } else {
      type = 'iphone-touchid';
      label = 'Touch ID';
      subLabel = 'Cảm biến vân tay Apple Touch ID';
      title = 'Thiết Lập Xác Thực Touch ID';
      description = 'Bảo mật tài khoản bằng cảm biến vân tay Touch ID của iPhone.';
      actionTitle = 'Bật Touch ID';
      iconType = 'touchid';
    }
  } else if (isMac) {
    type = 'mac-touchid';
    label = 'Touch ID trên Mac';
    subLabel = 'Bảo mật vân tay Apple';
    title = 'Thiết Lập Xác Thực Touch ID';
    description = 'Đăng nhập nhanh chóng và bảo mật bằng cảm biến Touch ID trên máy Mac của bạn.';
    actionTitle = 'Bật Touch ID';
    iconType = 'touchid';
  } else if (isWindows) {
    type = 'windows-hello';
    label = 'Windows Hello';
    subLabel = 'Mã PIN / Vân tay / Khuôn mặt Windows';
    title = 'Thiết Lập Xác Thực Windows Hello';
    description = 'Đăng nhập bảo mật tức thì bằng mã PIN, cảm biến vân tay hoặc camera Windows Hello.';
    actionTitle = 'Bật Windows Hello';
    iconType = 'windows';
  }

  return {
    type,
    label,
    subLabel,
    title,
    description,
    actionTitle,
    iconType,
    isWebAuthnSupported,
    isPlatformAvailable,
    isConditionalSupported,
    isNative,
  };
}

/**
 * Gọi cảm biến sinh trắc học Native của máy:
 * - Android: BiometricPrompt (Vân tay hoặc Face Unlock của máy)
 * - iPhone: Face ID / Touch ID Native
 */
export async function verifyDeviceBiometrics(
  reason: string = 'Xác thực sinh trắc học để tiếp tục'
): Promise<{ success: boolean; message?: string }> {
  // 1. Android Native qua Capacitor BiometricAuth Plugin
  if (typeof window !== 'undefined' && Capacitor.isNativePlatform() && Capacitor.getPlatform() === 'android') {
    try {
      const checkRes = await BiometricAuth.isAvailable();
      if (!checkRes.isAvailable) {
        return {
          success: false,
          message: checkRes.message || 'Thiết bị Android chưa bật hoặc chưa cài đặt vân tay/khuôn mặt trong Cài đặt máy.',
        };
      }

      const res = await BiometricAuth.authenticate({
        title: 'Xác Thực Sinh Trắc Học',
        subtitle: 'Chạm cảm biến vân tay hoặc nhìn vào camera máy',
        description: reason,
        negativeButtonText: 'Hủy bỏ',
      });

      if (res && res.success) {
        return { success: true, message: 'Xác thực sinh trắc học thiết bị thành công!' };
      } else {
        return {
          success: false,
          message: res?.message || 'Xác thực sinh trắc học đã bị hủy hoặc không khớp.',
        };
      }
    } catch (err: any) {
      console.warn('Lỗi gọi BiometricAuth trên Android:', err);
      return { success: false, message: err?.message || 'Lỗi khi kích hoạt cảm biến sinh trắc học thiết bị.' };
    }
  }

  // 2. iPhone Native qua WebKit Script Message Handler
  if (typeof window !== 'undefined' && (window as any).webkit?.messageHandlers?.nativeFaceId) {
    return new Promise((resolve) => {
      (window as any).__handleNativeFaceIdResult = (success: boolean, errorMsg: string) => {
        delete (window as any).__handleNativeFaceIdResult;
        if (success) {
          resolve({ success: true, message: 'Xác thực Face ID thành công!' });
        } else {
          resolve({ success: false, message: errorMsg || 'Xác thực Face ID đã bị hủy hoặc không khớp.' });
        }
      };

      try {
        (window as any).webkit.messageHandlers.nativeFaceId.postMessage({ action: 'authenticate', reason });
      } catch (err) {
        resolve({ success: false, message: 'Lỗi gọi cảm biến Face ID của thiết bị' });
      }
    });
  }

  // 3. Trình duyệt Web thông thường (không có Native Bridge)
  return { success: false, message: 'NO_NATIVE_BRIDGE' };
}

export function verifyDeviceFaceId(
  reason: string = 'Xác thực sinh trắc học ZoneMart'
): Promise<{ success: boolean; message?: string }> {
  return verifyDeviceBiometrics(reason);
}

/**
 * Kích hoạt và liên kết sinh trắc học của thiết bị vào tài khoản:
 * Dùng trực tiếp cảm biến phần cứng của máy (Vân tay / Face Unlock gốc), KHÔNG mở form UniFace camera trên mobile!
 */
export async function registerDeviceBiometrics(userId: string): Promise<{
  success: boolean;
  message?: string;
  registeredAt?: string;
  biometricLabel?: string;
}> {
  const device = await detectBiometricDevice();
  const isNative = typeof window !== 'undefined' && (
    Capacitor.isNativePlatform() ||
    (window as any).webkit?.messageHandlers?.nativeFaceId
  );

  // 1. Thiết bị Di Động Native (Android / iOS):
  if (isNative) {
    const promptReason = `Xác thực ${device.label} để kích hoạt đăng nhập nhanh cho tài khoản ${userId}`;
    const verifyRes = await verifyDeviceBiometrics(promptReason);

    if (!verifyRes.success) {
      return {
        success: false,
        message: verifyRes.message || `Xác thực ${device.label} không thành công hoặc bạn đã hủy thao tác.`,
      };
    }

    const nowStr = new Date().toLocaleString('vi-VN');

    // Lưu vào CSDL Backend qua endpoint native
    try {
      const res = await apiFetch('/api/auth/biometric/register-native', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId,
          biometricType: device.label,
        }),
      });
      const data = await res.json().catch(() => ({}));
      if (res.ok && data.success) {
        localStorage.setItem('zonemart_biometric_enabled', 'true');
        localStorage.setItem('zonemart_passkey_saved', 'true');
        localStorage.setItem('zonemart_biometric_account', userId);
        localStorage.setItem('zonemart_biometric_registered_at', data.registeredAt || nowStr);
        return {
          success: true,
          message: data.message || `Đã kích hoạt ${device.label} thành công!`,
          registeredAt: data.registeredAt || nowStr,
          biometricLabel: device.label,
        };
      }
    } catch (e) {}

    // Fallback lưu local nếu mất mạng
    localStorage.setItem('zonemart_biometric_enabled', 'true');
    localStorage.setItem('zonemart_passkey_saved', 'true');
    localStorage.setItem('zonemart_biometric_account', userId);
    localStorage.setItem('zonemart_biometric_registered_at', nowStr);

    return {
      success: true,
      message: `Đã kích hoạt ${device.label} trên thiết bị thành công!`,
      registeredAt: nowStr,
      biometricLabel: device.label,
    };
  }

  // 2. Trình duyệt Web Desktop qua WebAuthn Passkey (Windows Hello / Mac Touch ID)
  if (device.isPlatformAvailable) {
    const passkeyRes = await registerBiometricPasskey(userId);
    if (passkeyRes.success) {
      return {
        success: true,
        message: passkeyRes.message || 'Đã kích hoạt khóa bảo mật thành công!',
        registeredAt: new Date().toLocaleString('vi-VN'),
        biometricLabel: device.label,
      };
    }
    return {
      success: false,
      message: passkeyRes.message || 'Không thể đăng ký khóa bảo mật trên trình duyệt.',
    };
  }

  return {
    success: false,
    message: 'Thiết bị chưa hỗ trợ hoặc chưa bật sinh trắc học trong phần cài đặt máy.',
  };
}

/**
 * Hủy kích hoạt sinh trắc học cho tài khoản
 */
export async function disableDeviceBiometrics(userId: string): Promise<{ success: boolean; message?: string }> {
  try {
    const res = await apiFetch('/api/auth/face/disable', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userId }),
    });
    const data = await res.json().catch(() => ({}));
    localStorage.removeItem('zonemart_biometric_enabled');
    localStorage.removeItem('zonemart_passkey_saved');
    localStorage.removeItem('zonemart_biometric_registered_at');
    return {
      success: true,
      message: data.message || 'Đã tắt sinh trắc học cho tài khoản.',
    };
  } catch {
    localStorage.removeItem('zonemart_biometric_enabled');
    localStorage.removeItem('zonemart_passkey_saved');
    localStorage.removeItem('zonemart_biometric_registered_at');
    return {
      success: true,
      message: 'Đã tắt sinh trắc học trên thiết bị.',
    };
  }
}

/**
 * Kiểm tra trạng thái sinh trắc học của tài khoản
 */
export async function checkDeviceBiometricStatus(userId: string): Promise<{
  isEnabled: boolean;
  registeredAt?: string;
}> {
  if (!userId) return { isEnabled: false };

  // 1. Kiểm tra Backend API
  try {
    const res = await apiFetch(`/api/auth/face/status?userId=${encodeURIComponent(userId)}`);
    if (res && res.ok) {
      const data = await res.json();
      if (data && data.success && data.faceAuthEnabled) {
        return {
          isEnabled: true,
          registeredAt: data.registeredAt,
        };
      }
    }
  } catch {}

  // 2. Kiểm tra localStorage
  try {
    const savedBio = localStorage.getItem('zonemart_biometric_enabled');
    const savedPasskey = localStorage.getItem('zonemart_passkey_saved');
    const savedAccount = localStorage.getItem('zonemart_biometric_account');
    const savedTime = localStorage.getItem('zonemart_biometric_registered_at');

    if ((savedBio === 'true' || savedPasskey === 'true') && (!savedAccount || savedAccount.toLowerCase() === userId.toLowerCase())) {
      return {
        isEnabled: true,
        registeredAt: savedTime || undefined,
      };
    }
  } catch {}

  return { isEnabled: false };
}

/**
 * WebAuthn Passkey Registration (Dành cho Web Browser)
 */
export async function registerBiometricPasskey(username: string): Promise<{ success: boolean; message?: string }> {
  if (!browserSupportsWebAuthn()) {
    return { success: false, message: 'Trình duyệt không hỗ trợ WebAuthn' };
  }

  const clientOrigin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173';
  const clientHost = typeof window !== 'undefined' ? window.location.hostname : 'localhost';

  try {
    const optRes = await apiFetch('/auth/webauthn/register-options', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, displayName: username, clientHost, clientOrigin }),
    });

    if (!optRes.ok) {
      const err = await optRes.json().catch(() => ({}));
      return { success: false, message: err.message || 'Lỗi nhận cấu hình đăng ký' };
    }

    const options = await optRes.json();
    const attResp: RegistrationResponseJSON = await startRegistration({ optionsJSON: options });

    const verifyRes = await apiFetch('/auth/webauthn/register-verify', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, attestationResponse: attResp, clientHost, clientOrigin }),
    });

    const verifyData = await verifyRes.json().catch(() => ({}));
    if (verifyRes.ok && verifyData.success) {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem('zonemart_passkey_saved', 'true');
        localStorage.setItem('zonemart_passkey_user', username);
      }
      return { success: true, message: 'Kích hoạt khóa bảo mật thành công!' };
    } else {
      return { success: false, message: verifyData.message || 'Xác thực đăng ký thất bại' };
    }
  } catch (err: any) {
    if (err.name === 'NotAllowedError') {
      return { success: false, message: 'Thao tác quét sinh trắc học đã bị hủy.' };
    }
    return { success: false, message: err.message || 'Lỗi đăng ký sinh trắc học' };
  }
}

/**
 * WebAuthn Passkey Login (Dành cho Web Browser)
 */
export async function authenticateBiometricPasskey(
  username?: string,
  useAutofill: boolean = false
): Promise<{ success: boolean; data?: any; message?: string; hasPasskey?: boolean }> {
  if (!browserSupportsWebAuthn()) {
    return { success: false, message: 'Trình duyệt không hỗ trợ WebAuthn' };
  }

  const clientOrigin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173';
  const clientHost = typeof window !== 'undefined' ? window.location.hostname : 'localhost';

  try {
    const optRes = await apiFetch('/auth/webauthn/login-options', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: username || '', clientHost, clientOrigin }),
    });

    if (!optRes.ok) {
      return { success: false, message: 'Không thể khởi tạo phiên xác thực sinh trắc học' };
    }

    const options = await optRes.json();
    if (options && options.success === false && options.hasPasskey === false) {
      return { success: false, hasPasskey: false, message: options.message || 'Tài khoản chưa lưu sinh trắc học trên thiết bị.' };
    }

    const asseResp: AuthenticationResponseJSON = await startAuthentication({
      optionsJSON: options,
      useBrowserAutofill: useAutofill,
    });

    const verifyRes = await apiFetch('/auth/webauthn/login-verify', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ assertionResponse: asseResp, clientHost, clientOrigin }),
    });

    const verifyData = await verifyRes.json().catch(() => ({}));
    if (verifyRes.ok && verifyData.success) {
      return { success: true, data: verifyData, message: verifyData.message || 'Đăng nhập sinh trắc học thành công!' };
    } else {
      return { success: false, message: verifyData.message || 'Xác minh sinh trắc học không khớp.' };
    }
  } catch (err: any) {
    if (err.name === 'NotAllowedError') {
      return { success: false, message: 'Thao tác quét sinh trắc học đã bị hủy.' };
    }
    return { success: false, message: err.message || 'Đăng nhập sinh trắc học thất bại' };
  }
}

export async function checkUserPasskeyStatus(username: string): Promise<{ hasPasskey: boolean }> {
  try {
    const res = await apiFetch(`/auth/webauthn/status?username=${encodeURIComponent(username)}`);
    if (res.ok) {
      const data = await res.json();
      return { hasPasskey: Boolean(data.hasPasskey) };
    }
  } catch {}
  return { hasPasskey: false };
}

export async function registerPasskeyForCurrentUser(
  username: string,
  _deviceLabel?: string
): Promise<{ success: boolean; message: string }> {
  const res = await registerDeviceBiometrics(username);
  return {
    success: res.success,
    message: res.message || (res.success ? 'Kích hoạt sinh trắc học thành công!' : 'Kích hoạt thất bại.'),
  };
}
