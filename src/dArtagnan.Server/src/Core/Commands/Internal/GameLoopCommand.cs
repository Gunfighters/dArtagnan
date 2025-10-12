using System.Numerics;
using System.Threading.Tasks;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 새로운 게임 루프 명령 - 베팅금 차감, 정확도 업데이트, 플레이어 상태 관리
/// </summary>
public class GameLoopCommand : IGameCommand
{
    required public float DeltaTime;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        switch (gameManager.CurrentGameState)
        {
            case GameState.Waiting:
                // 대기 상태: 빈 서버 타이머 체크 + 총쏘기 + MIning (대기 상태 재미를 위한 최소 로직만)
                await CheckEmptyServerTimeout(gameManager);
                await UpdatePlayerMiningStates(gameManager);
                SimulatePlayerPosition(gameManager);
                UpdatePlayerRemainingReloadTimes(gameManager);
                break;

            case GameState.Round:
                await UpdateBettingTimer(gameManager);
                await UpdatePlayerAccuracyStates(gameManager);
                await UpdatePlayerMiningStates(gameManager);
                await UpdatePlayerItemEffects(gameManager);
                SimulatePlayerPosition(gameManager);
                UpdatePlayerRemainingReloadTimes(gameManager);
                await UpdateBotAI(gameManager);
                break;

            case GameState.InitialRoulette:
                await UpdateInitialRouletteTimer(gameManager);
                break;

            case GameState.Shop:
                await UpdateShopTimer(gameManager);
                break;
        }
    }

    /// <summary>
    /// 대기 상태에서 빈 서버 타이머를 체크하고 타임아웃 시 서버 종료
    /// </summary>
    private async Task CheckEmptyServerTimeout(GameManager gameManager)
    {
        //개발 모드일 때는 자동 종료 x
        if (Program.DEV_MODE)
        {
            return;
        }

        var realPlayers = gameManager.Players.Values.Where(p => p is not Bot).ToList();
        
        if (realPlayers.Count == 0)
        {
            // 실제 플레이어가 없을 때 타이머 증가
            gameManager.emptyServerTimer += DeltaTime;
            
            // 첫 시작 시에만 로그 출력 (1초마다가 아니라)
            if (gameManager.emptyServerTimer <= DeltaTime) // 첫 프레임
            {
                Logger.log($"[서버] 대기 상태에서 플레이어가 없음 - {GameManager.EMPTY_SERVER_TIMEOUT}초 후 서버 종료 예정");
            }
            
            // 타임아웃 체크
            if (gameManager.emptyServerTimer >= GameManager.EMPTY_SERVER_TIMEOUT)
            {
                Logger.log($"[서버] {GameManager.EMPTY_SERVER_TIMEOUT}초 타임아웃 - 서버 종료");
                Environment.Exit(0);
            }
        }
        else
        {
            // 실제 플레이어가 있으면 타이머 리셋
            if (gameManager.emptyServerTimer > 0f)
            {
                Logger.log("[서버] 플레이어 접속으로 종료 타이머 취소");
                gameManager.emptyServerTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 라운드 상태에서 베팅금 타이머 업데이트
    /// </summary>
    private async Task UpdateBettingTimer(GameManager gameManager)
    {
        gameManager.BettingTimer += DeltaTime;
        if (gameManager.BettingTimer >= Constants.BETTING_PERIOD)
        {
            await DeductBettingMoney(gameManager);
            gameManager.BettingTimer -= Constants.BETTING_PERIOD;
        }
    }

    /// <summary>
    /// 10초마다 호출되는 베팅금 차감 메서드
    /// </summary>
    private async Task DeductBettingMoney(GameManager gameManager)
    {
        if (gameManager.CurrentGameState != GameState.Round || gameManager.Round <= 0 ||
            gameManager.Round > Constants.MAX_ROUNDS)
            return;

        var totalDeducted = 0;

        Logger.log($"[베팅] 라운드 {gameManager.Round}: {gameManager.BaseBettingAmount}달러씩 차감 시작");

        foreach (var player in gameManager.Players.Values.Where(p => p.Alive))
        {
            var deducted = await player.Withdraw(player.NextDeductionAmount, false);
            totalDeducted += deducted;

            Logger.log($"[베팅] {player.Nickname}: {deducted}달러 차감 (잔액: {player.Balance}달러)");
        }

        // 총 판돈에 추가
        gameManager.TotalPrizeMoney += totalDeducted;
        Logger.log($"[베팅] 총 {totalDeducted}달러 차감, 현재 판돈: {gameManager.TotalPrizeMoney}달러");

        // 징수 횟수 증가 및 N회마다 차감액 업데이트
        gameManager.DeductionCount++;
        if (gameManager.DeductionCount % Constants.DEDUCTION_MULTIPLY_PERIOD == 0)
        {
            int currentMultiplier = (int)Math.Pow(2, gameManager.DeductionCount / Constants.DEDUCTION_MULTIPLY_PERIOD);
            Logger.log($"[베팅] {Constants.DEDUCTION_MULTIPLY_PERIOD}회 징수 완료! 차감액 {currentMultiplier}배");

            foreach (var player in gameManager.Players.Values)
            {
                await player.UpdateNextDeductionAmount();
            }

            await gameManager.BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1,
                Message = $"베팅금 {Constants.DEDUCTION_MULTIPLY_PERIOD}회 징수로 베팅금이 2배 증가했습니다!"
            });
        }

        // 베팅금 차감 브로드캐스트
        await gameManager.BroadcastToAll(new StakeUpdateBroadcast
        {
            TotalPrizeMoney = gameManager.TotalPrizeMoney
        });
        var tasks = gameManager.Players.Values.Where(p => p.Alive).Select(p =>
            gameManager.BroadcastToAll(new BalanceUpdateBroadcast { Balance = p.Balance, PlayerId = p.Id }));
        await Task.WhenAll(tasks);

        // 베팅금 차감 후 게임/라운드 종료 조건 체크
        await gameManager.CheckAndHandleGameEndAsync();
    }


    /// <summary>
    /// 플레이어들의 채굴 상태를 업데이트합니다 (극단적으로 낮은 보상 위주 분포)
    /// </summary>
    private async Task UpdatePlayerMiningStates(GameManager gameManager)
    {
        // 분포 파라미터
        int a = Constants.MINING_MIN_REWARD; // 0
        int b = Constants.MINING_MAX_REWARD; // 20
        double r = 0.7; // 낮을수록 고보상 희소
        int n = b - a + 1;

        // 확률 누적합 배열 계산
        double totalWeight = 0;
        double[] prefix = new double[n];
        for (int i = a; i <= b; i++)
        {
            double w = Math.Pow(r, i - a); // w_i = r^(i-a)
            totalWeight += w;
            prefix[i - a] = totalWeight;
        }

        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            if (await player.UpdateMining(DeltaTime))
            {
                bool hasGoldenShovel = player.GetItemCount(ItemId.GreatMiner) > 0;

                // 확률적 보상 선택
                double pick = Random.Shared.NextDouble() * totalWeight;
                int lo = 0, hi = n - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (pick <= prefix[mid]) hi = mid;
                    else lo = mid + 1;
                }

                int baseReward = a + lo;
                int reward = hasGoldenShovel
                    ? (int)(baseReward * ItemConstants.GoldenShovel.REWARD_MULTIPLIER)
                    : baseReward;

                string message = reward switch
                {
                    0 => $"{player.Nickname}님의 채굴 실패...",
                    _ when reward >= 10 => $"{player.Nickname}님이 대박! 채굴로 {reward}달러 획득!!",
                    _ when reward >= 5 => $"{player.Nickname}님이 채굴로 {reward}달러 획득!!",
                    _ => $"{player.Nickname}님이 채굴로 {reward}달러 획득"
                };

                await gameManager.BroadcastToAll(new MiningCompleteBroadcast
                {
                    PlayerId = player.Id,
                    RewardAmount = reward
                });

                if (reward > 0)
                    await player.Deposit(reward);

                await gameManager.BroadcastToAll(new ChatBroadcast
                {
                    PlayerId = -1,
                    Message = message
                });

                Logger.log($"[채굴] 플레이어 {player.Id}({player.Nickname}) 채굴 완료: {reward}달러");
            }
        }
    }


    /// <summary>
    /// 플레이어들의 위치를 업데이트합니다
    /// </summary>
    private void SimulatePlayerPosition(GameManager gameManager)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            var newPosition = CalculateNewPosition(player.Direction, player.Position, player.Speed);
            if (Vector2.Distance(newPosition, player.Position) > 0.01f)
            {
                player.Position = newPosition;
            }
        }
    }

    /// <summary>
    /// 플레이어들의 재장전시간을 업데이트합니다
    /// </summary>
    private void UpdatePlayerRemainingReloadTimes(GameManager gameManager)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;
            if (player.RemainingReloadTime > 0f)
            {
                player.RemainingReloadTime = Math.Max(0f, player.RemainingReloadTime - DeltaTime);
            }
        }
    }
    
    /// <summary>
    /// 플레이어들의 정확도 상태에 따른 정확도 변경 처리
    /// </summary>
    private async Task UpdatePlayerAccuracyStates(GameManager gameManager)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            player.AccuracyUpdateTimer += DeltaTime;
            
            // ACCURACY_UPDATE_INTERVAL마다 정확도 업데이트
            if (player.AccuracyUpdateTimer >= Constants.ACCURACY_UPDATE_INTERVAL)
            {
                player.AccuracyUpdateTimer -= Constants.ACCURACY_UPDATE_INTERVAL;
                
                if(player.Balance >= player.AccuracyChangeCosts[player.AccuracyState + 1]){
                    await player.Withdraw(player.AccuracyChangeCosts[player.AccuracyState + 1]);
                    
                    int accuracyChange = player.AccuracyState * Constants.ACCURACY_STATE_RATE * (1 + player.GetItemCount(ItemId.AccuracyBooster));
                    await player.SetAccuracy(player.Accuracy + accuracyChange);
                }else{
                    await player.SetAccuracyState(0);
                }
            }
        }
    }

    private async Task UpdatePlayerItemEffects(GameManager gameManager)
    {
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;

            // 사망보험 아이템 효과
            if(player.GetItemCount(ItemId.LifeInsurance) > 0){
                player.LifeInsuranceTimer += DeltaTime;
                if (player.LifeInsuranceTimer >= ItemConstants.LifeInsurance.TIME_PERIOD)
                {
                    player.LifeInsuranceTimer -= ItemConstants.LifeInsurance.TIME_PERIOD;
                    await player.Withdraw(ItemConstants.LifeInsurance.COST_PER_SECOND);
                }
            }

            // 정조준 아이템 효과
            if(player.GetItemCount(ItemId.SteadyAim) > 0){
                if(player.Direction == 0) {
                    // 가만히 있을 때: 타이머 증가 및 주기적 사거리 증가
                    player.SteadyAimTimer += DeltaTime;
                    if (player.SteadyAimTimer >= ItemConstants.SteadyAim.UPDATE_INTERVAL) {
                        player.SteadyAimTimer -= ItemConstants.SteadyAim.UPDATE_INTERVAL;
                        player.SteadyAimStack += ItemConstants.SteadyAim.RANGE_INCREASE_PER_UPDATE;
                        await player.UpdateRange();
                    }
                }
                else {
                    // 움직일 때: 사거리 스택 및 타이머 초기화
                    if(player.SteadyAimStack > 0) {
                        player.SteadyAimStack = 0f;
                        player.SteadyAimTimer = 0f;
                        await player.UpdateRange();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 플레이어의 새로운 위치를 계산합니다
    /// </summary>
    private Vector2 CalculateNewPosition(int direction, Vector2 position, float speed)
    {
        var vector = direction.IntToDirection();
        if (vector == Vector2.Zero) return position;

        return position + vector * speed * DeltaTime;
    }

    /// <summary>
    /// 이니셜 룰렛 타이머를 업데이트합니다
    /// </summary>
    private async Task UpdateInitialRouletteTimer(GameManager gameManager)
    {
        gameManager.InitialRouletteTimer += DeltaTime;

        if (gameManager.InitialRouletteTimer >= Constants.INITIAL_ROULETTE_DURATION)
        {
            Logger.log($"[이니셜룰렛] 타임아웃! {Constants.INITIAL_ROULETTE_DURATION}초 경과 - 라운드 1 시작");

            // 완료 플레이어 목록 초기화
            gameManager.initialRouletteCompletedPlayers.Clear();

            // 라운드 1 시작
            await gameManager.StartRoundStateAsync(1);
        }
    }

    /// <summary>
    /// 상점 타이머를 업데이트합니다
    /// </summary>
    private async Task UpdateShopTimer(GameManager gameManager)
    {
        var prev = gameManager.ShopTimer;
        gameManager.ShopTimer += DeltaTime;
        
        if (gameManager.ShopTimer >= Constants.SHOP_DURATION)
        {
            Logger.log("[상점] 상점 시간 종료! 다음 라운드 시작");

            // 게임 종료 조건 체크 (MAX_ROUNDS 초과 또는 파산하지 않은 플레이어 1명 이하)
            if (gameManager.ShouldEndGame())
            {
                // 게임 종료
                await gameManager.AnnounceGameWinner();
                await Task.Delay(2500);
                await gameManager.StartWaitingStateAsync();
            }
            else
            {
                // 다음 라운드 시작
                int nextRound = gameManager.Round + 1;
                await gameManager.StartRoundStateAsync(nextRound);
            }
        }
        else if ((int)Math.Round(prev) != (int)Math.Round(gameManager.ShopTimer))
            await gameManager.BroadcastToAll(new ShopTimeElapsed { ElapsedTime = gameManager.ShopTimer });
    }

    /// <summary>
    /// 봇들의 AI를 업데이트합니다
    /// </summary>
    private async Task UpdateBotAI(GameManager gameManager)
    {
        var bots = gameManager.Players.Values.OfType<Bot>().ToList();
        var tasks = from b in bots select b.UpdateAI(DeltaTime);
        await Task.WhenAll(tasks);
    }
}