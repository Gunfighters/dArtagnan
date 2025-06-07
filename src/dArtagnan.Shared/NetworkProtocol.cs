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
    public interface IPacket
    {
    };

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
}