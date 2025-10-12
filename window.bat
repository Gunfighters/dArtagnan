@echo off
REM # 로컬 윈도우 테스트 용
echo === D'Artagnan Server (Windows) ===
echo.

REM Docker 이미지 빌드
echo [1/2] Building Docker image...
docker build -t dartagnan-gameserver:v2 -f Dockerfile.server .
if %ERRORLEVEL% neq 0 (
    echo Failed to build Docker image
    pause
    exit /b 1
)

REM 로비 서버 시작
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

REM Windows에서는 localhost 사용 (server.js 기본값)
node server.js