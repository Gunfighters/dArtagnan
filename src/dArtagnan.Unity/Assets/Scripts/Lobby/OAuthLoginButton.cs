using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OAuth 전용 Login 씬을 위한 UI 컨트롤러
/// Google 로그인 버튼만 관리하는 단순한 컴포넌트
/// </summary>
public class OAuthLoginButton : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("OAuth 설정")]
    [SerializeField] private GoogleOAuthManager googleOAuthManager;

    private void Start()
    {
        // 이벤트 구독
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnOAuthLoginComplete += OnOAuthLoginComplete;
            LobbyManager.Instance.OnAuthComplete += OnAuthComplete;
            LobbyManager.Instance.OnError += OnError;
        }

        if (googleOAuthManager != null)
        {
            googleOAuthManager.OnOAuthComplete += OnGoogleOAuthComplete;
        }

        // 버튼 이벤트 연결
        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.AddListener(OnGoogleLoginButtonClick);
        }

        // Initial status setup
        SetStatusText("Please login with your Google account");
        SetGoogleLoginButtonEnabled(true);

        // Force AWS server setting (OAuth scene is always for deployment)
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.SetServerType(true); // Use AWS server
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnOAuthLoginComplete -= OnOAuthLoginComplete;
            LobbyManager.Instance.OnAuthComplete -= OnAuthComplete;
            LobbyManager.Instance.OnError -= OnError;
        }

        if (googleOAuthManager != null)
        {
            googleOAuthManager.OnOAuthComplete -= OnGoogleOAuthComplete;
        }

        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.RemoveListener(OnGoogleLoginButtonClick);
        }
    }

    /// <summary>
    /// Google 로그인 버튼 클릭 시 호출
    /// </summary>
    private void OnGoogleLoginButtonClick()
    {
        SetStatusText("Google login in progress...");
        SetGoogleLoginButtonEnabled(false);

        if (googleOAuthManager != null)
        {
            googleOAuthManager.StartGoogleLogin();
        }
        else
        {
            SetStatusText("Google OAuth Manager is not configured");
            SetGoogleLoginButtonEnabled(true);
            Debug.LogError("[OAuthLoginButton] GoogleOAuthManager is not assigned!");
        }
    }

    /// <summary>
    /// Google OAuth 로그인 완료 시 호출
    /// </summary>
    private void OnGoogleOAuthComplete(bool success, string result)
    {
        if (success)
        {
            SetStatusText("Verifying server token...");
            // Send ID Token to LobbyManager
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.LoginWithOAuth(result);
            }
            else
            {
                SetStatusText("Cannot find LobbyManager");
                SetGoogleLoginButtonEnabled(true);
            }
        }
        else
        {
            SetStatusText($"Google login failed: {result}");
            SetGoogleLoginButtonEnabled(true);
        }
    }

    /// <summary>
    /// 서버 OAuth 로그인 완료 시 호출
    /// </summary>
    private void OnOAuthLoginComplete(bool success, string message)
    {
        if (success)
        {
            SetStatusText("Connecting to server...");
            // WebSocket connection handled automatically by LobbyManager
        }
        else
        {
            SetStatusText($"Server login failed: {message}");
            SetGoogleLoginButtonEnabled(true);
        }
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

        Debug.Log($"[OAuthLoginButton] {text}");
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
}