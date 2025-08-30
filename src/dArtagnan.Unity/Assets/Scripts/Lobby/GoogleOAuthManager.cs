using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Collections;

/// <summary>
/// Google Play Games를 이용한 OAuth 로그인을 관리하는 컴포넌트
/// OAuth 전용 Login 씬에서 사용 - 2024 최신 API 사용
/// </summary>
public class GoogleOAuthManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLog = true;
    
    private bool isInitialized = false;
    
    void Start()
    {
        // 버튼 이벤트 연결
        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.AddListener(StartGoogleLogin);
        }

        // LobbyManager 이벤트 구독 (WebSocket 관련)
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnAuthComplete += OnAuthComplete;
            LobbyManager.Instance.OnError += OnError;
            
            // Force AWS server setting (OAuth scene is always for deployment)
            LobbyManager.Instance.SetServerType(true);
        }

        // Initial UI setup
        SetStatusText("Please login with your Google account");
        SetGoogleLoginButtonEnabled(true);
        
        InitializeGooglePlayGames();
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnAuthComplete -= OnAuthComplete;
            LobbyManager.Instance.OnError -= OnError;
        }

        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.RemoveListener(StartGoogleLogin);
        }
    }
    
    /// <summary>
    /// Google Play Games 초기화 (2024 최신 방식)
    /// </summary>
    private void InitializeGooglePlayGames()
    {
        try
        {
            // Google Play Games 활성화 (최신 API)
            PlayGamesPlatform.Activate();
            
            isInitialized = true;
            LogDebug("Google Play Games initialized successfully");
            
            // Auto authenticate attempt
            TryAutoAuthenticate();
        }
        catch (Exception e)
        {
            LogError($"Google Play Games initialization failed: {e.Message}");
            HandleOAuthResult(false, "", "Google Play Games initialization failed");
        }
    }
    
    /// <summary>
    /// 자동 인증 시도 (2024 최신 방식)
    /// </summary>
    private void TryAutoAuthenticate()
    {
        if (!isInitialized)
        {
            LogWarning("Google Play Games not initialized");
            return;
        }
        
        LogDebug("Attempting auto authentication...");
        
        // Latest API: Check auto login with Authenticate
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }
    
    /// <summary>
    /// 수동 Google 로그인 시작 (사용자가 버튼 클릭 시)
    /// </summary>
    public void StartGoogleLogin()
    {
        SetStatusText("Google login in progress...");
        SetGoogleLoginButtonEnabled(false);
        
        if (!isInitialized)
        {
            LogError("Google Play Games not initialized");
            HandleOAuthResult(false, "", "Google Play Games initialization required");
            return;
        }
        
        LogDebug("Starting manual Google login");
        
        // Latest API: Use ManuallyAuthenticate
        PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
    }
    
    /// <summary>
    /// 인증 결과 처리 (2024 최신 방식)
    /// </summary>
    private void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            string userName = Social.localUser.userName ?? "Unknown User";
            LogDebug($"Google login successful: {userName}");
            
            // Request Auth Code for server
            RequestServerSideAccess();
        }
        else
        {
            LogError($"Google login failed: {status}");
            HandleOAuthResult(false, "", $"Google login failed: {status}");
        }
    }
    
    /// <summary>
    /// 서버 사이드 액세스 코드 요청 (2024 최신 방식)
    /// </summary>
    private void RequestServerSideAccess()
    {
        LogDebug("Requesting server-side access code...");
        
        // Set forceRefreshToken = true to request new token
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, (string authCode) =>
        {
            if (!string.IsNullOrEmpty(authCode))
            {
                LogDebug("Auth Code obtained successfully");
                // 서버에 Auth Code 전송
                StartCoroutine(SendAuthCodeToServer(authCode));
            }
            else
            {
                LogError("Failed to obtain Auth Code");
                HandleOAuthResult(false, "", "Failed to obtain Auth Code");
            }
        });
    }
    
    /// <summary>
    /// 서버에 Authorization Code 전송하여 OAuth 로그인 완료
    /// </summary>
    private IEnumerator SendAuthCodeToServer(string authCode)
    {
        SetStatusText("Verifying server token...");
        LogDebug("Sending Auth Code to server for verification");
        
        // LobbyManager에서 서버 URL 가져오기
        string serverUrl = LobbyManager.Instance.GetCurrentServerUrl();
        
        var oAuthRequest = new OAuthTokenRequest { authCode = authCode };
        string jsonData = JsonUtility.ToJson(oAuthRequest);

        using (UnityEngine.Networking.UnityWebRequest request = 
               new UnityEngine.Networking.UnityWebRequest($"{serverUrl}/auth/google/verify-token", "POST"))
        {
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<OAuthLoginResponse>(request.downloadHandler.text);
                    if (response.success)
                    {
                        LogDebug($"Server OAuth verification successful: {response.nickname}");
                        HandleOAuthResult(true, response.sessionId, response.nickname);
                    }
                    else
                    {
                        LogError("Server OAuth verification failed");
                        HandleOAuthResult(false, "", "Server OAuth verification failed");
                    }
                }
                catch (Exception e)
                {
                    LogError($"Error parsing server response: {e.Message}");
                    HandleOAuthResult(false, "", "Error parsing server response");
                }
            }
            else
            {
                LogError($"Server request failed: {request.responseCode}");
                HandleOAuthResult(false, "", $"Server request failed: {request.responseCode}");
            }
        }
    }
    
    /// <summary>
    /// OAuth 결과 처리 - LobbyManager 직접 호출
    /// </summary>
    private void HandleOAuthResult(bool success, string sessionId, string message)
    {
        if (success)
        {
            SetStatusText("Connecting to server...");
            LogDebug($"OAuth login successful, connecting to WebSocket: {message} ({sessionId})");
            
            // LobbyManager에 직접 연결 요청
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.ConnectWithSession(sessionId, message); // message = nickname
            }
            else
            {
                LogError("LobbyManager not found!");
                SetStatusText("Cannot find LobbyManager");
                SetGoogleLoginButtonEnabled(true);
            }
        }
        else
        {
            LogError($"OAuth login failed: {message}");
            SetStatusText($"OAuth login failed: {message}");
            SetGoogleLoginButtonEnabled(true);
        }
    }
    
    /// <summary>
    /// 로그아웃 (2024 버전에서는 SignOut 메소드가 제거됨)
    /// </summary>
    public void Logout()
    {
        LogDebug("Direct logout is not supported in Google Play Games Plugin v11+");
        // SignOut method removed in latest version - no direct logout possible
        // App restart or account switching handled at system level
    }
    
    /// <summary>
    /// 현재 로그인 상태 확인
    /// </summary>
    public bool IsAuthenticated()
    {
        return isInitialized && Social.localUser.authenticated;
    }
    
    /// <summary>
    /// 현재 사용자 정보 반환
    /// </summary>
    public string GetUserDisplayName()
    {
        return IsAuthenticated() ? Social.localUser.userName ?? "Unknown User" : "Not logged in";
    }
    
    /// <summary>
    /// 현재 사용자 ID 반환
    /// </summary>
    public string GetUserId()
    {
        return IsAuthenticated() ? Social.localUser.id ?? "" : "";
    }
    
    /// <summary>
    /// 웹소켓 인증 완료 시 호출 (로비로 이동)
    /// </summary>
    private void OnAuthComplete()
    {
        SetStatusText("Login successful! Moving to lobby...");

        // Move to lobby scene after 1 second
        Invoke(nameof(GoToLobby), 1f);
    }

    /// <summary>
    /// Called when error occurs
    /// </summary>
    private void OnError(string errorMessage)
    {
        SetStatusText($"Error occurred: {errorMessage}");
        SetGoogleLoginButtonEnabled(true);
    }

    /// <summary>
    /// 로비 씬으로 이동
    /// </summary>
    private void GoToLobby()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.GoToLobby();
        }
    }

    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    private void SetStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }

        LogDebug($"Status: {text}");
    }

    /// <summary>
    /// Google 로그인 버튼 활성화/비활성화
    /// </summary>
    private void SetGoogleLoginButtonEnabled(bool enabled)
    {
        if (googleLoginButton != null)
        {
            googleLoginButton.interactable = enabled;
        }
    }
    
    #region Debug Logging
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GoogleOAuth] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning($"[GoogleOAuth] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[GoogleOAuth] {message}");
    }
    #endregion
}

// OAuth 관련 데이터 클래스들
[System.Serializable]
public class OAuthTokenRequest
{
    public string authCode;
}

[System.Serializable]
public class OAuthLoginResponse
{
    public bool success;
    public string sessionId;
    public string nickname;
    public bool needSetNickname;
}