using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 사격 명령 - 사격 처리 및 피해 계산을 수행합니다
/// </summary>
public class PlayerShootingCommand : IGameCommand
{
    required public int ShooterId;
    required public int TargetId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var shooter = gameManager.GetPlayerById(ShooterId);

        if(!(gameManager.CurrentGameState == GameState.Round || gameManager.CurrentGameState == GameState.Waiting))
        {
            Logger.log($"[전투] 현재 게임 상태가 라운드나 대기 상태가 아닙니다");
            return;
        }

        if (shooter == null || !shooter.Alive)
        {
            Logger.log($"[전투] 플레이어 {ShooterId} 사격 불가 (사망)");
            return;
        }

        if (shooter.IsMining)
        {
            await gameManager.BroadcastToAll(new MiningStateBroadcast { IsMining = false, PlayerId = ShooterId });
            return;
        }
            
        var target = gameManager.GetPlayerById(TargetId);
        if (target == null || !target.Alive)
        {
            Logger.log($"[전투] 유효하지 않은 타겟: {TargetId}");
            return;
        }
        
        // 재장전 시간 체크
        if (!shooter.IsNotReloading())
        {
            Logger.log($"[전투] 플레이어 {ShooterId} 사격 불가 (재장전 중: {shooter.RemainingReloadTime:F1}초 남음)");
            return;
        }

        // 탄창 보유시 재장전 안함
        if(shooter.GetItemCount(ItemId.Magazine) > 0)
        {
            await shooter.RemoveItem(ItemId.Magazine);
        }
        else
        {
            await shooter.StartReloading();
        }
        
        // 명중률 계산 
        bool hit = Random.Shared.NextDouble() * 100 < shooter.Accuracy;
        
        Logger.log($"[전투] 플레이어 {shooter.Id} -> {target.Id} 사격: {(hit ? "명중" : "빗나감")}");
        
        // 사격 결과 브로드캐스트
        await gameManager.BroadcastToAll(new ShootingBroadcast
        {
            ShooterId = ShooterId,
            TargetId = TargetId,
            Hit = hit
        });

        //todo: 대기상태에서 할 수있는 행동들 통일성있게 정리
        if(gameManager.CurrentGameState == GameState.Waiting)
        {
            return;
        }

        if (hit)
        {
            // 타겟의 방탄조끼 체크
            if(target.GetItemCount(ItemId.BodyArmor) > 0)
            {
                await target.RemoveItem(ItemId.BodyArmor);
                await gameManager.BroadcastToAll(new ChatBroadcast
                {
                    PlayerId = -1,
                    Message = $"{target.Nickname}님의 방탄조끼가 총알을 막았습니다"
                });
                Logger.log($"[전투] 플레이어 {target.Id}의 방탄조끼가 총알을 막았습니다");
            }
            else
            {
                // 강탈 아이템 효과 적용 (타겟이 죽기 전에)
                if(shooter.GetItemCount(ItemId.Robbery) > 0)
                {
                    await shooter.RemoveItem(ItemId.Robbery);

                    // 타겟이 가진 상점 아이템 중 랜덤하게 하나 선택
                    var shopItems = target.Items.Where(item => ItemConstants.Items[item].Tags.HasFlag(ItemTags.ShopItem)).ToList();

                    if(shopItems.Count > 0)
                    {
                        var randomIndex = Random.Shared.Next(shopItems.Count);
                        var stolenItem = shopItems[randomIndex];

                        await target.RemoveItem(stolenItem, false);
                        await shooter.AddItem(stolenItem);

                        Logger.log($"[강탈] 플레이어 {shooter.Id}가 {target.Id}로부터 {stolenItem} 아이템을 강탈했습니다");

                        await gameManager.BroadcastToAll(new ChatBroadcast
                        {
                            PlayerId = -1,
                            Message = $"{shooter.Nickname}님이 {target.Nickname}님으로부터 {ItemConstants.Items[stolenItem].Name}을(를) 강탈했습니다!"
                        });
                    }
                }

                // 적을 죽이면 소지금의 25%를 빼앗고, 죽은 적은 추가로 베팅금만큼 감소 (증발)
                var deposited = Math.Max(gameManager.BaseBettingAmount, (int)Math.Round(target.Balance * 0.25));
                var withdrawn = deposited + gameManager.BaseBettingAmount;

                //다윗과 골리앗 (DavidGoliath): 나보다 명중률이 높은 사람을 처치하면 소지금을 2배 빼앗습니다.
                if(shooter.GetItemCount(ItemId.DavidGoliath) > 0 && target.Accuracy > shooter.Accuracy)
                {
                    deposited *= (int)ItemConstants.DavidGoliath.MONEY_STEAL_MULTIPLIER;
                    withdrawn = deposited + gameManager.BaseBettingAmount;
                }

                deposited = Math.Min(deposited, target.Balance);
                withdrawn = Math.Min(withdrawn, target.Balance);
                await gameManager.BroadcastToAll(new LootBroadcast
                {
                    FromPlayerId = target.Id,
                    ToPlayerId = shooter.Id,
                    LootAmount = deposited
                });
                await shooter.Deposit(deposited);
                await target.Withdraw(withdrawn);
                await target.Die();

                // 처치 시스템 메시지
                await gameManager.BroadcastToAll(new ChatBroadcast
                {
                    PlayerId = -1,
                    Message = $"{shooter.Nickname}님이 {target.Nickname}님을 처치했습니다!"
                });

                await gameManager.CheckAndHandleGameEndAsync();
            }
        }else{
            if(shooter.GetItemCount(ItemId.FearBullet) > 0)
            {
                await shooter.RemoveItem(ItemId.FearBullet);
                await target.SetAccuracy(ItemConstants.FearBullet.TARGET_ACCURACY_ON_MISS);

                Logger.log($"[Item][{shooter.Nickname}] 공포탄 효과: {target.Nickname}의 명중률이 {ItemConstants.FearBullet.TARGET_ACCURACY_ON_MISS}%로 감소");

                await gameManager.BroadcastToAll(new ChatBroadcast
                {
                    PlayerId = -1,
                    Message = $"{shooter.Nickname}님의 공포탄이 {target.Nickname}님을 공포에 떨게 했습니다!"
                });
            }
        }
    }
} 