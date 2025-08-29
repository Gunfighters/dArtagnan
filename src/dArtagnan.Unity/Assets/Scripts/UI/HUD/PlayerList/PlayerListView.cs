using System.Collections.Generic;
using System.Linq;
using Game.Player.Data;
using UnityEngine;

namespace UI.HUD.PlayerList
{
    public class PlayerListView : MonoBehaviour
    {
        public PlayerListItem itemPrefab;
        private readonly List<PlayerListItem> _pool = new();

        private void Start()
        {
            foreach (var item in GetComponentsInChildren<PlayerListItem>())
            {
                Destroy(item.gameObject);
            }

            PlayerListPresenter.Initialize(new PlayerListModel(), this);
        }

        public void Add(PlayerModel player)
        {
            var obj = Instantiate(itemPrefab, transform);
            obj.Initialize(player);
            _pool.Add(obj);
        }

        public void Remove(PlayerModel player)
        {
            var removed = _pool.Single(item => item.PlayerModel.ID == player.ID);
            _pool.Remove(removed);
            Destroy(removed.gameObject);
        }
    }
}