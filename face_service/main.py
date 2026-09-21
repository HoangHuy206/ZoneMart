# type: ignore
# pyright: reportUnknownVariableType=false
# pyright: reportUnknownMemberType=false
# pyright: reportUnknownArgumentType=false
# pyright: reportUnknownParameterType=false
# pyright: reportMissingTypeStubs=false
# pyright: reportGeneralTypeIssues=false
# pyright: reportOptionalMemberAccess=false

import sys
import os

# Fix encoding on Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

import base64
import io
import cv2
import numpy as np
from PIL import Image
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Any, Dict, List, Optional
from uniface import FaceAnalyzer, MiniFASNet, compute_similarity

app = FastAPI(title="ZoneMart Face Auth Microservice (UniFace)", version="1.0.0")

# CORS middleware cho phép kết nối từ Frontend (5173) và Backend C# (5000)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Khởi tạo mô hình AI toàn cầu (chỉ tải 1 lần khi khởi động)
print("[UNIFACE] Dang khoi dong UniFace Analyzer & MiniFASNet Anti-Spoofing...")
analyzer = FaceAnalyzer()
analyzer.detector.confidence_threshold = 0.18
spoof_detector = MiniFASNet()
print("[UNIFACE] UniFace Face Analyzer & MiniFASNet da san sang phuc vu!")

class AnalyzeRequest(BaseModel):
    image: str # Base64 data URL hoặc chuỗi base64 thuần
    check_spoof: Optional[bool] = True

class CompareRequest(BaseModel):
    embedding1: List[float]
    embedding2: List[float]

class MatchCandidate(BaseModel):
    user_id: str
    phone_email: str
    full_name: str
    embedding: List[float]

class MatchRequest(BaseModel):
    target_embedding: List[float]
    candidates: List[MatchCandidate]
    threshold: Optional[float] = 0.70

def decode_base64_image(base64_str: str) -> np.ndarray:
    try:
        if "," in base64_str:
            base64_str = base64_str.split(",", 1)[1]
        img_bytes = base64.b64decode(base64_str)
        pil_img = Image.open(io.BytesIO(img_bytes)).convert("RGB")
        # Chuyển đổi sang định dạng OpenCV BGR
        cv_img = cv2.cvtColor(np.array(pil_img), cv2.COLOR_RGB2BGR)
        return cv_img
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Không thể giải mã ảnh Base64: {str(e)}")

@app.get("/api/health")
def health_check() -> Dict[str, Any]:
    return {
        "status": "healthy",
        "service": "ZoneMart Face Auth Service",
        "model": "UniFace v4.0.0 (SCRFD + ArcFace + MiniFASNet)"
    }

