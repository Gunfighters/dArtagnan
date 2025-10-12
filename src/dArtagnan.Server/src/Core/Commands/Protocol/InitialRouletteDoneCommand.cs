using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 이니셜 룰렛 완료 명령 - 플레이어가 룰렛을 확인했을 때 처리합니다
/// </summary>
public class InitialRouletteDoneCommand : IGameCommand
{
    required public int PlayerId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;
        
        // Initial Roulette 상태가 아니면 무시
        if (gameManager.CurrentGameState != GameState.InitialRoulette)
        {
            Logger.log($"[이니셜룰렛] 플레이어 {PlayerId}의 완료 요청이 잘못된 상태에서 도착: {gameManager.CurrentGameState}");
            return;
        }
        
        Logger.log($"[이니셜룰렛] 플레이어 {PlayerId}({player.Nickname}) 룰렛 확인 완료");
        
        // 완료한 플레이어 목록에 추가
        gameManager.initialRouletteCompletedPlayers.Add(PlayerId);
        
        // 모든 플레이어가 완료했는지 확인
        var alivePlayers = gameManager.Players.Values.Count(p => !p.Bankrupt);
        
        Logger.log($"[이니셜룰렛] 완료 상황: {gameManager.initialRouletteCompletedPlayers.Count}/{alivePlayers}");
        
        if (gameManager.initialRouletteCompletedPlayers.Count >= alivePlayers)
        {
            Logger.log($"[이니셜룰렛] 모든 플레이어 완료! 라운드 1 시작");
            
            // 완료 플레이어 목록 초기화
            gameManager.initialRouletteCompletedPlayers.Clear();
            
            // 라운드 1 시작
            await gameManager.StartRoundStateAsync(1);
        }
    }
}