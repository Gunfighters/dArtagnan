using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 네트워크 연결과 패킷 라우팅을 담당하는 클래스
/// </summary>
public class ClientConnection : IDisposable
{
    private readonly NetworkStream stream;
    private readonly TcpClient tcpClient;
    private bool isConnected;
    private readonly GameManager gameManager;

    public int Id { get; }
    public string IpAddress { get; }
    public float Ping { get; private set; }

    public ClientConnection(int id, TcpClient client, GameManager gameManager)
    {
        Id = id;
        tcpClient = client;
        IpAddress = client.Client.RemoteEndPoint!.ToString()!.Split(":")[0];
        stream = client.GetStream();
        isConnected = true;
        this.gameManager = gameManager;

        // GameManager에 클라이언트 등록
        gameManager.AddClient(this);

        // 패킷 수신 루프 시작
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(UpdatePingLoop);
    }

    public bool IsConnected => isConnected && tcpClient.Connected;

    public void Dispose()
    {
        _ = DisconnectAsync();
    }

    public async Task SendPacketAsync(IPacket packet)
    {
        if (!IsConnected) return;

        try
        {
            await NetworkUtils.SendPacketAsync(stream, packet);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[클라이언트 {Id}] 패킷 전송 실패: {ex.Message}");
            await DisconnectAsync();
        }
    }

    public Task DisconnectAsync()
    {
        if (!isConnected) return Task.CompletedTask;

        isConnected = false;
        Console.WriteLine($"[클라이언트 {Id}] 연결 해제 중...");

        try
        {
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

    private async Task RoutePacket(IPacket packet)
    {
        try
        {
            Console.WriteLine($"[클라이언트 {Id}] 패킷 라우팅: {packet.GetType().Name}");

            switch (packet)
            {
                case PlayerJoinRequest joinRequest:
                    await PacketHandlers.HandlePlayerJoin(joinRequest, this, gameManager);
                    break;

                case PlayerMovementDataFromClient movementData:
                    await PacketHandlers.HandlePlayerMovementInformation(movementData, this, gameManager);
                    break;

                case PlayerShootingFromClient shootingData:
                    await PacketHandlers.HandlePlayerShooting(shootingData, this, gameManager);
                    break;

                case PlayerLeaveFromClient leaveData:
                    await PacketHandlers.HandlePlayerLeave(leaveData, this, gameManager);
                    break;
                    
                case PlayerIsTargetingFromClient isTargetingData:
                    await PacketHandlers.HandlePlayerIsTargeting(isTargetingData, this, gameManager);
                    break;
                    
                case StartGame start:
                    await PacketHandlers.HandleStartGame(start, this, gameManager);
                    break;
                    
                default:
                    Console.WriteLine($"[클라이언트 {Id}] 처리되지 않은 패킷 타입: {packet.GetType().Name}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[클라이언트 {Id}] 패킷 라우팅 중 오류 발생: {ex.Message}");
            Console.WriteLine($"[클라이언트 {Id}] 패킷 타입: {packet.GetType().Name}");
        }
    }

    private async Task ReceiveLoop()
    {
        try
        {
            Console.WriteLine($"[클라이언트 {Id}] 연결됨. 패킷 수신 시작.");
                
            while (IsConnected)
            {
                var packet = await NetworkUtils.ReceivePacketAsync(stream);
                Console.WriteLine($"[클라이언트 {Id}] 패킷 수신: {packet.GetType().Name}");
                    
                // 패킷 라우팅 처리
                await RoutePacket(packet);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[클라이언트 {Id}] 수신 루프 오류: {ex.Message}");
        }
        finally
        {
            // 비정상 종료 시 GameManager에서 정리
            await gameManager.RemoveClient(Id);
            await DisconnectAsync();
        }
    }

    private async Task UpdatePingLoop()
    {
        using Ping p = new();
        while (true)
        {
            try
            {
                var result = await p.SendPingAsync(IpAddress);
                if (result.Status == IPStatus.Success)
                {
                    Ping = result.RoundtripTime / 1000f;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[핑] {Id}번 플레이어({IpAddress}) 핑 측정 실패:  {ex.Message}");
            }

            await Task.Delay(500);
        }
    }
}