using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 상점 아이템 구매 명령 - 플레이어가 상점에서 아이템을 구매할 때 처리합니다
/// </summary>
public class ShopPurchaseItemCommand : IGameCommand
{
    required public int PlayerId;
    required public ItemId ItemId;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;
        
        // Shop 상태가 아니면 무시
        if (gameManager.CurrentGameState != GameState.Shop)
        {
            Logger.log($"[상점] 플레이어 {PlayerId}의 구매 요청이 잘못된 상태에서 도착: {gameManager.CurrentGameState}");
            return;
        }
        
        var shopData = player.ShopData;
        
        // None 아이템은 구매할 수 없음
        if(ItemId == ItemId.None)
        {
            Logger.log($"[상점] 플레이어 {PlayerId}가 구매할 수 없는 아이템: {ItemId}");
            return;
        }

        // 구매할 아이템이 플레이어의 상점 목록에 있는지 확인
        var shopItemIndex = -1;
        ShopItem shopItem = default;
        
        for (int i = 0; i < shopData.YourItems.Count; i++)
        {
            if (shopData.YourItems[i].ItemId == ItemId)
            {
                shopItemIndex = i;
                shopItem = shopData.YourItems[i];
                break;
            }
        }
        if (shopItemIndex == -1)
        {
            Logger.log($"[상점] 플레이어 {PlayerId}가 구매할 수 없는 아이템: {ItemId} (없거나 이미 구매됨)");
            return;
        }
        
        // 잔액 확인 - 구매 후 잔액이 0원 초과여야 함
        if (player.Balance - shopItem.Price <= 0)
        {
            Logger.log($"[상점] 플레이어 {PlayerId} 구매 불가: 구매 후 잔액이 0원 이하가 됨 ({player.Balance} - {shopItem.Price} = {player.Balance - shopItem.Price})");
            return;
        }
        
        // 돈 차감
        await player.Withdraw(shopItem.Price);
        
        // 아이템 획득
        await player.AddItem(ItemId);
        
        // 구매한 아이템을 None 아이템으로 변경
        var updatedShopData = shopData;
        var noneItem = ItemConstants.Items[ItemId.None];
        updatedShopData.YourItems[shopItemIndex] = new ShopItem
        {
            ItemId = noneItem.Id,
            Price = noneItem.BasePrice,
            Name = noneItem.Name,
            Description = noneItem.Description
        };
        player.ShopData = updatedShopData;
        
        // 해당 플레이어에게 업데이트된 상점 목록 전송
        await gameManager.SendToPlayer(PlayerId, new ShopDataUpdateFromServer
        {
            ShopData = updatedShopData
        });
        
        Logger.log($"[상점] 플레이어 {PlayerId}({player.Nickname})가 {ItemId} 구매 완료 ({shopItem.Price}달러)");
    }
}