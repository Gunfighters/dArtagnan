using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Networking;
using UnityEngine;

public class NetworkManager : MonoBehaviour, IChannelListener
{
    [Header("Config")] [SerializeField] private NetworkManagerConfig config;
    [SerializeField] private int targetFrameRate;
    private readonly Channel<IPacket> _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();
    private CancellationTokenSource _cancellationTokenSource;

    private TcpClient _client;
    private NetworkStream _stream;
    private bool _initialized;

    private void Awake() => DontDestroyOnLoad(gameObject);

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void Update()
    {
        while (_channel.Reader.TryRead(out var packet))
        {
            PacketChannel.Raise(packet);
        }
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _stream?.Close();
        _client?.Close();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        PacketChannel.On<MovementDataFromClient>(Send);
        PacketChannel.On<ShootingFromClient>(Send);
        PacketChannel.On<PlayerIsTargetingFromClient>(Send);
        PacketChannel.On<StartGameFromClient>(Send);
        PacketChannel.On<UpdateAccuracyStateFromClient>(Send);
        PacketChannel.On<RouletteDoneFromClient>(Send);
        PacketChannel.On<AugmentDoneFromClient>(Send);
        PacketChannel.On<UpdateItemCreatingStateFromClient>(Send);
        PacketChannel.On<UseItemFromClient>(Send);
        PacketChannel.On<ChatFromClient>(Send);
        LocalEventChannel.OnEndpointSelected += Connect;
    }

    private void Connect(string host, int port)
    {
        Debug.Log($"[NetworkManager] Connect called with {host}:{port}");
        _cancellationTokenSource = new CancellationTokenSource();
        ConnectToServer(host, port)
            .ContinueWith(StartListeningLoop)
            .Forget();
    }

    private async UniTask ConnectToServer(string host, int port)
    {
        Debug.Log($"[NetworkManager] Starting TCP connection to: {host}:{port}");
        _client = new TcpClient();
        _client.NoDelay = true;
        
        // 연결 시도 전 상태 로그
        Debug.Log($"[NetworkManager] TcpClient created, attempting connection...");
        
        try
        {
            var startTime = System.DateTime.Now;
            await _client.ConnectAsync(host, port).AsUniTask();
            var endTime = System.DateTime.Now;
            Debug.Log($"[NetworkManager] TCP connection successful! Time taken: {(endTime - startTime).TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] TCP connection failed: {e.GetType().Name}: {e.Message}");
            Debug.LogError($"[NetworkManager] Stack trace: {e.StackTrace}");
            LocalEventChannel.InvokeOnConnectionFailure();
            return;
        }

        _stream = _client.GetStream();
        Debug.Log($"[NetworkManager] Network stream obtained successfully");
        LocalEventChannel.InvokeOnConnectionSuccess();
        Debug.Log($"[NetworkManager] Connection success event invoked");
    }

    private void Send<T>(T payload) where T : IPacket
    {
        try
        {
            NetworkUtils.SendPacketSync(_stream, payload);
        }
        catch
        {
            LocalEventChannel.InvokeOnConnectionFailure();
        }
    }

    private async UniTask StartListeningLoop()
    {
        await UniTask.SwitchToThreadPool();
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var packet = NetworkUtils.ReceivePacketSync(_stream);
                _channel.Writer.TryWrite(packet);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch
        {
            await UniTask.SwitchToMainThread();
            LocalEventChannel.InvokeOnConnectionFailure();
        }
    }
}