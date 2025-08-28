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
    public const float SHOWDOWN_DURATION = 3.0f; // 3초 대기
    public const float EMPTY_SERVER_TIMEOUT = 10f;


    // 커맨드 시스템
    private readonly Channel<IGameCommand> _commandQueue = Channel.CreateUnbounded<IGameCommand>(
        new UnboundedChannelOptions
        {
            SingleReader = true, // 단일 소비자
            SingleWriter = false // 다중 생산자
        });

    public readonly int[] BettingAmounts = { 10, 20, 30, 40 }; // 라운드별 베팅금

    public readonly ConcurrentDictionary<int, ClientConnection> Clients = new();

    // 방 정보
    public readonly ConcurrentDictionary<int, Player> Players = new();
    public HashSet<int> augmentSelectionDonePlayers = []; // 증강 선택을 완료한 플레이어 ID
    public int BettingAmount = 0;
    public float BettingTimer = 0f; // 베팅금 차감 타이머 constants.BETTING_PERIOD 마다
    public GameState CurrentGameState = GameState.Waiting;

    // 서버 종료 타이머
    public float emptyServerTimer = 0f;
    public Player? Host;

    // 증강 시스템
    public Dictionary<int, List<int>> playerAugmentOptions = []; // 플레이어별 증강 옵션 저장
    public int Round = 0;

    // 쇼다운 상태 타이머
    public float ShowdownTimer = 0f;

    // 베팅금/판돈 시스템
    public int TotalPrizeMoney = 0; // 총 판돈

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

    internal int GetNextAvailableId()
    {
        int id = 1;
        while (Clients.ContainsKey(id) || Players.ContainsKey(id))
        {
            id++;
        }

        return id;
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
    /// 봇을 생성하고 게임에 추가합니다
    /// </summary>
    public async Task<Bot> AddBot(int botId, string nickname, Vector2 position)
    {
        var bot = new Bot(botId, nickname, position, this);
        Players.TryAdd(bot.Id, bot);

        Console.WriteLine($"[봇] {nickname} 생성 완료 (ID: {botId}, 위치: {position})");

        // 다른 플레이어들에게 봇 참가 알림
        await BroadcastToAll(new JoinBroadcast
        {
            PlayerInfo = bot.PlayerInformation
        });

        return bot;
    }

    /// <summary>
    /// 내부적으로만 사용되는 클라이언트 제거 메서드
    /// </summary>
    internal async Task RemoveClientInternal(int clientId)
    {
        Console.WriteLine($"[DEBUG] RemoveClientInternal called for client {clientId}");
        Console.WriteLine($"[DEBUG] Current thread: {Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine($"[DEBUG] Stack trace: {Environment.StackTrace}");
        var player = GetPlayerById(clientId);

        if (player != null)
        {
            Console.WriteLine($"[게임] 플레이어 {player.Id}({player.Nickname}) 퇴장 처리");

            await BroadcastToAllExcept(new LeaveBroadcast
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
            var nextHost = Players.Values.FirstOrDefault(p => p.Alive && p is not Bot);
            await SetHost(nextHost);
        }

        var realPlayers = Players.Values.Where(p => p is not Bot).ToList();
        if (realPlayers.Count == 0)
        {
            Console.WriteLine("[게임] 실제 플레이어가 모두 나가서 게임을 대기 상태로 초기화합니다");
            await StartWaitingStateAsync();
        }
    }

    public Player? GetPlayerById(int clientId)
    {
        Players.TryGetValue(clientId, out var player);
        return player;
    }

    public async Task BroadcastToAll(IPacket packet)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}][게임] {packet.GetType().Name} 패킷 브로드캐스트");

        // 실제 클라이언트에게 패킷 전송
        var clientTasks = Clients.Values
            .Select(client => client.SendPacketAsync(packet)).ToList();

        // 봇들에게는 AI 처리 로직 실행
        var botTasks = Players.Values.OfType<Bot>()
            .Select(bot => bot.HandlePacketAsync(packet)).ToList();

        // 모든 작업을 병렬로 실행
        var allTasks = clientTasks.Concat(botTasks);
        if (allTasks.Any())
        {
            await Task.WhenAll(allTasks);
        }
    }

    public async Task BroadcastToAllExcept(IPacket packet, int excludeClientId)
    {
        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}][게임] {packet.GetType().Name} 패킷 브로드캐스트 (제외: {excludeClientId})");
        var tasks = Clients.Values
            .Where(client => client.Id != excludeClientId)
            .Select(client => client.SendPacketAsync(packet));

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 특정 플레이어에게만 패킷을 전송합니다 (봇인 경우 AI 로직 실행)
    /// </summary>
    public async Task SendToPlayer(int playerId, IPacket packet)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}][게임] {packet.GetType().Name} 패킷을 플레이어 {playerId}에게 전송");

        // 플레이어가 봇인지 확인
        if (Players.TryGetValue(playerId, out var player) && player is Bot bot)
        {
            // 봇인 경우 AI 처리 로직 실행
            await bot.HandlePacketAsync(packet);
        }
        else if (Clients.TryGetValue(playerId, out var client))
        {
            // 실제 클라이언트인 경우 패킷 전송
            await client.SendPacketAsync(packet);
        }
        else
        {
            Console.WriteLine($"[게임] 플레이어 {playerId}를 찾을 수 없습니다");
        }
    }

    public List<PlayerInformation> PlayersInRoom()
    {
        return Players.Values.Select(player => player.PlayerInformation).ToList();
    }

    /// <summary>
    /// 증강 선택을 시작합니다
    /// </summary>
    public async Task StartAugmentStateAsync()
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
            var augmentOptions = GenerateAugmentOptions(player);

            // 플레이어별 증강 옵션 저장
            playerAugmentOptions[player.Id] = augmentOptions;

            await SendToPlayer(player.Id, new AugmentStartFromServer
            {
                AugmentOptions = augmentOptions
            });

            // 증강 이름도 함께 로그 출력
            var optionNames = augmentOptions.Select(id =>
            {
                if (id == -1) return "없음";
                if (AugmentConstants.Augments.TryGetValue((AugmentId)id, out var augmentData))
                    return $"{augmentData.Name}({id})";
                return id.ToString();
            });

            Console.WriteLine(
                $"[증강] {player.Id}번 플레이어({player.Nickname})에게 증강 선택 옵션 전송: [{string.Join(", ", optionNames)}]");
        }
    }

    /// <summary>
    /// 플레이어별로 맞춤 증강 옵션 3개를 생성합니다 (이미 보유한 증강 제외)
    /// </summary>
    /// <param name="player">옵션을 생성할 플레이어</param>
    private List<int> GenerateAugmentOptions(Player player)
    {
        // 플레이어가 이미 보유한 증강 제외한 사용 가능한 증강 목록
        var availableAugments = AugmentConstants.Augments.Keys
            .Where(augmentId => !player.Augments.Contains((int)augmentId))
            .Select(id => (int)id)
            .ToList();

        var options = new List<int>();
        var random = new Random();

        // 사용 가능한 증강에서 최대 3개까지 가중치 기반 선택
        var tempAvailable = new List<int>(availableAugments);
        while (options.Count < 3 && tempAvailable.Count > 0)
        {
            // 가중치 기반 선택을 위해 사용 가능한 증강들의 총 가중치 계산
            var availableAugmentData = AugmentConstants.Augments
                .Where(kvp => tempAvailable.Contains((int)kvp.Key))
                .ToList();

            if (availableAugmentData.Count == 0) break;

            var totalWeight = availableAugmentData.Sum(kvp => kvp.Value.Weight);
            var randomValue = random.Next(totalWeight);

            var currentWeight = 0;
            AugmentId selectedAugment = AugmentId.None;

            foreach (var kvp in availableAugmentData)
            {
                currentWeight += kvp.Value.Weight;
                if (randomValue < currentWeight)
                {
                    selectedAugment = kvp.Key;
                    break;
                }
            }

            if (selectedAugment != AugmentId.None)
            {
                var selectedId = (int)selectedAugment;
                options.Add(selectedId);
                tempAvailable.Remove(selectedId); // 중복 방지
            }
        }

        // 사용 가능한 증강이 3개 미만이면 -1로 패딩
        while (options.Count < 3)
        {
            options.Add(-1);
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
        await BroadcastToAll(new BalanceUpdateBroadcast
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

            // 플레이어 사망 시스템 메시지 브로드캐스트
            await BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1, // 시스템 메시지
                Message = $"{player.Nickname}님이 파산으로 탈락했습니다!"
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
        await BroadcastToAll(new BalanceUpdateBroadcast
        {
            PlayerId = from.Id,
            Balance = from.Balance
        });
        await BroadcastToAll(new BalanceUpdateBroadcast
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
                await Task.Delay(2500);
                await Task.Delay(2500);
                await StartWaitingStateAsync();
            }
            else
            {
                await StartAugmentStateAsync();
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
        return Round >= Constants.MAX_ROUNDS || Players.Values.Count(p => !p.Bankrupt) <= 1;
    }

    /// <summary>
    /// 대기 상태로 게임 전체를 초기화 (모든 플레이어 + 게임 상태)
    /// </summary>
    public void InitToWaiting()
    {
        // 실제 플레이어들만 대기 상태로 초기화 및 배치
        var realPlayers = Players.Values.Where(p => p is not Bot).ToList();
        for (var index = 0; index < realPlayers.Count; index++)
        {
            var player = realPlayers[index];
            player.InitToWaiting();
            player.MovementData.Position = Player.GetSpawnPosition(index);
        }

        // 게임 상태 초기화
        Round = 0;
        TotalPrizeMoney = 0;
        BettingTimer = 0f;

        // 시스템 상태 초기화
        playerAugmentOptions.Clear();
        augmentSelectionDonePlayers.Clear();

        // 게임 상태 변경
        CurrentGameState = GameState.Waiting;
    }

    /// <summary>
    /// 라운드 상태로 초기화 (파산하지 않은 플레이어 + 라운드 상태)
    /// </summary>
    public void InitToRound(int newRound)
    {
        Round = newRound;
        BettingAmount = BettingAmounts[newRound - 1];

        // 파산하지 않은 플레이어만 라운드 상태로 초기화 및 배치
        var alivePlayers = Players.Values.Where(p => !p.Bankrupt).ToList();
        for (var index = 0; index < alivePlayers.Count; index++)
        {
            var player = alivePlayers[index];
            player.InitToRound();
            player.MovementData.Position = Player.GetSpawnPosition(index);
        }

        // 베팅 시스템 초기화
        BettingTimer = 0f;
        TotalPrizeMoney = 0;

        // 게임 상태 변경
        CurrentGameState = GameState.Round;
    }

    /// <summary>
    /// 모든 봇을 제거합니다
    /// </summary>
    private async Task RemoveAllBots()
    {
        var botsToRemove = Players.Values.OfType<Bot>().ToList();
        if (botsToRemove.Count == 0)
        {
            return;
        }

        Console.WriteLine($"[봇] {botsToRemove.Count}명의 봇을 제거합니다");

        foreach (var bot in botsToRemove)
        {
            Players.TryRemove(bot.Id, out _);
            Console.WriteLine($"[봇] {bot.Nickname} 제거 완료 (ID: {bot.Id})");

            // 다른 플레이어들에게 봇 퇴장 알림
            await BroadcastToAll(new LeaveBroadcast
            {
                PlayerId = bot.Id,
            });
        }

        Console.WriteLine($"[봇] 모든 봇 제거 완료. 남은 참가자: {Players.Count}명");
    }

    /// <summary>
    /// 다음 라운드를 시작합니다
    /// </summary>
    public async Task StartRoundStateAsync(int newRound)
    {
        var oldState = CurrentGameState;

        InitToRound(newRound);

        Console.WriteLine($"[라운드 {newRound}] 라운드 시작! 현재 베팅금: {BettingAmount}달러");
        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {CurrentGameState}");

        await BroadcastToAll(new RoundStartFromServer
        {
            PlayersInfo = PlayersInRoom(),
            Round = Round,
            BettingAmount = BettingAmounts[Round - 1]
        });

        // 라운드 시작 시스템 메시지 브로드캐스트
        await BroadcastToAll(new ChatBroadcast
        {
            PlayerId = -1, // 시스템 메시지
            Message = $"라운드 {Round} 시작! 베팅금: {BettingAmount}달러"
        });

        LobbyReporter.ReportState(1);
    }

    /// <summary>
    /// 게임을 완전히 초기화하고 대기 상태로 돌아갑니다
    /// </summary>
    public async Task StartWaitingStateAsync()
    {
        var oldState = CurrentGameState;

        InitToWaiting();

        // 서버 종료 타이머 리셋
        emptyServerTimer = 0f;

        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {CurrentGameState}");

        await BroadcastToAll(new WaitingStartFromServer { PlayersInfo = PlayersInRoom() });

        // 대기 상태 시스템 메시지 브로드캐스트
        await BroadcastToAll(new ChatBroadcast
        {
            PlayerId = -1, // 시스템 메시지
            Message = "게임이 종료되었습니다. 대기 상태로 전환됩니다."
        });

        await RemoveAllBots();
        LobbyReporter.ReportState(0);
    }

    /// <summary>
    /// 쇼다운을 시작합니다 (정확도 배정 후 3초 대기 후 자동으로 라운드 시작)
    /// </summary>
    public async Task StartShowdownStateAsync()
    {
        var oldState = CurrentGameState;

        // 정확도 풀 생성 및 플레이어 배정
        var accuracyPool = GenerateAccuracyPool();
        AssignAccuracyToPlayers(accuracyPool);

        // 쇼다운 상태로 변경
        CurrentGameState = GameState.Showdown;
        ShowdownTimer = 0f; // 타이머 초기화

        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {CurrentGameState}");
        Console.WriteLine($"[게임] {SHOWDOWN_DURATION}초 후 자동으로 라운드 시작...");

        // 모든 플레이어에게 쇼다운 시작 브로드캐스트
        await BroadcastShowdownStart();

        LobbyReporter.ReportState(2);
    }

    /// <summary>
    /// 모든 클라이언트에게 쇼다운 시작을 알립니다
    /// </summary>
    private async Task BroadcastShowdownStart()
    {
        var broadcastTasks = Players.Values
            .Select(player => SendToPlayer(player.Id, new ShowdownStartFromServer
            {
                AccuracyPool = Players.Select(p => new KeyValuePair<int, int>(p.Key, p.Value.Accuracy)).ToDictionary()
            }));

        await Task.WhenAll(broadcastTasks);
        Console.WriteLine($"[쇼다운] 모든 플레이어에게 쇼다운 시작 알림 전송 완료");
    }

    /// <summary>
    /// 랜덤 정확도 풀을 생성합니다
    /// </summary>
    private List<int> GenerateAccuracyPool()
    {
        List<int> accuracyPool = [];
        for (var i = 0; i < 8; i++)
        {
            accuracyPool.Add(Random.Shared.Next(Constants.SHOWDOWN_MIN_ACCURACY, Constants.SHOWDOWN_MAX_ACCURACY + 1));
        }

        return accuracyPool;
    }

    /// <summary>
    /// 플레이어들에게 정확도를 할당합니다
    /// </summary>
    private void AssignAccuracyToPlayers(List<int> accuracyPool)
    {
        var availableAccuracies = new List<int>(accuracyPool);

        foreach (var player in Players.Values)
        {
            var randomIndex = Random.Shared.Next(0, availableAccuracies.Count);
            var randomAccuracy = availableAccuracies[randomIndex];
            availableAccuracies.RemoveAt(randomIndex); // 중복방지

            player.Accuracy = randomAccuracy;

            // 정확도 변경 시 사격 최소 필요 에너지 업데이트
            player.UpdateMinEnergyToShoot();

            // 현재정확도에 반비례한 사거리 계산
            float t = Math.Clamp(randomAccuracy / (float)Constants.SHOWDOWN_MAX_ACCURACY, 0f, 1f);
            player.Range = Constants.MAX_RANGE + t * (Constants.MIN_RANGE - Constants.MAX_RANGE);

            Console.WriteLine(
                $"[정확도] {player.Nickname}: {player.Accuracy}% (사거리: {player.Range:F2}, 최소필요에너지: {player.MinEnergyToShoot})");
        }
    }

    /// <summary>
    /// 라운드 승리자에게 판돈을 지급합니다
    /// </summary>
    private async Task GiveRoundPrizeToWinner()
    {
        var survivors = Players.Values.Where(p => p.Alive).ToList();
        int prizePerWinner = survivors.Count == 0 ? 0 : TotalPrizeMoney / survivors.Count;
        var winnerIds = new List<int>();

        foreach (var winner in survivors)
        {
            winner.Balance += prizePerWinner;
            winnerIds.Add(winner.Id);

            // 승리자 잔액 업데이트 브로드캐스트
            await BroadcastToAll(new BalanceUpdateBroadcast
            {
                PlayerId = winner.Id,
                Balance = winner.Balance
            });
        }

        if (survivors.Count == 1)
        {
            Console.WriteLine($"[라운드 {Round}] {survivors[0].Nickname}이(가) 라운드 승리! 판돈 {TotalPrizeMoney}달러 획득");
        }
        //사실 현재 게임로직 상 라운드 승리자가 여러명일 수는 없음.
        else
        {
            Console.WriteLine($"[라운드 {Round}] 생존자 {survivors.Count}명이 판돈을 공유! 각자 {prizePerWinner}달러 획득");
        }

        // 라운드 승리자 브로드캐스트
        await BroadcastToAll(new RoundWinnerBroadcast
        {
            PlayerIds = winnerIds,
            Round = Round,
            PrizeMoney = TotalPrizeMoney
        });

        // 라운드 승리자 시스템 메시지 브로드캐스트
        if (survivors.Count == 1)
        {
            await BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1, // 시스템 메시지
                Message = $"라운드 {Round} 승리: {survivors[0].Nickname}님! (상금: {TotalPrizeMoney}달러)"
            });
        }
        else if (survivors.Count > 1)
        {
            var winnerNames = string.Join(", ", survivors.Select(s => s.Nickname));
            await BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1, // 시스템 메시지
                Message = $"라운드 {Round} 승리: {winnerNames}! (각자 {prizePerWinner}달러 획득)"
            });
        }
    }

    private async Task AnnounceGameWinner()
    {
        var winners = Players.Values.Where(p => !p.Bankrupt).Select(p => p.Id).ToList();

        Console.WriteLine($"[게임 종료] 최종 승리자 {winners.Count}명: {string.Join(", ", winners)}");

        await BroadcastToAll(new GameWinnerBroadcast { PlayerIds = winners });

        // 게임 최종 승리자 시스템 메시지 브로드캐스트
        var winnerPlayers = Players.Values.Where(p => !p.Bankrupt).ToList();
        if (winnerPlayers.Count == 1)
        {
            await BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1, // 시스템 메시지
                Message = $"게임 종료! 최종 승리자: {winnerPlayers[0].Nickname}님! 축하합니다!"
            });
        }
        else if (winnerPlayers.Count > 1)
        {
            var winnerNames = string.Join(", ", winnerPlayers.Select(p => p.Nickname));
            await BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1, // 시스템 메시지
                Message = $"게임 종료! 최종 승리자: {winnerNames}! 축하합니다!"
            });
        }
    }

    /// <summary>
    /// 가중치 기반으로 랜덤 아이템을 선택합니다
    /// </summary>
    public static ItemId GetRandomItemByWeight()
    {
        var totalWeight = ItemConstants.Items.Values.Sum(item => item.Weight);
        var randomValue = Random.Shared.Next(totalWeight);

        var currentWeight = 0;
        foreach (var item in ItemConstants.Items.Values)
        {
            currentWeight += item.Weight;
            if (randomValue < currentWeight)
            {
                return item.Id;
            }
        }

        return ItemId.SpeedBoost; // 기본값
    }

    /// <summary>
    /// 가중치 기반으로 랜덤 증강을 선택합니다
    /// </summary>
    public static AugmentId GetRandomAugmentByWeight()
    {
        var totalWeight = AugmentConstants.Augments.Values.Sum(augment => augment.Weight);
        var randomValue = Random.Shared.Next(totalWeight);

        var currentWeight = 0;
        foreach (var augment in AugmentConstants.Augments.Values)
        {
            currentWeight += augment.Weight;
            if (randomValue < currentWeight)
            {
                return augment.Id;
            }
        }

        return AugmentId.AccuracyStateDoubleApplication; // 기본값
    }
}