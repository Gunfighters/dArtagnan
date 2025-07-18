using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Networking;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private NetworkManagerConfig config;

    private TcpClient _client;
    private NetworkStream _stream;
    private readonly Channel<IPacket> _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();

    private void OnEnable()
    {
        EventChannel<IPacket>.Instance.On<PlayerMovementDataFromClient>(Send);
        EventChannel<IPacket>.Instance.On<PlayerShootingFromClient>(Send);
        EventChannel<IPacket>.Instance.On<PlayerIsTargetingFromClient>(Send);
        EventChannel<IPacket>.Instance.On<StartGameFromClient>(Send);
        EventChannel<IPacket>.Instance.On<SetAccuracyState>(Send);
        EventChannel<IPacket>.Instance.On<RouletteDone>(Send);
    }

    private void Update()
    {
        if (_channel.Reader.TryRead(out var packet))
        {
            EventChannel<IPacket>.Instance.Raise(packet);
        }
    }

    public void Connect(string host, int port)
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