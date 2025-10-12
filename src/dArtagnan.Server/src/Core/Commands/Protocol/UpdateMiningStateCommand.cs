using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 채굴 상태 업데이트 명령 - 플레이어가 채굴을 시작/중단할 때 처리합니다
/// </summary>
public class UpdateMiningStateCommand : IGameCommand
{
    required public int PlayerId;
    required public bool IsMining;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null || !player.Alive) return;
        
        // 이미 같은 상태면 무시
        if (player.IsMining == IsMining) return;
        
        if (IsMining)
        {
            // 채굴 시작
            await player.StartMining();
        }
        else
        {
            // 채굴 중단
            await player.StopMining();
        }

        Logger.log($"[채굴] 플레이어 {PlayerId}({player.Nickname}) 채굴 상태 변경: {player.IsMining} -> {IsMining}");
    }
}