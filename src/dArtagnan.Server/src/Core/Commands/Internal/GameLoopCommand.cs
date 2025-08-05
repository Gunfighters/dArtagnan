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
                // 대기 상태: 정확도, 아이템 제작, 위치, 에너지 업데이트
                await UpdateByAccuracyState(gameManager, DeltaTime);
                await UpdatePlayerCreatingStates(gameManager, DeltaTime);
                UpdatePlayerMovementStates(gameManager, DeltaTime);
                UpdatePlayerEnergyStates(gameManager, DeltaTime);
                break;

            case GameState.Round:
                // 라운드 상태: 베팅금 차감 + 모든 플레이어 상태 업데이트 + 봇 AI 업데이트
                await UpdateBettingTimer(gameManager);
                await UpdateByAccuracyState(gameManager, DeltaTime);
                await UpdatePlayerCreatingStates(gameManager, DeltaTime);
                UpdatePlayerMovementStates(gameManager, DeltaTime);
                UpdatePlayerEnergyStates(gameManager, DeltaTime);
                await UpdateBotAI(gameManager, DeltaTime);
                break;

            case GameState.Roulette:
            case GameState.Augment:
                break;
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
            var deducted = await gameManager.WithdrawFromPlayerAsync(player, gameManager.BettingAmount);
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
    /// 플레이어들의 정확도를 업데이트합니다
    /// </summary>
    private async Task UpdateByAccuracyState(GameManager gameManager, float deltaTime)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            if (player.UpdateByAccuracyState(deltaTime))
            {
                await gameManager.BroadcastToAll(new UpdatePlayerAccuracyBroadcast
                {
                    PlayerId = player.Id,
                    Accuracy = player.Accuracy
                });
                
                await gameManager.BroadcastToAll(new UpdatePlayerRangeBroadcast
                {
                    PlayerId = player.Id,
                    Range = player.Range
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
    private void UpdatePlayerMovementStates(GameManager gameManager, float deltaTime)
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
        // 임시로 아이템 ID 1~5 중 랜덤 선택 (나중에 아이템 시스템 확장 시 수정)
        int randomItemId = Random.Shared.Next(1, 6);

        player.AcquireItem(randomItemId);

        // 아이템 획득 브로드캐스트
        await gameManager.BroadcastToAll(new ItemAcquiredBroadcast
        {
            PlayerId = player.Id,
            ItemId = randomItemId
        });
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


}