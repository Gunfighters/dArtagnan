using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server.Game
{
    /// <summary>
    /// 게임 규칙과 계산 로직을 담당하는 클래스
    /// </summary>
    public static class GameRules
    {
        // 게임 상수들
        public const float DEFAULT_RELOAD_TIME = 2.0f;
        public const float WALKING_SPEED = 1.0f;
        public const float RUNNING_SPEED = 4.0f;
        public const int MIN_ACCURACY = 1;
        public const int MAX_ACCURACY = 100;
        public const int MIN_PLAYERS = 3;
        public const int MAX_PLAYERS = 8;

        /// <summary>
        /// 명중률을 기반으로 사격 성공 여부를 계산합니다
        /// </summary>
        public static bool CalculateHit(int accuracy)
        {
            return Random.Shared.Next(1, 101) <= accuracy;
        }

        /// <summary>
        /// 달리기 상태에 따른 속도를 반환합니다
        /// </summary>
        public static float GetSpeedByRunning(bool isRunning)
        {
            return isRunning ? RUNNING_SPEED : WALKING_SPEED;
        }

        /// <summary>
        /// 방향에 따른 벡터를 반환합니다
        /// </summary>
        private static Vector3 GetDirectionVector(int direction)
        {
            return DirectionHelper.IntToDirection(direction);
        }

        /// <summary>
        /// 플레이어의 새로운 위치를 계산합니다
        /// </summary>
        public static (float newX, float newY) CalculateNewPosition(
            float currentX, float currentY, int direction, float speed, float deltaTime)
        {
            var vector = GetDirectionVector(direction);
            
            // 정지 상태가 아닐 때만 이동
            if (vector == Vector3.Zero)
            {
                return (currentX, currentY);
            }

            float moveX = vector.X * speed * deltaTime;
            float moveY = vector.Y * speed * deltaTime;

            return (currentX + moveX, currentY + moveY);
        }

        /// <summary>
        /// 재장전 시간을 업데이트합니다
        /// </summary>
        public static float UpdateReloadTime(float currentReloadTime, float deltaTime)
        {
            return Math.Max(0, currentReloadTime - deltaTime);
        }

        /// <summary>
        /// 플레이어가 사격 가능한지 확인합니다
        /// </summary>
        public static bool CanShoot(Player player)
        {
            return player.Alive && player.IsInGame && player.RemainingReloadTime <= 0;
        }

        /// <summary>
        /// 랜덤 명중률을 생성합니다
        /// </summary>
        public static int GenerateRandomAccuracy()
        {
            return Random.Shared.Next(MIN_ACCURACY, MAX_ACCURACY + 1);
        }

        /// <summary>
        /// 게임이 종료되어야 하는지 확인합니다
        /// </summary>
        public static bool ShouldEndGame(int alivePlayerCount)
        {
            return alivePlayerCount <= 1;
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
    }
} 