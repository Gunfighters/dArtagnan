using UnityEngine;
using UnityEngine.UI;
using TMPro;
using dArtagnan.Shared;

/// <summary>
/// 로비 씬을 위한 간단한 UI 컨트롤러
/// 방 생성, 방 참가 버튼 및 상태 표시를 처리합니다
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TMP_InputField roomIdInputField; // (선택 사항) 방 ID 입력 필드

    private void Start()
    {
        // 로비 매니저 이벤트 구독
        LobbyManager.Instance.OnCreateRoomResult += OnCreateRoomResult;
        LobbyManager.Instance.OnJoinRoomResult += OnJoinRoomResult;
        LobbyManager.Instance.OnError += OnError;
        
        // 버튼 이벤트 연결
        createRoomButton.onClick.AddListener(OnCreateRoomClick);
        joinRoomButton.onClick.AddListener(OnJoinRoomClick);
        
        // 초기 상태
        SetStatusText("Create or join a room");
        SetButtonsEnabled(true);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnCreateRoomResult -= OnCreateRoomResult;
            LobbyManager.Instance.OnJoinRoomResult -= OnJoinRoomResult;
            LobbyManager.Instance.OnError -= OnError;
        }
        
        // 버튼 이벤트 리스너 제거
        createRoomButton?.onClick.RemoveAllListeners();
        joinRoomButton?.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 방 생성 버튼 클릭 처리
    /// </summary>
    private void OnCreateRoomClick()
    {
        string roomId = string.IsNullOrEmpty(roomIdInputField.text.Trim()) ? null : roomIdInputField.text.Trim();
        
        SetStatusText("Creating room...");
        SetButtonsEnabled(false);
        
        LobbyManager.Instance.CreateRoom(roomId);
    }

    /// <summary>
    /// 방 참가 버튼 클릭 처리
    /// </summary>
    private void OnJoinRoomClick()
    {
        string roomId = string.IsNullOrEmpty(roomIdInputField.text.Trim()) ? null : roomIdInputField.text.Trim();
        
        if (roomId == null)
        {
            SetStatusText("Attempting random match...");
        }
        else
        {
            SetStatusText($"Joining room {roomId}...");
        }
        
        SetButtonsEnabled(false);
        
        LobbyManager.Instance.JoinRoom(roomId);
    }

    /// <summary>
    /// 방 생성 결과 처리
    /// </summary>
    private void OnCreateRoomResult(LobbyProtocol.CreateRoomResult result)
    {
        if (result.success)
        {
            SetStatusText($"Room created! Connecting to game server... ({result.ip}:{result.port})");
            // 게임 서버 연결은 LobbyManager가 자동으로 처리
        }
        else
        {
            SetStatusText($"Room creation failed: {result.errorCode}");
            SetButtonsEnabled(true);
        }
    }

    /// <summary>
    /// 방 참가 결과 처리
    /// </summary>
    private void OnJoinRoomResult(LobbyProtocol.JoinRoomResult result)
    {
        if (result.success)
        {
            SetStatusText($"Room joined! Connecting to game server... ({result.ip}:{result.port})");
            // 게임 서버 연결은 LobbyManager가 자동으로 처리
        }
        else
        {
            SetStatusText($"Room join failed: {result.errorCode}");
            SetButtonsEnabled(true);
        }
    }

    /// <summary>
    /// 오류 발생 처리
    /// </summary>
    private void OnError(string errorCode)
    {
        SetStatusText($"Error occurred: {errorCode}");
        SetButtonsEnabled(true);
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
        
        Debug.Log($"[LobbyUI] {text}");
    }

    /// <summary>
    /// 버튼 활성화/비활성화
    /// </summary>
    private void SetButtonsEnabled(bool enabled)
    {
        if (createRoomButton != null)
        {
            createRoomButton.interactable = enabled;
        }
        
        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = enabled;
        }
    }
}
