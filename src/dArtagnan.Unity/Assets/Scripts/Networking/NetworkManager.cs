using System.Net.Sockets;
using System.Net;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using Networking;
using UnityEngine;

public class NetworkManager : MonoBehaviour, IChannelListener
{
    [Header("Config")]
    [SerializeField] private NetworkManagerConfig config;

    private TcpClient _client;
    private NetworkStream _stream;
    private UdpClient _udpClient;
    private IPEndPoint _serverUdpEndpoint;
    private readonly Channel<IPacket> _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();

    public void Initialize()
    {
        PacketChannel.On<PlayerMovementDataFromClient>(SendUdp);  // UDP로 전송
        PacketChannel.On<PlayerShootingFromClient>(Send);         // TCP로 전송
        PacketChannel.On<PlayerIsTargetingFromClient>(Send);      // TCP로 전송
        PacketChannel.On<StartGameFromClient>(Send);              // TCP로 전송
        PacketChannel.On<SetAccuracyState>(Send);                 // TCP로 전송
        PacketChannel.On<RouletteDone>(Send);                     // TCP로 전송
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
        
        // TCP 연결
        _client = new TcpClient();
        _client.NoDelay = true;
        await _client.ConnectAsync(host, port).AsUniTask();
        _stream = _client.GetStream();
        
        // UDP 연결 설정
        try
        {
            _udpClient = new UdpClient();
            var serverAddress = host == "localhost" ? IPAddress.Loopback : IPAddress.Parse(host);
            _serverUdpEndpoint = new IPEndPoint(serverAddress, port + 1);  // UDP는 포트 + 1
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UDP 클라이언트 생성 실패: {ex.Message}");
            throw;
        }
        
        await NetworkUtils.SendPacketAsync(_stream, new PingPacket());
        await NetworkUtils.SendPacketAsync(_stream, new PlayerJoinRequest());
        
        Debug.Log($"Connected to TCP:{port} and UDP:{port + 1}");
        
        // UDP 수신 루프 시작
        StartUdpListeningLoop().Forget();
    }

    private void Send<T>(T payload) where T : IPacket
    {
        NetworkUtils.SendPacketSync(_stream, payload);
    }

    private void SendUdp<T>(T payload) where T : IPacket
    {
        try
        {
            var data = NetworkUtils.SerializeUdpPacket(payload);
            _udpClient.SendAsync(data, data.Length, _serverUdpEndpoint);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UDP 패킷 전송 실패: {ex.Message}");
        }
    }

    private async UniTask StartListeningLoop()
    {
        while (true)
        {
            var received = await NetworkUtils.ReceivePacketAsync(_stream);
            _channel.Writer.TryWrite(received);
        }
    }

    private async UniTask StartUdpListeningLoop()
    {
        try
        {
            while (true)
            {
                var result = await _udpClient.ReceiveAsync().AsUniTask();
                var packet = NetworkUtils.DeserializeUdpPacket(result.Buffer);
                
                // UDP 패킷을 채널에 추가하여 메인 스레드에서 처리
                _channel.Writer.TryWrite(packet);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UDP 수신 루프 오류: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        _udpClient?.Close();
        _stream?.Close();
        _client?.Close();
    }
}