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
            LogDebug("Google Play Games 초기화 완료");
            
            // 자동 로그인 시도
            TryAutoAuthenticate();
        }
        catch (Exception e)
        {
            LogError($"Google Play Games 초기화 실패: {e.Message}");
            OnOAuthComplete?.Invoke(false, "Google Play Games 초기화 실패");
        }
    }
    
    /// <summary>
    /// 자동 인증 시도 (2024 최신 방식)
    /// </summary>
    private void TryAutoAuthenticate()
    {
        if (!isInitialized)
        {
            LogWarning("Google Play Games가 초기화되지 않음");
            return;
        }
        
        LogDebug("자동 인증 시도 중...");
        
        // 최신 API: Authenticate로 자동 로그인 확인
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }
    
    /// <summary>
    /// 수동 Google 로그인 시작 (사용자가 버튼 클릭 시)
    /// </summary>
    public void StartGoogleLogin()
    {
        if (!isInitialized)
        {
            LogError("Google Play Games가 초기화되지 않음");
            OnOAuthComplete?.Invoke(false, "Google Play Games 초기화 필요");
            return;
        }
        
        LogDebug("수동 Google 로그인 시작");
        
        // 최신 API: ManuallyAuthenticate 사용
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
            LogDebug($"Google 로그인 성공: {userName}");
            
            // 서버용 Auth Code 요청
            RequestServerSideAccess();
        }
        else
        {
            LogError($"Google 로그인 실패: {status}");
            OnOAuthComplete?.Invoke(false, $"Google 로그인 실패: {status}");
        }
    }
    
    /// <summary>
    /// 서버 사이드 액세스 코드 요청 (2024 최신 방식)
    /// </summary>
    private void RequestServerSideAccess()
    {
        LogDebug("서버 사이드 액세스 코드 요청 중...");
        
        // forceRefreshToken = true로 설정하여 새로운 토큰 요청
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, (string authCode) =>
        {
            if (!string.IsNullOrEmpty(authCode))
            {
                LogDebug("Auth Code 획득 성공");
                OnOAuthComplete?.Invoke(true, authCode);
            }
            else
            {
                LogError("Auth Code 획득 실패");
                OnOAuthComplete?.Invoke(false, "Auth Code 획득 실패");
            }
        });
    }
    
    /// <summary>
    /// 로그아웃 (2024 버전에서는 SignOut 메소드가 제거됨)
    /// </summary>
    public void Logout()
    {
        LogDebug("Google Play Games Plugin v11+ 에서는 직접적인 로그아웃이 지원되지 않습니다.");
        // 최신 버전에서는 SignOut이 제거되어 직접적인 로그아웃 불가능
        // 대신 앱 재시작이나 계정 전환은 시스템 레벨에서 처리됨
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