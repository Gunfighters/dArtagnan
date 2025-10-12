using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 정확도 상태 설정 명령 - 플레이어의 정확도 증가/감소/유지 상태를 설정합니다
/// </summary>
public class SetAccuracyCommand : IGameCommand
{
    required public int PlayerId;
    required public int AccuracyState;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;

        // 클라이언트 검증 - 정확도 상태 유효성 검사
        if (AccuracyState < -1 || AccuracyState > 1)
        {
            Logger.log($"[정확도] 클라이언트 {PlayerId}가 잘못된 정확도 상태 요청: {AccuracyState}");
            return;
        }

        // 플레이어의 정확도 상태 설정
        await player.SetAccuracyState(AccuracyState);
    }
} 