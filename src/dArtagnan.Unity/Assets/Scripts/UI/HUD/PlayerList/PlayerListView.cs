using System.Collections.Generic;
using System.Linq;
using Game.Player.Components;
using UnityEngine;

namespace UI.HUD.PlayerList
{
    public class PlayerListView : MonoBehaviour
    {
        public PlayerListItem itemPrefab;
        private readonly List<PlayerListItem> _pool = new();

        private void Awake() => PlayerListPresenter.Initialize(this);

        public void Add(PlayerCore player)
        {
            var obj = Instantiate(itemPrefab, transform);
            obj.Initialize(player);
            _pool.Add(obj);
        }

        public void Remove(PlayerCore player)
        {
            var removed = _pool.Single(item => item.ID == player.ID);
            _pool.Remove(removed);
            Destroy(removed.gameObject);
        }
    }
}