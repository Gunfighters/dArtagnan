using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

public class Player(int id, string nickname, Vector2 position)
{
    public readonly int Id = id;
    public readonly string Nickname = nickname;
    public int Accuracy = GenerateRandomAccuracy();
    public float Range = Constants.DEFAULT_RANGE;
        
    public float TotalReloadTime = Constants.DEFAULT_RELOAD_TIME;
    public float RemainingReloadTime = Constants.DEFAULT_RELOAD_TIME / 2;
    public bool Alive = true;
    public Player? Target;
    public MovementData MovementData = new() { Direction = 0, Position = position, Speed = Constants.WALKING_SPEED };
    public int Balance = 200;
    public bool Bankrupt => Balance <= 0;
    
    // 정확도 상태: -1(감소), 0(유지), 1(증가)
    public int AccuracyState = 0;
    
    // 정확도 업데이트를 위한 타이머
    private float accuracyTimer = 0f;
    
    // 정확도 업데이트 간격 (1초)
    private const float ACCURACY_UPDATE_INTERVAL = 1.0f;

    public void ResetForInitialGame()
    {
        TotalReloadTime = Constants.DEFAULT_RELOAD_TIME;
        Range = Constants.DEFAULT_RANGE;
        Balance = 200;
        AccuracyState = 0; // 초기 게임 시작 시 정확도 상태 초기화
        accuracyTimer = 0f;
    }

    public void ResetForNextRound()
    {
        Alive = true;
        Target = null;
        Accuracy = GenerateRandomAccuracy();
        MovementData = new MovementData { Direction = 0, Position = Vector2.Zero, Speed = Constants.WALKING_SPEED };
        RemainingReloadTime = TotalReloadTime / 2;
        AccuracyState = 0; // 라운드 시작 시 정확도 상태 초기화
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
    };

    public static int GenerateRandomAccuracy()
    {
        return Random.Shared.Next(Constants.MIN_ACCURACY, Constants.MAX_ACCURACY + 1);
    }

    public static float GetSpeedByRunning(bool isRunning)
    {
        return isRunning ? Constants.RUNNING_SPEED : Constants.WALKING_SPEED;
    }

    /// <summary>
    /// index에 따른 원형 배치 위치.
    /// </summary>
    public static Vector2 GetSpawnPosition(int index)
    {
        const int magicNumber = 53; // 소수여야 함
        var angle = index * magicNumber * (float)(Math.PI / 180);
        return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Constants.SPAWN_RADIUS;
    }

    public void UpdateMovementData(Vector2 position, int direction, float speed)
    {
        UpdatePosition(position);
        UpdateDirection(direction);
        UpdateSpeed(speed);
    }

    public void UpdatePosition(Vector2 newPosition)
    {
        MovementData.Position = newPosition;
    }

    public void UpdateDirection(int direction)
    {
        MovementData.Direction = direction;
    }

    public void UpdateSpeed(float newSpeed)
    {
        MovementData.Speed = newSpeed;
    }

    public void UpdateAlive(bool alive)
    {
        Alive = alive;
    }

    public void UpdateReloadTime(float remaining)
    {
        RemainingReloadTime = remaining;
    }

    public void UpdateTarget(Player target)
    {
        Target = target;
    }

    public void UpdateRange(float range)
    {
        Range = range;
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
    public void UpdateAccuracy(float deltaTime)
    {
        if (AccuracyState == 0) return; // 유지 상태면 처리하지 않음
        
        accuracyTimer += deltaTime;
        
        // 1초마다 정확도 업데이트
        if (accuracyTimer >= ACCURACY_UPDATE_INTERVAL)
        {
            accuracyTimer = 0f;
            
            int newAccuracy = Accuracy + AccuracyState;
            
            // 정확도 범위 제한
            newAccuracy = Math.Max(Constants.MIN_ACCURACY, Math.Min(Constants.MAX_ACCURACY, newAccuracy));
            
            if (newAccuracy != Accuracy)
            {
                Accuracy = newAccuracy;
                Console.WriteLine($"[정확도] 플레이어 {Id}의 정확도 변경: {Accuracy}% (상태: {AccuracyState})");
            }
        }
    }
}