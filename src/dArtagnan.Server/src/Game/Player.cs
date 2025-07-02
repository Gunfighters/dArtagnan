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

        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int Accuracy { get; set; }
        public int Direction { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float range { get; set; }
        
        // 게임 상태
        public float TotalReloadTime { get; set; }
        public float RemainingReloadTime { get; set; }
        public float Speed { get; set; }
        public bool Alive { get; set; }
        public bool IsInGame { get; set; }
        public bool IsReady { get; set; }
        public int targeting { get; set; }

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
            RemainingReloadTime = 0.0f;
            Speed = WALKING_SPEED;
            Alive = true;
            IsInGame = false;
            IsReady = false;
            range = 200f;
        }

        /// <summary>
        /// 랜덤 명중률을 생성합니다
        /// </summary>
        public static int GenerateRandomAccuracy()
        {
            return Random.Shared.Next(MIN_ACCURACY, MAX_ACCURACY + 1);
        }

        /// <summary>
        /// 달리기 상태에 따른 속도를 반환합니다
        /// </summary>
        public static float GetSpeedByRunning(bool isRunning)
        {
            return isRunning ? RUNNING_SPEED : WALKING_SPEED;
        }

        /// <summary>
        /// 플레이어의 초기 위치를 설정합니다 (스폰 포인트)
        /// </summary>
        public static (float x, float y) GetSpawnPosition(int playerId)
        {
            // 간단한 원형 배치로 스폰 위치 결정
            float angle = (playerId * 45) * (float)(Math.PI / 180); // 45도씩 회전
            float radius = 5.0f;
            float x = (float)Math.Cos(angle) * radius;
            float y = (float)Math.Sin(angle) * radius;
            
            return (x, y);
        }

        /// <summary>
        /// 플레이어의 위치를 업데이트합니다
        /// </summary>
        public void UpdatePosition(float newX, float newY)
        {
            X = newX;
            Y = newY;
        }

        /// <summary>
        /// 플레이어의 속도를 업데이트합니다
        /// </summary>
        public void UpdateSpeed(float newSpeed)
        {
            Speed = newSpeed;
        }

        /// <summary>
        /// 플레이어의 생존 상태를 업데이트합니다
        /// </summary>
        public void UpdateAlive(bool alive)
        {
            Alive = alive;
        }

        /// <summary>
        /// 재장전 시간을 업데이트합니다
        /// </summary>
        public void UpdateReloadTime(float remaining)
        {
            RemainingReloadTime = remaining;
        }

        /// <summary>
        /// 게임 참가 상태를 설정합니다
        /// </summary>
        public void JoinGame()
        {
            IsInGame = true;
        }

        /// <summary>
        /// 게임 퇴장 상태를 설정합니다
        /// </summary>
        public void LeaveGame()
        {
            IsInGame = false;
        }

        /// <summary>
        /// 플레이어의 Ready 상태를 업데이트합니다
        /// </summary>
        public void UpdateReady(bool ready)
        {
            IsReady = ready;
        }
    }
} 