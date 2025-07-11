using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private Channel<IPacket> _channel;
    private TcpClient _client;
    private NetworkStream _stream;
    public static NetworkManager Instance { get; private set; }
    public string awsHost;
    public string customHost;
    public bool useCustomHost;
    public int port;

    private async UniTaskVoid Awake()
    {
        Instance = this;
        _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();
    }

    private async UniTaskVoid Start()
    {
        await ConnectToServer();
        StartListeningLoop().Forget();
        SendJoinRequest();
    }

    private void Update()
    {
        if (_channel.Reader.TryRead(out var packet))
        {
            HandlePacket(packet);
        }
    }

    private async UniTask ConnectToServer()
    {
        var host = useCustomHost ? customHost : awsHost;
        Debug.Log($"Connecting to: {host}:{port}");
        _client = new TcpClient();
        await _client.ConnectAsync(host, port).AsUniTask();

        // TCP NoDelay 설정 (Nagle's algorithm 비활성화)
        _client.NoDelay = true;
        Debug.Log($"Connected to: {host}:{port}");
        _stream = _client.GetStream();
    }

    private void Send(IPacket payload)
    {
        NetworkUtils.SendPacketSync(_stream, payload);
    }

    private async UniTask StartListeningLoop()
    {
        while (true)
        {
            var received = await NetworkUtils.ReceivePacketAsync(_stream);
            _channel.Writer.TryWrite(received);
        }
    }

    public void SendJoinRequest()
    {
        Send(new PlayerJoinRequest());
    }

    public void SendPlayerMovementData(Vector3 position, Vector3 direction, bool running)
    {
        Send(new PlayerMovementDataFromClient
        {
            Direction = DirectionHelperClient.DirectionToInt(direction),
            Position = new (position.x, position.y),
            Running = running
        });
    }

    public void SendPlayerShooting(int target)
    {
        Send(new PlayerShootingFromClient { TargetId = target });
    }

    public void SendPlayerNewTarget(int target)
    {
        Send(new PlayerIsTargetingFromClient { TargetId = target });
    }

    public void SendStartGame()
    {
        Send(new StartGame());
    }

    private void HandlePacket(IPacket packet)
    {
        switch (packet)
        {
            case YouAre are:
                GameManager.Instance.OnYouAre(are);
                break;
            case PlayerJoinBroadcast joinBroadcast:
                GameManager.Instance.OnPlayerJoinBroadcast(joinBroadcast);
                break;
            case PlayerMovementDataBroadcast playerMovementDataBroadcast:
                GameManager.Instance.OnPlayerMovementData(playerMovementDataBroadcast);
                break;
            case PlayerShootingBroadcast shootingBroadcast:
                GameManager.Instance.OnPlayerShootingBroadcast(shootingBroadcast);
                break;
            case UpdatePlayerAlive updatePlayerAlive:
                GameManager.Instance.OnUpdatePlayerAlive(updatePlayerAlive);
                break;
            case PlayerLeaveBroadcast playerLeaveBroadcast:
                GameManager.Instance.OnPlayerLeaveBroadcast(playerLeaveBroadcast);
                break;
            case NewHost newHost:
                GameManager.Instance.OnNewHost(newHost);
                break;
            case PlayerIsTargetingBroadcast playerIsTargetingBroadcast:
                GameManager.Instance.OnPlayerIsTargeting(playerIsTargetingBroadcast);
                break;
            case Winner winner:
                GameManager.Instance.OnWinner(winner);
                break;
            case GameWaiting gameWaiting:
                GameManager.Instance.OnGameWaiting(gameWaiting);
                break;
            case GamePlaying gamePlaying:
                GameManager.Instance.OnGamePlaying(gamePlaying);
                break;
            case PlayerBalanceUpdate playerBalanceUpdate:
                GameManager.Instance.OnPlayerBalanceUpdate(playerBalanceUpdate);
                break;
            default:
                Debug.LogWarning($"Unhandled packet: {packet}");
                break;
        }
    }
}