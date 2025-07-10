using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

public class NetworkManager : MonoBehaviour
{
    private Channel<IPacket> _channel;
    private TcpClient _client;
    private NetworkStream _stream;
    public static NetworkManager Instance { get; private set; }
    public string host;
    public int port;

    async void Awake()
    {
        Instance = this;
        _channel = Channel.CreateUnbounded<IPacket>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        try
        {
            await ConnectToServer();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    async Task ConnectToServer()
    {
        Debug.Log($"Connecting to: {host}:{port}");
        _client = new TcpClient();
        try
        {
            await _client.ConnectAsync(host, port);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }
        _stream = _client.GetStream();
        _ = StartSendingLoop();
        _ = StartListeningLoop();
        StartCoroutine(StartGetPingLoop());
    }

    IEnumerator StartGetPingLoop()
    {
        while (true)
        {
            Ping p = new(host);
            yield return new WaitUntil(() => p.isDone);
            GameManager.Instance.SetPing(p);
            yield return new WaitForSeconds(0.5f);
        }
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
            try
            {
                var received = await NetworkUtils.ReceivePacketAsync(_stream);
                HandlePacket(received);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    async Task SendPacket(IPacket packet)
    {
        await NetworkUtils.SendPacketAsync(_stream, packet);
    }

    public void SendJoinRequest()
    {
        Enqueue(new PlayerJoinRequest());
    }

    public void SendPlayerMovementData(Vector3 position, Vector3 direction, bool running)
    {
        Enqueue(new PlayerMovementDataFromClient
        {
            Direction = DirectionHelperClient.DirectionToInt(direction),
            Position = new Vector2(position.x, position.y),
            Running = running
        });
    }


    public void SendPlayerShooting(int target)
    {
        Enqueue(new PlayerShootingFromClient { TargetId = target });
    }

    public void SendPlayerNewTarget(int target)
    {
        Enqueue(new PlayerIsTargetingFromClient { TargetId = target });
    }

    public void SendStartGame()
    {
        Enqueue(new StartGame());
    }

    void HandlePacket(IPacket packet)
    {
        switch (packet)
        {
            case YouAre are:
                GameManager.Instance.OnYouAre(are);
                break;
            case PlayerJoinBroadcast joinBroadcast:
                GameManager.Instance.OnPlayerJoinBroadcast(joinBroadcast);
                break;
            case PlayerMovementDataBroadcast playerMovementDataBroadcast:
                GameManager.Instance.OnPlayerMovementData(playerMovementDataBroadcast);
                break;
            case PlayerShootingBroadcast shootingBroadcast:
                GameManager.Instance.OnPlayerShootingBroadcast(shootingBroadcast);
                break;
            case UpdatePlayerAlive updatePlayerAlive:
                GameManager.Instance.OnUpdatePlayerAlive(updatePlayerAlive);
                break;
            case PlayerLeaveBroadcast playerLeaveBroadcast:
                GameManager.Instance.OnPlayerLeaveBroadcast(playerLeaveBroadcast);
                break;
            case NewHost newHost:
                GameManager.Instance.OnNewHost(newHost);
                break;
            case PlayerIsTargetingBroadcast playerIsTargetingBroadcast:
                GameManager.Instance.OnPlayerIsTargeting(playerIsTargetingBroadcast);
                break;
            case Winner winner:
                GameManager.Instance.OnWinner(winner);
                break;
            case GameWaiting gameWaiting:
                GameManager.Instance.OnGameWaiting(gameWaiting);
                break;
            case GamePlaying gamePlaying:
                GameManager.Instance.OnGamePlaying(gamePlaying);
                break;
            case PlayerBalanceUpdate playerBalanceUpdate:
                GameManager.Instance.OnPlayerBalanceUpdate(playerBalanceUpdate);
                break;
            default:
                Debug.LogWarning($"Unhandled packet: {packet}");
                break;
        }
    }
}