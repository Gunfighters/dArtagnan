using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using Game.Player.Components;
using JetBrains.Annotations;
using UnityEngine;
using ObservableCollections;

namespace Game
{
    public static class PlayerGeneralManager
    {
        public static readonly ObservableDictionary<int, PlayerCore> Players = new();

        public static IEnumerable<PlayerCore> Survivors =>
            Players
                .Where(pair => pair.Value.Health.Alive)
                .Select(pair => pair.Value);

        private static int _localPlayerId;
        private static int _hostId;
        public static PlayerCore LocalPlayerCore => GetPlayer(_localPlayerId);
        public static PlayerCore HostPlayerCore => GetPlayer(_hostId);

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            PacketChannel.On<PlayerJoinBroadcast>(OnJoin);
            PacketChannel.On<YouAre>(OnYouAre);
            PacketChannel.On<NewHostBroadcast>(OnNewHost);
            PacketChannel.On<PlayerLeaveBroadcast>(e => RemovePlayer(e.PlayerId));

            PacketChannel.On<WaitingStartFromServer>(e => ResetEveryone(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(e => ResetEveryone(e.PlayersInfo));

            PacketChannel.On<WaitingStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            PacketChannel.On<RoundStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            PacketChannel.On<RouletteStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
            PacketChannel.On<AugmentStartFromServer>(_ => StopLocalPlayerAndUpdateToServer());
        }

        [CanBeNull]
        public static PlayerCore GetPlayer(int id)
        {
            return Players.GetValueOrDefault(id, null);
        }

        private static void OnJoin(PlayerJoinBroadcast e)
        {
            if (e.PlayerInfo.PlayerId != _localPlayerId)
            {
                CreatePlayer(e.PlayerInfo);
            }
        }

        private static void OnYouAre(YouAre e)
        {
            _localPlayerId = e.PlayerId;
            if (e.PlayerId == _hostId)
                LocalEventChannel.InvokeOnNewHost(HostPlayerCore, HostPlayerCore == LocalPlayerCore);
        }

        private static void OnNewHost(NewHostBroadcast e)
        {
            _hostId = e.HostId;
            LocalEventChannel.InvokeOnNewHost(HostPlayerCore, HostPlayerCore == LocalPlayerCore);
        }

        private static void CreatePlayer(PlayerInformation info)
        {
            var p = PlayerPoolManager.Instance.Pool.Get();
            p.Initialize(info);

            Players.Add(info.PlayerId, p);
        }

        private static void OnLocalPlayerSet()
        {
            LocalEventChannel.InvokeOnNewCameraTarget(
                LocalPlayerCore.Health.Alive
                    ? LocalPlayerCore
                    : Players.First(p => p.Value.Health.Alive).Value);
            LocalEventChannel.InvokeOnLocalPlayerAlive(LocalPlayerCore.Health.Alive);
            LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(LocalPlayerCore.Balance.Balance);
        }

        private static void RemovePlayer(int playerId)
        {
            if (Players.Remove(playerId, out var removed))
            {
                PlayerPoolManager.Instance.Pool.Release(removed);
            }
        }

        private static void ResetEveryone(IEnumerable<PlayerInformation> infoList)
        {
            RemovePlayerAll();
            foreach (var info in infoList)
            {
                CreatePlayer(info);
            }

            OnLocalPlayerSet();
        }

        private static void RemovePlayerAll()
        {
            foreach (var p in Players.ToArray())
            {
                RemovePlayer(p.Value.ID);
            }
        }

        private static void StopLocalPlayerAndUpdateToServer()
        {
            LocalPlayerCore.Physics.Stop();
            PacketChannel.Raise(LocalPlayerCore.Physics.MovementData);
        }
    }
}