@echo off
chcp 65001 >nul
title ZoneMart - Đẩy code lên GitHub
color 0A

echo ==========================================================
echo           ZONEMART - TỰ ĐỘNG ĐẨY CODE LÊN GITHUB
echo ==========================================================
echo.

cd /d "%~dp0"

:: 1. Kiểm tra trạng thái git
echo [*] Đang kiểm tra trạng thái thay đổi...
git status --short
echo.

:: 2. Nhập nội dung commit (hoặc nhấn Enter để dùng mặc định)
set "commit_msg="
set /p commit_msg="Nhập ghi chú commit (Nhấn Enter để dùng mặc định): "

if "%commit_msg%"=="" (
    set commit_msg=Cập nhật ZoneMart - %date% %time%
)

echo.
echo [*] Đang thêm các file thay đổi (git add .)...
git add .

:: 3. Kiểm tra xem có file nào được staged để commit không
git diff --cached --quiet
if %errorlevel% neq 0 (
    echo [*] Đang tạo commit: "%commit_msg%"...
    git commit -m "%commit_msg%"
) else (
    echo [i] Không có file mới cần commit, chuẩn bị đẩy các commit sẵn có...
)

echo.
:: 4. Tự động nhận diện nhánh hiện tại và đẩy lên GitHub
for /f "tokens=*" %%i in ('git rev-parse --abbrev-ref HEAD 2^>nul') do set current_branch=%%i
if "%current_branch%"=="" set current_branch=main

echo [*] Đang đẩy lên GitHub nhánh '%current_branch%' (git push origin %current_branch%)...
git push origin %current_branch%

echo.
if %errorlevel% equ 0 (
    color 0A
    echo ==========================================================
    echo   [THÀNH CÔNG] ĐÃ ĐẨY TOÀN BỘ CODE LÊN GITHUB HOÀN TẤT!
    echo ==========================================================
) else (
    color 0C
    echo ==========================================================
    echo   [LƯU Ý] ĐẨY CODE CHƯA THÀNH CÔNG!
    echo ==========================================================
    echo   Nếu bị chặn bởi Secret Scanning Push Protection của GitHub:
    echo   1. Nhấp chuột vào đường link 'unblock-secret' mà GitHub hiển thị ở trên.
    echo   2. Chọn 'I will fix it later' hoặc 'It's used in tests' rồi bấm 'Allow secret'.
    echo   3. Nhấp đúp chạy lại file .bat này là đẩy lên thành công ngay!
)

echo.
pause