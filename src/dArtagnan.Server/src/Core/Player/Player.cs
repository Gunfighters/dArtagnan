using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

public class Player
{
    public readonly int Id;
    public readonly string Nickname;
    public int Accuracy;
    public float Range;
    public EnergyData EnergyData; // 에너지 데이터
    public int MinEnergyToShoot; // 사격하기 위한 최소 필요 에너지
    public bool Alive;
    public Player? Target;
    public MovementData MovementData;
    public int Balance;
    public bool Bankrupt => Balance <= 0;
    public int AccuracyState; // 정확도 상태: -1(감소), 0(유지), 1(증가)
    public List<int> Augments; // 보유한 증강 ID 리스트
    public int CurrentItem; // 현재 소지한 아이템 ID (-1이면 없음)
    public bool IsCreatingItem; // 아이템 제작 중인지 여부
    public float CreatingRemainingTime; // 아이템 제작 남은 시간
    private float accuracyTimer; // 정확도 업데이트를 위한 타이머
    
    // 아이템 효과 관련 필드
    public float SpeedBoostTimer; // 속도 증가 남은 시간
    public float SpeedMultiplier; // 현재 속도 배율
    public float BaseSpeed; // 버프 적용 전 기본 속도
    public bool HasDamageShield; // 피해 가드 보유 여부
    public List<int> ActiveEffects; // 현재 활성화된 효과 목록 (아이템: 1~999, 증강: 1000+)

    public Player(int id, string nickname, Vector2 position)
    {
        Id = id;
        Nickname = nickname;
        BaseSpeed = Constants.MOVEMENT_SPEED;
        MovementData = new MovementData { Direction = 0, Position = position, Speed = BaseSpeed };
        Augments = [];
        ActiveEffects = new List<int>();
        InitToWaiting();
    }

    /// <summary>
    /// 대기 상태로 플레이어를 초기화 (게임 완전 초기화)
    /// </summary>
    public void InitToWaiting()
    {
        // 기본 능력치 초기화
        Accuracy = Random.Shared.Next(Constants.ROULETTE_MIN_ACCURACY, Constants.ROULETTE_MAX_ACCURACY + 1);
        Range = Constants.DEFAULT_RANGE;
        EnergyData = new EnergyData
        {
            MaxEnergy = Constants.DEFAULT_MAX_ENERGY,
            CurrentEnergy = 0
        };
        UpdateMinEnergyToShoot();
        
        // 생존 상태 초기화
        Alive = true;
        Target = null;
        
        // 경제 시스템 초기화
        Balance = 200;
        
        // 게임 시스템 초기화
        AccuracyState = 0;
        Augments.Clear();
        CurrentItem = -1;
        
        // 아이템 제작 상태 초기화
        IsCreatingItem = false;
        CreatingRemainingTime = 0f;
        accuracyTimer = 0f;
        
        // 아이템 효과 초기화
        SpeedBoostTimer = 0f;
        SpeedMultiplier = 1f;
        BaseSpeed = Constants.MOVEMENT_SPEED;
        HasDamageShield = false;
        ActiveEffects.Clear();
        
        // 라운드 상태도 함께 초기화
        InitToRound();
    }

    /// <summary>
    /// 라운드 상태로 플레이어를 초기화
    /// </summary>
    public void InitToRound()
    {
        // 생존 상태
        Alive = true;
        Target = null;
        
        // 이동 상태 (위치는 GameManager에서 별도 설정)
        BaseSpeed = Constants.MOVEMENT_SPEED;
        MovementData = new MovementData { Direction = 0, Position = Vector2.Zero, Speed = BaseSpeed };
        
        // 전투 상태
        EnergyData = new EnergyData
        {
            MaxEnergy = Constants.DEFAULT_MAX_ENERGY,
            CurrentEnergy = 0
        };
        AccuracyState = 0;
        
        // 아이템 제작 상태
        IsCreatingItem = false;
        CreatingRemainingTime = 0f;
        accuracyTimer = 0f;
        
        // 아이템 효과 초기화 (라운드 시작 시)
        SpeedBoostTimer = 0f;
        SpeedMultiplier = 1f;
        BaseSpeed = Constants.MOVEMENT_SPEED;
        HasDamageShield = false;
        ActiveEffects.Clear();
    }

