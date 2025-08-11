#!/bin/bash
echo "=== D'Artagnan Server (Unix/Linux) ==="
echo

# AWS 환경 감지
IS_AWS=false
if curl -s --max-time 3 http://169.254.169.254/latest/meta-data/instance-id >/dev/null 2>&1; then
    IS_AWS=true
    echo "AWS EC2 환경 감지됨"
else
    echo "로컬 환경에서 실행"
fi

echo "[1/2] Building Docker image..."
docker build -t dartagnan-gameserver:latest -f Dockerfile.server .
if [ $? -ne 0 ]; then
    echo "Failed to build Docker image"
    exit 1
fi

echo "[2/2] Starting lobby server..."
cd src/dArtagnan.Lobby
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

echo
echo "=== Server started ==="

# AWS인 경우 퍼블릭 IP 설정
if [ "$IS_AWS" = true ]; then
    PUBLIC_IP=$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4)
    if [ -n "$PUBLIC_IP" ]; then
        echo "AWS 퍼블릭 IP: $PUBLIC_IP"
        echo "Lobby: http://$PUBLIC_IP:3000"
        export PUBLIC_IP
    else
        echo "Warning: Failed to get AWS public IP, using localhost"
        echo "Lobby: http://localhost:3000"
    fi
else
    echo "Lobby: http://localhost:3000"
fi

echo
node server.js