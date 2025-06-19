using System.Numerics;

namespace dArtagnan.Server.Game
{
    /// <summary>
    /// 플레이어의 상태와 정보를 관리하는 모델 클래스
    /// </summary>
    public class Player
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int Accuracy { get; set; }
        public int Direction { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        
        // 게임 상태
        public float TotalReloadTime { get; set; }
        public float RemainingReloadTime { get; set; }
        public float Speed { get; set; }
        public bool Alive { get; set; }
        public bool IsInGame { get; set; }

        public Player(int id, int playerId, string nickname)
        {
            Id = id;
            PlayerId = playerId;
            Nickname = nickname;
            Accuracy = GameRules.GenerateRandomAccuracy();
            Direction = 0;
            X = 0;
            Y = 0;
            TotalReloadTime = GameRules.DEFAULT_RELOAD_TIME;
            RemainingReloadTime = 0.0f;
            Speed = GameRules.WALKING_SPEED;
            Alive = true;
            IsInGame = false;
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
    }
} 