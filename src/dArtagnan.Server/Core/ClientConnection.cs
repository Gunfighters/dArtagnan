using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    public class ClientConnection : IDisposable
    {
        private readonly GameServer gameServer;
        private readonly NetworkStream stream;
        private readonly TcpClient tcpClient;
        private bool isConnected;
        private bool isInGame;

        public ClientConnection(TcpClient client, GameServer server, int playerId)
        {
            tcpClient = client;
            stream = client.GetStream();
            gameServer = server;
            PlayerId = playerId;
            isConnected = true;
            isInGame = true;

            // 패킷 수신 루프 시작
            _ = Task.Run(ReceiveLoop);
        }

        public int PlayerId { get; private set; }

        public string Nickname { get; private set; } = "";

        // public PlayerInfo PlayerInfo { get; private set; }
        public bool IsInGame => isInGame;
        public bool IsConnected => isConnected && tcpClient.Connected;

        public void Dispose()
        {
            _ = DisconnectAsync();
        }

        // public void SetPlayerInfo(string nickname)
        // {
        //     Nickname = nickname;
        //     isInGame = true;
        //     
        //     PlayerInfo = new PlayerInfo
        //     {
        //         PlayerId = PlayerId,
        //         Nickname = Nickname,
        //         Accuracy = new Random().Next(50, 100)
        //     };
        // }

        // 패킷 전송
        public async Task SendPacketAsync(IPacket packet)
        {
            if (!IsConnected) return;

            try
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 전송: {packet}");
                await NetworkUtils.SendPacketAsync(stream, packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 전송 실패: {ex.Message}");
                await DisconnectAsync();
            }
        }

        // 수신 루프
        private async Task ReceiveLoop()
        {
            try
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 연결됨");

                while (IsConnected)
                {
                    var packet = await NetworkUtils.ReceivePacketAsync(stream);
                    // 패킷 수신 로그 출력
                    Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 수신: {packet}");
                    await HandlePacket(packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 수신 루프 오류: {ex.Message}");
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
                default:
                    Console.WriteLine($"Unhandled packet: {packet}");
                    break;
            }
        }

        private async Task HandlePlayerJoin(JoinRequestFromClient joinData)
        {
            await gameServer.JoinGame(this);
        }

        private async Task HandlePlayerMove(PlayerDirectionFromClient moveData)
        {
            if (IsInGame)
            {
                await gameServer.BroadcastToAll(new PlayerDirectionFromServer
                {
                    direction = moveData.direction,
                    playerId = PlayerId
                });
            }
        }

        // private async Task HandlePlayerShoot(ShootPacket shootData)
        // {
        //     if (IsInGame)
        //     {
        //         await gameServer.BroadcastToAll(PacketType.PlayerShootResponse, shootData, PlayerId);
        //     }
        // }

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
                await gameServer.RemoveClient(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 연결 해제 오류: {ex.Message}");
            }

            Console.WriteLine($"[클라이언트 {PlayerId}] 연결 해제됨");
        }
    }
}