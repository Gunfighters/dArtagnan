using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;

/// <summary>
/// Google Play Games를 이용한 OAuth 로그인을 관리하는 컴포넌트
/// OAuth 전용 Login 씬에서 사용 - 2024 최신 API 사용
/// </summary>
public class GoogleOAuthManager : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLog = true;
    
    // Events
    public event Action<bool, string> OnOAuthComplete; // success, message or authCode
    
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeGooglePlayGames();
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
            OnOAuthComplete?.Invoke(false, "Google Play Games initialization failed");
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
        if (!isInitialized)
        {
            LogError("Google Play Games not initialized");
            OnOAuthComplete?.Invoke(false, "Google Play Games initialization required");
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
            OnOAuthComplete?.Invoke(false, $"Google login failed: {status}");
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
                OnOAuthComplete?.Invoke(true, authCode);
            }
            else
            {
                LogError("Failed to obtain Auth Code");
                OnOAuthComplete?.Invoke(false, "Failed to obtain Auth Code");
            }
        });
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