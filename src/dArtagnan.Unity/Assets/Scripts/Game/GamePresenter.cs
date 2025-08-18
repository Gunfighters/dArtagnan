using Game.Player.Components;
using ObservableCollections;
using R3;

namespace Game
{
    public static class GamePresenter
    {
        public static void Initialize(GameView view, GameModel model)
        {
            model.Players
                .ObserveAdd()
                .Subscribe(e => OnPlayerAdd(e.Value.Value))
                .AddTo(view);
            model.Players
                .ObserveRemove()
                .Subscribe(e => OnPlayerRemove(e.Value.Value))
                .AddTo(view);
            model.LocalPlayerId
                .Subscribe(OnLocalPlayerIdChanged)
                .AddTo(view);
            model.HostPlayerId
                .Subscribe(OnHostPlayerIdChanged)
                .AddTo(view);
        }

        private static void OnPlayerAdd(PlayerCore player)
        {
            // Player already created in GameModel.CreatePlayer
        }

        private static void OnPlayerRemove(PlayerCore player)
        {
            // Player already released in GameModel.RemovePlayer
        }

        private static void OnLocalPlayerIdChanged(int playerId)
        {
            // Local player ID updated
        }

        private static void OnHostPlayerIdChanged(int hostId)
        {
            // Host ID updated
        }
    }
}