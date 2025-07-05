using System.Collections.Generic;
using System.Numerics;
using MessagePack;

namespace dArtagnan.Shared
{
    public enum GameState
    {
        Waiting,    // 대기 중 (Ready 단계 포함)
        Playing     // 게임 진행 중
    }
    [Union(0, typeof(PlayerJoinRequest))]
    [Union(1, typeof(YouAre))]
    [Union(2, typeof(InformationOfPlayers))]
    [Union(3, typeof(PlayerJoinBroadcast))]
    [Union(4, typeof(PlayerMovementDataFromClient))]
    [Union(5, typeof(PlayerMovementDataBroadcast))]
    [Union(6, typeof(PlayerShootingFromClient))]
    [Union(7, typeof(PlayerShootingBroadcast))]
    [Union(8, typeof(UpdatePlayerAlive))]
    [Union(9, typeof(PlayerLeaveFromClient))]
    [Union(10, typeof(PlayerLeaveBroadcast))]
    [Union(11, typeof(GameStarted))]
    [Union(12, typeof(PlayerIsTargetingFromClient))]
    [Union(13, typeof(PlayerIsTargetingBroadcast))]
    [Union(14, typeof(StartGame))]
    [Union(15, typeof(NewHost))]
    [Union(16, typeof(NewGameState))]
    [Union(17, typeof(Winner))]
    public interface IPacket
    {
    }

    [MessagePackObject]
    public struct PlayerJoinRequest : IPacket
    {
    }

    [MessagePackObject]
    public struct YouAre : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    [MessagePackObject]
    public struct InformationOfPlayers : IPacket
    {
        [Key(0)] public List<PlayerInformation> Info;
    }

    [MessagePackObject]
    public struct PlayerInformation
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public string Nickname;
        [Key(2)] public int Accuracy;
        [Key(3)] public float TotalReloadTime;
        [Key(4)] public float RemainingReloadTime;
        [Key(5)] public bool Alive;
        [Key(6)] public int Targeting; // -1 if targeting none.
        [Key(7)] public float Range;
        [Key(8)] public MovementData MovementData;
    }

    [MessagePackObject]
    public struct MovementData
    {
        [Key(0)] public int Direction;
        [Key(1)] public Vector2 Position;
        [Key(2)] public float Speed;
    }

    [MessagePackObject]
    public struct PlayerJoinBroadcast : IPacket
    {
        [Key(0)] public PlayerInformation PlayerInfo;
    }

    [MessagePackObject]
    public struct PlayerMovementDataFromClient : IPacket
    {
        [Key(0)] public int Direction;
        [Key(1)] public Vector2 Position;
        [Key(2)] public bool Running;
    }

    [MessagePackObject]
    public struct PlayerMovementDataBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public MovementData MovementData;
    }

    [MessagePackObject]
    public struct PlayerShootingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    [MessagePackObject]
    public struct PlayerShootingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
        [Key(2)] public bool Hit;
        [Key(3)] public float ShooterRemainingReloadingTime;
    }

    [MessagePackObject]
    public struct UpdatePlayerAlive : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public bool Alive;
    }

    [MessagePackObject]
    public struct PlayerLeaveFromClient : IPacket
    {
    }

    [MessagePackObject]
    public struct PlayerLeaveBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    [MessagePackObject]
    public struct PlayerIsTargetingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    [MessagePackObject]
    public struct PlayerIsTargetingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
    }

    [MessagePackObject]
    public struct StartGame : IPacket
    {}

    [MessagePackObject]
    public struct GameStarted : IPacket
    {
        [Key(0)] public List<PlayerInformation> Players;
    }

    [MessagePackObject]
    public struct NewHost : IPacket
    {
        [Key(0)] public int HostId;
    }

    [MessagePackObject]
    public struct NewGameState : IPacket
    {
        [Key(0)] public GameState GameState;
    }

    [MessagePackObject]
    public struct Winner : IPacket
    {
        [Key(0)] public int PlayerId;
    }

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

    public static class Constants
    {
        public const float DEFAULT_RELOAD_TIME = 15.0f;
        public const float WALKING_SPEED = 40f;
        public const float RUNNING_SPEED = 160f;
        public const int MIN_ACCURACY = 1;
        public const int MAX_ACCURACY = 100;
        public const float DEFAULT_RANGE = 600f;
        public const float SPAWN_RADIUS = 40.0f;
    }
}