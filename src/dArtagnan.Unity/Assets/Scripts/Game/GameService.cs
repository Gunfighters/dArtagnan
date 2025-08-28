using System.Collections.Generic;
using dArtagnan.Shared;
using Game.Player.Components;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Game
{
    public static class GameService
    {
        private static GameModel _instance;

        public static ReadOnlyReactiveProperty<GameState> State => _instance.State.ToReadOnlyReactiveProperty();

        public static ReadOnlyReactiveProperty<int> LocalPlayerId =>
            _instance?.LocalPlayerId.ToReadOnlyReactiveProperty();

        public static ReadOnlyReactiveProperty<int> HostPlayerId =>
            _instance?.HostPlayerId.ToReadOnlyReactiveProperty();

        public static PlayerCore LocalPlayer => _instance?.LocalPlayer;
        public static PlayerCore HostPlayer => _instance?.HostPlayer;

        public static ObservableDictionary<int, PlayerCore> Players => _instance?.Players;
        public static IEnumerable<PlayerCore> Survivors => _instance?.Survivors;

        [CanBeNull]
        public static PlayerCore GetPlayer(int id)
        {
            return _instance?.GetPlayer(id);
        }

        public static void SetInstance(GameModel gameModel)
        {
            _instance = gameModel;
        }

        public static void ClearInstance()
        {
            _instance = null;
        }
    }
}