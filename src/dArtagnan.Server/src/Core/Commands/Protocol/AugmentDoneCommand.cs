using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어가 증강을 선택했을 때 처리하는 명령
/// </summary>
public class AugmentDoneCommand : IGameCommand
{
    required public int ClientId;
    required public int SelectedAugmentId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        // 게임 상태 확인
        if (gameManager.CurrentGameState != GameState.Augment)
        {
            Console.WriteLine($"[증강] {ClientId}번 클라이언트 - 잘못된 게임 상태에서 증강 선택 시도");
            return;
        }

        // 플레이어 확인
        var player = gameManager.GetPlayerById(ClientId);
        if (player == null)
        {
            Console.WriteLine($"[증강] 존재하지 않는 플레이어: {ClientId}");
            return;
        }

        // 파산한 플레이어는 증강 선택 불가
        if (player.Bankrupt)
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어 - 파산으로 인한 증강 선택 불가");
            return;
        }

        // 이미 선택한 플레이어인지 확인
        if (gameManager.augmentSelectionDonePlayers.Contains(ClientId))
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어 - 이미 증강 선택 완료");
            return;
        }

        // 플레이어의 증강 옵션 가져오기
        if (!gameManager.playerAugmentOptions.TryGetValue(ClientId, out var augmentOptions))
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어 - 증강 옵션을 찾을 수 없음");
            return;
        }

        if (!augmentOptions.Contains(SelectedAugmentId))
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어 - 제시되지 않은 증강을 선택: {SelectedAugmentId}번 증강");
            return;
        }

        // -1 (없음) 선택 시 처리
        if (SelectedAugmentId == -1)
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어가 증강 선택을 패스했습니다");
        }
        else
        {
            // 증강 데이터 확인
            var augmentId = (AugmentId)SelectedAugmentId;
            if (!AugmentConstants.Augments.TryGetValue(augmentId, out var augmentData))
            {
                Console.WriteLine($"[증강] 알 수 없는 증강 ID: {SelectedAugmentId}");
                return;
            }

            // 플레이어에게 증강 추가
            player.Augments.Add(SelectedAugmentId);
            
            // 증강 효과 적용
            await ApplyAugmentEffect(gameManager, player, augmentId);
            
            Console.WriteLine($"[증강] {ClientId}번 플레이어가 증강 {augmentData.Name}({SelectedAugmentId})를 획득했습니다");
        }

        // 선택 완료 표시
        gameManager.augmentSelectionDonePlayers.Add(ClientId);

        // 모든 플레이어가 증강을 선택했는지 확인
        var alivePlayers = gameManager.Players.Values.Where(p => !p.Bankrupt).ToList();
        
        if (gameManager.augmentSelectionDonePlayers.Count >= alivePlayers.Count)
        {
            Console.WriteLine("[증강] 모든 플레이어가 증강 선택 완료 - 다음 라운드 시작");
            await gameManager.StartShowdownStateAsync();
        }
    }

    private async Task ApplyAugmentEffect(GameManager gameManager, Player player, AugmentId augmentId)
    {
        switch (augmentId)
        {
            case AugmentId.AccuracyStateDoubleApplication:
                // AccuracyState 두 배 적용 - 플레이어 객체에 표시만 함, 실제 적용은 게임루프에서
                player.ActiveEffects.Add((int)augmentId);
                Console.WriteLine($"[증강] 플레이어 {player.Id}가 정확도 상태 두 배 적용 증강 획득");
                
                // 활성 효과 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateActiveEffectsBroadcast
                {
                    PlayerId = player.Id,
                    ActiveEffects = player.ActiveEffects
                });
                break;
                
            case AugmentId.HalfBettingCost:
                // 베팅금 절반 - 실제 적용은 베팅 차감 시
                player.ActiveEffects.Add((int)augmentId);
                Console.WriteLine($"[증강] 플레이어 {player.Id}가 베팅금 절반 증강 획득");
                
                // 활성 효과 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateActiveEffectsBroadcast
                {
                    PlayerId = player.Id,
                    ActiveEffects = player.ActiveEffects
                });
                break;
                
            case AugmentId.MaxEnergyIncrease:
                // 최대 에너지 1 증가
                player.UpdateMaxEnergy(player.EnergyData.MaxEnergy + AugmentConstants.MAX_ENERGY_INCREASE_AMOUNT);
                player.ActiveEffects.Add((int)augmentId);
                Console.WriteLine($"[증강] 플레이어 {player.Id}의 최대 에너지가 {player.EnergyData.MaxEnergy}로 증가");
                
                // 최대 에너지 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateMaxEnergyBroadcast
                {
                    PlayerId = player.Id,
                    MaxEnergy = player.EnergyData.MaxEnergy
                });
                
                // 활성 효과 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateActiveEffectsBroadcast
                {
                    PlayerId = player.Id,
                    ActiveEffects = player.ActiveEffects
                });
                break;
                
            case AugmentId.DoubleMoneySteakOnKill:
                // 돈 획득 두 배 - 실제 적용은 사격 시
                player.ActiveEffects.Add((int)augmentId);
                Console.WriteLine($"[증강] 플레이어 {player.Id}가 돈 획득 두 배 증강 획득");
                
                // 활성 효과 변경 브로드캐스트
                await gameManager.BroadcastToAll(new UpdateActiveEffectsBroadcast
                {
                    PlayerId = player.Id,
                    ActiveEffects = player.ActiveEffects
                });
                break;
                
            default:
                Console.WriteLine($"[증강] 알 수 없는 증강 ID: {augmentId}");
                break;
        }
    }
} 