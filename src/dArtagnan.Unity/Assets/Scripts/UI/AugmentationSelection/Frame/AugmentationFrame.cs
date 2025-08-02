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

        private void Awake()
        {
            selectedToggle.onValueChanged.AddListener(value =>
            {
                if (value)
                    AugmentationSelectionModel.SelectAugmentation(Augmentation.id);
            });
        }

        public void Setup(int id)
        {
            Augmentation = augmentationCollection.GetAugmentationById(id);
            nameText.text = Augmentation.name;
            image.sprite = Augmentation.sprite;
            ;
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
            if (AugmentationSelectionModel.IsSelectionComplete.Value) return;
            AugmentationSelectionModel.SelectAugmentation(Augmentation.id);
        }

        public void UpdateSelection(bool isSelected)
        {
            selectedToggle.isOn = isSelected;
        }

        public void SetInteractable(bool interactable)
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = interactable;
            canvasGroup.alpha = interactable ? 1f : 0.4f;
        }
    }
}