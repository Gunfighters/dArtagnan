using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 정확도 상태 설정 명령 - 플레이어의 정확도 증가/감소/유지 상태를 설정합니다
/// </summary>
public class SetAccuracyCommand : IGameCommand
{
    public int PlayerId;
    public int AccuracyState;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);

        // 플레이어의 정확도 상태 설정
        player.SetAccuracyState(AccuracyState);
        
        // 모든 플레이어들에게 정확도 상태 변경 브로드캐스트
        await gameManager.BroadcastToAll(new PlayerAccuracyStateBroadcast
        {
            PlayerId = PlayerId,
            AccuracyState = AccuracyState
        });
    }
} 