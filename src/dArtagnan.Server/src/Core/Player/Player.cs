using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

public class Player
{
    public readonly int Id;
    public readonly string Nickname;
    public int Accuracy;
    public float Range;
    public float TotalReloadTime; // 총 재장전시간 (사격 시 설정)
    public float RemainingReloadTime; // 현재 남은 재장전시간
    public bool Alive;
    public Player? Target;

    public int Direction;
    public Vector2 Position;
    public float Speed;

    public int Balance;
    public bool Bankrupt => Balance <= 0;
    public int AccuracyState; // 정확도 상태: -1(감소), 0(유지), 1(증가)
    public List<ItemId> Items; // 보유한 아이템 리스트
    public List<int> AccuracyChangeCosts; // [감소, 유지, 증가] 순서의 비용
    public bool IsMining; // 채굴 중인지 여부
    public float MiningRemainingTime; // 채굴 남은 시간
    public ShopData ShopData; // 상점 상태일 때만 존재
    public List<int> RoulettePool; // 플레이어의 룰렛 풀 (InitialRoulette에서 생성되어 게임 끝까지 유지)
    public int NextDeductionAmount; // 다음 차감 금액
    public bool Fury; // 폭주 모드 (다음 징수금 >= 소지금)

    //타이머
    public float AccuracyUpdateTimer;
    public float LifeInsuranceTimer;
    public float SteadyAimTimer; // 정조준 업데이트 타이머

    // 아이템 효과 관련
    public float SteadyAimStack; // 정조준 누적 사거리 보너스

    protected readonly GameManager _gameManager;

    public Player(int id, string nickname, Vector2 position, GameManager gameManager)
    {
        Id = id;
        Nickname = nickname;
        Items = [];
        AccuracyChangeCosts = [10, 0, 50];
        RoulettePool = [];
        _gameManager = gameManager;

        // 이동 관련 필드 초기화
        Direction = 0;
        Position = position + new Vector2(0.5f, 0.5f);
        Speed = Constants.MOVEMENT_SPEED;

        // InitToWaiting()은 외부에서 수동 호출 필요
    }

    public bool InCell(Cell cell)
        => Math.Abs(Position.X - (cell.X + 0.5f)) < 0.25f && Math.Abs(Position.Y - (cell.Y + 0.5f)) < 0.25f;

    public Cell InWhichCell() =>
        new((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y));

    public static int PickAccuracy(int min, int max, bool weighted)
    {
        if (!weighted) return Random.Shared.Next(min, max + 1);
        return GetRightSkewedValueByPower(min, max);
    }
    
    /// <summary>
    /// 왜도가 skewStrength인 정규분포를 만들고 무작위 값을 추출.
    /// </summary>
    /// <param name="lower">The minimum value (inclusive, e.g., 1).</param>
    /// <param name="upper">The maximum value (inclusive, e.g., 100).</param>
    /// <param name="skewStrength">A double between 0.0 and 1.0. Lower values 
    ///                             result in a stronger skew (more values near 'lower').
    ///                             A value of 0.5 provides a strong starting skew.</param>
    /// <returns>An integer in the range [lower, upper] with right-skewed probability.</returns>
    private static int GetRightSkewedValueByPower(int lower, int upper, double skewStrength = 0.9)
    {
        // 1. Generate a uniform random double in [0.0, 1.0)
        var r = Random.Shared.NextDouble();

        // 2. Apply skew: r^power. 
        //    If power < 1.0 (e.g., 0.5), it skews the result towards 1.0.
        //    We want the skew towards 'lower', which corresponds to 0.0 in the [0, 1] range.
        var skewedR = Math.Pow(r, skewStrength);

        // 3. Invert the skew: (1.0 - skewed_r) flips the distribution to concentrate
        //    results toward 0.0.
        var invertedR = 1.0 - skewedR;

        // 4. Scale and shift to the desired range [lower, upper]
        //    We use (upper - lower + 1) for the range size.
        var rangeSize = (double)upper - lower + 1;

        // Apply scaling and shift, then round down (Floor) to get an integer index.
        var result = (int)Math.Floor(lower + invertedR * rangeSize);
        
        // Ensure the result is clamped within the exact [lower, upper] integer range.
        return Math.Clamp(result, lower, upper);
    }

