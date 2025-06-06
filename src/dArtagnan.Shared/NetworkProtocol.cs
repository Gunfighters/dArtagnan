using System;
using MessagePack;
using UnityEngine;

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
        public Vector2 position { get; set; }

        [Key(2)]
        public int accuracy { get; set; }
    }

    [MessagePackObject]
    public struct PlayerDirectionFromClient
    {
        [Key(0)]
        public Vector2 direction { get; set; }
    }

    [MessagePackObject]
    public struct PlayerDirectionFromServer
    {
        [Key(0)]
        public int playerId { get; set; }

        [Key(1)]
        public Vector2 direction { get; set; }
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