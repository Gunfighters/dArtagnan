using dArtagnan.Shared;

namespace dArtagnan.Server;

public class PlayerMovementCommand : IGameCommand
{
    required public int PlayerId;
    required public MovementData MovementData;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        await gameManager.BroadcastToAll(new PlayerMovementDataBroadcast
        {
            PlayerId = PlayerId,
            MovementData = MovementData,
        });
    }
} 