using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using JetBrains.Annotations;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 플레이어 생성, 플레이어 삭제, 방장 설정, 로컬플레이어 설정을 관리하는 매니저.
    /// </summary>
    public class PlayerGeneralManager : MonoBehaviour
    {
        private static readonly Dictionary<int, Player> Players = new();
        public static IEnumerable<Player> Survivors => Players.Values.Where(p => p.Alive);
        private static int _localPlayerId;
        private static int _hostId;
        public static Player LocalPlayer => GetPlayer(_localPlayerId);
        public static Player HostPlayer => GetPlayer(_hostId); // TODO: private

        public void Awake()
        {
            PacketChannel.On<PlayerJoinBroadcast>(OnJoin);
            PacketChannel.On<YouAre>(OnYouAre);
            PacketChannel.On<NewHostBroadcast>(OnNewHost);
            PacketChannel.On<PlayerLeaveBroadcast>(e => RemovePlayer(e.PlayerId));

            PacketChannel.On<GameInWaitingFromServer>(e => ResetEveryone(e.PlayersInfo));
            PacketChannel.On<GameInPlayingFromServer>(e => ResetEveryone(e.PlayersInfo));
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
            Debug.Log($"Create Player #{info.PlayerId} at {info.MovementData.Position} with direction {info.MovementData.Direction} and speed {info.MovementData.Speed}");
            var p = PlayerPoolManager.Instance.Pool.Get();
            p.Initialize(info);
            Players.Add(info.PlayerId, p);
            if (p == LocalPlayer)
            {
                LocalEventChannel.InvokeOnNewCameraTarget(p);
            }
        }

        private static void RemovePlayer(int playerId)
        {
            if (Players.Remove(playerId, out var removed))
            {
                PlayerPoolManager.Instance.Pool.Release(removed);
            }
            else
            {
                Debug.LogWarning($"Player {playerId} could not be removed.");
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