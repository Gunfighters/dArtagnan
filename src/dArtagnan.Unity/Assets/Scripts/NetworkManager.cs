using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public GameManager GameManager;
    private TcpClient _client;
    private NetworkStream _stream;

    private Queue<Func<Task>> Q;
    private bool sending;
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

    async Task SendPacket<T>(PacketType type, T packet) where T : struct
    {
        await NetworkUtils.SendPacketAsync(_stream, NetworkUtils.CreatePacket(type, packet));
    }

    async Task SendLoop()
    {
        sending = true;
        while (Q.Count > 0)
        {
            var t = Q.Dequeue();
            await t();
        }

        sending = false;
    }

    void Enqueue(Func<Task> T)
    {
        Q.Enqueue(T);
    }

    public void SendJoinRequest()
    {
        Enqueue(async () => await SendPacket(PacketType.JoinRequestFromClient, new JoinRequestFromClient()));
    }

    public void SendPlayerDirection(Vector2 direction)
    {
        Enqueue(async () => await SendPacket(PacketType.PlayerDirectionFromClient, new PlayerDirectionFromClient
        {
            direction = GameManager.DirectionToInt(direction)
        }));
    }

    public void SendRunning(bool isRunning)
    {
        Enqueue(async () => await SendPacket(PacketType.PlayerRunningFromClient, new PlayerRunningFromClient
        {
            isRunning = isRunning
        }));
    }

    async Task ListenLoop()
    {
        while (true)
        {
            var packet = await NetworkUtils.ReceivePacketAsync(_stream);
            if (packet is null)
            {
                Debug.LogError("ReceivePacketAsync returned null");
                return;
            }

            HandlePacket((Packet)packet);
        }
    }

    void HandlePacket(Packet packet)
    {
        switch (packet.Type)
        {
            case PacketType.JoinResponseFromServer:
                GameManager.OnJoinResponseFromServer(NetworkUtils.GetData<JoinResponseFromServer>(packet));
                break;
            case PacketType.PlayerDirectionFromServer:
                GameManager.OnPlayerDirectionFromServer(NetworkUtils.GetData<PlayerDirectionFromServer>(packet));
                break;
            case PacketType.PlayerRunningFromServer:
                GameManager.OnPlayerRunningFromServer(NetworkUtils.GetData<PlayerRunningFromServer>(packet));
                break;
            case PacketType.YouAre:
                GameManager.OnYouAre(NetworkUtils.GetData<YouAre>(packet));
                break;
            default:
                Debug.LogError($"Unhandled packet type in HandlePacket(): {packet.Type}");
                break;
        }
    }
}