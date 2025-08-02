using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using Networking;
using UnityEngine;

public class NetworkManager : MonoBehaviour, IChannelListener
{
    [Header("Config")] [SerializeField] private NetworkManagerConfig config;

    private TcpClient _client;
    private NetworkStream _stream;
    private readonly Channel<IPacket> _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();

    public void Initialize()
    {
        PacketChannel.On<PlayerMovementDataFromClient>(Send);
        PacketChannel.On<PlayerShootingFromClient>(Send);
        PacketChannel.On<PlayerIsTargetingFromClient>(Send);
        PacketChannel.On<StartGameFromClient>(Send);
        PacketChannel.On<SetAccuracyState>(Send);
        PacketChannel.On<RouletteDone>(Send);
        PacketChannel.On<AugmentDoneFromClient>(Send);
        PacketChannel.On<ItemCreatingStateFromClient>(Send);
        PacketChannel.On<UseItemFromClient>(Send);
        LocalEventChannel.OnEndpointSelected += Connect;
    }

    private void Update()
    {
        if (_channel.Reader.TryRead(out var packet))
        {
            PacketChannel.Raise(packet);
        }
    }

    private void Connect(string host, int port)
    {
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
        await NetworkUtils.SendPacketAsync(_stream, new PingPacket());
        await NetworkUtils.SendPacketAsync(_stream, new PlayerJoinRequest());
    }

    private void Send<T>(T payload) where T : IPacket
    {
        try
        {
            NetworkUtils.SendPacketSync(_stream, payload);
        }
        catch (Exception e)
        {
            LocalEventChannel.InvokeOnConnectionFailure();
        }
    }

    private async UniTask StartListeningLoop()
    {
        while (true)
        {
            IPacket packet;
            try
            {
                packet = await NetworkUtils.ReceivePacketAsync(_stream);
            }
            catch (Exception e)
            {
                LocalEventChannel.InvokeOnConnectionFailure();
                break;
            }

            _channel.Writer.TryWrite(packet);
        }
    }
}