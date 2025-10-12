using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 퇴장 명령 - 플레이어가 게임을 떠날 때 처리합니다
/// </summary>
public class PlayerLeaveCommand : IGameCommand
{
    required public int PlayerId;
    required public ClientConnection Client;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        Logger.log($"[게임] 클라이언트 {PlayerId} 정상 퇴장 요청 수신");
        
        var removeCommand = new RemoveClientCommand
        {
            ClientId = PlayerId,
            Client = Client,
            IsNormalDisconnect = true
        };
        
        await removeCommand.ExecuteAsync(gameManager);
    }
} 