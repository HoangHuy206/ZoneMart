#!/usr/bin/env bash
# ==============================================================================
# ZoneMart Automated VPS Deployment Script
# Target: Ubuntu 22.04 / 24.04 LTS (2 CPU | 2GB RAM | 60GB SSD)
# Domain: zonemart.id.vn | IP: 103.179.172.235
# ==============================================================================

set -e

DOMAIN="zonemart.id.vn"
ADMIN_EMAIL="hh9393100@gmail.com"
APP_DIR="/var/www/zonemart"
REPO_URL="https://github.com/HoangHuy206/ZoneMart.git"

echo "=========================================================="
echo "🚀 BẮT ĐẦU TRIỂN KHAI HỆ THỐNG ZONEMART LÊN VPS"
echo "🌐 Domain: https://$DOMAIN"
echo "=========================================================="

# 1. Cấu hình Swap RAM 4GB để đảm bảo VPS 2GB RAM không bị treo/OOM khi build
if [ ! -f /swapfile ]; then
    echo ">> [1/8] Đang khởi tạo 4GB Swap RAM tối ưu tài nguyên..."
    fallocate -l 4G /swapfile || dd if=/dev/zero of=/swapfile bs=1M count=4096
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
    echo "vm.swappiness=20" >> /etc/sysctl.conf
    sysctl -p
    echo ">> [OK] Đã kích hoạt 4GB Swap RAM thành công."
else
    echo ">> [1/8] Swap RAM đã tồn tại, bỏ qua."
fi

# 2. Cập nhật hệ thống và cài đặt các gói cần thiết
echo ">> [2/8] Cập nhật hệ điều hành và gói phần mềm..."
apt-get update -y
apt-get install -y curl wget git nginx certbot python3-certbot-nginx ufw htop unattended-upgrades

# 3. Cài đặt Node.js 20 LTS & npm
if ! command -v node &> /dev/null; then
    echo ">> [3/8] Đang cài đặt Node.js 20 LTS..."
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
    apt-get install -y nodejs
else
    echo ">> [3/8] Node.js đã được cài đặt: $(node -v)"
fi

# 4. Cài đặt .NET 8 / .NET 9 SDK
if ! command -v dotnet &> /dev/null; then
    echo ">> [4/8] Đang cài đặt .NET SDK..."
    # Cài đặt qua Microsoft Package Feed chính thức
    UBUNTU_RELEASE=$(lsb_release -rs)
    wget https://packages.microsoft.com/config/ubuntu/${UBUNTU_RELEASE}/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm -f packages-microsoft-prod.deb
    apt-get update -y
    apt-get install -y dotnet-sdk-8.0 || apt-get install -y dotnet-sdk-9.0 || apt-get install -y aspnetcore-runtime-8.0
else
    echo ">> [4/8] .NET đã sẵn sàng: $(dotnet --version)"
fi

# 5. Tải/Cập nhật mã nguồn ZoneMart
echo ">> [5/8] Đang chuẩn bị thư mục mã nguồn tại $APP_DIR..."
mkdir -p /var/www
if [ ! -d "$APP_DIR/.git" ]; then
    git clone "$REPO_URL" "$APP_DIR"
else
    cd "$APP_DIR"
    git fetch origin
    git reset --hard origin/main
fi

cd "$APP_DIR"

# 6. Thiết lập appsettings.Production.json
echo ">> [6/8] Kiểm tra file cấu hình appsettings.Production.json..."
if [ ! -f "$APP_DIR/server/appsettings.Production.json" ]; then
    echo ">> Tạo template appsettings.Production.json từ appsettings.json..."
    cp "$APP_DIR/server/appsettings.json" "$APP_DIR/server/appsettings.Production.json"
fi

# 7. Build Frontend Vue 3 và Publish Backend .NET
echo ">> [7/8] Đang biên dịch Frontend và Backend cho môi trường Production..."

# 7.1 Build Frontend Vue 3
cd "$APP_DIR/ZoneMart"
npm ci || npm install
npm run build

# 7.2 Publish Backend .NET
cd "$APP_DIR/server"
dotnet publish -c Release -o /var/www/zonemart/publish

# Phân quyền thư mục
chown -R www-data:www-data "$APP_DIR"
chmod -R 755 "$APP_DIR"

# 8. Cấu hình Systemd Service cho Backend .NET
echo ">> [8/8] Đang khởi tạo Systemd Service cho ZoneMart Backend..."
cat << 'EOF' > /etc/systemd/system/zonemart.service
[Unit]
Description=ZoneMart ASP.NET Core Backend API
After=network.target

[Service]
WorkingDirectory=/var/www/zonemart/publish
ExecStart=/usr/bin/dotnet /var/www/zonemart/publish/server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=zonemart-backend
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable zonemart.service
systemctl restart zonemart.service

# 9. Cấu hình Nginx Reverse Proxy
echo ">> Đang cấu hình Nginx Web Server..."
cat << 'EOF' > /etc/nginx/sites-available/zonemart
server {
    listen 80;
    listen [::]:80;
    server_name zonemart.id.vn www.zonemart.id.vn;

    client_max_body_size 50M;

    # Security Headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Frontend Vue 3 Static Files
    root /var/www/zonemart/ZoneMart/dist;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Backend API Reverse Proxy
    location /api/ {
        proxy_pass http://127.0.0.1:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_buffering off;
        proxy_read_timeout 100s;
    }

    # SignalR WebSocket Hubs
    location /chatHub {
        proxy_pass http://127.0.0.1:5000/chatHub;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Swagger Documentation (Tùy chọn)
    location /swagger {
        proxy_pass http://127.0.0.1:5000/swagger;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
    }
}
EOF

# Kích hoạt site Nginx
ln -sf /etc/nginx/sites-available/zonemart /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl restart nginx

# 10. Mở Firewall UFW
ufw allow 22/tcp || true
ufw allow 80/tcp || true
ufw allow 443/tcp || true
ufw --force enable || true

# 11. Cấp chứng chỉ SSL HTTPS miễn phí tự động
echo ">> Đang kiểm tra và kích hoạt chứng chỉ SSL HTTPS Let's Encrypt..."
if certbot --nginx -d zonemart.id.vn -d www.zonemart.id.vn --non-interactive --agree-tos --email "$ADMIN_EMAIL" --redirect; then
    echo ">> [OK] Kích hoạt chứng chỉ SSL HTTPS thành công!"
else
    echo ">> [CẢNH BÁO] Chưa thể cấp SSL tự động ngay vì bản ghi DNS có thể chưa kịp nhận diện."
    echo ">> Bạn hãy kiểm tra lại cấu hình DNS tại iNET. Sau đó chỉ cần chạy lệnh sau trên server:"
    echo "   certbot --nginx -d zonemart.id.vn -d www.zonemart.id.vn"
fi

echo "=========================================================="
echo "🎉 CHÚC MỪNG! HỆ THỐNG ZONEMART ĐÃ TRIỂN KHAI THÀNH CÔNG!"
echo "🌐 Địa chỉ trang web: https://zonemart.id.vn"
echo "🔧 Trạng thái Service: systemctl status zonemart"
echo "=========================================================="
