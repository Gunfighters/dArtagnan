using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginButton : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("서버 선택")]
    [SerializeField] private Toggle serverToggle; // AWS/Localhost 토글
    [SerializeField] private TextMeshProUGUI serverStatusText; // 현재 서버 표시

    private void Start()
    {
        // 로그인 결과 이벤트 구독
        LobbyManager.Instance.OnLoginComplete += OnLoginComplete;
        LobbyManager.Instance.OnAuthComplete += OnAuthComplete;
        LobbyManager.Instance.OnError += OnError;
        LobbyManager.Instance.OnServerChanged += OnServerChanged;
        
        // 서버 토글 이벤트 연결
        if (serverToggle != null)
        {
            serverToggle.onValueChanged.AddListener(OnServerToggleChanged);
            serverToggle.isOn = LobbyManager.Instance.IsUsingAwsServer();
        }
        
        // 초기 상태 설정
        SetStatusText("Enter nickname and login");
        SetLoginButtonEnabled(true);
        UpdateServerStatusText();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLoginComplete -= OnLoginComplete;
            LobbyManager.Instance.OnAuthComplete -= OnAuthComplete;
            LobbyManager.Instance.OnError -= OnError;
            LobbyManager.Instance.OnServerChanged -= OnServerChanged;
        }
        
        serverToggle?.onValueChanged.RemoveAllListeners();
    }

    /// <summary>
    /// 로그인 버튼 클릭 시 호출되는 메소드
    /// </summary>
    public void OnLoginButtonClick()
    {
        string nickname = nicknameInputField.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            SetStatusText("Please enter a nickname");
            return;
        }

        // 로그인 시작
        SetStatusText("Logging in...");
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
            SetStatusText("Connecting to server...");
            // 웹소켓 연결은 LobbyManager에서 자동으로 처리됨
        }
        else
        {
            SetStatusText($"Login failed: {message}");
            SetLoginButtonEnabled(true);
        }
    }

    /// <summary>
    /// 웹소켓 인증 완료 시 호출
    /// </summary>
    private void OnAuthComplete()
    {
        SetStatusText("Login successful! Moving to lobby...");
        
        // 1초 후 로비 씬으로 이동
        Invoke(nameof(GoToLobby), 1f);
    }

    /// <summary>
    /// 에러 발생 시 호출
    /// </summary>
    private void OnError(string errorCode)
    {
        SetStatusText($"Error: {errorCode}");
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
    
    /// <summary>
    /// 서버 토글 변경 처리
    /// </summary>
    private void OnServerToggleChanged(bool useAws)
    {
        LobbyManager.Instance.SetServerType(useAws);
        SetStatusText($"Server changed to {(useAws ? "AWS" : "Localhost")}");
    }
    
    /// <summary>
    /// 서버 변경 이벤트 처리
    /// </summary>
    private void OnServerChanged(bool useAws)
    {
        UpdateServerStatusText();
    }
    
    /// <summary>
    /// 서버 상태 텍스트 업데이트
    /// </summary>
    private void UpdateServerStatusText()
    {
        if (serverStatusText != null)
        {
            string serverType = LobbyManager.Instance.IsUsingAwsServer() ? "AWS" : "Localhost";
            string serverUrl = LobbyManager.Instance.GetCurrentServerUrl();
            serverStatusText.text = $"Server: {serverType}\n{serverUrl}";
        }
    }
}