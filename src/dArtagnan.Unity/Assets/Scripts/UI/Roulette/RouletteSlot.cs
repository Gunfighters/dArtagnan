using System.Linq;
using TMPro;
using UI.Roulette;
using UnityEngine;
using UnityEngine.UI;

public class RouletteSlot : MonoBehaviour
{
    [SerializeField] private Image ItemImage;
    [SerializeField] private TextMeshProUGUI SlotText;

    public void Setup(RouletteItem item)
    {
        ItemImage.sprite = item.icon.Sprites.Single(s => s.name == "Side");
        SlotText.text = item.name;
    }
}