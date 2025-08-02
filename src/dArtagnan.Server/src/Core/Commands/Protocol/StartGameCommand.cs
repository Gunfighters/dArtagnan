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
        
        // === 룰렛 관련 세세한 처리 ===
        var accuracyPool = GenerateAccuracyPool();
        AssignAccuracyToPlayers(gameManager, accuracyPool);
        
        // 룰렛 상태로 변경
        gameManager.CurrentGameState = GameState.Roulette;
        Console.WriteLine($"[게임] 게임 상태 변경: Waiting -> RouletteSpinning");
        
        // === 룰렛 시작 브로드캐스트 ===
        await BroadcastRouletteStart(gameManager, accuracyPool);
    }





    /// <summary>
    /// 랜덤 정확도 풀을 생성합니다
    /// </summary>
    private static List<int> GenerateAccuracyPool()
    {
        List<int> accuracyPool = [];
        for (var i = 0; i < 8; i++)
        {
            accuracyPool.Add(Player.GenerateRandomAccuracy());
        }
        return accuracyPool;
    }

    /// <summary>
    /// 플레이어들에게 정확도를 할당합니다
    /// </summary>
    private static void AssignAccuracyToPlayers(GameManager gameManager, List<int> accuracyPool)
    {
        foreach (var player in gameManager.Players.Values)
        {
            var randomAccuracy = accuracyPool[Random.Shared.Next(0, accuracyPool.Count)];
            player.Accuracy = randomAccuracy;
            
            // 현재정확도에 반비례한 재장전 시간 계산
            player.TotalReloadTime = randomAccuracy == 0
                ? Constants.DEFAULT_RELOAD_TIME
                : (0.1f + (float)Math.Sqrt(randomAccuracy) / 10f * 0.9f) * Constants.DEFAULT_RELOAD_TIME;
            player.RemainingReloadTime = player.TotalReloadTime;
            
            Console.WriteLine($"[정확도] {player.Nickname}: {player.Accuracy}% (재장전: {player.TotalReloadTime:F2}초)");
        }
    }

    /// <summary>
    /// 모든 클라이언트에게 룰렛 시작을 알립니다
    /// </summary>
    private static async Task BroadcastRouletteStart(GameManager gameManager, List<int> accuracyPool)
    {
        var broadcastTasks = gameManager.Players.Values
            .Select(player => gameManager.SendToPlayer(player.Id, new YourAccuracyAndPool
            { 
                AccuracyPool = accuracyPool, 
                YourAccuracy = player.Accuracy 
            }));
            
        await Task.WhenAll(broadcastTasks);
        Console.WriteLine($"[룰렛] 모든 플레이어에게 룰렛 시작 알림 전송 완료");
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