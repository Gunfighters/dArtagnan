using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 네트워크 연결과 패킷 라우팅을 담당하는 클래스
/// </summary>
public class ClientConnection
{
    private readonly NetworkStream stream;
    private readonly TcpClient tcpClient;
    private readonly GameManager gameManager;
    private bool isRunning = true;
    public readonly int Id;
    public readonly string IpAddress;

    //로그 출력용
    public string? Nickname { get; set; }
    private string LogSubject => Nickname ?? $"Client-{Id}";

    public ClientConnection(int id, TcpClient client, GameManager gameManager)
    {
        Id = id;
        tcpClient = client;
        
        tcpClient.NoDelay = true;
        
        IpAddress = client.Client.RemoteEndPoint!.ToString()!.Split(":")[0];
        stream = client.GetStream();
        this.gameManager = gameManager;

        // 패킷 수신 루프 시작
        _ = Task.Run(ReceiveLoop);
    }

    private async Task ReceiveLoop()
    {
        try
        {
            Logger.log($"[Network][{LogSubject}] 연결 수립: 패킷 수신 시작");

            while (isRunning)
            {
                var packet = await NetworkUtils.ReceivePacketAsync(stream);

                await RoutePacket(packet);
            }
        }
        catch (Exception ex)
        {
            Logger.log($"[Network][{LogSubject}] 수신 루프 오류: {ex.Message}");
            if (isRunning)
            {
                var removeCommand = new RemoveClientCommand
                {
                    ClientId = Id,
                    Client = this,
                    IsNormalDisconnect = false
                };

                await gameManager.EnqueueCommandAsync(removeCommand);
            }
        }
    }

    private async Task RoutePacket(IPacket packet)
    {
        try
        {
            Logger.log($"[Packet][{LogSubject}] ⬅️ {packet.GetType().Name} 수신");

            IGameCommand? command = packet switch
            {
                JoinRequest joinRequest => new PlayerJoinCommand
                {
                    ClientId = Id,
                    Nickname = joinRequest.Nickname,
                    ProviderId = joinRequest.ProviderId,
                    CostumeId = joinRequest.CostumeId,
                    Client = this
                },
                MovementDataFromClient movementData => new PlayerMovementCommand
                {
                    PlayerId = Id,
                    MovementData = movementData.MovementData
                },
                ShootingFromClient shootingData => new PlayerShootingCommand
                {
                    ShooterId = Id,
                    TargetId = shootingData.TargetId
                },
                LeaveFromClient => new PlayerLeaveCommand
                {
                    PlayerId = Id,
                    Client = this
                },
                PlayerIsTargetingFromClient isTargetingData => new PlayerTargetingCommand
                {
                    ShooterId = Id,
                    TargetId = isTargetingData.TargetId
                },
                StartGameFromClient => new StartGameCommand
                {
                    PlayerId = Id
                },
                PingPacket => new PingCommand
                {
                    Client = this
                },
                UpdateAccuracyStateFromClient accuracyState => new SetAccuracyCommand
                {
                    PlayerId = Id,
                    AccuracyState = accuracyState.AccuracyState
                },
                ChatFromClient chatMessage => new ChatFromClientCommand
                {
                    PlayerId = Id,
                    Message = chatMessage.Message
                },
                InitialRouletteDoneFromClient => new InitialRouletteDoneCommand
                {
                    PlayerId = Id
                },
                PurchaseItemFromClient purchaseItem => new ShopPurchaseItemCommand
                {
                    PlayerId = Id,
                    ItemId = purchaseItem.ItemId
                },
                ShopRouletteFromClient => new ShopRouletteCommand
                {
                    PlayerId = Id
                },
                UpdateMiningStateFromClient miningState => new UpdateMiningStateCommand
                {
                    PlayerId = Id,
                    IsMining = miningState.IsMining
                },
                _ => null
            };

            if (command != null)
            {
                await gameManager.EnqueueCommandAsync(command);
            }
            else
            {
                Logger.log($"[Packet][{LogSubject}] 처리되지 않은 패킷 타입: {packet.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            Logger.log($"[Packet][{LogSubject}] 패킷 라우팅 오류: {ex.Message} (타입: {packet.GetType().Name})");
        }
    }

    public async Task SendPacketAsync(IPacket packet)
    {
        try
        {
            await NetworkUtils.SendPacketAsync(stream, packet);
        }
        catch (Exception ex)
        {
            Logger.log($"[Packet][{LogSubject}] 패킷 전송 실패: {ex.Message} (타입: {packet.GetType().Name})");

            var removeCommand = new RemoveClientCommand
            {
                ClientId = Id,
                Client = this,
                IsNormalDisconnect = false
            };
            await gameManager.EnqueueCommandAsync(removeCommand);
        }
    }

    public Task DisconnectAsync()
    {
        Logger.log($"[Network][{LogSubject}] 연결 해제 시작");

        try
        {
            isRunning = false;
            stream.Close();
            tcpClient.Close();
        }
        catch (Exception ex)
        {
            Logger.log($"[Network][{LogSubject}] 연결 해제 오류: {ex.Message}");
        }

        Logger.log($"[Network][{LogSubject}] 연결 해제 완료");
        return Task.CompletedTask;
    }
}