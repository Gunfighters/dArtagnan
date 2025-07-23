using System.Collections.Generic;
using System.Numerics;
using MessagePack;

namespace dArtagnan.Shared
{
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
    [Union(10, typeof(RoundStartFromServer))]
    [Union(11, typeof(PlayerIsTargetingFromClient))]
    [Union(12, typeof(PlayerIsTargetingBroadcast))]
    [Union(13, typeof(StartGameFromClient))]
    [Union(14, typeof(NewHostBroadcast))]
    [Union(15, typeof(RoundWinnerBroadcast))]
    [Union(16, typeof(GameWinnerBroadcast))]
    [Union(17, typeof(WaitingStartFromServer))]
    [Union(18, typeof(PlayerBalanceUpdateBroadcast))]
    [Union(19, typeof(PingPacket))]
    [Union(20, typeof(PongPacket))]
    [Union(21, typeof(SetAccuracyState))]
    [Union(22, typeof(PlayerAccuracyStateBroadcast))]
    [Union(23, typeof(YourAccuracyAndPool))]
    [Union(24, typeof(RouletteDone))]
    [Union(25, typeof(BettingDeductionBroadcast))]
    [Union(26, typeof(AugmentStartFromServer))]
    [Union(27, typeof(AugmentDoneFromClient))]
    [Union(28, typeof(ItemCreatingStateFromClient))]
    [Union(29, typeof(PlayerCreatingStateBroadcast))]
    [Union(30, typeof(ItemAcquiredBroadcast))]
    [Union(31, typeof(UseItemFromClient))]
    [Union(32, typeof(ItemUsedBroadcast))]
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
        [Key(10)] public int AccuracyState;
        [Key(11)] public List<int> Augments; // 보유한 증강 ID 리스트
        [Key(12)] public int CurrentItem; // 현재 소지한 아이템 ID. 없으면 -1
        [Key(13)] public bool IsCreatingItem; // 아이템 제작 중인지 여부
        [Key(14)] public float CreatingRemainingTime; // 아이템 제작 남은 시간
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
    /// 플레이어가 자신의 이동 방향, 위치를 서버에 보내줄 때 쓰는 패킷.
    /// </summary>
    [MessagePackObject]
    public struct PlayerMovementDataFromClient : IPacket
    {
        [Key(0)] public int Direction;
        // [Key(1)] public Vector2 Position;
        [Key(1)] public MovementData MovementData;
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
    public struct StartGameFromClient : IPacket
    {}

    /// <summary>
    /// [서버 => 클라이언트]
    /// 'Waiting' 상태를 시작한다 플레이어들의 상태는 PlayersInfo와 같고 Round번째 라운드를 진행 중이다.
    /// 클라이언트가 방에 처음 입장하거나 게임이 종료된 후 게임이 대기 상태로 돌아갈때 만 보내진다.
    /// </summary>
    [MessagePackObject]
    public struct WaitingStartFromServer : IPacket
    {
        [Key(0)] public List<PlayerInformation> PlayersInfo;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 'Round' 상태를 시작한다 플레이어들의 상태는 PlayersInfo와 같고 Round번째 라운드를 진행 중이다.
    /// 게임이 시작되거나 각 라운드가 시작될 때만 보내진다.
    /// 클라: BettingAmount는 이번 라운드의 10초마다 차감되는 베팅금이다.
    /// </summary>
    [MessagePackObject]
    public struct RoundStartFromServer : IPacket
    {
        [Key(0)] public List<PlayerInformation> PlayersInfo;
        [Key(1)] public int Round;
        [Key(2)] public int BettingAmount;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// HostId번 플레이어가 새로 방장이 되었다.
    /// </summary>
    [MessagePackObject]
    public struct NewHostBroadcast : IPacket
    {
        [Key(0)] public int HostId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어들이 라운드에서 승리했다.
    /// 클라: 이패킷 받으면 단순히 게임승리 UI만 띄우면 됨.
    /// </summary>
    [MessagePackObject]
    public struct RoundWinnerBroadcast : IPacket
    {
        [Key(0)] public List<int> PlayerIds; // 라운드 승자들
        [Key(1)] public int Round; // 라운드 번호
        [Key(2)] public int PrizeMoney; // 획득한 판돈
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어들이 게임 전체에서 승리했다.
    /// 클라: 이패킷 받으면 단순히 게임승리 UI만 띄우면 됨.
    /// </summary>
    [MessagePackObject]
    public struct GameWinnerBroadcast : IPacket
    {
        [Key(0)] public List<int> PlayerIds; // 게임 최종 승자들. 승자가 없으면 빈 리스트.
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 소지금이 Balance로 바뀌었다.
    /// </summary>
    [MessagePackObject]
    public struct PlayerBalanceUpdateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int Balance;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어의 정확도 증감 상태를 변경하겠다고 요청.
    /// </summary>
    [MessagePackObject]
    public struct SetAccuracyState : IPacket
    {
        [Key(0)] public int AccuracyState; // -1: 정확도 감소, 0: 정확도 유지, 1: 정확도 증가
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 정확도 상태가 AccuracyState로 변경되었다.
    /// </summary>
    [MessagePackObject]
    public struct PlayerAccuracyStateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int AccuracyState; // -1: 정확도 감소, 0: 정확도 유지, 1: 정확도 증가
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 너의 정확도는 YourAccuracy이다. 등장가능한 정확도 풀은 AccuracyPool과 같다.
    /// </summary>
    [MessagePackObject]
    public struct YourAccuracyAndPool : IPacket
    {
        [Key(0)] public int YourAccuracy;
        [Key(1)] public List<int> AccuracyPool;
    }
    
