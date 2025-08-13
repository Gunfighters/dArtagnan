using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 씬을 위한 간단한 UI 컨트롤러
/// 방 생성, 방 참가 버튼 및 상태 표시를 처리합니다
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("UI 요소")] [SerializeField] private Button createRoomButton;

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
        // SetStatusText("Create or join a room");
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

        SetStatusText("방 만드는 중...");
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
            SetStatusText("아무 방이나 들어가는 중...");
        }
        else
        {
            SetStatusText($"{roomId}번 방에 들어가는 중...");
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
            SetStatusText($"방 생성 완료! 게임 서버 연결중... ({result.ip}:{result.port})");
            // 게임 서버 연결은 LobbyManager가 자동으로 처리
        }
        else
        {
            SetStatusText($"방 생성 실패: {result.errorCode}");
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
            SetStatusText($"방 접속 완료! 게임서버 연결하는 중... ({result.ip}:{result.port})");
            // 게임 서버 연결은 LobbyManager가 자동으로 처리
        }
        else
        {
            SetStatusText($"방 접속 실패: {result.errorCode}");
            SetButtonsEnabled(true);
        }
    }

    /// <summary>
    /// 오류 발생 처리
    /// </summary>
    private void OnError(string errorCode)
    {
        SetStatusText($"오류: {errorCode}");
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