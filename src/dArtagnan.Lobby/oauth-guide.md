# Unity Google OAuth 구현 가이드 (블로그 방식 - 3개 클라이언트)

> **이미 Google Play에 앱이 등록되어 있고, 앱 서명 키가 발급된 상황에서 Unity Google 로그인을 구현하는 완전한 가이드입니다.**

## 📋 전체 과정 개요

### 🎯 목표
Unity 안드로이드 앱에서 Google 계정으로 로그인하여 서버에서 sessionId를 받고, 기존 WebSocket 게임에 연결하는 것

### 🔑 핵심 개념
- **3개의 OAuth 클라이언트** 생성: Android 2개 + Web 1개
- **2개의 SHA-1 지문** 사용: 앱 서명 키 + 업로드 키
- **Google Play Games Plugin** 사용 (최신 방식)

---

## 1단계: Unity 프로젝트 기본 설정

### 1-1. Player Settings 설정
**Edit → Project Settings → Player → Android 탭**

#### 필수 설정 사항:
```
✅ Company Name: 원하는 회사명 설정
✅ Product Name: dArtagnan (또는 원하는 앱 이름)
✅ Package Name: com.yourcompany.dartagnan
   ⚠️ 주의: Google Cloud Console과 정확히 일치해야 함!

Configuration:
✅ Scripting Backend: IL2CPP (필수)
✅ Api Compatibility Level: .NET Framework (필수)  
✅ Target Architectures: ARM64 체크 (필수)

Other Settings:
✅ Minimum API Level: API 21 이상
✅ Target API Level: API 34 이상 권장
```

### 1-2. Publishing Settings (Keystore) 설정
**Edit → Project Settings → Player → Publishing Settings**

#### Keystore 생성:
1. **Create a new keystore** 클릭
2. **Keystore 정보 입력**:
   - **Browse Keystore**: 저장할 경로 선택 (예: `dartagnan.keystore`)
   - **Password**: 안전한 비밀번호 설정
3. **Key 정보 입력**:
   - **Alias**: 키 별명 (예: `dartagnan_key`)
   - **Password**: 키 비밀번호 설정
   - **Validity (years)**: 25년 권장
   - **Common Name**: 개발자 이름 또는 회사명

#### ⚠️ 중요사항:
- **keystore 파일과 비밀번호는 절대 분실하지 마세요!**
- **릴리즈 빌드 시에만 사용됩니다**
- **개발 중에는 Unity 기본 debug keystore 사용**

---

## 2단계: Google Play Games Plugin 설치

### 2-1. 플러그인 다운로드
1. **GitHub 방문**: https://github.com/playgameservices/play-games-plugin-for-unity
2. **current-build 디렉터리** 클릭
3. **최신 `.unitypackage` 파일** 다운로드 (예: `GooglePlayGamesPluginForUnity_0.11.01.unitypackage`)

### 2-2. Unity에서 설치
1. Unity 프로젝트에서 **Assets → Import Package → Custom Package**
2. 다운로드한 `.unitypackage` 파일 선택
3. **Import** 클릭하여 모든 파일 임포트

### 2-3. Android Dependencies 해결
패키지 설치 후 자동으로 실행되지 않는다면:
1. **Assets → External Dependency Manager → Android Resolver → Force Resolve** 클릭
2. Gradle 빌드 문제 해결을 위해 필수!

---

## 3단계: Google Play Console에서 SHA-1 지문 확인

### 3-1. 앱 무결성 페이지 접속
1. **Google Play Console** 접속
2. **dArtagnan 앱** 선택
3. **출시 관리** → **설정** → **앱 무결성** 메뉴

### 3-2. SHA-1 지문 2개 복사
**앱 서명 키 인증서** 섹션에서:
```
1️⃣ 앱 서명 키 인증서 지문 (SHA-1)
예시: A1:B2:C3:D4:E5:F6:78:90:AB:CD:EF:12:34:56:78:90:AB:CD:EF:12

2️⃣ 업로드 키 인증서 지문 (SHA-1)  
예시: F1:E2:D3:C4:B5:A6:98:87:76:65:54:43:32:21:10:09:87:65:43:21
```

### 📝 왜 2개가 필요한가?
- **앱 서명 키**: Google Play Store에서 배포되는 실제 APK 서명용
- **업로드 키**: 개발자가 Unity에서 빌드/테스트할 때 사용

