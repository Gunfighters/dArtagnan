using dArtagnan.Shared;
using R3;

namespace UI.HUD.Controls.ItemCraft
{
    public static class ItemCraftButtonPresenter
    {
        public static void Initialize(ItemCraftButtonModel model, ItemCraftButtonView view)
        {
            model.ItemId.Subscribe(id =>
            {
                if (id == ItemId.None)
                    view.HideItem();
                else
                    view.ShowItem(id);
            });
        }
    }
}