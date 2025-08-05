using System.Collections.Generic;
using System.Linq;
using Game.Player.Components;
using UnityEngine;

namespace UI.HUD.PlayerList
{
    public class PlayerListView : MonoBehaviour
    {
        public PlayerListItem itemPrefab;
        private List<PlayerListItem> _pool;

        private void Awake()
        {
            _pool = GetComponentsInChildren<PlayerListItem>(true).ToList();
            _pool.ForEach(obj => obj.gameObject.SetActive(false));
            PlayerListPresenter.Initialize(this);
        }

        public void Add(PlayerCore info)
        {
            var obj = _pool.Find(obj => !obj.gameObject.activeSelf) ?? Instantiate(itemPrefab, transform);
            obj.Setup(info);
            obj.gameObject.SetActive(true);
            obj.transform.SetSiblingIndex(info.ID);
        }

        public void Remove(PlayerCore info)
        {
            _pool.Single(item => item.ID == info.ID).gameObject.SetActive(false);
        }
    }
}