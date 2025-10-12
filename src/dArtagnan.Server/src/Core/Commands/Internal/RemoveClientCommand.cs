using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 제거 명령 - 정상/비정상 종료 모두 통일된 방식으로 처리합니다
/// </summary>
public class RemoveClientCommand : IGameCommand
{
    required public int ClientId;
    required public ClientConnection? Client;
    required public bool IsNormalDisconnect; // 정상 종료 여부
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var disconnectType = IsNormalDisconnect ? "정상 퇴장" : "비정상 종료";
        Logger.log($"[게임] 클라이언트 {ClientId} {disconnectType} 처리 시작");
        
        Logger.log($"[DEBUG] RemoveClientInternal called for client {ClientId}");
        Logger.log($"[DEBUG] Current thread: {Thread.CurrentThread.ManagedThreadId}");
        Logger.log($"[DEBUG] Stack trace: {Environment.StackTrace}");
        var player = gameManager.GetPlayerById(ClientId);

        if (player != null)
        {
            Logger.log($"[게임] 플레이어 {player.Id}({player.Nickname}) 퇴장 처리");

            // 로비서버에 플레이어 퇴장 보고
            LobbyReporter.ReportPlayerLeave(player.ProviderId);

            await gameManager.BroadcastToAllExcept(new LeaveBroadcast
            {
                PlayerId = player.Id
            }, ClientId);

            await gameManager.BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1, // 시스템 메시지
                Message = $"{player.Nickname}님이 퇴장했습니다"
            });
        }

        gameManager.Players.TryRemove(ClientId, out _);
        gameManager.Clients.TryRemove(ClientId, out _);
        LobbyReporter.ReportPlayerCount(gameManager.Clients.Count);

        if (player != null)
        {
            Logger.log($"[게임] 플레이어 {player.Id} 제거 완료 (현재 인원: {gameManager.Players.Count}, 접속자: {gameManager.Clients.Count})");
        }

        if (player == gameManager.Host)
        {
            var nextHost = gameManager.Players.Values.FirstOrDefault(p => p.Alive && p is not Bot);
            await gameManager.SetHost(nextHost);
        }
        await gameManager.CheckAndHandleGameEndAsync();
        
        // 연결 종료 (비동기로 처리)
        if (Client != null)
        {
            _ = Task.Run(() => Client.DisconnectAsync());
        }
        
        Logger.log($"[게임] 클라이언트 {ClientId} {disconnectType} 처리 완료");
    }
} 