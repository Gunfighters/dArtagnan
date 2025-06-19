using System.Net.Sockets;
using dArtagnan.Server.Handlers;
using dArtagnan.Shared;

namespace dArtagnan.Server.Network
{
    /// <summary>
    /// 클라이언트 네트워크 연결과 패킷 라우팅을 담당하는 클래스
    /// </summary>
    public class ClientConnection : IDisposable
    {
        private readonly NetworkStream stream;
        private readonly TcpClient tcpClient;
        private bool isConnected;

        // 연결 해제 콜백
        private readonly Func<ClientConnection, Task> onDisconnected;

        // 핸들러들
        private readonly JoinHandler joinHandler;
        private readonly MovementHandler movementHandler;
        private readonly CombatHandler combatHandler;
        private readonly LeaveHandler leaveHandler;

        // 브로드캐스트 함수들
        private readonly Func<IPacket, Task> broadcastToAll;
        private readonly Func<IPacket, int, Task> broadcastToAllExcept;

        public int Id { get; }

        public ClientConnection(
            int id, 
            TcpClient client,
            JoinHandler joinHandler,
            MovementHandler movementHandler,
            CombatHandler combatHandler,
            LeaveHandler leaveHandler,
            Func<IPacket, Task> broadcastToAll,
            Func<IPacket, int, Task> broadcastToAllExcept,
            Func<ClientConnection, Task> onDisconnected)
        {
            Id = id;
            tcpClient = client;
            stream = client.GetStream();
            isConnected = true;
            
            this.joinHandler = joinHandler;
            this.movementHandler = movementHandler;
            this.combatHandler = combatHandler;
            this.leaveHandler = leaveHandler;
            this.broadcastToAll = broadcastToAll;
            this.broadcastToAllExcept = broadcastToAllExcept;
            this.onDisconnected = onDisconnected;

            // 패킷 수신 루프 시작
            _ = Task.Run(ReceiveLoop);
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
        public async Task DisconnectAsync()
        {
            if (!isConnected) return;

            isConnected = false;
            Console.WriteLine($"[클라이언트 {Id}] 연결 해제 중...");

            try
            {
                // 연결 해제 콜백 호출
                await onDisconnected(this);

                stream?.Close();
                tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {Id}] 연결 해제 중 오류: {ex.Message}");
            }

            Console.WriteLine($"[클라이언트 {Id}] 연결 해제 완료");
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
                        await joinHandler.HandlePlayerJoin(joinRequest, this, broadcastToAll);
                        break;

                    case PlayerDirectionFromClient directionData:
                        await movementHandler.HandlePlayerDirection(directionData, this, broadcastToAll);
                        break;

                    case PlayerRunningFromClient runningData:
                        await movementHandler.HandlePlayerRunning(runningData, this, broadcastToAll);
                        break;

                    case PlayerShootingFromClient shootingData:
                        await combatHandler.HandlePlayerShooting(shootingData, this, broadcastToAll);
                        break;

                    case PlayerLeaveFromClient leaveData:
                        await leaveHandler.HandlePlayerLeave(leaveData, this, broadcastToAllExcept);
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
        /// 클라이언트 연결 해제를 처리합니다
        /// </summary>
        public async Task HandleDisconnect()
        {
            try
            {
                Console.WriteLine($"[클라이언트 {Id}] 연결 해제 처리");
                await leaveHandler.HandleClientDisconnect(this, broadcastToAllExcept);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {Id}] 연결 해제 처리 중 오류 발생: {ex.Message}");
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
                // 연결 해제 처리
                await DisconnectAsync();
            }
        }
    }
} 