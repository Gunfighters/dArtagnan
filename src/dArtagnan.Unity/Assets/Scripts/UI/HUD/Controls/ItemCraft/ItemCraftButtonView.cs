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
        private InGameItem _item;
        private bool hasItem;
        private bool canUseItem;

        private void Awake()
        {
            ItemCraftButtonPresenter.Initialize(this);
            currentItemIcon.enabled = false;
        }

        public void ShowItem(ItemId id)
        {
            hasItem = true;
            _item = itemCollection.items.First(item => item.data.Id == id);
            currentItemIcon.sprite = _item.icon;
            costText.text = _item.data.EnergyCost.ToString();
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
            switch (hasItem)
            {
                case true when canUseItem:
                    PacketChannel.Raise(new UseItemFromClient());
                    break;
                case false:
                    PacketChannel.Raise(new UpdateItemCreatingStateFromClient { IsCreatingItem = false });
                    break;
                default:
                    canUseItem = hasItem;
                    break;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (hasItem)
            {
                Debug.Log(_item.data.Description);
            }
            else
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