using dArtagnan.Shared;
using Game.Items;
using Game.Player.Data;
using Game.Player.UI;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.Components
{
    public class PlayerCraft : MonoBehaviour
    {
        [SerializeField] private CraftSlider slider;
        [SerializeField] private Image itemIcon;
        [SerializeField] private ItemSo itemCollection;

        public void Initialize(PlayerInfoModel model)
        {
            model.Crafting.Subscribe(ToggleCraft);
            model.CurrentItem.Subscribe(id =>
            {
                if (id != ItemId.None)
                    SetItem(itemCollection.items.Find(i => i.data.Id == id));
                ToggleItem(id != ItemId.None);
                LocalEventChannel.InvokeOnLocalPlayerNewItem(model.CurrentItem.CurrentValue);
            });
        }

        private void ToggleCraft(bool craft)
        {
            slider.SetProgress(0);
            slider.gameObject.SetActive(craft);
        }

        private void SetItem(InGameItem item)
        {
            itemIcon.sprite = item.icon;
        }

        private void ToggleItem(bool toggle)
        {
            itemIcon.enabled = toggle;
        }
    }
}