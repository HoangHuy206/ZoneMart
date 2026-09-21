@echo off
title ZoneMart Face ID AI Microservice (UniFace)
echo ========================================================
echo   ZoneMart Face ID Biometric Service (Port 8000)
echo   Model: UniFace v4.0 (SCRFD + ArcFace + MiniFASNet)
echo ========================================================
cd /d "%~dp0face_service"
python -m uvicorn main:app --host 127.0.0.1 --port 8000
pause

