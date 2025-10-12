using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 핑 명령 - 클라이언트의 핑 요청에 응답합니다
/// </summary>
public class PingCommand : IGameCommand
{
    required public ClientConnection Client;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        await gameManager.SendToPlayer(Client.Id, new PongPacket());
    }
} 