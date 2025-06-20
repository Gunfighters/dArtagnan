using dArtagnan.Server.Game;
using dArtagnan.Server.Network;
using dArtagnan.Shared;

namespace dArtagnan.Server.Handlers
{
    /// <summary>
    /// 플레이어 게임 참가 처리를 담당하는 핸들러
    /// </summary>
    public class JoinHandler
    {
        private readonly GameSession gameSession;

        public JoinHandler(GameSession gameSession)
        {
            this.gameSession = gameSession;
        }

        /// <summary>
        /// 플레이어 참가 요청을 처리합니다
        /// </summary>
        public async Task HandlePlayerJoin(PlayerJoinRequest request, ClientConnection client, Func<IPacket, Task> broadcastToAll)
        {
            Console.WriteLine($"[게임] 플레이어 {client.Id} 참가 요청");

            // 플레이어가 이미 존재하는지 확인
            var existingPlayer = gameSession.GetPlayerByClientId(client.Id);
            Player player;

            if (existingPlayer == null)
            {
                // 새 플레이어 생성
                player = gameSession.AddPlayer(client.Id, "sample_nickname");
                Console.WriteLine($"[게임] 새 플레이어 생성: {player.PlayerId}");
            }
            else
            {
                player = existingPlayer;
                Console.WriteLine($"[게임] 기존 플레이어 재참가: {player.PlayerId}");
            }

            // 플레이어 게임 참가 처리
            gameSession.JoinPlayer(client.Id);

            // 스폰 위치 설정
            var (spawnX, spawnY) = Player.GetSpawnPosition(player.PlayerId);
            player.UpdatePosition(spawnX, spawnY);

            Console.WriteLine($"[게임] 플레이어 {player.PlayerId} 참가 완료 (현재 인원: {gameSession.PlayerCount})");

            // YouAre 패킷 전송 (본인에게만)
            await client.SendPacketAsync(new YouAre
            {
                playerId = player.PlayerId
            });

            // PlayerJoinBroadcast 전송 (모든 플레이어에게)
            await broadcastToAll(new PlayerJoinBroadcast
            {
                playerId = player.PlayerId,
                initX = (int)player.X,
                initY = (int)player.Y,
                accuracy = player.Accuracy
            });

            // 현재 모든 플레이어 정보 전송 (모든 플레이어에게)
            await SendPlayersInformation(broadcastToAll);
        }

        /// <summary>
        /// 현재 게임에 참여 중인 모든 플레이어 정보를 전송합니다
        /// </summary>
        private async Task SendPlayersInformation(Func<IPacket, Task> broadcastToAll)
        {
            var playerInfoList = gameSession.GetPlayersInformation();

            if (playerInfoList.Count == 0) return;

            var packet = new InformationOfPlayers
            {
                info = playerInfoList
            };

            await broadcastToAll(packet);
        }
    }
} 