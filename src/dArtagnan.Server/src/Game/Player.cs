using dArtagnan.Shared;

namespace dArtagnan.Server;

public class Player(int id, string nickname, float x, float y)
{
    public const float DEFAULT_RELOAD_TIME = 15.0f;
    public const float WALKING_SPEED = 40f;
    public const float RUNNING_SPEED = 160f;
    public const int MIN_ACCURACY = 1;
    public const int MAX_ACCURACY = 100;
    public const float DEFAULT_RANGE = 600f;

    public int Id = id;
    public string Nickname = nickname;
    public int Accuracy;
    public int Direction;
    public float X = x;
    public float Y = y;
    public float Range = DEFAULT_RANGE;
        
    public float TotalReloadTime = DEFAULT_RELOAD_TIME;
    public float RemainingReloadTime = DEFAULT_RELOAD_TIME / 2;
    public float Speed;
    public bool Alive;
    public bool IsInGame;
    public bool IsReady;
    public int targeting;

    public PlayerInformation PlayerInformation => new()
    {
        accuracy = Accuracy,
        direction = Direction,
        alive = Alive,
        nickname = Nickname,
        remainingReloadTime = RemainingReloadTime,
        speed = Speed,
        totalReloadTime = TotalReloadTime,
        targeting = targeting,
        x = X,
        y = Y,
        range = Range
    };

    public static int GenerateRandomAccuracy()
    {
        return Random.Shared.Next(MIN_ACCURACY, MAX_ACCURACY + 1);
    }

    public static float GetSpeedByRunning(bool isRunning)
    {
        return isRunning ? RUNNING_SPEED : WALKING_SPEED;
    }

    public static (float x, float y) GetSpawnPosition(int playerId)
    {
        // 간단한 원형 배치로 스폰 위치 결정
        float angle = (playerId * 45) * (float)(Math.PI / 180); // 45도씩 회전
        float radius = 5.0f;
        float x = (float)Math.Cos(angle) * radius;
        float y = (float)Math.Sin(angle) * radius;
            
        return (x, y);
    }

    public void UpdatePosition(float newX, float newY)
    {
        X = newX;
        Y = newY;
    }

    public void UpdateSpeed(float newSpeed)
    {
        Speed = newSpeed;
    }

    public void UpdateAlive(bool alive)
    {
        Alive = alive;
    }

    public void UpdateReloadTime(float remaining)
    {
        RemainingReloadTime = remaining;
    }

    public void JoinGame()
    {
        IsInGame = true;
    }

    public void LeaveGame()
    {
        IsInGame = false;
    }

    public void UpdateReady(bool ready)
    {
        IsReady = ready;
    }
}