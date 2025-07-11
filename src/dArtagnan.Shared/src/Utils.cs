using System.Collections.Generic;
using System.Numerics;

namespace dArtagnan.Shared
{
    public enum GameState
    {
        Waiting,    // 대기 중 (Ready 단계 포함)
        Playing     // 게임 진행 중
    }

    /// <summary>
    /// 클라이언트와 서버가 공유하는 방향.
    /// </summary>
    public class DirectionHelper
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
        public static Vector2 IntToDirection(int direction)
        {
            return Directions[direction];
        }
    }

    /// <summary>
    /// 클라이언트와 서버가 공유하는 상수.
    /// </summary>
    public static class Constants
    {
        public const float DEFAULT_RELOAD_TIME = 15.0f;
        public const float WALKING_SPEED = 40f;
        public const float RUNNING_SPEED = 160f;
        public const int MIN_ACCURACY = 1;
        public const int MAX_ACCURACY = 100;
        public const float DEFAULT_RANGE = 600f;
        public const float SPAWN_RADIUS = 100.0f;
    }
} 