    /// <summary>
    /// 대기 상태로 플레이어를 초기화 (게임 완전 초기화)
    /// </summary>
    public async Task InitToWaiting()
    {
        // 1. 생존 상태 초기화
        Alive = true;

        // 2. 아이템 초기화
        Items = [ItemId.DebuffRange, ItemId.DebuffTotalReloadTime];

        // 3. 전투 상태 초기화
        Target = null;
        AccuracyState = 0;
        AccuracyChangeCosts = [0, 0, 0];

        // 4. 경제 시스템 초기화
        Balance = Constants.INITIAL_BALANCE;
        NextDeductionAmount = 0;
        Fury = false;

        // 5. 채굴 상태 초기화
        IsMining = false;
        MiningRemainingTime = 0f;

        // 6. 타이머 초기화
        AccuracyUpdateTimer = 0f;
        LifeInsuranceTimer = 0f;
        SteadyAimTimer = 0f;
        SteadyAimStack = 0f;

        // 7. 능력치 초기화 (Fury 상태 반영 포함)
        Accuracy = 0;
        await UpdateRange();
        await UpdateTotalReloadTime();
        await UpdateSpeed();
        RemainingReloadTime = TotalReloadTime;

        // 8. 위치 초기화
        Direction = 0;
        Position = _gameManager.SpawnPointOnRoomEntry.ToVec() + new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// 라운드 상태로 플레이어를 초기화 (랜덤 스폰 위치에 배치)
    /// </summary>
    public virtual async Task InitToRound()
    {
        // 1. 생존 상태 초기화
        Alive = true;

        // 2. 전투 상태 초기화
        Target = null;
        AccuracyState = 0;
        AccuracyChangeCosts = [0, 0, 0];

        // 3. 채굴 상태 초기화
        IsMining = false;
        MiningRemainingTime = 0f;

        // 4. 타이머 초기화
        AccuracyUpdateTimer = 0f;
        SteadyAimTimer = 0f;
        SteadyAimStack = 0f;

        // 5. 능력치 재계산 (현재 Fury, 아이템 상태 반영)
        await UpdateRange();
        await UpdateTotalReloadTime();
        await UpdateSpeed();
        RemainingReloadTime = TotalReloadTime;

        // 6. 위치 초기화
        var shuffled = _gameManager.SpawnPointOnRoundStart.ToList().OrderBy(_ => Random.Shared.Next());
        var spawnAt = shuffled.First(pair => !pair.Value).Key;
        Direction = 0;
        Position = spawnAt.ToVec() + new Vector2(0.5f, 0.5f);
        _gameManager.SpawnPointOnRoundStart[spawnAt] = true; // Taken
    }

    public PlayerInformation PlayerInformation => new()
    {
        PlayerId = Id,
        Accuracy = Accuracy,
        Alive = Alive,
        Nickname = Nickname,
        CostumeId = CostumeId,
        Targeting = Target?.Id ?? -1,
        Range = Range,
        MovementData = new MovementData
        {
            Direction = Direction,
            Position = Position,
            Speed = Speed
        },
        Balance = Balance,
        AccuracyState = AccuracyState,
        AccuracyChangeCosts = AccuracyChangeCosts,
        TotalReloadTime = TotalReloadTime,
        RemainingReloadTime = RemainingReloadTime,
        IsMining = IsMining,
        MiningRemainingTime = MiningRemainingTime,
        OwnedItems = Items.ToList(),
        NextDeductionAmount = NextDeductionAmount,
        Fury = Fury
    };

    public async Task UpdateMovementData(MovementData data)
    {
        // 변경사항이 있는지 확인
        if (Direction != data.Direction || Position != data.Position || Math.Abs(Speed - data.Speed) > 0.0001f)
        {
            Direction = data.Direction;
            Position = data.Position;
            Speed = data.Speed;

            await _gameManager.BroadcastToAll(new MovementDataBroadcast
            {
                PlayerId = Id,
                MovementData = data
            });
        }
    }

    /// <summary>
    /// 이동속도를 업데이트합니다 (Boots 아이템 효과 및 폭주 모드 반영)
    /// </summary>
    public async Task UpdateSpeed()
    {
        var oldSpeed = Speed;

        // 장화 효과: 개수당 속도 증가
        float bootsBonus = GetItemCount(ItemId.Boots) * ItemConstants.Boots.SPEED_INCREASE;

        // 폭주 모드 효과: 속도 배율 적용
        float furyMultiplier = 1f;
        if (Fury)
        {
            furyMultiplier = Constants.FURY_SPEED_MULTIPLIER;

            // 광기의 연료 아이템 효과: 폭주 모드 추가 속도 배율
            if (GetItemCount(ItemId.Berserker) > 0)
            {
                furyMultiplier *= ItemConstants.Underflow.FURY_SPEED_MULTIPLIER;
            }
        }

        // 최종 속도 계산
        float newSpeed = (Constants.MOVEMENT_SPEED + bootsBonus) * furyMultiplier;
        newSpeed = Math.Clamp(newSpeed, Constants.MIN_SPEED, Constants.MAX_SPEED);

        if (Math.Abs(oldSpeed - newSpeed) > 0.0001f)
        {
            Speed = newSpeed;

            await _gameManager.BroadcastToAll(new UpdateSpeedBroadcast
            {
                PlayerId = Id,
                Speed = newSpeed
            });
            Logger.log($"[Player][{Nickname}] 속도 변경: {oldSpeed:F2} → {newSpeed:F2} (장화: {GetItemCount(ItemId.Boots)}, 폭주: {Fury}, 광기연료: {GetItemCount(ItemId.Berserker) > 0})");
        }
    }

    public async Task<int> Withdraw(int amount, bool broadcast = true)
    {
        var oldBalance = Balance;
        var wasBankrupt = Bankrupt;
        var actual = Math.Min(amount, Balance);
        Balance -= actual;

        if(oldBalance != Balance)
        {
            if(broadcast)
            {
                await _gameManager.BroadcastToAll(new BalanceUpdateBroadcast
                {
                    PlayerId = Id,
                    Balance = Balance
                });
            }

            if(Bankrupt && !wasBankrupt)
            {
                //Todo: BalanceUpdateBroadcast 왜 두번?
                await _gameManager.BroadcastToAll(new BalanceUpdateBroadcast
                {
                    PlayerId = Id,
                    Balance = Balance
                });

                await _gameManager.BroadcastToAll(new ChatBroadcast
                {
                    PlayerId = -1,
                    Message = $"{Nickname}님이 파산했습니다!"
                });
                Logger.log($"[Player][{Nickname}] 파산 발생");
                await Die();
            }

            await UpdateFuryState();
        }

        return actual;
    }

    public async Task Deposit(int amount, bool broadcast = true)
    {
        var oldBalance = Balance;
        Balance += amount;

        if(oldBalance != Balance)
        {
            if(broadcast)
            {
                await _gameManager.BroadcastToAll(new BalanceUpdateBroadcast
                {
                    PlayerId = Id,
                    Balance = Balance
                });
            }

            await UpdateFuryState();
        }
    }

    public async Task UpdateRange()
    {
        var oldRange = Range;

        const float MIN_RANGE = 0.5f;
        // 디버프 개수에 비례하고 Accuracy에 반비례하도록 구성
        float debuffFactor = 1f - GetItemCount(ItemId.DebuffRange) * 0.4f * (Accuracy / 100f);
        // 최종식
        float newRange = Constants.DEFAULT_RANGE * debuffFactor + SteadyAimStack;
        // 최소 사거리 보장
        newRange = Math.Max(newRange, MIN_RANGE);

        Range = newRange;

        if(Math.Abs(oldRange - Range) > 0.01f)
        {
            await _gameManager.BroadcastToAll(new UpdateRangeBroadcast
            {
                PlayerId = Id,
                Range = Range
            });
            Logger.log($"[Player][{Nickname}] 사거리 변경: {oldRange:F1} → {Range:F1}m");
        }
    }


    public async Task UpdateTotalReloadTime()
    {
        var oldTotalReloadTime = TotalReloadTime;

        const float MIN_RELOAD_TIME = 0.5f;
        float debuffFactor = 1f + Accuracy / 100f * (GetItemCount(ItemId.DebuffTotalReloadTime) * 0.5f);
        float fastReloadFactor = GetItemCount(ItemId.FastReload) * ItemConstants.FastReload.RELOAD_TIME_REDUCTION;
        // 최종식
        float newReloadTime = Constants.BASE_RELOAD_TIME * debuffFactor - fastReloadFactor;
        // 최소 재장전시간 보장
        newReloadTime = Math.Max(newReloadTime, MIN_RELOAD_TIME);

        TotalReloadTime = newReloadTime;
        // 총재장전 시간이 남은 재장전시간보다 짧으면 남은 재장전시간을 총재장전시간으로 설정
        RemainingReloadTime = Math.Min(TotalReloadTime, RemainingReloadTime);

        if(Math.Abs(oldTotalReloadTime - TotalReloadTime) > 0.01f)
        {
            await _gameManager.BroadcastToAll(new ReloadTimeBroadcast
            {
                PlayerId = Id,
                TotalReloadTime = TotalReloadTime,
                RemainingReloadTime = RemainingReloadTime
            });
            Logger.log($"[Player][{Nickname}] 재장전시간 변경: {oldTotalReloadTime:F1} → {TotalReloadTime:F1}초");
        }
    }

    public async Task SetAccuracy(int accuracy)
    {
        var oldAccuracy = Accuracy;
        
        Accuracy = Math.Clamp(accuracy, 1, 100);

        if(oldAccuracy != Accuracy)
        {
            await _gameManager.BroadcastToAll(new UpdateAccuracyBroadcast
            {
                PlayerId = Id,
                Accuracy = Accuracy
            });
            Logger.log($"[Player][{Nickname}] 정확도 변경: {oldAccuracy} → {Accuracy}%");

            //정확도에 종속적인 아이템(디버프) 효과들 땜애 사거리와 재장전시간 업데이트
            await UpdateRange();
            await UpdateTotalReloadTime();
        }
    }

    //todo: 플레이어 스탯 변경시 다른 스탯들 업데이트 및 브로드캐스트 타이밍 일관성있게 변경
    //현재는 정확도 로직이 혼자 독립적으로 권위적으로 존재함
    // public async Task UpdatePlayerStats()
    // {
    //     await UpdateRange();
    //     await UpdateTotalReloadTime();
    // }

    public async Task Die()
    {
        var oldAlive = Alive;
        if(GetItemCount(ItemId.LifeInsurance) > 0)
        {
            await RemoveItem(ItemId.LifeInsurance);
            await Deposit(ItemConstants.LifeInsurance.DEATH_REWARD);
        }
        Alive = false;
        if(oldAlive != Alive)
        {
            await _gameManager.BroadcastToAll(new UpdatePlayerAlive
            {
                PlayerId = Id,
                Alive = Alive
            });
        }

        Direction = 0;
        await _gameManager.EnqueueCommandAsync(new PlayerMovementCommand
            {
                MovementData = new MovementData
                {
                    Direction = Direction,
                    Position = Position,
                    Speed = Speed
                },
                PlayerId = Id
            });
        Logger.log($"[Player][{Nickname}] 사망");
    }

    /// <summary>
    /// 정확도 상태를 설정합니다.
    /// </summary>
    /// <param name="accuracyState">-1: 감소, 0: 유지, 1: 증가</param>
    public async Task SetAccuracyState(int accuracyState)
    {
        var oldAccuracyState = AccuracyState;
        AccuracyState = accuracyState;
        if(oldAccuracyState != AccuracyState)
        {
            await _gameManager.BroadcastToAll(new UpdateAccuracyStateBroadcast
            {
                PlayerId = Id,
                AccuracyState = accuracyState
            });
            Logger.log($"[Player][{Nickname}] 정확도 상태 변경: {accuracyState} (현재: {Accuracy}%)");
        }
    }

    /// <summary>
    /// 채굴을 시작합니다.
    /// </summary>
    public async Task StartMining()
    {
        var oldIsMining = IsMining;
        IsMining = true;
        if(oldIsMining != IsMining)
        {
            MiningRemainingTime = Constants.MINING_DURATION;
            await _gameManager.BroadcastToAll(new MiningStateBroadcast
            {
                PlayerId = Id,
                IsMining = IsMining
            });
            Logger.log($"[Player][{Nickname}] 채굴 시작");
        }
    }

    /// <summary>
    /// 채굴을 중단합니다.
    /// </summary>
    public async Task StopMining()
    {
        var oldIsMining = IsMining;
        IsMining = false;
        MiningRemainingTime = 0f;
        if(oldIsMining != IsMining)
        {
            await _gameManager.BroadcastToAll(new MiningStateBroadcast
            {
                PlayerId = Id,
                IsMining = IsMining
            });
            Logger.log($"[Player][{Nickname}] 채굴 중단");
        }
    }

    /// <summary>
    /// 채굴 타이머를 업데이트합니다. 게임 루프에서 호출됩니다.
    /// </summary>
    /// <param name="deltaTime">프레임 시간</param>
    /// <returns>채굴이 완료되었으면 true</returns>
    public async Task<bool> UpdateMining(float deltaTime)
    {
        if (!IsMining) return false;

        MiningRemainingTime -= deltaTime;

        if (MiningRemainingTime <= 0f)
        {
            await StopMining();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 아이템을 추가합니다.
    /// </summary>
    /// <param name="itemId">추가할 아이템 ID</param>
    public async Task AddItem(ItemId itemId)
    {
        var itemTags = ItemConstants.Items[itemId].Tags;
        if (itemTags.HasFlag(ItemTags.Instant))
        {
            switch (itemId)
            {
                case ItemId.GiftBox:
                    bool GiftBox = Random.Shared.NextSingle() * 100f < ItemConstants.GiftBox.GIFTBOX_CHANCE;

                    if (GiftBox)
                    {
                        await Deposit(ItemConstants.GiftBox.GIFTBOX_REWARD);
                        await _gameManager.BroadcastToAll(new ChatBroadcast
                        {
                            PlayerId = -1,
                            Message = $"{Nickname}님이 잭팟 대박! {ItemConstants.GiftBox.GIFTBOX_REWARD}달러 획득!"
                        });
                    }
                    else
                    {
                        await _gameManager.BroadcastToAll(new ChatBroadcast
                        {
                            PlayerId = -1,
                            Message = $"{Nickname}님의 잭팟 꽝... 다음 기회에!"
                        });
                    }
                    break;
                    
                case ItemId.NewRoulette:
                    RoulettePool.Clear();
                    for (int i = 0; i < 8; i++)
                    {
                        RoulettePool.Add(PickAccuracy(Constants.INITIAL_ROULETTE_MIN_ACCURACY, Constants.INITIAL_ROULETTE_MAX_ACCURACY + 1, true));
                    }

                    var currentShopData = ShopData;
                    currentShopData.ShopRoulettePool = RoulettePool;
                    ShopData = currentShopData;

                    Logger.log($"[Item][{Nickname}] 새 돌림판 사용: 룰렛 풀 [{string.Join(", ", RoulettePool)}]");

                    break;

                case ItemId.Scope:
                    await RemoveItem(ItemId.DebuffRange, false);
                    break;
            }
        }
        else if (itemTags.HasFlag(ItemTags.Stackable) || (!itemTags.HasFlag(ItemTags.Stackable) && !Items.Contains(itemId)))
        {
            Items.Add(itemId);

            switch(itemId)
            {
                case ItemId.LifeInsurance:
                    LifeInsuranceTimer = 0f;
                    break;
                case ItemId.VipTicket:
                    await UpdateNextDeductionAmount();
                    break;
                case ItemId.Boots:
                case ItemId.Berserker:
                    await UpdateSpeed();
                    break;
            }

            await _gameManager.BroadcastToAll(new UpdateOwnedItemsBroadcast
            {
                PlayerId = Id,
                OwnedItems = Items.ToList()
            });
            Logger.log($"[Item][{Nickname}] 아이템 획득: {itemId}");
        }
    }

    /// <summary>
    /// 아이템을 제거합니다.
    /// </summary>
    /// <param name="itemId">제거할 아이템 ID</param>
    /// <param name="isUsed">본인이 아이템을 직접 사용하여 아이템이 제거되어 클라이언트 효과용 브로드캐스트 필요 = true, 수동적으로 없어짐 = false</param>
    /// <returns>제거 성공 여부</returns>
    public async Task<bool> RemoveItem(ItemId itemId, bool isUsed = true)
    {
        bool removed = Items.Remove(itemId);
        if (removed)
        {
            switch(itemId)
            {
                case ItemId.LifeInsurance:
                    LifeInsuranceTimer = 0f;
                    break;
                case ItemId.SteadyAim:
                    SteadyAimStack = 0f;
                    break;
                case ItemId.VipTicket:
                    await UpdateNextDeductionAmount();
                    break;
                case ItemId.Boots:
                case ItemId.Berserker:
                    await UpdateSpeed();
                    break;
            }
            if(isUsed)
            {
                await _gameManager.BroadcastToAll(new ItemUsedBroadcast
                {
                    PlayerId = Id,
                    ItemId = itemId,
                });
            }
            await _gameManager.BroadcastToAll(new UpdateOwnedItemsBroadcast
            {
                PlayerId = Id,
                OwnedItems = Items.ToList()
            });
            Logger.log($"[Item][{Nickname}] 아이템 사용/제거: {itemId}");
        }
        return removed;
    }

    /// <summary>
    /// 특정 아이템을 보유 갯수를 반환합니다.
    /// </summary>
    public int GetItemCount(ItemId itemId)
    {
        return Items.Count(item => item == itemId);
    }

    /// <summary>
    /// 사격 시 재장전시간을 설정합니다.
    /// </summary>
    public async Task StartReloading()
    {
        RemainingReloadTime = TotalReloadTime;

        await _gameManager.BroadcastToAll(new ReloadTimeBroadcast
        {
            PlayerId = Id,
            TotalReloadTime = TotalReloadTime,
            RemainingReloadTime = RemainingReloadTime
        });
        
        Logger.log($"[Player][{Nickname}] 재장전 시작: {TotalReloadTime:F1}초");
    }

    /// <summary>
    /// 사격 가능 여부를 확인합니다.
    /// </summary>
    public bool IsNotReloading()
    {
        return RemainingReloadTime <= 0f;
    }

    /// <summary>
    /// VipTicket 보유 여부에 따라 차감 금액을 계산하고 브로드캐스트합니다.
    /// N회 징수마다 2배씩 증가합니다.
    /// </summary>
    public async Task UpdateNextDeductionAmount()
    {
        int multiplier = (int)Math.Pow(2, _gameManager.DeductionCount / Constants.DEDUCTION_MULTIPLY_PERIOD);
        int baseAmount = _gameManager.BaseBettingAmount * multiplier;

        // VipTicket 보유 시 BETTING_REDUCTION 비율만큼 감소 (25% 감소)
        int newAmount = GetItemCount(ItemId.VipTicket) > 0
            ? Math.Max(1, (int)(baseAmount * (1f - ItemConstants.VipTicket.BETTING_REDUCTION)))
            : baseAmount;

        if (NextDeductionAmount != newAmount)
        {
            NextDeductionAmount = newAmount;
            await _gameManager.BroadcastToAll(new NextDeductionAmountUpdateBroadcast
            {
                PlayerId = Id,
                NextDeductionAmount = NextDeductionAmount
            });
            Logger.log($"[Player][{Nickname}] 다음 차감 금액: {NextDeductionAmount}달러 (VIP할인: {GetItemCount(ItemId.VipTicket) > 0})");
        
            await UpdateFuryState();
        }
    }

    /// <summary>
    /// 폭주 모드 상태를 업데이트합니다 (다음 징수금 >= 소지금일 때 활성화)
    /// </summary>
    public async Task UpdateFuryState()
    {
        var oldFury = Fury;
        Fury = NextDeductionAmount >= Balance;

        if (oldFury != Fury)
        {
            await _gameManager.BroadcastToAll(new FuryStateBroadcast
            {
                PlayerId = Id,
                Fury = Fury
            });
            Logger.log($"[Player][{Nickname}] 폭주 모드: {Fury} (소지금: {Balance}, 다음 차감: {NextDeductionAmount})");

            if (Fury && _gameManager.CurrentGameState == GameState.Round && Alive)
            {
                await _gameManager.BroadcastToAll(new ChatBroadcast
                {
                    PlayerId = -1,
                    Message = $"{Nickname}님이 파산 직전! 속도가 증가합니다"
                });
            }

            await UpdateSpeed();
        }
    }
}