using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 아이템 제작 상태 변경 명령 - 플레이어가 아이템 제작을 시작하거나 취소할 때 처리합니다
/// </summary>
public class ItemCreatingStateCommand : IGameCommand
{
    required public int PlayerId;
    required public bool IsCreatingItem;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null || !player.Alive) return;

        // 제작 상태 변경
        if (IsCreatingItem)
        {
            // 이미 제작 중이면 중복 요청 무시
            if (player.IsCreatingItem)
            {
                Console.WriteLine($"[아이템] 플레이어 {PlayerId}는 이미 제작 중");
                return;
            }
            
            // 에너지 체크
            if (!player.ConsumeEnergy(Constants.CRAFT_ENERGY_COST))
            {
                Console.WriteLine($"[아이템] 플레이어 {PlayerId} 제작 불가 (에너지 부족: {player.EnergyData.CurrentEnergy:F1}/{player.EnergyData.MaxEnergy})");
                return;
            }

            // 현재 에너지만 브로드캐스트
            await gameManager.BroadcastToAll(new UpdateCurrentEnergyBroadcast
            {
                PlayerId = PlayerId,
                CurrentEnergy = player.EnergyData.CurrentEnergy
            });
            
            player.StartCreatingItem();
        }
        else
        {
            // 제작 중이 아니면 취소 요청 무시
            if (!player.IsCreatingItem)
            {
                Console.WriteLine($"[아이템] 플레이어 {PlayerId}는 제작 중이 아님");
                return;
            }
            
            player.CancelCreatingItem();
        }

        // 모든 플레이어들에게 제작 상태 변경 브로드캐스트
        await gameManager.BroadcastToAll(new UpdateCreatingStateBroadcast
        {
            PlayerId = PlayerId,
            IsCreatingItem = player.IsCreatingItem
        });
    }
} 