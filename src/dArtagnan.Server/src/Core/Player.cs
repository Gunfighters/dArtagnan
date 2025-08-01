using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

public class Player(int id, string nickname, Vector2 position)
{
    public readonly int Id = id;
    public readonly string Nickname = nickname;
    public int Accuracy;
    public float Range = Constants.DEFAULT_RANGE;
    public float TotalReloadTime = Constants.DEFAULT_RELOAD_TIME;
    public float RemainingReloadTime = Constants.DEFAULT_RELOAD_TIME;
    public bool Alive = true;
    public Player? Target;
    public MovementData MovementData = new() { Direction = 0, Position = position, Speed = Constants.MOVEMENT_SPEED };
    public int Balance = 200;
    public bool Bankrupt => Balance <= 0;
    public int AccuracyState = 0; // 정확도 상태: -1(감소), 0(유지), 1(증가)
    public List<int> Augments = []; // 보유한 증강 ID 리스트
    public int CurrentItem = -1; // 현재 소지한 아이템 ID (-1이면 없음)
    public bool IsCreatingItem = false; // 아이템 제작 중인지 여부
    public float CreatingRemainingTime = 0f; // 아이템 제작 남은 시간
    private float accuracyTimer = 0f; // 정확도 업데이트를 위한 타이머
    private const float ACCURACY_UPDATE_INTERVAL = 1.0f; // 정확도 업데이트 간격 (1초)

    /// <summary>
    /// 대기 상태로 플레이어를 초기화 (게임 완전 초기화)
    /// </summary>
    public void InitToWaiting(int accuracy)
    {
        // 기본 능력치 초기화
        Accuracy = accuracy;
        TotalReloadTime = accuracy == 0
            ? Constants.DEFAULT_RELOAD_TIME
            : accuracy / 100f * 1.5f * Constants.DEFAULT_RELOAD_TIME;
        Range = Constants.DEFAULT_RANGE;
        
        // 경제 시스템 초기화
        Balance = 200;
        
        // 게임 시스템 초기화
        AccuracyState = 0;
        Augments.Clear();
        CurrentItem = -1;
        
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
        MovementData = new MovementData { Direction = 0, Position = Vector2.Zero, Speed = Constants.MOVEMENT_SPEED };
        
        // 전투 상태
        RemainingReloadTime = TotalReloadTime;
        
        // 아이템 제작 상태
        IsCreatingItem = false;
        CreatingRemainingTime = 0f;
        accuracyTimer = 0f;
    }

    public PlayerInformation PlayerInformation => new()
    {
        PlayerId = Id,
        Accuracy = Accuracy,
        Alive = Alive,
        Nickname = Nickname,
        RemainingReloadTime = RemainingReloadTime,
        TotalReloadTime = TotalReloadTime,
        Targeting = Target?.Id ?? -1,
        Range = Range,
        MovementData = MovementData,
        Balance = Balance,
        AccuracyState = AccuracyState,
        Augments = Augments,
        CurrentItem = CurrentItem,
        IsCreatingItem = IsCreatingItem,
        CreatingRemainingTime = CreatingRemainingTime,
    };

    public static int GenerateRandomAccuracy()
    {
        return Random.Shared.Next(Constants.MIN_ACCURACY, Constants.MAX_ACCURACY + 1);
    }

    /// <summary>
    /// index에 따른 원형 배치 위치.
    /// </summary>
    public static Vector2 GetSpawnPosition(int index)
    {
        const int magicNumber = 53;
        var angle = index * magicNumber * (float)(Math.PI / 180);
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
    /// 정확도를 업데이트합니다. 게임 루프에서 호출됩니다.
    /// </summary>
    /// <param name="deltaTime">프레임 시간</param>
    public bool UpdateAccuracy(float deltaTime)
    {
        if (AccuracyState == 0) return false; // 유지 상태면 처리하지 않음

        accuracyTimer += deltaTime;

        // 1초마다 정확도 업데이트
        if (accuracyTimer >= ACCURACY_UPDATE_INTERVAL)
        {
            accuracyTimer = 0f;

            int newAccuracy = Accuracy + AccuracyState;

            // 정확도 범위 제한
            newAccuracy = Math.Clamp(newAccuracy, Constants.MIN_ACCURACY, Constants.MAX_ACCURACY);

            if (newAccuracy != Accuracy)
            {
                Accuracy = newAccuracy;
                Console.WriteLine($"[정확도] 플레이어 {Id}의 정확도 변경: {Accuracy}% (상태: {AccuracyState})");
                return true; // 정확도가 실제로 변경됨
            }
        }

        return false; // 정확도 변경 없음
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
}