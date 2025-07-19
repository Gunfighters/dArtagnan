using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 사격 명령 - 사격 처리 및 피해 계산을 수행합니다
/// </summary>
public class PlayerShootingCommand : IGameCommand
{
    public required int ShooterId { get; init; }
    public required int TargetId { get; init; }
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var shooter = gameManager.GetPlayerById(ShooterId);
        if (shooter == null || !shooter.Alive || shooter.RemainingReloadTime > 0)
        {
            Console.WriteLine($"[전투] 플레이어 {ShooterId} 사격 불가 (사망 또는 재장전 중)");
            return;
        }
            
        var target = gameManager.GetPlayerById(TargetId);
        if (target == null || !target.Alive)
        {
            Console.WriteLine($"[전투] 유효하지 않은 타겟: {TargetId}");
            return;
        }
            
        // 명중 여부 계산
        bool hit = Random.Shared.NextDouble() * 100 < shooter.Accuracy;
        
        // 재장전 시간 설정
        shooter.UpdateReloadTime(shooter.TotalReloadTime);
        
        Console.WriteLine($"[전투] 플레이어 {shooter.Id} -> {target.Id} 사격: {(hit ? "명중" : "빗나감")}");
        
        // 사격 결과 브로드캐스트
        await gameManager.BroadcastToAll(new PlayerShootingBroadcast
        {
            ShooterId = ShooterId,
            TargetId = TargetId,
            Hit = hit,
            ShooterRemainingReloadingTime = shooter.RemainingReloadTime
        });
        
        // 명중 시 피해 처리
        if (hit && gameManager.IsGamePlaying())
        {
            // 타겟의 돈 일부를 사격자에게 이전
            shooter.Balance += target.Withdraw(Math.Max(target.Balance / 10, 50));
            
            // 잔액 업데이트 브로드캐스트
            await gameManager.BroadcastToAll(new PlayerBalanceUpdateBroadcast
            { 
                Balance = shooter.Balance, 
                PlayerId = shooter.Id 
            });
            await gameManager.BroadcastToAll(new PlayerBalanceUpdateBroadcast
            { 
                Balance = target.Balance, 
                PlayerId = target.Id 
            });
            
            // 타겟 사망 처리
            await KillPlayer(target, gameManager);
        }
    }
    
    private async Task KillPlayer(Player player, GameManager gameManager)
    {
        Console.WriteLine($"[전투] 플레이어 {player.Id} 사망");
        player.UpdateAlive(false);
        
        await gameManager.BroadcastToAll(new UpdatePlayerAlive
        {
            PlayerId = player.Id,
            Alive = player.Alive
        });
        
        if (gameManager.ShouldEndRound())
        {
            await gameManager.EndCurrentRoundAsync();
        }
    }
} 