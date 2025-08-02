using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// AI 봇 클래스 - Player를 상속받아 게임에 참여하는 봇입니다
/// 멈춰있으면서 10초마다 정확도 상태를 변경하고, 쿨타임마다 사격합니다
/// </summary>
public class Bot : Player
{
    private float accuracyStateTimer = 0f;
    private const float ACCURACY_STATE_CHANGE_INTERVAL = 10.0f; // 10초마다 정확도 상태 변경
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
    public void UpdateAI(float deltaTime)
    {
        if (!Alive) return;

        // 정확도 상태 변경 타이머 업데이트
        UpdateAccuracyStateTimer(deltaTime);
        
        // 사격 로직 업데이트
        UpdateShootingLogic();
    }

    /// <summary>
    /// 10초마다 정확도 상태를 랜덤으로 변경합니다
    /// </summary>
    private void UpdateAccuracyStateTimer(float deltaTime)
    {
        accuracyStateTimer += deltaTime;
        
        if (accuracyStateTimer >= ACCURACY_STATE_CHANGE_INTERVAL)
        {
            // -1(감소), 0(유지), 1(증가) 중 랜덤 선택
            var newAccuracyState = random.Next(-1, 2);
            AccuracyState = newAccuracyState;
            
            Console.WriteLine($"[봇 AI] {Nickname}의 정확도 상태 변경: {AccuracyState}");
            
            // 정확도 상태 변경 명령 실행
            _ = Task.Run(async () =>
            {
                await gameManager.EnqueueCommandAsync(new SetAccuracyCommand 
                { 
                    PlayerId = Id,
                    AccuracyState = AccuracyState
                });
            });
            
            accuracyStateTimer = 0f;
        }
    }

    /// <summary>
    /// 쿨타임이 끝나면 사거리 내 타겟을 찾아 사격합니다
    /// </summary>
    private void UpdateShootingLogic()
    {
        // 쿨타임이 남아있으면 사격 불가
        if (RemainingReloadTime > 0) return;
        
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

        // 사격 명령 실행 (PlayerShootingCommand와 동일한 로직)
        _ = Task.Run(async () =>
        {
            await gameManager.EnqueueCommandAsync(new PlayerShootingCommand 
            { 
                ShooterId = Id,
                TargetId = target.Id
            });
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
            case YourAccuracyAndPool accuracyPacket:
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
        // 약간의 지연 후 룰렛 완료 (0.5~2초 랜덤)
        await Task.Delay(random.Next(500, 2000));
        
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