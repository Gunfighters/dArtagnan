#!/bin/bash
# 배포용 ec2 스크립트
echo "=== D'Artagnan Server (EC2) ==="
echo

# Docker 이미지 빌드
echo "[1/2] Building Docker image..."
docker build -t dartagnan-gameserver:latest -f Dockerfile.server .
if [ $? -ne 0 ]; then
    echo "Failed to build Docker image"
    exit 1
fi

# 로비 서버 시작
echo "[2/2] Starting lobby server..."
cd src/dArtagnan.Lobby
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

echo
echo "=== Server started ==="
echo "Lobby: http://dartagnan.shop"
echo

# EC2에서는 도메인 사용
export PUBLIC_DOMAIN="dartagnan.shop"
node server.js