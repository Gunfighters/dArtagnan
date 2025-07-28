using dArtagnan.Shared;

namespace dArtagnan.Server;

public class PlayerMovementCommand : IGameCommand
{
    required public int PlayerId;
    required public MovementData MovementData;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        // UDP로 위치 데이터 브로드캐스트 (보낸 클라이언트 제외)
        await gameManager.BroadcastUdpToAllExcept(new PlayerMovementDataBroadcast
        {
            PlayerId = PlayerId,
            MovementData = MovementData,
        }, PlayerId);
    }
} 