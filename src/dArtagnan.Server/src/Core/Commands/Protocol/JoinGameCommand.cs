using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 참가 명령 - 새로운 플레이어가 게임에 참가할 때 처리합니다
/// </summary>
public class PlayerJoinCommand : IGameCommand
{
    public required int ClientId { get; init; }
    public required string Nickname { get; init; }
    public required ClientConnection Client { get; init; }
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        if (gameManager.IsGamePlaying())
        {
            Console.WriteLine($"[게임] {ClientId}번 클라이언트 난입 거부.");
            return;
        }
        
        Console.WriteLine($"[게임] {ClientId}번 클라이언트 참가 요청");
        
        // 플레이어가 이미 존재하는지 확인
        var existingPlayer = gameManager.GetPlayerById(ClientId);
        if (existingPlayer != null)
        {
            Console.WriteLine($"[오류] 이미 존재하는 플레이어: {ClientId}");
            return;
        }
        
        // 새 플레이어 생성
        var player = await gameManager.AddPlayer(ClientId, Nickname);
        Console.WriteLine($"[게임] 새 플레이어 생성: {player.Id} ({player.Nickname})");
        
        // 클라이언트에게 플레이어 ID 전송
        await Client.SendPacketAsync(new YouAre
        {
            PlayerId = player.Id
        });
        
        // 현재 게임 상태 전송
        var waitingPacket = new GameInWaitingFromServer 
        { 
            PlayersInfo = gameManager.PlayersInRoom() 
        };
        await Client.SendPacketAsync(waitingPacket);
        
        // 다른 플레이어들에게 새 플레이어 참가 알림
        await gameManager.BroadcastToAll(new PlayerJoinBroadcast 
        { 
            PlayerInfo = player.PlayerInformation 
        });
        
        Console.WriteLine($"[게임] 플레이어 {player.Id} 참가 완료 (현재 인원: {gameManager.Players.Count})");
    }
} 