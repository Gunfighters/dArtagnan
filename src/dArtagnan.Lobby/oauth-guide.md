# OAuth 설정 가이드

## Google OAuth 설정 방법

### 1. Google Cloud Console 접속
https://console.cloud.google.com/ 접속

### 2. 프로젝트 생성 또는 선택
- 새 프로젝트 만들기 또는 기존 프로젝트 선택

### 3. OAuth 동의 화면 설정
- 왼쪽 메뉴 → "API 및 서비스" → "OAuth 동의 화면"
- 사용자 유형: **외부** 선택
- 앱 이름: "dArtagnan"
- 사용자 지원 이메일: 본인 이메일
- 개발자 연락처: 본인 이메일
- 저장

### 4. OAuth 클라이언트 ID 생성
- 왼쪽 메뉴 → "API 및 서비스" → "사용자 인증 정보"
- "+ 사용자 인증 정보 만들기" → "OAuth 클라이언트 ID"
- 애플리케이션 유형: **웹 애플리케이션**
- 이름: "dArtagnan Local"
- 승인된 리디렉션 URI: `http://localhost:3000/auth/google/callback`
- 만들기

### 5. 환경변수 설정
1. `.env.example`을 `.env`로 복사
2. 발급받은 클라이언트 ID와 Secret을 입력:

```bash
cp .env.example .env
# .env 파일을 열어서 다음을 수정:
GOOGLE_CLIENT_ID=여기에_발급받은_클라이언트_ID
GOOGLE_CLIENT_SECRET=여기에_발급받은_시크릿
```

## MySQL 설정

### 1. MySQL 설치 확인
```bash
mysql --version
```

### 2. 데이터베이스 생성
```sql
mysql -u root -p
CREATE DATABASE dartagnan;
```

### 3. 환경변수 설정
`.env` 파일에서 MySQL 설정 수정:
```bash
DB_PASSWORD=your_mysql_root_password
```

## 테스트

### 1. 서버 실행
```bash
node server.js
```

### 2. OAuth 테스트
브라우저에서 접속: http://localhost:3000/auth/google

### 3. 예상 플로우
1. 구글 로그인 페이지로 리다이렉트
2. 구글 계정으로 로그인
3. 신규 사용자면 닉네임 설정 요구
4. 기존 사용자면 바로 로그인 완료

## API 엔드포인트

- `GET /auth/google` - 구글 로그인 시작
- `POST /set-nickname` - 닉네임 설정 (신규 사용자)
- `POST /login` - 기존 방식 로그인 (호환성)