    /// <summary>
    /// [클라이언트 => 서버]
    /// 룰렛을 돌려 나의 명중률을 확인하였다.
    /// TrialCount는 현재 안쓰이는 변수이지만 빈 패킷끼리 구분을 못하는 버그 땜애 임시로 존재
    /// </summary>
    [MessagePackObject]
    public struct RouletteDone : IPacket
    {
        [Key(0)] public int TrialCount;
    }

    [MessagePackObject]
    public struct PingPacket : IPacket
    {
    }

    [MessagePackObject]
    public struct PongPacket : IPacket
    {  
    }
    
    /// <summary>
    /// [서버 => 클라이언트]
    /// 10초마다 베팅금이 차감되었음을 알려주는 패킷
    /// 클라: 이패킷 받으면 판돈을 TotalPrizeMoney 로 업데이트하고
    /// 모든 플레이어의 잔액을 DeductedAmount만큼 감소시킨다.
    /// </summary>
    [MessagePackObject]
    public struct BettingDeductionBroadcast : IPacket
    {
        [Key(0)] public int DeductedAmount; // 차감된 베팅금
        [Key(1)] public int TotalPrizeMoney; // 업데이트된 총 판돈
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 라운드 종료 후 증강 선택을 시작할 때 보내는 패킷
    /// 3개의 증강 옵션을 제공한다.
    /// </summary>
    [MessagePackObject]
    public struct AugmentStartFromServer : IPacket
    {
        [Key(0)] public List<int> AugmentOptions; // 선택할 수 있는 3개의 증강 ID
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 선택한 증강 번호를 보내는 패킷
    /// </summary>
    [MessagePackObject]
    public struct AugmentDoneFromClient : IPacket
    {
        [Key(0)] public int SelectedAugmentIndex; // 선택한 증강의 인덱스 (0, 1, 2)
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 아이템 제작 상태를 변경하겠다는 패킷
    /// </summary>
    [MessagePackObject]
    public struct ItemCreatingStateFromClient : IPacket
    {
        [Key(0)] public bool IsCreatingItem; // true: 제작 시작, false: 제작 취소
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 아이템 제작 상태가 변경되었음을 알리는 패킷
    /// </summary>
    [MessagePackObject]
    public struct PlayerCreatingStateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public bool IsCreatingItem; // 아이템 제작 중인지 여부
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어가 ItemId번 아이템을 획득했음을 알리는 패킷
    /// </summary>
    [MessagePackObject]
    public struct ItemAcquiredBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int ItemId; // 획득한 아이템의 ID
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 현재 소지한 아이템을 사용하겠다는 패킷
    /// </summary>
    [MessagePackObject]
    public struct UseItemFromClient : IPacket
    {
        [Key(0)] public int TargetPlayerId; // 타겟이 필요한 아이템의 경우 대상 플레이어 ID. 자기 자신이나 타겟이 없으면 -1
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어가 ItemId번 아이템을 사용했음을 알리는 패킷
    /// </summary>
    [MessagePackObject]
    public struct ItemUsedBroadcast : IPacket
    {
        [Key(0)] public int PlayerId; // 아이템을 사용한 플레이어
        [Key(1)] public int ItemId; // 사용한 아이템의 ID
        // [Key(2)] public int TargetPlayerId; // 아이템의 대상이 된 플레이어 ID. 없으면 -1
    }
}