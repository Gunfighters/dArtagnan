using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 타겟팅 명령 - 플레이어가 다른 플레이어를 타겟팅할 때 처리합니다
/// </summary>
public class PlayerTargetingCommand : IGameCommand
{
    public int ShooterId;
    public int TargetId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        await gameManager.BroadcastToAll(new PlayerIsTargetingBroadcast
        { 
            ShooterId = ShooterId, 
            TargetId = TargetId 
        });
    }
} 