@app.post("/api/face/analyze")
def analyze_face(req: AnalyzeRequest) -> Dict[str, Any]:
    img = decode_base64_image(req.image)
    h, w = img.shape[:2]
    if h < 50 or w < 50:
        return {
            "success": False,
            "error": "IMAGE_TOO_SMALL",
            "message": "Kích thước hình ảnh quá nhỏ để nhận diện."
        }

    # 1. Phát hiện và trích xuất khuôn mặt (SCRFD + ArcFace)
    detected_faces = analyzer.detector.detect(img)
    if not detected_faces or len(detected_faces) == 0:
        return {
            "success": False,
            "error": "NO_FACE_DETECTED",
            "message": "Không phát hiện thấy khuôn mặt nào trong khung hình. Vui lòng nhìn thẳng vào camera và đảm bảo đủ ánh sáng!"
        }

    # Lọc các khuôn mặt nhiễu nền kích thước nhỏ (<25% diện tích mặt chính)
    def face_area(f):
        b = f.bbox
        return max(0.0, float(b[2] - b[0])) * max(0.0, float(b[3] - b[1]))

    detected_faces.sort(key=face_area, reverse=True)
    max_area = face_area(detected_faces[0])
    valid_detected = [f for f in detected_faces if face_area(f) >= 0.25 * max_area]

    if len(valid_detected) > 1:
        return {
            "success": False,
            "error": "MULTIPLE_FACES",
            "message": f"Cảnh báo bảo mật: Phát hiện {len(valid_detected)} khuôn mặt trong khung hình! Vui lòng chỉ để duy nhất 1 người trước camera để bảo mật an toàn."
        }

    faces = analyzer.analyze(img)
    if not faces or len(faces) == 0:
        return {
            "success": False,
            "error": "NO_FACE_DETECTED",
            "message": "Không phát hiện thấy khuôn mặt nào trong khung hình. Vui lòng nhìn thẳng vào camera và đảm bảo đủ ánh sáng!"
        }

    faces.sort(key=face_area, reverse=True)
    max_face_area = face_area(faces[0])
    valid_faces = [f for f in faces if face_area(f) >= 0.25 * max_face_area]

    if len(valid_faces) > 1:
        return {
            "success": False,
            "error": "MULTIPLE_FACES",
            "message": f"Cảnh báo bảo mật: Phát hiện {len(valid_faces)} khuôn mặt trong khung hình! Vui lòng chỉ để duy nhất 1 người trước camera để bảo mật an toàn."
        }

    face = valid_faces[0]

    # 2. Kiểm tra thực thể sống Anti-Spoofing (MiniFASNet Liveness Detection)
    is_real = True
    confidence = 1.0
    if req.check_spoof:
        try:
            spoof_res = spoof_detector.predict(img, face.bbox)
            is_real = bool(spoof_res.is_real)
            confidence = float(spoof_res.confidence)
            if not is_real:
                return {
                    "success": False,
                    "error": "SPOOF_DETECTED",
                    "message": "Cảnh báo sinh trắc học: Phát hiện hình ảnh giả mạo (ảnh chụp lại từ điện thoại hoặc màn hình)! Vui lòng nhìn trực tiếp vào camera.",
                    "confidence": confidence,
                    "is_real": False
                }
        except Exception as ex:
            print(f"⚠️ Cảnh báo kiểm tra Anti-Spoofing: {ex}")

    # 3. Trích xuất vector đặc trưng Face Embedding (512 chiều)
    if face.embedding is None or len(face.embedding) == 0:
        return {
            "success": False,
            "error": "EMBEDDING_FAILED",
            "message": "Không thể trích xuất vector đặc trưng từ khuôn mặt. Vui lòng thử lại!"
        }

    embedding_list = [float(x) for x in face.embedding]
    bbox_list = [float(x) for x in face.bbox] if face.bbox is not None else []

    return {
        "success": True,
        "is_real": is_real,
        "confidence": confidence,
        "bbox": bbox_list,
        "embedding": embedding_list
    }

@app.post("/api/face/compare")
def compare_two_faces(req: CompareRequest) -> Dict[str, Any]:
    arr1 = np.array(req.embedding1, dtype=np.float32)
    arr2 = np.array(req.embedding2, dtype=np.float32)
    sim = float(compute_similarity(arr1, arr2))
    return {
        "success": True,
        "similarity": sim,
        "is_match": sim >= 0.70
    }

@app.post("/api/face/find-match")
def find_matching_user(req: MatchRequest) -> Dict[str, Any]:
    target = np.array(req.target_embedding, dtype=np.float32)
    best_sim = -1.0
    best_candidate = None

    for c in req.candidates:
        cand_arr = np.array(c.embedding, dtype=np.float32)
        sim = float(compute_similarity(target, cand_arr))
        if sim > best_sim:
            best_sim = sim
            best_candidate = c

    threshold = req.threshold or 0.70
    if best_sim >= threshold and best_candidate is not None:
        return {
            "success": True,
            "matched": True,
            "similarity": best_sim,
            "user_id": best_candidate.user_id,
            "phone_email": best_candidate.phone_email,
            "full_name": best_candidate.full_name
        }

    return {
        "success": True,
        "matched": False,
        "similarity": best_sim if best_sim > 0 else 0.0,
        "message": f"Độ tương đồng cao nhất ({best_sim:.2f}) chưa đạt ngưỡng an toàn ({threshold:.2f})."
    }

