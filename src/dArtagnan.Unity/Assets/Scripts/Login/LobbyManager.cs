using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using dArtagnan.Shared;

/// <summary>
/// 로비 서버와의 HTTP/WebSocket 통신을 관리하는 싱글톤 매니저
/// 씬 전환 시에도 유지되어 웹소켓 연결을 계속 관리함
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("서버 설정")]
    public string lobbyUrl = "http://localhost:3000";

    private string sessionId;
    private string nickname;
    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationTokenSource;
    
    // 이벤트
    public event Action<bool, string> OnLoginComplete; // 성공 여부, 메시지
    public event Action OnAuthComplete;
    public event Action<string> OnError;
    public event Action<LobbyProtocol.CreateRoomResult> OnCreateRoomResult;
    public event Action<LobbyProtocol.JoinRoomResult> OnJoinRoomResult;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        DisconnectWebSocket();
    }

    /// <summary>
    /// HTTP로 로그인 요청을 보내고 sessionId를 받음
    /// </summary>
    public void Login(string inputNickname)
    {
        if (string.IsNullOrEmpty(inputNickname))
        {
            OnLoginComplete?.Invoke(false, "닉네임을 입력해주세요");
            return;
        }

        StartCoroutine(LoginCoroutine(inputNickname));
    }

    private IEnumerator LoginCoroutine(string inputNickname)
    {
        var loginRequest = new LoginRequest { nickname = inputNickname };
        string jsonData = JsonUtility.ToJson(loginRequest);
        
        Debug.Log($"전송할 JSON: {jsonData}");
        
        using (UnityWebRequest request = new UnityWebRequest($"{lobbyUrl}/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                    sessionId = response.sessionId;
                    nickname = response.nickname;
                    
                    Debug.Log($"로그인 성공: {nickname} (세션: {sessionId})");
                    OnLoginComplete?.Invoke(true, "로그인 성공");
                    
                    // 웹소켓 연결 시작
                    ConnectWebSocket();
                }
                catch (Exception e)
                {
                    Debug.LogError($"로그인 응답 파싱 오류: {e.Message}");
                    OnLoginComplete?.Invoke(false, "로그인 응답 처리 중 오류가 발생했습니다");
                }
            }
            else
            {
                try
                {
                    var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                    OnLoginComplete?.Invoke(false, errorResponse.code);
                }
                catch
                {
                    OnLoginComplete?.Invoke(false, $"로그인 실패: {request.responseCode}");
                }
            }
        }
    }

    /// <summary>
    /// 웹소켓 연결 및 인증
    /// </summary>
    private async void ConnectWebSocket()
    {
        try
        {
            DisconnectWebSocket(); // 기존 연결 정리
            
            webSocket = new ClientWebSocket();
            cancellationTokenSource = new CancellationTokenSource();
            
            string wsUrl = lobbyUrl.Replace("http://", "ws://").Replace("https://", "wss://");
            await webSocket.ConnectAsync(new Uri(wsUrl), cancellationTokenSource.Token);
            
            Debug.Log("웹소켓 연결 성공");
            
            // 인증 메시지 전송
            var authMessage = new AuthMessage { type = "auth", sessionId = sessionId };
            await SendWebSocketMessage(JsonUtility.ToJson(authMessage));
            
            // 메시지 수신 시작
            StartCoroutine(WebSocketReceiveLoop());
        }
        catch (Exception e)
        {
            Debug.LogError($"웹소켓 연결 실패: {e.Message}");
            OnError?.Invoke("서버 연결에 실패했습니다");
        }
    }

    private async System.Threading.Tasks.Task SendWebSocketMessage(string message)
    {
        if (webSocket?.State == WebSocketState.Open)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
        }
    }

    private IEnumerator WebSocketReceiveLoop()
    {
        byte[] buffer = new byte[4096];
        
        while (webSocket?.State == WebSocketState.Open && !cancellationTokenSource.Token.IsCancellationRequested)
        {
            var receiveTask = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
            
            // Unity 메인 스레드에서 대기
            while (!receiveTask.IsCompleted)
            {
                yield return null;
            }
            
            try
            {
                var result = receiveTask.Result;
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleWebSocketMessage(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"웹소켓 수신 오류: {e.Message}");
                yield break;
            }
        }
    }

    private void HandleWebSocketMessage(string message)
    {
        try
        {
            // Unity JsonUtility로 메시지 타입 파싱
            var typeMessage = JsonUtility.FromJson<MessageType>(message);
            
            switch (typeMessage.type)
            {
                case "auth_success":
                    Debug.Log("인증 성공");
                    OnAuthComplete?.Invoke();
                    break;
                    
                case "error":
                    var errorMessage = JsonUtility.FromJson<ErrorMessage>(message);
                    Debug.LogWarning($"서버 오류: {errorMessage.code}");
                    OnError?.Invoke(errorMessage.code);
                    break;
                    
                case "create_room_response":
                    var createResponse = JsonUtility.FromJson<CreateRoomResponseMessage>(message);
                    if (createResponse.ok)
                    {
                        OnCreateRoomResult?.Invoke(new LobbyProtocol.CreateRoomResult 
                        { 
                            success = true, 
                            roomId = createResponse.roomId, 
                            ip = createResponse.ip, 
                            port = createResponse.port 
                        });
                    }
                    break;
                    
                case "join_room_response":
                    var joinResponse = JsonUtility.FromJson<JoinRoomResponseMessage>(message);
                    if (joinResponse.ok)
                    {
                        OnJoinRoomResult?.Invoke(new LobbyProtocol.JoinRoomResult 
                        { 
                            success = true, 
                            roomId = joinResponse.roomId ?? "", 
                            ip = joinResponse.ip, 
                            port = joinResponse.port 
                        });
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"웹소켓 메시지 처리 오류: {e.Message}");
            Debug.LogError($"메시지 내용: {message}");
        }
    }

    /// <summary>
    /// 방 생성 요청
    /// </summary>
    public async void CreateRoom(string roomId = null)
    {
        if (webSocket?.State != WebSocketState.Open)
        {
            OnError?.Invoke("서버 연결이 끊어졌습니다");
            return;
        }

        var message = new CreateRoomMessage { type = "create_room", roomId = roomId };
        string messageJson = JsonUtility.ToJson(message);
            
        await SendWebSocketMessage(messageJson);
    }

    /// <summary>
    /// 방 참가 요청
    /// </summary>
    public async void JoinRoom(string roomId = null)
    {
        if (webSocket?.State != WebSocketState.Open)
        {
            OnError?.Invoke("서버 연결이 끊어졌습니다");
            return;
        }

        var message = new JoinRoomMessage { type = "join_room", roomId = roomId };
        string messageJson = JsonUtility.ToJson(message);
            
        await SendWebSocketMessage(messageJson);
    }

    /// <summary>
    /// 웹소켓 연결 해제
    /// </summary>
    public void DisconnectWebSocket()
    {
        try
        {
            cancellationTokenSource?.Cancel();
            webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"웹소켓 종료 중 오류: {e.Message}");
        }
        finally
        {
            webSocket?.Dispose();
            cancellationTokenSource?.Dispose();
            webSocket = null;
            cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 로비 씬으로 이동
    /// </summary>
    public void GoToLobby()
    {
        SceneManager.LoadScene("Lobby");
    }

    /// <summary>
    /// 게임 씬으로 이동 (TCP 연결 정보와 함께)
    /// </summary>
    public void GoToGame(string gameServerIp, int gameServerPort)
    {
        // GameManager에 연결 정보 전달 (필요시)
        PlayerPrefs.SetString("GameServerIP", gameServerIp);
        PlayerPrefs.SetInt("GameServerPort", gameServerPort);
        
        SceneManager.LoadScene("Game"); // 게임 씬 이름에 맞게 수정
    }
}

// 이제 모든 프로토콜 클래스는 dArtagnan.Shared.LobbyProtocol에서 가져와서 사용합니다.