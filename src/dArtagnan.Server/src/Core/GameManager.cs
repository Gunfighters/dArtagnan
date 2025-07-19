using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Channels;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 게임 세션, 클라이언트 연결, 브로드캐스팅을 통합 관리하는 클래스
/// </summary>
public class GameManager
{
    public readonly ConcurrentDictionary<int, Player> Players = new();
    public readonly ConcurrentDictionary<int, ClientConnection> Clients = new();
    public Player? Host;
    public GameState CurrentGameState = GameState.Waiting;
    public int Round = 0; 
    
    // 베팅금/판돈 시스템
    public int TotalPrizeMoney = 0; // 총 판돈
    public float BettingTimer = 0f; // 베팅금 차감 타이머 (10초마다)
    public readonly int[] BettingAmounts = { 10, 20, 30, 40 }; // 라운드별 베팅금
    public const int MAX_ROUNDS = 4; // 최대 라운드 수
    
    public HashSet<Player> rouletteDonePlayers = [];
    private readonly Channel<IGameCommand> _commandQueue = Channel.CreateUnbounded<IGameCommand>(new UnboundedChannelOptions
    {
        SingleReader = true,  // 단일 소비자
        SingleWriter = false  // 다중 생산자
    });
    
    public GameManager()
    {
        _ = Task.Run(() => ProcessCommandsAsync());
    }
    
    /// <summary>
    /// Command를 큐에 추가하는 메서드
    /// </summary>
    public async Task EnqueueCommandAsync(IGameCommand command)
    {
        await _commandQueue.Writer.WriteAsync(command);
    }
    
