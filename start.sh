#!/bin/bash
echo "=== D'Artagnan Server (Unix) ==="
echo

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
echo "Lobby: http://localhost:3000"
echo
node server.js