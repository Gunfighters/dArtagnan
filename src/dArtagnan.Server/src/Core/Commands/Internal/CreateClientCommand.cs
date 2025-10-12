using System.Net.Sockets;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 생성 명령 - 새로운 클라이언트 연결을 위한 ID 할당 및 ClientConnection 생성
/// </summary>
public class CreateClientCommand : IGameCommand
{
    public required TcpClient TcpClient;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        try
        {
            // thread-safe한 ID 할당
            int clientId = gameManager.GetNextAvailableId();
            
            // ClientConnection 생성
            var client = new ClientConnection(clientId, TcpClient, gameManager);
            
            // Clients Dictionary에 추가
            gameManager.Clients.TryAdd(clientId, client);
            LobbyReporter.ReportPlayerCount(gameManager.Clients.Count);

            Logger.log($"[Network][Client-{clientId}] 새 클라이언트 생성: 현재 접속자 {gameManager.Clients.Count}명");
        }
        catch (Exception ex)
        {
            Logger.log($"[Network][System] 클라이언트 생성 실패: {ex.Message}");
        }
    }
} 