using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server
{
    /// <summary>
    /// 플레이어의 상태와 정보를 관리하는 모델 클래스
    /// </summary>
    public class Player
    {
        // 플레이어 관련 상수들
        public const float DEFAULT_RELOAD_TIME = 15.0f;
        public const float WALKING_SPEED = 40f;
        public const float RUNNING_SPEED = 160f;
        public const int MIN_ACCURACY = 1;
        public const int MAX_ACCURACY = 100;

        public int Id;
        public int PlayerId;
        public string Nickname;
        public int Accuracy;
        public int Direction;
        public float X;
        public float Y;
        public float range;
        
        // 게임 상태
        public float TotalReloadTime;
        public float RemainingReloadTime;
        public float Speed;
        public bool Alive;
        public bool IsInGame;
        public bool IsReady;
        public int targeting;

        public PlayerInformation playerInformation => new()
        {
            accuracy = Accuracy,
            direction = Direction,
            alive = Alive,
            nickname = Nickname,
            playerId = PlayerId,
            remainingReloadTime = RemainingReloadTime,
            speed = Speed,
            totalReloadTime = TotalReloadTime,
            targeting = targeting,
            x = X,
            y = Y,
            range = range
        };

        public Player(int id, int playerId, string nickname)
        {
            Id = id;
            PlayerId = playerId;
            Nickname = nickname;
            Accuracy = GenerateRandomAccuracy();
            Direction = 0;
            X = 0;
            Y = 0;
            TotalReloadTime = DEFAULT_RELOAD_TIME;
            RemainingReloadTime = TotalReloadTime / 2;
            Speed = WALKING_SPEED;
            Alive = true;
            IsInGame = false;
            IsReady = false;
            range = 800f;
        }

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
} 