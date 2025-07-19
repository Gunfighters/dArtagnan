using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 관리자 킬 명령 - 관리자가 특정 플레이어를 강제로 죽일 때 처리합니다
/// </summary>
public class AdminKillPlayerCommand : IGameCommand
{
    required public int TargetPlayerId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(TargetPlayerId);
        if (player == null)
        {
            Console.WriteLine($"[관리자] 플레이어 ID {TargetPlayerId}를 찾을 수 없습니다.");
            return;
        }
        
        if (!player.Alive)
        {
            Console.WriteLine($"[관리자] 플레이어 {TargetPlayerId}({player.Nickname})는 이미 사망한 상태입니다.");
            return;
        }
        
        Console.WriteLine($"[관리자] 플레이어 {TargetPlayerId}({player.Nickname})를 죽입니다...");
        
        // 플레이어 사망 처리
        player.Alive = false;
        
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