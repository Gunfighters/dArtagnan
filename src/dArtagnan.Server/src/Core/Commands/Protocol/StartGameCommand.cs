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
        if (gameManager.IsGamePlaying())
        {
            Console.WriteLine($"[게임] 이미 게임 진행중");
            return;
        }

        Console.WriteLine($"[게임] 게임 시작! (참가자: {gameManager.Players.Count}명)");

        // === 게임 전체 상태 초기화 (한 번에) ===
        InitializeGameState(gameManager);
        
        // === 플레이어들 초기화 & 정확도 설정 ===
        var accuracyPool = GenerateAccuracyPool();
        await InitializeAllPlayers(gameManager, accuracyPool);
        
        // === 룰렛 시작 브로드캐스트 ===
        await BroadcastRouletteStart(gameManager, accuracyPool);
    }



    /// <summary>
    /// 게임 전체 상태를 초기화합니다 (중복 제거)
    /// </summary>
    private static void InitializeGameState(GameManager gameManager)
    {
        gameManager.Round = 0;
        gameManager.TotalPrizeMoney = 0;
        gameManager.BettingTimer = 0f;
        gameManager.rouletteDonePlayers.Clear();
        gameManager.CurrentGameState = GameState.RouletteSpinning;
        
        Console.WriteLine($"[게임] 게임 상태 초기화 완료 -> RouletteSpinning");
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
    /// 모든 플레이어를 게임용으로 초기화합니다
    /// </summary>
    private static Task InitializeAllPlayers(GameManager gameManager, List<int> accuracyPool)
    {
        // 각 플레이어 초기화 & 정확도 할당
        foreach (var player in gameManager.Players.Values)
        {
            var randomAccuracy = accuracyPool[Random.Shared.Next(0, accuracyPool.Count)];
            player.ResetForInitialGame(randomAccuracy);
            Console.WriteLine($"[초기화] {player.Nickname}: {player.Accuracy}% (잔액: {player.Balance}달러)");
        }
        
        // 위치 재배치 (한 번만)
        gameManager.ResetRespawnAll(true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 모든 클라이언트에게 룰렛 시작을 알립니다
    /// </summary>
    private static async Task BroadcastRouletteStart(GameManager gameManager, List<int> accuracyPool)
    {
        var broadcastTasks = gameManager.Players.Values
            .Select(player => gameManager.Clients[player.Id].SendPacketAsync(new YourAccuracyAndPool
            { 
                AccuracyPool = accuracyPool, 
                YourAccuracy = player.Accuracy 
            }));
            
        await Task.WhenAll(broadcastTasks);
        Console.WriteLine($"[룰렛] 모든 플레이어에게 룰렛 시작 알림 전송 완료");
    }
} 