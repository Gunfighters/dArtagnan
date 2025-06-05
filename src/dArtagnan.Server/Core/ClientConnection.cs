using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    public class ClientConnection : IDisposable
    {
        private readonly TcpClient tcpClient;
        private readonly NetworkStream stream;
        private readonly GameServer gameServer;
        private bool isConnected;
        private bool isInGame;

        public int PlayerId { get; private set; }
        public string Nickname { get; private set; } = "";
        public PlayerInfo PlayerInfo { get; private set; }
        public bool IsInGame => isInGame;
        public bool IsConnected => isConnected && tcpClient.Connected;

        public ClientConnection(TcpClient client, GameServer server, int playerId)
        {
            tcpClient = client;
            stream = client.GetStream();
            gameServer = server;
            PlayerId = playerId;
            isConnected = true;
            isInGame = false;

            // 패킷 수신 루프 시작
            _ = Task.Run(ReceiveLoop);
        }

        public void SetPlayerInfo(string nickname)
        {
            Nickname = nickname;
            isInGame = true;
            
            PlayerInfo = new PlayerInfo
            {
                PlayerId = PlayerId,
                Nickname = Nickname,
                Accuracy = new Random().Next(50, 100)
            };
        }

        // 패킷 전송
        public async Task SendPacketAsync<T>(PacketType packetType, T data) where T : struct
        {
            if (!IsConnected) return;

            try
            {
                var packet = NetworkUtils.CreatePacket(packetType, data);
                Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 전송: {packetType} (데이터 크기: {packet.Data.Length} bytes)");
                await NetworkUtils.SendPacketAsync(stream, packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 전송 실패: {ex.Message}");
                await DisconnectAsync();
            }
        }

        public async Task SendPacketAsync(PacketType packetType)
        {
            if (!IsConnected) return;

            try
            {
                var packet = new Packet(packetType);
                Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 전송: {packetType} (데이터 크기: {packet.Data.Length} bytes)");
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
                    if (packet != null)
                    {
                        // 패킷 수신 로그 출력
                        Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 수신: {packet.Value.Type} (데이터 크기: {packet.Value.Data.Length} bytes)");
                        await HandlePacket(packet.Value);
                    }
                    else
                    {
                        break; // 연결 종료
                    }
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

        private async Task HandlePacket(Packet packet)
        {
            try
            {
                switch (packet.Type)
                {
                    case PacketType.PlayerJoin:
                        var joinData = NetworkUtils.GetData<PlayerJoinPacket>(packet);
                        await HandlePlayerJoin(joinData);
                        break;

                    case PacketType.PlayerMove:
                        var moveData = NetworkUtils.GetData<MovePacket>(packet);
                        await HandlePlayerMove(moveData);
                        break;

                    case PacketType.PlayerShoot:
                        var shootData = NetworkUtils.GetData<ShootPacket>(packet);
                        await HandlePlayerShoot(shootData);
                        break;

                    case PacketType.Disconnect:
                        await HandleDisconnect();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 패킷 처리 오류: {ex.Message}");
            }
        }

        private async Task HandlePlayerJoin(PlayerJoinPacket joinData)
        {
            await gameServer.JoinGame(this, joinData.Nickname);
        }

        private async Task HandlePlayerMove(MovePacket moveData)
        {
            if (IsInGame)
            {
                await gameServer.BroadcastToAll(PacketType.PlayerMoveResponse, moveData, PlayerId);
            }
        }

        private async Task HandlePlayerShoot(ShootPacket shootData)
        {
            if (IsInGame)
            {
                await gameServer.BroadcastToAll(PacketType.PlayerShootResponse, shootData, PlayerId);
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
                await gameServer.RemoveClient(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 {PlayerId}] 연결 해제 오류: {ex.Message}");
            }
            
            Console.WriteLine($"[클라이언트 {PlayerId}] 연결 해제됨");
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
        }
    }
} 