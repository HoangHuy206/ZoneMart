import { initializeApp } from "firebase/app";
import {
  getAuth,
  RecaptchaVerifier,
  signInWithPhoneNumber,
  type ConfirmationResult
} from "firebase/auth";

export const firebaseConfig = {
  apiKey: "AIzaSyC3VAt43G0IAzfiwhEP1xFoJFVplx7VPLs",
  authDomain: "zonemart-f1df6.firebaseapp.com",
  projectId: "zonemart-f1df6",
  storageBucket: "zonemart-f1df6.firebasestorage.app",
  messagingSenderId: "283758241833",
  appId: "1:283758241833:web:035bc426c83416feeb5438",
  measurementId: "G-S80W7D7T3V"
};

export const app = initializeApp(firebaseConfig);
export const auth = getAuth(app);

let confirmationResult: ConfirmationResult | null = null;
let recaptchaVerifier: RecaptchaVerifier | null = null;

export function getOrCreateRecaptchaVerifier(containerId = "recaptcha-container"): RecaptchaVerifier {
  if (recaptchaVerifier) {
    try {
      recaptchaVerifier.clear();
    } catch {}
    recaptchaVerifier = null;
  }

  recaptchaVerifier = new RecaptchaVerifier(auth, containerId, {
    size: "invisible",
    callback: () => {}
  });

  return recaptchaVerifier;
}

export async function sendFirebaseSmsOtp(phoneNumber: string, containerId = "recaptcha-container"): Promise<boolean> {
  let clean = phoneNumber.trim().replace(/\s+/g, "");
  if (clean.startsWith("0")) {
    clean = "+84" + clean.substring(1);
  } else if (!clean.startsWith("+")) {
    clean = "+84" + clean;
  }

  const verifier = getOrCreateRecaptchaVerifier(containerId);
  confirmationResult = await signInWithPhoneNumber(auth, clean, verifier);
  return true;
}

export async function verifyFirebaseSmsOtp(otpCode: string): Promise<any> {
  if (!confirmationResult) {
    throw new Error("Chưa gửi mã OTP! Vui lòng bấm gửi mã trước.");
  }
  const res = await confirmationResult.confirm(otpCode.trim());
  return res.user;
}
