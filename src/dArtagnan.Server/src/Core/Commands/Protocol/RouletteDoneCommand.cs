using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 룰렛 완료 명령 - 플레이어가 정확도 룰렛을 완료했을 때 처리합니다
/// 모든 플레이어 완료시 첫 라운드를 즉시 시작합니다
/// </summary>
public class RouletteDoneCommand : IGameCommand
{
    public required int PlayerId { get; init; }
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;
        
        // 이미 완료한 플레이어인지 확인
        if (!gameManager.rouletteDonePlayers.Add(player)) return;
        
        Console.WriteLine($"[룰렛] 플레이어 {PlayerId} 룰렛 완료 ({gameManager.rouletteDonePlayers.Count}/{gameManager.Players.Count})");
        
        // 모든 플레이어가 룰렛을 완료했는지 확인
        if (gameManager.rouletteDonePlayers.Count >= gameManager.Players.Count)
        {
            Console.WriteLine("[룰렛] 모든 플레이어 룰렛 완료 - 첫 라운드 시작!");
            await StartFirstRound(gameManager);
        }
    }

    /// <summary>
    /// 첫 라운드를 깔끔하게 시작합니다 (중복 처리 제거)
    /// </summary>
    private static async Task StartFirstRound(GameManager gameManager)
    {
        // === 라운드 상태 설정 ===
        gameManager.Round = 1;
        gameManager.BettingTimer = 0f; // 베팅 타이머만 리셋 (다른 건 건드리지 않음)
        gameManager.CurrentGameState = GameState.Playing;
        
        Console.WriteLine($"[게임] 게임 상태 변경: RouletteSpinning -> Playing");
        Console.WriteLine($"[라운드 1] 첫 라운드 시작! 베팅금: 10달러/10초");
        
        // === 생존한 플레이어들만 위치 재배치 ===
        gameManager.ResetRespawnAll(false); // 파산자 제외
        
        // === 게임 시작 브로드캐스트 ===
        await gameManager.BroadcastToAll(new GameInPlayingFromServer
        { 
            PlayersInfo = gameManager.PlayersInRoom(), 
            Round = gameManager.Round, 
            TotalTime = 0f, 
            RemainingTime = 0f,
            BettingAmount = gameManager.GetCurrentBettingAmount()
        });
    }
} 