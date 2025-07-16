using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 게임 시작 명령 - 방장이 게임을 시작할 때 처리합니다
/// </summary>
public class StartGameCommand : IGameCommand
{
    public required int PlayerId { get; init; }
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var starter = gameManager.GetPlayerById(PlayerId);
        if (starter == null)
        {
            Console.WriteLine($"[게임] 플레이어 {PlayerId}를 찾을 수 없습니다.");
            return;
        }
        
        if (starter != gameManager.Host)
        {
            Console.WriteLine($"[게임] 경고: 방장이 아닌 플레이어가 게임 시작 시도 (Player #{starter.Id})");
            return;
        }

        if (gameManager.IsGamePlaying())
        {
            Console.WriteLine($"[게임] 경고: 이미 게임이 진행중.");
            return;
        }

        await gameManager.StartGame();
    }
} 