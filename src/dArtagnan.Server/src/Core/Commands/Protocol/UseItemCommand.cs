using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 아이템 사용 명령 - 플레이어가 소지한 아이템을 사용할 때 처리합니다
/// </summary>
public class UseItemCommand : IGameCommand
{
    required public int PlayerId;
    required public int TargetPlayerId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null || !player.Alive) return;

        // 소지한 아이템이 없으면 사용 불가
        if (player.CurrentItem == -1)
        {
            Console.WriteLine($"[아이템] 플레이어 {PlayerId}는 사용할 아이템이 없음");
            return;
        }

        // 재장전 중이면 아이템 사용 불가 (공유 쿨타임)
        // if (player.RemainingReloadTime > 0)
        // {
        //     Console.WriteLine($"[아이템] 플레이어 {PlayerId}는 쿨타임 중으로 아이템 사용 불가");
        //     return;
        // }

        int usedItemId = player.UseItem();
        
        // // 아이템 사용으로 인한 쿨타임 적용 (임시로 기본 재장전 시간 사용)
        // player.RemainingReloadTime = player.TotalReloadTime;

        // 아이템 사용 브로드캐스트
        await gameManager.BroadcastToAll(new ItemUsedBroadcast
        {
            PlayerId = PlayerId,
            ItemId = usedItemId
        });

        // 아이템 효과 적용 (임시 구현 - 나중에 아이템별로 세분화)
        await ApplyItemEffect(gameManager, player, usedItemId, TargetPlayerId);
    }

    /// <summary>
    /// 아이템 효과를 적용합니다 (임시 구현)
    /// </summary>
    private static async Task ApplyItemEffect(GameManager gameManager, Player user, int itemId, int targetId)
    {
        Console.WriteLine($"[아이템] 플레이어 {user.Id}가 아이템 {itemId} 효과 적용 (대상: {targetId})");
        
        // 임시로 아이템 효과 없이 로그만 출력
        // 나중에 아이템 종류별로 실제 효과 구현 예정
        switch (itemId)
        {
            case 1:
                Console.WriteLine($"[아이템] 아이템 1 효과: 치유");
                break;
            case 2:
                Console.WriteLine($"[아이템] 아이템 2 효과: 공격력 증가");
                break;
            case 3:
                Console.WriteLine($"[아이템] 아이템 3 효과: 방어력 증가");
                break;
            case 4:
                Console.WriteLine($"[아이템] 아이템 4 효과: 속도 증가");
                break;
            case 5:
                Console.WriteLine($"[아이템] 아이템 5 효과: 정확도 증가");
                break;
            default:
                Console.WriteLine($"[아이템] 알 수 없는 아이템 {itemId}");
                break;
        }
        
        await Task.CompletedTask;
    }
} 