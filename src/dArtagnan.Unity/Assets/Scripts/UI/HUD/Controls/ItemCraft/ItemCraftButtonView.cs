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
        [SerializeField] private GameObject descriptionBox;
        [SerializeField] private TextMeshProUGUI descriptionText;
        private InGameItem _item;
        private bool hasItem;
        private bool canUseItem;

        private void Awake()
        {
            ItemCraftButtonPresenter.Initialize(this);
            descriptionBox.SetActive(false);
            currentItemIcon.enabled = false;
            costText.text = Constants.CRAFT_ENERGY_COST.ToString();
        }

        public void ShowItem(ItemId id)
        {
            hasItem = true;
            _item = itemCollection.items.First(item => item.data.Id == id);
            currentItemIcon.sprite = _item.icon;
            costText.text = _item.data.EnergyCost.ToString();
            descriptionText.text = _item.data.Description;
            currentItemIcon.enabled = true;
            craftIcon.enabled = false;
        }

        public void HideItem()
        {
            costText.text = Constants.CRAFT_ENERGY_COST.ToString();
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
                    descriptionBox.SetActive(false);
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
            if (!hasItem)
            {
                PacketChannel.Raise(new UpdateItemCreatingStateFromClient { IsCreatingItem = true });
                canUseItem = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.1f;
            descriptionBox.SetActive(hasItem);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
            descriptionBox.SetActive(false);
        }
    }
}