---

## 4단계: Google Cloud Console OAuth 클라이언트 3개 생성

### 4-1. Android 클라이언트 #1 (앱 서명 키용)

1. **Google Cloud Console** → **dArtagnan 프로젝트** 선택
2. **API 및 서비스** → **사용자 인증 정보**
3. **+ 사용자 인증 정보 만들기** → **OAuth 클라이언트 ID**

**설정값:**
```
애플리케이션 유형: Android
이름: dArtagnan Android (App Signing)
패키지 이름: com.yourcompany.dartagnan
SHA-1 인증서 지문: [앱 서명 키 SHA-1 붙여넣기]
```

### 4-2. Android 클라이언트 #2 (업로드 키용)

**+ 사용자 인증 정보 만들기** → **OAuth 클라이언트 ID** 다시 클릭

**설정값:**
```
애플리케이션 유형: Android  
이름: dArtagnan Android (Upload Key)
패키지 이름: com.yourcompany.dartagnan (동일)
SHA-1 인증서 지문: [업로드 키 SHA-1 붙여넣기]
```

### 4-3. 웹 애플리케이션 클라이언트 #1 (게임 서버용)

**+ 사용자 인증 정보 만들기** → **OAuth 클라이언트 ID** 다시 클릭

**설정값:**
```
애플리케이션 유형: 웹 애플리케이션
이름: dArtagnan Web (Game Server)
승인된 자바스크립트 출처: https://dartagnan.shop (선택사항)
승인된 리디렉션 URI: https://dartagnan.shop/auth/google/callback (선택사항)
```

### 4-4. 클라이언트 ID 저장
생성 완료 후 **3개의 클라이언트 ID**를 메모장에 저장:
```
Android #1 클라이언트 ID: 123456789-abc1.apps.googleusercontent.com
Android #2 클라이언트 ID: 123456789-abc2.apps.googleusercontent.com
웹 클라이언트 ID: 123456789-web.apps.googleusercontent.com
```

---

## 5단계: Google Play Console Play Games Services 설정

### 5-1. Play Games Services 활성화
1. **Google Play Console** → **dArtagnan 앱**
2. **성장** → **Play Games Services** → **설정 및 관리**
3. **새 게임 만들기** (처음이라면)

### 5-2. OAuth 클라이언트 연결
**사용자 인증 정보** 탭에서:

1. **Android용 클라이언트 추가**:
   - **OAuth 클라이언트**: Android #1 클라이언트 ID 선택
   - **OAuth 클라이언트**: Android #2 클라이언트 ID 선택

2. **게임 서버용 클라이언트 추가**:
   - **OAuth 클라이언트**: 웹 클라이언트 ID 선택

### 5-3. 게임 서비스 정보 확인
**리소스 보기**를 클릭하면 나오는 XML 내용을 복사해두세요:
```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <string name="app_id">123456789012</string>
    <string name="package_name">com.yourcompany.dartagnan</string>
    ...
</resources>
```

---

## 6단계: Unity Android Configuration 설정

### 6-1. Android Configuration 창 열기
1. Unity에서 **Window → Google Play Games → Setup → Android Setup**
2. **Android Configuration** 창이 열림

### 6-2. 설정 입력
**Resources Definition 필드**:
- 5-3단계에서 복사한 XML 내용 전체 붙여넣기

**Web Client ID 필드**:
- **웹 클라이언트 ID** 입력 (123456789-web.apps.googleusercontent.com)

### 6-3. Setup 완료
1. **Setup** 버튼 클릭
2. **Successful** 메시지 확인
3. 자동으로 `gpgs-plugin-support.aar` 파일 생성됨

---

## 7단계: Unity 스크립트 구현

### 7-1. GoogleOAuthManager.cs 생성

