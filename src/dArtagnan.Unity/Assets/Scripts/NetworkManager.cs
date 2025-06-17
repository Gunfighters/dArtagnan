using System;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Int32;

public class NetworkManager : MonoBehaviour
{
    private Channel<IPacket> _channel;
    private TcpClient _client;
    private NetworkStream _stream;
    public static NetworkManager Instance { get; private set; }
    public GameObject ServerEndpointInputFieldObject;
    public GameObject ServerEndpointConfirmButtonObject;

    void Awake()
    {
        Instance = this;
        _channel = Channel.CreateUnbounded<IPacket>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    void Start()
    {
        var btn = ServerEndpointConfirmButtonObject.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            var inputField = ServerEndpointInputFieldObject.GetComponent<TMP_InputField>();
            if (inputField.text != "")
            {
                Debug.Log(inputField.text);
                var split = inputField.text.Split(':');
                var host = split[0];
                var port = Parse(split[1]);
                ConnectToServer(host, port);
            }
        });
    }

    async Task ConnectToServer(string host, int port)
    {
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
        GameManager.Instance.GetPing(host);
        ServerEndpointConfirmButtonObject.SetActive(false);
        ServerEndpointInputFieldObject.SetActive(false);
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

    public void SendPlayerDirection(Vector3 position, Vector3 direction)
    {
        Enqueue(new PlayerDirectionFromClient
        {
            direction = DirectionHelperClient.DirectionToInt(direction),
            currentX = position.x,
            currentY = position.y
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
            case YouAre are:
                GameManager.Instance.OnYouAre(are);
                break;
            case InformationOfPlayers info:
                GameManager.Instance.OnInformationOfPlayers(info);
                break;
            case PlayerJoinBroadcast joinBroadcast:
                GameManager.Instance.OnPlayerJoinBroadcast(joinBroadcast);
                break;
            case PlayerDirectionBroadcast playerDirectionBroadcast:
                GameManager.Instance.OnPlayerDirectionBroadcast(playerDirectionBroadcast);
                break;
            case UpdatePlayerSpeedBroadcast update:
                GameManager.Instance.OnUpdatePlayerSpeedBroadcast(update);
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
            case UpdatePlayerPosition position:
                GameManager.Instance.OnUpdatePlayerPosition(position);
                break;
            default:
                Debug.LogWarning($"Unhandled packet: {packet}");
                break;
        }
    }
}