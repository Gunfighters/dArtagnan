using System.Collections.Generic;
using MessagePack;

namespace dArtagnan.Shared
{
    [Union(0, typeof(JoinRequestFromClient))]
    [Union(1, typeof(YouAre))]
    [Union(2, typeof(JoinResponseFromServer))]
    [Union(3, typeof(PlayerDirectionFromClient))]
    [Union(4, typeof(PlayerDirectionFromServer))]
    [Union(5, typeof(PlayerRunningFromClient))]
    [Union(6, typeof(PlayerRunningFromServer))]
    [Union(7, typeof(InformationOfPlayers))]
    [Union(8, typeof(PlayerShootingFromClient))]
    [Union(9, typeof(PlayerShootingFromServer))]
    public interface IPacket
    {
    }

    [MessagePackObject]
    public struct JoinRequestFromClient : IPacket
    {
    }

    [MessagePackObject]
    public struct YouAre : IPacket
    {
        [Key(0)] public int playerId { get; set; }
    }

    [MessagePackObject]
    public struct JoinResponseFromServer : IPacket
    {
        [Key(0)] public int playerId { get; set; }

        [Key(1)] public float initX { get; set; }

        [Key(2)] public float initY { get; set; }

        [Key(3)] public int accuracy { get; set; }
    }

    [MessagePackObject]
    public struct InformationOfPlayers : IPacket
    {
        [Key(0)] public List<PlayerInformation> info;
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
        [Key(6)] public bool isRunning;
    }

    [MessagePackObject]
    public struct PlayerDirectionFromClient : IPacket
    {
        [Key(0)] public int direction { get; set; }
    }

    [MessagePackObject]
    public struct PlayerDirectionFromServer : IPacket
    {
        [Key(0)] public int playerId { get; set; }

        [Key(1)] public int direction { get; set; }
    }

    [MessagePackObject]
    public struct PlayerRunningFromClient : IPacket
    {
        [Key(0)] public bool isRunning { get; set; }
    }

    [MessagePackObject]
    public struct PlayerRunningFromServer : IPacket
    {
        [Key(0)] public int playerId { get; set; }

        [Key(1)] public bool isRunning { get; set; }
    }

    [MessagePackObject]
    public struct PlayerShootingFromClient : IPacket
    {
        [Key(0)] public int targetId { get; set; }
    }

    [MessagePackObject]
    public struct PlayerShootingFromServer : IPacket
    {
        [Key(0)] public int playerId { get; set; }
        [Key(1)] public int targetId { get; set; }
    }
}