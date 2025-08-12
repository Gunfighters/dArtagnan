# D'Artagnan

A real-time multiplayer probability-based battle royale game inspired by the "Gunslinger Theory" from StarCraft II Arcade.
스타크래프트 II 아케이드의 '총잡이 이론'에서 영감을 받은 실시간 멀티플레이어 확률형 배틀로얄 게임입니다.

## Quick Start | 빠른 시작

### Windows (로컬 개발)
```cmd
window.bat
```

### Linux/Mac (로컬 개발)
```bash
chmod +x startUnix.sh
./unix.sh
```

### AWS EC2 (프로덕션)
```bash
chmod +x startUnix.sh
./unix.sh
```
AWS 환경이 자동 감지되어 도메인 기반 서비스가 설정됩니다.

## Architecture | 아키텍처

```
Client (Unity)
      ↓ HTTPS/WSS
   Nginx (SSL)
      ↓ HTTP/WS  
Lobby Server (Node.js) - Port 3000
      ↓ Docker API
Game Servers (Docker containers) - Dynamic ports
```

- **Unity Client**: Windows, Android, iOS 지원
- **Nginx**: HTTPS/WSS 종료, 리버스 프록시
- **Lobby Server**: 매칭, 방 관리, WebSocket API
- **Game Servers**: 실시간 게임 로직, TCP 통신

### 환경별 설정

#### 로컬 개발 환경
- Unity 클라이언트: `http://localhost:3000` 선택
- 로비 서버: `http://localhost:3000`
- 게임 서버: Docker 컨테이너 (localhost)

#### 프로덕션 환경 (dartagnan.shop)
- Unity 클라이언트: `https://dartagnan.shop` 선택  
- Nginx: HTTPS/WSS → HTTP/WS 변환
- 로비 서버: `http://localhost:3000` (백엔드)
- 게임 서버: Docker 컨테이너 (도메인 기반)

### 주요 기능

- **크로스 플랫폼**: Windows, Android, iOS
- **실시간 매칭**: WebSocket 기반 빠른 매칭
- **동적 스케일링**: Docker 컨테이너 자동 생성/삭제
- **보안 통신**: HTTPS/WSS (모바일 호환)

## Requirements | 요구사항

- .NET 8.0+
- Node.js 18+
- Docker
- Unity 2022.3 LTS+

## Acknowledgments | 감사의 말

- Inspired by "[Gunslinger Theory](https://namu.wiki/w/총잡이%20이론)" from StarCraft II Arcade
- Based on the "Gunslinger's Dilemma" from game theory
- 이 성과는 2025년도 과학기술정보통신부의 재원으로 정보통신기획평가원의 지원을 받아 수행된 연구임(IITP-2025-SW마에스트로과정).