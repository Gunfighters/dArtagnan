using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    public class GameServer
    {
        private TcpListener tcpListener;
        private bool isRunning;
        private int nextPlayerId = 1;
        
        // 클라이언트 관리- ConcurrentDictionary는 스레드 안전함
        private readonly ConcurrentDictionary<int, ClientConnection> clients = new();

        public async Task StartAsync(int port)
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                isRunning = true;

                Console.WriteLine($"D'Artagnan 게임 서버가 포트 {port}에서 시작되었습니다.");
                Console.WriteLine("클라이언트 연결을 기다리는 중...");

                // 클라이언트 연결 대기 루프
                while (isRunning)
                {
                    try
                    {
                        var tcpClient = await tcpListener.AcceptTcpClientAsync();
                        var client = new ClientConnection(tcpClient, this, nextPlayerId++);
                        clients.TryAdd(client.PlayerId, client);
                        Console.WriteLine($"새 클라이언트 연결됨 (ID: {client.PlayerId})");
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
        public async Task JoinGame(ClientConnection client, string nickname)
        {
            // 플레이어 정보 설정
            client.SetPlayerInfo(nickname);
            
            // 최대 인원 체크 (8명)
            if (clients.Count > 8)
            {
                var failResponse = new PlayerJoinResponsePacket
                {
                    Success = false,
                    Message = "인원 초과",
                    PlayerInfo = default
                };
                await client.SendPacketAsync(PacketType.PlayerJoinResponse, failResponse);
                Console.WriteLine($"[게임] 플레이어 {client.PlayerId}({nickname}) 참가 실패 - 인원 초과");
                return;
            }
            
            Console.WriteLine($"[게임] 플레이어 {client.PlayerId}({nickname}) 참가 (현재 인원: {clients.Count})");

            // 성공 응답 전송
            var response = new PlayerJoinResponsePacket
            {
                Success = true,
                Message = "참가 성공",
                PlayerInfo = client.PlayerInfo
            };

            await client.SendPacketAsync(PacketType.PlayerJoinResponse, response);

            // 다른 플레이어들에게 새 플레이어 참가 알림
            await BroadcastToAll(PacketType.PlayerJoinResponse, new PlayerJoinResponsePacket
            {
                Success = true,
                Message = "새 플레이어 참가",
                PlayerInfo = client.PlayerInfo
            }, client.PlayerId);
        }

        // 플레이어 게임 퇴장 처리
        public async Task LeaveGame(ClientConnection client)
        {
            if (!client.IsInGame) return;

            Console.WriteLine($"[게임] 플레이어 {client.PlayerId}({client.Nickname}) 퇴장 (현재 인원: {clients.Count - 1})");

            // 다른 플레이어들에게 퇴장 알림
            await BroadcastToAll(PacketType.PlayerLeave, new PlayerJoinPacket
            {
                Nickname = client.Nickname
            }, client.PlayerId);
        }

        // 모든 플레이어에게 브로드캐스트 (본인 제외)
        public async Task BroadcastToAll<T>(PacketType packetType, T data, int excludePlayerId = -1) where T : struct
        {
            var tasks = new List<Task>();
            
            foreach (var client in clients.Values)
            {
                if (client.PlayerId != excludePlayerId && client.IsConnected && client.IsInGame)
                {
                    tasks.Add(client.SendPacketAsync(packetType, data));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        // 클라이언트 제거
        public async Task RemoveClient(ClientConnection client)
        {
            clients.TryRemove(client.PlayerId, out _);
            Console.WriteLine($"클라이언트 {client.PlayerId} 제거됨 (현재 접속자: {clients.Count})");
        }

        // 서버 종료
        public async Task StopAsync()
        {
            Console.WriteLine("서버를 종료합니다...");
            isRunning = false;

            try
            {
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

        // 서버 상태 출력
        public void PrintStatus()
        {
            Console.WriteLine($"=== 서버 상태 ===");
            Console.WriteLine($"접속 중인 클라이언트: {clients.Count}명");
            
            var gameClients = clients.Values.Where(c => c.IsInGame).ToList();
            Console.WriteLine($"게임 중인 플레이어: {gameClients.Count}명");
            
            foreach (var client in gameClients)
            {
                Console.WriteLine($"  플레이어 {client.PlayerId}: {client.Nickname}");
            }
            Console.WriteLine($"================");
        }
    }
} 