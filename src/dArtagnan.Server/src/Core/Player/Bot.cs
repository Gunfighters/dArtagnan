using System.Numerics;
using System.Threading.Tasks;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// AI 봇 클래스 - Player를 상속받아 게임에 참여하는 봇입니다
/// 멈춰있으면서 5초마다 정확도 상태 변경과 사격을 각각 일정 확률로 시도합니다
/// </summary>
public class Bot : Player
{
    private float accuracyStateTimer = 0f;
    private float shootingTimer = 0f;
    private const float ACTION_INTERVAL = 1.0f; // 1초마다 행동 시도
    private const float ACCURACY_ACTION_CHANCE = 0.15f; // 15% 확률로 정확도 상태 변경
    private const float SHOOTING_ACTION_CHANCE = 0.2f; // 20% 확률로 사격 시도
    private Random random = new Random();
    private readonly GameManager gameManager;
    
    public Bot(int id, string nickname, Vector2 position, GameManager gameManager) : base(id, nickname, position)
    {
        MovementData.Speed = 0f;
        this.gameManager = gameManager;
    }

    /// <summary>
    /// 봇의 AI 로직을 업데이트합니다 (GameLoop에서 호출)
    /// </summary>
    public async Task UpdateAI(float deltaTime)
    {
        if (!Alive) return;

        // 정확도 상태 변경 타이머 업데이트
        await UpdateAccuracyStateTimer(deltaTime);
        
        // 사격 타이머 업데이트  
        await UpdateShootingTimerAsync(deltaTime);
    }

    /// <summary>
    /// 5초마다 정확도 상태를 랜덤으로 변경합니다 (70% 확률)
    /// </summary>
    private async Task UpdateAccuracyStateTimer(float deltaTime)
    {
        accuracyStateTimer += deltaTime;
        
        if (accuracyStateTimer >= ACTION_INTERVAL)
        {
            // 70% 확률로 정확도 상태 변경, 아니면 건너뛰기
            if (random.NextDouble() > ACCURACY_ACTION_CHANCE)
            {
                accuracyStateTimer = 0f;
                return;
            }

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
            
            accuracyStateTimer = 0f;
        }
    }

    /// <summary>
    /// 5초마다 사거리 내 타겟을 찾아 사격합니다 (80% 확률)
    /// </summary>
    private async Task UpdateShootingTimerAsync(float deltaTime)
    {
        shootingTimer += deltaTime;
        
        if (shootingTimer >= ACTION_INTERVAL)
        {
            // 80% 확률로 사격 시도, 아니면 건너뛰기
            if (random.NextDouble() > SHOOTING_ACTION_CHANCE)
            {
                shootingTimer = 0f;
                return;
            }

            // 쿨타임이 남아있으면 사격 불가
            if (RemainingReloadTime > 0)
            {
                shootingTimer = 0f;
                return;
            }
            
            // 사거리 내에 있는 살아있는 다른 플레이어들을 찾습니다
            var potentialTargets = gameManager.Players.Values
                .Where(p => p.Id != Id && p.Alive)
                .Where(p => Vector2.Distance(MovementData.Position, p.MovementData.Position) <= Range)
                .ToList();

            if (potentialTargets.Count == 0)
            {
                shootingTimer = 0f;
                return;
            }

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
            
            shootingTimer = 0f;
        }
    }

    /// <summary>
    /// 브로드캐스트 패킷을 받았을 때 봇의 반응을 처리합니다
    /// </summary>
    public async Task HandlePacketAsync(IPacket packet)
    {
        // 대부분의 패킷은 무시하지만, 선택이 필요한 패킷들은 자동으로 처리
        switch (packet)
        {
            case RouletteStartFromServer accuracyPacket:
                // 룰렛 완료 자동 처리
                await HandleRouletteCompletion();
                break;
                
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
    /// 룰렛 완료를 자동으로 처리합니다
    /// </summary>
    private async Task HandleRouletteCompletion()
    {
        Console.WriteLine($"[봇 AI] {Nickname}이 룰렛을 자동 완료합니다");
        
        // RouletteDoneCommand 직접 실행
        await gameManager.EnqueueCommandAsync(new RouletteDoneCommand 
        { 
            PlayerId = Id 
        });
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