    /// <summary>
    /// Command Queue 처리 루프
    /// </summary>
    private async Task ProcessCommandsAsync()
    {
        await foreach (var command in _commandQueue.Reader.ReadAllAsync())
        {
            try
            {
                await command.ExecuteAsync(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[오류] 커맨드 실행 중 오류: {ex.Message}");
            }
        }
    }
    


    private async Task SetHost(Player? player)
    {
        Host = player;
        Console.WriteLine($"[게임] 새 방장: {Host?.Id}");
        if (Host != null)
        {
            await BroadcastToAll(new NewHostBroadcast { HostId = Host.Id });
        }
    }

    public async Task<Player> AddPlayer(int clientId, string nickname)
    {
        var player = new Player(clientId, nickname, Vector2.Zero);
        Players.TryAdd(player.Id, player);
        if (Host == null)
        {
            await SetHost(player);
        }
        return player;
    }

    /// <summary>
    /// 내부적으로만 사용되는 클라이언트 제거 메서드
    /// </summary>
    internal async Task RemoveClientInternal(int clientId)
    {
        var player = GetPlayerById(clientId);
            
        if (player != null)
        {
            Console.WriteLine($"[게임] 플레이어 {player.Id}({player.Nickname}) 퇴장 처리");
                
            await BroadcastToAllExcept(new PlayerLeaveBroadcast
            {
                PlayerId = player.Id
            }, clientId);
        }

        Players.TryRemove(clientId, out _);
        Clients.TryRemove(clientId, out _);
            
        if (player != null)
        {
            Console.WriteLine($"[게임] 플레이어 {player.Id} 제거 완료 (현재 인원: {Players.Count}, 접속자: {Clients.Count})");
        }

        if (player == Host)
        {
            var nextHost = Players.Values.FirstOrDefault(p => p.Alive);
            await SetHost(nextHost);
        }

        if (Players.IsEmpty && CurrentGameState == GameState.Playing)
        {
            await ResetGameToWaiting();
        }
    }

    public Player? GetPlayerById(int clientId)
    {
        Players.TryGetValue(clientId, out var player);
        return player;
    }

    public async Task BroadcastToAll(IPacket packet)
    {
        var tasks = Clients.Values
            .Select(client => client.SendPacketAsync(packet)).ToList();

        if (tasks.Count != 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    public async Task BroadcastToAllExcept(IPacket packet, int excludeClientId)
    {
        var tasks = Clients.Values
            .Where(client => client.Id != excludeClientId)
            .Select(client => client.SendPacketAsync(packet));

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    public List<PlayerInformation> PlayersInRoom()
    {
        return Players.Values.Select(player => player.PlayerInformation).ToList();
    }



    /// <summary>
    /// 라운드 종료 조건을 확인합니다
    /// </summary>
    public bool ShouldEndRound()
    {
        return Players.Values.Count(p => p.Alive) <= 1;
    }

    /// <summary>
    /// 게임 종료 조건을 확인합니다
    /// </summary>
    public bool ShouldEndGame()
    {
        // 4라운드 완료시 게임 종료 또는 파산하지 않은 플레이어가 1명 이하일 때
        return Round >= MAX_ROUNDS || Players.Values.Count(p => !p.Bankrupt) <= 1;
    }

    public void ResetRespawnAll(bool includeBankrupts)
    {
        var pool = includeBankrupts ? Players.Values : Players.Values.Where(p => !p.Bankrupt).ToList();
        var size = pool.Count;
        for (var index = 0; index < size; index++)
        {
            var player = pool.ElementAt(index);
            player.ResetForNextRound();
            player.MovementData.Position = Player.GetSpawnPosition(index);
        }
    }



    /// <summary>
    /// 플레이 상태로 전환합니다
    /// </summary>
    private async Task TransitionToPlayingAsync()
    {
        var oldState = CurrentGameState;
        CurrentGameState = GameState.Playing;
        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {CurrentGameState}");
        
        await BroadcastToAll(new GameInPlayingFromServer { 
            PlayersInfo = PlayersInRoom(), 
            Round = Round, 
            TotalTime = 0f, 
            RemainingTime = 0f,
            BettingAmount = BettingAmounts[Round - 1]
        });
    }

    /// <summary>
    /// 현재 라운드를 종료하고 다음 단계로 진행합니다
    /// </summary>
    public async Task EndCurrentRoundAsync()
    {
        await GiveRoundPrizeToWinner();

        await Task.Delay(2500);
        
        if (ShouldEndGame())
        {
            await AnnounceGameWinner();
            await ResetGameToWaiting();
        }
        else
        {
            await StartNextRoundAsync(Round + 1);
        }
    }

    /// <summary>
    /// 게임이 진행 중인지 확인합니다
    /// </summary>
    public bool IsGamePlaying()
    {
        return CurrentGameState != GameState.Waiting;
    }

    /// <summary>
    /// 다음 라운드를 시작합니다
    /// </summary>
    public async Task StartNextRoundAsync(int newRound)
    {
        ResetRespawnAll(false);
        Round = newRound;
        
        // 새 라운드 시작시 베팅 타이머 초기화
        BettingTimer = 0f;
        
        Console.WriteLine($"[라운드 {newRound}] 라운드 시작! 현재 판돈: {TotalPrizeMoney}달러");
        
        await TransitionToPlayingAsync();
    }

    /// <summary>
    /// 게임을 완전히 초기화하고 대기 상태로 돌아갑니다
    /// </summary>
    private async Task ResetGameToWaiting()
    {
        var oldState = CurrentGameState;
        
        // 게임 완전 초기화
        foreach (var p in Players.Values)
        {
            p.ResetForInitialGame(0);
        }
        ResetRespawnAll(true);
        Round = 0;
        rouletteDonePlayers.Clear();
        
        // 베팅 시스템 초기화
        TotalPrizeMoney = 0;
        BettingTimer = 0f;
        
        // 대기 상태로 전환
        CurrentGameState = GameState.Waiting;
        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {CurrentGameState}");
        
        await BroadcastToAll(new GameInWaitingFromServer { PlayersInfo = PlayersInRoom() });
    }

    /// <summary>
    /// 라운드 승리자에게 판돈을 지급합니다
    /// </summary>
    private async Task GiveRoundPrizeToWinner()
    {
        var survivors = Players.Values.Where(p => p.Alive).ToList();
        
        // 생존자가 1명일 때만 판돈 지급
        if (survivors.Count == 1 && TotalPrizeMoney > 0)
        {
            var winner = survivors[0];
            winner.Balance += TotalPrizeMoney;
            
            Console.WriteLine($"[라운드 {Round}] {winner.Nickname}이(가) 라운드 승리! 판돈 {TotalPrizeMoney}달러 획득");
            
            // 라운드 승리자 브로드캐스트
            await BroadcastToAll(new RoundWinnerBroadcast
            {
                PlayerId = winner.Id,
                Round = Round,
                PrizeMoney = TotalPrizeMoney
            });
            
            // 승리자 잔액 업데이트 브로드캐스트
            await BroadcastToAll(new PlayerBalanceUpdateBroadcast
            {
                PlayerId = winner.Id,
                Balance = winner.Balance
            });
        }
        else if (TotalPrizeMoney > 0)
        {
            Console.WriteLine($"[라운드 {Round}] 생존자가 {survivors.Count}명이므로 판돈 {TotalPrizeMoney}달러는 다음 라운드로 이월");
        }
        
        // 다음 라운드를 위해 판돈 초기화 (생존자가 1명일 때만)
        if (survivors.Count == 1)
        {
            TotalPrizeMoney = 0;
        }
    }
    
    private async Task AnnounceGameWinner()
    {
        var winner = Players.Values.SingleOrDefault(p => !p.Bankrupt)?.Id;
        
        Console.WriteLine($"[게임 종료] 최종 승리자: {winner}");
        await BroadcastToAll(new GameWinnerBroadcast { PlayerId = winner ?? -1 });
    }
    
    /// <summary>
    /// 10초마다 호출되는 베팅금 차감 메서드
    /// </summary>
    public async Task DeductBettingMoney()
    {
        if (CurrentGameState != GameState.Playing || Round <= 0 || Round > MAX_ROUNDS)
            return;
            
        var currentBettingAmount = BettingAmounts[Round - 1];
        var totalDeducted = 0;
        
        Console.WriteLine($"[베팅] 라운드 {Round}: {currentBettingAmount}달러씩 차감 시작");
        
        foreach (var player in Players.Values.Where(p => p.Alive))
        {
            var deducted = Math.Min(currentBettingAmount, player.Balance);
            player.Balance -= deducted;
            totalDeducted += deducted;
            
            Console.WriteLine($"[베팅] {player.Nickname}: {deducted}달러 차감 (잔액: {player.Balance}달러)");
            
            // 개별 플레이어 잔액 업데이트 브로드캐스트
            await BroadcastToAll(new PlayerBalanceUpdateBroadcast
            {
                PlayerId = player.Id,
                Balance = player.Balance
            });
        }
        
        // 총 판돈에 추가
        TotalPrizeMoney += totalDeducted;
        Console.WriteLine($"[베팅] 총 {totalDeducted}달러 차감, 현재 판돈: {TotalPrizeMoney}달러");
        
        // 베팅금 차감 브로드캐스트
        await BroadcastToAll(new BettingDeductionBroadcast 
        { 
            DeductedAmount = currentBettingAmount,
            TotalPrizeMoney = TotalPrizeMoney
        });
    }
}