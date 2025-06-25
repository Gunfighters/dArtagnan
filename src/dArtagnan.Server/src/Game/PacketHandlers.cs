using dArtagnan.Shared;

namespace dArtagnan.Server
{
    /// <summary>
    /// 모든 패킷 처리를 담당하는 static 메서드들
    /// </summary>
    public static class PacketHandlers
    {
        /// <summary>
        /// 플레이어 참가 요청을 처리합니다
        /// </summary>
        public static async Task HandlePlayerJoin(PlayerJoinRequest request, ClientConnection client, GameManager gameManager)
        {
            Console.WriteLine($"[게임] 플레이어 {client.Id} 참가 요청");

            // 플레이어가 이미 존재하는지 확인
            var existingPlayer = gameManager.GetPlayerByClientId(client.Id);
            Player player;

            if (existingPlayer == null)
            {
                // 새 플레이어 생성
                player = gameManager.AddPlayer(client.Id, "sample_nickname");
                Console.WriteLine($"[게임] 새 플레이어 생성: {player.PlayerId}");
            }
            else
            {
                player = existingPlayer;
                Console.WriteLine($"[게임] 기존 플레이어 재참가: {player.PlayerId}");
            }

            // 플레이어 게임 참가 처리
            gameManager.JoinPlayer(client.Id);

            // 스폰 위치 설정
            var (spawnX, spawnY) = Player.GetSpawnPosition(player.PlayerId);
            player.UpdatePosition(spawnX, spawnY);

            Console.WriteLine($"[게임] 플레이어 {player.PlayerId} 참가 완료 (현재 인원: {gameManager.PlayerCount})");

            // YouAre 패킷 전송 (본인에게만)
            await client.SendPacketAsync(new YouAre
            {
                playerId = player.PlayerId
            });

            // InformationOfPlayers 패킷 전송 (본인에게만)
            await client.SendPacketAsync(new InformationOfPlayers
            {
                info = gameManager.GetPlayersInformation()
            });

            // PlayerJoinBroadcast 전송 (모든 플레이어에게)
            await gameManager.BroadcastToAll(new PlayerJoinBroadcast
            {
                playerId = player.PlayerId,
                initX = (int)player.X,
                initY = (int)player.Y,
                accuracy = player.Accuracy
            });
        }

        /// <summary>
        /// 플레이어 퇴장 요청을 처리합니다
        /// </summary>
        public static async Task HandlePlayerLeave(PlayerLeaveFromClient leaveData, ClientConnection client, GameManager gameManager)
        {
            Console.WriteLine($"[게임] 클라이언트 {client.Id} 퇴장 요청 수신");
            
            // GameManager에서 모든 정리 작업 처리
            await gameManager.RemoveClient(client.Id);
            
            // 연결 종료
            _ = Task.Run(() => client.DisconnectAsync());
        }

        /// <summary>
        /// 플레이어 방향 변경을 처리합니다
        /// </summary>
        public static async Task HandlePlayerDirection(PlayerDirectionFromClient directionData, ClientConnection client, GameManager gameManager)
        {
            var player = gameManager.GetPlayerByClientId(client.Id);
            if (player == null || !player.IsInGame) return;

            // 플레이어 상태 업데이트
            player.Direction = directionData.direction;
            var directionVector = DirectionHelper.IntToDirection(directionData.direction);
            player.UpdateSpeed(Player.GetSpeedByRunning(directionData.running));
            var currentX = directionData.currentX + player.Speed * directionVector.X * gameManager.ping[client.Id] / 2;
            var currentY = directionData.currentY + player.Speed * directionVector.Y * gameManager.ping[client.Id] / 2;
            player.UpdatePosition(currentX, currentY);

            Console.WriteLine($"[이동] 플레이어 {player.PlayerId} 방향: {directionVector}, 위치: ({player.X:F2}, {player.Y:F2})");

            // 방향 변경을 모든 플레이어에게 브로드캐스트
            await gameManager.BroadcastToAll(new PlayerDirectionBroadcast
            {
                direction = player.Direction,
                playerId = player.PlayerId,
                currentX = player.X,
                currentY = player.Y,
                speed = player.Speed
            });
        }

        /// <summary>
        /// 플레이어 달리기 상태 변경을 처리합니다
        /// </summary>
        public static async Task HandlePlayerRunning(PlayerRunningFromClient runningData, ClientConnection client, GameManager gameManager)
        {
            var player = gameManager.GetPlayerByClientId(client.Id);
            if (player == null || !player.IsInGame) return;

            // 달리기 상태에 따라 속도 설정
            float newSpeed = Player.GetSpeedByRunning(runningData.isRunning);
            var RTT = gameManager.ping[client.Id];
            var currentX = player.X - RTT / 2 * player.Speed * player.Direction + RTT / 2 * newSpeed * player.Direction;
            var currentY = player.Y - RTT / 2 * player.Speed * player.Direction + RTT / 2 * newSpeed * player.Direction;
            player.UpdateSpeed(newSpeed);
            player.UpdatePosition(currentX, currentY);
            
            Console.WriteLine($"[이동] 플레이어 {player.PlayerId} 달리기: {runningData.isRunning}, 속도: {player.Speed}");

            // 속도 업데이트를 모든 플레이어에게 브로드캐스트
            await gameManager.BroadcastToAll(new UpdatePlayerSpeedBroadcast
            {
                playerId = player.PlayerId,
                speed = player.Speed,
            });
        }

