#!/bin/bash
# 로컬 맥 테스트 용
echo "=== D'Artagnan Server (Mac) ==="
echo

# Docker 이미지 빌드
echo "[1/2] Building Docker image..."
docker build -t dartagnan-gameserver:v2 -f Dockerfile.server .
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
echo "Lobby: http://localhost:3000"
echo

# Mac에서는 localhost 사용 (server.js 기본값)
node server.js