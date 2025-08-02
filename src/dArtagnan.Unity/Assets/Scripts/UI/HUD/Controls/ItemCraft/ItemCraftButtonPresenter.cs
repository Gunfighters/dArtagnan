using R3;

namespace UI.HUD.Controls.ItemCraft
{
    public static class ItemCraftButtonPresenter
    {
        public static void Initialize(ItemCraftButtonView view)
        {
            ItemCraftButtonModel.ItemId.Subscribe(id =>
            {
                if (id == 0)
                    view.HideItem();
                else
                    view.ShowItem(id);
            });
        }
    }
}