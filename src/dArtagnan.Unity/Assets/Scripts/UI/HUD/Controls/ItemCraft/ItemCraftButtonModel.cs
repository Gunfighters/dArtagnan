using dArtagnan.Shared;
using Game;
using R3;

namespace UI.HUD.Controls.ItemCraft
{
    public class ItemCraftButtonModel
    {
        public readonly ReactiveProperty<ItemId> ItemId = new();

        public ItemCraftButtonModel()
        {
            GameService.LocalPlayerSet.Subscribe(player => player.CurrentItem.Subscribe(id => ItemId.Value = id));
        }
    }
}