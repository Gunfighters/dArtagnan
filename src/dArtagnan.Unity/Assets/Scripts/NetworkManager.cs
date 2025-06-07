using System;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public GameManager gameManager;
    private Channel<IPacket> _channel;
    private TcpClient _client;
    private NetworkStream _stream;

    void Awake()
    {
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

    void HandlePacket(IPacket packet)
    {
        switch (packet)
        {
            case InformationOfPlayers informationOfPlayers:
                gameManager.OnInformationOfPlayers(informationOfPlayers);
                break;
            case JoinResponseFromServer response:
                gameManager.OnJoinResponseFromServer(response);
                break;
            case PlayerDirectionFromServer direction:
                gameManager.OnPlayerDirectionFromServer(direction);
                break;
            case PlayerRunningFromServer running:
                gameManager.OnPlayerRunningFromServer(running);
                break;
            case YouAre are:
                gameManager.OnYouAre(are);
                break;
            default:
                Debug.LogWarning($"Unhandled packet: {packet}");
                break;
        }
    }
}