    public PlayerInformation PlayerInformation => new()
    {
        PlayerId = Id,
        Accuracy = Accuracy,
        Alive = Alive,
        Nickname = Nickname,
        EnergyData = EnergyData,
        MinEnergyToShoot = MinEnergyToShoot,
        Targeting = Target?.Id ?? -1,
        Range = Range,
        MovementData = MovementData,
        Balance = Balance,
        AccuracyState = AccuracyState,
        Augments = Augments,
        CurrentItem = CurrentItem,
        IsCreatingItem = IsCreatingItem,
        CreatingRemainingTime = CreatingRemainingTime,
        SpeedMultiplier = SpeedMultiplier,
        HasDamageShield = HasDamageShield,
        ActiveEffects = ActiveEffects,
    };

    /// <summary>
    /// index에 따른 원형 배치 위치.
    /// </summary>
    public static Vector2 GetSpawnPosition(int index)
    {
        var angle = index * (360f / Constants.MAX_PLAYER_COUNT) * (float)(Math.PI / 180);
        return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Constants.SPAWN_RADIUS;
    }

    public void UpdateMovementData(Vector2 position, int direction, float speed)
    {
        MovementData.Position = position;
        MovementData.Direction = direction;
        MovementData.Speed = speed;
    }

    public int Withdraw(int amount)
    {
        var actual = Math.Min(amount, Balance);
        Balance -= actual;
        return actual;
    }

    /// <summary>
    /// 정확도 상태를 설정합니다.
    /// </summary>
    /// <param name="accuracyState">-1: 감소, 0: 유지, 1: 증가</param>
    public void SetAccuracyState(int accuracyState)
    {
        AccuracyState = accuracyState;
        Console.WriteLine($"[정확도] 플레이어 {Id}의 정확도 상태 변경: {accuracyState} (현재 정확도: {Accuracy}%)");
    }

    /// <summary>
    /// AccuracyState에 따라 Accuracy, Range, MinEnergyToShoot을 업데이트합니다. 게임루프에서 호출
    /// </summary>
    /// <param name="deltaTime">프레임 시간</param>
    public bool UpdateByAccuracyState(float deltaTime)
    {
        if (AccuracyState == 0) return false; // 유지 상태면 처리하지 않음

        accuracyTimer += deltaTime * Constants.ACCURACY_STATE_RATE;

        // 1초마다 정확도 업데이트
        if (accuracyTimer >= Constants.ACCURACY_UPDATE_INTERVAL)
        {
            accuracyTimer = 0f;

            // 정확도 상태 두 배 적용 증강 체크
            int accuracyChange = AccuracyState;
            if (Augments.Contains((int)AugmentId.AccuracyStateDoubleApplication))
            {
                accuracyChange *= AugmentConstants.ACCURACY_STATE_DOUBLE_MULTIPLIER;
                Console.WriteLine($"[증강] 플레이어 {Id}: 정확도 상태 두 배 적용 ({AccuracyState} -> {accuracyChange})");
            }
            
            int newAccuracy = Accuracy + accuracyChange;

            // 정확도 범위 제한
            newAccuracy = Math.Clamp(newAccuracy, 1, 100);  

            if (newAccuracy != Accuracy)
            {
                float t = Math.Clamp(newAccuracy / (float)Constants.ROULETTE_MAX_ACCURACY, 0f, 1f);
                Range = Constants.MAX_RANGE + t * (Constants.MIN_RANGE - Constants.MAX_RANGE);
                Console.WriteLine($"[정확도] 플레이어 {Id}의 사거리 변경: {Range}");

                Accuracy = newAccuracy;
                bool energyStatsChanged = UpdateMinEnergyToShoot(); // 정확도 변경 시 사격 최소 필요 에너지 업데이트
                Console.WriteLine($"[정확도] 플레이어 {Id}의 정확도 변경: {Accuracy}% (상태: {AccuracyState})");
                return true; // 브로드 캐스트 필요
            }
        }

        return false; // 브로드 캐스트 필요 없음
    }

    /// <summary>
    /// 아이템 제작을 시작합니다.
    /// </summary>
    public void StartCreatingItem()
    {
        IsCreatingItem = true;
        CreatingRemainingTime = Constants.CREATING_DURATION;
        Console.WriteLine($"[아이템] 플레이어 {Id}가 아이템 제작 시작 ({Constants.CREATING_DURATION}초)");
    }

    /// <summary>
    /// 아이템 제작을 취소합니다.
    /// </summary>
    public void CancelCreatingItem()
    {
        IsCreatingItem = false;
        CreatingRemainingTime = 0f;
        Console.WriteLine($"[아이템] 플레이어 {Id}가 아이템 제작 취소");
    }

