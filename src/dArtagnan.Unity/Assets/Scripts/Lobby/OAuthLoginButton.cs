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

        // 초기 상태 설정
        SetStatusText("Google 계정으로 로그인해주세요.");
        SetGoogleLoginButtonEnabled(true);

        // AWS 서버 강제 설정 (OAuth 씬은 항상 배포용)
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.SetServerType(true); // AWS 서버 사용
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
        SetStatusText("Google 로그인 중...");
        SetGoogleLoginButtonEnabled(false);

        if (googleOAuthManager != null)
        {
            googleOAuthManager.StartGoogleLogin();
        }
        else
        {
            SetStatusText("Google OAuth Manager가 설정되지 않았습니다.");
            SetGoogleLoginButtonEnabled(true);
            Debug.LogError("[OAuthLoginButton] GoogleOAuthManager가 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// Google OAuth 로그인 완료 시 호출
    /// </summary>
    private void OnGoogleOAuthComplete(bool success, string result)
    {
        if (success)
        {
            SetStatusText("서버 토큰 검증 중...");
            // ID Token을 LobbyManager로 전달
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.LoginWithOAuth(result);
            }
            else
            {
                SetStatusText("LobbyManager를 찾을 수 없습니다.");
                SetGoogleLoginButtonEnabled(true);
            }
        }
        else
        {
            SetStatusText($"Google 로그인 실패: {result}");
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
            SetStatusText("서버에 연결하는 중...");
            // 웹소켓 연결은 LobbyManager에서 자동으로 처리됨
        }
        else
        {
            SetStatusText($"서버 로그인 실패: {message}");
            SetGoogleLoginButtonEnabled(true);
        }
    }

    /// <summary>
    /// 웹소켓 인증 완료 시 호출 (로비로 이동)
    /// </summary>
    private void OnAuthComplete()
    {
        SetStatusText("로그인 성공! 로비로 이동합니다...");

        // 1초 후 로비 씬으로 이동
        Invoke(nameof(GoToLobby), 1f);
    }

    /// <summary>
    /// 에러 발생 시 호출
    /// </summary>
    private void OnError(string errorMessage)
    {
        SetStatusText($"오류 발생: {errorMessage}");
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