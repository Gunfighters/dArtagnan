using System;
using MessagePack;

namespace dArtagnan.Shared
{
    public enum PacketType : byte
    {
        JoinRequestFromClient,
        YouAre,
        JoinResponseFromServer,
        PlayerDirectionFromClient,
        PlayerDirectionFromServer,
        PlayerRunningFromClient,
        PlayerRunningFromServer
    }

    [MessagePackObject]
    public struct Packet
    {
        [Key(0)]
        public PacketType Type { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }

        public Packet(PacketType type, byte[] data = null)
        {
            Type = type;
            Data = data ?? new byte[0];
        }
    }

    [MessagePackObject]
    public struct JoinRequestFromClient
    {
    }

    [MessagePackObject]
    public struct YouAre
    {
        [Key(0)]
        public int playerId { get; set; }
    }

    [MessagePackObject]
    public struct JoinResponseFromServer
    {
        [Key(0)]
        public int playerId { get; set; }

        [Key(1)]
        public int initX { get; set; }

        [Key(2)]
        public int initY { get; set; }

        [Key(3)]
        public int accuracy { get; set; }
    }

    [MessagePackObject]
    public struct PlayerDirectionFromClient
    {
        [Key(0)]
        public int direction { get; set; }
    }

    [MessagePackObject]
    public struct PlayerDirectionFromServer
    {
        [Key(0)]
        public int playerId { get; set; }

        [Key(1)]
        public int direction { get; set; }
    }

    [MessagePackObject]
    public struct PlayerRunningFromClient
    {
        [Key(0)]
        public bool isRunning { get; set; }
    }

    [MessagePackObject]
    public struct PlayerRunningFromServer
    {
        [Key(0)]
        public int playerId { get; set; }

        [Key(1)]
        public bool isRunning { get; set; }
    }
} 