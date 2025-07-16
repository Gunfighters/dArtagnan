using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 제거 명령 - 정상/비정상 종료 모두 통일된 방식으로 처리합니다
/// </summary>
public class RemoveClientCommand : IGameCommand
{
    public required int ClientId { get; init; }
    public required ClientConnection? Client { get; init; }
    public required bool IsNormalDisconnect { get; init; } // 정상 종료 여부
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var disconnectType = IsNormalDisconnect ? "정상 퇴장" : "비정상 종료";
        Console.WriteLine($"[게임] 클라이언트 {ClientId} {disconnectType} 처리 시작");
        
        // GameManager에서 모든 상태 변경 처리
        await gameManager.RemoveClientInternal(ClientId);
        
        // 연결 종료 (비동기로 처리)
        if (Client != null)
        {
            _ = Task.Run(() => Client.DisconnectAsync());
        }
        
        Console.WriteLine($"[게임] 클라이언트 {ClientId} {disconnectType} 처리 완료");
    }
} 