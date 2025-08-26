using System.Numerics;
using System.Threading.Tasks;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// AI 봇 클래스 - Player를 상속받아 게임에 참여하는 봇입니다
/// 1초마다 일정 확률로 이동 방향 변경(10%), 정확도 상태 변경(15%), 사격(20%)을 시도합니다
/// </summary>
public class Bot : Player
{
    private float actionTimer = 0f; // 모든 행동을 위한 통합 타이머
    private const float ACTION_INTERVAL = 1.0f; // 1초마다 행동 시도
    private const float ACCURACY_ACTION_CHANCE = 0.15f; // 15% 확률로 정확도 상태 변경
    private const float SHOOTING_ACTION_CHANCE = 0.2f; // 20% 확률로 사격 시도
    private Random random = new Random();
    private readonly GameManager gameManager;
    
    public Bot(int id, string nickname, Vector2 position, GameManager gameManager) : base(id, nickname, position)
    {
        MovementData.Speed = Constants.MOVEMENT_SPEED; // 기본 이동 속도 설정
        this.gameManager = gameManager;
    }
    
    /// <summary>
    /// 라운드 시작 시 봇 초기화를 오버라이드
    /// </summary>
    public new void InitToRound()
    {
        base.InitToRound();
        
        actionTimer = 0f; // 액션 타이머 리셋
    }

    /// <summary>
    /// 봇의 AI 로직을 업데이트합니다 (GameLoop에서 호출)
    /// </summary>
    public async Task UpdateAI(float deltaTime)
    {
        if (!Alive) return;

        actionTimer += deltaTime;
        
        if (actionTimer >= ACTION_INTERVAL)
        {
            await TryMovement();
            await TryAccuracyChange();
            await TryShooting();
            
            actionTimer = 0f;
        }
    }

    /// <summary>
    /// 10% 확률로 이동 상태를 변경합니다
    /// 70% 확률로 정지, 20% 확률로 현상유지, 10% 확률로 새로운 방향 이동
    /// </summary>
    private async Task TryMovement()
    {
        var rand = random.NextDouble();
        int newDirection;
        string actionDescription;
        
        if (rand <= 0.80) // 정지
        {
            newDirection = 0;
            actionDescription = "정지";
        }
        else if (rand <= 0.90) // 현재 방향 그대로 유지
        {
            return; 
        }
        else // 10% 확률로 새로운 방향 (1~8)
        {
            newDirection = random.Next(1, 9); // 정지(0) 제외한 1~8 방향
            actionDescription = newDirection switch
            {
                1 => "위로 이동",
                2 => "오른쪽 위로 이동",
                3 => "오른쪽으로 이동",
                4 => "오른쪽 아래로 이동",
                5 => "아래로 이동",
                6 => "왼쪽 아래로 이동",
                7 => "왼쪽으로 이동",
                8 => "왼쪽 위로 이동",
                _ => "알 수 없는 방향"
            };
        }
        
        // 이동 방향 변경
        MovementData.Direction = newDirection;
        
        Console.WriteLine($"[봇 AI] {Nickname}의 행동: {actionDescription} (방향값: {newDirection})");
        
        // 이동 데이터 브로드캐스트
        await gameManager.BroadcastToAll(new MovementDataBroadcast
        {
            PlayerId = Id,
            MovementData = MovementData
        });
    }
    
    /// <summary>
    /// 15% 확률로 정확도 상태를 랜덤으로 변경합니다
    /// </summary>
    private async Task TryAccuracyChange()
    {
        // 15% 확률로 정확도 상태 변경
        if (random.NextDouble() > ACCURACY_ACTION_CHANCE) return;

        // -1(감소), 0(유지), 1(증가) 중 랜덤 선택
        var newAccuracyState = random.Next(-1, 2);
        AccuracyState = newAccuracyState;
        
        Console.WriteLine($"[봇 AI] {Nickname}의 정확도 상태 변경: {AccuracyState}");
        
        // 정확도 상태 변경 명령 실행
        await gameManager.EnqueueCommandAsync(new SetAccuracyCommand 
        { 
            PlayerId = Id,
            AccuracyState = AccuracyState
        });
    }

    /// <summary>
    /// 20% 확률로 사거리 내 타겟을 찾아 사격합니다
    /// </summary>
    private async Task TryShooting()
    {
        // 20% 확률로 사격 시도
        if (random.NextDouble() > SHOOTING_ACTION_CHANCE) return;

        // 에너지가 부족하면 사격 불가
        if (EnergyData.CurrentEnergy < MinEnergyToShoot) return;
        
        // 사거리 내에 있는 살아있는 다른 플레이어들을 찾습니다
        var potentialTargets = gameManager.Players.Values
            .Where(p => p.Id != Id && p.Alive)
            .Where(p => Vector2.Distance(MovementData.Position, p.MovementData.Position) <= Range)
            .ToList();

        if (potentialTargets.Count == 0) return;

        // 랜덤하게 타겟 선택
        var target = potentialTargets[random.Next(potentialTargets.Count)];
        Target = target;

        Console.WriteLine($"[봇 AI] {Nickname}이 {target.Nickname}을 타겟으로 사격 시도");

        // 사격 명령 실행
        await gameManager.EnqueueCommandAsync(new PlayerShootingCommand 
        { 
            ShooterId = Id,
            TargetId = target.Id
        });
    }

    /// <summary>
    /// 브로드캐스트 패킷을 받았을 때 봇의 반응을 처리합니다
    /// </summary>
    public async Task HandlePacketAsync(IPacket packet)
    {
        // 대부분의 패킷은 무시하지만, 선택이 필요한 패킷들은 자동으로 처리
        switch (packet)
        {
                
            case AugmentStartFromServer augmentPacket:
                // 증강 선택 자동 처리 (첫 번째 증강 선택)
                if (augmentPacket.AugmentOptions.Count > 0)
                {
                    await HandleAugmentSelection(augmentPacket.AugmentOptions[0]);
                }
                break;
                
            default:
                // 다른 패킷들은 무시
                break;
        }
    }


    /// <summary>
    /// 증강 선택을 자동으로 처리합니다
    /// </summary>
    private async Task HandleAugmentSelection(int augmentId)
    {
        Console.WriteLine($"[봇 AI] {Nickname}이 증강 {augmentId}를 자동 선택합니다");
        
        await gameManager.EnqueueCommandAsync(new AugmentDoneCommand 
        { 
            ClientId = Id,
            SelectedAugmentId = augmentId
        });
    }
}