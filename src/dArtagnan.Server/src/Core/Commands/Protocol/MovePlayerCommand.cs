using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 이동 명령 - 플레이어의 이동 데이터를 업데이트하고 브로드캐스트합니다
/// </summary>
public class PlayerMovementCommand : IGameCommand
{
    public int PlayerId;
    public MovementData MovementData;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        
        // 플레이어 위치, 방향, 속도 업데이트
        player.UpdateMovementData(
            MovementData.Position, 
            MovementData.Direction, 
            MovementData.Speed
        );
        
        // 모든 플레이어에게 이동 정보 브로드캐스트
        await gameManager.BroadcastToAll(new PlayerMovementDataBroadcast
        {
            PlayerId = PlayerId,
            MovementData = player.MovementData,
        });
    }
} 