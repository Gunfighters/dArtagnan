using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RouletteSlot : MonoBehaviour
{
    public Image ItemImage;
    public TextMeshProUGUI SlotText;

    public void SetItem(ItemSprite item)
    {
        ItemImage.sprite = item.Sprites.Single(s => s.name == "Side");
    }

    public void SetSlotText(string text)
    {
        SlotText.text = text;
    }
}