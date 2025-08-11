using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginButton : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        // 로그인 결과 이벤트 구독
        LobbyManager.Instance.OnLoginComplete += OnLoginComplete;
        LobbyManager.Instance.OnAuthComplete += OnAuthComplete;
        LobbyManager.Instance.OnError += OnError;
        
        // 초기 상태 설정
        SetStatusText("닉네임을 입력하고 로그인하세요");
        SetLoginButtonEnabled(true);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLoginComplete -= OnLoginComplete;
            LobbyManager.Instance.OnAuthComplete -= OnAuthComplete;
            LobbyManager.Instance.OnError -= OnError;
        }
    }

    /// <summary>
    /// 로그인 버튼 클릭 시 호출되는 메소드
    /// </summary>
    public void OnLoginButtonClick()
    {
        string nickname = nicknameInputField.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            SetStatusText("닉네임을 입력해주세요");
            return;
        }

        // 로그인 시작
        SetStatusText("로그인 중...");
        SetLoginButtonEnabled(false);
        
        LobbyManager.Instance.Login(nickname);
    }

    /// <summary>
    /// HTTP 로그인 완료 시 호출
    /// </summary>
    private void OnLoginComplete(bool success, string message)
    {
        if (success)
        {
            SetStatusText("서버 연결 중...");
            // 웹소켓 연결은 LobbyManager에서 자동으로 처리됨
        }
        else
        {
            SetStatusText($"로그인 실패: {message}");
            SetLoginButtonEnabled(true);
        }
    }

    /// <summary>
    /// 웹소켓 인증 완료 시 호출
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
    private void OnError(string errorCode)
    {
        SetStatusText($"오류: {errorCode}");
        SetLoginButtonEnabled(true);
    }

    private void GoToLobby()
    {
        LobbyManager.Instance.GoToLobby();
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
        
        Debug.Log($"[LoginButton] {text}");
    }

    /// <summary>
    /// 로그인 버튼 활성화/비활성화
    /// </summary>
    private void SetLoginButtonEnabled(bool enabled)
    {
        if (loginButton != null)
        {
            loginButton.interactable = enabled;
        }
    }
}