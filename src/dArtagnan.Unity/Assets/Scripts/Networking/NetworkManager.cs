using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using UnityEngine;

namespace Networking
{
    public class NetworkManager : MonoBehaviour
    {
        private readonly Channel<IPacket> _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();
        private TcpClient _client;
        private NetworkStream _stream;

        private void Awake()
        {
            PacketChannel.On<MovementDataFromClient>(Send);
            PacketChannel.On<ShootingFromClient>(Send);
            PacketChannel.On<PlayerIsTargetingFromClient>(Send);
            PacketChannel.On<StartGameFromClient>(Send);
            PacketChannel.On<UpdateAccuracyStateFromClient>(Send);
            PacketChannel.On<AugmentDoneFromClient>(Send);
            PacketChannel.On<UpdateItemCreatingStateFromClient>(Send);
            PacketChannel.On<UseItemFromClient>(Send);
            PacketChannel.On<ChatFromClient>(Send);
        }

        private void Start()
        {
            Connect(PlayerPrefs.GetString("GameServerIP"), PlayerPrefs.GetInt("GameServerPort"));
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
            PacketChannel.Clear();
            _stream?.Close();
            _client?.Close();
        }

        private void Connect(string host, int port)
        {
            ConnectToServer(host, port)
                .ContinueWith(StartListeningLoop)
                .Forget();
        }

        private async UniTask ConnectToServer(string host, int port)
        {
            _client = new TcpClient { NoDelay = true };
            try
            {
                await _client.ConnectAsync(host, port).AsUniTask();
            }
            catch (Exception e)
            {
                GameService.ConnectionFailure.OnNext(true);
                Debug.LogException(e);
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
                GameService.ConnectionFailure.OnNext(true);
            }
        }

        private async UniTask StartListeningLoop()
        {
            await UniTask.SwitchToThreadPool();
            try
            {
                while (true)
                {
                    var packet = NetworkUtils.ReceivePacketSync(_stream);
                    _channel.Writer.TryWrite(packet);
                }
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                GameService.ConnectionFailure.OnNext(true);
            }
        }
    }
}