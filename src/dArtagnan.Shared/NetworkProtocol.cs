using System.Collections.Generic;
using System.Numerics;
using MessagePack;

namespace dArtagnan.Shared
{
    [Union(0, typeof(PlayerJoinRequest))]
    [Union(1, typeof(YouAre))]
    [Union(2, typeof(InformationOfPlayers))]
    [Union(3, typeof(PlayerJoinBroadcast))]
    [Union(4, typeof(PlayerDirectionFromClient))]
    [Union(5, typeof(PlayerDirectionBroadcast))]
    [Union(6, typeof(PlayerRunningFromClient))]
    [Union(7, typeof(UpdatePlayerSpeedBroadcast))]
    [Union(8, typeof(PlayerShootingFromClient))]
    [Union(9, typeof(PlayerShootingBroadcast))]
    [Union(10, typeof(UpdatePlayerAlive))]
    [Union(11, typeof(PlayerLeaveFromClient))]
    [Union(12, typeof(PlayerLeaveBroadcast))]
    [Union(13, typeof(UpdatePlayerPosition))]
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
        [Key(0)] public int playerId { get; set; }
    }

    [MessagePackObject]
    public struct InformationOfPlayers : IPacket
    {
        [Key(0)] public List<PlayerInformation> info { get; set; }
    }

    [MessagePackObject]
    public struct PlayerInformation
    {
        [Key(0)] public int playerId;
        [Key(1)] public string nickname;
        [Key(2)] public int direction;
        [Key(3)] public float x;
        [Key(4)] public float y;
        [Key(5)] public int accuracy;
        [Key(6)] public float totalReloadTime;
        [Key(7)] public float remainingReloadTime;
        [Key(8)] public float speed;
        [Key(9)] public bool alive;
    }

    [MessagePackObject]
    public struct PlayerJoinBroadcast : IPacket
    {
        [Key(0)] public int playerId { get; set; }
        [Key(1)] public int initX { get; set; }
        [Key(2)] public int initY { get; set; }
        [Key(3)] public int accuracy { get; set; }
    }

    [MessagePackObject]
    public struct PlayerDirectionFromClient : IPacket
    {
        [Key(0)] public int direction { get; set; }
        [Key(1)] public float currentX { get; set; }
        [Key(2)] public float currentY { get; set; }
    }

    [MessagePackObject]
    public struct PlayerDirectionBroadcast : IPacket
    {
        [Key(0)] public int playerId { get; set; }
        [Key(1)] public int direction { get; set; }
        [Key(2)] public float currentX { get; set; }
        [Key(3)] public float currentY { get; set; }
    }

    [MessagePackObject]
    public struct PlayerRunningFromClient : IPacket
    {
        [Key(0)] public bool isRunning { get; set; }
    }

    [MessagePackObject]
    public struct UpdatePlayerSpeedBroadcast : IPacket
    {
        [Key(0)] public int playerId { get; set; }
        [Key(1)] public float speed { get; set; }
    }

    [MessagePackObject]
    public struct PlayerShootingFromClient : IPacket
    {
        [Key(0)] public int targetId { get; set; }
    }

    [MessagePackObject]
    public struct PlayerShootingBroadcast : IPacket
    {
        [Key(0)] public int shooterId { get; set; }
        [Key(1)] public int targetId { get; set; }
        [Key(2)] public bool hit { get; set; }
    }

    [MessagePackObject]
    public struct UpdatePlayerAlive : IPacket
    {
        [Key(0)] public int playerId { get; set; }
        [Key(1)] public bool alive { get; set; }
    }

    [MessagePackObject]
    public struct PlayerLeaveFromClient : IPacket
    {
    }

    [MessagePackObject]
    public struct PlayerLeaveBroadcast : IPacket
    {
        [Key(0)] public int playerId { get; set; }
    }

    [MessagePackObject]
    public struct UpdatePlayerPosition : IPacket
    {
        [Key(0)] public List<PlayerPosition> positionList { get; set; }
    }

    [MessagePackObject]
    public struct PlayerPosition
    {
        [Key(0)] public int playerId;
        [Key(1)] public float x;
        [Key(2)] public float y;
    }

    public class DirectionHelper
    {
        public static readonly List<Vector3> Directions = new()
        {
            Vector3.Zero,
            Vector3.UnitY,
            Vector3.Normalize(Vector3.UnitY + Vector3.UnitX),
            Vector3.UnitX,
            Vector3.Normalize(Vector3.UnitX - Vector3.UnitY),
            -Vector3.UnitY,
            Vector3.Normalize(-Vector3.UnitY - Vector3.UnitX),
            -Vector3.UnitX,
            Vector3.Normalize(-Vector3.UnitX + Vector3.UnitY),
        };
        public static Vector3 IntToDirection(int direction)
        {
            return Directions[direction];
        }
    }
}