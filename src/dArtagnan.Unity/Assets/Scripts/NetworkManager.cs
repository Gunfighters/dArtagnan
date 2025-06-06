using System.Net.Sockets;
using System.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public GameManager GameManager;
    private TcpClient _client;
    private NetworkStream _stream;
    private bool sending = false;
    public static NetworkManager Instance { get; private set; }

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

    async void Update()
    {
        if (!sending) _ = SendLoop();
    }

    public async Task SendPacket<T>(PacketType type, T packet) where T : struct
    {
        await NetworkUtils.SendPacketAsync(_stream, NetworkUtils.CreatePacket(type, packet));
    }

    public void Enqueue(Vector2 v)
    {
        Q.Enqueue(v);
    }

    private async Task SendLoop()
    {
        sending = true;
        while (Q.Count > 0)
        {
            var v = Q.Dequeue();
            Debug.Log(v);
            await SendPacket(PacketType.PlayerMove, new MovePacket
            {
                PlayerId = GameManager.controlledPlayerIndex,
                X = v.x,
                Y = v.y
            });
        }

        sending = false;
    }

    private async Task ListenLoop()
    {
        await SendPacket(PacketType.PlayerJoin, new PlayerJoinPacket
        {
            Nickname = "hello"
        });
        await SendPacket(PacketType.PlayerMove, new MovePacket
        {
            PlayerId = 0,
            X = 0,
            Y = 0
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