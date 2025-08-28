using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
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

        public readonly ObservableDictionary<int, PlayerModel> Players = new();
        public readonly ReactiveProperty<GameState> State = new();

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

        public IEnumerable<PlayerModel> Survivors =>
            Players
                .Where(pair => pair.Value.Alive.CurrentValue)
                .Select(pair => pair.Value);

        public PlayerModel LocalPlayer => GetPlayer(LocalPlayerId.Value);
        public PlayerModel HostPlayer => GetPlayer(HostPlayerId.Value);

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        [CanBeNull]
        public PlayerModel GetPlayer(int id)
        {
            return Players.GetValueOrDefault(id, null);
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
            if (e.PlayerId == HostPlayerId.Value)
                LocalEventChannel.InvokeOnNewHost(HostPlayer, HostPlayer == LocalPlayer);
        }

        private void OnNewHost(NewHostBroadcast e)
        {
            HostPlayerId.Value = e.HostId;
            LocalEventChannel.InvokeOnNewHost(HostPlayer, HostPlayer == LocalPlayer);
        }

        private void CreatePlayer(PlayerInformation info)
        {
            var view = PlayerPoolManager.Instance.Pool.Get();
            var model = new PlayerModel(info);
            PlayerPresenter.Initialize(model, view);
            Players.Add(info.PlayerId, model);
        }

        private void OnLocalPlayerSet()
        {
            LocalEventChannel.InvokeOnNewCameraTarget(
                LocalPlayer.Alive.CurrentValue
                    ? LocalPlayer
                    : Players.First(p => p.Value.Alive.CurrentValue).Value);
            LocalEventChannel.InvokeOnLocalPlayerAlive(LocalPlayer.Alive.CurrentValue);
            LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(LocalPlayer.Balance.CurrentValue);
        }

        private void RemovePlayer(int playerId)
        {
            if (Players.Remove(playerId, out var removed))
            {
                PlayerPoolManager.Instance.Pool.Release(GameService.GetPlayer(removed.ID.CurrentValue));
            }
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
            foreach (var p in Players.ToArray())
            {
                RemovePlayer(p.Value.ID.CurrentValue);
            }
        }
    }
}