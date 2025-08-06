using System.Linq;
using dArtagnan.Shared;
using Game.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.HUD.Controls.ItemCraft
{
    public class ItemCraftButtonView : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Image craftIcon;
        [SerializeField] private Image currentItemIcon;
        [SerializeField] private Image filler;
        [SerializeField] private ItemSo itemCollection;
        [SerializeField] private TextMeshProUGUI costText;
        private bool hasItem;
        private bool canUseItem;

        private void Awake()
        {
            ItemCraftButtonPresenter.Initialize(this);
            currentItemIcon.enabled = false;
        }

        public void ShowItem(int id)
        {
            hasItem = true;
            currentItemIcon.sprite = itemCollection.items.First(item => item.id == id).icon;
            currentItemIcon.enabled = true;
            craftIcon.enabled = false;
        }

        public void HideItem()
        {
            hasItem = false;
            currentItemIcon.enabled = false;
            craftIcon.enabled = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (hasItem && canUseItem)
                PacketChannel.Raise(new UseItemFromClient());
            else if (!hasItem)
                PacketChannel.Raise(new UpdateItemCreatingStateFromClient { IsCreatingItem = false });
            else canUseItem = hasItem;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!hasItem)
            {
                PacketChannel.Raise(new UpdateItemCreatingStateFromClient { IsCreatingItem = true });
                canUseItem = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}