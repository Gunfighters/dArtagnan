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
            Console.WriteLine("  move [x] [y] - 플레이어 이동");
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

                    case "move":
                        if (parts.Length >= 3)
                        {
                            var x = float.Parse(parts[1]);
                            var y = float.Parse(parts[2]);
                            await SendMove(x, y);
                        }
                        else
                        {
                            Console.WriteLine("사용법: move [x] [y]");
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
                var joinPacket = NetworkUtils.CreatePacket(PacketType.PlayerJoin, new PlayerJoinPacket
                {
                    Nickname = nickname
                });

                Console.WriteLine($"[전송] 패킷: {PacketType.PlayerJoin} (데이터 크기: {joinPacket.Data.Length} bytes)");
                await NetworkUtils.SendPacketAsync(stream, joinPacket);
                Console.WriteLine($"게임 참가 요청을 보냈습니다: {nickname}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"게임 참가 실패: {ex.Message}");
            }
        }

        static async Task SendMove(float x, float y)
        {
            if (!isConnected || stream == null)
            {
                Console.WriteLine("먼저 서버에 연결해주세요.");
                return;
            }

            try
            {
                var movePacket = NetworkUtils.CreatePacket(PacketType.PlayerMove, new MovePacket
                {
                    PlayerId = 0, // 서버에서 설정됨
                    X = x,
                    Y = y
                });

                Console.WriteLine($"[전송] 패킷: {PacketType.PlayerMove} (데이터 크기: {movePacket.Data.Length} bytes)");
                await NetworkUtils.SendPacketAsync(stream, movePacket);
                Console.WriteLine($"이동 패킷 전송: ({x}, {y})");
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
                var shootPacket = NetworkUtils.CreatePacket(PacketType.PlayerShoot, new ShootPacket
                {
                    PlayerId = 0, // 서버에서 설정됨
                    TargetPlayerId = targetId
                });

                Console.WriteLine($"[전송] 패킷: {PacketType.PlayerShoot} (데이터 크기: {shootPacket.Data.Length} bytes)");
                await NetworkUtils.SendPacketAsync(stream, shootPacket);
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
                try
                {
                    if (stream != null && isConnected)
                    {
                        var packet = await NetworkUtils.ReceivePacketAsync(stream);
                        if (packet != null)
                        {
                            Console.WriteLine($"[수신] 패킷: {packet.Value.Type} (데이터 크기: {packet.Value.Data.Length} bytes)");
                            await HandlePacket(packet.Value);
                        }
                        else
                        {
                            Console.WriteLine("서버와의 연결이 끊어졌습니다.");
                            isConnected = false;
                            break;
                        }
                    }
                    else
                    {
                        await Task.Delay(100); // 연결 대기
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"패킷 수신 오류: {ex.Message}");
                    isConnected = false;
                    break;
                }
            }
        }

        static async Task HandlePacket(Packet packet)
        {
            try
            {
                switch (packet.Type)
                {
                    case PacketType.PlayerJoinResponse:
                        var joinResponse = NetworkUtils.GetData<PlayerJoinResponsePacket>(packet);
                        if (joinResponse.Success)
                        {
                            Console.WriteLine($"게임 참가 성공! 플레이어 ID: {joinResponse.PlayerInfo.PlayerId}, 닉네임: {joinResponse.PlayerInfo.Nickname}");
                        }
                        else
                        {
                            Console.WriteLine($"게임 참가 실패: {joinResponse.Message}");
                        }
                        break;

                    case PacketType.PlayerMoveResponse:
                        var moveResponse = NetworkUtils.GetData<MovePacket>(packet);
                        Console.WriteLine($"플레이어 {moveResponse.PlayerId} 이동: ({moveResponse.X}, {moveResponse.Y})");
                        break;

                    case PacketType.PlayerShootResponse:
                        var shootResponse = NetworkUtils.GetData<ShootPacket>(packet);
                        Console.WriteLine($"플레이어 {shootResponse.PlayerId}가 플레이어 {shootResponse.TargetPlayerId}를 공격했습니다!");
                        break;

                    default:
                        Console.WriteLine($"처리되지 않은 패킷 타입: {packet.Type}");
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
