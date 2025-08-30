# dArtagnan Lobby Server

Unity 멀티플레이어 게임용 로비 서버 (Node.js + WebSocket + OAuth)

## 🚀 빠른 시작

### 1. 데이터베이스 설정

#### 방법 1: 자동 스크립트 (추천)
```bash
cd scripts
setup-db.bat
```

#### 방법 2: 수동 설정
```sql
mysql -u root -p
CREATE DATABASE dartagnan CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
exit
```

### 2. 환경변수 설정
```bash
# .env 파일 생성
cp .env.tmp .env

# Google OAuth 설정 (웹 클라이언트 정보)
GOOGLE_CLIENT_ID=your-web-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-web-client-secret

# 데이터베이스 설정
DB_PASSWORD=your-mysql-password
```

### 3. 서버 실행
```bash
npm install
node server.js
```

## 📋 주요 기능

- **Google OAuth 로그인**: Unity 안드로이드 앱 OAuth 지원
- **WebSocket 통신**: 실시간 멀티플레이어 게임
- **Docker 게임서버**: 동적 게임방 생성
- **MySQL 사용자 관리**: OAuth 사용자 정보 저장

## 🔧 데이터베이스 스키마

### users 테이블
- **UTF-8 완전 지원**: 이모지, 다국어 닉네임 가능
- **OAuth 통합**: Google, Apple 등 다중 Provider 지원
- **임시 닉네임**: 자동 생성 후 사용자가 변경 가능

## 🌐 API 엔드포인트

### OAuth 로그인
- `POST /auth/google/verify-token` - Unity에서 Google ID Token 검증
- `GET /auth/google` - 웹 브라우저용 Google OAuth (개발용)

### 게임 서버 관리  
- `POST /internal/rooms/:roomId/state` - 게임서버 상태 업데이트

## 🎮 Unity 연동

1. **Google Play Games Plugin** 설치
2. **OAuth 전용 Login 씬** 사용
3. **WebSocket 연결**로 게임 진입

## 📁 파일 구조

```
src/dArtagnan.Lobby/
├── server.js          # 메인 서버
├── db.js              # MySQL 연결 및 사용자 관리
├── auth.js            # Google OAuth 설정
├── scripts/           # 데이터베이스 초기화 스크립트
│   ├── init-db.sql
│   └── setup-db.bat
├── .env.tmp           # 환경변수 템플릿 (로컬 개발용)
├── .env.production    # 환경변수 템플릿 (AWS 배포용)
└── README.md
```

## 🔒 보안 고려사항

- **Authorization Code Flow**: 가장 안전한 OAuth 방식
- **서버 사이드 검증**: Google 토큰을 서버에서 검증
- **sessionId 기반**: 자체 세션 관리로 보안 강화

## 🚨 문제 해결

### 502 OAuth Fail
1. MySQL 서버 실행 확인
2. `dartagnan` 데이터베이스 존재 확인  
3. `.env` 파일의 Google 클라이언트 ID/SECRET 확인

### MySQL 연결 실패
1. 데이터베이스 생성: `scripts/setup-db.bat` 실행
2. 비밀번호 확인: `.env` 파일의 `DB_PASSWORD` 확인
3. MySQL 서비스 실행 확인