    /// <summary>
    /// 아이템 제작 타이머를 업데이트합니다. 게임 루프에서 호출됩니다.
    /// </summary>
    /// <param name="deltaTime">프레임 시간</param>
    /// <returns>아이템 제작이 완료되었으면 true</returns>
    public bool UpdateCreating(float deltaTime)
    {
        if (!IsCreatingItem) return false;

        CreatingRemainingTime -= deltaTime;

        if (CreatingRemainingTime <= 0f)
        {
            IsCreatingItem = false;
            CreatingRemainingTime = 0f;
            Console.WriteLine($"[아이템] 플레이어 {Id}의 아이템 제작 완료!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 아이템을 획득합니다.
    /// </summary>
    /// <param name="itemId">획득할 아이템 ID</param>
    public void AcquireItem(int itemId)
    {
        CurrentItem = itemId;
        Console.WriteLine($"[아이템] 플레이어 {Id}가 아이템 {itemId} 획득");
    }

    /// <summary>
    /// 아이템을 사용합니다.
    /// </summary>
    /// <returns>사용한 아이템 ID, 아이템이 없으면 -1</returns>
    public int UseItem()
    {
        if (CurrentItem == -1) return -1;

        int usedItem = CurrentItem;
        CurrentItem = -1;
        Console.WriteLine($"[아이템] 플레이어 {Id}가 아이템 {usedItem} 사용");
        return usedItem;
    }

    /// <summary>
    /// 에너지를 소모합니다. 에너지 소모 시에는 클라이언트 동기화를 위해 브로드캐스트가 필요합니다.
    /// </summary>
    /// <param name="amount">소모할 에너지 양</param>
    /// <returns>에너지가 충분하면 true, 부족하면 false</returns>
    public bool ConsumeEnergy(int amount)
    {
        if (EnergyData.CurrentEnergy < amount) return false;
        
        EnergyData = new EnergyData
        {
            MaxEnergy = EnergyData.MaxEnergy,
            CurrentEnergy = EnergyData.CurrentEnergy - amount
        };
        Console.WriteLine($"[에너지] 플레이어 {Id}가 에너지 {amount} 소모 (현재: {EnergyData.CurrentEnergy:F1}/{EnergyData.MaxEnergy})");
        return true;
    }

    /// <summary>
    /// 에너지 회복을 연속적으로 업데이트합니다. 게임 루프에서 호출됩니다.
    /// 클라이언트에서도 동일하게 시뮬레이션하므로 브로드캐스트는 하지 않습니다.
    /// </summary>
    /// <param name="deltaTime">프레임 시간</param>
    public void UpdateEnergyRecovery(float deltaTime)
    {
        if (EnergyData.CurrentEnergy >= EnergyData.MaxEnergy) return;

        float oldEnergy = EnergyData.CurrentEnergy;
        float newEnergy = Math.Min(EnergyData.MaxEnergy, EnergyData.CurrentEnergy + Constants.ENERGY_RECOVERY_RATE * deltaTime);
        
        if (Math.Abs(newEnergy - oldEnergy) > 0.001f) // 미세한 변화도 감지
        {
            EnergyData = new EnergyData
            {
                MaxEnergy = EnergyData.MaxEnergy,
                CurrentEnergy = newEnergy
            };
            
            // 정수 단위로 넘어갈 때만 로그 출력
            if ((int)newEnergy > (int)oldEnergy)
            {
                Console.WriteLine($"[에너지] 플레이어 {Id}의 에너지 회복: {EnergyData.CurrentEnergy:F1}/{EnergyData.MaxEnergy}");
            }
        }
    }

    /// <summary>
    /// 정확도에 따른 사격 최소 필요 에너지를 업데이트합니다.
    /// 정확도 1~100%에 비례하여 최소 필요 에너지 (정비례)
    /// </summary>
    /// <returns>최소 필요 에너지가 변경되었으면 true</returns>
    public bool UpdateMinEnergyToShoot()
    {
        int oldMinEnergy = MinEnergyToShoot;
        
        // 정확도를 1~100 범위로 클램프하고, 1~DEFAULT_MAX_ENERGY 에너지로 매핑
        int clampedAccuracy = Math.Clamp(Accuracy, 1, 100);
        MinEnergyToShoot = Math.Max(1, (int)Math.Round(clampedAccuracy / 100.0f * Constants.DEFAULT_MAX_ENERGY));
        
        if (oldMinEnergy != MinEnergyToShoot)
        {
            Console.WriteLine($"[에너지] 플레이어 {Id}의 사격 최소 필요 에너지 업데이트: {oldMinEnergy} → {MinEnergyToShoot} (정확도: {Accuracy}%)");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 에너지 스탯이 변경되었는지 확인합니다.
    /// </summary>
    /// <param name="oldMinEnergyToShoot">이전 사격 최소 필요 에너지</param>
    /// <returns>변경되었으면 true</returns>
    public bool HasEnergyStatsChanged(int oldMinEnergyToShoot)
    {
        return MinEnergyToShoot != oldMinEnergyToShoot;
    }

    /// <summary>
    /// 최대 에너지를 업데이트합니다.
    /// </summary>
    /// <param name="newMaxEnergy">새로운 최대 에너지</param>
    /// <returns>최대 에너지가 변경되었으면 true</returns>
    public bool UpdateMaxEnergy(int newMaxEnergy)
    {
        if (EnergyData.MaxEnergy == newMaxEnergy) return false;
        
        EnergyData = new EnergyData
        {
            MaxEnergy = newMaxEnergy,
            CurrentEnergy = Math.Min(EnergyData.CurrentEnergy, newMaxEnergy) // 현재 에너지가 최대를 넘지 않도록
        };
        Console.WriteLine($"[에너지] 플레이어 {Id}의 최대 에너지 변경: {newMaxEnergy}");
        return true;
    }

    /// <summary>
    /// 속도 버프를 적용합니다.
    /// </summary>
    /// <param name="duration">지속시간</param>
    /// <param name="multiplier">속도 배율</param>
    public void ApplySpeedBoost(float duration, float multiplier)
    {
        SpeedBoostTimer = duration;
        SpeedMultiplier = multiplier;
        
        // 배율 적용된 속도 업데이트
        MovementData.Speed = BaseSpeed * SpeedMultiplier;
        
        // 활성 효과 목록에 추가 (중복 방지)
        if (!ActiveEffects.Contains((int)ItemId.SpeedBoost))
        {
            ActiveEffects.Add((int)ItemId.SpeedBoost);
        }
        
        Console.WriteLine($"[버프] 플레이어 {Id}에게 속도 증가 적용 ({multiplier}배, {duration}초)");
    }

    /// <summary>
    /// 속도 버프 타이머를 업데이트합니다.
    /// </summary>
    /// <param name="deltaTime">프레임 시간</param>
    /// <returns>버프가 종료되었으면 true</returns>
    public bool UpdateSpeedBoost(float deltaTime)
    {
        if (SpeedBoostTimer <= 0) return false;
        
        SpeedBoostTimer -= deltaTime;
        
        if (SpeedBoostTimer <= 0)
        {
            SpeedBoostTimer = 0f;
            SpeedMultiplier = 1f;
            
            // 원래 속도로 복구
            MovementData.Speed = BaseSpeed;
            
            // 활성 효과 목록에서 제거
            ActiveEffects.Remove((int)ItemId.SpeedBoost);
            
            Console.WriteLine($"[버프] 플레이어 {Id}의 속도 증가 종료");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 피해 가드를 적용합니다.
    /// </summary>
    public void ApplyDamageShield()
    {
        HasDamageShield = true;
        
        // 활성 효과 목록에 추가 (중복 방지)
        if (!ActiveEffects.Contains((int)ItemId.DamageShield))
        {
            ActiveEffects.Add((int)ItemId.DamageShield);
        }
        
        Console.WriteLine($"[버프] 플레이어 {Id}에게 피해 가드 적용");
    }

    /// <summary>
    /// 피해 가드를 소모합니다.
    /// </summary>
    /// <returns>가드가 있었으면 true</returns>
    public bool ConsumeDamageShield()
    {
        if (!HasDamageShield) return false;
        
        HasDamageShield = false;
        
        // 활성 효과 목록에서 제거
        ActiveEffects.Remove((int)ItemId.DamageShield);
        
        Console.WriteLine($"[버프] 플레이어 {Id}의 피해 가드 소모");
        return true;
    }

    /// <summary>
    /// 에너지를 회복합니다.
    /// </summary>
    /// <param name="amount">회복량</param>
    public void RestoreEnergy(int amount)
    {
        var oldEnergy = EnergyData.CurrentEnergy;
        EnergyData = new EnergyData
        {
            MaxEnergy = EnergyData.MaxEnergy,
            CurrentEnergy = Math.Min(EnergyData.MaxEnergy, EnergyData.CurrentEnergy + amount)
        };
        Console.WriteLine($"[에너지] 플레이어 {Id}의 에너지 회복: {oldEnergy:F1} → {EnergyData.CurrentEnergy:F1}");
    }
}