# D'Artagnan

A real-time multiplayer probability-based battle royale game inspired by the "Gunslinger Theory" from StarCraft II Arcade.
스타크래프트 II 아케이드의 '총잡이 이론'에서 영감을 받은 실시간 멀티플레이어 확률형 배틀로얄 게임입니다.

## Quick Start | 빠른 시작

### Windows (로컬)
```cmd
start.bat
```

### Linux/Mac (로컬)
```bash
chmod +x start.sh
./start.sh
```

### AWS EC2 (클라우드)
```bash
chmod +x start.sh
./start.sh
```
AWS 환경이 자동 감지되어 퍼블릭 IP가 설정됩니다.

## Architecture | 아키텍처

```
Host Machine
├── Lobby Server (Node.js) - Port 3000
└── Game Servers (Docker containers) - Dynamic ports
```

- **Lobby Server**: Node.js로 직접 실행, WebSocket + HTTP API 제공
- **Game Servers**: Docker 컨테이너로 동적 생성/관리, .NET TCP 서버

### 환경별 설정

#### 로컬 환경
- 로비 서버: `http://localhost:3000`
- 게임 서버: Docker 컨테이너 자동 생성 (localhost)

#### AWS EC2
- 로비 서버: `http://[퍼블릭IP]:3000` (자동 감지)
- 게임 서버: Docker 컨테이너 자동 생성 (퍼블릭 IP 사용)
- Unity 클라이언트: AWS 서버 선택 시 `http://13.125.222.113:3000`

## Test Client | 테스트 클라이언트

```bash
cd src/dArtagnan.ClientTest
dotnet run
```

### Commands | 명령어
- `lg` or `login` - 로그인 (기본값: test, http://localhost:3000)
- `ct` or `connect` - 게임 서버 연결 (기본값: localhost 3000)
- `cr` or `create_room` - 방 생성
- `jr` or `join_room` - 방 참가

## Requirements | 요구사항

- .NET 8.0+
- Node.js 18+
- Docker

## Acknowledgments | 감사의 말

- Inspired by "[Gunslinger Theory](https://namu.wiki/w/총잡이%20이론)" from StarCraft II Arcade
- Based on the "Gunslinger's Dilemma" from game theory
- 이 성과는 2025년도 과학기술정보통신부의 재원으로 정보통신기획평가원의 지원을 받아 수행된 연구임(IITP-2025-SW마에스트로과정).