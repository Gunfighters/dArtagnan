using System.Net.Sockets;
using dArtagnan.Server.Core.Commands.Protocol;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 네트워크 연결과 패킷 라우팅을 담당하는 클래스
/// </summary>
public class ClientConnection
{
    private readonly GameManager gameManager;
    public readonly int Id;
    public readonly string IpAddress;
    private readonly NetworkStream stream;
    private readonly TcpClient tcpClient;
    private bool isRunning = true;

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
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}][클라이언트 {Id}] {packet.GetType().Name} 패킷 수신");
            IGameCommand? command = null; // command 변수를 미리 선언합니다.

            // switch '식'을 switch '문'으로 변경합니다.
            switch (packet)
            {
                case PlayerJoinRequest joinRequest:
                    command = new PlayerJoinCommand
                    {
                        ClientId = Id,
                        Nickname = $"Player #{Id}",
                        Client = this
                    };
                    break;

                case PlayerMovementDataFromClient movementData:
                    command = new PlayerMovementCommand
                    {
                        PlayerId = Id,
                        MovementData = movementData.MovementData,
                    };
                    break;

                case PlayerShootingFromClient shootingData:
                    command = new PlayerShootingCommand
                    {
                        ShooterId = Id,
                        TargetId = shootingData.TargetId
                    };
                    break;

                case PlayerLeaveFromClient:
                    command = new PlayerLeaveCommand
                    {
                        PlayerId = Id,
                        Client = this
                    };
                    break;

                case PlayerIsTargetingFromClient isTargetingData:
                    command = new PlayerTargetingCommand
                    {
                        ShooterId = Id,
                        TargetId = isTargetingData.TargetId
                    };
                    break;

                case StartGameFromClient:
                    command = new StartGameCommand
                    {
                        PlayerId = Id
                    };
                    break;

                case PingPacket:
                    command = new PingCommand
                    {
                        Client = this
                    };
                    break;

                case SetAccuracyState accuracyState:
                    command = new SetAccuracyCommand
                    {
                        PlayerId = Id,
                        AccuracyState = accuracyState.AccuracyState
                    };
                    break;

                case WantToSelectAccuracyFromClient e:
                    command = new SelectAccuracyCommand
                    {
                        PlayerId = Id,
                        AccuracyIndex = e.AccuracyIndexDesired
                    };
                    break;

                case AugmentDoneFromClient augmentDone:
                    command = new AugmentDoneCommand
                    {
                        ClientId = Id,
                        SelectedAugmentId = augmentDone.SelectedAugmentID
                    };
                    break;

                case ItemCreatingStateFromClient itemCreating:
                    command = new ItemCreatingStateCommand
                    {
                        PlayerId = Id,
                        IsCreatingItem = itemCreating.IsCreatingItem
                    };
                    break;

                case UseItemFromClient useItem:
                    command = new UseItemCommand
                    {
                        PlayerId = Id,
                        TargetPlayerId = useItem.TargetPlayerId
                    };
                    break;

                // 처리되지 않은 패킷은 command가 null로 유지됩니다.
            }

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