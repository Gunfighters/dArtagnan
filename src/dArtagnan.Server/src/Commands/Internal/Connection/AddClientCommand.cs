namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 추가 명령 - 새로운 클라이언트 연결을 게임 매니저에 추가
/// </summary>
public class AddClientCommand : IGameCommand
{
    public int ClientId { get; set; }
    public ClientConnection Client { get; set; }
    
    public Task ExecuteAsync(GameManager gameManager)
    {
        gameManager.Clients.TryAdd(ClientId, Client);
        Console.WriteLine($"클라이언트 {ClientId} 추가됨 (현재 접속자: {gameManager.Clients.Count})");
        return Task.CompletedTask;
    }
} 