using System;
using MessagePack;
using UnityEngine;

namespace dArtagnan.Shared
{
    // 패킷 타입 정의 (단순화)
    public enum PacketType : byte
    {
        // 연결 관련
        Connect = 1,
        ConnectResponse = 2,
        Disconnect = 3,
        
        // 플레이어 관련
        PlayerJoin = 10,
        PlayerJoinResponse = 11,
        PlayerLeave = 12,
        
        // 게임 관련
        PlayerMove = 20,
        PlayerMoveResponse = 21,
        PlayerShoot = 30,
        PlayerShootResponse = 31,
        
        // 응답 관련
        PlayerDeath = 23
    }

    // 기본 패킷 구조
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
            Data = data ?? Array.Empty<byte>();
        }
    }

    // 플레이어 정보
    [MessagePackObject]
    public struct PlayerInfo
    {
        [Key(0)]
        public int PlayerId { get; set; }
        
        [Key(1)]
        public string Nickname { get; set; }
        
        [Key(2)]
        public int Accuracy { get; set; }
    }

    // 플레이어 참가 패킷
    [MessagePackObject]
    public struct PlayerJoinPacket
    {
        [Key(0)]
        public string Nickname { get; set; }
    }

    [MessagePackObject]
    public struct PlayerJoinResponsePacket
    {
        [Key(0)]
        public bool Success { get; set; }
        
        [Key(1)]
        public string Message { get; set; }
        
        [Key(2)]
        public PlayerInfo PlayerInfo { get; set; }
    }

    // 플레이어 이동 패킷
    [MessagePackObject]
    public struct MovePacket
    {
        [Key(0)]
        public int PlayerId { get; set; }
        
        [Key(1)]
        public float X { get; set; }
        
        [Key(2)]
        public float Y { get; set; }
    }

    // 플레이어 슈팅 패킷
    [MessagePackObject]
    public struct ShootPacket
    {
        [Key(0)]
        public int PlayerId { get; set; }
        
        [Key(1)]
        public int TargetPlayerId { get; set; }
    }
} 