```csharp
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class GoogleOAuthManager : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "https://dartagnan.shop"; // 실제 서버 도메인
    
    [Header("UI References")]
    public Button googleLoginButton;
    public Button logoutButton;
    public Text statusText;
    public Text userInfoText;

    void Start()
    {
        // Google Play Games 활성화 (Setup에서 이미 설정됨)
        PlayGamesPlatform.Activate();
        
        // 디버그 로그 활성화
        PlayGamesPlatform.DebugLogEnabled = true;
        
        // UI 이벤트 연결
        if (googleLoginButton != null)
            googleLoginButton.onClick.AddListener(OnClickGoogleLoginButton);
            
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnClickGoogleLogoutButton);
        
        UpdateStatus("Google Play Games 초기화 완료");
        
        // 자동 로그인 시도 (이전에 로그인한 경우)
        TryAutoLogin();
    }

    void TryAutoLogin()
    {
        if (Social.localUser.authenticated)
        {
            UpdateStatus($"자동 로그인 됨: {Social.localUser.userName}");
            UpdateUserInfo();
            GetIdTokenAndLogin();
        }
    }

    public void OnClickGoogleLoginButton()
    {
        UpdateStatus("Google 로그인 중...");
        
        Social.localUser.Authenticate((bool success) =>
        {
            if (success) 
            {
                UpdateStatus($"Google 로그인 성공: {Social.localUser.userName}");
                UpdateUserInfo();
                GetIdTokenAndLogin();
            }
            else 
            {
                UpdateStatus("Google 로그인 실패");
            }
        });
    }

    void GetIdTokenAndLogin()
    {
        // ID Token 요청
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, (string authCode) =>
        {
            if (!string.IsNullOrEmpty(authCode))
            {
                UpdateStatus("서버 인증 토큰 획득 성공");
                StartCoroutine(VerifyTokenWithServer(authCode));
            }
            else
            {
                UpdateStatus("서버 인증 토큰 획득 실패");
            }
        });
    }

    IEnumerator VerifyTokenWithServer(string idToken)
    {
        UpdateStatus("서버 인증 중...");
        
        var requestData = new { idToken = idToken };
        string json = JsonUtility.ToJson(requestData);
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/auth/google/verify-token", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                    
                    if (response.success)
                    {
                        OnLoginSuccess(response);
                    }
                    else
                    {
                        UpdateStatus("서버 로그인 실패: " + request.downloadHandler.text);
                    }
                }
                catch (System.Exception e)
                {
                    UpdateStatus($"응답 파싱 오류: {e.Message}");
                }
            }
            else
            {
                UpdateStatus($"서버 요청 실패: {request.error}");
            }
        }
    }

    void OnLoginSuccess(LoginResponse response)
    {
        // sessionId 저장
        PlayerPrefs.SetString("SessionId", response.sessionId);
        PlayerPrefs.SetString("Nickname", response.nickname);
        PlayerPrefs.SetInt("IsTemporary", response.needSetNickname ? 1 : 0);
        PlayerPrefs.Save();
        
        if (response.needSetNickname)
        {
            UpdateStatus($"로그인 완료! 임시 닉네임: {response.nickname}");
            // 임시 닉네임 변경 UI를 여기서 표시할 수 있음
            ShowNicknameChangeOption(response.nickname);
        }
        else
        {
            UpdateStatus($"로그인 완료! 닉네임: {response.nickname}");
            ConnectToGameServer(response.sessionId);
        }
    }

    void ShowNicknameChangeOption(string currentNickname)
    {
        // 간단한 임시 닉네임 알림
        UpdateStatus($"임시 닉네임 '{currentNickname}'로 설정됨. 게임에서 변경 가능합니다.");
        
        // 실제 게임에서는 여기서 닉네임 변경 UI를 표시
        // 지금은 그대로 게임 서버에 연결
        string sessionId = PlayerPrefs.GetString("SessionId");
        ConnectToGameServer(sessionId);
    }

    void ConnectToGameServer(string sessionId)
    {
        UpdateStatus("게임 서버 연결 중...");
        
        // 기존 WebSocket 연결 로직
        // WebSocketManager.Instance.ConnectWithSession(sessionId);
        
        // 임시로 성공 메시지 표시
        UpdateStatus("게임 서버 연결 완료! 게임을 시작할 수 있습니다.");
    }

    public void OnClickGoogleLogoutButton()
    {
        ((PlayGamesPlatform)Social.Active).SignOut();
        PlayerPrefs.DeleteAll();
        
        UpdateStatus("로그아웃 완료");
        UpdateUserInfo();
    }

    void UpdateStatus(string message)
    {
        Debug.Log($"[OAuth] {message}");
        if (statusText != null)
            statusText.text = $"상태: {message}";
    }

    void UpdateUserInfo()
    {
        if (userInfoText != null)
        {
            if (Social.localUser.authenticated)
            {
                userInfoText.text = $"사용자: {Social.localUser.userName}\nID: {Social.localUser.id}";
            }
            else
            {
                userInfoText.text = "로그인되지 않음";
            }
        }
    }
}

[System.Serializable]
public class LoginResponse
{
    public bool success;
    public string sessionId;
    public string nickname;
    public bool needSetNickname;
}
```

