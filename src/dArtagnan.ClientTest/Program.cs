using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.ClientTest
{
    internal class Program
    {
        private static TcpClient? client;
        private static NetworkStream? stream;
        private static bool isConnected = false;
        private static bool isRunning = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== D'Artagnan 테스트 클라이언트 ===");
            Console.WriteLine("명령어:");
            Console.WriteLine("  connect [host] [port] - 서버 연결 (기본: localhost 7777)");
            Console.WriteLine("  join [nickname] - 게임 참가");
            Console.WriteLine("  dir [i] - 플레이어 이동 방향 변경");
            Console.WriteLine("  shoot [targetId] - 플레이어 공격");
            Console.WriteLine("  quit - 종료");
            Console.WriteLine("=====================================");

            var receiveTask = Task.Run(ReceiveLoop);

            while (isRunning)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;

                await ProcessCommand(input);
            }

            await receiveTask;
        }

        static async Task ProcessCommand(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var command = parts[0].ToLower();

            try
            {
                switch (command)
                {
                    case "connect":
                        var host = parts.Length > 1 ? parts[1] : "localhost";
                        var port = parts.Length > 2 ? int.Parse(parts[2]) : 7777;
                        await ConnectToServer(host, port);
                        break;

                    case "join":
                        var nickname = parts.Length > 1 ? parts[1] : "TestPlayer";
                        await JoinGame(nickname);
                        break;

                    case "dir":
                        if (parts.Length >= 2)
                        {
                            var i = int.Parse(parts[1]);
                            await SendDirection(i);
                        }
                        else
                        {
                            Console.WriteLine("사용법: dir [i]");
                        }

                        break;

                    case "shoot":
                        if (parts.Length >= 2)
                        {
                            var targetId = int.Parse(parts[1]);
                            await SendShoot(targetId);
                        }
                        else
                        {
                            Console.WriteLine("사용법: shoot [targetId]");
                        }

                        break;

                    case "quit":
                        await Disconnect();
                        isRunning = false;
                        break;

                    default:
                        Console.WriteLine("알 수 없는 명령어입니다.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"명령어 처리 오류: {ex.Message}");
            }
        }

        static async Task ConnectToServer(string host, int port)
        {
            try
            {
                if (isConnected)
                {
                    Console.WriteLine("이미 서버에 연결되어 있습니다.");
                    return;
                }

                client = new TcpClient();
                await client.ConnectAsync(host, port);
                stream = client.GetStream();
                isConnected = true;

                Console.WriteLine($"서버에 연결되었습니다: {host}:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
            }
        }

        static async Task JoinGame(string nickname)
        {
            if (!isConnected || stream == null)
            {
                Console.WriteLine("먼저 서버에 연결해주세요.");
                return;
            }

            try
            {
                var joinPacket = new JoinRequestFromClient();
                await NetworkUtils.SendPacketAsync(stream, joinPacket);
                Console.WriteLine($"게임 참가 요청을 보냈습니다: {nickname}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"게임 참가 실패: {ex.Message}");
            }
        }

        static async Task SendDirection(int direction)
        {
            if (!isConnected || stream == null)
            {
                Console.WriteLine("먼저 서버에 연결해주세요.");
                return;
            }

            try
            {
                var playerDirection = new PlayerDirectionFromClient() { direction = direction };
                await NetworkUtils.SendPacketAsync(stream, playerDirection);
                Console.WriteLine($"이동 패킷 전송: {direction}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"이동 패킷 전송 실패: {ex.Message}");
            }
        }

        static async Task SendShoot(int targetId)
        {
            if (!isConnected || stream == null)
            {
                Console.WriteLine("먼저 서버에 연결해주세요.");
                return;
            }

            try
            {
                await NetworkUtils.SendPacketAsync(stream, new PlayerShootingFromClient
                {
                    targetId = targetId
                });
                Console.WriteLine($"공격 패킷 전송: 타겟 {targetId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"공격 패킷 전송 실패: {ex.Message}");
            }
        }

        static async Task ReceiveLoop()
        {
            while (isRunning)
            {
                if (stream != null && isConnected)
                {
                    try
                    {
                        var packet = await NetworkUtils.ReceivePacketAsync(stream);
                        Console.WriteLine($"[수신] 패킷: {packet}");
                        await HandlePacket(packet);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("서버와의 연결이 끊어졌습니다.");
                        isConnected = false;
                        break;
                    }
                }
            }
        }

        static async Task HandlePacket(IPacket packet)
        {
            try
            {
                switch (packet)
                {
                    case JoinResponseFromServer joinResponse:
                        Console.WriteLine($"게임 참가 성공! 플레이어 ID: {joinResponse.playerId}, 명중률: {joinResponse.accuracy}");
                        break;
                    case PlayerDirectionFromServer playerDirection:
                        Console.WriteLine($"{playerDirection.playerId}번 플레이어 방향 변경: {playerDirection.direction}");
                        break;
                    case PlayerRunningFromServer running:
                        var msg = running.isRunning ? "달립니다" : "그만 달립니다";
                        Console.WriteLine($"{running.playerId}번 플레이어가 {msg}.");
                        break;
                    case InformationOfPlayers infoPlayers:
                        foreach (var info in infoPlayers.info)
                        {
                            Console.WriteLine($"{info.playerId}번 플레이어가 있습니다., 명중률: {info.accuracy}%");
                        }

                        break;
                    case PlayerShootingFromServer shooting:
                        Console.WriteLine($"플레이어 {shooting.playerId}가 플레이어 {shooting.targetId}를 공격했습니다!");
                        break;
                    default:
                        Console.WriteLine($"처리되지 않은 패킷 타입: {packet}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"패킷 처리 오류: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        static async Task Disconnect()
        {
            try
            {
                isConnected = false;
                stream?.Close();
                client?.Close();
                Console.WriteLine("서버와의 연결을 해제했습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 해제 오류: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}