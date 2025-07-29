using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 이동 명령 - 플레이어의 이동 데이터를 업데이트하고 브로드캐스트합니다
/// </summary>
public class PlayerMovementCommand : IGameCommand
{
    required public int PlayerId;
    required public MovementData MovementData;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;

        // 아이템 제작 중에는 움직일 수 없음
        if (player.IsCreatingItem)
        {
            Console.WriteLine($"[이동] 플레이어 {PlayerId}는 아이템 제작 중으로 이동 불가");
            return;
        }
        
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