@echo off
echo === D'Artagnan Server (Windows) ===
echo.

echo [1/2] Building Docker image...
docker build -t dartagnan-gameserver:latest -f Dockerfile.server .
if %ERRORLEVEL% neq 0 (
    echo Failed to build Docker image
    pause
    exit /b 1
)

echo [2/2] Starting lobby server...
cd src\dArtagnan.Lobby
if not exist node_modules (
    echo Installing dependencies...
    npm install
)
echo.
echo === Server started ===
echo Lobby: http://localhost:3000
echo.
node server.js