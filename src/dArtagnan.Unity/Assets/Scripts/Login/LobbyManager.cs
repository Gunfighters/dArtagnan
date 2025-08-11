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

    [Header("Server Settings")]
    public string lobbyUrl = "http://localhost:3000";
    public string awsLobbyUrl = "http://13.125.222.113:3000";
    
    private bool useAwsServer = false;

    private string sessionId;
    private string nickname;
    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationTokenSource;
    
    // Events
    public event Action<bool, string> OnLoginComplete; // success, message
    public event Action OnAuthComplete;
    public event Action<string> OnError;
    public event Action<LobbyProtocol.CreateRoomResult> OnCreateRoomResult;
    public event Action<LobbyProtocol.JoinRoomResult> OnJoinRoomResult;
    public event Action<bool> OnServerChanged; // AWS 서버 사용 여부

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
    /// 서버 선택 - localhost 또는 AWS
    /// </summary>
    public void SetServerType(bool useAws)
    {
        useAwsServer = useAws;
        OnServerChanged?.Invoke(useAws);
        Debug.Log($"[LobbyManager] Server changed to: {(useAws ? "AWS" : "Localhost")}");
    }
    
    /// <summary>
    /// 현재 선택된 서버 URL 반환
    /// </summary>
    public string GetCurrentServerUrl()
    {
        return useAwsServer ? awsLobbyUrl : lobbyUrl;
    }
    
    /// <summary>
    /// 현재 AWS 서버 사용 여부
    /// </summary>
    public bool IsUsingAwsServer()
    {
        return useAwsServer;
    }

    /// <summary>
    /// HTTP로 로그인 요청을 보내고 sessionId를 받음
    /// </summary>
    public void Login(string inputNickname)
    {
        if (string.IsNullOrEmpty(inputNickname))
        {
            OnLoginComplete?.Invoke(false, "Please enter a nickname.");
            return;
        }

        StartCoroutine(LoginCoroutine(inputNickname));
    }

    private IEnumerator LoginCoroutine(string inputNickname)
    {
        var loginRequest = new LoginRequest { nickname = inputNickname };
        string jsonData = JsonUtility.ToJson(loginRequest);
        
        Debug.Log($"Sending JSON: {jsonData}");
        
        using (UnityWebRequest request = new UnityWebRequest($"{GetCurrentServerUrl()}/login", "POST"))
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
                    
                    Debug.Log($"Login successful: {nickname} (Session: {sessionId})");
                    OnLoginComplete?.Invoke(true, "Login successful.");
                    
                    // 웹소켓 연결 시작
                    ConnectWebSocket();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing login response: {e.Message}");
                    OnLoginComplete?.Invoke(false, "An error occurred while processing the login response.");
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
                    OnLoginComplete?.Invoke(false, $"Login failed: {request.responseCode}");
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
            
            string wsUrl = GetCurrentServerUrl().Replace("http://", "ws://").Replace("https://", "wss://");
            await webSocket.ConnectAsync(new Uri(wsUrl), cancellationTokenSource.Token);
            
            Debug.Log("WebSocket connection successful.");
            
            // 인증 메시지 전송
            var authMessage = new AuthMessage { type = "auth", sessionId = sessionId };
            await SendWebSocketMessage(JsonUtility.ToJson(authMessage));
            
            // 메시지 수신 시작
            StartCoroutine(WebSocketReceiveLoop());
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket connection failed: {e.Message}");
            OnError?.Invoke("Failed to connect to the server.");
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
                Debug.LogError($"WebSocket receive error: {e.Message}");
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
                    Debug.Log("Authentication successful.");
                    OnAuthComplete?.Invoke();
                    break;
                    
                case "error":
                    var errorMessage = JsonUtility.FromJson<ErrorMessage>(message);
                    Debug.LogWarning($"Server error: {errorMessage.code}");
                    OnError?.Invoke(errorMessage.code);
                    break;
                    
                case "create_room_response":
                    var createResponse = JsonUtility.FromJson<CreateRoomResponseMessage>(message);
                    if (createResponse.ok)
                    {
                        var result = new LobbyProtocol.CreateRoomResult 
                        { 
                            success = true, 
                            roomId = createResponse.roomId, 
                            ip = createResponse.ip, 
                            port = createResponse.port 
                        };
                        
                        OnCreateRoomResult?.Invoke(result);
                        
                        Debug.Log($"[LobbyManager] Invoking ConnectToGameServer with {createResponse.ip}:{createResponse.port}");
                        // NetworkManager에게 게임 서버 연결 요청
                        ConnectToGameServer(createResponse.ip, createResponse.port);
                    }
                    break;
                    
                case "join_room_response":
                    var joinResponse = JsonUtility.FromJson<JoinRoomResponseMessage>(message);
                    if (joinResponse.ok)
                    {
                        var result = new LobbyProtocol.JoinRoomResult 
                        { 
                            success = true, 
                            roomId = joinResponse.roomId ?? "", 
                            ip = joinResponse.ip, 
                            port = joinResponse.port 
                        };
                        
                        OnJoinRoomResult?.Invoke(result);
                        
                        Debug.Log($"[LobbyManager] Invoking ConnectToGameServer with {joinResponse.ip}:{joinResponse.port}");
                        // NetworkManager에게 게임 서버 연결 요청
                        ConnectToGameServer(joinResponse.ip, joinResponse.port);
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing WebSocket message: {e.Message}");
            Debug.LogError($"Message content: {message}");
        }
    }

    /// <summary>
    /// 방 생성 요청
    /// </summary>
    public async void CreateRoom(string roomId = null)
    {
        if (webSocket?.State != WebSocketState.Open)
        {
            OnError?.Invoke("Server connection lost.");
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
            OnError?.Invoke("Server connection lost.");
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
            Debug.LogWarning($"Error while closing WebSocket: {e.Message}");
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
    /// NetworkManager에게 게임 서버 연결을 요청
    /// 기존 LocalEventChannel 시스템을 활용
    /// </summary>
    private void ConnectToGameServer(string ip, int port)
    {
        Debug.Log($"[LobbyManager] ConnectToGameServer called: {ip}:{port}");
        Debug.Log($"[LobbyManager] About to invoke LocalEventChannel.InvokeOnEndpointSelected");
        
        // 기존 NetworkManager 시스템과 연동
        LocalEventChannel.InvokeOnEndpointSelected(ip, port);
        Debug.Log($"[LobbyManager] LocalEventChannel.InvokeOnEndpointSelected completed");
    }
    
    /// <summary>
    /// 게임 씬으로 이동 (TCP 연결 정보와 함께) - 호환성용 유지
    /// </summary>
    public void GoToGame(string gameServerIp, int gameServerPort)
    {
        // GameManager에 연결 정보 전달 (필요시)
        PlayerPrefs.SetString("GameServerIP", gameServerIp);
        PlayerPrefs.SetInt("GameServerPort", gameServerPort);
        
        SceneManager.LoadScene("Game"); // 게임 씬 이름에 맞게 수정
    }
}