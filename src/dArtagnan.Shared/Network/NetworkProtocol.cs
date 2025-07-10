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
    [Union(2, typeof(PlayerJoinBroadcast))]
    [Union(3, typeof(PlayerMovementDataFromClient))]
    [Union(4, typeof(PlayerMovementDataBroadcast))]
    [Union(5, typeof(PlayerShootingFromClient))]
    [Union(6, typeof(PlayerShootingBroadcast))]
    [Union(7, typeof(UpdatePlayerAlive))]
    [Union(8, typeof(PlayerLeaveFromClient))]
    [Union(9, typeof(PlayerLeaveBroadcast))]
    [Union(10, typeof(GamePlaying))]
    [Union(11, typeof(PlayerIsTargetingFromClient))]
    [Union(12, typeof(PlayerIsTargetingBroadcast))]
    [Union(13, typeof(StartGame))]
    [Union(14, typeof(NewHost))]
    [Union(15, typeof(Winner))]
    //[Union(16, typeof(GamePlaying))]
    [Union(17, typeof(GameWaiting))]
    [Union(19, typeof(PlayerBalanceUpdate))]
    [Union(20, typeof(PingPacket))]
    [Union(21, typeof(PongPacket))]
    public interface IPacket
    {
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 게임에 접속하고 싶을 때 보내는 패킷.
    /// </summary>
    [MessagePackObject]
    public struct PlayerJoinRequest : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 플레이어가 자신의 번호가 몇 번인지를 알 수 있도록 보내주는 패킷.
    /// </summary>
    [MessagePackObject]
    public struct YouAre : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 특정 플레이어의 정보를 담은 패킷.
    /// </summary>
    [MessagePackObject]
    public struct PlayerInformation
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public string Nickname;
        [Key(2)] public int Accuracy;
        [Key(3)] public float TotalReloadTime;
        [Key(4)] public float RemainingReloadTime;
        [Key(5)] public bool Alive;
        [Key(6)] public int Targeting; // 겨누는 플레이어의 번호. 겨누는 플레이어가 없을 경우 -1이다.
        [Key(7)] public float Range;
        [Key(8)] public MovementData MovementData;
        [Key(9)] public int Balance;
    }

    /// <summary>
    /// 이동 정보. 단독으로는 쓰이지 않고 다른 패킷에 담겨서 쓰인다. 방향과 위치와 속도를 담고 있다.
    /// </summary>
    [MessagePackObject]
    public struct MovementData
    {
        [Key(0)] public int Direction;
        [Key(1)] public Vector2 Position;
        [Key(2)] public float Speed;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 특정 플레이어가 접속했음을 알려주는 패킷.
    /// </summary>
    [MessagePackObject]
    public struct PlayerJoinBroadcast : IPacket
    {
        [Key(0)] public PlayerInformation PlayerInfo;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 자신의 이동 방향, 위치, 달리기 여부를 서버에 보내줄 때 쓰는 패킷.
    /// </summary>
    [MessagePackObject]
    public struct PlayerMovementDataFromClient : IPacket
    {
        [Key(0)] public int Direction;
        [Key(1)] public Vector2 Position;
        [Key(2)] public bool Running;
    }

    /// <summary>
    /// [서버 => 브로드캐스트]
    /// 특정 플레이어의 이동 정보를 브로드캐스트.
    /// </summary>
    [MessagePackObject]
    public struct PlayerMovementDataBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public MovementData MovementData;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 TargetId번을 쏘겠다고 요청.
    /// </summary>
    [MessagePackObject]
    public struct PlayerShootingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// ShooterId번이 TargetId번을 사격했으며 그 결과는 Hit이고 사격자는 ShooteRemainingReloadTime만큼 쿨타임이 돈다.
    /// </summary>
    [MessagePackObject]
    public struct PlayerShootingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
        [Key(2)] public bool Hit;
        [Key(3)] public float ShooterRemainingReloadingTime;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 생존 여부를 보낸다.
    /// </summary>
    [MessagePackObject]
    public struct UpdatePlayerAlive : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public bool Alive;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어 퇴장 통보.
    /// </summary>
    [MessagePackObject]
    public struct PlayerLeaveFromClient : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 플레이어 퇴장 브로드캐스트.
    /// </summary>
    [MessagePackObject]
    public struct PlayerLeaveBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// TargetId번 플레이어를 겨누고 있음.
    /// </summary>
    [MessagePackObject]
    public struct PlayerIsTargetingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// ShooterId번 플레이어가 TargetId번 플레이어를 겨누고 있음.
    /// </summary>
    [MessagePackObject]
    public struct PlayerIsTargetingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 게임을 시작하겠다는 패킷. 방장만 전송 가능.
    /// </summary>
    [MessagePackObject]
    public struct StartGame : IPacket
    {}

    /// <summary>
    /// [서버 => 클라이언트]
    /// 게임이 현재 '대기' 상태에 있으며 플레이어들의 상태는 PlayersInfo와 같다.
    /// </summary>
    [MessagePackObject]
    public struct GameWaiting : IPacket
    {
        [Key(0)] public List<PlayerInformation> PlayersInfo;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 게임이 현재 '진행중' 상태에 있으며 플레이어들의 상태는 PlayersInfo와 같고 Round번째 라운드를 진행 중이다.
    /// 남은 시간은 RemainingTime, 총 시간은 TotalTime이다.
    /// </summary>
    [MessagePackObject]
    public struct GamePlaying : IPacket
    {
        [Key(0)] public List<PlayerInformation> PlayersInfo;
        [Key(1)] public int Round;
        [Key(2)] public float TotalTime;
        [Key(3)] public float RemainingTime;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// HostId번 플레이어가 새로 방장이 되었다.
    /// </summary>
    [MessagePackObject]
    public struct NewHost : IPacket
    {
        [Key(0)] public int HostId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어가 승리했다.
    /// </summary>
    [MessagePackObject]
    public struct Winner : IPacket
    {
        [Key(0)] public int PlayerId; // 승자가 없으면 -1.
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 소지금이 Balance로 바뀌었다.
    /// </summary>
    [MessagePackObject]
    public struct PlayerBalanceUpdate : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int Balance;
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

    [MessagePackObject]
    public struct PingPacket : IPacket
    {
    }

    [MessagePackObject]
    public struct PongPacket : IPacket
    {
        
    }
}