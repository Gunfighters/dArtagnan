using System.Collections.Generic;
using System.Numerics;
using MessagePack;

namespace dArtagnan.Shared
{
    // 연결 및 세션 관리
    [Union(0, typeof(JoinRequest))]
    [Union(1, typeof(YouAreFromServer))]
    [Union(2, typeof(JoinBroadcast))]
    [Union(3, typeof(LeaveFromClient))]
    [Union(4, typeof(LeaveBroadcast))]
    [Union(5, typeof(PingPacket))]
    [Union(6, typeof(PongPacket))]

    // 이동 및 위치
    [Union(7, typeof(MovementDataFromClient))]
    [Union(8, typeof(MovementDataBroadcast))]

    // 전투 시스템
    [Union(9, typeof(ShootingFromClient))]
    [Union(10, typeof(ShootingBroadcast))]
    [Union(11, typeof(PlayerIsTargetingFromClient))]
    [Union(12, typeof(PlayerIsTargetingBroadcast))]
    [Union(13, typeof(UpdatePlayerAlive))]

    // 게임 상태 관리
    [Union(14, typeof(StartGameFromClient))]
    [Union(15, typeof(WaitingStartFromServer))]
    [Union(16, typeof(RoundStartFromServer))]
    [Union(17, typeof(NewHostBroadcast))]
    [Union(18, typeof(RoundWinnerBroadcast))]
    [Union(19, typeof(GameWinnerBroadcast))]
    [Union(25, typeof(BettingDeductionBroadcast))]

    // 플레이어 스탯
    [Union(20, typeof(BalanceUpdateBroadcast))]
    [Union(21, typeof(UpdateAccuracyStateFromClient))]
    [Union(22, typeof(UpdateAccuracyStateBroadcast))]
    [Union(23, typeof(UpdateAccuracyBroadcast))]
    [Union(24, typeof(UpdateRangeBroadcast))]
    [Union(40, typeof(UpdateSpeedBroadcast))]
    [Union(41, typeof(UpdateActiveEffectsBroadcast))]

    // 에너지 시스템
    [Union(26, typeof(UpdateCurrentEnergyBroadcast))]
    [Union(27, typeof(UpdateMaxEnergyBroadcast))]
    [Union(28, typeof(UpdateMinEnergyToShootBroadcast))]

    // 쇼다운 시스템
    [Union(29, typeof(ShowdownStartFromServer))]

    // 증강 시스템
    [Union(31, typeof(AugmentStartFromServer))]
    [Union(32, typeof(AugmentDoneFromClient))]

    // 아이템 시스템
    [Union(33, typeof(UpdateItemCreatingStateFromClient))]
    [Union(34, typeof(UpdateCreatingStateBroadcast))]
    [Union(35, typeof(ItemAcquiredBroadcast))]
    [Union(36, typeof(UseItemFromClient))]
    [Union(37, typeof(ItemUsedBroadcast))]

    // 채팅 시스템
    [Union(38, typeof(ChatFromClient))]
    [Union(39, typeof(ChatBroadcast))]
    public interface IPacket
    {
    }

    #region 공통 데이터 구조

