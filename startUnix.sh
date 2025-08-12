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

# 서버 정보 출력 및 설정
if [ "$IS_AWS" = true ]; then
    echo "Lobby: http://localhost:3000 (Backend)"
    echo "Public: http://dartagnan.shop (Nginx → Backend)"
    # 게임 서버도 도메인 사용
    export GAME_SERVER_HOST="dartagnan.shop"
else
    echo "Lobby: http://localhost:3000"
fi

echo
node server.js