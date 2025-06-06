using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using dArtagnan.Shared;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public GameManager GameManager;
    private TcpClient _client;
    private NetworkStream _stream;

    public async Task SendPacket<T>(PacketType type, T packet) where T : struct
    {
        await NetworkUtils.SendPacketAsync(_stream, NetworkUtils.CreatePacket(type, packet));
    }

    private void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        _client = new TcpClient();
        await _client.ConnectAsync("localhost", 7777);
        _stream = _client.GetStream();
        _ = ListenLoop();
    }

    private async Task ListenLoop()
    {
        await SendPacket(PacketType.PlayerJoin, new PlayerJoinPacket
        {
            Nickname = "hello"
        });
        Debug.Log("Joined!");
        while (true)
        {
            var packet = await NetworkUtils.ReceivePacketAsync(_stream);
            UnityMainThreadDispatcher.Enqueue(() => { HandlePacket(packet.Value); });
        }
    }

    private void HandlePacket(Packet packet)
    {
        switch (packet.Type)
        {
            case PacketType.PlayerMove:
                var movePacket = NetworkUtils.GetData<MovePacket>(packet);
                GameManager.OnPlayerMove(movePacket.PlayerId, new Vector2(movePacket.X, movePacket.Y));
                break;
        }
    }
}
