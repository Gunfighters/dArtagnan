using System;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private Channel<IPacket> _channel;
    private TcpClient _client;
    private NetworkStream _stream;
    public static NetworkManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _channel = Channel.CreateUnbounded<IPacket>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    async void Start()
    {
        _client = new TcpClient();
        await _client.ConnectAsync("localhost", 7777);
        _stream = _client.GetStream();
        _ = StartSendingLoop();
        _ = StartListeningLoop();
    }

    void Enqueue(IPacket payload)
    {
        _channel.Writer.TryWrite(payload);
    }

    async Task StartSendingLoop()
    {
        while (await _channel.Reader.WaitToReadAsync())
        {
            while (_channel.Reader.TryRead(out var packet))
            {
                try
                {
                    await SendPacket(packet);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

    async Task StartListeningLoop()
    {
        while (true)
        {
            var received = await NetworkUtils.ReceivePacketAsync(_stream);
            HandlePacket(received);
        }
    }

    async Task SendPacket(IPacket packet)
    {
        await NetworkUtils.SendPacketAsync(_stream, packet);
    }

    public void SendJoinRequest()
    {
        Enqueue(new JoinRequestFromClient());
    }

    public void SendPlayerDirection(Vector3 direction)
    {
        Enqueue(new PlayerDirectionFromClient
        {
            direction = DirectionHelper.DirectionToInt(direction),
        });
    }

    public void SendPlayerIsRunning(bool isRunning)
    {
        Enqueue(new PlayerRunningFromClient() { isRunning = isRunning });
    }

    public void SendPlayerShooting(int target)
    {
        Enqueue(new PlayerShootingFromClient { targetId = target });
    }

    void HandlePacket(IPacket packet)
    {
        switch (packet)
        {
            case InformationOfPlayers informationOfPlayers:
                GameManager.Instance.OnInformationOfPlayers(informationOfPlayers);
                break;
            case JoinResponseFromServer response:
                GameManager.Instance.OnJoinResponseFromServer(response);
                break;
            case PlayerDirectionFromServer direction:
                GameManager.Instance.OnPlayerDirectionFromServer(direction);
                break;
            case PlayerRunningFromServer running:
                GameManager.Instance.OnPlayerRunningFromServer(running);
                break;
            case PlayerShootingFromServer shooting:
                GameManager.Instance.OnPlayerShootingFromServer(shooting);
                break;
            case YouAre are:
                GameManager.Instance.OnYouAre(are);
                break;
            default:
                Debug.LogWarning($"Unhandled packet: {packet}");
                break;
        }
    }
}