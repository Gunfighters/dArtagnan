using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    public class ClientConnection : IDisposable
    {
        private readonly GameServer gameServer;
        private readonly NetworkStream stream;
        private readonly TcpClient tcpClient;
        public int Id;
        private bool isConnected;
        private bool isInGame;
        // Player state properties (only for server-side logic)
        public int PlayerId { get; private set; }
        public string Nickname { get; private set; } = string.Empty;
        public int Accuracy { get; private set; }
        public int Direction { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        
        // 새로운 프로토콜 필드들
        public float TotalReloadTime { get; private set; }
        public float RemainingReloadTime { get; private set; }
        public float Speed { get; private set; }
        public bool Alive { get; private set; }

        public ClientConnection(int id, TcpClient client, GameServer server)
        {
            Id = id;
            tcpClient = client;
            stream = client.GetStream();
            gameServer = server;
            isConnected = true;
            isInGame = false;

            // 패킷 수신 루프 시작
            _ = Task.Run(ReceiveLoop);
        }

        public bool IsInGame => isInGame;
        public bool IsConnected => isConnected && tcpClient.Connected;

        public void Dispose()
        {
            _ = DisconnectAsync();
        }

        public void SetPlayerInfo(int playerId, string nickname)
        {
            PlayerId = playerId;
            Nickname = nickname;
            Accuracy = Random.Shared.Next(1, 100);
            Direction = 0;
            X = 0;
            Y = 0;
            
            // 새로운 필드들 초기화
            TotalReloadTime = 2.0f; // 기본 재장전 시간 2초
            RemainingReloadTime = 0.0f;
            Speed = 1f; // 기본 속도 (걷기)
            Alive = true;
        }

        /// <summary>
        /// 플레이어의 위치를 업데이트합니다
        /// </summary>
        public void UpdatePosition(float newX, float newY)
        {
            X = newX;
            Y = newY;
        }

        /// <summary>
        /// 플레이어의 속도를 업데이트합니다
        /// </summary>
        public void UpdateSpeed(float newSpeed)
        {
            Speed = newSpeed;
        }

        /// <summary>
        /// 플레이어의 생존 상태를 업데이트합니다
        /// </summary>
        public void UpdateAlive(bool alive)
        {
            Alive = alive;
        }

        /// <summary>
        /// 재장전 시간을 업데이트합니다
        /// </summary>
        public void UpdateReloadTime(float remaining)
        {
            RemainingReloadTime = remaining;
        }

        // 패킷 전송
        public async Task SendPacketAsync(IPacket packet)
        {
            if (!IsConnected) return;

            try
            {
                Console.WriteLine($"[클라이언트 {Id}] 패킷 전송: {packet}");
                await NetworkUtils.SendPacketAsync(stream, packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {Id}] 패킷 전송 실패: {ex.Message}");
                await DisconnectAsync();
            }
        }

        // 수신 루프
        private async Task ReceiveLoop()
        {
            try
            {
                Console.WriteLine($"[클라이언트 {Id}] 연결됨. 초기 정보 전송.");
                while (IsConnected)
                {
                    var packet = await NetworkUtils.ReceivePacketAsync(stream);
                    // 패킷 수신 로그 출력
                    Console.WriteLine($"[클라이언트 {Id}] 패킷 수신: {packet}");
                    await HandlePacket(packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {Id}] 수신 루프 오류: {ex.Message}");
            }
            finally
            {
                // 연결 해제 시 퇴장 처리 (정상/비정상 모두 동일한 경로)
                if (isInGame)
                {
                    await HandlePlayerLeave(new PlayerLeaveFromClient());
                }
                else
                {
                    await DisconnectAsync();
                }
            }
        }

        private async Task HandlePacket(IPacket packet)
        {
            switch (packet)
            {
                case PlayerJoinRequest request:
                    await HandlePlayerJoin(request);
                    break;
                case PlayerDirectionFromClient direction:
                    await HandlePlayerMove(direction);
                    break;
                case PlayerRunningFromClient running:
                    await HandlePlayerRunning(running);
                    break;
                case PlayerShootingFromClient shooting:
                    await HandlePlayerShooting(shooting);
                    break;
                case PlayerLeaveFromClient leave:
                    await HandlePlayerLeave(leave);
                    break;
                default:
                    Console.WriteLine($"처리되지 않은 패킷: {packet}");
                    break;
            }
        }

        private async Task HandlePlayerRunning(PlayerRunningFromClient running)
        {
            // 달리기 상태에 따라 속도 직접 설정
            float newSpeed = running.isRunning ? 4f : 1f;
            UpdateSpeed(newSpeed);
            
            // 속도 업데이트 브로드캐스트
            await gameServer.BroadcastToAll(new UpdatePlayerSpeedBroadcast
            {
                playerId = PlayerId,
                speed = Speed
            });
        }

        private async Task HandlePlayerJoin(PlayerJoinRequest joinData)
        {
            isInGame = true;
            await gameServer.JoinGame(this);
        }

        private async Task HandlePlayerMove(PlayerDirectionFromClient moveData)
        {
            if (IsInGame)
            {
                Direction = moveData.direction;
                X = moveData.currentX;
                Y = moveData.currentY;
                await gameServer.BroadcastToAll(new PlayerDirectionBroadcast
                {
                    direction = Direction,
                    playerId = PlayerId,
                    currentX = moveData.currentX,
                    currentY = moveData.currentY,
                });
            }
        }

        private async Task HandlePlayerShooting(PlayerShootingFromClient shooting)
        {
            if (IsInGame && Alive)
            {
                // 재장전 중인지 확인
                if (RemainingReloadTime > 0)
                {
                    Console.WriteLine($"[클라이언트 {Id}] 재장전 중이므로 사격 불가");
                    return;
                }

                // 명중 여부 계산 (Accuracy 기반)
                bool hit = Random.Shared.Next(1, 101) <= Accuracy;
                
                // 재장전 시간 설정
                RemainingReloadTime = TotalReloadTime;
                
                // 사격 브로드캐스트
                await gameServer.BroadcastToAll(new PlayerShootingBroadcast
                {
                    shooterId = PlayerId,
                    targetId = shooting.targetId,
                    hit = hit
                });
                
                // 명중 시 타겟 플레이어 처리
                if (hit)
                {
                    await gameServer.HandlePlayerHit(shooting.targetId);
                }
            }
        }

        private async Task HandlePlayerLeave(PlayerLeaveFromClient leave)
        {
            if (isInGame)
            {
                await gameServer.HandlePlayerLeave(this);
            }
            await DisconnectAsync();
        }

        public async Task DisconnectAsync()
        {
            if (!isConnected) return;

            isConnected = false;

            try
            {
                stream?.Close();
                tcpClient?.Close();
                gameServer.RemoveClient(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {Id}] 연결 해제 오류: {ex.Message}");
            }

            Console.WriteLine($"[클라이언트 {Id}] 연결 해제됨");
        }
    }
}