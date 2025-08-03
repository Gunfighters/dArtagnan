using dArtagnan.Shared;
using System.Numerics;

namespace dArtagnan.Server;

/// <summary>
/// 게임 시작 명령 - 방장이 게임을 시작할 때 처리합니다
/// 게임 전체 초기화부터 룰렛 준비까지 모든 로직을 담당합니다
/// </summary>
public class StartGameCommand : IGameCommand
{
    required public int PlayerId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        // 클라이언트 검증 - 방장만 게임 시작 가능
        var starter = gameManager.GetPlayerById(PlayerId);
        if (starter == null || starter != gameManager.Host) 
        {
            Console.WriteLine($"[게임] 비방장 {PlayerId}의 게임 시작 요청 거부");
            return;
        }
        
        // 이미 게임 진행 중인지 확인
        if (gameManager.CurrentGameState != GameState.Waiting)
        {
            Console.WriteLine($"[게임] 이미 게임 진행중");
            return;
        }

        Console.WriteLine($"[게임] 게임 시작! (참가자: {gameManager.Players.Count}명)");

        // === 봇 생성 로직 ===
        await CreateBots(gameManager);
        
        // === 룰렛 시작 ===
        await gameManager.StartRouletteStateAsync();
    }











    /// <summary>
    /// 최대인원이 될 때까지 봇을 생성합니다
    /// </summary>
    private static async Task CreateBots(GameManager gameManager)
    {
        var currentPlayerCount = gameManager.Players.Count;
        var botsNeeded = Constants.MAX_PLAYER_COUNT - currentPlayerCount;

        if (botsNeeded <= 0)
        {
            Console.WriteLine($"[봇] 플레이어가 충분합니다. 봇 생성 없음 (현재: {currentPlayerCount}명)");
            return;
        }

        Console.WriteLine($"[봇] {botsNeeded}명의 봇을 생성합니다 (현재 플레이어: {currentPlayerCount}명)");

        for (int i = 0; i < botsNeeded; i++)
        {
            var botId = gameManager.GetNextAvailableId();
            var botNickname = $"Bot{i + 1}";
            var spawnPosition = Player.GetSpawnPosition(currentPlayerCount + i);

            await gameManager.AddBot(botId, botNickname, spawnPosition);
        }

        Console.WriteLine($"[봇] 총 {botsNeeded}명의 봇 생성 완료. 전체 참가자: {gameManager.Players.Count}명");
    }
} 