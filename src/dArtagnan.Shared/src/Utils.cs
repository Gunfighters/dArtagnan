using System.Collections.Generic;
using System.Numerics;

namespace dArtagnan.Shared
{
    public enum GameState
    {
        Waiting,    // 대기 중 (Ready 단계 포함)
        Round,    // 라운드 진행 중
        Roulette, // 룰렛 돌리는 중
        Augment,  // 증강 선택 중
    }

    /// <summary>
    /// 클라이언트와 서버가 공유하는 방향.
    /// </summary>
    public static class DirectionHelper
    {
        public static readonly List<Vector2> Directions = new()
        {
            Vector2.Zero,
            Vector2.UnitY,
            Vector2.Normalize(Vector2.UnitY + Vector2.UnitX),
            Vector2.UnitX,
            Vector2.Normalize(Vector2.UnitX - Vector2.UnitY),
            -Vector2.UnitY,
            Vector2.Normalize(-Vector2.UnitY - Vector2.UnitX),
            -Vector2.UnitX,
            Vector2.Normalize(-Vector2.UnitX + Vector2.UnitY),
        };
        public static Vector2 IntToDirection(this int direction)
        {
            return Directions[direction];
        }
    }

    /// <summary>
    /// 클라이언트와 서버가 공유하는 상수.
    /// </summary>
    public static class Constants
    {
        public const int MAX_PLAYER_COUNT = 2;
        public const int MAX_ROUNDS = 4;
        public const int DEFAULT_MAX_ENERGY = 7;
        public const float ENERGY_RECOVERY_RATE = 0.5f; // 초당 0.5칸 회복 (2초에 1칸)
        public const int CRAFT_ENERGY_COST = 2; // 아이템 제작 시 소모되는 에너지
        public const int USE_ITEM_ENERGY_COST = 1; // 아이템 사용 시 소모되는 에너지
        public const float ACCURACY_UPDATE_INTERVAL = 1.0f;
        public const float MOVEMENT_SPEED = 1.5f;
        public const int ROULETTE_MIN_ACCURACY = 25;
        public const int ROULETTE_MAX_ACCURACY = 75;
        public const float DEFAULT_RANGE = 2f;
        public const float MAX_RANGE = 2.8f;  // 정확도가 낮을 때의 최대 사거리
        public const float MIN_RANGE = 1.2f;  // 정확도가 높을 때의 최소 사거리
        public const float SPAWN_RADIUS = 1.5f;
        public const float BETTING_PERIOD = 3.0f;
        public const float CREATING_DURATION = 3.0f; 
    }
} 