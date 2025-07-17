using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using UnityEngine;

namespace Game
{
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] private PlayerPoolManager playerPoolManager;

        private readonly Dictionary<int, Player> _players = new();
        
        public Player GetPlayer(int id)
        {
            return _players.GetValueOrDefault(id);
        }

        public Player CreatePlayer(PlayerInformation info)
        {
            Debug.Log($"Create Player #{info.PlayerId} at {info.MovementData.Position} with direction {info.MovementData.Direction} and speed {info.MovementData.Speed}");
            var p = playerPoolManager.Pool.Get();
            p.Initialize(info);
            _players.Add(info.PlayerId, p);
            return p;
        }

        public void RemovePlayer(int playerId)
        {
            if (_players.Remove(playerId, out var removed))
            {
                playerPoolManager.Pool.Release(removed);
            }
            else
            {
                Debug.LogWarning($"Player {playerId} could not be removed.");
            }
        }
    }
}