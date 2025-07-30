using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Networking;
using UnityEngine;

public class NetworkManager : MonoBehaviour, IChannelListener
{
    [Header("Config")] [SerializeField] private NetworkManagerConfig config;

    private readonly Channel<IPacket> _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();

    private TcpClient _client;
    private NetworkStream _stream;

    private void Update()
    {
        if (_channel.Reader.TryRead(out var packet))
        {
            PacketChannel.Raise(packet);
        }
    }

    public void Initialize()
    {
        PacketChannel.On<PlayerMovementDataFromClient>(Send);
        PacketChannel.On<PlayerShootingFromClient>(Send);
        PacketChannel.On<PlayerIsTargetingFromClient>(Send);
        PacketChannel.On<StartGameFromClient>(Send);
        PacketChannel.On<SetAccuracyState>(Send);
        PacketChannel.On<AugmentDoneFromClient>(Send);
        PacketChannel.On<WantToSelectAccuracyFromClient>(Send);
        LocalEventChannel.OnEndpointSelected += Connect;
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
        await _client.ConnectAsync(host, port).AsUniTask();
        _stream = _client.GetStream();
        await NetworkUtils.SendPacketAsync(_stream, new PingPacket());
        await NetworkUtils.SendPacketAsync(_stream, new PlayerJoinRequest());
    }

    private void Send<T>(T payload) where T : IPacket
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
}