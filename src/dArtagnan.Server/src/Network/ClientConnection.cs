using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server
{
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

        public ClientConnection(int id, TcpClient client, GameManager gameManager)
        {
            Id = id;
            tcpClient = client;
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

        /// <summary>
        /// 패킷을 클라이언트로 전송합니다
        /// </summary>
        public async Task SendPacketAsync(IPacket packet)
        {
            if (!IsConnected) return;

            try
            {
                Console.WriteLine($"[클라이언트 {Id}] 패킷 전송: {packet.GetType().Name}");
                await NetworkUtils.SendPacketAsync(stream, packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {Id}] 패킷 전송 실패: {ex.Message}");
                await DisconnectAsync();
            }
        }

        /// <summary>
        /// 연결을 해제합니다
        /// </summary>
        public Task DisconnectAsync()
        {
            if (!isConnected) return Task.CompletedTask;

            isConnected = false;
            Console.WriteLine($"[클라이언트 {Id}] 연결 해제 중...");

            try
            {
                stream?.Close();
                tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {Id}] 연결 해제 중 오류: {ex.Message}");
            }

            Console.WriteLine($"[클라이언트 {Id}] 연결 해제 완료");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 패킷을 적절한 핸들러로 라우팅합니다
        /// </summary>
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

                    case PlayerDirectionFromClient directionData:
                        await PacketHandlers.HandlePlayerDirection(directionData, this, gameManager);
                        break;

                    case PlayerRunningFromClient runningData:
                        await PacketHandlers.HandlePlayerRunning(runningData, this, gameManager);
                        break;

                    case PlayerShootingFromClient shootingData:
                        await PacketHandlers.HandlePlayerShooting(shootingData, this, gameManager);
                        break;

                    case PlayerLeaveFromClient leaveData:
                        await PacketHandlers.HandlePlayerLeave(leaveData, this, gameManager);
                        break;

                    case Ready readyData:
                        await PacketHandlers.HandleReady(readyData, this, gameManager);
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

        /// <summary>
        /// 패킷 수신 루프
        /// </summary>
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
            var addr = tcpClient.Client.RemoteEndPoint.ToString().Split(':')[0];
            using Ping p = new();
            while (true)
            {
                try
                {
                    var result = await p.SendPingAsync(addr);
                    if (result.Status == IPStatus.Success)
                    {
                        gameManager.ping[Id] = result.RoundtripTime / 1000f;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[핑] {Id}번 플레이어({addr}) 핑 측정 실패:  {ex.Message}");
                }

                await Task.Delay(500);
            }
        }
    }
} 