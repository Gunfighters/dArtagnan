using dArtagnan.Shared;

namespace dArtagnan.Server
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
            }

            // 플레이어를 세션에서 완전히 제거
            gameSession.RemovePlayer(client.Id);
        }
    }
} 