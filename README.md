# D'Artagnan

A real-time multiplayer probability-based battle royale game inspired by the "Gunslinger Theory" from StarCraft II Arcade.
스타크래프트 II 아케이드의 '총잡이 이론'에서 영감을 받은 실시간 멀티플레이어 확률형 배틀로얄 게임입니다.

## Overview | 개요

D'Artagnan is a multiplayer game where players with different accuracy rates compete to be the last one standing. The game combines strategic decision-making with probability-based combat, creating an engaging experience based on the "Gunslinger's Dilemma" from game theory.

달타냥은 서로 다른 명중률을 가진 플레이어들이 최후의 1인이 되기 위해 경쟁하는 멀티플레이어 게임입니다. 게임 이론의 '총잡이의 딜레마'를 기반으로 전략적 의사결정과 확률 기반 전투를 결합하여 몰입감 있는 게임 경험을 제공합니다.

### Key Features | 주요 기능

- Real-time multiplayer gameplay
  실시간 멀티플레이어 게임플레이
- Probability-based combat system
  확률 기반 전투 시스템
- Dynamic economy with money drops
  동적 경제 시스템과 돈 드롭
- Shop system between rounds
  라운드 간 상점 시스템
- Voice chat support
  음성 채팅 지원
- Cross-platform support (Windows, macOS, Linux)
  크로스 플랫폼 지원 (Windows, macOS, Linux)

## Project Structure

```
src/
  ├── dArtagnan.Unity/     # Unity client project
  │   ├── Assets/
  │   │   ├── Scripts/     # Client-side scripts
  │   │   ├── Scenes/      # Unity scenes
  │   │   ├── Prefabs/     # Game prefabs
  │   │   └── Resources/   # Runtime resources
  │   └── ProjectSettings/ # Unity project settings
  │
  ├── dArtagnan.Server/    # Server project
  │   ├── Core/           # Server core systems
  │   ├── Network/        # Server networking
  │   └── Game/           # Game logic
  │
  └── dArtagnan.Shared/   # Shared code between client and server
      ├── Models/         # Shared data models
      └── Utils/          # Shared utilities
```

## Prerequisites

- Unity 6000.1.5f1
- .NET 6.0 SDK or later
- Git

## Setup

1. Clone the repository:
```bash
git clone https://github.com/yourusername/dArtagnan.git
cd dArtagnan
```

2. Install Unity Hub and Unity 6000.1.5f1

3. Open the project in Unity:
```bash
make run
```

4. Build and run the server:
```bash
make dev
```

## Development

### Available Make Commands

- `make run` - Run Unity in editor mode
- `make build` - Build for all platforms
- `make build-mac` - Build for macOS
- `make build-win` - Build for Windows
- `make build-linux` - Build for Linux
- `make test` - Run Unity tests
- `make update` - Update Unity packages
- `make clean` - Clean build artifacts
- `make clean-all` - Clean all project artifacts

### Game Rules | 게임 규칙

1. Player Count: 3-8 players per game
   플레이어 수: 게임당 3-8명

2. Each player starts with:
   각 플레이어는 다음을 가지고 시작합니다:
   - Random accuracy rate
     랜덤 명중률
   - Initial money
     초기 자금

3. Game Flow:
   게임 진행:
   - Players can move freely in the arena
     플레이어는 경기장에서 자유롭게 이동할 수 있습니다
   - Shooting has a cooldown period
     사격에는 쿨다운 시간이 있습니다
   - Dead players drop partial money
     사망한 플레이어는 일부 돈을 떨어뜨립니다
   - Last player standing triggers shop phase
     마지막 생존자가 상점 단계를 시작합니다
   - Shop phase allows item purchases
     상점 단계에서 아이템을 구매할 수 있습니다
   - Game continues until all but one player is bankrupt
     한 명을 제외한 모든 플레이어가 파산할 때까지 게임이 계속됩니다

### Development Guidelines | 개발 가이드라인

1. Code Style:
   코드 스타일:
   - Use PascalCase for class names
     클래스 이름은 PascalCase를 사용합니다
   - Use camelCase for methods and variables
     메소드와 변수는 camelCase를 사용합니다
   - Use UPPER_CASE for constants
     상수는 UPPER_CASE를 사용합니다
   - Add XML documentation for public methods
     public 메소드에 XML 문서를 추가합니다

2. Version Control:
   버전 관리:
   - Create feature branches for new features
     새로운 기능은 feature 브랜치를 생성합니다
   - Use meaningful commit messages
     의미 있는 커밋 메시지를 사용합니다
   - Test before merging to main
     main 브랜치에 병합하기 전에 테스트합니다

3. Testing:
   테스트:
   - Write unit tests for core mechanics
     핵심 메커니즘에 대한 단위 테스트를 작성합니다
   - Test networking features
     네트워킹 기능을 테스트합니다
   - Playtest new features
     새로운 기능을 플레이테스트합니다

## Contributing | 기여하기

1. Fork the repository
   저장소를 포크합니다
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
   기능 브랜치를 생성합니다 (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
   변경사항을 커밋합니다 (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
   브랜치에 푸시합니다 (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
   Pull Request를 생성합니다

## Acknowledgments | 감사의 말

- Inspired by "[Gunslinger Theory](https://namu.wiki/w/총잡이%20이론)" from StarCraft II Arcade
  스타크래프트 II 아케이드의 '총잡이 이론'에서 영감을 받았습니다
- Based on the "Gunslinger's Dilemma" from game theory
  게임 이론의 '총잡이의 딜레마'를 기반으로 합니다 
