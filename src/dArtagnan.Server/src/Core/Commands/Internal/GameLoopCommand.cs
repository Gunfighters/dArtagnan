using System.Numerics;
using System.Threading.Tasks;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 새로운 게임 루프 명령 - 베팅금 차감, 정확도 업데이트, 플레이어 상태 관리
/// </summary>
public class GameLoopCommand : IGameCommand
{
    required public float DeltaTime;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        switch (gameManager.CurrentGameState)
        {
            case GameState.Waiting:
                // 대기 상태: 빈 서버 타이머 체크 + 플레이어 상태 업데이트
                await CheckEmptyServerTimeout(gameManager);
                await UpdateByAccuracyState(gameManager, DeltaTime);
                await UpdatePlayerCreatingStates(gameManager, DeltaTime);
                SimulatePlayerPosition(gameManager, DeltaTime);
                UpdatePlayerEnergyStates(gameManager, DeltaTime);
                await UpdatePlayerBuffStates(gameManager, DeltaTime);
                break;

            case GameState.Round:
                // 라운드 상태: 베팅금 차감 + 모든 플레이어 상태 업데이트 + 봇 AI 업데이트
                await UpdateBettingTimer(gameManager);
                await UpdateByAccuracyState(gameManager, DeltaTime);
                await UpdatePlayerCreatingStates(gameManager, DeltaTime);
                SimulatePlayerPosition(gameManager, DeltaTime);
                UpdatePlayerEnergyStates(gameManager, DeltaTime);
                await UpdatePlayerBuffStates(gameManager, DeltaTime);
                await UpdateBotAI(gameManager, DeltaTime);
                break;

            case GameState.Showdown:
                // 쇼다운 상태: 3초 타이머 카운트다운
                await UpdateShowdownTimer(gameManager);
                break;
                
            case GameState.Augment:
                break;
        }
    }

    /// <summary>
    /// 대기 상태에서 빈 서버 타이머를 체크하고 타임아웃 시 서버 종료
    /// </summary>
    private async Task CheckEmptyServerTimeout(GameManager gameManager)
    {
        //개발 모드일 때는 자동 종료 x
        if (Program.DEV_MODE)
        {
            return;
        }

        var realPlayers = gameManager.Players.Values.Where(p => p is not Bot).ToList();
        
        if (realPlayers.Count == 0)
        {
            // 실제 플레이어가 없을 때 타이머 증가
            gameManager.emptyServerTimer += DeltaTime;
            
            // 첫 시작 시에만 로그 출력 (1초마다가 아니라)
            if (gameManager.emptyServerTimer <= DeltaTime) // 첫 프레임
            {
                Console.WriteLine($"[서버] 대기 상태에서 플레이어가 없음 - {GameManager.EMPTY_SERVER_TIMEOUT}초 후 서버 종료 예정");
            }
            
            // 타임아웃 체크
            if (gameManager.emptyServerTimer >= GameManager.EMPTY_SERVER_TIMEOUT)
            {
                Console.WriteLine($"[서버] {GameManager.EMPTY_SERVER_TIMEOUT}초 타임아웃 - 서버 종료");
                Environment.Exit(0);
            }
        }
        else
        {
            // 실제 플레이어가 있으면 타이머 리셋
            if (gameManager.emptyServerTimer > 0f)
            {
                Console.WriteLine("[서버] 플레이어 접속으로 종료 타이머 취소");
                gameManager.emptyServerTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 라운드 상태에서 베팅금 타이머 업데이트
    /// </summary>
    private async Task UpdateBettingTimer(GameManager gameManager)
    {
        gameManager.BettingTimer += DeltaTime;
        if (gameManager.BettingTimer >= Constants.BETTING_PERIOD)
        {
            await DeductBettingMoney(gameManager);
            gameManager.BettingTimer -= Constants.BETTING_PERIOD;
        }
    }

    /// <summary>
    /// 10초마다 호출되는 베팅금 차감 메서드
    /// </summary>
    private async Task DeductBettingMoney(GameManager gameManager)
    {
        if (gameManager.CurrentGameState != GameState.Round || gameManager.Round <= 0 ||
            gameManager.Round > Constants.MAX_ROUNDS)
            return;

        var totalDeducted = 0;

        Console.WriteLine($"[베팅] 라운드 {gameManager.Round}: {gameManager.BettingAmount}달러씩 차감 시작");

        foreach (var player in gameManager.Players.Values.Where(p => p.Alive))
        {
            // 베팅금 절반 증강 체크
            int playerBettingAmount = gameManager.BettingAmount;
            if (player.Augments.Contains((int)AugmentId.HalfBettingCost))
            {
                playerBettingAmount = (int)(gameManager.BettingAmount * AugmentConstants.HALF_BETTING_COST_MULTIPLIER);
                Console.WriteLine($"[증강] 플레이어 {player.Id}: 베팅금 절반 적용 ({gameManager.BettingAmount} -> {playerBettingAmount})");
            }
            
            var deducted = await gameManager.WithdrawFromPlayerAsync(player, playerBettingAmount);
            totalDeducted += deducted;

            Console.WriteLine($"[베팅] {player.Nickname}: {deducted}달러 차감 (잔액: {player.Balance}달러)");
        }

        // 총 판돈에 추가
        gameManager.TotalPrizeMoney += totalDeducted;
        Console.WriteLine($"[베팅] 총 {totalDeducted}달러 차감, 현재 판돈: {gameManager.TotalPrizeMoney}달러");

        // 베팅금 차감 브로드캐스트
        await gameManager.BroadcastToAll(new BettingDeductionBroadcast
        {
            DeductedAmount = gameManager.BettingAmount,
            TotalPrizeMoney = gameManager.TotalPrizeMoney
        });

        // 베팅금 차감 후 게임/라운드 종료 조건 체크
        await gameManager.CheckAndHandleGameEndAsync();
    }

    /// <summary>
    /// 플레이어들의 AccuracyState에 따라 Accuracy, Range, MinEnergyToShoot을 업데이트합니다.
    /// </summary>
    private async Task UpdateByAccuracyState(GameManager gameManager, float deltaTime)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            if (player.UpdateByAccuracyState(deltaTime))
            {
                await gameManager.BroadcastToAll(new UpdateAccuracyBroadcast
                {
                    PlayerId = player.Id,
                    Accuracy = player.Accuracy
                });
                
                await gameManager.BroadcastToAll(new UpdateRangeBroadcast
                {
                    PlayerId = player.Id,
                    Range = player.Range
                });

                await gameManager.BroadcastToAll(new UpdateMinEnergyToShootBroadcast
                {
                    PlayerId = player.Id,
                    MinEnergyToShoot = player.MinEnergyToShoot
                });
            }
        }
    }

    /// <summary>
    /// 플레이어들의 아이템 제작 타이머를 업데이트합니다
    /// </summary>
    private async Task UpdatePlayerCreatingStates(GameManager gameManager, float deltaTime)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            if (player.UpdateCreating(deltaTime))
            {
                // 아이템 제작 완료 시 랜덤 아이템 지급
                await GiveRandomItemToPlayer(gameManager, player);
            }
        }
    }

    /// <summary>
    /// 플레이어들의 위치를 업데이트합니다
    /// </summary>
    private void SimulatePlayerPosition(GameManager gameManager, float deltaTime)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            var newPosition = CalculateNewPosition(player.MovementData, deltaTime);
            if (Vector2.Distance(newPosition, player.MovementData.Position) > 0.01f)
            {
                player.MovementData.Position = newPosition;
            }
        }
    }

    /// <summary>
    /// 플레이어들의 에너지를 업데이트합니다
    /// </summary>
    private void UpdatePlayerEnergyStates(GameManager gameManager, float deltaTime)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;
            player.UpdateEnergyRecovery(deltaTime);
        }
    }

    /// <summary>
    /// 플레이어들의 버프 상태를 업데이트합니다
    /// </summary>
    private async Task UpdatePlayerBuffStates(GameManager gameManager, float deltaTime)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            // 속도 버프 타이머 업데이트
            if (player.UpdateSpeedBoost(deltaTime))
            {
                // 버프가 종료되면 속도 원복 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateSpeedBroadcast
                {
                    PlayerId = player.Id,
                    Speed = player.MovementData.Speed
                });
                
                // 활성 효과 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateActiveEffectsBroadcast
                {
                    PlayerId = player.Id,
                    ActiveEffects = player.ActiveEffects
                });
            }
        }
    }

    /// <summary>
    /// 플레이어의 새로운 위치를 계산합니다
    /// </summary>
    private static Vector2 CalculateNewPosition(MovementData movementData, float deltaTime)
    {
        var vector = DirectionHelper.IntToDirection(movementData.Direction);
        if (vector == Vector2.Zero) return movementData.Position;

        return movementData.Position + vector * movementData.Speed * deltaTime;
    }

    /// <summary>
    /// 플레이어에게 랜덤 아이템을 지급합니다
    /// </summary>
    private static async Task GiveRandomItemToPlayer(GameManager gameManager, Player player)
    {
        // 가중치 기반 랜덤 아이템 선택
        var randomItemId = GameManager.GetRandomItemByWeight();

        player.AcquireItem((int)randomItemId);

        // 아이템 획득 브로드캐스트
        await gameManager.BroadcastToAll(new ItemAcquiredBroadcast
        {
            PlayerId = player.Id,
            ItemId = (int)randomItemId
        });

        Console.WriteLine($"[아이템] 플레이어 {player.Id}가 {randomItemId} 아이템 획득");
    }

    /// <summary>
    /// 봇들의 AI를 업데이트합니다
    /// </summary>
    private async Task UpdateBotAI(GameManager gameManager, float deltaTime)
    {
        var bots = gameManager.Players.Values.OfType<Bot>().ToList();
        foreach (var bot in bots)
        {
            await bot.UpdateAI(deltaTime);
        }
    }
    
    /// <summary>
    /// 쇼다운 상태에서 타이머를 업데이트하고 시간이 되면 자동으로 라운드를 시작합니다
    /// </summary>
    private async Task UpdateShowdownTimer(GameManager gameManager)
    {
        gameManager.ShowdownTimer += DeltaTime;
        
        if (gameManager.ShowdownTimer >= GameManager.SHOWDOWN_DURATION)
        {
            // 타이머 완료 - 라운드 시작
            Console.WriteLine("[게임] 쇼다운 시간 종료 - 자동으로 라운드 1 시작!");
            await gameManager.StartRoundStateAsync(1);
        }
    }
}