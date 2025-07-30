using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Roulette
{
    public class RouletteSlot : MonoBehaviour
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI slotText;
        [SerializeField] private SpriteCollection gunCollection;
        public bool IsTarget { get; private set; }

        public void Setup(RouletteItem item)
        {
            itemImage.sprite = gunCollection
                .GunSpriteByAccuracy(item.value)
                .Sprites
                .Single(s => s.name == "Side");
            slotText.text = $"{item.value}%";
            IsTarget = item.isTarget;
        }
    }
}