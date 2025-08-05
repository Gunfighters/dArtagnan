using ObservableCollections;
using R3;

namespace UI.HUD.PlayerList
{
    public static class PlayerListPresenter
    {
        public static void Initialize(PlayerListView view)
        {
            PlayerListModel.PlayerList.ObserveAdd().Subscribe(e => view.Add(e.Value));
            PlayerListModel.PlayerList.ObserveRemove().Subscribe(e => view.Remove(e.Value));
        }
    }
}