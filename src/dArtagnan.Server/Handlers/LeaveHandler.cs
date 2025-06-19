using dArtagnan.Server.Game;
using dArtagnan.Server.Network;
using dArtagnan.Shared;

namespace dArtagnan.Server.Handlers
{
    /// <summary>
    /// 플레이어 퇴장 처리를 담당하는 핸들러
    /// </summary>
    public class LeaveHandler
    {
        private readonly GameSession gameSession;

        public LeaveHandler(GameSession gameSession)
        {
            this.gameSession = gameSession;
        }

        /// <summary>
        /// 플레이어 퇴장 요청을 처리합니다
        /// </summary>
        public async Task HandlePlayerLeave(PlayerLeaveFromClient leaveData, ClientConnection client, Func<IPacket, int, Task> broadcastToAllExcept)
        {
            var player = gameSession.GetPlayerByClientId(client.Id);
            if (player == null || !player.IsInGame) return;

            Console.WriteLine($"[게임] 플레이어 {player.PlayerId}({player.Nickname}) 퇴장 요청");

            // 플레이어 게임 퇴장 처리
            gameSession.LeavePlayer(client.Id);

            Console.WriteLine($"[게임] 플레이어 {player.PlayerId} 퇴장 완료 (현재 인원: {gameSession.PlayerCount})");

            // 다른 플레이어들에게 퇴장 알림 (본인 제외)
            await broadcastToAllExcept(new PlayerLeaveBroadcast
            {
                playerId = player.PlayerId
            }, client.Id);

            // 게임 종료 조건 확인
            await CheckGameEnd();
        }

        /// <summary>
        /// 클라이언트 연결 해제 시 처리 (비정상 종료 포함)
        /// </summary>
        public async Task HandleClientDisconnect(ClientConnection client, Func<IPacket, int, Task> broadcastToAllExcept)
        {
            var player = gameSession.GetPlayerByClientId(client.Id);
            if (player == null) return;

            Console.WriteLine($"[연결] 클라이언트 {client.Id} 연결 해제");

            // 게임에 참여 중이었다면 퇴장 처리
            if (player.IsInGame)
            {
                // 플레이어 게임 퇴장 처리
                gameSession.LeavePlayer(client.Id);

                Console.WriteLine($"[게임] 플레이어 {player.PlayerId} 비정상 퇴장 (현재 인원: {gameSession.PlayerCount})");

                // 다른 플레이어들에게 퇴장 알림
                await broadcastToAllExcept(new PlayerLeaveBroadcast
                {
                    playerId = player.PlayerId
                }, client.Id);

                // 게임 종료 조건 확인
                await CheckGameEnd();
            }

            // 플레이어를 세션에서 완전히 제거
            gameSession.RemovePlayer(client.Id);
        }

        /// <summary>
        /// 게임 종료 조건을 확인합니다
        /// </summary>
        private Task CheckGameEnd()
        {
            int playerCount = gameSession.Players.Count();
            int aliveCount = gameSession.GetAlivePlayerCount();

            // 플레이어 수가 최소 인원 미만이면 게임 중단
            if (playerCount < GameRules.MIN_PLAYERS)
            {
                Console.WriteLine($"[게임] 인원 부족으로 게임 중단 (현재: {playerCount}명, 최소: {GameRules.MIN_PLAYERS}명)");
                // 게임 중단 처리 로직 추가 가능
                return Task.CompletedTask;
            }

            // 생존자가 1명 이하면 게임 종료
            if (GameRules.ShouldEndGame(aliveCount))
            {
                Console.WriteLine($"[게임] 게임 종료 - 생존자: {aliveCount}명");
                // 게임 종료 처리 로직 추가 가능
                return Task.CompletedTask;
            }

            Console.WriteLine($"[게임] 게임 계속 진행 - 참여자: {playerCount}명, 생존자: {aliveCount}명");
            return Task.CompletedTask;
        }
    }
} 