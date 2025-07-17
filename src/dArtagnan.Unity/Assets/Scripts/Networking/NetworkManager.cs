using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Networking;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private Channel<IPacket> _channel;
    private TcpClient _client;
    private NetworkStream _stream;
    public static NetworkManager Instance { get; private set; }
    public NetworkManagerConfig config;
    public bool connected;
    public float pingAvg;
    public float pingDeviance;
    private float LastPingRequestTime;
    private List<float> pingSample = new();

    private void Awake()
    {
        Instance = this;
        _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();
    }

    // private async UniTaskVoid Start()
    // {
    //     await ConnectToServer();
    //     StartListeningLoop().Forget();
    //     SendJoinRequest();
    // }

    private void Update()
    {
        if (_channel.Reader.TryRead(out var packet))
        {
            HandlePacket(packet);
        }
    }

    public void Connect(string host, int port)
    {
        ConnectToServer(host, port)
            .ContinueWith(StartListeningLoop)
            .Forget();
    }

    private async UniTask ConnectToServer(string host, int port)
    {
        // var host = useCustomHost ? customHost : awsHost;
        Debug.Log($"Connecting to: {host}:{port}");
        _client = new TcpClient();
        _client.NoDelay = true;
        await _client.ConnectAsync(host, port).AsUniTask();
        // TCP NoDelay 설정 (Nagle's algorithm 비활성화)
        Debug.Log($"Connected to: {host}:{port}");
        _stream = _client.GetStream();
        connected = true;
        await NetworkUtils.SendPacketAsync(_stream, new PingPacket());
        await NetworkUtils.SendPacketAsync(_stream, new PlayerJoinRequest());
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

    public void SendPlayerMovementData(Vector3 position, Vector3 direction, bool running, float speed)
    {
        MovementData data = new()
        {
            Direction = DirectionHelperClient.DirectionToInt(direction),
            Position = VecConverter.ToSystemVec(position + pingAvg / 2 * direction),
            Speed = speed
        };
        Send(new PlayerMovementDataFromClient
        {
            Direction = DirectionHelperClient.DirectionToInt(direction),
            MovementData = data,
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
        Send(new StartGameFromClient());
    }
    
    public void SendAccuracyState(int accuracyState)
    {
        Send(new SetAccuracyState { AccuracyState = accuracyState });
    }

    public void SendRouletteDone()
    {
        Send(new RouletteDone());
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
            case PlayerMovementDataBroadcast d:
                var v = DirectionHelper.IntToDirection(d.MovementData.Direction);
                d.MovementData.Position += v * pingAvg / 2; 
                GameManager.Instance.OnPlayerMovementData(d);
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
            case NewHostBroadcast newHost:
                GameManager.Instance.OnNewHost(newHost);
                break;
            case PlayerIsTargetingBroadcast playerIsTargetingBroadcast:
                GameManager.Instance.OnPlayerIsTargeting(playerIsTargetingBroadcast);
                break;
            case WinnerBroadcast winner:
                GameManager.Instance.OnWinner(winner);
                break;
            case GameInWaitingFromServer gameWaiting:
                GameManager.Instance.OnGameWaiting(gameWaiting);
                break;
            case GameInPlayingFromServer gamePlaying:
                GameManager.Instance.OnGamePlaying(gamePlaying);
                break;
            case PlayerBalanceUpdateBroadcast playerBalanceUpdate:
                GameManager.Instance.OnPlayerBalanceUpdate(playerBalanceUpdate);
                break;
            case PlayerAccuracyStateBroadcast accuracyStateBroadcast:
                GameManager.Instance.OnPlayerAccuracyStateBroadcast(accuracyStateBroadcast);
                break;
            case YourAccuracyAndPool yourAccuracyAndPool:
                GameManager.Instance.OnYourAccuracyAndPool(yourAccuracyAndPool);
                break;
            case PongPacket _:
                var elapsed = Time.time - LastPingRequestTime;
                Send(new PingPacket());
                LastPingRequestTime = Time.time;
                if (pingSample.Count >= 100) // 표본 100개
                {
                    var avg = pingSample.Average();
                    var sd = Mathf.Sqrt((float)pingSample.Average(p => Math.Pow(p - avg, 2)));
                    if (Mathf.Abs(elapsed - avg) > sd) return;
                    GameManager.Instance.UpdatePing(elapsed);
                    pingSample.Add(elapsed);
                    pingSample.RemoveAt(0);
                    pingAvg = avg;
                    pingDeviance = sd;
                }
                else
                {
                    pingSample.Add(elapsed);
                }
                break;
            default:
                Debug.LogWarning($"Unhandled packet: {packet}");
                break;
        }
    }
}