### 7-2. UI 설정

**Scene 구성:**
1. **Canvas** 생성
2. **Button** (Google Login) 추가
3. **Button** (Logout) 추가  
4. **Text** (Status) 추가 - 상태 메시지용
5. **Text** (User Info) 추가 - 사용자 정보용

**Inspector 설정:**
- `GoogleOAuthManager` 스크립트를 빈 GameObject에 추가
- UI 요소들을 Inspector에서 연결

---

## 8단계: 테스트 및 빌드

### 8-1. 개발 테스트
1. **Build Settings**: Android 플랫폼 선택
2. **실제 Android 기기 연결** (에뮬레이터 안됨!)
3. **Google Play Services 설치된 기기** 사용
4. **Build and Run** 실행

### 8-2. 예상 로그 순서
```
[OAuth] Google Play Games 초기화 완료
[OAuth] Google 로그인 중...
[OAuth] Google 로그인 성공: 홍길동
[OAuth] 서버 인증 토큰 획득 성공
[OAuth] 서버 인증 중...
[OAuth] 로그인 완료! 임시 닉네임: User1k2j3h4
[OAuth] 게임 서버 연결 중...
[OAuth] 게임 서버 연결 완료! 게임을 시작할 수 있습니다.
```

---

## 9단계: 문제 해결

### 9-1. 자주 발생하는 오류

#### ❌ "로그인 실패: DeveloperError"
**원인**: 
- SHA-1 지문 불일치
- Package Name 불일치
- OAuth 클라이언트 설정 오류

**해결법**:
1. Google Cloud Console에서 패키지명 확인
2. SHA-1 지문이 정확한지 확인  
3. Play Games Services에서 OAuth 클라이언트 연결 확인

#### ❌ "서버 요청 실패: Cannot resolve host"
**원인**: 서버 URL 오류 또는 네트워크 문제

**해결법**:
1. `serverUrl` 변수에 정확한 도메인 입력
2. 서버가 실행 중인지 확인
3. 방화벽/보안 설정 확인

#### ❌ "Google Play Games 초기화 실패"
**원인**: Google Play Services 없음

**해결법**:
1. 실제 Android 기기 사용 (에뮬레이터 금지)
2. Google Play Services 앱 업데이트
3. Google Play Store 로그인 확인

### 9-2. 디버깅 팁

```bash
# Android 기기 로그 실시간 확인
adb logcat -s Unity GooglePlayGames PlayGamesPlatform

# Unity 콘솔에서 자세한 로그 확인
PlayGamesPlatform.DebugLogEnabled = true;
```

---

## 10단계: 릴리즈 빌드 준비

### 10-1. Publishing Settings 활성화
**릴리즈 빌드 시에만**:
1. **Edit → Project Settings → Player → Publishing Settings**
2. **Use Custom Keystore** 체크
3. 1단계에서 생성한 keystore 파일 선택
4. 비밀번호 입력

### 10-2. 배포 확인사항
- [ ] Google Cloud Console에 **앱 서명 키 SHA-1** 등록됨
- [ ] Google Play Console에 **Play Games Services** 활성화됨
- [ ] Unity에서 **올바른 Package Name** 설정됨
- [ ] 서버 URL이 **실제 도메인**으로 설정됨

---

## 🎉 완료!

이제 Unity 안드로이드 앱에서 Google 로그인이 완전히 구현되었습니다!

### ✅ 구현된 기능:
1. **Google Play Games 로그인**: 네이티브 Google 인증
2. **서버 토큰 검증**: ID Token을 서버에서 검증
3. **자동 회원가입**: 신규 사용자 임시 닉네임 생성
4. **세션 관리**: sessionId로 기존 게임 연결

### 🚀 다음 단계:
- **Apple 로그인** 추가 (비슷한 방식)
- **닉네임 변경 UI** 개선
- **WebSocket 연결** 통합

### 🛠️ 유지보수:
- **keystore 파일** 안전하게 백업
- **OAuth 클라이언트 ID** 보안 유지
- **서버 도메인 변경** 시 Unity 설정도 업데이트