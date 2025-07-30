using dArtagnan.Shared;
using R3;
using R3.Triggers;
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
        [SerializeField] private Toggle selectedToggle;

        public void Setup(int id)
        {
            Augmentation = augmentationCollection.GetAugmentationById(id);
            nameText.text = Augmentation.name;
            image.sprite = Augmentation.sprite;;
            description.text = Augmentation.description;
            selectedToggle.isOn = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.05f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            selectedToggle.isOn = true;
            PacketChannel.Raise(new AugmentDoneFromClient { SelectedAugmentID = Augmentation.id });
        }
    }
}