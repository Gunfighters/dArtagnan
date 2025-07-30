using dArtagnan.Shared;

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

        // === 게임 전체 상태 초기화 (한 번에) ===
        InitializeGameState(gameManager);

        // === 플레이어들 초기화 & 정확도 설정 ===
        var pool = GenerateAccuracyPool(gameManager.Players.Count);
        gameManager.AccuracyPool = pool
            .Select(acc => new AccuracyPoolItem { Accuracy = acc, Taken = false })
            .ToList();
        await gameManager.BroadcastToAll(new AccuracySelectionStartFromServer { AccuracyPool = pool });
        gameManager.WaitingForAccuracySelection = true;
        gameManager.AccuracySelectionTurn = gameManager.Players.Values.First().Id;
        await gameManager.BroadcastToAll(
            new PlayerTurnToSelectAccuracy { PlayerId = gameManager.AccuracySelectionTurn });
    }


    /// <summary>
    /// 게임 전체 상태를 초기화합니다
    /// </summary>
    private static void InitializeGameState(GameManager gameManager)
    {
        gameManager.Round = 0;
        gameManager.TotalPrizeMoney = 0;
        gameManager.BettingTimer = 0f;
        gameManager.BettingAmount = 0;
        gameManager.WaitingForAccuracySelection = false;
        gameManager.AccuracySelectionTurn = 0;
        gameManager.CurrentGameState = GameState.AccuracySelection;

        Console.WriteLine($"[게임] 게임 상태 초기화 완료 -> RouletteSpinning");
    }

    /// <summary>
    /// 랜덤 정확도 풀을 생성합니다
    /// </summary>
    private static List<int> GenerateAccuracyPool(int size)
    {
        List<int> accuracyPool = [];
        for (var i = 0; i < size; i++)
        {
            accuracyPool.Add(Player.GenerateRandomAccuracy());
        }

        return accuracyPool;
    }
}