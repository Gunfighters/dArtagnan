using System.Linq;
using dArtagnan.Shared;
using Game.Items;
using Game.Player.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.Components
{
    public class PlayerCraft : MonoBehaviour
    {
        [SerializeField] private CraftSlider slider;
        [SerializeField] private Image itemIcon;
        [SerializeField] private ItemSo itemCollection;
        private PlayerCore _core;
        public bool Crafting { get; private set; }

        private void Awake()
        {
            _core = GetComponent<PlayerCore>();
        }

        private void SetMotion(bool dig)
        {
            if (dig)
                _core.Model.Craft();
            else
                _core.Model.Idle();
        }

        public void Initialize(PlayerInformation info)
        {
            ToggleCraft(info.IsCreatingItem);
            if (info.CurrentItem != -1)
                SetItem(itemCollection.items.Find(i => i.data.Id == (ItemId)info.CurrentItem));
            ToggleItem(info.CurrentItem != -1);
        }

        public void ToggleCraft(bool craft)
        {
            Crafting = craft;
            slider.SetProgress(0);
            slider.gameObject.SetActive(craft);
            SetMotion(Crafting);
        }

        public void SetItem(InGameItem item)
        {
            itemIcon.sprite = item.icon;
        }

        public void ToggleItem(bool toggle)
        {
            itemIcon.enabled = toggle;
        }
    }
}