using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 사격 명령 - 사격 처리 및 피해 계산을 수행합니다
/// </summary>
public class PlayerShootingCommand : IGameCommand
{
    required public int ShooterId;
    required public int TargetId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var shooter = gameManager.GetPlayerById(ShooterId);
        if (shooter == null || !shooter.Alive || shooter.RemainingReloadTime > 0)
        {
            Console.WriteLine($"[전투] 플레이어 {ShooterId} 사격 불가 (사망 또는 재장전 중)");
            return;
        }

        // 아이템 제작 중에는 사격할 수 없음
        if (shooter.IsCreatingItem)
        {
            Console.WriteLine($"[전투] 플레이어 {ShooterId}는 아이템 제작 중으로 사격 불가");
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
        shooter.RemainingReloadTime = shooter.TotalReloadTime;
        
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
        if (hit && gameManager.CurrentGameState != GameState.Waiting)
        {
            // 타겟의 돈 일부를 사격자에게 이전
            await gameManager.TransferMoneyBetweenPlayersAsync(target, shooter, gameManager.BettingAmount);
            
            // 타겟 사망 처리
            await KillPlayer(target, gameManager);
        }
    }
    
    private async Task KillPlayer(Player player, GameManager gameManager)
    {
        // 이미 죽은 플레이어는 처리하지 않음 (파산 등으로 이미 사망한 경우)
        if (!player.Alive)
        {
            Console.WriteLine($"[전투] 플레이어 {player.Id}는 이미 사망 상태 (파산 등)");
            await gameManager.CheckAndHandleGameEndAsync();
            return;
        }
        
        Console.WriteLine($"[전투] 플레이어 {player.Id} 사격으로 사망");
        player.Alive = false;
        
        await gameManager.BroadcastToAll(new UpdatePlayerAlive
        {
            PlayerId = player.Id,
            Alive = player.Alive
        });
        
        await gameManager.CheckAndHandleGameEndAsync();
    }
} 