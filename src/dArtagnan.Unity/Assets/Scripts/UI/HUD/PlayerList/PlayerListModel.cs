using Game;
using Game.Player.Components;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.HUD.PlayerList
{
    public static class PlayerListModel
    {
        public static readonly ObservableList<PlayerCore> PlayerList = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            PlayerGeneralManager
                .Players
                .ObserveDictionaryAdd()
                .Subscribe(e => PlayerList.Add(e.Value));
            PlayerGeneralManager
                .Players
                .ObserveDictionaryRemove()
                .Subscribe(e => PlayerList.Remove(e.Value));
        }
    }
}