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
        public const int MAX_PLAYER_COUNT = 7;
        public const int MAX_ROUNDS = 4;
        public const float DEFAULT_RELOAD_TIME = 13.0f;
        public const float ACCURACY_UPDATE_INTERVAL = 1.0f;
        public const float MOVEMENT_SPEED = 1.5f;
        public const int ROULETTE_MIN_ACCURACY = 30;
        public const int ROULETTE_MAX_ACCURACY = 70;
        public const float DEFAULT_RANGE = 5f;
        public const float SPAWN_RADIUS = 1.5f;
        public const float BETTING_PERIOD = 3.0f;
        public const float CREATING_DURATION = 3.0f; 
    }
} 