    /// <summary>
    /// 특정 플레이어의 정보를 담은 구조체
    /// </summary>
    [MessagePackObject]
    public struct PlayerInformation
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public string Nickname;
        [Key(2)] public int Accuracy;
        [Key(3)] public EnergyData EnergyData;
        [Key(4)] public int MinEnergyToShoot; // 사격하기 위한 최소 필요 에너지 (정확도에 비례)
        [Key(5)] public bool Alive;
        [Key(6)] public int Targeting; // 겨누는 플레이어의 번호. 겨누는 플레이어가 없을 경우 -1
        [Key(7)] public float Range;
        [Key(8)] public MovementData MovementData;
        [Key(9)] public int Balance;
        [Key(10)] public int AccuracyState;
        [Key(11)] public List<int> Augments; // 보유한 증강 ID 리스트
        [Key(12)] public int CurrentItem; // 현재 소지한 아이템 ID. 없으면 -1
        [Key(13)] public bool IsCreatingItem; // 아이템 제작 중인지 여부
        [Key(14)] public float CreatingRemainingTime; // 아이템 제작 남은 시간
        [Key(15)] public float SpeedMultiplier; // 현재 속도 배율
        [Key(16)] public bool HasDamageShield; // 피해 가드 보유 여부
        [Key(17)] public List<int> ActiveEffects; // 현재 적용 중인 효과 목록 (아이템: 1~999, 증강: 1000+)
    }

    /// <summary>
    /// 이동 정보. 단독으로는 쓰이지 않고 다른 패킷에 담겨서 쓰인다
    /// </summary>
    [MessagePackObject]
    public struct MovementData
    {
        [Key(0)] public int Direction;
        [Key(1)] public Vector2 Position;
        [Key(2)] public float Speed; //시뮬되어야 하는 최종 계산된 speed
    }

    /// <summary>
    /// 에너지 정보. 연속적으로 시뮬레이션 가능한 에너지 데이터
    /// </summary>
    [MessagePackObject]
    public struct EnergyData
    {
        [Key(0)] public int MaxEnergy; // 최대 에너지 (정수)
        [Key(1)] public float CurrentEnergy; // 현재 에너지 (소수점 단위)
    }

    #endregion

    #region 연결 및 세션 관리 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 게임에 접속하고 싶을 때 보내는 패킷
    /// </summary>
    [MessagePackObject]
    public struct JoinRequest : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 플레이어가 자신의 번호가 몇 번인지를 알 수 있도록 보내주는 패킷
    /// </summary>
    [MessagePackObject]
    public struct YouAreFromServer : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 특정 플레이어가 접속했음을 알려주는 패킷
    /// </summary>
    [MessagePackObject]
    public struct JoinBroadcast : IPacket
    {
        [Key(0)] public PlayerInformation PlayerInfo;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어 퇴장 통보
    /// </summary>
    [MessagePackObject]
    public struct LeaveFromClient : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 플레이어 퇴장 브로드캐스트
    /// </summary>
    [MessagePackObject]
    public struct LeaveBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 연결 상태 확인을 위한 핑 패킷
    /// </summary>
    [MessagePackObject]
    public struct PingPacket : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 핑에 대한 응답 패킷
    /// </summary>
    [MessagePackObject]
    public struct PongPacket : IPacket
    {
    }

    #endregion

    #region 이동 및 위치 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 자신의 이동 방향, 위치를 서버에 보내줄 때 쓰는 패킷
    /// </summary>
    [MessagePackObject]
    public struct MovementDataFromClient : IPacket
    {
        [Key(0)] public int Direction;
        [Key(1)] public MovementData MovementData;
    }

    /// <summary>
    /// [서버 => 브로드캐스트]
    /// 특정 플레이어의 이동 정보를 브로드캐스트
    /// </summary>
    [MessagePackObject]
    public struct MovementDataBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public MovementData MovementData;
    }

    #endregion

    #region 전투 시스템 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 TargetId번을 쏘겠다고 요청
    /// </summary>
    [MessagePackObject]
    public struct ShootingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// ShooterId번이 TargetId번을 사격했으며 그 결과와 사격자의 현재 에너지를 알려줌
    /// </summary>
    [MessagePackObject]
    public struct ShootingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
        [Key(2)] public bool Hit;
        [Key(3)] public int ShooterCurrentEnergy;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// TargetId번 플레이어를 겨누고 있음
    /// </summary>
    [MessagePackObject]
    public struct PlayerIsTargetingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// ShooterId번 플레이어가 TargetId번 플레이어를 겨누고 있음
    /// </summary>
    [MessagePackObject]
    public struct PlayerIsTargetingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 생존 여부를 보낸다
    /// </summary>
    [MessagePackObject]
    public struct UpdatePlayerAlive : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public bool Alive;
    }

    #endregion

    #region 게임 상태 관리 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 게임을 시작하겠다는 패킷. 방장만 전송 가능
    /// </summary>
    [MessagePackObject]
    public struct StartGameFromClient : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 'Waiting' 상태를 시작한다. 플레이어들의 상태는 PlayersInfo와 같다
    /// 클라이언트가 방에 처음 입장하거나 게임이 종료된 후 대기 상태로 돌아갈 때만 보내진다
    /// </summary>
    [MessagePackObject]
    public struct WaitingStartFromServer : IPacket
    {
        [Key(0)] public List<PlayerInformation> PlayersInfo;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 'Round' 상태를 시작한다. 플레이어들의 상태는 PlayersInfo와 같고 Round번째 라운드를 진행 중이다
    /// 게임이 시작되거나 각 라운드가 시작될 때만 보내진다
    /// BettingAmount는 이번 라운드의 10초마다 차감되는 베팅금이다
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
    /// HostId번 플레이어가 새로 방장이 되었다
    /// </summary>
    [MessagePackObject]
    public struct NewHostBroadcast : IPacket
    {
        [Key(0)] public int HostId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어들이 라운드에서 승리했다
    /// 이 패킷을 받으면 단순히 게임승리 UI만 띄우면 된다
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
    /// PlayerId번 플레이어들이 게임 전체에서 승리했다
    /// 이 패킷을 받으면 단순히 게임승리 UI만 띄우면 된다
    /// </summary>
    [MessagePackObject]
    public struct GameWinnerBroadcast : IPacket
    {
        [Key(0)] public List<int> PlayerIds; // 게임 최종 승자들. 승자가 없으면 빈 리스트
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 10초마다 베팅금이 차감되었음을 알려주는 패킷
    /// 이 패킷을 받으면 판돈을 TotalPrizeMoney로 업데이트하고
    /// 모든 플레이어의 잔액을 DeductedAmount만큼 감소시킨다
    /// </summary>
    [MessagePackObject]
    public struct BettingDeductionBroadcast : IPacket
    {
        [Key(0)] public int DeductedAmount; // 차감된 베팅금
        [Key(1)] public int TotalPrizeMoney; // 업데이트된 총 판돈
    }

    #endregion

    #region 플레이어 스탯 패킷들

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 소지금이 Balance로 바뀌었다
    /// </summary>
    [MessagePackObject]
    public struct BalanceUpdateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int Balance;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어의 정확도 증감 상태를 변경하겠다고 요청
    /// </summary>
    [MessagePackObject]
    public struct UpdateAccuracyStateFromClient : IPacket
    {
        [Key(0)] public int AccuracyState; // -1: 정확도 감소, 0: 정확도 유지, 1: 정확도 증가
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 정확도 상태가 AccuracyState로 변경되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateAccuracyStateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int AccuracyState; // -1: 정확도 감소, 0: 정확도 유지, 1: 정확도 증가
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 정확도가 Accuracy로 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateAccuracyBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int Accuracy;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 사거리가 Range로 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateRangeBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public float Range;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 속도가 Speed로 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateSpeedBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public float Speed;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 활성 효과 목록이 업데이트 되었다 (UI 표시용)
    /// 아이템 ID: 1~999, 증강 ID: 1000+
    /// </summary>
    [MessagePackObject]
    public struct UpdateActiveEffectsBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public List<int> ActiveEffects; // 활성 효과 ID 목록
    }

    #endregion

    #region 에너지 시스템 패킷들

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 현재 에너지가 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateCurrentEnergyBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public float CurrentEnergy;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 최대 에너지가 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateMaxEnergyBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int MaxEnergy;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 사격 최소 필요 에너지가 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateMinEnergyToShootBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int MinEnergyToShoot;
    }

    #endregion

    #region 쇼다운 시스템 패킷들

    /// <summary>
    /// [서버 => 클라이언트]
    /// 쇼다운 시작 - 정확도 배정 완료, 3초 후 자동으로 라운드 시작
    /// </summary>
    [MessagePackObject]
    public struct ShowdownStartFromServer : IPacket
    {
        [Key(0)] public Dictionary<int, int> AccuracyPool;
    }

    #endregion

    #region 증강 시스템 패킷들

    /// <summary>
    /// [서버 => 클라이언트]
    /// 라운드 종료 후 증강 선택을 시작할 때 보내는 패킷
    /// 3개의 증강 옵션을 제공한다
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
        [Key(0)] public int SelectedAugmentID; // 선택한 증강의 번호
    }

    #endregion

    #region 아이템 시스템 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 아이템 제작 상태를 변경하겠다는 패킷
    /// </summary>
    [MessagePackObject]
    public struct UpdateItemCreatingStateFromClient : IPacket
    {
        [Key(0)] public bool IsCreatingItem; // true: 제작 시작, false: 제작 취소
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 아이템 제작 상태가 변경되었음을 알리는 패킷
    /// </summary>
    [MessagePackObject]
    public struct UpdateCreatingStateBroadcast : IPacket
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
    }

    #endregion

    #region 채팅 시스템 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 채팅 메시지를 보내는 패킷
    /// </summary>
    [MessagePackObject]
    public struct ChatFromClient : IPacket
    {
        [Key(0)] public string Message;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 채팅 메시지 브로드캐스트
    /// </summary>
    [MessagePackObject]
    public struct ChatBroadcast : IPacket
    {
        [Key(0)] public int PlayerId; // 시스템 메시지일 경우 -1
        [Key(1)] public string Message;
    }

    #endregion
}