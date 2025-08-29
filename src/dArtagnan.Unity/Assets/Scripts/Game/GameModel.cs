using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using Game.Player.Components;
using Game.Player.Data;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Game
{
    public class GameModel : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        public readonly ReactiveProperty<int> HostPlayerId = new();
        public readonly ReactiveProperty<int> LocalPlayerId = new();

        public readonly ObservableDictionary<int, PlayerModel> PlayerModels = new();
        public readonly ReactiveProperty<GameState> State = new();
        
        public IEnumerable<PlayerModel> Survivors =>
            PlayerModels
                .Where(pair => pair.Value.Alive.CurrentValue)
                .Select(pair => pair.Value);

        public PlayerModel LocalPlayer => GetPlayerModel(LocalPlayerId.Value);
        public PlayerModel HostPlayer => GetPlayerModel(HostPlayerId.Value);

        private readonly Dictionary<int, PlayerView> _playerViews = new();
        public PlayerView GetPlayerView(int id) => _playerViews[id];

        public readonly ReactiveProperty<PlayerModel> CameraTarget = new();
        public readonly Subject<bool> ConnectionFailure = new();
        public readonly Subject<string> AlertMessage = new();
        public readonly Subject<bool> LocalPlayerAlive = new();
        public readonly Subject<PlayerModel> NewHost = new();
        public readonly Subject<ItemId> LocalPlayerNewItem = new();

        public GameModel()
        {
            PacketChannel.On<JoinBroadcast>(OnJoin);
            PacketChannel.On<YouAreFromServer>(OnYouAre);
            PacketChannel.On<NewHostBroadcast>(OnNewHost);
            PacketChannel.On<LeaveBroadcast>(e => RemovePlayer(e.PlayerId));

            PacketChannel.On<WaitingStartFromServer>(e => ResetEveryone(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(e => ResetEveryone(e.PlayersInfo));
            
            // TODO
            // PacketChannel.On<WaitingStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            // PacketChannel.On<RoundStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            // PacketChannel.On<ShowdownStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            // PacketChannel.On<AugmentStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());

            PacketChannel.On<RoundStartFromServer>(_ => State.Value = GameState.Round);
            PacketChannel.On<WaitingStartFromServer>(_ => State.Value = GameState.Waiting);
            PacketChannel.On<ShowdownStartFromServer>(_ => State.Value = GameState.Showdown);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        [CanBeNull]
        public PlayerModel GetPlayerModel(int id)
        {
            return PlayerModels.GetValueOrDefault(id, null);
        }

        private void OnJoin(JoinBroadcast e)
        {
            if (e.PlayerInfo.PlayerId != LocalPlayerId.Value)
            {
                CreatePlayer(e.PlayerInfo);
            }
        }

        private void OnYouAre(YouAreFromServer e)
        {
            LocalPlayerId.Value = e.PlayerId;
            NewHost.OnNext(HostPlayer);
        }

        private void OnNewHost(NewHostBroadcast e)
        {
            HostPlayerId.Value = e.HostId;
            NewHost.OnNext(HostPlayer);
        }

        private void CreatePlayer(PlayerInformation info)
        {
            var view = PlayerPoolManager.Instance.Pool.Get();
            var model = new PlayerModel(info);
            PlayerPresenter.Initialize(model, view);
            PlayerModels.Add(info.PlayerId, model);
            _playerViews.Add(info.PlayerId, view);
        }

        private void OnLocalPlayerSet()
        {
            CameraTarget.Value = LocalPlayer.Alive.CurrentValue
                ? LocalPlayer
                : PlayerModels.First(p => p.Value.Alive.CurrentValue).Value;
            LocalPlayerAlive.OnNext(LocalPlayer.Alive.CurrentValue);
        }

        private void RemovePlayer(int playerId)
        {
            if (PlayerModels.Remove(playerId, out var model))
                if (_playerViews.Remove(playerId, out var view))
                    PlayerPoolManager.Instance.Pool.Release(view);
        }

        private void ResetEveryone(IEnumerable<PlayerInformation> infoList)
        {
            RemovePlayerAll();
            foreach (var info in infoList)
            {
                CreatePlayer(info);
            }

            OnLocalPlayerSet();
        }

        private void RemovePlayerAll()
        {
            foreach (var p in PlayerModels.ToArray())
            {
                RemovePlayer(p.Value.ID.CurrentValue);
            }
        }
    }
}