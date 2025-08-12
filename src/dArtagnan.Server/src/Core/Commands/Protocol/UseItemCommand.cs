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

        // 아이템 정보 가져오기
        var itemId = (ItemId)player.CurrentItem;
        if (!ItemConstants.Items.TryGetValue(itemId, out var itemData))
        {
            Console.WriteLine($"[아이템] 알 수 없는 아이템 ID: {player.CurrentItem}");
            return;
        }

        // 에너지 체크
        if (itemData.EnergyCost > 0 && !player.ConsumeEnergy(itemData.EnergyCost))
        {
            Console.WriteLine($"[아이템] 플레이어 {PlayerId} 아이템 사용 불가 (에너지 부족: {player.EnergyData.CurrentEnergy:F1}/{player.EnergyData.MaxEnergy})");
            return;
        }

        // 에너지가 소모된 경우 브로드캐스트
        if (itemData.EnergyCost > 0)
        {
            await gameManager.BroadcastToAll(new UpdateCurrentEnergyBroadcast
            {
                PlayerId = PlayerId,
                CurrentEnergy = player.EnergyData.CurrentEnergy
            });
        }

        int usedItemId = player.UseItem();

        // 아이템 사용 브로드캐스트
        await gameManager.BroadcastToAll(new ItemUsedBroadcast
        {
            PlayerId = PlayerId,
            ItemId = usedItemId
        });

        // 아이템 효과 적용
        if (gameManager.CurrentGameState == GameState.Round)
        {
            await ApplyItemEffect(gameManager, player, itemId, TargetPlayerId);
        }
    }

    /// <summary>
    /// 아이템 효과를 적용합니다
    /// </summary>
    private static async Task ApplyItemEffect(GameManager gameManager, Player user, ItemId itemId, int targetId)
    {
        Console.WriteLine($"[아이템] 플레이어 {user.Id}가 {itemId} 효과 적용");
        
        switch (itemId)
        {
            case ItemId.SpeedBoost:
                // 속도 증가 효과
                user.ApplySpeedBoost(ItemConstants.SPEED_BOOST_DURATION, ItemConstants.SPEED_BOOST_MULTIPLIER);
                
                // 속도 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateSpeedBroadcast
                {
                    PlayerId = user.Id,
                    Speed = user.MovementData.Speed
                });
                
                // 활성 효과 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateActiveEffectsBroadcast
                {
                    PlayerId = user.Id,
                    ActiveEffects = user.ActiveEffects
                });
                break;
                
            case ItemId.EnergyRestore:
                // 에너지 회복
                user.RestoreEnergy(ItemConstants.ENERGY_RESTORE_AMOUNT);
                
                // 에너지 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateCurrentEnergyBroadcast
                {
                    PlayerId = user.Id,
                    CurrentEnergy = user.EnergyData.CurrentEnergy
                });
                break;
                
            case ItemId.DamageShield:
                // 피해 가드 적용
                user.ApplyDamageShield();
                
                // 활성 효과 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateActiveEffectsBroadcast
                {
                    PlayerId = user.Id,
                    ActiveEffects = user.ActiveEffects
                });
                break;
                
            case ItemId.AccuracyReset:
                // 정확도 재설정 (25~75 사이 랜덤)
                int newAccuracy = Random.Shared.Next(Constants.ROULETTE_MIN_ACCURACY, Constants.ROULETTE_MAX_ACCURACY + 1);
                user.Accuracy = newAccuracy;
                user.UpdateMinEnergyToShoot();
                
                Console.WriteLine($"[아이템] 플레이어 {user.Id}의 정확도 재설정: {newAccuracy}%");
                
                // 정확도 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateAccuracyBroadcast
                {
                    PlayerId = user.Id,
                    Accuracy = user.Accuracy
                });
                
                // 최소 에너지 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateMinEnergyToShootBroadcast
                {
                    PlayerId = user.Id,
                    MinEnergyToShoot = user.MinEnergyToShoot
                });
                
                // 사거리 변경 브로드캐스트
                float t = Math.Clamp(newAccuracy / (float)Constants.ROULETTE_MAX_ACCURACY, 0f, 1f);
                user.Range = Constants.MAX_RANGE + t * (Constants.MIN_RANGE - Constants.MAX_RANGE);
                
                await gameManager.BroadcastToAll(new UpdateRangeBroadcast
                {
                    PlayerId = user.Id,
                    Range = user.Range
                });
                break;
                
            default:
                Console.WriteLine($"[아이템] 처리되지 않은 아이템: {itemId}");
                break;
        }
    }
} 