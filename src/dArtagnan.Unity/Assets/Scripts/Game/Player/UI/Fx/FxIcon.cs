using dArtagnan.Shared;
using Game.Items;
using UI.AugmentationSelection.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI.Fx
{
    public class FxIcon : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private ItemSo itemCollection;
        [SerializeField] private AugmentationCollection augmentationCollection;

        public void Setup(int id)
        {
            if (itemCollection.items.Exists(item => item.data.Id == (ItemId)id))
                icon.sprite = itemCollection.items.Find(item => item.data.Id == (ItemId)id).icon;
            else if (augmentationCollection.augmentations.Exists(aug => aug.data.Id == (AugmentId)id))
                icon.sprite = augmentationCollection.augmentations.Find(aug => aug.data.Id == (AugmentId)id).sprite;
            else
                icon.sprite = null;
        }
    }
}