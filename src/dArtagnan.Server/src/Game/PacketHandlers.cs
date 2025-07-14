using dArtagnan.Shared;

namespace dArtagnan.Server;

public static class PacketHandlers
{

    public static async Task HandlePing(PingPacket ping, ClientConnection client, GameManager gameManager)
    {
        await client.SendPacketAsync(new PongPacket());
    }

    public static async Task HandleStartGame(StartGameFromClient startGame, ClientConnection client, GameManager gameManager)
    {
        var starter = gameManager.Players[client.Id];
        if (starter != gameManager.Host)
        {
            Console.WriteLine($"[게임] 경고: 방장이 아닌 플레이어가 게임 시작 시도 (Player #{starter.Id})");
            return;
        }

        if (gameManager.CurrentGameState == GameState.Playing)
        {
            Console.WriteLine($"[게임] 경고: 이미 게임이 진행중.");
            return;
        }
        foreach (var p in gameManager.Players.Values)
        {
            p.ResetForNextRound();
        }

        await gameManager.StartGame();
    }

    public static async Task HandlePlayerJoin(PlayerJoinRequest request, ClientConnection client, GameManager gameManager)
    {
        if (gameManager.IsGamePlaying())
        {
            Console.WriteLine($"[게임] {client.Id}번 클라이언트 난입 거부.");
            return;
        }
        Console.WriteLine($"[게임] {client.Id}번 클라이언트 참가 요청");

        // 플레이어가 이미 존재하는지 확인
        var existingPlayer = gameManager.GetPlayerById(client.Id);
        Player player;

        if (existingPlayer == null)
        {
            // 새 플레이어 생성
            player = await gameManager.AddPlayer(client.Id, $"Player #{client.Id}");
            Console.WriteLine($"[게임] 새 플레이어 생성: {player.Id}");
        }
        else
        {
            throw new Exception($"제거되지 않은 플레이어: {client.Id}");
        }

        Console.WriteLine($"[게임] 플레이어 {player.Id} 참가 완료 (현재 인원: {gameManager.Players.Count})");

        await client.SendPacketAsync(new YouAre
        {
            PlayerId = player.Id
        });

        GameInWaitingFromServer waiting = new() { PlayersInfo = gameManager.PlayersInRoom() };
        foreach (var p in waiting.PlayersInfo)
        {
            Console.WriteLine($"{p.PlayerId}: {p.MovementData.Position}");
        }

        await client.SendPacketAsync(waiting);

        await gameManager.BroadcastToAll(new PlayerJoinBroadcast { PlayerInfo = player.PlayerInformation });
    }

    public static async Task HandlePlayerLeave(PlayerLeaveFromClient leaveData, ClientConnection client, GameManager gameManager)
    {
        Console.WriteLine($"[게임] 클라이언트 {client.Id} 퇴장 요청 수신");
            
        // GameManager에서 모든 정리 작업 처리
        await gameManager.RemoveClient(client.Id);
            
        // 연결 종료
        _ = Task.Run(() => client.DisconnectAsync());
    }

    public static async Task HandlePlayerMovementData(PlayerMovementDataFromClient movementData, ClientConnection client, GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(client.Id);
        if (player == null) return;
        var directionIndex = movementData.Direction;
        var directionVector = DirectionHelper.IntToDirection(directionIndex);
        var newSpeed = movementData.MovementData.Speed;
        var newPosition = movementData.MovementData.Position;
        player.UpdateMovementData(newPosition, directionIndex, newSpeed);
        Console.WriteLine($"[이동] 플레이어 {player.Id} 방향: {directionVector}, 위치: ({player.MovementData.Position}) 속도: ({player.MovementData.Speed:F2})");

        // 방향 변경을 모든 플레이어에게 브로드캐스트
        await gameManager.BroadcastToAll(new PlayerMovementDataBroadcast
        {
            PlayerId = player.Id,
            MovementData = player.MovementData,
            Running = movementData.Running,
        });
    }

    public static async Task HandlePlayerShooting(PlayerShootingFromClient shootingData, ClientConnection client, GameManager gameManager)
    {
        var shooter = gameManager.GetPlayerById(client.Id);
        if (shooter == null) return;

        // 사격 가능한지 확인
        if (!CanShoot(shooter))
        {
            Console.WriteLine($"[전투] 플레이어 {shooter.Id} 사격 불가 (재장전 중 또는 사망)");
            return;
        }

        // 타겟 플레이어 확인
        var target = gameManager.GetPlayerById(shootingData.TargetId);
        if (target == null || !target.Alive)
        {
            Console.WriteLine($"[전투] 유효하지 않은 타겟: {shootingData.TargetId}");
            return;
        }

        // 명중 여부 계산
        bool hit = CalculateHit(shooter.Accuracy);
            
        // 재장전 시간 설정
        shooter.UpdateReloadTime(shooter.TotalReloadTime);

        Console.WriteLine($"[전투] 플레이어 {shooter.Id} -> {target.Id} 사격: {(hit ? "명중" : "빗나감")}");

        // 사격 브로드캐스트
        await gameManager.BroadcastToAll(new PlayerShootingBroadcast
        {
            ShooterId = shooter.Id,
            TargetId = target.Id,
            Hit = hit,
            ShooterRemainingReloadingTime = shooter.RemainingReloadTime
        });

        // 명중 시 타겟 처리
        if (hit)
        {
            // 게임이 진행 중일 때만 실제 피해 처리
            if (gameManager.IsGamePlaying())
            {
                shooter.Balance += target.Withdraw(Math.Max(target.Balance / 10, 50));
                await gameManager.BroadcastToAll(new PlayerBalanceUpdateBroadcast
                    { Balance = shooter.Balance, PlayerId = shooter.Id });
                await gameManager.BroadcastToAll(new PlayerBalanceUpdateBroadcast
                    { Balance = target.Balance, PlayerId = target.Id });
                await KillPlayer(target, gameManager);
            }
            else
            {
                Console.WriteLine($"[전투] 게임이 진행 중이 아니므로 사격 피해 무시 (상태: {gameManager.CurrentGameState})");
            }
        }
    }

    public static async Task HandlePlayerIsTargeting(
        PlayerIsTargetingFromClient isTargetingData,
        ClientConnection client,
        GameManager gameManager)
    {
        await gameManager.BroadcastToAll(new PlayerIsTargetingBroadcast
            { ShooterId = client.Id, TargetId = isTargetingData.TargetId });
    }

    private static bool CanShoot(Player player)
    {
        return player.Alive && player.RemainingReloadTime <= 0;
    }

    private static bool CalculateHit(float accuracy)
    {
        var random = new Random();
        return random.NextDouble() * 100 < accuracy;
    }

    public static async Task KillPlayer(Player player, GameManager gameManager)
    {
        Console.WriteLine($"[전투] 플레이어 {player.Id} 사망");
        player.UpdateAlive(false);
        await gameManager.BroadcastToAll(new UpdatePlayerAlive
        {
            PlayerId = player.Id,
            Alive = player.Alive
        });
        if (gameManager.RoundOver())
        {
            await gameManager.ProcessRoundOver();
        }
    }
}