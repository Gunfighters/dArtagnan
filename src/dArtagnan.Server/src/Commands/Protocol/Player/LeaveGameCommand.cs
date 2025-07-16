using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 퇴장 명령 - 플레이어가 게임을 떠날 때 처리합니다
/// </summary>
public class PlayerLeaveCommand : IGameCommand
{
    public required int PlayerId { get; init; }
    public required ClientConnection Client { get; init; }
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        Console.WriteLine($"[게임] 클라이언트 {PlayerId} 정상 퇴장 요청 수신");
        
        // 통일된 RemoveClientCommand 사용
        var removeCommand = new RemoveClientCommand
        {
            ClientId = PlayerId,
            Client = Client,
            IsNormalDisconnect = true
        };
        
        await removeCommand.ExecuteAsync(gameManager);
    }
} 