package com.zonemart.delivery;

import androidx.annotation.NonNull;
import androidx.biometric.BiometricManager;
import androidx.biometric.BiometricPrompt;
import androidx.core.content.ContextCompat;
import androidx.fragment.app.FragmentActivity;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;

import java.util.concurrent.Executor;

@CapacitorPlugin(name = "BiometricAuth")
public class BiometricPlugin extends Plugin {

    @PluginMethod
    public void isAvailable(PluginCall call) {
        JSObject ret = new JSObject();
        try {
            BiometricManager biometricManager = BiometricManager.from(getContext());
            int authenticators = BiometricManager.Authenticators.BIOMETRIC_STRONG | BiometricManager.Authenticators.BIOMETRIC_WEAK;
            int canAuthenticate = biometricManager.canAuthenticate(authenticators);

            if (canAuthenticate == BiometricManager.BIOMETRIC_SUCCESS) {
                ret.put("isAvailable", true);
                ret.put("biometryType", "fingerprint");
                ret.put("code", "BIOMETRIC_SUCCESS");
                ret.put("message", "Thiết bị hỗ trợ sinh trắc học và đã cài đặt.");
            } else {
                ret.put("isAvailable", false);
                ret.put("biometryType", "none");
                switch (canAuthenticate) {
                    case BiometricManager.BIOMETRIC_ERROR_NO_HARDWARE:
                        ret.put("code", "NO_HARDWARE");
                        ret.put("message", "Thiết bị không có phần cứng sinh trắc học.");
                        break;
                    case BiometricManager.BIOMETRIC_ERROR_HW_UNAVAILABLE:
                        ret.put("code", "HW_UNAVAILABLE");
                        ret.put("message", "Cảm biến sinh trắc học hiện không khả dụng.");
                        break;
                    case BiometricManager.BIOMETRIC_ERROR_NONE_ENROLLED:
                        ret.put("code", "NONE_ENROLLED");
                        ret.put("message", "Thiết bị chưa cài đặt dữ liệu vân tay hoặc khuôn mặt.");
                        break;
                    case BiometricManager.BIOMETRIC_ERROR_SECURITY_UPDATE_REQUIRED:
                        ret.put("code", "SECURITY_UPDATE_REQUIRED");
                        ret.put("message", "Thiết bị cần cập nhật bảo mật trước khi dùng sinh trắc học.");
                        break;
                    default:
                        ret.put("code", "UNKNOWN_ERROR");
                        ret.put("message", "Không thể xác thực sinh trắc học trên thiết bị.");
                        break;
                }
            }
            call.resolve(ret);
        } catch (Exception e) {
            ret.put("isAvailable", false);
            ret.put("error", e.getMessage());
            call.resolve(ret);
        }
    }

    @PluginMethod
    public void authenticate(final PluginCall call) {
        final String title = call.getString("title", "Xác thực Sinh Trắc Học ZoneMart");
        final String subtitle = call.getString("subtitle", "Chạm cảm biến vân tay để tiếp tục");
        final String description = call.getString("description", "Đăng nhập nhanh và an toàn vào ZoneMart");
        final String negativeButtonText = call.getString("negativeButtonText", "Hủy bỏ");

        getActivity().runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    FragmentActivity activity = (FragmentActivity) getActivity();
                    if (activity == null) {
                        JSObject ret = new JSObject();
                        ret.put("success", false);
                        ret.put("message", "Không thể truy cập Activity của ứng dụng.");
                        call.resolve(ret);
                        return;
                    }

                    Executor executor = ContextCompat.getMainExecutor(activity);

                    BiometricPrompt biometricPrompt = new BiometricPrompt(activity, executor, new BiometricPrompt.AuthenticationCallback() {
                        @Override
                        public void onAuthenticationError(int errorCode, @NonNull CharSequence errString) {
                            super.onAuthenticationError(errorCode, errString);
                            JSObject ret = new JSObject();
                            ret.put("success", false);
                            ret.put("errorCode", errorCode);
                            ret.put("message", errString.toString());
                            call.resolve(ret);
                        }

                        @Override
                        public void onAuthenticationSucceeded(@NonNull BiometricPrompt.AuthenticationResult result) {
                            super.onAuthenticationSucceeded(result);
                            JSObject ret = new JSObject();
                            ret.put("success", true);
                            ret.put("message", "Xác thực sinh trắc học thành công!");
                            call.resolve(ret);
                        }

                        @Override
                        public void onAuthenticationFailed() {
                            super.onAuthenticationFailed();
                            // Vân tay không khớp - hệ thống Android tự rung và hiển thị thử lại
                        }
                    });

                    BiometricPrompt.PromptInfo promptInfo = new BiometricPrompt.PromptInfo.Builder()
                            .setTitle(title)
                            .setSubtitle(subtitle)
                            .setDescription(description)
                            .setNegativeButtonText(negativeButtonText)
                            .setAllowedAuthenticators(BiometricManager.Authenticators.BIOMETRIC_STRONG | BiometricManager.Authenticators.BIOMETRIC_WEAK)
                            .build();

                    biometricPrompt.authenticate(promptInfo);
                } catch (Exception e) {
                    JSObject ret = new JSObject();
                    ret.put("success", false);
                    ret.put("message", "Lỗi khởi động cảm biến sinh trắc học: " + e.getMessage());
                    call.resolve(ret);
                }
            }
        });
    }
}
