using System.Numerics;
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
        // 게임이 플레이 중일 때만 타이머 업데이트
        if (gameManager.CurrentGameState == GameState.Playing)
        {
            // 베팅금 타이머 업데이트 (10초마다 차감)
            gameManager.BettingTimer += DeltaTime;
            if (gameManager.BettingTimer >= Constants.BETTING_PERIOD)
            {
                await DeductBettingMoney(gameManager);
                gameManager.BettingTimer -= Constants.BETTING_PERIOD;
            }
        }
        
        // 플레이어 상태 업데이트 (게임 상태와 무관하게 실행)
        await UpdatePlayerStates(gameManager, DeltaTime);
    }
    
    /// <summary>
    /// 10초마다 호출되는 베팅금 차감 메서드
    /// </summary>
    private async Task DeductBettingMoney(GameManager gameManager)
    {
        if (gameManager.CurrentGameState != GameState.Playing || gameManager.Round <= 0 || gameManager.Round > GameManager.MAX_ROUNDS)
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
    /// 모든 플레이어의 상태를 업데이트합니다
    /// </summary>
    private Task UpdatePlayerStates(GameManager gameManager, float deltaTime)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;
            
            // 정확도 업데이트 (매초마다 1% 증감)
            player.UpdateAccuracy(deltaTime);
            
            // 위치 업데이트
            var newPosition = CalculateNewPosition(player.MovementData, deltaTime);
            if (Vector2.Distance(newPosition, player.MovementData.Position) > 0.01f)
            {
                player.MovementData.Position = newPosition;
            }
            
            // 재장전 시간 업데이트
            if (player.RemainingReloadTime > 0)
            {
                player.RemainingReloadTime = Math.Max(0, player.RemainingReloadTime - deltaTime);
            }
        }
        
        return Task.CompletedTask;
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
} 