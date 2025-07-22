using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어가 증강을 선택했을 때 처리하는 명령
/// </summary>
public class AugmentDoneCommand : IGameCommand
{
    required public int ClientId;
    required public int SelectedAugmentIndex;
    
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

        // 증강 인덱스 유효성 검증 (0, 1, 2)
        if (SelectedAugmentIndex < 0 || SelectedAugmentIndex > 2)
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어 - 잘못된 증강 인덱스: {SelectedAugmentIndex}");
            return;
        }

        // 플레이어의 증강 옵션 가져오기
        if (!gameManager.playerAugmentOptions.TryGetValue(ClientId, out var augmentOptions))
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어 - 증강 옵션을 찾을 수 없음");
            return;
        }

        // 선택된 증강 ID 가져오기
        if (SelectedAugmentIndex >= augmentOptions.Count)
        {
            Console.WriteLine($"[증강] {ClientId}번 플레이어 - 증강 인덱스 범위 초과: {SelectedAugmentIndex}");
            return;
        }
        
        var selectedAugmentId = augmentOptions[SelectedAugmentIndex];
        
        // 플레이어에게 증강 추가
        player.Augments.Add(selectedAugmentId);
        
        // 증강 효과 적용
        ApplyAugmentEffect(player, selectedAugmentId);
        
        // 선택 완료 표시
        gameManager.augmentSelectionDonePlayers.Add(ClientId);
        
        Console.WriteLine($"[증강] {ClientId}번 플레이어가 증강 {selectedAugmentId}를 획득했습니다");

        // 모든 플레이어가 증강을 선택했는지 확인
        var alivePlayers = gameManager.Players.Values.Where(p => !p.Bankrupt).ToList();
        
        if (gameManager.augmentSelectionDonePlayers.Count >= alivePlayers.Count)
        {
            Console.WriteLine("[증강] 모든 플레이어가 증강 선택 완료 - 다음 라운드 시작");
            await gameManager.StartNextRoundAsync(gameManager.Round + 1);
        }
    }

    private void ApplyAugmentEffect(Player player, int augmentId)
    {
        // 예시 증강들
        switch (augmentId)
        {
            case 1: // 정확도 +10
                player.Accuracy = Math.Min(100, player.Accuracy + 10);
                break;
            case 2: // 사거리 +1
                player.Range += 1f;
                break;
            case 3: // 재장전 시간 -2초
                player.TotalReloadTime = Math.Max(3f, player.TotalReloadTime - 2f);
                break;
            case 4: // 정확도 +15
                player.Accuracy = Math.Min(100, player.Accuracy + 15);
                break;
            case 5: // 사거리 +1.5
                player.Range += 1.5f;
                break;
            default:
                Console.WriteLine($"[증강] 알 수 없는 증강 ID: {augmentId}");
                break;
        }
    }
} 