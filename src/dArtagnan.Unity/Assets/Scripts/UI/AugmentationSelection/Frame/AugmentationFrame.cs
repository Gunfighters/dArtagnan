using TMPro;
using UI.AugmentationSelection.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI.AugmentationSelection.Frame
{
    public class AugmentationFrame : MonoBehaviour
    {
        public Augmentation Augmentation { get; private set; }
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private AugmentationCollection augmentationCollection;

        public void Setup(int id)
        {
            Augmentation = augmentationCollection.GetAugmentationById(id);
            image.sprite = Augmentation.sprite;
            description.text = Augmentation.description;
        }
    }
}