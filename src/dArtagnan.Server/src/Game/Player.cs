using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

public class Player(int id, string nickname, Vector2 position)
{
    public const float DEFAULT_RELOAD_TIME = 15.0f;
    public const float WALKING_SPEED = 40f;
    public const float RUNNING_SPEED = 160f;
    public const int MIN_ACCURACY = 1;
    public const int MAX_ACCURACY = 100;
    public const float DEFAULT_RANGE = 600f;
    public const float SPAWN_RADIUS = 40.0f;

    public readonly int Id = id;
    public readonly string Nickname = nickname;
    public int Accuracy = GenerateRandomAccuracy();
    public float Range = DEFAULT_RANGE;
        
    public float TotalReloadTime = DEFAULT_RELOAD_TIME;
    public float RemainingReloadTime = DEFAULT_RELOAD_TIME / 2;
    public bool Alive = true;
    public Player? Target;
    public MovementData MovementData = new() { Direction = 0, Position = position, Speed = WALKING_SPEED };

    public void Reset()
    {
        Alive = true;
        Target = null;
        Accuracy = GenerateRandomAccuracy();
        MovementData = new MovementData { Direction = 0, Position = Vector2.Zero, Speed = WALKING_SPEED };
        TotalReloadTime = DEFAULT_RELOAD_TIME;
        RemainingReloadTime = TotalReloadTime / 2;
        Range = DEFAULT_RANGE;
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
        MovementData = MovementData
    };

    public static int GenerateRandomAccuracy()
    {
        return Random.Shared.Next(MIN_ACCURACY, MAX_ACCURACY + 1);
    }

    public static float GetSpeedByRunning(bool isRunning)
    {
        return isRunning ? RUNNING_SPEED : WALKING_SPEED;
    }

    public static Vector2 GetSpawnPosition(int playerId)
    {
        // 간단한 원형 배치로 스폰 위치 결정
        var angle = playerId * 45 * (float)(Math.PI / 180); // 45도씩 회전
        return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * SPAWN_RADIUS;
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
}