        /// <summary>
        /// 플레이어 사격을 처리합니다
        /// </summary>
        public static async Task HandlePlayerShooting(PlayerShootingFromClient shootingData, ClientConnection client, GameManager gameManager)
        {
            var shooter = gameManager.GetPlayerByClientId(client.Id);
            if (shooter == null) return;

            // 사격 가능한지 확인
            if (!CanShoot(shooter))
            {
                Console.WriteLine($"[전투] 플레이어 {shooter.PlayerId} 사격 불가 (재장전 중 또는 사망)");
                return;
            }

            // 타겟 플레이어 확인
            var target = gameManager.GetPlayerByPlayerId(shootingData.targetId);
            if (target == null || !target.Alive)
            {
                Console.WriteLine($"[전투] 유효하지 않은 타겟: {shootingData.targetId}");
                return;
            }

            // 명중 여부 계산
            bool hit = CalculateHit(shooter.Accuracy);
            
            // 재장전 시간 설정
            shooter.UpdateReloadTime(Player.DEFAULT_RELOAD_TIME);

            Console.WriteLine($"[전투] 플레이어 {shooter.PlayerId} -> {target.PlayerId} 사격: {(hit ? "명중" : "빗나감")}");

            // 사격 브로드캐스트
            await gameManager.BroadcastToAll(new PlayerShootingBroadcast
            {
                shooterId = shooter.PlayerId,
                targetId = target.PlayerId,
                hit = hit
            });

            // 명중 시 타겟 처리
            if (hit)
            {
                // 게임이 진행 중일 때만 실제 피해 처리
                if (gameManager.IsGamePlaying())
                {
                    await HandlePlayerHit(target, gameManager);
                }
                else
                {
                    Console.WriteLine($"[전투] 게임이 진행 중이 아니므로 사격 피해 무시 (상태: {gameManager.CurrentGameState})");
                }
            }
        }

        /// <summary>
        /// 플레이어 Ready 상태 변경을 처리합니다
        /// </summary>
        public static async Task HandleReady(Ready readyData, ClientConnection client, GameManager gameManager)
        {
            var player = gameManager.GetPlayerByClientId(client.Id);
            if (player == null || !player.IsInGame) return;

            // 플레이어 Ready 상태 업데이트
            gameManager.UpdatePlayerReady(client.Id, readyData.ready);

            Console.WriteLine($"[게임] 플레이어 {player.PlayerId} Ready 상태: {readyData.ready} (Ready 플레이어: {gameManager.GetReadyPlayerCount()}/{gameManager.PlayerCount})");

            // Ready 상태 변경을 모든 플레이어에게 브로드캐스트
            await gameManager.BroadcastToAll(new ReadyBroadcast
            {
                playerId = player.PlayerId,
                ready = readyData.ready
            });

            // 모든 플레이어가 Ready 상태이고 최소 2명 이상인지 확인
            if (gameManager.AreAllPlayersReady() && gameManager.PlayerCount >= 2)
            {
                Console.WriteLine($"[게임] 모든 플레이어가 Ready! 게임을 시작합니다.");
                await gameManager.StartGame();
            }
        }

        /// <summary>
        /// 플레이어가 사격 가능한지 확인합니다
        /// </summary>
        private static bool CanShoot(Player player)
        {
            return player.Alive && player.RemainingReloadTime <= 0;
        }

        /// <summary>
        /// 명중률에 따라 명중 여부를 계산합니다
        /// </summary>
        private static bool CalculateHit(float accuracy)
        {
            var random = new Random();
            return random.NextDouble() * 100 < accuracy;
        }

        /// <summary>
        /// 플레이어 피격 처리
        /// </summary>
        private static async Task HandlePlayerHit(Player target, GameManager gameManager)
        {
            // 플레이어 사망 처리
            target.UpdateAlive(false);
            
            Console.WriteLine($"[전투] 플레이어 {target.PlayerId} 사망");

            // 사망 브로드캐스트
            await gameManager.BroadcastToAll(new UpdatePlayerAlive
            {
                playerId = target.PlayerId,
                alive = false
            });

            // 게임 종료 상태 로그
            int aliveCount = gameManager.GetAlivePlayerCount();
            if (gameManager.ShouldEndGame())
            {
                Console.WriteLine($"[게임] 게임 종료 조건 달성 - 생존자: {aliveCount}명");
            }
        }
    }
} 