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

        // === 대기 상태로 완전 초기화 ===
        gameManager.InitToWaiting();
        
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
            
            // 정확도에 따른 재장전 시간 재계산
            player.TotalReloadTime = randomAccuracy == 0
                ? Constants.DEFAULT_RELOAD_TIME
                : randomAccuracy / 100f * 1.5f * Constants.DEFAULT_RELOAD_TIME;
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
            .Select(player => gameManager.Clients[player.Id].SendPacketAsync(new YourAccuracyAndPool
            { 
                AccuracyPool = accuracyPool, 
                YourAccuracy = player.Accuracy 
            }));
            
        await Task.WhenAll(broadcastTasks);
        Console.WriteLine($"[룰렛] 모든 플레이어에게 룰렛 시작 알림 전송 완료");
    }
} 