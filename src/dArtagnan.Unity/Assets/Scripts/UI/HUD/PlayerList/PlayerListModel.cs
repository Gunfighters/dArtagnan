using Game;
using Game.Player.Components;
using Game.Player.Data;
using ObservableCollections;
using R3;

namespace UI.HUD.PlayerList
{
    public class PlayerListModel
    {
        public readonly ObservableList<PlayerModel> PlayerList = new();

        public PlayerListModel()
        {
            GameService
                .PlayerModels
                .ObserveDictionaryAdd()
                .Subscribe(e => PlayerList.Add(e.Value));
            GameService
                .PlayerModels
                .ObserveDictionaryRemove()
                .Subscribe(e => PlayerList.Remove(e.Value));
        }
    }
}