using dArtagnan.Server.Game;
using dArtagnan.Server.Network;
using dArtagnan.Shared;

namespace dArtagnan.Server.Handlers
{
    /// <summary>
    /// 플레이어 전투(사격) 처리를 담당하는 핸들러
    /// </summary>
    public class CombatHandler
    {
        private readonly GameSession gameSession;

        public CombatHandler(GameSession gameSession)
        {
            this.gameSession = gameSession;
        }

        /// <summary>
        /// 플레이어 사격을 처리합니다
        /// </summary>
        public async Task HandlePlayerShooting(PlayerShootingFromClient shootingData, ClientConnection client, Func<IPacket, Task> broadcastToAll)
        {
            var shooter = gameSession.GetPlayerByClientId(client.Id);
            if (shooter == null) return;

            // 사격 가능한지 확인
            if (!GameRules.CanShoot(shooter))
            {
                Console.WriteLine($"[전투] 플레이어 {shooter.PlayerId} 사격 불가 (재장전 중 또는 사망)");
                return;
            }

            // 타겟 플레이어 확인
            var target = gameSession.GetPlayerByPlayerId(shootingData.targetId);
            if (target == null || !target.Alive)
            {
                Console.WriteLine($"[전투] 유효하지 않은 타겟: {shootingData.targetId}");
                return;
            }

            // 명중 여부 계산
            bool hit = GameRules.CalculateHit(shooter.Accuracy);
            
            // 재장전 시간 설정
            shooter.UpdateReloadTime(GameRules.DEFAULT_RELOAD_TIME);

            Console.WriteLine($"[전투] 플레이어 {shooter.PlayerId} -> {target.PlayerId} 사격: {(hit ? "명중" : "빗나감")}");

            // 사격 브로드캐스트
            await broadcastToAll(new PlayerShootingBroadcast
            {
                shooterId = shooter.PlayerId,
                targetId = target.PlayerId,
                hit = hit
            });

            // 명중 시 타겟 처리
            if (hit)
            {
                await HandlePlayerHit(target, broadcastToAll);
            }
        }

        /// <summary>
        /// 플레이어 피격을 처리합니다
        /// </summary>
        private async Task HandlePlayerHit(Player target, Func<IPacket, Task> broadcastToAll)
        {
            if (!target.Alive) return;

            Console.WriteLine($"[전투] 플레이어 {target.PlayerId} 사망");

            // 플레이어 사망 처리
            target.UpdateAlive(false);

            // 사망 브로드캐스트
            await broadcastToAll(new UpdatePlayerAlive
            {
                playerId = target.PlayerId,
                alive = false
            });

            // 게임 종료 체크
            await CheckGameEnd(broadcastToAll);
        }

        /// <summary>
        /// 게임 종료 조건을 확인합니다
        /// </summary>
        private Task CheckGameEnd(Func<IPacket, Task> broadcastToAll)
        {
            int aliveCount = gameSession.GetAlivePlayerCount();
            
            if (GameRules.ShouldEndGame(aliveCount))
            {
                Console.WriteLine($"[게임] 게임 종료 - 생존자: {aliveCount}명");
                
                // 게임 종료 처리 로직 추가 가능
                // 예: 승자 발표, 게임 리셋 등
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 게임 루프에서 호출되는 재장전 시간 업데이트
        /// </summary>
        public void UpdateReloadTimes(float deltaTime)
        {
            foreach (var player in gameSession.Players)
            {
                if (!player.Alive) continue;

                if (player.RemainingReloadTime > 0)
                {
                    float newReloadTime = GameRules.UpdateReloadTime(player.RemainingReloadTime, deltaTime);
                    player.UpdateReloadTime(newReloadTime);
                }
            }
        }
    }
} 