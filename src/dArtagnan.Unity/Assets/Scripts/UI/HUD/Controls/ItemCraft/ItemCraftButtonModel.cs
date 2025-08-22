using dArtagnan.Shared;
using Game;
using R3;

namespace UI.HUD.Controls.ItemCraft
{
    public static class ItemCraftButtonModel
    {
        public static readonly ReactiveProperty<ItemId> ItemId = new();

        public static void Initialize()
        {
            LocalEventChannel.OnLocalPlayerNewItem += id => ItemId.Value = id;
            PacketChannel.On<ItemAcquiredBroadcast>(e =>
            {
                if (e.PlayerId == GameService.LocalPlayer.ID)
                    ItemId.Value = (ItemId)e.ItemId;
            });
            PacketChannel.On<ItemUsedBroadcast>(e =>
            {
                if (e.PlayerId == GameService.LocalPlayer.ID)
                    ItemId.Value = dArtagnan.Shared.ItemId.None;
            });
        }
    }
}