using ObservableCollections;
using R3;

namespace UI.HUD.PlayerList
{
    public static class PlayerListPresenter
    {
        public static void Initialize(PlayerListModel model, PlayerListView view)
        {
            model.PlayerList.ObserveAdd().Subscribe(e => view.Add(e.Value));
            model.PlayerList.ObserveRemove().Subscribe(e => view.Remove(e.Value));
        }
    }
}