using System.Linq;
using dArtagnan.Shared;
using Game;
using Game.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.HUD.Controls.ItemCraft
{
    public class ItemCraftButtonView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Image outline;
        [SerializeField] private Image craftIcon;
        [SerializeField] private Image currentItemIcon;
        [SerializeField] private Image filler;
        [SerializeField] private ItemSo itemCollection;
        [SerializeField] private Image costIcon;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private GameObject descriptionBox;
        [SerializeField] private TextMeshProUGUI descriptionText;
        private bool _hasItem;
        private InGameItem _item;

        private void Awake()
        {
            descriptionBox.SetActive(false);
            currentItemIcon.enabled = false;
            costText.text = Constants.CRAFT_ENERGY_COST.ToString();
        }

        private void Start()
        {
            ItemCraftButtonPresenter.Initialize(this);
        }

        private void Update()
        {
            if (_hasItem)
            {
                filler.fillAmount = GameService.LocalPlayer.EnergyData.CurrentValue.CurrentEnergy / _item.data.EnergyCost;
                if (filler.fillAmount >= 1)
                {
                    outline.color = Color.green;
                    costIcon.color = costText.color = currentItemIcon.color = Color.white;
                }
                else
                {
                    outline.color = Color.grey;
                    costIcon.color = costText.color = currentItemIcon.color = Color.grey;
                }
            }
            else if (GameService.LocalPlayer.Crafting.CurrentValue)
                filler.fillAmount = 1;
            else
            {
                var ratio = GameService
                                .LocalPlayer
                                .EnergyData
                                .CurrentValue
                                .CurrentEnergy /
                            Constants.CRAFT_ENERGY_COST;
                filler.fillAmount = ratio;
                craftIcon.color = costIcon.color = costText.color = ratio >= 1 ? Color.white : Color.grey;
                outline.color = ratio >= 1 && !GameService.LocalPlayer.Crafting.CurrentValue
                    ? Color.green
                    : Color.grey;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_hasItem)
            {
                if (GameService.LocalPlayer.EnergyData.CurrentValue.CurrentEnergy < _item.data.EnergyCost)
                    GameService.AlertMessage.OnNext("에너지가 부족합니다");
                else
                    PacketChannel.Raise(new UseItemFromClient());
            }
            else
            {
                if (GameService.LocalPlayer.EnergyData.CurrentValue.CurrentEnergy < Constants.CRAFT_ENERGY_COST)
                    GameService.AlertMessage.OnNext("에너지가 부족합니다");
                else
                {
                    GameService.LocalPlayer.Direction.Value = Vector2.zero;
                    PacketChannel.Raise(GameService.LocalPlayer.GetMovementDataFromClient());
                    PacketChannel.Raise(new UpdateItemCreatingStateFromClient { IsCreatingItem = true });
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.1f;
            descriptionBox.SetActive(_hasItem);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
            descriptionBox.SetActive(false);
        }

        public void ShowItem(ItemId id)
        {
            if (id is ItemId.None or 0) return;
            _hasItem = true;
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
            _hasItem = false;
            currentItemIcon.enabled = false;
            craftIcon.enabled = true;
        }
    }
}