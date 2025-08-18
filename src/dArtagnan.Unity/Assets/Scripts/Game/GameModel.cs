using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using Game.Player.Components;
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

        public readonly ObservableDictionary<int, PlayerCore> Players = new();
        public readonly ReactiveProperty<GameState> State = new();

        public GameModel()
        {
            PacketChannel.On<JoinBroadcast>(OnJoin);
            PacketChannel.On<YouAreFromServer>(OnYouAre);
            PacketChannel.On<NewHostBroadcast>(OnNewHost);
            PacketChannel.On<LeaveBroadcast>(e => RemovePlayer(e.PlayerId));

            PacketChannel.On<WaitingStartFromServer>(e => ResetEveryone(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(e => ResetEveryone(e.PlayersInfo));

            PacketChannel.On<WaitingStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            PacketChannel.On<RoundStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            PacketChannel.On<RouletteStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            PacketChannel.On<AugmentStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());

            PacketChannel.On<RoundStartFromServer>(_ => State.Value = GameState.Round);
            PacketChannel.On<WaitingStartFromServer>(_ => State.Value = GameState.Waiting);
            PacketChannel.On<RouletteStartFromServer>(_ => State.Value = GameState.Roulette);
        }

        public IEnumerable<PlayerCore> Survivors =>
            Players
                .Where(pair => pair.Value.Health.Alive.CurrentValue)
                .Select(pair => pair.Value);

        public PlayerCore LocalPlayerCore => GetPlayer(LocalPlayerId.Value);
        public PlayerCore HostPlayerCore => GetPlayer(HostPlayerId.Value);

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        [CanBeNull]
        public PlayerCore GetPlayer(int id)
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
                LocalEventChannel.InvokeOnNewHost(HostPlayerCore, HostPlayerCore == LocalPlayerCore);
        }

        private void OnNewHost(NewHostBroadcast e)
        {
            HostPlayerId.Value = e.HostId;
            LocalEventChannel.InvokeOnNewHost(HostPlayerCore, HostPlayerCore == LocalPlayerCore);
        }

        private void CreatePlayer(PlayerInformation info)
        {
            var p = PlayerPoolManager.Instance.Pool.Get();
            p.Initialize(info);

            Players.Add(info.PlayerId, p);
        }

        private void OnLocalPlayerSet()
        {
            LocalEventChannel.InvokeOnNewCameraTarget(
                LocalPlayerCore.Health.Alive.CurrentValue
                    ? LocalPlayerCore
                    : Players.First(p => p.Value.Health.Alive.CurrentValue).Value);
            LocalEventChannel.InvokeOnLocalPlayerAlive(LocalPlayerCore.Health.Alive.CurrentValue);
            LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(LocalPlayerCore.Balance.Balance);
        }

        private void RemovePlayer(int playerId)
        {
            if (Players.Remove(playerId, out var removed))
            {
                PlayerPoolManager.Instance.Pool.Release(removed);
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
                RemovePlayer(p.Value.ID);
            }
        }

        private void StopLocalPlayerAndUpdateToServer()
        {
            LocalPlayerCore.Physics.Stop();
            PacketChannel.Raise(LocalPlayerCore.Physics.MovementData);
        }
    }
}