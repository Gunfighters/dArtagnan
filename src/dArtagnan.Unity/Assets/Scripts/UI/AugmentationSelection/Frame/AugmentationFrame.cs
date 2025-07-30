using TMPro;
using UI.AugmentationSelection.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.AugmentationSelection.Frame
{
    public class AugmentationFrame : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Augmentation Augmentation { get; private set; }
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private AugmentationCollection augmentationCollection;

        public void Setup(int id)
        {
            Augmentation = augmentationCollection.GetAugmentationById(id);
            nameText.text = Augmentation.name;
            image.sprite = Augmentation.sprite;
            description.text = Augmentation.description;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale *= 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            
        }
    }
}