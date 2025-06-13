using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    public class GameServer
    {
        // 클라이언트 관리- ConcurrentDictionary는 스레드 안전함
        public readonly ConcurrentDictionary<int, ClientConnection> clients = new();
        private bool isRunning;
        private int nextPlayerId = 1;
        private TcpListener tcpListener = null!; // null!는 "나중에 초기화할 것"을 의미
        private GameLoop gameLoop = null!; // 게임 루프

        public async Task StartAsync(int port)
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                isRunning = true;

                Console.WriteLine($"D'Artagnan 게임 서버가 포트 {port}에서 시작되었습니다.");
                Console.WriteLine("클라이언트 연결을 기다리는 중...");

                // 게임 루프 초기화 및 시작
                gameLoop = new GameLoop(this);
                _ = Task.Run(() => gameLoop.StartAsync());

                // 클라이언트 연결 대기 루프
                while (isRunning)
                {
                    try
                    {
                        var tcpClient = await tcpListener.AcceptTcpClientAsync();
                        var client = new ClientConnection(nextPlayerId++, tcpClient, this);
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

        // 플레이어 게임 참가 처리
        public async Task JoinGame(ClientConnection client)
        {
            Console.WriteLine($"[게임] 플레이어 {client.Id} 참가 (현재 인원: {clients.Count})");
            client.SetPlayerInfo(client.Id, "sample_nickname");
            
            // YouAre 패킷 전송
            await client.SendPacketAsync(new YouAre
            {
                playerId = client.PlayerId
            });

            // PlayerJoinBroadcast 전송
            await BroadcastToAll(new PlayerJoinBroadcast
            {
                playerId = client.PlayerId,
                initX = (int)client.X,
                initY = (int)client.Y,
                accuracy = client.Accuracy
            });

            // 현재 모든 플레이어 정보 전송
            await SendPlayersInformation();
        }

        // 플레이어 피격 처리
        public async Task HandlePlayerHit(int targetPlayerId)
        {
            var targetClient = clients.Values.FirstOrDefault(c => c.PlayerId == targetPlayerId);
            if (targetClient != null && targetClient.Alive)
            {
                Console.WriteLine($"[게임] 플레이어 {targetPlayerId} 피격됨");
                
                // 플레이어 사망 처리
                targetClient.UpdateAlive(false);
                
                // 사망 브로드캐스트
                await BroadcastToAll(new UpdatePlayerAlive
                {
                    playerId = targetPlayerId,
                    alive = false
                });
            }
        }

        // 플레이어 게임 퇴장 처리
        public async Task HandlePlayerLeave(ClientConnection client)
        {
            if (!client.IsInGame) return;

            Console.WriteLine($"[게임] 플레이어 {client.PlayerId}({client.Nickname}) 퇴장 (현재 인원: {clients.Count - 1})");

            // 다른 플레이어들에게 퇴장 알림
            await BroadcastToAll(new PlayerLeaveBroadcast
            {
                playerId = client.PlayerId
            }, client.Id);
        }

        // 모든 플레이어 정보 전송
        public async Task SendPlayersInformation()
        {
            var playersInGame = clients.Values
                .Where(c => c.IsConnected && c.IsInGame)
                .ToList();

            if (playersInGame.Count == 0) return;

            var playerInfoList = playersInGame.Select(client => new PlayerInformation
            {
                playerId = client.PlayerId,
                nickname = client.Nickname,
                direction = client.Direction,
                x = client.X,
                y = client.Y,
                accuracy = client.Accuracy,
                totalReloadTime = client.TotalReloadTime,
                remainingReloadTime = client.RemainingReloadTime,
                speed = client.Speed,
                alive = client.Alive
            }).ToList();

            var packet = new InformationOfPlayers
            {
                info = playerInfoList
            };

            await BroadcastToAll(packet);
        }

        // 플레이어 위치 업데이트 브로드캐스트
        public async Task BroadcastPlayerPositions()
        {
            var playersInGame = clients.Values
                .Where(c => c.IsConnected && c.IsInGame)
                .ToList();

            if (playersInGame.Count == 0) return;

            var positionList = playersInGame.Select(client => new PlayerPosition
            {
                playerId = client.PlayerId,
                x = client.X,
                y = client.Y
            }).ToList();

            var packet = new UpdatePlayerPosition
            {
                positionList = positionList
            };

            await BroadcastToAll(packet);
        }

        // 모든 플레이어에게 브로드캐스트 (본인 제외)
        public async Task BroadcastToAll(IPacket data, int excludePlayerId = -1)
        {
            var tasks = new List<Task>();

            foreach (var client in clients.Values)
            {
                if (client.Id != excludePlayerId && client.IsConnected && client.IsInGame)
                {
                    tasks.Add(client.SendPacketAsync(data));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        // 클라이언트 제거
        public void RemoveClient(ClientConnection client)
        {
            clients.TryRemove(client.Id, out _);
            Console.WriteLine($"클라이언트 {client.Id} 제거됨 (현재 접속자: {clients.Count})");
        }

        // 서버 종료
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

                tcpListener.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 종료 오류: {ex.Message}");
            }

            Console.WriteLine("서버가 종료되었습니다.");
        }
    }
}