using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Channels;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 게임 세션, 클라이언트 연결, 브로드캐스팅을 통합 관리하는 클래스
/// 커맨드에서 공통으로 쓰이는 유틸함수들을 모아둔다.
/// 나머지 게임로직은 커맨드에서 직접 처리한다.
/// </summary>
public class GameManager
{

    // 방 정보
    public readonly ConcurrentDictionary<int, Player> Players = new();
    public readonly ConcurrentDictionary<int, ClientConnection> Clients = new();
    public Player? Host;
    public GameState CurrentGameState = GameState.Waiting;
    public int Round = 0; 
    
    // 베팅금/판돈 시스템
    public int TotalPrizeMoney = 0; // 총 판돈
    public readonly int[] BettingAmounts = { 10, 20, 30, 40 }; // 라운드별 베팅금
    public int BettingAmount = 0;
    public float BettingTimer = 0f; // 베팅금 차감 타이머 constants.BETTING_PERIOD 마다
    public const int MAX_ROUNDS = 4; // 최대 라운드 수
    
    // 증강 시스템
    public HashSet<Player> rouletteDonePlayers = [];
    public Dictionary<int, List<int>> playerAugmentOptions = []; // 플레이어별 증강 옵션 저장
    public HashSet<int> augmentSelectionDonePlayers = []; // 증강 선택을 완료한 플레이어 ID
    
    
    // 커맨드 시스템
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

        if (Players.IsEmpty && CurrentGameState == GameState.Round)
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
    /// 증강 선택을 시작합니다
    /// </summary>
    public async Task StartAugmentSelection()
    {
        Console.WriteLine("[증강] 증강 선택 단계 시작");
        
        // 게임 상태를 Augment로 변경
        CurrentGameState = GameState.Augment;
        
        // 플레이어별 증강 옵션 저장소 초기화
        playerAugmentOptions.Clear();
        augmentSelectionDonePlayers.Clear();
        
        // 파산하지 않은 플레이어들에게 증강 선택 패킷 전송
        var alivePlayers = Players.Values.Where(p => !p.Bankrupt).ToList();
        
        foreach (var player in alivePlayers)
        {
            var client = Clients.GetValueOrDefault(player.Id);
            if (client != null)
            {
                var augmentOptions = GenerateAugmentOptions();
                
                // 플레이어별 증강 옵션 저장
                playerAugmentOptions[player.Id] = augmentOptions;
                
                await client.SendPacketAsync(new AugmentStartFromServer
                {
                    AugmentOptions = augmentOptions
                });
                
                Console.WriteLine($"[증강] {player.Id}번 플레이어에게 증강 선택 옵션 전송: [{string.Join(", ", augmentOptions)}]");
            }
        }
    }

    /// <summary>
    /// 랜덤 증강 옵션 3개를 생성합니다
    /// </summary>
    private List<int> GenerateAugmentOptions()
    {
        // 임시로 3개의 랜덤 증강 ID 생성 (1~10 범위)
        var options = new List<int>();
        var random = new Random();
        
        while (options.Count < 3)
        {
            var augmentId = random.Next(1, 11);
            if (!options.Contains(augmentId))
            {
                options.Add(augmentId);
            }
        }
        
        return options;
    }

    /// <summary>
    /// 플레이어에서 돈을 차감하고 잔액 업데이트를 브로드캐스트합니다
    /// </summary>
    public async Task<int> WithdrawFromPlayerAsync(Player player, int amount)
    {
        var actualWithdrawn = player.Withdraw(amount);
        
        // 잔액 업데이트 브로드캐스트
        await BroadcastToAll(new PlayerBalanceUpdateBroadcast
        {
            PlayerId = player.Id,
            Balance = player.Balance
        });
        
        // 파산 시 즉시 사망 처리
        if (player.Bankrupt && player.Alive)
        {
            Console.WriteLine($"[게임] 플레이어 {player.Id}({player.Nickname}) 파산으로 사망!");
            
            player.Alive = false;
            await BroadcastToAll(new UpdatePlayerAlive
            {
                PlayerId = player.Id,
                Alive = player.Alive
            });
        }
        
        return actualWithdrawn;
    }
    
    /// <summary>
    /// 플레이어 간 돈 이전을 처리하고 양쪽 모두 잔액 브로드캐스트를 합니다 (사격 등에서 사용)
    /// </summary>
    public async Task<int> TransferMoneyBetweenPlayersAsync(Player from, Player to, int amount)
    {
        var actualTransferred = from.Withdraw(amount);
        to.Balance += actualTransferred;
        
        // 양쪽 플레이어 잔액 업데이트 브로드캐스트
        await BroadcastToAll(new PlayerBalanceUpdateBroadcast
        {
            PlayerId = from.Id,
            Balance = from.Balance
        });
        await BroadcastToAll(new PlayerBalanceUpdateBroadcast
        {
            PlayerId = to.Id,
            Balance = to.Balance
        });
        
        // 돈을 잃은 플레이어의 파산 체크
        if (from.Bankrupt && from.Alive)
        {
            Console.WriteLine($"[게임] 플레이어 {from.Id}({from.Nickname}) 파산으로 사망!");
            
            from.Alive = false;
            await BroadcastToAll(new UpdatePlayerAlive
            {
                PlayerId = from.Id,
                Alive = from.Alive
            });
        }
        
        return actualTransferred;
    }
    
    /// <summary>
    /// 게임/라운드 종료 조건을 체크하고 적절한 처리를 수행합니다
    /// </summary>
    public async Task CheckAndHandleGameEndAsync()
    {
        if (ShouldEndRound())
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
                // 라운드 종료 후 다음 라운드 진행 전에 증강 선택 단계 시작
                await StartAugmentSelection();
            }
        }
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
    /// 다음 라운드를 시작합니다
    /// </summary>
    public async Task StartNextRoundAsync(int newRound)
    {
        ResetRespawnAll(false);
        Round = newRound;
        BettingAmount = BettingAmounts[Round-1];
        BettingTimer = 0f;
        TotalPrizeMoney = 0;
        
        Console.WriteLine($"[라운드 {newRound}] 라운드 시작! 현재 베팅금: {BettingAmount}달러");
        
        var oldState = CurrentGameState;
        CurrentGameState = GameState.Round;
        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {CurrentGameState}");
        
        await BroadcastToAll(new RoundStartFromServer { 
            PlayersInfo = PlayersInRoom(), 
            Round = Round,
            BettingAmount = BettingAmounts[Round - 1]
        });
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
        
        await BroadcastToAll(new WaitingStartFromServer { PlayersInfo = PlayersInRoom() });
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
    }
    
    private async Task AnnounceGameWinner()
    {
        var winner = Players.Values.SingleOrDefault(p => !p.Bankrupt)?.Id;
        
        Console.WriteLine($"[게임 종료] 최종 승리자: {winner}");
        await BroadcastToAll(new GameWinnerBroadcast { PlayerId = winner ?? -1 });
    }
}