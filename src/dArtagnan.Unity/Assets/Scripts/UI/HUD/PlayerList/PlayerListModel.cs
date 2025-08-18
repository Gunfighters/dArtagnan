using Game;
using Game.Player.Components;
using ObservableCollections;
using R3;

namespace UI.HUD.PlayerList
{
    public class PlayerListModel
    {
        public readonly ObservableList<PlayerCore> PlayerList = new();

        public PlayerListModel()
        {
            GameService
                .Players
                .ObserveDictionaryAdd()
                .Subscribe(e => PlayerList.Add(e.Value));
            GameService
                .Players
                .ObserveDictionaryRemove()
                .Subscribe(e => PlayerList.Remove(e.Value));
        }
    }
}