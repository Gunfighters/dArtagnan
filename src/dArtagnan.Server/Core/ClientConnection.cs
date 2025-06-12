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
        public bool IsRunning { get; private set; }

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
            IsRunning = false;
            X = 0;
            Y = 0;
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
                await DisconnectAsync();
            }
        }

        private async Task HandlePacket(IPacket packet)
        {
            switch (packet)
            {
                case JoinRequestFromClient request:
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
                default:
                    Console.WriteLine($"Unhandled packet: {packet}");
                    break;
            }
        }

        private async Task HandlePlayerRunning(PlayerRunningFromClient running)
        {
            IsRunning = running.isRunning;
            await gameServer.BroadcastToAll(new PlayerRunningFromServer
                { isRunning = IsRunning, playerId = PlayerId });
        }

        private async Task HandlePlayerJoin(JoinRequestFromClient joinData)
        {
            isInGame = true;
            await gameServer.JoinGame(this);
        }

        private async Task HandlePlayerMove(PlayerDirectionFromClient moveData)
        {
            if (IsInGame)
            {
                Direction = moveData.direction;
                await gameServer.BroadcastToAll(new PlayerDirectionFromServer
                {
                    direction = Direction,
                    playerId = PlayerId
                });
            }
        }

        private async Task HandlePlayerShooting(PlayerShootingFromClient shooting)
        {
            if (IsInGame)
            {
                await gameServer.BroadcastToAll(new PlayerShootingFromServer
                {
                    playerId = PlayerId,
                    targetId = shooting.targetId
                });
            }
        }

        private async Task HandleDisconnect()
        {
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