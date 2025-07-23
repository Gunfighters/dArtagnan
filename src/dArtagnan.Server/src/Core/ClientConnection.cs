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
            Console.WriteLine($"[클라이언트 {Id}] 연결됨. 패킷 수신 시작.");
                
            while (isRunning)
            {
                var packet = await NetworkUtils.ReceivePacketAsync(stream);
                    
                await RoutePacket(packet);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[클라이언트 {Id}] 수신 루프 오류: {ex.Message}");
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
            IGameCommand? command = packet switch
            {
                PlayerJoinRequest joinRequest => new PlayerJoinCommand
                {
                    ClientId = Id,
                    Nickname = $"Player #{Id}",
                    Client = this
                },
                
                PlayerMovementDataFromClient movementData => new PlayerMovementCommand
                {
                    PlayerId = Id,
                    MovementData = movementData.MovementData,
                },
                
                PlayerShootingFromClient shootingData => new PlayerShootingCommand
                {
                    ShooterId = Id,
                    TargetId = shootingData.TargetId
                },
                
                PlayerLeaveFromClient => new PlayerLeaveCommand
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
                
                SetAccuracyState accuracyState => new SetAccuracyCommand
                {
                    PlayerId = Id,
                    AccuracyState = accuracyState.AccuracyState
                },
                
                RouletteDone => new RouletteDoneCommand
                {
                    PlayerId = Id
                },
                
                AugmentDoneFromClient augmentDone => new AugmentDoneCommand
                {
                    ClientId = Id,
                    SelectedAugmentIndex = augmentDone.SelectedAugmentIndex
                },
                
                ItemCreatingStateFromClient itemCreating => new ItemCreatingStateCommand
                {
                    PlayerId = Id,
                    IsCreatingItem = itemCreating.IsCreatingItem
                },
                
                UseItemFromClient useItem => new UseItemCommand
                {
                    PlayerId = Id,
                    TargetPlayerId = useItem.TargetPlayerId
                },
                
                _ => null
            };
            
            if (command != null)
            {
                await gameManager.EnqueueCommandAsync(command);
            }
            else
            {
                Console.WriteLine($"[클라이언트 {Id}] 처리되지 않은 패킷 타입: {packet.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[클라이언트 {Id}] 패킷 라우팅 중 오류 발생: {ex.Message}");
            Console.WriteLine($"[클라이언트 {Id}] 패킷 타입: {packet.GetType().Name}");
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
            Console.WriteLine($"[클라이언트 {Id}] 패킷 전송 실패: {ex.Message}");
            
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
        Console.WriteLine($"[클라이언트 {Id}] 연결 해제 중...");

        try
        {
            isRunning = false;
            stream.Close();
            tcpClient.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[클라이언트 {Id}] 연결 해제 중 오류: {ex.Message}");
        }

        Console.WriteLine($"[클라이언트 {Id}] 연결 해제 완료");
        return Task.CompletedTask;
    }
}