using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using JetBrains.Annotations;
using UnityEngine;

namespace Game
{
    public class PlayerGeneralManager : MonoBehaviour, IChannelListener
    {
        private static readonly Dictionary<int, Player> Players = new();
        public static IEnumerable<Player> Survivors => Players.Values.Where(p => p.Alive);
        private static int _localPlayerId;
        private static int _hostId;
        public static Player LocalPlayer => GetPlayer(_localPlayerId);
        public static Player HostPlayer => GetPlayer(_hostId);

        public void Initialize()
        {
            PacketChannel.On<PlayerJoinBroadcast>(OnJoin);
            PacketChannel.On<YouAre>(OnYouAre);
            PacketChannel.On<NewHostBroadcast>(OnNewHost);
            PacketChannel.On<PlayerLeaveBroadcast>(e => RemovePlayer(e.PlayerId));

            PacketChannel.On<WaitingStartFromServer>(e => ResetEveryone(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(e => ResetEveryone(e.PlayersInfo));
        }
        
        public static Player GetPlayer(int id)
        {
            return Players.GetValueOrDefault(id);
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
                LocalEventChannel.InvokeOnNewHost(HostPlayer, HostPlayer == LocalPlayer);
        }

        private static void OnNewHost(NewHostBroadcast e)
        {
            _hostId = e.HostId;
            LocalEventChannel.InvokeOnNewHost(HostPlayer, HostPlayer == LocalPlayer);
        }
        
        private static void CreatePlayer(PlayerInformation info)
        {
            var p = PlayerPoolManager.Instance.Pool.Get();
            
            bool isRemotePlayer = info.PlayerId != _localPlayerId;
            p.Initialize(info, isRemotePlayer);
            p.Physics.Initialize(isRemotePlayer);
            
            Players.Add(info.PlayerId, p);
            
            if (info.PlayerId == _localPlayerId)
            {
                LocalEventChannel.InvokeOnNewCameraTarget(p);
                LocalEventChannel.InvokeOnLocalPlayerAlive(true);
                LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(p.Balance);
            }
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
        }

        private static void RemovePlayerAll()
        {
            foreach (var p in Players.Values.ToList())
            {
                RemovePlayer(p.ID);
            }
        }
    }
}