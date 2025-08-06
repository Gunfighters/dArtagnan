using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.Controls.ItemCraft
{
    public static class ItemCraftButtonModel
    {
        public static readonly ReactiveProperty<ItemId> ItemId = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            PacketChannel.On<ItemAcquiredBroadcast>(e =>
            {
                if (e.PlayerId == PlayerGeneralManager.LocalPlayerCore.ID)
                    ItemId.Value = (ItemId)e.ItemId;
            });
            PacketChannel.On<ItemUsedBroadcast>(e =>
            {
                if (e.PlayerId == PlayerGeneralManager.LocalPlayerCore.ID)
                    ItemId.Value = 0;
            });
        }
    }
}