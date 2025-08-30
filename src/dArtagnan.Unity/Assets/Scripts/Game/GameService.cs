using System.Collections.Generic;
using dArtagnan.Shared;
using Game.Player.Components;
using Game.Player.Data;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Game
{
    public static class GameService
    {
        private static GameModel _instance;

        public static ReadOnlyReactiveProperty<GameState> State => _instance.State;
        public static ReadOnlyReactiveProperty<int> LocalPlayerID => _instance?.LocalPlayerId;
        public static PlayerModel LocalPlayer => _instance?.LocalPlayer;
        
        public static ObservableDictionary<int, PlayerModel> PlayerModels => _instance?.PlayerModels;
        public static IEnumerable<PlayerModel> Survivors => _instance?.Survivors;

        [CanBeNull] public static PlayerModel GetPlayerModel(int id) => _instance?.GetPlayerModel(id);
        [CanBeNull] public static PlayerView GetPlayerView(int id) => _instance?.GetPlayerView(id);

        public static ReactiveProperty<PlayerModel> CameraTarget => _instance?.CameraTarget;
        public static Subject<bool> ConnectionFailure => _instance?.ConnectionFailure;
        public static Subject<string> AlertMessage => _instance?.AlertMessage;
        public static Subject<bool> LocalPlayerAlive => _instance?.LocalPlayerAlive;
        public static Subject<ItemId> LocalPlayerNewItem => _instance?.LocalPlayerNewItem;
        public static Subject<PlayerModel> NewHost => _instance?.NewHost;

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