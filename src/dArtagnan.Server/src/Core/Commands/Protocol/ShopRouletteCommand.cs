using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 상점 룰렛 명령 - 플레이어가 상점에서 룰렛을 돌릴 때 처리합니다
/// </summary>
public class ShopRouletteCommand : IGameCommand
{
    required public int PlayerId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;
        
        // Shop 상태가 아니면 무시
        if (gameManager.CurrentGameState != GameState.Shop)
        {
            Logger.log($"[샵룰렛] 플레이어 {PlayerId}의 룰렛 요청이 잘못된 상태에서 도착: {gameManager.CurrentGameState}");
            return;
        }
        
        var shopData = player.ShopData;
        
        // 잔액 확인
        if (player.Balance <= shopData.ShopRoulettePrice)
        {
            Logger.log($"[샵룰렛] 플레이어 {PlayerId} 잔액 이하: {player.Balance} <= {shopData.ShopRoulettePrice}");
            return;
        }
        
        // 룰렛 풀에서 랜덤 선택
        if (shopData.ShopRoulettePool.Count == 0)
        {
            Logger.log($"[샵룰렛] 플레이어 {PlayerId}의 룰렛 풀이 비어있음");
            return;
        }

        Logger.log($"[샵룰렛] 플레이어 {PlayerId}의 현재 룰렛 풀: [{string.Join(", ", shopData.ShopRoulettePool)}]");
        var randomAccuracy = shopData.ShopRoulettePool[Random.Shared.Next(shopData.ShopRoulettePool.Count)];
        Logger.log($"[샵룰렛] 플레이어 {PlayerId}가 {randomAccuracy}% 뽑음");

        // 단골 할인 효과: 50% 확률로 비용 0원
        int finalPrice = shopData.ShopRoulettePrice;
        bool discountApplied = false;
        if (player.GetItemCount(ItemId.RegularDiscount) > 0 && Random.Shared.NextDouble() < ItemConstants.RegularDiscount.FREE_PROBABILITY)
        {
            finalPrice = 0;
            discountApplied = true;
            Logger.log($"[단골할인] 플레이어 {PlayerId}에게 룰렛 할인 적용 (0원)");
        }

        // 돈 차감
        await player.Withdraw(finalPrice);
        
        // 정확도 변경
        await player.SetAccuracy(randomAccuracy);
        
        // 해당 플레이어에게 룰렛 결과 전송
        await gameManager.SendToPlayer(PlayerId, new ShopRouletteResultFromServer
        {
            NewAccuracy = randomAccuracy,
            ShopRoulettePrice = finalPrice
        });
    }
}