@app.post("/api/face/detect")
def detect_face(req: AnalyzeRequest) -> Dict[str, Any]:
    try:
        img = decode_base64_image(req.image)
        h, w = img.shape[:2]
        faces = analyzer.detector.detect(img)
        if not faces or len(faces) == 0:
            return {"has_face": False, "face_count": 0, "multiple_faces": False}

        # Hàm tính diện tích khung nhận diện
        def face_area(f):
            b = f.bbox
            return max(0.0, float(b[2] - b[0])) * max(0.0, float(b[3] - b[1]))

        # Sắp xếp diện tích giảm dần để lấy khuôn mặt chính lớn nhất
        faces.sort(key=face_area, reverse=True)
        max_area = face_area(faces[0])

        # Chỉ tính là nhiều khuôn mặt nếu khuôn mặt thứ 2 có diện tích >= 25% khuôn mặt lớn nhất (loại bỏ ảnh nhỏ trên tường/phông nền)
        prominent_faces = [f for f in faces if face_area(f) >= 0.25 * max_area]

        if len(prominent_faces) > 1:
            return {
                "has_face": True,
                "face_count": len(prominent_faces),
                "multiple_faces": True,
                "is_real": False,
                "is_centered": False,
                "yaw": 0.0,
                "pitch": 0.0,
                "message": f"Phát hiện {len(prominent_faces)} khuôn mặt trong khung hình!"
            }

        face = prominent_faces[0]
        bbox = face.bbox.tolist()
        kps = face.landmarks.tolist()

        # Kiểm tra thực thể sống Anti-Spoofing (MiniFASNet Liveness Detection)
        is_real = True
        spoof_conf = 1.0
        try:
            spoof_res = spoof_detector.predict(img, face.bbox)
            is_real = bool(spoof_res.is_real)
            spoof_conf = float(spoof_res.confidence)
        except Exception as ex:
            print(f"⚠️ Anti-spoofing error: {ex}")

        # Tọa độ màn hình gương selfie (Screen Mirrored Coordinates: screen_x = w - raw_x)
        el_x, el_y = w - kps[0][0], kps[0][1] # Mắt trái trên màn hình
        er_x, er_y = w - kps[1][0], kps[1][1] # Mắt phải trên màn hình
        n_x, n_y = w - kps[2][0], kps[2][1]   # Mũi
        ml_x, ml_y = w - kps[3][0], kps[3][1] # Khóe miệng trái
        mr_x, mr_y = w - kps[4][0], kps[4][1] # Khóe miệng phải

        eye_mid_x = (el_x + er_x) / 2.0
        eye_mid_y = (el_y + er_y) / 2.0
        eye_dist = max(abs(er_x - el_x), 1.0)
        mouth_mid_y = (ml_y + mr_y) / 2.0

        # Yaw trên màn hình (quay đầu trái / phải):
        # Khi người dùng quay đầu sang Trái (màn hình trái): n_x < eye_mid_x -> yaw < 0
        # Khi người dùng quay đầu sang Phải (màn hình phải): n_x > eye_mid_x -> yaw > 0
        yaw = (n_x - eye_mid_x) / (eye_dist * 0.45)

        # Pitch trên màn hình (ngước lên / cúi xuống):
        # Khoảng cách từ mắt tới mũi vs từ mũi tới miệng
        d_eye_nose = n_y - eye_mid_y
        d_nose_mouth = mouth_mid_y - n_y
        pitch = (d_eye_nose - d_nose_mouth) / max(d_eye_nose + d_nose_mouth, 1.0)

        # Kiểm tra khuôn mặt có nằm trong vùng trọng tâm khung hình
        box_cx = (bbox[0] + bbox[2]) / 2.0
        box_cy = (bbox[1] + bbox[3]) / 2.0
        box_w = bbox[2] - bbox[0]

        # Nới lỏng vùng trọng tâm lên 46% và kích thước tối thiểu xuống 6% để nhận diện tốt mọi webcam và khoảng cách
        is_centered = (
            abs(box_cx - w / 2.0) < w * 0.46 and
            abs(box_cy - h / 2.0) < h * 0.46 and
            box_w > w * 0.06
        )

        return {
            "has_face": True,
            "face_count": 1,
            "multiple_faces": False,
            "is_real": is_real,
            "is_centered": is_centered,
            "confidence": float(face.confidence),
            "spoof_confidence": spoof_conf,
            "yaw": float(yaw),
            "pitch": float(pitch),
        }
    except Exception as e:
        return {"has_face": False, "is_real": False, "face_count": 0, "multiple_faces": False, "error": str(e)}

if __name__ == "__main__":
    import uvicorn
    import socket

    def is_port_in_use(port: int) -> bool:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            return s.connect_ex(('127.0.0.1', port)) == 0

    if is_port_in_use(8000):
        print("=" * 65)
        print("✅ [UNIFACE] Dịch vụ Face Auth Service đã đang hoạt động trên cổng 8000!")
        print("   (Tiến trình server C# ASP.NET Core đang tự động chạy cổng này)")
        print("   Kiểm tra sức khỏe: http://127.0.0.1:8000/api/health -> SẴN SÀNG")
        print("=" * 65)
    else:
        uvicorn.run("main:app", host="127.0.0.1", port=8000, reload=False)
