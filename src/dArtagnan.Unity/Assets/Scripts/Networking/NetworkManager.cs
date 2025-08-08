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
        PacketChannel.On<MovementDataFromClient>(Send);
        PacketChannel.On<ShootingFromClient>(Send);
        PacketChannel.On<PlayerIsTargetingFromClient>(Send);
        PacketChannel.On<StartGameFromClient>(Send);
        PacketChannel.On<UpdateAccuracyStateFromClient>(Send);
        PacketChannel.On<RouletteDoneFromClient>(Send);
        PacketChannel.On<AugmentDoneFromClient>(Send);
        PacketChannel.On<UpdateItemCreatingStateFromClient>(Send);
        PacketChannel.On<UseItemFromClient>(Send);
        LocalEventChannel.OnEndpointSelected += Connect;
    }

    private void Connect(string host, int port)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        ConnectToServer(host, port)
            .ContinueWith(StartListeningLoop)
            .Forget();
    }

    private async UniTask ConnectToServer(string host, int port)
    {
        Debug.Log($"Connecting to: {host}:{port}");
        _client = new TcpClient();
        _client.NoDelay = true;
        try
        {
            await _client.ConnectAsync(host, port).AsUniTask();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            LocalEventChannel.InvokeOnConnectionFailure();
            return;
        }

        _stream = _client.GetStream();
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