using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using dArtagnan.Server.Game;
using dArtagnan.Server.Handlers;
using dArtagnan.Server.Network;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    /// <summary>
    /// TCP 서버와 클라이언트 연결 관리만 담당하는 클래스
    /// </summary>
    public class GameServer
    {
        // 클라이언트 관리 - ConcurrentDictionary는 스레드 안전함
        private readonly ConcurrentDictionary<int, ClientConnection> clients = new();
        private bool isRunning;
        private int nextClientId = 1;
        private TcpListener tcpListener = null!;
        private GameLoop gameLoop = null!;

        // 게임 세션과 핸들러들
        private GameSession gameSession = null!;
        private JoinHandler joinHandler = null!;
        private MovementHandler movementHandler = null!;
        private CombatHandler combatHandler = null!;
        private LeaveHandler leaveHandler = null!;

        public async Task StartAsync(int port)
        {
            try
            {
                // 게임 세션 초기화
                gameSession = new GameSession();

                // 핸들러들 초기화
                joinHandler = new JoinHandler(gameSession);
                movementHandler = new MovementHandler(gameSession);
                combatHandler = new CombatHandler(gameSession);
                leaveHandler = new LeaveHandler(gameSession);

                // TCP 리스너 시작
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                isRunning = true;

                Console.WriteLine($"D'Artagnan 게임 서버가 포트 {port}에서 시작되었습니다.");
                Console.WriteLine("클라이언트 연결을 기다리는 중...");

                // 게임 루프 초기화 및 시작
                gameLoop = new GameLoop(this, movementHandler, combatHandler);
                _ = Task.Run(() => gameLoop.StartAsync());

                // 클라이언트 연결 대기 루프
                while (isRunning)
                {
                    try
                    {
                        var tcpClient = await tcpListener.AcceptTcpClientAsync();
                        var client = new ClientConnection(
                            nextClientId++, 
                            tcpClient,
                            joinHandler,
                            movementHandler,
                            combatHandler,
                            leaveHandler,
                            BroadcastToAll,
                            BroadcastToAllExcept,
                            OnClientDisconnected
                        );
                        clients.TryAdd(client.Id, client);
                        Console.WriteLine($"새 클라이언트 연결됨 (ID: {client.Id})");
                    }
                    catch (ObjectDisposedException)
                    {
                        // 서버 종료 시 발생하는 예외, 무시
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"클라이언트 연결 수락 오류: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 시작 오류: {ex.Message}");
            }
        }



        /// <summary>
        /// 클라이언트 연결 해제 시 호출되는 콜백
        /// </summary>
        private async Task OnClientDisconnected(ClientConnection client)
        {
            await client.HandleDisconnect();
            // 클라이언트 제거
            clients.TryRemove(client.Id, out _);
            Console.WriteLine($"클라이언트 {client.Id} 제거됨 (현재 접속자: {clients.Count})");
        }

        /// <summary>
        /// 모든 플레이어에게 브로드캐스트
        /// </summary>
        public async Task BroadcastToAll(IPacket packet)
        {
            var tasks = new List<Task>();

            foreach (var client in clients.Values)
            {
                if (client.IsConnected)
                {
                    tasks.Add(client.SendPacketAsync(packet));
        }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// 특정 클라이언트를 제외하고 모든 플레이어에게 브로드캐스트
        /// </summary>
        public async Task BroadcastToAllExcept(IPacket packet, int excludeClientId)
        {
            var tasks = new List<Task>();

            foreach (var client in clients.Values)
            {
                if (client.Id != excludeClientId && client.IsConnected)
                {
                    tasks.Add(client.SendPacketAsync(packet));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// 서버 종료
        /// </summary>
        public async Task StopAsync()
        {
            Console.WriteLine("서버를 종료합니다...");
            isRunning = false;

            try
            {
                // 게임 루프 중지
                gameLoop?.Stop();

                // 모든 클라이언트 연결 해제
                var disconnectTasks = clients.Values.Select(client => client.DisconnectAsync());
                await Task.WhenAll(disconnectTasks);

                tcpListener?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 종료 오류: {ex.Message}");
            }

            Console.WriteLine("서버가 종료되었습니다.");
        }

        /// <summary>
        /// 현재 연결된 클라이언트 수 반환
        /// </summary>
        public int GetClientCount() => clients.Count;

        /// <summary>
        /// 게임 세션 반환 (CommandHandler에서 사용)
        /// </summary>
        public GameSession GetGameSession() => gameSession;

        /// <summary>
        /// 핸들러들 반환 (CommandHandler에서 사용)
        /// </summary>
        public MovementHandler GetMovementHandler() => movementHandler;
        public CombatHandler GetCombatHandler() => combatHandler;
    }
}