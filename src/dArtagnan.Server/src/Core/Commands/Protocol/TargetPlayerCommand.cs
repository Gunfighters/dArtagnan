using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 타겟팅 명령 - 플레이어가 다른 플레이어를 타겟팅할 때 처리합니다
/// </summary>
public class PlayerTargetingCommand : IGameCommand
{
    required public int ShooterId;
    required public int TargetId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var shooter = gameManager.GetPlayerById(ShooterId);
        var target = gameManager.GetPlayerById(TargetId);
        if (shooter == null || target == null) return;
        
        await gameManager.BroadcastToAll(new PlayerIsTargetingBroadcast
        { 
            ShooterId = ShooterId, 
            TargetId = TargetId 
        });
    }
} 