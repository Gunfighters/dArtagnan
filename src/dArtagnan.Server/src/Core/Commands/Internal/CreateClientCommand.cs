using System.Net.Sockets;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 생성 명령 - 새로운 클라이언트 연결을 위한 ID 할당 및 ClientConnection 생성
/// </summary>
public class CreateClientCommand : IGameCommand
{
    required public TcpClient TcpClient;
    
    public Task ExecuteAsync(GameManager gameManager)
    {
        try
        {
            // thread-safe한 ID 할당
            int clientId = GetNextAvailableId(gameManager);
            
            // ClientConnection 생성
            var client = new ClientConnection(clientId, TcpClient, gameManager);
            
            // Clients Dictionary에 추가
            gameManager.Clients.TryAdd(clientId, client);
            
            Console.WriteLine($"새 클라이언트 연결됨 (ID: {clientId}, 현재 접속자: {gameManager.Clients.Count})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"클라이언트 생성 실패: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
    
    private static int GetNextAvailableId(GameManager gameManager)
    {
        int id = 1;
        while (gameManager.Clients.ContainsKey(id))
        {
            id++;
        }
        return id;
    }
} 